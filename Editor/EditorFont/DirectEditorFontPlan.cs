// ==========================================
// DirectEditorFontPlan
// What "change the Editor's font" means, stated as
// data, and the rules that decide whether a given
// request is safe to carry out.
//
// This file deliberately contains no Unity calls that
// touch the Editor's live GUI. It is the half of the
// feature that can be reasoned about and tested: which
// source a font came from, how a size delta resolves,
// and - the important one - whether applying this font
// would leave somebody with an Editor they cannot read
// their way out of.
//
// That last rule is why this is a separate file rather
// than a few ifs inside the patcher.
//
// Changing the Editor's UI font is the one feature in
// this package that can brick the tool that undoes it.
// Point Unity's chrome at a font with no Latin in it -
// a CJK-only subset, an icon font, a Persian display
// face that skipped ASCII - and every menu, every
// button and every label becomes a row of empty boxes,
// including the menu item labelled "Revert to Unity
// Default". The user's remaining options at that point
// are to count menu positions from memory or to go
// hunting through EditorPrefs.
//
// So the plan is validated before anything is touched,
// and a font that cannot draw the Editor's own
// interface is refused with an explanation rather than
// applied with a warning. A warning is something you
// read after the text stopped rendering.
// ==========================================
using System.Collections.Generic;

namespace UnityDirectTMP.Editor
{
    /// <summary>Where the Editor's replacement UI font comes from.</summary>
    internal enum DirectEditorFontSource
    {
        /// <summary>Unity's own font. The tool is not overriding anything.</summary>
        UnityDefault = 0,

        /// <summary>A Font asset imported into this project's Assets folder.</summary>
        ProjectFont = 1,

        /// <summary>One of the operating system's installed font families, by name.</summary>
        SystemFont = 2
    }

    /// <summary>
    /// Everything that can be wrong with a request to restyle the Editor.
    /// Ordered by severity: <see cref="NoLatinCoverage"/> is the one that is
    /// never merely advisory.
    /// </summary>
    internal enum DirectEditorFontProblem
    {
        None = 0,

        /// <summary>A source was chosen but no font was named.</summary>
        NoFontChosen,

        /// <summary>The Font asset the plan points at is gone.</summary>
        MissingAsset,

        /// <summary>The OS has no family by that name any more.</summary>
        MissingSystemFamily,

        /// <summary>
        /// The Font asset is imported with a baked character set rather than
        /// as a dynamic font, so it can only ever draw the characters that
        /// were baked in. Fixable in one click; not fatal.
        /// </summary>
        NotDynamic,

        /// <summary>
        /// The font cannot draw basic Latin. Applying it would make the Editor
        /// - including the command that undoes this - unreadable. Refused.
        /// </summary>
        NoLatinCoverage
    }

    /// <summary>
    /// An immutable description of the Editor font override the user has
    /// asked for. Serialized into EditorPrefs by
    /// <see cref="DirectEditorFontSettings"/> and carried out by
    /// <see cref="DirectEditorFont"/>.
    /// </summary>
    internal readonly struct DirectEditorFontPlan
    {
        public readonly bool Enabled;
        public readonly DirectEditorFontSource Source;

        /// <summary>Project-relative asset path, when the source is a project font.</summary>
        public readonly string AssetPath;

        /// <summary>OS family name, when the source is a system font.</summary>
        public readonly string SystemFamily;

        /// <summary>
        /// Points added to (or taken off) every style's font size. Zero leaves
        /// Unity's sizing alone, which is what most people want - the reason to
        /// change the font is usually the script it covers, not its size.
        /// </summary>
        public readonly int SizeDelta;

        public DirectEditorFontPlan(bool enabled, DirectEditorFontSource source, string assetPath, string systemFamily, int sizeDelta)
        {
            Enabled = enabled;
            Source = source;
            AssetPath = assetPath ?? string.Empty;
            SystemFamily = systemFamily ?? string.Empty;
            SizeDelta = DirectEditorFontRules.ClampSizeDelta(sizeDelta);
        }

        /// <summary>The do-nothing plan: Unity's own font, untouched.</summary>
        public static DirectEditorFontPlan Off =>
            new DirectEditorFontPlan(false, DirectEditorFontSource.UnityDefault, null, null, 0);

        /// <summary>True when this plan would actually change anything.</summary>
        public bool IsActive => Enabled && Source != DirectEditorFontSource.UnityDefault;

        /// <summary>
        /// A one-line description for the Settings page and the About window,
        /// so "is my Editor restyled right now?" is answerable without opening
        /// the font window.
        /// </summary>
        public string Describe()
        {
            if (!IsActive) { return DirectTMPText.L("Unity default", "Unity 標準", "پیش‌فرض یونیتی"); }

            string name = Source == DirectEditorFontSource.SystemFont
                ? SystemFamily
                : System.IO.Path.GetFileNameWithoutExtension(AssetPath);

            if (SizeDelta == 0) { return name; }
            return name + (SizeDelta > 0 ? " +" : " ") + SizeDelta;
        }

        public override string ToString() => Describe();
    }

