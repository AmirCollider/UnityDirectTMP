// ==========================================
// DirectTMPSettingsProvider
// The "Settings…" screen, living in
// Project Settings ▸ Unity DirectTMP.
//
// Four sections, in the order somebody looks for
// them:
//
//   Rasterization   the numbers that decide how a font
//                   file becomes an atlas. Every one
//                   has a tooltip saying what it costs
//                   as well as what it does, because
//                   "Atlas Width" with no explanation
//                   is a slider people either never
//                   touch or set to 8192.
//   Editor font     a read-out and one button. The
//                   real controls live in their own
//                   window; duplicating them here would
//                   be two places to change one thing.
//   Housekeeping    the cache, the log, and the
//                   dismissed dialogs.
//   About           version, links, and the catalogue.
//
// The Settings… menu item just opens this page.
// ==========================================
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace UnityDirectTMP.Editor
{
    internal static class DirectTMPSettingsProvider
    {
        private static readonly int[] AtlasSizes = { 256, 512, 1024, 2048, 4096, 8192 };
        private static readonly string[] AtlasSizeLabels = { "256", "512", "1024", "2048", "4096", "8192" };

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new SettingsProvider(DirectTMPEditorConstants.SettingsPath, SettingsScope.Project)
            {
                label = "Unity DirectTMP",
                guiHandler = _ => DrawGUI(),

                // Searching Project Settings for "font", "persian" or "tofu"
                // should land here. The keyword list is the only thing that
                // makes a settings page findable by somebody who does not
                // already know the tool's name.
                keywords = new HashSet<string>(new[]
                {
                    "font", "fonts", "tmp", "textmeshpro", "sdf", "atlas", "direct", "directtmp",
                    "ttf", "otf", "glyph", "glyphs", "fallback", "localization", "localisation",
                    "cjk", "japanese", "persian", "arabic", "rtl", "unicode", "tofu", "editor font",
                    "amircollider", "inkwell"
                })
            };
        }

        private static void DrawGUI()
        {
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawBody();
                }
                GUILayout.Space(8);
            }
        }

        private static void DrawBody()
        {
            DrawLanguageRow();

            // ==========================================
            // Rasterization
            // ==========================================
            DirectTMPBrand.SectionTitle(DirectTMPText.L(
                "Default rasterization", "既定のラスタライズ設定", "تنظیمات پیش‌فرض رسترایز"));

            DirectTMPBrand.Note(
                "These are stamped onto the Direct Fonts the batch converter creates, and used as the starting point when you add a Direct Font by hand. A label can still override them for itself.",
                "一括変換で作成される Direct Font に適用され、手動で追加したときの初期値にもなります。ラベルごとに個別に上書きすることもできます。",
                "این‌ها روی Direct Font هایی که تبدیل‌کننده‌ی گروهی می‌سازه حک می‌شن، و وقتی دستی یه Direct Font اضافه می‌کنی نقطه‌ی شروع هستن. هر لیبل می‌تونه برای خودش این‌ها رو بازنویسی کنه.",
                DirectTMPBrand.NoteLevel.Plain);

            DirectFontSettings current = DirectTMPSettings.CurrentSettings;

            EditorGUI.BeginChangeCheck();

            int size = EditorGUILayout.IntSlider(
                DirectTMPText.C(
                    "Sampling Point Size", "サンプリングサイズ", "سایز نمونه‌برداری",
                    "The point size glyphs are rasterized at. Higher stays crisp when text is scaled up, and fills the atlas faster — a page of CJK at 120 needs several atlas textures where 90 needs one.",
                    "グリフをラスタライズする際のポイントサイズ。大きいほど拡大時に鮮明ですが、アトラスの消費も早くなります。CJK 1 ページ分は 90 なら 1 枚、120 では複数枚のアトラスが必要になります。",
                    "سایز پوینتی که گلیف‌ها باهاش رسترایز می‌شن. بزرگ‌تر یعنی موقع بزرگ‌نمایی تیزتر، ولی اطلس زودتر پر می‌شه — یه صفحه متن CJK با ۹۰ توی یه اطلس جا می‌شه و با ۱۲۰ چند تا لازم داره."),
                current.SamplingPointSize,
                DirectTMPConstants.MinSamplingPointSize,
                DirectTMPConstants.MaxSamplingPointSize);

            int padding = EditorGUILayout.IntSlider(
                DirectTMPText.C(
                    "Atlas Padding", "アトラスの余白", "فاصله‌ی اطلس",
                    "Spacing baked around each glyph so neighbouring SDF spreads never bleed together. Too low shows as faint ghosting around outlined text.",
                    "各グリフの周囲に確保する余白。隣接するグリフの SDF がにじみ合わないようにします。小さすぎると、アウトライン付き文字の周囲にゴーストが見えます。",
                    "فاصله‌ای که دور هر گلیف پخته می‌شه تا SDF گلیف‌های کناری توی هم نره. خیلی کم که باشه، دور متن‌های Outline دار یه سایه‌ی کم‌رنگ می‌بینی."),
                current.AtlasPadding, 0, DirectTMPConstants.MaxAtlasPadding);

            int width = AtlasSizeField(
                DirectTMPText.C(
                    "Atlas Width", "アトラス幅", "عرض اطلس",
                    "Width of one atlas texture. Glyphs spill into a new texture when one fills up, so this is a memory-granularity choice, not a limit on how many glyphs you can have.",
                    "アトラステクスチャ 1 枚の幅。1 枚が埋まると次のテクスチャに続くため、これはグリフ数の上限ではなくメモリ確保の粒度の設定です。",
                    "عرض یه تکسچر اطلس. وقتی یکی پر بشه گلیف‌ها می‌رن روی تکسچر بعدی، پس این سقفِ تعداد گلیف نیست، فقط دانه‌بندی مصرف حافظه‌ست."),
                current.AtlasWidth);

            int height = AtlasSizeField(
                DirectTMPText.C("Atlas Height", "アトラス高さ", "ارتفاع اطلس",
                    "Height of one atlas texture.",
                    "アトラステクスチャ 1 枚の高さ。",
                    "ارتفاع یه تکسچر اطلس."),
                current.AtlasHeight);

            var mode = (GlyphRenderMode)EditorGUILayout.EnumPopup(
                DirectTMPText.C(
                    "Render Mode", "レンダーモード", "حالت رندر",
                    "SDFAA is TextMeshPro's crisp, scalable default and what you want unless you have a specific reason otherwise.",
                    "SDFAA は TextMeshPro の既定で、拡大に強い鮮明なモードです。特別な理由がなければこのままで構いません。",
                    "SDFAA پیش‌فرض تیز و مقیاس‌پذیر TextMeshPro هست و مگه دلیل خاصی داشته باشی، همون رو بذار."),
                current.RenderMode);

            if (EditorGUI.EndChangeCheck())
            {
                DirectTMPSettings.CurrentSettings = new DirectFontSettings(size, padding, width, height, mode);
            }

            // ==========================================
            // Editor font
            // ==========================================
            EditorGUILayout.Space(6);
            DirectTMPBrand.SectionTitle(DirectTMPText.L(
                "Unity's own interface", "Unity 自身の UI", "رابط خود یونیتی"));

            DirectEditorFontPlan plan = DirectEditorFontSettings.Plan;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    DirectTMPText.L("Editor font", "エディタのフォント", "فونت ادیتور"),
                    DirectTMPBrand.Line,
                    GUILayout.Width(EditorGUIUtility.labelWidth));
                EditorGUILayout.LabelField(DirectTMPText.Show(plan.Describe()), DirectTMPBrand.BoldLine);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(DirectTMPText.L("Choose a font…", "フォントを選ぶ…", "یه فونت انتخاب کن…")))
                {
                    DirectEditorFontWindow.ShowWindow();
                }
                using (new EditorGUI.DisabledScope(!DirectEditorFont.IsActive))
                {
                    if (GUILayout.Button(DirectTMPText.L("Revert to Unity's font", "Unity のフォントに戻す", "برگشت به فونت یونیتی")))
                    {
                        DirectEditorFontSettings.Clear();
                    }
                }
            }

            // ==========================================
            // Housekeeping
            // ==========================================
            EditorGUILayout.Space(8);
            DirectTMPBrand.SectionTitle(DirectTMPText.L("Housekeeping", "メンテナンス", "نگهداری"));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(DirectTMPText.L("Show cache folder", "キャッシュフォルダーを開く", "پوشه‌ی کش رو نشون بده")))
                {
                    string dir = DirectTMPSettings.ResolveRuntimeCacheFolderAbsolute();
                    System.IO.Directory.CreateDirectory(dir);
                    EditorUtility.RevealInFinder(dir);
                }
                if (GUILayout.Button(DirectTMPText.L("Clear font cache", "フォントキャッシュを消去", "پاک کردن کش فونت")))
                {
                    int released = DirectTMP.ClearCache();
                    DirectTMPLog.Info($"Cleared {released} cached atlas(es).");
                }
            }

            bool muted = DirectTMPPrefs.GetBool(DirectTMPPrefs.MuteLog, false);
            bool nowMuted = EditorGUILayout.Toggle(
                DirectTMPText.C(
                    "Quiet console", "コンソールを静かに", "کنسول ساکت",
                    "Drops this package's informational and warning messages. Errors always get through — a font that failed to load is something you need to be told about.",
                    "このパッケージの情報・警告メッセージを抑制します。エラーは常に表示されます(フォントの読み込み失敗は必ず知る必要があるため)。",
                    "پیام‌های اطلاعی و هشدار این پکیج رو خاموش می‌کنه. خطاها همیشه رد می‌شن — فونتی که لود نشده چیزیه که باید بهت گفته بشه."),
                muted);

            if (nowMuted != muted)
            {
                DirectTMPPrefs.SetBool(DirectTMPPrefs.MuteLog, nowMuted);
                DirectTMPLog.Muted = nowMuted;
            }

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(DirectTMPText.L("Reset rasterization defaults", "ラスタライズ設定を初期化", "بازگرداندن پیش‌فرض‌های رسترایز")))
                {
                    DirectTMPSettings.ResetToDefaults();
                }
                if (GUILayout.Button(DirectTMPText.L("Bring back dismissed tips", "非表示にしたヒントを戻す", "برگرداندن راهنماهای پنهان‌شده")))
                {
                    DirectTMPPrefs.ResetAll();
                }
            }

            // ==========================================
            // About
            // ==========================================
            EditorGUILayout.Space(10);
            DirectTMPBrand.Divider(2f, 6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(DirectTMPText.L("Font Catalog", "フォントカタログ", "کاتالوگ فونت"), EditorStyles.miniButtonLeft))
                {
                    DirectFontCatalogWindow.ShowWindow();
                }
                if (GUILayout.Button(DirectTMPText.L("Health check", "動作チェック", "بررسی سلامت"), EditorStyles.miniButtonMid))
                {
                    DirectTMPHealthCheckWindow.ShowWindow();
                }
                if (GUILayout.Button(DirectTMPText.L("Docs", "ドキュメント", "راهنما"), EditorStyles.miniButtonMid))
                {
                    Application.OpenURL(DirectTMPConstants.DocsUrl);
                }
                if (GUILayout.Button(DirectTMPText.L("About", "このツールについて", "درباره"), EditorStyles.miniButtonRight))
                {
                    DirectTMPAboutWindow.ShowWindow();
                }
            }

            EditorGUILayout.Space(6);
            DirectTMPPromo.DrawCatalogLink();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(
                $"{DirectTMPConstants.Mark} {DirectTMPConstants.ToolName}  v{DirectTMPConstants.Version}  ·  {DirectTMPConstants.ThemeName}",
                EditorStyles.centeredGreyMiniLabel);
        }

        private static void DrawLanguageRow()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    DirectTMPText.Word("language"),
                    DirectTMPBrand.Line,
                    GUILayout.Width(EditorGUIUtility.labelWidth));

                int index = DirectTMPText.IndexOf(DirectTMPText.Current);
                int picked = EditorGUILayout.Popup(index, DirectTMPText.Labels, GUILayout.Width(140));
                if (picked != index)
                {
                    DirectTMPText.Current = DirectTMPText.Codes[picked];
                    DirectTMPBrand.InvalidateStyles();
                }

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.Space(4);
        }

        private static int AtlasSizeField(GUIContent label, int value)
        {
            int index = System.Array.IndexOf(AtlasSizes, Mathf.ClosestPowerOfTwo(value));
            if (index < 0) { index = 2; } // default to 1024
            int picked = EditorGUILayout.Popup(label, index, AtlasSizeLabels);
            return AtlasSizes[Mathf.Clamp(picked, 0, AtlasSizes.Length - 1)];
        }
    }
}
