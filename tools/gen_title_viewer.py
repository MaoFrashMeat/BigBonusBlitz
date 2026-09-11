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
PETALS = []
for i in range(1, 17):
    p = os.path.join(UI, 'Title', f'petal_{i}.png')
    if not os.path.exists(p):
        break
    im = Image.open(p).convert('RGBA').resize((128, 128), Image.LANCZOS)
    buf = io.BytesIO(); im.save(buf, 'PNG', optimize=True)
    PETALS.append('data:image/png;base64,' + base64.b64encode(buf.getvalue()).decode('ascii'))
saved = json.load(open(TITLE_JSON, encoding='utf-8')) if os.path.exists(TITLE_JSON) else None

# UI 部品（舞台に置くもの）。ロゴと TAP TO START、枠（9 分割）、アイコン
MANIFEST = os.path.join(ROOT, 'assets', 'title', 'parts', 'frames', 'frames_manifest.json')
man = json.load(open(MANIFEST, encoding='utf-8')) if os.path.exists(MANIFEST) else {}
UI_IMG, UI_ASPECT, FRAME_SLICE, FRAME_WIDTH = {}, {}, {}, {}


def embed(path, max_w):
    im = Image.open(path).convert('RGBA'); k = 1.0
    if im.width > max_w:
        k = max_w / im.width; im = im.resize((max_w, max(1, round(im.height * k))), Image.LANCZOS)
    buf = io.BytesIO(); im.save(buf, 'PNG', optimize=True)
    return 'data:image/png;base64,' + base64.b64encode(buf.getvalue()).decode('ascii'), k, im.width / im.height


for n in ('bbb_logo_main',):
    p = os.path.join(UI, n + '.png')
    if os.path.exists(p): UI_IMG[n], _, UI_ASPECT[n] = embed(p, 600)
p = os.path.join(UI, 'Title', 'tap_to_start.png')
if os.path.exists(p): UI_IMG['tap_to_start'], _, UI_ASPECT['tap_to_start'] = embed(p, 600)
for n in ('pill_navy_sm', 'btn_blue', 'btn_pill_blue', 'plate_hex_sky', 'plate_hex_cream', 'bar_cream_sm', 'toast_brown', 'btn_cream'):
    p = os.path.join(UI, 'Frames', n + '.png')
    if os.path.exists(p) and man.get(n, {}).get('border'):
        UI_IMG['frame_' + n], k, _ = embed(p, 512)
        FRAME_SLICE[n] = [round(v * k) for v in man[n]['border']]
        FRAME_WIDTH[n] = [v / man[n].get('scale', 1.0) for v in man[n]['border']]
