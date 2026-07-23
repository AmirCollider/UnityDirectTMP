// ==========================================
// DirectTMPFallbackWindow
// A small hub for fallback chains - the reusable,
// ordered "try this font, then this one" assets.
// Create a new chain, or jump to one you already
// have. Editing the order happens in the Inspector
// of the chain itself (it's a plain ScriptableObject
// with a reorderable Fonts list), which keeps this
// window simple and Unity-native.
// ==========================================
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    internal sealed class DirectTMPFallbackWindow : EditorWindow
    {
        private Vector2 scroll;

        public static void ShowWindow()
        {
            var window = GetWindow<DirectTMPFallbackWindow>(true, "Fallback Chains — " + DirectTMPConstants.ToolName, true);
            window.minSize = new Vector2(420, 320);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Ordered fallback chains", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "A fallback chain is a reusable list of fonts, in priority order. For every character, " +
                "the first font in the chain that actually has the glyph supplies it. Drop the same chain " +
                "onto any number of Direct Font components.",
                MessageType.Info);

            EditorGUILayout.Space(4);
            if (GUILayout.Button("＋  Create New Fallback Chain", GUILayout.Height(28)))
            {
                CreateChain();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Chains in this project", EditorStyles.boldLabel);

            List<DirectFontFallbackChain> chains = FindChains();
            if (chains.Count == 0)
            {
                EditorGUILayout.LabelField("None yet — create one above.", EditorStyles.miniLabel);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (DirectFontFallbackChain chain in chains)
            {
                if (chain == null) { continue; }
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    int fontCount = chain.Fonts != null ? chain.Fonts.Count : 0;
                    EditorGUILayout.LabelField(chain.name, GUILayout.MinWidth(120));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"{fontCount} font(s)", EditorStyles.miniLabel, GUILayout.Width(70));
                    if (GUILayout.Button("Select", GUILayout.Width(70)))
                    {
                        Selection.activeObject = chain;
                        EditorGUIUtility.PingObject(chain);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static void CreateChain()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "New Fallback Chain",
                "DirectFontFallbackChain",
                "asset",
                "Where should the fallback chain asset live?");
            if (string.IsNullOrEmpty(path)) { return; }

            var chain = CreateInstance<DirectFontFallbackChain>();
            AssetDatabase.CreateAsset(chain, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = chain;
            EditorGUIUtility.PingObject(chain);
        }

        private static List<DirectFontFallbackChain> FindChains()
        {
            var list = new List<DirectFontFallbackChain>();
            foreach (string guid in AssetDatabase.FindAssets("t:DirectFontFallbackChain"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var chain = AssetDatabase.LoadAssetAtPath<DirectFontFallbackChain>(path);
                if (chain != null) { list.Add(chain); }
            }
            return list;
        }
    }
}
