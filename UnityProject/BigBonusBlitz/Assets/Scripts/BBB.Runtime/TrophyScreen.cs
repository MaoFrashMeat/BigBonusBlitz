using BBB.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// 実績と図鑑の窓（実績 / 装備の図鑑 / お宝の図鑑 の 3 タブ）。
    /// 冒険中は右下の ★、街の下の段の「実績」から開く。板は細い縁の紺（panel_navy_sm）、題は左上のタブ。
    /// </summary>
    public static class TrophyScreen
    {
        const float W = 760f, H = 500f, edge = 22f;

        /// <summary>窓を作って返す。閉じるのは呼び手（onClose の中で Destroy する）。</summary>
        public static GameObject Build(Transform stage, SlotMachine m, AudioManager audio, System.Action onClose, int startTab = 0)
        {
            var overlay = UiFactory.Panel(stage, "TrophyOverlay", Vector2.zero, new Vector2(4000, 4000), new Color(0, 0, 0, 0.62f));
            var closeBtn = overlay.gameObject.AddComponent<Button>();
            closeBtn.transition = Selectable.Transition.None;
            closeBtn.onClick.AddListener(() => onClose?.Invoke());
            var card = UiSkin.Card(overlay, "Card", Vector2.zero, new Vector2(W, H), 14, frameOverride: "panel_navy_sm");
            var eat = card.gameObject.AddComponent<Button>();        // カード内のタップは閉じない
            eat.transition = Selectable.Transition.None;
            const float tabW = 150f, tabH = 40f;
            var tab = UiSkin.Img(card, "Tab", new Vector2(-W * 0.5f + edge + tabW * 0.5f, H * 0.5f - 4), new Vector2(tabW, tabH), UiSkin.Frame("pill_navy_sm"), Color.white);
            var title = UiFactory.Label(tab.transform, "Title", new Vector2(0, 1), new Vector2(tabW, tabH), "実績と図鑑", 15, TextAnchor.MiddleCenter, UiSkin.Text);
            title.fontStyle = FontStyle.Bold;
            UiSkin.IconButton(card, "Close", new Vector2(W * 0.5f - edge, H * 0.5f - 4), 30, "×", () => onClose?.Invoke(), UiSkin.Btn, 16);

            var view = new View { m = m, audio = audio, tab = startTab };
            // 上にタブ 3 つ（実績 / 装備図鑑 / お宝図鑑）、右に解除数、残りが一覧
            string[] tabNames = { "実績", "装備の図鑑", "お宝の図鑑" };
            for (int i = 0; i < 3; i++)
            {
                int t = i;
                view.tabs[i] = UiSkin.Button(card, "Tab" + i, new Vector2(-W * 0.5f + 70 + i * 148, H * 0.5f - 54), new Vector2(140, 34), tabNames[i],
                    () => { view.tab = t; audio.UiPop(); view.Refresh(); }, UiSkin.Btn, 13, false, 8, "pill_navy_sm");
            }
            view.head = UiFactory.Label(card, "Head", new Vector2(120, H * 0.5f - 54), new Vector2(440, 18), "", 12, TextAnchor.MiddleRight, UiSkin.TextSub);
            UiSkin.ScrollBox(card, "List", new Vector2(0, -34), new Vector2(704, 360), out view.list);
            view.Refresh();
            return overlay.gameObject;
        }

        /// <summary>窓の中身。タブを押すたびに一覧を作り直す（解除と進み具合が変わるため）。</summary>
        private sealed class View
        {
            public SlotMachine m;
            public AudioManager audio;
            public RectTransform list;
            public Text head;
            public int tab;                                   // 0=実績 1=装備図鑑 2=お宝図鑑
            public readonly Button[] tabs = new Button[3];

            public void Refresh()
            {
                if (list == null) return;
                foreach (Transform c in list) Object.Destroy(c.gameObject);
                for (int i = 0; i < tabs.Length; i++)
                {
                    if (tabs[i] == null) continue;
                    bool on = i == tab;
                    var body = tabs[i].targetGraphic as Image;
                    var tint = on ? new Color(1f, 0.92f, 0.62f, 1f) : Color.white;
                    var cb = tabs[i].colors; cb.normalColor = tint; cb.highlightedColor = tint; cb.selectedColor = tint; tabs[i].colors = cb;
                    if (body != null) body.color = tint;
                    var tl = tabs[i].GetComponentInChildren<Text>(); if (tl != null) tl.color = on ? UiSkin.Gold : UiSkin.Text;
                }
                if (tab == 1) { RefreshEquipCodex(); return; }
                if (tab == 2) { RefreshTreasureCodex(); return; }
                var defs = m.Achievements;
                int unlocked = 0;
                foreach (var d in defs) if (m.Ach.Unlocked.Contains(d.id)) unlocked++;
                head.text = $"解除  {unlocked} / {defs.Count}";
                const float rowH = 44f;
                float w = list.rect.width > 0 ? list.rect.width : 704f;
                foreach (var d in defs)
                {
                    bool done = m.Ach.Unlocked.Contains(d.id);
                    bool secret = d.hidden && !done;
                    var row = UiSkin.Img(list, "Row", Vector2.zero, new Vector2(w, rowH), UiSkin.Rounded(6), done ? new Color(1f, 0.82f, 0.25f, 0.10f) : new Color(1, 1, 1, 0.04f));
                    row.gameObject.AddComponent<LayoutElement>().preferredHeight = rowH;
                    var star = UiSkin.Img(row.transform, "Star", Vector2.zero, new Vector2(26, 26), UiSkin.Star(96), done ? UiSkin.Gold : new Color(1, 1, 1, 0.18f));
                    star.rectTransform.anchorMin = new Vector2(0, 0.5f); star.rectTransform.anchorMax = new Vector2(0, 0.5f);
                    star.rectTransform.anchoredPosition = new Vector2(24, 0);
                    var name = UiFactory.Label(row.transform, "Name", Vector2.zero, Vector2.zero, secret ? "？？？" : d.name, 13, TextAnchor.MiddleLeft, done ? UiSkin.Gold : UiSkin.Text);
                    name.fontStyle = FontStyle.Bold; Side(name.rectTransform, 48, 300); name.rectTransform.anchoredPosition += new Vector2(0, 9); name.rectTransform.sizeDelta = new Vector2(252, 18); name.rectTransform.anchorMin = new Vector2(0, 0.5f); name.rectTransform.anchorMax = new Vector2(0, 0.5f);
                    var desc = UiFactory.Label(row.transform, "Desc", Vector2.zero, Vector2.zero, secret ? "解除すると見える" : d.desc, 10, TextAnchor.MiddleLeft, UiSkin.TextSub);
                    Side(desc.rectTransform, 48, 420); desc.rectTransform.anchoredPosition += new Vector2(0, -9); desc.rectTransform.sizeDelta = new Vector2(372, 16); desc.rectTransform.anchorMin = new Vector2(0, 0.5f); desc.rectTransform.anchorMax = new Vector2(0, 0.5f);
                    // 右: 進み具合とご褒美
                    long cur = System.Math.Min(m.Ach.Get(d.counter), d.target);
                    var prog = UiFactory.Label(row.transform, "Prog", Vector2.zero, Vector2.zero, done ? "達成" : (secret ? "" : $"{cur:N0} / {d.target:N0}"), 11, TextAnchor.MiddleRight, done ? UiSkin.Gold : UiSkin.TextSub);
                    Side(prog.rectTransform, w - 300, w - 120); prog.rectTransform.anchoredPosition += new Vector2(0, 9); prog.rectTransform.sizeDelta = new Vector2(180, 16); prog.rectTransform.anchorMin = new Vector2(0, 0.5f); prog.rectTransform.anchorMax = new Vector2(0, 0.5f);
                    var track = UiSkin.Img(row.transform, "Track", Vector2.zero, Vector2.zero, UiSkin.Rounded(3), new Color(1, 1, 1, 0.10f));
                    track.rectTransform.anchorMin = new Vector2(0, 0.5f); track.rectTransform.anchorMax = new Vector2(0, 0.5f); track.rectTransform.pivot = new Vector2(0, 0.5f);
                    track.rectTransform.anchoredPosition = new Vector2(w - 300, -10); track.rectTransform.sizeDelta = new Vector2(180, 6);
                    float ratio = secret ? 0f : AchievementDirector.Progress(d, m.Ach);
                    var fill = UiSkin.Img(row.transform, "Fill", Vector2.zero, Vector2.zero, UiSkin.Rounded(3), done ? UiSkin.Gold : UiSkin.Green);
                    fill.rectTransform.anchorMin = new Vector2(0, 0.5f); fill.rectTransform.anchorMax = new Vector2(0, 0.5f); fill.rectTransform.pivot = new Vector2(0, 0.5f);
                    fill.rectTransform.anchoredPosition = new Vector2(w - 300, -10); fill.rectTransform.sizeDelta = new Vector2(180 * ratio, 6);
                    var reward = UiFactory.Label(row.transform, "Reward", Vector2.zero, Vector2.zero, d.rewardSouls > 0 ? $"+{d.rewardSouls:N0} ソウル" : "", 11, TextAnchor.MiddleRight, done ? UiSkin.TextSub : UiSkin.Hex("#a98bff"));
                    Side(reward.rectTransform, w - 112, w - 10);
                }
            }

            /// <summary>図鑑の行の下地。</summary>
            private Image CodexRow(float w, float h, bool seen)
            {
                var row = UiSkin.Img(list, "Row", Vector2.zero, new Vector2(w, h), UiSkin.Rounded(6), seen ? new Color(1, 1, 1, 0.06f) : new Color(1, 1, 1, 0.025f));
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = h;
                return row;
            }

            /// <summary>図鑑の見出し行（種類名と 入手 n / N）。</summary>
            private void CodexHeader(float w, string text)
            {
                var h = UiFactory.Label(list, "Head", Vector2.zero, new Vector2(w, 22), text, 11, TextAnchor.MiddleLeft, UiSkin.Gold);
                h.fontStyle = FontStyle.Bold;
                h.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;
            }

            /// <summary>装備の図鑑: 種類ごとに 40 種。拾ったことのある物は名前と効果、まだの物は灰色の「？？？」。</summary>
            private void RefreshEquipCodex()
            {
                var eq = m.Config.equipment;
                if (eq?.bases == null) return;
                float w = list.rect.width > 0 ? list.rect.width : 704f;
                int seenTotal = 0;
                foreach (var b in eq.bases) if (m.Ach.Get(AchievementCounters.SeenPrefix + b.id) > 0) seenTotal++;
                head.text = $"見つけた装備  {seenTotal} / {eq.bases.Count}";
                const float rowH = 40f;
                foreach (var kind in EquipSlot.Kinds)
                {
                    var ofKind = eq.bases.FindAll(b => b != null && EquipSlot.Normalize(b.slot) == kind);
                    if (ofKind.Count == 0) continue;
                    int seenKind = 0;
                    foreach (var b in ofKind) if (m.Ach.Get(AchievementCounters.SeenPrefix + b.id) > 0) seenKind++;
                    CodexHeader(w, $"{EquipSlot.DisplayName(kind)}   {seenKind} / {ofKind.Count}");
                    foreach (var b in ofKind)
                    {
                        long n = m.Ach.Get(AchievementCounters.SeenPrefix + b.id);
                        bool seen = n > 0;
                        var row = CodexRow(w, rowH, seen);
                        var icon = UiSkin.Img(row.transform, "Icon", Vector2.zero, new Vector2(24, 24), UiSkin.Icon(string.IsNullOrEmpty(b.icon) ? "shield" : b.icon, 64), seen ? Color.white : new Color(1, 1, 1, 0.2f));
                        icon.rectTransform.anchorMin = new Vector2(0, 0.5f); icon.rectTransform.anchorMax = new Vector2(0, 0.5f); icon.rectTransform.anchoredPosition = new Vector2(24, 0);
                        var name = UiFactory.Label(row.transform, "Name", Vector2.zero, Vector2.zero, seen ? b.name : "？？？", 13, TextAnchor.MiddleLeft, seen ? UiSkin.Text : UiSkin.TextDim);
                        name.fontStyle = FontStyle.Bold; Side(name.rectTransform, 48, 200);
                        if (seen)
                        {
                            // 上段: 効果と伸び / 下段: その効果の意味
                            string eff = $"{EquipDirector.EffectName(b.effect)}  深さ 1 で +{Mathf.Max(b.min, Mathf.RoundToInt(b.perLevel))}{EquipDirector.EffectUnit(b.effect)}〜";
                            CodexLine(row.transform, "Eff", eff, 10, UiSkin.Text, 200, w - 120, 8);
                            CodexLine(row.transform, "Desc", EquipDirector.EffectDesc(b.effect), 9, UiSkin.TextSub, 200, w - 120, -9);
                        }
                        else CodexLine(row.transform, "Eff", $"深さ {b.minDepth} から出る", 10, UiSkin.TextSub, 200, w - 120, 0);
                        var cnt = UiFactory.Label(row.transform, "Count", Vector2.zero, Vector2.zero, seen ? $"×{n:N0}" : "", 11, TextAnchor.MiddleRight, UiSkin.Gold);
                        Side(cnt.rectTransform, w - 110, w - 12);
                    }
                }
            }

            /// <summary>図鑑の行の中の 1 行テキスト（x0〜x1 の帯、行の中央から dy だけ上）。</summary>
            private static void CodexLine(Transform row, string name, string text, int size, Color color, float x0, float x1, float dy)
            {
                var t = UiFactory.Label(row, name, Vector2.zero, Vector2.zero, text, size, TextAnchor.MiddleLeft, color);
                var rt = t.rectTransform;
                rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f); rt.pivot = new Vector2(0, 0.5f);
                rt.anchoredPosition = new Vector2(x0, dy); rt.sizeDelta = new Vector2(x1 - x0, 16);
            }

            /// <summary>お宝の図鑑: 宝箱から出る物と、見つけた回数。</summary>
            private void RefreshTreasureCodex()
            {
                var defs = m.Config.adventure?.treasures;
                if (defs == null) return;
                float w = list.rect.width > 0 ? list.rect.width : 704f;
                int seenTotal = 0;
                foreach (var t in defs) if (m.Ach.Get(AchievementCounters.TreasurePrefix + t.id) > 0) seenTotal++;
                head.text = $"見つけたお宝  {seenTotal} / {defs.Count}";
                const float rowH = 44f;
                foreach (var t in defs)
                {
                    long n = m.Ach.Get(AchievementCounters.TreasurePrefix + t.id);
                    bool seen = n > 0;
                    var row = CodexRow(w, rowH, seen);
                    var icon = UiSkin.Img(row.transform, "Icon", Vector2.zero, new Vector2(26, 26), UiSkin.Icon(TreasureIcon(t.kind), 64), seen ? Color.white : new Color(1, 1, 1, 0.2f));
                    icon.rectTransform.anchorMin = new Vector2(0, 0.5f); icon.rectTransform.anchorMax = new Vector2(0, 0.5f); icon.rectTransform.anchoredPosition = new Vector2(24, 0);
                    var name = UiFactory.Label(row.transform, "Name", Vector2.zero, Vector2.zero, seen ? t.name : "？？？", 13, TextAnchor.MiddleLeft, seen ? UiSkin.Text : UiSkin.TextDim);
                    name.fontStyle = FontStyle.Bold; Side(name.rectTransform, 48, 260);
                    var desc = UiFactory.Label(row.transform, "Desc", Vector2.zero, Vector2.zero, seen ? TreasureDesc(t) : "宝箱から出る", 10, TextAnchor.MiddleLeft, UiSkin.TextSub);
                    Side(desc.rectTransform, 260, w - 120);
                    var cnt = UiFactory.Label(row.transform, "Count", Vector2.zero, Vector2.zero, seen ? $"×{n:N0}" : "", 11, TextAnchor.MiddleRight, UiSkin.Gold);
                    Side(cnt.rectTransform, w - 110, w - 12);
                }
            }

            private static string TreasureIcon(string kind)
            {
                switch (kind)
                {
                    case "souls": return "soul";
                    case "atSpins": return "book";
                    case "atExpect": return "amulet";
                    case "exp": return "book";
                    case "torch": return "potion";
                    case "embers": return "ember";
                    default: return "amulet";
                }
            }

            private static string TreasureDesc(TreasureDef t)
            {
                switch (t.kind)
                {
                    case "souls": return $"ソウル +{t.amount:N0}";
                    case "atSpins": return $"次の洞窟（AT）に +{t.amount}G";
                    case "atExpect": return $"次のボーナスの AT 期待度 +{t.amount}%";
                    case "exp": return $"EXP +{t.amount:N0}";
                    case "torch": return $"回復薬 +{t.amount}";
                    case "embers": return $"エンバー +{t.amount:N0}";
                    default: return t.kind;
                }
            }

            /// <summary>行の中で、左端から x0〜x1 の帯に置く（縦は行いっぱい）。</summary>
            private static void Side(RectTransform rt, float x0, float x1)
            {
                rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 0.5f);
                rt.anchoredPosition = new Vector2(x0, 0);
                rt.sizeDelta = new Vector2(x1 - x0, 0);
            }
        }
    }
}
