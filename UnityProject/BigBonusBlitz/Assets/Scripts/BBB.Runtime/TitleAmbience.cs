using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// タイトルに漂わせる粒。光の玉（埃のように上へゆらゆら）と、桜の花びら（散る）。
    /// 花びらは assets/title/BG/flower の絵（Resources/Art/UI/Title/petal_N）。後ろに桃色の光を敷いて脈打たせる。
    /// 絵が無ければ UiSkin の手続き描画に戻る。粒は使い回すので生成し直さない。
    /// </summary>
    public sealed class TitleAmbience : MonoBehaviour
    {
        private sealed class Bit
        {
            public RectTransform rt;
            public Image img;
            public float x, y, vx, vy, spin, angle, size, phase, wob, life, maxLife;
            public bool petal;
            /// <summary>花びらの後ろの光（絵の花びらのときだけ）。</summary>
            public Image glow;
        }

        private readonly List<Bit> _bits = new List<Bit>();
        private RectTransform _root;
        private float _w, _h;
        private System.Random _rng;

        /// <param name="orbs">光の玉の数</param>
        /// <param name="petals">花びらの数</param>
        public static TitleAmbience Create(Transform parent, int orbs = 26, int petals = 22, int seed = 0)
        {
            var rt = UiSkin.Rect(parent, "Ambience", Vector2.zero, Vector2.zero);
            UiSkin.Stretch(rt);
            var a = rt.gameObject.AddComponent<TitleAmbience>();
            a._root = rt;
            a._rng = new System.Random(seed == 0 ? 20260911 : seed);
            a.Build(orbs, petals);
            return a;
        }

        private float R(float a, float b) => a + (float)_rng.NextDouble() * (b - a);

        private readonly List<Sprite> _petalArts = new List<Sprite>();

        private void Build(int orbs, int petals)
        {
            var orb = UiSkin.Glow(64);
            var petal = UiSkin.Petal(64);
            // 花びらの絵。petal_1 から続く番号を全部拾う（無ければ手続き描画）
            for (int i = 1; i <= 16; i++)
            {
                if (Resources.Load<Texture2D>("Art/UI/Title/petal_" + i) == null) break;
                var sp = ArtLoader.Sprite("Art/UI/Title/petal_" + i);
                if (sp != null) _petalArts.Add(sp);
            }
            for (int i = 0; i < orbs + petals; i++)
            {
                bool isPetal = i >= orbs;
                Image glow = null;
                if (isPetal && _petalArts.Count > 0)
                {
                    // 光は花びらの後ろ（先に作る）。桃色で、花びらより二回り大きい
                    glow = UiSkin.Img(_root, "PetalGlow", Vector2.zero, Vector2.one * 24f, orb, UiSkin.Hex("#ffb3d9"));
                    glow.raycastTarget = false;
                }
                var art = isPetal && _petalArts.Count > 0 ? _petalArts[i % _petalArts.Count] : (isPetal ? petal : orb);
                var img = UiSkin.Img(_root, isPetal ? "Petal" : "Orb", Vector2.zero, Vector2.one * 10f, art, Color.white);
                img.raycastTarget = false;
                if (glow != null) img.preserveAspect = true;
                var b = new Bit { rt = img.rectTransform, img = img, petal = isPetal, glow = glow };
                _bits.Add(b);
                Reset(b, true);
            }
        }

        private void Reset(Bit b, bool anywhere)
        {
            // 画面の大きさは Update で毎回見ている。最初の 1 回だけ手当てする
            float w = _w > 1f ? _w : 1200f, h = _h > 1f ? _h : 560f;
            b.phase = R(0f, 100f);
            b.wob = R(18f, 46f);
            b.maxLife = R(6f, 13f);
            b.life = anywhere ? R(0f, b.maxLife) : 0f;
            if (b.petal)
            {
                b.size = b.glow != null ? R(14f, 32f) : R(9f, 20f);   // 絵の花びらは描き込みがあるので大きめ
                b.x = R(-w * 0.5f, w * 0.5f);
                b.y = anywhere ? R(-h * 0.5f, h * 0.5f) : h * 0.5f + b.size;
                b.vx = R(-16f, -4f);          // 少し左へ流れる
                b.vy = R(-30f, -14f);
                b.spin = R(-70f, 70f);
                b.angle = R(0f, 360f);
                // 桜。白に近い桃色から、少し濃い桃色まで（絵の花びらは元の色のまま）
                b.img.color = b.glow != null ? Color.white : Color.Lerp(UiSkin.Hex("#ffd9e6"), UiSkin.Hex("#ff9dc0"), R(0f, 1f));
                if (b.glow != null) b.glow.rectTransform.sizeDelta = Vector2.one * (b.size * 2.4f);
            }
            else
            {
                b.size = R(5f, 16f);
                b.x = R(-w * 0.5f, w * 0.5f);
                b.y = anywhere ? R(-h * 0.5f, h * 0.5f) : -h * 0.5f - b.size;
                b.vx = R(-6f, 6f);
                b.vy = R(7f, 20f);            // 埃のように上へ
                b.spin = 0f;
                b.angle = 0f;
                b.img.color = Color.Lerp(UiSkin.Hex("#fff3c8"), UiSkin.Hex("#bfe4ff"), R(0f, 1f));
            }
            b.rt.sizeDelta = Vector2.one * b.size;
        }

        private void Update()
        {
            var r = _root.rect;
            _w = r.width; _h = r.height;
            float dt = Time.deltaTime;
            float t = Time.time;
            foreach (var b in _bits)
            {
                b.life += dt;
                b.x += (b.vx + Mathf.Sin((t + b.phase) * 0.7f) * b.wob * 0.35f) * dt;
                b.y += b.vy * dt;
                b.angle += b.spin * dt;

                // 出入りは端で切らずに、透け具合で見せ消しする
                float k = Mathf.Clamp01(b.life / b.maxLife);
                float a = Mathf.Sin(k * Mathf.PI);                  // 0 → 1 → 0
                a *= b.petal ? 0.95f : 0.55f;
                var c = b.img.color; c.a = a; b.img.color = c;

                b.rt.anchoredPosition = new Vector2(b.x, b.y);
                if (b.petal)
                {
                    // 舞う: 回りながら、風に煽られるように傾きが揺れる
                    float flutter = b.glow != null ? 22f * Mathf.Sin((t + b.phase) * 1.6f) : 0f;
                    b.rt.localRotation = Quaternion.Euler(0, 0, b.angle + flutter);
                }
                if (b.glow != null)
                {
                    // 後ろの光: 花びらと同じ所で、ゆっくり脈打つ。裏返る（傾きが大きい）ときほど強く光る
                    float pulse = 0.55f + 0.45f * Mathf.Sin((t + b.phase) * 2.2f);
                    var g = b.glow.color; g.a = a * 0.6f * pulse; b.glow.color = g;
                    b.glow.rectTransform.anchoredPosition = new Vector2(b.x, b.y);
                    b.glow.rectTransform.localScale = Vector3.one * (0.9f + 0.25f * pulse);
                }

                bool gone = b.life >= b.maxLife
                         || b.y > _h * 0.5f + 40f || b.y < -_h * 0.5f - 40f
                         || b.x < -_w * 0.5f - 60f || b.x > _w * 0.5f + 60f;
                if (gone) Reset(b, false);
            }
        }
    }
}
