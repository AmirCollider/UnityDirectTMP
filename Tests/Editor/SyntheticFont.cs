// ==========================================
// SyntheticFont
// A tiny TrueType font builder, for tests.
//
// DirectFontFile is the one piece of this package
// that reads bytes it did not write, and it is
// therefore the one piece where "it worked on the
// three fonts I had lying around" is not evidence of
// anything. Real fonts are also useless as fixtures:
// they are megabytes, they cannot be committed for
// licensing reasons, and they cannot be made subtly
// wrong on purpose.
//
// So the tests build their own. This constructs a
// valid sfnt with exactly the four tables the parser
// reads, from a description a test can state in one
// line - "a font called Test Sans containing A, B and
// あ" - and can then truncate, corrupt or contradict
// to check that the parser fails the way it promises
// to.
//
// It writes real big-endian sfnt structures rather
// than a mock, so what the tests exercise is the
// actual parse path, byte offsets included.
// ==========================================
using System.Collections.Generic;
using System.Text;

namespace UnityDirectTMP.Editor.Tests
{
    /// <summary>Builds minimal, valid .ttf byte arrays for the parser tests.</summary>
    internal static class SyntheticFont
    {
        /// <summary>An inclusive codepoint range the built font will map.</summary>
        public struct Range
        {
            public int Start;
            public int End;

            public Range(int start, int end) { Start = start; End = end; }
            public static Range Single(int codepoint) => new Range(codepoint, codepoint);
        }

        /// <summary>
        /// Builds a font with a BMP-only format 4 cmap. This is what the vast
        /// majority of real fonts ship, and the format whose per-character
        /// glyph resolution the parser has to get right.
        /// </summary>
        public static byte[] BuildFormat4(string family, string style, int glyphCount, params Range[] ranges)
        {
            return Build(family, style, glyphCount, BuildCmap4(ranges));
        }

        /// <summary>
        /// Builds a font with a format 12 cmap, which is how a font declares
        /// characters above the BMP - emoji, rare kanji, and anything else
        /// that needs more than sixteen bits.
        /// </summary>
        public static byte[] BuildFormat12(string family, string style, int glyphCount, params Range[] ranges)
        {
            return Build(family, style, glyphCount, BuildCmap12(ranges), platformId: 3, encodingId: 10);
        }

        // ==========================================
        // GSUB
        //
        // One joining substitution, as a font states it:
        // a feature tag, the letters it covers, and where
        // those letters go. DirectFontGsub reads exactly
        // this and nothing else, so a fixture that can
        // say it in both single-substitution formats -
        // and behind an extension lookup - covers the
        // whole of what a real font can throw at it.
        // ==========================================
        /// <summary>One 'init' / 'medi' / 'fina' / 'isol' rule for the built font.</summary>
        public struct Substitution
        {
            /// <summary>The four-character OpenType feature tag.</summary>
            public string Feature;

            /// <summary>The codepoints this rule applies to, ascending.</summary>
            public int[] Codepoints;

            /// <summary>
            /// The glyph each codepoint substitutes to, one per codepoint -
            /// which makes this a format 2 rule. Null for a format 1 rule,
            /// where every covered glyph moves by <see cref="Delta"/>.
            /// </summary>
            public int[] Glyphs;

            /// <summary>The shift applied to every covered glyph in a format 1 rule.</summary>
            public int Delta;

            /// <summary>Wrap the rule in a type 7 extension lookup, as a large font does.</summary>
            public bool Extension;

            public static Substitution Format1(string feature, int delta, params int[] codepoints)
                => new Substitution { Feature = feature, Delta = delta, Codepoints = codepoints };

            public static Substitution Format2(string feature, int[] codepoints, int[] glyphs)
                => new Substitution { Feature = feature, Codepoints = codepoints, Glyphs = glyphs };
        }

