using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// タイトルの背景を奥行きのある層で組む。
    /// 層の並び・位置・大きさ・揺れ方は Resources/Data/title_layers.json（tools/title_viewer.html で作る）。
    /// 無ければ既定の 3 層（空と雲海 / 城と湖 / 手前のバルコニー）。
    /// 画像は assets/title/BG から tools/ui/cut_sheets.py --no-cut が Resources/Art/UI/Title へ入れる。
    ///
    /// front = true の層は立ち絵より手前に出す。TitleScreen が立ち絵を置いたあと RaiseFront() を呼ぶ。
    /// 座標は板（元絵と同じ比）の中心を原点にした比（-0.5〜0.5）。揺れ幅は x が板の幅、y が板の高さに対する %。
    /// </summary>
    public sealed class TitleParallax : MonoBehaviour
    {
        public const string Path = "Data/title_layers";

        [System.Serializable]
        public sealed class LayerDef
        {
            public string name = "";
            public float x, y;
            public float scale = 1f;
            /// <summary>none / float / drift / sway / breathe / orbit / figure8 / tilt</summary>
            public string motion = "drift";
            public float ampX = 1f, ampY = 0.5f;   // %
            public float period = 20f;             // 秒
            public float phase;                    // ラジアン
            public float breathe;                  // 大きさの脈 %
            public float rot;                      // 傾きの振れ幅（度）
            public float alpha = 1f;
            public bool front;
            public bool visible = true;
        }

        [System.Serializable]
        private sealed class File { public int version = 1; public List<LayerDef> layers = new List<LayerDef>(); }

        private sealed class Layer { public LayerDef def; public RectTransform rt; public float aspect; }

        private readonly List<Layer> _layers = new List<Layer>();
        private RectTransform _back, _front, _board;

        /// <summary>既定の 3 層。JSON が無いときに使う。</summary>
        public static List<LayerDef> DefaultLayers() => new List<LayerDef>
        {
            new LayerDef { name = "sky_mountains_cloudsea", x = 0f, y = 0f, scale = 1.12f, motion = "drift", ampX = 0.4f, ampY = 0.2f, period = 40f, breathe = 0.6f },
            new LayerDef { name = "castle_lake", x = 0.04f, y = -0.06f, scale = 0.92f, motion = "drift", ampX = 1.0f, ampY = 0.5f, period = 22f, phase = 1.7f, breathe = 1.2f },
            new LayerDef { name = "terrace_balcony", x = 0.03f, y = -0.16f, scale = 1.10f, motion = "drift", ampX = 1.8f, ampY = 0.6f, period = 16f, phase = 3.1f, breathe = 1.0f },
        };

        private static List<LayerDef> Load()
        {
            var ta = Resources.Load<TextAsset>(Path);
            if (ta == null) return DefaultLayers();
            try
            {
                var f = JsonConvert.DeserializeObject<File>(ta.text);
                if (f?.layers != null && f.layers.Count > 0) return f.layers;
            }
            catch (System.Exception e) { Debug.LogWarning("title_layers.json が読めない（既定の層を使う）: " + e.Message); }
            return DefaultLayers();
        }

        /// <summary>parent（元絵と同じ縦横比の板）の中に層を積む。1 枚も読めなければ null。</summary>
        public static TitleParallax Create(RectTransform parent)
        {
            var defs = Load();
            var go = new GameObject("TitleParallax", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            UiSkin.Stretch(rt);
            var p = go.AddComponent<TitleParallax>();
            p._board = rt;
            p._back = UiSkin.Rect(rt, "Back", Vector2.zero, Vector2.zero); UiSkin.Stretch(p._back);
            p._front = UiSkin.Rect(rt, "Front", Vector2.zero, Vector2.zero); UiSkin.Stretch(p._front);

            // 一番奥は空の色の板。層の絵は角が透けているので、そこから黒が見えないようにする
            var backing = UiSkin.Img(p._back, "Backing", Vector2.zero, Vector2.zero, null, new Color(0.80f, 0.88f, 0.97f));
            UiSkin.Stretch(backing.rectTransform);
            backing.raycastTarget = false;

            int n = 0;
            foreach (var d in defs)
            {
                if (d == null || !d.visible) continue;
                var sp = ArtLoader.Sprite("Art/UI/Title/" + d.name);
                if (sp == null) continue;
                var img = UiSkin.Img(d.front ? p._front : p._back, d.name, Vector2.zero, Vector2.zero, sp, new Color(1, 1, 1, d.alpha));
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.raycastTarget = false;
                var lrt = img.rectTransform;
                lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
                lrt.pivot = new Vector2(0.5f, 0.5f);
                p._layers.Add(new Layer { def = d, rt = lrt, aspect = sp.rect.height > 0 ? sp.rect.width / sp.rect.height : 1f });
                n++;
            }
            if (n == 0) { Destroy(go); return null; }
            return p;
        }

        /// <summary>立ち絵を置いたあとに呼ぶ。front の層を立ち絵より手前へ。</summary>
        public void RaiseFront()
        {
            _front.SetParent(_board.parent, false);
            UiSkin.Stretch(_front);
            _front.SetAsLastSibling();
        }

        private void LateUpdate()
        {
            var size = _board.rect.size;
            if (size.x <= 0f) return;
            float t = Time.time;
            foreach (var l in _layers)
            {
                var d = l.def;
                float w = size.x * d.scale;
                l.rt.sizeDelta = new Vector2(w, w / l.aspect);
                float k = d.period > 0.01f ? t * Mathf.PI * 2f / d.period + d.phase : d.phase;
                float ax = d.ampX * 0.01f, ay = d.ampY * 0.01f;
                float dx = 0f, dy = 0f, rot = 0f, sc = 1f;
                switch (d.motion)
                {
                    case "float":   dy = ay * Mathf.Sin(k); break;
                    case "drift":   dx = ax * Mathf.Sin(k); dy = ay * Mathf.Sin(k * 1.3f + 0.8f); break;
                    case "sway":    dx = ax * Mathf.Sin(k); rot = -d.rot * Mathf.Sin(k); break;
                    case "breathe": break;
                    case "orbit":   dx = ax * Mathf.Cos(k); dy = ay * Mathf.Sin(k); break;
                    case "figure8": dx = ax * Mathf.Sin(k); dy = ay * Mathf.Sin(k * 2f); break;
                    case "tilt":    rot = d.rot * Mathf.Sin(k); break;
                }
                if (d.breathe > 0f) sc = 1f + d.breathe * 0.01f * Mathf.Sin(k * 0.5f);
                l.rt.anchoredPosition = new Vector2((d.x + dx) * size.x, (d.y + dy) * size.y);
                l.rt.localRotation = Quaternion.Euler(0, 0, rot);
                l.rt.localScale = Vector3.one * sc;
            }
        }
    }
}
