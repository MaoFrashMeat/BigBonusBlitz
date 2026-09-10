# スプライトのモーションビューア（単体 HTML）を作る。
# 使い方: python tools/gen_sprite_viewer.py
# 出力: tools/sprite_viewer.html
#
# 画像を data URI で埋め込むので、ファイルを 1 つ開くだけで動く。
# 絵を描き直したら gen_hero_sprites.py → このスクリプトの順で実行する。
import base64
import io
import json
import os

HERE = os.path.dirname(__file__)
ART = os.path.join(HERE, '..', 'UnityProject', 'BigBonusBlitz', 'Assets', 'Resources', 'Art')
OUT = os.path.join(HERE, 'sprite_viewer.html')

# (グループ, 表示名, [(ファイル, 表示名, コマ数, 1周の秒数)])
SETS = [
    ('gen', '主人公 生成版（ComfyUI）', 'HeroGen', [
        ('hero_idle',    '待機 idle',      4, 1.6),
        ('hero_walk',    '歩行 walk',      6, 0.8),
        ('hero_attack',  '攻撃 attack',    4, 0.6),
        ('hero_slash',   '薙ぎ払い slash', 4, 0.5),
        ('hero_cast',    '大技 cast',      4, 0.8),
        ('hero_guard',   'ガード guard',   2, 0.6),
        ('hero_hit',     '被弾 hit',       2, 0.5),
        ('hero_victory', '勝利 victory',   4, 0.9),
        ('hero_focus',   '集中 focus',     4, 1.2),
    ]),
    ('chibi', '主人公 ちび（2.5頭身）', 'HeroChibi', [
        ('hero_idle',    '待機 idle',      4, 1.6),
        ('hero_walk',    '歩行 walk',      6, 0.8),
        ('hero_attack',  '攻撃 attack',    4, 0.6),
        ('hero_slash',   '薙ぎ払い slash', 4, 0.5),
        ('hero_cast',    '大技 cast',      4, 0.8),
        ('hero_guard',   'ガード guard',   2, 0.6),
        ('hero_hit',     '被弾 hit',       2, 0.5),
        ('hero_victory', '勝利 victory',   4, 0.9),
        ('hero_focus',   '集中 focus',     4, 1.2),
    ]),
    ('hero', '主人公 等身（今の版）', 'Hero', [
        ('hero_idle',    '待機 idle',      4, 1.6),
        ('hero_walk',    '歩行 walk',      6, 0.8),
        ('hero_attack',  '攻撃 attack',    4, 0.6),
        ('hero_slash',   '薙ぎ払い slash', 4, 0.5),
        ('hero_cast',    '大技 cast',      4, 0.8),
        ('hero_guard',   'ガード guard',   2, 0.6),
        ('hero_hit',     '被弾 hit',       2, 0.5),
        ('hero_victory', '勝利 victory',   4, 0.9),
        ('hero_focus',   '集中 focus',     4, 1.2),
    ]),
    ('traveler', '旅人 A〜E', 'Travelers', [
        ('traveler_A', 'A 行商人',   4, 0.55),
        ('traveler_B', 'B 巡礼者',   4, 0.55),
        ('traveler_C', 'C 狩人',     4, 0.55),
        ('traveler_D', 'D 占い師',   4, 0.55),
        ('traveler_E', 'E 王の使者', 4, 0.55),
    ]),
    ('enemy', '敵', 'Enemies', [
        ('slime',  'スライム', 1, 1),
        ('goblin', 'ゴブリン', 1, 1),
        ('bat',    'コウモリ', 1, 1),
    ]),
]

# 開いた瞬間に出しておくスプライトシート。
# ここに置いたものは「シートを開く」側に最初から並ぶ。
# 大きい画像は長辺 MAX_SHEET px に縮めてから埋め込む（表示用。原寸で扱うなら D&D する）
SHEET_DIRS = [
    os.path.join(HERE, '..', 'assets', 'Chr0001', '001'),
]
SHEET_GLOBS = ['ChatGPT*.png']
MAX_SHEET = 900


def data_uri(path):
    with open(path, 'rb') as f:
        return 'data:image/png;base64,' + base64.b64encode(f.read()).decode('ascii')


