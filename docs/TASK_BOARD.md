# やること棚（タスク棚）の回し方

BBB の「やること棚」は claude.ai の Artifact（db 付き）。本人がスマホでチェックを入れ、Claude のセッションがチェック済みを上から 1 件ずつ進める。
**この手順はどのセッション・どの PC でも同じ。**セッションで `/task-board` と打てば `.claude/skills/task-board` がこの手順で動く。

- 棚: https://claude.ai/code/artifact/59334708-2f13-4dc1-adfe-accb1ff46f60
- db: collection `tasks`（doc id は `t01`… / `c01`… / 本人が足した id）
- 1 件の fields: `title` `desc` `size`(S/M/L) `checked`(bool) `status`(candidate / queued / doing / done / blocked) `note` `order`(昇順で上) `by`(user / claude) `createdAt` `updatedAt` `doneAt`
- お知らせの実体: `UnityProject/BigBonusBlitz/Assets/Resources/Data/notices.json`（BOM 付き UTF-8。読むときは `encoding="utf-8-sig"`）

## 1 件の流れ

1. `git status` で作業木を見る。自分が触っていない変更（別セッションのもの）があるファイルを覚えておく（後述「別セッションの未コミット変更」）
2. 読む: `Artifact` ツール `action: read_db`, `db_op: list`, `collection: tasks`, `query: {limit: 200}`。
   `checked == true` かつ `status` が `queued` のものを `order` 昇順に並べ、先頭を取る。
   `doing` は他の PC・セッションが作業中かもしれない: `updatedAt` が **3 時間より古い**ものだけ引き取る（それより新しい `doing` と `blocked` は飛ばす）
3. 着手: `status: doing`, `updatedAt`, `note: "作業中 <PC 名か場所> <時刻>"` に更新（下の「書き方」）
4. 実装する。同じ commit に入れるものをここで書く:
   - `notices.json` の先頭に 1 件（`version` は `git fetch` した最新の "dev N" に +1。先頭に 1 entry をテキストで挿入する。`json.dump` で書き戻さない＝BOM・改行・整形を変えない）
   - 決めたことがあれば `docs/devlog.md` に「決定」を追記
5. 検証（下の表）。NG なら直す。直せない・環境が無いなら commit せず `blocked`（`note: "待ち: 検証環境 …"`）
6. commit（1 件 1 commit、`feat(...)`: 題（棚 c04）の形）。`git diff --cached --name-only` に notices.json が入っていることを確認 → `git pull --rebase --autostash` → push
7. 完了: `status: done`, `doneAt`, `note` に「済み <commit>: 結果 1 行」
8. 判断や素材が要るものは `status: blocked`, `note: "待ち: 何が要るか（案があれば 1 行ずつ）"` で飛ばす。**再開は本人が `queued` に戻す**（note に答えを書く）。Claude は blocked を自分で解除しない
9. チェック済みが尽きたら候補を足して止まる。ただし **`by: claude` の未チェック候補が 3 件未満のときだけ**（`status: candidate`, `checked: false`, `order` は最大 +10）。3 件以上あれば足さず「棚は空、候補 N 件待ち」と 1 行で終える

## 書き方（db の更新）

`write_db` は **`db_op: batch`** で、各 entry に **`if_version`**（直前の read が返した `version`）を付ける。
単発の `update` / `set` は `if_version` を渡せず version_mismatch で弾かれる（2026-09-14 時点のツールの仕様）。

```json
{"action": "write_db", "url": "<棚の URL>", "db_op": "batch", "writes": [
  {"op": "update", "collection": "tasks", "doc_id": "c04", "if_version": 3,
   "data": {"status": "done", "doneAt": "2026-09-14T07:00:00Z", "updatedAt": "2026-09-14T07:00:00Z", "note": "済み 4708620: 中ボスに体力バー"}},
  {"op": "set", "collection": "tasks", "doc_id": "c09",
   "data": {"by": "claude", "checked": false, "status": "candidate", "order": 380, "size": "S", "title": "…", "desc": "…", "note": "", "createdAt": "…", "updatedAt": "…"}}
]}
```

新規（`set`）は `if_version` 不要。version が変わっていて弾かれたら read し直して付け直す。

## 検証（commit の前に。結果は「検証通過」の 1 行で報告）