        /// <summary>
        /// Builds a font that carries OpenType joining rules - the thing every
        /// real Arabic font has and the presentation-form blocks were only ever
        /// a stand-in for.
        ///
        /// <paramref name="script"/> is the script tag the rules hang off.
        /// Passing something other than "arab" builds the font that looks like
        /// it can set Persian and cannot, which is a case worth having.
        /// </summary>
        public static byte[] BuildWithGsub(
            string family, int glyphCount, Range[] ranges, string script, params Substitution[] substitutions)
        {
            var tables = new List<(string Tag, byte[] Data)>
            {
                ("GSUB", BuildGsub(script ?? "arab", substitutions ?? new Substitution[0])),
                ("cmap", WrapCmap(BuildCmap4(ranges), 3, 1)),
                ("head", BuildHead(2048)),
                ("maxp", BuildMaxp(glyphCount)),
                ("name", BuildName(family, "Regular"))
            };
            return Assemble(0x00010000u, tables);
        }

        /// <summary>The glyph id <see cref="BuildCmap4"/> gives a codepoint: one past the code itself.</summary>
        public static int GlyphFor(int codepoint) => (codepoint + 1) & 0xFFFF;

        private static byte[] BuildGsub(string script, Substitution[] subs)
        {
            byte[][] lookups = new byte[subs.Length][];
            for (int i = 0; i < subs.Length; i++) { lookups[i] = BuildLookup(subs[i]); }

            byte[] scriptList = BuildScriptList(script, subs.Length);
            byte[] featureList = BuildFeatureList(subs);
            byte[] lookupList = BuildLookupList(lookups);

            const int header = 10;
            var gsub = new byte[header + scriptList.Length + featureList.Length + lookupList.Length];

            int scriptAt = header;
            int featureAt = scriptAt + scriptList.Length;
            int lookupAt = featureAt + featureList.Length;

            WriteU32(gsub, 0, 0x00010000);          // version 1.0
            WriteU16(gsub, 4, scriptAt);
            WriteU16(gsub, 6, featureAt);
            WriteU16(gsub, 8, lookupAt);

            System.Array.Copy(scriptList, 0, gsub, scriptAt, scriptList.Length);
            System.Array.Copy(featureList, 0, gsub, featureAt, featureList.Length);
            System.Array.Copy(lookupList, 0, gsub, lookupAt, lookupList.Length);

            return gsub;
        }

        // One script, one default language system, every feature listed in it.
        private static byte[] BuildScriptList(string script, int featureCount)
        {
            int langSysSize = 6 + (featureCount * 2);
            var list = new byte[2 + 6 + 4 + langSysSize];

            WriteU16(list, 0, 1);                                   // scriptCount
            System.Array.Copy(Encoding.ASCII.GetBytes(script), 0, list, 2, 4);
            WriteU16(list, 6, 8);                                   // offset to the Script table

            int table = 8;
            WriteU16(list, table, 4);                               // defaultLangSysOffset
            WriteU16(list, table + 2, 0);                           // langSysCount

            int langSys = table + 4;
            WriteU16(list, langSys, 0);                             // lookupOrderOffset
            WriteU16(list, langSys + 2, 0xFFFF);                    // requiredFeatureIndex
            WriteU16(list, langSys + 4, featureCount);
            for (int i = 0; i < featureCount; i++)
            {
                WriteU16(list, langSys + 6 + (i * 2), i);
            }

            return list;
        }

        // Feature i uses lookup i, which is all a fixture ever needs.
        private static byte[] BuildFeatureList(Substitution[] subs)
        {
            int recordsSize = subs.Length * 6;
            const int featureSize = 6;                              // params + count + one index

            var list = new byte[2 + recordsSize + (subs.Length * featureSize)];
            WriteU16(list, 0, subs.Length);

            for (int i = 0; i < subs.Length; i++)
            {
                int record = 2 + (i * 6);
                System.Array.Copy(Encoding.ASCII.GetBytes(subs[i].Feature), 0, list, record, 4);

                int feature = 2 + recordsSize + (i * featureSize);
                WriteU16(list, record + 4, feature);

                WriteU16(list, feature, 0);                         // featureParamsOffset
                WriteU16(list, feature + 2, 1);                     // lookupIndexCount
                WriteU16(list, feature + 4, i);                     // lookupListIndex
            }

            return list;
        }

