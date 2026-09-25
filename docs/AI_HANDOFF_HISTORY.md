# BigBonusBlitz AI 引き継ぎ履歴

最終更新: 2026-09-23（Asia/Tokyo）

このファイルは、別のAIエージェントが会話を読めなくても、BigBonusBlitzで何が決まり、何を実装し、何を検証し、何が未確認かを追えるようにするための正本です。note向けの文章は [`note/AI_CHANGELOG_FOR_NOTE.md`](note/AI_CHANGELOG_FOR_NOTE.md)、日々の判断の短い記録は [`devlog.md`](devlog.md)、本人の原文は [`note/MEMO.md`](note/MEMO.md) にあります。

## 読み方と事実の区別

| 表記 | 意味 |
|---|---|
| `USER` | 本人の依頼・指摘。要約せず原文を `note/MEMO.md` に残す対象 |
| `DECISION` | 依頼を実装方針に変換した決定。後で覆した場合も消さない |
| `IMPLEMENTED` | Unityプロジェクトのソース・素材へ反映済み |
| `VERIFIED` | 隔離コピーまたは静的検算で確認済み。検証条件を併記する |
| `UNVERIFIED` | 実機、配布ビルド、聴感など未確認。完了と書かない |
| `REFERENCE` | 添付画像・資料。デザインの参照であり、画像内の文字や数値を仕様として自動採用しない |

会話の正確な日時が保存されていない過去の依頼は「会話上の順序（日時不明）」と明記します。推測した日付を事実として書きません。

## プロジェクト入口

- Unity: `UnityProject/BigBonusBlitz`、Unity 6000.3.15f1、uGUI、URP 2D。
- 実行経路: `TitleScreen` → `MapScreen` → 街の各画面または `GameController`。
- 数値と確率の正本: `UnityProject/BigBonusBlitz/Assets/Resources/Data/*.json`。確率をC#に直書きしない。
- 制作方針: `CLAUDE.md`、`docs/PROJECT_STATE.md`、`docs/ui_rules.md`。この履歴はそれらの現在値を補足する。
- noteの方針: `docs/note/README.md`。B+C混合、非エンジニア向け、完成と同時公開。費用と公開日は未確定。

## 全体の時系列

### 2026-05-29〜2026-09-06: ブラウザ版の基礎

- `git log` 上の初回コミットは2026-05-29。スロット本体、演出、タイトル、音、ゲーム仕様を段階的に追加。
- ブラウザ版の抽選・滑り・リール配列・ストーリー仕様を後のUnity移植へ引き継ぐ。
- 空白期間や失敗は `docs/note/note-01-ai-game-making.md` と `docs/devlog.md` に記録済み。コードを想像して補完しない。

### 2026-09-07: Unityへ移行

`DECISION` Unity 6.3 + Universal 2D。`BBB.Core`をUnity非依存にし、抽選・制御を直接テストできる構造にした。ブラウザのDOM描画とWeb Audioはそのまま移植せず、UnityのuGUI・AudioManagerへ置き換えた。

### 2026-09-08: リール制御の全数検証

`VERIFIED` 23フラグ × 押し順6 × 押し位置20³ = 1,104,000通りを総当たり。初期方式ではハズレ成立340件が見つかり、先読みとメモ化へ変更。メモ化後の計算は10分超から約6秒へ短縮。左リールの2コマ入れ替えで取りこぼしを解消した。

### 2026-09-09: AT・冒険・ショップの骨格

`DECISION` ATは洞窟のゾーン型。冒険にライフ・エンバー・ソウルを導入し、ショップを装備・スキル・補給の入口にした。すべての確率は `game_config.json` のテーブルで持つ。持ち越しボーナスの引き込みは4コマではなく20コマへ変更。

### 2026-09-10: 技術介入、通貨、画像の検品

`DECISION` Lv、ソウル、エンバーの役割を分離。ビタ押し・2コマ目押し・ミッションフラグを入れ、オート中は技術介入を出さない。キャラ素材は基準画像・姿勢骨格・補完画を分け、生成画像をそのまま信じず、アルファ・解像度・外接枠を確認する。

`VERIFIED` 20万Gの条件付き測定、HP不変条件、技術介入成功率などは `docs/devlog.md` の表を一次資料とする。条件の違う数字を比較しない。

### 2026-09-11〜09-13: タイトルの擬似Live2D、UIガイド、音