ICON_NAMES = []
icon_dirs = [os.path.join(UI, 'Icons'), os.path.join(ROOT, 'assets', 'title', 'parts', 'icon_v2')]
seen = set()
for d in icon_dirs:
    if not os.path.isdir(d): continue
    for fn in sorted(os.listdir(d)):
        if fn.endswith('.png') and fn[:-4] not in seen:
            seen.add(fn[:-4]); UI_IMG['icon_' + fn[:-4]], _, _ = embed(os.path.join(d, fn), 96); ICON_NAMES.append(fn[:-4])

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
  .prop { display:grid; grid-template-columns: 6em 1fr 4.6em; gap:4px 6px; align-items:center; }
  .prop .v { color:#ffcf3f; text-align:right; font-variant-numeric:tabular-nums; }
  .prop input.numv { width:100%; background:#1b2030; color:#ffcf3f; border:1px solid #3a4562; border-radius:6px; padding:2px 4px; text-align:right; font:inherit; font-variant-numeric:tabular-nums; }
  .note { color:#98a3b8; font-size:12px; }
  #msg { color:#3ddc84; font-size:12px; min-width:12em; }
  .petals { position:absolute; inset:0; pointer-events:none; overflow:hidden; }
  .petal, .pglow { position:absolute; left:0; top:0; transform-origin:50% 50%; pointer-events:none; }
  .pglow { border-radius:50%; background:radial-gradient(circle, rgba(255,179,217,1) 0%, rgba(255,179,217,.5) 35%, transparent 70%); }
  .charBox { position:absolute; border:1px dashed rgba(255,207,63,.8); pointer-events:none; display:none; }
  .tabs { display:flex; gap:4px; } .tabs button { flex:1; }
  .stageBox { position:absolute; pointer-events:none; border:1px dashed rgba(77,163,255,.7); }
  .guides { position:absolute; left:0; top:0; pointer-events:none; }
  .gl { position:absolute; background:rgba(255,120,255,.55); }
  .gl.v { width:1px; top:0; bottom:0; } .gl.h { height:1px; left:0; right:0; }
  .gl.margin { background:rgba(255,200,60,.6); }
  .gl.center { background:rgba(80,255,180,.55); }
  .gl.hit { background:#ffcf3f; box-shadow:0 0 4px #ffcf3f; }
  .gl.edge { background:rgba(120,200,255,.6); }
  .uiel { position:absolute; box-sizing:border-box; pointer-events:auto; cursor:move; transform-origin:50% 50%; }
  .uiel.sel { outline:2px solid #ffcf3f; outline-offset:2px; z-index:60; }
  .uiel .hd { position:absolute; right:-6px; bottom:-6px; width:12px; height:12px; background:#ffcf3f; border:1px solid #000; cursor:nwse-resize; display:none; z-index:61; }
  .uiel.sel .hd { display:block; }
  .uiel .skin { position:absolute; inset:0; pointer-events:none; box-sizing:border-box; border-style:solid; border-color:transparent; }
  .uiel .skin.proc { background:#27304a; border-radius:20px; }
  .uiel .lab { position:absolute; left:0; top:0; right:0; bottom:0; display:flex; align-items:center; justify-content:center; color:#f4f6fa; font-weight:700; text-shadow:1.5px -1.5px 0 rgba(0,0,0,.85); pointer-events:none; white-space:nowrap; }
  .uiel .ico { position:absolute; top:50%; pointer-events:none; }
  .uiel img.pic { position:absolute; inset:0; width:100%; height:100%; object-fit:contain; pointer-events:none; }
  input[type=text] { background:#1b2030; color:#fff; border:1px solid #3a4562; border-radius:6px; padding:3px 6px; width:100%; }
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
    <div class="board-wrap" id="bw"><div class="board" id="board"></div><div class="charBox" id="charBox"></div><div class="stageBox" id="stageBox"></div><div class="guides" id="guides"></div><div class="uiRoot" id="uiRoot" style="position:absolute;left:0;top:0;pointer-events:none"></div><div class="phoneMask" id="phoneMask"></div></div>
    <div class="side">
      <div class="tabs"><button id="tabLayers" class="on">層</button><button id="tabPetals">花びら</button><button id="tabUi">UI</button></div>
      <div id="paneLayers">
      <h3>層（上が手前）</h3>
      <div class="list" id="list"></div>
      <div class="row" style="justify-content:flex-start"><select id="addSel"></select><button id="add">層を足す</button></div>
      <h3 id="pTitle">（層を選ぶ）</h3>
      <div class="prop" id="props"></div>
      <div class="note">板の上でドラッグして位置。矢印キーで 0.5%（Shift で 2%）。「立ち絵より手前」を付けた層は立ち絵の上に出る。</div>
      </div>
      <div id="paneUi" style="display:none">
      <h3>補助線と吸着</h3>
      <div class="row" style="justify-content:flex-start">
        <label><input type="checkbox" id="gShow" checked> 線を出す</label>
        <label><input type="checkbox" id="gSnap" checked> 吸着</label>
        <label><input type="checkbox" id="gCenter" checked> 中心線</label>
        <label><input type="checkbox" id="gEdges" checked> 他の部品の端</label>
      </div>
      <div class="prop" id="gProps"></div>
      <div class="note">マージン = 舞台の端からの余白（黄）。パディング = 部品どうしの間隔で、他の部品の端から離れた線（桃）。吸着距離の内側に入ると線が光って引き寄せる。</div>
      <h3>UI 部品（舞台 960×540）</h3>
      <div class="list" id="uiList"></div>
      <h3 id="uiTitle">（部品を選ぶ）</h3>
      <div class="prop" id="uiProps"></div>
      <div class="note">舞台の上でドラッグして位置、右下の角で大きさ。矢印キーで 1px（Shift で 10px）。座標は舞台の中心が原点。ロゴと TAP は縦横比を保つ。</div>
      </div>
      <div id="panePetals" style="display:none">
      <h3>花びら（TitleAmbience）</h3>
      <div class="prop" id="pprops"></div>
      <div class="note">「立ち絵の枠」は板の中心原点の比。枠の中では「立ち絵で薄く」のぶん透ける。「立ち絵の後ろ」を付けると花びらは立ち絵に隠れる（光の玉は手前のまま）。</div>
      </div>
    </div>
  </div>
  <div class="note">座標は板（元絵と同じ比）の中心が原点。x は幅、y は高さに対する比。揺れ幅は %。式は TitleParallax.LateUpdate と同じ。埋め込みの絵は縮めてあるので粗いが、Unity は元の解像度を使う。</div>
</div>
<script>
const IMG = __IMG__, ASPECT = __ASPECT__, SAVED = __SAVED__;
// スライダー + 数値の欄。数値は直接打てて、スライダーの範囲外も受け付ける
function numRow(grid, label, value, mn, mx, st, onChange) {
  const a = document.createElement('div'); a.textContent = label; grid.appendChild(a);
  const r = document.createElement('input'); r.type = 'range'; r.min = mn; r.max = mx; r.step = st; r.value = value; grid.appendChild(r);
  const n = document.createElement('input'); n.type = 'number'; n.className = 'numv'; n.step = st; n.value = value; grid.appendChild(n);
  const dec = st < 0.01 ? 3 : st < 1 ? 2 : 0;
  r.oninput = () => { const v = +r.value; n.value = v.toFixed(dec); onChange(v); };
  n.onchange = () => { const v = +n.value; if (isNaN(v)) return; r.value = v; onChange(v); };
  n.onkeydown = ev => { if (ev.key === 'Enter') { n.blur(); } ev.stopPropagation(); };
  return { set: v => { r.value = v; n.value = (+v).toFixed(dec); } };
}
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
const PETAL_IMG = __PETALS__;
const PETAL_DEF = { count:22, orbs:26, sizeMin:14, sizeMax:32, alpha:0.9, blur:0, glow:0.6, glowSize:2.4, fallMin:14, fallMax:30, driftMin:4, driftMax:16, spin:70, flutter:22, behindChar:false, charMask:0, charX:0.28, charY:-0.05, charW:0.42, charH:0.95, orbAlpha:0.55 };
let petals = Object.assign({}, PETAL_DEF, (SAVED && SAVED.petals) || {});
const PFIELDS = [
  ['count','数',0,80,1], ['sizeMin','大きさ 小',4,60,1], ['sizeMax','大きさ 大',4,80,1], ['alpha','不透明',0,1,0.05], ['blur','ぼかし 0〜3',0,3,1],
  ['glow','光の強さ',0,1.5,0.05], ['glowSize','光の大きさ',1,5,0.1], ['fallMin','落ちる速さ 小',0,80,1], ['fallMax','落ちる速さ 大',0,120,1],
  ['driftMin','横流れ 小',0,40,1], ['driftMax','横流れ 大',0,60,1], ['spin','回転 °/秒',0,200,5], ['flutter','風の煽り °',0,60,1],
  ['charMask','立ち絵で薄く',0,1,0.05], ['charX','枠 x',-0.5,0.5,0.01], ['charY','枠 y',-0.5,0.5,0.01], ['charW','枠 幅',0.05,1,0.01], ['charH','枠 高さ',0.05,1.2,0.01],
  ['orbs','光の玉の数',0,60,1], ['orbAlpha','光の玉の不透明',0,1,0.05],
];
// ---- 花びらの動き（TitleAmbience と同じ式。単位は板の px で、揺れの見え方を合わせるため 0.6 倍で描く）----
let bits = [], petalRng = 1;
function prand() { petalRng = (petalRng * 1103515245 + 12345) & 0x7fffffff; return petalRng / 0x7fffffff; }
const PR = (a, b) => a + prand() * (b - a);
function petalLayer() {
  let pl = board.querySelector('.petals.' + (petals.behindChar ? 'behind' : 'front'));
  return pl;
}
function buildPetals() {
  board.querySelectorAll('.petals').forEach(n => n.remove());
  bits = []; petalRng = 20260911;
  // 立ち絵の前と後ろの 2 枚の置き場を作り、設定に応じてどちらかに入れる
  const behind = document.createElement('div'); behind.className = 'petals behind';
  const front = document.createElement('div'); front.className = 'petals front';
  const chars = board.querySelectorAll('.charImg');
  if (chars.length) board.insertBefore(behind, chars[0]); else board.appendChild(behind);
  board.appendChild(front);
  const host = petals.behindChar ? behind : front;
  const W = BW * zoom, H = BH * zoom;
  const total = petals.orbs + petals.count;
  for (let i = 0; i < total; i++) {
    const isPetal = i >= petals.orbs;
    const b = { petal: isPetal, x:0, y:0, vx:0, vy:0, spin:0, angle:0, size:10, phase:0, wob:0, life:0, maxLife:1 };
    if (isPetal && petals.glow > 0) { b.glow = document.createElement('div'); b.glow.className = 'pglow'; host.appendChild(b.glow); }
    b.el = document.createElement(isPetal ? 'img' : 'div'); b.el.className = isPetal ? 'petal' : 'pglow';
    if (isPetal) { b.el.src = PETAL_IMG[i % PETAL_IMG.length]; }
    else b.el.style.background = 'radial-gradient(circle, rgba(255,243,200,1) 0%, rgba(191,228,255,.5) 40%, transparent 70%)';
    (isPetal ? host : front).appendChild(b.el);
    resetBit(b, true, W, H); bits.push(b);
  }
  document.getElementById('charBox').style.display = petals.charMask > 0 ? 'block' : 'none';
}
function resetBit(b, anywhere, W, H) {
  b.phase = PR(0, 100); b.wob = PR(18, 46); b.maxLife = PR(6, 13); b.life = anywhere ? PR(0, b.maxLife) : 0;
  if (b.petal) {
    b.size = PR(petals.sizeMin, petals.sizeMax); b.x = PR(-W/2, W/2); b.y = anywhere ? PR(-H/2, H/2) : H/2 + b.size;
    b.vx = -PR(petals.driftMin, petals.driftMax); b.vy = -PR(petals.fallMin, petals.fallMax); b.spin = PR(-petals.spin, petals.spin); b.angle = PR(0, 360);
  } else {
    b.size = PR(5, 16); b.x = PR(-W/2, W/2); b.y = anywhere ? PR(-H/2, H/2) : -H/2 - b.size; b.vx = PR(-6, 6); b.vy = PR(7, 20); b.spin = 0; b.angle = 0;
  }
}
function inCharBox(x, y) {
  if (petals.charMask <= 0) return 0;
  const px = x / (BW * zoom), py = y / (BH * zoom);
  const dx = Math.abs(px - petals.charX) / (petals.charW / 2), dy = Math.abs(py - petals.charY) / (petals.charH / 2);
  const d = Math.max(dx, dy); return 1 - Math.min(1, Math.max(0, (d - 0.85) / 0.15));
}
function framePetals(dt) {
  const W = BW * zoom, H = BH * zoom, s = zoom;   // Unity の px を板の倍率で縮める
  for (const b of bits) {
    b.life += dt;
    b.x += (b.vx + Math.sin((t + b.phase) * 0.7) * b.wob * 0.35) * dt * s; b.y += b.vy * dt * s; b.angle += b.spin * dt;
    const k = Math.min(1, b.life / b.maxLife); let a = Math.sin(k * Math.PI);
    if (b.petal) { a *= petals.alpha; a *= 1 - petals.charMask * inCharBox(b.x, b.y); } else a *= petals.orbAlpha;
    const size = b.size * s, cx = W/2 + b.x, cy = H/2 - b.y;
    const flutter = b.petal ? petals.flutter * Math.sin((t + b.phase) * 1.6) : 0;
    // 花びらの絵は画布の半分に描いてある（cut_sheets.py の PETAL_FILL）。見える大きさを size にするため 2 倍で置く
    const box = b.petal ? size * 2 : size;
    b.el.style.width = b.el.style.height = box + 'px';
    if (b.petal) b.el.style.filter = petals.blur > 0 ? `blur(${[0, 0.04, 0.09, 0.16][petals.blur] * size}px)` : '';
    b.el.style.transform = `translate(${cx - box/2}px, ${cy - box/2}px) rotate(${-(b.angle + flutter)}deg)`; b.el.style.opacity = a;
    if (b.glow) { const pulse = 0.55 + 0.45 * Math.sin((t + b.phase) * 2.2); const gs = size * petals.glowSize * (0.9 + 0.25 * pulse); b.glow.style.width = b.glow.style.height = gs + 'px'; b.glow.style.transform = `translate(${cx - gs/2}px, ${cy - gs/2}px)`; b.glow.style.opacity = a * petals.glow * pulse; }
    if (b.life >= b.maxLife || b.y > H/2 + 40 || b.y < -H/2 - 40 || b.x < -W/2 - 60 || b.x > W/2 + 60) resetBit(b, false, W, H);
  }
  const cb = document.getElementById('charBox');
  cb.style.left = (W/2 + (petals.charX - petals.charW/2) * W) + 'px'; cb.style.top = (H/2 - (petals.charY + petals.charH/2) * H) + 'px';
  cb.style.width = petals.charW * W + 'px'; cb.style.height = petals.charH * H + 'px';
}
function renderPetalProps() {
  const p = document.getElementById('pprops'); p.innerHTML = '';
  const addRow = (label, el, v) => { const a = document.createElement('div'); a.textContent = label; p.appendChild(a); p.appendChild(el); const s = document.createElement('div'); s.className = 'v'; s.textContent = v; p.appendChild(s); return s; };
  const bc = document.createElement('input'); bc.type = 'checkbox'; bc.checked = !!petals.behindChar; bc.onchange = () => { petals.behindChar = bc.checked; buildPetals(); };
  addRow('立ち絵の後ろ', bc, '');
  for (const [k, label, mn, mx, st] of PFIELDS)
    numRow(p, label, petals[k], mn, mx, st, v => { petals[k] = v; if (['count','orbs','blur','glow','glowSize'].includes(k)) buildPetals(); });
}
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
// ==== UI 部品（TitleScreen の配置。TitleUiLayout が読む）====
const UI_IMG = __UIIMG__, UI_ASPECT = __UIASPECT__, SLICE = __SLICE__, WIDTH = __WIDTH__, ICONS = __ICONS__;
const SW = 960, SH = 540;
const stageScale = () => (BH * zoom) / SH;      // 舞台 px → 画面 px（板を高さ合わせで覆う）
const UI_FRAMES = ['', 'none', 'pill_navy_sm', 'btn_blue', 'btn_pill_blue', 'plate_hex_sky', 'plate_hex_cream', 'bar_cream_sm', 'toast_brown', 'btn_cream'];
const logoH = 470 * (UI_ASPECT.bbb_logo_main ? 1 / UI_ASPECT.bbb_logo_main : 0.58);
const tapH = 420 * (UI_ASPECT.tap_to_start ? 1 / UI_ASPECT.tap_to_start : 0.2);
const UI_DEFS = [
  { id:'logo', name:'ロゴ', kind:'img', img:'bbb_logo_main', x:-166, y:112, w:470, h:logoH, keepAspect:true },
  { id:'tap', name:'TAP TO START', kind:'img', img:'tap_to_start', x:-166, y:-52, w:420, h:tapH, keepAspect:true },
  { id:'pillNews', name:'お知らせ', kind:'pill', x:-380, y:-186, w:152, h:40, frame:'pill_navy_sm', icon:'bell', label:'お知らせ', font:13, iconSize:18, iconX:26, labelX:11 },
  { id:'pillConfig', name:'設定', kind:'pill', x:-216, y:-186, w:152, h:40, frame:'pill_navy_sm', icon:'gear', label:'設定', font:13, iconSize:18, iconX:26, labelX:11 },
  { id:'pillTransfer', name:'引き継ぎ', kind:'pill', x:-52, y:-186, w:152, h:40, frame:'pill_navy_sm', icon:'chain', label:'引き継ぎ', font:13, iconSize:18, iconX:26, labelX:11 },
  { id:'menu', name:'右上メニュー', kind:'pill', x:446, y:236, w:44, h:44, frame:'', icon:'', label:'≡', font:22, iconSize:18, iconX:22, labelX:0 },
];
const UI_BASE = Object.fromEntries(UI_DEFS.map(d => [d.id, { x:d.x, y:d.y, w:d.w, h:d.h, frame:d.frame||'', icon:d.icon||'', label:d.label||'', font:d.font||13, iconSize:d.iconSize||18, iconX:d.iconX||26, labelX:d.labelX||0, visible:true }]));
let ui = JSON.parse(JSON.stringify(UI_BASE));
function loadUi(obj) { for (const [id, e] of Object.entries(obj || {})) if (ui[id]) Object.assign(ui[id], e); }
if (SAVED && SAVED.ui) loadUi(SAVED.ui);
let uiSel = null, uiDrag = null;
// 補助線。値は舞台 px。margin は舞台の端からの余白、padding は部品どうしの間隔、snap は吸着距離
const guide = { show:true, snap:true, center:true, edges:true, marginX:24, marginY:24, padding:12, snapDist:6, gridStep:0 };
if (SAVED && SAVED.guide) Object.assign(guide, SAVED.guide);
let hitLines = { x:[], y:[] };
function guideLines(excludeId) {
  // 返り値: 舞台座標の縦線（x）と横線（y）。種類は色分け用
  const xs = [], ys = [];
  const add = (arr, v, kind) => arr.push({ v, kind });
  add(xs, -SW/2 + guide.marginX, 'margin'); add(xs, SW/2 - guide.marginX, 'margin');
  add(ys, -SH/2 + guide.marginY, 'margin'); add(ys, SH/2 - guide.marginY, 'margin');
  if (guide.center) { add(xs, 0, 'center'); add(ys, 0, 'center'); }
  if (guide.edges) for (const d of UI_DEFS) {
    if (d.id === excludeId) continue; const e = ui[d.id]; if (!e.visible) continue;
    const l = e.x - e.w/2, r = e.x + e.w/2, t = e.y + e.h/2, b = e.y - e.h/2;
    add(xs, l, 'edge'); add(xs, r, 'edge'); add(xs, e.x, 'edge'); add(ys, t, 'edge'); add(ys, b, 'edge'); add(ys, e.y, 'edge');
    if (guide.padding > 0) { add(xs, l - guide.padding, 'pad'); add(xs, r + guide.padding, 'pad'); add(ys, t + guide.padding, 'pad'); add(ys, b - guide.padding, 'pad'); }
  }
  if (guide.gridStep > 0) for (let v = 0; v <= SW/2; v += guide.gridStep) { add(xs, v, 'grid'); if (v) add(xs, -v, 'grid'); }
  if (guide.gridStep > 0) for (let v = 0; v <= SH/2; v += guide.gridStep) { add(ys, v, 'grid'); if (v) add(ys, -v, 'grid'); }
  return { xs, ys };
}
// 部品の左端・中心・右端（上端・中心・下端）のどれかが線に近ければ寄せる
function snapRect(e, lines) {
  hitLines = { x:[], y:[] };
  if (!guide.snap) return;
  const tryAxis = (cands, lineList, apply) => {
    let best = null;
    for (const c of cands) for (const L of lineList) { const d = Math.abs(c.v - L.v); if (d <= guide.snapDist && (!best || d < best.d)) best = { d, delta: L.v - c.v, L }; }
    if (best) { apply(best.delta); return best.L; }
    return null;
  };
  const hx = tryAxis([{ v:e.x - e.w/2 }, { v:e.x }, { v:e.x + e.w/2 }], lines.xs, dl => e.x += dl);
  const hy = tryAxis([{ v:e.y - e.h/2 }, { v:e.y }, { v:e.y + e.h/2 }], lines.ys, dl => e.y += dl);
  if (hx) hitLines.x.push(hx.v); if (hy) hitLines.y.push(hy.v);
}
function drawGuides() {
  const g = document.getElementById('guides'); g.innerHTML = '';
  const onUi = document.getElementById('paneUi').style.display !== 'none';
  if (!guide.show || !onUi) return;
  const k = stageScale(), ox = (BW * zoom) / 2, oy = (BH * zoom) / 2;
  const sx0 = ox - SW/2*k, sy0 = oy - SH/2*k;
  g.style.width = BW * zoom + 'px'; g.style.height = BH * zoom + 'px';
  const lines = guideLines(uiDrag ? uiDrag.id : null);
  for (const L of lines.xs) { if (L.kind === 'grid' && !uiDrag) continue; const d = document.createElement('div'); d.className = 'gl v ' + L.kind + (hitLines.x.includes(L.v) ? ' hit' : ''); d.style.left = (ox + L.v * k) + 'px'; d.style.top = sy0 + 'px'; d.style.height = SH * k + 'px'; g.appendChild(d); }
  for (const L of lines.ys) { if (L.kind === 'grid' && !uiDrag) continue; const d = document.createElement('div'); d.className = 'gl h ' + L.kind + (hitLines.y.includes(L.v) ? ' hit' : ''); d.style.top = (oy - L.v * k) + 'px'; d.style.left = sx0 + 'px'; d.style.width = SW * k + 'px'; g.appendChild(d); }
}
function renderGuideProps() {
  const p = document.getElementById('gProps'); p.innerHTML = '';
  numRow(p, 'マージン x', guide.marginX, 0, 200, 1, v => { guide.marginX = v; drawGuides(); });
  numRow(p, 'マージン y', guide.marginY, 0, 200, 1, v => { guide.marginY = v; drawGuides(); });
  numRow(p, 'パディング', guide.padding, 0, 100, 1, v => { guide.padding = v; drawGuides(); });
  numRow(p, '吸着距離', guide.snapDist, 0, 30, 1, v => { guide.snapDist = v; });
  numRow(p, 'グリッド（0 で無し）', guide.gridStep, 0, 100, 2, v => { guide.gridStep = v; drawGuides(); });
  for (const [id, k] of [['gShow','show'],['gSnap','snap'],['gCenter','center'],['gEdges','edges']]) { const el = document.getElementById(id); el.checked = guide[k]; el.onchange = () => { guide[k] = el.checked; drawGuides(); }; }
}
const uiNodes = {};
const uiRoot = document.getElementById('uiRoot');
function uiPlace(id) {
  const d = UI_DEFS.find(v => v.id === id), e = ui[id], n = uiNodes[id], k = stageScale();
  const ox = (BW * zoom) / 2, oy = (BH * zoom) / 2;
  n.style.left = (ox + (e.x - e.w/2) * k) + 'px'; n.style.top = (oy - (e.y + e.h/2) * k) + 'px';
  n.style.width = (e.w * k) + 'px'; n.style.height = (e.h * k) + 'px';
  n.style.display = e.visible ? '' : 'none';
  n.querySelectorAll(':scope > .skin, :scope > .lab, :scope > .ico, :scope > img.pic').forEach(x => x.remove());
  if (d.kind === 'img') {
    const im = document.createElement('img'); im.className = 'pic'; im.src = UI_IMG[d.img] || ''; n.appendChild(im);
  } else {
    const sk = document.createElement('div'); sk.className = 'skin';
    const fr = e.frame === '' ? (d.frame || '') : e.frame;
    if (fr && fr !== 'none' && UI_IMG['frame_' + fr] && SLICE[fr]) {
      const [l, b, r, t] = SLICE[fr], [wl, wb, wr, wt] = WIDTH[fr].map(v => v * k);
      sk.style.borderImage = `url(${UI_IMG['frame_' + fr]}) ${t} ${r} ${b} ${l} fill / ${wt}px ${wr}px ${wb}px ${wl}px`;
      sk.style.borderWidth = `${wt}px ${wr}px ${wb}px ${wl}px`;
    } else sk.classList.add('proc');
    n.appendChild(sk);
    if (e.icon && UI_IMG['icon_' + e.icon]) {
      const ic = document.createElement('img'); ic.className = 'ico'; ic.src = UI_IMG['icon_' + e.icon];
      const s = e.iconSize * k; ic.style.width = ic.style.height = s + 'px'; ic.style.left = (e.iconX * k - s/2) + 'px'; ic.style.marginTop = (-s/2) + 'px';
      n.appendChild(ic);
    }
    const lab = document.createElement('div'); lab.className = 'lab'; lab.textContent = e.label || '';
    lab.style.fontSize = (e.font * k) + 'px'; lab.style.paddingLeft = (e.labelX * 2 * k) + 'px'; n.appendChild(lab);
  }
  const hd = document.createElement('div'); hd.className = 'hd'; n.appendChild(hd);
  hd.onmousedown = ev => { ev.stopPropagation(); uiSelect(id); uiDrag = { id, sx:ev.clientX, sy:ev.clientY, ow:e.w, oh:e.h, ox:e.x, oy:e.y, kind:'size' }; };
  n.classList.toggle('sel', uiSel === id);
}
function uiBuild() {
  uiRoot.innerHTML = '';
  for (const d of UI_DEFS) {
    const n = document.createElement('div'); n.className = 'uiel'; n.dataset.id = d.id; uiRoot.appendChild(n); uiNodes[d.id] = n;
    n.onmousedown = ev => { if (ev.target.classList.contains('hd')) return; ev.stopPropagation(); ev.preventDefault(); uiSelect(d.id); const e = ui[d.id]; uiDrag = { id:d.id, sx:ev.clientX, sy:ev.clientY, ox:e.x, oy:e.y, kind:'move' }; };
    uiPlace(d.id);
  }
  const sb = document.getElementById('stageBox'), k = stageScale();
  sb.style.left = ((BW * zoom - SW * k) / 2) + 'px'; sb.style.top = ((BH * zoom - SH * k) / 2) + 'px'; sb.style.width = SW * k + 'px'; sb.style.height = SH * k + 'px';
  uiRenderList(); drawGuides();
}
function uiSelect(id) { uiSel = id; for (const k in uiNodes) uiNodes[k].classList.toggle('sel', k === id); uiRenderList(); uiRenderProps(); }
function uiRenderList() {
  const list = document.getElementById('uiList'); list.innerHTML = '';
  for (const d of UI_DEFS) {
    const e = ui[d.id], it = document.createElement('div'); it.className = 'item' + (d.id === uiSel ? ' sel' : '');
    it.innerHTML = `<input type="checkbox" ${e.visible ? 'checked' : ''} title="表示"><span class="nm">${d.name}</span>`;
    it.querySelector('input').onchange = ev => { e.visible = ev.target.checked; uiPlace(d.id); };
    it.onclick = ev => { if (ev.target.tagName === 'INPUT') return; uiSelect(d.id); };
    list.appendChild(it);
  }
}
function uiRenderProps() {
  const p = document.getElementById('uiProps'); p.innerHTML = '';
  const d = UI_DEFS.find(v => v.id === uiSel); document.getElementById('uiTitle').textContent = d ? `${d.name}（${d.id}）` : '（部品を選ぶ）';
  if (!d) return;
  const e = ui[d.id];
  const row = (label, el, v) => { const a = document.createElement('div'); a.textContent = label; p.appendChild(a); p.appendChild(el); const s = document.createElement('div'); s.className = 'v'; s.textContent = v ?? ''; p.appendChild(s); return s; };
  const num = (k, label, mn, mx, st) => numRow(p, label, e[k], mn, mx, st, v => { e[k] = v; if (k === 'w' && d.keepAspect) e.h = e.w * d.h / d.w; uiPlace(d.id); });
  num('x', 'x', -480, 480, 1); num('y', 'y', -270, 270, 1); num('w', '幅', 20, 960, 1);
  if (!d.keepAspect) num('h', '高さ', 12, 540, 1);
  if (d.kind === 'pill') {
    const fs = document.createElement('select'); for (const f of UI_FRAMES) { const o = document.createElement('option'); o.value = f; o.textContent = f === '' ? `自動（${d.frame || '手続き'}）` : f === 'none' ? '手続き（前の見た目）' : f; fs.appendChild(o); } fs.value = e.frame; fs.onchange = () => { e.frame = fs.value; uiPlace(d.id); }; row('枠', fs);
    const is = document.createElement('select'); { const o = document.createElement('option'); o.value = ''; o.textContent = '（無し）'; is.appendChild(o); } for (const n of ICONS) { const o = document.createElement('option'); o.value = n; o.textContent = n; is.appendChild(o); } is.value = e.icon; is.onchange = () => { e.icon = is.value; uiPlace(d.id); }; row('アイコン', is);
    num('iconSize', 'アイコン大きさ', 8, 64, 1); num('iconX', 'アイコン位置', 0, 200, 1);
    const tx = document.createElement('input'); tx.type = 'text'; tx.value = e.label; tx.oninput = () => { e.label = tx.value; uiPlace(d.id); }; row('文字', tx);
    num('font', '文字の大きさ', 8, 40, 1); num('labelX', '文字のずらし', -60, 60, 1);
  }
  const rs = document.createElement('button'); rs.textContent = 'これだけ既定に戻す'; rs.onclick = () => { Object.assign(e, JSON.parse(JSON.stringify(UI_BASE[d.id]))); uiPlace(d.id); uiRenderProps(); }; row('', rs);
}
document.addEventListener('mousemove', ev => {
  if (!uiDrag) return;
  const e = ui[uiDrag.id], d = UI_DEFS.find(v => v.id === uiDrag.id), k = stageScale();
  const dx = (ev.clientX - uiDrag.sx) / k, dy = -(ev.clientY - uiDrag.sy) / k;
  if (uiDrag.kind === 'move') { e.x = Math.round(uiDrag.ox + dx); e.y = Math.round(uiDrag.oy + dy); snapRect(e, guideLines(uiDrag.id)); }
  else {
    const nw = Math.max(20, Math.round(uiDrag.ow + dx));
    let nh = d.keepAspect ? nw * d.h / d.w : Math.max(12, Math.round(uiDrag.oh - dy));
    e.x = uiDrag.ox + (nw - uiDrag.ow) / 2; e.y = uiDrag.oy - (nh - uiDrag.oh) / 2; e.w = nw; e.h = nh;
    // 大きさを変えるときは右端・下端だけ吸着（左上は固定）
    if (guide.snap) {
      const lines = guideLines(uiDrag.id); hitLines = { x:[], y:[] };
      const r = e.x + e.w/2, b = e.y - e.h/2;
      let bx = null, by = null;
      for (const L of lines.xs) { const dd = Math.abs(r - L.v); if (dd <= guide.snapDist && (!bx || dd < bx.d)) bx = { d:dd, v:L.v }; }
      for (const L of lines.ys) { const dd = Math.abs(b - L.v); if (dd <= guide.snapDist && (!by || dd < by.d)) by = { d:dd, v:L.v }; }
      if (bx) { const l = e.x - e.w/2; e.w = bx.v - l; e.x = l + e.w/2; hitLines.x.push(bx.v); if (d.keepAspect) { e.h = e.w * d.h / d.w; e.y = uiDrag.oy + uiDrag.oh/2 - e.h/2; } }
      if (by && !d.keepAspect) { const t = e.y + e.h/2; e.h = t - by.v; e.y = t - e.h/2; hitLines.y.push(by.v); }
    }
  }
  uiPlace(uiDrag.id); uiRenderProps(); drawGuides();
});
document.addEventListener('mouseup', () => { if (uiDrag) { uiDrag = null; hitLines = { x:[], y:[] }; drawGuides(); } });
document.addEventListener('keydown', ev => {
  if (!uiSel || document.getElementById('paneUi').style.display === 'none' || ['INPUT','SELECT'].includes(document.activeElement.tagName)) return;
  const e = ui[uiSel], st = ev.shiftKey ? 10 : 1;
  if (ev.key === 'ArrowLeft') e.x -= st; else if (ev.key === 'ArrowRight') e.x += st; else if (ev.key === 'ArrowUp') e.y += st; else if (ev.key === 'ArrowDown') e.y -= st; else return;
  ev.preventDefault(); uiPlace(uiSel); uiRenderProps();
});
function showPane(which) {
  for (const [id, pane] of [['tabLayers','paneLayers'],['tabPetals','panePetals'],['tabUi','paneUi']]) { document.getElementById(pane).style.display = id === which ? '' : 'none'; document.getElementById(id).classList.toggle('on', id === which); }
  uiRoot.style.pointerEvents = which === 'tabUi' ? 'auto' : 'none';
  document.getElementById('stageBox').style.display = which === 'tabUi' ? 'block' : 'none';
  if (which === 'tabPetals') renderPetalProps();
  if (which === 'tabUi') { uiRenderList(); uiRenderProps(); renderGuideProps(); }
  drawGuides();
}
document.getElementById('tabUi').onclick = () => showPane('tabUi');
document.getElementById('tabLayers').onclick = () => showPane('tabLayers');
document.getElementById('tabPetals').onclick = () => showPane('tabPetals');
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
  buildPetals();
  renderList(); renderProps(); frame(true);
  uiBuild();
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
function tick(now) { const dt = Math.min(0.05, (now - last) / 1000); last = now; if (anim) { t += dt * speed; frame(); framePetals(dt * speed); } requestAnimationFrame(tick); }
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
  for (const [k, label, mn, mx, st] of FIELDS)
    numRow(p, label, l[k], mn, mx, st, v => { l[k] = v; frame(); });
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
const payload = () => ({ version: 1, layers: layers.map(l => ({ ...l })), petals: { ...petals }, ui: Object.fromEntries(Object.entries(ui).map(([k, e]) => [k, { ...e, x: Math.round(e.x), y: Math.round(e.y), w: Math.round(e.w), h: Math.round(e.h) }])), guide: { ...guide } });
const msg = (s, ok = true) => { const m = document.getElementById('msg'); m.textContent = s; m.style.color = ok ? '#3ddc84' : '#ff4d6d'; };
document.getElementById('copy').onclick = async () => { try { await navigator.clipboard.writeText(JSON.stringify(payload(), null, 2)); msg('JSON をコピーした'); } catch (e) { msg('コピーできない', false); } };
document.getElementById('save').onclick = async () => {
  try { const r = await fetch('/save_title', { method:'POST', headers:{ 'Content-Type':'application/json' }, body: JSON.stringify(payload()) }); const j = await r.json(); msg(j.ok ? `保存した（${j.count} 層）。Unity で Play し直すと反映` : '保存できない: ' + j.error, j.ok); }
  catch (e) { msg('サーバが無い。python tools/ui_server.py を起動するか、JSON をコピーして title_layers.json に貼る', false); }
};
fetch('/title_layout').then(r => r.json()).then(j => { if (j.petals) petals = Object.assign({}, PETAL_DEF, j.petals); if (j.ui) loadUi(j.ui); if (j.guide) Object.assign(guide, j.guide); if (j.layers && j.layers.length) { layers = j.layers; sel = 0; } rebuild(); }).catch(() => {});
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
    f.write(HTML.replace('__IMG__', json.dumps(imgs)).replace('__ASPECT__', json.dumps(aspects)).replace('__SAVED__', json.dumps(saved, ensure_ascii=False)).replace('__PETALS__', json.dumps(PETALS))
            .replace('__UIIMG__', json.dumps(UI_IMG)).replace('__UIASPECT__', json.dumps(UI_ASPECT)).replace('__SLICE__', json.dumps(FRAME_SLICE)).replace('__WIDTH__', json.dumps(FRAME_WIDTH)).replace('__ICONS__', json.dumps(ICON_NAMES)))
print(OUT, os.path.getsize(OUT) // 1024, 'KB')
