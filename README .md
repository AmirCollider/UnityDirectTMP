<a id="top"></a>
<p align="center">
  <img src="Docs~/logo.png" alt="Unity DocSnap logo" width="180"/>
</p>

<h1 align="center">🧋 Unity DocSnap ✨</h1>

<p align="center"><em>Snap your whole Unity project into a cozy little website.</em></p>
<p align="center"><em>あなたのUnityプロジェクトを、まるごと可愛いWebサイトに閉じ込めます。</em></p>
<p align="center"><em>کل پروژه‌ی یونیتی‌ت رو تبدیل کن به یه وب‌سایت کوچولوی دنج.</em></p>

<p align="center">
  <a href="#english">English</a> ・
  <a href="#japanese">日本語</a> ・
  <a href="#persian">فارسی</a>
</p>

<p align="center">
  <img alt="license" src="https://img.shields.io/badge/license-MIT-ffb6c1?style=flat-square">
  <img alt="unity version" src="https://img.shields.io/badge/Unity-2021.3%2B-b19cd9?style=flat-square&logo=unity&logoColor=white">
  <img alt="editor extension" src="https://img.shields.io/badge/type-Editor%20Extension-ffd6e8?style=flat-square">
  <img alt="prs welcome" src="https://img.shields.io/badge/PRs-welcome-c8f7c5?style=flat-square">
  <img alt="kawaii level" src="https://img.shields.io/badge/kawaii-100%25-ffb6c1?style=flat-square">
</p>

<p align="center">━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</p>

<a id="english"></a>
## 🧋 English

Ever open a project after two weeks away and have absolutely no idea what's inside your own Hierarchy anymore? **Unity DocSnap** remembers so you don't have to.

It's an Editor extension that walks every Scene in your project — every GameObject, every Component, every field, every reference — and every Asset's import settings, then bakes all of it into a clean, offline HTML site you can open in any browser. No server, no build step, just double-click and go. Built for the humans who need to remember what they made, and for the AI assistants that need the full picture handed to them in one clean file instead of forty screenshots. 🍰

### ✨ Features

- 🌳 **Full Hierarchy snapshot** — every GameObject in a scene, nested exactly as it sits in the Hierarchy window, with its tag, layer, active state, and static flags.
- 🔍 **Complete Inspector export** — every Component on every GameObject, every serialized field, and its live value, exactly as the Inspector shows it.
- 🔗 **Real connections, not just names** — when a script references another GameObject, a Prefab, or a ScriptableObject, that reference becomes a clickable link in the output, so you can trace exactly how a scene is wired together.
- 🖼️ **Asset *info*, not asset files** — point DocSnap at a folder, say `Assets/Images/Backgrounds`, and it exports the metadata for every file inside: import settings, compression, max size, dimensions, format — never a copy of the file itself.
- 📁 **One menu entry per Scene** — DocSnap scans your project and lists every Scene as its own menu item, so exporting one Scene is a single click.
- 🖱️ **Right-click, anywhere** — every menu action is also available from the Project window's right-click context menu on any folder or asset.
- 🌐 **An actual local website** — everything bakes into a self-contained `index.html` plus a handful of linked pages, complete with a sidebar and cross-links between objects and the assets they reference.
- 🤖 **Built for AI too** — alongside the pretty HTML, DocSnap writes a structured JSON export, so handing your whole project's context to an AI assistant takes one file instead of a screen-sharing session.
- 🧩 **Editor-only** — lives entirely inside an `Editor` assembly. Zero runtime cost, zero added build size.

### 📋 Requirements

- Unity **2021.3 LTS** or newer (Unity 6.x supported)
- No third-party dependencies

### 📦 Installation

**Option A — Package Manager (recommended)**
1. Open **Window → Package Manager**
2. Click **+ → Add package from git URL…**
3. Paste `https://github.com/AmirCollider/UnityDocSnap.git`
4. Click **Add**

