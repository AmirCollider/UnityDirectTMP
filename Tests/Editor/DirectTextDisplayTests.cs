// ==========================================
// DirectTextDisplayTests
// The shaper, the bidi reorderer and the pieces of
// the editor that hand text to them.
//
// This is the file that would have caught the bug it
// was written for. Unity DirectTMP 1.0 shipped with
// every Persian string in its own interface unjoined
// and mirrored - in a package whose entire subject is
// getting text on screen correctly - and nothing in
// the suite noticed, because nothing in the suite
// looked at what a string turns into on its way to a
// label.
//
// The expectations are written as codepoints rather
// than as pasted glyphs. A test that asserts one
// Arabic string equals another Arabic string tells a
// reader nothing when it fails, and is one careless
// normalization away from asserting nothing at all.
// ==========================================
using NUnit.Framework;
using System.Collections.Generic;

namespace UnityDirectTMP.Editor.Tests
{
    public sealed class DirectTextDisplayTests
    {
        // ==========================================
        // Shaping
        // ==========================================

        [Test]
        public void EnglishIsReturnedUntouched()
        {
            const string text = "Font Catalog 1.0.0";

            Assert.AreSame(text, DirectArabicShaper.Shape(text));
            Assert.AreSame(text, DirectDisplayText.Prepare(text));
        }

        [Test]
        public void JapaneseIsReturnedUntouched()
        {
            const string text = "フォントカタログ";

            Assert.AreSame(text, DirectDisplayText.Prepare(text));
        }

        [Test]
        public void LettersTakeTheFormTheirNeighboursCallFor()
        {
            // فونت - feh, waw, noon, teh.
            //   feh  has something after it and nothing before  -> initial
            //   waw  joins backwards only                       -> final
            //   noon starts again after the waw                 -> initial
            //   teh  ends the word                              -> final
            Assert.AreEqual(
                "\uFED3\uFEEE\uFEE7\uFE96",
                DirectArabicShaper.Shape("فونت"));
        }

        [Test]
        public void LamAndAlefBecomeOneGlyph()
        {
            // Standing alone.
            Assert.AreEqual("\uFEFB", DirectArabicShaper.Shape("لا"));

            // With a letter in front of it, the ligature takes its final form
            // and the letter before takes an initial one.
            Assert.AreEqual("\uFE91\uFEFC", DirectArabicShaper.Shape("بلا"));

            // سلام is three glyphs, not four - which is the whole point.
            Assert.AreEqual("\uFEB3\uFEFC\uFEE1", DirectArabicShaper.Shape("سلام"));
        }

        [Test]
        public void ZeroWidthNonJoinerBreaksTheJoinAndLeaves()
        {
            // می‌شه, with the non-joiner Persian actually writes it with:
            // the yeh must NOT connect to the sheen.
            string broken = DirectArabicShaper.Shape("می‌شه");
            Assert.AreEqual("\uFEE3\uFBFD\uFEB7\uFEEA", broken);

            // Same letters without it: the yeh is medial and the sheen picks
            // up the connection.
            Assert.AreEqual("\uFEE3\uFBFF\uFEB8\uFEEA", DirectArabicShaper.Shape("میشه"));

            // And the non-joiner itself is gone - it has no glyph worth
            // asking a font for.
            Assert.AreEqual(-1, broken.IndexOf(DirectArabicShaper.ZeroWidthNonJoiner));
        }

        [Test]
        public void VowelMarksDoNotBreakAJoin()
        {
            // بَب - a fatha over the first beh. The two behs still connect.
            string shaped = DirectArabicShaper.Shape("بَب");

            Assert.AreEqual("\uFE91\u064E\uFE90", shaped,
                "A mark is drawn above the join, not in it.");
        }

        [Test]
        public void HamzaJoinsNothing()
        {
            // ء has a presentation form but no connections. The beh in front
            // of it must stay isolated rather than reach for one.
            Assert.AreEqual("\uFE8F\uFE80", DirectArabicShaper.Shape("بء"));
        }

        [Test]
        public void ThePersianLettersArabicDoesNotHaveAreShapedToo()
        {
            // پگچژ - peh, gaf, cheh, jeh. A shaping table lifted from an
            // Arabic-only source has none of these, and Persian is unreadable
            // without them.
            Assert.AreEqual(
                "\uFB58\uFB95\uFB7D\uFB8B",
                DirectArabicShaper.Shape("پگچژ"));
        }

