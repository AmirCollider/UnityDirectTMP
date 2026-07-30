# Contributing to Unity DirectTMP

Issues and pull requests are welcome. This file is short on purpose — it only
covers the things that are specific to this package and that a reviewer would
otherwise have to say out loud on every pull request.

## Before you open a pull request

```bash
python3 .github/scripts/validate_package.py
```

That runs with no Unity licence and catches the things that are cheap to break
and expensive to notice: a version that drifted between `package.json` and
`DirectTMPConstants.Version`, a script committed without its `.meta`, a
duplicate GUID, a missing required file, a changelog whose newest entry is not
the version being shipped.

Then run the EditMode tests: **Window → General → Test Runner → EditMode → Run
All**.

## Three rules that are easy to break by accident

**Commit the `.meta` file.** Unity resolves every asset by the GUID inside its
`.meta`. A script that ships without one gets a fresh random GUID in every
user's project, and every reference to it rots — which the author never sees,
because their own copy has the original. `validate_package.py` fails on this.

**Every user-facing string goes through `DirectTMPText.L(en, ja, fa)`.** All
three, at the call site. Not a `TODO`, not English three times. The set of
languages is deliberately the three the author can proof-read; a fourth that
nobody can check is worse than an honest three.

**Never let the Editor-font feature apply a font that cannot draw Latin.**
`DirectEditorFontRules.CanDrawEditorChrome` is the only thing standing between a
user and an Editor whose "Revert to Unity Default" menu item is a row of empty
boxes. If you are changing anything in `Editor/EditorFont/`, the tests around
that rule are not optional and the check is not downgradeable to a warning.

## Where things live

| Folder | What belongs there |
|---|---|
| `Runtime/` | Anything a player build needs. No `UnityEditor` references, ever. |
| `Runtime/DirectFontFile.cs` | The sfnt parser. Bounds-check every read; never throw past the public boundary. |
| `Editor/Brand/` | The palette, Inky, the text pack, the shared chrome. |
| `Editor/Catalog/` | The Font Catalog. Model and rules in `DirectFontCatalog.cs`; the window holds no logic worth testing. |
| `Editor/EditorFont/` | Restyling Unity itself. `…Plan.cs` is pure and tested; `DirectEditorFont.cs` is the part that touches live GUI objects. |
| `Tests/Editor/` | EditMode tests. |

The split between a window and its rules is deliberate everywhere: if a piece of
logic is worth a test, it does not live in an `OnGUI`.

## Style

Match the file you are in. In practice that means: `// ====` banner comments
above each region, comments that say *why* rather than *what*, braces on their
own line, `_camelCase` for private instance fields in windows, `s_` for statics
that survive a domain reload.

Comments in this codebase carry the reasoning that is not visible in the code —
why a check exists, what broke once, what the alternative was. If a reviewer
would ask "why is this here?", the answer belongs above it.

## Adding a script to the coverage badges

`DirectFontScripts` is the single source of truth. Add the enum value, its
entry in `All`, its probe characters, its preview sample and its three names —
all in that one file. Anything that adds a script without a label, or a label
without probes, is a badge that renders blank.

Probe characters matter more than they look. The Arabic set includes `پ` and
`گ` specifically because a font with Arabic but without the Persian letters
must report *partial* rather than *full* — that distinction is the most useful
thing the catalog says.

## Reporting a bug

Open **Unity DirectTMP ▸ Health Check…** and press **Copy report**, then paste
it into the issue. It carries the Unity version, the TextMeshPro version,
whether TMP Essential Resources are imported, how many menu items registered,
and the Editor-font state — which is most of the first round of questions,
answered.

For a security issue, see [SECURITY.md](SECURITY.md) instead.
