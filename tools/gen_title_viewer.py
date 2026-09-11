# タイトル背景の層を並べて、位置・大きさ・揺れ方を決めるビューアを作る。
#   python tools/gen_title_viewer.py     →  tools/title_viewer.html
#   python tools/ui_server.py            →  http://localhost:8765/title_viewer.html（「保存」が効く）
#
# 「保存」で Resources/Data/title_layers.json に書き、Unity は次の Play で TitleParallax が読む。
# 揺れの式は TitleParallax.LateUpdate と同じ。座標は板（元絵と同じ比 1847:851）の中心が原点の比。
import base64
import io
import json
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..'))
BG = os.path.join(ROOT, 'assets', 'title', 'BG')
UI = os.path.join(ROOT, 'UnityProject', 'BigBonusBlitz', 'Assets', 'Resources', 'Art', 'UI')
TITLE_JSON = os.path.join(ROOT, 'UnityProject', 'BigBonusBlitz', 'Assets', 'Resources', 'Data', 'title_layers.json')
OUT = os.path.join(HERE, 'title_viewer.html')
PREVIEW_W = 1000   # 埋め込みは縮めて軽くする（見た目の確認用。Unity は元の解像度を使う）

LAYERS = ['sky_mountains_cloudsea', 'sky_mountains', 'castle_mountains_lake', 'castle_mountains', 'castle_lake', 'town_lake', 'terrace_balcony', 'petals_overlay']


def uri_small(path):
    im = Image.open(path).convert('RGBA')
    if im.width > PREVIEW_W:
        im = im.resize((PREVIEW_W, round(im.height * PREVIEW_W / im.width)), Image.LANCZOS)
    buf = io.BytesIO(); im.save(buf, 'PNG', optimize=True)
    return 'data:image/png;base64,' + base64.b64encode(buf.getvalue()).decode('ascii'), im.width / im.height


imgs, aspects = {}, {}
for n in LAYERS:
    p = os.path.join(BG, n + '.png')
    if os.path.exists(p):
        imgs[n], aspects[n] = uri_small(p)
for n in ('title_char', 'title_hair'):
    p = os.path.join(UI, n + '.png')
    if os.path.exists(p):
        imgs[n], aspects[n] = uri_small(p)
saved = json.load(open(TITLE_JSON, encoding='utf-8')) if os.path.exists(TITLE_JSON) else None

