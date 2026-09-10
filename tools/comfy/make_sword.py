"""剣を 1 枚、手続きで描く。

    py -3 tools/comfy/make_sword.py

生成モデルに剣を任せると、向き・本数・見切れが毎回変わる。
剣は形が単純なので、こちらで描いたほうが確実で、全コマ同じ剣になる。

出力: tools/comfy/sword.png
  刃を真上に向けた縦向き。柄尻が下。
  握る位置は画像の高さに対する割合で GRIP_Y に書いてある（postprocess.py が使う）。
"""
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, 'sword.png')

W, H = 512, 2048        # キャラの解像度が上がったので剣も倍で描く
SS = 4                      # 4 倍で描いてから縮める（輪郭を滑らかにするため）

# 色。キャラの意匠（白・紺・金・水色）に合わせる
LINE = (38, 44, 68, 255)          # 輪郭
STEEL_LIGHT = (238, 246, 255, 255)
STEEL_DARK = (176, 196, 226, 255)
GOLD = (226, 192, 116, 255)
GOLD_DARK = (186, 148, 76, 255)
NAVY = (46, 62, 116, 255)
CYAN = (110, 206, 236, 255)

GRIP_Y = 0.815              # 握る位置（画像の上からの割合）


def draw(d, s):
    """s は拡大率。座標は 256x1024 のつもりで書く。"""
    k = W / 256.0                      # 座標は 256x1024 のつもりで書き、ここで実寸へ直す

    def P(*pts):
        return [(x * k * s, y * k * s) for x, y in pts]

    def LW(v):
        """線の太さも同じ比率で太くする。"""
        return max(1, int(v * k * s))

    cx = 128
    tip_y, guard_y = 48, 690
    half = 30                                  # 刃の根元の半幅

    # 刃。先を尖らせた六角形
    blade = P((cx, tip_y),
              (cx + half, tip_y + 120),
              (cx + half, guard_y),
              (cx - half, guard_y),
              (cx - half, tip_y + 120))
    d.polygon(blade, fill=STEEL_LIGHT, outline=LINE, width=LW(5))
    # 刃の右半分を暗くして立体に見せる
    d.polygon(P((cx, tip_y), (cx + half, tip_y + 120), (cx + half, guard_y), (cx, guard_y)),
              fill=STEEL_DARK)
    d.polygon(blade, outline=LINE, width=LW(5))
    # 樋（中央の溝）
    d.line(P((cx, tip_y + 150), (cx, guard_y - 30)), fill=STEEL_LIGHT, width=LW(9))

    # 鍔。中央が厚く、両端が跳ね上がった形
    gy0, gy1 = guard_y, guard_y + 46
    d.polygon(P((cx - 96, gy0 + 26), (cx - 60, gy0), (cx + 60, gy0), (cx + 96, gy0 + 26),
               (cx + 60, gy1), (cx - 60, gy1)),
              fill=GOLD, outline=LINE, width=LW(5))
    d.polygon(P((cx - 96, gy0 + 26), (cx - 60, gy1), (cx + 60, gy1), (cx + 96, gy0 + 26)),
              fill=GOLD_DARK)
    d.polygon(P((cx - 96, gy0 + 26), (cx - 60, gy0), (cx + 60, gy0), (cx + 96, gy0 + 26),
               (cx + 60, gy1), (cx - 60, gy1)),
              outline=LINE, width=LW(5))
    # 鍔の中央の宝玉
    d.ellipse(P((cx - 17, gy0 + 6), (cx + 17, gy0 + 40)), fill=CYAN, outline=LINE, width=LW(4))

    # 柄
    hy0, hy1 = gy1, gy1 + 186
    d.rounded_rectangle(P((cx - 22, hy0), (cx + 22, hy1)), radius=LW(10),
                        fill=NAVY, outline=LINE, width=LW(5))
    for i in range(4):                          # 柄の巻き（i は拡大率 k と別物）
        y = hy0 + 26 + i * 40
        d.line(P((cx - 22, y), (cx + 22, y - 14)), fill=GOLD_DARK, width=LW(5))

    # 柄頭
    d.ellipse(P((cx - 32, hy1 - 8), (cx + 32, hy1 + 56)), fill=GOLD, outline=LINE, width=LW(5))
    d.ellipse(P((cx - 13, hy1 + 11), (cx + 13, hy1 + 37)), fill=CYAN, outline=LINE, width=LW(4))


def main():
    big = Image.new('RGBA', (W * SS, H * SS), (0, 0, 0, 0))
    draw(ImageDraw.Draw(big), SS)
    img = big.resize((W, H), Image.LANCZOS)
    img.save(OUT)
    bb = img.getbbox()
    # 握る位置を postprocess.py に渡す
    import json
    with open(os.path.join(HERE, 'sword.json'), 'w', encoding='utf-8') as f:
        json.dump({'gripY': GRIP_Y, 'top': bb[1], 'bottom': bb[3],
                   'width': W, 'height': H}, f, ensure_ascii=False, indent=2)
    print('できました:', OUT)
    print('  剣の範囲 y %d〜%d / 握る位置 y=%d（%.3f）' %
          (bb[1], bb[3], int(H * GRIP_Y), GRIP_Y))


if __name__ == '__main__':
    main()
