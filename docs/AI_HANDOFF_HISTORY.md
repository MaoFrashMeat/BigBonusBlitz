# BigBonusBlitz AI 引き継ぎ履歴

### 2026-10-19 HPカードの3行テキストとカード境界を検証（dev 399）

- 狩猟/エンゲージ中の敵名・HP・残りG/SET状態を3行へ分割し、カードを縦へ拡張。状態値・ダメージ・報酬には触れない。
- attempt04隔離Unityで7状態/2解像度、3行データ・カード内寸法・表示順と透過、実BET回帰1件をPassed。実戦リールはHPゲージとHP65/100が一致し、敵名とSET/ターンも表示。
- attempt03のJUDGE fixtureテストでSET文字を期待して1回失敗。JUDGE状態で正しい値を照合するよう直して再検証。これは製品不具合ではない。
- harness/Core/Runtime/Tests/check_layout/diff-check Passed。隔離専用でsave未接触、実機/配布build/最終美術は未承認。[レビュー](art/2026-10-19/COMBAT_HP_CARD_TEXT_FIT_REVIEW.md)

### 2026-10-19 戦闘帯がHP表示の後ろに透ける問題を修正（dev 398）

- attempt12画面の拡大で、敵HPカード背後に帯文字が残ることを確認。カード背景alpha 0.97を1へ変更し、7状態×2解像度で背景alphaと兄弟順をassertするよう検査を更新。
- 今回の隔離Unityで強弱7状態/14枚、接近158枚、攻撃sequence、実BET EditMode 1件、17 sample、source SHA256 manifest一致をPASS。両解像度のHPカード拡大で文字残りが消えた。`docs/check_layout.py`、`tools/verify/run.py`、`git diff --check`もPASS。
- 本番save/抽選/報酬/遭遇/数値/画像は不変。最終美術/実機/聴感/配布buildは未承認。コミット/push/公開なし。[レビュー](art/2026-10-19/COMBAT_HP_OPAQUE_SURFACE_REVIEW.md)

### 2026-10-18 戦闘帯を紺/金の情報面へ（dev 397）

- 画面上の半透明帯が背景を透かして文字や主役と競合したため、不透明な濃紺面/細い金線/左の状態色へ変更。採用済みWeb版スクロール/文/速度は維持し、新規生成画像なし。
- attempt11は専用描画検査後、実BET回帰が旧Bg tintを状態色として読むassertで失敗。fixtureをStateAccentへ追従させ、attempt12で接近/攻撃/7状態、2解像度、実BET1 scenario、manifest、17 sample、機械割検査までPassed。
- 白文と背景の分離を目視。ルート札/敵HPの背後を走る全体構成は未完了、最終美術/実機/聴感/配布未確認。[レビュー](art/2026-10-18/COMBAT_BAND_OPAQUE_SURFACE_REVIEW.md)。抽選/HP/報酬/遭遇/save不変、コミット/push/公開なし。

### 2026-10-18 戦闘ピークの帯表示を同フレームで修正（dev 396）

- Setup/chargeは上下2本を維持、敵攻撃/英雄攻撃/Potion/Judgeは上1本にして足元を空ける候補を追加。旧帯を破棄前に非表示化し、同フレーム置換時の残像を防止。
- attempt09は参照フィールドだけを検査して古い帯の描画を見逃した。視覚上の不合格を記録し、attempt10で実active帯本数と7状態/14枚を確認。実BET/接近/time summary/source hashと検証ハーネスもPassed。
- 上段/全画面の最終美術、実機/聴感/配布buildは未承認。save/抽選/報酬/遭遇は不変。[レビュー](art/2026-10-18/COMBAT_INTENSITY_PEAK_STRIP_REVIEW.md)

### 2026-10-18 BET攻撃フェーズとcue境界を再検査（dev 395）

- attempt08隔離Unityで剣/槍のf1/f2全保持sample、anticipation/swing/contactの段階と1回性、walk復帰を確認。攻撃135枚、接近158枚、EditMode実BET1 scenario、source manifest一致Passed。verify harnessのcompile/100万GもPassed。
- QA assertionのみ。槍pose32を変えず、素材意図は未決定。抽選/HP/報酬/セーブ/実時間/最終美術は変更・承認なし。[レビュー](art/2026-10-18/HERO_ATTACK_SEQUENCE_PHASE_GUARD_REVIEW.md)

### 2026-10-18 BET通常攻撃の全段を接続計測（dev 394）

- attempt06隔離Unityで剣/槍のf1→f2→f3/4→contact→walk復帰を120Hz、CSV、連番静止画で記録。各原画pose通過、接触cue一回、最終walkを確認。EditMode実BET回帰1件とapproach比較/source hashもPassed。
- 目視では槍のf1終端とf2が同一pose32で、第一停止に見た目上の変化なし。新原画は作らず未決定として残す。attempt04のQA失敗は槍poseの期待値を剣と共有したテスト誤りで、attempt06で修正。
- QAは通常fixture制御の隔離確認。プレイヤー待ち時間/実機入力/美術/聴感/配布buildの承認ではない。戦闘・抽選・報酬・saveは不変。[レビュー](art/2026-10-18/HERO_ATTACK_SEQUENCE_REVIEW.md)

### 2026-10-18 接近歩行から攻撃姿勢へのhandoffを測定（dev 393）

- 120Hz隔離Unity traceで接近終端0.3583s pose18から0.3667s剣26/槍33への切替を確認。切替時rootはほぼ固定し、contactは原画イベント時刻に一度だけ発火。
- attempt03 接近158枚/実BET/照準0px/復帰/EditMode 1件/source manifest/検証ハーネスPassed。初回の誤ったcontact窓assertを修正し、QAの失敗経緯もレビューへ記録。
- これは既存の一段pose切替の計測であり、中割り/支持足/最終美術・実機・配布承認ではない。[レビュー](art/2026-10-18/HERO_APPROACH_WALK_CADENCE_REVIEW.md)

### 2026-10-18 攻撃接近の歩行再生速度を原画に合わせる（dev 392）

