"""assets/symbols に置かれた日本語名のファイル（「ChatGPT Image 2026年9月14日 00_21_37 (1).png」など）を
<名前>_<番号>.png に付け替える。番号は同じ名前の既存ファイルの続きから。並びは作られた順（ファイル名の時刻 → 括弧の番号）。

    py -3 tools/symbol_rename.py bar            # assets/symbols 直下の日本語名を bar_N.png に
    py -3 tools/symbol_rename.py push button    # assets/symbols/button の日本語名を push_N.png に
    py -3 tools/symbol_rename.py --list         # 日本語名のファイルを数えるだけ
"""
import os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BASE = os.path.join(ROOT, "assets", "symbols")


def is_japanese_name(name):
    return re.search(r"[぀-ヿ一-鿿]", name) is not None


def sort_key(name):
    # 「ChatGPT Image 2026年9月14日 00_21_37 (3).png」→ (時刻, 括弧の番号)
    t = re.search(r"(\d{1,2})_(\d{2})_(\d{2})", name)
    n = re.search(r"\((\d+)\)", name)
    tk = tuple(int(x) for x in t.groups()) if t else (99, 99, 99)
    # 同じバッチの中では秒と括弧の番号がともに増えるので、時刻 → 括弧の番号 の順で並べれば作られた順になる
    return (tk[0], tk[1], tk[2], int(n.group(1)) if n else 0)


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    folder = os.path.join(BASE, args[1]) if len(args) > 1 else BASE
    files = sorted((f for f in os.listdir(folder) if is_japanese_name(f) and f.lower().endswith(".png")), key=sort_key)
    if "--list" in sys.argv or not args:
        print(f"{folder}: 日本語名 {len(files)} 件")
        for f in files: print("  " + f)
        return
    prefix = args[0]
    used = [int(m.group(1)) for f in os.listdir(folder) for m in [re.fullmatch(re.escape(prefix) + r"_(\d+)\.png", f)] if m]
    n = max(used) if used else 0
    for f in files:
        n += 1
        dst = f"{prefix}_{n}.png"
        os.rename(os.path.join(folder, f), os.path.join(folder, dst))
        print(f"{f} -> {dst}")


if __name__ == "__main__":
    main()
