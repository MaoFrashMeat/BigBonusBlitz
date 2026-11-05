# 霧の森の実描画と引き継ぎ

## 2026-10-10 dev381: EditMode隔離再検証

`run.ps1 -Tests`を本番とは別の`%TEMP%/bbb-environment-c1-qa`へ実行。EditMode 129件中123件Passed、失敗0、既知のIgnore 6件（`Skipped:Ignored`）を確認し、`2026-10-11/v10-editmode-refresh/`へXML/ログを保存した。回帰ゲートであり、実機/URP/配布ビルド/聴感/最終美術/本編統合は未承認。[レビュー](../../docs/art/2026-10-10/EDITMODE_REFRESH_REVIEW.md)

## 2026-10-10 dev380: 背景ソース・光影の隔離再実描画

`run.ps1`を本番とは別の`%TEMP%/bbb-environment-c1-qa`へ再実行し、30地点/3層、時間帯4、天候9、森12条件、両解像度、屋内天候、セーブ不変を再照合。新しい取得物はホスト日付跨ぎのため`2026-10-11/v9-background-refresh/`に保存。夜雨の青い逆光、雨線、遺跡アーチ、手前植生とC-1ゲーム画面を目視した。これはソース/実描画ゲートで、URP/実機GPU/配布ビルド/負荷/サウンド/最終美術は未承認。[レビュー](../../docs/art/2026-10-10/BACKGROUND_REFRESH_REVIEW.md)

## 2026-10-10 dev376: 光影プローブ差分ガード

`run_light_shadow_guard.py` でdev371の実画像を読み取り、C-1/森昼晴れ/夜雨、30地点・森12条件・屋内天候・セーブ不変を再照合。平均RGB差56.128を確認した。光影差分は機械Passedだが、URP/端末/GPU/配布ビルド/最終美術は未承認。[レビュー](../../docs/art/2026-10-10/BACKGROUND_LIGHT_SHADOW_GUARD_REVIEW.md)

## 2026-10-10 dev371: 30地点/光影ソース境界ゲート

`py -3 tools/environment-qa/run_source_guard.py --output tools/environment-qa/2026-10-10/dev371-background-source-guard/validation.json`。30地点の一意ID、C-1遠/中/近3層、隔離Unityで再描画した森12条件・時間4/天候9、屋内天候/継ぎ目/セーブ不変結果を再照合する読み取り専用ゲート。URP Play Mode、実機GPU負荷、配布ビルド、最終美術は承認しない。`run.ps1`はこのPCのHubパスを優先し、旧D:パスへフォールバックする。

2026-09-14。`C-1` の新画風を3層に分け、Unityへ接続した確認資料。

## 見る

`index.html` をブラウザで開くか、リポジトリのルートで次を実行する。

```powershell
py -3 -m http.server 8874 --bind 127.0.0.1 --directory tools/environment-qa
```

[確認ページ](http://127.0.0.1:8874/) はUnityで撮影した12条件の画像とゲーム画面を表示する。スクロールは48枚の実描画を往復表示する早送りの位置確認で、リアルタイムUnityの性能測定ではない。

## 再生成

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/environment-qa/run.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/environment-qa/run.ps1 -Tests
```

前のUnityプロセスが終了してから次を実行する。非同期起動のため、戻り値のpidと `captures/unity.log` または `captures/tests.log` で終了を確認する。実描画の成功は `ADVENTURE_ENVIRONMENT_VALIDATION passed` と `validation.json`、テストは `editmode.xml` のresultで判定する。失敗後に古い成功JSONだけを読まない。

実行環境: Unity 6000.3.15f1。スクリプト冒頭のUnity実行ファイルの場所はこのPC用。`%TEMP%/bbb-environment-c1-qa` へソースをコピーし、プロジェクトに既にあるローカルPackageCacheを参照する。開いている本体のLibraryとセーブは使わない。ライセンスへ接続できるユーザー環境で起動する。

QAはBuilt-inのカメラで実際のuGUIと専用シェーダーを描画する。URPパイプラインでのPlay Mode、端末実機、配布ビルド、GPU負荷は未検証。通常テストはCore/Runtime/Editor/Testsのコンパイルを含む。

## 確認結果

- 実描画: 30地点の読み込み、C-1の朝昼夕夜×晴れ曇り雨=12条件、全9天候のAPI指定が成功。
- C-1各層について正逆方向それぞれ3回の周期境界の前後を測定。最大平均RGB差は `0.0002013812`（0〜1）。画素差は境界の飛びを検出する補助であり、継ぎ目全体の見た目は静止画と移動位置でも確認。
- 旧アトラスの境界検査も成功。屋外の雨／曇りが屋内へ残らないことを確認。
- GameControllerの実際のUI生成とステージ追従、キャラより背景が奥にあることを確認。1280×720と2556×1179の画像を保存。
- EditMode: **102件成功、失敗0、スキップ0**。レイアウト検算も重なり・はみ出しなし。
- HTMLの昼晴れ→夜雨切替、スクロール再生と停止、通常表示への復帰をブラウザで確認。

素材の検査は `inspect_sources.py`（Pillow、NumPyが必要）。画像を変更せず、RGBマットの基本キー変換と左右端を調べる。シェーダーの近傍補正を含む最終アルファの全画素比較ではない。

生成画像とプロンプト: `docs/art/backgrounds/2026-09-14/c1-layers/`。現行設定と次の作業: `docs/BACKGROUND_ART_DIRECTION.md`。
