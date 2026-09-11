const fs=require('node:fs'),path=require('node:path'),{createRequire}=require('node:module');
const req=process.env.SALIA_NODE_MODULES?createRequire(path.join(process.env.SALIA_NODE_MODULES,'_salia.cjs')):require;
const sharp=req('sharp');
const root=path.resolve(__dirname,'../..'),dir=path.join(root,'assets/title/Character/salia-rig');
(async()=>{const rig=JSON.parse(fs.readFileSync(path.join(dir,'rig.json')));fs.mkdirSync(path.join(dir,'inpaint-input'),{recursive:true});for(const p of rig.parts.filter(p=>!p.id.startsWith('eye-'))){await sharp({create:{width:rig.width,height:rig.height,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite([{input:path.join(dir,p.file),left:p.x,top:p.y}]).png().toFile(path.join(dir,'inpaint-input',p.id+'.png'));}console.log('Prepared 13 registered layer references.');})();
