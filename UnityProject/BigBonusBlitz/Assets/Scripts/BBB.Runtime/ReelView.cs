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

        /// <summary>直前の停止の滑りコマ数（制御側の値）。</summary>
        public int LastSlip { get; private set; }
        /// <summary>直前の停止が「引き込み」（通常の最大滑りを超える。持ち越しボーナス狙い）だったか。</summary>
        public bool LastWasPullIn { get; private set; }
        /// <summary>引き込み時に余分に回す周回数。1 = もう一周してから止まる（「引き込まれている」のを見せる）。</summary>
        public static int PullInExtraLaps = 1;

        private float _pullInTotal;      // 引き込み時の総移動コマ数（減速カーブ用）
        private float _bounceT = -1f;    // 停止後の小さな跳ね（秒）。負なら無し
        private float _bounceOffset;

        /// <summary>停止指示。finalIdx が上段に来るまで進んで止まる。slip は制御側が決めた滑りコマ数。</summary>
        public void StopAt(int finalIdx, int slip = 0)
        {
            int len = Strip.Length;
            float cur = ((_pos % len) + len) % len;
            float dist = cur - finalIdx;
            if (dist < 0) dist += len;
            LastSlip = slip;
            LastWasPullIn = slip > SlipController.DefaultMaxSlip;
            // 引き込み: 目的のコマまでの距離に加えて一周余分に回し、終盤で減速して「吸い込まれる」ように止める
            if (LastWasPullIn) dist += len * Mathf.Max(0, PullInExtraLaps);
            _remainingSlip = dist;
            _pullInTotal = dist;
        }

        private void Update()
        {
            if (!IsSpinning)
            {
                // 引き込み停止後の跳ね（1〜2 フレームの余韻）
                if (_bounceT >= 0f)
                {
                    _bounceT += Time.deltaTime;
                    const float d = 0.22f;
                    _bounceOffset = _bounceT >= d ? 0f : 6f * Mathf.Sin(_bounceT / d * Mathf.PI) * (1f - _bounceT / d);
                    if (_bounceT >= d) { _bounceT = -1f; _bounceOffset = 0f; }
                    Redraw();
                }
                return;
            }
            float dt = Mathf.Min(Time.deltaTime, 0.1f) * SpeedScale;
            float speed = SymbolsPerSecond;
            // 引き込み中の終盤 6 コマは 100% → 30% へ滑らかに減速（ガコン、と吸い込まれる感じ）
            if (LastWasPullIn && _remainingSlip >= 0f && _remainingSlip < 6f)
                speed *= Mathf.Lerp(0.3f, 1f, Mathf.SmoothStep(0f, 1f, _remainingSlip / 6f));
            float move = speed * dt;
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
                IsSpinning = false;
                _remainingSlip = -1f;
                if (LastWasPullIn) _bounceT = 0f;
                Redraw();
                Stopped?.Invoke(this);
            }
        }

        private void Redraw()
        {
            int len = Strip.Length;
            int top = TopIndex;
            // 上段コマの「余り」分だけ下にずらして描くと連続スクロールになる
            float frac = _pos - Mathf.Floor(_pos);
            float shift = frac * SymbolHeight + _bounceOffset;
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