- 接近中に8歩行コマを0.36秒へ均等圧縮していたため、0.11秒ずつの原画タイミングで表示するように変更。
- attempt22の隔離Unityは剣/槍時間ゲート、126姿勢比較、EditMode実BET1 scenario、17fixture集計、本編/隔離manifestをPassed。戦闘ロジック/セーブ不変。
- 接近→攻撃の中割りと支持足、実機/入力/聴感/配布ビルド/最終美術は未承認。[レビュー](art/2026-10-18/HERO_APPROACH_WALK_CADENCE_REVIEW.md)

### 2026-10-16 BET結果が終わったら戦闘帯を隠す（dev 391）

- 各BETのcontact/敵反応中は帯を表示し、`EngageStepRoutine`後に隠す。次BETは現在状態の帯を作ってから結果coroutineを開始。同期的な初回contactと、同一敵への古い結果coroutine競合の両方を防止。
- attempt21は隔離EditMode 1件/接近126枚/source manifest/fixture 17spinタイムライン集計Passed。`py -3 tools/verify/run.py` compileと収支ハーネスPassed。attempt16 strike欠落を検知、attempt17〜20のsynthetic cancellation誤検知を判別し、実BETのassertion範囲を修正。
- 目視は帯の消失/再表示だけ確認。結果文とGETの重なり、全体レイアウト/実入力/音/実機/配布ビルドは未承認。抽選/HP/報酬/セーブ不変。P2全体未完了、push未実施。[レビュー](art/2026-10-16/COMBAT_BAND_RESULT_HANDOFF_REVIEW.md)

### 2026-10-16 実BETの状態タイムラインを追加（dev 390）

- P3の測定準備として、fixture-driven BETごとにBET/各停止/Evaluate後のhero/enemy状態、contact cue、HP/報酬、Unity時刻、capture FPSを記録するCSVと集計JSONを追加。
- attempt15は17サンプル、EditMode回帰1件と126枚接近比較Passed、本編/隔離hash一致。QA runnerがCSV解析とsummary JSON生成の検査まで行う。ゲーム本編/抽選/HP/報酬/セーブ動作は変更しない。
- タイムラインは実マウス入力やプレイヤーの待ち時間ではなく、Capture FPS固定も含む試験fixture。波の採用秒数や聴感/実機評価と混同しない。GET/戦闘帯の同時表示の読みやすさも別課題として残る。失敗したattempt11〜13は保存。[レビュー](art/2026-10-16/COMBAT_TIMING_REEL_REVIEW.md)

### 2026-10-16 剣攻撃の踏み込みを実リールで確認（dev 389）

- 隔離の実BET/Stop/Evaluateで剣とどめ/槍通常攻撃の踏み込み、各武器先端X-敵中心X=0px、各contact1回、recover後rest位置へ復帰を検証。
- attempt10でsource/isolated SHA256一致とEditMode結果を保存。セーブ/抽選/報酬/Encounterの変更なし。縦接触や美術/実機/配布ビルドは未承認。[レビュー](art/2026-10-16/HERO_SWORD_APPROACH_REEL_REVIEW.md)

### 2026-10-16 攻撃前の踏み込みを本編で有効化（dev 388）

- 本編の踏み込み設定を有効化し上限600px/0.36秒。既存の敵Transform追跡・recover復帰を利用。source poseに合わない候補接触高さは導入しなかった。
- compile Passed、隔離Unity126枚の接触/足元/低FPS/取消比較 Passed、実敵槍リール1件Passed、本編境界ガードPassed。QA専用セーブのみで `saveDataTouched=false`。
- 機能は接続したが槍スライムの接触位置は頭上寄りで美術未承認。slime/goblinのみruntime、森狼preview-only。戦闘数値/遭遇/抽選/報酬/セーブ不変。実機/配布/聴感/最終美術は未確認。[レビュー](art/2026-10-16/HERO_ATTACK_APPROACH_INTEGRATION_REVIEW.md)

### 2026-10-16 森狼の槍接触後復帰を低姿勢/標準姿勢で比較（dev 387）

- 同一候補で隔離Unityを2回実行し、low/standard各126枚。接触・foot-plane・低FPS・取消/割込み・アンカーのvalidationが両方Passed。
- 目視では低姿勢の復帰が四足の森狼に対する接触軸を保ち、標準は立ち上がりが早い。候補評価であり美術承認ではない。
- 足滑りは複数時点の歩行復帰連番が必要。`approach.enabled=false`、`finalArtApproved=false`、本編敵/遭遇/抽選/報酬/セーブ不変を維持。[レビュー](art/2026-10-16/WOLF_RECOVERY_STANDARD_LOW_COMPARE_REVIEW.md)

### 2026-10-14 戦闘強弱帯の可読性調整（dev 386）

- 隔離画像の目視で反復文字がステージ札/敵HP表示を横切る問題を確認し、帯を28px/文字13/幅0.98倍/傾き2度へ縮小、上段を90px内側へ、HPカードを濃くした。
- 7状態/14枚/2解像度の隔離Unity検査Passed、色/文対応・上下一致・セーブ不変。下段帯は主人公の背面を通る。最終美術/実機/聴感/配布ビルドは未承認。[レビュー](art/2026-10-14/COMBAT_INTENSITY_READABILITY_REVIEW.md)

### 2026-10-14 敵20種ロスター再検査（dev 385）

- 読み取り専用ガードで20 ID/通常16/ボス4/既存6遭遇ID/hash不変をPassed。artStatus existing 3 / preview-only 1 / planned 16。
- `allTwentyPlayable=false`、`finalArtApproved=false`。敵絵/遭遇/報酬/セーブには触れていない。[レビュー](art/2026-10-14/ENEMY_ROSTER_REVALIDATION_REVIEW.md)

### 2026-10-14 敵3種・剣槍動作見本の隔離再描画（dev 384）

