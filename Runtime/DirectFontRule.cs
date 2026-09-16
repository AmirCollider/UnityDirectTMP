// ==========================================
// DirectFontRule
// "Text in THESE codepoints uses THIS font."
//
// The named fields on DirectFont cover the scripts this
// package has opinions about - Persian/Arabic, the three
// CJK languages, Latin, emoji. That list can never be
// complete, and the failure mode of an incomplete list is
// the one this package exists to fix: somebody with
// Hebrew, Devanagari, Thai, Armenian, Georgian or
// cuneiform has a font that works, no field to put it in,
// and no way to add one without editing the package.
//
// So the list is open. A rule is three things a person
// can type: a name for their own benefit, the codepoint
// ranges the script occupies, and the font file. Nothing
// here knows what "cuneiform" means and nothing needs to.
//
// ==========================================
// WHY RANGES AND NOT AN ENUM
// ==========================================
// An enum entry would mean a package release per script,
// which is the same dead end one step further along.
// Unicode already assigns every script a block, those
// blocks are published, and "U+12000-U+123FF" is a thing
// somebody can copy off the Unicode chart for the script
// they are actually trying to render.
//
// ==========================================
// WHAT A BAD RULE DOES
// ==========================================
// Nothing. A rule with no font, no ranges, or ranges that
// do not parse matches nothing and is skipped - it never
// throws and never takes a label's font away. The
// Inspector says which rules are live and which are being
// ignored, because a rule that silently does nothing is
// the worst of the three outcomes.
//
// Pure C# apart from the Font reference.
// ==========================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>One user-defined script: some codepoint ranges, and the font for them.</summary>
    [Serializable]
    public sealed class DirectFontRule
    {
        [Tooltip("What this is, for your own benefit. Shown in the Inspector and in the report. "
               + "Nothing depends on it.")]
        public string name = "";

        [Tooltip("The codepoint ranges this script occupies, from the Unicode charts. "
               + "Hex, comma or space separated. A range is 'from-to'; a single codepoint is "
               + "just itself. '0x' and 'U+' are both accepted and both optional. "
               + "Example, cuneiform:  12000-123FF, 12400-1247F")]
        public string ranges = "";

        [Tooltip("The .ttf or .otf to use when the text is mostly this script. "
               + "It is also added as a fallback, so these letters render inside text "
               + "of any other script.")]
        public Font font;

        [Tooltip("Use this font for EVERY character it actually has a glyph for, read from "
               + "the font's own cmap table. The ranges above are ignored. "
               + "Turn this on when you do not know the ranges, or when the font is the "
               + "authority on what it covers - which it always is.")]
        public bool wholeFont;

        // The parsed form: pairs, flattened, [lo0, hi0, lo1, hi1, ...].
        // Rebuilt only when the string it came from changes, because this is
        // consulted once per codepoint of every label that changes text and
        // re-parsing there would make a text change cost a parse per character.
        [NonSerialized] private int[] _parsed;
        [NonSerialized] private string _parsedFrom;
        [NonSerialized] private bool _parsedOk;

        // The font's own coverage, read from its cmap the first time it is
        // asked for and kept until the font reference changes. Parsing is a
        // few hundred microseconds on a real font and this is consulted once
        // per codepoint of every label that changes text, so it is cached
        // rather than re-read - and keyed on the Font, so swapping the field
        // re-reads rather than answering for the old one.
        [NonSerialized] private int[] _coverage;
        [NonSerialized] private Font _coverageFor;
        [NonSerialized] private bool _coverageRead;

        /// <summary>
        /// What the font file says it covers, as flattened [lo, hi] pairs.
        /// Empty when there is no font, or when its cmap could not be read.
        /// </summary>
        public int[] Coverage()
        {
            if (_coverageRead && ReferenceEquals(_coverageFor, font) && _coverage != null)
            {
                return _coverage;
            }

            _coverageFor = font;
            _coverageRead = true;
            _coverage = font == null
                ? new int[0]
                : DirectFontCoverage.Ranges(DirectFontBytes.For(font));

            return _coverage;
        }

        /// <summary>Forget the cached coverage, so the next question re-reads the font.</summary>
        public void InvalidateCoverage() { _coverageRead = false; _coverage = null; _coverageFor = null; }

        /// <summary>True when this rule has a font and something to match against.</summary>
        public bool IsUsable
        {
            get
            {
                if (font == null) { return false; }
                if (wholeFont) { return Coverage().Length > 0; }

                Parse();
                return _parsedOk && _parsed.Length > 0;
            }
        }

        // ==========================================
        // Claims
        // Whether this rule takes a codepoint - the question
        // DirectFont actually asks.
        //
        // Matches() stays the pure range test, so it can be
        // tested without Unity and so "what did I type in the
        // Ranges box" remains answerable on its own. This is the
        // one that knows about whole-font mode.
        // ==========================================
        public bool Claims(int codepoint)
        {
            if (font == null) { return false; }
            if (!wholeFont) { return Matches(codepoint); }

            int[] cover = Coverage();
            for (int i = 0; i + 1 < cover.Length; i += 2)
            {
                if (codepoint >= cover[i] && codepoint <= cover[i + 1]) { return true; }
            }
            return false;
        }

        /// <summary>
        /// True when the ranges were written but none of them could be read.
        /// The Inspector uses this to say so rather than leaving a typo silent.
        /// </summary>
        public bool HasUnreadableRanges
        {
            get
            {
                if (wholeFont) { return false; }

                Parse();
                return !string.IsNullOrEmpty(ranges) && !string.IsNullOrEmpty(ranges.Trim())
                    && (!_parsedOk || _parsed.Length == 0);
            }
        }

        /// <summary>How many ranges this rule actually parsed to.</summary>
        public int RangeCount
        {
            get { Parse(); return _parsed == null ? 0 : _parsed.Length / 2; }
        }

        /// <summary>Whether this rule claims <paramref name="codepoint"/>.</summary>
        public bool Matches(int codepoint)
        {
            Parse();
            if (!_parsedOk || _parsed == null) { return false; }

            for (int i = 0; i < _parsed.Length; i += 2)
            {
                if (codepoint >= _parsed[i] && codepoint <= _parsed[i + 1]) { return true; }
            }
            return false;
        }

        /// <summary>Forget the parsed ranges, so the next question re-reads the string.</summary>
        public void Invalidate() { _parsedFrom = null; }

        private void Parse()
        {
            string source = ranges ?? "";
            if (ReferenceEquals(source, _parsedFrom) || source == _parsedFrom) { return; }

            _parsedFrom = source;
            _parsed = ParseRanges(source, out _parsedOk);
        }

        // ==========================================
        // ParseRanges
        //
        // Deliberately forgiving about the things people
        // genuinely type and strict about the rest:
        //
        //   12000-123FF      the form the Unicode charts print
        //   U+12000-U+123FF  the form they print it WITH
        //   0x12000-0x12FFF  the form a programmer types
        //   1F600            one codepoint on its own
        //   a, b   a b       comma or whitespace between them
        //   12FF–1200        reversed, and swapped back rather
        //                    than dropped: somebody typed the
        //                    ends the wrong way round, they
        //                    plainly meant the range between
        //                    them, and refusing is no help.
        //
        // An en dash is accepted with the hyphen because it is
        // what a copy-paste out of a PDF chart produces, and a
        // rule that fails on an invisible difference between two
        // dashes is a rule nobody can debug.
        //
        // `ok` is false only when a piece was present and could
        // not be read at all. Pieces that DO parse are kept
        // either way, so one typo in a list of six ranges costs
        // that range and not the other five.
        // ==========================================
        public static int[] ParseRanges(string text, out bool ok)
        {
            ok = true;
            List<int> outRanges = new List<int>();
            if (string.IsNullOrEmpty(text)) { return outRanges.ToArray(); }

            string[] pieces = text.Split(new[] { ',', ';', '\n', '\r', '\t', ' ' },
                                         StringSplitOptions.RemoveEmptyEntries);

            foreach (string raw in pieces)
            {
                string piece = raw.Trim();
                if (piece.Length == 0) { continue; }

                // The separator is looked for AFTER the first character so a
                // leading sign is never mistaken for one. No codepoint is
                // negative, but "-1200" should read as a typo rather than as
                // an empty low end.
                int split = -1;
                for (int i = 1; i < piece.Length; i++)
                {
                    if (piece[i] == '-' || piece[i] == '–' || piece[i] == '—')
                    {
                        split = i;
                        break;
                    }
                }

                int lo, hi;
                if (split < 0)
                {
                    if (!TryHex(piece, out lo)) { ok = false; continue; }
                    hi = lo;
                }
                else
                {
                    if (!TryHex(piece.Substring(0, split), out lo)
                        || !TryHex(piece.Substring(split + 1), out hi))
                    {
                        ok = false;
                        continue;
                    }
                }

                if (lo > hi) { int swap = lo; lo = hi; hi = swap; }

                // Outside Unicode entirely. Keeping it would be a range that
                // can never match, which reads as a rule that works.
                if (hi < 0 || lo > 0x10FFFF) { ok = false; continue; }
                if (lo < 0) { lo = 0; }
                if (hi > 0x10FFFF) { hi = 0x10FFFF; }

                outRanges.Add(lo);
                outRanges.Add(hi);
            }

            return outRanges.ToArray();
        }

        private static bool TryHex(string text, out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(text)) { return false; }

            string s = text.Trim();
            if (s.StartsWith("U+", StringComparison.OrdinalIgnoreCase)) { s = s.Substring(2); }
            else if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) { s = s.Substring(2); }
            if (s.Length == 0 || s.Length > 6) { return false; }

            int result = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                int digit;
                if (c >= '0' && c <= '9') { digit = c - '0'; }
                else if (c >= 'a' && c <= 'f') { digit = c - 'a' + 10; }
                else if (c >= 'A' && c <= 'F') { digit = c - 'A' + 10; }
                else { return false; }

                result = (result << 4) | digit;
            }

            value = result;
            return true;
        }
    }
}
