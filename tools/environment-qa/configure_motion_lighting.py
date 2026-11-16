import json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
data=root/'UnityProject/BigBonusBlitz/Assets/Resources/Data'
p=data/'adventure_environments.json'
d=json.loads(p.read_text(encoding='utf-8-sig'))
d['lighting']=dict(actorTintWeight=.48,indoorActorTintWeight=.38,shadowOpacity=.6,overcastShadowOpacity=.32,indoorShadowOpacity=.46,shadowWidth=115,shadowHeight=19,lowSunStretch=.6,shadowOffset=24,depthShade=.16,indoorDepthShade=.22)
def lamp(x,y,rx,ry,color,power):return dict(x=x,y=y,radiusX=rx,radiusY=ry,intensity=power,color=dict(zip(('r','g','b','a'),(*color,1))))
for stage in d['stages']:
    if stage['id']=='E-2':
        stage['layers'][1]['lights']=[lamp(.14,.49,.065,.31,(.16,.78,1),.22),lamp(.56,.68,.075,.28,(.15,.75,1),.24),lamp(.90,.37,.065,.25,(.23,.70,1),.20),lamp(.55,.22,.05,.18,(.65,.27,1),.16)]
        stage['layers'][2]['lights']=[lamp(.17,.70,.05,.22,(.2,.65,1),.14),lamp(.84,.72,.05,.22,(.2,.7,1),.16)]
    if stage['id']=='H-1':
        stage['layers'][1]['lights']=[lamp(.25,.64,.11,.22,(1,.39,.07),.22),lamp(.75,.65,.12,.20,(1,.39,.07),.22)]
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
p=data/'hero_presentation_prototype.json';d=json.loads(p.read_text(encoding='utf-8-sig'));d['approach']['returnWalkStart']=.25
d['approach']['returnWalkCycles']=.5
for motion in d['motions']:
    if motion['id']=='recover':
        durations={None:[.14,.40],'w_spear':[.16,.40],'w_dagger':[.12,.32],'w_hammer':[.24,.46]}
        if motion.get('weaponId') in durations:motion['retreatSeconds']=durations[motion.get('weaponId')]
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
