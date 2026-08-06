// ==========================================
// DirectFontGsub
// Asks a font how IT joins its own letters.
//
// The presentation-form blocks - U+FE70..FEFF for the
// Arabic alphabet, U+FB50..FBFF for the letters Persian
// and Urdu add - are a legacy convenience. They exist so
// that a system with no shaping engine can still set the
// script: one codepoint per letter per joining form,
// looked up in the font's cmap like any other character.
// DirectFontJoiner uses them where a font has them, and
// for most older fonts that is the whole story.
//
// It is not the whole story for Persian. Those two
// blocks are separate, and a font is free to carry one
// and not the other - Segoe UI, which is on every
// Windows machine, carries the Arabic block in full and
// none of the Persian one. So ر و ه ن come out joined
// and پ چ ژ گ do not, and a word with a پ in it breaks
// in the middle. Version 1.1.1 made that failure tidy:
// the letter stands alone rather than drawing connecting
// strokes into nothing. Tidy is still broken.
//
// But the font HAS those shapes. Every font that sets
// Arabic has an initial peh, a medial peh and a final
// peh - it reaches them through its OpenType GSUB table,
// which is what a real shaping engine uses and what the
// presentation blocks were always a stand-in for. The
// glyphs are there; only the codepoints are missing.
//
// So this file reads GSUB directly and answers, per
// letter and per joining form, WHICH GLYPH the font
// would use. DirectFontJoiner then registers that glyph
// in the TextMeshPro font asset under a codepoint the
// shaper can emit - and the missing block stops
// mattering, because the shapes were never what was
// missing.
//
// Only what is needed is read:
//   * the four joining features - isol, fina, init, medi
//   * lookup type 1 (single substitution), formats 1
//     and 2, which is how every font on earth expresses
//     "this letter, in this position, is that glyph"
//   * lookup type 4 (ligature), but only far enough to
//     find lam-alef - the one ligature Arabic will not
//     do without
//   * lookup type 7 (extension), which is only a
//     redirection to one of the above in a font too
//     large to address with 16-bit offsets
// Contextual lookups are deliberately not read: they are
// how a font draws Nastaliq, which this is not trying to
// be. A face that expresses a joining form contextually
// still joins - it simply uses the standard shape rather
// than its own stylistic variant of it.
//
// Pure C#. Bytes in, facts out, every read bounds-checked
// against the buffer, and no exception ever crosses the
// boundary - a corrupt font produces an empty answer and
// the shaper carries on exactly as it did before.
// ==========================================
using System;
using System.Collections.Generic;

namespace UnityDirectTMP
{
    /// <summary>
    /// Which glyph a font draws a given letter with, in each joining form.
    /// Produced by <see cref="DirectFontGsub"/>; consumed by
    /// <c>DirectFontJoiner</c>, which registers those glyphs in the font
    /// asset under the codepoints the shaper emits.
    /// </summary>
    public sealed class DirectJoinedGlyphs
    {
        // letter -> four glyph indices, by DirectForm. 0 is the .notdef
        // glyph and therefore doubles as "this font does not substitute here".
        private readonly Dictionary<char, uint[]> _glyphs = new Dictionary<char, uint[]>();

        /// <summary>False when the bytes could not be read as a font at all.</summary>
        public bool IsValid { get; internal set; }

        /// <summary>Why nothing could be read, when that is the case. Empty on success.</summary>
        public string Error { get; internal set; } = string.Empty;

        /// <summary>True when the font's own GSUB named at least one joined glyph.</summary>
        public bool HasJoiningFeatures => Substitutions > 0;

        /// <summary>How many letters of the ones asked for this font has at all.</summary>
        public int LetterCount => _glyphs.Count;

        /// <summary>
        /// How many joined glyphs the GSUB named. Counted separately from
        /// <see cref="LetterCount"/> because every letter present in the cmap
        /// gets an isolated form for free - it is the glyph the cmap already
        /// points at - and a font whose Arabic script wires up no joining
        /// features at all would otherwise look like it had answered.
        /// </summary>
        public int Substitutions { get; internal set; }

        /// <summary>Every letter this font joins. Ordered arbitrarily.</summary>
        public IEnumerable<char> Letters => _glyphs.Keys;

        /// <summary>
        /// The glyph this font draws <paramref name="letter"/> with in
        /// <paramref name="form"/>, or 0 when the font makes no substitution
        /// there.
        ///
        /// Zero is the honest answer for two different situations and the
        /// caller does not need to tell them apart: the font may have no such
        /// form, or the form may simply be the letter's own nominal glyph -
        /// which is the normal state of affairs for 'isol', a feature most
        /// fonts leave out precisely because the isolated shape is the glyph
        /// the cmap already points at.
        /// </summary>
        public uint GlyphIndex(char letter, DirectForm form)
        {
            int index = (int)form;
            if (index < 0 || index > 3) { return 0; }
            return _glyphs.TryGetValue(letter, out uint[] forms) ? forms[index] : 0u;
        }

