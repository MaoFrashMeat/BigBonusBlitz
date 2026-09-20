# -*- coding: utf-8 -*-
"""
主人公のボイスを 録る → 変換（VoiceChangerAI の RVC）→ 聞く → 場面のキーに入れる を 1 画面でやる小さなサーバ。

    py -3 tools/voice_studio.py            # http://localhost:8768/ を開く（Chrome / Edge。マイクの許可が要る）

- 録音はブラウザ（MediaRecorder）。届いた音を ffmpeg で 48kHz モノラル wav にして tools/voice/raw/take-<番号>.wav に置く
- 変換は ../VoiceChangerAI/training/convert.py（.venv-rvc の python）。出力は VoiceChangerAI/datasets/converted/
- 取り込みは tools/voice_import.py と同じ（無音を切って Resources/Audio/Voice/voice_<キー>.wav）
- 台本と場面のキーは docs/voice_script.md の表から読む
ffmpeg は VoiceChangerAI の同梱（training/.ffmpeg-bin）を使う。
"""
import io, json, os, re, subprocess, sys, threading, time, urllib.parse
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HERE = os.path.join(ROOT, "tools")
VC = os.path.join(os.path.dirname(ROOT), "VoiceChangerAI")
PY = os.path.join(VC, "training", ".venv-rvc", "Scripts", "python.exe")
CONVERT = os.path.join(VC, "training", "convert.py")
CONVERTED = os.path.join(VC, "datasets", "converted")
WEIGHTS = os.path.join(VC, "vendor", "rvc", "assets", "weights")
RAW = os.path.join(HERE, "voice", "raw")
VOICE = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Audio", "Voice")
SCRIPT = os.path.join(ROOT, "docs", "voice_script.md")
PORT = int(sys.argv[1]) if len(sys.argv) > 1 else 8768
sys.path.insert(0, HERE)
import voice_import   # noqa: E402  取り込みの処理（無音切り・meta）を使い回す


def ffmpeg():
    return voice_import.ffmpeg()


# いま何をしているか（UI が 0.5 秒ごとに聞きに来る）。step="" なら暇
PROG = {"step": "", "since": 0.0, "log": ""}
_lock = threading.Lock()


LOG = os.path.join(HERE, "voice", "studio.log")


def note(msg):
    """時間の記録はファイルへ（コンソールに書くと窓のクリックで止まる）。"""
    try:
        with io.open(LOG, "a", encoding="utf-8") as f: f.write(time.strftime("%H:%M:%S ") + msg + chr(10))
    except OSError:
        pass


def prog(step, log=None):
    with _lock:
        if step != PROG["step"]: PROG["since"] = time.time()
        PROG["step"] = step
        if log is not None: PROG["log"] = log


def script_keys():
    """docs/voice_script.md の表（キー | 場面 | セリフの例）を読む。"""
    keys = []
    try:
        for line in io.open(SCRIPT, encoding="utf-8"):
            m = re.match(r"\|\s*`(\w+)`\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|", line)
            if m: keys.append({"key": m.group(1), "scene": m.group(2), "lines": m.group(3)})
    except OSError:
        pass
    return keys


def existing_voices():
    out = {}
    if os.path.isdir(VOICE):
        for f in sorted(os.listdir(VOICE)):
            m = re.match(r"voice_([a-z]+)(?:_(\d+))?\.wav$", f)
            if m: out.setdefault(m.group(1), []).append("voice/" + f)
    return out


def takes():
    os.makedirs(RAW, exist_ok=True)
    raws = sorted((f for f in os.listdir(RAW) if f.endswith(".wav")), reverse=True)
    conv = {}
    if os.path.isdir(CONVERTED):
        for f in os.listdir(CONVERTED):
            m = re.match(r"(take-\d+)_pitch([+\-]\d+)(?:_.*)?\.wav$", f)
            if m: conv.setdefault(m.group(1), []).append({"file": "converted/" + f, "pitch": int(m.group(2))})
    return [{"id": os.path.splitext(f)[0], "raw": "raw/" + f, "converted": sorted(conv.get(os.path.splitext(f)[0], []), key=lambda x: x["pitch"])} for f in raws]


def models():
    if not os.path.isdir(WEIGHTS): return []
    return sorted(f for f in os.listdir(WEIGHTS) if f.endswith(".pth") and "_e" not in f) or sorted(f for f in os.listdir(WEIGHTS) if f.endswith(".pth"))


