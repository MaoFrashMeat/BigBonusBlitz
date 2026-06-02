import os
import sys
from PIL import Image

def slice_and_transparent(input_path, output_dir):
    try:
        img = Image.open(input_path).convert("RGBA")
        width, height = img.size
        
        # 4等分
        frame_width = width // 4
        
        for i in range(4):
            box = (i * frame_width, 0, (i + 1) * frame_width, height)
            cropped = img.crop(box)
            
            # 背景透過
            datas = cropped.getdata()
            newData = []
            for item in datas:
                # 白背景(240以上)を透過
                if item[0] > 240 and item[1] > 240 and item[2] > 240:
                    newData.append((255, 255, 255, 0))
                else:
                    newData.append(item)
                    
            cropped.putdata(newData)
            
            output_path = os.path.join(output_dir, f"popora_idle_{i+1}.png")
            cropped.save(output_path, "PNG")
            print(f"Saved {output_path}")
            
    except Exception as e:
        print(f"Error: {e}")

slice_and_transparent(r"C:\Users\sato_takuma\.gemini\antigravity\brain\d5be8c52-7899-4d13-bff1-47e8f29fc48c\popora_true_idle_spritesheet_1780366629278.png", r"d:\GitHub\BigBonusBlitz\assets")
