// ==========================================
// DirectArabicShaper
// Arabic-script letters, joined.
//
// Unicode stores Arabic and Persian the way a person
// types them: one codepoint per letter, in the order
// the letters are spoken. U+0633 U+0644 U+0627 U+0645
// is "سلام", four separate letters. What a reader
// expects to SEE is one connected word, in which the
// seen has an initial shape, the lam a medial shape and
// the meem a final shape - four different pictures for
// four letters that were stored as their neutral,
// standalone selves.
//
// Choosing that picture is called shaping, and nothing
// in Unity does it. Unity's IMGUI - the Editor's own
// windows, this package's windows - hands each
// codepoint to the font as-is, so Persian arrives on
// screen as a row of disconnected, standalone letters
// reading the wrong way. TextMeshPro does not do it
// either; its isRightToLeftText flips the order and
// leaves the letters unjoined.
//
// So this file does it. For every letter it looks at
// the neighbours that can actually connect - skipping
// the vowel marks that sit above and below and join
// nothing - and swaps the letter for the Unicode
// presentation form that has the right entry and exit
// strokes. Fonts that carry Arabic carry these forms:
// they are the U+FB50 and U+FE70 blocks, and they exist
// precisely so that a system without a shaping engine
// can still set the script.
//
// Two details that are the difference between "renders"
// and "renders correctly in Persian":
//
//   * ZWNJ (U+200C). Persian writes می‌شه and فونت‌ها
//     with a zero-width non-joiner: the letters must
//     NOT connect across it. It is consumed here - it
//     has done its job once the forms are picked, and
//     leaving it in the string is one more codepoint
//     for a font to have no glyph for.
//
//   * lam-alef. لا is not a lam next to an alef; it is
//     a single ligature glyph, and a font asked to draw
//     the two separately draws something no reader
//     accepts. The four lam-alef combinations get their
//     own codepoints.
//
// Pure C#. No Unity types, no file access, no
// allocation beyond the one StringBuilder - so it runs
// in a player build, in an editor window, and in a unit
// test without a Unity Editor around it.
// ==========================================
using System.Collections.Generic;
using System.Text;

namespace UnityDirectTMP
{
    /// <summary>
    /// How a letter of the Arabic script connects to its neighbours. The names
    /// are Unicode's own (ArabicShaping.txt), because the rules read wrong in
    /// any other vocabulary.
    /// </summary>
    public enum DirectJoining
    {
        /// <summary>Connects to nothing. Spaces, Latin, digits, punctuation, ZWNJ.</summary>
        None = 0,

        /// <summary>Accepts a connection from the letter before it, offers none to the letter after. ا د ر و</summary>
        Right = 1,

        /// <summary>Connects on both sides. Most of the alphabet.</summary>
        Dual = 2,

        /// <summary>Forces its neighbours to connect without being a letter itself. Tatweel, ZWJ.</summary>
        Causing = 3,

        /// <summary>Invisible to joining entirely - it is drawn above or below. Harakat, hamza above.</summary>
        Transparent = 4
    }

    /// <summary>
    /// Turns logically-ordered Arabic-script text into the presentation forms a
    /// font can actually draw joined up.
    /// </summary>
    public static class DirectArabicShaper
    {
        /// <summary>Zero-width non-joiner. Persian's word-internal break.</summary>
        public const char ZeroWidthNonJoiner = '\u200C';

        /// <summary>Zero-width joiner. Forces a connection where there would be none.</summary>
        public const char ZeroWidthJoiner = '\u200D';

        /// <summary>Arabic tatweel - the stretching line. Joins on both sides and is drawn.</summary>
        public const char Tatweel = '\u0640';

        private const char Lam = '\u0644';        // ل

