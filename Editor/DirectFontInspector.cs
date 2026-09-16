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
        private bool _cjkOpen;
        private bool _scriptsOpen;

        // ==========================================
        // DrawScriptRules
        // The open-ended list: a script, its Unicode ranges,
        // and a font.
        //
        // Drawn by hand rather than as a plain PropertyField on
        // the list, for one reason: a rule that does not work
        // must SAY so. The ranges are a string somebody types
        // from a Unicode chart, so the ways to get it wrong are
        // ordinary - a typo, the wrong dash, a missing font -
        // and every one of them otherwise produces a rule that
        // sits in the Inspector looking exactly like a rule that
        // works while matching nothing at all.
        //
        // So each row is checked as it is drawn and says which
        // of the three states it is in. Nothing here can make a
        // rule fail; it can only make a failure visible.
        // ==========================================
        private void DrawScriptRules()
        {
            SerializedProperty list = serializedObject.FindProperty("scripts");
            if (list == null) { return; }

            if (list.arraySize > 0) { _scriptsOpen = true; }

            _scriptsOpen = EditorGUILayout.Foldout(_scriptsOpen,
                new GUIContent("Any other script" + (list.arraySize > 0 ? "  (" + list.arraySize + ")" : ""),
                    "Hebrew, Thai, Devanagari, cuneiform - anything with no field of its own. "
                    + "Give each one the Unicode ranges from the chart and a font."), true);

            if (!_scriptsOpen) { return; }

            using (new EditorGUI.IndentLevelScope())
            {
                for (int i = 0; i < list.arraySize; i++)
                {
                    SerializedProperty entry = list.GetArrayElementAtIndex(i);
                    SerializedProperty name = entry.FindPropertyRelative("name");
                    SerializedProperty ranges = entry.FindPropertyRelative("ranges");
                    SerializedProperty ruleFont = entry.FindPropertyRelative("font");

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(name,
                        new GUIContent("Name", "For your own benefit. Nothing depends on it."));
                    if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                    {
                        list.DeleteArrayElementAtIndex(i);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(ranges,
                        new GUIContent("Ranges",
                            "Hex, from the Unicode chart. 'from-to' for a range, or one codepoint "
                            + "on its own. Comma or space between them. Example, cuneiform: "
                            + "12000-123FF, 12400-1247F"));
                    EditorGUILayout.PropertyField(ruleFont,
                        new GUIContent("Font", "The .ttf or .otf for this script."));

                    DrawRuleStatus(ranges.stringValue, ruleFont.objectReferenceValue != null);

                    EditorGUILayout.EndVertical();
                }

                if (GUILayout.Button("Add a script"))
                {
                    list.arraySize++;

                    // A new element is a copy of the one above it in Unity's
                    // serialization, which would hand somebody a duplicate of
                    // the rule they just wrote and no sign that it happened.
                    SerializedProperty added = list.GetArrayElementAtIndex(list.arraySize - 1);
                    added.FindPropertyRelative("name").stringValue = "";
                    added.FindPropertyRelative("ranges").stringValue = "";
                    added.FindPropertyRelative("font").objectReferenceValue = null;
                }
            }
        }

        // One rule's state, in the words somebody needs to fix it.
        private static void DrawRuleStatus(string ranges, bool hasFont)
        {
            bool ok;
            int[] parsed = DirectFontRule.ParseRanges(ranges, out ok);
            bool blank = string.IsNullOrEmpty(ranges) || ranges.Trim().Length == 0;

            if (blank && !hasFont)
            {
                EditorGUILayout.HelpBox("Empty - add the ranges and a font, or remove this row.",
                    MessageType.None);
                return;
            }

            if (!ok)
            {
                EditorGUILayout.HelpBox(
                    parsed.Length > 0
                        ? "Some of these ranges could not be read, and those are ignored. "
                          + "The " + (parsed.Length / 2) + " that did parse are in use."
                        : "None of these ranges could be read, so this rule matches nothing. "
                          + "They are hex from the Unicode chart, like 12000-123FF.",
                    MessageType.Warning);
                return;
            }

            if (blank)
            {
                EditorGUILayout.HelpBox("No ranges, so this font is never chosen for a script - "
                    + "though it is still added as a fallback.", MessageType.Warning);
                return;
            }

            if (!hasFont)
            {
                EditorGUILayout.HelpBox("No font, so this rule does nothing yet.", MessageType.Warning);
                return;
            }

            int count = 0;
            for (int i = 0; i < parsed.Length; i += 2) { count += parsed[i + 1] - parsed[i] + 1; }

            EditorGUILayout.HelpBox(
                "In use: " + (parsed.Length / 2) + (parsed.Length == 2 ? " range, " : " ranges, ")
                + count.ToString("N0") + " codepoints.", MessageType.Info);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            SerializedProperty font = serializedObject.FindProperty("font");

            EditorGUILayout.PropertyField(font, new GUIContent("Font", "The .ttf or .otf this label is drawn from."));

            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("persianArabic"),
                new GUIContent("Persian / Arabic", "Used when the text is mostly Persian, Arabic or Urdu. Empty = use Font."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("japaneseChineseKorean"),
                new GUIContent("日本語 / 中文 / 한국어", "Used when the text is mostly Japanese, Chinese or Korean. Empty = use Font."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("latin"),
                new GUIContent("English / Latin", "Used when the text is mostly English or another Latin language. Empty = use Font."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cyrillic"),
                new GUIContent("Кириллица", "Used when the text is mostly Russian or another Cyrillic language. Empty = use Font."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("emoji"),
                new GUIContent("Emoji 😀", "Used when the text is mostly emoji, and added as a fallback so emoji "
                                         + "inside other text render too. No text font carries colour emoji."));

            // ==========================================
            // The three CJK languages, behind a foldout.
            //
            // Folded because one CJK font sets all three for most
            // people, and three more object fields in front of
            // everybody to serve the minority who need them is
            // the wrong trade. Opened by itself when any of them
            // is already filled in, so a scene that uses them
            // never hides them from the person who set them.
            // ==========================================
            SerializedProperty japanese = serializedObject.FindProperty("japanese");
            SerializedProperty chinese = serializedObject.FindProperty("chinese");
            SerializedProperty korean = serializedObject.FindProperty("korean");

            bool anyCjk = japanese.objectReferenceValue != null
                || chinese.objectReferenceValue != null
                || korean.objectReferenceValue != null;

            if (anyCjk) { _cjkOpen = true; }

            _cjkOpen = EditorGUILayout.Foldout(_cjkOpen,
                new GUIContent("Split Japanese / Chinese / Korean",
                    "One CJK font usually sets all three. Fill these in only when they need "
                    + "different faces - a Japanese and a Chinese font draw the same ideograph "
                    + "differently, and many Japanese fonts have no hangul at all."), true);

            if (_cjkOpen)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(japanese,
                        new GUIContent("日本語 Japanese", "Japanese specifically. Empty = the CJK font above, then Font."));
                    EditorGUILayout.PropertyField(chinese,
                        new GUIContent("中文 Chinese", "Chinese specifically. Empty = the CJK font above, then Font."));
                    EditorGUILayout.PropertyField(korean,
                        new GUIContent("한국어 Korean", "Korean specifically. Empty = the CJK font above, then Font."));
                }
            }

            EditorGUILayout.Space(6);
            DrawScriptRules();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Outline — this label only", EditorStyles.boldLabel);

            SerializedProperty width = serializedObject.FindProperty("outlineWidth");
            SerializedProperty ownMaterial = serializedObject.FindProperty("ownMaterial");

            EditorGUILayout.PropertyField(width,
                new GUIContent("Width",
                    "How thick an outline is drawn around this label's letters. 0 is no outline."));

            using (new EditorGUI.DisabledScope(width.floatValue <= 0f))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("outlineColor"),
                    new GUIContent("Colour", "The colour of that outline."));
            }

            // ==========================================
            // Said here because it is the question this
            // field raises: why is the outline on the
            // component rather than on the material, where
            // TextMeshPro keeps everybody else's?
            //
            // Because the material behind a font asset this
            // package generates is generated with it, at
            // load, with no file underneath - so nothing set
            // on it can be saved, and an outline set by hand
            // is gone at the next recompile.
            // ==========================================
            if (width.floatValue > 0f && !ownMaterial.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "An outline cannot be shared, so this label is given a material of its own "
                  + "anyway — otherwise the outline would be on every label using this font.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(ownMaterial,
                new GUIContent("Own material",
                    "Give this label its own material, so anything set on it does not change "
                  + "every other label using the same font. Costs one draw call. An outline "
                  + "switches this on by itself."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fixRightToLeft"),
                new GUIContent("Join Persian / Arabic",
                    "Costs nothing for text with no Arabic script in it."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fixWrappedLines"),
                new GUIContent("Keep wrapped lines in order",
                    "Needed for any right-to-left paragraph long enough to wrap. "
                  + "Turn it off only to rule it out."));

            bool edited = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            // ==========================================
            // Applied now rather than at the next tick.
            //
            // The component does its work in LateUpdate,
            // which in edit mode runs when the Editor
            // decides to and not when a slider is dragged.
            // Waiting for it is what makes an outline look
            // like it does nothing: you drag the width,
            // nothing happens, you drag it back.
            // ==========================================
            if (edited)
            {
                foreach (Object each in targets) { (each as DirectFont)?.Rebuild(); }
            }

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

            // ==========================================
            // Built here rather than reported as missing.
            //
            // The component builds its font asset in
            // LateUpdate, and the Inspector paints whenever
            // the mouse moves over it - so "not built yet"
            // is an ordinary state that lasts a fraction of
            // a second, and an Inspector that reported it as
            // a project-level failure was accusing the
            // project of something perfectly fine. Asking
            // for the asset here answers the question
            // instead of guessing at it, and the red box
            // below now only appears when the font genuinely
            // cannot be built.
            // ==========================================
            TMP_FontAsset asset = direct.FontAsset != null ? direct.FontAsset : direct.Rebuild();

            EditorGUILayout.LabelField("This label", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Writing systems", DirectScripts.Describe(label.text));
            EditorGUILayout.LabelField("Font in use", asset != null ? asset.name : "— not built —");

            if (asset == null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(Diagnose(direct), MessageType.Error);

                // A font that could not be built is not asked again, or the
                // Console fills with the same warning once per repaint. This is
                // how you say "I have fixed it now" without restarting anything.
                if (GUILayout.Button("Try again"))
                {
                    DirectTMP.ClearCache();
                    direct.Rebuild();
                }

                return;
            }

            // Only worth reporting for text that has Arabic script in it.
            if (direct.Script != DirectScript.Arabic) { return; }

            DirectFontJoiner joiner = DirectFontJoiner.For(asset);
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

            return $"Could not build a font asset from '{file.name}', and reading the file "
                 + "directly did not work either. If TextMeshPro's Essential Resources are not "
                 + "imported yet, do that first: Window ▸ TextMeshPro ▸ Import TMP Essential "
                 + "Resources. The Console has the details.";
        }

    }
}
