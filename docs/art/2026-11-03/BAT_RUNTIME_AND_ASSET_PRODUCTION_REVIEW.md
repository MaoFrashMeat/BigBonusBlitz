# 2026-11-03 内蔵生成の再開とコウモリ本編接続

## 変更・判断
内蔵 image_gen が利用可能になったため、APIキー待ちを解除して制作を再開。原本は加工せず、送信プロンプトとSHA256を各フォルダーへ保存した。

- bat-motion-v1: 1536×1024 RGBA、8姿勢。UnityのEnemyPresentationへidle/ready/strike/attack/hit/defeatの6動作を追加。gameplayTypesへbatを追加し、本編の既存コウモリ遭遇を動作表示へ接続。HeroPresentationLabの切替候補にも追加。
- moth-motion-v1: 灯蛾8姿勢の生成候補。ランタイム未接続。
- weapons-five-v1: 大槌・短剣・弓・杖・聖剣の生成候補。握り座標と固有モーションは未実装。7系統完成ではない。

## 確認
隔離shop-qaでBatMotionValidationを実行。96連番とready/contact/defeat/runtime-contactの実PNG、接触一回、ready長時間保持、被弾割込み時の接触抑止、defeat保持、非対応IDへの復帰をPASS。
AdventureEnemyPlayTests.MainEnemyPresentationKeepsResultsAndFallbacksをslime/goblin/batへ拡張し、隔離Unity EditMode内のPlay遷移で1件Passed、0 failed。既存AT入口・通常戦闘・色/倍率・非表示・遅延反応・設定無効時の旧コウモリ表示も検査。1280×720 / 2556×1179の画像を保存。bat-contact-1280と単独runtime-contact/idleを目視し、透過合成とサリア付近への接触を確認。配置検算も成功。

## 修正・制約
生成プレビューの暗い周囲色だけで不透明と判断しない。実PNGのalphaは0〜254で、alpha>=32の輪郭は全8セル内。alphaが1程度の微小値はセル境界にもあるため、厳密な完全透明境界とは区別する。原本加工なし。今回Unity試験の失敗なし。
本番の遭遇/戦闘数値・保存形式は変更なし。既存ボスのbat型も同じbat表示経路を使用するが、専用飛竜ボス原画の完成ではない。新コウモリのAT専用実戦ケース、全武器との接触、聴感、実機、配布ビルド、全画面美術は未検証。最終美術承認false。

## 証拠・次の手順
- tools/enemy-gameplay-qa/2026-11-03/bat-v1/validation.json と96連番
- tools/enemy-gameplay-qa/2026-11-03/bat-gameplay-v1/tests.xml と本編PNG
- tools/enemy-gameplay-qa/BatMotionValidation.cs（隔離Editorへコピーして実行）

次は灯蛾の切り出し・固有タイミング、残り敵の原画/動作、5武器の握りと固有動作、背景A-1/E-2/H-1の新画風3層から展開する。P2〜P5全体は未完了。共有作業場のステージ済み変更は保持し、今回の追加は未コミット。先行push済み065d343と今回のローカル追加を混同しない。
