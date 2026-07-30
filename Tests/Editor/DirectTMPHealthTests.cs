// ==========================================
// DirectTMPHealthTests
// The health report's ordering and its plain-text
// form.
//
// The report is read by two audiences: a person
// looking for the one thing that is wrong, and
// whoever they paste it to afterwards. Both are served
// by the same two rules - problems come first, and the
// text form carries every finding - and both are the
// kind of rule that quietly stops holding when a
// seventh check gets appended to the list.
// ==========================================
using NUnit.Framework;
using System.Collections.Generic;

namespace UnityDirectTMP.Editor.Tests
{
    public sealed class DirectTMPHealthTests
    {
        private static DirectHealthEntry Entry(DirectHealthLevel level, string subject)
        {
            return new DirectHealthEntry { Level = level, Subject = subject, Finding = subject + " finding" };
        }

        [Test]
        public void SortedPutsProblemsFirstAndOkLast()
        {
            var report = new DirectHealthReport();
            report.Entries.Add(Entry(DirectHealthLevel.Ok, "Cache"));
            report.Entries.Add(Entry(DirectHealthLevel.Problem, "TMP Resources"));
            report.Entries.Add(Entry(DirectHealthLevel.Warning, "Project fonts"));

            List<DirectHealthEntry> sorted = report.Sorted();

            Assert.AreEqual(DirectHealthLevel.Problem, sorted[0].Level);
            Assert.AreEqual(DirectHealthLevel.Warning, sorted[1].Level);
            Assert.AreEqual(DirectHealthLevel.Ok, sorted[2].Level);
        }

        [Test]
        public void SortingIsStableWithinALevel()
        {
            // Two runs of an unchanged project must produce the same report,
            // or a diff between two pastes is meaningless.
            var report = new DirectHealthReport();
            report.Entries.Add(Entry(DirectHealthLevel.Warning, "Zebra"));
            report.Entries.Add(Entry(DirectHealthLevel.Warning, "Alpha"));

            List<DirectHealthEntry> sorted = report.Sorted();

            Assert.AreEqual("Alpha", sorted[0].Subject);
            Assert.AreEqual("Zebra", sorted[1].Subject);
        }

        [Test]
        public void WorstIsTheHighestLevelPresent()
        {
            var report = new DirectHealthReport();
            Assert.AreEqual(DirectHealthLevel.Ok, report.Worst, "An empty report is not a failing one.");

            report.Entries.Add(Entry(DirectHealthLevel.Ok, "A"));
            Assert.AreEqual(DirectHealthLevel.Ok, report.Worst);

            report.Entries.Add(Entry(DirectHealthLevel.Warning, "B"));
            Assert.AreEqual(DirectHealthLevel.Warning, report.Worst);

            report.Entries.Add(Entry(DirectHealthLevel.Problem, "C"));
            Assert.AreEqual(DirectHealthLevel.Problem, report.Worst);
        }

        [Test]
        public void CountAtCountsOnlyThatLevel()
        {
            var report = new DirectHealthReport();
            report.Entries.Add(Entry(DirectHealthLevel.Warning, "A"));
            report.Entries.Add(Entry(DirectHealthLevel.Warning, "B"));
            report.Entries.Add(Entry(DirectHealthLevel.Ok, "C"));

            Assert.AreEqual(2, report.CountAt(DirectHealthLevel.Warning));
            Assert.AreEqual(1, report.CountAt(DirectHealthLevel.Ok));
            Assert.AreEqual(0, report.CountAt(DirectHealthLevel.Problem));
        }

        [Test]
        public void EveryLevelHasItsOwnMarker()
        {
            Assert.AreNotEqual(
                DirectTMPHealth.Marker(DirectHealthLevel.Ok),
                DirectTMPHealth.Marker(DirectHealthLevel.Warning));

            Assert.AreNotEqual(
                DirectTMPHealth.Marker(DirectHealthLevel.Warning),
                DirectTMPHealth.Marker(DirectHealthLevel.Problem));
        }

        [Test]
        public void MarkersAreAsciiSoTheySurviveBeingPasted()
        {
            // The report goes into issue trackers and chat clients that render
            // emoji as a question mark or as nothing at all.
            foreach (DirectHealthLevel level in System.Enum.GetValues(typeof(DirectHealthLevel)))
            {
                foreach (char c in DirectTMPHealth.Marker(level))
                {
                    Assert.Less((int)c, 128, "Marker for " + level + " is not ASCII.");
                }
            }
        }

        [Test]
        public void TheFormattedReportCarriesEveryFindingAndItsAdvice()
        {
            var report = new DirectHealthReport();
            report.Entries.Add(new DirectHealthEntry
            {
                Level = DirectHealthLevel.Problem,
                Subject = "TMP Essential Resources",
                Finding = "Not imported into this project.",
                Advice = "Window > TextMeshPro > Import TMP Essential Resources."
            });
            report.Entries.Add(Entry(DirectHealthLevel.Ok, "Font cache"));

            string text = DirectTMPHealth.Format(report);

            StringAssert.Contains("TMP Essential Resources", text);
            StringAssert.Contains("Not imported into this project.", text);
            StringAssert.Contains("Import TMP Essential Resources", text);
            StringAssert.Contains("Font cache", text);
            StringAssert.Contains(DirectTMPConstants.Version, text,
                "A pasted report with no version number costs a round trip to get one.");
        }

        [Test]
        public void FormattingAnEmptyReportStillNamesTheToolAndVersion()
        {
            string text = DirectTMPHealth.Format(new DirectHealthReport());

            StringAssert.Contains(DirectTMPConstants.ToolName, text);
            StringAssert.Contains(DirectTMPConstants.Version, text);
        }

        // ==========================================
        // The live report
        //
        // Running it for real inside the test Editor is
        // the only way to know the checks themselves do
        // not throw on a project shaped unlike the
        // author's.
        // ==========================================
        [Test]
        public void RunningTheRealReportProducesFindingsAndDoesNotThrow()
        {
            DirectHealthReport report = null;

            Assert.DoesNotThrow(() => report = DirectTMPHealth.Run());
            Assert.IsNotNull(report);
            Assert.IsNotEmpty(report.Entries, "A report with no checks in it tells nobody anything.");

            foreach (DirectHealthEntry entry in report.Entries)
            {
                Assert.IsNotEmpty(entry.Subject);
                Assert.IsNotEmpty(entry.Finding);
            }
        }

        [Test]
        public void EveryFindingWithAnActionAlsoHasALabelForIt()
        {
            foreach (DirectHealthEntry entry in DirectTMPHealth.Run().Entries)
            {
                if (entry.Fix == null) { continue; }

                Assert.IsNotEmpty(entry.ActionLabel,
                    $"'{entry.Subject}' has a fix with no button label, so the button never renders.");
            }
        }

        [Test]
        public void EveryProblemAndWarningExplainsWhatToDo()
        {
            foreach (DirectHealthEntry entry in DirectTMPHealth.Run().Entries)
            {
                if (entry.Level == DirectHealthLevel.Ok) { continue; }

                bool actionable = !string.IsNullOrEmpty(entry.Advice) || entry.Fix != null;
                Assert.IsTrue(actionable,
                    $"'{entry.Subject}' reports a problem with neither advice nor a fix, which is just bad news.");
            }
        }
    }
}
