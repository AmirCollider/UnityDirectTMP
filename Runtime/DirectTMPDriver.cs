// ==========================================
// DirectTMPDriver
// One hidden object, one LateUpdate, for the whole
// project.
//
// The project-wide override has no component on any
// label, which is the entire point of it - but the
// work it does still needs a heartbeat: a label whose
// text changed has to be re-prepared, and a label that
// was instantiated a moment ago has to be found. In
// the Editor that heartbeat is EditorApplication.update.
// In a build there is no such thing, so something in
// the scene has to have a LateUpdate.
//
// It is exactly one object, it is hidden, it is not
// saved, and it survives scene loads. It creates
// itself when the override is applied and it is never
// created at all when the override is off - a project
// that does not use the global font pays nothing.
//
// Not created in edit mode, ever. A GameObject that
// appears in somebody's Hierarchy on its own is a bug
// report no matter how well it is hidden, and the
// Editor already has a heartbeat that does not need
// one.
// ==========================================
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// The Play-mode heartbeat behind <see cref="DirectGlobalFont"/>. Created
    /// on demand, hidden, never saved, and shared by the whole project.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class DirectTMPDriver : MonoBehaviour
    {
        private static DirectTMPDriver s_instance;

        /// <summary>The live driver, or null when the override is not running.</summary>
        public static DirectTMPDriver Instance => s_instance;

        /// <summary>
        /// Makes sure a driver exists, in Play mode only. Safe to call as
        /// often as you like; it does nothing once one is alive.
        /// </summary>
        public static void Ensure()
        {
            if (!Application.isPlaying) { return; }
            if (s_instance != null) { return; }

            var host = new GameObject(DirectTMPConstants.ToolName)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            s_instance = host.AddComponent<DirectTMPDriver>();
            DontDestroyOnLoad(host);
        }

        /// <summary>Tears the driver down. The override stops being maintained.</summary>
        public static void Dismiss()
        {
            if (s_instance == null) { return; }

            GameObject host = s_instance.gameObject;
            s_instance = null;

            if (Application.isPlaying) { Destroy(host); }
            else { DestroyImmediate(host); }
        }

        private void Awake()
        {
            // A second driver can only come from a domain reload racing a
            // scene load. One is enough, and two would tick every label twice.
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_instance = this;
        }

        // LateUpdate, not Update: a label whose text was set this frame should
        // be re-prepared after whatever set it, not before.
        private void LateUpdate()
        {
            DirectGlobalFont.Tick();
        }

        private void OnDestroy()
        {
            if (s_instance == this) { s_instance = null; }
        }
    }
}
