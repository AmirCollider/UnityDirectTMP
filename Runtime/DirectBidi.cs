// ==========================================
// DirectBidi
// Right-to-left text, put in the order it is read in.
//
// A string is stored in LOGICAL order - the order the
// characters were typed, first-spoken first. A screen
// draws in VISUAL order, left to right, always.
// For English those are the same order and nobody ever
// has to think about it. For Persian, Arabic and Hebrew
// they are opposite, and something has to do the
// turning.
//
// On a web page the browser does it. In a native app
// the OS text stack does it. In Unity's IMGUI - which
// is what every Editor window in Unity, including this
// package's own, is drawn with - nothing does it. The
// characters go to the font in the order they are
// stored, so "کاتالوگ فونت" arrives on screen reading
// backwards. That is not a font problem and no font
// fixes it; the screenshot of a Persian editor window
// with every line mirrored is this, and only this.
//
// So this file is the Unicode Bidirectional Algorithm
// (UAX #9), implemented over one line of text at a
// time. It is not the whole specification - the
// explicit embedding controls (RLE/LRO/PDF and the
// isolates) are folded away rather than nested, because
// no user interface string in this package or anybody
// else's has ever contained one. Everything that a real
// interface DOES contain is here and is the hard part
// anyway:
//
//   * A Latin run inside a Persian sentence keeps
//     reading left to right. "فایل فونت رو بده به
//     TextMeshPro" has to show TextMeshPro forwards,
//     not sdrawkcab.
//
//   * Numbers. "۱۲ فونت" and "12 fonts" both put the
//     digits in ascending order regardless of which
//     direction the sentence runs, and a number
//     attached to Arabic text behaves differently from
//     one attached to Latin text - which is why the
//     algorithm has two kinds of digit.
//
//   * Punctuation between them. A full stop at the end
//     of a Persian sentence belongs on the LEFT of the
//     line. A comma between two Latin words does not
//     move at all. Neither is a special case; both fall
//     out of the neutral-resolution rules below.
//
//   * Brackets. "(فارسی)" opens on the right. The
//     characters are swapped for their mirror images,
//     because a font has no idea which way round the
//     sentence is.
//
// Pure C#: no Unity types, no allocation per character,
// and every rule reachable from a unit test.
// ==========================================
using System.Collections.Generic;
using System.Globalization;

namespace UnityDirectTMP
{
    /// <summary>
    /// The Unicode bidirectional character classes this implementation
    /// distinguishes. Named as UAX #9 names them.
    /// </summary>
    internal enum DirectBidiClass
    {
        /// <summary>Strong left-to-right. Latin, Greek, CJK, Kana, Thai…</summary>
        L,

        /// <summary>Strong right-to-left. Hebrew, N'Ko, Thaana.</summary>
        R,

        /// <summary>Strong right-to-left, Arabic. Different from R because it changes how digits behave.</summary>
        AL,

        /// <summary>European number. ASCII digits - and, counter-intuitively, Persian ones.</summary>
        EN,

        /// <summary>European number separator. Plus and minus.</summary>
        ES,

        /// <summary>European number terminator. Currency signs, degree, percent.</summary>
        ET,

        /// <summary>Arabic number. The Arabic-Indic digits ٠١٢.</summary>
        AN,

        /// <summary>Common number separator. Comma, full stop, colon, slash.</summary>
        CS,

        /// <summary>Non-spacing mark. Takes the direction of whatever it sits on.</summary>
        NSM,

        /// <summary>Boundary neutral. Removed before anything else runs.</summary>
        BN,

        /// <summary>Paragraph separator.</summary>
        B,

        /// <summary>Segment separator. Tab.</summary>
        S,

        /// <summary>Whitespace.</summary>
        WS,

        /// <summary>Other neutral. Every punctuation mark and symbol not listed above.</summary>
        ON
    }

    /// <summary>
    /// Reorders logically-stored text into the order it is read on screen.
    /// </summary>
    public static class DirectBidi
    {
        /// <summary>Left-to-right mark.</summary>
        public const char LeftToRightMark = '\u200E';

