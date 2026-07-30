// ==========================================
// DirectRichText
// Shaping and reordering for text that has markup in
// it - which, in TextMeshPro, is all of it.
//
// DirectDisplayText prepares a plain string. Hand the
// same string to a TMP label and it is no longer plain:
//
//   "سلام <b>دنیا</b>"
//
// Reordering that as one run turns "<b>" into ">b<",
// mirrors the brackets, and hands TextMeshPro a tag it
// cannot parse - so the label draws the markup instead
// of applying it. Skipping the tags instead and
// preparing what is between them is no better: each
// fragment comes out correct on its own and the
// fragments stay in the order they were stored, so the
// sentence still reads backwards.
//
// The tags have to move WITH the words they wrap. This
// file does that, in three steps:
//
//   1. the tags are lifted out, each remembering the
//      character it stood before;
//   2. what is left - one continuous run of text, so
//      letters join across a <b> exactly as they do
//      without it - is shaped and reordered, keeping
//      the map of where every character went;
//   3. each tag is put back at the visual edge of the
//      span it covers: the opening one at the left end
//      of its span, the closing one at the right,
//      swapping over automatically when the span turned
//      around.
//
// LINES
// -----
// A line break - "\n" or <br> - ends a paragraph, and
// each paragraph is prepared on its own. That is not a
// nicety: reordering two lines as one string swaps
// them, and the second line of a Persian address ends
// up above the first.
//
// Soft wrapping is the same problem and cannot be
// solved here, because nothing in this file knows how
// wide a glyph is. PrepareAtBreaks takes the break
// positions from whoever does - DirectText asks
// TextMeshPro, which measured them - and prepares each
// line between them.
//
// Pure C#: no Unity types, so every rule here is
// reachable from a unit test.
// ==========================================
using System;
using System.Collections.Generic;
using System.Text;

namespace UnityDirectTMP
{
    /// <summary>
    /// Arabic-script shaping and bidirectional reordering over text that
    /// contains TextMeshPro rich-text tags, with the tags kept intact and
    /// moved to stay around the words they wrap.
    /// </summary>
    public static class DirectRichText
    {
        // TextMeshPro stops looking for the closing '>' after 128 characters,
        // so a '<' with no '>' within that distance is not a tag to it, and
        // must not be one here either.
        private const int MaxTagLength = 128;

        private enum TagKind
        {
            Open,
            Close,
            Void
        }

        private struct Tag
        {
            public string Text;
            public string Name;
            public TagKind Kind;

            /// <summary>Index in the tag-free text this tag stood in front of.</summary>
            public int Plain;

            /// <summary>Index of the matching open/close tag, or -1.</summary>
            public int Partner;
        }

        private struct Insertion
        {
            public int Index;
            public int Rank;
            public int Order;
            public string Text;
        }

        // ==========================================
        // The public three
        // ==========================================

        /// <summary>
        /// True when this text contains anything written right to left, and so
        /// would be changed by <see cref="Prepare(string,int,Func{char,bool})"/>.
        /// </summary>
        public static bool NeedsPreparing(string text)
        {
            return DirectBidi.ContainsRightToLeft(text);
        }

        /// <summary>
        /// Joins the letters up and leaves the order alone, with rich-text
        /// tags kept where they were.
        ///
        /// This is half a preparation, and it exists for the caller that has
        /// to measure before it can finish: shaped text is the width the
        /// finished text will be, in the order line breaking has to be decided
        /// in.
        /// </summary>
        public static string Shape(string text, Func<char, bool> hasGlyph = null)
        {
            if (string.IsNullOrEmpty(text)) { return text; }
            if (!DirectArabicShaper.NeedsShaping(text)) { return text; }

            var tags = new List<Tag>();
            string logical = Split(text, tags);

            if (tags.Count == 0) { return DirectArabicShaper.Shape(text, hasGlyph, null); }

            var shapeMap = new List<int>();
            string shaped = DirectArabicShaper.Shape(logical, hasGlyph, shapeMap);
            int[] plainToShaped = MapForward(logical.Length, shaped.Length, shapeMap);

            var insertions = new List<Insertion>(tags.Count);
            for (int i = 0; i < tags.Count; i++)
            {
                insertions.Add(new Insertion
                {
                    Index = plainToShaped[tags[i].Plain],
                    Rank = 0,
                    Order = i,
                    Text = tags[i].Text
                });
            }

            return Weave(shaped, insertions);
        }

