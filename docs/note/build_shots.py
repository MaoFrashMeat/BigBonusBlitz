# -*- coding: utf-8 -*-
"""本人のスクリーンショット（Screenpresso）と QA 画像から、note 用の画像を作る。

  py -3 docs/note/build_shots.py

元ファイルが無い PC では、その画像だけスキップして続行する。
BBB に関係ない画像（本業の 3D 作業など）は絶対に触らない。ここに列挙したものだけ使う。
"""
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(HERE, "images")
SP = r"C:\Users\sato_takuma\Pictures\Screenpresso"

W = 1280
INK = (26, 26, 26)
SUB = (107, 107, 102)
BAD = (192, 57, 43)
GOOD = (30, 122, 90)
FONT_B = r"C:\Windows\Fonts\BIZ-UDGothicB.ttc"
FONT_R = r"C:\Windows\Fonts\BIZ-UDGothicR.ttc"
UNITY_BAR = 46          # Unity の Game ビュー上部のツールバー高さ（1058 幅のとき）

made, skipped = [], []


def font(size, bold=False):
    try:
        return ImageFont.truetype(FONT_B if bold else FONT_R, size, index=0)
    except Exception:
        return ImageFont.load_default()


def load(path, crop_unity=False):
    if not os.path.exists(path):
        skipped.append(os.path.basename(path))
        return None
    im = Image.open(path).convert("RGB")
    if crop_unity and im.width == 1058:
        im = im.crop((0, UNITY_BAR, im.width, im.height))
    return im


def save(im, name):
    if im.width > W:
        im = im.resize((W, int(im.height * W / im.width)), Image.LANCZOS)
    im.save(os.path.join(OUT, name))
    made.append(name)
    print("  made", name, im.size)


def before_after(top, bottom, top_label, bottom_label, title, name):
    """2枚を縦に並べ、左上にラベル。幅は揃える。"""
    if top is None or bottom is None:
        skipped.append(name)
        return
    w = 1200
    t = top.resize((w, int(top.height * w / top.width)), Image.LANCZOS)
    b = bottom.resize((w, int(bottom.height * w / bottom.width)), Image.LANCZOS)
    pad, gap, head = 40, 56, 96
    im = Image.new("RGB", (w + pad * 2, head + t.height + gap + b.height + pad), (255, 255, 255))
    dr = ImageDraw.Draw(im)
    dr.text((pad, 34), title, font=font(34, True), fill=INK)
    y = head
    for img, label, col in ((t, top_label, BAD), (b, bottom_label, GOOD)):
        im.paste(img, (pad, y))
        tw = dr.textbbox((0, 0), label, font=font(22, True))[2]
        dr.rectangle((pad, y, pad + tw + 28, y + 40), fill=col)
        dr.text((pad + 14, y + 7), label, font=font(22, True), fill=(255, 255, 255))
        y += img.height + gap
    save(im, name)


def main():
    os.makedirs(OUT, exist_ok=True)

    # 11: スロット本編（現在の幅。QA 用に Unity で描き出したもの）
    game_v2 = load(os.path.join(ROOT, "tools", "adventure-ui-qa", "adventure-v2-1280.png"))
    if game_v2 is not None:
        save(game_v2, "11-unity-game.png")

    # 12: 地図（9/14 のアトリエ風）
    map_v2 = load(os.path.join(SP, "2026-09-14_12h49_08.png"), crop_unity=True)
    if map_v2 is not None:
        save(map_v2, "12-stage-map.png")

    # 22: 冒険画面の前後（9/11 金縁 → 9/12 iPhone 幅）
    game_v1 = load(os.path.join(SP, "2026-09-11_21h52_53.png"), crop_unity=True)
    before_after(game_v1, game_v2,
                 "9月11日  金縁・960×540", "9月12日  iPhone 幅 1170×540・STOP ボタン廃止",
                 "冒険画面。端末の幅を決めたら、枠を全部置き直した", "22-hud-before-after.png")

    # 23: 地図の前後（9/11 ミニマップ → 9/14 アトリエ風）
    map_v1 = load(os.path.join(SP, "2026-09-11_21h36_37.png"), crop_unity=True)
    before_after(map_v1, map_v2,
                 "9月11日  ミニマップ", "9月14日  地図（ドラッグとホイールで動く）",
                 "地図画面。3日で作り直した", "23-map-before-after.png")

    # 25: タイトルの前後（9/11 19:28 惹句あり・赤枠は本人の指示 → 9/11 21:35）
    title_v1 = load(os.path.join(SP, "2026-09-11_19h28_10.png"), crop_unity=True)
    title_v2 = load(os.path.join(SP, "2026-09-11_21h35_15.png"), crop_unity=True)
    before_after(title_v1, title_v2,
                 "19:28  縦書きの惹句あり。赤枠は私が付けた「ここを外して」",
                 "21:35  外したあと。背景も3層に",
                 "タイトル画面。同じ日の2時間", "25-title-before-after.png")

    # 26: リールの配列エディタ（9/8。右の検証結果に「全順:NG」が並んでいる頃）
    reel = load(os.path.join(SP, "2026-09-08_16h54_29.png"))
    if reel is not None:
        save(reel, "26-reel-editor.png")

    # 27: 設定資料から切り出した基準の1枚（9/11）
    ref = load(os.path.join(SP, "2026-09-11_20h54_25.png"))
    if ref is not None:
        save(ref, "27-character-reference.png")

    print("\n作成 %d 件" % len(made))
    if skipped:
        print("スキップ:", ", ".join(skipped))


if __name__ == "__main__":
    main()
