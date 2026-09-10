# -*- coding: utf-8 -*-
"""note 用の画像を生成する。

  py -3 docs/note/build_images.py

出力: docs/note/images/*.png
- 実在のスクリーンショットはリサイズしてコピー
- スプライトからコンタクトシートを合成
- 仕組みの図解は PIL で描画
再実行しても同じものが出る。素材が無いものはスキップして最後に一覧を出す。
"""
import os
import sys
import glob

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "images")

W = 1280                     # note の表示幅に合わせた基準幅
BG = (255, 255, 255)
INK = (26, 26, 26)
SUB = (107, 107, 102)
LINE = (216, 216, 212)
BAD = (192, 57, 43)
GOOD = (30, 122, 90)
FILL_BAD = (253, 240, 238)
FILL_GOOD = (236, 247, 242)
FILL_GRAY = (246, 246, 244)

FONT_B = r"C:\Windows\Fonts\BIZ-UDGothicB.ttc"
FONT_R = r"C:\Windows\Fonts\BIZ-UDGothicR.ttc"

skipped = []
made = []


def font(size, bold=False):
    path = FONT_B if bold else FONT_R
    try:
        return ImageFont.truetype(path, size, index=0)
    except Exception:
        return ImageFont.load_default()


def text_w(draw, s, f):
    return draw.textbbox((0, 0), s, font=f)[2]


def canvas(h):
    im = Image.new("RGB", (W, h), BG)
    return im, ImageDraw.Draw(im)


def title(draw, s, y=44):
    f = font(38, True)
    draw.text((60, y), s, font=f, fill=INK)
    return y + 62


def caption(draw, s, y):
    f = font(23)
    draw.text((60, y), s, font=f, fill=SUB)
    return y + 36


def box(draw, xy, fill, border, radius=14, width=2):
    draw.rounded_rectangle(xy, radius=radius, fill=fill, outline=border, width=width)


def save(im, name):
    im.save(os.path.join(OUT, name))
    made.append(name)
    print("  made", name)


def fit(src, name, max_w=W):
    """既存画像を幅 max_w に収めて保存する。"""
    p = os.path.join(ROOT, src)
    if not os.path.exists(p):
        skipped.append("%s (元ファイルが無い: %s)" % (name, src))
        return
    im = Image.open(p).convert("RGB")
    if im.width > max_w:
        h = int(im.height * max_w / im.width)
        im = im.resize((max_w, h), Image.LANCZOS)
    save(im, name)


# ---------------------------------------------------------------- 04 9ポーズ

