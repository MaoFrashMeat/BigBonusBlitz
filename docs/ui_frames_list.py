# -*- coding: utf-8 -*-
"""UI の枠の実寸を、コードの定数から計算して一覧にする。"""
import io
import sys

StageW, StageH, Margin = 1170.0, 540.0, 8.0   # 冒険画面は iPhone 幅
ContentW = StageW - Margin * 2
StageCardH, BandH, MidH, CtrlH, SideW = 332.0, 28.0, 120.0, 56.0, 360.0
ReelW, SymH = 123.0, 56.0
reelPitch = ReelW + 8.0
cabW = reelPitch * 3 + 24
reelH = SymH * 3
innerW = SideW - 24
AreaW, AreaH = ContentW - 4, StageCardH - BandH - 4

ROWS = [
 ('■ ゲーム画面（舞台 960×540）', None, None, None, None),
 ('ステージ札（上段の大枠）', ContentW, StageCardH, 14, 'UiSkin.Card / GameController StageCard'),
 ('表示器（左・EMBER/PAYOUT）', SideW, MidH, 12, 'Card / Display'),
 ('リール筐体', cabW, MidH + 8 + CtrlH, 12, 'Card 相当 / cabinet（中段と下段にまたがる）'),
 ('操作パネル（右・PLAYER）', SideW, MidH, 12, 'Card / Side'),
 ('MAX BET ボタン', SideW, CtrlH, 10, 'UiSkin.Button'),
 ('AUTO ボタン', SideW - 44*4 - 32, CtrlH, 10, 'UiSkin.Button'),
 ('装備・実績・歯車・グラフ（右下の隅）', 44, 44, 10, 'UiSkin.Button + Icon'),
 ('', None, None, None, None),
 ('■ くぼみ（Inset。枠ではなく彫り込み）', None, None, None, None),
 ('リール窓（3 つ）', ReelW + 8, reelH + 8, 8, 'Inset / Window'),
 ('EMBER の窓', innerW - 78, 26, 6, 'Inset / CreditInset'),
 ('PAYOUT の窓', innerW - 78, 26, 6, 'Inset / PayoutInset'),
 ('LIFE の窓', innerW - 78, 26, 6, 'Inset / TorchTag'),
 ('常駐スランプ', (innerW - 12) / 2, 40, 5, 'Inset / MiniSlump（右パネルの右の列）'),
 ('DEBUG の情報欄', 280, 268, 8, 'Inset / Info'),
 ('', None, None, None, None),
 ('■ 札（小さな見出し）', None, None, None, None),
 ('ステージ札', 330, 30, 15, '角丸は Shadow(15,10) に合わせている'),
 ('ルート札', 134, 118, 15, ''),
 ('ナビのバッジ（3 つ）', 66, 66, 33, '円形'),
 ('', None, None, None, None),
 ('■ 窓（モーダル）', None, None, None, None),
 ('サウンド / 設定', 400, 272, 14, 'BuildModal'),
 ('DEBUG', 660, 420, 14, 'BuildModal'),
 ('スランプグラフ', 720, 500, 14, 'BuildModal'),
 ('冒険マップ', 760, 460, 14, 'BuildModal'),
 ('装備', 800, 470, 16, 'EquipScreen'),
 ('ショップ', 780, 470, 16, 'ShopScreen'),
 ('呪いと祝福', 520, 260, 16, 'GameController CurseCard'),
 ('', None, None, None, None),
 ('■ 窓の中の小さな枠', None, None, None, None),
 ('装備: 着ている部位（3 つ）', 250, 68, 10, 'EquipScreen Worn'),
 ('装備: 詳細', 800 - 36, 92, 10, 'EquipScreen Detail'),
 ('ショップ: ステータスの列（3 つ）', 232, 250, 12, 'ShopScreen Stat'),
 ('ショップ: 補給のカード（2 つ）', 340, 170, 12, 'ShopScreen Torch/Coin'),
 ('ショップ: 品物の行', 360, 84, 12, 'ShopScreen Item'),
 ('呪い / 祝福の札（2 つ）', 520 * 0.5 - 24, 108, 10, 'GameController'),
 ('', None, None, None, None),
 ('■ ミニマップ', None, None, None, None),
 ('財布', 220, 52, 12, 'MapScreen Purse'),
 ('行き先カード（街 / 冒険）', 250, 170, 16, 'MapScreen MapNode'),
 ('ステージマップの板', 640, 330, 14, 'MapScreen MapPanel'),
]

out = []
out.append('| 枠 | 幅 x 高さ (px) | 角丸 | 出どころ |')
out.append('|---|---|---|---|')
for name, w, h, r, note in ROWS:
    if w is None:
        out.append('| **%s** | | | |' % name if name else '| | | | |')
        continue
    out.append('| %s | %d x %d | %d | %s |' % (name, round(w), round(h), r, note))
print('\n'.join(out))
if len(sys.argv) > 1:
    CRLF = chr(13) + chr(10)
    io.open(sys.argv[1], 'w', encoding='utf-8', newline=CRLF).write(chr(10).join(out) + chr(10))
