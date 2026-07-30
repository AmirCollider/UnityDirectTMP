// ==========================================
// DirectTMPAboutWindow
// Inky, the version, what the tool is for in three
// lines, and the links.
//
// An About window is read for exactly two reasons -
// "what version am I on" and "where do I report this"
// - so both are above the fold and neither needs a
// click to reveal. The state panel underneath answers
// the third question people actually open it for:
// whether the Editor font override is on right now.
// ==========================================
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    internal sealed class DirectTMPAboutWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<DirectTMPAboutWindow>(true, "About " + DirectTMPConstants.ToolName, true);
            window.minSize = new Vector2(440, 420);
            window.maxSize = new Vector2(440, 420);
        }

        private void OnGUI()
        {
            DirectTMPBrand.Header(
                DirectTMPConstants.ToolName,
                string.Format(
                    DirectTMPText.L(
                        "{0} theme · mascot {1} · MIT licensed",
                        "{0} テーマ · マスコット {1} · MIT ライセンス",
                        "تم {0} · نماد {1} · لایسنس MIT"),
                    DirectTMPConstants.ThemeName, DirectTMPConstants.MascotName));

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(14);
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(4);

                    GUILayout.Label(
                        DirectTMPText.L(
                            "Hand TextMeshPro the font file itself — a plain .ttf or .otf — and every language the font supports just works. No Font Asset Creator, no character ranges, no tofu.",
                            "TextMeshPro にフォントファイル(ふつうの .ttf / .otf)をそのまま渡せば、そのフォントが対応する言語はすべてそのまま表示されます。Font Asset Creator も文字範囲の指定も豆腐もありません。",
                            "فایل فونت رو — یه .ttf یا .otf ساده — مستقیم بده به TextMeshPro، و هر زبونی که فونت پشتیبانی می‌کنه همون‌جوری کار می‌کنه. نه Font Asset Creator، نه رنج کاراکتر، نه مربع خالی."),
                        EditorStyles.wordWrappedLabel);

                    GUILayout.Space(8);
                    DirectTMPBrand.SectionTitle(DirectTMPText.L("Right now", "現在の状態", "همین حالا"));

                    using (new EditorGUILayout.VerticalScope(DirectTMPBrand.Card))
                    {
                        Row(DirectTMPText.L("Version", "バージョン", "نسخه"), DirectTMPConstants.Version);
                        Row(DirectTMPText.L("Editor font", "エディタのフォント", "فونت ادیتور"),
                            DirectEditorFontSettings.Plan.Describe());
                        Row(DirectTMPText.L("Cached atlases", "キャッシュ済みアトラス", "اطلس‌های کش‌شده"),
                            DirectTMP.CachedFontCount.ToString());
                        Row(DirectTMPText.Word("language"),
                            DirectTMPText.Labels[DirectTMPText.IndexOf(DirectTMPText.Current)]);
                    }

                    GUILayout.Space(8);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("GitHub", GUILayout.Height(26)))
                        {
                            Application.OpenURL(DirectTMPConstants.GithubUrl);
                        }
                        if (GUILayout.Button(DirectTMPText.L("Product page", "製品ページ", "صفحه‌ی محصول"), GUILayout.Height(26)))
                        {
                            Application.OpenURL(DirectTMPConstants.ProductUrl);
                        }
                        if (GUILayout.Button(DirectTMPText.L("Changelog", "変更履歴", "تغییرات"), GUILayout.Height(26)))
                        {
                            Application.OpenURL(DirectTMPConstants.ChangelogUrl);
                        }
                    }

                    GUILayout.Space(8);
                    DirectTMPPromo.DrawStarNudge();

                    GUILayout.Space(6);
                    GUILayout.Label(
                        string.Format(
                            DirectTMPText.L("Made with {0} by {1}", "{0} を込めて {1} より", "با {0} ساخته‌ی {1}"),
                            DirectTMPConstants.Mark, DirectTMPConstants.Author),
                        EditorStyles.centeredGreyMiniLabel);
                }
                GUILayout.Space(14);
            }
        }

        private static void Row(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, DirectTMPBrand.Subtitle, GUILayout.Width(160));
                GUILayout.Label(value, EditorStyles.miniLabel);
            }
        }
    }
}
