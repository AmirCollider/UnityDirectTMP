<a id="top"></a>
<p align="center">
  <img src="Docs~/mascot.svg" alt="Inky, the Unity DirectTMP mascot" width="150"/>
</p>

<h1 align="center">🖋️ Unity DirectTMP</h1>

<p align="center"><em>Hand TextMeshPro the font file itself — and every language just works.</em></p>
<p align="center"><em>フォントファイルをそのままTextMeshProへ。どんな言語も、そのまま表示されます。</em></p>
<p align="center"><em>فایل فونت رو مستقیم بده به TextMeshPro — هر زبونی، همون‌جوری که هست.</em></p>

<p align="center">
  <a href="#english">English</a> ・
  <a href="#japanese">日本語</a> ・
  <a href="#persian">فارسی</a>
</p>

<p align="center">
  <img alt="license" src="https://img.shields.io/badge/license-MIT-14808C?style=flat-square">
  <img alt="price" src="https://img.shields.io/badge/price-free-14808C?style=flat-square">
  <img alt="unity version" src="https://img.shields.io/badge/Unity-2021.3%2B-0B5A63?style=flat-square&logo=unity&logoColor=white">
  <img alt="textmeshpro" src="https://img.shields.io/badge/TextMeshPro-3.0%2B-0B5A63?style=flat-square">
  <img alt="theme" src="https://img.shields.io/badge/theme-Inkwell-F0A73E?style=flat-square">
  <img alt="languages" src="https://img.shields.io/badge/UI-EN%20%2F%20JA%20%2F%20FA-F0A73E?style=flat-square">
  <img alt="tofu level" src="https://img.shields.io/badge/tofu-0%25-E8927C?style=flat-square">
</p>

<p align="center">
  <a href="https://amircollider.n95pluss.workers.dev/unity-directtmp"><b>Product page</b></a> ・
  <a href="https://amircollider.n95pluss.workers.dev/tools">All tools</a> ・
  <a href="CHANGELOG.md">Changelog</a>
</p>

<p align="center">━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</p>

<a id="english"></a>
## 🖋️ English

Ever found the perfect font, dropped it into Unity, spent an hour in the Font Asset Creator picking character ranges — and still got a row of □□□ the moment someone typed their own name in Japanese or Persian? **Unity DirectTMP** skips that whole step.

It's a small component that sits next to your `TextMeshProUGUI` / `TextMeshPro` and lets you assign the **font file itself** — a plain `.ttf` or `.otf` — instead of a pre-baked SDF font asset. Glyphs are rasterized straight from that file the moment they're first drawn, so every character the font actually contains is available immediately: Japanese, Chinese, Korean, Persian, Arabic, Thai, Cyrillic, symbols, all of it. No character sets to guess in advance, no atlases to rebuild, no tofu.

And since **1.0**, it does the same thing for Unity itself.

### 🖥️ New in 1.0 — it can restyle the Editor too

Name a GameObject `敵スポーナー` or a folder `فونت‌ها`, and Unity will store it correctly and then show you `□□□□□` forever. The asset is fine. Unity's own interface is drawn with Unity's own font, and nothing in a TextMeshPro package has ever touched it.

**Unity DirectTMP ▸ Editor Font ▸ Choose a Font…** points the menu bar, the Hierarchy, the Inspector, the Project window and the Console at a font of your choosing.

- Nothing is written to your Unity installation, and no built-in asset is edited on disk. The change lives in memory for the session, so **quitting Unity is always a complete undo**.
- A font that cannot draw basic Latin letters, digits and punctuation is **refused, not warned about** — otherwise the menu item that undoes the change would itself be a row of empty boxes.
- Right-click any `.ttf` in the Project window → **Unity DirectTMP ▸ Use This Font For The Editor** is the one-click version.

### 🗂️ New in 1.0 — the Font Catalog

**Unity DirectTMP ▸ Font Catalog…** lists every font in the project *and* every font installed on the machine, and reads each file directly to tell you the truth about it:

- the **family and style the foundry actually wrote into the file** — not the filename, which is why `subset-2.ttf` tells you nothing today
- the **real glyph count**, from the font's own `maxp` table
- **which writing systems it can set**, from the font's own `cmap`: green for full coverage, amber for partial, grey for none

That amber badge is the point. A font with Arabic but without `پ گ چ ژ` cannot set Persian, and a boolean "supports Arabic" flag will happily tell you it can.

Type your actual UI string into the preview field once, and all of them render it at the same time.

### ✨ Everything else

- 🅰️ **Point at a font file, not a font asset** — drag a `.ttf` or `.otf` onto the component and you're done. The SDF side of things is handled for you, quietly, in the background.
- 🌏 **Every script the font supports, instantly** — glyphs come from the font file on demand, so a font that covers 20,000 kanji covers 20,000 kanji. You never enumerate a character set again.
- 🚫 **Goodbye, Font Asset Creator** — no character-range dialog, no *Generate Font Atlas*, no regenerating every time the text changes.
- 🔗 **Fallback chains, ordered by you** — line up a Latin UI font, a CJK font, and a symbol font; the first one that actually has the glyph wins, per character.
- 💾 **Load fonts at runtime** — from `StreamingAssets`, `persistentDataPath`, a downloaded `byte[]`, or the player's own installed fonts. Made for localization patches, user-selectable fonts, and modding.
- ♻️ **An atlas that manages itself** — glyphs are added as they're used, atlases grow across multiple textures when needed, and unused glyphs can be cleared on scene change.
- ⚡ **Cached, not rebuilt** — font faces are cached per *(file + sampling size + render mode)*, so a screen with 200 labels sharing one font builds exactly one atlas.
- 🧹 **A repo that stays small** — the `.ttf` is the only thing you commit. No multi-megabyte atlas textures in version control, no merge conflicts on binary assets.
- 🩺 **A health check that explains itself** — **Unity DirectTMP ▸ Health Check…** reports what the package can see of your project, fixes what it can in one click, and copies the whole report to your clipboard for a bug report.
- ✍️ **Arabic-script text that actually reads** — Persian, Arabic and Urdu are joined up, put in reading order and wrapped correctly in your own labels, not only in this package's windows. See [New in 1.1.0](#new-in-110).
- 🌐 **Trilingual editor UI** — English, 日本語 and فارسی, switchable from **Unity DirectTMP ▸ Language**.

<a id="new-in-110"></a>
### ✍️ New in 1.1.0 — labels that read