        /// <summary>True when this font substitutes a distinct glyph for that form.</summary>
        public bool Has(char letter, DirectForm form) => GlyphIndex(letter, form) != 0;

        // lam + alef, keyed by the alef and the shape the ligature takes.
        // Only isolated and final exist: the ligature ends in an alef.
        private readonly Dictionary<int, uint> _ligatures = new Dictionary<int, uint>();

        private static int LigatureKey(char alef, DirectForm form)
            => (alef << 2) | ((int)form & 3);

        /// <summary>
        /// The single glyph this font draws lam followed by
        /// <paramref name="alef"/> with, or 0 when it expresses that ligature
        /// some way this reader does not follow.
        /// </summary>
        public uint LigatureGlyph(char alef, DirectForm form)
            => _ligatures.TryGetValue(LigatureKey(alef, form), out uint glyph) ? glyph : 0u;

        /// <summary>How many lam-alef ligatures were found.</summary>
        public int LigatureCount => _ligatures.Count;

        internal void SetLigature(char alef, DirectForm form, uint glyph)
        {
            if (glyph == 0) { return; }
            _ligatures[LigatureKey(alef, form)] = glyph;
        }

        /// <summary>The glyph the cmap maps the plain letter to, or 0.</summary>
        public uint NominalGlyph(char letter) => GlyphIndex(letter, DirectForm.Isolated);

        /// <summary>
        /// Records a glyph the font's GSUB named. Substitutions onto a letter
        /// the cmap does not have are dropped: a lookup covers glyph ids, and
        /// a glyph id we never asked about is some other script's letter.
        /// </summary>
        internal void Set(char letter, DirectForm form, uint glyph)
        {
            Set(letter, form, glyph, true);
        }

        /// <summary>
        /// The same, for a form the font's Arabic features COVER but do not
        /// move the glyph for.
        ///
        /// That is a real answer, not the absence of one. A great many faces
        /// draw ر, ز, و and د identically standing alone and at the end of a
        /// word, so their 'fina' lookup names the letter and substitutes it
        /// with itself. Reading that as "this font has no final reh" left the
        /// presentation codepoint U+FEAE unregistered, which made the shaper
        /// treat reh as unjoinable, which dropped the letter BEFORE it out of
        /// its initial shape. One honest non-substitution, two broken letters.
        ///
        /// <paramref name="moved"/> is false for those, so they never count
        /// towards <see cref="Substitutions"/>: a font that wires no joining
        /// features to Arabic at all must still come back saying so.
        /// </summary>
        internal void Set(char letter, DirectForm form, uint glyph, bool moved)
        {
            if (glyph == 0 || !_glyphs.TryGetValue(letter, out uint[] forms)) { return; }

            forms[(int)form] = glyph;
            if (moved) { Substitutions++; }
        }

        /// <summary>
        /// The glyph the cmap maps the plain letter to, which is also its
        /// isolated form until an 'isol' feature says otherwise.
        /// </summary>
        internal void SetNominal(char letter, uint glyph)
        {
            if (glyph == 0) { return; }

            var forms = new uint[4];
            forms[(int)DirectForm.Isolated] = glyph;
            _glyphs[letter] = forms;
        }
    }

    /// <summary>
    /// Reads the Arabic joining substitutions out of a font's OpenType GSUB
    /// table. Never throws: an unreadable font comes back with
    /// <see cref="DirectJoinedGlyphs.IsValid"/> false and a reason.
    /// </summary>
    public static class DirectFontGsub
    {
        // A table directory of more than a few dozen entries is corrupt or
        // hostile, and walking it would be this parser's own denial of service.
        private const int MaxTables = 512;

        // Bounds on the structures below. Every one of them is a count read
        // straight out of the file, so every one of them is attacker-controlled
        // in the only sense that matters: a truncated download can say anything.
        private const int MaxScripts = 512;
        private const int MaxFeatures = 4096;
        private const int MaxLookups = 4096;
        private const int MaxSubtables = 512;
        private const int MaxCoverage = 65536;

        private const uint TagTtcf = 0x74746366;   // 'ttcf'

        // The four features, in DirectForm order, as their big-endian
        // tag words. Comparing an integer is both faster and less error-prone
        // than pulling four bytes back out as a string per feature record.
        private static readonly uint[] FeatureTags =
        {
            0x69736F6C,   // 'isol'
            0x66696E61,   // 'fina'
            0x696E6974,   // 'init'
            0x6D656469    // 'medi'
        };

        private const uint ScriptArabic = 0x61726162;   // 'arab'
        private const uint ScriptDefault = 0x44464C54;  // 'DFLT'

