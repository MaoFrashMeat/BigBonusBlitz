# 本編サリア接続の検品

- 2026-10-16/runtime-approach-attempt10: `run_runtime_approach_qa.ps1`。126 source-pose描画と実BET EditMode1件Passed。剣/槍の接近、両武器contact先端の横照準誤差0px、recover後rest復帰true。`source-manifest.json`は6ファイルの本編/隔離SHA256一致を示し、summary gate true。最終美術承認ではない。[レビュー](../../docs/art/2026-10-16/HERO_SWORD_APPROACH_REEL_REVIEW.md)

- 2026-10-10/v11-identity-guard: 既存v1低槍候補を読み取り専用で検査。重要ポーズ、意図したループ重複、支持アンカー、接地値29〜36、握り座標、剣/槍カタログpassed。ランタイム未接続・最終美術未承認。[レビュー](../../docs/art/2026-10-10/WOLF_IDENTITY_SUPPORT_GUARD_REVIEW.md)

- 2026-10-10/v10-wolf-background-approach: C-1背景上で森狼の剣ベースラインと槍の低い接近/接触/復帰を126枚・2解像度描画。水平接触、復帰、割り込み、支持足、セーブ不変passed。先行v9は本編JSONにQAアトラス名がなく検証前停止。候補のみ、`mainEnabled=false`、最終美術未承認。[レビュー](../../docs/art/2026-10-10/WOLF_BACKGROUND_WEAPON_REVIEW.md)

- 2026-10-03/v3-grip-comparison: JsonUtilityの未指定手データに対する試験のnull判定で失敗。v4で幅/高さも確認するよう修正。
- 2026-10-03/v4-grip-comparison: 手を前景に重ねる剣3ポーズの比較。184枚、無音2秒/30fps動画2本。描画順/握り位置/切替後の非表示passed。`baseline-config.json`は採用前の比較用。再現は隔離側だけにbaselineを配置し、`HERO_GRIP_CANDIDATE`へ候補座標JSONを指定して`AdventureHeroValidation.RunGripComparison`を実行する。
- 2026-10-03/v5-grip-final: 剣の待機・復帰3ポーズを本編に反映後、実BET主人公/敵2シナリオPassed、54枚/2解像度passed。7対象ファイルの本編/隔離SHA256一致。歩行候補は未採用。正本は[握りレビュー](../../docs/art/2026-10-03/GRIP_LAYER_REVIEW.md)。

正本: [HERO_GAMEPLAY_P2.md](../../docs/HERO_GAMEPLAY_P2.md)。日付2026-09-28、隔離shop-qa/Unity6000.3.15f1。

- 2026-10-10/v8-combat-intensity-rerun: `run_combat_intensity.py` が既存の `EngageBandText` / `EngageBandColor` を7状態（turn1/敵攻撃/溜め/攻撃確定/次は大攻撃/ポーション/JUDGE）で14枚・2解像度描画。上下帯の色一致、文と色の対応、7色一意、セーブ不変がPassed。初回v8は「次の攻撃が大」という誤った期待文で失敗したため、設定正本の「次は大攻撃」に修正して再実行した。`source-manifest.json` と `intensity.csv` を保存。[レビュー](../../docs/art/2026-10-10/COMBAT_INTENSITY_REVIEW.md)。実機/聴感/配布ビルド・最終美術は未確認。

- v1: 本編2武器×11要求×2解像度と会話2枚、計46枚passed。Play Mode検査は初期化時のNullReferenceで失敗。
- v2-play: テストの参照保持を静的ヘルパーへ変更し1シナリオPassed。実BET、右→左→中/中→右→左、構え30秒保持、第二停止contact一度、第三/重複停止で追加なし、セーブ再読込、装備解除を確認。
- v3-final: 46描画passed、敵/前兆でidle、旧表示への切戻しも成功。Play Mode1件Passed（Update経由のモーダル停止/復帰を追加）。P1回帰1件Passed（会話/GET/呪い/OP送り）。コンパイルと配置検算成功。
- v4-final: 剣先とステージ札の重なりを避けるため、本編の身長190→170に変更した最終描画版。46枚passed。ゲームコードとPlay Mode検査はv3から変更なし。

