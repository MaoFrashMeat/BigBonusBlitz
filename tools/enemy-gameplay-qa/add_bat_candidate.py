"""Append the generated bat candidate without replacing other enemy definitions."""
import copy, hashlib, json, shutil
from pathlib import Path

root = Path(__file__).resolve().parents[2]
assets = root / 'UnityProject/BigBonusBlitz/Assets'
record = root / 'docs/art/2026-11-03/bat-motion-v1'
source = record / 'source.png'
target = assets / 'Resources/Art/HeroPrototype/enemy-bat-v1.png'
shutil.copy2(source, target)
path = assets / 'Resources/Data/enemy_presentation_prototype.json'
data = json.loads(path.read_text(encoding='utf-8-sig'))
assert not any(e['id'] == 'bat' for e in data['enemies']), 'Bat already exists; review instead of overwriting'
bat = copy.deepcopy(data['enemies'][0])
bat.update(id='bat', atlas='enemy-bat-v1', referenceHeight=284, displayHeight=120, gameplayHeight=120, shadowWidth=65)
bat['poses'] = [dict(x=(i%4)*384,y=(i//4)*512,w=384,h=512,foot=112 if i!=7 else 103,offsetX=-245 if i==4 else (12 if i==6 else 0),offsetY=0 if i==7 else (24 if i==4 else 60)) for i in range(8)]
durations = {'idle':[.18,.18],'attack':[.2,.22,.14,.26],'ready':[.2,.22],'strike':[.14,.26]}
for motion in bat['motions']:
    if motion['id'] in durations: motion['seconds'] = durations[motion['id']]
data['enemies'].append(bat)
# Keep the new art in the preview until the Unity runtime probe has passed.
path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(record/'manifest.json').write_text(json.dumps({'date':'2026-11-03','generator':'built-in image_gen','source':'source.png','prompt':'prompt.txt','sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'unityResource':'Art/HeroPrototype/enemy-bat-v1','status':'candidate','finalArtApproved':False},indent=2)+'\n',encoding='utf-8')