        // ==========================================
        // Read
        // ==========================================
        /// <summary>
        /// Reads how <paramref name="data"/> joins each of
        /// <paramref name="letters"/>.
        ///
        /// Only the letters asked for are resolved, and only substitutions
        /// FROM one of them are recorded - so the cost is a scan of the
        /// joining lookups rather than a model of the whole font, and a CJK
        /// face with forty thousand glyphs is as cheap to ask as a Persian one.
        /// </summary>
        public static DirectJoinedGlyphs Read(byte[] data, IList<char> letters)
        {
            var result = new DirectJoinedGlyphs();

            if (data == null || data.Length < 12)
            {
                result.Error = "Not a font: fewer than 12 bytes.";
                return result;
            }
            if (letters == null || letters.Count == 0)
            {
                result.Error = "No letters asked for.";
                return result;
            }

            try
            {
                int sfnt = 0;
                if (ReadU32(data, 0) == TagTtcf)
                {
                    if (data.Length < 16) { result.Error = "Truncated .ttc header."; return result; }
                    sfnt = (int)ReadU32(data, 12);
                    if (sfnt < 0 || sfnt + 12 > data.Length) { result.Error = "Bad .ttc face offset."; return result; }
                }

                Dictionary<uint, int> tables = ReadTableDirectory(data, sfnt);
                if (tables == null) { result.Error = "Unreadable table directory."; return result; }

                if (!tables.TryGetValue(Tag("cmap"), out int cmap))
                {
                    result.Error = "The font maps no characters (no cmap table).";
                    return result;
                }

                // --- the letters, as this font's own glyph ids ---
                var nominal = new Dictionary<char, uint>();
                int subtable = BestCmapSubtable(data, cmap);
                if (subtable < 0)
                {
                    result.Error = "No usable cmap subtable.";
                    return result;
                }

                for (int i = 0; i < letters.Count; i++)
                {
                    char letter = letters[i];
                    uint glyph = LookupGlyph(data, subtable, letter);
                    if (glyph == 0) { continue; }

                    // The nominal glyph doubles as the isolated form. A font
                    // that also declares 'isol' overwrites this below; most do
                    // not, because for most letters there is nothing to say.
                    result.SetNominal(letter, glyph);
                    nominal[letter] = glyph;
                }

                result.IsValid = true;
                if (nominal.Count == 0)
                {
                    result.Error = "The font has none of those letters.";
                    return result;
                }

                if (!tables.TryGetValue(Tag("GSUB"), out int gsub))
                {
                    // A perfectly ordinary font, just not one with OpenType
                    // shaping in it. The presentation blocks are all there is,
                    // and the caller falls back to them.
                    result.Error = "The font has no GSUB table.";
                    return result;
                }

                ReadJoiningFeatures(data, gsub, nominal, result);
                ReadLamAlefLigatures(data, gsub, result);
                return result;
            }
            catch (Exception e)
            {
                result.Error = e.Message;
                return result;
            }
        }

        // ==========================================
        // GSUB: script -> features -> lookups
        //
        // A feature is only in force for the scripts that
        // reference it, and a font may well carry an
        // 'init' for Syriac that has nothing to do with
        // Arabic. So the Arabic script's own feature
        // indices decide what gets read - and a font that
        // wires none of them to 'arab' genuinely cannot
        // set Arabic, which is the right answer to give
        // back rather than a set of Syriac substitutions.
        // ==========================================
        private static void ReadJoiningFeatures(
            byte[] data, int gsub, Dictionary<char, uint> nominal, DirectJoinedGlyphs result)
        {
            if (gsub + 10 > data.Length) { result.Error = "Truncated GSUB header."; return; }

            int scriptList = gsub + ReadU16(data, gsub + 4);
            int featureList = gsub + ReadU16(data, gsub + 6);
            int lookupList = gsub + ReadU16(data, gsub + 8);

            HashSet<int> wanted = ScriptFeatures(data, scriptList);
            if (wanted == null || wanted.Count == 0)
            {
                result.Error = "The font's GSUB does not wire any feature to the Arabic script.";
                return;
            }

            if (featureList + 2 > data.Length) { return; }
            int featureCount = ReadU16(data, featureList);
            if (featureCount <= 0 || featureCount > MaxFeatures) { return; }
            if (featureList + 2 + (featureCount * 6) > data.Length) { return; }

            // One working set of glyphs per joining form, each starting as the
            // plain letters. A feature's lookups are applied to it in order,
            // so a font that reaches a form in two steps - a joining lookup
            // and then a localized-form one, which is how a Persian face
            // swaps in its own kaf - ends up where the font meant it to.
            var current = new Dictionary<char, uint>[4];

            // Which letters each form's lookups actually COVERED, whether
            // or not the substitution moved the glyph. A font whose final
            // reh is drawn with the same glyph as its isolated reh - which
            // is most of them - covers reh in 'fina' and substitutes it
            // with itself, and that is the font saying "this is my final
            // reh", not the font saying nothing.
            var covered = new HashSet<char>[4];

            for (int i = 0; i < featureCount; i++)
            {
                if (!wanted.Contains(i)) { continue; }

                int record = featureList + 2 + (i * 6);
                int form = FormOf(ReadU32(data, record));
                if (form < 0) { continue; }

                int feature = featureList + ReadU16(data, record + 4);
                if (feature + 4 > data.Length) { continue; }

                int lookupCount = ReadU16(data, feature + 2);
                if (lookupCount <= 0 || lookupCount > MaxLookups) { continue; }
                if (feature + 4 + (lookupCount * 2) > data.Length) { continue; }

                current[form] = current[form] ?? new Dictionary<char, uint>(nominal);
                covered[form] = covered[form] ?? new HashSet<char>();

                for (int k = 0; k < lookupCount; k++)
                {
                    int index = ReadU16(data, feature + 4 + (k * 2));
                    int lookup = ResolveLookup(data, lookupList, index);
                    if (lookup < 0) { continue; }

                    ApplyLookup(data, lookup, current[form], covered[form]);
                }
            }

            for (int form = 0; form < 4; form++)
            {
                if (current[form] == null) { continue; }

                foreach (KeyValuePair<char, uint> letter in current[form])
                {
                    bool moved = letter.Value != nominal[letter.Key];

                    // A letter the feature never touched has no form here -
                    // an 'init' lookup covering only پ says nothing about ب.
                    if (!moved && (covered[form] == null || !covered[form].Contains(letter.Key))) { continue; }

                    result.Set(letter.Key, (DirectForm)form, letter.Value, moved);
                }
            }

            if (result.Substitutions == 0)
            {
                result.Error = "The font's GSUB names no joined glyphs for these letters.";
            }
        }

