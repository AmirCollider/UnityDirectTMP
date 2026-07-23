// ==========================================
// DirectTMPEditorConstants
// Menu paths and Project-Settings identifiers for
// the editor side of Unity DirectTMP. The menu tree
// here must match the one drawn in README.md exactly:
//
//   Unity DirectTMP
//   ├── Convert
//   │   ├── Selected Objects
//   │   ├── Current Scene
//   │   └── Whole Project
//   ├── Fallback Chain…
//   ├── Font Cache
//   │   ├── Show Cache Folder
//   │   └── Clear Cache
//   ├── Settings…
//   └── About Unity DirectTMP
// ==========================================
namespace UnityDirectTMP.Editor
{
    internal static class DirectTMPEditorConstants
    {
        // ==========================================
        // Top menu bar (must match README.md exactly)
        // ==========================================
        public const string MenuRoot = "Unity DirectTMP/";

        public const string MenuConvertSelected = MenuRoot + "Convert/Selected Objects";
        public const string MenuConvertScene = MenuRoot + "Convert/Current Scene";
        public const string MenuConvertProject = MenuRoot + "Convert/Whole Project";

        public const string MenuFallbackChain = MenuRoot + "Fallback Chain…";

        public const string MenuShowCacheFolder = MenuRoot + "Font Cache/Show Cache Folder";
        public const string MenuClearCache = MenuRoot + "Font Cache/Clear Cache";

        public const string MenuSettings = MenuRoot + "Settings…";
        public const string MenuAbout = MenuRoot + "About Unity DirectTMP";

        // ==========================================
        // Menu priorities. Gaps of 11+ tell Unity to
        // draw a separator, which is what groups
        // Convert / Fallback / Cache / Settings / About.
        // ==========================================
        public const int PriorityConvertSelected = 1;
        public const int PriorityConvertScene = 2;
        public const int PriorityConvertProject = 3;
        public const int PriorityFallbackChain = 20;
        public const int PriorityShowCacheFolder = 40;
        public const int PriorityClearCache = 41;
        public const int PrioritySettings = 60;
        public const int PriorityAbout = 80;

        // ==========================================
        // Project-window right-click context menu
        // ==========================================
        public const string AssetsContextRoot = "Assets/Unity DirectTMP/";
        public const string ContextConvertFolder = AssetsContextRoot + "Convert TMP In Folder";
        public const string ContextNewFallbackChain = AssetsContextRoot + "New Fallback Chain";

        // ==========================================
        // Project Settings page
        // ==========================================
        public const string SettingsPath = "Project/Unity DirectTMP";
    }
}
