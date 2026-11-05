# MATERIAL.md — note の材料の割り当て表

### 2026-10-18 [見た目回帰/Unityライフサイクル] 戦闘帯のピーク退避
- 出どころ: `docs/devlog.md` dev 396、[COMBAT_INTENSITY_PEAK_STRIP_REVIEW.md](../art/2026-10-18/COMBAT_INTENSITY_PEAK_STRIP_REVIEW.md)、attempt09/10。peakで下段を除き、同フレーム`Destroy`遅延の古い帯残像を非表示化で防ぐ。attempt09の参照assertionだけでは残像を見逃し、実画像とactive object数を検査する必要があった。
- 記事のどこに使えそうか: 4. Unity実描画とゲーム本編の回帰、8. UIの重なりをどう検証するか、9. 全画面美術/実機の残作業。fixtureの機械合格と目視/最終承認を分ける。

### 2026-10-18 [QA回帰ゲート] BET停止区間/cue境界
- 出どころ: `docs/devlog.md` dev 395、[全sample検査レビュー](../art/2026-10-18/HERO_ATTACK_SEQUENCE_PHASE_GUARD_REVIEW.md)、隔離Unity attempt08。剣/槍のf1/f2を停止要求まで保持し、anticipation/swing/contactの段階と各一回性を確認。ゲーム/セーブ/原画は変更せず、槍の同pose32の意図は未決定。
- 記事のどこに使えそうか: 4. 既存戦闘を壊さない接続、9. 今も残る制約。状態検査の通過と原画/実機の美術承認を分ける材料。

### 2026-10-18 [BET攻撃全段/隔離計測] f1→f2→f3/4
- 出どころ: `docs/devlog.md` dev 394、[通常攻撃全段レビュー](../art/2026-10-18/HERO_ATTACK_SEQUENCE_REVIEW.md)、`tools/hero-gameplay-qa/2026-10-18/runtime-approach-attempt06/`。剣/槍を120Hzで測定し、f1/f2 stop handoff、全原画pose、contact一度、walk復帰を確認。槍はf1終端/f2ともpose32で一段目停止に絵の変化がない。素材判断は未決定。
- 記事のどこに使えそうか: 4. 既存BET/戦闘を壊さない接続、8. 絵と音、9. 残る動作。計測ゲートと槍の原画不足/最終美術判断を分け、QA fixture失敗(attempt04)はテスト誤期待だったことも記録。


### 2026-10-18 [接近→攻撃/120Hz隔離計測] handoffの時間とpose
- 出どころ: `docs/devlog.md` dev 393、[接近・攻撃切替レビュー](../art/2026-10-18/HERO_APPROACH_WALK_CADENCE_REVIEW.md)、`tools/hero-gameplay-qa/2026-10-18/runtime-approach-attempt03/`。剣/槍各16枚、0.3583s pose18から0.3667s pose26/33の切替と接触イベントを測定。最初のQA誤assertと修正も保持。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、8. 絵と音、9. 今も直っていないもの。一段切替の機械検査を中割り/支持足/美術合格と混同しない。

### 2026-10-18 [歩行テンポ/隔離実描画] 攻撃前接近の原画同期
- 出どころ: `docs/devlog.md` dev 392、[HERO_APPROACH_WALK_CADENCE_REVIEW.md](../art/2026-10-18/HERO_APPROACH_WALK_CADENCE_REVIEW.md)、隔離Unity attempt22。8コマを0.36秒に圧縮していた表示を、原画の0.11秒/コマへ合わせた。剣/槍の時間ゲートと実BET回帰Passed。素材/ゲーム数値/セーブ変更なし。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、8. 絵と音をどうしたか、9. 今も直っていないもの。機械修正の合格と接近→攻撃の最終中割り/美術承認を区別する。

### 2026-10-16 [表示境界/競合修正] 戦闘帯とBET結果の切り替え
- 出どころ: `docs/devlog.md` dev 391、[試験・失敗・目視範囲](../art/2026-10-16/COMBAT_BAND_RESULT_HANDOFF_REVIEW.md)、`tools/hero-gameplay-qa/2026-10-16/runtime-approach-attempt21/`。隔離Unity実BET回帰/接近126枚/fixture 17spin summary/検証ハーネスを確認。結果とGETの重なりや最終美術は未承認。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、9. 今も直っていないもの。Coroutine世代競合の修正と、機能検査/最終画面品質の境界。

### 2026-10-16 [検査/計測準備] 実BET状態タイムライン
- 出どころ: `docs/devlog.md` dev 390、[試験条件・失敗・未検証](../art/2026-10-16/COMBAT_TIMING_REEL_REVIEW.md)、[17件の集計](../../tools/hero-gameplay-qa/2026-10-16/runtime-approach-attempt15/real-enemy-reel/battle-timing-summary.json)。隔離fixture 17spin、実リール回帰と126枚接近比較Passed。実入力/演出秒数/美術承認ではない。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、9. 今も直っていないもの。試験計測値とプレイ実測を分ける材料。

### 2026-10-16 [本編接続/機能検証] 剣/槍攻撃の踏み込み実リール
- 出どころ: `docs/devlog.md` dev 389、[HERO_SWORD_APPROACH_REEL_REVIEW.md](../art/2026-10-16/HERO_SWORD_APPROACH_REEL_REVIEW.md)、[実リール結果](../../tools/hero-gameplay-qa/2026-10-16/runtime-approach-attempt10/real-enemy-reel/validation.json)。剣/槍機能passed、両武器先端X誤差0px、recover復帰Passed。最終美術/実機/配布ビルド未承認。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、9. 今も残る制約。機能ゲートと美術承認を分ける材料。

### 2026-10-16 [本編接続/隔離検証] サリアの攻撃前踏み込み
- 出どころ: `docs/devlog.md` dev 388、[HERO_ATTACK_APPROACH_INTEGRATION_REVIEW.md](../art/2026-10-16/HERO_ATTACK_APPROACH_INTEGRATION_REVIEW.md)、[隔離比較JSON](../../tools/hero-gameplay-qa/2026-10-16/runtime-approach-attempt05/validation.json)、[実敵リールJSON](../../tools/hero-gameplay-qa/2026-10-16/runtime-approach-attempt05/real-enemy-reel/validation.json)。既存敵へ踏み込む機能は検証済み、槍スライム接触絵は未承認、セーブ/遭遇/報酬不変。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、8. 絵と音をどうしたか、9. 今も直っていないもの。機能接続と美術承認を分ける材料。

### 2026-10-14 [検品/可読性] 戦闘強弱帯
- 出どころ: `docs/devlog.md` dev 386、[COMBAT_INTENSITY_READABILITY_REVIEW.md](../art/2026-10-14/COMBAT_INTENSITY_READABILITY_REVIEW.md)、`tools/hero-gameplay-qa/2026-10-14/dev386-intensity-band-layout-final/validation.json`。7状態/14枚を2解像度で隔離Unity検証。既存マーキーの文字過大・札/HP重なりを縮小調整した。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、8. 絵と音をどうしたか。機械可読性と最終美術承認を分ける材料。

