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
- Arabic-script shaping from the font's own OpenType `GSUB` tables, so a font
  with contextual alternates or a full ligature set uses them. 1.1.0 shapes
  via the Unicode presentation forms, and falls back to the plain letters for
  a font that carries none.
- Colour & emoji fonts (COLR / CBDT).
- Variable font axes — weight, width, slant.

## [1.1.0] - 2026-07-30

The glyphs were there and the words were still wrong.

1.0.1 taught this package's own Editor windows to draw Persian. It did not
teach your labels to, and the bug report that followed is the one this
release exists for: a `Text (TMP)` with a Direct Font on it and a real font
file assigned. No more boxes — and Persian arriving as a row of disconnected
letters running the wrong way.

That is not a font problem, and adding a better font does not fix it.
TextMeshPro hands each codepoint to the font in the order it is stored;
`isRightToLeftText` reverses that order without joining anything. So a label
could have every glyph it needs and still be unreadable, and the package
answered "your font is missing characters" to a question nobody was asking.

### Added
- **`DirectText`** — the component that makes a TMP label read. Drop it next
  to any `TextMeshProUGUI` / `TextMeshPro` and Persian, Arabic, Urdu and
  Hebrew are joined, reordered and aligned. `label.text` still holds the
  string you assigned, in logical order, because the work happens in
  TextMeshPro's own `textPreprocessor` — which also means anything that writes
  to a label is covered without knowing the component exists: a `TMP_Dropdown`
  filling its caption, a localisation package, a coroutine typing one
  character at a time.
- **Wrapped paragraphs read top to bottom.** Reordered text cannot be wrapped:
  reordering makes the LAST word of a paragraph the leftmost thing on the
  line, so a renderer breaking that line into three produces three lines that
  read upwards. `DirectText` lays the text out twice — once shaped and still
  in reading order, so TextMeshPro reports where the lines fall, then once
  more with each of those lines reordered on its own. Every wrapped
  right-to-left label in Unity has this bug and it is invisible until a
  sentence gets long enough to fold.
- **`DirectRichText`** — shaping and reordering over text with markup in it.
  `<b>` and `<color>` survive intact and move with the words they wrap, so the
  opening tag lands at the left end of its span and the closing one at the
  right, swapping over automatically when the span turns around. Letters join
  ACROSS a tag, `<br>` is treated as the line break it is, and a `<` that is
  not a tag is left alone.
- **Fonts with no presentation forms are no longer boxes.** The forms are a
  legacy Unicode block, and the faces a Persian designer actually reaches for
  — Vazirmatn, Noto Sans Arabic — carry the plain letters plus OpenType rules
  and nothing at U+FE70. Shaping into a form such a font has no glyph for
  turns readable-if-unjoined text into tofu, so every form is now checked
  against the font first and falls back to the isolated form, then to the
  letter itself.
- `DirectFont` gained **Shape Text** (on by default), which adds the
  `DirectText` component for you — because "I added Direct Font and gave it a
  font" is exactly the report above, and needing to know about a second
  component was the package's failure, not the user's.
- `DirectTMP.ShapeAll()` for an existing scene, and `DirectTMP.Prepare(text)`
  for text you draw yourself.
- Index maps on the two lower layers: `DirectArabicShaper.Shape` and
  `DirectBidi.Reorder` can now report where every character came from and
  went, which is what lets a tag be put back where it belongs.
- `DirectRichTextTests` — 31 tests over markup, wrapping, the index maps and
  the missing-form fallback.

### Fixed
- **The language dropdown showed Persian backwards on its button.** A popup is
  two text systems, not one: the list that drops down is drawn by the
  operating system, which shapes and reorders on its own, while the button
  that opens it is IMGUI, which does neither. 1.0.0 prepared both, so the list
  read backwards; 1.0.1 prepared neither, so the button did. `DirectTMPPopup`
  now gives each half what it needs, and the Font Catalog's script and sort
  filters use it too.
- A `TMP_InputField`'s own text component is deliberately left alone —
  reordering it would put the caret in the wrong place. Its placeholder is
  shaped as normal.

