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
            float padX = 70f, usableW = size.x - padX * 2;
            for (int ci = 0; ci < columns.Count; ci++)
            {
                var list = byCol[columns[ci]];
                float x = columns.Count == 1 ? 0 : -usableW * 0.5f + usableW * ci / (columns.Count - 1);
                float gap = Mathf.Min(120f, (size.y - 70f) / Mathf.Max(1, list.Count));
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
                    var col = walked ? UiSkin.Gold : planned ? UiSkin.Hex(AdventureDirector.ColorFor(cfg.Find(t))) : new Color(1, 1, 1, 0.18f);
                    Line(root, pos[n.id], pos[t], walked || planned ? 4f : 2f, col);
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
                if (here) UiSkin.Img(root, "Glow" + n.id, p, new Vector2(NodeD * 2.4f, NodeD * 2.4f), UiSkin.Glow(96), new Color(color.r, color.g, color.b, 0.55f));
                var ring = UiSkin.Img(root, "Ring" + n.id, p, new Vector2(NodeD + 6, NodeD + 6), UiSkin.Circle(64), known ? color : new Color(1, 1, 1, 0.12f));
                var body = UiSkin.Img(root, "Node" + n.id, p, new Vector2(NodeD, NodeD), UiSkin.Circle(64), known ? (been ? color : UiSkin.Panel) : UiSkin.Hex("#141a29"));
                var idLabel = UiFactory.Label(root, "Id" + n.id, p, new Vector2(NodeD, NodeD), known ? n.id : "?", known ? 12 : 16, TextAnchor.MiddleCenter, been ? UiSkin.Bg : UiSkin.Text);
                idLabel.fontStyle = FontStyle.Bold;
                string name = known ? n.name : "？？？";
                string kind = known ? AdventureDirector.KindLabel(n.kind) : "";
                UiFactory.Label(root, "Name" + n.id, p + new Vector2(0, -NodeD * 0.5f - 12), new Vector2(120, 16), name, 11, TextAnchor.MiddleCenter, known ? UiSkin.Text : UiSkin.TextDim);
                if (kind.Length > 0 && n.kind != "normal")
                    UiSkin.Chip(root, "Kind" + n.id, p + new Vector2(0, NodeD * 0.5f + 11), new Vector2(40, 16), kind, color, UiSkin.Bg, 10);
                if (here && st != null)
                    UiFactory.Label(root, "Left" + n.id, p + new Vector2(0, -NodeD * 0.5f - 27), new Vector2(120, 14), $"残り {st.spinsLeft} G", 10, TextAnchor.MiddleCenter, UiSkin.Gold);
            }
            return root;
        }

        private static IEnumerable<string> Targets(StageNode n)
        {
            var seen = new HashSet<string>();
            if (n.routeDefault != null) foreach (var k in n.routeDefault.Keys) if (k != "NONE" && seen.Add(k)) yield return k;
            if (n.routeByFlag != null)
                foreach (var t in n.routeByFlag.Values)
                    if (t != null) foreach (var k in t.Keys) if (k != "NONE" && seen.Add(k)) yield return k;
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
