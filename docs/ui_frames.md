# UI の枠の寸法

枠の絵を外部で作るための一覧。数値は `docs/ui_frames_list.py` がコードの定数から
計算したもの。**コード側の定数を変えたら、この表も作り直す。**

```bash
py -3 docs/ui_frames_list.py
```

---

## 0. 先に読んでほしいこと

### 出力する解像度は 3 倍で

ゲームは **960 x 540 の仮想の舞台**を基準に組んであり、端末の画面に合わせて拡大される。
iPhone 横持ち（2556 x 1179）では約 **2.18 倍**になる。
下の表の px はこの仮想の舞台での寸法なので、**表の 3 倍の大きさで書き出す**と
拡大しても粗が出ない（例: 944 x 252 の枠なら 2832 x 756 で作る）。

### 1 枚ずつ作るより、9 分割にしたほうが安い

枠は 30 種類以上あり、寸法もこの先の調整で動く。
1 サイズ 1 枚で作ると、寸法を変えるたびに描き直しになる。

**9 分割（ナインスライス）**なら、四隅と辺だけを持った 1 枚でどの大きさにも伸ばせる。
必要なのは次の 4 枚だけで済む。

| 用意する枚数 | 使い回す先 | 作る大きさの目安 | 角の余白 |
|---|---|---|---|
| 板（大） | ステージ札・各モーダル・ショップ・装備 | 384 x 384 | 48 px |
| 板（小） | 表示器・操作パネル・リール筐体・窓の中の小枠 | 288 x 288 | 36 px |
| ボタン | MAX BET / STOP / AUTO / 各ボタン | 240 x 168 | 30 px |
| くぼみ | リール窓・数値の窓・スランプ | 192 x 192 | 24 px |

「角の余白」は、その内側なら引き伸ばしてよい範囲の境目。
絵の四隅の飾りは必ずこの余白の内側に収める。**はみ出すと伸ばしたときに崩れる。**

### やらない選択肢

いまの枠は画像を使わず、`UiSkin.Card` が手続きで描いている。
画像に置き換えると、色を後から変えるのが難しくなり（灯りの強さ・板の色は
いま定数 1 つで動く）、容量も増える。
**枠の見た目を大きく変えたいときだけ**画像にする価値がある。
色味や角の丸みを詰めたいだけなら、いまのまま数値を変えるほうが早い。

---

## 1. 寸法の一覧

| 枠 | 幅 x 高さ (px) | 角丸 | 出どころ |
|---|---|---|---|
| **■ ゲーム画面（舞台 960×540）** | | | |
| ステージ札（上段の大枠） | 944 x 252 | 14 | UiSkin.Card / GameController StageCard |
| 表示器（左・EMBER/PAYOUT） | 242 x 196 | 12 | Card / Display |
| リール筐体 | 444 x 196 | 12 | Card 相当 / cabinet |
| 操作パネル（右・PLAYER） | 242 x 196 | 12 | Card / Side |
| MAX BET ボタン | 242 x 56 | 10 | UiSkin.Button |
| STOP ボタン（3 つ） | 132 x 56 | 10 | UiSkin.Button |
| AUTO ボタン | 242 x 56 | 10 | UiSkin.Button |
| | | | |
| **■ くぼみ（Inset。枠ではなく彫り込み）** | | | |
| リール窓（3 つ） | 140 x 188 | 8 | Inset / Window |
| EMBER の窓 | 218 x 40 | 8 | Inset / CreditInset |
| PAYOUT の窓 | 218 x 40 | 8 | Inset / PayoutInset |
| 常駐スランプ | 218 x 22 | 5 | Inset / MiniSlump |
| DEBUG の情報欄 | 280 x 268 | 8 | Inset / Info |
| | | | |
| **■ 札（小さな見出し）** | | | |
| ステージ札 | 330 x 30 | 15 | 角丸は Shadow(15,10) に合わせている |
| ライフ札 | 232 x 34 | 17 |  |
| ルート札 | 134 x 118 | 15 |  |
| ナビのバッジ（3 つ） | 66 x 66 | 33 | 円形 |
| | | | |
| **■ 窓（モーダル）** | | | |
| サウンド / 設定 | 400 x 272 | 14 | BuildModal |
| DEBUG | 660 x 420 | 14 | BuildModal |
| スランプグラフ | 720 x 500 | 14 | BuildModal |
| 冒険マップ | 760 x 460 | 14 | BuildModal |
| 装備 | 800 x 470 | 16 | EquipScreen |
| ショップ | 780 x 470 | 16 | ShopScreen |
| 呪いと祝福 | 520 x 260 | 16 | GameController CurseCard |
| | | | |
| **■ 窓の中の小さな枠** | | | |
| 装備: 着ている部位（3 つ） | 250 x 68 | 10 | EquipScreen Worn |
| 装備: 詳細 | 764 x 92 | 10 | EquipScreen Detail |
| ショップ: ステータスの列（3 つ） | 232 x 250 | 12 | ShopScreen Stat |
| ショップ: 補給のカード（2 つ） | 340 x 170 | 12 | ShopScreen Torch/Coin |
| ショップ: 品物の行 | 360 x 84 | 12 | ShopScreen Item |
| 呪い / 祝福の札（2 つ） | 236 x 108 | 10 | GameController |
| | | | |
| **■ ミニマップ** | | | |
| 財布 | 220 x 52 | 12 | MapScreen Purse |
| 行き先カード（街 / 冒険） | 250 x 170 | 16 | MapScreen MapNode |
| ステージマップの板 | 640 x 330 | 14 | MapScreen MapPanel |
---

