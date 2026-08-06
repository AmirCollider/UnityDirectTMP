// ==========================================
// DirectJoining
// How the Arabic script connects its letters.
//
// Joining is a property of the SCRIPT, not of the font.
// A beh joins on both sides in every font that has a
// beh, and in fonts that do not have one at all.
//
// That sentence is the whole reason this file exists.
// The previous implementation derived joining from "does
// this font contain the U+FE9x presentation codepoint",
// which made a fact about Unicode depend on a fact about
// the font - so one missing codepoint silently changed
// the shape of the letter NEXT to it, and words came
// apart at letters that were perfectly fine.
//
// Nothing here looks at a font.
// ==========================================
using System.Collections.Generic;

namespace UnityDirectTMP
{
    /// <summary>
    /// How a character connects to its neighbours. Unicode's own vocabulary
    /// (ArabicShaping.txt), because the rules read wrong in any other.
    /// </summary>
    public enum DirectJoiningClass
    {
        /// <summary>Joins nothing. Spaces, Latin, digits, punctuation, hamza.</summary>
        None = 0,

        /// <summary>Accepts a join from the letter before; offers none after. ا د ر و</summary>
        Right = 1,

        /// <summary>Joins on both sides. Most of the alphabet.</summary>
        Dual = 2,

        /// <summary>Forces neighbours to join without being a letter. Tatweel, ZWJ.</summary>
        Causing = 3,

        /// <summary>Drawn above or below; invisible to joining. Harakat.</summary>
        Transparent = 4
    }

    /// <summary>The four shapes a letter of the Arabic script can take.</summary>
    public enum DirectForm
    {
        /// <summary>Standing alone.</summary>
        Isolated = 0,

        /// <summary>Joined to the letter before it.</summary>
        Final = 1,

        /// <summary>Joined to the letter after it.</summary>
        Initial = 2,

        /// <summary>Joined on both sides.</summary>
        Medial = 3
    }

    /// <summary>
    /// Joining classes for the Arabic script, and the Unicode presentation
    /// codepoints that name each joined shape where one exists.
    /// </summary>
    public static class DirectJoining
    {
        /// <summary>Zero-width non-joiner. Persian's word-internal break: می‌شه.</summary>
        public const char ZeroWidthNonJoiner = '\u200C';

        /// <summary>Zero-width joiner. Forces a join where there would be none.</summary>
        public const char ZeroWidthJoiner = '\u200D';

        /// <summary>Arabic tatweel, the stretching line. Joins both sides and is drawn.</summary>
        public const char Tatweel = '\u0640';

        /// <summary>Lam. The first half of the one ligature Arabic insists on.</summary>
        public const char Lam = '\u0644';

        // ==========================================
        // Right-joining letters. Everything else in the
        // letter ranges below is dual-joining, which is
        // the shorter list to write down and the one that
        // does not rot when Unicode adds a letter.
        // ==========================================
        private static readonly HashSet<int> RightJoining = new HashSet<int>
        {
            0x0622, 0x0623, 0x0624, 0x0625, 0x0627, 0x0629, 0x062F, 0x0630,
            0x0631, 0x0632, 0x0648,
            0x0671, 0x0672, 0x0673, 0x0675, 0x0676, 0x0677,
            0x0688, 0x0689, 0x068A, 0x068B, 0x068C, 0x068D, 0x068E, 0x068F,
            0x0690, 0x0691, 0x0692, 0x0693, 0x0694, 0x0695, 0x0696, 0x0697,
            0x0698, 0x0699,
            0x06C0, 0x06C3, 0x06C4, 0x06C5, 0x06C6, 0x06C7, 0x06C8, 0x06C9,
            0x06CA, 0x06CB, 0x06CD, 0x06CF, 0x06D2, 0x06D3, 0x06D5,
            0x06EE, 0x06EF,
            0x0710, 0x0715, 0x0716, 0x0717, 0x0718, 0x0719, 0x071E,
            0x074D, 0x0759, 0x075A, 0x075B,
            0x076B, 0x076C, 0x0771, 0x0773, 0x0774, 0x0778, 0x0779,
        };

