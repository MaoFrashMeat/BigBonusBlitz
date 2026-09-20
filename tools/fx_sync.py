"""game_config.json の演出の値を tools/fx_defaults.js に、tools/symbol_choice.json の図柄の選択を tools/symbol_defaults.js に写す
（fx_viewer.html / symbol_viewer.html の「既定」をゲームの今の値にそろえる。他の PC で開いても同じ絵・同じ大きさで始まる）。

    py -3 tools/fx_sync.py

fx_viewer.html は file:// でも読めるように JSON でなく JS（window.BBB_FX_DEFAULTS）で持つ。
写すキーは FX の key と同じ（reelFx.emberGain / tech.rankFx）。game_config.json を変えたら実行する。
"""
import io, json, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CFG = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Data", "game_config.json")
OUT = os.path.join(ROOT, "tools", "fx_defaults.js")
KEYS = ["reelFx.emberGain", "tech.rankFx", "reelFx.naviCombo"]


def main():
    with io.open(CFG, encoding="utf-8-sig") as f: cfg = json.load(f)
    out = {}
    for key in KEYS:
        node = cfg
        for part in key.split("."):
            node = node.get(part) if isinstance(node, dict) else None
            if node is None: break
        if isinstance(node, dict): out[key] = node
    # 書体の選択（tools/text_choice.json）も写す。ビューアの「数字と獲得の書体」の初期値になる
    choice_path = os.path.join(ROOT, "tools", "text_choice.json")
    if os.path.exists(choice_path):
        with io.open(choice_path, encoding="utf-8") as f: choice = json.load(f)
        tc = dict(choice.get("numbers", {}).get("variant", {}))
        for name, w in choice.get("words", {}).items():
            if isinstance(w, dict): tc[name] = w.get("variant", 1)
        out["textChoice"] = tc
    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write("// tools/fx_sync.py が game_config.json から写す。fx_viewer.html の「既定」（既定に戻す）はこの値\n")
        f.write("window.BBB_FX_DEFAULTS = " + json.dumps(out, ensure_ascii=False, separators=(",", ":")) + ";\n")
    print(f"{os.path.relpath(OUT, ROOT)}: " + ", ".join(f"{k} {len(v)} 項目" for k, v in out.items()))
    # 図柄の選択（symbol_viewer.html 用）。キーは Unity 側の名前（red7 / blue7 / bar / star / …）
    sym_path = os.path.join(ROOT, "tools", "symbol_choice.json")
    if os.path.exists(sym_path):
        with io.open(sym_path, encoding="utf-8") as f: sym = json.load(f)
        sym_out = os.path.join(ROOT, "tools", "symbol_defaults.js")
        with io.open(sym_out, "w", encoding="utf-8", newline="\n") as f:
            f.write("// tools/fx_sync.py が tools/symbol_choice.json から写す。symbol_viewer.html の「既定」（選び直す）はこの値\n")
            f.write("window.BBB_SYMBOL_DEFAULTS = " + json.dumps(sym.get("symbols", {}), ensure_ascii=False, separators=(",", ":")) + ";\n")
        print(f"{os.path.relpath(sym_out, ROOT)}: {len(sym.get('symbols', {}))} 図柄")


if __name__ == "__main__":
    main()
