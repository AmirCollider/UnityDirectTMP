// ==========================================
// DirectFontForms
// Gives a font the joined shapes it already has.
//
// DirectArabicShaper turns "پروژه" into a run of
// presentation-form codepoints, because those are the
// only way to ask a font for a joined letter through a
// cmap. That works until it meets a font carrying the
// Arabic block (U+FE70..FEFF) and not the Persian one
// (U+FB50..FBFF) - which is Segoe UI, and therefore most
// Windows machines. Then ر و ه ن join and پ چ ژ گ do
// not, and the word breaks in the middle at exactly the
// letters that make it Persian.
//
// 1.1.1 answered that by standing those letters alone,
// so at least nothing drew a stroke into empty space.
// It was still a broken-looking word, and the letter
// that got the blame was still ی.
//
// The premise was wrong. The font is not missing the
// SHAPES - every font that sets Arabic has an initial
// peh, because that is what its OpenType 'init' feature
// is for. It is missing the CODEPOINTS that point at
// them. So:
//
//   1. DirectFontGsub reads the font's own init / medi /
//      fina / isol lookups and reports, per letter, the
//      glyph index the font would use.
//   2. This file rasterizes that glyph into the label's
//      atlas and registers it in the TextMeshPro font
//      asset under the presentation codepoint the shaper
//      wants to emit.
//
// After which U+FB58 IS in the font asset, HasCharacter
// says so, and the shaper - unchanged - emits it and
// gets a properly joined peh. The missing block stops
// mattering.
//
// ==========================================
// EVERY STEP IS ALLOWED TO FAIL
// ==========================================
// Reading a font's GSUB, rasterizing by glyph index and
// writing into TextMeshPro's lookup tables are three
// things that depend on the shape of somebody else's
// data and the internals of somebody else's package. So
// none of them is required to work: any failure at any
// point leaves the font asset exactly as it was, and the
// shaper falls back to the 1.1.1 behaviour it has always
// had. The worst outcome of this file is the behaviour
// that shipped without it.
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
    /// What <see cref="DirectFontForms.TopUp"/> managed to do for one font
    /// asset. Cached per asset and read by the Inspector, so a user can see
    /// whether their font needed help and whether it got any.
    /// </summary>
    public sealed class DirectFontFormsReport
    {
        /// <summary>True once a top-up has been attempted for this asset.</summary>
        public bool Attempted { get; internal set; }

        /// <summary>How many presentation forms were added to the font asset.</summary>
        public int FormsAdded { get; internal set; }

        /// <summary>The letters that can now be joined and could not before, space separated.</summary>
        public string LettersJoined { get; internal set; } = string.Empty;

        /// <summary>Why nothing was added, when nothing was. Empty on success.</summary>
        public string Reason { get; internal set; } = string.Empty;

        /// <summary>True when this font needed no help - it had every form already.</summary>
        public bool NothingMissing { get; internal set; }

        internal static readonly DirectFontFormsReport Empty = new DirectFontFormsReport();
    }

    /// <summary>
    /// Fills in the Arabic-script presentation forms a font asset is missing,
    /// using the glyphs the font's own OpenType tables would use for them.
    /// </summary>
    public static class DirectFontForms
    {
        // Reports and remembered sources are keyed by instance id rather than
        // by reference: a font asset that Unity destroys on a domain reload
        // must not keep this table alive, and an int cannot.
        private static readonly Dictionary<int, DirectFontFormsReport> s_reports = new Dictionary<int, DirectFontFormsReport>();
        private static readonly Dictionary<int, DirectFontSource> s_sources = new Dictionary<int, DirectFontSource>();

        private static bool s_glyphAdderResolved;
        private static MethodInfo s_glyphAdder;

        private struct DirectFontSource
        {
            public string Path;
            public byte[] Bytes;
        }

        // ==========================================
        // Remember
        //
        // The bytes are needed to read GSUB, and a
        // TMP_FontAsset does not keep them: it holds a
        // Unity Font, whose file is not readable from a
        // player build. So whoever built the asset from a
        // path or a buffer says so here, and the Editor
        // can always fall back to the imported asset's
        // own file.
        // ==========================================
        /// <summary>Records where a font asset's bytes came from, for later GSUB reading.</summary>
        internal static void Remember(TMP_FontAsset asset, string path, byte[] bytes)
        {
            if (asset == null) { return; }
            if (string.IsNullOrEmpty(path) && (bytes == null || bytes.Length == 0)) { return; }

            s_sources[asset.GetInstanceID()] = new DirectFontSource { Path = path, Bytes = bytes };
        }

        /// <summary>Forgets everything known about every asset. Used when the font cache is cleared.</summary>
        internal static void Forget()
        {
            s_reports.Clear();
            s_sources.Clear();
        }

        // ==========================================
        // TopUp
        //
        // Idempotent and cached: the answer is a property
        // of the font, and the question is asked whenever
        // a label's font changes.
        // ==========================================
        /// <summary>
        /// Adds to <paramref name="asset"/> every Arabic-script presentation
        /// form it does not already have but whose glyph its own OpenType
        /// tables can name. Returns what happened, and never throws.
        /// </summary>
        public static DirectFontFormsReport TopUp(TMP_FontAsset asset)
        {
            if (asset == null) { return DirectFontFormsReport.Empty; }

            int id = asset.GetInstanceID();
            if (s_reports.TryGetValue(id, out DirectFontFormsReport cached)) { return cached; }

            var report = new DirectFontFormsReport { Attempted = true };
            s_reports[id] = report;

            try
            {
                Fill(asset, report);
            }
            catch (Exception e)
            {
                // Not a warning. This is an optimisation over the behaviour
                // that already works, and a user whose font needs no help
                // should never learn that it exists.
                report.Reason = e.Message;
            }

            return report;
        }

        /// <summary>What the last top-up did for this asset, without starting one.</summary>
        public static DirectFontFormsReport ReportFor(TMP_FontAsset asset)
        {
            if (asset == null) { return DirectFontFormsReport.Empty; }
            return s_reports.TryGetValue(asset.GetInstanceID(), out DirectFontFormsReport report)
                ? report
                : DirectFontFormsReport.Empty;
        }

        // ==========================================
        // The work
        // ==========================================
        private static void Fill(TMP_FontAsset asset, DirectFontFormsReport report)
        {
            byte[] data = BytesFor(asset);
            if (data == null)
            {
                report.Reason = "The font's file could not be read, so its own joining rules could not be asked for.";
                return;
            }

            // Which forms are missing is a question for the font FILE, not for
            // the atlas. A dynamic font asset only knows about the characters
            // it has been asked to draw so far, so asking it would report the
            // whole alphabet missing - and answering that question with
            // `tryAddCharacter` would rasterize three hundred glyphs into the
            // atlas of a label that is about to draw eleven.
            DirectFontFileInfo file = DirectFontFile.Parse(data);
            if (!file.IsValid)
            {
                report.Reason = file.Error;
                return;
            }

            var letters = new List<char>();
            var wanted = new List<char[]>();

            var forms = new char[4];
            foreach (char letter in DirectArabicShaper.JoiningLetters)
            {
                if (!DirectArabicShaper.TryGetForms(letter, forms)) { continue; }

                // A letter the font does not have at all is not this file's
                // problem: there is no glyph to join, joined or otherwise.
                if (!file.HasCodepoint(letter)) { continue; }

                char[] missing = null;
                for (int i = 0; i < 4; i++)
                {
                    if (forms[i] == '\0') { continue; }
                    if (file.HasCodepoint(forms[i])) { continue; }

                    missing = missing ?? new char[4];
                    missing[i] = forms[i];
                }

                if (missing == null) { continue; }
                letters.Add(letter);
                wanted.Add(missing);
            }

            // A font carrying both presentation blocks in full - Vazirmatn,
            // Noto Naskh Arabic, most of what a Persian designer reaches for -
            // stops here and pays nothing else.
            if (letters.Count == 0)
            {
                report.NothingMissing = true;
                return;
            }

            DirectJoinedGlyphs joined = DirectFontGsub.Read(data, letters);
            if (!joined.IsValid || !joined.HasJoiningFeatures)
            {
                report.Reason = string.IsNullOrEmpty(joined.Error)
                    ? "The font has no OpenType joining rules."
                    : joined.Error;
                return;
            }

            if (!LoadFace(asset))
            {
                report.Reason = "The font face could not be opened for rasterizing.";
                return;
            }

            string letterList = null;
            for (int i = 0; i < letters.Count; i++)
            {
                char letter = letters[i];
                char[] missing = wanted[i];

                bool any = false;
                for (int f = 0; f < 4; f++)
                {
                    if (missing[f] == '\0') { continue; }

                    uint glyphIndex = joined.GlyphIndex(letter, (DirectJoiningForm)f);
                    if (glyphIndex == 0) { continue; }

                    if (!Register(asset, missing[f], glyphIndex)) { continue; }

                    report.FormsAdded++;
                    any = true;
                }

                if (any) { letterList = letterList == null ? letter.ToString() : letterList + " " + letter; }
            }

            report.LettersJoined = letterList ?? string.Empty;

            if (report.FormsAdded == 0)
            {
                report.Reason = "The font names no joined glyphs for the letters it is missing forms for.";
                return;
            }

            // The lookup dictionaries are rebuilt from the character table, so
            // the additions have to survive a re-read - which anything that
            // touches the asset can trigger.
            asset.ReadFontAssetDefinition();
        }

        // ==========================================
        // Rasterize one glyph, by index, and give it a
        // codepoint.
        //
        // TextMeshPro addresses glyphs by character
        // everywhere except here: TryAddGlyphInternal is
        // the one entry point that takes a glyph index,
        // and it is internal, so it is called
        // reflectively. When it cannot be found the
        // top-up simply does not happen.
        // ==========================================
        private static bool Register(TMP_FontAsset asset, char codepoint, uint glyphIndex)
        {
            Glyph glyph = AddGlyph(asset, glyphIndex);
            if (glyph == null) { return false; }

            try
            {
                var character = new TMP_Character(codepoint, glyph) { textAsset = asset };

                asset.characterTable.Add(character);
                asset.characterLookupTable[codepoint] = character;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Glyph AddGlyph(TMP_FontAsset asset, uint glyphIndex)
        {
            // Already rasterized - another form of another letter may well be
            // the same glyph, and a font that reuses one is common.
            if (asset.glyphLookupTable != null
                && asset.glyphLookupTable.TryGetValue(glyphIndex, out Glyph existing)
                && existing != null)
            {
                return existing;
            }

            MethodInfo adder = ResolveGlyphAdder();
            if (adder == null) { return null; }

            try
            {
                object[] args = { glyphIndex, null };
                bool added = adder.Invoke(asset, args) is bool answer && answer;
                return added ? args[1] as Glyph : null;
            }
            catch
            {
                return null;
            }
        }

        private static MethodInfo ResolveGlyphAdder()
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

        // The FontEngine keeps ONE face open at a time, and whichever call
        // last loaded one owns it. Rasterizing by index needs this asset's
        // face to be the current one.
        private static bool LoadFace(TMP_FontAsset asset)
        {
            try
            {
                // Cast rather than compare: FaceInfo.pointSize is an int on
                // some TextCore versions and a float on others, and this file
                // has to compile against both.
                int pointSize = (int)asset.faceInfo.pointSize;
                if (pointSize <= 0) { pointSize = DirectTMPConstants.DefaultSamplingPointSize; }

                if (asset.sourceFontFile != null
                    && FontEngine.LoadFontFace(asset.sourceFontFile, pointSize) == FontEngineError.Success)
                {
                    return true;
                }

                if (s_sources.TryGetValue(asset.GetInstanceID(), out DirectFontSource source)
                    && !string.IsNullOrEmpty(source.Path)
                    && FontEngine.LoadFontFace(source.Path, pointSize) == FontEngineError.Success)
                {
                    return true;
                }

                // TryAddGlyphInternal loads the asset's own face when it has to,
                // so a failure here is not necessarily the end of it.
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ==========================================
        // Where the bytes are
        //
        // In order of how much we can trust them: the
        // buffer or path this package was handed, then -
        // in the Editor only - the imported font asset's
        // own file. A player build given a Font asset has
        // no route to the bytes at all, and says so.
        // ==========================================
        private static byte[] BytesFor(TMP_FontAsset asset)
        {
            if (s_sources.TryGetValue(asset.GetInstanceID(), out DirectFontSource source))
            {
                if (source.Bytes != null && source.Bytes.Length > 0) { return source.Bytes; }

                byte[] fromPath = ReadAllBytes(source.Path);
                if (fromPath != null) { return fromPath; }
            }

#if UNITY_EDITOR
            if (asset.sourceFontFile != null)
            {
                string path = UnityEditor.AssetDatabase.GetAssetPath(asset.sourceFontFile);
                if (!string.IsNullOrEmpty(path))
                {
                    byte[] imported = ReadAllBytes(System.IO.Path.GetFullPath(path));
                    if (imported != null) { return imported; }
                }
            }
#endif
            return null;
        }

        private static byte[] ReadAllBytes(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) { return null; }
                return System.IO.File.ReadAllBytes(path);
            }
            catch
            {
                return null;
            }
        }
    }
}
