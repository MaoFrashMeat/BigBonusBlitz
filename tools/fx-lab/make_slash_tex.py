# -*- coding: utf-8 -*-
"""斬撃の繊維テクスチャを作る（docs/FX_RESEARCH.md の調べ: 細い線の重ね・点の横ぶれ・横に伸ばした消えノイズ）。
出力 textures/slash_fibers.png（1024x128・リニア・U 方向に継ぎ目なし）
  R = 繊維 1（線 34 本＋横ぶれの点） G = 繊維 2（別の乱数。R と違う速さで流して単調さを消す） B = 消えるためのノイズ（fBm を U に 4 倍伸ばす）
V は 0 = 内側、1 = 外縁（刃先）。外縁に寄せて線を多く置く"""
import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter, uniform_filter1d
import os
W, H = 1024, 128   # 帯は画面で 40〜80px。V 128 なら線 3〜8px が画面で 1.5〜4px になる

def fibers(seed):
    r = np.random.default_rng(seed)
    u = np.arange(W)[None, :] / W; v = np.arange(H)[:, None] / H
    img = np.zeros((H, W))
    for _ in range(34):
        v0 = 1 - r.random() ** 1.6            # 外縁寄り
        w = r.uniform(2.5, 7.0) / H           # 線の太さ 2.5〜7px
        a = r.uniform(0.3, 1.0)
        us = r.random(); L = r.uniform(0.25, 0.95)
        du = (u - us) % 1.0                   # U は巻き戻して継ぎ目なし
        edge = np.clip(du / 0.1, 0, 1) * np.clip((L - du) / 0.15, 0, 1) * (du < L)
        wob = 0.75 + 0.25 * np.sin(2 * np.pi * (u * r.integers(1, 4) + r.random()))
        img += a * np.exp(-((v - v0) / w) ** 2) * edge * wob
    # 点を U 方向にだけぼかして筋にする（Photoshop の「ぼかし（移動）」）
    dots = (r.random((H, W)) > 0.994).astype(float) * r.uniform(0.5, 1.0, (H, W))
    streak = uniform_filter1d(dots, size=W // 6, axis=1, mode="wrap") * (W // 6) * 0.25
    streak = gaussian_filter(streak, (1.2, 0), mode="wrap")
    img += streak * (0.3 + 0.7 * v)
    return np.tanh(img * 1.1)

def erosion(seed):
    r = np.random.default_rng(seed); acc = np.zeros((H, W)); amp = 1.0
    for sig in (24, 10, 4):
        n = gaussian_filter(r.standard_normal((H, W)), (sig * 0.5, sig * 2.0), mode="wrap")   # U に 4 倍伸ばす
        acc += amp * n / n.std(); amp *= 0.5
    acc -= acc.min(); return acc / acc.max()

out = np.stack([fibers(11), fibers(29), erosion(47), np.ones((H, W))], -1)
img = Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8)[::-1], "RGBA")   # 画像の下 = V0（内側）
dst = os.path.join(os.path.dirname(os.path.abspath(__file__)), "textures", "slash_fibers.png")
img.save(dst, optimize=True); print(dst, os.path.getsize(dst) // 1024, "KB")
