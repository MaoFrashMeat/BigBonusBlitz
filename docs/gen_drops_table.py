# -*- coding: utf-8 -*-
"""
落とし物の一覧表を game_config.json から作る（docs/gen_equipment_table.py と同じ方式）。

    py -3 docs/gen_drops_table.py     →  docs/drops.md

数字の実体は game_config.json の drops（雑魚 / ボス / 狩猟 / 宝箱）と adventure.treasures（宝箱の中身）。
表は読むためのもので、直すのは JSON 側。
"""
import io, json, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CFG = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Data", "game_config.json")
OUT = os.path.join(ROOT, "docs", "drops.md")

SOURCE = [("mob", "雑魚", "エンゲージで倒したとき"), ("boss", "中ボス", "中ボスを倒したとき"),
          ("hunt", "狩猟", "AT の狩猟で討伐したとき"), ("treasure", "宝箱", "地図の宝箱を開けたとき")]
KIND = {"souls": "ソウル", "embers": "エンバー", "torch": "回復薬", "atSpins": "次の AT の G 数", "atExpect": "AT 期待度",
        "exp": "EXP", "equip": "装備"}


def kind_name(kind):
    return KIND.get(kind, kind)


def main():
    c = json.load(io.open(CFG, encoding="utf-8-sig"))
    drops = c.get("drops", {})
    eq = c.get("equipment", {})
    rarities = eq.get("rarities", [])
    treasures = c.get("adventure", {}).get("treasures", [])
    L = []
    w = L.append
    w("# 落とし物の一覧（game_config.json の drops / adventure.treasures から自動生成）")
    w("")
    w("直すのは `UnityProject/BigBonusBlitz/Assets/Resources/Data/game_config.json` の `drops`（出どころごと）と `adventure.treasures`（宝箱の中身）。")
    w("直したら `py -3 docs/gen_drops_table.py` でこの表を作り直す。装備そのものの表は `docs/equipment.md`。")
    w("")
    w("## 仕組み（短く）")
    w("")
    w("- 出どころは 4 つ: 雑魚 / 中ボス / 狩猟 / 宝箱。それぞれ「装備が落ちる率」と「品の一覧」を持つ")
    w("- 装備: 率で 1 個。深さ + 上乗せ（ボスは +3）で作るので、上位のレア度が出やすい（レア度の出方は equipment.md）")
    w("- 品: 一覧の 1 つずつが独立に rate % で落ちる（複数落ちることもある）。鞄は関係ない")
    w("- 宝箱の中身（adventure.treasures）は重みで 1 つ選ぶ。宝箱の「品」の一覧はそれとは別に、上の率で足される")
    w("")
    w("## 出どころごとの率")
    w("")
    w("| 出どころ | いつ | 装備が落ちる率 | 装備の深さの上乗せ | 品の数 |")
    w("|---|---|---|---|---|")
    for key, name, when in SOURCE:
        s = drops.get(key, {})
        w("| %s | %s | %d%% | +%d | %d |" % (name, when, s.get("equipRate", 0), s.get("equipDepthBonus", 0), len(s.get("items", []))))
    w("")
    for key, name, when in SOURCE:
        s = drops.get(key, {})
        w("## %s（%s）" % (name, when))
        w("")
        w("装備 %d%%（深さ +%d）" % (s.get("equipRate", 0), s.get("equipDepthBonus", 0)))
        w("")
        items = s.get("items", [])
        if not items:
            w("品は落ちない")
            w("")
            continue
        w("| 品 | 中身 | 量 | 率 |")
        w("|---|---|---|---|")
        for it in items:
            w("| %s | %s | %s | %d%% |" % (it.get("name", ""), kind_name(it.get("kind", "")), it.get("amount", 0), it.get("rate", 0)))
        w("")
        # 期待値（1 回あたり）
        ev = {}
        for it in items:
            k = it.get("kind", "")
            ev[k] = ev.get(k, 0) + it.get("amount", 0) * it.get("rate", 0) / 100.0
        w("1 回あたりの期待値: " + "  /  ".join("%s %.1f" % (kind_name(k), v) for k, v in ev.items()))
        w("")
    if treasures:
        total = sum(max(0, t.get("weight", 0)) for t in treasures) or 1
        w("## 宝箱の中身（adventure.treasures。重みで 1 つ）")
        w("")
        w("| id | 名前 | 中身 | 量 | 重み | 率 |")
        w("|---|---|---|---|---|---|")
        for t in treasures:
            w("| `%s` | %s | %s | %s | %d | %.1f%% |" % (t.get("id", ""), t.get("name", ""), kind_name(t.get("kind", "")), t.get("amount", 0), t.get("weight", 0), 100.0 * max(0, t.get("weight", 0)) / total))
        w("")
    if rarities:
        w("## 装備のレア度（参考。深さごとの出方は equipment.md）")
        w("")
        w("| レア度 | id | 売値（深さ 1） |")
        w("|---|---|---|")
        for r in rarities:
            w("| %s | `%s` | %d |" % (r.get("name", ""), r.get("id", ""), r.get("sellSouls", 0)))
        w("")
    io.open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")
    print("wrote", os.path.relpath(OUT, ROOT), "(%d 行)" % len(L))


if __name__ == "__main__":
    main()
