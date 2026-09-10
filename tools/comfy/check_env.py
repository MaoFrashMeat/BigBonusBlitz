# このPCでスプライト生成ができるかを調べる。
# 使い方: python tools/comfy/check_env.py
#
# ComfyUI の場所を自動で探し、必要なモデルとノードが揃っているかを一覧にする。
# 見つかったら config.json を書き出すので、そのあとは run_batch.py がそのまま動く。
import glob
import json
import os
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
CONFIG = os.path.join(HERE, 'config.json')

# ComfyUI がありそうな場所。ここに無ければ手で聞く
CANDIDATES = [
    r'D:\Mao-PC\ComfyUI\ComfyUI_windows_portable',
    r'F:\ComfyUI\ComfyUI_windows_portable',
    r'D:\ComfyUI\ComfyUI_windows_portable',
    r'C:\ComfyUI\ComfyUI_windows_portable',
    r'E:\ComfyUI\ComfyUI_windows_portable',
    r'D:\ComfyUI_windows_portable',
    r'D:\EasyWan\ComfyUI',
    r'D:\Mao-PC\ComfyUI\ComfyUI_windows_portable',
]

NEED_MODELS = [
    ('checkpoints', ['Illustrious-XL-v2.0.safetensors', 'waiNSFWIllustrious_v60.safetensors',
                     'JANKUTrainedChenkinNoobai_v60.safetensors'],
     '本体モデル（どれか 1 つ）'),
    ('controlnet', ['OpenPoseXL2.safetensors'], 'ポーズ固定'),
    ('ipadapter', ['ip-adapter-plus_sdxl_vit-h.safetensors'], 'キャラ固定'),
    ('clip_vision', ['CLIP-ViT-H-14-laion2B-s32B-b79K.safetensors'], 'IPAdapter が使う画像理解'),
]
NEED_NODES = [('comfyui_ipadapter_plus', 'IPAdapter のノード')]

OK, NG, WARN = '  [OK]', '  [NG]', '  [--]'

def model_count(root):
    """その ComfyUI が持っているモデルの数。空の入れ物を掴まないための目安。"""
    models = os.path.join(root, 'models')
    if not os.path.isdir(models):
        return -1
    n = 0
    for d in ('checkpoints', 'unet', 'diffusion_models', 'controlnet', 'ipadapter', 'clip_vision', 'loras'):
        p = os.path.join(models, d)
        if not os.path.isdir(p):
            continue
        n += len([x for x in os.listdir(p) if x.endswith(('.safetensors', '.ckpt', '.gguf', '.pth', '.bin'))])
    return n


def find_comfy(verbose=False):
    """
    ComfyUI の本体フォルダを探す。**見つかった順ではなく、モデルが多い方を選ぶ。**
    空の ComfyUI が先に見つかると、本体があるのに「モデルが無い」と誤診するため。
    """
    found = {}
    for base in CANDIDATES:
        for cand in (os.path.join(base, 'ComfyUI'), base):
            if os.path.isdir(os.path.join(cand, 'models')) and os.path.isdir(os.path.join(cand, 'custom_nodes')):
                found[os.path.normpath(cand)] = model_count(cand)
    # ドライブ直下もざっと見る（2 階層まで）
    for drive in 'CDEFG':
        root = drive + ':\\'
        if not os.path.isdir(root):
            continue
        for m in glob.glob(root + '*/ComfyUI*/ComfyUI/models') + glob.glob(root + '*/ComfyUI/models'):
            cand = os.path.dirname(m)
            if os.path.isdir(os.path.join(cand, 'custom_nodes')):
                found.setdefault(os.path.normpath(cand), model_count(cand))
    if not found:
        return None
    if verbose and len(found) > 1:
        print('  この PC で見つかった ComfyUI:')
        for k, v in sorted(found.items(), key=lambda x: -x[1]):
            print('    %-58s モデル %d 個' % (k, v))
    return max(found.items(), key=lambda x: x[1])[0]

