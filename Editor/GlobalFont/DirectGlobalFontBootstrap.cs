// ==========================================
// DirectGlobalFontBootstrap
// Keeps the project-wide font applied in the Editor,
// and keeps it out of everything the Editor writes to
// disk.
//
// The runtime half of this feature (DirectGlobalFont)
// has one entry point in a build and it is enough,
// because a build loads once and runs. The Editor
// loads and unloads constantly: a domain reload after
// every script change, a scene opened, a scene saved,
// Play mode entered and left, a GameObject created in
// the Hierarchy. Every one of those either drops the
// override or produces a label the last sweep never
// saw, and each of them is hooked here.
//
// ==========================================
// THE SAVE GUARD
// ==========================================
// The single most important thing in this file.
//
// The override applies a font asset that is generated
// at load and never saved to disk. If Unity writes a
// scene while that asset is assigned to forty labels,
// it writes forty references to an object that has no
// file - which serialize as null. Reopening the scene
// then shows forty labels with no font at all until
// the override re-applies, and a version-control diff
// full of changes nobody made.
//
// So every path Unity can save through is bracketed:
// everything goes back to what the project actually
// declares, Unity writes THAT, and the override is put
// back on afterwards. The scene on disk is byte-for-
// byte the scene the user built, whether or not the
// override was ever on.
// ==========================================
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityDirectTMP.Editor
{
    /// <summary>
    /// Applies and maintains the project-wide font override inside the Editor,
    /// and makes sure nothing it does is ever written to disk.
    /// </summary>
    [InitializeOnLoad]
    internal static class DirectGlobalFontBootstrap
    {
        private static bool s_sweepPending;
        private static bool s_resumePending;

        static DirectGlobalFontBootstrap()
        {
            // Deferred: at static-constructor time the AssetDatabase will not
            // answer, so Resources.Load returns null and the override would
            // silently skip the reload that matters most.
            EditorApplication.delayCall += () =>
            {
                DirectGlobalFont.InvalidateConfig();
                ApplyNow();

                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;

                EditorApplication.hierarchyChanged -= OnHierarchyChanged;
                EditorApplication.hierarchyChanged += OnHierarchyChanged;

                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;

                EditorSceneManager.sceneOpened -= OnSceneOpened;
                EditorSceneManager.sceneOpened += OnSceneOpened;

                EditorSceneManager.sceneSaving -= OnSceneSaving;
                EditorSceneManager.sceneSaving += OnSceneSaving;

                EditorSceneManager.sceneSaved -= OnSceneSaved;
                EditorSceneManager.sceneSaved += OnSceneSaved;
            };
        }

        // ==========================================
        // Apply
        // ==========================================
        /// <summary>
        /// Re-reads the settings asset and applies it, including the Editor
        /// chrome if the project asked for that. Safe to call at any time.
        /// </summary>
        public static void ApplyNow()
        {
            DirectGlobalFont.InvalidateConfig();

            DirectGlobalFontConfig config = DirectGlobalFont.Config;
            if (config == null) { return; }

            // Repair only when something actually failed. Import settings are
            // the reason the same .ttf works in one project and not in
            // another - and this is the one font the user pointed at by hand,
            // so it is repaired rather than reported - but a SaveAndReimport
            // on every domain reload of a healthy project would be a reimport
            // nobody asked for, every time anybody touches a script.
            if (!DirectGlobalFont.Apply() && config.IsActive)
            {
                if (RepairImport(config.FontFile)) { DirectGlobalFont.Apply(); }
            }

            ApplyEditorChrome(config);
        }

        // ==========================================
        // The Editor's own chrome
        //
        // The same font, in the menu bar, the Hierarchy,
        // the Inspector and the Console - because a
        // project whose UI is Persian has Persian
        // GameObject names and Persian folders, and a
        // Hierarchy full of empty boxes is the same
        // problem one layer up.
        //
        // The per-machine EditorPrefs plan still wins if
        // somebody set one: a developer who deliberately
        // chose a different Editor font should not have
        // the project overrule them on the next reload.
        // ==========================================
        private static void ApplyEditorChrome(DirectGlobalFontConfig config)
        {
            string wantedPath = config != null && config.RestyleEditorChrome && config.FontFile != null
                ? AssetDatabase.GetAssetPath(config.FontFile)
                : null;

            DirectEditorFontPlan stored = DirectEditorFontSettings.Plan;

            // The stored plan is where this has to be written, not just
            // applied: DirectEditorFont keeps a heartbeat that reverts
            // anything the stored plan does not account for, so an override
            // applied without storing it would come back off a second later.
            if (string.IsNullOrEmpty(wantedPath))
            {
                // Take back only what this setting put there. A font somebody
                // chose by hand in the Editor Font window is their business,
                // and a project setting is not consent to undo it.
                bool ours = stored.IsActive
                            && stored.Source == DirectEditorFontSource.ProjectFont
                            && config != null && config.FontFile != null
                            && stored.AssetPath == AssetDatabase.GetAssetPath(config.FontFile);

                if (ours) { DirectEditorFontSettings.Clear(); }
                return;
            }

            if (stored.IsActive)
            {
                // Already ours, or somebody's personal choice. Either way there
                // is nothing to do and nothing to overrule.
                return;
            }

            var plan = new DirectEditorFontPlan(true, DirectEditorFontSource.ProjectFont, wantedPath, null, 0);

            // A font that cannot draw Latin letters would turn "Revert to
            // Unity Default" into a row of empty boxes. The refusal is the
            // feature; it is checked here exactly as the window checks it.
            DirectEditorFontProblem problem = DirectEditorFont.Validate(plan);
            if (DirectEditorFontRules.IsFatal(problem))
            {
                DirectTMPLog.Warn(
                    "The project asked for Unity's own interface to be drawn in "
                    + $"'{config.FontFile.name}', and it was refused: {DirectEditorFontRules.Describe(problem)}");
                return;
            }

            DirectEditorFontSettings.Plan = plan;
            DirectEditorFont.Apply(plan, silent: true);
        }

        // ==========================================
        // Import repair
        //
        // "Include Font Data" off, or a baked character
        // set, is the difference between a font that
        // works and one that produces nothing - and it
        // lives in the .meta file, which is why the same
        // .ttf behaves differently in two projects.
        //
        // The package can read the file directly and work
        // around both. It repairs them anyway, because
        // every OTHER tool in the project - the legacy UI
        // Text, a third-party label, Unity's own
        // GUIStyle - reads the imported asset and cannot
        // work around anything.
        // ==========================================
        /// <summary>
        /// Switches a font's importer to Dynamic with font data included, if
        /// it is not already. Returns true when something was changed.
        /// </summary>
        public static bool RepairImport(Font font)
        {
            if (font == null) { return false; }

            string path = AssetDatabase.GetAssetPath(font);
            if (string.IsNullOrEmpty(path)) { return false; }

            return RepairImport(path);
        }

        /// <summary>The same, by asset path.</summary>
        public static bool RepairImport(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) { return false; }

            var importer = AssetImporter.GetAtPath(assetPath) as TrueTypeFontImporter;
            if (importer == null) { return false; }

            bool changed = false;

            if (!importer.includeFontData)
            {
                importer.includeFontData = true;
                changed = true;
            }

            if (importer.fontTextureCase != FontTextureCase.Dynamic)
            {
                importer.fontTextureCase = FontTextureCase.Dynamic;
                changed = true;
            }

            if (!changed) { return false; }

            importer.SaveAndReimport();
            DirectTMPLog.Info(
                $"'{System.IO.Path.GetFileName(assetPath)}' was imported with settings that stop TextMeshPro reading it "
                + "(Include Font Data off, or a baked character set). Both were corrected — this is the usual reason "
                + "the same font file works in one project and not in another.");
            return true;
        }

        // ==========================================
        // Editor heartbeat
        //
        // The Hierarchy changing is the Editor's version
        // of "a label was instantiated": precise, free,
        // and fired far too often to sweep from directly.
        // So it raises a flag and the next update pass
        // does the work once.
        // ==========================================
        private static void Tick()
        {
            if (s_sweepPending)
            {
                s_sweepPending = false;
                if (!Application.isPlaying) { DirectGlobalFont.Sweep(); }
            }

            if (s_resumePending)
            {
                s_resumePending = false;
                DirectGlobalFont.ResumeAfterSave();
            }
        }

        private static void OnHierarchyChanged()
        {
            if (!DirectGlobalFont.IsActive) { return; }
            s_sweepPending = true;
        }

        // ==========================================
        // Scene open
        //
        // The report this whole design exists to answer:
        // "it disappears when the scene reloads". It
        // cannot disappear, because nothing was ever
        // stored - the scene simply arrives without it,
        // and this puts it on.
        // ==========================================
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (DirectGlobalFont.IsActive) { DirectGlobalFont.Sweep(); }
            else { ApplyNow(); }
        }

        // ==========================================
        // Play mode
        //
        // Leaving edit mode serializes the scene, so the
        // override comes off first and the snapshot Unity
        // takes is the project's own. Entering play mode
        // reloads the domain and the runtime bootstrap
        // applies it again; coming back to edit mode has
        // no bootstrap of its own, so this is it.
        // ==========================================
        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                    DirectGlobalFont.SuspendForSave();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    ApplyNow();
                    break;
            }
        }

        // ==========================================
        // Saving
        // ==========================================
        private static void OnSceneSaving(Scene scene, string path)
        {
            DirectGlobalFont.SuspendForSave();
        }

        private static void OnSceneSaved(Scene scene)
        {
            DirectGlobalFont.ResumeAfterSave();
        }

        /// <summary>Asked for by <see cref="DirectGlobalFontSaveGuard"/> after a save.</summary>
        internal static void ResumeOnNextTick() => s_resumePending = true;

        // ==========================================
        // The settings asset
        // ==========================================
        /// <summary>
        /// The project-wide settings asset, created (with its Resources
        /// folder) if the project does not have one yet.
        /// </summary>
        public static DirectGlobalFontConfig EnsureConfigAsset()
        {
            DirectGlobalFontConfig existing = DirectGlobalFont.Config;
            if (existing != null) { return existing; }

            var loaded = AssetDatabase.LoadAssetAtPath<DirectGlobalFontConfig>(DirectTMPConstants.GlobalConfigAssetPath);
            if (loaded != null)
            {
                DirectGlobalFont.InvalidateConfig();
                return DirectGlobalFont.Config ?? loaded;
            }

            try
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }

                var created = ScriptableObject.CreateInstance<DirectGlobalFontConfig>();
                created.Active = false;   // nothing changes until a font is picked
                AssetDatabase.CreateAsset(created, DirectTMPConstants.GlobalConfigAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                DirectGlobalFont.InvalidateConfig();
                return DirectGlobalFont.Config ?? created;
            }
            catch (Exception e)
            {
                DirectTMPLog.Error(
                    $"Could not create {DirectTMPConstants.GlobalConfigAssetPath}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Writes the settings asset back to disk and re-applies it. Called by
        /// the window after every edit.
        /// </summary>
        public static void SaveAndApply(DirectGlobalFontConfig config)
        {
            if (config == null) { return; }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            ApplyNow();
            InternalRepaint();
        }

        private static void InternalRepaint()
        {
            try { UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); }
            catch (Exception) { /* nothing worth reporting */ }
        }
    }

    // ==========================================
    // DirectGlobalFontSaveGuard
    //
    // The saves that do not go through the scene
    // callbacks: Save Project, an asset written by a
    // tool, the implicit save before a domain reload.
    // Same bracket as a scene save - come off, let Unity
    // write the project's own state, go back on.
    //
    // Top-level rather than nested inside the bootstrap
    // because Unity discovers these by scanning for
    // subclasses, and a top-level class is the shape
    // every version of that scan has agreed on.
    // ==========================================
    internal sealed class DirectGlobalFontSaveGuard : UnityEditor.AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            if (DirectGlobalFont.IsActive)
            {
                DirectGlobalFont.SuspendForSave();
                DirectGlobalFontBootstrap.ResumeOnNextTick();
            }
            return paths;
        }
    }
}