        /// <summary>
        /// Joins the letters up and puts the text in reading order, keeping
        /// rich-text tags around the words they wrap and preparing each line
        /// of a multi-line string on its own.
        ///
        /// Text with nothing right-to-left in it is returned unchanged, by
        /// reference, unless the paragraph direction is forced.
        /// </summary>
        public static string Prepare(
            string text,
            int paragraphDirection = DirectBidi.AutoDirection,
            Func<char, bool> hasGlyph = null)
        {
            if (string.IsNullOrEmpty(text)) { return text; }
            if (paragraphDirection != DirectBidi.RightToLeft && !NeedsPreparing(text)) { return text; }

            var result = new StringBuilder(text.Length + 8);

            int start = 0;
            int i = 0;
            while (i < text.Length)
            {
                int breakLength = HardBreakAt(text, i);
                if (breakLength <= 0) { i++; continue; }

                result.Append(PrepareParagraph(text.Substring(start, i - start), paragraphDirection, hasGlyph));
                result.Append(text, i, breakLength);

                i += breakLength;
                start = i;
            }

            result.Append(PrepareParagraph(text.Substring(start), paragraphDirection, hasGlyph));
            return result.ToString();
        }

        /// <summary>
        /// Prepares text that has already been <see cref="Shape"/>d, breaking
        /// it into lines at the given positions first.
        ///
        /// This is the answer to wrapping. A renderer that wraps prepared text
        /// gets lines that read bottom to top, because the last word of the
        /// paragraph is the first thing on the first visual line. The breaks
        /// have to be decided while the text is still in reading order, and
        /// then each line reordered on its own - so whoever can measure the
        /// text passes the positions in here.
        ///
        /// <paramref name="breaks"/> are indices into <paramref name="shaped"/>,
        /// each the first character of a new line.
        /// </summary>
        public static string PrepareAtBreaks(
            string shaped,
            IList<int> breaks,
            int paragraphDirection = DirectBidi.AutoDirection,
            Func<char, bool> hasGlyph = null)
        {
            if (string.IsNullOrEmpty(shaped)) { return shaped; }
            if (breaks == null || breaks.Count == 0) { return Prepare(shaped, paragraphDirection, hasGlyph); }

            var result = new StringBuilder(shaped.Length + breaks.Count);

            int start = 0;
            for (int i = 0; i <= breaks.Count; i++)
            {
                int end = i < breaks.Count ? breaks[i] : shaped.Length;
                if (end <= start || end > shaped.Length) { continue; }

                string line = shaped.Substring(start, end - start);
                result.Append(Prepare(line, paragraphDirection, hasGlyph));

                // The break we are about to add is a soft one - the renderer
                // decided it, there is no character for it - unless the line
                // already ends in a break of its own.
                if (i < breaks.Count && HardBreakAtEnd(line) == 0) { result.Append('\n'); }

                start = end;
            }

            return result.ToString();
        }