        // ==========================================
        // The forms table.
        //
        // Four entries per letter, in the order the
        // Unicode blocks themselves use:
        //
        //   [0] isolated   standing alone
        //   [1] final      joined to the letter before
        //   [2] initial    joined to the letter after
        //   [3] medial     joined on both sides
        //
        // '\0' means the letter has no such form, which
        // is also how its joining class is read back: a
        // letter with an initial form is dual-joining, a
        // letter with only a final form is right-joining,
        // and a letter with neither joins nothing.
        //
        // The Arabic block first, then the letters
        // Persian and Urdu add on top of it - which are
        // the ones a table copied from an Arabic-only
        // source always misses, and the reason "پگاه"
        // renders as boxes in half the shapers on the
        // internet.
        // ==========================================
        private static readonly Dictionary<char, char[]> Forms = new Dictionary<char, char[]>
        {
            // --- Arabic ---
            { '\u0621', new[] { '\uFE80', '\0'   , '\0'   , '\0'    } },  // ء hamza
            { '\u0622', new[] { '\uFE81', '\uFE82', '\0'   , '\0'    } }, // آ alef madda
            { '\u0623', new[] { '\uFE83', '\uFE84', '\0'   , '\0'    } }, // أ alef hamza above
            { '\u0624', new[] { '\uFE85', '\uFE86', '\0'   , '\0'    } }, // ؤ waw hamza
            { '\u0625', new[] { '\uFE87', '\uFE88', '\0'   , '\0'    } }, // إ alef hamza below
            { '\u0626', new[] { '\uFE89', '\uFE8A', '\uFE8B', '\uFE8C' } }, // ئ yeh hamza
            { '\u0627', new[] { '\uFE8D', '\uFE8E', '\0'   , '\0'    } }, // ا alef
            { '\u0628', new[] { '\uFE8F', '\uFE90', '\uFE91', '\uFE92' } }, // ب beh
            { '\u0629', new[] { '\uFE93', '\uFE94', '\0'   , '\0'    } }, // ة teh marbuta
            { '\u062A', new[] { '\uFE95', '\uFE96', '\uFE97', '\uFE98' } }, // ت teh
            { '\u062B', new[] { '\uFE99', '\uFE9A', '\uFE9B', '\uFE9C' } }, // ث theh
            { '\u062C', new[] { '\uFE9D', '\uFE9E', '\uFE9F', '\uFEA0' } }, // ج jeem
            { '\u062D', new[] { '\uFEA1', '\uFEA2', '\uFEA3', '\uFEA4' } }, // ح hah
            { '\u062E', new[] { '\uFEA5', '\uFEA6', '\uFEA7', '\uFEA8' } }, // خ khah
            { '\u062F', new[] { '\uFEA9', '\uFEAA', '\0'   , '\0'    } }, // د dal
            { '\u0630', new[] { '\uFEAB', '\uFEAC', '\0'   , '\0'    } }, // ذ thal
            { '\u0631', new[] { '\uFEAD', '\uFEAE', '\0'   , '\0'    } }, // ر reh
            { '\u0632', new[] { '\uFEAF', '\uFEB0', '\0'   , '\0'    } }, // ز zain
            { '\u0633', new[] { '\uFEB1', '\uFEB2', '\uFEB3', '\uFEB4' } }, // س seen
            { '\u0634', new[] { '\uFEB5', '\uFEB6', '\uFEB7', '\uFEB8' } }, // ش sheen
            { '\u0635', new[] { '\uFEB9', '\uFEBA', '\uFEBB', '\uFEBC' } }, // ص sad
            { '\u0636', new[] { '\uFEBD', '\uFEBE', '\uFEBF', '\uFEC0' } }, // ض dad
            { '\u0637', new[] { '\uFEC1', '\uFEC2', '\uFEC3', '\uFEC4' } }, // ط tah
            { '\u0638', new[] { '\uFEC5', '\uFEC6', '\uFEC7', '\uFEC8' } }, // ظ zah
            { '\u0639', new[] { '\uFEC9', '\uFECA', '\uFECB', '\uFECC' } }, // ع ain
            { '\u063A', new[] { '\uFECD', '\uFECE', '\uFECF', '\uFED0' } }, // غ ghain
            { '\u0641', new[] { '\uFED1', '\uFED2', '\uFED3', '\uFED4' } }, // ف feh
            { '\u0642', new[] { '\uFED5', '\uFED6', '\uFED7', '\uFED8' } }, // ق qaf
            { '\u0643', new[] { '\uFED9', '\uFEDA', '\uFEDB', '\uFEDC' } }, // ك kaf
            { '\u0644', new[] { '\uFEDD', '\uFEDE', '\uFEDF', '\uFEE0' } }, // ل lam
            { '\u0645', new[] { '\uFEE1', '\uFEE2', '\uFEE3', '\uFEE4' } }, // م meem
            { '\u0646', new[] { '\uFEE5', '\uFEE6', '\uFEE7', '\uFEE8' } }, // ن noon
            { '\u0647', new[] { '\uFEE9', '\uFEEA', '\uFEEB', '\uFEEC' } }, // ه heh
            { '\u0648', new[] { '\uFEED', '\uFEEE', '\0'   , '\0'    } }, // و waw
            // Alef maksura is dual-joining in ArabicShaping.txt, but the
            // presentation blocks give it no initial and no medial form - in
            // practice it only ever ends a word - so two forms is the whole
            // truth a font has a glyph for.
            { '\u0649', new[] { '\uFEEF', '\uFEF0', '\0'   , '\0'    } }, // ى alef maksura
            { '\u064A', new[] { '\uFEF1', '\uFEF2', '\uFEF3', '\uFEF4' } }, // ي yeh

            // --- Persian, Urdu and the wider Arabic script ---
            { '\u0671', new[] { '\uFB50', '\uFB51', '\0'   , '\0'    } }, // ٱ alef wasla
            { '\u067B', new[] { '\uFB52', '\uFB53', '\uFB54', '\uFB55' } }, // ٻ beeh
            { '\u067E', new[] { '\uFB56', '\uFB57', '\uFB58', '\uFB59' } }, // پ peh    پ
            { '\u0680', new[] { '\uFB5A', '\uFB5B', '\uFB5C', '\uFB5D' } }, // ڀ beheh
            { '\u067A', new[] { '\uFB5E', '\uFB5F', '\uFB60', '\uFB61' } }, // ٺ tteheh
            { '\u067F', new[] { '\uFB62', '\uFB63', '\uFB64', '\uFB65' } }, // ٿ teheh
            { '\u0679', new[] { '\uFB66', '\uFB67', '\uFB68', '\uFB69' } }, // ٹ tteh
            { '\u06A4', new[] { '\uFB6A', '\uFB6B', '\uFB6C', '\uFB6D' } }, // ڤ veh
            { '\u06A6', new[] { '\uFB6E', '\uFB6F', '\uFB70', '\uFB71' } }, // ڦ peheh
            { '\u0684', new[] { '\uFB72', '\uFB73', '\uFB74', '\uFB75' } }, // ڄ dyeh
            { '\u0683', new[] { '\uFB76', '\uFB77', '\uFB78', '\uFB79' } }, // ڃ nyeh
            { '\u0686', new[] { '\uFB7A', '\uFB7B', '\uFB7C', '\uFB7D' } }, // چ tcheh  چ
            { '\u0687', new[] { '\uFB7E', '\uFB7F', '\uFB80', '\uFB81' } }, // ڇ tcheheh
            { '\u068D', new[] { '\uFB82', '\uFB83', '\0'   , '\0'    } }, // ڍ ddahal
            { '\u068C', new[] { '\uFB84', '\uFB85', '\0'   , '\0'    } }, // ڌ dahal
            { '\u068E', new[] { '\uFB86', '\uFB87', '\0'   , '\0'    } }, // ڎ dul
            { '\u0688', new[] { '\uFB88', '\uFB89', '\0'   , '\0'    } }, // ڈ ddal
            { '\u0698', new[] { '\uFB8A', '\uFB8B', '\0'   , '\0'    } }, // ژ jeh    ژ
            { '\u0691', new[] { '\uFB8C', '\uFB8D', '\0'   , '\0'    } }, // ڑ rreh
            { '\u06A9', new[] { '\uFB8E', '\uFB8F', '\uFB90', '\uFB91' } }, // ک keheh  ک
            { '\u06AF', new[] { '\uFB92', '\uFB93', '\uFB94', '\uFB95' } }, // گ gaf    گ
            { '\u06B3', new[] { '\uFB96', '\uFB97', '\uFB98', '\uFB99' } }, // ڳ gueh
            { '\u06B1', new[] { '\uFB9A', '\uFB9B', '\uFB9C', '\uFB9D' } }, // ڱ ngoeh
            { '\u06BA', new[] { '\uFB9E', '\uFB9F', '\0'   , '\0'    } }, // ں noon ghunna
            { '\u06BB', new[] { '\uFBA0', '\uFBA1', '\uFBA2', '\uFBA3' } }, // ڻ rnoon
            { '\u06C0', new[] { '\uFBA4', '\uFBA5', '\0'   , '\0'    } }, // ۀ heh + yeh above
            { '\u06C1', new[] { '\uFBA6', '\uFBA7', '\uFBA8', '\uFBA9' } }, // ہ heh goal
            { '\u06BE', new[] { '\uFBAA', '\uFBAB', '\uFBAC', '\uFBAD' } }, // ھ heh doachashmee
            { '\u06D2', new[] { '\uFBAE', '\uFBAF', '\0'   , '\0'    } }, // ے yeh barree
            { '\u06D3', new[] { '\uFBB0', '\uFBB1', '\0'   , '\0'    } }, // ۓ yeh barree hamza
            { '\u06AD', new[] { '\uFBD3', '\uFBD4', '\uFBD5', '\uFBD6' } }, // ڭ ng
            { '\u06C7', new[] { '\uFBD7', '\uFBD8', '\0'   , '\0'    } }, // ۇ u
            { '\u06C6', new[] { '\uFBD9', '\uFBDA', '\0'   , '\0'    } }, // ۆ oe
            { '\u06C8', new[] { '\uFBDB', '\uFBDC', '\0'   , '\0'    } }, // ۈ yu
            { '\u06CB', new[] { '\uFBDE', '\uFBDF', '\0'   , '\0'    } }, // ۋ ve
            { '\u06C5', new[] { '\uFBE0', '\uFBE1', '\0'   , '\0'    } }, // ۅ kirghiz oe
            { '\u06C9', new[] { '\uFBE2', '\uFBE3', '\0'   , '\0'    } }, // ۉ kirghiz yu
            { '\u06D0', new[] { '\uFBE4', '\uFBE5', '\uFBE6', '\uFBE7' } }, // ې e
            { '\u06CC', new[] { '\uFBFC', '\uFBFD', '\uFBFE', '\uFBFF' } }, // ی farsi yeh ی
        };

