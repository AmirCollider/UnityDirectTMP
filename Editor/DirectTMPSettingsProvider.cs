// ==========================================
// DirectTMPSettingsProvider
// The "Settings…" screen, living in
// Project Settings ▸ Unity DirectTMP. It edits the
// project defaults from DirectTMPSettings - sampling
// point size, atlas size, padding, render mode - and
// offers a couple of housekeeping buttons for the
// font cache. The Settings… menu item just opens
// this page.
// ==========================================
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace UnityDirectTMP.Editor
{
    internal static class DirectTMPSettingsProvider
    {
        private static readonly int[] AtlasSizes = { 256, 512, 1024, 2048, 4096, 8192 };
        private static readonly string[] AtlasSizeLabels = { "256", "512", "1024", "2048", "4096", "8192" };

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new SettingsProvider(DirectTMPEditorConstants.SettingsPath, SettingsScope.Project)
            {
                label = "Unity DirectTMP",
                guiHandler = _ => DrawGUI(),
                keywords = new HashSet<string>(new[]
                {
                    "font", "tmp", "textmeshpro", "sdf", "atlas", "direct", "ttf", "otf", "glyph", "fallback"
                })
            };
        }

        private static void DrawGUI()
        {
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawBody();
                }
                GUILayout.Space(8);
            }
        }

        private static void DrawBody()
        {
            EditorGUILayout.LabelField("Default rasterization", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These defaults are stamped onto the DirectFonts the batch converter creates, and " +
                "used as the starting point when you add a Direct Font component.",
                MessageType.None);

            DirectFontSettings current = DirectTMPSettings.CurrentSettings;

            EditorGUI.BeginChangeCheck();

            int size = EditorGUILayout.IntSlider(
                new GUIContent("Sampling Point Size", "Point size glyphs are rasterized at. Higher = crisper when scaled up, but fills the atlas faster."),
                current.SamplingPointSize,
                DirectTMPConstants.MinSamplingPointSize,
                DirectTMPConstants.MaxSamplingPointSize);

            int padding = EditorGUILayout.IntSlider(
                new GUIContent("Atlas Padding", "Spacing baked around each glyph so neighbouring SDF spreads never bleed together."),
                current.AtlasPadding, 0, 32);

            int width = AtlasSizeField("Atlas Width", current.AtlasWidth);
            int height = AtlasSizeField("Atlas Height", current.AtlasHeight);

            var mode = (GlyphRenderMode)EditorGUILayout.EnumPopup(
                new GUIContent("Render Mode", "SDFAA is TextMeshPro's crisp, scalable default."),
                current.RenderMode);

            if (EditorGUI.EndChangeCheck())
            {
                DirectTMPSettings.CurrentSettings = new DirectFontSettings(size, padding, width, height, mode);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Font cache", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Show Cache Folder"))
                {
                    string dir = DirectTMPSettings.ResolveRuntimeCacheFolderAbsolute();
                    System.IO.Directory.CreateDirectory(dir);
                    EditorUtility.RevealInFinder(dir);
                }
                if (GUILayout.Button("Clear Cache"))
                {
                    int released = DirectTMP.ClearCache();
                    Debug.Log($"[{DirectTMPConstants.ToolName}] Cleared {released} cached atlas(es).");
                }
            }

            EditorGUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset to Defaults"))
                {
                    DirectTMPSettings.ResetToDefaults();
                }
                if (GUILayout.Button("Open GitHub"))
                {
                    Application.OpenURL(DirectTMPConstants.GithubUrl);
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"{DirectTMPConstants.ToolName}  v{DirectTMPConstants.Version}", EditorStyles.centeredGreyMiniLabel);
        }

        private static int AtlasSizeField(string label, int value)
        {
            int index = System.Array.IndexOf(AtlasSizes, Mathf.ClosestPowerOfTwo(value));
            if (index < 0) { index = 2; } // default to 1024
            int picked = EditorGUILayout.Popup(new GUIContent(label), index, AtlasSizeLabels);
            return AtlasSizes[Mathf.Clamp(picked, 0, AtlasSizes.Length - 1)];
        }
    }
}
