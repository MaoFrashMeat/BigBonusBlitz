using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// タイトルに漂わせる粒。光の玉（埃のように上へゆらゆら）と、桜の花びら（散る）。
    /// 花びらは assets/title/BG/flower の絵（Resources/Art/UI/Title/petal_N と、ぼかした petal_N_b1..3）。
    /// 後ろに桃色の光を敷いて脈打たせる。絵が無ければ UiSkin の手続き描画に戻る。
    ///
    /// 数・大きさ・透け具合・ぼかし・光・速さ・立ち絵の上で薄くする範囲は
    /// Resources/Data/title_layers.json の "petals"（tools/title_viewer.html で決める）。無ければ既定値。
    /// 粒は使い回すので生成し直さない。
    /// </summary>
    public sealed class TitleAmbience : MonoBehaviour
    {
        [System.Serializable]
        public sealed class PetalConfig
        {
            public int count = 22;
            public int orbs = 26;
            public float sizeMin = 14f, sizeMax = 32f;
            /// <summary>花びらの不透明さ（0〜1）。</summary>
            public float alpha = 0.9f;
            /// <summary>ぼかし 0〜3（petal_N_bK を使う）。</summary>
            public int blur = 0;
            /// <summary>後ろの光の強さ（0 で無し）と、花びらに対する大きさ。</summary>
            public float glow = 0.6f, glowSize = 2.4f;
            /// <summary>落ちる速さ（px/秒）と横流れ、回転、風の煽り。</summary>
            public float fallMin = 14f, fallMax = 30f, driftMin = 4f, driftMax = 16f, spin = 70f, flutter = 22f;
            /// <summary>立ち絵の後ろに置くか（true なら立ち絵に隠れる）。</summary>
            public bool behindChar = false;
            /// <summary>立ち絵の枠の中でどれだけ薄くするか（0 = しない、1 = 消す）。</summary>
            public float charMask = 0f;
            /// <summary>立ち絵の枠（板の中心を原点にした比）。</summary>
            public float charX = 0.28f, charY = -0.05f, charW = 0.42f, charH = 0.95f;
            public float orbAlpha = 0.55f;
        }

        [System.Serializable]
        private sealed class File { public PetalConfig petals; }

        private sealed class Bit
        {
            public RectTransform rt;
            public Image img;
            public float x, y, vx, vy, spin, angle, size, phase, wob, life, maxLife;
            public bool petal;
            /// <summary>花びらの後ろの光（絵の花びらのときだけ）。</summary>
            public Image glow;
        }

        /// <summary>花びらの絵は画布の半分に描いてある（周りはぼかしの余白）。見える大きさを size にするための倍率。</summary>
        private const float PetalCanvasScale = 2f;   // = 1 / cut_sheets.py の PETAL_FILL

        private readonly List<Bit> _bits = new List<Bit>();
        private readonly List<Sprite> _petalArts = new List<Sprite>();
        private RectTransform _root, _board;
        private PetalConfig _cfg = new PetalConfig();
        private float _w, _h;
        private System.Random _rng;

        /// <summary>設定を読む（無ければ既定）。TitleScreen が立ち絵の前後どちらに置くか決めるのにも使う。</summary>
        public static PetalConfig LoadConfig()
        {
            var ta = Resources.Load<TextAsset>(TitleParallax.Path);
            if (ta == null) return new PetalConfig();
            try { var f = JsonConvert.DeserializeObject<File>(ta.text); if (f?.petals != null) return f.petals; }
            catch (System.Exception e) { Debug.LogWarning("title_layers.json の petals が読めない（既定を使う）: " + e.Message); }
            return new PetalConfig();
        }

        /// <param name="parent">粒を置く親</param>
        /// <param name="board">元絵と同じ比の板（立ち絵の枠の座標の基準）。null なら親を使う</param>
        /// <param name="cfg">null なら title_layers.json から読む</param>
        /// <param name="orbs">光の玉の数（-1 で設定値）</param>
        /// <param name="petals">花びらの数（-1 で設定値）</param>
        public static TitleAmbience Create(Transform parent, RectTransform board = null, PetalConfig cfg = null, int orbs = -1, int petals = -1, int seed = 0)
        {
            var rt = UiSkin.Rect(parent, "Ambience", Vector2.zero, Vector2.zero);
            UiSkin.Stretch(rt);
            var a = rt.gameObject.AddComponent<TitleAmbience>();
            a._root = rt;
            a._board = board;
            a._cfg = cfg ?? LoadConfig();
            a._rng = new System.Random(seed == 0 ? 20260911 : seed);
            a.Build(orbs < 0 ? a._cfg.orbs : orbs, petals < 0 ? a._cfg.count : petals);
            return a;
        }

        private float R(float a, float b) => a + (float)_rng.NextDouble() * (b - a);

        private void Build(int orbs, int petals)
        {
            var orb = UiSkin.Glow(64);
            var petal = UiSkin.Petal(64);
            // 花びらの絵。petal_1 から続く番号を全部拾う。ぼかしは _bK 付き（無ければ素のまま）
            string suffix = _cfg.blur > 0 ? "_b" + Mathf.Clamp(_cfg.blur, 1, 3) : "";
            for (int i = 1; i <= 16; i++)
            {
                if (Resources.Load<Texture2D>("Art/UI/Title/petal_" + i) == null) break;
                string path = "Art/UI/Title/petal_" + i + suffix;
                if (Resources.Load<Texture2D>(path) == null) path = "Art/UI/Title/petal_" + i;
                var sp = ArtLoader.Sprite(path);
                if (sp != null) _petalArts.Add(sp);
            }
            for (int i = 0; i < orbs + petals; i++)
            {
                bool isPetal = i >= orbs;
                Image glow = null;
                if (isPetal && _petalArts.Count > 0 && _cfg.glow > 0f)
                {
                    // 光は花びらの後ろ（先に作る）。桃色で、花びらより二回り大きい
                    glow = UiSkin.Img(_root, "PetalGlow", Vector2.zero, Vector2.one * 24f, orb, UiSkin.Hex("#ffb3d9"));
                    glow.raycastTarget = false;
                }
                var art = isPetal && _petalArts.Count > 0 ? _petalArts[i % _petalArts.Count] : (isPetal ? petal : orb);
                var img = UiSkin.Img(_root, isPetal ? "Petal" : "Orb", Vector2.zero, Vector2.one * 10f, art, Color.white);
                img.raycastTarget = false;
                if (isPetal && _petalArts.Count > 0) img.preserveAspect = true;
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
                bool art = _petalArts.Count > 0;
                b.size = art ? R(_cfg.sizeMin, _cfg.sizeMax) : R(9f, 20f);
                b.x = R(-w * 0.5f, w * 0.5f);
                b.y = anywhere ? R(-h * 0.5f, h * 0.5f) : h * 0.5f + b.size;
                b.vx = -R(_cfg.driftMin, _cfg.driftMax);          // 少し左へ流れる
                b.vy = -R(_cfg.fallMin, _cfg.fallMax);
                b.spin = R(-_cfg.spin, _cfg.spin);
                b.angle = R(0f, 360f);
                // 桜。白に近い桃色から、少し濃い桃色まで（絵の花びらは元の色のまま）
                b.img.color = art ? Color.white : Color.Lerp(UiSkin.Hex("#ffd9e6"), UiSkin.Hex("#ff9dc0"), R(0f, 1f));
                if (b.glow != null) b.glow.rectTransform.sizeDelta = Vector2.one * (b.size * _cfg.glowSize);
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
            b.rt.sizeDelta = Vector2.one * (b.petal && _petalArts.Count > 0 ? b.size * PetalCanvasScale : b.size);
        }

        /// <summary>立ち絵の枠の中にいるか（枠の縁で少しなだらかに）。0 = 外、1 = 中。</summary>
        private float InCharBox(float x, float y)
        {
            if (_cfg.charMask <= 0f || _cfg.charW <= 0f || _cfg.charH <= 0f) return 0f;
            // 粒の座標（親の中心原点）→ 板の中心原点の比へ
            Vector2 p = new Vector2(x, y);
            if (_board != null)
            {
                var world = _root.TransformPoint(p);
                var local = (Vector2)_board.InverseTransformPoint(world);
                var s = _board.rect.size;
                if (s.x <= 0f || s.y <= 0f) return 0f;
                p = new Vector2(local.x / s.x, local.y / s.y);
            }
            else p = new Vector2(x / Mathf.Max(1f, _w), y / Mathf.Max(1f, _h));
            float dx = Mathf.Abs(p.x - _cfg.charX) / (_cfg.charW * 0.5f);
            float dy = Mathf.Abs(p.y - _cfg.charY) / (_cfg.charH * 0.5f);
            float d = Mathf.Max(dx, dy);                     // 1 で枠の縁
            return 1f - Mathf.Clamp01((d - 0.85f) / 0.15f);  // 縁の 15% でなだらかに
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
                if (b.petal)
                {
                    a *= _petalArts.Count > 0 ? _cfg.alpha : 0.95f;
                    a *= 1f - _cfg.charMask * InCharBox(b.x, b.y);   // 立ち絵の上では薄く
                }
                else a *= _cfg.orbAlpha;
                var c = b.img.color; c.a = a; b.img.color = c;

                b.rt.anchoredPosition = new Vector2(b.x, b.y);
                if (b.petal)
                {
                    // 舞う: 回りながら、風に煽られるように傾きが揺れる
                    float flutter = _petalArts.Count > 0 ? _cfg.flutter * Mathf.Sin((t + b.phase) * 1.6f) : 0f;
                    b.rt.localRotation = Quaternion.Euler(0, 0, b.angle + flutter);
                }
                if (b.glow != null)
                {
                    // 後ろの光: 花びらと同じ所で、ゆっくり脈打つ
                    float pulse = 0.55f + 0.45f * Mathf.Sin((t + b.phase) * 2.2f);
                    var g = b.glow.color; g.a = a * _cfg.glow * pulse; b.glow.color = g;
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