        private static byte[] BuildLookupList(byte[][] lookups)
        {
            int headerSize = 2 + (lookups.Length * 2);
            int total = headerSize;
            foreach (byte[] lookup in lookups) { total += lookup.Length; }

            var list = new byte[total];
            WriteU16(list, 0, lookups.Length);

            int at = headerSize;
            for (int i = 0; i < lookups.Length; i++)
            {
                WriteU16(list, 2 + (i * 2), at);
                System.Array.Copy(lookups[i], 0, list, at, lookups[i].Length);
                at += lookups[i].Length;
            }

            return list;
        }

        private static byte[] BuildLookup(Substitution sub)
        {
            byte[] inner = sub.Glyphs != null ? BuildSingleFormat2(sub) : BuildSingleFormat1(sub);

            if (!sub.Extension)
            {
                var lookup = new byte[8 + inner.Length];
                WriteU16(lookup, 0, 1);        // lookupType: single substitution
                WriteU16(lookup, 2, 0);        // lookupFlag
                WriteU16(lookup, 4, 1);        // subTableCount
                WriteU16(lookup, 6, 8);        // subtableOffset
                System.Array.Copy(inner, 0, lookup, 8, inner.Length);
                return lookup;
            }

            // Type 7 is a 32-bit redirection to the real subtable, which is
            // how a font too large for 16-bit offsets says the same thing.
            var extended = new byte[16 + inner.Length];
            WriteU16(extended, 0, 7);          // lookupType: extension
            WriteU16(extended, 2, 0);
            WriteU16(extended, 4, 1);
            WriteU16(extended, 6, 8);

            WriteU16(extended, 8, 1);          // substFormat
            WriteU16(extended, 10, 1);         // extensionLookupType: single substitution
            WriteU32(extended, 12, 8);         // offset to the real subtable, from here

            System.Array.Copy(inner, 0, extended, 16, inner.Length);
            return extended;
        }

        private static byte[] BuildSingleFormat1(Substitution sub)
        {
            byte[] coverage = BuildCoverage(sub.Codepoints);

            var table = new byte[6 + coverage.Length];
            WriteU16(table, 0, 1);             // substFormat
            WriteU16(table, 2, 6);             // coverageOffset
            WriteU16(table, 4, sub.Delta & 0xFFFF);
            System.Array.Copy(coverage, 0, table, 6, coverage.Length);
            return table;
        }

        private static byte[] BuildSingleFormat2(Substitution sub)
        {
            byte[] coverage = BuildCoverage(sub.Codepoints);
            int count = sub.Glyphs.Length;
            int coverageAt = 6 + (count * 2);

            var table = new byte[coverageAt + coverage.Length];
            WriteU16(table, 0, 2);             // substFormat
            WriteU16(table, 2, coverageAt);    // coverageOffset
            WriteU16(table, 4, count);         // glyphCount

            for (int i = 0; i < count; i++)
            {
                WriteU16(table, 6 + (i * 2), sub.Glyphs[i]);
            }

            System.Array.Copy(coverage, 0, table, coverageAt, coverage.Length);
            return table;
        }

        // Format 1: an ascending list of covered glyphs. Its ORDER is the index
        // into a format 2 substitution array, which is the detail that decides
        // whether a word comes out spelled right.
        private static byte[] BuildCoverage(int[] codepoints)
        {
            var coverage = new byte[4 + (codepoints.Length * 2)];
            WriteU16(coverage, 0, 1);                      // coverageFormat
            WriteU16(coverage, 2, codepoints.Length);

            for (int i = 0; i < codepoints.Length; i++)
            {
                WriteU16(coverage, 4 + (i * 2), GlyphFor(codepoints[i]));
            }

            return coverage;
        }

        /// <summary>A font with no cmap table at all - valid sfnt, useless to this tool.</summary>
        public static byte[] BuildWithoutCmap(string family, int glyphCount)
        {
            var tables = new List<(string Tag, byte[] Data)>
            {
                ("head", BuildHead(2048)),
                ("maxp", BuildMaxp(glyphCount)),
                ("name", BuildName(family, "Regular"))
            };
            return Assemble(0x00010000u, tables);
        }

