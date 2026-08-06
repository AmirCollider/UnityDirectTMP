// ==========================================
// DirectFontJoiner
// Makes the joined shapes AVAILABLE.
//
// DirectShaper works out which of the four shapes each
// letter should take. This answers the other half: given
// "the initial peh", hand back a codepoint that actually
// draws one.
//
// ==========================================
// WHY THE OLD ONE BROKE
// ==========================================
// The presentation blocks - U+FE70..FEFF for the Arabic
// alphabet, U+FB50..FBFF for the letters Persian and
// Urdu add - are a legacy convenience: one codepoint per
// letter per shape, looked up in the cmap like any other
// character. Fonts built before OpenType carry them.
//
// Modern Persian faces - Vazirmatn, Sahel, Shabnam,
// IRANSans, Noto Sans Arabic, anything a designer will
// actually reach for - do not. They carry the plain
// letters and express joining in their OpenType tables,
// because that is what a real shaping engine reads.
//
// The old implementation ASKED the font whether it had
// U+FB58 and, when it did not, gave up on the whole word
// and drew it unjoined. So a Persian project got
// disconnected letters for every word that needed an
// initial or medial shape - which is most words, but not
// all of them: "درد" and "راز" are made only of letters
// that never join to the left, so they came out right.
// That is the whole of "simple words work, most words
// do not".
//
// ==========================================
// WHAT THIS ONE DOES
// ==========================================
// It stops asking and starts ADDING. The shapes were
// never missing - every font that sets Arabic has an
// initial peh, because that is what its 'init' feature
// is for. Only the CODEPOINTS were missing. So:
//
//   1. DirectFontGsub reads the font's own init / medi /
//      fina / isol lookups and reports, per letter and
//      per shape, the glyph the font itself would use.
//   2. That glyph is registered in the TextMeshPro font
//      asset under a codepoint of our choosing - the
//      Unicode presentation form where one exists, a
//      private-use codepoint where Unicode never
//      assigned one.
//   3. The shaper emits that codepoint and gets a
//      properly joined letter.
//
// The answer is now a property of the font FILE, which
// does not change, rather than of whatever the atlas
// happened to contain when the question was first asked.
// ==========================================
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace UnityDirectTMP
{
    /// <summary>
    /// Supplies, for one <see cref="TMP_FontAsset"/>, a codepoint that draws
    /// any given letter in any given joining shape - adding the glyph to the
    /// asset when it has to.
    /// </summary>
    public sealed class DirectFontJoiner
    {
        // One joiner per font asset. Keyed by instance id because a font asset
        // generated at runtime has no path and no stable name.
        private static readonly Dictionary<int, DirectFontJoiner> s_joiners
            = new Dictionary<int, DirectFontJoiner>();

        // Private-use codepoints for shapes Unicode never named. The Arabic
        // script has far more letters than the presentation blocks cover -
        // most of Arabic Extended-A, and a good deal of what Kurdish, Sindhi
        // and the African orthographies use.
        private const int PrivateUseFirst = 0xE000;
        private const int PrivateUseLast = 0xF8FF;

        private readonly TMP_FontAsset _asset;
        private readonly Func<byte[]> _bytes;

        // (letter, form) -> the codepoint that draws it. '\0' means this font
        // genuinely cannot, which is a fact about the font file and therefore
        // safe to remember.
        private readonly Dictionary<int, char> _resolved = new Dictionary<int, char>();

        private DirectJoinedGlyphs _joined;
        private bool _read;
        private int _nextPrivateUse = PrivateUseFirst;

        private DirectFontJoiner(TMP_FontAsset asset, Func<byte[]> bytes)
        {
            _asset = asset;
            _bytes = bytes;
        }

        /// <summary>Why this font cannot be joined, when it cannot. Empty otherwise.</summary>
        public string Problem { get; private set; } = string.Empty;

        /// <summary>True once the font's own joining rules have been read.</summary>
        public bool HasJoiningRules => _joined != null && _joined.HasJoiningFeatures;

        /// <summary>
        /// The joiner for a font asset, creating it on first use.
        /// <paramref name="bytes"/> is called at most once, and only if the
        /// asset turns out to need glyphs it does not have.
        /// </summary>
        public static DirectFontJoiner For(TMP_FontAsset asset, Func<byte[]> bytes = null)
        {
            if (asset == null) { return null; }

            int id = asset.GetInstanceID();
            if (s_joiners.TryGetValue(id, out DirectFontJoiner existing)) { return existing; }

            var joiner = new DirectFontJoiner(asset, bytes ?? (() => DirectFontSource.BytesFor(asset)));
            s_joiners[id] = joiner;
            return joiner;
        }

        /// <summary>Forgets every joiner. Font assets rebuilt from scratch need this.</summary>
        public static void Clear() => s_joiners.Clear();

        /// <summary>
        /// Shapes <paramref name="text"/> for this font: the shapes are chosen
        /// from Unicode, and every one of them is made available here before
        /// it is emitted.
        /// </summary>
        public string Shape(string text)
        {
            string shaped = DirectShaper.Shape(text, Resolve, ResolveLigature, null);

            // TextMeshPro rebuilds its lookup dictionaries from the character
            // table, and anything that touches the asset can trigger that. The
            // characters added above have to survive the rebuild, so the asset
            // is told about them once per shaping pass rather than once per
            // glyph.
            if (_dirty)
            {
                _dirty = false;
                try { _asset.ReadFontAssetDefinition(); } catch { /* not fatal */ }
            }

            return shaped;
        }

        private bool _dirty;

        // ==========================================
        // Resolve
        //
        // The order matters, and it is cheapest-first:
        //
        //   1. The isolated shape is the plain letter.
        //      Nothing to add, and it is the shape most
        //      letters in most strings are in.
        //   2. The presentation codepoint, if the font
        //      has one in its cmap. TextMeshPro can add
        //      that by itself, through public API, with
        //      no reflection and no glyph indices.
        //   3. The font's own GSUB, registered by glyph
        //      index. This is the path every modern
        //      Persian face takes, and the one the old
        //      implementation only ever reached as a
        //      rescue that was allowed to fail silently.
        // ==========================================
        /// <summary>
        /// A codepoint that draws <paramref name="letter"/> in
        /// <paramref name="form"/>, or '\0' when this font cannot draw it at
        /// all - in which case the caller emits the plain letter.
        /// </summary>
        public char Resolve(char letter, DirectForm form)
        {
            if (form == DirectForm.Isolated) { return letter; }
            if (_asset == null) { return '\0'; }

            int key = (letter << 2) | ((int)form & 3);
            if (_resolved.TryGetValue(key, out char cached)) { return cached; }

            char presentation = DirectJoining.PresentationForm(letter, form);

            // The font already carries the legacy codepoint - Segoe UI, Noto
            // Naskh, DejaVu and most of what predates OpenType shaping.
            if (presentation != '\0' && TryAddByCodepoint(presentation))
            {
                _resolved[key] = presentation;
                return presentation;
            }

            uint glyph = GlyphFor(letter, form);
            if (glyph == 0)
            {
                // A permanent no: the font's own tables name no glyph for this
                // shape. Remembering it costs one dictionary entry and saves
                // re-reading the GSUB for every label on screen.
                _resolved[key] = '\0';
                return '\0';
            }

            char target = presentation != '\0' ? presentation : NextPrivateUse();
            if (target == '\0') { _resolved[key] = '\0'; return '\0'; }

            if (!Register(target, glyph))
            {
                // Rasterizing can fail for reasons that pass - no atlas yet,
                // no font face loaded. Not remembered, so the next label tries
                // again rather than inheriting a verdict from a bad moment.
                return '\0';
            }

            _resolved[key] = target;
            return target;
        }

        /// <summary>
        /// A codepoint that draws lam followed by <paramref name="alef"/> as
        /// the single glyph Arabic requires, or '\0'.
        /// </summary>
        public char ResolveLigature(char alef, DirectForm form)
        {
            if (_asset == null) { return '\0'; }

            char[] pair = DirectJoining.LamAlef(alef);
            if (pair == null) { return '\0'; }

            char target = pair[form == DirectForm.Final ? 1 : 0];
            if (target == '\0') { return '\0'; }

            int key = (target << 2) | 3;   // form 3 is unused for ligatures
            if (_resolved.TryGetValue(key, out char cached)) { return cached; }

            if (TryAddByCodepoint(target))
            {
                _resolved[key] = target;
                return target;
            }

            uint glyph = Joined()?.LigatureGlyph(alef, form) ?? 0u;
            if (glyph == 0) { _resolved[key] = '\0'; return '\0'; }

            if (!Register(target, glyph)) { return '\0'; }

            _resolved[key] = target;
            return target;
        }

        // ==========================================
        // The font's own joining rules, read once.
        // ==========================================
        private DirectJoinedGlyphs Joined()
        {
            if (_read) { return _joined; }
            _read = true;

            byte[] data = null;
            try { data = _bytes?.Invoke(); }
            catch (Exception e) { Problem = e.Message; }

            if (data == null || data.Length == 0)
            {
                Problem = "The font file could not be read, so its own joining rules "
                        + "could not be asked for.";
                return null;
            }

            var letters = new List<char>();
            foreach (char c in DirectJoining.PresentationLetters) { letters.Add(c); }

            // Letters Unicode never gave a presentation codepoint to still
            // join, and the font's GSUB is the only place that says how.
            for (int u = 0x0620; u <= 0x08BF; u++)
            {
                var c = (char)u;
                if (!DirectJoining.IsShaped(c)) { continue; }
                if (DirectJoining.PresentationForm(c, DirectForm.Isolated) != '\0') { continue; }
                letters.Add(c);
            }

            _joined = DirectFontGsub.Read(data, letters);

            if (_joined != null && !_joined.HasJoiningFeatures && string.IsNullOrEmpty(Problem))
            {
                Problem = string.IsNullOrEmpty(_joined.Error)
                    ? "This font has no OpenType joining rules."
                    : _joined.Error;
            }

            return _joined;
        }

        private uint GlyphFor(char letter, DirectForm form)
        {
            DirectJoinedGlyphs joined = Joined();
            return joined == null ? 0u : joined.GlyphIndex(letter, form);
        }

        private char NextPrivateUse()
        {
            if (_nextPrivateUse > PrivateUseLast) { return '\0'; }

            // Skip anything the font itself puts in the private-use area, so a
            // registered shape can never collide with a glyph the font meant.
            while (_nextPrivateUse <= PrivateUseLast && Has(_nextPrivateUse)) { _nextPrivateUse++; }

            return _nextPrivateUse > PrivateUseLast ? '\0' : (char)_nextPrivateUse++;
        }

        private bool Has(int codepoint)
        {
            try { return _asset.HasCharacter(codepoint); }
            catch { return false; }
        }

        // The public route: ask TextMeshPro to add a character it can find in
        // the font's own cmap. No reflection, no glyph indices, works on every
        // TextMeshPro version.
        private bool TryAddByCodepoint(char codepoint)
        {
            if (Has(codepoint)) { return true; }

            try
            {
                return _asset.TryAddCharacters(new[] { (uint)codepoint }, out uint[] missing)
                    && (missing == null || missing.Length == 0);
            }
            catch
            {
                return false;
            }
        }

        // ==========================================
        // Register one glyph, by index, under a
        // codepoint of our choosing.
        //
        // TextMeshPro addresses glyphs by CHARACTER
        // everywhere except here: TryAddGlyphInternal is
        // the one entry point that takes a glyph index,
        // and it is internal - so it is called
        // reflectively, and everything below tolerates it
        // not being there.
        // ==========================================
        private bool Register(char codepoint, uint glyphIndex)
        {
            try
            {
                Glyph glyph = AddGlyph(glyphIndex);
                if (glyph == null) { return false; }

                var character = new TMP_Character(codepoint, glyph) { textAsset = _asset };
                _asset.characterTable.Add(character);
                _asset.characterLookupTable[codepoint] = character;
                _dirty = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ==========================================
        // The FontEngine keeps ONE face open at a time,
        // and whichever call last loaded one owns it.
        //
        // That matters more here than anywhere else in
        // the package, because this is the only place
        // that asks for a glyph by INDEX. An index means
        // nothing without a face: ask for glyph 1,247
        // while somebody else's font is loaded and you
        // get their glyph 1,247, rasterized into our
        // atlas, under our codepoint. The text then
        // renders - as the wrong letters.
        //
        // A codepoint could not do this. A glyph index
        // can, so the face is made current first, every
        // time.
        // ==========================================
        private bool LoadFace()
        {
            int pointSize = (int)_asset.faceInfo.pointSize;
            if (pointSize <= 0) { pointSize = DirectFontLibrary.SamplingPointSize; }

            try
            {
                if (_asset.sourceFontFile != null
                    && FontEngine.LoadFontFace(_asset.sourceFontFile, pointSize) == FontEngineError.Success)
                {
                    return true;
                }

                byte[] data = _bytes?.Invoke();
                if (data != null && data.Length > 0
                    && FontEngine.LoadFontFace(data, pointSize) == FontEngineError.Success)
                {
                    return true;
                }
            }
            catch { /* fall through */ }

            // Some TextMeshPro versions load the asset's own face inside
            // TryAddGlyphInternal, so a failure here is not the end of it.
            return false;
        }

        private Glyph AddGlyph(uint glyphIndex)
        {
            // Already rasterized: one glyph often serves several shapes of
            // several letters, and a font that reuses one is the common case.
            if (_asset.glyphLookupTable != null
                && _asset.glyphLookupTable.TryGetValue(glyphIndex, out Glyph existing)
                && existing != null)
            {
                return existing;
            }

            LoadFace();

            MethodInfo adder = GlyphAdder();
            if (adder == null)
            {
                Problem = "This TextMeshPro version does not expose glyph-index rasterizing, "
                        + "so fonts without the legacy presentation codepoints cannot be joined.";
                return null;
            }

            object[] args = { glyphIndex, null };
            bool added = adder.Invoke(_asset, args) is bool answer && answer;
            return added ? args[1] as Glyph : null;
        }

        private static MethodInfo s_glyphAdder;
        private static bool s_glyphAdderResolved;

        private static MethodInfo GlyphAdder()
        {
            if (s_glyphAdderResolved) { return s_glyphAdder; }
            s_glyphAdderResolved = true;

            foreach (MethodInfo m in typeof(TMP_FontAsset).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (m.Name != "TryAddGlyphInternal") { continue; }

                ParameterInfo[] p = m.GetParameters();
                if (p.Length != 2) { continue; }
                if (p[0].ParameterType != typeof(uint)) { continue; }
                if (!p[1].IsOut || p[1].ParameterType.GetElementType() != typeof(Glyph)) { continue; }

                s_glyphAdder = m;
                break;
            }

            return s_glyphAdder;
        }
    }
}
