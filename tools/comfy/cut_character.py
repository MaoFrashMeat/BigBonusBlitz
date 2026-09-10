"""タイトル絵からキャラを切り抜く（Live2D 風に動かすための下ごしらえ・1 段目）。

    py -3 tools/comfy/cut_character.py

ComfyUI の BiRefNet（Kay_BiRefNet_Loader + Remove_BG）に投げて、
  tools/comfy/layers/char.png  … 背景を抜いたキャラ（RGBA）
  tools/comfy/layers/mask.png  … そのマスク（白＝キャラ）
を書き出す。背景の穴埋めは次の段（inpaint_bg.py）で行う。
"""
import io
import json
import os
import shutil
import time
import urllib.request
import uuid

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, 'layers')
CONFIG = os.path.join(HERE, 'config.json')
SRC = os.path.join(HERE, '..', '..', 'assets', 'Title.png')


def cfg():
    with io.open(CONFIG, encoding='utf-8') as f:
        return json.load(f)


def run(server, prompt, out_names):
    cid = str(uuid.uuid4())
    data = json.dumps({'prompt': prompt, 'client_id': cid}).encode('utf-8')
    req = urllib.request.Request('http://%s/prompt' % server, data=data,
                                 headers={'Content-Type': 'application/json'})
    pid = json.loads(urllib.request.urlopen(req, timeout=60).read())['prompt_id']
    for _ in range(900):
        time.sleep(1)
        h = json.loads(urllib.request.urlopen(
            'http://%s/history/%s' % (server, pid), timeout=30).read())
        if pid not in h:
            continue
        got = []
        for node, out in h[pid]['outputs'].items():
            for im in out.get('images', []):
                got.append((node, im))
        return sorted(got, key=lambda t: int(t[0]))
    raise RuntimeError('時間切れ。ComfyUI の様子を見てほしい')


def main():
    c = cfg()
    os.makedirs(OUT, exist_ok=True)
    shutil.copy2(SRC, os.path.join(c['comfy_input'], 'bbb_title_src.png'))

    wf = {
        "1": {"class_type": "LoadImage",
              "inputs": {"image": "bbb_title_src.png", "upload": "image"}},
        # 人物を抜く。Kay の BiRefNet は fp16 と fp32 が噛み合わず落ちるので、
        # dtype を指定できる StarLucidaRMBG（BiRefNet 系）を使う
        "2": {"class_type": "StarLucidaRMBG",
              "inputs": {"image": ["1", 0], "refine": True, "decontaminate": True,
                         "dtype": "fp32", "device": "cuda"}},
        "3": {"class_type": "SaveImage",
              "inputs": {"filename_prefix": "bbb/char", "images": ["2", 0]}},
        "4": {"class_type": "MaskToImage", "inputs": {"mask": ["2", 1]}},
        "5": {"class_type": "SaveImage",
              "inputs": {"filename_prefix": "bbb/mask", "images": ["4", 0]}},
    }
    print('キャラを切り抜いています...')
    got = run(c['server'], wf, 2)
    names = {'3': 'char.png', '5': 'mask.png'}
    for node, im in got:
        dst = names.get(node)
        if not dst:
            continue
        src = os.path.join(c['comfy_output'], im.get('subfolder', ''), im['filename'])
        shutil.copy2(src, os.path.join(OUT, dst))
        print('  ', dst)
    print('できました:', OUT)


if __name__ == '__main__':
    main()
