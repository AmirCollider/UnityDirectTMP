// ==========================================
// DirectTMPPopup
// A dropdown whose two halves are drawn by two
// different text systems, and get two different
// strings.
//
// EditorGUILayout.Popup looks like one control and is
// not. The list that drops down is handed to the
// operating system, which has a complete text stack -
// it shapes Arabic and reorders right-to-left text on
// its own. The button that opens it is IMGUI, drawn by
// Unity, which does neither.
//
// So one array of strings cannot serve both, and this
// is the bug that has flipped back and forth in this
// package twice:
//
//   1.0.0  prepared strings everywhere. The button
//          read correctly and the open list read
//          backwards, because the OS prepared it a
//          second time.
//   1.0.1  stored strings everywhere, to stop that.
//          Now the list reads correctly and the button
//          reads backwards - which is the screenshot
//          that arrived: فارسی in the list, یسراف on
//          the control above it.
//
// Both halves were right about their own half. This
// control is the fix: the button gets prepared text
// because IMGUI draws it, and the item list gets the
// text as stored because the OS draws that. Same
// selection, same order, two strings.
//
// EditorUtility.DisplayCustomMenu is the same call
// EditorGUI.Popup makes internally, so the list is the
// list Unity would have shown - including whatever it
// does about direction on this platform.
// ==========================================
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    /// <summary>
    /// A popup that prepares the label on its button and leaves the labels in
    /// its list exactly as stored.
    /// </summary>
    internal static class DirectTMPPopup
    {
        private static readonly int PopupHash = "DirectTMPPopup".GetHashCode();

        // A menu is not answered inside the OnGUI call that opened it: the
        // user picks later, and the callback arrives on its own. The choice
        // is parked here until the control that opened the menu next draws.
        private static int s_pendingControl;
        private static int s_pendingChoice = -1;
        private static EditorWindow s_window;

        /// <summary>
        /// The button's label: this one IS prepared, because IMGUI draws it
        /// and IMGUI joins nothing and reorders nothing.
        /// </summary>
        internal static GUIContent ButtonContent(string nativeLabel)
        {
            return new GUIContent(DirectTMPText.Show(nativeLabel));
        }

        /// <summary>
        /// Draws the popup. <paramref name="nativeLabels"/> are as stored -
        /// logical order, unshaped - because the list is drawn by the
        /// operating system.
        /// </summary>
        public static int Field(int selected, string[] nativeLabels, GUIStyle style, params GUILayoutOption[] options)
        {
            if (nativeLabels == null || nativeLabels.Length == 0) { return selected; }
            if (style == null) { style = EditorStyles.popup; }

            selected = Mathf.Clamp(selected, 0, nativeLabels.Length - 1);

            GUIContent content = ButtonContent(nativeLabels[selected]);
            Rect rect = GUILayoutUtility.GetRect(content, style, options);

            return Field(rect, selected, nativeLabels, content, style);
        }

        /// <summary>The same control at a rect the caller has already measured.</summary>
        public static int Field(Rect rect, int selected, string[] nativeLabels, GUIContent content, GUIStyle style)
        {
            int control = GUIUtility.GetControlID(PopupHash, FocusType.Keyboard, rect);

            // A choice made in the menu since the last repaint. Read before
            // the button is drawn rather than instead of drawing it: IMGUI
            // counts controls, and a frame that skips one is a frame that
            // mismatches every id after it.
            if (s_pendingControl == control && s_pendingChoice >= 0)
            {
                int picked = Mathf.Clamp(s_pendingChoice, 0, nativeLabels.Length - 1);

                s_pendingControl = 0;
                s_pendingChoice = -1;

                if (picked != selected)
                {
                    selected = picked;
                    GUI.changed = true;
                }
            }

            if (EditorGUI.DropdownButton(rect, content, FocusType.Keyboard, style))
            {
                var items = new GUIContent[nativeLabels.Length];
                for (int i = 0; i < nativeLabels.Length; i++)
                {
                    items[i] = new GUIContent(nativeLabels[i]);
                }

                s_window = EditorWindow.focusedWindow;
                EditorUtility.DisplayCustomMenu(rect, items, selected, Picked, control);
            }

            return selected;
        }

        private static void Picked(object userData, string[] options, int index)
        {
            s_pendingControl = userData is int control ? control : 0;
            s_pendingChoice = index;

            // The menu closes over a window that has no reason to redraw on
            // its own, and the choice is only read on the next repaint.
            if (s_window != null) { s_window.Repaint(); }
            s_window = null;
        }
    }
}
