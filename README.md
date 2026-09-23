<a id="top"></a>
<p align="center">
  <img src="Docs~/mascot.svg" alt="Inky, the Unity DirectTMP mascot" width="150"/>
</p>

<h1 align="center">🖋️ Unity DirectTMP</h1>

<p align="center">
  <em>Point TextMeshPro at a <code>.ttf</code>. That's the whole tool.</em><br>
  <em>فایل فونت رو مستقیم بده به TextMeshPro — همین.</em><br>
  <em>TextMeshPro に <code>.ttf</code> を渡すだけ。それだけです。</em>
</p>

<p align="center">
  <a href="#english"><b>English</b></a>
  ・
  <a href="#persian"><b>فارسی</b></a>
  ・
  <a href="#japanese"><b>日本語</b></a>
</p>

<p align="center">
  <img alt="license" src="https://img.shields.io/badge/license-MIT-14808C?style=flat-square">
  <img alt="unity version" src="https://img.shields.io/badge/Unity-2021.3%2B-0B5A63?style=flat-square&logo=unity&logoColor=white">
  <img alt="textmeshpro" src="https://img.shields.io/badge/TextMeshPro-3.0%2B-0B5A63?style=flat-square">
  <img alt="shaping" src="https://img.shields.io/badge/shaping-verified%20vs%20HarfBuzz-F0A73E?style=flat-square">
  <img alt="price" src="https://img.shields.io/badge/price-free-14808C?style=flat-square">
</p>

<p align="center">
  <a href="https://amircollider.com/en/unity-directtmp"><b>🌐 Plugin page</b></a>
  ・
  <a href="https://amircollider.com/unity-directtmp"><b>🌐 صفحهٔ افزونه</b></a>
  ・
  <a href="https://amircollider.com/ja/unity-directtmp"><b>🌐 プラグインページ</b></a>
  ・
  <a href="https://amircollider.com/en/videos"><b>🎬 Video tutorials · آموزش ویدیویی · 動画</b></a>
</p>

<p align="center" dir="rtl">
  <b>افزونه‌ی رایگان و متن‌باز فارسی‌نویسی یونیتی</b> — حل مشکل حروف جدا و برعکسِ فارسی و عربی در TextMeshPro،
  بدون فارسی‌ساز و بدون Font Asset Creator.
</p>

---

<a id="english"></a>

## 🇬🇧 English

**No Font Asset Creator. No character set. No `□□□`.**
Drop a font file on a label and it is drawn from that file — every glyph, every
script, rasterized the moment it is first needed.

And Persian, Arabic and Urdu come out **joined and in reading order**, using the
font's own OpenType tables — so modern faces like Vazirmatn, Sahel, Shabnam,
IRANSans and Noto Sans Arabic work exactly as they were designed to.

### 📦 Install

Package Manager ▸ **+** ▸ *Add package from git URL…*

```
https://github.com/AmirCollider/UnityDirectTMP.git
```

### 🚀 Use it

1. Select your TextMeshPro label.
2. **Add Component ▸ Unity DirectTMP ▸ Direct Font**
3. Drop a `.ttf` or `.otf` into **Font**.

That's it. One component, one label, one font — nothing is applied
project-wide, and two labels with two different fonts never interfere.

> 💡 Have a whole UI to convert? Select the Canvas and use
> **Unity DirectTMP ▸ Add Direct Font to Selection** — it does every label underneath it.

From code, if you prefer:

```csharp
DirectTMP.Apply(label, myFont);              // same thing the component does
DirectTMP.Outline(label, 0.2f, Color.black); // an outline on this label only
label.font = DirectTMP.Load(myFont);         // just the font asset
label.font = DirectTMP.LoadFromFile(path);   // a .ttf on disk, at runtime
```

### 🎛️ The fields