        // ==========================================
        // One paragraph
        //
        // Everything above splits; this is the part
        // that prepares.
        // ==========================================
        private static string PrepareParagraph(string text, int paragraphDirection, Func<char, bool> hasGlyph)
        {
            if (string.IsNullOrEmpty(text)) { return text; }

            var tags = new List<Tag>();
            string logical = Split(text, tags);

            if (paragraphDirection != DirectBidi.RightToLeft && !DirectBidi.ContainsRightToLeft(logical))
            {
                return text;
            }

            // --- shape, then reorder, keeping both maps ---
            var shapeMap = new List<int>();
            string shaped = DirectArabicShaper.Shape(logical, hasGlyph, shapeMap);
            int[] plainToShaped = MapForward(logical.Length, shaped.Length, shapeMap);

            var visualMap = new List<int>();
            string visual = DirectBidi.Reorder(shaped, paragraphDirection, visualMap);

            if (tags.Count == 0) { return visual; }

            int[] shapedToVisual = MapBack(shaped.Length, visualMap);
            bool rightToLeft = paragraphDirection == DirectBidi.RightToLeft
                || (paragraphDirection == DirectBidi.AutoDirection && DirectBidi.IsRightToLeftParagraph(logical));

            var insertions = new List<Insertion>(tags.Count);
            for (int i = 0; i < tags.Count; i++)
            {
                Tag tag = tags[i];

                int index;
                int rank;

                switch (tag.Kind)
                {
                    case TagKind.Open:
                    {
                        // An opening tag covers everything up to its partner -
                        // or, when the markup runs on past this line, the rest
                        // of the line. Visually that span may have turned
                        // around, so the tag belongs at its LEFT edge whichever
                        // end of the text that now is.
                        int last = tag.Partner >= 0 ? tags[tag.Partner].Plain : logical.Length;
                        if (!SpanBounds(tag.Plain, last, plainToShaped, shapedToVisual, shaped.Length, out int min, out int max))
                        {
                            // Nothing visible on this line to wrap. Anything
                            // this tag turns on belongs to the lines after it,
                            // so it goes at the very end.
                            index = visual.Length;
                        }
                        else
                        {
                            index = min;
                        }
                        rank = 2;
                        break;
                    }

                    case TagKind.Close:
                    {
                        int first = tag.Partner >= 0 ? tags[tag.Partner].Plain : 0;
                        if (!SpanBounds(first, tag.Plain, plainToShaped, shapedToVisual, shaped.Length, out int min, out int max))
                        {
                            // The span it closes is on an earlier line.
                            index = 0;
                        }
                        else
                        {
                            index = max + 1;
                        }
                        rank = 0;
                        break;
                    }

                    default:
                        // A tag that draws something of its own - a sprite, a
                        // space - sits in the gap it was typed in.
                        index = Gap(tag.Plain, logical.Length, plainToShaped, shapedToVisual,
                                    shaped.Length, visual.Length, rightToLeft);
                        rank = 1;
                        break;
                }

                insertions.Add(new Insertion
                {
                    Index = Clamp(index, 0, visual.Length),
                    Rank = rank,
                    Order = i,
                    Text = tag.Text
                });
            }

            return Weave(visual, insertions);
        }

        // ==========================================
        // Where a tag lands
        // ==========================================

        // The visual extent of a logical range: leftmost and rightmost
        // position any character of it ended up at. A range that turned
        // around still has both, which is the whole point - the opening tag
        // wants the left end regardless of which character that now is.
        private static bool SpanBounds(
            int from, int to, int[] plainToShaped, int[] shapedToVisual, int shapedLength,
            out int min, out int max)
        {
            min = int.MaxValue;
            max = -1;

            for (int p = from; p < to; p++)
            {
                int visual = VisualOf(p, plainToShaped, shapedToVisual, shapedLength);
                if (visual < 0) { continue; }

                if (visual < min) { min = visual; }
                if (visual > max) { max = visual; }
            }

            return max >= 0;
        }

        // The gap between two logical neighbours, in visual coordinates. When
        // they are visually adjacent the answer is exact; at the ends of the
        // line the paragraph's own direction decides which end is "after".
        private static int Gap(
            int plain, int plainLength, int[] plainToShaped, int[] shapedToVisual,
            int shapedLength, int visualLength, bool rightToLeft)
        {
            int before = plain > 0 ? VisualOf(plain - 1, plainToShaped, shapedToVisual, shapedLength) : -1;
            int after = plain < plainLength ? VisualOf(plain, plainToShaped, shapedToVisual, shapedLength) : -1;

            if (before >= 0 && after >= 0) { return after > before ? after : after + 1; }
            if (after >= 0) { return rightToLeft ? after + 1 : after; }
            if (before >= 0) { return rightToLeft ? before : before + 1; }

            return rightToLeft ? 0 : visualLength;
        }

