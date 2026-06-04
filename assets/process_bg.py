import sys
from PIL import Image
import math

def remove_magenta(input_path, output_path):
    try:
        img = Image.open(input_path).convert("RGBA")
    except Exception as e:
        print(f"Error opening {input_path}: {e}")
        return

    data = img.getdata()
    new_data = []
    
    for item in data:
        r, g, b, a = item
        # Calculate distance to pure magenta (255, 0, 255)
        dist = math.sqrt((r - 255)**2 + g**2 + (b - 255)**2)
        
        # If very close, completely transparent
        if dist < 60:
            new_data.append((255, 255, 255, 0))
        elif dist < 100:
            # Semi-transparent blending
            alpha = int((dist - 60) / 40 * 255)
            new_data.append((r, g, b, alpha))
        else:
            new_data.append(item)
            
    img.putdata(new_data)
    img.save(output_path, "PNG")
    print(f"Processed {input_path} -> {output_path}")

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python process.py input.png output.png")
    else:
        remove_magenta(sys.argv[1], sys.argv[2])
