# 冒険改良 P1 — 顔付き会話・GET・呪いHUD

実装開始: 2026-09-23 / 描画検証: 2026-09-26 / リセット確認・記録整理: 2026-09-27（Asia/Tokyo）。
依頼の正本: [冒険改良企画](ADVENTURE_REDESIGN_PLAN_2026-09-23.md)。本書はそのうち情報表示の実装記録。敵20体、主人公の新スプライト、武器外見、戦闘の波、HD2D相当の描画はこの実装に含めない。別作業の背景・エフェクト見本はPROJECT_STATEの新しい記録を参照する。

## 最初に読む現在地

- OPと冒険を共通の紺・金の会話窓へ接続。話者の顔、日本語の文字送り、小さな文字送りSE、全文表示、長文ページ分割を実装した。
- GETをキャラクターのMaskの外、会話より後に描く専用階層へ移した。1件は会話の上、複数は右側の縦列。同じフレームの獲得をまとめ、前の組が抜けてから次の組を出す。
- 取得中の呪いを4種の絵で表示。同種の取得数と、現在の合算効果、取得時に対になった祝福を説明する。ホバー・フォーカス・クリック固定に対応。
- 2026-09-24の別作業で入った `dialogueScale: 0.7` を維持。枠、顔、文字を一緒に縮め、下端を揃える。字幕サイズ設定はその上に適用する。
- Coreの抽選や報酬付与の処理はこのP1で変更していない。会話の音にゲームの乱数を使わない。

## コードと変更する場所

以下は `UnityProject/BigBonusBlitz/Assets/` からの相対パス。

| ファイル | 責務・次の編集時の注意 |
|---|---|
| `Scripts/BBB.Runtime/DialoguePresenter.cs` | OP/冒険共通の顔・名前・本文・入力ヒント。Begin / Tick / Reveal / Advance。物語進行は持たせない |
| `Scripts/BBB.Runtime/AdventurePresentation.cs` | JSON設定と顔・呪いアトラスのSprite切出し、欠損時の代替絵 |
| `Resources/Data/adventure_presentation.json` | 速度、句読点待ち、読了待ち、音間隔・音量、ホバー待ち、全体縮尺、話者対応 |
| `Scripts/BBB.Runtime/OpeningPlayer.cs` | 既存OP場面の進行、暗転、スキップ、クリック/Space/Enter。OP本文や保存済み進行は変更しない |
| `Scripts/BBB.Runtime/GameController.cs` | ShowLine / ExplainRoutine、RewardOverlay、GainQueueRoutine / GainSlide / PlaceGain、HUD更新 |
| `Scripts/BBB.Runtime/AudioManager.Dialogue.cs` | 既存DialogueキューとSE音量を使う文字送り音。空きボイスがなければ鳴らさない |
| `Scripts/BBB.Runtime/CurseHudPresenter.cs` | Curse.Takenを元に表示。CurseHudFocusがポインター/選択イベントを受ける |
| `Scripts/BBB.Runtime/CurseScreen.cs` | 既存の効果説明を共用。BET追加消費の単位を「エンバー」へ明確化 |
| `Editor/AdventureDialogueTextureImporter.cs` | アトラスをTexture2D、Clamp、mipmapなし、NPOT維持で読む |
| `Editor/AdventurePresentationValidation.cs` | 実uGUIの2解像度画像と、ページ・階層・呪い情報の検査 |
| `Tests/EditMode/AdventurePresentationPlayTests.cs` | 隔離コピー内でPlay Modeへ入り、実コルーチンとButton/EventSystemを検査 |
| `tools/fx_viewer.html` / `tools/fx_sync.py` | 「冒険の会話・文字送り」の調整見本。同期後のdefaultsも更新する |

## 会話の作り方と完成判断

冒険窓は論理幅660、高さ112（字幕拡大時140）、OPは幅850、高さ156（拡大時194）。窓の下端を固定し、文字を装飾から離す。顔は金の円形フレーム、本文は日本語対応のNoto Serif JP。本文、顔、背景は別部品で、文字を画像に焼き込まない。

1. `Begin`で話者と本文を渡す。主人公の空名/「主人公」はサリアへ正規化する。`*`は地の文なので顔と名前を消す。「声」「？？」は匿名の紋章にする。
2. 本文全体のレイアウトを先に決め、未表示の末尾を透明な文字にする。文字が増えるたびに改行位置が変わる方式に戻さない。
3. `StringInfo`で文字要素の境界を扱う。結合文字やサロゲートペアの途中を切らない。実フォントのpreferredHeightでページを分け、字幕150%でも全ページを読めるようにする。
4. 通常は毎秒30文字、句読点で0.13秒待つ。音は最大約0.085秒間隔、1更新で1音まで。追いつき処理で何十音も同時に鳴らさない。
5. 最初のタップは全文表示、全文表示後は次ページ/次の行へ。冒険の自動進行は全文が出てから読了時間を取る。OPは手動送りを保持する。
6. 字幕OFFでは任意会話を省くが、OPと進行説明は消さない。高コントラストでは黒地・白文字。設定画面中は冒険会話の進みを待つ。

