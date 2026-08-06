// ==========================================
// DirectTMPMenu
// The "Unity DirectTMP" top-level menu (drawn to
// match README.md exactly), its mirror under
// Window ▸ Unity DirectTMP, and the matching
// right-click actions in the Project window.
//
// Each item is a thin call into a window, the
// converter, the cache or the settings page. The logic
// lives there; the wiring lives here, so the menu tree
// can be read in one screen and compared against the
// README without reading any behaviour.
// ==========================================
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    internal static class DirectTMPMenu
    {
        // ==========================================
        // Global Font…
        //
        // First in the menu because it is the shortest
        // path from installing the package to the package
        // having done its job: pick a .ttf, and every
        // TextMeshPro label in the project is drawn from
        // it, with nothing added to any GameObject and
        // nothing written into any scene.
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.MenuGlobalFont, false, DirectTMPEditorConstants.PriorityGlobalFont)]
        [MenuItem(DirectTMPEditorConstants.WindowGlobalFont, false, 2199)]
        private static void OpenGlobalFont() => DirectGlobalFontWindow.ShowWindow();

        // ==========================================
        // Font Catalog…
        // The answer to the question people install this
        // package with: "which of my fonts can set this
        // language?"
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.MenuCatalog, false, DirectTMPEditorConstants.PriorityCatalog)]
        [MenuItem(DirectTMPEditorConstants.WindowCatalog, false, 2200)]
        private static void OpenCatalog() => DirectFontCatalogWindow.ShowWindow();

        // ==========================================
        // Editor Font ▸ Choose a Font… / Revert
        // The one feature that changes Unity itself.
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.MenuEditorFont, false, DirectTMPEditorConstants.PriorityEditorFont)]
        [MenuItem(DirectTMPEditorConstants.WindowEditorFont, false, 2201)]
        private static void OpenEditorFont() => DirectEditorFontWindow.ShowWindow();

        [MenuItem(DirectTMPEditorConstants.MenuEditorFontRevert, false, DirectTMPEditorConstants.PriorityEditorFontRevert)]
        private static void RevertEditorFont() => DirectEditorFontSettings.Clear();

        // Greyed out when there is nothing to revert, so the menu itself
        // reports whether the override is on - which is the question somebody
        // opens this submenu to answer.
        [MenuItem(DirectTMPEditorConstants.MenuEditorFontRevert, true)]
        private static bool ValidateRevertEditorFont() => DirectEditorFont.IsActive;

        // ==========================================
        // Convert ▸ Selected Objects
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.MenuConvertSelected, false, DirectTMPEditorConstants.PriorityConvertSelected)]
        private static void ConvertSelected() => DirectTMPConverter.ConvertSelected();

        [MenuItem(DirectTMPEditorConstants.MenuConvertSelected, true)]
        private static bool ValidateConvertSelected() => Selection.gameObjects.Length > 0;

        // ==========================================
        // Convert ▸ Current Scene
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.MenuConvertScene, false, DirectTMPEditorConstants.PriorityConvertScene)]
        private static void ConvertScene() => DirectTMPConverter.ConvertCurrentScene();

        // ==========================================
        // Convert ▸ Whole Project
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.MenuConvertProject, false, DirectTMPEditorConstants.PriorityConvertProject)]
        private static void ConvertProject() => DirectTMPConverter.ConvertWholeProject();

        // ==========================================
        // Fallback Chain…
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.MenuFallbackChain, false, DirectTMPEditorConstants.PriorityFallbackChain)]
        private static void OpenFallbackChain() => DirectTMPFallbackWindow.ShowWindow();

        // ==========================================
        // Font Cache ▸ Show Cache Folder
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.MenuShowCacheFolder, false, DirectTMPEditorConstants.PriorityShowCacheFolder)]
        private static void ShowCacheFolder()
        {
            string dir = DirectTMPSettings.ResolveRuntimeCacheFolderAbsolute();
            Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
        }

        // ==========================================
        // Font Cache ▸ Clear Cache
        // Drops the in-memory atlases and deletes the
        // on-disk spool files that raw-byte fonts leave
        // behind.
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.MenuClearCache, false, DirectTMPEditorConstants.PriorityClearCache)]
        private static void ClearCache()
        {
            int released = DirectTMP.ClearCache();

            int filesDeleted = 0;
            try
            {
                string dir = DirectTMPSettings.ResolveRuntimeCacheFolderAbsolute();
                if (Directory.Exists(dir))
                {
                    filesDeleted = Directory.GetFiles(dir).Length;
                    Directory.Delete(dir, true);
                }
            }
            catch (System.Exception e)
            {
                DirectTMPLog.Warn($"Could not delete the spool folder: {e.Message}");
            }

            DirectTMPLog.Info($"Cleared {released} cached atlas(es) and {filesDeleted} spooled font file(s).");

            // The project-wide font was one of those atlases, and every label
            // in the scene is pointing at it. Rebuild it now rather than
            // leaving a project that looks like clearing a cache broke it.
            DirectGlobalFontBootstrap.ApplyNow();
        }

        // ==========================================
        // Language
        //
        // Checkmarks rather than a submenu of buttons, so
        // the menu shows which language is active without
        // opening a settings page. Changing it repaints
        // every open window, because a language switch
        // that only takes effect on the next window is a
        // language switch people think did not work.
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.MenuLanguageEnglish, false, DirectTMPEditorConstants.PriorityLanguage)]
        private static void LanguageEnglish() => SetLanguage(DirectTMPText.English);

        [MenuItem(DirectTMPEditorConstants.MenuLanguageEnglish, true)]
        private static bool ValidateLanguageEnglish() => Check(DirectTMPEditorConstants.MenuLanguageEnglish, DirectTMPText.English);

        [MenuItem(DirectTMPEditorConstants.MenuLanguageJapanese, false, DirectTMPEditorConstants.PriorityLanguage + 1)]
        private static void LanguageJapanese() => SetLanguage(DirectTMPText.Japanese);

        [MenuItem(DirectTMPEditorConstants.MenuLanguageJapanese, true)]
        private static bool ValidateLanguageJapanese() => Check(DirectTMPEditorConstants.MenuLanguageJapanese, DirectTMPText.Japanese);

        [MenuItem(DirectTMPEditorConstants.MenuLanguagePersian, false, DirectTMPEditorConstants.PriorityLanguage + 2)]
        private static void LanguagePersian() => SetLanguage(DirectTMPText.Persian);

        [MenuItem(DirectTMPEditorConstants.MenuLanguagePersian, true)]
        private static bool ValidateLanguagePersian() => Check(DirectTMPEditorConstants.MenuLanguagePersian, DirectTMPText.Persian);

        private static void SetLanguage(string code)
        {
            DirectTMPText.Current = code;
            DirectTMPBrand.InvalidateStyles();
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window != null) { window.Repaint(); }
            }
        }

        private static bool Check(string menuPath, string code)
        {
            Menu.SetChecked(menuPath, DirectTMPText.Current == code);
            return true;
        }

        // ==========================================
        // Settings…
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.MenuSettings, false, DirectTMPEditorConstants.PrioritySettings)]
        private static void OpenSettings() => SettingsService.OpenProjectSettings(DirectTMPEditorConstants.SettingsPath);

        // ==========================================
        // Health Check…
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.MenuHealthCheck, false, DirectTMPEditorConstants.PriorityHealthCheck)]
        [MenuItem(DirectTMPEditorConstants.WindowHealthCheck, false, 2202)]
        private static void OpenHealthCheck() => DirectTMPHealthCheckWindow.ShowWindow();

        // ==========================================
        // Welcome
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.MenuWelcome, false, DirectTMPEditorConstants.PriorityWelcome)]
        private static void OpenWelcome() => DirectTMPWelcomeWindow.ShowWindow();

        // ==========================================
        // About Unity DirectTMP
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.MenuAbout, false, DirectTMPEditorConstants.PriorityAbout)]
        private static void About() => DirectTMPAboutWindow.ShowWindow();

        // ==========================================
        // Project window ▸ Convert TMP In Folder
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.ContextConvertFolder, false, 20)]
        private static void ContextConvertFolder()
        {
            string folder = SelectedFolder();
            if (folder != null) { DirectTMPConverter.ConvertPrefabsInFolder(folder); }
        }

        [MenuItem(DirectTMPEditorConstants.ContextConvertFolder, true)]
        private static bool ValidateContextConvertFolder() => SelectedFolder() != null;

        // ==========================================
        // Project window ▸ New Fallback Chain
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.ContextNewFallbackChain, false, 21)]
        private static void ContextNewFallbackChain()
        {
            string folder = SelectedFolder() ?? "Assets";
            var chain = ScriptableObject.CreateInstance<DirectFontFallbackChain>();
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/DirectFontFallbackChain.asset");
            AssetDatabase.CreateAsset(chain, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = chain;
            EditorGUIUtility.PingObject(chain);
        }

        // ==========================================
        // Project window ▸ Use This Font Everywhere
        //
        // Right-clicking the .ttf and choosing this is
        // the entire package in one action: the settings
        // asset is created if the project has none, the
        // font's import settings are repaired if they
        // would have stopped TextMeshPro reading it, and
        // every label in the project is drawn from it
        // from the next repaint onward.
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.ContextUseEverywhere, false, 29)]
        private static void ContextUseEverywhere()
        {
            var font = Selection.activeObject as Font;
            if (font == null) { return; }

            DirectGlobalFontConfig config = DirectGlobalFontBootstrap.EnsureConfigAsset();
            if (config == null) { return; }

            Undo.RecordObject(config, "Unity DirectTMP Global Font");
            config.FontFile = font;
            config.Active = true;

            DirectGlobalFontBootstrap.RepairImport(font);
            DirectGlobalFontBootstrap.SaveAndApply(config);
            DirectGlobalFontWindow.ShowWindow();
        }

        [MenuItem(DirectTMPEditorConstants.ContextUseEverywhere, true)]
        private static bool ValidateContextUseEverywhere() => Selection.activeObject is Font;

        // ==========================================
        // Project window ▸ Use This Font For The Editor
        //
        // Right-clicking the .ttf and choosing this is
        // the shortest path there is from "the Hierarchy
        // is full of empty boxes" to "it isn't any more".
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.ContextUseForEditor, false, 30)]
        private static void ContextUseForEditor()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            var plan = new DirectEditorFontPlan(true, DirectEditorFontSource.ProjectFont, path, null, 0);

            DirectEditorFontProblem problem = DirectEditorFont.Validate(plan);
            if (DirectEditorFontRules.IsFatal(problem))
            {
                EditorUtility.DisplayDialog(
                    DirectTMPConstants.ToolName,
                    DirectEditorFontRules.Describe(problem),
                    "OK");
                return;
            }

            DirectEditorFontSettings.Plan = plan;
            DirectEditorFont.Apply(plan);
        }

        [MenuItem(DirectTMPEditorConstants.ContextUseForEditor, true)]
        private static bool ValidateContextUseForEditor() => Selection.activeObject is Font;

        // ==========================================
        // Project window ▸ Inspect This Font
        // Opens the catalog, where the font that was
        // right-clicked is one row among its siblings -
        // which is the comparison somebody is making when
        // they ask what is in it.
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.ContextInspectFont, false, 31)]
        private static void ContextInspectFont() => DirectFontCatalogWindow.ShowWindow();

        [MenuItem(DirectTMPEditorConstants.ContextInspectFont, true)]
        private static bool ValidateContextInspectFont() => Selection.activeObject is Font;

        // Returns the currently selected Assets folder (or the folder that
        // contains the selected asset), or null if the selection isn't inside
        // this project's Assets.
        private static string SelectedFolder()
        {
            Object obj = Selection.activeObject;
            if (obj == null) { return null; }

            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) { return null; }
            if (AssetDatabase.IsValidFolder(path)) { return path; }

            string dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
            return (!string.IsNullOrEmpty(dir) && dir.StartsWith("Assets")) ? dir : null;
        }
    }
}
