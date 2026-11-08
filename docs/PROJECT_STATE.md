### 2026-11-09 調べるための記録（dev 412）

- `IMPLEMENTED`: `DiagnosticLog` が `persistentDataPath/diagnostics/bbb-日付.log` に、レバー（状態1行）・結果・入力の鍵の掛け外し（理由つき）・押したのに受け付けなかった理由（同じ理由は1秒に1回）・機械側の停止が弾かれたとき・AUTO の入り切り・例外とエラー（スタックつき）を書く。4MBで .old に回す。dev 411 のゲーム画面のログの「いま」の行は戻した。
- `VERIFIED`: 隔離コピーで書き出しを確認（LEVER / RESULT / LOCK / REFUSED / AUTO が出る）。全EditMode 148/148。
- `LIMIT`: 本人の「BONUS 成立のまま押せない」はまだ再現していない。次に起きたら、その時刻の REFUSED 行の「理由=」で判別する。

### 2026-11-09 ログの「いま」・演出の待ちの期限・ベル3体斬りの例外（dev 411）

- `IMPLEMENTED`: ログ枠の1行目に「いま」（待っているもの・押せない理由・押すもの）を常に出す。説明の行は吹き出しの有無に関係なくログへ全部残す。入力の鍵に理由と期限を付け（`LockInput`）、期限のある演出待ち（PUSH の受付・ボーナス開始）は期限を過ぎたら外してログに残す。ベル3体斬りの倒す演出が次のレバーで消された敵に触って例外になり、演出が止まっていたのを修正（`_bellShowSerial`、`Effects` は対象が消えたら終わる）。
- `VERIFIED`: 隔離コピーで AUTO x4・時間3倍を3つの種で各150秒、20Gごとにボーナスを成立させて回した（2026-11-09）。例外は本体のコード 66〜93件 → 直した後 0件。
- `LIMIT`: 本人の画面（BONUS 成立のまま MAX BET が押せない）はまだ再現できていない。長回しで見つかった止まり方は、呪いの申し出の待ちと街へ戻る場面だけで、どちらも画面に理由が出ている。次に起きたらログの「いま」で理由が分かる。

### 2026-11-09 敵の体力バー・狙え！の柱・絵の圧縮（dev 410）

- `IMPLEMENTED`: 敵の体力を頭上の3行の箱から、表示域の上端の1行のボスバー（名前 / ゲージ / 数値 / SET）へ（本人 案A）。バーが出ている間は示唆セリフを下へずらす。「狙え！」の柱は成功する止め位置の実際の配列を上・中・下で見せる。主人公・武器・敵の絵（`Art/HeroPrototype` 33枚）を高品質圧縮へ（`HeroPrototypeImporter`）。
- `VERIFIED`: 隔離コピー（iOS）で取り込み、全33枚 ASTC 4x4。1枚 12.6MB→3.1MB（Unityエディタの`GetRuntimeMemorySizeLong`）。元絵との差は最悪の所を4倍に拡大しても見分けにくい（[見比べ](art/2026-11-09/compress-compare.png)）。ボスバーは1280×720・2556×1179で描き出し、ステージ札・示唆セリフと重ならない（[描き出し](art/2026-11-09/boss-bar-2556.png)）。
- `LIMIT`: Windows向け（Standalone）は幅・高さが4の倍数でない3枚（body-v2, sword-v2, walk-v3）が圧縮されない可能性がある。実機の見た目は未確認。

### 2026-11-07 Unityの見直し（dev 409）

- `IMPLEMENTED`: 範囲はf375b3f..9a7fd27（敵20種・武器7種・背景の統合）。①`Resources.UnloadUnusedAssets`が1か所も無く、通った背景と敵の絵が残り続けていたのを解放（`AdventureAssets`。回転の合間だけ・最短20秒間隔）。②同じ敵の攻撃を接触後に頼み直すと結果表示が丸ごと飛ぶのを修正。③未登録の見た目で白い四角が出るのを修正。④呪いHUDの毎コマの文字列生成をやめ、5種類以上で「+N」が重なる/枠を出るのを修正。⑤確率エディタで見た目IDの打ち間違いを警告。
- `VERIFIED`: 隔離コピー（productName=shop-qa）で全EditMode 148/148（確認用1件を含む）。普段飛ばすプレイモード試験7件も通過。100万G 112.2%は修正前と同じ。解放は15地点・10種の移動後に300MB→24MB（2026-11-07、Unityエディタの`GetRuntimeMemorySizeLong`。実機の値ではない）。[記録](art/2026-11-07/UNITY_REVIEW.md)
- `LIMIT`: 実機のメモリ・解放時のひっかかりは未計測。主人公の絵（無圧縮）とC-1の縮小は未対応で、記録の「残した指摘」に書いた。

### 2026-11-06 他AI向けUnity統合・引き継ぎ（dev 408）

- 最新指示により追加制作を区切り、生成済み9背景（C-3/C-4/D-1〜D-6/E-1）をResourcesと本編背景設定へ接続。新画風は16/30地点（実アルファ15＋既存C-1）。原本・プロンプトを保存。
- D-3の樹冠切れをv2で修正。原画の透明余白から中景の下端位置を揃えた。水平な地形下端と道路の馴染みは美術調整を残す。
- 先行20敵・7武器は90bef83でpush済み。今回の検証結果・クリーンな再開場所・残り14背景と動作/光影・再実行コマンドは[引き継ぎ正本](art/2026-11-06/AGENT_HANDOFF.md)。[最終検証](art/2026-11-06/BACKGROUND_HANDOFF_VALIDATION.json)を参照。
- 共有作業場のindex/既存変更と本番セーブを保持。隔離Unity検証のみ。機械合格と最終美術承認は別。端末実機/配布build/URP/性能/聴感は未検証。計画全体の完了ではない。

### 2026-11-03 未登録敵アートの誤流用を遮断（dev 404）

- `IMPLEMENTED`: `ArtLoader.EnemySprite` の未知ID→ゴブリン既定画像を削除。既存3種だけを明示マップし、未登録IDはnull。
- `VERIFIED`: 現行共有ソースを同期した専用cloneでCore/Runtime/Tests compile、100万Gハーネス、不変条件違反0、EditMode 131/137・失敗0・Ignored6。未知ID/nullの公開API null返却を確認（警告文一致assertはなし）。詳細は[dev404レビュー](art/2026-11-03/ENEMY_UNMAPPED_ART_FALLBACK_REVIEW.md)。
- `LIMIT`: これは表示の安全境界のみ。20種・専用アート・各モーション・本編遭遇表は未完了。画像生成APIの利用は本人許可済みだが `OPENAI_API_KEY` 未設定で新規原画は未作成。

### 2026-10-20 歩行→攻撃を既存予備poseで3段接続（dev 401）

- `IMPLEMENTED`: 接近の最後0.08秒を、装備に応じたattack-f2最終pose（剣25/槍32）の表示へ。攻撃strikeは剣26/槍33へ続く。接近の長さ/距離、contact cue、抽選/HP/報酬/音の所有者は変更しない。
- `VERIFIED`: 隔離Unity attempt07は接近170枚、剣/槍の接近系列、hand-off前後の実PNG変化、contact時刻/一回性、足元、低FPS、取消/割込みをPASS。攻撃135枚、戦闘7状態×2解像度、実BET EditMode 1件、17 sample、source manifestもPASS。`py -3 tools/verify/run.py` Core/Runtime/Tests compileと100万G harness PASS。
- `FAILED→FIXED`: attempt05は古いfixtureがlead=.35秒でwalk pose18を期待していた。attempt06ではcontact追跡時間が古いサンプル時刻に依存。どちらも試験側の期待値/時間計算を更新し、attempt07でPASS。
- `OBSERVED`: 画像ではwalk pose17→予備25/32→strike26/33の3段。姿勢の骨格差と支持足の連続は美術未承認。実機/実入力/聴感/配布build、敵3種/20敵/P3〜P5は未完了。
- [視覚/検証記録](art/2026-10-20/HERO_WALK_ATTACK_HANDOFF_VISUAL_REVIEW.md) / [attempt07](../tools/hero-gameplay-qa/2026-10-20/runtime-approach-attempt07/)

### 2026-10-20 歩行→攻撃の証拠画像対応と隔離QAを修正（dev 400）

- `IMPLEMENTED`: QAの接近traceへframe番号とcapture名を明記し、歩行pose18→攻撃pose26/33で実PNGが変わることを検査。並行実行時の一時Unity project衝突を避けるGUID付き出力先も追加。
- `VERIFIED`: `runtime-approach-attempt04` で接近158枚、攻撃135枚、戦闘7状態×2解像度、実BET EditMode 1件、17 sample、source manifest一致がPassed。pixel-change assertion=true。Core/Runtime/Tests compileと100万G harnessもPassed。
- `CORRECTED`: trace elapsedはStep後の時刻、画像名は0始まりのStep番号。walk pose18は042、attack pose26は043、044もpose26。以前の目視記録の043/044リンクを042/043に修正。製品ランタイム不具合ではなく、証拠の対応ずれ。
- `LIMIT`: 画像ではwalk pose18からattack pose26へ0.0084秒で直接切替。支持足/中割りの美術承認は未取得。敵3種の本編接続、20敵、残り装備、背景光影、実機/聴感/配布build、P3〜P5も未完了。新規原画なし。保存領域未使用。予約済みbigbonusblitz自動タスクは解除済み。
- [確認記録](art/2026-10-20/HERO_WALK_ATTACK_HANDOFF_VISUAL_REVIEW.md) / [隔離QA](../tools/hero-gameplay-qa/2026-10-20/runtime-approach-attempt04/)

### 2026-10-20 P2接近・攻撃経路を再確認

- `VERIFIED`: 隔離Unityの既存ランナーを再実行。接近158枚、攻撃135枚、7状態×2解像度、実BET 1 scenario、reel 17 sample、本編/隔離manifest一致がPassed。`py -3 tools/verify/run.py` のCore/Runtime/Tests compileと100万G harnessもPassed。saveDataTouched=false相当の画像/ゲーム素材変更なし。`finalArtApproved=false`を維持。
- `LIMIT`: P2の支持足/歩行→攻撃中割り目視承認、敵3種の本編接続、20敵/装備/背景光影、実機/実入力/聴感/配布buildは未完了。[記録](art/2026-10-20/P2_RUNTIME_REGRESSION_REVIEW.md) / [QA](../tools/hero-gameplay-qa/2026-10-20/runtime-approach/)
- `OBSERVED`: 同じQAの隣接画像では歩行pose18→攻撃pose26が0.0084秒で切替。root接近差0.023、contact前。中割り/支持足の美術承認は未取得。[目視記録](art/2026-10-20/HERO_WALK_ATTACK_HANDOFF_VISUAL_REVIEW.md)

# BigBonusBlitz 現状まとめ

### 2026-10-19 HPカードを敵名・HP・状態の3段に整える（dev 399）

- `IMPLEMENTED`: 敵HP情報を敵名 / HP / 残りGまたはSET状態へ分け、カードを高さ70・テキスト領域214×44に拡張。ゲージを下段へ移す。
- `VERIFIED`: attempt04隔離Unityで7状態×2解像度、敵名/HP/状態の非空3行・寸法内・状態文字、実BET回帰1件、接近/攻撃系列、17 sample/source manifestをPASS。実戦画像でも3行とHPゲージ一致を目視。
- `FAILED→FIXED`: attempt03はJUDGE状態に一般SET文字を期待するQA fixture誤りで失敗。製品側のJUDGE表示に合わせて状態別assertへ修正しattempt04でPASS。
- `LIMIT`: 実機/配布build/全敵名の長文とP2〜P5全体は未完了。save/抽選/HP計算/報酬/遭遇/素材不変、最終美術未承認。[レビュー](art/2026-10-19/COMBAT_HP_CARD_TEXT_FIT_REVIEW.md) / [QA](../tools/hero-gameplay-qa/2026-10-19/runtime-approach-attempt04/)

### 2026-10-19 戦闘帯の下でHPカード文字が透ける不具合を修正（dev 398）