        // ==========================================
        // A font with a HOLE in it, rather than a font
        // without the block.
        //
        // The shaper asks, per form, whether the font can
        // actually draw it - because a modern face built for a
        // real shaping engine carries the plain letters and its
        // joining rules in OpenType, with nothing at U+FE70, and
        // shaping into a form it has no glyph for turns readable
        // text into a row of boxes.
        //
        // The rule used to be all-or-nothing: one missing form
        // and the whole letter went out plain. For a
        // right-joining letter that is one missing codepoint out
        // of two, and it did not stop at that letter - a letter
        // that cannot be joined tells its NEIGHBOURS not to reach
        // for it, so the letter before it dropped from its
        // initial shape to its isolated one as well. A hole where
        // reh's isolated form should be came out as a broken ش,
        // and only in words where something follows the reh:
        // شروع and کردم fell apart, قبر and صبر did not.
        // ==========================================
        [Test]
        public void ALetterKeepsJoiningWhenOnlyItsIsolatedFormIsMissing()
        {
            // Everything except reh's isolated form (U+FEAD). Its final
            // form is right there, and the word needs no other.
            System.Func<char, bool> hasGlyph = c => c != '\uFEAD';

            // شروع - sheen, reh, waw, ain. The sheen must stay INITIAL:
            // it is joined to the reh after it.
            Assert.AreEqual(
                "\uFEB7\uFEAE\uFEED\uFEC9",
                DirectArabicShaper.Shape("شروع", hasGlyph, null),
                "the sheen joins forward to a reh the font can still draw joined");

            Assert.IsTrue(DirectArabicShaper.CanJoin('ر', hasGlyph));
        }

        // Standing alone, that same reh has to be drawn with
        // something - and the plain letter IS the isolated shape,
        // so there is always an answer.
        [Test]
        public void AMissingIsolatedFormFallsBackToThePlainLetter()
        {
            System.Func<char, bool> hasGlyph = c => c != '\uFEAD';

            Assert.AreEqual("ر", DirectArabicShaper.Shape("ر", hasGlyph, null));
        }

        // The old rule is still right for the case it was written
        // for: a letter with no joined shape at all connects to
        // nothing, and its neighbours must be told so rather than
        // drawing a connecting stroke into empty space.
        [Test]
        public void ALetterWithNoJoinedFormAtAllStillStandsAlone()
        {
            // A font with the plain letters and none of the Arabic
            // presentation block - which is most faces designed for a
            // real shaping engine.
            System.Func<char, bool> hasGlyph = c => !DirectArabicShaper.IsPresentationForm(c);

            Assert.AreEqual("شروع", DirectArabicShaper.Shape("شروع", hasGlyph, null));
            Assert.IsFalse(DirectArabicShaper.CanJoin('ر', hasGlyph));
            Assert.IsFalse(DirectArabicShaper.CanJoin('ش', hasGlyph));
        }

        [Test]
        public void EveryJoiningClassIsReportedCorrectly()
        {
            Assert.AreEqual(DirectJoining.Dual, DirectArabicShaper.JoiningOf('ب'));
            Assert.AreEqual(DirectJoining.Right, DirectArabicShaper.JoiningOf('ا'));
            Assert.AreEqual(DirectJoining.None, DirectArabicShaper.JoiningOf('ء'));
            Assert.AreEqual(DirectJoining.Causing, DirectArabicShaper.JoiningOf(DirectArabicShaper.Tatweel));
            Assert.AreEqual(DirectJoining.Transparent, DirectArabicShaper.JoiningOf('َ'));
            Assert.AreEqual(DirectJoining.None, DirectArabicShaper.JoiningOf('A'));
        }

        // ==========================================
        // Reordering
        // ==========================================

        [Test]
        public void LeftToRightTextIsNotTouched()
        {
            const string text = "Hello world 42";

            Assert.AreSame(text, DirectBidi.Reorder(text));
            Assert.IsFalse(DirectBidi.ContainsRightToLeft(text));
        }

        [Test]
        public void RightToLeftTextIsReversed()
        {
            // Hebrew, which needs no shaping - so this tests the reordering
            // and nothing else.
            Assert.AreEqual("גבא", DirectBidi.Reorder("אבג"));
        }

