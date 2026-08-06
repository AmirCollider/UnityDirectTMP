# Contributing to Unity DirectTMP

The package is deliberately small. Please keep it that way — the version before
2.0.0 grew to eleven windows and 22,000 lines while the one thing it existed to
do was broken, and that is the mistake this codebase is now organised against.

## Where things live

| Path | What it is |
|---|---|
| `Runtime/DirectJoining.cs` | Unicode joining classes and the presentation-form table. Pure data and pure functions. **Never looks at a font.** |
| `Runtime/DirectShaper.cs` | Chooses which of the four shapes each letter takes. Pure C#; no Unity types. |
| `Runtime/DirectFontGsub.cs` | Reads a font's `GSUB` — the joining features, and lam-alef ligatures. Bytes in, facts out; every read bounds-checked. |
| `Runtime/DirectFontJoiner.cs` | Makes a shape *available*: resolves the glyph and registers it in the TMP font asset. The only file that touches TextMeshPro internals. |
| `Runtime/DirectBidi.cs` | Reordering for display. Runs **after** shaping, never before. |
| `Runtime/DirectFont.cs` | `.ttf`/`.otf` → dynamic `TMP_FontAsset`, cached. |
| `Runtime/DirectTMP.cs` | The public API. |
| `Runtime/DirectTMPDriver.cs` | Keeps every label pointed at the font, via `ITextPreprocessor`. |
| `Editor/` | One window and one bootstrap. That is the whole editor surface. |

## The two rules that matter

**1. Joining is a property of the script, not of the font.** A letter's joining
class comes from Unicode and nothing else. Deriving it from what a font happens
to contain is exactly the bug that made words come apart at letters that were
perfectly fine — one missing glyph changed the shape of its *neighbour*.

**2. Never ask a font whether it has a shape. Make the shape available.** If the
font's own `GSUB` names a glyph for it, register that glyph. Giving up quietly
and leaving the word unshaped is what shipped for three versions.

## Shaping changes must be checked against HarfBuzz

Anything touching `DirectJoining`, `DirectShaper` or `DirectFontGsub` needs to
be diffed glyph-for-glyph against HarfBuzz before it is believed — including on
a font with **no** presentation codepoints, which is the case every modern
Persian face falls into and the one that was broken.

```bash
pip install fonttools uharfbuzz
```

Build the no-presentation-forms test font by stripping `U+FB50..FDFF` and
`U+FE70..FEFF` out of a font's cmap while leaving its `GSUB` alone, then compare
your shaper's glyph ids against `hb.shape()` over a Persian/Arabic/Urdu corpus.
A word that differs by anything other than a contextual stylistic variant is a
bug.

## Before opening a PR

- `python3 .github/scripts/validate_package.py` passes.
- EditMode tests pass (`Tests/Editor/`).
- Any new file has a `.meta` with a GUID that is unique in the repository.
- `Runtime/` uses no `UnityEditor` type outside a `#if UNITY_EDITOR` guard.
- `CHANGELOG.md` has an entry, and `package.json` has the matching version.

## Reporting a bug

Say which **font file** and which **exact string**. "Persian is broken" cannot
be acted on; "سلام in Vazirmatn-Regular.ttf renders unjoined" can be reproduced
in a minute. Include the Unity and TextMeshPro versions.
