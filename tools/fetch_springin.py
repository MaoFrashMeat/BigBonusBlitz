# -*- coding: utf-8 -*-
"""
Springin' Sound Stock（https://www.springin.org/sound-stock/）の mp3 を assets/sounds/springin/<カテゴリ>/ に取ってくる。

    py -3 tools/fetch_springin.py            # 全カテゴリ（BGM 2 つは除く）
    py -3 tools/fetch_springin.py battle     # カテゴリを絞る
    py -3 tools/fetch_springin.py --bgm      # BGM も

取ったものは git に入れない（assets/sounds/springin は .gitignore）。規約で「素材そのものの再頒布」が禁止なので、
リポジトリで配らず、使う PC ごとにこのスクリプトで取る。ゲームに組み込んで配るのは規約で可（素材の再生が主でないもの）。
既にあるファイルは飛ばすので、途中で止めても続きから。1 本ごとに 0.3 秒空ける。
"""
import io, os, re, sys, time, html, urllib.request, urllib.parse

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "assets", "sounds", "springin")
BASE = "https://www.springin.org"
CATS = {"battle": "戦闘", "staging": "演出", "life": "生活", "machine": "機械・乗り物", "environment": "環境音",
        "system": "ボタン・システム", "nature-animals": "自然・動物", "retrogame": "レトロゲーム", "voice": "声"}
BGM = {"bgm": "BGM", "bgm-short": "10秒BGM"}
UA = {"User-Agent": "Mozilla/5.0 (BigBonusBlitz asset fetch)"}


def get(url, binary=False):
    # 日本語のファイル名はパーセントエンコードしないと http.client が落ちる
    p = urllib.parse.urlsplit(url)
    url = urllib.parse.urlunsplit((p.scheme, p.netloc, urllib.parse.quote(p.path), p.query, p.fragment))
    req = urllib.request.Request(url, headers=UA)
    data = urllib.request.urlopen(req, timeout=60).read()
    return data if binary else data.decode("utf-8", "replace")


def links(cat):
    """カテゴリの全ページから mp3 の URL を集める。"""
    urls, page = [], 1
    while True:
        u = "%s/sound-stock/category/%s/" % (BASE, cat) + ("page/%d/" % page if page > 1 else "")
        try: s = get(u)
        except urllib.error.HTTPError as e:
            if e.code == 404: break
            raise
        found = [html.unescape(m) for m in re.findall(r'https://www\.springin\.org/wp-content/uploads/[^"\'\s<>]+\.mp3', s)]
        if not found: break
        for f in found:
            if f not in urls: urls.append(f)
        pages = [int(x) for x in re.findall(r'/sound-stock/category/%s/page/(\d+)/' % re.escape(cat), s)]
        if page >= (max(pages) if pages else 1): break
        page += 1; time.sleep(0.3)
    return urls


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    cats = dict(CATS)
    if "--bgm" in sys.argv: cats.update(BGM)
    if args: cats = {k: v for k, v in cats.items() if k in args}
    total = 0
    for cat, jp in cats.items():
        urls = links(cat)
        d = os.path.join(OUT, cat); os.makedirs(d, exist_ok=True)
        got = 0
        for u in urls:
            name = urllib.parse.unquote(u.rsplit("/", 1)[-1])
            dst = os.path.join(d, name)
            if os.path.exists(dst) and os.path.getsize(dst) > 0: continue
            try:
                data = get(u, binary=True)
            except Exception as e:
                print("  取れない:", name, e); continue
            io.open(dst, "wb").write(data); got += 1; time.sleep(0.3)
        n = len([f for f in os.listdir(d) if f.endswith(".mp3")])
        total += n
        print("%s（%s）: %d 本（新しく %d）" % (jp, cat, n, got))
    print("合計 %d 本 → %s" % (total, os.path.relpath(OUT, ROOT)))


if __name__ == "__main__":
    main()
