// ==========================================
// DirectTMPBrand
// The look of every Unity DirectTMP window.
//
// Unity DocSnap is the boba cup and the cozy pastel
// site. This is the other half of the same shelf: the
// Inkwell palette - teal ink on warm cream paper with
// a brass nib - and Inky, the little inkwell with
// three glyph bubbles rising out of it.
//
// Why a Unity editor tool bothers with a mascot at
// all: a window that looks like every other IMGUI
// panel is a window nobody remembers installing. The
// tool ships two things a person sees before they see
// a single feature - the entry in the menu bar and
// the header of the first window they open - and both
// of them are free to make recognisable.
//
// Everything here is drawn, not imported. The mascot
// is rasterized in code into a small texture at first
// use, which means:
//   * no .png in the package, so no import settings
//     to get wrong, no texture compression to blur it,
//     and nothing for a sprite atlas to swallow;
//   * it renders at the editor's own DPI, so it is
//     crisp on a Retina display rather than a fuzzy
//     32px bitmap scaled up;
//   * it tints for the dark skin instead of shipping
//     twice.
//
// Colours come from DirectTMPConstants, which is also
// what the product page and the README badges read,
// so the tool and its shop window can never drift
// apart.
// ==========================================
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    /// <summary>
    /// Palette, mascot and the handful of drawing helpers that give every
    /// Unity DirectTMP window the same face.
    /// </summary>
    internal static class DirectTMPBrand
    {
        // ==========================================
        // The Inkwell palette
        // ==========================================
        public static readonly Color Ink = Hex(DirectTMPConstants.ColorInk);
        public static readonly Color InkDeep = Hex(DirectTMPConstants.ColorInkDeep);
        public static readonly Color Brass = Hex(DirectTMPConstants.ColorBrass);
        public static readonly Color Paper = Hex(DirectTMPConstants.ColorPaper);
        public static readonly Color Blush = Hex(DirectTMPConstants.ColorBlush);
        public static readonly Color Night = Hex(DirectTMPConstants.ColorNight);

        /// <summary>
        /// The accent, adjusted for the skin in use. The teal that reads well
        /// on cream is too dark to sit on Unity's dark background, so the dark
        /// skin gets a lifted version rather than a second palette.
        /// </summary>
        public static Color Accent => EditorGUIUtility.isProSkin ? Lift(Ink, 0.32f) : Ink;

        /// <summary>The page behind a branded panel.</summary>
        public static Color Surface => EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.045f)
            : new Color(Paper.r, Paper.g, Paper.b, 0.85f);

        /// <summary>A hairline. Never a shadow - shadows in IMGUI are texture work per repaint.</summary>
        public static Color Border => EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.12f)
            : new Color(0f, 0f, 0f, 0.13f);

        /// <summary>Secondary text that still has to clear a contrast check.</summary>
        public static Color TextDim => EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.62f)
            : new Color(0f, 0f, 0f, 0.58f);

        // Semantic colours for the coverage badges. Green / amber / grey is
        // the whole vocabulary: a font either has the script, has some of it,
        // or does not.
        public static Color Ok => EditorGUIUtility.isProSkin ? new Color(0.42f, 0.80f, 0.55f) : new Color(0.13f, 0.48f, 0.28f);
        public static Color Warn => EditorGUIUtility.isProSkin ? Lift(Brass, 0.10f) : new Color(0.72f, 0.52f, 0.10f);
        public static Color Off => TextDim;

        // ==========================================
        // Styles
        //
        // Built lazily and cached. A GUIStyle allocated
        // inside OnGUI is a garbage-collected object per
        // repaint per control, which on a catalog window
        // with two hundred rows is the difference between
        // a smooth scroll and a stuttering one.
        // ==========================================
        private static GUIStyle s_title;
        private static GUIStyle s_subtitle;
        private static GUIStyle s_section;
        private static GUIStyle s_badge;
        private static GUIStyle s_card;
        private static GUIStyle s_wordmark;
        private static GUIStyle s_body;
        private static GUIStyle s_bold;
        private static GUIStyle s_line;

        // ==========================================
        // Alignment follows the language.
        //
        // A Persian paragraph left-aligned inside an
        // English layout is legible and still looks
        // wrong - the ragged edge lands on the side the
        // eye starts from. Every style the tool owns
        // flips; Unity's own EditorStyles do not, which
        // is why the windows use these for anything
        // longer than a word.
        //
        // The styles are cached, so a language switch
        // has to drop them - DirectTMPMenu and the two
        // language dropdowns all call InvalidateStyles.
        // ==========================================
        private static bool Rtl => DirectTMPText.IsRightToLeft;

        private static TextAnchor Leading(TextAnchor ltr, TextAnchor rtl) => Rtl ? rtl : ltr;

        public static GUIStyle Title => s_title ?? (s_title = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = Leading(TextAnchor.MiddleLeft, TextAnchor.MiddleRight),
            margin = new RectOffset(0, 0, 0, 0)
        });

        public static GUIStyle Wordmark => s_wordmark ?? (s_wordmark = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = Leading(TextAnchor.LowerLeft, TextAnchor.LowerRight),
            padding = new RectOffset(0, 0, 0, 0),
            normal = { textColor = EditorGUIUtility.isProSkin ? Lift(Ink, 0.42f) : InkDeep }
        });

        public static GUIStyle Subtitle => s_subtitle ?? (s_subtitle = new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap = true,
            alignment = Leading(TextAnchor.UpperLeft, TextAnchor.UpperRight),
            normal = { textColor = TextDim }
        });

        public static GUIStyle Section => s_section ?? (s_section = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            alignment = Leading(TextAnchor.MiddleLeft, TextAnchor.MiddleRight),
            margin = new RectOffset(0, 0, 10, 4)
        });

        /// <summary>A wrapped paragraph. Drawn through <see cref="Body(string,float)"/>, never directly.</summary>
        public static GUIStyle BodyText => s_body ?? (s_body = new GUIStyle(EditorStyles.label)
        {
            wordWrap = true,
            alignment = Leading(TextAnchor.UpperLeft, TextAnchor.UpperRight)
        });

        /// <summary>A bold single line that follows the reading direction.</summary>
        public static GUIStyle BoldLine => s_bold ?? (s_bold = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = Leading(TextAnchor.MiddleLeft, TextAnchor.MiddleRight)
        });

        /// <summary>A plain single line that follows the reading direction.</summary>
        public static GUIStyle Line => s_line ?? (s_line = new GUIStyle(EditorStyles.label)
        {
            alignment = Leading(TextAnchor.MiddleLeft, TextAnchor.MiddleRight)
        });

        public static GUIStyle Badge => s_badge ?? (s_badge = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(7, 7, 1, 1),
            margin = new RectOffset(0, 4, 1, 1),
            fontSize = 10
        });

        public static GUIStyle Card => s_card ?? (s_card = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(11, 11, 9, 9),
            margin = new RectOffset(0, 0, 3, 3)
        });

        /// <summary>
        /// Drops the cached styles. Called when the editor skin flips, because
        /// every cached style baked the old skin's text colour into itself.
        /// </summary>
        public static void InvalidateStyles()
        {
            s_title = null;
            s_subtitle = null;
            s_section = null;
            s_badge = null;
            s_card = null;
            s_wordmark = null;
            s_body = null;
            s_bold = null;
            s_line = null;
        }

        // ==========================================
        // Header
        //
        // The band at the top of every window: Inky, the
        // wordmark, a one-line description of what this
        // particular window is for, and the version.
        //
        // It is the same shape in all five windows on
        // purpose. A person who has opened the catalog
        // once knows where the title is in the Editor
        // Font window without reading it.
        // ==========================================
        public static void Header(string title, string subtitle)
        {
            const float height = 54f;
            Rect band = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(band, Surface);
            EditorGUI.DrawRect(new Rect(band.x, band.yMax - 1f, band.width, 1f), Border);

            // In a right-to-left interface the whole band mirrors: the accent
            // stripe, the mascot and the text all move to the side the eye
            // starts from, and the version - which is the one thing that is
            // never translated - takes the other end. Reading order is a
            // layout property, not a text property, and a header that ignores
            // it is the tell that a translation was bolted on.
            bool rtl = Rtl;

            float stripeX = rtl ? band.xMax - 3f : band.x;
            EditorGUI.DrawRect(new Rect(stripeX, band.y, 3f, band.height), Accent);

            var mascot = rtl
                ? new Rect(band.xMax - 53f, band.y + 7f, 40f, 40f)
                : new Rect(band.x + 13f, band.y + 7f, 40f, 40f);
            GUI.DrawTexture(mascot, Mascot, ScaleMode.ScaleToFit);

            const float versionWidth = 74f;
            float textWidth = band.width - 64f - versionWidth - 10f;
            float textX = rtl ? band.x + versionWidth + 10f : mascot.xMax + 11f;

            var titleRect = new Rect(textX, band.y + 6f, Mathf.Max(40f, textWidth), 24f);
            GUI.Label(titleRect, title, Wordmark);

            var subRect = new Rect(titleRect.x, titleRect.yMax + 1f, titleRect.width, 22f);
            GUI.Label(subRect, subtitle, Subtitle);

            var versionRect = rtl
                ? new Rect(band.x + 10f, band.y + 8f, 64f, 14f)
                : new Rect(band.xMax - versionWidth, band.y + 8f, 64f, 14f);

            var version = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = rtl ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight,
                normal = { textColor = TextDim }
            };
            GUI.Label(versionRect, "v" + DirectTMPConstants.Version, version);

            GUILayout.Space(6);
        }

        // ==========================================
        // Body
        //
        // A wrapped paragraph, in the reader's script
        // and the reader's direction.
        //
        // IMGUI cannot wrap right-to-left text: it is
        // handed a string that has already been turned
        // around for display, so where it chooses to
        // break the line is where the paragraph breaks
        // BACKWARDS, and three lines come out in reverse
        // order. DirectTMPText.Wrap breaks the text
        // first, while it is still in reading order, and
        // hands IMGUI lines that need no wrapping.
        //
        // Which means the width has to be known before
        // the text is measured, and IMGUI does not offer
        // it during the layout pass. currentViewWidth
        // minus an inset is the honest answer, and the
        // inset errs generous on purpose: a line broken
        // slightly early costs nothing, and a line
        // broken slightly late gets re-broken by IMGUI
        // in the wrong place.
        // ==========================================
        /// <summary>The default room a panel's own padding and scrollbar take.</summary>
        public const float BodyInset = 72f;

        /// <summary>
        /// Draws a localised paragraph. This is the overload to reach for:
        /// it picks the language itself, so the text is still in logical order
        /// when it is broken into lines.
        /// </summary>
        public static void Body(string en, string ja, string fa)
        {
            Body(DirectTMPText.Raw(en, ja, fa), BodyText, BodyInset);
        }

        /// <summary>The same, with room taken out for a panel's own padding.</summary>
        public static void Body(string en, string ja, string fa, float inset)
        {
            Body(DirectTMPText.Raw(en, ja, fa), BodyText, inset);
        }

        /// <summary>
        /// Draws a wrapped paragraph from text in LOGICAL order - a string as
        /// stored, not one that has already been through L() or Show().
        ///
        /// The distinction is not pedantry. Wrapping means choosing where to
        /// break, and the only order in which a break point means anything is
        /// the order the words are read in. Text that has already been
        /// reordered for display would be broken into lines that come out
        /// bottom-to-top.
        /// </summary>
        public static void Body(string logical, float inset = BodyInset)
        {
            Body(logical, BodyText, inset);
        }

        /// <summary>The same, in a style of the caller's choosing.</summary>
        public static void Body(string logical, GUIStyle style, float inset = BodyInset)
        {
            if (string.IsNullOrEmpty(logical)) { return; }

            GUILayout.Label(DirectTMPText.Wrap(logical, style, WrapWidth(inset)), style);
        }

        /// <summary>
        /// The width a paragraph has to fit into. Never smaller than something
        /// a word fits on, because a docked inspector two hundred pixels wide
        /// would otherwise ask for a line break after every character.
        /// </summary>
        public static float WrapWidth(float inset = BodyInset)
        {
            return Mathf.Max(120f, EditorGUIUtility.currentViewWidth - inset);
        }

        // ==========================================
        // Note
        //
        // What EditorGUILayout.HelpBox would draw, if
        // HelpBox could wrap Persian.
        //
        // It cannot: the message goes straight into a
        // style with wordWrap on, and there is no way to
        // pre-break it because the box owns its own
        // width. Every long explanation in this tool is
        // in a HelpBox, and every one of them was
        // unreadable in Persian - so the tool draws its
        // own, out of the card it already had.
        // ==========================================
        /// <summary>How loud a note is.</summary>
        public enum NoteLevel
        {
            Plain,
            Info,
            Warning,
            Error
        }

        /// <summary>A boxed, localised explanation. Picks the language itself.</summary>
        public static void Note(string en, string ja, string fa, NoteLevel level = NoteLevel.Info)
        {
            Note(DirectTMPText.Raw(en, ja, fa), level);
        }

        /// <summary>
        /// A boxed explanation from text in LOGICAL order. See
        /// <see cref="Body(string,float)"/> for why the distinction matters.
        /// </summary>
        public static void Note(string logical, NoteLevel level = NoteLevel.Info, float inset = BodyInset)
        {
            if (string.IsNullOrEmpty(logical)) { return; }

            bool marked = level != NoteLevel.Plain;

            using (new EditorGUILayout.HorizontalScope(Card))
            {
                if (marked && !Rtl) { Marker(level); }

                // The marker column and the card's own padding come out of the
                // width the text has to fit in, or the last word of every
                // paragraph wraps on its own.
                Body(logical, BodyText, inset + (marked ? 48f : 26f));

                if (marked && Rtl) { Marker(level); }
            }
        }

        private static void Marker(NoteLevel level)
        {
            Color previous = GUI.contentColor;
            GUI.contentColor = TintFor(level);
            GUILayout.Label(MarkerFor(level), EditorStyles.boldLabel, GUILayout.Width(18));
            GUI.contentColor = previous;
        }

        private static Color TintFor(NoteLevel level)
        {
            switch (level)
            {
                case NoteLevel.Error: return Blush;
                case NoteLevel.Warning: return Warn;
                case NoteLevel.Info: return Accent;
                default: return TextDim;
            }
        }

        private static string MarkerFor(NoteLevel level)
        {
            switch (level)
            {
                case NoteLevel.Error: return "!";
                case NoteLevel.Warning: return "~";
                default: return "i";
            }
        }

        // ==========================================
        // Small drawing helpers
        // ==========================================

        /// <summary>A rounded, tinted chip - the coverage badges and tier tags.</summary>
        public static void Pill(string label, Color tint, string tooltip = null)
        {
            var content = new GUIContent(label, tooltip);
            Vector2 size = Badge.CalcSize(content);
            Rect rect = GUILayoutUtility.GetRect(size.x, 16f, Badge, GUILayout.Width(size.x));

            EditorGUI.DrawRect(rect, new Color(tint.r, tint.g, tint.b, EditorGUIUtility.isProSkin ? 0.18f : 0.14f));

            Color previous = GUI.contentColor;
            GUI.contentColor = tint;
            GUI.Label(rect, content, Badge);
            GUI.contentColor = previous;
        }

        /// <summary>A hairline separator with breathing room on both sides.</summary>
        public static void Divider(float spacingAbove = 8f, float spacingBelow = 8f)
        {
            GUILayout.Space(spacingAbove);
            Rect rect = GUILayoutUtility.GetRect(0, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, Border);
            GUILayout.Space(spacingBelow);
        }

        /// <summary>A section heading in the brand's accent.</summary>
        public static void SectionTitle(string label)
        {
            Color previous = GUI.contentColor;
            GUI.contentColor = Accent;
            GUILayout.Label(label, Section);
            GUI.contentColor = previous;
        }

        /// <summary>
        /// The footer every window shares: the tool, its version, and the two
        /// links a person actually wants (the docs and the issue tracker).
        /// </summary>
        public static void Footer()
        {
            Divider(10f, 6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                var muted = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = TextDim } };
                GUILayout.Label($"{DirectTMPConstants.Mark} {DirectTMPConstants.ToolName} v{DirectTMPConstants.Version}", muted);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(DirectTMPText.L("Docs", "ドキュメント", "راهنما"), EditorStyles.miniButton, GUILayout.Width(78)))
                {
                    Application.OpenURL(DirectTMPConstants.DocsUrl);
                }
                if (GUILayout.Button(DirectTMPText.L("Report a bug", "不具合を報告", "گزارش مشکل"), EditorStyles.miniButton, GUILayout.Width(104)))
                {
                    Application.OpenURL(DirectTMPConstants.IssuesUrl);
                }
            }
        }

        // ==========================================
        // Inky
        //
        // Rasterized once into a 128px texture and
        // cached for the session. The drawing is a stack
        // of analytically-defined shapes (discs, capsules
        // and a quadrilateral for the nib) sampled 3x3
        // per pixel, which is what gives it clean edges
        // without shipping an image or depending on a
        // shader.
        //
        // Composition, bottom to top:
        //   the pot      a squat rounded body in teal ink
        //   the ink      a darker ellipse for the surface
        //   the nib      a brass wedge leaning out of it
        //   the bubbles  three glyph bubbles rising - the
        //                tool's entire pitch, drawn: one
        //                Latin, one CJK, one Arabic-script
        //
        // The bubbles are drawn as plain discs here and
        // the letters are painted over them by the header
        // as text, because rasterizing three scripts by
        // hand into a 128px texture is a font problem,
        // and this package has opinions about solving
        // font problems with fonts.
        // ==========================================
        private static Texture2D s_mascot;
        private static bool s_mascotProSkin;

        public static Texture2D Mascot
        {
            get
            {
                if (s_mascot == null || s_mascotProSkin != EditorGUIUtility.isProSkin)
                {
                    if (s_mascot != null) { Object.DestroyImmediate(s_mascot); }
                    s_mascot = BuildMascot(128);
                    s_mascotProSkin = EditorGUIUtility.isProSkin;
                }
                return s_mascot;
            }
        }

        private static Texture2D BuildMascot(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "DirectTMP Inky"
            };

            Color ink = EditorGUIUtility.isProSkin ? Lift(Ink, 0.22f) : Ink;
            Color inkDeep = EditorGUIUtility.isProSkin ? Lift(InkDeep, 0.16f) : InkDeep;
            Color brass = Brass;
            Color bubble = EditorGUIUtility.isProSkin ? Lift(Paper, -0.06f) : Paper;
            Color outline = EditorGUIUtility.isProSkin ? new Color(0.06f, 0.05f, 0.09f, 1f) : Night;

            var pixels = new Color32[size * size];
            const int samples = 3;
            float step = 1f / (samples + 1);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Supersample: the shapes are analytic, so the only way to
                    // get a smooth edge is to ask several times per pixel.
                    float r = 0f, g = 0f, b = 0f, a = 0f;
                    for (int sy = 1; sy <= samples; sy++)
                    {
                        for (int sx = 1; sx <= samples; sx++)
                        {
                            // Normalised to a 0..1 box with y pointing up, so
                            // the shape maths below reads like a drawing
                            // rather than like texture indexing.
                            float u = (x + (sx * step)) / size;
                            float v = 1f - ((y + (sy * step)) / size);

                            Color c = SampleMascot(u, v, ink, inkDeep, brass, bubble, outline);
                            r += c.r * c.a;
                            g += c.g * c.a;
                            b += c.b * c.a;
                            a += c.a;
                        }
                    }

                    int taken = samples * samples;
                    if (a > 0.0001f)
                    {
                        pixels[(y * size) + x] = new Color(r / a, g / a, b / a, a / taken);
                    }
                    else
                    {
                        pixels[(y * size) + x] = new Color(0f, 0f, 0f, 0f);
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        // One sample of the drawing, in a 0..1 box with the origin bottom-left.
        // Painted back to front; the last shape that claims the point wins.
        private static Color SampleMascot(float u, float v, Color ink, Color inkDeep, Color brass, Color bubble, Color outline)
        {
            Color result = new Color(0f, 0f, 0f, 0f);

            // --- three glyph bubbles rising out of the pot ---
            // Descending size so the eye reads them as moving away.
            if (InDisc(u, v, 0.235f, 0.845f, 0.088f)) { result = Ring(u, v, 0.235f, 0.845f, 0.088f, bubble, outline); }
            if (InDisc(u, v, 0.500f, 0.905f, 0.076f)) { result = Ring(u, v, 0.500f, 0.905f, 0.076f, bubble, outline); }
            if (InDisc(u, v, 0.760f, 0.820f, 0.064f)) { result = Ring(u, v, 0.760f, 0.820f, 0.064f, bubble, outline); }

            // --- the nib: a brass wedge leaning out of the pot ---
            // Two overlapping capsules make a tapered shape without needing a
            // polygon test, and the taper is what makes it read as a pen.
            if (InCapsule(u, v, 0.545f, 0.470f, 0.775f, 0.700f, 0.052f))
            {
                result = Blend(result, brass);
                if (OnCapsuleEdge(u, v, 0.545f, 0.470f, 0.775f, 0.700f, 0.052f, 0.011f)) { result = Blend(result, outline); }
            }
            if (InCapsule(u, v, 0.500f, 0.415f, 0.585f, 0.505f, 0.031f))
            {
                result = Blend(result, Lift(brass, -0.18f));
            }

            // --- the pot ---
            // A rounded box: wider than tall, with a slightly narrower neck,
            // which is the silhouette that says "inkwell" and not "mug".
            if (InRoundedBox(u, v, 0.500f, 0.300f, 0.360f, 0.230f, 0.095f))
            {
                result = Blend(result, ink);
                if (!InRoundedBox(u, v, 0.500f, 0.300f, 0.360f - 0.020f, 0.230f - 0.020f, 0.085f))
                {
                    result = Blend(result, outline);
                }
            }

            // --- the ink surface, seen through the mouth of the pot ---
            if (InEllipse(u, v, 0.500f, 0.487f, 0.215f, 0.058f))
            {
                result = Blend(result, inkDeep);
                if (!InEllipse(u, v, 0.500f, 0.487f, 0.195f, 0.042f)) { result = Blend(result, outline); }
            }

            // --- the label band, because a jar wants a label ---
            if (v > 0.235f && v < 0.300f && InRoundedBox(u, v, 0.500f, 0.300f, 0.360f, 0.230f, 0.095f))
            {
                result = Blend(result, new Color(bubble.r, bubble.g, bubble.b, 0.92f));
            }

            // --- two dot eyes, so Inky is a character and not a bottle ---
            if (InDisc(u, v, 0.418f, 0.352f, 0.026f)) { result = Blend(result, outline); }
            if (InDisc(u, v, 0.582f, 0.352f, 0.026f)) { result = Blend(result, outline); }

            return result;
        }

        // ==========================================
        // Shape predicates. Analytic, allocation-free,
        // and each one small enough to be obviously
        // right - the texture is built once, so clarity
        // beats cleverness here.
        // ==========================================
        private static bool InDisc(float u, float v, float cx, float cy, float radius)
        {
            float dx = u - cx, dy = v - cy;
            return (dx * dx) + (dy * dy) <= radius * radius;
        }

        private static bool InEllipse(float u, float v, float cx, float cy, float rx, float ry)
        {
            float dx = (u - cx) / rx, dy = (v - cy) / ry;
            return (dx * dx) + (dy * dy) <= 1f;
        }

        private static bool InRoundedBox(float u, float v, float cx, float cy, float width, float height, float radius)
        {
            float dx = Mathf.Abs(u - cx) - ((width * 0.5f) - radius);
            float dy = Mathf.Abs(v - cy) - ((height * 0.5f) - radius);
            if (dx <= 0f && dy <= 0f) { return true; }
            dx = Mathf.Max(dx, 0f);
            dy = Mathf.Max(dy, 0f);
            return (dx * dx) + (dy * dy) <= radius * radius;
        }

        private static float DistanceToSegment(float u, float v, float x0, float y0, float x1, float y1)
        {
            float dx = x1 - x0, dy = y1 - y0;
            float lengthSquared = (dx * dx) + (dy * dy);
            float t = lengthSquared <= 0f ? 0f : Mathf.Clamp01((((u - x0) * dx) + ((v - y0) * dy)) / lengthSquared);
            float px = x0 + (t * dx) - u;
            float py = y0 + (t * dy) - v;
            return Mathf.Sqrt((px * px) + (py * py));
        }

        private static bool InCapsule(float u, float v, float x0, float y0, float x1, float y1, float radius)
        {
            return DistanceToSegment(u, v, x0, y0, x1, y1) <= radius;
        }

        private static bool OnCapsuleEdge(float u, float v, float x0, float y0, float x1, float y1, float radius, float thickness)
        {
            float d = DistanceToSegment(u, v, x0, y0, x1, y1);
            return d <= radius && d >= radius - thickness;
        }

        // A filled disc with a dark rim, which is the cartoon look the whole
        // mascot is built out of.
        private static Color Ring(float u, float v, float cx, float cy, float radius, Color fill, Color rim)
        {
            float dx = u - cx, dy = v - cy;
            float d = Mathf.Sqrt((dx * dx) + (dy * dy));
            return d >= radius - 0.013f ? rim : fill;
        }

        private static Color Blend(Color under, Color over)
        {
            if (over.a >= 0.999f) { return over; }
            float a = over.a + (under.a * (1f - over.a));
            if (a <= 0.0001f) { return new Color(0f, 0f, 0f, 0f); }
            return new Color(
                ((over.r * over.a) + (under.r * under.a * (1f - over.a))) / a,
                ((over.g * over.a) + (under.g * under.a * (1f - over.a))) / a,
                ((over.b * over.a) + (under.b * under.a * (1f - over.a))) / a,
                a);
        }

        // ==========================================
        // Colour maths
        // ==========================================

        /// <summary>
        /// Parses "#RRGGBB" into a Color. Returns magenta on malformed input
        /// rather than throwing - a palette constant with a typo should be
        /// loud and obvious in the window, not a stack trace on domain reload.
        /// </summary>
        public static Color Hex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) { return Color.magenta; }
            string trimmed = hex.TrimStart('#');
            if (trimmed.Length != 6) { return Color.magenta; }

            if (!int.TryParse(trimmed, System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out int value))
            {
                return Color.magenta;
            }

            return new Color(
                ((value >> 16) & 0xFF) / 255f,
                ((value >> 8) & 0xFF) / 255f,
                (value & 0xFF) / 255f,
                1f);
        }

        /// <summary>
        /// Moves a colour toward white (positive amount) or black (negative).
        /// One function for both directions so the dark-skin adjustments read
        /// as one idea rather than as a lighten helper and a darken helper that
        /// drifted apart.
        /// </summary>
        public static Color Lift(Color color, float amount)
        {
            float target = amount >= 0f ? 1f : 0f;
            float t = Mathf.Abs(amount);
            return new Color(
                Mathf.Lerp(color.r, target, t),
                Mathf.Lerp(color.g, target, t),
                Mathf.Lerp(color.b, target, t),
                color.a);
        }

        /// <summary>The badge colour for a coverage verdict.</summary>
        public static Color ColorFor(DirectScriptCoverage coverage)
        {
            switch (coverage)
            {
                case DirectScriptCoverage.Full: return Ok;
                case DirectScriptCoverage.Partial: return Warn;
                default: return Off;
            }
        }
    }
}
