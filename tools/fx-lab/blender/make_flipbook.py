# -*- coding: utf-8 -*-
"""fire_sim.py の連番を 1 枚の連番テクスチャにする。
使い方: py -3 make_flipbook.py <入力フォルダ> <出力.png> [横コマ=8] [縦コマ=8] [1 コマの幅=256] [wall]
- wall: 横は切り抜かず、縁も空けない（枠の帯で横に並べて繰り返すため）。下端も炎の根元でそろえる
- 全コマを重ねた外接枠で切り抜く（炎が板の中で大きく見えるように）。縁は 6% 空ける（板の縁に触れると四角が見える）
- 明るさの上位 0.5% を 1.0 にそろえる（コマごとに明るさがぶれないよう、全コマで同じ倍率）
- 出力は白＋アルファ（RGB = アルファ = 明るさ）。色はゲーム側の Lab/Fire が温度の段で塗る"""
import sys, glob, os
import numpy as np
from PIL import Image

src, dst = sys.argv[1], sys.argv[2]
tx = int(sys.argv[3]) if len(sys.argv) > 3 else 8
ty = int(sys.argv[4]) if len(sys.argv) > 4 else 8
tw = int(sys.argv[5]) if len(sys.argv) > 5 else 256
wall = len(sys.argv) > 6 and sys.argv[6] == "wall"
files = sorted(glob.glob(os.path.join(src, "f_*.png")))[: tx * ty]
frames = [np.asarray(Image.open(f)).astype(np.float32) for f in files]
frames = [f[..., 0] if f.ndim == 3 else f for f in frames]
stack = np.stack(frames)
peak = np.percentile(stack[stack > 0], 99.5)
mask = (stack.max(0) > peak * 0.02)
ys, xs = np.where(mask)
y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
if wall: x0, x1 = 0, stack.shape[2]
h, w = y1 - y0, x1 - x0
pad = int(max(h, w) * 0.06)
padx = 0 if wall else pad
side_w, side_h = w + padx * 2, h + pad * (1 if wall else 2)   # 壁は下を空けない（根元 = 板の下端）
th = int(tw * side_h / side_w)
sheet = np.zeros((th * ty, tw * tx), np.float32)
for i, f in enumerate(frames):
    c = np.zeros((side_h, side_w), np.float32)
    c[pad:pad + h, padx:padx + w] = f[y0:y1, x0:x1]
    im = Image.fromarray(np.clip(c / peak, 0, 1) * 255).convert("L").resize((tw, th), Image.LANCZOS)
    r, q = divmod(i, tx)
    sheet[r * th:(r + 1) * th, q * tw:(q + 1) * tw] = np.asarray(im)
g = sheet.astype(np.uint8)
out = np.stack([g, g, g, g], -1)
Image.fromarray(out, "RGBA").save(dst, optimize=True)
print(dst, out.shape[1], "x", out.shape[0], "tile", tw, "x", th, "frames", len(frames))
