# -*- coding: utf-8 -*-
"""タイトルの立ち絵（サリアのレイヤーモデル）から note 用の画像を2枚作る。

  py -3 docs/note/build_rig_images.py

  19-title-rig-layers.png   … 15 パーツを並べた一覧（GPT 側で切り分けた素材そのもの）
  20-title-rig-composite.png … model.json の順に重ねた立ち絵

背景と光・花びらの演出は Unity 側にあるので、ここでは描かない（実機画面は本人が撮る）。
"""
import io
import json
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
RIG = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "SaliaRig")
OUT = os.path.join(HERE, "images")

W = 1280
INK = (26, 26, 26)
SUB = (107, 107, 102)
LINE = (216, 216, 212)
FONT_B = r"C:\Windows\Fonts\BIZ-UDGothicB.ttc"
FONT_R = r"C:\Windows\Fonts\BIZ-UDGothicR.ttc"

JP = {
    "body": "胴", "sword": "剣", "hair-back-left": "後ろ髪（左）", "hair-back-right": "後ろ髪（右）",
    "coat-left": "外套（左）", "coat-right": "外套（右）", "skirt": "スカート", "armor": "胸甲",
    "arm-left": "腕（左）", "arm-right": "腕（右）", "head": "頭", "eye-left": "目（左）",
    "eye-right": "目（右）", "hair-crown": "前髪", "ribbon": "リボン",
}
MOTION = {0: "固定", 1: "髪", 2: "服", 3: "呼吸"}


def font(size, bold=False):
    try:
        return ImageFont.truetype(FONT_B if bold else FONT_R, size, index=0)
    except Exception:
        return ImageFont.load_default()


def load_model():
    with io.open(os.path.join(RIG, "model.json"), encoding="utf-8") as f:
        return json.load(f)


def composite(model):
    canvas = Image.new("RGBA", (model["width"], model["height"]), (0, 0, 0, 0))
    for p in model["parts"]:
        path = os.path.join(RIG, p["file"] + ".png")
        if not os.path.exists(path):
            print("  missing", path)
            continue
        layer = Image.open(path).convert("RGBA")
        if layer.size != (p["width"], p["height"]):
            layer = layer.resize((p["width"], p["height"]), Image.LANCZOS)
        canvas.alpha_composite(layer, (p["x"], p["y"]))
    return canvas


def make_composite(model):
    char = composite(model)
    bbox = char.getbbox()
    char = char.crop(bbox)
    # 柔らかい単色の地に置く。背景の絵は Unity 側の演出なのでここでは使わない
    pad = 40
    bg = Image.new("RGB", (char.width + pad * 2, char.height + pad * 2 + 70), (238, 240, 246))
    bg.paste(char, (pad, pad), char)
    dr = ImageDraw.Draw(bg)
    dr.text((pad, char.height + pad + 18),
            "15 パーツを重ねた状態。呼吸・目パチ・髪と服の揺れは Unity 側で動く",
            font=font(26), fill=SUB)
    if bg.width > W:
        bg = bg.resize((W, int(bg.height * W / bg.width)), Image.LANCZOS)
    bg.save(os.path.join(OUT, "20-title-rig-composite.png"))
    print("  made 20-title-rig-composite.png", bg.size)


def make_layers(model):
    parts = model["parts"]
    cols = 5
    rows = (len(parts) + cols - 1) // cols
    cell, pad, lab = 228, 18, 40
    w = pad + cols * (cell + pad)
    h = pad + 70 + rows * (cell + lab + pad)
    im = Image.new("RGB", (w, h), (255, 255, 255))
    dr = ImageDraw.Draw(im)
    dr.text((pad, pad), "タイトルの立ち絵は、15 枚のパーツでできている", font=font(30, True), fill=INK)
    y0 = pad + 70
    fl = font(20, True)
    fs = font(17)
    for i, p in enumerate(parts):
        cx = pad + (i % cols) * (cell + pad)
        cy = y0 + (i // cols) * (cell + lab + pad)
        dr.rounded_rectangle((cx, cy, cx + cell, cy + cell), radius=10,
                             fill=(238, 240, 246), outline=LINE, width=2)
        path = os.path.join(RIG, p["file"] + ".png")
        if os.path.exists(path):
            t = Image.open(path).convert("RGBA")
            t.thumbnail((cell - 20, cell - 20), Image.LANCZOS)
            im.paste(t, (cx + (cell - t.width) // 2, cy + (cell - t.height) // 2), t)
        name = JP.get(p["id"], p["id"])
        dr.text((cx + 6, cy + cell + 8), "%02d  %s" % (i + 1, name), font=fl, fill=INK)
    if im.width > W:
        im = im.resize((W, int(im.height * W / im.width)), Image.LANCZOS)
    im.save(os.path.join(OUT, "19-title-rig-layers.png"))
    print("  made 19-title-rig-layers.png", im.size)


def main():
    if not os.path.isdir(RIG):
        print("SaliaRig が無い:", RIG)
        return
    model = load_model()
    print("parts:", len(model["parts"]), "canvas:", model["width"], "x", model["height"])
    make_layers(model)
    make_composite(model)


if __name__ == "__main__":
    main()
