# -*- coding: utf-8 -*-
"""扉のステップアップ（本人 2026-09-29「大門や大扉、ギミック的なシャッターが閉まり、第 1〜第 2 停止でぐらつき、第 3 で開く」）の絵を作る
出力 textures/door_gate_L.png（大門の左扉。右は左右反転で使う）/ door_vault_L.png（金庫の大扉の左）/ door_vault_lock.png（中央の錠）/ shutter.png（シャッター）
アニメ塗り: べた塗り 2〜3 段＋黒い輪郭＋金の縁取り。1 枚 = 画面の半分（左扉）または全面（シャッター）"""
import os, math
from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "textures")
W, H = 640, 720          # 左扉 1 枚（画面 1280x720 の半分）
K = 2                    # 2 倍で描いて縮める（輪郭をなめらかに）

def canvas(w, h): return Image.new("RGBA", (w * K, h * K), (0, 0, 0, 0))
def box(d, x0, y0, x1, y1, fill, outline=(15, 10, 8, 255), width=4): d.rectangle((x0 * K, y0 * K, x1 * K, y1 * K), fill=fill, outline=outline, width=width * K)
def save(im, name, w, h):
    im = im.resize((w, h), Image.LANCZOS); im.save(os.path.join(OUT, name), optimize=True); print(name, im.size)

# ---- 大門（木と鉄の門・金の縁・紋章の半分）
im = canvas(W, H); d = ImageDraw.Draw(im)
box(d, 0, 0, W, H, (70, 40, 22, 255), width=6)
for i in range(7):                                   # 縦の板
    x = 30 + i * 88
    box(d, x, 40, x + 84, H - 40, (104 if i % 2 else 92, 62, 34, 255), outline=(40, 22, 12, 255), width=3)
    d.line(((x + 20) * K, 60 * K, (x + 26) * K, (H - 60) * K), fill=(120, 76, 44, 255), width=3 * K)   # 木目
for y in (110, 360, 610):                            # 鉄の帯と鋲
    box(d, 10, y - 26, W - 4, y + 26, (58, 60, 66, 255), outline=(20, 20, 24, 255), width=4)
    d.rectangle((10 * K, (y - 26) * K, (W - 4) * K, (y - 16) * K), fill=(96, 100, 110, 255))
    for x in range(40, W, 70): d.ellipse(((x - 9) * K, (y - 9) * K, (x + 9) * K, (y + 9) * K), fill=(150, 150, 160, 255), outline=(20, 20, 24, 255), width=2 * K)
cx, cy, r = W, H // 2, 200                           # 中央の紋章（半円）: 合わせ目で丸になる
d.pieslice(((cx - r) * K, (cy - r) * K, (cx + r) * K, (cy + r) * K), 90, 270, fill=(160, 120, 40, 255), outline=(30, 20, 8, 255), width=6 * K)
d.pieslice(((cx - r + 30) * K, (cy - r + 30) * K, (cx + r - 30) * K, (cy + r - 30) * K), 90, 270, fill=(110, 30, 26, 255), outline=(230, 190, 90, 255), width=5 * K)
for a in range(100, 270, 20):                        # 放射の金の線
    t = math.radians(a); d.line(((cx + math.cos(t) * 60) * K, (cy + math.sin(t) * 60) * K, (cx + math.cos(t) * (r - 40)) * K, (cy + math.sin(t) * (r - 40)) * K), fill=(230, 190, 90, 255), width=5 * K)
d.rectangle(((W - 14) * K, 0, W * K, H * K), fill=(230, 190, 90, 255))           # 合わせ目の金
d.rectangle((0, 0, 14 * K, H * K), fill=(230, 190, 90, 255))
save(im, "door_gate_L.png", W, H)

# ---- 金庫の大扉（鋼・リベット・警告の帯）
im = canvas(W, H); d = ImageDraw.Draw(im)
box(d, 0, 0, W, H, (88, 96, 110, 255), width=6)
d.rectangle((0, 0, W * K, 40 * K), fill=(130, 140, 155, 255))                   # 上の明るい縁（光の当たり）
for (x0, y0, x1, y1) in ((50, 70, 590, 330), (50, 390, 590, 650)):              # へこんだ板
    box(d, x0, y0, x1, y1, (70, 78, 92, 255), outline=(30, 34, 40, 255), width=5)
    d.line((x0 * K, (y1 - 6) * K, x1 * K, (y1 - 6) * K), fill=(120, 130, 145, 255), width=4 * K)
    for x in range(x0 + 20, x1, 60):
        for y in (y0 + 18, y1 - 18): d.ellipse(((x - 8) * K, (y - 8) * K, (x + 8) * K, (y + 8) * K), fill=(160, 168, 180, 255), outline=(30, 34, 40, 255), width=2 * K)
for i in range(0, 16):                               # 警告の斜め帯（下）
    x = i * 50 - 40
    d.polygon([((x) * K, 680 * K), ((x + 25) * K, 680 * K), ((x + 55) * K, 720 * K), ((x + 30) * K, 720 * K)], fill=(240, 190, 20, 255))
d.rectangle((0, 674 * K, W * K, 680 * K), fill=(20, 20, 24, 255))
d.rectangle(((W - 18) * K, 0, W * K, H * K), fill=(40, 44, 52, 255))            # 合わせ目
save(im, "door_vault_L.png", W, H)

# 中央の錠（丸いハンドル）
S = 360; im = canvas(S, S); d = ImageDraw.Draw(im); c0 = S // 2
d.ellipse((10 * K, 10 * K, (S - 10) * K, (S - 10) * K), fill=(150, 158, 170, 255), outline=(25, 28, 34, 255), width=8 * K)
d.ellipse((50 * K, 50 * K, (S - 50) * K, (S - 50) * K), fill=(100, 108, 120, 255), outline=(25, 28, 34, 255), width=6 * K)
for a in range(0, 360, 60):
    t = math.radians(a); d.line(((c0 + math.cos(t) * 30) * K, (c0 + math.sin(t) * 30) * K, (c0 + math.cos(t) * 150) * K, (c0 + math.sin(t) * 150) * K), fill=(230, 190, 60, 255), width=16 * K)
d.ellipse(((c0 - 40) * K, (c0 - 40) * K, (c0 + 40) * K, (c0 + 40) * K), fill=(230, 190, 60, 255), outline=(25, 28, 34, 255), width=6 * K)
save(im, "door_vault_lock.png", S, S)

# ---- シャッター（横の板・下の警告帯・取っ手）
SW, SH = 1280, 720
im = canvas(SW, SH); d = ImageDraw.Draw(im)
for i in range(0, SH, 48):
    d.rectangle((0, i * K, SW * K, (i + 46) * K), fill=(118, 124, 134, 255))
    d.rectangle((0, (i + 30) * K, SW * K, (i + 46) * K), fill=(90, 96, 106, 255))
    d.line((0, (i + 46) * K, SW * K, (i + 46) * K), fill=(30, 32, 38, 255), width=3 * K)
for i in range(0, 30):
    x = i * 60 - 60
    d.polygon([(x * K, (SH - 60) * K), ((x + 30) * K, (SH - 60) * K), ((x + 70) * K, SH * K), ((x + 40) * K, SH * K)], fill=(240, 190, 20, 255))
d.rectangle((0, (SH - 66) * K, SW * K, (SH - 60) * K), fill=(20, 20, 24, 255))
for x in (420, 860): box(d, x - 50, SH - 110, x + 50, SH - 86, (60, 64, 72, 255), width=3)
save(im, "shutter.png", SW, SH)