- `IMPLEMENTED`: 戦闘帯と重なる敵HPカードの紺面alphaを0.97から1へ変更し、背後の流れる文字がHPラベルに残らないようにした。
- `VERIFIED`: 隔離Unityで7状態/2解像度を再描画し、各状態でHP面alpha=1かつ帯より前面をassert。実BET回帰・接近/攻撃画像・source manifestも同じランナーで確認する。
- `LIMIT`: 確認は隔離Unityのみ。全画面美術/実機/聴感/配布ビルドは未承認。主人公・敵・武器の原画、抽選/HP数値/報酬/遭遇/saveは変更しない。[レビュー](art/2026-10-19/COMBAT_HP_OPAQUE_SURFACE_REVIEW.md) / [QA結果](../tools/hero-gameplay-qa/2026-10-19/)

### 2026-10-18 戦闘帯を不透明な紺面へ（dev 397）

- `IMPLEMENTED`: 半透明の色面を不透明な濃紺にし、金の細線と左端の状態色アクセントへ。Web版のスクロール、文章、速度、上下/ピーク切替は維持。
- `VERIFIED`: attempt12隔離Unityで7状態/2解像度14枚、実BET回帰1 scenario、接近158枚/攻撃135枚、17 sample、source manifest一致、112.2% harnessをPASS。layout checkも重なり/はみ出しなし。
- `FAILED→FIXED`: attempt11回帰は旧テストがBg自体を状態色として読んでいたため失敗。状態アクセント参照へ直しattempt12でPASS。製品側失敗ではない。
- `LIMIT`: 表示面の候補と隔離検査のみ。ルート札/敵HPとスクロール文の全体位置、実機/聴感/配布build/最終美術、P2〜P5は未承認。新画像なし、抽選/HP/報酬/遭遇/save不変。[レビュー](art/2026-10-18/COMBAT_BAND_OPAQUE_SURFACE_REVIEW.md) / [QA](../tools/hero-gameplay-qa/2026-10-18/runtime-approach-attempt12/)

### 2026-10-18 戦闘のピークで下段帯を一時退避（dev 396）

- `IMPLEMENTED`: build-up中は上下の帯を維持し、敵攻撃/確定攻撃/Potion/Judge中は下段を出さない。Destroyは遅延するため旧帯を先に非表示化し、同一フレーム置換でも重ならないようにした。
- `VERIFIED`: attempt10の隔離Unityで7状態/14枚、active帯の実数、copy/tint対応、2解像度/source hash一致を確認。実BET 1 scenario、接近158枚/17-state timeline、検証ハーネスもPassed。
- `LIMIT`: attempt09の機械assertionは古いDestroy待ちの残像を見逃した。attempt10で再修正。セーブ/抽選/HP/報酬/遭遇/原画不変。上段と画面全体の読みやすさ、最終美術、実機/入力/聴感/配布ビルドは未承認。[レビュー](art/2026-10-18/COMBAT_INTENSITY_PEAK_STRIP_REVIEW.md) / [QA](../tools/hero-gameplay-qa/2026-10-18/runtime-approach-attempt10/)

### 2026-10-18 通常BETの停止フェーズを全sample検査（dev 395）

- `VERIFIED`: attempt08の隔離Unityで剣/槍のf1/f2を停止要求まで各120Hz sampleで保持。anticipation→swing→contactが正しい停止段階で各一度、全攻撃pose通過、queued walk復帰を確認。攻撃135 capture、接近158枚、実BET EditMode 1 scenario、source manifest一致がPassed。
- `IMPLEMENTED`: QA assertion/summaryだけを強化。既存槍のf1/f2同pose32は維持し、美術判断待ち。抽選/HP/報酬/接触時間/セーブ/原画は変更なし。
- `LIMIT`: `py -3 tools/verify/run.py` compile/100万G Passed（機械割112.2%、不変条件違反なし）。実入力/聴感/実機/配布build/最終美術は未確認。[レビュー](art/2026-10-18/HERO_ATTACK_SEQUENCE_PHASE_GUARD_REVIEW.md) / [QA](../tools/hero-gameplay-qa/2026-10-18/runtime-approach-attempt08/)

### 2026-10-18 通常BET攻撃3段の描画時計を記録（dev 394）

- `VERIFIED`: 隔離Unity attempt06で剣/槍のBET→f1保持→第一停止f2→第二停止f3/4→contact→walk復帰を120Hz trace化。各clipの全原画姿勢、contact一回、walk復帰をassert。連番静止画135枚、EditMode実BET回帰1 test passed、source manifest一致。
- `OBSERVED`: 槍のf1とf2は共にpose32を最後/唯一の絵として使い、第一停止に視覚上の変化がない。素材上の観察であり、補間/原画追加/採用決定なし。
- `LIMIT`: attempt04はQA期待値が槍poseを剣のpose24と誤認したfixture失敗。修正後attempt06 Passed。画風/実機/聴感/配布ビルドは未確認。実BET EditMode回帰はfixture駆動。ゲーム/抽選/HP/報酬/セーブ不変。[レビュー](art/2026-10-18/HERO_ATTACK_SEQUENCE_REVIEW.md) / [QA](../tools/hero-gameplay-qa/2026-10-18/runtime-approach-attempt06/)

### 2026-10-18 攻撃への切替を120Hzで計測（dev 393）

- `IMPLEMENTED`: 隔離Unityに接近終端の120Hz pose/position/contact traceと前後の実描画キャプチャを追加。製品挙動/数値/素材は変更なし。
- `VERIFIED`: attempt03は接近158枚、剣/槍ともcontactイベント時刻以前の発火なし/イベント一回、実BET接触照準0px・復帰、EditMode 1 scenario、source manifest一致がPassed。0.3583秒のpose18歩行から0.3667秒に剣pose26/槍pose33へ切替。通常検証ハーネスもPass。
- `LIMIT`: 切替は一段のpose変更であり中割り/支持足/最終美術を承認しない。初回120Hz試験は「切替窓全域contactゼロ」という誤ったQA前提で失敗し、原画イベント時刻基準に直した。実入力/実機/聴感/配布ビルド未確認。セーブ/抽選/HP/報酬不変。[レビュー](art/2026-10-18/HERO_APPROACH_WALK_CADENCE_REVIEW.md) / [QA](../tools/hero-gameplay-qa/2026-10-18/runtime-approach-attempt03/)

### 2026-10-18 接近中の歩行速度を原画に合わせる（dev 392）

- `IMPLEMENTED`: 0.36秒の攻撃接近中に8コマ周期を均等圧縮していた表示を、歩行JSONの各フレーム時間に沿う再生へ変更。原画は1コマ0.11秒/周期0.88秒。接近中の戦闘時計停止を維持。
- `VERIFIED`: attempt22の隔離Unityで接近比較126枚、剣/槍の歩行キー時間チェック、実BET回帰1 scenario、17 fixture timeline summary、本編/隔離source manifest一致がPassed。
- `LIMIT`: 実機/実入力/聴感/配布ビルド/最終美術は未確認。歩行→攻撃の接続と支持足の連続アニメ評価は未完了。新規画像なし。抽選/接触時間/HP/報酬/セーブ不変。[レビュー](art/2026-10-18/HERO_APPROACH_WALK_CADENCE_REVIEW.md) / [QA](../tools/hero-gameplay-qa/2026-10-16/runtime-approach-attempt22/)

### 2026-10-16 戦闘帯を結果表示の前に片づける（dev 391）

- `IMPLEMENTED`: 各BET結果のアクション中は帯を表示し、接触/反応が終わると隠す。結果routine世代を分け、前のBETの終了が次BETの帯を消さないようにした。
- `VERIFIED`: attempt21の隔離Unity実BET回帰1件Passed、接近比較126枚、fixture 17件timeline summary Passed、本編/隔離manifest一致。`py -3 tools/verify/run.py` compile/harness Passed。
- `LIMIT`: 実入力/聴感/実機/配布ビルドと結果全体の最終レイアウト/美術は未確認。抽選/HP/報酬/セーブ不変、`saveDataTouched=false`。[レビュー](art/2026-10-16/COMBAT_BAND_RESULT_HANDOFF_REVIEW.md) / [QA](../tools/hero-gameplay-qa/2026-10-16/runtime-approach-attempt21/)

### 2026-10-16 戦闘BETの状態遷移タイムラインを記録（dev 390）

- `IMPLEMENTED`: 隔離実BETシナリオがBET/各リール停止/Evaluate後の主人公・敵状態、contact cue、HP/報酬状態、時刻をspin ID付きCSVに保存。集計ツールは接触→敵反応/復帰などの差分をJSON化する。
- `VERIFIED`: attempt15で17 spinを記録しEditMode 1シナリオPassed。剣/槍接近・contact0px・recover復帰と既存126枚source-pose比較もPassed。本編/隔離SHA256一致。runnerがCSV集計JSONの生成まで自動検査する。
- `LIMIT`: StopReelは試験fixtureが直接呼び、数例はcaptureFramerateを固定。実入力のプレイ時間・演出密度・聴感・波の採用値ではない。`productionIntegrated=false` / `finalArtApproved=false`。検査用記録追加で、抽選/HP/報酬/セーブ動作は不変。[レビュー](art/2026-10-16/COMBAT_TIMING_REEL_REVIEW.md) / [CSV](../tools/hero-gameplay-qa/2026-10-16/runtime-approach-attempt15/real-enemy-reel/battle-timeline.csv)

### 2026-10-16 剣攻撃の踏み込みを実リールで確認（dev 389）

- `VERIFIED`: 隔離EditModeの実BET/停止/Evaluateで剣と槍の踏み込みを確認。contact時の両武器先端Xと敵表示中心Xの誤差0px、剣の撃破/槍の通常攻撃ともcontact1回、recover後はrest位置へ復帰。
- `IMPLEMENTED`: QAに剣の踏み込み・照準計測のassertion、source/isolated SHA256 manifestを追加。runtime・ゲーム数値・セーブ形式は不変。
- `LIMIT`: 機能確認で、接触絵の最終承認ではない。planned 5武器/16敵、実機/聴感/配布ビルドは未完了。[レビュー](art/2026-10-16/HERO_SWORD_APPROACH_REEL_REVIEW.md) / [隔離結果](../tools/hero-gameplay-qa/2026-10-16/runtime-approach-attempt10/real-enemy-reel/validation.json)

### 2026-10-16 攻撃前の踏み込みを本編で有効化（dev 388）

- `IMPLEMENTED`: 本編サリアの攻撃前踏み込みを `approach.enabled=true`、距離600px/準備0.36秒で有効化。既存GameControllerの対象参照とAdventureHeroPresenter実装を利用。source poseに合わないQA候補高さ補正は採用しなかった。
- `VERIFIED`: Core/Runtime/Tests compile Passed。隔離Unityで126静止画の水平接触/足元/低FPS/取消比較、実敵リールの槍攻撃前踏み込みを各1シナリオPassed。本編境界ガードPassed、saveDataTouched=false。
- `LIMIT`: 槍スライムの接触画は頭上寄り。機能検査の合格で、接触絵の美術承認ではない。敵runtimeはslime/goblinのみ、森狼preview-only。遭遇/乱数/ダメージ/報酬/セーブ/実機/配布ビルドは未確認。`productionIntegrated=false` / `finalArtApproved=false`。[レビュー](art/2026-10-16/HERO_ATTACK_APPROACH_INTEGRATION_REVIEW.md)

### 2026-10-16 森狼の槍復帰を低姿勢/標準姿勢で比較（dev 387）

- `VERIFIED`: 同じ隔離Unity/同じ候補で復帰モードだけを変えて各126枚を再描画。低FPS接触、足元ゲート、取消/割込み、敵別アンカーは両方Passed。接触後画像では低姿勢が四足の胸帯と槍軸を保ち、標準姿勢は立ち上がりが早い。
- `CANDIDATE ONLY`: 復帰後の連番・歩行再開の足滑りはこの静止画比較では判定できない。`approach.enabled=false`、`finalArtApproved=false`を維持。本編の敵/遭遇/抽選/報酬/セーブは不変。[レビュー](art/2026-10-16/WOLF_RECOVERY_STANDARD_LOW_COMPARE_REVIEW.md) / [低姿勢](../tools/hero-gameplay-qa/2026-10-14/dev387-height-rule-continuation/forest_wolf-w_spear-approach-recovery-1280.png) / [標準姿勢](../tools/hero-gameplay-qa/2026-10-14/dev387-height-rule-standard/forest_wolf-w_spear-approach-recovery-1280.png)

