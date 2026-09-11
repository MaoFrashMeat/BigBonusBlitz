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
</style>
<div class="wrap">
  <div class="stage" id="stage">
    <div class="reel" style="left:274px"></div><div class="reel" style="left:414px"></div><div class="reel" style="left:554px"></div>
  </div>
  <div class="row" id="presets"></div>
  <div class="row">
    <label>背景の揺れ <input type="range" id="bgAmp" min="0" max="3" step="0.1" value="1"><span class="val" id="bgAmpV">1.0</span></label>
    <label>文字の揺れ <input type="range" id="fgAmp" min="0" max="3" step="0.1" value="1"><span class="val" id="fgAmpV">1.0</span></label>
    <label>速さ <input type="range" id="speed" min="0.2" max="3" step="0.1" value="1"><span class="val" id="speedV">1.0</span></label>
    <label>表示倍率 <input type="range" id="zoom" min="1" max="3" step="0.5" value="2"><span class="val" id="zoomV">2.0</span></label>
    <button id="pop">文字をぽんと出す</button>
  </div>
  <div class="note">式は GameController.AnimateNavi と同じ。倍率 1.0 が今の Unity の値。気に入った倍率を言ってもらえれば C# に写す。</div>
</div>
<script>
const IMG = {
__IMG__
};
// 状態の並び: 各リールの文字と、光の色・明るさ・暗さ
const PRESETS = {
  'ナビ開始（1 ? ?）': [['1','gold',0.45,false],['?','blue',0.2,false],['?','blue',0.2,false]],
  '第一停止後・択（- ? ?）': [['-','none',0,true],['?','blue',0.45,false],['?','blue',0.45,false]],
  '成功（- ○ -）': [['-','none',0,true],['○','gold',0.6,false],['-','none',0,true]],
  '失敗（- ○ ×）': [['-','none',0,true],['○','none',0,true],['×','red',0.5,false]],
  'AT 押し順（1 - -）': [['1','gold',0.55,false],['-','none',0,true],['-','none',0,true]],
};
const GLYPH = {'1':'navi_01','2':'navi_02','3':'navi_03','?':'navi_question','○':'navi_circle','×':'navi_cross','-':'navi_hyphen'};
const GLOW = { gold:'rgba(255,217,77,1)', blue:'rgba(102,153,255,1)', red:'rgba(255,77,102,1)', none:'rgba(0,0,0,0)' };

const stage = document.getElementById('stage');
const cells = [];
for (let i = 0; i < 3; i++) {
  const c = document.createElement('div'); c.className = 'cell'; c.style.left = (274 + 140*i + 66/2 + 0) + 'px';
  c.innerHTML = '<div class="glow"></div><img class="bg"><img class="fg">';
  stage.appendChild(c);
  cells.push({ el:c, glow:c.querySelector('.glow'), bg:c.querySelector('.bg'), fg:c.querySelector('.fg'), glyph:null, popT:9 });
}
const P = { bgAmp:1, fgAmp:1, speed:1, zoom:2 };
for (const k of ['bgAmp','fgAmp','speed','zoom']) {
  const el = document.getElementById(k), v = document.getElementById(k+'V');
  el.oninput = () => { P[k] = parseFloat(el.value); v.textContent = P[k].toFixed(1); if (k==='zoom') applyZoom(); };
}
function applyZoom(){ stage.style.transform = 'scale(' + P.zoom + ')'; stage.style.transformOrigin = '50% 0'; stage.style.marginBottom = (300*(P.zoom-1)) + 'px'; }
applyZoom();

function apply(preset) {
  preset.forEach(([txt, glow, ga, dim], i) => {
    const c = cells[i];
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

// ---- GameController.AnimateNavi と同じ式 ----
let last = performance.now(), t = 0;
function frame(now) {
  const dt = Math.min(0.05, (now - last) / 1000); last = now; t += dt * P.speed;
  cells.forEach((c, i) => {
    const ph = i * 0.9;
    const by = 2.5 * Math.sin(t*1.5 + ph) * P.bgAmp;
    const br = 2.5 * Math.sin(t*0.9 + ph) * P.bgAmp;
    const bs = 1 + 0.03 * Math.sin(t*1.5 + ph + 1.2) * P.bgAmp;
    c.bg.style.transform = `translate(0px, ${-by}px) rotate(${-br}deg) scale(${bs})`;   // Unity は y 上向きなので符号を反転
    c.popT += dt;
    let pop = 1;
    if (c.popT < 0.28) { const u = c.popT / 0.28; pop = 1.4 - 0.4 * (1 - (1-u)*(1-u)); }
    const fx = 1.5 * Math.sin(t*1.1 + ph) * P.fgAmp;
    const fy = 2 + 3.5 * Math.sin(t*2.3 + ph + 2.4) * P.fgAmp;
    const fs = pop * (1 + 0.05 * Math.sin(t*2.3 + ph) * P.fgAmp);
    c.fg.style.transform = `translate(${fx}px, ${-fy}px) scale(${fs})`;
  });
  requestAnimationFrame(frame);
}
requestAnimationFrame(frame);
</script>
'''

with open(OUT, 'w', encoding='utf-8') as f:
    f.write(HTML.replace('__IMG__', img_js))
print(OUT, os.path.getsize(OUT) // 1024, 'KB')
