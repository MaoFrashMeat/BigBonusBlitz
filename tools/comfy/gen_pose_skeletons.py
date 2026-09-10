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
W, H = 832, 1216        # SDXL の縦長。横に二人目が入らない形

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
          r_arm=(-98, -94), l_arm=(-82, -86),    # 肩から見た角度（度）: 肘, 手首
          r_leg=(-94, -91), l_leg=(-86, -89),
          r_arm_len=1.0, l_arm_len=1.0,          # 手前に出した腕は短く見える（正面向きの奥行き）
          r_leg_len=1.0, l_leg_len=1.0,
          head_tilt=0.0, crouch=0.0):
    """
    角度で腕と脚を組む。**画面の向きで測る**。真下が -90 度、右が 0 度、左が 180 度。
    キャラは正面を向いているので、キャラの右手は画面の左（x がマイナス側）に来る。
    r_arm/l_arm は (肘へ向かう角度, 手首へ向かう角度)。
    長さの倍率は、正面向きで前後に振った手足を短く見せるために使う。
    """
    def polar(p, ang, ln):
        a = math.radians(ang)
        # y は上が正。画面の下向き -90 度を世界座標に直すと +sin になる
        return (p[0] + math.cos(a) * ln, p[1] + math.sin(a) * ln)

    neck = (lean, neck_y + bob)
    nose = (lean + head_tilt * 0.10, neck_y + 0.42 + bob)
    r_sh = (lean - 0.34, neck_y - 0.05 + bob)
    l_sh = (lean + 0.34, neck_y - 0.05 + bob)
    r_hip = (-0.22, 0.62 - crouch + bob * 0.5)
    l_hip = (0.22, 0.62 - crouch + bob * 0.5)

    r_el = polar(r_sh, r_arm[0], 0.46 * r_arm_len)
    r_wr = polar(r_el, r_arm[1], 0.44 * r_arm_len)
    l_el = polar(l_sh, l_arm[0], 0.46 * l_arm_len)
    l_wr = polar(l_el, l_arm[1], 0.44 * l_arm_len)
    r_kn = polar(r_hip, r_leg[0], 0.56 * r_leg_len)
    r_an = polar(r_kn, r_leg[1], 0.54 * r_leg_len)
    l_kn = polar(l_hip, l_leg[0], 0.56 * l_leg_len)
    l_an = polar(l_kn, l_leg[1], 0.54 * l_leg_len)

    eye_dx, eye_y = 0.12, 0.50
    return [
        nose, neck, r_sh, r_el, r_wr, l_sh, l_el, l_wr,
        r_hip, r_kn, r_an, l_hip, l_kn, l_an,
        (nose[0] - eye_dx, neck_y + eye_y + bob), (nose[0] + eye_dx, neck_y + eye_y + bob),
        (nose[0] - 0.26, neck_y + 0.46 + bob), (nose[0] + 0.26, neck_y + 0.46 + bob),
    ]