**Option B — Manual**
1. Download or clone this repository
2. Copy the `Editor/UnityDocSnap` folder into your project's `Assets` folder
3. Unity compiles it automatically — no restart needed

### 🚀 Usage

Once installed, a new menu shows up in Unity's top menu bar: **Unity DocSnap**.

```
Unity DocSnap
├── Export Scene
│   ├── MainMenu
│   ├── Level01
│   └── Level02              ← one entry per Scene found in your project
├── Export Asset Info
│   ├── Entire Assets Folder
│   └── Selected Folder…
├── Export Full Project      (Scenes + Assets, all cross-linked)
├── Export Full Project With Files
├── Update Previous Export    (fast incremental refresh — reuses unchanged Scenes/Assets)
├── Open Output Folder
└── About Unity DocSnap
```

The generated site also has a **search box** in the sidebar (All / Scenes / Assets), a **Packages** page listing every package the project depends on, and marks **Prefab instances / variants / overridden fields** throughout.

**Exporting a single Scene**
`Unity DocSnap → Export Scene → [YourSceneName]` walks that Scene's entire Hierarchy and writes a full snapshot of every GameObject and Component into the output folder.

**Exporting asset info**
`Unity DocSnap → Export Asset Info → Selected Folder…` lets you pick a folder — for example `Assets/Images/Backgrounds` — and DocSnap exports the Inspector info for every file inside it. For an image like `bakery_street.png`, that means Texture Type, sRGB, Compression, Max Size, Filter Mode, Wrap Mode, generated mip maps, and every other import setting, captured exactly as Unity has it configured. The pixels themselves never leave your project.

**Opening the result**
By default, output lands in `<ProjectRoot>/UnityDocSnap_Output/`. Use `Unity DocSnap → Open Output Folder` to jump straight there, then open `index.html` in any browser.

### 📁 Output Structure

Every export writes **two forms of the same information** side by side: the **full** offline site (browse it, or read the raw JSON) and a **simple** set of short summaries — Markdown *and* JSON — gathered in the `summary/` folder, small enough to paste straight into an AI assistant.

```
UnityDocSnap_Output/
├── index.html         ← the full offline site (start here)
├── packages.html      ← packages the project depends on (Unity + third-party)
├── summary.md         ← project index → points into summary/
├── summary/           ← simple, AI-friendly (short — hand these to an AI)
│   ├── scene-MainMenu.md            ← readable
│   ├── scene-MainMenu.json          ← structured (a few hundred lines)
│   ├── folder-Images_Backgrounds.md
│   └── folder-Images_Backgrounds.json
├── scenes/
│   └── MainMenu.html                ← full interactive page
├── folders/
│   └── Images_Backgrounds.html
├── data/              ← full, every-field JSON (the advanced form)
├── theme/             ← css/js + search-index.js + thumbnails for the site itself
└── source-files/      ← optional verbatim asset copies (With Files export only)
```

The site itself has a **Simple / Advanced** toggle in the sidebar: *Simple* shows a clean skim (hierarchy, custom-script configuration, key asset facts), *Advanced* shows every serialized field. It opens in Simple by default and remembers your choice.

### 🧠 Built for Humans *and* AI

Every exported page follows the same clean, predictable structure — proper headings, labeled fields, consistent IDs. A person can skim it in a browser in under a minute. An AI assistant can be handed the `data/` folder (or a single JSON file) and immediately understand your Hierarchy, your Components, and your asset settings — without you typing out an explanation by hand.

### 🗺️ Roadmap

- [x] Optional thumbnail previews for image assets
- [x] Search across the whole exported site
- [ ] Diff view between two exports
- [ ] Dark mode for the generated site 🌙

### 🤝 Contributing

Issues and pull requests are always welcome.

### 📜 License

MIT — see [LICENSE](LICENSE).

### 💌 Credits

