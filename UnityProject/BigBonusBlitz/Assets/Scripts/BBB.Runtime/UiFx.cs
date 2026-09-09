using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// 外部素材なしの UI エフェクト集。テクスチャは起動時に生成（円・星・破片・リング）。
    /// Canvas 上の Image をプールして飛ばすので、URP/2D でも確実に描ける。
    /// 使い方: UiFx.Burst(parent, pos, UiFx.Preset.Coins) など。
    /// </summary>
    public sealed class UiFx : MonoBehaviour
    {
        public enum Shape { Circle, Star, Shard, Ring, Spark }

        public sealed class Preset
        {
            public Shape shape = Shape.Circle;
            public int count = 16;
            public float sizeMin = 8, sizeMax = 16;
            public float speedMin = 200, speedMax = 500;
            public float gravity = -900;
            public float life = 0.7f;
            public float spread = 360;      // 放出角の幅（度）
            public float direction = 90;    // 放出の中心方向（度、90=上）
            public float drag = 1.5f;
            public float spin = 360;        // 回転速度（度/秒）の最大
            public Color[] colors = { Color.white };
            public bool additive = false;   // 明るい色の加算風（アルファ抑えめで重ねる）

            public static Preset Coins => new Preset { shape = Shape.Circle, count = 18, sizeMin = 10, sizeMax = 16, speedMin = 250, speedMax = 520, gravity = -1100, life = 0.9f, spread = 100, direction = 90, colors = new[] { new Color(1f, 0.85f, 0.3f), new Color(1f, 0.95f, 0.6f), new Color(0.95f, 0.7f, 0.2f) } };
            public static Preset Sparks => new Preset { shape = Shape.Spark, count = 14, sizeMin = 6, sizeMax = 14, speedMin = 300, speedMax = 700, gravity = -300, life = 0.35f, spread = 140, direction = 60, colors = new[] { new Color(1f, 0.9f, 0.5f), Color.white, new Color(1f, 0.6f, 0.2f) } };
            public static Preset SuccessStars => new Preset { shape = Shape.Star, count = 24, sizeMin = 12, sizeMax = 26, speedMin = 150, speedMax = 420, gravity = -250, life = 1.1f, spread = 360, direction = 90, spin = 540, colors = new[] { new Color(1f, 0.85f, 0.3f), Color.white, new Color(1f, 0.6f, 0.9f) } };
            public static Preset RedShards => new Preset { shape = Shape.Shard, count = 16, sizeMin = 8, sizeMax = 18, speedMin = 200, speedMax = 480, gravity = -900, life = 0.6f, spread = 120, direction = 120, colors = new[] { new Color(1f, 0.3f, 0.3f), new Color(0.8f, 0.1f, 0.15f) } };
            public static Preset Explode => new Preset { shape = Shape.Shard, count = 28, sizeMin = 8, sizeMax = 22, speedMin = 250, speedMax = 650, gravity = -700, life = 0.9f, spread = 360, direction = 90, spin = 720, colors = new[] { new Color(0.7f, 0.9f, 1f), Color.white, new Color(0.4f, 0.6f, 1f) } };
            public static Preset Confetti => new Preset { shape = Shape.Shard, count = 60, sizeMin = 6, sizeMax = 12, speedMin = 200, speedMax = 600, gravity = -350, life = 2.2f, spread = 120, direction = 90, spin = 900, drag = 2.5f, colors = new[] { new Color(1f, 0.3f, 0.4f), new Color(1f, 0.85f, 0.3f), new Color(0.3f, 0.8f, 1f), new Color(0.5f, 1f, 0.5f), Color.white } };
            public static Preset Dust => new Preset { shape = Shape.Circle, count = 10, sizeMin = 14, sizeMax = 28, speedMin = 60, speedMax = 180, gravity = 60, life = 0.6f, spread = 160, direction = 90, colors = new[] { new Color(0.8f, 0.75f, 0.6f, 0.7f) } };
            public static Preset Focus => new Preset { shape = Shape.Circle, count = 12, sizeMin = 4, sizeMax = 8, speedMin = 40, speedMax = 90, gravity = 120, life = 1.4f, spread = 360, direction = 90, drag = 0.5f, colors = new[] { new Color(0.6f, 0.8f, 1f, 0.8f) } };
            /// <summary>チェリー: 桜色の花びらがひらひら落ちる（Rain で上から降らせる）。</summary>
            public static Preset Petals => new Preset { shape = Shape.Circle, count = 1, sizeMin = 7, sizeMax = 12, speedMin = 70, speedMax = 140, gravity = -90, life = 1.9f, spread = 50, direction = 270, drag = 0.6f, spin = 240, colors = new[] { new Color(1f, 0.6f, 0.75f), new Color(1f, 0.8f, 0.88f), new Color(1f, 0.45f, 0.6f) } };
            /// <summary>スイカ: 緑のしぶきが弾ける。</summary>
            public static Preset Splash => new Preset { shape = Shape.Circle, count = 24, sizeMin = 6, sizeMax = 13, speedMin = 260, speedMax = 560, gravity = -1300, life = 0.75f, spread = 120, direction = 90, colors = new[] { new Color(0.45f, 1f, 0.5f), new Color(0.2f, 0.85f, 0.35f), new Color(0.85f, 1f, 0.85f) } };
            /// <summary>リプレイ: 青い小さな星がふわっと舞う。</summary>
            public static Preset Sparkle => new Preset { shape = Shape.Star, count = 14, sizeMin = 8, sizeMax = 15, speedMin = 60, speedMax = 170, gravity = 40, life = 1.0f, spread = 360, direction = 90, drag = 0.8f, spin = 420, colors = new[] { new Color(0.55f, 0.8f, 1f), Color.white, new Color(0.35f, 0.6f, 1f) } };
            /// <summary>チャンス目: 虹色の星＋紙吹雪。</summary>
            public static Preset RainbowStars => new Preset { shape = Shape.Star, count = 32, sizeMin = 12, sizeMax = 28, speedMin = 180, speedMax = 520, gravity = -300, life = 1.3f, spread = 360, direction = 90, spin = 600, colors = new[] { new Color(1f, 0.35f, 0.4f), new Color(1f, 0.85f, 0.3f), new Color(0.4f, 1f, 0.5f), new Color(0.4f, 0.75f, 1f), new Color(0.8f, 0.5f, 1f), Color.white } };
        }

        private sealed class P
        {
            public RectTransform rt; public Image img; public Vector2 vel; public float life, maxLife, spin, size; public Color color;
        }

        private static UiFx _inst;
        private static readonly Dictionary<Shape, Sprite> _sprites = new Dictionary<Shape, Sprite>();
        private readonly List<P> _live = new List<P>();
        private readonly Stack<Image> _pool = new Stack<Image>();
        private RectTransform _layer;

        /// <summary>エフェクト層を作る（最前面に置きたい親を渡す）。</summary>
        public static void Init(Transform parent)
        {
            if (_inst != null) return;
            var go = new GameObject("UiFx", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
            _inst = go.AddComponent<UiFx>();
            _inst._layer = rt;
        }

        // ----------------------------------------------------------- API
        /// <summary>worldPos は任意の RectTransform の位置（同じ Canvas 内）。</summary>
        public static void Burst(RectTransform at, Preset preset, Vector2 offset = default)
        {
            if (_inst == null) return;
            var pos = _inst.ToLayer(at) + offset;
            _inst.Spawn(pos, preset);
        }

        /// <summary>衝撃波リング: 広がって消える。</summary>
        public static void Ring(RectTransform at, Color color, float fromSize = 40, float toSize = 260, float duration = 0.45f)
        {
            if (_inst == null) return;
            _inst.StartCoroutine(_inst.RingRoutine(_inst.ToLayer(at), color, fromSize, toSize, duration));
        }

        /// <summary>斬撃線: 一瞬走る白い線。angle は度。</summary>
        public static void Slash(RectTransform at, float angle = -30f, float length = 220f, Color? color = null)
        {
            if (_inst == null) return;
            _inst.StartCoroutine(_inst.SlashRoutine(_inst.ToLayer(at), angle, length, color ?? Color.white));
        }

        /// <summary>数字ポップ: 「+15」などが跳ねて消える。</summary>
        public static void PopText(RectTransform at, string text, Color color, int fontSize = 22, Vector2 offset = default)
        {
            if (_inst == null) return;
            _inst.StartCoroutine(_inst.PopTextRoutine(_inst.ToLayer(at) + offset, text, color, fontSize));
        }

        /// <summary>幅 width の範囲の上端（offsetY）から duration 秒間、毎秒 perSecond 個を降らせる（花びら等）。</summary>
        public static void Rain(RectTransform at, Preset preset, float width, float offsetY, float duration, float perSecond)
        {
            if (_inst == null) return;
            _inst.StartCoroutine(_inst.RainRoutine(_inst.ToLayer(at), preset, width, offsetY, duration, perSecond));
        }

        /// <summary>
        /// 役名カットイン: 角丸の帯にアイコン＋文字。ポップイン → 保持 → 右へ流れて消える。
        /// scale で大きさ、rainbow=true で縁と文字の色相が回る（チャンス目）。
        /// </summary>
        public static void Cutin(RectTransform at, string text, Color color, Sprite icon, float scale = 1f, float hold = 0.8f, bool rainbow = false, Vector2 offset = default)
        {
            if (_inst == null) return;
            _inst.StartCoroutine(_inst.CutinRoutine(_inst.ToLayer(at) + offset, text, color, icon, scale, hold, rainbow));
        }

        // ------------------------------------------------------- internals
        private Vector2 ToLayer(RectTransform at)
        {
            var world = at.TransformPoint(Vector3.zero);
            var local = _layer.InverseTransformPoint(world);
            return local;
        }

        private Image Get(Shape shape)
        {
            Image img;
            if (_pool.Count > 0) { img = _pool.Pop(); img.gameObject.SetActive(true); }
            else
            {
                var go = new GameObject("p", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_layer, false);
                img = go.GetComponent<Image>();
                img.raycastTarget = false;
            }
            img.sprite = GetSprite(shape);
            return img;
        }

        private void Spawn(Vector2 pos, Preset pr)
        {
            for (int i = 0; i < pr.count; i++) SpawnOne(pos, pr);
        }

        private void SpawnOne(Vector2 pos, Preset pr)
        {
            {
                var img = Get(pr.shape);
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos;
                float size = Random.Range(pr.sizeMin, pr.sizeMax);
                rt.sizeDelta = pr.shape == Shape.Spark ? new Vector2(size * 3f, size * 0.5f) : new Vector2(size, size);
                float ang = (pr.direction + Random.Range(-pr.spread * 0.5f, pr.spread * 0.5f)) * Mathf.Deg2Rad;
                float spd = Random.Range(pr.speedMin, pr.speedMax);
                var vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
                rt.localRotation = Quaternion.Euler(0, 0, pr.shape == Shape.Spark ? ang * Mathf.Rad2Deg : Random.Range(0, 360f));
                var c = pr.colors[Random.Range(0, pr.colors.Length)];
                img.color = c;
                _live.Add(new P { rt = rt, img = img, vel = vel, life = pr.life * Random.Range(0.7f, 1.1f), maxLife = pr.life, spin = Random.Range(-pr.spin, pr.spin), size = size, color = c });
                _liveDrag[img] = pr.drag; _liveGravity[img] = pr.gravity; _liveSpark[img] = pr.shape == Shape.Spark;
            }
        }

        private readonly Dictionary<Image, float> _liveDrag = new Dictionary<Image, float>();
        private readonly Dictionary<Image, float> _liveGravity = new Dictionary<Image, float>();
        private readonly Dictionary<Image, bool> _liveSpark = new Dictionary<Image, bool>();

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var p = _live[i];
                p.life -= dt;
                if (p.life <= 0f)
                {
                    p.img.gameObject.SetActive(false);
                    _pool.Push(p.img);
                    _live.RemoveAt(i);
                    continue;
                }
                float drag = _liveDrag[p.img], g = _liveGravity[p.img];
                p.vel += new Vector2(0, g) * dt;
                p.vel *= Mathf.Max(0f, 1f - drag * dt);
                p.rt.anchoredPosition += p.vel * dt;
                float k = p.life / p.maxLife;
                if (_liveSpark[p.img]) p.rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(p.vel.y, p.vel.x) * Mathf.Rad2Deg);
                else p.rt.localRotation *= Quaternion.Euler(0, 0, p.spin * dt);
                p.img.color = new Color(p.color.r, p.color.g, p.color.b, p.color.a * Mathf.Clamp01(k * 1.5f));
                float s = p.size * (0.6f + 0.4f * k);
                p.rt.sizeDelta = _liveSpark[p.img] ? new Vector2(s * 3f, s * 0.5f) : new Vector2(s, s);
            }
        }

        private IEnumerator RingRoutine(Vector2 pos, Color color, float from, float to, float duration)
        {
            var img = Get(Shape.Ring);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.localRotation = Quaternion.identity;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 2f);
                float s = Mathf.Lerp(from, to, u);
                rt.sizeDelta = new Vector2(s, s);
                img.color = new Color(color.r, color.g, color.b, color.a * (1f - u));
                yield return null;
            }
            img.gameObject.SetActive(false);
            _pool.Push(img);
        }

        private IEnumerator SlashRoutine(Vector2 pos, float angle, float length, Color color)
        {
            var img = Get(Shape.Spark);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.localRotation = Quaternion.Euler(0, 0, angle);
            float t = 0, d = 0.18f;
            while (t < d)
            {
                t += Time.deltaTime;
                float u = t / d;
                float grow = u < 0.4f ? u / 0.4f : 1f;
                float fade = u < 0.4f ? 1f : 1f - (u - 0.4f) / 0.6f;
                rt.sizeDelta = new Vector2(length * grow, 10f * (1f - 0.5f * u));
                img.color = new Color(color.r, color.g, color.b, fade);
                yield return null;
            }
            img.gameObject.SetActive(false);
            _pool.Push(img);
        }

        private IEnumerator RainRoutine(Vector2 origin, Preset pr, float width, float offsetY, float duration, float perSecond)
        {
            float t = 0, acc = 0;
            while (t < duration)
            {
                float dt = Time.deltaTime;
                t += dt;
                acc += perSecond * dt;
                while (acc >= 1f)
                {
                    acc -= 1f;
                    SpawnOne(origin + new Vector2(Random.Range(-width * 0.5f, width * 0.5f), offsetY), pr);
                }
                yield return null;
            }
        }

        private IEnumerator CutinRoutine(Vector2 pos, string text, Color color, Sprite icon, float scale, float hold, bool rainbow)
        {
            float w = 320f * scale, h = 66f * scale;
            var root = UiSkin.Rect(_layer, "cutin", pos, new Vector2(w, h));
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            int r = Mathf.RoundToInt(h * 0.5f);
            UiSkin.Img(root, "Shadow", new Vector2(0, -5), new Vector2(w + 24, h + 24), UiSkin.Shadow(r, 12), new Color(0, 0, 0, 0.55f));
            var glow = UiSkin.Img(root, "Glow", Vector2.zero, new Vector2(w + 60, h + 60), UiSkin.Shadow(r, 30), new Color(color.r, color.g, color.b, 0.35f));
            var edge = UiSkin.Img(root, "Edge", Vector2.zero, new Vector2(w, h), UiSkin.Rounded(r), color);
            UiSkin.Img(root, "Body", Vector2.zero, new Vector2(w - 6, h - 6), UiSkin.Rounded(r - 3), new Color(color.r * 0.18f, color.g * 0.18f, color.b * 0.18f, 0.96f));
            UiSkin.Img(root, "Sheen", new Vector2(0, h * 0.22f), new Vector2(w - 10, h * 0.42f), UiSkin.GradientV(true), new Color(1, 1, 1, 0.12f)).type = Image.Type.Simple;
            float textX = 0;
            if (icon != null)
            {
                var ic = UiSkin.Img(root, "Icon", new Vector2(-w * 0.5f + h * 0.5f + 8, 0), new Vector2(h - 18, h - 18), icon, Color.white);
                ic.preserveAspect = true;
                textX = h * 0.32f;
            }
            var label = UiFactory.Label(root, "Text", new Vector2(textX, 1), new Vector2(w - (icon != null ? h : 0) - 20, h), text, Mathf.RoundToInt(28 * scale), TextAnchor.MiddleCenter, Color.white);
            label.fontStyle = FontStyle.Bold;
            var sh = label.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.8f); sh.effectDistance = new Vector2(1, -2);

            float t = 0, pop = 0.22f, outT = 0.28f;
            float total = pop + hold + outT;
            while (t < total)
            {
                t += Time.deltaTime;
                if (t < pop)
                {
                    float u = t / pop;
                    float s = u < 0.7f ? Mathf.Lerp(0.3f, 1.12f, u / 0.7f) : Mathf.Lerp(1.12f, 1f, (u - 0.7f) / 0.3f);
                    root.localScale = Vector3.one * s;
                    cg.alpha = Mathf.Clamp01(u * 2f);
                }
                else if (t < pop + hold)
                {
                    root.localScale = Vector3.one * (1f + 0.02f * Mathf.Sin((t - pop) * 12f));
                    cg.alpha = 1f;
                }
                else
                {
                    float u = (t - pop - hold) / outT;
                    root.anchoredPosition = pos + new Vector2(160f * u * u, 0);
                    cg.alpha = 1f - u;
                }
                if (rainbow)
                {
                    var c = Color.HSVToRGB((t * 1.2f) % 1f, 0.75f, 1f);
                    edge.color = c; glow.color = new Color(c.r, c.g, c.b, 0.4f); label.color = Color.Lerp(Color.white, c, 0.6f);
                }
                yield return null;
            }
            Destroy(root.gameObject);
        }

        private IEnumerator PopTextRoutine(Vector2 pos, string text, Color color, int fontSize)
        {
            var t = UiFactory.Label(_layer, "pop", pos, new Vector2(300, 40), text, fontSize, TextAnchor.MiddleCenter, color);
            t.fontStyle = FontStyle.Bold;
            var rt = t.rectTransform;
            float e = 0, d = 0.9f;
            while (e < d)
            {
                e += Time.deltaTime;
                float u = e / d;
                float y = 60f * (1f - Mathf.Pow(1f - Mathf.Min(1f, u * 1.6f), 2f));
                rt.anchoredPosition = pos + new Vector2(0, y);
                rt.localScale = Vector3.one * (u < 0.15f ? Mathf.Lerp(0.4f, 1.15f, u / 0.15f) : u < 0.3f ? Mathf.Lerp(1.15f, 1f, (u - 0.15f) / 0.15f) : 1f);
                t.color = new Color(color.r, color.g, color.b, u < 0.6f ? 1f : 1f - (u - 0.6f) / 0.4f);
                yield return null;
            }
            Destroy(t.gameObject);
        }

        // ---------------------------------------------------- textures
        private static Sprite GetSprite(Shape shape)
        {
            if (_sprites.TryGetValue(shape, out var s)) return s;
            const int N = 32;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float a;
                    switch (shape)
                    {
                        case Shape.Circle: a = Mathf.Clamp01((1f - r) * 6f); break;
                        case Shape.Ring: a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) * 9f); break;
                        case Shape.Spark: a = Mathf.Clamp01((1f - Mathf.Abs(v)) * 3f) * Mathf.Clamp01((1f - Mathf.Abs(u)) * 1.5f); break;
                        case Shape.Shard: a = (Mathf.Abs(u) + Mathf.Abs(v) * 0.6f) < 0.9f ? 1f : 0f; break;
                        default: // Star
                            {
                                float ang = Mathf.Atan2(v, u);
                                float star = 0.55f + 0.45f * Mathf.Cos(ang * 5f);
                                a = r < star ? 1f : Mathf.Clamp01((star + 0.08f - r) * 12f);
                                break;
                            }
                    }
                    px[y * N + x] = new Color(1, 1, 1, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            _sprites[shape] = s;
            return s;
        }
    }
}