### 2026-10-14 [検証/設計境界] 敵20種ロスター
- 出どころ: `docs/devlog.md` dev 385、[ENEMY_ROSTER_REVALIDATION_REVIEW.md](../art/2026-10-14/ENEMY_ROSTER_REVALIDATION_REVIEW.md)、`tools/enemy-roster-qa/2026-10-14/dev385-roster-revalidation/validation.json`。20一意ID/通常16/ボス4/既存6 encounter ID/hash維持。planned 16で、プレイ可能化・美術承認ではない。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、9. 今も直っていないもの。設計数と実装数の違いを記録する材料。

### 2026-10-14 [隔離再描画] 敵3種・剣槍見本
- 出どころ: `docs/devlog.md` dev 384、[ENEMY_THREE_MOTION_REFRESH_REVIEW.md](../art/2026-10-14/ENEMY_THREE_MOTION_REFRESH_REVIEW.md)、`tools/hero-prototype-qa/2026-10-14/v10-motion-refresh/validation.json`。85静止画、敵288フレーム、主人公43ポーズ/20クリップ、低FPS/割込み/決着/槍両手軸などを再検証。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、8. 絵と音をどうしたか、9. 今も直っていないもの。隔離見本と本編統合/最終美術を分ける材料。

### 2026-10-14 [境界/再検査] P2本編境界ガード
- 出どころ: `docs/devlog.md` dev 383、[PRODUCTION_BOUNDARY_REFRESH_REVIEW.md](../art/2026-10-14/PRODUCTION_BOUNDARY_REFRESH_REVIEW.md)、`tools/production-boundary-qa/2026-10-14/dev383-boundary-refresh/validation.json`。主人公接近、敵runtime/preview、武器7系統、20敵、遭遇IDとランタイム参照を再監査。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、9. 今も直っていないもの。本番境界と設計カタログを分ける材料。

### 2026-10-10 [境界/再監査] P2本編境界ガード
- 出どころ: `docs/devlog.md` dev 382、[PRODUCTION_BOUNDARY_RERUN_REVIEW.md](../art/2026-10-10/PRODUCTION_BOUNDARY_RERUN_REVIEW.md)、`tools/production-boundary-qa/2026-10-10/dev382-production-boundary-rerun/validation.json`。runtime/preview、武器7系統、20敵、既存6遭遇ID、カタログ参照を読み取り専用再照合。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、9. 今も直っていないもの。本編保護と候補/設計カタログの境界を再確認する材料。

### 2026-10-10 [回帰/隔離] EditMode再検証
- 出どころ: `docs/devlog.md` dev 381、[EDITMODE_REFRESH_REVIEW.md](../art/2026-10-10/EDITMODE_REFRESH_REVIEW.md)、`tools/environment-qa/2026-10-11/v10-editmode-refresh/editmode.xml`。隔離Unityで129件/123Passed/0失敗/6 Ignoreを再実行。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、9. 今も直っていないもの。機械回帰の結果と本編統合/最終美術の境界を分ける材料。

### 2026-10-10 [再実描画/光影] 背景ソース更新
- 出どころ: `docs/devlog.md` dev 380、[BACKGROUND_REFRESH_REVIEW.md](../art/2026-10-10/BACKGROUND_REFRESH_REVIEW.md)、`tools/environment-qa/2026-10-11/v9-background-refresh/validation.json`。隔離Unityで30地点/森12条件/時間4/天候9/両解像度を再描画し、森昼晴れ・夜雨とC-1ゲーム画面を目視。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、8. 絵と音をどうしたか、9. 今も直っていないもの。背景のソース検証と原画/最終美術承認を分ける材料。

### 2026-10-10 [境界/検証] P2本編境界ガード
- 出どころ: `docs/devlog.md` dev 379、[PRODUCTION_BOUNDARY_GUARD_REVIEW.md](../art/2026-10-10/PRODUCTION_BOUNDARY_GUARD_REVIEW.md)、`tools/production-boundary-qa/2026-10-10/dev379-production-boundary/validation.json`。接近/敵runtime/武器7系統/20敵/既存6遭遇ID/カタログ参照を読み取り専用監査。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、9. 今も直っていないもの。本編保護と候補/設計カタログを分ける材料。

### 2026-10-10 [検証/再実描画] 敵3種・剣槍の隔離更新
- 出どころ: `docs/devlog.md` dev 378、[ENEMY_THREE_MOTION_REFRESH_REVIEW.md](../art/2026-10-10/ENEMY_THREE_MOTION_REFRESH_REVIEW.md)、`tools/hero-prototype-qa/2026-10-11/v9-enemy-motion-refresh/validation.json`。85静止画/288敵フレーム/24ポーズ/12クリップ、森狼接触とスライム槍攻撃の目視を再確認。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、8. 絵と音をどうしたか、9. 今も直っていないもの。隔離再実描画と本編統合/最終美術を分ける材料。

### 2026-10-10 [検証/行列] 敵・背景同場面証拠マトリクス
- 出どころ: `docs/devlog.md` dev 377、[SCENE_MATRIX_GUARD_REVIEW.md](../art/2026-10-10/SCENE_MATRIX_GUARD_REVIEW.md)、`tools/scene-matrix-qa/2026-10-10/dev377-scene-matrix-guard/validation.json`。既存v10/v8/dev371/dev376を読み取り専用再照合。126/85枚、30地点/森12条件、両ゲーム解像度と1280×426源画像を確認。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、8. 絵と音をどうしたか、9. 今も直っていないもの。同場面の機械検査と原画/本編/最終美術承認を分ける材料。

### 2026-10-10 [検証/光影] 昼晴れ・夜雨プローブ
- 出どころ: `docs/devlog.md` dev 376、[BACKGROUND_LIGHT_SHADOW_GUARD_REVIEW.md](../art/2026-10-10/BACKGROUND_LIGHT_SHADOW_GUARD_REVIEW.md)、`tools/environment-qa/2026-10-10/dev376-light-shadow-guard/validation.json`。平均RGB差56.128と背景ソース境界を照合。

### 2026-10-10 [検証/境界] 敵3種ソース/本編
- 出どころ: `docs/devlog.md` dev 375、[ENEMY_THREE_SOURCE_GUARD_REVIEW.md](../art/2026-10-10/ENEMY_THREE_SOURCE_GUARD_REVIEW.md)、`tools/enemy-gameplay-qa/2026-10-10/dev375-three-enemy-guard/validation.json`。3体のモーション定義とruntime境界を照合。

