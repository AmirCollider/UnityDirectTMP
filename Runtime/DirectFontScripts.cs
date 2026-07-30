// ==========================================
// DirectFontScripts
// "Does this font actually have Persian in it?"
//
// The whole promise of Unity DirectTMP is that every
// character the font contains is available without
// anybody enumerating it in advance. The flip side of
// that promise is the question this file answers: a
// font that does not contain a script will not grow
// one, and the tool should say so at the moment
// somebody picks the font - not three screens later
// as a row of empty boxes.
//
// Nothing here touches Unity, the FontEngine or a
// file. It is a table of Unicode ranges, a handful of
// probe characters per script, and the pure functions
// that turn "these codepoints exist in the file" into
// "this font covers Japanese". That keeps it fully
// unit-testable, which matters because the coverage
// badges in the Font Catalog are the tool's most
// visible claim and the easiest thing to get subtly
// wrong.
//
// The ranges are deliberately coarse. This is a
// glance-level answer for a font picker, not a
// Unicode segmentation engine - a font that has
// U+0627 ARABIC LETTER ALEF and U+06AF ARABIC LETTER
// GAF can render Persian, and no amount of extra
// range detail changes that answer.
// ==========================================
using System.Collections.Generic;

namespace UnityDirectTMP
{
    /// <summary>
    /// The writing systems Unity DirectTMP reports coverage for. Ordered the
    /// way the Font Catalog lists them: the scripts most Unity projects need
    /// first, the specialist ones after.
    /// </summary>
    public enum DirectScript
    {
        Latin,
        Cyrillic,
        Greek,
        Arabic,
        Hebrew,
        Devanagari,
        Thai,
        Han,
        Kana,
        Hangul,
        Symbols,
        Emoji
    }

    /// <summary>
    /// How much of a script a font actually carries, as far as the probe
    /// characters can tell.
    /// </summary>
    public enum DirectScriptCoverage
    {
        /// <summary>Not one probe character is in the font.</summary>
        None,

        /// <summary>Some probes hit. Usable for some text, not for all of it.</summary>
        Partial,

        /// <summary>Every probe character is present.</summary>
        Full
    }

    /// <summary>
    /// Unicode ranges, probe characters and preview samples for every script
    /// Unity DirectTMP reports on. Pure data and pure functions - no Unity
    /// types, no file access.
    /// </summary>
    public static class DirectFontScripts
    {
        /// <summary>Every script, in catalog display order.</summary>
        public static readonly DirectScript[] All =
        {
            DirectScript.Latin,
            DirectScript.Cyrillic,
            DirectScript.Greek,
            DirectScript.Arabic,
            DirectScript.Hebrew,
            DirectScript.Devanagari,
            DirectScript.Thai,
            DirectScript.Han,
            DirectScript.Kana,
            DirectScript.Hangul,
            DirectScript.Symbols,
            DirectScript.Emoji
        };

