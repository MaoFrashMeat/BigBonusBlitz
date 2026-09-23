# 星詠みの聖域 — ステータス強化画面
更新: 2026-09-22 / Asia/Tokyo

## 依頼と採用方針
本人の原文: 「ステータス強化の画面を画像のように改良したい」「背景は画像のようなものではなく宇宙をイメージしたものでお願いします。」
参照: `assets/refarence/ChatGPT Image 2026年9月21日 21_07_53 (1).png`。金装飾、白い中央パネル、ライフ・テクニック・ラックの三列、紋章、強化前後の表示を採用。参照内の数値・価格は固定せず、既存の StatsDirector と game_config.json を使う。

## 実装
- `Assets/Scripts/BBB.Runtime/StatsScreen.cs`: ショップと冒険中で共用。1170×540の安全領域。背景はCanvas全体へ拡張。
- `CelestialStatsSkin.cs`: 宇宙背景、透過金枠、3紋章を読込。原PNGを変更せず、アトラスはSprite領域で切り分ける。
- `Assets/Editor/CelestialStatsTextureImporter.cs`: sRGB、Clamp、mipmapなし、最大4096、アルファ保持。
- `Assets/Resources/Art/UI/CelestialStats/`: cosmos.png、card.png、emblems.png。内蔵 image_gen で新規生成（CLI未使用）。
- 説明面は不透明、可変数値と説明はUnity Text。色だけに頼らず、名前・アイコン・ボタン文言で状態を伝える。
- 加算時は1ポイント消費、既存の保存・サウンド・呼び元更新を維持。最大値とポイント無しでボタンを無効にする。
- 振り直しは費用を確認してから実行。確認時に残高・割当数・費用を再確認。キャンセルで変更しない。章クリア後の無料条件は既存ロジックを維持。
- 戻る・閉じるは呼び元へ戻る。添付の「冒険へ」はショップからも開く共用画面なので「戻る」と表記。

## 検証
`Assets/Editor/CelestialStatsValidation.cs` を隔離コピー `work/shop-qa` で実行。ユーザーのセーブを読まず、コピー内のみ SaveData.Save を無効化。出力: `tools/celestial-stats-qa/2026-09-22/`。
対象: 1280×720、2556×1179、3能力加算、ポイント不足、上限、6桁残高、有料/無料振り直し、キャンセル、確認時残高変更、戻る、ショップ入口/復帰、文字重なりと切れ。
最終結果: `tools/celestial-stats-qa/2026-09-22/v8-final/validation.json` は passed=true。9状態×2解像度の18枚を保存。Core/Runtime/Tests のコンパイル成功、docs/check_layout.py は重なり・はみ出しなし。端末でのタッチ、配布ビルド、聴感は未検証。

### 改善中に見つけたこと
- v1: 日本語明朝の行高不足で見出しが描かれなかった。表示枠の高さを拡張。
- v2: 上部見出しとタイトルが3px重複。間隔を再配分。
- v3: 操作テスト通過。目視で、単色の内面が金装飾を隠す点と効果の数値位置が不揃いな点を修正。
- v4: 独自GraphicにCanvasRendererがなく描画不可。RequireComponentで依存を明示。
- v5〜7: 内面を羽根模様が残る静的な色のぼかしへ変更。効果の項目と値を分割し、金枠の内側へ余白を確保。確定時に状態が変わった場合も残高表示を更新。
- v8-final: 要約を意味の切れ目で2行にし、全操作・2解像度・文字検査を再実行。

## 生成プロンプト（内蔵 image_gen）
### cosmos.png
Use case: stylized-concept. Production background illustration for a luxurious Japanese fantasy RPG stat upgrade screen. ONLY a full bleed cosmic environment, no interface. Wide landscape 2.16:1 aspect ratio. Deep midnight navy and ultramarine space with luminous sapphire, cyan and restrained violet nebulae, countless delicate stars, fine celestial gold constellation lines and orbital arcs mostly at the outer edges. A beautiful crescent planet at the upper right, glowing stardust along the left and lower edges, a distant ethereal galaxy arc. Painterly polished anime fantasy art, magnificent celestial mood, detailed but calm dark central area where live UI will be overlaid. No buildings, no terraces, no characters, no flowers, no text, no typography, no cards, no borders, no buttons, no logos. Rich spatial depth and elegant controlled luminosity. Artwork only.
### card.png
Use case: stylized-concept. One production fantasy JRPG UI card sprite, portrait aspect 4:5, centered front-on orthographic rectangle, no perspective. Luxurious intricate polished gold metal double border with sharp elegant filigree corner flourishes, small blue sapphire at bottom center. Border occupies outer 7 percent. Opaque midnight navy blue interior with barely visible engraved wing and celestial motifs, quiet smooth deep dark fill so white typography can be overlaid. An empty circular GOLD medallion socket seamlessly attached at top center (diameter 22 percent of card width), navy interior, no icon inside it. Genuine transparent alpha outside the silhouette, tightly framed with 3 percent margin. Premium hand painted Japanese fantasy game interface art, delicate crisp highlights. SINGLE CARD only. No text, letters, numbers, symbols in the medallion, buttons, objects, illustrations or other UI. Preserve very large empty interior.
### emblems.png
Use case: stylized-concept. Production fantasy RPG ability icons atlas, wide 3:1 canvas divided into THREE EQUAL SQUARE CELLS in ONE horizontal row, no visible grid. Left cell: glowing emerald green heart of faceted crystal. Center cell: two crossed elegant crimson luminous swords with gold hilts. Right cell: glowing golden four leaf clover. Each icon centered within central 65 percent of its cell, same visual scale. All three encircled by a delicate polished antique gold round medallion rim with a dark midnight blue opaque interior disk. High-end painterly Japanese fantasy RPG UI icons, crisp readable silhouettes, restrained magical sparkles. Actual transparent alpha outside the three medallions; no background, no text, no numbers, no extra objects.

## 続けるAIへ
1. この設計書と実装を読み、最新のQA PNGを見る。
2. 背景と文字を一枚絵に統合しない。残高や能力値はセーブの実値。
3. 調整時は上限・ポイントなし・最大桁数も描画する。
4. 変更理由・失敗・修正結果を devlog と note/MATERIAL に追記し、QAの過去版を残す。
