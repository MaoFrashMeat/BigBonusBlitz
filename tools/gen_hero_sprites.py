# 主人公ポポラ（女騎士・ピンク髪・大剣）のピクセルアートを生成する。
# 使い方: python tools/gen_hero_sprites.py
# 出力: UnityProject/BigBonusBlitz/Assets/Resources/Art/Hero/hero_<action>.png
#       （横並びストリップ。1コマ 64x72 を 4 倍に拡大）
#
# 部品（髪・顔・胴・腕・脚・剣）を座標と角度で描くので、POSES に行を足せば
# 新しい動きをすぐ作れる。剣は角度指定の太線で描く。
from PIL import Image, ImageDraw
import math
import os

OUT = os.path.join(os.path.dirname(__file__), '..', 'UnityProject', 'BigBonusBlitz',
                   'Assets', 'Resources', 'Art', 'Hero')
W, H, SCALE = 96, 72, 4

# --- 配色（protagonist.md: ピンク髪 / 白基調の衣装 / 大剣）---
HAIR      = (255, 138, 190)
HAIR_DARK = (198,  86, 140)
SKIN      = (255, 224, 196)
SKIN_DARK = (222, 180, 150)
DRESS     = (250, 250, 252)
DRESS_SH  = (206, 210, 224)
TRIM      = (255, 190, 60)     # 金の縁取り
RIBBON    = (240,  70, 110)
BOOT      = (110,  92, 130)
BLADE     = (226, 236, 248)
BLADE_ED  = (150, 168, 200)
HILT      = (140, 100, 60)
EYE       = ( 60,  40,  70)
GLOW      = (255, 240, 170)

def rect(d, x, y, w, h, c):
    if w <= 0 or h <= 0:
        return
    d.rectangle([x, y, x + w - 1, y + h - 1], fill=c)

