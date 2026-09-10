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

def data_uri(path):
    with open(path, 'rb') as f:
        return 'data:image/png;base64,' + base64.b64encode(f.read()).decode('ascii')

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
</style>
</head>
<body>
<header>
  <h1>スプライトビューア</h1>
  <div class="ctl">対象 <select id="who"></select></div>
  <div class="ctl">速さ <input id="fps" type="range" min="1" max="24" value="8"><span id="fpsv">8 fps</span></div>
  <div class="ctl">拡大 <input id="zoom" type="range" min="0.5" max="4" step="0.25" value="1"><span id="zoomv">1x</span></div>
  <div class="ctl">
    <button id="grid">方眼</button>
    <button id="compare" class="on">表示枠 206px</button>
    <button id="play" class="on">再生中</button>
  </div>
</header>
<main id="main"></main>
<p class="note">
  破線の四角がゲーム内での実寸（206px）。この中で細部が潰れないかを見てください。
  絵を直したら <code>python tools/gen_hero_sprites.py</code> と
  <code>python tools/gen_sprite_viewer.py</code> を実行して、このページを開き直します。
</p>
<script>
const DATA = __DATA__;
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
      while (a.t >= step) { a.t -= step; a.i = (a.i + 1) % a.frames; draw(a); mark(a); }
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
</script>
</body>
</html>
'''

def main():
    data = build_data()
    html = HTML.replace('__DATA__', json.dumps(data, ensure_ascii=False))
    with io.open(OUT, 'w', encoding='utf-8') as f:
        f.write(html)
    print('wrote', OUT, f'({os.path.getsize(OUT)/1024:.0f} KB)')

if __name__ == '__main__':
    main()
