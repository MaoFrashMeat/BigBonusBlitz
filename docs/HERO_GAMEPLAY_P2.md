# 新サリアの本編接続

### 2026-10-18 BET停止フェーズ/cue境界を全sampleで検査（dev 395）

`AdventureHeroValidation.RunAttackSequenceComparison`の隔離QAを強化。剣/槍ともBET〜第一停止までf1、第一〜第二停止までf2を120Hzの各sampleで保持し、anticipationはf1、swing/contactは第二停止後のf3/4で各一回出ることを確認。attempt08は攻撃135枚/接近158枚、実BET EditMode1 scenario、source/isolated manifest一致がPassed。verify harnessもcompile/100万G Passed。

検品中の槍pose32の再利用、ゲーム数値/抽選/HP/報酬/保存/素材は変えていない。このassertは状態遷移の機械ゲートであり、同じ姿勢を二停止で使う原画意図や第一停止の美術を承認しない。実入力/聴感/実機/配布build/最終美術は未確認。[レビュー](art/2026-10-18/HERO_ATTACK_SEQUENCE_PHASE_GUARD_REVIEW.md)


### 2026-10-18 通常BET攻撃を全段計測（dev 394）

隔離UnityでBET/f1、第一停止/f2、第二停止/f3-4、接触、復帰を120Hzで剣/槍各一巡記録。原画姿勢とcontact一度、walk復帰はPassed。attempt04はQA側で槍f1期待poseを誤って剣pose24に固定して失敗、槍pose32へ直しattempt06でPassed。製品の問題ではない。槍はf1終端とf2が同じpose32で、第一停止に姿勢変化がない。新原画を作らず、ゲーム処理/抽選/報酬/セーブも変更していない。美術判断と実機は未承認。[レビュー](art/2026-10-18/HERO_ATTACK_SEQUENCE_REVIEW.md)

### 2026-10-18 接近終端の攻撃ポーズへ切替を測定（dev 393）

120Hz隔離Unity traceで0.3583秒pose18→0.3667秒剣26/槍33のhandoffを記録。contactは攻撃原画時刻で一回、BET回帰と復帰もPassed。切替が一段で支持足/中割りは未承認。QA初回失敗はイベント時刻を考慮しないassert、修正後passed。セーブ/戦闘数値不変。[レビュー](art/2026-10-18/HERO_APPROACH_WALK_CADENCE_REVIEW.md)

### 2026-10-18 接近歩行を原画コマ時間で再生（dev 392）

0.36秒の接近中に8コマを圧縮せず、JSONの0.11秒/コマに従って描画する。attempt22で剣/槍ともpose15→16→17→18の時刻ゲート、接近中のcontact抑止/戦闘時計停止、実BET contact/recover回帰を確認。機械的な速度整合で、歩行→攻撃間の原画品質や支持足/足滑りの承認ではない。数値/抽選/HP/報酬/セーブは不変。[レビュー](art/2026-10-18/HERO_APPROACH_WALK_CADENCE_REVIEW.md)

### 2026-10-16 剣攻撃の踏み込みを実リールで確認（dev 389）

- **確認:** 隔離実リール試験へ剣のとどめ接近を追加。剣先Xとゴブリン表示中心Xの差0px、contact1回、敵の決着までPassed。既存の槍通常攻撃踏み込みも同シナリオでPassed。
- **再現性:** `run_runtime_approach_qa.ps1` が本編/隔離の対象6ファイルSHA256を `source-manifest.json` に保存。
- **境界:** ランタイム/敵表/乱数/ダメージ/報酬/保存変更なし。水平sprite先端の計測であり、縦の命中や美術承認ではない。実機/URP/配布ビルド/聴感未検証。[レビュー](art/2026-10-16/HERO_SWORD_APPROACH_REEL_REVIEW.md)

### 2026-10-16 攻撃時の踏み込み（dev 388）