        /// <summary>Right-to-left mark.</summary>
        public const char RightToLeftMark = '\u200F';

        /// <summary>Paragraph direction is decided by the text's own first strong character.</summary>
        public const int AutoDirection = -1;

        /// <summary>Force a left-to-right paragraph.</summary>
        public const int LeftToRight = 0;

        /// <summary>Force a right-to-left paragraph.</summary>
        public const int RightToLeft = 1;

        // ==========================================
        // Detection
        // ==========================================

        /// <summary>
        /// True when the text contains at least one strongly right-to-left
        /// character. Text without one needs no reordering at all, which is
        /// almost every string a program ever draws - so this check is what
        /// keeps the cost of supporting Persian at zero for everybody else.
        /// </summary>
        public static bool ContainsRightToLeft(string text)
        {
            if (string.IsNullOrEmpty(text)) { return false; }

            for (int i = 0; i < text.Length; i++)
            {
                DirectBidiClass type = Classify(text[i]);
                if (type == DirectBidiClass.R || type == DirectBidiClass.AL) { return true; }
            }
            return false;
        }

        /// <summary>
        /// The direction a paragraph reads in, from its first strong character
        /// (UAX #9 rules P2 and P3). Text with no strong character at all -
        /// "1.0.0", "· 42 ·" - reads left to right.
        /// </summary>
        public static bool IsRightToLeftParagraph(string text)
        {
            if (string.IsNullOrEmpty(text)) { return false; }

            for (int i = 0; i < text.Length; i++)
            {
                switch (Classify(text[i]))
                {
                    case DirectBidiClass.L: return false;
                    case DirectBidiClass.R:
                    case DirectBidiClass.AL: return true;
                }
            }
            return false;
        }

        // ==========================================
        // Reorder
        // ==========================================

        /// <summary>
        /// Turns one line of logically-ordered text into visual order, with
        /// the paragraph direction taken from the text itself.
        /// </summary>
        public static string Reorder(string text)
        {
            return Reorder(text, AutoDirection);
        }

        /// <summary>
        /// Turns one line of logically-ordered text into visual order.
        /// <paramref name="paragraphDirection"/> is <see cref="AutoDirection"/>,
        /// <see cref="LeftToRight"/> or <see cref="RightToLeft"/>.
        /// </summary>
        public static string Reorder(string text, int paragraphDirection)
        {
            return Reorder(text, paragraphDirection, null);
        }

        /// <summary>
        /// The same reordering, filling <paramref name="sourceIndices"/> with
        /// the index in <paramref name="text"/> that each character of the
        /// result came from.
        ///
        /// Reordering is a permutation, and a caller that has to keep
        /// something attached to a character through it - a rich-text tag
        /// that has to still wrap the same word once the word has moved to
        /// the other end of the line - needs the permutation, not just its
        /// result. Characters the algorithm removes outright (rule X9's
        /// boundary neutrals) simply have no entry.
        /// </summary>
        public static string Reorder(string text, int paragraphDirection, List<int> sourceIndices)
        {
            if (sourceIndices != null) { sourceIndices.Clear(); }
            if (string.IsNullOrEmpty(text)) { return text; }

            if (paragraphDirection != RightToLeft && !ContainsRightToLeft(text))
            {
                if (sourceIndices != null)
                {
                    for (int i = 0; i < text.Length; i++) { sourceIndices.Add(i); }
                }
                return text;
            }

            // Newlines are paragraph boundaries, and a paragraph is the unit
            // the algorithm works on. Running the whole label through as one
            // string would reorder its lines against each other.
            if (text.IndexOf('\n') >= 0)
            {
                var joined = new System.Text.StringBuilder(text.Length);
                int start = 0;

                while (true)
                {
                    int newline = text.IndexOf('\n', start);
                    int end = newline < 0 ? text.Length : newline;

                    joined.Append(ReorderLine(text.Substring(start, end - start), paragraphDirection, sourceIndices, start));

                    if (newline < 0) { break; }

                    joined.Append('\n');
                    if (sourceIndices != null) { sourceIndices.Add(newline); }
                    start = newline + 1;
                }

                return joined.ToString();
            }

            return ReorderLine(text, paragraphDirection, sourceIndices, 0);
        }

