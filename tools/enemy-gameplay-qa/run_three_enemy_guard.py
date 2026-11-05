"""Read-only source guard for the three authored enemy motion samples."""
import argparse
import json
from pathlib import Path


REQUIRED = {"idle", "attack", "hit", "defeat", "ready", "strike"}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--output", required=True, type=Path)
    args = ap.parse_args()
    root = Path(__file__).resolve().parents[2]
    data = root / "UnityProject/BigBonusBlitz/Assets/Resources/Data"
    presentation = json.loads((data / "enemy_presentation_prototype.json").read_text(encoding="utf-8-sig"))
    roster = json.loads((data / "enemy_roster_p2.json").read_text(encoding="utf-8-sig"))
    expected = ["slime", "goblin", "forest_wolf"]
    defs = {e.get("id"): e for e in presentation.get("enemies", [])}
    motion_ids = {key: {m.get("id") for m in value.get("motions", [])} for key, value in defs.items()}
    roster_rows = {e.get("gameplayType") or e.get("presentationId"): e for e in roster.get("entries", [])}
    authored = all(key in defs and REQUIRED <= motion_ids.get(key, set()) for key in expected)
    art_gate = all(roster_rows.get(key, {}).get("dedicatedArtApproved") is False for key in expected)
    gameplay_types = presentation.get("gameplayTypes", [])
    previewBoundary = gameplay_types == ["slime", "goblin"]
    result = {
        "passed": authored and art_gate and previewBoundary,
        "enemyIds": expected,
        "enemyDefinitionCount": len(defs),
        "requiredMotions": sorted(REQUIRED),
        "motionCoverage": {key: sorted(motion_ids.get(key, set())) for key in expected},
        "dedicatedArtApproved": {key: roster_rows.get(key, {}).get("dedicatedArtApproved") for key in expected},
        "runtimeGameplayTypes": gameplay_types,
        "forestWolfMainEnabled": "forest_wolf" in gameplay_types,
        "productionIntegrated": False,
        "finalArtApproved": False,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False))
    if not result["passed"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
