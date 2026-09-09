using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// 画像素材なしで「角丸・影・グラデ・発光」を出すための実行時生成スプライト集と、
    /// それを使った部品ビルダー。全画面で同じ見た目を使い回すために一元化する。
    /// 色の役割（game-design §13）: 自分=緑 / 敵・危険=赤 / 中立=金 / 選択=アクセント1色。
    /// </summary>
    public static class UiSkin
    {
        // ------------------------------------------------------------ colors
        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        public static readonly Color Bg = Hex("#0a0d16");
        public static readonly Color Panel = Hex("#151b2b");
        public static readonly Color PanelHi = Hex("#1c2438");
        public static readonly Color PanelEdge = Hex("#2a3554");
        public static readonly Color InsetColor = Hex("#0b0f19");
        public static readonly Color Text = Hex("#f4f6fa");
        public static readonly Color TextSub = Hex("#98a3b8");
        public static readonly Color TextDim = Hex("#5f6a80");
        public static readonly Color Accent = Hex("#ff4d6d");
        public static readonly Color Gold = Hex("#ffcf3f");
        public static readonly Color GoldDeep = Hex("#c8961e");
        public static readonly Color Green = Hex("#3ddc84");
        public static readonly Color Blue = Hex("#4da3ff");
        public static readonly Color Btn = Hex("#27304a");
        public static readonly Color BtnDisabled = Hex("#161c2b");

        // ----------------------------------------------------------- sprites
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>角丸矩形（9スライス）。radius は px。</summary>
        public static Sprite Rounded(int radius)
        {
            string key = "r" + radius;
            if (_cache.TryGetValue(key, out var s)) return s;
            int size = radius * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * RoundedAlpha(x, y, size, size, radius, 1f)));
            tex.SetPixels32(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect, new Vector4(radius + 1, radius + 1, radius + 1, radius + 1));
            _cache[key] = s;
            return s;
        }

        /// <summary>角丸のぼかし影（9スライス）。blur はぼかし幅 px。</summary>
        public static Sprite Shadow(int radius, int blur)
        {
            string key = $"s{radius}_{blur}";
            if (_cache.TryGetValue(key, out var s)) return s;
            int pad = blur + radius;
            int size = pad * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // 内側の角丸矩形（blur ぶん内側）からの距離でフェード
                    float d = RoundedDistance(x + 0.5f, y + 0.5f, blur, blur, size - blur, size - blur, radius);
                    float a = d <= 0 ? 1f : Mathf.Clamp01(1f - d / blur);
                    a = a * a * (3f - 2f * a);   // smoothstep
                    px[y * size + x] = new Color32(0, 0, 0, (byte)(255 * a));
                }
            tex.SetPixels32(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect, new Vector4(pad + 1, pad + 1, pad + 1, pad + 1));
            _cache[key] = s;
            return s;
        }

        /// <summary>縦グラデ（上=白、下=透明）。色は Image.color で付ける。</summary>
        public static Sprite GradientV(bool topOpaque = true)
        {
            string key = "gv" + (topOpaque ? 1 : 0);
            if (_cache.TryGetValue(key, out var s)) return s;
            const int h = 64;
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                float a = topOpaque ? t : 1f - t;
                tex.SetPixel(0, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
            _cache[key] = s;
            return s;
        }

        /// <summary>円（ソフトエッジ）。ランプ・発光に使う。</summary>
        public static Sprite Circle(int diameter, float softness = 0.08f)
        {
            string key = $"c{diameter}_{softness}";
            if (_cache.TryGetValue(key, out var s)) return s;
            var tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float r = diameter * 0.5f;
            for (int y = 0; y < diameter; y++)
                for (int x = 0; x < diameter; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    float a = Mathf.Clamp01((1f - d) / softness);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, diameter, diameter), new Vector2(0.5f, 0.5f), 1f);
            _cache[key] = s;
            return s;
        }

        /// <summary>放射グロー（中心不透明→外側透明、二乗フェード）。</summary>
        public static Sprite Glow(int diameter)
        {
            string key = "g" + diameter;
            if (_cache.TryGetValue(key, out var s)) return s;
            var tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float r = diameter * 0.5f;
            for (int y = 0; y < diameter; y++)
                for (int x = 0; x < diameter; x++)
                {
                    float d = Mathf.Clamp01(Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r);
                    float a = (1f - d) * (1f - d);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, diameter, diameter), new Vector2(0.5f, 0.5f), 1f);
            _cache[key] = s;
            return s;
        }

        private static float RoundedAlpha(int x, int y, int w, int h, int radius, float aa)
        {
            float d = RoundedDistance(x + 0.5f, y + 0.5f, 0, 0, w, h, radius);
            return Mathf.Clamp01(0.5f - d / aa);
        }

        /// <summary>角丸矩形 (x0,y0)-(x1,y1) の縁からの符号付き距離（内側が負）。</summary>
        private static float RoundedDistance(float px, float py, float x0, float y0, float x1, float y1, float radius)
        {
            float cx = Mathf.Clamp(px, x0 + radius, x1 - radius);
            float cy = Mathf.Clamp(py, y0 + radius, y1 - radius);
            float dx = px - cx, dy = py - cy;
            // 角丸矩形の SDF（内側は -radius で頭打ち。縁付近の AA と影のフェードにはこれで十分）
            return Mathf.Sqrt(dx * dx + dy * dy) - radius;
        }

        // ---------------------------------------------------------- builders
        public static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Img(Transform parent, string name, Vector2 pos, Vector2 size, Sprite sprite, Color color, bool raycast = false)
        {
            var rt = Rect(parent, name, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border.sqrMagnitude > 0) img.type = Image.Type.Sliced;
            return img;
        }

        /// <summary>親いっぱいに広げる。</summary>
        public static void Stretch(RectTransform rt, float pad = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad); rt.offsetMax = new Vector2(-pad, -pad);
        }

        /// <summary>影付き角丸カード。返すのは中身の親（カード本体）。</summary>
        public static RectTransform Card(Transform parent, string name, Vector2 pos, Vector2 size, int radius = 12, Color? color = null, bool edge = true, bool shadow = true, bool sheen = true)
        {
            var root = Rect(parent, name, pos, size);
            if (shadow)
            {
                var sh = Img(root, "Shadow", new Vector2(0, -6), size + new Vector2(24, 24), Shadow(radius, 14), new Color(0, 0, 0, 0.55f));
            }
            if (edge)
            {
                var e = Img(root, "Edge", Vector2.zero, size, Rounded(radius), PanelEdge);
            }
            var body = Img(root, "Body", Vector2.zero, edge ? size - new Vector2(2, 2) : size, Rounded(Mathf.Max(2, radius - 1)), color ?? Panel, true);
            if (sheen)
            {
                // 上 40% にうっすら光沢
                var s = Img(root, "Sheen", new Vector2(0, size.y * 0.3f), new Vector2(size.x - 2, size.y * 0.4f), GradientV(true), new Color(1, 1, 1, 0.045f));
                s.type = Image.Type.Simple;
            }
            return root;
        }

        /// <summary>くぼんだ表示器（数値・リール窓の下地）。</summary>
        public static RectTransform Inset(Transform parent, string name, Vector2 pos, Vector2 size, int radius = 8, Color? color = null)
        {
            var root = Rect(parent, name, pos, size);
            Img(root, "Body", Vector2.zero, size, Rounded(radius), color ?? InsetColor);
            // 上辺に落ちる内影
            var top = Img(root, "InnerShade", new Vector2(0, size.y * 0.5f - size.y * 0.2f), new Vector2(size.x - 4, size.y * 0.4f), GradientV(true), new Color(0, 0, 0, 0.45f));
            top.type = Image.Type.Simple;
            // 下辺にかすかな縁光
            Img(root, "BottomLine", new Vector2(0, -size.y * 0.5f + 0.5f), new Vector2(size.x - radius, 1), null, new Color(1, 1, 1, 0.06f));
            return root;
        }

        /// <summary>小さな色付きチップ（状態表示）。返すのはラベル。</summary>
        public static Text Chip(Transform parent, string name, Vector2 pos, Vector2 size, string text, Color bg, Color fg, int fontSize = 12)
        {
            var root = Rect(parent, name, pos, size);
            Img(root, "Bg", Vector2.zero, size, Rounded(Mathf.RoundToInt(size.y * 0.5f)), bg);
            var t = UiFactory.Label(root, "Label", Vector2.zero, size, text, fontSize, TextAnchor.MiddleCenter, fg);
            t.fontStyle = FontStyle.Bold;
            return t;
        }

        /// <summary>角丸ボタン（影・光沢・押下色つき）。上辺の LED は lamp に返す（null 可）。</summary>
        public static Button Button(Transform parent, string name, Vector2 pos, Vector2 size, string text, System.Action onClick, Color color, int fontSize = 18, bool lamp = false, int radius = 10)
        {
            var root = Rect(parent, name, pos, size);
            Img(root, "Shadow", new Vector2(0, -4), size + new Vector2(16, 16), Shadow(radius, 10), new Color(0, 0, 0, 0.5f));
            var body = Img(root, "Body", Vector2.zero, size, Rounded(radius), color, true);
            var sheen = Img(root, "Sheen", new Vector2(0, size.y * 0.25f), new Vector2(size.x - 4, size.y * 0.5f), GradientV(true), new Color(1, 1, 1, 0.10f));
            sheen.type = Image.Type.Simple;
            Img(root, "Bottom", new Vector2(0, -size.y * 0.5f + 1.5f), new Vector2(size.x - radius * 2, 3), Rounded(2), new Color(0, 0, 0, 0.35f));
            var label = UiFactory.Label(root, "Label", Vector2.zero, size, text, fontSize, TextAnchor.MiddleCenter, Text);
            label.fontStyle = FontStyle.Bold;
            var btn = root.gameObject.AddComponent<Button>();
            btn.targetGraphic = body;
            var cb = btn.colors;
            cb.normalColor = color;
            cb.highlightedColor = color * 1.18f;
            cb.pressedColor = color * 0.72f;
            cb.selectedColor = color;
            cb.disabledColor = BtnDisabled;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.06f;
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick?.Invoke());
            if (lamp)
            {
                var l = Img(root, "Lamp", new Vector2(0, size.y * 0.5f - 5), new Vector2(size.x * 0.5f, 3), Rounded(2), new Color(1, 1, 1, 0.15f));
                l.name = "Lamp";
            }
            return btn;
        }

        public static void SetButtonColor(Button b, Color color, Color? textColor = null)
        {
            var cb = b.colors;
            cb.normalColor = color;
            cb.highlightedColor = color * 1.18f;
            cb.pressedColor = color * 0.72f;
            cb.selectedColor = color;
            b.colors = cb;
            var t = b.GetComponentInChildren<Text>();
            if (t != null && textColor.HasValue) t.color = textColor.Value;
        }

        /// <summary>ボタン上辺の LED を点灯/消灯。</summary>
        public static void SetLamp(Button b, bool on, Color onColor)
        {
            var lamp = b.transform.Find("Lamp");
            if (lamp == null) return;
            lamp.GetComponent<Image>().color = on ? onColor : new Color(1, 1, 1, 0.12f);
        }

        public static Text SetButtonText(Button b, string text)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
            return t;
        }

        /// <summary>丸いアイコンボタン（⚙ など1文字）。</summary>
        public static Button IconButton(Transform parent, string name, Vector2 pos, float diameter, string glyph, System.Action onClick, Color color, int fontSize = 16)
        {
            var root = Rect(parent, name, pos, new Vector2(diameter, diameter));
            Img(root, "Shadow", new Vector2(0, -3), new Vector2(diameter + 14, diameter + 14), Shadow(Mathf.RoundToInt(diameter * 0.5f), 8), new Color(0, 0, 0, 0.45f));
            var body = Img(root, "Body", Vector2.zero, new Vector2(diameter, diameter), Rounded(Mathf.RoundToInt(diameter * 0.5f)), color, true);
            var label = UiFactory.Label(root, "Label", new Vector2(0, 1), new Vector2(diameter, diameter), glyph, fontSize, TextAnchor.MiddleCenter, Text);
            label.fontStyle = FontStyle.Bold;
            var btn = root.gameObject.AddComponent<Button>();
            btn.targetGraphic = body;
            var cb = btn.colors;
            cb.normalColor = color; cb.highlightedColor = color * 1.2f; cb.pressedColor = color * 0.7f; cb.selectedColor = color; cb.disabledColor = BtnDisabled; cb.colorMultiplier = 1f; cb.fadeDuration = 0.06f;
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        /// <summary>ゲージ（角丸トラック＋フィル）。fill の幅を 0..trackWidth で更新する。</summary>
        public static Image Gauge(Transform parent, string name, Vector2 pos, Vector2 size, Color fillColor, out RectTransform track)
        {
            track = Rect(parent, name, pos, size);
            int r = Mathf.RoundToInt(size.y * 0.5f);
            Img(track, "Track", Vector2.zero, size, Rounded(r), new Color(0, 0, 0, 0.5f));
            var fill = Img(track, "Fill", Vector2.zero, new Vector2(0, size.y), Rounded(r), fillColor);
            var frt = fill.rectTransform;
            frt.anchorMin = new Vector2(0, 0.5f); frt.anchorMax = new Vector2(0, 0.5f); frt.pivot = new Vector2(0, 0.5f); frt.anchoredPosition = Vector2.zero;
            var sheen = Img(fill.rectTransform, "Sheen", Vector2.zero, Vector2.zero, GradientV(true), new Color(1, 1, 1, 0.25f));
            sheen.type = Image.Type.Simple;
            Stretch(sheen.rectTransform);
            return fill;
        }

        /// <summary>見出し（小さい大文字ラベル + 下線）。</summary>
        public static Text Heading(Transform parent, string name, Vector2 pos, float width, string text)
        {
            var t = UiFactory.Label(parent, name, pos, new Vector2(width, 14), text, 11, TextAnchor.MiddleLeft, TextSub);
            t.fontStyle = FontStyle.Bold;
            Img(parent, name + "Line", new Vector2(pos.x, pos.y - 10), new Vector2(width, 1), null, new Color(1, 1, 1, 0.08f));
            return t;
        }

        /// <summary>数値表示（桁固定・右寄せ・太字）。</summary>
        public static Text Number(Transform parent, string name, Vector2 pos, Vector2 size, string text, int fontSize, Color color)
        {
            var t = UiFactory.Label(parent, name, pos, size, text, fontSize, TextAnchor.MiddleRight, color);
            t.fontStyle = FontStyle.Bold;
            return t;
        }

        /// <summary>縦グラデの暗幕（HUD の下地）。topDark=true で上が暗い。</summary>
        public static Image Vignette(Transform parent, string name, Vector2 pos, Vector2 size, float alpha, bool topDark)
        {
            var img = Img(parent, name, pos, size, GradientV(topDark), new Color(0, 0, 0, alpha));
            img.type = Image.Type.Simple;
            return img;
        }
    }
}
