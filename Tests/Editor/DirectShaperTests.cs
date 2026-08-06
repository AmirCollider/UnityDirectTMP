// ==========================================
// DirectShaperTests
// The shaping rules, checked against the corpus that was
// diffed glyph-for-glyph against HarfBuzz while this
// pipeline was being written.
//
// These run with no font: DirectShaper's default
// resolver emits Unicode presentation forms, so the
// SHAPE each letter was put in is visible in the output
// and can be asserted on directly. Which codepoint a
// real font ends up drawing that shape with is
// DirectFontJoiner's business and needs a Unity font
// asset, which an EditMode test has no business
// building.
// ==========================================
using NUnit.Framework;
using UnityDirectTMP;

namespace UnityDirectTMP.Tests
{
    public class DirectJoiningTests
    {
        [Test]
        public void DualJoiningLettersAreDual()
        {
            foreach (char c in "بپتثجچحخسشصضطظعغفقكکگلمنهیي")
            {
                Assert.AreEqual(DirectJoiningClass.Dual, DirectJoining.ClassOf(c), $"{c} should be dual-joining");
            }
        }

        [Test]
        public void RightJoiningLettersAreRight()
        {
            foreach (char c in "اآأإدذرزژوؤةۇۆۈ")
            {
                Assert.AreEqual(DirectJoiningClass.Right, DirectJoining.ClassOf(c), $"{c} should be right-joining");
            }
        }

        [Test]
        public void HamzaJoinsNothing()
        {
            // It has a presentation form because it is drawn, not because it
            // connects. Reading it as right-joining puts the letter before it
            // into an initial shape with nothing to connect to.
            Assert.AreEqual(DirectJoiningClass.None, DirectJoining.ClassOf('ء'));
        }

        [Test]
        public void HarakatAreTransparent()
        {
            foreach (char c in "ًٌٍَُِّْٰ")
            {
                Assert.AreEqual(DirectJoiningClass.Transparent, DirectJoining.ClassOf(c));
            }
        }

        [Test]
        public void JoiningIsNotDerivedFromPresentationForms()
        {
            // The bug this whole rewrite exists for. These letters join, and
            // Unicode never gave them a presentation codepoint - so a table
            // built out of presentation forms reports them as joining nothing
            // and the words they appear in come apart.
            foreach (char c in "ࢠݐݪ")
            {
                Assert.AreEqual(DirectJoiningClass.Dual, DirectJoining.ClassOf(c),
                    $"U+{(int)c:X4} joins, with or without a presentation codepoint");
            }
        }

        [Test]
        public void PlainTextNeedsNoShaping()
        {
            Assert.IsFalse(DirectJoining.NeedsShaping("Hello, world"));
            Assert.IsFalse(DirectJoining.NeedsShaping("こんにちは"));
            Assert.IsFalse(DirectJoining.NeedsShaping(string.Empty));
            Assert.IsTrue(DirectJoining.NeedsShaping("سلام"));
        }
    }

    public class DirectShaperFormTests
    {
        private static void AssertForms(string word, params DirectForm[] expected)
        {
            Assert.AreEqual(expected.Length, word.Length, $"test itself is wrong for '{word}'");

            for (int i = 0; i < word.Length; i++)
            {
                Assert.AreEqual(expected[i], DirectShaper.FormAt(word, i),
                    $"'{word}' letter {i} ({word[i]})");
            }
        }

        [Test]
        public void SalamJoinsThroughout()
        {
            // س initial, ل medial, ا final, م isolated
            AssertForms("سلام",
                DirectForm.Initial, DirectForm.Medial, DirectForm.Final, DirectForm.Isolated);
        }

        [Test]
        public void RightJoiningLettersBreakTheChain()
        {
            // پ initial, ر final, و isolated, ژ isolated, ه isolated
            AssertForms("پروژه",
                DirectForm.Initial, DirectForm.Final, DirectForm.Isolated,
                DirectForm.Isolated, DirectForm.Isolated);
        }

        [Test]
        public void WordsOfOnlyRightJoiningLettersNeverNeedAJoinedShape()
        {
            // These are the words that looked correct even when the old
            // implementation shaped nothing at all - which is exactly why the
            // bug read as "some words work and some do not".
            foreach (string word in new[] { "درد", "راز", "زود", "دارا", "رود" })
            {
                for (int i = 0; i < word.Length; i++)
                {
                    DirectForm form = DirectShaper.FormAt(word, i);
                    Assert.IsTrue(form == DirectForm.Isolated || form == DirectForm.Final,
                        $"'{word}' letter {i} should never be initial or medial");
                }
            }
        }