- `shop-qa`でHeroPrototypeValidationを再実行。85静止画/192補助/144リール/288敵フレーム、24敵ポーズ/12クリップ、低FPS/割込み/決着/停止/槍両手軸/武器別復帰/歩行位相がPassed。
- 見本の隔離再描画のみ。森狼preview-only、`productionIntegrated=false`、聴感/実BET/実機/URP/配布ビルド/最終美術は未承認。遭遇/抽選/報酬/セーブ不変。[レビュー](art/2026-10-14/ENEMY_THREE_MOTION_REFRESH_REVIEW.md)

### 2026-10-14 P2本編境界の再確認（dev 383）

- 読み取り専用ガード11項目を再実行し、接近無効、敵runtime/preview、武器7種、20敵と既存遭遇ID、カタログ参照なしをPassed。`saveDataTouched=false`。
- `productionIntegrated=false`、20敵/7武器全実装false、`finalArtApproved=false`を維持。本編データ・原画・runtime変更なし。[レビュー](art/2026-10-14/PRODUCTION_BOUNDARY_REFRESH_REVIEW.md)

### 2026-10-10 P2本編境界再監査（dev 382）

- 読み取り専用のproduction boundary guardを再実行し、主人公接近/敵runtime/森狼preview/武器7系統/20敵/既存6遭遇ID/カタログ参照をPassed。`saveDataTouched=false`を維持。
- `productionIntegrated=false`、`allTwentyPlayable=false`、`allSevenPlayable=false`、`finalArtApproved=false`。本編遭遇・抽選・報酬・セーブ・runtime・原画は変更なし。[レビュー](art/2026-10-10/PRODUCTION_BOUNDARY_RERUN_REVIEW.md)

### 2026-10-10 EditMode隔離再検証（dev 381）

- 本番とは別の一時Unityで`run.ps1 -Tests`を完走。129件中123件Passed、失敗0、Ignore 6件。`result="Skipped:Ignored"`は既知のIgnoreによるもの。
- `editmode.xml`/`tests.log`を`tools/environment-qa/2026-10-11/v10-editmode-refresh/`へ保存。実機/URP/配布ビルド/聴感/最終美術/本編統合は未承認で、セーブ/抽選/報酬/遭遇は不変。[レビュー](art/2026-10-10/EDITMODE_REFRESH_REVIEW.md)

### 2026-10-10 背景ソース・光影の隔離再実描画（dev 380）

- 本番とは別の一時Unityで`AdventureEnvironmentValidation.Run`を再実行。`validation.json`は30地点/3層、時間帯4、天候9、森12条件、1280x720/2556x1179、屋内天候、実ゲーム統合、`saveDataTouched=false`でPassed。
- 森昼晴れ/夜雨、C-1ゲーム画面、継ぎ目前後を`tools/environment-qa/2026-10-11/v9-background-refresh/`へ保存し、夜雨の光影とUIを目視確認。ホスト日付跨ぎを記録。
- 再実描画はソース機械ゲートであり、本編統合/URP/実機/配布ビルド/負荷/サウンド/最終美術は未承認。次の実装へ進む前に境界を再利用する。[レビュー](art/2026-10-10/BACKGROUND_REFRESH_REVIEW.md)

### 2026-10-10 P2本編境界ガード（dev 379）

- 読み取り専用で主人公接近、敵3種runtime/preview、武器7系統、20敵カタログ、既存6遭遇ID、武器カタログのC#参照を同時に監査しPassed。
- `approach.enabled=false`、runtime enemy=`slime/goblin`、森狼preview-only、existing/planned境界、`saveDataTouched=false`を維持。セーブ/抽選/報酬/遭遇は変更なし。
- このゲートは本編統合や美術承認ではない。次の原画・動作・実機/配布ビルド判断前に再利用する。[レビュー](art/2026-10-10/PRODUCTION_BOUNDARY_GUARD_REVIEW.md)

### 2026-10-10 敵3種動作見本・剣槍の隔離再実描画（dev 378）

- 隔離 `shop-qa` で `HeroPrototypeValidation.Run` を再実行。validation Passed、敵288フレーム/24ポーズ/12クリップ、剣/槍、低FPS/割込み/決着/槍両手軸/武器別復帰/歩行位相を再確認。
- 森狼接触とスライム槍攻撃の1280静止画を目視し、C-1背景/紺金UIの可読性を確認。出力はホスト日付跨ぎのため `tools/hero-prototype-qa/2026-10-11/v9-enemy-motion-refresh/`。
- 再描画は見本の機械/目視ゲートであり、`productionIntegrated=false`、`audioListeningVerified=false`、`finalArtApproved=false`を維持。遭遇・抽選・報酬・セーブは不変。[レビュー](art/2026-10-10/ENEMY_THREE_MOTION_REFRESH_REVIEW.md)

### 2026-10-10 敵・背景同場面証拠マトリクスガード（dev 377）

- 既存のv10/v8/dev371/dev376 JSONと画像を編集せず再照合し、森狼/スライム/ゴブリン、剣/槍、C-1背景を同じ行列へ固定。
- 126枚の敵背景接触、85枚の敵動作、30地点/12森条件、昼晴れ/夜雨、両ゲーム解像度をPassed。`saveDataTouched=false`、`productionIntegrated=false`、`finalArtApproved=false`。
- 新規レンダリングでも原画承認でもない。次はidentity/肩手/支持足、URP/実機/配布ビルドと専用美術の判断を分離して進める。[レビュー](art/2026-10-10/SCENE_MATRIX_GUARD_REVIEW.md)

### 2026-10-10 背景・光影プローブ差分ガード（dev 376）

- dev371の実画像を読み取り、C-1/森昼晴れ/夜雨と30地点・森12条件を再照合。平均RGB差56.128、屋内天候/セーブ不変Passed。
- 光影差分のみで、URP/端末/GPU/配布ビルド/最終美術は未承認。[レビュー](art/2026-10-10/BACKGROUND_LIGHT_SHADOW_GUARD_REVIEW.md)

### 2026-10-10 敵3種ソース/本編境界ガード（dev 375）