        // ==========================================
        // lam-alef
        //
        // The one ligature Arabic will not do without: لا
        // is a single glyph, and a font asked to draw the
        // two letters separately draws something no
        // reader accepts.
        //
        // The subtlety that makes this worth parsing is
        // the ORDER. OpenType runs the joining features
        // first, and 'rlig' consumes their output - the
        // real entry in a font is
        //
        //     lam-INITIAL + alef-FINAL  ->  lam-alef
        //
        // not lam + alef. Looking the ligature up under
        // the plain letters, which is the obvious thing
        // to do, finds nothing in any font on this
        // machine. So this runs after ReadJoiningFeatures
        // and matches on the SHAPED glyphs.
        //
        // A lam followed by an alef is initial when
        // nothing joins into it and medial when something
        // does; the ligature it forms is correspondingly
        // isolated or final. Fonts that key their rule on
        // the isolated and final lam instead are read the
        // same way.
        // ==========================================
        private const uint FeatureRlig = 0x726C6967;   // 'rlig'
        private const uint FeatureLiga = 0x6C696761;   // 'liga'

        private static readonly char[] Alefs = { 'آ', 'أ', 'إ', 'ا' };

        private static void ReadLamAlefLigatures(byte[] data, int gsub, DirectJoinedGlyphs result)
        {
            if (gsub + 10 > data.Length) { return; }

            // Every glyph the font may use for a lam, and the shape the
            // resulting ligature takes when it does.
            var lams = new Dictionary<uint, DirectForm>();
            AddLam(result, DirectForm.Initial, DirectForm.Isolated, lams);
            AddLam(result, DirectForm.Isolated, DirectForm.Isolated, lams);
            AddLam(result, DirectForm.Medial, DirectForm.Final, lams);
            AddLam(result, DirectForm.Final, DirectForm.Final, lams);
            if (lams.Count == 0) { return; }

            // Every glyph the font may use for the alef that follows.
            var alefs = new Dictionary<uint, char>();
            for (int i = 0; i < Alefs.Length; i++)
            {
                AddAlef(result, Alefs[i], DirectForm.Final, alefs);
                AddAlef(result, Alefs[i], DirectForm.Isolated, alefs);
            }
            if (alefs.Count == 0) { return; }

            int scriptList = gsub + ReadU16(data, gsub + 4);
            int featureList = gsub + ReadU16(data, gsub + 6);
            int lookupList = gsub + ReadU16(data, gsub + 8);

            HashSet<int> wanted = ScriptFeatures(data, scriptList);
            if (wanted == null || wanted.Count == 0) { return; }

            if (featureList + 2 > data.Length) { return; }
            int featureCount = ReadU16(data, featureList);
            if (featureCount <= 0 || featureCount > MaxFeatures) { return; }
            if (featureList + 2 + (featureCount * 6) > data.Length) { return; }

            for (int i = 0; i < featureCount; i++)
            {
                if (!wanted.Contains(i)) { continue; }

                int record = featureList + 2 + (i * 6);
                uint tag = ReadU32(data, record);
                if (tag != FeatureRlig && tag != FeatureLiga) { continue; }

                int feature = featureList + ReadU16(data, record + 4);
                if (feature + 4 > data.Length) { continue; }

                int lookupCount = ReadU16(data, feature + 2);
                if (lookupCount <= 0 || lookupCount > MaxLookups) { continue; }
                if (feature + 4 + (lookupCount * 2) > data.Length) { continue; }

                for (int k = 0; k < lookupCount; k++)
                {
                    int index = ReadU16(data, feature + 4 + (k * 2));
                    int lookup = ResolveLookup(data, lookupList, index);
                    if (lookup < 0) { continue; }

                    ReadLigatureLookup(data, lookup, lams, alefs, result);
                }
            }
        }