def main():
    print('=== スプライト生成の環境チェック ===\n')
    ng = 0

    print('Python と画像ライブラリ')
    print(f'{OK} Python {sys.version.split()[0]}')
    try:
        import PIL
        print(f'{OK} Pillow {PIL.__version__}')
    except ImportError:
        print(f'{NG} Pillow が無い  →  pip install pillow')
        ng += 1
    try:
        import rembg  # noqa: F401
        print(f'{OK} rembg（背景除去の精度が上がる）')
    except ImportError:
        print(f'{WARN} rembg は無い（白背景なら無くても動く）  →  pip install rembg')

    print('\nComfyUI')
    comfy = find_comfy(verbose=True)
    if not comfy:
        print(f'{NG} 見つからない。config.json に手で書いてください')
        print('       例: "comfy_input": "D:\\\\ComfyUI\\\\ComfyUI_windows_portable\\\\ComfyUI\\\\input"')
        return
    print(f'{OK} {comfy}')

    models = os.path.join(comfy, 'models')
    print('\nモデル')
    for folder, names, label in NEED_MODELS:
        d = os.path.join(models, folder)
        have = [n for n in names if os.path.exists(os.path.join(d, n))]
        if have:
            print(f'{OK} {label}: {have[0]}')
        else:
            got = [f for f in os.listdir(d)[:40] if f.endswith(('.safetensors', '.pth', '.ckpt'))] if os.path.isdir(d) else []
            print(f'{NG} {label} が無い（{folder}/）')
            if got:
                print(f'       ある物: {", ".join(got[:5])}')
            ng += 1

    print('\nカスタムノード')
    cn = os.path.join(comfy, 'custom_nodes')
    for name, label in NEED_NODES:
        if os.path.isdir(os.path.join(cn, name)):
            print(f'{OK} {label}')
        else:
            print(f'{NG} {label} が無い  →  ComfyUI Manager から "IPAdapter plus" を入れる')
            ng += 1

    print('\n素材')
    for f, label in (('salia_ref.png', '参照画像'), ('poses/index.json', '骨格')):
        p = os.path.join(HERE, f)
        print(f'{OK if os.path.exists(p) else NG} {label}: {f}')
        if not os.path.exists(p):
            ng += 1
            if f.startswith('poses'):
                print('       →  python tools/comfy/gen_pose_skeletons.py')

    # config.json を書く（見つかったパスで上書き）
    cfg = {
        'server': '127.0.0.1:8188',
        'comfy_input': os.path.join(comfy, 'input'),
        'comfy_output': os.path.join(comfy, 'output'),
        'ref_image': 'salia_ref.png',
    }
    if os.path.exists(CONFIG):
        try:
            with open(CONFIG, encoding='utf-8') as f:
                old = json.load(f)
            cfg['server'] = old.get('server', cfg['server'])
        except Exception:
            pass          # 壊れていたら作り直す
    with open(CONFIG, 'w', encoding='utf-8') as f:
        json.dump(cfg, f, ensure_ascii=False, indent=2)
    print(f'\nconfig.json を書きました（input: {cfg["comfy_input"]}）')

    print('\nGPU')
    if shutil.which('nvidia-smi'):
        try:
            out = subprocess.check_output(
                ['nvidia-smi', '--query-gpu=name,memory.total,memory.used', '--format=csv,noheader'],
                text=True, timeout=10).strip()
            print(f'{OK} {out}')
            mb = int(out.split(',')[1].strip().split()[0])
            if mb < 10000:
                print(f'{WARN} VRAM が 10GB 未満。ControlNet と IPAdapter を同時に使うと厳しい')
                print('       run_nvidia_gpu.bat に --lowvram を付けるか、別の PC で回す')
        except Exception as e:
            print(f'{WARN} nvidia-smi が答えない: {e}')
    else:
        print(f'{WARN} nvidia-smi が無い。GPU が無いか、パスが通っていない')

    print('\n' + ('準備できています。ComfyUI を起動して run_batch.py を実行してください'
                  if ng == 0 else f'{ng} 件足りません。上の [NG] を埋めてください'))

if __name__ == '__main__':
    main()
