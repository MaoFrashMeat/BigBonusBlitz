"""assets/logo の候補と game_config.json の zoneFx を tools/zone_defaults.js に写す（tools/zone_viewer.html 用）。

    py -3 tools/zone_sync.py

zone_viewer.html は file:// でも開けるように、JSON ではなく JS（window.BBB_ZONE）で持つ。
候補の絵を足したり game_config.json の zoneFx を変えたら、これを走らせる。
"""
import io, json, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CFG = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Data", "game_config.json")
OUT = os.path.join(ROOT, "tools", "zone_defaults.js")
FOLDERS = {"engage": os.path.join("assets", "logo", "engage"), "bonus": os.path.join("assets", "logo", "bonus")}


def main():
    images = {}
    for key, rel in FOLDERS.items():
        folder = os.path.join(ROOT, rel)
        files = sorted(f for f in os.listdir(folder) if f.lower().endswith(".png")) if os.path.isdir(folder) else []
        images[key] = ["../" + rel.replace("\\", "/") + "/" + f for f in files]
    zone = {}
    if os.path.exists(CFG):
        with io.open(CFG, encoding="utf-8-sig") as f:
            zone = json.load(f).get("zoneFx", {}) or {}
    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write("// tools/zone_sync.py が書く。zone_viewer.html の候補一覧と「既定に戻す」の値\n")
        f.write("window.BBB_ZONE = " + json.dumps({"images": images, "zoneFx": zone}, ensure_ascii=False, separators=(",", ":")) + ";\n")
    print(f"{os.path.relpath(OUT, ROOT)}: " + ", ".join(f"{k} {len(v)} 枚" for k, v in images.items()) + f"、zoneFx {len(zone)} 面")


if __name__ == "__main__":
    main()
