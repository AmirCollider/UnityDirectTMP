// ==========================================
// DirectTMPPromo
// The one place this tool advertises anything.
//
// Unity DirectTMP is free and MIT-licensed, so there
// is no upgrade to sell. What there is, is a sibling:
// Unity DocSnap, by the same author, on the same shelf
// - and a person who installed a font tool because
// their project has three languages in it is very
// often the same person who needs documentation of
// that project.
//
// The rules this panel holds itself to, because a
// developer tool that nags is a developer tool that
// gets deleted:
//
//   * One line. Not a banner, not an image, not a
//     modal, and never on first launch - the welcome
//     window has enough to say without also selling
//     something.
//
//   * Dismissible forever, from the panel itself, in
//     one click, with no "are you sure". A dismissal
//     that has to be repeated is not a dismissal.
//
//   * Never in the way of work. It sits in the footer
//     of two windows a person opens deliberately, and
//     appears nowhere in the Inspector, the menus, the
//     Console or the Project Settings page.
//
//   * It links to the tools page, not to a checkout.
//
// It also carries the star nudge, which is the only
// thing a free tool can honestly ask for.
// ==========================================
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    internal static class DirectTMPPromo
    {
        /// <summary>
        /// Draws the promo strip, unless it has been dismissed. Safe to call
        /// from any window's footer.
        /// </summary>
        public static void Draw()
        {
            if (DirectTMPPrefs.GetBool(DirectTMPPrefs.HidePromo, false)) { return; }

            GUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope(DirectTMPBrand.Card))
            {
                GUILayout.Label(DirectTMPConstants.SiblingToolMark, GUILayout.Width(22));

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label(
                        string.Format(
                            DirectTMPText.L(
                                "Also from {0}: {1}",
                                "{0} の別ツール: {1}",
                                "از همین سازنده ({0}): {1}"),
                            DirectTMPConstants.Author, DirectTMPConstants.SiblingToolName),
                        EditorStyles.boldLabel);

                    GUILayout.Label(
                        DirectTMPText.L(
                            "Snaps a whole Unity project into an offline website — every Scene, every Component, every field — for humans and AI alike.",
                            "Unity プロジェクト全体をオフラインの Web サイトに。全 Scene・全 Component・全フィールドを、人にも AI にも読める形で。",
                            "کل پروژه‌ی یونیتی رو می‌کنه یه وب‌سایت آفلاین — هر سین، هر کامپوننت، هر فیلد — هم برای آدم‌ها هم برای هوش مصنوعی."),
                        DirectTMPBrand.Subtitle);
                }

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(86)))
                {
                    if (GUILayout.Button(DirectTMPText.L("Take a look", "見てみる", "ببین"), EditorStyles.miniButton))
                    {
                        Application.OpenURL(DirectTMPConstants.SiblingToolUrl);
                    }
                    if (GUILayout.Button(DirectTMPText.L("Hide this", "非表示", "پنهان کن"), EditorStyles.miniButton))
                    {
                        DirectTMPPrefs.SetBool(DirectTMPPrefs.HidePromo, true);
                    }
                }
            }
        }

        /// <summary>
        /// The star nudge. Shown only in the About window - the one screen
        /// somebody opens because they are already thinking about the tool
        /// rather than about their work.
        /// </summary>
        public static void DrawStarNudge()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    DirectTMPText.L(
                        "Free and MIT-licensed. A star on the repo is the whole price.",
                        "無料・MIT ライセンスです。リポジトリへの ⭐ が唯一の対価です。",
                        "رایگان و با لایسنس MIT. تنها هزینه‌ش یه ⭐ روی ریپازیتوریه."),
                    DirectTMPBrand.Subtitle);

                if (GUILayout.Button(DirectTMPText.L("Star it", "スターを付ける", "ستاره بده"), EditorStyles.miniButton, GUILayout.Width(96)))
                {
                    Application.OpenURL(DirectTMPConstants.GithubUrl);
                }
            }
        }

        /// <summary>
        /// The line that points at the whole shelf rather than at one sibling.
        /// Used on the Project Settings page, where a link out to a catalogue
        /// is expected and a pitch would not be.
        /// </summary>
        public static void DrawCatalogLink()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    DirectTMPText.L(
                        "More Unity tools by this author",
                        "この作者の他の Unity ツール",
                        "ابزارهای یونیتی دیگه از همین سازنده"),
                    DirectTMPBrand.Subtitle);

                if (GUILayout.Button(DirectTMPText.L("Open the catalogue", "カタログを開く", "کاتالوگ رو باز کن"),
                        EditorStyles.miniButton, GUILayout.Width(150)))
                {
                    Application.OpenURL(DirectTMPConstants.ToolsCatalogUrl);
                }
            }
        }
    }
}
