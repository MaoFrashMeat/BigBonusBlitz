"""Copy original atlases and author sprite/animation data; never modify PNG pixels."""
import json
import shutil
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
DATA=ROOT/'UnityProject/BigBonusBlitz/Assets/Resources/Data'
ART=DATA.parent/'Art/HeroPrototype'
path=DATA/'hero_presentation_prototype.json'
cfg=json.loads(path.read_text(encoding='utf-8-sig'))
definitions={
 'w_hammer':('hero-heavy-v1',[(192,275),(160,210),(165,143),(260,273),(281,287),(265,326),(192,278),(195,267)],
             [(0,0),(165,180),(212,95),(294,300),(313,313),(280,300),(0,0),(0,0)],[-20,0,0,0,0,0,-20,15]),
 'w_bow':('hero-bow-v1',[(300,265),(310,200),(334,201),(335,201),(334,201),(286,236),(300,260),(276,252)],
          [(190,270),(185,230),(183,196),(140,187),(110,268),(190,236),(192,270),(195,240)],[0]*8),
 'w_staff':('hero-staff-v1',[(210,251),(195,198),(184,113),(199,72),(202,72),(185,104),(215,248),(185,181)],
            [(0,0)]*8,[-12,-30,-65,-70,-70,-45,-12,10]),
}
for weapon,(atlas,grips,supports,angles) in definitions.items():
    shutil.copyfile(ROOT/'docs/art/2026-11-04'/atlas/'source.png',ART/(atlas+'.png'))
    existing=next((i for i,p in enumerate(cfg['poses']) if p['atlas']==atlas),None)
    start=len(cfg['poses']) if existing is None else existing
    alpha=Image.open(ART/(atlas+'.png')).getchannel('A')
    for i,((gx,gy),(sx,sy),angle) in enumerate(zip(grips,supports,angles)):
        x,y=i%4*384,i//4*512
        box=alpha.crop((x,y,x+384,y+512)).point(lambda a:255 if a>=32 else 0).getbbox()
        two=weapon=='w_hammer' and i in (1,2,3,4,5)
        patch=lambda px,py:dict(x=int(px)-21,y=int(py)-20,w=42,h=40)
        p=dict(atlas=atlas,x=x,y=y,w=384,h=512,foot=512-box[3],height=380,
               gripX=gx,gripY=gy,swordAngle=angle,spearAngle=angle,twoHanded=two,
               supportX=sx,supportY=sy,swordHand=patch(gx,gy),bowDraw=weapon=='w_bow' and i in (1,2))
        if two:p['supportHand']=patch(sx,sy)
        if existing is None:cfg['poses'].append(p)
        else:cfg['poses'][start+i]=p
    def motion(name,frames,seconds,events=None,**kwargs):
        m=dict(id=name,weaponId=weapon,frames=[start+i for i in frames],seconds=seconds,events=events or ['']*len(frames),**kwargs)
        cfg['motions'][:]=[v for v in cfg['motions'] if not(v['id']==name and v.get('weaponId')==weapon)]
        cfg['motions'].append(m)
    heavy=weapon=='w_hammer'
    motion('idle',[0],[1.6],loop=True)
    motion('attack-f1',[0,1],[.12,.22] if heavy else [.10,.16],['','anticipation'],hold=True)
    motion('attack-f2',[2],[.30 if heavy else .22],hold=True)
    motion('attack-f3-4',[3,4],[.10,.22] if heavy else [.14,.22],['swing','contact'] if heavy else ['contact',''],next='recover')
    motion('attack',[0,1,2,3,4],[.12,.22,.30,.10,.22] if heavy else [.10,.16,.22,.14,.22],['','anticipation','','swing','contact'] if heavy else ['','anticipation','','contact',''],next='recover')
    motion('recover',[5,6],[.24,.18] if heavy else [.16,.12])
    motion('hit',[7],[.26],['hurt'],next='recover')
    motion('focus',[1],[1.2],loop=True)
    motion('guard',[7,0],[.20,.16],['guard',''],next='recover')
    motion('victory',[2,6],[.6,.2],['victory',''],next='recover')
    motion('cast',[2,3,4],[.25,.16,.18],['anticipation','magic',''],next='recover')

catalog=json.loads((DATA/'weapon_visuals.json').read_text(encoding='utf-8-sig'))
for weapon in definitions:
    source=next(e for e in catalog['entries'] if e['id']==weapon)
    fields=['id','atlas','x','y','w','h','gripX','gripY','height']
    w={k:source[k] for k in fields}
    w.update(foregroundGrip=True,alignToSupport=weapon=='w_hammer',ranged=weapon in ('w_bow','w_staff'))
    if weapon=='w_bow':
        w.update(atlas='bow-components-v1',x=380,y=16,w=250,h=992,gripX=169,gripY=490,height=230,
                 bowString=True,stringTopX=32,stringTopY=60,stringBottomX=32,stringBottomY=930,
                 projectileAtlas='bow-components-v1',projectileX=800,projectileY=460,projectileW=670,projectileH=100,
                 projectileWidth=115,flightSeconds=.28)
    if weapon=='w_staff':w.update(projectileWidth=32,flightSeconds=.34)
    cfg['weapons'][:]=[v for v in cfg['weapons'] if v['id']!=weapon]
    cfg['weapons'].append(w)
    source.update(w,artStatus='runtime-integrated-pending-qa',dedicatedArtApproved=False)
shutil.copyfile(ROOT/'docs/art/2026-11-05/bow-components-v1/source.png',ART/'bow-components-v1.png')
cfg['status']='Seven weapon families integrated; isolated Unity verification and final art review pending'
catalog['integrationRule']='Seven runtime weapon families; hammer uses authored two-hand poses, bow has separate animated string and arrow, staff casts from a fixed position. Final art approval remains separate.'
path.write_text(json.dumps(cfg,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(DATA/'weapon_visuals.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