        // ==========================================
        // Assembly
        // ==========================================
        private static byte[] Build(string family, string style, int glyphCount, byte[] cmapSubtable,
            int platformId = 3, int encodingId = 1)
        {
            var tables = new List<(string Tag, byte[] Data)>
            {
                ("cmap", WrapCmap(cmapSubtable, platformId, encodingId)),
                ("head", BuildHead(2048)),
                ("maxp", BuildMaxp(glyphCount)),
                ("name", BuildName(family, style))
            };
            return Assemble(0x00010000u, tables);
        }

        private static byte[] Assemble(uint sfntVersion, List<(string Tag, byte[] Data)> tables)
        {
            // Table records must be sorted by tag; real parsers do not care,
            // but building it correctly means a fixture is never the reason a
            // test fails.
            tables.Sort((a, b) => string.CompareOrdinal(a.Tag, b.Tag));

            int headerSize = 12 + (tables.Count * 16);
            int total = headerSize;
            foreach ((string Tag, byte[] Data) table in tables)
            {
                total += Align4(table.Data.Length);
            }

            var bytes = new byte[total];
            WriteU32(bytes, 0, sfntVersion);
            WriteU16(bytes, 4, (ushort)tables.Count);
            WriteU16(bytes, 6, 0);   // searchRange - unread by this parser
            WriteU16(bytes, 8, 0);   // entrySelector
            WriteU16(bytes, 10, 0);  // rangeShift

            int record = 12;
            int dataOffset = headerSize;

            foreach ((string Tag, byte[] Data) table in tables)
            {
                byte[] tag = Encoding.ASCII.GetBytes(table.Tag);
                System.Array.Copy(tag, 0, bytes, record, 4);
                WriteU32(bytes, record + 4, 0);                        // checksum, unread
                WriteU32(bytes, record + 8, (uint)dataOffset);
                WriteU32(bytes, record + 12, (uint)table.Data.Length);

                System.Array.Copy(table.Data, 0, bytes, dataOffset, table.Data.Length);

                record += 16;
                dataOffset += Align4(table.Data.Length);
            }

            return bytes;
        }

        // ==========================================
        // Individual tables
        // ==========================================
        private static byte[] BuildHead(int unitsPerEm)
        {
            var head = new byte[54];
            WriteU32(head, 0, 0x00010000);   // version
            WriteU32(head, 4, 0x00010000);   // fontRevision
            WriteU32(head, 8, 0);            // checkSumAdjustment
            WriteU32(head, 12, 0x5F0F3CF5);  // magicNumber
            WriteU16(head, 16, 0);           // flags
            WriteU16(head, 18, (ushort)unitsPerEm);
            return head;
        }

        private static byte[] BuildMaxp(int glyphCount)
        {
            var maxp = new byte[32];
            WriteU32(maxp, 0, 0x00010000);
            WriteU16(maxp, 4, (ushort)glyphCount);
            return maxp;
        }

        private static byte[] BuildName(string family, string style)
        {
            // Two records: nameID 1 (family) and nameID 2 (subfamily), both
            // Windows / Unicode BMP, which is what the parser prefers.
            byte[] familyBytes = Encoding.BigEndianUnicode.GetBytes(family ?? string.Empty);
            byte[] styleBytes = Encoding.BigEndianUnicode.GetBytes(style ?? string.Empty);

            const int recordCount = 2;
            int recordsSize = recordCount * 12;
            int headerSize = 6 + recordsSize;

            var name = new byte[headerSize + familyBytes.Length + styleBytes.Length];
            WriteU16(name, 0, 0);                        // format
            WriteU16(name, 2, recordCount);
            WriteU16(name, 4, (ushort)headerSize);       // stringOffset

            WriteNameRecord(name, 6, nameId: 1, length: familyBytes.Length, offset: 0);
            WriteNameRecord(name, 18, nameId: 2, length: styleBytes.Length, offset: familyBytes.Length);

            System.Array.Copy(familyBytes, 0, name, headerSize, familyBytes.Length);
            System.Array.Copy(styleBytes, 0, name, headerSize + familyBytes.Length, styleBytes.Length);

            return name;
        }