        // ==========================================
        // Probe characters.
        //
        // Between four and six codepoints per script,
        // chosen so that a font which passes all of them
        // can genuinely set text in that script - not
        // merely show one letter from it.
        //
        // The Arabic set is the one worth explaining. A
        // font that stops at U+0627..U+064A covers Arabic
        // and fails Persian, because Persian needs the
        // four letters Arabic does not have: پ گ چ ژ. A
        // "supports Arabic script" badge that goes green
        // on a font which cannot write "پگاه" is exactly
        // the lie this whole file exists to prevent, so
        // GAF and PEH are in the probe set.
        // ==========================================
        private static readonly Dictionary<DirectScript, int[]> Probes = new Dictionary<DirectScript, int[]>
        {
            // A z é ñ ß  - basic Latin plus the accented forms
            // Latin-1 fonts sometimes skip.
            { DirectScript.Latin, new[] { 0x0041, 0x007A, 0x00E9, 0x00F1, 0x00DF } },

            // А я Ж ё  - upper, lower, a distinctive letterform,
            // and the yo that half-finished Cyrillic sets drop.
            { DirectScript.Cyrillic, new[] { 0x0410, 0x044F, 0x0416, 0x0451 } },

            // Α ω Σ π
            { DirectScript.Greek, new[] { 0x0391, 0x03C9, 0x03A3, 0x03C0 } },

            // ا ب ی پ گ  - Arabic alef/beh/farsi-yeh, then the
            // two Persian letters that separate "Arabic" from
            // "Arabic script".
            { DirectScript.Arabic, new[] { 0x0627, 0x0628, 0x06CC, 0x067E, 0x06AF } },

            // א ב ש ם
            { DirectScript.Hebrew, new[] { 0x05D0, 0x05D1, 0x05E9, 0x05DD } },

            // क ख ा ी
            { DirectScript.Devanagari, new[] { 0x0915, 0x0916, 0x093E, 0x0940 } },

            // ก ข ไ ่
            { DirectScript.Thai, new[] { 0x0E01, 0x0E02, 0x0E44, 0x0E48 } },

            // 一 中 語 漢 龍  - one stroke through to a
            // fourteen-stroke character, because a font can
            // carry the first thousand kanji and stop.
            { DirectScript.Han, new[] { 0x4E00, 0x4E2D, 0x8A9E, 0x6F22, 0x9F8D } },

            // あ ん ア ン ー  - both kana and the prolonged
            // sound mark, which is not optional in Japanese.
            { DirectScript.Kana, new[] { 0x3042, 0x3093, 0x30A2, 0x30F3, 0x30FC } },

            // 한 글 가 힣  - and the last syllable in the block,
            // because a font with only the common syllables
            // will fail on a real name.
            { DirectScript.Hangul, new[] { 0xD55C, 0xAE00, 0xAC00, 0xD7A3 } },

            // → • … ★ ©  - the punctuation and dingbats a UI
            // actually reaches for.
            { DirectScript.Symbols, new[] { 0x2192, 0x2022, 0x2026, 0x2605, 0x00A9 } },

            // 😀 🎮 ✨ 🍰
            { DirectScript.Emoji, new[] { 0x1F600, 0x1F3AE, 0x2728, 0x1F370 } }
        };

        // ==========================================
        // Primary Unicode ranges, used to classify a
        // codepoint that came from somewhere else (a
        // preload string, a label's text) into a script.
        //
        // Order matters: Classify walks these top to
        // bottom and returns the first hit, so the
        // narrow blocks are listed before the wide ones.
        // ==========================================
        private static readonly (int Start, int End, DirectScript Script)[] Ranges =
        {
            (0x0041, 0x005A, DirectScript.Latin),
            (0x0061, 0x007A, DirectScript.Latin),
            (0x00C0, 0x024F, DirectScript.Latin),
            (0x0370, 0x03FF, DirectScript.Greek),
            (0x1F00, 0x1FFF, DirectScript.Greek),
            (0x0400, 0x052F, DirectScript.Cyrillic),
            (0x0590, 0x05FF, DirectScript.Hebrew),
            (0xFB1D, 0xFB4F, DirectScript.Hebrew),
            (0x0600, 0x06FF, DirectScript.Arabic),
            (0x0750, 0x077F, DirectScript.Arabic),
            (0x08A0, 0x08FF, DirectScript.Arabic),
            (0xFB50, 0xFDFF, DirectScript.Arabic),
            (0xFE70, 0xFEFF, DirectScript.Arabic),
            (0x0900, 0x097F, DirectScript.Devanagari),
            (0x0E00, 0x0E7F, DirectScript.Thai),
            (0x3040, 0x309F, DirectScript.Kana),
            (0x30A0, 0x30FF, DirectScript.Kana),
            (0x31F0, 0x31FF, DirectScript.Kana),
            (0xFF66, 0xFF9D, DirectScript.Kana),
            (0x1100, 0x11FF, DirectScript.Hangul),
            (0x3130, 0x318F, DirectScript.Hangul),
            (0xAC00, 0xD7AF, DirectScript.Hangul),
            (0x3400, 0x4DBF, DirectScript.Han),
            (0x4E00, 0x9FFF, DirectScript.Han),
            (0xF900, 0xFAFF, DirectScript.Han),
            (0x20000, 0x2FA1F, DirectScript.Han),
            (0x1F300, 0x1FAFF, DirectScript.Emoji),
            (0x1F000, 0x1F2FF, DirectScript.Emoji),
            (0x2600, 0x27BF, DirectScript.Emoji),
            (0x2000, 0x206F, DirectScript.Symbols),
            (0x2190, 0x21FF, DirectScript.Symbols),
            (0x2200, 0x22FF, DirectScript.Symbols),
            (0x2500, 0x25FF, DirectScript.Symbols)
        };

