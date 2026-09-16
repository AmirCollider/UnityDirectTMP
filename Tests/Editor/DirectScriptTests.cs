// ==========================================
// DirectScriptTests
// Which writing system a string is in, and which font
// that answer sends a label to.
//
// The whole reason this file exists is one bug that was
// invisible for thirteen releases: DirectScripts
// classified a `char`, and a char is a UTF-16 code UNIT.
// Every emoji worth the name lives above U+FFFF, so it
// arrived as two lone surrogates, neither of which is a
// letter - and a label of pure emoji reported having no
// writing system at all. Nothing crashed and nothing
// logged; the feature simply could not be built on top of
// it, because there was nothing there to win a vote.
//
// So the first group below is astral codepoints, and
// every one of those cases fails against 2.1.13.
// ==========================================
using NUnit.Framework;
using UnityDirectTMP;

namespace UnityDirectTMP.Tests
{
    public class DirectScriptTests
    {
        // ------------------------------------------------------
        // Astral codepoints: the bug this release fixes.
        // ------------------------------------------------------

        [Test]
        public void AnEmojiIsOneCodepointNotTwoSurrogates()
        {
            Assert.AreEqual(DirectScript.Emoji, DirectScripts.DominantSpecificOf("\U0001F600"));
        }

        [Test]
        public void ALabelOfEmojiIsEmoji()
        {
            Assert.AreEqual(DirectScript.Emoji, DirectScripts.DominantSpecificOf("\U0001F600\U0001F680\U0001F9E0"));
        }

        [Test]
        public void CuneiformIsCountedAtAll()
        {
            // Not Emoji and not None: an astral script with no field of its
            // own lands in Other, which is what a custom rule then overrides.
            Assert.AreEqual(DirectScript.Other, DirectScripts.DominantSpecificOf("\U00012000\U00012001\U00012002"));
        }

        [Test]
        public void ALoneSurrogateHasNoScript()
        {
            Assert.AreEqual(DirectScript.None, DirectScripts.SpecificOf(0xD83D));
            Assert.AreEqual(DirectScript.None, DirectScripts.SpecificOf(0xDE00));
        }

        [Test]
        public void CodepointAtStepsOverASurrogatePair()
        {
            string text = "a\U0001F600b";
            int i = 0;

            Assert.AreEqual('a', DirectScripts.CodepointAt(text, ref i));
            Assert.AreEqual(1, i);
            Assert.AreEqual(0x1F600, DirectScripts.CodepointAt(text, ref i));
            Assert.AreEqual(3, i, "the pair is two chars, so the index moves by two");
            Assert.AreEqual('b', DirectScripts.CodepointAt(text, ref i));
            Assert.AreEqual(4, i);
        }

        // ------------------------------------------------------
        // Emoji must not steal text that merely contains one.
        // ------------------------------------------------------

        [Test]
        public void OneEmojiDoesNotHijackAPersianSentence()
        {
            Assert.AreEqual(DirectScript.Arabic, DirectScripts.DominantSpecificOf("سلام دنیا \U0001F600"));
        }

        [Test]
        public void MostlyEmojiWins()
        {
            Assert.AreEqual(DirectScript.Emoji, DirectScripts.DominantSpecificOf("\U0001F600\U0001F601\U0001F602 hi"));
        }

        [Test]
        public void EmojiModifiersDoNotVote()
        {
            // A variation selector and a zero-width joiner are how emoji
            // sequences are built. On their own they are invisible and belong
            // to whatever they follow, so counting them as emoji would let a
            // single joiner outvote a word.
            Assert.AreEqual(DirectScript.None, DirectScripts.SpecificOf(0xFE0F));
            Assert.AreEqual(DirectScript.None, DirectScripts.SpecificOf(0x200D));
        }

        [Test]
        public void DigitsAreNotKeycapEmoji()
        {
            // '3' is a keycap base only inside a keycap sequence. A phone
            // number is not emoji.
            Assert.AreEqual(DirectScript.None, DirectScripts.SpecificOf('3'));
            Assert.AreEqual(DirectScript.None, DirectScripts.DominantSpecificOf("0912 345 6789"));
        }

