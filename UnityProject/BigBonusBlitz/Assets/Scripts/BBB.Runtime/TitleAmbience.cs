using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// タイトルに漂わせる粒。光の玉（埃のように上へゆらゆら）と、桜の花びら（散る）。
    /// 画像は使わず UiSkin の手続き描画で作る。粒は使い回すので生成し直さない。
    /// </summary>
    public sealed class TitleAmbience : MonoBehaviour
    {
        private sealed class Bit
        {
            public RectTransform rt;
            public Image img;
            public float x, y, vx, vy, spin, angle, size, phase, wob, life, maxLife;
            public bool petal;
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

        private void Build(int orbs, int petals)
        {
            var orb = UiSkin.Glow(64);
            var petal = UiSkin.Petal(64);
            for (int i = 0; i < orbs + petals; i++)
            {
                bool isPetal = i >= orbs;
                var img = UiSkin.Img(_root, isPetal ? "Petal" : "Orb", Vector2.zero,
                                     Vector2.one * 10f, isPetal ? petal : orb, Color.white);
                img.raycastTarget = false;
                var b = new Bit { rt = img.rectTransform, img = img, petal = isPetal };
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
                b.size = R(9f, 20f);
                b.x = R(-w * 0.5f, w * 0.5f);
                b.y = anywhere ? R(-h * 0.5f, h * 0.5f) : h * 0.5f + b.size;
                b.vx = R(-16f, -4f);          // 少し左へ流れる
                b.vy = R(-30f, -14f);
                b.spin = R(-70f, 70f);
                b.angle = R(0f, 360f);
                // 桜。白に近い桃色から、少し濃い桃色まで
                b.img.color = Color.Lerp(UiSkin.Hex("#ffd9e6"), UiSkin.Hex("#ff9dc0"), R(0f, 1f));
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
                if (b.petal) b.rt.localRotation = Quaternion.Euler(0, 0, b.angle);

                bool gone = b.life >= b.maxLife
                         || b.y > _h * 0.5f + 40f || b.y < -_h * 0.5f - 40f
                         || b.x < -_w * 0.5f - 60f || b.x > _w * 0.5f + 60f;
                if (gone) Reset(b, false);
            }
        }
    }
}