### 2026-10-10 [検証/動作見本] 敵3種の予兆・接触・決着
- 出どころ: `docs/devlog.md` dev 374、[ENEMY_THREE_MOTION_RERUN_REVIEW.md](../art/2026-10-10/ENEMY_THREE_MOTION_RERUN_REVIEW.md)、`tools/hero-prototype-qa/2026-10-10/v8-enemy-motion-rerun/validation.json`。敵3種288連番/12クリップと剣槍接続を照合。

### 2026-10-10 [検証/契約] 森狼・剣槍支持足/握り
- 出どころ: `docs/devlog.md` dev 373、[WOLF_IDENTITY_SUPPORT_GUARD_REVIEW.md](../art/2026-10-10/WOLF_IDENTITY_SUPPORT_GUARD_REVIEW.md)、`tools/hero-gameplay-qa/2026-10-10/v11-identity-guard/validation.json`。既存候補の姿勢数値契約を照合。

### 2026-10-10 [検証/比較] 森狼・背景・剣槍同場面
- 出どころ: `docs/devlog.md` dev 372、[WOLF_BACKGROUND_WEAPON_REVIEW.md](../art/2026-10-10/WOLF_BACKGROUND_WEAPON_REVIEW.md)、`tools/hero-gameplay-qa/2026-10-10/v10-wolf-background-approach/validation.json`。v9失敗ログを保持し、v10の126枚/2解像度とセーブ不変を照合。

記録の本体は `docs/devlog.md`（決定と理由）と外部知能の `errors/ERRORS.md`（失敗）。
**同じことをここに書き写さない。** このファイルが持つのは次の2つだけ。

1. 記事のどの節に、どの材料が入っているか（照合用）
2. まだ記事に入れていない材料の控え

---

### 2026-10-10 [検証/境界] 背景30地点と光影ソース
- 出どころ: `docs/devlog.md` dev 371、[BACKGROUND_SOURCE_GUARD_REVIEW.md](../art/2026-10-10/BACKGROUND_SOURCE_GUARD_REVIEW.md)、`tools/environment-qa/2026-10-10/dev371-background-source-guard/validation.json`。30地点/C-1三層、12条件、継ぎ目、屋内天候、セーブ不変を再照合。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、8. 絵と音をどうしたか、9. 今も直っていないもの。光影の検証と最終美術/実機性能を分ける材料。

### 2026-10-10 [仕組み/境界] 装備外見7系統カタログ
- 出どころ: `docs/devlog.md` dev 370、[WEAPON_VISUAL_CATALOG_GUARD_REVIEW.md](../art/2026-10-10/WEAPON_VISUAL_CATALOG_GUARD_REVIEW.md)、`tools/equipment-qa/2026-10-10/dev370-weapon-visual-guard/validation.json`。剣/槍は既存アトラスと一致し、5武器はplannedのまま。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、8. 絵と音をどうしたか。外見定義の設計ゲートと実画像/本編表示を分ける材料。

### 2026-10-10 [検証/境界] 20敵カタログと遭遇表のSHA256ゲート
- 出どころ: `docs/devlog.md` dev 369、[ENEMY_ROSTER_CATALOG_GUARD_REVIEW.md](../art/2026-10-10/ENEMY_ROSTER_CATALOG_GUARD_REVIEW.md)、`tools/enemy-roster-qa/2026-10-10/dev369-catalog-guard/validation.json`。20一意ID、通常16/ボス4、既存6遭遇IDと遭遇表不変ハッシュを確認。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、9. 今も直っていないもの。設計カタログを20体実装・美術承認と数えない境界の材料。

### 2026-10-10 [仕組み/比較] 戦闘の強弱帯の隔離検証
- 出どころ: `docs/devlog.md` dev 368、[COMBAT_INTENSITY_REVIEW.md](../art/2026-10-10/COMBAT_INTENSITY_REVIEW.md)、`tools/hero-gameplay-qa/2026-10-10/v8-combat-intensity-rerun/`。7状態を2解像度で14枚、上下帯の色一致・文との対応・7色一意・セーブ不変を確認。
- 記事のどこに使えそうか: 4. 既存ゲームを壊さない接続、8. 絵と音をどうしたか。戦闘強弱の機械検査と新原画/最終美術の承認を分ける材料。

### 2026-10-10 [失敗] 森狼の透明復帰原画identity drift
- 出どころ: `docs/devlog.md` 2026-10-10、`docs/art/2026-10-10/spear-wolf-recovery-rejected-v3.png` と全文プロンプト/台帳。透明・低姿勢・両手軸・支持足は得たが、body-v2の髪型/リボン/画風からずれQA未配置。
- 記事のどこに使えそうか: 6. 壊れた箇所カタログ、8. 絵と音をどうしたか、9. 今も直っていないもの。生成を続けず既存機械候補と次の戦闘強弱へ移る判断の材料。

### 2026-10-10 [計測] 森狼復帰候補の支持足アルファ境界
- 出どころ: `docs/devlog.md` 2026-10-10、`tools/hero-gameplay-qa/2026-10-08/v7-wolf-standard-recovery-stills/footplane-metrics.json`。低い/標準戻りの最下位アルファ行1166/1172を測定。透明境界差であり、Unityの足接地は未合格。
- 記事のどこに使えそうか: 6. 壊れた箇所カタログ、8. 絵と音をどうしたか、9. 今も直っていないもの。画像境界と実画面接地を分ける判断の材料。

### 2026-10-10 [比較] 森狼の標準/低い復帰を同画面検品
- 出どころ: `docs/devlog.md` 2026-10-10、隔離Unity `tools/hero-gameplay-qa/2026-10-08/v6-wolf-recovery-stills/` と `v7-wolf-standard-recovery-stills/`。両ルート126描画で機械検査passed。低い復帰は四足高さ帯へ連続、標準は立ち上がりが強いという暫定目視判断とRGB差分平均を保存。支持足は未合格。
- 記事のどこに使えそうか: 6. 壊れた箇所カタログ、8. 絵と音をどうしたか、9. 今も直っていないもの。機械接触と復帰の見た目を別ゲートにする材料。

### 2026-10-10 [仕組み/絵] 森狼の歩行復帰静止画を連番化
- 出どころ: `docs/devlog.md` 2026-10-10、`tools/hero-gameplay-qa/2026-10-08/v6-wolf-recovery-stills/`。接触後0.18秒の復帰静止画を追加し、126描画・接触/足元/低FPS/取消・割込み/contact1回passed。既存候補の機械比較で、支持足と最終美術は未合格。
- 記事のどこに使えそうか: 6. 壊れた箇所カタログ、8. 絵と音をどうしたか、9. 今も直っていないもの。復帰の見た目を連番へ残し、原画採用と分離する判断の材料。

