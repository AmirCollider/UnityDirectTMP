// ==========================================
// DirectFontDiagnostics
// Why the same package, with the same font, on the
// same Unity version, works in one project and does
// nothing at all in the next one.
//
// That report has come in more than any other, and it
// has never once been a difference between the two
// fonts. A .ttf carries no import settings; the .meta
// file beside it does, and that file belongs to the
// project. So does whether TextMeshPro's resource
// folder was ever imported, and whether its shaders
// came with it. Four project-level facts decide
// whether a font file can become a dynamic atlas, and
// every one of them can differ between two projects
// that look identical:
//
//   1. TMP Essential Resources imported. Without them
//      TMP_Settings is null and TextMeshPro has no
//      shaders, so every generated material is built
//      on a null shader and draws nothing. The dialog
//      that offers to import them is closed by a lot
//      of people.
//
//   2. "Include Font Data" on the font importer. Off,
//      and Unity keeps a reference to the font by name
//      instead of its bytes - so TMP_FontAsset's
//      factory has nothing to read and returns null.
//      This is the single most common one, and the
//      only signal was a font asset that never
//      appeared.
//
//   3. The importer's character mode. Anything but
//      Dynamic bakes a fixed bitmap set at import
//      time, and the FontEngine cannot open a face
//      from it at all.
//
//   4. The distance-field shader being findable.
//
// None of these produced a message before. The factory
// returned null, DirectFont returned quietly, and the
// label kept whatever font it already had - which
// looks exactly like a package that did not install.
//
// This file states which of the four is wrong, in a
// sentence with the fix in it. DirectFontFactory then
// works around the two it can work around (2 and 3)
// by reading the font file off disk itself.
// ==========================================
using System;
using TMPro;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// Project-level checks that decide whether a font file can be turned into
    /// a dynamic TextMeshPro atlas, and plain-language explanations when one
    /// of them fails.
    /// </summary>
    public static class DirectFontDiagnostics
    {
        private static bool s_projectReported;

        // ==========================================
        // TMP Essential Resources
        // ==========================================
        /// <summary>
        /// True when TextMeshPro's resource folder has been imported into this
        /// project. Without it there is no <c>TMP Settings</c> asset, no
        /// default material and - depending on the TMP version - no shaders.
        /// </summary>
        public static bool TmpResourcesImported
        {
            get
            {
                try { return TMP_Settings.instance != null; }
                catch (Exception) { return false; }
            }
        }

        // ==========================================
        // The shader every generated material sits on
        //
        // Tried in the order TextMeshPro itself prefers.
        // A project that has the package but not its
        // resources finds none of them, and a material
        // built on a null shader renders nothing at all -
        // which reads as "the font never applied".
        // ==========================================
        /// <summary>The distance-field shader TMP builds font materials on, or null.</summary>
        public static Shader FindDistanceFieldShader()
        {
            Shader shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (shader == null) { shader = Shader.Find("TextMeshPro/Distance Field"); }
            if (shader == null) { shader = Shader.Find("TextMeshPro/Mobile/Distance Field SSD"); }
            return shader;
        }

        // ==========================================
        // ProjectProblem
        //
        // The one thing wrong with this project, or null
        // when there is nothing wrong with it. Ordered by
        // how early it stops everything.
        // ==========================================
        /// <summary>
        /// A description of the project-level reason nothing this package does
        /// can work here, or <c>null</c> when the project is set up correctly.
        /// </summary>
        public static string ProjectProblem()
        {
            if (!TmpResourcesImported)
            {
                return "TextMeshPro's Essential Resources are not in this project. TextMeshPro is an assembly "
                     + "plus a folder of resources, and without the folder there is no TMP Settings asset and no "
                     + "material for a generated font to sit on — so no font this package builds can ever draw. "
                     + "Import them from Window ▸ TextMeshPro ▸ Import TMP Essential Resources. This is the usual "
                     + "reason the package works in one project and does nothing in another.";
            }

            if (FindDistanceFieldShader() == null)
            {
                return "TextMeshPro's distance-field shader could not be found in this project. Every font asset "
                     + "this package builds needs it, and a material built without it draws nothing. Re-import "
                     + "Window ▸ TextMeshPro ▸ Import TMP Essential Resources; in a player build, add "
                     + "\"TextMeshPro/Mobile/Distance Field\" to Project Settings ▸ Graphics ▸ Always Included Shaders.";
            }

            return null;
        }

        /// <summary>
        /// Writes <see cref="ProjectProblem"/> to the console once per session.
        /// Called before the first font build; a project with fifty labels
        /// should not get fifty copies of the same paragraph.
        /// </summary>
        public static void ReportProjectOnce()
        {
            if (s_projectReported) { return; }
            s_projectReported = true;

            string problem = ProjectProblem();
            if (problem != null) { DirectTMPLog.Error(problem); }
        }

        /// <summary>Lets a fixed project be re-reported if it breaks again.</summary>
        public static void ResetProjectReport() => s_projectReported = false;

        // ==========================================
        // ExplainFont
        //
        // Why THIS font could not be read, as opposed to
        // why nothing in the project can be. Everything
        // below the first branch is editor-only, because
        // import settings only exist in an editor - but
        // the editor is where somebody is standing when
        // they hit this.
        // ==========================================
        /// <summary>
        /// Why <paramref name="font"/> could not be turned into a dynamic font
        /// asset, with the fix. Never returns null.
        /// </summary>
        public static string ExplainFont(Font font)
        {
            if (font == null) { return "No font file was assigned."; }

            string project = ProjectProblem();
            if (project != null) { return project; }

#if UNITY_EDITOR
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(font);
            var importer = string.IsNullOrEmpty(assetPath)
                ? null
                : UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TrueTypeFontImporter;

            if (importer != null)
            {
                if (!importer.includeFontData)
                {
                    return $"'{font.name}' is imported with \"Include Font Data\" switched off, so Unity kept only "
                         + "the font's NAME and not its bytes — and TextMeshPro has nothing to rasterize from. "
                         + "This setting lives in the .meta file next to the font, which is why the very same .ttf "
                         + "works in one project and not in another. Tick Include Font Data in the font's Inspector, "
                         + "or use Unity DirectTMP ▸ Health Check… to fix it in one click.";
                }

                if (importer.fontTextureCase != UnityEditor.FontTextureCase.Dynamic)
                {
                    return $"'{font.name}' is imported with a baked character set ({importer.fontTextureCase}), not "
                         + "as a Dynamic font. A baked import throws away the outlines and keeps a fixed bitmap, so "
                         + "no glyph outside that set can ever be drawn — which is the exact problem this package "
                         + "exists to remove. Set Character to Dynamic in the font's Inspector, or use "
                         + "Unity DirectTMP ▸ Health Check… to fix it in one click.";
                }
            }
#endif

            if (!font.dynamic)
            {
                return $"'{font.name}' is not a dynamic font, so its outlines are not available to rasterize from. "
                     + "Re-import the .ttf/.otf with Character set to Dynamic and Include Font Data ticked.";
            }

            return $"TextMeshPro could not open a font face from '{font.name}'. The file may be a font COLLECTION "
                 + "(.ttc/.otc), a format the FontEngine does not read, or damaged. Try the file in the "
                 + "Font Catalog — it reads the file directly and reports what is actually in it.";
        }
    }
}
