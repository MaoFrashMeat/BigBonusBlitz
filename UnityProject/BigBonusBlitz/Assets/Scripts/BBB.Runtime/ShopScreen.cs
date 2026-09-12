using System.Collections.Generic;
using BBB.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// 街のショップ。ソウルでスキル（青）と装備（金）を買う。買うとレベルが上がり効果量が増える。
    /// 品揃えと効果量は game_config.json の shop で決まる。
    /// </summary>
    public static class ShopScreen
    {
        /// <summary>ショップを開く。返した GameObject を Destroy すれば閉じる。</summary>
        public static GameObject Build(Transform stage, SlotMachine m, AudioManager audio, System.Action onSoulsChanged, System.Action onClose)
        {
            const float W = 900f, H = 510f;
            var overlay = UiFactory.Panel(stage, "ShopOverlay", Vector2.zero, new Vector2(4000, 4000), new Color(0, 0, 0, 0.86f));
            var eatBg = overlay.gameObject.AddComponent<Button>();
            eatBg.transition = Selectable.Transition.None;
            eatBg.onClick.AddListener(() => onClose?.Invoke());

            var card = Card(overlay, "ShopCard", Vector2.zero, new Vector2(W, H), 16);
            var eat = card.gameObject.AddComponent<Button>();
            eat.transition = Selectable.Transition.None;

            // Header, wallet and navigation occupy separate rows.
            const float headY = 145f, tabW = 160f;
            const float tab1Cx = -346f, tab2Cx = -174f, tab3Cx = -2f;
            Label(card, "Eyebrow", new Vector2(-270, 228), new Vector2(312, 16), "TOWN / TRAVELER'S EMPORIUM", 10, TextAnchor.MiddleLeft, Gold);
            var title = Label(card, "Title", new Vector2(-270, 201), new Vector2(312, 34), "街のショップ", 28, TextAnchor.MiddleLeft, UiSkin.Text);
            title.fontStyle = FontStyle.Bold;
            var soul = Wallet(card, "Soul", new Vector2(124, 212), "ソウル / 強化", Blue, "soul");
            var ember = Wallet(card, "Ember", new Vector2(300, 212), "エンバー / 補給", Gold, "ember");
            Button(card, "Close", new Vector2(408, 213), new Vector2(44, 44), "×", () => onClose?.Invoke(), Surface, 24);
            UiSkin.Img(card, "HeaderLine", new Vector2(0, 177), new Vector2(W - 48, 1), null, Edge);
            Label(card, "NavHint", new Vector2(293, headY), new Vector2(264, 22), "旅支度を整え、次の冒険へ", 13, TextAnchor.MiddleRight, Muted);
            // ===== タブ（装備・スキル / 補給）=====
            var res = m.Config.adventure?.resource;
            bool hasSupply = m.AdventureEnabled && res != null && res.enabled;
            var statsCfg = m.Config.stats;
            bool hasStats = statsCfg != null && statsCfg.enabled;
            var gridRoot = UiSkin.Rect(card, "GridRoot", Vector2.zero, new Vector2(W, H));
            var supplyRoot = UiSkin.Rect(card, "SupplyRoot", Vector2.zero, new Vector2(W, H));
            var statsRoot = UiSkin.Rect(card, "StatsRoot", Vector2.zero, new Vector2(W, H));
            supplyRoot.gameObject.SetActive(false);
            statsRoot.gameObject.SetActive(false);
            Text note = null;
            Button tabGear = null, tabSupply = null, tabStats = null;
            void SetTab(int tab)   // 0=装備 1=補給 2=ステータス
            {
                if (note != null)
                {
                    note.text = tab == 0 ? "ソウルで装備・スキルを強化できます" : tab == 1 ? "エンバーを使って、冒険の備えを整えましょう" : "ポイントを割り振って、自分だけの戦い方に";
                    note.color = Muted;
                }
                gridRoot.gameObject.SetActive(tab == 0);
                supplyRoot.gameObject.SetActive(tab == 1);
                statsRoot.gameObject.SetActive(tab == 2);
                if (tabGear != null) UiSkin.SetButtonColor(tabGear, tab == 0 ? Blue : UiSkin.Btn, tab == 0 ? UiSkin.Bg : UiSkin.Text);
                if (tabSupply != null) UiSkin.SetButtonColor(tabSupply, tab == 1 ? UiSkin.Green : UiSkin.Btn, tab == 1 ? UiSkin.Bg : UiSkin.Text);
                if (tabStats != null) UiSkin.SetButtonColor(tabStats, tab == 2 ? Gold : UiSkin.Btn, tab == 2 ? UiSkin.Bg : UiSkin.Text);
            }
            if (hasSupply || hasStats)
            {
                tabGear = Button(card, "TabGear", new Vector2(tab1Cx, headY), new Vector2(tabW, 44), "装備・スキル", () => { audio?.UiPop(); SetTab(0); }, Blue, 15, false, 8);
                if (hasSupply) tabSupply = Button(card, "TabSupply", new Vector2(tab2Cx, headY), new Vector2(tabW, 44), "補給", () => { audio?.UiPop(); SetTab(1); }, Surface, 15, false, 8);
                if (hasStats) tabStats = Button(card, "TabStats", new Vector2(tab3Cx, headY), new Vector2(tabW, 44), "ステータス", () => { audio?.UiPop(); SetTab(2); }, Surface, 15, false, 8);
            }

            note = Label(card, "Note", new Vector2(0, -H * 0.5f + 22), new Vector2(W - 60, 24), "ソウルで装備・スキルを強化できます", 13, TextAnchor.MiddleCenter, Muted);

            UiSkin.Img(card, "FooterLine", new Vector2(0, -214), new Vector2(W - 48, 1), null, Edge);
            var cfg = m.Config.shop;
            var items = cfg?.items ?? new List<ShopItem>();
            var rows = new List<System.Action>();

            // ===== ステータスタブ: ライフ / テクニック / ラック =====
            if (hasStats)
            {
                var head = Label(statsRoot, "StatHead", new Vector2(0, 102), new Vector2(W - 60, 20), "", 14, TextAnchor.MiddleCenter, UiSkin.Text);
                head.fontStyle = FontStyle.Bold;

                const float colW = 272f;
                var addButtons = new List<Button>();
                var valueTexts = new List<Text>();
                var effectTexts = new List<Text>();
                var nextTexts = new List<Text>();
                var colors = new[] { UiSkin.Hex("#3ddc84"), UiSkin.Hex("#ff7a45"), UiSkin.Hex("#ffcf3f") };

                for (int i = 0; i < StatsDirector.Keys.Length; i++)
                {
                    string key = StatsDirector.Keys[i];
                    var col = colors[i];
                    float cx = (i - 1) * (colW + 12f);
                    var box = Card(statsRoot, "Stat_" + key, new Vector2(cx, -43), new Vector2(colW, 258), 12, Surface);
                    UiSkin.Img(box, "Bar", new Vector2(0, 122), new Vector2(colW - 2, 5), UiSkin.Rounded(2), col);
                    var nm = Label(box, "Name", new Vector2(0, 99), new Vector2(colW - 20, 24), StatsDirector.DisplayName(key), 19, TextAnchor.MiddleCenter, UiSkin.Text);
                    nm.fontStyle = FontStyle.Bold;
                    Label(box, "Sum", new Vector2(0, 71), new Vector2(colW - 32, 30), StatsDirector.Summary(key), 13, TextAnchor.UpperCenter, Muted);
                    var val = UiSkin.Number(box, "Val", new Vector2(0, 34), new Vector2(colW - 20, 34), "0", 30, col);
                    val.alignment = TextAnchor.MiddleCenter;
                valueTexts.Add(val);
                    var eff = Label(box, "Eff", new Vector2(0, -16), new Vector2(colW - 32, 56), "", 13, TextAnchor.UpperLeft, UiSkin.Text);
                    effectTexts.Add(eff);
                    var nxt = Label(box, "Next", new Vector2(0, -63), new Vector2(colW - 32, 28), "", 12, TextAnchor.UpperCenter, Muted);
                    nextTexts.Add(nxt);
                    string k = key;
                    var add = Button(box, "Add", new Vector2(0, -102), new Vector2(colW - 32, 44), "＋ 1 振る", () =>
                    {
                        if (!StatsDirector.Spend(statsCfg, m.Stats, k)) { note.text = "振れるポイントがありません"; note.color = UiSkin.Accent; RefreshAll(); return; }
                        audio?.UiPop();
                        SaveData.Save(m, audio);
                        note.text = $"{StatsDirector.DisplayName(k)} を上げました";
                        note.color = UiSkin.Green;
                        RefreshAll();
                    }, col, 14, false, 8);
                    addButtons.Add(add);
                }

                var respec = Button(statsRoot, "Respec", new Vector2(0, -190), new Vector2(280, 36), "", () =>
                {
                    int cost = m.Adv.chapter > 1 && statsCfg.freeRespecOnChapterClear ? 0 : Mathf.Max(0, statsCfg.respecCost);
                    if (m.Wallet.Souls < cost) { note.text = "ソウルが足りません"; note.color = UiSkin.Accent; RefreshAll(); return; }
                    m.Wallet.Souls -= cost;
                    int back = StatsDirector.Respec(m.Stats);
                    audio?.UiPop();
                    SaveData.Save(m, audio);
                    note.text = $"{back} ポイントを戻しました";
                    note.color = UiSkin.Green;
                    RefreshAll();
                }, Surface, 15, false, 8);

                rows.Add(() =>
                {
                    head.text = $"Lv {m.PlayerLevel}   振れるポイント {m.Stats.Unspent}";
                    head.color = m.Stats.Unspent > 0 ? Gold : Muted;
                    for (int i = 0; i < StatsDirector.Keys.Length; i++)
                    {
                        string key = StatsDirector.Keys[i];
                        int cur = m.Stats.Get(key);
                        valueTexts[i].text = $"{cur} / {statsCfg.maxPerStat}";
                        effectTexts[i].text = StatsDirector.Effects(statsCfg, m.Stats, key);
                        nextTexts[i].text = cur < statsCfg.maxPerStat ? StatsDirector.Describe(statsCfg, key, cur) : "これ以上は上げられない";
                        addButtons[i].interactable = m.Stats.Unspent > 0 && cur < statsCfg.maxPerStat;
                    }
                    int cost = m.Adv.chapter > 1 && statsCfg.freeRespecOnChapterClear ? 0 : Mathf.Max(0, statsCfg.respecCost);
                    UiSkin.SetButtonText(respec, cost == 0 ? "振り直す（無料）" : $"振り直す   {cost:N0} ソウル");
                    respec.interactable = m.Stats.Total > 0 && m.Wallet.Souls >= cost;
                });
            }

            // ===== 補給タブ: 回復薬とエンバー =====
            if (hasSupply)
            {
                var head = Label(supplyRoot, "SupHead", new Vector2(0, 102), new Vector2(W - 60, 20), "冒険に持っていくもの（エンバーで買う）", 14, TextAnchor.MiddleCenter, Muted);

                var torchCard = Card(supplyRoot, "TorchCard", new Vector2(-215, -29), new Vector2(414, 232), 12, Surface);
                UiSkin.Img(torchCard, "Bar", new Vector2(0, 114), new Vector2(382, 2), UiSkin.Rounded(2), UiSkin.Hex("#e08a2a"));
                var tName = Label(torchCard, "Name", new Vector2(0, 76), new Vector2(300, 26), res.name, 20, TextAnchor.MiddleCenter, UiSkin.Text);
                tName.fontStyle = FontStyle.Bold;
                var tHave = UiSkin.Number(torchCard, "Have", new Vector2(0, 29), new Vector2(382, 32), "", 22, UiSkin.Hex("#ffb45c"));
                tHave.alignment = TextAnchor.MiddleCenter;
                var tDesc = Label(torchCard, "Desc", new Vector2(0, -15), new Vector2(300, 18), "", 12, TextAnchor.MiddleCenter, Muted);
                Button buyTorch = null;
                buyTorch = Button(torchCard, "Buy", new Vector2(0, -77), new Vector2(366, 48), "", () =>
                {
                    int cost = Mathf.Max(0, res.torchCost);
                    if (m.Wallet.Embers < cost) { note.text = "エンバーが足りません"; note.color = UiSkin.Accent; RefreshAll(); return; }
                    if (AdventureDirector.AddTorch(m.Config.adventure, m.Adv, 1, m.TorchSpinsPerUnit) <= 0)
                    { note.text = $"{res.name} はもう持てません"; note.color = UiSkin.Accent; RefreshAll(); return; }
                    m.Wallet.Embers -= cost;
                    audio?.UiPop();
                    SaveData.Save(m, audio);
                    note.text = $"{res.name} を 1 個 買いました";
                    note.color = UiSkin.Green;
                    RefreshAll();
                }, UiSkin.Hex("#e08a2a"), 15, false, 10);

                var coinCard = Card(supplyRoot, "CoinCard", new Vector2(215, -29), new Vector2(414, 232), 12, Surface);
                UiSkin.Img(coinCard, "Bar", new Vector2(0, 114), new Vector2(382, 2), UiSkin.Rounded(2), Gold);
                var cName = Label(coinCard, "Name", new Vector2(0, 76), new Vector2(300, 26), "灯火（エンバー）", 18, TextAnchor.MiddleCenter, UiSkin.Text);
                cName.fontStyle = FontStyle.Bold;
                var cHave = UiSkin.Number(coinCard, "Have", new Vector2(0, 29), new Vector2(382, 32), "", 22, Gold);
                cHave.alignment = TextAnchor.MiddleCenter;
                var cDesc = Label(coinCard, "Desc", new Vector2(0, -15), new Vector2(300, 18), "", 12, TextAnchor.MiddleCenter, Muted);
                Button buyCredit = null;
                buyCredit = Button(coinCard, "Buy", new Vector2(0, -77), new Vector2(366, 48), "", () =>
                {
                    int cost = Mathf.Max(0, res.creditCost);
                    if (m.Wallet.Embers < cost) { note.text = "エンバーが足りません"; note.color = UiSkin.Accent; RefreshAll(); return; }
                    m.Wallet.Embers -= cost;
                    m.Credit += Mathf.Max(0, res.creditAmount);
                    audio?.UiPop();
                    SaveData.Save(m, audio);
                    note.text = $"エンバー {res.creditAmount} を分けてもらいました";
                    note.color = UiSkin.Green;
                    RefreshAll();
                }, Gold, 15, false, 10);

                Label(supplyRoot, "SupNote", new Vector2(0, -181), new Vector2(W - 90, 44),
                    $"{res.hpName} は通常時の 1G につき 1 減る。0 になるかエンバーが尽きると力尽きて、章の最初からやり直しになる。ボーナス中のベルでも回復する。", 12, TextAnchor.MiddleCenter, Muted);

                rows.Add(() =>
                {
                    int per = m.TorchSpinsPerUnit;
                    int have = Mathf.Max(0, m.Adv.torches);
                    tHave.text = $"{have} / {res.maxTorches} 個   {res.hpName} {m.Hp} / {m.HpMax}";
                    tDesc.text = $"1 個で {res.hpName} が {per} 回復する";
                    UiSkin.SetButtonText(buyTorch, $"購入  {res.torchCost:N0} エンバー");
                    buyTorch.interactable = m.Wallet.Embers >= res.torchCost && have < res.maxTorches;
                    cHave.text = $"{m.Credit:N0}";
                    cDesc.text = $"1 口で {res.creditAmount} 分けてもらう";
                    UiSkin.SetButtonText(buyCredit, $"交換  {res.creditCost:N0} エンバー");
                    buyCredit.interactable = m.Wallet.Embers >= res.creditCost;
                });
            }

            void RefreshAll()
            {
                soul.text = $"{m.Wallet.Souls:N0}";
                ember.text = $"{m.Wallet.Embers:N0}";
                foreach (var r in rows) r();
                onSoulsChanged?.Invoke();
            }

            if (items.Count == 0)
            {
                Label(gridRoot, "Empty", Vector2.zero, new Vector2(W - 60, 40), "ただいま商品を準備しています", 14, TextAnchor.MiddleCenter, Muted);
                RefreshAll();
                return overlay.gameObject;
            }

            // 3 列の小さな札で並べる（900 幅の窓に 9 品まで一度に見える。多ければ縦に送る）
            const int cols = 3;
            const float rowW = 274f, rowH = 96f, gap = 10f, viewH = 316f;
            var viewport = UiSkin.Rect(gridRoot, "Viewport", new Vector2(-5, -46), new Vector2(842, viewH));
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            int gridRows = Mathf.CeilToInt(items.Count / (float)cols);
            float contentH = Mathf.Max(viewH, gridRows * (rowH + gap) - gap);
            var content = UiSkin.Rect(viewport, "Content", Vector2.zero, new Vector2(842, contentH));
            content.anchorMin = content.anchorMax = new Vector2(.5f, 1);
            content.pivot = new Vector2(.5f, 1);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32;
            var track = UiSkin.Rect(gridRoot, "ScrollTrack", new Vector2(426, -46), new Vector2(8, viewH));
            UiSkin.Img(track, "Track", Vector2.zero, track.sizeDelta, UiSkin.Rounded(4), Surface, true);
            var handle = UiSkin.Img(track, "Handle", Vector2.zero, new Vector2(8, viewH), UiSkin.Rounded(4), Muted, true);
            handle.rectTransform.sizeDelta = Vector2.zero;
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle.rectTransform; bar.targetGraphic = handle;
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.value = 1;
            scroll.verticalScrollbar = bar;
            scroll.verticalNormalizedPosition = 1;
            track.gameObject.SetActive(contentH > viewH);
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int col = i % cols, row = i / cols;
                var pos = new Vector2((col - 1) * (rowW + gap), -rowH * .5f - row * (rowH + gap));
                var body = Card(content, "Item_" + item.id, pos, new Vector2(rowW, rowH), 10, Surface);
                body.anchorMin = body.anchorMax = new Vector2(.5f, 1);
                bool isGear = item.kind == "gear";
                var accent = isGear ? Gold : Blue;
                UiSkin.Img(body, "Accent", new Vector2(-rowW * .5f + 4, 0), new Vector2(2, rowH - 28), null, accent);
                string iconKind = !string.IsNullOrEmpty(item.icon) ? item.icon : (isGear ? "amulet" : "sword");
                UiSkin.Img(body, "IconBg", new Vector2(-108, 20), new Vector2(36, 36), UiSkin.Rounded(8), Color.Lerp(Surface, accent, .13f));
                UiSkin.Img(body, "Icon", new Vector2(-108, 20), new Vector2(28, 28), UiSkin.Icon(iconKind), Color.white);
                // 1 行目: 名前（左）と 種類（右の小さな札）
                Label(body, "Kind", new Vector2(rowW * .5f - 12 - 24, 32), new Vector2(48, 12), isGear ? "装備" : "スキル", 9, TextAnchor.MiddleRight, accent);
                var nameT = Label(body, "Name", new Vector2(-84 + 150 * .5f, 30), new Vector2(150, 20), item.name, 14, TextAnchor.MiddleLeft, UiSkin.Text);
                nameT.fontStyle = FontStyle.Bold;
                nameT.resizeTextForBestFit = true; nameT.resizeTextMinSize = 11; nameT.resizeTextMaxSize = 14;
                // 2 行目: 効果（2 行まで。長ければ切る）
                var descT = Label(body, "Desc", new Vector2(-84 + 214 * .5f, 5), new Vector2(214, 30), "", 10, TextAnchor.UpperLeft, Muted);
                descT.lineSpacing = 1f;
                // 3 行目: Lv（左）と 強化ボタン（右）
                var lvT = Label(body, "Lv", new Vector2(-rowW * .5f + 12 + 44, -30), new Vector2(88, 18), "", 11, TextAnchor.MiddleLeft, accent);
                Button buy = null;
                var it = item;   // クロージャ用
                buy = Button(body, "Buy", new Vector2(rowW * .5f - 12 - 78, -30), new Vector2(156, 30), "", () =>
                {
                    if (ShopDirector.Buy(m.Wallet, it))
                    {
                        audio?.UiPop();
                        SaveData.Save(m, audio);
                        note.text = $"{it.name} を購入しました";
                        note.color = UiSkin.Green;
                    }
                    else
                    {
                        note.text = ShopDirector.NextCost(it, m.Wallet.LevelOf(it.id)) < 0 ? $"{it.name} はもう最大です" : "ソウルが足りません";
                        note.color = UiSkin.Accent;
                    }
                    RefreshAll();
                }, accent, 12, false, 7);
                rows.Add(() =>
                {
                    int lv = m.Wallet.LevelOf(it.id);
                    int cost = ShopDirector.NextCost(it, lv);
                    lvT.text = $"Lv {lv} / {it.maxLevel}";
                    descT.text = ShopDirector.Describe(it, lv);
                    var label = buy.GetComponentInChildren<Text>();
                    if (cost < 0) { label.text = "強化完了"; buy.interactable = false; }
                    else { label.text = m.Wallet.Souls >= cost ? $"強化  {cost:N0}" : $"不足  {cost:N0}"; buy.interactable = m.Wallet.Souls >= cost; }
                });
            }

            SetTab(0);
            RefreshAll();
            return overlay.gameObject;
        }

        private static Text Label(Transform parent, string name, Vector2 pos, Vector2 size, string text,
            int fontSize, TextAnchor align, Color color)
        {
            var label = UiFactory.Label(parent, name, pos, size, text, fontSize, align, color);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private static readonly Color Surface = UiSkin.Hex("#1b293b");
        private static readonly Color Edge = UiSkin.Hex("#34465a");
        private static readonly Color Muted = UiSkin.Hex("#b5c3d3");
        private static readonly Color Gold = UiSkin.Hex("#e6c187");
        private static readonly Color Blue = UiSkin.Hex("#8cc9ee");

        private static RectTransform Card(Transform parent, string name, Vector2 pos, Vector2 size,
            int radius = 12, Color? color = null)
        {
            var root = UiSkin.Rect(parent, name, pos, size);
            UiSkin.Img(root, "Edge", Vector2.zero, size, UiSkin.Rounded(radius), Edge);
            UiSkin.Img(root, "Body", Vector2.zero, size - Vector2.one * 2, UiSkin.Rounded(radius - 1), color ?? UiSkin.Hex("#101b2a"), true);
            return root;
        }

        private static Button Button(Transform parent, string name, Vector2 pos, Vector2 size, string text,
            System.Action onClick, Color color, int fontSize = 15, bool lamp = false, int radius = 8)
        {
            var root = UiSkin.Rect(parent, name, pos, size);
            var body = UiSkin.Img(root, "Body", Vector2.zero, size, UiSkin.Rounded(radius), Color.white, true);
            var label = Label(root, "Label", Vector2.zero, size - new Vector2(12, 4), text, fontSize, TextAnchor.MiddleCenter,
                color.grayscale > .5f ? UiSkin.Bg : UiSkin.Text);
            label.fontStyle = FontStyle.Bold;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = body;
            var colors = button.colors;
            colors.normalColor = colors.selectedColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, .15f);
            colors.pressedColor = Color.Lerp(color, Color.black, .18f);
            colors.disabledColor = UiSkin.Hex(color.grayscale > .5f ? "#748395" : "#344358");
            button.colors = colors;
            button.onClick.AddListener(() => onClick?.Invoke());
            return button;
        }

        private static Text Wallet(Transform parent, string name, Vector2 pos, string caption, Color accent, string icon)
        {
            var root = Card(parent, name + "Wallet", pos, new Vector2(164, 58), 9, Surface);
            UiSkin.Img(root, "Icon", new Vector2(-60, 0), new Vector2(26, 26), UiSkin.Icon(icon), Color.white);
            Label(root, "Caption", new Vector2(19, 14), new Vector2(116, 16), caption, 10, TextAnchor.MiddleLeft, Muted);
            var value = Label(root, "Value", new Vector2(19, -9), new Vector2(116, 26), "", 20, TextAnchor.MiddleLeft, accent);
            value.resizeTextForBestFit = true; value.resizeTextMinSize = 12; value.resizeTextMaxSize = 20;
            return value;
        }
    }
}
