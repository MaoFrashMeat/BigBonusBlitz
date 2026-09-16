"""game_config.json の zoneFx が指す絵を Resources/Art/UI/Zones へ入れる。

    py -3 tools/ui/install_zone_art.py          # zoneFx の 3 面（エンゲージ / BIG / REG）を入れる
    py -3 tools/ui/install_zone_art.py --list   # 何が入っているかを見るだけ

透明な余白を切り、横幅 1024 を超えるものは縮めて保存する（告知は舞台の 1170px に対して最大でも 1100px）。
名前は zoneFx の image（拡張子なし）。取り込み設定（.meta）は Art/UI/Text の既存の .meta を写す。
tools/zone_viewer.html で決めた JSON を game_config.json に貼ってから走らせる。
"""
import io, json, os, sys, uuid
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CFG = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Data", "game_config.json")
OUT = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Art", "UI", "Zones")
META_SRC = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Art", "UI", "Text", "kakutoku.png.meta")
MAX_W = 1024


def write_meta(png):
    meta = png + ".meta"
    if os.path.exists(meta) or not os.path.exists(META_SRC): return
    with io.open(META_SRC, encoding="utf-8") as f: s = f.read()
    s = s.replace("guid: 5a73b330c8bc499aa8df93d1f593bb9d", "guid: " + uuid.uuid4().hex)
    with io.open(meta, "w", encoding="utf-8", newline="\n") as f: f.write(s)


def main():
    with io.open(CFG, encoding="utf-8-sig") as f: zone = json.load(f).get("zoneFx", {}) or {}
    os.makedirs(OUT, exist_ok=True)
    if "--list" in sys.argv:
        for f in sorted(os.listdir(OUT)):
            if f.endswith(".png"): print(f, Image.open(os.path.join(OUT, f)).size)
        return
    for key, z in zone.items():
        name, src = z.get("image") or "", z.get("src") or ""
        if not name or not src:
            print(f"{key}: 絵を選んでいない（文字のまま）"); continue
        path = os.path.join(ROOT, src.replace("/", os.sep))
        if not os.path.exists(path):
            print(f"{key}: 元絵が無い {src}"); continue
        im = Image.open(path).convert("RGBA")
        bb = im.split()[3].point(lambda v: 255 if v > 8 else 0).getbbox()
        if bb: im = im.crop(bb)
        if im.width > MAX_W: im = im.resize((MAX_W, max(1, round(im.height * MAX_W / im.width))), Image.LANCZOS)
        out = os.path.join(OUT, name + ".png")
        im.save(out, optimize=True); write_meta(out)
        print(f"{key}: {src} -> {os.path.relpath(out, ROOT)} {im.size}")


if __name__ == "__main__":
    main()