        private static string ReorderLine(string text, int paragraphDirection, List<int> sourceIndices, int offset)
        {
            if (string.IsNullOrEmpty(text)) { return text; }

            // --- X9: boundary neutrals are removed outright. ---
            var chars = new List<char>(text.Length);
            var kept = sourceIndices == null ? null : new List<int>(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                if (Classify(text[i]) == DirectBidiClass.BN) { continue; }
                chars.Add(text[i]);
                if (kept != null) { kept.Add(offset + i); }
            }
            if (chars.Count == 0) { return string.Empty; }

            int count = chars.Count;
            var original = new DirectBidiClass[count];
            var types = new DirectBidiClass[count];
            for (int i = 0; i < count; i++)
            {
                original[i] = Classify(chars[i]);
                types[i] = original[i];
            }

            // --- P2 / P3 ---
            int paragraphLevel;
            if (paragraphDirection == LeftToRight || paragraphDirection == RightToLeft)
            {
                paragraphLevel = paragraphDirection;
            }
            else
            {
                paragraphLevel = 0;
                for (int i = 0; i < count; i++)
                {
                    if (types[i] == DirectBidiClass.L) { break; }
                    if (types[i] == DirectBidiClass.R || types[i] == DirectBidiClass.AL)
                    {
                        paragraphLevel = 1;
                        break;
                    }
                }
            }

            // With the explicit controls folded away there is exactly one
            // level run, and the "start of sequence" and "end of sequence"
            // directions either side of it are the paragraph's own.
            DirectBidiClass boundary = paragraphLevel == 1 ? DirectBidiClass.R : DirectBidiClass.L;

            ResolveWeakTypes(types, boundary);
            ResolveNeutralTypes(types, boundary);

            var levels = new int[count];
            ResolveImplicitLevels(types, levels, paragraphLevel);

            ApplyLineBreakingLevels(original, levels, paragraphLevel);
            MirrorInPlace(chars, levels);

            // L2 is a permutation, so it is applied to an array of positions
            // rather than to the characters themselves. The characters fall
            // out of it, and so does the map back to where each one started.
            var order = new int[count];
            for (int i = 0; i < count; i++) { order[i] = i; }
            ReverseRuns(order, levels);

            var visual = new char[count];
            for (int i = 0; i < count; i++)
            {
                visual[i] = chars[order[i]];
                if (kept != null) { sourceIndices.Add(kept[order[i]]); }
            }

            return new string(visual);
        }