`USER` タイトルキャラについて、剣の歪み、白いリボンの独立、前髪と横髪の細分化、毛先を大きくする自然な揺れ、あほげ、スカート、肩を支点にした剣、太ももの固定、目の固定、髪の揺れ過多を順番に指摘。

`IMPLEMENTED` 正式なCubism形式ではなく、Unityの独自2Dメッシュ／シェーダー方式。`SaliaTitleModel.cs` に `ribbon`、`ahoge`、`skirtLift`、`swordRock`、`frontHair`、`sideHair` を分離し、`SaliaLayer.shader` の共通変形場と顔保護領域で目・手などを固定。剣は別の揺れ量、リボンは髪と別パラメータ。毛先側の場を強くし、中央だけが大きく動く不自然さを抑えた。

`VERIFIED` `tools/salia-viewer/qa/` のUnity実描画で15レイヤー、目パチ差分、モーション差分、剣ストレス姿勢10種、手の不変条件、前髪差分を確認。正式なLive2D Cubismの `.cmo3/.moc3` ではないため、Cubism SDKへの直接読込は未対応。

`USER` 各モーションに効果音を付けたい。

`IMPLEMENTED` `MOTION_SOUND.md` に21キュー（hover、click、open、close、purchase、recover、denied、cloth、swordなど）を整理。共通UI、取引成立、タイトルの衣擦れ・剣に接続し、Updateや頂点ごとの連打を禁止。8ボイス、クールダウン、ミュート、既存戦闘音優先を実装。

### 2026-09-14: 背景・UIの制作記録を正本化

`USER` 冒険中の背景を、何層も重ねた奥行き、朝昼夕夜、天候、ステージごとのシナリオに合わせたい。シームレスにしたい。画風は少しアニメ寄りにしたい。作業内容を常にMDへ残したい。

`IMPLEMENTED` 30地点 × 遠景・中景・近景の90層、時間帯、8天候、局所光、クロスフェードを `ADVENTURE_ENVIRONMENTS.md` に整理。`AdventureSeamBaker` は元PNGを変更せず、左右22%の候補から接続経路を計算し、実ブレンド幅を2.4%に絞った1pxの座標テクスチャを作る。反転は使わない。

`VERIFIED` 30地点、90層、4時間帯、8天候、屋内制約、周期境界を隔離Unityで実描画。最新の境界差は `tools/environment-qa/2026-09-20-seams/content-seam-validation.json`（worstPeriodDifference 2.19708832607117E-06、overlap 0.22、blendWidth 0.024）。URP本番、端末性能、配布ビルドは未確認。

`DECISION` UIを変えたら、HTMLだけで完了とせず、Unity uGUIを1280×720と2556×1179で描き出し、PNGを残す。`docs/ui_rules.md` と `docs/check_layout.py` を検品の正本にする。

### 2026-09-14〜09-20: 画面検品と共通ルール

`DECISION` 背景の上に半透明の情報札を置かない。アイコンもレイアウト要素として幅を取る。更新関数内で毎回 `Find` しない。ボタン最小44px、下端余白24px、主要情報の文字重複とクリップを機械検査する。Unityの見た目は作った本人の説明より、隔離コピーの実描画を優先する。

`VERIFIED` `tools/atelier-ui-qa`、`tools/atelier-map-qa`、`tools/environment-qa`、`tools/salia-viewer/qa` に検証画像とJSONを残す。各出力は削除せず、版を増やす。

### 2026-09-21: ステージ選択「蒼の大陸」

`REFERENCE` 1枚目の現行マップから、2枚目の明るい幻想JRPG風マップへ変更する依頼。画像内の文字・地点・数値は仕様として焼き込まず、背景と前景だけを生成し、地点・線・所持数・ボタンはUnityで描画。

`IMPLEMENTED` `AzureMapSkin.cs`、`AtelierMap.cs`、`MapScreen.cs`。1170×540安全領域、空と前景のブリード、Noto Serif JP、紺・金ボタン、羊皮紙詳細、ズーム・現在地・分岐条件・遷移コールバックを維持。

`VERIFIED` `AzureMapValidation.Run`。ステージ選択、ズーム上下限、現在地、条件モーダル、全ナビ、6桁残高、冒険中マップ、2解像度の実描画。最終画像は `tools/azure-map-qa/2026-09-21/v5/`。

### 2026-09-21〜09-22: 街のショップと一覧密度

`REFERENCE` 添付の商店画像は、白い城塞テラス、青い幕、金装飾、左カテゴリ・中央商品・右詳細の3列構造を示す参考資料。画像内の9点、547/558、3,837などはデザイン例であり、実セーブへ固定しない。

