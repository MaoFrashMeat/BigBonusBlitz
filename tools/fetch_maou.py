# -*- coding: utf-8 -*-
"""
魔王魂（https://maou.audio/）のフリー素材を assets/sounds/maou/<se|bgm|song>/ に取ってくる（mp3 だけ）。

    py -3 tools/fetch_maou.py                # se / bgm / song 全部
    py -3 tools/fetch_maou.py se bgm         # 種類を絞る
    py -3 tools/fetch_maou.py --list         # 取らずに本数と目安の大きさだけ

規約（https://maou.audio/rule/）: 商用可・改変可、ゲーム組み込み可。ただし「音楽：魔王魂」の著作表記が要る。
曲単品の再配布は不可なので git には入れない（assets/sounds/maou は .gitignore）。使う PC ごとにこのスクリプトで取る。
既にあるファイルは飛ばすので、途中で止めても続きから。1 本ごとに 0.3 秒空ける。
"""
import io, os, re, sys, time, urllib.request, urllib.parse

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "assets", "sounds", "maou")
BASE = "https://maou.audio"
# 音のファイルはブラウザ以外に 403 を返すので、ページからの参照として取る（サイトの「ダウンロード」ボタンと同じ経路）
UA = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0 Safari/537.36", "Referer": BASE + "/"}
KINDS = ("se", "bgm", "song")


def get(url, binary=False, method="GET"):
    req = urllib.request.Request(url, headers=UA, method=method)
    r = urllib.request.urlopen(req, timeout=60)
    if method == "HEAD": return r.headers
    data = r.read()
    return data if binary else data.decode("utf-8", "replace")


def category_pages(kind):
    """カテゴリの全ページの HTML を順に返す。"""
    page = 1
    while True:
        u = "%s/category/%s/" % (BASE, kind) + ("page/%d/" % page if page > 1 else "")
        try: s = get(u)
        except urllib.error.HTTPError as e:
            if e.code == 404: return
            raise
        yield s
        pages = [int(x) for x in re.findall(r'/category/%s/page/(\d+)/' % kind, s)]
        if page >= (max(pages) if pages else 1): return
        page += 1; time.sleep(0.3)


def links(kind):
    """mp3 の URL。se / bgm は一覧に直に出る。song は曲ページを開いて拾う（歌あり・inst・short の全部）。"""
    urls = []
    for s in category_pages(kind):
        found = re.findall(r'https://maou\.audio/sound/%s/[A-Za-z0-9_\-]+\.mp3' % kind, s)
        if kind == "song" and not found:
            posts = sorted(set(re.findall(r'https://maou\.audio/([0-9][a-z0-9_]+)/', s)))
            for p in posts:
                try: ps = get("%s/%s/" % (BASE, p))
                except Exception as e: print("  曲ページを開けない:", p, e); continue
                found += re.findall(r'https://maou\.audio/sound/song/[A-Za-z0-9_\-]+\.mp3', ps)
                time.sleep(0.3)
        for f in found:
            if f not in urls: urls.append(f)
    return urls


def main():
    kinds = [a for a in sys.argv[1:] if a in KINDS] or list(KINDS)
    listing = "--list" in sys.argv
    total = 0
    for kind in kinds:
        urls = links(kind)
        if listing:
            sizes = []
            for u in urls[:5]:
                try: sizes.append(int(get(u, method="HEAD").get("Content-Length") or 0))
                except Exception: pass
            avg = sum(sizes) / len(sizes) if sizes else 0
            print("%s: %d 本、1 本あたり約 %.1f MB → 約 %.0f MB" % (kind, len(urls), avg / 1e6, avg * len(urls) / 1e6)); continue
        d = os.path.join(OUT, kind); os.makedirs(d, exist_ok=True)
        got = 0
        for u in urls:
            dst = os.path.join(d, u.rsplit("/", 1)[-1])
            if os.path.exists(dst) and os.path.getsize(dst) > 0: continue
            try: data = get(u, binary=True)
            except Exception as e: print("  取れない:", u, e); continue
            io.open(dst, "wb").write(data); got += 1; time.sleep(0.3)
        n = len([f for f in os.listdir(d) if f.endswith(".mp3")]); total += n
        print("%s: %d 本（新しく %d）" % (kind, n, got))
    if not listing: print("合計 %d 本 → %s（著作表記: 音楽：魔王魂）" % (total, os.path.relpath(OUT, ROOT)))


if __name__ == "__main__":
    main()