### 2026-10-08 [失敗] 森狼の低い予備原画候補を却下
- 出どころ: `docs/devlog.md` 2026-10-08、`docs/art/2026-10-08/spear-wolf-prep-rejected-v2.png` と全文プロンプト/台帳。低い構え・支持足は使えるが、両手槍軸とbody-v2画風が一致せずQA未配置。
- 記事のどこに使えそうか: 6. 壊れた箇所カタログ、8. 絵と音をどうしたか、9. 今も直っていないもの。新原画の採否を両手接続・画風・支持足で分ける材料。

### 2026-10-08 [仕組み] 森狼の低い復帰ルートをQA比較
- 出どころ: `docs/devlog.md` 2026-10-08、`tools/hero-gameplay-qa/2026-10-08/v5-wolf-low-recovery/`。既存の低い戻り候補を `HERO_WOLF_RECOVERY=low` で森狼へ割り当て、114描画・接触/足元/低FPS/取消・割込み/contact1回passed。新原画は採用せず、美術は未合格。
- 記事のどこに使えそうか: 6. 壊れた箇所カタログ、8. 絵と音をどうしたか、9. 今も直っていないもの。ルート分岐の機械確認と、実画像・支持足を別ゲートにする判断の材料。

### 2026-10-08 [失敗] 森狼の歩行復帰原画編集を却下
- 出どころ: `docs/devlog.md` 2026-10-08、`docs/art/2026-10-08/spear-wolf-recovery-rejected-v1.png` と全文プロンプト/台帳。接触候補から腕だけを編集する試行が胴体・脚まで変え、低い復帰シルエットを保持できなかった。QAへは未配置。
- 記事のどこに使えそうか: 6. 壊れた箇所カタログ、8. 絵と音をどうしたか、9. 今も直っていないもの。局所編集の失敗を採用候補と混ぜず、pose33/34/35を別制作する判断の材料。

### 2026-10-08 [絵と仕組み] 森狼専用の低い槍接触原画候補
- 出どころ: `docs/devlog.md` 2026-10-08、`docs/art/2026-10-08/spear-wolf-contact-v1.png` と全文プロンプト、隔離Unity `tools/hero-gameplay-qa/2026-10-08/v4-wolf-contact-rules/`。森狼の上胸/肩へ向く原画を専用接触へ接続し、114描画で横/縦差0・足元・低FPS・取消/割込み・contact1回passed。候補測定で、美術採用は未合格。
- 記事のどこに使えそうか: 6. 壊れた箇所カタログ、8. 絵と音をどうしたか、9. 今も直っていないもの。敵別接触原画と機械検査を分ける判断、支持足と歩行復帰を残す材料。

### 2026-10-08 [絵と仕組み] 敵の高さ別に槍キーを選ぶ候補
- 出どころ: `docs/devlog.md` 2026-10-08、隔離Unity `tools/hero-gameplay-qa/2026-10-08/v3-enemy-height-rules/`。スライム低、ゴブリン標準、森狼低接触/標準復帰と下胴/胸/中心アンカーをメモリ上だけで比較。114描画・contact1回・横接触・低FPS・取消/割込みpassed。アンカーはQA測定で、美術採用は未合格。
- 記事のどこに使えそうか: 6. 壊れた箇所カタログ、8. 絵と音をどうしたか、9. 今も直っていないもの。敵の高さでモーションを選ぶ判断と、機械接触と美術接触を分ける材料。

### 2026-09-29 [仕組み] ポーション3分岐を実BETで確認
- 出どころ: `docs/devlog.md` 2026-09-29、隔離Unity `tools/enemy-gameplay-qa/2026-09-29/v8-potion/`。回復・次の大攻撃・レア役の討伐確定をBET停止Evaluateで再現し、Small大攻撃はcontact1回後に敵hit、Rareはcontactなしで決着。
- 記事のどこに使えそうか: 3〜5. 仕組み①②③、8. 絵と音をどうしたか。結果の種類ごとに主人公contactの有無を分ける判断。

### 2026-09-29 [絵と音] エンゲージの強弱を帯の段階色で表示
- 出どころ: `docs/devlog.md` 2026-09-29、隔離Unity `tools/enemy-gameplay-qa/2026-09-29/v10-wave/`。状態別の帯色とPotionLarge予約文を実BET後に検査。
- 記事のどこに使えそうか: 8. 絵と音をどうしたか、9. 今も直っていないもの。キャラ絵を塗らずに強弱を読ませる判断。

### 2026-09-29 [決定/失敗] 20敵ロスターと画像生成429
- 出どころ: `docs/devlog.md` 2026-09-29、`Assets/Resources/Data/enemy_roster_p2.json`、`docs/art/2026-09-29/enemy-roster-concept-prompt.txt`。20件を一意IDで固定し、内蔵生成はHTTP 429 `usage_limit_reached`で原本未生成。
- 記事のどこに使えそうか: 6. 壊れた箇所カタログ、8. 絵と音をどうしたか、9. 今も直っていないもの。件数だけ増やさずアートゲートを設ける判断。

### 2026-09-29 [仕組み/失敗] Judge第一停止の先行被弾を除去
- 出どころ: `docs/devlog.md` 2026-09-29、隔離Unity `tools/enemy-gameplay-qa/2026-09-29/v6-judge/`。Judge第一停止でenemy=hitがcontactより先に出た失敗と、ready保持→pose27/contact1→defeatの再検証。
- 記事のどこに使えそうか: 6. 壊れた箇所カタログ、8. 絵と音をどうしたか。前触れ演出と決着演出を同じ被弾状態にしない判断。

## 追記の書式

新しいものを「未整理の材料」の先頭に足す。1件2行で足りる。

```
### YYYY-MM-DD [種別] 一言で
- 出どころ: devlog の日付 / ERRORS の番号 / 実測の条件
- 記事のどこに使えそうか:
```

種別と行き先。

| 種別 | 記事の行き先 |
|---|---|
| `失敗` | 6. 壊れた箇所カタログ |
| `仕組み` | 3〜5. 仕組み①②③ / 11. 効かなかったこと |
| `数字` | 各所 + 付録の出どころ表 |
| `決定` | 9. 今も直っていないもの / 各所 |
| `絵と音` | 8. 絵と音をどうしたか |

**数字は「いつ・どの条件で測ったか」ごと持ってくる。** 条件が違うと比較にならない
（記事の「何回まわせば足りるのか」がその実例）。

---

## 未整理の材料

### 2026-09-28 [仕組み] 長い待ち時間と、第二停止での斬撃
- 出どころ: [HERO_REEL_BRIDGE_P2.md](../HERO_REEL_BRIDGE_P2.md)、現行GameControllerの停止数処理、同日devlog、`tools/hero-prototype-qa/2026-09-28/v5-reels-final/`。
- 記事のどこに使えそうか: 見本の連続アニメをそのまま本編に入れられない理由、停止待ちの保持、武器変更予約、既存の順序を読み違えない検証。