        [Test]
        public void ZeroWidthNonJoinerBreaksTheJoin()
        {
            // می‌شه - Persian's half-space. The ی must be FINAL, not medial,
            // and the ش must be INITIAL, not medial.
            const string word = "می‌شه";

            Assert.AreEqual(DirectForm.Initial, DirectShaper.FormAt(word, 0), "م");
            Assert.AreEqual(DirectForm.Final, DirectShaper.FormAt(word, 1), "ی before ZWNJ");
            Assert.AreEqual(DirectForm.Initial, DirectShaper.FormAt(word, 3), "ش after ZWNJ");
            Assert.AreEqual(DirectForm.Final, DirectShaper.FormAt(word, 4), "ه");
        }

        [Test]
        public void ZeroWidthNonJoinerIsNotDrawn()
        {
            Assert.IsFalse(DirectShaper.Shape("می‌شه").Contains("‌"));
        }

        [Test]
        public void HarakatDoNotBreakAJoin()
        {
            // A fatha between two letters is drawn above the join, not in it.
            const string word = "بَب";   // beh, fatha, beh

            Assert.AreEqual(DirectForm.Initial, DirectShaper.FormAt(word, 0));
            Assert.AreEqual(DirectForm.Final, DirectShaper.FormAt(word, 2));
        }

        [Test]
        public void TatweelJoinsBothSides()
        {
            const string word = "بـب";

            Assert.AreEqual(DirectForm.Initial, DirectShaper.FormAt(word, 0));
            Assert.AreEqual(DirectForm.Final, DirectShaper.FormAt(word, 2));
        }
    }

    public class DirectShaperOutputTests
    {
        [Test]
        public void TextWithNoArabicIsReturnedUnchanged()
        {
            const string text = "Hello, world 123";
            Assert.AreSame(text, DirectShaper.Shape(text));
        }

        [Test]
        public void LamAlefBecomesOneGlyph()
        {
            Assert.AreEqual("ﻻ", DirectShaper.Shape("لا"), "isolated lam-alef");

            // The lam is joined to the kaf before it, so the ligature takes
            // its final shape - and the alef is inside the glyph, so three
            // letters come out as two.
            Assert.AreEqual("ﮐﻼ", DirectShaper.Shape("کلا"), "final lam-alef");
        }

        [Test]
        public void LamAlefShortensTheString()
        {
            // Two codepoints in, one out.
            Assert.AreEqual(1, DirectShaper.Shape("لا").Length);
            Assert.AreEqual(3, DirectShaper.Shape("سلام").Length);
        }

        [Test]
        public void EveryLetterOfAPersianSentenceIsShaped()
        {
            foreach (string word in new[]
            {
                "سلام", "پروژه", "کتاب", "فونت", "ایران", "دنیا", "نور", "خوب",
                "بازی", "برنامه", "تست", "مرد", "خانه", "دوست", "بزرگ", "کوچک",
                "نوشتن", "فارسی", "زبان", "همیشه", "دانشگاه", "کتابخانه",
            })
            {
                string shaped = DirectShaper.Shape(word);

                foreach (char c in shaped)
                {
                    Assert.IsTrue(DirectJoining.IsPresentationForm(c),
                        $"'{word}' -> '{shaped}': {c} (U+{(int)c:X4}) was left unshaped");
                }
            }
        }
    }

    public class DirectRichTextTests
    {
        [Test]
        public void TagsAreLeftAlone()
        {
            string result = DirectRichText.Apply("<b>abc</b>", s => s.ToUpperInvariant());
            Assert.AreEqual("<b>ABC</b>", result);
        }

        [Test]
        public void TagsWithAttributesAreLeftAlone()
        {
            string result = DirectRichText.Apply("<color=#ff0000>x</color>", s => "Y");
            Assert.AreEqual("<color=#ff0000>Y</color>", result);
        }

        [Test]
        public void ALoneAngleBracketIsOrdinaryText()
        {
            // "a < b" is a comparison, not a broken tag, and must survive.
            Assert.AreEqual("A < B", DirectRichText.Apply("a < b", s => s.ToUpperInvariant()));
            Assert.IsFalse(DirectRichText.HasTags("a < b"));
        }

        [Test]
        public void TextWithNoTagsIsTransformedWhole()
        {
            Assert.AreEqual("ABC", DirectRichText.Apply("abc", s => s.ToUpperInvariant()));
        }

        [Test]
        public void ArabicInsideATagIsStillShaped()
        {
            string result = DirectRichText.Apply("<b>سلام</b>", DirectShaper.Shape);

            Assert.IsTrue(result.StartsWith("<b>"), "opening tag survived");
            Assert.IsTrue(result.EndsWith("</b>"), "closing tag survived");
            Assert.IsFalse(result.Contains("س"), "the text between the tags was shaped");
        }
    }
}
