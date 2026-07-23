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

        [Tooltip("Optional ordered fallback chain. The first font in the chain that has a given glyph supplies it.")]
        [SerializeField] private DirectFontFallbackChain fallbackChain;

        [Tooltip("Keep this component's material look (outline, underlay, gradient…) when the font is (re)built, instead of resetting to the font's plain material.")]
        [SerializeField] private bool preserveMaterial = true;

        [Tooltip("Build and apply the font automatically whenever this component is enabled.")]
        [SerializeField] private bool applyOnEnable = true;

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
                Debug.LogWarning($"[{DirectTMPConstants.ToolName}] DirectFont needs a TextMeshPro component on the same GameObject.", this);
                return;
            }

            TMP_FontAsset asset = BuildCurrent();
            if (asset == null) { return; }

            builtAsset = asset;

            Material previous = tmp.fontSharedMaterial;
            tmp.font = asset; // TMP switches fontSharedMaterial to the asset's material here.

            if (fallbackChain != null) { fallbackChain.ApplyTo(asset, ResolvedSettings); }
            if (preserveMaterial && previous != null) { PreserveMaterial(previous, asset.material); }

            tmp.havePropertiesChanged = true;
            tmp.SetAllDirty();
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
            Texture atlas = to.HasProperty(ShaderUtilities.ID_MainTex) ? to.GetTexture(ShaderUtilities.ID_MainTex) : null;
            float texW = to.HasProperty(ShaderUtilities.ID_TextureWidth) ? to.GetFloat(ShaderUtilities.ID_TextureWidth) : 0f;
            float texH = to.HasProperty(ShaderUtilities.ID_TextureHeight) ? to.GetFloat(ShaderUtilities.ID_TextureHeight) : 0f;
            float gradient = to.HasProperty(ShaderUtilities.ID_GradientScale) ? to.GetFloat(ShaderUtilities.ID_GradientScale) : 0f;

            to.CopyPropertiesFromMaterial(from);

            // Put the new atlas back - everything else (outline, glow, underlay…) is preserved.
            if (atlas != null && to.HasProperty(ShaderUtilities.ID_MainTex)) { to.SetTexture(ShaderUtilities.ID_MainTex, atlas); }
            if (to.HasProperty(ShaderUtilities.ID_TextureWidth)) { to.SetFloat(ShaderUtilities.ID_TextureWidth, texW); }
            if (to.HasProperty(ShaderUtilities.ID_TextureHeight)) { to.SetFloat(ShaderUtilities.ID_TextureHeight, texH); }
            if (to.HasProperty(ShaderUtilities.ID_GradientScale)) { to.SetFloat(ShaderUtilities.ID_GradientScale, gradient); }
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
