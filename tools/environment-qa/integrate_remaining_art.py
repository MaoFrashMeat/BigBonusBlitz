"""Integrate unchanged generated originals; only author UV/pose metadata."""
import copy
import hashlib
import json
import re
import shutil
import uuid
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
RES = ROOT / 'UnityProject/BigBonusBlitz/Assets/Resources'
ART = ROOT / 'docs/art/2026-11-18'

def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')

def install(source, destination, template):
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(source, destination)
    meta = Path(str(destination)+'.meta')
    if not meta.exists():
        text = Path(str(template)+'.meta').read_text(encoding='utf-8-sig')
        meta.write_text(re.sub(r'guid: [0-9a-f]+', 'guid: '+uuid.uuid4().hex, text, count=1), encoding='utf-8')

env_path = RES / 'Data/adventure_environments.json'
env = json.loads(env_path.read_text(encoding='utf-8-sig'))
report = []
for folder in sorted(ART.glob('environment-*-v1')):
    stage_id = folder.name[len('environment-'):-len('-v1')]
    source = folder / 'source.png'
    image = Image.open(source)
    assert image.size == (1536, 1024) and image.mode == 'RGBA', source
    alpha = image.getchannel('A')
    def gutter(lo, hi):
        runs, run = [], []
        for y in range(lo, hi):
            if alpha.crop((0,y,1536,y+1)).getextrema()[1] < 32:run.append(y)
            elif run:runs.append(run);run=[]
        if run:runs.append(run)
        longest=max(runs,key=len)
        assert len(longest)>=8,(stage_id,'No safe gutter')
        return longest[len(longest)//2]
    cuts = [0, gutter(300,410), gutter(730,825), 1024]
    for cut in cuts[1:-1]:
        assert alpha.crop((0, cut-2, 1536, cut+2)).getextrema()[1] < 32, (stage_id, cut)
    destination = RES / 'Art/Adventure/Modules' / stage_id / 'layers-v1.png'
    install(source, destination, RES / 'Art/Adventure/Modules/E-1/layers-v1.png')
    stage = next(s for s in env['stages'] if s['id'] == stage_id)
    stage['layers'] = []
    bounds = []
    for i in range(3):
        box = alpha.crop((0, cuts[i], 1536, cuts[i+1])).point(lambda a: 255 if a >= 32 else 0).getbbox()
        assert box, (stage_id, i)
        bounds.append(box)
        height = [.8, 1.05, .48][i]
        tail = 1-box[3]/(cuts[i+1]-cuts[i])
        bottom = [.08, .155-height*tail, -.075][i]
        stage['layers'].append(dict(texture='Art/Adventure/Modules/'+stage_id+'/layers-v1', top=cuts[i]/1024,
                                    rowHeight=(cuts[i+1]-cuts[i])/1024, nativeAlpha=True,
                                    speed=[.007,.024,.065][i], phase=[.31,0,.61][i], height=height, bottom=round(bottom,6)))
    stage['groundHeight'] = .235
    palettes={
        'E-3':((.15,.17,.17),(.29,.27,.23)), 'E-4':((.16,.17,.23),(.30,.30,.36)),
        'E-5':((.22,.20,.15),(.37,.32,.24)), 'E-6':((.28,.28,.29),(.42,.42,.39)),
        'E-7':((.20,.18,.10),(.35,.28,.15)), 'E-8':((.10,.18,.16),(.22,.26,.19)),
        'F-1':((.29,.27,.21),(.46,.41,.31)), 'F-2':((.13,.19,.22),(.26,.31,.32)),
        'F-3':((.21,.16,.19),(.36,.27,.25)), 'F-4':((.25,.29,.28),(.42,.44,.38)),
        'F-5':((.21,.21,.21),(.36,.33,.28)), 'G-1':((.16,.13,.17),(.29,.21,.23)),
        'G-2':((.16,.12,.15),(.29,.19,.20)), 'G-3':((.19,.16,.21),(.33,.27,.29))}
    for key,color in zip(('ground','path'),palettes[stage_id]):stage[key]=dict(zip(('r','g','b','a'),(*color,1)))
    # Weather and indoor lighting remain owned by the stage definition.
    report.append(dict(id=stage_id, source=str(source.relative_to(ROOT)), sha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                       cuts=cuts, opaqueBounds=bounds, runtime=str(destination.relative_to(ROOT))))
write(env_path, env)
assert len(report) == 14
write(ART / 'background-manifest.json', report)

hero_path = RES / 'Data/hero_presentation_prototype.json'
hero = json.loads(hero_path.read_text(encoding='utf-8-sig'))
def append_atlas(atlas, columns, grips, supports, offsets, angles, height):
    source = ART / atlas / 'source.png'
    destination = RES / 'Art/HeroPrototype' / (atlas+'.png')
    install(source, destination, RES / 'Art/HeroPrototype/spear-v2.png')
    alpha = Image.open(source).getchannel('A')
    start = next((i for i,p in enumerate(hero['poses']) if p['atlas']==atlas), len(hero['poses']))
    w = 1536//columns
    for i, ((gx,gy),(sx,sy),offset,angle) in enumerate(zip(grips,supports,offsets,angles)):
        x,y = i%columns*w,i//columns*512
        box = alpha.crop((x,y,x+w,y+512)).point(lambda a:255 if a>=32 else 0).getbbox()
        patch = lambda px,py:dict(x=px-18,y=py-17,w=36,h=34)
        p = dict(atlas=atlas,x=x,y=y,w=w,h=512,foot=512-box[3],height=height,
                 gripX=gx,gripY=gy,swordAngle=angle,spearAngle=angle,twoHanded=sx>0,
                 supportX=sx,supportY=sy,offsetX=offset,swordHand=patch(gx,gy))
        if sx>0:p['supportHand']=patch(sx,sy)
        if start+i<len(hero['poses']):hero['poses'][start+i]=p
        else:hero['poses'].append(p)
    return start

walk = append_atlas('hero-spear-walk-v1',4,
    [(220,210),(211,219),(216,209),(222,209),(214,199),(214,211),(215,201),(217,201)],
    [(328,202),(329,209),(329,201),(328,199),(327,199),(328,202),(328,198),(328,198)],
    [0]*8,[-90]*8,440)
base = copy.deepcopy(next(m for m in hero['motions'] if m['id']=='walk' and not m.get('weaponId')))
base.update(weaponId='w_spear',frames=list(range(walk,walk+8)))
hero['motions'][:]=[m for m in hero['motions'] if not(m['id']=='walk' and m.get('weaponId')=='w_spear')]
hero['motions'].append(base)
low = append_atlas('hero-low-contact-v1',3,
    [(207,187),(400,352),(423,351),(243,256),(283,276),(277,280)],
    [(0,0)]*3+[(382,311),(436,307),(441,310)],
    [10,10,0,35,45,35],[35,-100,-98,-110,-105,-105],440)
hero['lowTargetPoses'] = []
for weapon, pairs in [('w_blade',[(23,0),(24,0),(25,0),(26,1),(27,2),(28,2),(29,2)]),
                      ('w_spear',[(31,3),(32,3),(33,4),(34,5),(35,5)])]:
    for original, replacement in pairs:
        hero['lowTargetPoses'].append(dict(weaponId=weapon,original=original,pose=low+replacement))
hero['status']='30 backgrounds integrated; spear gait and planted low-target attack poses pending isolated art review'
write(hero_path, hero)
print('Installed 14 backgrounds, spear walk, six low-target poses:', walk, low)
