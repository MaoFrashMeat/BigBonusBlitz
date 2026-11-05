# 敵モーションの本編接続

## 2026-10-16 戦闘帯をBET結果の区切りで表示（dev 391）

各BETのアクションは現在の戦闘帯を表示したままcontact/敵反応まで再生し、アクションが終わると結果・報酬の読み取り用に帯を隠す。次のBET結果では最新状態に応じて帯を再生成する。別世代tokenで古い結果coroutineが新しい帯を隠さないようにした。帯の描画を演出routineより先に作り、同期的な初回cueより前に存在するよう順序を整えた。

- attempt21でactive contact/strike時の帯、attack-resolved時の非表示、次の実BET切替を検査。EditMode 1件、接近126枚、17 fixture timeline summary、本編/隔離manifestをPassed。verify harnessもPassed。
- attempt16でstrike時の帯欠落を検知。attempt17〜20は最後の直接routineキャンセル試験をBET assertion対象に誤って含めたため修正。attempt21で通過。
- `attack-resolved-1280.png`/`combat-reward-resumed-1280.png`の目視では帯は非表示。ただしGETと説明/結果文が同時に残り、結果画面全体のレイアウト承認ではない。実入力/実機/音の聴感/配布ビルドは未確認。抽選/戦闘数値/セーブは不変。[レビュー](art/2026-10-16/COMBAT_BAND_RESULT_HANDOFF_REVIEW.md)

## 2026-10-16 実BETの状態遷移・時刻記録を追加（dev 390）

P3の「強弱を測ってから調整」に向け、既存の隔離`AdventureEnemyReelTests.ReelResultsOwnBattleActions`へ出力を追加。各fixture-driven BETを一意IDで分け、BET、StopReel、Evaluate、主人公/敵の状態とポーズ、contact cue、HP/払い出し状態、Unity時刻とcapture FPSを`battle-timeline.csv`へ記録する。`summarize_battle_timing.py`は接触から敵反応/復帰などを`battle-timing-summary.json`へ集計する。テストだけの観測で、本編のテンポ変更や新しい戦闘ルールは追加していない。

- attempt15は17 spinを記録、実BET回帰1件Passed、126枚接近比較Passed、本編/隔離6ファイルhash一致。runner内で集計JSON生成と成功フラグも検査する。個別BETに対して接触と敵反応がcontact後に並ぶ状態を追跡できる。
- これは試験fixtureからStopReelを直接呼び出した計測。実入力の停止間隔、プレイヤーが感じる緊張、聴感、長い探索区間の波を測ったものではない。複数ケースはcaptureFramerate固定で、タイム値を本編の推奨演出秒数に使わない。
- 次はこの記録形式で各結果の候補1区間を実操作/実機条件で測り、静かな区間、接触までの演出、報酬余韻の実測を分けてから、既存の段階表示へ戻す。演出で結果/乱数/報酬を変えず、実測前に波のルールや長さを決めない。[レビュー](art/2026-10-16/COMBAT_TIMING_REEL_REVIEW.md)

実リール静止画では、GET/報酬カードと戦闘帯が同じ画面に並ぶ既存レイアウトも再確認できる。この証拠は接触時刻と状態の確認に使い、情報の読みやすさ/最終配置は別レビューへ残す。

## 2026-10-14 20敵ロスター境界の再検査（dev 385）

`run_catalog_guard.py`はPassed。通常16/ボス4の20一意ID、既存6遭遇ID、`enemy_tables.json`の固定hashとランタイム遭遇表不変を確認した。artStatusはexisting 3 / preview-only 1 / planned 16。これはカタログと本編境界だけの機械検査で、20体の画像・動作・遭遇・最終美術承認ではない。`saveDataTouched=false`。[レビュー](art/2026-10-14/ENEMY_ROSTER_REVALIDATION_REVIEW.md)

## 2026-09-29 20敵ロスターの設計土台を固定（dev 343）

