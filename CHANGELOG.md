# Changelog

All notable changes to **Unity DirectTMP** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Planned, in roughly the order they're likely to land. Tracked against the
roadmap in the [README](README.md#-roadmap).

### Planned
- Ordered fallback chains authored as a reusable ScriptableObject with a
  dedicated reorderable editor (the runtime `DirectFontFallbackChain` already
  ships; the richer editor UX is next).
- `.ttc` collection support with a face-index picker.
- Use the player's own installed system fonts as a first-class source in the
  Inspector (the runtime `SetSystemFont` API already exists).
- Arabic-script shaping (Persian / Arabic / Urdu joining forms) from the font's
  own OpenType tables.
- Colour & emoji fonts (COLR / CBDT).
- Variable font axes — weight, width, slant.

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
- A **Unity DirectTMP** top-level menu, matching the README exactly:
  Convert (Selected Objects / Current Scene / Whole Project), Fallback Chain…,
  Font Cache (Show Cache Folder / Clear Cache), Settings…, About.
- Batch converter — turns existing TextMeshPro labels that use a baked font
  asset into DirectTMP labels pointing at that asset's source font file, across
  the selection, the open scene, or every Scene and Prefab in the project (with
  a confirmation dialog and progress reporting).
- Custom `DirectFont` Inspector with a live read-out of the built font (family,
  style, glyphs rasterized so far) and Rebuild / Clear-cache actions.
- Project Settings page (**Project Settings ▸ Unity DirectTMP**) for the default
  sampling size, atlas size, padding and render mode.
- Fallback Chains window and an About window.
- Project-window context actions: Convert TMP In Folder, New Fallback Chain.

**Package**
- Unity Package Manager layout with `Runtime/`, `Editor/`, `Tests/` and
  `Samples~/` assemblies, assembly definitions, and full `.meta` coverage.
- EditMode tests for the cache-key equality/clamping, the fallback ordering, and
  the path/hash helpers.
- **Multilingual Demo** sample — one scene, one font, twelve languages.
- MIT license, this changelog, and a trilingual (English / 日本語 / فارسی) README.

### Requirements
- Unity **2021.3 LTS** or newer (Unity 6.x supported).
- **TextMeshPro 3.0+** (the version bundled with those Unity releases).
- No other third-party dependencies.

### Notes
- Rasterizing a font file from a raw path or `byte[]` at runtime uses
  TextMeshPro's public path/byte factory on Unity 2022.2+ (TMP 3.2+). On Unity
  2021.3 the package falls back to the low-level FontEngine; for the most
  predictable results on 2021.3, reference the `.ttf` as an imported `Font`
  asset (which is the same file, just referenced as a Unity `Font`).
- Arabic-script text renders glyph-by-glyph; contextual joining-form shaping is
  on the roadmap.

[Unreleased]: https://github.com/AmirCollider/UnityDirectTMP/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/AmirCollider/UnityDirectTMP/releases/tag/v0.1.0
