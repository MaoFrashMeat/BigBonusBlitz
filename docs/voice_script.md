# 主人公のボイス台本（VoiceChangerAI で録って変換する）

録り方（1 本 1 セリフ。1 つの take に 1 本だけ入れる）:

1. `D:\Mao-PC\Github\VoiceChangerAI\target\release\vc-app.exe` を開く → 「学習用の録音」の段 → **録音** → セリフを 1 本言う → **録音を停止**。`datasets/voice/take-<番号>.wav` ができる
2. 変換: `training\.venv-rvc\Scripts\python.exe training\convert.py --input datasets\voice\take-<番号>.wav --model rika2.pth --pitch 12`（男→女の目安。高すぎれば 8〜10）。`datasets/converted/` に出る
3. 取り込み: BBB で `py -3 tools/voice_import.py <変換した wav> <キー>`（無音を切って `Resources/Audio/Voice/voice_<キー>.wav` に写す。同じキーの 2 本目は `_2`）
4. ゲームは `voice_<キー>` があれば鳴らし、無ければ黙る。何本かあればランダム

叫びと痛がりは録るときに演技で決まる（変換は声質だけ）。同じキーは 2〜3 本録っておくと単調にならない。

| キー | 場面 | セリフの例（好きに変えてよい） |
|---|---|---|
| `start` | タイトルで TAP TO START | 「……行くか」「今日も、灯を持って」「さあ、始めよう」 |
| `attack` | チェリー / スイカで敵を斬る | 「くらえっ！！」「はあっ！」「そこだ！」 |
| `hit` | 押し順ナビ失敗の被弾 | 「いたっ……！！」「くっ……」「やられた……」 |
| `defeat` | 敵を倒した | 「やった！」「ふう……」「次！」 |
| `boss` | 中ボス出現 | 「……大きいな」「来たか」 |
| `bonus` | BIG / REG 確定 | 「きたっ！」「よし！」 |
| `at` | AT（洞窟）に入った | 「行くぞ」「洞窟か……」 |
| `perfect` | 技術介入で Perfect!! | 「決まった！」「完璧」 |
| `miss` | 技術介入に失敗 | 「……惜しい」「次だ」 |
| `levelup` | レベルアップ | 「強くなった気がする」 |
| `death` | 力尽きて街へ | 「灯が……」「……次は、選ぶ」 |
| `clear` | 章を踏破 | 「帰ろう」「持って帰ろう」 |

取り込んだ物は `docs/CREDITS.md` に書かなくてよい（自分の声）。
