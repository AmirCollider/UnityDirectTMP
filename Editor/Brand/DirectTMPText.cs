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
//
// ==========================================
// LOGICAL TEXT AND DISPLAY TEXT
// ==========================================
// L() does one more thing than pick a string: it hands
// the result to DirectDisplayText, which joins the
// Arabic-script letters up and puts them in reading
// order. Unity's IMGUI does neither, so a Persian
// string drawn straight from a C# literal comes out
// unjoined and mirrored - which is what this package's
// own windows did until 1.0.1, in a tool whose entire
// subject is text rendering.
//
// That makes two kinds of string, and mixing them up
// produces a bug that looks exactly like no fix at all:
//
//   DISPLAY text  is ready to draw and nothing else.
//                 L(), Word(), C(), F(), Show().
//                 It is reordered, so its characters
//                 are no longer in the order they are
//                 read in - do not search it, format
//                 into it, or write it to a file.
//
//   LOGICAL text  is the string as stored. Raw(),
//                 WordRaw(), For(). Compose with these,
//                 then pass the finished sentence
//                 through Show() once.
//
// The reason composition has to happen first is
// "{0}": reordering a Persian format string moves the
// placeholder to where the number belongs on screen,
// and string.Format would then insert the number into
// a sentence that has already been turned around. F()
// exists so no call site has to remember that.
//
// Preparing a string twice would also undo it, so
// DirectDisplayText refuses to reorder text that
// already contains presentation forms. That guard is
// what makes a hundred and eighty call sites safe.
// ==========================================
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

        /// <summary>Native names, as stored. Use <see cref="DisplayLabels"/> to draw them.</summary>
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
                s_display.Clear();
                s_wrapped.Clear();
            }
        }

        /// <summary>True when the current language is written right-to-left.</summary>
        public static bool IsRightToLeft => Current == Persian;

        /// <summary>
        /// The paragraph direction interface prose should be laid out with.
        /// Forced rather than detected, because a Persian sentence that opens
        /// with "TextMeshPro" or a version number would otherwise be read as a
        /// left-to-right paragraph and laid out backwards from the second word
        /// on.
        /// </summary>
        public static int Direction => IsRightToLeft ? DirectBidi.RightToLeft : DirectBidi.AutoDirection;

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

        /// <summary>
        /// The language names as they should be drawn. فارسی is Arabic script
        /// like anything else and needs joining up before it goes into a
        /// dropdown.
        /// </summary>
        public static string[] DisplayLabels
        {
            get
            {
                if (s_displayLabels == null)
                {
                    s_displayLabels = new string[Labels.Length];
                    for (int i = 0; i < Labels.Length; i++)
                    {
                        s_displayLabels[i] = DirectDisplayText.Prepare(Labels[i]);
                    }
                }
                return s_displayLabels;
            }
        }

        private static string[] s_displayLabels;

        // ==========================================
        // L - pick the string for the current language,
        // ready to draw.
        //
        // A missing translation falls back to English
        // rather than to empty. An untranslated button
        // is mildly annoying; a blank button is broken.
        // ==========================================
        public static string L(string en, string ja, string fa)
        {
            return Display(Raw(en, ja, fa));
        }

        /// <summary>
        /// The same pick, in logical order - the string as it is stored.
        /// Compose with this, then pass the finished sentence through
        /// <see cref="Show"/> once.
        /// </summary>
        public static string Raw(string en, string ja, string fa)
        {
            return For(Current, en, ja, fa);
        }

        /// <summary>
        /// Pick and format in one call, in the right order: the format string
        /// is filled in while it is still in logical order, and the finished
        /// sentence is prepared for display afterwards.
        /// </summary>
        public static string F(string en, string ja, string fa, params object[] args)
        {
            string format = Raw(en, ja, fa);
            if (args == null || args.Length == 0) { return Display(format); }

            return Display(string.Format(format, args));
        }

        /// <summary>
        /// The same pick, for a language named explicitly, in logical order.
        /// Used by tests and by anything that has to render all three at once.
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

        // ==========================================
        // Show - prepare a string this file did not
        // write.
        //
        // A font family name, an asset path, a folder
        // called فونت‌ها, the preview line somebody
        // typed. None of it goes through L(), and all of
        // it lands in a label - and a font browser that
        // renders the name of a Persian font backwards
        // has failed at the one thing it is for.
        //
        // Direction is detected here rather than forced,
        // because this is data rather than prose: an
        // asset path that happens to contain one Persian
        // folder name is still a left-to-right path.
        // ==========================================
        public static string Show(string text)
        {
            if (string.IsNullOrEmpty(text)) { return text; }
            if (!DirectDisplayText.NeedsPreparing(text)) { return text; }

            return Cached(text, DirectBidi.AutoDirection);
        }

        /// <summary>
        /// A <see cref="GUIContent"/> whose label and tooltip are both
        /// localised and both ready to draw. Nearly every control in the tool
        /// is built with this rather than a bare string, which is how the
        /// tooltips stay in the same language as the labels above them.
        /// </summary>
        public static GUIContent C(
            string labelEn, string labelJa, string labelFa,
            string tipEn = null, string tipJa = null, string tipFa = null)
        {
            string label = L(labelEn, labelJa, labelFa);
            string tip = tipEn == null ? null : L(tipEn, tipJa, tipFa);
            return new GUIContent(label, tip);
        }

        // ==========================================
        // Display / Wrap
        // ==========================================

        /// <summary>
        /// Interface prose, ready to draw: joined up, in reading order, and
        /// laid out with the interface's own direction.
        /// </summary>
        public static string Display(string text)
        {
            if (string.IsNullOrEmpty(text)) { return text; }
            if (!DirectDisplayText.NeedsPreparing(text)) { return text; }

            return Cached(text, Direction);
        }

        /// <summary>
        /// A paragraph, broken into lines that each fit <paramref name="width"/>
        /// and each prepared on their own.
        ///
        /// A right-to-left paragraph cannot be handed to IMGUI's own word
        /// wrapping: reordering makes the last word of the paragraph the
        /// leftmost character of one long line, and IMGUI then breaks that
        /// line into three that read bottom to top. Breaking first, in logical
        /// order, is the only way round it.
        ///
        /// Text with nothing right-to-left in it is returned untouched, for
        /// IMGUI to wrap as it always has.
        ///
        /// The input has to be in LOGICAL order - a raw string, not one that
        /// has already been through L() or Show(). Choosing where to break a
        /// line only means anything in the order the words are read in.
        /// </summary>
        public static string Wrap(string logical, GUIStyle style, float width)
        {
            string text = logical;
            if (string.IsNullOrEmpty(text) || style == null || width <= 0f) { return Display(text); }
            if (!DirectDisplayText.NeedsPreparing(text)) { return text; }

            // Widths wobble by a fraction of a pixel between repaints, and a
            // cache keyed on the exact float would never hit. Rounding to two
            // pixels is finer than any wrap this changes.
            int bucket = Mathf.RoundToInt(width * 0.5f);
            var key = new WrapKey(text, bucket, style.GetHashCode());

            if (s_wrapped.TryGetValue(key, out string cached)) { return cached; }

            var content = new GUIContent();
            string wrapped = DirectDisplayText.Wrap(
                text,
                width,
                line =>
                {
                    content.text = line;
                    return style.CalcSize(content).x;
                },
                Direction);

            Trim(s_wrapped);
            s_wrapped[key] = wrapped;
            return wrapped;
        }

        // ==========================================
        // Dialog
        //
        // EditorUtility.DisplayDialog owns its own box
        // and wraps its own text, and there is no hook
        // to break a line inside it - so a prepared
        // Persian paragraph would be re-wrapped into
        // lines that read bottom to top, the one failure
        // this whole file exists to prevent.
        //
        // The answer is to break it here, by character
        // count rather than by measurement, and hand the
        // dialog lines short enough that it never needs
        // to wrap them itself. A dialog is a handful of
        // sentences; counting characters is coarse and
        // completely adequate.
        // ==========================================
        private const int DialogLineLength = 64;

        /// <summary>
        /// Prepares a message for <c>EditorUtility.DisplayDialog</c>, which
        /// draws text with the same Unity text stack every other label uses
        /// and has the same trouble with it. Takes LOGICAL text.
        /// </summary>
        public static string Dialog(string logical)
        {
            if (string.IsNullOrEmpty(logical)) { return logical; }
            if (!DirectDisplayText.NeedsPreparing(logical)) { return logical; }

            var lines = new List<string>();
            foreach (string line in logical.Split('\n'))
            {
                if (!DirectDisplayText.NeedsPreparing(line))
                {
                    lines.Add(line);
                    continue;
                }

                lines.AddRange(DirectDisplayText.WrapToLines(
                    line,
                    DialogLineLength,
                    candidate => candidate.Length,
                    DirectBidi.AutoDirection));
            }

            return string.Join("\n", lines.ToArray());
        }

        // ==========================================
        // The cache.
        //
        // Shaping and reordering are linear and cheap,
        // but OnGUI runs them on every label on every
        // repaint, and the allocation is what costs -
        // a window with sixty labels would produce sixty
        // strings a frame for the collector. The same
        // literal always prepares to the same result, so
        // one dictionary removes all of it.
        //
        // Bounded, because the catalog can push a few
        // hundred font names through Show() and a cache
        // that only grows is a leak with a friendly
        // name.
        // ==========================================
        private const int CacheLimit = 1024;

        private static readonly Dictionary<DisplayKey, string> s_display = new Dictionary<DisplayKey, string>();
        private static readonly Dictionary<WrapKey, string> s_wrapped = new Dictionary<WrapKey, string>();

        private static string Cached(string text, int direction)
        {
            var key = new DisplayKey(text, direction);
            if (s_display.TryGetValue(key, out string prepared)) { return prepared; }

            prepared = DirectDisplayText.Prepare(text, direction);

            Trim(s_display);
            s_display[key] = prepared;
            return prepared;
        }

        private static void Trim<TKey>(Dictionary<TKey, string> cache)
        {
            if (cache.Count >= CacheLimit) { cache.Clear(); }
        }

        private readonly struct DisplayKey : System.IEquatable<DisplayKey>
        {
            private readonly string _text;
            private readonly int _direction;

            public DisplayKey(string text, int direction)
            {
                _text = text;
                _direction = direction;
            }

            public bool Equals(DisplayKey other) => _direction == other._direction && _text == other._text;
            public override bool Equals(object obj) => obj is DisplayKey other && Equals(other);
            public override int GetHashCode() => (_text.GetHashCode() * 397) ^ _direction;
        }

        private readonly struct WrapKey : System.IEquatable<WrapKey>
        {
            private readonly string _text;
            private readonly int _width;
            private readonly int _style;

            public WrapKey(string text, int width, int style)
            {
                _text = text;
                _width = width;
                _style = style;
            }

            public bool Equals(WrapKey other) =>
                _width == other._width && _style == other._style && _text == other._text;

            public override bool Equals(object obj) => obj is WrapKey other && Equals(other);

            public override int GetHashCode()
            {
                int hash = _text.GetHashCode();
                hash = (hash * 397) ^ _width;
                return (hash * 397) ^ _style;
            }
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
            if (Application.systemLanguage == SystemLanguage.Japanese) { return Japanese; }

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
        /// A word from the shared vocabulary, ready to draw. An unknown key
        /// returns the key itself, which shows up in the UI as something
        /// obviously wrong rather than as a blank space.
        /// </summary>
        public static string Word(string key)
        {
            return Display(WordRaw(key));
        }

        /// <summary>The same word in logical order, for composing into a sentence.</summary>
        public static string WordRaw(string key)
        {
            if (!Common.TryGetValue(key, out string[] forms)) { return key; }
            return For(Current, forms[0], forms[1], forms[2]);
        }
    }
}
