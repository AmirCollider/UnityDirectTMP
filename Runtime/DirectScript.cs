// ==========================================
// DirectScript
// Which writing system a piece of text is in.
//
// Used to pick a font per language: a label showing
// Persian gets the Persian font, the same label showing
// Japanese later gets the Japanese one, and neither
// needs a second component or a second label.
//
// Only the groups that matter for choosing a FONT are
// distinguished. Japanese, Chinese and Korean are one
// group because one CJK font almost always sets all
// three; Persian, Arabic and Urdu are one group for the
// same reason.
//
// Pure C#. No Unity types.
// ==========================================
namespace UnityDirectTMP
{
    /// <summary>A family of writing systems that a single font usually covers.</summary>
    public enum DirectScript
    {
        /// <summary>Nothing with a strong script - digits, punctuation, spaces.</summary>
        None = 0,

        /// <summary>English and the rest of the Latin alphabet.</summary>
        Latin = 1,

        /// <summary>Persian, Arabic, Urdu, Kurdish, Pashto.</summary>
        Arabic = 2,

        /// <summary>Japanese, Chinese, Korean.</summary>
        Cjk = 3,

        /// <summary>Russian and the rest of Cyrillic.</summary>
        Cyrillic = 4,

        /// <summary>Anything with letters that none of the above covers.</summary>
        Other = 5
    }

    /// <summary>Works out which writing system some text is in.</summary>
    public static class DirectScripts
    {
        /// <summary>The script of one character.</summary>
        public static DirectScript Of(char c)
        {
            int u = c;

            if (u < 0x0080)
            {
                return (u >= 'A' && u <= 'Z') || (u >= 'a' && u <= 'z')
                    ? DirectScript.Latin
                    : DirectScript.None;
            }

            // Latin-1 Supplement through Latin Extended-B, and Latin Extended Additional.
            if (u >= 0x00C0 && u <= 0x024F) { return DirectScript.Latin; }
            if (u >= 0x1E00 && u <= 0x1EFF) { return DirectScript.Latin; }

            if (u >= 0x0400 && u <= 0x052F) { return DirectScript.Cyrillic; }

            // Arabic, Syriac, Arabic Supplement, Extended-A, and both
            // presentation-form blocks - a shaped string is still Arabic.
            if (u >= 0x0600 && u <= 0x06FF) { return DirectScript.Arabic; }
            if (u >= 0x0750 && u <= 0x077F) { return DirectScript.Arabic; }
            if (u >= 0x08A0 && u <= 0x08FF) { return DirectScript.Arabic; }
            if (u >= 0xFB50 && u <= 0xFDFF) { return DirectScript.Arabic; }
            if (u >= 0xFE70 && u <= 0xFEFF) { return DirectScript.Arabic; }

            // Kana, CJK ideographs, Hangul, and the fullwidth forms.
            if (u >= 0x3040 && u <= 0x30FF) { return DirectScript.Cjk; }
            if (u >= 0x3400 && u <= 0x4DBF) { return DirectScript.Cjk; }
            if (u >= 0x4E00 && u <= 0x9FFF) { return DirectScript.Cjk; }
            if (u >= 0xAC00 && u <= 0xD7AF) { return DirectScript.Cjk; }
            if (u >= 0xF900 && u <= 0xFAFF) { return DirectScript.Cjk; }
            if (u >= 0xFF00 && u <= 0xFF60) { return DirectScript.Cjk; }

            // CJK punctuation is shared with Latin in mixed text often enough
            // that letting it choose a font would be wrong.
            if (u >= 0x3000 && u <= 0x303F) { return DirectScript.None; }

            // Everything else that is a letter rather than punctuation.
            return char.IsLetter(c) ? DirectScript.Other : DirectScript.None;
        }

        /// <summary>
        /// The script most of <paramref name="text"/> is in - the one a font
        /// should be chosen for.
        ///
        /// Counted by character rather than decided by the first letter, so
        /// "Unity ۱۲۳ سلام دنیا" is Persian rather than English. Characters
        /// with no script of their own - digits, spaces, punctuation - do not
        /// vote.
        /// </summary>
        public static DirectScript DominantOf(string text)
        {
            if (string.IsNullOrEmpty(text)) { return DirectScript.None; }

            int latin = 0, arabic = 0, cjk = 0, cyrillic = 0, other = 0;

            for (int i = 0; i < text.Length; i++)
            {
                switch (Of(text[i]))
                {
                    case DirectScript.Latin: latin++; break;
                    case DirectScript.Arabic: arabic++; break;
                    case DirectScript.Cjk: cjk++; break;
                    case DirectScript.Cyrillic: cyrillic++; break;
                    case DirectScript.Other: other++; break;
                }
            }

            DirectScript winner = DirectScript.None;
            int best = 0;

            if (arabic > best) { best = arabic; winner = DirectScript.Arabic; }
            if (cjk > best) { best = cjk; winner = DirectScript.Cjk; }
            if (cyrillic > best) { best = cyrillic; winner = DirectScript.Cyrillic; }
            if (latin > best) { best = latin; winner = DirectScript.Latin; }
            if (other > best) { winner = DirectScript.Other; }

            return winner;
        }

        /// <summary>Every script present in the text, for showing in an Inspector.</summary>
        public static string Describe(string text)
        {
            if (string.IsNullOrEmpty(text)) { return "nothing"; }

            bool latin = false, arabic = false, cjk = false, cyrillic = false, other = false;

            for (int i = 0; i < text.Length; i++)
            {
                switch (Of(text[i]))
                {
                    case DirectScript.Latin: latin = true; break;
                    case DirectScript.Arabic: arabic = true; break;
                    case DirectScript.Cjk: cjk = true; break;
                    case DirectScript.Cyrillic: cyrillic = true; break;
                    case DirectScript.Other: other = true; break;
                }
            }

            string list = string.Empty;
            if (latin) { list = Add(list, "Latin"); }
            if (arabic) { list = Add(list, "Persian/Arabic"); }
            if (cjk) { list = Add(list, "Japanese/Chinese/Korean"); }
            if (cyrillic) { list = Add(list, "Cyrillic"); }
            if (other) { list = Add(list, "other"); }

            return list.Length == 0 ? "no letters" : list;
        }

        private static string Add(string list, string item)
            => list.Length == 0 ? item : list + ", " + item;
    }
}
