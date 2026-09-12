"""BBB の UI 配置を検算する。UI を触ったら必ず走らせる。

    py -3 docs/check_layout.py

コードの数値をここに写して確認する。目視で「重なっていないように見える」は当てにしない。
新しい行を足したら、この表にも足すこと。ルールは docs/ui_rules.md。
"""
import sys

FAIL = []


def row(title, items, left=None, right=None, note='', skip_pairs=()):
    """items: [(名前, 中心x, 幅)]。左から並べて重なりと枠外を見る。"""
    print(f'\n■ {title}')
    if note:
        print(f'  ({note})')
    items = sorted(items, key=lambda t: t[1])
    prev_r, prev_n = None, None
    for name, cx, w in items:
        l, r = cx - w / 2, cx + w / 2
        flag = ''
        if left is not None and l < left - 0.5:
            flag += f'  枠外(左 {left:.0f})'
        if right is not None and r > right + 0.5:
            flag += f'  枠外(右 {right:.0f})'
        if prev_r is not None and l < prev_r - 0.5 and (prev_n, name) not in skip_pairs:
            flag += f'  ← {prev_n} と {prev_r - l:.0f}px 重なり'
        if flag:
            FAIL.append(f'{title} / {name}:{flag}')
        print(f'    {name:<14} {l:7.0f} 〜 {r:7.0f}{flag}')
        prev_r, prev_n = r, name


def stack(title, items, top=None, bottom=None):
    """items: [(名前, 中心y, 高さ)]。上から並べて縦の重なりを見る。"""
    print(f'\n■ {title}')
    items = sorted(items, key=lambda t: -t[1])
    prev_b, prev_n = None, None
    for name, cy, h in items:
        t, b = cy + h / 2, cy - h / 2
        flag = ''
        if top is not None and t > top + 0.5:
            flag += f'  はみ出し(上 {top:.0f})'
        if bottom is not None and b < bottom - 0.5:
            flag += f'  はみ出し(下 {bottom:.0f})'
        if prev_b is not None and t > prev_b + 0.5:
            flag += f'  ← {prev_n} と {t - prev_b:.0f}px 重なり'
        if flag:
            FAIL.append(f'{title} / {name}:{flag}')
        print(f'    {name:<14} 上{t:7.0f} 下{b:7.0f}{flag}')
        prev_b, prev_n = b, name


# ---------------------------------------------------------------- 定数
# 冒険画面の舞台は iPhone 横持ちを使い切る 1170x540（他の画面は 960）
StageW, StageH = 1170.0, 540.0
Margin = 8.0
ContentW = StageW - Margin * 2      # 1154
SideW = 360.0
innerW = SideW - 24                 # 336
StageCardH, MidH, CtrlH = 332.0, 120.0, 56.0
BandH = 28.0
AreaW, AreaH, AreaY = ContentW - 4, StageCardH - BandH - 4, -BandH / 2
StageCardY = StageH / 2 - Margin - StageCardH / 2
MidY = StageCardY - StageCardH / 2 - Margin - MidH / 2
CtrlY = MidY - MidH / 2 - Margin - CtrlH / 2
ReelW, SymH = 123.0, 56.0
pitch = ReelW + 8; cabW = pitch * 3 + 24
CabH = MidH + Margin + CtrlH
CardEdge = 3.0

# ---------------------------------------------------------------- ゲーム画面
HeadIco, HeadGap = 15.0, 5.0
HeadIndent = HeadIco + HeadGap

# 全体の縦（上段の札 → 中段 → 下段）。筐体は中段と下段にまたがる
stack('冒険画面の縦', [
    ('ステージ札', StageCardY, StageCardH),
    ('中段（左右パネル）', MidY, MidH),
    ('下段（ボタン）', CtrlY, CtrlH),
], top=StageH / 2 - Margin, bottom=-StageH / 2 + Margin)
stack('筐体の縦', [('筐体', MidY - (Margin + CtrlH) / 2, CabH)],
      top=MidY + MidH / 2, bottom=CtrlY - CtrlH / 2)
stack('筐体の中（リール窓）', [('リール窓', 0, SymH * 3 + 8)], top=CabH / 2 - 3, bottom=-CabH / 2 + 3)

# 横並び（中段）
row('中段の横（表示器 / 筐体 / 操作パネル）', [
    ('表示器', -ContentW / 2 + SideW / 2, SideW),
    ('筐体', 0, cabW),
    ('操作パネル', ContentW / 2 - SideW / 2, SideW),
], left=-ContentW / 2, right=ContentW / 2)

# 下段の横（MAX BET / AUTO / 装備 / 歯車 / グラフ）
ToolIco, ToolGap = 44.0, 8.0
autoW = SideW - ToolIco * 3 - ToolGap * 3
row('下段の横', [
    ('MAX BET', -ContentW / 2 + SideW / 2, SideW),
    ('AUTO', ContentW / 2 - SideW + autoW / 2, autoW),
    ('装備', ContentW / 2 - ToolIco * 2.5 - ToolGap * 2, ToolIco),
    ('歯車', ContentW / 2 - ToolIco * 1.5 - ToolGap, ToolIco),
    ('グラフ', ContentW / 2 - ToolIco / 2, ToolIco),
], left=-ContentW / 2, right=ContentW / 2)

