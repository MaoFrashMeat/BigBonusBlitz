"""Archive original image-generation outputs; inspect alpha without editing pixels."""
import argparse
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image

p = argparse.ArgumentParser()
p.add_argument('name')
p.add_argument('source')
p.add_argument('--date', default='2026-11-04')
p.add_argument('--suffix', default='motion-v1')
a = p.parse_args()
root = Path(__file__).resolve().parents[2]
folder = root / 'docs/art' / a.date / (a.name + '-' + a.suffix)
folder.mkdir(parents=True, exist_ok=True)
target = folder / 'source.png'
if target.exists() and target.read_bytes() != Path(a.source).read_bytes():
    raise SystemExit('Refusing to replace an archived original')
shutil.copyfile(a.source, target)
im = Image.open(target)
assert im.mode == 'RGBA', 'Actual alpha required'
alpha = im.getchannel('A')
w, h = im.size
cells = []
for i in range(8):
    rect = (i % 4 * w // 4, i // 4 * h // 2, (i % 4 + 1) * w // 4, (i // 4 + 1) * h // 2)
    box = alpha.crop(rect).point(lambda x: 255 if x >= 32 else 0).getbbox()
    cells.append(dict(cell=i, rect=rect, alpha32Bounds=box))
manifest = dict(date=a.date, generator='built-in image_gen', source='source.png', prompt='prompt.txt',
                sha256=hashlib.sha256(target.read_bytes()).hexdigest(), size=im.size,
                alphaRange=alpha.getextrema(), cells=cells, status='candidate', finalArtApproved=False)
(folder / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
print(a.name, im.size, alpha.getextrema(), [c['alpha32Bounds'] for c in cells])
