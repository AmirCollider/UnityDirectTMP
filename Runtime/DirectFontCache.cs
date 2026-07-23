// ==========================================
// DirectFontCache
// One dynamic TMP_FontAsset per unique
// (source + settings). This is what makes a screen
// of 200 labels that all share one font build
// exactly one atlas instead of 200. The key is a
// cheap string identity for the source (a Font's
// instance id, a file path, or a byte hash) paired
// with the rasterization settings; ask twice with
// the same key and you get the very same asset back.
// ==========================================
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// Identity of a cached font: what the glyphs come from, and how they are
    /// rasterized. Two keys are equal when both halves match.
    /// </summary>
    internal readonly struct DirectFontKey : IEquatable<DirectFontKey>
    {
        public readonly string SourceId;
        public readonly DirectFontSettings Settings;

        public DirectFontKey(string sourceId, DirectFontSettings settings)
        {
            SourceId = sourceId ?? string.Empty;
            Settings = settings;
        }

        public static DirectFontKey ForFont(Font font, DirectFontSettings settings)
            => new DirectFontKey("font:" + (font == null ? "null" : font.GetInstanceID().ToString()), settings);

        public static DirectFontKey ForPath(string absolutePath, DirectFontSettings settings)
            => new DirectFontKey("path:" + NormalizePath(absolutePath), settings);

        public static DirectFontKey ForBytes(byte[] bytes, DirectFontSettings settings)
            => new DirectFontKey("bytes:" + (bytes == null ? "null" : DirectFontFactory.HashBytes(bytes)), settings);

        internal static string NormalizePath(string path)
            => string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');

        public bool Equals(DirectFontKey other)
            => string.Equals(SourceId, other.SourceId, StringComparison.Ordinal) && Settings.Equals(other.Settings);

        public override bool Equals(object obj) => obj is DirectFontKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked { return (SourceId.GetHashCode() * 397) ^ Settings.GetHashCode(); }
        }

        public override string ToString() => $"{SourceId} [{Settings}]";
    }

    /// <summary>
    /// Process-wide cache of dynamic font assets. Static because the whole
    /// point is to share one atlas across every DirectFont, scene and window
    /// that asks for the same font.
    /// </summary>
    internal static class DirectFontCache
    {
        private static readonly Dictionary<DirectFontKey, TMP_FontAsset> s_assets = new Dictionary<DirectFontKey, TMP_FontAsset>();

        /// <summary>How many distinct font assets are currently cached.</summary>
        public static int Count => s_assets.Count;

        /// <summary>
        /// Returns the cached asset for <paramref name="key"/>, building it with
        /// <paramref name="builder"/> on first request. A builder that returns
        /// null is not cached, so a transient failure can be retried later.
        /// </summary>
        public static TMP_FontAsset GetOrCreate(DirectFontKey key, Func<TMP_FontAsset> builder)
        {
            if (s_assets.TryGetValue(key, out TMP_FontAsset cached) && cached != null)
            {
                return cached;
            }

            // A destroyed-but-still-mapped entry (e.g. domain reload) is stale.
            if (cached == null) { s_assets.Remove(key); }

            TMP_FontAsset built = builder != null ? builder() : null;
            if (built != null) { s_assets[key] = built; }
            return built;
        }

        public static bool TryGet(DirectFontKey key, out TMP_FontAsset asset)
        {
            if (s_assets.TryGetValue(key, out asset) && asset != null) { return true; }
            asset = null;
            return false;
        }

        /// <summary>Drops and destroys a single cached asset.</summary>
        public static void Remove(DirectFontKey key)
        {
            if (s_assets.TryGetValue(key, out TMP_FontAsset asset))
            {
                DestroyAsset(asset);
                s_assets.Remove(key);
            }
        }

        /// <summary>
        /// Clears every cached atlas. Use it on a scene change to reclaim the
        /// memory held by glyphs you no longer need; the next label that wants
        /// a font simply rebuilds it.
        /// </summary>
        public static int Clear()
        {
            int n = s_assets.Count;
            foreach (KeyValuePair<DirectFontKey, TMP_FontAsset> kvp in s_assets)
            {
                DestroyAsset(kvp.Value);
            }
            s_assets.Clear();
            return n;
        }

        private static void DestroyAsset(TMP_FontAsset asset)
        {
            if (asset == null) { return; }

            // A generated asset owns its material and atlas textures - tear
            // those down too, or clearing the cache leaks them.
            if (asset.material != null) { DestroyObject(asset.material); }
            if (asset.atlasTextures != null)
            {
                foreach (Texture2D tex in asset.atlasTextures)
                {
                    if (tex != null) { DestroyObject(tex); }
                }
            }
            DestroyObject(asset);
        }

        private static void DestroyObject(UnityEngine.Object obj)
        {
            if (obj == null) { return; }
            if (Application.isPlaying) { UnityEngine.Object.Destroy(obj); }
            else { UnityEngine.Object.DestroyImmediate(obj); }
        }
    }
}
