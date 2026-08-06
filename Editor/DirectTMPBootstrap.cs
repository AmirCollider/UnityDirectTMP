// ==========================================
// DirectTMPBootstrap
// Applies the project's setting in the Editor, without
// anybody having to open a window.
//
// The setting lives in an asset, so it survives a domain
// reload - but the font asset it builds does not, and
// neither does the preprocessor on each label. Both are
// rebuilt here, on every reload, every scene open and
// every entry into Play mode.
// ==========================================
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.EditorTools
{
    [InitializeOnLoad]
    internal static class DirectTMPBootstrap
    {
        // Edit mode has no Update loop, and labels appear whenever somebody
        // drags one into a scene. Ticking on a timer rather than every editor
        // frame keeps the cost off the main thread's back.
        private const double Interval = 0.5;

        private static double s_next;

        static DirectTMPBootstrap()
        {
            EditorApplication.delayCall += Apply;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += _ => EditorApplication.delayCall += Apply;
        }

        private static void Apply()
        {
            DirectTMPSettings settings = DirectTMPSettings.Instance;
            if (settings == null || settings.Font == null) { return; }

            DirectTMP.FixRightToLeft = settings.FixRightToLeft;

            TMPro.TMP_FontAsset asset = DirectFont.Load(settings.Font);
            if (asset == null) { return; }

            DirectTMP.Use(asset);

            // A label's font is serialized; assigning it here would mark the
            // scene dirty for a change nobody asked to save. The project-wide
            // fallback draws the same glyphs and writes nothing.
            DirectTMPDriver.Tick(assignFont: Application.isPlaying);
        }

        private static void Tick()
        {
            if (!DirectTMP.Active) { return; }
            if (EditorApplication.timeSinceStartup < s_next) { return; }

            s_next = EditorApplication.timeSinceStartup + Interval;
            DirectTMPDriver.Tick(assignFont: Application.isPlaying);
        }
    }
}
