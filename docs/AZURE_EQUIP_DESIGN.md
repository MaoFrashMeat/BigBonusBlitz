# 騎士の装備 — Azure equipment
更新: 2026-09-22 / Asia/Tokyo

## 本人の依頼
「装備の画面を改善したい」「１枚目の画像のようになっているので２枚目のようなかっこいいUIにしたい」「立ち絵もD:\\Mao-PC\\Github\\BigBonusBlitz\\assets\\characters\\saliaにあるしいろんな立ち絵を使いたい」「元気な子がいい」

参照: `assets/refarence/ChatGPT Image 2026年9月22日 03_00_48 (4).png`。暗い城塞・紺と金・左に大きなサリア・中央に装備一覧・右に比較と翼剣の紋章という構成を採用。画像内の数値、存在しないスキル/覚醒機能を仕様として追加しない。

## 実装と操作
- `Assets/Scripts/BBB.Runtime/AtelierEquip.cs` を改良。既存 EquipScreen の入口を維持。1170×540の安全領域とCanvas全体の背景。
- `AzureEquipSkin.cs` に画像・色・書体・金縁を集約。既存のAzureMapボタン素材、Noto Serif JPを再利用。
- `Assets/Resources/Art/UI/AzureEquip/` に城塞背景、翼剣の空欄パネル、文字用の詳細パネル、サリア5姿を格納。
- 立ち絵は左右ボタンで循環。装備の選択・ページとは独立し、勝手に時間切り替えしない。元PNGの縦横比と透過を保持。全身画像を大きく見せるため下半身は表示領域でトリミングし、目や剣への変形は掛けない。
- 装備/鞄件数、4行ページ、装備中フィルタ、着脱、前後効果、売却・まとめ売り確認、工房品の売却禁止、鞄満杯時の解除禁止、セーブ、効果音を維持。
- 街から開いた場合もステータス画面を開いて戻れる。冒険側が渡すステータス・呪いのコールバックも維持。
- 値と説明は生のUnity Text。空欄でも入手方法・探索装備と工房品の違いを表示。
- 小さな参考ボタンも44px以上にし、状態は色と文言で示す。情報面は不透明。

## 使用した立ち絵の対応
原本フォルダ: `assets/characters/salia/`。今回の作業で再生成・加工せずコピー。
| 実装素材 | 原本 | 意図 |
|---|---|---|
| salia-0.png | ChatGPT Image 2026年9月21日 21_55_27 (1).png | 笑顔で手を差し出す「一緒に行こう」 |
| salia-1.png | ChatGPT Image 2026年9月21日 21_55_28 (2).png | ウインクとピース |
| salia-2.png | ChatGPT Image 2026年9月21日 21_55_28 (3).png | 剣を肩に担ぐ「準備万端」 |
| salia-3.png | ChatGPT Image 2026年9月21日 22_01_11 (1).png | 前へ飛び出す元気なポーズ |
| salia-4.png | ChatGPT Image 2026年9月21日 22_01_12 (2).png | 手を上げて跳ぶポーズ |

いずれも1086×1448 RGBA。似た日時の複製があるため原本名で特定する。

## 検証
`Assets/Editor/AzureEquipValidation.cs`。隔離コピー `work/shop-qa`、コピー内のみ SaveData.Save 無効、実セーブ不使用。
`tools/azure-equip-qa/2026-09-22/` に版別PNGとvalidation.jsonを保存。
対象: 1280×720/2556×1179、空欄、5姿と循環、ページ・選択の保持、装備と解除、比較、フィルタ、通常売却とキャンセル、まとめ売りと工房/装備中の保護、満杯、長い名前、6桁残高、多数効果のスクロール、ステータスから戻る、コールバック。
実機タッチ、配布ビルド、聴感は未検証。

