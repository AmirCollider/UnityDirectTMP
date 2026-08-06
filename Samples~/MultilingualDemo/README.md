# Multilingual Demo

One font file, several writing systems, at runtime.

This sample is the whole point of Unity DirectTMP on one screen: assign **one**
font file and watch English, Japanese, Korean, Russian, Persian, Arabic and
Hebrew all render from it — no Font Asset Creator, no character ranges, no □□□.

## How to run it

1. Import from **Package Manager ▸ Unity DirectTMP ▸ Samples ▸ Import**.
2. Add the **Multilingual Demo** component to an empty GameObject
   (`Add Component ▸ Scripts ▸ UnityDirectTMP.Samples ▸ Multilingual Demo`).
3. Drag a font into **Font**. Something broad like
   [Noto Sans](https://fonts.google.com/noto) shows the most scripts at once.
4. Press **Play**.

## What it is doing

The whole of it is one line:

```csharp
DirectTMP.Use(font);
```

From that point every TextMeshPro label in the project is drawn from that file,
including the ones the demo creates afterwards. Glyphs are rasterized the first
time they are drawn.

Persian and Arabic are joined and put in reading order automatically — the
joined shapes come from the font's own OpenType tables, so a modern face that
carries no legacy presentation codepoints works exactly as well as an old one.

## No font covers everything

No single font has every script. A character the font does not have will still
draw as a box — that is the font's coverage, not a failure of the tool. Pick a
face that carries the scripts you need, or set up TextMeshPro's own fallback
list with a second font.
