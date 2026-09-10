using System.Collections.Generic;
using BBB.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// 冒険のステージマップ（列 A〜D のノードと道）。ミニマップ画面とゲーム中のマップモーダルで共用。
    /// hideRoute のときは「通った所」と「そこから伸びる道の先（？）」だけ描く。
    /// </summary>
    public static class StageMapView
    {
        private const float NodeD = 46f;

        /// <summary>size の枠に収めて描く。戻り値は再描画用に破棄できるルート。</summary>
        public static RectTransform Build(Transform parent, AdventureConfig cfg, AdventureState st, Vector2 size)
        {
            var root = UiSkin.Rect(parent, "StageMap", Vector2.zero, size);
            if (cfg?.nodes == null || cfg.nodes.Count == 0)
            {
                UiFactory.Label(root, "Empty", Vector2.zero, size, "ステージが設定されていません", 13, TextAnchor.MiddleCenter, UiSkin.TextSub);
                return root;
            }

            // 列（A, B, C, D…）は出てきた順。列の中はノードの並び順
            var columns = new List<string>();
            var byCol = new Dictionary<string, List<StageNode>>();
            foreach (var n in cfg.nodes)
            {
                if (n == null) continue;
                string c = string.IsNullOrEmpty(n.column) ? "?" : n.column;
                if (!byCol.ContainsKey(c)) { columns.Add(c); byCol[c] = new List<StageNode>(); }
                byCol[c].Add(n);
            }
            var pos = new Dictionary<string, Vector2>();
            float padX = 54f, usableW = size.x - padX * 2;
            // 一番多い列に合わせて丸の大きさを決める（枝が多いほど小さくなる）
            int maxRows = 1;
            foreach (var c in columns) maxRows = Mathf.Max(maxRows, byCol[c].Count);
            float usableH = size.y - 46f;
            float nodeD = Mathf.Clamp(usableH / maxRows - 10f, 16f, NodeD);
            float gap = Mathf.Min(120f, usableH / Mathf.Max(1, maxRows));
            for (int ci = 0; ci < columns.Count; ci++)
            {
                var list = byCol[columns[ci]];
                float x = columns.Count == 1 ? 0 : -usableW * 0.5f + usableW * ci / (columns.Count - 1);
                for (int i = 0; i < list.Count; i++)
                {
                    float y = (list.Count - 1) * gap * 0.5f - i * gap;
                    pos[list[i].id] = new Vector2(x, y);
                }
                // 列の見出し
                UiFactory.Label(root, "Col" + columns[ci], new Vector2(x, size.y * 0.5f - 12), new Vector2(80, 18), columns[ci], 12, TextAnchor.MiddleCenter, UiSkin.TextDim);
            }

            bool hide = cfg.hideRoute;
            var visited = new HashSet<string>(st?.visited ?? new List<string>());
            var revealed = new HashSet<string>(visited);
            if (hide)
            {
                foreach (var n in cfg.nodes)
                {
                    if (n == null || !visited.Contains(n.id)) continue;
                    foreach (var t in Targets(n)) revealed.Add(t);
                }
                if (st != null && !string.IsNullOrEmpty(st.nextId)) revealed.Add(st.nextId);
            }
            else foreach (var n in cfg.nodes) if (n != null) revealed.Add(n.id);

            // 道（先に描いてノードの下に）
            foreach (var n in cfg.nodes)
            {
                if (n == null || !pos.ContainsKey(n.id)) continue;
                bool srcVisible = !hide || visited.Contains(n.id);
                if (!srcVisible) continue;
                foreach (var t in Targets(n))
                {
                    if (!pos.ContainsKey(t)) continue;
                    bool walked = visited.Contains(n.id) && visited.Contains(t) && IndexOf(st, n.id) + 1 == IndexOf(st, t);
                    bool planned = st != null && st.nodeId == n.id && st.nextId == t;
                    bool fwd = IsForward(n, t);
                    var col = walked ? UiSkin.Gold
                            : planned ? UiSkin.Hex(AdventureDirector.ColorFor(cfg.Find(t)))
                            : fwd ? new Color(1, 1, 1, 0.20f) : new Color(1f, 0.45f, 0.45f, 0.10f);
                    Line(root, pos[n.id], pos[t], walked || planned ? 3.5f : (fwd ? 2f : 1.2f), col);
                }
            }

            // ノード
            foreach (var n in cfg.nodes)
            {
                if (n == null || !pos.ContainsKey(n.id)) continue;
                bool known = revealed.Contains(n.id);
                bool here = st != null && st.nodeId == n.id;
                bool been = visited.Contains(n.id);
                var color = UiSkin.Hex(AdventureDirector.ColorFor(n));
                var p = pos[n.id];
                if (here) UiSkin.Img(root, "Glow" + n.id, p, new Vector2(nodeD * 2.4f, nodeD * 2.4f), UiSkin.Glow(96), new Color(color.r, color.g, color.b, 0.55f));
                UiSkin.Img(root, "Ring" + n.id, p, new Vector2(nodeD + 5, nodeD + 5), UiSkin.Circle(64), known ? color : new Color(1, 1, 1, 0.12f));
                UiSkin.Img(root, "Node" + n.id, p, new Vector2(nodeD, nodeD), UiSkin.Circle(64), known ? (been ? color : UiSkin.Panel) : UiSkin.Hex("#141a29"));
                // 丸が小さいときは段の文字を省いて枝番号だけにする（列見出しで段は分かる）
                int idFont = nodeD >= 38 ? 12 : (nodeD >= 30 ? 11 : 10);
                string idText = n.id;
                if (nodeD < 30)
                {
                    int dash = idText.IndexOf('-');
                    if (dash >= 0 && dash + 1 < idText.Length) idText = idText.Substring(dash + 1);
                }
                var idLabel = UiFactory.Label(root, "Id" + n.id, p, new Vector2(nodeD + 10, nodeD), known ? idText : "?", idFont, TextAnchor.MiddleCenter, been ? UiSkin.Bg : UiSkin.Text);
                idLabel.fontStyle = FontStyle.Bold;
                bool roomy = gap >= 44f;   // 丸が詰まっているときは文字を出さない
                if (roomy)
                {
                    string name = known ? n.name : "？？？";
                    UiFactory.Label(root, "Name" + n.id, p + new Vector2(0, -nodeD * 0.5f - 10), new Vector2(110, 15), name, 10, TextAnchor.MiddleCenter, known ? UiSkin.Text : UiSkin.TextDim);
                    if (known && n.kind != "normal")
                        UiSkin.Chip(root, "Kind" + n.id, p + new Vector2(0, nodeD * 0.5f + 10), new Vector2(38, 15), AdventureDirector.KindLabel(n.kind), color, UiSkin.Bg, 9);
                }
                if (here && st != null)
                    UiFactory.Label(root, "Left" + n.id, p + new Vector2(0, -nodeD * 0.5f - (roomy ? 24f : 11f)), new Vector2(110, 14), $"残り {st.spinsLeft}G", 10, TextAnchor.MiddleCenter, UiSkin.Gold);
            }
            return root;
        }

        /// <summary>そのステージから伸びる道（達成条件の先・確率の先・既定・戻り先）。</summary>
        private static IEnumerable<string> Targets(StageNode n)
        {
            var seen = new HashSet<string>();
            if (n.routeConditions != null)
                foreach (var c in n.routeConditions)
                    if (c != null && !string.IsNullOrEmpty(c.to) && seen.Add(c.to)) yield return c.to;
            if (n.routeDefault != null) foreach (var k in n.routeDefault.Keys) if (k != "NONE" && seen.Add(k)) yield return k;
            if (n.routeByFlag != null)
                foreach (var t in n.routeByFlag.Values)
                    if (t != null) foreach (var k in t.Keys) if (k != "NONE" && seen.Add(k)) yield return k;
            if (!string.IsNullOrEmpty(n.back) && seen.Add(n.back)) yield return n.back;
        }

        /// <summary>前へ進む道か（戻り道は薄く描く）。</summary>
        private static bool IsForward(StageNode from, string to)
        {
            if (from?.routeConditions != null)
                foreach (var c in from.routeConditions) if (c != null && c.to == to) return true;
            if (from?.routeDefault != null && from.routeDefault.ContainsKey(to)) return true;
            return false;
        }

        private static int IndexOf(AdventureState st, string id) => st == null ? -1 : st.visited.IndexOf(id);

        private static void Line(Transform parent, Vector2 a, Vector2 b, float thickness, Color color)
        {
            var d = b - a;
            var img = UiSkin.Img(parent, "Edge", (a + b) * 0.5f, new Vector2(d.magnitude, thickness), null, color);
            img.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }
    }
}
