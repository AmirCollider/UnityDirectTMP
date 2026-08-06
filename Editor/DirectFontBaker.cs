// ==========================================
// DirectFontBaker
// Carries the font FILES into the build.
//
// The joiner needs a font's own GSUB table to know what
// the designer drew for each joined shape, and reading
// GSUB needs the file. An imported Unity `Font` gives no
// route to it outside the Editor - see DirectFontBytes
// for the whole of why that matters.
//
// So before a build, every font a DirectFont points at is
// copied to a `.bytes` asset and listed in one table
// under Resources. Nothing else about the project changes,
// and a project that never builds never grows the copies.
// ==========================================
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityDirectTMP.EditorTools
{
    /// <summary>
    /// Builds <see cref="DirectFontBytesTable"/> from every
    /// <see cref="DirectFont"/> in the project's scenes and prefabs.
    /// </summary>
    public static class DirectFontBaker
    {
        private const string TableFolder = "Assets/Resources";
        private const string TablePath = TableFolder + "/" + DirectFontBytes.ResourcePath + ".asset";
        private const string BytesFolder = TableFolder + "/DirectTMPFontData";

        // The fields a DirectFont can hold a font in. Read through
        // SerializedObject because they are private, which is where they
        // belong - a component's storage is not an API.
        private static readonly string[] FontFields =
        {
            "font", "persianArabic", "japaneseChineseKorean", "latin",
        };

        [MenuItem("Unity DirectTMP/Bake Fonts For Build", false, 160)]
        public static void BakeFromMenu()
        {
            int baked = Bake(out string report);

            EditorUtility.DisplayDialog(
                "Unity DirectTMP",
                baked == 0
                    ? "No Direct Font component points at a font, so there was nothing to bake.\n\n" + report
                    : $"Baked {baked} font file(s) into the build.\n\n{report}",
                "OK");
        }

        /// <summary>
        /// Copies every font a <see cref="DirectFont"/> uses into the table,
        /// and returns how many are in it. Safe to call repeatedly: a file
        /// whose bytes have not changed is left alone.
        /// </summary>
        public static int Bake(out string report)
        {
            var fonts = new List<Font>();

            CollectFromPrefabs(fonts);
            CollectFromScenes(fonts);

            if (fonts.Count == 0)
            {
                report = "Nothing found in the build scenes or in any prefab.";
                return 0;
            }

            Directory.CreateDirectory(BytesFolder);

            var rows = new List<DirectFontBytesTable.Row>(fonts.Count);
            var lines = new List<string>(fonts.Count);

            foreach (Font font in fonts)
            {
                TextAsset bytes = CopyOf(font);
                if (bytes == null)
                {
                    Debug.LogWarning(
                        $"[DirectTMP] '{font.name}' has no readable file, so its joining rules "
                        + "cannot be carried into a build. Right-to-left text using it will fall "
                        + "back to the legacy presentation forms on device.", font);
                    continue;
                }

                rows.Add(new DirectFontBytesTable.Row { font = font, bytes = bytes });
                lines.Add($"  • {font.name}");
            }

            Write(rows);

            report = string.Join("\n", lines);
            return rows.Count;
        }

        // ==========================================
        // Where a DirectFont can be hiding.
        // ==========================================

        private static void CollectFromPrefabs(List<Font> into)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { continue; }

                foreach (DirectFont label in prefab.GetComponentsInChildren<DirectFont>(true))
                {
                    Collect(label, into);
                }
            }
        }

        /// <summary>
        /// Opens each scene in Build Settings, looks, and puts the Editor back
        /// exactly as it was. Whatever was open - and unsaved - is restored,
        /// so this never costs anybody their work in progress.
        /// </summary>
        private static void CollectFromScenes(List<Font> into)
        {
            EditorBuildSettingsScene[] inBuild = EditorBuildSettings.scenes;
            if (inBuild == null || inBuild.Length == 0) { return; }

            // Look at what is already open first, so an unsaved scene the
            // developer is working in is covered without being reloaded.
            var seen = new HashSet<string>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene open = SceneManager.GetSceneAt(i);
                if (!open.isLoaded) { continue; }

                CollectFrom(open, into);
                if (!string.IsNullOrEmpty(open.path)) { seen.Add(open.path); }
            }

            var toOpen = new List<string>();
            foreach (EditorBuildSettingsScene scene in inBuild)
            {
                if (!scene.enabled || string.IsNullOrEmpty(scene.path)) { continue; }
                if (seen.Contains(scene.path)) { continue; }

                toOpen.Add(scene.path);
            }

            if (toOpen.Count == 0) { return; }

            SceneSetup[] wasOpen = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                foreach (string path in toOpen)
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    CollectFrom(scene, into);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[DirectTMP] A scene could not be read while baking fonts: {e.Message}");
            }
            finally
            {
                if (wasOpen != null && wasOpen.Length > 0)
                {
                    try { EditorSceneManager.RestoreSceneManagerSetup(wasOpen); }
                    catch { /* the developer's scenes are already where they were */ }
                }
            }
        }

        private static void CollectFrom(Scene scene, List<Font> into)
        {
            if (!scene.IsValid() || !scene.isLoaded) { return; }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (DirectFont label in root.GetComponentsInChildren<DirectFont>(true))
                {
                    Collect(label, into);
                }
            }
        }

        private static void Collect(DirectFont label, List<Font> into)
        {
            if (label == null) { return; }

            var read = new SerializedObject(label);

            foreach (string field in FontFields)
            {
                SerializedProperty property = read.FindProperty(field);
                if (property == null) { continue; }

                if (property.objectReferenceValue is Font font && !into.Contains(font)) { into.Add(font); }
            }
        }

        // ==========================================
        // The copy itself.
        // ==========================================

        /// <summary>
        /// The font's file as a <c>.bytes</c> asset, copying it first if it is
        /// missing or out of date. Null when the font has no file to copy.
        /// </summary>
        private static TextAsset CopyOf(Font font)
        {
            string source = AssetDatabase.GetAssetPath(font);
            if (string.IsNullOrEmpty(source) || !File.Exists(source)) { return null; }

            byte[] bytes;
            try { bytes = File.ReadAllBytes(source); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[DirectTMP] Could not read '{source}': {e.Message}");
                return null;
            }

            // Named from the GUID, not from the font: two fonts can be called
            // the same thing in two folders, and a name can have characters a
            // file system will not take.
            string guid = AssetDatabase.AssetPathToGUID(source);
            string target = $"{BytesFolder}/{guid}.bytes";

            if (!Same(target, bytes))
            {
                File.WriteAllBytes(target, bytes);
                AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate);
            }

            return AssetDatabase.LoadAssetAtPath<TextAsset>(target);
        }

        private static bool Same(string path, byte[] bytes)
        {
            if (!File.Exists(path)) { return false; }

            try
            {
                var already = new FileInfo(path);
                if (already.Length != bytes.LongLength) { return false; }

                byte[] existing = File.ReadAllBytes(path);
                for (int i = 0; i < existing.Length; i++)
                {
                    if (existing[i] != bytes[i]) { return false; }
                }

                return true;
            }
            catch { return false; }
        }

        private static void Write(List<DirectFontBytesTable.Row> rows)
        {
            Directory.CreateDirectory(TableFolder);

            var table = AssetDatabase.LoadAssetAtPath<DirectFontBytesTable>(TablePath);
            bool isNew = table == null;

            if (isNew) { table = ScriptableObject.CreateInstance<DirectFontBytesTable>(); }

            // Rows somebody added by hand for a font nothing points at yet are
            // kept: the table is allowed to be edited, and a bake should not
            // quietly undo that.
            foreach (DirectFontBytesTable.Row kept in table.Rows)
            {
                if (kept.font == null || kept.bytes == null) { continue; }
                if (rows.Exists(row => row.font == kept.font)) { continue; }

                rows.Add(kept);
            }

            table.Rows = rows.ToArray();

            if (isNew) { AssetDatabase.CreateAsset(table, TablePath); }
            else { EditorUtility.SetDirty(table); }

            AssetDatabase.SaveAssets();
        }
    }

    /// <summary>
    /// Bakes the fonts before every build, so a device never gets a build with
    /// the joining rules left behind.
    /// </summary>
    public sealed class DirectFontBuildStep : IPreprocessBuildWithReport
    {
        /// <summary>Early, so the copies exist before anything is packed.</summary>
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                int baked = DirectFontBaker.Bake(out string _);
                if (baked > 0) { Debug.Log($"[DirectTMP] {baked} font file(s) baked into this build."); }
            }
            catch (System.Exception e)
            {
                // A build is never failed over this. Without the bake the text
                // falls back to the legacy presentation forms, which is what
                // every version before 2.1.7 did on device anyway.
                Debug.LogWarning($"[DirectTMP] Fonts could not be baked for this build: {e.Message}");
            }
        }
    }
}
