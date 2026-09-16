// ==========================================
// DirectScript
// Which writing system a piece of text is in.
//
// Used to pick a font per language: a label showing
// Persian gets the Persian font, the same label showing
// Japanese later gets the Japanese one, and neither
// needs a second component or a second label.
//
// ==========================================
// TWO LEVELS, AND WHY THERE ARE TWO
// ==========================================
// A font question has a coarse answer and a fine one,
// and they are not the same question:
//
//   GROUP    Arabic, Cjk, Latin, Cyrillic. One font
//            usually covers a whole group - a CJK face
//            almost always sets Japanese, Chinese and
//            Korean, and an Arabic face sets Persian,
//            Arabic and Urdu. This is what DominantOf()
//            returns and what DirectFont.Script reports,
//            unchanged since the first release.
//
//   SPECIFIC Japanese, Chinese, Korean, Emoji. Sometimes
//            one font is NOT enough: a Japanese face and
//            a Simplified Chinese face draw the same
//            ideograph differently and a reader notices,
//            and emoji need a colour font that no text
//            face carries at all. SpecificOf() answers
//            that, and DirectFont uses it to prefer a
//            specific font when one is filled in.
//
// The specific values are the new ones and the group
// values are untouched, so code comparing against
// DirectScript.Cjk or .Arabic keeps meaning what it
// meant. Group() maps one to the other.
//
// ==========================================
// CODEPOINTS, NOT CHARS
// ==========================================
// This file used to classify a `char`. A char is a UTF-16
// code UNIT, and every emoji worth the name lives above
// U+FFFF - so 😀 is not a character here, it is two lone
// surrogates. Neither is a letter, so both scored as
// "no script", and a label of pure emoji reported having
// no writing system at all and voted for no font.
//
// That is also why an emoji font field could not have
// worked before this: there was nothing for it to win.
// Cuneiform (U+12000) and every other astral script had
// the same problem.
//
// So the primitive takes an int codepoint, and every walk
// over a string advances by two when it meets a surrogate
// pair. Of(char) stays, because it is public and it is
// still correct for everything in the Basic Multilingual
// Plane.
//
// Pure C#. No Unity types.
// ==========================================
namespace UnityDirectTMP
{
    /// <summary>A family of writing systems that a single font usually covers.</summary>
    public enum DirectScript
    {
        /// <summary>Nothing with a strong script - digits, punctuation, spaces.</summary>
        None = 0,

        /// <summary>English and the rest of the Latin alphabet.</summary>
        Latin = 1,

        /// <summary>Persian, Arabic, Urdu, Kurdish, Pashto.</summary>
        Arabic = 2,

        /// <summary>Japanese, Chinese, Korean - the group one CJK font usually covers.</summary>
        Cjk = 3,

        /// <summary>Russian and the rest of Cyrillic.</summary>
        Cyrillic = 4,

        /// <summary>Anything with letters that none of the above covers.</summary>
        Other = 5,

        // ------------------------------------------------------
        // Specific scripts. Added in 2.1.14, with values of their
        // own so nothing above shifts. DominantOf() never returns
        // one of these - see the note at the top.
        // ------------------------------------------------------

        /// <summary>Emoji and pictographs. Needs a colour emoji font; no text face has these.</summary>
        Emoji = 6,

        /// <summary>Japanese kana specifically. Group is <see cref="Cjk"/>.</summary>
        Japanese = 7,

        /// <summary>Han ideographs - Chinese, and the kanji in Japanese. Group is <see cref="Cjk"/>.</summary>
        Chinese = 8,

        /// <summary>Korean Hangul specifically. Group is <see cref="Cjk"/>.</summary>
        Korean = 9
    }

    /// <summary>Works out which writing system some text is in.</summary>
    public static class DirectScripts
    {
        /// <summary>
        /// The group a specific script belongs to - the coarse answer, which is
        /// what a single font usually covers.
        /// </summary>
        public static DirectScript Group(DirectScript script)
        {
            switch (script)
            {
                case DirectScript.Japanese:
                case DirectScript.Chinese:
                case DirectScript.Korean:
                    return DirectScript.Cjk;

                // Emoji are their own group. They are not "other letters":
                // no text font covers them, and a label of emoji wants the
                // emoji font rather than a fallback nobody chose.
                default:
                    return script;
            }
        }

