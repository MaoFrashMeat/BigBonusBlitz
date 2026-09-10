# ControlNet OpenPose 用の骨格画像を、ポーズ定義から書き出す。
# 使い方: python tools/comfy/gen_pose_skeletons.py
# 出力: tools/comfy/poses/<action>_<frame>.png  （1024x1024）
#       tools/comfy/poses/index.json            （各コマの説明とプロンプト補助）
#
# ここで作る骨格が「どのコマでどんな姿勢か」の唯一の正になる。
# 生成した絵が思ったポーズにならないときは、まずこの骨格を直す。
import json
import math
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(__file__)
OUT = os.path.join(HERE, 'poses')
SIZE = 1024

# OpenPose（COCO 18 点）の並び。ControlNet はこの色と順番を前提にしている
#  0鼻 1首 2右肩 3右肘 4右手首 5左肩 6左肘 7左手首
#  8右腰 9右膝 10右足首 11左腰 12左膝 13左足首 14右目 15左目 16右耳 17左耳
COLORS = [
    (255, 0, 0), (255, 85, 0), (255, 170, 0), (255, 255, 0), (170, 255, 0),
    (85, 255, 0), (0, 255, 0), (0, 255, 85), (0, 255, 170), (0, 255, 255),
    (0, 170, 255), (0, 85, 255), (0, 0, 255), (85, 0, 255), (170, 0, 255),
    (255, 0, 255), (255, 0, 170), (255, 0, 85),
]
# 骨（0 始まりの点番号）と、その骨に使う色の番号
LIMBS = [
    (1, 2, 0), (1, 5, 1), (2, 3, 2), (3, 4, 3), (5, 6, 4), (6, 7, 5),
    (1, 8, 6), (8, 9, 7), (9, 10, 8), (1, 11, 9), (11, 12, 10), (12, 13, 11),
    (1, 0, 12), (0, 14, 13), (14, 16, 14), (0, 15, 15), (15, 17, 16),
]

# --------------------------------------------------------------------------
# 骨格の組み立て
#   体の中心を (0,0)、上が +y の座標で書き、最後に画像座標へ直す。
#   単位はドットではなく「頭の高さ」に近い相対値（1.0 ≒ 肩幅の半分）。
# --------------------------------------------------------------------------
def build(neck_y=1.55, lean=0.0, bob=0.0,
          r_arm=(-40, -95), l_arm=(-140, -95),   # 肩から見た角度（度）: 肘, 手首
          r_leg=(-95, -92), l_leg=(-85, -88),
          head_tilt=0.0, crouch=0.0):
    """
    角度で腕と脚を組む。角度は「下向き -90 度」を基準にした画面座標系。
    r_arm/l_arm は (肘へ向かう角度, 手首へ向かう角度)。
    """
    def polar(p, ang, ln):
        a = math.radians(ang)
        return (p[0] + math.cos(a) * ln, p[1] - math.sin(a) * ln)

    neck = (lean, neck_y + bob)
    nose = (lean + head_tilt * 0.10, neck_y + 0.42 + bob)
    r_sh = (lean - 0.34, neck_y - 0.05 + bob)
    l_sh = (lean + 0.34, neck_y - 0.05 + bob)
    r_hip = (-0.22, 0.62 - crouch + bob * 0.5)
    l_hip = (0.22, 0.62 - crouch + bob * 0.5)

    r_el = polar(r_sh, r_arm[0], 0.46)
    r_wr = polar(r_el, r_arm[1], 0.44)
    l_el = polar(l_sh, l_arm[0], 0.46)
    l_wr = polar(l_el, l_arm[1], 0.44)
    r_kn = polar(r_hip, r_leg[0], 0.56)
    r_an = polar(r_kn, r_leg[1], 0.54)
    l_kn = polar(l_hip, l_leg[0], 0.56)
    l_an = polar(l_kn, l_leg[1], 0.54)

    eye_dx, eye_y = 0.12, 0.50
    return [
        nose, neck, r_sh, r_el, r_wr, l_sh, l_el, l_wr,
        r_hip, r_kn, r_an, l_hip, l_kn, l_an,
        (nose[0] - eye_dx, neck_y + eye_y + bob), (nose[0] + eye_dx, neck_y + eye_y + bob),
        (nose[0] - 0.26, neck_y + 0.46 + bob), (nose[0] + 0.26, neck_y + 0.46 + bob),
    ]

