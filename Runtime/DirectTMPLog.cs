// ==========================================
// DirectTMPLog
// Every line this package writes to the console
// goes through here.
//
// Two reasons it exists rather than a bare
// Debug.LogWarning at each call site:
//
//   * One prefix. A user who wants to know what the
//     font tooling has been doing types "DirectTMP"
//     into the Console filter and sees all of it and
//     nothing else. That only holds if every message
//     is stamped the same way, which is a rule no
//     amount of care keeps once there are forty call
//     sites.
//
//   * A mute switch. A screen that rebuilds a font
//     every frame because somebody wired Refresh()
//     into Update() will otherwise write the same
//     warning sixty times a second until the Console
//     is the only thing in the project that is slow.
//     Muting is opt-in, off by default, and never
//     silences errors - the point is to stop a flood,
//     not to hide a failure.
// ==========================================
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// Console output for Unity DirectTMP: one prefix, one place to silence
    /// warnings from, and errors that are never silenced.
    /// </summary>
    public static class DirectTMPLog
    {
        /// <summary>
        /// When true, <see cref="Info"/> and <see cref="Warn"/> are dropped.
        /// <see cref="Error"/> always gets through - a font that failed to
        /// load is a thing the developer has to be told about, and a setting
        /// somebody flipped six months ago is not consent to hide it.
        /// </summary>
        public static bool Muted { get; set; }

        public static void Info(string message)
        {
            if (Muted) { return; }
            Debug.Log(DirectTMPConstants.LogPrefix + message);
        }

        public static void Info(string message, Object context)
        {
            if (Muted) { return; }
            Debug.Log(DirectTMPConstants.LogPrefix + message, context);
        }

        public static void Warn(string message)
        {
            if (Muted) { return; }
            Debug.LogWarning(DirectTMPConstants.LogPrefix + message);
        }

        public static void Warn(string message, Object context)
        {
            if (Muted) { return; }
            Debug.LogWarning(DirectTMPConstants.LogPrefix + message, context);
        }

        public static void Error(string message)
        {
            Debug.LogError(DirectTMPConstants.LogPrefix + message);
        }

        public static void Error(string message, Object context)
        {
            Debug.LogError(DirectTMPConstants.LogPrefix + message, context);
        }
    }
}
