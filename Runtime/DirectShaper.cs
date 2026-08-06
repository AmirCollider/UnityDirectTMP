// ==========================================
// DirectShaper
// Arabic-script letters, joined.
//
// Unicode stores Arabic and Persian the way a person
// types them: one codepoint per letter, in the order the
// letters are spoken. "سلام" is four separate letters.
// What a reader expects to SEE is one connected word, in
// which the seen has an initial shape, the lam a medial
// shape and the alef a final shape.
//
// Choosing that shape is called shaping, and nothing in
// Unity does it.
//
// ==========================================
// WHAT THIS FILE DOES NOT DO
// ==========================================
// It does not decide whether a shape is AVAILABLE. It
// works out, from Unicode alone, which of the four
// shapes each letter should be in, and asks a resolver
// for a codepoint that draws it.
//
// That split is the fix. The old implementation asked
// the font "do you have U+FB58?" while it was choosing
// shapes, so a font missing one legacy codepoint changed
// the shape of the letters AROUND it and words came
// apart in the middle. Availability is now somebody
// else's problem - DirectFontJoiner's - and it solves it
// by ADDING the glyph rather than by giving up.
//
// Verified glyph-for-glyph against HarfBuzz over a
// Persian/Arabic/Urdu corpus on five fonts, including a
// font with no presentation codepoints at all.
//
// Pure C#. No Unity types, no file access - so it runs
// in a player build, in an editor window and in a unit
// test with no Unity Editor around it.
// ==========================================
using System;
using System.Collections.Generic;
using System.Text;

namespace UnityDirectTMP
{
    /// <summary>
    /// Turns logically-ordered Arabic-script text into the shapes a font can
    /// draw joined up. Reordering for display is a separate pass - see
    /// <see cref="DirectBidi"/> - and must happen after this one.
    /// </summary>
    public static class DirectShaper
    {
        /// <summary>
        /// Asked for a codepoint that draws <paramref name="letter"/> in
        /// <paramref name="form"/>. Returns '\0' when the font cannot draw
        /// that shape at all, in which case the plain letter is emitted.
        /// </summary>
        public delegate char FormResolver(char letter, DirectForm form);

        /// <summary>
        /// Asked for a codepoint that draws lam followed by
        /// <paramref name="alef"/> as the single ligature glyph Arabic
        /// requires. Returns '\0' when the font has no such glyph, in which
        /// case the two letters are shaped separately.
        /// </summary>
        public delegate char LigatureResolver(char alef, DirectForm form);

        /// <summary>
        /// Shapes with no font behind it: every letter becomes its Unicode
        /// presentation form where one exists. Useful for tests and for
        /// IMGUI, which has no font asset to add glyphs to.
        /// </summary>
        public static string Shape(string text)
            => Shape(text, DirectJoining.PresentationForm, DefaultLigature, null);

        private static char DefaultLigature(char alef, DirectForm form)
        {
            char[] pair = DirectJoining.LamAlef(alef);
            return pair == null ? '\0' : pair[form == DirectForm.Final ? 1 : 0];
        }

        /// <summary>
        /// Shapes <paramref name="text"/>, asking <paramref name="resolve"/>
        /// for the codepoint of each shape.
        ///
        /// <paramref name="sourceIndices"/>, when given, is filled with the
        /// index in <paramref name="text"/> that each output character came
        /// from - so a caller that has to put something back where it was can
        /// follow the letters through a pass that drops ZWNJ and turns lam+alef
        /// into one glyph.
        /// </summary>
        public static string Shape(
            string text,
            FormResolver resolve,
            LigatureResolver resolveLigature,
            List<int> sourceIndices)
        {
            if (sourceIndices != null) { sourceIndices.Clear(); }

            if (!DirectJoining.NeedsShaping(text))
            {
                FillIdentity(sourceIndices, text);
                return text;
            }

            resolve = resolve ?? DirectJoining.PresentationForm;
            resolveLigature = resolveLigature ?? DefaultLigature;

            var output = new StringBuilder(text.Length);

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // The non-joiner's whole job was to be a non-joining character
                // while the shapes below were chosen. Keeping it afterwards is
                // one more codepoint for a font to have no glyph for.
                if (c == DirectJoining.ZeroWidthNonJoiner) { continue; }

                if (!DirectJoining.IsShaped(c))
                {
                    Emit(output, sourceIndices, c, i);
                    continue;
                }

                DirectForm form = FormAt(text, i);

                // لا, before anything else.
                if (c == DirectJoining.Lam
                    && i + 1 < text.Length
                    && DirectJoining.LamAlef(text[i + 1]) != null)
                {
                    // The ligature takes the lam's own shape: final when
                    // something joins into the lam, isolated otherwise. It can
                    // never be initial or medial - it ends in an alef.
                    DirectForm ligatureForm =
                        form == DirectForm.Final || form == DirectForm.Medial
                            ? DirectForm.Final
                            : DirectForm.Isolated;

                    char ligature = resolveLigature(text[i + 1], ligatureForm);
                    if (ligature != '\0')
                    {
                        Emit(output, sourceIndices, ligature, i);
                        i++;                       // the alef is inside the glyph
                        continue;
                    }

                    // No ligature glyph. Falling through shapes the lam and the
                    // alef as ordinary neighbours - not what a reader expects,
                    // but it is still the word.
                }

                char shaped = resolve(c, form);
                Emit(output, sourceIndices, shaped == '\0' ? c : shaped, i);
            }

            return output.ToString();
        }

        /// <summary>
        /// Which of the four shapes the letter at <paramref name="index"/>
        /// takes, from Unicode joining classes alone.
        /// </summary>
        public static DirectForm FormAt(string text, int index)
        {
            DirectJoiningClass self = DirectJoining.ClassOf(text[index]);

            bool before = DirectJoining.Offers(Neighbour(text, index, -1))
                       && DirectJoining.Accepts(self);
            bool after = DirectJoining.Offers(self)
                      && DirectJoining.Accepts(Neighbour(text, index, +1));

            if (before && after) { return DirectForm.Medial; }
            if (before) { return DirectForm.Final; }
            if (after) { return DirectForm.Initial; }
            return DirectForm.Isolated;
        }

        // Marks are skipped when looking for a neighbour: a fatha sitting over
        // a letter is drawn above the join, not in it, so a word with vowel
        // marks joins exactly as it does without them.
        private static DirectJoiningClass Neighbour(string text, int index, int step)
        {
            for (int i = index + step; i >= 0 && i < text.Length; i += step)
            {
                DirectJoiningClass j = DirectJoining.ClassOf(text[i]);
                if (j != DirectJoiningClass.Transparent) { return j; }
            }
            return DirectJoiningClass.None;
        }

        private static void Emit(StringBuilder output, List<int> sourceIndices, char c, int from)
        {
            output.Append(c);
            if (sourceIndices != null) { sourceIndices.Add(from); }
        }

        private static void FillIdentity(List<int> sourceIndices, string text)
        {
            if (sourceIndices == null || string.IsNullOrEmpty(text)) { return; }
            for (int i = 0; i < text.Length; i++) { sourceIndices.Add(i); }
        }
    }
}
