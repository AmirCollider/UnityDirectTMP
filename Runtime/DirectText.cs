// ==========================================
// DirectText
// The second half of "and every language just works".
//
// Direct Font gives a label every glyph its font file
// contains, which is what stops Persian showing up as
// □□□. It does not make Persian READ, and nothing else
// in Unity does either: TextMeshPro hands each
// codepoint to the font in the order it is stored, so
// "کریم" arrives as four disconnected letters running
// the wrong way. Every glyph is present and the word
// is still wrong. ی and ر get the blame in the reports
// because their isolated shapes look least like their
// joined ones; every letter is affected equally.
//
// Drop this next to any TextMeshProUGUI / TextMeshPro
// and the label draws Persian, Arabic, Urdu and Hebrew
// correctly. Nothing else changes: the text you set is
// still the text you get back, `label.text` is still
// the string you stored, and a label with nothing
// right-to-left in it pays one scan and no allocation.
//
// ==========================================
// WHERE THE WORK ACTUALLY IS
// ==========================================
// Not here. Since 1.3.0 the whole pipeline lives in
// DirectTextEngine, which is a plain class, and this
// component is the four serialized options plus a
// lifecycle. That split is what lets the same
// behaviour be applied to a whole project without
// adding a component to anything - see
// DirectGlobalFont - and it is why a project-wide
// setting survives a scene reload: there is nothing in
// the scene to survive.
//
// ==========================================
// HOW IT HOOKS IN
// ==========================================
// Through TMP_Text.textPreprocessor, which TextMeshPro
// calls with the source string every time it parses.
// That matters more than it sounds:
//
//   * `label.text` keeps the string you assigned, in
//     logical order. Read it, search it, save it - it
//     is your string, not a reordered one.
//   * Everything that writes to a label is covered
//     without knowing this component exists: a
//     TMP_Dropdown writing its caption, a localisation
//     package, a coroutine typing one character at a
//     time.
//
// ==========================================
// WRAPPING
// ==========================================
// Reordered text cannot be wrapped. Reordering makes
// the LAST word of a paragraph the leftmost thing on
// the line, so when the renderer breaks that line into
// three, the three come out bottom to top and the
// paragraph reads upwards. Every wrapped right-to-left
// label in Unity has this bug, and it is invisible
// until a sentence gets long enough to fold.
//
// The breaks have to be chosen in reading order, and
// choosing them needs glyph widths - which is
// TextMeshPro's job. So the text is laid out twice:
// shaped and still in reading order, then again with
// each of the lines TMP reported reordered on its own.
// ==========================================
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// Which way a label's paragraphs run.
    /// </summary>
    public enum DirectTextDirection
    {
        /// <summary>Decided per paragraph by its own first strong character.</summary>
        Auto = 0,

        /// <summary>Always right-to-left, even when the line opens with a Latin word or a number.</summary>
        RightToLeft = 1,

        /// <summary>Always left-to-right.</summary>
        LeftToRight = 2
    }

    /// <summary>
    /// A TextMeshPro text preprocessor that joins Arabic-script letters and
    /// puts right-to-left text in reading order.
    ///
    /// Assign one to <see cref="TMP_Text.textPreprocessor"/> and the label
    /// draws correctly while <c>label.text</c> keeps the string you stored.
    /// <see cref="DirectText"/> does exactly that, and adds the line handling
    /// a wrapped paragraph needs; this class on its own is the single-line
    /// case, for a pipeline that wants no MonoBehaviour.
    /// </summary>
    public sealed class DirectTextPreprocessor : ITextPreprocessor
    {
        /// <summary>Paragraph direction passed to the reordering pass.</summary>
        public int ParagraphDirection = DirectBidi.AutoDirection;

        /// <summary>
        /// Optional probe asking whether the font can actually draw a given
        /// presentation form. See <see cref="DirectArabicShaper.Shape(string, Func{char, bool}, List{int})"/>.
        /// </summary>
        public Func<char, bool> GlyphProbe;

        /// <summary>A preprocessor that was already installed, run first.</summary>
        public ITextPreprocessor Inner;

        private string _source;
        private string _output;

        /// <summary>
        /// Serve <paramref name="output"/> whenever TextMeshPro parses
        /// <paramref name="source"/>. Passing null for either falls back to
        /// preparing the text on the spot.
        /// </summary>
        public void Serve(string source, string output)
        {
            _source = source;
            _output = output;
        }

        /// <inheritdoc/>
        public string PreprocessText(string text)
        {
            string input = Inner != null ? Inner.PreprocessText(text) : text;
            if (string.IsNullOrEmpty(input)) { return input; }

            // The prepared answer, when it was prepared from this exact
            // string. A label whose text changed since is served an honest
            // one-pass preparation now and the full treatment a frame later,
            // which is better than one frame of the previous label's words.
            if (_output != null && string.Equals(_source, input, StringComparison.Ordinal))
            {
                return _output;
            }

            // Text somebody already prepared by hand. Preparing it a second
            // time would reverse it back into nonsense - a bug that looks
            // exactly like the one this class exists to fix.
            if (DirectDisplayText.LooksPrepared(input)) { return input; }

            return DirectRichText.Prepare(input, ParagraphDirection, GlyphProbe);
        }
    }

    /// <summary>
    /// Makes a TextMeshPro label render Arabic-script and other right-to-left
    /// text correctly: letters joined, words in reading order, wrapped lines
    /// top to bottom.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Unity DirectTMP/Direct Text")]
    [HelpURL(DirectTMPConstants.GithubUrl)]
    public sealed class DirectText : MonoBehaviour
    {
        // ==========================================
        // Serialized configuration
        // ==========================================
        [Tooltip("Which way paragraphs run. Auto reads it from the text itself; force Right To Left when a Persian line can open with a Latin word or a number.")]
        [SerializeField] private DirectTextDirection direction = DirectTextDirection.Auto;

        [Tooltip("Flip a left-aligned label to right-aligned while it is showing right-to-left text. Centred and justified labels are never touched.")]
        [SerializeField] private bool alignToDirection = true;

        [Tooltip("Re-break wrapped paragraphs so their lines read top to bottom. Turn this off only for labels that never wrap — it costs one extra layout per text change.")]
        [SerializeField] private bool fixWrappedLines = true;

        [Tooltip("When the font has no glyph for a joined form, fall back to the plain letter instead of drawing a box. Fonts built for a real shaping engine often carry no presentation forms at all.")]
        [SerializeField] private bool substituteMissingGlyphs = true;

        // ==========================================
        // State, none of it serialized
        // ==========================================
        [NonSerialized] private TMP_Text _text;
        [NonSerialized] private DirectTextEngine _engine;

        // ==========================================
        // Public API
        // ==========================================

        /// <summary>The TMP component this drives (found on the same GameObject).</summary>
        public TMP_Text Text => _text != null ? _text : (_text = GetComponent<TMP_Text>());

        /// <summary>Which way this label lays its paragraphs out.</summary>
        public DirectTextDirection Direction
        {
            get => direction;
            set { direction = value; PushOptions(); }
        }

        /// <summary>Re-prepare the label now, rather than at the end of the frame.</summary>
        public void Refresh()
        {
            if (_engine == null) { return; }
            PushOptions();
            _engine.Prepare();
        }

        /// <summary>
        /// Adds a <see cref="DirectText"/> to a GameObject that has a TMP
        /// component and does not have one yet. Used by
        /// <see cref="DirectFont"/>, and safe to call repeatedly.
        /// </summary>
        public static DirectText EnsureOn(GameObject target)
        {
            if (target == null) { return null; }

            var existing = target.GetComponent<DirectText>();
            if (existing != null) { return existing; }

            if (target.GetComponent<TMP_Text>() == null) { return null; }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // A prefab sitting in the project has no scene and gets no
                // callbacks; adding to it here would be an edit nobody asked
                // for, at a moment nobody can see.
                if (!target.scene.IsValid()) { return null; }
                return UnityEditor.Undo.AddComponent<DirectText>(target);
            }
#endif
            return target.AddComponent<DirectText>();
        }

        // ==========================================
        // Unity lifecycle
        // ==========================================
        private void OnEnable()
        {
            _text = GetComponent<TMP_Text>();
            if (_text == null)
            {
                DirectTMPLog.Warn("Direct Text needs a TextMeshPro component on the same GameObject.", this);
                enabled = false;
                return;
            }

            // A label a project-wide setting is already driving must not be
            // driven twice: two preprocessors on one label means one of them
            // is preparing text the other already prepared.
            DirectGlobalFont.ReleaseShaping(_text);

            _engine = new DirectTextEngine(_text);
            PushOptions();
            _engine.Install();
            _engine.Prepare();
        }

        private void OnDisable()
        {
            if (_engine != null)
            {
                _engine.Uninstall();
                _engine = null;
            }
        }

        private void LateUpdate()
        {
            _engine?.Tick();
        }

        // A label that changed width has to be broken again: the breaks baked
        // into it were chosen for the width it used to be.
        private void OnRectTransformDimensionsChange()
        {
            _engine?.MarkDirty();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            PushOptions();
            _engine?.MarkDirty();
        }
#endif

        private void PushOptions()
        {
            if (_engine == null) { return; }
            _engine.Direction = direction;
            _engine.AlignToDirection = alignToDirection;
            _engine.FixWrappedLines = fixWrappedLines;
            _engine.SubstituteMissingGlyphs = substituteMissingGlyphs;
        }

        // ==========================================
        // Editor-facing accessors, so the Inspector can
        // read state without the fields being public.
        // ==========================================
        internal bool EditorSkipped => _engine != null && _engine.Skipped;
        internal bool EditorWrapped => _engine != null && _engine.Wrapped;

        /// <summary>The Persian letters this label's font cannot join, or null.</summary>
        internal string EditorUnjoinedLetters => _engine?.UnjoinedLetters;

        /// <summary>What was taken out of the font's own OpenType tables for it, or null.</summary>
        internal DirectFontFormsReport EditorForms => _engine?.Forms;
    }
}
