// ==========================================
// DirectEditorFontWindow
// The screen where somebody changes the font Unity
// itself is drawn in.
//
// The layout answers, top to bottom, the three
// questions a person actually has - and in that order,
// because getting them out of order is how this
// feature becomes frightening:
//
//   1. What will it look like?   The preview is above
//      the Apply button, and it draws in the real font
//      at the real size, in every script the font
//      covers. Nobody should have to apply a change to
//      their entire IDE to find out what it does.
//
//   2. Will it break anything?   The verdict line sits
//      between the preview and the button. A font that
//      cannot draw the Editor is refused there, in
//      words, before the button is reachable.
//
//   3. How do I undo it?         Said twice: a Revert
//      button next to Apply, and a line stating that
//      quitting Unity restores everything no matter
//      what. That sentence is the reason people are
//      willing to press Apply at all.
// ==========================================
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    internal sealed class DirectEditorFontWindow : EditorWindow
    {
        // The window's two 10px gutters, the card's 11px padding and room for
        // a scrollbar - the width a paragraph inside a card actually gets.
        private const float CardInset = 10f + 10f + 11f + 11f + 18f;

        private DirectEditorFontPlan _plan;
        private Vector2 _scroll;

        private string[] _families = new string[0];
        private string _familyFilter = string.Empty;
        private Vector2 _familyScroll;

        private DirectFontFileInfo _coverage;
        private string _coverageKey;

        private GUIStyle _previewStyle;
        private Font _previewFont;

        public static void ShowWindow()
        {
            var window = GetWindow<DirectEditorFontWindow>(false, DirectTMPConstants.ShortName + " · Editor Font", true);
            window.minSize = new Vector2(470, 560);
            window.Show();
        }

        private void OnEnable()
        {
            _plan = DirectEditorFontSettings.Plan;
            _families = DirectEditorFont.InstalledFamilies();
        }

        private void OnDisable()
        {
            _previewStyle = null;
            _previewFont = null;

            // The preview font for an OS family is a dynamic Font this window
            // created. It is HideAndDontSave, so nothing else will ever clean
            // it up - closing the window has to.
            if (_previewSystemFont != null)
            {
                DestroyImmediate(_previewSystemFont);
                _previewSystemFont = null;
                _previewSystemFamily = null;
            }
        }

        private void OnGUI()
        {
            DirectTMPBrand.Header(
                DirectTMPText.L("Editor Font", "エディタのフォント", "فونت خود یونیتی"),
                DirectTMPText.L(
                    "Change the font Unity's own interface is drawn in — menus, Hierarchy, Inspector, Console.",
                    "Unity 自身の UI(メニュー・Hierarchy・Inspector・Console)を描画するフォントを変更します。",
                    "فونتی که خود رابط یونیتی با اون کشیده می‌شه رو عوض کن — منوها، Hierarchy، Inspector، Console."));

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawWhy();
                    DrawSource();
                    DrawSize();
                    DrawCoverage();
                    DrawPreview();
                    DrawVerdictAndActions();
                    DrawSafetyNote();
                }
                GUILayout.Space(10);
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                using (new EditorGUILayout.VerticalScope())
                {
                    DirectTMPPromo.Draw();
                    DirectTMPBrand.Footer();
                }
                GUILayout.Space(10);
            }
            GUILayout.Space(6);
        }

        // ==========================================
        // Why this exists.
        //
        // One paragraph, shown once and dismissible. A
        // feature that changes the user's whole IDE has
        // earned three sentences explaining itself; a
        // feature that repeats them every visit has not.
        // ==========================================
        private void DrawWhy()
        {
            if (DirectTMPPrefs.GetBool(DirectTMPPrefs.HideEditorFontIntro, false)) { return; }

            using (new EditorGUILayout.VerticalScope(DirectTMPBrand.Card))
            {
                DirectTMPBrand.Body(
                    "Unity draws its own interface with its own font, which is why a GameObject named 敵スポーナー or a folder named فونت‌ها shows up as empty boxes in the Hierarchy. The asset is fine; the font Unity is drawing it with is not. Point the Editor at a font that covers the script and the boxes become letters.",
                    "Unity は自身の UI を Unity のフォントで描画します。そのため「敵スポーナー」という GameObject や「فونت‌ها」というフォルダーは Hierarchy で豆腐になります。アセットは正常で、描画に使われているフォントが対応していないだけです。その文字体系を含むフォントに切り替えれば、そのまま読めるようになります。",
                    "یونیتی رابط خودش رو با فونت خودش می‌کشه؛ برای همین یه GameObject به اسم «敵スポーナー» یا یه پوشه به اسم «فونت‌ها» توی Hierarchy مربع خالی نشون داده می‌شه. اسست سالمه، فونتی که باهاش کشیده می‌شه سالم نیست. اگه فونت خود ادیتور رو بذاری روی فونتی که اون خط رو داره، مربع‌ها تبدیل به حروف می‌شن.",
                    CardInset);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(DirectTMPText.L("Got it", "了解", "فهمیدم"), EditorStyles.miniButton, GUILayout.Width(90)))
                    {
                        DirectTMPPrefs.SetBool(DirectTMPPrefs.HideEditorFontIntro, true);
                    }
                }
            }
            GUILayout.Space(6);
        }

        // ==========================================
        // Source
        // ==========================================
        private void DrawSource()
        {
            DirectTMPBrand.SectionTitle(DirectTMPText.L("Font source", "フォントの取得元", "منبع فونت"));

            var labels = new[]
            {
                DirectTMPText.L("Unity default", "Unity 標準", "پیش‌فرض یونیتی"),
                DirectTMPText.L("A font in this project", "プロジェクト内のフォント", "یک فونت داخل پروژه"),
                DirectTMPText.L("A font installed on this machine", "この PC にインストール済みのフォント", "یک فونت نصب‌شده روی این سیستم")
            };

            int current = (int)_plan.Source;
            int picked = GUILayout.SelectionGrid(current, labels, 1, EditorStyles.radioButton);
            if (picked != current)
            {
                _plan = new DirectEditorFontPlan(
                    picked != (int)DirectEditorFontSource.UnityDefault,
                    (DirectEditorFontSource)picked,
                    _plan.AssetPath,
                    _plan.SystemFamily,
                    _plan.SizeDelta);
            }

            GUILayout.Space(4);

            switch (_plan.Source)
            {
                case DirectEditorFontSource.ProjectFont:
                    DrawProjectFontPicker();
                    break;
                case DirectEditorFontSource.SystemFont:
                    DrawSystemFontPicker();
                    break;
            }
        }

        private void DrawProjectFontPicker()
        {
            Font current = DirectEditorFontSettings.ResolveAsset(_plan);

            var picked = (Font)EditorGUILayout.ObjectField(
                DirectTMPText.C(
                    "Font asset", "Font アセット", "فایل فونت",
                    "A .ttf or .otf imported into this project. Import it as a Dynamic font so it can draw every character the file contains.",
                    "このプロジェクトに取り込んだ .ttf / .otf。ファイル内の全文字を描画できるよう、Dynamic でインポートしてください。",
                    "یه .ttf یا .otf که توی این پروژه ایمپورت شده. حالت ایمپورتش رو Dynamic بذار تا هر کاراکتری که فایل داره رو بتونه بکشه."),
                current, typeof(Font), false);

            if (picked != current)
            {
                string path = picked == null ? string.Empty : AssetDatabase.GetAssetPath(picked);
                _plan = new DirectEditorFontPlan(true, DirectEditorFontSource.ProjectFont, path, _plan.SystemFamily, _plan.SizeDelta);
            }

            if (string.IsNullOrEmpty(_plan.AssetPath))
            {
                DirectTMPBrand.Note(
                    "No font picked yet. Any .ttf or .otf in your Assets folder will do — a font that covers the language your object names are in.",
                    "まだフォントが選ばれていません。Assets 内の .ttf / .otf なら何でも構いません(オブジェクト名の言語を含むフォントを選んでください)。",
                    "هنوز فونتی انتخاب نشده. هر .ttf یا .otf داخل پوشه‌ی Assets کار می‌کنه — فونتی که زبون اسم آبجکت‌هات رو داشته باشه.",
                    DirectTMPBrand.NoteLevel.Plain);
            }
        }

        private void DrawSystemFontPicker()
        {
            if (_families.Length == 0)
            {
                DirectTMPBrand.Note(
                    "Unity could not enumerate this machine's installed fonts.",
                    "この PC のインストール済みフォントを列挙できませんでした。",
                    "یونیتی نتونست فونت‌های نصب‌شده‌ی این سیستم رو لیست کنه.",
                    DirectTMPBrand.NoteLevel.Warning);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    DirectTMPText.L("Installed", "インストール済み", "نصب‌شده"),
                    DirectTMPBrand.Line,
                    GUILayout.Width(EditorGUIUtility.labelWidth));
                _familyFilter = EditorGUILayout.TextField(_familyFilter);
            }

            List<string> matches = Filter(_families, _familyFilter);

            EditorGUILayout.LabelField(
                $"{matches.Count} / {_families.Length}",
                EditorStyles.miniLabel);

            _familyScroll = EditorGUILayout.BeginScrollView(_familyScroll, GUILayout.Height(132));
            foreach (string family in matches)
            {
                bool selected = family == _plan.SystemFamily;
                bool nowSelected = GUILayout.Toggle(selected, DirectTMPText.Show(family), EditorStyles.miniButton);
                if (nowSelected && !selected)
                {
                    _plan = new DirectEditorFontPlan(true, DirectEditorFontSource.SystemFont, _plan.AssetPath, family, _plan.SizeDelta);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Case-insensitive substring filter over the family list. Extracted
        /// and internal so the tests can pin the behaviour that matters: an
        /// empty filter shows everything rather than nothing, which is the
        /// bug every hand-rolled search box ships once.
        /// </summary>
        internal static List<string> Filter(IReadOnlyList<string> families, string filter)
        {
            var matches = new List<string>();
            if (families == null) { return matches; }

            bool matchAll = string.IsNullOrEmpty(filter) || string.IsNullOrWhiteSpace(filter);
            string needle = matchAll ? null : filter.Trim();

            for (int i = 0; i < families.Count; i++)
            {
                string family = families[i];
                if (string.IsNullOrEmpty(family)) { continue; }
                if (matchAll || family.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matches.Add(family);
                }
            }
            return matches;
        }

        // ==========================================
        // Size
        // ==========================================
        private void DrawSize()
        {
            if (!_plan.IsActive) { return; }

            GUILayout.Space(6);
            DirectTMPBrand.SectionTitle(DirectTMPText.L("Size", "サイズ", "اندازه"));

            int delta = EditorGUILayout.IntSlider(
                DirectTMPText.C(
                    "Size adjustment", "サイズ調整", "تنظیم اندازه",
                    "Points added to every style. Leave it at 0 unless the replacement font runs visibly smaller or larger than Unity's — most do not.",
                    "すべてのスタイルに加算されるポイント数。置き換えたフォントが明らかに大小しない限り 0 のままで構いません。",
                    "چند پوینت به همه‌ی استایل‌ها اضافه می‌شه. مگه اینکه فونت جایگزین واضحاً ریزتر یا درشت‌تر باشه، همون صفر رو نگه دار."),
                _plan.SizeDelta,
                DirectEditorFontRules.MinSizeDelta,
                DirectEditorFontRules.MaxSizeDelta);

            if (delta != _plan.SizeDelta)
            {
                _plan = new DirectEditorFontPlan(_plan.Enabled, _plan.Source, _plan.AssetPath, _plan.SystemFamily, delta);
            }
        }

        // ==========================================
        // Coverage
        //
        // The same badges the Font Catalog draws. Here
        // they answer a narrower question: will the
        // Hierarchy be able to spell the names in it?
        // ==========================================
        private void DrawCoverage()
        {
            if (_plan.Source != DirectEditorFontSource.ProjectFont || string.IsNullOrEmpty(_plan.AssetPath))
            {
                // Clear rather than just return. A stale coverage map from the
                // font picked a moment ago would otherwise decide which
                // preview rows the CURRENT font gets - and quietly claim it
                // has scripts it does not.
                _coverage = null;
                _coverageKey = null;
                return;
            }

            if (_coverageKey != _plan.AssetPath)
            {
                _coverage = DirectFontFile.Read(DirectEditorFont.AssetPathToAbsolute(_plan.AssetPath));
                _coverageKey = _plan.AssetPath;
            }

            if (_coverage == null || !_coverage.IsValid)
            {
                if (_coverage != null && !string.IsNullOrEmpty(_coverage.Error))
                {
                    DirectTMPBrand.Note(_coverage.Error, DirectTMPBrand.NoteLevel.Warning, CardInset);
                }
                return;
            }

            GUILayout.Space(6);
            DirectTMPBrand.SectionTitle(DirectTMPText.L("What this font contains", "このフォントの収録範囲", "این فونت چی داره"));

            using (new EditorGUILayout.VerticalScope(DirectTMPBrand.Card))
            {
                EditorGUILayout.LabelField(
                    DirectTMPText.Show(_coverage.DisplayName),
                    DirectTMPBrand.BoldLine);

                EditorGUILayout.LabelField(
                    DirectTMPText.F(
                        "{0:N0} glyphs · {1:N0} characters mapped",
                        "{0:N0} グリフ · {1:N0} 文字を収録",
                        "{0:N0} گلیف · {1:N0} کاراکتر",
                        _coverage.GlyphCount, _coverage.CodepointCount),
                    EditorStyles.miniLabel);

                DirectTMPCoverageBadges.Draw(_coverage.Coverage());
            }
        }

        // ==========================================
        // Preview
        //
        // Drawn with the font itself, at the size it
        // will actually be used at, in whatever scripts
        // it covers. This is the whole reason a person
        // opens this window rather than editing a pref.
        // ==========================================
        private void DrawPreview()
        {
            if (!_plan.IsActive) { return; }

            Font font = _plan.Source == DirectEditorFontSource.SystemFont
                ? ResolvePreviewSystemFont(_plan.SystemFamily)
                : DirectEditorFontSettings.ResolveAsset(_plan);

            GUILayout.Space(6);
            DirectTMPBrand.SectionTitle(DirectTMPText.L("Preview", "プレビュー", "پیش‌نمایش"));

            if (font == null)
            {
                DirectTMPBrand.Note(
                    "Nothing to preview yet.", "プレビューするものがありません。", "فعلاً چیزی برای پیش‌نمایش نیست.",
                    DirectTMPBrand.NoteLevel.Plain);
                return;
            }

            if (_previewStyle == null || _previewFont != font)
            {
                _previewStyle = new GUIStyle(EditorStyles.label) { richText = false, wordWrap = true };
                _previewFont = font;
            }
            _previewStyle.font = font;
            _previewStyle.fontSize = DirectEditorFontRules.ResolveFontSize(12, _plan.SizeDelta);

            using (new EditorGUILayout.VerticalScope(DirectTMPBrand.Card))
            {
                // A line that looks like the Editor, because the Editor is
                // what is being restyled. Abstract pangrams do not tell you
                // whether the Project window will be legible.
                GUILayout.Label("Assets / Prefabs / UI / MainMenu.prefab", _previewStyle);
                GUILayout.Label("Transform · Position X 0.0  Y 1.25  Z -3.5", _previewStyle);

                DirectTMPBrand.Divider(4f, 4f);

                foreach (DirectScript script in DirectFontScripts.All)
                {
                    if (!ShouldPreview(script)) { continue; }

                    // The Arabic and Hebrew samples are the two rows somebody
                    // opens this window to look at. Drawing them unprepared
                    // would show the font failing at something the font is
                    // fine at.
                    GUILayout.Label(DirectTMPText.Show(DirectFontScripts.SampleFor(script)), _previewStyle);
                }
            }
        }

        // Only preview the scripts the font can actually set. A preview row of
        // empty boxes for every script the font does not have is noise that
        // buries the two rows that matter.
        private bool ShouldPreview(DirectScript script)
        {
            if (script == DirectScript.Latin) { return true; }
            if (_coverage == null || !_coverage.IsValid) { return script == DirectScript.Latin; }

            return DirectFontScripts.CoverageOf(script, _coverage.HasCodepoint) != DirectScriptCoverage.None;
        }

        private Font _previewSystemFont;
        private string _previewSystemFamily;

        private Font ResolvePreviewSystemFont(string family)
        {
            if (string.IsNullOrEmpty(family)) { return null; }
            if (_previewSystemFont != null && _previewSystemFamily == family) { return _previewSystemFont; }

            if (_previewSystemFont != null) { DestroyImmediate(_previewSystemFont); }

            _previewSystemFont = Font.CreateDynamicFontFromOSFont(family, 14);
            if (_previewSystemFont != null) { _previewSystemFont.hideFlags = HideFlags.HideAndDontSave; }
            _previewSystemFamily = family;
            return _previewSystemFont;
        }

        // ==========================================
        // Verdict and the two buttons
        // ==========================================
        private void DrawVerdictAndActions()
        {
            GUILayout.Space(8);

            DirectEditorFontProblem problem = DirectEditorFont.Validate(_plan);
            bool fatal = DirectEditorFontRules.IsFatal(problem);

            if (problem != DirectEditorFontProblem.None)
            {
                DirectTMPBrand.Note(
                    DirectEditorFontRules.Describe(problem),
                    fatal ? DirectTMPBrand.NoteLevel.Error : DirectTMPBrand.NoteLevel.Warning,
                    CardInset);

                if (problem == DirectEditorFontProblem.NotDynamic)
                {
                    if (GUILayout.Button(DirectTMPText.L(
                            "Switch this font's import mode to Dynamic",
                            "このフォントのインポート設定を Dynamic にする",
                            "حالت ایمپورت این فونت رو بذار Dynamic")))
                    {
                        DirectEditorFont.MakeImportDynamic(_plan.AssetPath);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(fatal || !_plan.IsActive))
                {
                    if (GUILayout.Button(
                            DirectTMPText.L("Apply to the Editor", "エディタに適用", "اعمال روی ادیتور"),
                            GUILayout.Height(30)))
                    {
                        DirectEditorFontSettings.Plan = _plan;
                        DirectEditorFont.Apply(_plan);
                    }
                }

                using (new EditorGUI.DisabledScope(!DirectEditorFont.IsActive))
                {
                    if (GUILayout.Button(
                            DirectTMPText.L("Revert to Unity's font", "Unity のフォントに戻す", "برگشت به فونت یونیتی"),
                            GUILayout.Height(30), GUILayout.Width(190)))
                    {
                        DirectEditorFontSettings.Clear();
                        _plan = DirectEditorFontPlan.Off;
                    }
                }
            }

            if (DirectEditorFont.IsActive)
            {
                EditorGUILayout.LabelField(
                    DirectTMPText.F(
                        "Active now · {0} style(s) restyled",
                        "適用中 · {0} 個のスタイルを変更",
                        "الآن فعاله · {0} استایل عوض شده",
                        DirectEditorFont.PatchedStyleCount),
                    EditorStyles.miniLabel);
            }
        }

        // ==========================================
        // The sentence that makes Apply pressable
        // ==========================================
        private void DrawSafetyNote()
        {
            GUILayout.Space(6);
            DirectTMPBrand.Note(
                "Nothing is written to your Unity installation. The change lives in memory for this session only, so quitting Unity always restores the default font — and a font that cannot draw the Editor's own menus is refused rather than applied.",
                "Unity のインストール先には一切書き込みません。変更はこのセッションのメモリ上だけに存在するため、Unity を終了すれば必ず既定のフォントに戻ります。エディタのメニューを描画できないフォントは、適用せず拒否します。",
                "هیچ‌چیزی روی نصب یونیتی نوشته نمی‌شه. تغییر فقط توی حافظه‌ی همین سشن زندگی می‌کنه، پس بستن یونیتی همیشه فونت پیش‌فرض رو برمی‌گردونه — و فونتی که نتونه منوهای خود ادیتور رو بکشه، اصلاً اعمال نمی‌شه.",
                DirectTMPBrand.NoteLevel.Info);
        }
    }
}
