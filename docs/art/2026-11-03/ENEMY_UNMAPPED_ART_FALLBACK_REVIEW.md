# 未登録敵アートの誤流用を止める（dev 404）

日付: 2026-11-03（Asia/Tokyo）

## 対象と判断

敵20種の制作・本編接続に先立つ安全境界。`ArtLoader.EnemySprite` の未知IDが既定でゴブリン画像を使う挙動をやめ、既存素材が明示されている `slime` / `goblin` / `bat` のみ解決する。未知IDとnullは警告を出して `null` を返す。テストは未知ID/nullの返却値を確認し、警告文自体はassertしていない。EncounterTable、抽選、HP、報酬、接触、セーブ形式には手を入れない。

## 修正

- `ArtLoader.TryEnemySpritePath` を追加し、3つの既存素材パスのみを許可リスト化。
- `EnemySprite` は許可リスト外をゴブリンへフォールバックせず、警告付きでnullを返す。
- `WorkflowAndEnemyTests` に3既存IDのパス確認と、`forest_wolf` / `table_wolf_1` / 未登録ID / nullの公開API `null` 返却確認を追加。警告文は実装するが、文字列一致テストは設けない。

## 検証

最初の隔離コピーは共有作業場にのみある `AdventureHeroPresenter` / `AdventureEnemyPresenter` / `CurseHudPresenter` が不足し、compile前にUnity EditModeが停止した。同期後の初回.NET compileではUnity Test Frameworkをdotnet tests projectから呼ぶ `LogAssert` が解決できず失敗したため、そのassertだけ外して公開APIのnull返却確認を維持。現行共有の `Assets/Scripts` / `Assets/Tests` / `Assets/Editor` / `Assets/Resources/Data` を隔離cloneへ同期して再実行。共有本体は変更していない。

最終再実行: `py -3 tools/verify/run.py --tests`（隔離clone、Unity 6000.3.15f1）

- Core / Runtime / Tests: compile成功
- 100万G harness: 成功、20万Gの通常149,388G / REPLAY 50,612G、機械割112.2%、不変条件違反0
- Unity EditMode: 137件中131件成功、失敗0、既知Ignored 6件
- 追加ケース: 許可リスト3件と未知/null 4件を含む
- 保存結果: [`editmode-results.xml`](../../../tools/enemy-guard-qa/2026-11-03/dev404/editmode-results.xml)、[`run-summary.json`](../../../tools/enemy-guard-qa/2026-11-03/dev404/run-summary.json)

この確認はバッチEditMode検証で、Unityシーンの描画・美術品質確認ではない。実機/配布build/URPの承認も行っていない。

## 未完了と次の手順

- 20敵ロスターのうち専用素材・動作・本編遭遇は未完了。表示ガードは20種完成や本編導入を意味しない。
- CLI/API画像生成は本人許可済みだが、この実行環境で `OPENAI_API_KEY` が未設定。新規画像や生成プロンプト付き原画を捏造していない。キーをWindowsユーザー環境に設定した後、画像生成CLIで原画とpromptを保存し、敵/装備/背景を順に制作する。
- ゲームplayデータ、セーブ、遭遇ID、報酬、抽選は変更していない。
