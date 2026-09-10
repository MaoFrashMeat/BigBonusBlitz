# -*- coding: utf-8 -*-
"""タイトル画面の見え方を確かめる（iPhone 横持ちの比で描く）。

    py -3 docs/preview_title.py out.png

Unity を開かずに配置だけ検分するためのもの。数値は TitleScreen に合わせてある。
"""
from PIL import Image, ImageDraw, ImageFont
import os, sys

CW, CH = 1170, 540
SW, SH = 960, 540
SS = 2
TitleX, LogoY, LogoW = -166.0, 112.0, 470.0
TapY = -52.0
PillW, PillH, PillY = 152.0, 40.0, -186.0
GOLD = (255, 207, 63)
TEXT = (244, 246, 250)
SUB = (152, 163, 184)
BTN = (39, 41, 52)
EDGE = (95, 61, 30)

def F(sz):
    for n in ('meiryo.ttc','YuGothM.ttc','msgothic.ttc'):
        p=os.path.join('C:/Windows/Fonts',n)
        if os.path.exists(p):
            try: return ImageFont.truetype(p, sz)
            except Exception: pass
    return ImageFont.load_default()

im = Image.new('RGBA', (CW*SS, CH*SS), (10, 11, 15, 255))
art = Image.open('D:/Mao-PC/Github/BigBonusBlitz/assets/Title.png').convert('RGBA')
ar = art.width / art.height
if CW / CH > ar: nw, nh = CW*SS, int(CW*SS/ar)
else:            nh, nw = CH*SS, int(CH*SS*ar)
art = art.resize((nw, nh), Image.LANCZOS)
im.alpha_composite(art, ((CW*SS-nw)//2, (CH*SS-nh)//2))

scr = Image.new('RGBA', im.size, (0,0,0,0)); sd = ImageDraw.Draw(scr)
for x in range(CW*SS):
    t = 1.0 - x/(CW*SS-1)
    sd.rectangle((x, 0, x+1, CH*SS), fill=(0,0,0,int(184*t*t)))
im.alpha_composite(scr)
vg = Image.new('RGBA', im.size, (0,0,0,0)); vd = ImageDraw.Draw(vg)
for y in range(int(CH*SS*0.45)):
    a = int(128 * (y/(CH*SS*0.45)))
    vd.rectangle((0, CH*SS-1-y, CW*SS, CH*SS-y), fill=(0,0,0,a))
im.alpha_composite(vg)

d = ImageDraw.Draw(im, 'RGBA')
def P(x, y): return ((CW*0.5 + x)*SS, (CH*0.5 - y)*SS)
def shade(xy, txt, font, col, anchor='mm'):
    d.text((xy[0]+1.5*SS, xy[1]+1.5*SS), txt, font=font, fill=(0,0,0,217), anchor=anchor)
    d.text(xy, txt, font=font, fill=col, anchor=anchor)

# ロゴ
logo = Image.open('D:/Mao-PC/Github/BigBonusBlitz/UnityProject/BigBonusBlitz/Assets/Resources/Art/UI/bbb_logo_main.png').convert('RGBA')
lh = LogoW * logo.height / logo.width
logo = logo.resize((int(LogoW*SS), int(lh*SS)), Image.LANCZOS)
lx, ly = P(TitleX - LogoW*0.5, LogoY + lh*0.5)
im.alpha_composite(logo, (int(lx), int(ly)))

# 縦書き
def vtext(x, ytop, txt, size, col):
    f = F(size*SS)
    for k, ch in enumerate(txt):
        if ch in 'ー―-': ch = '｜'
        shade(P(x, ytop - k*size*1.28), ch, f, col)
vtext(-SW*0.5+52, 44, '―みんなの笑顔を', 15, TEXT)
vtext(-SW*0.5+30, 30, '守るために！', 15, TEXT)
vtext(-SW*0.5+8, -74, 'いっくよー！', 15, GOLD)

# TAP TO START
for dy in (26, -26):
    a, b = P(TitleX-170, TapY+dy), P(TitleX+170, TapY+dy)
    d.line((a[0], a[1], b[0], b[1]), fill=(255,255,255,128), width=SS)
shade(P(TitleX, TapY), 'T A P   T O   S T A R T', F(22*SS), TEXT)

# はじめから
x0, y0 = P(TitleX-100, TapY-56+15); x1, y1 = P(TitleX+100, TapY-56-15)
d.rounded_rectangle((x0,y0,x1,y1), 15*SS, fill=EDGE)
d.rounded_rectangle((x0+3*SS,y0+3*SS,x1-3*SS,y1-3*SS), 13*SS, fill=BTN)
shade(P(TitleX, TapY-56), 'はじめから', F(13*SS), TEXT)

# 丸ボタン 3 つ
f_p = F(13*SS)
px = -SW*0.5 + 24 + PillW*0.5
for icon, lbl in (('🔔','お知らせ'), ('⚙','設定'), ('🔗','引き継ぎ')):
    a, b = P(px-PillW*0.5, PillY+PillH*0.5), P(px+PillW*0.5, PillY-PillH*0.5)
    d.rounded_rectangle((a[0],a[1],b[0],b[1]), 20*SS, fill=EDGE)
    d.rounded_rectangle((a[0]+3*SS,a[1]+3*SS,b[0]-3*SS,b[1]-3*SS), 18*SS, fill=BTN)
    ic = P(px-PillW*0.5+26, 0)[0], P(0, PillY)[1]
    d.ellipse((ic[0]-9*SS, ic[1]-9*SS, ic[0]+9*SS, ic[1]+9*SS), fill=GOLD)
    shade(P(px+11, PillY), lbl, f_p, TEXT)
    px += PillW + 12

# 右上のメニュー
a, b = P(SW*0.5-34-22, SH*0.5-34+22), P(SW*0.5-34+22, SH*0.5-34-22)
d.rounded_rectangle((a[0],a[1],b[0],b[1]), 10*SS, fill=EDGE)
d.rounded_rectangle((a[0]+3*SS,a[1]+3*SS,b[0]-3*SS,b[1]-3*SS), 8*SS, fill=BTN)
shade(P(SW*0.5-34, SH*0.5-34), '≡', F(22*SS), GOLD)

# フッター
shade(P(-SW*0.5+10, -SH*0.5+18), 'Ver.1.0.0', F(11*SS), SUB, anchor='lm')
shade(P(SW*0.5-10, -SH*0.5+28), '© 2026 BIG BONUS BLITZ', F(10*SS), SUB, anchor='rm')
shade(P(SW*0.5-10, -SH*0.5+12), 'All Rights Reserved.', F(10*SS), SUB, anchor='rm')

for x in (-SW*0.5, SW*0.5):
    xx = P(x, 0)[0]
    d.line((xx, 0, xx, CH*SS), fill=(255,255,255,40), width=SS)

out_path = sys.argv[1] if len(sys.argv) > 1 else 'title_preview.png'
im.convert('RGB').resize((CW, CH), Image.LANCZOS).save(out_path)
print('タイトルの確認用の絵を作った', (CW, CH))
