# ゲーム画面の配置エディタを作る。
#   python tools/gen_ui_viewer.py      →  tools/ui_viewer.html
#   python tools/ui_server.py          →  http://localhost:8765/ui_viewer.html（「保存」が効く）
#
# 枠をドラッグで動かし、角で大きさを変え、「保存」で Resources/Data/ui_layout.json に書く。
# Unity は次の Play でそのファイルを読む（UiLayout.cs）。id は GameController の UiLayout.Get の id と同じ。
# 位置と大きさの既定値は GameController の定数（docs/ui_frames.md の表）そのまま。
# 枠は Resources/Art/UI/Frames の画像を CSS の border-image で 9 分割し、縁の幅は UiSkin.FrameBorders と同じ。
import base64
import json
import os

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..'))
UI = os.path.join(ROOT, 'UnityProject', 'BigBonusBlitz', 'Assets', 'Resources', 'Art', 'UI')
LAYOUT = os.path.join(ROOT, 'UnityProject', 'BigBonusBlitz', 'Assets', 'Resources', 'Data', 'ui_layout.json')
OUT = os.path.join(HERE, 'ui_viewer.html')
MANIFEST = os.path.join(ROOT, 'assets', 'title', 'parts', 'frames', 'frames_manifest.json')

FRAMES = ['panel_navy', 'slot_navy', 'btn_blue', 'btn_blue_lg', 'btn_pink', 'btn_gray', 'btn_pill_blue', 'btn_pill_red', 'btn_pill_purple',
          'toast_green', 'toast_brown', 'pill_navy_sm', 'pill_coin', 'pill_gem', 'circle_navy', 'gauge_track', 'gauge_fill',
          'plate_hex_cream', 'plate_hex_sky', 'panel_cream_sm', 'bar_cream_sm']
ICONS = ['ember', 'crystal', 'swords', 'star_navy']
NAVI = ['navi_bg', 'navi_01', 'navi_question']


def uri(path):
    with open(path, 'rb') as f:
        return 'data:image/png;base64,' + base64.b64encode(f.read()).decode('ascii')


EMBED_MAX_W = 640   # 枠の埋め込みはこの幅まで縮める（v2 は 2000px 超で、そのままだと 30MB になる）


def uri_frame(path):
    """縮めて埋め込む。返り値: (data URI, 縮めた倍率)"""
    from PIL import Image
    import io as _io
    im = Image.open(path).convert('RGBA')
    k = 1.0
    if im.width > EMBED_MAX_W:
        k = EMBED_MAX_W / im.width
        im = im.resize((EMBED_MAX_W, max(1, round(im.height * k))), Image.LANCZOS)
    buf = _io.BytesIO(); im.save(buf, 'PNG', optimize=True)
    return 'data:image/png;base64,' + base64.b64encode(buf.getvalue()).decode('ascii'), k


man = json.load(open(MANIFEST, encoding='utf-8'))
imgs, embed_k = {}, {}
for n in FRAMES:
    p = os.path.join(UI, 'Frames', n + '.png')
    if os.path.exists(p):
        imgs[n], embed_k[n] = uri_frame(p)
for n in ICONS:
    imgs['icon_' + n] = uri(os.path.join(UI, 'Icons', n + '.png'))
for n in NAVI:
    imgs[n] = uri(os.path.join(UI, 'Navi', n + '.png'))
# border-image の slice は「埋め込んだ画像の px」、width は「舞台の px」。それぞれ別に出す
borders = {n: [round(v * embed_k[n]) for v in man[n]['border']] for n in FRAMES if man.get(n, {}).get('border') and n in embed_k}
widths = {n: [v / man[n].get('scale', 1.0) for v in man[n]['border']] for n in FRAMES if man.get(n, {}).get('border')}
saved = json.load(open(LAYOUT, encoding='utf-8')) if os.path.exists(LAYOUT) else {'version': 1, 'elements': {}}