| 変えたもの | やること |
|---|---|
| C# / JSON の数値 | `py -3 tools/verify/run.py` — Core / Runtime / Tests のコンパイルと 100 万G のハーネス（`不変条件の違反なし`、機械割が動いたら数値を devlog に）。速く見るだけなら `--compile` |
| C# 全般（画面も） | `py -3 tools/verify/run.py --tests` — 本体を複製して Unity バッチで EditMode テスト（数分）。dotnet が無い PC はこれだけでもコンパイルの確認になる |
| 画面の見た目 | 複製プロジェクトでプローブを回して PNG を描き、目で見る: `Unity.exe -quit -batchmode -projectPath <複製> -executeMethod <Probe>.Run -logFile <log>`（プローブは `Assets/Editor/Probes/*.cs`。出力先は各プローブの環境変数、例 `EXPLAIN_UI_OUTPUT`）。`py -3 docs/check_layout.py` も通す |
| 演出 | `tools/fx_viewer.html` に項目を足す。数値は game_config.json と同じキー。`py -3 tools/fx_sync.py` で既定を同期 |
| JSON | `json.load(io.open(p, encoding="utf-8-sig"))` で読めることを確認。BOM は触らない |

前提: dotnet SDK 8 以上、Unity（Hub の既定の場所。違えば `UNITY_EDITOR_DIR`）、`UnityProject/BigBonusBlitz/Library`（clone 直後は無い。Unity で本体を一度開く）。
run.py はどれが無くても「NG …」の 1 行で教える。本体を Unity で開いたまま本体に対してバッチを回さない（Library を取り合う）。複製は run.py が `%TEMP%/bbb_verify_proj` に作る。

## commit の約束

- 1 件 1 commit、push まで。末尾に `Co-Authored-By: Claude …`
- **別セッションの未コミット変更に触らない。**`git status` で自分が触っていない変更があるファイル（例: GameController.cs）を変えるときは
  `git show HEAD:<path> > <scratch>/stage/<file>` を作り、**HEAD の写しと作業木の両方に同じ編集を当てる**（差分を取って移すのでなく、同じ Edit を 2 回）。
  commit は写しの中身だけ: `git hash-object -w <写し>` → `git update-index --cacheinfo 100644,<hash>,<path>`。
  相手の作業を壊さず、相手が後で commit しても自分の変更が消えない
- 禁止: `git stash`（手動）/ `git checkout -- <path>` / `git reset --hard` / `git clean`。検証 NG で戻すのは自分が変えたファイルだけ（変更前の中身を scratch に取っておく）
- push 前に `git pull --rebase --autostash`（作業木が別セッションの変更で汚れていても通る）。衝突したら `git rebase --abort` で戻し、commit だけ残して `note: "push 待ち: …"`。
  notices.json が衝突したら両方残して自分の番号を最新 +1 に振り直す
- 元絵（`assets/`）は LFS。1 枚 1〜2MB でも入れてよい（2026-09-14 本人）。QA の画面撮り（`tools/*-qa/`）と `*_preview.png` は入れない

## 今の棚の状態（2026-09-15）

- done: t01〜t20, c01〜c13
- candidate（by: claude、未チェック）: c14（第 3 章のステージ名）/ c15（物語の上下の台詞が出ない段）/ c16（4 章目以降は最後の章を使い回す）/ c17（AT が全 G の 55〜61%）/ c18（StageMapView.cs を消す）

## 別 PC の push 待ちで HEAD の Runtime がコンパイルできないとき（2026-09-15 の例）

`AtelierMap.cs` が `AdventureEnvironmentProfile.UsesModules` を使うのに、その定義（別 PC の未 push 分）が無い。
`py -3 tools/verify/run.py` の runtime / tests は NG になるが、自分の変更の検証は次でできる:
- scratchpad に runtime.csproj の写しを作り、足りない定義を足した `AdventureEnvironmentCatalog.cs` の写し（`public bool UsesModules => false;`）に `<Compile Remove>` + `<Compile Include>` で差し替えて `dotnet build`
- Unity の EditMode テスト / プローブは、複製プロジェクト（`Assets` を robocopy /MIR で写す）に同じ穴埋めの写しを置いてから回す
- 直すのは相手の仕事。自分では直さず「指摘」に留める