        /// <summary>
        /// The script of one BMP character.
        ///
        /// Kept because it is public and because it is still right for
        /// everything below U+10000. Anything above that arrives here as a
        /// lone surrogate, which has no script on its own - use
        /// <see cref="SpecificOf(int)"/> with a real codepoint instead.
        /// </summary>
        public static DirectScript Of(char c) => Group(SpecificOf(c));

        /// <summary>The specific script of one Unicode codepoint.</summary>
        public static DirectScript SpecificOf(int u)
        {
            if (u < 0x0080)
            {
                return (u >= 'A' && u <= 'Z') || (u >= 'a' && u <= 'z')
                    ? DirectScript.Latin
                    : DirectScript.None;
            }

            // Latin-1 Supplement through Latin Extended-B, and Latin Extended Additional.
            if (u >= 0x00C0 && u <= 0x024F) { return DirectScript.Latin; }
            if (u >= 0x1E00 && u <= 0x1EFF) { return DirectScript.Latin; }

            if (u >= 0x0400 && u <= 0x052F) { return DirectScript.Cyrillic; }

            // Arabic, Syriac, Arabic Supplement, Extended-A, and both
            // presentation-form blocks - a shaped string is still Arabic.
            if (u >= 0x0600 && u <= 0x06FF) { return DirectScript.Arabic; }
            if (u >= 0x0750 && u <= 0x077F) { return DirectScript.Arabic; }
            if (u >= 0x08A0 && u <= 0x08FF) { return DirectScript.Arabic; }
            if (u >= 0xFB50 && u <= 0xFDFF) { return DirectScript.Arabic; }
            if (u >= 0xFE70 && u <= 0xFEFF) { return DirectScript.Arabic; }

            // ------------------------------------------------------
            // CJK, split three ways.
            //
            // Kana is Japanese and Hangul is Korean with no ambiguity.
            // Han ideographs are genuinely shared - the same block is
            // Chinese hanzi and Japanese kanji - and they are reported
            // as Chinese because that is the only language written in
            // Han alone. Japanese text always carries kana, so a
            // Japanese sentence out-votes its own kanji and lands on
            // Japanese; a Chinese sentence has no kana and lands on
            // Chinese. Which is the distinction that actually matters
            // for choosing between a Japanese and a Chinese face.
            // ------------------------------------------------------
            if (u >= 0x3040 && u <= 0x30FF) { return DirectScript.Japanese; }   // hiragana + katakana
            if (u >= 0x31F0 && u <= 0x31FF) { return DirectScript.Japanese; }   // katakana extensions
            if (u >= 0xFF66 && u <= 0xFF9F) { return DirectScript.Japanese; }   // halfwidth katakana

            if (u >= 0x1100 && u <= 0x11FF) { return DirectScript.Korean; }     // hangul jamo
            if (u >= 0x3130 && u <= 0x318F) { return DirectScript.Korean; }     // compatibility jamo
            if (u >= 0xA960 && u <= 0xA97F) { return DirectScript.Korean; }     // jamo extended-A
            if (u >= 0xAC00 && u <= 0xD7AF) { return DirectScript.Korean; }     // hangul syllables

            if (u >= 0x3400 && u <= 0x4DBF) { return DirectScript.Chinese; }    // extension A
            if (u >= 0x4E00 && u <= 0x9FFF) { return DirectScript.Chinese; }    // unified ideographs
            if (u >= 0xF900 && u <= 0xFAFF) { return DirectScript.Chinese; }    // compatibility ideographs
            if (u >= 0x20000 && u <= 0x2FA1F) { return DirectScript.Chinese; }  // extensions B-F, astral

            // Fullwidth Latin and punctuation. Fullwidth forms appear in CJK
            // typesetting, so they belong to the group rather than to Latin.
            if (u >= 0xFF00 && u <= 0xFF60) { return DirectScript.Chinese; }

            // ------------------------------------------------------
            // Emoji and pictographs.
            //
            // The ranges a colour emoji font actually covers, not every
            // block with a picture in it. Three things are deliberately
            // NOT here, because treating them as emoji would hand the
            // emoji font a string that is not emoji:
            //
            //   U+FE0F  the variation selector. It follows another
            //           character and asks for its emoji presentation;
            //           on its own it is a modifier, not a glyph.
            //   U+200D  the zero-width joiner, which builds family and
            //           profession sequences out of other emoji.
            //   0-9 # * the keycap bases. They are digits until a
            //           keycap sequence says otherwise, and a phone
            //           number is not emoji.
            //
            // All three are invisible on their own and score None, so
            // they neither win a vote nor break one.
            // ------------------------------------------------------
            if (u >= 0x1F300 && u <= 0x1F5FF) { return DirectScript.Emoji; }   // misc symbols + pictographs
            if (u >= 0x1F600 && u <= 0x1F64F) { return DirectScript.Emoji; }   // emoticons
            if (u >= 0x1F680 && u <= 0x1F6FF) { return DirectScript.Emoji; }   // transport + map
            if (u >= 0x1F900 && u <= 0x1F9FF) { return DirectScript.Emoji; }   // supplemental pictographs
            if (u >= 0x1FA70 && u <= 0x1FAFF) { return DirectScript.Emoji; }   // symbols extended-A
            if (u >= 0x1F1E6 && u <= 0x1F1FF) { return DirectScript.Emoji; }   // regional indicators (flags)
            if (u >= 0x2600 && u <= 0x26FF) { return DirectScript.Emoji; }     // misc symbols
            if (u >= 0x2700 && u <= 0x27BF) { return DirectScript.Emoji; }     // dingbats
            if (u >= 0x1F000 && u <= 0x1F0FF) { return DirectScript.Emoji; }   // mahjong, dominoes, cards
            if (u >= 0x1F200 && u <= 0x1F2FF) { return DirectScript.Emoji; }   // enclosed ideographic supplement

            // CJK punctuation is shared with Latin in mixed text often enough
            // that letting it choose a font would be wrong.
            if (u >= 0x3000 && u <= 0x303F) { return DirectScript.None; }

            // A lone surrogate reaching here is half of a pair somebody
            // sliced apart. It has no script and must not be guessed at.
            if (u >= 0xD800 && u <= 0xDFFF) { return DirectScript.None; }

            // Everything else that is a letter rather than punctuation.
            // char.IsLetter only takes a char, so an astral codepoint is
            // asked about through its own overload.
            if (u > 0xFFFF)
            {
                return System.Char.IsLetter(System.Char.ConvertFromUtf32(u), 0)
                    ? DirectScript.Other
                    : DirectScript.None;
            }

            return System.Char.IsLetter((char)u) ? DirectScript.Other : DirectScript.None;
        }