        [Test]
        public void ALatinRunInsideRightToLeftTextStillReadsForwards()
        {
            // This is the rule that matters most for this package: half its
            // sentences name TextMeshPro, .ttf or Convert ▸ Whole Project.
            Assert.AreEqual("דג TMP בא", DirectBidi.Reorder("אב TMP גד"));
        }

        [Test]
        public void ARightToLeftRunInsideLatinTextStaysWhereItIs()
        {
            Assert.AreEqual("a בא b", DirectBidi.Reorder("a אב b"));
        }

        [Test]
        public void TheFullStopOfARightToLeftSentenceGoesToTheLeft()
        {
            Assert.AreEqual(".בא", DirectBidi.Reorder("אב."));
        }

        [Test]
        public void DigitsKeepAscendingInEitherDirection()
        {
            Assert.AreEqual("12 בא", DirectBidi.Reorder("אב 12"));
            Assert.AreEqual("a 12 b", DirectBidi.Reorder("a 12 b"));
        }

        [Test]
        public void BracketsAreMirrored()
        {
            // The opening bracket has to be drawn as a closing one, because
            // the font has no idea which way the sentence runs.
            Assert.AreEqual("(בא)", DirectBidi.Reorder("(אב)"));
        }

        [Test]
        public void EachLineIsAParagraphOfItsOwn()
        {
            Assert.AreEqual("בא\nHello", DirectBidi.Reorder("אב\nHello"));
        }

        [Test]
        public void ParagraphDirectionComesFromTheFirstStrongCharacter()
        {
            Assert.IsTrue(DirectBidi.IsRightToLeftParagraph("سلام TextMeshPro"));
            Assert.IsFalse(DirectBidi.IsRightToLeftParagraph("TextMeshPro سلام"));
            Assert.IsFalse(DirectBidi.IsRightToLeftParagraph("1.0.0"),
                "No strong character at all reads left to right.");
        }

        [Test]
        public void ForcingTheDirectionOverridesTheFirstWord()
        {
            // A Persian sentence that happens to open with a Latin product
            // name is still a Persian sentence, and the interface knows that
            // even though the string does not.
            const string text = "TextMeshPro خوب است";

            string auto = DirectDisplayText.Prepare(text, DirectBidi.AutoDirection);
            string forced = DirectDisplayText.Prepare(text, DirectBidi.RightToLeft);

            Assert.AreNotEqual(auto, forced);
            Assert.IsTrue(forced.EndsWith("TextMeshPro"),
                "In a right-to-left paragraph the Latin run belongs at the visual right.");
            Assert.IsTrue(auto.StartsWith("TextMeshPro"));
        }

        // ==========================================
        // Shaping and reordering together
        // ==========================================

        [Test]
        public void PrepareShapesFirstAndReordersSecond()
        {
            // Doing it the other way round joins each letter to the wrong
            // neighbour, which looks almost right and is completely wrong.
            Assert.AreEqual(
                "\uFE8E\uFBFF\uFEE7\uFEA9 \uFEE1\uFEFC\uFEB3",
                DirectDisplayText.Prepare("سلام دنیا"));
        }

        [Test]
        public void PreparingTwiceDoesNotUndoIt()
        {
            // Text travels through several helpers on its way to a label. A
            // second pass reversing the first would produce exactly the bug
            // this file exists to prevent, and be far harder to find.
            string once = DirectDisplayText.Prepare("سلام دنیا");

            Assert.AreEqual(once, DirectDisplayText.Prepare(once));
            Assert.IsTrue(DirectDisplayText.LooksPrepared(once));
            Assert.IsFalse(DirectDisplayText.LooksPrepared("سلام دنیا"));
        }

        [Test]
        public void AnEmojiSurvivesBeingReordered()
        {
            // A surrogate pair is two chars that mean one character. Reversing
            // between them produces a lone surrogate, which renders as nothing
            // anywhere.
            const string emoji = "\U0001F600";

            string prepared = DirectDisplayText.Prepare("سلام " + emoji);

            StringAssert.Contains(emoji, prepared);
        }

        [Test]
        public void NullAndEmptyGoStraightThrough()
        {
            Assert.IsNull(DirectDisplayText.Prepare(null));
            Assert.AreEqual(string.Empty, DirectDisplayText.Prepare(string.Empty));
            Assert.IsNull(DirectArabicShaper.Shape(null));
            Assert.IsNull(DirectBidi.Reorder(null));
        }

        // ==========================================
        // Wrapping
        // ==========================================

