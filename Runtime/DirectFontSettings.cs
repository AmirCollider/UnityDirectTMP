// ==========================================
// DirectFontSettings
// The knobs that decide how a font file is turned
// into a dynamic SDF atlas: sampling point size,
// atlas dimensions, padding and render mode. Two
// DirectFonts that point at the same file but ask
// for different settings get two different atlases;
// two that ask for the same settings share one.
// That sharing is why this type is a value type
// with real equality - it doubles as half of the
// font cache key.
// ==========================================
using System;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace UnityDirectTMP
{
    /// <summary>
    /// Immutable-by-convention description of how to rasterize a font file
    /// into a TextMeshPro dynamic font asset. Implements value equality so
    /// it can be used as (part of) the key in <see cref="DirectFontCache"/>.
    /// </summary>
    [Serializable]
    public struct DirectFontSettings : IEquatable<DirectFontSettings>
    {
        [Tooltip("Point size the FontEngine rasterizes glyphs at. Bigger = crisper when scaled up, but fills the atlas faster.")]
        [SerializeField] private int samplingPointSize;

        [Tooltip("Spacing (in px) baked around each glyph so neighbouring SDF spreads never bleed into one another.")]
        [SerializeField] private int atlasPadding;

        [Tooltip("Width of a single atlas texture. Glyphs spill into a new texture once one fills up.")]
        [SerializeField] private int atlasWidth;

        [Tooltip("Height of a single atlas texture.")]
        [SerializeField] private int atlasHeight;

        [Tooltip("How glyphs are rasterized. SDFAA is the crisp, scalable default TextMeshPro uses.")]
        [SerializeField] private GlyphRenderMode renderMode;

        public int SamplingPointSize => samplingPointSize;
        public int AtlasPadding => atlasPadding;
        public int AtlasWidth => atlasWidth;
        public int AtlasHeight => atlasHeight;
        public GlyphRenderMode RenderMode => renderMode;

        public DirectFontSettings(int samplingPointSize, int atlasPadding, int atlasWidth, int atlasHeight, GlyphRenderMode renderMode)
        {
            this.samplingPointSize = samplingPointSize;
            this.atlasPadding = atlasPadding;
            this.atlasWidth = atlasWidth;
            this.atlasHeight = atlasHeight;
            this.renderMode = renderMode;
        }

        /// <summary>The package defaults, straight from <see cref="DirectTMPConstants"/>.</summary>
        public static DirectFontSettings Default => new DirectFontSettings(
            DirectTMPConstants.DefaultSamplingPointSize,
            DirectTMPConstants.DefaultAtlasPadding,
            DirectTMPConstants.DefaultAtlasWidth,
            DirectTMPConstants.DefaultAtlasHeight,
            DirectTMPConstants.DefaultRenderMode);

        /// <summary>
        /// Returns a copy with every value forced back inside the safe range
        /// declared in <see cref="DirectTMPConstants"/>. Called before the
        /// settings ever reach the FontEngine, so a hand-edited or corrupted
        /// value can never ask for a 0 px or a 16 384 px atlas.
        /// </summary>
        public DirectFontSettings Clamped()
        {
            int size = Mathf.Clamp(samplingPointSize, DirectTMPConstants.MinSamplingPointSize, DirectTMPConstants.MaxSamplingPointSize);
            int width = Mathf.Clamp(Mathf.ClosestPowerOfTwo(atlasWidth), DirectTMPConstants.MinAtlasDimension, DirectTMPConstants.MaxAtlasDimension);
            int height = Mathf.Clamp(Mathf.ClosestPowerOfTwo(atlasHeight), DirectTMPConstants.MinAtlasDimension, DirectTMPConstants.MaxAtlasDimension);
            int padding = Mathf.Clamp(atlasPadding, 0, 32);
            return new DirectFontSettings(size, padding, width, height, renderMode);
        }

        public bool Equals(DirectFontSettings other)
        {
            return samplingPointSize == other.samplingPointSize
                && atlasPadding == other.atlasPadding
                && atlasWidth == other.atlasWidth
                && atlasHeight == other.atlasHeight
                && renderMode == other.renderMode;
        }

        public override bool Equals(object obj) => obj is DirectFontSettings other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + samplingPointSize;
                hash = hash * 31 + atlasPadding;
                hash = hash * 31 + atlasWidth;
                hash = hash * 31 + atlasHeight;
                hash = hash * 31 + (int)renderMode;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{samplingPointSize}px, {atlasWidth}x{atlasHeight}, pad {atlasPadding}, {renderMode}";
        }

        public static bool operator ==(DirectFontSettings a, DirectFontSettings b) => a.Equals(b);
        public static bool operator !=(DirectFontSettings a, DirectFontSettings b) => !a.Equals(b);
    }
}
