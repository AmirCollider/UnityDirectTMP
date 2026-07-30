// ==========================================
// DirectTMPCoverageBadges
// The row of little chips that says which writing
// systems a font actually contains.
//
// It is one file, drawn identically in three places -
// the Font Catalog rows, the Editor Font window, and
// the DirectFont Inspector - because those three
// answering the same question in three slightly
// different visual languages is how a user stops
// trusting any of them.
//
// The vocabulary is three states and no more:
//
//   green   every probe character is in the font.
//           Set text in this script and it will render.
//   amber   some are. Usually a font with Arabic but
//           without the four Persian letters, or CJK
//           with the common kanji and nothing else.
//           The most useful badge of the three, and
//           the one a boolean "supported" flag cannot
//           express.
//   grey    none. Drawn dimmed rather than omitted,
//           because "this font has no CJK" is an
//           answer and an absent badge is not.
//
// Scripts with no coverage are collapsed into a single
// trailing count once there are more than a couple, so
// a Latin-only UI font shows one green chip and a
// quiet "+9 more" instead of eleven grey ones.
// ==========================================
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    internal static class DirectTMPCoverageBadges
    {
        /// <summary>
        /// Draws the full badge row for a coverage map, wrapping to the width
        /// of whatever layout it is inside.
        /// </summary>
        public static void Draw(IDictionary<DirectScript, DirectScriptCoverage> coverage, bool showEmpty = false)
        {
            if (coverage == null) { return; }

            // Verdicts are carried alongside the scripts rather than looked
            // up again below. A coverage map that is missing a key - which a
            // caller assembling one by hand can easily produce - would
            // otherwise be an indexer throwing KeyNotFoundException in the
            // middle of an OnGUI, which takes the window down rather than
            // drawing one badge wrong.
            var present = new List<KeyValuePair<DirectScript, DirectScriptCoverage>>();
            int absent = 0;

            foreach (DirectScript script in DirectFontScripts.All)
            {
                DirectScriptCoverage verdict = coverage.TryGetValue(script, out DirectScriptCoverage value)
                    ? value
                    : DirectScriptCoverage.None;

                if (verdict == DirectScriptCoverage.None && !showEmpty) { absent++; }
                else { present.Add(new KeyValuePair<DirectScript, DirectScriptCoverage>(script, verdict)); }
            }

            // Wrapping by hand: IMGUI has no flow layout, so the row is
            // measured against the inspector width and broken when the next
            // chip would overflow. Without this a font covering nine scripts
            // pushes its own badges off the side of a catalog row.
            float available = EditorGUIUtility.currentViewWidth - 60f;
            float used = 0f;

            EditorGUILayout.BeginHorizontal();
            foreach (KeyValuePair<DirectScript, DirectScriptCoverage> entry in present)
            {
                DirectScript script = entry.Key;
                DirectScriptCoverage verdict = entry.Value;
                // Measure what will actually be drawn. Joining Arabic letters
                // up changes their width, so measuring the stored form would
                // wrap the row in the wrong place in exactly one language.
                string label = DirectTMPText.Show(Label(script, verdict));
                float width = DirectTMPBrand.Badge.CalcSize(new GUIContent(label)).x + 6f;

                if (used + width > available && used > 0f)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    used = 0f;
                }

                DirectTMPBrand.Pill(
                    label,
                    DirectTMPBrand.ColorFor(verdict),
                    DirectTMPText.Show(Tooltip(script, verdict)));
                used += width;
            }

            if (absent > 0)
            {
                string more = DirectTMPText.F("+{0} not covered", "+{0} 未収録", "+{0} پوشش ندارد", absent);
                DirectTMPBrand.Pill(more, DirectTMPBrand.Off, DirectTMPText.Show(Absent(coverage)));
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// A one-line summary for places too tight for chips - a catalog row
        /// collapsed to a single line, a tooltip, a log message.
        ///
        /// LOGICAL order: it is a list of script names joined together, and
        /// the caller that draws it prepares the finished line.
        /// </summary>
        public static string Summarize(IDictionary<DirectScript, DirectScriptCoverage> coverage)
        {
            if (coverage == null) { return string.Empty; }

            List<DirectScript> full = DirectFontScripts.FullyCovered(coverage);
            if (full.Count == 0)
            {
                return DirectTMPText.Raw("No complete script", "完全な文字体系なし", "هیچ خطی کامل نیست");
            }

            var names = new List<string>(full.Count);
            foreach (DirectScript script in full)
            {
                names.Add(DirectFontScripts.NameFor(script, DirectTMPText.Current));
            }
            return string.Join(" · ", names.ToArray());
        }

        // Label, Tooltip and Absent all return LOGICAL text: each one glues a
        // script name onto a sentence, and the gluing has to happen before
        // anything is turned around for display.
        private static string Label(DirectScript script, DirectScriptCoverage verdict)
        {
            string name = DirectFontScripts.NameFor(script, DirectTMPText.Current);
            return verdict == DirectScriptCoverage.Partial ? name + " ~" : name;
        }

        private static string Tooltip(DirectScript script, DirectScriptCoverage verdict)
        {
            string name = DirectFontScripts.NameFor(script, DirectTMPText.Current);
            switch (verdict)
            {
                case DirectScriptCoverage.Full:
                    return string.Format(
                        DirectTMPText.Raw(
                            "{0}: every character checked for is present.",
                            "{0}: 確認した文字はすべて収録されています。",
                            "{0}: همه‌ی کاراکترهایی که چک شد موجوده."),
                        name);

                case DirectScriptCoverage.Partial:
                    return string.Format(
                        DirectTMPText.Raw(
                            "{0}: some characters are present and some are not. Text in this script will render with gaps.",
                            "{0}: 一部の文字だけが収録されています。この文字体系のテキストは一部が欠けて表示されます。",
                            "{0}: بعضی کاراکترها هست و بعضی نیست. متن این خط با جای خالی رندر می‌شه."),
                        name);

                default:
                    return string.Format(
                        DirectTMPText.Raw(
                            "{0}: not in this font.",
                            "{0}: このフォントには入っていません。",
                            "{0}: توی این فونت نیست."),
                        name);
            }
        }

        private static string Absent(IDictionary<DirectScript, DirectScriptCoverage> coverage)
        {
            var names = new List<string>();
            foreach (DirectScript script in DirectFontScripts.All)
            {
                if (coverage.TryGetValue(script, out DirectScriptCoverage verdict) && verdict == DirectScriptCoverage.None)
                {
                    names.Add(DirectFontScripts.NameFor(script, DirectTMPText.Current));
                }
            }
            return DirectTMPText.Raw("Not in this font: ", "このフォントに未収録: ", "توی این فونت نیست: ")
                + string.Join(", ", names.ToArray());
        }
    }
}
