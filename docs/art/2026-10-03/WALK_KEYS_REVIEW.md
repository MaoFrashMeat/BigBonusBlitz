# 左右の接地姿勢を先に分ける（2026-10-03 / dev 347）

## 今回の成果と採用境界

前回の8コマ生成が同じ脚運びを反復するため、接地2枚を先に制作した。内蔵画像生成4回の原本・プロンプト・SHA256・出力元は[素材台帳](walk-key-assets.json)に保存。生成したPNGはコピーのみで編集していない。

| 原本 | 判定 | 根拠 |
|---|---|---|
| walk-contact-pair-v1-rejected.png | 不採用 | 左右とも同じ手前の腿が右へ伸びる。接地2枚になっていない |
| walk-contact-pair-v2.png | 接地キー候補 | 右側の下半身だけを指示して描き直すと、手前の腿が左へ伸び、反対側の腿から右の靴へつながった。脚の所有関係を追える |
| walk-v7-rejected.png | 不採用 | 接地ペアから8コマを一括展開すると、後半の支持脚交替が失われ、passing/upも似た脚の形を反復する |
| walk-passing-pair-v1-review.png | 検品のみ | 片足支持・曲げた膝は出たが、左右で支持する脚の入替が明確でない。前後の重なりを変えただけでは合格にしない |

**本編採用はゼロ。** 本編の歩行はwalk-v3、剣の構えはdev346の前景手方式を維持。接地2枚が改善しても歩行ループや攻撃接続の完成ではない。現時点の接地候補は構え原画より細身にも見えるため、接続時の頭身検品が必要。

## 隔離Unityの描画

`AdventureHeroValidation.RunWalkKeyPoses`を追加した。キーを個別に静止表示する専用入口で、未完成の4枚から仮の歩行ループを作らない。shop-qaのパス・productName・batchmodeを要求。pose15だけをメモリ内で差し替え、finallyで復元し、本編JSON/PNGを変更しない。

- [v6-walk-keys](../../../tools/hero-gameplay-qa/2026-10-03/v6-walk-keys/keypose-validation.json): Unity6000.3.15f1、C-1、seed1003、20枚、exit0。4キー×剣/槍×2解像度=16枚と既存構え4枚。1280×720 / 2556×1179。
- key0/1は接地ペア左/右。key2/3はpassing試作左/右。剣では前景の手を表示し、槍では消す。描画中に次の歩行コマへ進まないことも検査。
- [座標](walk-keyposes-qa.json): 1774×887の原本からx=190/970、w=704、y=0、h=887を使用。alpha>64の足元を計測し、接地のfoot=31、passingのfoot=18。height=851固定で、コマごとに体を拡大しない。Grip=(406,362)、剣角度-55度、手矩形=(369,335,77,61)。これらは検品用で採用確定値ではない。
- 実描画を目視し、接地2姿勢の腿/膝/靴の違い、剣の顔への重なり回避、前景手、槍の表示を確認。槍は片手の携行候補で、両手動作の合格ではない。passingの支持脚、頭身差、足滑りと連続動作は未合格。
- Core/Runtime/Testsコンパイル成功。原本4件のSHA256が生成元と一致。本編HeroPresentation.csとhero_presentation_prototype.jsonはdev346検証時とSHA256一致。新しい実BET試験は行っておらず、dev346の2件Passedとは区別する。

## 再現と次の手順

1. 隔離shop-qaへ最新Editorソースを同期。contact-pair-v2をResources/Art/HeroPrototype/walk-contact-pair-qa.png、passing-pair-v1をwalk-passing-pair-qa.pngとしてコピー。本編Resourcesへ入れない。
2. `HERO_WALK_KEYS`にwalk-keyposes-qa.jsonの絶対パス、`HERO_GAMEPLAY_OUTPUT`に新しい出力先を設定し、`-batchmode -quit -executeMethod AdventureHeroValidation.RunWalkKeyPoses`で描画する。`-nographics`は使わない。
3. 次はpassingを**片側1枚ずつ**作る。手前の腿→膝→靴を追ってどちらが支持脚かを記録する。膝の前後関係だけ変えた似た絵を「反対脚」と扱わない。接地ペアの右側だけの下半身編集は有効だったため、この限定編集を使う。
4. 接地/通過の左右4キーが読めてからdown/upを補う。一括8枚の再試行を繰り返さない。フルサイクルが成立して初めて歩行→構え→接触→復帰の動画を撮り、実BET回帰後に本編採用を判断する。

今回届いた複数のheartbeatを独立した完成作業として数えない。この記録は実際に生成・描画した一連の検品。敵3種最終動作、20敵（元企画の通常16/ボス4との対応照合を含む）、全武器、光影、実機/聴感は未完了。本番セーブを使わず、コミット/push/公開なし。
