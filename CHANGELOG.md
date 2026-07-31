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
- The rest of `GSUB`: contextual alternates and a font's full ligature set.
  1.2.0 reads the joining features — `isol` / `fina` / `init` / `medi` — which
  is what makes a letter connect; the remainder is what makes it beautiful.
- Colour & emoji fonts (COLR / CBDT).
- Variable font axes — weight, width, slant.

## [1.2.0] - 2026-07-30

The font was never missing the shapes. It was missing the codepoints.

1.1.0 and 1.1.1 both took the Unicode presentation forms as the source of
truth: to draw a joined پ you ask the font for U+FB58, and if it has no such
character there is nothing more to be done. Half of that is right — the forms
are the only way to reach a joined letter through a cmap, and they are why
this package can shape Persian at all without a text engine.

The other half was wrong. Those blocks are split: پ چ ژ ک گ ی shape into
U+FB50–FBFF and the rest of the alphabet into U+FE70–FEFF, and a font may
carry one and not the other. Segoe UI does exactly that, which is why the bug
was on every Windows machine. But **Segoe UI can draw a joined peh** — every
font that sets Arabic can, through its OpenType `GSUB` table, which is what a
real shaping engine uses and what the presentation blocks were always standing
in for. The glyphs were there the whole time. Only the address was missing.

So 1.1.1 gave up one letter too early, and `پروژه` came out as a peh standing
alone next to `روژه`. That is the screenshot this release exists for.

### Fixed
- **A font's own joining rules are read, and the missing forms are built from
  them.** Before a label is shaped, `DirectFontForms` asks the font which
  glyph it would use for each letter in each joining position, rasterizes
  those glyphs, and registers them in the TextMeshPro font asset under the
  presentation codepoints the shaper emits. The shaper is unchanged; U+FB58 is
  simply there now. On a font carrying Forms-B and no Forms-A this recovers
  every Persian letter — پ چ ژ ک گ ی and thirty more — and `پروژه`, `پوشه‌ها`
  and `اسکریپت` join in full.
- **ی is no longer stood in for when it does not need to be.** The alef-maksura
  substitution added in 1.1.1 is now the third answer, not the second: the
  font's real farsi-yeh forms are used whenever `GSUB` can name them, so the
  letter is drawn as the font's designer drew it rather than as its nearest
  Arabic relative.
- **The warning says what was tried.** Reaching the "this font cannot join
  these letters" message now means both routes came up empty, and the message
  names which one failed and why — in English, 日本語 and فارسی, in the console
  and in the Inspector.

- **Preserving a label's material no longer renders its strokes at the wrong
  weight.** `Preserve Material` copies the outline, underlay and gradient from
  the old material onto the rebuilt one, and it was copying the shader's scale
  ratios with them. TextMeshPro derives those from the atlas's gradient scale,
  padding and sampling size, and the shader multiplies face dilation, weight
  and outline width by them — so a label that had been showing another font,
  or the same font rasterized differently, drew every stroke too thin. The
  strokes that disappear first are the thinnest in the label, which in Persian
  are the ones that JOIN one letter to the next: the word does not come apart,
  its joins are drawn too faint to see, and the two look identical.
- **A font that cannot set Persian now says so.** The coverage badges answer
  "does this font contain Arabic letters", and a font can pass that and still
  be useless: Arial Unicode MS carries every Persian letter, has no joined
  shapes for any of them and no OpenType rules to derive some, and sets
  `پروژه` as five separate letters no matter what any shaper does. Nothing
  anywhere said so, so the tool looked broken and the font looked fine.
  `DirectFontFile` now reports `ArabicJoining` — whether a font joins through
  the presentation forms, through OpenType, or not at all — and names the
  letters it cannot join. The Direct Font Inspector shows the verdict under
  the badges, in all three languages.

