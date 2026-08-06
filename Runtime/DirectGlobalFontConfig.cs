// ==========================================
// DirectGlobalFontConfig
// One font, one setting, the whole project.
//
// Everything this package could do before 1.3.0 was
// reachable one way: add a Direct Font component to a
// label. That is the right shape for "this caption is
// special" and the wrong shape for "this project is in
// Persian", which is what almost everybody actually
// wanted - because it has to be done on every label
// anyone ever adds, and re-done whenever somebody
// builds a screen without knowing the package exists.
//
// This asset is the other shape. It names a font file
// and a few switches, and DirectGlobalFont applies it
// to every TextMeshPro label in the project without
// touching a single GameObject.
//
// Why an asset in Resources rather than EditorPrefs
// -------------------------------------------------
// Because it has to exist in a BUILD. The Editor-font
// override is a per-machine preference and lives in
// EditorPrefs for good reasons; the font your GAME
// draws with is a fact about the project, has to be
// the same for everybody on the team, and has to be
// readable by a player that has no Editor at all.
// Resources is the one folder a build always carries
// and a runtime can always read.
//
// Why nothing here is written into scenes
// ---------------------------------------
// A scene that stores "this label uses the generated
// asset" stores a reference to an object that is
// rebuilt on every domain reload and never saved to
// disk - so the reference is null the next time the
// scene opens, and the label is back to whatever it
// was. The settings live here, the application happens
// at load, and the scene file is never modified. That
// is also why a scene reload cannot lose it: there was
// never anything in the scene to lose.
// ==========================================
using System.Collections.Generic;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// Project-wide Unity DirectTMP settings: which font file every
    /// TextMeshPro label should use, and how far the override reaches. Lives
    /// at <c>Assets/Resources/UnityDirectTMPSettings.asset</c> so a player
    /// build can read it too.
    /// </summary>
    public sealed class DirectGlobalFontConfig : ScriptableObject
    {
        [Tooltip("Turn the whole project-wide override on or off in one place.")]
        [SerializeField] private bool active = true;

        [Tooltip("The font file itself — a .ttf or .otf imported into this project. Every TextMeshPro label uses it, with no component added to anything.")]
        [SerializeField] private Font fontFile;

        [Tooltip("More fonts, tried in order after the one above. For every character the first font that actually has it supplies that glyph — so one project can mix Persian, Japanese and emoji from three different files.")]
        [SerializeField] private List<Font> moreFonts = new List<Font>();

        // ==========================================
        // How far the override reaches
        //
        // Two independent mechanisms, because they fail
        // in opposite directions and most projects want
        // both:
        //
        //   Replace label fonts is the strong one. Every
        //   label is drawn in YOUR font, which is what
        //   somebody means when they say "use this font".
        //   It cannot reach a label that does not exist
        //   yet - a prefab instantiated in the tenth
        //   minute of play carries its own font asset.
        //
        //   Global fallback is the safety net. It hands
        //   the font to TextMeshPro's own project-wide
        //   fallback list, so EVERY label - including
        //   ones nothing ever swept, including
        //   TMP_Dropdown's captions and a label built by
        //   a package you did not write - resolves any
        //   character its own font lacks out of yours.
        //   It never changes how anything looks; it only
        //   stops tofu.
        // ==========================================
        [Tooltip("Draw every TextMeshPro label in this font, replacing whatever font asset it was built with. Applied at load; nothing is written into your scenes.")]
        [SerializeField] private bool replaceLabelFonts = true;

        [Tooltip("Also register this font in TextMeshPro's own project-wide fallback list, so any label anywhere resolves characters its font lacks from yours. This is what stops □□□ in labels nothing swept.")]
        [SerializeField] private bool globalFallback = true;

        [Tooltip("Also join Arabic-script letters and put right-to-left words in reading order, on every label, with no component added.")]
        [SerializeField] private bool shapeRightToLeft = true;

        [Tooltip("Which way paragraphs run. Auto reads it from the text itself; force Right To Left when a Persian line can open with a Latin word or a number.")]
        [SerializeField] private DirectTextDirection direction = DirectTextDirection.Auto;

        [Tooltip("Flip a left-aligned label to right-aligned while it is showing right-to-left text. Centred and justified labels are never touched.")]
        [SerializeField] private bool alignToDirection = true;

        [Tooltip("Re-break wrapped paragraphs so their lines read top to bottom. Costs one extra layout per text change.")]
        [SerializeField] private bool fixWrappedLines = true;

        [Tooltip("Apply the override while you are editing, so the Scene and Game views show what the build will show. Off means it applies in Play mode and in builds only.")]
        [SerializeField] private bool applyInEditMode = true;

        // ==========================================
        // Watching for new labels
        //
        // A label instantiated from a prefab in the tenth
        // minute of play carries its own font asset, and
        // no sweep that already ran can know about it.
        // With the global fallback on it can never show
        // □□□ either way - so this is about the DESIGN
        // being right, not about tofu.
        //
        // It costs one FindObjectsOfType a second. That
        // is small and it is not nothing, so it is a
        // switch: turn it off and call
        // DirectTMP.RefreshGlobalFont() after you
        // instantiate instead.
        // ==========================================
        [Tooltip("Re-sweep about once a second so labels instantiated at runtime pick the font up too. Off is cheaper — call DirectTMP.RefreshGlobalFont() yourself after instantiating.")]
        [SerializeField] private bool watchForNewLabels = true;

        [Tooltip("Also draw Unity's OWN interface — menu bar, Hierarchy, Inspector, Project window, Console — in this font. Editor only; nothing is written to your Unity installation and quitting Unity always restores it.")]
        [SerializeField] private bool restyleEditorChrome = false;

        [Tooltip("Use custom rasterization settings instead of the package defaults.")]
        [SerializeField] private bool overrideSettings = false;

        [SerializeField] private DirectFontSettings settings = DirectFontSettings.Default;

        // ==========================================
        // Read-only accessors
        // ==========================================
        /// <summary>Is the project-wide override switched on, with a font to apply?</summary>
        public bool IsActive => active && fontFile != null;

        /// <summary>The master switch, independent of whether a font has been chosen.</summary>
        public bool Active { get => active; set => active = value; }

        /// <summary>The font file every label should be drawn from.</summary>
        public Font FontFile { get => fontFile; set => fontFile = value; }

        /// <summary>Extra fonts, in per-character search order, tried after <see cref="FontFile"/>.</summary>
        public List<Font> MoreFonts => moreFonts;

        /// <summary>Replace each label's own font asset, rather than only filling its gaps.</summary>
        public bool ReplaceLabelFonts { get => replaceLabelFonts; set => replaceLabelFonts = value; }

        /// <summary>Register the font in TextMeshPro's project-wide fallback list.</summary>
        public bool GlobalFallback { get => globalFallback; set => globalFallback = value; }

        /// <summary>Join Arabic-script letters and reorder right-to-left text on every label.</summary>
        public bool ShapeRightToLeft { get => shapeRightToLeft; set => shapeRightToLeft = value; }

        /// <summary>Paragraph direction handed to every label the override shapes.</summary>
        public DirectTextDirection Direction { get => direction; set => direction = value; }

        /// <summary>Flip left-aligned labels to right-aligned while they show right-to-left text.</summary>
        public bool AlignToDirection { get => alignToDirection; set => alignToDirection = value; }

        /// <summary>Re-break wrapped right-to-left paragraphs so their lines read top to bottom.</summary>
        public bool FixWrappedLines { get => fixWrappedLines; set => fixWrappedLines = value; }

        /// <summary>Apply the override in the Editor as well as in Play mode and builds.</summary>
        public bool ApplyInEditMode { get => applyInEditMode; set => applyInEditMode = value; }

        /// <summary>Re-sweep periodically so labels created at runtime are covered too.</summary>
        public bool WatchForNewLabels { get => watchForNewLabels; set => watchForNewLabels = value; }

        /// <summary>Also restyle Unity's own IMGUI chrome with this font (Editor only).</summary>
        public bool RestyleEditorChrome { get => restyleEditorChrome; set => restyleEditorChrome = value; }

        /// <summary>Whether the rasterization settings below are used at all.</summary>
        public bool OverrideSettings { get => overrideSettings; set => overrideSettings = value; }

        /// <summary>The rasterization settings, custom or package default.</summary>
        public DirectFontSettings ResolvedSettings => overrideSettings ? settings.Clamped() : DirectFontSettings.Default;

        /// <summary>The custom rasterization settings, whether or not they are in use.</summary>
        public DirectFontSettings Settings { get => settings; set => settings = value; }
    }
}
