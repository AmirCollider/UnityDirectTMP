<a id="top"></a>
<p align="center">
  <img src="Docs~/logo.png" alt="Unity DirectTMP logo" width="180"/>
</p>

<h1 align="center">🖋️ Unity DirectTMP ✨</h1>

<p align="center"><em>Hand TextMeshPro the font file itself — and every language just works.</em></p>
<p align="center"><em>フォントファイルをそのままTextMeshProへ。どんな言語も、そのまま表示されます。</em></p>
<p align="center"><em>فایل فونت رو مستقیم بده به TextMeshPro — هر زبونی، همون‌جوری که هست.</em></p>

<p align="center">
  <a href="#english">English</a> ・
  <a href="#japanese">日本語</a> ・
  <a href="#persian">فارسی</a>
</p>

<p align="center">
  <img alt="license" src="https://img.shields.io/badge/license-MIT-ffb6c1?style=flat-square">
  <img alt="unity version" src="https://img.shields.io/badge/Unity-2021.3%2B-b19cd9?style=flat-square&logo=unity&logoColor=white">
  <img alt="textmeshpro" src="https://img.shields.io/badge/TextMeshPro-3.0%2B-ffd6e8?style=flat-square">
  <img alt="type" src="https://img.shields.io/badge/type-Runtime%20%2B%20Editor-c8f7c5?style=flat-square">
  <img alt="prs welcome" src="https://img.shields.io/badge/PRs-welcome-c8f7c5?style=flat-square">
  <img alt="tofu level" src="https://img.shields.io/badge/tofu-0%25-ffb6c1?style=flat-square">
</p>

<p align="center">━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</p>

<a id="english"></a>
## 🖋️ English

Ever found the perfect font, dropped it into Unity, spent an hour in the Font Asset Creator picking character ranges — and still got a row of □□□ the moment someone typed their own name in Japanese or Persian? **Unity DirectTMP** skips that whole step.

It's a small component that sits next to your `TextMeshProUGUI` / `TextMeshPro` and lets you assign the **font file itself** — a plain `.ttf` or `.otf` — instead of a pre-baked SDF font asset. Glyphs are rasterized straight from that file the moment they're first drawn, so every character the font actually contains is available immediately: Japanese, Chinese, Korean, Persian, Arabic, Thai, Cyrillic, symbols, all of it. No character sets to guess in advance, no atlases to rebuild, no tofu. 🍰

### ✨ Features

- 🅰️ **Point at a font file, not a font asset** — drag a `.ttf` or `.otf` onto the component and you're done. The SDF side of things is handled for you, quietly, in the background.
- 🌏 **Every script the font supports, instantly** — glyphs come from the font file on demand, so a font that covers 20,000 kanji covers 20,000 kanji. You never enumerate a character set again.
- 🚫 **Goodbye, Font Asset Creator** — no character-range dialog, no *Generate Font Atlas*, no regenerating every time the text changes.
- 🔗 **Fallback chains, ordered by you** — line up a Latin UI font, a CJK font, and a symbol font; the first one that actually has the glyph wins, per character.
- 💾 **Load fonts at runtime** — from `StreamingAssets`, `persistentDataPath`, a downloaded `byte[]`, or the player's own installed fonts. Made for localization patches, user-selectable fonts, and modding.
- ♻️ **An atlas that manages itself** — glyphs are added as they're used, atlases grow across multiple textures when needed, and unused glyphs can be cleared on scene change.
- 🎛️ **The Inspector you already know** — one small component beside TMP. Materials, outlines, gradients, underlays, auto-sizing and the RTL toggle keep behaving exactly as before.
- ⚡ **Cached, not rebuilt** — font faces are cached per *(file + sampling size + render mode)*, so a screen with 200 labels sharing one font builds exactly one atlas.
- 🧹 **A repo that stays small** — the `.ttf` is the only thing you commit. No multi-megabyte atlas textures in version control, no merge conflicts on binary assets.
- 🧩 **Runtime *and* Editor** — it works in Play Mode, in builds, and previews live in the Scene view while you're still designing.

### 📋 Requirements

- Unity **2021.3 LTS** or newer (Unity 6.x supported)
- **TextMeshPro 3.0+** (the version that ships with those Unity releases)
- No other third-party dependencies

### 📦 Installation

