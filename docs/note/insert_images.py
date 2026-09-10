# -*- coding: utf-8 -*-
"""記事本体に画像の差し込み位置を入れる。1回だけ実行する（実行済みなら何もしない）。"""
import io
import os

P = os.path.join(os.path.dirname(os.path.abspath(__file__)), "note-01-ai-game-making.md")


def img(name, cap):
    return "![%s](images/%s)\n*%s*\n" % (cap, name, cap)


# (この文字列の直前に画像を入れる, 画像ファイル名, キャプション)
BEFORE = [
    ("私はコードを書いていません。\n絵も描いていません。",
     "06-timeline.png", "105日のうち、約2ヶ月は止まっていた。仕組みを入れる前の3ヶ月と、入れたあとの4日"),
    ("## \"決定の蒸発\"",
     "01-title-screen.png", "8月17日の時点でできていたもの。ブラウザ版のタイトル画面（2026年6月）"),
    ("## 具体的に、何が起きたか",
     "07-before-after.png", "決定を会話に置くか、ファイルに置くか。AIは毎回、初対面になる"),
    ("## 効かなかった書き方",
     "02-settings-editor.png", "確率を全部この画面に出した。私はコードを読まずに数字を変えられる"),
    ("## 実例:3回失敗してからルールになったもの",
     "08-memory-promotion.png", "観測1回はメモ、2回で書庫、3回でルール。1回のまぐれをルールにしない"),
    ("## 私が実際にやっている検証",
     "09-acceptance-flow.png", "頼む前に完成条件、終わったら証拠、最後に別のAIが突き合わせる"),
    ("## 型A:動くものは作るが、壊れないものは作らない",
     "10-failure-types.png", "14件を原因で分けると4つになった。型Dだけは、公開したあとだと直せない"),
    ("## アイコンは、画像を使わずに描いた",
     "04-hero-9poses.png", "主人公の9ポーズ。すべてAIが描いた"),
]

# (この文字列の直後に画像を入れる, 画像ファイル名, キャプション)
AFTER = [
    ("新しい抽選を足すときは、必ず対応するテーブルとエディタのタブも作る。\n```\n",
     "03-workflow-editor.png", "役ごとに、どの演出が何%で出るかを並べた画面。数字はここだけで動く"),
    ("この2つで、9ポーズ分が使える精度になりました。\n",
     "05-pose-to-sprite.png", "基準の1枚と姿勢の指定を毎回セットで渡す。顔と服のブレが減った"),
]


def main():
    s = io.open(P, encoding="utf-8").read()
    if "](images/" in s:
        print("すでに差し込み済み。何もしない")
        return
    n = 0
    for anchor, name, cap in BEFORE:
        if anchor not in s:
            print("MISS(before):", anchor[:28].replace("\n", " "))
            continue
        s = s.replace(anchor, img(name, cap) + "\n" + anchor, 1)
        n += 1
    for anchor, name, cap in AFTER:
        if anchor not in s:
            print("MISS(after):", anchor[:28].replace("\n", " "))
            continue
        s = s.replace(anchor, anchor + "\n" + img(name, cap), 1)
        n += 1
    io.open(P, "w", encoding="utf-8").write(s)
    print("差し込み %d / %d 件" % (n, len(BEFORE) + len(AFTER)))


if __name__ == "__main__":
    main()