HTML = r'''<!doctype html>
<meta charset="utf-8">
<title>BBB タイトル背景ビューア</title>
<style>
  body { margin:0; background:#101218; color:#e8ecf4; font:13px/1.4 system-ui, "Yu Gothic UI", sans-serif; }
  .wrap { display:flex; flex-direction:column; align-items:center; gap:10px; padding:12px; }
  .row { display:flex; gap:8px; flex-wrap:wrap; align-items:center; justify-content:center; }
  button, label, select, input[type=number] { background:#1b2030; color:#fff; border:1px solid #3a4562; border-radius:8px; padding:5px 10px; font:inherit; }
  button { cursor:pointer; } button.save { background:#3ddc84; color:#0a0b0f; } button.on { background:#4da3ff; color:#0a0b0f; }
  input[type=number] { width:70px; padding:3px 6px; } input[type=range] { width:110px; }
  .main { display:flex; gap:12px; align-items:flex-start; }
  .board-wrap { position:relative; background:#000; overflow:hidden; border-radius:12px; user-select:none; }
  .board { position:absolute; left:0; top:0; overflow:hidden; }
  .layer { position:absolute; left:50%; top:50%; transform-origin:50% 50%; pointer-events:none; }
  .layer.edit { pointer-events:auto; cursor:move; }
  .layer.sel { outline:2px dashed #ffcf3f; outline-offset:-2px; }
  .phoneMask { position:absolute; inset:0; pointer-events:none; box-shadow:0 0 0 2000px rgba(0,0,0,.55); border:1px dashed rgba(255,80,80,.6); }
  .side { width:360px; background:#151a2a; border-radius:10px; padding:10px; display:flex; flex-direction:column; gap:8px; }
  .side h3 { margin:0; font-size:13px; color:#ffcf3f; }
  .list { display:flex; flex-direction:column; gap:4px; }
  .item { display:flex; gap:6px; align-items:center; background:#1b2030; border-radius:6px; padding:4px 6px; cursor:pointer; }
  .item.sel { outline:1px solid #ffcf3f; }
  .item .nm { flex:1; }
  .item button { padding:1px 6px; font-size:11px; }
  .prop { display:grid; grid-template-columns: 6em 1fr 3.5em; gap:4px 6px; align-items:center; }
  .prop .v { color:#ffcf3f; text-align:right; font-variant-numeric:tabular-nums; }
  .note { color:#98a3b8; font-size:12px; }
  #msg { color:#3ddc84; font-size:12px; min-width:12em; }
</style>
<div class="wrap">
  <div class="row">
    <button id="save" class="save">保存（Unity に反映）</button>
    <button id="copy">JSON をコピー</button>
    <button id="reset">既定の 3 層に戻す</button>
    <span id="msg"></span>
    <label><input type="checkbox" id="showChar" checked> 立ち絵を見せる</label>
    <label><input type="checkbox" id="phone" checked> iPhone の見える範囲</label>
    <label><input type="checkbox" id="anim" checked> 動かす</label>
    <label>速さ <input type="range" id="speed" min="0.2" max="4" step="0.1" value="1"><span id="speedV">1.0</span></label>
    <label>倍率 <input type="range" id="zoom" min="0.4" max="1.2" step="0.05" value="0.6"><span id="zoomV">0.60</span></label>
  </div>
  <div class="main">
    <div class="board-wrap" id="bw"><div class="board" id="board"></div><div class="phoneMask" id="phoneMask"></div></div>
    <div class="side">
      <h3>層（上が手前）</h3>
      <div class="list" id="list"></div>
      <div class="row" style="justify-content:flex-start"><select id="addSel"></select><button id="add">層を足す</button></div>
      <h3 id="pTitle">（層を選ぶ）</h3>
      <div class="prop" id="props"></div>
      <div class="note">板の上でドラッグして位置。矢印キーで 0.5%（Shift で 2%）。「立ち絵より手前」を付けた層は立ち絵の上に出る。</div>
    </div>
  </div>
  <div class="note">座標は板（元絵と同じ比）の中心が原点。x は幅、y は高さに対する比。揺れ幅は %。式は TitleParallax.LateUpdate と同じ。埋め込みの絵は縮めてあるので粗いが、Unity は元の解像度を使う。</div>
</div>
<script>
const IMG = __IMG__, ASPECT = __ASPECT__, SAVED = __SAVED__;
const BW = 1847, BH = 851;                   // 板（元絵の比）
const PHONE = 2556 / 1179;                   // 見える範囲: 板を高さ合わせで覆い、横をこの比で切る
const MOTIONS = { none:'なし', float:'ゆっくり上下', drift:'漂う（現状）', sway:'左右に振れる', breathe:'脈だけ', orbit:'小さな円', figure8:'8 の字', tilt:'傾くだけ' };
const DEFAULT = [
  { name:'sky_mountains_cloudsea', x:0, y:0, scale:1.12, motion:'drift', ampX:0.4, ampY:0.2, period:40, phase:0, breathe:0.6, rot:0, alpha:1, front:false, visible:true },
  { name:'castle_lake', x:0.04, y:-0.06, scale:0.92, motion:'drift', ampX:1.0, ampY:0.5, period:22, phase:1.7, breathe:1.2, rot:0, alpha:1, front:false, visible:true },
  { name:'terrace_balcony', x:0.03, y:-0.16, scale:1.10, motion:'drift', ampX:1.8, ampY:0.6, period:16, phase:3.1, breathe:1.0, rot:0, alpha:1, front:false, visible:true },
];
const FIELDS = [
  ['x','x（幅の比）',-0.6,0.6,0.005], ['y','y（高さの比）',-0.6,0.6,0.005], ['scale','大きさ',0.3,2.5,0.01],
  ['ampX','揺れ幅 x %',0,8,0.1], ['ampY','揺れ幅 y %',0,8,0.1], ['period','周期 秒',2,90,1], ['phase','位相',0,6.28,0.1],
  ['breathe','脈 %',0,6,0.1], ['rot','傾き °',0,10,0.1], ['alpha','不透明',0,1,0.01],
];
let layers = (SAVED && SAVED.layers && SAVED.layers.length) ? SAVED.layers : JSON.parse(JSON.stringify(DEFAULT));
let sel = 0, zoom = 0.6, t = 0, speed = 1, anim = true, dragging = null;

const bw = document.getElementById('bw'), board = document.getElementById('board');
function layout() {
  const showPhone = document.getElementById('phone').checked;
  bw.style.width = BW * zoom + 'px'; bw.style.height = BH * zoom + 'px';
  board.style.width = BW * zoom + 'px'; board.style.height = BH * zoom + 'px';
  const pm = document.getElementById('phoneMask');
  const pw = BH * PHONE * zoom;
  pm.style.display = showPhone ? 'block' : 'none';
  pm.style.left = ((BW * zoom - pw) / 2) + 'px'; pm.style.width = pw + 'px'; pm.style.top = '0'; pm.style.height = BH * zoom + 'px'; pm.style.inset = 'auto';
}
function rebuild() {
  board.innerHTML = '';
  const backing = document.createElement('div'); Object.assign(backing.style, { position:'absolute', inset:'0', background:'#cce0f7' }); board.appendChild(backing);
  const order = [...layers.filter(l => !l.front), { name:'__char__' }, ...layers.filter(l => l.front)];
  order.forEach(l => {
    if (l.name === '__char__') {
      for (const n of ['title_char', 'title_hair']) if (IMG[n]) { const im = document.createElement('img'); im.className = 'layer charImg'; im.src = IMG[n]; im.style.width = BW * zoom + 'px'; im.style.height = BH * zoom + 'px'; im.style.marginLeft = (-BW * zoom / 2) + 'px'; im.style.marginTop = (-BH * zoom / 2) + 'px'; board.appendChild(im); }
      return;
    }
    if (!IMG[l.name]) return;
    const im = document.createElement('img'); im.className = 'layer edit'; im.src = IMG[l.name]; im.dataset.idx = layers.indexOf(l);
    im.draggable = false;
    im.addEventListener('mousedown', ev => { ev.preventDefault(); select(+im.dataset.idx); dragging = { sx:ev.clientX, sy:ev.clientY, ox:l.x, oy:l.y }; });
    board.appendChild(im);
  });
  document.querySelectorAll('.charImg').forEach(c => c.style.display = document.getElementById('showChar').checked ? '' : 'none');
  renderList(); renderProps(); frame(true);
}
function frame(force) {
  for (const im of board.querySelectorAll('.layer.edit')) {
    const l = layers[+im.dataset.idx];
    const w = BW * l.scale * zoom, h = w / ASPECT[l.name];
    const k = l.period > 0.01 ? t * Math.PI * 2 / l.period + l.phase : l.phase;
    const ax = l.ampX / 100, ay = l.ampY / 100; let dx = 0, dy = 0, rot = 0, sc = 1;
    switch (l.motion) {
      case 'float': dy = ay * Math.sin(k); break;
      case 'drift': dx = ax * Math.sin(k); dy = ay * Math.sin(k * 1.3 + 0.8); break;
      case 'sway': dx = ax * Math.sin(k); rot = -l.rot * Math.sin(k); break;
      case 'orbit': dx = ax * Math.cos(k); dy = ay * Math.sin(k); break;
      case 'figure8': dx = ax * Math.sin(k); dy = ay * Math.sin(k * 2); break;
      case 'tilt': rot = l.rot * Math.sin(k); break;
    }
    if (l.breathe > 0) sc = 1 + l.breathe / 100 * Math.sin(k * 0.5);
    im.style.width = w + 'px'; im.style.height = h + 'px';
    im.style.marginLeft = (-w / 2) + 'px'; im.style.marginTop = (-h / 2) + 'px';
    im.style.transform = `translate(${(l.x + dx) * BW * zoom}px, ${-(l.y + dy) * BH * zoom}px) rotate(${-rot}deg) scale(${sc})`;
    im.style.opacity = l.visible ? l.alpha : 0.15;
    im.classList.toggle('sel', +im.dataset.idx === sel);
  }
}
let last = performance.now();
function tick(now) { const dt = Math.min(0.05, (now - last) / 1000); last = now; if (anim) { t += dt * speed; frame(); } requestAnimationFrame(tick); }
requestAnimationFrame(tick);

function select(i) { sel = i; renderList(); renderProps(); frame(); }
function renderList() {
  const list = document.getElementById('list'); list.innerHTML = '';
  const order = [...layers.map((l, i) => i)].reverse();   // 手前を上に
  for (const i of order) {
    const l = layers[i];
    const d = document.createElement('div'); d.className = 'item' + (i === sel ? ' sel' : '');
    d.innerHTML = `<input type="checkbox" ${l.visible ? 'checked' : ''} title="表示"><span class="nm">${l.name}${l.front ? '（立ち絵より手前）' : ''}</span><button data-a="up">▲</button><button data-a="down">▼</button><button data-a="del">×</button>`;
    d.querySelector('input').onchange = e => { l.visible = e.target.checked; frame(); };
    d.onclick = e => { if (e.target.tagName === 'BUTTON' || e.target.tagName === 'INPUT') return; select(i); };
    d.querySelector('[data-a=up]').onclick = () => { if (i < layers.length - 1) { [layers[i], layers[i+1]] = [layers[i+1], layers[i]]; sel = i + 1; rebuild(); } };
    d.querySelector('[data-a=down]').onclick = () => { if (i > 0) { [layers[i], layers[i-1]] = [layers[i-1], layers[i]]; sel = i - 1; rebuild(); } };
    d.querySelector('[data-a=del]').onclick = () => { layers.splice(i, 1); sel = Math.max(0, Math.min(sel, layers.length - 1)); rebuild(); };
    list.appendChild(d);
  }
}
function renderProps() {
  const p = document.getElementById('props'); p.innerHTML = '';
  const l = layers[sel]; document.getElementById('pTitle').textContent = l ? l.name : '（層を選ぶ）';
  if (!l) return;
  const addRow = (label, el, v) => { const a = document.createElement('div'); a.textContent = label; p.appendChild(a); p.appendChild(el); const s = document.createElement('div'); s.className = 'v'; s.textContent = v; p.appendChild(s); return s; };
  const ms = document.createElement('select'); for (const [k, n] of Object.entries(MOTIONS)) { const o = document.createElement('option'); o.value = k; o.textContent = n; ms.appendChild(o); } ms.value = l.motion; ms.onchange = () => { l.motion = ms.value; };
  addRow('揺れ方', ms, '');
  for (const [k, label, mn, mx, st] of FIELDS) {
    const r = document.createElement('input'); r.type = 'range'; r.min = mn; r.max = mx; r.step = st; r.value = l[k];
    const s = addRow(label, r, (+l[k]).toFixed(st < 0.01 ? 3 : st < 1 ? 2 : 0));
    r.oninput = () => { l[k] = +r.value; s.textContent = (+r.value).toFixed(st < 0.01 ? 3 : st < 1 ? 2 : 0); frame(); };
  }
  const fr = document.createElement('input'); fr.type = 'checkbox'; fr.checked = !!l.front; fr.onchange = () => { l.front = fr.checked; rebuild(); };
  addRow('立ち絵より手前', fr, '');
}
document.addEventListener('mousemove', ev => {
  if (!dragging) return;
  const l = layers[sel]; if (!l) return;
  l.x = Math.round((dragging.ox + (ev.clientX - dragging.sx) / (BW * zoom)) * 200) / 200;
  l.y = Math.round((dragging.oy - (ev.clientY - dragging.sy) / (BH * zoom)) * 200) / 200;
  renderProps(); frame();
});
document.addEventListener('mouseup', () => dragging = null);
document.addEventListener('keydown', ev => {
  if (['INPUT','SELECT'].includes(document.activeElement.tagName)) return;
  const l = layers[sel]; if (!l) return; const st = ev.shiftKey ? 0.02 : 0.005;
  if (ev.key === 'ArrowLeft') l.x -= st; else if (ev.key === 'ArrowRight') l.x += st; else if (ev.key === 'ArrowUp') l.y += st; else if (ev.key === 'ArrowDown') l.y -= st; else return;
  ev.preventDefault(); l.x = +l.x.toFixed(3); l.y = +l.y.toFixed(3); renderProps(); frame();
});
const addSel = document.getElementById('addSel');
for (const n of Object.keys(IMG)) if (!n.startsWith('title_')) { const o = document.createElement('option'); o.value = n; o.textContent = n; addSel.appendChild(o); }
document.getElementById('add').onclick = () => { layers.push({ name:addSel.value, x:0, y:0, scale:1, motion:'drift', ampX:1, ampY:0.5, period:20, phase:0, breathe:0, rot:0, alpha:1, front:false, visible:true }); sel = layers.length - 1; rebuild(); };
document.getElementById('reset').onclick = () => { layers = JSON.parse(JSON.stringify(DEFAULT)); sel = 0; rebuild(); };
const payload = () => ({ version: 1, layers: layers.map(l => ({ ...l })) });
const msg = (s, ok = true) => { const m = document.getElementById('msg'); m.textContent = s; m.style.color = ok ? '#3ddc84' : '#ff4d6d'; };
document.getElementById('copy').onclick = async () => { try { await navigator.clipboard.writeText(JSON.stringify(payload(), null, 2)); msg('JSON をコピーした'); } catch (e) { msg('コピーできない', false); } };
document.getElementById('save').onclick = async () => {
  try { const r = await fetch('/save_title', { method:'POST', headers:{ 'Content-Type':'application/json' }, body: JSON.stringify(payload()) }); const j = await r.json(); msg(j.ok ? `保存した（${j.count} 層）。Unity で Play し直すと反映` : '保存できない: ' + j.error, j.ok); }
  catch (e) { msg('サーバが無い。python tools/ui_server.py を起動するか、JSON をコピーして title_layers.json に貼る', false); }
};
fetch('/title_layout').then(r => r.json()).then(j => { if (j.layers && j.layers.length) { layers = j.layers; sel = 0; rebuild(); } }).catch(() => {});
document.getElementById('showChar').onchange = rebuild;
document.getElementById('phone').onchange = layout;
document.getElementById('anim').onchange = e => anim = e.target.checked;
document.getElementById('speed').oninput = e => { speed = +e.target.value; document.getElementById('speedV').textContent = speed.toFixed(1); };
document.getElementById('zoom').oninput = e => { zoom = +e.target.value; document.getElementById('zoomV').textContent = zoom.toFixed(2); layout(); rebuild(); };
zoom = Math.max(0.4, Math.min(0.6, Math.floor((innerWidth - 420) / BW * 20) / 20)); document.getElementById('zoom').value = zoom; document.getElementById('zoomV').textContent = zoom.toFixed(2);
layout(); rebuild();
</script>
'''

with open(OUT, 'w', encoding='utf-8') as f:
    f.write(HTML.replace('__IMG__', json.dumps(imgs)).replace('__ASPECT__', json.dumps(aspects)).replace('__SAVED__', json.dumps(saved, ensure_ascii=False)))
print(OUT, os.path.getsize(OUT) // 1024, 'KB')
