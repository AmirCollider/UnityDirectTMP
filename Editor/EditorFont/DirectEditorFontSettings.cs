// ==========================================
// DirectEditorFontSettings
// Where the Editor-font override is remembered.
//
// EditorPrefs, salted with the project path, exactly
// like DirectTMPSettings. Two reasons it is not a
// ProjectSettings asset:
//
//   * It has to be readable before anything in the
//     project is. The patcher runs from
//     [InitializeOnLoad], which is earlier than the
//     AssetDatabase is willing to answer questions,
//     and a setting that cannot be read at that point
//     is a setting that applies one frame late on
//     every single domain reload.
//
//   * It is per-machine by nature. Whether the person
//     at this keyboard needs the Editor in a font that
//     can draw Persian is not a fact about the
//     repository, and committing it would hand it to
//     everybody else on the team.
//
// It is still salted per project, because the font it
// points at usually lives in that project's Assets and
// would be a dangling path anywhere else.
// ==========================================
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    /// <summary>
    /// Reads and writes the Editor-font plan, and remembers the one-off
    /// choices around it (whether the safety check has been explained, whether
    /// the tip has been dismissed).
    /// </summary>
    internal static class DirectEditorFontSettings
    {
        private static readonly string Prefix = "UnityDirectTMP." + ProjectSalt() + ".editorFont.";

        private static string KeyEnabled => Prefix + "enabled";
        private static string KeySource => Prefix + "source";
        private static string KeyAssetPath => Prefix + "assetPath";
        private static string KeySystemFamily => Prefix + "systemFamily";
        private static string KeySizeDelta => Prefix + "sizeDelta";

        /// <summary>
        /// The stored plan. Reading is cheap enough to do from
        /// <c>EditorApplication.update</c>; writing re-applies it immediately.
        /// </summary>
        public static DirectEditorFontPlan Plan
        {
            get
            {
                return new DirectEditorFontPlan(
                    EditorPrefs.GetBool(KeyEnabled, false),
                    (DirectEditorFontSource)EditorPrefs.GetInt(KeySource, (int)DirectEditorFontSource.UnityDefault),
                    EditorPrefs.GetString(KeyAssetPath, string.Empty),
                    EditorPrefs.GetString(KeySystemFamily, string.Empty),
                    EditorPrefs.GetInt(KeySizeDelta, 0));
            }
            set
            {
                EditorPrefs.SetBool(KeyEnabled, value.Enabled);
                EditorPrefs.SetInt(KeySource, (int)value.Source);
                EditorPrefs.SetString(KeyAssetPath, value.AssetPath ?? string.Empty);
                EditorPrefs.SetString(KeySystemFamily, value.SystemFamily ?? string.Empty);
                EditorPrefs.SetInt(KeySizeDelta, DirectEditorFontRules.ClampSizeDelta(value.SizeDelta));
            }
        }

        /// <summary>Forgets the override entirely and puts the Editor back to Unity's font.</summary>
        public static void Clear()
        {
            Plan = DirectEditorFontPlan.Off;
            DirectEditorFont.Revert();
        }

        /// <summary>
        /// The Font asset a project-font plan points at, or null. Kept here
        /// rather than in the patcher so the window and the patcher resolve the
        /// same path the same way.
        /// </summary>
        public static Font ResolveAsset(DirectEditorFontPlan plan)
        {
            if (plan.Source != DirectEditorFontSource.ProjectFont || string.IsNullOrEmpty(plan.AssetPath))
            {
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<Font>(plan.AssetPath);
        }

        // A short, stable, project-specific salt so EditorPrefs keys never
        // collide between two projects opened on the same machine - which
        // would otherwise mean opening project B restyles the Editor with a
        // font that only exists in project A.
        private static string ProjectSalt()
        {
            string path = Application.dataPath;
            return (path == null ? 0 : path.GetHashCode()).ToString("x8");
        }
    }
}
