# BigBonusBlitz AI 引き継ぎ履歴

最終更新: 2026-09-29（Asia/Tokyo）

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

## 2026-09-29 Judgeの第一停止とcontact順（dev 340）

- `IMPLEMENTED`: `GameController.JudgeStopFx`の第一停止から先行`EnemyHit`を除去し、敵を`ready`に保持。勝利時は既存の`EngageStepRoutine`が剣/槍の`attack-f3-4` contactを受けてから敵のhit/defeatとジャッジ勝利演出を進める。
- `VERIFIED`: 隔離Unity 6000.3.15f1 / productName=shop-qa の `tools/enemy-gameplay-qa/2026-09-29/v6-judge/`。実BET5分岐、剣/槍、剣のとどめ、Judge勝利、説明枠の停止/再開/取消、敵交替を1件Passed。traceは第一停止`enemy=ready`、Judge接触後`pose=27 contacts=1 enemy=defeat`。2解像度の接触画像とvalidation.jsonを保存。
- `DECISION`: Judge第一停止は「熱さの前触れ」であり、被弾/決着状態を先に出さない。fixtureの`PendingTier2=false`/`IsTier2=true`は次の実BETで状態が消えるのを防ぐ検査条件で、本編の抽選率・HP・報酬は変更しない。
- `UNVERIFIED`: 実機入力、聴感、全20敵、全Potion分岐、配布ビルド、最終美術は未確認。本番セーブ、コミット、push、公開は未使用。

## 2026-09-29 ポーション3分岐の実BET確認（dev 341）

- `IMPLEMENTED`: 既存の`StepEngage`/`EngageStepRoutine`契約を変更せず、PotionHeal（LIFE回復）、PotionLarge（次の大攻撃予約）、PotionDefeat（レア役で討伐確定）の本編表示順を検査するfixtureを追加。Smallの予約攻撃もcontact待機を通る。
- `VERIFIED`: 隔離Unity 6000.3.15f1 / productName=shop-qa の `tools/enemy-gameplay-qa/2026-09-29/v8-potion/`。実BET1件Passed。既存5分岐、剣/槍、剣のとどめ、Judge、Potion3分岐、説明枠、敵交替を再確認し、trace/validation.json/静止画を保存。大攻撃はpose27/contact1→enemy hit、Rareはhero contactなしでhp0決着。
- `DECISION`: PotionDefeatは攻撃contactを二重に出さず、ルーレットの確定結果として決着させる。fixtureのTier2開状態と既定敵HPの注入は抽選率/報酬を変えない。
- `UNVERIFIED`: 実機入力、聴感、全20敵、配布ビルド、最終美術、全装備と背景光影は未確認。本番セーブ、コミット、push、公開は未使用。

## 2026-09-29 エンゲージの強弱を帯で区別（dev 342）

- `IMPLEMENTED`: `EngageBandColor`を追加し、準備/敵攻撃/主人公構え/ポーション/次の大攻撃/Judgeを段階色で表示。`EngageBandText`は`EngageNextLarge`を優先して「次は大攻撃！」を表示する。
- `VERIFIED`: 隔離Unity `tools/enemy-gameplay-qa/2026-09-29/v10-wave/`で実BET1件Passed。既存5分岐、剣/槍、剣のとどめ、Judge、Potion3分岐、説明枠、敵交替を再確認し、帯の暖色と文言を機械検査。2解像度の静止画を保存。コンパイル/配置検算/差分検査成功。
- `DECISION`: 波の手掛かりはキャラクターへ色を乗せず、既存の帯だけで段階を示す。抽選・ダメージ・報酬・保存は変更しない。
- `UNVERIFIED`: 敵20種の固有演出、BGMの波、全武器、実機入力、聴感、配布ビルド、最終美術は未確認。

## 2026-09-29 20敵ロスターの設計土台（dev 343）

