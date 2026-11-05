"""Author the 20-enemy roster using archived PNGs without raster processing.

Source alpha is inspected only to choose non-overlapping sprite rectangles.
All statistical tables are cloned explicitly from an existing variant; art and
regional selection do not introduce new defeat or payout rates.
"""
import copy
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / 'UnityProject/BigBonusBlitz/Assets/Resources/Data'
ART = DATA.parent / 'Art/HeroPrototype'
def read(name): return json.loads((DATA / name).read_text(encoding='utf-8-sig'))
def write(name, value): (DATA / name).write_text(json.dumps(value, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
def upsert(items, item):
    for i, old in enumerate(items):
        if old['id'] == item['id']:
            items[i] = item
            return
    items.append(item)

# id, source variant, region, screen height, hover, windup1/2, impact, recovery.
SPECS = [
    ('boar','B',['B-1','C-4'],145,0,.28,.32,.14,.30),
    ('vine','A',['C-4','E-8'],175,0,.30,.34,.18,.28),
    ('beetle','B',['D-2','E-7'],120,0,.32,.36,.16,.32),
    ('wisp','C',['D-3','E-3'],155,45,.20,.28,.15,.25),
    ('miner','B',['D-1','E-1'],180,0,.28,.36,.17,.30),
    ('crystal','A',['E-2','E-4'],195,0,.22,.26,.13,.24),
    ('book','C',['E-3','F-2'],140,45,.34,.40,.22,.30),
    ('gargoyle','B',['E-5','F-1'],165,0,.32,.38,.18,.34),
    ('sentinel','A',['F-1','F-3'],205,0,.24,.30,.16,.28),
    ('salamander','A',['E-6','G-2'],100,0,.24,.30,.18,.26),
    ('bone','C',['G-1','G-3'],175,65,.20,.26,.14,.24),
    ('ogre','boss_ogre',None,175,0,.40,.50,.20,.38),
    ('wyvern','boss_wyvern',None,145,0,.36,.48,.18,.36),
    ('slimeking','boss_slimeking',None,155,0,.38,.44,.20,.36),
    ('mirrorwarden','boss_wyvern',['E-4','F-2','F-3'],190,35,.38,.46,.20,.34),
]
enemy = read('enemy_presentation_prototype.json')
hero = read('hero_presentation_prototype.json')
roster = read('enemy_roster_p2.json')
tables = read('enemy_tables.json')
originals = {t['id']: copy.deepcopy(t) for t in tables['tables'] if not t.get('stageIds')}
base = copy.deepcopy(next(e for e in enemy['enemies'] if e['id']=='bat'))
for name, variant, stages, height, hover, wind1, wind2, strike, recover in SPECS:
    boss = variant.startswith('boss_')
    table_id = 'boss_'+name if boss else 'table_'+name+'_1'
    type_id = 'boss_'+name if boss else name
    entry = next(e for e in roster['entries'] if e['id']==table_id)
    folder = ROOT/'docs/art/2026-11-04'/(name+'-motion-v1')
    source = folder/'source.png'
    image = Image.open(source)
    assert image.size == (1536,1024) and image.mode=='RGBA', name
    alpha=image.getchannel('A')
    rows=[]
    for row in range(2):
        counts=[sum(alpha.getpixel((x,y))>=32 for y in range(row*512,(row+1)*512)) for x in range(1536)]
        boundaries=[0]
        for boundary in (384,768,1152):
            gaps=[x for x in range(boundary-28,boundary+29) if counts[x]==0]
            assert gaps, (name,row,boundary,'No transparent column')
            # Keep an exact equal split if clear, otherwise choose the middle of the gap.
            boundaries.append(boundary if counts[boundary]==0 else gaps[len(gaps)//2])
        rows.append(boundaries+[1536])
    look=copy.deepcopy(base)
    bbox=alpha.crop((0,0,384,512)).point(lambda x:255 if x>=32 else 0).getbbox()
    ref=bbox[3]-bbox[1]
    look.update(id=type_id,atlas='enemy-'+name+'-v1',referenceHeight=ref,
                displayHeight=height,gameplayHeight=height,shadowWidth=90 if not boss else 125)
    for i,pose in enumerate(look['poses']):
        x=rows[i//4][i%4]; right=rows[i//4][i%4+1]; y=i//4*512
        box=alpha.crop((x,y,right,y+512)).point(lambda a:255 if a>=32 else 0).getbbox()
        foot=512-box[3] if hover==0 or i==7 else 100
        offset=(x+(right-x)/2-(i%4*384+192))*height/ref
        pose.update(x=x,y=y,w=right-x,h=512,foot=foot,
                    offsetX=offset+(-220 if i==4 else 12 if i==6 else 0),offsetY=hover if i<7 else 0)
    if name=='wyvern':
        look['poses'][3]['offsetY']=20;look['poses'][4]['offsetY']=0
    timing={'idle':[.32,.32], 'attack':[wind1,wind2,strike,recover],
            'ready':[wind1,wind2], 'strike':[strike,recover], 'hit':[.18,recover]}
    for motion in look['motions']:
        if motion['id'] in timing: motion['seconds']=timing[motion['id']]
    upsert(enemy['enemies'],look)
    upsert(hero['enemies'],dict(id=type_id,name=entry['name'],atlas=look['atlas'],x=0,y=0,w=384,h=512,height=height))
    shutil.copyfile(source,ART/(look['atlas']+'.png'))
    source_id=variant if boss else {'A':'table_slime_1','B':'table_goblin_1','C':'table_bat_1'}[variant]
    table=copy.deepcopy(originals[source_id])
    table.update(id=table_id,name=entry['name'],enemyType=type_id,selectionWeight=1)
    if stages: table['stageIds']=stages
    if name=='mirrorwarden': table['scale']=1.25
    upsert(tables['tables'],table)
    entry.update(gameplayType=type_id,presentationId=type_id,artStatus='generated',motionStatus='integrated-pending-qa',encounterTableExists=True)
    manifest=json.loads((folder/'manifest.json').read_text())
    manifest.update(unityResource='Art/HeroPrototype/'+look['atlas'],rectangles=look['poses'],status='integrated-pending-qa')
    (folder/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')

for table_id,type_id,variant,stages in [('table_wolf_1','forest_wolf','B',['B-2','C-1']),('table_moth_1','lantern_moth','C',['C-1','D-3'])]:
    entry=next(e for e in roster['entries'] if e['id']==table_id)
    table=copy.deepcopy(originals[{'B':'table_goblin_1','C':'table_bat_1'}[variant]])
    table.update(id=table_id,name=entry['name'],enemyType=type_id,stageIds=stages,selectionWeight=1)
    upsert(tables['tables'],table)
    entry.update(gameplayType=type_id,presentationId=type_id,motionStatus='integrated-pending-qa',encounterTableExists=True)
enemy['gameplayTypes']=[e.get('gameplayType') for e in roster['entries']]
assert len(enemy['enemies'])==len(tables['tables'])==len(set(enemy['gameplayTypes']))==20
roster['status']='runtime-integrated-pending-qa'
roster['integrationRule']='All 20 entries have dedicated motion atlases and encounter tables. Regional candidates preserve the source variant statistics. Original six IDs remain. Validation and final art approval are recorded separately.'
write('enemy_presentation_prototype.json',enemy)
write('hero_presentation_prototype.json',hero)
write('enemy_roster_p2.json',roster)
write('enemy_tables.json',tables)
print('Authored 20 enemy types and encounter tables. Unity visual/gameplay QA required.')
