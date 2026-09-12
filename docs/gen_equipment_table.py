# -*- coding: utf-8 -*-
"""
装備の一覧表を game_config.json から作る。

    py -3 docs/gen_equipment_table.py     →  docs/equipment.md

数字の実体は game_config.json の equipment。表は読むためのもので、直すのは JSON 側。
"""
import io, json, os, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CFG = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Data", "game_config.json")
OUT = os.path.join(ROOT, "docs", "equipment.md")

KIND_NAME = {"head": "頭", "body": "体", "hands": "手", "feet": "足", "weapon": "武器", "accessory": "アクセ",
             "armor": "体", "trinket": "アクセ", "": "どこでも"}
KIND_ORDER = ["head", "body", "hands", "feet", "weapon", "accessory"]
EFFECT = {
    "statLife": ("ライフ", "", "ステータスのライフに +n（振り分けと同じ扱い）"),
    "statTechnique": ("テクニック", "", "ステータスのテクニックに +n"),
    "statLuck": ("ラック", "", "ステータスのラックに +n"),
    "defeatBonus": ("討伐率", "%", "エンゲージの討伐率に +%"),
    "battleDamage": ("狩猟ダメージ", "%", "AT の狩猟で与えるダメージに +%"),
    "torchSpins": ("回復薬の効き", "G", "回復薬 1 個で回復するライフに +G"),
    "soulGain": ("ソウル", "%", "手に入るソウルに +%"),
    "expGain": ("EXP", "%", "手に入る EXP に +%"),
    "atStartPercent": ("AT 期待度", "%", "ボーナス開始時の AT 期待度に +%"),
    "atInitialSpins": ("AT 初期G", "G", "AT の初期 G 数に +G"),
}

def normalize(slot):
    return {"armor": "body", "trinket": "accessory"}.get(slot, slot)

def scale(per_level, depth, mn, mx, power):
    v = int(round(per_level * depth * max(1, power) / 100.0))
    return max(mn, min(mx if mx > 0 else 9999, v))

def rarity_weights(rarities, depth):
    ws = [max(0, int(round(r["weight"] + r.get("weightPerDepth", 0) * (depth - 1)))) for r in rarities]
    total = sum(ws) or 1
    return [w * 100.0 / total for w in ws]