最終結果（2026-09-22）: `tools/azure-equip-qa/2026-09-22/v3-final/validation.json` は passed=true。14状態×2解像度=28枚を保存。5姿の循環、ページ保持、着脱・比較、通常/まとめ売りとキャンセル、工房品保護、満杯、多数効果スクロール、長い名前、6桁残高、ステータス復帰、コールバックを確認。Core/Runtime/Testsのコンパイル、`docs/check_layout.py` も成功。QAの数値は検証用の仮データでユーザーのセーブではない。v1/v2も修正の証拠として保持。

## 画像生成プロンプト
内蔵 image_gen で2点新規生成し、パネルの派生1点を編集生成。CLI/APIフォールバック未使用。ユーザー添付は構造・配色の参照。背景・パネルにゲームの文字や数値を焼き込まない。

### citadel.png
Use case: stylized-concept. Production background illustration for a luxury Japanese fantasy RPG equipment screen. Wide landscape 2.16:1. Moonlit gothic royal citadel courtyard, towering blue grey stone castle spires, deep navy embroidered banners with restrained golden winged sword heraldry, warm braziers at far bottom right, blue twilight clouds and stars above, atmospheric stone stairways and distant battlements. Beautiful finely painted anime fantasy game art, crisp magical romantic heroic mood. Main character and interface will be placed later: leave LEFT HALF without foreground objects, softly lit stone architecture to support a cheerful pink-haired knight portrait; center and right calm dark navy space behind interface. Detailed architecture concentrates on extreme edges, top and lower corners. NO characters, no portraits, no interface, no panels, no buttons, no text, no numbers, no logos, no watermark. Artwork only, full bleed.

### banner.png
Use case: stylized-concept. A SINGLE production fantasy RPG equipment panel sprite, portrait aspect 3:4, front-on orthographic with no perspective. Large rectangular dark navy blue shield-shaped panel, bottom center drops into a short elegant pointed tip. Intricate but delicate champagne-gold filigree double frame, art nouveau gold corner flourishes. Upper center contains one glowing gold winged SWORD crest encircled by thin celestial gold rings. Faint stone angel wings on left and right edges, symmetrical and subtle. The crest occupies top 50 percent; bottom 40 percent is empty quiet opaque midnight blue suitable for live text overlay. Premium high quality Japanese fantasy UI illustration, restrained luminous warm metal, elegant noble mood. Actual transparent alpha outside panel silhouette. Almost fill the canvas with 3 percent transparent margins. NO words, letters, numbers, captions, buttons, additional panels, or background scenery.

### detail.png（banner.pngを編集）
Edit this production UI panel sprite for its selected-equipment variant. Keep the exact outer gold frame, corner filigree, portrait proportions, navy colors, small bottom center tip, and genuine transparent alpha outside the panel. Remove the large central winged sword, celestial rings, rays, stars and statues from the INSIDE only. Replace the whole interior with calm opaque midnight navy silk texture and extremely subtle engraved feather motifs near the inner perimeter; central 80% must be quiet empty space for live equipment text. Preserve delicate gold frame continuously, especially bottom corners. No text, letters, numbers, items, buttons, characters or new background. Same dimensions and registration as input.

## 仕上げで得た知見
- 同じ人物でも手・剣の張り出しで安全な配置が違う。肩の剣は右へ、手を上げた姿は少し縮めて下へ移動。画像自体は変更しない。
- 空欄の紋章と装備選択時の文章には違う密度が必要。装飾を単色矩形で隠すと金枠も失われたため、共通の外枠を保持した詳細専用画像を生成した。
- RectTransform同士の非重複だけでは金の唐草への文字重なりを検出できない。実描画を見て詳細名を幅204、比較と効果の範囲を幅210へ狭めた。

## 続けるAIへ
依頼と配置はこのファイル、確率や効果はCore/JSON、実画面は版別QAを参照する。立ち絵変更で一覧ページや装備状態を初期化しない。原PNGを保持し、必要な調整は表示座標で行う。装備がない状態だけで完成とせず、実際の装備を入れた比較・売却・上限も検証する。失敗と修正をdevlog、note/MATERIAL、AI_HANDOFF_HISTORYへ残す。
