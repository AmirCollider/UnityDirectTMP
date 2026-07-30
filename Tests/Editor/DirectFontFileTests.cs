// ==========================================
// DirectFontFileTests
// The parser is the only place in this package that
// reads bytes somebody else wrote, so it is the only
// place that can be handed something hostile. These
// tests come in two halves:
//
//   what it must read correctly   the family name the
//     foundry wrote, the real glyph count, and exactly
//     which codepoints are mapped - including the case
//     that separates a correct parser from a plausible
//     one, where a format 4 segment spans characters
//     the font does not actually have.
//
//   what it must survive          empty buffers,
//     truncated headers, a table directory claiming
//     more tables than the file has room for, a cmap
//     pointing past the end of the file, and a plain
//     text file renamed to .ttf. Every one of them has
//     to come back as an invalid result with a reason,
//     never as an exception - a corrupt font in a
//     project must produce one bad row in the catalog,
//     not a window that will not open.
// ==========================================
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;

namespace UnityDirectTMP.Editor.Tests
{
    public sealed class DirectFontFileTests
    {
        // ==========================================
        // Reading a well-formed font
        // ==========================================
        [Test]
        public void Parse_ReadsTheFamilyAndStyleTheFoundryWrote()
        {
            byte[] font = SyntheticFont.BuildFormat4("Vazirmatn Test", "Medium", 512,
                new SyntheticFont.Range('A', 'Z'));

            DirectFontFileInfo info = DirectFontFile.Parse(font, "whatever-the-file-is-called.ttf");

            Assert.IsTrue(info.IsValid, info.Error);
            Assert.AreEqual("Vazirmatn Test", info.FamilyName);
            Assert.AreEqual("Medium", info.StyleName);
        }

        [Test]
        public void Parse_ReadsTheGlyphCountAndUnitsPerEm()
        {
            byte[] font = SyntheticFont.BuildFormat4("Test Sans", "Regular", 20342,
                new SyntheticFont.Range('A', 'Z'));

            DirectFontFileInfo info = DirectFontFile.Parse(font);

            Assert.AreEqual(20342, info.GlyphCount);
            Assert.AreEqual(2048, info.UnitsPerEm);
        }

        [Test]
        public void DisplayName_CombinesFamilyAndStyle_ButNotForRegular()
        {
            byte[] bold = SyntheticFont.BuildFormat4("Test Sans", "Bold", 10, new SyntheticFont.Range('A', 'Z'));
            byte[] regular = SyntheticFont.BuildFormat4("Test Sans", "Regular", 10, new SyntheticFont.Range('A', 'Z'));

            Assert.AreEqual("Test Sans Bold", DirectFontFile.Parse(bold).DisplayName);
            Assert.AreEqual("Test Sans", DirectFontFile.Parse(regular).DisplayName,
                "\"Regular\" is the default style and adding it to every name is noise in a list of forty fonts.");
        }

        [Test]
        public void DisplayName_FallsBackToTheFileName_WhenTheFontHasNoFamily()
        {
            byte[] font = SyntheticFont.BuildFormat4(string.Empty, string.Empty, 10,
                new SyntheticFont.Range('A', 'Z'));

            DirectFontFileInfo info = DirectFontFile.Parse(font, "/some/where/subset-2.ttf");

            Assert.AreEqual("subset-2", info.DisplayName,
                "A row with a blank name is a row nobody can act on.");
        }

        // ==========================================
        // Coverage
        // ==========================================
        [Test]
        public void Parse_MapsExactlyTheCodepointsInTheCmap()
        {
            byte[] font = SyntheticFont.BuildFormat4("Test Sans", "Regular", 100,
                new SyntheticFont.Range('A', 'Z'),
                new SyntheticFont.Range('a', 'z'));

            DirectFontFileInfo info = DirectFontFile.Parse(font);

            Assert.IsTrue(info.HasCodepoint('A'));
            Assert.IsTrue(info.HasCodepoint('M'));
            Assert.IsTrue(info.HasCodepoint('Z'));
            Assert.IsTrue(info.HasCodepoint('a'));
            Assert.IsTrue(info.HasCodepoint('z'));

            Assert.IsFalse(info.HasCodepoint('0'), "Digits were never mapped.");
            Assert.IsFalse(info.HasCodepoint('['), "The gap between Z and a must not be swallowed.");
            Assert.IsFalse(info.HasCodepoint(0x3042), "Hiragana was never mapped.");
        }

