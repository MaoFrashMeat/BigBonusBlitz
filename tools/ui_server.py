# UI ビューア（tools/ui_viewer.html）を配信し、「保存」を Resources/Data/ui_layout.json に書く小さなサーバ。
#   python tools/ui_server.py            → http://localhost:8765/ui_viewer.html
#   効果音ビューア（se_viewer.html）の「Unity に書き込む」は /se_apply: tools/se_choice.json を書いて se_build.py を回す
#
# 保存されたファイルは Unity が次の Play で読む（UiLayout.cs）。ブラウザは直接ファイルに書けないので、
# この 1 本を挟む。GET は tools/ の中をそのまま返す。
import json
import os
import subprocess
import sys
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..'))
LAYOUT = os.path.join(ROOT, 'UnityProject', 'BigBonusBlitz', 'Assets', 'Resources', 'Data', 'ui_layout.json')
TITLE = os.path.join(ROOT, 'UnityProject', 'BigBonusBlitz', 'Assets', 'Resources', 'Data', 'title_layers.json')
PORT = int(sys.argv[1]) if len(sys.argv) > 1 else 8765


class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *a, **kw):
        super().__init__(*a, directory=HERE, **kw)

    def log_message(self, fmt, *args):
        pass   # 黒い窓に出さない（窓をクリックすると出力が止まり、サーバごと固まる）

    def _json(self, code, obj):
        body = json.dumps(obj, ensure_ascii=False).encode('utf-8')
        self.send_response(code)
        self.send_header('Content-Type', 'application/json; charset=utf-8')
        self.send_header('Content-Length', str(len(body)))
        self.send_header('Cache-Control', 'no-store')
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        if self.path.split('?')[0] == '/title_layout':
            if os.path.exists(TITLE):
                with open(TITLE, encoding='utf-8') as f:
                    return self._json(200, json.load(f))
            return self._json(200, {'version': 1, 'layers': []})
        if self.path.split('?')[0] == '/layout':
            if os.path.exists(LAYOUT):
                with open(LAYOUT, encoding='utf-8') as f:
                    return self._json(200, json.load(f))
            return self._json(200, {'version': 1, 'elements': {}})
        return super().do_GET()

    def do_POST(self):
        if self.path.split('?')[0] == '/save_title':
            n = int(self.headers.get('Content-Length', 0))
            try:
                data = json.loads(self.rfile.read(n).decode('utf-8'))
                layers = data.get('layers')
                if not isinstance(layers, list):
                    raise ValueError('layers が無い')
            except Exception as e:
                return self._json(400, {'ok': False, 'error': str(e)})
            os.makedirs(os.path.dirname(TITLE), exist_ok=True)
            out = {'version': 1, 'layers': layers}
            if isinstance(data.get('petals'), dict):
                out['petals'] = data['petals']
            if isinstance(data.get('ui'), dict):
                out['ui'] = data['ui']
            if isinstance(data.get('guide'), dict):
                out['guide'] = data['guide']          # ビューアの補助線の設定（Unity は読まない）
            with open(TITLE, 'w', encoding='utf-8') as f:
                json.dump(out, f, ensure_ascii=False, indent=2)
            return self._json(200, {'ok': True, 'path': TITLE, 'count': len(layers)})
        if self.path.split('?')[0] == '/save_png':
            # ビューアの舞台を docs/art/<日付>/<name>.png に残す（見た目を変えたら 1 枚残す約束。本文は png のバイト列）
            import datetime, re, urllib.parse
            q = urllib.parse.parse_qs(self.path.split('?')[1] if '?' in self.path else '')
            name = re.sub(r'[^A-Za-z0-9_\-]', '', (q.get('name') or ['shot'])[0]) or 'shot'
            n = int(self.headers.get('Content-Length', 0)); data = self.rfile.read(n)
            d = os.path.join(ROOT, 'docs', 'art', datetime.date.today().isoformat()); os.makedirs(d, exist_ok=True)
            out = os.path.join(d, name + '.png')
            with open(out, 'wb') as f: f.write(data)
            return self._json(200, {'ok': True, 'path': os.path.relpath(out, ROOT)})
        if self.path.split('?')[0] == '/se_apply':
            n = int(self.headers.get('Content-Length', 0))
            try:
                data = json.loads(self.rfile.read(n).decode('utf-8'))
                if not isinstance(data.get('se'), dict) or not isinstance(data.get('bgm'), dict):
                    raise ValueError('se / bgm が無い')
            except Exception as e:
                return self._json(400, {'ok': False, 'error': str(e)})
            with open(os.path.join(HERE, 'se_choice.json'), 'w', encoding='utf-8') as f:
                json.dump(data, f, ensure_ascii=False, indent=1)
            r = subprocess.run([sys.executable, os.path.join(HERE, 'se_build.py')], cwd=ROOT, capture_output=True, text=True, encoding='utf-8', errors='replace')
            return self._json(200, {'ok': r.returncode == 0, 'log': (r.stdout + r.stderr).strip()})
        if self.path.split('?')[0] != '/save':
            return self._json(404, {'ok': False, 'error': 'unknown'})
        n = int(self.headers.get('Content-Length', 0))
        try:
            data = json.loads(self.rfile.read(n).decode('utf-8'))
            els = data.get('elements')
            if not isinstance(els, dict):
                raise ValueError('elements が無い')
            for k, v in els.items():
                for key in ('x', 'y', 'w', 'h'):
                    v[key] = round(float(v[key]), 1)
        except Exception as e:
            return self._json(400, {'ok': False, 'error': str(e)})
        os.makedirs(os.path.dirname(LAYOUT), exist_ok=True)
        with open(LAYOUT, 'w', encoding='utf-8') as f:
            json.dump({'version': 1, 'elements': els}, f, ensure_ascii=False, indent=2)
        return self._json(200, {'ok': True, 'path': LAYOUT, 'count': len(els)})


if __name__ == '__main__':
    print(f'http://localhost:{PORT}/ui_viewer.html  /title_viewer.html   保存先: {LAYOUT} / {TITLE}')
    ThreadingHTTPServer(('127.0.0.1', PORT), Handler).serve_forever()
