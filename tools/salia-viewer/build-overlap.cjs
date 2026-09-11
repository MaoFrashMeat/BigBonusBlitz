// Original visible pixels stay authoritative. Generated artwork is admitted only under opaque foreground layers.
const fs=require('node:fs'),path=require('node:path'),{createRequire}=require('node:module');
const req=process.env.SALIA_NODE_MODULES?createRequire(path.join(process.env.SALIA_NODE_MODULES,'_salia.cjs')):require;
const sharp=req('sharp'),{writePSD}=require('./build-assets.cjs');
const root=path.resolve(__dirname,'../..'),dir=path.join(root,'assets/title/Character/salia-rig');
const specs={
  body:{file:'body',target:[653,325,1160,941]},
  'hair-back-left':{file:'hair-back-left',target:[410,280,812,738]},
  'hair-back-right':{file:'hair-back-right',target:[855,12,1140,335]},
  'coat-left':{file:'coat-left',target:[140,572,900,941]},
  'coat-right':{file:'coat-right',target:[1005,590,1629,941]},
  skirt:{file:'skirt',target:[715,610,1210,941]},
  sword:{file:'sword',registered:true},
  armor:{file:'body',crop:[674,25,438,544],target:[736,302,1052,702]},
  head:{file:'head',transform:[.52,365,5]},
  'hair-crown':{file:'hair-crown',transform:[.55,332,-5]},
  ribbon:{file:'ribbon',target:[805,0,990,190]},
  'arm-left':{file:'arm-left',target:[444,270,830,725]},
  'arm-right':{file:'arm-right',target:[935,320,1335,660]}
};
function bounds(data,w,h){let l=w,t=h,r=-1,b=-1;for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(data[(y*w+x)*4+3]){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}return r<l?null:{x:l,y:t,width:r-l+1,height:b-t+1};}
function crop(data,w,box){const out=Buffer.alloc(box.width*box.height*4);for(let y=0;y<box.height;y++)data.copy(out,y*box.width*4,((box.y+y)*w+box.x)*4,((box.y+y)*w+box.x+box.width)*4);return out;}
async function fullPart(p,w,h){const raw=await sharp(path.join(dir,p.file)).ensureAlpha().raw().toBuffer(),full=Buffer.alloc(w*h*4);for(let y=0;y<p.height;y++)raw.copy(full,((p.y+y)*w+p.x)*4,y*p.width*4,(y+1)*p.width*4);return full;}
function distances(data,w,h,max){
  const n=w*h,d=new Uint16Array(n),near=new Int32Array(n),q=new Int32Array(n);d.fill(65535);near.fill(-1);let end=0;
  for(let i=0;i<n;i++)if(data[i*4+3]>200){d[i]=0;near[i]=i;q[end++]=i;}
  for(let head=0;head<end;head++){const i=q[head];if(d[i]>=max)continue;const x=i%w;
    for(const j of [x?i-1:-1,x<w-1?i+1:-1,i>=w?i-w:-1,i<n-w?i+w:-1])if(j>=0&&d[j]>d[i]+1){d[j]=d[i]+1;near[j]=near[i];q[end++]=j;}
  }return {d,near};
}
async function generated(spec,w,h){
  let s=sharp(path.join(dir,'inpaint-generated',spec.file+'.png'));
  if(spec.crop){const [left,top,width,height]=spec.crop;s=s.extract({left,top,width,height});}
  let {data,info}=await s.ensureAlpha().raw().toBuffer({resolveWithObject:true});
  // Chroma key only the generated support artwork, never the original.
  for(let i=0;i<info.width*info.height;i++){const j=i*4,r=data[j],g=data[j+1],b=data[j+2];if(g>r+35&&g>b+35)data[j+3]=0;}
  if(spec.registered)return sharp(data,{raw:{width:info.width,height:info.height,channels:4}}).resize(w,h).raw().toBuffer();
  if(spec.transform){
    const [scale,ox,oy]=spec.transform,sw=Math.round(info.width*scale),sh=Math.round(info.height*scale);
    const scaled=await sharp(data,{raw:{width:info.width,height:info.height,channels:4}}).resize(sw,sh).raw().toBuffer(),full=Buffer.alloc(w*h*4);
    for(let y=0;y<sh;y++)for(let x=0;x<sw;x++){const dx=x+ox,dy=y+oy;if(dx<0||dy<0||dx>=w||dy>=h)continue;scaled.copy(full,(dy*w+dx)*4,(y*sw+x)*4,(y*sw+x+1)*4);}return full;
  }
  const box=bounds(data,info.width,info.height);if(!box)throw Error('Empty generated part');
  const [x0,y0,x1,y1]=spec.target,scaled=await sharp(crop(data,info.width,box),{raw:{width:box.width,height:box.height,channels:4}}).resize(x1-x0,y1-y0,{fit:'fill'}).raw().toBuffer();
  const full=Buffer.alloc(w*h*4);for(let y=y0;y<Math.min(h,y1);y++)for(let x=Math.max(0,x0);x<Math.min(w,x1);x++){const si=((y-y0)*(x1-x0)+x-x0)*4;scaled.copy(full,(y*w+x)*4,si,si+4);}return full;
}
(async()=>{
  const rig=JSON.parse(fs.readFileSync(path.join(dir,'rig.json'))),w=rig.width,h=rig.height,n=w*h;
  const original=await sharp(path.join(dir,'../salia_title_reach.png')).ensureAlpha().raw().toBuffer();
  const parts=await Promise.all(rig.parts.map(p=>fullPart(p,w,h))),owner=new Int16Array(n);owner.fill(-1);
  // This cutout has alpha 252/253 throughout solid skin/armor. Normalize only near-opaque texels
  // so hidden support paint cannot ghost through an otherwise solid foreground layer.
  for(let i=0;i<n;i++)if(original[i*4+3]>=250)original[i*4+3]=255;
  for(const part of parts)for(let i=0;i<n;i++)if(part[i*4+3]>=250)part[i*4+3]=255;
  for(let k=0;k<parts.length;k++)for(let i=0;i<n;i++)if(parts[k][i*4+3])owner[i]=k;
  for(const name of ['layers','underpaint'])fs.mkdirSync(path.join(dir,name),{recursive:true});
  const output=[],psd=[],allFill=Buffer.alloc(n*4);let total=0;
  for(let k=0;k<rig.parts.length;k++){
    const p=rig.parts[k],src=parts[k],combined=Buffer.from(src),fill=Buffer.alloc(n*4),spec=specs[p.id];let count=0;
    if(spec){
      const paint=await generated(spec,w,h),{d,near}=distances(src,w,h,w+h);
      for(let i=0;i<n;i++){
        const j=i*4;
        if(src[j+3]||owner[i]<=k||original[j+3]!==255||paint[j+3]<240||near[i]<0)continue;
        const blend=Math.min(1,d[i]/10),edge=near[i]*4;
        for(let c=0;c<3;c++)fill[j+c]=Math.round(paint[j+c]*blend+src[edge+c]*(1-blend));
        fill[j+3]=255;fill.copy(combined,j,j,j+4);count++;
        allFill[j]=230;allFill[j+1]=160;allFill[j+2]=65;allFill[j+3]=Math.max(allFill[j+3],fill[j+3]);
      }
    }
    const box=bounds(combined,w,h),raw=crop(combined,w,box),fillBox=bounds(fill,w,h);
    const motionClass=p.id.startsWith('hair-')&&count?1:(p.id.startsWith('coat-')||p.id==='skirt'?2:0);
    await sharp(raw,{raw:{width:box.width,height:box.height,channels:4}}).png().toFile(path.join(dir,'layers',p.id+'.png'));
    await sharp(crop(fill,w,box),{raw:{width:box.width,height:box.height,channels:4}}).png().toFile(path.join(dir,'underpaint',p.id+'.png'));
    if(fillBox)psd.push({id:p.id+'-underpaint',...fillBox,data:crop(fill,w,fillBox)});
    psd.push({...p,data:crop(src,w,p)});
    output.push({...p,...box,file:'layers/'+p.id+'.png',underpaintFile:'underpaint/'+p.id+'.png',motionClass,paintedPixels:count,paintStatus:count?'painted-overlap':p.id.startsWith('eye-')?'expression-ready':'pending-new-paint',generatedSource:spec?.file||null});total+=count;
  }
  for(const p of rig.expressions){const data=await sharp(path.join(dir,p.file)).ensureAlpha().raw().toBuffer();psd.push({...p,data,hidden:true});}
  // Every underpaint texel is covered by a fully opaque original foreground texel at the rest pose.
  writePSD(psd,original,w,h,'salia-layered.psd');
  await sharp(allFill,{raw:{width:w,height:h,channels:4}}).png().toFile(path.join(dir,'underpaint-map.png'));
  const layered={...rig,format:'salia-layered-v2',parts:output,renderMode:'independent-overlapping-layers',motionRange:1.35,verification:{paintedPixels:total,paintedParts:output.filter(p=>p.paintedPixels).length,psdLayers:psd.length,visibleOriginalRgbPreserved:true,nearOpaqueAlphaNormalized:'250..254 to 255'},pendingParts:output.filter(p=>p.paintStatus==='pending-new-paint').map(p=>p.id),limitations:['Not a Cubism moc3/cmo3 model.','Hidden surfaces support subtle title-screen motion; extreme poses are not rigged.','Generated support artwork is registered approximately; visible original RGB is retained.']};
  fs.writeFileSync(path.join(dir,'rig-layered.json'),JSON.stringify(layered,null,2));
  const unity=path.join(root,'UnityProject/BigBonusBlitz/Assets/Resources/SaliaRig');fs.mkdirSync(path.join(unity,'layers'),{recursive:true});
  for(const p of output)fs.copyFileSync(path.join(dir,p.file),path.join(unity,p.file));
  fs.copyFileSync(path.join(dir,'eyes-closed.png'),path.join(unity,'eyes-closed.png'));
  fs.copyFileSync(path.join(dir,'../salia_title_reach.png'),path.join(dir,'source.png'));
  fs.copyFileSync(path.join(dir,'source.png'),path.join(unity,'source.png'));
  fs.writeFileSync(path.join(unity,'model.json'),JSON.stringify({width:w,height:h,parts:output.map(p=>({id:p.id,x:p.x,y:p.y,width:p.width,height:p.height,motionClass:p.motionClass,file:p.file.replace(/\.png$/,'')}))},null,2));
  console.log(JSON.stringify({paintedParts:layered.verification.paintedParts,paintedPixels:total,psdLayers:psd.length,pending:layered.pendingParts}));
})();
