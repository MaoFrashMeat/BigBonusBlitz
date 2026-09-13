"""game_config.json の演出の値を tools/fx_defaults.js に写す（tools/fx_viewer.html の「既定」をゲームの今の値にそろえる）。

    py -3 tools/fx_sync.py

fx_viewer.html は file:// でも読めるように JSON でなく JS（window.BBB_FX_DEFAULTS）で持つ。
写すキーは FX の key と同じ（reelFx.emberGain / tech.rankFx）。game_config.json を変えたら実行する。
"""
import io, json, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CFG = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Data", "game_config.json")
OUT = os.path.join(ROOT, "tools", "fx_defaults.js")
KEYS = ["reelFx.emberGain", "tech.rankFx"]


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


if __name__ == "__main__":
    main()