        [Test]
        public void WrappedLinesComeOutInReadingOrderTopToBottom()
        {
            // The failure this guards against: reorder the paragraph first and
            // the renderer breaks it into lines that read bottom to top.
            List<string> lines = DirectDisplayText.WrapToLines("אב גד הו", 5f, Measure);

            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual("דג בא", lines[0], "The first line holds the first words.");
            Assert.AreEqual("וה", lines[1]);
        }

        [Test]
        public void WrappingRespectsLineBreaksAlreadyInTheText()
        {
            List<string> lines = DirectDisplayText.WrapToLines("אב\nגד", 100f, Measure);

            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual("בא", lines[0]);
            Assert.AreEqual("דג", lines[1]);
        }

        [Test]
        public void AWordTooLongForTheLineGetsALineRatherThanBeingSplit()
        {
            List<string> lines = DirectDisplayText.WrapToLines("אבגדהוזח", 3f, Measure);

            Assert.AreEqual(1, lines.Count, "Breaking inside a joined word is worse than overhanging.");
        }

        [Test]
        public void WrappingWithoutAMeasureStillProducesUsableText()
        {
            List<string> lines = DirectDisplayText.WrapToLines("אב גד", 40f, null);

            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual("דג בא", lines[0]);
        }

        // ==========================================
        // The editor's own funnel
        // ==========================================

        [Test]
        public void PickingAStringStillHonoursTheLanguage()
        {
            // Raw() is the logical half of L() and must not have changed.
            string original = DirectTMPText.Current;
            try
            {
                DirectTMPText.Current = DirectTMPText.Persian;
                Assert.AreEqual("یک", DirectTMPText.Raw("one", "いち", "یک"));

                DirectTMPText.Current = DirectTMPText.Japanese;
                Assert.AreEqual("いち", DirectTMPText.Raw("one", "いち", "یک"));
            }
            finally
            {
                DirectTMPText.Current = original;
            }
        }

        [Test]
        public void TheDisplayFormOfALocalisedStringIsPrepared()
        {
            string original = DirectTMPText.Current;
            try
            {
                DirectTMPText.Current = DirectTMPText.Persian;

                string raw = DirectTMPText.Raw("Close", "閉じる", "بستن");
                string shown = DirectTMPText.L("Close", "閉じる", "بستن");

                Assert.AreNotEqual(raw, shown, "A Persian label drawn as stored is drawn backwards.");
                Assert.AreEqual(DirectDisplayText.Prepare(raw, DirectBidi.RightToLeft), shown);
            }
            finally
            {
                DirectTMPText.Current = original;
            }
        }

        [Test]
        public void FormattingHappensBeforeReordering()
        {
            // string.Format over an already-reordered format string would drop
            // the number into a sentence that has been turned around - and the
            // braces themselves are mirrored characters.
            string original = DirectTMPText.Current;
            try
            {
                DirectTMPText.Current = DirectTMPText.Persian;

                string formatted = DirectTMPText.F("{0} font(s)", "{0} 件", "{0} فونت", 12);

                StringAssert.Contains("12", formatted);
                Assert.AreEqual(-1, formatted.IndexOf('{'), "The placeholder was not filled in.");
            }
            finally
            {
                DirectTMPText.Current = original;
            }
        }

        [Test]
        public void EveryLanguageLabelIsDrawable()
        {
            Assert.AreEqual(DirectTMPText.Labels.Length, DirectTMPText.DisplayLabels.Length);

            for (int i = 0; i < DirectTMPText.Labels.Length; i++)
            {
                Assert.AreEqual(
                    DirectDisplayText.Prepare(DirectTMPText.Labels[i]),
                    DirectTMPText.DisplayLabels[i]);
            }
        }

        [Test]
        public void ShowLeavesDataThatNeedsNothingAlone()
        {
            const string path = "Assets/Fonts/Vazirmatn-Regular.ttf";

            Assert.AreSame(path, DirectTMPText.Show(path));
        }

        // ==========================================
        // The two text systems
        //
        // Unity's editor draws text two different ways,
        // and they need opposite treatment:
        //
        //   IMGUI    the inside of a window. Joins
        //            nothing, reorders nothing - so the
        //            package prepares the text.
        //   the OS   a Popup's item list, the menu bar,
        //            a right-click menu, a modal dialog.
        //            Has a full text stack and does both
        //            itself - so the package must NOT.
        //
        // Getting the second one wrong is invisible until
        // somebody opens a dropdown, and then the item
        // reads backwards while the button that opened it
        // reads correctly. These tests are the guard.
        // ==========================================

