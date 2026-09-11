# ベルナビの揺れを Unity を開かずに確かめるビューアを作る。
#   python tools/gen_navi_viewer.py   →  tools/navi_viewer.html（画像を埋め込んだ 1 枚。どこで開いても動く）
#
# 揺れの式は GameController.AnimateNavi と同じ。ここで数値を詰めたら C# 側へ写す。
import base64
import os

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..'))
NAVI = os.path.join(ROOT, 'UnityProject', 'BigBonusBlitz', 'Assets', 'Resources', 'Art', 'UI', 'Navi')
OUT = os.path.join(HERE, 'navi_viewer.html')

FILES = ['navi_bg', 'navi_01', 'navi_02', 'navi_03', 'navi_question', 'navi_circle', 'navi_cross', 'navi_hyphen']


def data_uri(path):
    with open(path, 'rb') as f:
        return 'data:image/png;base64,' + base64.b64encode(f.read()).decode('ascii')


imgs = {n: data_uri(os.path.join(NAVI, n + '.png')) for n in FILES}
img_js = ',\n'.join(f'  {n!r}: "{u}"' for n, u in imgs.items())

HTML = r'''<!doctype html>
<meta charset="utf-8">
<title>BBB ナビ ビューア</title>
<style>
  body { margin:0; background:#0a0b0f; color:#e8ecf4; font:14px/1.5 system-ui, sans-serif; }
  .wrap { display:flex; flex-direction:column; align-items:center; gap:14px; padding:16px; }
  .stage { position:relative; width:960px; height:300px; background:linear-gradient(#0f1424,#070912);
           border:1px solid #2a3350; border-radius:12px; overflow:hidden; }
  .reel { position:absolute; top:120px; width:132px; height:170px; background:#05070c; border:3px solid #c8961e; border-radius:8px; }
  .cell { position:absolute; top:52px; width:66px; height:66px; transform-origin:50% 50%; }
  .glow { position:absolute; left:50%; top:50%; width:160px; height:160px; margin:-80px 0 0 -80px; border-radius:50%;
          background:radial-gradient(circle, var(--g) 0%, transparent 60%); opacity:var(--ga); }
  .bg, .fg { position:absolute; left:50%; top:50%; width:92px; height:92px; margin:-46px 0 0 -46px; transform-origin:50% 50%; }
  .row { display:flex; gap:8px; flex-wrap:wrap; align-items:center; justify-content:center; }
  button { background:#27304a; color:#fff; border:1px solid #4a5a88; border-radius:8px; padding:6px 12px; cursor:pointer; }
  button.on { background:#4da3ff; color:#0a0b0f; }
  label { display:flex; gap:6px; align-items:center; background:#151a2a; padding:4px 10px; border-radius:8px; }
  input[type=range] { width:120px; }
  .val { min-width:2.6em; text-align:right; color:#ffcf3f; font-variant-numeric:tabular-nums; }
  .note { color:#98a3b8; font-size:12px; }
  .spark { position:absolute; left:0; top:0; width:12px; height:12px; margin:-6px 0 0 -6px; pointer-events:none;
           background:radial-gradient(circle, #fff 0 18%, rgba(255,240,180,.95) 30%, transparent 32%),
             conic-gradient(from 0deg, transparent 0 10%, rgba(255,235,160,.9) 12% 13%, transparent 15% 35%, rgba(255,235,160,.9) 37% 38%, transparent 40% 60%, rgba(255,235,160,.9) 62% 63%, transparent 65% 85%, rgba(255,235,160,.9) 87% 88%, transparent 90%);
           border-radius:50%; transform-origin:50% 50%; }
  .sheen { position:absolute; left:50%; top:50%; width:92px; height:92px; margin:-46px 0 0 -46px; pointer-events:none;
           -webkit-mask-size:100% 100%; mask-size:100% 100%; -webkit-mask-repeat:no-repeat; mask-repeat:no-repeat;
           background:linear-gradient(var(--ang,115deg), transparent 0%, transparent calc(var(--p) - var(--w)), rgba(255,255,255,0) calc(var(--p) - var(--w)*.6), rgba(255,255,255,var(--a,.75)) var(--p), rgba(255,255,255,0) calc(var(--p) + var(--w)*.6), transparent calc(var(--p) + var(--w)), transparent 100%);
           mix-blend-mode:screen; transform-origin:50% 50%; }
  select { background:#151a2a; color:#fff; border:1px solid #4a5a88; border-radius:6px; padding:4px 6px; }
  .combo { font-family:ui-monospace, monospace; color:#ffcf3f; background:#151a2a; padding:6px 12px; border-radius:8px; }
</style>
<div class="wrap">
  <div class="stage" id="stage">
    <div class="reel" style="left:274px"></div><div class="reel" style="left:414px"></div><div class="reel" style="left:554px"></div>
  </div>
  <div class="row" id="presets"></div>
  <div class="row">
    <label>背景の揺れ <input type="range" id="bgAmp" min="0" max="3" step="0.1" value="0.8"><span class="val" id="bgAmpV">0.8</span></label>
    <label>文字の揺れ <input type="range" id="fgAmp" min="0" max="3" step="0.1" value="0.4"><span class="val" id="fgAmpV">0.4</span></label>
    <label>速さ <input type="range" id="speed" min="0.2" max="3" step="0.1" value="0.7"><span class="val" id="speedV">0.7</span></label>
    <label>表示倍率 <input type="range" id="zoom" min="1" max="3" step="0.5" value="2"><span class="val" id="zoomV">2.0</span></label>
    <label>奥の大きさ <input type="range" id="back" min="0.4" max="1" step="0.02" value="0.7"><span class="val" id="backV">0.70</span></label>
    <button id="pop">文字をぽんと出す</button>
  </div>
  <div class="row">
    <label>背景の揺れ方 <select id="bgMotion"></select></label>
    <label>文字の揺れ方 <select id="fgMotion"></select></label>
    <label>同じ位相で <input type="checkbox" id="sync" checked></label>
  </div>
  <div class="row">
    <label>きらきら <input type="range" id="sparkRate" min="0" max="12" step="1" value="2"><span class="val" id="sparkRateV">2</span>/秒</label>
    <label>粒の大きさ <input type="range" id="sparkSize" min="0.5" max="2.5" step="0.1" value="0.5"><span class="val" id="sparkSizeV">0.5</span></label>
    <label>コーティング 間隔 <input type="range" id="sheenEvery" min="0" max="8" step="0.5" value="1.5"><span class="val" id="sheenEveryV">1.5</span>秒</label>
    <label>幅 <input type="range" id="sheenW" min="5" max="60" step="1" value="22"><span class="val" id="sheenWV">22</span>%</label>
    <label>角度 <input type="range" id="sheenAng" min="0" max="180" step="5" value="115"><span class="val" id="sheenAngV">115</span>°</label>
    <label>強さ <input type="range" id="sheenA" min="0" max="1" step="0.05" value="0.75"><span class="val" id="sheenAV">0.75</span></label>
    <label>当てる <select id="sheenOn"><option value="fg">文字</option><option value="bg">背景</option><option value="both" selected>両方</option></select></label>
  </div>
  <div class="combo" id="combo"></div>
  <div class="note">式は GameController.AnimateNavi と同じ。倍率 1.0・奥 0.66 が今の Unity の値。「ナビ開始」→「第一停止後」で ? が手前に出てくる。気に入った値を言ってもらえれば C# に写す。</div>
</div>
<script>
const IMG = {
__IMG__
};
// 状態の並び: 各リールの文字と、光の色・明るさ・暗さ
// [文字, 光の色, 光の強さ, 暗くするか, 奥行き(0=手前 1=次 2=奥)]
const PRESETS = {
  'ナビ開始（1 ? ?）': [['1','gold',0.45,false,0],['?','blue',0.2,false,1],['?','blue',0.2,false,1]],
  '第一停止後・択（- ? ?）': [['-','none',0,true,2],['?','blue',0.45,false,0],['?','blue',0.45,false,0]],
  '成功（- ○ -）': [['-','none',0,true,2],['○','gold',0.6,false,0],['-','none',0,true,2]],
  '失敗（- ○ ×）': [['-','none',0,true,2],['○','none',0,true,0],['×','red',0.5,false,0]],
  'AT 押し順（1 - -）': [['1','gold',0.55,false,0],['-','none',0,true,2],['-','none',0,true,2]],
};
const GLYPH = {'1':'navi_01','2':'navi_02','3':'navi_03','?':'navi_question','○':'navi_circle','×':'navi_cross','-':'navi_hyphen'};
const GLOW = { gold:'rgba(255,217,77,1)', blue:'rgba(102,153,255,1)', red:'rgba(255,77,102,1)', none:'rgba(0,0,0,0)' };

const stage = document.getElementById('stage');
const cells = [];
for (let i = 0; i < 3; i++) {
  const c = document.createElement('div'); c.className = 'cell'; c.style.left = (274 + 140*i + 66/2 + 0) + 'px';
  c.innerHTML = '<div class="glow"></div><img class="bg"><div class="sheen sheenBg"></div><img class="fg"><div class="sheen sheenFg"></div>';
  stage.appendChild(c);
  cells.push({ el:c, glow:c.querySelector('.glow'), bg:c.querySelector('.bg'), fg:c.querySelector('.fg'),
               sheenBg:c.querySelector('.sheenBg'), sheenFg:c.querySelector('.sheenFg'),
               glyph:null, popT:9, depth:1, depthTarget:1, sparks:[], sparkAcc:0, sheenT:-1, sheenNext:0.6 + i*0.8 });
}
// 既定は本人が選んだ組み合わせ（Unity の AnimateNavi と同じ）
const P = { bgAmp:0.8, fgAmp:0.4, speed:0.7, zoom:2, back:0.7, sparkRate:2, sparkSize:0.5, sheenEvery:1.5, sheenW:22, sheenAng:115, sheenA:0.75,
            bgMotion:'float', fgMotion:'bob', sync:true, sheenOn:'both' };
const DEC = { back:2, sparkRate:0, sheenW:0, sheenAng:0, sheenA:2 };
for (const k of ['bgAmp','fgAmp','speed','zoom','back','sparkRate','sparkSize','sheenEvery','sheenW','sheenAng','sheenA']) {
  const el = document.getElementById(k), v = document.getElementById(k+'V');
  el.oninput = () => { P[k] = parseFloat(el.value); v.textContent = P[k].toFixed(DEC[k] ?? 1); if (k==='zoom') applyZoom(); showCombo(); };
}

// ---- 揺れ方。どれも (t, 位相, 倍率) → {x, y, rot, s}。単位は px / 度 / 倍 ----
const MOTIONS = {
  none:     { name:'なし',                 f:(t,ph,a)=>({x:0,y:0,rot:0,s:1}) },
  float:    { name:'ゆっくり上下',         f:(t,ph,a)=>({x:0, y:2.5*a*Math.sin(t*1.5+ph), rot:0, s:1}) },
  bob:      { name:'上下（速め）',         f:(t,ph,a)=>({x:1.5*a*Math.sin(t*1.1+ph), y:3.5*a*Math.sin(t*2.3+ph+2.4), rot:0, s:1+0.05*a*Math.sin(t*2.3+ph)}) },
  hover:    { name:'浮遊（上下+傾き+脈）', f:(t,ph,a)=>({x:0, y:2.5*a*Math.sin(t*1.5+ph), rot:2.5*a*Math.sin(t*0.9+ph), s:1+0.03*a*Math.sin(t*1.5+ph+1.2)}) },
  sway:     { name:'振り子（左右）',       f:(t,ph,a)=>({x:4*a*Math.sin(t*1.3+ph), y:0, rot:-5*a*Math.sin(t*1.3+ph), s:1}) },
  breathe:  { name:'呼吸（大きさだけ）',   f:(t,ph,a)=>({x:0, y:0, rot:0, s:1+0.07*a*Math.sin(t*1.8+ph)}) },
  orbit:    { name:'小さな円',             f:(t,ph,a)=>({x:3*a*Math.cos(t*1.4+ph), y:3*a*Math.sin(t*1.4+ph), rot:0, s:1}) },
  figure8:  { name:'8 の字',               f:(t,ph,a)=>({x:4*a*Math.sin(t*1.2+ph), y:2*a*Math.sin(t*2.4+2*ph), rot:0, s:1}) },
  tilt:     { name:'傾きだけ（ゆらゆら）', f:(t,ph,a)=>({x:0, y:0, rot:6*a*Math.sin(t*1.1+ph), s:1}) },
  heartbeat:{ name:'鼓動（二拍）',         f:(t,ph,a)=>{ const u=((t*1.1+ph)%(2*Math.PI))/(2*Math.PI); const b=Math.exp(-((u-0.05)**2)/0.004)+0.6*Math.exp(-((u-0.22)**2)/0.004); return {x:0,y:0,rot:0,s:1+0.12*a*b}; } },
  drift:    { name:'漂う（低周波の合成）', f:(t,ph,a)=>({x:2.5*a*(Math.sin(t*0.7+ph)+0.5*Math.sin(t*1.9+ph*2)), y:2*a*(Math.sin(t*0.9+ph+1)+0.5*Math.sin(t*2.3+ph)), rot:2*a*Math.sin(t*0.5+ph), s:1}) },
  shiver:   { name:'小刻みに震える',       f:(t,ph,a)=>({x:0.8*a*Math.sin(t*23+ph), y:0.6*a*Math.sin(t*31+ph), rot:1.2*a*Math.sin(t*19+ph), s:1}) },
  bounce:   { name:'跳ねる（着地で潰れ）', f:(t,ph,a)=>{ const u=Math.abs(Math.sin(t*2.2+ph)); const sq=1-0.08*a*Math.max(0,0.15-u)/0.15; return {x:0, y:6*a*u, rot:0, s:sq, sx:1+(1-sq)}; } },
  spin:     { name:'ゆっくり一回転',       f:(t,ph,a)=>({x:0, y:0, rot:(t*20*a+ph*57)%360, s:1}) },
};
for (const id of ['bgMotion','fgMotion']) {
  const sel = document.getElementById(id);
  for (const [k, m] of Object.entries(MOTIONS)) { const o = document.createElement('option'); o.value = k; o.textContent = m.name; sel.appendChild(o); }
  sel.value = P[id]; sel.onchange = () => { P[id] = sel.value; showCombo(); };
}
document.getElementById('sync').onchange = e => { P.sync = e.target.checked; showCombo(); };
document.getElementById('sheenOn').onchange = e => { P.sheenOn = e.target.value; showCombo(); };
function showCombo(){
  document.getElementById('combo').textContent =
    `背景=${P.bgMotion} x${P.bgAmp}  文字=${P.fgMotion} x${P.fgAmp}  速さ${P.speed}  奥${P.back}  ` +
    `きらきら${P.sparkRate}/秒 大きさ${P.sparkSize}  コーティング ${P.sheenEvery}秒ごと 幅${P.sheenW}% 角度${P.sheenAng}° 強さ${P.sheenA} 当てる=${P.sheenOn}` + (P.sync ? '  同位相' : '');
}
showCombo();
function applyZoom(){ stage.style.transform = 'scale(' + P.zoom + ')'; stage.style.transformOrigin = '50% 0'; stage.style.marginBottom = (300*(P.zoom-1)) + 'px'; }
applyZoom();

function apply(preset) {
  preset.forEach(([txt, glow, ga, dim, rank], i) => {
    const c = cells[i];
    c.rank = rank;
    c.bg.src = IMG.navi_bg;
    const g = GLYPH[txt];
    if (c.glyph !== g) { c.glyph = g; c.fg.src = IMG[g]; c.popT = 0; }
    c.glow.style.setProperty('--g', GLOW[glow]); c.glow.style.setProperty('--ga', ga);
    const f = dim ? 'brightness(0.6) saturate(0.7)' : 'none';
    c.bg.style.filter = f; c.fg.style.filter = f; c.bg.style.opacity = c.fg.style.opacity = dim ? 0.9 : 1;
  });
}
const pr = document.getElementById('presets');
Object.entries(PRESETS).forEach(([name, p], n) => {
  const b = document.createElement('button'); b.textContent = name;
  b.onclick = () => { [...pr.children].forEach(x => x.classList.remove('on')); b.classList.add('on'); apply(p); };
  pr.appendChild(b); if (n === 0) b.click();
});
document.getElementById('pop').onclick = () => cells.forEach(c => c.popT = 0);

// ---- 揺れ・きらきら・コーティング。C# に写すときはここを見る ----
let last = performance.now(), t = 0;
function spawnSpark(c) {
  const el = document.createElement('div'); el.className = 'spark';
  const r = 30 + Math.random() * 16, a = Math.random() * Math.PI * 2;      // 紋章の縁のあたりに出す
  const sp = { el, x: 33 + Math.cos(a) * r, y: 33 + Math.sin(a) * r, life: 0, dur: 0.5 + Math.random() * 0.5, size: (0.6 + Math.random() * 0.8) * P.sparkSize, rot: Math.random() * 90 };
  el.style.left = sp.x + 'px'; el.style.top = sp.y + 'px';
  c.el.appendChild(el); c.sparks.push(sp);
}
function frame(now) {
  const dt = Math.min(0.05, (now - last) / 1000); last = now; t += dt * P.speed;
  cells.forEach((c, i) => {
    const ph = P.sync ? 0 : i * 0.9;
    // 奥行き: 手前 1.0 / 次 は手前と奥の中間 / 奥 = スライダー。押す番が変わると手前へ出てくる
    const target = c.rank === 0 ? 1 : c.rank === 1 ? (1 + P.back) / 2 : P.back;
    c.depth += (target - c.depth) * (1 - Math.exp(-dt * 9));
    const depth = c.depth;
    const B = MOTIONS[P.bgMotion].f(t, ph, P.bgAmp);
    const bT = `translate(${B.x}px, ${-B.y}px) rotate(${-B.rot}deg) scale(${depth*(B.sx||1)*B.s}, ${depth*B.s})`;   // Unity は y 上向き
    c.bg.style.transform = bT;
    c.popT += dt;
    let pop = 1;
    if (c.popT < 0.28) { const u = c.popT / 0.28; pop = 1.4 - 0.4 * (1 - (1-u)*(1-u)); }
    const F = MOTIONS[P.fgMotion].f(t, ph + 2.4, P.fgAmp);
    const fT = `translate(${F.x}px, ${-(2 + F.y)}px) rotate(${-F.rot}deg) scale(${depth*pop*(F.sx||1)*F.s}, ${depth*pop*F.s})`;
    c.fg.style.transform = fT;
    // コーティング: 一定間隔で光の帯が斜めに走る。文字か背景の形に合わせてマスクする
    c.sheenT += dt;
    if (c.sheenT > c.sheenNext && P.sheenEvery > 0) { c.sheenT = 0; c.sheenNext = P.sheenEvery + Math.random() * 0.6; }
    const prog = P.sheenEvery > 0 ? Math.min(1.2, c.sheenT / 0.6) : 2;   // 0.6 秒で通り抜ける
    for (const [layer, el, tr, src] of [['bg', c.sheenBg, bT, c.bg.src], ['fg', c.sheenFg, fT, c.fg.src]]) {
      const on = P.sheenOn === 'both' || P.sheenOn === layer;
      el.style.display = on && prog <= 1.2 ? 'block' : 'none';
      if (!on) continue;
      el.style.webkitMaskImage = `url(${src})`; el.style.maskImage = `url(${src})`;
      el.style.transform = tr;
      el.style.setProperty('--p', (-20 + prog * 140) + '%'); el.style.setProperty('--w', P.sheenW + '%');
      el.style.setProperty('--ang', P.sheenAng + 'deg'); el.style.setProperty('--a', P.sheenA);
    }
    // きらきら: 紋章の縁に星が生まれて、膨らんで消える（押す番のバッジに多く出す）
    c.sparkAcc += dt * P.sparkRate * (c.rank === 0 ? 1 : 0.35);
    while (c.sparkAcc >= 1) { c.sparkAcc -= 1; spawnSpark(c); }
    for (const sp of c.sparks) {
      sp.life += dt; const u = sp.life / sp.dur; const k = Math.sin(Math.PI * Math.min(1, u));
      sp.el.style.transform = `scale(${sp.size * k}) rotate(${sp.rot + u * 60}deg)`; sp.el.style.opacity = k;
    }
    c.sparks = c.sparks.filter(sp => { if (sp.life >= sp.dur) { sp.el.remove(); return false; } return true; });
  });
  requestAnimationFrame(frame);
}
requestAnimationFrame(frame);
</script>
'''

with open(OUT, 'w', encoding='utf-8') as f:
    f.write(HTML.replace('__IMG__', img_js))
print(OUT, os.path.getsize(OUT) // 1024, 'KB')
