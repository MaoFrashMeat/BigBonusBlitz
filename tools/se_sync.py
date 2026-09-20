# -*- coding: utf-8 -*-
"""
効果音ビューア（tools/se_viewer.html）の一覧と候補を tools/se_defaults.js に書く。

    py -3 tools/se_sync.py

読むもの:
- 効果音の一覧: AudioManager.cs の SeDefs（キー / 既定の素材名 / 音量 / 合成音の有無 / 場面の説明）
- 今の設定: Resources/Data/se_config.json（se_build.py が書く）
- 今の素材: Resources/Audio/SE/*.wav|mp3|ogg
- 合成音の試聴用 wav: tools/se/synth/<key>.wav（Unity バッチの SfxProbe.ExportSynth が書く。合成音を変えたら出し直す）
- 候補: assets/sounds/**/*.wav|mp3|ogg（自分で拾ってきた素材はここに置く）
ビューアは file:// でも読めるように JSON でなく JS（window.BBB_SE）で持つ。
"""
import io, json, os, re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
AUDIO = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Scripts", "BBB.Runtime", "AudioManager.cs")
MOTION = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Scripts", "BBB.Runtime", "AudioManager.Motion.cs")
BGM_DIR = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Audio", "BGM")
BGM_SOUNDS = os.path.join(ROOT, "assets", "sounds", "bgm")
SPRINGIN_BGM = [os.path.join(ROOT, "assets", "sounds", "springin", d) for d in ("bgm", "bgm-short")]
CONFIG = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Data", "se_config.json")
SE_DIR = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Audio", "SE")
SYNTH_DIR = os.path.join(ROOT, "tools", "se", "synth")
SOUNDS = os.path.join(ROOT, "assets", "sounds")
OUT = os.path.join(ROOT, "tools", "se_defaults.js")
EXT = (".wav", ".mp3", ".ogg")
# 場面ごとの絵（tools/se/pics。描き出しの切り抜き）
PICS = {"レバー・停止": "se/pics/reels.png", "役": "se/pics/reels.png", "払い出し": "se/pics/display.png", "ボーナス": "se/pics/display.png",
        "エンゲージ": "se/pics/area.png", "拾い物・GET": "se/pics/gain.png", "技術介入": "se/pics/reels.png", "その他": "se/pics/trophy.png",
        "UI の動き": "se/pics/ui.png"}
BGM_PICS = {"title": "se/pics/title.png", "town": "se/pics/town.png", "adventure": "se/pics/area.png", "bonus": "se/pics/display.png", "at": "se/pics/area.png", "engage": "se/pics/area.png"}


def read_defs():
    """SeDefs（効果音）と BgmDefs（BGM）を AudioManager.cs から、MotionDefs（UI の動き）を AudioManager.Motion.cs から読む。"""
    src = io.open(AUDIO, encoding="utf-8").read()
    pat = re.compile(r'new SeDef\("([^"]+)",\s*"([^"]*)",\s*([0-9.]+)f,\s*(null|\(\)\s*=>[^,]+),\s*"([^"]*)",\s*"([^"]*)",\s*"([^"]*)"\)')
    i_bgm = src.index("BgmDefs =")
    se, bgm = [], []
    for m in pat.finditer(src):
        key, file, vol, synth, group, name, when = m.groups()
        d = {"key": key, "file": file, "volume": float(vol), "synth": synth != "null", "group": group, "name": name, "when": when}
        (bgm if m.start() > i_bgm else se).append(d)
    if not se or not bgm: raise SystemExit("AudioManager.cs の SeDefs / BgmDefs を読めない")
    msrc = io.open(MOTION, encoding="utf-8").read()
    motion = [{"key": "motion_" + c.lower(), "file": "se_motion_" + c.lower(), "volume": 1.0, "synth": True, "group": "UI の動き", "name": n, "when": w + "（音量は倍率）", "cue": c}
              for c, n, w in re.findall(r'\(MotionCue\.(\w+),\s*"([^"]*)",\s*"([^"]*)"\)', msrc)]
    return se + motion, bgm


def rel(path):
    return os.path.relpath(path, os.path.join(ROOT, "tools")).replace("\\", "/")


