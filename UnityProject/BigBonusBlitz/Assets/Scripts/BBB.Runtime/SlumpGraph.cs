using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// エンバー（灯火）のスランプグラフ。1G ごとの増減（開始時からの差）を折れ線で描く。
    /// 画像を使わず、細い矩形をつないで線にしている（点が多いときは間引く）。
    /// </summary>
    public sealed class SlumpGraph : MonoBehaviour
    {
        /// <summary>保持する最大点数。これを超えたら 1 つおきに間引いて半分にする。</summary>
        public const int MaxPoints = 2000;
        /// <summary>小型版の最大点数。幅が 130px ほどなので、これ以上持っても見えない。</summary>
        public const int CompactPoints = 160;

        private readonly List<int> _diff = new List<int>();   // 開始からの増減
        private RectTransform _plot;
        private readonly List<Image> _segs = new List<Image>();
        private Image _zeroLine;
        private Text _info, _scaleTop, _scaleBottom;
        private int _baseCredit;
        private int _spins;
        /// <summary>小型表示（脇に常駐させる版）。目盛りと説明を出さない。</summary>
        private bool _compact;
        private int _maxPoints = MaxPoints;
        /// <summary>重ねて描く過去の波形（無ければ null）。</summary>
        private int[] _ghost;
        private readonly List<Image> _ghostSegs = new List<Image>();
        /// <summary>間引き倍率（1 なら毎G、2 なら 2G ごと…）。</summary>
        private int _step = 1;
        private int _sinceLast;

        public int Points => _diff.Count;
        public int Spins => _spins;
        public int LastDiff => _diff.Count > 0 ? _diff[_diff.Count - 1] : 0;
        public int MaxDiff { get; private set; }
        public int MinDiff { get; private set; }

        /// <param name="compact">脇に常駐させる小型版。目盛りと説明を出さず、線も細くする。</param>
        public static SlumpGraph Create(Transform parent, Vector2 pos, Vector2 size, int baseCredit,
                                        bool compact = false)
        {
            var root = UiSkin.Rect(parent, "SlumpGraph", pos, size);
            var g = root.gameObject.AddComponent<SlumpGraph>();
            g._compact = compact;
            g._maxPoints = compact ? CompactPoints : MaxPoints;
            g.Build(root, size, baseCredit);
            return g;
        }

        private void Build(RectTransform root, Vector2 size, int baseCredit)
        {
            _baseCredit = baseCredit;
            UiSkin.Inset(root, "Bg", Vector2.zero, size, _compact ? 5 : 8, UiSkin.Hex("#07090f"));
            // 小型版は文字を持たないぶん、描画域を目一杯まで広げる
            var pad = _compact ? new Vector2(6, 6) : new Vector2(16, 26);
            _plot = UiSkin.Rect(root, "Plot", _compact ? Vector2.zero : Vector2.zero, size - pad);
            _plot.gameObject.AddComponent<RectMask2D>();
            _zeroLine = UiSkin.Img(_plot, "Zero", Vector2.zero, new Vector2(_plot.sizeDelta.x, 1), null, new Color(1, 1, 1, 0.22f));
            if (_compact) { _diff.Add(0); return; }
            _info = UiFactory.Label(root, "Info", new Vector2(0, -size.y * 0.5f + 11), new Vector2(size.x - 16, 16), "", 11, TextAnchor.MiddleCenter, UiSkin.TextSub);
            _scaleTop = UiFactory.Label(root, "Top", new Vector2(-size.x * 0.5f + 34, size.y * 0.5f - 16), new Vector2(64, 14), "", 10, TextAnchor.MiddleLeft, UiSkin.TextDim);
            _scaleBottom = UiFactory.Label(root, "Bottom", new Vector2(-size.x * 0.5f + 34, -size.y * 0.5f + 28), new Vector2(64, 14), "", 10, TextAnchor.MiddleLeft, UiSkin.TextDim);
            _diff.Add(0);
        }

        /// <summary>過去の潜行の波形を重ねて描く。null で消す。</summary>
        public void SetGhost(int[] wave)
        {
            _ghost = (wave != null && wave.Length >= 2) ? wave : null;
            Redraw();
        }

        /// <summary>今の波形を points 点までに間引いて返す（履歴に残すため）。</summary>
        public int[] Snapshot(int points)
        {
            int n = _diff.Count;
            if (n == 0) return new int[0];
            if (n <= points) return _diff.ToArray();
            var outp = new int[points];
            for (int i = 0; i < points; i++)
                outp[i] = _diff[Mathf.Min(n - 1, Mathf.RoundToInt((float)i / (points - 1) * (n - 1)))];
            return outp;
        }

        /// <summary>1G ぶん記録する。credit は現在のエンバー。</summary>
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
            if (_diff.Count > _maxPoints) Thin();
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
            // 縦の範囲: 0 を必ず含み、上下に少し余白。重ねる波形があればそれも入れる
            int hi = Mathf.Max(MaxDiff, 10), lo = Mathf.Min(MinDiff, -10);
            if (_ghost != null)
                for (int i = 0; i < _ghost.Length; i++)
                {
                    if (_ghost[i] > hi) hi = _ghost[i];
                    if (_ghost[i] < lo) lo = _ghost[i];
                }
            int span = Mathf.Max(20, hi - lo);
            float zeroY = -h * 0.5f + h * (0f - lo) / span;
            _zeroLine.rectTransform.anchoredPosition = new Vector2(0, zeroY);
            // 小型版は目盛りも説明も持たない
            if (_scaleTop != null) _scaleTop.text = $"+{hi:N0}";
            if (_scaleBottom != null) _scaleBottom.text = $"{lo:N0}";
            if (_info != null)
                _info.text = $"{_spins:N0} G   増減 {LastDiff:+#,##0;-#,##0;0}   最高 {MaxDiff:+#,##0;0}   最低 {MinDiff:-#,##0;0}";

            // 過去の波形を先に、暗い色で描く（今の線の下に来るように先へ入れてある）
            DrawLine(_ghostSegs, _plot, _ghost, w, h, lo, span, _compact ? 1f : 2f,
                     new Color(1, 1, 1, 0.22f), false);
            DrawLine(_segs, _plot, _diff, w, h, lo, span, _compact ? 1.5f : 2f, default, true);
        }

        /// <summary>点の並びを折れ線にして描く。colorBySign が true なら増減で色を変える。</summary>
        private void DrawLine(List<Image> pool, RectTransform plot, IList<int> pts,
                              float w, float h, int lo, int span, float thick,
                              Color flat, bool colorBySign)
        {
            int n = pts?.Count ?? 0;
            int segCount = Mathf.Max(0, n - 1);
            while (pool.Count < segCount)
                pool.Add(UiSkin.Img(plot, "S", Vector2.zero, new Vector2(2, 2), null, UiSkin.Green));
            for (int i = 0; i < segCount; i++)
            {
                float x0 = -w * 0.5f + w * (n == 1 ? 0 : (float)i / (n - 1));
                float x1 = -w * 0.5f + w * (n == 1 ? 0 : (float)(i + 1) / (n - 1));
                float y0 = -h * 0.5f + h * (pts[i] - lo) / (float)span;
                float y1 = -h * 0.5f + h * (pts[i + 1] - lo) / (float)span;
                var a = new Vector2(x0, y0);
                var b = new Vector2(x1, y1);
                var d = b - a;
                var rt = pool[i].rectTransform;
                rt.anchoredPosition = (a + b) * 0.5f;
                rt.sizeDelta = new Vector2(Mathf.Max(1.5f, d.magnitude), thick);
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                pool[i].color = colorBySign ? (pts[i + 1] >= 0 ? UiSkin.Green : UiSkin.Accent) : flat;
                pool[i].gameObject.SetActive(true);
            }
            for (int i = segCount; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
        }

        /// <summary>記録を捨てて、今のエンバーを基準に取り直す。</summary>
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
