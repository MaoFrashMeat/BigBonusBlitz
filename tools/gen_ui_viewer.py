# ゲーム画面の配置を Unity を開かずに確かめるビューアを作る。
#   python tools/gen_ui_viewer.py   →  tools/ui_viewer.html（画像を埋め込んだ 1 枚）
#
# 位置と大きさは GameController の定数（docs/ui_frames.md の表）をそのまま写している。
# 枠は Resources/Art/UI/Frames の画像を CSS の border-image で 9 分割し、縁の幅は UiSkin.FrameBorders と同じ。
# 「枠の絵 / 手続き描画」を切り替えて比べられる。
import base64
import json
import os

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..'))
UI = os.path.join(ROOT, 'UnityProject', 'BigBonusBlitz', 'Assets', 'Resources', 'Art', 'UI')
OUT = os.path.join(HERE, 'ui_viewer.html')
MANIFEST = os.path.join(ROOT, 'assets', 'title', 'parts', 'frames', 'frames_manifest.json')

FRAMES = ['panel_navy', 'slot_navy', 'btn_blue', 'btn_pink', 'btn_gray', 'toast_green', 'pill_navy_sm', 'pill_coin',
          'circle_navy', 'gauge_track', 'gauge_fill']
ICONS = ['ember', 'crystal', 'swords', 'star_navy']
NAVI = ['navi_bg', 'navi_01', 'navi_question']


def uri(path):
    with open(path, 'rb') as f:
        return 'data:image/png;base64,' + base64.b64encode(f.read()).decode('ascii')


man = json.load(open(MANIFEST, encoding='utf-8'))
imgs = {}
for n in FRAMES:
    imgs[n] = uri(os.path.join(UI, 'Frames', n + '.png'))
for n in ICONS:
    imgs['icon_' + n] = uri(os.path.join(UI, 'Icons', n + '.png'))
for n in NAVI:
    imgs[n] = uri(os.path.join(UI, 'Navi', n + '.png'))
borders = {n: man[n]['border'] for n in FRAMES if man.get(n, {}).get('border')}   # [left, bottom, right, top]