20種をいきなり遭遇表へ投入して既存の仮絵を使い回すと、敵の個性と本編素材の合格条件が混ざるため、まず`Assets/Resources/Data/enemy_roster_p2.json`へ20個の一意ID、役割、バイオーム、予兆、接触、決着、強度帯を固定した。既存のslime/goblin/batは現行素材、forest_wolfは見本のみ、残り16種はアート待ちとして明示している。新規画像生成を1回試したが、内蔵生成がHTTP 429 `usage_limit_reached`で画像を返さなかったため、空絵や代替絵は本編へ入れていない。

- `tools/enemy-roster-qa/README.md`に、カタログと遭遇表・本編実装の境界、素材ゲート、生成失敗を記録。JSONはPowerShellで20件・20一意ID、existing 3 / preview-only 1 / planned 16を確認した。
- plannedの敵は、透明アトラス、原本とプロンプト、EnemyPresentationの動作、contact/決着、隔離Unityの実描画と機械検査が揃うまで`enemy_tables.json`へ追加しない。Forest wolfをbatへ置換しない方針を維持。
- `docs/art/2026-09-29/enemy-roster-concept-prompt.txt`に使用プロンプトと429結果を保存。画像原本は未生成で、カタログの機械検査合格を美術品質の合格とは扱わない。

## 2026-09-29 戦闘の強弱を帯の色と文で伝える（dev 342）

実BETの動作順が整ったため、戦闘中の波を画面上でも読めるようにした。エンゲージ帯を常時同じ青で流すのではなく、現在の状態に合わせて色と文を更新する。新規画像生成なし、抽選・ダメージ・報酬は変更しない。

- 通常の準備は青、敵の攻撃は赤、主人公の構えは青金、ポーションは紫、次の大攻撃予約は暖色、Judgeは深い赤紫へ切り替える。色は背景と文字のコントラストを保つ半透明帯に限定し、キャラクターの原画へ色を乗せない。
- `EngageBandText`へPotionLargeの予約文を追加し、「次は大攻撃！」を次のBETまで流す。`ShowEngageBanners`がEvaluate後のCore状態から再生成するため、セット/ターンの更新と表示がずれない。
- `tools/enemy-gameplay-qa/2026-09-29/v10-wave/`の実BET検査はPassed。既存5分岐、剣/槍、剣のとどめ、Judge、Potion3分岐、説明枠、敵交替を再通過し、PotionLarge直後の帯の暖色と文言を機械検査。静止画で色の段階も確認した。
- これは戦闘の強弱を伝えるUIの初段であり、敵20種の固有演出、BGMの波、全武器のリーチ、実機入力、聴感、最終美術は未検証。

## 2026-09-29 ポーション分岐を実BETへ接続確認（dev 341）

Judgeに続き、ポーションの3結果を本編の実BET→停止→Evaluate経路で確認した。新規画像生成なし、既存原本を保持。

- Loseの回転はLIFEだけを回復し、敵HPを変えず、敵を戦闘中のまま保持する。
- Smallの回転は次の攻撃を大攻撃へ予約し、次の実BETで主人公の`attack-f3-4` contact後に敵へhitを出す。予約は攻撃後に消費し、通常の中攻撃へ戻らない。
- Rareの回転は主人公の攻撃contactを発火せず、ルーレット後に討伐確定として決着する。Judgeの先行被弾修正と矛盾しない。
- `tools/enemy-gameplay-qa/2026-09-29/v8-potion/`の`ReelResultsOwnBattleActions`はPassed。既存5分岐、剣/槍、剣のとどめ、Judge、Potion3分岐、説明枠停止/再開/取消、敵交替を含む。traceはポーション大攻撃で`pose=27 contacts=1 enemy=hit`、レア役ポーションで`hero=walk enemy=idle hp=0`を記録。`validation.json`の機械条件はtrue。
- 1280×720/2556×1179の既存接触画像を再生成し、Potionsの結果は値とtraceで検証。実機・実マウス/タッチ・聴感・全20敵・最終美術は未検証。

