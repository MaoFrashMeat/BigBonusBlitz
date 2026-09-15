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
        /// <summary>図柄 1 コマの高さ。筐体が中段と下段にまたがる高さ（184）に 3 コマ収める。</summary>
        public const float SymbolHeight = 56f;
        /// <summary>リールの幅。図柄の枠は ReelWidth-8 x SymbolHeight-4 になる。
        /// いまの図柄は 160x73（比 2.19）なので、枠がその比に近くなるよう 132 にしてある
        /// （148 のままだと枠が 2.50 で、左右に 8.5px ずつ隙間が空く）。</summary>
        public const float ReelWidth = 123f;     // 枠 115x52（比 2.21）
        /// <summary>描画枚数。表示3枚 + 上下1枚ずつ（滑らかスクロール用）。</summary>
        private const int Rows = 5;

        public Symbol[] Strip { get; private set; }
        public bool IsSpinning { get; private set; }
        public float SpeedScale = 1f;

        private float _pos;
        private float _remainingSlip = -1f;
        private Image[] _rows;
        // 図柄の後ろの光（ランプ）と、図柄の形に重ねる層（ランプの内側の光 / 役の演出の層）。合成は UIBlend.shader
        private Image[] _lampBack, _lampInner, _fxLayer, _outline, _outlineGlow;
        private SymbolOutlineConfig _outlineCfg;      // mode = always のとき全段に出す
        private SymbolLampConfig _lamp;
        private static Shader _blendShader;
        private static readonly System.Collections.Generic.Dictionary<string, Material> _blendMats = new System.Collections.Generic.Dictionary<string, Material>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void ResetMats() { _blendMats.Clear(); _blendShader = null; _outlineMats.Clear(); _outlineShader = null; }

        /// <summary>合成の種類ごとの Material（add / screen / multiply）。無ければ null（通常の合成で描く）。</summary>
        // 縁取りの Material（通常 / 加算）。太さは _WidthUV（px / 表示幅）で、行ごとに SetFloat せず共有するので、太さごとに 1 つ
        private static Shader _outlineShader;
        private static readonly System.Collections.Generic.Dictionary<string, Material> _outlineMats = new System.Collections.Generic.Dictionary<string, Material>();
        public static Material OutlineMaterial(float widthPx, bool additive)
        {
            string key = (additive ? "a" : "n") + Mathf.RoundToInt(widthPx * 4);
            if (_outlineMats.TryGetValue(key, out var m) && m != null) return m;
            if (_outlineShader == null) _outlineShader = Resources.Load<Shader>("Art/Symbols/UIOutline");
            if (_outlineShader == null) return null;
            m = new Material(_outlineShader) { name = "UIOutline/" + key };
            m.SetFloat("_WidthUV", widthPx / (ReelWidth - 8f));
            m.SetFloat("_SrcBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.SrcAlpha));
            m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            _outlineMats[key] = m;
            return m;
        }

        public static Material BlendMaterial(string mode)
        {
            mode = string.IsNullOrEmpty(mode) ? "add" : mode.ToLowerInvariant();
            if (_blendMats.TryGetValue(mode, out var m) && m != null) return m;
            if (_blendShader == null) _blendShader = Resources.Load<Shader>("Art/Symbols/UIBlend");
            if (_blendShader == null) return null;
            m = new Material(_blendShader) { name = "UIBlend/" + mode };
            switch (mode)
            {
                case "screen": m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcColor); m.SetFloat("_Mode", 2); break;
                case "multiply": m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.DstColor); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero); m.SetFloat("_Mode", 3); break;
                default: m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One); m.SetFloat("_Mode", 1); break;
            }
            _blendMats[mode] = m;
            return m;
        }

        public static Color ParseColor(string hex, Color fallback)
        {
            return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
        }

        /// <summary>ランプの設定（game_config の reelFx.lamp）。null で消す。</summary>
        public void SetLamp(SymbolLampConfig lamp)
        {
            _lamp = lamp != null && lamp.enabled ? lamp : null;
            Redraw();
        }

        private bool LampFor(Symbol s, out Color color)
        {
            color = Color.white;
            if (_lamp == null || _lamp.symbols == null || !_lamp.symbols.Contains(s.ToString())) return false;
            string hex = null; _lamp.colors?.TryGetValue(s.ToString(), out hex);
            color = ParseColor(hex, s == Symbol.RED7 ? new Color(1f, .29f, .29f) : s == Symbol.BLUE7 ? new Color(.62f, .85f, 1f) : Color.white);
            return true;
        }
        private Text[] _rowLabels;
        private Image _frame;
        private bool _useSprites;
        public event Action<ReelView> Stopped;

        public int TopIndex => ((int)Mathf.Floor(_pos) % Strip.Length + Strip.Length) % Strip.Length;
        /// <summary>上段コマの中での位置（0〜1。1 に近いほどそのコマが来たばかり、0 に近いほど次のコマに移る直前）。</summary>
        public float Frac => _pos - Mathf.Floor(_pos);

        private double _updateRealtime = -1;   // 直前の Update の実時間と、そのときの位置
        private float _posAtUpdate;

        /// <summary>
        /// 押した瞬間（pressRealtime: Time.realtimeSinceStartup と同じ軸。負なら今）の上段コマと、そのコマ内の位置。
        /// 自由回転中は直前の Update からの時間ぶん位置を戻して出す（Input System のイベント時刻で 1 フレームより細かく測る。技術介入のランク用）。
        /// </summary>
        public void PressPosition(double pressRealtime, out int top, out float frac)
        {
            int len = Strip.Length;
            float pos = _pos;
            if (pressRealtime >= 0 && IsSpinning && _remainingSlip < 0f && _updateRealtime >= 0)
            {
                // 押してから直前の Update までの時間（負なら Update の後に押された分だけ進める）。回るほど _pos は減る
                float back = Mathf.Clamp((float)(_updateRealtime - pressRealtime), -0.1f, 0.1f);
                pos = _posAtUpdate + SymbolsPerSecond * SpeedScale * back;
            }
            pos = ((pos % len) + len) % len;
            int fl = (int)Mathf.Floor(pos);
            top = fl % len;
            frac = pos - fl;
        }

        private static Image Overlay(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var r = go.GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            var im = go.GetComponent<Image>();
            im.preserveAspect = true; im.raycastTarget = false; im.enabled = false;
            im.material = BlendMaterial("add");
            return im;
        }

        public static ReelView Create(Transform parent, Symbol[] strip, Vector2 pos)
        {
            var rt = UiFactory.Panel(parent, "Reel", pos, new Vector2(ReelWidth, SymbolHeight * 3), new Color(0.95f, 0.95f, 0.95f));
            rt.gameObject.AddComponent<RectMask2D>();
            var view = rt.gameObject.AddComponent<ReelView>();
            view.Strip = strip;
            view._frame = rt.GetComponent<Image>();
            view._rows = new Image[Rows];
            view._lampBack = new Image[Rows]; view._lampInner = new Image[Rows]; view._fxLayer = new Image[Rows]; view._outline = new Image[Rows]; view._outlineGlow = new Image[Rows];
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
                // ランプの後ろの光（図柄の下に置くので、行の Image より先の兄弟にする）
                var back = UiSkin.Img(rt, "Lamp" + i, new Vector2(0, y), new Vector2(ReelWidth * 1.4f, SymbolHeight * 2.2f), UiSkin.Glow(96), new Color(1, 1, 1, 0));
                back.material = BlendMaterial("add"); back.enabled = false; back.transform.SetSiblingIndex(r.GetSiblingIndex());
                view._lampBack[i] = back;
                // 図柄の形に重ねる層 2 枚（ランプの内側の光 / 役の演出の層）。親と同じ矩形・同じ絵
                view._lampInner[i] = Overlay(r, "Inner");
                view._outlineGlow[i] = Overlay(r, "OutlineGlow");
                view._outline[i] = Overlay(r, "Outline");
                view._fxLayer[i] = Overlay(r, "Layer");
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

        // 窓の段（0=上 1=中 2=下）ごとの演出。役が決まったコマに掛ける（GameController.PaylineFlashRoutine）
        private readonly Color[] _fxTint = { Color.white, Color.white, Color.white };
        private readonly float[] _fxScale = { 1f, 1f, 1f };
        private readonly Vector2[] _fxOffset = new Vector2[3];
        private readonly float[] _fxRot = new float[3];
        private readonly string[] _fxLayerMode = new string[3];
        private readonly Color[] _fxLayerColor = new Color[3];       // a = 0 で層なし

        private readonly Color[] _fxOutline = new Color[3];         // 揃ったコマの縁取り（a = 0 で無し）

        /// <summary>縁取りの設定（mode = always なら全段に常時）。</summary>
        public void SetOutline(SymbolOutlineConfig cfg) { _outlineCfg = cfg != null && cfg.enabled ? cfg : null; Redraw(); }

        /// <summary>段の図柄の縁取り（役の演出用）。color の a が濃さ。a=0 で消す。</summary>
        public void SetRowOutline(int windowRow, Color color)
        {
            if (windowRow < 0 || windowRow > 2) return;
            _fxOutline[windowRow] = color;
            if (!IsSpinning) Redraw();
        }

        /// <summary>段の図柄に重ねる層（mode: add / screen / multiply、color の a が強さ）。a=0 で消す。</summary>
        public void SetRowLayer(int windowRow, string mode, Color color)
        {
            if (windowRow < 0 || windowRow > 2) return;
            _fxLayerMode[windowRow] = mode; _fxLayerColor[windowRow] = color;
            if (!IsSpinning) Redraw();
        }

        /// <summary>窓の段（0=上 1=中 2=下）の図柄の明るさ（1 で通常）。役が決まったコマの点滅に使う。</summary>
        public void SetRowBrightness(int windowRow, float k) => SetRowFx(windowRow, new Color(k, k, k, 1f), 1f, Vector2.zero, 0f);

        /// <summary>段の図柄に 色（明るさ・色味）・拡大率・ずらし（px）・傾き（度）を掛ける。回転中は掛けない。</summary>
        public void SetRowFx(int windowRow, Color tint, float scale, Vector2 offset, float rot)
        {
            if (_rows == null || windowRow < 0 || windowRow > 2) return;
            _fxTint[windowRow] = tint; _fxScale[windowRow] = scale; _fxOffset[windowRow] = offset; _fxRot[windowRow] = rot;
            if (!IsSpinning) Redraw();
        }

        /// <summary>全段の演出を戻す（次の回転の前に）。</summary>
        public void ResetBrightness()
        {
            if (_rows == null) return;
            for (int r = 0; r < 3; r++) { _fxTint[r] = Color.white; _fxScale[r] = 1f; _fxOffset[r] = Vector2.zero; _fxRot[r] = 0f; _fxLayerColor[r] = new Color(0, 0, 0, 0); _fxOutline[r] = new Color(0, 0, 0, 0); }
            foreach (var img in _rows) if (img != null) { img.color = Color.white; img.rectTransform.localScale = Vector3.one; img.rectTransform.localRotation = Quaternion.identity; }
            Redraw();
        }

        private void Update()
        {
            if (!IsSpinning)
            {
                // ランプの脈（停止中も毎フレーム描き直す）
                if (_lamp != null && _lamp.pulse > 0f && _bounceT < 0f) Redraw();
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
            _updateRealtime = Time.realtimeSinceStartupAsDouble; _posAtUpdate = _pos;
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
                    // 窓の 3 段（i=1..3）には役の演出（明るさ・拡大・ずらし・傾き）を掛ける。回転中は掛けない
                    int wr = i - 1; bool fx = !IsSpinning && wr >= 0 && wr <= 2;
                    _rows[i].rectTransform.anchoredPosition = fx ? new Vector2(_fxOffset[wr].x, y + _fxOffset[wr].y) : new Vector2(0, y);
                    _rows[i].rectTransform.localScale = fx ? new Vector3(_fxScale[wr], _fxScale[wr], 1f) : Vector3.one;
                    _rows[i].rectTransform.localRotation = fx ? Quaternion.Euler(0, 0, _fxRot[wr]) : Quaternion.identity;
                    _rows[i].color = fx ? _fxTint[wr] : Color.white;
                    // 縁取り: always なら全段、それ以外は役の演出で指定された段だけ
                    var ol = _outline[i]; var og = _outlineGlow[i];
                    if (ol != null && og != null)
                    {
                        Color oc = new Color(0, 0, 0, 0); float ow = 2f; bool glow = false; float gw = 6f, ga = 0.5f;
                        if (_outlineCfg != null && _outlineCfg.mode == "always")
                        {
                            oc = ParseColor(_outlineCfg.color == "role" ? "#ffffff" : _outlineCfg.color, Color.white); oc.a = Mathf.Clamp01(_outlineCfg.alpha);
                            ow = _outlineCfg.width; glow = _outlineCfg.glow; gw = _outlineCfg.glowWidth; ga = _outlineCfg.glowAlpha;
                        }
                        else if (fx && _fxOutline[wr].a > 0.001f && _outlineCfg != null)
                        {
                            oc = _fxOutline[wr]; ow = _outlineCfg.width; glow = _outlineCfg.glow; gw = _outlineCfg.glowWidth; ga = _outlineCfg.glowAlpha;
                        }
                        bool show = oc.a > 0.001f;
                        ol.enabled = show; og.enabled = show && glow;
                        if (show)
                        {
                            ol.sprite = _rows[i].sprite; ol.color = oc; var om = OutlineMaterial(ow, false); if (om != null && ol.material != om) ol.material = om;
                            if (glow) { og.sprite = _rows[i].sprite; og.color = new Color(oc.r, oc.g, oc.b, oc.a * ga); var gm = OutlineMaterial(gw, true); if (gm != null && og.material != gm) og.material = gm; }
                        }
                    }
                    // 役の演出の層（揃ったコマだけ）
                    var layer = _fxLayer[i];
                    bool hasLayer = fx && _fxLayerColor[wr].a > 0.001f;
                    if (layer != null && (layer.enabled = hasLayer))
                    {
                        layer.sprite = _rows[i].sprite; layer.color = _fxLayerColor[wr];
                        var lm = BlendMaterial(_fxLayerMode[wr]); if (layer.material != lm && lm != null) layer.material = lm;
                    }
                    // ランプ（赤 7・白 7 など）。停止中はずっと、回転中は設定次第
                    Color lampColor = Color.white;
                    bool lampOn = _lamp != null && (!IsSpinning || _lamp.whileSpinning) && LampFor(s, out lampColor);
                    if (_lampBack[i] != null) _lampBack[i].enabled = lampOn;
                    if (_lampInner[i] != null) _lampInner[i].enabled = lampOn;
                    if (lampOn)
                    {
                        float pulse = _lamp.pulse > 0f ? 1f - Mathf.Clamp01(_lamp.pulseDepth) * (0.5f + 0.5f * Mathf.Sin(Time.time * _lamp.pulse * Mathf.PI * 2f + i * 0.9f)) : 1f;
                        _lampBack[i].rectTransform.anchoredPosition = new Vector2(0, y);
                        _lampBack[i].rectTransform.sizeDelta = new Vector2(ReelWidth * _lamp.size, SymbolHeight * _lamp.size * 1.6f);
                        _lampBack[i].color = new Color(lampColor.r, lampColor.g, lampColor.b, Mathf.Clamp01(_lamp.intensity) * pulse);
                        _lampInner[i].sprite = _rows[i].sprite;
                        float inner = _lamp.innerBySymbol != null && _lamp.innerBySymbol.TryGetValue(s.ToString(), out var iv) ? iv : _lamp.inner;
                        _lampInner[i].color = new Color(lampColor.r, lampColor.g, lampColor.b, Mathf.Clamp01(inner) * pulse);
                    }
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