        // ==========================================
        // W1 - W7: the weak types.
        //
        // These are the rules that make numbers behave.
        // Every one of them looks only at what came
        // before, which is why they run in a single
        // forward pass with two pieces of memory.
        // ==========================================
        private static void ResolveWeakTypes(DirectBidiClass[] types, DirectBidiClass boundary)
        {
            int count = types.Length;

            // W1: a non-spacing mark takes the type of the character it sits
            // on. A fatha over a beh is Arabic; a fatha at the start of a
            // line is whatever the line is.
            DirectBidiClass previous = boundary;
            for (int i = 0; i < count; i++)
            {
                if (types[i] == DirectBidiClass.NSM) { types[i] = previous; }
                else { previous = types[i]; }
            }

            // W2: a European digit becomes an Arabic one when the last strong
            // type before it was Arabic. This is the rule that puts "۱۲" and
            // "12" on opposite sides of the same Persian sentence.
            DirectBidiClass lastStrong = boundary;
            for (int i = 0; i < count; i++)
            {
                switch (types[i])
                {
                    case DirectBidiClass.L:
                    case DirectBidiClass.R:
                    case DirectBidiClass.AL:
                        lastStrong = types[i];
                        break;

                    case DirectBidiClass.EN:
                        if (lastStrong == DirectBidiClass.AL) { types[i] = DirectBidiClass.AN; }
                        break;
                }
            }

            // W3: Arabic letters are plain right-to-left from here on. Their
            // only remaining difference was the rule above.
            for (int i = 0; i < count; i++)
            {
                if (types[i] == DirectBidiClass.AL) { types[i] = DirectBidiClass.R; }
            }

            // W4: a single separator between two numbers of the same kind
            // joins them. "1,000" and "٢٠٢٥/٠٣" stay whole.
            for (int i = 1; i < count - 1; i++)
            {
                if (types[i] == DirectBidiClass.ES
                    && types[i - 1] == DirectBidiClass.EN
                    && types[i + 1] == DirectBidiClass.EN)
                {
                    types[i] = DirectBidiClass.EN;
                }
                else if (types[i] == DirectBidiClass.CS
                    && types[i - 1] == types[i + 1]
                    && (types[i - 1] == DirectBidiClass.EN || types[i - 1] == DirectBidiClass.AN))
                {
                    types[i] = types[i - 1];
                }
            }

            // W5: a run of terminators next to a European number joins it.
            // "$40", "40%", "40°C".
            for (int i = 0; i < count; i++)
            {
                if (types[i] != DirectBidiClass.ET) { continue; }

                int end = i;
                while (end < count && types[end] == DirectBidiClass.ET) { end++; }

                bool touchesNumber = (i > 0 && types[i - 1] == DirectBidiClass.EN)
                                  || (end < count && types[end] == DirectBidiClass.EN);

                if (touchesNumber)
                {
                    for (int j = i; j < end; j++) { types[j] = DirectBidiClass.EN; }
                }
                i = end - 1;
            }

            // W6: separators and terminators that did not join a number are
            // ordinary punctuation.
            for (int i = 0; i < count; i++)
            {
                if (types[i] == DirectBidiClass.ES
                    || types[i] == DirectBidiClass.ET
                    || types[i] == DirectBidiClass.CS)
                {
                    types[i] = DirectBidiClass.ON;
                }
            }

            // W7: a European number in Latin text is just more Latin text.
            lastStrong = boundary;
            for (int i = 0; i < count; i++)
            {
                switch (types[i])
                {
                    case DirectBidiClass.L:
                    case DirectBidiClass.R:
                        lastStrong = types[i];
                        break;

                    case DirectBidiClass.EN:
                        if (lastStrong == DirectBidiClass.L) { types[i] = DirectBidiClass.L; }
                        break;
                }
            }
        }

        // ==========================================
        // N1 - N2: the neutrals.
        //
        // Spaces, punctuation and symbols have no
        // direction of their own, so they take one from
        // their surroundings: the same direction on both
        // sides wins, and a disagreement falls back to
        // the paragraph. This is the rule that puts the
        // full stop of a Persian sentence on the left
        // and leaves the comma inside "a, b" alone.
        // ==========================================
        private static void ResolveNeutralTypes(DirectBidiClass[] types, DirectBidiClass boundary)
        {
            int count = types.Length;

            for (int i = 0; i < count; i++)
            {
                if (!IsNeutral(types[i])) { continue; }

                int end = i;
                while (end < count && IsNeutral(types[end])) { end++; }

                DirectBidiClass before = i > 0 ? DirectionOf(types[i - 1]) : boundary;
                DirectBidiClass after = end < count ? DirectionOf(types[end]) : boundary;

                DirectBidiClass resolved = before == after ? before : boundary;
                for (int j = i; j < end; j++) { types[j] = resolved; }

                i = end - 1;
            }
        }

        private static bool IsNeutral(DirectBidiClass type)
        {
            return type == DirectBidiClass.B
                || type == DirectBidiClass.S
                || type == DirectBidiClass.WS
                || type == DirectBidiClass.ON;
        }

        // For the purpose of resolving a neutral, a number counts as
        // right-to-left: a digit sitting between two Arabic words does not
        // turn the space beside it into Latin.
        private static DirectBidiClass DirectionOf(DirectBidiClass type)
        {
            switch (type)
            {
                case DirectBidiClass.L: return DirectBidiClass.L;
                case DirectBidiClass.R:
                case DirectBidiClass.EN:
                case DirectBidiClass.AN: return DirectBidiClass.R;
                default: return type;
            }
        }