        // Letters that are in the ranges but join nothing at all.
        private static readonly HashSet<int> NonJoining = new HashSet<int>
        {
            0x0621, // ء hamza - it has a presentation form because it is drawn,
                    // not because it connects.
            0x0674, 0x06DD, 0x08E2,
        };

        // Where the letters are. Anything in here that is not listed above and
        // is not a mark is dual-joining.
        private static readonly int[,] LetterRanges =
        {
            { 0x0620, 0x063F }, { 0x0641, 0x064A }, { 0x066E, 0x066F },
            { 0x0671, 0x06D3 }, { 0x06D5, 0x06D5 }, { 0x06E5, 0x06E6 },
            { 0x06EE, 0x06EF }, { 0x06FA, 0x06FF }, { 0x0750, 0x077F },
            { 0x08A0, 0x08BF },
        };

        // Marks: drawn above or below, and skipped entirely when a letter
        // looks for the neighbour it should join to. A fatha between two
        // letters must not stop them connecting.
        private static readonly int[,] MarkRanges =
        {
            { 0x0300, 0x036F }, { 0x0610, 0x061A }, { 0x064B, 0x065F },
            { 0x0670, 0x0670 }, { 0x06D6, 0x06DC }, { 0x06DF, 0x06E4 },
            { 0x06E7, 0x06E8 }, { 0x06EA, 0x06ED }, { 0x08D3, 0x08E1 },
            { 0x08E3, 0x08FF }, { 0xFE00, 0xFE0F }, { 0xFE20, 0xFE2F },
        };

        /// <summary>The joining class of one character.</summary>
        public static DirectJoiningClass ClassOf(char c)
        {
            int u = c;

            if (u == Tatweel || u == ZeroWidthJoiner) { return DirectJoiningClass.Causing; }
            if (InRanges(u, MarkRanges)) { return DirectJoiningClass.Transparent; }
            if (NonJoining.Contains(u)) { return DirectJoiningClass.None; }
            if (RightJoining.Contains(u)) { return DirectJoiningClass.Right; }
            if (InRanges(u, LetterRanges)) { return DirectJoiningClass.Dual; }

            return DirectJoiningClass.None;
        }

        /// <summary>Can a character of this class connect to the one after it?</summary>
        public static bool Offers(DirectJoiningClass j)
            => j == DirectJoiningClass.Dual || j == DirectJoiningClass.Causing;

        /// <summary>Can a character of this class accept a join from before it?</summary>
        public static bool Accepts(DirectJoiningClass j)
            => j == DirectJoiningClass.Dual
            || j == DirectJoiningClass.Right
            || j == DirectJoiningClass.Causing;

        /// <summary>True when this character is shaped at all.</summary>
        public static bool IsShaped(char c)
        {
            DirectJoiningClass j = ClassOf(c);
            return j == DirectJoiningClass.Dual || j == DirectJoiningClass.Right;
        }

