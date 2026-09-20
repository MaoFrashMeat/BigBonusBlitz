# -*- coding: utf-8 -*-
"""
録って変換した主人公のセリフを、ゲームの Resources/Audio/Voice/voice_<キー>.wav に取り込む。

    py -3 tools/voice_import.py <wav> <キー>          # 例: py -3 tools/voice_import.py ..\VoiceChangerAI\datasets\converted\take-123_pitch+12.wav attack
    py -3 tools/voice_import.py <wav> <キー> --keep   # 前後の無音を切らない

- 前後の無音（−40dB 以下）を切り、ピークを −1dB に揃える（ffmpeg。VoiceChangerAI の同梱を使う。無ければそのまま写す）
- 同じキーが既にあれば voice_<キー>_2, _3 … と増える（ゲームはランダムに 1 本）
- .meta は Audio/SE の wav のものを写して guid だけ新しく
キーの一覧は docs/voice_script.md。
"""
import io, os, shutil, subprocess, sys, uuid

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Audio", "Voice")
SE_DIR = os.path.join(ROOT, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Audio", "SE")
FFMPEG = [os.path.join(os.path.dirname(ROOT), "VoiceChangerAI", "training", ".ffmpeg-bin", "ffmpeg.exe"),
          os.path.join(os.path.dirname(ROOT), "VoiceChangerAI", "other", "RVC20260804Nvidia", "ffmpeg.exe"), "ffmpeg"]


def ffmpeg():
    for f in FFMPEG:
        if os.path.exists(f) or shutil.which(f): return f
    return None


def meta():
    cands = [f for f in os.listdir(SE_DIR) if f.endswith(".wav.meta")]
    s = io.open(os.path.join(SE_DIR, cands[0]), encoding="utf-8").read().splitlines()
    return "\n".join("guid: " + uuid.uuid4().hex if l.startswith("guid: ") else l for l in s) + "\n"


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if len(args) < 2: print(__doc__); sys.exit(1)
    src, key = args[0], args[1]
    if not os.path.exists(src): sys.exit("無い: " + src)
    os.makedirs(OUT, exist_ok=True)
    name = "voice_" + key; n = 2
    while os.path.exists(os.path.join(OUT, name + ".wav")): name = "voice_%s_%d" % (key, n); n += 1
    dst = os.path.join(OUT, name + ".wav")
    ff = ffmpeg()
    if ff and "--keep" not in sys.argv:
        # 前後の無音を切る（−40dB、0.05 秒）→ ピーク −1dB → 44.1kHz モノラル 16bit
        af = "silenceremove=start_periods=1:start_threshold=-40dB:start_silence=0.05,areverse,silenceremove=start_periods=1:start_threshold=-40dB:start_silence=0.05,areverse,alimiter=limit=0.89,volume=0dB"
        r = subprocess.run([ff, "-hide_banner", "-loglevel", "error", "-y", "-i", src, "-af", af, "-ac", "1", "-ar", "44100", "-sample_fmt", "s16", dst])
        if r.returncode != 0: print("ffmpeg が失敗。そのまま写す"); shutil.copyfile(src, dst)
    else:
        shutil.copyfile(src, dst)
    io.open(dst + ".meta", "w", encoding="utf-8", newline="\n").write(meta())
    if not os.path.exists(OUT + ".meta"):
        io.open(OUT + ".meta", "w", encoding="utf-8", newline="\n").write("fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % uuid.uuid4().hex)
    print("%s -> %s" % (src, os.path.relpath(dst, ROOT)))


if __name__ == "__main__":
    main()