- v5-journey: OP逃走→街→門→完了→帰還再入場のPlay Mode1件Passed、P1回帰1件Passed。再入場画像は撮影が早すぎて黒幕中。コンパイル/配置検算成功。
- v6-journey-final: MapFadeOut終了待ちを試験へ追加、1件Passed・10画像。会話中停止/逃走再開/非表示暗幕のraycast解除/槍維持を確認。実行時は環境変数HERO_JOURNEY_OUTPUTへ出力先絶対パスを設定し、隔離プロジェクトでEditModeのBBB.Tests.AdventureHeroJourneyTestsを実行。テスト内でEnterPlayModeする。

本番セーブは使わず、QAコピー専用の保存領域で読込を検査。実キー/マウス/タッチ/聴感/ビルド/OP全保存分岐は未確認。操作はcallback/StopReelの直接呼出しを含む。機械検査passedは最終美術合格ではない。敵3体の本編接続は次工程。

- 2026-10-03/v1-contact-resume: 攻撃中の復帰予約でcontact/世代を保持。AdventureHeroPlayTests 1件Passed、AdventureHeroValidation 54枚passed。歩行位相と攻撃中の装備予約を追加検査。既存PNG原本を使用。source-manifest.jsonで7対象ファイルの本編/隔離一致を確認。最終美術/実機/聴感は未確認。
- 同版enemy-reels.xml: 既存のAdventureEnemyReelTestsも1件Passed。reels/にJudge/Potion/剣/槍contactのtraceと静止画を保存。

- 2026-10-03/v2-walk-comparison: 既存walk-v3と未採用walk-v5の比較。208枚、無音2秒30fps動画2本。主要姿勢88枚は2解像度、連番120枚は1280。比較成功は美術採用ではない。[レビュー](../../docs/art/2026-10-03/WALK_REVIEW.md)。
  再現: 隔離shop-qaへ現在のソースと既存素材を同期し、候補原本をResources/Art/HeroPrototype/walk-v5-qa.pngへコピー。HERO_GAMEPLAY_OUTPUTに新しい出力先、HERO_WALK_CANDIDATEにdocs/art/2026-10-03/walk-v5-candidate-poses.jsonの絶対パスを設定。Unityを-batchmode -quit -executeMethod AdventureHeroValidation.RunWalkComparisonで起動（実描画のため-nographicsは指定しない）。本編JSONへの候補追記は不要。

- 2026-10-03/v6-walk-keys: RunWalkKeyPosesで4つの歩行キーを個別に描画、剣/槍/構え参考20枚・2解像度passed。ループは未作成、接地ペアは候補、passingは未合格。[原本と再現手順](../../docs/art/2026-10-03/WALK_KEYS_REVIEW.md)。

- 2026-10-03/v7-walk-blocking: 新規通過2枚を使った20静止画＋90連番、3秒無音MP4。HERO_WALK_BLOCKING=1で4キー→構え→攻撃→復帰を検査。110枚passed。本編未採用、中割りなしの粗い見本。[再現手順](../../docs/art/2026-10-03/WALK_BLOCKING_REVIEW.md)。

- 2026-10-04/v6-sword-clearance: 同系列の剣8姿勢、旧/候補200描画・各2秒動画。Texture/握り/下端/全キー/contact1回/idle復帰passed。v7-sword-candidate-reelsで候補を隔離へ一時適用し実BET2件Passed、設定復元。本編未採用。[原本・失敗・残課題と再現](../../docs/art/2026-10-04/SWORD_CYCLE_REVIEW.md)。
# Isolated runtime approach QA

Run `run_runtime_approach_qa.ps1` from PowerShell to create a fresh `YYYY-MM-DD/runtime-approach-attemptNN` result. The script builds an isolated `shop-qa` Unity project from current project sources without user saves, validates source/isolated SHA256 equality for its six declared inputs, runs the 126-image approach comparison and `AdventureEnemyReelTests.ReelResultsOwnBattleActions`, then parses `real-enemy-reel/battle-timeline.csv` through `summarize_battle_timing.py`. A successful runner exit requires the play-test XML, approach validation, CSV, and `battle-timing-summary.json` outputs.

The timeline contains fixture-driven BET, directly called stop-reel callbacks, Evaluate, pose/contact and recovery observations. Some cases set `Time.captureFramerate`; none are real mouse/touch, hardware, listening, production pacing, or art-approval measurements. Keep those claims separate. Prior failures (attempt11–13) are retained beside the latest result rather than deleted.
