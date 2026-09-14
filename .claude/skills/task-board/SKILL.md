---
name: task-board
description: BBB のやること棚（claude.ai の Artifact + db）を回す。チェック済みを上から 1 件ずつ、実装 → お知らせ → 検証 → commit / push → 棚を done に。「棚を回して」「タスク棚」「/task-board」「/loop 30m /task-board」で起動。空になったら候補を足して止まる
---

# やること棚を回す

正本は `docs/TASK_BOARD.md`（棚の URL・fields・書き方・検証・commit の約束）。**最初にそれを読む。**ここには順序だけ書く。

1. `git status` で別セッションの未コミット変更を把握する（そのファイルは HEAD の写しと作業木の両方に同じ編集を当て、写しだけ commit する）。`git fetch origin` で最新のお知らせ番号を見る。作業木を汚す・戻す操作（stash / checkout -- / reset --hard / clean）はしない
2. `Artifact` の `read_db`（`db_op: list`, `collection: tasks`, `query: {limit: 200}`）で棚を読む
3. `checked == true` かつ `status: queued` を `order` 昇順で 1 件（`doing` は `updatedAt` が 3 時間より古いものだけ引き取る。`blocked` は飛ばす）。無ければ 7 へ
4. `write_db`（`db_op: batch`, entry に `if_version`）で `status: doing`, `note: "作業中 <場所> <時刻>"`
5. 実装 → `notices.json` に 1 件（番号は最新 +1。先頭にテキストで挿入）→ 決定があれば `docs/devlog.md` → 検証（`py -3 tools/verify/run.py`。表は `docs/TASK_BOARD.md`）→ commit（notices.json が含まれているか確認）→ `git pull --rebase --autostash` → push
6. `status: done` + `doneAt` + `note`「済み <commit>: 結果 1 行」。判断や素材が要るなら `status: blocked` + `note`「待ち: …」で飛ばす（解除は本人）。次の 1 件へ（3 に戻る）
7. チェック済みが尽きたら: `by: claude` の未チェック候補が 3 件未満なら 3〜5 件足す（`status: candidate`, `checked: false`, `order` は最大 +10）。3 件以上あれば足さず「棚は空、候補 N 件待ち」で終える

報告は 3 行以内: 進めた件（commit）/ blocked にした件と理由 / 足した候補。数値は必要なものだけ。

引数があればそれを優先する（例: `/task-board c09` ならその 1 件だけ）。
