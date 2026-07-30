// ==========================================
// DirectTMPConverter
// Batch conversion: take TextMeshPro labels that use
// a baked SDF font asset and give each one a
// DirectFont pointing at that asset's *source* font
// file, so from now on their glyphs come straight
// from the .ttf. Works on the current selection, the
// open scene, or - in one pass - every Scene and
// Prefab in the project.
//
// The heavy "Whole Project" path is deliberately
// cautious: it asks first, shows progress, only saves
// assets it actually changed, and always restores the
// scene you started from.
// ==========================================
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityDirectTMP.Editor
{
    internal static class DirectTMPConverter
    {
        // ==========================================
        // ConvertSelected
        // Every TMP label on the selected objects and
        // their children.
        // ==========================================
        public static int ConvertSelected()
        {
            var labels = new List<TMP_Text>();
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go != null) { labels.AddRange(go.GetComponentsInChildren<TMP_Text>(true)); }
            }

            int converted = ConvertLabels(labels, recordUndo: true, apply: true, undoName: "Convert Selection to DirectTMP");
            Report(converted, "the selection");
            return converted;
        }

        // ==========================================
        // ConvertCurrentScene
        // ==========================================
        public static int ConvertCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            var labels = new List<TMP_Text>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                labels.AddRange(root.GetComponentsInChildren<TMP_Text>(true));
            }

            int converted = ConvertLabels(labels, recordUndo: true, apply: true, undoName: "Convert Scene to DirectTMP");
            if (converted > 0) { EditorSceneManager.MarkSceneDirty(scene); }
            Report(converted, "the open scene");
            return converted;
        }

        // ==========================================
        // ConvertWholeProject
        // Every Prefab, then every Scene. Guarded by a
        // confirmation dialog because it rewrites assets
        // on disk.
        // ==========================================
        public static void ConvertWholeProject()
        {
            bool proceed = EditorUtility.DisplayDialog(
                DirectTMPConstants.ToolName,
                DirectTMPText.Dialog(
                    "Convert EVERY TextMeshPro label in this project to DirectTMP?\n\n" +
                    "This adds a DirectFont to every label across all Prefabs and Scenes and saves the changes to disk. " +
                    "Please make sure your work is committed or backed up first.\n\n" +
                    "─────\n" +
                    "プロジェクト内のすべての TextMeshPro ラベルを変換します。先にバックアップを推奨します。\n\n" +
                    "─────\n" +
                    "همه‌ی لیبل‌های TextMeshPro پروژه رو تبدیل می‌کنه و روی دیسک ذخیره می‌کنه. بهتره اول بک‌آپ بگیری."),
                "Convert everything",
                "Cancel");
            if (!proceed) { return; }

            string originalScenePath = SceneManager.GetActiveScene().path;
            int total = 0;
            try
            {
                total += ConvertAllPrefabs();
                total += ConvertAllScenes();
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!string.IsNullOrEmpty(originalScenePath))
                {
                    EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                }
            }

            EditorUtility.DisplayDialog(
                DirectTMPConstants.ToolName,
                $"Done. Converted {total} TextMeshPro label(s) across the project.",
                "OK");
        }

        // ==========================================
        // ConvertPrefabsInFolder
        // Used by the Project-window context menu.
        // ==========================================
        public static int ConvertPrefabsInFolder(string folder)
        {
            int total = ConvertAllPrefabs(folder);
            AssetDatabase.SaveAssets();
            Report(total, $"'{folder}'");
            return total;
        }

        // ==========================================
        // ConvertAllPrefabs
        // ==========================================
        private static int ConvertAllPrefabs(string folder = "Assets")
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            int total = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (EditorUtility.DisplayCancelableProgressBar(
                        DirectTMPConstants.ToolName + " — Prefabs",
                        path,
                        guids.Length == 0 ? 1f : (float)i / guids.Length))
                {
                    break;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var labels = new List<TMP_Text>(root.GetComponentsInChildren<TMP_Text>(true));
                    // Don't apply inside prefab contents - a generated font asset
                    // is transient and must not be serialized into the prefab.
                    // DirectFont rebuilds it when the prefab is instantiated.
                    int c = ConvertLabels(labels, recordUndo: false, apply: false, undoName: null);
                    if (c > 0) { PrefabUtility.SaveAsPrefabAsset(root, path); }
                    total += c;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            return total;
        }

        // ==========================================
        // ConvertAllScenes
        // ==========================================
        private static int ConvertAllScenes()
        {
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            int total = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (EditorUtility.DisplayCancelableProgressBar(
                        DirectTMPConstants.ToolName + " — Scenes",
                        path,
                        guids.Length == 0 ? 1f : (float)i / guids.Length))
                {
                    break;
                }

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var labels = new List<TMP_Text>();
                foreach (GameObject rootGo in scene.GetRootGameObjects())
                {
                    labels.AddRange(rootGo.GetComponentsInChildren<TMP_Text>(true));
                }

                int c = ConvertLabels(labels, recordUndo: false, apply: false, undoName: null);
                if (c > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                total += c;
            }
            return total;
        }

        // ==========================================
        // ConvertLabels / ConvertLabel
        // ==========================================
        private static int ConvertLabels(List<TMP_Text> labels, bool recordUndo, bool apply, string undoName)
        {
            var seen = new HashSet<int>();
            int converted = 0;
            foreach (TMP_Text label in labels)
            {
                if (label == null || !seen.Add(label.GetInstanceID())) { continue; }
                if (ConvertLabel(label, recordUndo, apply, undoName)) { converted++; }
            }
            return converted;
        }

        private static bool ConvertLabel(TMP_Text label, bool recordUndo, bool apply, string undoName)
        {
            if (label == null) { return false; }
            if (label.GetComponent<DirectFont>() != null) { return false; } // already converted

            // The source .ttf/.otf behind the label's current font asset, if any.
            Font source = label.font != null ? label.font.sourceFontFile : null;

            DirectFont df;
            if (recordUndo)
            {
                if (!string.IsNullOrEmpty(undoName)) { Undo.SetCurrentGroupName(undoName); }
                df = Undo.AddComponent<DirectFont>(label.gameObject);
            }
            else
            {
                df = label.gameObject.AddComponent<DirectFont>();
            }

            df.EditorOverrideSettings = true;
            df.EditorSettings = DirectTMPSettings.CurrentSettings;
            if (source != null) { df.EditorFontFile = source; }

            EditorUtility.SetDirty(df);
            if (apply) { df.Refresh(); }
            return true;
        }

        // ==========================================
        // ApplyFontToSelection
        //
        // The Font Catalog's one-click "use this on what
        // I have selected". Different from ConvertSelected
        // in the one way that matters: the conversion
        // keeps whatever font each label already had and
        // simply changes where its glyphs come from, while
        // this REPLACES the font with a specific file
        // somebody just picked out of a list.
        //
        // It is one undo step for the whole selection, so
        // trying a font on forty labels and changing your
        // mind is one Ctrl+Z rather than forty.
        // ==========================================
        public static int ApplyFontToSelection(Font font)
        {
            if (font == null) { return 0; }

            var labels = new List<TMP_Text>();
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go != null) { labels.AddRange(go.GetComponentsInChildren<TMP_Text>(true)); }
            }

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Apply {font.name} with {DirectTMPConstants.ShortName}");

            var seen = new HashSet<int>();
            int applied = 0;

            foreach (TMP_Text label in labels)
            {
                if (label == null || !seen.Add(label.GetInstanceID())) { continue; }

                DirectFont directFont = label.GetComponent<DirectFont>();
                if (directFont == null)
                {
                    directFont = Undo.AddComponent<DirectFont>(label.gameObject);
                    directFont.EditorOverrideSettings = true;
                    directFont.EditorSettings = DirectTMPSettings.CurrentSettings;
                }
                else
                {
                    Undo.RecordObject(directFont, "Change Direct Font");
                }

                directFont.EditorFontFile = font;
                EditorUtility.SetDirty(directFont);
                directFont.Refresh();
                applied++;
            }

            Undo.CollapseUndoOperations(group);

            if (applied == 0)
            {
                EditorUtility.DisplayDialog(
                    DirectTMPConstants.ToolName,
                    DirectTMPText.Dialog(
                        "Select one or more GameObjects with a TextMeshPro component first.\n\n" +
                        "─────\n" +
                        "先に TextMeshPro を持つ GameObject を選択してください。\n\n" +
                        "─────\n" +
                        "اول یک یا چند GameObject که TextMeshPro دارن رو انتخاب کن."),
                    "OK");
            }
            else
            {
                DirectTMPLog.Info($"Applied '{font.name}' to {applied} TextMeshPro label(s).");
            }
            return applied;
        }

        private static void Report(int count, string where)
        {
            if (count == 0)
            {
                EditorUtility.DisplayDialog(
                    DirectTMPConstants.ToolName,
                    $"No new labels to convert in {where}.\n\n(They may already have a DirectFont.)",
                    "OK");
            }
            else
            {
                DirectTMPLog.Info($"Converted {count} TextMeshPro label(s) in {where}.");
            }
        }
    }
}