- 敵3種×6モーションの定義を照合。runtime有効はslime/goblin、森狼preview-only、全3体のdedicatedArtApproved=false。
- ソース境界ゲートであり、美術/実機/URP/配布ビルド/本編統合は未完了。[レビュー](art/2026-10-10/ENEMY_THREE_SOURCE_GUARD_REVIEW.md)

### 2026-10-10 敵3種動作見本・剣槍接続再検証（dev 374）

- 現行隔離Unityで敵3種を288連番/24ポーズ/12クリップ再描画。敵接触、割込み取消、決着保持、停止、低FPS、槍両手軸、武器予約をPassed。
- `productionIntegrated=false`、聴感/実機/配布ビルド/最終美術は未承認。[レビュー](art/2026-10-10/ENEMY_THREE_MOTION_RERUN_REVIEW.md)

### 2026-10-10 森狼・剣槍同一identity支持足/握りゲート（dev 373）

- v11読み取り専用ガードが重要ポーズ、意図したループ重複、支持アンカー、接地値29〜36、握り座標、剣/槍カタログをPassed。
- これは姿勢数値契約であり、美術identity/肩手接続/実機/URP/配布ビルドを承認しない。`mainEnabled=false`、`finalArtApproved=false`。[レビュー](art/2026-10-10/WOLF_IDENTITY_SUPPORT_GUARD_REVIEW.md)

### 2026-10-10 森狼・背景・剣槍同場面比較ゲート（dev 372）

- v9失敗: 本編候補JSONにQAアトラス名がなく `spear-low-start-v1-qa` の矩形検証前に停止。ログを保持。
- v10検証: 既存v1低槍候補でC-1背景、森狼、剣/槍を126枚・2解像度再描画。水平接触、復帰、割り込み、支持足、セーブ不変Passed。
- `mainEnabled=false`、`finalArtApproved=false`。次は同一identityの肩/手/支持足と実機/URP/配布ビルドを別ゲートで確認。[レビュー](art/2026-10-10/WOLF_BACKGROUND_WEAPON_REVIEW.md)

### 2026-10-10 背景30地点・光影ソース境界ゲート（dev 371）

- `VERIFIED`: 現行Hubパスを解決して隔離Unityを再実行。30地点/C-1三層/森12条件（時間4・天候9）、屋内天候ガード、継ぎ目、セーブ不変を再照合。
- `GATE`: URP、端末/GPU、配布ビルド、最終美術は未承認。`finalArtApproved=false`。[レビュー](art/2026-10-10/BACKGROUND_SOURCE_GUARD_REVIEW.md)。次は森狼支持足と剣/槍を同じ背景場面で比較。

### 2026-10-10 装備外見7系統の設計ゲート（dev 370）

- `VERIFIED`: 武器ベース7種のID/順序を `weapon_visuals.json` へ固定。既存剣/槍のアトラス範囲・握り座標・高さ一致、planned 5種は未承認。
- `GATE`: カタログはランタイム未接続（C#参照0件）。`allSevenPlayable=false`、`finalArtApproved=false`。装備効果/抽選/セーブ/途中切替は不変。[レビュー](art/2026-10-10/WEAPON_VISUAL_CATALOG_GUARD_REVIEW.md)。次は剣/槍支持足見本、planned武器は原画・動作契約後。

### 2026-10-10 20敵カタログ/遭遇表の境界ゲート（dev 369）

- `VERIFIED`: 20一意ID（通常16/ボス4）、企画順、既存6遭遇IDを読み取り専用で再検査。`enemy_tables.json`の整合記録SHA256一致、抽選/報酬/セーブ互換は不変。
- `GATE`: `dedicatedArtApproved=false`、`allTwentyPlayable=false`を明示。設計ゲートであり描画/動作/遭遇/美術の完了ではない。[レビュー](art/2026-10-10/ENEMY_ROSTER_CATALOG_GUARD_REVIEW.md)。次は同一identityの森狼素材・動作・接触を閉じる。
- `CHECK`: 初回EditModeは結果XML未生成の一時失敗。起動したUnityプロセスを整理して再実行し、`123/129`でPassed。

### 2026-10-10 戦闘の強弱を帯の色と文で隔離検証（dev 368）

- `VERIFIED`: turn1、敵攻撃、英雄の溜め、攻撃確定、次の大攻撃、ポーション、JUDGEの7状態を隔離Unityで14枚（2解像度）描画。上下帯の色一致、状態コピー、7色の一意性、セーブ不変を `validation.json` / `intensity.csv` で確認。
- `DECISION`: 既存 `EngageBandText` / `EngageBandColor` を戦闘強弱の読ませ方の基準として保持。新原画、本編接近、抽選/報酬、セーブは変更なし。
- `ART NOT APPROVED`: これは帯の比較候補であり、実機更新率・聴感・配布ビルド・20敵の最終素材・最終美術は未確認。[正本](art/2026-10-10/COMBAT_INTENSITY_REVIEW.md)。次は同一identityの森狼支持足候補が得られた場合だけ復帰比較へ戻る。

### 2026-10-10 森狼の透明復帰原画候補を却下（dev 367）

- `FAILED/REJECTED`: 透明背景・低姿勢・両手軸・支持足は得たが、body-v2の長いツインテール/白リボンと画風からずれた。原本・全文プロンプト・SHA256・理由を保存、QA未配置。
- `DECISION`: 単発生成を重ねずv4〜v7候補を正本として維持。本編43ポーズ・接近無効・セーブ不変。次は既存アトラスの足運び比較と戦闘強弱へ移る。

### 2026-10-10 森狼復帰候補の支持足アルファ計測（dev 366）

- `MEASURED`: 低い戻りの最下位アルファ行1166、標準戻り1172を読み取り専用で保存。両方1312×1199、透明境界の差は6px。
- `DECISION`: 画像境界差はUnity足接地を証明しないため採用保留。v6/v7検査と本編43ポーズ・接近無効・セーブを維持。[正本](art/2026-10-08/WOLF_FOOTPLANE_MEASUREMENT.md)。

