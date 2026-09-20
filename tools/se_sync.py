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
SPRINGIN_BGM = [os.path.join(ROOT, "assets", "sounds", "springin", d) for d in ("bgm", "bgm-short")] + [os.path.join(ROOT, "assets", "sounds", "maou", d) for d in ("bgm", "song")]
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


# 場面の語（ファイル名にこの語があればタグを付ける。日本語・英語どちらでも）
TAGS = [
    ("攻撃", ["剣", "斬", "slash", "sword", "攻撃", "attack", "打撃", "殴", "punch", "kick", "蹴", "hit", "会心", "一撃", "矢", "arrow", "銃", "gun", "shot"]),
    ("被弾", ["ダメージ", "damage", "痛", "くらう", "hurt"]),
    ("魔法", ["魔法", "magic", "呪文", "spell"]), ("炎", ["炎", "火", "fire", "flame", "burn"]), ("氷", ["氷", "ice", "freeze"]), ("雷", ["雷", "thunder", "電", "elec"]),
    ("水", ["水", "water", "泡", "bubble", "splash"]), ("風", ["風", "wind"]), ("闇", ["闇", "dark", "呪", "curse"]), ("光", ["光", "light", "holy", "聖"]),
    ("爆発", ["爆発", "explosion", "bomb", "爆"]), ("回復", ["回復", "heal", "cure"]), ("レベルアップ", ["レベルアップ", "levelup", "level_up", "level"]),
    ("決定", ["決定", "select", "ok", "click", "enter", "決"]), ("キャンセル", ["キャンセル", "cancel", "戻る", "back"]), ("カーソル", ["カーソル", "cursor", "move", "移動"]),
    ("ボタン", ["ボタン", "button", "system", "システム", "menu", "メニュー", "ピッ", "pi"]), ("警告", ["警告", "error", "エラー", "ブザー", "buzzer", "ng", "不正解"]),
    ("正解", ["正解", "correct", "成功", "success", "クリア", "clear"]), ("コイン", ["コイン", "coin", "お金", "money", "小銭", "チャリ", "gold"]),
    ("開く", ["宝箱", "chest", "open", "開", "扉", "door", "ドア"]), ("閉じる", ["閉", "close"]), ("足音", ["足音", "walk", "step", "歩"]), ("ジャンプ", ["ジャンプ", "jump"]),
    ("鐘", ["鐘", "bell", "チャイム", "chime", "鈴"]), ("歓声", ["歓声", "拍手", "cheer", "clap", "applause"]), ("敵の声", ["鳴き声", "monster", "モンスター", "クリーチャー", "creature", "animal", "獣", "唸", "吠"]),
    ("声", ["voice", "ボイス", "人", "human", "叫", "悲鳴", "scream", "笑"]), ("ジングル", ["ジングル", "jingle", "ファンファーレ", "fanfare", "drum", "ドラム", "ロール"]),
    ("スロット", ["スロット", "slot", "リール", "reel", "レバー", "コイン投入", "パチ"]), ("8bit", ["8bit", "レトロ", "retro", "ピコ"]), ("環境音", ["環境", "ambient", "雨", "rain", "波", "wave", "森", "forest", "鳥", "bird"]),
    ("機械", ["機械", "machine", "エンジン", "engine", "モーター", "motor", "ロボ", "robot", "車", "car"]), ("演出", ["演出", "キラ", "sparkle", "shine", "ワープ", "warp", "登場", "出現", "appear"]),
    ("ヒット", ["衝撃", "impact", "ガン", "ドン", "ズドン", "バーン", "壁にヒビ", "地響き", "落下", "骨折", "割れる", "つぶす", "叩く"]),
    ("戦闘", ["battle", "戦闘", "ショット", "振り回す", "気を溜める", "刀", "energy", "エネルギー", "発射"]),
    ("楽器", ["inst_", "guitar", "bass", "piano", "ピアノ", "グリッサンド", "マリンバ", "グロッケン", "チェレスタ", "ホイッスル", "スクラッチ", "ジャジャーン", "onepoint", "ワンポイント"]),
    ("生活", ["sound_", "カメラ", "スイッチ", "グラス", "掃除機", "カード", "タイピング", "料理", "ミキサー", "焼き", "缶", "ペットボトル", "ダイス", "証書", "電話", "phone", "pc", "elevator", "エレベーター", "鍵"]),
    ("動物", ["イヌ", "ネコ", "カラス", "犬", "猫", "鳥", "cat", "dog", "crow"]),
    ("上昇", ["上昇", "up", "rise"]), ("下降", ["下降", "down", "fall"]),
    ("警告", ["警報", "通信不良", "制限時間", "alarm", "siren"]), ("完了", ["完了", "complete", "done", "finish"]), ("選択", ["選択", "choose"]),
    ("演出", ["effect", "エフェクト", "グラビティ", "近未来", "クラッカー", "パフッ", "凍る", "ignition"]),
]


def tag_of(name):
    """ファイル名から シリーズ名（末尾の番号を除いた名前）と 場面のタグ を作る。"""
    import unicodedata
    base = os.path.splitext(os.path.basename(name))[0]
    series = re.sub(r"[_\- ]?(\d+|[０-９]+)$", "", base)          # 末尾の番号を落とす（会心の一撃1 → 会心の一撃、maou_se_magic_fire10 → maou_se_magic_fire）
    series = re.sub(r"^maou_(se|bgm|inst_short|inst|short)_?", "", series)   # 魔王魂の頭の印は落とす
    low = unicodedata.normalize("NFKC", base).lower()
    tags = [t for t, words in TAGS if any(w.lower() in low for w in words)]
    return series or base, tags


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
            dirs[:] = [x for x in dirs if x not in ("bgm", "song")]   # BGM と歌は効果音でない（BGM の候補に出す）
            for f in sorted(fs):
                if os.path.splitext(f)[1].lower() in EXT:
                    p = os.path.join(dp, f)
                    series, tags = tag_of(f)
                    candidates.append({"path": rel(p), "name": os.path.relpath(p, SOUNDS).replace("\\", "/"), "size": os.path.getsize(p), "series": series, "tags": tags})
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
                p = os.path.join(root, f); series, tags = tag_of(f)
                bgm_cands.append({"path": rel(p), "name": os.path.relpath(p, os.path.join(ROOT, "assets", "sounds")).replace("\\", "/"), "size": os.path.getsize(p), "series": series, "tags": tags})
    data = {"defs": defs, "candidates": candidates, "resources": [{"path": v, "name": k} for k, v in current.items()],
            "bgmDefs": bgmdefs, "bgmCandidates": bgm_cands, "bgmResources": [{"path": v, "name": k} for k, v in bgm_res.items()]}
    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write("// tools/se_sync.py が書く。se_viewer.html の一覧・候補・「既定に戻す」の値\n")
        f.write("window.BBB_SE = " + json.dumps(data, ensure_ascii=False) + ";\n")
    print("%s: 効果音 %d, 素材 %d, 合成 %d, 候補 %d | BGM %d, 候補 %d" % (os.path.relpath(OUT, ROOT), len(defs), len(current), sum(1 for d in defs if d["synthWav"]), len(candidates), len(bgmdefs), len(bgm_cands)))


if __name__ == "__main__":
    main()
