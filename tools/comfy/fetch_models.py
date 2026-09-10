"""スプライト生成に要るモデルを ComfyUI へ落とす。

    py -3 tools/comfy/fetch_models.py            # 足りないものだけ落とす
    py -3 tools/comfy/fetch_models.py --check    # 落とさずに確認だけ

置き場所は config.json の comfy_output から逆算する（ComfyUI 本体の models/）。
途中で止めても、次に走らせれば続きから落とす。
"""
import argparse
import io
import json
import os
import sys
import time
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
CONFIG = os.path.join(HERE, 'config.json')

# (置き場所, 保存名, URL, おおよその大きさ)
MODELS = [
    ('checkpoints', 'Illustrious-XL-v2.0.safetensors',
     'https://huggingface.co/OnomaAIResearch/Illustrious-XL-v2.0/resolve/main/Illustrious-XL-v2.0.safetensors',
     '本体モデル'),
    ('controlnet', 'OpenPoseXL2.safetensors',
     'https://huggingface.co/thibaud/controlnet-openpose-sdxl-1.0/resolve/main/OpenPoseXL2.safetensors',
     'ポーズ固定'),
    ('ipadapter', 'ip-adapter-plus_sdxl_vit-h.safetensors',
     'https://huggingface.co/h94/IP-Adapter/resolve/main/sdxl_models/ip-adapter-plus_sdxl_vit-h.safetensors',
     'キャラ固定'),
    ('clip_vision', 'CLIP-ViT-H-14-laion2B-s32B-b79K.safetensors',
     'https://huggingface.co/h94/IP-Adapter/resolve/main/models/image_encoder/model.safetensors',
     '画像理解'),
]


def comfy_models_dir():
    if not os.path.exists(CONFIG):
        print('config.json が無い。先に py -3 tools/comfy/check_env.py を走らせる')
        sys.exit(1)
    cfg = json.load(io.open(CONFIG, encoding='utf-8'))
    root = os.path.dirname(cfg['comfy_output'])   # .../ComfyUI/output → .../ComfyUI
    models = os.path.join(root, 'models')
    if not os.path.isdir(models):
        print('models フォルダが見つからない: ' + models)
        sys.exit(1)
    return models


def human(n):
    return '%.2f GB' % (n / 1073741824.0)


def fetch(url, dst, label):
    """途中から再開できる形で落とす。"""
    tmp = dst + '.part'
    done = os.path.getsize(tmp) if os.path.exists(tmp) else 0
    req = urllib.request.Request(url, headers={'User-Agent': 'bbb-fetch/1.0'})
    if done:
        req.add_header('Range', 'bytes=%d-' % done)
    try:
        r = urllib.request.urlopen(req, timeout=60)
    except Exception as e:
        print('    取得できない: %s' % e)
        return False
    total = int(r.headers.get('content-length', 0)) + done
    mode = 'ab' if done and r.status == 206 else 'wb'
    if mode == 'wb':
        done = 0
    t0, last = time.time(), 0
    with io.open(tmp, mode) as f:
        while True:
            chunk = r.read(1024 * 1024)
            if not chunk:
                break
            f.write(chunk)
            done += len(chunk)
            now = time.time()
            if now - last > 3:
                last = now
                sec = max(0.1, now - t0)
                pct = 100.0 * done / total if total else 0
                print('      %s / %s  (%.0f%%)  %.1f MB/s' %
                      (human(done), human(total), pct, done / sec / 1048576), flush=True)
    r.close()
    if total and abs(os.path.getsize(tmp) - total) > 1024:
        print('    大きさが合わない。次に走らせると続きから落とす')
        return False
    os.replace(tmp, dst)
    return True


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--check', action='store_true', help='落とさずに確認だけ')
    args = ap.parse_args()

    models = comfy_models_dir()
    print('置き場所: ' + models)
    print()

    todo = []
    for sub, name, url, label in MODELS:
        dst = os.path.join(models, sub, name)
        if os.path.exists(dst):
            print('  [済] %-10s %-46s %s' % (label, name, human(os.path.getsize(dst))))
        else:
            part = dst + '.part'
            note = '（途中まで %s）' % human(os.path.getsize(part)) if os.path.exists(part) else ''
            print('  [要] %-10s %-46s %s' % (label, name, note))
            todo.append((sub, name, url, label, dst))

    if not todo:
        print()
        print('全部そろっている')
        return 0
    if args.check:
        print()
        print('%d 件足りない。--check を外すと落とす' % len(todo))
        return 1

    print()
    for i, (sub, name, url, label, dst) in enumerate(todo, 1):
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        print('(%d/%d) %s : %s' % (i, len(todo), label, name), flush=True)
        if fetch(url, dst, label):
            print('      → 完了 %s' % human(os.path.getsize(dst)), flush=True)
        else:
            print('      → 未完了', flush=True)
    print()
    print('終わり。もう一度 py -3 tools/comfy/check_env.py で確認する')
    return 0


if __name__ == '__main__':
    sys.exit(main())
