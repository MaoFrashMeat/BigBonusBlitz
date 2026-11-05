"""Copy unchanged generated PNGs and author Unity sprite/motion metadata."""
import copy
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / 'UnityProject/BigBonusBlitz/Assets/Resources/Data'
ART = DATA.parent / 'Art/HeroPrototype'

def read(name):
    return json.loads((DATA / name).read_text(encoding='utf-8-sig'))

def write(name, value):
    (DATA / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

def upsert(items, item):
    for i, existing in enumerate(items):
        if existing['id'] == item['id']:
            items[i] = item
            return
    items.append(item)

for source, target in [('moth-motion-v1', 'enemy-moth-v1'), ('weapons-five-v1', 'weapons-five-v1')]:
    shutil.copyfile(ROOT / 'docs/art/2026-11-03' / source / 'source.png', ART / (target + '.png'))

enemy = read('enemy_presentation_prototype.json')
moth = copy.deepcopy(next(e for e in enemy['enemies'] if e['id'] == 'bat'))
moth.update(id='lantern_moth', atlas='enemy-moth-v1', referenceHeight=380, displayHeight=150, gameplayHeight=150, shadowWidth=65)
for i, pose in enumerate(moth['poses']):
    pose.update(foot=82 if i < 7 else 66, offsetX=0, offsetY=45 if i < 7 else 0)
# Recovery's wing crosses x=768. The gap between opaque silhouettes is x=771..794.
moth['poses'][5].update(x=384, w=398, offsetX=7 * 150 / 380)
moth['poses'][6].update(x=782, w=370, offsetX=12 - 7 * 150 / 380)
moth['poses'][4]['offsetX'] = -210
durations = {'idle': [.24, .24], 'attack': [.32, .38, .18, .30], 'ready': [.32, .38], 'strike': [.18, .30]}
for motion in moth['motions']:
    if motion['id'] in durations:
        motion['seconds'] = durations[motion['id']]
upsert(enemy['enemies'], moth)
write('enemy_presentation_prototype.json', enemy)

hero = read('hero_presentation_prototype.json')
upsert(hero['enemies'], dict(id='lantern_moth', name='灯蛾', atlas='enemy-moth-v1', x=0, y=0, w=384, h=512, height=150))
weapons = [
    dict(id='w_hammer', x=60, y=65, w=286, h=880, gripX=143, gripY=575, height=230),
    dict(id='w_dagger', x=425, y=300, w=124, h=455, gripX=62, gripY=345, height=110),
    dict(id='w_bow', x=675, y=16, w=190, h=980, gripX=69, gripY=479, height=230),
    dict(id='w_staff', x=992, y=16, w=180, h=984, gripX=83, gripY=634, height=260),
    dict(id='w_holy', x=1255, y=75, w=248, h=860, gripX=124, gripY=685, height=205),
]
for weapon in weapons:
    weapon['atlas'] = 'weapons-five-v1'
    if weapon['id'] in ('w_dagger', 'w_holy'):
        weapon['foregroundGrip'] = True
        upsert(hero['weapons'], weapon)
# Dagger shares the authored one-handed body keys, with a faster wind-up and recovery.
for motion in list(hero['motions']):
    if not motion.get('weaponId') and motion['id'] in ('attack', 'attack-f1', 'attack-f2', 'attack-f3-4', 'recover'):
        fast = copy.deepcopy(motion)
        fast['weaponId'] = 'w_dagger'
        fast['seconds'] = [round(t * .72, 4) for t in fast['seconds']]
        hero['motions'] = [m for m in hero['motions'] if not (m['id'] == fast['id'] and m.get('weaponId') == 'w_dagger')]
        hero['motions'].append(fast)
write('hero_presentation_prototype.json', hero)

catalog = read('weapon_visuals.json')
catalog['integrationRule'] = 'Runtime connects blade, spear, dagger and holy sword. Hammer, bow and staff have generated art and authored source rectangles; dedicated motion validation is pending.'
for entry in catalog['entries']:
    matching = next((w for w in weapons if w['id'] == entry['id']), None)
    if matching:
        entry.update(matching)
        entry['artStatus'] = 'integrated' if entry['id'] in ('w_dagger', 'w_holy') else 'candidate'
        entry['dedicatedArtApproved'] = False
write('weapon_visuals.json', catalog)
roster = read('enemy_roster_p2.json')
for entry in roster['entries']:
    if entry['id'] == 'table_bat_1':
        entry.update(motionStatus='integrated', artStatus='generated-integrated')
    if entry['id'] == 'table_moth_1':
        entry.update(motionStatus='preview-only', artStatus='preview-only', presentationId='lantern_moth')
write('enemy_roster_p2.json', roster)
