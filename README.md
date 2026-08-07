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
| **Own material** | Gives this label its own material, so its outline stays its own. |
| **Join Persian / Arabic** | Letter joining and right-to-left reading order. Free for text with no Arabic script. |
| **Fix wrapped lines** | Keeps wrapped right-to-left lines in the correct order. |

### ✨ Why people keep it

- **🌏 A font per language, on one label.** Fill in the language fields you have
  fonts for. Each time the text changes, the script it is *actually* in is
  detected and the matching font is used — by counting characters, so
  `"Unity ۱۲۳ سلام دنیا"` is Persian, not English.
- **🎨 No more outline bleed.** In TextMeshPro every label sharing a font shares
  **one** material, so an outline on one is an outline on all. **Own material**
  ends that, and it's on by default.
- **🩺 Persian that actually reads.** Joined shapes are read from the font's own
  `GSUB` table and verified **glyph-for-glyph against HarfBuzz** — the engine
  behind Chrome, Firefox, Android and macOS — across five fonts.
- **🧩 Everything else, free.** Japanese, Chinese, Korean, Cyrillic, Greek, Thai,
  emoji and symbols: nothing to configure. If the font has the glyph, you get it.
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
| **Own material** | متریال مخصوص همین لیبل، تا OutLine اش مال خودش بماند. |
| **Join Persian / Arabic** | چسبیدن حروف و ترتیب راست‌به‌چپ. برای متن بدون خط عربی هیچ هزینه‌ای ندارد. |
| **Fix wrapped lines** | ترتیب درست خط‌های شکسته‌شده در متن راست‌به‌چپ. |

### ✨ چرا نگهش می‌دارند

- **🌏 یک فونت برای هر زبان، روی یک لیبل.** فیلد زبان‌هایی را که فونت دارید پر
  کنید. هر بار متن عوض شود، تشخیص داده می‌شود متن *واقعاً* در چه خطی نوشته شده و
  فونت مربوطه استفاده می‌شود — با شمردن کاراکترها، پس
  `"Unity ۱۲۳ سلام دنیا"` فارسی حساب می‌شود، نه انگلیسی.
- **🎨 خداحافظی با OutLine سرایت‌کننده.** در TextMeshPro همهٔ لیبل‌هایی که یک فونت
  دارند **یک** متریال مشترک دارند، پس OutLine روی یکی یعنی OutLine روی همه.
  **Own material** این را تمام می‌کند و پیش‌فرض روشن است.
- **🩺 فارسیِ واقعاً خوانا.** شکل‌های چسبیده از جدول `GSUB` خودِ فونت خوانده می‌شود
  و خروجی **گلیف‌به‌گلیف با HarfBuzz** — موتور پشت کروم، فایرفاکس، اندروید و
  مک‌اواس — روی پنج فونت تست شده است.
- **🧩 بقیه‌اش مجانی.** ژاپنی، چینی، کره‌ای، سیریلیک، یونانی، تایلندی، ایموجی و
  نمادها: هیچ تنظیمی ندارند. اگر فونت گلیف را داشته باشد، شما هم دارید.
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
| **Own material** | このラベル専用のマテリアルを与え、アウトラインを他へ波及させません。 |
| **Join Persian / Arabic** | 文字の連結と右から左への語順。アラビア文字を含まないテキストでは無コストです。 |
| **Fix wrapped lines** | 折り返された右から左のテキストの行順を正しく保ちます。 |

### ✨ 選ばれている理由

- **🌏 1 つのラベルに、言語ごとのフォント。** 手持ちのフォントを言語欄に入れて
  おくだけ。テキストが変わるたびに*実際の*文字体系が判定され、対応するフォントが
  使われます。判定は文字数で行うので `"Unity ۱۲۳ سلام دنیا"` は英語ではなく
  ペルシャ語として扱われます。
- **🎨 アウトラインの巻き添えを解消。** TextMeshPro では同じフォントを使う
  ラベルが**1 つ**のマテリアルを共有するため、片方のアウトラインが全部に及びます。
  **Own material** がそれを断ち切ります（既定でオン）。
- **🩺 本当に読めるペルシャ語。** 連結形はフォント自身の `GSUB` から読み出し、
  Chrome・Firefox・Android・macOS を支えるシェーピングエンジン **HarfBuzz と
  グリフ単位で照合**して 5 書体で検証済みです。
- **🧩 それ以外も設定不要。** 日本語・中国語・韓国語・キリル文字・ギリシャ文字・
  タイ語・絵文字・記号。フォントにグリフがあれば、そのまま出ます。
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
