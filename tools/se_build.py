# -*- coding: utf-8 -*-
"""
効果音ビューア（tools/se_viewer.html）で決めた割り当てをゲームに入れる。

    py -3 tools/se_build.py            # tools/se_choice.json どおりに素材を Resources/Audio/SE へ写し、Resources/Data/se_config.json を書く

se_choice.json（ビューアの「JSON」を貼る）:
    { "se": { "<キー>": { "src": "assets/sounds/xxx.wav" | "resource:se_bet" | "synth" | "",
                          "volume": 0.6, "pitch": 1.0 } } }
- src が assets/… なら Resources/Audio/SE/se_<キー>.<拡張子> に写す（.meta は同じ拡張子の既存のものを写して guid だけ新しく）
- src が resource:<名前> なら今ある Resources の素材をそのまま使う（写さない）
- src が synth か空なら素材の指定なし（AudioManager の既定: 既定の素材 → 合成音）
- volume が -1 なら既定、pitch は 1 でそのまま
"""
import io, json, os, shutil, uuid

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CHOICE = os.path.join(ROOT, "tools", "se_choice.json")
SE_DIR = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Audio", "SE")
CONFIG = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Data", "se_config.json")
BGM_DIR = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Audio", "BGM")


def meta_for(ext):
    """同じ拡張子の既存 .meta（Audio/SE）を型にする（無ければ wav のもの）。guid は新しく。"""
    cands = [f for f in os.listdir(SE_DIR) if f.endswith(ext + ".meta")] or [f for f in os.listdir(SE_DIR) if f.endswith(".wav.meta")]
    src = io.open(os.path.join(SE_DIR, cands[0]), encoding="utf-8").read()
    lines = src.splitlines()
    for i, l in enumerate(lines):
        if l.startswith("guid: "): lines[i] = "guid: " + uuid.uuid4().hex
    return "\n".join(lines) + "\n"


def build_section(choice, section, dir_, prefix):
    """choice[section] を dir_ に写して設定を返す。prefix は写す名前の頭（se_ / bgm_）。motion_<cue> は se_motion_<cue>。"""
    out = {}
    for key, c in (choice.get(section) or {}).items():
        src = (c.get("src") or "").replace("\\", "/")
        entry = {"file": "", "volume": float(c.get("volume", -1)), "pitch": float(c.get("pitch", 1))}
        if src.startswith("resource:"):
            entry["file"] = src[len("resource:"):]
        elif src and src != "synth":
            path = os.path.join(ROOT, src)
            if not os.path.exists(path):
                # フォルダを整理して場所が変わった素材は、同じ名前を assets/sounds の下から探す
                found = [os.path.join(dp, f) for dp, _, fs in os.walk(os.path.join(ROOT, "assets", "sounds")) for f in fs if f == os.path.basename(src)]
                if not found: print("無い:", src); continue
                path = found[0]; print("  場所が変わっていた:", src, "->", os.path.relpath(path, ROOT))
            ext = os.path.splitext(path)[1].lower()
            name = ("se_" if key.startswith("motion_") else prefix) + key
            dst = os.path.join(dir_, name + ext)
            # 元からある素材（拡張子違いも）は上書きしない。同じ名前が別の拡張子であると Unity が 2 つ読むので _pick を付ける
            if any(os.path.exists(os.path.join(dir_, name + e)) and not is_built(os.path.join(dir_, name + e)) for e in (".wav", ".mp3", ".ogg")): name += "_pick"; dst = os.path.join(dir_, name + ext)
            # 同じ名前で別の拡張子が残っていると Unity が 2 つ読むので消す（自分が写した素材だけ。元からある物は触らない）
            for other in (".wav", ".mp3", ".ogg"):
                p = os.path.join(dir_, name + other)
                if other != ext and os.path.exists(p) and os.path.exists(p + ".meta") and is_built(p):
                    os.remove(p); os.remove(p + ".meta")
            shutil.copyfile(path, dst)
            if not os.path.exists(dst + ".meta"): io.open(dst + ".meta", "w", encoding="utf-8", newline="\n").write(meta_for(ext))
            mark_built(dst)
            entry["file"] = name
            print("%s: %s -> %s" % (key, src, os.path.relpath(dst, ROOT)))
        if entry["file"] == "" and entry["volume"] < 0 and abs(entry["pitch"] - 1) < 1e-6: continue   # 全部既定なら書かない
        out[key] = entry
    return out


def main():
    choice = json.load(io.open(CHOICE, encoding="utf-8"))
    se = build_section(choice, "se", SE_DIR, "se_")
    bgm = build_section(choice, "bgm", BGM_DIR, "bgm_")
    io.open(CONFIG, "w", encoding="utf-8", newline="\n").write(json.dumps({"se": se, "bgm": bgm}, ensure_ascii=False, indent=2) + "\n")
    print("%s: 効果音 %d 件, BGM %d 件" % (os.path.relpath(CONFIG, ROOT), len(se), len(bgm)))


BUILT = os.path.join(ROOT, "tools", "se", "built.txt")   # se_build.py が写した素材の一覧（消してよいのはこれだけ）


def built_set():
    try: return set(io.open(BUILT, encoding="utf-8").read().split())
    except OSError: return set()


def is_built(path): return os.path.basename(path) in built_set()


def mark_built(path):
    s = built_set(); s.add(os.path.basename(path))
    io.open(BUILT, "w", encoding="utf-8", newline="\n").write("\n".join(sorted(s)) + "\n")


if __name__ == "__main__":
    main()
