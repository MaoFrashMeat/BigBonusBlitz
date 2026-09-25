# 冒険背景：灯を継ぐ旅

## 2026-09-14：画風見直しの入口

ユーザーがv2の画風での進行を承認し、**C-1「霧の森」の3層を新画風へ変更・Unity接続済み**。C-1は左右に空白のある黒マットRGBの個別PNGを専用シェーダーで透過して繰り返し、連続した地面を別描画する。旧29地点は従来アトラスを使用する。

**Cloudy（曇り）を追加し全9天候**。雨なしの曇天を設定可能。屋内ではCloudyをClearへ変換する。C-1の4時間×3天候、正逆の周期境界、実GameControllerの2解像度を隔離Unityで確認。EditMode102件成功。確認画像と次の制作手順は [BACKGROUND_ART_DIRECTION.md](BACKGROUND_ART_DIRECTION.md)、再実行方法は [environment-qa](../tools/environment-qa/README.md)。QAはBuilt-inカメラのuGUI実描画で、本体のURP Play Mode・端末実機・配布ビルドは未検証。

以下は旧30地点背景の実装・過去の検証記録。現在のC-1と9天候については上記の更新を優先する。

## 反映したもの

第1章「灯を継ぐ」と第2章「分けた灯」の舞台、全30地点の専用背景を制作し、ゲーム本体へ接続しました。各PNGは遠景・中景・近景の3枚の透過イラストを収めたアトラスです。合計90のイラスト層に、Unityの空・天体・雲・霧・天候・局所光を重ねます。

歩行中は3層が別の速さで流れ、停止中も空と天候は動きます。ステージ変更は約1.4秒のクロスフェード。時間は朝から始まり、初期設定では12分で一周します。背景の外側も同じ時間・天候に連動します。

## シームレス化

2026-09-20更新：一律に16%の幅を透かして混ぜる方式から、各イラストの色・透明度を調べて接続経路を決める方式に変更しました。左右22%の重なりの中を、差が小さい位置に沿って接続します。実際に色を混ぜる幅は画像幅の2.4%に絞り、木・塔・柱が広い範囲で二重に透ける現象を抑えています。左右反転は使いません。

`AdventureSeamBaker` が元PNGを読み、3層それぞれの接続位置を動的計画法で求めます。結果は `Art/Adventure/Seams/<ID>.asset` の幅1pxの線形データテクスチャに保存します。このテクスチャは絵ではなく、各高さでの接続座標です。元のPNGは変更しません。実行時は追加のテクスチャ参照1回で座標を読み、狭い範囲を透過色込みで補間します。アトラスの上下端のフェザーとUIマスク対応は維持しています。

素材や行境界を変更したら、Unityメニューの **BBB → Adventure → Rebuild seamless joins** を実行して接続座標を更新してください。座標データがない背景は以前の16%補間にフォールバックします。前景の輪郭が大きく異なる素材では、原画側の描き直しが必要になる場合もあります。

中景の上端は表示域の上へ延ばし、アトラスの切り出し端に達する森の木や柱が画面途中で水平に切れないようにしています。

各イラストの行境界は透過率を測定して設定しています。元の画像を差し替える場合は `farEnd` / `middleEnd` も新しい画像に合わせて調整してください。

## Unityからの操作

Play中のHierarchyで `CharacterArea` を選び、`ParallaxBackground` のInspectorからステージ、Morning / Day / Evening / Night、天気を変更できます。`AutoCycle` で時間の自動進行、`DayLengthSeconds` で一周の長さを変更できます。`BgOuter` は主背景に同期するため、主背景側を操作してください。

```csharp
// ParallaxBackground.Create(area) で返る背景コンポーネントを保持して呼び出す。
background.SetStage("C-1");                         // 霧の森へ
background.SetTimeOfDay(AdventureTime.Evening, 3);  // 3秒で夕方へ
background.SetWeather(AdventureWeather.Rain, .7f, 2); // 強さ70%、2秒で雨へ
background.SetHour(23, 2);                          // 夜23時へ
background.AutoCycle = true;                       // 自動進行を再開
```

