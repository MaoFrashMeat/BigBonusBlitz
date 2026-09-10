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
StageW, StageH = 960.0, 540.0
Margin = 8.0
ContentW = StageW - Margin * 2      # 944
SideW = 218.0
innerW = SideW - 24                 # 194
AreaW, AreaH, AreaY = 940.0, 220.0, -15.0
BandH = 28.0

# ---------------------------------------------------------------- ゲーム画面
HeadIco, HeadGap = 15.0, 5.0
HeadIndent = HeadIco + HeadGap
row('左パネルの見出し行（EMBER / PAYOUT）',
    [('アイコン', -innerW / 2 + HeadIco / 2, HeadIco),
     ('見出し文字', 0 + HeadIndent / 2, innerW - HeadIndent)],
    left=-innerW / 2, right=innerW / 2)

SoulIco = 14.0
half = innerW / 2
row('左パネルの下の行（設定 / ソウル）',
    [('設定', -innerW * 0.25 - 2, half - 4),
     ('ソウル', innerW * 0.25 - SoulIco / 2 - 2, half - SoulIco - 8),
     ('魂アイコン', half - SoulIco / 2, SoulIco)],
    left=-half, right=half)

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
torchW, torchH = 232.0, 34.0
row('表示域の上（ステージ札 / 松明札）',
    [('ステージ札', -AreaW / 2 + 10 + tagW / 2, tagW),
     ('松明札', AreaW / 2 - 10 - torchW / 2, torchW)],
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
nodeW, nodeH = 250.0, 170.0
accentH, labelH, descH, btnH, gap, btnBottom = 6.0, 34.0, 38.0, 42.0, 6.0, 16.0
top = nodeH / 2
accentCy = top - accentH / 2
labelCy = accentCy - accentH / 2 - gap - labelH / 2
descCy = labelCy - labelH / 2 - 2 - descH / 2
btnCy = -top + btnBottom + btnH / 2
stack('ミニマップの行き先カード',
      [('帯', accentCy, accentH), ('見出し', labelCy, labelH),
       ('説明', descCy, descH), ('ボタン', btnCy, btnH)],
      top=top, bottom=-top)

purseW = 220.0
row('ミニマップのソウル札',
    [('魂アイコン', -92, 20), ('SOUL', -52 + 26, 52), ('数字', 55, 100)],
    left=-purseW / 2, right=purseW / 2)

# ---------------------------------------------------------------- マップの丸
print('\n■ ステージマップ（枝が最大 8 本のとき）')
for name, w, h in [('ゲーム中', 700.0, 340.0), ('街', 610.0, 270.0)]:
    rows_, cols = 8, 8
    usableH = h - 46
    nodeD = max(16.0, min(46.0, usableH / rows_ - 10))
    g = min(120.0, usableH / rows_)
    colGap = (w - 108) / (cols - 1)
    ok_v, ok_h = g > nodeD + 2, colGap > nodeD + 2
    if not (ok_v and ok_h):
        FAIL.append(f'ステージマップ({name}) の丸が詰まりすぎ')
    print(f'    {name:<8} 径{nodeD:5.0f}  縦{g:5.0f}  横{colGap:5.0f}   '
          f'縦{"OK" if ok_v else "NG"} 横{"OK" if ok_h else "NG"}  名前表示 {"あり" if g >= 44 else "なし"}')

# ---------------------------------------------------------------- 結果
print()
if FAIL:
    print('NG が %d 件:' % len(FAIL))
    for f in FAIL:
        print('  -', f)
    sys.exit(1)
print('重なり・はみ出しなし')