### 2026-10-10 森狼の標準/低い復帰を同画面比較（dev 365）

- `CANDIDATE`: `HERO_WOLF_RECOVERY=low/standard` の同場面復帰静止画を保存。低い復帰と標準復帰を比較し、本編43ポーズ・接近無効・セーブ不変。
- `VERIFIED`: v7標準も126描画で接触/足元/低FPS/取消・割込み/contact1回passed。v6との差分平均・アルファ同一性を保存。目視では低い復帰が四足の高さ帯へ連続、標準は立ち上がりが強い。
- `ART NOT APPROVED`: 暫定目視であり支持足、肩から手、実機/聴感/配布/最終美術未確認。[正本レビュー](art/2026-10-08/WOLF_RECOVERY_DUAL_REVIEW.md)。次は低い復帰の支持足を別原画化する。

### 2026-10-10 森狼の歩行復帰静止画を連番化（dev 364）

- `CANDIDATE`: 接触後0.18秒の復帰静止画をQA連番へ追加。森狼低い戻り候補とdev 360接触原画を使い、本編43ポーズ・接近無効・セーブ不変。
- `VERIFIED`: v6 `validation.json` は126描画、3敵×剣/槍、接触/足元/低FPS/取消・割込み/contact1回passed。復帰静止画を目視し、連番とSHA256を保存。
- `ART NOT APPROVED`: 既存候補の静止画比較であり、四足支持足、実機/聴感/20敵/最終美術未確認。[正本レビュー](art/2026-10-08/WOLF_RECOVERY_STILLS_REVIEW.md)。次は標準復帰と低い復帰を同画面比較する。

### 2026-10-08 森狼の低い予備原画候補を却下（dev 363）

- `FAILED/REJECTED`: pose33用の新規透明スプライトは低い構えと支持足を保持したが、両手が二手槍軸にならずbody-v2との画風もずれた。原本・全文プロンプト・SHA256・理由を保存、QA未配置。
- `DECISION`: 生成枚数を進捗に数えず、v4/v5機械候補と本編43ポーズを維持。次は既存body-v2縦帯の足運び比較か、両手軸を明示した別原画を再制作する。

### 2026-10-08 森狼の低い復帰ルートをQA比較（dev 362）

- `CANDIDATE`: 新原画は増やさず、既存の低い戻り候補を `HERO_WOLF_RECOVERY=low` で森狼へ割り当てるQA-only分岐を追加。標準復帰と同じ比較スクリプトで再現可能。本編43ポーズ・接近無効・セーブ不変。
- `VERIFIED`: v5は3敵×剣/槍114描画で横/縦接触、足元、低FPS、取消・割込み、contact1回passed。環境変数を含むsource-manifestと連番を保存。
- `ART NOT APPROVED`: 既存候補のルート切替は機械比較のみ。実画像の森狼歩行復帰、支持足、実機/聴感/最終美術未確認。[正本レビュー](art/2026-10-08/WOLF_RECOVERY_ROUTE_REVIEW.md)。次はpose33/34/35の実画像縦帯を別制作する。

### 2026-10-08 森狼の歩行復帰原画編集を却下（dev 361）

- `FAILED/REJECTED`: 接触候補から腕だけを戻す編集を内蔵 image_gen で試行したが、胴体・脚まで変化し低い復帰シルエットを保持できず。原本・全文プロンプト・却下台帳を保存、QA Resourceへは未配置。
- `DECISION`: v4専用接触候補と検査結果を維持。本編43ポーズ・接近無効・セーブ不変。次はpose33/34/35の実画像縦帯を別制作し、支持足と歩行復帰を比較する。

### 2026-10-08 森狼専用の低い槍接触原画候補（dev 360）

- `CANDIDATE`: 森狼の上胸/肩へ向く低い接触原画を内蔵 image_gen で作成。原本・全文プロンプト・SHA256、QA Resource、pose34候補設定を保存。本編43ポーズ・接近無効。
- `VERIFIED`: v4 `validation.json` は3敵×剣/槍の114描画。森狼槍の専用原画と胸帯へ接続し、横/縦差0、足元、低FPS、取消・割込み、contact1回passed。`contact-gaps.csv` と `source-manifest.json` を保存。
- `ART NOT APPROVED`: 専用原画は候補であり、支持足、四足接触帯、歩行復帰、実機/聴感/20敵専用原画/最終美術未確認。[正本レビュー](art/2026-10-08/WOLF_SPEAR_CONTACT_REVIEW.md)。本番セーブ保持、コミット/push/公開なし。次は縦帯と歩行復帰を比較する。

### 2026-10-08 敵の高さ別に低い槍キーを選ぶ候補（dev 359）

- `CANDIDATE`: 隔離Editor検証で `slime=low`、`goblin=standard`、`forest_wolf=pounce` を敵ごとにメモリ上だけ適用。本編43ポーズ・接近無効・セーブ不変。
- `VERIFIED`: v3 `validation.json` は敵別接触アンカー込み114描画、接触距離/足元/低FPS/contact1回/取消・割込みpassed。中心生値（スライム+42.552、ゴブリン-26.070、森狼+75.031）を保存し、下胴/胸/中心アンカー後は縦差0。`height-rules.json` と `contact-gaps.csv` を保存。
- `ART NOT APPROVED`: アンカーは候補測定であり標準仕様ではない。支持足、戻り終端、実機/聴感/20敵専用原画/最終美術未確認。[正本レビュー](art/2026-10-08/ENEMY_HEIGHT_RULE_REVIEW.md)。次は実画像の縦帯を調整し歩行へ戻す。

### 2026-10-08 攻防の後にGETを表示（dev 357、実装/Unity検証10-07）

