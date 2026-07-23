// ==========================================
// DirectTMPSettings
// The project-wide defaults the editor tooling uses:
// the sampling point size, atlas size, padding and
// render mode that the batch converter stamps onto
// the DirectFonts it creates, and that the Inspector
// offers as its starting point. Stored in EditorPrefs
// under a key salted with the project path, so two
// projects open on the same machine keep their own
// values.
// ==========================================
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace UnityDirectTMP.Editor
{
    internal static class DirectTMPSettings
    {
        private static readonly string Prefix = "UnityDirectTMP." + ProjectSalt() + ".";

        private static string KeySamplingSize => Prefix + "samplingPointSize";
        private static string KeyPadding => Prefix + "atlasPadding";
        private static string KeyAtlasWidth => Prefix + "atlasWidth";
        private static string KeyAtlasHeight => Prefix + "atlasHeight";
        private static string KeyRenderMode => Prefix + "renderMode";

        /// <summary>
        /// The current project defaults, read from EditorPrefs and clamped to
        /// the safe range. Setting it writes every field back.
        /// </summary>
        public static DirectFontSettings CurrentSettings
        {
            get
            {
                var s = new DirectFontSettings(
                    EditorPrefs.GetInt(KeySamplingSize, DirectTMPConstants.DefaultSamplingPointSize),
                    EditorPrefs.GetInt(KeyPadding, DirectTMPConstants.DefaultAtlasPadding),
                    EditorPrefs.GetInt(KeyAtlasWidth, DirectTMPConstants.DefaultAtlasWidth),
                    EditorPrefs.GetInt(KeyAtlasHeight, DirectTMPConstants.DefaultAtlasHeight),
                    (GlyphRenderMode)EditorPrefs.GetInt(KeyRenderMode, (int)DirectTMPConstants.DefaultRenderMode));
                return s.Clamped();
            }
            set
            {
                DirectFontSettings s = value.Clamped();
                EditorPrefs.SetInt(KeySamplingSize, s.SamplingPointSize);
                EditorPrefs.SetInt(KeyPadding, s.AtlasPadding);
                EditorPrefs.SetInt(KeyAtlasWidth, s.AtlasWidth);
                EditorPrefs.SetInt(KeyAtlasHeight, s.AtlasHeight);
                EditorPrefs.SetInt(KeyRenderMode, (int)s.RenderMode);
            }
        }

        /// <summary>Restore every project default to the package baseline.</summary>
        public static void ResetToDefaults()
        {
            CurrentSettings = DirectFontSettings.Default;
        }

        /// <summary>Absolute path to the runtime font-spool cache folder.</summary>
        public static string ResolveRuntimeCacheFolderAbsolute()
        {
            return System.IO.Path.Combine(Application.persistentDataPath, DirectTMPConstants.RuntimeCacheFolderName)
                .Replace('\\', '/');
        }

        // A short, stable, project-specific salt so EditorPrefs keys don't
        // collide between two projects opened on the same machine.
        private static string ProjectSalt()
        {
            string path = Application.dataPath;
            return (path == null ? 0 : path.GetHashCode()).ToString("x8");
        }
    }
}
