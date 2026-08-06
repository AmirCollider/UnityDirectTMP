// ==========================================
// DirectTMP
// The public API.
//
// There is no project-wide setting and no global state.
// A label is drawn from a font file because it has a
// Direct Font component on it, or because your code
// called one of these methods. Nothing happens to a
// label nobody asked about.
// ==========================================
using TMPro;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>Everything this package can be asked to do, in one place.</summary>
    public static class DirectTMP
    {
        // ==========================================
        // Text
        // ==========================================
        /// <summary>
        /// Joins Arabic-script letters and puts right-to-left text in reading
        /// order, using <paramref name="asset"/> to find the joined shapes.
        /// Text with no Arabic script in it comes back untouched.
        ///
        /// Rich-text tags are never shaped or reordered.
        /// </summary>
        public static string Prepare(string text, TMP_FontAsset asset)
        {
            if (string.IsNullOrEmpty(text)) { return text; }
            if (!DirectJoining.NeedsShaping(text) && !DirectBidi.ContainsRightToLeft(text)) { return text; }

            DirectFontJoiner joiner = asset == null ? null : DirectFontJoiner.For(asset);

            return DirectRichText.Apply(text, segment =>
            {
                string shaped = joiner == null
                    ? DirectShaper.Shape(segment)
                    : joiner.Shape(segment);

                return DirectBidi.Reorder(shaped);
            });
        }

        /// <summary>
        /// The same, with no font behind it - every letter becomes its Unicode
        /// presentation form. For text going somewhere that is not a
        /// TextMeshPro label.
        /// </summary>
        public static string Prepare(string text) => Prepare(text, null);

        // ==========================================
        // Labels
        // ==========================================
        /// <summary>
        /// Draws one label from a font file, from code - the same thing the
        /// Direct Font component does. Returns false if the font could not be
        /// built; the Console says why.
        /// </summary>
        public static bool Apply(TMP_Text label, Font file)
        {
            if (label == null || file == null) { return false; }

            DirectTMPFont direct = label.GetComponent<DirectTMPFont>();
            if (direct == null) { direct = label.gameObject.AddComponent<DirectTMPFont>(); }

            direct.Font = file;
            return direct.FontAsset != null;
        }

        /// <summary>
        /// A dynamic font asset for a font file, cached. Assign it to
        /// <c>label.font</c> yourself if you would rather not have a component.
        /// </summary>
        public static TMP_FontAsset Load(Font file) => DirectFont.Load(file);

        /// <summary>The same, for a .ttf/.otf on disk rather than an imported font.</summary>
        public static TMP_FontAsset LoadFromFile(string path) => DirectFont.LoadFromFile(path);

        /// <summary>The same, for font bytes already in memory.</summary>
        public static TMP_FontAsset LoadFromBytes(byte[] bytes, string name = null)
            => DirectFont.LoadFromBytes(bytes, name);

        /// <summary>
        /// Rasterizes characters up front, so the first frame that shows them
        /// does not pay for all of them at once. Returns the characters the
        /// font could not supply.
        /// </summary>
        public static string Preload(TMP_FontAsset asset, string characters)
        {
            if (asset == null || string.IsNullOrEmpty(characters)) { return characters; }

            asset.TryAddCharacters(characters, out string missing);
            return missing;
        }

        /// <summary>Drops every cached font asset. Anything still using one keeps working.</summary>
        public static int ClearCache() => DirectFont.ClearCache();
    }
}