        private static void AddLam(
            DirectJoinedGlyphs result, DirectForm lamForm, DirectForm ligatureForm,
            Dictionary<uint, DirectForm> into)
        {
            uint glyph = result.GlyphIndex(DirectJoining.Lam, lamForm);

            // A medial lam and a final lam are often the same glyph. The
            // ligature that follows a joined lam is the final one, so a
            // shape that maps to both must not be recorded as isolated.
            if (glyph == 0) { return; }
            if (into.TryGetValue(glyph, out DirectForm existing) && existing == DirectForm.Final) { return; }

            into[glyph] = ligatureForm;
        }

        private static void AddAlef(
            DirectJoinedGlyphs result, char alef, DirectForm form, Dictionary<uint, char> into)
        {
            uint glyph = result.GlyphIndex(alef, form);
            if (glyph != 0 && !into.ContainsKey(glyph)) { into[glyph] = alef; }
        }

        // LookupType 4, format 1. Coverage names the FIRST glyph of each
        // ligature; the ligature set at the same index holds the rules that
        // start with it. Only two-glyph rules are of interest - lam-alef is
        // the whole reason this is here, and a longer rule is a Nastaliq
        // flourish this package does not try to be.
        private static void ReadLigatureLookup(
            byte[] data, int lookup,
            Dictionary<uint, DirectForm> lams, Dictionary<uint, char> alefs,
            DirectJoinedGlyphs result)
        {
            if (lookup < 0 || lookup + 6 > data.Length) { return; }

            int type = ReadU16(data, lookup);
            if (type != 4 && type != 7) { return; }

            int count = ReadU16(data, lookup + 4);
            if (count <= 0 || count > MaxSubtables) { return; }
            if (lookup + 6 + (count * 2) > data.Length) { return; }

            for (int i = 0; i < count; i++)
            {
                int subtable = lookup + ReadU16(data, lookup + 6 + (i * 2));

                if (type == 7)
                {
                    // An extension is a 32-bit redirection to a real subtable,
                    // used by fonts too large for 16-bit offsets.
                    if (subtable + 8 > data.Length) { continue; }
                    if (ReadU16(data, subtable) != 1) { continue; }
                    if (ReadU16(data, subtable + 2) != 4) { continue; }
                    subtable += (int)ReadU32(data, subtable + 4);
                }

                ReadLigatureSubtable(data, subtable, lams, alefs, result);
            }
        }

        private static void ReadLigatureSubtable(
            byte[] data, int subtable,
            Dictionary<uint, DirectForm> lams, Dictionary<uint, char> alefs,
            DirectJoinedGlyphs result)
        {
            if (subtable < 0 || subtable + 6 > data.Length) { return; }
            if (ReadU16(data, subtable) != 1) { return; }

            int coverage = subtable + ReadU16(data, subtable + 2);
            int setCount = ReadU16(data, subtable + 4);
            if (setCount <= 0 || setCount > MaxCoverage) { return; }
            if (subtable + 6 + (setCount * 2) > data.Length) { return; }

            foreach (KeyValuePair<uint, int> covered in Coverage(data, coverage))
            {
                if (!lams.TryGetValue(covered.Key, out DirectForm ligatureForm)) { continue; }
                if (covered.Value < 0 || covered.Value >= setCount) { continue; }

                int set = subtable + ReadU16(data, subtable + 6 + (covered.Value * 2));
                if (set + 2 > data.Length) { continue; }

                int ligatureCount = ReadU16(data, set);
                if (ligatureCount <= 0 || ligatureCount > MaxCoverage) { continue; }
                if (set + 2 + (ligatureCount * 2) > data.Length) { continue; }

                for (int i = 0; i < ligatureCount; i++)
                {
                    int ligature = set + ReadU16(data, set + 2 + (i * 2));
                    if (ligature + 4 > data.Length) { continue; }

                    uint glyph = (uint)ReadU16(data, ligature);
                    int components = ReadU16(data, ligature + 2);

                    // componentCount counts the first glyph, which coverage
                    // already gave us, so a lam-alef rule has exactly one
                    // component listed.
                    if (components != 2) { continue; }
                    if (ligature + 6 > data.Length) { continue; }

                    uint second = (uint)ReadU16(data, ligature + 4);
                    if (!alefs.TryGetValue(second, out char alef)) { continue; }

                    result.SetLigature(alef, ligatureForm, glyph);
                }
            }
        }

