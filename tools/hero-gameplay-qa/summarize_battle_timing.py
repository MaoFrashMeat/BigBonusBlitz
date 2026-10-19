#!/usr/bin/env python3
"""Summarize controlled isolated-Unity battle timeline CSVs without changing gameplay."""
from __future__ import annotations

import csv
import json
import sys
from collections import defaultdict
from pathlib import Path


def first_time(rows, predicate):
    for row in rows:
        if predicate(row):
            return float(row["unitySeconds"])
    return None


def delta(later, earlier):
    return round(later - earlier, 4) if later is not None and earlier is not None and later >= earlier else None


def summarize(source: Path) -> dict:
    with source.open(newline="", encoding="utf-8-sig") as stream:
        groups = defaultdict(list)
        for row in csv.DictReader(stream):
            groups[row["scenario"]].append(row)

    spins = []
    for name, rows in groups.items():
        rows.sort(key=lambda row: float(row["unitySeconds"]))
        start = first_time(rows, lambda row: row["event"] == "before-bet")
        bet = first_time(rows, lambda row: row["event"] == "bet")
        evaluate = first_time(rows, lambda row: row["event"] == "evaluate-complete")
        contact = first_time(rows, lambda row: row["event"] == "hero-cue:contact")
        enemy_strike = first_time(rows, lambda row: row["enemyState"] == "strike")
        enemy_response = first_time(rows, lambda row: row["enemyState"] in {"hit", "defeat"})
        hero_recover = first_time(rows, lambda row: row["heroState"] == "recover")
        hero_rest = first_time(rows, lambda row: contact is not None and float(row["unitySeconds"]) >= contact and row["heroState"] in {"idle", "walk"})
        spins.append({
            "scenario": name,
            "captureFps": sorted({int(row["captureFps"]) for row in rows}),
            "secondsFromBetToEvaluate": delta(evaluate, bet),
            "secondsFromBetToEnemyStrike": delta(enemy_strike, bet),
            "secondsFromEnemyStrikeToHeroHit": delta(first_time(rows, lambda row: row["heroState"] == "hit"), enemy_strike),
            "secondsFromBetToHeroContact": delta(contact, bet),
            "secondsFromHeroContactToEnemyResponse": delta(enemy_response, contact),
            "secondsFromHeroContactToRecover": delta(hero_recover, contact),
            "secondsFromHeroContactToRest": delta(hero_rest, contact),
            "secondsObservedFromBet": delta(rows[-1] and float(rows[-1]["unitySeconds"]), start),
            "rowCount": len(rows),
            "enemyHpAfter": int(rows[-1]["enemyHp"]),
            "enemyActiveAfter": rows[-1]["enemyActive"] == "true",
        })

    return {
        "passed": bool(spins) and all(item["secondsFromBetToEvaluate"] is not None for item in spins),
        "sourceCsv": source.name,
        "sampleCount": len(spins),
        "fixtureDrivenStopReels": True,
        "realHardwareInput": False,
        "audioListeningVerified": False,
        "productionIntegrated": False,
        "finalArtApproved": False,
        "spins": spins,
    }


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: summarize_battle_timing.py battle-timeline.csv", file=sys.stderr)
        return 2
    source = Path(sys.argv[1]).resolve()
    if not source.is_file():
        print(f"missing timeline: {source}", file=sys.stderr)
        return 2
    result = summarize(source)
    target = source.with_name("battle-timing-summary.json")
    target.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"passed": result["passed"], "sampleCount": result["sampleCount"], "output": str(target)}))
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
