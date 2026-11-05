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
 'E-2':([0,380,740,1024],(.10,.15,.24),(.22,.27,.36)),
 'H-1':([0,345,780,1024],(.16,.12,.18),(.32,.24,.28)),
}
for id,(cuts,ground,road) in specs.items():
    version={'A-1':'v2','H-1':'v3'}.get(id,'v1')
    source=ROOT/'docs/art/2026-11-05'/('environment-'+id+'-'+version)/'source.png'
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
    if id=='E-2':p['layers'][0].update(height=1.5,bottom=.05)
path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
