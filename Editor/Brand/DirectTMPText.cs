// ==========================================
// DirectTMPText
// Three languages, one call.
//
//   DirectTMPText.L("Rebuild", "再構築", "بازسازی")
//
// Every user-facing string in the editor windows goes
// through L(). It reads clumsier than a resource file
// for about a week and then it is the reason the tool
// is actually translated: a string added in English
// sits three characters away from the two places its
// translations have to go, so "I will localise it
// later" never gets the chance to become "nobody ever
// did".
//
// The set of languages is deliberately the same three
// Unity DocSnap speaks - English, Japanese, Persian -
// because they are the ones this author can check, and
// a fourth language nobody can proof-read is worse
// than an honest three.
//
// Persian matters here beyond politeness. A tool whose
// entire subject is "your font has Persian in it, use
// it" would be a strange thing to ship with an
// English-only interface.
// ==========================================
using System.Collections.Generic;
using UnityEditor;

namespace UnityDirectTMP.Editor
{
    /// <summary>
    /// The three languages the editor UI speaks, plus the string-picking
    /// helper every window uses.
    /// </summary>
    internal static class DirectTMPText
    {
        public const string English = "en";
        public const string Japanese = "ja";
        public const string Persian = "fa";

        /// <summary>Every supported language code, in menu order.</summary>
        public static readonly string[] Codes = { English, Japanese, Persian };

        /// <summary>Native names, for the language dropdown.</summary>
        public static readonly string[] Labels = { "English", "日本語", "فارسی" };

        private const string LanguageKey = "UnityDirectTMP.language";

        // ==========================================
        // Current language.
        //
        // Stored per machine rather than per project:
        // the language somebody reads in is a property
        // of the person, not of the repository, and a
        // team with a Japanese artist and a Persian
        // programmer should not be arguing in version
        // control about whose Editor is in whose
        // language.
        //
        // The first-run default is guessed from the
        // Editor's own locale, so the person who most
        // needs a translation is the one who does not
        // have to go and find the setting.
        // ==========================================
        public static string Current
        {
            get
            {
                string stored = EditorPrefs.GetString(LanguageKey, string.Empty);
                return IsSupported(stored) ? stored : GuessFromEditor();
            }
            set
            {
                EditorPrefs.SetString(LanguageKey, IsSupported(value) ? value : English);
            }
        }

        /// <summary>True when the current language is written right-to-left.</summary>
        public static bool IsRightToLeft => Current == Persian;

        /// <summary>Is this a language the tool actually has strings for?</summary>
        public static bool IsSupported(string code)
        {
            if (string.IsNullOrEmpty(code)) { return false; }
            for (int i = 0; i < Codes.Length; i++)
            {
                if (Codes[i] == code) { return true; }
            }
            return false;
        }

        /// <summary>Index of a language in <see cref="Codes"/>, or 0 (English).</summary>
        public static int IndexOf(string code)
        {
            for (int i = 0; i < Codes.Length; i++)
            {
                if (Codes[i] == code) { return i; }
            }
            return 0;
        }

        // ==========================================
        // L - pick the string for the current language.
        //
        // A missing translation falls back to English
        // rather than to empty. An untranslated button
        // is mildly annoying; a blank button is broken.
        // ==========================================
        public static string L(string en, string ja, string fa)
        {
            switch (Current)
            {
                case Japanese: return string.IsNullOrEmpty(ja) ? en : ja;
                case Persian: return string.IsNullOrEmpty(fa) ? en : fa;
                default: return en;
            }
        }

        /// <summary>
        /// The same pick, for a language named explicitly. Used by tests and
        /// by anything that has to render all three at once.
        /// </summary>
        public static string For(string language, string en, string ja, string fa)
        {
            switch (language)
            {
                case Japanese: return string.IsNullOrEmpty(ja) ? en : ja;
                case Persian: return string.IsNullOrEmpty(fa) ? en : fa;
                default: return en;
            }
        }

        /// <summary>
        /// A <see cref="UnityEngine.GUIContent"/> whose label and tooltip are
        /// both localised. Nearly every control in the tool is built with this
        /// rather than a bare string, which is how the tooltips stay in the
        /// same language as the labels above them.
        /// </summary>
        public static UnityEngine.GUIContent C(
            string labelEn, string labelJa, string labelFa,
            string tipEn = null, string tipJa = null, string tipFa = null)
        {
            string label = L(labelEn, labelJa, labelFa);
            string tip = tipEn == null ? null : L(tipEn, tipJa, tipFa);
            return new UnityEngine.GUIContent(label, tip);
        }

        // ==========================================
        // GuessFromEditor
        //
        // Unity exposes its own UI language through
        // LocalizationDatabase / SystemLanguage. Japanese
        // is the only one of our three Unity itself ships,
        // so the guess is: a Japanese Editor gets Japanese,
        // a machine whose system language is Persian gets
        // Persian, everybody else gets English.
        // ==========================================
        private static string GuessFromEditor()
        {
            if (UnityEngine.Application.systemLanguage == UnityEngine.SystemLanguage.Japanese) { return Japanese; }

            // Unity has no SystemLanguage.Persian, so the check is the
            // culture the .NET runtime reports.
            string culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (culture == "fa") { return Persian; }
            if (culture == "ja") { return Japanese; }

            return English;
        }

        // ==========================================
        // A tiny shared vocabulary.
        //
        // Words that appear in four or five windows -
        // "Apply", "Revert", "Close" - live here so they
        // are translated once and can never disagree
        // between two panels that sit next to each other.
        // ==========================================
        private static readonly Dictionary<string, string[]> Common = new Dictionary<string, string[]>
        {
            //  key                             en                ja              fa
            { "apply", new[] { "Apply", "適用", "اعمال" } },
            { "revert", new[] { "Revert", "元に戻す", "برگرداندن" } },
            { "close", new[] { "Close", "閉じる", "بستن" } },
            { "cancel", new[] { "Cancel", "キャンセル", "انصراف" } },
            { "refresh", new[] { "Refresh", "更新", "تازه‌سازی" } },
            { "settings", new[] { "Settings", "設定", "تنظیمات" } },
            { "language", new[] { "Language", "言語", "زبان" } },
            { "font", new[] { "Font", "フォント", "فونت" } },
            { "preview", new[] { "Preview", "プレビュー", "پیش‌نمایش" } },
            { "none", new[] { "None", "なし", "هیچ‌کدام" } },
            { "size", new[] { "Size", "サイズ", "اندازه" } },
            { "glyphs", new[] { "glyphs", "グリフ", "گلیف" } },
            { "scripts", new[] { "Scripts", "文字体系", "خط‌ها" } }
        };

        /// <summary>
        /// A word from the shared vocabulary. An unknown key returns the key
        /// itself, which shows up in the UI as something obviously wrong
        /// rather than as a blank space.
        /// </summary>
        public static string Word(string key)
        {
            if (!Common.TryGetValue(key, out string[] forms)) { return key; }
            return For(Current, forms[0], forms[1], forms[2]);
        }
    }
}