        // ------------------------------------------------------
        // CJK, split three ways.
        // ------------------------------------------------------

        [Test]
        public void KanaIsJapanese()
        {
            Assert.AreEqual(DirectScript.Japanese, DirectScripts.DominantSpecificOf("こんにちは"));
        }

        [Test]
        public void JapaneseWithKanjiIsStillJapanese()
        {
            // Japanese always carries kana, so its own kanji cannot out-vote
            // it into Chinese. This is the case that decides whether a
            // Japanese label gets a Japanese or a Chinese face.
            Assert.AreEqual(DirectScript.Japanese, DirectScripts.DominantSpecificOf("日本語のテキストです"));
        }

        [Test]
        public void HanWithoutKanaIsChinese()
        {
            Assert.AreEqual(DirectScript.Chinese, DirectScripts.DominantSpecificOf("中文文本内容"));
        }

        [Test]
        public void HangulIsKorean()
        {
            Assert.AreEqual(DirectScript.Korean, DirectScripts.DominantSpecificOf("한국어 텍스트"));
        }

        [Test]
        public void AllThreeStillGroupAsCjk()
        {
            // The group is what DominantOf returns and what DirectFont.Script
            // reports, and it must not have changed: code written against
            // 2.1.13 compares against DirectScript.Cjk.
            Assert.AreEqual(DirectScript.Cjk, DirectScripts.DominantOf("こんにちは"));
            Assert.AreEqual(DirectScript.Cjk, DirectScripts.DominantOf("中文文本内容"));
            Assert.AreEqual(DirectScript.Cjk, DirectScripts.DominantOf("한국어 텍스트"));
            Assert.AreEqual(DirectScript.Cjk, DirectScripts.Of('あ'));
        }

        // ------------------------------------------------------
        // Nothing that worked before may have changed.
        // ------------------------------------------------------

        [Test]
        public void TheOldAnswersAreUnchanged()
        {
            Assert.AreEqual(DirectScript.Arabic, DirectScripts.DominantOf("سلام دنیا"));
            Assert.AreEqual(DirectScript.Latin, DirectScripts.DominantOf("Hello world"));
            Assert.AreEqual(DirectScript.Cyrillic, DirectScripts.DominantOf("Привет мир"));
            Assert.AreEqual(DirectScript.None, DirectScripts.DominantOf(""));
            Assert.AreEqual(DirectScript.None, DirectScripts.DominantOf("123 ..."));
        }

        [Test]
        public void DigitsAndPunctuationStillDoNotVote()
        {
            Assert.AreEqual(DirectScript.Arabic, DirectScripts.DominantOf("Unity ۱۲۳ سلام دنیا"));
        }

        [Test]
        public void DescribeNamesEveryScriptPresent()
        {
            string described = DirectScripts.Describe("Hello سلام こんにちは \U0001F600");

            Assert.IsTrue(described.Contains("Latin"), described);
            Assert.IsTrue(described.Contains("Persian/Arabic"), described);
            Assert.IsTrue(described.Contains("Japanese"), described);
            Assert.IsTrue(described.Contains("emoji"), described);
        }

        // ------------------------------------------------------
        // The user-defined rules.
        // ------------------------------------------------------

        [Test]
        public void RangesParseInEveryFormPeopleType()
        {
            bool ok;

            CollectionAssert.AreEqual(new[] { 0x12000, 0x123FF },
                DirectFontRule.ParseRanges("12000-123FF", out ok));
            Assert.IsTrue(ok);

            CollectionAssert.AreEqual(new[] { 0x12000, 0x123FF },
                DirectFontRule.ParseRanges("U+12000-U+123FF", out ok));
            Assert.IsTrue(ok);

            CollectionAssert.AreEqual(new[] { 0x12000, 0x123FF },
                DirectFontRule.ParseRanges("0x12000-0x123FF", out ok));
            Assert.IsTrue(ok);

            // An en dash, which is what a copy-paste out of a Unicode PDF
            // produces. Failing on an invisible difference between two dashes
            // is a rule nobody can debug.
            CollectionAssert.AreEqual(new[] { 0x12000, 0x123FF },
                DirectFontRule.ParseRanges("12000–123FF", out ok));
            Assert.IsTrue(ok);
        }

