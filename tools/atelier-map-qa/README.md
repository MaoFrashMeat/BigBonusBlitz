# 街の地図（アトリエ風）の QA

`Assets/Editor/AtelierMapQa.cs` を分離した batchmode で走らせ、実際の uGUI を PNG に描いたもの（docs/ui_rules.md 9）。

- `town-map-1280.png` / `town-map-2556.png`: 等倍。標本は B-2 現在地、A-1 / B-1 通過、ソウル 100、到着の知らせあり
- `town-map-zoomed-*.png`: ＋ を 2 回押した 1.56 倍
- `validation.json`: 押せる面 44 以上、同じ親の文字の重なり無し、下端のボタン列が端から 24 以上、ズームと「現在地」の往復

走らせ方（分離コピーは `tools/map-ui-qa/UnityCheck`。Assets/Scripts・Editor・Resources を写してから）:

```bash
ATELIER_MAP_OUTPUT=D:/GitHub/BigBonusBlitz/tools/atelier-map-qa "D:/Unity/Hub/Editor/6000.3.15f1/Editor/Unity.exe" -batchmode -quit -projectPath D:/GitHub/BigBonusBlitz/tools/map-ui-qa/UnityCheck -executeMethod AtelierMapQa.Run -logFile tools/atelier-map-qa/unity.log
```