### 2026-10-14 戦闘強弱帯の可読性調整（dev 386）

- `VERIFIED`: 7状態/2解像度の14枚を隔離Unityで再描画。帯を小さく・上段を内側へ移動し、HP下地を濃くした。色/文対応、上下一致、`saveDataTouched=false`はPassed。
- `GATE`: 下段帯はキャラの足元後方を通る。これは可読性修正の候補で最終美術・実機・聴感・配布ビルド承認ではない。抽選/ダメージ/報酬/セーブ不変。[レビュー](art/2026-10-14/COMBAT_INTENSITY_READABILITY_REVIEW.md) / [JSON](../tools/hero-gameplay-qa/2026-10-14/dev386-intensity-band-layout-final/validation.json)

### 2026-10-14 敵20種ロスター再検査（dev 385）

- `VERIFIED`: 読み取り専用catalogue guardがPassed。20一意ID/通常16/ボス4、既存6遭遇IDと遭遇表hash維持を確認。
- `GATE`: existing 3 / preview-only 1 / planned 16。`allTwentyPlayable=false`、`finalArtApproved=false`。敵絵/遭遇/報酬/セーブの変更なし。[レビュー](art/2026-10-14/ENEMY_ROSTER_REVALIDATION_REVIEW.md) / [JSON](../tools/enemy-roster-qa/2026-10-14/dev385-roster-revalidation/validation.json)

### 2026-10-14 敵3種・剣槍動作見本の隔離再描画（dev 384）

- `VERIFIED`: 隔離Unityで85静止画、補助192/リール144/敵288フレーム、敵24ポーズ/12クリップ、主人公43ポーズ/20クリップ、武器2/敵見本3を再検査。低FPS接触、割込み取消、決着保持、停止、槍両手軸、武器別復帰、歩行位相を確認。
- `GATE`: `productionIntegrated=false`、`audioListeningVerified=false`、`finalArtApproved=false`。森狼preview-only。実BET/実機/URP/配布ビルド/最終美術は未承認。遭遇/抽選/報酬/セーブ不変。[レビュー](art/2026-10-14/ENEMY_THREE_MOTION_REFRESH_REVIEW.md) / [JSON](../tools/hero-prototype-qa/2026-10-14/v10-motion-refresh/validation.json)

### 2026-10-14 P2本編境界の再確認（dev 383）

- `VERIFIED`: 読み取り専用ガードを再実行し、主人公接近無効、敵runtimeはslime/goblin、forest_wolfはpreview-only、既存剣/槍2種とplanned武器5種、敵20件（通常16/ボス4）、既存6遭遇ID、ランタイムから設計カタログ参照なしを確認。`saveDataTouched=false`。
- `GATE`: `productionIntegrated=false`、`allTwentyPlayable=false`、`allSevenPlayable=false`、`finalArtApproved=false`。本編データ/原画/runtimeは変更なし。[レビュー](art/2026-10-14/PRODUCTION_BOUNDARY_REFRESH_REVIEW.md) / [JSON](../tools/production-boundary-qa/2026-10-14/dev383-boundary-refresh/validation.json)

### 2026-10-10 P2本編境界再監査（dev 382）

- `VERIFIED`: 読み取り専用ガードを再実行し、主人公接近無効、敵runtime/slime・goblin、森狼preview-only、武器7系統、20敵のnormal16/boss4、既存6遭遇ID、武器カタログのC#参照なしを再確認。`saveDataTouched=false`。
- `GATE`: `productionIntegrated=false`、`allTwentyPlayable=false`、`allSevenPlayable=false`、`finalArtApproved=false`。設計/本編境界の監査であり、遭遇/抽選/報酬/セーブ/原画/runtimeは変更なし。[レビュー](art/2026-10-10/PRODUCTION_BOUNDARY_RERUN_REVIEW.md) / [JSON](../tools/production-boundary-qa/2026-10-10/dev382-production-boundary-rerun/validation.json)

### 2026-10-10 EditMode隔離再検証（dev 381）

- `VERIFIED`: `tools/environment-qa/run.ps1 -Tests` を本番とは別の一時Unityで完走。EditMode 129件中123件Passed、失敗0、保留6（既知のIgnore）を再確認。JSON/XMLとログを保存。
- `GATE`: `result="Skipped:Ignored"` はIgnore 6件によるもの。実機/URP/配布ビルド/聴感/最終美術/本編統合は未承認、セーブ/抽選/報酬/遭遇は不変。[レビュー](art/2026-10-10/EDITMODE_REFRESH_REVIEW.md) / [XML](../tools/environment-qa/2026-10-11/v10-editmode-refresh/editmode.xml)

### 2026-10-10 背景ソース・光影の隔離再実描画（dev 380）

- `VERIFIED`: 本番とは別の `%TEMP%/bbb-environment-c1-qa` を再起動し、30地点/3層、時間帯4、天候9、森12条件、両解像度、屋内天候ガードを再実描画。森昼晴れ/夜雨、C-1ゲーム画面、継ぎ目前後を保存し、`saveDataTouched=false`。
- `GATE`: `productionIntegrated=false`、`finalArtApproved=false`。URP/実機GPU/配布ビルド/負荷/サウンドと原画承認は未検証。本編セーブ/抽選/報酬/遭遇は不変。[レビュー](art/2026-10-10/BACKGROUND_REFRESH_REVIEW.md) / [JSON](../tools/environment-qa/2026-10-11/v9-background-refresh/validation.json)

### 2026-10-10 P2本編境界ガード（dev 379）

- `VERIFIED`: 主人公の本編接近無効、敵runtimeはslime/goblinのみ、森狼preview-only、武器7系統（existing 2 / planned 5）、20敵（normal 16 / boss 4、existing 3 / preview-only 1 / planned 16）、既存6遭遇ID、`weapon_visuals.json`のC#参照なしを読み取り専用で確認。
- `GATE`: セーブ/抽選/報酬/遭遇の変更なし。`productionIntegrated=false`、`allTwentyPlayable=false`、`allSevenPlayable=false`、`finalArtApproved=false`。[レビュー](art/2026-10-10/PRODUCTION_BOUNDARY_GUARD_REVIEW.md) / [JSON](../tools/production-boundary-qa/2026-10-10/dev379-production-boundary/validation.json)

### 2026-10-10 敵3種動作見本・剣槍の隔離再実描画（dev 378）

- `VERIFIED`: 現行 `shop-qa` を再実行し、85静止画/192補助フレーム/144リールフレーム/288敵フレーム、24敵ポーズ/12クリップ、剣/槍、低FPS接触、割込み取消、決着保持、槍両手軸、武器別復帰、歩行位相を再確認。新しい3敵接触と槍攻撃の1280×720静止画を目視した。
- `GATE`: 既存の見本・候補を隔離再描画しただけで、`productionIntegrated=false`、`audioListeningVerified=false`、`finalArtApproved=false`。森狼はpreview-only、遭遇/抽選/報酬/セーブは不変。[レビュー](art/2026-10-10/ENEMY_THREE_MOTION_REFRESH_REVIEW.md) / [JSON](../tools/hero-prototype-qa/2026-10-11/v9-enemy-motion-refresh/validation.json)

### 2026-10-10 敵・背景同場面証拠マトリクスガード（dev 377）

- `VERIFIED`: 既存の隔離Unity成果を読み取り専用で再照合。森狼/スライム/ゴブリン×剣/槍の126枚、敵3種動作85枚、C-1背景30地点/森12条件、昼晴れ/夜雨を同場面マトリクスとして確認。1280×720 / 2556×1179ゲーム画面と1280×426背景レイヤーの代表画像も確認。
- `GATE`: 各validation、接触/割込み/天候/セーブ不変がPassed。`productionIntegrated=false`、`finalArtApproved=false`、`saveDataTouched=false`を維持。新規レンダリング・原画承認・遭遇有効化ではない。[レビュー](art/2026-10-10/SCENE_MATRIX_GUARD_REVIEW.md) / [JSON](../tools/scene-matrix-qa/2026-10-10/dev377-scene-matrix-guard/validation.json)

### 2026-10-10 背景・光影プローブ差分ガード（dev 376）

- `VERIFIED`: dev371の隔離Unity実画像を再照合し、C-1/森昼晴れ/夜雨、30地点・森12条件・屋内天候・セーブ不変を確認。平均RGB差56.128。
- `GATE`: 光影の機械差分のみ。URP/端末/GPU/配布ビルド/最終美術は未承認、`finalArtApproved=false`。[レビュー](art/2026-10-10/BACKGROUND_LIGHT_SHADOW_GUARD_REVIEW.md) / [JSON](../tools/environment-qa/2026-10-10/dev376-light-shadow-guard/validation.json)

### 2026-10-10 敵3種ソース/本編境界ガード（dev 375）

- `VERIFIED`: スライム/ゴブリン/森狼の各6モーション定義（idle/ready/attack/strike/hit/defeat）を読み取り専用で照合。
- `GATE`: ランタイムはslime/goblinのみ、森狼はpreview-only。全3体 `dedicatedArtApproved=false`、`productionIntegrated=false`、`finalArtApproved=false`。遭遇/抽選/セーブ不変。[レビュー](art/2026-10-10/ENEMY_THREE_SOURCE_GUARD_REVIEW.md) / [JSON](../tools/enemy-gameplay-qa/2026-10-10/dev375-three-enemy-guard/validation.json)

### 2026-10-10 敵3種動作見本・剣槍接続再検証（dev 374）

- `VERIFIED`: 現行隔離Unityでリーフスライム/森のゴブリン/森狼の予兆・攻撃・被弾・決着を288連番、24ポーズ、12クリップで再描画。武器予約、槍両手軸、低FPS接触、割込み取消、決着保持、停止を確認。
- `GATE`: `productionIntegrated=false`、`audioListeningVerified=false`、`finalArtApproved=false`。本編遭遇/抽選/セーブ、実機/配布ビルド、聴感は未承認。[レビュー](art/2026-10-10/ENEMY_THREE_MOTION_RERUN_REVIEW.md) / [JSON](../tools/hero-prototype-qa/2026-10-10/v8-enemy-motion-rerun/validation.json)

### 2026-10-10 森狼・剣槍同一identity支持足/握りゲート（dev 373）

- `VERIFIED`: 既存v1低槍候補を読み取り専用で再検査。重要ポーズ/ループ重複、槍支持アンカー、接地値29〜36、握り座標、剣/槍カタログを確認。
- `GATE`: 数値契約のみで、髪/顔/胴体identity、肩/手の見た目接続、実機/URP/配布ビルド、最終美術は未承認。`mainEnabled=false`、`finalArtApproved=false`。[レビュー](art/2026-10-10/WOLF_IDENTITY_SUPPORT_GUARD_REVIEW.md) / [JSON](../tools/hero-gameplay-qa/2026-10-10/v11-identity-guard/validation.json)

### 2026-10-10 森狼・背景・剣槍同場面比較ゲート（dev 372）

- `VERIFIED`: 隔離UnityでC-1背景上の森狼剣/槍候補を126枚（2解像度）再描画。水平接触、復帰、キャンセル/割り込み、支持足、セーブ不変を確認。`mainEnabled=false`。
- `FAILED/RECOVERED`: v9は本編JSONにQA専用アトラス名がなく検証前停止。v10は既存v1低槍候補へ戻して成功。失敗ログも保持。
- `GATE`: 同一identityの肩/手/支持足連続性、URP/実機/配布ビルド、最終美術は未承認。`finalArtApproved=false`。[レビュー](art/2026-10-10/WOLF_BACKGROUND_WEAPON_REVIEW.md) / [JSON](../tools/hero-gameplay-qa/2026-10-10/v10-wolf-background-approach/validation.json)。

### 2026-10-10 背景30地点・光影ソース境界ゲート（dev 371）

- `VERIFIED`: 現行Hubパスで隔離Unityを再実行し、30地点の一意ID、C-1遠/中/近3層、森12条件（時間4/天候9）の実描画/屋内天候/継ぎ目/セーブ不変結果を確認。`saveDataTouched=false`。
- `GATE`: `run_source_guard.py`はソース/統合境界のみ。URP Play Mode、端末/GPU負荷、配布ビルド、最終美術は未承認。`finalArtApproved=false`。
- [レビュー](art/2026-10-10/BACKGROUND_SOURCE_GUARD_REVIEW.md) / [JSON](../tools/environment-qa/2026-10-10/dev371-background-source-guard/validation.json)。次は森狼の同一identity支持足、剣/槍の見本と背景を同場面で比較する。

