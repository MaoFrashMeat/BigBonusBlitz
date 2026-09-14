# BigBonusBlitz（リポジトリ側の案内。話し方の約束は ~/.claude/CLAUDE.md が正）

- 現状は `docs/PROJECT_STATE.md`。セッションの最初に読む
- **やること棚**（本人がチェックした仕事の一覧）は `/task-board` で回す。手順の正本は `docs/TASK_BOARD.md`（どの PC・どのセッションでも同じ）
- commit の前に `py -3 tools/verify/run.py`（Core / Runtime / Tests のコンパイルと 100 万G のハーネス。`--tests` で EditMode テストも）
- 変更のたびに `UnityProject/BigBonusBlitz/Assets/Resources/Data/notices.json` の先頭に 1 件（番号は `git pull` 後の最新 +1）。決めたことは `docs/devlog.md`
- 数値はコードに書かず `Assets/Resources/Data/*.json`。演出は `tools/fx_viewer.html` に項目を足し、`py -3 tools/fx_sync.py` で既定を同期
- 別セッション・別 PC が同時に触っている。push 前に `git pull --rebase`。自分が依頼されていないファイルの未コミット変更には触らない
