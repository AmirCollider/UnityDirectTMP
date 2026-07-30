#!/usr/bin/env python3
"""
Package validation for Unity DirectTMP.

Runs in CI without a Unity licence, and locally with
`python3 .github/scripts/validate_package.py`.

Each check exists because the thing it looks for is cheap to
break and expensive to notice:

  * version drift - package.json and DirectTMPConstants.Version
    are stamped into the About window, the Settings footer, the
    catalog header and every health report. They can disagree
    silently for releases.
  * missing .meta - a script shipped without one gets a fresh
    random GUID in every user's project, so any reference to it
    rots. Unity regenerates the file locally, so the author
    never sees the problem their users get.
  * required files - LICENSE, README.md, CHANGELOG.md,
    CONTRIBUTING.md, SECURITY.md, .gitignore and .gitattributes.
    The dot-files need their own check precisely because every
    other check here walks what Unity imports, and Unity cannot
    see a dot-file at all.
  * changelog freshness - the NEWEST entry has to be the version
    being shipped. Checking that the version appears anywhere in
    the file stays true forever once it has shipped, so a
    release could go out describing itself with the previous
    version's notes.
  * the menu tree matches the README - a documented menu item
    that does not exist reads to the reader as a broken install.
  * the Editor-font safety rule is still a refusal - the one
    invariant in this package that, if it quietly becomes a
    warning during a refactor, hands somebody an Editor they
    cannot undo the change from.
"""

import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

failures = []
checks_run = 0


def check(name, condition, detail=""):
    global checks_run
    checks_run += 1
    if condition:
        print("  ok    {}".format(name))
    else:
        print("  FAIL  {} {}".format(name, "- " + detail if detail else ""))
        failures.append(name)


def read(path):
    with open(os.path.join(ROOT, path), encoding="utf-8") as handle:
        return handle.read()


# ==========================================
# 1. package.json is valid and complete
# ==========================================
print("package.json")
try:
    package = json.loads(read("package.json"))
    package_ok = True
except Exception as exc:  # noqa: BLE001
    package = {}
    package_ok = False
    check("package.json parses", False, str(exc))

if package_ok:
    check("package.json parses", True)
    for field in ("name", "version", "displayName", "unity", "license", "description"):
        check("has '{}'".format(field), field in package and package[field])

    check(
        "name is reverse-DNS",
        re.match(r"^[a-z0-9]+(\.[a-z0-9-]+)+$", package.get("name", "")) is not None,
        package.get("name", ""),
    )

    # TextMeshPro lives inside com.unity.ugui from Unity 2023.2 / Unity 6
    # onward. Declaring it is what stops a project resolving this package but
    # not TMP - which kills the editor assembly's compile, which removes the
    # whole top-level menu, which is the single most common bug report this
    # package has ever had.
    check(
        "declares the com.unity.ugui dependency (carries TextMeshPro)",
        "com.unity.ugui" in package.get("dependencies", {}),
    )

# ==========================================
# 2. Version is stamped identically everywhere
# ==========================================
print("\nversion consistency")
constants = read("Runtime/DirectTMPConstants.cs")
match = re.search(r'public const string Version\s*=\s*"([^"]+)"', constants)
check("DirectTMPConstants.Version is present", match is not None)

if match and package_ok:
    cs_version = match.group(1)
    pkg_version = package.get("version", "")
    check(
        "package.json {} == DirectTMPConstants {}".format(pkg_version, cs_version),
        cs_version == pkg_version,
    )

    changelog = read("CHANGELOG.md")
    check(
        "CHANGELOG documents [{}]".format(pkg_version),
        "[{}]".format(pkg_version) in changelog,
    )

    # The NEWEST released entry, not merely "somewhere in the file", and not
    # [Unreleased] - which is a permanent heading and would satisfy a naive
    # check forever.
    headings = [h for h in re.findall(r"^## \[([^\]]+)\]", changelog, re.MULTILINE)
                if h.lower() != "unreleased"]
    newest = headings[0] if headings else ""
    check(
        "newest released CHANGELOG entry is [{}]".format(pkg_version),
        newest == pkg_version,
        "newest is [{}]".format(newest) if newest else "no '## [version]' heading found",
    )

# ==========================================
# 3. The files a repository is expected to carry
# ==========================================
print("\nrequired files")
for required in (
    "LICENSE",
    "README.md",
    "CHANGELOG.md",
    "CONTRIBUTING.md",
    "SECURITY.md",
    ".gitignore",
    ".gitattributes",
    "Docs~/mascot.svg",
):
    full = os.path.join(ROOT, required)
    exists = os.path.isfile(full)
    check(
        "{} exists".format(required),
        exists and os.path.getsize(full) > 0,
        "missing" if not exists else "empty",
    )

