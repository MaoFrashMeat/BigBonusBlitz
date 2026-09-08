# 旅人 A〜E のピクセルアート歩行ストリップ（4フレーム）を生成する
# 使い方: python tools/gen_traveler_sprites.py
# 出力: UnityProject/BigBonusBlitz/Assets/Resources/Art/Travelers/traveler_{A..E}.png (4フレーム横並び, 各 48x64, 4倍拡大で 192x256)
from PIL import Image, ImageDraw
import os

OUT = os.path.join(os.path.dirname(__file__), '..', 'UnityProject', 'BigBonusBlitz', 'Assets', 'Resources', 'Art', 'Travelers')
W, H, FRAMES, SCALE = 48, 64, 4, 4

TRAVELERS = {
    # id: (体色, 濃い色, 肌, 帽子色, 荷物色, 特徴)
    'A': ((200, 160, 96), (120, 90, 50), (240, 215, 180), (110, 80, 45), (150, 110, 60), 'pack'),      # 行商人: 大きな背負い荷
    'B': ((143, 176, 216), (70, 95, 140), (240, 215, 180), (230, 230, 240), (120, 100, 80), 'staff'),  # 巡礼者: 杖と頭巾
    'C': ((122, 176, 96), (60, 100, 45), (225, 195, 160), (60, 80, 40), (90, 70, 40), 'bow'),          # 狩人: 弓
    'D': ((176, 128, 208), (90, 55, 130), (240, 215, 190), (60, 30, 90), (200, 180, 90), 'orb'),       # 占い師: とんがり帽と水晶
    'E': ((232, 192, 64), (160, 120, 30), (240, 215, 180), (200, 40, 40), (220, 220, 230), 'flag'),    # 王の使者: 赤い帽子と旗
}

def px(d, x, y, c, w=1, h=1):
    d.rectangle([x, y, x + w - 1, y + h - 1], fill=c)

def draw_frame(d, f, body, dark, skin, hat, item, kind):
    # 歩行: フレームごとに上下バウンドと脚の前後
    bob = [0, -1, 0, -1][f]
    legL = [0, 2, 0, -2][f]
    legR = [0, -2, 0, 2][f]
    cx = 22
    top = 14 + bob
    outline = (30, 25, 35)
    # 影
    px(d, cx - 9, 60, (0, 0, 0), 20, 2)
    # 脚
    px(d, cx - 5 + legL, 46 + bob, dark, 5, 12)
    px(d, cx + 1 + legR, 46 + bob, dark, 5, 12)
    px(d, cx - 6 + legL, 56 + bob, outline, 7, 2)   # 靴
    px(d, cx + 0 + legR, 56 + bob, outline, 7, 2)
    # 体
    px(d, cx - 8, top + 14, body, 17, 20)
    px(d, cx - 8, top + 14, dark, 17, 2)             # 襟
    px(d, cx - 9, top + 20, dark, 2, 12)             # 腕（左）
    # 頭
    px(d, cx - 6, top, skin, 13, 13)
    px(d, cx - 3, top + 6, outline, 2, 2)            # 目
    px(d, cx + 3, top + 6, outline, 2, 2)
    # 帽子
    if kind == 'orb':
        # とんがり帽
        for i in range(8):
            px(d, cx - 6 + i, top - 8 + i, hat, 13 - 2 * i if 13 - 2 * i > 0 else 1, 1)
        px(d, cx - 9, top - 1, hat, 19, 3)
    elif kind == 'staff':
        px(d, cx - 8, top - 3, hat, 17, 6)           # 頭巾
        px(d, cx - 8, top + 3, hat, 3, 10)
        px(d, cx + 6, top + 3, hat, 3, 10)
    elif kind == 'flag':
        px(d, cx - 7, top - 5, hat, 15, 6)           # 赤い帽子
        px(d, cx - 2, top - 8, hat, 5, 4)
    else:
        px(d, cx - 9, top - 2, hat, 19, 3)           # つば
        px(d, cx - 6, top - 6, hat, 13, 5)
    # 持ち物
    if kind == 'pack':
        px(d, cx + 8, top + 10, item, 10, 22)        # 背負い荷
        px(d, cx + 8, top + 10, dark, 10, 2)
        px(d, cx + 8, top + 20, dark, 10, 2)
    elif kind == 'staff':
        px(d, cx + 11, top - 6, item, 2, 46)         # 杖
        px(d, cx + 10, top - 8, (240, 220, 120), 4, 4)
    elif kind == 'bow':
        px(d, cx + 10, top + 6, item, 2, 30)         # 弓
        px(d, cx + 12, top + 8, (230, 230, 230), 1, 26)
        px(d, cx + 9, top + 4, item, 4, 2)
        px(d, cx + 9, top + 36, item, 4, 2)
    elif kind == 'orb':
        px(d, cx - 13, top + 22, (150, 220, 255), 6, 6)  # 水晶
        px(d, cx - 12, top + 23, (255, 255, 255), 2, 2)
    elif kind == 'flag':
        px(d, cx + 11, top - 10, item, 2, 44)        # 旗竿
        px(d, cx + 13, top - 10, (200, 40, 40), 10, 8)
        px(d, cx + 13, top - 10, (240, 200, 60), 10, 2)

def main():
    os.makedirs(OUT, exist_ok=True)
    for tid, (body, dark, skin, hat, item, kind) in TRAVELERS.items():
        strip = Image.new('RGBA', (W * FRAMES, H), (0, 0, 0, 0))
        for f in range(FRAMES):
            fr = Image.new('RGBA', (W, H), (0, 0, 0, 0))
            d = ImageDraw.Draw(fr)
            draw_frame(d, f, body, dark, skin, hat, item, kind)
            strip.paste(fr, (f * W, 0))
        big = strip.resize((W * FRAMES * SCALE, H * SCALE), Image.NEAREST)
        path = os.path.join(OUT, f'traveler_{tid}.png')
        big.save(path)
        print('wrote', path)

if __name__ == '__main__':
    main()
