# 主人公ポポラの「ちび」版（2.5 頭身）ピクセルアートを生成する。
# 使い方: python tools/gen_hero_chibi.py
# 出力: UnityProject/BigBonusBlitz/Assets/Resources/Art/HeroChibi/hero_<action>.png
#
# 等身版（gen_hero_sprites.py）との違い:
#   - 頭が全身の 4 割。目は顔の 1/3 を占める大きさで、ハイライトを 2 つ入れる
#   - 輪郭は角を 1 ドットずつ削って丸くする（矩形の積み上げでも柔らかく見せる）
#   - 手足は短く、指は描かない（丸めたミトン）
#   - 髪は顔より大きく張り出させて「ふわっと」を作る
from PIL import Image, ImageDraw
import math
import os

OUT = os.path.join(os.path.dirname(__file__), '..', 'UnityProject', 'BigBonusBlitz',
                   'Assets', 'Resources', 'Art', 'HeroChibi')
W, H, SCALE = 192, 192, 2

HAIR_HI   = (255, 202, 228)
HAIR      = (255, 146, 196)
HAIR_MID  = (232, 112, 168)
HAIR_DARK = (182,  74, 128)
SKIN_HI   = (255, 248, 236)
SKIN      = (255, 228, 206)
SKIN_SH   = (238, 196, 172)
DRESS_HI  = (255, 255, 255)
DRESS     = (248, 250, 254)
DRESS_SH  = (214, 220, 236)
DRESS_DK  = (176, 184, 208)
TRIM      = (255, 206,  88)
TRIM_DK   = (206, 150,  40)
RIBBON    = (255,  98, 138)
RIBBON_DK = (198,  52,  92)
CAPE      = (255, 118, 156)
CAPE_DK   = (196,  66, 108)
BOOT      = (140, 118, 168)
BOOT_DK   = ( 94,  76, 122)
BLADE_HI  = (252, 254, 255)
BLADE     = (222, 234, 250)
BLADE_SH  = (168, 186, 218)
BLADE_ED  = (118, 138, 176)
HILT      = (158, 116,  70)
HILT_DK   = (104,  74,  44)
GEM       = (110, 226, 255)
EYE_DK    = ( 58,  36,  76)          # 瞳の上側
EYE_LT    = (108, 186, 244)          # 瞳の下側（明るい）
EYE_HI    = (255, 255, 255)
LASH      = ( 66,  40,  80)          # まつ毛
BLUSH     = (255, 154, 178)
LINE      = ( 74,  52,  86)          # 輪郭（真っ黒にしない＝柔らかく見える）
GLOW      = (255, 246, 198)

def rect(d, x, y, w, h, c):
    if w <= 0 or h <= 0:
        return
    d.rectangle([round(x), round(y), round(x + w - 1), round(y + h - 1)], fill=c)

def round_box(d, x, y, w, h, fill, edge=None, r=3):
    """角を r ドット落とした四角。丸みを出すための土台。"""
    if edge:
        rect(d, x, y, w, h, edge)
        for i in range(r):
            cut = r - i
            rect(d, x, y + i, cut, 1, (0, 0, 0, 0))
            rect(d, x + w - cut, y + i, cut, 1, (0, 0, 0, 0))
            rect(d, x, y + h - 1 - i, cut, 1, (0, 0, 0, 0))
            rect(d, x + w - cut, y + h - 1 - i, cut, 1, (0, 0, 0, 0))
        x, y, w, h = x + 1, y + 1, w - 2, h - 2
        r = max(1, r - 1)
    rect(d, x, y, w, h, fill)
    for i in range(r):
        cut = r - i
        rect(d, x, y + i, cut, 1, (0, 0, 0, 0))
        rect(d, x + w - cut, y + i, cut, 1, (0, 0, 0, 0))
        rect(d, x, y + h - 1 - i, cut, 1, (0, 0, 0, 0))
        rect(d, x + w - cut, y + h - 1 - i, cut, 1, (0, 0, 0, 0))

