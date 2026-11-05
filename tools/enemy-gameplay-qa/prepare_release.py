"""Copy the reviewed adventure change set without touching the shared index."""
import hashlib
import json
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT.parent / '_BigBonusBlitz_release-20261105'
ASSETS = 'UnityProject/BigBonusBlitz/Assets/'
selected = set()

def include(relative):
    path = ROOT / relative
    if not path.exists():
        raise FileNotFoundError(path)
    if path.is_dir():
        selected.update(p.relative_to(ROOT).as_posix() for p in path.rglob('*') if p.is_file())
    else:
        selected.add(relative)
    if Path(str(path) + '.meta').is_file():
        selected.add(relative + '.meta')

for name in ['EnemyEngage', 'GameConfig', 'SlotMachine']:
    include(ASSETS + 'Scripts/BBB.Core/' + name + '.cs')
for name in ['AdventureEnvironmentCatalog', 'AdventurePresentation', 'ArtLoader', 'GameController', 'MarqueeBand', 'ParallaxBackground']:
    include(ASSETS + 'Scripts/BBB.Runtime/' + name + '.cs')
for folder in ['Scripts/BBB.Runtime', 'Editor', 'Tests/EditMode', 'Resources/Data']:
    paths = subprocess.check_output(['git', 'ls-files', '--others', '--exclude-standard', ASSETS + folder], cwd=ROOT, text=True).splitlines()
    for path in paths:
        if path.endswith(('.cs', '.cs.meta', '.json', '.json.meta')):
            include(path)
for name in ['AdventureHeroPlayTests', 'WorkflowAndEnemyTests']:
    include(ASSETS + 'Tests/EditMode/' + name + '.cs')
for name in ['ProbabilityEditorWindow', 'AdventureHeroValidation']:
    include(ASSETS + 'Editor/' + name + '.cs')
for name in ['adventure_environments', 'adventure_presentation', 'enemy_tables', 'notices']:
    include(ASSETS + 'Resources/Data/' + name + '.json')
for folder in ['HeroPrototype', 'UI/AdventureDialogue'] + ['Adventure/Modules/' + id for id in ['A-1', 'B-1', 'B-2', 'C-2', 'C-3', 'C-4', 'D-1', 'D-2', 'D-3', 'D-4', 'D-5', 'D-6', 'E-1', 'E-2', 'H-1']]:
    include(ASSETS + 'Resources/Art/' + folder)
include(ASSETS + 'Resources/Art/Adventure/AdventureSeamless.shader')
for name in ['PROJECT_STATE', 'AI_HANDOFF_HISTORY', 'devlog', 'HERO_PRESENTATION_P2', 'HERO_GAMEPLAY_P2', 'HERO_REEL_BRIDGE_P2', 'HERO_SUPPORT_P2', 'ENEMY_GAMEPLAY_P2', 'ENEMY_PRESENTATION_P2', 'ADVENTURE_PRESENTATION_P1', 'ADVENTURE_REDESIGN_PLAN_2026-09-23', 'ADVENTURE_ENVIRONMENTS', 'BACKGROUND_ART_DIRECTION', 'note/MEMO', 'note/MATERIAL']:
    include('docs/' + name + '.md')
for date in ['2026-11-03', '2026-11-04', '2026-11-05', '2026-11-06']:
    include('docs/art/' + date)
for folder in ['tools/enemy-gameplay-qa', 'tools/environment-qa', 'tools/weapon-gameplay-qa']:
    for path in (ROOT / folder).glob('*'):
        if path.suffix in ['.py', '.cs', '.md', '.ps1']:
            include(path.relative_to(ROOT).as_posix())
for folder in ['tools/enemy-gameplay-qa/2026-11-05/roster-weapons-v3', 'tools/enemy-gameplay-qa/2026-11-05/release-v1', 'tools/enemy-gameplay-qa/2026-11-05/release-v2', 'tools/enemy-gameplay-qa/2026-11-06/remote-guard', 'tools/weapon-gameplay-qa/2026-11-05/families-v2', 'tools/environment-qa/2026-11-05/native-v2', 'tools/environment-qa/2026-11-06/native-v3', 'tools/environment-qa/2026-11-06/native-v4']:
    for path in (ROOT / folder).rglob('*'):
        if path.suffix in ['.json', '.xml']:
            include(path.relative_to(ROOT).as_posix())
for version in ['native-v4', 'native-v5', 'native-v6', 'native-v7']:
    folder = ROOT / 'tools/environment-qa/2026-11-06' / version
    for path in folder.glob('*.json'):
        include(path.relative_to(ROOT).as_posix())
    if version in ['native-v4', 'native-v7']:
        for path in folder.glob('game-*.png'):
            include(path.relative_to(ROOT).as_posix())
for filename in ['w_hammer-attack-030.png', 'w_staff-attack-f2.png', 'w_bow-attack-030.png']:
    include('tools/weapon-gameplay-qa/2026-11-05/families-v2/'+filename)
include('docs/art/2026-11-06/RELEASE_INTEGRATION_REVIEW.md')
for relative in sorted(selected):
    target = DEST / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(ROOT / relative, target)
manifest = {p: hashlib.sha256((DEST / p).read_bytes()).hexdigest() for p in sorted(selected)}
(ROOT / 'scratch/release-20261105-files.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
print(f'Copied {len(selected)} explicit files to {DEST}; shared index untouched.')
