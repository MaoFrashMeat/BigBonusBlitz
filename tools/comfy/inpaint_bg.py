"""キャラを消した背景を作る（Live2D 風の下ごしらえ・2 段目）。

    py -3 tools/comfy/inpaint_bg.py

cut_character.py が作ったマスクを広げて、その中を描き足す。
キャラが動くと背後が見えるので、背景は「キャラのいない絵」が要る。
出力: tools/comfy/layers/bg.png
"""
import io
import json
import os
import shutil
import time
import urllib.request
import uuid

import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, 'layers')
CONFIG = os.path.join(HERE, 'config.json')

POSITIVE = ("fantasy castle town, blue sky, white clouds, waterfalls, lake, "
            "stone balustrade, distant mountains, cherry blossom petals, "
            "no humans, no character, "
            "masterpiece, high score, great score, absurdres")
NEGATIVE = ("1girl, person, human, character, armor, sword, hand, face, hair, "
            "lowres, worst quality, low quality, blurry, text, watermark")


def cfg():
    with io.open(CONFIG, encoding='utf-8') as f:
        return json.load(f)


def main():
    c = cfg()
    src = Image.open(os.path.join(HERE, '..', '..', 'assets', 'Title.png')).convert('RGB')
    m = Image.open(os.path.join(OUT, 'mask.png')).convert('L')
    if m.size != src.size:
        m = m.resize(src.size, Image.LANCZOS)
    # キャラの外側も少し塗り替える。境目に元の輪郭が残ると、
    # キャラを動かしたとき「影」のように見えてしまう
    grown = m.filter(ImageFilter.MaxFilter(21)).filter(ImageFilter.GaussianBlur(6))
    grown.save(os.path.join(c['comfy_input'], 'bbb_bg_mask.png'))
    src.save(os.path.join(c['comfy_input'], 'bbb_bg_src.png'))

    wf = {
        "1": {"class_type": "CheckpointLoaderSimple",
              "inputs": {"ckpt_name": "animagine-xl-4.0.safetensors"}},
        "2": {"class_type": "CLIPTextEncode", "inputs": {"text": POSITIVE, "clip": ["1", 1]}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"text": NEGATIVE, "clip": ["1", 1]}},
        "4": {"class_type": "LoadImage", "inputs": {"image": "bbb_bg_src.png", "upload": "image"}},
        "5": {"class_type": "LoadImage", "inputs": {"image": "bbb_bg_mask.png", "upload": "image"}},
        "6": {"class_type": "ImageToMask", "inputs": {"image": ["5", 0], "channel": "red"}},
        "7": {"class_type": "VAEEncodeForInpaint",
              "inputs": {"pixels": ["4", 0], "vae": ["1", 2], "mask": ["6", 0],
                         "grow_mask_by": 12}},
        "8": {"class_type": "KSampler",
              "inputs": {"seed": 720720, "steps": 30, "cfg": 6.0,
                         "sampler_name": "dpmpp_2m", "scheduler": "karras", "denoise": 1.0,
                         "model": ["1", 0], "positive": ["2", 0], "negative": ["3", 0],
                         "latent_image": ["7", 0]}},
        "9": {"class_type": "VAEDecode", "inputs": {"samples": ["8", 0], "vae": ["1", 2]}},
        "10": {"class_type": "SaveImage",
               "inputs": {"filename_prefix": "bbb/bg", "images": ["9", 0]}},
    }
    cid = str(uuid.uuid4())
    data = json.dumps({'prompt': wf, 'client_id': cid}).encode('utf-8')
    req = urllib.request.Request('http://%s/prompt' % c['server'], data=data,
                                 headers={'Content-Type': 'application/json'})
    pid = json.loads(urllib.request.urlopen(req, timeout=60).read())['prompt_id']
    print('背景を描き足しています...')
    for _ in range(900):
        time.sleep(1)
        h = json.loads(urllib.request.urlopen(
            'http://%s/history/%s' % (c['server'], pid), timeout=30).read())
        if pid not in h:
            continue
        st = h[pid].get('status', {})
        if st.get('status_str') == 'error':
            for msg in st.get('messages', []):
                if 'error' in str(msg).lower():
                    print(str(msg)[:600])
            return
        for out in h[pid]['outputs'].values():
            for im in out.get('images', []):
                s = os.path.join(c['comfy_output'], im.get('subfolder', ''), im['filename'])
                d = os.path.join(OUT, 'bg.png')
                shutil.copy2(s, d)
                print('できました:', d)
                return
    print('時間切れ')


if __name__ == '__main__':
    main()
