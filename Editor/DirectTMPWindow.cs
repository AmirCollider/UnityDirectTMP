// ==========================================
// DirectTMPWindow
// The entire user interface: one font field and one
// checkbox.
//
// There is deliberately nothing else here. No catalog,
// no coverage report, no health check, no welcome
// screen. If the font works, the labels change; if it
// does not, the box at the bottom says why in a sentence
// with the fix in it.
// ==========================================
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.EditorTools
{
    /// <summary>Window ▸ Unity DirectTMP.</summary>
    public sealed class DirectTMPWindow : EditorWindow
    {
        private const string SettingsFolder = "Assets/Resources";
        private const string SettingsPath = SettingsFolder + "/DirectTMPSettings.asset";

        private DirectTMPSettings _settings;
        private string _status = string.Empty;
        private MessageType _statusKind = MessageType.None;

        [MenuItem("Window/Unity DirectTMP", false, 2000)]
        public static void Open()
        {
            DirectTMPWindow window = GetWindow<DirectTMPWindow>(true, "Unity DirectTMP");
            window.minSize = new Vector2(430, 260);
            window.Show();
        }

        private void OnEnable() => _settings = Load();

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Draw every TextMeshPro label from a font file",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Pick a .ttf or .otf. No font asset to build, no character set to choose.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(8);

            if (_settings == null) { _settings = Load(); }

            Font font = _settings == null ? null : _settings.Font;
            bool fixRtl = _settings == null || _settings.FixRightToLeft;

            EditorGUI.BeginChangeCheck();

            font = (Font)EditorGUILayout.ObjectField("Font", font, typeof(Font), false);

            EditorGUILayout.Space(4);
            fixRtl = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    "Join Persian / Arabic and read right-to-left",
                    "Costs nothing for text that has no Arabic script in it."),
                fixRtl);

            if (EditorGUI.EndChangeCheck())
            {
                Save(font, fixRtl);
                Apply();
            }

            EditorGUILayout.Space(10);

            using (new EditorGUI.DisabledScope(font == null))
            {
                if (GUILayout.Button("Apply now", GUILayout.Height(26))) { Apply(); }
            }

            if (GUILayout.Button("Turn off"))
            {
                DirectTMPDriver.Detach();
                DirectTMP.Stop();
                Save(null, fixRtl);
                _status = "Off. Labels keep whatever font they had.";
                _statusKind = MessageType.None;
            }

            EditorGUILayout.Space(10);

            if (!string.IsNullOrEmpty(_status)) { EditorGUILayout.HelpBox(_status, _statusKind); }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(
                "Persian and Arabic join in the Game view, in Play mode and in builds. "
                + "Nothing is added to your GameObjects and no scene is modified.",
                EditorStyles.wordWrappedMiniLabel);
        }

        // ==========================================
        // Applying
        // ==========================================
        private void Apply()
        {
            if (_settings == null || _settings.Font == null)
            {
                _status = "Pick a font file first.";
                _statusKind = MessageType.Info;
                return;
            }

            DirectTMP.FixRightToLeft = _settings.FixRightToLeft;

            TMP_FontAsset asset = DirectFont.Load(_settings.Font);
            if (asset == null)
            {
                _status = Diagnose(_settings.Font);
                _statusKind = MessageType.Error;
                return;
            }

            DirectTMP.Use(asset);

            // In the Editor a label's font is serialized, so it is left alone
            // and the project-wide fallback draws the glyphs instead. Nothing
            // is written to any scene.
            int touched = DirectTMPDriver.Tick(assignFont: false);

            var joiner = DirectFontJoiner.For(asset);
            joiner.Shape("سلام");   // makes it read the font's joining rules now

            _status = $"{_settings.Font.name} is in use on {touched} label(s).";
            _statusKind = MessageType.Info;

            if (!joiner.HasJoiningRules && !string.IsNullOrEmpty(joiner.Problem))
            {
                _status += "\n\nPersian/Arabic joining: " + joiner.Problem;
                _statusKind = MessageType.Warning;
            }

            SceneView.RepaintAll();
        }

        // The four project-level facts that stop a font file becoming a
        // dynamic atlas. None of them is visible from a scene, and every one
        // can differ between two projects that look identical.
        private static string Diagnose(Font font)
        {
            string path = AssetDatabase.GetAssetPath(font);

            if (TMP_Settings.instance == null)
            {
                return "TextMeshPro's Essential Resources are not imported. "
                     + "Window ▸ TextMeshPro ▸ Import TMP Essential Resources.";
            }

            var importer = AssetImporter.GetAtPath(path) as TrueTypeFontImporter;
            if (importer != null && !importer.includeFontData)
            {
                return $"'{font.name}' has \"Include Font Data\" turned OFF, so Unity kept the "
                     + "font's name instead of its outlines. Select it in the Project window, "
                     + "tick Include Font Data, and press Apply.";
            }

            if (importer != null && importer.fontTextureCase != FontTextureCase.Dynamic)
            {
                return $"'{font.name}' is imported with Character set to "
                     + $"{importer.fontTextureCase}, which bakes a fixed bitmap and throws the "
                     + "outlines away. Set Character to Dynamic in the Inspector.";
            }

            return $"Could not build a font asset from '{font.name}'. "
                 + "The Console has the reason.";
        }

        // ==========================================
        // The settings asset
        // ==========================================
        private static DirectTMPSettings Load()
        {
            var settings = AssetDatabase.LoadAssetAtPath<DirectTMPSettings>(SettingsPath);
            if (settings != null) { DirectTMPSettings.Instance = settings; }
            return settings;
        }

        private void Save(Font font, bool fixRtl)
        {
            if (_settings == null)
            {
                Directory.CreateDirectory(SettingsFolder);
                AssetDatabase.Refresh();

                _settings = CreateInstance<DirectTMPSettings>();
                AssetDatabase.CreateAsset(_settings, SettingsPath);
            }

            _settings.Font = font;
            _settings.FixRightToLeft = fixRtl;

            DirectTMPSettings.Instance = _settings;

            EditorUtility.SetDirty(_settings);
            AssetDatabase.SaveAssets();
        }
    }
}
