// ==========================================
// DirectFontGsubTests
//
// DirectFontGsub is the answer to the bug that survived
// 1.1.0 and 1.1.1: a font carrying the Arabic
// presentation block and not the Persian one, which
// joins ر و ه ن correctly and leaves پ چ ژ گ standing
// alone in the middle of the word. The fix is to stop
// treating the presentation blocks as the source of
// truth and ask the font how IT joins its own letters -
// which every font that sets Arabic can answer, through
// its OpenType 'init' / 'medi' / 'fina' lookups.
//
// That makes this a parser of somebody else's bytes,
// with the same two obligations as DirectFontFile:
//
//   what it must read correctly   both single
//     substitution formats, the coverage ORDER that
//     indexes the format 2 array - get that wrong by
//     one and a word comes out spelled with the
//     neighbouring letter, which is worse than the bug
//     being fixed - and a rule hidden behind a type 7
//     extension lookup, which is how a large font says
//     the same thing.
//
//   what it must survive          truncation at every
//     level, a script that is not Arabic, a font with
//     no GSUB at all, and lookup types it is not
//     allowed to guess at. All of them mean "this font
//     cannot help", and all of them have to come back
//     as an empty answer rather than an exception - the
//     shaper then falls back to exactly what it did
//     before, which is the whole safety story of the
//     feature.
// ==========================================
using NUnit.Framework;
using System.Collections.Generic;

namespace UnityDirectTMP.Editor.Tests
{
    public sealed class DirectFontGsubTests
    {
        // The six letters Persian adds to the Arabic alphabet - the ones that
        // shape into U+FB50..FBFF and go missing from half the fonts on
        // Windows.
        private const char Peh = 'پ';      // پ
        private const char Tcheh = 'چ';    // چ
        private const char Jeh = 'ژ';      // ژ
        private const char Keheh = 'ک';    // ک
        private const char Gaf = 'گ';      // گ
        private const char FarsiYeh = 'ی'; // ی

        private static readonly char[] Persian = { Peh, Tcheh, Jeh, Keheh, Gaf, FarsiYeh };

        private static SyntheticFont.Range ArabicBlock => new SyntheticFont.Range(0x0600, 0x06FF);

        // ==========================================
        // What it must read correctly
        // ==========================================

        [Test]
        public void Read_FindsTheGlyphAFontJoinsPehWith()
        {
            // A format 2 rule: every letter names its own substitute, which is
            // how a font whose joined forms are scattered through the glyph
            // order has to say it.
            byte[] font = SyntheticFont.BuildWithGsub(
                "Joins Persian", 4096, new[] { ArabicBlock }, "arab",
                SyntheticFont.Substitution.Format2("init", new[] { (int)Peh }, new[] { 900 }),
                SyntheticFont.Substitution.Format2("medi", new[] { (int)Peh }, new[] { 901 }),
                SyntheticFont.Substitution.Format2("fina", new[] { (int)Peh }, new[] { 902 }));

            DirectJoinedGlyphs joined = DirectFontGsub.Read(font, Persian);

            Assert.IsTrue(joined.IsValid, joined.Error);
            Assert.IsTrue(joined.HasJoiningFeatures, joined.Error);

            Assert.AreEqual(900u, joined.GlyphIndex(Peh, DirectJoiningForm.Initial));
            Assert.AreEqual(901u, joined.GlyphIndex(Peh, DirectJoiningForm.Medial));
            Assert.AreEqual(902u, joined.GlyphIndex(Peh, DirectJoiningForm.Final));
        }

        [Test]
        public void Read_ResolvesAFormatOneRuleByItsDelta()
        {
            // Format 1: one shift for the whole covered run, which is what a
            // font whose joined forms sit in a contiguous block writes.
            byte[] font = SyntheticFont.BuildWithGsub(
                "Contiguous", 8192, new[] { ArabicBlock }, "arab",
                SyntheticFont.Substitution.Format1("init", 500, Peh, Tcheh, Gaf));

            DirectJoinedGlyphs joined = DirectFontGsub.Read(font, Persian);

            Assert.AreEqual((uint)(SyntheticFont.GlyphFor(Peh) + 500), joined.GlyphIndex(Peh, DirectJoiningForm.Initial));
            Assert.AreEqual((uint)(SyntheticFont.GlyphFor(Tcheh) + 500), joined.GlyphIndex(Tcheh, DirectJoiningForm.Initial));
            Assert.AreEqual((uint)(SyntheticFont.GlyphFor(Gaf) + 500), joined.GlyphIndex(Gaf, DirectJoiningForm.Initial));
        }

