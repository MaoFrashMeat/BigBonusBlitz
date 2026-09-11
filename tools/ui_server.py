# UI ビューア（tools/ui_viewer.html）を配信し、「保存」を Resources/Data/ui_layout.json に書く小さなサーバ。
#   python tools/ui_server.py            → http://localhost:8765/ui_viewer.html
#
# 保存されたファイルは Unity が次の Play で読む（UiLayout.cs）。ブラウザは直接ファイルに書けないので、
# この 1 本を挟む。GET は tools/ の中をそのまま返す。
import json
import os
import sys
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..'))
LAYOUT = os.path.join(ROOT, 'UnityProject', 'BigBonusBlitz', 'Assets', 'Resources', 'Data', 'ui_layout.json')
PORT = int(sys.argv[1]) if len(sys.argv) > 1 else 8765


class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *a, **kw):
        super().__init__(*a, directory=HERE, **kw)

    def log_message(self, fmt, *args):
        if '/save' in fmt % args or '/layout' in fmt % args:
            super().log_message(fmt, *args)

    def _json(self, code, obj):
        body = json.dumps(obj, ensure_ascii=False).encode('utf-8')
        self.send_response(code)
        self.send_header('Content-Type', 'application/json; charset=utf-8')
        self.send_header('Content-Length', str(len(body)))
        self.send_header('Cache-Control', 'no-store')
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        if self.path.split('?')[0] == '/layout':
            if os.path.exists(LAYOUT):
                with open(LAYOUT, encoding='utf-8') as f:
                    return self._json(200, json.load(f))
            return self._json(200, {'version': 1, 'elements': {}})
        return super().do_GET()

    def do_POST(self):
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
    print(f'http://localhost:{PORT}/ui_viewer.html   保存先: {LAYOUT}')
    ThreadingHTTPServer(('127.0.0.1', PORT), Handler).serve_forever()