# 左パネル（3 行 + 下の行）
RowH, RowPitch = 26.0, 31.0
rowY0 = MidH / 2 - 12 - RowH / 2
labelW = 78.0
stack('左パネルの縦（LIFE / EMBER / PAYOUT / 設定とソウル）', [
    ('LIFE', rowY0, RowH),
    ('EMBER', rowY0 - RowPitch, RowH),
    ('PAYOUT', rowY0 - RowPitch * 2, RowH),
    ('設定とソウル', -MidH / 2 + 12, 14),
], top=MidH / 2 - CardEdge, bottom=-MidH / 2 + CardEdge)
row('左パネルの行の横（見出し / 窓）', [
    ('アイコン', -innerW / 2 + HeadIco / 2, HeadIco),
    ('見出し文字', -innerW / 2 + HeadIndent + (labelW - HeadIndent) / 2, labelW - HeadIndent),
    ('窓', -innerW / 2 + labelW + (innerW - labelW) / 2, innerW - labelW),
], left=-innerW / 2, right=innerW / 2)

SoulIco = 14.0
half = innerW / 2
row('左パネルの下の行（設定 / ソウル）',
    [('設定', -innerW * 0.25 - 2, half - 4),
     ('ソウル', innerW * 0.25 - SoulIco / 2 - 2, half - SoulIco - 8),
     ('魂アイコン', half - SoulIco / 2, SoulIco)],
    left=-half, right=half)

# 右パネル（2 列）
sColW = (innerW - 12) / 2
sColL = -innerW / 2 + sColW / 2
sColR = innerW / 2 - sColW / 2
top = MidH / 2
row('右パネルの横（2 列）', [('左の列', sColL, sColW), ('右の列', sColR, sColW)], left=-innerW / 2, right=innerW / 2)
stack('右パネルの左の列の縦', [
    ('PLAYER見出し', top - 20, 16), ('Lv', top - 38, 18), ('EXPゲージ', top - 51, 6),
    ('BONUS見出し', top - 70, 16), ('BONUS文字', top - 88, 16), ('BONUSゲージ', top - 102, 6),
], top=MidH / 2 - CardEdge, bottom=-MidH / 2 + CardEdge)
stack('右パネルの右の列の縦', [
    ('状態', top - 30, 40), ('常駐スランプ', top - 82, 40),
], top=MidH / 2 - CardEdge, bottom=-MidH / 2 + CardEdge)

row('上部の帯',
    [('モードチップ', -ContentW / 2 + 8 + 46, 88),
     ('エンゲージ', -ContentW / 2 + 8 + 88 + 8 + 60, 120),
     ('ATチップ', -ContentW / 2 + 8 + 88 + 8 + 90, 180),
     ('メッセージ', 36, 376),
     ('GAMEラベル', ContentW / 2 - 8 - 92 - 20, 40),
     ('G数', ContentW / 2 - 8 - 44, 88)],
    left=-ContentW / 2, right=ContentW / 2,
    note='エンゲージと AT は同時に出ない（AT 開始でエンゲージを打ち切る）',
    skip_pairs={('エンゲージ', 'ATチップ')})

tagW, tagH = 330.0, 30.0
row('表示域の上（ステージ札）', [('ステージ札', -AreaW / 2 + 10 + tagW / 2, tagW)],
    left=-AreaW / 2, right=AreaW / 2)

rtW, rtH = 134.0, 118.0
charSize = 206.0
charCx = -AreaW / 2 + 0.15 * AreaW + charSize / 2
row('表示域の左（ルート札 / キャラ）',
    [('ルート札', -AreaW / 2 + 6 + rtW / 2, rtW),
     ('キャラ', charCx, charSize)],
    left=-AreaW / 2)

tagTop = AreaY + AreaH / 2 - 6 - tagH / 2
stack('カード内の縦（ステージ札 → ルート札）',
      [('ステージ札', tagTop, tagH),
       ('ルート札', AreaY + AreaH / 2 - 44 - rtH / 2, rtH)],
      top=AreaY + AreaH / 2)

# ---------------------------------------------------------------- タイトルの縦
# TitleScreen: 左に寄せた列（ロゴ → TAP → はじめから → 確認文 → 丸ボタン）
LogoW, LogoY, TapY = 470.0, 112.0, -52.0
LogoH = LogoW / 1.725                # ロゴは 1623x941
PillH, PillY = 40.0, -186.0
stack('タイトルの縦（セーブありのとき）', [
    ('ロゴ', LogoY, LogoH),
    ('TAP の上線', TapY + 26, 1),
    ('TAP TO START', TapY, 32),
    ('TAP の下線', TapY - 26, 1),
    ('はじめから', TapY - 56, 30),
    ('確認文', TapY - 86, 22),
    ('丸ボタン', PillY, PillH),
], top=StageH / 2, bottom=-StageH / 2)

