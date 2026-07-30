// ==========================================
// DirectTextInspector
// Four controls and two notes.
//
// The notes are the point. Direct Text does its work
// silently and correctly, which means the two times it
// deliberately does NOT do the obvious thing are
// invisible - and an invisible refusal reads exactly
// like a bug:
//
//   * inside a TMP_InputField, where reordering the
//     text would move the caret away from the letter
//     it belongs to;
//   * on a label with no TextMeshPro component to
//     drive at all.
//
// Both say so here, in the reader's own language,
// rather than in a console line nobody scrolled back to.
// ==========================================
using TMPro;
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    [CustomEditor(typeof(DirectText))]
    [CanEditMultipleObjects]
    internal sealed class DirectTextInspector : UnityEditor.Editor
    {
        private SerializedProperty direction;
        private SerializedProperty alignToDirection;
        private SerializedProperty fixWrappedLines;
        private SerializedProperty substituteMissingGlyphs;

        private void OnEnable()
        {
            direction = serializedObject.FindProperty("direction");
            alignToDirection = serializedObject.FindProperty("alignToDirection");
            fixWrappedLines = serializedObject.FindProperty("fixWrappedLines");
            substituteMissingGlyphs = serializedObject.FindProperty("substituteMissingGlyphs");
        }

        public override void OnInspectorGUI()
        {
            var directText = (DirectText)target;

            if (directText.GetComponent<TMP_Text>() == null)
            {
                DirectTMPBrand.Note(
                    "Direct Text needs a TextMeshPro component (TextMeshProUGUI or TextMeshPro) on the same GameObject — it has nothing to shape on its own.",
                    "Direct Text は同じ GameObject 上の TextMeshPro(TextMeshProUGUI / TextMeshPro)を必要とします。単体では処理する対象がありません。",
                    "کامپوننت Direct Text به یه TextMeshPro (چه TextMeshProUGUI چه TextMeshPro) روی همون GameObject نیاز داره — تنهایی چیزی برای اصلاح کردن نداره.",
                    DirectTMPBrand.NoteLevel.Warning);
            }
            else if (directText.EditorSkipped)
            {
                DirectTMPBrand.Note(
                    "This is the text component of a TMP_InputField, so it is deliberately left alone: reordering the text a field is editing puts the caret in the wrong place and lets typing edit the wrong end of the word. Put Direct Text on the field's Placeholder instead — that one is safe.",
                    "これは TMP_InputField 本体のテキストコンポーネントのため、意図的に処理していません。編集中のテキストを並べ替えると、キャレット位置がずれ、入力が語の反対側に入ってしまいます。代わりに Placeholder 側に Direct Text を付けてください（そちらは安全です）。",
                    "این کامپوننتِ متنِ خودِ TMP_InputField هست، پس عمداً دست‌نخورده می‌مونه: مرتب کردن متنی که داره ویرایش می‌شه، مکان‌نما رو جابه‌جا می‌کنه و تایپ از سمت اشتباهِ کلمه انجام می‌شه. به‌جاش Direct Text رو روی Placeholder همون فیلد بذار — اون یکی مشکلی نداره.",
                    DirectTMPBrand.NoteLevel.Info);
            }

            string unjoined = directText.EditorUnjoinedLetters;
            if (!string.IsNullOrEmpty(unjoined))
            {
                // Not a failure of the shaping - a fact about the font, and
                // the one thing that still looks like a bug after everything
                // else is right.
                DirectTMPBrand.Note(
                    DirectTMPText.Raw(
                        $"This font has no joined shapes for {unjoined}. Persian adds those letters to the Arabic alphabet and they live in a different Unicode block (U+FB50–FBFF) from the rest of it (U+FE70–FEFF) — a font can carry one block in full and none of the other, which is why the rest of the word joins and these do not. They are drawn on their own; ک and ی are stood in for automatically. To join everything, use a font that carries both blocks.",
                        $"このフォントには {unjoined} の接続形がありません。ペルシア語がアラビア文字に追加したこれらの文字は、他の文字(U+FE70–FEFF)とは別のブロック(U+FB50–FBFF)にあります。一方のブロックだけを収録したフォントは珍しくなく、そのため語の他の部分は接続してこれらだけが接続しません。該当文字は単独字形で描画されます(ک と ی は自動で代替)。すべて接続させるには、両方のブロックを持つフォントを使ってください。",
                        $"این فونت شکلِ چسبیده‌ی {unjoined} رو نداره. این حروف رو فارسی به الفبای عربی اضافه کرده و توی یه بلوک یونیکد جدا (U+FB50–FBFF) از بقیه‌ی حروف (U+FE70–FEFF) هستن — خیلی از فونت‌ها یکی از این دو بلوک رو کامل دارن و اون یکی رو اصلاً ندارن، برای همین بقیه‌ی کلمه می‌چسبه و همین چند حرف نه. این‌ها جدا نوشته می‌شن (برای ک و ی خودکار جایگزین گذاشته می‌شه). اگه می‌خوای همه‌چیز بچسبه، فونتی بردار که هر دو بلوک رو داشته باشه."),
                    DirectTMPBrand.NoteLevel.Warning);
            }

            serializedObject.Update();

            EditorGUILayout.PropertyField(direction, DirectTMPText.C(
                "Direction", "文字方向", "جهت متن",
                "Which way paragraphs run. Auto decides per paragraph from its own first strong letter; force Right To Left when a Persian line can open with a Latin word or a number, which Auto would otherwise read as a left-to-right sentence.",
                "段落の方向です。Auto は各段落の最初の強い文字から判定します。ペルシア語の行がラテン文字や数字で始まりうる場合は Right To Left を指定してください（Auto では左から右の文と判定されます）。",
                "جهت پاراگراف‌ها. حالت Auto از روی اولین حرفِ جهت‌دارِ خود متن تصمیم می‌گیره؛ اگه ممکنه یه جمله‌ی فارسی با کلمه‌ی لاتین یا عدد شروع بشه، Right To Left رو دستی بذار — وگرنه Auto اون رو یه جمله‌ی چپ‌به‌راست می‌خونه."));

            EditorGUILayout.PropertyField(alignToDirection, DirectTMPText.C(
                "Align To Direction", "方向に合わせて揃える", "چینش بر اساس جهت",
                "Flip a LEFT-aligned label to right-aligned while it is showing right-to-left text, and back again when it is not. Centred, justified and deliberately right-aligned labels are never touched.",
                "右から左のテキストを表示している間、LEFT 揃えのラベルを右揃えにし、そうでなくなったら戻します。中央揃え・両端揃え・元から右揃えのラベルには一切触れません。",
                "تا وقتی متن راست‌به‌چپه، لیبلی که چینشش LEFT هست رو راست‌چین می‌کنه و بعد برمی‌گردونه. لیبل‌های وسط‌چین، جاستیفای و اونایی که خودت راست‌چین کردی اصلاً دست نمی‌خورن."));

            EditorGUILayout.PropertyField(fixWrappedLines, DirectTMPText.C(
                "Fix Wrapped Lines", "折り返し行を修正", "اصلاح خطوط شکسته",
                "Re-break wrapped paragraphs so their lines read top to bottom. Reordered text cannot be wrapped — the last word of the paragraph becomes the leftmost thing on the line — so the label is laid out twice: once in reading order to find the breaks, then once with each line reordered on its own. Turn it off only for labels that never wrap.",
                "折り返した段落を組み直し、行が上から下に読めるようにします。並べ替えたテキストは折り返せません（段落の最後の語が行の左端に来るため）。そのためラベルを2回レイアウトします — まず読み順で改行位置を求め、次に各行を個別に並べ替えます。折り返さないラベルでのみオフにしてください。",
                "پاراگراف‌های شکسته‌شده رو دوباره می‌شکنه تا خط‌ها از بالا به پایین خونده بشن. متنِ مرتب‌شده رو نمی‌شه شکست — آخرین کلمه‌ی پاراگراف می‌ره سمت چپ خط — برای همین لیبل دو بار چیده می‌شه: اول به ترتیب خوندن برای پیدا کردن محل شکستن، بعد با هر خط که جدا مرتب شده. فقط برای لیبل‌هایی که هیچ‌وقت نمی‌شکنن خاموشش کن."));

            EditorGUILayout.PropertyField(substituteMissingGlyphs, DirectTMPText.C(
                "Substitute Missing Glyphs", "無いグリフを置き換え", "جایگزینی گلیف‌های نبود",
                "When the font has no glyph for a joined form, fall back to the isolated form and then to the plain letter, instead of drawing a box. Modern Persian and Arabic faces (Vazirmatn, Noto Sans Arabic) often carry no presentation forms at all — they keep their joining rules in OpenType.",
                "接続形のグリフがフォントに無い場合、豆腐を描く代わりに孤立形へ、さらに素の文字へフォールバックします。最近のペルシア語・アラビア語書体(Vazirmatn、Noto Sans Arabic など)は表示形を持たず、接続規則を OpenType 側に持っていることがよくあります。",
                "اگه فونت گلیفِ شکل چسبیده رو نداشته باشه، به‌جای مربع اول به شکل جدا و بعد به خودِ حرف برمی‌گرده. فونت‌های امروزی فارسی و عربی (وزیرمتن، Noto Sans Arabic) معمولاً اصلاً شکل‌های نمایشی ندارن و قواعد چسبیدنشون توی OpenType هست."));

            serializedObject.ApplyModifiedProperties();

            if (!EditorApplication.isPlaying && GUILayout.Button(DirectTMPText.Word("refresh")))
            {
                foreach (Object each in targets)
                {
                    if (each is DirectText label) { label.Refresh(); }
                }
            }
        }
    }
}