HTML = r'''<!doctype html>
<meta charset="utf-8">
<title>BBB UI 配置ビューア</title>
<style>
  body { margin:0; background:#101218; color:#e8ecf4; font:13px/1.4 system-ui, "Yu Gothic UI", sans-serif; }
  .wrap { display:flex; flex-direction:column; align-items:center; gap:12px; padding:14px; }
  .row { display:flex; gap:8px; flex-wrap:wrap; align-items:center; justify-content:center; }
  button, label { background:#1b2030; color:#fff; border:1px solid #3a4562; border-radius:8px; padding:5px 10px; cursor:pointer; }
  button.on { background:#4da3ff; color:#0a0b0f; }
  .phone { position:relative; width:1170px; height:540px; background:#000; overflow:hidden; border-radius:18px; }
  .stage { position:absolute; left:105px; top:0; width:960px; height:540px; background:#0a0b0f; }
  .safe { position:absolute; inset:0; border:1px dashed rgba(255,80,80,.5); pointer-events:none; }
  .el { position:absolute; box-sizing:border-box; }
  .frame { border-style:solid; border-color:transparent; }
  .proc { background:#17181e; border:3px solid #5f3d1e; border-radius:4px; box-shadow:0 5px 12px rgba(0,0,0,.6); }
  .proc.inset { background:#0b0c10; border:none; border-radius:8px; }
  .proc.btn { border-radius:10px; border:none; }
  .txt { position:absolute; white-space:nowrap; font-weight:700; text-shadow:1px 1px 0 rgba(0,0,0,.8); }
  .sub { color:#98a3b8; font-weight:700; font-size:11px; }
  .area { position:absolute; border-radius:12px; overflow:hidden; background:linear-gradient(#3b6fb5, #7fb0e6 60%, #4c7a3c 61%, #3a5d2c); }
  .band { position:absolute; left:0; top:0; height:28px; width:100%; }
  .chip { position:absolute; top:4px; height:20px; border-radius:10px; font-size:12px; font-weight:700; line-height:20px; text-align:center; }
  .grid { position:absolute; inset:0; pointer-events:none; background-image:linear-gradient(rgba(255,255,255,.06) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,.06) 1px, transparent 1px); background-size:20px 20px; display:none; }
  .navi { position:absolute; width:66px; height:66px; }
  .navi img { position:absolute; left:50%; top:50%; width:92px; height:92px; margin:-46px 0 0 -46px; }
  .note { color:#98a3b8; font-size:12px; max-width:1100px; }
</style>
<div class="wrap">
  <div class="row">
    <button id="modeArt" class="on">全部 絵</button><button id="modeProc">全部 前の見た目</button>
    <span style="color:#98a3b8">｜ 種類ごとに:</span>
    <label>板 <select data-group="panel"><option value="art">絵</option><option value="proc">前</option></select></label>
    <label>ボタン <select data-group="btn"><option value="art">絵</option><option value="proc">前</option><option value="pill">小さい紺ピル</option></select></label>
    <label>くぼみ（数値・リール窓） <select data-group="inset"><option value="art">絵</option><option value="proc">前</option></select></label>
    <label>札（ステージ・ライフ・小ボタン） <select data-group="tag"><option value="art">絵</option><option value="proc">前</option></select></label>
    <label><input type="checkbox" id="grid"> 20px グリッド</label>
    <label><input type="checkbox" id="phone" checked> iPhone 横持ち（19.5:9）の余白を見せる</label>
    <label>倍率 <input type="range" id="zoom" min="0.6" max="2.2" step="0.1" value="1"><span id="zoomV">1.0</span></label>
    <button id="real">iPhone 実寸（2556×1179 = 2.18 倍）</button>
  </div>
  <div class="phone" id="phone"><div class="stage" id="stage"><div class="grid" id="gridEl"></div><div class="safe"></div></div></div>
  <div class="note" id="note"></div>
</div>
<script>
const IMG = __IMG__;
const BORDERS = __BORDERS__;   // [left, bottom, right, top]
const stage = document.getElementById('stage');
let mode = 'art';
const GROUP_MODE = { panel: 'art', btn: 'art', inset: 'art', tag: 'art' };
const GROUP_OF = { panel_navy: 'panel', btn_blue: 'btn', btn_pink: 'btn', btn_gray: 'btn', toast_green: 'btn',
                   slot_navy: 'inset', pill_coin: 'inset', pill_navy_sm: 'tag', gauge_track: 'inset', gauge_fill: 'inset' };

// Unity 流（中心原点・上が +y）→ CSS（左上原点）
function box(cx, cy, w, h) { return { left: 480 + cx - w/2, top: 270 - cy - h/2, width: w, height: h }; }
function el(cls, cx, cy, w, h, extra) {
  const d = document.createElement('div'); d.className = 'el ' + cls;
  const b = box(cx, cy, w, h); for (const k in b) d.style[k] = b[k] + 'px';
  if (extra) Object.assign(d.style, extra);
  stage.appendChild(d); return d;
}
function frame(name, cx, cy, w, h, procStyle, tint) {
  const d = el('', cx, cy, w, h);
  d.dataset.frame = name; d.dataset.proc = JSON.stringify(procStyle || {});
  d.dataset.tint = tint || '';
  return d;
}
function text(s, cx, cy, size, color, anchor, w) {
  const d = document.createElement('div'); d.className = 'txt'; d.textContent = s;
  d.style.fontSize = size + 'px'; d.style.color = color; d.style.lineHeight = size + 'px';
  const x = 480 + cx, y = 270 - cy;
  if (anchor === 'l') { d.style.left = x + 'px'; d.style.top = (y - size/2) + 'px'; }
  else if (anchor === 'r') { d.style.right = (960 - x) + 'px'; d.style.top = (y - size/2) + 'px'; }
  else { d.style.left = (x - (w||200)/2) + 'px'; d.style.width = (w||200) + 'px'; d.style.textAlign = 'center'; d.style.top = (y - size/2) + 'px'; }
  stage.appendChild(d); return d;
}
function applyMode() {
  for (const d of stage.querySelectorAll('[data-frame]')) {
    const name = d.dataset.frame, proc = JSON.parse(d.dataset.proc);
    d.removeAttribute('style'); const b = d._box; for (const k in b) d.style[k] = b[k] + 'px';
    d.className = 'el';
    const g = GROUP_OF[name] || 'panel';
    let gm = GROUP_MODE[g];
    let useName = name;
    if (gm === 'pill') { useName = 'pill_navy_sm'; gm = 'art'; }
    if (gm === 'art' && IMG[useName]) {
      name === useName || (d.style.filter = 'saturate(1.1)');
      const br = BORDERS[useName];
      if (br) {
        const [l, bt, r, t] = br;
        d.classList.add('frame');
        d.style.borderImage = `url(${IMG[useName]}) ${t} ${r} ${bt} ${l} fill / ${t}px ${r}px ${bt}px ${l}px`;
        d.style.borderWidth = `${t}px ${r}px ${bt}px ${l}px`;
      } else { d.style.background = `url(${IMG[useName]}) center / contain no-repeat`; }
      if (d.dataset.tint) d.style.filter = d.dataset.tint;
      d.style.boxShadow = '0 5px 12px rgba(0,0,0,.55)';
    } else {
      d.classList.add('proc'); if (proc.cls) d.classList.add(proc.cls);
      if (proc.bg) d.style.background = proc.bg;
    }
  }
}
const _el = el;
// 枠を置く（proc は前の見た目のときのスタイル）
function F(name, cx, cy, w, h, proc, tint) { const d = frame(name, cx, cy, w, h, proc, tint); d._box = box(cx, cy, w, h); return d; }

// ===== 上段: ステージカード 944x252 @ (0,136) =====
F('panel_navy', 0, 136, 944, 252, {});
// 帯（モードチップ）
// 帯は透明（モードチップだけが乗る）
const chipBox = (cx, w, s, bg, fg) => { const d = _el('chip', cx, 136 + 126 - 14, w, 20, { background: bg, color: fg, borderRadius:'10px', fontSize:'12px', lineHeight:'20px', textAlign:'center', fontWeight:'700' }); d.textContent = s; };
chipBox(-472 + 8 + 46, 88, '通常', '#27304a', '#f4f6fa');
chipBox(-472 + 8 + 88 + 8 + 60, 120, '', 'transparent', '#fff');
// 表示域（背景・キャラ）940x220 @ (0, 136-15)
const area = _el('area', 0, 136 - 15, 940, 220, {});
// ステージ札 330x30（左上）・ライフ札 232x34（右上）
const tagTop = 136 - 15 + 110 - 6 - 15;
F('pill_navy_sm', -470 + 10 + 165, tagTop, 330, 30, { bg: '#0b1120', cls: 'btn' });
text('ステージ  C-3  《燠の回廊》', -470 + 10 + 9 + 6, tagTop, 14, '#f4f6fa', 'l');
F('pill_navy_sm', 470 - 10 - 116, tagTop - 2, 232, 34, { bg: '#0b1120', cls: 'btn' });
text('ライフ 148 / 360', 470 - 10 - 232 + 9 + 6, tagTop - 2 + 5, 13, '#f4f6fa', 'l');
F('gauge_track', 470 - 10 - 116 + 2, tagTop - 2 - 9, 202, 5, { bg: 'rgba(0,0,0,.5)', cls: 'inset' });
F('gauge_fill', 470 - 10 - 116 + 2 - 101 + 42, tagTop - 2 - 9, 84, 5, { bg: '#7ee0a0', cls: 'inset' }, 'sepia(1) hue-rotate(90deg) saturate(3)');
// 次の道（左）134x118
const rtRt = _el('', -470 + 6 + 67, 136 - 15 + 110 - 44 - 59, 134, 118, { background: 'rgba(11,17,32,.92)', borderRadius: '10px', border: '1px solid rgba(255,255,255,.14)' });
text('次の道', -470 + 12, 136 - 15 + 110 - 44 - 12, 11, '#98a3b8', 'l');
text('リプレイ 3/15', -470 + 12, 136 - 15 + 110 - 44 - 36, 12, '#f4f6fa', 'l');
// ナビ（リールの真上）66px @ y = 136-126-2 = 8
for (let i = 0; i < 3; i++) {
  const n = _el('navi', (i - 1) * 140, 8, 66, 66, {});
  n.innerHTML = `<img src="${IMG.navi_bg}"><img src="${IMG[i === 0 ? 'navi_01' : 'navi_question']}" style="transform:scale(${i === 0 ? 1 : 0.83})">`;
  if (i) n.style.filter = 'brightness(.95)';
  n.style.zIndex = 5;
}

// ===== 中段: 表示器 / リール筐体 / 操作パネル（h 196 @ y -100）=====
F('panel_navy', -351, -100, 242, 196, {});
const innerW = 218, dispX = -351;
const ico = (name, cx, cy, s) => { const d = _el('', cx, cy, s, s, { background: `url(${IMG['icon_' + name]}) center / contain no-repeat` }); return d; };
ico('ember', dispX - innerW/2 + 7.5, -100 + 83, 15); text('EMBER', dispX - innerW/2 + 20, -100 + 83, 11, '#98a3b8', 'l');
F('slot_navy', dispX, -100 + 49, innerW, 40, { cls: 'inset' });
text('12,480', dispX + innerW/2 - 6 - 10, -100 + 49, 28, '#f4f6fa', 'r');
text('PAYOUT', dispX - innerW/2 + 20, -100 + 14, 11, '#98a3b8', 'l');
F('pill_coin', dispX, -100 - 20, innerW, 40, { cls: 'inset' });
text('15', dispX + innerW/2 - 6 - 10, -100 - 20, 28, '#ffcf3f', 'r');
text('設定 1 / モード B', dispX - innerW/2 + 2, -100 - 76, 11, '#98a3b8', 'l');
ico('crystal', dispX + innerW/2 - 7, -100 - 76, 14); text('1,240', dispX + innerW/2 - 16, -100 - 76, 11, '#a98bff', 'r');

// リール筐体 444x196
F('panel_navy', 0, -100, 444, 196, { bg: '#23252d' });
for (let i = 0; i < 3; i++) {
  F('slot_navy', (i - 1) * 140, -100, 140, 188, { cls: 'inset', bg: '#05070c' });
  const w = _el('', (i - 1) * 140, -100, 124, 172, { background: '#05070c', borderRadius: '6px' });
  for (let k = 0; k < 3; k++) { const s = _el('', (i - 1) * 140, -100 + (k - 1) * 60, 100, 44, { background: ['#c0392b','#2980b9','#f1c40f'][(i + k) % 3], borderRadius: '8px', opacity: .85 }); }
}

// 右パネル
const sideX = 351;
F('panel_navy', sideX, -100, 242, 196, {});
ico('swords', sideX - innerW/2 + 7.5, -100 + 83, 15); text('PLAYER', sideX - innerW/2 + 20, -100 + 83, 11, '#98a3b8', 'l');
text('Lv 12', sideX - innerW/2, -100 + 60, 16, '#ffcf3f', 'l'); text('EXP', sideX + innerW/2, -100 + 60, 10, '#5f6a80', 'r');
F('gauge_track', sideX, -100 + 44, innerW, 8, { bg: 'rgba(0,0,0,.5)', cls: 'inset' });
F('gauge_fill', sideX - innerW/2 + 60, -100 + 44, 120, 6, { bg: '#3ddc84', cls: 'inset' });
ico('star_navy', sideX - innerW/2 + 7.5, -100 + 24, 15); text('BONUS', sideX - innerW/2 + 20, -100 + 24, 11, '#98a3b8', 'l');
text('―', sideX - innerW/2, -100 + 3, 12, '#ffcf3f', 'l');
F('gauge_track', sideX, -100 - 12, innerW, 8, { bg: 'rgba(0,0,0,.5)', cls: 'inset' });
text('高確ステージ', sideX - innerW/2, -100 - 33 + 5, 12, '#f4f6fa', 'l');
for (const [cx, s] of [[sideX - innerW/2 + 30, '音量'], [sideX, 'グラフ'], [sideX + innerW/2 - 30, 'DEBUG']]) {
  F('pill_navy_sm', cx, -100 - 58, 56, 24, { bg: '#27304a', cls: 'btn' }); text(s, cx, -100 - 58, 12, '#f4f6fa', 'c', 56);
}
_el('', sideX - 30, -100 - 83, innerW - 62, 22, { background: '#07090f', borderRadius: '5px' }); text('+120', sideX + innerW/2, -100 - 83, 11, '#98a3b8', 'r');

// ===== 下段: ボタン（y -234, h 56）=====
F('btn_pink', -351, -234, 242, 56, { bg: '#ff4d6d', cls: 'btn' }); text('MAX BET', -351, -234 + 4, 20, '#fff', 'c', 242); text('Ctrl / Space', -351, -234 - 16, 10, 'rgba(255,255,255,.7)', 'c', 242);
for (let i = 0; i < 3; i++) { F('btn_blue', (i - 1) * 140, -234, 132, 56, { bg: '#27304a', cls: 'btn' }); text('STOP', (i - 1) * 140, -234 + 4, 18, '#fff', 'c', 132); text(['Z / ←','X / ↓','C / →'][i], (i - 1) * 140, -234 - 16, 10, 'rgba(255,255,255,.7)', 'c', 132); }
F('btn_blue', 351, -234, 242, 56, { bg: '#27304a', cls: 'btn' }); text('AUTO', 351, -234 + 4, 18, '#fff', 'c', 242); text('A / Space長押し', 351, -234 - 16, 10, 'rgba(255,255,255,.7)', 'c', 242);

applyMode();
function setAll(m) { for (const k in GROUP_MODE) GROUP_MODE[k] = m; for (const sel of document.querySelectorAll('select[data-group]')) sel.value = m; applyMode(); }
document.getElementById('modeArt').onclick = () => { setAll('art'); toggle('modeArt'); };
document.getElementById('modeProc').onclick = () => { setAll('proc'); toggle('modeProc'); };
for (const sel of document.querySelectorAll('select[data-group]')) sel.onchange = () => { GROUP_MODE[sel.dataset.group] = sel.value; applyMode(); toggle(''); };
function toggle(id) { for (const b of ['modeArt','modeProc']) document.getElementById(b).classList.toggle('on', b === id); }
document.getElementById('grid').onchange = e => document.getElementById('gridEl').style.display = e.target.checked ? 'block' : 'none';
document.getElementById('phone').onchange = e => { const p = document.getElementById('phone'); p.style.width = e.target.checked ? '1170px' : '960px'; stage.style.left = e.target.checked ? '105px' : '0'; };
document.getElementById('real').onclick = () => { const z = document.getElementById('zoom'); z.value = 2.183; z.dispatchEvent(new Event('input')); };
document.getElementById('zoom').oninput = e => { const z = parseFloat(e.target.value); document.getElementById('zoomV').textContent = z.toFixed(2); const p = document.getElementById('phone'); p.style.transform = `scale(${z})`; p.style.transformOrigin = '50% 0'; p.style.marginBottom = (540 * (z - 1)) + 'px'; };
document.getElementById('note').textContent = '位置・大きさは GameController の定数（docs/ui_frames.md）そのまま。枠は Resources の画像を 9 分割で伸ばしていて、縁の幅は UiSkin.FrameBorders と同じ。文字と数字は見本。背景と図柄は色の板。';
</script>
'''

with open(OUT, 'w', encoding='utf-8') as f:
    f.write(HTML.replace('__IMG__', json.dumps(imgs)).replace('__BORDERS__', json.dumps(borders)))
print(OUT, os.path.getsize(OUT) // 1024, 'KB')
