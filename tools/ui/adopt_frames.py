# アップスケールした枠（assets/title/parts/frames_V2 など、v1 と同じ名前で置いたもの）を採用する。
#
#   python tools/ui/adopt_frames.py assets/title/parts/frames_V2
#   python tools/ui/cut_sheets.py --no-cut        # ← そのあと Unity へ入れる
#
# やること:
#   1. 透明の余白を落として parts/frames/<name>.png を置き替える（v1 の切り出しと同じ「実体だけ」の状態にする）
#   2. frames_manifest.json の w / h / border を新しい大きさに合わせる。
#      border は v1 で決めた縁を、幅は幅の比・高さは高さの比で伸ばす（絵の縦横比が変わっていても崩れない）
#   3. 縁を決めたときの大きさ（baseW / baseH）と倍率（scale）も書く。
#      UiSkin.FrameBorders は baseW / baseH を持っているので、画像を差し替えるだけで縁は自動的に合う
#
# 元の v1 の値は frames_manifest.json の baseW / baseH / baseBorder に残るので、何度差し替えても基準がずれない。
import json
import os
import sys

from PIL import Image
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
FRAMES = os.path.join(ROOT, 'assets', 'title', 'parts', 'frames')
MANIFEST = os.path.join(FRAMES, 'frames_manifest.json')
ALPHA_MIN = 40


def trim(im):
    a = np.array(im)[..., 3] > ALPHA_MIN
    if not a.any():
        return im
    ys, xs = np.nonzero(a)
    return im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))


def main():
    if len(sys.argv) < 2:
        raise SystemExit('使い方: python tools/ui/adopt_frames.py <v1 と同じ名前で置いた枠のフォルダ>')
    src = os.path.abspath(sys.argv[1])
    man = json.load(open(MANIFEST, encoding='utf-8'))
    adopted, skipped = [], []
    for fn in sorted(os.listdir(src)):
        if not fn.lower().endswith('.png'):
            continue
        name = fn[:-4]
        if name not in man:
            skipped.append(name)
            continue
        m = man[name]
        # 縁を決めたときの大きさ。初回は v1 の w/h がそのまま基準になる
        m.setdefault('baseW', m['w']); m.setdefault('baseH', m['h'])
        m.setdefault('baseBorder', m['border'])
        im = trim(Image.open(os.path.join(src, fn)).convert('RGBA'))
        im.save(os.path.join(FRAMES, fn))
        sx, sy = im.width / m['baseW'], im.height / m['baseH']
        m['w'], m['h'] = im.width, im.height
        m['scale'] = round(sx, 4)
        if m['baseBorder']:
            l, b, r, t = m['baseBorder']
            m['border'] = [round(l * sx), round(b * sy), round(r * sx), round(t * sy)]
        adopted.append((name, im.width, im.height, round(sx, 2), round(sy, 2)))
    with open(MANIFEST, 'w', encoding='utf-8') as f:
        json.dump(man, f, ensure_ascii=False, indent=1)
    print(f'採用 {len(adopted)} 枚:')
    for name, w, h, sx, sy in adopted:
        flag = '  ← 縦横比が変わっている' if abs(sx - sy) / max(sx, sy) > 0.12 else ''
        print(f'  {name:22s} {w}x{h}  幅 {sx}x / 高さ {sy}x{flag}')
    if skipped:
        print('名前が一覧に無いので飛ばした:', ', '.join(skipped))
    print('\n次: python tools/ui/cut_sheets.py --no-cut')


if __name__ == '__main__':
    main()
