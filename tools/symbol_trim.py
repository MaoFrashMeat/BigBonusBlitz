"""assets/symbols の各 PNG の「透明な余白を切った矩形」を測って symbol_viewer.html の TRIM に書き込む。
絵を足したら  py -3 tools/symbol_trim.py  を一度走らせる。"""
import glob, io, json, os, re
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
base = os.path.join(ROOT, "assets", "symbols")
rects = {}
for f in sorted(glob.glob(os.path.join(base, "*.png"))) + sorted(glob.glob(os.path.join(ROOT, "tools", "symbol_src", "*_keyed.png"))):
    im = Image.open(f).convert("RGBA")
    a = im.split()[3].point(lambda v: 255 if v > 16 else 0)
    bb = a.getbbox() or (0, 0, im.width, im.height)
    rects[os.path.basename(f)] = {"x": bb[0], "y": bb[1], "w": bb[2] - bb[0], "h": bb[3] - bb[1]}
p = os.path.join(ROOT, "tools", "symbol_viewer.html")
s = io.open(p, encoding="utf-8").read()
s2 = re.sub(r"const TRIM = \{.*?\};", "const TRIM = " + json.dumps(rects, ensure_ascii=False) + ";", s, count=1, flags=re.S)
io.open(p, "w", encoding="utf-8", newline="").write(s2)
print("TRIM", len(rects))
