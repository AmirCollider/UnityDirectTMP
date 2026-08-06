#!/usr/bin/env python3
"""
Structural checks on the package, run in CI before Unity ever sees it.

Deliberately small. It checks the handful of things that are cheap to get
wrong and expensive to discover in the Editor - a missing .meta, an
unguarded UnityEditor reference in runtime code, an asmdef that has lost a
reference - and nothing else. Behaviour is the tests' job.
"""
import json
import os
import re
import sys

FAILURES = []


def check(condition, message):
    if not condition:
        FAILURES.append(message)


def read(path):
    with open(path, encoding="utf-8") as handle:
        return handle.read()


# ---------------------------------------------------------------- layout
REQUIRED = [
    "package.json",
    "README.md",
    "CHANGELOG.md",
    "LICENSE",
    "Runtime/AmirCollider.UnityDirectTMP.asmdef",
    "Editor/AmirCollider.UnityDirectTMP.Editor.asmdef",
    "Runtime/DirectJoining.cs",
    "Runtime/DirectShaper.cs",
    "Runtime/DirectFontGsub.cs",
    "Runtime/DirectFontJoiner.cs",
    "Runtime/DirectBidi.cs",
    "Runtime/DirectFont.cs",
    "Runtime/DirectTMP.cs",
    "Editor/DirectTMPWindow.cs",
]
for path in REQUIRED:
    check(os.path.exists(path), f"missing required file: {path}")

# ---------------------------------------------------------------- meta files
for root, dirs, files in os.walk("."):
    dirs[:] = [d for d in dirs if d not in (".git", ".github") and not d.endswith("~")]
    for name in files:
        if name.endswith(".meta") or name.startswith("."):
            continue
        path = os.path.join(root, name)
        if path.startswith("./.") or "Docs~" in path or "Samples~" in path:
            continue
        check(os.path.exists(path + ".meta"), f"missing .meta for {path}")

# ---------------------------------------------------------------- unique GUIDs
guids = {}
for root, dirs, files in os.walk("."):
    dirs[:] = [d for d in dirs if d != ".git"]
    for name in files:
        if not name.endswith(".meta"):
            continue
        path = os.path.join(root, name)
        match = re.search(r"^guid:\s*([0-9a-f]{32})\s*$", read(path), re.M)
        check(match is not None, f"no guid in {path}")
        if match:
            guids.setdefault(match.group(1), []).append(path)
for guid, paths in guids.items():
    check(len(paths) == 1, f"duplicate guid {guid}: {paths}")

# ---------------------------------------------------------------- asmdefs
runtime = json.loads(read("Runtime/AmirCollider.UnityDirectTMP.asmdef"))
editor = json.loads(read("Editor/AmirCollider.UnityDirectTMP.Editor.asmdef"))

check("Unity.TextMeshPro" in runtime["references"], "runtime asmdef must reference Unity.TextMeshPro")
check(runtime.get("includePlatforms") == [], "runtime asmdef must build for every platform")
check(runtime["name"] in editor["references"], "editor asmdef must reference the runtime assembly")
check(editor.get("includePlatforms") == ["Editor"], "editor asmdef must be Editor-only")

# ---------------------------------------------------------------- runtime purity
# UnityEditor in a runtime assembly breaks the build unless it is guarded.
for name in sorted(os.listdir("Runtime")):
    if not name.endswith(".cs"):
        continue
    source = read(os.path.join("Runtime", name))
    if "UnityEditor" not in source:
        continue

    guarded = True
    depth = 0
    for line in source.splitlines():
        stripped = line.strip()
        if stripped.startswith("#if UNITY_EDITOR"):
            depth += 1
        elif stripped.startswith("#endif") and depth:
            depth -= 1
        elif "UnityEditor" in stripped and depth == 0 and not stripped.startswith("//"):
            guarded = False
    check(guarded, f"Runtime/{name} uses UnityEditor outside a UNITY_EDITOR guard")

# ---------------------------------------------------------------- package.json
package = json.loads(read("package.json"))
for field in ("name", "version", "displayName", "description", "unity"):
    check(field in package, f"package.json is missing '{field}'")
check(re.match(r"^\d+\.\d+\.\d+$", package.get("version", "")), "package.json version must be x.y.z")
check(package.get("name", "").islower(), "package.json name must be lower case")

changelog = read("CHANGELOG.md")
check(package["version"] in changelog, f"CHANGELOG.md has no entry for {package['version']}")

# ---------------------------------------------------------------- report
if FAILURES:
    print(f"{len(FAILURES)} problem(s):")
    for failure in FAILURES:
        print("  -", failure)
    sys.exit(1)

print("package looks well formed")
