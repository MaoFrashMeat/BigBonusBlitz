using BBB.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// いま受けている呪いと祝福の一覧。1 組ずつ、左に呪い・右に祝福、受けた深さ。
    /// 装備画面の「呪いと祝福」から開く。潜行の間だけ続き、街へ戻ると消える。
    /// 板は細い縁の紺（panel_navy_sm）。題は左上の紺のタブ。
    /// </summary>
    public static class CurseScreen
    {
        private static readonly Color ColCurse = UiSkin.Hex("#c060ff");

        public static GameObject Build(Transform stage, SlotMachine m, AudioManager audio, System.Action onClose)
        {
            const float W = 760f, H = 500f, edge = 22f;
            var overlay = UiFactory.Panel(stage, "CurseListOverlay", Vector2.zero, new Vector2(4000, 4000), new Color(0, 0, 0, 0.62f));
            var closeBtn = overlay.gameObject.AddComponent<Button>();
            closeBtn.transition = Selectable.Transition.None;
            closeBtn.onClick.AddListener(() => onClose?.Invoke());
            var card = UiSkin.Card(overlay, "Card", Vector2.zero, new Vector2(W, H), 14, frameOverride: "panel_navy_sm");
            var eat = card.gameObject.AddComponent<Button>();        // カード内のタップは閉じない
            eat.transition = Selectable.Transition.None;
            const float tabW = 170f, tabH = 40f;
            var tab = UiSkin.Img(card, "Tab", new Vector2(-W * 0.5f + edge + tabW * 0.5f, H * 0.5f - 4), new Vector2(tabW, tabH), UiSkin.Frame("pill_navy_sm"), Color.white);
            var title = UiFactory.Label(tab.transform, "Title", new Vector2(0, 1), new Vector2(tabW, tabH), "呪いと祝福", 15, TextAnchor.MiddleCenter, UiSkin.Text);
            title.fontStyle = FontStyle.Bold;
            UiSkin.IconButton(card, "Close", new Vector2(W * 0.5f - edge, H * 0.5f - 4), 30, "×", () => onClose?.Invoke(), UiSkin.Btn, 16);

            var taken = m.Curse.Taken;
            int max = m.Config.curse?.maxStack ?? 0;
            var head = UiFactory.Label(card, "Head", new Vector2(0, H * 0.5f - 54), new Vector2(W - 60, 18),
                max > 0 ? $"受けている数  {taken.Count} / {max}    街へ戻ると全部消える" : "街へ戻ると全部消える", 12, TextAnchor.MiddleCenter, UiSkin.TextSub);
            UiSkin.ScrollBox(card, "List", new Vector2(0, -34), new Vector2(704, 360), out var list);
            float w = 704f;

            if (taken.Count == 0)
            {
                var empty = UiFactory.Label(list, "Empty", Vector2.zero, new Vector2(w, 80), "いま受けている呪いはない\n段を進むと申し出が来ることがある（受けると呪いと祝福が 1 つずつ付く）", 13, TextAnchor.MiddleCenter, UiSkin.TextSub);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
                return overlay.gameObject;
            }

            const float rowH = 96f;
            for (int i = 0; i < taken.Count; i++)
            {
                var c = taken[i];
                if (c == null) continue;
                var row = UiSkin.Img(list, "Row" + i, Vector2.zero, new Vector2(w, rowH), UiSkin.Rounded(8), new Color(1, 1, 1, 0.05f));
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = rowH;
                // 左: 呪い / 右: 祝福（申し出の窓と同じ並び）
                Half(row.transform, "Curse", -w * 0.25f + 2, w * 0.5f - 16, rowH, "呪い", c.curseName, Text(c.curseEffect, c.curseValue, true), ColCurse);
                Half(row.transform, "Bless", w * 0.25f - 2, w * 0.5f - 16, rowH, "祝福", c.blessName, Text(c.blessEffect, c.blessValue, false), UiSkin.Gold);
                var depth = UiFactory.Label(row.transform, "Depth", new Vector2(0, -rowH * 0.5f + 11), new Vector2(200, 14), $"深さ {c.depth} で受けた", 10, TextAnchor.MiddleCenter, UiSkin.TextSub);
                depth.rectTransform.anchorMin = depth.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            }
            return overlay.gameObject;
        }

        /// <summary>行の片側（種類 / 名前 / 効果）。</summary>
        private static void Half(Transform row, string name, float cx, float w, float h, string kind, string title, string desc, Color color)
        {
            var root = UiSkin.Rect(row, name, new Vector2(cx, 4), new Vector2(w, h - 16));
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            UiSkin.Img(root, "Bar", new Vector2(-w * 0.5f + 3, 0), new Vector2(4, h - 24), UiSkin.Rounded(2), color);
            var k = UiFactory.Label(root, "Kind", new Vector2(8, h * 0.5f - 18), new Vector2(w - 24, 14), kind, 10, TextAnchor.MiddleLeft, color);
            k.fontStyle = FontStyle.Bold;
            var t = UiFactory.Label(root, "Name", new Vector2(8, h * 0.5f - 36), new Vector2(w - 24, 20), title, 14, TextAnchor.MiddleLeft, UiSkin.Text);
            t.fontStyle = FontStyle.Bold;
            var d = UiFactory.Label(root, "Desc", new Vector2(8, h * 0.5f - 56), new Vector2(w - 24, 18), desc, 11, TextAnchor.MiddleLeft, UiSkin.TextSub);
            d.horizontalOverflow = HorizontalWrapMode.Wrap; d.verticalOverflow = VerticalWrapMode.Truncate;
        }

        /// <summary>効果の 1 行説明（呪い側は「減る」向き、祝福側は「増える」向き）。</summary>
        public static string Text(string effect, int value, bool isCurse)
        {
            string v = value.ToString();
            switch (effect)
            {
                case CurseEffects.TorchDrain: return $"ライフの減りが {v}% 速くなる";
                case CurseEffects.ConditionHarder: return $"ルートの必要回数が +{v}";
                case CurseEffects.PayoutCut: return $"払い出しが {v}% 減る";
                case CurseEffects.BetExtra: return $"1 回転あたり {v} 多く灯を使う";
                default:
                    return $"{EquipDirector.EffectName(effect)} {(isCurse ? "-" : "+")}{v}{EquipDirector.EffectUnit(effect)}";
            }
        }
    }
}
