# -*- coding: utf-8 -*-
"""
物語の台詞を一覧表にする（game_config.json の story と adventure から）。

    py -3 docs/gen_story_table.py     →  docs/story.md

台詞の実体は game_config.json の story。直すのは JSON 側（エディタの「物語」タブでも直せる）。
"""
import io, json, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CFG = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Data", "game_config.json")
OUT = os.path.join(ROOT, "docs", "story.md")

def lines(ls):
    """台詞の列を 1 セルに。話者があれば【声】のように前に付ける。"""
    out = []
    for l in ls or []:
        sp = l.get("speaker") or ""
        out.append(("【%s】" % sp if sp else "") + l.get("text", "").replace("|", "｜"))
    return "<br>".join(out) if out else "—"

def main():
    c = json.load(io.open(CFG, encoding="utf-8-sig"))
    st = c.get("story", {})
    adv = c.get("adventure", {})
    nodes = adv.get("nodes", [])
    cols = []
    for n in nodes:
        if n["column"] not in cols: cols.append(n["column"])
    L = []
    w = L.append
    w("# 物語の台詞（game_config.json の story から自動生成）")
    w("")
    w("直すのは `UnityProject/BigBonusBlitz/Assets/Resources/Data/game_config.json` の `story`。直したら `py -3 docs/gen_story_table.py` で作り直す。")
    w("")
    w("## 出し方")
    w("")
    w("- ステージに着くと、その段（列 A〜H）の台詞を出す。枝の高さで **上 / 中 / 下** を選ぶ（上の枝 = 枝番号 %d 以下、下の枝 = 下から %d 本）。灯が強いほど世界が応え、弱いほど冷たい" %
      (st.get("highBranchMax", 2), st.get("lowBranchFromBottom", 1)))
    w("- 章の頭で「章の始め」を出す。2 周目以降は「周回」の台詞を足す（用意していない周回は、いちばん近い番号のもの）")
    w("- 引き返した / ライフが尽きた / 力尽きた / 踏破した ときに、それぞれの台詞")
    w("- 用意していない章は第 1 章の台詞を使い回す")
    w("- 【声】は姿のない語り手。無印は主人公")
    w("")
    total = 0
    for ch in st.get("chapters", []):
        n = len(ch.get("opening", [])) + sum(len(ch.get(k, [])) for k in ("onBack", "onTorchOut", "onDeath", "onClear"))
        n += sum(len(s.get(t, [])) for s in ch.get("stages", []) for t in ("high", "mid", "low"))
        n += sum(len(v) for v in (ch.get("laps") or {}).values())
        total += n
        w("## 第 %d 章  %s（%d 行）" % (ch.get("chapter", 0), ch.get("title", "").split("  ")[-1], n))
        w("")
        w("問い: **%s**" % ch.get("theme", ""))
        w("")
        w("章の始め: " + lines(ch.get("opening")).replace("<br>", " ／ "))
        w("")
        w("| 段 | ステージ | 上の枝（灯が強い） | 真ん中 | 下の枝（灯が弱い） |")
        w("|---|---|---|---|---|")
        stages = {s["column"]: s for s in ch.get("stages", [])}
        for col in cols:
            names = " / ".join(n["name"] for n in nodes if n["column"] == col)
            s = stages.get(col, {})
            w("| %s | %s | %s | %s | %s |" % (col, names, lines(s.get("high")), lines(s.get("mid")), lines(s.get("low"))))
        w("")
        w("| 節目 | 台詞 |")
        w("|---|---|")
        for key, label in (("onBack", "引き返した"), ("onTorchOut", "ライフが尽きた"), ("onDeath", "力尽きた（灯が尽きた）"), ("onClear", "踏破した")):
            w("| %s | %s |" % (label, lines(ch.get(key))))
        for k, v in sorted((ch.get("laps") or {}).items(), key=lambda kv: int(kv[0]) if kv[0].isdigit() else 0):
            w("| %s 周目 | %s |" % (k, lines(v)))
        w("")
    w("合計 %d 行。" % total)
    w("")
    io.open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")
    print("wrote", OUT, "lines", total)

if __name__ == "__main__":
    main()
