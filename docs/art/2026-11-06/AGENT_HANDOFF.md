# Unity反映と他AIへの引き継ぎ

作業日: 2026-11-06（Asia/Tokyo）。本人の最新依頼は「一旦他のAIエージェントでも進めるようにUnityに反映終わらせてプッシュしてください」。新規制作をここで区切り、生成済みE-1まで本編のResourcesへ接続して引き渡す。計画全体の完了ではない。

## 再開する場所

- リモート: `origin/codex/p2-gameplay-handoff`。20敵・7武器と最初の6背景は `90bef83657b96ad5cca9765abb219971cc2e6c62` でpush済み。この文書を含む後続コミットで追加9背景を引き渡す。
- 整理した作業場: `D:/Mao-PC/Github/_BigBonusBlitz_release-20261105`、ローカルbranch `codex/roster-weapons-20261105`。このworktreeから `HEAD:codex/p2-gameplay-handoff` へpushする。
- 元の共有作業場 `D:/Mao-PC/Github/BigBonusBlitz` はHEADがf375b3fのまま、多数の既存staged/unstaged変更を保持している。最新リモートと一致するクリーンな作業場とはみなさない。reset/clean/stashや一括addで整理しない。別AIは整理済みworktree、またはリモートの新規clone/worktreeから再開する。
- 最初にAGENTS.md、CLAUDE.md、docs/PROJECT_STATE.md、本書、対象設計MDを読む。元の自動タスクbigbonusblitzは解除済み。

## 実装済み

- 通常16種＋ボス4種の専用画像・8姿勢と地域別遭遇。敵プレゼンターと本編抽選経路に接続。
- 剣・槍・短剣・聖剣・大槌・弓・杖の7系統。弓/杖は飛翔と着弾待ちを本編へ接続。20敵と7武器の詳細は前回RELEASE_INTEGRATION_REVIEW.md参照。
- 新画風の背景は16/30地点。実アルファ15地点=A-1、B-1、B-2、C-2、C-3、C-4、D-1〜D-6、E-1、E-2、H-1。既存C-1は黒マット方式の回帰対象。今回の追加9地点はC-3、C-4、D-1〜D-6、E-1。
- 原本とプロンプトはdocs/art/2026-11-05および2026-11-06/environment-*/。原本PNGを変更せずResources/Art/Adventure/Modulesへコピーし、adventure_environments.jsonのUV行で分離。生成ツールは内蔵image_gen。
- integrate_native_modules.pyは透明余白の下端を測って中景のbottomを設定する。新しい背景を足すときはspecsへ行境界/地面色/道色と日付・版を追加する。アルファ行境界検査を通す。

## 失敗・判断と検証範囲

- D-3 v1は樹冠が上端で切れたので不採用。v2を再生成し、両方の原本・プロンプトを保存した。
- native-v5は11実アルファ＋C-1、144時間/天候条件・216周期検査に成功したが、中景の余白による浮きが見えた。共通bottom定数を原画下端に応じた値へ変更。
- native-v6は14実アルファ＋C-1、180条件・270周期、全30地点読込、2解像度で成功。C-4/D-4/D-5/D-6の本編構成画像を目視。下端に水平の切り替わりがまだ読める地点があり、地面との自然な馴染みは次の美術調整として残す。機械成功だけで美術完了にしない。
- 最新E-1を含むnative-v7の結果と最終compile/layoutは同フォルダのBACKGROUND_HANDOFF_VALIDATION.jsonを正本とする。
- 本編構成画像はGameControllerを使用するが、fixtureは背景だけを切り替えるため現在地ラベルはA-1のまま。ステージ移動操作試験とは別。
- 隔離Unity `C:/Users/ts081/Documents/Codex/2026-09-12/new-chat/work/shop-qa`（productName=shop-qa）のみで実行。本番セーブへの読み書きなし。最終美術承認・端末実機・URP Play Mode・配布ビルド・性能・聴感は未検証。

## 次の順番

1. 残り14背景 E-3〜E-8、F-1〜F-5、G-1〜G-3を同画風で生成。原本/プロンプト保存→アルファ行分離→Resources接続→隔離Unity描画。水平な地形下端と道路の馴染みも改善する。
2. 敵の遠隔攻撃の独立した飛翔表現、主人公の支持足・歩行復帰・低い敵との接触姿勢を調整。実装済みの20画像/8姿勢を最終動作承認と混同しない。
3. 独立した局所発光、光と影、実機/URP/性能/音を検証。設計書のP3〜P5を再確認し、未完了チェックを勝手に完了へしない。

## 検証と保存の手順

1. `py -3 tools/environment-qa/integrate_native_modules.py`。対象Resources/Data JSONとModulesを隔離shop-qaへ個別コピー（削除同期しない）。NativeEnvironmentValidation.csを隔離Assets/Editorへコピーする。
2. Unity6000.3.15f1を `-batchmode -quit -projectPath <shop-qa> -executeMethod NativeEnvironmentValidation.Run -logFile <出力>/Unity.log` で起動。環境変数NATIVE_ENVIRONMENT_OUTPUTに新規出力フォルダを設定する。実GPU描画なので-nographicsは付けない。
3. validation.json、Unity終了コード、PNGを確認。JSONのみで美術承認しない。今回の証拠はtools/environment-qa/2026-11-06/native-v7。
4. `py -3 tools/enemy-gameplay-qa/prepare_release.py` は明示した対象を整理済みworktreeへコピーする補助。今後の他作業を上書きしないよう差分を先に確認。`py -3 docs/check_layout.py` と `py -3 tools/verify/run.py` を整理済みworktreeで実行。
5. 対象だけstage/commit、`git pull --rebase origin codex/p2-gameplay-handoff`、通常push。強制push禁止。変更・失敗・未検証を引き継ぎ文書へ追記する。
