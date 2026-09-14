# assets — 元絵と音の置き場

Unity が読むのは `UnityProject/.../Resources` 側。ここは元絵（生成物・シート・候補）で、`tools/` の各スクリプトがここから Resources へ組む。
2026-09-14 にジャンルで整理した。新しい素材は下のどれかに入れる。ルート直下には置かない。

| フォルダ | 中身 | 使うスクリプト |
|---|---|---|
| `characters/salia/` | セリア。`Chr0001_001`（ComfyUI の生成物と設定シート）、`salia-rig`（パーツ分け）、`Live2D_Title`、立ち絵 `salia_title_reach`、キービジュアル `keyart_title(_logo)`、ドット strip 2 枚 | tools/comfy、tools/salia-viewer、gen_sprite_viewer.py |
| `characters/enemies/` | 敵（bat / goblin / slime） | — |
| `symbols/` | リールの図柄・数字・文字の候補（LFS）。`number/`（num{d}_{v}）、`text/`、`button/` | symbol_*.py、text_build.py、symbol_viewer / fx_viewer |
| `ui/` | 枠とアイコン。`sheets/`（元のシート 3 枚）→ `frames/` `icons/`（切り出し）、`frames_V2/` `icon_v2/`（アップスケール版）、`navi/`（押し順ナビ）、`misc/` | tools/ui/cut_sheets.py、adopt_frames.py、gen_*_viewer.py |
| `logo/` | ロゴ 4 種 | — |
| `backgrounds/title/` | タイトルの背景 8 枚と `flower/`（花びら） | cut_sheets.py --no-cut、gen_title_viewer.py |
| `backgrounds/textures/` | 石畳など | — |
| `fx/` | 演出の素材（dust_cloud） | — |
| `sounds/` | BGM・効果音 | — |
| `_unused/` | 参照されていないもの。消していない（中の README に表） | — |
