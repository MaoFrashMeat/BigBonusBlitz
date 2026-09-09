using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// クレジットのスランプグラフ。1G ごとの差枚（開始クレジットからの増減）を折れ線で描く。
    /// 画像を使わず、細い矩形をつないで線にしている（点が多いときは間引く）。
    /// </summary>
    public sealed class SlumpGraph : MonoBehaviour
    {
        /// <summary>保持する最大点数。これを超えたら 1 つおきに間引いて半分にする。</summary>
        public const int MaxPoints = 2000;

        private readonly List<int> _diff = new List<int>();   // 開始からの差枚
        private RectTransform _plot;
        private readonly List<Image> _segs = new List<Image>();
        private Image _zeroLine;
        private Text _info, _scaleTop, _scaleBottom;
        private int _baseCredit;
        private int _spins;
        /// <summary>間引き倍率（1 なら毎G、2 なら 2G ごと…）。</summary>
        private int _step = 1;
        private int _sinceLast;

        public int Points => _diff.Count;
        public int LastDiff => _diff.Count > 0 ? _diff[_diff.Count - 1] : 0;
        public int MaxDiff { get; private set; }
        public int MinDiff { get; private set; }

        public static SlumpGraph Create(Transform parent, Vector2 pos, Vector2 size, int baseCredit)
        {
            var root = UiSkin.Rect(parent, "SlumpGraph", pos, size);
            var g = root.gameObject.AddComponent<SlumpGraph>();
            g.Build(root, size, baseCredit);
            return g;
        }

        private void Build(RectTransform root, Vector2 size, int baseCredit)
        {
            _baseCredit = baseCredit;
            UiSkin.Inset(root, "Bg", Vector2.zero, size, 8, UiSkin.Hex("#07090f"));
            _plot = UiSkin.Rect(root, "Plot", Vector2.zero, size - new Vector2(16, 26));
            _plot.gameObject.AddComponent<RectMask2D>();
            _zeroLine = UiSkin.Img(_plot, "Zero", Vector2.zero, new Vector2(_plot.sizeDelta.x, 1), null, new Color(1, 1, 1, 0.22f));
            _info = UiFactory.Label(root, "Info", new Vector2(0, -size.y * 0.5f + 11), new Vector2(size.x - 16, 16), "", 11, TextAnchor.MiddleCenter, UiSkin.TextSub);
            _scaleTop = UiFactory.Label(root, "Top", new Vector2(-size.x * 0.5f + 34, size.y * 0.5f - 16), new Vector2(64, 14), "", 10, TextAnchor.MiddleLeft, UiSkin.TextDim);
            _scaleBottom = UiFactory.Label(root, "Bottom", new Vector2(-size.x * 0.5f + 34, -size.y * 0.5f + 28), new Vector2(64, 14), "", 10, TextAnchor.MiddleLeft, UiSkin.TextDim);
            _diff.Add(0);
        }

        /// <summary>1G ぶん記録する。credit は現在のクレジット。</summary>
        public void Push(int credit)
        {
            _spins++;
            _sinceLast++;
            if (_sinceLast < _step) return;
            _sinceLast = 0;
            int d = credit - _baseCredit;
            _diff.Add(d);
            if (d > MaxDiff) MaxDiff = d;
            if (d < MinDiff) MinDiff = d;
            if (_diff.Count > MaxPoints) Thin();
        }

        /// <summary>点が増えすぎたら 1 つおきに捨てて、間引き倍率を倍にする。</summary>
        private void Thin()
        {
            for (int i = _diff.Count - 2; i > 0; i -= 2) _diff.RemoveAt(i);
            _step *= 2;
        }

        /// <summary>今の記録を描き直す。開いているときだけ呼べばよい。</summary>
        public void Redraw()
        {
            float w = _plot.sizeDelta.x, h = _plot.sizeDelta.y;
            int n = _diff.Count;
            // 縦の範囲: 0 を必ず含み、上下に少し余白
            int hi = Mathf.Max(MaxDiff, 10), lo = Mathf.Min(MinDiff, -10);
            int span = Mathf.Max(20, hi - lo);
            float zeroY = -h * 0.5f + h * (0f - lo) / span;
            _zeroLine.rectTransform.anchoredPosition = new Vector2(0, zeroY);
            _scaleTop.text = $"+{hi:N0}";
            _scaleBottom.text = $"{lo:N0}";
            _info.text = $"{_spins:N0} G   差枚 {LastDiff:+#,##0;-#,##0;0}   最高 {MaxDiff:+#,##0;0}   最低 {MinDiff:-#,##0;0}";

            int segCount = Mathf.Max(0, n - 1);
            EnsureSegments(segCount);
            for (int i = 0; i < segCount; i++)
            {
                float x0 = -w * 0.5f + w * (n == 1 ? 0 : (float)i / (n - 1));
                float x1 = -w * 0.5f + w * (n == 1 ? 0 : (float)(i + 1) / (n - 1));
                float y0 = -h * 0.5f + h * (_diff[i] - lo) / (float)span;
                float y1 = -h * 0.5f + h * (_diff[i + 1] - lo) / (float)span;
                var seg = _segs[i];
                var a = new Vector2(x0, y0);
                var b = new Vector2(x1, y1);
                var mid = (a + b) * 0.5f;
                var d = b - a;
                float len = Mathf.Max(1.5f, d.magnitude);
                var rt = seg.rectTransform;
                rt.anchoredPosition = mid;
                rt.sizeDelta = new Vector2(len, 2f);
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                seg.color = _diff[i + 1] >= 0 ? UiSkin.Green : UiSkin.Accent;
                seg.gameObject.SetActive(true);
            }
            for (int i = segCount; i < _segs.Count; i++) _segs[i].gameObject.SetActive(false);
        }

        private void EnsureSegments(int count)
        {
            while (_segs.Count < count)
                _segs.Add(UiSkin.Img(_plot, "S", Vector2.zero, new Vector2(2, 2), null, UiSkin.Green));
        }

        /// <summary>記録を捨てて、今のクレジットを基準に取り直す。</summary>
        public void ResetTo(int credit)
        {
            _diff.Clear();
            _diff.Add(0);
            MaxDiff = 0; MinDiff = 0; _spins = 0; _step = 1; _sinceLast = 0;
            _baseCredit = credit;
            Redraw();
        }
    }
}