        [Test]
        public void Read_KeepsEachLetterWithItsOwnGlyphAcrossACoveredRun()
        {
            // The one that matters most. Coverage order indexes the substitute
            // array, so an off-by-one here hands پ the glyph belonging to چ -
            // and the word is not drawn wrong, it is SPELLED wrong, which no
            // reader forgives and no screenshot makes obvious.
            int[] letters = { Peh, Tcheh, Jeh, Keheh, Gaf, FarsiYeh };
            int[] glyphs = { 701, 702, 703, 704, 705, 706 };

            byte[] font = SyntheticFont.BuildWithGsub(
                "Ordered", 4096, new[] { ArabicBlock }, "arab",
                SyntheticFont.Substitution.Format2("medi", letters, glyphs));

            DirectJoinedGlyphs joined = DirectFontGsub.Read(font, Persian);

            for (int i = 0; i < letters.Length; i++)
            {
                Assert.AreEqual(
                    (uint)glyphs[i],
                    joined.GlyphIndex((char)letters[i], DirectJoiningForm.Medial),
                    $"U+{letters[i]:X4} took the wrong glyph.");
            }
        }

        [Test]
        public void Read_FollowsAnExtensionLookupToTheRuleBehindIt()
        {
            byte[] font = SyntheticFont.BuildWithGsub(
                "Large", 60000, new[] { ArabicBlock }, "arab",
                new SyntheticFont.Substitution
                {
                    Feature = "fina",
                    Codepoints = new[] { (int)FarsiYeh },
                    Glyphs = new[] { 40000 },
                    Extension = true
                });

            DirectJoinedGlyphs joined = DirectFontGsub.Read(font, Persian);

            Assert.AreEqual(40000u, joined.GlyphIndex(FarsiYeh, DirectJoiningForm.Final));
        }

        [Test]
        public void Read_TreatsTheNominalGlyphAsTheIsolatedForm()
        {
            // Most fonts leave 'isol' out, because for most letters the
            // isolated shape IS the glyph the cmap already points at. Reading
            // that as "no isolated form" would strand every letter that only
            // ever stands alone.
            byte[] font = SyntheticFont.BuildWithGsub(
                "No isol", 4096, new[] { ArabicBlock }, "arab",
                SyntheticFont.Substitution.Format2("init", new[] { (int)Peh }, new[] { 900 }));

            DirectJoinedGlyphs joined = DirectFontGsub.Read(font, Persian);

            Assert.AreEqual((uint)SyntheticFont.GlyphFor(Peh), joined.GlyphIndex(Peh, DirectJoiningForm.Isolated));
            Assert.AreEqual((uint)SyntheticFont.GlyphFor(Peh), joined.NominalGlyph(Peh));
        }

        // ==========================================
        // "Covered, and substituted with itself" is an
        // answer, not silence.
        //
        // A great many faces draw ر, ز, و and د identically
        // standing alone and at the end of a word, so their
        // 'fina' lookup names the letter and maps it to the same
        // glyph. Reading that as "no final form" left U+FEAE
        // unregistered in the font asset, the shaper then treated
        // reh as a letter it could not join, and - because a
        // letter that joins nothing tells its NEIGHBOURS not to
        // reach for it - the ش before it dropped out of its
        // initial shape too. "شروع" and "کردم" came apart; "قبر"
        // and "صبر", where nothing follows the ر, did not.
        // ==========================================
        [Test]
        public void Read_TreatsACoveredButUnchangedFormAsAForm()
        {
            const char Reh = 'ر';
            byte[] font = SyntheticFont.BuildWithGsub(
                "Reh final == reh isolated", 4096, new[] { ArabicBlock }, "arab",
                // The font's own final reh, which happens to be its
                // nominal glyph - delta 0.
                SyntheticFont.Substitution.Format1("fina", 0, Reh),
                SyntheticFont.Substitution.Format2("init", new[] { (int)Peh }, new[] { 900 }));

            DirectJoinedGlyphs joined = DirectFontGsub.Read(font, new[] { Reh, Peh });

            Assert.AreEqual((uint)SyntheticFont.GlyphFor(Reh), joined.GlyphIndex(Reh, DirectJoiningForm.Final),
                "the font named reh in 'fina'; the glyph it named is the answer, moved or not");
        }

