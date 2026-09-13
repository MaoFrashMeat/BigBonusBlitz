using System.Collections.Generic;
using BBB.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// ステータス（ライフ / テクニック / ラック）にポイントを振る画面。
    /// 街のショップの「ステータス」タブと、冒険中の装備画面の「ステータス」ボタンから同じ板を出す。
    /// 板は細い縁の紺（panel_navy_sm）。題は左上の紺のタブ。
    /// </summary>
    public static class StatsScreen
    {
        private static readonly Color Surface = UiSkin.Hex("#1b293b");
        private static readonly Color Edge = UiSkin.Hex("#34465a");
        private static readonly Color Muted = UiSkin.Hex("#b5c3d3");
        private static readonly Color Gold = UiSkin.Hex("#e6c187");

        /// <summary>冒険中に開く単独の窓。閉じるのは呼び手（onClose の中で Destroy する）。</summary>
        public static GameObject Build(Transform stage, SlotMachine m, AudioManager audio, System.Action onChanged, System.Action onClose)
        {
            const float W = 900f, H = 470f, edge = 22f;
            var overlay = UiFactory.Panel(stage, "StatsOverlay", Vector2.zero, new Vector2(4000, 4000), new Color(0, 0, 0, 0.62f));
            var eatBg = overlay.gameObject.AddComponent<Button>();
            eatBg.transition = Selectable.Transition.None;
            eatBg.onClick.AddListener(() => onClose?.Invoke());
            var card = UiSkin.Card(overlay, "StatsCard", Vector2.zero, new Vector2(W, H), 16, frameOverride: "panel_navy_sm");
            var eat = card.gameObject.AddComponent<Button>();      // 中を押しても閉じない
            eat.transition = Selectable.Transition.None;
            const float tabW = 150f, tabH = 40f;
            var tab = UiSkin.Img(card, "Tab", new Vector2(-W * 0.5f + edge + tabW * 0.5f, H * 0.5f - 4), new Vector2(tabW, tabH), UiSkin.Frame("pill_navy_sm"), Color.white);
            var title = UiFactory.Label(tab.transform, "Title", new Vector2(0, 1), new Vector2(tabW, tabH), "ステータス", 15, TextAnchor.MiddleCenter, UiSkin.Text);
            title.fontStyle = FontStyle.Bold;
            UiSkin.IconButton(card, "Close", new Vector2(W * 0.5f - edge, H * 0.5f - 4), 30, "×", () => onClose?.Invoke(), UiSkin.Btn, 16);

            var note = Label(card, "Note", new Vector2(0, -H * 0.5f + 30), new Vector2(W - 80, 22), "ポイントを割り振って、自分だけの戦い方に", 12, TextAnchor.MiddleCenter, Muted);
            // 板の中身は街と同じ。街の板より低いぶん、上に寄せる（見出し 152 / 札 136〜-122 / 振り直す -140 / 注 -205）
            var root = UiSkin.Rect(card, "StatsRoot", new Vector2(0, 50), new Vector2(W, H));
            System.Action refresh = null;
            refresh = BuildPanel(root, m, audio, note, () => { onChanged?.Invoke(); refresh?.Invoke(); });
            refresh();
            return overlay.gameObject;
        }

        /// <summary>
        /// 3 列の板を root に作る。返す Action で表示を更新する。
        /// note は結果を書く欄（無ければ null）。onChanged は振ったあと・戻したあとに呼ぶ（セーブは中で済ませる）。
        /// </summary>
        public static System.Action BuildPanel(Transform root, SlotMachine m, AudioManager audio, Text note, System.Action onChanged)
        {
            var statsCfg = m.Config.stats;
            var head = Label(root, "StatHead", new Vector2(0, 102), new Vector2(840, 20), "", 14, TextAnchor.MiddleCenter, UiSkin.Text);
            head.fontStyle = FontStyle.Bold;

            const float colW = 272f;
            var addButtons = new List<Button>();
            var valueTexts = new List<Text>();
            var effectTexts = new List<Text>();
            var nextTexts = new List<Text>();
            var colors = new[] { UiSkin.Hex("#3ddc84"), UiSkin.Hex("#ff7a45"), UiSkin.Hex("#ffcf3f") };
            System.Action refresh = null;
            void Say(string text, Color color) { if (note == null) return; note.text = text; note.color = color; }

            for (int i = 0; i < StatsDirector.Keys.Length; i++)
            {
                string key = StatsDirector.Keys[i];
                var col = colors[i];
                float cx = (i - 1) * (colW + 12f);
                var box = Card(root, "Stat_" + key, new Vector2(cx, -43), new Vector2(colW, 258));
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
                var add = UiSkin.Button(box, "Add", new Vector2(0, -102), new Vector2(colW - 32, 44), "＋ 1 振る", () =>
                {
                    if (!StatsDirector.Spend(statsCfg, m.Stats, k)) { Say("振れるポイントがありません", UiSkin.Accent); refresh?.Invoke(); return; }
                    audio?.UiPop();
                    SaveData.Save(m, audio);
                    Say($"{StatsDirector.DisplayName(k)} を上げました", UiSkin.Green);
                    refresh?.Invoke();
                    onChanged?.Invoke();
                }, col, 14, false, 8, "btn_blue");
                addButtons.Add(add);
            }

            var respec = UiSkin.Button(root, "Respec", new Vector2(0, -190), new Vector2(280, 36), "", () =>
            {
                int cost = m.Adv.chapter > 1 && statsCfg.freeRespecOnChapterClear ? 0 : Mathf.Max(0, statsCfg.respecCost);
                if (m.Wallet.Souls < cost) { Say("ソウルが足りません", UiSkin.Accent); refresh?.Invoke(); return; }
                m.Wallet.Souls -= cost;
                int back = StatsDirector.Respec(m.Stats);
                audio?.UiPop();
                SaveData.Save(m, audio);
                Say($"{back} ポイントを戻しました", UiSkin.Green);
                refresh?.Invoke();
                onChanged?.Invoke();
            }, Surface, 15, false, 8, "pill_navy_sm");

            refresh = () =>
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
            };
            return refresh;
        }

        private static Text Label(Transform parent, string name, Vector2 pos, Vector2 size, string text,
            int fontSize, TextAnchor align, Color color)
        {
            var label = UiFactory.Label(parent, name, pos, size, text, fontSize, align, color);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        /// <summary>札。紺のピル（pill_navy_sm）の絵。無ければ角丸の板。</summary>
        private static RectTransform Card(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var root = UiSkin.Rect(parent, name, pos, size);
            var frame = UiSkin.Frame("pill_navy_sm");
            if (frame != null) UiSkin.Img(root, "Body", Vector2.zero, size, frame, Color.white, true);
            else
            {
                UiSkin.Img(root, "Edge", Vector2.zero, size, UiSkin.Rounded(12), Edge);
                UiSkin.Img(root, "Body", Vector2.zero, size - Vector2.one * 2, UiSkin.Rounded(11), UiSkin.Hex("#101b2a"), true);
            }
            return root;
        }
    }
}