        // Which feature indices the Arabic script uses - its default language
        // system plus every language-specific one, because a font may hang
        // Persian's forms off a 'FAR ' language system rather than the default.
        private static HashSet<int> ScriptFeatures(byte[] data, int scriptList)
        {
            if (scriptList + 2 > data.Length) { return null; }

            int count = ReadU16(data, scriptList);
            if (count <= 0 || count > MaxScripts) { return null; }
            if (scriptList + 2 + (count * 6) > data.Length) { return null; }

            int chosen = -1;
            for (int i = 0; i < count; i++)
            {
                int record = scriptList + 2 + (i * 6);
                uint tag = ReadU32(data, record);

                if (tag == ScriptArabic) { chosen = scriptList + ReadU16(data, record + 4); break; }
                if (chosen < 0 && tag == ScriptDefault) { chosen = scriptList + ReadU16(data, record + 4); }
            }

            if (chosen < 0 || chosen + 4 > data.Length) { return null; }

            var features = new HashSet<int>();

            int defaultLangSys = ReadU16(data, chosen);
            if (defaultLangSys != 0) { AddLangSysFeatures(data, chosen + defaultLangSys, features); }

            int langSysCount = ReadU16(data, chosen + 2);
            if (langSysCount > 0 && langSysCount <= MaxScripts
                && chosen + 4 + (langSysCount * 6) <= data.Length)
            {
                for (int i = 0; i < langSysCount; i++)
                {
                    int offset = ReadU16(data, chosen + 4 + (i * 6) + 4);
                    if (offset != 0) { AddLangSysFeatures(data, chosen + offset, features); }
                }
            }

            return features;
        }

        private static void AddLangSysFeatures(byte[] data, int langSys, HashSet<int> into)
        {
            if (langSys < 0 || langSys + 6 > data.Length) { return; }

            int required = ReadU16(data, langSys + 2);
            if (required != 0xFFFF) { into.Add(required); }

            int count = ReadU16(data, langSys + 4);
            if (count <= 0 || count > MaxFeatures) { return; }
            if (langSys + 6 + (count * 2) > data.Length) { return; }

            for (int i = 0; i < count; i++)
            {
                into.Add(ReadU16(data, langSys + 6 + (i * 2)));
            }
        }

        private static int ResolveLookup(byte[] data, int lookupList, int index)
        {
            if (lookupList + 2 > data.Length) { return -1; }

            int count = ReadU16(data, lookupList);
            if (index < 0 || index >= count || count > MaxLookups) { return -1; }
            if (lookupList + 2 + (count * 2) > data.Length) { return -1; }

            return lookupList + ReadU16(data, lookupList + 2 + (index * 2));
        }

        // ==========================================
        // One lookup
        //
        // Type 1 is the substitution that matters: one
        // glyph for one glyph, which is precisely "this
        // letter, joined this way". Type 7 is not a
        // substitution at all - it is a 32-bit
        // redirection to one, used by fonts too big for
        // 16-bit offsets - so it is followed and then
        // read as whatever it points at. Everything else
        // is skipped: a contextual or ligature rule is
        // not a joining form and pretending it is would
        // put the wrong glyph on the screen.
        //
        // A lookup substitutes at most ONCE per glyph:
        // its subtables are tried in order and the first
        // that covers the glyph wins. FreeSerif lists lam
        // in two subtables of its 'fina' lookup, and
        // taking the second gives a lam that is not the
        // one the font draws - so the rule is not a
        // detail.
        // ==========================================
        private static void ApplyLookup(
            byte[] data, int lookup, Dictionary<char, uint> current, HashSet<char> covered)
        {
            if (lookup < 0 || lookup + 6 > data.Length) { return; }

            int type = ReadU16(data, lookup);
            if (type != 1 && type != 7) { return; }

            int count = ReadU16(data, lookup + 4);
            if (count <= 0 || count > MaxSubtables) { return; }
            if (lookup + 6 + (count * 2) > data.Length) { return; }

            // Which letter each glyph in flight belongs to. Rebuilt per lookup
            // because the lookup before it may have moved them.
            var byGlyph = new Dictionary<uint, char>(current.Count);
            foreach (KeyValuePair<char, uint> letter in current) { byGlyph[letter.Value] = letter.Key; }

            var applied = new Dictionary<char, uint>();

            for (int i = 0; i < count; i++)
            {
                int subtable = lookup + ReadU16(data, lookup + 6 + (i * 2));

                if (type == 7)
                {
                    if (subtable + 8 > data.Length) { continue; }
                    if (ReadU16(data, subtable) != 1) { continue; }        // extension format
                    if (ReadU16(data, subtable + 2) != 1) { continue; }    // wrapping a type 1
                    subtable += (int)ReadU32(data, subtable + 4);
                }

                CollectSingleSubstitution(data, subtable, byGlyph, applied);
            }

            foreach (KeyValuePair<char, uint> letter in applied)
            {
                current[letter.Key] = letter.Value;

                // Covered counts even when the glyph did not move. The
                // font named this letter in this joining feature, and
                // that is the fact the caller needs - see
                // DirectJoinedGlyphs.Set(char, form, uint, bool).
                if (covered != null) { covered.Add(letter.Key); }
            }
        }