        // ==========================================
        // I1 / I2: from types to embedding levels.
        //
        // Even levels read left to right, odd levels
        // right to left. Everything that disagrees with
        // the level it is sitting in gets pushed one (or
        // two) levels deeper, and the reversal pass at
        // the end turns those depths into an order.
        // ==========================================
        private static void ResolveImplicitLevels(DirectBidiClass[] types, int[] levels, int paragraphLevel)
        {
            for (int i = 0; i < types.Length; i++)
            {
                int level = paragraphLevel;

                if ((paragraphLevel & 1) == 0)
                {
                    // Left-to-right paragraph.
                    if (types[i] == DirectBidiClass.R) { level += 1; }
                    else if (types[i] == DirectBidiClass.AN || types[i] == DirectBidiClass.EN) { level += 2; }
                }
                else
                {
                    // Right-to-left paragraph: Latin and both kinds of number
                    // are the exceptions that run the other way.
                    if (types[i] == DirectBidiClass.L
                        || types[i] == DirectBidiClass.AN
                        || types[i] == DirectBidiClass.EN)
                    {
                        level += 1;
                    }
                }

                levels[i] = level;
            }
        }

        // ==========================================
        // L1: whitespace at the end of a line goes back
        // to the paragraph's own direction.
        //
        // Without this, the trailing space of a Persian
        // line is dragged to the left-hand end and the
        // line looks indented by one space for no
        // reason. Note that it reads the ORIGINAL types,
        // not the resolved ones - a space that became
        // "R" three rules ago is still a space.
        // ==========================================
        private static void ApplyLineBreakingLevels(DirectBidiClass[] original, int[] levels, int paragraphLevel)
        {
            bool trailing = true;

            for (int i = levels.Length - 1; i >= 0; i--)
            {
                DirectBidiClass type = original[i];

                if (type == DirectBidiClass.B || type == DirectBidiClass.S)
                {
                    levels[i] = paragraphLevel;
                    trailing = true;
                }
                else if (trailing && type == DirectBidiClass.WS)
                {
                    levels[i] = paragraphLevel;
                }
                else
                {
                    trailing = false;
                }
            }
        }