def main():
    c = json.load(io.open(CFG, encoding="utf-8-sig"))
    eq = c["equipment"]
    st = c.get("stats", {})
    bases = eq["bases"]; affixes = eq["affixes"]; rarities = eq["rarities"]
    L = []
    w = L.append
    w("# 装備の一覧（game_config.json の equipment から自動生成）")
    w("")
    w("直すのは `UnityProject/BigBonusBlitz/Assets/Resources/Data/game_config.json` の `equipment`。")
    w("直したら `py -3 docs/gen_equipment_table.py` でこの表を作り直す。")
    w("")
    w("## 仕組み（短く）")
    w("")
    w("- 落ちる率: 敵 %d%% / ボス %d%% / 狩猟 %d%% / 宝 %d%%。鞄 %d 個（いっぱいなら一番弱い物と入れ替え）" %
      (eq["dropRateMob"], eq["dropRateBoss"], eq["dropRateHunt"], eq["dropRateTreasure"], eq["bagSize"]))
    w("- 1 個できるまで: ①種類（ベース）を重みで選ぶ（その深さで出る物だけ） → ②レア度を深さで選ぶ → ③基礎効果 → ④レア度ぶんの接辞を重みで足す")
    w("- 値の式: `round(深さ1あたり × 深さ × レア度の倍率 / 100)` を 最低〜最高 に収める。深いほど、レアほど大きい")
    w("- 名前は 接頭辞 + ベース + 接尾辞（例: 剛毅の 革の帽子 の守り）。同じ効果が重なれば足し算")
    w("- 着ける枠は 頭 / 体 / 手 / 足 / 武器 / アクセ 1〜3 の 8 つ。アクセは 3 つ着けられる")
    w("")
    w("## レア度")
    w("")
    w("| レア度 | id | 色 | 接辞の数 | 倍率 % | " + " | ".join("深さ %d" % d for d in (1, 2, 4, 6, 8)) + " |")
    w("|---|---|---|---|---|" + "---|" * 5)
    for i, r in enumerate(rarities):
        cells = []
        for d in (1, 2, 4, 6, 8):
            v = rarity_weights(rarities, d)[i]
            cells.append(("%.2f%%" if v < 1 else "%.0f%%") % v)
        w("| %s | `%s` | `%s` | %d | %d | %s |" % (r["name"], r["id"], r["color"], r["affixes"], r["power"], " | ".join(cells)))
    w("")
    w("深さごとの列は「その深さで落ちた 1 個がそのレア度になる確率」。")
    w("")
    w("## 効果の意味")
    w("")
    w("| 効果 | 単位 | 何に効くか |")
    w("|---|---|---|")
    for k, (nm, unit, desc) in EFFECT.items():
        w("| %s (`%s`) | %s | %s |" % (nm, k, unit or "点", desc))
    if st:
        w("")
        w("ステータス 1 点あたり（`stats`）: ライフ = BET 無料 %.2f%% / 力尽きた時の補填 +%.0f / 回復薬 +%.1fG、"
          "テクニック = エンゲージ +%.2fG / 討伐率 +%.1f%% / 狩猟ダメージ +%.1f%%、"
          "ラック = レア役 +%.2f%% / リプレイ +%.2f%% / 宝 +%.1f%%。上限 %d 点（振り分け + 装備）" %
          (st["life"]["freeBetRate"], st["life"]["rescueBonus"], st["life"]["torchSpins"],
           st["technique"]["engageSpins"], st["technique"]["defeatBonus"], st["technique"]["battleDamage"],
           st["luck"]["rareRate"], st["luck"]["replayRate"], st["luck"]["treasureBonus"], st["maxPerStat"]))
    # ---- 落とし物
    drops = c.get("drops")
    if drops:
        w("")
        w("## 落とし物（`drops`）")
        w("")
        w("装備は 1 回の討伐で 1 つまで（率 %）。「深さ +n」はボスなどで上位のレア度を出やすくする補正。装備以外は 1 つずつ独立に率で落ちる。")
        w("")
        w("| 出どころ | 装備の率 | 深さ +n | 装備以外（名前 量 率） |")
        w("|---|---|---|---|")
        for key, label in (("mob", "敵（エンゲージ討伐）"), ("boss", "ボス"), ("hunt", "狩猟（AT）"), ("treasure", "宝箱")):
            d = drops.get(key) or {}
            items = ", ".join("%s +%d %d%%" % (e.get("name") or e.get("kind"), e.get("amount", 0), e.get("rate", 0)) for e in d.get("items", []))
            w("| %s | %d%% | +%d | %s |" % (label, d.get("equipRate", 0), d.get("equipDepthBonus", 0), items or "—"))
    # ---- ベース
    w("")
    w("## ベース（種類ごと）")
    w("")
    w("「出る割合」は同じ種類の中での重みの比（深さ 8、全部が出る状態）。値の例は %s（倍率 %d）と %s（倍率 %d）の基礎効果。" %
      (rarities[0]["name"], rarities[0]["power"], rarities[-1]["name"], rarities[-1]["power"]))
    for kind in KIND_ORDER:
        rows = [b for b in bases if normalize(b["slot"]) == kind]
        if not rows: continue
        total = sum(b["weight"] for b in rows) or 1
        w("")
        w("### %s（%d 種）" % (KIND_NAME[kind], len(rows)))
        w("")
        w("| id | 名前 | 効果 | 深さ1あたり | 最低 | 重み | 出る割合 | 出はじめ | %s 深さ1 / 4 / 8 | %s 深さ4 / 8 |" % (rarities[0]["name"], rarities[-1]["name"]))
        w("|---|---|---|---|---|---|---|---|---|---|")
        for b in rows:
            nm, unit, _ = EFFECT.get(b["effect"], (b["effect"], "", ""))
            common = " / ".join("+%d%s" % (scale(b["perLevel"], d, b["min"], 0, rarities[0]["power"]), unit) for d in (1, 4, 8))
            legend = " / ".join("+%d%s" % (scale(b["perLevel"], d, b["min"], 0, rarities[-1]["power"]), unit) for d in (4, 8))
            w("| `%s` | %s | %s | %.2f | %d | %d | %.0f%% | 深さ %d | %s | %s |" %
              (b["id"], b["name"], nm, b["perLevel"], b["min"], b["weight"], b["weight"] * 100.0 / total, b["minDepth"], common, legend))
    # ---- 接辞
    w("")
    w("## 接辞（レア度の数だけ付く。同じ物は 2 回付かない）")
    w("")
    w("「付く確率」は 1 つ目の接辞としてそれが選ばれる確率（その種類に付けられる接辞の重みの比）。付く数はレア度の表の「接辞の数」。")
    w("")
    w("| id | 語 | 位置 | 効果 | 深さ1あたり | 最低〜最高 | 対象 | 重み | " + " | ".join(KIND_NAME[k] for k in KIND_ORDER) + " |")
    w("|---|---|---|---|---|---|---|---|" + "---|" * len(KIND_ORDER))
    pools = {}
    for kind in KIND_ORDER:
        pools[kind] = [a for a in affixes if a["slot"] == "" or normalize(a["slot"]) == kind]
    for a in affixes:
        nm, unit, _ = EFFECT.get(a["effect"], (a["effect"], "", ""))
        probs = []
        for kind in KIND_ORDER:
            pool = pools[kind]
            if a in pool:
                total = sum(x["weight"] for x in pool) or 1
                probs.append("%.0f%%" % (a["weight"] * 100.0 / total))
            else:
                probs.append("—")
        w("| `%s` | %s | %s | %s | %.2f | %d〜%d | %s | %d | %s |" %
          (a["id"], a["label"], "後" if a["suffix"] else "前", nm, a["perLevel"], a["min"], a["max"],
           KIND_NAME.get(normalize(a["slot"]), a["slot"]), a["weight"], " | ".join(probs)))
    w("")
    w("## 値の例（接辞）")
    w("")
    fine = next((r for r in rarities if r["id"] in ("fine", "uncommon")), rarities[min(1, len(rarities) - 1)])
    rare = next((r for r in rarities if r["id"] == "rare"), rarities[min(2, len(rarities) - 1)])
    leg = rarities[-1]
    w("| 接辞 | %s 深さ2 / 5 | %s 深さ5 / 8 | %s 深さ8 |" % (fine["name"], rare["name"], leg["name"]))
    w("|---|---|---|---|")
    for a in affixes:
        nm, unit, _ = EFFECT.get(a["effect"], (a["effect"], "", ""))
        f = " / ".join("+%d%s" % (scale(a["perLevel"], d, a["min"], a["max"], fine["power"]), unit) for d in (2, 5))
        r = " / ".join("+%d%s" % (scale(a["perLevel"], d, a["min"], a["max"], rare["power"]), unit) for d in (5, 8))
        l = "+%d%s" % (scale(a["perLevel"], 8, a["min"], a["max"], leg["power"]), unit)
        w("| %s（%s） | %s | %s | %s |" % (a["label"], nm, f, r, l))
    w("")
    io.open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")
    print("wrote", OUT, "bases", len(bases), "affixes", len(affixes))

if __name__ == "__main__":
    main()
