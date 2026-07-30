// ==========================================
// DirectRichTextTests
// The runtime half of "Persian renders correctly":
// text with markup in it, text that wraps, and fonts
// that do not carry the presentation forms at all.
//
// DirectTextDisplayTests covers the shaper and the
// reorderer on plain strings. Everything here is what
// happens once a real label is involved - because a
// TextMeshPro label's text is not a plain string. It
// has <b> and <color> in it, it folds onto three lines,
// and the font behind it may be one of the modern
// Persian faces that carry no presentation forms
// whatsoever.
//
// Expectations are written as codepoints, for the same
// reason the older file gives: an assertion that one
// Arabic string equals another tells a reader nothing
// when it fails.
// ==========================================
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace UnityDirectTMP.Editor.Tests
{
    public sealed class DirectRichTextTests
    {
        // ==========================================
        // Shaping, and the map a caller follows a tag
        // through.
        // ==========================================

        [Test]
        public void TheLettersInTheReportJoinCorrectly()
        {
            // کریم - keheh, reh, farsi yeh, meem. The pair the bug report
            // singles out ("ی and ر next to each other") is here:
            //   keheh     something after it, nothing before   -> initial
            //   reh       joins backwards only                 -> final
            //   farsi yeh a reh offers nothing forward, but the
            //             meem accepts one                     -> initial
            //   meem      joined from behind, nothing after    -> final
            //
            // Nothing was ever wrong with those two letters. EVERY letter was
            // unjoined, and those two are simply the ones whose isolated
            // shapes look least like their joined ones.
            Assert.AreEqual(
                "\uFB90\uFEAE\uFBFE\uFEE2",
                DirectArabicShaper.Shape("کریم"));
        }

        [Test]
        public void ShapingReportsWhereEveryCharacterCameFrom()
        {
            // The map is what lets a rich-text tag survive a pass that
            // removes characters.
            var map = new List<int>();
            string shaped = DirectArabicShaper.Shape("می‌ش", null, map);

            Assert.AreEqual(shaped.Length, map.Count, "One entry per output character.");
            CollectionAssert.AreEqual(new[] { 0, 1, 3 }, map,
                "The non-joiner at index 2 produced nothing, so it has no entry.");
        }

        [Test]
        public void ShapingMapsALigatureBackToItsFirstLetter()
        {
            // سلام is three glyphs from four letters.
            var map = new List<int>();
            DirectArabicShaper.Shape("سلام", null, map);

            CollectionAssert.AreEqual(new[] { 0, 1, 3 }, map,
                "The lam-alef glyph belongs to the lam it started at.");
        }

        [Test]
        public void ShapingWithoutAMapIsTheSameShaping()
        {
            Assert.AreEqual(
                DirectArabicShaper.Shape("سلام دنیا"),
                DirectArabicShaper.Shape("سلام دنیا", null, new List<int>()));
        }

        // ==========================================
        // A font with no presentation forms.
        //
        // The forms are a legacy block. Vazirmatn, Noto
        // Sans Arabic and most of what a Persian designer
        // reaches for carry the plain letters and their
        // joining rules in OpenType, and nothing at all
        // at U+FE70 - so shaping into that block would
        // turn readable text into boxes.
        // ==========================================

        [Test]
        public void AFontWithoutTheFormsKeepsTheLettersItHas()
        {
            const string word = "کریم";
            string shaped = DirectArabicShaper.Shape(word, c => c < 'ﭐ', null);

            Assert.AreEqual(word, shaped,
                "Unjoined but readable is a better answer than four boxes.");
        }

        [Test]
        public void AFontWithoutTheLigatureDrawsLamAndAlefSeparately()
        {
            string shaped = DirectArabicShaper.Shape("سلام", NoLamAlef, null);

            Assert.AreEqual("\uFEB3\uFEE0\uFE8E\uFEE1", shaped,
                "Seen initial, lam medial, alef final, meem isolated - the word, without the ligature.");
        }

        // ==========================================
        // The block a font leaves out.
        //
        // Persian's own letters - پ چ ژ ک گ ی - shape
        // into U+FB50..FBFF. The rest of the alphabet
        // shapes into U+FE70..FEFF. Fonts do not treat
        // the two alike: Segoe UI, the font on every
        // Windows machine, carries the second in full and
        // none of the first. So a Persian word joins
        // everywhere except the letters that make it
        // Persian, which is what the 1.1.0 bug report
        // looked like from the outside.
        // ==========================================

        // A font with the Arabic presentation forms and none of the Persian
        // ones. Not a hypothetical: it is what the report was written on.
        private static bool NoFormsA(char c)
        {
            return c < '\uFB50' || c > '\uFBFF';
        }

        [Test]
        public void ANeighbourDoesNotReachForALetterTheFontCannotJoin()
        {
            // پو. The peh has to be drawn plain, so the waw must be ISOLATED -
            // not final, whose glyph carries a connecting stroke that would
            // reach towards the peh and stop in mid-air.
            //
            // This is the whole difference between "this font cannot set
            // Persian" and "this package is broken", and 1.1.0 got it wrong:
            // it substituted the letter without telling its neighbours.
            Assert.AreEqual("\u067E\uFEED", DirectArabicShaper.Shape("پو", NoFormsA, null));
        }

        [Test]
        public void TheFarsiYehStandsInWithTheDotlessArabicShapes()
        {
            // یونیتی. Initial and medial yeh are drawn identically in both
            // blocks; isolated and final are dotless in Persian, which is
            // exactly what alef maksura is. So the word joins in full.
            Assert.AreEqual(
                "\uFEF3\uFEEE\uFEE7\uFEF4\uFE98\uFEF0",
                DirectArabicShaper.Shape("یونیتی", NoFormsA, null));
        }

        [Test]
        public void KehehStandsInWithKaf()
        {
            // کاربر - keheh initial is the same glyph as kaf initial.
            Assert.AreEqual(
                "\uFEDB\uFE8E\uFEAD\uFE91\uFEAE",
                DirectArabicShaper.Shape("کاربر", NoFormsA, null));
        }

        [Test]
        public void ALetterWithNoStandInIsLeftAloneAndSoAreItsNeighbours()
        {
            // پوشه: the peh alone, then a waw that knows not to reach for it,
            // then شه joined as normal. Unjoined in one place beats a stroke
            // hanging off nothing in two.
            Assert.AreEqual(
                "\u067E\uFEED\uFEB7\uFEEA",
                DirectArabicShaper.Shape("پوشه", NoFormsA, null));
        }

        [Test]
        public void CanJoinReportsWhatTheFontCanActuallyDo()
        {
            Assert.IsTrue(DirectArabicShaper.CanJoin('ر', NoFormsA));
            Assert.IsTrue(DirectArabicShaper.CanJoin('ی', NoFormsA), "ی has a stand-in every Arabic font carries.");
            Assert.IsTrue(DirectArabicShaper.CanJoin('ک', NoFormsA), "So does ک.");

            Assert.IsFalse(DirectArabicShaper.CanJoin('پ', NoFormsA),
                "peh has no stand-in: a beh would be a different letter.");
            Assert.IsFalse(DirectArabicShaper.CanJoin('گ', NoFormsA));

            Assert.IsTrue(DirectArabicShaper.CanJoin('ی', null), "With no probe, the table is the answer.");
            Assert.IsFalse(DirectArabicShaper.CanJoin('A', null), "Latin joins nothing.");
        }

        [Test]
        public void AFontWithBothBlocksStillUsesThePersianOnes()
        {
            // The stand-ins are a fallback, not a preference: a font that has
            // the real forms gets the real forms.
            Assert.AreEqual(
                "\uFBFE\uFEEE\uFEE7\uFBFF\uFE98\uFBFD",
                DirectArabicShaper.Shape("یونیتی", c => true, null));
        }

        [Test]
        public void TheProbeIsOnlyAskedAboutFormsItWouldDraw()
        {
            var asked = new List<char>();
            DirectArabicShaper.Shape("Hello", c => { asked.Add(c); return true; }, null);

            CollectionAssert.IsEmpty(asked, "Text with no Arabic in it never reaches the font.");
        }

        private static bool NoLamAlef(char c)
        {
            return c < 'ﻵ' || c > 'ﻼ';
        }

        // ==========================================
        // Reordering, and the map that goes with it.
        // ==========================================

        [Test]
        public void ReorderingReportsWhereEveryCharacterWent()
        {
            var map = new List<int>();
            string visual = DirectBidi.Reorder("سلا", DirectBidi.RightToLeft, map);

            Assert.AreEqual(visual.Length, map.Count);
            CollectionAssert.AreEqual(new[] { 2, 1, 0 }, map, "A right-to-left run is its own reverse.");
        }

        [Test]
        public void ReorderingMapsAcrossLinesWithoutLosingTheNewline()
        {
            var map = new List<int>();
            string visual = DirectBidi.Reorder("اب\nج", DirectBidi.RightToLeft, map);

            Assert.AreEqual(visual.Length, map.Count);
            CollectionAssert.AreEqual(new[] { 1, 0, 2, 3 }, map,
                "Each line reverses on its own and the newline stays at index 2.");
        }

        [Test]
        public void TheMapDoesNotChangeWhatReorderingProduces()
        {
            const string text = "نسخه 1.0 از TextMeshPro";

            Assert.AreEqual(
                DirectBidi.Reorder(text, DirectBidi.RightToLeft),
                DirectBidi.Reorder(text, DirectBidi.RightToLeft, new List<int>()));
        }

        // ==========================================
        // Markup
        //
        // A reordering pass that does not know what a tag
        // is turns "<b>" into ">b<" - mirrored, reversed
        // and unparseable - and the label draws the
        // markup instead of applying it.
        // ==========================================

        [Test]
        public void ATagPairStillWrapsTheSameWord()
        {
            string prepared = DirectRichText.Prepare("سلام <b>دنیا</b>");

            // دنیا is the leftmost word on screen once the line turns around,
            // so both tags moved with it - and swapped ends.
            Assert.AreEqual(
                "<b>\uFE8E\uFBFF\uFEE7\uFEA9</b> \uFEE1\uFEFC\uFEB3",
                prepared);
        }

        [Test]
        public void TagBracketsAreNeverMirrored()
        {
            string prepared = DirectRichText.Prepare("سلام <b>دنیا</b>");

            Assert.AreEqual(-1, prepared.IndexOf(">b<", StringComparison.Ordinal),
                "A mirrored bracket is a tag TextMeshPro draws instead of obeys.");
        }

        [Test]
        public void LettersJoinAcrossATag()
        {
            // A bold tag inside a word is a change of weight, not a break in
            // the writing: the seen still reaches the lam on the far side.
            string shaped = DirectRichText.Shape("س<b>لام");

            Assert.AreEqual("\uFEB3<b>\uFEFC\uFEE1", shaped);
        }

        [Test]
        public void NestedTagsStayNestedAndBalanced()
        {
            string prepared = DirectRichText.Prepare("<color=#ff0000>سلام <b>دنیا</b></color>");

            StringAssert.StartsWith("<color=#ff0000><b>", prepared);
            StringAssert.EndsWith("</color>", prepared);
            Assert.Less(
                prepared.IndexOf("</b>", StringComparison.Ordinal),
                prepared.IndexOf("</color>", StringComparison.Ordinal),
                "The inner tag has to close before the outer one.");
        }

        [Test]
        public void ATagOnALatinWordInsidePersianKeepsTheWordIntact()
        {
            string prepared = DirectRichText.Prepare("فونت <b>TextMeshPro</b> است");

            StringAssert.Contains("<b>TextMeshPro</b>", prepared,
                "A Latin run reads forwards, and its markup stays around it.");
        }

        [Test]
        public void ASpriteSitsInTheGapItWasTypedIn()
        {
            string prepared = DirectRichText.Prepare("سلام <sprite=3> دنیا");

            StringAssert.Contains("<sprite=3>", prepared);
            Assert.AreEqual(
                DirectRichText.Prepare("سلام دنیا").Length + "<sprite=3>".Length + 1,
                prepared.Length,
                "The sprite is one insertion, not a re-shaped word.");
        }

        [Test]
        public void MarkupCarriedInFromAnEarlierLineClosesAtTheRightEnd()
        {
            // What a line that starts inside a <b> span looks like on its own.
            StringAssert.EndsWith("</b>", DirectRichText.Prepare("دنیا</b>"));
            StringAssert.StartsWith("<b>", DirectRichText.Prepare("<b>دنیا"));
        }

        [Test]
        public void AnOpeningTagWithNothingLeftToWrapStaysAtTheEnd()
        {
            // It belongs to the lines AFTER it. Put at the visual start of a
            // right-to-left line it would bold the whole line instead.
            StringAssert.EndsWith("<b>", DirectRichText.Prepare("دنیا<b>"));
        }

        [Test]
        public void ALoneLessThanIsNotATag()
        {
            Assert.AreEqual("a < b", DirectRichText.Prepare("a < b"));
        }

        [Test]
        public void TextWithNothingRightToLeftIsReturnedUntouched()
        {
            const string english = "Hello <b>world</b>";

            Assert.AreSame(english, DirectRichText.Prepare(english),
                "Not merely equal - the same string, so a Latin UI allocates nothing.");
            Assert.AreSame(english, DirectRichText.Shape(english));
        }

        // ==========================================
        // Lines
        // ==========================================

        [Test]
        public void EachHardLineIsPreparedOnItsOwn()
        {
            Assert.AreEqual(
                DirectRichText.Prepare("سلام") + "\n" + DirectRichText.Prepare("دنیا"),
                DirectRichText.Prepare("سلام\nدنیا"),
                "Reordering two lines as one string swaps them.");
        }

        [Test]
        public void ABreakTagIsALineBreakRatherThanSomethingToMove()
        {
            Assert.AreEqual(
                DirectRichText.Prepare("سلام") + "<br>" + DirectRichText.Prepare("دنیا"),
                DirectRichText.Prepare("سلام<br>دنیا"));
        }

        [Test]
        public void ATagMerelyNamedLikeABreakIsStillJustATag()
        {
            // "<bring>" is not "<br>", and reading it as one would split a
            // paragraph at a style name.
            Assert.AreEqual(-1, DirectRichText.Prepare("سلام<bring>دنیا").IndexOf('\n'));
        }

        // ==========================================
        // Wrapping.
        //
        // The bug the whole two-pass arrangement exists
        // for: reorder a paragraph, let a renderer wrap
        // it, and the lines come out bottom to top.
        // ==========================================

        [Test]
        public void WrappedLinesReadTopToBottom()
        {
            string[] words = { "یک", "دو", "سه", "چهار", "پنج" };
            string shaped = DirectRichText.Shape(string.Join(" ", words));

            // Where a renderer measuring the shaped text would have broken it.
            int breakAt = shaped.Length - DirectRichText.Shape("چهار پنج").Length;

            string[] lines = DirectRichText
                .PrepareAtBreaks(shaped, new[] { breakAt }, DirectBidi.RightToLeft)
                .Split('\n');

            Assert.AreEqual(2, lines.Length);
            Assert.AreEqual(
                DirectRichText.Prepare("یک دو سه ", DirectBidi.RightToLeft), lines[0],
                "The FIRST line has to hold the FIRST words. Preparing the paragraph whole puts the LAST words here, which is the bug.");
            Assert.AreEqual(
                DirectRichText.Prepare("چهار پنج", DirectBidi.RightToLeft), lines[1]);
        }

        [Test]
        public void ASpanCrossingASoftBreakOpensAndClosesOnBothLines()
        {
            string shaped = DirectRichText.Shape("<b>یک دو سه</b>");
            int breakAt = shaped.IndexOf(DirectRichText.Shape("سه"), StringComparison.Ordinal);

            string[] lines = DirectRichText
                .PrepareAtBreaks(shaped, new[] { breakAt }, DirectBidi.RightToLeft)
                .Split('\n');

            Assert.AreEqual(2, lines.Length);
            StringAssert.StartsWith("<b>", lines[0], "Line one opens the span…");
            StringAssert.EndsWith("</b>", lines[1], "…and line two closes it.");
        }

        [Test]
        public void ASoftBreakOnTopOfAHardOneDoesNotDoubleIt()
        {
            string shaped = DirectRichText.Shape("یک\nدو");
            int afterNewline = shaped.IndexOf('\n') + 1;

            string prepared = DirectRichText.PrepareAtBreaks(
                shaped, new[] { afterNewline }, DirectBidi.RightToLeft);

            Assert.AreEqual(-1, prepared.IndexOf("\n\n", StringComparison.Ordinal),
                "A renderer reports the break it made at a newline too. Adding another one is a blank line nobody typed.");
        }

        [Test]
        public void NoBreaksAtAllIsJustAPreparation()
        {
            string shaped = DirectRichText.Shape("سلام دنیا");

            Assert.AreEqual(
                DirectRichText.Prepare(shaped, DirectBidi.RightToLeft),
                DirectRichText.PrepareAtBreaks(shaped, new int[0], DirectBidi.RightToLeft));
        }

        [Test]
        public void BreaksOutsideTheTextAreIgnored()
        {
            string shaped = DirectRichText.Shape("سلام دنیا");

            Assert.DoesNotThrow(() =>
                DirectRichText.PrepareAtBreaks(shaped, new[] { -3, 0, 9999 }, DirectBidi.RightToLeft));
        }

        // ==========================================
        // Nothing is lost, nothing is done twice
        // ==========================================

        [Test]
        public void PreparingKeepsEveryVisibleCharacter()
        {
            Assert.AreEqual(
                DirectArabicShaper.Shape("سلام دنیا").Length,
                DirectRichText.Prepare("سلام دنیا").Length,
                "Reordering is a permutation; it must not drop anything.");
        }

        [Test]
        public void ShapingIsStableOnTextThatIsAlreadyShaped()
        {
            // The two-pass layout shapes once and then prepares the result,
            // so the second pass has to be a no-op.
            string once = DirectRichText.Shape("سلام <b>دنیا</b>");

            Assert.AreEqual(once, DirectRichText.Shape(once));
        }

        [Test]
        public void TheNonJoinerNeverReachesTheFont()
        {
            Assert.AreEqual(-1,
                DirectRichText.Prepare("فونت‌ها").IndexOf(DirectArabicShaper.ZeroWidthNonJoiner));
        }
    }
}
