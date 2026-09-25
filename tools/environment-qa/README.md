# 霧の森の実描画と引き継ぎ

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