## 2026-09-29 ジャッジのとどめもcontact後に決着（dev 340）

ジャッジ勝利の実BETを追加検証したところ、第一停止の熱さ演出が先に`EnemyHit`を呼び、主人公のcontactより前に敵が`hit`へ遷移することが分かった。第一停止は前触れであり、決着ではないため、本編の`JudgeStopFx`から先行被弾を除去し、敵は`ready`のまま保持するよう修正した。新規画像生成なし、既存原本を保持。

- Judge勝利でも`EngageStepRoutine`が武器別`attack-f3-4`を要求し、contactを受けてからジャッジ勝利の斬撃・爆発・決着へ進む契約を確認。
- `AdventureEnemyReelTests`のfixtureは`PendingTier2`を消費済みの`IsTier2`へ明示し、次の実BETで`DrawLottery`が注入したJudge状態をリセットしないようにした。試験のための状態注入であり、抽選率の変更ではない。
- `tools/enemy-gameplay-qa/2026-09-29/v6-judge/`の`ReelResultsOwnBattleActions`はPassed。既存のHit/Guard/Dodge/Counter/Attack、剣のとどめ、説明枠の停止/再開/取消、敵交替の回帰を含め、`validation.json`の`heroContactBeforeEnemyHit`、`lethalSwordCompleted`、`cancelAndReplacementProtected`がtrue。traceはJudgeで`hero=attack-f3-4 pose=27 contacts=1 enemy=defeat`、第一停止中は`enemy=ready`を記録した。
- 1280×720/2556×1179のJudge接触静止画と剣/槍の接触静止画を保存。本編Core/Runtime/Testsコンパイル、`docs/check_layout.py`、`tools/fx_sync.py`、`git diff --check`も成功。画面にはGET/帯/決着が重なるため、これは接触順の機械検査と絵の証拠であり最終美術承認ではない。実機・実マウス/タッチ・聴感・全20敵・最終美術は未検証。

## 2026-09-29 剣・槍の接触と敵の被弾を同期（dev 338）

9月28日版のEngageStepRoutineは攻撃を要求した直後に敵hit/斬撃/音を出していた。さらにCounterの旧EnemyHitが反撃前にも敵を被弾させ、致死結果は攻撃と独立して決着を始めていた。今回、表示の順序を本編で修正した。新規画像生成なし、既存原本を保持。

- AdventureHeroPresenterへMotionVersionとImpactReadyを追加。表示クリップのcontactだけを記録し、ダメージ/報酬/音はここで発火しない。新しい動作要求とCancelで接触状態をリセットする。装備変更は既存の予約処理を維持。
- EngageのAttack/Counter/勝利Judgeではattack-f3-4を要求し、同じ敵・同じ動作のcontactまで待って既存の被弾/斬撃/攻撃音/ボイスを出す。剣の0.07秒、槍の0.075秒はJSONの既存クリップから進み、コードに待ち時間を複製しない。長いフレームで復帰へ進んでも、途中のcontactを記録して取りこぼさない。
- Counterの先行EnemyHitと固定0.25秒待ちは除去。敵strikeの接触後、既存の回避用揺れ→主人公の攻撃→接触後の敵反応へ。回避専用原画や武器の物理的リーチを完成させた変更ではない。
- PresentEngageResultが動作の後に決着表示を呼ぶ。敵交替/Hideによる世代変更後は古い決着を出さない。CoreのHP/報酬/保存は従来どおりEvaluate時に確定し、contactで再加算しない。旧表示の主人公は従来の即時反応に切り戻す。
- 接触待ち中のモーダル/呪い選択/OP会話では表示を待機。主人公の新要求やCancel、敵の交替で待機中の古い攻撃を破棄する。AT固有の攻撃処理は今回の変更範囲外。

### 検査条件・描画の失敗と修正

