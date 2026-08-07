// ==========================================
// DirectFontReport
// The diagnostic, ON THE DEVICE.
//
// "Diagnose Selected Label" answers this in the Editor,
// and the Editor is the one place the interesting
// failures do not happen. A build has no AssetDatabase,
// may have had its reflection targets stripped, and ships
// whatever font data somebody remembered to carry - none
// of which can be seen from a Game view.
//
// So this is the same questions, asked where the answer
// matters, and drawn on the screen with IMGUI because
// IMGUI needs no canvas, no font asset and no setup - the
// three things that might be what is broken.
// ==========================================
using System.Text;
using TMPro;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// Draws, on screen and in the log, exactly why a label is or is not joining. Drop it on any
    /// GameObject in the scene and build.
    /// </summary>
    [AddComponentMenu("Unity DirectTMP/Direct Font Report")]
    [HelpURL("https://github.com/AmirCollider/UnityDirectTMP#readme")]
    public sealed class DirectFontReport : MonoBehaviour
    {
        [Tooltip("The label to report on. Empty finds the first Direct Font in the scene.")]
        [SerializeField] private DirectFont label;

        [Tooltip("Text to shape as a sample. Empty uses whatever the label itself says.")]
        [SerializeField] private string sample = "شروع بازی";

        [Tooltip("Draw the report over the game.")]
        [SerializeField] private bool showOnScreen = true;

        [Tooltip("Write it to the log as well, for adb logcat.")]
        [SerializeField] private bool writeToLog = true;

        [SerializeField, Range(8, 48)] private int size = 18;

        private string _report = "(not built yet)";

        private void Start()
        {
            // A frame late: DirectFont builds its asset in LateUpdate, so asking during Start
            // reports a label that has not been set up yet and blames the package for it.
            Invoke(nameof(Build), 0.5f);
        }

        /// <summary>Works the report out again, from scratch.</summary>
        [ContextMenu("Build Report")]
        public void Build()
        {
            _report = Compose();

            if (writeToLog) { Debug.Log(_report, this); }
        }

        private string Compose()
        {
            var said = new StringBuilder();

            said.AppendLine("Unity DirectTMP — report");
            said.AppendLine($"platform      : {Application.platform}   screen {Screen.width}x{Screen.height}");

            DirectFont on = label != null ? label : FindFirst();

            if (on == null)
            {
                said.AppendLine("label         : NONE FOUND — no Direct Font component is in this scene.");
                said.AppendLine();
                said.AppendLine("That is the whole answer: nothing is shaping anything. Add a Direct Font");
                said.AppendLine("to the label and give it the .ttf.");
                return said.ToString();
            }

            said.AppendLine($"label         : {on.name}");

            Font file = on.Font;
            said.AppendLine($"font          : {(file == null ? "NONE — the component has no font, so it does nothing" : file.name)}");

            TMP_FontAsset asset = on.FontAsset;
            said.AppendLine($"font asset    : {(asset == null ? "NOT BUILT" : asset.name)}");

            if (asset == null)
            {
                said.AppendLine();
                said.AppendLine("No font asset means TextMeshPro could not rasterize this font at all.");
                said.AppendLine("Nearly always \"Include Font Data\" being off on the font's importer.");
                return said.ToString();
            }

            said.AppendLine($"characters    : {asset.characterTable.Count}");

            // ---- the two things that differ between the Editor and a build ----

            byte[] bytes = DirectFontSource.BytesFor(asset);

            said.AppendLine(bytes == null || bytes.Length == 0
                ? "font bytes    : NOT FOUND  ← the font's own joining rules cannot be read"
                : $"font bytes    : found, {bytes.Length:N0} bytes");

            said.AppendLine(DirectFontBytes.Table == null
                ? "baked table   : MISSING — run 'Bake Fonts For Build', or build again"
                : $"baked table   : present, {DirectFontBytes.Table.Rows.Length} font(s)");

            said.AppendLine(DirectFontJoiner.CanRasterizeByGlyphIndex
                ? "glyph adder   : available"
                : "glyph adder   : STRIPPED — TryAddGlyphInternal was removed from this build");

            DirectFontJoiner joiner = DirectFontJoiner.For(asset);

            said.AppendLine($"joining rules : {(joiner != null && joiner.HasJoiningRules ? "READ" : "NOT READ")}");

            if (joiner != null && !string.IsNullOrEmpty(joiner.Problem))
            {
                said.AppendLine($"problem       : {joiner.Problem}");
            }

            // ---- what actually comes out ----

            string source = !string.IsNullOrEmpty(sample) ? sample : (on.GetComponent<TMP_Text>()?.text ?? string.Empty);

            if (!string.IsNullOrEmpty(source))
            {
                string shaped = DirectTMP.Prepare(source, asset);

                said.AppendLine();
                said.AppendLine($"in            : {Codepoints(source)}");
                said.AppendLine($"out           : {Codepoints(shaped)}");
                said.AppendLine($"where from    : {Sources(shaped, joiner, asset)}");
            }

            said.AppendLine();
            said.AppendLine(Verdict(bytes, joiner));

            return said.ToString();
        }

        /// <summary>The one line worth reading, when nothing else is.</summary>
        private static string Verdict(byte[] bytes, DirectFontJoiner joiner)
        {
            if (!DirectFontJoiner.CanRasterizeByGlyphIndex)
            {
                return "VERDICT: managed stripping took TryAddGlyphInternal out of this build, so the\n"
                     + "font's own shapes cannot be registered. Add a link.xml preserving\n"
                     + "TMPro.TMP_FontAsset, or set Managed Stripping Level to Minimal.";
            }

            if (bytes == null || bytes.Length == 0)
            {
                return "VERDICT: the font file is not in this build, so the joining rules could not be\n"
                     + "read and every shape came from the legacy presentation block. Run\n"
                     + "'Unity DirectTMP ▸ Bake Fonts For Build' and build again.";
            }

            if (joiner == null || !joiner.HasJoiningRules)
            {
                return "VERDICT: the file is here but carries no OpenType joining rules for Arabic, so\n"
                     + "the legacy block is all there is. This font cannot do better — try one that\n"
                     + "has a GSUB table, such as Vazirmatn or Noto Sans Arabic.";
            }

            return "VERDICT: joining is working from the font's own rules. Anything still wrong is in\n"
                 + "the font itself or in the text, not in the setup.";
        }

        private static string Codepoints(string text)
        {
            var said = new StringBuilder();

            foreach (char c in text)
            {
                if (c == ' ') { said.Append("· "); continue; }

                said.Append($"{(int)c:X4} ");
            }

            return said.ToString().TrimEnd();
        }

        /// <summary>
        /// Per shaped codepoint: <c>gsub</c> for the font's own rules, <c>cmap</c> for the legacy
        /// block, <c>-</c> for a character that was never reshaped. A word of <c>cmap</c> is the
        /// answer to nearly every "why is this one letter wrong".
        /// </summary>
        private static string Sources(string shaped, DirectFontJoiner joiner, TMP_FontAsset asset)
        {
            if (joiner == null) { return "(no joiner)"; }

            var said = new StringBuilder();

            foreach (char c in shaped)
            {
                if (c == ' ') { said.Append("· "); continue; }

                string from = joiner.SourceOf(c);

                if (!string.IsNullOrEmpty(from)) { said.Append(from + " "); }
                else if (asset != null && !asset.HasCharacter(c)) { said.Append("MISSING "); }
                else { said.Append("- "); }
            }

            return said.ToString().TrimEnd();
        }

        private static DirectFont FindFirst()
        {
#if UNITY_2022_2_OR_NEWER
            DirectFont[] all = FindObjectsByType<DirectFont>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            DirectFont[] all = FindObjectsOfType<DirectFont>(true);
#endif
            foreach (DirectFont one in all)
            {
                if (one != null && one.Font != null) { return one; }
            }

            return all.Length > 0 ? all[0] : null;
        }

        private void OnGUI()
        {
            if (!showOnScreen) { return; }

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                alignment = TextAnchor.UpperLeft,
                wordWrap = false,
                richText = false,
            };

            style.normal.textColor = Color.white;

            Vector2 wanted = style.CalcSize(new GUIContent(_report));
            var area = new Rect(12f, 12f, wanted.x + 24f, wanted.y + 24f);

            GUI.color = new Color(0f, 0f, 0f, 0.82f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(area.x + 12f, area.y + 12f, area.width, area.height), _report, style);

            if (GUI.Button(new Rect(area.x + 12f, area.yMax + 6f, 140f, 40f), "Refresh")) { Build(); }
        }
    }
}
