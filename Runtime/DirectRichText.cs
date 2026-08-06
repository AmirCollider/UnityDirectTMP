// ==========================================
// DirectRichText
// Keeps <b>, <color=#fff> and the rest intact.
//
// A rich-text tag is Latin text sitting inside what may
// be a right-to-left line. Reordering the line as one
// string turns <b> into >b<, and TextMeshPro then draws
// the tag instead of obeying it.
//
// So the line is cut at its tags. Each stretch of real
// text between them is shaped and reordered on its own,
// and the tags are put back exactly where they were.
// Nothing inside a tag is ever shaped, reordered or
// otherwise touched.
//
// The limit of this approach is worth stating plainly: a
// tag in the MIDDLE of a right-to-left sentence splits
// it into two runs that are each ordered correctly but
// are laid out left-to-right relative to each other. A
// tag around a whole phrase - which is what almost every
// tag in almost every project is - has no such problem.
// ==========================================
using System.Text;

namespace UnityDirectTMP
{
    /// <summary>
    /// Splits text at TextMeshPro's rich-text tags so that shaping and
    /// reordering never see one.
    /// </summary>
    public static class DirectRichText
    {
        /// <summary>How a stretch of tag-free text should be transformed.</summary>
        public delegate string Transform(string segment);

        /// <summary>True when the text has something that looks like a tag in it.</summary>
        public static bool HasTags(string text)
        {
            if (string.IsNullOrEmpty(text)) { return false; }

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '<' && CloseOf(text, i) > i) { return true; }
            }
            return false;
        }

        /// <summary>
        /// Applies <paramref name="transform"/> to every stretch of text that
        /// is not inside a tag, and leaves the tags alone.
        /// </summary>
        public static string Apply(string text, Transform transform)
        {
            if (string.IsNullOrEmpty(text) || transform == null) { return text; }
            if (!HasTags(text)) { return transform(text); }

            var output = new StringBuilder(text.Length);
            int segment = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != '<') { continue; }

                int close = CloseOf(text, i);
                if (close <= i) { continue; }

                if (i > segment) { output.Append(transform(text.Substring(segment, i - segment))); }

                output.Append(text, i, close - i + 1);

                i = close;
                segment = close + 1;
            }

            if (segment < text.Length) { output.Append(transform(text.Substring(segment))); }

            return output.ToString();
        }

        // Where the tag opened at `open` ends, or -1 when it never does.
        //
        // A lone '<' is ordinary text - "a < b" is a comparison, not a broken
        // tag - and a '<' with no '>' after it on the same line is the same
        // thing. Both have to survive untouched, so an unterminated tag is not
        // a tag.
        private static int CloseOf(string text, int open)
        {
            if (open < 0 || open >= text.Length || text[open] != '<') { return -1; }

            for (int i = open + 1; i < text.Length; i++)
            {
                if (text[i] == '\n') { return -1; }
                if (text[i] == '<') { return -1; }
                if (text[i] == '>') { return i == open + 1 ? -1 : i; }
            }
            return -1;
        }
    }
}