本編設定で攻撃前の水平接近を有効化（上限600px、0.36秒）。GameControllerが渡す現在の敵Transformへ向かい、contact前に到達し、recoverで開始位置へ戻る。既存の剣/槍姿勢とslime/goblinが対象。隔離Unityでsource pose比較126枚、低FPS/取消/実敵槍contactを検証。槍スライムの接触位置は頭上寄りのため見た目は未承認。候補敵高さ補正は本編source poseに合わないので未採用。敵3種/20敵/全武器/最終美術の完了ではない。[レビュー](art/2026-10-16/HERO_ATTACK_APPROACH_INTEGRATION_REVIEW.md)

### 2026-10-03 剣の構え・復帰で刀身と握りを見せる（dev 346）

- `IMPLEMENTED`: 剣の待機/構え入口と復帰2コマに前景の手を追加。元PNGの小矩形を同じ座標で重ね、体→剣→手の描画順と-55度の角度で、頭から刀身だけが出る状態を修正。他ポーズと槍は従来順へ戻す。
- `VERIFIED`: 隔離Unityで既存/修正184枚、無音動画2本。手と柄の位置・描画順・切替時の非表示を検査。実BETの主人公/敵2シナリオPassed。Core/Runtime/Testsコンパイル、配置検算成功。正本: [握りの描画レビュー](art/2026-10-03/GRIP_LAYER_REVIEW.md)。
- `UNVERIFIED`: 歩行の左右脚/頭身差、動作全体の最終美術、全武器、20敵、光影、実機/聴感は未完了。歩行候補3案は未採用のまま。新規生成やPNG加工なし。本番セーブ保持、コミット/push/公開なし。

## 2026-10-03 攻撃中の復帰予約でcontactを失わない（dev 344）

- 原因: `AdventureHeroPresenter.Request`が全要求で世代番号とImpactReadyを初期化していた。Playerは攻撃中のwalk/idle/focusを復帰予約として保持するため、描画は続くのにGameControllerのcontact待機だけが無効になる不整合があった。
- 修正: 新しい動作の開始と、現在動作を保持する復帰予約を区別。予約では世代/contactを維持し、明示的なhit/攻撃/Cancel、保持解除、新しいループ開始では更新する。抽選・HP・報酬・保存処理は変更していない。
- 検証: `tools/hero-gameplay-qa/2026-10-03/v1-contact-resume/play-tests.xml` は隔離shop-qaのEnterPlayModeシナリオ1件Passed。実BETの剣/槍、歩行の位相保持、攻撃中の装備変更予約、contact前後の復帰予約、明示的な被弾割込み、モーダル停止/再開を確認。前回の未検証だった武器切替検査を今回実行した。
- 実BET回帰: 同フォルダ`enemy-reels.xml`のAdventureEnemyReelTestsも1件Passed。Hit/Guard/Dodge/Counter/Attack、剣/槍contact、Judge、Potion3分岐、説明停止/復帰/取消、敵交替を通過。新規2テスト合計ではなく、各1件の長いシナリオ計2件である。
- 描画: 同フォルダ`render/`の54枚/2解像度はpassed。剣/槍の予約中contactとidle復帰を追加し、両武器の接触姿勢と槍の復帰姿勢を目視。静止画と機械検査の合格であり、歩行の足滑り・中割り・頭身差を最終承認したわけではない。
- 新規生成画像なし。既存原本を使用。コンパイル/配置検算/fx同期は成功。QA専用セーブを使用し、本番セーブ、コミット、push、公開を行っていない。
- 次: 歩行→構え/被弾→復帰の連続描画と武器別リーチの美術調整。20敵ロスターは企画書の通常16＋ボス4とdev343の候補差を照合してから量産する。実機/聴感/全武器/光影/計画全体は未完了。

2026-09-28 / Asia/Tokyo。関連: [P2原画](HERO_PRESENTATION_P2.md)、[補助動作と音](HERO_SUPPORT_P2.md)、[リール契約](HERO_REEL_BRIDGE_P2.md)。

