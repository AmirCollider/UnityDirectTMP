// ==========================================
// DirectFont
// The one small component you drop next to a
// TextMeshProUGUI / TextMeshPro. Point it at a font
// *file* - a plain .ttf or .otf - and it hands that
// TMP component a dynamic font asset built straight
// from the file, so every glyph the font contains is
// available on demand. No Font Asset Creator, no
// character ranges, no tofu.
//
// It runs in edit mode too (ExecuteAlways), so the
// Scene view previews live while you design, and it
// exposes a handful of runtime setters for loading
// fonts from disk, from memory, or from the player's
// own system fonts.
// ==========================================
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// Where a <see cref="DirectFont"/> is currently getting its glyphs from.
    /// The serialized <see cref="Font"/> is the default; the runtime setters
    /// switch to one of the other sources.
    /// </summary>
    public enum DirectFontSourceKind
    {
        FontAsset,
        FilePath,
        Bytes,
        SystemFont
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Unity DirectTMP/Direct Font")]
    [HelpURL(DirectTMPConstants.GithubUrl)]
    public sealed class DirectFont : MonoBehaviour
    {
        // ==========================================
        // Serialized configuration
        // ==========================================
        [Tooltip("The font file itself - a .ttf or .otf imported into your project. This is the default source.")]
        [SerializeField] private Font fontFile;

        // ==========================================
        // More fonts.
        //
        // One font file rarely covers a real screen. A
        // line like "پروژه‌ی 敵スポーナー 🎮" needs a
        // Persian face, a Japanese face and an emoji
        // face, and no single .ttf has all three - Noto
        // ships them as separate files on purpose.
        //
        // Ordered, and the order is the answer: for every
        // character, the first font in the list that
        // actually has it supplies that glyph. Put the
        // font whose design you want for the bulk of the
        // text first.
        //
        // The reusable chain asset below does the same
        // thing for fonts you want to line up once and
        // use on twenty labels. These two are additive:
        // this list is tried first, then the chain.
        // ==========================================
        [Tooltip("More fonts, tried in order after the one above. For every character the first font in the list that actually has it supplies that glyph — so one label can mix Persian, Japanese and emoji from three different files.")]
        [SerializeField] private List<Font> moreFonts = new List<Font>();

        [Tooltip("Optional ordered fallback chain asset, tried after the fonts above. Use it to line fonts up once and reuse them on many labels.")]
        [SerializeField] private DirectFontFallbackChain fallbackChain;

        [Tooltip("Keep this component's material look (outline, underlay, gradient…) when the font is (re)built, instead of resetting to the font's plain material.")]
        [SerializeField] private bool preserveMaterial = true;

        [Tooltip("Build and apply the font automatically whenever this component is enabled.")]
        [SerializeField] private bool applyOnEnable = true;

        [Tooltip("Also make Arabic-script text read: letters joined, right-to-left words in reading order. Adds a Direct Text component, which is where the setting for it lives.")]
        [SerializeField] private bool shapeText = true;

        [Tooltip("Use custom rasterization settings for this label instead of the package defaults.")]
        [SerializeField] private bool overrideSettings = false;

        [SerializeField] private DirectFontSettings settings = DirectFontSettings.Default;

        // ==========================================
        // Runtime-only source overrides (not serialized)
        // ==========================================
        [System.NonSerialized] private DirectFontSourceKind sourceKind = DirectFontSourceKind.FontAsset;
        [System.NonSerialized] private string runtimePath;
        [System.NonSerialized] private byte[] runtimeBytes;
        [System.NonSerialized] private string runtimeSystemFont;
        [System.NonSerialized] private TMP_FontAsset builtAsset;

        private TMP_Text cachedText;

        // ==========================================
        // Public read-only state
        // ==========================================
        /// <summary>The TMP component this DirectFont drives (found on the same GameObject).</summary>
        public TMP_Text Text => cachedText != null ? cachedText : (cachedText = GetComponent<TMP_Text>());

        /// <summary>The dynamic font asset currently applied, or null if none has been built yet.</summary>
        public TMP_FontAsset FontAsset => builtAsset;

        /// <summary>Which source the next build will use.</summary>
        public DirectFontSourceKind Source => sourceKind;

        /// <summary>The rasterization settings this label will use (custom or package default).</summary>
        public DirectFontSettings ResolvedSettings => overrideSettings ? settings.Clamped() : DirectFontSettings.Default;

        // ==========================================
        // Unity lifecycle
        // ==========================================
        private void OnEnable()
        {
            if (applyOnEnable) { Apply(); }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Value changed in the Inspector: rebuild, but never from inside
            // OnValidate's serialization callback - defer a tick so asset
            // creation is legal.
            if (!isActiveAndEnabled) { return; }
            UnityEditor.EditorApplication.delayCall += DeferredValidateApply;
        }

        private void DeferredValidateApply()
        {
            if (this == null) { return; }
            if (isActiveAndEnabled) { Apply(); }
        }
#endif

        // ==========================================
        // Public runtime API
        // ==========================================

        /// <summary>Use an imported Font asset (or any Unity <see cref="Font"/>) and rebuild.</summary>
        public void SetFontFile(Font font)
        {
            fontFile = font;
            sourceKind = DirectFontSourceKind.FontAsset;
            Apply();
        }

        /// <summary>
        /// Load a font from a file path and rebuild. The path may be absolute,
        /// or relative to StreamingAssets / persistentDataPath.
        /// </summary>
        public void SetFontFile(string path)
        {
            runtimePath = path;
            sourceKind = DirectFontSourceKind.FilePath;
            Apply();
        }

        /// <summary>Load a font from an in-memory buffer (e.g. a download) and rebuild.</summary>
        public void SetFontBytes(byte[] fontBytes)
        {
            runtimeBytes = fontBytes;
            sourceKind = DirectFontSourceKind.Bytes;
            Apply();
        }

        /// <summary>Use one of the operating system's installed fonts, by family name, and rebuild.</summary>
        public void SetSystemFont(string familyName)
        {
            runtimeSystemFont = familyName;
            sourceKind = DirectFontSourceKind.SystemFont;
            Apply();
        }

        /// <summary>Swap the fallback chain and rebuild.</summary>
        public void SetFallbackChain(DirectFontFallbackChain chain)
        {
            fallbackChain = chain;
            Apply();
        }

        /// <summary>Rebuild and re-apply the font asset from the current source.</summary>
        public void Refresh() => Apply();

        // ==========================================
        // Apply
        // Build the font asset for the current source and
        // hand it to the TMP component, wiring up the
        // fallback chain and preserving the material look
        // if asked.
        // ==========================================
        private void Apply()
        {
            TMP_Text tmp = Text;
            if (tmp == null)
            {
                DirectTMPLog.Warn($"DirectFont needs a TextMeshPro component on the same GameObject.", this);
                return;
            }

            // Glyphs are half the answer. A font file with Persian in it stops
            // the boxes; it does not join the letters up or put the words in
            // reading order, because nothing in TextMeshPro does - so the
            // label that was □□□ becomes a row of disconnected letters running
            // backwards, which is a different kind of wrong, not a fixed one.
            // Direct Text is the other half, and this is why a user who did
            // the obvious thing gets it without having to know that.
            if (shapeText) { DirectText.EnsureOn(gameObject); }

            TMP_FontAsset asset = BuildCurrent();
            if (asset == null) { return; }

            builtAsset = asset;

            Material previous = tmp.fontSharedMaterial;
            tmp.font = asset; // TMP switches fontSharedMaterial to the asset's material here.

            ApplyFallbacks(asset);
            if (preserveMaterial && previous != null) { PreserveMaterial(previous, asset.material); }

            tmp.havePropertiesChanged = true;
            tmp.SetAllDirty();
        }

        // ==========================================
        // ApplyFallbacks
        //
        // The inline list first, then the shared chain
        // asset. Both feed one ordered table, because
        // TextMeshPro searches exactly one.
        //
        // An empty result with nothing configured leaves
        // the table alone rather than clearing it: a
        // label whose fallbacks somebody set by hand, or
        // in a prefab, should not lose them for having a
        // DirectFont on it.
        // ==========================================
        private void ApplyFallbacks(TMP_FontAsset primary)
        {
            DirectFontSettings resolved = ResolvedSettings;
            var chain = new List<TMP_FontAsset>();

            for (int i = 0; i < moreFonts.Count; i++)
            {
                if (moreFonts[i] == null) { continue; }

                TMP_FontAsset built = DirectFontLoader.FromFont(moreFonts[i], resolved);
                if (built != null) { chain.Add(built); }
            }

            if (fallbackChain != null) { chain.AddRange(fallbackChain.Build(resolved)); }

            if (chain.Count == 0 && fallbackChain == null) { return; }

            DirectFontFallback.Apply(primary, chain);
        }

        /// <summary>
        /// Replaces the inline list of extra fonts and rebuilds. Order is the
        /// search order: the first font that has a character supplies it.
        /// </summary>
        public void SetMoreFonts(IEnumerable<Font> fonts)
        {
            moreFonts.Clear();
            if (fonts != null) { moreFonts.AddRange(fonts); }
            Apply();
        }

        private TMP_FontAsset BuildCurrent()
        {
            DirectFontSettings s = ResolvedSettings;
            switch (sourceKind)
            {
                case DirectFontSourceKind.FilePath:
                    return DirectFontLoader.FromFile(runtimePath, s);
                case DirectFontSourceKind.Bytes:
                    return DirectFontLoader.FromBytes(runtimeBytes, s, gameObject.name);
                case DirectFontSourceKind.SystemFont:
                    return DirectFontLoader.FromSystemFont(runtimeSystemFont, s);
                case DirectFontSourceKind.FontAsset:
                default:
                    if (fontFile == null) { return null; }
                    return DirectFontLoader.FromFont(fontFile, s);
            }
        }

        // ==========================================
        // PreserveMaterial
        // Copy the outline / underlay / gradient / softness
        // properties the user set from the old material onto
        // the freshly-built one, then restore the new atlas
        // texture and its dimensions (which must NOT be
        // carried over from the old material).
        // ==========================================
        private static void PreserveMaterial(Material from, Material to)
        {
            if (from == null || to == null || from == to) { return; }
            if (from.shader != to.shader) { return; } // different shader family: leave the new look alone.

            // Snapshot the atlas-specific properties of the new material.
            //
            // The scale ratios belong on this list and were missing from it,
            // which is a subtler bug than it looks. TextMeshPro derives them
            // from the atlas's gradient scale, padding and sampling size, and
            // the shader multiplies face dilation, weight and outline width by
            // them. Copying another font's ratios over a font that was
            // rasterized differently renders every stroke at the wrong weight
            // - and the strokes that vanish first are the thinnest ones in the
            // label, which in Persian are exactly the strokes that JOIN one
            // letter to the next. The word does not come apart; its joins are
            // drawn too faint to see, which looks identical.
            Texture atlas = to.HasProperty(ShaderUtilities.ID_MainTex) ? to.GetTexture(ShaderUtilities.ID_MainTex) : null;
            float texW = to.HasProperty(ShaderUtilities.ID_TextureWidth) ? to.GetFloat(ShaderUtilities.ID_TextureWidth) : 0f;
            float texH = to.HasProperty(ShaderUtilities.ID_TextureHeight) ? to.GetFloat(ShaderUtilities.ID_TextureHeight) : 0f;
            float gradient = to.HasProperty(ShaderUtilities.ID_GradientScale) ? to.GetFloat(ShaderUtilities.ID_GradientScale) : 0f;
            float ratioA = to.HasProperty(ShaderUtilities.ID_ScaleRatio_A) ? to.GetFloat(ShaderUtilities.ID_ScaleRatio_A) : 0f;
            float ratioB = to.HasProperty(ShaderUtilities.ID_ScaleRatio_B) ? to.GetFloat(ShaderUtilities.ID_ScaleRatio_B) : 0f;
            float ratioC = to.HasProperty(ShaderUtilities.ID_ScaleRatio_C) ? to.GetFloat(ShaderUtilities.ID_ScaleRatio_C) : 0f;

            to.CopyPropertiesFromMaterial(from);

            // Put the new atlas back - everything else (outline, glow, underlay…) is preserved.
            if (atlas != null && to.HasProperty(ShaderUtilities.ID_MainTex)) { to.SetTexture(ShaderUtilities.ID_MainTex, atlas); }
            if (to.HasProperty(ShaderUtilities.ID_TextureWidth)) { to.SetFloat(ShaderUtilities.ID_TextureWidth, texW); }
            if (to.HasProperty(ShaderUtilities.ID_TextureHeight)) { to.SetFloat(ShaderUtilities.ID_TextureHeight, texH); }
            if (to.HasProperty(ShaderUtilities.ID_GradientScale)) { to.SetFloat(ShaderUtilities.ID_GradientScale, gradient); }
            if (to.HasProperty(ShaderUtilities.ID_ScaleRatio_A)) { to.SetFloat(ShaderUtilities.ID_ScaleRatio_A, ratioA); }
            if (to.HasProperty(ShaderUtilities.ID_ScaleRatio_B)) { to.SetFloat(ShaderUtilities.ID_ScaleRatio_B, ratioB); }
            if (to.HasProperty(ShaderUtilities.ID_ScaleRatio_C)) { to.SetFloat(ShaderUtilities.ID_ScaleRatio_C, ratioC); }
        }

        // ==========================================
        // Editor-facing accessors (used by the custom
        // Inspector so it can read/write serialized state
        // without making the fields public).
        // ==========================================
        internal Font EditorFontFile { get => fontFile; set => fontFile = value; }
        internal bool EditorOverrideSettings { get => overrideSettings; set => overrideSettings = value; }
        internal DirectFontSettings EditorSettings { get => settings; set => settings = value; }
    }
}
