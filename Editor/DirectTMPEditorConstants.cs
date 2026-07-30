// ==========================================
// DirectTMPEditorConstants
// Menu paths and Project-Settings identifiers for
// the editor side of Unity DirectTMP.
//
// The menu tree here must match the one drawn in
// README.md exactly. DirectTMPMenuPathTests asserts
// it, because a README that documents a menu item
// nobody can find is worse than no README - the
// reader concludes the install is broken.
//
//   Unity DirectTMP
//   ├── Font Catalog…
//   ├── Editor Font
//   │   ├── Choose a Font…
//   │   └── Revert to Unity Default
//   ├── Convert
//   │   ├── Selected Objects
//   │   ├── Current Scene
//   │   └── Whole Project
//   ├── Fallback Chain…
//   ├── Font Cache
//   │   ├── Show Cache Folder
//   │   └── Clear Cache
//   ├── Language
//   ├── Settings…
//   ├── Health Check…
//   ├── Welcome
//   └── About Unity DirectTMP
//
// Why the same three windows are ALSO registered under
// Window ▸ Unity DirectTMP
// -------------------------------------------------
// "The menu doesn't appear in the top bar" is the most
// common report this package gets, and it is almost
// never the menu: it is the editor assembly failing to
// compile, usually because TextMeshPro is not present
// in that project. When an editor assembly does not
// compile, every [MenuItem] inside it vanishes at once,
// and a whole missing top-level menu reads as "the tool
// didn't install" rather than as "there is a compile
// error in the Console".
//
// The Window entries do not fix that - nothing in a
// dead assembly runs - but they give a second, more
// familiar place to look, and they lead to a Health
// Check window written specifically to explain that
// case in words. package.json now also declares the
// uGUI dependency that carries TextMeshPro, which is
// the actual fix.
// ==========================================
namespace UnityDirectTMP.Editor
{
    internal static class DirectTMPEditorConstants
    {
        // ==========================================
        // Top menu bar (must match README.md exactly)
        // ==========================================
        public const string MenuRoot = "Unity DirectTMP/";

        public const string MenuCatalog = MenuRoot + "Font Catalog…";

        public const string MenuEditorFont = MenuRoot + "Editor Font/Choose a Font…";
        public const string MenuEditorFontRevert = MenuRoot + "Editor Font/Revert to Unity Default";

        public const string MenuConvertSelected = MenuRoot + "Convert/Selected Objects";
        public const string MenuConvertScene = MenuRoot + "Convert/Current Scene";
        public const string MenuConvertProject = MenuRoot + "Convert/Whole Project";

        public const string MenuFallbackChain = MenuRoot + "Fallback Chain…";

        public const string MenuShowCacheFolder = MenuRoot + "Font Cache/Show Cache Folder";
        public const string MenuClearCache = MenuRoot + "Font Cache/Clear Cache";

        public const string MenuLanguageRoot = MenuRoot + "Language/";
        public const string MenuLanguageEnglish = MenuLanguageRoot + "English";
        public const string MenuLanguageJapanese = MenuLanguageRoot + "日本語";

        // ==========================================
        // فارسی, as stored.
        //
        // A [MenuItem] path is drawn by the operating
        // system's own menu, not by IMGUI - and the OS
        // has a full text stack that joins and reorders
        // on its own. So this one is NOT prepared: the
        // plain codepoints are what the menu bar wants,
        // and a prepared string here would be turned
        // around twice.
        //
        // The same rule covers dropdown lists, right-
        // click menus and modal dialogs; see
        // DirectTMPText.Native.
        // ==========================================
        public const string MenuLanguagePersian = MenuLanguageRoot + "فارسی";

        public const string MenuSettings = MenuRoot + "Settings…";
        public const string MenuHealthCheck = MenuRoot + "Health Check…";
        public const string MenuWelcome = MenuRoot + "Welcome";
        public const string MenuAbout = MenuRoot + "About Unity DirectTMP";

        // ==========================================
        // Window ▸ Unity DirectTMP - the second door.
        // Only the three windows worth a shortcut; the
        // batch operations stay in one place.
        // ==========================================
        public const string WindowRoot = "Window/Unity DirectTMP/";
        public const string WindowCatalog = WindowRoot + "Font Catalog";
        public const string WindowEditorFont = WindowRoot + "Editor Font";
        public const string WindowHealthCheck = WindowRoot + "Health Check";

        // ==========================================
        // Menu priorities. Gaps of 11+ tell Unity to
        // draw a separator, which is what groups the
        // tree into: the two windows people open, the
        // batch conversions, the housekeeping, and the
        // about-box tail.
        // ==========================================
        public const int PriorityCatalog = 0;
        public const int PriorityEditorFont = 1;
        public const int PriorityEditorFontRevert = 2;

        public const int PriorityConvertSelected = 20;
        public const int PriorityConvertScene = 21;
        public const int PriorityConvertProject = 22;

        public const int PriorityFallbackChain = 40;

        public const int PriorityShowCacheFolder = 60;
        public const int PriorityClearCache = 61;

        public const int PriorityLanguage = 80;

        public const int PrioritySettings = 100;
        public const int PriorityHealthCheck = 101;
        public const int PriorityWelcome = 120;
        public const int PriorityAbout = 121;

        // ==========================================
        // Project-window right-click context menu
        // ==========================================
        public const string AssetsContextRoot = "Assets/Unity DirectTMP/";
        public const string ContextConvertFolder = AssetsContextRoot + "Convert TMP In Folder";
        public const string ContextNewFallbackChain = AssetsContextRoot + "New Fallback Chain";
        public const string ContextUseForEditor = AssetsContextRoot + "Use This Font For The Editor";
        public const string ContextInspectFont = AssetsContextRoot + "Inspect This Font";

        // ==========================================
        // Project Settings page
        // ==========================================
        public const string SettingsPath = "Project/Unity DirectTMP";
    }
}
