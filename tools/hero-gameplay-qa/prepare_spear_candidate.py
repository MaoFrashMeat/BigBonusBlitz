"""Prepare QA-only spear candidate and provenance. Never edits PNG pixels or main Resources."""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'docs/art/2026-10-04'
QA = Path('C:/Users/ts081/Documents/Codex/2026-09-12/new-chat/work/shop-qa')
GENERATED = Path('C:/Users/ts081/.codex/generated_images/01a0918e-f596-7aa0-b6a0-25dbb4e2f317')
SOURCES = {
    'ready': 'b8cebf72-c9ba-4b1d-bcac-b265be3d8c7f',
    'prep': '7b90adb6-a76d-4b33-be87-7d261e39b82b',
    'start': '691e8fe5-a688-4136-b31d-8e1eecec0783',
    'contact': '96590d41-1e10-4517-b022-8e95ac850165',
    'return': 'ab95147e-6ca2-4e41-a613-19b5c57abe37',
}
GRIPS = {'ready': (719,483,1087,395), 'prep': (610,492,1090,463),
         'start': (708,475,1208,384), 'contact': (1018,439,1248,411),
         'return': (835,463,1111,400)}

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    assert (QA / 'ProjectSettings/ProjectSettings.asset').is_file()
    cfg = json.loads((ART / 'sword-cycle-v3-qa.json').read_text(encoding='utf-8-sig'))
    ledger = []
    for index, name in enumerate(('ready','prep','start','contact','return','ready'), 31):
        path = ART / f'spear-{name}-walkmatched-v1.png'
        raw = GENERATED / f'exec-{SOURCES[name]}.png'
        assert sha(raw) == sha(path)
        with Image.open(path) as image:
            assert image.size == (1312,1199) and image.mode == 'RGBA'
            bbox = image.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox()
        gx,gy,sx,sy=GRIPS[name]
        cfg['poses'][index] = dict(atlas=f'spear-{name}-walkmatched-v1-qa',x=0,y=0,w=1306,h=1199,
            foot=1199-bbox[3],height=1145,gripX=gx,gripY=gy,supportX=sx,supportY=sy,
            swordAngle=-55,spearAngle=-80,twoHanded=True)
        if index==36:
            continue
        ref = ('sword-ready-walkmatched-v1.png' if name=='ready' else
               'spear-start-walkmatched-v1.png' if name=='contact' else 'spear-ready-walkmatched-v1.png')
        ledger.append(dict(name=name,source=str(raw),file=str(path),reference=str(ART/ref),
            prompt=(ART/f'spear-{name}-walkmatched-v1-prompt.txt').read_text(encoding='utf-8'),
            sha256=sha(path),size=[1312,1199],alphaBounds64=bbox,tool='built-in image_gen',pixelEdits=False))
        shutil.copyfile(path,QA/f'Assets/Resources/Art/HeroPrototype/spear-{name}-walkmatched-v1-qa.png')
    spear=next(w for w in cfg['weapons'] if w['id']=='w_spear')
    spear['gripY']=675  # Farther back on the same rigid shaft; lets the wide grips avoid the gold collar.
    cfg['status']='QA candidate only: walk/sword v3 plus five spear originals over six poses; final recovery reuses ready.'
    (ART/'spear-cycle-v1-qa.json').write_text(json.dumps(cfg,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    path=ART/'spear-start-walkmatched-v2.png'
    raw=GENERATED/'exec-603ae1b8-6705-436c-b95e-6606ee054ce0.png'
    if not path.exists():
        shutil.copyfile(raw,path)
    assert sha(raw)==sha(path)
    with Image.open(path) as image:
        bbox=image.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox()
    cfg['poses'].append(dict(cfg['poses'][33]))  # Keep the first start drawing as pose 43.
    cfg['poses'][33]=dict(cfg['poses'][33],atlas='spear-start-walkmatched-v2-qa',
        foot=1199-bbox[3],gripX=909,gripY=438,supportX=1208,supportY=384)
    for motion in cfg['motions']:
        if motion.get('weaponId')!='w_spear' or 33 not in motion['frames']:
            continue
        at=motion['frames'].index(33)
        seconds=motion['seconds'][at]
        event=motion['events'][at]
        motion['frames'][at:at+1]=[43,33]
        motion['seconds'][at:at+1]=[seconds/2,seconds/2]
        motion['events'][at:at+1]=[event,'']
    cfg['status']='QA candidate only: 44 poses; spear thrust adds one in-between within unchanged swing/contact timing.'
    (ART/'spear-cycle-v2-qa.json').write_text(json.dumps(cfg,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    shutil.copyfile(path,QA/'Assets/Resources/Art/HeroPrototype/spear-start-walkmatched-v2-qa.png')
    ledger.append(dict(name='start-intermediate',source=str(raw),file=str(path),
        reference=str(ART/'spear-start-walkmatched-v1.png'),
        prompt=(ART/'spear-start-walkmatched-v2-prompt.txt').read_text(encoding='utf-8'),
        sha256=sha(path),size=[1312,1199],alphaBounds64=bbox,tool='built-in image_gen',pixelEdits=False))
    (ART/'spear-cycle-assets.json').write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    main_editor=ROOT/'UnityProject/BigBonusBlitz/Assets/Editor/AdventureHeroValidation.cs'
    shutil.copyfile(main_editor,QA/'Assets/Editor/AdventureHeroValidation.cs')
    print('Prepared six untouched originals, seven spear poses, QA-only Resources and v1/v2 candidate configs.')

if __name__ == '__main__':
    main()
