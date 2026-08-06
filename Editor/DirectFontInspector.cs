// ==========================================
// DirectFontInspector
// The Inspector for the Direct Font component.
//
// It draws the fields, and then it answers the two
// questions somebody staring at a broken label actually
// has: what script is this text in, and did the font
// manage to join it.
// ==========================================
using TMPro;
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.EditorTools
{
    [CustomEditor(typeof(DirectFont))]
    [CanEditMultipleObjects]
    public sealed class DirectFontInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty font = serializedObject.FindProperty("font");

            EditorGUILayout.PropertyField(font, new GUIContent("Font", "The .ttf or .otf this label is drawn from."));

            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("persianArabic"),
                new GUIContent("Persian / Arabic", "Used when the text is mostly Persian, Arabic or Urdu. Empty = use Font."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("japaneseChineseKorean"),
                new GUIContent("日本語 / 中文 / 한국어", "Used when the text is mostly Japanese, Chinese or Korean. Empty = use Font."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("latin"),
                new GUIContent("English / Latin", "Used when the text is mostly English or another Latin language. Empty = use Font."));

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ownMaterial"),
                new GUIContent("Own material",
                    "Give this label its own material, so an outline on it does not change "
                  + "every other label using the same font. Costs one draw call."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fixRightToLeft"),
                new GUIContent("Join Persian / Arabic",
                    "Costs nothing for text with no Arabic script in it."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fixWrappedLines"),
                new GUIContent("Keep wrapped lines in order",
                    "Needed for any right-to-left paragraph long enough to wrap. "
                  + "Turn it off only to rule it out."));

            serializedObject.ApplyModifiedProperties();

            if (font.objectReferenceValue == null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox("Assign a font file. That is all this needs.", MessageType.Info);
                return;
            }

            if (targets.Length == 1) { DrawStatus((DirectFont)target); }
        }

        // ==========================================
        // What is actually going on with this label
        // ==========================================
        private static void DrawStatus(DirectFont direct)
        {
            var label = direct.GetComponent<TMP_Text>();
            if (label == null) { return; }

            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("This label", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Writing systems", DirectScripts.Describe(label.text));
            EditorGUILayout.LabelField("Font in use",
                direct.FontAsset != null ? direct.FontAsset.name : "— not built —");

            if (direct.FontAsset == null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(Diagnose(direct), MessageType.Error);
                return;
            }

            // Only worth reporting for text that has Arabic script in it.
            if (direct.Script != DirectScript.Arabic) { return; }

            DirectFontJoiner joiner = DirectFontJoiner.For(direct.FontAsset);
            joiner.Shape("سلام");   // makes it read the font's joining rules now

            EditorGUILayout.Space(4);

            if (joiner.HasJoiningRules)
            {
                EditorGUILayout.HelpBox(
                    "Persian/Arabic joining is coming from this font's own OpenType tables.",
                    MessageType.Info);
            }
            else if (!string.IsNullOrEmpty(joiner.Problem))
            {
                EditorGUILayout.HelpBox("Persian/Arabic joining: " + joiner.Problem, MessageType.Warning);
            }
        }

        // The project-level faults that stop a font file becoming a dynamic
        // atlas. None of them is visible from a scene, and every one can
        // differ between two projects that look identical.
        private static string Diagnose(DirectFont direct)
        {
            SerializedObject so = new SerializedObject(direct);
            var file = so.FindProperty("font").objectReferenceValue as Font;
            if (file == null) { return "No font assigned."; }

            string path = AssetDatabase.GetAssetPath(file);
            var importer = AssetImporter.GetAtPath(path) as TrueTypeFontImporter;

            if (importer != null && !importer.includeFontData)
            {
                return $"'{file.name}' has \"Include Font Data\" turned OFF, so Unity kept the font's "
                     + "name instead of its outlines. Select it in the Project window, tick "
                     + "Include Font Data, and press Apply.";
            }

            if (importer != null && importer.fontTextureCase != FontTextureCase.Dynamic)
            {
                return $"'{file.name}' is imported with Character set to {importer.fontTextureCase}, "
                     + "which bakes a fixed bitmap and throws the outlines away. Set Character to "
                     + "Dynamic in the Inspector.";
            }

            return $"Could not build a font asset from '{file.name}'. If TextMeshPro's Essential "
                 + "Resources are not imported yet, do that first: Window ▸ TextMeshPro ▸ "
                 + "Import TMP Essential Resources. The Console has the details.";
        }

    }
}
