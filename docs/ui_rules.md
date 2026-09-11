# BBB の UI でやってはいけないこと

実際に事故が起きた順に並べた。新しく画面を組むとき、レビューするときはこれを上から順に当てる。
根拠のある一般論ではなく、**このプロジェクトで実際に壊れたもの**だけを書く。

---

## 1. 要素を「見た目の勘」で置かない

### やってはいけない

隣り合う要素の位置を、それぞれ独立した数値で書く。

```csharp
// NG: 両方とも右上に置いたつもりだが、重なっているかどうか式から読み取れない
var soul  = UiSkin.Number(card, "Soul",  new Vector2(W * 0.5f - 120, y), new Vector2(200, 28), ...);
UiSkin.IconButton(card, "Close", new Vector2(W * 0.5f - 26, y), 30, "×", ...);
// soul  の右端 = 370
// close の左端 = 349   → 21px 重なる
```

### やること

**端から順に領域を取り、隣の位置を前の要素から計算する。** そうすれば重なりが式の上で起きない。

```csharp
const float pad = 12f;                                  // 要素どうしの最小の間
float closeCx  = W * 0.5f - 14f - closeD * 0.5f;        // 右端から確保
float soulCx   = closeCx - closeD * 0.5f - pad - soulW * 0.5f;
float tabCx    = soulCx - soulW * 0.5f - pad - tabW * 0.5f;
```

### アイコンも「要素」

**あとからアイコンを足すときが一番危ない。** 見出しは左寄せなので、同じ左端にアイコンを置けば必ず重なる。

```csharp
// NG: 見出しは左寄せ。同じ左端に置けば文字に重なる
UiSkin.Heading(disp, "CreditLabel", new Vector2(0, 83), innerW, "EMBER");
UiSkin.Img(disp, "EmberIcon", new Vector2(-innerW * 0.5f + 16, 83), ...);

// OK: アイコンの領域を先に取り、文字はその分だけ字下げする
const float HeadIco = 15f, HeadGap = 5f, HeadIndent = HeadIco + HeadGap;
UiSkin.Img(disp, "EmberIcon", new Vector2(-innerW * 0.5f + HeadIco * 0.5f, 83), new Vector2(HeadIco, HeadIco), ...);
UiSkin.Heading(disp, "CreditLabel", new Vector2(0, 83), innerW, "EMBER", HeadIndent);
```

左寄せの文字の左、右寄せの文字の右にアイコンを置くときは、**必ず文字側の幅を削る**。
アイコンを足しても文字の領域は変わらない、という思い込みが事故の元。

同じ行に左寄せと右寄せを同居させるときも、行を半分に割ってそれぞれに領域を与える。
「短い文字列なら重ならないだろう」は通用しない。桁が増えた瞬間に重なる。

### 確認のしかた

配置を書いたら、その場で左端・右端を出して並べる。目視で確認しない。
`docs/check_layout.py` に主要な行の検算を置いてある。**UI を触ったら必ず走らせる。**

```bash
py -3 docs/check_layout.py
```

```python
items = [('題名', titleCx, titleW), ('ソウル', soulCx, soulW), ('閉じる', closeCx, closeD)]
items.sort(key=lambda t: t[1])
prev = None
for name, cx, w in items:
    l, r = cx - w/2, cx + w/2
    if prev is not None and l < prev: print(name, '重なり!')
    prev = r
```

同時に出ない要素（AT チップとエンゲージチップなど）だけは例外にしてよい。
ただし「同時に出ない」ことがコードで保証されている場合に限る。

---

## 2. 背景の上に半透明の札を置かない

### やってはいけない

```csharp
// NG: 82% 不透明でも、後ろの空や木立が透けて文字が読めない
UiSkin.Img(tagRt, "Bg", ..., new Color(0.05f, 0.07f, 0.12f, 0.82f));
```

背景が動く（パララックス）ため、透過があると読める瞬間と読めない瞬間ができる。

### やること

- 常時表示の札は **完全不透明**（`Hex("#0b1120")` など）
- 縁を 1px 足して背景から分離する
- 文字は太字＋影（`TextShadow(t, 1f)`）

演出用の一時的なオーバーレイだけ半透明にしてよい。

---

## 3. 背景やマスクの内側に情報を置かない

### やってはいけない

```csharp
// NG: _area には Mask と動く背景がある。その子にすると背景に埋もれる
var tagRt = UiSkin.Rect(_area, "StageTag", ...);
```

### やること

常時読ませたい情報（残りG数、資源、モード）は、**背景を持つコンテナの外側**に置く。
BBB では `_area`（表示域）ではなく `stageCard`（その親）の直下に置く。

置いたあと、意図した重なり順になっているか明示する。

```csharp
_stageTagBg.transform.parent.SetAsLastSibling();
```

---

## 4. 可変長の文字列を固定幅に流し込まない

### やってはいけない

```csharp
// NG: 「松明 3 本   残り 40 G」は 192px に入らず右が切れる
_torchTag.text = $"{res.name}  {have} 本   残り {_m.Adv.torchSpins} G";
```

### やること

- 最大長で見積もる。ソウルやエンバーは 6 桁（`999,999`）まで想定する
- 入らなければ **文言を削る**。フォントを小さくして詰め込まない
- 数値と単位の間の全角スペースをやめる（`3 本` → `3本`）

