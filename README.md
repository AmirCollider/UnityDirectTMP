<a id="top"></a>
<p align="center">
  <img src="Docs~/mascot.svg" alt="Inky, the Unity DirectTMP mascot" width="150"/>
</p>

<h1 align="center">🖋️ Unity DirectTMP</h1>

<p align="center"><em>Point TextMeshPro at a .ttf. That's the whole tool.</em></p>
<p align="center"><em>فایل فونت رو مستقیم بده به TextMeshPro — همین.</em></p>

<p align="center">
  <a href="#english">English</a> ・
  <a href="#persian">فارسی</a>
</p>

<p align="center">
  <img alt="license" src="https://img.shields.io/badge/license-MIT-14808C?style=flat-square">
  <img alt="unity version" src="https://img.shields.io/badge/Unity-2021.3%2B-0B5A63?style=flat-square&logo=unity&logoColor=white">
  <img alt="textmeshpro" src="https://img.shields.io/badge/TextMeshPro-3.0%2B-0B5A63?style=flat-square">
  <img alt="shaping" src="https://img.shields.io/badge/shaping-verified%20vs%20HarfBuzz-F0A73E?style=flat-square">
</p>

---

<a id="english"></a>
## 🖋️ How to use it

1. Select your TextMeshPro label.
2. **Add Component ▸ Unity DirectTMP ▸ Direct Font**
   (or select a Canvas and use **Unity DirectTMP ▸ Add Direct Font to Selection**,
   which does every label underneath it).
3. Drop a `.ttf` or `.otf` into **Font**.

That label is now drawn from that file. Glyphs are rasterized the first time
they are drawn, so there is no character set to choose and no atlas to rebuild.

One component, one label, one font. Nothing is applied project-wide, and two
labels with two different fonts do not know about each other.

From code, if you prefer:

```csharp
DirectTMP.Apply(label, myFont);                 // same thing the component does
label.font = DirectTMP.Load(myFont);            // or just the font asset
label.font = DirectTMP.LoadFromFile(path);      // a .ttf on disk, at runtime
```

### The fields

| Field | What it does |
|---|---|
| **Font** | The font file. This is the only one you need. |
| **Persian / Arabic** | Used when the text is mostly Persian, Arabic or Urdu. Empty = use **Font**. |
| **日本語 / 中文 / 한국어** | Used when the text is mostly Japanese, Chinese or Korean. Empty = use **Font**. |
| **English / Latin** | Used when the text is mostly English or another Latin language. Empty = use **Font**. |
| **Own material** | Give this label its own material, so an outline on it does not change every other label using the same font. |
| **Join Persian / Arabic** | Joining and reading order. Costs nothing for text with no Arabic script in it. |

---

## 🌏 A font per language, on one label

A label that shows Persian today and Japanese tomorrow — a player name, a chat
line, a localized string — wants a different font in each case, and no single
font is good at both.

Fill in the language fields you have fonts for and leave the rest empty. Each
time the text changes, the writing system it is actually in is worked out and
the matching font is used.

It counts characters rather than looking at the first letter, so
`"Unity ۱۲۳ سلام دنیا"` is Persian, not English. Digits, spaces and punctuation
do not vote.

Whatever you assign also goes into the chosen font's fallback list, so a
Persian line with an English word in it still finds the English glyphs even
though only one font can be the label's own.

---

## 🎨 The outline that changed every label

Every TextMeshPro label using the same font asset shares **one** material. So an
outline, a face dilate or a glow set on one label is set on **every** label using
that font.

That is not a bug in TextMeshPro and it is not a bug in this package — it is what
"shared material" means. But it surprises everybody, and it surprises people
harder when a tool has just pointed forty labels at the same font.

**Own material** gives the label a material of its own, so its outline stays its
own. It is on by default, because a surprise that costs one draw call is better
than a surprise that changes every label in the scene. Turn it off on labels
that are meant to share a look and the batching comes back.

---

## 🩺 If Persian or Arabic used to come apart

That was a real bug, it is fixed, and it is worth explaining because the shape
of it is confusing: **some words looked right and most did not.**

Words made only of letters that never join to the left — درد, راز, زود, دارا,
رود — look identical whether they have been shaped or not. Those were the ones
that worked. Everything else broke.

The cause: shaping used to emit Unicode **presentation forms** — the legacy
`U+FE70..FEFF` and `U+FB50..FBFF` blocks — and then ask the font whether it had
them. Fonts built before OpenType do. Modern Persian faces — Vazirmatn, Sahel,
Shabnam, IRANSans, Noto Sans Arabic, essentially anything a designer reaches
for — do **not**. They carry the plain letters and express joining in their
OpenType `GSUB` table, because that is what a real shaping engine reads.

So the font said no, and the whole word was left unjoined.

The shapes were never missing. Every font that sets Arabic has an initial peh —
that is what its `init` feature is *for*. Only the **codepoints** were missing.
So Unity DirectTMP now reads the font's own `GSUB`, asks it which glyph it would
use for each letter in each position, and registers that glyph in the font asset
under a codepoint the shaper can emit.

Two more things that were wrong and are not any more:

- **A letter's joining class is a fact about Unicode, not about the font.** It
  used to be derived from which presentation codepoints a font happened to
  contain, so one missing glyph changed the shape of the letter *next to* it —
  which is how a gap in a font for ر showed up as a broken ش.
- **Letters Unicode never gave a presentation codepoint to now join too** —
  most of Arabic Extended-A, and much of what Kurdish, Sindhi and the African
  orthographies use.

### How it is checked