        // The counter-rule, and the reason the one above cannot
        // simply record every letter for every wired feature: an
        // 'init' lookup that covers only پ says nothing whatever
        // about ب, and claiming otherwise would have the shaper
        // emit a joined form no glyph exists for.
        [Test]
        public void Read_RecordsNothingForALetterAFeatureNeverCovered()
        {
            byte[] font = SyntheticFont.BuildWithGsub(
                "init for peh only", 4096, new[] { ArabicBlock }, "arab",
                SyntheticFont.Substitution.Format2("init", new[] { (int)Peh }, new[] { 900 }));

            DirectJoinedGlyphs joined = DirectFontGsub.Read(font, Persian);

            Assert.AreEqual(900u, joined.GlyphIndex(Peh, DirectJoiningForm.Initial));
            Assert.AreEqual(0u, joined.GlyphIndex(Gaf, DirectJoiningForm.Initial));
            Assert.AreEqual(0u, joined.GlyphIndex(Gaf, DirectJoiningForm.Medial));
        }

        // A self-substitution is a form, but it is not evidence
        // that the font does any joining - so it must not be what
        // makes HasJoiningFeatures true. A font whose Arabic
        // script wires up nothing real still has to come back
        // saying so, or DirectFontForms rasterizes a pile of
        // plain glyphs under joined codepoints.
        [Test]
        public void Read_DoesNotCountASelfSubstitutionAsJoiningEvidence()
        {
            const char Reh = 'ر';
            byte[] font = SyntheticFont.BuildWithGsub(
                "fina that moves nothing", 4096, new[] { ArabicBlock }, "arab",
                SyntheticFont.Substitution.Format1("fina", 0, Reh));

            DirectJoinedGlyphs joined = DirectFontGsub.Read(font, new[] { Reh });

            Assert.IsTrue(joined.IsValid, joined.Error);
            Assert.IsFalse(joined.HasJoiningFeatures,
                "nothing moved, so this font has demonstrated no joining");
        }

        [Test]
        public void Read_LetsAnExplicitIsolFeatureWinOverTheNominalGlyph()
        {
            byte[] font = SyntheticFont.BuildWithGsub(
                "Has isol", 4096, new[] { ArabicBlock }, "arab",
                SyntheticFont.Substitution.Format2("isol", new[] { (int)Keheh }, new[] { 950 }));

            DirectJoinedGlyphs joined = DirectFontGsub.Read(font, Persian);

            Assert.AreEqual(950u, joined.GlyphIndex(Keheh, DirectJoiningForm.Isolated));
        }

        [Test]
        public void Read_ReportsOnlyTheLettersItWasAskedFor()
        {
            byte[] font = SyntheticFont.BuildWithGsub(
                "Whole alphabet", 4096, new[] { ArabicBlock }, "arab",
                SyntheticFont.Substitution.Format1("init", 500, 0x0628, Peh, 0x062A));

            DirectJoinedGlyphs joined = DirectFontGsub.Read(font, new[] { Peh });

            Assert.AreEqual(1, joined.LetterCount);
            Assert.IsTrue(joined.Has(Peh, DirectJoiningForm.Initial));
            Assert.AreEqual(0u, joined.GlyphIndex('ب', DirectJoiningForm.Initial));
        }

        // ==========================================
        // What it must survive
        // ==========================================

        [Test]
        public void Read_SaysSoWhenTheRulesBelongToAnotherScript()
        {
            // A font can carry an 'init' for Syriac and nothing for Arabic.
            // Borrowing it would draw a Syriac letter inside a Persian word.
            byte[] font = SyntheticFont.BuildWithGsub(
                "Syriac only", 4096, new[] { ArabicBlock }, "syrc",
                SyntheticFont.Substitution.Format2("init", new[] { (int)Peh }, new[] { 900 }));

            DirectJoinedGlyphs joined = DirectFontGsub.Read(font, Persian);

            Assert.IsTrue(joined.IsValid, "The font itself is perfectly readable.");
            Assert.IsFalse(joined.HasJoiningFeatures);
            Assert.AreEqual(0u, joined.GlyphIndex(Peh, DirectJoiningForm.Initial));
        }

        [Test]
        public void Read_SaysSoWhenTheFontHasNoGsubAtAll()
        {
            byte[] font = SyntheticFont.BuildFormat4("Plain", "Regular", 4096, ArabicBlock);

            DirectJoinedGlyphs joined = DirectFontGsub.Read(font, Persian);

            Assert.IsTrue(joined.IsValid);
            Assert.IsFalse(joined.HasJoiningFeatures);
            Assert.IsNotEmpty(joined.Error);
        }

        [Test]
        public void Read_SaysSoWhenTheFontHasNoneOfThoseLetters()
        {
            byte[] font = SyntheticFont.BuildFormat4("Latin only", "Regular", 512,
                new SyntheticFont.Range('A', 'Z'));

            DirectJoinedGlyphs joined = DirectFontGsub.Read(font, Persian);

            Assert.AreEqual(0, joined.LetterCount);
            Assert.IsFalse(joined.HasJoiningFeatures);
        }