### 2026-10-10 装備外見7系統の設計ゲート（dev 370）

- `IMPLEMENTED/VERIFIED`: `weapon_visuals.json` を追加し、`game_config.json` の武器7種を既存の剣/槍アトラス範囲・握り座標・高さと対応づけた。planned 5種（大槌/短剣/弓/杖/聖剣）は未承認のまま。
- `GATE`: `run_weapon_visual_guard.py`で7 ID順、剣/槍の既存定義一致、C#参照0件を確認。装備効果、抽選、セーブ、途中切替、5種の画像/動作/実機表示は変更なし。`allSevenPlayable=false`、`finalArtApproved=false`。
- [レビュー](art/2026-10-10/WEAPON_VISUAL_CATALOG_GUARD_REVIEW.md) / [JSON](../tools/equipment-qa/2026-10-10/dev370-weapon-visual-guard/validation.json)。次は同一identityの剣/槍支持足見本、planned武器は原画と動作契約後。

### 2026-10-10 20敵カタログ/遭遇表の境界ゲート（dev 369）

- `VERIFIED`: `enemy_roster_p2.json` の通常16＋ボス4、企画書順、既存6遭遇IDを読み取り専用で再検査。`enemy_tables.json` は2026-10-04整合記録のSHA256と一致し、`runtimeEncounterTableUnchanged=true`。
- `GATE`: これは設計カタログと本編遭遇表の境界を守る機械ゲートであり、20体の描画/動作/遭遇/美術承認ではない。artStatusはexisting3 / preview-only1 / planned16、`allTwentyPlayable=false`を維持。
- [レビュー](art/2026-10-10/ENEMY_ROSTER_CATALOG_GUARD_REVIEW.md) / [JSON](../tools/enemy-roster-qa/2026-10-10/dev369-catalog-guard/validation.json)。次は同一identityの森狼素材/動作/接触を閉じる。

### 2026-10-10 戦闘の強弱を帯の色と文で隔離検証（dev 368）

- `IMPLEMENTED/VERIFIED`: 既存の `EngageBandText` / `EngageBandColor` を対象に、turn1、敵攻撃、英雄の溜め、攻撃確定、次の大攻撃、ポーション、JUDGEの7状態を隔離Unityで14枚（1280×720 / 2556×1179）描画。上下帯の色一致、状態ごとの文、7色の一意性、保存データ不変を確認。
- `ART CANDIDATE`: [帯の強弱レビュー](art/2026-10-10/COMBAT_INTENSITY_REVIEW.md)と `tools/hero-gameplay-qa/2026-10-10/v8-combat-intensity-rerun/` にCSV/JSON/静止画を保存。既存の森背景・サリア表示を使った比較見本であり、新規原画・本編接近・最終美術は未承認。
- `GATE`: 本編の抽選・ダメージ・報酬・セーブ、`approach.enabled=false` は不変。実機更新率/聴感、配布ビルド、20敵の最終素材、最終美術は未確認。次は支持足を含む同一identityの森狼復帰候補が得られた場合だけ槍の比較へ戻る。

### 2026-10-10 森狼の透明復帰原画候補を却下（dev 367）

- `FAILED/REJECTED`: `transparent_background=true` で再生成し、透明・低姿勢・両手軸・支持足は得たが、body-v2の長いツインテール/白リボンと画風からずれた。原本・全文プロンプト・SHA256・理由を保存し、QAへは未配置。
- `DECISION`: これ以上の単発生成でidentity driftを増やさず、v4〜v7の機械候補を正本として保持。本編43ポーズ・`approach.enabled=false`・セーブは不変。次は既存アトラスの足運び比較と戦闘強弱へ進む。

### 2026-10-10 森狼復帰候補の支持足アルファ計測（dev 366）

- `MEASURED`: 低い戻り候補の最下位アルファ行1166、標準戻り1172（両方1312×1199）を読み取り専用で計測。`footplane-metrics.json` にboundsと候補限定フラグを保存。
- `DECISION`: 透明境界の6px差はUnityのfoot値や足滑りを直接証明しないため、美術採用・本編接続は保留。v6/v7の126描画と接触検査、本編43ポーズ・接近無効・セーブを維持。[支持足計測](art/2026-10-08/WOLF_FOOTPLANE_MEASUREMENT.md)。

### 2026-10-10 森狼の標準/低い復帰を同画面比較（dev 365）

- `CANDIDATE`: `HERO_WOLF_RECOVERY=low/standard` を同じQA候補・同じ場面で切替。低い復帰と標準復帰の接触後0.18秒静止画を保存し、原画・本編設定は変更なし。
- `VERIFIED`: v7標準復帰も126描画で横/縦接触、足元、低FPS、取消/割込み、contact1回をPassed。v6低い復帰とのRGB差分平均・アルファ同一性を読み取り専用で記録。目視では低い復帰が四足の高さ帯に連続し、標準は立ち上がりが強い。
- `ART NOT APPROVED`: 目視は暫定判断で、支持足・肩から手・実機/聴感・配布ビルド・最終美術は未確認。本編43ポーズ・`approach.enabled=false`・セーブを維持。[比較レビュー](art/2026-10-08/WOLF_RECOVERY_DUAL_REVIEW.md)。次は低い復帰の支持足を別原画で作り直す。

### 2026-10-10 森狼の歩行復帰静止画を連番化（dev 364）

- `CANDIDATE`: 接触後0.18秒の復帰静止画をQA連番へ追加。森狼は既存の低い戻り候補、接触はdev 360専用原画を使用し、原画・本編設定は変更なし。
- `VERIFIED`: v6で126描画、3敵×剣/槍の横/縦接触、足元、低FPS、取消/割込み、contact1回をPassed。森狼槍の復帰静止画を目視確認し、`source-manifest.json` に126枚とSHA256を保存。
- `ART NOT APPROVED`: 復帰静止画は候補比較であり、四足支持足・実機/聴感・20敵専用原画・最終美術は未確認。本編43ポーズ・`approach.enabled=false`・セーブを維持。[静止画レビュー](art/2026-10-08/WOLF_RECOVERY_STILLS_REVIEW.md)。次は標準復帰と低い復帰を同画面で比較する。

### 2026-10-08 森狼の低い予備原画候補を却下（dev 363）

- `FAILED/REJECTED`: pose33用に新規透明スプライトを生成。低い構えと支持足は使えるが、両手が一本の槍軸に分離せず、body-v2との画風もずれたため採用しない。原本・全文プロンプト・SHA256・理由を保存、QAへは未配置。
- `DECISION`: v4/v5の機械候補と本編43ポーズを維持し、生成原画の数で進捗を増やさない。次は既存body-v2の縦帯を使った足運び比較か、両手軸を明示した別原画を再制作する。

### 2026-10-08 森狼の低い復帰ルートをQA比較（dev 362）

- `CANDIDATE`: 画像を増やさず、既存の低い戻り候補を森狼へ割り当てるQA-only環境変数 `HERO_WOLF_RECOVERY=low` を追加。標準復帰との切替を同じ設定で再現できるようにし、本編コード/接近/セーブは変更なし。
- `VERIFIED`: v5で低い復帰ルートを含む3敵×剣/槍の114描画、横/縦接触、足元、低FPS、取消/割込み、contact1回をPassed。`source-manifest.json` に環境変数とSHA256を保存。生成原画の歩行復帰はまだ採用していない。
- `ART NOT APPROVED`: 低い戻り候補は機械接続の比較用で、森狼の四足支持足・実画像縦帯・歩行復帰の見た目・実機/聴感・最終美術は未確認。本編43ポーズ・`approach.enabled=false`を維持。[v5比較レビュー](art/2026-10-08/WOLF_RECOVERY_ROUTE_REVIEW.md)。

### 2026-10-08 森狼の歩行復帰原画編集を却下（dev 361）

- `FAILED/REJECTED`: 森狼接触候補を基準に歩行復帰の腕だけを編集する内蔵 image_gen 試行を実施したが、生成結果が胴体・脚まで立ち姿へ変わり低い復帰シルエットを保持できなかった。原本・全文プロンプト・却下理由を保存し、QA Resourceへはコピーしていない。
- `DECISION`: v4の森狼専用接触候補と機械検査結果を正本候補として維持し、失敗原画は採用しない。本編43ポーズ・`approach.enabled=false`・セーブは不変。
- `NEXT`: pose33/34/35の実画像縦帯を別々に作り直し、支持足と歩行復帰が自然に連続する候補だけを再比較する。[却下原画台帳](art/2026-10-08/spear-wolf-recovery-rejected-v1-assets.json)。

### 2026-10-08 森狼専用の低い槍接触原画候補（dev 360）

- `CANDIDATE`: 森狼の上胸/肩へ向く低い接触姿勢を内蔵 image_gen で制作。透明背景、足位置、髪・胴体を保持し、原本・全文プロンプト・SHA256を保存。本編素材は変更なし。
- `VERIFIED`: 隔離Unity v4で3敵×剣/槍の114描画。森狼の槍接触に専用原画を割り当て、横差0・縦差0・足元・低FPS・取消/割込み・contact1回をPassed。`source-manifest.json` と `contact-gaps.csv` を保存。
- `ART NOT APPROVED`: 機械検査は候補合格だが、支持足、四足の胸帯、歩行からの復帰、実機・聴感、20敵専用原画、最終美術は未確認。本編43ポーズ・`approach.enabled=false`・セーブを維持。[森狼接触レビュー](art/2026-10-08/WOLF_SPEAR_CONTACT_REVIEW.md)。次は縦帯と歩行復帰の実画像を比較する。

### 2026-10-08 敵の高さ別に低い槍キーを選ぶ候補（dev 359）

- `CANDIDATE`: 隔離Unityのメモリ上だけで `slime=low`、`goblin=standard`、`forest_wolf=pounce` の3区分を比較。低い予備/接触/戻り始めと標準戻りを敵の見かけ高さに割り当て。本編43ポーズ・`approach.enabled=false`・遭遇表・セーブは変更なし。
- `VERIFIED`: v3で敵別接触アンカーを適用した114描画、3敵×剣/槍、接触距離0、足元、低FPS、取消・割込み、contact1回がPassed。中心点での生値（スライム+42.552、森狼+75.031、ゴブリン-26.070）を保存し、下胴/胸/中心アンカー後は3敵とも縦差0。候補復元を確認。
- `ART NOT APPROVED`: 数値合格と見た目の近さを分離。アンカーは候補測定であり標準仕様ではない。森狼の四足接触帯、支持足、戻り終端、実機・聴感・20敵専用原画・最終美術は未確認。[高さ別ルールレビュー](art/2026-10-08/ENEMY_HEIGHT_RULE_REVIEW.md)。本番セーブ保持、コミット/push/公開なし。次は実画像の縦帯と歩行復帰を調整する。

### 2026-10-08 低い槍突きの前後キー候補（dev 358）

- `CANDIDATE`: 接触直前と戻り始めの2枚を内蔵 image_gen で制作し、44ポーズQA候補のpose33/35へ接続。本編43ポーズ・approach無効は維持。
- `VERIFIED`: 3テスト回帰3件Passed（候補設定復元、本編不変）。姿勢比較114描画で3敵×2武器の接触距離/足元/低FPS/取消・割込み/contact1回passed。原本・プロンプト・SHA256・連番・失敗ログを保存。[低い槍の前後レビュー](art/2026-10-08/LOW_SPEAR_TRANSITION_REVIEW.md)。
- `ART NOT APPROVED`: 生成原画の支持足、髪/胴体の連続性、敵高さ別選択、戻り終端、実機・聴感・配布ビルド・最終美術は未確認。本番セーブ保持、コミット/push/公開なし。次は敵高さ別の姿勢ルールと歩行復帰を比較する。

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

最終更新: 2026-10-03。**別セッションはこの 1 枚を読めば続きから始められる。詳細な全履歴は [AI_HANDOFF_HISTORY.md](AI_HANDOFF_HISTORY.md)。**