def main():
    defs, bgmdefs = read_defs()
    try:
        config = json.load(io.open(CONFIG, encoding="utf-8-sig")).get("se", {})
    except (OSError, ValueError):
        config = {}
    current = {}
    for f in sorted(os.listdir(SE_DIR)) if os.path.isdir(SE_DIR) else []:
        base, ext = os.path.splitext(f)
        if ext.lower() in EXT: current[base] = rel(os.path.join(SE_DIR, f))
    for d in defs:
        st = config.get(d["key"]) or {}
        d["setting"] = {"file": st.get("file", ""), "volume": st.get("volume", -1), "pitch": st.get("pitch", 1)}
        d["current"] = current.get(st.get("file") or d["file"])          # 今 Resources にある素材（無ければ null）
        d["currentFile"] = (st.get("file") or d["file"]) if d["current"] else ""
        sp = os.path.join(SYNTH_DIR, ("enemy_appear_whoosh" if d["key"] == "enemy_appear" else d["key"]) + ".wav")   # 敵出現の合成は 接近 + 着地 の 2 本。試聴は接近だけ
        d["synthWav"] = rel(sp) if os.path.exists(sp) else None
        d["pic"] = PICS.get(d["group"], "se/pics/area.png")
        d["composite"] = d["key"] in ("navi_success", "escape")   # 素材が無いときは他の音の組み合わせ（リプレイ音を高く + コイン 等）
    candidates = []
    if os.path.isdir(SOUNDS):
        for dp, dirs, fs in os.walk(SOUNDS):
            dirs[:] = [x for x in dirs if x != "bgm"]   # BGM は効果音でない
            for f in sorted(fs):
                if os.path.splitext(f)[1].lower() in EXT:
                    p = os.path.join(dp, f)
                    candidates.append({"path": rel(p), "name": os.path.relpath(p, SOUNDS).replace("\\", "/"), "size": os.path.getsize(p)})
    # BGM: 今の素材（Resources/Audio/BGM）と候補（assets/sounds/bgm、springin の bgm）
    bgm_cfg = {}
    try: bgm_cfg = json.load(io.open(CONFIG, encoding="utf-8-sig")).get("bgm", {})
    except (OSError, ValueError): pass
    bgm_res = {}
    for f in sorted(os.listdir(BGM_DIR)) if os.path.isdir(BGM_DIR) else []:
        base, ext = os.path.splitext(f)
        if ext.lower() in EXT: bgm_res[base] = rel(os.path.join(BGM_DIR, f))
    for d in bgmdefs:
        st = bgm_cfg.get(d["key"]) or {}
        d["setting"] = {"file": st.get("file", ""), "volume": st.get("volume", -1), "pitch": 1}
        d["current"] = bgm_res.get(st.get("file") or d["file"]); d["currentFile"] = (st.get("file") or d["file"]) if d["current"] else ""
        d["synthWav"] = None; d["pic"] = BGM_PICS.get(d["key"], "se/pics/area.png"); d["bgm"] = True
    bgm_cands = []
    for root in [BGM_SOUNDS] + SPRINGIN_BGM:
        if not os.path.isdir(root): continue
        for f in sorted(os.listdir(root)):
            if os.path.splitext(f)[1].lower() in EXT:
                p = os.path.join(root, f); bgm_cands.append({"path": rel(p), "name": os.path.relpath(p, os.path.join(ROOT, "assets", "sounds")).replace("\\", "/"), "size": os.path.getsize(p)})
    data = {"defs": defs, "candidates": candidates, "resources": [{"path": v, "name": k} for k, v in current.items()],
            "bgmDefs": bgmdefs, "bgmCandidates": bgm_cands, "bgmResources": [{"path": v, "name": k} for k, v in bgm_res.items()]}
    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write("// tools/se_sync.py が書く。se_viewer.html の一覧・候補・「既定に戻す」の値\n")
        f.write("window.BBB_SE = " + json.dumps(data, ensure_ascii=False) + ";\n")
    print("%s: 効果音 %d, 素材 %d, 合成 %d, 候補 %d | BGM %d, 候補 %d" % (os.path.relpath(OUT, ROOT), len(defs), len(current), sum(1 for d in defs if d["synthWav"]), len(candidates), len(bgmdefs), len(bgm_cands)))


if __name__ == "__main__":
    main()