def hero_sheet():
    d = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Art", "HeroChibi")
    order = [("hero_idle", "立つ"), ("hero_walk", "歩く"), ("hero_attack", "攻撃"),
             ("hero_slash", "斬る"), ("hero_cast", "詠唱"), ("hero_guard", "構える"),
             ("hero_focus", "集中"), ("hero_hit", "被弾"), ("hero_victory", "勝利")]
    tiles = []
    for key, label in order:
        p = os.path.join(d, key + ".png")
        if not os.path.exists(p):
            skipped.append("04-hero-9poses.png (%s が無い)" % key)
            return
        sheet = Image.open(p).convert("RGBA")
        fr = sheet.height                      # 1フレームは正方形
        tiles.append((sheet.crop((0, 0, fr, fr)), label))

    cell, pad, lab = 300, 24, 42
    cols, rows = 3, 3
    w = pad + cols * (cell + pad)
    h = pad + rows * (cell + lab + pad)
    im = Image.new("RGB", (w, h), (250, 250, 248))
    dr = ImageDraw.Draw(im)
    f = font(24, True)
    for i, (tile, label) in enumerate(tiles):
        cx = pad + (i % cols) * (cell + pad)
        cy = pad + (i // cols) * (cell + lab + pad)
        dr.rounded_rectangle((cx, cy, cx + cell, cy + cell), radius=12,
                             fill=(255, 255, 255), outline=LINE, width=2)
        t = tile.resize((cell - 24, cell - 24), Image.LANCZOS)
        im.paste(t, (cx + 12, cy + 12), t)
        tw = dr.textbbox((0, 0), label, font=f)[2]
        dr.text((cx + (cell - tw) // 2, cy + cell + 8), label, font=f, fill=INK)
    if im.width > W:
        im = im.resize((W, int(im.height * W / im.width)), Image.LANCZOS)
    save(im, "04-hero-9poses.png")


# --------------------------------------------------------- 05 ポーズ→生成結果

def pose_to_sprite():
    pose = os.path.join(ROOT, "tools", "comfy", "poses", "slash_0.png")
    ref = os.path.join(ROOT, "tools", "comfy", "popora_ref.png")
    out = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets",
                       "Resources", "Art", "HeroChibi", "hero_slash.png")
    for p in (pose, ref, out):
        if not os.path.exists(p):
            skipped.append("05-pose-to-sprite.png (%s が無い)" % os.path.basename(p))
            return

    cell = 360
    im, dr = canvas(cell + 232)
    y = title(dr, "同じキャラを保つためにやったこと")
    labels = ["① 基準にする1枚", "② 姿勢の指定", "③ 出てきた絵"]
    srcs = [Image.open(ref).convert("RGBA"),
            Image.open(pose).convert("RGBA"),
            None]
    sheet = Image.open(out).convert("RGBA")
    srcs[2] = sheet.crop((0, 0, sheet.height, sheet.height))

    gap = 40
    total = 3 * cell + 2 * gap
    x0 = (W - total) // 2
    fl = font(25, True)
    for i, (lab, src) in enumerate(zip(labels, srcs)):
        x = x0 + i * (cell + gap)
        dr.rounded_rectangle((x, y, x + cell, y + cell), radius=12,
                             fill=(250, 250, 248), outline=LINE, width=2)
        s = src.copy()
        s.thumbnail((cell - 28, cell - 28), Image.LANCZOS)
        bgw = Image.new("RGBA", (cell - 28, cell - 28), (255, 255, 255, 255))
        bgw.paste(s, ((cell - 28 - s.width) // 2, (cell - 28 - s.height) // 2), s)
        im.paste(bgw.convert("RGB"), (x + 14, y + 14))
        tw = text_w(dr, lab, fl)
        dr.text((x + (cell - tw) // 2, y + cell + 14), lab, font=fl, fill=INK)
        if i < 2:
            ax = x + cell + gap // 2
            dr.text((ax - 10, y + cell // 2 - 20), "→", font=font(36, True), fill=SUB)
    caption(dr, "① と ② を毎回セットで渡す。姿勢を固定すると、顔と服のブレが減る", y + cell + 62)
    save(im, "05-pose-to-sprite.png")


# ------------------------------------------------------------- 06 タイムライン

def timeline():
    im, dr = canvas(500)
    y = title(dr, "105日のうち、約2ヶ月は止まっていた")
    y += 24

    x0, x1 = 80, W - 80
    bar_y = y + 78
    bar_h = 56
    segs = [(26, FILL_GRAY, LINE, "ブラウザ版をつくる", "26日 / 26コミット"),
            (54, FILL_BAD, BAD, "止まっていた", "約2ヶ月 / 1コミット"),
            (21, FILL_GRAY, LINE, "仕組みをつくる", "21日"),
            (4, FILL_GOOD, GOOD, "Unityで作り直す", "9/7から4日 / 12,308行")]
    total = sum(x[0] for x in segs)

    x = x0
    for days, fill, border, _h, _s in segs:
        w = int((x1 - x0) * days / total)
        dr.rounded_rectangle((x, bar_y, x + w, bar_y + bar_h), radius=8,
                             fill=fill, outline=border, width=2)
        x += w

    fm = font(21, True)
    fs = font(20)
    marks = [(x0, "5/29", "作り始めた", 0),
             (x0 + int((x1 - x0) * 26 / total), "6/24", "止まった", 0),
             (x0 + int((x1 - x0) * 80 / total), "8/17", "再開", 0),
             (x1, "9/10", "今日", 1)]
    for mx, a, b, right in marks:
        dr.line((mx, bar_y - 30, mx, bar_y - 6), fill=SUB, width=2)
        for txt, f, col, dy in ((a, fm, INK, -70), (b, fs, SUB, -44)):
            tw = text_w(dr, txt, f)
            px = mx - tw if right else mx - tw // 2
            px = min(max(px, 8), W - tw - 8)
            dr.text((px, bar_y + dy), txt, font=f, fill=col)

    ly = bar_y + bar_h + 34
    cw = (x1 - x0) // 4
    for i, (_d, fill, border, head, sub) in enumerate(segs):
        lx = x0 + i * cw
        dr.rounded_rectangle((lx, ly + 4, lx + 18, ly + 22), radius=4,
                             fill=fill, outline=border, width=2)
        dr.text((lx + 30, ly), head, font=font(22, True), fill=INK)
        dr.text((lx + 30, ly + 30), sub, font=fs, fill=SUB)

    caption(dr, "仕組みを入れる前の3ヶ月と、入れたあとの4日。使ったAIは同じ", ly + 76)
    save(im, "06-timeline.png")


# --------------------------------------------------------- 07 決定の蒸発→指示書

def before_after():
    im, dr = canvas(620)
    y = title(dr, "決定を、どこに置くか")
    y += 16

    cw = (W - 60 * 2 - 48) // 2
    ch = 400
    fl = font(27, True)
    fs = font(22)

    # 左: だめな置き方
    x = 60
    box(dr, (x, y, x + cw, y + ch), FILL_BAD, BAD)
    dr.text((x + 26, y + 24), "会話の中に置く", font=fl, fill=BAD)
    dr.text((x + 26, y + 62), "私がやっていた3ヶ月", font=fs, fill=SUB)
    items = ["セッション1 「数字は1箇所にまとめて」",
             "  ↓ 会話が終わる",
             "セッション2 決定を知らない状態で再開",
             "  ↓ 私が説明し直す",
             "セッション3 少しズレて伝わる",
             "  ↓",
             "仕様が静かに2つに割れる"]
    ty = y + 112
    for it in items:
        col = BAD if it.startswith("仕様") else INK
        dr.text((x + 26, ty), it, font=font(22, it.startswith("仕様")), fill=col)
        ty += 38

    # 右: よい置き方
    x = 60 + cw + 48
    box(dr, (x, y, x + cw, y + ch), FILL_GOOD, GOOD)
    dr.text((x + 26, y + 24), "ファイルに置く", font=fl, fill=GOOD)
    dr.text((x + 26, y + 62), "9月から", font=fs, fill=SUB)
    items = ["指示書1枚  ← 毎回いちばん最初に読む",
             "  ・応答は15行以内",
             "  ・依頼した箇所だけ変更する",
             "  ・数字はコードに書かない",
             "",
             "記憶ファイル ← 途中で分かったことを足す",
             "セッションが変わっても、続きから始まる"]
    ty = y + 112
    for it in items:
        col = GOOD if it.startswith("セッション") else INK
        dr.text((x + 26, ty), it, font=font(22, it.startswith("セッション")), fill=col)
        ty += 38

    caption(dr, "AIは毎回、初対面。口頭で引き継ぐのをやめて、読ませる場所を決めた", y + ch + 28)
    save(im, "07-before-after.png")


# ------------------------------------------------------------- 08 記憶の3段昇格

def promotion():
    im, dr = canvas(540)
    y = title(dr, "1回のまぐれを、ルールにしない")
    y += 20

    stages = [("1回", "observations", "ただのメモ\nまだ信用しない", FILL_GRAY, LINE),
              ("2回", "knowledge", "参考にしてよい\n別の案件でも見た", (243, 246, 250), (140, 160, 190)),
              ("3回", "rules", "無条件に従ってよい\n判断を変えるレベル", FILL_GOOD, GOOD)]
    cw, gap = 340, 46
    x0 = (W - (3 * cw + 2 * gap)) // 2
    ch = 250
    for i, (cnt, name, desc, fill, border) in enumerate(stages):
        x = x0 + i * (cw + gap)
        box(dr, (x, y, x + cw, y + ch), fill, border)
        dr.text((x + 24, y + 22), "観測 " + cnt, font=font(30, True), fill=INK)
        dr.text((x + 24, y + 66), name + "/", font=font(24, True), fill=border if i else SUB)
        ly = y + 112
        for line in desc.split("\n"):
            dr.text((x + 24, ly), line, font=font(22), fill=SUB)
            ly += 34
        if i < 2:
            dr.text((x + cw + 8, y + ch // 2 - 22), "→", font=font(38, True), fill=SUB)

    dr.text((x0, y + ch + 34), "6ヶ月参照されなかったルールは、observations へ降格させる",
            font=font(23), fill=SUB)
    caption(dr, "AIは書いてあることを疑わない。だから、書く前に回数の条件をつけた", y + ch + 76)
    save(im, "08-memory-promotion.png")


# --------------------------------------------------------- 09 完成条件と検品

def acceptance_flow():
    im, dr = canvas(600)
    y = title(dr, "「できました」を、合格理由にしない")
    y += 20

    steps = [("① 頼む前", "完成条件を3つ書く\n「何ができたら完成か」", FILL_GRAY, LINE),
             ("② 作業", "AIが作る\n条件は書き換えさせない", FILL_GRAY, LINE),
             ("③ 証拠", "条件ごとに証拠を書かせる\n空欄のまま完了にしない", FILL_GRAY, LINE),
             ("④ 検品", "別のAIが突き合わせる\n合格・不合格・検証不能", FILL_GOOD, GOOD)]
    cw, gap = 272, 30
    x0 = (W - (4 * cw + 3 * gap)) // 2
    ch = 220
    for i, (head, desc, fill, border) in enumerate(steps):
        x = x0 + i * (cw + gap)
        box(dr, (x, y, x + cw, y + ch), fill, border)
        dr.text((x + 22, y + 22), head, font=font(27, True), fill=INK)
        ly = y + 74
        for line in desc.split("\n"):
            dr.text((x + 22, ly), line, font=font(19), fill=SUB)
            ly += 32
        if i < 3:
            dr.text((x + cw + 2, y + ch // 2 - 20), "→", font=font(34, True), fill=SUB)

    ny = y + ch + 46
    box(dr, (x0, ny, x0 + 4 * cw + 3 * gap, ny + 128), FILL_BAD, BAD)
    dr.text((x0 + 24, ny + 20), "検品役に必ず書く1行", font=font(25, True), fill=BAD)
    dr.text((x0 + 24, ny + 62), "「あなたは検品役です。実装も修正もしないでください」",
            font=font(24, True), fill=INK)
    dr.text((x0 + 24, ny + 96), "これが無いと、検品役が勝手に直し始める", font=font(21), fill=SUB)
    save(im, "09-acceptance-flow.png")


# ----------------------------------------------------------- 10 壊れ方の4つの型

def failure_types():
    im, dr = canvas(900)
    y = title(dr, "19件を並べて見えた、5つの型")
    y += 16

    rows = [("型A", "画面では気づけない",
             "見た目は正常に動く。壊れているのは平均値・境界・例外", "11件", "測る（通しテストを回す）"),
            ("型B", "指示どおりだが、人が見ると破綻する",
             "半透明の板も、隣に置いたアイコンも、指示は正しく実行された", "2件", "「読めること」を完成条件に入れる"),
            ("型C", "文脈の外の制約を知らない",
             "環境の癖、そのPCが誰の資産か。聞かれるまで考慮しない", "3件", "制約を先に渡す／転んだら記録"),
            ("型D", "調べる場所を間違える",
             "直前に触った場所から疑う。記録を読んで、実装を読まない", "2件", "「まず現物を見てから答えて」"),
            ("型E", "出典を混ぜる",
             "引用と実測、他人の話と自分の話を、悪意なく混ぜる", "1件", "数字に「誰が測ったか」を書かせる")]

    rh, gap = 128, 18
    fl = font(26, True)
    fs = font(21)
    for i, (tag, head, desc, cnt, fix) in enumerate(rows):
        ry = y + i * (rh + gap)
        is_d = tag == "型E"
        box(dr, (60, ry, W - 60, ry + rh), FILL_BAD if is_d else FILL_GRAY,
            BAD if is_d else LINE)
        dr.text((84, ry + 20), tag, font=font(30, True), fill=BAD if is_d else INK)
        dr.text((84, ry + 66), cnt, font=fs, fill=SUB)
        dr.text((188, ry + 20), head, font=fl, fill=INK)
        dr.text((188, ry + 60), desc, font=fs, fill=SUB)
        dr.text((188, ry + 92), "対処: " + fix, font=font(21, True),
                fill=BAD if is_d else GOOD)

    ly = y + 5 * (rh + gap) + 10
    dr.text((60, ly), "型A〜D は直せば済む。型E だけは、公開したあとだと直せない",
            font=font(24, True), fill=BAD)
    save(im, "10-failure-types.png")


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    print("出力先:", OUT)

    fit("game_bg_in_game_final.png", "01-title-screen.png")
    fit("editor_after.png", "02-settings-editor.png", max_w=900)
    fit("wf_full_test.png", "03-workflow-editor.png")
    hero_sheet()
    pose_to_sprite()
    timeline()
    before_after()
    promotion()
    acceptance_flow()
    failure_types()

    print("\n作成 %d 件" % len(made))
    if skipped:
        print("スキップ %d 件:" % len(skipped))
        for s in skipped:
            print("  -", s)


if __name__ == "__main__":
    main()
