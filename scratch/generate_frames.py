import os
from PIL import Image

def create_idle_sequence(input_path, output_dir):
    img = Image.open(input_path).convert("RGBA")
    width, height = img.size
    
    # フレーム1: そのまま
    img.save(os.path.join(output_dir, "popora_idle_1.png"))
    
    # フレーム2: 少し沈む (height 98%, width 101%)
    img2 = img.resize((int(width * 1.01), int(height * 0.98)), Image.Resampling.LANCZOS)
    # パディングして元のサイズに合わせる (bottom alignment)
    new_img2 = Image.new("RGBA", (width, height), (0,0,0,0))
    new_img2.paste(img2, ((width - img2.width)//2, height - img2.height))
    new_img2.save(os.path.join(output_dir, "popora_idle_2.png"))
    
    # フレーム3: もっと沈む (height 96%, width 102%)
    img3 = img.resize((int(width * 1.02), int(height * 0.96)), Image.Resampling.LANCZOS)
    new_img3 = Image.new("RGBA", (width, height), (0,0,0,0))
    new_img3.paste(img3, ((width - img3.width)//2, height - img3.height))
    new_img3.save(os.path.join(output_dir, "popora_idle_3.png"))
    
    # フレーム4: 戻る途中 (フレーム2と同じ)
    new_img2.save(os.path.join(output_dir, "popora_idle_4.png"))

create_idle_sequence(r"d:\GitHub\BigBonusBlitz\assets\popora_idle.png", r"d:\GitHub\BigBonusBlitz\assets")
print("Generated 4 idle frames.")