HTML = r'''<!doctype html>
<meta charset="utf-8">
<title>BBB UI 配置エディタ</title>
<style>
  body { margin:0; background:#101218; color:#e8ecf4; font:13px/1.4 system-ui, "Yu Gothic UI", sans-serif; }
  .wrap { display:flex; flex-direction:column; align-items:center; gap:10px; padding:12px; }
  .row { display:flex; gap:8px; flex-wrap:wrap; align-items:center; justify-content:center; }
  button, label, select, input[type=number] { background:#1b2030; color:#fff; border:1px solid #3a4562; border-radius:8px; padding:5px 10px; font:inherit; }
  button { cursor:pointer; } button.on, button.pri { background:#4da3ff; color:#0a0b0f; } button.save { background:#3ddc84; color:#0a0b0f; }
  input[type=number] { width:64px; padding:3px 6px; }
  .phone { position:relative; width:1170px; height:540px; background:#000; overflow:hidden; border-radius:18px; user-select:none; }
  .stage { position:absolute; left:105px; top:0; width:960px; height:540px; background:#0a0b0f; }
  .safe { position:absolute; inset:0; border:1px dashed rgba(255,80,80,.5); pointer-events:none; }
  .el { position:absolute; box-sizing:border-box; }
  .frame { border-style:solid; border-color:transparent; }
  .proc { background:#17181e; border:3px solid #5f3d1e; border-radius:4px; box-shadow:0 5px 12px rgba(0,0,0,.6); }
  .proc.inset { background:#0b0c10; border:none; border-radius:8px; }
  .proc.btn { background:#27304a; border-radius:10px; border:none; }
  .proc.tag { background:#0b1120; border:1px solid rgba(255,255,255,.14); border-radius:15px; }
  .txt { position:absolute; white-space:nowrap; font-weight:700; text-shadow:1px 1px 0 rgba(0,0,0,.8); pointer-events:none; }
  .area { position:absolute; border-radius:12px; overflow:hidden; background:linear-gradient(#3b6fb5, #7fb0e6 60%, #4c7a3c 61%, #3a5d2c); pointer-events:none; }
  .grid { position:absolute; inset:0; pointer-events:none; background-image:linear-gradient(rgba(255,255,255,.06) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,.06) 1px, transparent 1px); background-size:20px 20px; display:none; }
  .navi img { position:absolute; left:50%; top:50%; width:140%; height:140%; margin:-70% 0 0 -70%; pointer-events:none; }
  .deco { position:absolute; pointer-events:none; }
  .edit { cursor:move; }
  .edit.sel { outline:2px solid #ffcf3f; outline-offset:2px; z-index:50; }
  .edit .hd { position:absolute; right:-6px; bottom:-6px; width:12px; height:12px; background:#ffcf3f; border:1px solid #000; cursor:nwse-resize; display:none; z-index:51; }
  .edit.sel .hd { display:block; }
  .lbl { position:absolute; left:2px; top:-16px; font-size:10px; color:#ffcf3f; background:rgba(0,0,0,.7); padding:0 4px; border-radius:3px; display:none; pointer-events:none; }
  .edit.sel .lbl, .showLabels .edit .lbl { display:block; }
  .panel { display:flex; gap:8px; flex-wrap:wrap; align-items:center; background:#151a2a; padding:8px 12px; border-radius:10px; }
  .warn { color:#ff9a3c; font-size:12px; white-space:pre-line; }
  .note { color:#98a3b8; font-size:12px; max-width:1140px; }
  #msg { color:#3ddc84; font-size:12px; min-width:14em; }
</style>
<div class="wrap">
  <div class="row">
    <button id="save" class="save">保存（Unity に反映）</button>
    <button id="copy">JSON をコピー</button>
    <button id="resetAll">全部 既定に戻す</button>
    <span id="msg"></span>
    <label><input type="checkbox" id="labels"> 名前を出す</label>
    <label><input type="checkbox" id="grid"> 20px グリッド</label>
    <label>吸着 <select id="snap"><option value="1">1px</option><option value="2" selected>2px</option><option value="4">4px</option><option value="8">8px</option></select></label>
    <label><input type="checkbox" id="phoneChk" checked> iPhone の余白</label>
    <label>倍率 <input type="range" id="zoom" min="0.6" max="2.2" step="0.1" value="1"><span id="zoomV">1.0</span></label>
    <button id="real">iPhone 実寸</button>
  </div>
  <div class="panel" id="props">
    <b id="pName">（枠を選ぶ）</b>
    <label>x <input type="number" id="px" step="1"></label>
    <label>y <input type="number" id="py" step="1"></label>
    <label>w <input type="number" id="pw" step="1"></label>
    <label>h <input type="number" id="ph" step="1"></label>
    <label>枠 <select id="pframe"></select></label>
    <button id="resetOne">これだけ既定に戻す</button>
    <span class="note">矢印キーで 1px（Shift で 10px）。角の黄色い四角で大きさ</span>
  </div>
  <div class="phone" id="phone"><div class="stage" id="stage"><div class="grid" id="gridEl"></div><div class="safe"></div></div></div>
  <div class="warn" id="warn"></div>
  <div class="note">座標は Unity と同じ（中心が原点、上が +y）。子の枠（EMBER の窓など）は親を動かすと一緒に動く。
  「保存」は tools/ui_server.py を起動しているときだけ効く（無ければ JSON をコピーして Assets/Resources/Data/ui_layout.json に貼る）。
  リール窓の大きさは図柄に合わせた固定値なので、ここでは動かせない。表示域（背景）はステージ札に付いて動く。</div>
</div>
<script>
const IMG = __IMG__;
const BORDERS = __BORDERS__;     // [left, bottom, right, top]（埋め込んだ画像の px。slice 用）
const WIDTHS = __WIDTHS__;       // [left, bottom, right, top]（舞台の px。width 用）
const SAVED = __SAVED__;         // Resources/Data/ui_layout.json の中身（生成時点）
const SW = 960, SH = 540, Margin = 8, ContentW = 944, StageCardH = 252, MidH = 196, CtrlH = 56, SideW = 242, ReelW = 132, SymH = 60;
const AreaW = 940, AreaH = 220, AreaY = -15;
const StageCardY = SH/2 - Margin - StageCardH/2, MidY = StageCardY - StageCardH/2 - Margin - MidH/2, CtrlY = MidY - MidH/2 - Margin - CtrlH/2;
const pitch = ReelW + 8, cabW = pitch * 3 + 24, innerW = SideW - 24, reelH = SymH * 3;

// 枠の候補（種類ごと）。'' は「自動（UiSkin が選ぶ）」、none は前の手続き描画
const FRAME_CHOICES = {
  panel: ['', 'none', 'panel_navy', 'panel_cream_sm'],
  btn:   ['', 'none', 'btn_blue', 'btn_blue_lg', 'btn_pink', 'btn_gray', 'btn_pill_blue', 'btn_pill_red', 'btn_pill_purple', 'toast_green', 'toast_brown', 'pill_navy_sm', 'plate_hex_cream', 'plate_hex_sky'],
  inset: ['', 'none', 'slot_navy', 'pill_coin', 'pill_gem', 'pill_navy_sm', 'bar_cream_sm'],
  tag:   ['', 'none', 'pill_navy_sm', 'bar_cream_sm'],
  navi:  [''],
};
const AUTO_FRAME = { panel: 'panel_navy', btn: 'btn_blue', inset: 'slot_navy', tag: 'pill_navy_sm' };

// ---- 要素。既定値は GameController と同じ式。parent があれば親を動かすと一緒に動く ----
const DEFS = [
  { id:'stageCard', name:'ステージ札', group:'panel', x:0, y:StageCardY, w:ContentW, h:StageCardH },
  { id:'stageTag',  name:'ステージ名の札', group:'tag', parent:'stageCard', x:-AreaW/2+10+165, y:StageCardY+AreaY+AreaH/2-6-15, w:330, h:30, proc:'tag' },
  { id:'lifeTag',   name:'ライフ札', group:'tag', parent:'stageCard', x:AreaW/2-10-116, y:StageCardY+AreaY+AreaH/2-6-17, w:232, h:34, proc:'tag' },
  { id:'routeTag',  name:'次のルート', group:'tag', parent:'stageCard', x:-AreaW/2+6+67, y:StageCardY+AreaY+AreaH/2-44-59, w:134, h:118, proc:'tag', noFrame:true },
  { id:'navi',      name:'ナビ（3 つ）', group:'navi', x:0, y:StageCardY-StageCardH/2-2, w:cabW, h:66, noFrame:true },
  { id:'disp',      name:'表示器（左）', group:'panel', x:-ContentW/2+SideW/2, y:MidY, w:SideW, h:MidH },
  { id:'credit',    name:'EMBER の窓', group:'inset', parent:'disp', x:-ContentW/2+SideW/2, y:MidY+49, w:innerW, h:40, proc:'inset', auto:'slot_navy' },
  { id:'payout',    name:'PAYOUT の窓', group:'inset', parent:'disp', x:-ContentW/2+SideW/2, y:MidY-20, w:innerW, h:40, proc:'inset', auto:'pill_coin' },
  { id:'cabinet',   name:'リール筐体', group:'panel', x:0, y:MidY, w:cabW, h:MidH },
  { id:'side',      name:'操作パネル（右）', group:'panel', x:ContentW/2-SideW/2, y:MidY, w:SideW, h:MidH },
  { id:'mini',      name:'常駐スランプ', group:'inset', parent:'side', x:ContentW/2-SideW/2, y:MidY-83, w:innerW, h:22, proc:'inset', noFrame:true },
  { id:'bet',       name:'MAX BET', group:'btn', x:-ContentW/2+SideW/2, y:CtrlY, w:SideW, h:CtrlH, proc:'btn', auto:'btn_pink' },
  { id:'auto',      name:'AUTO', group:'btn', x:ContentW/2-SideW/2, y:CtrlY, w:SideW, h:CtrlH, proc:'btn' },
];
const BASE = Object.fromEntries(DEFS.map(d => [d.id, { x:d.x, y:d.y, w:d.w, h:d.h, frame:'' }]));
let cur = JSON.parse(JSON.stringify(BASE));
function loadInto(els) { for (const [id, r] of Object.entries(els || {})) if (cur[id]) Object.assign(cur[id], { x:r.x, y:r.y, w:r.w, h:r.h, frame:r.frame || '' }); }
loadInto(SAVED.elements);

const stage = document.getElementById('stage');
const nodes = {};
let sel = null, dragging = null, zoom = 1;
const snapV = () => +document.getElementById('snap').value;
const R = v => Math.round(v);

function box(cx, cy, w, h) { return { left: 480 + cx - w/2, top: 270 - cy - h/2, width: w, height: h }; }
function text(parentEl, s, dx, dy, size, color, anchor, w) {
  // 親の枠に対する相対位置（親の中心が原点・上が +y）
  const d = document.createElement('div'); d.className = 'txt'; d.textContent = s;
  d.style.fontSize = size + 'px'; d.style.color = color; d.style.lineHeight = size + 'px';
  const pw = parseFloat(parentEl.style.width), ph = parseFloat(parentEl.style.height);
  const x = pw/2 + dx, y = ph/2 - dy;
  if (anchor === 'l') { d.style.left = x + 'px'; d.style.top = (y - size/2) + 'px'; }
  else if (anchor === 'r') { d.style.right = (pw - x) + 'px'; d.style.top = (y - size/2) + 'px'; }
  else { const ww = w || pw; d.style.left = (x - ww/2) + 'px'; d.style.width = ww + 'px'; d.style.textAlign = 'center'; d.style.top = (y - size/2) + 'px'; }
  parentEl.appendChild(d); return d;
}
function icon(parentEl, name, dx, dy, s) {
  const d = document.createElement('div'); d.className = 'deco';
  const pw = parseFloat(parentEl.style.width), ph = parseFloat(parentEl.style.height);
  Object.assign(d.style, { left:(pw/2+dx-s/2)+'px', top:(ph/2-dy-s/2)+'px', width:s+'px', height:s+'px', background:`url(${IMG['icon_'+name]}) center / contain no-repeat` });
  parentEl.appendChild(d); return d;
}
function deco(parentEl, dx, dy, w, h, style) {
  const d = document.createElement('div'); d.className = 'deco';
  const pw = parseFloat(parentEl.style.width), ph = parseFloat(parentEl.style.height);
  Object.assign(d.style, { left:(pw/2+dx-w/2)+'px', top:(ph/2-dy-h/2)+'px', width:w+'px', height:h+'px' }, style);
  parentEl.appendChild(d); return d;
}

// 枠の絵は要素そのものではなく、中に敷いた 1 枚（.skinLayer）に描く。
// 要素に border を持たせると、中の文字やアイコンの座標が縁のぶんずれる（absolute は padding の内側が基準）
function skin(d, def, r) {
  const name = r.frame === '' ? (def.noFrame ? 'none' : (def.auto || AUTO_FRAME[def.group])) : r.frame;
  let sl = d.querySelector(':scope > .skinLayer');
  if (!sl) { sl = document.createElement('div'); sl.className = 'skinLayer'; Object.assign(sl.style, { position:'absolute', inset:'0', pointerEvents:'none', boxSizing:'border-box' }); d.insertBefore(sl, d.firstChild); }
  sl.removeAttribute('style'); Object.assign(sl.style, { position:'absolute', inset:'0', pointerEvents:'none', boxSizing:'border-box' });
  sl.className = 'skinLayer';
  if (name !== 'none' && IMG[name]) {
    const br = BORDERS[name];
    if (br) { const [l, bt, rr, t] = br, [wl, wb, wr, wt] = WIDTHS[name]; sl.style.borderStyle = 'solid'; sl.style.borderColor = 'transparent'; sl.style.borderImage = `url(${IMG[name]}) ${t} ${rr} ${bt} ${l} fill / ${wt}px ${wr}px ${wb}px ${wl}px`; sl.style.borderWidth = `${wt}px ${wr}px ${wb}px ${wl}px`; }
    else sl.style.background = `url(${IMG[name]}) center / contain no-repeat`;
    sl.style.boxShadow = '0 5px 12px rgba(0,0,0,.55)';
  } else { sl.classList.add('proc'); if (def.proc) sl.classList.add(def.proc); }
}

// 中身（文字・アイコン）。枠の大きさが変わっても親の中心基準で置き直す
function decorate(id, d) {
  d.querySelectorAll(':scope > .txt, :scope > .deco, :scope > .area, :scope > .navi').forEach(n => n.remove());
  const r = cur[id], W = r.w, H = r.h;
  if (id === 'stageCard') {
    const a = document.createElement('div'); a.className = 'area';
    Object.assign(a.style, { left:(W/2-AreaW/2)+'px', top:(H/2-AreaY-AreaH/2)+'px', width:AreaW+'px', height:AreaH+'px' });
    d.appendChild(a);
    deco(d, -W/2+8+46, H/2-14-1, 88, 20, { background:'#27304a', borderRadius:'10px', color:'#f4f6fa', fontSize:'12px', lineHeight:'20px', textAlign:'center', fontWeight:'700' }).textContent = '通常';
    text(d, '0 G', W/2-8-44, H/2-14-1, 15, '#ffcf3f', 'c', 88);
  }
  if (id === 'stageTag') text(d, 'ステージ  C-3  《燠の回廊》', -W/2+16, 0, 14, '#f4f6fa', 'l');
  if (id === 'lifeTag') { text(d, 'ライフ 148 / 360', -W/2+16, 5, 13, '#f4f6fa', 'l'); deco(d, 2, -9, W-30, 5, { background:'rgba(0,0,0,.5)', borderRadius:'3px' }); deco(d, 2-(W-30)/2+(W-30)*0.2, -9, (W-30)*0.4, 5, { background:'#7ee0a0', borderRadius:'3px' }); }
  if (id === 'routeTag') { text(d, '次のルート', 0, H/2-13, 11, '#98a3b8', 'c'); text(d, 'リプレイ 3/15', -W/2+30, H/2-38, 11, '#f4f6fa', 'l'); }
  if (id === 'navi') for (let i = 0; i < 3; i++) { const n = document.createElement('div'); n.className = 'navi'; Object.assign(n.style, { position:'absolute', left:(W/2+(i-1)*pitch-H/2)+'px', top:'0', width:H+'px', height:H+'px' }); n.innerHTML = `<img src="${IMG.navi_bg}"><img src="${IMG[i===0?'navi_01':'navi_question']}" style="transform:scale(${i===0?1:0.83})">`; d.appendChild(n); }
  if (id === 'disp') { const iw = W - 24; icon(d, 'ember', -iw/2+7.5, 83, 15); text(d, 'EMBER', -iw/2+20, 83, 11, '#98a3b8', 'l'); text(d, 'PAYOUT', -iw/2+20, 14, 11, '#98a3b8', 'l'); text(d, '設定 1 / モード B', -iw/2, -76, 11, '#98a3b8', 'l'); icon(d, 'crystal', iw/2-7, -76, 14); text(d, '1,240', iw/2-16, -76, 11, '#a98bff', 'r'); }
  if (id === 'credit') text(d, '12,480', W/2-16, 0, 28, '#f4f6fa', 'r');
  if (id === 'payout') text(d, '15', W/2-16, 0, 28, '#ffcf3f', 'r');
  if (id === 'cabinet') for (let i = 0; i < 3; i++) {
    const w = deco(d, (i-1)*pitch, 0, 140, 188, { background:'#05070c', borderRadius:'8px', border:'2px solid rgba(255,207,63,.35)' });
    for (let k = 0; k < 3; k++) { const s = document.createElement('div'); Object.assign(s.style, { position:'absolute', left:'18px', top:(10+k*60)+'px', width:'100px', height:'44px', borderRadius:'8px', background:['#c0392b','#2980b9','#f1c40f'][(i+k)%3], opacity:.85 }); w.appendChild(s); }
  }
  if (id === 'side') { const iw = W - 24; icon(d, 'swords', -iw/2+7.5, 83, 15); text(d, 'PLAYER', -iw/2+20, 83, 11, '#98a3b8', 'l'); text(d, 'Lv 12', -iw/2, 60, 16, '#ffcf3f', 'l'); text(d, 'EXP', iw/2, 60, 10, '#5f6a80', 'r');
    deco(d, 0, 44, iw, 8, { background:'rgba(0,0,0,.5)', borderRadius:'4px' }); deco(d, -iw/2+iw*0.28, 44, iw*0.56, 6, { background:'#3ddc84', borderRadius:'3px' });
    icon(d, 'star_navy', -iw/2+7.5, 24, 15); text(d, 'BONUS', -iw/2+20, 24, 11, '#98a3b8', 'l'); deco(d, 0, -12, iw, 8, { background:'rgba(0,0,0,.5)', borderRadius:'4px' }); text(d, '高確ステージ', -iw/2, -28, 12, '#f4f6fa', 'l');
    for (const [dx, s] of [[-iw/2+30, '音量'], [0, 'グラフ'], [iw/2-30, 'DEBUG']]) deco(d, dx, -58, 56, 24, { background:'#27304a', borderRadius:'8px', color:'#fff', fontSize:'11px', textAlign:'center', lineHeight:'24px', fontWeight:'700' }).textContent = s;
  }
  if (id === 'mini') text(d, '+120', W/2-2, 0, 11, '#98a3b8', 'r');
  if (id === 'bet') { text(d, 'MAX BET', 0, 4, 20, '#fff', 'c'); text(d, 'Ctrl / Space', 0, -16, 10, 'rgba(255,255,255,.7)', 'c'); }
  if (id === 'auto') { text(d, 'AUTO', 0, 4, 18, '#fff', 'c'); text(d, 'A / Space長押し', 0, -16, 10, 'rgba(255,255,255,.7)', 'c'); }
}

function build() {
  for (const def of DEFS) {
    const r = cur[def.id];
    const parentNode = def.parent ? nodes[def.parent] : stage;
    const d = document.createElement('div'); d.className = 'el edit'; d.dataset.id = def.id;
    d.style.width = r.w + 'px'; d.style.height = r.h + 'px';
    parentNode.appendChild(d);
    const lbl = document.createElement('div'); lbl.className = 'lbl'; lbl.textContent = def.name; d.appendChild(lbl);
    const hd = document.createElement('div'); hd.className = 'hd'; d.appendChild(hd);
    nodes[def.id] = d;
    d.addEventListener('mousedown', ev => { if (ev.target === hd) return; ev.stopPropagation(); select(def.id); const rr = cur[def.id]; dragging = { id:def.id, sx:ev.clientX, sy:ev.clientY, ox:rr.x, oy:rr.y, kind:'move' }; });
    hd.addEventListener('mousedown', ev => { ev.stopPropagation(); select(def.id); const rr = cur[def.id]; dragging = { id:def.id, sx:ev.clientX, sy:ev.clientY, ow:rr.w, oh:rr.h, ox:rr.x, oy:rr.y, kind:'size' }; });
  }
  refresh();
}
function place(id) {
  const def = DEFS.find(v => v.id === id), r = cur[id], d = nodes[id];
  if (def.parent) { const pr = cur[def.parent]; d.style.left = (pr.w/2 + (r.x - pr.x) - r.w/2) + 'px'; d.style.top = (pr.h/2 - (r.y - pr.y) - r.h/2) + 'px'; }
  else { const b = box(r.x, r.y, r.w, r.h); d.style.left = b.left + 'px'; d.style.top = b.top + 'px'; }
  d.style.width = r.w + 'px'; d.style.height = r.h + 'px';
  skin(d, def, r); decorate(id, d);
  // 子は上に
  d.querySelectorAll(':scope > .edit').forEach(c => d.appendChild(c));
}
function refresh() { for (const def of DEFS) place(def.id); check(); syncProps(); }
function moveTree(id, dx, dy) { cur[id].x += dx; cur[id].y += dy; for (const def of DEFS) if (def.parent === id) moveTree(def.id, dx, dy); }

document.addEventListener('mousemove', ev => {
  if (!dragging) return;
  const s = snapV(), dx = (ev.clientX - dragging.sx) / zoom, dy = -(ev.clientY - dragging.sy) / zoom;
  const r = cur[dragging.id];
  if (dragging.kind === 'move') {
    // 吸着は「動かした量」に掛ける。絶対座標に掛けると、触っただけで 1px ずれる
    const nx = dragging.ox + Math.round(dx / s) * s, ny = dragging.oy + Math.round(dy / s) * s;
    moveTree(dragging.id, nx - r.x, ny - r.y);
  } else {
    // 右下の角を引く: 左上を固定して幅と高さを変える
    const nw = Math.max(20, dragging.ow + Math.round(dx / s) * s), nh = Math.max(12, dragging.oh - Math.round(dy / s) * s);
    r.x = dragging.ox + (nw - dragging.ow) / 2; r.y = dragging.oy - (nh - dragging.oh) / 2; r.w = nw; r.h = nh;
  }
  refresh();
});
document.addEventListener('mouseup', () => dragging = null);
document.addEventListener('keydown', ev => {
  if (!sel || ['INPUT','SELECT','TEXTAREA'].includes(document.activeElement.tagName)) return;
  const st = ev.shiftKey ? 10 : 1; let dx = 0, dy = 0;
  if (ev.key === 'ArrowLeft') dx = -st; else if (ev.key === 'ArrowRight') dx = st; else if (ev.key === 'ArrowUp') dy = st; else if (ev.key === 'ArrowDown') dy = -st; else return;
  ev.preventDefault(); moveTree(sel, dx, dy); refresh();
});
function select(id) { sel = id; for (const k in nodes) nodes[k].classList.toggle('sel', k === id); syncProps(); }
stage.addEventListener('mousedown', () => select(null));

// ---- プロパティ ----
const pframe = document.getElementById('pframe');
function syncProps() {
  const def = DEFS.find(v => v.id === sel);
  document.getElementById('pName').textContent = def ? `${def.name}（${def.id}）` : '（枠を選ぶ）';
  for (const k of ['x','y','w','h']) { const el = document.getElementById('p'+k); if (document.activeElement !== el) el.value = def ? R(cur[sel][k]) : ''; }
  if (!pframe.dataset.for || pframe.dataset.for !== (sel || '')) {
    pframe.innerHTML = ''; pframe.dataset.for = sel || '';
    if (def) for (const f of FRAME_CHOICES[def.group]) { const o = document.createElement('option'); o.value = f; o.textContent = f === '' ? `自動（${def.noFrame ? 'なし' : (def.auto || AUTO_FRAME[def.group])}）` : f === 'none' ? '前の見た目（手続き）' : f; pframe.appendChild(o); }
  }
  if (def) pframe.value = cur[sel].frame;
  pframe.disabled = !def || !!def.noFrame;
}
for (const k of ['x','y','w','h']) document.getElementById('p'+k).addEventListener('input', ev => { if (!sel) return; const v = +ev.target.value || 0; if (k === 'x' || k === 'y') { const d = v - cur[sel][k]; moveTree(sel, k === 'x' ? d : 0, k === 'y' ? d : 0); } else cur[sel][k] = v; refresh(); });
pframe.addEventListener('change', () => { if (sel) { cur[sel].frame = pframe.value; refresh(); } });
document.getElementById('resetOne').onclick = () => { if (!sel) return; const b = BASE[sel]; moveTree(sel, b.x - cur[sel].x, b.y - cur[sel].y); cur[sel].w = b.w; cur[sel].h = b.h; cur[sel].frame = ''; refresh(); };
document.getElementById('resetAll').onclick = () => { cur = JSON.parse(JSON.stringify(BASE)); refresh(); };

// ---- 検算（docs/ui_rules.md 1 番: 隣り合うものの重なり・はみ出し）----
function check() {
  const out = [];
  const top = DEFS.filter(d => !d.parent && d.id !== 'navi').map(d => ({ d, r: cur[d.id] }));   // ナビはリールの上に乗せるもの
  for (let i = 0; i < top.length; i++) for (let j = i + 1; j < top.length; j++) {
    const a = top[i].r, b = top[j].r;
    const ox = Math.min(a.x + a.w/2, b.x + b.w/2) - Math.max(a.x - a.w/2, b.x - b.w/2);
    const oy = Math.min(a.y + a.h/2, b.y + b.h/2) - Math.max(a.y - a.h/2, b.y - b.h/2);
    if (ox > 0.5 && oy > 0.5) out.push(`${top[i].d.name} と ${top[j].d.name} が ${R(ox)}x${R(oy)}px 重なっている`);
  }
  for (const { d, r } of top)
    if (r.x - r.w/2 < -480 - 0.5 || r.x + r.w/2 > 480 + 0.5 || r.y - r.h/2 < -270 - 0.5 || r.y + r.h/2 > 270 + 0.5) out.push(`${d.name} が舞台からはみ出している`);
  for (const def of DEFS) if (def.parent) {
    const r = cur[def.id], p = cur[def.parent];
    if (r.x - r.w/2 < p.x - p.w/2 - 0.5 || r.x + r.w/2 > p.x + p.w/2 + 0.5 || r.y - r.h/2 < p.y - p.h/2 - 0.5 || r.y + r.h/2 > p.y + p.h/2 + 0.5) out.push(`${def.name} が ${DEFS.find(v => v.id === def.parent).name} からはみ出している`);
  }
  document.getElementById('warn').textContent = out.join('\n');
}

// ---- 書き出し・保存 ----
function payload() {
  const elements = {};
  for (const def of DEFS) { const r = cur[def.id]; elements[def.id] = { x: R(r.x), y: R(r.y), w: R(r.w), h: R(r.h), frame: r.frame }; }
  return { version: 1, elements };
}
const msg = (s, ok = true) => { const m = document.getElementById('msg'); m.textContent = s; m.style.color = ok ? '#3ddc84' : '#ff4d6d'; };
document.getElementById('copy').onclick = async () => { try { await navigator.clipboard.writeText(JSON.stringify(payload(), null, 2)); msg('JSON をコピーした'); } catch (e) { msg('コピーできない: ' + e, false); } };
document.getElementById('save').onclick = async () => {
  try {
    const res = await fetch('/save', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload()) });
    const j = await res.json();
    msg(j.ok ? `保存した（${j.count} 件）。Unity で Play し直すと反映` : '保存できない: ' + j.error, j.ok);
  } catch (e) { msg('サーバが無い。python tools/ui_server.py を起動するか、JSON をコピーして貼る', false); }
};
// サーバがあれば最新の保存を読む
fetch('/layout').then(r => r.json()).then(j => { loadInto(j.elements); refresh(); }).catch(() => {});

// ---- 表示 ----
document.getElementById('labels').onchange = e => stage.classList.toggle('showLabels', e.target.checked);
document.getElementById('grid').onchange = e => document.getElementById('gridEl').style.display = e.target.checked ? 'block' : 'none';
document.getElementById('phoneChk').onchange = e => { const p = document.getElementById('phone'); p.style.width = e.target.checked ? '1170px' : '960px'; stage.style.left = e.target.checked ? '105px' : '0'; };
const zoomEl = document.getElementById('zoom');
zoomEl.oninput = () => { zoom = parseFloat(zoomEl.value); document.getElementById('zoomV').textContent = zoom.toFixed(2); const p = document.getElementById('phone'); p.style.transform = `scale(${zoom})`; p.style.transformOrigin = '50% 0'; p.style.marginBottom = (540 * (zoom - 1)) + 'px'; };
document.getElementById('real').onclick = () => { zoomEl.value = 2.183; zoomEl.dispatchEvent(new Event('input')); };
// 画面が狭ければ全体が入る倍率から始める
zoomEl.value = Math.max(0.6, Math.min(1, Math.floor((innerWidth - 40) / 1170 * 10) / 10)); zoomEl.dispatchEvent(new Event('input'));
build();
</script>
'''

with open(OUT, 'w', encoding='utf-8') as f:
    f.write(HTML.replace('__IMG__', json.dumps(imgs)).replace('__BORDERS__', json.dumps(borders)).replace('__WIDTHS__', json.dumps(widths)).replace('__SAVED__', json.dumps(saved, ensure_ascii=False)))
print(OUT, os.path.getsize(OUT) // 1024, 'KB')
