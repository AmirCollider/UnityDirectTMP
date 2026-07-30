// ==========================================
// DirectTMPFallbackWindow
// A small hub for fallback chains - the reusable,
// ordered "try this font, then this one" assets.
// Create a new chain, or jump to one you already
// have. Editing the order happens in the Inspector
// of the chain itself (it's a plain ScriptableObject
// with a reorderable Fonts list), which keeps this
// window simple and Unity-native.
//
// It was also, until 1.0.1, the one window in the tool
// that spoke only English - which is a strange thing
// for a package whose menu bar offers three languages,
// and the kind of gap that only ever shows up in a
// screenshot somebody else took.
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
            EditorGUILayout.LabelField(
                DirectTMPText.L("Ordered fallback chains", "順序付きフォールバックチェーン", "زنجیره‌های فال‌بک ترتیب‌دار"),
                DirectTMPBrand.BoldLine);

            DirectTMPBrand.Note(
                "A fallback chain is a reusable list of fonts, in priority order. For every character, the first font in the chain that actually has the glyph supplies it. Drop the same chain onto any number of Direct Font components.",
                "フォールバックチェーンは、優先順に並べた再利用可能なフォント一覧です。1 文字ごとに、その文字を実際に持つ最初のフォントが使われます。同じチェーンをいくつの Direct Font に割り当てても構いません。",
                "زنجیره‌ی فال‌بک یه لیست قابل استفاده‌ی مجدد از فونت‌هاست، به ترتیب اولویت. برای هر کاراکتر، اولین فونتی که واقعاً اون گلیف رو داشته باشه انتخاب می‌شه. یه زنجیره رو می‌تونی روی هر تعداد Direct Font بذاری.",
                DirectTMPBrand.NoteLevel.Info);

            EditorGUILayout.Space(4);
            if (GUILayout.Button(
                    "＋  " + DirectTMPText.L("Create New Fallback Chain", "フォールバックチェーンを作成", "ساخت زنجیره‌ی فال‌بک تازه"),
                    GUILayout.Height(28)))
            {
                CreateChain();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(
                DirectTMPText.L("Chains in this project", "このプロジェクトのチェーン", "زنجیره‌های این پروژه"),
                DirectTMPBrand.BoldLine);

            List<DirectFontFallbackChain> chains = FindChains();
            if (chains.Count == 0)
            {
                EditorGUILayout.LabelField(
                    DirectTMPText.L(
                        "None yet — create one above.",
                        "まだありません。上のボタンから作成してください。",
                        "هنوز هیچی نیست — از بالا یکی بساز."),
                    EditorStyles.miniLabel);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (DirectFontFallbackChain chain in chains)
            {
                if (chain == null) { continue; }
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    int fontCount = chain.Fonts != null ? chain.Fonts.Count : 0;

                    // An asset somebody named فونت‌های فارسی is exactly the
                    // asset this package expects to find in a project.
                    EditorGUILayout.LabelField(DirectTMPText.Show(chain.name), GUILayout.MinWidth(120));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(
                        DirectTMPText.F("{0} font(s)", "{0} フォント", "{0} فونت", fontCount),
                        EditorStyles.miniLabel,
                        GUILayout.Width(70));

                    if (GUILayout.Button(
                            DirectTMPText.L("Select", "選択", "انتخاب"),
                            GUILayout.Width(70)))
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
            // A save panel is an OS window: it does its own shaping and
            // reordering, so it wants the strings as stored.
            string path = EditorUtility.SaveFilePanelInProject(
                DirectTMPText.Native("New Fallback Chain", "新しいフォールバックチェーン", "زنجیره‌ی فال‌بک تازه"),
                "DirectFontFallbackChain",
                "asset",
                DirectTMPText.Native(
                    "Where should the fallback chain asset live?",
                    "フォールバックチェーンのアセットをどこに置きますか?",
                    "فایل زنجیره‌ی فال‌بک کجا ساخته بشه؟"));
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