- `IMPLEMENTED`: `Assets/Resources/Data/enemy_roster_p2.json`に一意の20体を登録。役割/バイオーム/強度帯/telegraph/contact/defeatと`artStatus`を持たせ、existing 3、preview-only 1、planned 16へ分離した。`tools/enemy-roster-qa/README.md`が遭遇表・本編接続との境界を説明する。
- `VERIFIED`: JSONの件数20、IDの一意数20、状態数3/1/16を機械確認。`enemy_tables.json`、抽選、既存素材は変更なし。森狼をbatの見た目へ置換していない。
- `FAILED`: 内蔵画像生成へ20体コンセプトシートを依頼したがHTTP 429 `usage_limit_reached`。原本は存在せず、プロンプトと失敗を`docs/art/2026-09-29/enemy-roster-concept-prompt.txt`へ保存。失敗した画像を本編へ追加していない。
- `DECISION`: 20体を名前だけ遭遇表へ増やさず、各体の透明アトラス/動作/contact/Unity QAを揃えた順に接続する。次工程は生成枠が戻り次第、5体単位で原画→Importer→EnemyPresentation→実描画を通す。

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

### 2026-09-23: 冒険・戦闘・会話の改良を企画（未実装）

`USER` 戦闘の強弱、敵20種、OP会話、話者アイコン、会話裏のGET、主人公のスプライト、モーション接続、武器外見、光と影、文字送りSE、呪い表示とホバー説明を企画してほしい。続けて他AIも作業できる改善手順の記録を依頼。原文は `note/MEMO.md`。

`DECISION` [ADVENTURE_REDESIGN_PLAN_2026-09-23.md](ADVENTURE_REDESIGN_PLAN_2026-09-23.md) を企画正本にする。20体は既存を含む通常16＋ボス4の案。最初に会話/GET/呪い、次に森・3敵・2武器の完成見本、その後に全素材と地域へ展開。演出と抽選変更を分け、OPは現行短縮版29行を維持する。素材契約・変更先・工程・検証・次AI用指示文を記載した。

`VERIFIED` ソースとJSONを調査。現敵は6テーブル/見た目3系統、武器7系統、呪い4種、背景はuGUI RawImage。企画の20体の数量、既存6IDとの一致、12要望、6工程、OP29行、文書内リンク、引き継ぎ参照、お知らせdev 317のJSONと番号重複、差分空白を静的確認済み。不在だった旧背景方針MDへの案内は、存在する記録への案内と不在注記へ修正した。

`UNVERIFIED` 新機能は未実装。GETの実行再現、絵の品質、アニメ接続、武器交換、Light2D構成、性能・実機・聴感は今後の工程。推奨方式や仮定の数値を既存API/実測値として扱わない。

### 2026-09-23〜27: 冒険P1を実装・検品・記録

`USER` 企画を他AIにも渡せるようにしたうえで、繰り返し続行を依頼。原文はMEMO。新しい敵や武器を先に量産せず、計画した情報表示の土台を進めた。

`IMPLEMENTED` OP/冒険共通のDialoguePresenter、話者/表情の生成アトラス、文字送りと既存SEプールへの接続、GETの専用描画階層とモーダル待機、呪いHUD/ホバー/固定/スクロールを実装。[ADVENTURE_PRESENTATION_P1.md](ADVENTURE_PRESENTATION_P1.md)にコード・素材・再制作仕様・次AIの作業単位を記録。2026-09-24の別作業が採用した会話70%縮尺も維持した。

`DECISION` 生成画像は原本を保持してセル切出し。数値/文字はUnityで描画する。GETはMaskと描画順の問題を専用階層で扱い、表示処理から報酬を再付与しない。呪いの同種合算だけで取得時の祝福を捨てない。Cubemap meta、Button root Image、EditMode Awake、QAのCore同期漏れ、実フレームを待たないテストの失敗を正本とdevlogへ残した。

`VERIFIED` 2026-09-26、隔離shop-qaにて1280×720/2556×1179の32枚がpassed。Play Mode連続シナリオ1件passed（GET4件・遅れて開く会話・モーダルで停止復帰・BET終了・残高不変、呪いホバー/選択/固定/閉じる、OP全文/次行/スキップ）。Core/Runtime/Testsコンパイル、配置検算、差分空白検査成功。2026-09-27に記録整理。証拠: `tools/adventure-redesign-qa/2026-09-26/p1-final/`。

`VERIFIED` 2026-09-27、セーブリセットで新SlotMachineへHUDをつなぎ直す修正と解除検査を追加し、同じPlay Modeシナリオが再度passed。証拠: `tools/adventure-redesign-qa/2026-09-27/p1-reset-check/`。お知らせdev 327。

