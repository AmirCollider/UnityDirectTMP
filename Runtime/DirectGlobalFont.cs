// ==========================================
// DirectGlobalFont
// The whole package, with nothing added to anything.
//
// Point it at a .ttf once - Unity DirectTMP ▸ Global
// Font… - and every TextMeshPro label in the project
// is drawn from that file, joined and in reading order
// where the script needs it, in the Editor, in Play
// mode and in the build. No component on any label, no
// prefab touched, no scene modified.
//
// ==========================================
// WHY "NO COMPONENT" IS THE WHOLE POINT
// ==========================================
// A component has to be added to every label, kept on
// every label, and added again to every label anybody
// makes next week. Worse, it has to store what it
// applied - and what it applies is a font asset built
// at runtime, which is never saved to disk. So a scene
// that stored it stored a reference to nothing, and
// the reference came back null on the next reload.
// That is the "it disappears when the scene reloads"
// report, and it is not a bug that can be fixed inside
// that design; it is the design.
//
// So nothing is stored. The settings live in one asset
// in Resources, and the application happens on every
// load: domain reload, scene open, entering Play mode,
// scene loaded at runtime, additive scene loaded at
// runtime. There is nothing in a scene to lose,
// therefore nothing a reload can lose.
//
// ==========================================
// TWO MECHANISMS, ON PURPOSE
// ==========================================
//   1. Replace label fonts. A sweep of every loaded
//      TMP_Text, setting label.font. Strong - the text
//      is drawn in YOUR font - but it can only reach
//      labels that exist.
//
//   2. TextMeshPro's own project-wide fallback list.
//      One list, consulted by every label that ever
//      draws, including ones instantiated in the tenth
//      minute of play and ones inside packages you did
//      not write. It cannot change how anything LOOKS,
//      but no label anywhere can show □□□ for a
//      character your font contains.
//
// Between them there is no label the override cannot
// reach, and neither one writes to disk.
//
// ==========================================
// WHAT IT DOES NOT TOUCH
// ==========================================
//   * A label with its own Direct Font component. That
//     is somebody being explicit, and explicit wins.
//   * The text component inside a TMP_InputField -
//     reordering it would put the caret in the wrong
//     place. Its placeholder is shaped as normal.
//   * TMP Settings on disk. The default-font and
//     fallback fields are patched in memory and put
//     back before Unity is allowed to save the asset,
//     so the change never reaches version control.
// ==========================================
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityDirectTMP
{
    /// <summary>
    /// Applies one font file to every TextMeshPro label in the project,
    /// without adding a component to anything and without modifying a scene.
    /// </summary>
    public static class DirectGlobalFont
    {
        // ==========================================
        // Configuration
        // ==========================================
        private static DirectGlobalFontConfig s_config;
        private static bool s_configResolved;

        /// <summary>
        /// The project-wide settings asset, or null when the project has none
        /// yet. Loaded from Resources once per domain.
        /// </summary>
        public static DirectGlobalFontConfig Config
        {
            get
            {
                if (!s_configResolved)
                {
                    s_configResolved = true;
                    s_config = Resources.Load<DirectGlobalFontConfig>(DirectTMPConstants.GlobalConfigResourceName);
                }
                return s_config;
            }
        }

        /// <summary>
        /// Forgets the cached settings asset, so the next call re-loads it.
        /// Called by the editor after the asset is created or edited.
        /// </summary>
        public static void InvalidateConfig()
        {
            s_configResolved = false;
            s_config = null;
        }

        // ==========================================
        // State
        // ==========================================
        /// <summary>The dynamic font asset currently driving the project, or null.</summary>
        public static TMP_FontAsset Asset { get; private set; }

        /// <summary>Is the project-wide override applied right now?</summary>
        public static bool IsActive => Asset != null;

        /// <summary>Why the last <see cref="Apply"/> could not run, or null.</summary>
        public static string LastProblem { get; private set; }

        // A count of what IS, not of what changed. The second sweep over an
        // already-swept scene changes nothing, and a window that then reported
        // "0 labels" would be reporting that the feature had stopped working.
        /// <summary>How many labels are currently being drawn from this font.</summary>
        public static int LabelsUsingFont { get; private set; }

        /// <summary>How many labels the override is currently shaping.</summary>
        public static int LabelsShaped => s_shaped.Count;

        // What each label was drawing with before the sweep touched it, so a
        // revert - and the moment before Unity saves a scene - can put it back
        // exactly. Keyed by instance id: a destroyed label must not be kept
        // alive by this table, and an int cannot keep anything alive.
        private static readonly Dictionary<int, TMP_FontAsset> s_originalFonts = new Dictionary<int, TMP_FontAsset>();
        private static readonly Dictionary<int, DirectTextEngine> s_shaped = new Dictionary<int, DirectTextEngine>();

        private static TMP_FontAsset s_previousDefault;
        private static bool s_settingsPatched;
        private static bool s_sceneHooked;

        // ==========================================
        // Boot
        //
        // BeforeSceneLoad, so the first scene's labels
        // are swept before anything draws. A player build
        // has no other entry point - there is no
        // InitializeOnLoad out there - which is why this
        // lives in the runtime assembly rather than the
        // editor one.
        // ==========================================
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BootRuntime()
        {
            InvalidateConfig();
            Apply();
        }

        // ==========================================
        // Apply
        // ==========================================
        /// <summary>
        /// Builds the configured font and pushes it across the project.
        /// Returns false - with <see cref="LastProblem"/> set - when there is
        /// nothing to apply or the font could not be read.
        /// </summary>
        public static bool Apply()
        {
            DirectGlobalFontConfig config = Config;
            if (config == null || !config.IsActive)
            {
                Revert();
                LastProblem = null;
                return false;
            }

            if (!Application.isPlaying && !config.ApplyInEditMode)
            {
                Revert();
                LastProblem = null;
                return false;
            }

            DirectFontDiagnostics.ReportProjectOnce();

            DirectFontSettings settings = config.ResolvedSettings;
            TMP_FontAsset asset = DirectFontLoader.FromFont(config.FontFile, settings);
            if (asset == null)
            {
                LastProblem = DirectFontDiagnostics.ExplainFont(config.FontFile);
                return false;
            }

            ApplyExtraFonts(config, asset, settings);

            Asset = asset;
            LastProblem = null;

            PatchTmpSettings(config, asset);
            HookSceneLoaded();
            Sweep();
            DirectTMPDriver.Ensure();

            return true;
        }

        private static void ApplyExtraFonts(DirectGlobalFontConfig config, TMP_FontAsset primary, DirectFontSettings settings)
        {
            List<Font> extras = config.MoreFonts;
            if (extras == null || extras.Count == 0) { return; }

            var chain = new List<TMP_FontAsset>(extras.Count);
            for (int i = 0; i < extras.Count; i++)
            {
                if (extras[i] == null) { continue; }
                TMP_FontAsset built = DirectFontLoader.FromFont(extras[i], settings);
                if (built != null) { chain.Add(built); }
            }

            if (chain.Count > 0) { DirectFontFallback.Apply(primary, chain); }
        }

        // ==========================================
        // Sweep
        //
        // Every loaded label, once. Cheap enough to run
        // on every scene load - it is one FindObjectsOfType
        // and a reference comparison per label - and the
        // comparison means a second sweep over an
        // already-swept scene changes nothing and
        // allocates nothing.
        // ==========================================
        /// <summary>
        /// Applies the override to every TextMeshPro label currently loaded.
        /// Returns how many had their font changed.
        /// </summary>
        public static int Sweep()
        {
            DirectGlobalFontConfig config = Config;
            if (config == null || !config.IsActive || Asset == null) { return 0; }

            TMP_Text[] labels = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
            int changed = 0;
            int using_ = 0;

            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (ApplyToLabel(label, config)) { changed++; }
                if (label != null && ReferenceEquals(label.font, Asset)) { using_++; }
            }

            LabelsUsingFont = using_;
            return changed;
        }

        private static bool ApplyToLabel(TMP_Text label, DirectGlobalFontConfig config)
        {
            if (label == null) { return false; }

            // Somebody was explicit about this label. Explicit wins.
            if (label.GetComponent<DirectFont>() != null) { return false; }

            bool changed = false;

            if (config.ReplaceLabelFonts && !ReferenceEquals(label.font, Asset))
            {
                int id = label.GetInstanceID();
                if (!s_originalFonts.ContainsKey(id)) { s_originalFonts[id] = label.font; }

                label.font = Asset;
                label.havePropertiesChanged = true;
                changed = true;
            }

            if (config.ShapeRightToLeft && label.GetComponent<DirectText>() == null)
            {
                EnsureShaping(label, config);
            }

            return changed;
        }

        // ==========================================
        // Shaping, without a component
        //
        // One DirectTextEngine per label, held here
        // rather than on the GameObject. TextMeshPro's
        // textPreprocessor is not serialized, so this
        // leaves no trace in the scene file at all - and
        // an engine whose label has been destroyed is
        // dropped by the ticker on its next pass.
        // ==========================================
        private static void EnsureShaping(TMP_Text label, DirectGlobalFontConfig config)
        {
            int id = label.GetInstanceID();

            if (s_shaped.TryGetValue(id, out DirectTextEngine existing))
            {
                if (existing != null && existing.Text != null)
                {
                    existing.Direction = config.Direction;
                    existing.AlignToDirection = config.AlignToDirection;
                    existing.FixWrappedLines = config.FixWrappedLines;
                    return;
                }
                s_shaped.Remove(id);
            }

            var engine = new DirectTextEngine(label)
            {
                Direction = config.Direction,
                AlignToDirection = config.AlignToDirection,
                FixWrappedLines = config.FixWrappedLines,
                SubstituteMissingGlyphs = true
            };

            // Quiet: a project-wide sweep meeting forty input fields should
            // not write forty identical refusals to the console. A user who
            // added the component by hand asked a direct question and gets a
            // direct answer; a sweep did not ask.
            if (!engine.Install(quiet: true)) { return; }

            s_shaped[id] = engine;
            engine.Prepare();
        }

        /// <summary>
        /// Hands a label back if the project-wide override was shaping it.
        /// Called when a <see cref="DirectText"/> component is enabled on the
        /// same label, so one label is never driven by two preprocessors.
        /// </summary>
        public static void ReleaseShaping(TMP_Text label)
        {
            if (label == null || s_shaped.Count == 0) { return; }

            int id = label.GetInstanceID();
            if (!s_shaped.TryGetValue(id, out DirectTextEngine engine)) { return; }

            s_shaped.Remove(id);
            engine?.Uninstall();
        }

        // ==========================================
        // Revert
        //
        // Every label back to the font it had, every
        // preprocessor unhooked, TMP Settings back to
        // what it declared. Anything already destroyed is
        // skipped rather than reported - it is gone, and
        // that is the correct end state for it.
        // ==========================================
        /// <summary>Takes the project-wide override back off everything.</summary>
        public static void Revert()
        {
            foreach (KeyValuePair<int, DirectTextEngine> entry in s_shaped)
            {
                entry.Value?.Uninstall();
            }
            s_shaped.Clear();

            RestoreOriginalFonts();
            UnpatchTmpSettings();

            Asset = null;
            LabelsUsingFont = 0;
        }

        private static void RestoreOriginalFonts()
        {
            if (s_originalFonts.Count == 0) { return; }

            TMP_Text[] labels = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (label == null) { continue; }

                if (!s_originalFonts.TryGetValue(label.GetInstanceID(), out TMP_FontAsset original)) { continue; }
                if (!ReferenceEquals(label.font, Asset)) { continue; }

                label.font = original;
                label.havePropertiesChanged = true;
            }

            s_originalFonts.Clear();
        }

        // ==========================================
        // Saving
        //
        // The override lives in memory. A scene or an
        // asset saved WHILE it is applied would write a
        // reference to a font asset that is never saved
        // to disk - which serializes as null and leaves
        // the project a little worse than it started.
        //
        // So the editor calls these two around every
        // save: everything goes back to what the project
        // actually declares, Unity writes that, and the
        // override is put on again afterwards.
        // ==========================================
        /// <summary>Puts the project back to its on-disk state, ready to be saved.</summary>
        public static void SuspendForSave()
        {
            if (!IsActive) { return; }

            RestoreOriginalFonts();
            UnpatchTmpSettings();
        }

        /// <summary>Re-applies the override after a save has finished.</summary>
        public static void ResumeAfterSave()
        {
            if (Asset == null) { return; }

            DirectGlobalFontConfig config = Config;
            if (config == null || !config.IsActive) { return; }

            PatchTmpSettings(config, Asset);
            Sweep();
        }

        // ==========================================
        // TMP Settings
        //
        // Two fields, both private, both reachable only
        // by reflection - and both worth it. The default
        // font asset is what a brand-new TextMeshPro
        // component picks up, so a label somebody adds
        // after the sweep is already correct. The
        // fallback list is what every label on earth
        // consults for a character its own font lacks,
        // which is the only mechanism in TextMeshPro that
        // reaches a label nothing swept.
        //
        // Nothing is marked dirty and nothing is saved.
        // ==========================================
        private static void PatchTmpSettings(DirectGlobalFontConfig config, TMP_FontAsset asset)
        {
            TMP_Settings settings = TmpSettings();
            if (settings == null) { return; }

            if (!s_settingsPatched)
            {
                s_previousDefault = TMP_Settings.defaultFontAsset;
                s_settingsPatched = true;
            }

            if (config.ReplaceLabelFonts)
            {
                SetPrivateField(settings, "m_defaultFontAsset", asset);
            }

            if (!config.GlobalFallback) { return; }

            List<TMP_FontAsset> fallbacks = TMP_Settings.fallbackFontAssets;
            if (fallbacks == null)
            {
                fallbacks = new List<TMP_FontAsset>();
                if (!SetPrivateField(settings, "m_fallbackFontAssets", fallbacks)) { return; }
            }

            fallbacks.RemoveAll(f => f == null);
            if (!fallbacks.Contains(asset)) { fallbacks.Insert(0, asset); }
        }

        private static void UnpatchTmpSettings()
        {
            if (!s_settingsPatched) { return; }
            s_settingsPatched = false;

            TMP_Settings settings = TmpSettings();
            if (settings == null) { return; }

            SetPrivateField(settings, "m_defaultFontAsset", s_previousDefault);
            s_previousDefault = null;

            List<TMP_FontAsset> fallbacks = TMP_Settings.fallbackFontAssets;
            if (fallbacks != null && Asset != null) { fallbacks.Remove(Asset); }
        }

        private static TMP_Settings TmpSettings()
        {
            try { return TMP_Settings.instance; }
            catch (Exception) { return null; }
        }

        private static bool SetPrivateField(object target, string fieldName, object value)
        {
            try
            {
                FieldInfo field = target.GetType().GetField(
                    fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field == null) { return false; }

                field.SetValue(target, value);
                return true;
            }
            catch (Exception)
            {
                // A TextMeshPro version that renamed the field. The sweep still
                // covers every label that exists; only labels created later
                // miss out, and that is a smaller loss than an exception here.
                return false;
            }
        }

        // ==========================================
        // Scene loading
        //
        // A scene loaded at runtime brings labels the
        // last sweep never saw. Additive loads too, which
        // is why this is sceneLoaded rather than anything
        // to do with the active scene.
        // ==========================================
        private static void HookSceneLoaded()
        {
            if (s_sceneHooked) { return; }
            s_sceneHooked = true;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Sweep();
            DirectTMPDriver.Ensure();
        }

        // ==========================================
        // Tick
        //
        // Driven by DirectTMPDriver in Play mode and by
        // the editor bootstrap out of it. Both are the
        // same two jobs: keep the shaped labels up to
        // date, and notice labels that have appeared
        // since - a prefab instantiated at runtime, a
        // GameObject somebody just created in the
        // Hierarchy.
        // ==========================================
        private static int s_sweepCountdown;

        /// <summary>
        /// One frame of upkeep. Ticks the shaped labels every time, and
        /// re-sweeps for newly created labels once every
        /// <see cref="SweepInterval"/> ticks.
        /// </summary>
        public static void Tick()
        {
            if (!IsActive) { return; }

            DirectTextTicker.TickAll();

            DirectGlobalFontConfig config = Config;
            if (config == null || !config.WatchForNewLabels) { return; }

            if (--s_sweepCountdown > 0) { return; }
            s_sweepCountdown = SweepInterval;
            Sweep();
        }

        // One second at 60fps. A label created this frame is already drawn
        // from TMP_Settings.defaultFontAsset - this font - and already free of
        // tofu through the global fallback, so this sweep is catching prefabs
        // that carry a font asset of their own, not fixing anything visible.
        // Every frame would be a FindObjectsOfType per frame, which is the
        // kind of cost that gets a package uninstalled.
        private const int SweepInterval = 60;
    }
}
