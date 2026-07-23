# Multilingual Demo

One scene, one font, twelve languages.

This sample shows the whole point of Unity DirectTMP in a single screen: assign
**one** font file and watch English, Japanese, Chinese, Korean, Persian, Arabic,
Hebrew, Russian, Greek, Thai, Hindi and emoji all render from it — no Font Asset
Creator, no character ranges, no □□□.

## How to run it

1. Import this sample from **Package Manager → Unity DirectTMP → Samples → Import**.
2. Create an empty GameObject in a scene and add the **Multilingual Demo** component
   (`Add Component → Scripts → UnityDirectTMP.Samples → Multilingual Demo`).
3. Drag a broad pan-Unicode font file into **Font File** — something like
   [Noto Sans](https://fonts.google.com/noto) works beautifully. A single Noto
   family won't cover *every* script, so for the widest coverage also create a
   **Fallback Chain** (`Assets → Create → Unity DirectTMP → Fallback Chain`),
   line up a few Noto variants, and drop it into **Fallback Chain**.
4. Press **Play**.

The script builds its own Canvas and one `TextMeshProUGUI` per line at runtime,
adds a `DirectFont` to each, and points it at your font file. In a real project
you'd skip the script entirely and just add a **Direct Font** component next to
any TextMeshPro label in the Inspector.

## What to notice

- Every line is drawn from the **same** `.ttf`. Glyphs are rasterized on first
  use, so the atlas only ever holds the characters actually shown.
- Nothing was baked ahead of time. Change the text to your own name in any of
  these scripts and it still renders.

> **Font licensing is yours to check** — shipping a `.ttf` inside a build counts
> as redistribution. Noto is licensed under the SIL Open Font License, which
> permits bundling; always confirm the license of whatever font you ship.
