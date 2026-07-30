// ==========================================
// DirectTMPIdentityTests
// The version, the menu tree, the brand palette and
// the language pack.
//
// None of these are behaviour. They are the promises
// the package makes about itself in five places at
// once - package.json, the About window, the README,
// the product page, the console prefix - and the only
// thing keeping five copies of one fact in agreement
// is a test that reads them all.
//
// The version half of this exists because it has
// happened, in the sibling package: a constant sat two
// releases behind package.json, and every generated
// artefact reported a version the tool had not been on
// for weeks.
// ==========================================
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor.Tests
{
    public sealed class DirectTMPIdentityTests
    {
        // ==========================================
        // Version
        // ==========================================
        [Test]
        public void ConstantMatchesPackageJson()
        {
            string packageJsonPath = FindPackageJson();
            if (packageJsonPath == null)
            {
                Assert.Ignore("package.json is not reachable from this test context.");
                return;
            }

            string declared = ReadVersion(File.ReadAllText(packageJsonPath));

            Assert.IsNotNull(declared, "package.json has no \"version\" field: " + packageJsonPath);
            Assert.AreEqual(declared, DirectTMPConstants.Version,
                "DirectTMPConstants.Version is printed in the About window, the Settings footer, every window header and every health report. It has to be the package version.");
        }

        [Test]
        public void VersionLooksLikeSemanticVersioning()
        {
            Assert.IsTrue(Regex.IsMatch(DirectTMPConstants.Version, @"^\d+\.\d+\.\d+"),
                "Got: " + DirectTMPConstants.Version);
        }

        [Test]
        public void EveryUrlPointsAtSomethingAndUsesHttps()
        {
            string[] urls =
            {
                DirectTMPConstants.GithubUrl,
                DirectTMPConstants.SiteBaseUrl,
                DirectTMPConstants.ProductUrl,
                DirectTMPConstants.ToolsCatalogUrl,
                DirectTMPConstants.DocsUrl,
                DirectTMPConstants.IssuesUrl,
                DirectTMPConstants.ChangelogUrl,
                DirectTMPConstants.SiblingToolUrl
            };

            foreach (string url in urls)
            {
                Assert.IsNotEmpty(url);
                StringAssert.StartsWith("https://", url,
                    "A button in the Editor opens this in a browser; http would be a downgrade nobody notices.");
            }
        }

        [Test]
        public void TheLogPrefixNamesTheTool()
        {
            StringAssert.Contains(DirectTMPConstants.ToolName, DirectTMPConstants.LogPrefix,
                "Filtering the Console by the tool's name is the whole reason the prefix exists.");
        }

        // ==========================================
        // The brand
        // ==========================================
        [Test]
        public void EveryPaletteColourParses()
        {
            // Hex() returns magenta on malformed input rather than throwing,
            // which is loud in the UI but silent in a test - so the test has
            // to check for it directly.
            var palette = new Dictionary<string, Color>
            {
                { nameof(DirectTMPConstants.ColorInk), DirectTMPBrand.Ink },
                { nameof(DirectTMPConstants.ColorInkDeep), DirectTMPBrand.InkDeep },
                { nameof(DirectTMPConstants.ColorBrass), DirectTMPBrand.Brass },
                { nameof(DirectTMPConstants.ColorPaper), DirectTMPBrand.Paper },
                { nameof(DirectTMPConstants.ColorBlush), DirectTMPBrand.Blush },
                { nameof(DirectTMPConstants.ColorNight), DirectTMPBrand.Night }
            };

            foreach (KeyValuePair<string, Color> entry in palette)
            {
                Assert.AreNotEqual(Color.magenta, entry.Value,
                    entry.Key + " did not parse — Hex() fell back to magenta.");
            }
        }

        [Test]
        public void HexRejectsMalformedInputWithoutThrowing()
        {
            Assert.AreEqual(Color.magenta, DirectTMPBrand.Hex(null));
            Assert.AreEqual(Color.magenta, DirectTMPBrand.Hex(string.Empty));
            Assert.AreEqual(Color.magenta, DirectTMPBrand.Hex("#12345"));
            Assert.AreEqual(Color.magenta, DirectTMPBrand.Hex("nothex"));
        }

        [Test]
        public void HexReadsTheChannelsInTheRightOrder()
        {
            Color red = DirectTMPBrand.Hex("#FF0000");

            Assert.AreEqual(1f, red.r, 0.001f);
            Assert.AreEqual(0f, red.g, 0.001f);
            Assert.AreEqual(0f, red.b, 0.001f);
        }

        [Test]
        public void LiftMovesTowardsWhiteAndBlack()
        {
            Color mid = new Color(0.5f, 0.5f, 0.5f, 1f);

            Assert.Greater(DirectTMPBrand.Lift(mid, 0.5f).r, mid.r);
            Assert.Less(DirectTMPBrand.Lift(mid, -0.5f).r, mid.r);
            Assert.AreEqual(mid.r, DirectTMPBrand.Lift(mid, 0f).r, 0.001f);
        }

        [Test]
        public void LiftKeepsTheAlpha()
        {
            var translucent = new Color(0.2f, 0.4f, 0.6f, 0.35f);

            Assert.AreEqual(0.35f, DirectTMPBrand.Lift(translucent, 0.4f).a, 0.001f);
        }

        [Test]
        public void EveryCoverageVerdictHasItsOwnColour()
        {
            Color full = DirectTMPBrand.ColorFor(DirectScriptCoverage.Full);
            Color partial = DirectTMPBrand.ColorFor(DirectScriptCoverage.Partial);
            Color none = DirectTMPBrand.ColorFor(DirectScriptCoverage.None);

            Assert.AreNotEqual(full, partial, "Full and partial coverage must be distinguishable at a glance.");
            Assert.AreNotEqual(partial, none);
        }

        // ==========================================
        // Language
        // ==========================================
        [Test]
        public void ThereAreAsManyLabelsAsLanguageCodes()
        {
            Assert.AreEqual(DirectTMPText.Codes.Length, DirectTMPText.Labels.Length,
                "A mismatch here draws the language dropdown with the wrong name on an entry.");
        }

        [Test]
        public void EveryLanguageCodeIsSupported()
        {
            foreach (string code in DirectTMPText.Codes)
            {
                Assert.IsTrue(DirectTMPText.IsSupported(code), code);
            }
        }

        [Test]
        public void AnUnknownLanguageIsNotSupportedAndIndexesToEnglish()
        {
            Assert.IsFalse(DirectTMPText.IsSupported("de"));
            Assert.IsFalse(DirectTMPText.IsSupported(null));
            Assert.AreEqual(0, DirectTMPText.IndexOf("de"));
        }

        [Test]
        public void PickingAStringHonoursTheLanguage()
        {
            Assert.AreEqual("one", DirectTMPText.For(DirectTMPText.English, "one", "いち", "یک"));
            Assert.AreEqual("いち", DirectTMPText.For(DirectTMPText.Japanese, "one", "いち", "یک"));
            Assert.AreEqual("یک", DirectTMPText.For(DirectTMPText.Persian, "one", "いち", "یک"));
        }

        [Test]
        public void AMissingTranslationFallsBackToEnglishRatherThanEmpty()
        {
            Assert.AreEqual("one", DirectTMPText.For(DirectTMPText.Japanese, "one", null, "یک"));
            Assert.AreEqual("one", DirectTMPText.For(DirectTMPText.Persian, "one", "いち", string.Empty),
                "An untranslated button is mildly annoying; a blank button is broken.");
        }

        [Test]
        public void EveryWordInTheSharedVocabularyResolvesInEveryLanguage()
        {
            string[] keys =
            {
                "apply", "revert", "close", "cancel", "refresh", "settings",
                "language", "font", "preview", "none", "size", "glyphs", "scripts"
            };

            string original = DirectTMPText.Current;
            try
            {
                foreach (string code in DirectTMPText.Codes)
                {
                    DirectTMPText.Current = code;
                    foreach (string key in keys)
                    {
                        string word = DirectTMPText.Word(key);
                        Assert.IsNotEmpty(word, $"'{key}' is empty in '{code}'.");
                        Assert.AreNotEqual(key, word,
                            $"'{key}' has no entry in '{code}' — Word() returned the key itself.");
                    }
                }
            }
            finally
            {
                DirectTMPText.Current = original;
            }
        }

        // ==========================================
        // The menu tree
        // ==========================================
        [Test]
        public void EveryMenuItemLivesUnderTheToolsOwnRoot()
        {
            foreach (string path in DirectTMPHealth.RegisteredMenuPaths())
            {
                bool known = path.StartsWith(DirectTMPEditorConstants.MenuRoot)
                          || path.StartsWith(DirectTMPEditorConstants.WindowRoot)
                          || path.StartsWith(DirectTMPEditorConstants.AssetsContextRoot);

                Assert.IsTrue(known, $"'{path}' is outside every root this package declares.");
            }
        }

        [Test]
        public void TheMenuItemsPeopleActuallyLookForAreRegistered()
        {
            List<string> paths = DirectTMPHealth.RegisteredMenuPaths();

            string[] required =
            {
                DirectTMPEditorConstants.MenuCatalog,
                DirectTMPEditorConstants.MenuEditorFont,
                DirectTMPEditorConstants.MenuEditorFontRevert,
                DirectTMPEditorConstants.MenuConvertSelected,
                DirectTMPEditorConstants.MenuConvertScene,
                DirectTMPEditorConstants.MenuConvertProject,
                DirectTMPEditorConstants.MenuFallbackChain,
                DirectTMPEditorConstants.MenuSettings,
                DirectTMPEditorConstants.MenuHealthCheck,
                DirectTMPEditorConstants.MenuAbout
            };

            foreach (string path in required)
            {
                CollectionAssert.Contains(paths, path);
            }
        }

        [Test]
        public void TheThreeWindowsHaveASecondDoorUnderTheWindowMenu()
        {
            // A whole missing top-level menu reads as "the tool did not
            // install". A second, familiar place to look is what turns that
            // into "something is wrong, here is the health check".
            List<string> paths = DirectTMPHealth.RegisteredMenuPaths();

            CollectionAssert.Contains(paths, DirectTMPEditorConstants.WindowCatalog);
            CollectionAssert.Contains(paths, DirectTMPEditorConstants.WindowEditorFont);
            CollectionAssert.Contains(paths, DirectTMPEditorConstants.WindowHealthCheck);
        }

        [Test]
        public void NoMenuPathIsRegisteredTwice()
        {
            List<string> paths = DirectTMPHealth.RegisteredMenuPaths();
            var seen = new HashSet<string>();

            foreach (string path in paths)
            {
                Assert.IsTrue(seen.Add(path), $"'{path}' is registered more than once.");
            }
        }

        // ==========================================
        // First run
        // ==========================================
        [Test]
        public void TheWelcomeScreenShowsOncePerVersion()
        {
            Assert.IsTrue(DirectTMPFirstRun.ShouldShow(string.Empty, "1.0.0", false, false),
                "A fresh install has never seen it.");
            Assert.IsFalse(DirectTMPFirstRun.ShouldShow("1.0.0", "1.0.0", false, false),
                "Once per version means once.");
            Assert.IsTrue(DirectTMPFirstRun.ShouldShow("1.0.0", "1.1.0", false, false),
                "An upgrade is the only honest moment to mention a feature that did not exist before.");
        }

        [Test]
        public void TheWelcomeScreenNeverOpensInBatchModeOrWhileUnityIsBusy()
        {
            Assert.IsFalse(DirectTMPFirstRun.ShouldShow(string.Empty, "1.0.0", batchMode: true, busy: false),
                "A window in a headless build is a hung CI job.");
            Assert.IsFalse(DirectTMPFirstRun.ShouldShow(string.Empty, "1.0.0", batchMode: false, busy: true),
                "Opening during a compile lands the window behind the progress bar.");
        }

        // ==========================================
        // Helpers
        // ==========================================
        private static string ReadVersion(string packageJson)
        {
            Match match = Regex.Match(packageJson, "\"version\"\\s*:\\s*\"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : null;
        }

        // Walks up from this test script's own location until a package.json
        // turns up, so the test works whether the package is embedded in
        // Packages/, in Assets/, or installed from a registry.
        private static string FindPackageJson()
        {
            string[] guids = AssetDatabase.FindAssets("DirectTMPIdentityTests t:MonoScript");
            string scriptPath = guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : string.Empty;

            string directory = string.IsNullOrEmpty(scriptPath)
                ? Path.GetDirectoryName(Path.GetFullPath("Assets"))
                : Path.GetDirectoryName(Path.GetFullPath(scriptPath));

            for (int i = 0; i < 8 && !string.IsNullOrEmpty(directory); i++)
            {
                string candidate = Path.Combine(directory, "package.json");
                if (File.Exists(candidate)) { return candidate; }

                DirectoryInfo parent = Directory.GetParent(directory);
                directory = parent != null ? parent.FullName : null;
            }
            return null;
        }
    }
}
