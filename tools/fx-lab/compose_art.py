# 見本の舞台を作る: ゲームの背景レイヤーを 1280x720 に重ね、主人公とゴブリンを切り出す
# 使い方: py -3 tools/fx-lab/compose_art.py <出力フォルダ>
import os
import sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, '..', '..', 'UnityProject', 'BigBonusBlitz', 'Assets', 'Resources', 'Art')
out = sys.argv[1]
os.makedirs(out, exist_ok=True)
N = Image.NEAREST


def load(rel):
    return Image.open(os.path.join(ART, rel)).convert('RGBA')


bg = load('Backgrounds/bg_layer6_sky.png').resize((1280, 720), Image.BILINEAR)


def put(im, scale, bottom):
    im = im.resize((round(im.width * scale), round(im.height * scale)), N)
    x = 0
    while x < 1280:
        bg.paste(im, (x, bottom - im.height), im)
        x += im.width


put(load('Backgrounds/layer5_mountains_transparent.png').crop((0, 231, 1024, 768)), 1.25, 400)
put(load('Backgrounds/bg_layer4_distant.png'), 1.25, 470)
put(load('Backgrounds/bg_layer3_woods.png'), 0.62, 560)
ground = load('Backgrounds/bg_ground.png').crop((0, 719, 1024, 1024)).resize((1280, 381), N)
bg.paste(ground, (0, 720 - 381 + 40), ground)
bg.convert('RGB').save(os.path.join(out, 'bg.png'))

# 主人公（立ちの 1 コマ目）と敵。周りに余白を足す（オーラのシェーダーが縁の外を読むため）
hero = load('Characters/chr0001_idle_strip.png').crop((0, 0, 1024, 1024)).crop((110 - 60, 33 - 60, 816 + 60, 970 + 60))
hero.save(os.path.join(out, 'hero.png'))
gob = load('Enemies/goblin.png')
pad = Image.new('RGBA', (gob.width + 80, gob.height + 80))
pad.alpha_composite(gob, (40, 40))
pad.save(os.path.join(out, 'goblin.png'))
print('art ->', out)