def sheet_uri(path):
    """表示用に縮めて data URI にする。
    透過が無ければ JPEG（この用途では PNG の 1/8 以下になる）。原寸で扱いたいときは D&D する。"""
    try:
        from PIL import Image
    except ImportError:
        return data_uri(path)
    im = Image.open(path)
    has_alpha = im.mode in ('RGBA', 'LA') and im.getchannel('A').getextrema()[0] < 250
    im = im.convert('RGBA')
    w, h = im.size
    if max(w, h) > MAX_SHEET:
        k = MAX_SHEET / max(w, h)
        im = im.resize((max(1, round(w * k)), max(1, round(h * k))), Image.LANCZOS)
    buf = io.BytesIO()
    if has_alpha:
        im.save(buf, 'PNG', optimize=True)
        mime = 'image/png'
    else:
        im.convert('RGB').save(buf, 'JPEG', quality=88, optimize=True)
        mime = 'image/jpeg'
    return 'data:' + mime + ';base64,' + base64.b64encode(buf.getvalue()).decode('ascii')


def build_sheets():
    import glob
    out = []
    seen = set()
    for d in SHEET_DIRS:
        if not os.path.isdir(d):
            continue
        for pat in SHEET_GLOBS:
            for p in sorted(glob.glob(os.path.join(d, pat))):
                name = os.path.basename(p)
                if name in seen:
                    continue
                seen.add(name)
                out.append({'name': name, 'src': sheet_uri(p)})
    total = sum(len(o['src']) for o in out)
    print(f'シート {len(out)} 枚を埋め込み {total/1024:.0f} KB')
    return out

def build_data():
    out = {}
    total = 0
    for key, label, folder, items in SETS:
        arr = []
        for file, name, frames, dur in items:
            p = os.path.join(ART, folder, file + '.png')
            if not os.path.exists(p):
                print('  なし:', p)
                continue
            total += os.path.getsize(p)
            arr.append({'name': name, 'frames': frames, 'dur': dur, 'src': data_uri(p)})
        out[key] = {'label': label, 'items': arr}
    print(f'埋め込み {total/1024:.0f} KB')
    return out

