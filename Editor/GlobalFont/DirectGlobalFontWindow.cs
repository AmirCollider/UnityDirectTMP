// ==========================================
// DirectGlobalFontWindow
// One screen, one font, the whole project.
//
// This is the window the package should always have
// opened with. Everything before it asked the user to
// understand the design first - which component goes
// on which label, what a font asset is, why the one
// they baked has no Persian in it. This asks for a
// .ttf.
//
// The order of the screen is the order of the
// questions:
//
//   1. Which font?          The only required answer.
//   2. Is it working?       Stated in a sentence, with
//                           the count of labels it is
//                           driving, because "I ticked
//                           it and I cannot tell" is
//                           the next report otherwise.
//   3. Anything wrong?      The three project-level
//                           faults that make this
//                           package do nothing, each
//                           with the button that fixes
//                           it. This panel is the whole
//                           answer to "it works in my
//                           other project".
//   4. How far does it go?  The switches, below the
//                           fold, defaulted so that
//                           nobody has to read them.
// ==========================================
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    internal sealed class DirectGlobalFontWindow : EditorWindow
    {
        // The window's two 10px gutters, the card's 11px padding and room for
        // a scrollbar - the width a paragraph inside a card actually gets.
        private const float CardInset = 10f + 10f + 11f + 11f + 18f;

        private Vector2 _scroll;
        private SerializedObject _serialized;

        public static void ShowWindow()
        {
            var window = GetWindow<DirectGlobalFontWindow>(false, DirectTMPConstants.ShortName + " · Global Font", true);
            window.minSize = new Vector2(470, 560);
            window.Show();
        }

        private void OnEnable()
        {
            Rebind();
        }

        private void Rebind()
        {
            DirectGlobalFontConfig config = DirectGlobalFont.Config;
            _serialized = config != null ? new SerializedObject(config) : null;
        }

        private void OnGUI()
        {
            DirectTMPBrand.Header(
                DirectTMPText.L("Global Font", "プロジェクト全体のフォント", "فونت سراسری پروژه"),
                DirectTMPText.L(
                    "One font file, every TextMeshPro label in the project. No component on anything.",
                    "フォントファイルを 1 つ選ぶだけで、プロジェクト内のすべての TextMeshPro ラベルに適用されます。コンポーネントは不要です。",
                    "یک فایل فونت، برای همه‌ی متن‌های TextMeshPro پروژه. بدون اضافه کردن هیچ کامپوننتی."));

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                using (new EditorGUILayout.VerticalScope())
                {
                    DirectGlobalFontConfig config = DirectGlobalFont.Config;

                    if (config == null) { DrawNoConfig(); }
                    else { DrawConfigured(config); }

                    DrawDiagnostics(config);
                }
                GUILayout.Space(10);
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                using (new EditorGUILayout.VerticalScope())
                {
                    DirectTMPBrand.Footer();
                }
                GUILayout.Space(10);
            }
            GUILayout.Space(6);
        }

        // ==========================================
        // Nothing set up yet
        // ==========================================
        private void DrawNoConfig()
        {
            using (new EditorGUILayout.VerticalScope(DirectTMPBrand.Card))
            {
                DirectTMPBrand.Body(
                    "This project has no Unity DirectTMP settings asset yet. One click creates it — a single small asset in Assets/Resources, which is what lets the setting survive a domain reload, a scene reload and a build without anything being written into your scenes.",
                    "このプロジェクトにはまだ Unity DirectTMP の設定アセットがありません。ボタン 1 つで Assets/Resources に小さなアセットを作成します。これにより、シーンに何も書き込むことなく、ドメインリロード・シーンの再読み込み・ビルドをまたいで設定が保持されます。",
                    "این پروژه هنوز فایل تنظیمات Unity DirectTMP رو نداره. با یک کلیک ساخته می‌شه — یک اسست کوچیک توی Assets/Resources، و همین باعث می‌شه تنظیمات بعد از ریلود دامین، ریلود سین و توی بیلد هم بمونه، بدون اینکه چیزی داخل سین‌های تو نوشته بشه.",
                    CardInset);

                GUILayout.Space(6);
                if (GUILayout.Button(DirectTMPText.L("Create the settings asset", "設定アセットを作成", "ساختن فایل تنظیمات"), GUILayout.Height(26)))
                {
                    DirectGlobalFontBootstrap.EnsureConfigAsset();
                    Rebind();
                }
            }
        }

        // ==========================================
        // The one required answer, then the verdict
        // ==========================================
        private void DrawConfigured(DirectGlobalFontConfig config)
        {
            // Rebound whenever the asset behind it changed - a settings asset
            // created a moment ago, or one reimported by version control.
            // A SerializedObject over a destroyed target throws on use.
            if (_serialized == null || !ReferenceEquals(_serialized.targetObject, config))
            {
                _serialized = new SerializedObject(config);
            }

            _serialized.Update();

            DirectTMPBrand.SectionTitle(DirectTMPText.L("The font", "フォント", "فونت"));

            using (new EditorGUILayout.VerticalScope(DirectTMPBrand.Card))
            {
                EditorGUI.BeginChangeCheck();

                var font = (Font)EditorGUILayout.ObjectField(
                    DirectTMPText.C(
                        "Font file", "フォントファイル", "فایل فونت",
                        "A plain .ttf or .otf from your project. Not a font asset.",
                        "プロジェクト内の .ttf / .otf ファイル。フォントアセットではありません。",
                        "یک فایل .ttf یا .otf ساده از پروژه‌ت. نه Font Asset."),
                    config.FontFile, typeof(Font), false);

                bool active = EditorGUILayout.ToggleLeft(
                    DirectTMPText.C(
                        "Use it everywhere", "プロジェクト全体で使う", "همه‌جا از همین استفاده کن",
                        "Turn the whole project-wide override on or off.",
                        "プロジェクト全体の上書きを有効/無効にします。",
                        "کل بازنویسی سراسری پروژه رو روشن یا خاموش می‌کنه."),
                    config.Active);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Unity DirectTMP Global Font");
                    config.FontFile = font;
                    config.Active = active;
                    DirectGlobalFontBootstrap.SaveAndApply(config);
                    Rebind();
                }

                GUILayout.Space(4);

                SerializedProperty more = _serialized.FindProperty("moreFonts");
                if (more != null)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(more, DirectTMPText.C(
                        "More fonts", "追加のフォント", "فونت‌های بیشتر",
                        "Tried in order after the one above. The first font that has a character supplies it.",
                        "上のフォントの後に順番に試されます。その文字を持つ最初のフォントが使われます。",
                        "به ترتیب بعد از فونت بالا امتحان می‌شن. اولین فونتی که کاراکتر رو داشته باشه همون رو می‌ده."), true);

                    if (EditorGUI.EndChangeCheck())
                    {
                        _serialized.ApplyModifiedProperties();
                        DirectGlobalFontBootstrap.SaveAndApply(config);
                    }
                }
            }

            DrawVerdict(config);
            DrawReach(config);
        }

        // ==========================================
        // Is it working?
        //
        // A count, not a tick. "Applied" is a claim; "43
        // labels" is a fact somebody can check against
        // their own Hierarchy, and it is the difference
        // between believing the window and re-installing
        // the package.
        // ==========================================
        private void DrawVerdict(DirectGlobalFontConfig config)
        {
            GUILayout.Space(6);

            if (!config.IsActive)
            {
                DirectTMPBrand.Note(
                    "Off. Every label keeps the font asset it was built with.",
                    "オフです。各ラベルはビルド時のフォントアセットのままです。",
                    "خاموشه. هر متن همون Font Asset خودش رو نگه می‌داره.",
                    DirectTMPBrand.NoteLevel.Plain);
                return;
            }

            if (!DirectGlobalFont.IsActive)
            {
                string problem = DirectGlobalFont.LastProblem;
                DirectTMPBrand.Note(
                    string.IsNullOrEmpty(problem)
                        ? DirectTMPText.Raw(
                            "Configured, but not applied yet.",
                            "設定済みですが、まだ適用されていません。",
                            "تنظیم شده، ولی هنوز اعمال نشده.")
                        : problem,
                    DirectTMPBrand.NoteLevel.Error);

                GUILayout.Space(4);
                if (GUILayout.Button(DirectTMPText.L("Apply now", "今すぐ適用", "همین حالا اعمال کن"), GUILayout.Height(24)))
                {
                    DirectGlobalFontBootstrap.ApplyNow();
                }
                return;
            }

            // Composed in LOGICAL order and handed to Note in logical order:
            // Note prepares what it is given, and text prepared twice comes
            // back reversed into nonsense.
            string applied = string.Format(
                DirectTMPText.Raw(
                    "Applied. {0} label(s) are being drawn from this file, and {1} are being joined and put in reading order. Nothing was written into any scene.",
                    "適用中です。{0} 個のラベルがこのファイルから描画され、{1} 個が結合・並べ替えされています。シーンには何も書き込まれていません。",
                    "اعمال شد. {0} متن از همین فایل کشیده می‌شه و {1} تا هم چسبیده و به ترتیب درست چیده می‌شه. هیچ‌چیزی داخل سین نوشته نشده."),
                DirectGlobalFont.LabelsUsingFont,
                DirectGlobalFont.LabelsShaped);

            DirectTMPBrand.Note(applied, DirectTMPBrand.NoteLevel.Info);

            GUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(DirectTMPText.L("Re-apply", "再適用", "دوباره اعمال کن"), GUILayout.Height(24)))
                {
                    DirectGlobalFontBootstrap.ApplyNow();
                }

                if (GUILayout.Button(DirectTMPText.L("Turn off", "オフにする", "خاموشش کن"), GUILayout.Height(24)))
                {
                    Undo.RecordObject(config, "Unity DirectTMP Global Font");
                    config.Active = false;
                    DirectGlobalFont.Revert();
                    DirectGlobalFontBootstrap.SaveAndApply(config);
                }
            }
        }

        // ==========================================
        // How far it reaches
        // ==========================================
        private void DrawReach(DirectGlobalFontConfig config)
        {
            GUILayout.Space(8);
            DirectTMPBrand.SectionTitle(DirectTMPText.L("How far it reaches", "適用範囲", "تا کجا اعمال بشه"));

            using (new EditorGUILayout.VerticalScope(DirectTMPBrand.Card))
            {
                EditorGUI.BeginChangeCheck();

                bool replace = EditorGUILayout.ToggleLeft(
                    DirectTMPText.C(
                        "Draw every label in this font", "すべてのラベルをこのフォントで描画", "همه‌ی متن‌ها با این فونت کشیده بشن",
                        "Replaces each label's own font asset at load. Nothing is saved into the scene.",
                        "読み込み時に各ラベルのフォントアセットを置き換えます。シーンには保存されません。",
                        "موقع لود، Font Asset هر متن رو عوض می‌کنه. چیزی توی سین ذخیره نمی‌شه."),
                    config.ReplaceLabelFonts);

                bool fallback = EditorGUILayout.ToggleLeft(
                    DirectTMPText.C(
                        "Never show □□□ anywhere", "どこでも □□□ を出さない", "هیچ‌جا □□□ نشون داده نشه",
                        "Registers the font in TextMeshPro's own project-wide fallback list, so any label anywhere — including ones this window never swept — finds the characters its own font lacks.",
                        "TextMeshPro のプロジェクト全体のフォールバックリストに登録します。この画面が触れていないラベルでも、自分のフォントにない文字を補えます。",
                        "فونت رو توی لیست fallback سراسری خود TextMeshPro ثبت می‌کنه، طوری که هر متنی — حتی متنی که این پنجره بهش دست نزده — کاراکترهای نداشته‌ی فونت خودش رو از این پیدا کنه."),
                    config.GlobalFallback);

                bool shape = EditorGUILayout.ToggleLeft(
                    DirectTMPText.C(
                        "Join and reorder right-to-left text", "右から左のテキストを結合・並べ替え", "متن راست‌به‌چپ رو بچسبون و مرتب کن",
                        "Persian, Arabic, Urdu and Hebrew, on every label, with no component added.",
                        "ペルシャ語・アラビア語・ウルドゥー語・ヘブライ語を、コンポーネントなしですべてのラベルに適用します。",
                        "فارسی، عربی، اردو و عبری — روی همه‌ی متن‌ها، بدون اضافه کردن کامپوننت."),
                    config.ShapeRightToLeft);

                bool editMode = EditorGUILayout.ToggleLeft(
                    DirectTMPText.C(
                        "Apply while editing", "編集中も適用", "موقع ادیت هم اعمال بشه",
                        "So the Scene and Game views show what the build will show.",
                        "Scene / Game ビューがビルドと同じ見た目になります。",
                        "که Scene و Game ویو همون چیزی رو نشون بدن که بیلد نشون می‌ده."),
                    config.ApplyInEditMode);

                bool watch = EditorGUILayout.ToggleLeft(
                    DirectTMPText.C(
                        "Catch labels created at runtime", "実行時に生成されたラベルも拾う", "متن‌هایی که موقع اجرا ساخته می‌شن هم گرفته بشن",
                        "Re-sweeps about once a second. Off is cheaper — call DirectTMP.RefreshGlobalFont() after instantiating instead.",
                        "約 1 秒ごとに再スキャンします。オフの方が軽量です。代わりに生成後 DirectTMP.RefreshGlobalFont() を呼んでください。",
                        "تقریباً هر ثانیه یک بار دوباره اسکن می‌کنه. خاموشش سبک‌تره — به‌جاش بعد از Instantiate خودت DirectTMP.RefreshGlobalFont() رو صدا بزن."),
                    config.WatchForNewLabels);

                GUILayout.Space(4);

                bool chrome = EditorGUILayout.ToggleLeft(
                    DirectTMPText.C(
                        "Draw Unity's own interface in it too", "Unity 自身の UI にも適用", "خود رابط یونیتی هم با همین کشیده بشه",
                        "Menu bar, Hierarchy, Inspector, Project window, Console. Nothing is written to your Unity installation and quitting Unity always restores it.",
                        "メニューバー・Hierarchy・Inspector・Project・Console。Unity のインストール先には何も書き込まれず、Unity を終了すれば必ず元に戻ります。",
                        "منوبار، Hierarchy، Inspector، پنجره‌ی Project و Console. هیچ‌چیزی روی نصب یونیتی نوشته نمی‌شه و بستن یونیتی همیشه همه‌چیز رو برمی‌گردونه."),
                    config.RestyleEditorChrome);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Unity DirectTMP Global Font");
                    config.ReplaceLabelFonts = replace;
                    config.GlobalFallback = fallback;
                    config.ShapeRightToLeft = shape;
                    config.ApplyInEditMode = editMode;
                    config.WatchForNewLabels = watch;
                    config.RestyleEditorChrome = chrome;
                    DirectGlobalFontBootstrap.SaveAndApply(config);
                }
            }
        }

        // ==========================================
        // "It works in my other project"
        //
        // Four project-level facts decide whether ANY of
        // this can work, none of them is visible from a
        // scene, and every one of them can differ between
        // two projects that look identical. Each is
        // stated here with the button that fixes it.
        // ==========================================
        private void DrawDiagnostics(DirectGlobalFontConfig config)
        {
            GUILayout.Space(10);
            DirectTMPBrand.SectionTitle(DirectTMPText.L("This project", "このプロジェクト", "این پروژه"));

            using (new EditorGUILayout.VerticalScope(DirectTMPBrand.Card))
            {
                bool clean = true;

                if (!DirectFontDiagnostics.TmpResourcesImported)
                {
                    clean = false;
                    DirectTMPBrand.Body(
                        "TextMeshPro's Essential Resources are not imported. Without them there is no TMP Settings asset and no material for a generated font to sit on, so nothing this package builds can draw. This is the usual reason it works in one project and does nothing in another.",
                        "TextMeshPro の Essential Resources がインポートされていません。TMP Settings アセットも、生成されたフォントが使うマテリアルも存在しないため、このパッケージが作るものは描画できません。「別のプロジェクトでは動くのに」の典型的な原因です。",
                        "منابع Essential Resources مربوط به TextMeshPro ایمپورت نشدن. بدون اون‌ها نه فایل TMP Settings هست نه متریالی که فونت ساخته‌شده روش بشینه، پس هیچ‌چیزی که این پکیج می‌سازه کشیده نمی‌شه. این معمول‌ترین دلیل «توی یک پروژه کار می‌کنه، توی اون یکی نه» هست.",
                        CardInset);

                    GUILayout.Space(4);
                    if (GUILayout.Button(DirectTMPText.L("Import TMP Essential Resources", "TMP Essential Resources をインポート", "ایمپورت کردن TMP Essential Resources")))
                    {
                        EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
                    }
                    GUILayout.Space(6);
                }
                else if (DirectFontDiagnostics.FindDistanceFieldShader() == null)
                {
                    clean = false;
                    DirectTMPBrand.Body(
                        "TextMeshPro's distance-field shader could not be found. Every font this package builds needs it, and a material without it draws nothing. Re-import the TMP Essential Resources.",
                        "TextMeshPro の Distance Field シェーダーが見つかりません。このパッケージが作るすべてのフォントに必要で、これがないマテリアルは何も描画しません。TMP Essential Resources を再インポートしてください。",
                        "شیدر Distance Field مربوط به TextMeshPro پیدا نشد. هر فونتی که این پکیج می‌سازه بهش نیاز داره و متریالی که نداشته باشدش هیچی نمی‌کشه. دوباره TMP Essential Resources رو ایمپورت کن.",
                        CardInset);
                    GUILayout.Space(6);
                }

                if (config != null && config.FontFile != null)
                {
                    string path = AssetDatabase.GetAssetPath(config.FontFile);
                    var importer = AssetImporter.GetAtPath(path) as TrueTypeFontImporter;

                    if (importer != null && (!importer.includeFontData || importer.fontTextureCase != FontTextureCase.Dynamic))
                    {
                        clean = false;
                        DirectTMPBrand.Body(
                            "This font is imported with settings that stop TextMeshPro reading it — \"Include Font Data\" off, or a baked character set instead of Dynamic. Those settings live in the .meta file beside the font, not in the font, which is exactly why the same .ttf behaves differently in two projects.",
                            "このフォントは TextMeshPro が読めない設定でインポートされています（Include Font Data がオフ、または Dynamic ではなく固定文字セット）。この設定はフォント本体ではなく隣の .meta ファイルにあります。同じ .ttf がプロジェクトによって挙動を変えるのはこのためです。",
                            "این فونت با تنظیماتی ایمپورت شده که TextMeshPro نمی‌تونه بخونتش — یا «Include Font Data» خاموشه یا به‌جای Dynamic یک مجموعه‌ی کاراکتر ثابت داره. این تنظیمات توی فایل .meta کنار فونت ذخیره می‌شن، نه توی خود فونت — و دقیقاً برای همینه که همون .ttf توی دو پروژه دو جور رفتار می‌کنه.",
                            CardInset);

                        GUILayout.Space(4);
                        if (GUILayout.Button(DirectTMPText.L("Fix the import settings", "インポート設定を修正", "درست کردن تنظیمات ایمپورت")))
                        {
                            DirectGlobalFontBootstrap.RepairImport(path);
                            DirectGlobalFontBootstrap.ApplyNow();
                        }
                        GUILayout.Space(6);
                    }
                }

                if (clean)
                {
                    DirectTMPBrand.Body(
                        "TextMeshPro's resources are present, its shader resolves, and the font is imported the way it needs to be. Nothing here can stop the package working.",
                        "TextMeshPro のリソースがあり、シェーダーも解決でき、フォントのインポート設定も適切です。ここが原因で動かないことはありません。",
                        "منابع TextMeshPro موجوده، شیدرش پیدا می‌شه و فونت هم درست ایمپورت شده. از این سمت چیزی جلوی کار پکیج رو نمی‌گیره.",
                        CardInset);
                }
            }
        }
    }
}
