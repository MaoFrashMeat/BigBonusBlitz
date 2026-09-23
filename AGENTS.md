# BigBonusBlitz: AIへの引き継ぎ

更新: 2026-09-23（Asia/Tokyo）。本人の依頼に基づく継続方針。

- 作業開始時に `CLAUDE.md`、`docs/PROJECT_STATE.md`、対象の設計MDを読む。共有作業場の他の変更を戻さない。
- **UIの新規作成・改修では `docs/AZURE_UI_QUALITY_STANDARD.md` を必ず読む。** 本人は「毎回画像を渡して指摘しなくても、2026-09-23の設定参考画像の質にしてほしい」と指定した。設定・ショップ・ステージ選択の白/紺/金、装備の紺/金など、画面ごとの採用済み画風を維持する。
- 「ボタンが動く」「重ならない」だけを完了条件にしない。背景・装飾・文字・状態・操作を分離して実装し、Unityの実描画を目視する。初期状態だけでなく別タブ・最大文字・不足・確認・復帰も検証する。
- UI変更は `docs/check_layout.py` と対象の隔離Unity検証を実行。画像とJSONを `tools/<画面>-qa/<日付>/<版>/` に保存。実機や配布ビルド未確認ならその範囲を明記。
- 本人の依頼原文は `docs/note/MEMO.md`、判断・失敗と修正は `docs/devlog.md`、現在地は `docs/PROJECT_STATE.md`、他AI向け時系列は `docs/AI_HANDOFF_HISTORY.md` に日付付きで残す。note材料は `docs/note/MATERIAL.md` から正本へリンクする。会話にない日時を推測しない。
- お知らせは `Assets/Resources/Data/notices.json` の最新番号を確認して1件追加。公開・コミットは別途その作業の指示に従う。
