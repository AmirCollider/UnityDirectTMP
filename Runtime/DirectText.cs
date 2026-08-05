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
// The hard part, and the reason this is a component
// rather than one call.
//
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
// TextMeshPro's job, not this package's. So the text
// is laid out twice:
//
//   1. shaped, still in reading order. TMP measures it
//      and reports where the lines fell.
//   2. each of those lines reordered on its own, with
//      the breaks made explicit.
//
// Two layouts per text change, and only for text that
// is actually right-to-left. Anything else returns at
// the first scan.
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
        // How many frames to keep asking for a layout that is not there yet
        // before settling for a single-paragraph preparation. Bounded, so a
        // label that can never be measured does not re-prepare itself for the
        // rest of the session.
        private const int MeasureRetries = 3;

        // The letters Persian adds to the Arabic alphabet. They shape into a
        // different Unicode block from the rest of the alphabet, and plenty
        // of fonts carry one block and not the other - so these six are the
        // ones worth checking, and the ones a reader notices.
        private static readonly char[] PersianLetters = { 'پ', 'چ', 'ژ', 'گ', 'ک', 'ی' };

        // One warning per font, not per label: a Persian screen has fifty.
        private static readonly HashSet<string> s_warned = new HashSet<string>();

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
        [NonSerialized] private DirectTextPreprocessor _processor;
        [NonSerialized] private ITextPreprocessor _replaced;
        [NonSerialized] private Func<char, bool> _probe;
        [NonSerialized] private readonly Dictionary<char, bool> _glyphs = new Dictionary<char, bool>();
        [NonSerialized] private readonly List<int> _breaks = new List<int>();

        [NonSerialized] private string _source;
        [NonSerialized] private TMP_FontAsset _font;
        [NonSerialized] private bool _busy;
        [NonSerialized] private bool _dirty = true;
        [NonSerialized] private bool _wrapped;
        [NonSerialized] private bool _skipped;
        [NonSerialized] private int _retries;
        [NonSerialized] private bool _realigned;
        [NonSerialized] private string _unjoined;
        [NonSerialized] private DirectFontFormsReport _forms;
        [NonSerialized] private HorizontalAlignmentOptions _alignmentBefore;

        // ==========================================
        // Public API
        // ==========================================

        /// <summary>The TMP component this drives (found on the same GameObject).</summary>
        public TMP_Text Text => _text != null ? _text : (_text = GetComponent<TMP_Text>());

        /// <summary>Which way this label lays its paragraphs out.</summary>
        public DirectTextDirection Direction
        {
            get => direction;
            set { direction = value; _dirty = true; }
        }

        /// <summary>Re-prepare the label now, rather than at the end of the frame.</summary>
        public void Refresh()
        {
            _dirty = true;
            Prepare();
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

            _probe = HasGlyph;
            Install();

            _dirty = true;
            Prepare();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += EditorTick;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorTick;
#endif
            Restore();
            Uninstall();
        }

        private void LateUpdate()
        {
            Tick();
        }

        // A label that changed width has to be broken again: the breaks baked
        // into it were chosen for the width it used to be. A label that has
        // just been given a layout for the first time is the other half of
        // this - it is the moment there is finally something to measure.
        private void OnRectTransformDimensionsChange()
        {
            if (_wrapped || _retries > 0) { _dirty = true; }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _dirty = true;
        }

        // ExecuteAlways gets Update when the scene changes, which is not the
        // same as "whenever the text changed" - a localisation window or an
        // Inspector edit on another object can both do it silently.
        private void EditorTick()
        {
            if (this == null)
            {
                UnityEditor.EditorApplication.update -= EditorTick;
                return;
            }

            if (Application.isPlaying || !isActiveAndEnabled) { return; }
            Tick();
        }
