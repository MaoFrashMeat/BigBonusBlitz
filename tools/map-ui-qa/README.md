# ミニマップ V2 UI

MapScreen に適用済み。画面を作り直すため、Unityでタイトルからマップへ入り直してください。

- V2素材: assets/title/parts/frames_V2 と assets/title/parts/icon_v2。
- 使用素材を Assets/Resources/Art/UI/MapV2 にコピー。元のPNGは変更していません。
- MapUiV2 が透明余白をSpriteの矩形で除外し、9スライスで金枠の太さを保ちます。
- 街と冒険の説明・ボタン、章名・現在地、所持ソウル、資源を独立した領域に配置。
- 主ボタンは48、戻るボタンは44 Canvas単位。押下・フォーカスの色変化あり。
- StageMapView の overview オプションでミニマップだけ文字と現在地を調整。ゲーム内マップの既定表示、ルート開示条件、ゲーム進行は維持。
- 図はルートの確認用です。出発には「冒険へ進む」を使います。

検証画像 map-v2-1280.png / map-v2-2556.png は実際のUnity uGUIを分離プロジェクトで描画したものです。サンプルは所持ソウル999,999、B-2、残り9G。ユーザーのセーブは読み書きしません。

配置確認: docs/check_layout.py。Unity検証: Assets/Editor/MapUiValidation.cs（分離したbatchmode専用、MAP_UI_OUTPUTに出力先を設定）。
