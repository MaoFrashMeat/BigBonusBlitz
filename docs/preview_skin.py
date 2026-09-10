# -*- coding: utf-8 -*-
"""UiSkin.Card のとおりに描いて、枠の見え方を確かめる。

    py -3 docs/preview_skin.py out.png

Unity を開かずに枠だけ検分するためのもので、配置は GameController の定数に合わせてある。
"""
from PIL import Image, ImageDraw, ImageFilter, ImageFont
import os

SW, SH = 960, 540           # SafeStage
SS = 2                      # 拡大して描いてから縮める
BG = (10, 11, 15)
PANEL = (23, 24, 30)
EDGE = (95, 61, 30)
INSET = (11, 12, 16)
EMBER = (255, 138, 58)
RIVET = (70, 72, 79)
RIVET_HI = (146, 150, 164)
TEXT = (244, 246, 250)
SUB = (152, 163, 184)
GOLD = (255, 207, 63)
GREEN = (126, 224, 160)
ACCENT = (255, 77, 109)

def F(sz):
    for n in ('meiryo.ttc', 'YuGothM.ttc', 'msgothic.ttc'):
        p = os.path.join('C:/Windows/Fonts', n)
        if os.path.exists(p):
            try: return ImageFont.truetype(p, sz)
            except Exception: pass
    return ImageFont.load_default()

def card(base, cx, cy, w, h, radius=12, sheen=True, rivets=True, body=None):
    """UiSkin.Card と同じ手順で 1 枚描く。座標は中心・Unity 流に上が +y。"""
    r = max(2, min(radius, 6)) * SS
    x0 = (SW * 0.5 + cx - w * 0.5) * SS
    y0 = (SH * 0.5 - cy - h * 0.5) * SS
    x1, y1 = x0 + w * SS, y0 + h * SS

    # 影（下へ 5px ずらして 20px 大きく）
    lay = Image.new('RGBA', base.size, (0, 0, 0, 0))
    ImageDraw.Draw(lay).rounded_rectangle(
        (x0 - 10*SS, y0 - 10*SS + 5*SS, x1 + 10*SS, y1 + 10*SS + 5*SS), r, fill=(0, 0, 0, 158))
    base.alpha_composite(lay.filter(ImageFilter.GaussianBlur(6*SS)))

    # 縁からにじむ灯り（26px 大きく、alpha 0.30）
    lay = Image.new('RGBA', base.size, (0, 0, 0, 0))
    ImageDraw.Draw(lay).rounded_rectangle(
        (x0 - 8*SS, y0 - 8*SS, x1 + 8*SS, y1 + 8*SS), r, fill=EMBER + (38,))
    base.alpha_composite(lay.filter(ImageFilter.GaussianBlur(5*SS)))

    d = ImageDraw.Draw(base, 'RGBA')
    d.rounded_rectangle((x0, y0, x1, y1), r, fill=EDGE)                     # 焼けた縁
    ir = max(2, r - 2*SS)
    d.rounded_rectangle((x0 + 3*SS, y0 + 3*SS, x1 - 3*SS, y1 - 3*SS), ir, fill=body or PANEL)

    if sheen:                                                              # 上から落ちる金属の艶
        lay = Image.new('RGBA', base.size, (0, 0, 0, 0))
        ld = ImageDraw.Draw(lay)
        sh_h = h * 0.5 * SS
        top = y0 + (h * 0.5 - h * 0.22 - h * 0.25) * SS
        for k in range(int(sh_h)):
            a = int(19 * (1 - k / sh_h))
            ld.rectangle((x0 + 4*SS, top + k, x1 - 4*SS, top + k + 1), fill=(199, 207, 230, a))
        base.alpha_composite(lay)

    if rivets and w >= 90 and h >= 60:                                     # 四隅の鋲
        for sx in (-1, 1):
            for sy in (-1, 1):
                rx = (SW*0.5 + cx + sx*(w*0.5 - 11)) * SS
                ry = (SH*0.5 - cy - sy*(h*0.5 - 11)) * SS
                d.ellipse((rx-4.5*SS, ry-4.5*SS, rx+4.5*SS, ry+4.5*SS), fill=RIVET)
                d.ellipse((rx-1.2*SS-2*SS, ry-1.2*SS-2*SS, rx-1.2*SS+2*SS, ry-1.2*SS+2*SS), fill=RIVET_HI)
    return d

def inset(d, cx, cy, w, h, radius=8):
    r = radius * SS
    x0 = (SW*0.5 + cx - w*0.5) * SS; y0 = (SH*0.5 - cy - h*0.5) * SS
    d.rounded_rectangle((x0, y0, x0 + w*SS, y0 + h*SS), r, fill=INSET)
    d.line((x0 + r, y0 + h*SS - SS, x0 + w*SS - r, y0 + h*SS - SS), fill=(255, 158, 82, 26), width=SS)

def txt(d, cx, cy, s, font, col, anchor='lm'):
    d.text(((SW*0.5 + cx) * SS, (SH*0.5 - cy) * SS), s, font=font, fill=col, anchor=anchor)

im = Image.new('RGBA', (SW*SS, SH*SS), BG + (255,))
f_s, f_m, f_l, f_xl = F(11*SS), F(13*SS), F(16*SS), F(26*SS)

# 上段: ステージのカード（GameController の StageCard 相当）
d = card(im, 0, 155, 944, 176, radius=12)
txt(d, -440, 215, 'ステージ  C-3  《燠の回廊》', f_m, TEXT)
txt(d, 300, 215, 'ライフ  148 / 180', f_m, GREEN)
inset(d, 0, 150, 900, 108, 8)
txt(d, -430, 150, '次の道: リプレイを 15 回  /  ？？？？', f_m, SUB)

# 中段: 左の表示器 / リール / 右の操作パネル
d = card(im, -363, -100, 218, 196, radius=12)
txt(d, -450, -35, 'EMBER', f_s, SUB)
txt(d, -450, -58, '12,480', f_xl, GOLD)
inset(d, -363, -140, 194, 60, 8)

d = card(im, 0, -100, 468, 196, radius=12, sheen=False, rivets=False)
for i in range(3):
    inset(d, (i - 1) * 148, -100, 132, 176, 6)

d = card(im, 363, -100, 218, 196, radius=12)
txt(d, 276, -12, 'PLAYER', f_s, SUB)
txt(d, 276, -36, 'Lv 12', f_l, GOLD)
txt(d, 276, -124, '状態: 高確ステージ', f_m, TEXT)
inset(d, 363, -183, 194, 22, 5)          # 常駐スランプ

# 下段: 操作バー
for cx, w, lbl, col in ((-363, 218, 'MAX BET', ACCENT), (-148, 132, 'STOP', (39, 41, 52)),
                        (0, 132, 'STOP', (39, 41, 52)), (148, 132, 'STOP', (39, 41, 52)),
                        (363, 218, 'AUTO', (39, 41, 52))):
    d = card(im, cx, -234, w, 56, radius=10, rivets=False, body=col)
    txt(d, cx, -234, lbl, f_l, TEXT, anchor='mm')

import sys
out_path = sys.argv[1] if len(sys.argv) > 1 else 'skin_preview.png'
im.convert('RGB').resize((SW, SH), Image.LANCZOS).save(out_path)
print('確認用の絵を作った')
