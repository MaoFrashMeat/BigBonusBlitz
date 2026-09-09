using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>
    /// セーフエリア対応の「舞台」。
    ///   Canvas（高さ 540 基準で横に伸びる）
    ///     └ SafeRoot（Screen.safeArea にぴったり合わせる）
    ///         └ Stage（960×540 の仮想 16:9 枠。SafeRoot に収まるよう等倍縮小）
    /// 19.5:9 のスマホ横持ちでは Stage の外側に余白ができる。そこは背景だけを見せる（game-design §3.2）。
    /// 解像度・回転・セーフエリアの変化は毎フレーム監視して追従する。
    /// </summary>
    public sealed class SafeStage : MonoBehaviour
    {
        public const float StageW = 960f, StageH = 540f;

        public RectTransform SafeRoot { get; private set; }
        public RectTransform Stage { get; private set; }
        /// <summary>Stage の現在の縮尺（1 = 等倍）。</summary>
        public float Scale { get; private set; } = 1f;
        /// <summary>Canvas 単位でのセーフエリア幅・高さ。</summary>
        public Vector2 SafeSize { get; private set; }

        private Canvas _canvas;
        private RectTransform _canvasRt;
        private Rect _lastSafe;
        private int _lastW, _lastH;
        private float _lastScaleFactor;
        private Vector2 _lastCanvasSize;

        public static SafeStage Create(Canvas canvas)
        {
            var root = new GameObject("SafeRoot", typeof(RectTransform));
            var safeRt = root.GetComponent<RectTransform>();
            safeRt.SetParent(canvas.transform, false);
            safeRt.anchorMin = Vector2.zero; safeRt.anchorMax = Vector2.one;
            safeRt.offsetMin = Vector2.zero; safeRt.offsetMax = Vector2.zero;

            var stage = new GameObject("Stage", typeof(RectTransform));
            var stageRt = stage.GetComponent<RectTransform>();
            stageRt.SetParent(safeRt, false);
            stageRt.anchorMin = stageRt.anchorMax = new Vector2(0.5f, 0.5f);
            stageRt.pivot = new Vector2(0.5f, 0.5f);
            stageRt.sizeDelta = new Vector2(StageW, StageH);

            var s = root.AddComponent<SafeStage>();
            s._canvas = canvas;
            s._canvasRt = canvas.GetComponent<RectTransform>();
            s.SafeRoot = safeRt;
            s.Stage = stageRt;
            s.Apply(true);
            return s;
        }

        private void Update() => Apply(false);

        private void Apply(bool force)
        {
            var safe = Screen.safeArea;
            float sf = _canvas.scaleFactor;
            var canvasSize = _canvasRt.rect.size;
            if (!force && safe == _lastSafe && Screen.width == _lastW && Screen.height == _lastH && Mathf.Approximately(sf, _lastScaleFactor) && canvasSize == _lastCanvasSize) return;
            _lastSafe = safe; _lastW = Screen.width; _lastH = Screen.height; _lastScaleFactor = sf; _lastCanvasSize = canvasSize;
            if (canvasSize.x <= 0 || canvasSize.y <= 0) { _lastCanvasSize = Vector2.zero; return; }   // Canvas 未確定なら次フレームに再試行

            // Screen 座標（px）→ Canvas 単位
            float w = Screen.width, h = Screen.height;
            if (w <= 0 || h <= 0 || sf <= 0) return;
            var min = new Vector2(safe.xMin / w, safe.yMin / h);
            var max = new Vector2(safe.xMax / w, safe.yMax / h);
            SafeRoot.anchorMin = min;
            SafeRoot.anchorMax = max;
            SafeRoot.offsetMin = Vector2.zero;
            SafeRoot.offsetMax = Vector2.zero;

            SafeSize = new Vector2(canvasSize.x * (max.x - min.x), canvasSize.y * (max.y - min.y));
            Scale = Mathf.Min(1f, Mathf.Min(SafeSize.x / StageW, SafeSize.y / StageH));
            if (Scale <= 0 || float.IsNaN(Scale)) Scale = 1f;
            Stage.localScale = Vector3.one * Scale;
        }

        /// <summary>Stage の外側（左右の余白）が何 Canvas 単位あるか。</summary>
        public float SideBleed => Mathf.Max(0f, (SafeSize.x - StageW * Scale) * 0.5f);
    }
}