        // ==========================================
        // Lam-alef.
        //
        // Keyed by the alef that follows a lam, each
        // entry holding the isolated and final forms of
        // the resulting single glyph. There is no initial
        // or medial form: the ligature ends in an alef,
        // and an alef never offers a connection to its
        // right.
        // ==========================================
        private static readonly Dictionary<char, char[]> LamAlef = new Dictionary<char, char[]>
        {
            { '\u0622', new[] { '\uFEF5', '\uFEF6' } },       // lam + alef madda
            { '\u0623', new[] { '\uFEF7', '\uFEF8' } },       // lam + alef hamza above
            { '\u0625', new[] { '\uFEF9', '\uFEFA' } },       // lam + alef hamza below
            { '\u0627', new[] { '\uFEFB', '\uFEFC' } },       // lam + alef
        };

        /// <summary>
        /// The joining class of one character. Everything the table does not
        /// know about is either a mark that joins nothing (transparent) or an
        /// ordinary character that joins nothing at all.
        /// </summary>
        public static DirectJoining JoiningOf(char c)
        {
            if (c == Tatweel || c == ZeroWidthJoiner) { return DirectJoining.Causing; }
            if (IsTransparent(c)) { return DirectJoining.Transparent; }

            if (Forms.TryGetValue(c, out char[] forms))
            {
                if (forms[2] != '\0') { return DirectJoining.Dual; }

                // A letter with an isolated form and nothing else joins
                // nothing. U+0621 HAMZA is the one that matters: it is in the
                // table because it has a presentation form, not because it
                // connects, and reading it as right-joining would put the
                // letter before it into an initial shape with nothing to
                // connect to.
                return forms[1] != '\0' ? DirectJoining.Right : DirectJoining.None;
            }
            return DirectJoining.None;
        }

