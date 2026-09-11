# assets/title のシート 2 枚（枠・アイコン）から部品を切り出し、Unity へ入れる。
#
#   python tools/ui/cut_sheets.py            # assets/title/parts/ へ切り出すだけ
#   python tools/ui/cut_sheets.py --install  # Resources/Art/UI/Frames, Icons へもコピー
#
# 枠は 9 分割で伸ばす前提。伸ばしてよい範囲の境目（縁の幅）は下の FRAMES に書き、
# frames_manifest.json に出す。UiSkin.FrameBorders はその値を写したもの。
# 位置はシートの画像から拾った数値なので、シートを描き直したらここも直す。
import argparse
import json
import os
import shutil

from PIL import Image, ImageOps
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SRC = os.path.join(ROOT, 'assets', 'title')
OUT = os.path.join(SRC, 'parts')
UNITY_UI = os.path.join(ROOT, 'UnityProject', 'BigBonusBlitz', 'Assets', 'Resources', 'Art', 'UI')

ALPHA_MIN = 40


# ------------------------------------------------------------------ 道具
def crop(im, box):
    x, y, w, h = box
    return im.crop((x, y, x + w, y + h))


def trim(im, pad=0):
    """透明を落として実体だけにする。"""
    a = np.array(im)[..., 3] > ALPHA_MIN
    if not a.any():
        return im
    ys, xs = np.nonzero(a)
    x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    x0, y0 = max(0, x0 - pad), max(0, y0 - pad)
    x1, y1 = min(im.width, x1 + pad), min(im.height, y1 + pad)
    return im.crop((x0, y0, x1, y1))


def split(im, axis, min_gap=4):
    """透明の帯で切って部品に分ける。axis=0 で上下、1 で左右。"""
    a = np.array(im)[..., 3] > ALPHA_MIN
    proj = a.any(axis=1 - axis)         # axis=0: 各行に実体があるか
    parts, start, gap = [], None, 0
    for i, v in enumerate(proj):
        if v:
            if start is None:
                start = i
            gap = 0
        else:
            if start is not None:
                gap += 1
                if gap >= min_gap:
                    parts.append((start, i - gap + 1))
                    start, gap = None, 0
    if start is not None:
        parts.append((start, len(proj)))
    out = []
    for s, e in parts:
        box = (0, s, im.width, e) if axis == 0 else (s, 0, e, im.height)
        out.append(trim(im.crop(box)))
    return out


