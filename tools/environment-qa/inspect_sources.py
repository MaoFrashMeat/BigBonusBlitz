"""Read-only checks on generated matte sources. Does not modify any image."""
import json
from pathlib import Path
import numpy as np
from PIL import Image

root = Path(__file__).resolve().parents[2]
sources = root / 'UnityProject/BigBonusBlitz/Assets/Resources/Art/Adventure/Modules/C-1'
report = {}
for name in ('far', 'middle', 'near'):
    image = Image.open(sources / f'{name}.png')
    rgb = np.asarray(image.convert('RGB'), dtype=float) / 255
    value = rgb.max(axis=2)
    alpha = np.clip((value - .015) / (.10 - .015), 0, 1)
    alpha = alpha * alpha * (3 - 2 * alpha)
    # Match only the key decoding; renderer also guards the first/last 2%.
    edge = np.concatenate((alpha[:, :2], alpha[:, -2:]), axis=1)
    assert float(edge.max()) < .001, f'{name}: nonempty edge'
    assert (alpha < .001).mean() > .1, f'{name}: background not removed'
    assert (alpha > .99).mean() > .03, f'{name}: missing illustration'
    report[name] = dict(size=image.size, sourceMode=image.mode,
                        decodedEmptyFraction=float((alpha < .001).mean()),
                        decodedOpaqueFraction=float((alpha > .99).mean()),
                        decodedEdgeAlphaMax=float(edge.max()))
output = Path(__file__).parent / 'source-validation.json'
output.write_text(json.dumps(report, indent=2), encoding='utf-8')
print(output.read_text())