## 現在の範囲

新サリアの体と別描画の剣/槍を本編GameControllerへ接続。`hero_presentation_prototype.json` の `gameplayEnabled=true` が起動時の既定。見本だけだった前段から、本編で表示する段階へ進んだ。敵3体は引き続き検品画面だけ。20敵、戦闘の波、全武器、照明、最終アニメ品質の完了ではない。

新規の画像生成は行わず、前段で保存した43ポーズ/20クリップの原本を使用。原本とプロンプトはHERO_SUPPORT_P2から辿れる。

## 接続方法と守った契約

| 変更先 | 役割 |
|---|---|
| `Scripts/BBB.Runtime/AdventureHeroPresenter.cs` | 本編要求→表示時計。装備IDの変化だけを反映、リール以外の構え解除 |
| `Scripts/BBB.Runtime/GameController.cs` | BuildUiで接続、PlayCharacterを転送、RefreshUi/Updateで装備同期、Updateで時刻を進める |
| `Scripts/BBB.Runtime/HeroPresentation.cs` | 設定にgameplayEnabled/gameplayHeightを追加 |
| `Resources/Data/hero_presentation_prototype.json` | 本編ONと身長170。検品画面の身長220は独立 |
| `Editor/AdventureHeroValidation.cs` | 本編uGUIの46描画と全表示要求/装備/切り戻し検査 |
| `Tests/EditMode/AdventureHeroPlayTests.cs` | Play Modeの実BET/任意停止順/保存再読込/停止復帰 |

パスは `UnityProject/BigBonusBlitz/Assets/` 配下。

- 既存 `_charRt` は位置・回転・移動・エフェクトの親として保持。新viewはその下へ置き、足元を親の下端へ合わせる。旧ImageとSpriteAnimatorだけを無効にして二重描画を防ぐ。キャラ画像のRaycastを切り、背後の冒険操作を妨げない。
- idle/walk/focus、attack-f1/f2/f3-4、slash/cast/guard/hit/victoryの11要求を転送。EnemyActive/PrecursorRemaining時のwalk→idleは本編側の既存処理を保持。背景の歩行状態は実際の表示時計から同期する。
- ベルのBETと第1停止は構えを保持、第2停止で斬撃。停止数で進む既存ロジックを変更しない。重複停止/第3停止で新しい攻撃を足さない。
- attack-f1はリール以外の演出にも使われる。非リール時にwalk/idle/focusを要求されたら保持を解除する。有限の攻撃中にループ要求が来た場合は、最後のループ要求を記憶して復帰後に流す。
- 装備は `_m.Equip.WornOf(EquipSlot.Weapon)` を読み、craftId→baseId→既定剣の順で外見を選ぶ。攻撃中の変更は復帰まで予約。同じ外見IDの再同期は歩行の位相を戻さない。無装備/未対応の武器は現状は既定剣。全武器の見た目が揃ったという意味ではない。
- 設定などのモーダル/呪い選択/OP会話が実際に見えている間は主人公の時計を停止。再開後に続きから流す。OpeningPlayer本体が残るだけの逃走操作中は動かす。背景の自動時刻や既存FX全体の停止仕様は今回の範囲外。
- **本編の音はGameController/既存FXが所有**。AdventureHeroPresenterはCueを音・ダメージ・報酬へ接続しない。第2停止のAttack/Voice、防御など既存音をそのまま使う。検品画面の動作音を二重に足さない。専用足音と聴感調整は残る。

## 切り戻し

JSONの `gameplayEnabled` をfalseにして本編画面を作り直すと旧Image/SpriteAnimatorを使う。実行中の切替UIや保存設定ではない。新規セーブ項目や装備形式の変更はない。

## 検証の記録

