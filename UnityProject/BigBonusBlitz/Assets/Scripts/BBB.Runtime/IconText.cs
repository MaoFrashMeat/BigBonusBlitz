using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// 文字の並びの中にアイコンを混ぜて 1 行に置く。「{soul}+90  {ember}+50」のように書くと、
    /// {soul} / {ember} / {exp} / {life} / {torch} がアイコンになる（2026-09-13 本人: SOUL や EMB は文字でなく絵で）。
    /// 横幅は文字の実測（preferredWidth）で並べ、全体を host の中央に寄せる。
    /// </summary>
    public static class IconText
    {
        /// <summary>アイコンの指定が入っているか。</summary>
        public static bool HasIcons(string s) => !string.IsNullOrEmpty(s) && s.IndexOf('{') >= 0 && s.IndexOf('}') >= 0;

        /// <summary>置いた文字の部品（色を後から変えるため）。</summary>
        public sealed class Row
        {
            public readonly List<Text> texts = new List<Text>();
            public readonly List<Image> icons = new List<Image>();
            public float width;
            public void SetColor(Color c) { foreach (var t in texts) if (t != null) t.color = c; }
        }

        /// <summary>
        /// host の中を作り直して text を並べる。fontSize に対して iconSize（0 なら fontSize × 1.15）の絵。
        /// shadow が正なら文字に影。
        /// </summary>
        public static Row Render(RectTransform host, string text, int fontSize, Color color, FontStyle style = FontStyle.Bold,
                                 float iconSize = 0f, float shadow = 0.6f, float gap = 3f, bool center = true,
                                 Color? shadowColor = null, Vector2? shadowDistance = null)
        {
            for (int i = host.childCount - 1; i >= 0; i--) Object.Destroy(host.GetChild(i).gameObject);
            var row = new Row();
            if (iconSize <= 0f) iconSize = fontSize * 1.15f;
            var parts = Split(text ?? "");
            // まず作って幅を測る
            var made = new List<RectTransform>();
            var widths = new List<float>();
            foreach (var (isIcon, s) in parts)
            {
                if (isIcon)
                {
                    var img = UiSkin.Img(host, "Icon", Vector2.zero, new Vector2(iconSize, iconSize), UiSkin.Icon(s, 64), Color.white);
                    img.preserveAspect = true;
                    row.icons.Add(img);
                    made.Add(img.rectTransform); widths.Add(iconSize);
                }
                else
                {
                    var t = UiFactory.Label(host, "T", Vector2.zero, new Vector2(10, host.sizeDelta.y), s, fontSize, TextAnchor.MiddleLeft, color);
                    t.fontStyle = style;
                    if (shadow > 0f)
                    {
                        var sh = t.gameObject.AddComponent<Shadow>();
                        sh.effectColor = shadowColor ?? new Color(0, 0, 0, shadow);
                        sh.effectDistance = shadowDistance ?? new Vector2(1, -2);
                    }
                    float w = Mathf.Ceil(t.preferredWidth);
                    t.rectTransform.sizeDelta = new Vector2(w, host.sizeDelta.y);
                    row.texts.Add(t);
                    made.Add(t.rectTransform); widths.Add(w);
                }
            }
            float total = 0f;
            for (int i = 0; i < widths.Count; i++) total += widths[i] + (i > 0 ? gap : 0f);
            row.width = total;
            // 中央寄せ（または左寄せ）で左から置く（pivot は中央のまま、中心座標で置く）
            float x = center ? -total * 0.5f : -host.sizeDelta.x * 0.5f;
            for (int i = 0; i < made.Count; i++)
            {
                if (i > 0) x += gap;
                made[i].anchoredPosition = new Vector2(x + widths[i] * 0.5f, 0);
                x += widths[i];
            }
            return row;
        }

        /// <summary>"{soul}" のような印で分ける。印の中身がアイコンの名前。</summary>
        private static List<(bool isIcon, string s)> Split(string text)
        {
            var list = new List<(bool, string)>();
            int i = 0;
            while (i < text.Length)
            {
                int open = text.IndexOf('{', i);
                int close = open >= 0 ? text.IndexOf('}', open + 1) : -1;
                if (open < 0 || close < 0)
                {
                    list.Add((false, text.Substring(i)));
                    break;
                }
                if (open > i) list.Add((false, text.Substring(i, open - i)));
                string name = text.Substring(open + 1, close - open - 1).Trim().ToLowerInvariant();
                list.Add((true, IconName(name)));
                i = close + 1;
            }
            // 空の文字は捨てる
            list.RemoveAll(p => !p.Item1 && string.IsNullOrEmpty(p.Item2));
            return list;
        }

        /// <summary>印 → UiSkin.Icon の種類。</summary>
        private static string IconName(string key)
        {
            switch (key)
            {
                case "soul": case "souls": return "soul";
                case "ember": case "embers": case "emb": return "ember";
                case "exp": return "book";
                case "life": case "hp": return "potion";
                case "torch": return "potion";
                default: return key;
            }
        }
    }
}