# ==========================================
# 4. Every imported file has a .meta, and no
#    .meta is orphaned
#
# Folders ending in '~' are invisible to Unity by
# design (Docs~, Samples~) and must NOT have one.
# ==========================================
print("\n.meta coverage")


def unity_visible_paths():
    """Every file and folder Unity would import, relative to the package root."""
    files, folders = set(), set()
    for current, dirs, names in os.walk(ROOT):
        dirs[:] = [d for d in dirs if not d.endswith("~") and not d.startswith(".")]
        rel_dir = os.path.relpath(current, ROOT)
        rel_dir = "" if rel_dir == "." else rel_dir.replace(os.sep, "/")
        for name in dirs:
            folders.add("{}/{}".format(rel_dir, name) if rel_dir else name)
        for name in names:
            if name.startswith("."):
                continue
            files.add("{}/{}".format(rel_dir, name) if rel_dir else name)
    return files, folders


visible, visible_folders = unity_visible_paths()
importable = visible | visible_folders

missing_meta = []
orphan_meta = []

for path in sorted(visible):
    if path.endswith(".meta"):
        if path[: -len(".meta")] not in importable:
            orphan_meta.append(path)
    elif path + ".meta" not in visible:
        missing_meta.append(path)

for path in sorted(visible_folders):
    if path + ".meta" not in visible:
        missing_meta.append(path + "/")

check("no file or folder is missing its .meta", not missing_meta, ", ".join(missing_meta[:8]))
check("no orphaned .meta files", not orphan_meta, ", ".join(orphan_meta[:8]))

# ==========================================
# 5. .meta GUIDs are unique
# ==========================================
print("\n.meta GUIDs")
guids = {}
duplicates = []
for path in sorted(visible):
    if not path.endswith(".meta"):
        continue
    found = re.search(r"^guid:\s*([0-9a-f]{32})\s*$", read(path), re.MULTILINE)
    if not found:
        duplicates.append("{} has no guid".format(path))
        continue
    guid = found.group(1)
    if guid in guids:
        duplicates.append("{} == {}".format(path, guids[guid]))
    guids[guid] = path

check("every .meta carries a unique guid", not duplicates, ", ".join(duplicates[:5]))

# ==========================================
# 6. Assemblies
# ==========================================
print("\nassemblies")
runtime_asmdef = json.loads(read("Runtime/AmirCollider.UnityDirectTMP.asmdef"))
editor_asmdef = json.loads(read("Editor/AmirCollider.UnityDirectTMP.Editor.asmdef"))

check(
    "the editor assembly is Editor-only",
    editor_asmdef.get("includePlatforms") == ["Editor"],
)
check(
    "the runtime assembly is NOT Editor-only",
    not editor_asmdef.get("includePlatforms") == runtime_asmdef.get("includePlatforms"),
)
check(
    "the editor assembly references the runtime one",
    "AmirCollider.UnityDirectTMP" in editor_asmdef.get("references", []),
)

# The runtime assembly compiles into player builds. One UnityEditor reference
# in it is a build that fails on the user's machine and compiles fine on ours,
# because the Editor has the type and the player does not.
runtime_sources = {}
for current, dirs, names in os.walk(os.path.join(ROOT, "Runtime")):
    dirs[:] = [d for d in dirs if not d.startswith(".") and not d.endswith("~")]
    for name in names:
        if name.endswith(".cs"):
            full = os.path.join(current, name)
            runtime_sources[os.path.relpath(full, ROOT).replace(os.sep, "/")] = read(full)


def code_lines(source):
    """Lines with // comments and string literals removed.

    Crude on purpose: it only has to stop a comment or a
    Japanese string constant from reading as code.
    """
    out = []
    for line in source.split("\n"):
        stripped = line.strip()
        if stripped.startswith("//") or stripped.startswith("*") or stripped.startswith("/*"):
            continue
        out.append(re.sub(r'"(?:[^"\\]|\\.)*"', '""', line.split("//")[0]))
    return out


unguarded_editor = []
for path, source in sorted(runtime_sources.items()):
    guarded = "#if UNITY_EDITOR" in source
    for number, line in enumerate(code_lines(source), start=1):
        if re.search(r"\bUnityEditor\b", line) and not guarded:
            unguarded_editor.append("{}:{}".format(path, number))

check(
    "no unguarded UnityEditor reference in Runtime/",
    not unguarded_editor,
    ", ".join(unguarded_editor[:5]),
)

