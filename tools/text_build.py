"""帯の文字の絵（「n EMB 獲得！」の数字と「獲得」）を組み立てる。

    py -3 tools/text_build.py            # tools/text_choice.json どおりに Resources/Art/UI/Text/num_0..9.png と kakutoku.png を作り直す
    py -3 tools/text_build.py --preview  # 出来上がりを 1 枚に並べて tools/text_preview.png に出す
    py -3 tools/text_build.py --rects    # 候補の「余白を切った範囲」だけを tools/text_rects.js に書く（普通の組み立てでも最後に書く）

text_choice.json:
    numbers.height   … 数字の出力の高さ（px）。透明な余白を切ってから、この高さに縮めて幅は比率のまま
    numbers.src      … 元絵のパス。{d} が数字、{v} が候補の番号（assets/symbols/number/num3_2.png なら d=3, v=2）
    numbers.variant  … 数字 → 使う候補の番号
    words.height     … 言葉の絵の出力の高さ（px）
    words.<名前>     … { src: 候補のパス（{v} が候補の番号）, variant: 使う候補 }。名前がそのまま出力名（kakutoku → kakutoku.png）
候補は tools/fx_viewer.html の「数字と文字の書体」で選んで、出た JSON をこのファイルに貼る。
ビューアは file:// で開くと絵の中身を読めない（余白を切れない）ので、候補の範囲は tools/text_rects.js から読む。
候補を足したら `--rects`（か普通の組み立て）で書き直す。
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


def trim_rect(path):
    im = Image.open(path).convert("RGBA")
    alpha = im.split()[3].point(lambda v: 255 if v > 16 else 0)
    bb = alpha.getbbox() or (0, 0, im.width, im.height)
    return [bb[0], bb[1], bb[2] - bb[0], bb[3] - bb[1]]


def write_rects():
    """assets/symbols/number と text の候補すべての、余白を切った範囲を tools/text_rects.js に書く（ビューア用）。"""
    rects = {}
    for sub in ("number", "text"):
        folder = os.path.join(ROOT, "assets", "symbols", sub)
        if not os.path.isdir(folder): continue
        for f in sorted(os.listdir(folder)):
            if f.lower().endswith(".png"): rects[f] = trim_rect(os.path.join(folder, f))
    out = os.path.join(ROOT, "tools", "text_rects.js")
    with io.open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write("// tools/text_build.py --rects が書く。候補の絵の「余白を切った範囲」[x, y, w, h]（fx_viewer.html が file:// でも読めるように JS で持つ）\n")
        f.write("window.BBB_TEXT_RECTS = " + json.dumps(rects, ensure_ascii=False, separators=(",", ":")) + ";\n")
    print(f"{os.path.relpath(out, ROOT)}: {len(rects)} 件")


def main():
    if "--rects" in sys.argv:
        write_rects(); return
    with io.open(os.path.join(ROOT, "tools", "text_choice.json"), encoding="utf-8") as f: choice = json.load(f)
    preview = "--preview" in sys.argv
    jobs = []   # (元絵, 高さ, 出力名)
    nums = choice.get("numbers", {})
    for d in range(10):
        v = nums.get("variant", {}).get(str(d), 1)
        jobs.append((nums["src"].format(d=d, v=v), int(nums.get("height", 128)), f"num_{d}"))
    words = choice.get("words", {})
    for name, w in words.items():
        if not isinstance(w, dict): continue
        jobs.append((w["src"].format(v=w.get("variant", 1)), int(w.get("height", words.get("height", 110))), name))
    made = []
    for src, height, name in jobs:
        im = build_one(os.path.join(ROOT, src), height)
        made.append(im)
        if not preview:
            out = os.path.join(OUT, name + ".png")
            im.save(out, optimize=True); write_meta(out)
            print(f"{src} -> {os.path.relpath(out, ROOT)} {im.size}")
    if preview:
        pad = 8; h = max(i.height for i in made)
        sheet = Image.new("RGBA", (sum(i.width for i in made) + pad * (len(made) + 1), h + pad * 2), (30, 30, 30, 255))
        x = pad
        for im in made: sheet.alpha_composite(im, (x, pad + (h - im.height) // 2)); x += im.width + pad
        out = os.path.join(ROOT, "tools", "text_preview.png"); sheet.save(out); print(out, sheet.size)
    else:
        write_rects()


if __name__ == "__main__":
    main()