        /// <summary>
        /// True when the text has at least one letter this shaper would change.
        /// Callers use it to skip the whole pass on English, Japanese or any
        /// other string, which is nearly all of them.
        /// </summary>
        public static bool NeedsShaping(string text)
        {
            if (string.IsNullOrEmpty(text)) { return false; }

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ZeroWidthNonJoiner) { return true; }
                if (Forms.ContainsKey(text[i])) { return true; }
            }
            return false;
        }

        // ==========================================
        // Shape
        //
        // One pass, left to right over the LOGICAL
        // string - which is the order the letters were
        // typed in, and the only order in which "what is
        // the letter before this one" has an answer.
        // Reordering for display happens afterwards, in
        // DirectBidi, and would destroy this if it
        // happened first.
        // ==========================================
        /// <summary>
        /// Replaces every Arabic-script letter with the presentation form that
        /// joins correctly to its neighbours. Text with no Arabic script in it
        /// is returned untouched, by reference.
        /// </summary>
        public static string Shape(string text)
        {
            return Shape(text, null, null);
        }

        /// <summary>
        /// The same shaping, with two things a renderer needs and a unit test
        /// does not.
        ///
        /// <paramref name="hasGlyph"/> is asked, per form, whether the font
        /// about to draw this text actually has that glyph. It matters more
        /// than it sounds: the presentation forms are a legacy block, and a
        /// modern face built for a real shaping engine - Vazirmatn, Noto Sans
        /// Arabic, most of what a Persian designer will reach for - carries
        /// the plain letters and its joining rules in OpenType, with nothing
        /// at U+FE70. Shaping into a form such a font has no glyph for turns
        /// readable-but-unjoined text into a row of boxes, which is a worse
        /// answer than the one we started with. When the probe says no, the
        /// letter falls back to its isolated form and then to itself.
        ///
        /// <paramref name="sourceIndices"/> is filled with the index in
        /// <paramref name="text"/> that each output character came from, so a
        /// caller that has to put something back where it was - a rich-text
        /// tag, a caret - can follow the letters through a pass that removes
        /// ZWNJ and turns lam+alef into one glyph.
        /// </summary>
        public static string Shape(string text, System.Func<char, bool> hasGlyph, List<int> sourceIndices)
        {
            if (sourceIndices != null) { sourceIndices.Clear(); }

            if (!NeedsShaping(text))
            {
                FillIdentity(sourceIndices, text);
                return text;
            }

            var output = new StringBuilder(text.Length);

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // The non-joiner's entire job was to be a non-joining
                // character while the forms below were chosen. It has no
                // glyph worth keeping afterwards.
                if (c == ZeroWidthNonJoiner) { continue; }

                if (!Forms.TryGetValue(c, out char[] forms))
                {
                    output.Append(c);
                    if (sourceIndices != null) { sourceIndices.Add(i); }
                    continue;
                }

                bool joinsBefore = Offers(PreviousJoining(text, i));

                // لا, before anything else: two codepoints, one glyph, and
                // treating them separately is the single most recognisable
                // way to get Arabic wrong.
                if (c == Lam
                    && i + 1 < text.Length
                    && LamAlef.TryGetValue(text[i + 1], out char[] ligature))
                {
                    char joined = joinsBefore ? ligature[1] : ligature[0];

                    if (hasGlyph == null || hasGlyph(joined))
                    {
                        output.Append(joined);
                        if (sourceIndices != null) { sourceIndices.Add(i); }
                        i++;
                        continue;
                    }

                    // No ligature glyph in this font. A lam and an alef drawn
                    // in their own joining forms is not the ligature a reader
                    // expects, but it is the word, and it is what a font
                    // without the ligature can draw.
                    char alef = text[i + 1];

                    output.Append(Supported(Pick(forms, joinsBefore, true), forms, c, hasGlyph));
                    if (sourceIndices != null) { sourceIndices.Add(i); }

                    output.Append(Forms.TryGetValue(alef, out char[] alefForms)
                        ? Supported(Pick(alefForms, true, false), alefForms, alef, hasGlyph)
                        : alef);
                    if (sourceIndices != null) { sourceIndices.Add(i + 1); }

                    i++;
                    continue;
                }

                bool linkBefore = joinsBefore && Accepts(JoiningOf(c));
                bool linkAfter = Offers(JoiningOf(c)) && Accepts(NextJoining(text, i));

                output.Append(Supported(Pick(forms, linkBefore, linkAfter), forms, c, hasGlyph));
                if (sourceIndices != null) { sourceIndices.Add(i); }
            }

            return output.ToString();
        }

        // The chosen form, if the font has it; the isolated form, if it has
        // that; the letter as it was stored, which the font demonstrably has
        // because it was drawing it before this file was involved.
        private static char Supported(char form, char[] forms, char original, System.Func<char, bool> hasGlyph)
        {
            if (form == '\0') { return original; }
            if (hasGlyph == null || hasGlyph(form)) { return form; }

            if (forms != null && forms[0] != '\0' && forms[0] != form && hasGlyph(forms[0])) { return forms[0]; }

            return original;
        }

        private static void FillIdentity(List<int> sourceIndices, string text)
        {
            if (sourceIndices == null || string.IsNullOrEmpty(text)) { return; }
            for (int i = 0; i < text.Length; i++) { sourceIndices.Add(i); }
        }

        // ==========================================
        // The four-way choice
        // ==========================================
        private static char Pick(char[] forms, bool linkBefore, bool linkAfter)
        {
            if (linkBefore && linkAfter && forms[3] != '\0') { return forms[3]; }
            if (linkBefore && forms[1] != '\0') { return forms[1]; }
            if (linkAfter && forms[2] != '\0') { return forms[2]; }
            return forms[0];
        }

        /// <summary>Can a letter of this class connect to the letter after it?</summary>
        private static bool Offers(DirectJoining joining)
        {
            return joining == DirectJoining.Dual || joining == DirectJoining.Causing;
        }

        /// <summary>Can a letter of this class accept a connection from before it?</summary>
        private static bool Accepts(DirectJoining joining)
        {
            return joining == DirectJoining.Dual
                || joining == DirectJoining.Right
                || joining == DirectJoining.Causing;
        }

        // Marks are skipped when looking for a neighbour: a fatha sitting over
        // a letter is drawn above the join, not in it, so a word with vowel
        // marks has to join exactly as it does without them.
        private static DirectJoining PreviousJoining(string text, int index)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                DirectJoining joining = JoiningOf(text[i]);
                if (joining != DirectJoining.Transparent) { return joining; }
            }
            return DirectJoining.None;
        }

        private static DirectJoining NextJoining(string text, int index)
        {
            for (int i = index + 1; i < text.Length; i++)
            {
                DirectJoining joining = JoiningOf(text[i]);
                if (joining != DirectJoining.Transparent) { return joining; }
            }
            return DirectJoining.None;
        }

        // ==========================================
        // Transparent characters - the marks that sit
        // above and below the line. Harakat, the Quranic
        // annotation marks, the superscript alef, and
        // the generic combining blocks, so a decomposed
        // character behaves like a composed one.
        // ==========================================
        private static bool IsTransparent(char c)
        {
            int u = c;

            if (u >= 0x0610 && u <= 0x061A) { return true; }   // Arabic honorifics
            if (u >= 0x064B && u <= 0x065F) { return true; }   // harakat
            if (u == 0x0670) { return true; }                  // superscript alef
            if (u >= 0x06D6 && u <= 0x06DC) { return true; }
            if (u >= 0x06DF && u <= 0x06E4) { return true; }
            if (u == 0x06E7 || u == 0x06E8) { return true; }
            if (u >= 0x06EA && u <= 0x06ED) { return true; }
            if (u >= 0x0300 && u <= 0x036F) { return true; }   // combining diacriticals
            if (u >= 0xFE00 && u <= 0xFE0F) { return true; }   // variation selectors
            if (u >= 0xFE20 && u <= 0xFE2F) { return true; }   // combining half marks

            return false;
        }

        /// <summary>
        /// True when the character is one of the presentation forms this
        /// shaper produces. Used as a "has this string already been shaped?"
        /// probe, so a string that travels through two display helpers is not
        /// shaped and reordered twice.
        /// </summary>
        public static bool IsPresentationForm(char c)
        {
            int u = c;
            return (u >= 0xFB50 && u <= 0xFDFF) || (u >= 0xFE70 && u <= 0xFEFF);
        }
    }
}
