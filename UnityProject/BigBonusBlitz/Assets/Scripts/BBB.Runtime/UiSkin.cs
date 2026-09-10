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
        public static readonly Color Bg = Hex("#0a0b0f");
        /// <summary>板そのもの。黒鉄。</summary>
        public static readonly Color Panel = Hex("#17181e");
        public static readonly Color PanelHi = Hex("#23252d");
        /// <summary>板の縁。火に焼けた鉄の色。</summary>
        public static readonly Color PanelEdge = Hex("#5f3d1e");
        public static readonly Color InsetColor = Hex("#0b0c10");
        /// <summary>縁からにじむ灯り（エンバー）。</summary>
        public static readonly Color Ember = Hex("#ff8a3a");
        /// <summary>鋲の頭と、その光。</summary>
        public static readonly Color Rivet = Hex("#46484f");
        public static readonly Color RivetHi = Hex("#9296a4");
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

        /// <summary>Play に入るたびにキャッシュを捨てる（前回の破棄済み Sprite を掴まないように）。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() { _cache.Clear(); }

        /// <summary>角丸矩形（9スライス）。radius は px。</summary>
        public static Sprite Rounded(int radius)
        {
            string key = "r" + radius;
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
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
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
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
                    // 白で作る。影にも灯りにも使えるよう、色は Image.color 側で付ける
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * a));
                }
            tex.SetPixels32(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect, new Vector4(pad + 1, pad + 1, pad + 1, pad + 1));
            _cache[key] = s;
            return s;
        }

        /// <summary>横グラデ（rightOpaque が false なら 左=白 → 右=透明）。色は Image.color で付ける。</summary>
        public static Sprite GradientH(bool rightOpaque = true)
        {
            string key = "gh" + (rightOpaque ? 1 : 0);
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
            const int w = 64;
            var tex = new Texture2D(w, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < w; x++)
            {
                float t = (float)x / (w - 1);
                // 端は一気に落とさず、真ん中あたりで消えるようにする
                float a = rightOpaque ? t : 1f - t;
                tex.SetPixel(x, 0, new Color(1, 1, 1, a * a));
            }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, w, 1), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
            _cache[key] = s;
            return s;
        }

        /// <summary>縦グラデ（上=白、下=透明）。色は Image.color で付ける。</summary>
        public static Sprite GradientV(bool topOpaque = true)
        {
            string key = "gv" + (topOpaque ? 1 : 0);
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
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
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
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
        /// <summary>桜の花びら 1 枚。先が割れた楕円。色は Image.color で付ける。</summary>
        public static Sprite Petal(int size = 64)
        {
            string key = "petal" + size;
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // -1〜1 に直す。縦長の楕円にして、上の先端に切れ込みを入れる
                    float u = (x + 0.5f) / size * 2f - 1f;
                    float v = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(u * u / 0.34f + v * v / 0.92f);   // 1 で楕円の縁
                    float a = Mathf.Clamp01((1f - d) * size * 0.16f);
                    // 上端の切れ込み
                    float notch = Mathf.Sqrt(u * u / 0.10f + (v - 1.02f) * (v - 1.02f) / 0.08f);
                    if (notch < 1f) a *= Mathf.Clamp01(notch * 2.2f);
                    // 中心をほんの少し明るく（重ねたとき厚みが出る）
                    byte c = (byte)(255 - Mathf.Clamp01(d) * 26f);
                    px[y * size + x] = new Color32(255, c, c, (byte)(255 * a));
                }
            tex.SetPixels32(px); tex.Apply();
            s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            _cache[key] = s;
            return s;
        }

        public static Sprite Glow(int diameter)
        {
            string key = "g" + diameter;
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
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

        // ------------------------------------------------------------ icons
        /// <summary>
        /// スキル・装備のアイコン。素材を使わず距離関数で描く（Resources/Art/Icons に同名の絵があればそちらを優先）。
        /// kind: sword / soul / eye / book / lantern / amulet / oil / potion / boots / shield
        /// </summary>
        public static Sprite Icon(string kind, int size = 64)
        {
            string key = "icon_" + kind + "_" + size;
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);

            switch (kind)
            {
                case "sword": DrawSword(px, size); break;
                case "soul": DrawSoul(px, size); break;
                case "eye": DrawEye(px, size); break;
                case "book": DrawBook(px, size); break;
                case "lantern": DrawLantern(px, size); break;
                case "amulet": DrawAmulet(px, size); break;
                case "oil": DrawOil(px, size); break;
                case "potion": DrawPotion(px, size); break;
                case "boots": DrawBoots(px, size); break;
                case "shield": DrawShield(px, size); break;
                case "lock": DrawLock(px, size); break;
                case "chain": DrawChain(px, size); break;
                case "ember": DrawEmber(px, size); break;
                case "bell": DrawBell(px, size); break;
                case "gear": DrawGear(px, size); break;
                case "link": DrawLink(px, size); break;
                default: DrawAmulet(px, size); break;
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply();
            var s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _cache[key] = s;
            return s;
        }

        // --- 描画の下ごしらえ（すべて -1..1 の座標で考える）---

        /// <summary>距離関数 d(x,y) が 0 未満の所を色で塗る。境界はぼかす。</summary>
        private static void Paint(Color32[] px, int size, System.Func<float, float, float> sdf, Color color)
        {
            float aa = 2.2f / size;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f;
                    float v = (y + 0.5f) / size * 2f - 1f;
                    float d = sdf(u, v);
                    float a = Mathf.Clamp01(0.5f - d / aa);
                    if (a <= 0.001f) continue;
                    px[y * size + x] = Over(px[y * size + x], color, a * color.a);
                }
        }

        private static Color32 Over(Color32 dst, Color src, float a)
        {
            float da = dst.a / 255f;
            float outA = a + da * (1f - a);
            if (outA <= 0.0001f) return new Color32(0, 0, 0, 0);
            float r = (src.r * a + (dst.r / 255f) * da * (1f - a)) / outA;
            float g = (src.g * a + (dst.g / 255f) * da * (1f - a)) / outA;
            float b = (src.b * a + (dst.b / 255f) * da * (1f - a)) / outA;
            return new Color32((byte)(Mathf.Clamp01(r) * 255), (byte)(Mathf.Clamp01(g) * 255), (byte)(Mathf.Clamp01(b) * 255), (byte)(Mathf.Clamp01(outA) * 255));
        }

        private static float SdCircle(float x, float y, float cx, float cy, float r)
            => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

        private static float SdBox(float x, float y, float cx, float cy, float w, float h, float round = 0f)
        {
            float dx = Mathf.Abs(x - cx) - w + round;
            float dy = Mathf.Abs(y - cy) - h + round;
            float outside = Mathf.Sqrt(Mathf.Max(dx, 0) * Mathf.Max(dx, 0) + Mathf.Max(dy, 0) * Mathf.Max(dy, 0));
            return outside + Mathf.Min(Mathf.Max(dx, dy), 0f) - round;
        }

        /// <summary>線分（太さ th）。</summary>
        private static float SdSeg(float x, float y, float ax, float ay, float bx, float by, float th)
        {
            float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / Mathf.Max(1e-5f, bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy) - th;
        }

        /// <summary>上下に伸びる菱形（刀身や炎の芯に使う）。</summary>
        private static float SdDiamond(float x, float y, float cx, float cy, float w, float h)
        {
            float dx = Mathf.Abs(x - cx) / Mathf.Max(1e-5f, w);
            float dy = Mathf.Abs(y - cy) / Mathf.Max(1e-5f, h);
            return (dx + dy - 1f) * Mathf.Min(w, h);
        }

        /// <summary>5 稜の星。</summary>
        private static float SdStar(float x, float y, float cx, float cy, float r)
        {
            float px2 = x - cx, py2 = y - cy;
            float ang = Mathf.Atan2(py2, px2) - Mathf.PI * 0.5f;
            float len = Mathf.Sqrt(px2 * px2 + py2 * py2);
            float seg = Mathf.PI * 2f / 5f;
            float a = Mathf.Repeat(ang + seg * 0.5f, seg) - seg * 0.5f;
            float rr = r * (0.55f + 0.45f * Mathf.Cos(a * 2.5f));
            return len - rr;
        }

        private static readonly Color IconEdge = new Color(0.02f, 0.03f, 0.06f, 0.9f);

        private static void DrawSword(Color32[] px, int n)
        {
            var steel = new Color(0.82f, 0.87f, 0.95f);
            var edge = new Color(0.55f, 0.62f, 0.75f);
            var grip = new Color(0.45f, 0.28f, 0.16f);
            var gold = new Color(1f, 0.81f, 0.35f);
            // 刀身
            Paint(px, n, (x, y) => Mathf.Max(SdDiamond(x, y, 0f, 0.18f, 0.20f, 0.72f), -y - 0.20f), edge);
            Paint(px, n, (x, y) => Mathf.Max(SdDiamond(x, y, 0f, 0.20f, 0.13f, 0.66f), -y - 0.18f), steel);
            // 鍔
            Paint(px, n, (x, y) => SdBox(x, y, 0f, -0.30f, 0.50f, 0.075f, 0.05f), gold);
            // 柄
            Paint(px, n, (x, y) => SdBox(x, y, 0f, -0.62f, 0.085f, 0.24f, 0.06f), grip);
            Paint(px, n, (x, y) => SdCircle(x, y, 0f, -0.88f, 0.11f), gold);
        }

        private static void DrawSoul(Color32[] px, int n)
        {
            var outer = new Color(0.45f, 0.32f, 0.85f, 0.55f);
            var mid = new Color(0.62f, 0.48f, 1f);
            var core = new Color(0.92f, 0.90f, 1f);
            Paint(px, n, (x, y) => SdCircle(x, y, 0f, 0f, 0.80f), outer);
            Paint(px, n, (x, y) => SdCircle(x, y, 0f, 0.02f, 0.52f), mid);
            Paint(px, n, (x, y) => SdCircle(x, y, -0.10f, 0.14f, 0.22f), core);
            // 立ちのぼる尾
            Paint(px, n, (x, y) => SdSeg(x, y, 0.02f, 0.55f, -0.10f, 0.92f, 0.055f), mid);
        }

        private static void DrawEye(Color32[] px, int n)
        {
            var white = new Color(0.93f, 0.95f, 1f);
            var iris = new Color(0.25f, 0.72f, 0.95f);
            var pupil = new Color(0.05f, 0.07f, 0.12f);
            var lid = new Color(0.55f, 0.72f, 0.9f);
            // 上下の弧が重なった形（レンズ）
            Paint(px, n, (x, y) => Mathf.Max(SdCircle(x, y, 0f, -0.62f, 0.98f), SdCircle(x, y, 0f, 0.62f, 0.98f)), lid);
            Paint(px, n, (x, y) => Mathf.Max(SdCircle(x, y, 0f, -0.66f, 0.94f), SdCircle(x, y, 0f, 0.66f, 0.94f)), white);
            Paint(px, n, (x, y) => SdCircle(x, y, 0f, 0f, 0.30f), iris);
            Paint(px, n, (x, y) => SdCircle(x, y, 0f, 0f, 0.14f), pupil);
            Paint(px, n, (x, y) => SdCircle(x, y, -0.10f, 0.12f, 0.06f), new Color(1f, 1f, 1f, 0.85f));
        }

        private static void DrawBook(Color32[] px, int n)
        {
            var cover = new Color(0.30f, 0.45f, 0.75f);
            var page = new Color(0.94f, 0.93f, 0.86f);
            var band = new Color(1f, 0.81f, 0.35f);
            Paint(px, n, (x, y) => SdBox(x, y, 0f, 0f, 0.62f, 0.74f, 0.08f), cover);
            Paint(px, n, (x, y) => SdBox(x, y, 0.06f, 0f, 0.50f, 0.62f, 0.05f), page);
            // 中央の綴じ目としおり
            Paint(px, n, (x, y) => SdBox(x, y, -0.44f, 0f, 0.07f, 0.74f, 0.03f), band);
            for (int i = 0; i < 3; i++)
            {
                float yy = 0.28f - i * 0.28f;
                Paint(px, n, (x, y) => SdBox(x, y, 0.10f, yy, 0.32f, 0.035f, 0.02f), new Color(0.55f, 0.58f, 0.66f, 0.8f));
            }
        }

        private static void DrawLantern(Color32[] px, int n)
        {
            var metal = new Color(0.68f, 0.55f, 0.28f);
            var glass = new Color(1f, 0.86f, 0.45f, 0.55f);
            var flame = new Color(1f, 0.65f, 0.15f);
            var hot = new Color(1f, 0.95f, 0.7f);
            // 吊り手
            Paint(px, n, (x, y) => Mathf.Abs(SdCircle(x, y, 0f, 0.66f, 0.26f)) - 0.045f, metal);
            // 本体
            Paint(px, n, (x, y) => SdBox(x, y, 0f, 0.62f, 0.34f, 0.07f, 0.04f), metal);
            Paint(px, n, (x, y) => SdBox(x, y, 0f, -0.05f, 0.44f, 0.56f, 0.12f), metal);
            Paint(px, n, (x, y) => SdBox(x, y, 0f, -0.05f, 0.32f, 0.44f, 0.08f), glass);
            Paint(px, n, (x, y) => SdBox(x, y, 0f, -0.72f, 0.40f, 0.10f, 0.05f), metal);
            // 中の炎
            Paint(px, n, (x, y) => SdDiamond(x, y, 0f, -0.06f, 0.15f, 0.30f), flame);
            Paint(px, n, (x, y) => SdCircle(x, y, 0f, -0.16f, 0.11f), hot);
        }

        private static void DrawAmulet(Color32[] px, int n)
        {
            var chain = new Color(0.72f, 0.74f, 0.80f);
            var ring = new Color(1f, 0.81f, 0.35f);
            var gem = new Color(0.35f, 0.85f, 0.70f);
            Paint(px, n, (x, y) => Mathf.Abs(SdCircle(x, y, 0f, 0.62f, 0.30f)) - 0.05f, chain);
            Paint(px, n, (x, y) => Mathf.Abs(SdCircle(x, y, 0f, -0.18f, 0.60f)) - 0.10f, ring);
            Paint(px, n, (x, y) => SdCircle(x, y, 0f, -0.18f, 0.50f), new Color(0.08f, 0.12f, 0.20f));
            Paint(px, n, (x, y) => SdStar(x, y, 0f, -0.18f, 0.44f), gem);
        }

        private static void DrawOil(Color32[] px, int n)
        {
            var jar = new Color(0.35f, 0.55f, 0.42f);
            var oil = new Color(1f, 0.78f, 0.30f);
            var cork = new Color(0.55f, 0.38f, 0.22f);
            // 壺（下が丸く、首が細い）
            Paint(px, n, (x, y) => SdCircle(x, y, 0f, -0.28f, 0.60f), jar);
            Paint(px, n, (x, y) => SdBox(x, y, 0f, 0.38f, 0.18f, 0.30f, 0.05f), jar);
            Paint(px, n, (x, y) => SdBox(x, y, 0f, 0.70f, 0.25f, 0.09f, 0.04f), cork);
            // 中の油
            Paint(px, n, (x, y) => Mathf.Max(SdCircle(x, y, 0f, -0.28f, 0.46f), -(y + 0.42f) * -1f), oil);
            Paint(px, n, (x, y) => SdCircle(x, y, -0.16f, -0.34f, 0.10f), new Color(1f, 0.94f, 0.65f, 0.8f));
        }

        private static void DrawPotion(Color32[] px, int n)
        {
            var glass = new Color(0.75f, 0.85f, 0.95f, 0.55f);
            var liquid = new Color(0.95f, 0.30f, 0.45f);
            var cork = new Color(0.55f, 0.38f, 0.22f);
            Paint(px, n, (x, y) => SdCircle(x, y, 0f, -0.25f, 0.58f), glass);
            Paint(px, n, (x, y) => SdBox(x, y, 0f, 0.42f, 0.16f, 0.32f, 0.04f), glass);
            Paint(px, n, (x, y) => SdBox(x, y, 0f, 0.74f, 0.22f, 0.10f, 0.04f), cork);
            Paint(px, n, (x, y) => Mathf.Max(SdCircle(x, y, 0f, -0.25f, 0.46f), y - 0.05f), liquid);
        }

        private static void DrawBoots(Color32[] px, int n)
        {
            var leather = new Color(0.48f, 0.32f, 0.20f);
            var sole = new Color(0.25f, 0.20f, 0.18f);
            var band = new Color(1f, 0.81f, 0.35f);
            Paint(px, n, (x, y) => SdBox(x, y, -0.12f, 0.18f, 0.26f, 0.55f, 0.10f), leather);
            Paint(px, n, (x, y) => SdBox(x, y, 0.16f, -0.44f, 0.54f, 0.20f, 0.10f), leather);
            Paint(px, n, (x, y) => SdBox(x, y, 0.10f, -0.66f, 0.62f, 0.10f, 0.05f), sole);
            Paint(px, n, (x, y) => SdBox(x, y, -0.12f, 0.30f, 0.30f, 0.07f, 0.03f), band);
        }

        /// <summary>灯火（エンバー）。芯の白い炎。</summary>
        private static void DrawEmber(Color32[] px, int n)
        {
            var outer = new Color(1f, 0.42f, 0.10f, 0.85f);
            var mid = new Color(1f, 0.66f, 0.18f);
            var core = new Color(1f, 0.94f, 0.72f);
            // 下がふくらみ、上がとがる炎の形
            System.Func<float, float, float, float> flame = (x, y, w) =>
            {
                float t = Mathf.Clamp01((y + 0.85f) / 1.7f);
                float half = w * (0.30f + 0.70f * Mathf.Sin(t * Mathf.PI * 0.92f)) * (1f - t * 0.55f);
                float body = Mathf.Abs(x) - Mathf.Max(0.02f, half);
                return Mathf.Max(body, Mathf.Max(-y - 0.86f, y - 0.90f));
            };
            Paint(px, n, (x, y) => flame(x, y + 0.02f, 0.92f), outer);
            Paint(px, n, (x, y) => flame(x, y - 0.06f, 0.62f), mid);
            Paint(px, n, (x, y) => flame(x, y - 0.20f, 0.32f), core);
        }

        /// <summary>南京錠。掛け金は鋼、本体は真鍮。</summary>
        /// <summary>鐘（お知らせ）。</summary>
        private static void DrawBell(Color32[] px, int n)
        {
            var brass = new Color(0.94f, 0.78f, 0.34f);
            var brassDark = new Color(0.64f, 0.48f, 0.14f);
            // 傘の部分は「円と箱の重なり」で作る
            Paint(px, n, (x, y) => Mathf.Max(SdCircle(x, y, 0f, 0.06f, 0.44f), -0.30f - y), brassDark);
            Paint(px, n, (x, y) => Mathf.Max(SdCircle(x, y, 0f, 0.08f, 0.38f), -0.28f - y), brass);
            Paint(px, n, (x, y) => SdBox(x, y, 0f, -0.30f, 0.50f, 0.08f, 0.05f), brassDark);
            Paint(px, n, (x, y) => SdBox(x, y, 0f, -0.31f, 0.44f, 0.05f, 0.04f), brass);
            // 上のつまみと、下の舌
            Paint(px, n, (x, y) => SdCircle(x, y, 0f, 0.48f, 0.09f), brass);
            Paint(px, n, (x, y) => SdCircle(x, y, 0f, -0.44f, 0.10f), brassDark);
        }

        /// <summary>歯車（設定）。</summary>
        private static void DrawGear(Color32[] px, int n)
        {
            var steel = new Color(0.80f, 0.84f, 0.92f);
            var steelDark = new Color(0.46f, 0.50f, 0.60f);
            // 歯は箱を回して 4 本ぶん重ねる（8 歯に見える）
            for (int k = 0; k < 4; k++)
            {
                float a = k * Mathf.PI / 4f;
                float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
                Paint(px, n, (x, y) =>
                {
                    float rx = x * cs + y * sn, ry = -x * sn + y * cs;
                    return SdBox(rx, ry, 0f, 0f, 0.28f, 0.98f, 0.04f);
                }, steelDark);
            }
            Paint(px, n, (x, y) => SdCircle(x, y, 0f, 0f, 0.40f), steelDark);
            Paint(px, n, (x, y) => SdCircle(x, y, 0f, 0f, 0.34f), steel);
            Paint(px, n, (x, y) => SdCircle(x, y, 0f, 0f, 0.15f), new Color(0.10f, 0.11f, 0.15f));
        }

        /// <summary>鎖の輪 2 つ（引き継ぎ）。</summary>
        private static void DrawLink(Color32[] px, int n)
        {
            var steel = new Color(0.82f, 0.86f, 0.94f);
            var steelDark = new Color(0.48f, 0.52f, 0.62f);
            for (int k = 0; k < 2; k++)
            {
                float cx = k == 0 ? -0.20f : 0.20f;
                float cy = k == 0 ? 0.18f : -0.18f;
                Paint(px, n, (x, y) => Mathf.Abs(SdCircle(x, y, cx, cy, 0.30f)) - 0.11f, steelDark);
                Paint(px, n, (x, y) => Mathf.Abs(SdCircle(x, y, cx, cy, 0.30f)) - 0.075f, steel);
            }
        }

        private static void DrawLock(Color32[] px, int n)
        {
            var steel = new Color(0.78f, 0.82f, 0.90f);
            var steelDark = new Color(0.48f, 0.52f, 0.60f);
            var brass = new Color(0.92f, 0.74f, 0.30f);
            var brassDark = new Color(0.62f, 0.48f, 0.16f);
            var hole = new Color(0.12f, 0.10f, 0.07f);
            // 掛け金（上半分だけの輪）
            Paint(px, n, (x, y) => Mathf.Max(Mathf.Abs(SdCircle(x, y, 0f, 0.26f, 0.36f)) - 0.11f, 0.26f - y), steelDark);
            Paint(px, n, (x, y) => Mathf.Max(Mathf.Abs(SdCircle(x, y, 0f, 0.26f, 0.36f)) - 0.075f, 0.26f - y), steel);
            // 本体
            Paint(px, n, (x, y) => SdBox(x, y, 0f, -0.30f, 0.56f, 0.46f, 0.14f), brassDark);
            Paint(px, n, (x, y) => SdBox(x, y, 0f, -0.30f, 0.50f, 0.40f, 0.12f), brass);
            // 鍵穴
            Paint(px, n, (x, y) => SdCircle(x, y, 0f, -0.20f, 0.14f), hole);
            Paint(px, n, (x, y) => SdBox(x, y, 0f, -0.46f, 0.06f, 0.18f, 0.02f), hole);
        }

        /// <summary>鎖のひとこま（輪が 2 つ）。横に並べて鎖にする。</summary>
        private static void DrawChain(Color32[] px, int n)
        {
            var metal = new Color(0.74f, 0.78f, 0.86f);
            var dark = new Color(0.40f, 0.44f, 0.52f);
            System.Func<float, float, float, float> ring = (x, y, cx) =>
            {
                float dx = (x - cx) / 0.54f, dy = y / 0.72f;
                return Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - 0.70f) - 0.26f;
            };
            Paint(px, n, (x, y) => ring(x, y, -0.44f), dark);
            Paint(px, n, (x, y) => ring(x, y, 0.44f), dark);
            Paint(px, n, (x, y) => ring(x, y, -0.44f) + 0.055f, metal);
            Paint(px, n, (x, y) => ring(x, y, 0.44f) + 0.055f, metal);
        }

        private static void DrawShield(Color32[] px, int n)
        {
            var steel = new Color(0.72f, 0.78f, 0.88f);
            var trim = new Color(1f, 0.81f, 0.35f);
            var crest = new Color(0.30f, 0.50f, 0.85f);
            System.Func<float, float, float> body = (x, y) =>
            {
                float top = SdBox(x, y, 0f, 0.28f, 0.62f, 0.50f, 0.14f);
                float tip = SdDiamond(x, y, 0f, -0.30f, 0.62f, 0.62f);
                return Mathf.Min(top, tip);
            };
            Paint(px, n, (x, y) => body(x, y), trim);
            Paint(px, n, (x, y) => body(x, y) + 0.09f, steel);
            Paint(px, n, (x, y) => SdDiamond(x, y, 0f, 0.05f, 0.26f, 0.40f), crest);
        }

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
        /// <summary>
        /// 板（カード）。黒鉄の板に焼けた縁を付け、外へ灯りをにじませる。
        /// 角の丸みは小さく固定する（板なので、丸いと鉄に見えない）。
        /// </summary>
        public static RectTransform Card(Transform parent, string name, Vector2 pos, Vector2 size, int radius = 12, Color? color = null, bool edge = true, bool shadow = true, bool sheen = true)
        {
            var root = Rect(parent, name, pos, size);
            int r = Mathf.Clamp(radius, 2, 6);          // 鉄板なので角は立てる
            if (shadow)
            {
                Img(root, "Shadow", new Vector2(0, -5), size + new Vector2(20, 20), Shadow(r, 12), new Color(0, 0, 0, 0.62f));
                // 縁からにじむ灯り。影の上に重ねて、板の下に入れる。
                // 板が隣り合うと重なって背景ごと橙になるので、控えめに、狭く。
                var glow = Ember; glow.a = 0.15f;
                Img(root, "Glow", Vector2.zero, size + new Vector2(16, 16), Shadow(r, 9), glow);
            }
            if (edge) Img(root, "Edge", Vector2.zero, size, Rounded(r), PanelEdge);
            var inner = edge ? size - new Vector2(6, 6) : size;
            Img(root, "Body", Vector2.zero, inner, Rounded(Mathf.Max(2, r - 2)), color ?? Panel, true);
            if (sheen)
            {
                // 上から落ちる金属の艶。板の上半分だけ
                var sh = Img(root, "Sheen", new Vector2(0, size.y * 0.22f), new Vector2(inner.x - 2, size.y * 0.5f), GradientV(true), new Color(0.78f, 0.81f, 0.9f, 0.075f));
                sh.type = Image.Type.Simple;
            }
            // 鋲。小さな板に打つと潰れるので、ある程度の大きさのときだけ
            if (edge && size.x >= 90f && size.y >= 60f)
            {
                float ox = size.x * 0.5f - 11f, oy = size.y * 0.5f - 11f;
                for (int k = 0; k < 4; k++)
                {
                    var at = new Vector2((k % 2 == 0 ? -1 : 1) * ox, (k < 2 ? 1 : -1) * oy);
                    Img(root, "Rivet" + k, at, new Vector2(9, 9), Circle(24), Rivet);
                    Img(root, "RivetHi" + k, at + new Vector2(-1.2f, 1.2f), new Vector2(4, 4), Circle(16), RivetHi);
                }
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
            // 下辺のかすかな縁光。板が暖色なので、白ではなく灯りの色に寄せる
            Img(root, "BottomLine", new Vector2(0, -size.y * 0.5f + 0.5f), new Vector2(size.x - radius, 1), null, new Color(1f, 0.62f, 0.32f, 0.10f));
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
        /// <summary>
        /// 小見出し（左寄せ＋下線）。indent はアイコンぶんの字下げ。
        /// 左にアイコンを置くときは必ずこれを使い、文字と重ならない位置から書き始める。
        /// </summary>
        public static Text Heading(Transform parent, string name, Vector2 pos, float width, string text, float indent = 0f)
        {
            var t = UiFactory.Label(parent, name, new Vector2(pos.x + indent * 0.5f, pos.y), new Vector2(width - indent, 14), text, 11, TextAnchor.MiddleLeft, TextSub);
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
