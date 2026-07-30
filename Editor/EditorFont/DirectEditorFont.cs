// ==========================================
// DirectEditorFont
// The part of Unity DirectTMP that changes Unity
// itself.
//
// Everything else in this package is about the text
// your GAME draws. This is about the text the EDITOR
// draws: the menu bar, the Hierarchy, the Inspector,
// the Project window, the Console. Until now the tool
// could give a Japanese player a Japanese UI and still
// leave the developer building it looking at a
// Hierarchy full of empty boxes, because Unity's own
// chrome is drawn with Unity's own font and nothing in
// a TextMeshPro package touches it.
//
// It matters more than it sounds. Name a GameObject
// "敵スポーナー" or a folder "فونت‌ها" and Unity will
// happily store it and then show you □□□□□ forever.
// The asset exists, the name is correct, and the
// Project window is unusable.
//
// How it works
// ------------
// IMGUI resolves a glyph through GUIStyle.font, and
// falls back to GUISkin.font when the style has none.
// So restyling the Editor is: walk every GUISkin
// currently loaded, walk every GUIStyle inside it,
// walk the static styles hanging off EditorStyles, and
// point them all at one Font. Unity rebuilds those
// objects on domain reload and when the light/dark
// skin flips, which is why this runs from
// [InitializeOnLoad] and then keeps a slow heartbeat
// rather than applying once and hoping.
//
// What it does NOT do
// -------------------
// It writes nothing to your Unity installation, edits
// no built-in asset on disk, and survives no restart
// by design. Everything it changes lives in memory for
// the session. Quitting Unity is always a complete,
// guaranteed undo - which is stated in the window,
// because "can I get my Editor back?" is the first
// thing anyone sensibly asks before clicking Apply.
//
// The safety rule
// ---------------
// A font that cannot draw Latin letters would turn the
// Revert button into an empty box. DirectEditorFontRules
// refuses those before anything is touched. That check
// is not a nicety; it is the difference between a
// feature and a trap.
// ==========================================
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    /// <summary>
    /// Applies (and takes back) a font override across the Unity Editor's own
    /// IMGUI chrome.
    /// </summary>
    [InitializeOnLoad]
    internal static class DirectEditorFont
    {
        // How often the heartbeat re-checks that the override is still in
        // place. Windows created after the last pass carry Unity's font until
        // the next one, so this is a visible delay - but a per-frame sweep
        // over every loaded GUISkin is real work on every idle frame, forever,
        // to catch an event that happens a handful of times an hour.
        private const double HeartbeatSeconds = 1.0;

        private static double s_nextHeartbeat;

        // What each style looked like before we touched it. Restored on
        // revert. Rebuilt from scratch after every domain reload, because the
        // managed GUIStyle objects are rebuilt too.
        private static readonly Dictionary<GUIStyle, StyleSnapshot> s_snapshots = new Dictionary<GUIStyle, StyleSnapshot>();
        private static readonly Dictionary<GUISkin, Font> s_skinSnapshots = new Dictionary<GUISkin, Font>();

        // The font currently pushed onto the Editor, or null when the override
        // is off. Held so the heartbeat can tell "already applied" from
        // "needs applying" without rebuilding the plan.
        private static Font s_applied;
        private static int s_appliedDelta;

        // The dynamic Font built from an OS family name. Owned here and
        // destroyed on revert; creating one per heartbeat would leak a font
        // object a second.
        private static Font s_systemFont;
        private static string s_systemFamily;

        // Coverage of the chosen font, parsed once per font rather than once
        // per validation. Reading a 20 MB CJK font on every heartbeat would be
        // a disk read a second for no new information.
        private static string s_coverageKey;
        private static DirectFontFileInfo s_coverageInfo;

        private struct StyleSnapshot
        {
            public Font Font;
            public int FontSize;
        }

        static DirectEditorFont()
        {
            // Deferred: at static-constructor time EditorStyles is not built
            // yet and the AssetDatabase will not answer, so applying here
            // would silently do nothing on the very reload that matters most.
            EditorApplication.delayCall += () =>
            {
                ApplyFromSettings();
                EditorApplication.update -= Heartbeat;
                EditorApplication.update += Heartbeat;
            };
        }

        /// <summary>Is an override active right now?</summary>
        public static bool IsActive => s_applied != null;

        /// <summary>The font currently forced onto the Editor, or null.</summary>
        public static Font AppliedFont => s_applied;

        // ==========================================
        // Heartbeat
        //
        // Re-applies the override to anything Unity has
        // rebuilt since the last pass: a window opened,
        // the skin flipped, a style was reset.
        // ==========================================
        private static void Heartbeat()
        {
            if (EditorApplication.timeSinceStartup < s_nextHeartbeat) { return; }
            s_nextHeartbeat = EditorApplication.timeSinceStartup + HeartbeatSeconds;

            DirectEditorFontPlan plan = DirectEditorFontSettings.Plan;

            if (!plan.IsActive)
            {
                if (s_applied != null) { Revert(); }
                return;
            }

            // Cheap idempotent pass. Styles already carrying our font are
            // skipped inside Push, so the steady-state cost is a dictionary
            // lookup per style rather than an assignment.
            ApplyFromSettings(silent: true);
        }

        // ==========================================
        // ApplyFromSettings
        // ==========================================
        /// <summary>
        /// Reads the stored plan and carries it out. Returns the problem that
        /// stopped it, or <see cref="DirectEditorFontProblem.None"/>.
        /// </summary>
        public static DirectEditorFontProblem ApplyFromSettings(bool silent = false)
        {
            return Apply(DirectEditorFontSettings.Plan, silent);
        }

        /// <summary>
        /// Carries out a plan without storing it - what the window's live
        /// preview uses. A fatal problem leaves the Editor exactly as it was.
        /// </summary>
        public static DirectEditorFontProblem Apply(DirectEditorFontPlan plan, bool silent = false)
        {
            if (!plan.IsActive)
            {
                Revert();
                return DirectEditorFontProblem.None;
            }

            DirectEditorFontProblem problem = Validate(plan);
            if (DirectEditorFontRules.IsFatal(problem))
            {
                if (!silent)
                {
                    DirectTMPLog.Warn("Editor font not applied. " + DirectEditorFontRules.Describe(problem));
                }
                return problem;
            }

            Font font = Resolve(plan);
            if (font == null)
            {
                if (!silent) { DirectTMPLog.Warn("Editor font not applied: the font could not be loaded."); }
                return DirectEditorFontProblem.MissingAsset;
            }

            PushEverywhere(font, plan.SizeDelta);
            s_applied = font;
            s_appliedDelta = plan.SizeDelta;
            RepaintEverything();

            return problem;   // may still be the advisory NotDynamic
        }

        // ==========================================
        // Revert
        //
        // Puts back exactly what was recorded when each
        // style was first touched. Styles Unity has
        // rebuilt since then were never touched by us and
        // already carry Unity's font, so they need
        // nothing.
        // ==========================================
        /// <summary>Restores the Editor's own font everywhere this tool changed it.</summary>
        public static void Revert()
        {
            foreach (KeyValuePair<GUIStyle, StyleSnapshot> entry in s_snapshots)
            {
                if (entry.Key == null) { continue; }
                try
                {
                    entry.Key.font = entry.Value.Font;
                    entry.Key.fontSize = entry.Value.FontSize;
                }
                catch (Exception)
                {
                    // A style Unity has since disposed. Nothing to restore and
                    // nothing to report - the object is already gone.
                }
            }
            s_snapshots.Clear();

            foreach (KeyValuePair<GUISkin, Font> entry in s_skinSnapshots)
            {
                if (entry.Key == null) { continue; }
                try { entry.Key.font = entry.Value; }
                catch (Exception) { }
            }
            s_skinSnapshots.Clear();

            DestroySystemFont();

            s_applied = null;
            s_appliedDelta = 0;
            RepaintEverything();
        }

        // ==========================================
        // Validation
        // ==========================================
        /// <summary>
        /// Checks a plan against this project and this machine. Public because
        /// the window shows the verdict live, before anybody clicks anything.
        /// </summary>
        public static DirectEditorFontProblem Validate(DirectEditorFontPlan plan)
        {
            return DirectEditorFontRules.Validate(
                plan,
                assetExists: path => AssetDatabase.LoadAssetAtPath<Font>(path) != null,
                assetIsDynamic: IsDynamicImport,
                systemFamilyExists: SystemFamilyExists,
                hasCodepoint: BuildCoverageProbe(plan));
        }

        /// <summary>
        /// The parsed font file behind a plan, for the window's coverage
        /// badges. Null when the plan points at a system font (there is no
        /// file path to read) or at nothing.
        /// </summary>
        public static DirectFontFileInfo CoverageFor(DirectEditorFontPlan plan)
        {
            BuildCoverageProbe(plan);
            return s_coverageInfo;
        }

        // Builds the "does this font have this character?" predicate for a
        // plan, caching the expensive half.
        //
        // Two very different mechanisms, because the two sources expose
        // different things: a project font is a file we can read the cmap out
        // of, while an OS family is only reachable through Unity's own
        // dynamic-font wrapper, which answers HasCharacter and nothing else.
        private static Func<int, bool> BuildCoverageProbe(DirectEditorFontPlan plan)
        {
            if (plan.Source == DirectEditorFontSource.SystemFont)
            {
                s_coverageInfo = null;
                s_coverageKey = null;

                Font probe = ResolveSystemFont(plan.SystemFamily);
                if (probe == null) { return null; }

                return codepoint =>
                {
                    if (codepoint > 0xFFFF) { return false; }
                    try { return probe.HasCharacter((char)codepoint); }
                    catch (Exception) { return false; }
                };
            }

            if (plan.Source != DirectEditorFontSource.ProjectFont || string.IsNullOrEmpty(plan.AssetPath))
            {
                s_coverageInfo = null;
                s_coverageKey = null;
                return null;
            }

            if (s_coverageKey != plan.AssetPath || s_coverageInfo == null)
            {
                string absolute = AssetPathToAbsolute(plan.AssetPath);
                s_coverageInfo = DirectFontFile.Read(absolute);
                s_coverageKey = plan.AssetPath;
            }

            DirectFontFileInfo info = s_coverageInfo;

            // An unreadable file must not be reported as "covers nothing",
            // which would trip the Latin guard and refuse a font that is
            // probably fine. Returning null means "no opinion", and the rules
            // skip the coverage check.
            if (info == null || !info.IsValid) { return null; }

            return info.HasCodepoint;
        }

        // ==========================================
        // Font resolution
        // ==========================================
        private static Font Resolve(DirectEditorFontPlan plan)
        {
            switch (plan.Source)
            {
                case DirectEditorFontSource.ProjectFont:
                    return DirectEditorFontSettings.ResolveAsset(plan);

                case DirectEditorFontSource.SystemFont:
                    return ResolveSystemFont(plan.SystemFamily);

                default:
                    return null;
            }
        }

        // One dynamic font per family, kept alive for as long as it is the
        // chosen one. The size passed here is only the initial rasterization
        // hint; a dynamic font grows to whatever the styles ask for.
        private static Font ResolveSystemFont(string family)
        {
            if (string.IsNullOrEmpty(family)) { return null; }

            if (s_systemFont != null && s_systemFamily == family) { return s_systemFont; }

            DestroySystemFont();

            try
            {
                s_systemFont = Font.CreateDynamicFontFromOSFont(family, DirectEditorFontRules.BaseFontSize);
                if (s_systemFont != null)
                {
                    s_systemFont.hideFlags = HideFlags.HideAndDontSave;
                    s_systemFamily = family;
                }
            }
            catch (Exception e)
            {
                DirectTMPLog.Warn($"Could not open the installed font '{family}': {e.Message}");
                s_systemFont = null;
                s_systemFamily = null;
            }

            return s_systemFont;
        }

        private static void DestroySystemFont()
        {
            if (s_systemFont != null)
            {
                UnityEngine.Object.DestroyImmediate(s_systemFont);
            }
            s_systemFont = null;
            s_systemFamily = null;
        }

        /// <summary>Every font family installed on this machine, sorted.</summary>
        public static string[] InstalledFamilies()
        {
            try
            {
                string[] names = Font.GetOSInstalledFontNames();
                if (names == null) { return new string[0]; }

                var unique = new List<string>(names.Length);
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string name in names)
                {
                    if (string.IsNullOrEmpty(name)) { continue; }
                    if (seen.Add(name)) { unique.Add(name); }
                }
                unique.Sort(StringComparer.OrdinalIgnoreCase);
                return unique.ToArray();
            }
            catch (Exception)
            {
                return new string[0];
            }
        }

        private static bool SystemFamilyExists(string family)
        {
            if (string.IsNullOrEmpty(family)) { return false; }
            foreach (string installed in InstalledFamilies())
            {
                if (string.Equals(installed, family, StringComparison.OrdinalIgnoreCase)) { return true; }
            }
            return false;
        }

        /// <summary>
        /// True when a Font asset is imported as a dynamic font - the mode in
        /// which it can rasterize any character the file contains. The
        /// alternative is a baked character set, which is the very thing this
        /// whole package exists to get people away from.
        /// </summary>
        public static bool IsDynamicImport(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) { return false; }

            var importer = AssetImporter.GetAtPath(assetPath) as TrueTypeFontImporter;
            if (importer == null)
            {
                // Not a TrueType import at all (a font inside a package, a
                // custom font asset). Nothing to object to.
                return true;
            }
            return importer.fontTextureCase == FontTextureCase.Dynamic;
        }

        /// <summary>
        /// Switches a Font asset's import mode to Dynamic and reimports it.
        /// Offered as a one-click fix next to the NotDynamic warning, because
        /// the alternative instruction is six clicks into an Inspector most
        /// people have never opened.
        /// </summary>
        public static bool MakeImportDynamic(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TrueTypeFontImporter;
            if (importer == null) { return false; }

            importer.fontTextureCase = FontTextureCase.Dynamic;
            importer.SaveAndReimport();
            return true;
        }

        // ==========================================
        // The actual patching
        // ==========================================
        private static void PushEverywhere(Font font, int sizeDelta)
        {
            // Touching the built-in skins forces Unity to load them if it has
            // not yet. Without this, the Scene and Game view overlays keep the
            // default font until the first time each one is drawn.
            TouchBuiltinSkins();

            GUISkin[] skins = Resources.FindObjectsOfTypeAll<GUISkin>();
            foreach (GUISkin skin in skins)
            {
                if (skin == null) { continue; }
                PushSkin(skin, font, sizeDelta);
            }

            PushEditorStyles(font, sizeDelta);
        }

        private static void TouchBuiltinSkins()
        {
            try
            {
                EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
                EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
                EditorGUIUtility.GetBuiltinSkin(EditorSkin.Game);
            }
            catch (Exception)
            {
                // A Unity version that has renamed or removed one of the
                // skins. The sweep below still finds whatever is loaded.
            }
        }

        private static void PushSkin(GUISkin skin, Font font, int sizeDelta)
        {
            if (!s_skinSnapshots.ContainsKey(skin))
            {
                s_skinSnapshots[skin] = skin.font;
            }
            if (skin.font != font) { skin.font = font; }

            // GUISkin exposes an enumerator over every style it holds - the
            // named ones and the custom ones together. It is the only way to
            // reach all of them without naming twenty properties and missing
            // the twenty-first when Unity adds one.
            try
            {
                foreach (GUIStyle style in skin)
                {
                    PushStyle(style, font, sizeDelta);
                }
            }
            catch (Exception)
            {
                // If the enumerator is unavailable on this Unity version, fall
                // back to the styles we can name. Partial coverage of the
                // Editor's chrome beats none.
                PushNamedStyles(skin, font, sizeDelta);
            }

            if (skin.customStyles != null)
            {
                foreach (GUIStyle style in skin.customStyles)
                {
                    PushStyle(style, font, sizeDelta);
                }
            }
        }

        private static void PushNamedStyles(GUISkin skin, Font font, int sizeDelta)
        {
            GUIStyle[] named =
            {
                skin.box, skin.button, skin.toggle, skin.label, skin.textField, skin.textArea,
                skin.window, skin.horizontalSlider, skin.horizontalSliderThumb,
                skin.verticalSlider, skin.verticalSliderThumb,
                skin.horizontalScrollbar, skin.horizontalScrollbarThumb,
                skin.verticalScrollbar, skin.verticalScrollbarThumb,
                skin.scrollView
            };

            foreach (GUIStyle style in named)
            {
                PushStyle(style, font, sizeDelta);
            }
        }

        private static void PushStyle(GUIStyle style, Font font, int sizeDelta)
        {
            if (style == null) { return; }

            // Already ours. This is the common case on every heartbeat after
            // the first, and skipping it here is what keeps the steady state
            // free.
            if (style.font == font && (sizeDelta == 0 || s_snapshots.ContainsKey(style))) { return; }

            if (!s_snapshots.ContainsKey(style))
            {
                s_snapshots[style] = new StyleSnapshot { Font = style.font, FontSize = style.fontSize };
            }

            try
            {
                style.font = font;
                if (sizeDelta != 0)
                {
                    StyleSnapshot original = s_snapshots[style];
                    style.fontSize = DirectEditorFontRules.ResolveFontSize(original.FontSize, sizeDelta);
                }
            }
            catch (Exception)
            {
                // Some internal styles are read-only proxies on some versions.
                // One refusing is not a reason to stop restyling the rest.
            }
        }

        // ==========================================
        // EditorStyles
        //
        // The Inspector, the Hierarchy and the toolbar
        // draw from EditorStyles' own static GUIStyle
        // instances, which are NOT inside any GUISkin the
        // sweep above can see. There is no public API for
        // them, so this reaches the internal instance the
        // class keeps and walks its GUIStyle fields.
        //
        // Reflection into another team's internals is a
        // thing to justify, so: it is read-and-write on
        // fields whose type we check, every step is
        // wrapped, and a Unity version that renames the
        // field costs the Inspector its restyling and
        // nothing else. The alternative is a feature that
        // changes the menu bar and leaves the Inspector
        // in tofu, which is worse than not shipping it.
        // ==========================================
        private static void PushEditorStyles(Font font, int sizeDelta)
        {
            try
            {
                // Force EditorStyles to build itself for the current skin.
                GUIStyle probe = EditorStyles.label;
                if (probe == null) { return; }

                Type type = typeof(EditorStyles);
                FieldInfo currentField = type.GetField("s_Current", BindingFlags.NonPublic | BindingFlags.Static);
                object current = currentField?.GetValue(null);
                if (current == null)
                {
                    // Nothing internal to reach. The public statics still
                    // resolve through GUI.skin, which the sweep has already
                    // restyled.
                    return;
                }

                foreach (FieldInfo field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    if (field.FieldType != typeof(GUIStyle)) { continue; }
                    PushStyle(field.GetValue(current) as GUIStyle, font, sizeDelta);
                }
            }
            catch (Exception)
            {
                // Reflection failed on this Unity version. Everything reached
                // through GUISkin is still restyled.
            }
        }

        // ==========================================
        // Repaint
        // ==========================================
        private static void RepaintEverything()
        {
            try
            {
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
            catch (Exception)
            {
                // Fall back to repainting whatever windows we can reach.
                foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                {
                    if (window != null) { window.Repaint(); }
                }
            }
        }

        // ==========================================
        // Paths
        // ==========================================
        internal static string AssetPathToAbsolute(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) { return string.Empty; }

            string dataPath = Application.dataPath.Replace('\\', '/');
            string normalized = assetPath.Replace('\\', '/');

            if (normalized.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return dataPath + normalized.Substring("Assets".Length);
            }

            // A font living inside a package. Project-relative from the folder
            // above Assets.
            string projectRoot = dataPath.Substring(0, dataPath.Length - "/Assets".Length);
            return projectRoot + "/" + normalized;
        }

        /// <summary>How many styles the override is currently holding down.</summary>
        public static int PatchedStyleCount => s_snapshots.Count;

        /// <summary>The size delta currently in force.</summary>
        public static int AppliedSizeDelta => s_appliedDelta;
    }
}