隔離shop-qaのAdventureEnemyReelTestsを拡張。従来の実BET5分岐に、剣/槍の被弾先行検出と、HPを試験で1へ設定した剣のとどめを追加。敵hit/defeatを観測した時点で主人公contactが1回済んでいること、決着後に敵が非表示になることを確認。別の直接動作要求区間で、設定中の待機、Cancel、敵交替による遅延攻撃の抑止を検証する。遭遇や役、押下位置の指定は試験fixtureであり、実入力や全抽選の検証ではない。

初回`2026-09-29/v1-impact`は1件Passed。ただし画像書出しのフレーム落ちでサリアが既に復帰しており、接触ポーズの絵を撮れていない。次版では該当試験区間だけTime.captureFramerate=60として、観測した主人公state/pose/contact回数/敵stateをtrace.txtへ保存する。実ゲームの速度を変えていない。

最終`tools/enemy-gameplay-qa/2026-09-29/v2-impact-final/`は4シナリオPassed。実BET5分岐、剣/槍の接触後被弾、剣のとどめ、設定中の停止、Cancel、敵交替による遅延攻撃抑止を確認。2解像度の敵/主人公静止画、接触時のtrace、コンパイル・配置検算を保存。`reels/validation.json`は`heroContactBeforeEnemyHit`、`lethalSwordCompleted`、`cancelAndReplacementProtected`をtrueと記録する。実機・実マウス/タッチ・聴感・全遭遇抽選は未検証。継続設定bigbonusblitzを維持し、本番セーブ・コミット・push・公開には触れない。

接触時の絵を明確に残すため、試験側の`OnHeroCue`直後も撮影する`v3-impact-visual`を追加。traceは槍がpose34、剣がpose27でcontact=1のまま敵hit/defeatへ進んだことを記録し、1280×720/2556×1179の接触静止画を保存した。撮影用の60fps固定は試験区間だけで、本編速度の変更ではない。

## 2026-09-28 戦闘動作を説明枠で隠さない（dev 337）

前回の反撃連番では、接近中のゴブリンの下半身が説明枠に隠れた。今回は説明の表示時刻を動作と連携する本編修正。枠の画風や座標を変えず、動作中の視認性を改善する。新規画像生成なし。

- GameControllerの`BattleMotionCoversExplanation`で、新敵のstrike/attack/hit/defeat、主人公の攻撃/復帰/被弾/防御を判定する。ready/idleなど、入力待ちの構えでは説明を表示する。固定秒数を別に持たず、JSONで定義された動作の実際の終了に従う。
- `RefreshExplanationVisibility`は説明中だけ枠を非表示にする。ShowLineは非表示中に文字送り・既読保持時間・ページ送りを進めない。動作終了後に同じ本文・位置から再開し、見えない間の送り入力は捨てる。主人公/敵の時計を説明の待機で止めない。
- 物語の会話はこの待機の対象外。HideDialogueで取消された説明は後から復活しない。通常のモーダル停止と、会話後のGET回避配置は既存処理を維持する。
- 対象は新モーションの見えるスライム/ゴブリン、または既存AT戦中の新主人公。旧batの動作や前兆影/ボーナス3敵の遮蔽をすべて解決したとはしない。全画面の最終美術承認でもない。

### 検証と資料

`AdventureEnemyReelTests`に、実反撃のstrikeフレームで説明枠が非表示である検査を追加。別の時計制御区間で、長文の文字数保持、モーダルからの再開、説明の取消、物語の優先を確認する。後半は動作要求を試験から直接投入し、実BET検証とは区別する。

初回`v10-dialogue`は1件Passed。1280×720/2556×1179の防御・結果・長文再開の6静止画と反撃連番を保存。接近中の敵の足まで見えることを目視。再開画像の「あ」反復文は試験用で、本編文章ではない。最終試験では意味のある検証用長文へ変更した。

