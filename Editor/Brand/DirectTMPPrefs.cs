// ==========================================
// DirectTMPPrefs
// The small, boring per-machine flags: which
// one-off explanations have been read, whether the
// promo panel was dismissed, when the welcome window
// last appeared.
//
// They are kept apart from DirectTMPSettings (which
// holds the rasterization defaults that change what
// the tool DOES) and from DirectEditorFontSettings
// (which holds one specific feature's state) because
// mixing "how big is the atlas" with "has this person
// clicked Got it" in one class means every read of
// either has to think about both.
//
// Every key is declared here as a constant rather
// than typed at the call site. A pref key with a typo
// is a setting that silently never persists, and the
// only symptom is a dialog somebody swears they
// dismissed coming back tomorrow.
// ==========================================
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    internal static class DirectTMPPrefs
    {
        // ==========================================
        // Keys
        // ==========================================
        public const string HideEditorFontIntro = "hideEditorFontIntro";
        public const string HidePromo = "hidePromo";
        public const string WelcomeShownForVersion = "welcomeShownForVersion";
        public const string CatalogIncludeSystemFonts = "catalogIncludeSystemFonts";
        public const string CatalogSort = "catalogSort";
        public const string CatalogScriptFilter = "catalogScriptFilter";
        public const string MuteLog = "muteLog";

        // Salted per project for the same reason every other pref here is: two
        // projects open on one machine must not share a dismissed dialog about
        // a font only one of them has.
        private static readonly string Prefix = "UnityDirectTMP." + ProjectSalt() + ".";

        public static bool GetBool(string key, bool fallback) => EditorPrefs.GetBool(Prefix + key, fallback);
        public static void SetBool(string key, bool value) => EditorPrefs.SetBool(Prefix + key, value);

        public static int GetInt(string key, int fallback) => EditorPrefs.GetInt(Prefix + key, fallback);
        public static void SetInt(string key, int value) => EditorPrefs.SetInt(Prefix + key, value);

        public static string GetString(string key, string fallback) => EditorPrefs.GetString(Prefix + key, fallback);
        public static void SetString(string key, string value) => EditorPrefs.SetString(Prefix + key, value ?? string.Empty);

        /// <summary>
        /// Forgets every dismissal and every one-off flag, so the tool behaves
        /// exactly as it does on a fresh install. Offered on the Settings page
        /// next to "Reset to Defaults", because the two are the same request
        /// asked about different halves of the tool.
        /// </summary>
        public static void ResetAll()
        {
            string[] keys =
            {
                HideEditorFontIntro, HidePromo, WelcomeShownForVersion,
                CatalogIncludeSystemFonts, CatalogSort, CatalogScriptFilter, MuteLog
            };
            foreach (string key in keys)
            {
                EditorPrefs.DeleteKey(Prefix + key);
            }
        }

        private static string ProjectSalt()
        {
            string path = Application.dataPath;
            return (path == null ? 0 : path.GetHashCode()).ToString("x8");
        }
    }
}
