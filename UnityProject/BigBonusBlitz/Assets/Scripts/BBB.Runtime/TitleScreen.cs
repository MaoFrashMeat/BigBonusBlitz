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
        private const float TitleX = -236f;

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
            var titleSprite = ArtLoader.Sprite("Art/UI/title_bg");
            if (titleSprite != null)
            {
                var artGo = new GameObject("TitleArt", typeof(RectTransform), typeof(Image));
                var artRt = artGo.GetComponent<RectTransform>();
                artRt.SetParent(bgArea, false);
                artRt.anchorMin = artRt.anchorMax = new Vector2(0.5f, 0.5f);
                var img = artGo.GetComponent<Image>();
                img.sprite = titleSprite;
                img.raycastTarget = false;
                var r = titleSprite.rect;
                AspectCover.Attach(artRt, r.height > 0 ? r.width / r.height : 16f / 9f);
            }
            else
            {
                // 絵が無いときはこれまでどおりパララックスで見せる
                var bg = ParallaxBackground.Create(bgArea);
                bg.IsWalking = true;
            }
            // 左半分を暗くしてロゴと文字を読みやすく（§9.2）。人物は右にいるので触らない
            var scrim = UiSkin.Img(root, "Scrim", Vector2.zero, Vector2.zero, UiSkin.GradientH(false), new Color(0, 0, 0, 0.62f));
            UiSkin.Stretch(scrim.rectTransform);
            scrim.raycastTarget = false;
            UiSkin.Vignette(root, "DimBottom", Vector2.zero, Vector2.zero, 0.5f, false);
            UiSkin.Stretch(root.Find("DimBottom").GetComponent<RectTransform>());

            // セーフエリアに収まる舞台（960×540、縮小のみ）
            _safe = SafeStage.Create(_canvas);
            var stage = _safe.Stage;

            // ロゴ
            var logoSprite = ArtLoader.Sprite("Art/UI/bbb_logo_casual_pop");
            var logoGo = new GameObject("Logo", typeof(RectTransform), typeof(Image));
            _logo = logoGo.GetComponent<RectTransform>();
            _logo.SetParent(stage, false);
            _logo.anchorMin = _logo.anchorMax = new Vector2(0.5f, 0.5f);
            _logo.sizeDelta = new Vector2(300, 300);
            _logo.anchoredPosition = new Vector2(TitleX, 96);
            var logoImg = logoGo.GetComponent<Image>();
            logoImg.sprite = logoSprite;
            logoImg.preserveAspect = true;
            logoImg.raycastTarget = false;
            if (logoSprite == null)
            {
                logoImg.enabled = false;
                UiFactory.Label(stage, "TitleText", new Vector2(TitleX, 96), new Vector2(420, 80), "BIG BONUS BLITZ", 40, TextAnchor.MiddleCenter, ColGold).fontStyle = FontStyle.Bold;
            }

            // ボタン
            float by = -92f;
            if (_hasSave)
            {
                _btnContinue = MakeButton(stage, "Continue", new Vector2(TitleX, by), "つづきから", () => Begin(false), ColAccent);
                by -= 62f;
            }
            _btnNew = MakeButton(stage, "NewGame", new Vector2(TitleX, by), "はじめから", OnNewGame, _hasSave ? ColBtn : ColAccent);
            _confirm = UiFactory.Label(stage, "Confirm", new Vector2(TitleX, by - 32), new Vector2(420, 24), "", 13, TextAnchor.MiddleCenter, ColGold);

            // PRESS SPACE
            _press = UiFactory.Label(stage, "Press", new Vector2(TitleX, -StageH * 0.5f + 62), new Vector2(420, 28),
                _hasSave ? "SPACE / ENTER でつづきから" : "SPACE / ENTER でスタート", 15, TextAnchor.MiddleCenter, ColText);

            // フッター
            UiFactory.Label(stage, "Version", new Vector2(StageW * 0.5f - 90, -StageH * 0.5f + 16), new Vector2(170, 20),
                "v" + Application.version, 11, TextAnchor.MiddleRight, ColTextSub);
            UiFactory.Label(stage, "Copy", new Vector2(-StageW * 0.5f + 120, -StageH * 0.5f + 16), new Vector2(230, 20),
                "BIG BONUS BLITZ", 11, TextAnchor.MiddleLeft, ColTextSub);

            // 暗転用（最前面）
            var fadeRt = UiFactory.Panel(root, "Fade", Vector2.zero, new Vector2(4000, 4000), Color.black);
            _fade = fadeRt.gameObject.AddComponent<CanvasGroup>();
            _fade.alpha = 1f;
            _fade.blocksRaycasts = false;
            StartCoroutine(FadeTo(0f, 0.6f));
        }

        private Button MakeButton(Transform parent, string name, Vector2 pos, string text, System.Action onClick, Color color)
        {
            return UiSkin.Button(parent, name, pos, new Vector2(280, 52), text, () => { _audio.UiPop(); onClick(); }, color, 18, false, 14);
        }

        // --------------------------------------------------------------- LOOP
        private void Update()
        {
            _t += Time.deltaTime;
            if (_logo != null) _logo.anchoredPosition = new Vector2(0, 92 + Mathf.Sin(_t * 1.6f) * 6f);
            if (_press != null && !_starting)
            {
                var c = _press.color;
                c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(_t * 2.2f));
                _press.color = c;
            }
            if (_confirm != null && _confirm.text.Length > 0 && Time.time > _confirmUntil) _confirm.text = "";

            if (_starting) return;
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
            _press.text = "START!";
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
