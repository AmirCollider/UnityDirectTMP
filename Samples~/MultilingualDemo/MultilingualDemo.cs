// ==========================================
// MultilingualDemo
// One scene, one font, twelve languages. Drop this on
// an empty GameObject, assign a single broad font
// file (something like Noto Sans or a good pan-Unicode
// face), press Play, and watch every line render from
// that one .ttf - no Font Asset Creator, no character
// ranges, no tofu.
//
// It builds its own Canvas and labels at runtime so
// the sample is a single self-contained script; in a
// real project you'd just add a Direct Font component
// next to any TextMeshPro label in the Inspector.
// ==========================================
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityDirectTMP;

namespace UnityDirectTMP.Samples
{
    public sealed class MultilingualDemo : MonoBehaviour
    {
        [Tooltip("A single font file broad enough to cover many scripts (e.g. a Noto Sans variant).")]
        [SerializeField] private Font fontFile;

        [Tooltip("Optional ordered fallback chain, for when one file can't cover every script on its own.")]
        [SerializeField] private DirectFontFallbackChain fallbackChain;

        // The same twelve lines you'll see stacked on screen - all supplied by
        // one font file the moment each glyph is first drawn.
        private static readonly List<string> Lines = new List<string>
        {
            "English — Hello, world!",
            "日本語 — こんにちは世界",
            "中文 — 你好，世界",
            "한국어 — 안녕하세요 세계",
            "فارسی — سلام دنیا",
            "العربية — مرحبا بالعالم",
            "עברית — שלום עולם",
            "Русский — Привет, мир",
            "Ελληνικά — Γειά σου Κόσμε",
            "ไทย — สวัสดีชาวโลก",
            "हिन्दी — नमस्ते दुनिया",
            "Emoji — 🖋️ ✨ 🍰 ⭐",
        };

        private void Start()
        {
            if (fontFile == null)
            {
                Debug.LogWarning("[MultilingualDemo] Assign a Font File in the Inspector to see the demo.");
                return;
            }

            Canvas canvas = BuildCanvas();
            foreach (string line in Lines)
            {
                CreateLabel(canvas.transform, line);
            }
        }

        private static Canvas BuildCanvas()
        {
            var canvasGo = new GameObject("DirectTMP Demo Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            var layoutGo = new GameObject("Lines", typeof(VerticalLayoutGroup));
            layoutGo.transform.SetParent(canvasGo.transform, false);
            var rect = layoutGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.05f);
            rect.anchorMax = new Vector2(0.95f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var layout = layoutGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            return canvas;
        }

        private void CreateLabel(Transform parent, string text)
        {
            var go = new GameObject("Label", typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 44;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;

            // The one line that matters: hand this label the font file directly.
            var direct = go.AddComponent<DirectFont>();
            if (fallbackChain != null) { direct.SetFallbackChain(fallbackChain); }
            direct.SetFontFile(fontFile);
        }
    }
}
