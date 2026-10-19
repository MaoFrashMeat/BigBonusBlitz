# 歩行から攻撃への表示切替を連続画像で確認

記録日: 2026-10-20（Asia/Tokyo）

## 観察

10月20日の隔離Unity `shop-qa` captureを、support-foot/歩行→攻撃切替に絞って目視した。120Hz記録は剣で0.3583秒時点に歩行pose18、0.3667秒時点に攻撃pose26へ切り替わる。rootの接近量は367.534から367.557で差0.023、contactはまだ0回。したがって、この隣接2画像で見える姿勢差は、同区間の大きな水平移動ではなく描画poseの切替による。

隣接フレーム: [pose18 / 0.3583秒](../../../tools/hero-gameplay-qa/2026-10-20/runtime-approach/w_blade-approach-handoff-042-1280.png)、[pose26 / 0.3667秒](../../../tools/hero-gameplay-qa/2026-10-20/runtime-approach/w_blade-approach-handoff-043-1280.png)。QAファイル名はStepに渡した0始まりframe番号で、画像時刻はStep後に進むため、画像番号はtrace時間から1小さい。従来のリンク（043/044）はどちらもpose26を示す誤りだった。今回のQA更新ではCSVにframe番号と画像名を併記し、18→26/33の変更時に実PNG内容も変わることをassertする。

## 判断範囲

これは現行の本編接続設定を使った隔離Unity画像の視覚確認であり、足の実接地・支持足の連続・完成美術を合格にするものではない。既存データは歩行pose18から攻撃pose26へ直接移り、中間poseは再生していないことを記録する。移動速度やcontact時刻だけを変更しても姿勢差は埋まらない。原画の中間案が必要か、既存poseの再接続で満たせるかは未決定。

現環境の利用可能ツールには内蔵画像生成機能が公開されていないため、新原画は作らず、CLI等での代替生成・編集もしていない。画像生成の手段が使えるまでは、この観察を制作判断や採用素材へ昇格させない。

回帰試験自体は[同日の記録](P2_RUNTIME_REGRESSION_REVIEW.md)のとおりPassed。今回の修正は隔離QAのframe↔capture対応記録と、歩行→攻撃時の実PNG変化assert、再実行先の一意化。ゲームランタイム/JSON/画像素材/抽選/HP/報酬/セーブは変更しない。本番セーブ不使用、コミット/push/公開なし。

## 2026-10-20 更新: 既存の攻撃準備poseでhandoffを3段にする（dev 401）

接近の最後の0.08秒だけ、選択武器の `attack-f2` 最終poseを表示する。新規画像は作らず、剣pose25 / 槍pose32を使い、歩行→準備pose→剣26/槍33へ進む。本編接近の合計時間/距離、攻撃f3-4内のswing/contactイベント、damage/抽選/報酬/音の発火元は変えない。準備pose表示中も主人公・敵のゲーム時計は進めず、新しいcueを発火しない。

`tools/hero-gameplay-qa/2026-10-20/runtime-approach-attempt07/` は機械検査Passed。120Hz traceでは剣/槍ともcapture032でwalk pose17、capture033からwind-up pose25/32、capture043でstrike pose26/33へ移る。最終接近位置は変更なし、capture時点のcontact=0。実BET EditMode 1 test、攻撃系列/戦闘強弱、17 sample、source manifestもPassed。実画像 `w_blade-approach-handoff-033-1280.png` と `w_spear-approach-handoff-033-1280.png` で準備姿勢を目視。attempt05の失敗は歩行18固定fixtureがhandoff変更後も残っていたため。attempt06は旧時間計算に依存するassertで接触までの追跡が短かった。fixtureを修正したattempt07で通過し、製品側動作の失敗と混同しない。

このコード/データ調整で一段切替の時間は改善したが、姿勢の骨格差・支持足・原画中割りの美術承認にはしない。実機/実入力/聴感/配布buildは未確認。画像生成機能が利用できないため新原画なし。保存領域不使用、commit/push/publishなし。
