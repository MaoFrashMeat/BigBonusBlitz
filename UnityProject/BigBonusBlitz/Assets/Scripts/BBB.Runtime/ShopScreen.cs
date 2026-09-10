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
            const float W = 780f, H = 470f;
            var overlay = UiFactory.Panel(stage, "ShopOverlay", Vector2.zero, new Vector2(4000, 4000), new Color(0, 0, 0, 0.86f));
            var eatBg = overlay.gameObject.AddComponent<Button>();
            eatBg.transition = Selectable.Transition.None;
            eatBg.onClick.AddListener(() => onClose?.Invoke());

            var card = UiSkin.Card(overlay, "ShopCard", Vector2.zero, new Vector2(W, H), 16);
            var eat = card.gameObject.AddComponent<Button>();
            eat.transition = Selectable.Transition.None;

            // 見出し行は右から領域を取る（閉じるボタン → ソウル → タブ → 題名）。
            // 幅を足し合わせてから配置し、隣と重ならないことを式の上で保証する。
            const float headY = H * 0.5f - 28f;
            const float pad = 12f;                       // 要素どうしの最小の間
            const float closeD = 30f;                    // 閉じるボタンの直径
            const float soulW = 170f, tabW = 104f, titleW = 190f;
            float closeCx = W * 0.5f - 14f - closeD * 0.5f;               // 右端から 14
            float soulRight = closeCx - closeD * 0.5f - pad;              // ソウルの右端
            float soulCx = soulRight - soulW * 0.5f;
            float tab3Right = soulCx - soulW * 0.5f - pad;                // 一番右のタブの右端
            float tab3Cx = tab3Right - tabW * 0.5f;
            float tab2Cx = tab3Cx - tabW - 8f;
            float tab1Cx = tab2Cx - tabW - 8f;
            float titleLeft = -W * 0.5f + 20f;
            float titleCx = titleLeft + titleW * 0.5f;

            var title = UiFactory.Label(card, "Title", new Vector2(titleCx, headY), new Vector2(titleW, 28), "街のショップ", 20, TextAnchor.MiddleLeft, UiSkin.Text);
            title.fontStyle = FontStyle.Bold;
            // 所持は 1 枠に 2 種。ソウル=スキル・装備 / エンバー=補給（松明・路銀）
            var soul = UiSkin.Number(card, "Soul", new Vector2(soulCx, headY + 7), new Vector2(soulW, 16), "", 14, UiSkin.Hex("#8f6bff"));
            var ember = UiSkin.Number(card, "Ember", new Vector2(soulCx, headY - 8), new Vector2(soulW, 16), "", 14, UiSkin.Hex("#ffb45c"));
            UiSkin.Img(card, "Line", new Vector2(0, H * 0.5f - 48), new Vector2(W - 36, 1), null, new Color(1, 1, 1, 0.09f));
            UiSkin.IconButton(card, "Close", new Vector2(closeCx, H * 0.5f - 26), closeD, "×", () => onClose?.Invoke(), UiSkin.Btn, 17);

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
            Button tabGear = null, tabSupply = null, tabStats = null;
            void SetTab(int tab)   // 0=装備 1=補給 2=ステータス
            {
                gridRoot.gameObject.SetActive(tab == 0);
                supplyRoot.gameObject.SetActive(tab == 1);
                statsRoot.gameObject.SetActive(tab == 2);
                if (tabGear != null) UiSkin.SetButtonColor(tabGear, tab == 0 ? UiSkin.Blue : UiSkin.Btn, tab == 0 ? UiSkin.Bg : UiSkin.Text);
                if (tabSupply != null) UiSkin.SetButtonColor(tabSupply, tab == 1 ? UiSkin.Green : UiSkin.Btn, tab == 1 ? UiSkin.Bg : UiSkin.Text);
                if (tabStats != null) UiSkin.SetButtonColor(tabStats, tab == 2 ? UiSkin.Gold : UiSkin.Btn, tab == 2 ? UiSkin.Bg : UiSkin.Text);
            }
            if (hasSupply || hasStats)
            {
                tabGear = UiSkin.Button(card, "TabGear", new Vector2(tab1Cx, headY), new Vector2(tabW, 26), "装備・スキル", () => { audio?.UiPop(); SetTab(0); }, UiSkin.Blue, 12, false, 8);
                if (hasSupply) tabSupply = UiSkin.Button(card, "TabSupply", new Vector2(tab2Cx, headY), new Vector2(tabW, 26), "補給", () => { audio?.UiPop(); SetTab(1); }, UiSkin.Btn, 12, false, 8);
                if (hasStats) tabStats = UiSkin.Button(card, "TabStats", new Vector2(tab3Cx, headY), new Vector2(tabW, 26), "ステータス", () => { audio?.UiPop(); SetTab(2); }, UiSkin.Btn, 12, false, 8);
            }

            var note = UiFactory.Label(card, "Note", new Vector2(0, -H * 0.5f + 22), new Vector2(W - 40, 20), "", 12, TextAnchor.MiddleCenter, UiSkin.TextSub);

            var cfg = m.Config.shop;
            var items = cfg?.items ?? new List<ShopItem>();
            var rows = new List<System.Action>();

            // ===== ステータスタブ: ライフ / テクニック / ラック =====
            if (hasStats)
            {
                var head = UiFactory.Label(statsRoot, "StatHead", new Vector2(0, H * 0.5f - 74), new Vector2(W - 60, 20), "", 14, TextAnchor.MiddleCenter, UiSkin.Text);
                head.fontStyle = FontStyle.Bold;

                const float colW = 232f;
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
                    var box = UiSkin.Card(statsRoot, "Stat_" + key, new Vector2(cx, 6), new Vector2(colW, 250), 12, UiSkin.PanelHi, true, false, false);
                    UiSkin.Img(box, "Bar", new Vector2(0, 122), new Vector2(colW - 2, 5), UiSkin.Rounded(2), col);
                    var nm = UiFactory.Label(box, "Name", new Vector2(0, 96), new Vector2(colW - 20, 24), StatsDirector.DisplayName(key), 19, TextAnchor.MiddleCenter, UiSkin.Text);
                    nm.fontStyle = FontStyle.Bold;
                    UiFactory.Label(box, "Sum", new Vector2(0, 74), new Vector2(colW - 16, 30), StatsDirector.Summary(key), 11, TextAnchor.UpperCenter, UiSkin.TextSub);
                    var val = UiSkin.Number(box, "Val", new Vector2(0, 38), new Vector2(colW - 20, 34), "0", 30, col);
                    valueTexts.Add(val);
                    var eff = UiFactory.Label(box, "Eff", new Vector2(0, -22), new Vector2(colW - 20, 60), "", 11, TextAnchor.UpperLeft, UiSkin.Text);
                    effectTexts.Add(eff);
                    var nxt = UiFactory.Label(box, "Next", new Vector2(0, -74), new Vector2(colW - 20, 30), "", 10, TextAnchor.UpperCenter, UiSkin.TextDim);
                    nextTexts.Add(nxt);
                    string k = key;
                    var add = UiSkin.Button(box, "Add", new Vector2(0, -108), new Vector2(colW - 40, 32), "＋ 1 振る", () =>
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

                var respec = UiSkin.Button(statsRoot, "Respec", new Vector2(0, -H * 0.5f + 66), new Vector2(240, 30), "", () =>
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
                }, UiSkin.Btn, 12, false, 8);

                rows.Add(() =>
                {
                    head.text = $"Lv {m.PlayerLevel}   振れるポイント {m.Stats.Unspent}";
                    head.color = m.Stats.Unspent > 0 ? UiSkin.Gold : UiSkin.TextSub;
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
                    UiSkin.SetButtonText(respec, cost == 0 ? "振り直す（無料）" : $"振り直す   {cost:N0}");
                    respec.interactable = m.Stats.Total > 0 && m.Wallet.Souls >= cost;
                });
            }

            // ===== 補給タブ: 松明とエンバー =====
            if (hasSupply)
            {
                var head = UiFactory.Label(supplyRoot, "SupHead", new Vector2(0, H * 0.5f - 76), new Vector2(W - 60, 20), "冒険に持っていくもの（エンバーで買う）", 14, TextAnchor.MiddleCenter, UiSkin.TextSub);

                var torchCard = UiSkin.Card(supplyRoot, "TorchCard", new Vector2(-186, 40), new Vector2(340, 170), 12, UiSkin.PanelHi, true, false, false);
                UiSkin.Img(torchCard, "Bar", new Vector2(0, 82), new Vector2(338, 5), UiSkin.Rounded(2), UiSkin.Hex("#e08a2a"));
                var tName = UiFactory.Label(torchCard, "Name", new Vector2(0, 52), new Vector2(300, 26), res.name, 20, TextAnchor.MiddleCenter, UiSkin.Text);
                tName.fontStyle = FontStyle.Bold;
                var tHave = UiSkin.Number(torchCard, "Have", new Vector2(0, 18), new Vector2(300, 30), "", 24, UiSkin.Hex("#ffb45c"));
                var tDesc = UiFactory.Label(torchCard, "Desc", new Vector2(0, -12), new Vector2(300, 18), "", 12, TextAnchor.MiddleCenter, UiSkin.TextSub);
                Button buyTorch = null;
                buyTorch = UiSkin.Button(torchCard, "Buy", new Vector2(0, -52), new Vector2(260, 38), "", () =>
                {
                    int cost = Mathf.Max(0, res.torchCost);
                    if (m.Wallet.Embers < cost) { note.text = "エンバーが足りません"; note.color = UiSkin.Accent; RefreshAll(); return; }
                    if (AdventureDirector.AddTorch(m.Config.adventure, m.Adv, 1, m.TorchSpinsPerUnit) <= 0)
                    { note.text = $"{res.name} はもう持てません"; note.color = UiSkin.Accent; RefreshAll(); return; }
                    m.Wallet.Embers -= cost;
                    audio?.UiPop();
                    SaveData.Save(m, audio);
                    note.text = $"{res.name} を 1 本 買いました";
                    note.color = UiSkin.Green;
                    RefreshAll();
                }, UiSkin.Hex("#e08a2a"), 15, false, 10);

                var coinCard = UiSkin.Card(supplyRoot, "CoinCard", new Vector2(186, 40), new Vector2(340, 170), 12, UiSkin.PanelHi, true, false, false);
                UiSkin.Img(coinCard, "Bar", new Vector2(0, 82), new Vector2(338, 5), UiSkin.Rounded(2), UiSkin.Gold);
                var cName = UiFactory.Label(coinCard, "Name", new Vector2(0, 52), new Vector2(300, 26), "灯火（エンバー）", 18, TextAnchor.MiddleCenter, UiSkin.Text);
                cName.fontStyle = FontStyle.Bold;
                var cHave = UiSkin.Number(coinCard, "Have", new Vector2(0, 18), new Vector2(300, 30), "", 24, UiSkin.Gold);
                var cDesc = UiFactory.Label(coinCard, "Desc", new Vector2(0, -12), new Vector2(300, 18), "", 12, TextAnchor.MiddleCenter, UiSkin.TextSub);
                Button buyCredit = null;
                buyCredit = UiSkin.Button(coinCard, "Buy", new Vector2(0, -52), new Vector2(260, 38), "", () =>
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
                }, UiSkin.Gold, 15, false, 10);

                UiFactory.Label(supplyRoot, "SupNote", new Vector2(0, -H * 0.5f + 62), new Vector2(W - 60, 34),
                    $"{res.name} が尽きると街へ引き返す（進行はそのまま）。エンバーが尽きると力尽きて、章の最初からやり直しになる。", 12, TextAnchor.MiddleCenter, UiSkin.TextDim);

                rows.Add(() =>
                {
                    int per = m.TorchSpinsPerUnit;
                    int have = Mathf.Max(0, m.Adv.torches);
                    tHave.text = $"{have} / {res.maxTorches} 本";
                    tDesc.text = $"1 本で {per} G 進める" + (have > 0 ? $"（今の 1 本は残り {m.Adv.torchSpins} G）" : "");
                    UiSkin.SetButtonText(buyTorch, $"1 本 買う   {res.torchCost:N0}");
                    buyTorch.interactable = m.Wallet.Embers >= res.torchCost && have < res.maxTorches;
                    cHave.text = $"{m.Credit:N0}";
                    cDesc.text = $"1 口で {res.creditAmount} 分けてもらう";
                    UiSkin.SetButtonText(buyCredit, $"1 口 もらう   {res.creditCost:N0}");
                    buyCredit.interactable = m.Wallet.Embers >= res.creditCost;
                });
            }

            void RefreshAll()
            {
                soul.text = $"SOUL {m.Wallet.Souls:N0}";
                ember.text = $"EMBER {m.Wallet.Embers:N0}";
                foreach (var r in rows) r();
                onSoulsChanged?.Invoke();
            }

            if (items.Count == 0)
            {
                UiFactory.Label(gridRoot, "Empty", Vector2.zero, new Vector2(W - 60, 40), "品揃えがありません（game_config.json の shop を確認してください）", 14, TextAnchor.MiddleCenter, UiSkin.TextSub);
                RefreshAll();
                return overlay.gameObject;
            }

            // 2 列 × 行。1 品ごとに 1 行のカード
            const float rowW = 360f, rowH = 84f;
            float startY = H * 0.5f - 48 - 12 - rowH * 0.5f;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int col = i % 2, row = i / 2;
                var pos = new Vector2(col == 0 ? -rowW * 0.5f - 8 : rowW * 0.5f + 8, startY - row * (rowH + 8));
                var body = UiSkin.Card(gridRoot, "Item_" + item.id, pos, new Vector2(rowW, rowH), 12, UiSkin.PanelHi, true, false, false);

                bool isGear = item.kind == "gear";
                var accent = isGear ? UiSkin.Gold : UiSkin.Blue;
                UiSkin.Img(body, "Bar", new Vector2(-rowW * 0.5f + 3, 0), new Vector2(5, rowH - 8), UiSkin.Rounded(2), accent);

                // アイコン（丸い台座＋図形）
                string iconKind = !string.IsNullOrEmpty(item.icon) ? item.icon : (isGear ? "amulet" : "sword");
                UiSkin.Img(body, "IconBg", new Vector2(-rowW * 0.5f + 36, 0), new Vector2(46, 46), UiSkin.Circle(48), new Color(accent.r * 0.22f, accent.g * 0.22f, accent.b * 0.22f, 1f));
                UiSkin.Img(body, "IconRing", new Vector2(-rowW * 0.5f + 36, 0), new Vector2(48, 48), UiSkin.Circle(48), new Color(accent.r, accent.g, accent.b, 0.28f));
                UiSkin.Img(body, "Icon", new Vector2(-rowW * 0.5f + 36, 0), new Vector2(38, 38), UiSkin.Icon(iconKind), Color.white);
                UiSkin.Chip(body, "Kind", new Vector2(-rowW * 0.5f + 92, rowH * 0.5f - 16), new Vector2(52, 18), isGear ? "装備" : "スキル", accent, UiSkin.Bg, 10);

                var nameT = UiFactory.Label(body, "Name", new Vector2(64, rowH * 0.5f - 16), new Vector2(180, 20), item.name, 15, TextAnchor.MiddleLeft, UiSkin.Text);
                nameT.fontStyle = FontStyle.Bold;
                var lvT = UiFactory.Label(body, "Lv", new Vector2(rowW * 0.5f - 60, rowH * 0.5f - 16), new Vector2(100, 20), "", 12, TextAnchor.MiddleRight, UiSkin.TextSub);
                var descT = UiFactory.Label(body, "Desc", new Vector2(46, -2), new Vector2(rowW - 100, 18), "", 12, TextAnchor.MiddleLeft, UiSkin.TextSub);

                Button buy = null;
                var it = item;   // クロージャ用
                buy = UiSkin.Button(body, "Buy", new Vector2(rowW * 0.5f - 76, -rowH * 0.5f + 20), new Vector2(128, 30), "", () =>
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
                }, accent, 14, false, 8);

                rows.Add(() =>
                {
                    int lv = m.Wallet.LevelOf(it.id);
                    int cost = ShopDirector.NextCost(it, lv);
                    lvT.text = $"Lv {lv} / {it.maxLevel}";
                    descT.text = ShopDirector.Describe(it, lv);
                    var label = buy.GetComponentInChildren<Text>();
                    if (cost < 0) { label.text = "最大"; buy.interactable = false; }
                    else { label.text = $"{cost:N0} で購入"; buy.interactable = m.Wallet.Souls >= cost; }
                });
            }

            RefreshAll();
            return overlay.gameObject;
        }
    }
}
