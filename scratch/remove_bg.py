import sys
try:
    from PIL import Image
except ImportError:
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "pillow"])
    from PIL import Image

def remove_white_bg(input_path, output_path):
    img = Image.open(input_path).convert("RGBA")
    datas = img.getdata()
    
    newData = []
    # しきい値240以上なら白とみなして透過
    for item in datas:
        if item[0] > 240 and item[1] > 240 and item[2] > 240:
            newData.append((255, 255, 255, 0))
        else:
            newData.append(item)
            
    img.putdata(newData)
    img.save(output_path, "PNG")

remove_white_bg(r"C:\Users\sato_takuma\.gemini\antigravity\brain\d5be8c52-7899-4d13-bff1-47e8f29fc48c\popora_true_idle_1780364921701.png", r"d:\GitHub\BigBonusBlitz\assets\popora_idle.png")
print("Background removed successfully.")