class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *a, **kw):
        super().__init__(*a, directory=HERE, **kw)

    def log_message(self, fmt, *args):
        pass   # 黒い窓に出さない。窓をクリックすると Windows が出力を止め、サーバごと固まる（クイック編集モード）

    def _json(self, code, obj):
        body = json.dumps(obj, ensure_ascii=False).encode("utf-8")
        self.send_response(code); self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body))); self.send_header("Cache-Control", "no-store"); self.end_headers(); self.wfile.write(body)

    def _file(self, path):
        if not os.path.isfile(path): self.send_response(404); self.end_headers(); return
        data = io.open(path, "rb").read()
        self.send_response(200); self.send_header("Content-Type", "audio/wav"); self.send_header("Content-Length", str(len(data)))
        self.send_header("Cache-Control", "no-store"); self.end_headers(); self.wfile.write(data)

    def do_GET(self):
        p = urllib.parse.unquote(self.path.split("?")[0])
        if p == "/": self.path = "/voice_studio.html"; return super().do_GET()
        if p == "/api/state":
            return self._json(200, {"keys": script_keys(), "voices": existing_voices(), "takes": takes(), "models": models(),
                                    "ready": {"ffmpeg": bool(ffmpeg()), "python": os.path.exists(PY), "convert": os.path.exists(CONVERT)}})
        if p == "/api/progress":
            with _lock: return self._json(200, {"step": PROG["step"], "seconds": round(time.time() - PROG["since"], 1) if PROG["step"] else 0, "log": PROG["log"]})
        if p.startswith("/audio/raw/"): return self._file(os.path.join(RAW, os.path.basename(p)))
        if p.startswith("/audio/converted/"): return self._file(os.path.join(CONVERTED, os.path.basename(p)))
        if p.startswith("/audio/voice/"): return self._file(os.path.join(VOICE, os.path.basename(p)))
        return super().do_GET()

    def do_POST(self):
        p = self.path.split("?")[0]
        n = int(self.headers.get("Content-Length", 0)); body = self.rfile.read(n)
        try:
            prog("受け取り", "")
            if p == "/api/record": return self._json(200, self.record(body))
            data = json.loads(body.decode("utf-8")) if n else {}
            if p == "/api/convert": return self._json(200, self.convert(data))
            if p == "/api/import": return self._json(200, self.do_import(data))
            if p == "/api/delete_take": return self._json(200, self.delete_take(data))
            return self._json(404, {"ok": False, "error": "unknown"})
        except Exception as e:
            return self._json(500, {"ok": False, "error": str(e)})
        finally:
            prog("")

    def record(self, blob):
        """ブラウザの録音（webm / ogg）を wav に。"""
        os.makedirs(RAW, exist_ok=True)
        stamp = int(time.time()); src = os.path.join(RAW, "take-%d.webm" % stamp); dst = os.path.join(RAW, "take-%d.wav" % stamp)
        io.open(src, "wb").write(blob)
        ff = ffmpeg()
        if not ff: raise RuntimeError("ffmpeg が無い（VoiceChangerAI/training/.ffmpeg-bin）")
        prog("wav に変換（ffmpeg）")
        t0 = time.time()
        r = subprocess.run([ff, "-hide_banner", "-loglevel", "error", "-y", "-i", src, "-ac", "1", "-ar", "48000", "-sample_fmt", "s16", dst], capture_output=True, text=True)
        os.remove(src)
        note("record: %d bytes -> wav %.1fs" % (len(blob), time.time() - t0))
        if r.returncode != 0: raise RuntimeError("wav にできない: " + r.stderr[-300:])
        return {"ok": True, "id": "take-%d" % stamp, "raw": "raw/take-%d.wav" % stamp}

    def convert(self, d):
        """convert.py を回す（数十秒）。出来た物の名前を返す。"""
        tid = re.sub(r"[^a-z0-9\-]", "", d.get("id", "")); pitch = int(d.get("pitch", 12)); model = os.path.basename(d.get("model") or "rika2.pth")
        src = os.path.join(RAW, tid + ".wav")
        if not os.path.exists(src): raise RuntimeError("録音が無い: " + tid)
        if not os.path.exists(PY): raise RuntimeError("VoiceChangerAI の python が無い: " + PY)
        cmd = [PY, CONVERT, "--input", src, "--model", model, "--pitch", str(pitch)]
        if d.get("indexRate") is not None: cmd += ["--index-rate", str(float(d["indexRate"]))]
        prog("声を変換（RVC。最初の 1 回はモデルの読み込みで長い）", "")
        t0 = time.time(); lines = []
        proc = subprocess.Popen(cmd, cwd=VC, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, encoding="utf-8", errors="replace")
        for line in proc.stdout:
            line = line.rstrip()
            if line: lines.append(line); prog(PROG["step"], line[-120:])
        proc.wait()
        note("convert: %s pitch %+d %.1fs" % (tid, pitch, time.time() - t0))
        out = "%s_pitch%+d.wav" % (tid, pitch)
        if not os.path.exists(os.path.join(CONVERTED, out)):
            raise RuntimeError("変換に失敗: " + chr(10).join(lines)[-600:])
        return {"ok": True, "file": "converted/" + out, "pitch": pitch}

    def do_import(self, d):
        """変換した物（か録音そのもの）を voice_<キー>.wav に。"""
        key = re.sub(r"[^a-z]", "", d.get("key", "")); f = d.get("file", "")
        if not key: raise RuntimeError("キーが無い")
        path = os.path.join(CONVERTED, os.path.basename(f)) if f.startswith("converted/") else os.path.join(RAW, os.path.basename(f))
        if not os.path.exists(path): raise RuntimeError("無い: " + f)
        prog("無音を切って Resources に写す")
        sys.argv = ["voice_import.py", path, key]
        buf = io.StringIO(); old = sys.stdout; sys.stdout = buf
        try: voice_import.main()
        finally: sys.stdout = old
        return {"ok": True, "log": buf.getvalue().strip(), "voices": existing_voices()}

    def delete_take(self, d):
        tid = re.sub(r"[^a-z0-9\-]", "", d.get("id", ""))
        p = os.path.join(RAW, tid + ".wav")
        if os.path.exists(p): os.remove(p)
        return {"ok": True}


if __name__ == "__main__":
    os.makedirs(RAW, exist_ok=True)
    print("voice studio: http://localhost:%d/  （Ctrl+C で止める）" % PORT)
    print("  ffmpeg:", ffmpeg() or "無い", "| convert:", "ok" if os.path.exists(PY) and os.path.exists(CONVERT) else "無い（VoiceChangerAI）")
    print("  この窓はクリックしない（クリックすると止まる。止まったら Enter で戻る）")
    try:
        srv = ThreadingHTTPServer(("127.0.0.1", PORT), Handler)
    except OSError:
        sys.exit("もう動いている（前の黒い窓が残っている）。そちらを閉じてからもう一度")
    srv.serve_forever()