        [Test]
        public void Parse_CountsMappedCodepoints()
        {
            byte[] font = SyntheticFont.BuildFormat4("Test Sans", "Regular", 100,
                new SyntheticFont.Range('A', 'Z'),   // 26
                new SyntheticFont.Range('a', 'z'));  // 26

            DirectFontFileInfo info = DirectFontFile.Parse(font);

            Assert.AreEqual(52, info.CodepointCount);
        }

        [Test]
        public void Parse_ReadsAstralCodepointsFromAFormat12Cmap()
        {
            // U+1F600 GRINNING FACE is above the BMP, so a format 4 cmap
            // cannot express it at all. A font that ships one has to be read
            // through format 12 or its emoji are invisible to us.
            byte[] font = SyntheticFont.BuildFormat12("Test Emoji", "Regular", 3000,
                new SyntheticFont.Range(0x1F600, 0x1F64F));

            DirectFontFileInfo info = DirectFontFile.Parse(font);

            Assert.IsTrue(info.IsValid, info.Error);
            Assert.IsTrue(info.HasCodepoint(0x1F600));
            Assert.IsTrue(info.HasCodepoint(0x1F64F));
            Assert.IsFalse(info.HasCodepoint(0x1F650));
        }

        [Test]
        public void Parse_ReportsPersianSeparatelyFromArabic()
        {
            // The distinction the whole coverage feature exists for. Both
            // fonts "support Arabic script"; only one of them can set Persian,
            // because Persian needs پ گ چ ژ and Arabic does not have them.
            byte[] arabicOnly = SyntheticFont.BuildFormat4("Arabic Only", "Regular", 400,
                new SyntheticFont.Range(0x0600, 0x064A));

            byte[] withPersian = SyntheticFont.BuildFormat4("Arabic Plus Persian", "Regular", 500,
                new SyntheticFont.Range(0x0600, 0x06FF));

            DirectFontFileInfo a = DirectFontFile.Parse(arabicOnly);
            DirectFontFileInfo b = DirectFontFile.Parse(withPersian);

            Assert.AreEqual(DirectScriptCoverage.Partial,
                DirectFontScripts.CoverageOf(DirectScript.Arabic, a.HasCodepoint),
                "A font without گ or پ cannot set Persian and must not claim full coverage.");

            Assert.AreEqual(DirectScriptCoverage.Full,
                DirectFontScripts.CoverageOf(DirectScript.Arabic, b.HasCodepoint));
        }

        // ==========================================
        // Range merging
        //
        // HasCodepoint binary-searches the range list, so
        // an unsorted or unmerged list is not slow - it is
        // wrong, silently, for some inputs and not others.
        // ==========================================
        [Test]
        public void Merge_SortsOverlappingAndAdjacentRanges()
        {
            var ranges = new List<DirectCodepointRange>
            {
                new DirectCodepointRange(100, 110),
                new DirectCodepointRange(0, 10),
                new DirectCodepointRange(11, 20),      // adjacent to the one above
                new DirectCodepointRange(105, 130),    // overlaps 100..110
            };

            List<DirectCodepointRange> merged = DirectFontFile.Merge(ranges);

            Assert.AreEqual(2, merged.Count);
            Assert.AreEqual(0, merged[0].Start);
            Assert.AreEqual(20, merged[0].End);
            Assert.AreEqual(100, merged[1].Start);
            Assert.AreEqual(130, merged[1].End);
        }

        [Test]
        public void Merge_OfNothingIsNothing()
        {
            Assert.AreEqual(0, DirectFontFile.Merge(null).Count);
            Assert.AreEqual(0, DirectFontFile.Merge(new List<DirectCodepointRange>()).Count);
        }

        [Test]
        public void HasCodepoint_IsCorrectAtEveryBoundary()
        {
            byte[] font = SyntheticFont.BuildFormat4("Test", "Regular", 10,
                new SyntheticFont.Range(0x0100, 0x0200));

            DirectFontFileInfo info = DirectFontFile.Parse(font);

            Assert.IsFalse(info.HasCodepoint(0x00FF), "One below the range.");
            Assert.IsTrue(info.HasCodepoint(0x0100), "The first codepoint in the range.");
            Assert.IsTrue(info.HasCodepoint(0x0200), "The last codepoint in the range.");
            Assert.IsFalse(info.HasCodepoint(0x0201), "One above the range.");
        }