### 2026-09-28 [絵と音] 状態検査が通っても攻撃が届いて見えない
- 出どころ: [ENEMY_PRESENTATION_P2.md](../ENEMY_PRESENTATION_P2.md)、同日devlog、敵原本と全送信プロンプト、`tools/hero-prototype-qa/2026-09-28/`の各版。
- 記事のどこに使えそうか: 3敵の予兆の違い、セル境界の修正、被弾を発火する時刻と実際の距離、検品画面と本編の区別。

### 2026-09-27 [仕組み] 同じ高さへの正規化がしゃがみを壊す
- 出どころ: [HERO_PRESENTATION_P2.md](../HERO_PRESENTATION_P2.md)の続行版、同日devlog、`tools/hero-prototype-qa/2026-09-27/v8-motion-final/`、追加素材台帳と送信プロンプト。
- 記事のどこに使えそうか: 足元と倍率の分離、両手の槍の軸、武器ごとの復帰、機械検査成功後に画像で見つけた撮影タイミングの誤り。

### 2026-09-27 [絵とモーション] キーポーズを生成しただけではアニメは完成しない
- 出どころ: [HERO_PRESENTATION_P2.md](../HERO_PRESENTATION_P2.md)、同日のdevlog、`tools/hero-prototype-qa/2026-09-27/v6-final/`。原本・送信プロンプト・alpha/セル境界検品・Unity動画を保存。
- 記事のどこに使えそうか: 武器を別に描く理由、生成シートの等分事故、時計と美術の検査の違い、本編交換前の見本作り。

### 2026-09-27 [実装と検品] 会話・GET・呪いの土台と、テスト自身の誤判定
- 出どころ: [ADVENTURE_PRESENTATION_P1.md](../ADVENTURE_PRESENTATION_P1.md)、devlog 2026-09-23〜27、`tools/adventure-redesign-qa/2026-09-26/p1-final/`。生成4アトラスと再制作仕様、32枚と実動作検証を保存。
- 記事のどこに使えそうか: 「かっこよさ」の前提となる情報階層、画像metaの罠、実フレームを待たないテストの誤判定、次AIへの実装/未検証の区別。

### 2026-09-23 [決定] 冒険の改良を、AIが実装できる工程と素材契約へ
- 出どころ: [devlogの2026-09-23冒険企画](../devlog.md)、[企画正本](../ADVENTURE_REDESIGN_PLAN_2026-09-23.md)。企画のみで新機能は未実装。
- 記事のどこに使えそうか: 曖昧な「かっこよく」を、20敵の台帳・小さな完成見本・モーション接続・検証条件へ具体化する方法。

### 2026-09-23 [UIと継続運用] 設定画面から共通品質基準へ
- 出どころ: devlog同日「設定画面と、言われる前に揃えるUI品質」、`AZURE_SETTINGS_DESIGN.md`、`AZURE_UI_QUALITY_STANDARD.md`。つまみ寸法・最大字幕・実呼び出し元・再発防止の案内を一緒に残す。

### 2026-09-14 [決定] 透過指示がRGBで返った背景を、実画素とUnity描画で確かめる
- 出どころ: devlog「2026-09-14 C-1を新画風の3層にしてUnityへ接続」。黒マット方式・12条件・102テスト・未検証範囲を記録。
- 記事のどこに使えそうか: 絵と音／生成画像の見た目と実際のファイル形式を区別する検品。

### 2026-11-03 [依頼] 敵・装備・背景の残り計画を完了
- 出どころ: `docs/note/MEMO.md` 2026-11-03
- 記事のどこに使えそうか: [dev404レビュー](../art/2026-11-03/ENEMY_UNMAPPED_ART_FALLBACK_REVIEW.md)と今後の敵/装備/背景制作記録。

（ここに追記する。反映したら下の表へ移す）

*なし。2026-09-29 に第4稿へ全部反映した（9/15〜29 の 15 件）。*

---

## 反映済み — 壊れた箇所カタログ（36件）

記事の番号と、出どころの対応表。番号を足すときは記事側の件数も直す
（タイトル / 目次 / 6章冒頭 / 7章の型 / 買う前に / 付録）。

| # | 中身 | 出どころ | 型 |
|---|---|---|---|
| ① | リプレイでエンバーが減っていた | PROJECT_STATE 7節 | A |
| ② | オートが AT のナビを見ず、11枚が3枚に | 同上 | A |
| ③ | 特化ゾーンが強すぎて AT が終わらない（1回3,669G） | 同上 | A |
| ④ | 画面更新の途中で例外 → ボーナス後に BET が押せない | 同上 / OBSERVATIONS O-015 | A |
| ⑤ | 札が動く背景に埋もれる | 同上 | B |
| ⑥ | アイコンが文字に重なる | 同上 | B |
| ⑦ | 枝の端に到達できない | 同上 | A |
| ⑧ | スライムが通常プレイで出ない（**未修正**） | PROJECT_STATE 6節 | A |
| ⑨ | ボーナス成立中なのに揃わない（4コマで実測 2%/G）→ 20コマへ | devlog 09-09 | A |
| ⑩ | 松明を入れたのに 25万G で一度も尽きなかった | OBSERVATIONS O-014 | A |
| ⑪ | AT と通常エンゲージが同時成立（100万G 中 558回 = 0.056%） | OBSERVATIONS O-012 | A |
| ⑫ | モンスター 3体が読み込みで 6体になった | OBSERVATIONS O-011 | A |
| ⑬ | 技術介入の失敗の 25% が制御由来だった（75% → 100%） | devlog 09-10 | A |
| ⑭ | 外部記事の引用を「本人の実測」として4箇所に書かれた | ERRORS E-A01 | E |
| ⑮ | ヒアドキュメントで長文が壊れる（3案件3回で R-006 へ昇格） | ERRORS E-006 / E-007 | C |
| ⑯ | スクリーンショット検証が3回続けて無効 | ERRORS E-004 | C |
| ⑰ | 会社PCでシステム設定に依存する自動化を設計 | ERRORS E-002(09-03) | C |
| ⑱ | 原因を、自分が最後に触った層だと決めつけた | ERRORS E-003 | D |
| ⑲ | 記録どおりに答えたが、実装では Lv が無効・エンバーが不在 | devlog 09-10 | D |
| ⑳ | 剣の柄頭が消えた（ループ変数 k が拡大率 k を上書き） | MATERIAL 09-10 | D |
| ㉑ | 青い塊をマントだと3回誤診（設定資料シートを描いていた） | MATERIAL 09-10 | D |
| ㉒ | 「見た目が悪い」の原因は後処理（192ドット・32色）だった | MATERIAL 09-10 | D |
| ㉓ | キャラ崩れの原因は4つ（符号逆・向き矛盾・種ランダム・外接枠正規化） | MATERIAL 09-10 | D |
| ㉔ | 自分で書いた UI ルールを翌日に自分で破った → 検算スクリプトへ | MATERIAL 09-10 / OBSERVATIONS O-016 | F |
| ㉕ | 枠の検算は通ったのに文字が2行はみ出した | MATERIAL 09-12 | B |
| ㉖ | ボーナス中ベル 11/8 枚で設定5・6の REG が終わらない（未修正） | MATERIAL 09-12 | A |
| ㉗ | 通しテストが拾った2件（ソウル未報告 1,252回 / 松明上限 344回） | MATERIAL 09-10 | A |
| ㉘ | 複数PCで Unity が .meta を勝手に書き換え同期が止まる | MATERIAL 09-10 | C |
| ㉙ | 生成PNGの meta が Cubemap 扱いで Texture2D として読めない | devlog 09-23〜27 | C |
| ㉚ | ジャッジ第一停止で敵が先に被弾し、主人公の攻撃より前に出た | devlog 09-29 | B |
| ㉛ | 敵の踏み込み始めに主人公が反応し、距離も遠かった（接触を85%へ） | devlog 09-28 | B |
| ㉜ | 不透明な高さで拡大率を決めたので、しゃがみだけ巨大化 | devlog 09-27 | A |
| ㉝ | 自動検査は通ったのに、撮れた画像が復帰中のコマだった | devlog 09-27 | B |
| ㉞ | 色にじみに見えたが alpha=0 の RGB。見かけで透過失敗と断定しない | devlog 09-27 | D |
| ㉟ | DontSave の見本が FindFirstObjectByType で見つからず二重生成 | devlog 09-27 | C |
| ㊱ | テストが実時間の経過前に判定し「GETが1件」と誤判定 | devlog 09-23〜27 | A |

