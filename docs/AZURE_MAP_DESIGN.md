# ステージ選択：蒼の大陸（2026-09-21）

ユーザー指定の参考画像「ChatGPT Image 2026年9月21日 15_09_58 (4).png」に合わせ、明るい幻想世界、紺と金の操作面、羊皮紙の詳細パネルをUnity uGUIへ実装。

## 素材と実装

生成方式：内蔵 image_gen。生成PNGは全て元のまま Assets/Resources/Art/UI/AzureMap/ に保存。ボタンと羊皮紙の透明余白は Sprite.Create の矩形で除外しており、画像ファイル自体は加工していません。

- world.png：画面全体の幻想世界イラスト
- foreground.png：手前の騎士・花・テラス・書物の透過レイヤー
- parchment.png：詳細パネルの装飾紙
- button-navy.png：副操作と地点名札
- button-gold.png：冒険へ進む主ボタン

AzureMapSkin.cs が専用素材、明朝体、ボタンと所持数札を管理。AtelierMap.cs の経路・選択・進行・コールバックを使用し、MapScreen.cs の SafeStage を1170×540へ変更。狭い画面では舞台全体を縮小して安全領域に収めます。背景画像にもとの参考画像のUIは焼き込まず、地点・線・文字・所持数・ボタンはUnityで描画します。

Noto Serif JP SemiBold を Fonts/ に同梱。[公式配布元](https://github.com/notofonts/noto-cjk/tree/main/Serif/SubsetOTF/JP)、[ライセンス](https://github.com/notofonts/noto-cjk/blob/main/Serif/LICENSE)。ライセンス本文も NotoSerifJP-OFL.txt に同梱しています。

## 検証方法

AzureMapValidation.Run を隔離した work/shop-qa コピーで実行。1280×720、2556×1179の実描画、ステージ選択で詳細が切り替わること、ズーム上下限・現在地、分岐条件、全画面遷移コールバック、6桁残高、冒険中の表示、文字の重なりと切れ、44以上の操作領域を検証します。ユーザーのセーブデータは読み書きしません。実機タッチ操作・端末性能の検証は別途必要です。

## 最終生成プロンプト

### world.png

```
Use case: stylized-concept. Asset type: production Unity fantasy RPG stage-selection background illustration, wide 2.16:1 landscape (approximately 2304x1064), no UI baked into the image. Input image 1 is STYLE AND COMPOSITION REFERENCE ONLY. Re-create its luminous azure fantasy world and hand-painted premium Japanese RPG aesthetic. A vast blue continent seen from a white-stone terrace: turquoise waterways, forested islands and mountains, waterfalls cascading through clouds, a radiant medieval white-and-blue castle high in the upper central distance. Light warm ivory clouds in the top left provide calm low-detail space for a chapter heading. At the far lower left, a small pink-haired female knight in elegant navy, white and gold armor with a sword, viewed from behind looking across the world, occupying only the leftmost 15% of the frame and lower 40%. White flowers and aged stone balustrade across the bottom, restrained drifting pink petals. At extreme right and lower right a richly painted scholar's table with a leather atlas, brass compass, rolled charts, white feather quill and a little warm candle light; these props stay in the outermost 15% and bottom 13%, leaving the right central 32% calm because a live detail panel will cover that part. Brilliant airy natural sunlight, intricate polished anime fantasy game illustration, azure, lapis, turquoise, ivory and antique gold. CRITICAL: remove ALL interface from reference: no nodes, no route lines, no rings, no labels, no buttons, no text, no logo, no numbers, no parchment panel, no HUD. Entire frame must be an uninterrupted beautiful illustration, edge-to-edge, no border. Preserve the reference's sense of scale, bright air, high detail and romantic adventure, not a dark map.
```

### parchment.png

```
Use case: stylized-concept. Production Unity UI asset. Reference image is style guidance. Generate ONLY the blank warm ivory parchment detail card/frame on the right of the reference, isolated on genuinely transparent background. Portrait 3:4 aspect, card nearly fills canvas with 2% transparent margin. Fine antique gold filigree in all four corners, layered fine golden rim, tiny compass-star accents, restrained blue feather accent on upper right rim, gentle dimensional paper edge and soft shadow. Large completely blank, flat opaque ivory paper interior covering 85% of card, with very subtle natural fiber texture. Front-on orthographic, straight rectangle, not perspective. NO text, no image inset, no buttons, no lines in centre, no icons in centre, no illustration inside, no writing whatsoever. Premium high-detail Japanese fantasy RPG UI matching reference. Transparency outside the card only; fully opaque paper inside.
```

### button-navy.png

```
Use case: stylized-concept. Production Unity UI button sprite based on the navy-and-gold buttons in the reference. A SINGLE horizontal elongated navy enamel button frame, about 5:1 width to height, centred on transparent background, canvas landscape. Shape: chamfered corners with gently pointed mid-left and mid-right ends, very fine double antique gold rim and small delicate filigree ornaments ONLY at the very ends, deep lapis navy opaque interior with subtle painted silk sheen. Wide completely empty central area to place live UI text. Straight-on orthographic, crisp polished detail, elegant high-end Japanese fantasy RPG. Button must fill the canvas width with 2% margins, occupy a horizontal band at vertical centre; genuinely transparent pixels everywhere outside button. NO letters, NO words, NO icons, NO other objects, NO extra buttons, NO mockup background. Make the central 70% visually quiet. Avoid a thick heavy frame.
```

### button-gold.png

```
Use case: stylized-concept. Production Unity UI primary button sprite based on the warm gold adventure button in the reference. A SINGLE horizontal elongated champagne-gold enamel button frame, about 5:1 width to height, centred on transparent background, canvas landscape. Shape: chamfered corners with gently pointed mid-left and mid-right ends, fine embossed antique gold double rim and delicate small compass-star ornaments ONLY at left and right ends, fully opaque warm ivory-gold interior with soft metallic sheen and central luminance. Wide empty central area for live dark navy text. Orthographic front view. High-end Japanese fantasy RPG polished UI. Button fills width with 2% margins, sits in horizontal band at vertical centre; genuinely transparent pixels outside button. NO text, NO letters, NO central icons, NO other objects, NO extra buttons. Do not make an orange or brown button; luminous pale champagne gold.
```

### foreground.png

```
Use case: background-extraction. Edit target: provided world illustration. Create a matching transparent foreground layer with EXACT same wide canvas size and object placement as the input. Keep only the lower-left pink-haired knight, her sword, the nearest flowers at the far left/bottom, and nearest stone terrace balustrade along the bottom 12%. Keep the lower-right scholar table, book, rolled scrolls, compass, the candles and white quill at the extreme right edge. Remove ALL the distant landscape, sky, clouds, distant castles, mountains, floating islands and water, replacing them with actual transparent alpha. Do not shift, resize or repaint the retained objects. No top-left pillar or compass, no right upper tower, no UI, no text. Middle 80% of image must be fully transparent. This layer goes above route-map nodes while the original image remains underneath, so matching pixels and positions is essential.
```

