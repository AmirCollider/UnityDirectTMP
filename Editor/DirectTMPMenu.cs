// ==========================================
// DirectTMPMenu
// The "Unity DirectTMP" top-level menu (drawn to
// match README.md exactly) plus the matching
// right-click actions in the Project window. Each
// item is a thin call into the converter, the cache,
// the settings page, or a window - the logic lives
// there, the wiring lives here.
// ==========================================
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    internal static class DirectTMPMenu
    {
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
                Debug.LogWarning($"[{DirectTMPConstants.ToolName}] Could not delete the spool folder: {e.Message}");
            }

            Debug.Log($"[{DirectTMPConstants.ToolName}] Cleared {released} cached atlas(es) and {filesDeleted} spooled font file(s).");
        }

        // ==========================================
        // Settings…
        // ==========================================
        [MenuItem(DirectTMPEditorConstants.MenuSettings, false, DirectTMPEditorConstants.PrioritySettings)]
        private static void OpenSettings() => SettingsService.OpenProjectSettings(DirectTMPEditorConstants.SettingsPath);

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