def square(im):
    """アイコン用。正方形の透明キャンバスの中央に置く。"""
    n = max(im.width, im.height)
    canvas = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    canvas.alpha_composite(im, ((n - im.width) // 2, (n - im.height) // 2))
    return canvas


def mirror_lr(im):
    return ImageOps.mirror(im)


def nine_from_half(left_part, mid_sample, right_part=None, total_w=None):
    """
    左端 + 中央の見本 + 右端（無ければ左端の鏡）を横に並べて 9 分割用の絵にする。
    ゲージやピルのように「片側だけ絵が乗っている」部品から、伸ばせる 1 枚を作る。
    """
    if right_part is None:
        right_part = mirror_lr(left_part)
    h = left_part.height
    mid_w = mid_sample.width if total_w is None else max(mid_sample.width, total_w - left_part.width - right_part.width)
    canvas = Image.new('RGBA', (left_part.width + mid_w + right_part.width, h), (0, 0, 0, 0))
    canvas.alpha_composite(left_part, (0, 0))
    # 中央は見本を敷き詰める（伸ばすのは Unity 側なので、ここでは埋めるだけ）
    x = left_part.width
    while x < left_part.width + mid_w:
        w = min(mid_sample.width, left_part.width + mid_w - x)
        canvas.alpha_composite(mid_sample.crop((0, 0, w, h)), (x, 0))
        x += w
    canvas.alpha_composite(right_part, (left_part.width + mid_w, 0))
    return canvas


def desaturate_light(im):
    """フィルを白っぽくして、Unity 側で色を掛けられるようにする（明るさは残す）。"""
    a = np.array(im).astype(np.float32)
    rgb, al = a[..., :3], a[..., 3:]
    lum = rgb.max(axis=2, keepdims=True)          # 一番明るい成分を明度として使う
    lum = np.clip(lum * 1.15 + 20, 0, 255)
    out = np.concatenate([np.repeat(lum, 3, axis=2), al], axis=2)
    return Image.fromarray(out.astype(np.uint8), 'RGBA')


# ------------------------------------------------------------------ 枠
# name: (シート上の箱 (x, y, w, h), 分け方, 縁 (left, bottom, right, top) / None なら伸ばさない)
# 分け方: None=そのまま / ('rows', n番目) / ('cols', n番目)。枠は隣と密着しているので座標で切っている
FRAMES = {
    # 板
    'panel_navy':      ((267, 328, 290, 115), None, (28, 26, 28, 26)),
    'panel_cream_sm':  ((1020, 64, 98, 85), None, (22, 20, 22, 20)),
    'panel_cream_tab': ((257, 152, 559, 165), None, None),        # 見出しの札つき。伸ばさず使う
    'panel_cream_tab_sm': ((15, 809, 439, 128), None, None),
    'panel_cream_tabs': ((260, 570, 512, 229), None, None),      # タブ 4 つ + 罫線つき
    'card_castle':     ((775, 575, 223, 226), None, None),
    'bar_cream_sm':    ((588, 941, 161, 44), None, (16, 12, 16, 12)),
    # 板の中の枠（くぼみ）
    'slot_navy':       ((1007, 730, 97, 90), None, (20, 18, 20, 18)),
    # ボタン（隣と 1〜2px しか離れていないので座標で切る）
    'btn_blue_lg':     ((257, 453, 289, 102), None, (48, 22, 48, 22)),
    'btn_blue':        ((548, 453, 227, 102), None, (40, 20, 40, 20)),
    'btn_gray':        ((778, 453, 236, 100), None, (40, 20, 40, 20)),
    'btn_pink':        ((561, 344, 246, 90), None, (40, 20, 40, 20)),
    'btn_cream':       ((808, 344, 206, 90), None, (40, 20, 40, 20)),
    'btn_blue_light':  ((1018, 344, 204, 88), None, (40, 20, 40, 20)),
    'btn_pill_blue':   ((1244, 538, 190, 64), None, (36, 18, 36, 18)),
    'btn_pill_red':    ((1244, 605, 190, 61), None, (36, 18, 36, 18)),
    'btn_pill_purple': ((1244, 669, 190, 59), None, (36, 18, 36, 18)),
    'plate_hex_sky':   ((8, 166, 248, 74), None, (30, 18, 30, 18)),
    'plate_hex_cream': ((8, 243, 248, 76), None, (30, 18, 30, 18)),
    'pill_navy_sm':    ((1341, 366, 94, 45), None, (20, 14, 20, 14)),
    # 数値の窓（左にアイコンが乗っている。左の縁を広く取ってアイコンを伸ばさない）
    'pill_gem':        ((650, 73, 180, 56), None, (60, 14, 24, 14)),
    'pill_coin':       ((831, 72, 182, 56), None, (60, 14, 24, 14)),
    'pill_compass':    ((9, 54, 368, 100), None, (112, 22, 30, 22)),
    'pill_ring':       ((382, 54, 267, 100), None, (96, 20, 30, 20)),
    # 小物（伸ばさない）
    'circle_navy':     ((17, 946, 80, 86), None, None),
    'ring_gray':       ((773, 809, 140, 132), None, None),
    'medal_compass':   ((1328, 54, 98, 100), None, None),
    'toggle_on':       ((1228, 314, 99, 46), None, None),
    'toggle_off':      ((1333, 314, 97, 46), None, None),
    'check_navy_sm':   ((1240, 367, 45, 44), None, None),
    'diamond_navy':    ((1242, 471, 61, 60), None, None),
    'diamond_down':    ((1185, 479, 54, 46), None, None),
    # 帯・リボン
    'ribbon_blue':     ((926, 822, 216, 62), None, None),
    'ribbon_red':      ((926, 884, 216, 65), None, None),
    'ribbon_gold':     ((926, 940, 216, 70), None, None),
    'toast_green':     ((1147, 823, 284, 59), None, (30, 12, 30, 12)),
    'toast_brown':     ((1147, 884, 284, 57), None, (30, 12, 30, 12)),
    'toast_red':       ((1147, 943, 284, 62), None, (30, 12, 30, 12)),
    # 絵（背景の額）
    'banner_castle_wide': ((823, 166, 397, 156), None, None),
    'banner_castle_sm':   ((1227, 166, 210, 138), None, None),
}

# ゲージ: 緑のバー（232x28）から「空のトラック」と「白いフィル」を組み立てる
GAUGE_SRC = (1006, 543, 232, 28)


def build_gauges(sheet, k=1.0):
    # ゲームのゲージは高さ 5〜10px。元の 28px のまま 9 分割すると縁だけで潰れるので、先に 12px へ縮める
    bar = trim(crop(sheet, tuple(round(v * k) for v in GAUGE_SRC)))
    h = 12
    bar = bar.resize((round(bar.width * h / bar.height), h), Image.LANCZOS)
    w = bar.width
    cap = 6                                   # 端の丸み + 金の縁
    # トラック: 右端（空いている側）を左右の端に使い、中央は空の部分の見本
    right = bar.crop((w - cap, 0, w, h))
    mid_empty = bar.crop((w - cap - 16, 0, w - cap, h))
    track = nine_from_half(mirror_lr(right), mid_empty, right, total_w=48)
    # フィル: 左端（塗られている側）を両端に、中央は塗りの見本。白くして Unity で色を掛ける
    inner = 2                                 # トラックの縁の内側から始める
    left = bar.crop((inner, inner, inner + cap, h - inner))
    mid_full = bar.crop((inner + cap, inner, inner + cap + 16, h - inner))
    fill = desaturate_light(nine_from_half(left, mid_full, total_w=48))
    return {
        'gauge_track': (track, (cap, 4, cap, 4)),
        'gauge_fill': (fill, (cap - 1, 3, cap - 1, 3)),
    }


# ------------------------------------------------------------------ アイコン
ICONS = {
    'compass':      ((1, 48, 365, 234), ('cols', 0)),
    'home':         ((1, 48, 365, 234), ('cols', 1)),
    'star_emblem':  ((1250, 84, 180, 186), None),
    'swords':       ((392, 105, 150, 151), None),
    'party':        ((566, 115, 148, 129), None),
    'bag':          ((741, 111, 137, 143), None),
    'book':         ((910, 115, 154, 136), None),
    'shop':         ((1091, 111, 146, 137), None),
    'gift':         ((37, 317, 138, 152), None),
    'mail':         ((216, 341, 138, 107), None),
    'gear':         ((389, 319, 151, 151), None),
    'menu':         ((577, 343, 129, 109), None),
    'sound':        ((754, 333, 144, 131), None),
    'music':        ((937, 316, 125, 148), None),
    'sparkle':      ((1109, 322, 146, 142), None),
    'search':       ((1286, 321, 136, 141), None),
    'check_on':     ((46, 539, 114, 111), None),
    'check_off':    ((207, 538, 108, 108), None),
    'arrow_left':   ((351, 544, 71, 108), None),
    'arrow_right':  ((479, 544, 72, 108), None),
    'arrow_up':     ((593, 549, 119, 79), None),
    'arrow_down':   ((744, 569, 99, 71), None),
    'diamond_star': ((880, 531, 121, 123), None),
    'diamond_star2': ((1018, 532, 120, 121), None),
    'diamond_dots': ((1153, 532, 120, 122), None),
    'refresh':      ((1290, 541, 131, 127), None),
    'lock':         ((45, 710, 109, 146), None),
    'swords_lg':    ((194, 710, 150, 152), None),
    'swords_dark':  ((372, 710, 156, 152), None),
    'crystal':      ((569, 708, 118, 175), None),
    'coin':         ((729, 718, 146, 150), None),
    'tome':         ((902, 711, 174, 169), None),
    'feather':      ((1108, 708, 137, 171), None),
    'star_navy':    ((1269, 710, 139, 154), None),
    'crystal2':     ((77, 899, 107, 153), None),
    'ember':        ((243, 911, 118, 132), None),
    'shard':        ((402, 910, 111, 136), None),
    'orb':          ((569, 921, 114, 119), None),
    'heart':        ((730, 916, 138, 121), None),
    'star_gold':    ((913, 912, 126, 135), None),
    'star_blue':    ((1084, 923, 101, 108), None),
    'moon':         ((1242, 911, 119, 127), None),
}


NAVI_SRC = os.path.join(ROOT, 'assets', 'navi')     # navi_BG.png + navi_normal_*.png（同じ 1254px の紙に描いてある）
NAVI_SIZE = 256                                     # 画面では 90px。2 倍端末でも 256 あれば足りる
NAVI_FILES = {
    'navi_BG.png': 'navi_bg',
    'navi_normal_01.png': 'navi_01', 'navi_normal_02.png': 'navi_02', 'navi_normal_03.png': 'navi_03',
    'navi_normal_question.png': 'navi_question', 'navi_normal_circle.png': 'navi_circle',
    'navi_normal_cross.png': 'navi_cross', 'navi_normal_hyphen.png': 'navi_hyphen',
}


def install_navi():
    """
    ナビの紋章（背景）と文字（手前）を縮めて Resources へ。
    どれも同じ大きさの紙に位置を合わせて描いてあるので、切り詰めずに紙ごと縮める（重ねたときズレない）。
    """
    if not os.path.isdir(NAVI_SRC):
        return
    dst = os.path.join(UNITY_UI, 'Navi')
    os.makedirs(dst, exist_ok=True)
    n = 0
    for src_name, out_name in NAVI_FILES.items():
        src = os.path.join(NAVI_SRC, src_name)
        if not os.path.exists(src):
            continue
        im = Image.open(src).convert('RGBA')
        im = im.resize((NAVI_SIZE, round(im.height * NAVI_SIZE / im.width)), Image.LANCZOS)
        im.save(os.path.join(dst, out_name + '.png'))
        n += 1
    print(f'  ナビ {n} 枚 → {dst}')


TITLE_BG_SRC = os.path.join(ROOT, 'assets', 'title', 'BG')
TITLE_BG_FILES = ['sky_mountains_cloudsea', 'sky_mountains', 'castle_mountains_lake', 'castle_mountains', 'castle_lake', 'town_lake', 'terrace_balcony', 'petals_overlay']   # TitleParallax が選べる層


def install_title_bg():
    """タイトル背景の層を Resources へ。縮めずにそのまま（1672px。iPhone では 1.5 倍に伸びる）。"""
    if not os.path.isdir(TITLE_BG_SRC):
        return
    dst = os.path.join(UNITY_UI, 'Title')
    os.makedirs(dst, exist_ok=True)
    n = 0
    for name in TITLE_BG_FILES:
        src = os.path.join(TITLE_BG_SRC, name + '.png')
        if os.path.exists(src):
            shutil.copy2(src, os.path.join(dst, name + '.png'))
            n += 1
    print(f'  タイトル背景 {n} 枚 → {dst}')


PETAL_SRC = os.path.join(ROOT, 'assets', 'title', 'BG', 'flower')   # 花びら（1 枚 1 ファイル。名前は問わない）
PETAL_CANVAS = 256      # 画布。花びらはこの半分（128px）に収め、周りをぼかしの余白にする
PETAL_FILL = 0.5        # 画布に対する花びらの大きさ。TitleAmbience / title_viewer はこの逆数を掛けて「見える大きさ」に合わせる
# ぼかしの強さ（花びらの大きさに対する比）。ゲーム側の blur 1..3
PETAL_BLUR = {1: 0.04, 2: 0.09, 3: 0.16}


def install_petals():
    """
    花びらを Resources/Art/UI/Title/petal_N（素）と petal_N_b1..3（ぼかし）へ。
    画面では 8〜48px（iPhone で 2 倍）なので画布 256px で足りる。
    ぼかしは表示の大きさに対する比で決めている。元の 1254px に 2〜9px 掛けても、縮めたら消えてしまうため。
    """
    if not os.path.isdir(PETAL_SRC):
        return
    from PIL import ImageFilter
    dst = os.path.join(UNITY_UI, 'Title')
    os.makedirs(dst, exist_ok=True)
    files = sorted(f for f in os.listdir(PETAL_SRC) if f.lower().endswith('.png'))
    petal_px = round(PETAL_CANVAS * PETAL_FILL)
    for i, f in enumerate(files, 1):
        im = trim(Image.open(os.path.join(PETAL_SRC, f)).convert('RGBA'))
        k = petal_px / max(im.width, im.height)
        im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
        canvas = Image.new('RGBA', (PETAL_CANVAS, PETAL_CANVAS), (0, 0, 0, 0))
        canvas.alpha_composite(im, ((PETAL_CANVAS - im.width) // 2, (PETAL_CANVAS - im.height) // 2))
        canvas.save(os.path.join(dst, f'petal_{i}.png'))
        for level, frac in PETAL_BLUR.items():
            canvas.filter(ImageFilter.GaussianBlur(frac * petal_px)).save(os.path.join(dst, f'petal_{i}_b{level}.png'))
    print(f'  花びら {len(files)} 枚 ×（素 + ぼかし 3 段）→ {dst}（画布 {PETAL_CANVAS}px、花びら {petal_px}px）')


def install(fdir, idir):
    """parts/ の画像を Unity の Resources へコピーする。名前が同じなら上書き。"""
    install_navi()
    install_title_bg()
    install_petals()
    for sub, src_dir in (('Frames', fdir), ('Icons', idir)):
        dst = os.path.join(UNITY_UI, sub)
        os.makedirs(dst, exist_ok=True)
        n = 0
        for fn in os.listdir(src_dir):
            if fn.endswith('.png'):
                shutil.copy2(os.path.join(src_dir, fn), os.path.join(dst, fn))
                n += 1
        print(f'  {n} 枚 → {dst}')


def pick(sheet, box, how):
    im = crop(sheet, box)
    if how is None:
        return trim(im)
    kind, idx = how
    parts = split(im, 0 if kind == 'rows' else 1)
    if idx >= len(parts):
        raise SystemExit(f'{box}: {kind} は {len(parts)} 個しか無い（{idx} 番目は無い）')
    return parts[idx]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--install', action='store_true', help='Unity の Resources へもコピーする')
    ap.add_argument('--scale', type=float, default=1.0, help='シートを何倍にアップスケールしてあるか（箱と縁を同じ倍率で読む）')
    ap.add_argument('--no-cut', action='store_true', help='切り出さず、parts/ にある画像をそのまま Unity へ入れる（1 枚ずつアップスケールしたあと用）')
    ap.add_argument('--frames', default='ui_frames_sheet.png')
    ap.add_argument('--icons', default='icon_parts_sheet.png')
    args = ap.parse_args()
    k = args.scale

    def sc_box(b): return tuple(round(v * k) for v in b)
    def sc_border(b): return tuple(round(v * k) for v in b) if b else None

    fdir, idir = os.path.join(OUT, 'frames'), os.path.join(OUT, 'icons')
    if args.no_cut:
        install(fdir, idir)
        return
    frames_sheet = Image.open(os.path.join(SRC, args.frames)).convert('RGBA')
    icons_sheet = Image.open(os.path.join(SRC, args.icons)).convert('RGBA')
    os.makedirs(fdir, exist_ok=True)
    os.makedirs(idir, exist_ok=True)

    manifest = {}
    for name, (box, how, border) in FRAMES.items():
        im = pick(frames_sheet, sc_box(box), how)
        border = sc_border(border)
        im.save(os.path.join(fdir, name + '.png'))
        manifest[name] = {'w': im.width, 'h': im.height, 'border': list(border) if border else None}
    for name, (im, border) in build_gauges(frames_sheet, k).items():
        im.save(os.path.join(fdir, name + '.png'))
        manifest[name] = {'w': im.width, 'h': im.height, 'border': list(border)}
    with open(os.path.join(fdir, 'frames_manifest.json'), 'w', encoding='utf-8') as f:
        json.dump(manifest, f, ensure_ascii=False, indent=1)

    for name, (box, how) in ICONS.items():
        im = square(pick(icons_sheet, sc_box(box), how))
        im.save(os.path.join(idir, name + '.png'))

    print(f'枠 {len(manifest)} 枚 → {fdir}')
    print(f'アイコン {len(ICONS)} 枚 → {idir}')

    if args.install:
        install(fdir, idir)

    # UiSkin.FrameBorders に写す用（縁と、その値を決めたときの画像の幅）
    print(r'') ; print('// UiSkin.FrameBorders（left, bottom, right, top）, 画像の幅')
    for name, m in manifest.items():
        if m['border']:
            l, b, r, t = m['border']
            print(f'            {{ "{name}", (new Vector4({l}, {b}, {r}, {t}), {m["w"]}) }},')

if __name__ == '__main__':
    main()