`StoryLine`に新しい顔IDを追加したわけではない。現行は話者名対応とOP場面IDで表情を選ぶ。各行の感情演技を増やすときは企画のデータ契約へ段階的に移行し、旧JSONを壊さない。

## GETが隠れない構造

```text
StageCard
  CharacterArea（Mask・背景・キャラ）
  AdventureDialogue
  RewardOverlay（blocksRaycasts=false）
  CurseHud
Stage
  CurseTooltip / 各モーダル
OpeningCanvas（sortingOrder 900）
```

上下位置だけでは、別階層のMaskや兄弟順による隠れは直らない。専用のRewardOverlayを会話の後へ置く。通知は表示だけを担当し、所持数を増やさない。説明をクリックするための入力も遮らない。

会話の表示・字幕サイズが途中で変わっても、`PlaceGain`が毎フレーム実Rectを参照する。複数獲得は右側の列で同時表示し、本文にかからないよう縮尺と行間を合わせる。数字とGETの横幅も枠に収める。入場中/退場中の意図した画面外移動と、停止位置の切れを混同しない。

モーダルやOP中はCanvasGroupで非表示にし、アニメーション時間も進めない。BET待ちの組はタイムアウトが0ならBETまで保持する。次の組を単に固定秒数後に開始すると、BET待ちの前の組と重なるため、子の消滅まで待つ。

既存 `game_config.json` の `reelFx.emberGain.aboveDialogue` を使用する。派手さのパラメータを別のファイルへ重複させない。初期検品画像の旧ピクセル主人公はP2の交換対象で、このP1の完成素材ではない。

## 呪いHUDの情報契約

| ID | アイコン | 表示の注意 |
|---|---|---|
| `c_drain` | ひび割れた灯 | 消耗の実効果を合算 |
| `c_hard` | 分岐と岩場 | 説明はCurseScreen.Textの既存単位を使う |
| `c_cut` | 欠けた硬貨 | 実効減少率は100%を上限とする |
| `c_toll` | 関所と硬貨 | 追加消費はエンバー。別資源「灯」と誤解させない |

`machine.Curse.Taken`が情報源。UIに独自の呪い履歴を保存しない。同種をまとめても取得ごとの祝福・深さを失わない。呪い0件では枠ごと非表示。将来4種を超えたときは「+N」から全件説明へ進む。

セーブリセットでSlotMachine自体を作り直す経路では、`BindMachine`で新しい状態へつなぎ直す。帰還で同じインスタンスの取得一覧が空になる場合と区別し、古いマシンへの参照を残さない。

ホバー/キーボードフォーカスで0.2秒待って表示、離れると閉じる。クリック/タップで固定し、外側で閉じる。説明が長い場合は固定してスクロールする。Esc/レバー側のCloseModalsでも閉じ、閉じた直後に古いhoverIdで再表示されないよう消す。設定・装備などのモーダル中はHUDと説明を隠す。

## 生成素材と再制作の指示

使用ツール: 内蔵 `image_gen`。生成は2026-09-23〜24。以下の英語文は制作内容から整理した**再制作用プロンプト**で、送信時の全文ログの逐語転載ではない。PNG原本は `Assets/Resources/Art/UI/AdventureDialogue/` に保存済み。再生成結果は同一にはならないため、既存の原本を優先する。ファイルの寸法・SHA256は [素材台帳](art/adventure-dialogue-assets.json)。

共通: Azure系JRPGのアニメ調、紺・象牙・金、縮小時に顔/シルエットが読める絵。文字、枠名、数値、透かしを描かない。各セルの中央に同じ余白で置き、絵が隣のセルへ渡らない。

| PNG | グリッド（左上から行順） | セルの用途 |
|---|---|---|
| `salia.png` | 2×2 | 笑顔 / 決意 / 消耗 / 驚き |
| `town.png` | 2×2 | 宿の主人 / 灯守 / 店主 / 門番 |
| `travelers.png` | 2×3 | 行商人 / 巡礼者 / 狩人 / 占い師 / 王の使者 / 予備の羅針盤 |
| `curses.png` | 2×2 | c_drain / c_hard / c_cut / c_toll |

サリアは既存 `Art/UI/AzureEquip/salia-0.png` を参照し、髪色・目・白いリボン・あほげ・白金青の鎧を統一した。透明な生成背景はUI側の紺面で受ける。旅人は旧スプライトの顔切抜きも代替として残すが、通常は新しい人物画を使う。

