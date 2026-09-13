"""リールの図柄を組み立てる。

    py -3 tools/symbol_build.py            # tools/symbol_choice.json どおりに Resources/Art/Symbols/*.png を作り直す
    py -3 tools/symbol_build.py --preview  # 出来上がりを 1 枚に並べて scratch に置かず tools/symbol_preview.png に出す

symbol_choice.json:
    cell      … 1 コマの比率（今のリールは 160×73）。出力はその 2 倍（320×146）の透過 PNG
    symbols   … 図柄名 → { src: 元絵, scale: コマに対する大きさ（1.0 で枠いっぱい）, key_bg: 元絵の背景色を透明にする }
元絵は透明な余白を切ってから、コマ × scale に収まる大きさに縮めて中央に置く（tools/symbol_viewer.html の見え方と同じ手順）。
"""
import io, json, os, sys
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Art", "Symbols")


def key_background(im, tol=28):
    """四隅の色を背景とみなして透明にする（近い色ほど透明。縁は少し残す）。"""
    im = im.convert("RGBA")
    px = im.load()
    w, h = im.size
    corners = [px[0, 0], px[w - 1, 0], px[0, h - 1], px[w - 1, h - 1]]
    bg = tuple(sum(c[i] for c in corners) // 4 for i in range(3))
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            d = max(abs(r - bg[0]), abs(g - bg[1]), abs(b - bg[2]))
            if d < tol:
                px[x, y] = (r, g, b, 0 if d < tol * 0.5 else int(a * (d - tol * 0.5) / (tol * 0.5)))
    return im


def build_one(src, scale, key_bg, cell_w, cell_h, mult=2):
    im = Image.open(src)
    im = key_background(im) if key_bg else im.convert("RGBA")
    alpha = im.split()[3].point(lambda v: 255 if v > 16 else 0)
    bb = alpha.getbbox() or (0, 0, im.width, im.height)
    im = im.crop(bb)
    W, H = cell_w * mult, cell_h * mult
    k = min(W * scale / im.width, H * scale / im.height)
    w, h = max(1, round(im.width * k)), max(1, round(im.height * k))
    im = im.resize((w, h), Image.LANCZOS)
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    canvas.paste(im, ((W - w) // 2, (H - h) // 2), im)
    return canvas


def main():
    cfg = json.load(io.open(os.path.join(ROOT, "tools", "symbol_choice.json"), encoding="utf-8"))
    cell_w, cell_h = cfg.get("cell", [160, 73])
    mult = int(cfg.get("multiplier", 2))
    outs = {}
    for name, spec in cfg["symbols"].items():
        src = os.path.join(ROOT, spec["src"])
        if spec.get("key_bg"):
            # ビューア用に、背景を透明にした元絵も置いておく（ビューアは file:// で画素を触れないため）
            keyed = key_background(Image.open(src))
            keyed.save(os.path.join(ROOT, "tools", "symbol_src", name + "_keyed.png"))
        img = build_one(src, float(spec.get("scale", 1.0)), bool(spec.get("key_bg", False)), cell_w, cell_h, mult)
        outs[name] = img
        if "--preview" not in sys.argv:
            img.save(os.path.join(OUT, name + ".png"))
            print(f"{name:<11} <- {spec['src']}  scale {spec.get('scale', 1.0)}")
    # 並べた確認用（リールの地の色に載せる）
    names = list(outs)
    sheet = Image.new("RGBA", (len(names) * (cell_w * mult + 12) + 12, cell_h * mult + 24), (242, 242, 242, 255))
    for i, n in enumerate(names):
        sheet.paste(outs[n], (12 + i * (cell_w * mult + 12), 12), outs[n])
    sheet.save(os.path.join(ROOT, "tools", "symbol_preview.png"))
    print("preview -> tools/symbol_preview.png")


if __name__ == "__main__":
    main()