        // ==========================================
        // L2: reverse, deepest first.
        //
        // Every contiguous run at or above each level,
        // from the highest level down to the lowest odd
        // one, is reversed in place. Two lines of code
        // and the entire reason the output reads
        // correctly.
        // ==========================================
        private static void ReverseRuns(int[] order, int[] levels)
        {
            int highest = 0;
            int lowestOdd = int.MaxValue;

            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] > highest) { highest = levels[i]; }
                if ((levels[i] & 1) == 1 && levels[i] < lowestOdd) { lowestOdd = levels[i]; }
            }

            if (lowestOdd == int.MaxValue) { return; }

            for (int level = highest; level >= lowestOdd; level--)
            {
                for (int i = 0; i < levels.Length; i++)
                {
                    if (levels[i] < level) { continue; }

                    int start = i;
                    while (i < levels.Length && levels[i] >= level) { i++; }
                    Reverse(order, start, i - 1);
                }
            }
        }

        private static void Reverse(int[] order, int start, int end)
        {
            while (start < end)
            {
                int swap = order[start];
                order[start] = order[end];
                order[end] = swap;
                start++;
                end--;
            }
        }

        // ==========================================
        // L4: mirroring.
        //
        // An opening bracket in right-to-left text is
        // drawn as a closing one. The font cannot know
        // which way the sentence runs, so the character
        // itself is swapped for its mirror image.
        // ==========================================
        private static readonly Dictionary<char, char> Mirrors = new Dictionary<char, char>
        {
            { '(', ')' }, { ')', '(' },
            { '[', ']' }, { ']', '[' },
            { '{', '}' }, { '}', '{' },
            { '<', '>' }, { '>', '<' },
            { '\u00AB', '\u00BB' }, { '\u00BB', '\u00AB' },   // guillemets
            { '\u2039', '\u203A' }, { '\u203A', '\u2039' },   // single guillemets
            { '\u2045', '\u2046' }, { '\u2046', '\u2045' },   // square brackets with quill
            { '\u2264', '\u2265' }, { '\u2265', '\u2264' },   // less/greater than or equal
            { '\u27E8', '\u27E9' }, { '\u27E9', '\u27E8' },   // mathematical angle brackets
            { '\u3008', '\u3009' }, { '\u3009', '\u3008' },   // CJK angle brackets
            { '\u300A', '\u300B' }, { '\u300B', '\u300A' },
            { '\u300C', '\u300D' }, { '\u300D', '\u300C' },
            { '\uFF08', '\uFF09' }, { '\uFF09', '\uFF08' }    // fullwidth parentheses
        };

        private static void MirrorInPlace(List<char> chars, int[] levels)
        {
            for (int i = 0; i < chars.Count; i++)
            {
                if ((levels[i] & 1) == 0) { continue; }
                if (Mirrors.TryGetValue(chars[i], out char mirrored)) { chars[i] = mirrored; }
            }
        }

        // ==========================================
        // Classification.
        //
        // The explicit ranges first, because they are
        // the ones Unicode's general categories get
        // wrong for this purpose - a Persian digit is a
        // DecimalDigitNumber and a EUROPEAN number, and
        // an Arabic-Indic digit is the same category and
        // an ARABIC one. Everything the ranges do not
        // name falls through to the category, which is
        // right for the ninety-odd scripts that are
        // simply left-to-right.
        // ==========================================
        private static DirectBidiClass Classify(char c)
        {
            int u = c;

            // --- ASCII, which is most of every string ---
            if (u < 0x80)
            {
                if ((u >= 'a' && u <= 'z') || (u >= 'A' && u <= 'Z')) { return DirectBidiClass.L; }
                if (u >= '0' && u <= '9') { return DirectBidiClass.EN; }

                switch (u)
                {
                    case '\n':
                    case '\r': return DirectBidiClass.B;
                    case '\t':
                    case 0x0B:
                    case 0x1F: return DirectBidiClass.S;
                    case '\f':
                    case ' ': return DirectBidiClass.WS;
                    case '+':
                    case '-': return DirectBidiClass.ES;
                    case '#':
                    case '$':
                    case '%': return DirectBidiClass.ET;
                    case ',':
                    case '.':
                    case '/':
                    case ':': return DirectBidiClass.CS;
                }

                return u < 0x20 || u == 0x7F ? DirectBidiClass.BN : DirectBidiClass.ON;
            }

            // --- the marks and controls that steer the algorithm ---
            switch (u)
            {
                case 0x200E: return DirectBidiClass.L;    // LRM
                case 0x200F: return DirectBidiClass.R;    // RLM
                case 0x061C: return DirectBidiClass.AL;   // ALM
                case 0x00A0: return DirectBidiClass.CS;   // no-break space
                case 0x2044: return DirectBidiClass.CS;   // fraction slash
                case 0x202F: return DirectBidiClass.CS;   // narrow no-break space
                case 0x2212: return DirectBidiClass.ES;   // minus sign
                case 0x00AD: return DirectBidiClass.BN;   // soft hyphen
                case 0x2028: return DirectBidiClass.WS;   // line separator
                case 0x2029:
                case 0x0085: return DirectBidiClass.B;
                case 0x1680:
                case 0x205F:
                case 0x3000: return DirectBidiClass.WS;

                // Fullwidth and small forms of the punctuation that already
                // has a class above. A CJK project's Persian labels are full
                // of these and they behave exactly as their ASCII twins do.
                case 0xFF0B:
                case 0xFF0D:
                case 0xFE62:
                case 0xFE63:
                case 0xFB29: return DirectBidiClass.ES;
                case 0xFF0C:
                case 0xFF0E:
                case 0xFF0F:
                case 0xFF1A:
                case 0xFE50:
                case 0xFE52:
                case 0xFE55: return DirectBidiClass.CS;
            }

            // Zero-width controls, the explicit embedding and isolate
            // controls, and the byte-order mark. Folding the embeddings into
            // BN is the one simplification this implementation makes.
            if ((u >= 0x200B && u <= 0x200D)
                || (u >= 0x202A && u <= 0x202E)
                || (u >= 0x2060 && u <= 0x2064)
                || (u >= 0x2066 && u <= 0x2069)
                || u == 0xFEFF
                || (u >= 0x0080 && u <= 0x0084)
                || (u >= 0x0086 && u <= 0x009F))
            {
                return DirectBidiClass.BN;
            }

            // --- right-to-left, Arabic ---
            if (u >= 0x0600 && u <= 0x0605) { return DirectBidiClass.AN; }
            if (u == 0x0608 || u == 0x060B || u == 0x060D) { return DirectBidiClass.AL; }
            if (u == 0x0609 || u == 0x060A || u == 0x066A) { return DirectBidiClass.ET; }
            if (u == 0x060C) { return DirectBidiClass.CS; }
            if (u == 0x0606 || u == 0x0607 || u == 0x060E || u == 0x060F) { return DirectBidiClass.ON; }
            if (u >= 0x0610 && u <= 0x061A) { return DirectBidiClass.NSM; }
            if (u >= 0x064B && u <= 0x065F) { return DirectBidiClass.NSM; }
            if (u == 0x0670) { return DirectBidiClass.NSM; }
            if (u >= 0x06D6 && u <= 0x06DC) { return DirectBidiClass.NSM; }
            if (u >= 0x06DF && u <= 0x06E4) { return DirectBidiClass.NSM; }
            if (u == 0x06E7 || u == 0x06E8) { return DirectBidiClass.NSM; }
            if (u >= 0x06EA && u <= 0x06ED) { return DirectBidiClass.NSM; }
            if (u >= 0x0660 && u <= 0x0669) { return DirectBidiClass.AN; }
            if (u == 0x066B || u == 0x066C || u == 0x06DD) { return DirectBidiClass.AN; }
            if (u >= 0x06F0 && u <= 0x06F9) { return DirectBidiClass.EN; }   // Persian digits
            if (u == 0x06DE || u == 0x06E9) { return DirectBidiClass.ON; }
            if (u >= 0x061B && u <= 0x064A) { return DirectBidiClass.AL; }
            if (u >= 0x066D && u <= 0x066F) { return DirectBidiClass.AL; }
            if (u >= 0x0671 && u <= 0x06D5) { return DirectBidiClass.AL; }
            if (u == 0x06E5 || u == 0x06E6 || u == 0x06EE || u == 0x06EF) { return DirectBidiClass.AL; }
            if (u >= 0x06FA && u <= 0x06FF) { return DirectBidiClass.AL; }
            if (u >= 0x0700 && u <= 0x074F) { return CategoryFallback(c, DirectBidiClass.AL); }  // Syriac
            if (u >= 0x0750 && u <= 0x077F) { return DirectBidiClass.AL; }
            if (u >= 0x0780 && u <= 0x07BF) { return CategoryFallback(c, DirectBidiClass.AL); }  // Thaana
            if (u >= 0x0860 && u <= 0x08FF) { return CategoryFallback(c, DirectBidiClass.AL); }

            // The two ornate parentheses are the only characters in the
            // Arabic presentation blocks that are neutral rather than Arabic -
            // and they are a mirrored pair, so getting them wrong shows.
            if (u == 0xFD3E || u == 0xFD3F) { return DirectBidiClass.ON; }
            if (u >= 0xFB50 && u <= 0xFDFF) { return DirectBidiClass.AL; }
            if (u >= 0xFE70 && u <= 0xFEFC) { return DirectBidiClass.AL; }

            // --- right-to-left, everything else ---
            // Hebrew's own punctuation is strongly right-to-left rather than
            // neutral: maqaf, sof pasuq, geresh and gershayim all belong to
            // the word they are attached to.
            if (u == 0x05BE || u == 0x05C0 || u == 0x05C3 || u == 0x05C6
                || u == 0x05EF || u == 0x05F3 || u == 0x05F4) { return DirectBidiClass.R; }
            if (u >= 0x0590 && u <= 0x05FF) { return CategoryFallback(c, DirectBidiClass.R); }
            if (u >= 0x07C0 && u <= 0x085F) { return CategoryFallback(c, DirectBidiClass.R); }
            if (u >= 0xFB1D && u <= 0xFB4F) { return CategoryFallback(c, DirectBidiClass.R); }

            // --- the number-adjacent symbols ---
            if (u >= 0x00A2 && u <= 0x00A5) { return DirectBidiClass.ET; }
            if (u == 0x00B0 || u == 0x00B1) { return DirectBidiClass.ET; }
            if (u >= 0x2030 && u <= 0x2034) { return DirectBidiClass.ET; }
            if (u >= 0x20A0 && u <= 0x20CF) { return DirectBidiClass.ET; }
            if (u == 0x00B2 || u == 0x00B3 || u == 0x00B9) { return DirectBidiClass.EN; }
            if (u == 0x2070 || (u >= 0x2074 && u <= 0x2079)) { return DirectBidiClass.EN; }
            if (u >= 0x2080 && u <= 0x2089) { return DirectBidiClass.EN; }
            if (u == 0x207A || u == 0x207B || u == 0x208A || u == 0x208B) { return DirectBidiClass.ES; }
            if (u >= 0x2000 && u <= 0x200A) { return DirectBidiClass.WS; }

            return CategoryFallback(c, DirectBidiClass.L);
        }

        // ==========================================
        // The general category answers the rest.
        //
        // The default matters more than it looks. It is
        // the STRONG direction, not "neutral", because
        // the ranges above have already named every
        // right-to-left block and every kind of
        // punctuation gets its own case below - so what
        // is left over is a letter. Including a letter
        // this runtime's Unicode tables are too old to
        // have heard of, which is exactly the case a
        // neutral default gets wrong: an unknown letter
        // dropped into a Persian sentence would be
        // reordered along with the Persian instead of
        // reading forwards.
        // ==========================================
        private static DirectBidiClass CategoryFallback(char c, DirectBidiClass strong)
        {
            switch (CharUnicodeInfo.GetUnicodeCategory(c))
            {
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.EnclosingMark:
                    return DirectBidiClass.NSM;

                case UnicodeCategory.Format:
                    return DirectBidiClass.BN;

                case UnicodeCategory.SpaceSeparator:
                    return DirectBidiClass.WS;

                case UnicodeCategory.LineSeparator:
                    return DirectBidiClass.WS;

                case UnicodeCategory.ParagraphSeparator:
                    return DirectBidiClass.B;

                case UnicodeCategory.Control:
                    return DirectBidiClass.BN;

                // Punctuation and symbols have no direction of their own -
                // they take one from their neighbours in N1/N2.
                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.DashPunctuation:
                case UnicodeCategory.OpenPunctuation:
                case UnicodeCategory.ClosePunctuation:
                case UnicodeCategory.InitialQuotePunctuation:
                case UnicodeCategory.FinalQuotePunctuation:
                case UnicodeCategory.OtherPunctuation:
                case UnicodeCategory.MathSymbol:
                case UnicodeCategory.CurrencySymbol:
                case UnicodeCategory.ModifierSymbol:
                case UnicodeCategory.OtherSymbol:
                    return DirectBidiClass.ON;

                case UnicodeCategory.Surrogate:
                    // Half of an astral character - an emoji, or a rare CJK
                    // ideograph. Neither is right-to-left, and both halves
                    // have to agree or the pair is torn apart by a reversal.
                    return DirectBidiClass.L;

                default:
                    // Letters, spacing marks, numbers - and anything this
                    // runtime has no category for at all.
                    return strong;
            }
        }
    }
}