        // ==========================================
        // Preview samples, used by the Font Catalog and
        // the Editor Font window to show the font
        // rendering something a reader can judge rather
        // than "AaBbCc" in every row.
        //
        // Each one is a real phrase in a language that
        // uses the script, kept short enough to fit a
        // catalog row at preview size.
        // ==========================================
        private static readonly Dictionary<DirectScript, string> Samples = new Dictionary<DirectScript, string>
        {
            { DirectScript.Latin, "The quick brown fox 0123" },
            { DirectScript.Cyrillic, "Съешь ещё" },
            { DirectScript.Greek, "Γνῶθι σεαυτόν" },
            { DirectScript.Arabic, "سلام دنیا گل" },
            { DirectScript.Hebrew, "שלום עולם" },
            { DirectScript.Devanagari, "नमस्ते दुनिया" },
            { DirectScript.Thai, "สวัสดีชาวโลก" },
            { DirectScript.Han, "你好世界 漢字" },
            { DirectScript.Kana, "こんにちは世界" },
            { DirectScript.Hangul, "안녕하세요" },
            { DirectScript.Symbols, "→ • … ★ © ±" },
            { DirectScript.Emoji, "\U0001F600 \U0001F3AE ✨ \U0001F370" }
        };

        // ==========================================
        // Display names, in the three languages the
        // tool speaks. Kept here rather than in the
        // editor text pack because the script list and
        // its labels are one thing, and splitting them
        // is how a script gets added with no label.
        // ==========================================
        private static readonly Dictionary<DirectScript, string[]> Names = new Dictionary<DirectScript, string[]>
        {
            //                                        en                ja                    fa
            { DirectScript.Latin, new[] { "Latin", "ラテン", "لاتین" } },
            { DirectScript.Cyrillic, new[] { "Cyrillic", "キリル", "سیریلیک" } },
            { DirectScript.Greek, new[] { "Greek", "ギリシャ", "یونانی" } },
            { DirectScript.Arabic, new[] { "Arabic script", "アラビア文字", "خط عربی" } },
            { DirectScript.Hebrew, new[] { "Hebrew", "ヘブライ", "عبری" } },
            { DirectScript.Devanagari, new[] { "Devanagari", "デーヴァナガリー", "دواناگری" } },
            { DirectScript.Thai, new[] { "Thai", "タイ", "تایی" } },
            { DirectScript.Han, new[] { "Han (CJK)", "漢字", "هان (CJK)" } },
            { DirectScript.Kana, new[] { "Kana", "仮名", "کانا" } },
            { DirectScript.Hangul, new[] { "Hangul", "ハングル", "هانگول" } },
            { DirectScript.Symbols, new[] { "Symbols", "記号", "نمادها" } },
            { DirectScript.Emoji, new[] { "Emoji", "絵文字", "ایموجی" } }
        };

        /// <summary>The probe codepoints used to decide coverage of a script.</summary>
        public static int[] ProbesFor(DirectScript script)
        {
            return Probes.TryGetValue(script, out int[] probes) ? probes : new int[0];
        }

        /// <summary>A short, real phrase in this script, for preview rows.</summary>
        public static string SampleFor(DirectScript script)
        {
            return Samples.TryGetValue(script, out string sample) ? sample : string.Empty;
        }

        /// <summary>
        /// The script's name in one of the tool's three languages
        /// ("en", "ja", "fa"). An unknown language code falls back to English
        /// rather than returning empty, because a blank badge is worse than an
        /// untranslated one.
        /// </summary>
        public static string NameFor(DirectScript script, string language)
        {
            if (!Names.TryGetValue(script, out string[] names)) { return script.ToString(); }
            int index = language == "ja" ? 1 : language == "fa" ? 2 : 0;
            return names[index];
        }

        /// <summary>
        /// Which script a single codepoint belongs to, or null when it falls
        /// outside every range the tool reports on (control characters,
        /// whitespace, and the long tail of scripts that have no badge).
        /// </summary>
        public static DirectScript? Classify(int codepoint)
        {
            for (int i = 0; i < Ranges.Length; i++)
            {
                if (codepoint >= Ranges[i].Start && codepoint <= Ranges[i].End)
                {
                    return Ranges[i].Script;
                }
            }
            return null;
        }

