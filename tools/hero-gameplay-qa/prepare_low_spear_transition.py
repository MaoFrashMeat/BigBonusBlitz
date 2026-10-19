"""Build a QA-only low-spear transition candidate from untouched imagegen outputs."""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "docs/art/2026-10-08"
BASE = ROOT / "docs/art/2026-10-04/spear-cycle-v2-qa.json"
GENERATED = Path("C:/Users/ts081/.codex/generated_images/01a0918e-f596-7aa0-b6a0-25dbb4e2f317")
QA = Path("C:/Users/ts081/Documents/Codex/2026-09-12/new-chat/work/shop-qa")

ASSETS = {
    "start": ("exec-319db680-5866-4ae8-9ca8-202e6076d6c8.png", "spear-low-inbetween-v1.png",
               "spear-low-inbetween-v1-prompt.txt", 820, 615, 1150, 725),
    "return": ("exec-fa3a3ffb-da5c-464a-aa4d-78dee6d907d2.png", "spear-low-retract-v1.png",
               "spear-low-retract-v1-prompt.txt", 790, 580, 1100, 680),
}

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    cfg = json.loads(BASE.read_text(encoding="utf-8-sig"))
    ledger = []
    for kind, (raw_name, project_name, prompt_name, grip_x, grip_y, support_x, support_y) in ASSETS.items():
        raw = GENERATED / raw_name
        project = ART / project_name
        prompt = ART / prompt_name
        assert raw.is_file() and project.is_file() and prompt.is_file()
        assert sha(raw) == sha(project)
        with Image.open(project) as image:
            assert image.size == (1312, 1199) and image.mode == "RGBA"
            bbox = image.getchannel("A").point(lambda a: 255 if a > 64 else 0).getbbox()
        pose_index = 33 if kind == "start" else 35
        cfg["poses"][pose_index] = dict(atlas=f"spear-low-{kind}-v1-qa", x=0, y=0,
            w=1306, h=1199, foot=1199-bbox[3], height=1145,
            gripX=grip_x, gripY=grip_y, supportX=support_x, supportY=support_y,
            swordAngle=-55, spearAngle=-80, twoHanded=True)
        shutil.copyfile(project, QA / f"Assets/Resources/Art/HeroPrototype/spear-low-{kind}-v1-qa.png")
        ledger.append(dict(name=kind, source=str(raw), file=str(project),
            prompt=str(prompt), promptText=prompt.read_text(encoding="utf-8"),
            sha256=sha(project), size=[1312,1199], alphaBounds64=bbox,
            tool="built-in image_gen", pixelEdits=False, candidatePose=pose_index))
    cfg["status"] = "QA candidate only: low spear pre-contact and recovery inbetweens; main remains unchanged."
    out = ART / "spear-low-transition-v1-qa.json"
    out.write_text(json.dumps(cfg, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (ART / "spear-low-transition-assets.json").write_text(json.dumps(ledger, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {out} and {len(ledger)} provenance records")

if __name__ == "__main__":
    main()
