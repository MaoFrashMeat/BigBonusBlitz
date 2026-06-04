import sys
from PIL import Image

def fit(f):
    img = Image.open(f).convert("RGBA")
    box = img.getbbox()
    if box:
        print(f"{f} box: {box}")
        img.crop(box).save(f)
    else:
        print(f"{f} is completely empty!")

fit('D:/GitHub/BigBonusBlitz/assets/bg_layer2_debris.png')
fit('D:/GitHub/BigBonusBlitz/assets/bg_layer3_woods.png')
fit('D:/GitHub/BigBonusBlitz/assets/bg_layer4_distant.png')
