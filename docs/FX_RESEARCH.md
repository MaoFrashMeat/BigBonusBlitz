# エフェクトの決まり（調査と数値）

調べた日: 2026-09-26。本人「ただ強くしただけ。見づらい。ノウハウとデータを集めて再現して」を受けて調べた。
見本（`tools/fx-lab`）と本体のエフェクトは、この決まりに合わせて作る。数値は 60fps のフレーム数（F）で書く。
「推測」は出典に数値がなく、私が換算や推論で決めた値。

## 前回（強めただけ）の失敗
- 光る物を全体で 1.7 倍、ブルームも強めた → 芯だけでなく本体まで光って、白い塊になった（出典 [26][28] の「加算とブルームだけで形が溶ける」そのもの）
- 背景のコントラストと彩度も上げた → 背景がエフェクトと同じ明るさ・鮮やかさの帯域に入り、エフェクトが負けた [10][12]
- 溜め・ヒットストップ・余韻がない → 急に出て急に消える [13]

## 1. 時間（溜め → 一撃 → 止め → 余韻）
| 段 | 長さ | 出典 |
|---|---|---|
| 予備（刃の出だしの細い光） | 2〜4F | 推測。Riot は全エフェクトに予備を置く [12][13] |
| 振り（刃が走る） | 4〜6F | 推測。アニメのスミアは 1〜2 コマ [15] |
| 当たりの白 | 敵は 1〜2F だけ白、その後 2F は被弾色 | Nuclear Throne [6] |
| ヒットストップ | 弱 8F / 中 12F / 強 15F / とどめ 20F 前後 | SFV 8/12/15F [1]、GBVS 10〜16F（カウンター最大 28F）[2] |
| 光の芯（閃光） | 6F（0.1 秒）。大きく出てすぐ縮む | Riot [10]、スケールは 0 から始めない [GEA] |
| 衝撃波 | 3F 遅れて出る。半径は一気に広がり後はゆっくり。太さは細→太→細 | [17]、遅延 0.05 秒 [GEA] |
| 火花 | 20〜40F。30 個・速さ 5〜15 | [GEA] |
| 余韻（細かい粒） | 一番長く残す。40〜60F | Riot「主グローはすぐ消し、粒が一番長く残る」[10] |
| 全体の尺 | 0.3〜0.5 秒＋ヒットストップ | Effekseer の斬撃ヒット 30F [11] |

- ヒットストップ中も、火花・閃光・画面の揺れは止めない。止めるのは刃とキャラだけ（格ゲーの作り）
- アニメ寄りは 2 コマ打ち（30fps）で絵を保持する。GGXrd も中割りなしのキーを保持している [16]
- 「長いと感じたら長すぎる」[13]

## 2. 形
- 斬撃は三日月。刃先の縁は硬く鋭く、両端は細く絞る [11][18][19]
- 3 段で塗る: 白い芯（細い）／飽和した本体色／暗い縁。アニメ塗りの 3 段と同じ考え方 [25]（段の割り当ては推測）
- 消し方は薄くするのではなく削る（アルファエロージョン）。形を保ってから崩す [20][RIME]
- 強弱の差は、火花の速さ・飛ぶ向き（貫通方向）・衝撃波の有無で付ける。属性違いに見えるほど足さない [23]

## 3. 明暗と色
- 光らせる（HDR 1 を超える）のは細い芯だけ。本体色は 1 未満にしてブルームに乗せない（推測。Cyanilux の強度 20 は丸ごと白く飛ぶ）
- ブルームは控えめ。閾値 1.0 以上、広がりは狭く、強度は「気づかない程度」 [26][27]
- 背景は一撃の間だけ暗く、色を抜く。背景の明度 0.5 → 0.2 にすると、グロー 0.5 → 0.8 でも十分強く読める [10]
- 明度・彩度 0% と 100% を広い面積に使わない [12]
- 色数は 2〜4 に絞る [CC2]

## 4. 画面の揺れ
- 揺れの量は trauma の 2 乗（0.3 → 9%、0.6 → 36%）。trauma は一撃ごとに足して、時間で直線的に減らす [29]
- 平行移動と回転を併用。乱数より滑らかなノイズ [29]
- 攻撃の向きに押す。横揺れのほうが疲れにくい [30]
- 1280×720 の振幅: 弱 3〜4px・中 6〜8px・強 12〜16px。6〜12F で減衰（推測。320×240 の 4〜6px を 4 倍）