最終`tools/enemy-gameplay-qa/2026-09-28/v11-dialogue-final/`は敵実リール/敵接続/主人公実リール/P1会話GET呪いの4シナリオPassed。32静止画と38反撃連番、無音12fpsの`reels/counter-real-reels.mp4`を保存。Core/Runtime/Testsコンパイル・配置検算成功。source-manifest.jsonは本編/QA対象ソースの照合。実機・実マウス/タッチ・聴感・全遭遇抽選は未検証。本番セーブは使用せず、隔離shop-qaで実描画した。

### 次の実装へ

説明による遮蔽の後は、攻撃原画の接触と敵の被弾開始を合わせる。現在のEngageStepRoutineは主人公へ攻撃を要求した直後に敵hitと斬撃FXを出すため、contactの待機・武器別リーチ・回避姿勢を順に検品する。報酬やHPをcontactで再計算せず、表示だけ遅延させ、敵交替/帰還/モーダルで古い演出を残さない検査が必要。Judge/Potion/決着、20敵/戦闘の波/全武器/背景の光影は引き続き未完了。

## 2026-09-28 実BETの戦闘分岐と演出競合（dev 336）

最新証拠: `tools/enemy-gameplay-qa/2026-09-28/v9-final/`。以下のv5記録は前段の履歴。この追記でHit/Guard/Dodge/Counter/Attackの実BET検証を追加した。Judge/Potion全分岐や実入力の合格を意味しない。

### 修正と理由

- GameControllerの通常役演出と戦闘結果が両方サリアを動かしていた。構えだけのHAZEで通常ハズレの被弾、回避になるベルでも第2停止の攻撃が先行する。BET/StopReelではBattleOwnsCharacter、Evaluateでは確定結果のbattleActionで所有を分け、戦闘中はEngage/AT結果側にキャラ動作を任せる。払い出し・GET・リール役FXは既存どおり。通常探索のベル保持/第2停止攻撃は回帰試験で維持を確認。
- PopNaviBadgeのGetComponentに`??`を使うと、Unityのmissing component参照をnullと判定せずCanvasGroup.alphaで例外になるケースを実停止中に検出。Unityの`== null`による追加に変更。遅いReveal/Pop処理はBET世代番号が変わったら終了し、次のナビを古い処理で消さない。
- 抽選、Coreのダメージ、報酬、保存仕様は変更していない。新規原画生成なし。専用動作の描画接続を修正した本編実装であり、最終美術の合格ではない。

### 再現手順・失敗・検証条件

1. `AdventureEnemyReelTests.ReelResultsOwnBattleActions`を隔離shop-qaで実行。ENEMY_REEL_OUTPUTを新しいQAフォルダへ設定。環境ガードで本番project/productNameでは試験を無視する。
2. 敵種とPendingTier2を試験で設定後、OnBetClicked→StopReel→ReelViewの停止コールバック→実Evaluateを通す。旗と押下位置、待機時間は試験側から指定し、防御分岐はその試験のConfigだけ重み100へ。敵遭遇抽選、マウス/タッチ、実機検証ではない。
3. 各フレームで主人公のhit/guard、敵contact、主人公contact回数を観測。構えで被弾しない、敵接触前に被弾しない、回避は攻撃しない、反撃/攻撃はcontact1回と敵HP減少を確認。コルーチンへ結果を直接渡す旧試験とは分ける。
4. v6-reels-before: 修正前に構えターンのhero=hitを再現し失敗。v7-reels: 動作競合を直した後、ベルのナビでMissingComponentExceptionを検出。無視せず修正し、v8-reelsは1件Passed。v9では失敗時もcaptureFramerateを戻すUnityTearDownを追加。
5. v9-final: 敵実リール/敵接続/主人公実リール/P1会話GET呪いの4シナリオPassed。26枚の敵描画、4枚の実リール静止画（1280×720/2556×1179）、38枚の反撃連番。Core/Runtime/Testsコンパイル、check_layout成功。source-manifest.jsonに対象ソースの本編/QA一致を記録。

反撃動画は`v9-final/reels/counter-real-reels.mp4`。連番を12fpsで符号化した無音の試験動画。短い攻撃は一部フレームでしか見えず、滑らかさ/音の品質を承認する資料ではない。静止画・連番を目視し、槍の保持、敵の接近、GETと説明の同時表示を確認した。

