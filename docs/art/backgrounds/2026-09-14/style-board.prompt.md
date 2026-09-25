# 背景テイスト比較・生成プロンプト

- 生成方法: 組み込み `image_gen`（CLI/API 不使用）。
- 用途: A/B/C を同一構図で比較する1枚のコンセプトボード。ユーザー回答で初回3案は未採用、参考画像待ち。
- 既存 `A-1.png` は目視参照。生成入力には渡さず、草原・遠い街・灯の道というモチーフを文章化した。
- 本番レイヤー、Unity実描画、シームレス検証済み素材ではない。

```text
Use case: stylized-concept.
Asset type: a single art-direction comparison board for BigBonusBlitz, a Japanese 2D side-scrolling fantasy RPG, to choose its background art style before producing game assets.
Create ONE polished comparison sheet with THREE large panoramic landscape panels stacked vertically. Canvas 1536 x 1536 or similar square high resolution. Narrow warm-white gutters. Each panel roughly 3.2:1, same camera, same scene layout, same neutral daytime light and same restrained green/blue/ochre palette, so the only meaningful difference is drawing style. Small clean label A, B, C at top-left of each panel, no other text.
Scene in EVERY panel: 'grassland entrance' of a journey sustained by lantern light. Side-on continuous countryside, low blue mountains in the far distance, a SMALL old stone town and bridge on the left in far background; gentle grassy hills in the middle distance, a few simple weathered stone lantern posts along a broad horizontal dirt walking path; sparse rounded rocks and clusters of grass framing the bottom. Keep the middle lower portion open for characters. Horizon around 40% from top of each panel. No deep road vanishing to the center, no dramatic lens perspective. Left and right boundaries should have similar ground height and vegetation density, suitable as the concept for a horizontal scrolling panorama. Distinct distant/middle/foreground silhouettes. Small town must not dominate.
A, top panel: CLEAR CEL ANIME. Confident economical colored contours, two or three tone cel-shading, clean broad color shapes, angular yet inviting rock simplification, tree and grass masses simplified into readable silhouettes, almost no microtexture. Professional polished TV animation background with graphic clarity, not a cheap vector placeholder.
B, middle panel: SOFT HAND-PAINTED ANIME BACKGROUND. Delicate selective colored edge lines, beautifully grouped shapes with soft gouache-like edge variation and broad two or three value shading, expressive rounded foliage clusters, gentle atmosphere; restrained painterly texture only inside large shapes. More charming and drawn than realism. This is the balanced recommended direction: illustrated adventure atmosphere, clear readable shapes, no photorealistic detail.
C, bottom panel: STORYBOOK ANIME. Rounder and slightly more stylized trees, rocks, hills and town, subtle paper/gouache texture, warm inviting flat layered shapes, decorative but controlled silhouettes. Deliberately more stylized than B. Still a fantasy adventure environment, not childish clip-art.
Constraints: visibly different art direction between A/B/C while preserving exactly the same scene composition. Avoid photorealistic grass blades, granular stone textures, individual tiny flowers everywhere, 3D rendering, bloom, god rays, baked strong sunbeams, long directional shadows, rain, snow, fog veils, glowing lanterns. Sky should be calm plain pale blue for this concept only. No people, creatures, interface, watermark, symbols other than A B C. This board is a visual concept, NOT a layer atlas or a technical seamless certification.
```