# タイトル下端の行（バージョンと著作権）
row('タイトルの下端', [
    ('バージョン', -StageW / 2 + 90, 160),
    ('著作権', StageW / 2 - 150, 280),
], left=-StageW / 2, right=StageW / 2)

# 丸ボタンの横
_pw = 152.0
row('タイトルの丸ボタン', [
    (n, -StageW / 2 + 24 + _pw * 0.5 + i * (_pw + 12), _pw)
    for i, n in enumerate(('お知らせ', '設定', '引き継ぎ'))
], left=-StageW / 2, right=StageW / 2)

# ---------------------------------------------------------------- グラフの窓
# GameController: Graph モーダル（720 x 500）。見出し線は上端から 38
grH = 500.0
_g = [('グラフ', grH / 2 - 38 - 8 - 132, 264),
      ('履歴の見出し', grH / 2 - 38 - 8 - 280, 16)]
for _i in range(5):
    _g.append((f'履歴{_i + 1}行目', grH / 2 - 38 - 8 - 302 - _i * 26, 24))
_g.append(('取り直しボタン', -grH / 2 + 20, 28))
stack('グラフの窓の縦', _g, top=grH / 2 - 38, bottom=-grH / 2 + CardEdge)

# ---------------------------------------------------------------- 街のショップ
W, H = 780.0, 500.0
pad, closeD, soulW, tabW, titleW = 12.0, 30.0, 170.0, 104.0, 190.0
closeCx = W / 2 - 14 - closeD / 2
soulCx = closeCx - closeD / 2 - pad - soulW / 2
tab3Cx = soulCx - soulW / 2 - pad - tabW / 2
tab2Cx = tab3Cx - tabW - 8
tab1Cx = tab2Cx - tabW - 8
row('ショップの見出し行',
    [('題名', -W / 2 + 20 + titleW / 2, titleW),
     ('装備タブ', tab1Cx, tabW), ('補給タブ', tab2Cx, tabW), ('ステタブ', tab3Cx, tabW),
     ('ソウル', soulCx, soulW), ('閉じる', closeCx, closeD)],
    left=-W / 2, right=W / 2)

# ---------------------------------------------------------------- ミニマップ
nodeW, nodeH = 264.0, 180.0
labelH, descH, btnH, gap, btnBottom = 32.0, 42.0, 48.0, 8.0, 18.0
top = nodeH / 2
labelCy = top - 24 - labelH / 2
descCy = labelCy - labelH / 2 - gap - descH / 2
btnCy = -top + btnBottom + btnH / 2
stack('ミニマップの行き先カード',
      [('見出し', labelCy, labelH),
       ('説明', descCy, descH), ('ボタン', btnCy, btnH)],
      top=top, bottom=-top)

purseW = 224.0
row('ミニマップのソウル札',
    [('魂アイコン', -90, 36), ('数字', 17, 168)],
    left=-purseW / 2, right=purseW / 2)
row('V2 map columns', [('actions', -324, 264), ('route', 142, 628)], left=-456, right=456)
row('V2 footer', [('back', -376, 160), ('life', -180, 196), ('supply', 40, 196), ('ember', 260, 196)], left=-480, right=480)
stack('V2 map headings', [('chapter',150,28), ('current',124,20), ('columns',104,18), ('nodes',-22,227), ('legend',-158,22)], top=187, bottom=-187)

# ---------------------------------------------------------------- マップの丸
print('\n■ ステージマップ（枝が最大 8 本のとき）')
for name, w, h in [('ゲーム中', 700.0, 340.0), ('街', 564.0, 258.0)]:
    rows_, cols = 8, 8
    overview = name == '街'
    usableH = h - (30 if overview else 46)
    nodeD = max(22.0 if overview else 16.0, min(46.0, usableH / rows_ - (8 if overview else 10)))
    g = min(120.0, usableH / rows_)
    colGap = (w - (48 if overview else 108)) / (cols - 1)
    ok_v, ok_h = g > nodeD + 2, colGap > nodeD + 2
    if not (ok_v and ok_h):
        FAIL.append(f'ステージマップ({name}) の丸が詰まりすぎ')
    print(f'    {name:<8} 径{nodeD:5.0f}  縦{g:5.0f}  横{colGap:5.0f}   '
          f'縦{"OK" if ok_v else "NG"} 横{"OK" if ok_h else "NG"}  名前表示 {"あり" if g >= 44 else "なし"}')

# ---------------------------------------------------------------- 結果
stack('Adventure V2 amounts', [('ember caption',76,20), ('ember value',39,44), ('payout caption',-5,20), ('payout value',-37,36), ('resources',-76,32)], top=98, bottom=-98)
row('Adventure V2 controls', [('BET',-351,242), ('settings',-78,144), ('graph',78,144), ('AUTO',351,242)], left=-480, right=480)
print()
if FAIL:
    print('NG が %d 件:' % len(FAIL))
    for f in FAIL:
        print('  -', f)
    sys.exit(1)
print('重なり・はみ出しなし')
