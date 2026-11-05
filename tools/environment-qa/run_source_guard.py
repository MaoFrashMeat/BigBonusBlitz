"""Read-only source gate for the 30-stage, three-layer adventure backdrop."""
import argparse
import hashlib
import json
from pathlib import Path


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def run(root: Path) -> dict:
    data = root / "UnityProject/BigBonusBlitz/Assets/Resources/Data"
    env_path = data / "adventure_environments.json"
    env = json.loads(env_path.read_text(encoding="utf-8-sig"))
    stages = env["stages"]
    assert len(stages) == 30, "stage count drifted"
    assert len({stage["id"] for stage in stages}) == 30, "stage IDs are not unique"
    assert all(stage.get("atlas") and "hour" in stage and "weather" in stage for stage in stages)
    c1 = root / "UnityProject/BigBonusBlitz/Assets/Resources/Art/Adventure/Modules/C-1"
    layers = [c1 / name for name in ("far.png", "middle.png", "near.png")]
    assert all(path.is_file() for path in layers), "C-1 layer source missing"
    fresh_validation_path = root / "tools/environment-qa/captures/validation.json"
    existing_validation_path = fresh_validation_path if fresh_validation_path.is_file() else root / "tools/environment-qa/validation.json"
    existing_validation = json.loads(existing_validation_path.read_text(encoding="utf-8"))
    source_validation = json.loads((root / "tools/environment-qa/source-validation.json").read_text(encoding="utf-8"))
    seam_validation = json.loads((root / "tools/environment-qa/seam-validation.json").read_text(encoding="utf-8"))
    assert existing_validation["passed"] and existing_validation["stages"] == 30
    assert existing_validation["illustratedPlanesPerStage"] == 3
    assert existing_validation["indoorWeatherGuard"] and not existing_validation["saveDataTouched"]
    assert all(layer in source_validation for layer in ("far", "middle", "near"))
    assert seam_validation["passed"]
    return {
        "passed": True,
        "stageCount": len(stages),
        "c1Layers": [path.name for path in layers],
        "timeOfDayPresets": existing_validation["timeOfDayPresets"],
        "weatherPresets": existing_validation["weatherPresets"],
        "renderValidation": existing_validation,
        "sourceValidation": source_validation,
        "seamValidation": seam_validation,
        "saveDataTouched": False,
        "artGate": {"finalArtApproved": False, "note": "Source/integration gate only; URP, device, build and final-art approval remain open."},
        "sourceHashes": {"environments": sha256(env_path), **{f"c1-{path.stem}": sha256(path) for path in layers}},
        "renderValidationSource": str(existing_validation_path.relative_to(root)).replace("\\", "/"),
    }


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    result = run(root)
    text = json.dumps(result, ensure_ascii=False, indent=2) + "\n"
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(text, encoding="utf-8")
    print(text, end="")