## [1.0.1] - 2026-07-30

A text tool has to get its own text right.

1.0 shipped with a trilingual editor UI, and the Persian third of it was
unreadable: every label drawn unjoined and in reverse. The first thing a new
user saw was the welcome screen, and on that screen a package that exists to
fix broken text was displaying broken text.

The cause was not the font and not the translation. Unity's IMGUI — which
draws every Editor window, this package's included — does no Arabic shaping
and no bidirectional reordering. It hands each codepoint to the font in the
order it is stored, which for Persian is neither the shape nor the order a
reader needs. Nothing in Unity does this, and nothing in TextMeshPro does it
either.

So the package now does it.

### Added
- **`DirectArabicShaper`** — joins Arabic-script letters up, choosing the
  isolated / initial / medial / final form each letter's neighbours call for.
  Covers the Arabic block plus the letters Persian and Urdu add on top of it
  (`پ گ چ ژ ک ی ...`), the four lam-alef ligatures, the harakat that must not
  break a join, and ZWNJ — which Persian needs for `می‌شه` and `فونت‌ها`, and
  which a shaper written for Arabic alone always misses.
- **`DirectBidi`** — the Unicode Bidirectional Algorithm (UAX #9) over one
  line at a time: Latin runs inside Persian sentences still read forwards,
  numbers stay ascending, trailing punctuation lands on the correct end, and
  brackets are mirrored.
- **`DirectDisplayText`** — the two in the order they have to happen, plus the
  line breaking a wrapped right-to-left paragraph needs. Reordering a
  paragraph and then letting the renderer wrap it produces lines that read
  bottom to top, so lines are broken first, in reading order.
- All three are public, `Runtime`, Unity-free and unit-tested, so a project
  can use them on its own labels — TextMeshPro's `isRightToLeftText` reverses
  without joining, and this is the missing half.
- `DirectTextDisplayTests` — 36 tests over shaping, reordering, wrapping and
  the two text systems the editor draws with.

### Fixed
- **Every Persian string in every window** now renders joined and in reading
  order: the welcome screen, the Font Catalog, the Editor Font window, the
  Health Check, the About box, the Project Settings page, the DirectFont
  Inspector, the coverage badges, the tooltips and the confirmation dialogs.
- **Font names, asset paths and preview text** are prepared too, not just the
  tool's own strings. A font family named in Persian, a folder called
  `فونت‌ها`, and whatever you type into the catalog's shared preview field all
  render correctly — which for a font browser is the entire job.
- **Long paragraphs wrap in the right direction.** `EditorGUILayout.HelpBox`
  cannot be told where to break a line, so the explanations that used it are
  drawn by the package instead.
- **Dropdown lists, the menu bar, right-click menus and modal dialogs are
  left alone.** Unity's editor draws text two different ways: IMGUI draws the
  inside of a window and does no shaping and no reordering, but a Popup's item
  list, a `[MenuItem]` path and an `EditorUtility.DisplayDialog` are handed to
  the operating system, which has a full text stack and does both itself.
  Preparing those does the job twice and undoes it — the open dropdown read
  backwards while the button that opened it read correctly. Every OS-drawn
  surface now goes through `DirectTMPText.Native`, so the assumption is one
  method and one grep.
- **The Fallback Chain window** was the one screen that had never been
  translated. It speaks all three languages now.

### Changed
- Windows mirror their layout when the interface language is right-to-left:
  the header's mascot, accent stripe and version swap ends, cards put their
  mark on the leading side, and paragraphs align to the side the eye starts
  from.
- `DirectTMPText.L()`, `Word()` and `C()` return display-ready text; `Raw()`,
  `WordRaw()` and `For()` return it as stored, for composing. `F()` formats
  and prepares in one call, in that order — reordering a format string first
  would move `{0}` to where the number belongs on screen.

### Repository
- Added `.gitignore` and `.gitattributes`. Both were required by
  `validate_package.py` and neither existed, so CI had been failing two checks
  on every push. `.meta` files are marked `merge=ours`, because a merge
  conflict inside one makes Unity mint a fresh GUID and quietly detach every
  reference to that asset.

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