        private static void CollectSingleSubstitution(
            byte[] data, int subtable, Dictionary<uint, char> byGlyph, Dictionary<char, uint> applied)
        {
            if (subtable < 0 || subtable + 6 > data.Length) { return; }

            int format = ReadU16(data, subtable);
            int coverage = subtable + ReadU16(data, subtable + 2);

            if (format == 1)
            {
                // Every covered glyph moves by the same amount - the layout a
                // font uses when its joined forms sit in one contiguous block.
                int delta = (short)ReadU16(data, subtable + 4);

                foreach (KeyValuePair<uint, int> covered in Coverage(data, coverage))
                {
                    if (!byGlyph.TryGetValue(covered.Key, out char letter)) { continue; }
                    if (applied.ContainsKey(letter)) { continue; }

                    applied[letter] = (uint)((covered.Key + delta) & 0xFFFF);
                }
                return;
            }

            if (format != 2) { return; }

            // An explicit glyph per covered glyph, indexed by coverage order.
            int glyphCount = ReadU16(data, subtable + 4);
            if (glyphCount <= 0 || glyphCount > MaxCoverage) { return; }
            if (subtable + 6 + (glyphCount * 2) > data.Length) { return; }

            foreach (KeyValuePair<uint, int> covered in Coverage(data, coverage))
            {
                if (covered.Value < 0 || covered.Value >= glyphCount) { continue; }
                if (!byGlyph.TryGetValue(covered.Key, out char letter)) { continue; }
                if (applied.ContainsKey(letter)) { continue; }

                applied[letter] = (uint)ReadU16(data, subtable + 6 + (covered.Value * 2));
            }
        }

        // ==========================================
        // Coverage: which glyphs a lookup applies to,
        // and in what order - the order IS the index
        // into a format 2 substitution array, so getting
        // it wrong substitutes the neighbouring letter
        // and produces a word that is spelled wrong
        // rather than one that is drawn wrong.
        // ==========================================
        private static IEnumerable<KeyValuePair<uint, int>> Coverage(byte[] data, int coverage)
        {
            if (coverage < 0 || coverage + 4 > data.Length) { yield break; }

            int format = ReadU16(data, coverage);
            int count = ReadU16(data, coverage + 2);

            if (format == 1)
            {
                if (count < 0 || count > MaxCoverage) { yield break; }
                if (coverage + 4 + (count * 2) > data.Length) { yield break; }

                for (int i = 0; i < count; i++)
                {
                    yield return new KeyValuePair<uint, int>((uint)ReadU16(data, coverage + 4 + (i * 2)), i);
                }
                yield break;
            }

            if (format != 2) { yield break; }
            if (count < 0 || count > MaxCoverage) { yield break; }
            if (coverage + 4 + (count * 6) > data.Length) { yield break; }

            for (int i = 0; i < count; i++)
            {
                int record = coverage + 4 + (i * 6);
                int start = ReadU16(data, record);
                int end = ReadU16(data, record + 2);
                int index = ReadU16(data, record + 4);

                if (start > end || end - start > MaxCoverage) { continue; }

                for (int glyph = start; glyph <= end; glyph++)
                {
                    yield return new KeyValuePair<uint, int>((uint)glyph, index + (glyph - start));
                }
            }
        }

        // ==========================================
        // cmap, one codepoint at a time.
        //
        // Only the glyph id is wanted, for a hundred or so
        // characters - so each one is looked up rather
        // than enumerating a cmap of sixty thousand.
        // ==========================================
        private static int BestCmapSubtable(byte[] data, int cmap)
        {
            if (cmap + 4 > data.Length) { return -1; }

            int count = ReadU16(data, cmap + 2);
            if (count <= 0 || count > MaxTables) { return -1; }
            if (cmap + 4 + (count * 8) > data.Length) { return -1; }

            int best = -1;
            int bestScore = -1;

            for (int i = 0; i < count; i++)
            {
                int record = cmap + 4 + (i * 8);
                int platform = ReadU16(data, record);
                int encoding = ReadU16(data, record + 2);
                int offset = cmap + (int)ReadU32(data, record + 4);
                if (offset < 0 || offset + 2 > data.Length) { continue; }

                int score = ScoreEncoding(platform, encoding);
                if (score > bestScore) { bestScore = score; best = offset; }
            }

            return best;
        }

        private static int ScoreEncoding(int platformId, int encodingId)
        {
            if (platformId == 3 && encodingId == 10) { return 100; }
            if (platformId == 0 && encodingId >= 4) { return 90; }
            if (platformId == 3 && encodingId == 1) { return 80; }
            if (platformId == 0) { return 70; }
            if (platformId == 3 && encodingId == 0) { return 20; }
            if (platformId == 1) { return 10; }
            return 0;
        }

        private static uint LookupGlyph(byte[] data, int subtable, int codepoint)
        {
            if (subtable < 0 || subtable + 2 > data.Length) { return 0; }

            switch (ReadU16(data, subtable))
            {
                case 0: return LookupFormat0(data, subtable, codepoint);
                case 4: return LookupFormat4(data, subtable, codepoint);
                case 6: return LookupFormat6(data, subtable, codepoint);
                case 12: return LookupFormat12(data, subtable, codepoint);
                default: return 0;
            }
        }