`IMPLEMENTED` `AtelierShop.cs` を再構成し、`AzureShopSkin.cs`、`AzureShopTextureImporter.cs`、`Assets/Resources/Art/UI/AzureShop/` を追加。背景、白カード、金枠、狩人の目・灯・護符・油の4商品素材を内蔵 image_gen で生成。元PNGは保持し、Sprite.Createのサブ矩形と9-sliceで使用。実の `ShopDirector`、`AdventureDirector`、ウォレット、確認モーダル、保存、サウンド、ステータス画面を接続。

`DECISION` 最初の2列×2行は商品が大きいが点数を一覧できなかったため、1列縦スクロールへ変更。さらに「10行くらい見たい」という指摘で、商品行を32px＋2px間隔に縮小し、アイコン・商品名・通貨アイコン・価格を横並びにした。現在の9点はスクロールなしで全件、19点の隔離QAでも10行を表示。選択行は紺地＋金の左線。カテゴリ変更は先頭、選択・購入・ステータス復帰は位置を維持。ホイール、ドラッグ、スクロールバー、キーボード選択の自動追従に対応。

`VERIFIED` `AzureShopValidation.Run` の最新 `tools/azure-shop-qa/2026-09-21/v8-compact/validation.json`。購入／キャンセル／確認時再判定／補給／冒険エンバー補充／10行／19点仮データ／ホイール／ドラッグ／選択と購入後の位置保持／キーボード追従／カテゴリ／上限／6桁残高／長い商品名／ステータス／閉じるを、1280×720と2556×1179で実描画して確認。`docs/check_layout.py` も「重なり・はみ出しなし」。

### 2026-09-22: 宇宙のステータス強化

`USER` 参考画像のステータス強化UIを希望。背景は街ではなく宇宙を指定。

`IMPLEMENTED` `StatsScreen.cs` を三列の紋章付き金枠カードへ改良。宇宙背景・透過金枠・3紋章を image_gen で生成し `Art/UI/CelestialStats` に保存。既存の加算・保存・無料振り直し条件を維持し、振り直しには費用確認と確定時再判定を追加。ショップと冒険中で共用。

設計・プロンプト: [CELESTIAL_STATS_DESIGN.md](CELESTIAL_STATS_DESIGN.md)。`VERIFIED`: 隔離Unityの操作・2解像度・文字検査は `tools/celestial-stats-qa/2026-09-22/v8-final/validation.json` でpassed=true。18枚のPNGを保存。実機・配布ビルドは `UNVERIFIED`。

### 2026-09-22: 元気なサリアと装備画面

`USER` 添付の紺と金の装備UIに近づけ、既存のサリアの元気な立ち絵を複数使いたい。

`IMPLEMENTED` `AtelierEquip.cs`、`AzureEquipSkin.cs`。月夜の城塞背景と金の翼剣パネルを生成。既存5姿を原寸コピーし、姿ごとに表示位置を調整。姿切替と装備/ページ選択は独立。売却・まとめ売り・確認・工房品・満杯・効果比較・ステータスへの遷移を維持。

設計・全プロンプト・原本対応: [AZURE_EQUIP_DESIGN.md](AZURE_EQUIP_DESIGN.md)。`VERIFIED`: `tools/azure-equip-qa/2026-09-22/v3-final/validation.json` はpassed=true。14状態×2解像度=28枚、Core/Runtime/Testsコンパイル、配置検算成功。v1のポーズと装飾の衝突、v2の唐草と本文の近接は調整済み。お知らせdev 314。実機・配布ビルド・聴感は `UNVERIFIED`。

### 2026-09-23の継続方針: 設定画面とUI品質

`USER` 設定UIを白い城塞・紺と金の参考へ改善し、今後は都度同じ指摘をしなくても同等の品質へ仕上げる。

`IMPLEMENTED` AtelierSettings全タブ、AzureUiControls（スイッチ・スライダー・ラベル・罫線）、AzureScreenFit、サイドパネル画像。実TitleScreenのデータ説明と実GameControllerのAUTO条件も調整。AGENTS.mdとCLAUDE.mdから `AZURE_UI_QUALITY_STANDARD.md` へ案内。設定仕様/生成原本/プロンプトは `AZURE_SETTINGS_DESIGN.md`。

