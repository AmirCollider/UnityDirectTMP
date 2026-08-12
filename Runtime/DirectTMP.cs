// ==========================================
// DirectTMP
// The public API.
//
// There is no project-wide setting and no global state.
// A label is drawn from a font file because it has a
// Direct Font component on it, or because your code
// called one of these methods. Nothing happens to a
// label nobody asked about.
// ==========================================
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>Everything this package can be asked to do, in one place.</summary>
    public static class DirectTMP
    {
        // ==========================================
        // Text
        // ==========================================
        /// <summary>
        /// Joins Arabic-script letters and puts right-to-left text in reading
        /// order, using <paramref name="asset"/> to find the joined shapes.
        /// Text with no Arabic script in it comes back untouched.
        ///
        /// Rich-text tags are never shaped or reordered.
        /// </summary>
        public static string Prepare(string text, TMP_FontAsset asset)
        {
            if (string.IsNullOrEmpty(text)) { return text; }
            if (!DirectJoining.NeedsShaping(text) && !DirectBidi.ContainsRightToLeft(text)) { return text; }

            DirectFontJoiner joiner = asset == null ? null : DirectFontJoiner.For(asset);

            return DirectRichText.Apply(text, segment =>
            {
                string shaped = joiner == null
                    ? DirectShaper.Shape(segment)
                    : joiner.Shape(segment);

                return DirectBidi.Reorder(shaped);
            });
        }

        /// <summary>
        /// The same, with no font behind it - every letter becomes its Unicode
        /// presentation form. For text going somewhere that is not a
        /// TextMeshPro label.
        /// </summary>
        public static string Prepare(string text) => Prepare(text, null);

        /// <summary>
        /// Joins the letters but leaves them in logical order - no reordering.
        ///
        /// This is what you measure with. Shaped text has the same glyphs, and
        /// therefore the same width and the same line breaks, as the reordered
        /// text that will finally be drawn - but it is still in the order the
        /// letters were typed, so a line break found in it is a break at a
        /// known place in the original sentence.
        /// </summary>
        public static string Shape(string text, TMP_FontAsset asset)
        {
            if (string.IsNullOrEmpty(text) || !DirectJoining.NeedsShaping(text)) { return text; }

            DirectFontJoiner joiner = asset == null ? null : DirectFontJoiner.For(asset);

            return DirectRichText.Apply(text, segment =>
                joiner == null ? DirectShaper.Shape(segment) : joiner.Shape(segment));
        }

        /// <summary>
        /// Reorders one already-shaped line into the order it is read in.
        /// </summary>
        public static string Reorder(string line) => DirectBidi.Reorder(line);

        /// <summary>
        /// Splits text into paragraphs at the line breaks its author typed,
        /// recognising all three endings - "\r\n", "\r" and "\n" - and
        /// keeping none of them.
        ///
        /// Unity's own text fields hand back WINDOWS line endings, so a break
        /// is "\r\n" and not "\n". Splitting on "\n" alone leaves the "\r"
        /// stuck to the end of the paragraph before it, where it does damage
        /// twice over: reordering carries it to the front of a right-to-left
        /// line, and TextMeshPro then draws it as a line break of its own.
        ///
        /// Pure, and separated out so that it can be tested without a label,
        /// a font or a running Editor.
        /// </summary>
        public static string[] Paragraphs(string text)
        {
            if (text == null) { return new string[0]; }
            if (text.IndexOf('\r') < 0 && text.IndexOf('\n') < 0) { return new[] { text }; }

            var paragraphs = new List<string>();
            int start = 0;
            int i = 0;

            while (i < text.Length)
            {
                char c = text[i];
                if (c != '\r' && c != '\n') { i++; continue; }

                paragraphs.Add(text.Substring(start, i - start));

                i += c == '\r' && i + 1 < text.Length && text[i + 1] == '\n' ? 2 : 1;
                start = i;
            }

            paragraphs.Add(text.Substring(start));
            return paragraphs.ToArray();
        }

        // ==========================================
        // Labels
        // ==========================================
        /// <summary>
        /// Draws one label from a font file, from code - the same thing the
        /// Direct Font component does. Returns false if the font could not be
        /// built; the Console says why.
        /// </summary>
        public static bool Apply(TMP_Text label, Font file)
        {
            if (label == null || file == null) { return false; }

            DirectFont direct = label.GetComponent<DirectFont>();
            if (direct == null) { direct = label.gameObject.AddComponent<DirectFont>(); }

            direct.Font = file;
            return direct.FontAsset != null;
        }

        /// <summary>
        /// Puts an outline on one label and on no other. Returns false if the
        /// label has no Direct Font on it — <see cref="Apply"/> adds one.
        ///
        /// The outline is kept on the component rather than written straight
        /// onto a material, because the material behind a generated font asset
        /// is generated too and nothing set on it survives a reload.
        /// </summary>
        public static bool Outline(TMP_Text label, float width, Color color)
        {
            DirectFont direct = label == null ? null : label.GetComponent<DirectFont>();
            if (direct == null) { return false; }

            // Colour first: the width is what decides whether anything is
            // written at all, so setting it second means one write, not two.
            direct.OutlineColor = color;
            direct.OutlineWidth = width;
            return true;
        }

        /// <summary>
        /// A dynamic font asset for a font file, cached. Assign it to
        /// <c>label.font</c> yourself if you would rather not have a component.
        /// </summary>
        public static TMP_FontAsset Load(Font file) => DirectFontLibrary.Load(file);

        /// <summary>The same, for a .ttf/.otf on disk rather than an imported font.</summary>
        public static TMP_FontAsset LoadFromFile(string path) => DirectFontLibrary.LoadFromFile(path);

        /// <summary>The same, for font bytes already in memory.</summary>
        public static TMP_FontAsset LoadFromBytes(byte[] bytes, string name = null)
            => DirectFontLibrary.LoadFromBytes(bytes, name);

        /// <summary>
        /// Rasterizes characters up front, so the first frame that shows them
        /// does not pay for all of them at once. Returns the characters the
        /// font could not supply.
        /// </summary>
        public static string Preload(TMP_FontAsset asset, string characters)
        {
            if (asset == null || string.IsNullOrEmpty(characters)) { return characters; }

            asset.TryAddCharacters(characters, out string missing);
            return missing;
        }

        /// <summary>Drops every cached font asset. Anything still using one keeps working.</summary>
        public static int ClearCache() => DirectFontLibrary.ClearCache();
    }
}