    /// <summary>
    /// The rules. Pure functions over a plan and a coverage probe - no
    /// EditorPrefs, no GUIStyle, nothing that needs a running Editor.
    /// </summary>
    internal static class DirectEditorFontRules
    {
        /// <summary>
        /// How far the size may be nudged. Wider than most people need in
        /// either direction, and bounded because a delta of -20 renders the
        /// Editor at zero point something and a delta of +40 puts one menu
        /// item on the screen.
        /// </summary>
        public const int MinSizeDelta = -4;
        public const int MaxSizeDelta = 10;

        /// <summary>
        /// The size Unity draws most chrome at. Only used when a style reports
        /// a font size of 0, which is IMGUI's way of saying "whatever the font
        /// says" - there is no number to add a delta to in that case, so this
        /// stands in as the base.
        /// </summary>
        public const int BaseFontSize = 12;

        /// <summary>Smallest size any style is allowed to end up at.</summary>
        public const int MinResolvedSize = 6;

        /// <summary>
        /// The characters the Editor's own interface is made of. If a font
        /// cannot draw these, it cannot draw the menu that switches it back
        /// off, which is the whole reason this list exists.
        ///
        /// Digits are in there because the Inspector is mostly numbers, and
        /// the punctuation because a Project window without '/' or '.' is not
        /// a Project window.
        /// </summary>
        public static readonly int[] EditorChromeProbe =
        {
            'A', 'M', 'Z', 'a', 'g', 'm', 'z',
            '0', '5', '9',
            '.', '/', ':', '-', '_', '(', ')', ' '
        };

        /// <summary>Forces a size delta back inside the allowed range.</summary>
        public static int ClampSizeDelta(int delta)
        {
            if (delta < MinSizeDelta) { return MinSizeDelta; }
            if (delta > MaxSizeDelta) { return MaxSizeDelta; }
            return delta;
        }

        /// <summary>
        /// The size a style should end up at. A style whose size is 0 is
        /// inheriting from the font, so the delta is applied to
        /// <see cref="BaseFontSize"/> instead - and a delta of zero leaves the
        /// 0 alone, so styles that were inheriting keep inheriting.
        /// </summary>
        public static int ResolveFontSize(int currentSize, int delta)
        {
            if (delta == 0) { return currentSize; }

            int baseSize = currentSize > 0 ? currentSize : BaseFontSize;
            int resolved = baseSize + delta;
            return resolved < MinResolvedSize ? MinResolvedSize : resolved;
        }

        /// <summary>
        /// Can this font draw the Editor's own interface? Every probe
        /// character has to be present - not most of them. A font missing a
        /// single digit turns every number field into a gap.
        /// </summary>
        public static bool CanDrawEditorChrome(System.Func<int, bool> hasCodepoint)
        {
            if (hasCodepoint == null) { return false; }

            for (int i = 0; i < EditorChromeProbe.Length; i++)
            {
                // A font is allowed not to have a glyph for the space
                // character - plenty encode it as an advance width with no
                // outline, and some cmaps skip it entirely.
                if (EditorChromeProbe[i] == ' ') { continue; }
                if (!hasCodepoint(EditorChromeProbe[i])) { return false; }
            }
            return true;
        }

        /// <summary>
        /// Which probe characters are missing. The window lists them, because
        /// "this font cannot draw the Editor" is an assertion and
        /// "it has no digits" is a reason.
        /// </summary>
        public static List<string> MissingChromeCharacters(System.Func<int, bool> hasCodepoint)
        {
            var missing = new List<string>();
            if (hasCodepoint == null) { return missing; }

            for (int i = 0; i < EditorChromeProbe.Length; i++)
            {
                int codepoint = EditorChromeProbe[i];
                if (codepoint == ' ') { continue; }
                if (!hasCodepoint(codepoint)) { missing.Add(char.ConvertFromUtf32(codepoint)); }
            }
            return missing;
        }

