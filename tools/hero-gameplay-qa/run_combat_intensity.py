"""Run the isolated Unity combat-intensity band probe and stop only that QA child."""
import argparse
import json
import os
import subprocess
import time
from pathlib import Path

UNITY = Path("C:/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor/Unity.exe")
QA = Path("C:/Users/ts081/Documents/Codex/2026-09-12/new-chat/work/shop-qa")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--output", required=True, type=Path)
    args = ap.parse_args()
    out = args.output.resolve()
    out.mkdir(parents=True, exist_ok=False)
    env = dict(os.environ, HERO_GAMEPLAY_OUTPUT=str(out))
    si = subprocess.STARTUPINFO()
    si.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    si.wShowWindow = 0
    proc = subprocess.Popen(
        [str(UNITY), "-batchmode", "-projectPath", str(QA),
         "-executeMethod", "AdventureHeroValidation.RunCombatIntensityValidation",
         "-logFile", str(out / "unity.log")],
        env=env, startupinfo=si,
    )
    validation = out / "validation.json"
    deadline = time.time() + 600
    try:
        while time.time() < deadline and not validation.exists():
            if proc.poll() is not None:
                raise RuntimeError(f"Unity exited before validation.json: {proc.returncode}")
            time.sleep(1)
        if not validation.exists():
            raise TimeoutError("Timed out waiting for combat intensity validation")
        time.sleep(3)
        data = json.loads(validation.read_text(encoding="utf-8"))
        print(json.dumps(data, ensure_ascii=False))
    finally:
        if proc.poll() is None:
            proc.terminate()
            try:
                proc.wait(timeout=15)
            except subprocess.TimeoutExpired:
                proc.kill()
                proc.wait(timeout=15)
    if not validation.exists() or not json.loads(validation.read_text(encoding="utf-8")).get("passed"):
        raise SystemExit(1)


if __name__ == "__main__":
    main()