A font file with Persian in it stops the boxes. It does not make Persian
*read*.

TextMeshPro hands each codepoint to the font in the order it is stored, so a
label with every glyph it needs still draws a row of disconnected letters
running the wrong way. `isRightToLeftText` reverses that order without
joining anything. No font fixes it, because it is not a font problem — and
`ی` and `ر` get the blame in the bug reports only because their isolated
shapes look least like their joined ones.

**Add a Direct Text** next to any `TextMeshProUGUI` / `TextMeshPro` — or
leave **Shape Text** on in Direct Font, which adds it for you:

```csharp
label.text = "سلام دنیا";   // still exactly this, in logical order
```

What that gets you:

- **Letters join.** Initial, medial, final and isolated forms, the four
  lam-alef ligatures, harakat that must not break a join, and ZWNJ for
  `می‌شه` and `فونت‌ها`.
- **Words read right to left**, with Latin runs and numbers still reading
  forwards inside them, punctuation on the correct end and brackets mirrored.
- **Wrapped paragraphs read top to bottom.** This one is the reason it is a
  component rather than a function: reordered text cannot be wrapped, because
  reordering makes the *last* word of the paragraph the leftmost thing on the
  line. The label is laid out twice — once in reading order so TextMeshPro
  reports where its lines fell, then once more with each of those lines
  reordered on its own.
- **Rich text survives.** `<b>` and `<color>` move with the words they wrap
  and swap ends when the span turns around; letters still join across a tag.
- **Fonts without the presentation forms degrade, rather than break.**
  Vazirmatn, Noto Sans Arabic and most modern Persian faces carry the plain
  letters and their OpenType rules and nothing at U+FE70. Every form is
  checked against the font first, and falls back to the letter itself.
- **`label.text` is still your string.** The work happens in TextMeshPro's own
  `textPreprocessor`, so what you set is what you read back — and anything
  that writes to a label is covered without knowing the component exists: a
  `TMP_Dropdown` filling its caption, a localisation package, a coroutine
  typing one character at a time.

For a scene you already built, and for text you draw yourself:

```csharp
using UnityDirectTMP;

DirectTMP.ShapeAll();                          // every TMP label loaded, dropdowns included
string visual = DirectTMP.Prepare("سلام دنیا"); // one string, ready to draw
```

One deliberate exception: the text component *inside* a `TMP_InputField` is
left alone. Reordering it would put the caret in the wrong place and let
typing edit the wrong end of the word. Its placeholder is shaped as normal.

<a id="new-in-101"></a>
### ✍️ 1.0.1 — the shaper and the bidi algorithm underneath

Unity does not join Arabic-script letters, and it does not reorder
right-to-left text. Neither does TextMeshPro — `isRightToLeftText` reverses
the order and leaves every letter in its standalone form. So a label reading
`سلام دنیا` comes out as a row of disconnected letters running the wrong way,
and no font fixes it, because it is not a font problem.

1.0.1 fixes it, starting with this package's own windows — 1.0's Persian
interface had exactly this bug, which is an embarrassing thing for a text tool
to ship.

Three small, dependency-free classes in `Runtime/` do the work, and they are
public because your labels have the same problem:

```csharp
using UnityDirectTMP;

// Joined up, in reading order, ready to draw.
label.text = DirectDisplayText.Prepare("سلام دنیا");

// Nothing right-to-left in it? Returned unchanged, by reference.
DirectDisplayText.Prepare("Hello world");   // same string, no allocation

// The two halves, if you want them separately.
DirectArabicShaper.Shape("فونت");           // initial / medial / final forms
DirectBidi.Reorder("אב TMP גד");            // "דג TMP בא"
```

What it handles: the Arabic block plus the letters Persian and Urdu add
(`پ گ چ ژ ک ی`), the four lam-alef ligatures, harakat that must not break a
join, ZWNJ (`می‌شه`, `فونت‌ها`), Latin runs inside Persian sentences, both
kinds of digit, mirrored brackets, and paragraph wrapping — which has to break
lines *before* reordering or the paragraph reads bottom-up.

What it does not handle yet: shaping from a font's own OpenType `GSUB` table.
It uses the Unicode presentation forms, which every Arabic-capable font
carries.

### 📋 Requirements

- Unity **2021.3 LTS** or newer (Unity 6.x supported)
- **TextMeshPro 3.0+** — bundled with those Unity releases; the package declares `com.unity.ugui`, which carries TMP on Unity 6
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

