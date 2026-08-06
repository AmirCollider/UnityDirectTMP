// ==========================================
// DirectTMPSettings
// The whole configuration surface: a font, and two
// checkboxes.
//
// It lives in Assets/Resources so that a build can load
// it, and so that opening a scene, entering Play mode or
// reloading the domain cannot lose it - the setting is
// in an asset on disk, not in a scene.
// ==========================================
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// The font this project draws its TextMeshPro labels with, and whether to
    /// fix right-to-left text. One asset, in Resources, for the whole project.
    /// </summary>
    public sealed class DirectTMPSettings : ScriptableObject
    {
        /// <summary>Where the one asset lives, relative to a Resources folder.</summary>
        public const string ResourcePath = "DirectTMPSettings";

        /// <summary>The .ttf/.otf every label is drawn with. Null turns the tool off.</summary>
        public Font Font;

        /// <summary>
        /// Join Arabic-script letters and put right-to-left text in reading
        /// order. Off costs nothing for a project with no Arabic in it, but
        /// leaving it on costs nothing either - text with no Arabic script in
        /// it is returned untouched.
        /// </summary>
        public bool FixRightToLeft = true;

        private static DirectTMPSettings s_instance;

        /// <summary>
        /// The project's settings, or null when none have been created. Never
        /// creates one - only the Editor does that, and only when asked.
        /// </summary>
        public static DirectTMPSettings Instance
        {
            get
            {
                if (s_instance != null) { return s_instance; }
                s_instance = Resources.Load<DirectTMPSettings>(ResourcePath);
                return s_instance;
            }
            set { s_instance = value; }
        }
    }
}
