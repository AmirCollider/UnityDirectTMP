#!/usr/bin/env python3
"""
Assemble a minimal Unity project around the package so the Test
Runner has something to open.

Unity DirectTMP is a package, not a project: it has no Assets/,
no ProjectSettings/, and the Test Runner cannot open it directly.
This builds the smallest host that can run the EditMode tests and
references the package from disk, so what gets tested is the
working tree exactly as it stands rather than a copy.

    python3 make_test_project.py <package-dir> <host-project-dir>
"""

import json
import os
import sys

PROJECT_VERSION = """m_EditorVersion: 2021.3.45f1
"""

# Only what the Test Runner needs; everything else Unity generates
# on first open.
PROJECT_SETTINGS = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!129 &1
PlayerSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 23
  productGUID: 7d1c9b4a3f2e46d8b5c0a917e4f2d3b6
  companyName: UnityDirectTMPCI
  productName: UnityDirectTMPHost
  defaultScreenWidth: 1024
  defaultScreenHeight: 768
"""


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        return 2

    package_dir = os.path.abspath(sys.argv[1])
    host_dir = os.path.abspath(sys.argv[2])

    if not os.path.isfile(os.path.join(package_dir, "package.json")):
        print("error: no package.json in {}".format(package_dir))
        return 1

    with open(os.path.join(package_dir, "package.json"), encoding="utf-8") as handle:
        manifest_source = json.load(handle)
        package_name = manifest_source["name"]

    os.makedirs(os.path.join(host_dir, "Assets"), exist_ok=True)
    os.makedirs(os.path.join(host_dir, "Packages"), exist_ok=True)
    os.makedirs(os.path.join(host_dir, "ProjectSettings"), exist_ok=True)

    # A file: dependency keeps the package where it is. The Test
    # Runner still discovers the package's own Tests assembly
    # because testables lists it explicitly - without that, a
    # package's tests are invisible to the project that consumes it.
    #
    # TextMeshPro is pulled in explicitly rather than relying on the
    # package's own dependency, because the host project has to
    # resolve it on 2021.3 (where it is com.unity.textmeshpro) as
    # well as on Unity 6 (where com.unity.ugui carries it). Asking
    # for both and letting UPM ignore the one that does not exist on
    # a given editor is simpler than branching on the version.
    dependencies = {
        package_name: "file:{}".format(package_dir.replace(os.sep, "/")),
        "com.unity.test-framework": "1.1.33",
        "com.unity.ugui": "1.0.0",
    }

    manifest = {"dependencies": dependencies, "testables": [package_name]}

    with open(os.path.join(host_dir, "Packages", "manifest.json"), "w", encoding="utf-8") as handle:
        json.dump(manifest, handle, indent=2)

    with open(
        os.path.join(host_dir, "ProjectSettings", "ProjectVersion.txt"), "w", encoding="utf-8"
    ) as handle:
        handle.write(PROJECT_VERSION)

    with open(
        os.path.join(host_dir, "ProjectSettings", "ProjectSettings.asset"), "w", encoding="utf-8"
    ) as handle:
        handle.write(PROJECT_SETTINGS)

    print("Host project ready at {}".format(host_dir))
    print("  package: {} -> {}".format(package_name, package_dir))
    return 0


if __name__ == "__main__":
    sys.exit(main())