- `IMPLEMENTED`: 報酬表示を攻撃・被弾・復帰中は待機し、終了後に再開。本編のrewardWaitForCombat=true。表示済み帯の時計も止め、BETの記憶と報酬計算を維持。
- `VERIFIED`: 隔離Unityで候補3件、本編設定2件Passed。各58 PNG、接触時のGET非表示と復帰後の8 GETを目視。4件の同時表示/停止/再開、非表示中BET、二重加算なし。コンパイル・配置・差分・FX構文成功。初回テスト引数不足を修正し失敗ログ保存。
- `ART NOT APPROVED`: 本編43ポーズ・接近無効を保持。新原画44ポーズ/低い槍は見本。支持足・低い突きの前後姿勢・敵別の選択・後退速度・画面全体の美術は未完了。[表示順の検証正本](art/2026-10-07/COMBAT_REWARD_ORDER_REVIEW.md)。本番セーブ保持、コミット/push/公開なし。次は低い槍の予備/戻りと敵別選択を進める。

### 2026-10-07 接近時間と低い槍突きの候補（dev 356）

- `CANDIDATE`: 0.36秒の予備移動中は歩行キーを表示し攻撃時計を停止、その後f3-4を再生。内蔵画像生成で低い接触原画1枚を追加し、槍がスライム本体へ届く描画を確認。本編はapproach無効、44ポーズ候補/低い接触とも未採用。
- `VERIFIED`: v4/v5各114描画、各1.5秒動画2本。接近中のcontact抑制/歩行キー/装備予約/動く敵/低FPS刻み/取消・復帰passed。v6実BET2件Passed、隔離設定復元/本編不変。コンパイル・配置・差分・FX構文検査成功。
- `ART NOT APPROVED`: 低い突きの前後姿勢、支持足、敵別の選択、戻り速度が未完成。実BETではGETと接触が重なる場面も残り、演出時間の調整が必要。原本/プロンプト/判断/再現は[予備移動と低い槍突き](art/2026-10-07/TIMED_APPROACH_LOW_THRUST_REVIEW.md)。本番セーブ保持、コミット/push/公開なし。

### 2026-10-07 剣・槍と敵3種の横方向の接触を検証（dev 355）

- `CANDIDATE`: 武器先端と敵の現在位置から踏み込み量を計算し、recover/取消/割込み/敵交替で戻す処理を追加。本編は `approach.enabled=false`。新原画44ポーズと森狼はQA見本のまま。抽選/ダメージ/報酬は変更なし。
- `VERIFIED`: v2で3敵×2武器の既存/候補を2解像度24枚。横差は剣362.641→0、槍350.684→0。足元y、動く標的、60/15/5fps刻み、contact1回、取消/復帰passed。v3実BET2件Passed、隔離設定復元/本編不変。コンパイル・配置・差分・FX viewer構文検査成功。
- `ART NOT APPROVED`: スライムに対して槍が高く、接触まで0.075秒の長距離移動も自然な足運びとは未判定。本編有効化は保留。v1の敵ID/透明度ミスと修正を含む正本は[接触距離レビュー](art/2026-10-07/APPROACH_REVIEW.md)。次は低い敵向け接触姿勢と予備移動、表示の優先度を調整。本番セーブ保持、コミット/push/公開なし。

### 2026-10-04 歩行と槍の体格・両手接続を整える（dev 354）

- `CANDIDATE`: 内蔵画像生成6原本を保存し、槍7姿勢へ。最新候補は44ポーズ/20クリップ、本編は43のまま。突きに中割りを追加し、contact時刻を変えず最大隣接握り距離59.789→39.340。本編未採用、支持足の美術は未合格。[槍レビューと再現手順](art/2026-10-04/SPEAR_CYCLE_REVIEW.md)。
- `VERIFIED`: 修正版v11で200描画・2.4秒動画2本、柄の範囲内の両手接続/固定長/歩行位相/接触後の装備変更passed。v12の実BET回帰2件Passed、候補設定を復元し本編SHA256不変。Core/Runtime/Testsコンパイル・配置検算成功。
- 次は剣/槍と敵3種を同場面で比較し、支持足・予兆・接触・決着をまとめて調整。低FPS/全武器/実機/聴感/最終美術/全20敵は未完了。本番セーブ保持、コミット/push/公開なし。全体の継続設定は維持。

### 2026-10-04 剣の復帰と敵20種の制作台帳を整備（dev 353）

- `CANDIDATE`: 剣の戻り原画1枚を修正し、28→29の握り移動85.264→66.239。次区間は17.967→26.988へ分散。前回候補との直接比較200描画・2秒動画2本、8姿勢/contact1回/復帰passed。本編未採用、支持足と槍接続は未完了。[戻りレビュー](art/2026-10-04/SWORD_RETURN_REVIEW.md)。
- `DESIGN`: 敵台帳を企画の通常16＋ボス4・既存6遭遇IDへ整合。旧案保存、共有ボス画像と専用美術を区別。ID/区分/企画対応検査passed。遭遇表SHA256不変、20敵完成ではない。[台帳整合](art/2026-10-04/ENEMY_ROSTER_RECONCILIATION.md)。
- Core/Runtime/Testsコンパイル・配置検算成功。実BET再試験は今回未実施（前回v2は2件Passed）。実機/聴感/最終美術未確認。本番セーブ保持、コミット/push/公開なし。次は剣の支持足、歩行→槍構えと敵3体見本をまとめて確認する。

### 2026-10-04 剣の構えから斬撃・復帰まで8姿勢を同系列へ（dev 352）

- `CANDIDATE`: 新規7原本＋前回readyで剣8姿勢を統一。原本・プロンプト・SHA256保存。入口/出口だけでは残る中盤の体格差へ対応し、振り抜きの剣先見切れも角度修正。本編未採用。
- `VERIFIED`: v6-sword-clearanceで200描画、旧/新各2秒無音動画、8姿勢通過・contact1回・idle復帰・握り/Texture/剣の下端を検査。候補を隔離へ一時設置した実BET回帰2件Passed、終了後設定復元。Core/Runtime/Testsコンパイル・配置検算成功。
- 正本: [剣8姿勢レビュー](art/2026-10-04/SWORD_CYCLE_REVIEW.md)。28→29の握り移動と足位置のずれが残り、最終美術は未合格。次は戻りの中間姿勢を改善し、歩行/槍接続とまとめて本編採用判断。本番セーブ保持、コミット/push/公開なし。