`UNVERIFIED` 実機タッチ、OS実キー、聴感、配布ビルド、OP保存からの全分岐再開。P2〜P5（新主人公、3敵/2武器見本、20敵、全武器、戦闘の波、照明・負荷）は別工程。既存ピクセル主人公が写る検品画像を新スプライトの完成見本と扱わない。

### 2026-09-27: サリア・武器・敵のP2初回見本

2026-09-28の接続見本: `IMPLEMENTED` ベルのBET/第一停止を保持し、第二停止で解放する分割クリップとHeroReelPreview。新メニュー「サリアのリール連動見本」。37ポーズ/15クリップ。正本: [HERO_REEL_BRIDGE_P2.md](HERO_REEL_BRIDGE_P2.md)。第三停止で斬撃と誤読しない。GameControllerの現在の停止数処理を確認済み。本編差し替えはまだで、focus/cast/guard等が次の接続条件。

2026-09-28の定期続行: `IMPLEMENTED` 敵3体×8ポーズ、4状態×3体の表示時計、検品画面の敵操作と相互の被弾反応。v1の早いcontactと遠い位置の被弾をv2/v3で修正。正本: [ENEMY_PRESENTATION_P2.md](ENEMY_PRESENTATION_P2.md)。本編遭遇・命中・報酬・20敵は`UNVERIFIED/未実装`。次はサリアの接続とリール停止単位の本編対応を詰める。定期実行文は新しい本人発言としてMEMOへ重複引用しない。

同日の続行版: `USER`「続きをお願い／何も言わなくてもずっと作業してください」。1時間ごとのこのタスク継続`bigbonusblitz`を設定。`IMPLEMENTED` 歩行8/剣8/槍6の追加原画、両手の軸で槍を回転、武器専用idle/attack/recover、装備切替の位相保持、屈みを拡大しない倍率。`VERIFIED` v8-motion-finalは25枚/144連番と状態検査passed。v7の固定時刻による槍攻撃の撮影ずれも修正。`UNVERIFIED` 最終的な歩行位相、全接続、本編/20敵。原本・プロンプト・失敗理由は正本冒頭へ。継続設定を重複作成せず、今後も最新差分と残作業から再開する。

直近のP2制作記録（2026-09-27）: `USER`「つづきをおねがi」。`IMPLEMENTED` 新サリア9キーポーズ/歩行6コマ、剣/槍、3敵の静止画とUnity専用検品画面。体・武器の分離、動作時計、武器変更予約を追加。正本は [HERO_PRESENTATION_P2.md](HERO_PRESENTATION_P2.md)。`VERIFIED` 最終v6-finalで25静止画/144連番/無音12秒動画、反復歩行・低FPS・握り・未知ID・復帰・再表示の重複なし・DontSave・EventSystem所有の検査passed、コンパイル/配置検算成功。v5で検出したDontSaveと通常検索の不整合も修正した。`UNVERIFIED` 最終アニメ品質、槍の両手原画、中割り、本編接続、敵動作、実機/負荷/聴感。お知らせdev 328。生成PNGは改変せず保存し、セル境界事故とalpha/RGBの判別も記録した。

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


### 2026-09-28 集中・防御・詠唱6コマ、初回合図と動作音

既存の継続依頼を進行。新原画support-v1を内蔵生成し、43ポーズ/20クリップへ。focus/guard/cast/slashを定義し、リール振り始めの最初のevents欠落を修正。武器角度はv6描画で頭の後ろへ消えたためv7で修正。効果音は検品Play Modeのスイッチから既存AudioManagerへ接続、被弾の二重音を抑止。

正本 [HERO_SUPPORT_P2.md](HERO_SUPPORT_P2.md)。v7-support-finalは85静止画/768連番/4無音動画でpassed。v6-supportのPlay Mode音検査1件Passed、共有音量と所有/消音/停止/初回発火を確認。聴感・専用足音・本編・実機は未確認。次は本編キャラ表示と既存Attack/Voice所有の棚卸しと隔離接続。以前の数量は当時の記録で、現状を巻き戻さない。原本/プロンプト/失敗/検査証拠のリンクは正本に集約。コミット・push・公開なし。


### 2026-09-28 新サリアが本編表示へ