## 4.5 斬撃の作り方（2 回目の調べ、2026-09-26）
- 形・中の筋・消え方を別の情報にする。筋はグレーで作り、色は後から段で付ける [Paulina][Venom][VFX Apprentice]
- 筋の作り方: 細い線を何十本も重ねる（開始・終了をばらし、端はぼかす）＋点を横方向にだけぶらす（Photoshop の「ぼかし（移動）」）。数値の出典はなく、本数・太さは描き出して決めた
- 筋は 2 枚を違う速さで流す（単調さが消える）。逆向きに流すと筋がその場に残って見える [Cyanilux][VFXDoc]
- 内側（尾側）は太い筋だけ残して「かすれ」にする。刃先の縁は硬く [jasontomlee][realtimevfx 4325]
- 消え方は削る。しきい値を [-幅, 1+幅] まで動かさないと消え切らない。尾と内側から先に欠けさせる [VFXDoc][torchinsky]
- 層: 本体＋2F 遅れて 1.5 倍長く残るかすれの層。全体の尺は 20〜30F [Effekseer][realtimevfx 27134]
- 失敗例: 太さが均一で尾が細くならない、HDR を上げすぎて色が消える、細い線がちらつく（画面で 2px 未満）
- 出典: cyanilux.com/tutorials/sword-slash-shader-breakdown/ ・realtimevfx.com/t/venom-slash-breakdown-of-the-effect-included/18903 ・paulinavfx.com/attack-vfx-02/ ・heyyocg.link/en/design-process-of-flame-slash/ ・vfxdoc.readthedocs.io（texcoord / alpha-erosion） ・jasontomlee.itch.io/slashfx（devlog 629732） ・realtimevfx.com/t/4325・27134・9407 ・godotshaders.com/shader/procedural-cyclic-slash/ ・unity-effect.com/368/

## 5. 見本への当てはめ
- 一撃の強さを 3 段（弱・強・とどめ）に決め、上の表の値を束ねて `Impact` 1 つで呼ぶ（ヒットストップ・敵の白・背景の暗転・揺れ）
- 背景の暗転は背景の絵そのものに掛ける（画面全体に掛けるとエフェクトまで暗くなる）
- 強いときだけ、白黒の衝撃コマを 1〜3 コマ（アニメの衝撃コマ [14]）

## 出典
[1] shoryuken.com/2016/06/07/hitstop-in-street-fighter-v-kens-not-so-little-secret/ ・[2] dustloop.com/w/GBVS/Mechanics ・[6] infovore.org/archives/2013/10/22/making-game-feel/ ・[10] 80.lv/articles/constructing-vfx-worthy-of-league-of-legends ・[11] effekseer.github.io/Help_Tool/ja/ToolTutorial/05.html ・[12] Riot VFX style guide（slideshare） ・[13] vfxapprentice.com/blog/10-league-of-legends-vfx-design-tips ・[14] brainvoyage.blog/impact-frames-meaning-animation-guide ・[15] blog.cg-wire.com/smear-frames/ ・[16] GGXrd GDC2015（arcsystemworks.com） ・[17] kyky.blog.jp/archives/41067430.html ・[18] unity-effect.com/368/ ・[19] optica.livedoor.blog/archives/11719287.html ・[20] torchinsky.me/stylized-vfx-unity-01/ ・[23] note.com/bbds_blog/n/n99e89912fafe ・[25] アニメ塗りの段数（bd_publishing） ・[26] learnopengl.com/Guest-Articles/2022/Phys.-Based-Bloom ・[27] Unity URP Bloom ・[28] 桜井政博「エフェクトを目立たせる」 ・[29] GDC2016 Eiserloh（archive.org） ・[30] davetech.co.uk/gamedevscreenshake
[GEA] unity-effect.com/245/ ・[RIME] simonschreibt.de/gat/stylized-vfx-in-rime/ ・[CC2] gamemakers.jp/article/2023_12_19_57404/ ・Cyanilux sword slash breakdown
