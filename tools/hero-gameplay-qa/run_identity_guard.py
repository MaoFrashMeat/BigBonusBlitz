"""Read-only QA guard for same-identity sword/spear support-foot and hand contracts."""
import argparse
import json
from pathlib import Path


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--candidate", required=True, type=Path)
    ap.add_argument("--output", required=True, type=Path)
    args = ap.parse_args()
    cfg = json.loads(args.candidate.read_text(encoding="utf-8"))
    poses = cfg.get("poses", [])
    by_atlas = {p.get("atlas"): p for p in poses}
    ids = [p.get("atlas") for p in poses]
    required = [
        "body-v2", "sword-ready-walkmatched-v1-qa", "spear-ready-walkmatched-v1-qa",
        "spear-low-start-v1-qa", "spear-contact-walkmatched-v1-qa",
        "spear-low-return-v1-qa", "spear-start-walkmatched-v1-qa",
    ]
    support = [p for p in poses if p.get("atlas", "").startswith("spear-") and "supportX" in p]
    weapon_ids = [w.get("id") for w in cfg.get("weapons", [])]
    critical = [by_atlas.get(name) for name in required]
    critical_present = all(p is not None for p in critical)
    counts = {name: ids.count(name) for name in required}
    critical_unique = counts.get("body-v2", 0) >= 1 and counts.get("spear-ready-walkmatched-v1-qa") == 2 and all(counts.get(name, 0) == 1 for name in required if name not in ("body-v2", "spear-ready-walkmatched-v1-qa"))
    support_complete = all(p.get("supportX") is not None and p.get("supportY") is not None for p in support)
    foot_values = [by_atlas[name].get("foot") for name in required[1:6] if by_atlas.get(name)]
    foot_bounded = bool(foot_values) and min(foot_values) >= 26 and max(foot_values) <= 36
    grip_complete = all(by_atlas[name].get("gripX") is not None and by_atlas[name].get("gripY") is not None for name in required if by_atlas.get(name))
    passed = critical_present and critical_unique and support_complete and foot_bounded and grip_complete and set(("w_blade", "w_spear")) <= set(weapon_ids)
    result = {
        "passed": passed,
        "candidate": str(args.candidate),
        "criticalPoseContract": critical_present,
        "criticalPoseMultiplicity": {name: counts.get(name, 0) for name in required},
        "supportAnchorsComplete": support_complete,
        "criticalFootRange": {"min": min(foot_values) if foot_values else None, "max": max(foot_values) if foot_values else None, "bounded": foot_bounded},
        "gripCoordinatesComplete": grip_complete,
        "bladeAndSpearCatalogued": set(("w_blade", "w_spear")) <= set(weapon_ids),
        "mainEnabled": False,
        "finalArtApproved": False,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False))
    if not passed:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