```text
SALIA: Create a consistent 2 by 2 JRPG dialogue portrait atlas using the provided Salia reference. Same energetic young adult knight in every cell, pink hair, blue eyes, white hair ribbon and ahoge, white armor with gold trim and blue fabric. Shoulders-up portraits, centered with equal safe margins. Reading order: warm happy smile; determined resolve; exhausted after escaping; surprised and attentive. Preserve identity and costume. No text, no lettering, no UI labels.

TOWN: Create a 2 by 2 anime JRPG shoulders-up portrait atlas on deep navy backgrounds, equal cells and consistent scale. Reading order: kind middle-aged innkeeper, brown hair graying at the temples, mustache, burgundy vest; elderly lantern keeper, silver beard, indigo cloak; confident female shopkeeper, auburn braid, green and cream clothes; young male gate guard, dark hair, white and navy armor. Elegant painted finish, clear faces at small sizes, no text or labels.

TRAVELERS: Create a 2-column by 3-row JRPG portrait atlas with equal cells and deep navy backgrounds. Reading order: weathered male traveling merchant with a brown cap; female pilgrim in pale blue hood; male hunter in green; female fortune teller in violet; male royal messenger in gold and red; an anonymous golden compass emblem. Consistent shoulders-up framing, detailed anime painting, no text or labels.

CURSES: Create a 2 by 2 fantasy RPG icon atlas, ivory and antique gold with violet cracks against deep navy. Reading order: cracked lantern; forked path through harsh crags; chipped coin breaking into fragments; closed toll gate with a coin. Distinct bold silhouettes readable at 32 pixels, equal centered spacing, elegant painterly finish, no text or numbers.
```

原本の生成ファイル名（Codexのgenerated_images配下）: salia=`exec-917a35b6-ba92-457d-bd7e-ffeaf6186e48.png`、town=`exec-d481160b-e65e-4f97-be6f-5b1d263a7b47.png`、travelers=`exec-0d214c06-f906-4677-a7f8-11ec13862587.png`、curses=`exec-15864874-e4de-4e87-a452-59d56bfb92a6.png`。

## 検証と失敗から分かったこと

検証場所は別コピー `work/shop-qa`、productNameも`shop-qa`。本番のセーブとPlayerPrefsを使わない。実行方法と版別結果は [QA案内](../tools/adventure-redesign-qa/README.md)。

- 実uGUI: 1280×720 / 2556×1179。顔、地の文、説明、1/4件GET、字幕150%、呪い0/1/4/重複、モーダル、OPを検査する。
- 実行状態: 報酬の同時表示、遅れて開く会話、モーダルでの待機/復帰、BETでの終了、残高不変、ホバー/選択/固定、OPの1押目全文・2押目次行・スキップをPlay Modeで検査する。
- コンパイル: Core/Runtime/Tests。既存の配置検算も実行する。
- 未確認: 実機タッチ、OSからの実キー入力、聴感、配布ビルド、OP保存からの全分岐再開、長時間プレイ。描画検品の32枚は動画検証ではない。

| 失敗 | 分かったこと・修正 |
|---|---|
| 画像がResources.Load<Texture2D>でnull | 最小metaのtextureShapeがCubemapだった。ImporterでTexture2Dを明示し、既存metaも修正。Importerだけ直してキャッシュが残った状態を成功扱いしない |
| 呪いボタンのNullReference | UiSkin.ButtonはrootにImageを置かない。button.targetGraphicを参照する |
| EditMode画像の白い主人公 | 通常のAwakeが呼ばれない検品環境だった。検証側でSpriteAnimatorの初期化を行う。製品の素材不具合と分ける |
| QAコピーだけJudgeFxConfigが見つからない | 別作業でCoreも更新されていた。RuntimeだけでなくCoreとDataも同じ時点で同期する |
| 実動作テストでGETが1件/数字が途中 | EditModeランナーの固定待ちで、実フレームが十分進む前に判定していた。実際の組生成とTime.time/Time.frameCountを待つ。製品のグループ処理を推測で変えない |
| 固定した呪い説明を閉じても再表示し得る | HideTooltipでhoverIdも消し、CloseModalsへ接続した |

## 次のAIへ渡す作業

1. 最初にPROJECT_STATE、元企画、最新QA案内を読む。このP1を二重実装しない。現在のゲーム設定と共有作業の差分を読み直す。
2. P2の森1地点・3敵・2武器を着手単位にする。既存の背景更新や `tools/fx-lab/` の素材も棚卸しし、採用前の見本を本番済みと誤認しない。
3. 元気なサリアの基準姿、全フレームの足元・握り位置・キャンバスを決める。待機/歩行/斬撃/被弾/復帰を先に接続し、剣/槍を分離した表示で試す。
4. 実表示サイズの動画で、足滑り、剣の二重表示、割込み、装備確定から外見更新までを見る。合格するまで20敵と7武器を量産しない。
5. 成功条件、失敗、生成プロンプト、採用/不採用、日時、QA出力を更新する。本人の新しい指摘で変更した数値は旧案と区別する。
