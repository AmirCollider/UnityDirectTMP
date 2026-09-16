// ==========================================
// DirectFontCoverage
// Which codepoints a font file actually has glyphs for,
// read from its own `cmap` table.
//
// ==========================================
// WHY THIS EXISTS
// ==========================================
// A DirectFontRule normally says "these Unicode ranges
// use this font", typed from a Unicode chart. That is
// exact and it is also a second copy of a fact the font
// already contains - and when the two disagree, the font
// wins and the reader gets empty boxes.
//
// They disagree more often than they should, because a
// large share of the fonts people have for unusual
// scripts are LEGACY HACK FONTS: the glyphs are drawn
// onto ASCII slots, so typing `a` gives a cuneiform sign
// and the real Unicode codepoint for that sign is not in
// the file at all. Every such font looks correct in a
// font preview, works when you type Latin letters, and
// produces nothing but tofu for properly encoded text.
//
// Nothing in a range list can detect that. Reading the
// cmap can: it is the font stating, in its own bytes,
// exactly which codepoints it will draw.
//
// So a rule can be set to "the whole font" and skip the
// ranges entirely, and the Inspector can read the same
// table to SHOW what a font covers - which is what turns
// "why is my font not working" into a number somebody can
// look at.
//
// ==========================================
// WHAT IT READS
// ==========================================
// The four cmap subtable formats that occur in practice:
//
//   4    the BMP format essentially every font has
//   12   the format a font needs for anything above
//        U+FFFF, which is every astral script - emoji,
//        cuneiform, the CJK extension planes
//   6    a small contiguous range, in older fonts
//   0    the 256-entry byte table, in very old ones
//
// Subtables are merged rather than ranked, because a font
// with both a format 4 and a format 12 has them agree on
// the BMP and only the format 12 covers the rest.
//
// ==========================================
// IT MUST NEVER THROW
// ==========================================
// This parses a file somebody dropped into a field. A
// truncated download, a .otf with CFF outlines, a web
// font, something that is not a font at all - all of them
// reach here, and none of them is worth an exception in
// the middle of a scene loading. Every read is bounds
// checked and anything unreadable comes back as "covers
// nothing", which the caller already handles: a rule that
// matches nothing is a rule the Inspector reports as
// doing nothing.
//
// Pure C#. No Unity types, so it can be tested outside
// the Editor - which matters, because the interesting
// input is a real font file.
// ==========================================
using System;
using System.Collections.Generic;

namespace UnityDirectTMP
{
    /// <summary>Reads which codepoints a TrueType/OpenType file has glyphs for.</summary>
    public static class DirectFontCoverage
    {
        // A font with more mapped ranges than this is either exotic or not a
        // font. The cap is generous - a full CJK face coalesces to a few
        // hundred - and exists so a malformed table cannot allocate without
        // bound.
        private const int MaxRanges = 4096;

        /// <summary>
        /// Every codepoint the font maps, as coalesced [lo, hi] pairs, flattened.
        /// Empty when the file cannot be read - never null, never an exception.
        /// </summary>
        public static int[] Ranges(byte[] font)
        {
            try
            {
                return Read(font);
            }
            catch
            {
                // See the note at the top: an unreadable font is a font that
                // covers nothing, not a crash in somebody's scene.
                return new int[0];
            }
        }

        /// <summary>How many codepoints a flattened range list covers.</summary>
        public static int Count(int[] ranges)
        {
            if (ranges == null) { return 0; }

            int total = 0;
            for (int i = 0; i + 1 < ranges.Length; i += 2) { total += ranges[i + 1] - ranges[i] + 1; }
            return total;
        }

        /// <summary>Formats a flattened range list the way the Ranges field expects it.</summary>
        public static string Describe(int[] ranges)
        {
            if (ranges == null || ranges.Length == 0) { return ""; }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i + 1 < ranges.Length; i += 2)
            {
                if (sb.Length > 0) { sb.Append(", "); }
                sb.Append(ranges[i].ToString("X4"));
                if (ranges[i + 1] != ranges[i]) { sb.Append('-').Append(ranges[i + 1].ToString("X4")); }
            }
            return sb.ToString();
        }

