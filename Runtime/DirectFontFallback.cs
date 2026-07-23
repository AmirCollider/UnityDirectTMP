// ==========================================
// DirectFontFallback
// Fallback chains, ordered by you. Line up a Latin
// UI font, a CJK font and a symbol font; for every
// character the first font in the line that actually
// has the glyph wins. TextMeshPro already walks a
// font asset's fallback table per character - all
// this does is build that table from an ordered list
// of font files and keep it free of duplicates and
// self-references.
//
// The chain itself is a ScriptableObject so you can
// order your fonts once and drop the same asset onto
// every DirectFont (roadmap item, delivered).
// ==========================================
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// Pure, side-effect-free helpers for assembling a TextMeshPro fallback
    /// table. Kept separate from the ScriptableObject so the ordering rules
    /// can be unit-tested on their own.
    /// </summary>
    internal static class DirectFontFallback
    {
        /// <summary>
        /// Returns the input in order, dropping nulls and any font asset that
        /// has already appeared (compared by instance). Order is significant -
        /// it is exactly the per-character search order TMP will use.
        /// </summary>
        public static List<TMP_FontAsset> OrderAndDedupe(IEnumerable<TMP_FontAsset> assets)
        {
            var result = new List<TMP_FontAsset>();
            if (assets == null) { return result; }

            var seen = new HashSet<int>();
            foreach (TMP_FontAsset asset in assets)
            {
                if (asset == null) { continue; }
                if (!seen.Add(asset.GetInstanceID())) { continue; }
                result.Add(asset);
            }
            return result;
        }

        /// <summary>
        /// Installs <paramref name="fallbacks"/> as the fallback table of
        /// <paramref name="primary"/>. The primary is never listed as its own
        /// fallback, and the list is de-duplicated first.
        /// </summary>
        public static void Apply(TMP_FontAsset primary, IEnumerable<TMP_FontAsset> fallbacks)
        {
            if (primary == null) { return; }

            List<TMP_FontAsset> ordered = OrderAndDedupe(fallbacks);
            ordered.RemoveAll(f => f == primary);

            primary.fallbackFontAssetTable = ordered;
        }
    }

    /// <summary>
    /// A reusable, ordered list of font files to fall back through. Create one
    /// via <c>Assets → Create → Unity DirectTMP → Fallback Chain</c>, order the
    /// fonts, then reference it from any <see cref="DirectFont"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "DirectFontFallbackChain", menuName = "Unity DirectTMP/Fallback Chain", order = 200)]
    public sealed class DirectFontFallbackChain : ScriptableObject
    {
        [Tooltip("Fonts are tried top-to-bottom. The first one that actually contains a given character supplies that glyph.")]
        [SerializeField] private List<Font> fonts = new List<Font>();

        /// <summary>The fonts in this chain, in priority order.</summary>
        public IReadOnlyList<Font> Fonts => fonts;

        /// <summary>
        /// Builds a dynamic font asset for every font in the chain, in order,
        /// using the given rasterization settings. Missing entries are skipped.
        /// </summary>
        public List<TMP_FontAsset> Build(DirectFontSettings settings)
        {
            var built = new List<TMP_FontAsset>();
            foreach (Font font in fonts)
            {
                if (font == null) { continue; }
                TMP_FontAsset asset = DirectFontLoader.FromFont(font, settings);
                if (asset != null) { built.Add(asset); }
            }
            return DirectFontFallback.OrderAndDedupe(built);
        }

        /// <summary>
        /// Convenience: build this chain and install it as the fallback table
        /// of <paramref name="primary"/>.
        /// </summary>
        public void ApplyTo(TMP_FontAsset primary, DirectFontSettings settings)
        {
            DirectFontFallback.Apply(primary, Build(settings));
        }
    }
}