        [Test]
        public void ASingleCodepointIsARangeOfOne()
        {
            bool ok;
            CollectionAssert.AreEqual(new[] { 0x1F600, 0x1F600 },
                DirectFontRule.ParseRanges("1F600", out ok));
            Assert.IsTrue(ok);
        }

        [Test]
        public void AReversedRangeIsSwappedRatherThanDropped()
        {
            bool ok;
            CollectionAssert.AreEqual(new[] { 0x12000, 0x123FF },
                DirectFontRule.ParseRanges("123FF-12000", out ok));
            Assert.IsTrue(ok);
        }

        [Test]
        public void OneTypoCostsThatRangeAndNotTheOthers()
        {
            bool ok;
            int[] parsed = DirectFontRule.ParseRanges("12000-123FF, zzz, 12400-1247F", out ok);

            Assert.IsFalse(ok, "the caller is told something was unreadable");
            CollectionAssert.AreEqual(new[] { 0x12000, 0x123FF, 0x12400, 0x1247F }, parsed);
        }

        [Test]
        public void NonsenseAndOutOfRangeAreRefused()
        {
            bool ok;

            Assert.AreEqual(0, DirectFontRule.ParseRanges("zzz", out ok).Length);
            Assert.IsFalse(ok);

            Assert.AreEqual(0, DirectFontRule.ParseRanges("110000", out ok).Length, "past U+10FFFF");
            Assert.IsFalse(ok);

            Assert.AreEqual(0, DirectFontRule.ParseRanges("1234567", out ok).Length, "too long");
            Assert.IsFalse(ok);
        }

        [Test]
        public void EmptyRangesAreNotAnError()
        {
            bool ok;
            Assert.AreEqual(0, DirectFontRule.ParseRanges("", out ok).Length);
            Assert.IsTrue(ok, "an empty rule is unfinished, not wrong");

            Assert.AreEqual(0, DirectFontRule.ParseRanges("   ", out ok).Length);
            Assert.IsTrue(ok);
        }

        [Test]
        public void ARuleMatchesInsideItsRangesAndNotOutside()
        {
            DirectFontRule rule = new DirectFontRule { ranges = "12000-123FF, 1F600" };

            Assert.IsTrue(rule.Matches(0x12000), "the low end is inclusive");
            Assert.IsTrue(rule.Matches(0x12200));
            Assert.IsTrue(rule.Matches(0x123FF), "the high end is inclusive");
            Assert.IsTrue(rule.Matches(0x1F600), "a single codepoint matches itself");

            Assert.IsFalse(rule.Matches(0x11FFF));
            Assert.IsFalse(rule.Matches(0x12400));
            Assert.IsFalse(rule.Matches('a'));
        }

        [Test]
        public void ARuleWithNoFontIsNotUsable()
        {
            DirectFontRule rule = new DirectFontRule { ranges = "12000-123FF" };

            Assert.IsFalse(rule.IsUsable, "no font, so nothing to choose");
            Assert.AreEqual(1, rule.RangeCount, "the ranges still parsed");
        }

        [Test]
        public void UnreadableRangesAreReportedRatherThanSilent()
        {
            DirectFontRule rule = new DirectFontRule { ranges = "not hex at all" };

            Assert.IsTrue(rule.HasUnreadableRanges);
            Assert.IsFalse(rule.IsUsable);
        }

        [Test]
        public void EditingTheRangesReparsesThem()
        {
            // The parsed form is cached, because Matches is asked once per
            // codepoint of every label that changes text. The cache must not
            // outlive the string it came from.
            DirectFontRule rule = new DirectFontRule { ranges = "12000-123FF" };
            Assert.IsTrue(rule.Matches(0x12100));

            rule.ranges = "1F600-1F64F";
            Assert.IsFalse(rule.Matches(0x12100), "the old ranges are gone");
            Assert.IsTrue(rule.Matches(0x1F601), "the new ones are in force");
        }
    }
}