        [Test]
        public void Read_SurvivesEveryTruncationOfAFontThatWasValid()
        {
            byte[] font = SyntheticFont.BuildWithGsub(
                "Truncate me", 4096, new[] { ArabicBlock }, "arab",
                SyntheticFont.Substitution.Format2("init", new[] { (int)Peh }, new[] { 900 }),
                SyntheticFont.Substitution.Format1("fina", 500, Peh, Gaf));

            // Every prefix, not a sampled few: the offsets inside GSUB point
            // forwards, so each byte removed is a different read walking off
            // the end.
            for (int length = 0; length < font.Length; length++)
            {
                var truncated = new byte[length];
                System.Array.Copy(font, truncated, length);

                DirectJoinedGlyphs joined = DirectFontGsub.Read(truncated, Persian);
                Assert.IsNotNull(joined, $"Truncated to {length} bytes returned null.");
            }
        }

        [Test]
        public void Read_SurvivesCorruptionAtEveryOffset()
        {
            byte[] original = SyntheticFont.BuildWithGsub(
                "Corrupt me", 4096, new[] { ArabicBlock }, "arab",
                SyntheticFont.Substitution.Format2("medi", new[] { (int)Peh, (int)Gaf }, new[] { 900, 901 }));

            for (int i = 0; i < original.Length; i++)
            {
                var corrupt = (byte[])original.Clone();
                corrupt[i] = 0xFF;

                DirectJoinedGlyphs joined = DirectFontGsub.Read(corrupt, Persian);
                Assert.IsNotNull(joined, $"A 0xFF at byte {i} returned null.");
            }
        }

        [Test]
        public void Read_HandlesNothingGracefully()
        {
            Assert.IsFalse(DirectFontGsub.Read(null, Persian).IsValid);
            Assert.IsFalse(DirectFontGsub.Read(new byte[0], Persian).IsValid);
            Assert.IsFalse(DirectFontGsub.Read(new byte[] { 1, 2, 3 }, Persian).IsValid);

            byte[] font = SyntheticFont.BuildFormat4("Fine", "Regular", 512, ArabicBlock);
            Assert.IsFalse(DirectFontGsub.Read(font, null).IsValid);
            Assert.IsFalse(DirectFontGsub.Read(font, new List<char>()).IsValid);
        }

        [Test]
        public void Read_IsNotFooledByAPlainTextFileRenamedToTtf()
        {
            byte[] text = System.Text.Encoding.UTF8.GetBytes(
                "This is not a font. It is a README somebody dragged into the wrong folder.");

            DirectJoinedGlyphs joined = DirectFontGsub.Read(text, Persian);

            Assert.IsFalse(joined.IsValid);
            Assert.IsNotEmpty(joined.Error);
        }

        // ==========================================
        // "Has Arabic" is not "can set Arabic"
        //
        // The distinction this whole verdict exists for.
        // A coverage badge that goes green on ا ب پ says
        // a font supports the script; Arial Unicode MS
        // passes that test, cannot connect a single
        // letter, and sets پروژه as five separate
        // shapes. Somebody picked it, saw broken Persian,
        // and had nothing anywhere telling them the font
        // was why.
        // ==========================================
        private static readonly SyntheticFont.Range PresentationFormsA = new SyntheticFont.Range(0xFB50, 0xFBFF);
        private static readonly SyntheticFont.Range PresentationFormsB = new SyntheticFont.Range(0xFE70, 0xFEFF);

        [Test]
        public void Joining_IsPresentationFormsWhenTheFontCarriesThemOutright()
        {
            byte[] font = SyntheticFont.BuildFormat4("Complete", "Regular", 8192,
                ArabicBlock, PresentationFormsA, PresentationFormsB);

            DirectFontFileInfo info = DirectFontFile.Parse(font);

            Assert.AreEqual(DirectJoiningSupport.PresentationForms, info.ArabicJoining);
            Assert.IsTrue(info.JoinsArabicScript);
            Assert.IsEmpty(info.UnjoinableLetters);
        }

