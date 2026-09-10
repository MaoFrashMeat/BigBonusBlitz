# ポポラのスプライト生成（ComfyUI）

主人公の 9 モーション（34 コマ）を ComfyUI で描かせ、Unity に置ける
スプライトシートへ変換する一式。**GPU を使うのは 2 番だけ**なので、
生成は 4070 Ti の PC（ts081）で回し、他は別の PC でも動く。

---

## 全体の流れ

```
1. 骨格を作る        gen_pose_skeletons.py   →  poses/*.png（34 枚）
2. 絵を出す          run_batch.py            →  raw/*.png（ComfyUI が描く）
3. シートにする      postprocess.py          →  sheets/hero_*.png
4. Unity へ入れる    postprocess.py --install →  Resources/Art/HeroGen/
```

1 と 3 と 4 は CPU だけで動く。2 だけ GPU を使う。

---

## 準備（生成する PC で 1 回）

まずこれを実行する。ComfyUI の場所を自動で探し、足りないものを一覧にして、
`config.json` まで書いてくれる。

```bash
python tools/comfy/check_env.py
```

必要なもの（wk1171-pc の `F:\ComfyUI\ComfyUI_windows_portable` では確認済み）。
**他の PC にも同じものが要る。**

| 用途 | ファイル |
|---|---|
| 本体モデル | `checkpoints/Illustrious-XL-v2.0.safetensors` |
| ポーズ固定 | `controlnet/OpenPoseXL2.safetensors` |
| キャラ固定 | `ipadapter/ip-adapter-plus_sdxl_vit-h.safetensors` |
| 画像理解 | `clip_vision/CLIP-ViT-H-14-laion2B-s32B-b79K.safetensors` |
| 必須ノード | `custom_nodes/comfyui_ipadapter_plus` |

`check_env.py` が見つけられなかったときだけ、`config.json` を手で書く。

```json
{
  "server": "127.0.0.1:8188",
  "comfy_input":  "F:\\ComfyUI\\ComfyUI_windows_portable\\ComfyUI\\input",
  "comfy_output": "F:\\ComfyUI\\ComfyUI_windows_portable\\ComfyUI\\output",
  "ref_image": "popora_ref.png"
}
```

---

## 使い方

```bash
# 1. 骨格（ポーズを変えたいときだけ実行し直す）
python tools/comfy/gen_pose_skeletons.py

# 2. 生成（ComfyUI を起動してから）
python tools/comfy/run_batch.py --pull

#    一部だけ試すとき
python tools/comfy/run_batch.py --only idle --pull
python tools/comfy/run_batch.py --only attack --seed 12345 --pull

# 3. シート化して Unity へ
python tools/comfy/postprocess.py --install
```

`--pull` を付けると ComfyUI の output から `raw/` へ回収する。
付けない場合は ComfyUI 側の output フォルダに溜まるので、手で `raw/` へ移す。

---

## 調整するところ

| 直したいこと | 直す場所 |
|---|---|
| ポーズが思ったのと違う | `gen_pose_skeletons.py` の `POSES`（角度）と `HINTS`（言葉） |
| 服や髪が設定と違う | `run_batch.py` の `STYLE` |
| 変なものが混ざる | `run_batch.py` の `NEGATIVE` |
| ポーズに従わない | `workflow_popora.json` の `strength`（0.85 → 1.0） |
| キャラが似ない | `workflow_popora.json` の IPAdapter `weight`（0.75 → 0.9） |
| 絵が硬い・崩れる | 上の 2 つを下げる。強くしすぎると破綻する |
| ドットが粗い / 細かい | `postprocess.py --dots 192`（既定 192） |
| 色が多すぎる | `postprocess.py --colors 24` |

---

## うまくいかないときの順番

1. **3 人並んだ絵が出る** → `NEGATIVE` に `multiple views` が入っているか確認。
   参照画像が三面図のままだと起きやすい（`popora_ref.png` は正面だけに切ってある）
2. **ポーズが無視される** → ControlNet の `strength` を上げる。
   それでも駄目なら骨格の腕の角度が極端すぎないか見る
3. **コマごとに顔が変わる** → `seed` を固定して回し、気に入ったコマだけ残して他を再生成する
4. **背景が抜けない** → `pip install rembg` を入れると精度が上がる。
   入れない場合は白背景で出す必要がある（`STYLE` の `plain white background`）
5. **並べたとき身長が揺れる** → postprocess が足元基準で揃えているので、
   それでも揺れるなら生成側で全身が入っていないコマがある。そのコマだけ作り直す

---

## 見た目の確認

```bash
python tools/gen_sprite_viewer.py
```

`tools/sprite_viewer.html` を開くと、手描きのドット絵と並べて動きを比べられる。
生成版を見るには `gen_sprite_viewer.py` の `SETS` に `HeroGen` を足す。

---

## 限界（先に知っておくこと）

- コマ間の一貫性は完全ではない。髪の房や剣の飾りは毎コマ少し変わる。
  気になるなら同じ seed で回して、揺れの小さいコマを選ぶ
- 34 コマを一発で揃えるのは無理。**選別と作り直しが前提**
- 完全に安定させたいならポポラの LoRA を学習する（素材 20〜30 枚、学習 2 時間ほど）
