# 各クリップを mp4 にし、題名を入れて 2 回ずつつないだ一本（reel.mp4）も作る
# 使い方: py -3 tools/fx-lab/reel.py <作業フォルダ>   （<作業フォルダ>/fx/<clip>/f_0000.png … を読む）
import glob
import os
import shutil
import subprocess
import sys
from PIL import Image, ImageDraw, ImageFont

CLIPS = [
    ('slash1', '斬撃1  一文字'),
    ('slash2', '斬撃2  袈裟斬り（実写寄りの火花入り）'),
    ('slash3', '斬撃3  十字斬り'),
    ('slash4', '斬撃4  回転斬り'),
    ('slash5', '斬撃5  乱舞'),
    ('shake', '画面の揺れ（弱い一撃 → 強い一撃）'),
    ('lines', '流線 → 集中線'),
    ('nega', 'ネガ反転（点滅型 → 白黒で溜める型）'),
    ('coins', 'コインが飛び散る'),
    ('heal', '回復のオーラ'),
    ('attack', '攻撃のオーラ'),
    ('shield', 'シールドのオーラ'),
]
work = sys.argv[1]
FF = 'ffmpeg'
font = ImageFont.truetype('C:/Windows/Fonts/meiryob.ttc', 30)
os.makedirs(os.path.join(work, 'mp4'), exist_ok=True)
reel_dir = os.path.join(work, 'reel')
shutil.rmtree(reel_dir, ignore_errors=True)
os.makedirs(reel_dir)


def encode(pattern, dst, crf):
    subprocess.run([FF, '-y', '-loglevel', 'error', '-framerate', '60', '-i', pattern, '-c:v', 'libx264',
                    '-pix_fmt', 'yuv420p', '-crf', str(crf), '-movflags', '+faststart', dst], check=True)


n = 0
for name, title in CLIPS:
    src = os.path.join(work, 'fx', name)
    frames = sorted(glob.glob(os.path.join(src, 'f_*.png')))
    if not frames:
        continue
    encode(os.path.join(src, 'f_%04d.png'), os.path.join(work, 'mp4', name + '.mp4'), 17)
    for rep in range(2):
        for f in frames:
            im = Image.open(f).convert('RGB')
            d = ImageDraw.Draw(im, 'RGBA')
            tw = d.textlength(title, font=font)
            d.rounded_rectangle((20, 18, 20 + tw + 28, 68), 10, fill=(0, 0, 0, 150))
            d.text((34, 24), title, font=font, fill=(255, 255, 255))
            im.save(os.path.join(reel_dir, f'r_{n:05d}.png'))
            n += 1
encode(os.path.join(reel_dir, 'r_%05d.png'), os.path.join(work, 'reel.mp4'), 20)
print('reel frames', n, '->', os.path.join(work, 'reel.mp4'))
