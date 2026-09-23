# 旅支度の商店 — Azure shop

2026-09-21。添付参照画像をもとに、実際の Unity uGUI ショップを改良。

## 実装

- 白い城塞都市のテラス背景、アイボリーの三列パネル、金の装飾、紺の操作ボタン。
- 左：すべて／装備品／スキル／補給／ステータス。中央：1列の縦スクロール一覧（ドラッグ・ホイール・スクロールバー対応）。右：選択商品の大きな絵、現在と強化後の効果、レベル、価格、購入操作。
- 所持ソウル・所持エンバーは実際のウォレットの値。補給の支払い通貨は所持エンバー（冒険中のエンバーとは別）。
- 元の ShopDirector、AdventureDirector、保存、サウンド、確認モーダルを維持。最大レベルの効果説明では次レベルの予告を表示しない。
- 1170×540の安全領域に配置。背景は Canvas 全域を覆う。文字や価格は画像に焼き込まず、Unity Text を使用。
- 書体：既存の Noto Serif JP SemiBold（同梱 OFL ライセンス）。説明文は読みやすい既存 UI 書体。

## ファイル

UnityProject/BigBonusBlitz/Assets/Scripts/BBB.Runtime/AtelierShop.cs
UnityProject/BigBonusBlitz/Assets/Scripts/BBB.Runtime/AzureShopSkin.cs
UnityProject/BigBonusBlitz/Assets/Resources/Art/UI/AzureShop/

採用画像は terrace.png、card.png、panel-gold.png、items.png。全て built-in image_gen で生成。CLI/API フォールバックは未使用。PNG 原本は変更せず、枠は Sprite.Create の領域指定と 9-slice、商品アトラスは4等分の Sprite 領域で使用。

紺／金のボタン素材は前回の AzureMap の生成画像を再利用。剣・魂・本・回復・エンバーの既存画像も再利用。参考画像内の数値や強化状態はデザイン例であり、ユーザーのセーブデータを書き換えない。

## 検証

Assets/Editor/AzureShopValidation.cs を隔離した work/shop-qa コピーで実行。ユーザーのセーブ読み込みなし、コピー内だけ SaveData の保存を抑止。
出力：tools/azure-shop-qa/2026-09-21/ のバージョン別ディレクトリ。1280×720 と 2556×1179 の実際の uGUI レンダリングを確認。
購入／キャンセル／確認時の再判定／補給／冒険エンバー補充／縦スクロール／カテゴリ／強化上限／補給上限／6桁残高／長い商品名／ステータス／閉じるを検証。
静的検算：docs/check_layout.py。実機タッチテストは含まない。

## 採用画像のプロンプト

参照画像：codex-clipboard-887876e4-73f1-4114-9fde-a2c4414288a6.png（ユーザー添付）。背景・白カードのスタイル参照として使用。金枠は白カードを編集、4商品アトラスは新規生成。

### background

Use case: stylized-concept. Production Unity fantasy RPG shop background illustration. Input image is STYLE AND COMPOSITION reference. Generate ONLY the illustrated backdrop behind the shop interface: a luminous white-stone merchant guild terrace in a fantastical blue-sky castle town, towering ivory cathedral architecture, royal blue hanging banners with intricate gold heraldic embroidery at extreme left and right, brass armillary sphere and leather books at the lower right, elegant white flowers at the bottom corners, drifting soft pink petals, bright blue sky and romantic summer clouds. Wide 2.16:1 composition matching the reference. Central 80% is calm pale sky/soft distant city since interactive ivory UI panels will cover it. Upper left-middle has a draped deep royal-blue silk banner in the BACKGROUND from x15% to x46% and y0% to y17%, with no writing, giving space for a white live shop heading. Keep left/right heraldic banners at outer edges. Luxurious, highly detailed hand-painted Japanese fantasy RPG game illustration. NO user interface whatsoever: no panels, no cards, no buttons, no merchandise icons, no text, no numbers, no typography, no watermark. Edge-to-edge full illustration.

### card

Use case: stylized-concept. Production Unity shop product card background sprite inspired by the white product cards of the reference. One SINGLE horizontal rectangular card aspect 4:3, nearly fills frame, isolated on genuinely transparent background. Opaque softly pearlescent warm ivory interior with very faint diamond lattice/celestial geometric engraving, thin silver-white double rim, small elegant diamond-shaped corner accents, tiny central points extending slightly at midpoint of each edge. Front-on orthographic rectangle, no perspective. Central 80% low contrast, empty and clean to put a large merchandise icon and live dark text over. Fine highlights and subtle embossed shading like luxury fantasy JRPG shop UI. NO object illustration, NO swords, NO text, NO numbers, NO blue fill, NO large gold border. Keep faint decorative pattern sufficiently subtle for easy reading.

### Gold frame (selected card and main panels)

Edit this UI sprite: change ONLY the thin silver frame and corner ornaments into polished warm GOLD, matching high quality fantasy JRPG gold filigree. Keep the entire ivory interior, subtle celestial texture and shape exactly. CRITICAL: keep the original transparent alpha OUTSIDE the frame. Do not add a backdrop or solid exterior. The output must be an RGBA transparent-background sprite. No added text, symbols or icons. Preserve original proportions.

### Four merchandise icons

Use case: stylized-concept. Create a production fantasy JRPG inventory icon atlas, square image, precise 2 by 2 equal grid, NO grid lines. Four separate large centered icons, one in each equal quadrant, each icon confined to the central 70% of its quadrant with clear empty padding. Top left: luminous sapphire BLUE EYE framed in a gold four-point compass star, hunter's vision. Top right: beautiful antique gold LANTERN with blue glass and a warm small flame, eternal light. Bottom left: enchanted gold and sapphire AMULET pendant with white wings. Bottom right: elegant small glass OIL BOTTLE with gold stopper containing luminous golden oil. High quality polished hand-painted anime fantasy RPG items, bold readable silhouette, crisp gold bevels, matching a luxurious ivory/gold/royal-blue shop UI. Transparent alpha background around all items, no white squares, no shadows outside silhouette, no words, letters, numbers, captions, UI, cards, frames or extra objects. All four icons similar visual size, naturally colorful.


## 不採用素材

最初の独立した金枠パネルとその透過修正版は外側に背景色が残ったため採用せず。代わりに採用済み白カードの枠を金色に変更した透過素材を使用。


## 縦スクロール改良

中央は横長の商品カードを1列で表示。RectMask2D と標準 ScrollRect によるクリッピング、縦方向のみの慣性スクロール。右端にスクロールバーを配置。選択・購入・ステータスからの復帰でスクロール位置を維持し、カテゴリ変更では先頭へ戻る。キーボードで選択した行も表示域内に移動。ページボタンは廃止。検証画像は v4-scroll 以降に保存。

## 10行表示のコンパクト一覧

ユーザーの「10行くらい見たい」という指定に合わせ、商品行は高さ32・間隔2、表示域344として10行を表示。商品名と価格を横並びにして単位の繰り返しを通貨アイコンに置換。選択行は紺地と金の左線で表示。商品行だけ44の旧タッチ寸法ルールより密度を優先し、購入・閉じる・カテゴリは従来の寸法を維持。実際の商品は9点なので全品が見える。隔離QAの10点・19点の仮データでも表示件数、スクロール、購入後の位置保持を確認する。ゲームの商品設定は変更しない。
