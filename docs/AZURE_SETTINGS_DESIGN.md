# あなたのための冒険 — Azure settings

更新: 2026-09-23 / Asia/Tokyo

## 依頼と参照

本人:「設定項目のUIが変なので２枚目のように改善してください。今後他の箇所のUIでもこのように言わなくても２枚目の質になるよう改善してください。」

変更前: `C:/Users/ts081/OneDrive/画像/Screenpresso/2026-09-23_14h41_48.png`。
参照: `assets/refarence/ChatGPT Image 2026年9月23日 14_46_12 (2).png`。
白い城塞、象牙色の情報面、紺と金のサイドパネル、金の罫線、明朝の見出し、右の字幕プレビューを採用。画像内の数値・架空機能は仕様に加えない。

## 実装

- `AtelierSettings.cs`: 表示・演出、サウンド、操作ガイド、呼び出し元の第4タブを共通の画面構成へ変更。設定キーと100〜150%の字幕範囲は維持。
- `AzureUiControls.cs`: ラベル、金罫線、左右つまみのON/OFF、宝石スライダーを共通化。44pxの操作範囲と装飾の大きさを分離。
- `AzureScreenFit.cs`: 1170×540の内容をSafeRootへ収める。960幅のタイトル画面からも開ける。
- `AzureSettingsTextureImporter.cs`: 対象フォルダだけの透過・NPOT保持・Clamp・mipmap無効。
- `AzureSettingsValidation.cs`: 隔離Unityの実描画・操作・複数入口検証。
- 新規素材 `Assets/Resources/Art/UI/AzureSettings/navigation.png`。既存の `AzureShop/terrace.png`、`AzureShop/card.png`、`panel-gold.png`、`AzureMap/button-navy.png`、`button-gold.png`、既存アイコンを再利用。

## 操作と表示の契約

- ON/OFFは位置・色・文字で区別。字幕を非表示にするとプレビューの字幕背景も消える。
- 字幕は100〜150%・10%刻み。表示文字とつまみ位置を同じ丸め値に揃える。
- 表示設定の初期化は表示タブだけに置く。音量は変更しない。
- 音量はドラッグとキーボード変更でも保存。既存の離したときの試聴音と保存を保持。
- タイトルのデータ削除は既存の2段階操作を保持。UI検証は1回目の確認だけを押し、削除は行わない。
- 冒険側の戻る・スランプ・AUTO条件・セーブ削除コールバックを保持。危険な操作の役割が分かる色を維持。
- UIの文字と状態は画像に焼き込まずUnityの生UIで表示。

## 発見と修正

1. 旧スライダーのつまみは自身のアンカーだけを修正していた。UnityのSlider.UpdateVisualsが縦アンカーを再びstretchへ戻すため、操作後は縦長になる。親HandleAreaの高さを28へ固定し、子の宝石は24×32で描く。
2. 同じ設定をタイトル960幅と冒険1170幅から開く。親のSafeRootを基準に表示全体を収め、背景だけはCanvasまで広げる。
3. v1隔離描画で独自宝石GraphicにCanvasRendererが無く失敗。RequireComponentを追加。C#コンパイルだけでは描画依存の不足を検出できない。
4. v2でタイトルのデータ説明が40pxへ収まらず、150%字幕のかぎ括弧だけが折り返した。説明幅と高さを増やし、字幕を意味単位で改行。
5. v3のEditModeでTime.time=確認期限=0となり既存の確定分岐へ入った。本番データは未使用。タイトルと冒険の削除に「確認期限が正」を追加し、最終版では初回確認のみ検証した。
6. v4は機械検査に合格しても、実冒険入口のAUTO条件が暗い旧ボタンに戻っていた。3行の共通スイッチとRefreshSwitchへ移し、更新後のON/OFFも可読性を維持。

## 画像生成

内蔵image_genを使用。CLI/APIフォールバック未使用。背景を再生成せず、採用済み城塞素材を活用。生成原本は `$CODEX_HOME/generated_images/01a0918e-f596-7aa0-b6a0-25dbb4e2f317/exec-ad1814bb-37d5-4344-82c0-32d0ead79f4a.png`。プロジェクトのnavigation.pngへそのままコピー。

### navigation.png の全プロンプト

Use case: stylized-concept. Production UI asset for an elegant Japanese fantasy RPG settings navigation sidebar. One single tall vertical heraldic panel, approximately 1:3 aspect. Flat orthographic frontal. Midnight navy blue silk and softly brushed sapphire, very fine warm champagne gold double border, luxurious delicate art nouveau corner filigree. Straight sides, shallow pointed center at bottom. An intricate small gold compass rose in upper 14 percent, faint engraved winged compass crest in lower 28 percent, center 55 percent completely empty quiet opaque navy for live menu labels. Elegant restrained gold, premium painted game interface. Genuine transparent alpha outside the panel; interior fully opaque. Panel almost fills image with 2 percent transparent margin. NO text, letters, numbers, buttons, menu rows, characters, landscape or watermark. The gold decorations stay within outer 8 percent except top/bottom crests.

## 検証と引き継ぎ

隔離プロジェクト: `work/shop-qa`、productNameもshop-qaを検査し、本番のPlayerPrefs/セーブを使わない。実描画は `tools/azure-settings-qa/2026-09-23/` に版別で保存する。
対象: 1280×720、2556×1179、初期表示、字幕130/150%、高コントラスト、非表示、設定再読込、初期化と音量保持、音量とBGM切替、ガイド、音なし、実TitleScreenのデータ頁と確認、実GameControllerの設定と入力遮断。
実機タッチ、配布ビルド、聴感は未検証。最新validation.jsonを見て合否を判断する。

最終結果: `tools/azure-settings-qa/2026-09-23/v5-final/validation.json` はpassed=true。12状態×2解像度=24枚を保存。コンパイル（Core/Runtime/Tests）、配置検算、差分空白検査も成功。実タイトル・実冒険の設定を構築し、AUTOビット保存、最初の削除確認、設定中のBET遮断も検証。OSの実キー送信、実端末のタッチ操作、音の聴感までは検証していない。お知らせdev 315。

今後の共通品質: [AZURE_UI_QUALITY_STANDARD.md](AZURE_UI_QUALITY_STANDARD.md)。記録の正本はdevlogとAI_HANDOFF_HISTORY、本人の原文はnote/MEMO。
