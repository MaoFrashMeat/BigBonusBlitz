# -*- coding: utf-8 -*-
"""盾の紋章（光の盾）のテクスチャを作る → textures/shield_crest.png（512x512、白＋アルファ。色はゲーム側で付ける）
形: カイトシールド（上が平ら、下が尖る）。外枠の太い光の線・内枠の細い線・中央の十字と菱形の紋・面はうっすら
明るさの段: 外枠 1.0 / 紋 0.9 / 内枠 0.7 / 面 0.25（面は上ほど明るいグラデーション）"""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

S = 512
def shield_poly(scale=1.0, cx=256, cy=250):
    w, h = 190 * scale, 225 * scale
    pts = []
    # 上辺（少しだけ弧）
    for i in range(21):
        x = -w + 2 * w * i / 20
        pts.append((cx + x, cy - h + 18 * scale * (1 - (x / w) ** 2) * -1 + 18 * scale))
    # 右の辺 → 下の尖り → 左の辺（2 次曲線）
    for i in range(1, 41):
        t = i / 40
        x = w * max(0.0, 1 - min(1.0, max(0.0, (t - 0.42) / 0.58)) ** 1.6) ** 0.9   # 上半分はまっすぐ、下で丸く尖る
        y = -h + 18 * scale + (2 * h) * t
        pts.append((cx + x, cy + y))
    for i in range(39, 0, -1):
        t = i / 40
        x = -w * max(0.0, 1 - min(1.0, max(0.0, (t - 0.42) / 0.58)) ** 1.6) ** 0.9
        y = -h + 18 * scale + (2 * h) * t
        pts.append((cx + x, cy + y))
    return pts

def draw(level_fn):
    im = Image.new("L", (S * 2, S * 2), 0)
    d = ImageDraw.Draw(im)
    level_fn(d)
    return np.asarray(im.resize((S, S), Image.LANCZOS)).astype(np.float32) / 255

def scaled(pts): return [(x * 2, y * 2) for x, y in pts]

outer = draw(lambda d: d.line(scaled(shield_poly(1.0)) + [scaled(shield_poly(1.0))[0]], fill=255, width=26, joint="curve"))
inner = draw(lambda d: d.line(scaled(shield_poly(0.84)) + [scaled(shield_poly(0.84))[0]], fill=255, width=9, joint="curve"))
face_mask = draw(lambda d: d.polygon(scaled(shield_poly(1.0)), fill=255))
def emblem(d):
    cx, cy = 512, 470
    d.rectangle((cx - 20, cy - 250, cx + 20, cy + 260), fill=255)     # 縦
    d.rectangle((cx - 230, cy - 90, cx + 230, cy - 50), fill=255)     # 横
    d.polygon([(cx, cy - 170), (cx + 95, cy - 70), (cx, cy + 40), (cx - 95, cy - 70)], outline=255, width=18)   # 菱形
emb = draw(emblem)
yy = np.linspace(1, 0, S)[:, None] * np.ones((1, S))
face = face_mask * (0.12 + 0.2 * yy)
a = np.maximum.reduce([outer * 1.0, emb * 0.9, inner * 0.7, face])
glow = np.asarray(Image.fromarray((np.maximum(outer, emb) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(10))).astype(np.float32) / 255
a = np.clip(np.maximum(a, glow * 0.45), 0, 1)
g = (a * 255).astype(np.uint8)
out = np.stack([np.full_like(g, 255)] * 3 + [g], -1)
dst = os.path.join(os.path.dirname(os.path.abspath(__file__)), "textures", "shield_crest.png")
Image.fromarray(out, "RGBA").save(dst, optimize=True); print(dst)
