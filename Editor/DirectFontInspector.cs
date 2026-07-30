// ==========================================
// DirectFontInspector
// The Inspector you already know, with one small
// panel added.
//
// The rebuild of this file was mostly about
// descriptions. Every field now says what it does AND
// what it costs, in the tooltip, in the reader's
// language - because the old version drew six controls
// with names like "Preserve Material" and left the
// reader to guess, and "the tool has no descriptions"
// was a fair thing to say about it.
//
// Under the controls sits the part that earns the
// panel: what the chosen font actually contains, and
// what has been rasterized out of it so far. That
// second number is the one that turns "why is this
// scene slow the first time it shows Japanese" into an
// answer.
// ==========================================
using TMPro;
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    [CustomEditor(typeof(DirectFont))]
    [CanEditMultipleObjects]
    internal sealed class DirectFontInspector : UnityEditor.Editor
    {
        // An Inspector is narrow and indented, and its width is the whole
        // window's rather than a panel's.
        private const float InspectorInset = 42f;

        private SerializedProperty fontFile;
        private SerializedProperty fallbackChain;
        private SerializedProperty preserveMaterial;
        private SerializedProperty applyOnEnable;
        private SerializedProperty shapeText;
        private SerializedProperty overrideSettings;
        private SerializedProperty settings;

        private DirectFontFileInfo _fileInfo;
        private string _fileInfoKey;

        private void OnEnable()
        {
            fontFile = serializedObject.FindProperty("fontFile");
            fallbackChain = serializedObject.FindProperty("fallbackChain");
            preserveMaterial = serializedObject.FindProperty("preserveMaterial");
            applyOnEnable = serializedObject.FindProperty("applyOnEnable");
            shapeText = serializedObject.FindProperty("shapeText");
            overrideSettings = serializedObject.FindProperty("overrideSettings");
            settings = serializedObject.FindProperty("settings");
        }

        public override void OnInspectorGUI()
        {
            var directFont = (DirectFont)target;

            if (directFont.GetComponent<TMP_Text>() == null)
            {
                DirectTMPBrand.Note(
                    "Direct Font needs a TextMeshPro component (TextMeshProUGUI or TextMeshPro) on the same GameObject — it has nothing to drive on its own.",
                    "Direct Font は同じ GameObject 上の TextMeshPro(TextMeshProUGUI / TextMeshPro)を必要とします。単体では動作対象がありません。",
                    "کامپوننت Direct Font به یه TextMeshPro (چه TextMeshProUGUI چه TextMeshPro) روی همون GameObject نیاز داره — تنهایی چیزی برای اداره کردن نداره.",
                    DirectTMPBrand.NoteLevel.Warning);
            }

            serializedObject.Update();

            EditorGUILayout.PropertyField(fontFile, DirectTMPText.C(
                "Font File", "フォントファイル", "فایل فونت",
                "The .ttf or .otf glyphs are rasterized from. Import it as a Dynamic font so every character in the file is reachable.",
                "グリフの生成元となる .ttf / .otf。ファイル内の全文字を使えるよう Dynamic でインポートしてください。",
                "همون .ttf یا .otf که گلیف‌ها ازش ساخته می‌شن. حالت ایمپورتش رو Dynamic بذار تا هر کاراکتری که توی فایل هست در دسترس باشه."));

            EditorGUILayout.PropertyField(fallbackChain, DirectTMPText.C(
                "Fallback Chain", "フォールバックチェーン", "زنجیره‌ی فال‌بک",
                "Optional. An ordered list of fonts: for each character, the first font in the chain that actually has that glyph supplies it.",
                "任意。順序付きのフォント一覧です。1 文字ごとに、その文字を実際に持つ最初のフォントが使われます。",
                "اختیاری. یه لیست ترتیب‌دار از فونت‌ها: برای هر کاراکتر، اولین فونتی که واقعاً اون گلیف رو داشته باشه انتخاب می‌شه."));

            EditorGUILayout.Space(4);

            EditorGUILayout.PropertyField(preserveMaterial, DirectTMPText.C(
                "Preserve Material", "マテリアルを保持", "حفظ متریال",
                "Keep this label's outline, underlay, gradient and softness when the font is rebuilt. Off means the label resets to the font's plain material every time.",
                "フォント再構築時に、このラベルのアウトライン・Underlay・グラデーション・ソフトネスを維持します。オフにすると、毎回フォント既定のマテリアルに戻ります。",
                "موقع ساخته شدن دوباره‌ی فونت، Outline و Underlay و گرادیان و نرمی همین لیبل حفظ می‌شه. اگه خاموش باشه، هر بار به متریال ساده‌ی فونت برمی‌گرده."));

            EditorGUILayout.PropertyField(applyOnEnable, DirectTMPText.C(
                "Apply On Enable", "有効時に適用", "اعمال هنگام فعال شدن",
                "Build and apply the font whenever this component is enabled. Leave it on unless you are driving the font entirely from code.",
                "このコンポーネントが有効になるたびにフォントを構築して適用します。コードから完全に制御する場合以外はオンのままで構いません。",
                "هر بار که این کامپوننت فعال می‌شه فونت رو می‌سازه و اعمال می‌کنه. مگه اینکه کلاً از کد کنترلش کنی، روشن بذارش."));

            EditorGUILayout.PropertyField(shapeText, DirectTMPText.C(
                "Shape Text", "文字を整形", "اصلاح متن",
                "Also make Arabic-script text read: letters joined, right-to-left words in reading order, wrapped lines top to bottom. A font file alone stops the boxes; it cannot do this, and neither can TextMeshPro. Adds a Direct Text component, where the settings for it live.",
                "アラビア文字系のテキストが正しく読めるようにします。字形の結合、右から左への語順、折り返し行の順序まで含みます。フォントファイルだけでは豆腐が消えるだけで、この処理は TextMeshPro にもありません。設定は追加される Direct Text コンポーネント側にあります。",
                "کاری می‌کنه که متن فارسی/عربی درست خونده بشه: حروف بهم بچسبن، کلمه‌ها راست‌به‌چپ بشن و خطوط شکسته‌شده از بالا به پایین خونده بشن. فایل فونت فقط جلوی مربع‌ها رو می‌گیره؛ این کار نه از فونت برمیاد نه از TextMeshPro. یه کامپوننت Direct Text اضافه می‌کنه که تنظیماتش اونجاست."));

            EditorGUILayout.Space(4);

            EditorGUILayout.PropertyField(overrideSettings, DirectTMPText.C(
                "Override Settings", "設定を上書き", "بازنویسی تنظیمات",
                "Use custom rasterization settings for this label instead of the project defaults. Note that a label with its own settings builds its OWN atlas — two labels sharing a font but not its settings cost two atlases.",
                "プロジェクト既定ではなく、このラベル専用のラスタライズ設定を使います。独自設定のラベルは独自のアトラスを持つため、同じフォントでも設定が違えばアトラスは 2 枚になります。",
                "به‌جای پیش‌فرض‌های پروژه، برای همین لیبل تنظیمات رسترایز جدا استفاده کن. دقت کن: لیبلی که تنظیمات خودش رو داره اطلس خودش رو هم می‌سازه — دو لیبل با یه فونت ولی تنظیمات متفاوت، دو تا اطلس خرج دارن."));

            if (overrideSettings.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(settings.FindPropertyRelative("samplingPointSize"),
                        DirectTMPText.C("Sampling Point Size", "サンプリングサイズ", "سایز نمونه‌برداری"));
                    EditorGUILayout.PropertyField(settings.FindPropertyRelative("atlasPadding"),
                        DirectTMPText.C("Atlas Padding", "アトラスの余白", "فاصله‌ی اطلس"));
                    EditorGUILayout.PropertyField(settings.FindPropertyRelative("atlasWidth"),
                        DirectTMPText.C("Atlas Width", "アトラス幅", "عرض اطلس"));
                    EditorGUILayout.PropertyField(settings.FindPropertyRelative("atlasHeight"),
                        DirectTMPText.C("Atlas Height", "アトラス高さ", "ارتفاع اطلس"));
                    EditorGUILayout.PropertyField(settings.FindPropertyRelative("renderMode"),
                        DirectTMPText.C("Render Mode", "レンダーモード", "حالت رندر"));
                }
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(DirectTMPText.L("Rebuild now", "今すぐ再構築", "همین حالا بساز")))
                {
                    foreach (Object t in targets)
                    {
                        (t as DirectFont)?.Refresh();
                    }
                }
                if (GUILayout.Button(DirectTMPText.L("Clear font cache", "フォントキャッシュを消去", "پاک کردن کش فونت")))
                {
                    int released = DirectTMP.ClearCache();
                    DirectTMPLog.Info($"Cleared {released} cached atlas(es).");
                }
                if (GUILayout.Button(DirectTMPText.L("Catalog", "カタログ", "کاتالوگ"), GUILayout.Width(90)))
                {
                    DirectFontCatalogWindow.ShowWindow();
                }
            }

            if (!serializedObject.isEditingMultipleObjects)
            {
                DrawFontFacts();
                DrawLiveReadout((DirectFont)target);
            }
        }

        // ==========================================
        // What the font file contains
        //
        // Read from the file itself rather than from the
        // built atlas, so it is answerable before a
        // single glyph has been rasterized - which is
        // when somebody is deciding whether this is the
        // right font.
        // ==========================================
        private void DrawFontFacts()
        {
            var font = fontFile.objectReferenceValue as Font;
            if (font == null) { return; }

            string assetPath = AssetDatabase.GetAssetPath(font);
            if (string.IsNullOrEmpty(assetPath)) { return; }

            if (_fileInfoKey != assetPath)
            {
                _fileInfo = DirectFontFile.Read(DirectEditorFont.AssetPathToAbsolute(assetPath));
                _fileInfoKey = assetPath;
            }

            if (_fileInfo == null) { return; }

            EditorGUILayout.Space(8);
            DirectTMPBrand.SectionTitle(DirectTMPText.L("This font", "このフォント", "این فونت"));

            if (!_fileInfo.IsValid)
            {
                DirectTMPBrand.Note(_fileInfo.Error, DirectTMPBrand.NoteLevel.Warning, InspectorInset);
                return;
            }

            using (new EditorGUILayout.VerticalScope(DirectTMPBrand.Card))
            {
                EditorGUILayout.LabelField(DirectTMPText.Show(_fileInfo.DisplayName), DirectTMPBrand.BoldLine);
                EditorGUILayout.LabelField(
                    DirectTMPText.Show(string.Format("{0:N0} {1} · {2:N0} characters · {3}",
                        _fileInfo.GlyphCount,
                        DirectTMPText.WordRaw("glyphs"),
                        _fileInfo.CodepointCount,
                        DirectFontCatalogFilter.FormatBytes(_fileInfo.FileBytes))),
                    EditorStyles.miniLabel);

                if (_fileInfo.FaceCount > 1)
                {
                    DirectTMPBrand.Note(
                        string.Format(
                            DirectTMPText.Raw(
                                "This is a font collection with {0} faces. DirectTMP uses the first one; a face picker is on the roadmap.",
                                "これは {0} 個のフェイスを持つフォントコレクションです。DirectTMP は最初のフェイスを使用します(フェイス選択は予定)。",
                                "این یه کالکشن فونت با {0} فیس هست. DirectTMP از اولی استفاده می‌کنه؛ انتخاب فیس توی نقشه‌ی راهه."),
                            _fileInfo.FaceCount),
                        DirectTMPBrand.NoteLevel.Info,
                        InspectorInset);
                }

                if (!DirectEditorFont.IsDynamicImport(assetPath))
                {
                    DirectTMPBrand.Note(
                        DirectEditorFontRules.Describe(DirectEditorFontProblem.NotDynamic),
                        DirectTMPBrand.NoteLevel.Warning,
                        InspectorInset);

                    if (GUILayout.Button(DirectTMPText.L(
                            "Set import mode to Dynamic", "インポート設定を Dynamic にする", "حالت ایمپورت رو بذار Dynamic")))
                    {
                        DirectEditorFont.MakeImportDynamic(assetPath);
                    }
                }

                DirectTMPCoverageBadges.Draw(_fileInfo.Coverage());
            }
        }

        // ==========================================
        // DrawLiveReadout
        // A quick glance at whatever font is built right
        // now: family / style and how many glyphs have
        // been rasterized into the atlas so far.
        // ==========================================
        private static void DrawLiveReadout(DirectFont directFont)
        {
            TMP_FontAsset asset = directFont.FontAsset;
            EditorGUILayout.Space(6);
            DirectTMPBrand.SectionTitle(DirectTMPText.L("Built right now", "現在の生成状態", "چیزی که الآن ساخته شده"));

            if (asset == null)
            {
                DirectTMPBrand.Note(
                    "Nothing built yet. Assign a Font File and it builds when this component is enabled.",
                    "まだ何も生成されていません。Font File を設定すると、このコンポーネントが有効になったときに生成されます。",
                    "هنوز چیزی ساخته نشده. یه Font File بذار تا موقع فعال شدن این کامپوننت ساخته بشه.",
                    DirectTMPBrand.NoteLevel.Plain);
                return;
            }

            using (new EditorGUILayout.VerticalScope(DirectTMPBrand.Card))
            {
                EditorGUILayout.LabelField(
                    DirectTMPText.L("Family", "ファミリー", "خانواده"),
                    DirectTMPText.Show(asset.faceInfo.familyName));
                EditorGUILayout.LabelField(
                    DirectTMPText.L("Style", "スタイル", "استایل"),
                    DirectTMPText.Show(asset.faceInfo.styleName));

                int glyphs = asset.glyphTable != null ? asset.glyphTable.Count : 0;
                int chars = asset.characterTable != null ? asset.characterTable.Count : 0;

                EditorGUILayout.LabelField(
                    DirectTMPText.L("Rasterized so far", "これまでにラスタライズ済み", "تا الآن رسترایز شده"),
                    DirectTMPText.F("{0} glyph(s), {1} character(s)", "{0} グリフ / {1} 文字", "{0} گلیف، {1} کاراکتر",
                        glyphs, chars));

                EditorGUILayout.LabelField(
                    DirectTMPText.Word("settings"),
                    directFont.ResolvedSettings.ToString());
            }
        }
    }
}