---

## 5. 画面更新の途中で落ちるコードを書かない

### やってはいけない

```csharp
// NG: Edge は Bg の子ではなく親（tagRt）の子。Find は null を返し、ここで例外
_stageTagBg.transform.Find("Edge").GetComponent<Image>().color = sc;
```

`RefreshUi()` はボタンの有効・無効も更新する。途中で例外が出ると **その後ろが全部止まる**。
実際にこれで「ボーナス後に MAX BET が押せない」「Space 長押しで AUTO にならない」が同時に起きた。

### やること

- 参照は **生成時にフィールドへ持つ**。`Find` で毎回探さない
- `RefreshUi` の中で `Find` / `GetComponentInChildren` を使わない
- 触る前に null を確認する

---

## 6. 静的キャッシュに破棄済みの Unity オブジェクトを残さない

### やってはいけない

```csharp
// NG: Play を止めて再生すると、キャッシュの中身は破棄済みで見えなくなる
if (_cache.TryGetValue(path, out var s)) return s;
```

### やること

```csharp
if (_cache.TryGetValue(path, out var s) && s != null) return s;   // 破棄済みなら作り直す

[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetCache() { _cache.Clear(); }
```

---

## 7. 画面遷移のたびに音を作り直さない

### やってはいけない

各画面の `Start()` で `AudioManager.Create()`、遷移時に `Destroy(_audio.gameObject)`。
破棄と再生成が重なると BGM が鳴らないことがある。

### やること

画面をまたいで 1 つだけ持つ。既にあれば使い回す。

```csharp
var exist = FindFirstObjectByType<AudioManager>();
if (exist != null) return exist;
```

---

## 8. 押した操作に反応を返さないままにしない

ナビのバッジを押しても表示が変わらないと、次のゲームのナビと混ざって「どれが今の指示か」が分からなくなる。

### やること

- 押したら **その場で消す**（1.55 倍に膨らませながらフェード、0.22 秒）
- 次のゲームのレバーで必ず初期化する（前のゲームの結果表示を持ち越さない）

---

## 9. 近似の道具で「通った」と言わない

街の画面（2026-09-11）。Claude は HTML のビューアとコンパイルで確かめて「反映した」と報告したが、
実機では枠の縁が 100 倍に計算されて四隅だけになっていた（`Claude-ExternalBrain` O-018）。
同じ素材で GPT-6 が組み直した版は、分離した batchmode で実際の uGUI を PNG に描き、それを見てから納めた。

### やること

- 画面を組んだら `Assets/Editor/MapUiValidation.cs` の型で **実際に描いて PNG を出し、自分の目で見る**。
  1280×720 と 2556×1179 の 2 つ。出力は `tools/<画面>-qa/`、結果は `validation.json`
- タッチ寸法（44 未満）と重なりは例外で落とす。落ちたら直すまで報告しない
- HTML のビューア・`check_layout.py` は当たりを付ける道具。完了の根拠にしない

## 10. 枠を全部に付けない。数値は札の型に揃える

同じ街の画面で、Claude 版は残高・カード・見出しの帯まで金縁で、本人に「配置見た目ヤバい」と言われた。
通った版の型:

- 枠は **押せる場所と主役の板だけ**（カード 2 + 地図 1 + ボタン 3）。見出し・残高・下の数値は枠なし
- ボタンは 主 = 桃 / 通常 = 青 / 戻る = 灰。**画面の主は 1 つ**。文言は動詞（「冒険へ進む」「街で準備する」）
- 数値は **アイコン + 小見出し（12〜15、#b9cce5）+ 太い値（18〜26、白か金 #f7d991）** の 2 段。文章に混ぜない
- アイコン左 + 文字左揃え。左上に「画面の目的」の見出しと 1 行の副題
- 余白 24・間隔 14〜20 を定数にし、位置はそこから派生させる
- 詳しい差分表は `game-design` スキルの §8.2

---

## チェックリスト

新しい画面・要素を足したら、これを順に確認する。

- [ ] 隣り合う要素の左端・右端を計算し、重なりがないことを数字で確認したか
- [ ] 常時表示の札は完全不透明で、縁と影があるか
- [ ] 背景やマスクの内側に、読ませたい情報を置いていないか
- [ ] 最長の文字列（6 桁の数値を含む）で幅が足りるか
- [ ] `RefreshUi` の中に `Find` や例外の出る参照がないか
- [ ] 静的キャッシュに破棄チェックがあるか
- [ ] 押した操作に見た目の反応があるか
- [ ] 1280×720 と、スマホ横持ち（19.5:9）の両方で切れないか
- [ ] 実際の uGUI を描いた PNG を見たか（HTML の近似とコンパイルだけで済ませていないか）
- [ ] 枠は押せる場所と主役の板だけか。主ボタンは 1 つか。数値は札の型か

---

## 参考

配置と間隔の一般的な規範は `app-ui-design` スキルの `references/game-design.md`（§3 セーフエリア、§8 間隔、§9 可読性、§16 パチスロ実機）を見る。
この文書はそれを置き換えるものではなく、**BBB で実際に踏んだ穴**の記録。