def line_thick(d, x0, y0, x1, y1, c, t=3):
    """太い直線（ピクセル寄せ）。剣身に使う。"""
    steps = int(max(abs(x1 - x0), abs(y1 - y0), 1))
    for i in range(steps + 1):
        x = x0 + (x1 - x0) * i / steps
        y = y0 + (y1 - y0) * i / steps
        rect(d, round(x - t // 2), round(y - t // 2), t, t, c)

def sword(d, px, py, angle_deg, length=30, flip=False):
    """(px,py) を柄尻として angle 方向へ伸びる大剣。angle 0=右, 90=上。"""
    a = math.radians(angle_deg)
    dx, dy = math.cos(a), -math.sin(a)
    # 柄
    line_thick(d, px, py, px + dx * 7, py + dy * 7, HILT, 3)
    # 鍔
    gx, gy = px + dx * 7, py + dy * 7
    line_thick(d, gx - dy * 4, gy - dx * 4, gx + dy * 4, gy + dx * 4, TRIM, 3)
    # 剣身（根元は太く、先は細く）
    line_thick(d, gx, gy, px + dx * length, py + dy * length, BLADE, 5)
    line_thick(d, px + dx * (length * 0.6), py + dy * (length * 0.6),
               px + dx * length, py + dy * length, BLADE, 4)
    # 稜線
    line_thick(d, gx + dx * 2, gy + dy * 2, px + dx * (length - 2), py + dy * (length - 2), BLADE_ED, 1)

def head(d, cx, cy, tilt=0, mouth='normal', eyes='open'):
    """顔と髪。tilt はドット単位の横ずらし。"""
    x = cx + tilt
    # 後ろ髪
    rect(d, x - 10, cy - 2, 20, 18, HAIR_DARK)
    # 顔
    rect(d, x - 7, cy, 14, 13, SKIN)
    rect(d, x - 7, cy + 11, 14, 2, SKIN_DARK)
    # 前髪
    rect(d, x - 9, cy - 4, 18, 6, HAIR)
    rect(d, x - 9, cy + 2, 4, 5, HAIR)
    rect(d, x + 5, cy + 2, 4, 5, HAIR)
    # サイドテール＋リボン
    rect(d, x + 8, cy + 1, 3, 12, HAIR)
    rect(d, x + 7, cy - 1, 5, 3, RIBBON)
    # 目
    if eyes == 'open':
        rect(d, x - 4, cy + 5, 2, 3, EYE)
        rect(d, x + 3, cy + 5, 2, 3, EYE)
    elif eyes == 'closed':
        rect(d, x - 5, cy + 6, 4, 1, EYE)
        rect(d, x + 2, cy + 6, 4, 1, EYE)
    else:  # 'wide' 驚き
        rect(d, x - 5, cy + 4, 3, 4, EYE)
        rect(d, x + 3, cy + 4, 3, 4, EYE)
    # 口
    if mouth == 'open':
        rect(d, x - 1, cy + 10, 3, 2, (170, 70, 90))
    elif mouth == 'smile':
        rect(d, x - 2, cy + 10, 4, 1, (190, 110, 120))

def body(d, cx, cy, lean=0, skirt=True):
    """胴（白のドレス風）。lean は上体の横ずらし。"""
    x = cx + lean
    rect(d, x - 8, cy, 16, 14, DRESS)          # 胴
    rect(d, x - 8, cy + 12, 16, 2, DRESS_SH)
    rect(d, x - 2, cy + 1, 4, 10, TRIM)        # 中央の飾り
    if skirt:
        rect(d, x - 11, cy + 13, 22, 7, DRESS)  # スカート
        rect(d, x - 11, cy + 18, 22, 2, DRESS_SH)
        rect(d, x - 11, cy + 13, 22, 1, TRIM)

def arms(d, cx, cy, la=(-10, 0), ra=(10, 0)):
    """腕。la/ra は肩からの相対位置（手先）。"""
    rect(d, cx + la[0], cy + la[1], 4, 8, SKIN)
    rect(d, cx + ra[0], cy + ra[1], 4, 8, SKIN)

def legs(d, cx, cy, l=0, r=0):
    rect(d, cx - 7 + l, cy, 5, 10, DRESS_SH)
    rect(d, cx + 2 + r, cy, 5, 10, DRESS_SH)
    rect(d, cx - 8 + l, cy + 9, 7, 4, BOOT)
    rect(d, cx + 1 + r, cy + 9, 7, 4, BOOT)

# --------------------------------------------------------------------------
# ポーズ定義。1 コマ = 1 つの関数。cx は中心 x、gy は地面の y。
# --------------------------------------------------------------------------
def pose_idle(d, f):
    cx, gy = 44, 58
    bob = [0, -1, 0, 1][f]
    breath = [0, 0, 1, 0][f]
    legs(d, cx, gy - 13)
    body(d, cx, gy - 33 + bob)
    arms(d, cx, gy - 30 + bob, (-11, 0), (9, 0))
    head(d, cx, gy - 48 + bob + breath, mouth='smile')
    sword(d, cx + 13, gy - 6, 78, 30)                 # 背に立てかけるように構える

def pose_walk(d, f):
    cx, gy = 44, 58
    bob = [0, -2, 0, -2, 0, -1][f]
    lf = [-3, 0, 3, 3, 0, -3][f]
    rf = [3, 0, -3, -3, 0, 3][f]
    legs(d, cx, gy - 13, lf, rf)
    body(d, cx, gy - 33 + bob, lean=[0, 1, 0, -1, 0, 1][f])
    arms(d, cx, gy - 30 + bob, (-11, rf // 2), (9, lf // 2))
    head(d, cx, gy - 48 + bob, tilt=[0, 1, 0, -1, 0, 0][f], mouth='smile')
    sword(d, cx + 13, gy - 6 + bob, 78, 30)

def pose_attack(d, f):
    """振りかぶり → 振り下ろし（4コマ）。"""
    cx, gy = 44, 58
    ang = [110, 135, 40, 5][f]         # 剣の角度
    lean = [-2, -3, 2, 3][f]
    bob = [0, -2, 1, 2][f]
    legs(d, cx, gy - 13, -2, 2)
    body(d, cx, gy - 33 + bob, lean=lean)
    arms(d, cx + lean, gy - 32 + bob, (-9, 2), (8, -2 if f < 2 else 4))
    head(d, cx, gy - 48 + bob, tilt=lean, mouth='open',
         eyes='open' if f < 2 else 'closed')
    sword(d, cx + 10 + lean, gy - 30 + bob, ang, 32)
    if f == 3:                                        # 斬撃の軌跡
        line_thick(d, cx + 6, gy - 46, cx + 34, gy - 16, GLOW, 2)

def pose_slash(d, f):
    """横薙ぎ（4コマ）。"""
    cx, gy = 44, 58
    ang = [170, 120, 20, -10][f]
    lean = [-3, -1, 3, 4][f]
    legs(d, cx, gy - 13, -3, 3)
    body(d, cx, gy - 33, lean=lean)
    arms(d, cx + lean, gy - 31, (-9, 1), (9, 1))
    head(d, cx, gy - 48, tilt=lean, mouth='open')
    sword(d, cx + 8 + lean, gy - 28, ang, 33)
    if f >= 2:
        line_thick(d, cx - 12, gy - 30, cx + 34, gy - 28, GLOW, 2)

def pose_cast(d, f):
    """大技: 剣を地面に突き立てて衝撃波（4コマ）。"""
    cx, gy = 44, 58
    up = [-8, -14, 0, 0][f]
    legs(d, cx, gy - 13 - max(0, -up) // 3)
    body(d, cx, gy - 33 + up // 2)
    arms(d, cx, gy - 34 + up // 2, (-10, -6 if f < 2 else 2), (8, -6 if f < 2 else 2))
    head(d, cx, gy - 48 + up // 2, mouth='open', eyes='closed' if f == 3 else 'open')
    ang = [95, 95, 60, 270][f]
    sword(d, cx + 4, gy - 30 + up // 2, ang, 32)
    if f == 3:                                        # 着弾の衝撃波
        for i in range(3):
            rect(d, cx - 22 - i * 4, gy - 4 + i, 12 - i * 2, 2, GLOW)
            rect(d, cx + 12 + i * 4, gy - 4 + i, 12 - i * 2, 2, GLOW)

def pose_guard(d, f):
    """剣を盾にして構える（2コマ）。"""
    cx, gy = 44, 58
    sh = [0, 1][f]
    legs(d, cx, gy - 13, 2, -2)
    body(d, cx, gy - 33 + sh, lean=-2)
    arms(d, cx - 2, gy - 32 + sh, (-6, 0), (4, 0))
    head(d, cx, gy - 48 + sh, tilt=-1, eyes='closed', mouth='normal')
    sword(d, cx - 6, gy - 12 + sh, 88, 34)            # 体の前に垂直に立てる
    rect(d, cx - 14, gy - 44 + sh, 3, 34, BLADE_ED)   # 盾の縁

def pose_hit(d, f):
    """被弾（2コマ）。"""
    cx, gy = 44, 58
    back = [3, 5][f]
    legs(d, cx + back // 2, gy - 13, 3, -1)
    body(d, cx + back, gy - 32, lean=2)
    arms(d, cx + back, gy - 30, (-12, -3), (10, -3))
    head(d, cx + back + 1, gy - 47, tilt=2, eyes='closed', mouth='open')
    sword(d, cx + back + 12, gy - 8, 60, 28)
    rect(d, cx - 16, gy - 40, 3, 3, RIBBON)           # 汗
    rect(d, cx - 13, gy - 44, 2, 2, RIBBON)

def pose_victory(d, f):
    """勝利: 剣を天に掲げる（4コマ）。"""
    cx, gy = 44, 58
    jump = [0, -4, -6, -3][f]
    legs(d, cx, gy - 13 + jump, -1, 1)
    body(d, cx, gy - 33 + jump)
    arms(d, cx, gy - 36 + jump, (-11, -4), (8, -6))
    head(d, cx, gy - 48 + jump, mouth='open', eyes='closed')
    sword(d, cx + 8, gy - 40 + jump, 90, 30)
    if f >= 1:                                        # きらめき
        rect(d, cx + 4, gy - 66, 2, 2, GLOW)
        rect(d, cx + 14, gy - 62, 2, 2, GLOW)
        rect(d, cx - 6, gy - 60, 2, 2, GLOW)

def pose_focus(d, f):
    """集中（択ナビ中）: 静かに構えて息を整える（4コマ）。"""
    cx, gy = 44, 58
    bob = [0, -1, -1, 0][f]
    legs(d, cx, gy - 13, 1, -1)
    body(d, cx, gy - 33 + bob, lean=-1)
    arms(d, cx - 1, gy - 31 + bob, (-9, 1), (7, 1))
    head(d, cx, gy - 48 + bob, eyes='closed', mouth='normal')
    sword(d, cx + 2, gy - 14 + bob, 85, 32)
    if f in (1, 2):                                   # 集中線
        rect(d, cx - 20, gy - 40, 2, 6, (150, 200, 255))
        rect(d, cx + 18, gy - 44, 2, 6, (150, 200, 255))

POSES = {
    'idle':    (pose_idle, 4),
    'walk':    (pose_walk, 6),
    'attack':  (pose_attack, 4),
    'slash':   (pose_slash, 4),
    'cast':    (pose_cast, 4),
    'guard':   (pose_guard, 2),
    'hit':     (pose_hit, 2),
    'victory': (pose_victory, 4),
    'focus':   (pose_focus, 4),
}

def main():
    os.makedirs(OUT, exist_ok=True)
    for name, (fn, frames) in POSES.items():
        strip = Image.new('RGBA', (W * frames, H), (0, 0, 0, 0))
        for f in range(frames):
            fr = Image.new('RGBA', (W, H), (0, 0, 0, 0))
            d = ImageDraw.Draw(fr)
            rect(d, 28, 68, 32, 3, (0, 0, 0, 90))     # 影
            fn(d, f)
            strip.paste(fr, (f * W, 0))
        big = strip.resize((W * frames * SCALE, H * SCALE), Image.NEAREST)
        path = os.path.join(OUT, f'hero_{name}.png')
        big.save(path)
        print(f'wrote {path}  ({frames} frames)')

if __name__ == '__main__':
    main()