A welcome screen appears once after installing. It is also at **Unity DirectTMP ▸ Welcome** whenever you want it back.

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
├── Font Catalog…            (every font, and what's really in it)
├── Editor Font
│   ├── Choose a Font…       (restyle Unity's own interface)
│   └── Revert to Unity Default
├── Convert
│   ├── Selected Objects
│   ├── Current Scene
│   └── Whole Project        (every Scene + every Prefab, in one pass)
├── Fallback Chain…          (order your fonts once, reuse everywhere)
├── Font Cache
│   ├── Show Cache Folder
│   └── Clear Cache
├── Language                 (English / 日本語 / فارسی)
├── Settings…                (sampling point size, atlas size, render mode)
├── Health Check…            (what's working, what isn't, and why)
├── Welcome
└── About Unity DirectTMP
```

The three windows are also under **Window ▸ Unity DirectTMP**, and every batch action is on the Project window's right-click menu for any font file or folder.

> **The menu isn't there?** That is almost always a compile error rather than a missing menu — when an editor assembly fails to compile, Unity drops every menu item in it at once. Check the Console first, then open **Window ▸ Unity DirectTMP ▸ Health Check**, which is written to explain exactly this case.

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

// ask a font file what it contains, without building an atlas
DirectFontFileInfo info = DirectFontFile.Read(path);
Debug.Log($"{info.DisplayName}: {info.GlyphCount} glyphs");
Debug.Log(info.HasCodepoint('گ') ? "can set Persian" : "cannot set Persian");
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
│   ├── DirectFontFactory.cs    ← the TextMeshPro bridge
│   ├── DirectFontCache.cs      ← one atlas per (font + settings)
│   ├── DirectFontFallback.cs   ← ordered fallback chains
│   ├── DirectFontFile.cs       ← reads name / maxp / cmap straight from the file
│   ├── DirectFontScripts.cs    ← which writing system a codepoint belongs to
│   ├── DirectArabicShaper.cs   ← Arabic-script letters, joined
│   ├── DirectBidi.cs           ← the Unicode bidirectional algorithm (UAX #9)
│   ├── DirectDisplayText.cs    ← both of the above, in the order they must happen
│   ├── DirectRichText.cs       ← the same, over text with <b> and <color> in it
│   ├── DirectText.cs           ← the component that makes a TMP label read
│   └── DirectTMP.cs            ← the small public API
├── Editor/
│   ├── Brand/                  ← the Inkwell palette, Inky, the text pack
│   ├── Catalog/                ← the Font Catalog
│   ├── EditorFont/             ← restyling Unity's own interface
│   ├── DirectFontInspector.cs  ← Inspector drawer + live read-out
│   ├── DirectTMPConverter.cs   ← batch conversion for Scenes & Prefabs
│   └── DirectTMPHealthCheck.cs ← the diagnostics report
├── Tests/Editor/               ← EditMode tests
├── Samples~/MultilingualDemo/  ← one scene, one font, twelve languages
└── Docs~/
```

**Good to know**

- 🔤 Each glyph is rasterized once and then cached — the cost lands on first use, never per frame. If you're about to reveal a wall of brand-new CJK text in a single frame, `Preload` it.
- 📦 The font file stays a font file. Nothing is baked into your project, so swapping a font is a drag-and-drop, not a rebuild.
- ⚖️ Font licensing is still yours to check — shipping a `.ttf` inside a build counts as redistribution. The Editor Font feature does not redistribute anything: it only points the Editor at a font already on your machine.

### 🌍 Script Support

| Script | Glyphs | Notes |
|---|---|---|
| Latin / Cyrillic / Greek | ✅ | Nothing to configure |
| CJK — 日本語 / 中文 / 한국어 | ✅ | On demand, so no 40 MB atlas sitting in memory |
| Arabic script — فارسی / العربية / اردو | ✅ | Joined, reordered and wrapped by `DirectText` on your labels, and by `DirectDisplayText` in the Editor's own windows |
| Hebrew | ✅ | Reordered by the same two; Hebrew needs no joining |
| Devanagari / Thai | ✅ | Glyphs render; reordering and conjunct shaping are on the roadmap |
| Color emoji (COLR / CBDT) | 🚧 | Planned |

The Font Catalog reports coverage per script from the font's own `cmap`, with a distinct **partial** state — which is how you find out a font has Arabic but not the four Persian letters *before* shipping a Persian build.

### 🧠 Why not just bake an SDF font asset?

Baking is genuinely the right answer when you know every character your game will ever draw — an English-only UI with a fixed word list, for example. It stops being the right answer the moment the text comes from somewhere you don't control: player names, chat, user-generated content, a community translation that landed last week, a Japanese localization that would need three thousand kanji enumerated by hand.

Unity DirectTMP is for that second world. The font file already knows exactly which glyphs it has — DirectTMP just stops pretending it doesn't.

### 🗺️ Roadmap

- [x] Direct `.ttf` / `.otf` on any TMP component
- [x] Runtime loading from `StreamingAssets`, `byte[]`, or a downloaded file
- [x] Ordered fallback chains as a reusable ScriptableObject
- [x] Use the player's own installed system fonts
- [x] Font Catalog with real per-script coverage read from the file
- [x] Restyle the Unity Editor's own interface
- [x] Arabic-script joining forms and bidirectional reordering (Persian / Arabic / Urdu / Hebrew)
- [x] The same on your own TMP labels, with rich text and wrapped lines handled
- [ ] `.ttc` collection support with a face-index picker
- [ ] Arabic-script shaping from the font's own OpenType `GSUB` table, for contextual alternates
- [ ] Color & emoji fonts (COLR / CBDT)
- [ ] Variable font axes — weight, width, slant

### 🎨 Brand

The mascot is **Inky** — a teal inkwell with a brass nib and three glyph bubbles rising out of it: `A`, `あ`, `س`. That is the tool's whole pitch drawn in one picture.

The palette is **Inkwell**: teal ink `#14808C`, deep ink `#0B5A63`, brass `#F0A73E`, cream paper `#FBF6EC`, blush `#E8927C`, night `#1B1725`. The same six values are used by the editor windows, the product page and the badges above, so the tool and its shop window are always the same colour.

Inky is drawn in code, not shipped as an image — no import settings to get wrong, no compression to blur it, and it renders crisply at any editor DPI.

### 🧪 Tests & CI

- EditMode tests cover the font-file parser (with hand-built fonts), script coverage, the Editor-font safety rules, the catalog's filtering and sorting, the cache key, the fallback ordering, the path helpers, and the consistency of the version and menu tree against this README.
- Arabic-script text gets its own two files: `DirectTextDisplayTests` for the shaper and the bidi algorithm on plain strings, and `DirectRichTextTests` for what a real label brings with it — rich-text tags that have to move with the words they wrap, wrapped lines that have to read top to bottom, and fonts that carry no presentation forms at all.
- `python3 .github/scripts/validate_package.py` runs the whole package check with no Unity licence — version drift, missing `.meta` files, duplicate GUIDs, required files, and the invariants no test can reach.

### 🤝 Contributing

Issues and pull requests are always welcome — see [CONTRIBUTING.md](CONTRIBUTING.md).

### 📜 License

MIT — see [LICENSE](LICENSE).

### 💌 Credits

Made with 🖋️ by [AmirCollider](https://github.com/AmirCollider).

Free and MIT-licensed. A ⭐ on the repo is the whole price.

Also on the shelf: 🧋 [**Unity DocSnap**](https://amircollider.n95pluss.workers.dev/unity-docsnap) — snaps a whole Unity project into an offline website, for humans and AI alike.

<p align="right"><a href="#top">⬆ Back to top</a></p>

<p align="center">━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</p>

<a id="japanese"></a>
## 🍵 日本語

気に入ったフォントを見つけて、Unityに入れて、Font Asset Creatorで文字セットを選ぶのに1時間——それでもプレイヤーが日本語やペルシャ語で自分の名前を入力した瞬間、□□□ が並ぶ。そんな経験はありませんか?**Unity DirectTMP** は、その工程をまるごと省きます。

これは `TextMeshProUGUI` / `TextMeshPro` の隣に置く小さなコンポーネントで、焼き上げ済みのSDFフォントアセットではなく、**フォントファイルそのもの**(ふつうの `.ttf` や `.otf`)を直接指定できるようにするものです。グリフは最初に描画された瞬間にフォントファイルから生成されるので、そのフォントが持っている文字はすべて、すぐにそのまま使えます。日本語、中国語、韓国語、ペルシャ語、アラビア語、タイ語、キリル文字、記号——全部です。

そして **1.0** からは、Unity 自身に対しても同じことをします。

### 🖥️ 1.0 の新機能 — エディタ自体のフォントも変えられます

GameObject に `敵スポーナー` と名前を付けたり、フォルダーを `فونت‌ها` と名付けたりすると、Unity はそれを正しく保存したうえで、`□□□□□` と表示し続けます。アセットは正常です。Unity 自身の UI は Unity のフォントで描画されており、TextMeshPro のパッケージがそこに触れたことは一度もなかったからです。

**Unity DirectTMP ▸ Editor Font ▸ Choose a Font…** で、メニューバー・Hierarchy・Inspector・Project ウィンドウ・Console を、選んだフォントで描画させられます。

- Unity のインストール先には一切書き込まず、ビルトインアセットをディスク上で編集することもありません。変更はそのセッションのメモリ上だけに存在するため、**Unity を終了すれば必ず元に戻ります**。
- 基本ラテン文字・数字・記号を描画できないフォントは、**警告ではなく拒否**されます。そうでなければ、この変更を取り消すメニュー項目自体が豆腐になってしまうからです。
- Project ウィンドウで `.ttf` を右クリック →**Unity DirectTMP ▸ Use This Font For The Editor** が、ワンクリック版です。

### 🗂️ 1.0 の新機能 — フォントカタログ

**Unity DirectTMP ▸ Font Catalog…** は、プロジェクト内のすべてのフォントと、この PC にインストールされたすべてのフォントを一覧し、各ファイルを直接読んで実態を表示します。

- **フォント制作者がファイルに書き込んだファミリー名とスタイル**(ファイル名ではありません。`subset-2.ttf` から何も分からないのはそのためです)
- フォント自身の `maxp` テーブルから読んだ**実際のグリフ数**
- フォント自身の `cmap` から判定した**対応する文字体系**: 完全収録は緑、一部収録は琥珀、未収録は灰色

この「琥珀」が肝心です。アラビア文字を持っていても `پ گ چ ژ` を持たないフォントはペルシャ語を組めませんが、「アラビア文字対応」という真偽値のフラグは平然と「対応している」と答えます。

実際に UI に表示する文字列をプレビュー欄に一度入力すれば、すべてのフォントでの見た目を同時に確認できます。

### ✨ そのほかの機能

- 🅰️ **指定するのはフォントアセットではなく、フォントファイル** — `.ttf` や `.otf` をコンポーネントにドラッグするだけ。
- 🌏 **フォントが持つ文字は、すべてそのまま** — 2万字の漢字を含むフォントなら、2万字そのまま使えます。
- 🚫 **Font Asset Creator とお別れ** — 文字範囲のダイアログも、*Generate Font Atlas* もありません。
- 🔗 **順番を自分で決められるフォールバック** — その文字を実際に持っている最初のフォントが、1文字ごとに選ばれます。
- 💾 **実行時のフォント読み込み** — `StreamingAssets`、`persistentDataPath`、ダウンロードした `byte[]`、OS のインストール済みフォントから。
- ♻️ **自分で面倒をみるアトラス** — 使われたグリフから順に追加され、必要に応じて複数テクスチャに拡張されます。
- ⚡ **作り直さず、キャッシュする** — 同じフォントのラベルが200個ある画面でも、アトラスは1つだけです。
- 🩺 **理由まで説明する動作チェック** — **Unity DirectTMP ▸ Health Check…** が状態を報告し、直せるものはワンクリックで直し、レポート全体をクリップボードにコピーします。
- ✍️ **読めるアラビア文字** — ペルシア語・アラビア語・ウルドゥー語を、描画前に接続形へ変換し、表示順に並べ替え、折り返しまで正しく処理します。本パッケージのウィンドウだけでなく、あなたのラベルでも。
- 🌐 **3言語のエディタUI** — English / 日本語 / فارسی。**Unity DirectTMP ▸ Language** で切り替えられます。

### ✍️ 1.1.0 の新機能 — 「読める」ラベル

ペルシア語を含むフォントファイルを渡せば、豆腐は消えます。それだけでは
ペルシア語は**読めるようになりません**。

TextMeshPro は各コードポイントを保存された順のままフォントへ渡すので、
必要なグリフがすべて揃っていても、ラベルにはばらばらの文字が逆向きに
並びます。`isRightToLeftText` は順序を反転するだけで、文字は接続しません。
フォントの問題ではないので、フォントを替えても直りません。

`TextMeshProUGUI` / `TextMeshPro` の隣に **Direct Text** を追加してください。
Direct Font の **Shape Text**（既定でオン）が自動で追加もします:

```csharp
label.text = "سلام دنیا";   // 保存順のまま、この文字列のまま
```

これで得られるもの:

- **文字が接続します** — 語頭形・語中形・語末形・孤立形、ラーム・アリフ合字4種、
  接続を壊してはいけない発音記号、`می‌شه` や `فونت‌ها` の ZWNJ。
- **語が右から左に並びます** — 中のラテン文字と数字は左から右のまま、句読点は
  正しい端に、括弧は鏡像になります。
- **折り返した段落が上から下に読めます。** これが関数ではなくコンポーネントで
  ある理由です。並べ替えたテキストは折り返せません（段落の**最後**の語が行の
  左端に来るため）。そこでラベルを2回レイアウトします — まず読み順のままで
  TextMeshPro に改行位置を報告させ、次にその各行を個別に並べ替えます。
- **リッチテキストが壊れません。** `<b>` や `<color>` は囲んでいる語と一緒に
  移動し、範囲が反転すれば開始タグと終了タグも入れ替わります。タグをまたいで
  文字も接続します。
- **表示形を持たないフォントでも豆腐になりません。** Vazirmatn や Noto Sans
  Arabic など最近のペルシア語書体は、素の文字と OpenType の規則だけを持ち
  U+FE70 には何もありません。各表示形はフォントに存在するか確認され、無ければ
  元の文字へフォールバックします。
- **`label.text` はあなたの文字列のままです。** 処理は TextMeshPro 自身の
  `textPreprocessor` で行われるので、設定した文字列がそのまま読み出せます。
  ラベルに書き込むものはすべて自動的に対象になります — `TMP_Dropdown` の
  キャプション、ローカライズ系パッケージ、1文字ずつ流すコルーチンも。

既存のシーンと、自分で描画するテキストには:

```csharp
using UnityDirectTMP;

DirectTMP.ShapeAll();                          // 読み込み済みの全 TMP ラベル（ドロップダウン内も）
string visual = DirectTMP.Prepare("سلام دنیا"); // 1文字列を描画可能な形へ
```

意図的な例外がひとつ。`TMP_InputField` の**内部**のテキストコンポーネントは
対象外です。並べ替えるとキャレット位置がずれ、入力が語の反対側に入って
しまうためです。プレースホルダーは通常どおり処理されます。

### ✍️ 1.0.1 — その土台のシェーパーと双方向アルゴリズム

Unity はアラビア文字を接続せず、右から左へのテキストを並べ替えもしません。
TextMeshPro も同じで、`isRightToLeftText` は順序を反転するだけで、文字は
孤立形のままです。そのため `سلام دنیا` は、ばらばらの文字が逆向きに並んだ
状態で表示されます。これはフォントの問題ではないので、フォントを替えても
直りません。

1.0.1 はこれを、まず本パッケージ自身のウィンドウから直しました
(1.0 のペルシア語UIはまさにこの不具合を抱えていました)。

`Runtime/` の依存関係なしの3クラスが担当し、あなたのラベルにも同じ問題が
あるため public です:

```csharp
using UnityDirectTMP;

// 接続済み・表示順、そのまま描画できます。
label.text = DirectDisplayText.Prepare("سلام دنیا");

// 右から左の文字が無ければ、そのまま同じ参照が返ります。
DirectDisplayText.Prepare("Hello world");

// 個別に使うこともできます。
DirectArabicShaper.Shape("فونت");
DirectBidi.Reorder("אב TMP גד");            // "דג TMP בא"
```

対応範囲: アラビア文字ブロックとペルシア語・ウルドゥー語の追加文字
(`پ گ چ ژ ک ی`)、ラーム・アリフ合字4種、接続を壊してはいけない発音記号、
ZWNJ(`می‌شه` / `فونت‌ها`)、ペルシア語文中のラテン文字、2種類の数字、
鏡像化する括弧、そして段落の折り返し(並べ替えの**前**に改行位置を決めないと
段落が下から上に読めてしまいます)。

未対応: フォント自身の OpenType `GSUB` テーブルからのシェーピング。現在は
Unicode の表示形(presentation forms)を使っています。

### 📋 必要環境

- Unity **2021.3 LTS** 以降(Unity 6系にも対応)
- **TextMeshPro 3.0以降**(上記Unityに同梱。Unity 6 では TMP を含む `com.unity.ugui` を依存として宣言しています)
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

インストール後に一度だけウェルカム画面が表示されます。**Unity DirectTMP ▸ Welcome** からいつでも再表示できます。

### 🚀 使い方

**30秒でできる導入**
1. すでに `TextMeshProUGUI` か `TextMeshPro` が付いているGameObjectを選択
2. **Add Component → Unity DirectTMP → Direct Font**
3. **Font File** の欄に `.ttf` / `.otf` をドラッグ

**メニュー**

```
Unity DirectTMP
├── Font Catalog…            (すべてのフォントと、その中身)
├── Editor Font
│   ├── Choose a Font…       (Unity 自身の UI を変更)
│   └── Revert to Unity Default
├── Convert
│   ├── Selected Objects
│   ├── Current Scene
│   └── Whole Project        (すべてのScene + すべてのPrefabを一括で)
├── Fallback Chain…          (一度並べれば、どこでも同じ順番で)
├── Font Cache
│   ├── Show Cache Folder
│   └── Clear Cache
├── Language                 (English / 日本語 / فارسی)
├── Settings…                (サンプリングサイズ、アトラスサイズ、レンダーモード)
├── Health Check…            (何が動いていて、何が動いていないか)
├── Welcome
└── About Unity DirectTMP
```

3つのウィンドウは **Window ▸ Unity DirectTMP** からも開けます。

> **メニューが出てこない場合** — ほとんどはメニューの問題ではなくコンパイルエラーです。エディタアセンブリのコンパイルが失敗すると、その中の `[MenuItem]` はすべて一度に消えます。まず Console を確認し、次に **Window ▸ Unity DirectTMP ▸ Health Check** を開いてください。この状況を説明するために書かれた画面です。

**コードから**

```csharp
using UnityDirectTMP;

directFont.SetFontFile("Fonts/NotoSansJP-Regular.ttf");
DirectTMP.SetGlobalFont(fontBytes);
DirectTMP.Preload(fontBytes, "こんにちは世界 سلام دنیا Hello");

// アトラスを作らずに、フォントの中身を調べる
DirectFontFileInfo info = DirectFontFile.Read(path);
Debug.Log($"{info.DisplayName}: {info.GlyphCount} グリフ");
```

### 🗺️ ロードマップ

- [x] 任意のTMPコンポーネントで `.ttf` / `.otf` を直接使用
- [x] `StreamingAssets` / `byte[]` / ダウンロードファイルからの実行時読み込み
- [x] 再利用できるScriptableObjectとしての順序付きフォールバック
- [x] OS にインストール済みのフォントを利用
- [x] ファイルから直接読んだ文字体系カバレッジ付きのフォントカタログ
- [x] Unity エディタ自身の UI のフォント変更
- [x] アラビア文字の接続形と双方向テキストの並べ替え(ペルシア語・アラビア語・ウルドゥー語・ヘブライ語)
- [ ] `.ttc` コレクション対応(フェイスインデックス選択)
- [ ] フォント自身の OpenType `GSUB` を使ったシェーピング
- [ ] カラーフォント・絵文字フォント(COLR / CBDT)
- [ ] バリアブルフォントの軸

### 🎨 ブランド

マスコットは **Inky**(インキー)。真鍮のペン先を挿したティールのインク壺から、`A`・`あ`・`س` の3つのグリフの泡が昇っています。このツールの主張を1枚の絵にしたものです。

パレットは **Inkwell**: ティール `#14808C`、深いインク `#0B5A63`、真鍮 `#F0A73E`、クリーム `#FBF6EC`、ブラッシュ `#E8927C`、ナイト `#1B1725`。

### 📜 ライセンス

MIT — 詳細は [LICENSE](LICENSE) をご覧ください。無料です。リポジトリへの ⭐ が唯一の対価です。

同じ棚にもう一つ: 🧋 [**Unity DocSnap**](https://amircollider.n95pluss.workers.dev/unity-docsnap) — Unity プロジェクト全体をオフラインの Web サイトにします。

<p align="right"><a href="#top">⬆ トップに戻る</a></p>

<p align="center">━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</p>

<a id="persian"></a>
<div dir="rtl" align="right">

## ⭐ فارسی

تا حالا شده یه فونت خوشگل پیدا کنی، بندازیش توی یونیتی، یه ساعت توی Font Asset Creator بشینی و رِنج کاراکترها رو انتخاب کنی — و بازم همین که یکی اسم خودش رو فارسی یا ژاپنی تایپ کرد، یه ردیف □□□ ببینی؟ **Unity DirectTMP** کل این مرحله رو حذف می‌کنه.

این یه کامپوننت کوچیکه که کنار `TextMeshProUGUI` یا `TextMeshPro` می‌شینه و اجازه می‌ده به‌جای یه فونت‌اسِت SDF از پیش پخته‌شده، **خودِ فایل فونت** — یه `.ttf` یا `.otf` ساده — رو بهش بدی. گلیف‌ها همون لحظه‌ای که برای اولین بار قراره کشیده بشن مستقیم از همون فایل ساخته می‌شن؛ یعنی هر کاراکتری که فونت واقعاً داره بدون معطلی در دسترسه.

و از نسخه‌ی **۱.۰** به بعد، همین کار رو برای خودِ یونیتی هم انجام می‌ده.

### 🖥️ تازه در ۱.۰ — روی خودِ ادیتور یونیتی هم اثر می‌ذاره

یه GameObject رو `敵スポーナー` اسم بذار یا یه پوشه رو `فونت‌ها` — یونیتی درست ذخیره‌ش می‌کنه و بعد تا ابد بهت `□□□□□` نشون می‌ده. اسست سالمه. رابط خودِ یونیتی با فونت خودِ یونیتی کشیده می‌شه، و هیچ پکیج TextMeshPro ای تا حالا بهش دست نزده بود.

از مسیر **Unity DirectTMP ▸ Editor Font ▸ Choose a Font…** می‌تونی نوار منو، Hierarchy، Inspector، پنجره‌ی Project و Console رو با فونتی که خودت انتخاب می‌کنی بکشی.

- هیچ‌چیزی روی نصب یونیتی نوشته نمی‌شه و هیچ اسست داخلی‌ای روی دیسک ویرایش نمی‌شه. تغییر فقط توی حافظه‌ی همون سشن زندگی می‌کنه، پس **بستن یونیتی همیشه همه‌چیز رو کامل برمی‌گردونه**.
- فونتی که نتونه حروف پایه‌ی لاتین، عدد و علائم رو بکشه **رد می‌شه، نه اینکه فقط هشدار بگیره** — وگرنه همون گزینه‌ی منویی که این کار رو برمی‌گردونه، خودش می‌شه یه ردیف مربع خالی.
- راست‌کلیک روی هر `.ttf` توی پنجره‌ی Project ← **Unity DirectTMP ▸ Use This Font For The Editor** نسخه‌ی تک‌کلیکیشه.

### 🗂️ تازه در ۱.۰ — کاتالوگ فونت

مسیر **Unity DirectTMP ▸ Font Catalog…** همه‌ی فونت‌های پروژه و همه‌ی فونت‌های نصب‌شده روی سیستم رو لیست می‌کنه و هر فایل رو مستقیم می‌خونه تا حقیقتش رو بگه:

- **اسم خانواده و استایلی که خودِ سازنده‌ی فونت توی فایل نوشته** — نه اسم فایل، که دقیقاً به همین دلیل `subset-2.ttf` هیچی بهت نمی‌گه
- **تعداد واقعی گلیف‌ها**، از جدول `maxp` خود فونت
- **اینکه چه خط‌هایی رو می‌تونه بنویسه**، از جدول `cmap` خود فونت: سبز برای پوشش کامل، کهربایی برای ناقص، خاکستری برای هیچ

همون نشان کهربایی نکته‌ی اصلیه. فونتی که عربی داره ولی `پ گ چ ژ` نداره نمی‌تونه فارسی بنویسه، و یه فلگ درست/غلط با عنوان «از عربی پشتیبانی می‌کند» با خیال راحت بهت می‌گه می‌تونه.

متن واقعی UI ات رو یه بار توی فیلد پیش‌نمایش تایپ کن، همه‌ی فونت‌ها هم‌زمان باهاش رندر می‌شن.

### ✨ بقیه‌ی ویژگی‌ها

- 🅰️ **فایل فونت رو بده، نه فونت‌اسِت رو** — یه `.ttf` یا `.otf` رو بکش روی کامپوننت، تموم.
- 🌏 **هر خطی که فونت پشتیبانی می‌کنه، همون لحظه** — فونتی که ۲۰٬۰۰۰ کانجی داره، واقعاً ۲۰٬۰۰۰ کانجی داره.
- 🚫 **خداحافظ Font Asset Creator** — نه پنجره‌ی رِنج کاراکتر، نه *Generate Font Atlas*.
- 🔗 **زنجیره‌ی فال‌بک با ترتیب دلخواه خودت** — برای هر کاراکتر، اولین فونتی که اون گلیف رو داشته باشه انتخاب می‌شه.
- 💾 **لود کردن فونت در زمان اجرا** — از `StreamingAssets`، `persistentDataPath`، یه `byte[]` دانلودشده، یا فونت‌های نصب‌شده‌ی خود کاربر.
- ♻️ **اطلسی که خودش حواسش به خودشه** — گلیف‌ها به‌مرور اضافه می‌شن و در صورت نیاز روی چند تکسچر رشد می‌کنن.
- ⚡ **کَش می‌شه، دوباره ساخته نمی‌شه** — یه صفحه با ۲۰۰ لیبل که همه یه فونت دارن، فقط یه اطلس می‌سازه.
- 🩺 **یه بررسی سلامت که خودش رو توضیح می‌ده** — **Unity DirectTMP ▸ Health Check…** وضعیت رو گزارش می‌ده، هرچی رو بشه با یه کلیک درست می‌کنه، و کل گزارش رو برای ثبت باگ کپی می‌کنه.
- ✍️ **متن فارسی که واقعاً خونده می‌شه** — حروف به هم می‌چسبن، کلمه‌ها راست‌به‌چپ مرتب می‌شن و خط‌های شکسته‌شده هم درست می‌شن؛ نه فقط توی پنجره‌های خود پکیج، بلکه توی لیبل‌های خودت.
- 🌐 **رابط سه‌زبانه** — English / 日本語 / فارسی، از **Unity DirectTMP ▸ Language**.

### ✍️ تازه در ۱.۱.۰ — لیبل‌هایی که خونده می‌شن

فایل فونتی که فارسی داره، جلوی مربع‌ها رو می‌گیره. ولی باعث نمی‌شه فارسی
**خونده بشه**.

TextMeshPro هر کاراکتر رو به همون ترتیبی که ذخیره شده به فونت می‌ده، پس
لیبلی که تمام گلیف‌های لازم رو داره، باز هم یه ردیف حرفِ جدا و برعکس نشون
می‌ده. `isRightToLeftText` فقط ترتیب رو برعکس می‌کنه و حروف رو نمی‌چسبونه.
این مشکلِ فونت نیست، پس با عوض کردن فونت درست نمی‌شه — و اگه توی گزارش‌ها
همیشه اسم `ی` و `ر` میاد، فقط به این خاطره که شکل جدای این دوتا از همه بیشتر
با شکل چسبیده‌شون فرق داره.

کنار هر `TextMeshProUGUI` / `TextMeshPro` یه **Direct Text** بذار — یا گزینه‌ی
**Shape Text** توی Direct Font رو روشن بذار تا خودش اضافه‌ش کنه:

```csharp
label.text = "سلام دنیا";   // دقیقاً همین می‌مونه، به ترتیب منطقی
```

چی گیرت میاد:

- **حروف می‌چسبن** — شکل‌های آغازی، میانی، پایانی و جدا، چهار ترکیب لام‌الف،
  اعرابی که نباید اتصال رو بشکنه، و نیم‌فاصله برای `می‌شه` و `فونت‌ها`.
- **کلمه‌ها راست‌به‌چپ می‌شن** — کلمه‌های لاتین و عددها همچنان چپ‌به‌راست خونده
  می‌شن، نقطه و ویرگول سمت درست می‌شینن و پرانتزها آینه می‌شن.
- **پاراگرافِ شکسته‌شده از بالا به پایین خونده می‌شه.** دلیل اینکه این یه
  کامپوننته و نه یه تابع، همینه: متنِ مرتب‌شده رو نمی‌شه شکست، چون بعد از
  مرتب‌سازی **آخرین** کلمه‌ی پاراگراف می‌ره سمت چپِ خط. برای همین لیبل دو بار
  چیده می‌شه — اول به ترتیب خوندن، تا TextMeshPro بگه خط‌ها کجا شکستن، بعد
  دوباره با هر خط که جدا مرتب شده.
- **تگ‌های ریچ‌تکست سالم می‌مونن.** `<b>` و `<color>` همراه کلمه‌ای که دورشون
  گرفته جابه‌جا می‌شن و اگه اون تیکه برعکس بشه، تگ باز و بسته هم جاشون عوض
  می‌شه. حروف از دو طرف تگ هم به هم می‌چسبن.
- **فونت‌های بدون شکل‌های نمایشی دیگه مربع نمی‌دن.** وزیرمتن، Noto Sans Arabic
  و بیشتر فونت‌های امروزی فارسی فقط حروف ساده و قواعد OpenType رو دارن و توی
  U+FE70 هیچی ندارن. حالا هر شکل اول از فونت پرسیده می‌شه و اگه نبود، به خود
  حرف برمی‌گرده.
- **`label.text` هنوز همون رشته‌ی خودته.** کار توی `textPreprocessor` خود
  TextMeshPro انجام می‌شه، پس هرچی ست کردی همون رو پس می‌گیری — و هر چیزی که
  توی لیبل می‌نویسه بدون اینکه از این کامپوننت خبر داشته باشه پوشش داده می‌شه:
  کپشن یه `TMP_Dropdown`، پکیج‌های لوکالایز، یا کوروتینی که حرف‌به‌حرف تایپ
  می‌کنه.

برای صحنه‌ای که از قبل ساختی، و برای متنی که خودت رسم می‌کنی:

```csharp
using UnityDirectTMP;

DirectTMP.ShapeAll();                          // همه‌ی لیبل‌های TMP لودشده، شامل داخل دراپ‌دان‌ها
string visual = DirectTMP.Prepare("سلام دنیا"); // یه رشته، آماده‌ی رسم
```

یه استثنا هست، عمدی: کامپوننت متنِ **داخلِ** `TMP_InputField` دست‌نخورده
می‌مونه. اگه مرتبش کنیم، مکان‌نما جای اشتباه می‌ره و تایپ از سمت اشتباه کلمه
انجام می‌شه. پلیس‌هولدرش مثل بقیه اصلاح می‌شه.

### ✍️ ۱.۰.۱ — شِیپر و الگوریتم دوجهته‌ای که زیرش کار می‌کنه

یونیتی حروف خط عربی رو به هم نمی‌چسبونه و متن راست‌به‌چپ رو هم مرتب نمی‌کنه.
TextMeshPro هم همین‌طور: `isRightToLeftText` فقط ترتیب رو برعکس می‌کنه و حروف
توی شکل جدا باقی می‌مونن. برای همین یه لیبل با متن `سلام دنیا` به شکل یه ردیف
حرفِ جدا و برعکس نشون داده می‌شه — و این مشکلِ فونت نیست، پس با عوض کردن فونت
درست نمی‌شه.

نسخه‌ی ۱.۰.۱ این رو درست می‌کنه، و اول از همه توی پنجره‌های خودِ این پکیج:
رابط فارسی نسخه‌ی ۱.۰ دقیقاً همین ایراد رو داشت، که برای ابزاری که کارش
نمایش درست متنه اصلاً پذیرفتنی نیست.

سه کلاس کوچیک و بدون وابستگی توی `Runtime/` این کار رو می‌کنن، و public هستن
چون لیبل‌های خودت هم همین مشکل رو دارن:

```csharp
using UnityDirectTMP;

// چسبیده و به ترتیب نمایش، آماده‌ی رسم.
label.text = DirectDisplayText.Prepare("سلام دنیا");

// اگه چیزی راست‌به‌چپ توش نباشه، همون رشته برمی‌گرده.
DirectDisplayText.Prepare("Hello world");

// هر کدوم رو جدا هم می‌شه استفاده کرد.
DirectArabicShaper.Shape("فونت");
DirectBidi.Reorder("אב TMP גד");            // "דג TMP בא"
```

چی رو پوشش می‌ده: بلوک عربی به‌علاوه‌ی حروفی که فارسی و اردو اضافه می‌کنن
(`پ گ چ ژ ک ی`)، چهار ترکیب لام‌الف، اعرابی که نباید اتصال رو بشکنه،
نیم‌فاصله (`می‌شه`، `فونت‌ها`)، کلمه‌های لاتین وسط جمله‌ی فارسی، هر دو نوع
رقم، پرانتزهای آینه‌ای، و شکستن خط پاراگراف — که باید **قبل** از مرتب‌سازی
انجام بشه وگرنه پاراگراف از پایین به بالا خونده می‌شه.

چی رو هنوز پوشش نمی‌ده: شکل‌دهی از جدول `GSUB` خود فونت. فعلاً از شکل‌های
نمایشی یونیکد استفاده می‌شه که هر فونتِ دارای خط عربی اون‌ها رو داره.

### 📋 پیش‌نیازها

- یونیتی **2021.3 LTS** به بعد (یونیتی 6 هم پشتیبانی می‌شه)
- **TextMeshPro 3.0** به بعد — همراه همین نسخه‌های یونیتی میاد؛ پکیج وابستگی `com.unity.ugui` رو اعلام می‌کنه که توی یونیتی ۶ حامل TMP هست
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

بعد از نصب یه بار صفحه‌ی خوش‌آمد باز می‌شه. هر وقت خواستی از **Unity DirectTMP ▸ Welcome** دوباره بازش کن.

### 🚀 نحوه‌ی استفاده

**نسخه‌ی سی‌ثانیه‌ای**
۱. هر GameObject ای که از قبل `TextMeshProUGUI` یا `TextMeshPro` داره رو انتخاب کن
۲. **Add Component → Unity DirectTMP → Direct Font**
۳. فایل `.ttf` / `.otf` خودت رو بکش توی فیلد **Font File**

**منو**

```
Unity DirectTMP
├── Font Catalog…            (همه‌ی فونت‌ها و اینکه واقعاً چی توشونه)
├── Editor Font
│   ├── Choose a Font…       (رابط خود یونیتی رو عوض کن)
│   └── Revert to Unity Default
├── Convert
│   ├── Selected Objects
│   ├── Current Scene
│   └── Whole Project        (همه‌ی سین‌ها + همه‌ی پریفب‌ها، یکجا)
├── Fallback Chain…          (یه بار بچینش، همه‌جا همون ترتیب)
├── Font Cache
│   ├── Show Cache Folder
│   └── Clear Cache
├── Language                 (English / 日本語 / فارسی)
├── Settings…                (سایز نمونه‌برداری، سایز اطلس، حالت رندر)
├── Health Check…            (چی کار می‌کنه، چی کار نمی‌کنه، و چرا)
├── Welcome
└── About Unity DirectTMP
```

هر سه پنجره از **Window ▸ Unity DirectTMP** هم در دسترسن.

> **منو نیست؟** تقریباً همیشه مشکل از خطای کامپایله، نه از منو — وقتی یه اسمبلی ادیتور کامپایل نشه، یونیتی همه‌ی `[MenuItem]` های داخلش رو یکجا حذف می‌کنه. اول Console رو نگاه کن، بعد **Window ▸ Unity DirectTMP ▸ Health Check** رو باز کن که دقیقاً برای توضیح همین حالت نوشته شده.

**از توی کد**

```csharp
using UnityDirectTMP;

directFont.SetFontFile("Fonts/Vazirmatn-Regular.ttf");
DirectTMP.SetGlobalFont(fontBytes);
DirectTMP.Preload(fontBytes, "こんにちは世界 سلام دنیا Hello");

// بدون ساختن اطلس، از فونت بپرس چی داره
DirectFontFileInfo info = DirectFontFile.Read(path);
Debug.Log(info.HasCodepoint('گ') ? "فارسی رو داره" : "فارسی رو نداره");
```

### 🗺️ نقشه‌ی راه

- [x] استفاده‌ی مستقیم از `.ttf` / `.otf` روی هر کامپوننت TMP
- [x] لود در زمان اجرا از `StreamingAssets`، از `byte[]` یا از فایل دانلودشده
- [x] زنجیره‌ی فال‌بک ترتیب‌دار به‌شکل یه ScriptableObject
- [x] استفاده از فونت‌های نصب‌شده روی سیستم کاربر
- [x] کاتالوگ فونت با پوشش واقعی خط‌ها، خونده‌شده از خود فایل
- [x] عوض کردن فونت رابط خودِ ادیتور یونیتی
- [x] چسبیدن حروف خط عربی و مرتب‌سازی راست‌به‌چپ (فارسی، عربی، اردو، عبری)
- [ ] پشتیبانی از `.ttc` با انتخاب ایندکس فیس
- [ ] شکل‌دهی از جدول `GSUB` خود فونت، برای شکل‌های جایگزین وابسته به بافت
- [ ] فونت‌های رنگی و ایموجی (COLR / CBDT)
- [ ] محورهای فونت متغیر

### 🎨 برند

نماد ابزار **Inky** ـه — یه دوات ملات‌تیل با یه نوکِ برنجی و سه حبابِ گلیف که ازش بالا میان: `A`، `あ`، `س`. کل حرفِ این ابزار توی یه تصویر.

پالت **Inkwell** ـه: جوهر ملات‌تیل `#14808C`، جوهر تیره `#0B5A63`، برنجی `#F0A73E`، کاغذ کرم `#FBF6EC`، صورتی گرم `#E8927C`، شبانه `#1B1725`. همین شش رنگ توی پنجره‌های ادیتور، صفحه‌ی محصول و نشان‌های بالای همین صفحه استفاده می‌شن.

### 📜 لایسنس

MIT — جزئیات توی فایل [LICENSE](LICENSE). رایگانه؛ تنها هزینه‌ش یه ⭐ روی ریپازیتوریه.

روی همین قفسه یکی دیگه هم هست: 🧋 [**Unity DocSnap**](https://amircollider.n95pluss.workers.dev/unity-docsnap) — کل پروژه‌ی یونیتی رو می‌کنه یه وب‌سایت آفلاین.

<p align="right"><a href="#top">⬆ برگشت به بالا</a></p>

</div>

<p align="center">━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</p>

<p align="center"><sub>Made with 🖋️ for Unity — <a href="https://github.com/AmirCollider">AmirCollider</a></sub></p>