### 次に改善する見栄え

- 接近中の敵の下半身が説明枠に隠れる。説明の表示時刻/戦闘時配置を設計し直す。GETの前後関係は今回の回帰で保持したが、画面全体の読みやすさは未完成。
- 攻撃結果の表示と原画contactの時刻、槍の前進量、回避の姿勢・中割りを連続描画で詰める。現在は結果の二重動作を除去した段階で、リーチと命中感の最終調整は残る。
- その後Judge/Potion/決着を実BETで検証し、未対応bat原画/森狼の本編ID/20敵/戦闘の波/全武器/光影へ進む。既存継続設定bigbonusblitzは維持。本番セーブ、コミット、push、公開に触れない。

2026-09-28 / Asia/Tokyo。原画と生成プロンプトの正本は [ENEMY_PRESENTATION_P2.md](ENEMY_PRESENTATION_P2.md)。サリアは [HERO_GAMEPLAY_P2.md](HERO_GAMEPLAY_P2.md)。

## 実装した範囲

スライムとゴブリンを本編の通常戦/中ボス/AT戦の共通Enemyオブジェクトへ接続。森狼は見本のみ。コウモリ、前兆の影、ボーナス中の3敵は既存外見。新規画像生成なし。既存24ポーズ原本を使用し、接続のために原画や刀身を変形していない。

`enemy_presentation_prototype.json` の gameplayEnabled=true、gameplayTypes=[slime,goblin]。falseで次の出現から旧Imageへ戻す。bodyの足元を旧150px枠の下端へ固定し、gameplayHeightとテーブル倍率で拡大。大きい敵でも足を地面より下へ沈めない。

## コードの入口と契約

パスは `UnityProject/BigBonusBlitz/Assets/` 配下。

| 対象 | 役割 |
|---|---|
| Scripts/BBB.Runtime/AdventureEnemyPresenter.cs | 新body/影の表示、旧Imageの切替、色の継承、contact検知。抽選・ダメージ・報酬・音を発火しない |
| Scripts/BBB.Runtime/EnemyPresentation.cs | 本編許可種/身長/接触間隔のJSON設定、表示移動量の横倍率。見本の既定倍率は1のまま |
| Scripts/BBB.Runtime/GameController.cs | BeginEnemyPresentationを通常戦とAT戦から呼ぶ。Updateで時計を進め、結果の表示を転送 |
| Resources/Data/enemy_presentation_prototype.json | ready（予兆2コマの保持）とstrike（踏込みと復帰2コマ）を追加。既存attackクリップを分割して使う |
| Tests/EditMode/AdventureEnemyPlayTests.cs | 隔離Play Modeで各本編経路、切戻し、割込みと2解像度を検査 |

- 通常戦のEnemyAttack構えでreadyを保持。Hit/Guard/Dodge/Counterの確定結果を受け取ったらstrike。表示contactまで待って既存の被弾/防御/回避音とエフェクトを出す。Coreで決まった数値・セーブは変更しない。contactを新しいダメージにしない。
- 本編の左右間隔に合わせ、横の移動量だけを伸ばす。接触先はサリアの親座標+JSONのgameplayContactGap。spriteの縦横比/刀身/体は変形しない。ボス倍率を含めて計算する。
- 新bodyでは従来IdleBobとHitの拡大縮小を重ねない。被弾は専用原画と復帰。決着は専用2コマを最後まで再生してから既存の報酬演出/HideEnemy。既存の親の移動、CanvasGroup、色のヒントは保持。
- 旧Imageは非表示でも色の書込先として残す。LateUpdateでbodyへ反映し、虹色やボス色を消さない。未対応種に替わる時は新viewを隠して旧Imageを戻す。AT入口は通常サイズ/色に初期化し、直前のボスを引き継がない。
- 新しい出現/HideEnemyで世代番号を進める。遅れた出現・SlideIn/SlideOut・旧被弾/旧決着の入れ子コルーチンは世代が変わったら終了。新しい敵へ過去の移動/透明度が書き込まれないようにする。新viewの攻撃割込み後も遅れた被弾反応を出さない。
- モーダル/呪い選択/OP会話表示中は敵時計を停止。既存の背景/画面FX全体を停止する変更ではない。

