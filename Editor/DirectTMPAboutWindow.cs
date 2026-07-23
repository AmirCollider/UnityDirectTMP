// ==========================================
// DirectTMPAboutWindow
// A small, friendly About panel: version, tagline,
// and a link back to the repo.
// ==========================================
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    internal sealed class DirectTMPAboutWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<DirectTMPAboutWindow>(true, "About " + DirectTMPConstants.ToolName, true);
            window.minSize = new Vector2(380, 240);
            window.maxSize = new Vector2(380, 240);
        }

        private void OnGUI()
        {
            GUILayout.Space(12);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };
            GUILayout.Label("🖋️ " + DirectTMPConstants.ToolName, titleStyle);
            GUILayout.Label("v" + DirectTMPConstants.Version, EditorStyles.miniLabel);
            GUILayout.Space(8);
            GUILayout.Label(
                "Hand TextMeshPro the font file itself — a plain .ttf\nor .otf — and every language the font supports just\nworks. No Font Asset Creator, no character ranges,\nno tofu.",
                EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);
            GUILayout.Label("✨ Made with 🖋️ by " + DirectTMPConstants.Author, EditorStyles.label);
            GUILayout.Space(14);

            if (GUILayout.Button("Open GitHub Repository"))
            {
                Application.OpenURL(DirectTMPConstants.GithubUrl);
            }
            GUILayout.Space(6);
            GUILayout.Label("MIT License", EditorStyles.centeredGreyMiniLabel);
        }
    }
}