### Added
- **More Fonts** on `DirectFont` — an inline, ordered list of extra fonts, so
  one label can set `پروژه‌ی 敵スポーナー 🎮` from three files. For every
  character the first font in the list that has it supplies that glyph. It
  needed a ScriptableObject before, which is the right tool for fonts you line
  up once and reuse and the wrong one for "this label also needs emoji". Both
  work, and both feed the same table: this list first, then the chain asset.
  `SetMoreFonts(IEnumerable<Font>)` does it from code.
- `DirectFontFileInfo.ArabicJoining`, `.UnjoinableLetters` and
  `.JoinsArabicScript`, with `DirectJoiningSupport` — the verdict above.
- `DirectFontGsub` — reads `GSUB`'s `isol` / `fina` / `init` / `medi` features
  for the Arabic script: lookup type 1 in both single-substitution formats,
  and type 7 extension lookups. Pure C#, bytes in and facts out, bounds-checked
  at every read; a corrupt or hostile font produces an empty answer, never an
  exception. Verified glyph-for-glyph against the reference implementation on
  DejaVu Sans, FreeSerif, FreeSans and Unifont.
- `DirectFontForms.TopUp(TMP_FontAsset)` — the top-up itself, and
  `ReportFor(...)` to ask what it did. Every step of it is allowed to fail: any
  failure leaves the font asset untouched and the shaper falls back to exactly
  the 1.1.1 behaviour, so the worst outcome of this release is the previous one.
- `DirectArabicShaper.JoiningLetters` and `TryGetForms(...)`, so the alphabet is
  stated once rather than in two files that can drift apart.
- Sixteen tests in `DirectFontGsubTests`, over both substitution formats,
  coverage ordering, extension lookups, a font whose joining rules belong to
  another script, and every truncation and single-byte corruption of a font
  that was valid. `SyntheticFont` can now build a font with a `GSUB` table.

### Notes
- Reading `GSUB` needs the font's bytes. Those are available for a font loaded
  from a path, from a `byte[]`, or from a system font, and — in the Editor —
  for any imported font asset. A player build handed a Unity `Font` asset has
  no route to them, and falls back to the presentation forms, which is what
  every version before this one used.

## [1.1.1] - 2026-07-30

The letters Persian adds to the Arabic alphabet are in a different Unicode
block, and fonts do not treat the two alike.

پ چ ژ ک گ ی shape into U+FB50–FBFF. Every other letter — ر و ه س ت ن and the
rest — shapes into U+FE70–FEFF. A font can carry the second block in full and
none of the first, and Segoe UI, which is on every Windows machine, does
exactly that. 1.1.0 asked the font for each form and fell back to the plain
letter when it was missing, which was right as far as it went: no boxes.

But it substituted the letter without telling its NEIGHBOURS, and a neighbour
that still thinks it is joined draws a connecting stroke into a letter that
has none. So `یونیتی` came out as a plain ی, a waw with a tail reaching
towards nothing, a joined ن, another plain ی — six letters and four loose
strokes. Every Persian word with a پ or a ی in it looked shattered, and the
letter that got the blame was, again, ی.

### Fixed
- **A letter the font cannot join now joins nothing.** The joining rules ask
  the font first, so the letter before a plain پ takes its isolated form
  instead of reaching for a connection that will not be there. Unjoined in
  one place, rather than broken in three.
- **ک and ی are stood in for.** Both have shapes in the Arabic block that are
  the same drawing, not merely a similar letter: keheh initial and medial are
  kaf initial and medial, and Persian's dotless isolated and final yeh is
  exactly alef maksura. So on a font like Segoe UI, `یونیتی` and `کاربر` now
  join in full — and a font that has the real Persian forms still gets them.
  پ چ ژ گ have no such stand-in (a beh is not a peh) and stay as they are.
- **The font says so out loud.** `DirectText` names the letters a font cannot
  join, once per font, in the console and in its Inspector, in all three
  languages — because after everything else is right, this is the one thing
  left that still looks like a bug and is not one.

### Added
- `DirectArabicShaper.CanJoin(letter, hasGlyph)` — ask a font whether it can
  set a letter joined up, before blaming the shaper.
- Six more tests in `DirectRichTextTests` over the missing block, the
  stand-ins, and the neighbour rule.

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