型の内訳: A=15 / B=6 / C=6 / D=7 / E=1 / F=1。
⑳〜㉘は「そのあとの4日で増えたもの（9月11日〜14日）」、㉙〜㊱は「さらに2週間で増えたもの（9月15日〜29日）」の下にまとめた。

### 反映済み — 6章以外に入れた材料（第3稿）

| 材料 | 入れた場所 |
|---|---|
| 1,104,000通りの全数検証。違反340件 → 2コマ入れ替えで0件（devlog 09-08） | 5章「1,104,000通りを、全部調べた」+ 画像26 |
| リプレイでライフ回復 → 力尽きる頻度 1/1,278G → 1/3,303G（MATERIAL 09-10） | 5章「小さな変更が、全体を動かす」 |
| 設定資料3枚（ChatGPT）→ 基準の1枚（MATERIAL 09-10） | 8章「設定資料を、先に作る」+ 画像27 |
| 剣の向きだけ直らず、自前で1枚描いて回転（MATERIAL 09-10） | 8章「剣だけは、言うことを聞かなかった」 |
| 効果音4種を波形とピーク値で確認（MATERIAL 09-13） | 8章「音は、耳ではなく波形で確かめた」 |
| 画面幅を iPhone に。モック2枚から選択。STOP 廃止（MATERIAL 09-12） | 8章「画面の幅を、先に決める」+ 画像22 |
| 地図をアトリエ風に作り直し（commit 62eb93c） | 8章「地図を、3日で作り直した」+ 画像23 |
| タイトルの惹句を外す（commit 8f23bb2）。赤枠は本人の指示 | 8章「同じ日の2時間」+ 画像25 |
| 背景テイスト見直し中。3案不採用、参考3枚（MATERIAL 09-14） | 8章「背景のテイストは、まだ決まっていない」/ 9章 |
| 機械割 165.3%（設定1）/ 142.2%（設定5）2026-09-10 | 9章 / 付録 |
| 生成設定は強めるより矛盾を消す（MATERIAL 09-10） | 未使用。8章が長くなったので落とした |

### 反映済み — 6章以外に入れた材料（第4稿）

| 材料 | 入れた場所 |
|---|---|
| 技術介入の役ごとの決まり。スイカ・ベルの率は AI の仮置きと明記（devlog 09-24） | 5章「「仮置き」と書かせる」。実測 1.43% / 93% は付録にも |
| 透過が返らないので黒マットを描画時に抜く。曇りを足して全9天候（devlog 09-26） | 8章「AIに頼んでも、返ってこない形式がある」 |
| エフェクト見本13本。本体を触らず隔離した場所で描き出す（devlog 09-25） | 8章「見本を先に作って…」+ 画像34 |
| 設定画面の指摘から UI 品質基準を AI 自身に書かせた（devlog 09-23） | 8章「言わなくても、同じ質にしてもらう」+ 画像33 |
| エンゲージのターン制（devlog 09-21）/ 工房（同）/ 序章8場面（同） | 無料パート「今、どうなっているのか」 |
| 20敵のロスター。画像生成が上限で止まった（devlog 09-29） | 9章 / 7章の型E（返らなかったものを素材と呼ばなかった例） |
| 22,115行・94ファイル・総350コミット（9/29 実測） | 各所 + 付録 |


## 反映済み — 数字

条件つきのものは devlog の「検証の記録」が一次。ここは記事に出したものだけ。

| 数字 | 出どころ |
|---|---|
| 制作開始 2026-05-29 / 現在 2026-09-10 / 105日 | git log |
| ブラウザ版 26コミット、6/24〜8/17 の約2ヶ月が空白 | git log（`b13d544` が空白直前） |
| Unity 移植 9/7 開始、4日で 12,308行 / 47ファイル | git log + スクリプト実測 |
| 通しテスト 押し方4通り × 25万G = 100万G | PROJECT_STATE 4節 |
| 出玉の指標 132.6%（ナビ従） / 107.7%（順押し） | 設定1 / 200万G / 2026-09-09（devlog） |
| 20万G だと 128.8〜134.1% に揺れた | 同上。記事「何回まわせば足りるのか」で使用 |
| `game_config.json` 33キー / エディタ12タブ | PROJECT_STATE 3節 |
| キャラのポーズ 9種 | `tools/comfy/poses/index.json` |
| 実作業 平日1日2時間ほど / 触っていた期間 約50日 / 土日は2日で10時間ほど | 本人の実感値（2026-09-10, 09-14）。時間の記録は無い |
| 9/11〜14 のコミット149本（土日50本）/ 総202本 / 18,283行・80ファイル | git log + スクリプト実測（2026-09-14） |

**記事に書かなかったもの**

- 費用: 記録なし。「測っていません。次に作るときは測ります」と本文に明記
- AT実測（1回208G / +512枚 / 純増+2.46枚/G）: 調整前の値なので使わなかった
- リール制御 1,104,000パターン / メモ化で10分→6秒 / JS 5,766行:
  非エンジニア読者には効かないと判断して落とした。**Unity 版の画面が入ったら復活させてもいい**

## 反映済み — 仕組み・絵と音・決定