Made with 🧋 by [AmirCollider](https://github.com/AmirCollider).

If Unity DocSnap saves you some digging around later, a ⭐ on the repo goes a long way.

<p align="right"><a href="#top">⬆ Back to top</a></p>

<p align="center">━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</p>

<a id="japanese"></a>
## 🍰 日本語

2週間ぶりにプロジェクトを開いて、自分のHierarchyの中身を全部忘れてしまったこと、ありませんか?**Unity DocSnap** が代わりに覚えていてくれます。

これはエディタ拡張機能で、プロジェクト内のすべてのSceneを歩き回り——すべてのGameObject、すべてのComponent、すべてのフィールド、すべての参照——そしてすべてのAssetのインポート設定までをまるごとスナップショットして、ブラウザでそのまま開けるきれいなオフラインHTMLサイトに焼き上げます。サーバーもビルドも不要、ダブルクリックで開くだけです。「自分が何を作ったか思い出せない人」のためにも、「40枚のスクリーンショットではなく1つの整理された情報が欲しいAIアシスタント」のためにも作られました。🍰

### ✨ 特徴

- 🌳 **完全なHierarchyスナップショット** — シーン内のすべてのGameObjectを、Hierarchyウィンドウそのままの入れ子構造で。タグ、レイヤー、アクティブ状態、Staticフラグも含めて。
- 🔍 **完全なInspectorエクスポート** — すべてのGameObjectのすべてのComponent、すべてのシリアライズされたフィールドとその現在値を、Inspectorに表示されている通りに。
- 🔗 **名前だけでなく、本当のつながり** — あるスクリプトが別のGameObject・Prefab・ScriptableObjectを参照している場合、出力内でクリック可能なリンクになります。シーンがどう組み立てられているか、たどることができます。
- 🖼️ **ファイルの中身ではなく、ファイルの情報** — `Assets/Images/Backgrounds` のようなフォルダを指定すると、中の全ファイルの *メタデータ*(インポート設定、圧縮方式、最大サイズ、解像度、フォーマットなど)をエクスポートします。ファイル本体はコピーしません。
- 📁 **Sceneごとにメニュー項目を自動生成** — DocSnapがプロジェクトをスキャンし、すべてのSceneをそれぞれ独立したメニュー項目として表示します。
- 🖱️ **どこでも右クリック** — すべてのメニュー操作は、Projectウィンドウでフォルダやアセットを右クリックしたコンテキストメニューからも実行できます。
- 🌐 **本物のローカルWebサイト** — すべてが `index.html` と数枚のリンクされたページにまとめられ、サイドバーと、オブジェクト同士・参照アセット間の相互リンク付きです。
- 🤖 **AIのためにも** — 見やすいHTMLと一緒に、構造化されたJSONも出力します。プロジェクト全体の情報をAIアシスタントに渡すのに、画面共有ではなく1つのファイルで済みます。
- 🧩 **エディタ専用** — すべて `Editor` アセンブリの中に収まります。ランタイムコストはゼロ、ビルドサイズへの影響もゼロです。

### 📋 必要環境

- Unity **2021.3 LTS** 以降(Unity 6系にも対応)
- サードパーティ製の依存関係なし

### 📦 インストール

**方法A — Package Manager(推奨)**
1. **Window → Package Manager** を開く
2. **+ → Add package from git URL…** をクリック
3. `https://github.com/AmirCollider/UnityDocSnap.git` を貼り付ける
4. **Add** をクリック

**方法B — 手動インストール**
1. このリポジトリをダウンロードまたはクローン
2. `Editor/UnityDocSnap` フォルダをプロジェクトの `Assets` フォルダにコピー
3. Unityが自動的にコンパイルします。再起動は不要です

### 🚀 使い方

インストール後、Unityの上部メニューバーに **Unity DocSnap** という新しいメニューが追加されます。

```
Unity DocSnap
├── Export Scene
│   ├── MainMenu
│   ├── Level01
│   └── Level02              ← プロジェクト内のSceneごとに項目が追加されます
├── Export Asset Info
│   ├── Entire Assets Folder
│   └── Selected Folder…
├── Export Full Project      (Scene + Assetをまとめて、すべて相互リンク済みで)
├── Export Full Project With Files
├── Update Previous Export    (増分更新 — 変更のないScene/Assetは再利用)
├── Open Output Folder
└── About Unity DocSnap
```

生成されたサイトには、サイドバーの**検索ボックス**(All / Scenes / Assets)、依存パッケージ一覧の**Packages**ページ、そして**Prefabインスタンス/バリアント/上書きされたフィールド**の表示も追加されました。

**Sceneを1つだけエクスポートする**
`Unity DocSnap → Export Scene → [Scene名]` を選ぶと、そのSceneのHierarchy全体を歩き、すべてのGameObjectとComponentのスナップショットを出力フォルダに書き出します。

**アセット情報をエクスポートする**
`Unity DocSnap → Export Asset Info → Selected Folder…` でフォルダを選べます。例えば `Assets/Images/Backgrounds` を選ぶと、中の全ファイルのInspector情報がエクスポートされます。`bakery_street.png` のような画像なら、Texture Type、sRGB、Compression、Max Size、Filter Mode、Wrap Mode、Generate Mip Mapsなど、Unityに設定されているインポート設定がそのまま記録されます。画像のピクセルデータそのものはプロジェクトの外に出ません。

**結果を開く**
デフォルトでは出力先は `<プロジェクトルート>/UnityDocSnap_Output/` です。`Unity DocSnap → Open Output Folder` で直接そのフォルダを開き、`index.html` をブラウザで開いてください。

### 📁 出力構造

エクスポートするたびに、**同じ情報が2つの形**で並んで書き出されます。**フル版**のオフラインサイト(ブラウザで見る、または生のJSONを読む)と、AIアシスタントにそのまま貼り付けられる**シンプル版**の短い要約(MarkdownとJSONの両方)で、後者はすべて `summary/` フォルダにまとまっています。

```
UnityDocSnap_Output/
├── index.html         ← フル版のオフラインサイト(まずここから)
├── summary.md         ← プロジェクト索引 → summary/ への案内
├── summary/           ← シンプル / AI向け(短い。AIにはこれを渡す)
│   ├── scene-MainMenu.md            ← 読みやすい版
│   ├── scene-MainMenu.json          ← 構造化版(数百行)
│   ├── folder-Images_Backgrounds.md
│   └── folder-Images_Backgrounds.json
├── scenes/
│   └── MainMenu.html                ← フルの対話ページ
├── folders/
│   └── Images_Backgrounds.html
├── packages.html      ← プロジェクトが依存するパッケージ(Unity + サードパーティ)
├── data/              ← 完全な構造化JSON(全フィールドを含む詳細版)
├── theme/             ← サイト自体のcss/js + search-index.js + サムネイル
└── source-files/      ← アセット実体のコピー(任意 / With Files エクスポート時のみ)
```

サイトにはサイドバーに **Simple / Advanced** の切り替えがあります。*Simple* はすっきりした概要(ヒエラルキー、カスタムスクリプトの設定、アセットの要点)を、*Advanced* はすべてのシリアライズ済みフィールドを表示します。初期状態は Simple で、選択は記憶されます。

### 🧠 人にもAIにもやさしい理由

すべてのエクスポートページは、きちんとした見出し・ラベル付きのフィールド・一貫したIDという、わかりやすい構造に従っています。人間はブラウザで1分もあれば全体を把握できますし、AIアシスタントには `data/` フォルダ(または1つのJSONファイル)を渡すだけで、Hierarchy・Component・アセット設定を、いちいち手で説明しなくても理解してもらえます。

### 🗺️ ロードマップ

- [x] 画像アセットのサムネイルプレビュー(任意)
- [x] エクスポートしたサイト全体の検索機能
- [ ] 2つのエクスポート間の差分表示
- [ ] 生成されたサイトのダークモード 🌙

### 🤝 コントリビュート

IssueやPull Requestはいつでも歓迎です。

### 📜 ライセンス

MIT — 詳細は [LICENSE](LICENSE) をご覧ください。

### 💌 クレジット

🧋を込めて、[AmirCollider](https://github.com/AmirCollider) より。

Unity DocSnapが後々の手間を減らしてくれたなら、リポジトリへの ⭐ がとても励みになります。

<p align="right"><a href="#top">⬆ トップに戻る</a></p>

<p align="center">━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</p>

<a id="persian"></a>
<div dir="rtl" align="right">

## ⭐ فارسی

تا حالا شده بعد از دو هفته پروژه رو باز کنی و اصلاً یادت نیاد توی Hierarchy خودت چی ریخته بودی؟ **Unity DocSnap** جاش یادش می‌مونه.

این یه افزونه‌ی ادیتوره که توی همه‌ی سین‌های پروژه‌ت قدم می‌زنه — همه‌ی GameObject ها، همه‌ی کامپوننت‌ها، همه‌ی فیلدها، همه‌ی رفرنس‌ها — و تنظیمات ایمپورت همه‌ی فایل‌های پروژه رو هم برمی‌داره، بعد همه‌شون رو می‌ریزه توی یه سایت HTML تمیز و آفلاین که با هر مرورگری میشه بازش کرد. نه سروری لازمه نه بیلدی، فقط دابل‌کلیک کن و باز کن. هم برای آدم‌هایی ساخته شده که یادشون میره دقیقاً چی ساختن، هم برای دستیارهای هوش مصنوعی که به‌جای چهل تا اسکرین‌شات، یه فایل مرتب می‌خوان. 🍰

### ✨ ویژگی‌ها

- 🌳 **اسنپ‌شات کامل از Hierarchy** — همه‌ی GameObject های یه سین، دقیقاً با همون تودرتویی که توی پنجره‌ی Hierarchy می‌بینی، همراه با Tag، Layer، وضعیت فعال/غیرفعال و Static Flag.
- 🔍 **خروجی کامل از Inspector** — همه‌ی کامپوننت‌های روی هر GameObject، همه‌ی فیلدهای سریالایز شده و مقدار فعلی‌شون، دقیقاً همون‌طور که توی Inspector می‌بینی.
- 🔗 **اتصالات واقعی، نه فقط اسم** — اگه یه اسکریپت به یه GameObject دیگه، یه Prefab یا یه ScriptableObject رفرنس داشته باشه، توی خروجی یه لینک قابل‌کلیک میشه؛ اینجوری می‌تونی ببینی سین دقیقاً چطور به هم وصله.
- 🖼️ **اطلاعات فایل، نه خود فایل** — یه مسیر بهش بده، مثلاً `Assets/Images/Backgrounds`، اون هم *اطلاعات* همه‌ی فایل‌های اون مسیر رو خروجی می‌گیره: تنظیمات ایمپورت، فشرده‌سازی، حداکثر سایز، ابعاد، فرمت — بدون این‌که خود فایل کپی بشه.
- 📁 **یه گزینه‌ی منو برای هر سین** — DocSnap پروژه رو اسکن می‌کنه و همه‌ی سین‌ها رو جدا جدا توی منو میاره.
- 🖱️ **راست‌کلیک، هرجا که باشی** — همه‌ی گزینه‌های منو از راست‌کلیک روی هر فولدر یا فایل توی پنجره‌ی Project هم در دسترسن.
- 🌐 **یه وب‌سایت لوکال واقعی** — همه چی توی یه `index.html` و چندتا صفحه‌ی به‌هم‌وصل جمع میشه، با سایدبار و لینک‌های داخلی بین آبجکت‌ها و فایل‌هایی که بهشون رفرنس دارن.
- 🤖 **برای هوش مصنوعی هم ساخته شده** — کنار HTML قشنگش، یه خروجی JSON ساختاریافته هم می‌ده؛ دادن کل اطلاعات پروژه به یه دستیار هوش مصنوعی، به‌جای اشتراک‌گذاری صفحه، فقط یه فایل می‌خواد.
- 🧩 **فقط ادیتور** — کاملاً توی یه اسمبلی `Editor` جا می‌گیره. نه هزینه‌ای موقع اجرا داره، نه چیزی به حجم بیلد اضافه می‌کنه.

### 📋 پیش‌نیازها

- یونیتی **2021.3 LTS** به بعد (یونیتی 6 هم پشتیبانی میشه)
- بدون هیچ وابستگی به کتابخونه‌ی شخص‌ثالث

### 📦 نصب

**روش الف — Package Manager (پیشنهادی)**
۱. برو به **Window → Package Manager**
۲. کلیک کن روی **+ → Add package from git URL…**
۳. این آدرس رو بچسبون: `https://github.com/AmirCollider/UnityDocSnap.git`
۴. کلیک کن روی **Add**

**روش ب — نصب دستی**
۱. این ریپازیتوری رو دانلود یا کلون کن
۲. پوشه‌ی `Editor/UnityDocSnap` رو بریز توی پوشه‌ی `Assets` پروژه‌ت
۳. یونیتی خودش کامپایلش می‌کنه؛ نیازی به ری‌استارت نیست

### 🚀 نحوه‌ی استفاده

بعد از نصب، توی نوار بالای یونیتی یه منوی جدید به اسم **Unity DocSnap** اضافه میشه.

```
Unity DocSnap
├── Export Scene
│   ├── MainMenu
│   ├── Level01
│   └── Level02              ← به ازای هر سین توی پروژه، یه گزینه
├── Export Asset Info
│   ├── Entire Assets Folder
│   └── Selected Folder…
├── Export Full Project      (سین‌ها + فایل‌ها، همه به هم لینک‌شده)
├── Export Full Project With Files
├── Update Previous Export    (بروزرسانی افزایشی و سریع — موارد تغییرنکرده دوباره استفاده می‌شن)
├── Open Output Folder
└── About Unity DocSnap
```

سایت تولیدشده حالا یه **باکس جستجو** توی سایدبار داره (همه / سین‌ها / فایل‌ها)، یه صفحه‌ی **Packages** که همه‌ی پکیج‌های پروژه رو لیست می‌کنه، و **نمونه‌ها/واریانت‌های Prefab و فیلدهای بازنویسی‌شده** رو همه‌جا مشخص می‌کنه.

**اکسپورت گرفتن از یه سین**
با زدن `Unity DocSnap → Export Scene → [اسم سین]`، کل Hierarchy همون سین رو قدم می‌زنه و اسنپ‌شات کامل همه‌ی GameObject ها و کامپوننت‌هاشون رو توی پوشه‌ی خروجی می‌نویسه.

**اکسپورت گرفتن اطلاعات فایل‌ها**
با `Unity DocSnap → Export Asset Info → Selected Folder…` می‌تونی یه پوشه انتخاب کنی — مثلاً `Assets/Images/Backgrounds` — و DocSnap اطلاعات Inspector همه‌ی فایل‌های اون پوشه رو اکسپورت می‌کنه. برای یه عکس مثل `bakery_street.png`، یعنی Texture Type، sRGB، Compression، Max Size، Filter Mode، Wrap Mode، Generate Mip Maps و بقیه‌ی تنظیمات ایمپورتش، دقیقاً همون‌طور که توی یونیتی تنظیم شده. خود پیکسل‌های عکس هیچ‌وقت از پروژه بیرون نمیره.

**باز کردن نتیجه**
به‌صورت پیش‌فرض، خروجی توی مسیر `<ریشه‌ی پروژه>/UnityDocSnap_Output/` قرار می‌گیره. با `Unity DocSnap → Open Output Folder` مستقیم می‌ری اونجا، بعد `index.html` رو با هر مرورگری باز کن.

### 📁 ساختار خروجی

هر اکسپورت **دو شکل از یک اطلاعات** رو کنار هم می‌نویسه: نسخه‌ی **کامل** یعنی سایت آفلاین (توی مرورگر ببینش یا JSON خامش رو بخون) و نسخه‌ی **ساده** یعنی چند فایل خلاصه‌ی کوتاه — هم Markdown هم JSON — که همه توی پوشه‌ی `summary/` جمع شدن و می‌تونی مستقیم بچسبونی توی یه دستیار هوش مصنوعی.

```
UnityDocSnap_Output/
├── index.html         ← سایت آفلاین کامل (از اینجا شروع کن)
├── summary.md         ← فهرست پروژه → راهنما به summary/
├── summary/           ← ساده / مناسب هوش مصنوعی (کوتاه — اینا رو به AI بده)
│   ├── scene-MainMenu.md            ← نسخه‌ی خوانا
│   ├── scene-MainMenu.json          ← نسخه‌ی ساختاریافته (چند صد خط)
│   ├── folder-Images_Backgrounds.md
│   └── folder-Images_Backgrounds.json
├── scenes/
│   └── MainMenu.html                ← صفحه‌ی کامل و تعاملی
├── folders/
│   └── Images_Backgrounds.html
├── packages.html      ← پکیج‌هایی که پروژه بهشون وابسته‌ست (یونیتی + شخص‌ثالث)
├── data/              ← JSON کامل و ساختاریافته (نسخه‌ی پیشرفته، همه‌ی فیلدها)
├── theme/             ← css/js و search-index.js و تصاویر بندانگشتی خود سایت
└── source-files/      ← کپی خام فایل‌ها (اختیاری / فقط در اکسپورت With Files)
```

خود سایت توی سایدبار یه کلید **Simple / Advanced** داره: حالت *Simple* یه نمای تمیز و سریع نشون می‌ده (Hierarchy، تنظیمات اسکریپت‌های خودت، نکات کلیدی فایل‌ها) و حالت *Advanced* همه‌ی فیلدهای سریالایز‌شده رو. به‌صورت پیش‌فرض روی Simple باز میشه و انتخابت رو یادش می‌مونه.

### 🧠 چرا هم برای آدم‌ها هم برای هوش مصنوعی؟

هر صفحه‌ی اکسپورت‌شده یه ساختار تمیز و قابل‌پیش‌بینی داره — تیترهای درست، فیلدهای برچسب‌دار، آی‌دی‌های یکدست. یه آدم می‌تونه توی کمتر از یه دقیقه توی مرورگر کل ماجرا رو بفهمه، و به یه دستیار هوش مصنوعی هم کافیه پوشه‌ی `data/` (یا یه فایل JSON) رو بدی تا Hierarchy، کامپوننت‌ها و تنظیمات فایل‌هات رو، بدون این‌که مجبور باشی دستی توضیح بدی، بفهمه.

### 🗺️ نقشه‌ی راه

- [x] پیش‌نمایش کوچیک (Thumbnail) برای فایل‌های عکس (اختیاری)
- [x] جستجو توی کل سایت اکسپورت‌شده
- [ ] نمایش تفاوت بین دو تا خروجی مختلف
- [ ] حالت تاریک (Dark Mode) برای سایت تولیدشده 🌙

### 🤝 مشارکت

Issue و Pull Request همیشه خوش‌اومدن.

### 📜 لایسنس

MIT — جزئیات توی فایل [LICENSE](LICENSE).

### 💌 با تشکر از

با 🧋 ساخته شده توسط [AmirCollider](https://github.com/AmirCollider).

اگه Unity DocSnap یه‌کم از دردسر بعدیت کم کرد، یه ⭐ روی ریپو خیلی دلگرم‌کننده‌ست.

<p align="right"><a href="#top">⬆ برگشت به بالا</a></p>

</div>

<p align="center">━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</p>

<p align="center"><sub>Made with 🧋 🍰 ⭐ for Unity — <a href="https://github.com/AmirCollider">AmirCollider</a></sub></p>
