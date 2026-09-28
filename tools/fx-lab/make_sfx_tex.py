# -*- coding: utf-8 -*-
"""漫画の擬音（ドン！ ズバッ バリバリ ゴゴゴ キラーン）のテクスチャを作る → textures/sfx_<名前>.png
作り: 文字ごとに大きさ・傾き・位置をずらす（1 文字目が一番大きい。強弱）→ 中は上下のグラデーション → 太い黒フチ → さらに外に色フチ → 全体を斜めに
フォントは見本用に Windows の BIZ UD ゴシック Bold。本番は OFL のフォント（例: Dela Gothic One）に替える"""
import os, math
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "textures")
FONT = "C:/Windows/Fonts/BIZ-UDGothicB.ttc"

# 名前, 文字列, 文字ごとの大きさ倍率, 上の色, 下の色, 外フチの色
WORDS = [
    ("don",  "ドン！", [1.25, 0.95, 1.1], (255, 255, 240), (255, 190, 30), (220, 30, 20)),
    ("zuba", "ズバッ", [1.2, 1.0, 0.75], (255, 255, 255), (80, 220, 255), (20, 70, 200)),
    ("bari", "バリバリ", [1.15, 0.9, 1.05, 0.85], (255, 255, 200), (255, 230, 40), (130, 40, 220)),
    ("go",   "ゴ", [1.0], (120, 40, 160), (40, 10, 70), (255, 80, 200)),
    ("kira", "キラーン", [1.1, 0.95, 0.9, 1.0], (255, 255, 255), (255, 170, 230), (230, 60, 160)),
]

def gradient(size, top, bot):
    w, h = size
    g = Image.new("RGB", (1, h))
    for y in range(h):
        t = y / max(h - 1, 1)
        g.putpixel((0, y), tuple(int(top[i] + (bot[i] - top[i]) * t) for i in range(3)))
    return g.resize((w, h))

def make(name, text, scales, top, bot, rim, base=260, seed=0):
    import random
    rnd = random.Random(hash(name) & 0xffff)
    W, H = int(base * 1.25 * len(text) + 200), int(base * 2.1)
    mask = Image.new("L", (W, H), 0)
    x = 90
    for ch, sc in zip(text, scales):
        f = ImageFont.truetype(FONT, int(base * sc))
        # 1 文字ずつ別の板に描いて回し、少し上下にずらす
        cw = int(base * sc * 1.4)
        tile = Image.new("L", (cw, cw), 0)
        ImageDraw.Draw(tile).text((cw * 0.12, cw * 0.05), ch, font=f, fill=255)
        tile = tile.rotate(rnd.uniform(-9, 9), resample=Image.BICUBIC)
        y = int(H / 2 - cw / 2 + rnd.uniform(-0.08, 0.08) * base)
        mask.paste(ImageChops.lighter(mask.crop((x, y, x + cw, y + cw)), tile), (x, y))
        x += int(base * sc * (0.92 if ch not in "ーッ！" else 0.7))
    mask = mask.crop((0, 0, min(W, x + 120), H))
    W = mask.size[0]
    # フチ: 黒（太い）→ 色（さらに外）。MaxFilter で太らせる
    black = mask.filter(ImageFilter.MaxFilter(31))
    color = black.filter(ImageFilter.MaxFilter(23))
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    img.paste(Image.new("RGB", (W, H), rim), (0, 0), color)
    img.paste(Image.new("RGB", (W, H), (10, 8, 12)), (0, 0), black)
    fill = gradient((W, H), top, bot)
    # 中の上側にハイライトの帯（コミックの光沢）
    hl = Image.new("L", (W, H), 0)
    ImageDraw.Draw(hl).rectangle((0, int(H * 0.36), W, int(H * 0.44)), fill=90)
    fill = Image.composite(Image.new("RGB", (W, H), (255, 255, 255)), fill, hl)
    img.paste(fill, (0, 0), mask)
    # 全体を斜めに（右上がりの勢い）
    shear = -0.18
    img = img.transform((int(W + abs(shear) * H), H), Image.AFFINE, (1, shear, shear * H if shear < 0 else 0, 0, 1, 0), resample=Image.BICUBIC)
    bbox = img.split()[3].getbbox(); img = img.crop(bbox)
    # 端から 6% は空けて置く（マスクが板の縁に触れないように）
    pad = int(max(img.size) * 0.06)
    out = Image.new("RGBA", (img.size[0] + pad * 2, img.size[1] + pad * 2), (0, 0, 0, 0)); out.paste(img, (pad, pad))
    s = 1024 / max(out.size)
    out = out.resize((int(out.size[0] * s), int(out.size[1] * s)), Image.LANCZOS)
    path = os.path.join(OUT, f"sfx_{name}.png"); out.save(path, optimize=True)
    print(path, out.size)

for w in WORDS:
    make(*w)
