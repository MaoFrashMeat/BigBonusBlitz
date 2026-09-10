# 主人公ポポラ（女騎士・ピンク髪・大剣）のピクセルアートを生成する。
# 使い方: python tools/gen_hero_sprites.py
# 出力: UnityProject/BigBonusBlitz/Assets/Resources/Art/Hero/hero_<action>.png
#       （横並びストリップ。1コマ 192x192 を 2 倍に拡大して 384x384）
#
# 解像度について:
#   画面での表示は 206px 四方。1コマ 192x192 なら 1 ドットがほぼ 1px になり、
#   これ以上ドットを増やしても表示側で潰れる。ここが効く上限。
#
# 部品（髪・顔・胴・腕・脚・剣・マント）を座標と角度で描くので、
# POSES に行を足せば新しい動きをすぐ作れる。剣は角度指定の太線で描く。
from PIL import Image, ImageDraw
import math
import os

OUT = os.path.join(os.path.dirname(__file__), '..', 'UnityProject', 'BigBonusBlitz',
                   'Assets', 'Resources', 'Art', 'Hero')
W, H, SCALE = 192, 192, 2

# --- 配色（protagonist.md: ピンク髪 / 白基調の衣装 / 大剣）---
HAIR_HI   = (255, 190, 220)
HAIR      = (255, 138, 190)
HAIR_MID  = (228, 108, 162)
HAIR_DARK = (176,  70, 122)
SKIN_HI   = (255, 240, 220)
SKIN      = (255, 222, 194)
SKIN_SH   = (226, 182, 152)
DRESS_HI  = (255, 255, 255)
DRESS     = (244, 246, 250)
DRESS_SH  = (206, 212, 228)
DRESS_DK  = (166, 174, 196)
TRIM      = (255, 196,  70)
TRIM_DK   = (198, 142,  30)
RIBBON    = (238,  72, 112)
RIBBON_DK = (186,  40,  76)
CAPE      = (232,  86, 126)
CAPE_DK   = (170,  48,  86)
BOOT      = (120, 100, 140)
BOOT_DK   = ( 78,  62,  98)
BLADE_HI  = (250, 253, 255)
BLADE     = (216, 228, 244)
BLADE_SH  = (158, 176, 208)
BLADE_ED  = (108, 128, 166)
HILT      = (146, 104,  62)
HILT_DK   = ( 96,  66,  38)
GEM       = ( 92, 214, 255)
EYE       = ( 62,  40,  74)
EYE_HI    = (255, 255, 255)
IRIS      = ( 86, 156, 226)
BLUSH     = (255, 168, 186)
LINE      = ( 52,  38,  62)          # 輪郭
GLOW      = (255, 244, 186)

def rect(d, x, y, w, h, c):
    if w <= 0 or h <= 0:
        return
    d.rectangle([round(x), round(y), round(x + w - 1), round(y + h - 1)], fill=c)