def draw_pose(points, size=None):
    """OpenPose 形式（黒地に色付きの骨と関節）で描く。"""
    img = Image.new('RGB', (W, H), (0, 0, 0))
    d = ImageDraw.Draw(img)
    size = H
    cx, cy = W * 0.5, H * 0.5
    # 骨は目と足首までしか描かない。実際の絵はその外に髪と靴が出るので、
    # 頭頂 2.45 〜 靴底 -0.60 を全身とみなして枠の 8 割に収める
    scale = size * 0.26
    mid_y = 0.92

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
    # 立ち: 息づかいだけ。剣は右手（画面左）に下げて持つ
    'idle': [
        P(bob=0.000, r_arm=(-100, -96), l_arm=(-84, -88)),
        P(bob=0.025, r_arm=(-101, -97), l_arm=(-83, -87)),
        P(bob=0.035, r_arm=(-102, -98), l_arm=(-82, -86)),
        P(bob=0.015, r_arm=(-100, -96), l_arm=(-84, -88)),
    ],
    # 歩き: 正面向きなので、前に出した脚は短く見せて上下動で読ませる。
    # 腕は脚と逆に振る（右脚が上がるとき左腕が前）
    'walk': [
        # 右脚を大きく上げる
        P(bob=0.005, lean=-0.04, r_leg_len=0.70, l_leg_len=1.00,
          r_leg=(-78, -112), l_leg=(-92, -92),
          r_arm=(-108, -100), l_arm=(-74, -80), r_arm_len=1.06, l_arm_len=0.84),
        # 踏み出す途中。体が持ち上がる
        P(bob=0.075, lean=-0.02, r_leg_len=0.84, l_leg_len=0.99,
          r_leg=(-84, -104), l_leg=(-90, -91),
          r_arm=(-103, -97), l_arm=(-79, -84), r_arm_len=1.03, l_arm_len=0.91),
        # 両脚が揃う
        P(bob=0.030, lean=0.00, r_leg_len=0.97, l_leg_len=0.93,
          r_leg=(-93, -92), l_leg=(-87, -90),
          r_arm=(-98, -94), l_arm=(-84, -88), r_arm_len=0.98, l_arm_len=0.98),
        # 左脚を大きく上げる（1 コマ目の裏返し）
        P(bob=0.005, lean=0.04, r_leg_len=1.00, l_leg_len=0.70,
          r_leg=(-88, -88), l_leg=(-102, -68),
          r_arm=(-106, -100), l_arm=(-72, -80), r_arm_len=0.84, l_arm_len=1.06),
        P(bob=0.075, lean=0.02, r_leg_len=0.99, l_leg_len=0.84,
          r_leg=(-90, -89), l_leg=(-96, -76),
          r_arm=(-101, -96), l_arm=(-77, -83), r_arm_len=0.91, l_arm_len=1.03),
        P(bob=0.030, lean=0.00, r_leg_len=0.93, l_leg_len=0.97,
          r_leg=(-93, -90), l_leg=(-87, -88),
          r_arm=(-98, -94), l_arm=(-84, -88), r_arm_len=0.98, l_arm_len=0.98),
    ],
    # 振りかぶり → 振り下ろし。両手で柄を握るので、手首は頭の真上あたりで合う
    'attack': [
        P(lean=-0.05, r_arm=(125, -5), l_arm=(55, -175), r_leg=(-99, -93), l_leg=(-82, -87)),
        P(lean=-0.09, bob=0.05, r_arm=(112, 18), l_arm=(68, 162), r_leg=(-101, -94), l_leg=(-80, -86)),
        P(lean=0.07, bob=-0.03, r_arm=(-120, -40), l_arm=(-60, -140), r_leg=(-90, -90), l_leg=(-88, -90)),
        P(lean=0.10, bob=-0.06, crouch=0.07, r_arm=(-115, -45), l_arm=(-65, -135),
          r_leg=(-86, -89), l_leg=(-94, -92)),
    ],
    # 横薙ぎ: 画面右に引いてから左へ薙ぐ
    'slash': [
        P(lean=-0.09, r_arm=(-13, -13), l_arm=(-72, 12), r_leg=(-99, -93), l_leg=(-82, -87)),
        P(lean=-0.03, r_arm=(-38, -30), l_arm=(-85, -8), r_leg=(-95, -91), l_leg=(-85, -89)),
        P(lean=0.09, r_arm=(-95, 168), l_arm=(-167, -167), r_leg=(-88, -90), l_leg=(-92, -91)),
        P(lean=0.13, r_arm=(-108, 192), l_arm=(-167, -167), r_leg=(-85, -88), l_leg=(-95, -93)),
    ],
    # 天に掲げる → 地面に突き立てる
    'cast': [
        P(bob=0.05, r_arm=(115, 10), l_arm=(65, 170), r_leg=(-95, -91), l_leg=(-85, -89)),
        P(bob=0.11, r_arm=(105, 40), l_arm=(75, 140), r_leg=(-94, -91), l_leg=(-86, -89)),
        P(bob=-0.04, crouch=0.05, r_arm=(-118, -55), l_arm=(-62, -125), r_leg=(-92, -91), l_leg=(-88, -89)),
        P(bob=-0.09, crouch=0.12, r_arm=(-112, -62), l_arm=(-68, -118), r_leg=(-90, -91), l_leg=(-90, -91)),
    ],
    # 剣を立てて構える。両手は胸の中央で合わせる
    'guard': [
        P(lean=-0.03, crouch=0.04, r_arm=(-105, 40), l_arm=(-75, 140), r_leg=(-98, -93), l_leg=(-82, -87)),
        P(lean=-0.03, crouch=0.04, bob=0.02, r_arm=(-106, 38), l_arm=(-74, 142), r_leg=(-98, -93), l_leg=(-82, -87)),
    ],
    # のけぞる。腕は左右に投げ出す
    'hit': [
        P(lean=0.13, head_tilt=0.4, r_arm=(-130, -165), l_arm=(-55, -15), r_leg=(-84, -88), l_leg=(-98, -94)),
        P(lean=0.19, head_tilt=0.6, crouch=0.05, r_arm=(-142, -172), l_arm=(-42, -6), r_leg=(-80, -86), l_leg=(-102, -96)),
    ],
    # 勝利: 跳ねながら剣を掲げる。跳ぶコマは脚を畳む
    'victory': [
        P(r_arm=(110, 25), l_arm=(70, 155), r_leg=(-95, -91), l_leg=(-85, -89)),
        P(bob=0.08, r_arm=(107, 35), l_arm=(73, 145), r_leg=(-92, -96), l_leg=(-88, -84),
          r_leg_len=0.90, l_leg_len=0.90),
        P(bob=0.13, r_arm=(105, 45), l_arm=(75, 135), r_leg=(-90, -102), l_leg=(-90, -78),
          r_leg_len=0.82, l_leg_len=0.82),
        P(bob=0.06, r_arm=(109, 28), l_arm=(71, 152), r_leg=(-94, -92), l_leg=(-86, -88),
          r_leg_len=0.94, l_leg_len=0.94),
    ],
    # 集中: 目を閉じて、剣を体の前で下げる
    'focus': [
        P(r_arm=(-100, -55), l_arm=(-80, -125), r_leg=(-93, -91), l_leg=(-87, -89)),
        P(bob=0.02, r_arm=(-101, -57), l_arm=(-79, -123), r_leg=(-93, -91), l_leg=(-87, -89)),
        P(bob=0.03, r_arm=(-102, -58), l_arm=(-78, -122), r_leg=(-93, -91), l_leg=(-87, -89)),
        P(bob=0.01, r_arm=(-100, -55), l_arm=(-80, -125), r_leg=(-93, -91), l_leg=(-87, -89)),
    ],
}

