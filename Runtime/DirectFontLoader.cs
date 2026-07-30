// ==========================================
// DirectFontLoader
// The friendly middle layer between "here is a
// font, somehow" and "here is a cached, ready-to-use
// TMP_FontAsset". It knows how to find a font file
// (absolute path, StreamingAssets-relative,
// persistentDataPath-relative), how to pull one from
// the operating system, and it always routes through
// DirectFontCache so identical requests share one
// atlas. DirectFont and the public DirectTMP facade
// both sit on top of this.
// ==========================================
using System.IO;
using TMPro;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// Resolves any supported font source to a cached dynamic
    /// <see cref="TMP_FontAsset"/>. Every public method is safe to call with
    /// bad input - it logs and returns null rather than throwing.
    /// </summary>
    internal static class DirectFontLoader
    {
        // ==========================================
        // FromFont - an imported Font asset or an OS font.
        // ==========================================
        public static TMP_FontAsset FromFont(Font font, DirectFontSettings settings)
        {
            if (font == null) { return null; }
            DirectFontKey key = DirectFontKey.ForFont(font, settings);
            return DirectFontCache.GetOrCreate(key, () => DirectFontFactory.CreateFromFont(font, settings));
        }

        // ==========================================
        // FromFile - a path to a .ttf/.otf. The path may
        // be absolute or given relative to StreamingAssets
        // / persistentDataPath; ResolveFilePath sorts that
        // out. The *resolved* absolute path is what keys
        // the cache, so the same file requested two ways
        // still shares one atlas.
        // ==========================================
        public static TMP_FontAsset FromFile(string path, DirectFontSettings settings)
        {
            string resolved = ResolveFilePath(path);
            if (resolved == null)
            {
                DirectTMPLog.Warn($"Could not locate font file: '{path}'.");
                return null;
            }

            DirectFontKey key = DirectFontKey.ForPath(resolved, settings);
            return DirectFontCache.GetOrCreate(key, () => DirectFontFactory.CreateFromPath(resolved, settings));
        }

        // ==========================================
        // FromBytes - a font that only exists in memory.
        // ==========================================
        public static TMP_FontAsset FromBytes(byte[] bytes, DirectFontSettings settings, string displayName = null)
        {
            if (bytes == null || bytes.Length == 0) { return null; }
            DirectFontKey key = DirectFontKey.ForBytes(bytes, settings);
            return DirectFontCache.GetOrCreate(key, () => DirectFontFactory.CreateFromBytes(bytes, settings, displayName));
        }

        // ==========================================
        // FromSystemFont - one of the player's own
        // installed fonts, by family name. Unity hands us
        // a dynamic Font for it; from there it's the normal
        // Font route.
        // ==========================================
        public static TMP_FontAsset FromSystemFont(string familyName, DirectFontSettings settings)
        {
            if (string.IsNullOrEmpty(familyName)) { return null; }
            Font osFont = Font.CreateDynamicFontFromOSFont(familyName, settings.SamplingPointSize);
            if (osFont == null)
            {
                DirectTMPLog.Warn($"No installed system font named '{familyName}'.");
                return null;
            }
            return FromFont(osFont, settings);
        }

        // ==========================================
        // Preload
        // Rasterize a set of characters up front so the
        // first frame that shows them doesn't pay the
        // per-glyph cost all at once. Returns the
        // characters the font could NOT supply.
        // ==========================================
        public static string Preload(TMP_FontAsset fontAsset, string characters)
        {
            if (fontAsset == null || string.IsNullOrEmpty(characters)) { return characters; }
            fontAsset.TryAddCharacters(characters, out string missing);
            return missing;
        }

        // ==========================================
        // ResolveFilePath
        // Turn a user-supplied path into an absolute path
        // to a file that exists, or null. Search order:
        //   1. the path as given, if it's already absolute
        //   2. relative to StreamingAssets
        //   3. relative to persistentDataPath
        // Pure string work is factored into helpers below
        // so it can be unit-tested without a file system.
        // ==========================================
        public static string ResolveFilePath(string path)
        {
            if (string.IsNullOrEmpty(path)) { return null; }
            string normalized = path.Replace('\\', '/');

            if (Path.IsPathRooted(normalized) && File.Exists(normalized)) { return normalized; }

            string inStreaming = CombineStreamingAssets(Application.streamingAssetsPath, normalized);
            if (File.Exists(inStreaming)) { return inStreaming; }

            string inPersistent = CombineUnder(Application.persistentDataPath, normalized);
            if (File.Exists(inPersistent)) { return inPersistent; }

            return null;
        }

        /// <summary>Joins a relative font path onto StreamingAssets, forward-slashed.</summary>
        internal static string CombineStreamingAssets(string streamingAssetsPath, string relative)
            => CombineUnder(streamingAssetsPath, relative);

        internal static string CombineUnder(string root, string relative)
        {
            if (string.IsNullOrEmpty(relative)) { return root; }
            string trimmed = relative.Replace('\\', '/').TrimStart('/');
            return (root.Replace('\\', '/').TrimEnd('/') + "/" + trimmed);
        }
    }
}
