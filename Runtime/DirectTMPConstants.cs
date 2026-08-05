// ==========================================
// DirectTMPConstants
// Shared identity, brand and default values used
// across the Unity DirectTMP runtime. Anything an
// author might want to tune (sampling size, atlas
// size, render mode) has a default here; the editor
// Settings screen simply overrides these.
//
// Identity lives in the RUNTIME assembly rather than
// the editor one on purpose. A build that logs
// "[Unity DirectTMP] could not read that font" has
// to be able to name the tool, and a player build
// never compiles the Editor assembly.
// ==========================================
using UnityEngine.TextCore.LowLevel;

namespace UnityDirectTMP
{
    /// <summary>
    /// Constant identity and default tuning values for Unity DirectTMP.
    /// Kept in one place so the runtime, the Inspector and the Settings
    /// screen never disagree about a default.
    /// </summary>
    public static class DirectTMPConstants
    {
        // ==========================================
        // Identity
        //
        // Version must match "version" in package.json.
        // The two are stamped into the About window, the
        // Settings page footer, the catalog header and
        // every warning this package logs, so a drift
        // between them is a tool that misreports itself
        // in five places at once. PackageVersionTests
        // (inside Unity) and validate_package.py (in CI,
        // with no licence needed) both fail if they ever
        // disagree.
        // ==========================================
        public const string ToolName = "Unity DirectTMP";
        public const string ShortName = "DirectTMP";
        public const string Version = "1.2.2";
        public const string Author = "AmirCollider";
        public const string GithubUrl = "https://github.com/AmirCollider/UnityDirectTMP";

        // ==========================================
        // The brand.
        //
        // Unity DocSnap is the boba cup; this is the
        // inkwell. Both are AmirCollider tools and both
        // are meant to be recognisable at a glance in a
        // menu bar full of grey text, which is the whole
        // reason a mark and a named palette exist at all.
        //
        // "Inky" is the mascot: a squat teal inkwell with
        // a brass nib and three glyph bubbles rising out
        // of it (A / あ / س - Latin, CJK, Arabic script),
        // which is the tool's entire pitch drawn in one
        // picture. The full drawing ships as
        // Docs~/mascot.svg and as an inline vector the
        // editor windows paint themselves.
        //
        // "Inkwell" is the palette: teal ink on warm
        // cream paper, with a brass accent. Deliberately
        // nothing like DocSnap's pink-and-violet boba, so
        // two windows from the same author are never
        // mistaken for each other.
        // ==========================================
        public const string Mark = "\U0001F58B";          // the fountain pen
        public const string MarkAlt = "\U0001FAB6";       // the quill, for tighter spots
        public const string MascotName = "Inky";
        public const string ThemeName = "Inkwell";

        // The Inkwell palette, as hex. The editor chrome
        // reads these through DirectTMPBrand (which turns
        // them into Colors and dims them for the dark
        // skin); the product page and the README badges
        // use the very same six values, so the tool and
        // its shop window are the same colour.
        public const string ColorInk = "#14808C";         // teal ink - the primary
        public const string ColorInkDeep = "#0B5A63";     // pressed / borders
        public const string ColorBrass = "#F0A73E";       // the nib - highlights, "new"
        public const string ColorPaper = "#FBF6EC";       // warm cream - light surfaces
        public const string ColorBlush = "#E8927C";       // warnings that are not errors
        public const string ColorNight = "#1B1725";       // near-black ink - dark surfaces

        // ==========================================
        // Where the tool points people.
        //
        // These appear in the About window, the Settings
        // page, the catalog's empty state and the promo
        // panel. One of them changing and four of them
        // not is exactly the kind of thing nobody notices
        // until somebody clicks a dead link.
        // ==========================================
        public const string SiteBaseUrl = "https://amircollider.com";
        public const string ProductUrl = SiteBaseUrl + "/unity-directtmp";
        public const string ToolsCatalogUrl = SiteBaseUrl + "/tools";
        public const string DocsUrl = GithubUrl + "#readme";
        public const string IssuesUrl = GithubUrl + "/issues";
        public const string ChangelogUrl = GithubUrl + "/blob/main/CHANGELOG.md";

        // The sibling tool. DirectTMP promotes DocSnap and
        // DocSnap promotes DirectTMP; both are one line in
        // a panel a person can dismiss forever, which is
        // the only kind of cross-promotion a developer
        // tool gets away with.
        public const string SiblingToolName = "Unity DocSnap";
        public const string SiblingToolMark = "\U0001F9CB";   // the boba cup
        public const string SiblingToolUrl = SiteBaseUrl + "/unity-docsnap";

        // ==========================================
        // Atlas / rasterization defaults
        //
        // 90 is TextMeshPro's own default sampling
        // point size for a dynamic SDF atlas - large
        // enough to stay crisp when scaled up, small
        // enough that a page of CJK still fits a single
        // 1024 texture before spilling into a second.
        // ==========================================
        public const int DefaultSamplingPointSize = 90;
        public const int DefaultAtlasPadding = 9;
        public const int DefaultAtlasWidth = 1024;
        public const int DefaultAtlasHeight = 1024;
        public const GlyphRenderMode DefaultRenderMode = GlyphRenderMode.SDFAA;

        // A hard floor / ceiling so a stray Settings value
        // can never ask the FontEngine for a 0 px or a
        // 16 384 px atlas.
        public const int MinSamplingPointSize = 16;
        public const int MaxSamplingPointSize = 256;
        public const int MinAtlasDimension = 256;
        public const int MaxAtlasDimension = 8192;
        public const int MaxAtlasPadding = 32;

        // ==========================================
        // Runtime cache folder
        //
        // Fonts handed to DirectTMP as raw bytes are
        // spooled to a file first (the FontEngine reads a
        // face far more reliably from a path than from a
        // freshly-allocated managed array). They live
        // under persistentDataPath so a downloaded font
        // survives an app restart and is shared between
        // every DirectFont that asked for it.
        // ==========================================
        public const string RuntimeCacheFolderName = "UnityDirectTMP/FontCache";

        // ==========================================
        // The suffix appended to a generated dynamic
        // font asset's name, so it is obvious in a
        // profiler / memory view where the asset came
        // from.
        // ==========================================
        public const string GeneratedAssetSuffix = " (DirectTMP)";

        // ==========================================
        // The log prefix. Every Debug.Log this package
        // writes goes through DirectTMPLog, which stamps
        // this on the front, so a user can filter the
        // console by one string and see everything the
        // tool has said - and nothing else.
        // ==========================================
        public const string LogPrefix = "[" + ToolName + "] ";

        // ==========================================
        // The font file extensions the tool recognises
        // as a source. Shared by the catalog scanner,
        // the Project-window context menu validators and
        // the drag-and-drop handlers, so all three agree
        // on what counts as "a font".
        //
        // .ttc / .otc are font COLLECTIONS - several
        // faces in one file. They are listed because the
        // catalog should show them rather than pretend
        // they are not fonts, and the catalog marks them
        // as face-index-required until that lands.
        // ==========================================
        public static readonly string[] FontExtensions = { ".ttf", ".otf", ".ttc", ".otc" };
    }
}
