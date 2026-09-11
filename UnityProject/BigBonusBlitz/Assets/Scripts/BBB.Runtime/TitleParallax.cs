using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// タイトルの背景を奥行きのある層で組む。
    /// 奥から: 空と山脈と雲海（ほぼ動かない）→ 浮かぶ城と湖（ゆっくり漂う）→ 手前のバルコニー（一番大きく動く）。
    /// 立ち絵はこの上、光の玉と桜はさらに上に乗る。
    /// 画像は assets/title/BG から tools/ui/cut_sheets.py --no-cut が Resources/Art/UI/Title へ入れる。
    /// 1 枚でも無ければ null を返し、呼び側は従来の 1 枚絵に戻す。
    /// </summary>
    public sealed class TitleParallax : MonoBehaviour
    {
        private sealed class Layer
        {
            public RectTransform rt;
            public Vector2 basePos;     // 親の大きさに対する比（-0.5〜0.5）
            public float baseScale;
            public Vector2 drift;       // 揺れ幅（親の幅に対する比）
            public float period;        // 揺れの周期（秒）
            public float breathe;       // 大きさの脈（比）
            public float phase;
        }

        private readonly System.Collections.Generic.List<Layer> _layers = new System.Collections.Generic.List<Layer>();
        private RectTransform _rt;

        /// <summary>parent（元絵と同じ縦横比の板）の中に層を積む。素材が無ければ null。</summary>
        public static TitleParallax Create(RectTransform parent)
        {
            var sky = ArtLoader.Sprite("Art/UI/Title/sky_mountains_cloudsea");
            var castle = ArtLoader.Sprite("Art/UI/Title/castle_lake");
            var terrace = ArtLoader.Sprite("Art/UI/Title/terrace_balcony");
            if (sky == null || castle == null || terrace == null) return null;

            var go = new GameObject("TitleParallax", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            UiSkin.Stretch(rt);
            var p = go.AddComponent<TitleParallax>();
            p._rt = rt;

            // 一番奥は空の色の板。層の絵は角が透けているので、そこから黒が見えないようにする
            var backing = UiSkin.Img(rt, "Backing", Vector2.zero, Vector2.zero, null, new Color(0.80f, 0.88f, 0.97f));
            UiSkin.Stretch(backing.rectTransform);
            backing.raycastTarget = false;

            // 奥ほど小さく遅く、手前ほど大きく速く。位置は板の中心を原点にした比で持つ。
            // 空は板より 12% 大きくして、雲海の下端が透ける所を板の外へ出す
            p.Add("Sky", sky, new Vector2(0f, 0f), 1.12f, new Vector2(0.004f, 0.002f), 40f, 0.006f, 0f);
            p.Add("Castle", castle, new Vector2(0.04f, -0.06f), 0.92f, new Vector2(0.010f, 0.005f), 22f, 0.012f, 1.7f);
            p.Add("Terrace", terrace, new Vector2(0.03f, -0.16f), 1.10f, new Vector2(0.018f, 0.006f), 16f, 0.010f, 3.1f);
            return p;
        }

        private void Add(string name, Sprite sprite, Vector2 basePos, float scale, Vector2 drift, float period, float breathe, float phase)
        {
            // 板の幅いっぱいに置き、高さは絵の比から出す（板より縦に長いぶんは親のマスクで切れる）
            var img = UiSkin.Img(_rt, name, Vector2.zero, Vector2.zero, sprite, Color.white);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false;
            var lrt = img.rectTransform;
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            _layers.Add(new Layer { rt = lrt, basePos = basePos, baseScale = scale, drift = drift, period = period, breathe = breathe, phase = phase });
        }

        private void LateUpdate()
        {
            var size = _rt.rect.size;
            if (size.x <= 0f) return;
            float t = Time.time;
            foreach (var l in _layers)
            {
                var sp = l.rt.GetComponent<Image>().sprite;
                float aspect = sp.rect.height > 0 ? sp.rect.width / sp.rect.height : 1f;
                float w = size.x * l.baseScale;
                l.rt.sizeDelta = new Vector2(w, w / aspect);
                float k = t * Mathf.PI * 2f / l.period + l.phase;
                l.rt.anchoredPosition = new Vector2(
                    (l.basePos.x + l.drift.x * Mathf.Sin(k)) * size.x,
                    (l.basePos.y + l.drift.y * Mathf.Sin(k * 1.3f + 0.8f)) * size.y);
                l.rt.localScale = Vector3.one * (1f + l.breathe * Mathf.Sin(k * 0.5f));
            }
        }
    }
}
