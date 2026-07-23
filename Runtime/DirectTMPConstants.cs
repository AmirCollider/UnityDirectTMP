// ==========================================
// DirectTMPConstants
// Shared identity and default values used across
// the Unity DirectTMP runtime. Anything an author
// might want to tune (sampling size, atlas size,
// render mode) has a default here; the editor
// Settings screen simply overrides these.
// ==========================================
using UnityEngine.TextCore.LowLevel;

namespace UnityDirectTMP
{
    /// <summary>
    /// Constant identity and default tuning values for Unity DirectTMP.
    /// Kept in one place so the runtime, the Inspector and the Settings
    /// screen never disagree about a default.
    /// </summary>
    public static class DirectTMPConstants
    {
        // ==========================================
        // Identity
        // ==========================================
        public const string ToolName = "Unity DirectTMP";
        public const string Version = "0.1.0";
        public const string GithubUrl = "https://github.com/AmirCollider/UnityDirectTMP";
        public const string Author = "AmirCollider";

        // ==========================================
        // Atlas / rasterization defaults
        //
        // 90 is TextMeshPro's own default sampling
        // point size for a dynamic SDF atlas - large
        // enough to stay crisp when scaled up, small
        // enough that a page of CJK still fits a single
        // 1024 texture before spilling into a second.
        // ==========================================
        public const int DefaultSamplingPointSize = 90;
        public const int DefaultAtlasPadding = 9;
        public const int DefaultAtlasWidth = 1024;
        public const int DefaultAtlasHeight = 1024;
        public const GlyphRenderMode DefaultRenderMode = GlyphRenderMode.SDFAA;

        // A hard floor / ceiling so a stray Settings value
        // can never ask the FontEngine for a 0 px or a
        // 16 384 px atlas.
        public const int MinSamplingPointSize = 16;
        public const int MaxSamplingPointSize = 256;
        public const int MinAtlasDimension = 256;
        public const int MaxAtlasDimension = 8192;

        // ==========================================
        // Runtime cache folder
        //
        // Fonts handed to DirectTMP as raw bytes are
        // spooled to a file first (the FontEngine reads a
        // face far more reliably from a path than from a
        // freshly-allocated managed array). They live
        // under persistentDataPath so a downloaded font
        // survives an app restart and is shared between
        // every DirectFont that asked for it.
        // ==========================================
        public const string RuntimeCacheFolderName = "UnityDirectTMP/FontCache";

        // ==========================================
        // The suffix appended to a generated dynamic
        // font asset's name, so it is obvious in a
        // profiler / memory view where the asset came
        // from.
        // ==========================================
        public const string GeneratedAssetSuffix = " (DirectTMP)";
    }
}
