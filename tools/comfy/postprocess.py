# 生成された 1 枚絵を、Unity にそのまま置けるスプライトシートに変える。
# 使い方:
#   python tools/comfy/postprocess.py              # raw/ を読んで sheets/ に出す
#   python tools/comfy/postprocess.py --install    # そのまま Resources/Art/HeroGen へ入れる
#   python tools/comfy/postprocess.py --dots 192   # 1 コマのドット数を変える
#
# やること（順に）:
#   1. 背景を抜く（白背景を透過に。rembg があればそちらを使う）
#   2. 足元を基準に位置と大きさを揃える（コマ間で身長が揺れないように）
#   3. 色数を落とす（ドット絵らしい平坦な塗りにする）
#   4. ピクセル格子に乗せる（縮小してから最近傍で拡大）
#   5. アクションごとに横へ並べてストリップにする
import argparse
import json
import os
import shutil

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, 'raw')
SHEETS = os.path.join(HERE, 'sheets')
POSES = os.path.join(HERE, 'poses')
INSTALL = os.path.join(HERE, '..', '..', 'UnityProject', 'BigBonusBlitz',
                       'Assets', 'Resources', 'Art', 'HeroGen')

def remove_bg(im):
    """白背景を抜く。rembg があればそれを使い、無ければ明るさで判定する。"""
    try:
        from rembg import remove          # 入っていれば精度が高い
        return remove(im)
    except Exception:
        pass
    im = im.convert('RGBA')
    px = im.load()
    w, h = im.size
    # 四隅から白をたどって外側だけ抜く（服の白は残す）
    seen = set()
    stack = [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]
    while stack:
        x, y = stack.pop()
        if not (0 <= x < w and 0 <= y < h) or (x, y) in seen:
            continue
        r, g, b, a = px[x, y]
        if a == 0 or (r > 228 and g > 228 and b > 228):
            seen.add((x, y))
            px[x, y] = (r, g, b, 0)
            stack.extend(((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))
    return im

def body_span(im):
    """体の上端・下端・足元の左右中心を返す。

    外接枠をそのまま使うと、剣の先が飛び出したコマだけ小さく揃ってしまう。
    横に太い行だけを体とみなすことで、細い剣を無視する。
    """
    a = im.getchannel('A')
    w, h = im.size
    px = a.load()
    rows = []
    for y in range(h):
        n = 0
        for x in range(0, w, 2):          # 2 画素おきで足りる
            if px[x, y] > 128:
                n += 1
        rows.append(n * 2)
    body = max(rows) if rows else 0
    if body == 0:
        return None
    thick = max(8, int(body * 0.22))      # 体は最大幅の 2 割以上ある。剣は届かない
    ys = [y for y, n in enumerate(rows) if n >= thick]
    if not ys:
        return None
    top, bottom = ys[0], ys[-1]
    # 足元の左右中心（下から 12% ぶん）
    y0 = bottom - max(1, int((bottom - top) * 0.12))
    acc = tot = 0
    for y in range(y0, bottom + 1):
        for x in range(w):
            if px[x, y] > 128:
                acc += x
                tot += 1
    cx = (acc / tot) if tot else w * 0.5
    return top, bottom, cx

def align_all(images, box_h, canvas):
    """全コマを **同じ背丈** に揃え、足元を同じ高さに置く。

    倍率はコマごとに「頭から足まで」で決める。こうすると剣を振り上げても
    キャラの大きさは変わらない。はみ出した剣は枠の外に出るぶんだけ切る。
    """
    spans = [body_span(im) for im in images]
    floor_y = canvas - int(canvas * 0.06)

    out = []
    for im, sp in zip(images, spans):
        blank = Image.new('RGBA', (canvas, canvas), (0, 0, 0, 0))
        if not sp:
            out.append(blank)
            continue
        top, bottom, cx = sp
        sc = box_h / max(1, bottom - top)
        bb = im.getbbox()
        body = im.crop(bb)
        nw, nh = max(1, int(body.width * sc)), max(1, int(body.height * sc))
        body = body.resize((nw, nh), Image.LANCZOS)
        # 足の裏を floor_y に、足元の左右中心を画面中央に
        x = int(canvas * 0.5 - (cx - bb[0]) * sc)
        y = int(floor_y - (bottom - bb[1]) * sc)
        blank.paste(body, (x, y), body)
        out.append(blank)
    return out

def pixelate(im, dots, colors):
    """ドット数まで落として色を減らす。輪郭を残すため半透明は捨てる。"""
    small = im.resize((dots, dots), Image.LANCZOS)
    a = small.getchannel('A').point(lambda v: 255 if v > 128 else 0)
    rgb = small.convert('RGB').quantize(colors=colors, method=Image.MEDIANCUT, dither=Image.NONE).convert('RGB')
    rgb.putalpha(a)
    return rgb

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--dots', type=int, default=192, help='1 コマのドット数（既定 192）')
    ap.add_argument('--colors', type=int, default=32, help='色数（既定 32）')
    ap.add_argument('--scale', type=int, default=2, help='書き出し倍率（既定 2）')
    ap.add_argument('--install', action='store_true', help='Resources/Art/HeroGen へ入れる')
    args = ap.parse_args()

    if not os.path.isdir(RAW):
        print('raw/ がありません。先に run_batch.py --pull を実行してください')
        return
    with open(os.path.join(POSES, 'index.json'), encoding='utf-8') as f:
        index = json.load(f)

    # アクションごとにコマを集める
    actions = {}
    for it in index:
        actions.setdefault(it['action'], []).append(it['frame'])
    os.makedirs(SHEETS, exist_ok=True)

    # 先に全コマを読む。倍率はここで 1 つだけ決めて、全アクションに同じものを使う。
    # アクションごとに決めると、立ちと勝利でキャラの背丈が変わってしまう。
    loaded = {}
    for action, frames in actions.items():
        for fr in sorted(frames):
            fp = os.path.join(RAW, f'{action}_{fr}.png')
            if not os.path.exists(fp):
                print(f'  なし: {action}_{fr}.png（このコマは飛ばします）')
                continue
            loaded[(action, fr)] = remove_bg(Image.open(fp).convert('RGBA'))
    if not loaded:
        print('raw/ に画像がありません')
        return
    keys = list(loaded.keys())
    aligned = align_all([loaded[k] for k in keys], int(1024 * 0.72), 1024)
    aligned = dict(zip(keys, aligned))

    made = 0
    for action, frames in actions.items():
        cells = [pixelate(aligned[(action, fr)], args.dots, args.colors)
                 for fr in sorted(frames) if (action, fr) in aligned]
        if not cells:
            continue
        strip = Image.new('RGBA', (args.dots * len(cells), args.dots), (0, 0, 0, 0))
        for i, c in enumerate(cells):
            strip.paste(c, (i * args.dots, 0), c)
        big = strip.resize((strip.width * args.scale, strip.height * args.scale), Image.NEAREST)
        out = os.path.join(SHEETS, f'hero_{action}.png')
        big.save(out)
        made += 1
        print(f'{action}: {len(cells)} コマ -> {out}')

    if args.install and made:
        os.makedirs(INSTALL, exist_ok=True)
        for f in os.listdir(SHEETS):
            if f.endswith('.png'):
                shutil.copy2(os.path.join(SHEETS, f), os.path.join(INSTALL, f))
        print('Resources/Art/HeroGen へ入れました')
    print(f'{made} アクション分を書き出しました')

if __name__ == '__main__':
    main()