## 2. 画像に置き換えるときの手順（2026-09-11 に実施）

枠は `assets/title/ui_frames_sheet.png`、アイコンは `assets/title/icon_parts_sheet.png` から
`tools/ui/cut_sheets.py` が切り出す。`.meta` の `spriteBorder` は使わず、
`UiSkin.FrameBorders`（コード）に縁の幅を持ち、実行時に `Sprite.Create` で 9 分割にしている。
Unity 側の取り込み設定に依存しないので、別 PC で pull しただけで同じ見え方になる。

```bash
python tools/ui/cut_sheets.py --install      # 切り出して Resources/Art/UI/Frames, Icons へ
```

- 縁の幅を変えるときは `cut_sheets.py` の `FRAMES` と `UiSkin.FrameBorders` の**両方**を直す
- 1 枚ずつアップスケールするときは、`assets/title/parts/frames|icons/` の画像を**同じ名前で**置き替えて
  `python tools/ui/cut_sheets.py --no-cut` で入れる（`--install` はシートから切り直すので上書きされる）。
  縁は元の幅との比で自動的に広がるので、`FrameBorders` は触らなくてよい。倍率は枠ごとに違ってもよい
- シートごとアップスケールしたときは `--scale 3 --frames <新しいシート>` のように倍率を渡す
- 画像が無ければ各ビルダー（Card / Inset / Button / IconButton / Gauge / Icon）は
  手続き描画に戻る。差し替えの途中で壊れない
- 幅 110 か高さ 40 を切る小さなボタンは飾りの多い枠が潰れるので、`pill_navy_sm` に落とす
- ボタンの色は「役割 → 枠の絵」に読み替える（青=既定 / 桃=決定・危険 / 緑=進行中 / 茶金=金 / 灰=無効）
- `py -3 docs/preview_skin.py out.png` は手続き描画のままなので、画像の見え方は実機で確かめる

---

## 3. 関連

- **触って直すなら `docs/ui_editor.html`**（ダブルクリックで開く）。
  枠を掴んで動かし、重なりをその場で見て、結果を C# の定数として書き出せる
- 配置の検算: `docs/check_layout.py`（重なりとはみ出しを見る）
- 枠の見え方の確認: `docs/preview_skin.py` / タイトルは `docs/preview_title.py`
- UI の作法: `docs/ui_rules.md`

---

## 4. リールの図柄

| | 値 |
|---|---|
| 表示される枠 | **124 x 56 px（比 2.21 : 1）** |
| 新しく作るときの推奨 | **372 x 168 px**（枠の 3 倍） |
| 形式 | PNG・背景は透過 |
| 置き場所 | `Assets/Resources/Art/Symbols/` |

枠は `ReelView.ReelWidth - 8` x `ReelView.SymbolHeight - 4` から来ている。
`preserveAspect` で収めるので、**比が違っても歪まないが、余った側に隙間ができる**。

いまの図柄（160 x 73 / 比 2.19）に合わせて、リールの幅を 148 → 132 にしてある。
枠が 2.21 になり、隙間は左右あわせて 1.3px まで詰まった。
図柄を作り直すなら **比 2.21（372 x 168）** で作ると枠いっぱいに入る。

取り込みの設定は `Assets/Editor/BbbTexturePostprocessor.cs` が自動で当てるので、
Unity 側で触る必要はない（Sprite / 圧縮なし / ミップマップ無し / 最大 8192）。
