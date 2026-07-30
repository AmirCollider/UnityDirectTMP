// ==========================================
// DirectFontCatalogFilterTests
// The catalog's search box, script filter and four
// sort orders.
//
// None of this is complicated, and all of it is the
// kind of code that ships broken in a specific way:
// an empty query that matches nothing instead of
// everything, a sort that is not total so the list
// reshuffles between two identical scans, a script
// filter that hides the partial matches which are
// exactly what somebody filtering for Arabic wants to
// see.
// ==========================================
using NUnit.Framework;
using System.Collections.Generic;

namespace UnityDirectTMP.Editor.Tests
{
    public sealed class DirectFontCatalogFilterTests
    {
        // ==========================================
        // Fixtures
        // ==========================================
        private static DirectCatalogEntry Entry(
            string display,
            string file = null,
            string assetPath = null,
            int glyphs = 100,
            int fullyCovered = 1,
            bool usable = true,
            params DirectScript[] fullScripts)
        {
            var info = usable
                ? DirectFontFile.Parse(SyntheticFont.BuildFormat4(display, "Regular", glyphs,
                    new SyntheticFont.Range('A', 'Z')))
                : DirectFontFile.Parse(new byte[] { 1, 2, 3 });

            var entry = new DirectCatalogEntry
            {
                Source = assetPath != null ? DirectCatalogSource.Project : DirectCatalogSource.System,
                DisplayName = display,
                FileName = file ?? (display + ".ttf"),
                AssetPath = assetPath ?? string.Empty,
                AbsolutePath = "/fonts/" + (file ?? (display + ".ttf")),
                Info = info,
                FullyCoveredCount = fullyCovered
            };

            entry.Coverage = new Dictionary<DirectScript, DirectScriptCoverage>();
            foreach (DirectScript script in DirectFontScripts.All)
            {
                entry.Coverage[script] = DirectScriptCoverage.None;
            }
            foreach (DirectScript script in fullScripts)
            {
                entry.Coverage[script] = DirectScriptCoverage.Full;
            }

            return entry;
        }

        // The size and glyph count come from the parsed synthetic font, so the
        // sort tests set them directly rather than fighting the fixture.
        private static DirectCatalogEntry Sized(string display, int glyphs, long bytes)
        {
            DirectCatalogEntry entry = Entry(display, glyphs: glyphs);
            entry.Info.FileBytes = bytes;
            return entry;
        }

        // ==========================================
        // Query
        // ==========================================
        [Test]
        public void AnEmptyQueryMatchesEverything()
        {
            DirectCatalogEntry entry = Entry("Noto Sans JP");

            Assert.IsTrue(DirectFontCatalogFilter.MatchesQuery(entry, null));
            Assert.IsTrue(DirectFontCatalogFilter.MatchesQuery(entry, string.Empty));
            Assert.IsTrue(DirectFontCatalogFilter.MatchesQuery(entry, "   "),
                "Whitespace is somebody who has not finished typing, not a filter for nothing.");
        }

        [Test]
        public void QueryMatchingIsCaseInsensitive()
        {
            DirectCatalogEntry entry = Entry("Noto Sans JP");

            Assert.IsTrue(DirectFontCatalogFilter.MatchesQuery(entry, "noto"));
            Assert.IsTrue(DirectFontCatalogFilter.MatchesQuery(entry, "NOTO"));
            Assert.IsTrue(DirectFontCatalogFilter.MatchesQuery(entry, "SaNs"));
        }

        [Test]
        public void QueryMatchesTheFileNameAndThePathToo()
        {
            // People look for a font by all of these, and a box that only
            // matches the display name feels broken to the person who
            // remembers the folder rather than the family.
            DirectCatalogEntry entry = Entry("Vazirmatn", file: "Vazirmatn-Medium.ttf",
                assetPath: "Assets/Localization/Fonts/Vazirmatn-Medium.ttf");

            Assert.IsTrue(DirectFontCatalogFilter.MatchesQuery(entry, "Medium"));
            Assert.IsTrue(DirectFontCatalogFilter.MatchesQuery(entry, "Localization"));
        }

