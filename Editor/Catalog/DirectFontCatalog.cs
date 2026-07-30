// ==========================================
// DirectFontCatalog
// Every font this machine can give you, in one list,
// with the truth about each one.
//
// The problem it solves is small and constant: a
// project has eleven .ttf files with names like
// "NotoSansJP-Regular", "Vazirmatn-Medium",
// "subset-2", and the only way to find out which of
// them can set the language you need is to drag each
// one onto a label and look. On a project with a
// localisation folder that is twenty minutes, and it
// is twenty minutes again the next time somebody asks.
//
// So the catalog reads every font file directly - the
// foundry's own family name, the real glyph count, and
// exactly which codepoints the cmap maps - and answers
// the question in a row.
//
// It covers two sources:
//
//   the project   every Font asset in Assets and in
//                 packages. Fast: a project has a
//                 handful.
//
//   the machine   every font installed on this OS,
//                 through Font.GetPathsToOSFonts().
//                 Off by default, because a Windows
//                 install has three hundred of them
//                 and reading three hundred files is a
//                 progress bar, not a window opening.
//
// This file is the model and the rules. The window is
// next door and holds no logic worth testing, which is
// the split that lets the sorting and the filtering be
// covered by tests that never open a window.
// ==========================================
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    /// <summary>Where a catalog entry came from.</summary>
    internal enum DirectCatalogSource
    {
        Project,
        System
    }

    /// <summary>How the catalog is ordered.</summary>
    internal enum DirectCatalogSort
    {
        /// <summary>By family name, the way a person looks for a font they half-remember.</summary>
        Name = 0,

        /// <summary>Most glyphs first - which is nearly always "the CJK ones first".</summary>
        Glyphs = 1,

        /// <summary>Most scripts covered first - the multilingual candidates.</summary>
        Scripts = 2,

        /// <summary>Largest file first, for when the question is build size.</summary>
        Size = 3
    }

    /// <summary>One font, as the catalog knows it.</summary>
    internal sealed class DirectCatalogEntry
    {
        public DirectCatalogSource Source;

        /// <summary>Project-relative path, for a project font. Empty otherwise.</summary>
        public string AssetPath = string.Empty;

        /// <summary>Absolute path on disk. Always set - it is what was parsed.</summary>
        public string AbsolutePath = string.Empty;

        /// <summary>What the font file said about itself.</summary>
        public DirectFontFileInfo Info;

        /// <summary>Coverage verdicts, computed once at scan time.</summary>
        public Dictionary<DirectScript, DirectScriptCoverage> Coverage = new Dictionary<DirectScript, DirectScriptCoverage>();

        /// <summary>How many scripts this font fully covers. The Scripts sort key.</summary>
        public int FullyCoveredCount;

        /// <summary>The name shown in the row.</summary>
        public string DisplayName = string.Empty;

        /// <summary>File name including the extension, shown as the secondary line.</summary>
        public string FileName = string.Empty;

        public bool IsUsable => Info != null && Info.IsValid;

        public long Bytes => Info != null ? Info.FileBytes : 0L;
        public int GlyphCount => Info != null ? Info.GlyphCount : 0;

        /// <summary>The Font asset, for a project entry. Null for a system font.</summary>
        public Font LoadAsset()
        {
            return string.IsNullOrEmpty(AssetPath) ? null : AssetDatabase.LoadAssetAtPath<Font>(AssetPath);
        }
    }

    /// <summary>
    /// Scans, caches, filters and sorts the catalog. The scan touches the file
    /// system; everything else is pure and lives in
    /// <see cref="DirectFontCatalogFilter"/>.
    /// </summary>
    internal static class DirectFontCatalog
    {
        // Parsed entries survive until the user asks for a rescan or the
        // domain reloads. Re-reading three hundred font files because somebody
        // resized the window would be indefensible.
        private static List<DirectCatalogEntry> s_project;
        private static List<DirectCatalogEntry> s_system;

        /// <summary>How many project fonts are cached. Zero before the first scan.</summary>
        public static int ProjectCount => s_project?.Count ?? 0;

        /// <summary>How many system fonts are cached. Zero until they are asked for.</summary>
        public static int SystemCount => s_system?.Count ?? 0;

        /// <summary>Throws away every cached scan.</summary>
        public static void Invalidate()
        {
            s_project = null;
            s_system = null;
        }

        // ==========================================
        // Project fonts
        // ==========================================
        /// <summary>
        /// Every Font asset the AssetDatabase knows about, parsed. Cached
        /// after the first call.
        /// </summary>
        public static List<DirectCatalogEntry> ProjectFonts(bool force = false)
        {
            if (s_project != null && !force) { return s_project; }

            var entries = new List<DirectCatalogEntry>();
            string[] guids = AssetDatabase.FindAssets("t:Font");

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.IsNullOrEmpty(assetPath)) { continue; }
                    if (!LooksLikeFontFile(assetPath)) { continue; }

                    if (guids.Length > 12 && ReportProgress(
                            DirectTMPText.L("Reading project fonts", "プロジェクトのフォントを読み込み中", "خواندن فونت‌های پروژه"),
                            assetPath, i, guids.Length))
                    {
                        break;
                    }

                    string absolute = DirectEditorFont.AssetPathToAbsolute(assetPath);
                    DirectCatalogEntry entry = Build(absolute, DirectCatalogSource.Project);
                    entry.AssetPath = assetPath;
                    entries.Add(entry);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            s_project = entries;
            return s_project;
        }

        // ==========================================
        // System fonts
        //
        // Deliberately opt-in. Font.GetPathsToOSFonts()
        // on a developer's Windows box routinely returns
        // four hundred paths, and reading four hundred
        // files - several of them 20 MB CJK families -
        // is seconds of disk with the Editor frozen. A
        // catalog that takes four seconds to open is a
        // catalog nobody opens.
        // ==========================================
        /// <summary>
        /// Every font installed on this machine, parsed. The first call reads
        /// them all behind a cancellable progress bar; later calls are free.
        /// </summary>
        public static List<DirectCatalogEntry> SystemFonts(bool force = false)
        {
            if (s_system != null && !force) { return s_system; }

            var entries = new List<DirectCatalogEntry>();
            string[] paths;

            try
            {
                paths = Font.GetPathsToOSFonts() ?? new string[0];
            }
            catch (Exception e)
            {
                DirectTMPLog.Warn("Could not list this machine's installed fonts: " + e.Message);
                s_system = entries;
                return s_system;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    string path = paths[i];
                    if (string.IsNullOrEmpty(path) || !seen.Add(path)) { continue; }
                    if (!LooksLikeFontFile(path)) { continue; }

                    if (ReportProgress(
                            DirectTMPText.L("Reading installed fonts", "インストール済みフォントを読み込み中", "خواندن فونت‌های نصب‌شده"),
                            Path.GetFileName(path), i, paths.Length))
                    {
                        break;
                    }

                    entries.Add(Build(path, DirectCatalogSource.System));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            s_system = entries;
            return s_system;
        }

        /// <summary>
        /// The combined list the window shows, honouring the "include system
        /// fonts" toggle.
        /// </summary>
        public static List<DirectCatalogEntry> All(bool includeSystem, bool force = false)
        {
            var all = new List<DirectCatalogEntry>(ProjectFonts(force));
            if (includeSystem) { all.AddRange(SystemFonts(force)); }
            return all;
        }

        // ==========================================
        // Building one entry
        // ==========================================
        private static DirectCatalogEntry Build(string absolutePath, DirectCatalogSource source)
        {
            var entry = new DirectCatalogEntry
            {
                Source = source,
                AbsolutePath = absolutePath ?? string.Empty,
                FileName = string.IsNullOrEmpty(absolutePath) ? string.Empty : Path.GetFileName(absolutePath)
            };

            entry.Info = DirectFontFile.Read(absolutePath);
            entry.DisplayName = entry.Info != null && !string.IsNullOrEmpty(entry.Info.DisplayName)
                ? entry.Info.DisplayName
                : entry.FileName;

            if (entry.Info != null && entry.Info.IsValid)
            {
                entry.Coverage = entry.Info.Coverage();
                entry.FullyCoveredCount = DirectFontScripts.FullyCovered(entry.Coverage).Count;
            }

            return entry;
        }

        private static bool LooksLikeFontFile(string path)
        {
            if (string.IsNullOrEmpty(path)) { return false; }
            string extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension)) { return false; }

            foreach (string known in DirectTMPConstants.FontExtensions)
            {
                if (string.Equals(extension, known, StringComparison.OrdinalIgnoreCase)) { return true; }
            }
            return false;
        }

        // Returns true when the user cancelled. The scan stops where it is and
        // keeps what it already has, which is more useful than throwing the
        // partial result away.
        private static bool ReportProgress(string title, string detail, int index, int total)
        {
            if (total <= 0) { return false; }
            return EditorUtility.DisplayCancelableProgressBar(
                DirectTMPConstants.ToolName + " · " + title,
                detail,
                (float)index / total);
        }
    }

    /// <summary>
    /// The pure half: filtering and sorting a catalog. No Unity types beyond
    /// the entry itself, so the ordering rules can be tested directly.
    /// </summary>
    internal static class DirectFontCatalogFilter
    {
        /// <summary>
        /// Applies the window's three controls - a text query, an optional
        /// required script, and a sort - in that order.
        ///
        /// Broken fonts always sink to the bottom regardless of the sort. They
        /// still belong in the list (a truncated .ttf in the project is
        /// something the developer needs to see) but never above a font that
        /// works.
        /// </summary>
        public static List<DirectCatalogEntry> Apply(
            IEnumerable<DirectCatalogEntry> entries,
            string query,
            DirectScript? requiredScript,
            DirectCatalogSort sort)
        {
            var result = new List<DirectCatalogEntry>();
            if (entries == null) { return result; }

            foreach (DirectCatalogEntry entry in entries)
            {
                if (entry == null) { continue; }
                if (!MatchesQuery(entry, query)) { continue; }
                if (!MatchesScript(entry, requiredScript)) { continue; }
                result.Add(entry);
            }

            result.Sort((a, b) => Compare(a, b, sort));
            return result;
        }

        /// <summary>
        /// Does this entry match the search box? An empty query matches
        /// everything, and the search covers the family, the style, the file
        /// name and the path - because people look for a font by all four and
        /// a box that only matches one of them feels broken.
        /// </summary>
        public static bool MatchesQuery(DirectCatalogEntry entry, string query)
        {
            if (entry == null) { return false; }
            if (string.IsNullOrEmpty(query) || string.IsNullOrWhiteSpace(query)) { return true; }

            string needle = query.Trim();

            if (Contains(entry.DisplayName, needle)) { return true; }
            if (Contains(entry.FileName, needle)) { return true; }
            if (Contains(entry.AssetPath, needle)) { return true; }
            if (entry.Info != null)
            {
                if (Contains(entry.Info.FamilyName, needle)) { return true; }
                if (Contains(entry.Info.StyleName, needle)) { return true; }
            }
            return false;
        }

        /// <summary>
        /// Does this entry carry the script the user filtered on? Partial
        /// coverage counts as a match on purpose: somebody filtering for
        /// Arabic wants to be shown the font that has most of it, with its
        /// amber badge, rather than told there are no results.
        /// </summary>
        public static bool MatchesScript(DirectCatalogEntry entry, DirectScript? required)
        {
            if (!required.HasValue) { return true; }
            if (entry == null || entry.Coverage == null) { return false; }

            return entry.Coverage.TryGetValue(required.Value, out DirectScriptCoverage verdict)
                && verdict != DirectScriptCoverage.None;
        }

        /// <summary>
        /// The comparison behind every sort. Ties always fall back to the name
        /// so the order is total - a list that reshuffles between two identical
        /// scans looks like a bug even when nothing changed.
        /// </summary>
        public static int Compare(DirectCatalogEntry a, DirectCatalogEntry b, DirectCatalogSort sort)
        {
            if (ReferenceEquals(a, b)) { return 0; }
            if (a == null) { return 1; }
            if (b == null) { return -1; }

            if (a.IsUsable != b.IsUsable) { return a.IsUsable ? -1 : 1; }

            int comparison;
            switch (sort)
            {
                case DirectCatalogSort.Glyphs:
                    comparison = b.GlyphCount.CompareTo(a.GlyphCount);
                    break;
                case DirectCatalogSort.Scripts:
                    comparison = b.FullyCoveredCount.CompareTo(a.FullyCoveredCount);
                    break;
                case DirectCatalogSort.Size:
                    comparison = b.Bytes.CompareTo(a.Bytes);
                    break;
                default:
                    comparison = 0;
                    break;
            }

            if (comparison != 0) { return comparison; }

            return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool Contains(string haystack, string needle)
        {
            return !string.IsNullOrEmpty(haystack)
                && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// A file size a person can read at a glance. Used in the catalog row
        /// and in the Inspector read-out.
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            if (bytes <= 0) { return "—"; }
            if (bytes < 1024) { return bytes + " B"; }
            if (bytes < 1024 * 1024) { return (bytes / 1024f).ToString("0.#") + " KB"; }
            return (bytes / (1024f * 1024f)).ToString("0.#") + " MB";
        }
    }
}
