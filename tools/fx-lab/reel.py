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
    ('death_dissolve', '倒れる1  溶けて消える'),
    ('death_ash', '倒れる2  灰になって崩れる'),
    ('death_burn', '倒れる3  燃え尽きる'),
    ('death_holy', '倒れる4  昇天'),
    ('death_shatter', '倒れる5  砕け散る'),
    ('death_slice', '倒れる6  真っ二つ'),
    ('stepup', 'ステップアップ（画面の枠: 白 → 青 → 黄 → 緑 → 赤 → 虹）'),
    ('fire_frame', '炎の枠（本物寄り）'),
    ('p01_flare', '部品 01  閃光（四芒星）'),
    ('p02_burst', '部品 02  放射の爆ぜ'),
    ('p03_hitlines', '部品 03  衝撃線（連番）'),
    ('p04_bighit', '部品 04  爆ぜ（連番）'),
    ('p05_starexp', '部品 05  星の爆発（連番）'),
    ('p06_ring', '部品 06  衝撃波の輪'),
    ('p07_groundring', '部品 07  地面の輪＋砂煙'),
    ('p08_shock', '部品 08  空間の歪み'),
    ('p09_firering', '部品 09  炎の輪（連番）'),
    ('p10_elecring', '部品 10  電気の輪（連番）'),
    ('p11_sparks', '部品 11  火花（実写・跳ねる）'),
    ('p12_shards', '部品 12  破片（アニメ）'),
    ('p13_sparkle', '部品 13  きらめき'),
    ('p14_motes', '部品 14  光の粒が昇る'),
    ('p15_slash', '部品 15  斬撃（繊維）'),
    ('p16_cutline', '部品 16  斬線'),
    ('p17_trail', '部品 17  軌跡のリボン'),
    ('p18_flame', '部品 18  炎（焚き火）'),
    ('p19_embers', '部品 19  火の粉'),
    ('p20_smoke', '部品 20  煙'),
    ('p21_dust', '部品 21  砂煙と瓦礫'),
    ('p22_bolt', '部品 22  稲妻'),
    ('p23_arcs', '部品 23  電気のスパーク'),
    ('p24_charge', '部品 24  溜め'),
    ('p25_vortex', '部品 25  渦'),
    ('p26_magic', '部品 26  魔法陣'),
    ('p27_pillar', '部品 27  光の柱'),
    ('p28_aura', '部品 28  揺らめくオーラ'),
    ('p29_shield', '部品 29  シールド'),
    ('p30_dissolve', '部品 30  ディゾルブ（光る縁）'),
]
# 2 つ目の引数で絞る（run.ps1 -Only と同じ名前をカンマ区切り）
if len(sys.argv) > 2 and sys.argv[2]:
    only = sys.argv[2].split(',')
    CLIPS = [c for c in CLIPS if c[0] in only]
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
