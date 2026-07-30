// ==========================================
// DirectFontFile
// Reads a .ttf / .otf / .ttc without asking Unity,
// TextMeshPro or the FontEngine for permission.
//
// Why a font parser lives in a TextMeshPro helper:
// the Font Catalog has to answer "what is this font
// called, how many glyphs does it have, and does it
// contain Persian?" for every font file in a project
// - forty of them, on a window that has to open in
// under a second. Building a dynamic atlas per font
// to find out would rasterize forty SDF textures to
// answer a question that is written in plain bytes
// twelve hundred bytes into the file.
//
// So this reads the sfnt tables directly:
//   name   the family and style, as the foundry wrote
//          them (Unity's Font.name is the FILENAME,
//          which is why a project full of
//          "NotoSansJP-Regular" tells you nothing)
//   maxp   the real glyph count
//   head   units per em
//   cmap   exactly which codepoints are mapped, which
//          is the only honest source for a coverage
//          badge
//
// Every read is bounds-checked against the buffer and
// every failure returns an invalid result carrying a
// reason. A corrupt or truncated font is a thing that
// happens - a half-finished download, a Git LFS
// pointer committed as the font itself - and it must
// produce a catalog row that says so, never an
// exception that takes the window down.
// ==========================================
using System;
using System.Collections.Generic;
using System.Text;

namespace UnityDirectTMP
{
    /// <summary>
    /// An inclusive range of Unicode codepoints a font maps to real glyphs.
    /// Coverage is stored as ranges rather than a set because a CJK font maps
    /// tens of thousands of codepoints, and a catalog holds dozens of fonts.
    /// </summary>
    public readonly struct DirectCodepointRange
    {
        public readonly int Start;
        public readonly int End;

        public DirectCodepointRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        public int Count => End - Start + 1;

        public bool Contains(int codepoint) => codepoint >= Start && codepoint <= End;

        public override string ToString() => $"U+{Start:X4}..U+{End:X4}";
    }

    /// <summary>
    /// What a font file says about itself. Produced by
    /// <see cref="DirectFontFile"/>; consumed by the Font Catalog, the Editor
    /// Font picker and the Inspector read-out.
    /// </summary>
    public sealed class DirectFontFileInfo
    {
        /// <summary>False when the bytes could not be read as a font at all.</summary>
        public bool IsValid { get; internal set; }

        /// <summary>Why the read failed, when it did. Empty on success.</summary>
        public string Error { get; internal set; } = string.Empty;

        /// <summary>Where the file came from. May be empty for an in-memory parse.</summary>
        public string FilePath { get; internal set; } = string.Empty;

        /// <summary>The family name the foundry wrote into the font, e.g. "Noto Sans JP".</summary>
        public string FamilyName { get; internal set; } = string.Empty;

        /// <summary>The style within the family, e.g. "Regular", "Bold Italic".</summary>
        public string StyleName { get; internal set; } = string.Empty;

        /// <summary>How many glyphs the face contains, from the maxp table.</summary>
        public int GlyphCount { get; internal set; }

        /// <summary>Design units per em, from the head table. 1000 or 2048, usually.</summary>
        public int UnitsPerEm { get; internal set; }

        /// <summary>
        /// How many faces the file holds. 1 for a normal .ttf/.otf; more for a
        /// .ttc/.otc collection, which the catalog flags because DirectTMP
        /// currently rasterizes face 0 only.
        /// </summary>
        public int FaceCount { get; internal set; } = 1;

        /// <summary>True when the outlines are CFF (an OpenType/PostScript font).</summary>
        public bool IsCff { get; internal set; }

        /// <summary>Size of the file on disk, in bytes. Zero for an in-memory parse.</summary>
        public long FileBytes { get; internal set; }

        /// <summary>Every codepoint range the font maps, ascending and merged.</summary>
        public IReadOnlyList<DirectCodepointRange> CoveredRanges { get; internal set; } = new DirectCodepointRange[0];

        /// <summary>How many distinct codepoints are mapped in total.</summary>
        public int CodepointCount { get; internal set; }

