using System;
using BBB.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// 1本のリール表示。main.js startSpinning の移植。
    /// 位置は「上段に来るコマ番号」を実数で持ち、回転中は減っていく（JS の reelOffsets と同じ向き）。
    /// 図柄は Resources/Art/Symbols の png。無ければテキストで代替。
    /// </summary>
    public sealed class ReelView : MonoBehaviour
    {
        /// <summary>実機の約80rpm（20コマ×80/60 ≒ 26.7コマ/秒）。Web版は約44.9コマ/秒だった。</summary>
        public static float SymbolsPerSecond = 20f * 80f / 60f;
        public const float SymbolHeight = 60f;
        public const float ReelWidth = 148f;
        /// <summary>描画枚数。表示3枚 + 上下1枚ずつ（滑らかスクロール用）。</summary>
        private const int Rows = 5;

        public Symbol[] Strip { get; private set; }
        public bool IsSpinning { get; private set; }
        public float SpeedScale = 1f;

        private float _pos;
        private float _remainingSlip = -1f;
        private Image[] _rows;
        private Text[] _rowLabels;
        private Image _frame;
        private bool _useSprites;
        public event Action<ReelView> Stopped;

        public int TopIndex => ((int)Mathf.Floor(_pos) % Strip.Length + Strip.Length) % Strip.Length;

        public static ReelView Create(Transform parent, Symbol[] strip, Vector2 pos)
        {
            var rt = UiFactory.Panel(parent, "Reel", pos, new Vector2(ReelWidth, SymbolHeight * 3), new Color(0.95f, 0.95f, 0.95f));
            rt.gameObject.AddComponent<RectMask2D>();
            var view = rt.gameObject.AddComponent<ReelView>();
            view.Strip = strip;
            view._frame = rt.GetComponent<Image>();
            view._rows = new Image[Rows];
            view._rowLabels = new Text[Rows];
            view._useSprites = ArtLoader.SymbolSprite(Symbol.RED7) != null;
            for (int i = 0; i < Rows; i++)
            {
                float y = (2 - i) * SymbolHeight;
                var go = new GameObject("Row" + i, typeof(RectTransform), typeof(Image));
                var r = go.GetComponent<RectTransform>();
                r.SetParent(rt, false);
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = new Vector2(0, y);
                r.sizeDelta = new Vector2(ReelWidth - 8, SymbolHeight - 4);
                var img = go.GetComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;
                img.enabled = view._useSprites;
                view._rows[i] = img;
                view._rowLabels[i] = UiFactory.Label(rt, "Label" + i, new Vector2(0, y), new Vector2(ReelWidth, SymbolHeight), "", 28);
                view._rowLabels[i].enabled = !view._useSprites;
            }
            // 上下のグラデーション風の暗さ
            UiFactory.Panel(rt, "ShadeTop", new Vector2(0, SymbolHeight * 1.5f - 6), new Vector2(ReelWidth, 12), new Color(0, 0, 0, 0.35f)).GetComponent<Image>().raycastTarget = false;
            UiFactory.Panel(rt, "ShadeBottom", new Vector2(0, -SymbolHeight * 1.5f + 6), new Vector2(ReelWidth, 12), new Color(0, 0, 0, 0.35f)).GetComponent<Image>().raycastTarget = false;
            view._pos = 0;
            view.Redraw();
            return view;
        }

        public void StartSpin()
        {
            IsSpinning = true;
            _remainingSlip = -1f;
        }

        /// <summary>停止指示。finalIdx が上段に来るまで進んで止まる。</summary>
        public void StopAt(int finalIdx)
        {
            int len = Strip.Length;
            float cur = ((_pos % len) + len) % len;
            float dist = cur - finalIdx;
            if (dist < 0) dist += len;
            _remainingSlip = dist;
        }

        private void Update()
        {
            if (!IsSpinning) return;
            float dt = Mathf.Min(Time.deltaTime, 0.1f) * SpeedScale;
            float move = SymbolsPerSecond * dt;
            if (_remainingSlip >= 0f)
            {
                if (_remainingSlip <= move) { move = _remainingSlip; _remainingSlip = 0f; }
                else _remainingSlip -= move;
            }
            _pos -= move;
            int len = Strip.Length;
            if (_pos < 0) _pos += len;
            Redraw();
            if (_remainingSlip == 0f)
            {
                _pos = Mathf.Round(_pos) % len;
                Redraw();
                IsSpinning = false;
                _remainingSlip = -1f;
                Stopped?.Invoke(this);
            }
        }

        private void Redraw()
        {
            int len = Strip.Length;
            int top = TopIndex;
            // 上段コマの「余り」分だけ下にずらして描くと連続スクロールになる
            float frac = _pos - Mathf.Floor(_pos);
            float shift = frac * SymbolHeight;
            for (int i = 0; i < Rows; i++)
            {
                // i=0 は上段の1つ上、i=1 が上段、i=2 中段、i=3 下段、i=4 は下段の1つ下
                var s = Strip[((top + i - 1) % len + len) % len];
                float y = (2 - i) * SymbolHeight + shift;
                if (_useSprites)
                {
                    _rows[i].sprite = ArtLoader.SymbolSprite(s);
                    _rows[i].rectTransform.anchoredPosition = new Vector2(0, y);
                }
                else
                {
                    _rowLabels[i].text = SymbolText(s);
                    _rowLabels[i].color = SymbolColor(s);
                    _rowLabels[i].rectTransform.anchoredPosition = new Vector2(0, y);
                }
            }
        }

        public static string SymbolText(Symbol s)
        {
            switch (s)
            {
                case Symbol.RED7: return "7";
                case Symbol.BLUE7: return "7";
                case Symbol.BAR: return "BAR";
                case Symbol.STAR: return "★";
                case Symbol.WATERMELON: return "SUICA";
                case Symbol.CHERRY: return "CHE";
                case Symbol.REPLAY: return "RP";
                default: return "--";
            }
        }

        public static Color SymbolColor(Symbol s)
        {
            switch (s)
            {
                case Symbol.RED7: return new Color(1f, 0.3f, 0.3f);
                case Symbol.BLUE7: return new Color(0.4f, 0.6f, 1f);
                case Symbol.BAR: return Color.black;
                case Symbol.STAR: return new Color(0.9f, 0.7f, 0f);
                case Symbol.WATERMELON: return new Color(0.1f, 0.6f, 0.1f);
                case Symbol.CHERRY: return new Color(0.9f, 0.2f, 0.4f);
                case Symbol.REPLAY: return new Color(0.2f, 0.5f, 0.9f);
                default: return Color.gray;
            }
        }
    }
}
