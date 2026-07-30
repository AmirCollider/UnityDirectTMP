// ==========================================
// DirectFontCatalogWindow
// The catalog, drawn.
//
// One row per font. Each row answers the four things
// somebody is actually looking for, left to right:
// what it is called, what it contains, how big it is,
// and what they can do with it right now.
//
// Two decisions worth stating, because both are the
// opposite of what a font browser usually does:
//
//   * The preview text is one field at the top of the
//     window, shared by every row. Type Persian into
//     it and all forty rows re-render in Persian at
//     once, which turns "which of these can set my
//     menu labels?" into one glance instead of forty
//     clicks. A per-row preview would be prettier and
//     answer nothing.
//
//   * Rows are drawn only when they are on screen.
//     IMGUI has no virtualisation, and a catalog of
//     three hundred system fonts each drawing eleven
//     coverage chips is thousands of layout groups per
//     repaint - which is a window that scrolls at four
//     frames a second. The visible-range check is what
//     keeps it smooth.
// ==========================================
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    internal sealed class DirectFontCatalogWindow : EditorWindow
    {
        private const float RowHeight = 78f;

        private Vector2 _scroll;
        private string _query = string.Empty;
        private string _previewText = string.Empty;
        private DirectCatalogSort _sort = DirectCatalogSort.Name;
        private int _scriptFilter;          // 0 = any; otherwise index into DirectFontScripts.All + 1
        private bool _includeSystem;

        private List<DirectCatalogEntry> _visible = new List<DirectCatalogEntry>();
        private bool _dirty = true;

        private GUIStyle _previewStyle;
        private readonly Dictionary<string, Font> _previewFonts = new Dictionary<string, Font>();

        public static void ShowWindow()
        {
            var window = GetWindow<DirectFontCatalogWindow>(false, DirectTMPConstants.ShortName + " · Font Catalog", true);
            window.minSize = new Vector2(620, 480);
            window.Show();
        }

        private void OnEnable()
        {
            _includeSystem = DirectTMPPrefs.GetBool(DirectTMPPrefs.CatalogIncludeSystemFonts, false);
            _sort = (DirectCatalogSort)DirectTMPPrefs.GetInt(DirectTMPPrefs.CatalogSort, (int)DirectCatalogSort.Name);
            _scriptFilter = DirectTMPPrefs.GetInt(DirectTMPPrefs.CatalogScriptFilter, 0);
            _dirty = true;
        }

        private void OnDisable()
        {
            foreach (KeyValuePair<string, Font> entry in _previewFonts)
            {
                if (entry.Value != null) { DestroyImmediate(entry.Value); }
            }
            _previewFonts.Clear();
        }

        private void OnGUI()
        {
            DirectTMPBrand.Header(
                DirectTMPText.L("Font Catalog", "フォントカタログ", "کاتالوگ فونت"),
                DirectTMPText.L(
                    "Every font in this project and on this machine, with what each one actually contains.",
                    "このプロジェクトと この PC のすべてのフォントを、実際の収録内容つきで一覧します。",
                    "همه‌ی فونت‌های این پروژه و این سیستم، همراه با چیزی که واقعاً توش هست."));

            DrawToolbar();
            DrawPreviewField();

            if (_dirty) { Rebuild(); }

            if (_visible.Count == 0)
            {
                DrawEmptyState();
            }
            else
            {
                DrawList();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                using (new EditorGUILayout.VerticalScope())
                {
                    DirectTMPBrand.Footer();
                }
                GUILayout.Space(10);
            }
            GUILayout.Space(4);
        }

        // ==========================================
        // Toolbar
        // ==========================================
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                string typed = GUILayout.TextField(_query, EditorStyles.toolbarSearchField, GUILayout.MinWidth(140));
                if (typed != _query) { _query = typed; _dirty = true; }

                GUILayout.Space(6);

                var scriptLabels = new List<string>
                {
                    DirectTMPText.L("Any script", "すべての文字体系", "همه‌ی خط‌ها")
                };
                foreach (DirectScript script in DirectFontScripts.All)
                {
                    scriptLabels.Add(DirectFontScripts.NameFor(script, DirectTMPText.Current));
                }

                int pickedScript = EditorGUILayout.Popup(_scriptFilter, scriptLabels.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(140));
                if (pickedScript != _scriptFilter)
                {
                    _scriptFilter = pickedScript;
                    DirectTMPPrefs.SetInt(DirectTMPPrefs.CatalogScriptFilter, _scriptFilter);
                    _dirty = true;
                }

                var sortLabels = new[]
                {
                    DirectTMPText.L("Name", "名前", "نام"),
                    DirectTMPText.L("Most glyphs", "グリフ数", "بیشترین گلیف"),
                    DirectTMPText.L("Most scripts", "文字体系の数", "بیشترین خط"),
                    DirectTMPText.L("Largest file", "ファイルサイズ", "بزرگ‌ترین فایل")
                };

                var pickedSort = (DirectCatalogSort)EditorGUILayout.Popup((int)_sort, sortLabels, EditorStyles.toolbarPopup, GUILayout.Width(120));
                if (pickedSort != _sort)
                {
                    _sort = pickedSort;
                    DirectTMPPrefs.SetInt(DirectTMPPrefs.CatalogSort, (int)_sort);
                    _dirty = true;
                }

                GUILayout.FlexibleSpace();

                bool includeSystem = GUILayout.Toggle(
                    _includeSystem,
                    new GUIContent(
                        DirectTMPText.L("Installed fonts", "インストール済み", "فونت‌های نصب‌شده"),
                        DirectTMPText.L(
                            "Also read every font installed on this machine. The first scan takes a moment — there are usually a few hundred.",
                            "この PC にインストールされた全フォントも読み込みます。通常数百あるため、初回スキャンには少し時間がかかります。",
                            "فونت‌های نصب‌شده روی این سیستم رو هم بخون. اسکن اول کمی طول می‌کشه — معمولاً چند صد تا هستن.")),
                    EditorStyles.toolbarButton,
                    GUILayout.Width(130));

                if (includeSystem != _includeSystem)
                {
                    _includeSystem = includeSystem;
                    DirectTMPPrefs.SetBool(DirectTMPPrefs.CatalogIncludeSystemFonts, includeSystem);
                    _dirty = true;
                }

                if (GUILayout.Button(DirectTMPText.Word("refresh"), EditorStyles.toolbarButton, GUILayout.Width(72)))
                {
                    DirectFontCatalog.Invalidate();
                    _dirty = true;
                }
            }
        }

        // ==========================================
        // The shared preview line
        // ==========================================
        private void DrawPreviewField()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField(
                    DirectTMPText.C(
                        "Preview text", "プレビュー文字列", "متن پیش‌نمایش",
                        "Typed once, rendered by every font below. Paste the actual string your UI has to show.",
                        "一度入力すれば、下のすべてのフォントで描画されます。UI に実際に表示する文字列を貼り付けてください。",
                        "یه بار تایپ کن، همه‌ی فونت‌های پایین باهاش رندر می‌شن. همون متنی که واقعاً باید توی UI نشون بدی رو بچسبون."),
                    GUILayout.Width(96));

                _previewText = EditorGUILayout.TextField(_previewText);

                if (GUILayout.Button(DirectTMPText.L("Sample", "サンプル", "نمونه"), EditorStyles.miniButton, GUILayout.Width(70)))
                {
                    _previewText = "Aa 123 " + DirectFontScripts.SampleFor(DirectScript.Kana)
                        + " " + DirectFontScripts.SampleFor(DirectScript.Arabic);
                }
                GUILayout.Space(10);
            }
            GUILayout.Space(4);
        }

        // ==========================================
        // The list
        // ==========================================
        private void Rebuild()
        {
            DirectScript? required = _scriptFilter <= 0
                ? (DirectScript?)null
                : DirectFontScripts.All[Mathf.Clamp(_scriptFilter - 1, 0, DirectFontScripts.All.Length - 1)];

            _visible = DirectFontCatalogFilter.Apply(
                DirectFontCatalog.All(_includeSystem),
                _query,
                required,
                _sort);

            _dirty = false;
        }

        private void DrawList()
        {
            EditorGUILayout.LabelField(
                string.Format(
                    DirectTMPText.L("{0} font(s)", "{0} 件", "{0} فونت"),
                    _visible.Count),
                EditorStyles.miniLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // Reserve the full height up front, then draw only the rows inside
            // the viewport. This is the whole reason a three-hundred-font
            // catalog scrolls smoothly.
            float totalHeight = _visible.Count * RowHeight;
            Rect canvas = GUILayoutUtility.GetRect(0, totalHeight, GUILayout.ExpandWidth(true));

            float viewTop = _scroll.y;
            float viewBottom = _scroll.y + position.height;

            int first = Mathf.Max(0, Mathf.FloorToInt((viewTop - canvas.y) / RowHeight) - 1);
            int last = Mathf.Min(_visible.Count - 1, Mathf.CeilToInt((viewBottom - canvas.y) / RowHeight) + 1);

            for (int i = first; i <= last; i++)
            {
                var row = new Rect(canvas.x, canvas.y + (i * RowHeight), canvas.width, RowHeight);
                DrawRow(row, _visible[i], i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawRow(Rect row, DirectCatalogEntry entry, int index)
        {
            if (index % 2 == 1)
            {
                EditorGUI.DrawRect(row, new Color(0f, 0f, 0f, EditorGUIUtility.isProSkin ? 0.10f : 0.03f));
            }
            EditorGUI.DrawRect(new Rect(row.x, row.yMax - 1f, row.width, 1f), DirectTMPBrand.Border);

            var content = new Rect(row.x + 10f, row.y + 6f, row.width - 20f, row.height - 12f);

            // --- line 1: name, source, size ---
            var nameRect = new Rect(content.x, content.y, content.width - 210f, 18f);
            EditorGUI.LabelField(nameRect, entry.DisplayName, EditorStyles.boldLabel);

            var metaRect = new Rect(content.xMax - 205f, content.y, 205f, 18f);
            string meta = entry.IsUsable
                ? string.Format("{0:N0} {1} · {2}", entry.GlyphCount, DirectTMPText.Word("glyphs"),
                    DirectFontCatalogFilter.FormatBytes(entry.Bytes))
                : DirectTMPText.L("unreadable", "読み取り不可", "قابل خواندن نیست");

            var metaStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = entry.IsUsable ? DirectTMPBrand.TextDim : DirectTMPBrand.Blush }
            };
            EditorGUI.LabelField(metaRect, meta, metaStyle);

            // --- line 2: the live preview, in this font ---
            var previewRect = new Rect(content.x, content.y + 19f, content.width - 190f, 22f);
            DrawPreview(previewRect, entry);

            // --- line 2 right: the actions ---
            var actions = new Rect(content.xMax - 184f, content.y + 20f, 184f, 20f);
            DrawActions(actions, entry);

            // --- line 3: where it came from, and the coverage summary ---
            var footRect = new Rect(content.x, content.yMax - 16f, content.width, 16f);
            string origin = entry.Source == DirectCatalogSource.Project
                ? entry.AssetPath
                : DirectTMPText.L("Installed · ", "インストール済み · ", "نصب‌شده · ") + entry.FileName;

            var footStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = DirectTMPBrand.TextDim } };
            EditorGUI.LabelField(new Rect(footRect.x, footRect.y, footRect.width * 0.45f, footRect.height), origin, footStyle);

            var coverageStyle = new GUIStyle(footStyle) { alignment = TextAnchor.MiddleRight };
            string coverage = entry.IsUsable
                ? DirectTMPCoverageBadges.Summarize(entry.Coverage)
                : (entry.Info != null ? entry.Info.Error : string.Empty);

            EditorGUI.LabelField(
                new Rect(footRect.x + (footRect.width * 0.45f), footRect.y, footRect.width * 0.55f, footRect.height),
                new GUIContent(coverage, coverage),
                coverageStyle);
        }

        private void DrawPreview(Rect rect, DirectCatalogEntry entry)
        {
            string text = string.IsNullOrEmpty(_previewText)
                ? DirectFontScripts.SampleFor(DirectScript.Latin)
                : _previewText;

            Font font = PreviewFont(entry);
            if (font == null)
            {
                EditorGUI.LabelField(rect, text, EditorStyles.miniLabel);
                return;
            }

            if (_previewStyle == null)
            {
                _previewStyle = new GUIStyle(EditorStyles.label) { fontSize = 15, clipping = TextClipping.Clip };
            }
            _previewStyle.font = font;
            EditorGUI.LabelField(rect, text, _previewStyle);
        }

        // A dynamic Font per catalog entry, made once and kept until the
        // window closes. A project font already IS a Font asset; a system font
        // has to be opened from its family name, and doing that per repaint
        // would allocate a font object sixty times a second.
        private Font PreviewFont(DirectCatalogEntry entry)
        {
            if (entry.Source == DirectCatalogSource.Project) { return entry.LoadAsset(); }

            string key = entry.AbsolutePath;
            if (_previewFonts.TryGetValue(key, out Font cached)) { return cached; }

            Font built = null;
            if (entry.Info != null && !string.IsNullOrEmpty(entry.Info.FamilyName))
            {
                built = Font.CreateDynamicFontFromOSFont(entry.Info.FamilyName, 15);
                if (built != null) { built.hideFlags = HideFlags.HideAndDontSave; }
            }

            _previewFonts[key] = built;
            return built;
        }

        // ==========================================
        // Row actions
        //
        // Three, and only ones that are one click away
        // from useful. "Reveal" is there because the
        // single most common thing a person wants after
        // finding the right font in a list of forty is
        // the file itself.
        // ==========================================
        private void DrawActions(Rect rect, DirectCatalogEntry entry)
        {
            float width = rect.width / 3f;

            using (new EditorGUI.DisabledScope(!entry.IsUsable))
            {
                var useRect = new Rect(rect.x, rect.y, width - 2f, rect.height);
                if (GUI.Button(useRect, new GUIContent(
                        DirectTMPText.L("Editor", "エディタ", "ادیتور"),
                        DirectTMPText.L(
                            "Use this font for Unity's own interface.",
                            "このフォントを Unity 自身の UI に使います。",
                            "این فونت رو برای رابط خود یونیتی استفاده کن.")),
                        EditorStyles.miniButtonLeft))
                {
                    UseForEditor(entry);
                }

                var selectionRect = new Rect(rect.x + width, rect.y, width - 2f, rect.height);
                using (new EditorGUI.DisabledScope(entry.Source != DirectCatalogSource.Project || Selection.gameObjects.Length == 0))
                {
                    if (GUI.Button(selectionRect, new GUIContent(
                            DirectTMPText.L("Selection", "選択中", "انتخاب‌شده"),
                            DirectTMPText.L(
                                "Give every TextMeshPro label in the current selection a Direct Font pointing at this file.",
                                "現在選択中の TextMeshPro すべてに、このフォントを指す Direct Font を付けます。",
                                "به همه‌ی لیبل‌های TextMeshPro توی انتخاب فعلی یه Direct Font بده که به این فایل اشاره کنه.")),
                            EditorStyles.miniButtonMid))
                    {
                        DirectTMPConverter.ApplyFontToSelection(entry.LoadAsset());
                    }
                }
            }

            var revealRect = new Rect(rect.x + (width * 2f), rect.y, width, rect.height);
            if (GUI.Button(revealRect, new GUIContent(
                    DirectTMPText.L("Reveal", "表示", "نمایش فایل"),
                    entry.AbsolutePath),
                    EditorStyles.miniButtonRight))
            {
                EditorUtility.RevealInFinder(entry.AbsolutePath);
            }
        }

        private void UseForEditor(DirectCatalogEntry entry)
        {
            DirectEditorFontPlan plan = entry.Source == DirectCatalogSource.Project
                ? new DirectEditorFontPlan(true, DirectEditorFontSource.ProjectFont, entry.AssetPath, null, 0)
                : new DirectEditorFontPlan(true, DirectEditorFontSource.SystemFont, null,
                    entry.Info != null ? entry.Info.FamilyName : null, 0);

            DirectEditorFontProblem problem = DirectEditorFont.Validate(plan);
            if (DirectEditorFontRules.IsFatal(problem))
            {
                EditorUtility.DisplayDialog(
                    DirectTMPConstants.ToolName,
                    DirectEditorFontRules.Describe(problem),
                    "OK");
                return;
            }

            DirectEditorFontSettings.Plan = plan;
            DirectEditorFont.Apply(plan);
            DirectEditorFontWindow.ShowWindow();
        }

        // ==========================================
        // Empty state
        //
        // Never a blank panel. A catalog with nothing in
        // it is either a project with no fonts (say so,
        // and offer the machine's) or a filter that
        // matched nothing (say that instead, and offer
        // to clear it).
        // ==========================================
        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(380)))
                {
                    GUILayout.Label(DirectTMPBrand.Mascot, GUILayout.Width(64), GUILayout.Height(64));

                    bool filtered = !string.IsNullOrEmpty(_query) || _scriptFilter > 0;

                    GUILayout.Label(
                        filtered
                            ? DirectTMPText.L("Nothing matches that.", "一致するものがありません。", "چیزی با این جست‌وجو پیدا نشد.")
                            : DirectTMPText.L("No fonts in this project yet.", "このプロジェクトにフォントがまだありません。", "هنوز هیچ فونتی توی این پروژه نیست."),
                        DirectTMPBrand.Title);

                    GUILayout.Label(
                        filtered
                            ? DirectTMPText.L(
                                "Try a different name, or widen the script filter.",
                                "別の名前で検索するか、文字体系の絞り込みを緩めてください。",
                                "یه اسم دیگه امتحان کن، یا فیلتر خط رو بازتر کن.")
                            : DirectTMPText.L(
                                "Drop a .ttf or .otf anywhere in Assets, or switch on the installed fonts above to browse what this machine already has.",
                                "Assets のどこかに .ttf / .otf を置くか、上の「インストール済み」を有効にして この PC のフォントを見てください。",
                                "یه .ttf یا .otf هرجای Assets بنداز، یا از بالا «فونت‌های نصب‌شده» رو روشن کن تا چیزی که این سیستم داره رو ببینی."),
                        DirectTMPBrand.Subtitle);

                    GUILayout.Space(8);
                    if (filtered)
                    {
                        if (GUILayout.Button(DirectTMPText.L("Clear the filters", "絞り込みを解除", "پاک کردن فیلترها")))
                        {
                            _query = string.Empty;
                            _scriptFilter = 0;
                            _dirty = true;
                        }
                    }
                    else if (!_includeSystem)
                    {
                        if (GUILayout.Button(DirectTMPText.L("Show this machine's fonts", "この PC のフォントを表示", "فونت‌های این سیستم رو نشون بده")))
                        {
                            _includeSystem = true;
                            DirectTMPPrefs.SetBool(DirectTMPPrefs.CatalogIncludeSystemFonts, true);
                            _dirty = true;
                        }
                    }
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
        }
    }
}
