// ==========================================
// DirectFontScriptsTests
// The coverage badges are the most visible claim this
// package makes, and every one of them is computed
// here. A badge that says "Japanese" on a font with no
// kana is a worse outcome than no badge at all - the
// user acts on it.
//
// The completeness tests matter as much as the logic
// ones: the script list, the probe characters, the
// preview samples and the three-language names live in
// four separate tables in one file, and adding a script
// to one of them and not the others produces a badge
// that renders blank or a filter with no label.
// ==========================================
using NUnit.Framework;
using System.Collections.Generic;

namespace UnityDirectTMP.Editor.Tests
{
    public sealed class DirectFontScriptsTests
    {
        // ==========================================
        // Completeness
        // ==========================================
        [Test]
        public void EveryEnumValueIsListedInAll()
        {
            var listed = new HashSet<DirectScript>(DirectFontScripts.All);

            foreach (DirectScript script in System.Enum.GetValues(typeof(DirectScript)))
            {
                Assert.IsTrue(listed.Contains(script),
                    $"{script} exists but is not in DirectFontScripts.All, so the catalog will never show it.");
            }
        }

        [Test]
        public void EveryScriptHasProbeCharacters()
        {
            foreach (DirectScript script in DirectFontScripts.All)
            {
                int[] probes = DirectFontScripts.ProbesFor(script);
                Assert.IsNotEmpty(probes, $"{script} has no probe characters, so its coverage is always None.");
                Assert.GreaterOrEqual(probes.Length, 4,
                    $"{script} has fewer than four probes; one letter is not evidence a font can set a script.");
            }
        }

        [Test]
        public void EveryScriptHasAPreviewSample()
        {
            foreach (DirectScript script in DirectFontScripts.All)
            {
                Assert.IsNotEmpty(DirectFontScripts.SampleFor(script), $"{script} has no preview sample.");
            }
        }

        [Test]
        public void EveryScriptIsNamedInAllThreeLanguages()
        {
            foreach (DirectScript script in DirectFontScripts.All)
            {
                foreach (string language in new[] { "en", "ja", "fa" })
                {
                    string name = DirectFontScripts.NameFor(script, language);
                    Assert.IsNotEmpty(name, $"{script} has no name in '{language}'.");
                }
            }
        }

        [Test]
        public void AnUnknownLanguageFallsBackToEnglishRatherThanEmpty()
        {
            Assert.AreEqual(
                DirectFontScripts.NameFor(DirectScript.Han, "en"),
                DirectFontScripts.NameFor(DirectScript.Han, "de"));
        }

        [Test]
        public void EveryScriptsPreviewSampleContainsThatScript()
        {
            // A sample that does not actually contain the script it is
            // labelled with makes the preview row a lie.
            foreach (DirectScript script in DirectFontScripts.All)
            {
                List<DirectScript> found = DirectFontScripts.ScriptsIn(DirectFontScripts.SampleFor(script));
                Assert.Contains(script, found,
                    $"The preview sample for {script} does not contain any {script} characters.");
            }
        }

        // ==========================================
        // Classification
        // ==========================================
        [Test]
        public void ClassifyPutsCharactersInTheRightScript()
        {
            Assert.AreEqual(DirectScript.Latin, DirectFontScripts.Classify('A'));
            Assert.AreEqual(DirectScript.Latin, DirectFontScripts.Classify('z'));
            Assert.AreEqual(DirectScript.Cyrillic, DirectFontScripts.Classify(0x0410));
            Assert.AreEqual(DirectScript.Greek, DirectFontScripts.Classify(0x03C9));
            Assert.AreEqual(DirectScript.Arabic, DirectFontScripts.Classify(0x0627));
            Assert.AreEqual(DirectScript.Arabic, DirectFontScripts.Classify(0x06AF));
            Assert.AreEqual(DirectScript.Hebrew, DirectFontScripts.Classify(0x05D0));
            Assert.AreEqual(DirectScript.Devanagari, DirectFontScripts.Classify(0x0915));
            Assert.AreEqual(DirectScript.Thai, DirectFontScripts.Classify(0x0E01));
            Assert.AreEqual(DirectScript.Han, DirectFontScripts.Classify(0x4E00));
            Assert.AreEqual(DirectScript.Kana, DirectFontScripts.Classify(0x3042));
            Assert.AreEqual(DirectScript.Kana, DirectFontScripts.Classify(0x30A2));
            Assert.AreEqual(DirectScript.Hangul, DirectFontScripts.Classify(0xD55C));
            Assert.AreEqual(DirectScript.Emoji, DirectFontScripts.Classify(0x1F600));
        }

