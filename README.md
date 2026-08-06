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

1. **Window ▸ Unity DirectTMP**
2. Pick a `.ttf` or `.otf`.

That's it. Every TextMeshPro label in the project is now drawn from that file —
in the Editor, in Play mode and in the build. Glyphs are rasterized the first
time they are drawn, so there is no character set to choose in advance and no
atlas to rebuild.

There is one checkbox: **Join Persian / Arabic and read right-to-left**. Leave
it on; text with no Arabic script in it is returned untouched.

From code, if you prefer:

```csharp
DirectTMP.Use(myFont);                    // an imported Font
DirectTMP.Use(DirectFont.LoadFromFile(path));   // a .ttf on disk
```

**Nothing is added to your GameObjects, and no scene is modified.**

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

If a font produces nothing, the window names the reason. The four usual causes
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

1. **Window ▸ Unity DirectTMP**
2. یک فایل `.ttf` یا `.otf` انتخاب کنید.

تمام. از این به بعد همه‌ی لیبل‌های TextMeshPro در پروژه با همان فایل کشیده
می‌شوند — در ادیتور، در Play mode و در بیلد. هیچ Font Asset ای ساخته نمی‌شود و
لازم نیست از قبل مشخص کنید کدام کاراکترها را می‌خواهید.

فقط یک تیک دارد: **جوین شدن فارسی/عربی و راست‌به‌چپ**. روشن بگذارید؛ متنی که
حروف عربی ندارد اصلاً دست نمی‌خورد.

**هیچ کامپوننتی به هیچ GameObject ای اضافه نمی‌شود و هیچ صحنه‌ای تغییر
نمی‌کند.**

### چرا قبلاً بعضی کلمات درست بود و بیشترشان نه

این یک باگ واقعی بود و حالا درست شده.

کلماتی که فقط از حروفِ «نچسبِ از چپ» ساخته شده‌اند — درد، راز، زود، دارا، رود —
چه شکل‌دهی بشوند چه نشوند، یکسان دیده می‌شوند. همان‌ها کار می‌کردند. بقیه
خراب بودند.

دلیلش: نسخه‌ی قبلی حروف را به **فرم‌های نمایشی یونیکد** تبدیل می‌کرد
(بلوک‌های `U+FE70..FEFF` و `U+FB50..FBFF`) و بعد از فونت می‌پرسید آیا آن‌ها را
دارد یا نه. فونت‌های مدرن فارسی — وزیرمتن، ساحل، شبنم، ایران‌سنس، نوتو —
این بلوک‌ها را **ندارند**. آن‌ها حروف ساده را دارند و قواعد چسبیدن را در جدول
`GSUB` نگه می‌دارند.

پس جواب فونت «نه» بود و کل کلمه بدون جوین رها می‌شد.

اما آن شکل‌ها هیچ‌وقت گم نشده بودند. هر فونتی که عربی دارد، «پ» ابتدایی هم
دارد — کارِ ویژگی `init` دقیقاً همین است. فقط **کدپوینت‌ها** غایب بودند. حالا
Unity DirectTMP جدول `GSUB` خود فونت را می‌خواند، می‌پرسد برای هر حرف در هر
موقعیت کدام گلیف را می‌کشد، و همان گلیف را داخل Font Asset ثبت می‌کند.

### چطور تست شده

خروجی شکل‌دهی **گلیف‌به‌گلیف با HarfBuzz** مقایسه شده — همان موتوری که در
کروم، فایرفاکس، اندروید و مک‌اواس کار می‌کند — روی مجموعه‌ای از کلمات فارسی،
عربی و اردو و روی پنج فونت، از جمله فونتی که عمداً هیچ فرم نمایشی‌ای ندارد.
همه‌ی کلمات یکسان درآمدند.

---

<p align="center"><sub>MIT · <a href="CHANGELOG.md">Changelog</a></sub></p>