| 中身 | 出どころ |
|---|---|
| 指示書1枚（9節構成）。応答15行 / 依頼箇所だけ変更 / 数字はテーブル | CLAUDE.md |
| 複数PCは1行 import で実体を共有 | setup/SETUP.md |
| 入口1枚＋書庫。observations → knowledge → rules の3段昇格 | BRAIN.md |
| 完成条件（acceptance）と証拠（evidence） | RULES R-004 |
| 検品役を別に立てる。実装させない | `.claude/agents/inspector` |
| コミット前の3点検証 | PROJECT_STATE 4節 |
| 基準画像＋姿勢の骨格でキャラを保つ。ドット化は Python 側 | devlog 09-10 |
| UI アイコンは距離関数で手続き描画 / 効果音は合成 | PROJECT_STATE 2節 |
| note の方針（B+C混合 / 非エンジニア / 980〜1,500円 / 完成と同時公開） | 2026-09-10 本人 |
| 外部知能の中身は載せる。公開前に最終調整 | 2026-09-10 本人 |

- 2026-09-28: 初回コマの合図欠落と、頭の後ろへ消える剣。時計の合格後もUnityで姿勢を見直す必要があった。
  出どころ: [devlog](../devlog.md)「サリアの補助動作と音の試作」、[再現仕様と証拠](../HERO_SUPPORT_P2.md)。

- 2026-09-28: 見本から本編へ移す際、既存FXの親と音の所有者を残すことで、表示だけを置き換えた。実停止順と保存再読込まで検査。
  出どころ: [devlog](../devlog.md)「新サリアの本編接続」、[接続契約と検査](../HERO_GAMEPLAY_P2.md)。

- 2026-09-28: 会話窓の寿命と実表示を混同すると、操作中のキャラが止まる。テスト成功でも撮影が暗転中なら目視で見つけて検証を直す。
  出どころ: [devlog](../devlog.md)「OP逃走の停止修正と帰還検証」、[条件と証拠](../HERO_GAMEPLAY_P2.md)。

- 2026-09-28: 動作見本を本編へ移す際は、表示の寿命と結果の所有を分ける。機械検査成功後に残る戦闘帯を目視で発見した。
  出どころ: [devlog](../devlog.md)「敵2種の本編接続」、[接続・失敗・検証](../ENEMY_GAMEPLAY_P2.md)。

- 2026-09-28: 見本の動作が正しくても、実BETでは通常役と戦闘結果が競合する。入力から結果まで通して初めて二重演出とナビ例外を発見。
  出どころ: [devlog](../devlog.md)「実BETの戦闘動作競合とナビ例外」、[条件・動画・残課題](../ENEMY_GAMEPLAY_P2.md)。

- 2026-09-28: 動く敵の視認性は枠の配置だけでなく表示時刻でも改善できる。非表示中の文字送りと読了待ちを止め、会話の種類ごとに優先順位を保つ。
  出どころ: [devlog](../devlog.md)「戦闘中の説明枠を待機」、[動作と再開の検証](../ENEMY_GAMEPLAY_P2.md)。

- 2026-09-29: 攻撃の見栄えは固定秒数ではなく、剣/槍原画に記録したcontactイベントを基準にすると、入力負荷や武器差があっても敵の被弾を先行させずに済む。取消・敵交替も世代を分けて扱う。
  出どころ: [devlog](../devlog.md)「剣・槍のcontact同期」、[接触順の検証](../ENEMY_GAMEPLAY_P2.md)。

- 2026-09-29: 接触イベントを観測するだけでは、書出し時点の姿勢を確認できない。contact直後を別撮影し、順序の機械検査と見た目の静止画を分けて残す。
  出どころ: [devlog](../devlog.md)「接触フレームの描画証拠」、[v3の条件](../ENEMY_GAMEPLAY_P2.md)。

### 2026-10-03: 見た目の動作と接触待機の寿命を揃える（dev 344）

攻撃中の「歩行へ戻る予約」は現在の攻撃を消していないのに、外側の待機処理だけ取消していた。要求を全部リセットとみなさず、開始/割込み/復帰予約を区別する。正本: [HERO_GAMEPLAY_P2.md](../HERO_GAMEPLAY_P2.md)。隔離Play Mode1件と2解像度54枚を確認。最終アニメ美術の合格とは分ける。自動継続の実施記録であり、heartbeatを本人の新発言として引用しない。

- 2026-10-03 未整理: 歩行3案を生成しても左右交替が不成立。握りを揃えると剣が頭へ重なることをUnity比較で発見し採用保留（dev 345）。
  出どころ: [制作ログ](../devlog.md)、[原本・失敗理由・次の手順](../art/2026-10-03/WALK_REVIEW.md)。記事本体へ未反映。

- 2026-10-03 未整理: 全身より後ろの武器が頭から出る問題を、元絵の手だけを前へ重ねて修正。JsonUtilityの空オブジェクトで試験が誤判定した経緯も保存（dev 346）。
  出どころ: [制作ログ](../devlog.md)、[握りレビュー](../art/2026-10-03/GRIP_LAYER_REVIEW.md)。記事本体へ未反映。

- 2026-10-03 未整理: 8枚一括生成で支持脚が反復。2枚の接地を作り、片側の下半身だけ直すと左右を区別できた。passingは依然保留（dev 347）。
  出どころ: [制作ログ](../devlog.md)、[左右接地レビュー](../art/2026-10-03/WALK_KEYS_REVIEW.md)。記事本体へ未反映。

- 2026-10-03 未整理: 通過姿勢をペア同時ではなく単体→下半身だけ編集の順で作り、左右支持脚が読める候補を得た。4キーの歩行から攻撃復帰まで隔離再生（dev 348）。
  出どころ: [制作ログ](../devlog.md)、[通過姿勢レビュー](../art/2026-10-03/WALK_BLOCKING_REVIEW.md)。記事本体へ未反映。

- 2026-10-04 未整理: 単体中割り6案から4候補を8姿勢見本に追加。生成時の支持脚名が実際の腿の重なりと一致しない例を記録し、機械passedと美術不合格を分離（dev 349）。
  出どころ: [制作ログ](../devlog.md)、[8姿勢レビュー](../art/2026-10-03/WALK_EIGHT_KEY_REVIEW.md)。記事本体へ未反映。

- 2026-10-04 未整理: 接地と歩幅を2枚だけ修正し、画像の固定領域測定とUnity描画で前案との差を確認。局所改善と歩行全体の完成は区別（dev 350）。
  出どころ: [制作ログ](../devlog.md)、[足運び修正レビュー](../art/2026-10-04/WALK_FOOT_REFINEMENT_REVIEW.md)。記事本体へ未反映。

- 2026-10-04 未整理: 歩行から構えの原画を派生させ握りの跳びを減らす。同番号の素材差替えでは描画キャッシュが残り、実Texture照合が必要だった（dev 351）。
  出どころ: [制作ログ](../devlog.md)、[構え接続レビュー](../art/2026-10-04/WALK_READY_MATCH_REVIEW.md)。記事本体へ未反映。