HTML = '''<!doctype html>
<html lang="ja">
<head>
<meta charset="utf-8">
<title>BBB スプライトビューア</title>
<style>
  :root { --bg:#0b0f1a; --panel:#141a29; --edge:#2a3450; --text:#f2f4f8; --dim:#9aa4b8; --gold:#ffcc33; }
  * { box-sizing: border-box; }
  body { margin:0; background:var(--bg); color:var(--text);
         font:14px/1.6 "Segoe UI","Yu Gothic UI",system-ui,sans-serif; }
  header { padding:12px 18px; border-bottom:1px solid var(--edge); display:flex; gap:18px;
           align-items:center; flex-wrap:wrap; position:sticky; top:0; background:var(--bg); z-index:10; }
  h1 { font-size:15px; margin:0; }
  .ctl { display:flex; gap:8px; align-items:center; color:var(--dim); font-size:12px; }
  input[type=range] { width:120px; accent-color:var(--gold); }
  select,button { background:#232b40; color:var(--text); border:1px solid var(--edge);
                  border-radius:6px; padding:5px 10px; font:inherit; font-size:12px; cursor:pointer; }
  button.on { background:var(--gold); color:#0b0f1a; font-weight:700; }
  main { padding:18px; display:grid; grid-template-columns:repeat(auto-fill,minmax(250px,1fr)); gap:14px; }
  .card { background:var(--panel); border:1px solid var(--edge); border-radius:10px; overflow:hidden; }
  .card h2 { margin:0; padding:8px 12px; font-size:13px; border-bottom:1px solid var(--edge);
             display:flex; justify-content:space-between; align-items:center; }
  .card h2 span { color:var(--dim); font-weight:400; font-size:11px; }
  .stage { position:relative; display:grid; place-items:center; min-height:250px; padding:10px;
           background:linear-gradient(180deg,#1a2740 0%,#223052 56%,#2c3a24 56%,#384a2c 100%); }
  .stage.grid::after { content:""; position:absolute; inset:0; pointer-events:none;
      background-image:linear-gradient(rgba(255,255,255,.10) 1px,transparent 1px),
                       linear-gradient(90deg,rgba(255,255,255,.10) 1px,transparent 1px);
      background-size:24px 24px; }
  .stage.compare::before { content:""; position:absolute; width:206px; height:206px;
      border:1px dashed rgba(255,204,51,.55); border-radius:4px; pointer-events:none; }
  canvas { image-rendering:pixelated; display:block; }
  .frames { display:flex; gap:4px; padding:8px 10px; flex-wrap:wrap; border-top:1px solid var(--edge); }
  .frames canvas { border:1px solid var(--edge); border-radius:4px; background:#0d1220; cursor:pointer; }
  .frames canvas.cur { border-color:var(--gold); }
  .note { padding:14px 18px; color:var(--dim); font-size:12px; border-top:1px solid var(--edge); }
  code { background:#0d1220; padding:1px 6px; border-radius:4px; color:var(--gold); }
  .tabs { display:flex; gap:4px; }
  .tabs button.on { background:var(--gold); color:#0b0f1a; font-weight:700; }
  .drop { margin:18px; padding:26px; border:2px dashed var(--edge); border-radius:12px;
          text-align:center; color:var(--dim); font-size:13px; }
  .drop.hot { border-color:var(--gold); color:var(--text); background:#1a2136; }
  .drop b { color:var(--text); }
  .sheet { margin:18px; background:var(--panel); border:1px solid var(--edge); border-radius:10px; }
  .sheet > h2 { margin:0; padding:10px 14px; font-size:13px; border-bottom:1px solid var(--edge);
                display:flex; gap:12px; align-items:center; flex-wrap:wrap; }
  .sheet > h2 span { color:var(--dim); font-weight:400; font-size:11px; }
  .sheet .body { display:flex; gap:14px; padding:14px; flex-wrap:wrap; align-items:flex-start; }
  .sheet .opts { display:flex; flex-direction:column; gap:7px; min-width:210px; font-size:12px; color:var(--dim); }
  .sheet .opts label { display:flex; gap:7px; align-items:center; }
  .sheet .opts input[type=number] { width:58px; background:#0d1220; color:var(--text);
        border:1px solid var(--edge); border-radius:5px; padding:3px 6px; font:inherit; }
  .sheet .opts input[type=range] { width:96px; }
  .sheet .opts hr { border:0; border-top:1px solid var(--edge); width:100%; margin:3px 0; }
  .play { position:relative; background:linear-gradient(180deg,#1a2740 0%,#223052 56%,#2c3a24 56%,#384a2c 100%);
          border-radius:8px; padding:10px; display:grid; place-items:center; }
  .strip { display:flex; gap:4px; padding:10px 14px; flex-wrap:wrap; border-top:1px solid var(--edge); }
  .strip canvas { border:1px solid var(--edge); border-radius:4px; background:#0d1220; cursor:pointer; }
  .strip canvas.cur { border-color:var(--gold); }
  .warn { margin:0 14px 12px; padding:8px 10px; border-radius:7px; font-size:11.5px;
          background:#2a1b0e; border:1px solid #7c4a1a; color:#fcd9b6; }
</style>
</head>
<body>
<header>
  <h1>スプライトビューア</h1>
  <div class="ctl tabs">
    <button id="tabBuiltin" class="on">内蔵</button><button id="tabOpen">シートを開く</button>
  </div>
  <div class="ctl" id="whoWrap">対象 <select id="who"></select></div>
  <div class="ctl">速さ <input id="fps" type="range" min="1" max="24" value="8"><span id="fpsv">8 fps</span></div>
  <div class="ctl">拡大 <input id="zoom" type="range" min="0.5" max="4" step="0.25" value="1"><span id="zoomv">1x</span></div>
  <div class="ctl">
    <button id="grid">方眼</button>
    <button id="compare" class="on">表示枠 206px</button>
    <button id="play" class="on">再生中</button>
  </div>
</header>
<main id="main"></main>
<div id="openView" hidden>
  <div class="drop" id="drop">
    ここに <b>スプライトシートの PNG</b> をドラッグ&ドロップ、または
    <button id="pick">ファイルを選ぶ</button><br>
    コマ割りは余白から自動で推定します。合わなければ列と行を直接入れてください。
  </div>
  <div id="sheets"></div>
</div>
<p class="note">
  破線の四角がゲーム内での実寸（206px）。この中で細部が潰れないかを見てください。
  絵を直したら <code>python tools/gen_hero_sprites.py</code> と
  <code>python tools/gen_sprite_viewer.py</code> を実行して、このページを開き直します。
</p>
<script>
const DATA = __DATA__;
const SHEETS = __SHEETS__;
const main = document.getElementById('main');
const who = document.getElementById('who');
const fps = document.getElementById('fps'), fpsv = document.getElementById('fpsv');
const zoom = document.getElementById('zoom'), zoomv = document.getElementById('zoomv');
const gridBtn = document.getElementById('grid'), cmpBtn = document.getElementById('compare');
const playBtn = document.getElementById('play');
let playing = true, showGrid = false, showCompare = true, anims = [];

for (const k in DATA) {
  const o = document.createElement('option');
  o.value = k; o.textContent = DATA[k].label; who.appendChild(o);
}

function build() {
  anims = []; main.innerHTML = '';
  for (const it of DATA[who.value].items) {
    const card = document.createElement('div');
    card.className = 'card';
    card.innerHTML = `<h2>${it.name}<span>${it.frames} コマ / ${it.dur}s</span></h2>`;
    const stage = document.createElement('div');
    stage.className = 'stage' + (showGrid ? ' grid' : '') + (showCompare ? ' compare' : '');
    const cv = document.createElement('canvas');
    stage.appendChild(cv); card.appendChild(stage);
    const strip = document.createElement('div');
    strip.className = 'frames'; card.appendChild(strip);
    main.appendChild(card);

    const img = new Image();
    img.onload = () => {
      const fw = img.width / it.frames, fh = img.height;
      const a = { img, frames: it.frames, fw, fh, cv, ctx: cv.getContext('2d'), i: 0, t: 0, thumbs: [] };
      for (let f = 0; f < it.frames; f++) {
        const t = document.createElement('canvas');
        const s = 54 / Math.max(fw, fh);
        t.width = Math.round(fw * s); t.height = Math.round(fh * s);
        const tc = t.getContext('2d'); tc.imageSmoothingEnabled = false;
        tc.drawImage(img, f * fw, 0, fw, fh, 0, 0, t.width, t.height);
        t.onclick = () => { a.i = f; a.t = 0; draw(a); mark(a); };
        strip.appendChild(t); a.thumbs.push(t);
      }
      resize(a); draw(a); mark(a); anims.push(a);
    };
    img.src = it.src;
  }
}
function resize(a) {
  const z = parseFloat(zoom.value);
  a.cv.width = Math.round(a.fw * z); a.cv.height = Math.round(a.fh * z);
  a.ctx.imageSmoothingEnabled = false;
}
function draw(a) {
  a.ctx.clearRect(0, 0, a.cv.width, a.cv.height);
  a.ctx.imageSmoothingEnabled = false;
  a.ctx.drawImage(a.img, a.i * a.fw, 0, a.fw, a.fh, 0, 0, a.cv.width, a.cv.height);
}
function mark(a) { a.thumbs.forEach((t, i) => t.classList.toggle('cur', i === a.i)); }

let last = performance.now();
function tick(now) {
  const dt = (now - last) / 1000; last = now;
  if (playing) {
    const step = 1 / parseInt(fps.value, 10);
    for (const a of anims) {
      if (a.frames <= 1) continue;
      a.t += dt;
      while (a.t >= step) { a.t -= step; a.i = (a.i + 1) % a.frames; (a.render || (() => { draw(a); mark(a); }))(); }
    }
  }
  requestAnimationFrame(tick);
}
requestAnimationFrame(tick);

who.onchange = build;
fps.oninput = () => fpsv.textContent = fps.value + ' fps';
zoom.oninput = () => { zoomv.textContent = zoom.value + 'x'; for (const a of anims) { resize(a); draw(a); } };
gridBtn.onclick = () => { showGrid = !showGrid; gridBtn.classList.toggle('on', showGrid);
  document.querySelectorAll('.stage').forEach(s => s.classList.toggle('grid', showGrid)); };
cmpBtn.onclick = () => { showCompare = !showCompare; cmpBtn.classList.toggle('on', showCompare);
  document.querySelectorAll('.stage').forEach(s => s.classList.toggle('compare', showCompare)); };
playBtn.onclick = () => { playing = !playing; playBtn.classList.toggle('on', playing);
  playBtn.textContent = playing ? '再生中' : '停止中'; };

build();

/* ============================================================
   任意のスプライトシートを開く
============================================================ */
const openView = document.getElementById('openView');
const sheetsEl = document.getElementById('sheets');
const drop = document.getElementById('drop');
const pick = document.getElementById('pick');
const tabB = document.getElementById('tabBuiltin'), tabO = document.getElementById('tabOpen');
const whoWrap = document.getElementById('whoWrap');

function setTab(open) {
  tabB.classList.toggle('on', !open); tabO.classList.toggle('on', open);
  main.hidden = open; openView.hidden = !open; whoWrap.style.display = open ? 'none' : '';
}
tabB.onclick = () => setTab(false);
tabO.onclick = () => setTab(true);

const fileIn = document.createElement('input');
fileIn.type = 'file'; fileIn.accept = 'image/*'; fileIn.multiple = true;
fileIn.style.display = 'none'; document.body.appendChild(fileIn);
pick.onclick = () => fileIn.click();
fileIn.onchange = () => { for (const f of fileIn.files) loadSheet(f); fileIn.value = ''; };
['dragenter','dragover'].forEach(e => drop.addEventListener(e, ev => {
  ev.preventDefault(); drop.classList.add('hot'); }));
['dragleave','drop'].forEach(e => drop.addEventListener(e, ev => {
  ev.preventDefault(); drop.classList.remove('hot'); }));
drop.addEventListener('drop', ev => {
  for (const f of ev.dataTransfer.files) if (f.type.startsWith('image/')) loadSheet(f);
});

/* --- 画素の読み出しと背景判定 --- */
function readPixels(img) {
  const c = document.createElement('canvas');
  c.width = img.naturalWidth; c.height = img.naturalHeight;
  const x = c.getContext('2d', { willReadFrequently: true });
  x.drawImage(img, 0, 0);
  return { d: x.getImageData(0, 0, c.width, c.height).data, W: c.width, H: c.height };
}
/* 四隅から背景色を決める。透過素材ならアルファ0を背景として扱う */
function guessBg(px) {
  const d = px.d, W = px.W, H = px.H;
  const pts = [[0,0],[W-1,0],[0,H-1],[W-1,H-1],[W>>1,0],[W>>1,H-1]];
  let a = 0, r = 0, g = 0, b = 0;
  for (const pt of pts) { const i = (pt[1] * W + pt[0]) * 4; r += d[i]; g += d[i+1]; b += d[i+2]; a += d[i+3]; }
  const n = pts.length;
  return { r: r/n, g: g/n, b: b/n, a: a/n, alpha: (a/n) < 24 };
}
function isBgAt(px, bg, i, tol) {
  const d = px.d;
  if (bg.alpha) return d[i+3] < 24;
  if (d[i+3] < 24) return true;
  return Math.abs(d[i]-bg.r) <= tol && Math.abs(d[i+1]-bg.g) <= tol && Math.abs(d[i+2]-bg.b) <= tol;
}
/* 連続して埋まっている区間を返す */
function runs(occ, minLen) {
  const out = []; let s = -1;
  for (let i = 0; i < occ.length; i++) {
    if (occ[i] && s < 0) s = i;
    else if (!occ[i] && s >= 0) { if (i - s >= minLen) out.push([s, i - 1]); s = -1; }
  }
  if (s >= 0 && occ.length - s >= minLen) out.push([s, occ.length - 1]);
  return out;
}
/* 分割線の上にどれだけ中身が乗るかで分割数を採点する。
   絵が隣と接していても当たる。線はセルの 6% までずらして谷を探す */
function scoreN(prof, span, n) {
  const cell = span / n, win = Math.max(1, Math.floor(cell * 0.06));
  let sum = 0;
  for (let k = 1; k < n; k++) {
    const x = Math.round(k * cell);
    let mn = 1;
    for (let i = Math.max(0, x - win); i <= Math.min(span - 1, x + win); i++) {
      if (prof[i] < mn) mn = prof[i];
    }
    sum += mn;
  }
  return sum / (n - 1);
}
/* 候補の検証。n 分割したとき、どのスライスにも中身があり、
   中身の幅が揃っているか。これが無いと 6 コマのシートを 9 分割と誤る */
function sliceOK(prof, span, n) {
  const cell = span / n, sp = [];
  for (let k = 0; k < n; k++) {
    const a = Math.round(k * cell), b = Math.round((k + 1) * cell) - 1;
    let f = -1, l = -1;
    for (let i = a; i <= b; i++) if (prof[i] > 0.0005) { if (f < 0) f = i; l = i; }
    if (f < 0) return false;
    sp.push(l - f + 1);
  }
  const m = sp.reduce((s, v) => s + v, 0) / n;
  if (m <= 0) return false;
  const sd = Math.sqrt(sp.reduce((s, v) => s + (v - m) * (v - m), 0) / n) / m;
  return sd < 0.22;
}
/* 密度が十分低く、検証も通る候補のうち最大の分割数を採る。
   2 と 3 と 6 が同点になるのは 2,3 の線が 6 の線の部分集合だから */
function pickN(prof, span, maxN) {
  const sc = [];
  let lo = 1;
  for (let n = 2; n <= maxN; n++) { sc[n] = scoreN(prof, span, n); if (sc[n] < lo) lo = sc[n]; }
  if (lo > 0.10) return 1;
  const thr = Math.max(0.02, lo + 0.02);
  let best = 1;
  for (let n = 2; n <= maxN; n++) if (sc[n] <= thr && n > best && sliceOK(prof, span, n)) best = n;
  return best;
}
/* コマ割りの推定。余白法（完全な空列を数える）と密度法を両方かけ、大きい方を採る。
   どちらも「くっついて数え落とす」方向に外れるので、最大を採ると当たりやすい */
function detectGrid(px, bg, tol) {
  const W = px.W, H = px.H;
  const colN = new Float32Array(W), rowN = new Float32Array(H);
  const colB = new Uint8Array(W), rowB = new Uint8Array(H);
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    if (!isBgAt(px, bg, (y * W + x) * 4, tol)) {
      colN[x]++; rowN[y]++; colB[x] = 1; rowB[y] = 1;
    }
  }
  for (let x = 0; x < W; x++) colN[x] /= H;
  for (let y = 0; y < H; y++) rowN[y] /= W;
  const cr = Math.max(1, runs(colB, Math.max(2, Math.round(W * 0.01))).length);
  const rr = Math.max(1, runs(rowB, Math.max(2, Math.round(H * 0.01))).length);
  return { cols: Math.max(cr, pickN(colN, W, 16)), rows: Math.max(rr, pickN(rowN, H, 16)) };
}
/* 各コマの中身の外接矩形。均等セルの中だけを探す */
function frameBoxes(px, bg, tol, cols, rows) {
  const W = px.W, H = px.H, out = [];
  const cw = W / cols, ch = H / rows;
  for (let r = 0; r < rows; r++) for (let c = 0; c < cols; c++) {
    const x0 = Math.round(c * cw), x1 = Math.round((c + 1) * cw) - 1;
    const y0 = Math.round(r * ch), y1 = Math.round((r + 1) * ch) - 1;
    const cell = { x: x0, y: y0, w: x1 - x0 + 1, h: y1 - y0 + 1 };
    let mnx = x1, mny = y1, mxx = x0, mxy = y0, any = false;
    for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
      if (isBgAt(px, bg, (y * W + x) * 4, tol)) continue;
      any = true;
      if (x < mnx) mnx = x;
      if (x > mxx) mxx = x;
      if (y < mny) mny = y;
      if (y > mxy) mxy = y;
    }
    out.push(any
      ? { x: mnx, y: mny, w: mxx - mnx + 1, h: mxy - mny + 1, cell: cell }
      : { x: x0, y: y0, w: cell.w, h: cell.h, cell: cell, empty: true });
  }
  return out;
}
/* 背景を抜いた画像を作る */
function keyOut(img, bg, tol) {
  const c = document.createElement('canvas');
  c.width = img.naturalWidth; c.height = img.naturalHeight;
  const x = c.getContext('2d', { willReadFrequently: true });
  x.drawImage(img, 0, 0);
  if (bg.alpha) return c;
  const im = x.getImageData(0, 0, c.width, c.height), d = im.data;
  for (let i = 0; i < d.length; i += 4) {
    const dist = Math.max(Math.abs(d[i]-bg.r), Math.abs(d[i+1]-bg.g), Math.abs(d[i+2]-bg.b));
    if (dist <= tol) d[i+3] = 0;
    else if (dist <= tol * 1.7) d[i+3] = Math.round(d[i+3] * (dist - tol) / (tol * 0.7));
  }
  x.putImageData(im, 0, 0);
  return c;
}
function saveCanvas(cv, name) {
  cv.toBlob(b => {
    const a = document.createElement('a');
    a.href = URL.createObjectURL(b); a.download = name;
    document.body.appendChild(a); a.click(); a.remove();
    setTimeout(() => URL.revokeObjectURL(a.href), 4000);
  }, 'image/png');
}
function saveText(text, name) {
  const b = new Blob([text], { type: 'application/json' });
  const a = document.createElement('a');
  a.href = URL.createObjectURL(b); a.download = name;
  document.body.appendChild(a); a.click(); a.remove();
  setTimeout(() => URL.revokeObjectURL(a.href), 4000);
}
function loadSheet(file) {
  const img = new Image();
  img.onload = () => buildSheet(img, file.name);
  img.src = URL.createObjectURL(file);
}

function buildSheet(img, name) {
  const px = readPixels(img);
  const st = { tol: 18, cols: 1, rows: 1, trim: true, keyed: false, onion: 0, align: 'foot', i: 0, t: 0 };
  const bg0 = guessBg(px);
  const det = detectGrid(px, bg0, st.tol);
  st.cols = det.cols; st.rows = det.rows;

  const card = document.createElement('div');
  card.className = 'sheet';
  card.innerHTML = '<h2>' + name + '<span>' + px.W + ' x ' + px.H + ' px</span><span class="det"></span></h2>';
  const body = document.createElement('div'); body.className = 'body';
  const opts = document.createElement('div'); opts.className = 'opts';
  const stage = document.createElement('div'); stage.className = 'play';
  const cv = document.createElement('canvas'); stage.appendChild(cv);
  body.appendChild(opts); body.appendChild(stage);
  const strip = document.createElement('div'); strip.className = 'strip';
  card.appendChild(body); card.appendChild(strip);
  sheetsEl.appendChild(card);

  opts.innerHTML =
    '<label>列 <input type="number" class="pc" min="1" max="64" value="' + st.cols + '">' +
    ' 行 <input type="number" class="pr" min="1" max="64" value="' + st.rows + '"></label>' +
    '<label>背景しきい値 <input type="range" class="pt" min="0" max="90" value="' + st.tol + '"><b class="vt">' + st.tol + '</b></label>' +
    '<hr>' +
    '<label><input type="checkbox" class="ptrim" checked> 余白をトリミング</label>' +
    '<label>揃え <select class="pa"><option value="foot">足元</option><option value="cell">セル</option></select></label>' +
    '<label><input type="checkbox" class="pkey"> 背景を透過</label>' +
    '<label>オニオン <input type="range" class="po" min="0" max="3" value="0"><b class="vo">0</b></label>' +
    '<hr>' +
    '<button class="ex1">トリミング済みシートを保存</button>' +
    '<button class="ex2">frames.json を保存</button>' +
    '<button class="ex3">このコマを保存</button>';

  const q = sel => opts.querySelector(sel);
  const detEl = card.querySelector('.det');
  let boxes = [], src = img, maxW = 1, maxH = 1;

  function frameCount() { return st.cols * st.rows; }
  function dstSize() {
    return st.trim ? { w: maxW, h: maxH }
                   : { w: Math.round(px.W / st.cols), h: Math.round(px.H / st.rows) };
  }
  function recompute() {
    const bg = guessBg(px);
    boxes = frameBoxes(px, bg, st.tol, st.cols, st.rows);
    maxW = 1; maxH = 1;
    for (const b of boxes) { if (b.w > maxW) maxW = b.w; if (b.h > maxH) maxH = b.h; }
    src = st.keyed ? keyOut(img, bg, st.tol) : img;
    if (st.i >= frameCount()) st.i = 0;
    detEl.textContent = st.cols + ' x ' + st.rows + ' = ' + frameCount() + ' コマ / 最大 ' +
      maxW + ' x ' + maxH + ' px' + (bg.alpha ? ' / 透過素材' : '');
    buildStrip(); resizeStage(); render();
  }
  /* 1コマを (0,0)-(w,h) に描く。足元揃えなら下端中央を合わせる */
  function drawFrame(ctx, idx, w, h, alpha) {
    const b = boxes[idx]; if (!b) return;
    ctx.globalAlpha = (alpha === undefined) ? 1 : alpha;
    if (!st.trim) {
      ctx.drawImage(src, b.cell.x, b.cell.y, b.cell.w, b.cell.h, 0, 0, w, h);
    } else if (st.align === 'foot') {
      ctx.drawImage(src, b.x, b.y, b.w, b.h, Math.round((w - b.w) / 2), h - b.h, b.w, b.h);
    } else {
      ctx.drawImage(src, b.x, b.y, b.w, b.h, Math.round((w - b.w) / 2), Math.round((h - b.h) / 2), b.w, b.h);
    }
    ctx.globalAlpha = 1;
  }
  function resizeStage() {
    const z = parseFloat(zoom.value), s = dstSize();
    const fit = Math.min(1, 320 / Math.max(s.w, s.h));
    cv.width = Math.max(1, Math.round(s.w * fit * z));
    cv.height = Math.max(1, Math.round(s.h * fit * z));
  }
  function render() {
    const ctx = cv.getContext('2d');
    ctx.clearRect(0, 0, cv.width, cv.height);
    const s = dstSize(), n = frameCount();
    const tmp = document.createElement('canvas'); tmp.width = s.w; tmp.height = s.h;
    const tc = tmp.getContext('2d');
    for (let k = st.onion; k >= 1; k--) {
      drawFrame(tc, (st.i - k + n * 2) % n, s.w, s.h, 0.16 + 0.09 * (st.onion - k));
    }
    drawFrame(tc, st.i, s.w, s.h, 1);
    ctx.drawImage(tmp, 0, 0, cv.width, cv.height);
    const th = strip.querySelectorAll('canvas');
    for (let i = 0; i < th.length; i++) th[i].classList.toggle('cur', i === st.i);
  }
  function buildStrip() {
    strip.innerHTML = '';
    const s = dstSize();
    for (let f = 0; f < frameCount(); f++) {
      const t = document.createElement('canvas');
      const k = 58 / Math.max(s.w, s.h);
      t.width = Math.max(1, Math.round(s.w * k));
      t.height = Math.max(1, Math.round(s.h * k));
      const tmp = document.createElement('canvas'); tmp.width = s.w; tmp.height = s.h;
      drawFrame(tmp.getContext('2d'), f, s.w, s.h, 1);
      t.getContext('2d').drawImage(tmp, 0, 0, t.width, t.height);
      t.title = '#' + (f + 1);
      t.onclick = (function (idx) { return function () { st.i = idx; st.t = 0; render(); }; })(f);
      strip.appendChild(t);
    }
  }

  q('.pc').oninput = e => { st.cols = Math.max(1, parseInt(e.target.value, 10) || 1); st.i = 0; recompute(); };
  q('.pr').oninput = e => { st.rows = Math.max(1, parseInt(e.target.value, 10) || 1); st.i = 0; recompute(); };
  q('.pt').oninput = e => { st.tol = parseInt(e.target.value, 10); q('.vt').textContent = st.tol; recompute(); };
  q('.ptrim').onchange = e => { st.trim = e.target.checked; recompute(); };
  q('.pa').onchange = e => { st.align = e.target.value; recompute(); };
  q('.pkey').onchange = e => { st.keyed = e.target.checked; recompute(); };
  q('.po').oninput = e => { st.onion = parseInt(e.target.value, 10); q('.vo').textContent = st.onion; render(); };

  const base = name.replace(/\.[^.]+$/, '');
  q('.ex1').onclick = () => {
    const s = dstSize();
    const out = document.createElement('canvas');
    out.width = s.w * st.cols; out.height = s.h * st.rows;
    const oc = out.getContext('2d');
    for (let f = 0; f < frameCount(); f++) {
      const tmp = document.createElement('canvas'); tmp.width = s.w; tmp.height = s.h;
      drawFrame(tmp.getContext('2d'), f, s.w, s.h, 1);
      oc.drawImage(tmp, (f % st.cols) * s.w, Math.floor(f / st.cols) * s.h);
    }
    saveCanvas(out, base + '_' + st.cols + 'x' + st.rows + '_' + s.w + 'x' + s.h + '.png');
  };
  q('.ex2').onclick = () => {
    const s = dstSize();
    saveText(JSON.stringify({
      source: name,
      sheet: { w: px.W, h: px.H },
      cols: st.cols, rows: st.rows, frames: frameCount(),
      cell: s, trim: st.trim, align: st.align,
      pivot: st.align === 'foot' ? { x: 0.5, y: 1 } : { x: 0.5, y: 0.5 },
      boxes: boxes.map(b => ({ x: b.x, y: b.y, w: b.w, h: b.h }))
    }, null, 2), base + '_frames.json');
  };
  q('.ex3').onclick = () => {
    const s = dstSize();
    const tmp = document.createElement('canvas'); tmp.width = s.w; tmp.height = s.h;
    drawFrame(tmp.getContext('2d'), st.i, s.w, s.h, 1);
    saveCanvas(tmp, base + '_f' + String(st.i + 1).padStart(3, '0') + '.png');
  };

  recompute();
  anims.push({
    get frames() { return frameCount(); },
    get i() { return st.i; }, set i(v) { st.i = v; },
    get t() { return st.t; }, set t(v) { st.t = v; },
    render: render, resize: resizeStage
  });
}

/* 埋め込み済みのシートを最初から並べる。開いた瞬間に見える状態にする */
if (SHEETS && SHEETS.length) {
  setTab(true);
  for (const sh of SHEETS) {
    const im = new Image();
    im.onload = ((nm) => () => buildSheet(im, nm))(sh.name);
    im.src = sh.src;
  }
}

/* 拡大つまみを開いたシートにも効かせる */
const zoomPrev = zoom.oninput;
zoom.oninput = () => {
  zoomPrev();
  for (const a of anims) if (a.resize) { a.resize(); a.render(); }
};
</script>
</body>
</html>
'''

def main():
    data = build_data()
    sheets = build_sheets()
    html = HTML.replace('__DATA__', json.dumps(data, ensure_ascii=False))
    html = html.replace('__SHEETS__', json.dumps(sheets, ensure_ascii=False))
    with io.open(OUT, 'w', encoding='utf-8') as f:
        f.write(html)
    print('wrote', OUT, f'({os.path.getsize(OUT)/1024:.0f} KB)')

if __name__ == '__main__':
    main()