def line_thick(d, x0, y0, x1, y1, c, t=3):
    """太い直線（ピクセル寄せ）。剣身に使う。"""
    steps = int(max(abs(x1 - x0), abs(y1 - y0), 1))
    for i in range(steps + 1):
        x = x0 + (x1 - x0) * i / steps
        y = y0 + (y1 - y0) * i / steps
        rect(d, x - t // 2, y - t // 2, t, t, c)

# --------------------------------------------------------------------------
# 部品
# --------------------------------------------------------------------------
def sword(d, px, py, angle_deg, length=62):
    """(px,py) を柄尻として angle 方向へ伸びる大剣。angle 0=右, 90=上。"""
    a = math.radians(angle_deg)
    dx, dy = math.cos(a), -math.sin(a)
    nx, ny = -dy, -dx                      # 刃の横方向

    # 柄（握り）と柄頭
    line_thick(d, px, py, px + dx * 14, py + dy * 14, HILT_DK, 7)
    line_thick(d, px + dx * 2, py + dy * 2, px + dx * 12, py + dy * 12, HILT, 5)
    rect(d, px - 4, py - 4, 8, 8, TRIM_DK)
    rect(d, px - 3, py - 3, 5, 5, TRIM)

    # 鍔（横に張り出す）と中央の宝石
    gx, gy = px + dx * 14, py + dy * 14
    line_thick(d, gx + nx * 11, gy + ny * 11, gx - nx * 11, gy - ny * 11, TRIM_DK, 7)
    line_thick(d, gx + nx * 10, gy + ny * 10, gx - nx * 10, gy - ny * 10, TRIM, 5)
    rect(d, gx - 3, gy - 3, 6, 6, GEM)
    rect(d, gx - 2, gy - 2, 2, 2, BLADE_HI)

    # 刃: 外側の線 → 本体 → 稜線 → ハイライト。先に向かって細くする
    tipx, tipy = px + dx * length, py + dy * length
    line_thick(d, gx, gy, tipx, tipy, BLADE_ED, 13)
    line_thick(d, gx, gy, tipx, tipy, BLADE, 11)
    line_thick(d, gx + dx * (length * 0.55), gy + dy * (length * 0.55), tipx, tipy, BLADE, 8)
    line_thick(d, gx + dx * (length * 0.8), gy + dy * (length * 0.8), tipx, tipy, BLADE, 5)
    # 稜線（中央の溝）とハイライト（片側）
    line_thick(d, gx + dx * 4, gy + dy * 4, px + dx * (length - 4), py + dy * (length - 4), BLADE_SH, 3)
    line_thick(d, gx + dx * 6 + nx * 3, gy + dy * 6 + ny * 3,
               px + dx * (length - 8) + nx * 2, py + dy * (length - 8) + ny * 2, BLADE_HI, 2)

def head(d, cx, cy, tilt=0, mouth='normal', eyes='open'):
    """顔と髪。cy は頭頂。tilt は横ずらし。"""
    x = cx + tilt
    # 後ろ髪（輪郭）
    rect(d, x - 26, cy + 2, 52, 44, LINE)
    rect(d, x - 25, cy + 3, 50, 42, HAIR_DARK)
    rect(d, x - 22, cy + 6, 44, 34, HAIR_MID)
    # 顔
    rect(d, x - 18, cy + 8, 36, 34, LINE)
    rect(d, x - 17, cy + 9, 34, 32, SKIN)
    rect(d, x - 17, cy + 9, 34, 6, SKIN_HI)
    rect(d, x - 17, cy + 35, 34, 6, SKIN_SH)
    # 前髪（毛束を 3 つ）
    rect(d, x - 24, cy + 2, 48, 14, HAIR)
    rect(d, x - 24, cy + 2, 48, 4, HAIR_HI)
    for bx, bw, bh in ((-22, 9, 16), (-6, 11, 12), (11, 10, 15)):
        rect(d, x + bx, cy + 14, bw, bh, HAIR)
        rect(d, x + bx, cy + 14, bw, 3, HAIR_HI)
        rect(d, x + bx, cy + 14 + bh - 3, bw, 3, HAIR_MID)
    # サイドの髪とリボン
    rect(d, x + 20, cy + 8, 8, 34, HAIR)
    rect(d, x + 20, cy + 8, 8, 5, HAIR_HI)
    rect(d, x - 28, cy + 8, 7, 30, HAIR_MID)
    rect(d, x + 17, cy + 1, 14, 9, RIBBON_DK)
    rect(d, x + 18, cy + 2, 12, 6, RIBBON)
    rect(d, x + 20, cy + 3, 4, 2, (255, 160, 190))
    # 目
    if eyes == 'open':
        for ex in (-11, 5):
            rect(d, x + ex, cy + 18, 8, 10, LINE)          # まつ毛・輪郭
            rect(d, x + ex + 1, cy + 20, 6, 7, IRIS)
            rect(d, x + ex + 1, cy + 25, 6, 2, EYE)
            rect(d, x + ex + 2, cy + 21, 3, 3, EYE_HI)     # ハイライト
    elif eyes == 'closed':
        for ex in (-11, 5):
            rect(d, x + ex, cy + 23, 8, 2, LINE)
            rect(d, x + ex + 1, cy + 21, 6, 2, LINE)
    else:                                                   # wide（驚き）
        for ex in (-12, 5):
            rect(d, x + ex, cy + 16, 9, 13, LINE)
            rect(d, x + ex + 1, cy + 18, 7, 9, IRIS)
            rect(d, x + ex + 2, cy + 19, 3, 3, EYE_HI)
    # 頬と口
    rect(d, x - 16, cy + 29, 5, 3, BLUSH)
    rect(d, x + 12, cy + 29, 5, 3, BLUSH)
    if mouth == 'open':
        rect(d, x - 4, cy + 33, 8, 6, LINE)
        rect(d, x - 3, cy + 35, 6, 3, (196, 88, 108))
    elif mouth == 'smile':
        rect(d, x - 5, cy + 34, 10, 2, LINE)
        rect(d, x - 6, cy + 33, 2, 2, LINE)
        rect(d, x + 4, cy + 33, 2, 2, LINE)
    else:
        rect(d, x - 3, cy + 34, 6, 2, LINE)

def cape(d, cx, cy, sway=0):
    """背中のマント。sway で裾が揺れる。"""
    rect(d, cx - 4 + sway, cy, 30, 46, CAPE_DK)
    rect(d, cx - 2 + sway, cy + 2, 26, 40, CAPE)
    rect(d, cx + 2 + sway * 2, cy + 40, 22, 10, CAPE_DK)

def body(d, cx, cy, lean=0):
    """胴（白のドレス＋金の縁）。cy は肩の高さ。"""
    x = cx + lean
    # 胴
    rect(d, x - 20, cy, 40, 34, LINE)
    rect(d, x - 19, cy + 1, 38, 32, DRESS)
    rect(d, x - 19, cy + 1, 38, 6, DRESS_HI)
    rect(d, x + 12, cy + 1, 7, 32, DRESS_SH)
    # 胸当ての縁と中央の飾り
    rect(d, x - 19, cy + 1, 38, 4, TRIM)
    rect(d, x - 5, cy + 4, 10, 24, TRIM_DK)
    rect(d, x - 4, cy + 5, 8, 22, TRIM)
    rect(d, x - 2, cy + 8, 3, 12, (255, 230, 150))
    # ベルト
    rect(d, x - 20, cy + 30, 40, 7, TRIM_DK)
    rect(d, x - 20, cy + 31, 40, 4, TRIM)
    rect(d, x - 4, cy + 30, 9, 7, TRIM_DK)
    # スカート（台形）
    for i in range(5):
        w = 44 + i * 4
        rect(d, x - w // 2, cy + 37 + i * 4, w, 5, DRESS if i % 2 == 0 else DRESS_SH)
    rect(d, x - 32, cy + 53, 64, 4, DRESS_DK)
    # フリルの山
    for fx in range(-30, 31, 10):
        rect(d, x + fx, cy + 55, 8, 4, DRESS_HI)

def arms(d, cx, cy, la=(-26, 0), ra=(20, 0), glove=True):
    """腕（肩から手まで）。la/ra は肩からの相対位置。"""
    for ax, ay in (la, ra):
        rect(d, cx + ax, cy + ay, 10, 22, LINE)
        rect(d, cx + ax + 1, cy + ay + 1, 8, 20, SKIN)
        rect(d, cx + ax + 1, cy + ay + 1, 8, 4, SKIN_HI)
        if glove:                                      # 手（白いグローブ）
            rect(d, cx + ax - 1, cy + ay + 18, 12, 11, LINE)
            rect(d, cx + ax, cy + ay + 19, 10, 9, DRESS)
            rect(d, cx + ax, cy + ay + 19, 10, 3, TRIM)

def legs(d, cx, cy, l=0, r=0):
    """脚とブーツ。cy はスカートの下端。"""
    for dx, off in ((-16, l), (4, r)):
        rect(d, cx + dx + off, cy, 12, 22, LINE)
        rect(d, cx + dx + off + 1, cy + 1, 10, 20, DRESS_SH)
        rect(d, cx + dx + off - 2, cy + 18, 16, 14, LINE)       # ブーツ
        rect(d, cx + dx + off - 1, cy + 19, 14, 12, BOOT)
        rect(d, cx + dx + off - 1, cy + 19, 14, 4, TRIM_DK)     # 折り返し
        rect(d, cx + dx + off - 1, cy + 28, 14, 3, BOOT_DK)

# --------------------------------------------------------------------------
# ポーズ。1 コマ = 1 回の呼び出し。CX=中心 x、GY=地面 y。
# --------------------------------------------------------------------------
CX, GY = 92, 172

def pose_idle(d, f):
    bob = [0, -2, 0, 2][f]
    br = [0, 0, 2, 0][f]
    cape(d, CX + 12, GY - 92 + bob, sway=[0, 1, 0, -1][f])
    legs(d, CX, GY - 32)
    body(d, CX, GY - 96 + bob)
    arms(d, CX, GY - 92 + bob, (-28, 2), (18, 2))
    head(d, CX, GY - 142 + bob + br, mouth='smile')
    sword(d, CX + 30, GY - 14, 76, 62)

def pose_walk(d, f):
    bob = [0, -3, -1, -3, 0, -2][f]
    lf = [-6, 0, 6, 6, 0, -6][f]
    rf = [6, 0, -6, -6, 0, 6][f]
    lean = [0, 2, 0, -2, 0, 2][f]
    cape(d, CX + 12, GY - 92 + bob, sway=[2, 3, 1, -1, 0, 2][f])
    legs(d, CX, GY - 32, lf, rf)
    body(d, CX, GY - 96 + bob, lean=lean)
    arms(d, CX + lean, GY - 92 + bob, (-28, rf // 2), (18, lf // 2))
    head(d, CX, GY - 142 + bob, tilt=lean, mouth='smile')
    sword(d, CX + 30, GY - 14 + bob, 76, 62)

def pose_attack(d, f):
    """振りかぶり → 振り下ろし。"""
    ang = [112, 138, 42, 6][f]
    lean = [-4, -6, 4, 6][f]
    bob = [0, -4, 2, 4][f]
    cape(d, CX + 12, GY - 92 + bob, sway=[-2, -3, 2, 3][f])
    legs(d, CX, GY - 32, -4, 4)
    body(d, CX, GY - 96 + bob, lean=lean)
    arms(d, CX + lean, GY - 96 + bob, (-24, 6), (16, -6 if f < 2 else 10))
    head(d, CX, GY - 142 + bob, tilt=lean, mouth='open', eyes='open' if f < 2 else 'closed')
    sword(d, CX + 18 + lean, GY - 92 + bob, ang, 58)
    if f == 3:
        line_thick(d, CX + 10, GY - 124, CX + 62, GY - 46, GLOW, 4)
        line_thick(d, CX + 16, GY - 122, CX + 66, GY - 50, (255, 255, 255), 2)

def pose_slash(d, f):
    """横薙ぎ。"""
    ang = [172, 124, 22, -8][f]
    lean = [-6, -2, 6, 8][f]
    cape(d, CX + 12, GY - 92, sway=[-3, -1, 3, 4][f])
    legs(d, CX, GY - 32, -6, 6)
    body(d, CX, GY - 96, lean=lean)
    arms(d, CX + lean, GY - 94, (-24, 4), (18, 4))
    head(d, CX, GY - 142, tilt=lean, mouth='open')
    sword(d, CX + 18 + lean, GY - 88, ang, 66)
    if f >= 2:
        line_thick(d, CX - 40, GY - 92, CX + 78, GY - 86, GLOW, 4)
        line_thick(d, CX - 36, GY - 88, CX + 74, GY - 84, (255, 255, 255), 2)

def pose_cast(d, f):
    """大技: 剣を地面に突き立てて衝撃波。"""
    up = [-14, -26, 0, 0][f]
    cape(d, CX + 12, GY - 92 + up // 2, sway=[-2, -4, 2, 0][f])
    legs(d, CX, GY - 32 - max(0, -up) // 4)
    body(d, CX, GY - 96 + up // 2)
    arms(d, CX, GY - 100 + up // 2, (-26, -12 if f < 2 else 6), (18, -12 if f < 2 else 6))
    head(d, CX, GY - 142 + up // 2, mouth='open', eyes='closed' if f == 3 else 'wide')
    ang = [96, 96, 62, 272][f]
    sword(d, CX + 8, GY - 84 + up // 2, ang, 58)
    if f == 3:
        for i in range(4):
            rect(d, CX - 48 - i * 10, GY - 10 + i * 2, 26 - i * 4, 4, GLOW)
            rect(d, CX + 26 + i * 10, GY - 10 + i * 2, 26 - i * 4, 4, GLOW)
        rect(d, CX - 20, GY - 8, 42, 4, (255, 255, 255))

def pose_guard(d, f):
    """剣を盾にして構える。"""
    sh = [0, 2][f]
    cape(d, CX + 14, GY - 92 + sh, sway=-2)
    legs(d, CX, GY - 32, 4, -4)
    body(d, CX, GY - 96 + sh, lean=-4)
    arms(d, CX - 6, GY - 94 + sh, (-16, 2), (6, 2))
    head(d, CX, GY - 142 + sh, tilt=-2, eyes='closed')
    sword(d, CX - 18, GY - 26 + sh, 88, 70)

def pose_hit(d, f):
    """被弾。"""
    back = [6, 11][f]
    cape(d, CX + 14 + back, GY - 90, sway=4)
    legs(d, CX + back // 2, GY - 32, 6, -2)
    body(d, CX + back, GY - 94, lean=4)
    arms(d, CX + back, GY - 90, (-30, -8), (20, -8))
    head(d, CX + back + 2, GY - 140, tilt=4, eyes='closed', mouth='open')
    sword(d, CX + back + 28, GY - 18, 58, 58)
    for sx, sy, ss in ((-40, -108, 7), (-32, -122, 5), (-48, -92, 4)):
        rect(d, CX + sx, GY + sy, ss, ss, (150, 214, 255))

def pose_victory(d, f):
    """勝利: 剣を天に掲げる。"""
    jump = [0, -6, -10, -5][f]
    cape(d, CX + 12, GY - 92 + jump, sway=[0, -2, -3, -1][f])
    legs(d, CX, GY - 32 + jump, -2, 2)
    body(d, CX, GY - 96 + jump)
    arms(d, CX, GY - 104 + jump, (-28, -10), (16, -14))
    head(d, CX, GY - 142 + jump, mouth='open', eyes='closed')
    sword(d, CX + 18, GY - 96 + jump, 90, 58)
    if f >= 1:
        for gx, gy_, gs in ((6, -166, 5), (30, -158, 4), (-16, -152, 4), (44, -142, 3)):
            rect(d, CX + gx, GY + gy_, gs, gs, GLOW)

def pose_focus(d, f):
    """集中（択ナビ中）: 静かに構えて息を整える。"""
    bob = [0, -2, -2, 0][f]
    cape(d, CX + 12, GY - 92 + bob, sway=-1)
    legs(d, CX, GY - 32, 2, -2)
    body(d, CX, GY - 96 + bob, lean=-2)
    arms(d, CX - 2, GY - 94 + bob, (-24, 4), (16, 4))
    head(d, CX, GY - 142 + bob, eyes='closed')
    sword(d, CX + 8, GY - 30 + bob, 85, 64)
    if f in (1, 2):
        for lx, ly in ((-52, -100), (48, -110), (-46, -70), (44, -76)):
            rect(d, CX + lx, GY + ly, 4, 14, (150, 200, 255))

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
            # 影（段差で丸みを出す）
            rect(d, CX - 30, GY + 4, 60, 4, (0, 0, 0, 90))
            rect(d, CX - 24, GY + 2, 48, 2, (0, 0, 0, 70))
            rect(d, CX - 24, GY + 8, 48, 2, (0, 0, 0, 70))
            fn(d, f)
            strip.paste(fr, (f * W, 0))
        big = strip.resize((W * frames * SCALE, H * SCALE), Image.NEAREST)
        path = os.path.join(OUT, f'hero_{name}.png')
        big.save(path)
        print(f'wrote {path}  ({frames} frames, {W}x{H} dots)')

if __name__ == '__main__':
    main()
