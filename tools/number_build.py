"""帯の数字（「n EMB 獲得！」の n）の絵を組み立てる。

    py -3 tools/number_build.py            # tools/number_choice.json どおりに Resources/Art/UI/Text/num_0..9.png を作り直す
    py -3 tools/number_build.py --preview  # 0〜9 を 1 枚に並べて tools/number_preview.png に出す

number_choice.json:
    height   … 出力の高さ（px）。透明な余白を切ってから、この高さに縮めて幅は比率のまま
    src      … 元絵のパス。{d} が数字、{v} が候補の番号（assets/symbols/number/num3_2.png なら d=3, v=2）
    variant  … 数字 → 使う候補の番号（tools/fx_viewer.html の「数字の書体」で選んで貼る）
新しい PNG には kakutoku.png.meta を写した .meta（Sprite 取り込み）を付ける。既にあれば触らない。
"""
import io, json, os, sys, uuid
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Art", "UI", "Text")
META_SRC = os.path.join(OUT, "kakutoku.png.meta")


def build_one(src, height):
    im = Image.open(src).convert("RGBA")
    alpha = im.split()[3].point(lambda v: 255 if v > 16 else 0)
    bb = alpha.getbbox() or (0, 0, im.width, im.height)
    im = im.crop(bb)
    w = max(1, round(im.width * height / im.height))
    return im.resize((w, height), Image.LANCZOS)


def write_meta(png):
    meta = png + ".meta"
    if os.path.exists(meta): return
    with io.open(META_SRC, encoding="utf-8") as f: s = f.read()
    s = s.replace("guid: 5a73b330c8bc499aa8df93d1f593bb9d", "guid: " + uuid.uuid4().hex)
    with io.open(meta, "w", encoding="utf-8", newline="\n") as f: f.write(s)


def main():
    with io.open(os.path.join(ROOT, "tools", "number_choice.json"), encoding="utf-8") as f: choice = json.load(f)
    height = int(choice.get("height", 128))
    made = []
    for d in range(10):
        v = choice["variant"].get(str(d), 1)
        src = os.path.join(ROOT, choice["src"].format(d=d, v=v))
        im = build_one(src, height)
        made.append(im)
        if "--preview" not in sys.argv:
            out = os.path.join(OUT, f"num_{d}.png")
            im.save(out, optimize=True); write_meta(out)
            print(f"{os.path.relpath(src, ROOT)} -> {os.path.relpath(out, ROOT)} {im.size}")
    if "--preview" in sys.argv:
        pad = 8
        sheet = Image.new("RGBA", (sum(i.width for i in made) + pad * 11, height + pad * 2), (30, 30, 30, 255))
        x = pad
        for im in made: sheet.alpha_composite(im, (x, pad)); x += im.width + pad
        out = os.path.join(ROOT, "tools", "number_preview.png"); sheet.save(out); print(out, sheet.size)


if __name__ == "__main__":
    main()
