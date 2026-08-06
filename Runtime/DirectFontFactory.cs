// ==========================================
// DirectFontFactory
// The one place that actually talks to
// TextMeshPro's dynamic-font machinery. Given a
// Unity Font, a file path, or a raw byte[], it
// hands back a *dynamic* TMP_FontAsset - one that
// rasterizes glyphs from the source on demand
// rather than from a pre-baked atlas.
//
// Why it looks the way it does:
//   * From a Font, TextMeshPro has exposed a stable
//     public factory since TMP 3.0, so that path is
//     bound at compile time and Just Works on every
//     supported Unity version.
//   * From a path or from bytes, the clean public
//     factory only arrived in TMP 3.2 (Unity 2022.2+).
//     We call it by reflection when it's present, so
//     the package still compiles - and prefers the
//     supported route - on newer editors, while older
//     ones fall back to the low-level FontEngine
//     builder below. All of that is hidden behind
//     three tidy Create* methods.
// ==========================================
using System;
using System.Reflection;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace UnityDirectTMP
{
    /// <summary>
    /// Turns a font source (Unity <see cref="Font"/>, file path, or raw bytes)
    /// into a dynamic <see cref="TMP_FontAsset"/>. Stateless; every result is
    /// owned and cached by <see cref="DirectFontCache"/>.
    /// </summary>
    internal static class DirectFontFactory
    {
        private static bool s_engineInitialized;

        // Cached reflection handles for the TMP 3.2+ path/byte factories,
        // resolved once. `null` after resolution means "this editor's TMP
        // does not expose that overload; use the FontEngine fallback".
        private static bool s_pathFactoryResolved;
        private static MethodInfo s_pathFactory;
        private static bool s_bytesFactoryResolved;
        private static MethodInfo s_bytesFactory;

        // ==========================================
        // CreateFromFont
        // The recommended, fully-supported route. A
        // .ttf/.otf sitting in your project already *is*
        // a Unity Font, so "drag the font file onto the
        // component" ends up here.
        // ==========================================
        public static TMP_FontAsset CreateFromFont(Font font, DirectFontSettings settings)
        {
            if (font == null) { return null; }
            settings = settings.Clamped();

            // Whatever is wrong with the PROJECT is wrong before the first
            // font is touched, and saying so here is the difference between a
            // package that explains itself and one that appears not to have
            // installed.
            DirectFontDiagnostics.ReportProjectOnce();

            TMP_FontAsset asset = null;
            try
            {
                asset = TMP_FontAsset.CreateFontAsset(
                    font,
                    settings.SamplingPointSize,
                    settings.AtlasPadding,
                    settings.RenderMode,
                    settings.AtlasWidth,
                    settings.AtlasHeight,
                    AtlasPopulationMode.Dynamic,
                    true);
            }
            catch (Exception e)
            {
                DirectTMPLog.Warn($"TextMeshPro's font factory threw on '{font.name}': {e.Message}");
            }

            if (asset != null)
            {
                FinalizeAsset(asset, font.name);
                RememberSource(asset, ResolveFontFilePath(font), null);
                return asset;
            }

            // ==========================================
            // The route round the importer.
            //
            // Everything that makes CreateFontAsset(Font)
            // return null is a property of the IMPORT, not
            // of the font: "Include Font Data" switched
            // off, a baked character set, a Font asset
            // Unity built rather than imported. The file on
            // disk is fine in every one of those cases, and
            // the FontEngine will happily open it by path.
            //
            // So the font is read directly. That is what
            // makes the same .ttf behave the same way in
            // every project instead of depending on a .meta
            // file somebody else generated.
            // ==========================================
            string filePath = ResolveFontFilePath(font);
            if (!string.IsNullOrEmpty(filePath))
            {
                TMP_FontAsset fromFile = CreateFromPath(filePath, settings);
                if (fromFile != null)
                {
                    DirectTMPLog.Warn(
                        DirectFontDiagnostics.ExplainFont(font)
                        + $"\nThe file was read directly from disk instead, so '{font.name}' works anyway — but "
                        + "fixing the import settings is faster and makes the font behave the same everywhere.");
                    return fromFile;
                }
            }

            // Once per font, not once per label. A failed build is not cached
            // (so a project that gets fixed recovers without a restart), which
            // means every enabled label would otherwise re-report it.
            if (s_reportedFailures.Add(font.GetInstanceID()))
            {
                DirectTMPLog.Error(DirectFontDiagnostics.ExplainFont(font), font);
            }
            return null;
        }

        private static readonly HashSet<int> s_reportedFailures = new HashSet<int>();

        // ==========================================
        // ResolveFontFilePath
        //
        // Where a Unity Font actually lives on disk, when
        // that is knowable. In the Editor the
        // AssetDatabase always knows. In a player build
        // nothing does - the .ttf was compiled into the
        // Font asset and there is no file any more - so
        // this returns null and the caller reports the
        // real problem rather than inventing a path.
        // ==========================================
        internal static string ResolveFontFilePath(Font font)
        {
            if (font == null) { return null; }

#if UNITY_EDITOR
            try
            {
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(font);
                if (string.IsNullOrEmpty(assetPath)) { return null; }

                string extension = System.IO.Path.GetExtension(assetPath).ToLowerInvariant();
                if (System.Array.IndexOf(DirectTMPConstants.FontExtensions, extension) < 0) { return null; }

                string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath.Replace('\\', '/'));
                string absolute = (projectRoot + "/" + assetPath).Replace('\\', '/');
                return System.IO.File.Exists(absolute) ? absolute : null;
            }
            catch (Exception)
            {
                return null;
            }
#else
            return null;
#endif
        }

        // The bytes behind an asset, remembered so DirectFontForms can read the
        // font's own joining rules later. A TMP_FontAsset keeps a Unity Font,
        // and a Font's file is not readable from a player build - so whoever
        // had the path or the buffer is the only one who can say.
        private static void RememberSource(TMP_FontAsset asset, string path, byte[] bytes)
        {
            DirectFontForms.Remember(asset, path, bytes);
        }

        // ==========================================
        // CreateFromPath
        // A .ttf/.otf anywhere on disk - StreamingAssets,
        // persistentDataPath, an absolute path a mod
        // dropped in. Prefers the public TMP 3.2+ factory,
        // then falls back to the FontEngine builder.
        // ==========================================
        public static TMP_FontAsset CreateFromPath(string absolutePath, DirectFontSettings settings)
        {
            if (string.IsNullOrEmpty(absolutePath) || !System.IO.File.Exists(absolutePath))
            {
                DirectTMPLog.Warn($"Font file not found: {absolutePath}");
                return null;
            }

            settings = settings.Clamped();
            string name = System.IO.Path.GetFileNameWithoutExtension(absolutePath);

            MethodInfo factory = ResolvePathFactory();
            if (factory != null)
            {
                TMP_FontAsset asset = InvokeSourceFactory(factory, absolutePath, settings);
                if (asset != null) { FinalizeAsset(asset, name); RememberSource(asset, absolutePath, null); return asset; }
            }

            // Older TMP: build it ourselves from the FontEngine.
            TMP_FontAsset built = BuildViaFontEngine(() => FontEngine.LoadFontFace(absolutePath, settings.SamplingPointSize), settings, name);
            RememberSource(built, absolutePath, null);
            return built;
        }

        // ==========================================
        // CreateFromBytes
        // A font that only exists in memory - just
        // downloaded, or read out of an asset bundle.
        // The FontEngine reads a face far more reliably
        // from a path, so on the fallback route we spool
        // the bytes to the runtime cache folder first.
        // ==========================================
        public static TMP_FontAsset CreateFromBytes(byte[] fontBytes, DirectFontSettings settings, string displayName = null)
        {
            if (fontBytes == null || fontBytes.Length == 0)
            {
                DirectTMPLog.Warn($"Empty font byte buffer.");
                return null;
            }

            settings = settings.Clamped();
            string name = string.IsNullOrEmpty(displayName) ? "MemoryFont" : displayName;

            MethodInfo factory = ResolveBytesFactory();
            if (factory != null)
            {
                TMP_FontAsset asset = InvokeSourceFactory(factory, fontBytes, settings);
                if (asset != null) { FinalizeAsset(asset, name); RememberSource(asset, null, fontBytes); return asset; }
            }

            // Older TMP: spool to disk and go through the path builder.
            string spooled = SpoolToCache(fontBytes);
            if (spooled != null)
            {
                TMP_FontAsset built = BuildViaFontEngine(() => FontEngine.LoadFontFace(spooled, settings.SamplingPointSize), settings, name);
                RememberSource(built, spooled, fontBytes);
                return built;
            }
            return null;
        }

        // ==========================================
        // FinalizeAsset
        // Shared post-processing for every freshly built
        // asset: a recognisable name and one definition
        // read so its lookup tables are ready.
        // ==========================================
        private static void FinalizeAsset(TMP_FontAsset asset, string sourceName)
        {
            if (asset == null) { return; }
            asset.name = (string.IsNullOrEmpty(sourceName) ? "Font" : sourceName) + DirectTMPConstants.GeneratedAssetSuffix;
            asset.hideFlags = HideFlags.DontSave;
            asset.ReadFontAssetDefinition();
            EnsureMaterial(asset);
        }

        // ==========================================
        // EnsureMaterial
        //
        // TextMeshPro builds a generated font asset's
        // material on ShaderUtilities' cached shader
        // reference, and in a project whose TMP resources
        // were never imported that reference is null. A
        // Material with a null shader is not an error
        // anybody sees: it is a label that renders
        // nothing, which reads as "the font did not
        // apply". So the shader is looked up again by
        // name here, and if there genuinely is none, the
        // reason is stated rather than left to be
        // deduced from a blank screen.
        // ==========================================
        private static void EnsureMaterial(TMP_FontAsset asset)
        {
            try
            {
                if (asset.material != null && asset.material.shader != null) { return; }

                Shader shader = DirectFontDiagnostics.FindDistanceFieldShader();
                if (shader == null)
                {
                    DirectTMPLog.Error(DirectFontDiagnostics.ProjectProblem()
                        ?? "TextMeshPro's distance-field shader is missing, so this font has no material to draw with.");
                    return;
                }

                Texture atlas = asset.atlasTexture;
                var material = new Material(shader) { hideFlags = HideFlags.DontSave };
                if (atlas != null) { material.SetTexture(ShaderUtilities.ID_MainTex, atlas); }
                material.SetFloat(ShaderUtilities.ID_TextureWidth, asset.atlasWidth);
                material.SetFloat(ShaderUtilities.ID_TextureHeight, asset.atlasHeight);
                material.SetFloat(ShaderUtilities.ID_GradientScale, asset.atlasPadding + 1);

                asset.material = material;
            }
            catch (Exception e)
            {
                DirectTMPLog.Warn($"Could not give '{asset.name}' a material: {e.Message}");
            }
        }

        // ==========================================
        // ResolvePathFactory / ResolveBytesFactory
        // Look up TMP's public path/byte CreateFontAsset
        // overloads once. They exist on TMP 3.2+ (Unity
        // 2022.2+); on older editors the lookup returns
        // null and we take the FontEngine route.
        // ==========================================
        private static MethodInfo ResolvePathFactory()
        {
            if (!s_pathFactoryResolved)
            {
                s_pathFactory = FindSourceFactory(typeof(string));
                s_pathFactoryResolved = true;
            }
            return s_pathFactory;
        }

        private static MethodInfo ResolveBytesFactory()
        {
            if (!s_bytesFactoryResolved)
            {
                s_bytesFactory = FindSourceFactory(typeof(byte[]));
                s_bytesFactoryResolved = true;
            }
            return s_bytesFactory;
        }

        // Finds a static TMP_FontAsset.CreateFontAsset whose first parameter is
        // `sourceType` and whose tail matches (faceIndex, samplingPointSize,
        // atlasPadding, GlyphRenderMode, atlasWidth, atlasHeight, ...).
        private static MethodInfo FindSourceFactory(Type sourceType)
        {
            foreach (MethodInfo m in typeof(TMP_FontAsset).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (m.Name != "CreateFontAsset") { continue; }
                ParameterInfo[] p = m.GetParameters();
                if (p.Length < 8) { continue; }
                if (p[0].ParameterType != sourceType) { continue; }
                if (p[1].ParameterType != typeof(int)) { continue; }         // faceIndex
                if (p[2].ParameterType != typeof(int)) { continue; }         // samplingPointSize
                if (p[3].ParameterType != typeof(int)) { continue; }         // atlasPadding
                if (p[4].ParameterType != typeof(GlyphRenderMode)) { continue; }
                return m;
            }
            return null;
        }

        private static TMP_FontAsset InvokeSourceFactory(MethodInfo factory, object source, DirectFontSettings settings)
        {
            try
            {
                ParameterInfo[] p = factory.GetParameters();
                object[] args = new object[p.Length];
                args[0] = source;
                args[1] = 0;                              // faceIndex
                args[2] = settings.SamplingPointSize;
                args[3] = settings.AtlasPadding;
                args[4] = settings.RenderMode;
                args[5] = settings.AtlasWidth;
                args[6] = settings.AtlasHeight;
                for (int i = 7; i < p.Length; i++)
                {
                    // AtlasPopulationMode.Dynamic (value 1) and the multi-atlas
                    // flag, or whatever defaults the overload declares.
                    args[i] = p[i].ParameterType == typeof(bool)
                        ? (object)true
                        : (p[i].HasDefaultValue ? p[i].DefaultValue : DefaultFor(p[i].ParameterType));
                }
                return factory.Invoke(null, args) as TMP_FontAsset;
            }
            catch (Exception e)
            {
                DirectTMPLog.Warn($"TMP font factory threw, falling back to FontEngine: {e.Message}");
                return null;
            }
        }

        private static object DefaultFor(Type t)
        {
            if (t == typeof(AtlasPopulationMode)) { return AtlasPopulationMode.Dynamic; }
            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }

        // ==========================================
        // BuildViaFontEngine
        // The low-level fallback for editors whose TMP
        // predates the public path/byte factory. It loads
        // the face through the FontEngine and assembles a
        // dynamic TMP_FontAsset by hand. A handful of the
        // atlas fields only have internal setters on older
        // TMP, so those are set reflectively; everything is
        // wrapped so a version mismatch degrades to a clear
        // warning instead of an exception.
        // ==========================================
        private static TMP_FontAsset BuildViaFontEngine(Func<FontEngineError> loadFace, DirectFontSettings settings, string sourceName)
        {
            try
            {
                EnsureEngine();
                if (loadFace() != FontEngineError.Success)
                {
                    DirectTMPLog.Warn($"FontEngine could not read a face from '{sourceName}'.");
                    return null;
                }

                FaceInfo faceInfo = FontEngine.GetFaceInfo();

                TMP_FontAsset asset = ScriptableObject.CreateInstance<TMP_FontAsset>();

                // faceInfo and atlasPopulationMode have stable public setters on
                // every supported TMP; the atlas fields below only have internal
                // setters on older TMP, so those go through TrySet on the backing
                // field. That keeps this method compiling from TMP 3.0 upward.
                asset.faceInfo = faceInfo;
                asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

                var atlas = new Texture2D(settings.AtlasWidth, settings.AtlasHeight, TextureFormat.Alpha8, false);

                TrySet(asset, "m_AtlasRenderMode", settings.RenderMode);
                TrySet(asset, "m_AtlasTextures", new[] { atlas });
                TrySet(asset, "m_AtlasWidth", settings.AtlasWidth);
                TrySet(asset, "m_AtlasHeight", settings.AtlasHeight);
                TrySet(asset, "m_AtlasPadding", settings.AtlasPadding);
                TrySet(asset, "m_AtlasTextureCount", 1);
                TrySet(asset, "m_FreeGlyphRects", new List<GlyphRect> { new GlyphRect(0, 0, settings.AtlasWidth - 1, settings.AtlasHeight - 1) });
                TrySet(asset, "m_UsedGlyphRects", new List<GlyphRect>());

                Shader shader = Shader.Find("TextMeshPro/Distance Field");
                if (shader != null)
                {
                    var material = new Material(shader);
                    material.SetTexture(ShaderUtilities.ID_MainTex, atlas);
                    material.SetFloat(ShaderUtilities.ID_TextureWidth, settings.AtlasWidth);
                    material.SetFloat(ShaderUtilities.ID_TextureHeight, settings.AtlasHeight);
                    material.SetFloat(ShaderUtilities.ID_GradientScale, settings.AtlasPadding + 1);
                    TrySet(asset, "m_Material", material);
                }

                FinalizeAsset(asset, sourceName);
                return asset;
            }
            catch (Exception e)
            {
                DirectTMPLog.Warn(
                    "Runtime font building from a raw file/buffer needs Unity 2022.2+ (TMP 3.2+) " +
                    $"on this editor. Reference the .ttf as a Font asset instead, or upgrade. ({e.Message})");
                return null;
            }
        }

        private static void EnsureEngine()
        {
            if (s_engineInitialized) { return; }
            FontEngine.InitializeFontEngine();
            s_engineInitialized = true;
        }

        private static string SpoolToCache(byte[] bytes)
        {
            try
            {
                string dir = System.IO.Path.Combine(Application.persistentDataPath, DirectTMPConstants.RuntimeCacheFolderName);
                System.IO.Directory.CreateDirectory(dir);
                // Content-addressed name so identical bytes reuse one spool file.
                string file = System.IO.Path.Combine(dir, HashBytes(bytes) + ".ttf");
                if (!System.IO.File.Exists(file)) { System.IO.File.WriteAllBytes(file, bytes); }
                return file;
            }
            catch (Exception e)
            {
                DirectTMPLog.Warn($"Could not spool font bytes to the cache folder: {e.Message}");
                return null;
            }
        }

        internal static string HashBytes(byte[] bytes)
        {
            // FNV-1a 64-bit - tiny, allocation-free, and more than enough to
            // tell two font buffers apart for cache-file naming.
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= prime;
            }
            return hash.ToString("x16");
        }

        private static void TrySet(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            field?.SetValue(target, value);
        }
    }
}