def draw_pose(points, size=SIZE):
    """OpenPose 形式（黒地に色付きの骨と関節）で描く。"""
    img = Image.new('RGB', (size, size), (0, 0, 0))
    d = ImageDraw.Draw(img)
    cx, cy = size * 0.5, size * 0.5
    scale = size * 0.30                        # 全身が枠の 8 割ほどに収まる倍率
    mid_y = 0.75                               # 頭頂と足首の中間。ここを画面の中心に置く

    def to_px(p):
        return (cx + p[0] * scale, cy - (p[1] - mid_y) * scale)

    px = [to_px(p) for p in points]
    for a, b, ci in LIMBS:
        # OpenPose の骨は「太い楕円」で描かれる。近い見た目になるよう太い線で引く
        d.line([px[a], px[b]], fill=COLORS[ci], width=max(4, size // 100))
    for i, p in enumerate(px):
        r = max(3, size // 145)
        d.ellipse([p[0] - r, p[1] - r, p[0] + r, p[1] + r], fill=COLORS[i])
    return img

# --------------------------------------------------------------------------
# ポーズ表: gen_hero_sprites.py の動きと 1 対 1 で対応させる
#   arms/legs は (肘角, 手首角)。剣は右手（点 4）に持たせる前提でプロンプトに書く
# --------------------------------------------------------------------------
def P(**kw):
    return kw

POSES = {
    'idle': [
        P(bob=0.00, r_arm=(-62, -88), l_arm=(-118, -92)),
        P(bob=0.02, r_arm=(-60, -86), l_arm=(-120, -94)),
        P(bob=0.00, r_arm=(-62, -88), l_arm=(-118, -92)),
        P(bob=-0.02, r_arm=(-64, -90), l_arm=(-116, -90)),
    ],
    'walk': [
        P(bob=0.00, lean=-0.03, r_leg=(-112, -95), l_leg=(-68, -85), r_arm=(-50, -80), l_arm=(-130, -100)),
        P(bob=0.04, lean=0.00, r_leg=(-98, -92), l_leg=(-82, -88), r_arm=(-62, -88), l_arm=(-118, -92)),
        P(bob=0.02, lean=0.03, r_leg=(-70, -86), l_leg=(-110, -96), r_arm=(-76, -96), l_arm=(-104, -84)),
        P(bob=0.00, lean=0.03, r_leg=(-68, -85), l_leg=(-112, -95), r_arm=(-80, -98), l_arm=(-100, -82)),
        P(bob=0.04, lean=0.00, r_leg=(-82, -88), l_leg=(-98, -92), r_arm=(-62, -88), l_arm=(-118, -92)),
        P(bob=0.02, lean=-0.03, r_leg=(-110, -96), l_leg=(-70, -86), r_arm=(-52, -82), l_arm=(-128, -98)),
    ],
    'attack': [                      # 振りかぶり → 振り下ろし
        P(lean=-0.06, r_arm=(-20, 40), l_arm=(-150, 20), r_leg=(-105, -95), l_leg=(-72, -86)),
        P(lean=-0.10, bob=0.05, r_arm=(10, 70), l_arm=(-160, 45), r_leg=(-108, -96), l_leg=(-70, -85)),
        P(lean=0.08, bob=-0.03, r_arm=(-45, -20), l_arm=(-135, -30), r_leg=(-88, -90), l_leg=(-84, -88)),
        P(lean=0.12, bob=-0.06, r_arm=(-70, -60), l_arm=(-120, -70), r_leg=(-80, -88), l_leg=(-92, -92), crouch=0.06),
    ],
    'slash': [                       # 横薙ぎ
        P(lean=-0.10, r_arm=(-150, -170), l_arm=(-150, -160), r_leg=(-105, -95), l_leg=(-72, -86)),
        P(lean=-0.04, r_arm=(-110, -140), l_arm=(-140, -150), r_leg=(-98, -92), l_leg=(-80, -88)),
        P(lean=0.10, r_arm=(-30, -10), l_arm=(-120, -60), r_leg=(-84, -88), l_leg=(-92, -92)),
        P(lean=0.14, r_arm=(-10, 10), l_arm=(-110, -40), r_leg=(-80, -86), l_leg=(-96, -94)),
    ],
    'cast': [                        # 振り上げ → 地面に突き立て
        P(bob=0.06, r_arm=(30, 80), l_arm=(150, 100), r_leg=(-96, -92), l_leg=(-84, -88)),
        P(bob=0.12, r_arm=(50, 88), l_arm=(130, 92), r_leg=(-94, -92), l_leg=(-86, -90)),
        P(bob=-0.04, r_arm=(-40, -70), l_arm=(-140, -110), r_leg=(-92, -92), l_leg=(-88, -90), crouch=0.05),
        P(bob=-0.10, r_arm=(-70, -95), l_arm=(-110, -95), r_leg=(-90, -92), l_leg=(-90, -92), crouch=0.12),
    ],
    'guard': [                       # 剣を盾のように立てて構える
        P(lean=-0.05, r_arm=(-70, -20), l_arm=(-110, -30), r_leg=(-100, -94), l_leg=(-78, -86), crouch=0.04),
        P(lean=-0.05, bob=0.02, r_arm=(-72, -18), l_arm=(-108, -28), r_leg=(-100, -94), l_leg=(-78, -86), crouch=0.04),
    ],
    'hit': [                         # のけぞる
        P(lean=0.14, head_tilt=0.4, r_arm=(-40, -30), l_arm=(-150, -130), r_leg=(-76, -84), l_leg=(-104, -96)),
        P(lean=0.20, head_tilt=0.6, r_arm=(-30, -20), l_arm=(-158, -140), r_leg=(-72, -82), l_leg=(-108, -98), crouch=0.05),
    ],
    'victory': [                     # 剣を天に掲げる
        P(r_arm=(60, 88), l_arm=(120, 92), r_leg=(-96, -92), l_leg=(-84, -88)),
        P(bob=0.08, r_arm=(70, 90), l_arm=(110, 90), r_leg=(-100, -96), l_leg=(-80, -84)),
        P(bob=0.13, r_arm=(75, 90), l_arm=(105, 90), r_leg=(-104, -100), l_leg=(-76, -80)),
        P(bob=0.06, r_arm=(68, 89), l_arm=(112, 91), r_leg=(-98, -94), l_leg=(-82, -86)),
    ],
    'focus': [                       # 目を閉じて構える
        P(r_arm=(-68, -80), l_arm=(-112, -86), r_leg=(-94, -92), l_leg=(-86, -90)),
        P(bob=0.02, r_arm=(-70, -82), l_arm=(-110, -84), r_leg=(-94, -92), l_leg=(-86, -90)),
        P(bob=0.02, r_arm=(-70, -82), l_arm=(-110, -84), r_leg=(-94, -92), l_leg=(-86, -90)),
        P(r_arm=(-68, -80), l_arm=(-112, -86), r_leg=(-94, -92), l_leg=(-86, -90)),
    ],
}

# コマごとに足す言葉。骨格だけでは伝わらない「表情」「剣の向き」をここで補う
HINTS = {
    'idle':    ['standing, smiling, sword resting point down', 'standing, smiling', 'standing, smiling', 'standing, smiling'],
    'walk':    ['walking'] * 6,
    'attack':  ['raising greatsword overhead', 'greatsword raised high behind head, determined',
                'swinging greatsword down, motion blur', 'greatsword slammed down, eyes closed, shouting'],
    'slash':   ['greatsword held back to the left', 'starting horizontal swing',
                'horizontal slash across, motion blur', 'follow through of horizontal slash'],
    'cast':    ['raising greatsword to the sky', 'greatsword high overhead, glowing',
                'thrusting greatsword toward ground', 'greatsword stabbed into ground, shockwave, crouching'],
    'guard':   ['holding greatsword vertically as a shield, eyes closed', 'holding greatsword vertically as a shield, eyes closed'],
    'hit':     ['staggering backward, hurt expression, eyes closed', 'knocked back, hurt expression'],
    'victory': ['holding greatsword up in victory, cheerful', 'jumping with greatsword raised, cheerful, eyes closed',
                'jumping high with greatsword raised, cheerful', 'landing with greatsword raised, cheerful'],
    'focus':   ['calm stance, eyes closed, concentrating'] * 4,
}

def main():
    os.makedirs(OUT, exist_ok=True)
    index = []
    for action, frames in POSES.items():
        for i, kw in enumerate(frames):
            img = draw_pose(build(**kw))
            name = f'{action}_{i}'
            img.save(os.path.join(OUT, name + '.png'))
            index.append({
                'action': action,
                'frame': i,
                'file': name + '.png',
                'hint': HINTS.get(action, [''] * len(frames))[i],
            })
    with open(os.path.join(OUT, 'index.json'), 'w', encoding='utf-8') as f:
        json.dump(index, f, ensure_ascii=False, indent=2)
    print(f'{len(index)} 枚の骨格を {OUT} に書き出しました')

if __name__ == '__main__':
    main()