        // ==========================================
        // Surviving bad input
        // ==========================================
        [Test]
        public void Parse_OfNullIsInvalidRatherThanAnException()
        {
            DirectFontFileInfo info = DirectFontFile.Parse(null);

            Assert.IsFalse(info.IsValid);
            Assert.IsNotEmpty(info.Error);
        }

        [Test]
        public void Parse_OfAnEmptyBufferIsInvalid()
        {
            DirectFontFileInfo info = DirectFontFile.Parse(new byte[0]);

            Assert.IsFalse(info.IsValid);
            Assert.IsNotEmpty(info.Error);
        }

        [Test]
        public void Parse_OfATextFileRenamedToTtfIsInvalid()
        {
            // The commonest "corrupt font" in the wild by a wide margin: a Git
            // LFS pointer file committed where the .ttf should be.
            byte[] pointer = Encoding.ASCII.GetBytes(
                "version https://git-lfs.github.com/spec/v1\noid sha256:abcdef\nsize 1234\n");

            DirectFontFileInfo info = DirectFontFile.Parse(pointer, "NotoSansJP-Regular.ttf");

            Assert.IsFalse(info.IsValid);
            Assert.IsNotEmpty(info.Error);
        }

        [Test]
        public void Parse_OfATruncatedFontIsInvalid()
        {
            byte[] font = SyntheticFont.BuildFormat4("Test", "Regular", 10, new SyntheticFont.Range('A', 'Z'));

            // Keep the header and throw away the tables.
            var truncated = new byte[40];
            System.Array.Copy(font, truncated, System.Math.Min(font.Length, truncated.Length));

            DirectFontFileInfo info = DirectFontFile.Parse(truncated);

            Assert.IsFalse(info.IsValid);
        }

        [Test]
        public void Parse_OfEveryPrefixOfAValidFontNeverThrows()
        {
            // The strongest statement available without a fuzzer: every
            // possible truncation point of a real, valid font must produce a
            // result rather than an exception.
            byte[] font = SyntheticFont.BuildFormat4("Test Sans", "Regular", 400,
                new SyntheticFont.Range('A', 'Z'),
                new SyntheticFont.Range(0x3040, 0x309F));

            for (int length = 0; length <= font.Length; length++)
            {
                var prefix = new byte[length];
                System.Array.Copy(font, prefix, length);

                DirectFontFileInfo info = null;
                Assert.DoesNotThrow(() => info = DirectFontFile.Parse(prefix),
                    $"Parsing the first {length} bytes threw.");
                Assert.IsNotNull(info);
            }
        }

        [Test]
        public void Parse_OfACorruptedByteAtEveryOffsetNeverThrows()
        {
            byte[] font = SyntheticFont.BuildFormat4("Test Sans", "Regular", 400,
                new SyntheticFont.Range('A', 'Z'));

            for (int at = 0; at < font.Length; at++)
            {
                var corrupted = (byte[])font.Clone();
                corrupted[at] = 0xFF;

                Assert.DoesNotThrow(() => DirectFontFile.Parse(corrupted),
                    $"Flipping byte {at} to 0xFF threw.");
            }
        }

        [Test]
        public void Parse_OfAFontWithNoCmapSaysSoRatherThanReportingEmptyCoverage()
        {
            byte[] font = SyntheticFont.BuildWithoutCmap("No Cmap", 50);

            DirectFontFileInfo info = DirectFontFile.Parse(font);

            Assert.IsFalse(info.IsValid);
            Assert.IsNotEmpty(info.Error,
                "\"No characters\" and \"could not be read\" look identical in a list; the row has to say which.");
        }

        [Test]
        public void Read_OfAMissingFileIsInvalidRatherThanAnException()
        {
            DirectFontFileInfo info = DirectFontFile.Read("/definitely/not/a/real/path/font.ttf");

            Assert.IsFalse(info.IsValid);
            Assert.IsNotEmpty(info.Error);
        }

        [Test]
        public void Read_OfNullPathIsInvalid()
        {
            DirectFontFileInfo info = DirectFontFile.Read(null);

            Assert.IsFalse(info.IsValid);
        }
    }
}
