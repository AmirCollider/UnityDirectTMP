// ==========================================
// DirectFontBytes
// The font's own file, carried into a player build.
//
// ==========================================
// WHY THIS EXISTS
// ==========================================
// DirectFontJoiner reads the font's GSUB table to find
// out what the designer drew for each joined shape. That
// needs the font's BYTES.
//
// A font loaded from a path or from a byte[] carries its
// own bytes. An imported Unity `Font` asset does not: it
// hands TextMeshPro its outlines through the engine and
// keeps the file to itself. In the Editor that is no
// obstacle, because AssetDatabase can be asked where the
// .ttf is and the file read off disk.
//
// A player build has no AssetDatabase and no .ttf. So
// every version before 2.1.7 fell back, silently, to the
// legacy presentation block (U+FE70..FEFF / U+FB50..FBFF)
// on device while using the font's real joining rules in
// the Editor.
//
// That is the worst shape a bug can take. The Editor -
// the only place the developer looks - was the one place
// it did not happen, and a great many Persian faces fill
// that legacy block in badly or not at all. So the text
// was right in the Game view and came apart on the phone.
//
// The fix is to stop needing the AssetDatabase: the bytes
// are copied into a `.bytes` TextAsset at build time and
// listed in DirectFontBytesTable, one asset under
// Resources, which the player can load like anything else.
// ==========================================
using System.Collections.Generic;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// Reads the baked table. Loaded once; a project with no table simply has
    /// no baked fonts, which is the behaviour of every version before 2.1.7.
    /// </summary>
    public static class DirectFontBytes
    {
        /// <summary>Where the table lives, under any <c>Resources</c> folder.</summary>
        public const string ResourcePath = "DirectTMPFontBytes";

        private static DirectFontBytesTable s_table;
        private static bool s_looked;

        // Reading a TextAsset's bytes copies the array every time, and the
        // joiner asks once per font asset - but a scene reload asks again, and
        // a 7 MB CJK face is not something to copy for no reason.
        private static readonly Dictionary<int, byte[]> s_cache = new Dictionary<int, byte[]>();

        /// <summary>The table, or null when this project has never baked one.</summary>
        public static DirectFontBytesTable Table
        {
            get
            {
                if (s_looked) { return s_table; }
                s_looked = true;

                try { s_table = Resources.Load<DirectFontBytesTable>(ResourcePath); }
                catch { s_table = null; }

                return s_table;
            }
        }

        /// <summary>
        /// The file behind an imported font, or null. This is the only route
        /// to a font's own joining rules in a player build.
        /// </summary>
        public static byte[] For(Font font)
        {
            if (font == null) { return null; }

            int id = font.GetInstanceID();
            if (s_cache.TryGetValue(id, out byte[] cached)) { return cached; }

            DirectFontBytesTable table = Table;
            byte[] bytes = table == null ? null : table.For(font);

            // A miss is remembered too. Asking a table that does not list this
            // font is not going to start working, and the joiner asks for
            // every label on screen.
            s_cache[id] = bytes;
            return bytes;
        }

        /// <summary>Forgets the table and everything read from it.</summary>
        public static void Clear()
        {
            s_table = null;
            s_looked = false;
            s_cache.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => Clear();
    }
}
