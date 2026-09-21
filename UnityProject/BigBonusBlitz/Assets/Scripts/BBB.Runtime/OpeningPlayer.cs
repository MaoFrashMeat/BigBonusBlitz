using System;
using System.Collections;
using BBB.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// 序章の台詞窓（docs/scenario_op.md）。画面の上に暗幕 + 下の台詞窓 + 「飛ばす」を重ねる。
    /// タップ（クリック / Space / Enter）で 1 行ずつ進む。GameController と MapScreen の両方から使う。
    /// 台詞の話者: "" = 主人公、"声" = 姿のない語り手、"*" = ト書き（名札なし・薄い字）、それ以外 = その名前。
    /// </summary>
    public sealed class OpeningPlayer : MonoBehaviour
    {
        private Canvas _canvas;
        private Image _dim;
        private RectTransform _box;
        private Image _nameBg;
        private Text _name, _text, _hint;
        private bool _tapped;
        private Action _onSkip;
        public bool Skipped { get; private set; }
        public string HeroName = "サリア";

        public static OpeningPlayer Create(Action onSkip, string heroName)
        {
            var p = new GameObject("OpeningPlayer").AddComponent<OpeningPlayer>();
            p._onSkip = onSkip; p.HeroName = heroName ?? "主人公";
            p.Build();
            return p;
        }

        private void Build()
        {
            _canvas = UiFactory.CreateCanvas("OpeningCanvas");
            _canvas.sortingOrder = 900;
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;
            // 暗幕（全面）。タップの受け皿も兼ねる
            _dim = UiFactory.Panel(root, "Dim", Vector2.zero, new Vector2(4000, 4000), new Color(0, 0, 0, 0)).GetComponent<Image>();
            _dim.raycastTarget = true;
            var tap = _dim.gameObject.AddComponent<Button>(); tap.transition = Selectable.Transition.None; tap.onClick.AddListener(() => _tapped = true);
            // 台詞窓（下）
            _box = UiSkin.Rect(root, "Box", new Vector2(0, -190), new Vector2(760, 110));
            _box.anchorMin = _box.anchorMax = new Vector2(0.5f, 0.5f);
            UiSkin.Img(_box, "Bg", Vector2.zero, new Vector2(760, 110), UiSkin.Rounded(10), new Color(0.04f, 0.05f, 0.08f, 0.92f), true);
            UiSkin.Img(_box, "Edge", new Vector2(0, 54), new Vector2(740, 1), null, new Color(0.96f, 0.80f, 0.49f, 0.7f));
            _nameBg = UiSkin.Img(_box, "NameBg", new Vector2(-330 + 52, 40), new Vector2(104, 22), UiSkin.Rounded(11), UiSkin.Hex("#f4cd7c"));
            _name = UiFactory.Label(_nameBg.rectTransform, "Name", Vector2.zero, new Vector2(104, 22), "", 12, TextAnchor.MiddleCenter, UiSkin.Bg);
            _name.fontStyle = FontStyle.Bold;
            _text = UiFactory.Label(_box, "Text", new Vector2(0, -8), new Vector2(700, 70), "", 17, TextAnchor.MiddleLeft, UiSkin.Text);
            _text.horizontalOverflow = HorizontalWrapMode.Wrap; _text.verticalOverflow = VerticalWrapMode.Overflow;
            _hint = UiFactory.Label(_box, "Hint", new Vector2(320, -44), new Vector2(160, 16), "▼ タップで進む", 10, TextAnchor.MiddleRight, UiSkin.TextSub);
            var tapBox = _box.gameObject.AddComponent<Button>(); tapBox.transition = Selectable.Transition.None; tapBox.onClick.AddListener(() => _tapped = true);
            _box.GetComponent<Image>();   // Bg が raycast を受ける
            // 飛ばす（右上）
            UiSkin.Button(root, "Skip", new Vector2(420, 240), new Vector2(92, 30), "飛ばす", () => { Skipped = true; _onSkip?.Invoke(); }, UiSkin.PanelHi, 12, false, 8);
            _box.gameObject.SetActive(false);
        }

        public void SetDim(float a) { if (_dim != null) _dim.color = new Color(0, 0, 0, Mathf.Clamp01(a)); }

        public IEnumerator FadeDim(float to, float seconds)
        {
            float from = _dim.color.a, t = 0;
            while (t < seconds) { t += Time.deltaTime; SetDim(Mathf.Lerp(from, to, t / Mathf.Max(0.01f, seconds))); yield return null; }
            SetDim(to);
        }

        public void HideBox() { if (_box != null) _box.gameObject.SetActive(false); }

        /// <summary>1 場面を流す。飛ばされたら途中で抜ける。</summary>
        public IEnumerator Play(OpeningScene scene)
        {
            if (scene == null) yield break;
            SetDim(scene.dim);
            foreach (var ln in scene.lines)
            {
                if (Skipped) yield break;
                if (ln == null || string.IsNullOrEmpty(ln.text)) continue;
                Show(ln);
                yield return WaitTap();
            }
            HideBox();
        }

        /// <summary>1 行を出す（Play が使う。描き出しの確認にも）。</summary>
        public void Show(StoryLine ln)
        {
            _box.gameObject.SetActive(true);
            string sp = ln.speaker ?? "";
            if (sp == "*")
            {
                _nameBg.gameObject.SetActive(false);
                _text.text = "〔" + ln.text + "〕"; _text.color = UiSkin.TextSub; _text.fontStyle = FontStyle.Italic;
            }
            else
            {
                _nameBg.gameObject.SetActive(true);
                bool hero = sp.Length == 0, voice = sp == "声";
                _name.text = hero ? HeroName : voice ? "【声】" : sp;
                _nameBg.color = hero ? UiSkin.Hex("#7ee0a0") : voice ? UiSkin.Hex("#98a3b8") : UiSkin.Hex("#f4cd7c");
                _text.text = ln.text; _text.color = voice ? UiSkin.Hex("#cdd6ea") : UiSkin.Text; _text.fontStyle = voice ? FontStyle.Italic : FontStyle.Normal;
            }
            AudioManager.Create().MotionIfQuiet(MotionCue.Dialogue);
        }

        private IEnumerator WaitTap()
        {
            _tapped = false;
            yield return null;   // 同じフレームのクリックで飛ばさない
            while (!_tapped && !Skipped)
            {
                var kb = Keyboard.current;
                if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) break;
                yield return null;
            }
        }

        public void Close() { if (this != null && gameObject != null) Destroy(gameObject); }
    }
}