本人「tudukiwoonegai」と既存の継続指示から、新モーションをAdventureHeroPresenter経由でGameControllerへ接続。gameplayEnabled=trueを既定、falseで旧表示。_charRtは既存FXの親として維持。43ポーズ/20クリップの体と剣/槍、装備同期、攻撃後の予約反映、リール外の構え解除を実装。本編の音や戦闘結果に新しいCue発火を重ねていない。

正本 [HERO_GAMEPLAY_P2.md](HERO_GAMEPLAY_P2.md)。v3-finalは46描画、実BET/停止/セーブ/モーダルのPlay Mode1件、P1回帰1件がpassed。v1試験初期化の失敗も保存。剣先とステージ札の重なりを避ける倍率170の描画はv4-final。敵3種はまだ検品画面のみ。次はOP全分岐/帰還再入場/原画の頭身と中割り、敵本編接続。過去の「本編未接続」は当時の履歴として残す。本番セーブ保護、コミット・push・公開なし。


### 2026-09-28 / OP逃走の停止修正と帰還検証（dev 334）

- 継続指示を実行。OPのHideBox後もホストが残るため、新サリアの時計をIsShowingで止めると逃走中も静止する不具合を発見。IsDialogueVisibleへ分離し、非表示のDimはraycastTarget=falseへ。表示時に戻す。
- 隔離shop-qaで実OP逃走→倒れる→街→門→完了→帰還→再入場を通し、槍装備の維持を確認。ボタンcallback/StopReelを直接呼ぶ区間を含む。実マウス/タッチの合格とはしない。本番セーブは未使用。
- v5-journeyは機械検査成功でも再入場画像が黒。遷移先生成直後でMapFadeOutが残っていたため、試験だけ明転待ちへ修正。v6-journey-finalは1件Passed、2解像度10画像。P1回帰1件Passedはv5。コンパイル/配置検算成功。旧失敗画像も残した。
- 敵の接続前調査: 本編6テーブルの外見はslime/goblin/bat、見本はslime/goblin/wolf。無断でbatをwolfへ置換せず、未対応種の旧表示復帰と通常/AT別入口、色/倍率/IdleBob、古いコルーチンの停止を次の検査に含める。
- 正本 [HERO_GAMEPLAY_P2.md](HERO_GAMEPLAY_P2.md)、[敵調査](ENEMY_PRESENTATION_P2.md)。OP途中保存/全スキップ、頭身/中割り、敵本編、聴感、実機、20敵/波/光影は未完了。新画像生成なし、コミット/push/公開なし。既存bigbonusblitz継続設定を維持。


### 2026-09-28 / 敵2種の本編接続（dev 335）

- 継続指示に従い、スライム/ゴブリンを通常戦/中ボス/AT戦へ接続。新生成はせず既存24ポーズの原本を使用。ready/strikeの分割を加えて18クリップ。森狼は見本、batは旧表示。外見種を黙って差し替えない。
- 新AdventureEnemyPresenterは表示だけ。既存Enemy親、倍率、色ヒント、結果/音の所有を保持。新原画にIdleBob/Hitの拡大縮小を重ねず、足元と移動の距離を分離。接触後に既存の被弾反応を出し、Coreの値や報酬を追加しない。
- 世代番号で古い登場・入れ子FX・接触待ちを無効化。AT入口では前の帯/ボス状態を解除。新しい敵へ古い演出が遅れて書き込まれないようにした。
- 失敗: 汎用文字列置換でログ用switchへyieldを挿入しコンパイル失敗→対象メソッドへ限定。v1は再生時計をPlayCharacterへ誤配線→Updateへ移動。v2/v3は短い被弾を撮影後の固定時刻で検査して見逃し→要求直後/各フレームの観測に修正。v4は機械成功でもATに通常戦の帯が残るため、v5で共通初期化と検査を追加。
- 最終v5-finalは隔離Unity6000.3.15f1/shop-qaで敵/サリア/P1の3シナリオPassed、2解像度26画像。モーダル停止復帰、AT/敵交替、未対応種復帰、非表示後の接触/出現抑止、旧表示切戻しも確認。コンパイル/配置検算成功。実BETから敵の全結果を通す検査、動画の美術/聴感、実機は未完了。
- 正本 [ENEMY_GAMEPLAY_P2.md](ENEMY_GAMEPLAY_P2.md)。本番セーブ保護、コミット/push/公開なし。全20敵・戦闘の波・光影の完成ではなく、既存継続設定を維持する。


