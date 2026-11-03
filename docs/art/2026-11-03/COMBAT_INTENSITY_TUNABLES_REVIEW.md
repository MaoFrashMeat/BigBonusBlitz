# 戦闘の強弱を調整できるようにする

日付: 2026-11-03 (Asia/Tokyo)

## 実装

- `engage.attackFx` に小/中/大攻撃、反撃、JUDGE勝利/逃走の斬撃幅・画面揺れ時間/幅・文字サイズを追加。
- `tools/fx_viewer.html` に5種類の強さとJUDGE勝敗を切り替える描画プレビュー/調整欄を追加し、`fx_sync.py` が game_config の値を既定として同期する。
- 初期値は旧GameControllerの固定値および既定式を保持。Counterは中攻撃相当（0.37秒、幅7.8）に合わせた。
- 結果表示に関わる視覚パラメータのみ。役抽選、AttackSize、ダメージ、HP、遭遇、報酬、save、音の再生とタイミングは変更しない。

## 検証

- `py -3 tools/verify/run.py --tests`: Core/Runtime/Tests compile PASS; 1,000,000G harness PASS (機械割112.2%、不変条件違反なし); Unity EditMode 124 passed / 130 total / 0 failed / 6 ignored。
- 回帰テストは既定値（特にCounter）を読み戻し、攻撃構え/役9組で値を変えた場合もOutcome、AttackSize、Damageが一致することを確認。
- `docs/check_layout.py` と JSON parse / fx viewer inline JS syntax / `git diff --check` を実行。

## 判断と制限

- 強さは small/medium/large と Counter/JUDGEの表示強度として明示する。戦闘ロジックや役ごとの結果確率には関連づけない。
- これはP3戦闘表現の調整入口であり、ゲーム全体の戦闘演出/20敵/装備/背景/P4-P5計画完了ではない。
- Unityの隔離EditModeは合格。今回変更したプレビューの全状態/画面サイズ目視、実機・実入力、聴感、配布ビルド、最終美術の承認は未実施。
- 画像生成素材は今回の範囲に不要。原画は変更していない。

## 次

- 画面内の実攻撃プレビュー5種をUnity/ブラウザで各状態確認し、実機表示の確認を記録する。
- 継続計画の次の実作業は敵3種の本編接続/20種の遭遇整備だが、個別の機械検査・美術検査と本編実装を区別する。