### 2026-09-29 ジャッジのとどめもcontact後に決着（dev 340）

- [敵接続の最新記録](ENEMY_GAMEPLAY_P2.md)。Judge第一停止の先行EnemyHitを除去し、主人公の武器contact後にだけ敵hit/defeatとジャッジ勝利演出を開始するよう修正。
- `tools/enemy-gameplay-qa/2026-09-29/v6-judge/`は隔離Unityの実BET検査1件Passed。既存5分岐、剣/槍接触、剣のとどめ、Judge勝利、説明枠停止/再開/取消、敵交替を再確認し、1280×720/2556×1179の静止画とtraceを保存。第一停止中はenemy=ready、Judge contact後はpose27/contact1→defeat。
- 試験fixtureはJudge Gの状態を注入済みにするため`PendingTier2=false`/`IsTier2=true`を指定。確率・HP・報酬の本編仕様は変更していない。実機/聴感/全20敵/最終美術は未完了。本番セーブ、コミット、push、公開なし。

### 2026-09-29 ポーション3分岐を実BETで確認（dev 341）

- [敵接続の最新記録](ENEMY_GAMEPLAY_P2.md)。ポーションの回復、次の大攻撃予約、レア役の討伐確定を実BET→停止→Evaluateで検証。Smallの大攻撃はcontact1回後に敵hit、Rareの討伐確定は主人公contactなしでルーレット後に決着。
- `tools/enemy-gameplay-qa/2026-09-29/v8-potion/`は1件Passed。既存5分岐、剣/槍、剣のとどめ、Judge、Potion3分岐、説明枠停止/再開/取消、敵交替を再通過し、trace・validation.json・2解像度の静止画を保存。Core/Runtime/Testsコンパイル済み。
- ポーションfixtureは既に開いているTier2状態のみを注入し、敵HPは既定100へ合わせた。抽選率、HP、報酬、実機入力は変更していない。実機/聴感/全20敵/最終美術は未完了。本番セーブ、コミット、push、公開なし。

### 2026-09-29 戦闘の強弱を帯の色と文で伝える（dev 342）

- [敵接続の最新記録](ENEMY_GAMEPLAY_P2.md)。エンゲージ帯を状態連動にし、準備/敵攻撃/主人公構え/ポーション/次の大攻撃/Judgeを青・赤・青金・紫・暖色・赤紫で区別。PotionLargeの予約文も帯へ表示。
- `tools/enemy-gameplay-qa/2026-09-29/v10-wave/`は1件Passed。既存の実BET回帰とPotion/ Judgeを再通過し、帯の色と文言を機械検査、2解像度静止画を保存。Core/Runtime/Testsコンパイル、配置検算、差分検査成功。
- UIの段階表示だけを追加し、抽選・ダメージ・報酬・保存は変更していない。敵20種、BGMの波、全武器、実機/聴感、最終美術は未完了。本番セーブ、コミット、push、公開なし。

### 2026-09-29 20敵ロスターの設計土台を固定（dev 343）

- `Assets/Resources/Data/enemy_roster_p2.json`に20件の一意ID、役割、バイオーム、予兆、接触、決着、強度帯を追加。existing 3、preview-only 1、planned 16を明示し、仮絵の使い回しで本編へ入れない。
- `tools/enemy-roster-qa/README.md`と`docs/art/2026-09-29/enemy-roster-concept-prompt.txt`に境界と生成記録を保存。内蔵画像生成はHTTP 429 `usage_limit_reached`で失敗したため原本は未生成。JSON件数/一意IDの機械検査は成功。
- planned敵は透明アトラス、プロンプト原本、動作/contact、隔離Unity QAが揃うまで遭遇表へ未接続。Forest wolfをbatへ置換しない。全20敵の本編美術、実機/聴感、最終品質は未完了。本番セーブ、コミット、push、公開なし。

### 2026-09-29 剣・槍の接触後に敵が被弾する（dev 338）

- [敵接続の最新記録](ENEMY_GAMEPLAY_P2.md)。主人公のcontact合図まで敵hit/斬撃/音を待機。剣のとどめもcontact後に決着し、設定中の停止、Cancel、敵交替で古い攻撃が残らない。
- `tools/enemy-gameplay-qa/2026-09-29/v2-impact-final/`は4シナリオPassed。実BET5分岐と剣/槍、敵/主人公/P1回帰、2解像度の描画、trace、コンパイル/配置検算を確認。試験設定の役・押下位置・HPを含み、実機/聴感/全抽選は未確認。
- `v3-impact-visual/`ではcontact合図直後の剣/槍ポーズを追加撮影し、traceで槍pose34・剣pose27、contact=1から敵hit/defeatへ進むことを確認。試験側の撮影改善であり、本編速度は変更していない。
- 次は接触の見栄えと回避姿勢、中割り、Judge/Potion全分岐。全20敵/戦闘の波/全武器/光影、旧bat/ボーナス複数敵、実機/聴感は未完了。新規生成なし、既存bigbonusblitz継続、本番セーブ保持、コミット/push/公開なし。

### 2026-09-28 動作中の敵を説明枠で隠さない（dev 337）

- [実装・検証の正本](ENEMY_GAMEPLAY_P2.md)。新敵の攻撃/被弾/決着と主人公の攻撃/防御中は説明枠を待機。文字送り・読了待ちも止め、終了後に同じ位置から再開。入力待ちの構えや物語会話は表示を維持する。
- `tools/enemy-gameplay-qa/2026-09-28/v11-dialogue-final/`は4シナリオPassed。実BET反撃中の非表示、長文保持/モーダル復帰/取消/物語の優先と既存敵・主人公・P1の回帰を確認。32静止画/38連番/無音動画。コンパイル・配置検算成功。2解像度で敵の足が見えることと説明の復帰を目視。
- 次は原画contactと敵被弾/斬撃FXの時刻、武器別リーチ、回避姿勢。旧batやボーナス3敵の遮蔽、Judge/Potion全分岐、全20敵/戦闘の波/全武器/光影、実機/聴感は未完了。新規生成なし。既存bigbonusblitz継続、本番セーブ保持、コミット/push/公開なし。

### 2026-09-28 実BETの戦闘動作を整理（dev 336）

- [敵接続の最新記録](ENEMY_GAMEPLAY_P2.md)。構えだけで被弾する、回避ベルでも先に攻撃する問題を実リールで再現し修正。通常役と戦闘結果のキャラ動作を分離。ナビのCanvasGroup例外と前BETの遅延表示も修正。
- `tools/enemy-gameplay-qa/2026-09-28/v9-final/` は4シナリオPassed。Hit/Guard/Dodge/Counter/Attackを実BET→停止→Evaluateで確認し、既存敵/主人公/P1も回帰成功。30静止画と38連番、無音の反撃動画を保存。コンパイル/配置検算成功。遭遇/役/押下位置は試験で設定した条件で、実マウス/実機/最終美術は未確認。
- 次は接近中の敵が説明枠に隠れる点、主人公のリーチ/回避姿勢/中割りと命中時刻。Judge/Potion全分岐、全20敵/戦闘の波/全武器/光影も未完了。新規生成なし、原本を保持。既存bigbonusblitzは継続。

### 2026-09-28 スライム/ゴブリンの本編接続（dev 335）

- [敵の本編接続](ENEMY_GAMEPLAY_P2.md)。通常/中ボス/ATのEnemyにスライムとゴブリンを接続。ready保持→strike→復帰、被弾/決着原画、接触に合わせた既存サリア反応。新表示から抽選・ダメージ・報酬・音を追加しない。ボスの倍率/色、未対応種の旧Imageを保持。
- `tools/enemy-gameplay-qa/2026-09-28/v5-final/` は敵/主人公/P1の3シナリオPassed、26静止画。2解像度、通常2種・ボス色・bat復帰・AT、モーダル停止復帰、出現取消/接触前の非表示/旧表示切戻し。コンパイル/配置検算成功。ATに旧戦闘帯が残る点も目視後に修正。
- 原画は既存の生成原本を使用。森狼は見本のみ、コウモリ・前兆影・ボーナス3敵は旧表示。次は実BETから敵の全結果へ至る操作と動画、サリアと敵の中割り/頭身差。全20敵/戦闘の波/全武器/光影/聴感/実機は未完了。計画全体の完了ではなく、既存bigbonusblitzを維持。

### 2026-09-28 OP逃走・帰還・再入場の検証（dev 334）

- [本編接続の正本](HERO_GAMEPLAY_P2.md)。会話を隠した後も残るOPホストでサリアが停止する問題を修正。停止条件を会話の実表示へ分離し、非表示の全画面暗幕の入力判定も解除。
- 隔離Play Modeで逃走→街→門→OP完了→帰還→再入場、装備槍の保持を確認。`tools/hero-gameplay-qa/2026-09-28/v6-journey-final/` は1件Passed・10画像。P1回帰1件Passedはv5-journey。v5の再入場撮影は暗転中だったため、v6で明転待ちに修正。実入力/全保存分岐/聴感/実機は未確認。
- 次は頭身/中割りの仕上げと敵の本編接続。[敵調査](ENEMY_PRESENTATION_P2.md)を参照。本編はslime/goblin/bat、見本はslime/goblin/wolf。batをwolfへ置換しない。20敵/戦闘の波/全武器/光影は未完了。既存の継続設定を維持。

### 2026-09-28 新サリアを本編へ接続（P2進行中）

- [本編接続の正本](HERO_GAMEPLAY_P2.md)。43ポーズ/20クリップの新サリアをGameControllerへ接続し、剣/槍を装備欄から同期。既存の親位置/回転/FXを保持。gameplayEnabled=trueが既定、falseで旧表示へ切り戻せる。
- 本編の攻撃音/Voiceは既存処理が所有し、新viewから重ねて鳴らさない。11表示要求、ベルの第2停止、リール外の構え解除、攻撃中の武器予約を接続。
- 描画QA `tools/hero-gameplay-qa/2026-09-28/v3-final/` は46枚でpassed。剣先とステージ札の重なりを避ける倍率170の最終描画は`v4-final/`。2解像度、11要求×剣/槍、会話との配置、敵/前兆時idle、旧表示への切戻しを確認。実BET/停止/仮セーブ再読込/モーダル停止復帰のPlay Mode1件とP1回帰1件もPassed（XMLはv3-final）。
- 本編の敵は旧表示のまま。次はOP/帰還/再入場と頭身差・中割りの確認、敵3体の本編接続。専用足音/聴感/実機/配布ビルド/20敵/戦闘の波/光影は未完了。以下の「本編未接続」は各時点の履歴。

### 2026-09-28 集中・防御・詠唱と動作音（本編未接続）

- [補助動作・音の正本](HERO_SUPPORT_P2.md)。新6ポーズを内蔵画像生成で追加し、計43ポーズ/20クリップ。集中の反復、攻防後の復帰、剣/槍のslashも定義。最初のコマのswing欠落を修正。
- Unityメニュー「サリアの集中・防御・詠唱見本」。Play Modeでは右下から動作音をON。既存音管理/音量を使用し、被弾の二重音を抑止する。専用足音と聴感は未完了。
- 最終描画QA `tools/hero-prototype-qa/2026-09-28/v7-support-final/` はpassed。85静止画/768連番/4本の無音動画。Play Mode音検査は `v6-support/play-tests.xml` の1シナリオPassed。最終版との相違は原画6ポーズの武器角度だけ。コンパイル/配置検算成功。
- 次は本編のキャラ位置/フェード/全要求/武器再読込を調べ、隔離コピーで実接続する。歩行→構え/旧被弾との頭身差、専用足音、聴感、20敵、戦闘の波、光/影は未完了。見本合格を本編・最終美術の合格と扱わない。

### 2026-09-28 敵3種の動作見本（本編未接続）

- 続行: [リール接続の正本](HERO_REEL_BRIDGE_P2.md)。本編の既存順序（BET→第1停止で溜め→第2停止で斬撃）に合わせた保持と解除を実装。専用メニューを追加。37ポーズ/武器差分込み15クリップ。最新の総合検証先は`tools/hero-prototype-qa/2026-09-28/v5-reels-final/`。本編GameControllerは未変更。

