import os
from PIL import Image

def generate_9_frames(input_path, output_dir):
    img = Image.open(input_path).convert("RGBA")
    
    # 背景透過処理
    datas = img.getdata()
    newData = []
    for item in datas:
        if item[0] > 240 and item[1] > 240 and item[2] > 240:
            newData.append((255, 255, 255, 0))
        else:
            newData.append(item)
    img.putdata(newData)
    
    width, height = img.size
    
    # 呼吸のアニメーション比率 (1 -> 5 で沈み込み、 6 -> 9 で戻る)
    # y_scale, x_scale, y_shift
    scales = [
        (1.000, 1.000, 0), # 1
        (0.995, 1.002, 1), # 2
        (0.990, 1.004, 2), # 3
        (0.985, 1.006, 3), # 4
        (0.980, 1.008, 4), # 5 (Max squash)
        (0.985, 1.006, 3), # 6
        (0.990, 1.004, 2), # 7
        (0.995, 1.002, 1), # 8
        (1.000, 1.000, 0)  # 9
    ]
    
    for i, (y_s, x_s, y_shift) in enumerate(scales):
        frame_idx = i + 1
        
        # リサイズ
        new_w = int(width * x_s)
        new_h = int(height * y_s)
        resized = img.resize((new_w, new_h), Image.Resampling.LANCZOS)
        
        # 元のキャンバスサイズに下寄せで配置 (y_shiftで少し下げる)
        canvas = Image.new("RGBA", (width, height), (0,0,0,0))
        paste_x = (width - new_w) // 2
        paste_y = height - new_h + y_shift
        
        canvas.paste(resized, (paste_x, paste_y))
        
        out_path = os.path.join(output_dir, f"popora_idle_{frame_idx}.png")
        canvas.save(out_path, "PNG")
        print(f"Generated {out_path}")

generate_9_frames(r"C:\Users\sato_takuma\.gemini\antigravity\brain\d5be8c52-7899-4d13-bff1-47e8f29fc48c\media__1780371706232.jpg", r"d:\GitHub\BigBonusBlitz\assets")
