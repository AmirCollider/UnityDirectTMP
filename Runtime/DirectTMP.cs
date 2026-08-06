// ==========================================
// DirectTMP
// The small public API. Everything you can do with
// the DirectFont component, you can also do in one
// line from code: point the whole UI at a font,
// preload a wall of glyphs before a text-heavy scene
// appears, or just build a dynamic font asset and
// keep it yourself.
//
//   using UnityDirectTMP;
//
//   DirectTMP.SetGlobalFont(fontBytes);
//   DirectTMP.Preload(fontBytes, "こんにちは سلام Hello");
// ==========================================
using TMPro;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// Static entry point for Unity DirectTMP. A thin, friendly wrapper over
    /// the loader and cache for people who would rather call a method than add
    /// a component.
    /// </summary>
    public static class DirectTMP
    {
        /// <summary>
        /// The rasterization settings used by these static helpers when you
        /// don't pass your own. Defaults to the package defaults; assign it
        /// once at startup to change the whole app's baseline.
        /// </summary>
        public static DirectFontSettings DefaultSettings { get; set; } = DirectFontSettings.Default;

        // ==========================================
        // Load - build (or fetch from cache) a dynamic
        // font asset from any source, and hand it back.
        // ==========================================
        public static TMP_FontAsset Load(Font font) => DirectFontLoader.FromFont(font, DefaultSettings);
        public static TMP_FontAsset Load(Font font, DirectFontSettings settings) => DirectFontLoader.FromFont(font, settings);

        public static TMP_FontAsset LoadFromFile(string path) => DirectFontLoader.FromFile(path, DefaultSettings);
        public static TMP_FontAsset LoadFromFile(string path, DirectFontSettings settings) => DirectFontLoader.FromFile(path, settings);

        public static TMP_FontAsset LoadFromBytes(byte[] fontBytes, string name = null) => DirectFontLoader.FromBytes(fontBytes, DefaultSettings, name);
        public static TMP_FontAsset LoadFromBytes(byte[] fontBytes, DirectFontSettings settings, string name = null) => DirectFontLoader.FromBytes(fontBytes, settings, name);

        public static TMP_FontAsset LoadSystemFont(string familyName) => DirectFontLoader.FromSystemFont(familyName, DefaultSettings);

        // ==========================================
        // SetGlobalFont - point every TextMeshPro label
        // currently loaded in the scene(s) at one font,
        // in a single call. Made for "the user picked a
        // UI font" and localization patches.
        // ==========================================
        public static int SetGlobalFont(Font font) => ApplyToAll(Load(font));
        public static int SetGlobalFont(byte[] fontBytes) => ApplyToAll(LoadFromBytes(fontBytes));
        public static int SetGlobalFont(string path) => ApplyToAll(LoadFromFile(path));

        private static int ApplyToAll(TMP_FontAsset asset)
        {
            if (asset == null) { return 0; }

            TMP_Text[] labels = Object.FindObjectsOfType<TMP_Text>(true);
            foreach (TMP_Text label in labels)
            {
                label.font = asset;
                label.havePropertiesChanged = true;
            }
            return labels.Length;
        }

        // ==========================================
        // The project-wide font.
        //
        // SetGlobalFont above is a one-shot: it walks the
        // labels that are loaded right now and stops. The
        // three below are the standing arrangement -
        // whatever the project's settings asset says,
        // applied to everything, kept applied across
        // scene loads, and never written into a scene.
        //
        // Set it up once from Unity DirectTMP ▸ Global
        // Font…; these are here for the project that
        // would rather do it in code, and for the one
        // that instantiates a screenful of prefabs and
        // wants to say so rather than pay for a watcher.
        // ==========================================

        /// <summary>
        /// Is the project-wide font override applied right now?
        /// </summary>
        public static bool GlobalFontActive => DirectGlobalFont.IsActive;

        /// <summary>
        /// The dynamic font asset the project-wide override is using, or null.
        /// </summary>
        public static TMP_FontAsset GlobalFontAsset => DirectGlobalFont.Asset;

        /// <summary>
        /// Re-reads the project-wide settings and applies them to everything
        /// currently loaded. Call it after instantiating a screenful of
        /// prefabs if you turned the automatic watcher off. Returns false when
        /// there is nothing configured to apply.
        /// </summary>
        public static bool RefreshGlobalFont() => DirectGlobalFont.Apply();

        /// <summary>
        /// Takes the project-wide override back off every label, restoring the
        /// font each one had before.
        /// </summary>
        public static void ClearGlobalFont() => DirectGlobalFont.Revert();

        // ==========================================
        // Text that reads.
        //
        // The font side of this package answers "the
        // glyph is missing". These two answer "the glyph
        // is there and the word is still wrong", which
        // is every Arabic-script label TextMeshPro has
        // ever drawn: unjoined letters in stored order.
        // ==========================================

        /// <summary>
        /// Gives every TextMeshPro label currently loaded a
        /// <see cref="DirectText"/>, so Persian, Arabic, Urdu and Hebrew read
        /// correctly wherever they turn up - including the labels inside a
        /// TMP_Dropdown. Returns how many labels were changed.
        ///
        /// Labels that already have one are left alone, so this is safe to
        /// call after loading a scene, and safe to call twice.
        /// </summary>
        public static int ShapeAll()
        {
            TMP_Text[] labels = Object.FindObjectsOfType<TMP_Text>(true);

            int added = 0;
            foreach (TMP_Text label in labels)
            {
                if (label == null) { continue; }
                if (label.GetComponent<DirectText>() != null) { continue; }
                if (DirectText.EnsureOn(label.gameObject) != null) { added++; }
            }
            return added;
        }

        /// <summary>
        /// One string, joined up and put in reading order, with any rich-text
        /// tags kept around the words they wrap. For text you are drawing
        /// yourself - into a Texture2D, an IMGUI label, a legacy UI.Text -
        /// rather than through a TMP label with a <see cref="DirectText"/> on
        /// it. Text with nothing right-to-left in it comes back unchanged.
        /// </summary>
        public static string Prepare(string text) => DirectRichText.Prepare(text);

        /// <summary>The same, with the paragraph direction forced.</summary>
        public static string Prepare(string text, int paragraphDirection) => DirectRichText.Prepare(text, paragraphDirection);

        // ==========================================
        // Preload - rasterize glyphs ahead of time so the
        // first frame showing them doesn't pay the whole
        // per-glyph cost at once. Returns the characters
        // the font could not supply (empty when all fit).
        // ==========================================
        public static string Preload(Font font, string characters) => DirectFontLoader.Preload(Load(font), characters);
        public static string Preload(byte[] fontBytes, string characters) => DirectFontLoader.Preload(LoadFromBytes(fontBytes), characters);
        public static string Preload(string path, string characters) => DirectFontLoader.Preload(LoadFromFile(path), characters);
        public static string Preload(TMP_FontAsset fontAsset, string characters) => DirectFontLoader.Preload(fontAsset, characters);

        // ==========================================
        // Cache control
        // ==========================================

        /// <summary>How many distinct dynamic font assets are cached right now.</summary>
        public static int CachedFontCount => DirectFontCache.Count;

        /// <summary>
        /// Clear every cached atlas - a good thing to call on a scene change to
        /// reclaim the memory held by glyphs you no longer need. Labels rebuild
        /// their font on next use. Returns how many assets were released.
        /// </summary>
        public static int ClearCache() => DirectFontCache.Clear();
    }
}