        /// <summary>
        /// Every script present in a piece of text, in the order the catalog
        /// lists them. Surrogate pairs are decoded, so an emoji counts once as
        /// one codepoint rather than twice as two halves of nothing.
        /// </summary>
        public static List<DirectScript> ScriptsIn(string text)
        {
            var found = new List<DirectScript>();
            if (string.IsNullOrEmpty(text)) { return found; }

            var seen = new HashSet<DirectScript>();
            foreach (int codepoint in Codepoints(text))
            {
                DirectScript? script = Classify(codepoint);
                if (script.HasValue && seen.Add(script.Value)) { found.Add(script.Value); }
            }

            // Return them in catalog order rather than first-seen order, so
            // two strings with the same scripts always produce the same list.
            var ordered = new List<DirectScript>();
            foreach (DirectScript script in All)
            {
                if (seen.Contains(script)) { ordered.Add(script); }
            }
            return ordered;
        }

        /// <summary>
        /// Decodes a string into codepoints, combining surrogate pairs. Used
        /// everywhere the tool counts characters, because counting
        /// <c>string.Length</c> reports every emoji as two.
        /// </summary>
        public static IEnumerable<int> Codepoints(string text)
        {
            if (string.IsNullOrEmpty(text)) { yield break; }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    yield return char.ConvertToUtf32(c, text[i + 1]);
                    i++;
                }
                else
                {
                    yield return c;
                }
            }
        }

        /// <summary>
        /// Turns "how many probe characters did the font have?" into a verdict.
        /// All of them is <see cref="DirectScriptCoverage.Full"/>, none of them
        /// is <see cref="DirectScriptCoverage.None"/>, and anything between is
        /// Partial - which is the honest answer for a font that has Arabic but
        /// not the Persian letters, and the reason Partial exists at all.
        /// </summary>
        public static DirectScriptCoverage Verdict(int probesPresent, int probesTotal)
        {
            if (probesTotal <= 0 || probesPresent <= 0) { return DirectScriptCoverage.None; }
            if (probesPresent >= probesTotal) { return DirectScriptCoverage.Full; }
            return DirectScriptCoverage.Partial;
        }

        /// <summary>
        /// Coverage of one script, given a predicate that answers "is this
        /// codepoint in the font?". Taking a predicate rather than a font keeps
        /// this half of the logic testable without a font file, and lets the
        /// same code serve a parsed cmap, a live <c>Font</c> and a built
        /// TMP_FontAsset.
        /// </summary>
        public static DirectScriptCoverage CoverageOf(DirectScript script, System.Func<int, bool> hasCodepoint)
        {
            if (hasCodepoint == null) { return DirectScriptCoverage.None; }

            int[] probes = ProbesFor(script);
            int present = 0;
            for (int i = 0; i < probes.Length; i++)
            {
                if (hasCodepoint(probes[i])) { present++; }
            }
            return Verdict(present, probes.Length);
        }

        /// <summary>
        /// Coverage of every script, as a map. The Font Catalog draws one badge
        /// per entry.
        /// </summary>
        public static Dictionary<DirectScript, DirectScriptCoverage> CoverageOfAll(System.Func<int, bool> hasCodepoint)
        {
            var result = new Dictionary<DirectScript, DirectScriptCoverage>();
            foreach (DirectScript script in All)
            {
                result[script] = CoverageOf(script, hasCodepoint);
            }
            return result;
        }

        /// <summary>
        /// The scripts a font fully covers, in catalog order. This is the list
        /// the catalog row prints and the search box matches against.
        /// </summary>
        public static List<DirectScript> FullyCovered(IDictionary<DirectScript, DirectScriptCoverage> coverage)
        {
            var covered = new List<DirectScript>();
            if (coverage == null) { return covered; }

            foreach (DirectScript script in All)
            {
                if (coverage.TryGetValue(script, out DirectScriptCoverage verdict) && verdict == DirectScriptCoverage.Full)
                {
                    covered.Add(script);
                }
            }
            return covered;
        }
    }
}