        private static uint LookupFormat0(byte[] data, int subtable, int codepoint)
        {
            if (codepoint < 0 || codepoint > 255) { return 0; }
            int at = subtable + 6 + codepoint;
            return at < data.Length ? data[at] : 0u;
        }

        private static uint LookupFormat4(byte[] data, int subtable, int codepoint)
        {
            if (codepoint < 0 || codepoint > 0xFFFF) { return 0; }
            if (subtable + 14 > data.Length) { return 0; }

            int segCountX2 = ReadU16(data, subtable + 6);
            int segCount = segCountX2 / 2;
            if (segCount <= 0 || segCount > 32768) { return 0; }

            int endCodes = subtable + 14;
            int startCodes = endCodes + segCountX2 + 2;
            int idDeltas = startCodes + segCountX2;
            int idRangeOffsets = idDeltas + segCountX2;
            if (idRangeOffsets + segCountX2 > data.Length) { return 0; }

            // Segments are sorted by end code, so the segment that could hold
            // this codepoint is one binary search away.
            int low = 0;
            int high = segCount - 1;
            while (low <= high)
            {
                int mid = low + ((high - low) / 2);
                if (codepoint > ReadU16(data, endCodes + (mid * 2))) { low = mid + 1; }
                else { high = mid - 1; }
            }
            if (low >= segCount) { return 0; }

            int start = ReadU16(data, startCodes + (low * 2));
            if (codepoint < start) { return 0; }

            int delta = (short)ReadU16(data, idDeltas + (low * 2));
            int rangeOffset = ReadU16(data, idRangeOffsets + (low * 2));

            if (rangeOffset == 0) { return (uint)((codepoint + delta) & 0xFFFF); }

            int address = idRangeOffsets + (low * 2) + rangeOffset + ((codepoint - start) * 2);
            if (address + 2 > data.Length) { return 0; }

            int glyph = ReadU16(data, address);
            return glyph == 0 ? 0u : (uint)((glyph + delta) & 0xFFFF);
        }

        private static uint LookupFormat6(byte[] data, int subtable, int codepoint)
        {
            if (subtable + 10 > data.Length) { return 0; }

            int first = ReadU16(data, subtable + 6);
            int count = ReadU16(data, subtable + 8);
            if (codepoint < first || codepoint >= first + count) { return 0; }

            int at = subtable + 10 + ((codepoint - first) * 2);
            return at + 2 > data.Length ? 0u : (uint)ReadU16(data, at);
        }

        private static uint LookupFormat12(byte[] data, int subtable, int codepoint)
        {
            if (subtable + 16 > data.Length) { return 0; }

            long groups = ReadU32(data, subtable + 12);
            if (groups <= 0 || groups > 200000) { return 0; }

            int at = subtable + 16;
            if (at + (groups * 12) > data.Length) { return 0; }

            int low = 0;
            int high = (int)groups - 1;
            while (low <= high)
            {
                int mid = low + ((high - low) / 2);
                int record = at + (mid * 12);
                int start = (int)ReadU32(data, record);
                int end = (int)ReadU32(data, record + 4);

                if (codepoint < start) { high = mid - 1; }
                else if (codepoint > end) { low = mid + 1; }
                else { return ReadU32(data, record + 8) + (uint)(codepoint - start); }
            }
            return 0;
        }

        // ==========================================
        // Table directory
        // ==========================================
        private static Dictionary<uint, int> ReadTableDirectory(byte[] data, int sfnt)
        {
            if (sfnt + 12 > data.Length) { return null; }

            int count = ReadU16(data, sfnt + 4);
            if (count <= 0 || count > MaxTables) { return null; }

            int records = sfnt + 12;
            if (records + (count * 16) > data.Length) { return null; }

            var tables = new Dictionary<uint, int>(count);
            for (int i = 0; i < count; i++)
            {
                int record = records + (i * 16);
                int offset = (int)ReadU32(data, record + 8);
                if (offset < 0 || offset >= data.Length) { continue; }
                tables[ReadU32(data, record)] = offset;
            }
            return tables;
        }

        private static int FormOf(uint tag)
        {
            for (int i = 0; i < FeatureTags.Length; i++)
            {
                if (FeatureTags[i] == tag) { return i; }
            }
            return -1;
        }

        private static uint Tag(string four)
        {
            return ((uint)four[0] << 24) | ((uint)four[1] << 16) | ((uint)four[2] << 8) | four[3];
        }

        // ==========================================
        // Big-endian primitives. Every sfnt integer is
        // big-endian whatever the machine is.
        // ==========================================
        private static int ReadU16(byte[] data, int offset)
        {
            if (offset < 0 || offset + 2 > data.Length) { return 0; }
            return (data[offset] << 8) | data[offset + 1];
        }

        private static uint ReadU32(byte[] data, int offset)
        {
            if (offset < 0 || offset + 4 > data.Length) { return 0; }
            return ((uint)data[offset] << 24)
                 | ((uint)data[offset + 1] << 16)
                 | ((uint)data[offset + 2] << 8)
                 | data[offset + 3];
        }
    }
}
