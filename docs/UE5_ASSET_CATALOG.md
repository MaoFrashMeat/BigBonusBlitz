# BigBonusBlitz UE5.8 アセット整理台帳

作成日: 2026-11-25 / 対象エンジン: Unreal Engine 5.8 (`UE5Project/BigBonusBlitz.uproject`)
方針: C++ 移植は後段とし、先行して全現行アセットを UE5 規約で整理。3D / Niagara 刷新の受け皿フォルダを併設。

## 1. サマリー

- インポート対象総数: **480** 件（Texture: 421 / SoundWave: 58 / FontFace: 1）
- 現行（Active）: 438 件 / 旧世代隔離（Legacy）: 42 件
- 非アセット正本 JSON: 14 件（`UE5Project/Data/Json/`）
- ライセンス保全: 5 件（`UE5Project/Data/Licenses/`）
- Unity 固有スキップ: 34 件（`.shader` / `Seams/*.asset`）
- UE5.8 実インポート検証: **480 / 480** 件確認（失敗: 0 件 / 欠落: 0 件 / 初期3Dマップ `L_MainStage`: 作成済み）

## 2. UE5 Content フォルダ構成と件数

| UE5 Content パス | 件数 | 区分 | 役割 |
|---|---:|---|---|
| `/Game/BigBonusBlitz/Art/Characters/Hero/Textures` | 15 | Active | サリア現行アトラス・7武器対応 |
| `/Game/BigBonusBlitz/Art/Characters/Hero/SaliaRig` | 17 | Active | タイトル用サリア分割レイヤー |
| `/Game/BigBonusBlitz/Art/Characters/Hero/Legacy` | 30 | Legacy | 旧世代サリア（比較・参照用に隔離） |
| `/Game/BigBonusBlitz/Art/Characters/Hero/Meshes` | 0 | 3D/Niagara 受け皿 | 今後の3Dキャラモデル配置先 |
| `/Game/BigBonusBlitz/Art/Characters/Hero/Materials` | 0 | 3D/Niagara 受け皿 | 今後の3Dキャラマテリアル配置先 |
| `/Game/BigBonusBlitz/Art/Characters/Enemies/Textures` | 23 | Active | 敵20種・遠隔弾アトラス |
| `/Game/BigBonusBlitz/Art/Characters/Enemies/Legacy` | 3 | Legacy | 旧世代敵3種（隔離） |
| `/Game/BigBonusBlitz/Art/Characters/Enemies/Meshes` | 0 | 3D/Niagara 受け皿 | 今後の3D敵モデル配置先 |
| `/Game/BigBonusBlitz/Art/Characters/Enemies/Materials` | 0 | 3D/Niagara 受け皿 | 今後の3D敵マテリアル配置先 |
| `/Game/BigBonusBlitz/Art/Characters/Travelers` | 5 | Active | 旅人NPC 5種 |
| `/Game/BigBonusBlitz/Art/Environments/Stages/Atlases` | 31 | Active | 全30地点プレビュー/路面粒子 |
| `/Game/BigBonusBlitz/Art/Environments/Stages/Modules` | 32 | Active | 全30地点の現行3層背景 |
| `/Game/BigBonusBlitz/Art/Environments/Stages/Legacy` | 9 | Legacy | 旧背景・旧版モジュール（隔離） |
| `/Game/BigBonusBlitz/Art/Environments/Materials` | 0 | 3D/Niagara 受け皿 | 今後の3D環境マテリアル配置先 |
| `/Game/BigBonusBlitz/Art/Environments/Meshes` | 0 | 3D/Niagara 受け皿 | 今後の3D環境メッシュ配置先 |
| `/Game/BigBonusBlitz/Art/Symbols/Textures` | 8 | Active | リール図柄8種 |
| `/Game/BigBonusBlitz/Art/Symbols/Materials` | 0 | 3D/Niagara 受け皿 | 今後の3Dリール図柄マテリアル |
| `/Game/BigBonusBlitz/Art/Symbols/Meshes` | 0 | 3D/Niagara 受け皿 | 今後の3Dリールメッシュ配置先 |
| `/Game/BigBonusBlitz/UI/Textures/Common` | 9 | Active | UI テクスチャ |
| `/Game/BigBonusBlitz/UI/Textures/AdventureDialogue` | 4 | Active | UI テクスチャ |
| `/Game/BigBonusBlitz/UI/Textures/AzureEquip` | 8 | Active | UI テクスチャ |
| `/Game/BigBonusBlitz/UI/Textures/AzureMap` | 5 | Active | UI テクスチャ |
| `/Game/BigBonusBlitz/UI/Textures/AzureSettings` | 1 | Active | UI テクスチャ |
| `/Game/BigBonusBlitz/UI/Textures/AzureShop` | 4 | Active | UI テクスチャ |
| `/Game/BigBonusBlitz/UI/Textures/CelestialStats` | 3 | Active | UI テクスチャ |
| `/Game/BigBonusBlitz/UI/Textures/CutIn` | 5 | Active | UI テクスチャ |
| `/Game/BigBonusBlitz/UI/Textures/Frames` | 43 | Active | UI テクスチャ |
| `/Game/BigBonusBlitz/UI/Textures/Icons` | 44 | Active | UI テクスチャ |
| `/Game/BigBonusBlitz/UI/Textures/MapV2` | 11 | Active | UI テクスチャ |
| `/Game/BigBonusBlitz/UI/Textures/Navi` | 8 | Active | UI テクスチャ |
| `/Game/BigBonusBlitz/UI/Textures/Text` | 13 | Active | UI テクスチャ |
| `/Game/BigBonusBlitz/UI/Textures/Title` | 29 | Active | UI テクスチャ |
| `/Game/BigBonusBlitz/UI/Textures/Zones` | 3 | Active | UI テクスチャ |
| `/Game/BigBonusBlitz/UI/Fonts` | 1 | Active | NotoSerifJP-SemiBold フォント |
| `/Game/BigBonusBlitz/UI/Widgets` | 0 | 3D/Niagara 受け皿 | 今後のUMG / CommonUI 配置先 |
| `/Game/BigBonusBlitz/UI/Materials` | 0 | 3D/Niagara 受け皿 | 今後のUIマテリアル配置先 |
| `/Game/BigBonusBlitz/VFX/Textures/Procedural` | 6 | Active | Blender焼き込みプロシージャル素材（Niagaraマスク/ノイズ） |
| `/Game/BigBonusBlitz/VFX/Textures/Flipbooks` | 13 | Active | Niagara SubUV 用フリップブック |
| `/Game/BigBonusBlitz/VFX/Textures/Sprites` | 20 | Active | Niagara スプライト・斬撃・光輪 |
| `/Game/BigBonusBlitz/VFX/Textures/Doors` | 12 | Active | 扉・金庫演出テクスチャ |
| `/Game/BigBonusBlitz/VFX/Textures/TextFX` | 7 | Active | 擬音・ロゴ演出テクスチャ |
| `/Game/BigBonusBlitz/VFX/Materials` | 0 | 3D/Niagara 受け皿 | 今後のNiagaraマスターマテリアル配置先 |
| `/Game/BigBonusBlitz/VFX/Niagara` | 0 | 3D/Niagara 受け皿 | 今後のNiagara System / Emitter 配置先 |
| `/Game/BigBonusBlitz/Audio/BGM` | 7 | Active | BGM SoundWave（44.1kHz 16-bit PCM） |
| `/Game/BigBonusBlitz/Audio/SE` | 51 | Active | SE SoundWave（44.1kHz 16-bit PCM） |
| `/Game/BigBonusBlitz/Audio/MetaSounds` | 0 | 3D/Niagara 受け皿 | 今後のMetaSound配置先 |
| `/Game/BigBonusBlitz/Data/Tables` | 0 | 3D/Niagara 受け皿 | 今後のDataTable / DataAsset配置先 |
| `/Game/BigBonusBlitz/Blueprints/Core` | 0 | 3D/Niagara 受け皿 | 今後のCore系Blueprint配置先 |
| `/Game/BigBonusBlitz/Blueprints/Slot` | 0 | 3D/Niagara 受け皿 | 今後のリール・抽選Blueprint配置先 |
| `/Game/BigBonusBlitz/Blueprints/Adventure` | 0 | 3D/Niagara 受け皿 | 今後の冒険・戦闘Blueprint配置先 |
| `/Game/BigBonusBlitz/Maps` | 0 | 3D/Niagara 受け皿 | 3Dステージ・タイトル等のレベル |