        /// <summary>
        /// A display name that is always something: the foundry's family and
        /// style when they are there, the file name when they are not.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(FamilyName))
                {
                    return string.IsNullOrEmpty(FilePath)
                        ? "Unknown font"
                        : System.IO.Path.GetFileNameWithoutExtension(FilePath);
                }
                return string.IsNullOrEmpty(StyleName) || StyleName == "Regular"
                    ? FamilyName
                    : FamilyName + " " + StyleName;
            }
        }

        /// <summary>
        /// Is this codepoint mapped to a real glyph? Binary search over the
        /// merged ranges, so it stays cheap enough to call once per probe
        /// character per font while a catalog window is painting.
        /// </summary>
        public bool HasCodepoint(int codepoint)
        {
            IReadOnlyList<DirectCodepointRange> ranges = CoveredRanges;
            if (ranges == null || ranges.Count == 0) { return false; }

            int low = 0;
            int high = ranges.Count - 1;
            while (low <= high)
            {
                int mid = low + ((high - low) / 2);
                DirectCodepointRange range = ranges[mid];
                if (codepoint < range.Start) { high = mid - 1; }
                else if (codepoint > range.End) { low = mid + 1; }
                else { return true; }
            }
            return false;
        }

        /// <summary>Coverage verdicts for every script the tool reports on.</summary>
        public Dictionary<DirectScript, DirectScriptCoverage> Coverage()
        {
            return DirectFontScripts.CoverageOfAll(HasCodepoint);
        }
    }

    /// <summary>
    /// Minimal, defensive sfnt (TrueType / OpenType) reader. Reads only the
    /// four tables the tool needs and never throws past its own boundary.
    /// </summary>
    public static class DirectFontFile
    {
        // A font is a table directory of at most a few dozen entries. Anything
        // claiming hundreds is either corrupt or hostile, and walking it would
        // be the parser's own denial of service.
        private const int MaxTables = 512;

        // Format 4 cmaps are bounded by the BMP, but a crafted file can claim
        // a segment count that is not. Enumerating is capped so a bad header
        // costs a wrong answer rather than a frozen Editor.
        private const int MaxEnumeratedCodes = 300000;

        private const uint TagTrueType = 0x00010000;
        private const uint TagOtto = 0x4F54544F;      // 'OTTO'
        private const uint TagTrue = 0x74727565;      // 'true'
        private const uint TagTtcf = 0x74746366;      // 'ttcf'

        // ==========================================
        // Read - the disk entry point.
        // ==========================================
        /// <summary>
        /// Reads a font file from disk. Never throws: an unreadable or
        /// non-font file comes back with <see cref="DirectFontFileInfo.IsValid"/>
        /// false and a reason in <see cref="DirectFontFileInfo.Error"/>.
        /// </summary>
        public static DirectFontFileInfo Read(string absolutePath)
        {
            var info = new DirectFontFileInfo { FilePath = absolutePath ?? string.Empty };

            try
            {
                if (string.IsNullOrEmpty(absolutePath) || !System.IO.File.Exists(absolutePath))
                {
                    info.Error = "File not found.";
                    return info;
                }

                var file = new System.IO.FileInfo(absolutePath);
                byte[] bytes = System.IO.File.ReadAllBytes(absolutePath);

                DirectFontFileInfo parsed = Parse(bytes, absolutePath);
                parsed.FileBytes = file.Length;
                return parsed;
            }
            catch (Exception e)
            {
                info.Error = e.Message;
                return info;
            }
        }

        // ==========================================
        // Parse - the testable entry point.
        //
        // Everything above is file system; everything
        // below is bytes in, facts out, which is what
        // the tests exercise with hand-built fonts.
        // ==========================================
        /// <summary>
        /// Parses font bytes already in memory - a download, an asset bundle,
        /// or a buffer a test built by hand.
        /// </summary>
        public static DirectFontFileInfo Parse(byte[] data, string label = "")
        {
            var info = new DirectFontFileInfo { FilePath = label ?? string.Empty };

            if (data == null || data.Length < 12)
            {
                info.Error = "Not a font: fewer than 12 bytes.";
                return info;
            }

            try
            {
                uint tag = ReadU32(data, 0);

                int sfntOffset = 0;
                if (tag == TagTtcf)
                {
                    // A collection. Read how many faces it holds and point at
                    // the first one; the catalog surfaces the count so a user
                    // can see why only one face is available.
                    if (data.Length < 16) { info.Error = "Truncated .ttc header."; return info; }
                    int faceCount = (int)ReadU32(data, 8);
                    if (faceCount < 1 || faceCount > 4096) { info.Error = "Implausible face count in .ttc."; return info; }
                    info.FaceCount = faceCount;

                    if (data.Length < 12 + 4) { info.Error = "Truncated .ttc offset table."; return info; }
                    sfntOffset = (int)ReadU32(data, 12);
                    if (sfntOffset < 0 || sfntOffset + 12 > data.Length) { info.Error = "Bad .ttc face offset."; return info; }
                    tag = ReadU32(data, sfntOffset);
                }

                if (tag != TagTrueType && tag != TagOtto && tag != TagTrue)
                {
                    info.Error = $"Not a TrueType or OpenType font (tag 0x{tag:X8}).";
                    return info;
                }
                info.IsCff = tag == TagOtto;

                Dictionary<string, (int Offset, int Length)> tables = ReadTableDirectory(data, sfntOffset);
                if (tables == null)
                {
                    info.Error = "Unreadable table directory.";
                    return info;
                }

                ReadNames(data, tables, info);
                ReadMaxp(data, tables, info);
                ReadHead(data, tables, info);
                ReadCmap(data, tables, info);

                // A font with no cmap can still be a valid font (a subsetted
                // CFF used only through glyph ids), but it is useless to
                // DirectTMP, which addresses glyphs by character. Say so
                // rather than showing an empty coverage row that reads like a
                // parse failure.
                if (info.CodepointCount == 0 && string.IsNullOrEmpty(info.Error))
                {
                    info.Error = "The font maps no characters (no usable cmap table).";
                }

                info.IsValid = info.CodepointCount > 0;
                return info;
            }
            catch (Exception e)
            {
                info.Error = e.Message;
                return info;
            }
        }

        // ==========================================
        // Table directory
        // ==========================================
        private static Dictionary<string, (int Offset, int Length)> ReadTableDirectory(byte[] data, int sfntOffset)
        {
            if (sfntOffset + 12 > data.Length) { return null; }

            int numTables = ReadU16(data, sfntOffset + 4);
            if (numTables <= 0 || numTables > MaxTables) { return null; }

            int recordsStart = sfntOffset + 12;
            if (recordsStart + (numTables * 16) > data.Length) { return null; }

            var tables = new Dictionary<string, (int, int)>(numTables, StringComparer.Ordinal);
            for (int i = 0; i < numTables; i++)
            {
                int record = recordsStart + (i * 16);
                string tag = Encoding.ASCII.GetString(data, record, 4);
                int offset = (int)ReadU32(data, record + 8);
                int length = (int)ReadU32(data, record + 12);

                // A table whose offset or length points outside the buffer is
                // simply skipped. One bad record must not cost us the tables
                // that are fine.
                if (offset < 0 || length < 0 || offset > data.Length) { continue; }
                if (offset + length > data.Length) { length = data.Length - offset; }

                tables[tag] = (offset, length);
            }
            return tables;
        }

        // ==========================================
        // name - family and style
        //
        // Preference order is Windows/Unicode first
        // (nearly every font has it and it is UTF-16BE),
        // then Macintosh/Roman. Typographic names
        // (16/17) win over legacy names (1/2) when both
        // are present, because for a family with more
        // than four styles the legacy pair says "Noto
        // Sans / Regular" for what is really
        // "Noto Sans ExtraCondensed / Light".
        // ==========================================
        private static void ReadNames(byte[] data, Dictionary<string, (int Offset, int Length)> tables, DirectFontFileInfo info)
        {
            if (!tables.TryGetValue("name", out (int Offset, int Length) table)) { return; }
            if (table.Length < 6) { return; }

            int count = ReadU16(data, table.Offset + 2);
            int stringsBase = table.Offset + ReadU16(data, table.Offset + 4);
            int recordsBase = table.Offset + 6;
            if (recordsBase + (count * 12) > data.Length) { return; }

            string family = null, typographicFamily = null;
            string style = null, typographicStyle = null;

            for (int i = 0; i < count; i++)
            {
                int record = recordsBase + (i * 12);
                int platformId = ReadU16(data, record);
                int encodingId = ReadU16(data, record + 2);
                int nameId = ReadU16(data, record + 6);
                int length = ReadU16(data, record + 8);
                int offset = ReadU16(data, record + 10);

                if (nameId != 1 && nameId != 2 && nameId != 16 && nameId != 17) { continue; }

                int start = stringsBase + offset;
                if (start < 0 || length < 0 || start + length > data.Length) { continue; }

                string value = DecodeName(data, start, length, platformId, encodingId);
                if (string.IsNullOrEmpty(value)) { continue; }

                switch (nameId)
                {
                    case 1: family = family ?? value; break;
                    case 2: style = style ?? value; break;
                    case 16: typographicFamily = typographicFamily ?? value; break;
                    case 17: typographicStyle = typographicStyle ?? value; break;
                }
            }

            info.FamilyName = (typographicFamily ?? family ?? string.Empty).Trim();
            info.StyleName = (typographicStyle ?? style ?? string.Empty).Trim();
        }

        private static string DecodeName(byte[] data, int start, int length, int platformId, int encodingId)
        {
            try
            {
                // Platform 3 (Windows) and platform 0 (Unicode) are UTF-16BE.
                // Platform 1 (Macintosh) encoding 0 is MacRoman, which for the
                // ASCII range - all a family name realistically uses - is
                // byte-for-byte ASCII.
                bool utf16 = platformId == 3 || platformId == 0;
                if (utf16 && encodingId == 10 && length % 2 != 0) { utf16 = false; }

                string decoded = utf16
                    ? Encoding.BigEndianUnicode.GetString(data, start, length - (length % 2))
                    : Encoding.ASCII.GetString(data, start, length);

                return decoded.Replace("\0", string.Empty).Trim();
            }
            catch
            {
                return null;
            }
        }

        // ==========================================
        // maxp - glyph count
        // ==========================================
        private static void ReadMaxp(byte[] data, Dictionary<string, (int Offset, int Length)> tables, DirectFontFileInfo info)
        {
            if (!tables.TryGetValue("maxp", out (int Offset, int Length) table)) { return; }
            if (table.Length < 6) { return; }
            info.GlyphCount = ReadU16(data, table.Offset + 4);
        }

        // ==========================================
        // head - units per em
        // ==========================================
        private static void ReadHead(byte[] data, Dictionary<string, (int Offset, int Length)> tables, DirectFontFileInfo info)
        {
            if (!tables.TryGetValue("head", out (int Offset, int Length) table)) { return; }
            if (table.Length < 20) { return; }
            info.UnitsPerEm = ReadU16(data, table.Offset + 18);
        }

        // ==========================================
        // cmap - which characters the font actually has
        // ==========================================
        private static void ReadCmap(byte[] data, Dictionary<string, (int Offset, int Length)> tables, DirectFontFileInfo info)
        {
            if (!tables.TryGetValue("cmap", out (int Offset, int Length) table)) { return; }
            if (table.Length < 4) { return; }

            int numSubtables = ReadU16(data, table.Offset + 2);
            if (numSubtables <= 0 || numSubtables > MaxTables) { return; }

            int recordsBase = table.Offset + 4;
            if (recordsBase + (numSubtables * 8) > data.Length) { return; }

            // Score each subtable and take the best one. A font commonly ships
            // three: a (3,1) BMP format 4, a (3,10) full-repertoire format 12,
            // and a legacy Mac one. Picking the format 12 when it is there is
            // what makes emoji and rare kanji show up as covered.
            int bestOffset = -1;
            int bestScore = -1;

            for (int i = 0; i < numSubtables; i++)
            {
                int record = recordsBase + (i * 8);
                int platformId = ReadU16(data, record);
                int encodingId = ReadU16(data, record + 2);
                int offset = table.Offset + (int)ReadU32(data, record + 4);
                if (offset < 0 || offset + 2 > data.Length) { continue; }

                int score = ScoreEncoding(platformId, encodingId);
                if (score > bestScore) { bestScore = score; bestOffset = offset; }
            }

            if (bestOffset < 0) { return; }

            List<DirectCodepointRange> ranges = ReadCmapSubtable(data, bestOffset);
            if (ranges == null || ranges.Count == 0) { return; }

            List<DirectCodepointRange> merged = Merge(ranges);
            info.CoveredRanges = merged;

            long total = 0;
            for (int i = 0; i < merged.Count; i++) { total += merged[i].Count; }
            info.CodepointCount = (int)Math.Min(total, int.MaxValue);
        }

        private static int ScoreEncoding(int platformId, int encodingId)
        {
            if (platformId == 3 && encodingId == 10) { return 100; }  // Windows UCS-4
            if (platformId == 0 && encodingId >= 4) { return 90; }    // Unicode full repertoire
            if (platformId == 3 && encodingId == 1) { return 80; }    // Windows BMP
            if (platformId == 0) { return 70; }                       // Unicode BMP
            if (platformId == 3 && encodingId == 0) { return 20; }    // Windows Symbol
            if (platformId == 1) { return 10; }                       // Macintosh
            return 0;
        }

        private static List<DirectCodepointRange> ReadCmapSubtable(byte[] data, int offset)
        {
            int format = ReadU16(data, offset);
            switch (format)
            {
                case 0: return ReadCmapFormat0(data, offset);
                case 4: return ReadCmapFormat4(data, offset);
                case 6: return ReadCmapFormat6(data, offset);
                case 12: return ReadCmapFormat12(data, offset);
                default: return null;
            }
        }

        // Format 0: a flat 256-entry byte array. Ancient, but a few symbol
        // fonts still ship one as their only table.
        private static List<DirectCodepointRange> ReadCmapFormat0(byte[] data, int offset)
        {
            var ranges = new List<DirectCodepointRange>();
            int arrayStart = offset + 6;
            if (arrayStart + 256 > data.Length) { return ranges; }

            for (int code = 0; code < 256; code++)
            {
                if (data[arrayStart + code] != 0) { Append(ranges, code); }
            }
            return ranges;
        }

        // Format 4: the BMP workhorse. Segments carry either a delta or an
        // offset into a glyph array, and either can resolve to glyph 0 -
        // which means "not in this font" and must NOT count as coverage. A
        // segment-level answer would mark every Persian letter present in any
        // font whose Arabic segment happens to span the block.
        private static List<DirectCodepointRange> ReadCmapFormat4(byte[] data, int offset)
        {
            var ranges = new List<DirectCodepointRange>();
            if (offset + 14 > data.Length) { return ranges; }

            int segCountX2 = ReadU16(data, offset + 6);
            int segCount = segCountX2 / 2;
            if (segCount <= 0 || segCount > 32768) { return ranges; }

            int endCodes = offset + 14;
            int startCodes = endCodes + segCountX2 + 2;   // +2 for reservedPad
            int idDeltas = startCodes + segCountX2;
            int idRangeOffsets = idDeltas + segCountX2;
            if (idRangeOffsets + segCountX2 > data.Length) { return ranges; }

            int enumerated = 0;
            for (int seg = 0; seg < segCount; seg++)
            {
                int end = ReadU16(data, endCodes + (seg * 2));
                int start = ReadU16(data, startCodes + (seg * 2));
                if (start > end) { continue; }
                if (start == 0xFFFF) { continue; }  // the mandatory terminator

                int delta = (short)ReadU16(data, idDeltas + (seg * 2));
                int rangeOffset = ReadU16(data, idRangeOffsets + (seg * 2));

                for (int code = start; code <= end && code != 0xFFFF; code++)
                {
                    if (++enumerated > MaxEnumeratedCodes) { return Merge(ranges); }

                    int glyph;
                    if (rangeOffset == 0)
                    {
                        glyph = (code + delta) & 0xFFFF;
                    }
                    else
                    {
                        // The spec's obscure "address of this idRangeOffset
                        // entry, plus the offset, plus twice the distance into
                        // the segment" indirection.
                        int glyphAddress = idRangeOffsets + (seg * 2) + rangeOffset + ((code - start) * 2);
                        if (glyphAddress + 2 > data.Length) { continue; }
                        glyph = ReadU16(data, glyphAddress);
                        if (glyph != 0) { glyph = (glyph + delta) & 0xFFFF; }
                    }

                    if (glyph != 0) { Append(ranges, code); }
                }
            }
            return ranges;
        }

        // Format 6: a dense run starting at firstCode.
        private static List<DirectCodepointRange> ReadCmapFormat6(byte[] data, int offset)
        {
            var ranges = new List<DirectCodepointRange>();
            if (offset + 10 > data.Length) { return ranges; }

            int firstCode = ReadU16(data, offset + 6);
            int entryCount = ReadU16(data, offset + 8);
            int glyphs = offset + 10;
            if (glyphs + (entryCount * 2) > data.Length) { return ranges; }

            for (int i = 0; i < entryCount; i++)
            {
                if (ReadU16(data, glyphs + (i * 2)) != 0) { Append(ranges, firstCode + i); }
            }
            return ranges;
        }

        // Format 12: groups of consecutive codepoints mapped to consecutive
        // glyph ids. Every code in a group is mapped by construction, so the
        // group IS the range - no enumeration, which is what keeps a 40 000
        // glyph CJK font cheap to inspect.
        private static List<DirectCodepointRange> ReadCmapFormat12(byte[] data, int offset)
        {
            var ranges = new List<DirectCodepointRange>();
            if (offset + 16 > data.Length) { return ranges; }

            long groupCount = ReadU32(data, offset + 12);
            if (groupCount <= 0 || groupCount > 200000) { return ranges; }

            int groups = offset + 16;
            if (groups + (groupCount * 12) > data.Length) { return ranges; }

            for (int i = 0; i < groupCount; i++)
            {
                int record = groups + (i * 12);
                int start = (int)ReadU32(data, record);
                int end = (int)ReadU32(data, record + 4);
                int startGlyph = (int)ReadU32(data, record + 8);

                if (start > end || start < 0 || end > 0x10FFFF) { continue; }
                if (startGlyph == 0) { continue; }  // the whole group is .notdef

                ranges.Add(new DirectCodepointRange(start, end));
            }
            return ranges;
        }

        // ==========================================
        // Range assembly
        // ==========================================

        // Appends one codepoint, extending the previous range when it is
        // adjacent. Enumerating format 4 one code at a time would otherwise
        // produce a range per character; a Latin font would carry six hundred
        // one-element ranges instead of a dozen real ones.
        private static void Append(List<DirectCodepointRange> ranges, int codepoint)
        {
            if (ranges.Count > 0)
            {
                DirectCodepointRange last = ranges[ranges.Count - 1];
                if (codepoint == last.End + 1)
                {
                    ranges[ranges.Count - 1] = new DirectCodepointRange(last.Start, codepoint);
                    return;
                }
                if (last.Contains(codepoint)) { return; }
            }
            ranges.Add(new DirectCodepointRange(codepoint, codepoint));
        }

        /// <summary>
        /// Sorts and merges overlapping or adjacent ranges. Exposed internally
        /// because it is the one piece of range arithmetic worth testing on its
        /// own - <see cref="DirectFontFileInfo.HasCodepoint"/> binary-searches
        /// the result and quietly returns the wrong answer if it is unsorted.
        /// </summary>
        internal static List<DirectCodepointRange> Merge(List<DirectCodepointRange> ranges)
        {
            var merged = new List<DirectCodepointRange>();
            if (ranges == null || ranges.Count == 0) { return merged; }

            var sorted = new List<DirectCodepointRange>(ranges);
            sorted.Sort((a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : a.End.CompareTo(b.End));

            int start = sorted[0].Start;
            int end = sorted[0].End;

            for (int i = 1; i < sorted.Count; i++)
            {
                DirectCodepointRange next = sorted[i];
                if (next.Start <= end + 1)
                {
                    if (next.End > end) { end = next.End; }
                }
                else
                {
                    merged.Add(new DirectCodepointRange(start, end));
                    start = next.Start;
                    end = next.End;
                }
            }
            merged.Add(new DirectCodepointRange(start, end));
            return merged;
        }

        // ==========================================
        // Big-endian primitives. Every sfnt integer is
        // big-endian regardless of the machine, so
        // BitConverter is the wrong tool on every
        // platform Unity ships to.
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