        private static int VisualOf(int plain, int[] plainToShaped, int[] shapedToVisual, int shapedLength)
        {
            int shaped = plainToShaped[plain];
            if (shaped < 0 || shaped >= shapedLength) { return -1; }
            return shapedToVisual[shaped];
        }

        // ==========================================
        // Tags in, tags out
        // ==========================================

        // Lifts every tag out of the text, returning what is left and filling
        // the list with each tag and the position it stood in front of.
        private static string Split(string text, List<Tag> tags)
        {
            var plain = new StringBuilder(text.Length);

            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '<')
                {
                    int end = TagEnd(text, i);
                    if (end > 0)
                    {
                        tags.Add(NewTag(text.Substring(i, end - i + 1), plain.Length));
                        i = end + 1;
                        continue;
                    }
                }

                plain.Append(text[i]);
                i++;
            }

            Match(tags);
            return plain.ToString();
        }

        // The index of the '>' that closes a tag opening at 'start', or -1
        // when this '<' is just a less-than sign.
        private static int TagEnd(string text, int start)
        {
            int i = start + 1;
            if (i < text.Length && text[i] == '/') { i++; }
            if (i >= text.Length || !IsNameStart(text[i])) { return -1; }

            int limit = Math.Min(text.Length, start + MaxTagLength);
            for (int j = i; j < limit; j++)
            {
                char c = text[j];
                if (c == '>') { return j; }
                if (c == '<' || c == '\n' || c == '\r') { return -1; }
            }

            return -1;
        }

        private static Tag NewTag(string raw, int plain)
        {
            int i = 1;
            bool closing = i < raw.Length && raw[i] == '/';
            if (closing) { i++; }

            int nameStart = i;
            while (i < raw.Length && IsNameChar(raw[i])) { i++; }
            string name = raw.Substring(nameStart, i - nameStart);

            // "<#FF8800>" is the colour tag's short form, and the tag that
            // closes it is "</color>". Naming it here is what pairs them.
            if (nameStart < raw.Length && raw[nameStart] == '#') { name = "color"; }

            TagKind kind;
            if (closing) { kind = TagKind.Close; }
            else if (raw.Length >= 3 && raw[raw.Length - 2] == '/') { kind = TagKind.Void; }
            else { kind = IsVoidName(name) ? TagKind.Void : TagKind.Open; }

            return new Tag { Text = raw, Name = name, Kind = kind, Plain = plain, Partner = -1 };
        }

        // Tags that stand alone: they draw something or break something, and
        // there is no </sprite> to pair them with.
        private static bool IsVoidName(string name)
        {
            return Is(name, "br") || Is(name, "sprite") || Is(name, "space")
                || Is(name, "page") || Is(name, "nbsp");
        }

        // Pairs each closing tag with the nearest unclosed opening tag of the
        // same name. Anything left unpaired keeps Partner = -1, and is read
        // as running to the end (or from the start) of the line - which is
        // exactly what markup carried across a line break is doing.
        private static void Match(List<Tag> tags)
        {
            var open = new List<int>();

            for (int i = 0; i < tags.Count; i++)
            {
                Tag tag = tags[i];

                if (tag.Kind == TagKind.Open)
                {
                    open.Add(i);
                    continue;
                }

                if (tag.Kind != TagKind.Close) { continue; }

                for (int s = open.Count - 1; s >= 0; s--)
                {
                    int candidate = open[s];
                    if (!Is(tags[candidate].Name, tag.Name)) { continue; }

                    Tag opening = tags[candidate];
                    opening.Partner = i;
                    tags[candidate] = opening;

                    tag.Partner = candidate;
                    tags[i] = tag;

                    open.RemoveRange(s, open.Count - s);
                    break;
                }
            }
        }

        // Rebuilds a string with every tag put back at the position it was
        // given. Sorted so that at one position the closing tags come first,
        // then anything that draws, then the opening tags - which is the
        // order that keeps "</b><i>" from becoming "<i></b>".
        private static string Weave(string text, List<Insertion> insertions)
        {
            insertions.Sort(Compare);

            var output = new StringBuilder(text.Length + insertions.Count * 8);

            int next = 0;
            for (int i = 0; i <= text.Length; i++)
            {
                while (next < insertions.Count && insertions[next].Index <= i)
                {
                    output.Append(insertions[next].Text);
                    next++;
                }

                if (i < text.Length) { output.Append(text[i]); }
            }

            return output.ToString();
        }

        private static int Compare(Insertion a, Insertion b)
        {
            if (a.Index != b.Index) { return a.Index < b.Index ? -1 : 1; }
            if (a.Rank != b.Rank) { return a.Rank < b.Rank ? -1 : 1; }
            return a.Order < b.Order ? -1 : (a.Order > b.Order ? 1 : 0);
        }

        // ==========================================
        // Maps
        // ==========================================

        // For every position in the source - including the one just past the
        // end - where it ended up. Characters a pass consumed (a ZWNJ, the
        // alef of a lam-alef) point at whatever came next, so a tag that
        // stood in front of one still stands in front of something.
        private static int[] MapForward(int sourceLength, int outputLength, List<int> outputToSource)
        {
            var map = new int[sourceLength + 1];
            for (int i = 0; i <= sourceLength; i++) { map[i] = -1; }

            for (int i = 0; i < outputToSource.Count; i++)
            {
                int source = outputToSource[i];
                if (source >= 0 && source < sourceLength && map[source] < 0) { map[source] = i; }
            }

            map[sourceLength] = outputLength;
            for (int i = sourceLength - 1; i >= 0; i--)
            {
                if (map[i] < 0) { map[i] = map[i + 1]; }
            }

            return map;
        }

        // The inverse: for every source position, where it is in the output.
        // A position the output dropped keeps -1.
        private static int[] MapBack(int sourceLength, List<int> outputToSource)
        {
            var map = new int[sourceLength];
            for (int i = 0; i < sourceLength; i++) { map[i] = -1; }

            for (int i = 0; i < outputToSource.Count; i++)
            {
                int source = outputToSource[i];
                if (source >= 0 && source < sourceLength) { map[source] = i; }
            }

            return map;
        }

        // ==========================================
        // Line breaks
        // ==========================================

        /// <summary>
        /// The length of the hard line break starting at <paramref name="index"/>,
        /// or 0. Both kinds count: the newline character, and the &lt;br&gt;
        /// tag TextMeshPro treats as one.
        /// </summary>
        private static int HardBreakAt(string text, int index)
        {
            char c = text[index];

            if (c == '\r')
            {
                return index + 1 < text.Length && text[index + 1] == '\n' ? 2 : 1;
            }
            if (c == '\n') { return 1; }
            if (c != '<') { return 0; }

            int end = TagEnd(text, index);
            if (end < 0) { return 0; }

            string tag = text.Substring(index, end - index + 1);
            return IsBreakTag(tag) ? tag.Length : 0;
        }

        private static int HardBreakAtEnd(string line)
        {
            if (string.IsNullOrEmpty(line)) { return 0; }

            char last = line[line.Length - 1];
            if (last == '\n' || last == '\r') { return 1; }
            if (last != '>') { return 0; }

            int open = line.LastIndexOf('<');
            if (open < 0) { return 0; }

            string tag = line.Substring(open);
            return IsBreakTag(tag) ? tag.Length : 0;
        }

        private static bool IsBreakTag(string tag)
        {
            int i = 1;
            while (i < tag.Length && tag[i] == ' ') { i++; }

            int start = i;
            while (i < tag.Length && IsNameChar(tag[i])) { i++; }

            if (!Is(tag.Substring(start, i - start), "br")) { return false; }

            // "<br>", "<br/>", "<br />" - and nothing else, so a "<bring>"
            // somebody wrote as a style name is not a line break.
            while (i < tag.Length && (tag[i] == ' ' || tag[i] == '/')) { i++; }
            return i == tag.Length - 1;
        }

        // ==========================================
        // Small helpers
        // ==========================================
        private static bool IsNameStart(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '#';
        }

        private static bool IsNameChar(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                || (c >= '0' && c <= '9') || c == '-' || c == '_';
        }

        private static bool Is(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) { return min; }
            return value > max ? max : value;
        }
    }
}