## 3. 命名規約・テクスチャ設定

- プレフィックス: `T_`（Texture2D） / `SW_BGM_`・`SW_SE_`（SoundWave） / `FF_`（FontFace） / `L_`（Level）
- UI テクスチャ: `TEXTUREGROUP_UI` / `NoMipmaps` / `sRGB=True`
- 3D・キャラ・背景・図柄: `TEXTUREGROUP_WORLD` / `FromTextureGroup`（MipMapあり） / `sRGB=True`
- Niagara VFX（カラー）: `TEXTUREGROUP_EFFECTS` / `FromTextureGroup` / `sRGB=True`
- Niagara VFX（グレースケールマスク）: `TEXTUREGROUP_EFFECTS` / `TC_Grayscale` / `sRGB=False`
- 全 480 件の元パス ↔ UE5 パス完全対応表: `UE5Project/Data/asset_manifest.json`

## 4. 非アセット JSON データ (`UE5Project/Data/Json/`)

- `UE5Project/Data/Json/achievements.json` ← `UnityProject/BigBonusBlitz/Assets/Resources/Data/achievements.json`
- `UE5Project/Data/Json/adventure_environments.json` ← `UnityProject/BigBonusBlitz/Assets/Resources/Data/adventure_environments.json`
- `UE5Project/Data/Json/adventure_presentation.json` ← `UnityProject/BigBonusBlitz/Assets/Resources/Data/adventure_presentation.json`
- `UE5Project/Data/Json/enemy_presentation_prototype.json` ← `UnityProject/BigBonusBlitz/Assets/Resources/Data/enemy_presentation_prototype.json`
- `UE5Project/Data/Json/enemy_roster_p2.json` ← `UnityProject/BigBonusBlitz/Assets/Resources/Data/enemy_roster_p2.json`
- `UE5Project/Data/Json/enemy_tables.json` ← `UnityProject/BigBonusBlitz/Assets/Resources/Data/enemy_tables.json`
- `UE5Project/Data/Json/game_config.json` ← `UnityProject/BigBonusBlitz/Assets/Resources/Data/game_config.json`
- `UE5Project/Data/Json/hero_presentation_prototype.json` ← `UnityProject/BigBonusBlitz/Assets/Resources/Data/hero_presentation_prototype.json`
- `UE5Project/Data/Json/notices.json` ← `UnityProject/BigBonusBlitz/Assets/Resources/Data/notices.json`
- `UE5Project/Data/Json/se_config.json` ← `UnityProject/BigBonusBlitz/Assets/Resources/Data/se_config.json`
- `UE5Project/Data/Json/title_layers.json` ← `UnityProject/BigBonusBlitz/Assets/Resources/Data/title_layers.json`
- `UE5Project/Data/Json/weapon_visuals.json` ← `UnityProject/BigBonusBlitz/Assets/Resources/Data/weapon_visuals.json`
- `UE5Project/Data/Json/workflow_config.json` ← `UnityProject/BigBonusBlitz/Assets/Resources/Data/workflow_config.json`
- `UE5Project/Data/Json/salia_rig_model.json` ← `UnityProject/BigBonusBlitz/Assets/Resources/SaliaRig/model.json`

