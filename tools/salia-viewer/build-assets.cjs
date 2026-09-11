// Deterministic texture extraction and PSD packaging. No source artwork is repainted.
const fs = require('node:fs');
const path = require('node:path');
const {createRequire} = require('node:module');
const dependencyRoot = process.env.SALIA_NODE_MODULES;
const req = dependencyRoot ? createRequire(path.join(dependencyRoot, '_salia.cjs')) : require;
const sharp = req('sharp');
const {createCanvas} = req('@napi-rs/canvas');
const root = path.resolve(__dirname, '../..');
const out = path.join(root, 'assets/title/Character/salia-rig');
const definitions = [
  {id:'body', name:'胴体・首・脚', color:'#8795bb'},
  {id:'hair-back-left',name:'後ろ髪・左',color:'#ef8daa',polygon:[[641,297],[706,328],[756,483],[810,580],[766,667],[649,715],[503,726],[397,558],[401,449],[496,371],[581,350]]},
  {id:'hair-back-right',name:'後ろ髪・右',color:'#dc638e',polygon:[[854,14],[977,14],[1110,102],[1145,252],[1035,305],[953,310],[875,340],[852,235]]},
  {id:'sword',name:'剣',color:'#b8bce4',polygon:[[418,60],[527,57],[647,113],[883,216],[998,266],[1130,319],[1177,308],[1176,252],[1234,218],[1265,268],[1226,345],[1303,411],[1449,467],[1450,526],[1307,484],[1227,437],[1211,484],[1180,529],[1127,534],[1101,484],[1118,421],[1065,415],[885,332],[649,203],[483,150]]},
  {id:'coat-left',name:'外套・左裾',color:'#64bbb7',polygon:[[824,606],[728,627],[628,690],[471,733],[301,778],[190,858],[158,941],[671,941],[784,804],[879,667]]},
  {id:'coat-right',name:'外套・右裾',color:'#4b969f',polygon:[[1079,646],[1251,630],[1390,666],[1492,714],[1562,787],[1628,895],[1615,941],[1151,941],[1082,847],[1024,697]]},
  {id:'skirt',name:'スカート・フリル',color:'#6b7be5',polygon:[[913,637],[1004,655],[1069,720],[1099,785],[1202,879],[1195,923],[1067,906],[1005,811],[934,789],[891,768],[816,799],[742,855],[738,780],[826,706]]},
  {id:'armor',name:'胸甲・腹部・ベルト',color:'#d4b576',polygon:[[762,322],[861,309],[966,351],[1023,397],[1046,498],[1006,572],[1044,646],[988,696],[899,656],[823,629],[754,637],[759,592],[800,573],[773,526],[761,459]]},
  {id:'arm-right',name:'右腕・握り手',color:'#d29c75',polygon:[[964,345],[1010,365],[1073,458],[1122,498],[1170,484],[1208,414],[1204,372],[1241,356],[1290,374],[1327,409],[1320,456],[1278,485],[1250,552],[1180,620],[1110,651],[1025,610],[1024,548],[1016,471]]},
  {id:'head',name:'顔・前髪・横髪',color:'#f2a0bb',polygon:[[605,71],[703,11],[824,25],[882,77],[927,177],[919,261],[871,324],[795,325],[724,299],[673,330],[610,313],[559,274],[578,165]]},
  {id:'hair-crown',name:'頭頂・アホ毛',color:'#f57798',polygon:[[588,16],[797,0],[841,30],[802,63],[751,77],[707,108],[658,126],[606,121]]},
  {id:'ribbon',name:'リボン',color:'#d4cbe9',polygon:[[810,3],[853,0],[882,20],[918,12],[941,68],[928,104],[982,120],[962,183],[916,152],[900,123],[861,112],[845,65]]},
  {id:'arm-left',name:'左腕・差し出した手',color:'#b08b71',polygon:[[639,314],[692,291],[748,305],[785,366],[786,428],[751,477],[725,518],[675,548],[681,594],[726,624],[727,655],[679,659],[635,634],[636,698],[604,717],[578,659],[562,715],[534,711],[529,645],[480,677],[449,665],[462,627],[501,571],[521,535],[486,527],[477,501],[511,488],[603,484],[623,432],[610,401]]},
  {id:'eye-left',name:'左目・開',color:'#55bbed',polygon:[[689,218],[702,208],[721,207],[741,214],[754,232],[753,248],[738,255],[712,253],[698,242]]},
  {id:'eye-right',name:'右目・開',color:'#258ac5',polygon:[[778,184],[789,172],[811,165],[839,166],[850,176],[847,199],[834,214],[808,220],[787,209]]},
];
function u16(n){const b=Buffer.alloc(2);b.writeUInt16BE(n);return b;}
function i16(n){const b=Buffer.alloc(2);b.writeInt16BE(n);return b;}
function u32(n){const b=Buffer.alloc(4);b.writeUInt32BE(n);return b;}
function writePSD(layers,rgba,w,h,filename='salia-parts.psd'){
  const records=[],channels=[];
  for(const layer of [...layers].reverse()){
    const {x,y,width,height,data,id}=layer,n=width*height;
    const name=Buffer.from(id),padded=Buffer.alloc(Math.ceil((name.length+1)/4)*4);padded[0]=name.length;name.copy(padded,1);
    const extra=Buffer.concat([u32(0),u32(0),padded]);
    records.push(Buffer.concat([u32(y),u32(x),u32(y+height),u32(x+width),u16(4),...([0,1,2,-1].map(c=>Buffer.concat([i16(c),u32(n+2)]))),Buffer.from('8BIMnorm'),Buffer.from([255,0,layer.hidden?2:0,0]),u32(extra.length),extra]));
    for(let c=0;c<4;c++){const plane=Buffer.alloc(n);for(let i=0;i<n;i++)plane[i]=data[i*4+c];channels.push(Buffer.concat([u16(0),plane]));}
  }
  let info=Buffer.concat([i16(-layers.length),...records,...channels]);if(info.length%2)info=Buffer.concat([info,Buffer.alloc(1)]);
  const section=Buffer.concat([u32(info.length),info,u32(0)]),merged=[];
  for(let c=0;c<4;c++){const plane=Buffer.alloc(w*h);for(let i=0;i<w*h;i++)plane[i]=rgba[i*4+c];merged.push(plane);}
  fs.writeFileSync(path.join(out,filename),Buffer.concat([Buffer.from('8BPS'),u16(1),Buffer.alloc(6),u16(4),u32(h),u32(w),u16(8),u16(3),u32(0),u32(0),u32(section.length),section,u16(0),...merged]));
}
async function build(){
  fs.mkdirSync(path.join(out,'parts'),{recursive:true});
  const {data:rgba,info}=await sharp(path.join(root,'assets/title/Character/salia_title_reach.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const w=info.width,h=info.height,n=w*h;
  const cv=createCanvas(w,h),ctx=cv.getContext('2d');
  const owners=new Uint8Array(n);
  // Threshold independent masks: color-coded antialiased edges would invent other part IDs.
  definitions.forEach((p,k)=>{if(!p.polygon)return;ctx.clearRect(0,0,w,h);ctx.fillStyle='white';ctx.beginPath();p.polygon.forEach(([x,y],j)=>j?ctx.lineTo(x,y):ctx.moveTo(x,y));ctx.closePath();ctx.fill();const mask=ctx.getImageData(0,0,w,h).data;for(let i=0;i<n;i++)if(mask[i*4+3]>=128)owners[i]=k;});
  // Pink strands crossing steel, ribbon or glove cut boundaries belong to hair.
  for(let i=0;i<n;i++){
    const x=i%w,y=Math.floor(i/w),j=i*4,id=definitions[owners[i]].id;
    // Include outer hem pixels that fall just outside the hand-drawn polygon.
    if(id==='body'&&y>715&&x<735)owners[i]=4;
    if(id==='body'&&y>650&&x>1220)owners[i]=5;
    const pink=rgba[j]-rgba[j+1]>35&&rgba[j+2]-rgba[j+1]>5;
    if(pink&&['sword','ribbon','arm-left','arm-right','armor'].includes(id)){
      if(x>875&&x<1125&&y<330)owners[i]=2;
      else if(x<810&&y>325&&y<725)owners[i]=1;
    }
  }
  // A hard partition retains every source pixel exactly once, including its original alpha.
  const preview=Buffer.alloc(n*4),layers=[];
  for(let i=0;i<n;i++){const c=definitions[owners[i]].color;preview[i*4]=parseInt(c.slice(1,3),16);preview[i*4+1]=parseInt(c.slice(3,5),16);preview[i*4+2]=parseInt(c.slice(5,7),16);preview[i*4+3]=rgba[i*4+3];}
  for(let k=0;k<definitions.length;k++){
    let x0=w,y0=h,x1=0,y1=0,count=0;
    for(let y=0;y<h;y++)for(let x=0;x<w;x++){const i=y*w+x;if(owners[i]===k&&rgba[i*4+3]){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);count++;}}
    if(!count)continue;
    const width=x1-x0+1,height=y1-y0+1,data=Buffer.alloc(width*height*4);
    for(let y=y0;y<=y1;y++)for(let x=x0;x<=x1;x++){const i=y*w+x;if(owners[i]===k)rgba.copy(data,((y-y0)*width+x-x0)*4,i*4,i*4+4);}
    await sharp(data,{raw:{width,height,channels:4}}).png().toFile(path.join(out,`parts/${definitions[k].id}.png`));
    layers.push({...definitions[k],x:x0,y:y0,width,height,pixels:count,data});
  }
  await sharp(preview,{raw:{width:w,height:h,channels:4}}).png().toFile(path.join(out,'parts-map.png'));
  // Semantic motion masks keep hair/cloth forces away from skin, steel and gloves.
  const motion=Buffer.alloc(n*4);
  for(let i=0;i<n;i++){
    const id=definitions[owners[i]].id,x=i%w,y=Math.floor(i/w);
    const face=x>690&&x<867&&y>174&&y<324;
    const pink=rgba[i*4]-rgba[i*4+1]>60&&rgba[i*4]>rgba[i*4+2]*.93;
    motion[i*4]=(id.startsWith('hair-')||id==='ribbon'||(id==='head'&&pink&&!face))?255:0;
    motion[i*4+1]=(id.startsWith('coat-')||id==='skirt')?255:0;
    motion[i*4+3]=255;
  }
  const weights=await sharp(motion,{raw:{width:w,height:h,channels:4}}).blur(7).raw().toBuffer();
  await sharp(weights,{raw:{width:w,height:h,channels:4}}).png().toFile(path.join(out,'motion-weights.png'));
  const columns=140,rows=80,samples=[];
  for(let y=0;y<=rows;y++)for(let x=0;x<=columns;x++){
    const i=(Math.min(h-1,Math.round(y/rows*h))*w+Math.min(w-1,Math.round(x/columns*w)))*4;
    samples.push(Number((weights[i]/255).toFixed(4)),Number((weights[i+1]/255).toFixed(4)));
  }
  fs.writeFileSync(path.join(out,'motion-grid.json'),JSON.stringify({width:w,height:h,columns,rows,weights:samples}));
  // The generated whole frame is NOT used as the character texture. Only local eyelid patches are sampled.
  const blink=await sharp(path.join(out,'blink-reference.png')).resize(w,h,{fit:'fill'}).ensureAlpha().raw().toBuffer();
  const closed=Buffer.alloc(n*4);
  ctx.clearRect(0,0,w,h);ctx.fillStyle='white';
  for(const p of definitions.filter(p=>p.id.startsWith('eye-'))){ctx.beginPath();p.polygon.forEach(([x,y],j)=>j?ctx.lineTo(x,y):ctx.moveTo(x,y));ctx.closePath();ctx.fill();}
  const mask=await sharp(cv.toBuffer('image/png')).blur(1.2).ensureAlpha().raw().toBuffer();
  for(let i=0;i<n;i++){closed[i*4]=blink[i*4];closed[i*4+1]=blink[i*4+1];closed[i*4+2]=blink[i*4+2];closed[i*4+3]=Math.round(mask[i*4+3]*rgba[i*4+3]/255);}
  await sharp(closed,{raw:{width:w,height:h,channels:4}}).png().toFile(path.join(out,'eyes-closed.png'));
  const expressions=[];
  for(const p of definitions.filter(p=>p.id.startsWith('eye-'))){
    const x=Math.min(...p.polygon.map(v=>v[0]))-5,y=Math.min(...p.polygon.map(v=>v[1]))-5;
    const width=Math.max(...p.polygon.map(v=>v[0]))-x+6,height=Math.max(...p.polygon.map(v=>v[1]))-y+6;
    const data=await sharp(closed,{raw:{width:w,height:h,channels:4}}).extract({left:x,top:y,width,height}).raw().toBuffer();
    const id=p.id+'-closed';await sharp(data,{raw:{width,height,channels:4}}).png().toFile(path.join(out,`parts/${id}.png`));
    expressions.push({id,name:p.name.replace('開','閉'),x,y,width,height,data,hidden:true,file:`parts/${id}.png`});
  }
  const reconstruction=Buffer.alloc(n*4);
  for(const l of layers)for(let y=0;y<l.height;y++)for(let x=0;x<l.width;x++){const si=(y*l.width+x)*4,di=((l.y+y)*w+l.x+x)*4;if(l.data[si+3])l.data.copy(reconstruction,di,si,si+4);}
  let mismatches=0;for(let i=0;i<n;i++)if(rgba[i*4+3]&&[0,1,2,3].some(c=>rgba[i*4+c]!==reconstruction[i*4+c]))mismatches++;
  if(mismatches)throw Error(`Reconstruction mismatches: ${mismatches}`);
  const order=['body','sword','hair-back-left','hair-back-right','coat-left','coat-right','skirt','hair-crown','ribbon','arm-right','arm-left','armor','head','eye-left','eye-right'];
  layers.sort((a,b)=>order.indexOf(a.id)-order.indexOf(b.id));
  writePSD([...layers,...expressions],rgba,w,h);
  fs.writeFileSync(path.join(out,'rig.json'),JSON.stringify({format:'salia-web-mesh-v1',cubism:false,width:w,height:h,source:'../salia_title_reach.png',closedEyes:'eyes-closed.png',partsMap:'parts-map.png',parts:layers.map(({data,...p})=>({...p,file:`parts/${p.id}.png`})),expressions:expressions.map(({data,...p})=>p),parameters:{breath:{default:0.65,period:4.8},hair:{default:0.7},cloth:{default:0.65},blink:{default:true,interval:[3.2,5.6],duration:0.22}},verification:{reconstructionMismatches:mismatches,partCount:layers.length},limitations:['Not a Cubism moc3/cmo3 model.','Visible-pixel partition; hidden surfaces are not repainted.','Joined mesh preserves boundaries during subtle idle motion.']},null,2));
  console.log(JSON.stringify({width:w,height:h,parts:layers.length,reconstructionMismatches:mismatches,output:out}));
}
module.exports={writePSD};
if(require.main===module)build();