        // ==========================================
        // CodepointAt
        // One codepoint, and the index moved past it.
        //
        // Every walk over a string in this file goes through
        // here, so the surrogate-pair step exists once. It takes
        // the index by reference rather than returning a pair
        // because the alternative shapes all allocate: an
        // iterator allocates an enumerator, and a callback
        // allocates a closure the moment it captures a counter -
        // and the caller of this is DominantSpecificOf, which
        // runs on every text change of every label. The version
        // this replaced was a plain char loop with no allocation
        // at all, and making it correct for emoji is not worth
        // making it allocate.
        // ==========================================
        public static int CodepointAt(string text, ref int index)
        {
            char high = text[index];
            index++;

            if (char.IsHighSurrogate(high) && index < text.Length
                && char.IsLowSurrogate(text[index]))
            {
                char low = text[index];
                index++;
                return char.ConvertToUtf32(high, low);
            }

            return high;
        }

        /// <summary>
        /// Walks a string by codepoint, calling <paramref name="visit"/> once per
        /// codepoint with its specific script.
        ///
        /// A convenience for callers outside this file. Nothing in here uses it:
        /// see the note on CodepointAt about what a callback costs on a path that
        /// runs per text change.
        /// </summary>
        public static void ForEach(string text, System.Action<int, DirectScript> visit)
        {
            if (string.IsNullOrEmpty(text) || visit == null) { return; }

            for (int i = 0; i < text.Length; )
            {
                int u = CodepointAt(text, ref i);
                visit(u, SpecificOf(u));
            }
        }