# ==========================================
# 7. Codebase invariants
#
# The EditMode tests need a Unity licence, which a fork
# cannot have and which this repository may not have
# configured - so the job that runs them is skipped far
# more often than it runs. That makes it the wrong place
# for rules that are cheap to state textually.
# ==========================================
print("\ncodebase invariants")

editor_sources = {}
for current, dirs, names in os.walk(os.path.join(ROOT, "Editor")):
    dirs[:] = [d for d in dirs if not d.startswith(".") and not d.endswith("~")]
    for name in names:
        if name.endswith(".cs"):
            full = os.path.join(current, name)
            editor_sources[os.path.relpath(full, ROOT).replace(os.sep, "/")] = read(full)

# --- The Editor-font safety rule.
#
#     This is the one invariant in the package whose loss is
#     unrecoverable from inside the Editor: point Unity's chrome
#     at a font with no Latin in it and every menu, including
#     "Revert to Unity Default", becomes empty boxes. The rule
#     has to stay a refusal, and the refusal has to stay wired
#     into every path that can apply a font.
rules = editor_sources.get("Editor/EditorFont/DirectEditorFontPlan.cs", "")
patcher = editor_sources.get("Editor/EditorFont/DirectEditorFont.cs", "")

check(
    "the Latin-coverage guard exists",
    "CanDrawEditorChrome" in rules and "NoLatinCoverage" in rules,
)
check(
    "NoLatinCoverage is fatal, not advisory",
    re.search(r"IsFatal[\s\S]{0,400}NotDynamic", rules) is not None,
    "IsFatal must exempt only NotDynamic",
)
check(
    "the patcher refuses to apply a fatal plan",
    "DirectEditorFontRules.IsFatal" in patcher,
)

# --- Every path that applies the Editor font validates first.
for path, source in sorted(editor_sources.items()):
    if "DirectEditorFontSettings.Plan =" not in source:
        continue
    check(
        "{} validates before storing an Editor-font plan".format(os.path.basename(path)),
        "Validate(" in source or "DirectEditorFont.Apply" in source,
    )

# --- Nothing may write to the Unity installation. The whole
#     safety story of the Editor-font feature is "it is only in
#     memory"; a File.WriteAllBytes into an EditorApplication
#     path would silently make that false.
installation_writes = []
for path, source in sorted(editor_sources.items()):
    for number, line in enumerate(code_lines(source), start=1):
        if "EditorApplication.applicationContentsPath" in line or "EditorApplication.applicationPath" in line:
            installation_writes.append("{}:{}".format(path, number))

check(
    "nothing touches the Unity installation path",
    not installation_writes,
    ", ".join(installation_writes[:5]),
)

# --- Language strings. Every user-facing string goes through
#     DirectTMPText.L(en, ja, fa), which means three arguments.
#     A two-argument call is a translation somebody skipped.
text_pack = "Editor/Brand/DirectTMPText.cs"
check(
    "the text pack defines all three languages",
    all(marker in editor_sources.get(text_pack, "")
        for marker in ('"en"', '"ja"', '"fa"')),
)

# --- The menu tree in the constants must match the README.
print("\nmenu tree vs README")
menu_constants = editor_sources.get("Editor/DirectTMPEditorConstants.cs", "")
readme = read("README.md")

menu_leaves = re.findall(r'public const string Menu\w+\s*=\s*MenuRoot\s*\+\s*"([^"]+)"', menu_constants)
for leaf in menu_leaves:
    # A constant ending in "/" is a submenu ROOT that other constants build
    # on ("Language/"), not a leaf anybody clicks. It has no label of its own
    # to look for in the README.
    if leaf.endswith("/"):
        continue

    # The README draws the tree as a box diagram, so a nested path
    # "Convert/Current Scene" appears as its last segment on its own line.
    label = leaf.split("/")[-1]
    check(
        'README documents the "{}" menu item'.format(label),
        label in readme,
    )

# ==========================================
# 8. Housekeeping
# ==========================================
print("\nhousekeeping")
strays = [
    p
    for p in visible
    if os.path.basename(p).upper().startswith(("READ-ME-FIRST", "CHANGES_SUMMARY", "UPLOAD-NOTES"))
    or p.endswith((".orig", ".rej", ".bak"))
]
check("no scratch files committed", not strays, ", ".join(strays[:5]))

# A font accidentally committed at the package root is somebody's local test
# file, and shipping it means shipping a licence question with the package.
root_fonts = [p for p in visible if "/" not in p and p.lower().endswith((".ttf", ".otf", ".ttc"))]
check("no stray font file at the package root", not root_fonts, ", ".join(root_fonts))

print("\n{} checks, {} failed".format(checks_run, len(failures)))
if failures:
    print("\nFailed: " + ", ".join(failures))
    sys.exit(1)
print("Package looks good.")
