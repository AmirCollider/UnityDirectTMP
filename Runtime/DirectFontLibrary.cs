// ==========================================
// DirectFontLibrary
// A font file in, a TextMeshPro font asset out.
//
// The asset is DYNAMIC: it starts empty and rasterizes
// each glyph the first time something asks to draw it.
// That is the whole trick behind not needing the Font
// Asset Creator - there is no character set to choose in
// advance, because characters are added as they turn up.
//
// Every asset made here is remembered along with the
// BYTES it was made from, because DirectFontJoiner needs
// to read the font's own joining rules and a generated
// asset has no path to go back to.
// ==========================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace UnityDirectTMP
{
    /// <summary>
    /// Where the bytes of a generated font asset came from, so that its own
    /// OpenType tables can be read back later.
    /// </summary>
    public static class DirectFontSource
    {
        private static readonly Dictionary<int, Func<byte[]>> s_sources
            = new Dictionary<int, Func<byte[]>>();

        /// <summary>Remembers how to get the file behind a font asset.</summary>
        public static void Remember(TMP_FontAsset asset, Func<byte[]> bytes)
        {
            if (asset != null && bytes != null) { s_sources[asset.GetInstanceID()] = bytes; }
        }

        /// <summary>
        /// The font file behind an asset, or null. Falls back to the asset's
        /// own <c>sourceFontFile</c> - through the AssetDatabase in the Editor
        /// and through the baked table in a player build - which is how a font
        /// asset somebody else built still gets joined.
        /// </summary>
        public static byte[] BytesFor(TMP_FontAsset asset)
        {
            if (asset == null) { return null; }

            if (s_sources.TryGetValue(asset.GetInstanceID(), out Func<byte[]> source))
            {
                try
                {
                    byte[] remembered = source();
                    if (remembered != null && remembered.Length > 0) { return remembered; }
                }
                catch { /* fall through */ }
            }

            Font file = null;
            try { file = asset.sourceFontFile; } catch { /* fall through */ }

#if UNITY_EDITOR
            try
            {
                string path = file == null ? null : UnityEditor.AssetDatabase.GetAssetPath(file);
                if (!string.IsNullOrEmpty(path) && File.Exists(path)) { return File.ReadAllBytes(path); }
            }
            catch { /* fall through */ }
#endif

            // The only route left in a player build, where there is no
            // AssetDatabase and no .ttf on disk to read.
            return DirectFontBytes.For(file);
        }

        /// <summary>Forgets everything. Paired with <see cref="DirectFontLibrary.ClearCache"/>.</summary>
        public static void Clear() => s_sources.Clear();
    }

    /// <summary>
    /// Builds dynamic <see cref="TMP_FontAsset"/>s from font files and caches
    /// them, so the same file asked for twice shares one atlas.
    /// </summary>
    public static class DirectFontLibrary
    {
        /// <summary>The size glyphs are rasterized at. Bigger is sharper and heavier.</summary>
        public const int SamplingPointSize = 90;

        /// <summary>Padding between glyphs in the atlas, in pixels.</summary>
        public const int AtlasPadding = 9;

        /// <summary>The atlas starts at this size and grows by adding pages.</summary>
        public const int AtlasSize = 1024;

        private static readonly Dictionary<string, TMP_FontAsset> s_cache
            = new Dictionary<string, TMP_FontAsset>();

        /// <summary>How many font assets are cached.</summary>
        public static int CachedCount => s_cache.Count;

        /// <summary>
        /// Drops every cached font asset. The assets themselves are left for
        /// the garbage collector; anything still pointing at one keeps working.
        /// </summary>
        public static int ClearCache()
        {
            int count = s_cache.Count;
            s_cache.Clear();
            DirectFontSource.Clear();
            DirectFontJoiner.Clear();
            return count;
        }

        // ==========================================
        // From an imported Font asset - the ordinary
        // case, and the only one that works on every
        // TextMeshPro version.
        // ==========================================
        /// <summary>
        /// A dynamic font asset for an imported <see cref="Font"/>, or null
        /// with an explanation in the Console.
        /// </summary>
        public static TMP_FontAsset Load(Font font)
        {
            if (font == null) { return null; }

            string key = "font:" + font.GetInstanceID();
            if (s_cache.TryGetValue(key, out TMP_FontAsset cached) && cached != null) { return cached; }

            TMP_FontAsset asset = null;
            try
            {
                asset = TMP_FontAsset.CreateFontAsset(
                    font, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA,
                    AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DirectTMP] Could not build a font asset from '{font.name}': {e.Message}");
            }

            if (asset == null)
            {
                // Almost always "Include Font Data" being off on the importer,
                // which leaves Unity holding the font's NAME instead of its
                // outlines. Reading the file off disk sidesteps the importer
                // entirely.
                string path = PathOf(font);
                if (!string.IsNullOrEmpty(path)) { return LoadFromFile(path); }

                Debug.LogWarning(
                    $"[DirectTMP] '{font.name}' produced no font asset. Select the font in the "
                    + "Project window and turn ON \"Include Font Data\" in the Inspector.");
                return null;
            }

            asset.name = font.name + " (DirectTMP)";
            RememberImported(asset, font);
            s_cache[key] = asset;
            return asset;
        }

        // ==========================================
        // From a file on disk. Needs TextMeshPro 3.2 or
        // newer, which added a factory that takes a path
        // or bytes; below that, an imported Font is the
        // only way in.
        // ==========================================
        /// <summary>A dynamic font asset for a .ttf/.otf on disk, or null.</summary>
        public static TMP_FontAsset LoadFromFile(string path)
        {
            string resolved = Resolve(path);
            if (resolved == null)
            {
                Debug.LogWarning($"[DirectTMP] No font file at '{path}'.");
                return null;
            }

            string key = "path:" + resolved;
            if (s_cache.TryGetValue(key, out TMP_FontAsset cached) && cached != null) { return cached; }

            byte[] bytes;
            try { bytes = File.ReadAllBytes(resolved); }
            catch (Exception e)
            {
                Debug.LogWarning($"[DirectTMP] Could not read '{resolved}': {e.Message}");
                return null;
            }

            TMP_FontAsset asset = FromBytes(bytes, Path.GetFileNameWithoutExtension(resolved));
            if (asset == null) { return null; }

            DirectFontSource.Remember(asset, () => File.ReadAllBytes(resolved));
            s_cache[key] = asset;
            return asset;
        }

        /// <summary>A dynamic font asset from font bytes already in memory, or null.</summary>
        public static TMP_FontAsset LoadFromBytes(byte[] bytes, string name = null)
        {
            if (bytes == null || bytes.Length == 0) { return null; }

            TMP_FontAsset asset = FromBytes(bytes, name);
            if (asset == null) { return null; }

            byte[] copy = bytes;
            DirectFontSource.Remember(asset, () => copy);
            return asset;
        }

        private static TMP_FontAsset FromBytes(byte[] bytes, string name)
        {
            MethodInfo factory = ByteFactory();
            if (factory == null)
            {
                Debug.LogWarning(
                    "[DirectTMP] This TextMeshPro version cannot build a font asset from a file. "
                    + "Drag the .ttf into your project and assign the imported font instead.");
                return null;
            }

            try
            {
                var asset = factory.Invoke(null, new object[]
                {
                    bytes, 0, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA,
                    AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true
                }) as TMP_FontAsset;

                if (asset != null && !string.IsNullOrEmpty(name)) { asset.name = name + " (DirectTMP)"; }
                return asset;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DirectTMP] Could not build a font asset from bytes: {e.Message}");
                return null;
            }
        }

        private static MethodInfo s_byteFactory;
        private static bool s_byteFactoryResolved;

        // TMP 3.2 added CreateFontAsset(byte[], int, int, int, GlyphRenderMode,
        // int, int, AtlasPopulationMode, bool). Found reflectively so this
        // compiles against 3.0 as well.
        private static MethodInfo ByteFactory()
        {
            if (s_byteFactoryResolved) { return s_byteFactory; }
            s_byteFactoryResolved = true;

            foreach (MethodInfo m in typeof(TMP_FontAsset).GetMethods(
                BindingFlags.Public | BindingFlags.Static))
            {
                if (m.Name != "CreateFontAsset") { continue; }

                ParameterInfo[] p = m.GetParameters();
                if (p.Length != 9) { continue; }
                if (p[0].ParameterType != typeof(byte[])) { continue; }

                s_byteFactory = m;
                break;
            }

            return s_byteFactory;
        }

        private static void RememberFile(TMP_FontAsset asset, string path)
        {
            if (string.IsNullOrEmpty(path)) { return; }
            DirectFontSource.Remember(asset, () => File.Exists(path) ? File.ReadAllBytes(path) : null);
        }

        // ==========================================
        // An imported Font asset has two possible
        // routes to its own file, and which one is
        // open depends on where the game is running.
        //
        //   Editor - AssetDatabase knows where the .ttf
        //            is, so it is read off disk.
        //   Player - there is no AssetDatabase and no
        //            .ttf, so the bytes have to have been
        //            baked into the build beforehand.
        //
        // Both are registered as one source that tries
        // them in that order, because a path that was
        // valid when the asset was built can stop being
        // valid - and falling through to the baked copy
        // is better than falling through to no joining
        // rules at all.
        // ==========================================
        private static void RememberImported(TMP_FontAsset asset, Font font)
        {
            if (asset == null || font == null) { return; }

            string path = PathOf(font);

            DirectFontSource.Remember(asset, () =>
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path)) { return File.ReadAllBytes(path); }

                return DirectFontBytes.For(font);
            });
        }

        private static string PathOf(Font font)
        {
#if UNITY_EDITOR
            try
            {
                string path = UnityEditor.AssetDatabase.GetAssetPath(font);
                return string.IsNullOrEmpty(path) ? null : path;
            }
            catch { return null; }
#else
            return null;
#endif
        }

        /// <summary>
        /// Turns a user-supplied path into one that exists: as given, then
        /// under StreamingAssets, then under persistentDataPath.
        /// </summary>
        public static string Resolve(string path)
        {
            if (string.IsNullOrEmpty(path)) { return null; }

            string normalized = path.Replace('\\', '/');
            if (File.Exists(normalized)) { return normalized; }

            string streaming = Combine(Application.streamingAssetsPath, normalized);
            if (File.Exists(streaming)) { return streaming; }

            string persistent = Combine(Application.persistentDataPath, normalized);
            if (File.Exists(persistent)) { return persistent; }

            return null;
        }

        internal static string Combine(string root, string relative)
        {
            if (string.IsNullOrEmpty(relative)) { return root; }
            return root.Replace('\\', '/').TrimEnd('/') + "/" + relative.Replace('\\', '/').TrimStart('/');
        }
    }
}