#endif

        private void Tick()
        {
            if (_processor == null || _text == null) { return; }
            if (_busy) { return; }

            if (_dirty
                || !string.Equals(_text.text, _source, StringComparison.Ordinal)
                || !ReferenceEquals(_text.font, _font))
            {
                Prepare();
            }
        }

        // ==========================================
        // Install / uninstall
        //
        // The preprocessor is a live property rather than
        // a serialized one, so it has to be re-attached
        // on every enable - and taken off again on
        // disable, so a label with this component
        // switched off is exactly the label it was
        // before.
        // ==========================================
        private void Install()
        {
            if (_text == null || _processor != null) { return; }

            if (IsInputFieldText())
            {
                // An input field maps the caret and the selection through the
                // character positions of its text component. Reordering that
                // text puts the caret in the wrong place and typing edits the
                // wrong end of the word, so this is a refusal, not a bug.
                DirectTMPLog.Warn(
                    "Direct Text does not shape the text component of a TMP_InputField — reordering it would put the caret in the wrong place. The field's placeholder is safe to shape.",
                    this);
                _skipped = true;
                return;
            }

            ITextPreprocessor existing = _text.textPreprocessor;
            _replaced = existing is DirectTextPreprocessor ? null : existing;

            _processor = new DirectTextPreprocessor { Inner = _replaced };
            _text.textPreprocessor = _processor;
        }

        private void Uninstall()
        {
            if (_text != null && ReferenceEquals(_text.textPreprocessor, _processor))
            {
                _text.textPreprocessor = _replaced;
                _text.havePropertiesChanged = true;
                _text.SetAllDirty();
            }

            _processor = null;
            _replaced = null;
            _source = null;
            _glyphs.Clear();
        }

        private bool IsInputFieldText()
        {
            var field = GetComponentInParent<TMP_InputField>();
            return field != null && field.textComponent == _text;
        }

        // ==========================================
        // Prepare
        //
        // The whole job, in the order it has to happen.
        // ==========================================
        private void Prepare()
        {
            if (_processor == null || _text == null || _busy) { return; }

            _busy = true;
            try
            {
                string source = _text.text ?? string.Empty;

                if (!ReferenceEquals(_text.font, _font))
                {
                    _font = _text.font;
                    _glyphs.Clear();

                    // Before anything asks this font what it can draw: give it
                    // the joined shapes it already has but has no codepoint
                    // for. A font carrying the Arabic presentation block and
                    // not the Persian one - Segoe UI, and most of Windows -
                    // gains پ چ ژ گ ک ی here, and every probe below then
                    // answers yes for them. Fonts that need nothing pay one
                    // lookup per form and no allocation.
                    if (substituteMissingGlyphs) { _forms = DirectFontForms.TopUp(_font); }
                }
                else if (substituteMissingGlyphs && _forms != null && _forms.Retryable)
                {
                    // The first top-up of a label can run before TextMeshPro
                    // has an atlas or the FontEngine has a face, and fail for
                    // that reason alone. TopUp retries; the probe cache did
                    // not, so a form that arrived on the second attempt was
                    // still remembered as missing for the rest of the session -
                    // and one letter remembered as missing un-joins its
                    // neighbour.
                    int before = _forms.FormsAdded;
                    _forms = DirectFontForms.TopUp(_font);
                    if (_forms.FormsAdded != before) { _glyphs.Clear(); }
                }

                // New text is a fresh start for the "is there a layout yet?"
                // budget below.
                if (!string.Equals(source, _source, StringComparison.Ordinal)) { _retries = 0; }

                _source = source;
                _dirty = false;
                _wrapped = false;

                _processor.ParagraphDirection = ResolveDirection();
                _processor.GlyphProbe = substituteMissingGlyphs ? _probe : null;

                // Nothing right-to-left in it, or text somebody already
                // prepared by hand - either way, out of our way.
                if (!DirectRichText.NeedsPreparing(source) || DirectDisplayText.LooksPrepared(source))
                {
                    // Served as-is rather than left unserved: an unserved
                    // string would fall through to the preprocessor's own
                    // preparation, and text that is already prepared must not
                    // be prepared twice.
                    _processor.Serve(source, source);
                    Realign(false);
                    Reparse();
                    return;
                }

                Func<char, bool> probe = substituteMissingGlyphs ? _probe : null;
                ReportUnjoinableLetters(probe);

                string shaped = DirectRichText.Shape(source, probe);
                string prepared = null;

                if (fixWrappedLines)
                {
                    // Pass one: reading order, so TextMeshPro's own line
                    // breaking answers where the lines fall.
                    _processor.Serve(source, shaped);
                    Reparse();

                    if (Measured())
                    {
                        List<int> breaks = SoftBreaks(shaped);
                        _wrapped = breaks.Count > 0;
                        _retries = 0;

                        prepared = _wrapped
                            ? DirectRichText.PrepareAtBreaks(shaped, breaks, _processor.ParagraphDirection, probe)
                            : DirectRichText.Prepare(shaped, _processor.ParagraphDirection, probe);
                    }
                    else
                    {
                        // No layout to read yet - a label being built in
                        // Awake, or one whose TMP component has not been
                        // enabled. Prepare it as one paragraph and come back
                        // for the line breaks once there are some.
                        _dirty = _retries++ < MeasureRetries;
                    }
                }

                if (prepared == null)
                {
                    prepared = DirectRichText.Prepare(shaped, _processor.ParagraphDirection, probe);
                }

                _processor.Serve(source, prepared);
                Realign(IsRightToLeft(source));
                Reparse();
            }
            finally
            {
                _busy = false;
            }
        }

        private void Reparse()
        {
            _text.havePropertiesChanged = true;
            _text.ForceMeshUpdate(false, true);
        }

        // Did the pass we just asked for actually produce a layout? A label
        // being built in Awake, or one whose TMP component has not been
        // enabled yet, has nothing to read line breaks out of.
        private bool Measured()
        {
            TMP_TextInfo info = _text.textInfo;
            return info != null && info.characterCount > 0;
        }

        // ==========================================
        // Where TextMeshPro put the lines
        //
        // Every line after the first starts at some
        // character, and every character knows its index
        // in the string TMP was given - which is the
        // shaped text. That index is the break.
        // ==========================================
        private List<int> SoftBreaks(string shaped)
        {
            _breaks.Clear();

            TMP_TextInfo info = _text.textInfo;
            if (info == null || info.lineCount <= 1 || info.characterCount == 0) { return _breaks; }

            int lines = Mathf.Min(info.lineCount, info.lineInfo != null ? info.lineInfo.Length : 0);
            for (int i = 1; i < lines; i++)
            {
                int first = info.lineInfo[i].firstCharacterIndex;
                if (first <= 0 || first >= info.characterCount) { continue; }

                int index = info.characterInfo[first].index;
                if (index <= 0 || index >= shaped.Length) { continue; }
                if (_breaks.Count > 0 && index <= _breaks[_breaks.Count - 1]) { continue; }

                _breaks.Add(index);
            }

            return _breaks;
        }

        // ==========================================
        // Direction and alignment
        // ==========================================
        private int ResolveDirection()
        {
            switch (direction)
            {
                case DirectTextDirection.RightToLeft: return DirectBidi.RightToLeft;
                case DirectTextDirection.LeftToRight: return DirectBidi.LeftToRight;
                default: return DirectBidi.AutoDirection;
            }
        }

        private bool IsRightToLeft(string source)
        {
            switch (direction)
            {
                case DirectTextDirection.RightToLeft: return true;
                case DirectTextDirection.LeftToRight: return false;
                default: return DirectBidi.IsRightToLeftParagraph(source);
            }
        }

        // Only the one flip, and only back again. A label somebody centred
        // stays centred; a label somebody deliberately set to Right stays
        // Right when its text turns out to be English.
        private void Realign(bool rightToLeft)
        {
            if (!alignToDirection || _text == null) { return; }

            if (rightToLeft)
            {
                if (_realigned || _text.horizontalAlignment != HorizontalAlignmentOptions.Left) { return; }

                _alignmentBefore = _text.horizontalAlignment;
                _text.horizontalAlignment = HorizontalAlignmentOptions.Right;
                _realigned = true;
                return;
            }

            Restore();
        }

        private void Restore()
        {
            if (!_realigned || _text == null) { return; }

            if (_text.horizontalAlignment == HorizontalAlignmentOptions.Right)
            {
                _text.horizontalAlignment = _alignmentBefore;
            }

            _realigned = false;
        }

        // ==========================================
        // Which Persian letters this font cannot join.
        //
        // پ چ ژ ک گ ی shape into U+FB50..FBFF and the
        // rest of the alphabet shapes into U+FE70..FEFF.
        // A font can carry the second block in full and
        // none of the first - Segoe UI does, and it is on
        // every Windows machine - so a Persian word comes
        // out joined except for the letters that make it
        // Persian. ک and ی have stand-ins that any Arabic
        // font has; پ چ ژ گ do not, and the honest thing
        // is to say so rather than let somebody conclude
        // the package is broken.
        // ==========================================
        private void ReportUnjoinableLetters(Func<char, bool> probe)
        {
            _unjoined = null;
            if (probe == null || _font == null) { return; }

            for (int i = 0; i < PersianLetters.Length; i++)
            {
                char letter = PersianLetters[i];
                if (DirectArabicShaper.CanJoin(letter, probe)) { continue; }

                _unjoined = _unjoined == null ? letter.ToString() : _unjoined + " " + letter;
            }

            if (_unjoined == null) { return; }

            string key = _font.name + ":" + _unjoined;
            if (!s_warned.Add(key)) { return; }

            // By the time this runs, DirectFontForms has already tried to take
            // the shapes out of the font's own OpenType tables, for this font
            // AND for every fallback behind it. Reaching here means all of
            // that found nothing.
            //
            // The per-font breakdown goes in the message rather than being
            // left for somebody to ask about, because the failure it explains
            // is invisible from the outside: the text is unjoined either way,
            // and nothing on screen distinguishes "this font has nothing to
            // give" from "the letters are coming from a fallback nobody
            // looked at". One line settles which.
            string why = _forms != null && !string.IsNullOrEmpty(_forms.Detail)
                ? "\nWhat each font in this label's chain had to offer:\n" + _forms.Detail
                : string.Empty;

            DirectTMPLog.Warn(
                $"'{_font.name}' has no joined shapes for {_unjoined} — those letters will be drawn on their own. "
                + "Persian's own letters shape into U+FB50–FBFF and the rest of the alphabet into U+FE70–FEFF, "
                + "and a font can carry one block and not the other. The fonts' own OpenType joining rules were "
                + "asked next and could not supply them either. Pick a font that carries both blocks, or one with "
                + "OpenType Arabic shaping — most Persian fonts have it."
                + why,
                this);
        }

        // ==========================================
        // Can this font draw that form?
        //
        // Cached per label, because the answer is a
        // property of the font asset and the question is
        // asked once per letter per text change.
        // ==========================================
        private bool HasGlyph(char c)
        {
            TMP_FontAsset font = _text != null ? _text.font : null;
            if (font == null) { return true; }

            if (_glyphs.TryGetValue(c, out bool has)) { return has; }

            // A form THIS package rasterized and wrote into the
            // character table is drawable, and no second opinion is
            // wanted. TextMeshPro is the right authority for a
            // codepoint that came out of the font's own cmap; it is not
            // the right authority for one we put there by glyph index,
            // where HasCharacter can answer no for reasons that have
            // nothing to do with this glyph - a table rebuilt between
            // the two calls, an atlas under pressure. And a no here is
            // never confined to one letter: a letter that cannot be
            // joined tells the letter beside it not to reach for it, so
            // one wrong answer takes a second letter's shape with it.
            if (_forms != null && _forms.Supplied(c))
            {
                _glyphs[c] = true;
                return true;
            }

            has = font.HasCharacter(c, true, true);
            _glyphs[c] = has;
            return has;
        }

        // ==========================================
        // Editor-facing accessors, so the Inspector can
        // read state without the fields being public.
        // ==========================================
        internal bool EditorSkipped => _skipped;
        internal bool EditorWrapped => _wrapped;

        /// <summary>The Persian letters this label's font cannot join, or null.</summary>
        internal string EditorUnjoinedLetters => _unjoined;

        /// <summary>What was taken out of the font's own OpenType tables for it, or null.</summary>
        internal DirectFontFormsReport EditorForms => _forms;
    }
}
