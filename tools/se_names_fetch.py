# -*- coding: utf-8 -*-
"""
魔王魂の素材の日本語名をサイトから拾って tools/se_names.json に書く（ビューアの表示名。ファイル名は変えない）。

    py -3 tools/se_names_fetch.py

一覧ページの「ダウンロード」ボタンに download="魔王魂 人間02.wav" のように日本語名が入っている。
tools/se_names_manual.json（手で書く: ファイル名 → 日本語名）があれば、それを優先して合わせる。
"""
import io, json, os, re, time, urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "tools", "se_names.json")
MANUAL = os.path.join(ROOT, "tools", "se_names_manual.json")
BASE = "https://maou.audio"
UA = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0 Safari/537.36", "Referer": BASE + "/"}


def get(url):
    return urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=60).read().decode("utf-8", "replace")


def pages(kind):
    page = 1
    while True:
        u = "%s/category/%s/" % (BASE, kind) + ("page/%d/" % page if page > 1 else "")
        try: s = get(u)
        except urllib.error.HTTPError as e:
            if e.code == 404: return
            raise
        yield s
        ps = [int(x) for x in re.findall(r'/category/%s/page/(\d+)/' % kind, s)]
        if page >= (max(ps) if ps else 1): return
        page += 1; time.sleep(0.3)


def harvest(s, names):
    """download="魔王魂 <名前>.<ext>" と href の対応を拾う。説明は「作曲：」の後の 1 文。"""
    s = re.sub(r"<script.*?</script>|<style.*?</style>", "", s, flags=re.S)
    for m in re.finditer(r'href="https://maou\.audio/sound/[a-z]+/([A-Za-z0-9_\-]+)\.(?:mp3|ogg|wav|m4a)"[^>]*download="魔王魂 ([^"]+?)\.(?:mp3|ogg|wav|m4a)"', s):
        file, name = m.group(1), m.group(2).strip()
        names[file] = re.sub(r"^効果音\s*", "", name)


def main():
    names = {}
    for kind in ("se", "bgm", "song"):
        n0 = len(names)
        for s in pages(kind):
            harvest(s, names)
            if kind == "song":   # 歌は曲ページの中に inst / short の別版がある
                for p in sorted(set(re.findall(r'https://maou\.audio/([0-9][a-z0-9_]+)/', s))):
                    try: harvest(get("%s/%s/" % (BASE, p)), names)
                    except Exception as e: print("  曲ページ:", p, e)
                    time.sleep(0.3)
        print("%s: %d 件" % (kind, len(names) - n0))
    if os.path.exists(MANUAL):
        names.update(json.load(io.open(MANUAL, encoding="utf-8")))
    io.open(OUT, "w", encoding="utf-8", newline="\n").write(json.dumps(names, ensure_ascii=False, indent=1, sort_keys=True) + "\n")
    print("%s: %d 件" % (os.path.relpath(OUT, ROOT), len(names)))


if __name__ == "__main__":
    main()
