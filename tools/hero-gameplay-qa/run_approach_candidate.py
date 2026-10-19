"""Run the isolated Unity approach comparison without changing the main project."""
import argparse
import os
import subprocess
from pathlib import Path

UNITY = Path("C:/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor/Unity.exe")
QA = Path("C:/Users/ts081/Documents/Codex/2026-09-12/new-chat/work/shop-qa")

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--candidate", required=True, type=Path)
    ap.add_argument("--output", required=True, type=Path)
    args = ap.parse_args()
    out = args.output.resolve()
    out.mkdir(parents=True, exist_ok=False)
    env = dict(os.environ, HERO_SPEAR_CANDIDATE=str(args.candidate.resolve()),
               HERO_GAMEPLAY_OUTPUT=str(out))
    si = subprocess.STARTUPINFO()
    si.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    si.wShowWindow = 0
    result = subprocess.run([str(UNITY), "-batchmode", "-projectPath", str(QA),
        "-executeMethod", "AdventureHeroValidation.RunApproachComparison", "-quit",
        "-logFile", str(out / "unity.log")], env=env, startupinfo=si, timeout=600)
    print(f"Unity exit={result.returncode}")
    raise SystemExit(result.returncode)

if __name__ == "__main__":
    main()