## 失敗と修正

- 初回コンパイル: 同じswitch文を持つログ生成メソッドへ待機処理を挿入してしまった。EngageStepRoutineだけへ移動しコンパイルを回復。今後は大きなファイルへの文字列置換をメソッド範囲で限定する。
- v1 Play Mode: hitからidleへ戻らない。時計呼出しをPlayCharacterへ入れてしまい、毎フレーム進んでいなかった。Updateのnullガード直後へ修正。機械検査でこの配線ミスを検出。
- v2/v3: 短い被弾を固定時刻で確認したため、画像書出しによる長いフレームの後で既にidleへ戻った状態を検査して失敗。主人公は各フレームの観測、AT被弾は要求直後の状態を検査。待機はゲーム時刻を使い、決着の絵は時計を明示的に進めて描画する。機械検査のためにゲーム動作を遅くしていない。
- v3の決着画像には消えた後の報酬だけのものがある。v4では専用決着コマを進めた直後に撮る。過去画像/失敗XMLは保持。

## 検証の入口と未完了

隔離Unity `work/shop-qa`、productName=shop-qa、Unity6000.3.15f1。環境変数ENEMY_GAMEPLAY_OUTPUTに出力先を設定し、EditModeのBBB.Tests.AdventureEnemyPlayTestsを実行（内部でEnterPlayMode）。本番GameController.Startや本番セーブを使用しない。描画/動作は実uGUI、テスト側から結果/coroutineを投入する区間を含む。

最新結果は `tools/enemy-gameplay-qa/2026-09-28/`。機械検査と最終美術の合格は別。森狼の本編ID、コウモリの新原画、全20敵、主人公の頭身/中割り、戦闘の波、全武器、照明、聴感、実入力/実機/配布ビルドは未完了。

最終 `2026-09-28/v5-final/play-tests.xml` は3シナリオPassed。敵接続1件、サリアの実BET/停止/装備/モーダル回帰1件、会話/GET/呪い回帰1件。敵は2種×待機/構え/接触/被弾/決着の20枚と、ボス色/旧bat/ATの6枚＝26枚を2解像度で保存。新原画の足元・接触位置・決着、ボスの大きさと色を目視。コンパイル/配置検算成功、source-manifestは本編とQAの対象5ファイルの一致を記録。

v4は敵1件と回帰2件が成功したが、描画でAT入口に前の通常戦の帯が残る点を発見。v5では共通の出現初期化で帯・ボス表示状態を解除し、ATで残らない検査を追加した。モーダル停止復帰、AT登場途中で別敵へ替える、接触前にHideして遅れた被弾を抑止、出現予約の取消、JSON falseによる旧表示も確認。Guard/Dodge/Counterは接続コードを共通化したが全結果の独立した実BET通過は未確認。機械検査の成功から対象を拡大して報告しない。

次は本編の実BETから敵の全結果へ至る一連の操作と動画、原画の接続品質を詰める。試験の疑似結果投入を「全抽選/全20敵の通過」と扱わない。背景、前兆の影、ボーナス複数敵は対象ごとに接続する。


### 2026-11-03 内蔵画像生成を再開、コウモリ8姿勢を本編接続（dev 405）

コウモリ6動作を既存遭遇へ接続し、隔離Unityの96連番・本編回帰1件/0失敗・2解像度を確認。灯蛾と5武器は生成候補まで。全20敵/7武器/背景/P2〜P5は未完了、最終美術未承認。[制作・検証記録](art/2026-11-03/BAT_RUNTIME_AND_ASSET_PRODUCTION_REVIEW.md)。
