'use strict';
const $ = id => document.getElementById(id);
const ASSETS = '../../assets/title/Character/salia-rig/';
const state = {playing:!matchMedia('(prefers-reduced-motion: reduce)').matches, time:0, breath:.65, hair:.7, cloth:.65, range:1.35, speed:1, zoom:1, autoBlink:true, mesh:false, colors:false, part:'all'};
let layerTextures=[];
let gl, rig, motionGrid, program, vertexBuffer, indices, lineIndices, textures={}, partImages=[], assembled, activeTexture, blinkOverride=null;
let last=performance.now(), nextBlink=3.3, blinkStart=-10, manualStart=-10, randomSeed=512, pendingCapture=false;
let indexCount=0,lineCount=0,ready=false;
const canvas=$('scene');
const vertexShader=`
attribute vec2 aPosition;
uniform vec2 uViewport;
uniform vec2 uCanvasSize;
uniform float uTime,uBreath,uHair,uCloth,uZoom,uPartClass,uRange;
varying vec2 vUv;
float field(vec2 p,vec2 center,vec2 radius){vec2 d=(p-center)/radius;return exp(-dot(d,d)*2.0);}
float pinBox(vec2 p,vec2 lo,vec2 hi){vec2 d=max(max(lo-p,p-hi),vec2(0.0));return 1.0-smoothstep(0.0,30.0,length(d));}
float protectFace(vec2 p,vec2 c,vec2 r){return 1.0-smoothstep(1.0,1.4,length((p-c)/r));}
void main(){
  vec2 p=aPosition;vUv=p/uCanvasSize;
  float breath=sin(uTime*1.30899694)*uBreath;
  float upper=1.0-smoothstep(390.0,880.0,p.y);
  p.y-=breath*3.8*upper;
  float chest=field(aPosition,vec2(909.0,445.0),vec2(140.0,160.0));
  p.x+=(aPosition.x-909.0)*.004*breath*chest;
  p.y-=(aPosition.y-530.0)*.003*breath*chest;
  float leftHair=field(aPosition,vec2(477.0,462.0),vec2(94.0,146.0));
  float lowHair=field(aPosition,vec2(716.0,574.0),vec2(94.0,130.0));
  float rightHair=field(aPosition,vec2(1025.0,157.0),vec2(146.0,112.0));
  float sideHair=field(aPosition,vec2(615.0,225.0),vec2(53.0,80.0));
  float crown=field(aPosition,vec2(735.0,18.0),vec2(50.0,45.0));
  float wind=sin(uTime*1.65-aPosition.y*.004)+.3*sin(uTime*2.77+aPosition.x*.007);
  // A shared displacement field keeps both sides of every cut on the same surface.
  float hw=uRange;
  p.x+=uHair*hw*wind*(7.0*leftHair+5.0*lowHair+7.0*rightHair+2.5*sideHair+3.0*crown);
  p.y+=uHair*hw*(sin(uTime*1.65-.8)*3.0*rightHair+sin(uTime*1.65+.5)*2.2*leftHair);
  float face=max(protectFace(aPosition,vec2(727.0,234.0),vec2(36.0,27.0)),max(protectFace(aPosition,vec2(813.0,193.0),vec2(36.0,28.0)),protectFace(aPosition,vec2(792.0,277.0),vec2(44.0,27.0))));
  float front=(field(aPosition,vec2(748.0,157.0),vec2(83.0,63.0))*5.5+field(aPosition,vec2(664.0,241.0),vec2(32.0,83.0))*4.0+field(aPosition,vec2(878.0,234.0),vec2(33.0,88.0))*4.5)*(1.0-face);
  float frontWave=sin(uTime*1.65-aPosition.y*.009-.65);
  p+=uHair*hw*front*vec2(frontWave,.22*sin(uTime*1.65-aPosition.y*.009-1.2));
  float lower=smoothstep(635.0,900.0,aPosition.y);
  float skirt=field(aPosition,vec2(1004.0,792.0),vec2(194.0,116.0));
  float edge=max(1.0-smoothstep(400.0,825.0,aPosition.x),smoothstep(1040.0,1520.0,aPosition.x));
  float clothWave=sin(uTime*1.27+aPosition.x*.005-aPosition.y*.004);
  float cw=uRange;
  p.x+=uCloth*cw*lower*edge*clothWave*9.0;
  p.y+=uCloth*cw*(lower*edge*clothWave*12.0+skirt*sin(uTime*1.27-.7)*3.0);
  // Pin every vertex around both gloves, with a soft transition outside their silhouettes.
  float hands=max(pinBox(aPosition,vec2(430.0,478.0),vec2(740.0,725.0)),pinBox(aPosition,vec2(1195.0,350.0),vec2(1335.0,485.0)));
  p=mix(p,aPosition,hands);
  float scale=min(uViewport.x/(uCanvasSize.x*.98),uViewport.y/(uCanvasSize.y*1.04))*.98*uZoom;
  vec2 screen=(p-uCanvasSize*.5)*scale+uViewport*.5;
  gl_Position=vec4(screen.x/uViewport.x*2.0-1.0,1.0-screen.y/uViewport.y*2.0,0.0,1.0);
}`;
const fragmentShader=`
precision mediump float;
varying vec2 vUv;
uniform sampler2D uImage,uClosed,uMap,uSource;
uniform float uBlink,uBlinkRight,uColors,uWire,uPartClass,uJoined;
void main(){
  vec4 base=texture2D(uImage,vUv);
  if(uPartClass<-.5)base.a=smoothstep(.97,.99,base.a);
  if(base.a<.005)discard;
  // Bilinear samples must use the same visible color on both sides of a joined cut.
  if(uJoined>.5)base.rgb=texture2D(uSource,vUv).rgb;
  if(uWire>.5){gl_FragColor=vec4(.9,.76,.45,.3*base.a);return;}
  vec4 closed=texture2D(uClosed,vUv);
  float blink=vUv.x<.46?uBlink:uBlinkRight;
  base.rgb=mix(base.rgb,closed.rgb,closed.a*blink);
  vec4 map=texture2D(uMap,vUv);
  base.rgb=mix(base.rgb,map.rgb,uColors*.68);
  gl_FragColor=base;
}`;
function compile(type,code){const s=gl.createShader(type);gl.shaderSource(s,code);gl.compileShader(s);if(!gl.getShaderParameter(s,gl.COMPILE_STATUS))throw new Error(gl.getShaderInfoLog(s));return s;}
function makeTexture(image){const t=gl.createTexture();gl.bindTexture(gl.TEXTURE_2D,t);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL,false);gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,gl.RGBA,gl.UNSIGNED_BYTE,image);return t;}
function loadImage(url){return new Promise((resolve,reject)=>{const i=new Image();i.onload=()=>resolve(i);i.onerror=()=>reject(new Error('画像を読み込めません: '+url));i.src=url;});}
function makeMesh(){
  const nx=140,ny=80,v=[],tri=[],lines=[];
  for(let y=0;y<=ny;y++)for(let x=0;x<=nx;x++)v.push(x/nx*rig.width,y/ny*rig.height);
  for(let y=0;y<ny;y++)for(let x=0;x<nx;x++){const a=y*(nx+1)+x,b=a+1,c=a+nx+1,d=c+1;tri.push(a,c,b,b,c,d);if(x%3===0)lines.push(a,c);if(y%3===0)lines.push(a,b);}
  vertexBuffer=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,vertexBuffer);gl.bufferData(gl.ARRAY_BUFFER,new Float32Array(v),gl.STATIC_DRAW);
  const loc=gl.getAttribLocation(program,'aPosition');gl.enableVertexAttribArray(loc);gl.vertexAttribPointer(loc,2,gl.FLOAT,false,0,0);
  indices=gl.createBuffer();gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,indices);gl.bufferData(gl.ELEMENT_ARRAY_BUFFER,new Uint16Array(tri),gl.STATIC_DRAW);indexCount=tri.length;
  lineIndices=gl.createBuffer();gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,lineIndices);gl.bufferData(gl.ELEMENT_ARRAY_BUFFER,new Uint16Array(lines),gl.STATIC_DRAW);lineCount=lines.length;
}
const uniforms={};
function uni(name,value){gl.uniform1f(uniforms[name],value);}
function blinkValue(now,delay=0){if(blinkOverride!==null)return blinkOverride;const age=Math.min(state.time-blinkStart,now/1000-manualStart)-delay;if(age<0||age>=.21)return 0;const smooth=t=>t*t*(3-2*t);if(age<.055)return smooth(age/.055);if(age<.085)return 1;return 1-smooth((age-.085)/.125);}
function draw(now=performance.now()){
  if(!ready)return;
  const rect=canvas.getBoundingClientRect(),dpr=Math.min(devicePixelRatio||1,2),w=Math.round(rect.width*dpr),h=Math.round(rect.height*dpr);
  if(!w||!h)return;
  if(canvas.width!==w||canvas.height!==h){canvas.width=w;canvas.height=h;}
  gl.viewport(0,0,w,h);gl.clearColor(0,0,0,0);gl.clear(gl.COLOR_BUFFER_BIT);
  gl.useProgram(program);gl.uniform2f(uniforms.uViewport,rect.width,rect.height);gl.uniform2f(uniforms.uCanvasSize,rig.width,rig.height);
  uni('uTime',state.time);uni('uBreath',state.breath);uni('uHair',state.hair);uni('uCloth',state.cloth);uni('uZoom',state.zoom);uni('uBlink',blinkValue(now));uni('uBlinkRight',blinkValue(now,.006));uni('uColors',state.colors?1:0);uni('uWire',0);
  uni('uRange',state.range);
  uni('uJoined',state.part==='all'?1:0);
  gl.activeTexture(gl.TEXTURE3);gl.bindTexture(gl.TEXTURE_2D,textures.source);
  gl.activeTexture(gl.TEXTURE1);gl.bindTexture(gl.TEXTURE_2D,textures.closed);
  gl.activeTexture(gl.TEXTURE2);gl.bindTexture(gl.TEXTURE_2D,textures.map);
  if(state.part==='all'){
    uni('uPartClass',-1);uni('uWire',0);
    gl.activeTexture(gl.TEXTURE0);gl.bindTexture(gl.TEXTURE_2D,textures.source);
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,indices);gl.drawElements(gl.TRIANGLES,indexCount,gl.UNSIGNED_SHORT,0);
  }
  for(let i=0;i<rig.parts.length;i++){
    const p=rig.parts[i];if(state.part!=='all'&&state.part!==p.id)continue;
    uni('uPartClass',p.motionClass);uni('uWire',0);
    gl.activeTexture(gl.TEXTURE0);gl.bindTexture(gl.TEXTURE_2D,layerTextures[i]);
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,indices);gl.drawElements(gl.TRIANGLES,indexCount,gl.UNSIGNED_SHORT,0);
    if(state.mesh){uni('uWire',1);gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,lineIndices);gl.drawElements(gl.LINES,lineCount,gl.UNSIGNED_SHORT,0);}
  }
  if(pendingCapture){pendingCapture=false;canvas.toBlob(blob=>{if(!blob)return;const a=document.createElement('a'),url=URL.createObjectURL(blob);a.href=url;a.download='salia-motion.png';a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);},'image/png');}
}
function tick(now){
  const dt=Math.min((now-last)/1000,.05);last=now;
  if(state.playing){state.time+=dt*state.speed;if(state.autoBlink&&state.time>=nextBlink){blinkStart=state.time;randomSeed=(Math.imul(randomSeed,1664525)+1013904223)>>>0;nextBlink=state.time+3.2+(randomSeed/4294967296)*2.4;}}
  draw(now);$('time').textContent=String(Math.floor(state.time/60)).padStart(2,'0')+':'+String(Math.floor(state.time%60)).padStart(2,'0');requestAnimationFrame(tick);
}
function syncPlay(){ $('play').textContent=state.playing?'Ⅱ':'▶';$('play').setAttribute('aria-label',state.playing?'一時停止':'再生');$('play-label').textContent=state.playing?'モーション再生中':'一時停止'; }
function selectPart(id){state.part=id;$('part-select').value=id;document.querySelectorAll('.part-item').forEach(b=>b.classList.toggle('active',b.dataset.part===id));const p=rig.parts.find(p=>p.id===id);$('paint-status').textContent=p?(p.paintedPixels?`塗り足し済み · ${p.paintedPixels.toLocaleString()} px`:'追加の塗り足しは未実施'):`${rig.verification.paintedParts}パーツ補完済み / 未補完 ${rig.pendingParts.length}パーツ`;draw();}
function wireControls(){
  for(const key of ['breath','hair','cloth','range','speed','zoom'])$(key).addEventListener('input',()=>{state[key]=Number($(key).value);$(key+'-value').textContent=key==='speed'||key==='range'?state[key].toFixed(2)+'×':Math.round(state[key]*100)+'%';});
  $('play').onclick=()=>{state.playing=!state.playing;syncPlay();};
  $('blink-now').onclick=()=>{manualStart=performance.now()/1000;};
  $('auto-blink').onchange=()=>{state.autoBlink=$('auto-blink').checked;blinkStart=-10;nextBlink=state.time+3.3;};
  $('hold-blink').onchange=()=>{blinkOverride=$('hold-blink').checked?1:null;};
  $('show-mesh').onchange=()=>state.mesh=$('show-mesh').checked;
  $('show-parts').onchange=()=>state.colors=$('show-parts').checked;
  $('snapshot').onclick=()=>pendingCapture=true;
  $('reset-view').onclick=()=>{$('zoom').value=1;$('zoom').dispatchEvent(new Event('input'));};
  $('reset-motion').onclick=()=>{for(const [k,v] of Object.entries({breath:.65,hair:.7,cloth:.65,range:1.35,speed:1})){$(k).value=v;$(k).dispatchEvent(new Event('input'));}state.autoBlink=true;$('auto-blink').checked=true;$('hold-blink').checked=false;state.time=0;nextBlink=3.3;blinkStart=-10;manualStart=-10;blinkOverride=null;};
  $('fullscreen').onclick=async()=>{try{if(document.fullscreenElement)await document.exitFullscreen();else await document.documentElement.requestFullscreen();}catch{$('status').textContent='この表示環境は全画面に対応していません';}};
  document.addEventListener('fullscreenchange',()=>$('fullscreen').textContent=document.fullscreenElement?'⛶ 戻る':'⛶ 全画面');
  for(const tab of ['motion','parts'])$(tab+'-tab').onclick=()=>{for(const name of ['motion','parts']){$(name+'-tab').classList.toggle('active',name===tab);$(name+'-tab').setAttribute('aria-selected',String(name===tab));$(name+'-panel').hidden=name!==tab;}};
  $('part-select').onchange=()=>selectPart($('part-select').value);
  document.querySelectorAll('[data-bg]').forEach(b=>b.onclick=()=>{document.body.dataset.bg=b.dataset.bg;document.querySelectorAll('[data-bg]').forEach(x=>x.classList.toggle('active',x===b));});
  document.addEventListener('keydown',e=>{if(e.code==='Space'&&!/INPUT|SELECT|BUTTON|TEXTAREA/.test(e.target.tagName)){e.preventDefault();$('play').click();}});
  canvas.addEventListener('webglcontextlost',e=>{e.preventDefault();ready=false;$('loading').hidden=false;$('loading').textContent='描画が中断しました。ページを再読み込みしてください。';});
}
async function start(){
  try{
    const response=await fetch(ASSETS+'rig-layered.json');if(!response.ok)throw Error('パーツ定義がありません。build-overlap.cjs を実行してください。');rig=await response.json();
    gl=canvas.getContext('webgl',{alpha:true,antialias:true,premultipliedAlpha:false,preserveDrawingBuffer:true});if(!gl)throw Error('WebGLを利用できません。ハードウェアアクセラレーションを有効にしてください。');
    program=gl.createProgram();gl.attachShader(program,compile(gl.VERTEX_SHADER,vertexShader));gl.attachShader(program,compile(gl.FRAGMENT_SHADER,fragmentShader));gl.linkProgram(program);if(!gl.getProgramParameter(program,gl.LINK_STATUS))throw Error(gl.getProgramInfoLog(program));gl.useProgram(program);
    for(const n of ['uViewport','uCanvasSize','uTime','uBreath','uHair','uCloth','uZoom','uPartClass','uRange','uBlink','uBlinkRight','uColors','uWire','uImage','uClosed','uMap','uSource','uJoined'])uniforms[n]=gl.getUniformLocation(program,n);
    const loaded=await Promise.all([...rig.parts.map(p=>loadImage(ASSETS+p.file)),loadImage(ASSETS+rig.closedEyes),loadImage(ASSETS+rig.partsMap)]);partImages=loaded.slice(0,rig.parts.length);
    assembled=document.createElement('canvas');assembled.width=rig.width;assembled.height=rig.height;const ctx=assembled.getContext('2d');rig.parts.forEach((p,i)=>ctx.drawImage(partImages[i],p.x,p.y));
    layerTextures=rig.parts.map((p,i)=>{const c=document.createElement('canvas');c.width=rig.width;c.height=rig.height;c.getContext('2d').drawImage(partImages[i],p.x,p.y);return makeTexture(c);});
    textures.all=makeTexture(assembled);textures.closed=makeTexture(loaded[rig.parts.length]);textures.map=makeTexture(loaded[rig.parts.length+1]);activeTexture=textures.all;
    textures.source=makeTexture(await loadImage(ASSETS+'source.png'));
    gl.uniform1i(uniforms.uSource,3);
    gl.uniform1i(uniforms.uImage,0);gl.uniform1i(uniforms.uClosed,1);gl.uniform1i(uniforms.uMap,2);gl.enable(gl.BLEND);gl.blendFuncSeparate(gl.SRC_ALPHA,gl.ONE_MINUS_SRC_ALPHA,gl.ONE,gl.ONE_MINUS_SRC_ALPHA);makeMesh();
    rig.parts.forEach((p,i)=>{const o=document.createElement('option');o.value=p.id;o.textContent=p.name;$('part-select').append(o);const b=document.createElement('button');b.className='part-item';b.dataset.part=p.id;const img=document.createElement('img');img.src=ASSETS+p.file;img.alt='';b.append(img,document.createTextNode(p.name));b.onclick=()=>selectPart(p.id);$('part-list').append(b);});
    ready=true;wireControls();syncPlay();selectPart('all');$('loading').hidden=true;$('status').textContent='レイヤーモデル読み込み完了';$('model-info').textContent=`${rig.parts.length} LAYERS · ${rig.width} × ${rig.height} · UNITY READY`;
    window.saliaViewer={getState:()=>({...state,ready,blink:blinkValue(performance.now()),parts:rig.parts.length,webglError:gl.getError()}),setTime:t=>{state.time=t;draw();},setBlink:v=>{blinkOverride=v;draw();},selectPart,render:draw};
    last=performance.now();requestAnimationFrame(tick);
  }catch(error){console.error(error);$('loading').textContent=error.message;$('status').textContent='読み込みエラー';}
}
start();
