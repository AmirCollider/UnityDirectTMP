// ==========================================
// MultilingualDemo
// Builds a canvas of labels in several writing systems,
// from a single .ttf, at runtime.
//
// Point `font` at any font file that carries the scripts
// you want and press Play. There is no font asset to
// build and no character set to choose - each glyph is
// rasterized the first time it is drawn.
//
// Each label gets its own Direct Font component, which
// is all DirectTMP.Apply does. Nothing is applied
// project-wide.
// ==========================================
using TMPro;
using UnityEngine;

namespace UnityDirectTMP.Samples
{
    /// <summary>Drop this on an empty GameObject, assign a font, press Play.</summary>
    public sealed class MultilingualDemo : MonoBehaviour
    {
        [Tooltip("Any .ttf or .otf imported into the project.")]
        [SerializeField] private Font font;


        private static readonly string[] Lines =
        {
            "English — the quick brown fox",
            "日本語 — 素早い茶色の狐",
            "한국어 — 빠른 갈색 여우",
            "Русский — быстрая бурая лиса",
            "فارسی — روباه قهوه‌ای چابک",
            "العربية — الثعلب البني السريع",
            "עברית — השועל החום הזריז",
        };

        private void Start()
        {
            if (font == null)
            {
                Debug.LogWarning("[MultilingualDemo] Assign a font in the Inspector.");
                return;
            }

            Build();
        }

        private void Build()
        {
            var canvasObject = new GameObject("Demo Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();

            for (int i = 0; i < Lines.Length; i++)
            {
                var labelObject = new GameObject($"Line {i}");
                labelObject.transform.SetParent(canvasObject.transform, false);

                TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();

                // The one line that matters: this label is now drawn from the
                // font file, and its Persian and Arabic are joined.
                DirectTMP.Apply(label, font);

                label.text = Lines[i];
                label.fontSize = 32;
                label.alignment = TextAlignmentOptions.Center;

                var rect = label.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(900, 48);
                rect.anchoredPosition = new Vector2(0, 160 - (i * 52));
            }
        }
    }
}
