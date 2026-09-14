using System;
using System.Collections.Generic;
using BBB.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace BBB.Runtime
{
    /// <summary>
    /// 街の地図（アトリエ風）。左に経路の地図、右に選んだ地点の詳細、上に所持の札 4 つ、下にボタン列。
    /// 2026-09-14 の指摘で組み直した: 面 3 段・地点 4 段・主ボタン 1 つ・数値は札（docs/ui_rules.md 11〜13）。
    /// 地図は ドラッグ = 移動 / ホイール・ピンチ = 拡大縮小 / ◎ = 現在地へ。
    /// </summary>
    public static class AtelierMap
    {
        // 舞台 960x540。縁からの余白 24、板どうし 20、下のボタン列は端から 24 上（docs/ui_rules.md 12）
        const float Margin = 24f, Gap = 20f;
        const float ViewW = 592f, ViewH = 340f, ViewX = -158f, ViewY = -12f;       // 左 -454 .. 138、上 158 .. 下 -182
        const float DetailW = 282f, DetailX = 313f;                                // 172 .. 454
        const float RowY = -224f, RowH = 44f;                                       // 下端 -246（端 -270 から 24）
        const float ColStep = 114f, RowStep = 72f, ContentPad = 64f, ContentTop = 40f;

        static readonly Color ViewBg = UiSkin.Hex("#193a3d"), DetailBg = UiSkin.Hex("#10292e"), Secondary = UiSkin.Hex("#24484c");
        static readonly Color EdgeCol = new Color(.95f, .93f, .9f, .16f), GhostEdge = new Color(.95f, .93f, .9f, .32f);
        static readonly Color NodeReach = UiSkin.Hex("#1f4a4b"), NodeUnknown = new Color(.1f, .2f, .22f, .9f), RouteDim = UiSkin.Hex("#55736b");
        static readonly Dictionary<string, Sprite> Thumbs = new Dictionary<string, Sprite>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() => Thumbs.Clear();

        static IEnumerable<string> Targets(StageNode n)
        {
            var set = new HashSet<string>();
            if (n.routeConditions != null) foreach (var c in n.routeConditions) if (c != null && !c.hidden && !string.IsNullOrEmpty(c.to) && set.Add(c.to)) yield return c.to;
            if (n.routeDefault != null) foreach (var id in n.routeDefault.Keys) if (id != "NONE" && set.Add(id)) yield return id;
            if (n.routeByFlag != null) foreach (var routes in n.routeByFlag.Values) if (routes != null) foreach (var id in routes.Keys) if (id != "NONE" && set.Add(id)) yield return id;
        }

        /// <summary>
        /// 地点の絵。冒険の背景アトラス（上から 遠景 / 中景 / 近景 の 3 段）を、本編の ParallaxBackground と同じ高さで
        /// 空のグラデーションの上に重ねる。アトラスが無い地点はタイトルの空。
        /// </summary>
        static void Landscape(RectTransform box, string id)
        {
            var prof = AdventureEnvironmentCatalog.Find(id);
            var tex = prof != null && !prof.UsesModules && !string.IsNullOrEmpty(prof.atlas) ? Resources.Load<Texture2D>(prof.atlas) : null;
            if (tex == null) { AtelierUi.Art(box, "Landscape", 0, 0, box.sizeDelta.x, box.sizeDelta.x * .6f, AtelierUi.Sprite("Art/UI/Title/sky_mountains_cloudsea")); return; }
            AdventureEnvironmentCatalog.Palette(prof.hour, out var top, out var bottom, out _);
            Stretch(UiSkin.Img(box, "SkyBottom", Vector2.zero, Vector2.zero, null, bottom), 0, 1);
            Stretch(UiSkin.Img(box, "SkyTop", Vector2.zero, Vector2.zero, UiSkin.GradientV(true), top), 0, 1);
            float[] cuts = { 0, prof.farEnd, prof.middleEnd, 1 };
            // (bottom, height) は ParallaxBackground.heights と同じ
            var heights = new[] { new Vector2(.13f, .9f), new Vector2(-.01f, .85f), new Vector2(-.035f, .34f) };
            for (int i = 0; i < 3; i++)
            {
                string key = id + "/" + i;
                if (!Thumbs.TryGetValue(key, out var sp) || sp == null)
                    Thumbs[key] = sp = Sprite.Create(tex, new Rect(0, tex.height * (1 - cuts[i + 1]), tex.width, tex.height * Mathf.Max(.01f, cuts[i + 1] - cuts[i])), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                var im = UiSkin.Img(box, "Row" + i, Vector2.zero, Vector2.zero, sp, Color.white);
                Stretch(im, heights[i].x, heights[i].x + heights[i].y);
            }
        }
        static void Stretch(Image im, float bottom, float top)
        {
            var rt = im.rectTransform; rt.anchorMin = new Vector2(0, bottom); rt.anchorMax = new Vector2(1, top);
            rt.offsetMin = rt.offsetMax = Vector2.zero; im.raycastTarget = false; im.preserveAspect = false;
        }

        enum State { Current, Visited, Reachable, Unknown }

        public static Action Build(Transform stage, SlotMachine m, Action adventure, Action shop, Action equip, Action trophy, Action settings, Action title, out Text message, bool inRun = false)
        {
            var bg = AtelierUi.Panel(stage, "AtelierMap", 0, 0, 960, 540, UiSkin.Hex("#122e32")); bg.gameObject.AddComponent<AtelierModalInput>();
            var cfg = m.Config.adventure;
            string lap = m.Adv.chapter > 1 ? $"  ·  {m.Adv.chapter} 周目" : "";
            AtelierUi.Text(bg, "Eyebrow", -264, 235, 380, 18, "CHAPTER " + Mathf.Max(1, m.Adv.chapter) + " / ROUTE MAP" + lap, 10, AtelierUi.Light);
            AtelierUi.Text(bg, "Heading", -264, 203, 380, 38, cfg?.chapterName ?? "冒険の準備", 28, AtelierUi.Light, true);

            // 上の札 4 つ（右端から）: ソウル / エンバー / 補給 / ライフ。アイコン + 小見出し + 太い値
            const float tileW = 118f, tileGap = 8f; float tx = 480 - Margin - tileW * .5f;
            var res = cfg?.resource;
            var souls = AtelierUi.Tile(bg, "TileSoul", tx, 219, tileW, AtelierUi.Icon("soul"), "所持ソウル", AtelierUi.Gold); tx -= tileW + tileGap;
            var ember = AtelierUi.Tile(bg, "TileEmber", tx, 219, tileW, AtelierUi.Icon("ember"), "冒険用エンバー"); tx -= tileW + tileGap;
            var torch = AtelierUi.Tile(bg, "TileSupply", tx, 219, tileW, AtelierUi.Sprite("Art/UI/Icons/compass"), res?.name ?? "補給"); tx -= tileW + tileGap;
            var life = AtelierUi.Tile(bg, "TileLife", tx, 219, tileW, AtelierUi.Icon("potion"), res?.hpName ?? "ライフ");

            AtelierUi.Text(bg, "MapHint", ViewX, 170, ViewW, 20, "ドラッグで移動  ·  ホイール / ピンチで拡大縮小  ·  「現在地」で戻る", 11, AtelierUi.Sub);

            // 地図の窓。板 → 縁 → 中身（拡大縮小する）→ 隅のボタン → 下の帯
            var view = AtelierUi.Panel(bg, "MapViewport", ViewX, ViewY, ViewW, ViewH, ViewBg); view.GetComponent<Image>().raycastTarget = true; view.gameObject.AddComponent<RectMask2D>();
            var terrain = AtelierUi.Art(view, "Terrain", 0, 0, 626, ViewH, AtelierUi.Sprite("Art/UI/Title/sky_mountains_cloudsea")); terrain.color = new Color(.21f, .32f, .28f, 1);
            var contours = UiSkin.Rect(view, "Contours", Vector2.zero, new Vector2(ViewW, ViewH)).gameObject.AddComponent<AtelierContourGraphic>(); contours.color = new Color(.67f, .79f, .66f, .15f); contours.raycastTarget = false;
            var content = UiSkin.Rect(view, "MapContent", Vector2.zero, new Vector2(960, 600)); content.anchorMin = content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0, 1);
            var scroll = view.gameObject.AddComponent<ScrollRect>(); MotionSound.Attach(scroll);
            scroll.viewport = view; scroll.content = content; scroll.horizontal = true; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 0;                                   // ホイールは移動ではなく拡大縮小に使う
            var zoom = view.gameObject.AddComponent<AtelierMapZoom>(); zoom.Init(view, content);
            AtelierUi.Edge(view, EdgeCol);
            // 隅のボタン（＋ / － / ◎）。押せる面は 44
            float bx = ViewW * .5f - 8 - 22, by = ViewH * .5f - 8 - 22;
            AtelierUi.Button(view, "ZoomIn", bx, by, 44, "+", () => zoom.Step(1.25f), AtelierUi.Ink, AtelierUi.Light, 44, GhostEdge, 22);
            AtelierUi.Button(view, "ZoomOut", bx, by - 50, 44, "−", () => zoom.Step(1 / 1.25f), AtelierUi.Ink, AtelierUi.Light, 44, GhostEdge, 22);
            var homeBtn = AtelierUi.Button(view, "Home", bx, by - 100, 44, "現在地", null, AtelierUi.Ink, AtelierUi.Gold, 44, GhostEdge, 10);
            // 下の帯: 到着の知らせや出発できない理由。文が空なら消える
            var band = AtelierUi.Panel(view, "MessageBand", 0, -ViewH * .5f + 14, ViewW, 28, new Color(.06f, .13f, .16f, .82f));
            message = AtelierUi.Text(band, "Message", 6, 0, ViewW - 24, 24, "", 12, AtelierUi.Gold);
            band.gameObject.AddComponent<AtelierMessageBand>().Init(band.GetComponent<Image>(), message);

            var detail = AtelierUi.Panel(bg, "Destination", DetailX, ViewY, DetailW, ViewH, DetailBg); AtelierUi.Edge(detail, EdgeCol);

            // 下のボタン列。主（金）は右の板の「冒険へ進む」だけ。ここは副（濃い面 + 縁）と、戻るは縁だけ
            float x = -480 + Margin;
            Button(bg, "Town", ref x, 150, inRun ? "街へ戻る" : "街のショップ", shop, false);
            Button(bg, "Equipment", ref x, 108, "装備", equip, false);
            Button(bg, "Trophies", ref x, 108, "実績", trophy, false);
            Button(bg, "Settings", ref x, 108, "設定", settings, false);
            AtelierUi.Button(bg, "Title", 480 - Margin - 75, RowY, 150, inRun ? "マップを閉じる" : "タイトルへ", title, new Color(0, 0, 0, .001f), AtelierUi.Sub, RowH, GhostEdge);

            string selected = m.Adv.nodeId; bool first = true;
            var positions = new Dictionary<string, Vector2>();
            homeBtn.onClick.AddListener(() => { if (positions.TryGetValue(m.Adv.nodeId, out var c)) zoom.CenterOn(c, 1f); });

            void Refresh()
            {
                souls.text = m.Wallet.Souls.ToString("N0");
                ember.text = m.Credit.ToString("N0");
                torch.text = Mathf.Max(0, m.Adv.torches).ToString("N0");
                life.text = $"{m.Hp:N0} / {m.HpMax:N0}";
                AtelierUi.Clear(content); AtelierUi.Clear(detail);
                var nodes = cfg?.nodes ?? new List<StageNode>();
                var groups = new Dictionary<string, List<StageNode>>(); var columns = new List<string>();
                foreach (var n in nodes) { if (n == null) continue; string c = n.column ?? "?"; if (!groups.ContainsKey(c)) { groups[c] = new List<StageNode>(); columns.Add(c); } groups[c].Add(n); }
                int maxRows = 1; foreach (var col in columns) maxRows = Mathf.Max(maxRows, groups[col].Count);
                float w = Mathf.Max(ViewW, columns.Count * ColStep + ContentPad * 2 - ColStep), h = Mathf.Max(ViewH, (maxRows - 1) * RowStep + ContentTop + 60);
                content.sizeDelta = new Vector2(w, h);
                positions.Clear();
                float rowsTop = ContentTop + (h - ContentTop - 40 - (maxRows - 1) * RowStep) * .5f;    // 段を縦の中央に
                for (int ci = 0; ci < columns.Count; ci++)
                {
                    var ns = groups[columns[ci]]; float cx = ContentPad + ci * ColStep;
                    float top = rowsTop + (maxRows - ns.Count) * RowStep * .5f;
                    for (int j = 0; j < ns.Count; j++) positions[ns[j].id] = new Vector2(cx, -(top + j * RowStep));
                }
                var visited = new HashSet<string>(m.Adv.visited); visited.Add(m.Adv.nodeId);
                var reachable = new HashSet<string>();
                foreach (var n in nodes) if (n != null && visited.Contains(n.id)) foreach (var id in Targets(n)) if (!visited.Contains(id)) reachable.Add(id);
                if (!string.IsNullOrEmpty(m.Adv.nextId) && !visited.Contains(m.Adv.nextId)) reachable.Add(m.Adv.nextId);
                State StateOf(StageNode n) => n.id == m.Adv.nodeId ? State.Current : visited.Contains(n.id) ? State.Visited : reachable.Contains(n.id) ? State.Reachable : State.Unknown;

                // 経路: 通過 = 太い金 / 次に行ける = 薄緑 / それ以外 = 細く暗く
                foreach (var n in nodes)
                {
                    if (n == null || !positions.ContainsKey(n.id) || (cfg.hideRoute && !visited.Contains(n.id))) continue;
                    foreach (var id in Targets(n))
                    {
                        if (!positions.TryGetValue(id, out var b)) continue;
                        var a = positions[n.id]; var d = b - a;
                        bool walked = visited.Contains(n.id) && visited.Contains(id), next = visited.Contains(n.id) && reachable.Contains(id);
                        var line = AtelierUi.Panel(content, "Route", (a.x + b.x) * .5f, (a.y + b.y) * .5f, d.magnitude, walked ? 3 : next ? 2 : 1.5f, walked ? AtelierUi.Gold : next ? AtelierUi.Mint : new Color(RouteDim.r, RouteDim.g, RouteDim.b, .55f));
                        line.anchorMin = line.anchorMax = new Vector2(0, 1); line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                    }
                }
                // 地点: 現在地（金・光輪）/ 通過（薄緑）/ 行ける（金の縁）/ 未発見（小さく暗く）
                foreach (var n in nodes)
                {
                    if (n == null || !positions.ContainsKey(n.id)) continue;
                    var st = StateOf(n);
                    if (cfg.hideRoute && st == State.Unknown && !reachable.Contains(n.id)) continue;
                    bool known = !cfg.hideRoute || visited.Contains(n.id); var p = positions[n.id]; var node = n;
                    float d = st == State.Current ? 52 : st == State.Unknown ? 36 : 46;
                    string glyph = st == State.Current ? "◆" : st == State.Visited ? "✓" : st == State.Reachable ? Branch(n.id) : "?";
                    Color fill = st == State.Current ? AtelierUi.Gold : st == State.Visited ? AtelierUi.Mint : st == State.Reachable ? NodeReach : NodeUnknown;
                    Color ink = st == State.Current || st == State.Visited ? AtelierUi.Ink : st == State.Reachable ? AtelierUi.Light : AtelierUi.Sub;
                    var root = UiSkin.Rect(content, "Node_" + n.id, p, new Vector2(d, d)); root.anchorMin = root.anchorMax = new Vector2(0, 1);
                    if (st == State.Current)
                    {
                        var halo = UiSkin.Img(root, "Halo", Vector2.zero, new Vector2(d + 16, d + 16), UiSkin.Circle(64), new Color(AtelierUi.Gold.r, AtelierUi.Gold.g, AtelierUi.Gold.b, .45f));
                        halo.raycastTarget = false; halo.gameObject.AddComponent<AtelierPulse>();
                    }
                    if (n.id == selected) UiSkin.Img(root, "Selected", Vector2.zero, new Vector2(d + 10, d + 10), UiSkin.Circle(64), AtelierUi.Light).raycastTarget = false;
                    if (st == State.Reachable) UiSkin.Img(root, "Ring", Vector2.zero, new Vector2(d + 4, d + 4), UiSkin.Circle(64), AtelierUi.Gold).raycastTarget = false;
                    if (st == State.Unknown) UiSkin.Img(root, "Ring", Vector2.zero, new Vector2(d + 2, d + 2), UiSkin.Circle(64), new Color(RouteDim.r, RouteDim.g, RouteDim.b, .7f)).raycastTarget = false;
                    var body = UiSkin.Img(root, "Body", Vector2.zero, new Vector2(d, d), UiSkin.Circle(64), fill, true);
                    var t = AtelierUi.Text(root, "Glyph", 0, 1, d, d, glyph, st == State.Unknown ? 13 : 16, ink, true, TextAnchor.MiddleCenter);
                    var btn = root.gameObject.AddComponent<Button>(); btn.targetGraphic = body;
                    var cb = btn.colors; cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f); cb.selectedColor = new Color(1.2f, 1.2f, 1.2f); cb.pressedColor = new Color(.7f, .8f, .85f); btn.colors = cb;
                    MotionSound.Attach(btn); btn.onClick.AddListener(() => MotionSound.Invoke("Node", () => { selected = node.id; Refresh(); }));
                    root.gameObject.AddComponent<AtelierFocus>();
                    var label = AtelierUi.Text(content, "NodeLabel_" + n.id, p.x, p.y - d * .5f - 16, 110, 16, known ? n.id + "  " + n.name : n.id + "  未発見", 11, st == State.Unknown ? AtelierUi.Sub : AtelierUi.Light, st == State.Current, TextAnchor.MiddleCenter);
                    label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0, 1);
                }
                if (first && positions.TryGetValue(m.Adv.nodeId, out var current)) { Canvas.ForceUpdateCanvases(); zoom.CenterOn(current, 1f); first = false; }

                // 右の板: 絵 → 名前 → 事実 → 分岐条件 → 出発（主ボタンはこれだけ）
                var chosen = cfg?.Find(selected); bool isKnown = chosen != null && (!cfg.hideRoute || visited.Contains(chosen.id));
                var artBox = UiSkin.Rect(detail, "ArtBox", new Vector2(0, 110), new Vector2(DetailW - 32, 96)); artBox.gameObject.AddComponent<RectMask2D>();
                Landscape(artBox, isKnown ? chosen.id : null);
                var stTxt = chosen == null ? "" : chosen.id == m.Adv.nodeId ? "現在地" : visited.Contains(chosen.id) ? "通過済み" : reachable.Contains(chosen.id) ? "次に行ける" : "未発見";
                var stCol = chosen == null ? AtelierUi.Sub : chosen.id == m.Adv.nodeId ? AtelierUi.Gold : visited.Contains(chosen.id) ? AtelierUi.Mint : reachable.Contains(chosen.id) ? AtelierUi.Light : AtelierUi.Sub;
                AtelierUi.Text(detail, "State", 0, 53, DetailW - 32, 14, stTxt, 10, stCol, true);
                AtelierUi.Text(detail, "Name", 0, 29, DetailW - 32, 30, isKnown ? chosen.name : chosen != null ? "未発見の地点" : "冒険へ", 22, AtelierUi.Light, true);
                AtelierUi.Text(detail, "Facts", 0, -19, DetailW - 32, 60, isKnown ? $"{chosen.id}  /  深さ {chosen.depth}\n滞在 {chosen.spins} G  /  初到達 {chosen.firstVisitSouls} ソウル\n{(chosen.id == m.Adv.nodeId ? "残り " + m.Adv.spinsLeft + " G" : "通過ルートを確認中")}" : "先の地点は冒険を進めると判明します。\n進路は小役と達成条件で決まります。", 13, AtelierUi.Sub);
                string routeInfo = "進路は冒険中に決定します。";
                if (isKnown && chosen.id == m.Adv.nodeId) routeInfo = m.Adv.nextId != null ? "次の進路：" + m.Adv.nextId : "進路はまだ決まっていません";
                AtelierUi.Button(detail, "Conditions", 0, -82, DetailW - 32, "分岐条件を見る", () =>
                {
                    var text = new System.Text.StringBuilder(routeInfo);
                    if (isKnown && chosen.routeConditions != null) foreach (var c in chosen.routeConditions)
                    {
                        if (c == null) continue; bool met = AdventureDirector.Meets(c, m.Adv, m.Credit, m.Wallet.Souls, m.PlayerLevel);
                        if (c.hidden && !met) continue;
                        text.Append("\n\n→ ").Append(c.to).Append("  ").Append(AdventureDirector.DescribeCondition(c));
                        if (chosen.id == m.Adv.nodeId) text.Append("\n").Append(met ? "達成" : AdventureDirector.ProgressText(c, m.Adv, m.Credit, m.Wallet.Souls, m.PlayerLevel));
                    }
                    var panel = AtelierUi.Screen(bg, "RouteConditions", AtelierUi.Night, null, out var dialog);
                    AtelierUi.Header(panel, "ROUTE CONDITIONS", isKnown ? chosen.name : "未発見の地点", AtelierUi.Light);
                    var viewport = UiSkin.Rect(panel, "Viewport", new Vector2(0, -6), new Vector2(860, 352)); viewport.gameObject.AddComponent<RectMask2D>(); viewport.gameObject.AddComponent<Image>().color = Color.clear;
                    var label = AtelierUi.Text(viewport, "Conditions", 0, 0, 828, 800, text.ToString(), 18, AtelierUi.Light); label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, 1); label.rectTransform.pivot = new Vector2(.5f, 1); label.alignment = TextAnchor.UpperLeft; label.rectTransform.sizeDelta = new Vector2(828, Mathf.Max(352, label.preferredHeight + 20));
                    var sc = viewport.gameObject.AddComponent<ScrollRect>(); MotionSound.Attach(sc); sc.viewport = viewport; sc.content = label.rectTransform; sc.horizontal = false; sc.movementType = ScrollRect.MovementType.Clamped; sc.scrollSensitivity = 32;
                    AtelierUi.Button(panel, "Back", 0, -231, 260, "地図へ戻る", () => UnityEngine.Object.Destroy(dialog), AtelierUi.Gold, AtelierUi.Ink);
                }, Secondary, AtelierUi.Light, 44, EdgeCol);
                AtelierUi.Button(detail, "Depart", 0, -134, DetailW - 32, inRun ? "冒険に戻る  →" : "冒険へ進む  →", adventure, AtelierUi.Gold, AtelierUi.Ink, 44, null, 15);
            }
            Refresh(); return Refresh;
        }

        /// <summary>下の列の副ボタン。左から順に幅を取り、x を進める。</summary>
        static void Button(Transform p, string name, ref float x, float w, string text, Action action, bool primary)
        {
            AtelierUi.Button(p, name, x + w * .5f, RowY, w, text, action, primary ? AtelierUi.Gold : Secondary, primary ? AtelierUi.Ink : AtelierUi.Light, RowH, primary ? null : (Color?)EdgeCol);
            x += w + 10;
        }

        static string Branch(string id) { int dash = id.IndexOf('-'); return dash >= 0 && dash + 1 < id.Length ? id.Substring(dash + 1) : id; }
    }

    /// <summary>
    /// 地図の拡大縮小。ホイール = カーソル位置を中心に、ピンチ = 2 本の指の中点を中心に、0.5〜2.0 倍。
    /// 中身は pivot (0,1)・anchor (0,1) のまま localScale を変え、位置を補正して指の下の点を動かさない。
    /// </summary>
    public sealed class AtelierMapZoom : MonoBehaviour, IScrollHandler
    {
        public const float Min = .5f, Max = 2f;
        RectTransform view, content;
        float pinchDist = -1;
        public float Zoom { get; private set; } = 1f;
        public void Init(RectTransform viewport, RectTransform body) { view = viewport; content = body; }

        public void OnScroll(PointerEventData e)
        {
            if (view == null || Mathf.Approximately(e.scrollDelta.y, 0)) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(view, e.position, e.pressEventCamera ?? e.enterEventCamera, out var local);
            ZoomAt(local, e.scrollDelta.y > 0 ? 1.15f : 1 / 1.15f);
        }
        /// <summary>隅のボタン。窓の中心を基準に一段。</summary>
        public void Step(float factor) => ZoomAt(Vector2.zero, factor);

        /// <summary>viewLocal（窓の中心が原点）にある中身の点を動かさずに倍率を掛ける。</summary>
        public void ZoomAt(Vector2 viewLocal, float factor)
        {
            float z0 = Zoom, z1 = Mathf.Clamp(z0 * factor, Min, Max);
            if (Mathf.Approximately(z0, z1)) return;
            var origin = new Vector2(-view.rect.width * .5f, view.rect.height * .5f);        // 中身の anchor（窓の左上）
            var p = (viewLocal - origin - content.anchoredPosition) / z0;                      // 中身の座標（等倍）
            Zoom = z1; content.localScale = new Vector3(z1, z1, 1);
            content.anchoredPosition += p * (z0 - z1);
            Clamp();
        }
        /// <summary>中身の点 p（等倍座標、y は下向きに負）を窓の中央に出す。</summary>
        public void CenterOn(Vector2 p, float? zoom = null)
        {
            if (zoom != null) { Zoom = Mathf.Clamp(zoom.Value, Min, Max); content.localScale = new Vector3(Zoom, Zoom, 1); }
            content.anchoredPosition = new Vector2(view.rect.width * .5f - p.x * Zoom, -view.rect.height * .5f - p.y * Zoom);
            Clamp();
        }
        /// <summary>窓の外に余白が出ないように寄せる。中身が窓より小さい軸は中央に置く。</summary>
        void Clamp()
        {
            float W = view.rect.width, H = view.rect.height, w = content.rect.width * Zoom, h = content.rect.height * Zoom;
            var a = content.anchoredPosition;
            a.x = w <= W ? (W - w) * .5f : Mathf.Clamp(a.x, W - w, 0);
            a.y = h <= H ? -(H - h) * .5f : Mathf.Clamp(a.y, 0, h - H);
            content.anchoredPosition = a;
            GetComponent<ScrollRect>()?.StopMovement();
        }

        void Update()
        {
            var ts = Touchscreen.current; if (ts == null || view == null) return;
            int count = 0; Vector2 a = default, b = default;
            foreach (var t in ts.touches) { if (!t.press.isPressed) continue; if (count == 0) a = t.position.ReadValue(); else if (count == 1) b = t.position.ReadValue(); count++; }
            if (count < 2) { pinchDist = -1; return; }
            float dist = Vector2.Distance(a, b);
            if (pinchDist > 0 && dist > 1)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(view, (a + b) * .5f, null, out var local);
                ZoomAt(local, dist / pinchDist);
            }
            pinchDist = dist;
        }
    }

    /// <summary>現在地の光輪。1.6 秒で広がりながら薄くなるのを繰り返す。</summary>
    public sealed class AtelierPulse : MonoBehaviour
    {
        Image im; Color baseColor; Vector2 baseSize;
        void Awake() { im = GetComponent<Image>(); baseColor = im.color; baseSize = im.rectTransform.sizeDelta; }
        void Update()
        {
            float t = (Time.time % 1.6f) / 1.6f;
            im.rectTransform.sizeDelta = baseSize * (1 + t * .35f);
            var c = baseColor; c.a = baseColor.a * (1 - t); im.color = c;
        }
    }

    /// <summary>知らせの帯。文が空のときは帯ごと消す。</summary>
    public sealed class AtelierMessageBand : MonoBehaviour
    {
        Image bg; Text text;
        public void Init(Image band, Text label) { bg = band; text = label; Refresh(); }
        public void Refresh() { if (bg != null && text != null) bg.enabled = text.enabled = !string.IsNullOrEmpty(text.text); }
        void Update() => Refresh();
    }
}
