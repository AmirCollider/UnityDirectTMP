// ==========================================
// DirectFontInspector
// The Inspector you already know, with one small
// panel added. It draws the font-file field, the
// optional fallback chain and the material/settings
// toggles, warns if there's no TextMeshPro component
// to drive, and shows a live read-out of the font
// that's currently built (family, style, how many
// glyphs have been rasterized so far).
// ==========================================
using TMPro;
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    [CustomEditor(typeof(DirectFont))]
    [CanEditMultipleObjects]
    internal sealed class DirectFontInspector : UnityEditor.Editor
    {
        private SerializedProperty fontFile;
        private SerializedProperty fallbackChain;
        private SerializedProperty preserveMaterial;
        private SerializedProperty applyOnEnable;
        private SerializedProperty overrideSettings;
        private SerializedProperty settings;

        private void OnEnable()
        {
            fontFile = serializedObject.FindProperty("fontFile");
            fallbackChain = serializedObject.FindProperty("fallbackChain");
            preserveMaterial = serializedObject.FindProperty("preserveMaterial");
            applyOnEnable = serializedObject.FindProperty("applyOnEnable");
            overrideSettings = serializedObject.FindProperty("overrideSettings");
            settings = serializedObject.FindProperty("settings");
        }

        public override void OnInspectorGUI()
        {
            var directFont = (DirectFont)target;

            if (directFont.GetComponent<TMP_Text>() == null)
            {
                EditorGUILayout.HelpBox(
                    "Direct Font needs a TextMeshPro component (TextMeshProUGUI or TextMeshPro) on the same GameObject.",
                    MessageType.Warning);
            }

            serializedObject.Update();

            EditorGUILayout.PropertyField(fontFile, new GUIContent("Font File", "The .ttf/.otf to render glyphs from."));
            EditorGUILayout.PropertyField(fallbackChain, new GUIContent("Fallback Chain", "Optional ordered chain of fonts to fall back through, per character."));

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(preserveMaterial);
            EditorGUILayout.PropertyField(applyOnEnable);

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(overrideSettings, new GUIContent("Override Settings", "Use custom rasterization settings for this label instead of the project defaults."));
            if (overrideSettings.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(settings.FindPropertyRelative("samplingPointSize"), new GUIContent("Sampling Point Size"));
                    EditorGUILayout.PropertyField(settings.FindPropertyRelative("atlasPadding"), new GUIContent("Atlas Padding"));
                    EditorGUILayout.PropertyField(settings.FindPropertyRelative("atlasWidth"), new GUIContent("Atlas Width"));
                    EditorGUILayout.PropertyField(settings.FindPropertyRelative("atlasHeight"), new GUIContent("Atlas Height"));
                    EditorGUILayout.PropertyField(settings.FindPropertyRelative("renderMode"), new GUIContent("Render Mode"));
                }
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild Now"))
                {
                    foreach (Object t in targets)
                    {
                        (t as DirectFont)?.Refresh();
                    }
                }
                if (GUILayout.Button("Clear Font Cache"))
                {
                    int released = DirectTMP.ClearCache();
                    Debug.Log($"[{DirectTMPConstants.ToolName}] Cleared {released} cached atlas(es).");
                }
            }

            if (!serializedObject.isEditingMultipleObjects)
            {
                DrawLiveReadout(directFont);
            }
        }

        // ==========================================
        // DrawLiveReadout
        // A quick glance at whatever font is built right
        // now: family / style and how many glyphs have
        // been rasterized into the atlas so far.
        // ==========================================
        private static void DrawLiveReadout(DirectFont directFont)
        {
            TMP_FontAsset asset = directFont.FontAsset;
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Live font", EditorStyles.boldLabel);

            if (asset == null)
            {
                EditorGUILayout.HelpBox("No font built yet. Assign a Font File and it will build on enable.", MessageType.None);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Family", asset.faceInfo.familyName);
                EditorGUILayout.LabelField("Style", asset.faceInfo.styleName);
                int glyphs = asset.glyphTable != null ? asset.glyphTable.Count : 0;
                int chars = asset.characterTable != null ? asset.characterTable.Count : 0;
                EditorGUILayout.LabelField("Rasterized so far", $"{glyphs} glyph(s), {chars} character(s)");
                EditorGUILayout.LabelField("Settings", directFont.ResolvedSettings.ToString());
            }
        }
    }
}