## 5. 再インポート / 追加取り込み手順

```powershell
py -3 UE5Project/Tools/prepare_staging.py
& "F:\EpicGames\UE_5.8\Engine\Binaries\Win64\UnrealEditor-Cmd.exe" "D:\GitHub\BigBonusBlitz\UE5Project\BigBonusBlitz.uproject" -ExecutePythonScript="D:\GitHub\BigBonusBlitz\UE5Project\Tools\ue_import_assets.py" -unattended -nop4 -nosplash -NullRHI -stdout -FullStdOutLogOutput
py -3 UE5Project/Tools/generate_catalog.py
```

<a id="ue-tech-421"></a>
## 2026-11-25　UE版の役別目押し（dev 421）

手動のレバーで `tech.roleRules` を読み、左のチェリー・中のスイカ・右のベルの順に対応するリールを案内する。上中下の3コマに役と指定図柄が見え、対象リールを最初に押せば滑り後にも両方が残る図柄から抽選する。対象リールを止めると成否が決まり、最後のリールでゲーム結果と併記する。AUTO開始のGには課題を出さない。

- 実装は UE5Project の BBBSlotMachine、BBBMainStageController、BBBMainHudWidget。検証は BBBCoreAutomationTests の TechChallenge。課題順の優先は今回の作業判断。
- 初回のPIE描画では3D部分が極端に暗かった。診断用のカメラで物理露出を無効にすると見えたため、本編カメラにも適用し、回転時のモーションブラーを切った。生成側がActorラベルを使うため、PIEでそのラベルを参照する。移動対象の敵とサリアをMovableにした。
- [自動検証](../tools/tech-qa/2026-11-25/v1/results.json)。UEの手動/AUTO計1万Gで3リールの出題と成功・失敗を確認。別の20万G検査で違反0。配置検査成功。隔離Unityはライセンス認証で終了コード198となり、対象EditModeの結果なし。
- ドラムの円周全体に20コマを貼る方法では窓に6コマ以上が見え、7やBARも左右反転していた。高さ方向へ3コマを貼り、図柄の向きとカメラ位置を修正。回転中に前フレームの図柄が残るため、UE版のアンチエイリアスをFXAAに変更した。
- 隔離UEの通常プレイ画面で[出題](../tools/tech-qa/2026-11-25/v6/prompt.png)・[成功](../tools/tech-qa/2026-11-25/v6/success.png)・[失敗](../tools/tech-qa/2026-11-25/v6/miss.png)を目視。別途ドラムの停止位置を0〜19に固定して撮影し、図柄テンプレートと照合した。3リール×3段×20位置は[180/180一致](../tools/tech-qa/2026-11-25/v6/reel-windows.json)。
- 次は専用の「狙え！」柱、精度ランク、成功報酬。配布ビルド・実機での入力タイミングは未確認。
