// ==========================================
// DirectTextEngine
// Everything Direct Text does, with no
// MonoBehaviour attached to it.
//
// This used to live inside the DirectText component,
// which made it reachable exactly one way: add a
// component to every label. That is a fine way to opt
// one label in and a poor way to make a project read
// correctly, because it has to be done - and kept
// done - on every label anybody ever adds.
//
// The work never needed a component. TextMeshPro's
// textPreprocessor is a plain C# property that is not
// serialized, so a label can be driven entirely from
// outside itself and the scene file never changes.
// Pulling the logic out here gives:
//
//   * DirectText, unchanged in behaviour, now a thin
//     component that owns settings and forwards them.
//   * DirectGlobalFont, which drives one of these per
//     label for a whole project without adding a
//     single component - and therefore without
//     anything to lose when a scene is reloaded.
//
// One ticker rather than one per label
// ------------------------------------
// The old component subscribed its own delegate to
// EditorApplication.update. A screen with two hundred
// labels was two hundred delegates, each doing a
// string comparison, on every idle editor frame. They
// are all registered with DirectTextTicker now, which
// is one delegate and one loop.
// ==========================================
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// Drives one TextMeshPro label: joins Arabic-script letters, puts
    /// right-to-left text in reading order, and re-breaks wrapped paragraphs
    /// so their lines read top to bottom. Owns no serialized state - the
    /// caller sets the four options and calls <see cref="Tick"/>.
    /// </summary>
    internal sealed class DirectTextEngine
    {
        // How many frames to keep asking for a layout that is not there yet
        // before settling for a single-paragraph preparation. Bounded, so a
        // label that can never be measured does not re-prepare itself for the
        // rest of the session.
        private const int MeasureRetries = 3;

        // The letters Persian adds to the Arabic alphabet. They shape into a
        // different Unicode block from the rest of the alphabet, and plenty of
        // fonts carry one block and not the other - so these six are the ones
        // worth checking, and the ones a reader notices.
        private static readonly char[] PersianLetters = { 'پ', 'چ', 'ژ', 'گ', 'ک', 'ی' };

        // One warning per font, not per label: a Persian screen has fifty.
        private static readonly HashSet<string> s_warned = new HashSet<string>();

        // ==========================================
        // Options, pushed in by the owner
        // ==========================================
        public DirectTextDirection Direction = DirectTextDirection.Auto;
        public bool AlignToDirection = true;
        public bool FixWrappedLines = true;
        public bool SubstituteMissingGlyphs = true;

        // ==========================================
        // State
        // ==========================================
        private readonly TMP_Text _text;
        private readonly Func<char, bool> _probe;
        private readonly Dictionary<char, bool> _glyphs = new Dictionary<char, bool>();
        private readonly List<int> _breaks = new List<int>();

        private DirectTextPreprocessor _processor;
        private ITextPreprocessor _replaced;

        private string _source;
        private TMP_FontAsset _font;
        private bool _busy;
        private bool _dirty = true;
        private bool _wrapped;
        private bool _skipped;
        private int _retries;
        private bool _realigned;
        private float _width = -1f;
        private string _unjoined;
        private DirectFontFormsReport _forms;
        private HorizontalAlignmentOptions _alignmentBefore;

        public DirectTextEngine(TMP_Text text)
        {
            _text = text;
            _probe = HasGlyph;
        }

        /// <summary>The label this engine drives. Never null while it is installed.</summary>
        public TMP_Text Text => _text;

        /// <summary>True when this label was deliberately left alone (a TMP_InputField's own text).</summary>
        public bool Skipped => _skipped;

        /// <summary>True when the last preparation had to re-break wrapped lines.</summary>
        public bool Wrapped => _wrapped;

        /// <summary>The Persian letters this label's font cannot join, or null.</summary>
        public string UnjoinedLetters => _unjoined;

        /// <summary>What was taken out of the font's own OpenType tables for it, or null.</summary>
        public DirectFontFormsReport Forms => _forms;

        /// <summary>True once <see cref="Install"/> has hooked the label's preprocessor.</summary>
        public bool Installed => _processor != null;

        /// <summary>Re-prepare on the next tick.</summary>
        public void MarkDirty() => _dirty = true;

        // ==========================================
        // Install / uninstall
        //
        // The preprocessor is a live property rather than
        // a serialized one, so it has to be re-attached
        // on every enable - and taken off again on
        // disable, so a label this engine let go of is
        // exactly the label it was before.
        // ==========================================
        /// <summary>
        /// Hooks the label's text preprocessor. Returns false when the label is
        /// deliberately refused - the text component inside a TMP_InputField,
        /// where reordering would put the caret in the wrong place.
        /// </summary>
        public bool Install(bool quiet = false)
        {
            if (_text == null || _processor != null) { return _processor != null; }

            if (IsInputFieldText())
            {
                // An input field maps the caret and the selection through the
                // character positions of its text component. Reordering that
                // text puts the caret in the wrong place and typing edits the
                // wrong end of the word, so this is a refusal, not a bug.
                if (!quiet)
                {
                    DirectTMPLog.Warn(
                        "Direct Text does not shape the text component of a TMP_InputField — reordering it would put the caret in the wrong place. The field's placeholder is safe to shape.",
                        _text);
                }
                _skipped = true;
                return false;
            }

            ITextPreprocessor existing = _text.textPreprocessor;
            _replaced = existing is DirectTextPreprocessor ? null : existing;

            _processor = new DirectTextPreprocessor { Inner = _replaced };
            _text.textPreprocessor = _processor;

            DirectTextTicker.Add(this);
            return true;
        }

        /// <summary>Puts the label back exactly as it was before <see cref="Install"/>.</summary>
        public void Uninstall()
        {
            DirectTextTicker.Remove(this);
            Restore();

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
            var field = _text.GetComponentInParent<TMP_InputField>();
            return field != null && field.textComponent == _text;
        }

        // ==========================================
        // Tick
        //
        // The three things that invalidate a
        // preparation: somebody asked for one, the text
        // changed, or the font changed. A label that
        // changed WIDTH is the fourth - the breaks baked
        // into it were chosen for the width it used to
        // be - and a component can be told that by Unity,
        // while a label driven from outside cannot, so it
        // is measured here for both.
        // ==========================================
        public void Tick()
        {
            if (_processor == null || _text == null) { return; }
            if (_busy) { return; }

            if (_wrapped || _retries > 0)
            {
                float width = CurrentWidth();
                if (!Mathf.Approximately(width, _width)) { _dirty = true; }
                _width = width;
            }

            if (_dirty
                || !string.Equals(_text.text, _source, StringComparison.Ordinal)
                || !ReferenceEquals(_text.font, _font))
            {
                Prepare();
            }
        }

        private float CurrentWidth()
        {
            RectTransform rect = _text.rectTransform;
            return rect != null ? rect.rect.width : -1f;
        }

        // ==========================================
        // Prepare
        //
        // The whole job, in the order it has to happen.
        // ==========================================
        public void Prepare()
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
                    if (SubstituteMissingGlyphs) { _forms = DirectFontForms.TopUp(_font); }
                }
                else if (SubstituteMissingGlyphs && _forms != null && _forms.Retryable)
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
                _processor.GlyphProbe = SubstituteMissingGlyphs ? _probe : null;

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

                Func<char, bool> probe = SubstituteMissingGlyphs ? _probe : null;
                ReportUnjoinableLetters(probe);

                string shaped = DirectRichText.Shape(source, probe);
                string prepared = null;

                if (FixWrappedLines)
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

                _width = CurrentWidth();
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
            switch (Direction)
            {
                case DirectTextDirection.RightToLeft: return DirectBidi.RightToLeft;
                case DirectTextDirection.LeftToRight: return DirectBidi.LeftToRight;
                default: return DirectBidi.AutoDirection;
            }
        }

        private bool IsRightToLeft(string source)
        {
            switch (Direction)
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
            if (!AlignToDirection || _text == null) { return; }

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
        // Persian.
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
                _text);
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

            // A form THIS package rasterized and wrote into the character
            // table is drawable, and no second opinion is wanted.
            // TextMeshPro is the right authority for a codepoint that came
            // out of the font's own cmap; it is not the right authority for
            // one we put there by glyph index, where HasCharacter can answer
            // no for reasons that have nothing to do with this glyph.
            if (_forms != null && _forms.Supplied(c))
            {
                _glyphs[c] = true;
                return true;
            }

            has = font.HasCharacter(c, true, true);
            _glyphs[c] = has;
            return has;
        }
    }

    // ==========================================
    // DirectTextTicker
    //
    // One place that asks every live engine whether
    // anything changed.
    //
    // In play mode the components tick themselves from
    // LateUpdate and the global driver ticks the rest;
    // this exists mainly for EDIT mode, where there is
    // no Update at all and the only heartbeat is
    // EditorApplication.update. Subscribing one delegate
    // instead of one per label is the difference between
    // a scene of two hundred captions costing one loop
    // and costing two hundred delegate invocations, on
    // every idle frame, forever.
    // ==========================================
    internal static class DirectTextTicker
    {
        private static readonly List<DirectTextEngine> s_engines = new List<DirectTextEngine>();
#if UNITY_EDITOR
        private static bool s_hooked;
#endif

        public static int Count => s_engines.Count;

        public static void Add(DirectTextEngine engine)
        {
            if (engine == null || s_engines.Contains(engine)) { return; }
            s_engines.Add(engine);
            Hook();
        }

        public static void Remove(DirectTextEngine engine)
        {
            if (engine == null) { return; }
            s_engines.Remove(engine);
        }

        /// <summary>
        /// Ticks every registered engine, dropping the ones whose label has
        /// gone away. Safe to call from anywhere; does nothing when the list
        /// is empty.
        /// </summary>
        public static void TickAll()
        {
            for (int i = s_engines.Count - 1; i >= 0; i--)
            {
                DirectTextEngine engine = s_engines[i];
                if (engine == null || engine.Text == null)
                {
                    s_engines.RemoveAt(i);
                    continue;
                }
                engine.Tick();
            }
        }

        private static void Hook()
        {
#if UNITY_EDITOR
            if (s_hooked) { return; }
            s_hooked = true;
            UnityEditor.EditorApplication.update -= EditorTick;
            UnityEditor.EditorApplication.update += EditorTick;
#endif
        }

#if UNITY_EDITOR
        // ExecuteAlways gets Update when the scene changes, which is not the
        // same as "whenever the text changed" - a localisation window or an
        // Inspector edit on another object can both do it silently.
        private static void EditorTick()
        {
            if (Application.isPlaying) { return; }
            TickAll();
        }
#endif
    }
}
