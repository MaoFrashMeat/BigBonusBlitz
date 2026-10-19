"""Install a candidate only in isolated shop-qa, run reels regression, and restore in finally."""
import argparse
import hashlib
import json
import os
import subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
QA=Path('C:/Users/ts081/Documents/Codex/2026-09-12/new-chat/work/shop-qa')
UNITY=Path('C:/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor/Unity.exe')
REL=Path('Assets/Resources/Data/hero_presentation_prototype.json')

def sha(data):
    return hashlib.sha256(data).hexdigest()

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--candidate',required=True,type=Path)
    parser.add_argument('--output',required=True,type=Path)
    parser.add_argument('--test-filter',default='BBB.Tests.AdventureHeroPlayTests;BBB.Tests.AdventureEnemyReelTests')
    args=parser.parse_args()
    output=args.output.resolve();output.mkdir(parents=True,exist_ok=False)
    target=QA/REL; mainfile=ROOT/'UnityProject/BigBonusBlitz'/REL
    assert '/work/shop-qa/' in target.as_posix()
    original=target.read_bytes();mainbefore=mainfile.read_bytes()
    assert sha(original)==sha(mainbefore),'QA config must start matching current main'
    candidate=args.candidate.resolve().read_bytes()
    (output/'qa-config-before.json').write_bytes(original)
    (output/'candidate-config.json').write_bytes(candidate)
    env=dict(os.environ,ENEMY_REEL_OUTPUT=str(output/'reels'))
    info=subprocess.STARTUPINFO();info.dwFlags|=subprocess.STARTF_USESHOWWINDOW;info.wShowWindow=0
    result=None
    try:
        target.write_bytes(candidate)
        result=subprocess.run([str(UNITY),'-batchmode','-projectPath',str(QA),'-runTests',
            '-testPlatform','EditMode','-testFilter',args.test_filter,
            '-testResults',str(output/'reels.xml'),'-logFile',str(output/'unity.log')],
            env=env,startupinfo=info,timeout=600)
    finally:
        # Preserve unexpected concurrent edits instead of silently overwriting them.
        current=target.read_bytes()
        if sha(current)!=sha(candidate):
            (output/'unexpected-qa-config.json').write_bytes(current)
            raise RuntimeError('QA config changed concurrently; original backup retained for review')
        target.write_bytes(original)
        report=dict(restored=True,matchesMain=sha(target.read_bytes())==sha(mainfile.read_bytes()),
            mainUnchanged=sha(mainbefore)==sha(mainfile.read_bytes()),candidateSha256=sha(candidate),
            restoredSha256=sha(original),unityExitCode=None if result is None else result.returncode)
        (output/'qa-restore.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
        print(json.dumps(report))
    if result.returncode:
        raise SystemExit(result.returncode)

if __name__=='__main__':
    main()
