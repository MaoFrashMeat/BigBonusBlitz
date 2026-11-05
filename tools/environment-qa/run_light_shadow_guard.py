"""Read-only guard that proves the background lighting probes are distinct and complete."""
import argparse
import json
from pathlib import Path

from PIL import Image, ImageStat


def mean_rgb(path: Path):
    with Image.open(path).convert("RGB") as image:
        stat = ImageStat.Stat(image)
        return [round(v, 3) for v in stat.mean]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--evidence", required=True, type=Path)
    ap.add_argument("--output", required=True, type=Path)
    args = ap.parse_args()
    evidence = args.evidence
    validation = json.loads((evidence / "validation.json").read_text(encoding="utf-8"))
    forest = json.loads((evidence / "forest-validation.json").read_text(encoding="utf-8"))
    day = evidence / "forest-Day-Clear.png"
    night = evidence / "forest-Night-Rain.png"
    c1 = evidence / "game-C-1.png"
    required = [day, night, c1]
    assert all(path.is_file() for path in required), "lighting evidence missing"
    day_mean, night_mean = mean_rgb(day), mean_rgb(night)
    delta = round(sum(abs(a - b) for a, b in zip(day_mean, night_mean)) / 3, 3)
    render = validation.get("renderValidation", {})
    result = {
        "passed": bool(validation.get("passed") and forest.get("passed") and render.get("stages") == 30 and delta > 5),
        "stageCount": render.get("stages"),
        "forestCombinations": render.get("forestCombinations"),
        "probes": {"dayClear": day.name, "nightRain": night.name, "c1": c1.name},
        "meanRgb": {"dayClear": day_mean, "nightRain": night_mean},
        "meanLightingDelta": delta,
        "indoorWeatherGuard": render.get("indoorWeatherGuard"),
        "saveDataTouched": render.get("saveDataTouched"),
        "finalArtApproved": False,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False))
    if not result["passed"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