| Field | What it does |
|---|---|
| **Font** | The font file. The only one you actually need. |
| **Persian / Arabic** | Used when the text is mostly Persian, Arabic or Urdu. Empty = use **Font**. |
| **日本語 / 中文 / 한국어** | Used when the text is mostly Japanese, Chinese or Korean. Empty = use **Font**. |
| **English / Latin** | Used when the text is mostly English or another Latin language. Empty = use **Font**. |
| **Кириллица** | Used when the text is mostly Russian or another Cyrillic language. Empty = use **Font**. |
| **Emoji 😀** | Used when the text is mostly emoji — and added as a fallback ahead of everything else, so emoji render *inside* text of any other language. No text font carries colour emoji, so without this they are missing glyphs whatever else is filled in. |
| **Split 日本語 / 中文 / 한국어** | Three optional fields, one per language, behind a foldout. Filled in, the language wins; empty, the CJK field above is used; with neither, **Font**. |
| **Any other script** | A list you add to: a name, the Unicode ranges, and a font. For anything with no field of its own. See below. |
| **Outline ▸ Width** | How thick an outline is drawn around this label's letters. 0 is no outline. |
| **Outline ▸ Colour** | The colour of that outline. |
| **Own material** | Gives this label its own material, so anything set on it stays its own. |
| **Join Persian / Arabic** | Letter joining and right-to-left reading order. Free for text with no Arabic script. |
| **Fix wrapped lines** | Keeps wrapped right-to-left lines in the correct order. |

#### Any other script

Hebrew, Thai, Devanagari, Armenian, Georgian, cuneiform — each is somebody's
whole project, and none of them is ever going to get a field of its own. So the
list is open. Press **Add a script** and fill in three things:

| | |
|---|---|
| **Name** | `Cuneiform` — for your own benefit. Nothing depends on it. |
| **Font** | `NotoSansCuneiform-Regular.ttf` |
| **Whole font** | Tick this and the font is used for **every character it actually has**, read from the font's own table. The ranges below are then ignored. |
| **Ranges** | `12000-123FF, 12400-1247F` — hex, straight off the Unicode chart. Only needed if you did not tick **Whole font**. |

Under each rule the Inspector shows what the font **really** covers — how many
codepoints, and which ranges — with a **Copy to Ranges** button.

> **If your script shows empty boxes, look at that line first.** Fonts for
> unusual scripts are very often drawn onto ASCII slots rather than the real
> codepoints: the glyphs are at `a`, `b`, `c`, and `U+12000` is not in the file
> at all. Such a font renders nothing for correctly encoded text, in any tool.
> If the Inspector says it covers `0020-007E`, that is what happened — type
> Latin letters to get its shapes, or tick **Whole font**.

Ranges accept the forms people actually type: `12000-123FF`, `U+12000-U+123FF`,
`0x12000-0x12FFF`, a single codepoint on its own, and the en dash a copy-paste
out of a Unicode PDF leaves behind. A range written backwards is swapped rather
than refused. One typo in a list of six costs that range and not the other five,
and the Inspector tells you which rules are live, which are being ignored, and
why — a rule that silently matches nothing is the worst of the three outcomes.

Rules are checked **before** the built-in table, so a rule can also override a
script the package already knows about. Ties go to the built-in.

Each rule's font is added as a fallback too, so a Persian sentence with one
Hebrew word in it finds that word's letters.

### ✨ Why people keep it

- **🌏 A font per language, on one label.** Fill in the language fields you have
  fonts for. Each time the text changes, the script it is *actually* in is
  detected and the matching font is used — by counting characters, so
  `"Unity ۱۲۳ سلام دنیا"` is Persian, not English.
- **🎨 An outline that belongs to one label.** In TextMeshPro every label sharing
  a font shares **one** material, so an outline on one is an outline on all. Set
  **Outline ▸ Width** here instead: it goes on a material this label owns, and it
  is kept on the component — so it survives a recompile, play mode and the build,
  which an outline set on a generated material cannot.
- **🩺 Persian that actually reads.** Joined shapes are read from the font's own
  `GSUB` table and verified **glyph-for-glyph against HarfBuzz** — the engine
  behind Chrome, Firefox, Android and macOS — across five fonts.
- **🧩 Everything else, free.** Japanese, Chinese, Korean, Cyrillic, Greek, Thai
  and symbols: nothing to configure. If the font has the glyph, you get it —
  and for the scripts a font *doesn't* cover, give them one in **Any other
  script**. Colour **emoji** are the one case no text font ever covers, so they
  get a field of their own.
- **🤝 Polite.** Rich-text tags are never shaped or reordered, and `label.text`
  is never written to.

<details>
<summary><b>Under the hood, and the honest limits</b></summary>

<br>