        /// <summary>
        /// Validates a plan against the world it will run in.
        ///
        /// The three callbacks are the world: does this asset exist, is it a
        /// dynamic font, is this OS family installed, and what does the font
        /// actually contain. Passing them in rather than reaching for
        /// AssetDatabase keeps every rule above testable with three lambdas.
        /// </summary>
        public static DirectEditorFontProblem Validate(
            DirectEditorFontPlan plan,
            System.Func<string, bool> assetExists,
            System.Func<string, bool> assetIsDynamic,
            System.Func<string, bool> systemFamilyExists,
            System.Func<int, bool> hasCodepoint)
        {
            if (!plan.IsActive) { return DirectEditorFontProblem.None; }

            if (plan.Source == DirectEditorFontSource.ProjectFont)
            {
                if (string.IsNullOrEmpty(plan.AssetPath)) { return DirectEditorFontProblem.NoFontChosen; }
                if (assetExists != null && !assetExists(plan.AssetPath)) { return DirectEditorFontProblem.MissingAsset; }
            }
            else if (plan.Source == DirectEditorFontSource.SystemFont)
            {
                if (string.IsNullOrEmpty(plan.SystemFamily)) { return DirectEditorFontProblem.NoFontChosen; }
                if (systemFamilyExists != null && !systemFamilyExists(plan.SystemFamily)) { return DirectEditorFontProblem.MissingSystemFamily; }
            }

            // Coverage is checked before the import mode, because an
            // unreadable Editor is a worse outcome than a font that can only
            // draw the characters somebody baked into it - and because a
            // non-dynamic font that DOES cover the chrome is perfectly usable
            // for exactly the Editor, which only ever draws Latin.
            if (hasCodepoint != null && !CanDrawEditorChrome(hasCodepoint))
            {
                return DirectEditorFontProblem.NoLatinCoverage;
            }

            if (plan.Source == DirectEditorFontSource.ProjectFont
                && assetIsDynamic != null
                && !assetIsDynamic(plan.AssetPath))
            {
                return DirectEditorFontProblem.NotDynamic;
            }

            return DirectEditorFontProblem.None;
        }

        /// <summary>
        /// True when a problem must stop the apply rather than merely warn
        /// about it. Only one problem is advisory - a non-dynamic font still
        /// draws, it just cannot grow new glyphs - and everything else means
        /// there is nothing valid to apply.
        /// </summary>
        public static bool IsFatal(DirectEditorFontProblem problem)
        {
            return problem != DirectEditorFontProblem.None
                && problem != DirectEditorFontProblem.NotDynamic;
        }

        /// <summary>A human explanation of a problem, in the current language.</summary>
        public static string Describe(DirectEditorFontProblem problem)
        {
            switch (problem)
            {
                case DirectEditorFontProblem.NoFontChosen:
                    return DirectTMPText.L(
                        "Pick a font first.",
                        "先にフォントを選んでください。",
                        "اول یک فونت انتخاب کن.");

                case DirectEditorFontProblem.MissingAsset:
                    return DirectTMPText.L(
                        "That Font asset is no longer in the project.",
                        "その Font アセットはプロジェクトにありません。",
                        "اون فایل فونت دیگه توی پروژه نیست.");

                case DirectEditorFontProblem.MissingSystemFamily:
                    return DirectTMPText.L(
                        "This machine has no installed font by that name.",
                        "この PC にその名前のフォントは入っていません。",
                        "روی این سیستم فونتی با این اسم نصب نیست.");

                case DirectEditorFontProblem.NotDynamic:
                    return DirectTMPText.L(
                        "This Font asset is imported with a fixed character set, so it can only draw the characters baked into it. Switch its import mode to Dynamic for the full font.",
                        "この Font アセットは文字セットを固定して取り込まれているため、焼き込まれた文字しか描画できません。インポート設定を Dynamic に変えると全文字が使えます。",
                        "این فونت با یک مجموعه‌کاراکتر ثابت ایمپورت شده، پس فقط همون‌ها رو می‌تونه بکشه. حالت ایمپورتش رو بذار Dynamic تا کل فونت در دسترس باشه.");

                case DirectEditorFontProblem.NoLatinCoverage:
                    return DirectTMPText.L(
                        "This font cannot draw the Editor's own interface (it is missing basic Latin letters, digits or punctuation). Applying it would leave every menu — including the one that undoes this — as empty boxes, so it is refused.",
                        "このフォントは Editor の UI を描画できません(基本ラテン文字・数字・記号が不足しています)。適用するとメニューがすべて豆腐になり、元に戻す操作もできなくなるため、適用を拒否しました。",
                        "این فونت نمی‌تونه رابط خود یونیتی رو بکشه (حروف پایه‌ی لاتین، عدد یا علائم رو نداره). اگه اعمال بشه همه‌ی منوها — از جمله همونی که این کار رو برمی‌گردونه — مربع خالی می‌شن، برای همین اعمال نشد.");

                default:
                    return string.Empty;
            }
        }
    }
}
