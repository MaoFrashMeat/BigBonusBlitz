# -*- coding: utf-8 -*-
"""コマ画像から「時間の形」を測って図にする（本人 2026-10-01「苦手ならそれを補う学習をしろ」→ 目で分からない時間の良し悪しを数で見る）
使い方: py -3 timing_chart.py <コマのフォルダ> <出力 png> [名前]
描くもの（横軸はコマ番号。60fps）:
  明るさ   … 画面の平均明度（舞台を含む）。溜めで沈み、頂点で跳ね、散りで落ちる形になっているか
  最大     … 画面の上位 1% の明度。白（フラッシュ）が 1〜2 コマだけ出ているか
  動き     … 隣のコマとの差の平均。止め（ヒットストップ）は 0 になる。速い層があれば大きい
検査表（docs/FX_KNOWHOW.md 4.13）の「溜め → 頂点 → 散り」「白は 1〜2 コマ」「止めがあるか」「散りは短いか」をこの図で確かめる"""
import sys, os, glob
import numpy as np
from PIL import Image, ImageDraw, ImageFont

src, out = sys.argv[1], sys.argv[2]
name = sys.argv[3] if len(sys.argv) > 3 else os.path.basename(src.rstrip("/\\"))
files = sorted(glob.glob(os.path.join(src, "f_*.png")))
assert files, src
lum, top, mot = [], [], []
prev = None
for f in files:
    im = np.asarray(Image.open(f).convert("L").resize((320, 180), Image.BILINEAR), dtype=np.float32) / 255.0
    lum.append(float(im.mean())); top.append(float(np.percentile(im, 99)))
    mot.append(0.0 if prev is None else float(np.abs(im - prev).mean()))
    prev = im
lum, top, mot = np.array(lum), np.array(top), np.array(mot)
n = len(files)

W, H = 1400, 520; pad = 60
img = Image.new("RGB", (W, H), (18, 18, 22)); d = ImageDraw.Draw(img)
try: font = ImageFont.truetype("C:/Windows/Fonts/meiryo.ttc", 16)
except Exception: font = ImageFont.load_default()
def X(i): return pad + (W - 2 * pad) * i / max(1, n - 1)
def Y(v, lo, hi): return H - pad - (H - 2 * pad) * (v - lo) / (hi - lo)
# 目盛り: 10 コマごと（0.1667 秒）
for i in range(0, n, 10):
    d.line([(X(i), pad), (X(i), H - pad)], fill=(40, 40, 48)); d.text((X(i) - 8, H - pad + 6), str(i), fill=(140, 140, 150), font=font)
for i in range(0, n, 60): d.line([(X(i), pad), (X(i), H - pad)], fill=(70, 70, 90))
def plot(vals, lo, hi, col):
    pts = [(X(i), Y(v, lo, hi)) for i, v in enumerate(vals)]
    d.line(pts, fill=col, width=2)
plot(lum, 0, 1, (255, 200, 80)); plot(top, 0, 1, (255, 255, 255)); plot(mot, 0, max(0.05, float(mot.max())), (90, 180, 255))
# 止め（動きがほぼ 0）の区間と、白（最大 > 0.97）のコマを印す
for i in range(1, n):
    if mot[i] < 0.0015: d.rectangle([X(i) - 2, pad, X(i) + 2, pad + 8], fill=(255, 80, 80))
    if top[i] > 0.93: d.rectangle([X(i) - 2, pad + 12, X(i) + 2, pad + 20], fill=(255, 255, 255))
d.text((pad, 12), f"{name}  {n} コマ  黄=明るさ 白=最大(上位1%) 青=動き  赤印=止め(動き≈0) 白印=白(最大>0.93)", fill=(220, 220, 230), font=font)
# 数で要約: 最大の明るさのコマ・白のコマ数・止めの長さ・明るさが頂点の 30% に落ちるまでのコマ数
peak = int(lum.argmax()); whites = int((top > 0.93).sum()); holds = int((mot[1:] < 0.0015).sum())   # トーンマップ後は 0.95 付近が上限なので 0.93 を「白」とみなす
after = lum[peak:]; thr = lum[peak] * 0.3 + lum[:max(1, peak)].min() * 0.7 if peak > 0 else lum[peak] * 0.3
fall = int(np.argmax(after < thr)) if (after < thr).any() else -1
summ = f"頂点={peak} コマ  白={whites} コマ  止め={holds} コマ  頂点から 30% まで={fall} コマ ({fall / 60:.2f} 秒)" if fall >= 0 else f"頂点={peak} コマ  白={whites} コマ  止め={holds} コマ  散りきらない"
d.text((pad, 34), summ, fill=(255, 220, 120), font=font)
img.save(out); print(summ)