Shaping used to emit Unicode **presentation forms** (`U+FE70..FEFF`,
`U+FB50..FBFF`) and ask the font whether it had them. Fonts built before
OpenType do; modern Persian faces do **not** — they carry the plain letters and
express joining in `GSUB`. So the font said no and the word stayed unjoined.
Unity DirectTMP now reads the font's own `GSUB`, asks which glyph it would use
for each letter in each position, and registers that glyph in the font asset.

A letter's joining class is also treated as a fact about **Unicode**, not about
the font — so one missing glyph can no longer change the shape of the letter
next to it — and letters Unicode never gave a presentation codepoint to (most of
Arabic Extended-A, much of Kurdish, Sindhi and the African orthographies) join too.

The font asset is built at load and has no file behind it, and so does the
material that comes with it. That is what makes the Font Asset Creator
unnecessary, and it is also why the outline is a field on the component: a
material that cannot be saved cannot remember an outline you set on it, and
every reload — a recompile, play mode, reopening the scene — hands the label a
fresh one. Anything else you set on a material by hand has the same lifetime, so
set it from code after the label is up, or ask for a field for it.

What it does **not** do: contextual alternates (you get the standard joined
shape, not Nastaliq typesetting), mark positioning (`GPOS` — harakat sit at the
font's default advance), and a rich-text tag placed *mid-sentence* inside a
right-to-left line splits it into two correctly-ordered runs laid out
left-to-right relative to each other. A tag around a whole phrase is fine.

</details>

### 🧰 Requirements

Unity **2021.3+** and **TextMeshPro** (bundled with Unity). Nothing else.

> If a font produces nothing, the component's Inspector names the reason —
> usually TMP Essential Resources not imported, **Include Font Data** off on the
> font importer, its **Character** mode not set to Dynamic, or a missing
> distance-field shader.

### 🔗 Links

**[Plugin page](https://amircollider.com/en/unity-directtmp)**
・
**[amircollider.com](https://amircollider.com/en)**
・
[Changelog](CHANGELOG.md)

<p align="right"><a href="#top">↑ back to top</a></p>

---

<a id="persian"></a>

<div dir="rtl">

## 🇮🇷 فارسی

**Unity DirectTMP یک افزونه‌ی رایگان و متن‌باز (MIT) برای فارسی‌نویسی در یونیتی است.**
اگر متن فارسی در یونیتی و TextMeshPro **جدا جدا و برعکس** نشان داده می‌شود، یا به‌جای حروف
مربع خالی (`□`) می‌بینی، همین پکیج حلش می‌کند — بدون فارسی‌ساز جداگانه، بدون RTLTMPro و
بدون دست‌کاری رشته‌ها. [صفحه‌ی افزونه و ویدیوهای آموزشی فارسی](https://amircollider.com/unity-directtmp)

**بدون Font Asset Creator، بدون انتخاب کاراکتر، بدون `□□□`.**
فایل فونت را روی لیبل بگذارید و همان لیبل مستقیماً از همان فایل کشیده می‌شود —
هر گلیف، هر خط، درست همان لحظه‌ای که لازم شود.

و فارسی، عربی و اردو **چسبیده و با ترتیب درست** در می‌آیند، آن هم با استفاده از
جدول‌های OpenType خودِ فونت — پس وزیرمتن، ساحل، شبنم، ایران‌سنس و نوتو دقیقاً
همان‌طور کار می‌کنند که طراحی شده‌اند.

### 📦 نصب

Package Manager ‏▸ **+** ‏▸ *Add package from git URL…*

```
https://github.com/AmirCollider/UnityDirectTMP.git
```

### 🚀 استفاده

۱. لیبل TextMeshPro خود را انتخاب کنید.
۲. **Add Component ▸ Unity DirectTMP ▸ Direct Font**
۳. یک `.ttf` یا `.otf` داخل فیلد **Font** بیندازید.

تمام. یک کامپوننت، یک لیبل، یک فونت — هیچ چیزی روی کل پروژه اعمال نمی‌شود و دو
لیبل با دو فونت مختلف هیچ کاری به هم ندارند.

> 💡 یک UI کامل دارید؟ Canvas را انتخاب کنید و
> **Unity DirectTMP ▸ Add Direct Font to Selection** را بزنید — همهٔ لیبل‌های زیرش را انجام می‌دهد.

اگر کد را ترجیح می‌دهید:

```csharp
DirectTMP.Apply(label, myFont);              // همان کاری که کامپوننت می‌کند
DirectTMP.Outline(label, 0.2f, Color.black); // OutLine فقط روی همین لیبل
label.font = DirectTMP.Load(myFont);         // فقط خودِ فونت‌اَسِت
label.font = DirectTMP.LoadFromFile(path);   // یک .ttf روی دیسک، در زمان اجرا
```

### 🎛️ فیلدها

| فیلد | کارش چیست |
|---|---|
| **Font** | فایل فونت. تنها فیلدی که واقعاً لازم دارید. |
| **Persian / Arabic** | وقتی متن بیشتر فارسی، عربی یا اردو باشد. خالی = همان **Font**. |
| **日本語 / 中文 / 한국어** | وقتی متن بیشتر ژاپنی، چینی یا کره‌ای باشد. خالی = همان **Font**. |
| **English / Latin** | وقتی متن بیشتر انگلیسی یا لاتین باشد. خالی = همان **Font**. |
| **Кириллица** | وقتی متن بیشتر روسی یا سیریلیک باشد. خالی = همان **Font**. |
| **Emoji 😀** | وقتی متن بیشتر ایموجی باشد — و جلوتر از همه به‌عنوان فالبک هم اضافه می‌شود، تا ایموجی *داخل* متن هر زبان دیگری هم نمایش داده شود. هیچ فونت متنی ایموجی رنگی ندارد، پس بدون این فیلد هر چیز دیگری هم پر باشد باز گلیف کم می‌آید. |
| **تفکیک 日本語 / 中文 / 한국어** | سه فیلد اختیاری، یکی برای هر زبان، زیر یک فولدآوت. پر باشد همان زبان برنده است؛ خالی باشد فیلد CJK بالا؛ هیچ‌کدام نباشد **Font**. |
| **هر خط دیگر** | یک لیست که خودت به آن اضافه می‌کنی: یک نام، بازه‌های یونیکد، و یک فونت. برای هر چیزی که فیلد مخصوص خودش را ندارد. پایین‌تر توضیح داده شده. |
| **Outline ▸ Width** | ضخامت OutLine دور حروف همین لیبل. صفر یعنی بدون OutLine. |
| **Outline ▸ Colour** | رنگ همان OutLine. |
| **Own material** | متریال مخصوص همین لیبل، تا هرچه رویش تنظیم شود مال خودش بماند. |
| **Join Persian / Arabic** | چسبیدن حروف و ترتیب راست‌به‌چپ. برای متن بدون خط عربی هیچ هزینه‌ای ندارد. |
| **Fix wrapped lines** | ترتیب درست خط‌های شکسته‌شده در متن راست‌به‌چپ. |

#### هر خط دیگر

عبری، تایلندی، دواناگری، ارمنی، گرجی، خط میخی — هرکدام برای یک نفر کلِ پروژه‌اش
است، و قرار نیست هیچ‌کدام فیلد مخصوص خودش را بگیرد. پس لیست باز است. دکمه‌ی
**Add a script** را بزن و سه چیز را پر کن:

| | |
|---|---|
| **Name** | `Cuneiform` — فقط برای خودت. هیچ‌چیزی به آن وابسته نیست. |
| **Font** | `NotoSansCuneiform-Regular.ttf` |
| **Whole font** | تیک بزن تا فونت برای **هر کاراکتری که واقعاً دارد** استفاده شود — از روی جدول خودِ فونت خوانده می‌شود. آن وقت Ranges اصلاً خوانده نمی‌شود. |
| **Ranges** | `12000-123FF, 12400-1247F` — هگز، مستقیم از جدول یونیکد. فقط وقتی لازم است که **Whole font** را نزده باشی. |

زیر هر قانون، اینسپکتور نشان می‌دهد فونت **واقعاً** چه چیزی را پوشش می‌دهد —
چند کدپوینت و کدام بازه‌ها — با یک دکمه‌ی **Copy to Ranges**.

> **اگر خطت مربع خالی نشان می‌دهد، اول همین خط را نگاه کن.** فونت‌های خط‌های
> غیرمعمول خیلی وقت‌ها روی جایگاه‌های ASCII کشیده شده‌اند، نه روی کدپوینت‌های
> واقعی: گلیف‌ها روی `a` و `b` و `c` هستند و `U+12000` اصلاً داخل فایل نیست.
> چنین فونتی برای متنِ درست‌کدشده در **هیچ** ابزاری چیزی نشان نمی‌دهد. اگر
> اینسپکتور گفت پوششش `0020-007E` است، ماجرا همین است — یا حروف لاتین تایپ کن
> تا شکل‌هایش بیاید، یا **Whole font** را بزن.

بازه‌ها همان شکل‌هایی را که آدم واقعاً تایپ می‌کند قبول می‌کنند:
`12000-123FF`، `U+12000-U+123FF`، `0x12000-0x12FFF`، یک کدپوینت تنها، و همان
خط تیره‌ی بلندی که از کپی‌پیست PDF یونیکد جا می‌ماند. بازه‌ای که برعکس نوشته شده
جابه‌جا می‌شود نه اینکه رد شود. یک غلط تایپی در فهرستی شش‌تایی فقط همان یک بازه
را می‌برد نه پنج تای دیگر را، و اینسپکتور می‌گوید کدام قانون فعال است، کدام
نادیده گرفته می‌شود و چرا — قانونی که بی‌صدا به هیچ‌چیز نمی‌خورد بدترین حالت از
این سه است.

قانون‌ها **قبل از** جدول داخلی بررسی می‌شوند، پس یک قانون می‌تواند خطی را که
پکیج از قبل می‌شناسد هم بازنویسی کند. در تساوی، جدول داخلی برنده است.

فونت هر قانون به‌عنوان فالبک هم اضافه می‌شود، تا یک جمله‌ی فارسی که یک کلمه‌ی
عبری داخلش دارد حروف آن کلمه را پیدا کند.

### ✨ چرا نگهش می‌دارند

- **🌏 یک فونت برای هر زبان، روی یک لیبل.** فیلد زبان‌هایی را که فونت دارید پر
  کنید. هر بار متن عوض شود، تشخیص داده می‌شود متن *واقعاً* در چه خطی نوشته شده و
  فونت مربوطه استفاده می‌شود — با شمردن کاراکترها، پس
  `"Unity ۱۲۳ سلام دنیا"` فارسی حساب می‌شود، نه انگلیسی.
- **🎨 OutLine ای که فقط مال یک لیبل است.** در TextMeshPro همهٔ لیبل‌هایی که یک
  فونت دارند **یک** متریال مشترک دارند، پس OutLine روی یکی یعنی OutLine روی همه.
  به جایش **Outline ▸ Width** را همین‌جا تنظیم کنید: روی متریالی می‌نشیند که مال
  خودِ همین لیبل است و مقدارش روی خودِ کامپوننت ذخیره می‌شود — پس بعد از کامپایل
  دوباره، ورود به Play Mode و در بیلد هم سرِ جایش می‌ماند؛ کاری که OutLine
  تنظیم‌شده روی یک متریالِ ساخته‌شده در زمان اجرا نمی‌تواند بکند.
- **🩺 فارسیِ واقعاً خوانا.** شکل‌های چسبیده از جدول `GSUB` خودِ فونت خوانده می‌شود
  و خروجی **گلیف‌به‌گلیف با HarfBuzz** — موتور پشت کروم، فایرفاکس، اندروید و
  مک‌اواس — روی پنج فونت تست شده است.
- **🧩 بقیه‌اش مجانی.** ژاپنی، چینی، کره‌ای، سیریلیک، یونانی، تایلندی و نمادها:
  هیچ تنظیمی ندارند. اگر فونت گلیف را داشته باشد، شما هم دارید — و برای خط‌هایی
  که فونت پوشش نمی‌دهد، از **هر خط دیگر** برایشان فونت بگذارید. **ایموجی** رنگی
  تنها چیزی است که هیچ فونت متنی ندارد، برای همین فیلد جدا دارد.
- **🤝 مؤدب.** تگ‌های Rich Text هرگز شکل‌دهی یا جابه‌جا نمی‌شوند و چیزی در
  `label.text` نوشته نمی‌شود.

<details>
<summary><b>پشت صحنه، و محدودیت‌های صادقانه</b></summary>

<br>

قبلاً شکل‌دهی حروف را به **فرم‌های نمایشی یونیکد** (`U+FE70..FEFF` و
`U+FB50..FBFF`) تبدیل می‌کرد و بعد از فونت می‌پرسید آن‌ها را دارد یا نه.
فونت‌های قبل از OpenType دارند؛ فونت‌های مدرن فارسی **ندارند** — آن‌ها حروف ساده
را نگه می‌دارند و قواعد چسبیدن را در `GSUB` می‌گذارند. پس جواب «نه» می‌شد و کل
کلمه بدون جوین می‌ماند. حالا `GSUB` خودِ فونت خوانده می‌شود و همان گلیفی که خودِ
فونت می‌کشد در Font Asset ثبت می‌شود.

همچنین کلاسِ اتصال هر حرف یک واقعیت دربارهٔ **یونیکد** در نظر گرفته می‌شود نه
دربارهٔ فونت — پس یک گلیف غایب دیگر نمی‌تواند شکل حرفِ *کنارش* را خراب کند — و
حروفی که یونیکد اصلاً برایشان فرم نمایشی تعریف نکرده (بیشتر Arabic Extended-A و
بخش زیادی از کردی، سندی و خط‌های آفریقایی) هم می‌چسبند.

آنچه انجام نمی‌دهد: جایگزین‌های زمینه‌ای (شکل چسبیدهٔ استاندارد را می‌گیرید، نه
حروف‌چینی نستعلیق)، جای‌گذاری علامت‌ها (`GPOS` — حرکات سر جای پیش‌فرض فونت
می‌نشینند)، و یک تگ Rich Text در *وسط* یک جملهٔ راست‌به‌چپ آن را به دو بخش تقسیم
می‌کند که هرکدام ترتیب درستی دارند ولی نسبت به هم چپ‌به‌راست چیده می‌شوند. تگ دور
کل یک عبارت هیچ مشکلی ندارد.

</details>

### 🧰 پیش‌نیازها

یونیتی **2021.3** به بالا و **TextMeshPro** (همراه خود یونیتی). همین.

> اگر فونتی چیزی نشان نداد، خودِ Inspector کامپوننت دلیلش را می‌گوید — معمولاً
> وارد نشدن TMP Essential Resources، خاموش بودن **Include Font Data** در
> ایمپورتر فونت، Dynamic نبودن حالت **Character** آن، یا نبودن شیدر
> distance-field.

### 🔗 لینک‌ها

**[صفحهٔ افزونه](https://amircollider.com/unity-directtmp)**
・
**[amircollider.com](https://amircollider.com/)**
・
[تغییرات نسخه‌ها](CHANGELOG.md)

</div>

<p align="right"><a href="#top">↑ برگشت به بالا</a></p>

---

<a id="japanese"></a>

## 🇯🇵 日本語

**Font Asset Creator も、文字セットの指定も、`□□□` も、もうありません。**
ラベルにフォントファイルを渡せば、そのラベルはそのファイルから描画されます。
グリフは最初に必要になった瞬間にラスタライズされるので、事前準備はゼロです。

さらにペルシャ語・アラビア語・ウルドゥー語は、フォント自身の OpenType テーブルを
使って**正しく連結され、正しい語順**で表示されます。

### 📦 インストール

Package Manager ▸ **+** ▸ *Add package from git URL…*

```
https://github.com/AmirCollider/UnityDirectTMP.git
```

### 🚀 使い方

1. TextMeshPro のラベルを選択します。
2. **Add Component ▸ Unity DirectTMP ▸ Direct Font**
3. **Font** に `.ttf` または `.otf` をドラッグします。

以上です。1 コンポーネント・1 ラベル・1 フォント。プロジェクト全体には何も適用
されず、別々のフォントを使う 2 つのラベルが干渉することもありません。

> 💡 UI 全体をまとめて変えたいときは、Canvas を選んで
> **Unity DirectTMP ▸ Add Direct Font to Selection** を実行してください。配下のラベルすべてに適用されます。

コードから使う場合:

```csharp
DirectTMP.Apply(label, myFont);              // コンポーネントと同じ処理
DirectTMP.Outline(label, 0.2f, Color.black); // このラベルだけにアウトライン
label.font = DirectTMP.Load(myFont);         // フォントアセットだけ取得
label.font = DirectTMP.LoadFromFile(path);   // ランタイムに .ttf を直接読み込む
```

### 🎛️ 各項目

| 項目 | 内容 |
|---|---|
| **Font** | フォントファイル。実質これだけで動きます。 |
| **Persian / Arabic** | テキストが主にペルシャ語・アラビア語・ウルドゥー語のとき使用。空欄なら **Font**。 |
| **日本語 / 中文 / 한국어** | テキストが主に日本語・中国語・韓国語のとき使用。空欄なら **Font**。 |
| **English / Latin** | テキストが主に英語などラテン文字のとき使用。空欄なら **Font**。 |
| **Кириллица** | テキストが主にロシア語などキリル文字のとき使用。空欄なら **Font**。 |
| **Emoji 😀** | テキストが主に絵文字のとき使用。さらに最優先のフォールバックとして追加されるため、他言語の文中の絵文字も表示されます。カラー絵文字を収録したテキストフォントは存在しないので、この欄がないと他をいくら埋めてもグリフが足りません。 |
| **日本語 / 中文 / 한국어 の分割** | 言語ごとの任意欄が 3 つ、折りたたみの中にあります。指定があればその言語が優先、空欄なら上の CJK 欄、どちらもなければ **Font**。 |
| **その他の文字体系** | 自分で追加できる一覧です。名前・Unicode 範囲・フォントの 3 つ。専用欄のないものはすべてこちらへ。下記参照。 |
| **Outline ▸ Width** | このラベルの文字に描くアウトラインの太さ。0 でアウトラインなし。 |
| **Outline ▸ Colour** | そのアウトラインの色。 |
| **Own material** | このラベル専用のマテリアルを与え、設定を他へ波及させません。 |
| **Join Persian / Arabic** | 文字の連結と右から左への語順。アラビア文字を含まないテキストでは無コストです。 |
| **Fix wrapped lines** | 折り返された右から左のテキストの行順を正しく保ちます。 |

#### その他の文字体系

ヘブライ文字、タイ文字、デーヴァナーガリー、アルメニア文字、グルジア文字、楔形
文字 — どれも誰かにとってはプロジェクトそのものであり、そのすべてに専用欄を用意
することはできません。そこで一覧を開放しています。**Add a script** を押して 3 つ
を入力してください。

| | |
|---|---|
| **Name** | `Cuneiform` — ご自身のための名前です。動作には影響しません。 |
| **Font** | `NotoSansCuneiform-Regular.ttf` |
| **Whole font** | 有効にすると、そのフォントが**実際に持っているすべての文字**に使われます。フォント自身の表から読み取るため、下の Ranges は無視されます。 |
| **Ranges** | `12000-123FF, 12400-1247F` — Unicode チャートの 16 進表記。**Whole font** を使わない場合のみ必要です。 |

各ルールの下に、そのフォントが**実際に**カバーする範囲（コードポイント数と範囲）が
表示され、**Copy to Ranges** ボタンで Ranges 欄に取り込めます。

> **文字が □ になる場合は、まずその行を見てください。** 珍しい文字体系のフォントは、
> 本来のコードポイントではなく ASCII の位置にグリフが描かれていることが非常に多く、
> その場合 `a`・`b`・`c` に字形があり、`U+12000` はファイルに存在しません。こうした
> フォントは、正しく符号化されたテキストに対してどのツールでも何も表示しません。
> インスペクターが `0020-007E` と表示していればそれが原因です。ラテン文字を入力するか、
> **Whole font** を有効にしてください。

範囲は実際に入力される形式をそのまま受け付けます。`12000-123FF`、
`U+12000-U+123FF`、`0x12000-0x12FFF`、単一のコードポイント、そして Unicode の PDF
からコピーしたときに紛れ込む全角ダッシュも同様です。逆順に書かれた範囲は拒否せず
入れ替えます。6 個のうち 1 個に打ち間違いがあっても、失われるのはその 1 個だけで
残りは有効です。どのルールが有効で、どれが無視され、それはなぜかをインスペクター
が表示します — 何にも一致しないまま黙っているルールが、3 つの結果のうち最悪です。

ルールは内蔵テーブルより**先に**参照されるため、パッケージが既に知っている文字体系
を上書きすることもできます。同数の場合は内蔵側が優先されます。

各ルールのフォントはフォールバックにも追加されるので、ペルシャ語の文中にヘブライ
語の単語が 1 つあっても、その文字が見つかります。

### ✨ 選ばれている理由

- **🌏 1 つのラベルに、言語ごとのフォント。** 手持ちのフォントを言語欄に入れて
  おくだけ。テキストが変わるたびに*実際の*文字体系が判定され、対応するフォントが
  使われます。判定は文字数で行うので `"Unity ۱۲۳ سلام دنیا"` は英語ではなく
  ペルシャ語として扱われます。
- **🎨 そのラベルだけのアウトライン。** TextMeshPro では同じフォントを使う
  ラベルが**1 つ**のマテリアルを共有するため、片方のアウトラインが全部に及びます。
  代わりに **Outline ▸ Width** をここで設定してください。このラベル専用の
  マテリアルに書き込まれ、値はコンポーネント側に保存されるので、再コンパイル、
  Play Mode、ビルドを越えて残ります — 生成されたマテリアルに直接設定した
  アウトラインにはできないことです。
- **🩺 本当に読めるペルシャ語。** 連結形はフォント自身の `GSUB` から読み出し、
  Chrome・Firefox・Android・macOS を支えるシェーピングエンジン **HarfBuzz と
  グリフ単位で照合**して 5 書体で検証済みです。
- **🧩 それ以外も設定不要。** 日本語・中国語・韓国語・キリル文字・ギリシャ文字・
  タイ語・記号。フォントにグリフがあれば、そのまま出ます。フォントが収録していない
  文字体系には**その他の文字体系**でフォントを与えてください。カラー**絵文字**だけは
  どのテキストフォントにも入っていないため、専用の欄があります。
- **🤝 行儀がよい。** リッチテキストタグを整形・並べ替えすることはなく、
  `label.text` に書き込むこともありません。

<details>
<summary><b>仕組みと、正直な制限</b></summary>

<br>

以前のシェーピングは Unicode の**表示形**（`U+FE70..FEFF`、`U+FB50..FBFF`）を
出力し、フォントがそれを持っているか尋ねていました。OpenType 以前のフォントは
持っていますが、現代のペルシャ語書体は**持っていません** — 素の文字だけを収録し、
連結規則は `GSUB` で表現するからです。結果、フォントは「ない」と答え、単語は
連結されないまま残っていました。現在はフォント自身の `GSUB` を読み、各文字が
各位置でどのグリフになるかを尋ね、そのグリフをフォントアセットに登録します。

また、文字の接続クラスは**フォントではなく Unicode の事実**として扱われるため、
1 つのグリフの欠落が*隣の文字*の形を変えてしまうことはもうありません。Unicode が
表示形を与えなかった文字（Arabic Extended-A の大半、クルド語・シンド語・
アフリカ諸言語の正書法の多く）も連結します。

**対応していないこと**: 文脈依存の異体字（標準的な連結形になります。ナスタリーク
組版ではありません）、マーク配置（`GPOS` — ハラカートはフォント既定の送り位置に
置かれます）、そして右から左の文中*途中*に置かれたリッチテキストタグは、各々は
正しい順序を保ちつつ相互には左から右に並ぶ 2 つのランに分割されます。フレーズ
全体を囲むタグなら問題ありません。

</details>

### 🧰 動作環境

Unity **2021.3** 以降と **TextMeshPro**（Unity 同梱）。他の依存関係はありません。

> フォントが何も表示しない場合、コンポーネントの Inspector が理由を示します。
> 多くは TMP Essential Resources 未インポート、フォントインポーターの
> **Include Font Data** がオフ、**Character** モードが Dynamic でない、
> distance-field シェーダーが見つからない、のいずれかです。

### 🔗 リンク

**[プラグインページ](https://amircollider.com/ja/unity-directtmp)**
・
**[amircollider.com](https://amircollider.com/ja)**
・
[変更履歴](CHANGELOG.md)

<p align="right"><a href="#top">↑ トップへ戻る</a></p>

---

<p align="center">
  <a href="https://amircollider.com/unity-directtmp">فارسی</a>
  ・
  <a href="https://amircollider.com/en/unity-directtmp">English</a>
  ・
  <a href="https://amircollider.com/ja/unity-directtmp">日本語</a>
</p>

<p align="center">
  <sub>Made with 🖋️ by <a href="https://amircollider.com/">AmirCollider</a> · MIT · <a href="CHANGELOG.md">Changelog</a></sub>
</p>
