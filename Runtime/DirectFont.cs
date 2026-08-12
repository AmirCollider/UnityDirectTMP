// ==========================================
// DirectFont  ("Direct Font" in the Inspector)
// Put this next to a TextMeshPro label, give it a .ttf,
// and that label is drawn from the file.
//
// One component, one label, one font. Nothing is applied
// project-wide, nothing is stored in a settings asset,
// and two labels with two different fonts do not know
// about each other.
//
// ==========================================
// THE OUTLINE PROBLEM
// ==========================================
// Every TextMeshPro label using the same font asset
// shares ONE material. So an outline, a face dilate or a
// glow set on one label is set on every label that uses
// that font - which is the single most reported piece of
// TextMeshPro weirdness there is, and it is not a bug in
// this package or in TextMeshPro. It is what "shared
// material" means.
//
// `Own Material` gives this label a material of its own,
// so its outline stays its own. It is on by default,
// because a surprise that costs a draw call is better
// than a surprise that changes every label in the scene.
// Turn it off on labels that share a look and you get
// the batching back.
//
// That much was true and still is. What it left out is
// where the outline itself is supposed to LIVE.
//
// A material is the only place TextMeshPro keeps an
// outline, and the material behind a font asset this
// package generates is generated too: built at load,
// with no file under it. Nothing set on it can be saved,
// by the scene or by anything else. So an outline set by
// hand held until the next reload - a recompile, entering
// play mode, reopening the scene - and then the label
// came back on a fresh material with no outline on it.
// From a chair in front of Unity that reads as "the
// outline does nothing", because every way of checking it
// goes through one of those three.
//
// The outline is therefore kept HERE, on the component,
// where the scene can save it, and written onto the
// label's own material every time that material is
// rebuilt. It survives reloads and play mode, it is in
// the build, and it belongs to one label.
//
// ==========================================
// A FONT PER LANGUAGE
// ==========================================
// A label that shows Persian today and Japanese
// tomorrow - a name, a chat line, a localized string -
// wants a different font in each case, and no single
// font is good at both.
//
// So the language fields are optional overrides: fill in
// the ones you have a font for, leave the rest empty.
// The script the text is actually in is worked out each
// time the text changes, and the matching font is used.
// Anything with no font of its own falls back to Font.
// ==========================================
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// Draws one TextMeshPro label from a font file, joining Persian and
    /// Arabic on the way.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Unity DirectTMP/Direct Font")]
    [HelpURL("https://github.com/AmirCollider/UnityDirectTMP#readme")]
    public sealed class DirectFont : MonoBehaviour, ITextPreprocessor
    {
        [Tooltip("The .ttf or .otf this label is drawn from. This is the only field you need.")]
        [UnityEngine.Serialization.FormerlySerializedAs("fontFile")]
        [SerializeField] private Font font;

        [Header("A different font per language — optional")]
        [Tooltip("Used when the text is mostly Persian, Arabic or Urdu. Empty = use Font.")]
        [SerializeField] private Font persianArabic;

        [Tooltip("Used when the text is mostly Japanese, Chinese or Korean. Empty = use Font.")]
        [SerializeField] private Font japaneseChineseKorean;

        [Tooltip("Used when the text is mostly English or another Latin language. Empty = use Font.")]
        [SerializeField] private Font latin;

        [Header("Outline — this label only")]
        [Tooltip("Thickness of the outline drawn around this label's letters. 0 is no outline. "
               + "It is kept here rather than on the material because the material is generated "
               + "at load and cannot be saved.")]
        [SerializeField, Range(0f, 1f)] private float outlineWidth;

        [Tooltip("The colour of that outline.")]
        [SerializeField] private Color outlineColor = Color.black;

        [Header("Options")]
        [Tooltip("Give this label its own material, so anything set on it does not "
               + "change every other label using the same font. Costs one draw call. "
               + "An outline switches this on by itself.")]
        [SerializeField] private bool ownMaterial = true;

        [Tooltip("Join Persian/Arabic letters and put right-to-left text in reading order.")]
        [SerializeField] private bool fixRightToLeft = true;

        [Tooltip("Keep wrapped lines in the right order. Needed for any right-to-left "
               + "paragraph long enough to wrap; turn it off only to rule it out.")]
        [SerializeField] private bool fixWrappedLines = true;

        private TMP_Text _label;
        private TMP_FontAsset _asset;
        private DirectScript _script = DirectScript.None;
        private DirectScript _applied = DirectScript.None;

        private string _scanned;
        private string _sourceText;
        private string _shapedText;
        private bool _measuring;
        private bool _rebuild = true;

        /// <summary>The font file this label is drawn from.</summary>
        public Font Font
        {
            get => font;
            set { font = value; _rebuild = true; Apply(true); }
        }

        /// <summary>The font asset built from that file, or null.</summary>
        public TMP_FontAsset FontAsset => _asset;

        /// <summary>
        /// How thick an outline is drawn around this label's letters, 0 for none.
        /// It is this label's own: no other label using the same font is touched.
        /// </summary>
        public float OutlineWidth
        {
            get => outlineWidth;
            set { outlineWidth = Mathf.Clamp01(value); Style(); }
        }

        /// <summary>The colour of that outline.</summary>
        public Color OutlineColor
        {
            get => outlineColor;
            set { outlineColor = value; Style(); }
        }

        /// <summary>Which writing system the current text was found to be in.</summary>
        public DirectScript Script => _script;

        /// <summary>
        /// Builds this label's font asset now instead of at the next frame, and
        /// hands it back. Nothing has to call this — the component builds what it
        /// needs by itself — but a tool that wants the answer in the same frame it
        /// asked can have it.
        /// </summary>
        public TMP_FontAsset Rebuild()
        {
            Apply(true);
            return _asset;
        }

        /// <summary>
        /// Exactly what this component hands TextMeshPro for the label's
        /// current text, line breaks and all.
        ///
        /// This is what a diagnostic has to report. DirectTMP.Prepare is only
        /// half the story - it reorders a whole paragraph at a time, while the
        /// component breaks the paragraph into display lines first. Reporting
        /// the former makes a working per-line pass look like it never ran.
        /// </summary>
        public string PreparedText()
        {
            if (_label == null || font == null) { return null; }
            if (!fixRightToLeft) { return _label.text; }

            return Prepare(_label.text ?? string.Empty);
        }

        private void OnEnable()
        {
            _label = GetComponent<TMP_Text>();
            Apply(true);
        }

        private void OnDisable()
        {
            if (_label != null && ReferenceEquals(_label.textPreprocessor, this))
            {
                _label.textPreprocessor = null;
                Regenerate();
            }
        }

        // ==========================================
        // Only asks for the work to be done again.
        //
        // Building a font asset from inside OnValidate
        // happens during deserialization, which is not a
        // good place to be creating objects - LateUpdate
        // picks the change up a moment later, in both edit
        // mode and play mode.
        //
        // What it must NOT do is throw away the asset it
        // already has. OnValidate runs on every edit and on
        // every deserialization, and the Inspector paints
        // between it and the next LateUpdate - so a
        // component that nulled its asset here reported
        // itself, in red, as a font that could not be
        // built, every time anybody touched a field on it.
        // Nothing was wrong; the answer had simply been
        // deleted a moment before the question.
        // ==========================================
        private void OnValidate()
        {
            outlineWidth = Mathf.Clamp01(outlineWidth);

            _rebuild = true;
            _ownRefused = false;
            _sourceText = null;
            _scanned = null;
        }

        private void LateUpdate() => Apply(false);

        // ==========================================
        // Apply
        //
        // Cheap enough to call every frame: it does
        // nothing at all unless the text has changed to a
        // different script, or the font has not been
        // built yet.
        // ==========================================
        private void Apply(bool force)
        {
            if (_label == null) { _label = GetComponent<TMP_Text>(); }
            if (_label == null || font == null) { return; }

            string text = _label.text;

            // Scanning the text for its writing system is the only part of
            // this that is not a reference comparison, so it is done when the
            // text changes rather than on every frame.
            if (force || !ReferenceEquals(_scanned, text))
            {
                _scanned = text;
                _script = DirectScripts.DominantOf(text);
            }

            DirectScript script = _script;

            if (!force && !_rebuild && script == _applied && _asset != null && _label.font == _asset)
            {
                Style();
                Reshape(text);
                return;
            }

            TMP_FontAsset asset = Build(FontFor(script));

            // Nothing was built, so nothing is applied - and the asset this
            // label already had is kept, because a label drawn by the font it
            // had a moment ago is better than a label drawn by nothing.
            if (asset == null) { return; }

            _applied = script;
            _rebuild = false;

            if (_label.font != asset)
            {
                SetFontQuietly(asset);
                AddFallbacks(asset);
                _ownRefused = false;
            }

            _asset = asset;

            Style();

            if (!ReferenceEquals(_label.textPreprocessor, this))
            {
                _label.textPreprocessor = this;
                Regenerate();
            }

            _sourceText = null;
            Reshape(text);
        }

        private Font FontFor(DirectScript script)
        {
            switch (script)
            {
                case DirectScript.Arabic: return persianArabic != null ? persianArabic : font;
                case DirectScript.Cjk: return japaneseChineseKorean != null ? japaneseChineseKorean : font;
                case DirectScript.Latin: return latin != null ? latin : font;
                default: return font;
            }
        }

        private TMP_FontAsset Build(Font file) => file == null ? null : DirectFontLibrary.Load(file);

        // The other language fonts go behind this one, so a line of Persian
        // with an English word in it still finds the English word's glyphs
        // even though only one font can be the label's own.
        private void AddFallbacks(TMP_FontAsset asset)
        {
            if (asset.fallbackFontAssetTable == null)
            {
                asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }

            AddFallback(asset, font);
            AddFallback(asset, persianArabic);
            AddFallback(asset, japaneseChineseKorean);
            AddFallback(asset, latin);
        }

        private static void AddFallback(TMP_FontAsset asset, Font file)
        {
            if (file == null) { return; }

            TMP_FontAsset other = DirectFontLibrary.Load(file);
            if (other == null || other == asset) { return; }
            if (asset.fallbackFontAssetTable.Contains(other)) { return; }

            asset.fallbackFontAssetTable.Add(other);
        }

        // ==========================================
        // Assigning the font and the material without
        // dirtying the scene.
        //
        // `font` and `fontSharedMaterial` are serialized
        // fields of the label, so assigning either in the
        // Editor marks the scene modified - for values
        // that cannot be saved anyway, because the font
        // asset is generated at load and the material is
        // an instance of a material that is generated with
        // it. This component rebuilds both on every load,
        // so the scene should not be asked to remember
        // either.
        // ==========================================
#if UNITY_EDITOR
        private bool _wasDirty;
#endif

        private void BeginQuietly()
        {
#if UNITY_EDITOR
            _wasDirty = _label != null && UnityEditor.EditorUtility.IsDirty(_label);
#endif
        }

        private void EndQuietly()
        {
#if UNITY_EDITOR
            if (!_wasDirty && _label != null) { UnityEditor.EditorUtility.ClearDirty(_label); }
#endif
        }

        private void SetFontQuietly(TMP_FontAsset asset)
        {
            BeginQuietly();
            _label.font = asset;
            EndQuietly();
        }

        // ==========================================
        // The material, and what is written on it.
        // ==========================================
        // Assigning the font resets the label to the font
        // asset's SHARED material, so this runs after it,
        // never before.
        // ==========================================
        private static readonly int OutlineWidthProperty = Shader.PropertyToID("_OutlineWidth");
        private static readonly int OutlineColorProperty = Shader.PropertyToID("_OutlineColor");

        // The mobile SDF shader - the one a generated font asset is given -
        // compiles its outline out entirely unless this is on, so setting the
        // width alone changes nothing whatsoever. The desktop shader has no
        // such keyword and is unaffected either way.
        private const string OutlineKeyword = "OUTLINE_ON";

        private Material _own;
        private Texture _ownAtlas;
        private bool _ownRefused;
        private bool _wroteOutline;
        private float _wroteWidth;
        private Color _wroteColor;

        // An outline cannot be shared, so asking for one asks for a material of
        // its own whatever the checkbox says. The alternative is writing it onto
        // the shared material and changing every label using that font, which is
        // the exact surprise this component exists to prevent.
        private bool NeedsOwnMaterial => ownMaterial || outlineWidth > 0f;

        private void Style()
        {
            if (_label == null || _asset == null) { return; }

            if (!NeedsOwnMaterial) { Share(); return; }

            Own();
            Outline();
        }

        // ==========================================
        // A material of this label's own.
        //
        // Copied here rather than taken from TextMeshPro's
        // `fontMaterial` getter, which was what 2.1.0 did.
        // That getter caches the instance it made and,
        // depending on the version, hands the SAME one back
        // after the font asset underneath has been replaced
        // - an instance still pointing at the previous
        // atlas, which draws this font's letters out of the
        // wrong picture. A label that changes language
        // changes font asset, so that is not a corner case,
        // it is Tuesday.
        //
        // Copying is also the only way to be certain what
        // the label ends up on, which matters when the
        // whole point is that an outline written here goes
        // nowhere else.
        // ==========================================
        private void Own()
        {
            // Three things have to still be true, and they are the three cheapest
            // questions available: the material exists, the label is still on it,
            // and it still draws out of the atlas it was copied for. The last one
            // is what catches a font asset that has been replaced underneath.
            if (_own != null && _label.fontSharedMaterial == _own && _ownAtlas == _asset.atlasTexture)
            {
                return;
            }

            // Asked once. Retrying every frame would build a material every
            // frame and say the same thing to the Console as many times.
            // Cleared by a new font asset and by any edit to this component.
            if (_ownRefused) { return; }

            Material source = _asset.material;

            if (source == null)
            {
                _ownRefused = true;
                Debug.LogWarning(
                    $"[DirectTMP] '{name}' has no material to copy — the font asset was built "
                    + "without one, which usually means TextMeshPro could not find its "
                    + "distance-field shader.");
                return;
            }

            var instance = new Material(source)
            {
                // The name TextMeshPro gives its own instances, so that its
                // Inspector still recognises this as one.
                name = source.name + " (Instance)",

                // Nothing can save it - it is a copy of a material that is
                // itself built at load - so it must not be written into the
                // scene file either, where it would sit as an orphan pointing at
                // an atlas that will not exist next time.
                hideFlags = HideFlags.DontSave,
            };

            instance.shaderKeywords = source.shaderKeywords;

            BeginQuietly();
            _label.fontSharedMaterial = instance;
            EndQuietly();

            if (_own != null) { Discard(_own); }

            _own = instance;
            _ownAtlas = _asset.atlasTexture;

            // A fresh copy of the shared material carries nothing that was
            // written on the last one.
            _wroteOutline = false;

            Regenerate();
        }

        // Own Material turned off with no outline asked for: back to the shared
        // material, which is what gets the batching back.
        private void Share()
        {
            if (_own == null) { return; }

            BeginQuietly();
            if (_label.fontSharedMaterial == _own && _asset.material != null)
            {
                _label.fontSharedMaterial = _asset.material;
            }
            EndQuietly();

            Discard(_own);
            _own = null;
            _ownAtlas = null;
            _wroteOutline = false;
            Regenerate();
        }

        // ==========================================
        // Writes the outline, and only when there is one
        // to write - a material somebody set up by hand is
        // left exactly as it is until this component is
        // actually asked for an outline.
        // ==========================================
        private void Outline()
        {
            if (_own == null) { return; }

            bool wanted = outlineWidth > 0f;

            if (!wanted && !_wroteOutline) { return; }
            if (wanted && _wroteOutline && _wroteWidth == outlineWidth && _wroteColor == outlineColor) { return; }

            // Recorded before the write, not after, so that a material which
            // cannot take an outline is complained about once rather than on
            // every frame for the rest of the session.
            _wroteOutline = wanted;
            _wroteWidth = outlineWidth;
            _wroteColor = outlineColor;

            if (!_own.HasProperty(OutlineWidthProperty))
            {
                if (wanted)
                {
                    Debug.LogWarning(
                        $"[DirectTMP] '{name}' asks for an outline, but the shader on its material "
                        + $"('{(_own.shader == null ? "none" : _own.shader.name)}') has no outline "
                        + "to set.");
                }

                return;
            }

            _own.SetFloat(OutlineWidthProperty, wanted ? outlineWidth : 0f);
            _own.SetColor(OutlineColorProperty, outlineColor);

            if (wanted) { _own.EnableKeyword(OutlineKeyword); }
            else { _own.DisableKeyword(OutlineKeyword); }

            // ==========================================
            // The half of this that everybody misses.
            //
            // An outline is drawn OUTSIDE the letter, and
            // the quad the letter is drawn on is only as
            // big as the letter. Widen the outline without
            // widening the quad and the outline is cut off
            // at the edge of the glyph: a hairline at best,
            // nothing at all at worst - which looks exactly
            // like an outline that was never applied, and
            // is why setting `_OutlineWidth` from code and
            // calling it done does not work.
            //
            // TextMeshPro's own material editor calls this
            // for the same reason.
            // ==========================================
            _label.UpdateMeshPadding();
            Regenerate();
        }

        // Only ever given a material this component created, and checked again
        // here, because destroying a material somebody else assigned would leave
        // their label pink.
        private static void Discard(Material material)
        {
            if (material == null || material.hideFlags != HideFlags.DontSave) { return; }

            if (Application.isPlaying) { Destroy(material); }
            else { DestroyImmediate(material); }
        }

        // ==========================================
        // Shaping
        //
        // Done here, when the text changes, rather than
        // inside PreprocessText - because shaping may add
        // glyphs to the font asset, and doing that in the
        // middle of TextMeshPro's own text generation is
        // asking for trouble.
        // ==========================================
        // ==========================================
        // Shaping the same text more than once.
        //
        // The first pass over a label runs against a font
        // asset that was built moments ago and whose
        // atlas may not be ready. Registering the font's
        // own joined shapes can fail there - which the
        // joiner treats as temporary and does not
        // remember, precisely so that the next attempt
        // can do better.
        //
        // There was no next attempt. Text is shaped when
        // it CHANGES, so a label that shaped once during
        // that window kept its unjoined result for as
        // long as the scene lived.
        //
        // That is the whole of "Persian is broken when
        // the game starts in Persian, and fine when you
        // switch to it". Switching changes the text.
        // Starting in it does not.
        // ==========================================

        /// <summary>
        /// How many more frames this label will try the same text again, while the joiner is
        /// still reporting shapes it could not register. Bounded, so a font that genuinely
        /// cannot supply a shape costs a handful of frames rather than every frame forever.
        /// </summary>
        private int _attemptsLeft;

        private const int Attempts = 30;

        private void Reshape(string text)
        {
            if (!fixRightToLeft || string.IsNullOrEmpty(text))
            {
                _sourceText = text;
                _shapedText = text;
                _attemptsLeft = 0;
                return;
            }

            bool isNewText = _sourceText != text;

            if (!isNewText && _attemptsLeft <= 0) { return; }

            _attemptsLeft = isNewText ? Attempts : _attemptsLeft - 1;
            _sourceText = text;

            DirectFontJoiner joiner = _asset != null ? DirectFontJoiner.For(_asset) : null;
            joiner?.ClearRetryWanted();

            string shaped = Prepare(text);

            // Nothing was left unfinished, so there is nothing to come back for.
            if (joiner == null || !joiner.RetryWanted) { _attemptsLeft = 0; }

            if (shaped == _shapedText) { return; }

            _shapedText = shaped;
            Regenerate();
        }

        // ==========================================
        // Reordering has to happen PER DISPLAY LINE.
        //
        // This is the whole of "the lines came out in
        // the wrong order", and it is not a bug in the
        // reordering - it is a bug in doing it once for
        // the whole paragraph.
        //
        // Reordering a right-to-left paragraph reverses
        // it: the last word of the sentence ends up at
        // the start of the string. TextMeshPro then wraps
        // that string the way it wraps any string - first
        // line first - so the first line on screen holds
        // the END of the sentence and the last line holds
        // the beginning. Every line is internally correct,
        // which is what makes it so confusing to look at.
        //
        // Real text engines break the line first and
        // reorder each line afterwards; the Unicode
        // algorithm says so in as many words. So that is
        // what happens here:
        //
        //   1. shape the text but leave it in the order it
        //      was typed;
        //   2. ask TextMeshPro where IT would break that,
        //      at this font, this size, this width;
        //   3. cut the sentence at those places and
        //      reorder each piece on its own.
        //
        // Step 2 is honest measuring rather than a guess:
        // shaped text has the same glyphs as the text that
        // will be drawn, so it has the same widths and the
        // same breaks.
        // ==========================================
        private string Prepare(string text)
        {
            if (!fixWrappedLines || _label == null || !Wraps()) { return DirectTMP.Prepare(text, _asset); }

            // ==========================================
            // Line endings, all three of them.
            //
            // A line break the author typed is a
            // paragraph boundary, and each paragraph then
            // wraps on its own. But Unity's own text
            // fields hand back WINDOWS line endings, so
            // the break is "\r\n" and not "\n" - and
            // splitting on "\n" alone leaves the "\r"
            // stuck to the end of the paragraph before it.
            //
            // A stray carriage return then does damage
            // twice over: the reordering treats it as an
            // ordinary character and carries it to the
            // front of a right-to-left line, and
            // TextMeshPro draws it as a line break of its
            // own. One invisible character, one extra
            // blank line, and the first word of the
            // paragraph adrift.
            //
            // So all three endings are recognised, and
            // only "\n" is emitted.
            // ==========================================
            string[] paragraphs = DirectTMP.Paragraphs(text);
            var built = new System.Text.StringBuilder(text.Length + paragraphs.Length);

            for (int i = 0; i < paragraphs.Length; i++)
            {
                if (i > 0) { built.Append('\n'); }
                built.Append(Paragraph(paragraphs[i]));
            }

            return built.ToString();
        }

        // One paragraph: shaped, broken where TextMeshPro would break it, and
        // reordered a line at a time.
        private string Paragraph(string text)
        {
            if (string.IsNullOrEmpty(text)) { return text; }

            string shaped = DirectTMP.Shape(text, _asset);

            int[] breaks = LineBreaksOf(shaped);
            if (breaks == null || breaks.Length <= 1) { return DirectTMP.Reorder(shaped); }

            var built = new System.Text.StringBuilder(shaped.Length + breaks.Length);

            for (int i = 0; i < breaks.Length; i++)
            {
                int start = breaks[i];
                int end = i + 1 < breaks.Length ? breaks[i + 1] : shaped.Length;
                if (end <= start) { continue; }

                if (built.Length > 0) { built.Append('\n'); }
                built.Append(DirectTMP.Reorder(shaped.Substring(start, end - start)));
            }

            return built.ToString();
        }

        private bool Wraps()
        {
            try
            {
#if UNITY_2023_2_OR_NEWER
                return _label.textWrappingMode != TextWrappingModes.NoWrap;
#else
                return _label.enableWordWrapping;
#endif
            }
            catch { return true; }
        }

        // Where TextMeshPro would break this text, as indices into it.
        // Always starts with 0; null when it could not be measured.
        private int[] LineBreaksOf(string shaped)
        {
            TMP_TextInfo info;

            // GetTextInfo measures by SETTING the label's text and generating
            // it. Two things follow, and both have to be undone:
            //
            //   * label.text ends up holding the string we measured, not the
            //     one the game set. It is put back.
            //   * label.text is serialized, so in the Editor that would mark
            //     the scene modified for a measurement nobody asked to save.
            //
            // And because generating text calls this component back as the
            // label's preprocessor, it has to hand the string straight back
            // while that is happening, or we measure something else again.
            string original = _label.text;

#if UNITY_EDITOR
            bool wasDirty = UnityEditor.EditorUtility.IsDirty(_label);
#endif
            _measuring = true;
            try
            {
                info = _label.GetTextInfo(shaped);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (_label.text != original) { _label.text = original; }
                _measuring = false;
#if UNITY_EDITOR
                if (!wasDirty) { UnityEditor.EditorUtility.ClearDirty(_label); }
#endif
            }

            if (info == null || info.lineCount <= 1) { return null; }

            var breaks = new int[info.lineCount];
            for (int i = 0; i < info.lineCount; i++)
            {
                int first = info.lineInfo[i].firstCharacterIndex;
                if (first < 0 || first >= info.characterInfo.Length) { return null; }

                breaks[i] = info.characterInfo[first].index;
            }

            breaks[0] = 0;

            // Monotonic or the slicing below is nonsense.
            for (int i = 1; i < breaks.Length; i++)
            {
                if (breaks[i] <= breaks[i - 1] || breaks[i] > shaped.Length) { return null; }
            }

            return breaks;
        }

        // ==========================================
        // Making TextMeshPro look at the text again.
        //
        // SetVerticesDirty is not enough, and thinking
        // it was is why this package could attach its
        // preprocessor to a label and change nothing at
        // all.
        //
        // TextMeshPro only calls a preprocessor from
        // ParseInputText, and it only parses when
        // havePropertiesChanged is set. SetVerticesDirty
        // rebuilds the MESH from text that has already
        // been parsed - so a label whose text was parsed
        // before the preprocessor arrived kept the
        // unjoined, unreordered version forever, and
        // nothing about that looks like a missing re-parse
        // from the outside.
        // ==========================================
        private void Regenerate()
        {
            if (_label == null) { return; }

            _label.havePropertiesChanged = true;
            _label.SetVerticesDirty();
            _label.SetLayoutDirty();
        }

        /// <summary>
        /// Called by TextMeshPro with the label's own text, returning what
        /// should be drawn. The label's <c>.text</c> is never written to, so
        /// reading it back gives exactly the string you set.
        /// </summary>
        public string PreprocessText(string text)
        {
            if (_measuring) { return text; }
            if (!fixRightToLeft || string.IsNullOrEmpty(text)) { return text; }

            // The ordinary path: LateUpdate already shaped this exact string.
            if (_sourceText == text && _shapedText != null) { return _shapedText; }

            // Text set and drawn inside the same frame, before LateUpdate ran.
            return DirectTMP.Prepare(text, _asset);
        }
    }
}