**Option A — Package Manager (recommended)**
1. Open **Window → Package Manager**
2. Click **+ → Add package from git URL…**
3. Paste `https://github.com/AmirCollider/UnityDirectTMP.git`
4. Click **Add**

**Option B — Manual**
1. Download or clone this repository
2. Copy the `UnityDirectTMP` folder into your project's `Assets` folder
3. Unity compiles it automatically — no restart needed

### 🚀 Usage

**The thirty-second version**
1. Select any GameObject that already has a `TextMeshProUGUI` or `TextMeshPro` on it
2. **Add Component → Unity DirectTMP → Direct Font**
3. Drag your `.ttf` / `.otf` into the **Font File** field

That's it. Type Japanese, Persian, Korean, anything — it renders.

**The menu**

Once installed, a new menu appears in Unity's top menu bar: **Unity DirectTMP**.

```
Unity DirectTMP
├── Convert
│   ├── Selected Objects
│   ├── Current Scene
│   └── Whole Project        (every Scene + every Prefab, in one pass)
├── Fallback Chain…          (order your fonts once, reuse everywhere)
├── Font Cache
│   ├── Show Cache Folder
│   └── Clear Cache
├── Settings…                (sampling point size, atlas size, render mode)
└── About Unity DirectTMP
```

Every menu action is also available from the Project window's right-click context menu, on any font file or folder.

**From code**

```csharp
using UnityDirectTMP;

// a font that ships with your game (path relative to StreamingAssets)
directFont.SetFontFile("Fonts/Vazirmatn-Regular.ttf");

// a font you just downloaded, still in memory
directFont.SetFontBytes(request.downloadHandler.data);

// one line for the entire UI
DirectTMP.SetGlobalFont(fontBytes);

// warm up glyphs before a text-heavy scene appears
DirectTMP.Preload(fontBytes, "こんにちは世界 سلام دنیا Hello");
```

### 🧩 How It Works

No magic, and nothing rewritten inside TextMeshPro — DirectTMP simply changes *where the glyphs come from*.

```
your .ttf / .otf / .ttc
        │
        ▼
┌────────────────────────────────────────────┐
│              Unity DirectTMP               │
│  reads the raw font file (disk or memory)  │
│  opens it as a live font face              │
│  rasterizes each glyph on its first use    │
│  caches per font + size + render mode      │
└────────────────────────────────────────────┘
        │
        ▼
a dynamic font asset, handed straight to your existing
TextMeshProUGUI / TextMeshPro — same component, same
material, same everything
```

**Package structure**

```
UnityDirectTMP/
├── Runtime/
│   ├── DirectFont.cs           ← the component you drop next to TMP
│   ├── DirectFontLoader.cs     ← font file → live font face
│   ├── DirectFontCache.cs      ← one atlas per (font + settings)
│   ├── DirectFontFallback.cs   ← ordered fallback chains
│   └── DirectTMP.cs            ← the small public API
├── Editor/
│   ├── DirectFontInspector.cs  ← Inspector drawer + live preview
│   ├── DirectTMPMenu.cs        ← the Unity DirectTMP menu
│   ├── DirectTMPConverter.cs   ← batch conversion for Scenes & Prefabs
│   └── DirectTMPSettings.cs
├── Samples~/
│   └── MultilingualDemo/       ← one scene, one font, twelve languages
└── Docs~/
```

**Good to know**

- 🔤 Each glyph is rasterized once and then cached — the cost lands on first use, never per frame. If you're about to reveal a wall of brand-new CJK text in a single frame, `Preload` it.
- 📦 The font file stays a font file. Nothing is baked into your project, so swapping a font is a drag-and-drop, not a rebuild.
- ⚖️ Font licensing is still yours to check — shipping a `.ttf` inside a build counts as redistribution.

### 🌍 Script Support

| Script | Glyphs | Notes |
|---|---|---|
| Latin / Cyrillic / Greek | ✅ | Nothing to configure |
| CJK — 日本語 / 中文 / 한국어 | ✅ | On demand, so no 40 MB atlas sitting in memory |
| Arabic script — فارسی / العربية / اردو | ✅ | Glyphs render; joining-form shaping is on the roadmap |
| Devanagari / Thai / Hebrew | ✅ | Glyphs render; complex shaping is on the roadmap |
| Color emoji (COLR / CBDT) | 🚧 | Planned |

