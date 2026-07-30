// ==========================================
// DirectTMPWelcomeWindow
// The screen that appears once, the first time this
// version is loaded in a project.
//
// It exists because of a specific complaint: the
// package installs, and then nothing appears to have
// happened. There is no scene change, no new asset, no
// visible difference anywhere - the tool is a menu, an
// Inspector addition and two windows, none of which
// announce themselves. A first-run screen is the
// difference between "what did I just install" and
// "here are the four things this does".
//
// Rules it keeps, so that it is a welcome and not an
// interruption:
//
//   * Once per VERSION, not per session, not per
//     project open. Stored against the version string,
//     so an upgrade shows it again exactly once - which
//     is the only honest way to tell somebody about a
//     feature that did not exist when they installed.
//
//   * Deferred until the Editor is idle. Opening a
//     window during a domain reload fights with
//     whatever else is loading, and on a large project
//     that lands the window behind the compile bar.
//
//   * Never during a batch run. If Unity is in batch
//     mode or in the middle of a build, nothing opens.
//
//   * No pitch. The promo panel is not on this screen.
//     Selling something to somebody in the first ten
//     seconds is how a tool gets uninstalled in the
//     next ten.
// ==========================================
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    internal sealed class DirectTMPWelcomeWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<DirectTMPWelcomeWindow>(true, DirectTMPConstants.ToolName, true);
            window.minSize = new Vector2(540, 560);
            window.maxSize = new Vector2(700, 820);
            window.Show();
        }

        private Vector2 _scroll;

        private void OnGUI()
        {
            DirectTMPBrand.Header(
                DirectTMPConstants.ToolName,
                DirectTMPText.L(
                    "Hand TextMeshPro the font file itself — and every language just works.",
                    "フォントファイルをそのまま TextMeshPro へ。どんな言語も、そのまま表示されます。",
                    "فایل فونت رو مستقیم بده به TextMeshPro — هر زبونی، همون‌جوری که هست."));

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(14);
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(4);

                    Card(
                        DirectTMPConstants.Mark,
                        DirectTMPText.L("Point at a font file, not a font asset",
                            "指定するのはフォントアセットではなく、フォントファイル",
                            "فایل فونت رو بده، نه فونت‌اسِت رو"),
                        DirectTMPText.Raw(
                            "Add a Direct Font next to any TextMeshPro label and drag a .ttf or .otf onto it. Glyphs are rasterized from the file the moment they are first drawn, so every character the font contains is available — no Font Asset Creator, no character ranges, no tofu.",
                            "TextMeshPro の隣に Direct Font を追加し、.ttf / .otf をドラッグするだけです。グリフは最初に描画された瞬間にファイルから生成されるため、そのフォントが持つ文字はすべてそのまま使えます。Font Asset Creator も文字範囲の指定も豆腐もありません。",
                            "کنار هر لیبل TextMeshPro یه Direct Font اضافه کن و یه .ttf یا .otf روش بکش. گلیف‌ها همون لحظه‌ی اولین رسم از فایل ساخته می‌شن، پس هر کاراکتری که فونت داره در دسترسه — نه Font Asset Creator، نه رنج کاراکتر، نه مربع خالی."),
                        null, null);

                    Card(
                        "\U0001F5C2",
                        DirectTMPText.L("The Font Catalog", "フォントカタログ", "کاتالوگ فونت"),
                        DirectTMPText.Raw(
                            "Every font in the project and on this machine, in one list, each one showing what it actually contains: the real glyph count and which writing systems it can set. Type your UI string once and see all of them render it.",
                            "プロジェクトと この PC のすべてのフォントを一覧し、実際のグリフ数と対応する文字体系を表示します。UI の文字列を一度入力すれば、すべてのフォントでの見た目を一度に確認できます。",
                            "همه‌ی فونت‌های پروژه و این سیستم توی یه لیست، هرکدوم با چیزی که واقعاً داره: تعداد واقعی گلیف و اینکه چه خط‌هایی رو می‌تونه بنویسه. متن UI ات رو یه بار تایپ کن و رندر همه‌شون رو ببین."),
                        DirectTMPText.L("Open the catalog", "カタログを開く", "کاتالوگ رو باز کن"),
                        DirectFontCatalogWindow.ShowWindow);

                    Card(
                        "\U0001F5A5",
                        DirectTMPText.L("It can restyle Unity itself", "Unity 自身にも効きます", "روی خود یونیتی هم اثر می‌ذاره"),
                        DirectTMPText.Raw(
                            "New in 1.0: point the Editor's own interface at a font too. If a GameObject named 敵スポーナー or a folder named فونت‌ها shows up as empty boxes in your Hierarchy, this is the fix. Nothing is written to your Unity installation and quitting always restores the default.",
                            "1.0 の新機能: Unity 自身の UI のフォントも変更できます。「敵スポーナー」という GameObject や「فونت‌ها」というフォルダーが Hierarchy で豆腐になる場合の解決策です。Unity のインストール先には何も書き込まず、終了すれば必ず既定に戻ります。",
                            "تازه در نسخه‌ی ۱.۰: فونت رابط خود یونیتی رو هم می‌تونی عوض کنی. اگه یه GameObject به اسم «敵スポーナー» یا پوشه‌ای به اسم «فونت‌ها» توی Hierarchy مربع خالی نشون داده می‌شه، راه‌حلش همینه. چیزی روی نصب یونیتی نوشته نمی‌شه و بستن برنامه همیشه همه‌چیز رو برمی‌گردونه."),
                        DirectTMPText.L("Change the Editor font", "エディタのフォントを変える", "فونت ادیتور رو عوض کن"),
                        DirectEditorFontWindow.ShowWindow);

                    Card(
                        "\U0001F517",
                        DirectTMPText.L("Fallback chains and batch conversion",
                            "フォールバックチェーンと一括変換",
                            "زنجیره‌ی فال‌بک و تبدیل گروهی"),
                        DirectTMPText.Raw(
                            "Line up a Latin UI font, a CJK font and a symbol font — the first one that has a given character supplies it. And when you already have three hundred labels using baked font assets, Convert ▸ Whole Project moves all of them over in one pass.",
                            "欧文 UI フォント・CJK フォント・記号フォントを並べれば、その文字を持つ最初のフォントが 1 文字ごとに選ばれます。既に焼き込み済みフォントを使うラベルが 300 個ある場合は、Convert ▸ Whole Project で一括移行できます。",
                            "یه فونت لاتین برای UI، یه فونت CJK و یه فونت نماد رو پشت سر هم بچین — برای هر کاراکتر، اولین فونتی که اون رو داشته باشه انتخاب می‌شه. و اگه از قبل سیصد تا لیبل با فونت‌اسِت پخته داری، Convert ▸ Whole Project همه‌شون رو یکجا جابه‌جا می‌کنه."),
                        null, null);

                    DirectTMPBrand.Divider();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(
                            DirectTMPText.L("Interface language", "表示言語", "زبان رابط"),
                            DirectTMPBrand.Line,
                            GUILayout.Width(140));

                        int index = DirectTMPText.IndexOf(DirectTMPText.Current);
                        int picked = DirectTMPPopup.Field(index, DirectTMPText.Labels, EditorStyles.popup);
                        if (picked != index)
                        {
                            DirectTMPText.Current = DirectTMPText.Codes[picked];
                            DirectTMPBrand.InvalidateStyles();
                            Repaint();
                        }
                    }

                    GUILayout.Space(6);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(DirectTMPText.L("Read the docs", "ドキュメントを読む", "راهنما رو بخون"), GUILayout.Height(26)))
                        {
                            Application.OpenURL(DirectTMPConstants.DocsUrl);
                        }
                        if (GUILayout.Button(DirectTMPText.L("Health check", "動作チェック", "بررسی سلامت"), GUILayout.Height(26)))
                        {
                            DirectTMPHealthCheckWindow.ShowWindow();
                        }
                        if (GUILayout.Button(DirectTMPText.Word("close"), GUILayout.Height(26)))
                        {
                            Close();
                        }
                    }
                }
                GUILayout.Space(14);
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(14);
                DirectTMPBrand.Footer();
                GUILayout.Space(14);
            }
            GUILayout.Space(6);
        }

        // One card, mirrored end to end when the interface reads right to
        // left: the mark leads, the title follows it, and the action button
        // sits under the far end of the paragraph. Drawing a Persian card
        // with its icon on the left is the same mistake as drawing an English
        // one with its icon on the right.
        //
        // `body` arrives in LOGICAL order - Raw(), not L() - because it is
        // about to be broken into lines, and a break point only means
        // anything before the text has been turned around for display.
        private static void Card(string mark, string title, string body, string actionLabel, System.Action action)
        {
            bool rtl = DirectTMPText.IsRightToLeft;

            using (new EditorGUILayout.VerticalScope(DirectTMPBrand.Card))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (rtl)
                    {
                        GUILayout.Label(title, DirectTMPBrand.BoldLine);
                        GUILayout.Label(mark, GUILayout.Width(26));
                    }
                    else
                    {
                        GUILayout.Label(mark, GUILayout.Width(26));
                        GUILayout.Label(title, DirectTMPBrand.BoldLine);
                    }
                }

                DirectTMPBrand.Body(body, CardInset);

                if (action != null && !string.IsNullOrEmpty(actionLabel))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (!rtl) { GUILayout.FlexibleSpace(); }
                        if (GUILayout.Button(actionLabel, EditorStyles.miniButton, GUILayout.Width(210)))
                        {
                            action();
                        }
                        if (rtl) { GUILayout.FlexibleSpace(); }
                    }
                }
            }
            GUILayout.Space(4);
        }

        // The window's own 14px gutters, the card's 11px padding and room for
        // a scrollbar. Measured rather than guessed, because a paragraph
        // wrapped one word too wide is re-wrapped by IMGUI, and IMGUI puts
        // the break in the wrong place in a right-to-left line.
        private const float CardInset = 14f + 14f + 11f + 11f + 18f;
    }

    /// <summary>
    /// Decides whether the welcome screen is due, and opens it if so. Split
    /// from the window itself so the rule can be read - and changed - without
    /// touching four hundred lines of layout.
    /// </summary>
    [InitializeOnLoad]
    internal static class DirectTMPFirstRun
    {
        static DirectTMPFirstRun()
        {
            // The console-quiet preference has to be restored on every domain
            // reload, because DirectTMPLog.Muted is a static field and a
            // static field is exactly what a domain reload throws away. Doing
            // it here rather than in a fourth [InitializeOnLoad] keeps the
            // count of things that run at load time down to the two that have
            // to.
            DirectTMPLog.Muted = DirectTMPPrefs.GetBool(DirectTMPPrefs.MuteLog, false);

            EditorApplication.delayCall += MaybeShow;
        }

        private static void MaybeShow()
        {
            if (!ShouldShow(
                    DirectTMPPrefs.GetString(DirectTMPPrefs.WelcomeShownForVersion, string.Empty),
                    DirectTMPConstants.Version,
                    Application.isBatchMode,
                    EditorApplication.isCompiling || EditorApplication.isUpdating))
            {
                return;
            }

            DirectTMPPrefs.SetString(DirectTMPPrefs.WelcomeShownForVersion, DirectTMPConstants.Version);
            DirectTMPWelcomeWindow.ShowWindow();
        }

        /// <summary>
        /// The rule, as a pure function so it can be tested: show it when this
        /// version has not been welcomed yet, and never while Unity is busy or
        /// running headless.
        /// </summary>
        internal static bool ShouldShow(string shownForVersion, string currentVersion, bool batchMode, bool busy)
        {
            if (batchMode || busy) { return false; }
            if (string.IsNullOrEmpty(currentVersion)) { return false; }
            return shownForVersion != currentVersion;
        }
    }
}
