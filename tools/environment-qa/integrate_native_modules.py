"""Keep generated PNG originals intact; configure three UV rows per stage."""
import json
import shutil
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
RES=ROOT/'UnityProject/BigBonusBlitz/Assets/Resources'
path=RES/'Data/adventure_environments.json'
data=json.loads(path.read_text(encoding='utf-8-sig'))
specs={
 'A-1':([0,345,740,1024],(.19,.27,.13),(.47,.44,.28)),
 'B-1':([0,360,760,1024],(.24,.23,.12),(.48,.39,.24)),
 'B-2':([0,320,760,1024],(.13,.23,.15),(.32,.29,.19)),
 'C-2':([0,370,760,1024],(.16,.23,.24),(.34,.37,.34)),
 'C-3':([0,355,760,1024],(.14,.24,.22),(.29,.36,.31)),
 'C-4':([0,355,760,1024],(.20,.20,.12),(.31,.27,.19)),
 'D-1':([0,380,760,1024],(.12,.15,.19),(.25,.26,.28)),
 'D-2':([0,355,760,1024],(.30,.28,.23),(.50,.43,.32)),
 'D-3':([0,355,760,1024],(.13,.22,.14),(.30,.32,.22)),
 'D-4':([0,350,760,1024],(.25,.29,.31),(.40,.42,.40)),
 'D-5':([0,380,760,1024],(.10,.20,.16),(.22,.29,.23)),
 'D-6':([0,355,760,1024],(.30,.18,.16),(.47,.31,.23)),
 'E-1':([0,380,760,1024],(.13,.19,.20),(.28,.31,.29)),
 'E-2':([0,380,740,1024],(.10,.15,.24),(.22,.27,.36)),
 'H-1':([0,345,780,1024],(.16,.12,.18),(.32,.24,.28)),
}
for id,(cuts,ground,road) in specs.items():
    version={'A-1':'v2','H-1':'v3','D-3':'v2'}.get(id,'v1')
    date='2026-11-05' if id in ['A-1','B-1','B-2','C-2','E-2','H-1'] else '2026-11-06'
    source=ROOT/'docs/art'/date/('environment-'+id+'-'+version)/'source.png'
    destination=RES/'Art/Adventure/Modules'/id/('layers-'+version+'.png')
    destination.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,destination)
    alpha=Image.open(source).getchannel('A')
    for cut in cuts[1:-1]:
        assert alpha.crop((0,cut-2,1536,cut+2)).getextrema()[1]<32,(id,cut,'Crossed row')
    p=next(p for p in data['stages'] if p['id']==id)
    p['layers']=[dict(texture='Art/Adventure/Modules/'+id+'/layers-'+version,top=cuts[i]/1024,rowHeight=(cuts[i+1]-cuts[i])/1024,
                      nativeAlpha=True,speed=[.007,.024,.065][i],phase=[.31,0,.61][i],height=[.8,1.05,.48][i],bottom=[.08,.025,-.075][i]) for i in range(3)]
    p['ground']=dict(zip(('r','g','b','a'),(*ground,1)))
    p['path']=dict(zip(('r','g','b','a'),(*road,1)))
    p['groundHeight']=.235
    # Keep native silhouette baselines behind the continuous ground, even when
    # generation leaves different transparent padding below individual rows.
    middleBounds=alpha.crop((0,cuts[1],1536,cuts[2])).point(lambda a:255 if a>=32 else 0).getbbox()
    tail=1-middleBounds[3]/(cuts[2]-cuts[1])
    p['layers'][1]['bottom']=round(p['groundHeight']-.02-p['layers'][1]['height']*tail,6)
    if id=='E-2':p['layers'][0].update(height=1.5,bottom=.05)
path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
