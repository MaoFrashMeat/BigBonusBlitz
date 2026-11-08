# BigBonusBlitz（リポジトリ側の案内。話し方の約束は ~/.claude/CLAUDE.md が正）

- 現状は `docs/PROJECT_STATE.md`。セッションの最初に読む
- UIの新規作成・改修は `docs/AZURE_UI_QUALITY_STANDARD.md` の品質基準を既定にする（本人指定 2026-09-23）。画像を再提示されるまで旧来の仮UIで止めない。
- **やること棚**（本人がチェックした仕事の一覧）は `/task-board` で回す。手順の正本は `docs/TASK_BOARD.md`（どの PC・どのセッションでも同じ）
- commit の前に `py -3 tools/verify/run.py`（Core / Runtime / Tests のコンパイルと 100 万G のハーネス。`--tests` で EditMode テストも）
- 変更のたびに `UnityProject/BigBonusBlitz/Assets/Resources/Data/notices.json` の先頭に 1 件（番号は `git pull` 後の最新 +1）。決めたことは `docs/devlog.md`
- 数値はコードに書かず `Assets/Resources/Data/*.json`。演出は `tools/fx_viewer.html` に項目を足し、`py -3 tools/fx_sync.py` で既定を同期
- 別セッション・別 PC が同時に触っている。push 前に `git pull --rebase`。自分が依頼されていないファイルの未コミット変更には触らない
- 見せる前に、その分野の検査表を通す（本人指定 2026-11-09）。演出・動き＝`docs/FX_KNOWHOW.md` 4.13 / UI＝`docs/AZURE_UI_QUALITY_STANDARD.md` と `docs/ui_rules.md` のチェックリスト / 背景＝`docs/BACKGROUND_ART_DIRECTION.md` 5・7 節。未達は書き出して続け、「できました」で止めない
- エフェクトの絵は素材を土台にする（Blender で焼いた `tools/fx-lab/textures/proc_*.png` か CC0）。式だけ・コードで描いた絵・粒子＋ぼかしで作らない（4 回言われた）
- 当て推量で直さない。調べて `docs/FX_RESEARCH.md` に書き、書いた原則は実装の前に自分のコードへ当てて確かめる（カメラを傾けて破った）
