# Salia Motion Atelier

サリアのタイトル画面向けレイヤーモデルと専用ビューアです。呼吸、目パチ、髪、服の揺れを再生します。

## 表示

start.ps1 を PowerShell で実行するか、リポジトリ直下で node tools/salia-viewer/server.cjs を実行し、http://127.0.0.1:4178/tools/salia-viewer/ を開きます。Node.js が必要です。再生時の外部API・CDN接続はありません。

動きの強さ、速度、可動域、背景を調整できます。パーツタブで単独表示と補完量を確認できます。一時停止、閉じ目固定、メッシュ表示、透過PNG保存にも対応します。

## 素材

assets/title/Character/salia-rig/ に格納しています。

- salia-layered.psd: 1672×941、30レイヤー。元絵15層、塗り足し13層、閉じ目差分2層（非表示）。
- layers/: 補完込みの15枚の透過PNG。ランタイム用。
- underpaint/: 塗り足しだけの透過PNG。
- rig-layered.json: 描画順、配置、サイズ、揺れ分類、補完量。
- inpaint-generated/: AI生成の補完原稿。緑背景はビルド時に除去。
- parts/、salia-parts.psd、rig.json: 元絵の切り分け素材。

元絵の可視RGBを維持し、上位の不透明パーツに隠れる領域だけに補完画を追加しています。元画像のほぼ不透明なアルファ250〜254は255に補正しました。補完画は元絵に位置・大きさを合わせた推定画です。

## Unity

UnityProject/BigBonusBlitz の TitleScreen.cs に組み込み済みです。タイトル生成時に SaliaTitleModel.Create がモデルを生成します。

- Assets/Scripts/BBB.Runtime/SaliaTitleModel.cs: 再生、目パチ、パラメータ制御。
- Assets/Scripts/BBB.Runtime/SaliaLayerGraphic.cs: 各パーツのUIメッシュ。
- Assets/Resources/SaliaRig/: 配置JSON、PNG、UIシェーダー。
- Assets/Editor/SaliaTexturePostprocessor.cs: 対象素材のインポート設定。
- Assets/Editor/SaliaValidation.cs: 描画検証。

breath、hair、cloth、motionRange、speed を調整できます。Blink() で目パチ、SetPose(time,left,right) で状態を指定できます。GPU上で変形するため毎フレームのメッシュ再生成は不要です。

## 方式と制限

WebとUnityで各パーツを独立描画する独自2Dメッシュ方式です。正式なLive2D Cubism形式（cmo3 / moc3）ではありません。Cubism SDKやVTube Studioへ直接読み込めません。Cubism化にはこのPSDを素材にEditorでArtMesh・デフォーマ・パラメータを作成して書き出す工程が残ります。

切れ目対策として、全体再生では全パーツに共通の変形場と同じ格子を使用します。髪・裾の近くほど大きく動く連続した変形で、切り口の両側が離れない構成です。元絵を下地と全体表示の色参照に使い、透過サンプリングの細い線も抑えます。この再生方式では補完部分を独立移動で露出させません。塗り足し素材はPSDとパーツ単独表示に保持しています。パーツを自由に別移動させるリグは今後のCubism化で扱います。

タイトル向けの小さな待機動作用です。大きな首振り、腕のポーズ変更、完全な前髪・顔パーツ分割には追加作業が必要です。目パチは局所差分の短い補間です。可動域の初期値1.35倍を基準に調整してください。

## 再構築

sharp と @napi-rs/canvas を解決できるNode.js環境で順番に実行します。共有ライブラリを使う場合は SALIA_NODE_MODULES に node_modules の絶対パスを設定します。

    node tools/salia-viewer/build-assets.cjs
    node tools/salia-viewer/build-overlap.cjs

2番目のコマンドは補完PSD・PNGを生成し、UnityのResources素材も同期します。再構築時の画像生成API呼び出しは不要です。元画像は変更していません。
