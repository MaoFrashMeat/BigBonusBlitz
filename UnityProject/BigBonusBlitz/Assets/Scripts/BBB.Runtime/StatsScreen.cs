using System;
using System.Collections.Generic;
using BBB.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>Celestial status sanctuary shared by town and adventure. Effects come from StatsDirector.</summary>
    public static class StatsScreen
    {
        static readonly Color Muted = UiSkin.Hex("#c4cede"), Gold = UiSkin.Hex("#e9ce86");
        public static GameObject Build(Transform stage, SlotMachine m, AudioManager audio, Action onChanged, Action onClose)
        {
            var body = AtelierUi.Screen(stage, "StatsCard", Color.clear, null, out var overlay);
            body.sizeDelta = new Vector2(1170, 540);
            var bg = UiSkin.Img(body, "Cosmos", Vector2.zero, body.sizeDelta, CelestialStatsSkin.Sprite("cosmos"), Color.white);
            bg.raycastTarget = false;
            bg.gameObject.AddComponent<AzureMapBleed>().Apply();
            AzureMapSkin.Button(body, "Back", -475, 236, 172, "←  戻る", onClose);
            AzureShopSkin.Label(body, "Sanctuary", -238, 236, 276, 42, "星詠みの聖域", 23, TextAnchor.MiddleLeft, Gold);
            var souls = AzureShopSkin.Wallet(body, "SoulWallet", 241, 180, AtelierUi.Icon("soul"), "所持ソウル");
            var embers = AzureShopSkin.Wallet(body, "EmberWallet", 437, 180, AtelierUi.Icon("ember"), "所持エンバー");
            AzureMapSkin.Button(body, "Close", 550, 236, 44, "×", onClose);
            AzureShopSkin.Surface(body, "IvorySanctuary", 0, -20, 1000, 468, true);
            AzureShopSkin.Label(body, "Title", 0, 190, 550, 44, "ステータス強化", 28);
            AtelierUi.Panel(body, "TitleRuleL", -338, 185, 118, 1, Gold);
            AtelierUi.Panel(body, "TitleRuleR", 338, 185, 118, 1, Gold);
            var noteBand = AtelierUi.Panel(body, "NoteBand", 0, -260, 1000, 20, UiSkin.Hex("#0b142b"));
            var note = AtelierUi.Text(body, "Note", 0, -260, 980, 18,
                "ポイントを割り振って、自分だけの戦い方に", 12, AzureMapSkin.Paper, false, TextAnchor.MiddleCenter);
            void Wallets() { souls.text = m.Wallet.Souls.ToString("N0"); embers.text = m.Wallet.Embers.ToString("N0"); }
            var refresh = BuildPanel(body, m, audio, note, onChanged, Wallets);
            Wallets(); refresh();
            return overlay;
        }

        /// <summary>Build once; update cached controls without rebuilding the hierarchy.</summary>
        public static Action BuildPanel(Transform root, SlotMachine m, AudioManager audio, Text note, Action onChanged, Action onRefreshed = null)
        {
            var cfg = m.Config.stats;
            var head = AtelierUi.Text(root, "StatHead", 0, 153, 680, 24, "", 17, AzureMapSkin.Ink, true, TextAnchor.MiddleCenter);
            var addButtons = new List<Button>(); var addLabels = new List<Text>();
            var values = new List<Text>(); var effectNames = new List<Text[]>(); var effectValues = new List<Text[]>(); var previews = new List<Text>();
            var colors = new[] { UiSkin.Hex("#66edbb"), UiSkin.Hex("#ffad8a"), UiSkin.Hex("#ffe38b") };
            var fills = new[] { UiSkin.Hex("#142b32"), UiSkin.Hex("#302030"), UiSkin.Hex("#2b2831") };
            var summaries = new[] { "ライフを守り、\nエンバーの消費を抑える", "エンゲージと討伐率を強化\n戦いを有利に", "レア役とリプレイを引き寄せる\n宝との出会いを増やす" };
            Action refresh = null;
            void Say(string message, bool success) { if (note != null) { note.text = message; note.color = success ? UiSkin.Hex("#9cf2ce") : Gold; } }
            for (int i = 0; i < StatsDirector.Keys.Length; i++)
            {
                string key = StatsDirector.Keys[i];
                var box = UiSkin.Rect(root, "Stat_" + key, new Vector2((i - 1) * 300, -28), new Vector2(290, 332));
                CelestialStatsSkin.Card(box, fills[i]);
                AtelierUi.Art(box, "Medallion", 0, 139, 58, 58, CelestialStatsSkin.Emblem(i));
                AzureShopSkin.Label(box, "Name", 0, 90, 246, 34, StatsDirector.DisplayName(key), 21, TextAnchor.MiddleCenter, Color.white);
                AtelierUi.Text(box, "Summary", 0, 51, 206, 36, summaries[i], 14, Muted, false, TextAnchor.MiddleCenter);
                values.Add(AtelierUi.Text(box, "Val", 0, 7, 244, 42, "", 32, colors[i], true, TextAnchor.MiddleCenter));
                AtelierUi.Panel(box, "Rule", 0, -17, 206, 1, Gold);
                var names = new Text[3]; var amounts = new Text[3];
                for (int row = 0; row < 3; row++)
                {
                    float y = -31 - row * 21;
                    names[row] = AtelierUi.Text(box, "EffectName" + row, -29, y, 142, 20, "", 14, AzureMapSkin.Paper);
                    amounts[row] = AtelierUi.Text(box, "EffectValue" + row, 74, y, 52, 20, "", 15, colors[i], true, TextAnchor.MiddleRight);
                }
                effectNames.Add(names); effectValues.Add(amounts);
                previews.Add(AtelierUi.Text(box, "Next", 0, -103, 206, 34, "", 12, Muted, false, TextAnchor.MiddleCenter));
                var add = AzureMapSkin.Button(box, "Add_" + key, 0, -143, 250, "＋ 1 振る", () =>
                {
                    if (!StatsDirector.Spend(cfg, m.Stats, key))
                    { Say(m.Stats.Get(key) >= cfg.maxPerStat ? "この能力は最大まで強化されています" : "振れるポイントがありません", false); refresh?.Invoke(); return; }
                    audio?.UiPop(); SaveData.Save(m, audio);
                    Say(StatsDirector.DisplayName(key) + " を上げました", true);
                    refresh?.Invoke(); onChanged?.Invoke();
                }, false, 44, fontSize: 19);
                addButtons.Add(add); addLabels.Add(add.GetComponentInChildren<Text>());
            }
            int Cost() => m.Adv.chapter > 1 && cfg.freeRespecOnChapterClear ? 0 : Mathf.Max(0, cfg.respecCost);
            var respec = AzureMapSkin.Button(root, "Respec", 0, -222, 392, "", () =>
            {
                int quoted = Cost();
                if (m.Stats.Total <= 0 || m.Wallet.Souls < quoted) { Say("振り直すポイント、またはソウルがありません", false); refresh?.Invoke(); return; }
                AtelierUi.Confirm(root, $"振り分けた {m.Stats.Total} ポイントをすべて戻します。\n" +
                    (quoted == 0 ? "今回は無料で振り直せます。" : $"{quoted:N0} ソウルを使用します。"), () =>
                {
                    // Recheck state and the quote; cancelling never spends currency.
                    int cost = Cost();
                    if (cost != quoted || m.Stats.Total <= 0 || m.Wallet.Souls < cost)
                    { Say("状態が変わりました。所持数と費用をご確認ください", false); refresh?.Invoke(); return; }
                    m.Wallet.Souls -= cost; int back = StatsDirector.Respec(m.Stats);
                    audio?.UiPop(); SaveData.Save(m, audio);
                    Say($"{back} ポイントを戻しました", true);
                    refresh?.Invoke(); onChanged?.Invoke();
                });
            }, false, 44, "soul", 17);
            var respecLabel = respec.GetComponentInChildren<Text>();
            refresh = () =>
            {
                head.text = $"Lv {m.PlayerLevel}    振れるポイント {m.Stats.Unspent}";
                for (int i = 0; i < StatsDirector.Keys.Length; i++)
                {
                    string key = StatsDirector.Keys[i]; int cur = m.Stats.Get(key); bool maxed = cur >= cfg.maxPerStat;
                    values[i].text = $"{cur} / {cfg.maxPerStat}";
                    var lines = StatsDirector.Effects(cfg, m.Stats, key).Split('\n');
                    for (int row = 0; row < 3; row++)
                    {
                        string line = row < lines.Length ? lines[row] : "";
                        int split = line.LastIndexOf(' ');
                        effectNames[i][row].text = split < 0 ? line : line.Substring(0, split).TrimEnd();
                        effectValues[i][row].text = split < 0 ? "" : line.Substring(split + 1);
                    }
                    previews[i].text = maxed ? "最大まで強化済み" : StatsDirector.Describe(cfg, key, cur).Replace("   ", "\n");
                    addButtons[i].interactable = !maxed && m.Stats.Unspent > 0;
                    addLabels[i].text = maxed ? "強化完了" : m.Stats.Unspent > 0 ? "＋ 1 振る" : "ポイントなし";
                }
                int cost = Cost(); respecLabel.text = cost == 0 ? "振り直す   無料" : $"振り直す   {cost:N0} ソウル";
                respec.interactable = m.Stats.Total > 0 && m.Wallet.Souls >= cost;
                onRefreshed?.Invoke();
            };
            return refresh;
        }
    }
}