The shaper is diffed **glyph for glyph against HarfBuzz** — the shaping engine
in Chrome, Firefox, Android and macOS — over a Persian/Arabic/Urdu corpus on
five fonts, including one built specifically to have no presentation codepoints
at all. Every word matches.

### What it does not do

- **Contextual alternates.** A font with several stylistic variants of one
  joined shape gets the standard one. The text joins correctly; it is not
  Nastaliq typesetting.
- **Mark positioning (`GPOS`).** Harakat sit where the font's default advance
  puts them.
- **A rich-text tag in the middle of a right-to-left sentence** splits it into
  two runs that are each ordered correctly but laid out left-to-right relative
  to each other. A tag around a whole phrase — which is nearly every tag in
  nearly every project — is fine.

---

## 🌏 Everything else

Japanese, Chinese, Korean, Cyrillic, Greek, Thai, symbols: nothing to
configure. If the font has the glyph, the label draws it.

---

## 🧩 Requirements

- Unity **2021.3** or newer
- **TextMeshPro** (bundled with Unity)

If a font produces nothing, the component's Inspector names the reason. The four usual causes
are all project-level and none is visible from a scene: TMP Essential Resources
not imported, **Include Font Data** off on the font importer, the importer's
**Character** mode not set to Dynamic, or a missing distance-field shader.

---

## 📦 Installing

Package Manager ▸ **+** ▸ *Add package from git URL…*

```
https://github.com/AmirCollider/UnityDirectTMP.git
```

---

<a id="persian"></a>
## 🇮🇷 فارسی

### چطور کار می‌کند

۱. لیبل TextMeshPro را انتخاب کنید.
۲. **Add Component ▸ Unity DirectTMP ▸ Direct Font**
۳. یک فایل `.ttf` یا `.otf` داخل فیلد **Font** بگذارید.

همان لیبل از همان فایل کشیده می‌شود. هیچ Font Asset ای ساخته نمی‌شود و لازم
نیست از قبل مشخص کنید کدام کاراکترها را می‌خواهید.

یک کامپوننت، یک لیبل، یک فونت. هیچ چیزی روی کل پروژه اعمال نمی‌شود و دو لیبل با
دو فونت مختلف کاری به هم ندارند.

### فیلدها

| فیلد | کارش چیست |
|---|---|
| **Font** | فایل فونت. تنها فیلدی که واقعاً لازم دارید. |
| **Persian / Arabic** | وقتی متن بیشتر فارسی، عربی یا اردو باشد. خالی = همان **Font**. |
| **日本語 / 中文 / 한국어** | وقتی متن بیشتر ژاپنی، چینی یا کره‌ای باشد. خالی = همان **Font**. |
| **English / Latin** | وقتی متن بیشتر انگلیسی باشد. خالی = همان **Font**. |
| **Own material** | متریال مخصوص همین لیبل، تا OutLine روی بقیه‌ی لیبل‌ها اثر نگذارد. |
| **Join Persian / Arabic** | چسبیدن حروف و ترتیب راست‌به‌چپ. |

### یک فونت برای هر زبان، روی یک لیبل

فیلدهای زبان‌هایی را که فونت دارید پر کنید و بقیه را خالی بگذارید. هر بار که
متن عوض می‌شود، تشخیص داده می‌شود متن در چه خطی نوشته شده و فونت مربوطه استفاده
می‌شود.

تشخیص با شمردن کاراکترها انجام می‌شود نه با نگاه به حرف اول، پس
`"Unity ۱۲۳ سلام دنیا"` فارسی حساب می‌شود نه انگلیسی. عدد و فاصله و علائم رأی
نمی‌دهند.

### باگ OutLine

در TextMeshPro همه‌ی لیبل‌هایی که از یک Font Asset استفاده می‌کنند **یک**
متریال مشترک دارند. برای همین وقتی به یک متن OutLine می‌دهید، روی همه‌ی
متن‌هایی که همان فونت را دارند اعمال می‌شود.

این باگ TextMeshPro یا این پکیج نیست — معنیِ «متریال مشترک» همین است. اما همه را
غافلگیر می‌کند.

تیک **Own material** به این لیبل یک متریال مخصوص خودش می‌دهد تا OutLine اش مال
خودش بماند. به‌صورت پیش‌فرض روشن است. اگر چند لیبل قرار است ظاهر یکسان داشته
باشند خاموشش کنید تا batching برگردد.

### چرا قبلاً بعضی کلمات درست بود و بیشترشان نه

کلماتی که فقط از حروفِ «نچسبِ از چپ» ساخته شده‌اند — درد، راز، زود، دارا، رود —
چه شکل‌دهی بشوند چه نشوند، یکسان دیده می‌شوند. همان‌ها کار می‌کردند.

نسخه‌ی قبلی حروف را به **فرم‌های نمایشی یونیکد** تبدیل می‌کرد و بعد از فونت
می‌پرسید آیا آن‌ها را دارد. فونت‌های مدرن فارسی — وزیرمتن، ساحل، شبنم،
ایران‌سنس، نوتو — ندارند؛ آن‌ها قواعد چسبیدن را در جدول `GSUB` نگه می‌دارند. پس
جواب «نه» می‌شد و کل کلمه بدون جوین رها می‌شد.

حالا جدول `GSUB` خود فونت خوانده می‌شود و همان گلیفی که خود فونت می‌کشد داخل
Font Asset ثبت می‌شود. خروجی **گلیف‌به‌گلیف با HarfBuzz** روی پنج فونت تست شده.

---

<p align="center"><sub>MIT · <a href="CHANGELOG.md">Changelog</a></sub></p>
