// ==========================================
// DirectTMPMenu
// The menu, on the menu bar.
//
// It lives in a file of its own on purpose. A menu that
// is declared inside a CustomEditor disappears the
// moment anything else in that file stops compiling -
// and a menu that is not there is the hardest kind of
// failure to diagnose, because it looks exactly like a
// package that was never installed.
//
// Top level, not under Window or Tools, because that is
// where somebody looking for it will look.
// ==========================================
using TMPro;
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.EditorTools
{
    internal static class DirectTMPMenu
    {
        private const string Menu = "Unity DirectTMP/";

        // ==========================================
        // Add Direct Font to whatever is selected.
        //
        // Children too: a Canvas or a panel is what
        // people actually have selected, and asking them
        // to click every label under it is asking them to
        // do the thing this menu item exists to save.
        // ==========================================
        [MenuItem(Menu + "Add Direct Font to Selection", false, 100)]
        private static void AddToSelection()
        {
            int added = 0;
            int already = 0;

            foreach (GameObject root in Selection.gameObjects)
            {
                foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (label.GetComponent<DirectFont>() != null) { already++; continue; }

                    Undo.AddComponent<DirectFont>(label.gameObject);
                    added++;
                }
            }

            if (added == 0 && already == 0)
            {
                Debug.LogWarning("[DirectTMP] Nothing selected has a TextMeshPro label on it.");
                return;
            }

            Debug.Log($"[DirectTMP] Added Direct Font to {added} label(s)"
                    + (already > 0 ? $"; {already} already had one" : string.Empty)
                    + ". Assign a .ttf on each.");
        }

        [MenuItem(Menu + "Add Direct Font to Selection", true)]
        private static bool CanAddToSelection() => Selection.gameObjects.Length > 0;

        // ==========================================
        // Remove it again, so trying this package out
        // is never a one-way door.
        // ==========================================
        [MenuItem(Menu + "Remove Direct Font from Selection", false, 101)]
        private static void RemoveFromSelection()
        {
            int removed = 0;

            foreach (GameObject root in Selection.gameObjects)
            {
                foreach (DirectFont direct in root.GetComponentsInChildren<DirectFont>(true))
                {
                    Undo.DestroyObjectImmediate(direct);
                    removed++;
                }
            }

            Debug.Log($"[DirectTMP] Removed Direct Font from {removed} label(s).");
        }

        [MenuItem(Menu + "Clear Font Cache", false, 200)]
        private static void ClearCache()
            => Debug.Log($"[DirectTMP] Cleared {DirectTMP.ClearCache()} cached font asset(s).");

        [MenuItem(Menu + "Documentation", false, 300)]
        private static void Documentation()
            => Application.OpenURL("https://github.com/AmirCollider/UnityDirectTMP#readme");
    }
}
