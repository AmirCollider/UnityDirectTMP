// ==========================================
// DirectDisplayText
// The one call between a string you stored and a
// string you can draw.
//
//   DirectDisplayText.Prepare("سلام دنیا")
//
// Two things have to happen to Arabic-script text
// before any renderer that does not understand it can
// draw it, and they have to happen in this order:
//
//   1. shape it   DirectArabicShaper, which joins the
//                 letters up. It reads the LOGICAL
//                 order, because "the letter before
//                 this one" only means anything there.
//
//   2. reorder it DirectBidi, which turns logical order
//                 into the order a screen paints in.
//
// Doing them the other way round produces a word whose
// letters connect to the wrong neighbours - which looks
// almost right, and is the reason this is one function
// instead of two that a caller sequences by hand.
//
// Everything here no-ops on text with no right-to-left
// characters in it, by reference, after one scan. An
// English or Japanese interface pays a pointer
// comparison for this file existing.
//
// WRAPPING
// --------
// A wrapped paragraph cannot be reordered as one
// string. Reordering puts the last word of the
// paragraph at the visual left of one very long line;
// a renderer then breaks that line into three, and the
// three come out in the wrong order top to bottom -
// the paragraph reads bottom-up. Every line has to be
// broken FIRST, in logical order, and reordered one
// line at a time.
//
// Unity's IMGUI will not break a line where we tell it
// to, so WrapToLines does the breaking here, measuring
// through a delegate the caller supplies. That keeps
// this file free of Unity and lets the editor pass in
// GUIStyle.CalcSize while a test passes in "one unit
// per character".
// ==========================================
using System.Collections.Generic;

namespace UnityDirectTMP
{
    /// <summary>
    /// Shaping and reordering, in the order they have to happen, plus the
    /// line breaking a wrapped right-to-left paragraph needs.
    /// </summary>
    public static class DirectDisplayText
    {
        /// <summary>
        /// True when <see cref="Prepare"/> would change this text - that is,
        /// when it contains anything written right to left.
        /// </summary>
        public static bool NeedsPreparing(string text)
        {
            return DirectBidi.ContainsRightToLeft(text);
        }

        /// <summary>
        /// True when the text already holds Arabic presentation forms, and so
        /// has almost certainly been through <see cref="Prepare"/> already.
        ///
        /// This is a guard, not a feature. A string in this package can reach
        /// a label through several helpers, and preparing it twice would
        /// reverse it back into nonsense - a bug that looks exactly like the
        /// one this whole file exists to fix, and is far harder to find. The
        /// cost of the guard is that text an author deliberately typed in
        /// presentation forms is passed through untouched, which is the right
        /// answer for it anyway.
        /// </summary>
        public static bool LooksPrepared(string text)
        {
            if (string.IsNullOrEmpty(text)) { return false; }

            for (int i = 0; i < text.Length; i++)
            {
                if (DirectArabicShaper.IsPresentationForm(text[i])) { return true; }
            }
            return false;
        }

        /// <summary>
        /// Joins the letters up and puts them in reading order. Text with no
        /// right-to-left characters is returned unchanged.
        /// </summary>
        public static string Prepare(string text)
        {
            return Prepare(text, DirectBidi.AutoDirection);
        }

        /// <summary>
        /// Joins the letters up and puts them in reading order, with the
        /// paragraph direction forced rather than detected. Pass
        /// <see cref="DirectBidi.RightToLeft"/> when the surrounding interface
        /// is right-to-left and the string itself starts with a Latin word -
        /// a version number, a font family name - which would otherwise be
        /// read as a left-to-right paragraph.
        /// </summary>
        public static string Prepare(string text, int paragraphDirection)
        {
            if (string.IsNullOrEmpty(text)) { return text; }
            if (!NeedsPreparing(text)) { return text; }
            if (LooksPrepared(text)) { return text; }

            return DirectBidi.Reorder(DirectArabicShaper.Shape(text), paragraphDirection);
        }