        [Test]
        public void ClassifyReturnsNullOutsideEveryKnownRange()
        {
            Assert.IsNull(DirectFontScripts.Classify(0x0000), "NUL belongs to no script.");
            Assert.IsNull(DirectFontScripts.Classify(' '), "Whitespace belongs to no script.");
            Assert.IsNull(DirectFontScripts.Classify(0x0E80), "Lao is real, but this tool has no badge for it.");
        }

        [Test]
        public void ScriptsInReturnsCatalogOrderRegardlessOfInputOrder()
        {
            // Two strings with the same scripts in different orders must
            // produce the same list, or the same font renders a different
            // badge row depending on what somebody typed.
            List<DirectScript> a = DirectFontScripts.ScriptsIn("あA");
            List<DirectScript> b = DirectFontScripts.ScriptsIn("Aあ");

            CollectionAssert.AreEqual(a, b);
            Assert.AreEqual(DirectScript.Latin, a[0], "Latin is first in catalog order.");
        }

        [Test]
        public void ScriptsInOfEmptyOrNullIsEmpty()
        {
            Assert.IsEmpty(DirectFontScripts.ScriptsIn(null));
            Assert.IsEmpty(DirectFontScripts.ScriptsIn(string.Empty));
        }

        // ==========================================
        // Codepoints
        // ==========================================
        [Test]
        public void CodepointsCombinesSurrogatePairs()
        {
            // "😀" is two chars and one codepoint. Counting it as two is how a
            // glyph count ends up wrong and an emoji ends up classified as two
            // unassigned halves.
            var codepoints = new List<int>(DirectFontScripts.Codepoints("A\U0001F600B"));

            Assert.AreEqual(3, codepoints.Count);
            Assert.AreEqual('A', codepoints[0]);
            Assert.AreEqual(0x1F600, codepoints[1]);
            Assert.AreEqual('B', codepoints[2]);
        }

        [Test]
        public void CodepointsSurvivesALoneSurrogate()
        {
            // A truncated string from a text field can end mid-pair. It must
            // yield something rather than throw.
            var codepoints = new List<int>(DirectFontScripts.Codepoints("A\uD83D"));

            Assert.AreEqual(2, codepoints.Count);
        }

        // ==========================================
        // Verdicts
        // ==========================================
        [Test]
        public void VerdictIsFullOnlyWhenEveryProbeIsPresent()
        {
            Assert.AreEqual(DirectScriptCoverage.Full, DirectFontScripts.Verdict(5, 5));
            Assert.AreEqual(DirectScriptCoverage.Partial, DirectFontScripts.Verdict(4, 5));
            Assert.AreEqual(DirectScriptCoverage.Partial, DirectFontScripts.Verdict(1, 5));
            Assert.AreEqual(DirectScriptCoverage.None, DirectFontScripts.Verdict(0, 5));
        }

        [Test]
        public void VerdictOfNoProbesIsNoneRatherThanFull()
        {
            // Vacuous truth would report every font as fully covering a script
            // whose probe list somebody forgot to fill in.
            Assert.AreEqual(DirectScriptCoverage.None, DirectFontScripts.Verdict(0, 0));
        }

        [Test]
        public void CoverageOfANullProbeIsNone()
        {
            Assert.AreEqual(DirectScriptCoverage.None,
                DirectFontScripts.CoverageOf(DirectScript.Latin, null));
        }

        [Test]
        public void CoverageOfAllCoversEveryScript()
        {
            Dictionary<DirectScript, DirectScriptCoverage> coverage =
                DirectFontScripts.CoverageOfAll(_ => true);

            Assert.AreEqual(DirectFontScripts.All.Length, coverage.Count);
            foreach (DirectScript script in DirectFontScripts.All)
            {
                Assert.AreEqual(DirectScriptCoverage.Full, coverage[script]);
            }
        }

        [Test]
        public void FullyCoveredListsOnlyFullScriptsInCatalogOrder()
        {
            var coverage = new Dictionary<DirectScript, DirectScriptCoverage>
            {
                { DirectScript.Kana, DirectScriptCoverage.Full },
                { DirectScript.Latin, DirectScriptCoverage.Full },
                { DirectScript.Arabic, DirectScriptCoverage.Partial },
                { DirectScript.Han, DirectScriptCoverage.None }
            };

            List<DirectScript> full = DirectFontScripts.FullyCovered(coverage);

            CollectionAssert.AreEqual(new[] { DirectScript.Latin, DirectScript.Kana }, full);
        }

        [Test]
        public void FullyCoveredOfNullIsEmpty()
        {
            Assert.IsEmpty(DirectFontScripts.FullyCovered(null));
        }
    }
}
