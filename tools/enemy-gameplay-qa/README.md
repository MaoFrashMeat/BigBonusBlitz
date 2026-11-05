# 本編の敵モーションQA

- 2026-10-10/dev375-three-enemy-guard: 敵3種の各6モーション定義、runtime有効ID（slime/goblinのみ）、森狼preview-only境界、dedicatedArtApproved=falseを読み取り専用で検査。遭遇/抽選/セーブは不変、最終美術未承認。[レビュー](../../docs/art/2026-10-10/ENEMY_THREE_SOURCE_GUARD_REVIEW.md)

正本: [ENEMY_GAMEPLAY_P2.md](../../docs/ENEMY_GAMEPLAY_P2.md)。隔離shop-qa/Unity6000.3.15f1。

- 2026-09-28/v1: 時計の配線が誤っており、hitからidleへ戻らず失敗。
- v2: 時計修正。短いサリア被弾の観測時刻で失敗。
- v3: フレーム観測へ修正。AT被弾の撮影後時刻で同種の失敗。決着画像も一部消えた後。
- v4-final: 敵1件、既存のサリア/P1回帰2件Passed。通常戦の帯がAT入口に残る点を描画で発見。
- **v5-final**: 帯/ボス状態の初期化を追加。3件Passed、26画像、コンパイル/配置検算成功。source-manifestに本編/QA一致。最終美術/聴感/実入力/実機/全敵の合格ではない。
- **2026-09-29/v6-judge**: Judgeの第一停止で先行被弾が出る失敗を再現し、第一停止はready保持、勝利は主人公contact後に決着するよう本編を修正。実BET回帰1件Passed、Judge/剣/槍接触画像とtrace、validation.jsonを保存。最終美術/聴感/実入力/実機/全敵/Potion全分岐は未確認。
- **2026-09-29/v8-potion**: PotionHeal/PotionLarge/PotionDefeatの3結果を実BETで追加確認。Smallの大攻撃はcontact後に敵hit、Rareは主人公contactなしで討伐確定。1件Passed、trace/validation.json/静止画を保存。最終美術/聴感/実入力/実機/全敵は未確認。
- **2026-09-29/v10-wave**: エンゲージ帯を状態別の色と予約文へ更新。PotionLarge直後の暖色と「次は大攻撃！」を実BETで機械検査。既存回帰を含む1件Passed、2解像度静止画を保存。敵20種/BGM/実機/聴感/最終美術は未確認。

ENEMY_GAMEPLAY_OUTPUTに出力先絶対パスを設定。隔離コピーでEditModeのBBB.Tests.AdventureEnemyPlayTestsを実行し、内部でEnterPlayMode。新規の仮GameControllerへ結果を投入する検査を含む。本番プロジェクトをbatch起動せず、保存領域をproductName=shop-qaへ隔離する。