        private static int[] Read(byte[] d)
        {
            if (d == null || d.Length < 12) { return new int[0]; }

            // A TrueType collection (.ttc) starts with 'ttcf' and holds several
            // fonts. Reading the first one is the honest approximation: nothing
            // in this package can choose between them anyway.
            int start = 0;
            if (Tag(d, 0) == "ttcf")
            {
                if (d.Length < 16) { return new int[0]; }
                start = (int)U32(d, 12);
                if (start < 0 || start + 12 > d.Length) { return new int[0]; }
            }

            int tableCount = U16(d, start + 4);
            int cmap = -1;

            for (int i = 0; i < tableCount; i++)
            {
                int rec = start + 12 + i * 16;
                if (rec + 16 > d.Length) { return new int[0]; }
                if (Tag(d, rec) == "cmap") { cmap = (int)U32(d, rec + 8); break; }
            }

            if (cmap < 0 || cmap + 4 > d.Length) { return new int[0]; }

            SortedSet<int> starts = null;
            List<int> flat = new List<int>();
            List<KeyValuePair<int, int>> pairs = new List<KeyValuePair<int, int>>();

            int subtables = U16(d, cmap + 2);
            for (int i = 0; i < subtables; i++)
            {
                int rec = cmap + 4 + i * 8;
                if (rec + 8 > d.Length) { break; }

                int offset = (int)U32(d, rec + 4);
                int sub = cmap + offset;
                if (sub < 0 || sub + 4 > d.Length) { continue; }

                ReadSubtable(d, sub, pairs);
                if (pairs.Count > MaxRanges * 4) { break; }
            }

            if (pairs.Count == 0) { return new int[0]; }

            // Coalesce. The subtables overlap and each contributes single
            // codepoints, so the raw list is long and full of neighbours; what
            // a person reads, and what Matches() walks, wants it merged.
            pairs.Sort((a, b) => a.Key != b.Key ? a.Key.CompareTo(b.Key) : a.Value.CompareTo(b.Value));

            int lo = pairs[0].Key;
            int hi = pairs[0].Value;

            for (int i = 1; i < pairs.Count; i++)
            {
                int nextLo = pairs[i].Key;
                int nextHi = pairs[i].Value;

                if (nextLo <= hi + 1)
                {
                    if (nextHi > hi) { hi = nextHi; }
                }
                else
                {
                    flat.Add(lo); flat.Add(hi);
                    if (flat.Count >= MaxRanges * 2) { return flat.ToArray(); }
                    lo = nextLo; hi = nextHi;
                }
            }

            flat.Add(lo); flat.Add(hi);
            if (starts != null) { starts.Clear(); }
            return flat.ToArray();
        }

        private static void ReadSubtable(byte[] d, int sub, List<KeyValuePair<int, int>> pairs)
        {
            int format = U16(d, sub);

            if (format == 4)
            {
                int segX2 = U16(d, sub + 6);
                int segments = segX2 / 2;
                if (segments <= 0) { return; }

                int endsAt = sub + 14;
                int startsAt = endsAt + segX2 + 2;
                if (startsAt + segX2 > d.Length) { return; }

                for (int s = 0; s < segments; s++)
                {
                    int end = U16(d, endsAt + s * 2);
                    int begin = U16(d, startsAt + s * 2);

                    // The last segment of a format 4 table is the required
                    // 0xFFFF..0xFFFF terminator, which maps nothing.
                    if (begin == 0xFFFF || begin > end) { continue; }

                    // The glyph id is deliberately NOT resolved. A codepoint
                    // present in the segment list but mapping to glyph 0 is
                    // rare, and resolving it means walking idDelta and
                    // idRangeOffset for every character - a lot of code, and
                    // arithmetic that is wrong in a way that is invisible, to
                    // refine an answer nobody is going to act on differently.
                    pairs.Add(new KeyValuePair<int, int>(begin, end));
                }
            }
            else if (format == 12)
            {
                if (sub + 16 > d.Length) { return; }
                long groups = U32(d, sub + 12);
                if (groups <= 0 || groups > 0x100000) { return; }

                for (int g = 0; g < groups; g++)
                {
                    int rec = sub + 16 + g * 12;
                    if (rec + 12 > d.Length) { return; }

                    int begin = (int)U32(d, rec);
                    int end = (int)U32(d, rec + 4);
                    if (begin < 0 || end < begin || end > 0x10FFFF) { continue; }

                    pairs.Add(new KeyValuePair<int, int>(begin, end));
                }
            }
            else if (format == 6)
            {
                if (sub + 10 > d.Length) { return; }
                int first = U16(d, sub + 6);
                int count = U16(d, sub + 8);
                if (count <= 0) { return; }

                pairs.Add(new KeyValuePair<int, int>(first, first + count - 1));
            }
            else if (format == 0)
            {
                if (sub + 6 + 256 > d.Length) { return; }
                for (int c = 0; c < 256; c++)
                {
                    if (d[sub + 6 + c] != 0) { pairs.Add(new KeyValuePair<int, int>(c, c)); }
                }
            }
        }

        private static string Tag(byte[] d, int at)
        {
            if (at + 4 > d.Length) { return ""; }
            return "" + (char)d[at] + (char)d[at + 1] + (char)d[at + 2] + (char)d[at + 3];
        }

        private static int U16(byte[] d, int at)
        {
            if (at + 2 > d.Length) { return 0; }
            return (d[at] << 8) | d[at + 1];
        }

        private static uint U32(byte[] d, int at)
        {
            if (at + 4 > d.Length) { return 0; }
            return (uint)((d[at] << 24) | (d[at + 1] << 16) | (d[at + 2] << 8) | d[at + 3]);
        }
    }
}
