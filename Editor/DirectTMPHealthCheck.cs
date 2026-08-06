// ==========================================
// DirectTMPHealthCheck
// The answer to "I installed it and nothing
// happened."
//
// This package's single most common report is that the
// Unity DirectTMP menu is not in the menu bar. It has
// essentially never been the menu. When an editor
// assembly fails to compile, Unity drops every
// [MenuItem] inside it at once - so a missing
// dependency (TextMeshPro, in almost every case)
// removes the entire top-level menu, and what the user
// sees is a package that installed and then did
// nothing at all. The compile error is sitting in the
// Console, three hundred lines up, next to somebody
// else's warnings.
//
// So the tool checks itself and says so in sentences.
// Each check states what it looked for, what it found,
// and - when it found a problem - the exact next
// action, with a button that performs it where a
// button can.
//
// The report is also copyable as plain text, because
// the second half of "it doesn't work" is a bug report
// that has to travel to someone else. A person who can
// paste eighteen facts about their setup gets a real
// answer; a person who can only say "the menu is
// missing" gets four rounds of questions.
//
// The checks and their severities are pure data over
// facts gathered by the caller, so the report logic is
// unit-tested without needing a broken project to
// point it at.
// ==========================================
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    /// <summary>How much a finding matters.</summary>
    internal enum DirectHealthLevel
    {
        /// <summary>Working as intended. Reported anyway - a checklist of only failures is not a checklist.</summary>
        Ok = 0,

        /// <summary>Works, but something is not set up the way it should be.</summary>
        Warning = 1,

        /// <summary>The tool cannot do its job until this is fixed.</summary>
        Problem = 2
    }

    /// <summary>One line of the report.</summary>
    internal sealed class DirectHealthEntry
    {
        public DirectHealthLevel Level;

        /// <summary>What was checked, e.g. "TextMeshPro".</summary>
        public string Subject = string.Empty;

        /// <summary>What was found, in one line.</summary>
        public string Finding = string.Empty;

        /// <summary>What to do about it. Empty when there is nothing to do.</summary>
        public string Advice = string.Empty;

        /// <summary>An optional one-click fix, labelled by <see cref="ActionLabel"/>.</summary>
        public Action Fix;

        public string ActionLabel = string.Empty;
    }

    /// <summary>The whole report.</summary>
    internal sealed class DirectHealthReport
    {
        public readonly List<DirectHealthEntry> Entries = new List<DirectHealthEntry>();

        /// <summary>The worst level in the report. Drives the summary banner.</summary>
        public DirectHealthLevel Worst
        {
            get
            {
                DirectHealthLevel worst = DirectHealthLevel.Ok;
                foreach (DirectHealthEntry entry in Entries)
                {
                    if (entry.Level > worst) { worst = entry.Level; }
                }
                return worst;
            }
        }

        public int CountAt(DirectHealthLevel level)
        {
            int count = 0;
            foreach (DirectHealthEntry entry in Entries)
            {
                if (entry.Level == level) { count++; }
            }
            return count;
        }

        /// <summary>
        /// Problems first, then warnings, then the things that are fine.
        /// Sorting rather than appending in order matters because the checks
        /// are written in the order they are cheapest to run, and that is
        /// never the order they are most urgent in.
        /// </summary>
        public List<DirectHealthEntry> Sorted()
        {
            var sorted = new List<DirectHealthEntry>(Entries);
            sorted.Sort((a, b) =>
            {
                int byLevel = b.Level.CompareTo(a.Level);
                return byLevel != 0
                    ? byLevel
                    : string.Compare(a.Subject, b.Subject, StringComparison.Ordinal);
            });
            return sorted;
        }
    }

    /// <summary>
    /// Gathers the facts and turns them into a report.
    /// </summary>
    internal static class DirectTMPHealth
    {
        // ==========================================
        // Run
        // ==========================================
        public static DirectHealthReport Run()
        {
            var report = new DirectHealthReport();

            CheckPackage(report);
            CheckTextMeshPro(report);
            CheckTmpResources(report);
            CheckShader(report);
            CheckMenuRegistration(report);
            CheckProjectFonts(report);
            CheckGlobalFont(report);
            CheckEditorFont(report);
            CheckCache(report);

            return report;
        }

        // ==========================================
        // The shader every generated font sits on
        //
        // Separate from the resources check above because
        // the two fail apart: a project can carry the TMP
        // Settings asset and still have lost the shaders
        // to a botched upgrade or an aggressive .gitignore.
        // A material built on a null shader is not an
        // error anybody sees - it is a label that renders
        // nothing, which reads as this package being
        // broken.
        // ==========================================
        private static void CheckShader(DirectHealthReport report)
        {
            Shader shader = DirectFontDiagnostics.FindDistanceFieldShader();

            report.Entries.Add(new DirectHealthEntry
            {
                Level = shader != null ? DirectHealthLevel.Ok : DirectHealthLevel.Problem,
                Subject = "TMP shader",
                Finding = shader != null
                    ? $"Found — {shader.name}."
                    : "TextMeshPro's distance-field shader is not in this project.",
                Advice = shader != null
                    ? string.Empty
                    : "Every font this package builds needs it, and a material without it draws nothing at all. "
                    + "Re-import Window ▸ TextMeshPro ▸ Import TMP Essential Resources.",
                ActionLabel = shader != null ? string.Empty : "Open the TextMeshPro menu",
                Fix = shader != null
                    ? null
                    : (Action)(() => EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources"))
            });
        }

        // ==========================================
        // The project-wide font
        //
        // Three states worth telling apart, because they
        // send somebody to three different places: never
        // set up, set up and working, set up and refusing
        // to apply. The third one carries the reason.
        // ==========================================
        private static void CheckGlobalFont(DirectHealthReport report)
        {
            DirectGlobalFontConfig config = DirectGlobalFont.Config;

            if (config == null || !config.IsActive)
            {
                report.Entries.Add(new DirectHealthEntry
                {
                    Level = DirectHealthLevel.Ok,
                    Subject = "Global font",
                    Finding = config == null
                        ? "Not set up — every label keeps its own font asset."
                        : "Configured but switched off.",
                    Advice = "Unity DirectTMP ▸ Global Font… points every TextMeshPro label in the project at one "
                           + "font file, with no component on any GameObject and nothing written into any scene.",
                    ActionLabel = "Open the Global Font window",
                    Fix = DirectGlobalFontWindow.ShowWindow
                });
                return;
            }

            bool applied = DirectGlobalFont.IsActive;
            string problem = DirectGlobalFont.LastProblem;

            report.Entries.Add(new DirectHealthEntry
            {
                Level = applied ? DirectHealthLevel.Ok : DirectHealthLevel.Problem,
                Subject = "Global font",
                Finding = applied
                    ? $"'{config.FontFile.name}' — {DirectGlobalFont.LabelsUsingFont} label(s) drawn from it, "
                    + $"{DirectGlobalFont.LabelsShaped} shaped."
                    : $"'{config.FontFile.name}' is configured but has not been applied.",
                Advice = applied ? string.Empty : (problem ?? "Open the window and press Re-apply."),
                ActionLabel = "Open the Global Font window",
                Fix = DirectGlobalFontWindow.ShowWindow
            });
        }

        // ==========================================
        // The package itself
        // ==========================================
        private static void CheckPackage(DirectHealthReport report)
        {
            report.Entries.Add(new DirectHealthEntry
            {
                Level = DirectHealthLevel.Ok,
                Subject = DirectTMPConstants.ToolName,
                Finding = $"v{DirectTMPConstants.Version} · Unity {Application.unityVersion} · "
                        + (EditorGUIUtility.isProSkin ? "dark skin" : "light skin")
            });
        }

        // ==========================================
        // TextMeshPro
        //
        // The single most important check in the file.
        // If this assembly is running at all then TMP
        // resolved at compile time, so a failure here is
        // nearly impossible to reach - which is exactly
        // why it is worth stating positively. Somebody
        // reading this window has been told the tool
        // might be missing a dependency, and "found, at
        // this version" ends that theory in one line.
        // ==========================================
        private static void CheckTextMeshPro(DirectHealthReport report)
        {
            Type fontAsset = typeof(TMPro.TMP_FontAsset);
            Assembly assembly = fontAsset.Assembly;
            string version = assembly.GetName().Version.ToString();

            report.Entries.Add(new DirectHealthEntry
            {
                Level = DirectHealthLevel.Ok,
                Subject = "TextMeshPro",
                Finding = $"Found — {assembly.GetName().Name} {version}"
            });
        }

        // ==========================================
        // TMP Essential Resources
        //
        // TextMeshPro ships as an assembly plus a folder
        // of resources that Unity imports on first use
        // through a dialog people close. Without them
        // TMP_Settings is null, every label falls back to
        // nothing, and DirectTMP's generated font assets
        // have no default material to sit on - a failure
        // that looks exactly like this package being
        // broken.
        // ==========================================
        private static void CheckTmpResources(DirectHealthReport report)
        {
            bool present;
            try
            {
                present = TMPro.TMP_Settings.instance != null;
            }
            catch (Exception)
            {
                present = false;
            }

            if (present)
            {
                report.Entries.Add(new DirectHealthEntry
                {
                    Level = DirectHealthLevel.Ok,
                    Subject = "TMP Essential Resources",
                    Finding = "Imported."
                });
                return;
            }

            report.Entries.Add(new DirectHealthEntry
            {
                Level = DirectHealthLevel.Problem,
                Subject = "TMP Essential Resources",
                Finding = "Not imported into this project.",
                Advice = "TextMeshPro needs its resource folder before any label can render. "
                       + "Window ▸ TextMeshPro ▸ Import TMP Essential Resources.",
                ActionLabel = "Open the TextMeshPro menu",
                Fix = () => EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources")
            });
        }

        // ==========================================
        // Menu registration
        //
        // Counted by reflecting over this assembly's own
        // [MenuItem] attributes. It cannot prove the items
        // are drawn - Unity owns that - but it proves the
        // assembly compiled and the attributes are there,
        // which is the half of "the menu is missing" that
        // this package is responsible for.
        // ==========================================
        private static void CheckMenuRegistration(DirectHealthReport report)
        {
            List<string> paths = RegisteredMenuPaths();

            report.Entries.Add(new DirectHealthEntry
            {
                Level = paths.Count > 0 ? DirectHealthLevel.Ok : DirectHealthLevel.Problem,
                Subject = "Menu items",
                Finding = paths.Count > 0
                    ? $"{paths.Count} registered, including \"{DirectTMPEditorConstants.MenuCatalog}\"."
                    : "None registered — the editor assembly is present but has no menu attributes.",
                Advice = paths.Count > 0
                    ? string.Empty
                    : "This should be impossible if you can read this window. Please file an issue with the copied report."
            });
        }

        /// <summary>
        /// Every menu path this assembly declares. Used by the health report
        /// and by the test that checks the tree against the README.
        /// </summary>
        public static List<string> RegisteredMenuPaths()
        {
            var paths = new List<string>();

            try
            {
                Assembly assembly = typeof(DirectTMPMenu).Assembly;
                foreach (Type type in assembly.GetTypes())
                {
                    MethodInfo[] methods = type.GetMethods(
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                    foreach (MethodInfo method in methods)
                    {
                        foreach (MenuItem item in method.GetCustomAttributes(typeof(MenuItem), false))
                        {
                            // Validate functions share the path of the item
                            // they gate; counting both would double every
                            // entry that has one.
                            if (item.validate) { continue; }
                            if (!paths.Contains(item.menuItem)) { paths.Add(item.menuItem); }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // A type that will not load. The count is a diagnostic, not a
                // contract - report what was reachable.
            }

            paths.Sort(StringComparer.Ordinal);
            return paths;
        }

        // ==========================================
        // Fonts in the project
        // ==========================================
        private static void CheckProjectFonts(DirectHealthReport report)
        {
            string[] guids = AssetDatabase.FindAssets("t:Font");
            int baked = 0;
            int dataless = 0;
            var broken = new List<string>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!DirectEditorFont.IsDynamicImport(path)) { baked++; broken.Add(path); continue; }

                // "Include Font Data" off is the one that produces NOTHING and
                // says nothing: Unity keeps the font's name instead of its
                // bytes, TextMeshPro's factory has nothing to rasterize, and
                // the setting lives in the .meta file rather than in the font -
                // which is exactly why the same .ttf works in one project and
                // not in the next.
                var importer = AssetImporter.GetAtPath(path) as TrueTypeFontImporter;
                if (importer != null && !importer.includeFontData) { dataless++; broken.Add(path); }
            }

            if (broken.Count > 0)
            {
                report.Entries.Add(new DirectHealthEntry
                {
                    Level = DirectHealthLevel.Problem,
                    Subject = "Font import settings",
                    Finding = dataless > 0 && baked > 0
                        ? $"{baked} font(s) with a baked character set and {dataless} with \"Include Font Data\" switched off."
                        : dataless > 0
                            ? $"{dataless} font(s) imported with \"Include Font Data\" switched off."
                            : $"{baked} font(s) imported with a baked character set.",
                    Advice = "TextMeshPro cannot read a font face out of either of those, so a label assigned one "
                           + "silently keeps whatever font it had. These settings live in the .meta file beside "
                           + "each font rather than in the font itself, which is why the same file behaves "
                           + "differently in two projects.",
                    ActionLabel = "Fix all of them",
                    Fix = () =>
                    {
                        foreach (string path in broken) { DirectGlobalFontBootstrap.RepairImport(path); }
                        AssetDatabase.Refresh();
                    }
                });
            }

            if (guids.Length == 0)
            {
                report.Entries.Add(new DirectHealthEntry
                {
                    Level = DirectHealthLevel.Warning,
                    Subject = "Project fonts",
                    Finding = "No font files in this project yet.",
                    Advice = "Drop a .ttf or .otf anywhere in Assets. Everything this package does starts from one.",
                    ActionLabel = "Open the Font Catalog",
                    Fix = DirectFontCatalogWindow.ShowWindow
                });
                return;
            }

            // The count only. Whatever is WRONG with those imports has its own
            // entry above, with the button that fixes it - saying it twice in
            // one report just makes the report longer.
            report.Entries.Add(new DirectHealthEntry
            {
                Level = DirectHealthLevel.Ok,
                Subject = "Project fonts",
                Finding = broken.Count > 0
                    ? $"{guids.Length} font(s); {broken.Count} imported in a way TextMeshPro cannot read."
                    : $"{guids.Length} font(s), all readable.",
                ActionLabel = "Open the Font Catalog",
                Fix = DirectFontCatalogWindow.ShowWindow
            });
        }

        // ==========================================
        // The Editor font override
        // ==========================================
        private static void CheckEditorFont(DirectHealthReport report)
        {
            DirectEditorFontPlan plan = DirectEditorFontSettings.Plan;

            if (!plan.IsActive)
            {
                report.Entries.Add(new DirectHealthEntry
                {
                    Level = DirectHealthLevel.Ok,
                    Subject = "Editor font",
                    Finding = "Unity's own font (not overridden).",
                    ActionLabel = "Change it",
                    Fix = DirectEditorFontWindow.ShowWindow
                });
                return;
            }

            DirectEditorFontProblem problem = DirectEditorFont.Validate(plan);
            bool applied = DirectEditorFont.IsActive;

            report.Entries.Add(new DirectHealthEntry
            {
                Level = problem == DirectEditorFontProblem.None && applied
                    ? DirectHealthLevel.Ok
                    : DirectHealthLevel.Warning,
                Subject = "Editor font",
                Finding = applied
                    ? $"{plan.Describe()} — {DirectEditorFont.PatchedStyleCount} style(s) restyled."
                    : $"{plan.Describe()} — saved but not currently applied.",
                Advice = problem == DirectEditorFontProblem.None
                    ? string.Empty
                    : DirectEditorFontRules.Describe(problem),
                ActionLabel = "Open the Editor Font window",
                Fix = DirectEditorFontWindow.ShowWindow
            });
        }

        // ==========================================
        // The font cache
        // ==========================================
        private static void CheckCache(DirectHealthReport report)
        {
            int cached = DirectTMP.CachedFontCount;
            string folder = DirectTMPSettings.ResolveRuntimeCacheFolderAbsolute();
            int spooled = 0;

            try
            {
                if (System.IO.Directory.Exists(folder)) { spooled = System.IO.Directory.GetFiles(folder).Length; }
            }
            catch (Exception)
            {
                spooled = -1;
            }

            report.Entries.Add(new DirectHealthEntry
            {
                Level = DirectHealthLevel.Ok,
                Subject = "Font cache",
                Finding = spooled < 0
                    ? $"{cached} atlas(es) in memory."
                    : $"{cached} atlas(es) in memory · {spooled} spooled file(s) on disk.",
                ActionLabel = cached > 0 || spooled > 0 ? "Clear it" : string.Empty,
                Fix = cached > 0 || spooled > 0
                    ? (Action)(() => EditorApplication.ExecuteMenuItem(DirectTMPEditorConstants.MenuClearCache))
                    : null
            });
        }

        // ==========================================
        // Format
        //
        // The copyable version. Plain text on purpose:
        // it is going into an issue, a chat message or an
        // email, and every one of those mangles anything
        // fancier.
        // ==========================================
        public static string Format(DirectHealthReport report)
        {
            var text = new StringBuilder();
            text.AppendLine($"{DirectTMPConstants.ToolName} v{DirectTMPConstants.Version} — health check");
            text.AppendLine($"Unity {Application.unityVersion} · {Application.platform}");
            text.AppendLine();

            foreach (DirectHealthEntry entry in report.Sorted())
            {
                text.Append(Marker(entry.Level)).Append(' ').Append(entry.Subject).Append(": ").AppendLine(entry.Finding);
                if (!string.IsNullOrEmpty(entry.Advice))
                {
                    text.Append("    → ").AppendLine(entry.Advice);
                }
            }

            return text.ToString();
        }

        /// <summary>
        /// The ASCII marker for a level. Not an emoji: this string is pasted
        /// into terminals and issue trackers that render emoji as a question
        /// mark or as nothing.
        /// </summary>
        public static string Marker(DirectHealthLevel level)
        {
            switch (level)
            {
                case DirectHealthLevel.Problem: return "[!]";
                case DirectHealthLevel.Warning: return "[~]";
                default: return "[ok]";
            }
        }
    }
}
