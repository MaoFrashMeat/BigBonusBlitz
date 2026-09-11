import sys
from PIL import Image

def crop_bottom_half(input_path, output_path):
    try:
        img = Image.open(input_path)
        width, height = img.size
        # Crop the bottom half (0, height/2, width, height)
        cropped = img.crop((0, int(height/2), width, height))
        cropped.save(output_path)
        print(f"Cropped {input_path} -> {output_path}")
    except Exception as e:
        print(f"Error cropping {input_path}: {e}")

if __name__ == "__main__":
    crop_bottom_half(sys.argv[1], sys.argv[2])
