# ComfyUI に全ポーズを投げて、1 コマずつ生成する。
# 使い方（ComfyUI を起動した状態で）:
#   python tools/comfy/run_batch.py                 # 全部
#   python tools/comfy/run_batch.py --only attack   # 1 アクションだけ
#   python tools/comfy/run_batch.py --seed 12345    # 種を固定して作り直す
#
# 生成物は ComfyUI 側の output に出るので、--pull を付けると raw/ へ回収する。
import argparse
import json
import os
import shutil
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid

HERE = os.path.dirname(os.path.abspath(__file__))
POSES = os.path.join(HERE, 'poses')
RAW = os.path.join(HERE, 'raw')
WORKFLOW = os.path.join(HERE, 'workflow_popora.json')
CONFIG = os.path.join(HERE, 'config.json')

# --- 見た目を決める言葉。ここを直すと全コマに効く ---
STYLE = (
    "masterpiece, best quality, "
    "1girl, solo, full body, whole body visible from head to feet, "
    "front view, facing viewer, "
    # 参照画像と同じ意匠。ここを崩すと別人になる
    "short pink hair, blue eyes, cheerful, "
    "ornate white plate armor with (gold filigree trim:1.45), navy blue underlayer, "
    "pauldrons with cyan gems, (crescent moon emblem on skirt:1.2), white gloves, "
    "white frilled skirt with navy panel, brown leather belt, "
    "white knee-high armored boots with gold wing motif, "
    # 剣。crystalline と書くと青い塊になるので、素直な剣にする
    "(holding exactly one sword:1.1), silver straight blade with cross guard and blue hilt, "
    "flat cel shading, clean thick outline, game character sprite, "
    "(pure white background:1.3), simple background, no shadow on background"
)
NEGATIVE = (
    "worst quality, low quality, blurry, jpeg artifacts, "
    "character sheet, multiple views, turnaround, reference sheet, "
    "multiple characters, text, watermark, signature, "
    "cropped, out of frame, cut off, close-up, portrait, upper body, headshot, "
    # 前回これが出た: 頭の羽根、巨大な結晶の塊、体が隠れるほどの武器
    "head wings, horns, headgear, crystal, glowing aura, "
    "oversized weapon covering the body, floating weapon, detached cape, "
    "large flowing cloth, floating fabric, banner, flag, ribbon, cape, cloak, "
    "(two swords:1.3), dual wielding, extra weapon, giant sword, "
    "extra arms, extra legs, missing hands, shapeless boots, "
    "angel wings, feathered wings, halo, "
    "realistic, 3d render, photo, "
    "complex background, scenery, gradient background, dark background, "
    "beige background, cream background, colored background, vignette, "
    "wooden club, plank, staff, axe, blunt weapon"
)

def load_config():
    """ComfyUI の場所。config.json があればそれを使う。"""
    cfg = {
        'server': '127.0.0.1:8188',
        'comfy_input': r'F:\ComfyUI\ComfyUI_windows_portable\ComfyUI\input',
        'comfy_output': r'F:\ComfyUI\ComfyUI_windows_portable\ComfyUI\output',
        'ref_image': 'popora_ref.png',
    }
    if os.path.exists(CONFIG):
        with open(CONFIG, encoding='utf-8') as f:
            cfg.update(json.load(f))
    return cfg

def post(server, prompt, client_id):
    data = json.dumps({'prompt': prompt, 'client_id': client_id}).encode('utf-8')
    req = urllib.request.Request(f'http://{server}/prompt', data=data,
                                 headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.loads(r.read())

def history(server, pid):
    with urllib.request.urlopen(f'http://{server}/history/{pid}', timeout=30) as r:
        return json.loads(r.read())

def wait(server, pid, timeout=600):
    """終わるまで待って、出力ファイル名を返す。"""
    t0 = time.time()
    while time.time() - t0 < timeout:
        h = history(server, pid)
        if pid in h:
            out = []
            for node in h[pid].get('outputs', {}).values():
                for im in node.get('images', []):
                    out.append(im)
            return out
        time.sleep(1.5)
    raise TimeoutError('生成が終わりませんでした: ' + pid)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--only', help='このアクションだけ（例: attack）')
    ap.add_argument('--seed', type=int, default=771113,
                    help='全コマで同じ種を使う。0 を渡すと毎回ランダム（別人になりやすい）')
    ap.add_argument('--frame', type=int, help='このコマ番号だけ（--only と併用）')
    ap.add_argument('--pull', action='store_true', help='生成物を raw/ へ回収する')
    args = ap.parse_args()

    cfg = load_config()
    server = cfg['server']
    with open(WORKFLOW, encoding='utf-8') as f:
        base = json.load(f)
    with open(os.path.join(POSES, 'index.json'), encoding='utf-8') as f:
        index = json.load(f)

    # 骨格と参照画像を ComfyUI の input へ置く（LoadImage はそこしか見ない）
    inp = cfg['comfy_input']
    if not os.path.isdir(inp):
        print('ComfyUI の input が見つかりません:', inp)
        print('tools/comfy/config.json に正しいパスを書いてください')
        return
    for it in index:
        shutil.copy2(os.path.join(POSES, it['file']), os.path.join(inp, 'pose_' + it['file']))
    ref_src = os.path.join(HERE, cfg['ref_image'])
    if not os.path.exists(ref_src):
        print('参照画像がありません:', ref_src)
        return
    shutil.copy2(ref_src, os.path.join(inp, 'popora_ref.png'))

    os.makedirs(RAW, exist_ok=True)
    client_id = str(uuid.uuid4())
    todo = [it for it in index if not args.only or it['action'] == args.only]
    if args.frame is not None:
        todo = [it for it in todo if it['frame'] == args.frame]
    print(f'{len(todo)} コマを生成します')

    for n, it in enumerate(todo, 1):
        prompt = json.loads(json.dumps(base))
        prompt.pop('_comment', None)
        positive = STYLE + ', ' + it['hint']
        prompt['2']['inputs']['text'] = positive
        prompt['3']['inputs']['text'] = NEGATIVE
        prompt['4']['inputs']['image'] = 'pose_' + it['file']
        prompt['7']['inputs']['image'] = 'popora_ref.png'
        prompt['13']['inputs']['filename_prefix'] = f"popora/{it['action']}_{it['frame']}"
        prompt['11']['inputs']['seed'] = args.seed if args.seed else int(time.time() * 1000) % 2**31

        try:
            res = post(server, prompt, client_id)
        except urllib.error.URLError as e:
            print('ComfyUI につながりません:', e)
            print(f'  {server} で起動しているか確認してください')
            return
        pid = res['prompt_id']
        imgs = wait(server, pid)
        print(f'  [{n}/{len(todo)}] {it["action"]}_{it["frame"]} -> ' + ', '.join(i['filename'] for i in imgs))

        if args.pull:
            for im in imgs:
                src = os.path.join(cfg['comfy_output'], im.get('subfolder', ''), im['filename'])
                if os.path.exists(src):
                    shutil.copy2(src, os.path.join(RAW, f"{it['action']}_{it['frame']}.png"))

    print('完了。次は postprocess.py でドット化とシート化を行います')

if __name__ == '__main__':
    main()
