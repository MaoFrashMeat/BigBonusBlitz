# -*- coding: utf-8 -*-
"""タイトル画面の見え方を確かめる（iPhone 横持ちの比で描く）。

    py -3 docs/preview_title.py out.png

Unity を開かずに配置だけ検分するためのもの。数値は TitleScreen に合わせてある。
"""
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import os

CW, CH = 1170, 540          # 高さ 540 固定、横は iPhone の比（2.168）
SW, SH = 960, 540           # 舞台
SS = 2
TitleX, LogoY, LogoW = -236.0, 108.0, 456.0

def F(sz):
    for n in ('meiryo.ttc','YuGothM.ttc','msgothic.ttc'):
        p=os.path.join('C:/Windows/Fonts',n)
        if os.path.exists(p):
            try: return ImageFont.truetype(p, sz)
            except Exception: pass
    return ImageFont.load_default()

im = Image.new('RGBA', (CW*SS, CH*SS), (10, 11, 15, 255))

# 背景: 1 枚絵を画面いっぱいに覆う（余白ではなく切る）
art = Image.open('D:/Mao-PC/Github/BigBonusBlitz/assets/Title.png').convert('RGBA')
ar = art.width / art.height
if CW / CH > ar: nw, nh = CW*SS, int(CW*SS/ar)
else:            nh, nw = CH*SS, int(CH*SS*ar)
art = art.resize((nw, nh), Image.LANCZOS)
im.alpha_composite(art, ((CW*SS-nw)//2, (CH*SS-nh)//2))

# 左半分を暗くする横グラデ（alpha は t^2 で落とす）
scr = Image.new('RGBA', im.size, (0,0,0,0))
sd = ImageDraw.Draw(scr)
for x in range(CW*SS):
    t = 1.0 - x/(CW*SS-1)
    sd.rectangle((x, 0, x+1, CH*SS), fill=(0,0,0,int(184*t*t)))
im.alpha_composite(scr)
# 下のかすかな暗み
vg = Image.new('RGBA', im.size, (0,0,0,0))
vd = ImageDraw.Draw(vg)
for y in range(int(CH*SS*0.45)):
    a = int(128 * (y/(CH*SS*0.45)))
    vd.rectangle((0, CH*SS-1-y, CW*SS, CH*SS-y), fill=(0,0,0,a))
im.alpha_composite(vg)

d = ImageDraw.Draw(im, 'RGBA')
def P(x, y):                       # 舞台座標 → 画像座標（舞台は画面中央）
    return ((CW*0.5 + x)*SS, (CH*0.5 - y)*SS)

# ロゴ
logo = Image.open('D:/Mao-PC/Github/BigBonusBlitz/assets/logo.png').convert('RGBA')
lh = LogoW * logo.height / logo.width
logo = logo.resize((int(LogoW*SS), int(lh*SS)), Image.LANCZOS)
lx, ly = P(TitleX - LogoW*0.5, LogoY + lh*0.5)
im.alpha_composite(logo, (int(lx), int(ly)))

# ボタン
f_b, f_p = F(18*SS), F(15*SS)
for y, lbl, col in ((-44, 'つづきから', (255,77,109)), (-104, 'はじめから', (39,41,52))):
    x0, y0 = P(TitleX-140, y+26); x1, y1 = P(TitleX+140, y-26)
    d.rounded_rectangle((x0, y0, x1, y1), 14*SS, fill=(95,61,30))
    d.rounded_rectangle((x0+3*SS, y0+3*SS, x1-3*SS, y1-3*SS), 12*SS, fill=col)
    d.text(P(TitleX, y), lbl, font=f_b, fill=(244,246,250), anchor='mm')
d.text((P(TitleX, -208)[0]+1.5*SS, P(TitleX, -208)[1]+1.5*SS), 'SPACE / ENTER でつづきから', font=f_p, fill=(0,0,0,217), anchor='mm')
d.text(P(TitleX, -208), 'SPACE / ENTER でつづきから', font=f_p, fill=(244,246,250), anchor='mm')

# 舞台の外側（左右 105px ずつ）に薄い線を引いて、どこまでが舞台かを示す
for x in (-SW*0.5, SW*0.5):
    px = P(x, 0)[0]
    d.line((px, 0, px, CH*SS), fill=(255,255,255,40), width=SS)
d.text(P(-SW*0.5+6, -252), '← 舞台 960 →', font=F(11*SS), fill=(255,255,255,110), anchor='lm')

import sys
out_path = sys.argv[1] if len(sys.argv) > 1 else 'title_preview.png'
im.convert('RGB').resize((CW, CH), Image.LANCZOS).save(out_path)
print('タイトルの確認用の絵を作った', (CW, CH))
