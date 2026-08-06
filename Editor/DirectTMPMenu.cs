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

        // ==========================================
        // Diagnose
        //
        // Every question worth asking about one label,
        // answered in one Console message: what the text
        // is, what it was shaped into, which font is
        // drawing it, and - the one that matters -
        // whether the font asset can actually draw every
        // character the shaper emitted.
        //
        // A shaped codepoint with no character behind it
        // is a box on screen, and no amount of looking at
        // the Inspector will tell you which one it was.
        // ==========================================
        [MenuItem(Menu + "Diagnose Selected Label", false, 150)]
        private static void Diagnose()
        {
            var report = new System.Text.StringBuilder();
            int labels = 0;

            foreach (GameObject root in Selection.gameObjects)
            {
                foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    labels++;
                    Describe(label, report);
                }
            }

            if (labels == 0)
            {
                Debug.LogWarning("[DirectTMP] Select a TextMeshPro label (or something above one) first.");
                return;
            }

            Debug.Log("[DirectTMP] Diagnosis\n" + report);
        }

        private static void Describe(TMP_Text label, System.Text.StringBuilder report)
        {
            report.Append("\n── ").Append(label.name).Append(" ──\n");

            var direct = label.GetComponent<DirectFont>();
            report.Append("  Direct Font component : ").Append(direct == null ? "NO" : "yes").Append('\n');

            string text = label.text ?? string.Empty;
            report.Append("  text                  : ").Append(Show(text)).Append('\n');
            report.Append("  writing systems       : ").Append(DirectScripts.Describe(text)).Append('\n');

            TMP_FontAsset asset = label.font;
            report.Append("  font asset            : ")
                  .Append(asset == null ? "NONE" : asset.name).Append('\n');

            if (asset == null)
            {
                report.Append("  → the label has no font asset at all.\n");
                return;
            }

            report.Append("  atlas mode            : ").Append(asset.atlasPopulationMode).Append('\n');
            report.Append("  preprocessor attached : ")
                  .Append(label.textPreprocessor == null ? "NO" : label.textPreprocessor.GetType().Name)
                  .Append('\n');

            if (direct == null)
            {
                report.Append("  → no Direct Font on this label, so nothing shapes its text.\n");
                return;
            }

            DirectFontJoiner joiner = DirectFontJoiner.For(asset);
            string shaped = DirectTMP.Prepare(text, asset);

            report.Append("  shaped                : ").Append(Show(shaped)).Append('\n');
            report.Append("  joining rules read    : ").Append(joiner.HasJoiningRules ? "yes" : "NO").Append('\n');
            if (!string.IsNullOrEmpty(joiner.Problem))
            {
                report.Append("  joining problem       : ").Append(joiner.Problem).Append('\n');
            }

            // The two questions that decide what actually appears: can each
            // shape be drawn at all, and WHERE did its glyph come from. A
            // shape taken from the legacy presentation block is the usual
            // reason one letter in a word looks unattached while the rest are
            // fine - plenty of Persian fonts fill that block in badly.
            string missing = string.Empty;
            string sources = string.Empty;

            for (int i = 0; i < shaped.Length; i++)
            {
                char c = shaped[i];
                if (c == '\n' || c == '\r' || c == '\t') { continue; }

                if (!asset.HasCharacter(c, true, true)) { missing += $" U+{(int)c:X4}"; }

                string from = joiner.SourceOf(c);
                if (from != null) { sources += $" U+{(int)c:X4}={from}"; }
            }

            report.Append("  characters missing    : ")
                  .Append(missing.Length == 0 ? "none — every shape can be drawn" : missing)
                  .Append('\n');

            report.Append("  shape came from       : ")
                  .Append(sources.Length == 0 ? "(nothing needed a joined shape)" : sources.Trim())
                  .Append('\n');

            if (missing.Length > 0)
            {
                report.Append("  → those render as boxes.\n");
            }

            if (sources.Contains("=cmap"))
            {
                report.Append("  → shapes marked cmap come from the font's legacy presentation block\n")
                      .Append("    rather than its own joining rules. If one of those letters is the\n")
                      .Append("    one that looks wrong, the font's copy of that block is at fault.\n");
            }
        }

        // Text and codepoints together: Arabic in a Console message is
        // reordered by the Console itself, so the shape of the string is only
        // legible as numbers.
        private static string Show(string text)
        {
            if (string.IsNullOrEmpty(text)) { return "(empty)"; }

            var codes = new System.Text.StringBuilder();
            for (int i = 0; i < text.Length && i < 24; i++)
            {
                codes.Append(' ').Append(((int)text[i]).ToString("X4"));
            }
            if (text.Length > 24) { codes.Append(" …"); }

            return $"\"{text}\"   [{codes.ToString().Trim()}]";
        }

        [MenuItem(Menu + "Clear Font Cache", false, 200)]
        private static void ClearCache()
            => Debug.Log($"[DirectTMP] Cleared {DirectTMP.ClearCache()} cached font asset(s).");

        [MenuItem(Menu + "Documentation", false, 300)]
        private static void Documentation()
            => Application.OpenURL("https://github.com/AmirCollider/UnityDirectTMP#readme");
    }
}