- [敵動作の正本](ENEMY_PRESENTATION_P2.md)。スライム/ゴブリン/森狼を各8ポーズ、計24ポーズで制作。予兆の長さ・跳躍・振り下ろし・飛びかかりを分け、被弾と決着を追加。既存のサリア検品画面上部で操作できる。
- 表示専用の時計・移動・接触イベント。割込み後の遅い接触を抑止し、決着は保持。原本/プロンプト/失敗理由を保存。最新QAは`tools/hero-prototype-qa/2026-09-28/v3-enemies-final/`。
- 次はサリアの歩行/被弾/復帰の姿勢差と、本編のリール停止に対応する分割動作の設計・検証。20敵・本編への接続・照明の完成ではない。継続設定`bigbonusblitz`は既存のものを使用。

### 2026-09-27 サリアのP2見本（制作中・本編未接続）

- 続行版: 剣8/槍6/歩行8の原画を追加し、全37ポーズ・武器差分込み9クリップ。槍の専用両手軸、装備切替時の専用復帰、アトラス内の一定倍率を実装。詳細と未完了は正本冒頭へ。最新QA `tools/hero-prototype-qa/2026-09-27/v8-motion-final/` はpassed（v6は初回版の証拠）。
- 本人「何も言わなくてもずっと作業してください」。このタスクの1時間ごとの継続実行`bigbonusblitz`を作成済み。追加の続行確認は不要。最新差分を読んで進め、重複した自動化を作らない。

- [制作・引き継ぎ正本](HERO_PRESENTATION_P2.md)。新サリア9キーポーズ＋歩行6コマ、体から独立した剣/槍、スライム/ゴブリン/森狼の静止画を生成。Unityメニュー `BBB → サリアのモーション・武器見本` から確認できる。
- 6クリップの時計、同じ歩行の位相保持、攻撃中の武器変更予約、接触イベントの重複抑制、足元と握り位置をJSON化。基準は元気なサリアの既存立ち絵。C-1の背景は既存の新3層を使用。
- 最終隔離QA `tools/hero-prototype-qa/2026-09-27/v6-final/` はpassed。2解像度を含む静止画25枚、144連番・12秒の無音MP4を保存。再表示の重複なし、DontSave、専用EventSystem所有も確認。コンパイルと配置検算成功。原本/全生成プロンプト/修正理由も保存。お知らせdev 328は制作進捗として追加。
- 本編のHero/敵テーブル/装備セーブは未変更。次は歩行の左右位相、中割り、槍の両手構え、敵の動作。時計の検査合格を最終アニメ品質の合格と扱わない。P2は未完了で、20敵量産へはまだ進まない。

### 2026-09-23〜27 冒険改良P1（会話・GET・呪い）

- [実装・引き継ぎ正本](ADVENTURE_PRESENTATION_P1.md)。OP/冒険の紺金の顔付き会話、日本語文字送りSE、全文表示/ページ送り、GET専用階層と同時獲得の右列、呪い4種のHUDと効果/祝福の説明を実装。
- 4アトラスを生成しResourcesへ格納。サリア4表情、街4人、旅人5人、呪い4種。字幕OFFでもOP/必要な説明を表示。別作業の会話70%縮尺を維持。
- 2026-09-26の隔離QA: `tools/adventure-redesign-qa/2026-09-26/p1-final/`。32枚（1280×720/2556×1179）の描画検査passed、Play Modeの1連続シナリオpassed。同時GET/モーダル待機・復帰/BET終了/残高不変、呪いホバー・選択・固定・閉じる、OP全文・次行・スキップを確認。Core/Runtime/Testsコンパイルと配置検算成功。2026-09-27に記録整理。
- 2026-09-27追加検査: `tools/adventure-redesign-qa/2026-09-27/p1-reset-check/`。セーブリセット時の新マシンへHUDをつなぎ直し、古い呪いを残さない検査もpassed。お知らせdev 327。引き継ぎと素材台帳を保存。
- 未確認: 実機タッチ、実キー送信、聴感、配布ビルド、OP保存からの全分岐再開。次はP2の森・3敵・2武器の見本。敵20体/新主人公スプライト/武器表示/戦闘の波/HD2D描画まで完成したという意味ではない。

### 2026-09-25〜26 エフェクトの見本（本体は未変更）

- 火花＋12本（斬撃5・画面の揺れ・流線/集中線・ネガ反転・コイン・回復/攻撃/シールドのオーラ）を、本体に触らない作業場 `tools/fx-lab/` で描き出した。動画は `docs/art/2026-09-25/`。
- 本人の選択: 見本を先に作り合格したものだけ本体へ／画風は項目ごとに使い分け。12本の採否は未回答。
- 本体に要るエフェクトの棚卸し（無い・弱い箇所と、見本の当てはめ先）と次の手順: [FX_EFFECTS.md](FX_EFFECTS.md)。先に要るのは Canvas Overlay の上に演出を出す土台と URP のブルーム。

### 2026-09-23 冒険画面・戦闘の改良企画（作成当時の記録）

- 本人の依頼は改善の企画と他AI向けの手順化。正本: [ADVENTURE_REDESIGN_PLAN_2026-09-23.md](ADVENTURE_REDESIGN_PLAN_2026-09-23.md)。9月23日の企画作成時はゲーム未変更。その後のP1実装は上記を参照。
- 戦闘の波、敵合計20種案（通常16＋ボス4）、OP/冒険の顔付き会話と文字送りSE、GET描画順、サリアのスプライトと状態遷移、武器7系統、光/影/背景、呪い4種とホバー説明を設計。数量・敵名・性能目標は提案で未確定。
- 実装順: P0症状再現→P1会話/GET/呪い→P2小さな完成見本（森・3敵・2武器）→P3戦闘の波→P4全素材→P5背景30地点/負荷調整。各工程の変更先、素材契約、完成条件、次AIへの指示文を正本に記載。
- ソース/JSON確認済み: 敵6件は見た目3系統、OP短縮版29行、背景はRawImage。GETの原因と新描画方式の性能は実行検証前。実装開始時は現行コードを再確認し、OPの長い初稿や仮定の数値を採用しない。

### 2026-09-23 設定と今後のUI品質

- 本人指定: 以後のUI改修でも毎回言われる前に参考画像の質へ仕上げる。[AZURE_UI_QUALITY_STANDARD.md](AZURE_UI_QUALITY_STANDARD.md)を共通基準とし、AGENTS.md/CLAUDE.mdから案内。
- `AtelierSettings` を白い城塞・象牙色の面・紺と金・紋章パネル・ライブ字幕プレビューへ変更。ON/OFFと宝石スライダーを `AzureUiControls` へ共通化。タイトル960幅にも `AzureScreenFit` で対応。
- 正本: [AZURE_SETTINGS_DESIGN.md](AZURE_SETTINGS_DESIGN.md)。最終QA: `tools/azure-settings-qa/2026-09-23/v5-final/`（passed=true、24枚）。コンパイル・配置検算成功。お知らせdev 315。本番のセーブ/PlayerPrefsは検証に使用しない。実機タッチ・実キー送信・配布ビルド・聴感は未検証。

### 2026-09-22 装備画面

- `AtelierEquip` を月夜の城塞・紺と金・サリアの大きな立ち絵・中央一覧・右の比較へ改良。既存立ち絵から元気な5姿を採用し、左右で切り替え。装備とページは保持。
- 正本: [AZURE_EQUIP_DESIGN.md](AZURE_EQUIP_DESIGN.md)。最終QA: `tools/azure-equip-qa/2026-09-22/v3-final/`（passed=true、28枚）。着脱・比較・売却・工房品保護・満杯・ステータス復帰を隔離検証。コンパイルと配置検算も成功。お知らせdev 314。実機タッチ・配布ビルド・聴感は未検証。

### 2026-09-22 ステータス強化

- `StatsScreen` を宇宙背景・白金の中央面・3能力の紋章付きカードへ変更。ショップと冒険中で共用。参考画像の数値は採用せず実データを表示。
- 正本: [CELESTIAL_STATS_DESIGN.md](CELESTIAL_STATS_DESIGN.md)。最終QA: `tools/celestial-stats-qa/2026-09-22/v8-final/`（passed=true、18枚）。振り直しは費用を確認し、確定時に残高と割当を再確認。

### 2026-09-22 引き継ぎ・note整理

- 全履歴の正本: [AI_HANDOFF_HISTORY.md](AI_HANDOFF_HISTORY.md)。note用整理版: [note/AI_CHANGELOG_FOR_NOTE.md](note/AI_CHANGELOG_FOR_NOTE.md)。本人の原文: [note/MEMO.md](note/MEMO.md)。
- 2026-09-21〜22のショップは、1列・32px行・約10行表示のコンパクト縦一覧。最新QAは `tools/azure-shop-qa/2026-09-21/v8-compact/validation.json`。
- 会話の正確な日時がない過去の依頼は推測せず「会話上の順序（日時不明）」とする。実機、配布ビルド、プレイヤー評価は未検証。

### 背景制作の追記（2026-09-14）

- 30地点のうちC-1「霧の森」を新画風の遠・中・近3枚へ接続済み。Cloudy（曇り）を追加し全9天候。旧29地点は既存アトラスを継続使用。隔離Unityで12条件、正逆の周期境界、ゲーム画面2解像度を確認。EditModeは102件成功。
- 本人要望: アニメ寄りの画風を先に決め、横スクロールのつなぎ目をなくし、Unityで時間・天候・ステージ状態を変えられる構成へ。初回3案は未採用。参考画像3枚から作ったv2へ「近づいているので進めてください」と回答があり、**v2の方向で制作継続を承認済み**。
- 正本: [背景の画風と制作引き継ぎ](BACKGROUND_ART_DIRECTION.md)。既存実装: [ADVENTURE_ENVIRONMENTS.md](ADVENTURE_ENVIRONMENTS.md)。C-1新素材は黒マットRGBをシェーダーで透過する方式（実アルファ生成は失敗）。検証ツールは `tools/environment-qa/`。
- [確認ページと再実行方法](../tools/environment-qa/README.md)。次はA-1の開けた風景、E-2/H-1の暗所と発光。URP Play Mode・端末実機・GPU負荷・配布ビルドは未検証。
- 背景以外も常にMDへ現状・確定事項・未決定事項・確認結果・次の作業を残す。ルートの [AGENTS.md](../AGENTS.md) に記録手順を追加。
- この追記より下の全体情報は2026-09-10時点を含むため、対象コードと各セクションの新しい記録を優先する。

---

## 0. 30 秒で掴む

パチスロを土台にした RPG。リールを回して敵と戦い、章立てのマップを進む。

- Unity 6.3（6000.3.15f1）/ Universal 2D / uGUI をコード生成（プレハブなし）
- 場所: `UnityProject/BigBonusBlitz`
- 実行の入口: `TitleScreen`（`RuntimeInitializeOnLoadMethod` で自動起動）→ `MapScreen` →（冒険）→ `GameController`
- **数値は全部 `Assets/Resources/Data/*.json`。コードに確率を直書きしない**（本人の指示）
- 出玉バランスの最終調整は**システムが揃ってから**まとめて行う（本人決定・2026-09-10）

---

## 1. 遊びの流れ

```
タイトル ─→ ミニマップ ─┬─→ 街（ショップ: 装備/スキル・補給・ステータス）
                        └─→ 冒険（スロット本編）
                                 │
                    ステージを進む（8 段・枝 1〜8 本）
                      条件を引けば前へ／落とせば 1 つ戻る
                                 │
                    ボーナス → AT「洞窟」→ 特化ゾーン／狩猟
                                 │
                    ライフ切れ・エンバー切れ・章クリアで街へ
```

### 通貨と資源

| 名前 | 役割 | 増える | 減る |
|---|---|---|---|
| エンバー（灯火） | 1 回転の燃料。内部名は `Credit` | 役の払い出し、街で購入 | 1 回転 3。リプレイは 0 |
| ソウル | 成長の対価 | 敵の討伐、宝、章クリア | 街での買い物 |
| ライフ | 探索できるゲーム数 | **通常時のリプレイ +3**、ボーナス中のベル、街で回復薬を購入、宝、章クリア | 通常時 1G ごと |

**HOLD**: ボーナス中・AT 中・持ち越し中はステージのG数もライフも止まる（**回復もしない**）。
ステージ札が鎖と南京錠で封じられ「H O L D」と出る（`SlotMachine.StageHeld`）。

