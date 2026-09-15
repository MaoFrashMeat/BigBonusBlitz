# -*- coding: utf-8 -*-
"""
設定 1〜6 の通常時の表（probabilities_A〜D）とモードの振り分け（modeTransitions）を、設定 1 の行から作り直す。

    py -3 tools/settings_ladder.py            # 出来上がりの要点を表示するだけ
    py -3 tools/settings_ladder.py --apply    # game_config.json の該当ブロックだけ書き換える（BOM・整形はそのまま）

決め方（docs/devlog.md「設定差はボーナスとレア役だけで付け…」）:
- 各設定の行は設定 1 の行を土台にし、BONUS6 / RARE6 の倍率を設定 6 の目標として重み W で補間する
- ベル・リプレイは全設定同じ。差はボーナスとレア役だけ
- ハズレ（HAZE）は 65536 から残りを引いた値
測るときは tools/verify/run.py のハーネス（設定別の機械割）か、冒険なしで回す自前の計測で。
"""
import io, json, sys

P = __file__.replace("\\", "/").rsplit("/tools/", 1)[0] + "/UnityProject/BigBonusBlitz/Assets/Resources/Data/game_config.json"
BONUS6 = {"BB_A": 2.0, "BB_B": 1.5, "BB_C": 1.2, "BB_D": 1.0, "RB_A": 1.5, "RB_B": 1.5}   # 設定 6 の倍率（設定 1 比）
RARE6 = 1.25                                                                           # チェリー A・スイカ A B・チャンス
RARE = ["CHERRY_A", "SUICA_A", "SUICA_B", "CHANCE_A", "CHANCE_B", "CHANCE_C", "CHANCE_D"]
W = [0.0, 0.15, 0.3, 0.5, 0.72, 1.0]                                                   # 設定 1〜6 の補間の重み
MODE = {"initial": {"1": [25, 25, 25, 25], "2": [25, 25, 25, 25], "3": [24, 24, 26, 26], "4": [23, 23, 27, 27], "5": [22, 20, 28, 30], "6": [20, 10, 30, 40]},
        "bonus":   {"1": [25, 25, 25, 25], "2": [25, 25, 25, 25], "3": [24, 25, 25, 26], "4": [23, 25, 26, 26], "5": [23, 24, 26, 27], "6": [22, 25, 26, 27]}}


def build(cfg):
    out = {}
    for mode in "ABCD":
        key = "probabilities_" + mode
        base = cfg[key]["1"]
        tbl = {}
        for s in range(1, 7):
            w = W[s - 1]
            row = dict(base)
            for k, f in BONUS6.items(): row[k] = int(round(base[k] * (1 + (f - 1) * w)))
            for k in RARE: row[k] = int(round(base[k] * (1 + (RARE6 - 1) * w)))
            row["HAZE"] = 65536 - sum(v for k, v in row.items() if k != "HAZE")
            assert row["HAZE"] > 0, (mode, s)
            tbl[str(s)] = row
        out[key] = tbl
    out["modeTransitions"] = {t: {s: dict(zip("ABCD", v)) for s, v in d.items()} for t, d in MODE.items()}
    return out


def replace_block(raw, key, lines):
    start = raw.index('"%s": {' % key)
    end = raw.index("\n  }", start) + len("\n  }")
    return raw[:start] + "\n".join(lines) + raw[end:]


def apply(raw, new):
    for mode in "ABCD":
        key = "probabilities_" + mode
        lines = ['"%s": {' % key]
        items = list(new[key].items())
        for i, (s, row) in enumerate(items):
            lines.append('    "%s": {' % s)
            ks = list(row.keys())
            for j, k in enumerate(ks): lines.append('      "%s": %d' % (k, row[k]) + ("," if j < len(ks) - 1 else ""))
            lines.append("    }" + ("," if i < len(items) - 1 else ""))
        lines.append("  }")
        raw = replace_block(raw, key, lines)
    lines = ['"modeTransitions": {']
    timings = list(new["modeTransitions"].items())
    for ti, (timing, bys) in enumerate(timings):
        lines.append('    "%s": {' % timing)
        items = list(bys.items())
        for i, (s, row) in enumerate(items):
            lines.append('      "%s": {' % s)
            ks = list(row.keys())
            for j, k in enumerate(ks): lines.append('        "%s": %d' % (k, row[k]) + ("," if j < len(ks) - 1 else ""))
            lines.append("      }" + ("," if i < len(items) - 1 else ""))
        lines.append("    }" + ("," if ti < len(timings) - 1 else ""))
    lines.append("  }")
    return replace_block(raw, "modeTransitions", lines)


def main():
    raw = io.open(P, encoding="utf-8-sig").read()
    cfg = json.loads(raw)
    new = build(cfg)
    if "--apply" in sys.argv:
        raw = apply(raw, new)
        io.open(P, "w", encoding="utf-8-sig", newline="").write(raw)
        cfg2 = json.loads(raw)
        for mode in "ABCD":
            for s in "123456": assert sum(cfg2["probabilities_" + mode][s].values()) == 65536
        for t in cfg2["modeTransitions"].values():
            for s in "123456": assert sum(t[s].values()) == 100
        print("書き換えた:", P)
    t = new["probabilities_A"]
    for s in "123456":
        r = t[s]
        print("A 設定%s: BB_A 1/%.0f  RB 1/%.0f  チェリー A %.2f%%  スイカ %.2f%%  チャンス %.2f%%  ハズレ %.1f%%" % (
            s, 65536 / r["BB_A"], 65536 / (r["RB_A"] + r["RB_B"]), r["CHERRY_A"] * 100 / 65536,
            (r["SUICA_A"] + r["SUICA_B"]) * 100 / 65536, sum(r["CHANCE_" + x] for x in "ABCD") * 100 / 65536, r["HAZE"] * 100 / 65536))


if __name__ == "__main__":
    main()
