import sys
from PIL import Image

def remove_sky(input_path, output_path):
    try:
        img = Image.open(input_path).convert("RGBA")
        width, height = img.size
        pixels = img.load()
        
        for x in range(width):
            for y in range(height):
                r, g, b, a = pixels[x, y]
                # Check if it's a blue sky color (high blue, low red)
                if b > 150 and r < 100 and g < 150:
                    pixels[x, y] = (255, 255, 255, 0)
                else:
                    # Once we hit non-sky (grass/ground), stop going down for this column
                    # to protect any blue elements (like blue flowers) on the ground
                    break
                    
        img.save(output_path, "PNG")
        print(f"Successfully removed sky from {input_path} -> {output_path}")
    except Exception as e:
        print(f"Error processing {input_path}: {e}")

if __name__ == "__main__":
    if len(sys.argv) > 2:
        remove_sky(sys.argv[1], sys.argv[2])
    else:
        print("Usage: python script.py input.png output.png")