晴れ・雨・雷雨・雪・霧・火の粉・水滴・胞子の8種類を実装しています。屋内・地下に雨／雷雨／雪を指定した場合は水滴へ変換し、入場直後の天候遷移中にも屋外の雨が残らないようにしています。結晶洞や竜の巣では、ステージごとの光色を重ねます。

天候はuGUIの粒子・グラデーションメッシュによるUnity実装です。背景イラストに天候を焼き込んでいません。描画更新は30Hzを上限とし、ゲームの乱数や報酬処理には接続していません。

## ファイル構成

- `Assets/Resources/Art/Adventure/<ステージID>.png`：元イラスト30枚
- `Assets/Resources/Art/Adventure/AdventureSeamless.shader`：継ぎ目の補間・UIマスク対応
- `Assets/Resources/Art/Adventure/Seams/<ID>.asset`：90層分の接続座標（30ファイル）
- `AdventureSeamBaker.cs`：元絵から接続経路を再計算するEditorメニュー
- `Assets/Resources/Data/adventure_environments.json`：地点別設定とアトラスの行境界
- `ParallaxBackground.cs`：重なり順、スクロール、遷移、時間・天候API
- `AdventureAtmosphereGraphic.cs`：空・天体・雲・霧・天候
- `AdventureEnvironmentEditor.cs`：Inspector操作と取り込み設定
- `AdventureEnvironmentValidation.cs`：隔離Unityプロジェクトでの検証

## 地点別の設定

| ID | ステージ | 環境 | 基本天候 |
|---|---|---|---|
| A-1 | 草原の入口 | 屋外 | Clear |
| B-1 | 丘の道 | 屋外 | Clear |
| B-2 | 獣道 | 屋外 | Fog |
| C-1 | 霧の森 | 屋外 | Fog |
| C-2 | 古い橋 | 屋外 | Rain |
| C-3 | 沢沿い | 屋外 | Rain |
| C-4 | ぬかるみ | 屋外 | Rain |
| D-1 | 廃坑 | 地下・屋内 | Drips |
| D-2 | 石切場 | 屋外 | Clear |
| D-3 | 朽ちた社 | 屋外 | Fog |
| D-4 | 崖の細道 | 屋外 | Snow |
| D-5 | 苔の洞 | 地下・屋内 | Drips |
| D-6 | 枯れ谷 | 屋外 | Clear |
| E-1 | 地下水路 | 地下・屋内 | Drips |
| E-2 | 結晶の間 | 地下・屋内 | Clear |
| E-3 | 忘れられた書庫 | 地下・屋内 | Spores |
| E-4 | 鏡の広間 | 地下・屋内 | Fog |
| E-5 | 鐘楼 | 地下・屋内 | Clear |
| E-6 | 地熱の泉 | 地下・屋内 | Fog |
| E-7 | 虫の巣 | 地下・屋内 | Spores |
| E-8 | 底なし沼 | 屋外 | Fog |
| F-1 | 古代遺跡 | 屋外 | Clear |
| F-2 | 静寂の回廊 | 地下・屋内 | Spores |
| F-3 | 祭壇 | 地下・屋内 | Embers |
| F-4 | 風の抜け道 | 地下・屋内 | Spores |
| F-5 | 瓦礫の海 | 屋外 | Clear |
| G-1 | 竜の巣への道 | 屋外 | Embers |
| G-2 | 灼熱の岩場 | 屋外 | Embers |
| G-3 | 骨の道 | 屋外 | Embers |
| H-1 | 竜の巣 | 地下・屋内 | Embers |

## 検証

Unity 6000.3.15f1の隔離コピーで、全30背景の読み込み・実描画、4時間帯、8天候、屋内への遷移時の天候制約、キャラクターとの描画順、実際のGameControllerからのステージ追従を検証しています。ゲーム画面は1280×720と2556×1179で確認。ループ境界の直前・直後を描画して画素差を測定し、大きな飛びがないことを検証しています。

確認資料の画像と動画はUnityの実描画です。動画だけ時間の変化を早送りしています。HTMLはその確認画像を並べた閲覧用資料です。ユーザーのセーブデータを読み書きする検証はしていません。スマートフォン実機の性能確認と配布ビルドは未実施です。
