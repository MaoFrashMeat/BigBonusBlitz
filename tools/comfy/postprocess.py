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

def trim_and_align(im, box_h, canvas):
    """
    足元を下端に合わせ、身長を box_h に揃える。
    コマごとに大きさが違うと、並べたときに伸び縮みして見えるのを防ぐ。
    """
    bb = im.getbbox()
    if not bb:
        return Image.new('RGBA', (canvas, canvas), (0, 0, 0, 0))
    body = im.crop(bb)
    scale = box_h / body.height
    nw, nh = max(1, int(body.width * scale)), max(1, int(body.height * scale))
    body = body.resize((nw, nh), Image.LANCZOS)
    out = Image.new('RGBA', (canvas, canvas), (0, 0, 0, 0))
    out.paste(body, ((canvas - nw) // 2, canvas - nh - int(canvas * 0.04)), body)
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

    box_h = int(args.dots * 0.80)          # 身長はコマの 8 割で固定
    made = 0
    for action, frames in actions.items():
        cells = []
        for fr in sorted(frames):
            p = os.path.join(RAW, f'{action}_{fr}.png')
            if not os.path.exists(p):
                print(f'  なし: {action}_{fr}.png（このコマは飛ばします）')
                continue
            im = remove_bg(Image.open(p).convert('RGBA'))
            # 元は 1024 四方。まず大きいまま位置と身長を揃えてから、最後にドット化する
            im = trim_and_align(im, int(1024 * 0.80), 1024)
            cells.append(pixelate(im, args.dots, args.colors))
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