### 🧠 Why not just bake an SDF font asset?

Baking is genuinely the right answer when you know every character your game will ever draw — an English-only UI with a fixed word list, for example. It stops being the right answer the moment the text comes from somewhere you don't control: player names, chat, user-generated content, a community translation that landed last week, a Japanese localization that would need three thousand kanji enumerated by hand.

Unity DirectTMP is for that second world. The font file already knows exactly which glyphs it has — DirectTMP just stops pretending it doesn't.

### 🗺️ Roadmap

- [x] Direct `.ttf` / `.otf` on any TMP component
- [x] Runtime loading from `StreamingAssets`, `byte[]`, or a downloaded file
- [ ] Ordered fallback chains as a reusable ScriptableObject
- [ ] `.ttc` collection support with a face-index picker
- [ ] Use the player's own installed system fonts
- [ ] Arabic-script shaping (Persian / Arabic / Urdu joining forms) from the font's own OpenType tables
- [ ] Color & emoji fonts (COLR / CBDT)
- [ ] Variable font axes — weight, width, slant 🌙

### 🤝 Contributing

Issues and pull requests are always welcome.

### 📜 License

MIT — see [LICENSE](LICENSE).

### 💌 Credits

Made with 🖋️ by [AmirCollider](https://github.com/AmirCollider).

If Unity DirectTMP saved you a trip to the Font Asset Creator, a ⭐ on the repo goes a long way.

<p align="right"><a href="#top">⬆ Back to top</a></p>

<p align="center">━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</p>

<a id="japanese"></a>
## 🍰 日本語

気に入ったフォントを見つけて、Unityに入れて、Font Asset Creatorで文字セットを選ぶのに1時間——それでもプレイヤーが日本語やペルシャ語で自分の名前を入力した瞬間、□□□ が並ぶ。そんな経験はありませんか?**Unity DirectTMP** は、その工程をまるごと省きます。

これは `TextMeshProUGUI` / `TextMeshPro` の隣に置く小さなコンポーネントで、焼き上げ済みのSDFフォントアセットではなく、**フォントファイルそのもの**(ふつうの `.ttf` や `.otf`)を直接指定できるようにするものです。グリフは最初に描画された瞬間にフォントファイルから生成されるので、そのフォントが持っている文字はすべて、すぐにそのまま使えます。日本語、中国語、韓国語、ペルシャ語、アラビア語、タイ語、キリル文字、記号——全部です。文字セットを事前に予想する必要も、アトラスを焼き直す必要も、豆腐(□)もありません。🍰

### ✨ 特徴

- 🅰️ **指定するのはフォントアセットではなく、フォントファイル** — `.ttf` や `.otf` をコンポーネントにドラッグするだけ。SDFまわりの面倒はすべて裏側で処理されます。
- 🌏 **フォントが持つ文字は、すべてそのまま** — グリフは必要になった瞬間にフォントファイルから生成されます。2万字の漢字を含むフォントなら、2万字そのまま使えます。文字セットを列挙する作業はもう不要です。
- 🚫 **Font Asset Creator とお別れ** — 文字範囲のダイアログも、*Generate Font Atlas* も、テキストが変わるたびの作り直しもありません。
- 🔗 **順番を自分で決められるフォールバック** — 欧文UIフォント、CJKフォント、記号フォントを並べておけば、その文字を実際に持っている最初のフォントが、1文字ごとに選ばれます。
- 💾 **実行時のフォント読み込み** — `StreamingAssets`、`persistentDataPath`、ダウンロードした `byte[]`、プレイヤーがインストール済みのフォントから読み込めます。ローカライズの後追い配信、フォント選択機能、Mod対応にどうぞ。
- ♻️ **自分で面倒をみるアトラス** — 使われたグリフから順に追加され、必要に応じて複数テクスチャに拡張し、使わなくなったグリフはシーン切り替え時に破棄できます。
- 🎛️ **いつものInspectorのまま** — TMPの隣に小さなコンポーネントが1つ増えるだけ。マテリアル、アウトライン、グラデーション、Underlay、自動サイズ調整、RTLトグルはこれまで通り動きます。
- ⚡ **作り直さず、キャッシュする** — フォントフェイスは *(ファイル + サンプリングサイズ + レンダーモード)* ごとにキャッシュされるので、同じフォントのラベルが200個ある画面でも、アトラスは1つだけです。
- 🧹 **リポジトリが軽いまま** — コミットするのは `.ttf` だけ。数MBのアトラステクスチャをバージョン管理に入れる必要も、バイナリの衝突に悩む必要もありません。
- 🧩 **ランタイムでもエディタでも** — Play Modeでもビルドでも動作し、制作中のSceneビューではそのままプレビューされます。

### 📋 必要環境

- Unity **2021.3 LTS** 以降(Unity 6系にも対応)
- **TextMeshPro 3.0以降**(上記Unityに標準で同梱されているバージョン)
- その他のサードパーティ製依存関係なし

### 📦 インストール

**方法A — Package Manager(推奨)**
1. **Window → Package Manager** を開く
2. **+ → Add package from git URL…** をクリック
3. `https://github.com/AmirCollider/UnityDirectTMP.git` を貼り付ける
4. **Add** をクリック

**方法B — 手動インストール**
1. このリポジトリをダウンロードまたはクローン
2. `UnityDirectTMP` フォルダをプロジェクトの `Assets` フォルダにコピー
3. Unityが自動的にコンパイルします。再起動は不要です

### 🚀 使い方

**30秒でできる導入**
1. すでに `TextMeshProUGUI` か `TextMeshPro` が付いているGameObjectを選択
2. **Add Component → Unity DirectTMP → Direct Font**
3. **Font File** の欄に `.ttf` / `.otf` をドラッグ

以上です。日本語でもペルシャ語でも韓国語でも、そのまま表示されます。

**メニュー**

インストール後、Unityの上部メニューバーに **Unity DirectTMP** という新しいメニューが追加されます。

```
Unity DirectTMP
├── Convert
│   ├── Selected Objects
│   ├── Current Scene
│   └── Whole Project        (すべてのScene + すべてのPrefabを一括で)
├── Fallback Chain…          (一度並べれば、どこでも同じ順番で)
├── Font Cache
│   ├── Show Cache Folder
│   └── Clear Cache
├── Settings…                (サンプリングサイズ、アトラスサイズ、レンダーモード)
└── About Unity DirectTMP
```

すべてのメニュー操作は、Projectウィンドウでフォントファイルやフォルダを右クリックしたコンテキストメニューからも実行できます。

**コードから**

```csharp
using UnityDirectTMP;

// ゲームに同梱するフォント(StreamingAssets からの相対パス)
directFont.SetFontFile("Fonts/NotoSansJP-Regular.ttf");

// ダウンロードしたばかりの、メモリ上のフォント
directFont.SetFontBytes(request.downloadHandler.data);

// UI全体を一行で
DirectTMP.SetGlobalFont(fontBytes);

// テキストの多いシーンの前に、グリフを先に温めておく
DirectTMP.Preload(fontBytes, "こんにちは世界 سلام دنیا Hello");
```

### 🧩 仕組み

魔法はありませんし、TextMeshProの内部を書き換えてもいません。DirectTMPが変えているのは、**グリフがどこから来るか**、それだけです。

```
あなたの .ttf / .otf / .ttc
        │
        ▼
┌────────────────────────────────────────────┐
│              Unity DirectTMP               │
│  フォントファイルを生のまま読み込む            │
│  ライブなフォントフェイスとして開く            │
│  各グリフを初回使用時にラスタライズ            │
│  フォント + サイズ + レンダーモードでキャッシュ  │
└────────────────────────────────────────────┘
        │
        ▼
動的なフォントアセットが、いま使っている
TextMeshProUGUI / TextMeshPro にそのまま渡されます
——同じコンポーネント、同じマテリアル、同じ使い心地のまま
```

**パッケージ構成**

```
UnityDirectTMP/
├── Runtime/
│   ├── DirectFont.cs           ← TMPの隣に置くコンポーネント
│   ├── DirectFontLoader.cs     ← フォントファイル → フォントフェイス
│   ├── DirectFontCache.cs      ← (フォント + 設定)ごとに1アトラス
│   ├── DirectFontFallback.cs   ← 順序付きフォールバック
│   └── DirectTMP.cs            ← 小さな公開API
├── Editor/
│   ├── DirectFontInspector.cs  ← Inspector描画 + ライブプレビュー
│   ├── DirectTMPMenu.cs        ← Unity DirectTMP メニュー
│   ├── DirectTMPConverter.cs   ← Scene / Prefab の一括変換
│   └── DirectTMPSettings.cs
├── Samples~/
│   └── MultilingualDemo/       ← 1シーン、1フォント、12言語
└── Docs~/
```

**知っておくと便利なこと**

- 🔤 各グリフのラスタライズは1回きりで、その後はキャッシュされます。コストは初回だけで、毎フレームではありません。新しいCJKテキストを1フレームで大量に表示する予定なら、`Preload` を使ってください。
- 📦 フォントファイルはフォントファイルのままです。プロジェクトに焼き込まれるものがないので、フォントの差し替えはドラッグ&ドロップだけで済みます。
- ⚖️ フォントのライセンス確認はご自身で。ビルドに `.ttf` を同梱することは再配布にあたります。

### 🌍 対応スクリプト

| 文字体系 | グリフ | 備考 |
|---|---|---|
| ラテン / キリル / ギリシャ | ✅ | 設定不要 |
| CJK — 日本語 / 中文 / 한국어 | ✅ | 必要な分だけ。40MBのアトラスを常駐させません |
| アラビア文字 — فارسی / العربية / اردو | ✅ | 表示可。接続形(シェーピング)はロードマップ |
| デーヴァナーガリー / タイ / ヘブライ | ✅ | 表示可。複雑なシェーピングはロードマップ |
| カラー絵文字(COLR / CBDT) | 🚧 | 予定 |

### 🧠 SDFフォントアセットを焼くのでは駄目なのか?

ゲームが描画する文字がすべて分かっている場合——たとえば英語だけ、文言も固定のUIなら——焼くのは正しい選択です。正しくなくなるのは、テキストが自分の管理外から来た瞬間です。プレイヤー名、チャット、ユーザー投稿、先週届いた有志の翻訳、漢字3,000字を手作業で列挙しなければならない日本語ローカライズ。

Unity DirectTMP は、その「second world」のためのものです。どのグリフを持っているかは、フォントファイル自身がとっくに知っています。DirectTMPは、それを知らないふりをするのをやめただけです。

### 🗺️ ロードマップ

- [x] 任意のTMPコンポーネントで `.ttf` / `.otf` を直接使用
- [x] `StreamingAssets` / `byte[]` / ダウンロードファイルからの実行時読み込み
- [ ] 再利用できるScriptableObjectとしての順序付きフォールバック
- [ ] `.ttc` コレクション対応(フェイスインデックス選択)
- [ ] プレイヤーのOSにインストール済みのフォントを利用
- [ ] アラビア文字のシェーピング(ペルシャ語 / アラビア語 / ウルドゥー語の接続形)をフォント自身のOpenTypeテーブルから
- [ ] カラーフォント・絵文字フォント(COLR / CBDT)
- [ ] バリアブルフォントの軸——ウェイト、幅、斜体 🌙

### 🤝 コントリビュート

IssueやPull Requestはいつでも歓迎です。

### 📜 ライセンス

MIT — 詳細は [LICENSE](LICENSE) をご覧ください。

### 💌 クレジット

🖋️を込めて、[AmirCollider](https://github.com/AmirCollider) より。

Unity DirectTMPのおかげでFont Asset Creatorを開かずに済んだなら、リポジトリへの ⭐ がとても励みになります。

<p align="right"><a href="#top">⬆ トップに戻る</a></p>

<p align="center">━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</p>

<a id="persian"></a>
<div dir="rtl" align="right">

## ⭐ فارسی

تا حالا شده یه فونت خوشگل پیدا کنی، بندازیش توی یونیتی، یه ساعت توی Font Asset Creator بشینی و رِنج کاراکترها رو انتخاب کنی — و بازم همین که یکی اسم خودش رو فارسی یا ژاپنی تایپ کرد، یه ردیف □□□ ببینی؟ **Unity DirectTMP** کل این مرحله رو حذف می‌کنه.

این یه کامپوننت کوچیکه که کنار `TextMeshProUGUI` یا `TextMeshPro` می‌شینه و اجازه می‌ده به‌جای یه فونت‌اسِت SDF از پیش پخته‌شده، **خودِ فایل فونت** — یه `.ttf` یا `.otf` ساده — رو بهش بدی. گلیف‌ها همون لحظه‌ای که برای اولین بار قراره کشیده بشن، مستقیم از همون فایل ساخته می‌شن؛ یعنی هر کاراکتری که فونت واقعاً داره، بدون معطلی در دسترسه: فارسی، عربی، ژاپنی، چینی، کره‌ای، تایلندی، سیریلیک، نمادها، همه‌ش. نه لازمه از قبل حدس بزنی چه کاراکترهایی لازم می‌شه، نه اطلسی رو دوباره بسازی، نه مربع‌های خالی. 🍰

### ✨ ویژگی‌ها

- 🅰️ **فایل فونت رو بده، نه فونت‌اسِت رو** — یه `.ttf` یا `.otf` رو بکش روی کامپوننت، تموم. کارهای مربوط به SDF بی‌سروصدا پشت صحنه انجام می‌شه.
- 🌏 **هر خطی که فونت پشتیبانی می‌کنه، همون لحظه** — گلیف‌ها هر وقت لازم بشن از فایل فونت ساخته می‌شن؛ پس فونتی که ۲۰٬۰۰۰ کانجی داره، واقعاً ۲۰٬۰۰۰ کانجی داره. دیگه هیچ‌وقت لازم نیست لیست کاراکترها رو دستی بنویسی.
- 🚫 **خداحافظ Font Asset Creator** — نه پنجره‌ی انتخاب رِنج کاراکتر، نه *Generate Font Atlas*، نه ساختن دوباره هر بار که متن عوض می‌شه.
- 🔗 **زنجیره‌ی فال‌بک، با ترتیب دلخواه خودت** — یه فونت لاتین برای UI، یه فونت CJK و یه فونت نماد رو پشت سر هم بچین؛ برای هر کاراکتر، اولین فونتی که واقعاً اون گلیف رو داشته باشه انتخاب می‌شه.
- 💾 **لود کردن فونت در زمان اجرا** — از `StreamingAssets`، از `persistentDataPath`، از یه `byte[]` که تازه دانلود کردی، یا از فونت‌های نصب‌شده‌ی خودِ کاربر. مخصوص آپدیت‌های ترجمه، انتخاب فونت توسط بازیکن و ماد.
- ♻️ **اطلسی که خودش حواسش به خودشه** — گلیف‌ها به‌مرور که استفاده می‌شن اضافه می‌شن، اطلس در صورت نیاز روی چند تکسچر رشد می‌کنه، و گلیف‌های بلااستفاده رو می‌شه موقع عوض شدن سین پاک کرد.
- 🎛️ **همون Inspector همیشگی** — فقط یه کامپوننت کوچیک کنار TMP. متریال، Outline، گرادیان، Underlay، Auto Size و کلید RTL دقیقاً مثل قبل کار می‌کنن.
- ⚡ **کَش می‌شه، دوباره ساخته نمی‌شه** — هر فونت بر اساس *(فایل + سایز نمونه‌برداری + حالت رندر)* کَش می‌شه؛ پس یه صفحه با ۲۰۰ لیبل که همه یه فونت دارن، فقط و فقط یه اطلس می‌سازه.
- 🧹 **ریپازیتوری‌ای که سبک می‌مونه** — تنها چیزی که کامیت می‌کنی همون `.ttf` هست. نه تکسچرهای چندمگابایتی توی سورس‌کنترل، نه دردسر کانفلیکت روی فایل‌های باینری.
- 🧩 **هم Runtime هم Editor** — توی Play Mode و توی بیلد کار می‌کنه، و همون موقع طراحی هم توی Scene View زنده پیش‌نمایش می‌ده.

### 📋 پیش‌نیازها

- یونیتی **2021.3 LTS** به بعد (یونیتی 6 هم پشتیبانی می‌شه)
- **TextMeshPro 3.0** به بعد (همون نسخه‌ای که با این نسخه‌های یونیتی میاد)
- بدون هیچ وابستگی دیگه‌ای به کتابخونه‌ی شخص‌ثالث

### 📦 نصب

**روش الف — Package Manager (پیشنهادی)**
۱. برو به **Window → Package Manager**
۲. کلیک کن روی **+ → Add package from git URL…**
۳. این آدرس رو بچسبون: `https://github.com/AmirCollider/UnityDirectTMP.git`
۴. کلیک کن روی **Add**

**روش ب — نصب دستی**
۱. این ریپازیتوری رو دانلود یا کلون کن
۲. پوشه‌ی `UnityDirectTMP` رو بریز توی پوشه‌ی `Assets` پروژه‌ت
۳. یونیتی خودش کامپایلش می‌کنه؛ نیازی به ری‌استارت نیست

### 🚀 نحوه‌ی استفاده

**نسخه‌ی سی‌ثانیه‌ای**
۱. هر GameObject ای که از قبل `TextMeshProUGUI` یا `TextMeshPro` داره رو انتخاب کن
۲. **Add Component → Unity DirectTMP → Direct Font**
۳. فایل `.ttf` / `.otf` خودت رو بکش توی فیلد **Font File**

همین. فارسی تایپ کن، ژاپنی، کره‌ای، هرچی — رندر می‌شه.

**منو**

بعد از نصب، توی نوار بالای یونیتی یه منوی جدید به اسم **Unity DirectTMP** اضافه می‌شه.

```
Unity DirectTMP
├── Convert
│   ├── Selected Objects
│   ├── Current Scene
│   └── Whole Project        (همه‌ی سین‌ها + همه‌ی پریفب‌ها، یکجا)
├── Fallback Chain…          (یه بار بچینش، همه‌جا همون ترتیب)
├── Font Cache
│   ├── Show Cache Folder
│   └── Clear Cache
├── Settings…                (سایز نمونه‌برداری، سایز اطلس، حالت رندر)
└── About Unity DirectTMP
```

همه‌ی گزینه‌های منو از راست‌کلیک روی هر فایل فونت یا هر فولدر توی پنجره‌ی Project هم در دسترسن.

**از توی کد**

```csharp
using UnityDirectTMP;

// فونتی که همراه بازی ارسال می‌شه (مسیر نسبت به StreamingAssets)
directFont.SetFontFile("Fonts/Vazirmatn-Regular.ttf");

// فونتی که تازه دانلود شده و هنوز توی حافظه‌ست
directFont.SetFontBytes(request.downloadHandler.data);

// یه خط برای کل رابط کاربری
DirectTMP.SetGlobalFont(fontBytes);

// گرم کردن گلیف‌ها قبل از یه سین پر از متن
DirectTMP.Preload(fontBytes, "こんにちは世界 سلام دنیا Hello");
```

### 🧩 چطور کار می‌کنه

نه جادویی در کاره، نه چیزی توی دل TextMeshPro بازنویسی شده. تنها چیزی که DirectTMP عوض می‌کنه اینه که **گلیف‌ها از کجا میان**.

```
فایل .ttf / .otf / .ttc خودت
        │
        ▼
┌────────────────────────────────────────────┐
│              Unity DirectTMP               │
│  فایل فونت رو خام می‌خونه (دیسک یا حافظه)    │
│  به‌عنوان یه font face زنده بازش می‌کنه       │
│  هر گلیف رو موقع اولین استفاده می‌سازه        │
│  بر اساس فونت + سایز + حالت رندر کَش می‌کنه   │
└────────────────────────────────────────────┘
        │
        ▼
یه فونت‌اسِت داینامیک، که مستقیم تحویل همون
TextMeshProUGUI / TextMeshPro فعلی‌ت داده می‌شه —
همون کامپوننت، همون متریال، همون همه‌چیز
```

**ساختار پکیج**

```
UnityDirectTMP/
├── Runtime/
│   ├── DirectFont.cs           ← کامپوننتی که کنار TMP می‌ذاری
│   ├── DirectFontLoader.cs     ← فایل فونت → font face زنده
│   ├── DirectFontCache.cs      ← یه اطلس برای هر (فونت + تنظیمات)
│   ├── DirectFontFallback.cs   ← زنجیره‌های فال‌بک ترتیب‌دار
│   └── DirectTMP.cs            ← ای‌پی‌آی عمومی و کوچیک
├── Editor/
│   ├── DirectFontInspector.cs  ← رسم Inspector + پیش‌نمایش زنده
│   ├── DirectTMPMenu.cs        ← منوی Unity DirectTMP
│   ├── DirectTMPConverter.cs   ← تبدیل گروهی سین‌ها و پریفب‌ها
│   └── DirectTMPSettings.cs
├── Samples~/
│   └── MultilingualDemo/       ← یه سین، یه فونت، دوازده زبان
└── Docs~/
```

**خوبه که بدونی**

- 🔤 هر گلیف فقط یه بار ساخته می‌شه و بعدش کَش می‌مونه — هزینه‌ش فقط بار اوله، نه هر فریم. اگه قراره یه دیوار متن تازه‌ی ژاپنی/چینی رو توی یه فریم نشون بدی، اول `Preload` کن.
- 📦 فایل فونت همون فایل فونت باقی می‌مونه. چیزی توی پروژه پخته نمی‌شه، پس عوض کردن فونت فقط یه درگ‌اند‌دراپه، نه یه بیلد دوباره.
- ⚖️ بررسی لایسنس فونت هنوز با خودته — قرار دادن یه `.ttf` داخل بیلد، یعنی بازنشر.

### 🌍 پشتیبانی از خط‌ها

| خط | گلیف | توضیح |
|---|---|---|
| لاتین / سیریلیک / یونانی | ✅ | هیچ تنظیمی نمی‌خواد |
| CJK — 日本語 / 中文 / 한국어 | ✅ | فقط به‌اندازه‌ی نیاز؛ اطلس ۴۰ مگابایتی توی رم نمی‌مونه |
| خط عربی — فارسی / العربية / اردو | ✅ | گلیف‌ها رندر می‌شن؛ چسبیدن حروف (Shaping) توی نقشه‌ی راهه |
| دواناگری / تایلندی / عبری | ✅ | گلیف‌ها رندر می‌شن؛ شکل‌دهی پیچیده توی نقشه‌ی راهه |
| ایموجی رنگی (COLR / CBDT) | 🚧 | برنامه‌ریزی‌شده |

### 🧠 خب چرا فقط یه فونت‌اسِت SDF نپزیم؟

پختن فونت‌اسِت واقعاً جواب درستیه، وقتی دقیقاً می‌دونی بازیت قراره چه کاراکترهایی نشون بده — مثلاً یه UI فقط انگلیسی با متن‌های ثابت. اما همون لحظه‌ای که متن از جایی میاد که دستت نیست، دیگه جواب درستی نیست: اسم بازیکن‌ها، چت، محتوای ساخته‌ی کاربر، ترجمه‌ای که هفته‌ی پیش یکی از جامعه‌ی بازی برات فرستاده، یا یه لوکالایز ژاپنی که باید سه‌هزار کانجی رو دستی توش ردیف کنی.

Unity DirectTMP برای همون دنیای دومه. فایل فونت از قبل دقیقاً می‌دونه چه گلیف‌هایی داره — DirectTMP فقط دست از این برداشته که وانمود کنه نمی‌دونه.

### 🗺️ نقشه‌ی راه

- [x] استفاده‌ی مستقیم از `.ttf` / `.otf` روی هر کامپوننت TMP
- [x] لود در زمان اجرا از `StreamingAssets`، از `byte[]` یا از فایل دانلودشده
- [ ] زنجیره‌ی فال‌بک ترتیب‌دار به‌شکل یه ScriptableObject قابل‌استفاده‌ی مجدد
- [ ] پشتیبانی از `.ttc` با انتخاب ایندکس فیس
- [ ] استفاده از فونت‌های نصب‌شده روی سیستم خود کاربر
- [ ] چسبیدن حروف خط عربی (فارسی / عربی / اردو) با استفاده از جدول‌های OpenType خود فونت
- [ ] فونت‌های رنگی و ایموجی (COLR / CBDT)
- [ ] محورهای فونت متغیر — وزن، عرض، شیب 🌙

### 🤝 مشارکت

Issue و Pull Request همیشه خوش‌اومدن.

### 📜 لایسنس

MIT — جزئیات توی فایل [LICENSE](LICENSE).

### 💌 با تشکر از

با 🖋️ ساخته شده توسط [AmirCollider](https://github.com/AmirCollider).

اگه Unity DirectTMP یه سفر به Font Asset Creator رو ازت کم کرد، یه ⭐ روی ریپو خیلی دلگرم‌کننده‌ست.

<p align="right"><a href="#top">⬆ برگشت به بالا</a></p>

</div>

<p align="center">━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</p>

<p align="center"><sub>Made with 🖋️ 🍰 ⭐ for Unity — <a href="https://github.com/AmirCollider">AmirCollider</a></sub></p>