---

## 2. いま動いているもの

### スロット本体
- 3 リール・最大滑り 4 コマ。持ち越しボーナスを狙うGだけ 20 コマ引き込み（`bonusPullInSlip`）
- ボーナス当選 → 3〜6G の前兆 → 最後のGは擬似遊技で自動的に揃う。目押しで前兆を飛ばすことも可能
- 設定 1〜6。モード A〜D と天井（`ceilings`）

### ライフ（探索できるゲーム数）
- 通常時は 1G で 1 減る。0 で力尽き、章の最初へ戻る
- 回復は 3 つ: **通常時のリプレイ +3** / ボーナス中のベル（30% で +8）/ 街で回復薬を買う
- 数値は `game_config.json` の `adventure.resource`。エディタの「資源と帰還」から直せる
- 実測（設定1・ナビ従・50万G）: 実質の減り **0.644 / 通常1G**、力尽きるのは通常 3,303G に 1 回
  （リプレイ回復を切ると 0.854 / 1,278G に 1 回）

### エネミーエンゲージ
- ワークフロー抽選で敵が出現。**リプレイが出どころの 47%**（ハズレ 41%、高確ステージ 7%）
- **ターン制**（2026-09-21、`engage`。Engage.cs）。1 セット = 2 G × 3 セット。倒せなければ逃げる
  - 1 ターン目: ハズレ→敵の攻撃の構え / 小役→力を貯める / レア役→攻撃確定＋ポーション
  - 2 ターン目: 敵の攻撃 → ハズレ被弾（LIFE −3）/ 小役回避 / レア役カウンター。力を貯めた → 小役中攻撃 / レア役大攻撃 / ハズレは被弾（防御・回避 15% ずつ）。攻撃確定 → 中攻撃、レア役で大攻撃
  - 敵は HP 100（`engage.enemyHp`、敵ごとは `engageHp`）。攻撃が通ればダメージ（中 35 / 大 70 / カウンター 50 + 装備・技能）。0 でその場で討伐
  - 3 セットで削り切れなければ **ジャッジ**の 1 G: 倒せる率 = 削った割合 % + 小役 10 / レア役 40（上限 95）。外れれば逃走
  - ポーション: 次の G は追加の 1 回転（ルーレット）。ハズレ LIFE +3 / 小役 次が大攻撃 / レア役 討伐確定
- ベルで択ナビ（左右どちらかが正解）。正解もただの小役（2026-09-23）、外すとこぼれてハズレ
- 討伐でソウルと EXP。中ボスはボーナス中に出る

### ボーナス
- BB / RB。**G 数で終了**（BIG 60G / REG 30G、`bonusGames`）。規定枚数（BIG 300 / REG 90 純増）に先に届けばそこで終了
- 中は AT 期待度が貯まる。枠の色（白→青→黄→緑→赤→虹）とテロップで見せる
- ベルは**押し順ナビ**（60%）。従えば 11 枚、外せば 3 枚。ナビ無しは共通ベル 8 枚（`bonusBell`）
- ベルで 3 体斬り演出（1 停止ごとに 1 体、最後にポーズ、PUSH で Chance）

### AT「洞窟」
- **セット継続型**。1 セット 50G、終わるたびに継続を抽選（継続率 73%）
- 押し順ナビ 89%（正解 **11 枚** / 外し 3 枚）、残りは**共通ベル 3 枚**
- AT 中のテーブルはリプレイを厚くして投入を下げている（リプレイ 40% / ベル 41%）
- 実測: **1 回 208G（中央 157G） / +512 枚 / 純増 +2.46 枚/G**
- 分布: 100G 以上 68% / 200G 以上 39% / 400G 以上 12% / **600G 以上 4%**
- **特化ゾーン 4 種**（`at.zones`）。AT の 6% で滞在

| ゾーン | 効果 | G数 |
|---|---|---|
| 灯の間 | ナビ率 +25% | 10 |
| 爆進ラッシュ | ナビ率 +30%、正解が +5 枚 | 8 |
| 上乗せの祭壇 | 毎G 14% で +3G | 8 |
| 珍獣バトル | 毎G 14% で狩猟。モンスター固定 | 10 |

- 狩猟（モンハン型）: HP を削る。討伐で上乗せ。フラグ基準でダメージ

### 冒険（ステージ）
- 第 1 章 = **8 段 × 枝 1/2/4/6/8/5/3/1 の 30 ステージ**
- 3 段階の条件で、次の段のどの高さに出るかが決まる

| 条件 | 優先度 | 行き先 | 達成率の設計値 |
|---|---|---|---|
| 上の道（レア役） | 30 | 1 つ上の枝 | 8% |
| 同じ高さ | 20 | 同じ枝 | 20% |
| 下の道 | 10 | 1 つ下の枝 | 25% |
| 何も引けない | — | 前の段へ戻る | — |

- 実測の前進率 **64%**、章あたり 186G、後退 0.1 回（深度スケーリング込み）
- 上の枝ほど高確・宝・報酬が良い。初到達だけソウルが出る
- 章クリア報酬は引き返した回数だけ目減り（1 回 12%、下限 30%）

### 装備ドロップ（潜行ごとに拾い直す）
- 敵・ボス・狩猟・宝箱から落ちる（`drops`: 装備の率と、ソウル / エンバー / 回復薬 / 地図 / お守り / 竜の鱗などの落とし物。ボスは装備が必ず落ち、深さ +3 でレア度が上がる）。**街に戻ると装備は流れる**（恒久はソウル・ステータス・ショップだけ）
- 枠 8 つ（頭 / 体 / 手 / 足 / 武器 / アクセ ×3）。品の種類は 6（アクセは 3 枠のどれにでも）。ベース 40 種 × 接辞 16 種
- 効果にステータス（ライフ / テクニック / ラック +n）がある。装備込みの値は `LifeStat / TechniqueStat / LuckStat`
- 一覧表は `docs/equipment.md`（`py -3 docs/gen_equipment_table.py` で JSON から作り直す）
- レア度 9 段階（白 コモン / 青 アンコモン / 黄 レア / 緑 スーペリア / 赤 エピック / 紫 ミシック / 黒 アビス / 金 ディヴァイン / 虹 プリズム）。**深いほど上位が出る**（深さ 8 でアビス以上 1.6%）。プリズムは名前と枠が虹色に回る（`RainbowTint`）
- 鞄 16 個。いっぱいなら一番弱いものと自動で入れ替える。空き枠があれば拾った品を自動で着ける
- 装備画面は右下の装備ボタン（E キーも）。冒険中いつでも開ける。左に 8 枠（2 列）、右に鞄、下に詳細
- 昔の部位名（armor / trinket）はセーブを読むときに体 / アクセへ読み替える
- 実測: 25 万Gで拾い物 1943 個（並 58% / 上質 29% / 稀少 12% / 伝説 2%）

### 実績（トロフィー）
- 26 個（`Resources/Data/achievements.json`）。回転数・BIG/REG・洞窟・章クリア・討伐・宝・装備・押し順・レベル・最深部・ソウル累計・力尽き（隠し）
- 数えものはプレイヤー単位で永続（セーブの `achievements`）。解除で報酬のソウル
- 右下の ★ ボタンで一覧（解除 n / N、進み具合のバー、報酬）。解除時は舞台の上に金の帯

### 呪いと祝福
- 段に進むと 35% で「受けるか断るか」を提示（深さ 2 以上、同時に 4 つまで）
- 受けると**呪い 1 つと祝福 1 つが同時に付く**。深いほど両方が強い
- 呪い 4 種: ライフの減りが速い / ルートの必要回数 +1 / 払い出し減 / 1 回転の持ち出し増
- 祝福 6 種: 装備ドロップ率 / ソウル / 討伐率 / AT 期待度 / 回復薬 1 個のG数 / AT 初期G
- 街に戻ると流れる

### 深度スケーリング
- 段が深いほどルートの必要回数が増える（深さ 5 以上で +1）
- 初到達の報酬は深さ 1 段ごとに +14%
- 装備のレア度も深さで上がる

### ステータス（レベルアップで 2 ポイント、各上限 20）

| 系統 | 効果 |
|---|---|
| ライフ | BET が無料になる率、回復薬 1 個のG数、力尽き時の補填 |
| テクニック | エンゲージのG数、討伐率、狩猟ダメージ |
| ラック | 通常時のハズレがレア役/リプレイに化ける、宝の発見率 |

ラックは**既存の確率テーブルを書き換えず、ハズレ枠だけを分け直す**（設定差を壊さないため）。

### 街
- ショップ 4 タブ: 装備・スキル / 補給 / **工房** / ステータス
- **工房**（2026-09-21、`craft`）: 図面 50（6 部位 × 段 I〜IV）をソウルと素材（鉄鉱 / 獣の革 / 魔石 / 竜の鱗。落とし物 kind "material"）で作る。作った品は恒久・接辞なし・売れない・鞄の数に入らない。段は章の踏破で解放。表は `docs/shop_equipment_plan.md`
- 装備 7 品（アイコン付き）。回復薬と路銀（エンバー）を買う。ステータスの振り直し

### 物語
- **序章**（2026-09-21、`story.op`、`docs/scenario_op.md`）: 新しいゲームだけ。逃走 → 灯が尽きて倒れる → 暗転で誰かが助ける → 宿屋で目覚める → 灯守・商店・門の台詞 → 最初の回転（ベル）→ 本編。`OpeningPlayer` が台詞窓。飛ばせる
- テーマ（問い）: **「引き返すことは、負けなのか」**。プレイが頻繁に引き返す構造なので、それを主題にした
- 骨格: 世界は「灯」で動く。街で灯を分けてもらい、竜の巣の「消えない灯」を確かめに行く
- **段（8 つ）× 枝の高さ（上/中/下）で台詞を出し分ける**。上の枝ほど世界が応え、下ほど冷たい
- 節目の台詞: 章の始め / 引き返した / ライフ切れ / 力尽き / 踏破
- 第 2 章「分けた灯」（問い: 灯を分けると、自分の灯は減るのか）。同じ地図を二度目に通る話
- 周回で「また来たのか」と世界が覚えている（2 / 3 / 5 周目。第 3 章以降は第 1 章の台詞に戻る）
- データは `game_config.json` の `story`。エディタの「物語」タブで編集
- 台詞の一覧表は `docs/story.md`（`py -3 docs/gen_story_table.py` で JSON から作り直す）
- 分岐構造は**真珠の首飾り + 状態集計**。完全分岐は書く量が爆発するので採らない

### 演出
- 役ごとの事前予告と当選時の演出、揃った位置の点滅
- 払い出しで**中リールから火の玉がエンバーの数字へ吸い込まれる**（`UiFx.Absorb`）
- ソウルは**敵 → 主人公 → SOUL の数字**の二段で吸い込む
- ナビのバッジは押すと 1.55 倍に膨らんで消える
- アイコンは全部**距離関数で手続き描画**（`UiSkin.Icon`）。画像素材なし

---

## 3. ファイルの地図

### Core（`Assets/Scripts/BBB.Core/` 約 3,600 行）
MonoBehaviour に依存しない。テストとハーネスはここだけ見る。

| ファイル | 中身 |
|---|---|
| `SlotMachine.cs` | 本体。BET → レバー → 停止 → 判定。すべての状態を持つ |
| `Adventure.cs` | ステージ、ルート条件、宝、資源（ライフ）、前進/後退 |
| `AtMode.cs` | AT の設定、モンスター、**特化ゾーン**、狩猟 |
| `AtDirector.cs` | ボーナス中の AT 期待度とランク |
| `PlayerStats.cs` | ライフ / テクニック / ラック |
| `Shop.cs` | 街で買う装備・スキル、財布、効果の合計 |
| `Equipment.cs` | 落ちる装備の生成・鞄・装備中の効果 |
| `Curse.cs` | 呪いと祝福 |
| `EnemyEngage.cs` | 敵テーブルの選択と討伐抽選 |
| `WinEvaluator.cs` / `WinCombos.cs` | 役の判定。5 ラインとチェリー |
| `SlipController.cs` | 先読みの滑り制御 |
| `LotteryTable.cs` | 分母 65536 の抽選表 |
| `WorkflowLottery.cs` | 役ごとの演出・敵出現の抽選 |
| `HeroDirector.cs` / `TravelerDirector.cs` / `PrecogDirector.cs` | 台詞、旅人、予告 |