- 2026-10-04 未整理: 剣の入口/出口だけでは中盤の体格差が残るため8姿勢を同系列化。機械passed後の剣先見切れを実画像で発見・修正し、回帰2件でcontactを保持（dev 352）。
  出どころ: [制作ログ](../devlog.md)、[剣8姿勢レビュー](../art/2026-10-04/SWORD_CYCLE_REVIEW.md)。記事本体へ未反映。

- 2026-10-04 未整理: 剣の戻りを前回候補と直接比較し、最大移動と次区間への分散を両方記録。量産前に敵20件の独自台帳と企画の16＋4構成の食い違いを解消（dev 353）。
  出どころ: [制作ログ](../devlog.md)、[戻りレビュー](../art/2026-10-04/SWORD_RETURN_REVIEW.md)、[敵台帳整合](../art/2026-10-04/ENEMY_ROSTER_RECONCILIATION.md)。記事本体へ未反映。

- 2026-10-04 未整理: 槍の両手は直線一致だけでなく柄の範囲も検査。生成の位置ずれは座標変更だけで済ませず、中割りを足して接触時刻を保持した（dev 354）。
  出どころ: [制作ログ](../devlog.md)、[槍の原本・失敗・再現手順](../art/2026-10-04/SPEAR_CYCLE_REVIEW.md)。記事本体へ未反映。

- 2026-10-07 未整理: contactイベント合格でも武器が届くとは限らない。横差を解消した比較から低い敵への高さと移動の速さを別の未完了として残した（dev 355）。
  出どころ: [制作ログ](../devlog.md)、[接触距離レビューと失敗](../art/2026-10-07/APPROACH_REVIEW.md)。記事本体へ未反映。

- 2026-10-07 未整理: 接近を攻撃時計から分け、低い突き原画で高さを改善。一方、接近時間の追加でGETと接触が重なり、本編採用前の別課題を記録した（dev 356）。
  出どころ: [制作ログ](../devlog.md)、[原画・検証・未完了](../art/2026-10-07/TIMED_APPROACH_LOW_THRUST_REVIEW.md)。記事本体へ未反映。

- 2026-10-08: 攻撃が見えるまでGETを待つ。表示時計の停止とBET記憶を両立した。
  出どころ: [devlog dev357](../devlog.md)、[実描画/失敗/検証正本](../art/2026-10-07/COMBAT_REWARD_ORDER_REVIEW.md)。未反映。
- 2026-10-08: 低い槍突きの接触前と戻り始めを別キーにすると、接触時の低さを保ったまま復帰を見せられる。まだ候補であり、支持足と敵高さ別選択は未合格。
  出どころ: [dev358レビュー](../art/2026-10-08/LOW_SPEAR_TRANSITION_REVIEW.md)、[AI引き継ぎ](../AI_HANDOFF_HISTORY.md)。未反映。

- 2026-10-18: 戦闘帯の半透明背景を不透明な紺/金線/状態色アクセントへ改修。7状態/2解像度と実BETを隔離Unityで検証し、attempt11の古い色assert失敗も記録。
  出どころ: [dev397レビュー](../art/2026-10-18/COMBAT_BAND_OPAQUE_SURFACE_REVIEW.md)、[制作ログ](../devlog.md)、[引き継ぎ履歴](../AI_HANDOFF_HISTORY.md)。画面全体の最終美術/実機/聴感は未確認。
### 2026-10-19 [情報カード/不透明面] 戦闘帯背面の敵HP表示
- 出どころ: `docs/devlog.md` dev 398、[COMBAT_HP_OPAQUE_SURFACE_REVIEW.md](../art/2026-10-19/COMBAT_HP_OPAQUE_SURFACE_REVIEW.md)、隔離Unity結果。実描画の拡大で半透明HPカード越しにスクロール帯の文字が見えたため、面を完全不透明にし、状態と解像度ごとにalpha/兄弟順を検査する。
- 記事のどこに使えそうか: 8. UI重なりの実画像QA。既存機械テストが通ったことと、細部の拡大目視で初めて見つかる品質問題、全体/実機承認を区別する。

### 2026-10-19 [情報カード/可読性] 敵HPを名前・値・状態の3段へ
- 出どころ: `docs/devlog.md` dev 399、[COMBAT_HP_CARD_TEXT_FIT_REVIEW.md](../art/2026-10-19/COMBAT_HP_CARD_TEXT_FIT_REVIEW.md)、隔離Unity attempt04。長い情報行を1行へ詰めず、2解像度で各段がカード境界に収まることを検査。JUDGE状態の回帰fixture期待値の失敗も隠さず残す。
- 記事のどこに使えそうか: 8. UI情報の階層と幅設計。機能・実戦リール検査済みと実機/全敵長文/最終美術の未確認を分けて説明する。
### 2026-10-20 [QA証拠の対応] 歩行から攻撃へのcapture番号を修正
- 出どころ: [devlog.md dev 400](../devlog.md)、[隔離Unityの視覚記録](../art/2026-10-20/HERO_WALK_ATTACK_HANDOFF_VISUAL_REVIEW.md)、[attempt04 QA](../../tools/hero-gameplay-qa/2026-10-20/runtime-approach-attempt04/)。Step後の経過時間と0始まりcapture番号を区別し、walk18=042、attack26=043へ訂正。QAは実PNG差分をassert。
- 記事のどこに使えそうか: 8章の「機械的な接続検査と、人が見る姿勢の評価は別」。一時出力の競合回避と、証拠リンクの誤りをテストへ戻す事例。


## 2026-11-03 内蔵生成の再開

本人原文: MEMO.md。同日のコウモリ本編接続・灯蛾/5武器原本・検証範囲は[制作記録](../art/2026-11-03/BAT_RUNTIME_AND_ASSET_PRODUCTION_REVIEW.md)。原画生成と全計画完成を区別する。

- 2026-11-05: 20敵と7武器の展開。時間切れの合格JSONと、機械検査で見つからなかった槌の向きを修正。
  出どころ: [devlog](../devlog.md)、[制作・検証正本](../art/2026-11-05/ROSTER_WEAPONS_ENVIRONMENT_REVIEW.md)。記事未反映。


### 2026-11-06 統合回帰と背景展開

出典: 本人の継続依頼は[MEMO](MEMO.md)、実装・失敗修正・検証の正本は[制作記録](../art/2026-11-05/ROSTER_WEAPONS_ENVIRONMENT_REVIEW.md)と[devlog](../devlog.md)。機械検査と美術完成を区別する。


- 2026-11-06: 背景9地点の本編接続と他AIへ引き渡す境界。D-3樹冠切れ、透明余白と地面の馴染み、機械成功と美術残件を記録。
  出どころ: [devlog](../devlog.md)、[引き継ぎ正本](../art/2026-11-06/AGENT_HANDOFF.md)。記事未反映。