# コマごとに足す言葉。骨格だけでは伝わらない「表情」「剣の向き」をここで補う
HINTS = {
    'idle':    ['arms down at her sides, sword tip resting on the ground, standing, smiling'] * 4,
    'walk':    ['arms down at her sides, sword held low, walking forward'] * 6,
    'attack':  ['both arms raised, greatsword lifted to head height, ready to strike',
                'both arms straight up, greatsword raised high above her head, determined',
                'both arms swinging down in front of her, greatsword mid-swing, motion blur',
                'both arms low in front, greatsword slammed into the ground, shouting, eyes closed'],
    'slash':   ['both arms pulled to her left side, greatsword held back horizontally',
                'both arms starting a horizontal swing to her left',
                'both arms swept across to her right, horizontal slash, motion blur',
                'both arms extended to her right, follow through of a horizontal slash'],
    'cast':    ['both arms raised, greatsword pointed up to the sky',
                'both arms straight up, greatsword high overhead, glowing blade',
                'both arms low in front, thrusting the greatsword toward the ground',
                'both arms low, greatsword stabbed into the ground, crouching, shockwave'],
    'guard':   ['both arms in front of her chest, greatsword held vertically like a shield, eyes closed'] * 2,
    'hit':     ['arms flung out to the sides, staggering backward, hurt expression, eyes closed',
                'arms flung out wide, knocked back, hurt expression'],
    'victory': ['both arms raised, greatsword held up in victory, cheerful',
                'both arms up, jumping with the greatsword raised, cheerful, eyes closed',
                'both arms straight up, jumping high with the greatsword raised, cheerful',
                'both arms up, landing with the greatsword raised, cheerful'],
    'focus':   ['both hands together low in front, greatsword point down, eyes closed, concentrating'] * 4,
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
