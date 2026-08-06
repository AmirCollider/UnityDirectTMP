// ==========================================
// DirectGlobalFontTests
// The project-wide override is the one feature that
// touches every label in a project without being
// asked twice, so the parts of it that CAN be pinned
// down without a real font and a real scene are
// pinned down here:
//
//   * it is off until somebody actually names a font,
//     because an override that switches itself on is
//     the worst possible default;
//   * a hand-edited settings value can never reach the
//     FontEngine out of range;
//   * the Resources name and the asset path agree -
//     they are written in two places and a drift
//     between them is a setting that exists on disk
//     and cannot be loaded at runtime;
//   * every failure path says something. The whole of
//     "it works in my other project" was a package
//     that failed silently, and a diagnostic that can
//     return null or empty is that bug again.
// ==========================================
using NUnit.Framework;
using UnityDirectTMP;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace UnityDirectTMP.Editor.Tests
{
    internal sealed class DirectGlobalFontTests
    {
        private static DirectGlobalFontConfig NewConfig()
        {
            var config = ScriptableObject.CreateInstance<DirectGlobalFontConfig>();
            config.hideFlags = HideFlags.HideAndDontSave;
            return config;
        }

        // ==========================================
        // The settings asset
        // ==========================================
        [Test]
        public void A_Config_With_No_Font_Is_Not_Active()
        {
            DirectGlobalFontConfig config = NewConfig();
            try
            {
                config.Active = true;
                Assert.IsNull(config.FontFile);
                Assert.IsFalse(config.IsActive, "An override with no font to apply must never report itself as on.");
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void A_Config_Uses_The_Package_Defaults_Until_You_Override_Them()
        {
            DirectGlobalFontConfig config = NewConfig();
            try
            {
                config.OverrideSettings = false;
                config.Settings = new DirectFontSettings(32, 2, 512, 512, GlyphRenderMode.SDFAA);

                Assert.AreEqual(DirectFontSettings.Default, config.ResolvedSettings,
                    "Custom values must not take effect until the override switch is on.");
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void A_Hand_Edited_Settings_Value_Is_Clamped_Before_It_Is_Used()
        {
            DirectGlobalFontConfig config = NewConfig();
            try
            {
                config.OverrideSettings = true;
                config.Settings = new DirectFontSettings(1, 9999, 3, 5, GlyphRenderMode.SDFAA);

                DirectFontSettings resolved = config.ResolvedSettings;

                Assert.AreEqual(DirectTMPConstants.MinSamplingPointSize, resolved.SamplingPointSize);
                Assert.AreEqual(DirectTMPConstants.MinAtlasDimension, resolved.AtlasWidth);
                Assert.AreEqual(DirectTMPConstants.MinAtlasDimension, resolved.AtlasHeight);
                Assert.LessOrEqual(resolved.AtlasPadding, DirectTMPConstants.MaxAtlasPadding);
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void The_Resources_Name_And_The_Asset_Path_Agree()
        {
            // Written in two places, loaded from one of them at runtime. A
            // drift between them is a setting that exists on disk, is edited
            // happily in the Inspector, and is null in every build.
            Assert.IsTrue(
                DirectTMPConstants.GlobalConfigAssetPath.EndsWith(
                    "/" + DirectTMPConstants.GlobalConfigResourceName + ".asset"),
                DirectTMPConstants.GlobalConfigAssetPath);

            Assert.IsTrue(DirectTMPConstants.GlobalConfigAssetPath.Contains("/Resources/"),
                "Resources.Load only finds an asset that is actually inside a Resources folder.");

            Assert.IsFalse(DirectTMPConstants.GlobalConfigResourceName.Contains("/"),
                "The resource name is a Resources path, not a file path.");
        }

        // ==========================================
        // The diagnostics
        // ==========================================
        [Test]
        public void Explaining_A_Missing_Font_Still_Says_Something()
        {
            string explanation = DirectFontDiagnostics.ExplainFont(null);

            Assert.IsNotNull(explanation);
            Assert.IsNotEmpty(explanation,
                "A silent failure is the bug this whole file exists to remove.");
        }

        [Test]
        public void Asking_This_Project_What_Is_Wrong_With_It_Never_Throws()
        {
            string problem = null;
            Assert.DoesNotThrow(() => problem = DirectFontDiagnostics.ProjectProblem());

            // Whether there IS a problem depends on the project the tests are
            // running in, which is not something a test may assert. That it
            // answers in words rather than in an empty string is.
            if (problem != null) { Assert.IsNotEmpty(problem); }
        }

        // ==========================================
        // Lifecycle
        // ==========================================
        [Test]
        public void Reverting_When_Nothing_Was_Ever_Applied_Is_Safe()
        {
            Assert.DoesNotThrow(() => DirectGlobalFont.Revert());
            Assert.IsFalse(DirectGlobalFont.IsActive);
        }

        [Test]
        public void Ticking_With_No_Labels_Registered_Is_Safe()
        {
            Assert.DoesNotThrow(() => DirectTextTicker.TickAll());
        }

        [Test]
        public void Releasing_Shaping_From_A_Label_Nobody_Shaped_Is_Safe()
        {
            Assert.DoesNotThrow(() => DirectGlobalFont.ReleaseShaping(null));
        }

        // ==========================================
        // The menu
        // ==========================================
        [Test]
        public void The_Global_Font_Window_Is_Reachable_From_Both_Menus()
        {
            System.Collections.Generic.List<string> paths = DirectTMPHealth.RegisteredMenuPaths();

            CollectionAssert.Contains(paths, DirectTMPEditorConstants.MenuGlobalFont);
            CollectionAssert.Contains(paths, DirectTMPEditorConstants.WindowGlobalFont);
            CollectionAssert.Contains(paths, DirectTMPEditorConstants.ContextUseEverywhere);
        }
    }
}