        [Test]
        public void Joining_IsOpenTypeWhenTheFormsAreMissingButTheRulesAreNot()
        {
            // Segoe UI's situation, and the one 1.2.0 exists to rescue.
            byte[] font = SyntheticFont.BuildWithGsub(
                "Rules only", 8192, new[] { ArabicBlock }, "arab",
                SyntheticFont.Substitution.Format1("init", 1000,
                    Peh, Tcheh, Jeh, Keheh, Gaf, FarsiYeh, 'ب', 'س'));

            DirectFontFileInfo info = DirectFontFile.Parse(font);

            Assert.AreEqual(DirectJoiningSupport.OpenType, info.ArabicJoining);
            Assert.IsTrue(info.JoinsArabicScript);
            Assert.IsEmpty(info.UnjoinableLetters);
        }

        [Test]
        public void Joining_IsLettersOnlyWhenTheFontHasNeither()
        {
            // The font the user actually had. Every Persian letter present,
            // no way to connect any of them.
            byte[] font = SyntheticFont.BuildFormat4("Arial Unicode-like", "Regular", 50000, ArabicBlock);

            DirectFontFileInfo info = DirectFontFile.Parse(font);

            Assert.AreEqual(DirectJoiningSupport.LettersOnly, info.ArabicJoining);
            Assert.IsFalse(info.JoinsArabicScript);

            foreach (char letter in Persian)
            {
                StringAssert.Contains(letter.ToString(), info.UnjoinableLetters);
            }
        }

        [Test]
        public void Joining_SaysNothingAboutAFontWithNoArabicInIt()
        {
            byte[] font = SyntheticFont.BuildFormat4("Latin only", "Regular", 512,
                new SyntheticFont.Range('A', 'Z'));

            DirectFontFileInfo info = DirectFontFile.Parse(font);

            Assert.AreEqual(DirectJoiningSupport.NotArabicScript, info.ArabicJoining);
            Assert.IsFalse(info.JoinsArabicScript);
            Assert.IsEmpty(info.UnjoinableLetters);
        }

        [Test]
        public void Joining_NamesOnlyTheLettersThatCannotBeJoined()
        {
            // Forms-B in full and no Forms-A: the alphabet joins, and exactly
            // the letters that make it Persian do not. That is the shape of
            // the original bug report, and the read-out has to say so rather
            // than condemning the whole font.
            byte[] font = SyntheticFont.BuildFormat4("Forms-B only", "Regular", 8192,
                ArabicBlock, PresentationFormsB);

            DirectFontFileInfo info = DirectFontFile.Parse(font);

            Assert.AreEqual(DirectJoiningSupport.LettersOnly, info.ArabicJoining);

            // پ چ ژ گ have no stand-in and are named.
            StringAssert.Contains(Peh.ToString(), info.UnjoinableLetters);
            StringAssert.Contains(Tcheh.ToString(), info.UnjoinableLetters);
            StringAssert.Contains(Jeh.ToString(), info.UnjoinableLetters);
            StringAssert.Contains(Gaf.ToString(), info.UnjoinableLetters);

            // ک and ی do, and the rest of the alphabet was never in doubt.
            StringAssert.DoesNotContain(Keheh.ToString(), info.UnjoinableLetters);
            StringAssert.DoesNotContain(FarsiYeh.ToString(), info.UnjoinableLetters);
            StringAssert.DoesNotContain("ب", info.UnjoinableLetters);
            StringAssert.DoesNotContain("س", info.UnjoinableLetters);
        }

        // ==========================================
        // The alphabet the shaper and this file share
        // ==========================================

        [Test]
        public void EveryLetterTheShaperJoinsCanBeAskedAboutHere()
        {
            // DirectFontForms walks DirectArabicShaper.JoiningLetters and asks
            // this file about each one. Two lists of Persian letters that could
            // drift apart would be one list too many.
            var letters = new List<char>(DirectArabicShaper.JoiningLetters);

            Assert.Greater(letters.Count, 60, "The shaper's alphabet has shrunk.");
            foreach (char letter in Persian)
            {
                Assert.Contains(letter, letters, $"The shaper no longer joins U+{(int)letter:X4}.");
            }

            var forms = new char[4];
            foreach (char letter in letters)
            {
                Assert.IsTrue(DirectArabicShaper.TryGetForms(letter, forms),
                    $"U+{(int)letter:X4} is listed as joining but has no forms.");
                Assert.AreNotEqual('\0', forms[(int)DirectJoiningForm.Isolated],
                    $"U+{(int)letter:X4} has no isolated form.");
            }
        }

        [Test]
        public void TryGetForms_RefusesADestinationItCannotFill()
        {
            Assert.IsFalse(DirectArabicShaper.TryGetForms(Peh, null));
            Assert.IsFalse(DirectArabicShaper.TryGetForms(Peh, new char[3]));
            Assert.IsFalse(DirectArabicShaper.TryGetForms('A', new char[4]));
        }
    }
}
