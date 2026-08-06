// ==========================================
// DirectTMP
// The public API, and the thing that keeps every label
// pointed at your font.
//
// Two mechanisms, on purpose:
//
//   * Every loaded label has its font replaced, so the
//     text is drawn in YOUR font.
//   * The font is registered in TextMeshPro's own
//     project-wide fallback list, so any label anywhere -
//     including ones instantiated in the tenth minute of
//     play, and ones inside packages you did not write -
//     resolves characters its own font lacks out of
//     yours.
//
// Nothing is added to any GameObject, nothing is written
// into a scene, and no prefab is touched.
// ==========================================
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// Everything this package does, in one place.
    /// </summary>
    public static class DirectTMP
    {
        private static TMP_FontAsset s_font;
        private static bool s_fixRightToLeft = true;

        /// <summary>The font asset every label is being drawn with, or null.</summary>
        public static TMP_FontAsset FontAsset => s_font;

        /// <summary>True once a font has been applied.</summary>
        public static bool Active => s_font != null;

        // ==========================================
        // Starting up
        // ==========================================
        /// <summary>
        /// Reads the project settings and applies them. Called automatically
        /// before the first scene loads, and again by the Editor whenever the
        /// settings change.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            DirectTMPSettings settings = DirectTMPSettings.Instance;
            if (settings == null || settings.Font == null) { return; }

            s_fixRightToLeft = settings.FixRightToLeft;
            Use(settings.Font);
        }

        /// <summary>
        /// Draws every TextMeshPro label in the project with
        /// <paramref name="font"/>. Returns the number of labels changed.
        /// </summary>
        public static int Use(Font font)
        {
            TMP_FontAsset asset = DirectFont.Load(font);
            if (asset == null) { return 0; }

            return Use(asset);
        }

        /// <summary>The same, for a font asset already built.</summary>
        public static int Use(TMP_FontAsset asset)
        {
            if (asset == null) { return 0; }

            s_font = asset;
            RegisterFallback(asset);
            DirectTMPDriver.Ensure();

            return ApplyToAll();
        }

        /// <summary>Stops overriding. Labels keep whatever font they now have.</summary>
        public static void Stop()
        {
            UnregisterFallback(s_font);
            s_font = null;
            DirectTMPDriver.Stop();
        }

        /// <summary>Whether Arabic-script text is joined and reordered.</summary>
        public static bool FixRightToLeft
        {
            get => s_fixRightToLeft;
            set => s_fixRightToLeft = value;
        }

        // ==========================================
        // Text
        // ==========================================
        /// <summary>
        /// Joins Arabic-script letters and puts right-to-left text in reading
        /// order, for the current font. Text with no Arabic script in it comes
        /// back untouched.
        /// </summary>
        public static string Prepare(string text) => Prepare(text, s_font);

        /// <summary>The same, against a font asset of your choosing.</summary>
        public static string Prepare(string text, TMP_FontAsset asset)
        {
            if (string.IsNullOrEmpty(text)) { return text; }
            if (!DirectJoining.NeedsShaping(text) && !DirectBidi.ContainsRightToLeft(text)) { return text; }

            DirectFontJoiner joiner = asset == null ? null : DirectFontJoiner.For(asset);

            return DirectRichText.Apply(text, segment =>
            {
                string shaped = joiner == null
                    ? DirectShaper.Shape(segment)
                    : joiner.Shape(segment);

                return DirectBidi.Reorder(shaped);
            });
        }

        // ==========================================
        // Labels
        // ==========================================
        /// <summary>
        /// Points every TextMeshPro label currently loaded at the font.
        /// Returns how many were changed.
        /// </summary>
        public static int ApplyToAll()
        {
            if (s_font == null) { return 0; }

            int changed = 0;
            foreach (TMP_Text label in AllLabels())
            {
                if (Apply(label)) { changed++; }
            }
            return changed;
        }

        /// <summary>Points one label at the font. Returns true if anything changed.</summary>
        public static bool Apply(TMP_Text label)
        {
            if (label == null || s_font == null) { return false; }
            if (label.font == s_font) { return false; }

            label.font = s_font;
            return true;
        }

        /// <summary>Every TextMeshPro label loaded right now, active or not.</summary>
        public static IEnumerable<TMP_Text> AllLabels()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<TMP_Text>(true);
#endif
        }

        // ==========================================
        // TextMeshPro's own fallback list
        //
        // The safety net under everything else: a label
        // this package never saw still resolves the
        // characters its own font lacks out of yours.
        // ==========================================
        private static void RegisterFallback(TMP_FontAsset asset)
        {
            try
            {
                List<TMP_FontAsset> fallbacks = TMP_Settings.fallbackFontAssets;
                if (fallbacks == null || fallbacks.Contains(asset)) { return; }
                fallbacks.Insert(0, asset);
            }
            catch { /* TMP Essential Resources not imported yet; the override still works */ }
        }

        private static void UnregisterFallback(TMP_FontAsset asset)
        {
            if (asset == null) { return; }

            try { TMP_Settings.fallbackFontAssets?.Remove(asset); }
            catch { /* nothing to undo */ }
        }
    }
}