- 初回描画 `tools/hero-gameplay-qa/2026-09-28/v1/validation.json`: passed、46枚。2武器×11要求×2解像度、顔付き会話2枚。新旧二重描画、装備反映/予約/無装備復帰、リール以外の保持解除、親移動追随、表示だけで残高や音の発火数が変わらないことを確認。
- v1のPlay Mode試験はテスト初期化でNullReference。EnterPlayModeを跨ぐイテレータ内のキャプチャされたローカル関数を静的ヘルパーに変更し、v2-playで1件Passed。ゲーム側のコードをこの失敗に合わせて変更していない。
- v2-playは実OnBetClicked/StopReelを使用。剣は右→左→中、槍は中→右→左。30秒待機、第一停止保持、第二停止contact1回、重複/第三停止で追加なし、実リール終了、仮セーブの読込と装備外見復帰を検査。見た目の時計はテストから明示的に進める区間を含む。
- `v3-final/`は46描画passed。敵/前兆でidle、JSON falseで旧表示へ戻る検査も成功。play-tests.xmlは1シナリオPassed、GameController.Updateの実時間によるモーダル中停止/閉じた後の再開も確認。p1-regression.xmlも1シナリオPassed（会話/GET/呪い/OP送りの既存検査）。Core/Runtime/Testsコンパイル、check_layout成功。
- v3目視で振り上げ時の剣先がステージ札の後ろへ入る点を発見。本編だけの身長を190→170に調整。原画/武器形状は変えず、上方の余白を確保した。再描画先は `v4-final/`、46枚passed。v3のPlay Mode検査から変更したのはこの倍率だけ。

隔離Unityは `work/shop-qa`、productName=shop-qa。仮セーブもその専用保存領域だけ。主プロジェクトをbatch起動せず、本番セーブを変更していない。実機タッチ・キーボード/マウス実入力・聴感・配布ビルド・OP全分岐は未確認。コード接続の合格と、頭身差/中割り/足滑りの美術合格を分ける。

## 2026-09-28 OP逃走停止の修正と帰還の通し検査（dev 334）

- 発見: OPの会話をHideBoxしてもOpeningPlayer本体は逃走中に残る。IsShowingで新サリアを停止すると、操作できる逃走中も静止したままになる。OpeningPlayer.IsDialogueVisibleを追加し、新サリアの停止条件だけを会話の実表示へ変更。GET/呪いの従来の遮蔽条件はそのまま。
- 同時に、HideBox時は全画面DimのraycastTargetをfalse、Show時はtrueへ戻す。透明な暗幕がBETを拾う原因を除く。スキップボタンは独立して残る。
- 新試験 `Tests/EditMode/AdventureHeroJourneyTests.cs` はshop-qa以外ではIgnore。実OpeningEscapeRoutine→操作可能な逃走→倒れる→宿/街→門→OP完了→帰還→再入場をPlay Modeで進めた。会話中の停止、隠した後の再生、暗幕の入力判定、街/門通過、opDone保存、制作装備w_spearの維持を確認。ボタンcallback/StopReelを直接呼ぶ区間を含むため、実マウス操作の合格ではない。
- v5-journeyは機械検査Passedだが再入場画像が真っ黒だった。新GameController作成直後でMapFadeOutが残っていたのが原因。ゲームを変更せず、試験で暗転オブジェクトの消滅を待つよう修正。旧画像を消さず証拠として保存。
- 最終 `tools/hero-gameplay-qa/2026-09-28/v6-journey-final/` はPlay Mode1件Passed、5状態×2解像度の10枚。再入場の明転と槍外見を目視。P1の会話/GET/呪い/OP送り回帰1件Passedは `v5-journey/p1-regression.xml`。Runtimeのコードは同じで、v6変更は試験の撮影待ちだけ。コンパイル/配置検算も成功。
- 未確認: OP途中の各保存地点からの再開、スキップ全タイミング、実入力、実機、聴感。OP完了直後の章会話は文字送り途中の撮影を含む。10枚だけで全会話の可読性や最終美術の合格としない。新しい画像生成はなし。