        [Test]
        public void QueryDoesNotMatchAnUnrelatedString()
        {
            Assert.IsFalse(DirectFontCatalogFilter.MatchesQuery(Entry("Noto Sans JP"), "Comic"));
        }

        [Test]
        public void QueryAgainstANullEntryIsFalseRatherThanAnException()
        {
            Assert.IsFalse(DirectFontCatalogFilter.MatchesQuery(null, "anything"));
        }

        // ==========================================
        // Script filter
        // ==========================================
        [Test]
        public void NoScriptFilterMatchesEverything()
        {
            Assert.IsTrue(DirectFontCatalogFilter.MatchesScript(Entry("Anything"), null));
        }

        [Test]
        public void ScriptFilterMatchesFullCoverage()
        {
            DirectCatalogEntry entry = Entry("Noto Sans JP", fullScripts: DirectScript.Kana);

            Assert.IsTrue(DirectFontCatalogFilter.MatchesScript(entry, DirectScript.Kana));
            Assert.IsFalse(DirectFontCatalogFilter.MatchesScript(entry, DirectScript.Hebrew));
        }

        [Test]
        public void ScriptFilterAlsoMatchesPartialCoverage()
        {
            // Somebody filtering for Arabic wants to be shown the font that
            // has most of it, with its amber badge — not told there are no
            // results and left to find it by scrolling.
            DirectCatalogEntry entry = Entry("Half Arabic");
            entry.Coverage[DirectScript.Arabic] = DirectScriptCoverage.Partial;

            Assert.IsTrue(DirectFontCatalogFilter.MatchesScript(entry, DirectScript.Arabic));
        }

        // ==========================================
        // Sorting
        // ==========================================
        [Test]
        public void SortByNameIsAlphabeticalAndCaseInsensitive()
        {
            var entries = new List<DirectCatalogEntry>
            {
                Entry("zeta"), Entry("Alpha"), Entry("mid")
            };

            List<DirectCatalogEntry> sorted =
                DirectFontCatalogFilter.Apply(entries, null, null, DirectCatalogSort.Name);

            Assert.AreEqual("Alpha", sorted[0].DisplayName);
            Assert.AreEqual("mid", sorted[1].DisplayName);
            Assert.AreEqual("zeta", sorted[2].DisplayName);
        }

        [Test]
        public void SortByGlyphsPutsTheBiggestFirst()
        {
            var entries = new List<DirectCatalogEntry>
            {
                Sized("Small", 200, 1000),
                Sized("Huge", 40000, 1000),
                Sized("Medium", 3000, 1000)
            };

            List<DirectCatalogEntry> sorted =
                DirectFontCatalogFilter.Apply(entries, null, null, DirectCatalogSort.Glyphs);

            Assert.AreEqual("Huge", sorted[0].DisplayName);
            Assert.AreEqual("Small", sorted[2].DisplayName);
        }

        [Test]
        public void SortBySizePutsTheLargestFileFirst()
        {
            var entries = new List<DirectCatalogEntry>
            {
                Sized("Tiny", 100, 12_000),
                Sized("Enormous", 100, 22_000_000),
                Sized("Middling", 100, 400_000)
            };

            List<DirectCatalogEntry> sorted =
                DirectFontCatalogFilter.Apply(entries, null, null, DirectCatalogSort.Size);

            Assert.AreEqual("Enormous", sorted[0].DisplayName);
            Assert.AreEqual("Tiny", sorted[2].DisplayName);
        }

        [Test]
        public void SortByScriptsPutsTheMostMultilingualFirst()
        {
            var entries = new List<DirectCatalogEntry>
            {
                Entry("LatinOnly", fullyCovered: 1),
                Entry("Everything", fullyCovered: 9),
                Entry("Some", fullyCovered: 4)
            };

            List<DirectCatalogEntry> sorted =
                DirectFontCatalogFilter.Apply(entries, null, null, DirectCatalogSort.Scripts);

            Assert.AreEqual("Everything", sorted[0].DisplayName);
            Assert.AreEqual("LatinOnly", sorted[2].DisplayName);
        }

