import sys
from PIL import Image

def process_sprite(input_path, output_path):
    img = Image.open(input_path).convert("RGBA")
    datas = img.getdata()
    newData = []
    for item in datas:
        if item[0] > 240 and item[1] > 240 and item[2] > 240:
            newData.append((255, 255, 255, 0))
        else:
            newData.append(item)
    img.putdata(newData)
    img.save(output_path, "PNG")
    print(f"Dimensions: {img.width}x{img.height}")

process_sprite(r"C:\Users\sato_takuma\.gemini\antigravity\brain\d5be8c52-7899-4d13-bff1-47e8f29fc48c\popora_idle_spritesheet_1780364455702.png", r"d:\GitHub\BigBonusBlitz\assets\popora_idle_spritesheet.png")
