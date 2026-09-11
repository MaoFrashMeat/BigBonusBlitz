using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// スタート画面。起動時に最初に出る。
    /// 「つづきから」（セーブあり）／「はじめから」を選ぶか、Space / Enter でスタート。
    /// スタート時に暗転して GameController を起動する。
    /// </summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<TitleScreen>() != null || FindFirstObjectByType<MapScreen>() != null || FindFirstObjectByType<GameController>() != null) return;
            Open();
        }

        /// <summary>タイトルを開く（マップの「タイトルへ」からも呼ぶ）。</summary>
        public static TitleScreen Open()
        {
            var exist = FindFirstObjectByType<TitleScreen>();
            if (exist != null) return exist;
            return new GameObject("TitleScreen").AddComponent<TitleScreen>();
        }

        private const float StageW = 960f, StageH = 540f;

        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        static readonly Color ColBg = UiSkin.Bg;
        static readonly Color ColText = UiSkin.Text;
        static readonly Color ColTextSub = UiSkin.TextSub;
        static readonly Color ColAccent = UiSkin.Accent;
        static readonly Color ColGold = UiSkin.Gold;
        static readonly Color ColBtn = UiSkin.Btn;

        private Canvas _canvas;
        private SafeStage _safe;
        private AudioManager _audio;
        private RectTransform _logo;
        private Text _press, _confirm;
        private CanvasGroup _fade;
        private Button _btnContinue, _btnNew;
        private GameObject _settingsBox;
        private bool _starting;
        private bool _hasSave;
        private float _confirmUntil;   // 「はじめから」2度押し確認の期限
        private float _t;

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 120;
            _hasSave = SaveData.Exists();
            _audio = AudioManager.Create();
            SaveData.LoadAudio(_audio);      // 保存済みの音量・BGM ON/OFF をタイトルにも適用
            _audio.StartBgm();
            BuildUi();
        }

        /// <summary>ロゴとボタンを置く横位置。絵の人物が右にいるので左へ寄せる。</summary>
        private const float TitleX = -166f;
        /// <summary>ロゴの幅。高さは絵の縦横比から出す。</summary>
        /// <summary>元のタイトル絵の縦横比（絵が読めないときの保険）。</summary>
        private const float TitleArtAspect = 1847f / 851f;
        private const float LogoW = 470f;
        /// <summary>ロゴの高さ。今のロゴは比 1.73 で縦に高いので、
        /// TAP TO START との隙間が 12px 残る位置に上げてある。</summary>
        private const float LogoY = 112f;
        /// <summary>TAP TO START の高さ。</summary>
        private const float TapY = -52f;
        /// <summary>下の丸ボタン。</summary>
        private const float PillW = 152f, PillH = 40f, PillY = -186f;

        // ------------------------------------------------------------------ UI
        private void BuildUi()
        {
            UiFactory.EnsureEventSystem();
            _canvas = UiFactory.CreateCanvas("TitleCanvas");
            var root = _canvas.transform;
            var bgFull = UiSkin.Img(root, "BG", Vector2.zero, Vector2.zero, null, ColBg);
            UiSkin.Stretch(bgFull.rectTransform);

            // 背景: 1 枚絵を画面いっぱいに（セーフエリア外も覆う）。
            // 端末の縦横比が絵と違うぶんは、余白ではなく切って詰める
            var bgArea = UiSkin.Rect(root, "BgArea", Vector2.zero, Vector2.zero);
            UiSkin.Stretch(bgArea);
            bgArea.gameObject.AddComponent<RectMask2D>();
            // 背景と立ち絵を別々に置く。立ち絵は格子に割って波打たせるので、
            // 後ろに「キャラのいない背景」が要る（tools/comfy で作ったもの）
            var titleSprite = ArtLoader.Sprite("Art/UI/title_bg");
            var charSprite = ArtLoader.Sprite("Art/UI/title_char");
            if (titleSprite != null)
            {
                // 元絵と同じ縦横比の板を作り、その中で背景と立ち絵の位置を決める。
                // 板ごと画面いっぱいに広げるので、2 枚の位置関係は崩れない
                var artGo = new GameObject("TitleArt", typeof(RectTransform));
                var artRt = artGo.GetComponent<RectTransform>();
                artRt.SetParent(bgArea, false);
                artRt.anchorMin = artRt.anchorMax = new Vector2(0.5f, 0.5f);
                var r = titleSprite.rect;
                AspectCover.Attach(artRt, r.height > 0 ? r.width / r.height : TitleArtAspect);

                var bgImg = UiSkin.Img(artRt, "Bg", Vector2.zero, Vector2.zero, titleSprite, Color.white);
                UiSkin.Stretch(bgImg.rectTransform);
                bgImg.raycastTarget = false;

                // 体と髪を別々に重ねる。体は呼吸だけ、髪はそれより大きく揺れる。
                // 3 枚とも元絵と同じ画布なので、板いっぱいに広げれば位置は合う
                AddLayer(artRt, "Char", charSprite, TitleCharacterWarp.Mode.Body);
                AddLayer(artRt, "Hair", ArtLoader.Sprite("Art/UI/title_hair"),
                         TitleCharacterWarp.Mode.Hair);
            }
            else
            {
                // 絵が無いときはこれまでどおりパララックスで見せる
                var bg = ParallaxBackground.Create(bgArea);
                bg.IsWalking = true;
            }
            // 左半分を暗くしてロゴと文字を読みやすく（§9.2）。人物は右にいるので触らない
            var scrim = UiSkin.Img(root, "Scrim", Vector2.zero, Vector2.zero, UiSkin.GradientH(false), new Color(0, 0, 0, 0.72f));
            UiSkin.Stretch(scrim.rectTransform);
            scrim.raycastTarget = false;
            UiSkin.Vignette(root, "DimBottom", Vector2.zero, Vector2.zero, 0.5f, false);
            UiSkin.Stretch(root.Find("DimBottom").GetComponent<RectTransform>());

            // 光の玉と桜の花びら。暗みより手前、ロゴや文字より後ろに漂わせる
            TitleAmbience.Create(root);

            // セーフエリアに収まる舞台（960×540、縮小のみ）
            _safe = SafeStage.Create(_canvas);
            var stage = _safe.Stage;

            // ロゴ。幅を決めて、高さは絵の縦横比から出す（正方形の枠に収めると小さくなる）
            var logoSprite = ArtLoader.Sprite("Art/UI/bbb_logo_main");
            var logoGo = new GameObject("Logo", typeof(RectTransform), typeof(Image));
            _logo = logoGo.GetComponent<RectTransform>();
            _logo.SetParent(stage, false);
            _logo.anchorMin = _logo.anchorMax = new Vector2(0.5f, 0.5f);
            float logoH = LogoW / 3f;
            if (logoSprite != null)
            {
                var lr = logoSprite.rect;
                if (lr.width > 0) logoH = LogoW * lr.height / lr.width;
            }
            _logo.sizeDelta = new Vector2(LogoW, logoH);
            _logo.anchoredPosition = new Vector2(TitleX, LogoY);
            var logoImg = logoGo.GetComponent<Image>();
            logoImg.sprite = logoSprite;
            logoImg.preserveAspect = true;
            logoImg.raycastTarget = false;
            if (logoSprite == null)
            {
                logoImg.enabled = false;
                UiFactory.Label(stage, "TitleText", new Vector2(TitleX, LogoY), new Vector2(LogoW, 80), "BIG BONUS BLITZ", 40, TextAnchor.MiddleCenter, ColGold).fontStyle = FontStyle.Bold;
            }

            // 左端の縦書きの惹句は 2026-09-11 に外した（本人の判断。絵の邪魔になる）。VerticalText は残してある

            // TAP TO START。上下に細い飾り線
            UiSkin.Img(stage, "TapLineTop", new Vector2(TitleX, TapY + 26), new Vector2(340, 1), null, new Color(1, 1, 1, 0.5f));
            UiSkin.Img(stage, "TapLineBottom", new Vector2(TitleX, TapY - 26), new Vector2(340, 1), null, new Color(1, 1, 1, 0.5f));
            _press = UiFactory.Label(stage, "Press", new Vector2(TitleX, TapY), new Vector2(420, 32),
                "T A P   T O   S T A R T", 22, TextAnchor.MiddleCenter, ColText);
            _press.fontStyle = FontStyle.Bold;
            Shade(_press);
            // 画面のどこを押しても始まる（見本と同じ挙動）
            var tapAll = UiFactory.Panel(stage, "TapArea", Vector2.zero, new Vector2(StageW, StageH), new Color(0, 0, 0, 0));
            var tapBtn = tapAll.gameObject.AddComponent<Button>();
            tapBtn.transition = Selectable.Transition.None;
            tapBtn.onClick.AddListener(() => { if (!_starting) { _audio.UiPop(); Begin(false); } });
            tapAll.SetAsFirstSibling();     // ボタン類より後ろに置いて、そちらの操作を邪魔しない

            // はじめから（セーブがあるときだけ出す。無いときは TAP がそのまま新規）
            if (_hasSave)
            {
                _btnNew = UiSkin.Button(stage, "NewGame", new Vector2(TitleX, TapY - 56), new Vector2(200, 30),
                    "はじめから", () => { _audio.UiPop(); OnNewGame(); }, ColBtn, 13, false, 15);
            }
            _confirm = UiFactory.Label(stage, "Confirm", new Vector2(TitleX, TapY - 86), new Vector2(460, 22), "", 12, TextAnchor.MiddleCenter, ColGold);
            Shade(_confirm);

            // 下の丸ボタン 3 つ
            float px = -StageW * 0.5f + 24 + PillW * 0.5f;
            Pill(stage, "News", new Vector2(px, PillY), "bell", "お知らせ", () => Notice("お知らせは準備中です"));
            px += PillW + 12f;
            Pill(stage, "Config", new Vector2(px, PillY), "gear", "設定", ToggleSettings);
            px += PillW + 12f;
            Pill(stage, "Transfer", new Vector2(px, PillY), "link", "引き継ぎ", () => Notice("引き継ぎは準備中です"));

            // 右上のメニュー
            var menu = UiSkin.Button(stage, "Menu", new Vector2(StageW * 0.5f - 34, StageH * 0.5f - 34), new Vector2(44, 44),
                "≡", ToggleSettings, ColBtn, 22, false, 10);
            UiSkin.SetButtonColor(menu, ColBtn, ColGold);

            // フッター
            var ver = UiFactory.Label(stage, "Version", new Vector2(-StageW * 0.5f + 90, -StageH * 0.5f + 18), new Vector2(160, 18),
                "Ver." + Application.version, 11, TextAnchor.MiddleLeft, ColTextSub);
            Shade(ver);
            var cp1 = UiFactory.Label(stage, "Copy1", new Vector2(StageW * 0.5f - 150, -StageH * 0.5f + 28), new Vector2(280, 16),
                "© 2026 BIG BONUS BLITZ", 10, TextAnchor.MiddleRight, ColTextSub);
            var cp2 = UiFactory.Label(stage, "Copy2", new Vector2(StageW * 0.5f - 150, -StageH * 0.5f + 12), new Vector2(280, 16),
                "All Rights Reserved.", 10, TextAnchor.MiddleRight, ColTextSub);
            Shade(cp1); Shade(cp2);

            BuildSettings(stage);

            // 暗転用（最前面）
            var fadeRt = UiFactory.Panel(root, "Fade", Vector2.zero, new Vector2(4000, 4000), Color.black);
            _fade = fadeRt.gameObject.AddComponent<CanvasGroup>();
            _fade.alpha = 1f;
            _fade.blocksRaycasts = false;
            StartCoroutine(FadeTo(0f, 0.6f));
        }

        /// <summary>
        /// 縦書き。uGUI の Text は縦組みを持たないので、1 文字ずつ改行して縦に積む。
        /// 短い煽り文なら、これで十分それらしく見える。
        /// </summary>
        private static Text VerticalText(Transform parent, string name, Vector2 pos, string text, int size, Color color)
        {
            const char NL = (char)10;      // 改行
            var sb = new System.Text.StringBuilder(text.Length * 2);
            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0) sb.Append(NL);
                char c = text[i];
                // 長音とダッシュは縦組みだと向きが変わる
                if (c == 'ー' || c == '―' || c == '-') c = '｜';
                sb.Append(c);
            }
            float h = size * 1.28f * text.Length + 8f;
            var t = UiFactory.Label(parent, name, pos, new Vector2(size + 12f, h), sb.ToString(), size, TextAnchor.UpperCenter, color);
            t.lineSpacing = 1.0f;
            t.raycastTarget = false;
            Shade(t);
            return t;
        }

        /// <summary>下に並べる丸ボタン（アイコン＋文字）。</summary>
        private Button Pill(Transform parent, string name, Vector2 pos, string icon, string label, System.Action onClick)
        {
            var b = UiSkin.Button(parent, name, pos, new Vector2(PillW, PillH), "",
                () => { _audio.UiPop(); onClick(); }, ColBtn, 13, false, 20);
            var t = b.GetComponentInChildren<Text>();
            if (t != null)
            {
                t.text = label;
                t.alignment = TextAnchor.MiddleCenter;
                t.rectTransform.anchoredPosition = new Vector2(11, 0);
                Shade(t);
            }
            UiSkin.Img(b.transform, "Icon", new Vector2(-PillW * 0.5f + 26, 0), new Vector2(18, 18), UiSkin.Icon(icon, 64), ColGold);
            return b;
        }

        /// <summary>一言だけ知らせる（3 秒で消える）。</summary>
        private void Notice(string text)
        {
            if (_confirm == null) return;
            _confirm.text = text;
            _confirmUntil = Time.time + 3f;
        }

        // ------------------------------------------------------------ 設定
        private void BuildSettings(Transform stage)
        {
            var overlay = UiFactory.Panel(stage, "SettingsOverlay", Vector2.zero, new Vector2(4000, 4000), new Color(0, 0, 0, 0.62f));
            var close = overlay.gameObject.AddComponent<Button>();
            close.transition = Selectable.Transition.None;
            close.onClick.AddListener(ToggleSettings);
            var card = UiSkin.Card(overlay, "Card", Vector2.zero, new Vector2(400, 210), 14);
            var eat = card.gameObject.AddComponent<Button>();      // 中を押しても閉じない
            eat.transition = Selectable.Transition.None;
            var title = UiFactory.Label(card, "Title", new Vector2(-14, 76), new Vector2(320, 24), "設定", 15, TextAnchor.MiddleLeft, ColText);
            title.fontStyle = FontStyle.Bold;
            UiSkin.Img(card, "Line", new Vector2(0, 60), new Vector2(368, 1), null, new Color(1, 1, 1, 0.08f));
            UiSkin.IconButton(card, "Close", new Vector2(178, 76), 28, "×", ToggleSettings, ColBtn, 16);

            UiFactory.Label(card, "BgmLabel", new Vector2(-140, 26), new Vector2(60, 20), "BGM", 12, TextAnchor.MiddleLeft, ColTextSub);
            var bgm = UiFactory.Slider(card, "BgmSlider", new Vector2(30, 26), new Vector2(230, 20), _audio.BgmVolume, v => { _audio.BgmVolume = v; });
            bgm.gameObject.AddComponent<SliderReleaseSound>().OnRelease = () => { _audio.UiPop(); SaveData.SaveAudio(_audio); };
            UiFactory.Label(card, "SeLabel", new Vector2(-140, -6), new Vector2(60, 20), "SE", 12, TextAnchor.MiddleLeft, ColTextSub);
            var se = UiFactory.Slider(card, "SeSlider", new Vector2(30, -6), new Vector2(230, 20), _audio.SeVolume, v => { _audio.SeVolume = v; });
            se.gameObject.AddComponent<SliderReleaseSound>().OnRelease = () => { _audio.UiPop(); SaveData.SaveAudio(_audio); };
            UiSkin.Button(card, "BgmToggle", new Vector2(0, -52), new Vector2(200, 32), "BGM ON / OFF",
                () => { _audio.ToggleBgm(); _audio.UiPop(); SaveData.SaveAudio(_audio); }, ColBtn, 12, false, 8);

            _settingsBox = overlay.gameObject;
            _settingsBox.SetActive(false);
        }

        private void ToggleSettings()
        {
            if (_settingsBox == null) return;
            _settingsBox.SetActive(!_settingsBox.activeSelf);
            _audio.UiPop();
        }

        /// <summary>タイトル絵の板に 1 枚重ねる。板いっぱいに広げ、指定の動きを付ける。</summary>
        private static void AddLayer(RectTransform art, string name, Sprite sprite,
                                     TitleCharacterWarp.Mode mode)
        {
            if (sprite == null) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(art, false);
            UiSkin.Stretch(rt);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            go.AddComponent<TitleCharacterWarp>().mode = mode;
        }

        /// <summary>明るい絵の上でも読めるよう、文字に影を付ける。</summary>
        private static void Shade(Text t)
        {
            if (t == null) return;
            var sh = t.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.85f);
            sh.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private Button MakeButton(Transform parent, string name, Vector2 pos, string text, System.Action onClick, Color color)
        {
            return UiSkin.Button(parent, name, pos, new Vector2(280, 52), text, () => { _audio.UiPop(); onClick(); }, color, 18, false, 14);
        }

        // --------------------------------------------------------------- LOOP
        private void Update()
        {
            _t += Time.deltaTime;
            if (_press != null && !_starting)
            {
                var c = _press.color;
                c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(_t * 2.2f));
                _press.color = c;
            }
            if (_confirm != null && _confirm.text.Length > 0 && Time.time > _confirmUntil) _confirm.text = "";

            if (_starting || (_settingsBox != null && _settingsBox.activeSelf)) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                _audio.UiPop();
                Begin(false);
            }
        }

        private void OnNewGame()
        {
            if (!_hasSave) { Begin(true); return; }
            // セーブがあるときは2度押しで確定
            if (Time.time <= _confirmUntil) { Begin(true); return; }
            _confirmUntil = Time.time + 3f;
            _confirm.text = "セーブデータを消して最初から始めます。もう一度押すと確定";
        }

        /// <summary>スタート。clearSave=true でセーブを消してから開始。</summary>
        private void Begin(bool clearSave)
        {
            if (_starting) return;
            _starting = true;
            if (_btnContinue != null) _btnContinue.interactable = false;
            if (_btnNew != null) _btnNew.interactable = false;
            _press.text = "S T A R T !";
            _press.color = ColGold;
            StartCoroutine(BeginRoutine(clearSave));
        }

        private IEnumerator BeginRoutine(bool clearSave)
        {
            _fade.blocksRaycasts = true;
            yield return FadeTo(1f, 0.45f);
            if (clearSave) SaveData.Clear();
            // UI だけ片付ける（AudioManager は画面をまたいで使い回すので消さない）
            Destroy(_canvas.gameObject);
            MapScreen.Open();   // まずミニマップ。街と冒険をここで選ぶ
            // ゲーム側の Canvas より前に黒幕を出して明転
            var go = new GameObject("TitleFadeOut", typeof(Canvas), typeof(CanvasGroup));
            var cv = go.GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 1000;
            var img = new GameObject("Black", typeof(RectTransform), typeof(Image));
            var rt = img.GetComponent<RectTransform>();
            rt.SetParent(go.transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            img.GetComponent<Image>().color = Color.black;
            img.GetComponent<Image>().raycastTarget = false;
            _fade = go.GetComponent<CanvasGroup>();
            _fade.alpha = 1f;
            _fade.blocksRaycasts = false;
            yield return null;
            yield return FadeTo(0f, 0.5f);
            Destroy(go);
            Destroy(gameObject);
        }

        private IEnumerator FadeTo(float target, float sec)
        {
            float from = _fade.alpha, t = 0f;
            while (t < sec)
            {
                t += Time.deltaTime;
                _fade.alpha = Mathf.Lerp(from, target, t / sec);
                yield return null;
            }
            _fade.alpha = target;
        }
    }
}
