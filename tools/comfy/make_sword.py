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

# 色。設定資料（assets/Chr0001/001）の聖剣グランレーヴに合わせる
LINE = (40, 46, 66, 255)          # 輪郭
STEEL_LIGHT = (233, 241, 250, 255)
STEEL_MID = (196, 212, 232, 255)
STEEL_DARK = (150, 172, 200, 255)
GOLD = (231, 196, 116, 255)
GOLD_DARK = (188, 148, 72, 255)
GOLD_LIGHT = (247, 226, 168, 255)
NAVY = (44, 52, 78, 255)
GEM = (74, 168, 226, 255)
GEM_LIGHT = (150, 214, 244, 255)

GRIP_Y = 0.800              # 握る位置（画像の上からの割合）


def draw(d, s):
    """s は拡大率。座標は 256x1024 のつもりで書く。"""
    k = W / 256.0                      # 座標は 256x1024 のつもりで書き、ここで実寸へ直す

    def P(*pts):
        return [(x * k * s, y * k * s) for x, y in pts]

    def LW(v):
        """線の太さも同じ比率で太くする。"""
        return max(1, int(v * k * s))

    cx = 128
    tip_y, guard_y = 40, 660
    half = 42                                  # 刃の根元の半幅。大剣なので広い

    # 刃。先端を斜めに切り落とした、幅の広い両刃
    blade = P((cx - 14, tip_y), (cx + 14, tip_y),
              (cx + half, tip_y + 96),
              (cx + half, guard_y),
              (cx - half, guard_y),
              (cx - half, tip_y + 96))
    d.polygon(blade, fill=STEEL_LIGHT)
    # 右半分を暗くして厚みを見せる
    d.polygon(P((cx, tip_y), (cx + 14, tip_y), (cx + half, tip_y + 96),
               (cx + half, guard_y), (cx, guard_y)), fill=STEEL_MID)
    d.polygon(P((cx + 24, tip_y + 70), (cx + half, tip_y + 96),
               (cx + half, guard_y), (cx + 24, guard_y)), fill=STEEL_DARK)
    d.polygon(blade, outline=LINE, width=LW(5))
    # 樋（中央の溝）
    d.line(P((cx, tip_y + 120), (cx, guard_y - 40)), fill=STEEL_LIGHT, width=LW(11))
    d.line(P((cx, tip_y + 130), (cx, guard_y - 50)), fill=LINE, width=LW(3))

    # 鍔。両端が上へ跳ね上がった翼形
    gy = guard_y
    d.polygon(P((cx - 104, gy - 34), (cx - 74, gy + 6), (cx - 30, gy - 2),
               (cx - 30, gy + 44), (cx - 78, gy + 44), (cx - 108, gy + 4)),
              fill=GOLD, outline=LINE, width=LW(5))
    d.polygon(P((cx + 104, gy - 34), (cx + 74, gy + 6), (cx + 30, gy - 2),
               (cx + 30, gy + 44), (cx + 78, gy + 44), (cx + 108, gy + 4)),
              fill=GOLD, outline=LINE, width=LW(5))
    d.polygon(P((cx - 34, gy - 6), (cx + 34, gy - 6), (cx + 34, gy + 46), (cx - 34, gy + 46)),
              fill=GOLD_DARK, outline=LINE, width=LW(5))
    # 鍔の中央の宝玉（菱形）
    d.polygon(P((cx, gy + 2), (cx + 20, gy + 20), (cx, gy + 38), (cx - 20, gy + 20)),
              fill=GEM, outline=LINE, width=LW(4))
    d.polygon(P((cx, gy + 8), (cx + 9, gy + 20), (cx, gy + 26), (cx - 9, gy + 20)),
              fill=GEM_LIGHT)

    # 柄。長めの両手持ち
    hy0, hy1 = gy + 46, gy + 46 + 200
    d.rounded_rectangle(P((cx - 20, hy0), (cx + 20, hy1)), radius=LW(8),
                        fill=NAVY, outline=LINE, width=LW(5))
    for i in range(3):                          # 柄の金具（i は拡大率 k と別物）
        y = hy0 + 34 + i * 56
        d.rectangle(P((cx - 21, y), (cx + 21, y + 14)), fill=GOLD_DARK,
                    outline=LINE, width=LW(3))

    # 柄頭。金の台に菱形の宝玉
    d.polygon(P((cx - 30, hy1 - 4), (cx + 30, hy1 - 4), (cx + 22, hy1 + 30),
               (cx - 22, hy1 + 30)), fill=GOLD, outline=LINE, width=LW(5))
    d.polygon(P((cx, hy1 + 22), (cx + 22, hy1 + 48), (cx, hy1 + 76), (cx - 22, hy1 + 48)),
              fill=GEM, outline=LINE, width=LW(4))
    d.polygon(P((cx, hy1 + 32), (cx + 10, hy1 + 48), (cx, hy1 + 60), (cx - 10, hy1 + 48)),
              fill=GEM_LIGHT)


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