        /// <summary>
        /// The script group most of <paramref name="text"/> is in - the one a font
        /// should be chosen for.
        ///
        /// Counted by codepoint rather than decided by the first letter, so
        /// "Unity ۱۲۳ سلام دنیا" is Persian rather than English. Characters
        /// with no script of their own - digits, spaces, punctuation - do not
        /// vote.
        ///
        /// Returns a GROUP, never one of the specific values, so every caller
        /// written against the first release keeps getting what it expects.
        /// Use <see cref="DominantSpecificOf"/> for the finer answer.
        /// </summary>
        public static DirectScript DominantOf(string text)
            => Group(DominantSpecificOf(text));

        /// <summary>
        /// The specific script most of <paramref name="text"/> is in - Japanese
        /// rather than CJK, and Emoji rather than nothing.
        ///
        /// Ties are broken in a fixed order rather than by whichever comparison
        /// ran last, because "which font does this label use" must not depend on
        /// the order of lines in this file. Arabic first: it is the script this
        /// package exists for and the one whose text is unreadable in the wrong
        /// font, where Latin in a CJK face is merely ugly.
        /// </summary>
        public static DirectScript DominantSpecificOf(string text)
        {
            if (string.IsNullOrEmpty(text)) { return DirectScript.None; }

            int latin = 0, arabic = 0, cyrillic = 0, other = 0;
            int japanese = 0, chinese = 0, korean = 0, emoji = 0;

            for (int i = 0; i < text.Length; )
            {
                switch (SpecificOf(CodepointAt(text, ref i)))
                {
                    case DirectScript.Latin: latin++; break;
                    case DirectScript.Arabic: arabic++; break;
                    case DirectScript.Cyrillic: cyrillic++; break;
                    case DirectScript.Japanese: japanese++; break;
                    case DirectScript.Chinese: chinese++; break;
                    case DirectScript.Korean: korean++; break;
                    case DirectScript.Emoji: emoji++; break;
                    case DirectScript.Other: other++; break;
                }
            }

            DirectScript winner = DirectScript.None;
            int best = 0;

            // Strictly greater-than, in a fixed precedence. See the note above.
            if (arabic > best) { best = arabic; winner = DirectScript.Arabic; }
            if (japanese > best) { best = japanese; winner = DirectScript.Japanese; }
            if (korean > best) { best = korean; winner = DirectScript.Korean; }
            if (chinese > best) { best = chinese; winner = DirectScript.Chinese; }
            if (cyrillic > best) { best = cyrillic; winner = DirectScript.Cyrillic; }
            if (emoji > best) { best = emoji; winner = DirectScript.Emoji; }
            if (latin > best) { best = latin; winner = DirectScript.Latin; }
            if (other > best) { winner = DirectScript.Other; }

            return winner;
        }

        /// <summary>Every script present in the text, for showing in an Inspector.</summary>
        public static string Describe(string text)
        {
            if (string.IsNullOrEmpty(text)) { return "nothing"; }

            bool latin = false, arabic = false, cyrillic = false, other = false;
            bool japanese = false, chinese = false, korean = false, emoji = false;

            for (int i = 0; i < text.Length; )
            {
                switch (SpecificOf(CodepointAt(text, ref i)))
                {
                    case DirectScript.Latin: latin = true; break;
                    case DirectScript.Arabic: arabic = true; break;
                    case DirectScript.Cyrillic: cyrillic = true; break;
                    case DirectScript.Japanese: japanese = true; break;
                    case DirectScript.Chinese: chinese = true; break;
                    case DirectScript.Korean: korean = true; break;
                    case DirectScript.Emoji: emoji = true; break;
                    case DirectScript.Other: other = true; break;
                }
            }

            string list = string.Empty;
            if (latin) { list = Add(list, "Latin"); }
            if (arabic) { list = Add(list, "Persian/Arabic"); }
            if (japanese) { list = Add(list, "Japanese"); }
            if (chinese) { list = Add(list, "Chinese"); }
            if (korean) { list = Add(list, "Korean"); }
            if (cyrillic) { list = Add(list, "Cyrillic"); }
            if (emoji) { list = Add(list, "emoji"); }
            if (other) { list = Add(list, "other"); }

            return list.Length == 0 ? "no letters" : list;
        }

        private static string Add(string list, string item)
            => list.Length == 0 ? item : list + ", " + item;
    }
}
