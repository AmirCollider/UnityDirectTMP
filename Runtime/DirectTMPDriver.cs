// ==========================================
// DirectTMPDriver
// Keeps every label pointed at your font, and keeps
// Arabic-script text joined.
//
// ==========================================
// WHY NOTHING WRITES TO label.text
// ==========================================
// The obvious way to fix a label's text is to shape it
// and assign the result back. Every version of this
// package before now did that, and it is the source of
// most of what went wrong with it:
//
//   * assigning .text looks exactly like the game
//     setting new text, so the shaped string gets shaped
//     again, and reordered again - which puts it back
//     the wrong way round;
//   * .text is serialized, so doing it in the Editor
//     marks the scene dirty and saves shaped text into
//     the scene file, where it stays after the package
//     is removed;
//   * a label whose text the game sets every frame ends
//     up in a fight with whatever wrote to it last.
//
// TextMeshPro has a hook for precisely this:
// ITextPreprocessor. It is called during text
// generation, its return value is what gets drawn, and
// the label's own .text is never touched. So the game
// reads back exactly the string it set, nothing is
// serialized, nothing is shaped twice, and edit mode and
// play mode behave the same.
//
// The driver's whole job is therefore to make sure every
// label HAS the preprocessor - including labels created
// in the tenth minute of play, by a package you did not
// write, in an additively loaded scene.
// ==========================================
using TMPro;
using UnityEngine;

namespace UnityDirectTMP
{
    /// <summary>
    /// Shapes text on its way to being drawn. One instance, shared by every
    /// label, holding no per-label state.
    /// </summary>
    public sealed class DirectTMPPreprocessor : ITextPreprocessor
    {
        /// <summary>The one instance every label is given.</summary>
        public static readonly DirectTMPPreprocessor Instance = new DirectTMPPreprocessor();

        /// <summary>
        /// Called by TextMeshPro with the label's own text, and returns what
        /// should actually be drawn.
        /// </summary>
        public string PreprocessText(string text)
        {
            if (!DirectTMP.Active || !DirectTMP.FixRightToLeft) { return text; }
            return DirectTMP.Prepare(text);
        }
    }

    /// <summary>
    /// The one component this package adds - to a hidden object of its own,
    /// never to yours.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class DirectTMPDriver : MonoBehaviour
    {
        private static DirectTMPDriver s_instance;

        /// <summary>Creates the driver if it is not already running.</summary>
        public static void Ensure()
        {
            if (!Application.isPlaying || s_instance != null) { return; }

            var host = new GameObject("DirectTMP") { hideFlags = HideFlags.HideAndDontSave };
            s_instance = host.AddComponent<DirectTMPDriver>();
            DontDestroyOnLoad(host);
        }

        /// <summary>Shuts the driver down.</summary>
        public static void Stop()
        {
            if (s_instance == null) { return; }

            if (Application.isPlaying) { Destroy(s_instance.gameObject); }
            else { DestroyImmediate(s_instance.gameObject); }

            s_instance = null;
        }

        private void LateUpdate() => Tick();

        /// <summary>
        /// One pass over every label. Static so the Editor can drive it in
        /// edit mode, where there is no Update loop to hang off.
        ///
        /// <paramref name="assignFont"/> is false in the Editor: a label's
        /// font is serialized, so assigning it there would mark the scene
        /// dirty. The project-wide fallback covers edit mode instead, which
        /// draws the same glyphs and writes nothing to disk.
        /// </summary>
        public static int Tick(bool assignFont = true)
        {
            if (!DirectTMP.Active) { return 0; }

            int touched = 0;
            foreach (TMP_Text label in DirectTMP.AllLabels())
            {
                if (label == null) { continue; }

                bool changed = false;

                if (assignFont && DirectTMP.Apply(label)) { changed = true; }

                if (!ReferenceEquals(label.textPreprocessor, DirectTMPPreprocessor.Instance))
                {
                    label.textPreprocessor = DirectTMPPreprocessor.Instance;

                    // The text was already generated once without us. Ask for
                    // it again, or the label keeps its unshaped layout until
                    // something else happens to it.
                    label.SetVerticesDirty();
                    label.SetLayoutDirty();
                    changed = true;
                }

                if (changed) { touched++; }
            }

            return touched;
        }

        /// <summary>
        /// Takes the preprocessor off every label, so removing the package
        /// leaves nothing behind.
        /// </summary>
        public static int Detach()
        {
            int removed = 0;
            foreach (TMP_Text label in DirectTMP.AllLabels())
            {
                if (label == null) { continue; }
                if (!ReferenceEquals(label.textPreprocessor, DirectTMPPreprocessor.Instance)) { continue; }

                label.textPreprocessor = null;
                label.SetVerticesDirty();
                label.SetLayoutDirty();
                removed++;
            }
            return removed;
        }
    }
}