        private static void WriteNameRecord(byte[] buffer, int at, int nameId, int length, int offset)
        {
            WriteU16(buffer, at, 3);          // platformID: Windows
            WriteU16(buffer, at + 2, 1);      // encodingID: Unicode BMP
            WriteU16(buffer, at + 4, 0x0409); // languageID: en-US
            WriteU16(buffer, at + 6, (ushort)nameId);
            WriteU16(buffer, at + 8, (ushort)length);
            WriteU16(buffer, at + 10, (ushort)offset);
        }

        private static byte[] WrapCmap(byte[] subtable, int platformId, int encodingId)
        {
            var cmap = new byte[4 + 8 + subtable.Length];
            WriteU16(cmap, 0, 0);   // version
            WriteU16(cmap, 2, 1);   // numTables

            WriteU16(cmap, 4, (ushort)platformId);
            WriteU16(cmap, 6, (ushort)encodingId);
            WriteU32(cmap, 8, 12);  // offset to the subtable, from the start of cmap

            System.Array.Copy(subtable, 0, cmap, 12, subtable.Length);
            return cmap;
        }

        // Format 4, with idRangeOffset zero throughout and idDelta chosen so
        // every mapped code resolves to a non-zero glyph id.
        private static byte[] BuildCmap4(Range[] ranges)
        {
            var segments = new List<Range>(ranges ?? new Range[0]);
            segments.Sort((a, b) => a.Start.CompareTo(b.Start));

            // The spec requires a final segment mapping 0xFFFF.
            segments.Add(new Range(0xFFFF, 0xFFFF));

            int segCount = segments.Count;
            int size = 14 + (segCount * 2) + 2 + (segCount * 2) + (segCount * 2) + (segCount * 2);

            var table = new byte[size];
            WriteU16(table, 0, 4);                       // format
            WriteU16(table, 2, (ushort)size);            // length
            WriteU16(table, 4, 0);                       // language
            WriteU16(table, 6, (ushort)(segCount * 2));  // segCountX2
            WriteU16(table, 8, 0);                       // searchRange
            WriteU16(table, 10, 0);                      // entrySelector
            WriteU16(table, 12, 0);                      // rangeShift

            int endCodes = 14;
            int startCodes = endCodes + (segCount * 2) + 2;
            int idDeltas = startCodes + (segCount * 2);
            int idRangeOffsets = idDeltas + (segCount * 2);

            for (int i = 0; i < segCount; i++)
            {
                WriteU16(table, endCodes + (i * 2), (ushort)segments[i].End);
                WriteU16(table, startCodes + (i * 2), (ushort)segments[i].Start);

                // glyph = (code + 1) & 0xFFFF. Non-zero for everything except
                // the 0xFFFF terminator, which is exactly the intent.
                WriteU16(table, idDeltas + (i * 2), 1);
                WriteU16(table, idRangeOffsets + (i * 2), 0);
            }

            return table;
        }

        private static byte[] BuildCmap12(Range[] ranges)
        {
            var groups = new List<Range>(ranges ?? new Range[0]);
            groups.Sort((a, b) => a.Start.CompareTo(b.Start));

            int size = 16 + (groups.Count * 12);
            var table = new byte[size];

            WriteU16(table, 0, 12);                      // format
            WriteU16(table, 2, 0);                       // reserved
            WriteU32(table, 4, (uint)size);              // length
            WriteU32(table, 8, 0);                       // language
            WriteU32(table, 12, (uint)groups.Count);     // nGroups

            int at = 16;
            uint glyph = 1;
            foreach (Range group in groups)
            {
                WriteU32(table, at, (uint)group.Start);
                WriteU32(table, at + 4, (uint)group.End);
                WriteU32(table, at + 8, glyph);
                glyph += (uint)(group.End - group.Start + 1);
                at += 12;
            }

            return table;
        }

        // ==========================================
        // Big-endian primitives
        // ==========================================
        private static void WriteU16(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 1] = (byte)(value & 0xFF);
        }

        private static void WriteU32(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(value & 0xFF);
        }

        private static int Align4(int value) => (value + 3) & ~3;
    }
}