        [Test]
        public void TiesFallBackToTheName_SoTheOrderIsStable()
        {
            // Without a tiebreaker, two scans of an unchanged project can
            // produce two different orders, which reads as a bug even though
            // nothing changed.
            var entries = new List<DirectCatalogEntry>
            {
                Sized("Beta", 100, 500),
                Sized("Alpha", 100, 500)
            };

            List<DirectCatalogEntry> sorted =
                DirectFontCatalogFilter.Apply(entries, null, null, DirectCatalogSort.Glyphs);

            Assert.AreEqual("Alpha", sorted[0].DisplayName);
            Assert.AreEqual("Beta", sorted[1].DisplayName);
        }

        [Test]
        public void BrokenFontsSinkToTheBottomWhateverTheSort()
        {
            var entries = new List<DirectCatalogEntry>
            {
                Entry("Broken", glyphs: 99999, usable: false),
                Entry("Fine", glyphs: 10)
            };

            foreach (DirectCatalogSort sort in System.Enum.GetValues(typeof(DirectCatalogSort)))
            {
                List<DirectCatalogEntry> sorted =
                    DirectFontCatalogFilter.Apply(entries, null, null, sort);

                Assert.AreEqual("Fine", sorted[0].DisplayName,
                    $"A font that cannot be read must never outrank one that works ({sort}).");
            }
        }

        [Test]
        public void ApplyOfNullIsAnEmptyListRatherThanAnException()
        {
            Assert.IsEmpty(DirectFontCatalogFilter.Apply(null, null, null, DirectCatalogSort.Name));
        }

        [Test]
        public void ApplySkipsNullEntries()
        {
            var entries = new List<DirectCatalogEntry> { null, Entry("Real"), null };

            List<DirectCatalogEntry> sorted =
                DirectFontCatalogFilter.Apply(entries, null, null, DirectCatalogSort.Name);

            Assert.AreEqual(1, sorted.Count);
        }

        // ==========================================
        // Formatting
        // ==========================================
        [Test]
        public void ByteSizesAreFormattedForHumans()
        {
            Assert.AreEqual("—", DirectFontCatalogFilter.FormatBytes(0));
            Assert.AreEqual("—", DirectFontCatalogFilter.FormatBytes(-1));
            Assert.AreEqual("512 B", DirectFontCatalogFilter.FormatBytes(512));
            StringAssert.EndsWith("KB", DirectFontCatalogFilter.FormatBytes(20_000));
            StringAssert.EndsWith("MB", DirectFontCatalogFilter.FormatBytes(20_000_000));
        }

        // ==========================================
        // The installed-font picker's own filter
        // ==========================================
        [Test]
        public void TheSystemFontFilterShowsEverythingForAnEmptyQuery()
        {
            var families = new[] { "Segoe UI", "Arial", "Noto Sans JP" };

            Assert.AreEqual(3, DirectEditorFontWindow.Filter(families, string.Empty).Count);
            Assert.AreEqual(3, DirectEditorFontWindow.Filter(families, null).Count);
            Assert.AreEqual(3, DirectEditorFontWindow.Filter(families, "  ").Count);
        }

        [Test]
        public void TheSystemFontFilterMatchesCaseInsensitively()
        {
            var families = new[] { "Segoe UI", "Arial", "Noto Sans JP" };

            List<string> matches = DirectEditorFontWindow.Filter(families, "noto");

            Assert.AreEqual(1, matches.Count);
            Assert.AreEqual("Noto Sans JP", matches[0]);
        }

        [Test]
        public void TheSystemFontFilterHandlesNullInput()
        {
            Assert.IsEmpty(DirectEditorFontWindow.Filter(null, "anything"));
        }
    }
}
