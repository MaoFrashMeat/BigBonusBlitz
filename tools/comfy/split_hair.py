"""立ち絵を「髪」と「それ以外」に分ける（Live2D 風の下ごしらえ・4 段目）。

    py -3 tools/comfy/split_hair.py

髪を別レイヤーにすると、顔や体の上で髪だけを揺らせる。
これが Live2D らしさの本体になる。

髪は桃色ではっきりしているので、色で取る（Segformer などのモデルは要らない）。
髪を抜いたあとの穴は、周りの色を伸ばして塞ぐ。揺れ幅は数 px なので、
塞いだところは髪のレイヤーでほぼ隠れる。

出力:
  tools/comfy/layers/char_body.png … 髪を除いた立ち絵（穴は塞いである）
  tools/comfy/layers/char_hair.png … 髪だけ
"""
import io
import json
import os

import cv2
import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, 'layers')


def hair_mask(rgb):
    """桃色の髪を拾う。肌（赤が強く青が沈む）は外す。"""
    R, G, B = rgb[:, :, 0].astype(int), rgb[:, :, 1].astype(int), rgb[:, :, 2].astype(int)
    hair = (R > 150) & (R >= B) & (B > G) & ((R - G) > 28) & ((B - G) > 6)
    skin = (R > 190) & (G > B) & ((R - B) > 25)
    hair = hair & ~skin
    lab, n = ndimage.label(hair)
    if n:
        sz = ndimage.sum(hair, lab, range(1, n + 1))
        hair = np.isin(lab, np.where(sz > 300)[0] + 1)
    # 穴を埋めてから少し太らせる（髪の縁の柔らかいところまで含める）
    hair = ndimage.binary_closing(hair, np.ones((5, 5)))
    return ndimage.binary_dilation(hair, iterations=2)


def main():
    ch = Image.open(os.path.join(OUT, 'char.png')).convert('RGBA')
    a = np.array(ch)
    rgb, alpha = a[:, :, :3], a[:, :, 3]
    body = alpha > 8

    hair = hair_mask(rgb) & body
    print('立ち絵のうち髪は %.1f%%' % (100 * hair.sum() / max(1, body.sum())))

    # --- 髪だけのレイヤー ---
    hair_layer = a.copy()
    hair_layer[:, :, 3] = np.where(hair, alpha, 0)
    Image.fromarray(hair_layer).save(os.path.join(OUT, 'char_hair.png'))

    # --- 髪を抜いた立ち絵。穴は周りの色を伸ばして塞ぐ ---
    # 体の外（空を背にした長い髪）は塞がず透かす。背景が見えるのが正しい
    inside = ndimage.binary_erosion(body, iterations=3)
    fill = hair & inside                      # 顔や鎧に掛かっている髪だけ塞ぐ
    drop = hair & ~inside                     # 体の外へ流れている髪は透かす

    src = cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR)
    filled = cv2.inpaint(src, (fill * 255).astype(np.uint8), 6, cv2.INPAINT_TELEA)
    out = a.copy()
    out[:, :, :3] = cv2.cvtColor(filled, cv2.COLOR_BGR2RGB)
    out[:, :, 3] = np.where(drop, 0, alpha)
    Image.fromarray(out).save(os.path.join(OUT, 'char_body.png'))

    with io.open(os.path.join(OUT, 'hair.json'), 'w', encoding='utf-8') as f:
        json.dump({'w': ch.width, 'h': ch.height,
                   'hairRatio': round(float(hair.sum()) / max(1, body.sum()), 4)},
                  f, ensure_ascii=False, indent=2)
    print('できました: char_body.png / char_hair.png')


if __name__ == '__main__':
    main()
