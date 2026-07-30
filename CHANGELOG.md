# Changelog

All notable changes to **Unity DirectTMP** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Planned, in roughly the order they're likely to land. Tracked against the
roadmap in the [README](README.md#-roadmap).

### Planned
- `.ttc` / `.otc` collection support with a face-index picker. The catalog
  already reports the face count and flags that only face 0 is used.
- Arabic-script shaping (Persian / Arabic / Urdu joining forms) from the font's
  own OpenType tables.
- Colour & emoji fonts (COLR / CBDT).
- Variable font axes — weight, width, slant.

## [1.0.0] - 2026-07-30

The rebuild. 0.1.0 was a working component with a menu; this is a tool.

Three things drove it: the package had no identity of its own, it could not
answer the question people install it to answer ("which of my fonts can set
this language?"), and it changed the text in your *game* while leaving the
Unity Editor itself full of empty boxes.

### Added

**Restyle the Unity Editor itself**
- **Editor Font** — point Unity's own interface (menu bar, Hierarchy,
  Inspector, Project window, Console) at any font in the project or installed
  on the machine. A GameObject named `敵スポーナー` or a folder named
  `فونت‌ها` renders as its name instead of as `□□□□□`.
- Nothing is written to the Unity installation and no built-in asset is edited
  on disk — the change lives in memory for the session, so quitting Unity is a
  complete, guaranteed undo.
- A font that cannot draw basic Latin letters, digits and punctuation is
  **refused rather than applied**, because otherwise the menu item that undoes
  the change would itself be unreadable. The refusal lists exactly which
  characters are missing.
- A live preview at the real size, drawn only in the scripts the font actually
  covers, with a size adjustment from −4 to +10 points.
- Detects a Font asset imported with a baked character set and offers to switch
  it to Dynamic in one click.
- `Assets ▸ Unity DirectTMP ▸ Use This Font For The Editor` on any font file.

**Font Catalog**
- Every font in the project and every font installed on the machine in one
  list, with the family and style the foundry wrote into the file, the real
  glyph count, the file size, and per-script coverage.
- Coverage has three states, not two: full, **partial**, and none. Partial is
  the answer for a font that has Arabic but not `پ گ چ ژ` — which cannot set
  Persian, and which a boolean "supports Arabic" flag reports as fine.
- One shared preview field: type your actual UI string once and every font in
  the list renders it at the same time.
- Search across family, style, file name and path; filter by required script;
  sort by name, glyph count, scripts covered or file size.
- Row actions: use for the Editor, apply to the current selection, reveal the
  file.
- Rows outside the viewport are not drawn, so a three-hundred-font list from a
  Windows install still scrolls smoothly.

**Reading font files directly**
- `DirectFontFile` — a small, defensive sfnt reader for `name`, `maxp`, `head`
  and `cmap`. It answers "what is this font called, how many glyphs does it
  have, and does it contain Persian?" without building a single atlas, which is
  what makes a catalog of forty fonts open instantly.
- Handles cmap formats 0, 4, 6 and 12, prefers the full-repertoire subtable
  when a font ships several, resolves format 4 per character (so a codepoint
  mapped to glyph 0 is correctly reported as absent), and reads `.ttc`
  collection headers.
- Every read is bounds-checked; a truncated font or an LFS pointer committed as
  a `.ttf` produces a row that says so instead of an exception.
- `DirectFontScripts` — Unicode ranges, probe characters and preview samples for
  twelve writing systems, plus the pure functions that turn "these codepoints
  exist" into "this font covers Japanese".

**Health Check**
- `Unity DirectTMP ▸ Health Check…` reports what the package can see: the
  TextMeshPro assembly and whether its Essential Resources are imported, how
  many menu items registered, how many project fonts are imported with a baked
  character set, the Editor-font state and the cache state.
- One-click fixes where a fix exists, and a **Copy report** button, because the
  second half of "it doesn't work" is a bug report that has to travel.
- Written specifically to explain the most common report this package gets: a
  missing top-level menu is almost always a compile error, which drops every
  `[MenuItem]` in the assembly at once.

**Brand**
- **Inky**, the mascot: a teal inkwell with a brass nib and three glyph bubbles
  rising out of it (`A`, `あ`, `س`). Drawn in code and rasterized at the
  editor's own DPI, so there is no image to import, compress or blur.
- The **Inkwell** palette — teal ink, deep ink, brass, cream paper, blush,
  night — shared by the editor windows, the product page and the README badges.
- A shared header, card, badge, divider and footer across every window.

**Trilingual editor UI**
- English, 日本語 and فارسی throughout, switchable from
  `Unity DirectTMP ▸ Language` (with a checkmark on the active one) or from
  Project Settings. Guessed from the Editor's own locale on first run.

**Everything else**
- A welcome screen, shown once per version.
- A cross-promotion panel for the sibling tool, one line, dismissible forever
  in one click, absent from the Inspector and from first run.
- `DirectTMPConverter.ApplyFontToSelection` — the catalog's "apply to what I
  have selected", as a single undo step for the whole selection.
- `DirectTMPLog` — one console prefix for everything the package says, and a
  quiet-console setting that never silences errors.
- `Window ▸ Unity DirectTMP` as a second, more familiar door to the three
  windows.
- `CONTRIBUTING.md`, `SECURITY.md`, `.gitignore`, `.gitattributes`, a pull
  request template, a CI workflow, and `validate_package.py` — the package
  check that runs with no Unity licence.

### Changed
- **Every field in the Inspector and on the Settings page now has a
  description**, in all three languages, saying what it does *and* what it
  costs. "Override Settings" now explains that a label with its own settings
  builds its own atlas.
- The Inspector shows what the font file contains — family, glyph count,
  character count, size, coverage badges — above what has been rasterized from
  it so far.
- `package.json` declares `com.unity.ugui`, which is what carries TextMeshPro
  on Unity 6. A project that resolved the package but not TMP was the most
  likely cause of the whole menu being absent.
- The menu tree gained Font Catalog, Editor Font, Language, Health Check and
  Welcome, and is now asserted against the README by a test.
- Console output routes through `DirectTMPLog` instead of ad-hoc
  `Debug.LogWarning` calls with a hand-typed prefix.
- The Settings page groups rasterization, the Editor font, housekeeping and
  about, and is findable by searching Project Settings for "font", "persian",
  "tofu" or "editor font".

### Fixed
- The Project-window context menu offered no way to act on a font file itself.
  It now has *Use This Font For The Editor* and *Inspect This Font*.
- Clearing the cache from the Settings page did not delete the on-disk spool
  files the way the menu item did.

### Requirements
- Unity **2021.3 LTS** or newer (Unity 6.x supported).
- **TextMeshPro 3.0+** (bundled with those Unity releases).
- No other third-party dependencies.

### Notes
- Rasterizing a font file from a raw path or `byte[]` at runtime uses
  TextMeshPro's public path/byte factory on Unity 2022.2+ (TMP 3.2+). On Unity
  2021.3 the package falls back to the low-level FontEngine; for the most
  predictable results on 2021.3, reference the `.ttf` as an imported `Font`
  asset (which is the same file, just referenced as a Unity `Font`).
- Arabic-script text renders glyph-by-glyph; contextual joining-form shaping is
  on the roadmap.
- The Editor Font override is per machine and per project, stored in
  `EditorPrefs`. It is never committed and never affects anybody else on the
  team.

## [0.1.0] - 2026-07-23

The first public release. Everything you need to hand TextMeshPro a font file
directly and have every language the font supports just work.

### Added

**Runtime**
- `DirectFont` — the component you drop next to a `TextMeshProUGUI` /
  `TextMeshPro`. Point it at a `.ttf`/`.otf` and it builds a dynamic font asset
  from the file and applies it to the label. Runs in edit mode too
  (`[ExecuteAlways]`), so the Scene view previews live while you design.
- `DirectTMP` — the small static API: `Load`, `LoadFromFile`, `LoadFromBytes`,
  `LoadSystemFont`, `SetGlobalFont`, `Preload`, `ClearCache`.
- Font sources: an imported `Font` asset, a file path (absolute, or relative to
  `StreamingAssets` / `persistentDataPath`), a raw `byte[]` (e.g. a download),
  or one of the operating system's installed fonts by family name.
- `DirectFontCache` — one dynamic atlas per unique *(source + settings)*, shared
  process-wide, so a screen of 200 labels that all use one font builds exactly
  one atlas. Clearable on scene change to reclaim glyph memory.
- `DirectFontFallbackChain` — a reusable, ordered list of fonts; for every
  character the first font in the chain that actually has the glyph wins.
- `DirectFontSettings` — per-label or project-wide control over sampling point
  size, atlas size, padding and render mode, with safe-range clamping.
- Material preservation — swapping a label's font keeps its outline, underlay,
  gradient and softness instead of resetting to the plain font material.
- A version-tolerant TextMeshPro bridge: uses TMP's public dynamic-font factory
  on every supported Unity version, preferring the path/byte factory where the
  editor provides it (Unity 2022.2+ / TMP 3.2+) and falling back to the
  low-level FontEngine builder otherwise.

**Editor**
- A **Unity DirectTMP** top-level menu: Convert (Selected Objects / Current
  Scene / Whole Project), Fallback Chain…, Font Cache (Show Cache Folder /
  Clear Cache), Settings…, About.
- Batch converter — turns existing TextMeshPro labels that use a baked font
  asset into DirectTMP labels pointing at that asset's source font file, across
  the selection, the open scene, or every Scene and Prefab in the project (with
  a confirmation dialog and progress reporting).
- Custom `DirectFont` Inspector with a live read-out of the built font.
- Project Settings page (**Project Settings ▸ Unity DirectTMP**).
- Fallback Chains window and an About window.
- Project-window context actions: Convert TMP In Folder, New Fallback Chain.

**Package**
- Unity Package Manager layout with `Runtime/`, `Editor/`, `Tests/` and
  `Samples~/` assemblies, assembly definitions, and full `.meta` coverage.
- EditMode tests for the cache-key equality/clamping, the fallback ordering, and
  the path/hash helpers.
- **Multilingual Demo** sample — one scene, one font, twelve languages.
- MIT license, this changelog, and a trilingual (English / 日本語 / فارسی) README.

[Unreleased]: https://github.com/AmirCollider/UnityDirectTMP/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/AmirCollider/UnityDirectTMP/releases/tag/v1.0.0
[0.1.0]: https://github.com/AmirCollider/UnityDirectTMP/releases/tag/v0.1.0
