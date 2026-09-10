# 別の PC で同じ状態にする

「他の PC で開くと違う」の原因は、ほぼ次の 4 つに絞られる。上から順に潰す。

---

## 0. まず状態を調べる

**両方の PC でこれを走らせて、出力を見比べる。**

```bash
py -3 docs/check_project.py
```

Unity のバージョン、git の遅れ、LFS の実体、`.meta` の欠け、パッケージの固定版、
データファイルを一度に確認して、ずれている項目と直しかたを出す。
出力が同じになれば、残る差はウィンドウの並びなどの個人設定だけ。

---

## 1. Unity のバージョン違い（いちばん多い）

このプロジェクトは **6000.3.15f1** で作っている（`ProjectSettings/ProjectVersion.txt`）。

違うバージョンで開くと Unity が全アセットを作り直し、見た目も挙動も変わる。
しかも `ProjectVersion.txt` が書き換わって commit されると、他の PC も巻き込まれる。

### やること

- Unity Hub で **6000.3.15f1** を入れる
- Hub のプロジェクト一覧で、開く前に **エディターのバージョンが 6000.3.15f1 になっているか確認**する
- 「別のバージョンで開きますか」と聞かれたら **開かない**
- 上げるときは 1 台で上げてから `ProjectVersion.txt` を commit し、全 PC で同じ版に揃える

この PC に複数バージョンが入っていると、Hub が勝手に新しい方を選ぶことがある。
`check_project.py` が入っているバージョンを列挙するので、そこで気付ける。

---

## 2. 画像と音が落ちていない（LFS）

`.png` `.mp3` などは Git LFS で管理している。`git pull` だけでは中身が来ず、
「version https://git-lfs...」と書かれた小さなテキストのままになることがある。

Unity はそれを壊れた画像として読むので、絵が出ない・紫になる。

### やること

```bash
git lfs install      # その PC で 1 度だけ
git lfs pull         # 実体を落とす
```

`check_project.py` が「実体が無い」と出したらこれ。

---

## 3. `.meta` のずれ

`.meta` にはファイルの GUID とインポート設定が入っている。
これが無いと Unity が新しい GUID を振り、参照が切れる。落ちた PC と別 PC で別々の GUID になる。

### やること

- `.meta` は **必ず本体と一緒に commit する**
- `check_project.py` の「.meta が無い / 迷子の .meta」が 0 件であること
- 迷子の `.meta`（本体が消えているのに残っている）は削除する

---

## 4. 作業の取りこぼし

複数の PC で触っていると、片方が遅れたまま作業して衝突する。

### やること

作業を始める前に必ず:

```bash
git pull --rebase
git lfs pull
py -3 docs/check_project.py
```

終わったら:

```bash
py -3 docs/check_layout.py     # UI を触ったなら
git add -A && git commit && git push
```

**Unity を開いたまま pull しない。** 開いている間に `.meta` や設定が書き換わると、
取り込んだ内容と喧嘩する。閉じてから pull して、それから開く。

---

## 同期されるもの・されないもの

| もの | 同期 | 置き場所 |
|---|---|---|
| スクリプト、データ、素材、`.meta` | される | `Assets/` |
| プロジェクト設定、Unity のバージョン | される | `ProjectSettings/` |
| パッケージの版 | される | `Packages/packages-lock.json` |
| インポート結果のキャッシュ | されない（各 PC で作り直す） | `Library/` |
| ウィンドウの並び、最近開いたシーン | されない | `UserSettings/` |
| セーブデータ（ソウル、進行） | されない | PlayerPrefs（レジストリ） |

`Library/` と `UserSettings/` は `.gitignore` で除外している。これは正しい。
**セーブデータも PC ごと**なので、進行状況は同期されない。同じところから始めたいなら
タイトルで「はじめから」を選ぶ。

---

## それでも違うとき

`Library/` を消してから開き直すと、インポートをやり直す。

```bash
# Unity を閉じてから
rm -rf UnityProject/BigBonusBlitz/Library
```

消しても `.meta` があれば同じ結果になる。初回の起動は数分かかる。
