using System.Collections;
using BBB.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// ゾーン（告知）の演出。エンゲージ・BIG・REG の見出しを、絵と位置と効果で出す。
    /// 値は game_config.json の zoneFx（tools/zone_viewer.html で決めて写す）。絵は Resources/Art/UI/Zones/&lt;image&gt;。
    /// image が空、または絵が無いときは呼び側の従来の演出（文字）に落とす。
    /// 段取りは ビューアと同じ: 出る（inStyle / inSec）→ 保持（holdSec、idle と揺れ）→ 消える（outSec）。
    /// </summary>
    public static class ZoneFx
    {
        /// <summary>この面を絵で出せるか（image があり、Resources に絵がある）。</summary>
        public static bool Has(ZoneFxEntry z) => z != null && !string.IsNullOrEmpty(z.image) && Art(z.image) != null;

        private static Sprite Art(string name) => ArtLoader.Sprite("Art/UI/Zones/" + name);

        private static Color Col(string hex, Color fallback, float alpha = 1f)
        {
            var c = !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var p) ? p : fallback;
            c.a = alpha; return c;
        }

        /// <summary>
        /// 告知を出す。parent は舞台（StageW x StageH）。座標はビューアと同じ「舞台の中心から上が +y」。
        /// 終わると作った物は消える。呼び側は yield return で待てる。
        /// </summary>
        public static IEnumerator Play(RectTransform stage, ZoneFxEntry z, MonoBehaviour host, Image edgeOverlay = null)
        {
            var sprite = Art(z.image);
            if (sprite == null) yield break;

            var root = UiSkin.Rect(stage, "ZoneFx", Vector2.zero, stage.sizeDelta);
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            var pos = new Vector2(z.x, z.y);

            // 後ろから: 帯 → 光 → 絵
            RectTransform band = null;
            if (z.bandOn)
            {
                band = UiSkin.Rect(root, "Band", pos, new Vector2(stage.sizeDelta.x * 1.2f, Mathf.Max(8f, z.bandH)));
                UiSkin.Img(band, "Bg", Vector2.zero, band.sizeDelta, null, Col(z.bandColor, Color.black, Mathf.Clamp01(z.bandAlpha)));
                if (z.lineW > 0.5f)
                {
                    var line = Col(z.lineColor, new Color(1f, .3f, .42f), .9f);
                    UiSkin.Img(band, "LineTop", new Vector2(0, band.sizeDelta.y * .5f - z.lineW * .5f), new Vector2(band.sizeDelta.x, z.lineW), null, line);
                    UiSkin.Img(band, "LineBottom", new Vector2(0, -band.sizeDelta.y * .5f + z.lineW * .5f), new Vector2(band.sizeDelta.x, z.lineW), null, line);
                }
                band.localRotation = Quaternion.Euler(0, 0, z.bandRot);
                band.localScale = new Vector3(0f, 1f, 1f);
            }
            float w = Mathf.Max(40f, z.w), h = w * sprite.rect.height / Mathf.Max(1f, sprite.rect.width);
            var glow = UiSkin.Img(root, "Glow", pos, new Vector2(w * z.glowSize, w * z.glowSize * .55f), UiSkin.Glow(96), Col(z.glowColor, Color.white, 0f));
            var img = UiSkin.Img(root, "Art", pos, new Vector2(w, h), sprite, Color.white);
            img.preserveAspect = true;
            var art = img.rectTransform;
            art.localRotation = Quaternion.Euler(0, 0, z.rot);

            if (z.burstN > 0 && z.burst != "none") Burst(root, pos, z);
            if (z.shake > 0.1f && z.shakeSec > 0.01f) host.StartCoroutine(Effects.Shake(stage, z.shakeSec, z.shake));
            var edgeColor = Col(z.edgeColor, new Color(1f, .3f, .42f), 1f);

            float inSec = Mathf.Max(0.02f, z.inSec), hold = Mathf.Max(0f, z.holdSec), outSec = Mathf.Max(0.02f, z.outSec);
            float alpha = Mathf.Clamp01(z.alpha <= 0 ? 1f : z.alpha);
            float t = 0;
            // 1) 出る
            while (t < inSec)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / inSec), e = 1f - Mathf.Pow(1f - u, 3f);
                float s = 1f; Vector2 d = Vector2.zero; float a = alpha;
                switch ((z.inStyle ?? "slam").ToLowerInvariant())
                {
                    case "zoom": s = u * (1f + 0.3f * Mathf.Sin(u * Mathf.PI)); a = alpha * u; break;
                    case "slide": d.x = (1f - e) * -stage.sizeDelta.x * 0.6f; break;
                    case "drop": d.y = (1f - e) * 260f; break;
                    case "fade": a = alpha * u; break;
                    default: s = 1.5f - 0.5f * (1f - Mathf.Pow(1f - u, 2f)); break;   // slam
                }
                art.localScale = Vector3.one * s;
                art.anchoredPosition = pos + d;
                cg.alpha = a;
                if (band != null) band.localScale = new Vector3(e, 1f, 1f);
                glow.color = Col(z.glowColor, Color.white, z.glowA * u);
                if (edgeOverlay != null) edgeOverlay.color = new Color(edgeColor.r, edgeColor.g, edgeColor.b, z.edgeA * u);
                yield return null;
            }
            art.localScale = Vector3.one; art.anchoredPosition = pos; cg.alpha = alpha;
            if (band != null) band.localScale = Vector3.one;
            // 2) 保持
            float ft = 0;
            while (ft < hold)
            {
                ft += Time.deltaTime;
                switch ((z.idle ?? "none").ToLowerInvariant())
                {
                    case "breathe": art.localScale = Vector3.one * (1f + 0.03f * Mathf.Sin(ft * Mathf.PI)); break;
                    case "pulse": cg.alpha = alpha * (0.82f + 0.18f * Mathf.Sin(ft * Mathf.PI * 2f)); break;
                    case "sway": art.localRotation = Quaternion.Euler(0, 0, z.rot + 1.5f * Mathf.Sin(ft * 2f)); break;
                }
                glow.color = Col(z.glowColor, Color.white, z.glowA * (0.35f + 0.65f * Mathf.Exp(-ft * 1.6f)));
                if (edgeOverlay != null) edgeOverlay.color = new Color(edgeColor.r, edgeColor.g, edgeColor.b, z.edgeA * Mathf.Max(0f, 1f - ft / Mathf.Max(0.15f, z.shakeSec + 0.2f)));
                yield return null;
            }
            // 3) 消える
            t = 0;
            while (t < outSec)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / outSec);
                cg.alpha = alpha * (1f - u);
                glow.color = Col(z.glowColor, Color.white, z.glowA * 0.1f * (1f - u));
                if (edgeOverlay != null) edgeOverlay.color = new Color(edgeColor.r, edgeColor.g, edgeColor.b, z.edgeA * 0.1f * (1f - u));
                yield return null;
            }
            if (edgeOverlay != null) edgeOverlay.color = new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0f);
            Object.Destroy(root.gameObject);
        }

        /// <summary>飛び散り。UiFx の既製の型に寄せる（種類はビューアと同じ 4 つ）。</summary>
        private static void Burst(RectTransform at, Vector2 pos, ZoneFxEntry z)
        {
            UiFx.Preset pr;
            switch ((z.burst ?? "none").ToLowerInvariant())
            {
                case "confetti": pr = UiFx.Preset.Confetti; break;
                case "stars": pr = UiFx.Preset.RainbowStars; break;
                case "sparks": pr = UiFx.Preset.Sparks; break;
                default: pr = UiFx.Preset.RedShards; break;
            }
            UiFx.Burst(at, pr, pos);
        }
    }
}
