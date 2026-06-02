import sys
from PIL import Image

def crop_and_transparent(input_path):
    img = Image.open(input_path).convert("RGBA")
    width, height = img.size
    
    # 3分割する
    w = width // 3
    for i in range(3):
        box = (i * w, 0, (i + 1) * w, height)
        cropped = img.crop(box)
        
        # 背景透過
        datas = cropped.getdata()
        newData = []
        for item in datas:
            if item[0] > 240 and item[1] > 240 and item[2] > 240:
                newData.append((255, 255, 255, 0))
            else:
                newData.append(item)
                
        cropped.putdata(newData)
        output_path = f"d:\\GitHub\\BigBonusBlitz\\scratch\\popora_view_{i}.png"
        cropped.save(output_path, "PNG")

crop_and_transparent(r"C:\Users\sato_takuma\.gemini\antigravity\brain\d5be8c52-7899-4d13-bff1-47e8f29fc48c\popora_reference_sheet_fixed_1780364044006.png")
print("Cropped into 3 parts.")