        [Test]
        public void TextForAnOsDrawnSurfaceIsNotPrepared()
        {
            string original = DirectTMPText.Current;
            try
            {
                DirectTMPText.Current = DirectTMPText.Persian;

                const string en = "Any script";
                const string fa = "همه‌ی خط‌ها";

                Assert.AreEqual(fa, DirectTMPText.Native(en, "すべての文字体系", fa),
                    "A dropdown item is reordered by the OS; preparing it here does it twice.");
                Assert.AreNotEqual(
                    DirectTMPText.L(en, "すべての文字体系", fa),
                    DirectTMPText.Native(en, "すべての文字体系", fa),
                    "If these two agree, one of the two text systems is being served wrong.");

                Assert.AreEqual(DirectTMPText.WordRaw("close"), DirectTMPText.NativeWord("close"));
                Assert.AreEqual(fa, DirectTMPText.NativeShow(fa));
            }
            finally
            {
                DirectTMPText.Current = original;
            }
        }

        [Test]
        public void ThePersianMenuItemIsStoredUnprepared()
        {
            // A [MenuItem] path goes to the OS menu bar, which shapes and
            // reorders it. 1.0.1 briefly wrote this one out pre-prepared,
            // which turned it around twice.
            StringAssert.Contains("فارسی", DirectTMPEditorConstants.MenuLanguagePersian);

            Assert.IsFalse(
                DirectDisplayText.LooksPrepared(DirectTMPEditorConstants.MenuLanguagePersian),
                "The menu bar prepares its own text; this must go in as stored.");
        }

        [Test]
        public void TheLanguageDropdownUsesStoredNamesAndTheLabelUsesPreparedOnes()
        {
            // Labels[] feeds Popup, which the OS draws. DisplayLabels[] feeds
            // the IMGUI read-out in the About window. They must not be swapped.
            int persian = DirectTMPText.IndexOf(DirectTMPText.Persian);

            Assert.IsFalse(DirectDisplayText.LooksPrepared(DirectTMPText.Labels[persian]));
            Assert.IsTrue(DirectDisplayText.LooksPrepared(DirectTMPText.DisplayLabels[persian]));
        }

        [Test]
        public void EveryTranslatedStringInTheToolSurvivesBeingPrepared()
        {
            // A broad sweep rather than a specific claim: preparing any of the
            // tool's own strings must not lose characters, throw, or leave a
            // stray non-joiner behind. It is the cheapest guard there is
            // against a shaping table with a hole in it.
            string original = DirectTMPText.Current;
            try
            {
                DirectTMPText.Current = DirectTMPText.Persian;

                string[] keys =
                {
                    "apply", "revert", "close", "cancel", "refresh", "settings",
                    "language", "font", "preview", "none", "size", "glyphs", "scripts"
                };

                foreach (string key in keys)
                {
                    string raw = DirectTMPText.WordRaw(key);
                    string shown = DirectTMPText.Word(key);

                    Assert.IsNotEmpty(shown, key);
                    Assert.AreEqual(-1, shown.IndexOf(DirectArabicShaper.ZeroWidthNonJoiner), key);
                    Assert.AreEqual(CountVisible(raw), CountVisible(shown),
                        $"'{key}' lost or gained a character on its way to the screen.");
                }
            }
            finally
            {
                DirectTMPText.Current = original;
            }
        }

        // ==========================================
        // Helpers
        // ==========================================

        // One unit per character: enough to test where a break lands without
        // needing a GUIStyle or a running Editor.
        private static float Measure(string text)
        {
            return text.Length;
        }

        // Characters that end up as a glyph. The non-joiner is consumed by
        // shaping and a lam-alef ligature turns two codepoints into one, so a
        // plain length comparison would report both as losses.
        private static int CountVisible(string text)
        {
            int count = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == DirectArabicShaper.ZeroWidthNonJoiner) { continue; }

                // A lam followed by an alef is one glyph on the far side.
                if (c == 'ل' && i + 1 < text.Length && IsAlef(text[i + 1])) { i++; }

                count++;
            }
            return count;
        }

        private static bool IsAlef(char c)
        {
            return c == 'ا' || c == 'آ' || c == 'أ' || c == 'إ';
        }
    }
}