## 次の手順

1. 原画の歩行/構え/旧被弾/勝利の頭身差を動画で詰める。OPの基本通過と帰還再入場は確認済み。途中保存/スキップ全分岐は別途追加する。
2. 敵の本編出現/予兆/被弾/決着と24ポーズを接続する。[接続前調査](ENEMY_PRESENTATION_P2.md)に入口と注意を記録。本編のbatと見本のwolfを混同せず、まずslime/goblinと未対応時の旧表示復帰を検証。表示contactで抽選やダメージを増やさず、既存の結果を受け取る。
3. 音は発火元を維持して専用足音と聴感を仕上げる。P2合格後に戦闘の波、20敵、残りの装備外見、光/影へ進む。

### 2026-10-20 / 接近→攻撃の証拠indexとQA guard（dev 400）

- 既存の隣接画像記録を修正。traceの経過時刻はStep後、capture名は0始まりのStep番号であるため、walk pose18=042、attack pose26=043（044も同じpose26）。
- QAにCSV `frame`/`capture`列、handoff時PNG差分assert、一意な隔離出力先を追加。`runtime-approach-attempt04` は接近158、攻撃135、7状態×2解像度、実BET EditMode 1件、source manifestをPASS。
- 既存実装の一段切替は観察のみ。支持足/中割り/美術承認は未完了。原画、JSON、ゲーム動作、セーブ、戦闘値は変更なし。
- 次: 既存絵の再接続か中間原画が必要かを制作環境でレビュー。内蔵ImageGenが利用不可の間は画像素材を作らない。敵3種本編接続やP3〜P5へ移る前にP2美術ゲートを区別する。

### 2026-10-20 / 接近末尾に既存attack-f2 poseを表示（dev 401）

- `approach.handoffSeconds=0.08` を追加。接近中の最終80msは剣pose25または槍pose32を表示し、lead完了後は既存f3-4のpose26/33へhandoff。既存素材のみ。
- attempt05では旧fixtureが歩行pose18をlead=.35まで期待。attempt06ではcontactまでの試験時間が旧サンプル位置に依存して失敗した。状態・時間を新handoffに合わせたattempt07で、接近170枚、攻撃135枚、7状態×2解像度、実BET EditMode1件、17 sample、manifest一致がPassed。
- 実画像で3段（walk17→ready25/32→strike26/33）とcontact=0の準備区間を確認。位置/接近duration/contact時刻/cue/数値/保存は不変。支持足と原画中割りの最終美術は未承認。実機/聴感/配布buildは未確認。

既存bigbonusblitz継続設定を維持。コミット・push・公開はしない。


### 2026-10-20 接近/攻撃/表示の隔離回帰再確認

既存のruntime approach QAを同じshop-qa隔離方法で再実行。接近158枚、剣/槍のBET攻撃135枚、7状態×2解像度、実BET 1 scenario、17 sample、8ファイルsource manifest一致がPassed。py -3 tools/verify/run.pyはCore/Runtime/Tests compileと100万G検証がPassed。追加コード/素材/お知らせはなく、本編セーブも読み込んでいない。支持足と歩行→攻撃中割りの美術承認、3敵の本編接続、P3〜P5、実機/聴感/配布buildは未完了。[レビュー](art/2026-10-20/P2_RUNTIME_REGRESSION_REVIEW.md)。

### 2026-10-20 歩行→攻撃の隣接描画を目視

隔離画像で剣の歩行pose18（0.3583秒）から攻撃pose26（0.3667秒）への一段切替を観察。root移動差は0.023、contact前。機械QA合格とアニメーション美術承認を区別し、支持足の連続は未承認のまま。[目視記録](art/2026-10-20/HERO_WALK_ATTACK_HANDOFF_VISUAL_REVIEW.md)。この改善は中間原画または既存poseの再接続判断が必要だが、現在のツールには内蔵画像生成がなく、本編/JSON/原画を変更していない。