### Runtime（`Assets/Scripts/BBB.Runtime/` 約 7,000 行）

| ファイル | 中身 |
|---|---|
| `GameController.cs` | スロット画面。UI の生成と全演出。**一番大きい** |
| `MapScreen.cs` | ミニマップ。街と冒険の分岐 |
| `ShopScreen.cs` | 街の 3 タブ |
| `TitleScreen.cs` | タイトル |
| `StageMapView.cs` | ステージマップの描画（枝の数で丸が縮む） |
| `UiSkin.cs` | 手続き描画のスプライトとアイコン、部品 |
| `UiFx.cs` | パーティクル、吸い込み、カットイン |
| `SafeStage.cs` | セーフエリア → 960×540 の舞台 |
| `AudioManager.cs` | BGM/SE。**画面をまたいで 1 つだけ** |
| `SfxSynth.cs` | 素材の無い効果音を合成 |
| `SaveData.cs` | PlayerPrefs 保存。音量と操作は別キー |
| `GameDataLoader.cs` | JSON 読み込み（`ObjectCreationHandling.Replace` 必須） |
| `SlumpGraph.cs` | 差枚グラフ |
| `EquipScreen.cs` | 装備画面（紙人形 8 枠 + 鞄 + 詳細）。冒険中は E / 右下、街は右下の「装備」 |
| `TrophyScreen.cs` | 実績と図鑑の窓（実績 / 装備の図鑑 / お宝の図鑑）。冒険中は ★、街は右下の「実績」 |
| `StatsScreen.cs` | ステータスを振る板。街のショップのタブと、冒険中の装備画面の「ステータス」から同じ板 |
| `CurseScreen.cs` | 受けている呪いと祝福の一覧。装備画面の「呪いと祝福」から |
| `IconText.cs` | 文字の中にアイコンを混ぜて 1 行に並べる（"{soul}+90"）。メッセージ・帯・説明の吹き出しで使う |
| `MarqueeBand.cs` | 文が流れ続ける斜めの帯（エンゲージ中。Web 版の敵出現バナーの写し） |

演出と図柄の確認は `tools/fx_viewer.html`（演出を再生して数値を決める。数値は game_config.json と同じキー）と `tools/symbol_viewer.html`（図柄の候補・回転・点滅）。ブラウザで開くだけ。
| `RunIO.cs` | 潜行中の持ち物をセーブに載せる橋渡し |

### データ（`Assets/Resources/Data/`）
- `game_config.json` — ほぼ全部の数値（33 キー）
- `workflow_config.json` — 役ごとの演出・敵出現
- `enemy_tables.json` — 敵 6 種（雑魚 3 / 中ボス 3）

### エディタ
`Assets/Editor/ProbabilityEditorWindow.cs`（BBB > Probability Editor）。**12 タブ**で全テーブルを編集。

---

## 3.5 別の PC で開くとき

**Unity のバージョンは 6000.3.15f1 で固定。** 違う版で開くと全アセットが作り直され、
`ProjectVersion.txt` が書き換わって他の PC も巻き込む。

作業前に必ず:

```bash
git pull --rebase
git lfs pull
py -3 docs/check_project.py
```

`git lfs pull` を忘れると画像と音の実体が来ず、絵が出ない。
`check_project.py` は Unity のバージョン、git の遅れ、LFS の実体、`.meta` の欠け、
パッケージの固定版を一度に見て、ずれていれば直しかたを出す。

同期されないもの（`Library/` / `UserSettings/` / セーブデータ）と詳しい手順は `docs/SETUP.md`。

---

## 4. 検証のしかた

Unity を開かずに確かめる。**変更したら必ず全部走らせる。**

```bash
# scratchpad の場所（セッションごとに変わる。無ければ作り直す）
S="$LOCALAPPDATA/Temp/claude/D--Mao-PC-Github-BigBonusBlitz/<session>/scratchpad"

# 1. 4 アセンブリのコンパイル（Unity を開かずに）
for p in corecheck runtimecheck testcheck editorcheck; do
  (cd "$S/$p" && dotnet build -nologo)
done

# 2. 100 万Gの通しテスト（不変条件・出玉・冒険・AT）
(cd "$S/harness" && dotnet run -c Release)

# 3. UI の重なり検算
py -3 docs/check_layout.py
```

`corecheck` などは Unity のソースを直接コンパイルする csproj。`harness` は `corecheck.dll` を参照する
コンソールアプリで、押し方 4 通り × 25 万G を回して毎G 不変条件を確認する。
**この 3 つが通らないものはコミットしない。**

---

## 5. 決まっている作法

### 本人の指示
1. **全イベントは確率テーブルで管理**。コードに確率を直書きしない。新しい抽選を足すときは必ずテーブルとエディタのタブも作る
2. **報告は 3 行以内**。変更したファイルと確認待ちだけ
3. 出玉バランスの調整は最後にまとめて
4. 案を出すときは候補 3 つ以上・前例・死に筋・参入障壁・決め手の 5 点で
5. **このプロジェクトを触ったセッションは、終わりに `docs/note/MATERIAL.md` へ材料を追記する**。
   制作過程を有料 note として売る案件が並走している。失敗・数字・決定は記事の材料になる。
   入口は `docs/note/README.md`（1件3行で足りる。整形はしない）
6. **画面を変えたら、その画面を1枚は描き出して残す**（本人指示・2026-09-14）。
   `ui_rules.md` 9 の batchmode 描き出し（`tools/<画面>-qa/*.png`）は消さない。scratchpad に描いたものは
   `docs/art/<日付>/` へ写してからセッションを終える。見本・比較・3案ボードも同じ。
   ブラウザやデスクトップのスクショはファイルに残らないので、残したいものは描き出しにする。
   制作過程の note が `tools/*-qa/` と `docs/art/` から拾う（`docs/note/images/README.md`）

### UI（`docs/ui_rules.md` に全部書いてある）
実際に事故った 8 項目。**新しい要素を足したら上から当てる**。特に:
- 端から順に領域を取り、隣の位置を前の要素から計算する。**アイコンも要素**
- 動く背景の上に半透明の札を置かない。マスクの内側に情報を置かない
- `RefreshUi` の中で `Find` を使わない（例外が出るとボタンの更新に届かない）
- 静的キャッシュに破棄チェックを入れる

### 道具の癖
- `python` は動かない。**`py -3`** を使う
- ヒアドキュメントの Python は改行やエスケープが壊れる。**`.py` に書いて実行**する
- `sed` の `\U` `\L` は Windows パスを壊す。`cygpath -m` を通す
- JSON は `ObjectCreationHandling.Replace`。既定値のあるリストが二重になる

---

## 6. 未解決のもの

| 件 | 状態 |
|---|---|
| 機械割 | 設定 1 で 165.3%（ナビ従・200 万G・2026-09-10 実測）。素の 100% 化は最後にまとめて |
| 設定差 | 設定 2〜4 が同じ 167.0%。設定 5 が 142.2%、設定 6 が 150.1% で**設定 1 より低い**。テーブルの作り直しが要る |
| スライム | 通常プレイで出ない。ワークフローが該当の敵種別を出していない |
| 第 3 章以降 | 未作成。第 2 章をクリアすると第 1 章の台詞に戻る（周回の台詞つき） |
| ステータス画面 | 街からしか開けない。冒険中にレベルが上がっても振れない |
| スランプグラフ | 保存されない。街に戻ると消える |
| ストーリー | 第 1 章（68 行）と第 2 章（62 行）。第 3 章以降は未着手 |

---

## 7. 直近で直した不具合（同じ轍を踏まないため）

- **リプレイでエンバーが減っていた**。再遊技になっていなかった
- **剣の向きが生成モデルの指示に従わない**。剣は描かせず、別レイヤで重ねる方式にした
- **スプライトが甘い原因が後処理にあった**。192 ドット・32 色に落としていた
- **オートが AT のナビを見ていなかった**。外し続けて 15 枚が 3 枚に
- **AT の特化ゾーンが強すぎて AT が終わらなかった**（1 回 3669G）。上乗せ率と突入率を絞った
- **画面更新の途中で例外**。ボタンの更新に届かず「ボーナス後に BET が押せない」
- **札が背景に埋もれる**。マスクの内側に置いていた
- **アイコンが文字に重なる**。領域を取らずに置いた
- **枝の端に到達できない**。段ごとの本数が違うのに添字をそのまま使っていた
- **ソウルが結果に載っていなかった**。技術介入と任務ぶんが財布にだけ入り、吸収の演出が出なかった
- **骨格の手足が上下逆に生えていた**。角度の符号を間違えて、体が枠の上半分に潰れていた（`tools/comfy`）
- **コマごとに背丈が変わっていた**。1 コマずつ自分の外接枠で正規化していたので、剣を上げたコマほど小さくなる

---

## 8. テスト

`Assets/Tests/EditMode/` に 66 本。Unity の Test Runner（EditMode）で走る。

| ファイル | 本数 | 主な内容 |
|---|---|---|
| `SlotMachineTests.cs` | 19 | BET、引き込み、前兆、AT、4 通りの押し方での通し |
| `AdventureTests.cs` | 16 | ステージ移動、条件、ライフ、力尽き |
| `WorkflowAndEnemyTests.cs` | 15 | 抽選、敵、ショップ |
| `PlayerStatsTests.cs` | 9 | ステータスの効果と上限 |
| `LotteryTableTests.cs` / `SlipControllerTests.cs` | 8 | 抽選表、滑り |


### 2026-11-03 内蔵画像生成を再開、コウモリ8姿勢を本編接続（dev 405）

コウモリ6動作を既存遭遇へ接続し、隔離Unityの96連番・本編回帰1件/0失敗・2解像度を確認。灯蛾と5武器は生成候補まで。全20敵/7武器/背景/P2〜P5は未完了、最終美術未承認。[制作・検証記録](art/2026-11-03/BAT_RUNTIME_AND_ASSET_PRODUCTION_REVIEW.md)。

### 2026-11-05 敵20種・7武器・新背景3地点を展開（検証継続）

20敵の本編回帰22件/0失敗と206枚を確認。大槌・弓・杖の専用姿勢と飛び道具着弾待ちを実装し、7武器の単体描画238枚と取消/装備変更検査は成功。目視で大槌の握り前後とワイバーンの上端切れを修正し、本編再検証中。草原・結晶洞・竜の巣の3層素材を生成・接続。背景実描画、残り26地点、最終美術、実機、全計画完了とpushは未完了。[正本](art/2026-11-05/ROSTER_WEAPONS_ENVIRONMENT_REVIEW.md)。


### 2026-11-06 統合回帰と背景展開を継続

20敵と7武器の本編検査23件/0失敗、武器7系統の単体描画238枚を確認。隔離releaseの全EditModeは136件中135件成功・戦闘帯1件失敗。未取り込みだった既存MarqueeBand改修を関連テストと揃え、release-v2の該当1件は成功した。未知敵IDのnull返却と既存TryEnemySpritePath APIもpush先から保持。コンパイル3種/100万G/配置検査成功。

新画風の背景は既存C-1に加え、A-1/B-1/B-2/C-2/E-2/H-1の実アルファ3層を接続。H-1の端の垂直切れを原画v3で修正。追加分の時間・天候・周期・本編2解像度をnative-v3で検証中。残り23地点、敵の遠隔攻撃、支持足などは継続。機械検査の成功と最終美術・実機・配布ビルドの承認は別。共有index/本番セーブは保護。コミット/pushは隔離checkoutで準備中。

詳細: [制作・失敗・検証の正本](art/2026-11-05/ROSTER_WEAPONS_ENVIRONMENT_REVIEW.md)。


### 2026-11-06 push前の検証確定

戦闘帯回帰1件、未知ID/ロスター等27件は失敗0。背景native-v4は84条件/126周期/30地点読込/2解像度、264枚を成功。空色の下端帯を配置で除去し、B-1/B-2/C-2/H-1を目視確認。隔離releaseでcompile3種/100万Gも成功。新画風7/30地点、C-3候補のみ制作。残り23地点等は継続。詳細は[統合レビュー](art/2026-11-06/RELEASE_INTEGRATION_REVIEW.md)。