### 2026-10-04 歩行と剣の構えの体格・握りを合わせる候補（dev 351）

- `CANDIDATE`: 歩行原画から構え1枚を内蔵画像生成で制作。pose23だけの隔離差替えで、歩行→構えの握り移動が10.87→2.58（View単位）へ減少。本編未採用、振り上げ/復帰との体格差は残る。
- `VERIFIED`: v3-ready-matchで218枚、剣/槍各3秒無音動画。実Texture一致・8姿勢・武器接続・攻撃復帰passed。Core/Runtime/Testsコンパイル、配置検算成功。初回v2は描画キャッシュで比較静止画が更新されず無効、検品側を修正して再撮影。
- 正本: [構え接続レビュー](art/2026-10-04/WALK_READY_MATCH_REVIEW.md)。次は同体格の振り上げ入口/復帰端と歩行重心。実BET再試験・実機・聴感・動画リアルタイム視聴は未実施。本番セーブ保持、コミット/push/公開なし。

### 2026-10-04 歩行の接地と大股を局所修正（dev 350）

- `CANDIDATE`: 内蔵画像生成でキー5の後ろ足とキー7の前脚を修正。後ろ靴の下端は28px下がり、前靴の張り出しは138px縮小（固定画像領域の測定）。原本2枚・プロンプト・SHA256保存。本編未採用。
- `VERIFIED`: v1-foot-refinementで剣/槍の8姿勢→攻撃→復帰を216枚と各3秒無音動画へ保存。機械passed、変更姿勢/構えの抜粋目視、Core/Runtime/Testsコンパイル・配置検算成功。新規コードなし。
- 正本: [接地と歩幅のレビュー](art/2026-10-04/WALK_FOOT_REFINEMENT_REVIEW.md)。重心の上下差と構えとの体格差は未解決。次は骨盤/支持足の位置を揃える。実BET再試験・実機・聴感・動画リアルタイム視聴は未実施。本番セーブ保持、コミット/push/公開なし。

### 2026-10-04 歩行8姿勢の仮接続を剣・槍で検品（dev 349）

- `CANDIDATE`: 10月3日開始のdown/up単体制作6原本とプロンプトを保存。初稿2枚を除外し、4候補＋既存接地/通過を8姿勢へ。本編未採用。生成名と実際の支持脚が違う案は、検品順と理由を明記。
- `VERIFIED`: v8-eight-key-blockingで216枚、剣/槍各3秒無音動画。全8姿勢・握り座標・手レイヤー・攻撃後復帰の機械検査passed。Core/Runtime/Testsコンパイル、配置検算成功。原本/QAとコード7件のSHA256一致。
- `ART NOT APPROVED`: 後ろ足の浮き、左右歩幅、down/up上下差、構えとの体格差が残る。実BET再試験・実機・聴感・動画のリアルタイム視聴は未実施。正本: [8姿勢レビュー](art/2026-10-03/WALK_EIGHT_KEY_REVIEW.md)。次はキー5の接地とキー7の歩幅を修正。本番セーブ保持、コミット/push/公開なし。

### 2026-10-03 通過姿勢を片側ずつ制作し4キー接続を検品（dev 348）

- `CANDIDATE`: 内蔵画像生成で手前脚支持の単体通過姿勢を制作し、下半身だけの編集で奥脚支持へ変更。接地ペアと合わせた4キーを隔離で再生。本編歩行には未採用。
- `VERIFIED`: v7-walk-blockingは110枚（主要姿勢2解像度）、3秒無音動画。4キー全通過→構え→攻撃→idle復帰がpassed。原画/Unityで支持脚と浮く脚を目視。Core/Runtime/Testsコンパイル、配置検算成功。原本・プロンプト・SHA256保存。
- 正本: [通過姿勢と4キーのレビュー](art/2026-10-03/WALK_BLOCKING_REVIEW.md)。次はdown/upを1枚ずつ追加し、8姿勢の接地と連続動作を検品する。粗い4キー見本は最終美術合格ではない。本番セーブ保持、コミット/push/公開なし。

### 2026-10-03 歩行の左右接地を個別キーで検品（dev 347）

- `CANDIDATE`: 内蔵画像生成で接地ペア初稿→下半身修正→8コマ展開→passing試作の4原本を保存。下半身だけの修正で接地の左右脚を区別できたが、8コマ展開とpassingの支持脚は未合格。全て本編未採用。
- `VERIFIED`: 隔離Unityに個別キーの静止検品入口を追加。4キー×剣/槍＋構え参考を2解像度20枚で描画しpassed。前景の握り、脚の違い、足元を目視。Core/Runtime/Testsコンパイル・配置検算成功。ランタイムと本編ポーズJSONはdev346から変更なし。
- 正本: [左右接地の原本・失敗・次の手順](art/2026-10-03/WALK_KEYS_REVIEW.md)。次はpassingを片側1枚ずつ確定し、4キーが読めてからdown/upを追加する。本編歩行・最終美術・計画全体は未完了。本番セーブ保持、コミット/push/公開なし。

### 2026-10-03 剣の構え・復帰で刀身と握りを見せる（dev 346）

- `IMPLEMENTED`: 剣の待機/構え入口と復帰2コマに前景の手を追加。元PNGの小矩形を同じ座標で重ね、体→剣→手の描画順と-55度の角度で、頭から刀身だけが出る状態を修正。他ポーズと槍は従来順へ戻す。
- `VERIFIED`: 隔離Unityで既存/修正184枚、無音動画2本。手と柄の位置・描画順・切替時の非表示を検査。実BETの主人公/敵2シナリオPassed。Core/Runtime/Testsコンパイル、配置検算成功。正本: [握りの描画レビュー](art/2026-10-03/GRIP_LAYER_REVIEW.md)。
- `UNVERIFIED`: 歩行の左右脚/頭身差、動作全体の最終美術、全武器、20敵、光影、実機/聴感は未完了。歩行候補3案は未採用のまま。新規生成やPNG加工なし。本番セーブ保持、コミット/push/公開なし。