        // ==========================================
        // WrapToLines
        //
        // Greedy word wrapping over the LOGICAL text,
        // measuring each candidate line in its PREPARED
        // form - because the prepared form is what will
        // actually be drawn, and shaping changes the
        // width of a word. Each finished line is
        // prepared on its own, so the lines come out in
        // reading order top to bottom with each one
        // correct left to right.
        //
        // A word too long for the line is given a line
        // of its own rather than split: breaking inside
        // a word of joined script is a worse outcome
        // than one line that overhangs.
        // ==========================================
        /// <summary>
        /// Breaks text into display-ready lines that each fit
        /// <paramref name="maxWidth"/>, measured by <paramref name="measure"/>.
        /// Existing line breaks in the text are kept.
        /// </summary>
        public static List<string> WrapToLines(
            string text,
            float maxWidth,
            System.Func<string, float> measure,
            int paragraphDirection = DirectBidi.AutoDirection)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text)) { return lines; }

            if (measure == null || maxWidth <= 0f)
            {
                lines.Add(Prepare(text, paragraphDirection));
                return lines;
            }

            foreach (string hardLine in text.Split('\n'))
            {
                if (hardLine.Length == 0)
                {
                    lines.Add(string.Empty);
                    continue;
                }

                WrapOneLine(hardLine, maxWidth, measure, paragraphDirection, lines);
            }

            return lines;
        }

        /// <summary>
        /// The same wrapping, joined back into one string with newlines - the
        /// form a Unity label wants.
        /// </summary>
        public static string Wrap(
            string text,
            float maxWidth,
            System.Func<string, float> measure,
            int paragraphDirection = DirectBidi.AutoDirection)
        {
            List<string> lines = WrapToLines(text, maxWidth, measure, paragraphDirection);
            return string.Join("\n", lines.ToArray());
        }

        private static void WrapOneLine(
            string logical,
            float maxWidth,
            System.Func<string, float> measure,
            int paragraphDirection,
            List<string> output)
        {
            List<string> words = SplitWords(logical);

            var line = new System.Text.StringBuilder();
            int wordsOnLine = 0;

            for (int i = 0; i < words.Count; i++)
            {
                int lengthBefore = line.Length;
                line.Append(words[i]);

                // Trailing spaces never justify a break - they are invisible
                // at the end of a line, and breaking on them leaves a ragged
                // edge one space wide.
                string candidate = TrimEnd(line);

                if (wordsOnLine > 0 && measure(Prepare(candidate, paragraphDirection)) > maxWidth)
                {
                    line.Length = lengthBefore;
                    output.Add(Prepare(TrimEnd(line), paragraphDirection));

                    line.Length = 0;
                    line.Append(TrimStart(words[i]));
                    wordsOnLine = 1;
                    continue;
                }

                wordsOnLine++;
            }

            if (line.Length > 0 || output.Count == 0)
            {
                output.Add(Prepare(TrimEnd(line), paragraphDirection));
            }
        }

        // Words carry their own trailing whitespace, so re-joining them is
        // concatenation and the original spacing survives a wrap that does
        // not happen.
        private static List<string> SplitWords(string text)
        {
            var words = new List<string>();
            int start = 0;

            while (start < text.Length)
            {
                int end = start;
                while (end < text.Length && !IsBreakable(text[end])) { end++; }
                while (end < text.Length && IsBreakable(text[end])) { end++; }

                words.Add(text.Substring(start, end - start));
                start = end;
            }

            return words;
        }

        private static bool IsBreakable(char c)
        {
            return c == ' ' || c == '\t';
        }

        private static string TrimEnd(System.Text.StringBuilder builder)
        {
            int end = builder.Length;
            while (end > 0 && IsBreakable(builder[end - 1])) { end--; }
            return builder.ToString(0, end);
        }

        private static string TrimStart(string text)
        {
            int start = 0;
            while (start < text.Length && IsBreakable(text[start])) { start++; }
            return text.Substring(start);
        }
    }
}