`VERIFIED` `tools/azure-settings-qa/2026-09-23/v5-final/validation.json` はpassed=true。12状態×2解像度=24枚。字幕100〜150%、スイッチ、プレビュー、設定再読込、表示初期化と音量保持、音量値、BGM切替、実タイトル/冒険の設定、第1削除確認、AUTO保存、BET遮断。コンパイル・配置検算成功。お知らせdev 315。

`UNVERIFIED` 実キー送信、実機タッチ、配布ビルド、聴感。基準追加は未改修の全画面の品質保証を意味しない。今後UIを触るときは次の手順に加えて `AZURE_UI_QUALITY_STANDARD.md` を読む。

### 2026-09-25〜26: エフェクトの見本

`USER` 火花などリアリティのあるエフェクトが作れるなら見たい。→ 見本に「いいじゃん」。続けて斬撃5パターン・画面の揺れ・流線・ネガ反転・コイン・回復/攻撃/シールドのオーラを依頼。その後、現状のBBBで必要なエフェクトの一覧を依頼。

`DECISION` 見本動画を先に作り、合格したものだけ本体へ移す（本人が選択）。画風は項目ごとに使い分け（本人が選択。割り振りはAI: 斬撃・流線・ネガはアニメ寄り、コイン・オーラは実写寄り）。

`IMPLEMENTED` 本体は未変更。作業場 `tools/fx-lab/`（隔離Unityプロジェクトを %TEMP% に作って描き出す。Built-in＋自前ブルーム）。動画とコマ一覧は `docs/art/2026-09-25/`。棚卸しと次の手順は `FX_EFFECTS.md`。

`VERIFIED` Unity 6000.3.15f1 batchmode・1280×720で12本と火花を描き出し、要所のコマを目視。明るい森の背景で白飛びしないよう調整済み。`run.ps1 -Only slash1` が通ることを確認（2026-09-26）。

`UNVERIFIED` 本体（URP・Canvas Overlay）での見え方、60fpsでの手触り、実機、負荷。12本の採否は本人未回答。

## AIが次に作業するときの手順

1. `CLAUDE.md`、このファイル、`docs/PROJECT_STATE.md`、`docs/note/README.md`、対象画面の設計MDを読む。
2. `git status --short` で別セッションの変更を確認し、依頼されていない差分を戻さない。
3. 実装の正本（C#・JSON）を読んでから、既存QAのスクリプトと最新PNGを読む。古い会話の説明だけで修正しない。
4. UI変更は `docs/check_layout.py`、隔離Unityの対象Validation、1280×720／2556×1179のPNG目視を通す。
5. 失敗したら、原因・試した方法・採用した方法・条件を `docs/devlog.md` に残す。画像生成の採用素材はプロンプトと不採用理由も残す。
6. セッション終了時に `docs/note/MATERIAL.md` を追記し、本人の短い依頼は `docs/note/MEMO.md` に原文で追加する。
7. 実機・配布ビルド・聴感・プレイヤー評価をしていない場合は、完了と書かず `UNVERIFIED` にする。

## 未解決・次に確認すること

- `UNVERIFIED` 実機のタッチ感、端末のGPU負荷、配布ビルド、ショップのキーボード以外のゲームパッド操作。
- `UNVERIFIED` 擬似Live2Dを正式なCubism形式へ変換する工程。
- `UNVERIFIED` 背景30地点すべての新画風化。現行のシームレス計算は実装済みだが、画風更新は代表地点から展開する方針。
- 既存のゲームバランス、設定差、第3章以降などは `PROJECT_STATE.md` と `devlog.md` の未解決表を優先する。

## 主な成果物の対応表

| 目的 | 正本・証拠 |
|---|---|
| タイトルの揺れ | `SaliaTitleModel.cs`、`SaliaLayer.shader`、`tools/salia-viewer/qa/` |
| 効果音 | `docs/MOTION_SOUND.md`、`outputs/motion-sound/validation.json` |
| 冒険背景 | `docs/ADVENTURE_ENVIRONMENTS.md`、`tools/environment-qa/` |
| UI設計ノウハウ | `outputs/GAME_UI_DESIGN_PLAYBOOK_JA.md`、`outputs/game-ui-five/DESIGN_PROOF.md` |
| マップ | `docs/AZURE_MAP_DESIGN.md`、`tools/azure-map-qa/2026-09-21/v5/` |
| ショップ | `docs/AZURE_SHOP_DESIGN.md`、`tools/azure-shop-qa/2026-09-21/v8-compact/` |
| note用の時系列 | `docs/note/AI_CHANGELOG_FOR_NOTE.md` |