def line_thick(d, x0, y0, x1, y1, c, t=3):
    steps = int(max(abs(x1 - x0), abs(y1 - y0), 1))
    for i in range(steps + 1):
        x = x0 + (x1 - x0) * i / steps
        y = y0 + (y1 - y0) * i / steps
        rect(d, x - t // 2, y - t // 2, t, t, c)

# --------------------------------------------------------------------------
# 部品（ちび用）
# --------------------------------------------------------------------------
def sword(d, px, py, angle_deg, length=52):
    """ちびに合わせて短く太い大剣。"""
    a = math.radians(angle_deg)
    dx, dy = math.cos(a), -math.sin(a)
    nx, ny = -dy, -dx
    line_thick(d, px, py, px + dx * 11, py + dy * 11, HILT_DK, 7)
    line_thick(d, px + dx * 2, py + dy * 2, px + dx * 9, py + dy * 9, HILT, 5)
    rect(d, px - 4, py - 4, 8, 8, TRIM_DK)
    rect(d, px - 3, py - 3, 5, 5, TRIM)
    gx, gy = px + dx * 11, py + dy * 11
    line_thick(d, gx + nx * 10, gy + ny * 10, gx - nx * 10, gy - ny * 10, TRIM_DK, 7)
    line_thick(d, gx + nx * 9, gy + ny * 9, gx - nx * 9, gy - ny * 9, TRIM, 5)
    rect(d, gx - 3, gy - 3, 6, 6, GEM)
    rect(d, gx - 3, gy - 3, 3, 3, BLADE_HI)
    tipx, tipy = px + dx * length, py + dy * length
    line_thick(d, gx, gy, tipx, tipy, BLADE_ED, 15)
    line_thick(d, gx, gy, tipx, tipy, BLADE, 13)
    line_thick(d, gx + dx * (length * 0.6), gy + dy * (length * 0.6), tipx, tipy, BLADE, 9)
    line_thick(d, gx + dx * (length * 0.85), gy + dy * (length * 0.85), tipx, tipy, BLADE, 5)
    line_thick(d, gx + dx * 4, gy + dy * 4, px + dx * (length - 5), py + dy * (length - 5), BLADE_SH, 3)
    line_thick(d, gx + dx * 6 + nx * 4, gy + dy * 6 + ny * 4,
               px + dx * (length - 9) + nx * 3, py + dy * (length - 9) + ny * 3, BLADE_HI, 3)

def head(d, cx, cy, tilt=0, mouth='smile', eyes='open'):
    """
    大きな頭。cy は頭頂。顔は幅 54・高さ 52 で、目が縦 20 ドット（顔の 4 割）。
    """
    x = cx + tilt
    # 後ろ髪（顔より一回り大きく、ふわっと）
    round_box(d, x - 36, cy + 4, 72, 58, HAIR_DARK, LINE, r=8)
    round_box(d, x - 33, cy + 8, 66, 50, HAIR_MID, None, r=7)
    # 顔
    round_box(d, x - 27, cy + 12, 54, 52, SKIN, LINE, r=8)
    round_box(d, x - 25, cy + 13, 50, 12, SKIN_HI, None, r=5)
    rect(d, x - 22, cy + 57, 44, 4, SKIN_SH)
    # 前髪: 中央から左右に流れる大きめの毛束
    round_box(d, x - 34, cy + 2, 68, 22, HAIR, LINE, r=8)
    round_box(d, x - 31, cy + 4, 62, 8, HAIR_HI, None, r=5)
    for bx, bw, bh, r in ((-31, 16, 26, 5), (-11, 22, 20, 6), (13, 17, 24, 5)):
        round_box(d, x + bx, cy + 18, bw, bh, HAIR, None, r=r)
        rect(d, x + bx + 2, cy + 19, bw - 6, 3, HAIR_HI)
        rect(d, x + bx + 1, cy + 18 + bh - 4, bw - 3, 3, HAIR_MID)
    # サイドテール（右）とリボン
    round_box(d, x + 27, cy + 14, 13, 44, HAIR, LINE, r=5)
    rect(d, x + 30, cy + 17, 5, 16, HAIR_HI)
    round_box(d, x - 38, cy + 16, 10, 36, HAIR_MID, LINE, r=4)
    round_box(d, x + 21, cy + 2, 22, 14, RIBBON, RIBBON_DK, r=4)
    rect(d, x + 25, cy + 5, 6, 4, (255, 190, 210))
    # 目（顔の 4 割）: まつ毛 → 白目 → 瞳の 2 色 → ハイライト 2 つ
    if eyes == 'open':
        for ex, flip in ((-20, False), (6, True)):
            round_box(d, x + ex, cy + 26, 15, 20, EYE_HI, LASH, r=4)
            round_box(d, x + ex + 2, cy + 28, 11, 16, EYE_DK, None, r=3)   # 瞳（上）
            round_box(d, x + ex + 2, cy + 36, 11, 8, EYE_LT, None, r=3)    # 瞳（下・明るい）
            rect(d, x + ex + 3, cy + 29, 5, 5, EYE_HI)                     # 大きいハイライト
            rect(d, x + ex + 9, cy + 38, 3, 3, EYE_HI)                     # 小さいハイライト
            rect(d, x + ex, cy + 25, 15, 3, LASH)                          # 上まつ毛
            rect(d, x + ex + (10 if not flip else 0), cy + 23, 5, 3, LASH) # 目尻の跳ね
    elif eyes == 'closed':
        for ex in (-20, 6):
            rect(d, x + ex + 1, cy + 34, 13, 3, LASH)
            rect(d, x + ex + 3, cy + 31, 9, 3, LASH)
            rect(d, x + ex, cy + 37, 4, 3, LASH)
    else:                                                                   # wide（驚き）
        for ex in (-21, 6):
            round_box(d, x + ex, cy + 24, 16, 23, EYE_HI, LASH, r=4)
            round_box(d, x + ex + 3, cy + 27, 10, 15, EYE_DK, None, r=3)
            rect(d, x + ex + 4, cy + 28, 5, 5, EYE_HI)
    # 頬・鼻・口
    rect(d, x - 26, cy + 46, 8, 5, BLUSH)
    rect(d, x - 25, cy + 48, 6, 3, (255, 132, 160))
    rect(d, x + 18, cy + 46, 8, 5, BLUSH)
    rect(d, x + 19, cy + 48, 6, 3, (255, 132, 160))
    if mouth == 'open':
        round_box(d, x - 6, cy + 50, 12, 10, (214, 96, 118), LINE, r=4)
        rect(d, x - 3, cy + 52, 6, 3, (255, 178, 190))
    elif mouth == 'smile':
        rect(d, x - 5, cy + 53, 10, 3, LINE)
        rect(d, x - 7, cy + 51, 3, 2, LINE)
        rect(d, x + 4, cy + 51, 3, 2, LINE)
    else:
        rect(d, x - 3, cy + 53, 6, 3, LINE)

def cape(d, cx, cy, sway=0):
    round_box(d, cx - 4 + sway, cy, 26, 34, CAPE, CAPE_DK, r=5)
    rect(d, cx + sway + 2, cy + 30, 20, 8, CAPE_DK)

def body(d, cx, cy, lean=0):
    """小さな胴＋ふくらんだスカート。cy は肩。"""
    x = cx + lean
    round_box(d, x - 17, cy, 34, 24, DRESS, LINE, r=5)
    round_box(d, x - 15, cy + 1, 30, 7, DRESS_HI, None, r=3)
    rect(d, x - 17, cy, 34, 4, TRIM)
    rect(d, x - 4, cy + 4, 8, 15, TRIM)
    rect(d, x - 3, cy + 6, 3, 9, (255, 236, 168))
    rect(d, x - 17, cy + 20, 34, 6, TRIM_DK)
    rect(d, x - 17, cy + 21, 34, 3, TRIM)
    # スカート（下に向かって大きく広がる＝ちびらしさ）
    for i, (w, c) in enumerate(((38, DRESS), (46, DRESS), (54, DRESS_SH), (60, DRESS_SH))):
        round_box(d, x - w // 2, cy + 26 + i * 5, w, 7, c, None, r=3)
    rect(d, x - 30, cy + 44, 60, 4, DRESS_DK)
    for fx in range(-28, 29, 12):
        round_box(d, x + fx, cy + 46, 10, 5, DRESS_HI, None, r=2)

def arms(d, cx, cy, la=(-24, 0), ra=(16, 0)):
    """短い腕＋丸い手。"""
    for ax, ay in (la, ra):
        round_box(d, cx + ax, cy + ay, 10, 16, SKIN, LINE, r=3)
        rect(d, cx + ax + 2, cy + ay + 1, 6, 4, SKIN_HI)
        round_box(d, cx + ax - 2, cy + ay + 13, 14, 13, DRESS, LINE, r=5)   # ミトン
        rect(d, cx + ax - 1, cy + ay + 14, 12, 3, TRIM)

def legs(d, cx, cy, l=0, r=0):
    """短い脚＋大きめのブーツ。"""
    for dx, off in ((-14, l), (3, r)):
        round_box(d, cx + dx + off, cy, 11, 12, SKIN, LINE, r=3)
        round_box(d, cx + dx + off - 2, cy + 9, 15, 14, BOOT, LINE, r=5)
        rect(d, cx + dx + off - 1, cy + 10, 13, 4, TRIM_DK)
        rect(d, cx + dx + off - 1, cy + 19, 13, 3, BOOT_DK)

# --------------------------------------------------------------------------
# ポーズ
# --------------------------------------------------------------------------
CX, GY = 92, 176

def pose_idle(d, f):
    bob = [0, -2, 0, 2][f]
    cape(d, CX + 10, GY - 66 + bob, sway=[0, 1, 0, -1][f])
    legs(d, CX, GY - 24)
    body(d, CX, GY - 70 + bob)
    arms(d, CX, GY - 66 + bob, (-25, 2), (15, 2))
    head(d, CX, GY - 140 + bob, mouth='smile')
    sword(d, CX + 28, GY - 12, 78, 52)

def pose_walk(d, f):
    bob = [0, -3, -1, -3, 0, -2][f]
    lf = [-5, 0, 5, 5, 0, -5][f]
    rf = [5, 0, -5, -5, 0, 5][f]
    lean = [0, 2, 0, -2, 0, 2][f]
    cape(d, CX + 10, GY - 66 + bob, sway=[2, 3, 1, -1, 0, 2][f])
    legs(d, CX, GY - 24, lf, rf)
    body(d, CX, GY - 70 + bob, lean=lean)
    arms(d, CX + lean, GY - 66 + bob, (-25, rf // 2), (15, lf // 2))
    head(d, CX, GY - 140 + bob, tilt=lean, mouth='smile')
    sword(d, CX + 28, GY - 12 + bob, 78, 52)

def pose_attack(d, f):
    ang = [112, 140, 44, 8][f]
    lean = [-4, -6, 4, 6][f]
    bob = [0, -4, 2, 4][f]
    cape(d, CX + 10, GY - 66 + bob, sway=[-2, -3, 2, 3][f])
    legs(d, CX, GY - 24, -3, 3)
    body(d, CX, GY - 70 + bob, lean=lean)
    arms(d, CX + lean, GY - 70 + bob, (-22, 4), (14, -6 if f < 2 else 8))
    head(d, CX, GY - 140 + bob, tilt=lean, mouth='open', eyes='open' if f < 2 else 'closed')
    sword(d, CX + 16 + lean, GY - 66 + bob, ang, 50)
    if f == 3:
        line_thick(d, CX + 8, GY - 96, CX + 56, GY - 34, GLOW, 5)
        line_thick(d, CX + 14, GY - 94, CX + 60, GY - 40, (255, 255, 255), 2)

def pose_slash(d, f):
    ang = [172, 126, 24, -6][f]
    lean = [-6, -2, 6, 8][f]
    cape(d, CX + 10, GY - 66, sway=[-3, -1, 3, 4][f])
    legs(d, CX, GY - 24, -5, 5)
    body(d, CX, GY - 70, lean=lean)
    arms(d, CX + lean, GY - 68, (-22, 3), (16, 3))
    head(d, CX, GY - 140, tilt=lean, mouth='open')
    sword(d, CX + 14 + lean, GY - 62, ang, 52)
    if f >= 2:
        line_thick(d, CX - 34, GY - 66, CX + 62, GY - 62, GLOW, 5)
        line_thick(d, CX - 30, GY - 62, CX + 58, GY - 60, (255, 255, 255), 2)

def pose_cast(d, f):
    up = [-12, -22, 0, 0][f]
    cape(d, CX + 10, GY - 66 + up // 2, sway=[-2, -4, 2, 0][f])
    legs(d, CX, GY - 24 - max(0, -up) // 4)
    body(d, CX, GY - 70 + up // 2)
    arms(d, CX, GY - 74 + up // 2, (-24, -10 if f < 2 else 5), (16, -10 if f < 2 else 5))
    head(d, CX, GY - 140 + up // 2, mouth='open', eyes='closed' if f == 3 else 'wide')
    ang = [96, 96, 62, 272][f]
    sword(d, CX + 6, GY - 62 + up // 2, ang, 50)
    if f == 3:
        for i in range(4):
            rect(d, CX - 44 - i * 9, GY - 8 + i * 2, 22 - i * 3, 4, GLOW)
            rect(d, CX + 24 + i * 9, GY - 8 + i * 2, 22 - i * 3, 4, GLOW)
        rect(d, CX - 18, GY - 6, 38, 4, (255, 255, 255))

def pose_guard(d, f):
    sh = [0, 2][f]
    cape(d, CX + 12, GY - 66 + sh, sway=-2)
    legs(d, CX, GY - 24, 3, -3)
    body(d, CX, GY - 70 + sh, lean=-4)
    arms(d, CX - 5, GY - 68 + sh, (-15, 2), (5, 2))
    head(d, CX, GY - 140 + sh, tilt=-2, eyes='closed')
    sword(d, CX - 16, GY - 20 + sh, 88, 56)

def pose_hit(d, f):
    back = [5, 9][f]
    cape(d, CX + 12 + back, GY - 64, sway=4)
    legs(d, CX + back // 2, GY - 24, 5, -2)
    body(d, CX + back, GY - 68, lean=4)
    arms(d, CX + back, GY - 64, (-27, -6), (18, -6))
    head(d, CX + back + 2, GY - 138, tilt=4, eyes='closed', mouth='open')
    sword(d, CX + back + 26, GY - 16, 58, 48)
    for sx, sy, ss in ((-46, -120, 8), (-36, -136, 6), (-54, -104, 5)):
        rect(d, CX + sx, GY + sy, ss, ss, (170, 224, 255))

def pose_victory(d, f):
    jump = [0, -6, -10, -5][f]
    cape(d, CX + 10, GY - 66 + jump, sway=[0, -2, -3, -1][f])
    legs(d, CX, GY - 24 + jump, -2, 2)
    body(d, CX, GY - 70 + jump)
    arms(d, CX, GY - 78 + jump, (-25, -8), (14, -12))
    head(d, CX, GY - 140 + jump, mouth='open', eyes='closed')
    sword(d, CX + 16, GY - 88 + jump, 90, 48)
    if f >= 1:
        for gx, gy_, gs in ((4, -162, 6), (28, -154, 5), (-18, -148, 5), (40, -138, 4)):
            rect(d, CX + gx, GY + gy_, gs, gs, GLOW)

def pose_focus(d, f):
    bob = [0, -2, -2, 0][f]
    cape(d, CX + 10, GY - 66 + bob, sway=-1)
    legs(d, CX, GY - 24, 2, -2)
    body(d, CX, GY - 70 + bob, lean=-2)
    arms(d, CX - 2, GY - 68 + bob, (-22, 3), (14, 3))
    head(d, CX, GY - 140 + bob, eyes='closed')
    sword(d, CX + 6, GY - 24 + bob, 85, 50)
    if f in (1, 2):
        for lx, ly in ((-56, -96), (52, -106), (-50, -66), (48, -72)):
            rect(d, CX + lx, GY + ly, 4, 14, (170, 214, 255))

POSES = {
    'idle': (pose_idle, 4), 'walk': (pose_walk, 6), 'attack': (pose_attack, 4),
    'slash': (pose_slash, 4), 'cast': (pose_cast, 4), 'guard': (pose_guard, 2),
    'hit': (pose_hit, 2), 'victory': (pose_victory, 4), 'focus': (pose_focus, 4),
}

def main():
    os.makedirs(OUT, exist_ok=True)
    for name, (fn, frames) in POSES.items():
        strip = Image.new('RGBA', (W * frames, H), (0, 0, 0, 0))
        for f in range(frames):
            fr = Image.new('RGBA', (W, H), (0, 0, 0, 0))
            d = ImageDraw.Draw(fr)
            round_box(d, CX - 28, GY + 2, 56, 8, (0, 0, 0, 80), None, r=3)
            fn(d, f)
            strip.paste(fr, (f * W, 0))
        big = strip.resize((W * frames * SCALE, H * SCALE), Image.NEAREST)
        path = os.path.join(OUT, f'hero_{name}.png')
        big.save(path)
        print(f'wrote {path}  ({frames} frames)')

if __name__ == '__main__':
    main()
