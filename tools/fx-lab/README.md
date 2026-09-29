# fx-lab — エフェクト見本の作業場

本体プロジェクトに触らずに、エフェクトの見本を Unity で描き出す。
`%TEMP%\bbb-fx-lab` に作業用の Unity プロジェクト（組み込みモジュールだけ・設定は本体から写す）を作り、
batchmode で連番 PNG → mp4 にする。本体を Unity で開いたままでも動く。

```powershell
.\tools\fx-lab\run.ps1                       # 12 本すべて（約 5 分＋動画化 10 分）
.\tools\fx-lab\run.ps1 -Only slash1,coins    # 絞る
.\tools\fx-lab\run.ps1 -Spark                # 火花の見本だけ
.\tools\fx-lab\run.ps1 -Unity "C:/…/Unity.exe"   # Unity の場所が違う PC
```

出力: `%TEMP%\bbb-fx-lab\work\`（`fx/<clip>/f_0000.png…`、`mp4/<clip>.mp4`、`reel.mp4`）。
残す描き出しは `docs/art/<日付>/` へ写す。

| ファイル | 中身 |
|---|---|
| `unity/Assets/Editor/FxLab.cs` | 12 本の組み立てと描き出し。クリップは `Clips()`、1 本 = 1 関数（`Slash1` … `ShieldClip`）。一撃は `Impact`、発動は `Surge`（決まりは `docs/FX_RESEARCH.md`） |
| `unity/Assets/Editor/SparkLab.cs` | 火花の見本（最初に作ったもの） |
| `unity/Assets/Shaders/LabSlash.shader` | 三日月の斬撃。外・中・芯を同じ場から内側へ削って作る |
| `unity/Assets/Shaders/LabShield.shader` | 六角格子のシールド（縁の光・走る帯・被弾の波紋・下から張られる） |
| `unity/Assets/Shaders/LabAura.shader` | キャラのシルエットから立つ炎（攻撃のオーラ） |
| `unity/Assets/Shaders/LabCoin.shader` | メッシュ粒子のコイン（ライトなしで金属の映り込み） |
| `unity/Assets/Shaders/LabDissolve.shader` | 倒れる敵の絵。消える順の図（R）・破片の番号（G）・ひび（B）で削る。光る縁・焦げの帯・真っ二つの切り口 |
| `unity/Assets/Shaders/LabFrame.shader` | 画面の枠（ステップアップ）。角丸の四角を式で描く。光の流れ / 雷 / 炎、虹。暗い下地（乗算）＋光（加算） |
| `unity/Assets/Shaders/LabFire.shader` | 本物寄りの炎（粒子）。炎の連番の明るさを温度にして黒体の色で塗る |
| `unity/Assets/Shaders/LabCutBand.shader` / `LabCutChar.shader` | カットインの帯（流線・電撃・炎・矢印・虹）とキャラ（縁取り・白く飛ばす・四辺を消す） |
| `unity/Assets/Shaders/LabStage.shader` | 背景とキャラ。一撃の間だけ暗く・色を抜く（主役ごとに効きを変える） |
| `unity/Assets/Shaders/LabBloom.shader` | ブルームと仕上げ（揺れ・ズーム・方向ブラー・流線/集中線・ネガ・白フラッシュ・トーンマップ） |
| `unity/Assets/Shaders/LabFx*.shader` / `LabMisc.shader` | 粒子の加算・アルファ、板ポリの加算 |
| `textures/` | エフェクト素材（`LAB_TEX` で読む）。Kenney Particle Pack（CC0、`LICENSE-kenney.txt`）の単体 12 枚、Brackeys VFX Bundle（CC0、`LICENSE-brackeys.txt`）の連番 7 本（`fb_<名前>_<横>x<縦>.png`）、Unity 公式の炎の連番（CC0、`LICENSE-unity-vfx.txt`）、斬撃の繊維 `slash_fibers.png` |
| `blender/fire_sim.py` / `blender/make_flipbook.py` | Blender（5.2）で炎を流体シミュレーション → 8×8 の連番（`textures/bl_*_8x8.png`）。`blender -b -P blender/fire_sim.py -- <出力> 128 64 campfire` |
| `blender/render_vault.py` | 金庫の大扉を Blender でモデリングして撮る（枠・扉の閂あり/なし・ハンドル） |
| `blender/render_doors.py` | 扉のステップアップの絵。落とした CC0 の 3D 素材（Poly Haven の城門・シャッター、ambientCG の金属板）を Blender で正面から撮る。`make_door_tex.py`（自前の絵）は使わない |
| `make_shield_tex.py` | 盾の紋章（`textures/shield_crest.png`） |
| `unity/Assets/Shaders/LabGhost.shader` | 守護の影（キャラのシルエットを光で描く） |
| `make_sfx_tex.py` | 漫画の擬音のテクスチャを作る（`textures/sfx_*.png`） |
| `unity/Assets/Shaders/LabRing.shader` | 強弱のある輪と放射の筋（角度ごとのムラ・切れ目・削れて消える） |
| `unity/Assets/Shaders/LabSprite.shader` | 板の絵（擬音）。半透明・白く飛ばす |
| `make_slash_tex.py` | 斬撃の繊維テクスチャを作る（R・G = 繊維、B = 消えノイズ） |
| `compose_art.py` | 舞台（背景 1280×720・主人公・ゴブリン）をゲームの素材から作る |
| `reel.py` | 各 mp4 と、題名入りで 2 回ずつつないだ `reel.mp4` |

注意:
- 見本は **Built-in の描画＋自前のブルーム**。本体は URP なので、移すときはブルームを URP の Volume で作り直す
  （自作シェーダーは LightMode なしの CG なので URP でもそのまま描ける）
- 本体の UI は Canvas の ScreenSpaceOverlay。ParticleSystem やメッシュはそのままでは UI の上に出ない
- 数値は見本用にコードへ直書きしている。本体へ移すときは `game_config.json` と `tools/fx_viewer.html` に出す