### 2026-09-28 / 実BETの戦闘動作競合とナビ例外（dev 336）

- 継続依頼に従い、結果を直接投入する検査から実BET/StopReel/停止callback/Evaluateへ拡張。v6で構えだけのHAZEにhero=hitを再現。通常役の被弾/攻撃と戦闘結果が重なっていたため、戦闘中は結果側へ主人公動作の所有を集めた。払い出し/GET/リールFXとCoreの数値は保持。
- v7はベルのナビでCanvasGroupのMissingComponentException。GetComponentのUnity nullを??が拾わないため== nullへ変更し、前BETのReveal/Popを世代番号で取消。例外を試験から除外しない。v8で成功、v9に撮影時計の失敗時復旧を追加。
- v9-finalは隔離Unity/shop-qaで4シナリオPassed。Hit/Guard/Dodge/Counter/Attack、既存敵/サリア/P1を検証。30静止画・38連番・無音12fps動画、11ソースの本編/QA照合を保存。Core/Runtime/Testsコンパイルと配置検算成功。役/押下位置/敵初期化と防御の重みは試験設定。実入力/遭遇抽選/全分岐の合格ではない。
- 目視では接近中の敵が説明枠に隠れる点が残る。次は表示時刻/配置、リーチ/回避姿勢/中割り/命中時刻。最終美術や聴感、Judge/Potion全分岐、全20敵、戦闘の波、全武器、光影は未完了。正本 [ENEMY_GAMEPLAY_P2.md](ENEMY_GAMEPLAY_P2.md)。新規生成なし、本番セーブ保持、コミット/push/公開なし。継続設定を維持。


### 2026-09-28 / 戦闘中の説明枠を待機（dev 337）

- v9反撃の目視で敵の下半身が説明枠に隠れていたため、戦闘動作中は説明と文字送りを待機させ、原画の動作終了後に再開する方針を採用。枠の座標変更や固定待ち秒数を増やさず、既存モーションの状態を参照。物語の会話や構え中の説明は残す。
- v10は初回1件Passed。実反撃のstrike中の非表示、別の時計制御区間で長文の文字数保持/モーダル復帰/取消後の非復活/物語の優先を検証。テスト文章の「あ」反復を最終QAでは説明文へ変更した。本編文章の追加ではない。
- 最終v11-dialogue-finalは敵実リール/敵接続/主人公/P1の4シナリオPassed。2解像度32静止画、38反撃連番、無音12fps動画、11ファイル照合を保存。敵の足元が見える接触フレームと説明の復帰を目視。Core/Runtime/Testsコンパイル・配置検算成功。
- 対象は新モーションの説明遮蔽。旧bat/前兆/ボーナス複数敵、実入力/実機/聴感/最終美術の合格へ拡大しない。次は原画contactと被弾/斬撃FXの時刻、武器別リーチ、回避姿勢、Judge/Potion。正本 [ENEMY_GAMEPLAY_P2.md](ENEMY_GAMEPLAY_P2.md)。新規生成なし、本番セーブ保持、コミット/push/公開なし。既存bigbonusblitz継続。

### 2026-09-29 / 剣・槍のcontact同期（dev 338）

- 入力からEvaluateまでの実リール経路で、敵hit/斬撃/音を主人公contact後へ移動。AdventureHeroPresenterのMotionVersion/ImpactReadyで取消と敵交替を無効化し、致死決着もcontact後に開始する。
- `v2-impact-final`は4件Passed。剣/槍の接触順、剣のHP1決着、設定中停止、Cancel、敵交替、既存敵/主人公/P1を確認。固定待ち秒数を増やさず、JSONクリップのcontactイベントを使う。
- 接触静止画、trace、validation.jsonを保存。初回v1はフレーム落ちで接触絵を逃したため、v2は試験区間だけcaptureFramerate=60にし、実ゲーム速度は変えていない。実入力/実機/聴感/全抽選は未確認。

### 2026-09-29 / contact直後の描画証拠（dev 339）

- `AdventureEnemyReelTests`のHeroCue観測直後にも撮影する検査を追加。`v3-impact-visual`で槍pose34・剣pose27の接触静止画、敵hit/defeatへの順序、既存4シナリオPassedを保存した。
- 60fps固定は書出し区間だけで、本編のモーション秒数・Coreの結果・セーブを変更していない。実機/聴感/最終美術は未確認。