        /// <summary>
        /// True when the text contains anything this shaper would change - so
        /// English, Japanese and every other string skips the whole pass.
        /// </summary>
        public static bool NeedsShaping(string text)
        {
            if (string.IsNullOrEmpty(text)) { return false; }

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ZeroWidthNonJoiner) { return true; }
                if (IsShaped(text[i])) { return true; }
            }
            return false;
        }

        // ==========================================
        // Presentation codepoints.
        //
        // Not used to decide anything - only as the
        // address a joined glyph is registered under, so
        // that a shaped string stays as close to real
        // text as it can be. Letters with no entry here
        // get a private-use address instead; see
        // DirectFontJoiner.
        //
        // Four per letter, in DirectForm order:
        // isolated, final, initial, medial.
        // ==========================================
        private static readonly Dictionary<char, char[]> Presentation = new Dictionary<char, char[]>
        {
            { 'ء', new[] { 'ﺀ', '\0'   , '\0'   , '\0'    } }, // ء
            { 'آ', new[] { 'ﺁ', 'ﺂ', '\0'   , '\0'    } }, // آ
            { 'أ', new[] { 'ﺃ', 'ﺄ', '\0'   , '\0'    } }, // أ
            { 'ؤ', new[] { 'ﺅ', 'ﺆ', '\0'   , '\0'    } }, // ؤ
            { 'إ', new[] { 'ﺇ', 'ﺈ', '\0'   , '\0'    } }, // إ
            { 'ئ', new[] { 'ﺉ', 'ﺊ', 'ﺋ', 'ﺌ' } }, // ئ
            { 'ا', new[] { 'ﺍ', 'ﺎ', '\0'   , '\0'    } }, // ا
            { 'ب', new[] { 'ﺏ', 'ﺐ', 'ﺑ', 'ﺒ' } }, // ب
            { 'ة', new[] { 'ﺓ', 'ﺔ', '\0'   , '\0'    } }, // ة
            { 'ت', new[] { 'ﺕ', 'ﺖ', 'ﺗ', 'ﺘ' } }, // ت
            { 'ث', new[] { 'ﺙ', 'ﺚ', 'ﺛ', 'ﺜ' } }, // ث
            { 'ج', new[] { 'ﺝ', 'ﺞ', 'ﺟ', 'ﺠ' } }, // ج
            { 'ح', new[] { 'ﺡ', 'ﺢ', 'ﺣ', 'ﺤ' } }, // ح
            { 'خ', new[] { 'ﺥ', 'ﺦ', 'ﺧ', 'ﺨ' } }, // خ
            { 'د', new[] { 'ﺩ', 'ﺪ', '\0'   , '\0'    } }, // د
            { 'ذ', new[] { 'ﺫ', 'ﺬ', '\0'   , '\0'    } }, // ذ
            { 'ر', new[] { 'ﺭ', 'ﺮ', '\0'   , '\0'    } }, // ر
            { 'ز', new[] { 'ﺯ', 'ﺰ', '\0'   , '\0'    } }, // ز
            { 'س', new[] { 'ﺱ', 'ﺲ', 'ﺳ', 'ﺴ' } }, // س
            { 'ش', new[] { 'ﺵ', 'ﺶ', 'ﺷ', 'ﺸ' } }, // ش
            { 'ص', new[] { 'ﺹ', 'ﺺ', 'ﺻ', 'ﺼ' } }, // ص
            { 'ض', new[] { 'ﺽ', 'ﺾ', 'ﺿ', 'ﻀ' } }, // ض
            { 'ط', new[] { 'ﻁ', 'ﻂ', 'ﻃ', 'ﻄ' } }, // ط
            { 'ظ', new[] { 'ﻅ', 'ﻆ', 'ﻇ', 'ﻈ' } }, // ظ
            { 'ع', new[] { 'ﻉ', 'ﻊ', 'ﻋ', 'ﻌ' } }, // ع
            { 'غ', new[] { 'ﻍ', 'ﻎ', 'ﻏ', 'ﻐ' } }, // غ
            { 'ف', new[] { 'ﻑ', 'ﻒ', 'ﻓ', 'ﻔ' } }, // ف
            { 'ق', new[] { 'ﻕ', 'ﻖ', 'ﻗ', 'ﻘ' } }, // ق
            { 'ك', new[] { 'ﻙ', 'ﻚ', 'ﻛ', 'ﻜ' } }, // ك
            { 'ل', new[] { 'ﻝ', 'ﻞ', 'ﻟ', 'ﻠ' } }, // ل
            { 'م', new[] { 'ﻡ', 'ﻢ', 'ﻣ', 'ﻤ' } }, // م
            { 'ن', new[] { 'ﻥ', 'ﻦ', 'ﻧ', 'ﻨ' } }, // ن
            { 'ه', new[] { 'ﻩ', 'ﻪ', 'ﻫ', 'ﻬ' } }, // ه
            { 'و', new[] { 'ﻭ', 'ﻮ', '\0'   , '\0'    } }, // و
            { 'ى', new[] { 'ﻯ', 'ﻰ', 'ﯨ', 'ﯩ' } }, // ى
            { 'ي', new[] { 'ﻱ', 'ﻲ', 'ﻳ', 'ﻴ' } }, // ي

            // Persian, Urdu and the wider script.
            { 'ٱ', new[] { 'ﭐ', 'ﭑ', '\0'   , '\0'    } }, // ٱ
            { 'ٻ', new[] { 'ﭒ', 'ﭓ', 'ﭔ', 'ﭕ' } }, // ٻ
            { 'پ', new[] { 'ﭖ', 'ﭗ', 'ﭘ', 'ﭙ' } }, // پ
            { 'ڀ', new[] { 'ﭚ', 'ﭛ', 'ﭜ', 'ﭝ' } }, // ڀ
            { 'ٺ', new[] { 'ﭞ', 'ﭟ', 'ﭠ', 'ﭡ' } }, // ٺ
            { 'ٿ', new[] { 'ﭢ', 'ﭣ', 'ﭤ', 'ﭥ' } }, // ٿ
            { 'ٹ', new[] { 'ﭦ', 'ﭧ', 'ﭨ', 'ﭩ' } }, // ٹ
            { 'ڤ', new[] { 'ﭪ', 'ﭫ', 'ﭬ', 'ﭭ' } }, // ڤ
            { 'ڦ', new[] { 'ﭮ', 'ﭯ', 'ﭰ', 'ﭱ' } }, // ڦ
            { 'ڄ', new[] { 'ﭲ', 'ﭳ', 'ﭴ', 'ﭵ' } }, // ڄ
            { 'ڃ', new[] { 'ﭶ', 'ﭷ', 'ﭸ', 'ﭹ' } }, // ڃ
            { 'چ', new[] { 'ﭺ', 'ﭻ', 'ﭼ', 'ﭽ' } }, // چ
            { 'ڇ', new[] { 'ﭾ', 'ﭿ', 'ﮀ', 'ﮁ' } }, // ڇ
            { 'ڍ', new[] { 'ﮂ', 'ﮃ', '\0'   , '\0'    } }, // ڍ
            { 'ڌ', new[] { 'ﮄ', 'ﮅ', '\0'   , '\0'    } }, // ڌ
            { 'ڎ', new[] { 'ﮆ', 'ﮇ', '\0'   , '\0'    } }, // ڎ
            { 'ڈ', new[] { 'ﮈ', 'ﮉ', '\0'   , '\0'    } }, // ڈ
            { 'ژ', new[] { 'ﮊ', 'ﮋ', '\0'   , '\0'    } }, // ژ
            { 'ڑ', new[] { 'ﮌ', 'ﮍ', '\0'   , '\0'    } }, // ڑ
            { 'ک', new[] { 'ﮎ', 'ﮏ', 'ﮐ', 'ﮑ' } }, // ک
            { 'گ', new[] { 'ﮒ', 'ﮓ', 'ﮔ', 'ﮕ' } }, // گ
            { 'ڳ', new[] { 'ﮖ', 'ﮗ', 'ﮘ', 'ﮙ' } }, // ڳ
            { 'ڱ', new[] { 'ﮚ', 'ﮛ', 'ﮜ', 'ﮝ' } }, // ڱ
            { 'ں', new[] { 'ﮞ', 'ﮟ', '\0'   , '\0'    } }, // ں
            { 'ڻ', new[] { 'ﮠ', 'ﮡ', 'ﮢ', 'ﮣ' } }, // ڻ
            { 'ۀ', new[] { 'ﮤ', 'ﮥ', '\0'   , '\0'    } }, // ۀ
            { 'ہ', new[] { 'ﮦ', 'ﮧ', 'ﮨ', 'ﮩ' } }, // ہ
            { 'ھ', new[] { 'ﮪ', 'ﮫ', 'ﮬ', 'ﮭ' } }, // ھ
            { 'ے', new[] { 'ﮮ', 'ﮯ', '\0'   , '\0'    } }, // ے
            { 'ۓ', new[] { 'ﮰ', 'ﮱ', '\0'   , '\0'    } }, // ۓ
            { 'ڭ', new[] { 'ﯓ', 'ﯔ', 'ﯕ', 'ﯖ' } }, // ڭ
            { 'ۇ', new[] { 'ﯗ', 'ﯘ', '\0'   , '\0'    } }, // ۇ
            { 'ۆ', new[] { 'ﯙ', 'ﯚ', '\0'   , '\0'    } }, // ۆ
            { 'ۈ', new[] { 'ﯛ', 'ﯜ', '\0'   , '\0'    } }, // ۈ
            { 'ۋ', new[] { 'ﯞ', 'ﯟ', '\0'   , '\0'    } }, // ۋ
            { 'ۅ', new[] { 'ﯠ', 'ﯡ', '\0'   , '\0'    } }, // ۅ
            { 'ۉ', new[] { 'ﯢ', 'ﯣ', '\0'   , '\0'    } }, // ۉ
            { 'ې', new[] { 'ﯤ', 'ﯥ', 'ﯦ', 'ﯧ' } }, // ې
            { 'ی', new[] { 'ﯼ', 'ﯽ', 'ﯾ', 'ﯿ' } }, // ی
        };

        // ==========================================
        // Lam-alef. Two codepoints, one glyph, and
        // drawing them separately is the single most
        // recognisable way to get Arabic wrong.
        //
        // Isolated and final only: the ligature ends in
        // an alef, and an alef never offers a join.
        // ==========================================
        private static readonly Dictionary<char, char[]> LamAlefTable = new Dictionary<char, char[]>
        {
            { 'آ', new[] { 'ﻵ', 'ﻶ' } }, // lam + آ
            { 'أ', new[] { 'ﻷ', 'ﻸ' } }, // lam + أ
            { 'إ', new[] { 'ﻹ', 'ﻺ' } }, // lam + إ
            { 'ا', new[] { 'ﻻ', 'ﻼ' } }, // lam + ا
        };

        /// <summary>
        /// The Unicode presentation codepoint for one shape of one letter, or
        /// '\0' when Unicode never gave that shape a codepoint. Plenty of
        /// letters have none at all - which is exactly why the joined glyph is
        /// found through the font's own tables rather than through this.
        /// </summary>
        public static char PresentationForm(char letter, DirectForm form)
            => Presentation.TryGetValue(letter, out char[] f) ? f[(int)form] : '\0';

        /// <summary>The letters Unicode gave presentation codepoints to.</summary>
        public static IEnumerable<char> PresentationLetters => Presentation.Keys;

        /// <summary>
        /// The isolated and final codepoints of the lam-alef ligature formed by
        /// lam and <paramref name="alef"/>, or null when this is not an alef.
        /// </summary>
        public static char[] LamAlef(char alef)
            => LamAlefTable.TryGetValue(alef, out char[] pair) ? pair : null;

        /// <summary>True when the character is one of the shapes a shaper emits.</summary>
        public static bool IsPresentationForm(char c)
        {
            int u = c;
            return (u >= 0xFB50 && u <= 0xFDFF) || (u >= 0xFE70 && u <= 0xFEFF);
        }

        private static bool InRanges(int u, int[,] ranges)
        {
            for (int i = 0; i < ranges.GetLength(0); i++)
            {
                if (u >= ranges[i, 0] && u <= ranges[i, 1]) { return true; }
            }
            return false;
        }
    }
}
