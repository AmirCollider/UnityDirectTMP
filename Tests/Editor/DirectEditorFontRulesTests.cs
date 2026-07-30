// ==========================================
// DirectEditorFontRulesTests
// The safety rules around restyling Unity itself.
//
// This is the highest-stakes logic in the package. If
// the Latin guard stops refusing, somebody points the
// Editor at a CJK-only subset and every menu in Unity
// - including the one that undoes it - becomes a row
// of empty boxes. There is no in-Editor recovery from
// that; they would be counting menu positions from
// memory or editing EditorPrefs by hand.
//
// So these tests do not merely check that the guard
// exists. They check that it fails closed, that it is
// classified as fatal rather than advisory, and that
// the one problem which IS advisory stays advisory -
// because collapsing the two into one boolean is the
// refactor that would quietly break this.
// ==========================================
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace UnityDirectTMP.Editor.Tests
{
    public sealed class DirectEditorFontRulesTests
    {
        // A font that has everything.
        private static readonly Func<int, bool> Everything = _ => true;

        // A font that has nothing - an icon font, or a subset gone wrong.
        private static readonly Func<int, bool> Nothing = _ => false;

        // A CJK-only font: kana and kanji, no Latin at all. This is the exact
        // shape of the mistake the guard exists to catch, because it is a
        // completely reasonable font to want in a Japanese project.
        private static readonly Func<int, bool> CjkOnly = codepoint =>
            (codepoint >= 0x3040 && codepoint <= 0x30FF) || (codepoint >= 0x4E00 && codepoint <= 0x9FFF);

        // ==========================================
        // Size
        // ==========================================
        [Test]
        public void SizeDeltaIsClampedToTheAllowedRange()
        {
            Assert.AreEqual(DirectEditorFontRules.MinSizeDelta, DirectEditorFontRules.ClampSizeDelta(-99));
            Assert.AreEqual(DirectEditorFontRules.MaxSizeDelta, DirectEditorFontRules.ClampSizeDelta(99));
            Assert.AreEqual(0, DirectEditorFontRules.ClampSizeDelta(0));
            Assert.AreEqual(3, DirectEditorFontRules.ClampSizeDelta(3));
        }

        [Test]
        public void ThePlanClampsItsOwnSizeDelta()
        {
            var plan = new DirectEditorFontPlan(true, DirectEditorFontSource.SystemFont, null, "Whatever", 500);

            Assert.AreEqual(DirectEditorFontRules.MaxSizeDelta, plan.SizeDelta,
                "A plan read back from a hand-edited EditorPrefs must not carry an unbounded size.");
        }

        [Test]
        public void AZeroDeltaLeavesAnInheritingStyleInheriting()
        {
            // fontSize 0 in IMGUI means "whatever the font says". Turning that
            // into 12 for no reason would harden every style in the Editor to
            // a fixed size.
            Assert.AreEqual(0, DirectEditorFontRules.ResolveFontSize(0, 0));
            Assert.AreEqual(14, DirectEditorFontRules.ResolveFontSize(14, 0));
        }

        [Test]
        public void ANonZeroDeltaAppliesToTheBaseSizeWhenAStyleInherits()
        {
            Assert.AreEqual(
                DirectEditorFontRules.BaseFontSize + 2,
                DirectEditorFontRules.ResolveFontSize(0, 2));
        }

        [Test]
        public void ResolvedSizeNeverGoesBelowTheFloor()
        {
            Assert.GreaterOrEqual(
                DirectEditorFontRules.ResolveFontSize(7, -4),
                DirectEditorFontRules.MinResolvedSize);

            Assert.AreEqual(
                DirectEditorFontRules.MinResolvedSize,
                DirectEditorFontRules.ResolveFontSize(1, -4));
        }

        // ==========================================
        // The guard
        // ==========================================
        [Test]
        public void AFontWithEverythingCanDrawTheEditor()
        {
            Assert.IsTrue(DirectEditorFontRules.CanDrawEditorChrome(Everything));
        }

        [Test]
        public void AFontWithNothingCannot()
        {
            Assert.IsFalse(DirectEditorFontRules.CanDrawEditorChrome(Nothing));
        }

        [Test]
        public void ACjkOnlyFontCannotDrawTheEditor()
        {
            Assert.IsFalse(DirectEditorFontRules.CanDrawEditorChrome(CjkOnly),
                "This is the whole point: a perfectly good Japanese font with no Latin would make every Unity menu unreadable.");
        }

        [Test]
        public void AFontMissingOnlyTheDigitsCannotDrawTheEditor()
        {
            // The Inspector is mostly numbers. A font that passes every letter
            // and fails '0' turns every numeric field into a gap.
            Func<int, bool> noDigits = codepoint => codepoint < '0' || codepoint > '9';

            Assert.IsFalse(DirectEditorFontRules.CanDrawEditorChrome(noDigits));
        }

        [Test]
        public void AFontMissingOnlyTheSpaceCharacterIsStillFine()
        {
            // Plenty of real fonts encode the space as an advance width with
            // no outline, and some cmaps skip it. Refusing those would reject
            // fonts that draw the Editor perfectly.
            Func<int, bool> noSpace = codepoint => codepoint != ' ';

            Assert.IsTrue(DirectEditorFontRules.CanDrawEditorChrome(noSpace));
        }

        [Test]
        public void ANullProbeFailsClosed()
        {
            Assert.IsFalse(DirectEditorFontRules.CanDrawEditorChrome(null),
                "No information must never be read as permission.");
        }

        [Test]
        public void MissingCharactersAreListedSoTheRefusalHasAReason()
        {
            List<string> missing = DirectEditorFontRules.MissingChromeCharacters(CjkOnly);

            Assert.IsNotEmpty(missing);
            CollectionAssert.Contains(missing, "A");
            CollectionAssert.Contains(missing, "0");
            CollectionAssert.DoesNotContain(missing, " ");
        }

        [Test]
        public void NothingIsMissingFromAFontThatHasEverything()
        {
            Assert.IsEmpty(DirectEditorFontRules.MissingChromeCharacters(Everything));
        }

        // ==========================================
        // Validation
        // ==========================================
        [Test]
        public void AnInactivePlanHasNoProblems()
        {
            DirectEditorFontProblem problem = DirectEditorFontRules.Validate(
                DirectEditorFontPlan.Off, _ => false, _ => false, _ => false, Nothing);

            Assert.AreEqual(DirectEditorFontProblem.None, problem,
                "Not overriding the font cannot be wrong.");
        }

        [Test]
        public void AProjectPlanWithNoPathIsMissingItsFont()
        {
            var plan = new DirectEditorFontPlan(true, DirectEditorFontSource.ProjectFont, null, null, 0);

            Assert.AreEqual(DirectEditorFontProblem.NoFontChosen,
                DirectEditorFontRules.Validate(plan, _ => true, _ => true, _ => true, Everything));
        }

        [Test]
        public void AProjectPlanPointingAtADeletedAssetSaysSo()
        {
            var plan = new DirectEditorFontPlan(true, DirectEditorFontSource.ProjectFont,
                "Assets/Fonts/Gone.ttf", null, 0);

            Assert.AreEqual(DirectEditorFontProblem.MissingAsset,
                DirectEditorFontRules.Validate(plan, _ => false, _ => true, _ => true, Everything));
        }

        [Test]
        public void ASystemPlanForAnUninstalledFamilySaysSo()
        {
            var plan = new DirectEditorFontPlan(true, DirectEditorFontSource.SystemFont, null, "Uninstalled Sans", 0);

            Assert.AreEqual(DirectEditorFontProblem.MissingSystemFamily,
                DirectEditorFontRules.Validate(plan, _ => true, _ => true, _ => false, Everything));
        }

        [Test]
        public void AFontThatCannotDrawTheEditorIsRefused()
        {
            var plan = new DirectEditorFontPlan(true, DirectEditorFontSource.ProjectFont,
                "Assets/Fonts/CjkOnly.ttf", null, 0);

            DirectEditorFontProblem problem =
                DirectEditorFontRules.Validate(plan, _ => true, _ => true, _ => true, CjkOnly);

            Assert.AreEqual(DirectEditorFontProblem.NoLatinCoverage, problem);
            Assert.IsTrue(DirectEditorFontRules.IsFatal(problem),
                "If this ever stops being fatal, the Revert menu item becomes unreadable and there is no way back.");
        }

        [Test]
        public void CoverageIsCheckedBeforeTheImportMode()
        {
            // Both problems are present. The one that would break the Editor
            // has to be the one reported, because it is the one that stops the
            // apply.
            var plan = new DirectEditorFontPlan(true, DirectEditorFontSource.ProjectFont,
                "Assets/Fonts/Bad.ttf", null, 0);

            Assert.AreEqual(DirectEditorFontProblem.NoLatinCoverage,
                DirectEditorFontRules.Validate(plan, _ => true, _ => false, _ => true, CjkOnly));
        }

        [Test]
        public void ABakedCharacterSetIsAWarningRatherThanARefusal()
        {
            var plan = new DirectEditorFontPlan(true, DirectEditorFontSource.ProjectFont,
                "Assets/Fonts/Baked.ttf", null, 0);

            DirectEditorFontProblem problem =
                DirectEditorFontRules.Validate(plan, _ => true, _ => false, _ => true, Everything);

            Assert.AreEqual(DirectEditorFontProblem.NotDynamic, problem);
            Assert.IsFalse(DirectEditorFontRules.IsFatal(problem),
                "A font with a baked character set still draws; it just cannot grow new glyphs.");
        }

        [Test]
        public void NoCoverageInformationDoesNotBlockTheApply()
        {
            // An unreadable file gives no opinion. Treating "I could not parse
            // this" as "it has no Latin" would refuse fonts that are perfectly
            // fine and that Unity itself can read.
            var plan = new DirectEditorFontPlan(true, DirectEditorFontSource.ProjectFont,
                "Assets/Fonts/Unparseable.ttf", null, 0);

            Assert.AreEqual(DirectEditorFontProblem.None,
                DirectEditorFontRules.Validate(plan, _ => true, _ => true, _ => true, null));
        }

        [Test]
        public void EveryProblemExceptNotDynamicIsFatal()
        {
            foreach (DirectEditorFontProblem problem in
                     System.Enum.GetValues(typeof(DirectEditorFontProblem)))
            {
                bool expected = problem != DirectEditorFontProblem.None
                             && problem != DirectEditorFontProblem.NotDynamic;

                Assert.AreEqual(expected, DirectEditorFontRules.IsFatal(problem), problem.ToString());
            }
        }

        [Test]
        public void EveryProblemHasAnExplanation()
        {
            foreach (DirectEditorFontProblem problem in
                     System.Enum.GetValues(typeof(DirectEditorFontProblem)))
            {
                if (problem == DirectEditorFontProblem.None) { continue; }

                Assert.IsNotEmpty(DirectEditorFontRules.Describe(problem),
                    $"{problem} would be shown to a user as an empty message box.");
            }
        }

        // ==========================================
        // The plan itself
        // ==========================================
        [Test]
        public void TheOffPlanIsNotActive()
        {
            Assert.IsFalse(DirectEditorFontPlan.Off.IsActive);
            Assert.IsFalse(DirectEditorFontPlan.Off.Enabled);
        }

        [Test]
        public void APlanIsInactiveWhenItsSourceIsUnityDefault_EvenIfEnabled()
        {
            var plan = new DirectEditorFontPlan(true, DirectEditorFontSource.UnityDefault, null, null, 0);

            Assert.IsFalse(plan.IsActive);
        }

        [Test]
        public void DescribeNamesTheFontAndTheSizeAdjustment()
        {
            var plan = new DirectEditorFontPlan(true, DirectEditorFontSource.ProjectFont,
                "Assets/Fonts/Vazirmatn-Regular.ttf", null, 2);

            string described = plan.Describe();

            StringAssert.Contains("Vazirmatn-Regular", described);
            StringAssert.Contains("2", described);
        }

        [Test]
        public void DescribeOmitsTheSizeWhenItIsUnchanged()
        {
            var plan = new DirectEditorFontPlan(true, DirectEditorFontSource.SystemFont, null, "Segoe UI", 0);

            Assert.AreEqual("Segoe UI", plan.Describe());
        }
    }
}
