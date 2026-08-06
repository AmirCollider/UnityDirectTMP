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

        [Header("Options")]
        [Tooltip("Give this label its own material, so an outline on it does not "
               + "change every other label using the same font. Costs one draw call.")]
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

        /// <summary>The font file this label is drawn from.</summary>
        public Font Font
        {
            get => font;
            set { font = value; _asset = null; Apply(true); }
        }

        /// <summary>The font asset built from that file, or null.</summary>
        public TMP_FontAsset FontAsset => _asset;

        /// <summary>Which writing system the current text was found to be in.</summary>
        public DirectScript Script => _script;

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

        // Only forgets what it knows. Building a font asset from inside
        // OnValidate happens during deserialization, which is not a good place
        // to be creating objects - LateUpdate picks the change up a moment
        // later, in both edit mode and play mode.
        private void OnValidate()
        {
            _asset = null;
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

            if (!force && script == _applied && _asset != null && _label.font == _asset)
            {
                Reshape(text);
                return;
            }

            _applied = script;

            TMP_FontAsset asset = Build(FontFor(script));
            if (asset == null) { return; }

            if (_label.font != asset)
            {
                SetFontQuietly(asset);
                AddFallbacks(asset);
            }

            _asset = asset;

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
        // Assigning the font without dirtying the scene.
        //
        // `font` is a serialized field of the label, so
        // assigning it in the Editor marks the scene
        // modified - for a value that cannot be saved
        // anyway, because the asset is generated at load
        // and has no file behind it. This component
        // rebuilds it on every load, so the scene should
        // not be asked to remember it.
        // ==========================================
        private void SetFontQuietly(TMP_FontAsset asset)
        {
#if UNITY_EDITOR
            bool wasDirty = UnityEditor.EditorUtility.IsDirty(_label);
#endif
            _label.font = asset;

            // Assigning the font resets the label to the asset's SHARED
            // material, so the instance has to be made after it, not before.
            //
            // Reading TMP_Text.fontMaterial is what creates that instance -
            // the getter builds one from the shared material and puts the
            // label on it. That is TextMeshPro's own supported way of giving a
            // label a material of its own, which is why it is used here rather
            // than a `new Material(...)` that nothing would clean up.
            if (ownMaterial)
            {
                Material own = _label.fontMaterial;
                if (own == null) { Debug.LogWarning($"[DirectTMP] '{name}' could not be given its own material."); }
            }

#if UNITY_EDITOR
            if (!wasDirty) { UnityEditor.EditorUtility.ClearDirty(_label); }
#endif
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
        private void Reshape(string text)
        {
            if (!fixRightToLeft || string.IsNullOrEmpty(text))
            {
                _sourceText = text;
                _shapedText = text;
                return;
            }

            if (_sourceText == text) { return; }

            _sourceText = text;
            _shapedText = Prepare(text);

            if (_shapedText != text) { Regenerate(); }
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

            // A line break the author typed is a paragraph boundary, and each
            // paragraph wraps on its own. Treating one as a reason to skip all
            // of this - which is what 2.1.4 did - meant any text with a blank
            // line in it fell straight back to the whole-paragraph reordering
            // this method exists to replace, and came out in the wrong order
            // exactly as before.
            if (text.IndexOf('\n') < 0) { return Paragraph(text); }

            var built = new System.Text.StringBuilder(text.Length + 8);
            int start = 0;

            while (true)
            {
                int newline = text.IndexOf('\n', start);
                int end = newline < 0 ? text.Length : newline;

                built.Append(Paragraph(text.Substring(start, end - start)));

                if (newline < 0) { break; }

                built.Append('\n');
                start = newline + 1;
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