### 2026-10-03 歩行原画3案の比較と採用保留（dev 345）

- `CANDIDATE`: 内蔵画像生成で歩行3案を制作。原本・プロンプトを保存。v5は構えに近い体格と握り位置だが、左右の脚運びと刀身が頭へ重なる問題が残るため全案未採用。本編walk-v3を維持。
- `VERIFIED`: 隔離Unityの既存/候補、剣/槍、歩行→構え→接触→復帰を比較。208枚（主要姿勢は2解像度）、無音動画2本、握り座標を保存。Unity exit0、Core/Runtime/Testsコンパイル成功。比較の機械成功と美術合格を分離。
- 正本: [歩行レビューと次の手順](art/2026-10-03/WALK_REVIEW.md)。次は左右支持脚2枚の対を確定し、武器の携行角度と前後関係を詰める。本番セーブ保持、コミット/push/公開なし。計画全体は未完了。

### 2026-10-03 攻撃中の復帰予約と接触の保持（dev 344）

- `IMPLEMENTED`: 歩行/待機の復帰予約が実行中の攻撃の世代/contactを消さないよう修正。明示的な割込みは従来どおり取消する。
- `VERIFIED`: 隔離shop-qaで実BET/装備切替/接触保持のPlay Mode1件と敵の実BET回帰1件がPassed、2解像度54静止画の描画passed。Core/Runtime/Testsコンパイル、配置検算、fx同期成功。詳細は[本編サリア接続](HERO_GAMEPLAY_P2.md)と`tools/hero-gameplay-qa/2026-10-03/v1-contact-resume/`。
- `UNVERIFIED`: 連続動作の最終美術、20敵本編、全武器、光影、実機/聴感。生成は行わず既存原本を使用。本番セーブを保持し、この作業からのコミット/push/公開なし。

最終更新: 2026-10-03（Asia/Tokyo）

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
# 2026-10-08 / dev 358

- 低い槍突きの接触前・戻り始め原画2枚を内蔵 image_gen で作成。原本、正確なプロンプト、参照、SHA256を `docs/art/2026-10-08/spear-low-transition-assets.json` に保存した。
- 44ポーズQA候補へ pose33/35 として一時接続。姿勢比較114描画、3敵×剣/槍、接触距離/足元/低FPS/取消・割込み/contact1回を確認。実BET回帰3件もPassed、QA設定復元、本編不変。
- Unity起動指定の失敗をv2〜v5として保存。成功判定はv3 validation.jsonとv1 reels.xmlだけに限定し、候補と最終美術を混同しない。
- 報酬表示の攻防待機（dev357）と合わせて、GETが接触を覆わないことを確認。次は敵の高さ別に低いキーを選ぶ規則と、戻りから歩行へつなぐ姿勢を比較する。P2〜P5、20敵、背景光影、実機・聴感・配布ビルドは未完了。


### 2026-10-20 / P2接近・攻撃の既存回帰を再確認

Heartbeat継続作業として既存の隔離ランナーを再実行した。接近158 capture、剣/槍攻撃135 capture、戦闘7状態×2解像度、実BET回帰1 scenario、17 sample、8ファイルsource manifest一致がPassed。py -3 tools/verify/run.pyもcompile/harness Passed。追加コード・画像・ゲーム数値変更なし。実機/聴感/最終美術/配布build、P2美術とP3〜P5は未完了。本番セーブを使わず、commit/push/publishなし。[記録](art/2026-10-20/P2_RUNTIME_REGRESSION_REVIEW.md)。

### 2026-10-20 / 歩行→攻撃handoffの視覚確認

同日の隔離captureを再確認し、0.3583秒の歩行pose18→0.3667秒の攻撃pose26を隣接画像で記録した。root差0.023、contact前のため、速度調整でなくposeの切替が主な視覚差として残る。中間原画やpose再接続は未採用。利用可能ツールに内蔵画像生成がなく、生成素材は作成していない。P2の支持足/中割り、敵3種、後続P3〜P5は未完了。[目視証拠](art/2026-10-20/HERO_WALK_ATTACK_HANDOFF_VISUAL_REVIEW.md)。
### 2026-10-20 / dev 400: 接近→攻撃のframe/capture対応

- 既存目視資料の画像参照に1 frameずれがあったため、walk pose18を042、attack pose26を043へ訂正（044はpose26のまま）。frame indexとStep後のelapsed timeの定義を追記。
- QAへframe/capture CSV列、pose18→attack poseでPNG内容が変わるassert、一意GUID付き隔離project pathを追加。`runtime-approach-attempt04` 全回帰PASS（接近158/攻撃135、7状態×2解像度、EditMode実BET 1件、17 samples、source manifest一致、harness）。
- 支持足/中割りは美術未承認。ゲームデータ/原画/セーブは変更なし。内蔵ImageGen unavailableのため新画像なし。`bigbonusblitz` automationを解除した。
### 2026-10-20 / dev 401: 接近末尾へ既存の攻撃準備poseを表示

- 歩行→strikeの一段切替を減らすため、lead最後の0.08秒に武器ごとの既存attack-f2末尾（剣25/槍32）を表示し、その後のstrike開始pose（剣26/槍33）へ移る。新しいcueや時計進行は追加しない。
- attempt05は旧walk期待pose18、attempt06は旧contact時間算定のQA失敗。fixture修正後のattempt07で接近170/攻撃135、7状態×2解像度、EditMode BET 1件、17 sample、source manifest一致PASS。py verify harnessもPASS。
- 実画像は3段に見える。支持足/中割り美術は未承認。GameController/Core/Save/乱数/HP/報酬/SE所有は変えない。実機/実入力/聴感/配布buildは未確認。[目視](art/2026-10-20/HERO_WALK_ATTACK_HANDOFF_VISUAL_REVIEW.md) / [attempt07](../tools/hero-gameplay-qa/2026-10-20/runtime-approach-attempt07/)
