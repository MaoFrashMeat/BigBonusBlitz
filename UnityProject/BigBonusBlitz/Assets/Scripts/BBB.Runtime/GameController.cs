using System.Collections;
using BBB.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// ブラウザ版 main.js のゲーム進行（BET → レバー → 停止 → 判定 → 次G）を Unity 上で回す。
    /// UI・演出は全部コードで生成。キャラは Chr0001（仮）、敵3種、背景6層、Web版の音源。
    /// 操作: Ctrl=BET+レバー / Z,X,C or ←↓→=停止 / Space=BET→1→2→3 を順送り / A=オート
    ///       F1〜F6=設定変更 / B=BGM / R=セーブ削除して再開 / +(テンキー) or ;=エンバー100追加
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        /// <summary>ゲーム本体を起動する（TitleScreen から呼ぶ）。起動は TitleScreen.Bootstrap が入口。</summary>
        public static GameController Launch()
        {
            var exist = FindFirstObjectByType<GameController>();
            if (exist != null) return exist;
            return new GameObject("GameController").AddComponent<GameController>();
        }

        // Web版 #game-container 1280x720 → 0.75倍で 960x540 に収める
        private const float StageW = 960f, StageH = 540f;
        private const float AreaW = 940f;          // キャラクター表示域（StageCard 内）
        private const float AreaH = 220f;

        private SlotMachine _m;
        private AudioManager _audio;
        private ReelView[] _reels;
        private RectTransform _stage, _area;
        private ParallaxBackground _bg;

        private Text _credit, _payout, _message, _bonus, _mode, _debug, _player, _tier2, _hint, _gCount;
        private GameObject _tier2Box;
        private Image _expFill;
        private Button _btnBet, _btnAuto;
        private Slider _bgmSlider, _seSlider;
        private Button[] _btnStops;

        private RectTransform _charRt;
        private SpriteAnimator _charAnim;
        private Sprite[] _idleFrames, _walkFrames, _attackFrames;
        private Sprite[] _slashFrames, _castFrames, _guardFrames, _hitFrames, _victoryFrames, _focusFrames;
        private string _charState = "";        // 今流している動き。同じものを再指定しても頭出しし直さない
        private float _charHoldUntil;           // 一度きりの動き（斬る・被弾など）を守る時刻

        private RectTransform _enemyRt;
        private Image _enemyImg;
        private CanvasGroup _enemyCg;
        private Coroutine _enemyIdle;
        private Coroutine _enemyRainbow;
        /// <summary>敵表示の基準サイズ。テーブルの scale をこれに掛ける。</summary>
        private static readonly Vector2 EnemyBaseSize = new Vector2(150, 150);
        /// <summary>敵の基本色（中ボスは色付き）。示唆の色が乗っていないときはこれに戻す。</summary>
        private Color _enemyBaseColor = Color.white;
        private Image _darken;            // 前兆: 画面を段階的に暗くする
        private RectTransform _shadowRt;  // 前兆: 敵のシルエット
        private Image _shadowImg;
        private Coroutine _precursorRoutine;
        private TravelerView _traveler;
        private readonly SystemRandom _fxRng = new SystemRandom();
        private GameObject _naviBox;
        private Text _techBanner;      // 課題の内容（リール帯の上）
        private Text[] _naviLabels = new Text[3];
        private readonly RectTransform[] _naviCells = new RectTransform[3];
        /// <summary>そのリールのバッジを押して消したか（1G ごとにリセット）。</summary>
        private readonly bool[] _naviPopped = new bool[3];
        private Image[] _naviBg = new Image[3];
        private Image[] _naviGlow = new Image[3];
        // 会話 UI（旅人 ⇄ 主人公）
        private GameObject _dialogBox;
        private Text _dialogName, _dialogText;
        private Image _dialogNameBg;
        private Coroutine _dialogRoutine;
        // 役演出: 揃ったコマを光らせる（3リール × 3段 = 9セル。index = reel * 3 + row, row 0=上段）
        private CanvasGroup _paylineFlash;
        private readonly Image[] _cellFx = new Image[9];
        private Coroutine _paylineRoutine;
        private const int AllCells = 0x1FF;
        // ボーナス中の枠（AT 期待度をランクの色で点滅させる）
        private CanvasGroup _atFrameCg;
        private Image[] _atFrame;
        private AtRank _atRank;
        // ボーナス中の帯テロップ（AT のチャンスを煽る）
        private GameObject _tickerBox;
        private Text _tickerText;
        private Image _tickerLineTop, _tickerLineBottom, _tickerBg;
        private RectTransform _tickerRt, _tickerView;
        private string _tickerRank = "";
        private int _tickerIndex;
        private float _fxTimer;
        // ボーナス中の 15 枚ベル演出（3体斬り）
        private bool _bellShowActive;
        private int _bellShowStop;
        private readonly RectTransform[] _bellFoe = new RectTransform[3];
        private readonly Image[] _bellFoeImg = new Image[3];
        private readonly CanvasGroup[] _bellFoeCg = new CanvasGroup[3];
        private bool _pushPressed;
        // AT「洞窟」
        private Text _atChip;
        private Text _soulText;
        private Image _atChipBg, _caveTint;
        private GameObject _atChipBox, _hpBox;
        private Image _hpFill;
        private RectTransform _hpTrack;
        private Text _hpLabel;
        private bool _hotStopSound;
        private int _stopCountThisGame;
        private RectTransform _dustRt;
        private RectTransform _cutinRt;
        private CanvasGroup _cutinCg;
        private Image _redGlow;

        private bool _autoMode;
        /// <summary>オートの速さ（1〜6）。待ち時間をこの値で割る。</summary>
        private int _autoSpeed = 1;
        /// <summary>ボタンで選んだ速さ。スペース長押しのオートが終わったらここに戻す。</summary>
        private int _autoSpeedPref = 1;
        /// <summary>スペース長押し中の一時オート（常に x1）。</summary>
        private bool _holdAuto;
        private float _spaceHold;
        private const int AutoSpeedMax = 6;
        // スランプグラフ
        private SlumpGraph _graph;
        private GameObject _graphBox;
        /// <summary>右パネルに常駐する小型のスランプ（設定で出し入れ）。</summary>
        private SlumpGraph _mini;
        private RectTransform _miniBox;
        private Text _miniLabel;
        private Button _graphAlwaysBtn;
        /// <summary>グラフに重ねている履歴の番号（-1 なら重ねていない）。</summary>
        private int _histPicked = -1;
        private Button[] _histRows;
        private Text[] _histLabels;
        /// <summary>この潜行を始めたときのエンバー。履歴の差枚を出すのに使う。</summary>
        private int _runBaseCredit;
        /// <summary>潜行の終わりかた（力尽き / 章クリア / 自分で戻る）。街へ戻るときに記録する。</summary>
        private string _runEndReason = "";
        private bool _runRecorded;
        // 冒険マップ（ステージ札とモーダル）
        private Text _stageTag;
        private Image _stageTagBg, _stageTagEdge;
        // ステージのG数が止まっている間の錠前表示
        private GameObject _holdBox;
        private Text _holdText, _holdReason;
        private Image _holdLock;
        private readonly System.Collections.Generic.List<Image> _holdLinks = new System.Collections.Generic.List<Image>();
        // 次のルートの達成条件（表示域の左）
        private GameObject _routeBox;
        private Text _routeTitle;
        private readonly Text[] _routeArrow = new Text[3];
        private readonly Text[] _routeText = new Text[3];
        private readonly Image[] _routeBar = new Image[3];
        private GameObject _mapBox;
        private GameObject _equipBox, _curseBox;
        private RectTransform _mapBody, _mapView, _condList;
        private Text _mapInfo;
        private bool _leaving;   // 章クリア・帰還で街へ戻る途中
        private Text _torchTag;
        private Image _torchFill;
        private RectTransform _torchTrack, _torchTagRt;
        private Coroutine _autoRoutine;
        private float _messageFlashUntil;
        private int _lastPayout;
        private bool _lastWasReplay;
        private bool _inputLocked;     // BB確定音の間
        private float _fps;
        /// <summary>実機のウェイト。前回リール始動から次の始動まで最低この秒数空ける。</summary>
        public const float SpinWaitSeconds = 2.05f;   // 実機は4.1秒。半分に短縮（2026-09-08）
        private float _lastSpinStartTime = -100f;
        private Coroutine _waitRoutine;
        private float _stopUnlockTime;
        /// <summary>ウェイト中に演出を差し込むためのフック（残り秒）。今後ここにワクワク演出を足す。</summary>
        public System.Action<float> OnWaitStart;

        private void Start()
        {
            // 高フレームレート（VSync を切って 120fps 上限）。モニタが 60Hz なら 60 で頭打ち
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 120;
            _audio = AudioManager.Create();
            _m = GameDataLoader.CreateMachine(new SystemRandom());
            if (!SaveData.Load(_m, _audio)) SaveData.LoadAudio(_audio);   // セーブが無くても音量設定は引き継ぐ
            _autoSpeedPref = SaveData.LoadAutoSpeed();
            BuildUi();
            RefreshUi();
            SetMessage((_m.HeldBonusFlag != Flag.HAZE && _m.BonusAnnounceRemaining <= 0) ? "ボーナス成立中  ―  揃えてください" : "Ctrl または Space で BET");
            _audio.StartBgm();
            PlayCharacter("walk");
            // 章の頭に着いたところなら、導入を流す（周回なら「また来たのか」を足す）
            if (_m.AdventureEnabled && _m.Adv.nodeId == _m.Config.adventure.start && _m.Adv.visited.Count <= 1)
            {
                var lines = new System.Collections.Generic.List<StoryLine>();
                var op = StoryDirector.Opening(_m.Config.story, _m.Adv.chapter);
                if (op != null) lines.AddRange(op);
                var lap = StoryDirector.Lap(_m.Config.story, _m.Adv.chapter, _m.Adv.chapter);
                if (lap != null) lines.AddRange(lap);
                PlayStory(lines);
            }
        }

        private void OnApplicationQuit() => SaveData.Save(_m, _audio);

        // ------------------------------------------------------------------ UI
        // 配色は UiSkin に一元化（自分=緑 / 敵・危険=赤 / 中立=金 / 選択=アクセント1色）
        static readonly Color ColBg = UiSkin.Bg;
        static readonly Color ColPanel = UiSkin.Panel;
        static readonly Color ColPanelEdge = UiSkin.PanelEdge;
        static readonly Color ColText = UiSkin.Text;
        static readonly Color ColTextSub = UiSkin.TextSub;
        static readonly Color ColAccent = UiSkin.Accent;
        static readonly Color ColGold = UiSkin.Gold;
        static readonly Color ColGreen = UiSkin.Green;
        static readonly Color ColBtn = UiSkin.Btn;
        static readonly Color ColBtnDisabled = UiSkin.BtnDisabled;

        // 舞台の寸法（960×540 の仮想 16:9 枠。8px グリッド。スマホ横持ちでは枠ごと縮小し、外側は背景だけ）
        private const float Margin = 8f;
        private const float ContentW = StageW - Margin * 2;                                  // 944
        private const float StageCardH = 252f;                                                // 上段: 帯 28 + 表示域 220
        private const float BandH = 28f;
        private const float MidH = 196f;                                                      // 中段: 表示器 / リール / ステータス
        private const float CtrlH = 56f;                                                      // 下段: ボタン（タッチ最小 44pt 相当）
        /// <summary>左右のパネルの幅。リールを細くしたぶん、ここへ回している
        /// （SideW*2 + 筐体 = 928 で、ContentW 944 に対して左右 8px。以前と同じ詰まり具合）。</summary>
        private const float SideW = 242f;
        private const float StageCardY = StageH * 0.5f - Margin - StageCardH * 0.5f;         // 136
        private const float MidY = StageCardY - StageCardH * 0.5f - Margin - MidH * 0.5f;    // -100
        private const float CtrlY = MidY - MidH * 0.5f - Margin - CtrlH * 0.5f;              // -234
        private const float AreaY = -15f;                                                     // 表示域の StageCard 内位置
        private const float EnemyX = AreaW * 0.5f - 0.2f * AreaW - 75f;
        private const float EnemyY = -AreaH * 0.5f + 12f + 75f;
        private const float GroundY = -AreaH * 0.5f + 12f + 55f;                              // 旅人の足元

        private SafeStage _safe;
        private ParallaxBackground _bgOuter;
        private GameObject _settingsBox, _debugBox;
        private Image _bonusFill;
        private RectTransform _bonusTrack, _expTrack;
        private Text _creditNum, _payoutNum, _bonusLabel, _status;
        private Text _modeChip;
        private Image _modeChipBg;
        private Text _resetConfirm;
        private float _resetConfirmUntil;
        private bool _isTouch;

        private static Color Hex(string h) => UiSkin.Hex(h);

        /// <summary>ボタン下部の小さな操作ヒント（キー表記 / タッチ表記）。game-design §6.2「ボタン表記を必ず添える」。</summary>
        private static void AddSubHint(Button b, string text)
        {
            var label = b.GetComponentInChildren<Text>();
            var rt = b.GetComponent<RectTransform>();
            label.rectTransform.anchoredPosition = new Vector2(0, 5);
            UiFactory.Label(b.transform, "Sub", new Vector2(0, -rt.sizeDelta.y * 0.5f + 11), new Vector2(rt.sizeDelta.x - 8, 12), text, 10, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.45f));
        }

        private static void SkinSlider(Slider s)
        {
            var bg = s.transform.Find("Background")?.GetComponent<Image>();
            if (bg != null) { bg.sprite = UiSkin.Rounded(3); bg.type = Image.Type.Sliced; bg.color = new Color(0, 0, 0, 0.55f); }
            var fill = s.fillRect != null ? s.fillRect.GetComponent<Image>() : null;
            if (fill != null) { fill.sprite = UiSkin.Rounded(3); fill.type = Image.Type.Sliced; fill.color = ColGold; }
            var handle = s.handleRect != null ? s.handleRect.GetComponent<Image>() : null;
            if (handle != null) { handle.sprite = UiSkin.Rounded(6); handle.type = Image.Type.Sliced; handle.color = ColText; handle.rectTransform.sizeDelta = new Vector2(14, 0); }
        }

        /// <summary>文字に影を付けて背景から独立させる（game-design §9.2）。</summary>
        private static void TextShadow(Text t, float strength = 0.8f)
        {
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, strength);
            sh.effectDistance = new Vector2(1, -2);
        }

        /// <summary>モーダル: 暗幕（タップで閉じる）＋中央カード＋×。返すのは暗幕。</summary>
        private GameObject BuildModal(string name, Vector2 size, string title, System.Action onClose, out RectTransform body)
        {
            var overlay = UiFactory.Panel(_stage, name + "Overlay", Vector2.zero, new Vector2(4000, 4000), new Color(0, 0, 0, 0.62f));
            var closeBtn = overlay.gameObject.AddComponent<Button>();
            closeBtn.transition = Selectable.Transition.None;
            closeBtn.onClick.AddListener(() => onClose());
            var card = UiSkin.Card(overlay, "Card", Vector2.zero, size, 14);
            var eat = card.gameObject.AddComponent<Button>();        // カード内のタップは閉じない
            eat.transition = Selectable.Transition.None;
            var t = UiFactory.Label(card, "Title", new Vector2(-14, size.y * 0.5f - 22), new Vector2(size.x - 80, 24), title, 15, TextAnchor.MiddleLeft, ColText);
            t.fontStyle = FontStyle.Bold;
            UiSkin.Img(card, "TitleLine", new Vector2(0, size.y * 0.5f - 38), new Vector2(size.x - 32, 1), null, new Color(1, 1, 1, 0.08f));
            UiSkin.IconButton(card, "Close", new Vector2(size.x * 0.5f - 22, size.y * 0.5f - 22), 28, "×", onClose, ColBtn, 16);
            body = card;
            return overlay.gameObject;
        }

        private void BuildUi()
        {
            _isTouch = Application.isMobilePlatform;
            UiFactory.EnsureEventSystem();
            var canvas = UiFactory.CreateCanvas();
            var root = canvas.transform;

            // ===== 画面全体の背景（舞台の外側にも世界を見せる。19.5:9 の余白対策 §3.2）=====
            var bgFull = UiSkin.Img(root, "BgFull", Vector2.zero, Vector2.zero, null, ColBg);
            UiSkin.Stretch(bgFull.rectTransform);
            var outer = UiSkin.Rect(root, "BgOuter", Vector2.zero, new Vector2(1920, StageH));
            outer.gameObject.AddComponent<RectMask2D>();
            _bgOuter = ParallaxBackground.Create(outer);
            var outerDim = UiSkin.Img(root, "BgOuterDim", Vector2.zero, Vector2.zero, null, new Color(0.02f, 0.03f, 0.06f, 0.72f));
            UiSkin.Stretch(outerDim.rectTransform);
            var outerVig = UiSkin.Vignette(root, "BgOuterVig", Vector2.zero, Vector2.zero, 0.5f, false);
            UiSkin.Stretch(outerVig.rectTransform);

            // ===== セーフエリア → 舞台（960×540、収まらなければ縮小）=====
            _safe = SafeStage.Create(canvas);
            _stage = _safe.Stage;

            // ===== 上段: ステージカード（状態の帯 + キャラクター表示域）=====
            var stageCard = UiSkin.Card(_stage, "StageCard", new Vector2(0, StageCardY), new Vector2(ContentW, StageCardH), 14, UiSkin.PanelHi, true, true, false);

            // 表示域（角丸マスク）。タップで BET / 順送り停止（片手プレイ用）
            _area = UiSkin.Rect(stageCard, "CharacterArea", new Vector2(0, AreaY), new Vector2(AreaW, AreaH));
            var areaImg = _area.gameObject.AddComponent<Image>();
            areaImg.sprite = UiSkin.Rounded(12); areaImg.type = Image.Type.Sliced; areaImg.color = Hex("#111111"); areaImg.raycastTarget = true;
            var areaMask = _area.gameObject.AddComponent<Mask>();
            areaMask.showMaskGraphic = true;
            var areaBtn = _area.gameObject.AddComponent<Button>();
            areaBtn.transition = Selectable.Transition.None;
            areaBtn.onClick.AddListener(OnSpaceStep);
            _bg = ParallaxBackground.Create(_area);

            // キャラ（左 15%、足元 6px）
            // ポポラ（Art/Hero）。無ければ旧素材（Art/Characters）に戻す
            _idleFrames = ArtLoader.Strip("Art/Hero/hero_idle", 4);
            if (_idleFrames.Length > 0)
            {
                _walkFrames = ArtLoader.Strip("Art/Hero/hero_walk", 6);
                _attackFrames = ArtLoader.Strip("Art/Hero/hero_attack", 4);
                _slashFrames = ArtLoader.Strip("Art/Hero/hero_slash", 4);
                _castFrames = ArtLoader.Strip("Art/Hero/hero_cast", 4);
                _guardFrames = ArtLoader.Strip("Art/Hero/hero_guard", 2);
                _hitFrames = ArtLoader.Strip("Art/Hero/hero_hit", 2);
                _victoryFrames = ArtLoader.Strip("Art/Hero/hero_victory", 4);
                _focusFrames = ArtLoader.Strip("Art/Hero/hero_focus", 4);
            }
            else
            {
                _idleFrames = ArtLoader.Strip("Art/Characters/chr0001_idle_strip", 4);
                _walkFrames = ArtLoader.Strip("Art/Characters/popora_walk_strip_25f", 25);
                _attackFrames = ArtLoader.Strip("Art/Characters/chr0001_attack_strip", 4);
                _slashFrames = _castFrames = _guardFrames = _hitFrames = _victoryFrames = _focusFrames = _attackFrames;
            }
            // どれかが読めなかったら、読めたもので代用する（真っ白なキャラにしない）
            if (_walkFrames.Length == 0) _walkFrames = _idleFrames.Length > 0 ? _idleFrames : _attackFrames;
            if (_idleFrames.Length == 0) _idleFrames = _walkFrames.Length > 0 ? _walkFrames : _attackFrames;
            if (_slashFrames.Length == 0) _slashFrames = _attackFrames;
            if (_castFrames.Length == 0) _castFrames = _attackFrames;
            if (_guardFrames.Length == 0) _guardFrames = _idleFrames;
            if (_hitFrames.Length == 0) _hitFrames = _idleFrames;
            if (_victoryFrames.Length == 0) _victoryFrames = _attackFrames;
            if (_focusFrames.Length == 0) _focusFrames = _idleFrames;
            if (_idleFrames.Length == 0) Debug.LogWarning("キャラの画像が読み込めていません（Resources/Art/Hero か Art/Characters を確認）");
            const float charSize = 206f;
            _charRt = MakeImage(_area, "Character", new Vector2(-AreaW * 0.5f + 0.15f * AreaW + charSize * 0.5f, -AreaH * 0.5f + 6f + charSize * 0.5f), new Vector2(charSize, charSize), null);
            _charRt.GetComponent<Image>().color = Color.white;
            _charAnim = _charRt.gameObject.AddComponent<SpriteAnimator>();

            // 前兆: 暗幕（背景の上・キャラの下）とシルエット
            _darken = UiFactory.Panel(_area, "Darken", Vector2.zero, new Vector2(AreaW, AreaH), new Color(0, 0, 0, 0)).GetComponent<Image>();
            _darken.raycastTarget = false;
            _shadowRt = MakeImage(_area, "EnemyShadow", new Vector2(AreaW * 0.5f + 90f, EnemyY), new Vector2(150, 150), null);
            _shadowImg = _shadowRt.GetComponent<Image>();
            _shadowImg.color = new Color(0, 0, 0, 0);

            // 敵（右 20%）
            _enemyRt = MakeImage(_area, "Enemy", new Vector2(EnemyX, EnemyY), new Vector2(150, 150), null);
            _enemyImg = _enemyRt.GetComponent<Image>();
            _enemyCg = _enemyRt.gameObject.AddComponent<CanvasGroup>();
            _enemyCg.alpha = 0f;
            _dustRt = MakeImage(_area, "Dust", _enemyRt.anchoredPosition, new Vector2(180, 180), ArtLoader.Sprite("Art/UI/dust_cloud"));
            _dustRt.gameObject.SetActive(false);

            // 洞窟の色被せ（AT 中だけ。背景の上・キャラの下）
            _caveTint = UiSkin.Img(_area, "CaveTint", Vector2.zero, new Vector2(AreaW, AreaH), null, new Color(0.06f, 0.04f, 0.16f, 0f));
            _caveTint.transform.SetSiblingIndex(_darken.transform.GetSiblingIndex());

            // HUD の下地: 上下の暗いグラデ（文字を背景から独立させる §9.2）
            UiSkin.Vignette(_area, "VigTop", new Vector2(0, AreaH * 0.5f - 36), new Vector2(AreaW, 72), 0.45f, true);
            UiSkin.Vignette(_area, "VigBottom", new Vector2(0, -AreaH * 0.5f + 18), new Vector2(AreaW, 36), 0.25f, false);

            // バトル（狩猟）: モンスター名 + HP バー + 残りG。敵の頭上に出す
            var hp = UiSkin.Rect(_area, "BattleHp", new Vector2(EnemyX, EnemyY + 106), new Vector2(230, 46));
            UiSkin.Img(hp, "Bg", Vector2.zero, new Vector2(230, 46), UiSkin.Rounded(10), new Color(0.03f, 0.04f, 0.08f, 0.85f));
            _hpLabel = UiFactory.Label(hp, "Name", new Vector2(0, 12), new Vector2(214, 18), "", 12, TextAnchor.MiddleLeft, ColText);
            _hpLabel.fontStyle = FontStyle.Bold;
            _hpFill = UiSkin.Gauge(hp, "Hp", new Vector2(0, -10), new Vector2(206, 10), ColAccent, out _hpTrack);
            _hpBox = hp.gameObject;
            _hpBox.SetActive(false);

            // 示唆セリフ（中央上。影付き）
            // ステージ札（左上の隅。§3: 見失っても死なない情報は隅に固定）。タップでマップ
            const float tagW = 330f, tagH = 30f;
            float tagTop = AreaY + AreaH * 0.5f - 6 - tagH * 0.5f;   // 表示域の上端に沿わせる
            var tagRt = UiSkin.Rect(stageCard, "StageTag", new Vector2(-AreaW * 0.5f + 10 + tagW * 0.5f, tagTop), new Vector2(tagW, tagH));
            UiSkin.Img(tagRt, "Shadow", new Vector2(0, -3), new Vector2(tagW + 18, tagH + 16), UiSkin.Shadow(15, 10), new Color(0, 0, 0, 0.7f));
            var stagePill = UiSkin.Frame("pill_navy_sm");
            if (stagePill != null) _stageTagBg = UiSkin.Img(tagRt, "Bg", Vector2.zero, new Vector2(tagW, tagH), stagePill, Color.white, true);
            else
            {
                UiSkin.Img(tagRt, "Edge2", Vector2.zero, new Vector2(tagW + 2, tagH + 2), UiSkin.Rounded(16), new Color(1, 1, 1, 0.14f));
                _stageTagBg = UiSkin.Img(tagRt, "Bg", Vector2.zero, new Vector2(tagW, tagH), UiSkin.Rounded(15), Hex("#0b1120"), true);
            }
            _stageTagEdge = UiSkin.Img(tagRt, "Edge", new Vector2(-tagW * 0.5f + 7, 0), new Vector2(5, 20), UiSkin.Rounded(2), ColGold);
            _stageTag = UiFactory.Label(tagRt, "Text", new Vector2(9, 0), new Vector2(tagW - 28, tagH), "", 14, TextAnchor.MiddleLeft, ColText);
            _stageTag.fontStyle = FontStyle.Bold;
            TextShadow(_stageTag, 1f);
            var tagBtn = _stageTagBg.gameObject.AddComponent<Button>();
            tagBtn.transition = Selectable.Transition.None;
            tagBtn.onClick.AddListener(ToggleMap);
            tagRt.gameObject.SetActive(_m.AdventureEnabled);

            // ===== HOLD: ステージのG数が止まっている間、札を鎖と南京錠で封じる =====
            var holdRt = UiSkin.Rect(tagRt, "Hold", Vector2.zero, new Vector2(tagW, tagH));
            UiSkin.Img(holdRt, "Dim", Vector2.zero, new Vector2(tagW, tagH), UiSkin.Rounded(15), new Color(0.03f, 0.04f, 0.07f, 0.93f));
            // 鎖を横に通す（ひとこまずつ並べる）
            const float linkW = 30f, linkStep = 23f;
            int links = Mathf.CeilToInt((tagW - 8f) / linkStep);
            for (int i = 0; i < links; i++)
            {
                var lk = UiSkin.Img(holdRt, "Link" + i, new Vector2(-tagW * 0.5f + 6 + i * linkStep + linkW * 0.5f, 0),
                                    new Vector2(linkW, 19), UiSkin.Icon("chain", 48), new Color(0.74f, 0.78f, 0.86f, 0.95f));
                _holdLinks.Add(lk);
            }
            UiSkin.Img(holdRt, "ChainShade", new Vector2(0, -6), new Vector2(tagW, 8), null, new Color(0, 0, 0, 0.28f));
            // 南京錠（左寄せ。鎖の上に載る）
            _holdLock = UiSkin.Img(holdRt, "Lock", new Vector2(-tagW * 0.5f + 26, 1), new Vector2(30, 30), UiSkin.Icon("lock", 96), Color.white);
            _holdText = UiFactory.Label(holdRt, "Text", new Vector2(14, 1), new Vector2(tagW - 80, tagH), "H O L D", 16, TextAnchor.MiddleCenter, ColGold);
            _holdText.fontStyle = FontStyle.Bold;
            TextShadow(_holdText, 1f);
            _holdReason = UiFactory.Label(holdRt, "Reason", new Vector2(tagW * 0.5f - 36, 1), new Vector2(64, tagH), "", 10, TextAnchor.MiddleRight, UiSkin.TextSub);
            _holdBox = holdRt.gameObject;
            _holdBox.SetActive(false);

            // ライフの札（右上）。1G で 1 減るバー
            const float torchW = 232f, torchH = 34f;
            _torchTagRt = UiSkin.Rect(stageCard, "TorchTag", new Vector2(AreaW * 0.5f - 10 - torchW * 0.5f, AreaY + AreaH * 0.5f - 6 - torchH * 0.5f), new Vector2(torchW, torchH));
            UiSkin.Img(_torchTagRt, "Shadow", new Vector2(0, -3), new Vector2(torchW + 18, torchH + 16), UiSkin.Shadow(17, 10), new Color(0, 0, 0, 0.7f));
            var lifePill = UiSkin.Frame("pill_navy_sm");
            if (lifePill != null) UiSkin.Img(_torchTagRt, "Bg", Vector2.zero, new Vector2(torchW, torchH), lifePill, Color.white);
            else
            {
                UiSkin.Img(_torchTagRt, "Edge2", Vector2.zero, new Vector2(torchW + 2, torchH + 2), UiSkin.Rounded(18), new Color(1, 1, 1, 0.14f));
                UiSkin.Img(_torchTagRt, "Bg", Vector2.zero, new Vector2(torchW, torchH), UiSkin.Rounded(17), Hex("#0b1120"));
            }
            UiSkin.Img(_torchTagRt, "Edge", new Vector2(-torchW * 0.5f + 7, 0), new Vector2(5, 22), UiSkin.Rounded(2), Hex("#7ee0a0"));
            _torchTag = UiFactory.Label(_torchTagRt, "Text", new Vector2(9, 5), new Vector2(torchW - 28, 18), "", 13, TextAnchor.MiddleLeft, ColText);
            _torchTag.fontStyle = FontStyle.Bold;
            TextShadow(_torchTag, 1f);
            _torchFill = UiSkin.Gauge(_torchTagRt, "Gauge", new Vector2(2, -9), new Vector2(torchW - 30, 5), Hex("#7ee0a0"), out _torchTrack);
            _torchTagRt.gameObject.SetActive(false);

            // 次のルートの条件（左側。キャラに掛からない幅に収める）
            const float rtW = 134f, rtH = 118f, rtRow = 24f;   // キャラ（左端 -329）に掛からない幅
            // 表示域（背景とマスクを持つ）の中ではなく、その親に置く。docs/ui_rules.md 3 番
            var routeRt = UiSkin.Rect(stageCard, "RouteBox", new Vector2(-AreaW * 0.5f + 6 + rtW * 0.5f, AreaY + AreaH * 0.5f - 44 - rtH * 0.5f), new Vector2(rtW, rtH));
            UiSkin.Img(routeRt, "Shadow", new Vector2(0, -3), new Vector2(rtW + 16, rtH + 16), UiSkin.Shadow(15, 10), new Color(0, 0, 0, 0.7f));
            UiSkin.Img(routeRt, "Edge", Vector2.zero, new Vector2(rtW + 2, rtH + 2), UiSkin.Rounded(12), new Color(1, 1, 1, 0.13f));
            UiSkin.Img(routeRt, "Bg", Vector2.zero, new Vector2(rtW, rtH), UiSkin.Rounded(11), Hex("#0b1120"));
            _routeTitle = UiFactory.Label(routeRt, "Title", new Vector2(0, rtH * 0.5f - 13), new Vector2(rtW - 12, 18), "次のルート", 11, TextAnchor.MiddleCenter, ColTextSub);
            for (int i = 0; i < 3; i++)
            {
                float ry = rtH * 0.5f - 26 - rtRow * 0.5f - i * rtRow;
                _routeBar[i] = UiSkin.Img(routeRt, "Bar" + i, new Vector2(-rtW * 0.5f + 6, ry), new Vector2(4, rtRow - 6), UiSkin.Rounded(2), ColBtn);
                _routeArrow[i] = UiFactory.Label(routeRt, "Arrow" + i, new Vector2(-rtW * 0.5f + 20, ry), new Vector2(16, rtRow), "", 13, TextAnchor.MiddleCenter, ColTextSub);
                _routeArrow[i].fontStyle = FontStyle.Bold;
                _routeText[i] = UiFactory.Label(routeRt, "Text" + i, new Vector2(6, ry), new Vector2(rtW - 40, rtRow), "", 11, TextAnchor.MiddleLeft, ColText);
                TextShadow(_routeText[i], 1f);
            }
            _routeBox = routeRt.gameObject;
            _routeBox.SetActive(false);

            _hint = UiFactory.Label(_area, "Hint", new Vector2(0, AreaH * 0.5f - 34), new Vector2(640, 36), "", 24, TextAnchor.MiddleCenter, ColAccent);
            _hint.fontStyle = FontStyle.Bold;
            TextShadow(_hint);

            // 状態の帯（不透明。§16.5: モード / 残りG / G数は演出中も消さない）
            var band = UiSkin.Rect(stageCard, "Band", new Vector2(0, StageCardH * 0.5f - BandH * 0.5f - 1), new Vector2(ContentW - 2, BandH));
            UiSkin.Img(band, "BgTop", Vector2.zero, new Vector2(ContentW - 2, BandH), UiSkin.Rounded(12), Hex("#0c1019"));
            UiSkin.Img(band, "BgBottom", new Vector2(0, -BandH * 0.25f), new Vector2(ContentW - 2, BandH * 0.5f), null, Hex("#0c1019"));
            UiSkin.Img(band, "Line", new Vector2(0, -BandH * 0.5f + 0.5f), new Vector2(ContentW - 2, 1), null, new Color(1, 1, 1, 0.07f));
            _modeChip = UiSkin.Chip(band, "ModeChip", new Vector2(-ContentW * 0.5f + 8 + 46, 0), new Vector2(88, 20), "通常", ColBtn, ColText, 12);
            _modeChipBg = _modeChip.transform.parent.Find("Bg").GetComponent<Image>();
            _tier2 = UiSkin.Chip(band, "EngageChip", new Vector2(-ContentW * 0.5f + 8 + 88 + 8 + 60, 0), new Vector2(120, 20), "", new Color(0.85f, 0.12f, 0.2f), ColText, 12);
            _tier2Box = _tier2.transform.parent.gameObject;
            _tier2Box.SetActive(false);
            // AT「洞窟」: 残りG と 獲得枚数（常時表示。演出中も消さない §16.5）
            _atChip = UiSkin.Chip(band, "AtChip", new Vector2(-ContentW * 0.5f + 8 + 88 + 8 + 90, 0), new Vector2(180, 20), "", Hex("#2b1e5a"), ColText, 12);
            _atChipBg = _atChip.transform.parent.Find("Bg").GetComponent<Image>();
            _atChipBox = _atChip.transform.parent.gameObject;
            _atChipBox.SetActive(false);
            // メッセージは左のチップ群（最大で右端 -188）と右の G 数（左端 332）の間に収める
            _message = UiFactory.Label(band, "Message", new Vector2(36, 0), new Vector2(376, BandH), "", 16, TextAnchor.MiddleCenter, ColText);
            _message.fontStyle = FontStyle.Bold;
            TextShadow(_message, 0.6f);
            UiFactory.Label(band, "GLabel", new Vector2(ContentW * 0.5f - 8 - 92 - 20, 0), new Vector2(40, 20), "GAME", 10, TextAnchor.MiddleRight, UiSkin.TextDim);
            _gCount = UiSkin.Number(band, "G", new Vector2(ContentW * 0.5f - 8 - 44, 0), new Vector2(88, BandH), "0 G", 15, ColGold);

            // 会話 UI（旅人 ⇄ 主人公）。表示域の下部に重ねる不透明の帯。名前タグ＋本文
            var dlg = UiSkin.Rect(stageCard, "Dialogue", new Vector2(0, -StageCardH * 0.5f + 38), new Vector2(620, 54));
            UiSkin.Img(dlg, "Shadow", new Vector2(0, -5), new Vector2(644, 78), UiSkin.Shadow(14, 12), new Color(0, 0, 0, 0.55f));
            UiSkin.Img(dlg, "Edge", Vector2.zero, new Vector2(620, 54), UiSkin.Rounded(15), ColPanelEdge);
            UiSkin.Img(dlg, "Bg", Vector2.zero, new Vector2(618, 52), UiSkin.Rounded(14), new Color(0.03f, 0.04f, 0.08f, 0.94f));
            _dialogNameBg = UiSkin.Img(dlg, "NameBg", new Vector2(-310 + 10 + 46, 27), new Vector2(92, 20), UiSkin.Rounded(10), ColGold);
            _dialogName = UiFactory.Label(_dialogNameBg.rectTransform, "Name", new Vector2(0, 0), new Vector2(92, 20), "", 11, TextAnchor.MiddleCenter, ColBg);
            _dialogName.fontStyle = FontStyle.Bold;
            _dialogText = UiFactory.Label(dlg, "Text", new Vector2(8, -3), new Vector2(584, 44), "", 15, TextAnchor.MiddleLeft, ColText);
            _dialogBox = dlg.gameObject;
            _dialogBox.SetActive(false);

            // BB カットイン（表示域の中央。マスク外に出てよい）
            _cutinRt = MakeImage(_stage, "Cutin", new Vector2(0, StageCardY + AreaY), new Vector2(380, 380), ArtLoader.Sprite("Art/UI/britz_bonus_logo"));
            _cutinCg = _cutinRt.gameObject.AddComponent<CanvasGroup>();
            _cutinCg.alpha = 0f;
            _cutinCg.blocksRaycasts = false;

            // ===== 中段: 左 表示器 / 中央 リール筐体 / 右 ステータス =====
            float reelPitch = ReelView.ReelWidth + 8f;
            float cabW = reelPitch * 3 + 24;
            float innerW = SideW - 24;

            // 左: EMBER（灯火）/ PAYOUT（くぼんだ表示器、桁固定の右寄せ §8）
            var disp = UiSkin.Card(_stage, "Display", new Vector2(-ContentW * 0.5f + SideW * 0.5f, MidY), new Vector2(SideW, MidH), 12);
            // 見出しのアイコンは左端に領域を取り、文字はその分だけ字下げする（docs/ui_rules.md 1 番）
            const float HeadIco = 15f, HeadGap = 5f, HeadIndent = HeadIco + HeadGap;
            UiSkin.Img(disp, "EmberIcon", new Vector2(-innerW * 0.5f + HeadIco * 0.5f, 83), new Vector2(HeadIco, HeadIco), UiSkin.Icon("ember", 64), Color.white);
            UiSkin.Heading(disp, "CreditLabel", new Vector2(0, 83), innerW, "EMBER", HeadIndent);
            var creditInset = UiSkin.Inset(disp, "CreditInset", new Vector2(0, 49), new Vector2(innerW, 40), 8);
            _creditNum = UiSkin.Number(creditInset, "CreditNum", new Vector2(-6, 0), new Vector2(innerW - 20, 40), "50", 28, ColText);
            UiSkin.Img(disp, "PayoutIcon", new Vector2(-innerW * 0.5f + HeadIco * 0.5f, 14), new Vector2(HeadIco, HeadIco), UiSkin.Circle(32), ColGold);
            UiSkin.Img(disp, "PayoutIconIn", new Vector2(-innerW * 0.5f + HeadIco * 0.5f, 14), new Vector2(HeadIco * 0.5f, HeadIco * 0.5f), UiSkin.Circle(32), UiSkin.GoldDeep);
            UiSkin.Heading(disp, "PayoutLabel", new Vector2(0, 14), innerW, "PAYOUT", HeadIndent);
            var payInset = UiSkin.Inset(disp, "PayoutInset", new Vector2(0, -20), new Vector2(innerW, 40), 8, null, "pill_coin");   // 左にコインが乗った窓
            _payoutNum = UiSkin.Number(payInset, "PayoutNum", new Vector2(-6, 0), new Vector2(innerW - 20, 40), "0", 28, ColGold);
            // 下の行は 左=設定 / 右=ソウル。アイコンぶんを差し引いて領域を分ける
            const float SoulIco = 14f;
            float halfW = innerW * 0.5f;
            _mode = UiFactory.Label(disp, "Mode", new Vector2(-innerW * 0.25f - 2, -76), new Vector2(halfW - 4, 14), "", 11, TextAnchor.MiddleLeft, ColTextSub);
            _soulText = UiFactory.Label(disp, "Soul", new Vector2(innerW * 0.25f - SoulIco * 0.5f - 2, -76), new Vector2(halfW - SoulIco - 8, 14), "", 11, TextAnchor.MiddleRight, Hex("#a98bff"));
            UiSkin.Img(disp, "SoulIcon", new Vector2(halfW - SoulIco * 0.5f, -76), new Vector2(SoulIco, SoulIco), UiSkin.Icon("soul", 64), Color.white);

            // 中央: リール筐体（金の縁 + くぼんだ窓 + ガラスの光沢 + 中段ラインのマーカー）
            var cabinet = UiSkin.Rect(_stage, "ReelCabinet", new Vector2(0, MidY), new Vector2(cabW, MidH));
            UiSkin.Img(cabinet, "Shadow", new Vector2(0, -6), new Vector2(cabW + 24, MidH + 24), UiSkin.Shadow(12, 14), new Color(0, 0, 0, 0.6f));
            var cabFrame = UiSkin.Frame("panel_navy");
            if (cabFrame != null)
            {
                UiSkin.Img(cabinet, "Body", Vector2.zero, new Vector2(cabW, MidH), cabFrame, Color.white, true);
            }
            else
            {
                UiSkin.Img(cabinet, "GoldEdge", Vector2.zero, new Vector2(cabW, MidH), UiSkin.Rounded(12), UiSkin.GoldDeep);
                UiSkin.Img(cabinet, "GoldSheen", new Vector2(0, MidH * 0.25f), new Vector2(cabW - 2, MidH * 0.5f), UiSkin.GradientV(true), new Color(1, 1, 0.8f, 0.35f)).type = Image.Type.Simple;
                UiSkin.Img(cabinet, "Body", Vector2.zero, new Vector2(cabW - 6, MidH - 6), UiSkin.Rounded(10), UiSkin.PanelHi, true);
            }
            var strips = _m.Strips;
            _reels = new ReelView[3];
            float reelH = ReelView.SymbolHeight * 3;
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var pos = new Vector2((i - 1) * reelPitch, 0);
                UiSkin.Inset(cabinet, "Window" + i, pos, new Vector2(ReelView.ReelWidth + 8, reelH + 8), 8, Hex("#05070c"));
                _reels[i] = ReelView.Create(cabinet, strips[i], pos);
                _reels[i].Stopped += OnReelStopped;
                UiSkin.Img(cabinet, "Glass" + i, pos + new Vector2(0, reelH * 0.5f - 34), new Vector2(ReelView.ReelWidth, 68), UiSkin.GradientV(true), new Color(1, 1, 1, 0.10f)).type = Image.Type.Simple;
                UiSkin.Img(cabinet, "GlassB" + i, pos + new Vector2(0, -reelH * 0.5f + 16), new Vector2(ReelView.ReelWidth, 32), UiSkin.GradientV(false), new Color(0, 0, 0, 0.18f)).type = Image.Type.Simple;
                // タッチ: リールそのものが停止ボタン（§6.4 指はカーソルより太い）
                var tap = UiSkin.Img(cabinet, "Tap" + i, pos, new Vector2(ReelView.ReelWidth + 8, reelH + 8), null, new Color(0, 0, 0, 0), true);
                var tapBtn = tap.gameObject.AddComponent<Button>();
                tapBtn.transition = Selectable.Transition.None;
                tapBtn.onClick.AddListener(() => StopReel(idx));
            }
            // 中段ライン（払い出しライン）の目印: 左右の金ひし形
            foreach (float sx in new[] { -1f, 1f })
            {
                var mk = UiSkin.Img(cabinet, "LineMark", new Vector2(sx * (cabW * 0.5f - 11), 0), new Vector2(9, 9), UiSkin.Rounded(2), ColGold);
                mk.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            }
            // 役演出: 揃ったコマだけを役の色で光らせる（斜めライン・チェリーの段もそのまま出せる）
            var pl = UiSkin.Rect(cabinet, "PaylineFlash", Vector2.zero, new Vector2(cabW, MidH));
            _paylineFlash = pl.gameObject.AddComponent<CanvasGroup>();
            _paylineFlash.alpha = 0f;
            _paylineFlash.blocksRaycasts = false;
            var cellSize = new Vector2(ReelView.ReelWidth, ReelView.SymbolHeight);
            for (int i = 0; i < 3; i++)
                for (int row = 0; row < 3; row++)
                {
                    var cellPos = new Vector2((i - 1) * reelPitch, (1 - row) * ReelView.SymbolHeight);
                    var cellGlow = UiSkin.Img(pl, $"Cell{i}{row}", cellPos, cellSize * 1.5f, UiSkin.Glow(96), new Color(1, 1, 1, 0.55f));
                    UiSkin.Img(cellGlow.rectTransform, "Fill", Vector2.zero, cellSize, UiSkin.Rounded(6), new Color(1, 1, 1, 0.55f));
                    _cellFx[i * 3 + row] = cellGlow;
                    cellGlow.gameObject.SetActive(false);
                }

            // 右: PLAYER / BONUS / 状態 / 設定
            var side = UiSkin.Card(_stage, "Side", new Vector2(ContentW * 0.5f - SideW * 0.5f, MidY), new Vector2(SideW, MidH), 12);
            UiSkin.Img(side, "PlayerIcon", new Vector2(-innerW * 0.5f + HeadIco * 0.5f, 83), new Vector2(HeadIco, HeadIco), UiSkin.Icon("sword", 64), Color.white);
            UiSkin.Heading(side, "PlayerLabel", new Vector2(0, 83), innerW, "PLAYER", HeadIndent);
            _player = UiFactory.Label(side, "Lv", new Vector2(0, 60), new Vector2(innerW, 20), "Lv 1", 16, TextAnchor.MiddleLeft, ColGold);
            _player.fontStyle = FontStyle.Bold;
            UiFactory.Label(side, "ExpLabel", new Vector2(0, 60), new Vector2(innerW, 20), "EXP", 10, TextAnchor.MiddleRight, UiSkin.TextDim);
            _expFill = UiSkin.Gauge(side, "Exp", new Vector2(0, 44), new Vector2(innerW, 8), ColGreen, out _expTrack);
            UiSkin.Img(side, "BonusIcon", new Vector2(-innerW * 0.5f + HeadIco * 0.5f, 24), new Vector2(HeadIco, HeadIco), UiSkin.Icon("amulet", 64), Color.white);
            UiSkin.Heading(side, "BonusHead", new Vector2(0, 24), innerW, "BONUS", HeadIndent);
            _bonusLabel = UiFactory.Label(side, "BonusLabel", new Vector2(0, 3), new Vector2(innerW, 16), "―", 12, TextAnchor.MiddleLeft, ColGold);
            _bonusFill = UiSkin.Gauge(side, "BonusGauge", new Vector2(0, -12), new Vector2(innerW, 8), ColGold, out _bonusTrack);
            _status = UiFactory.Label(side, "Status", new Vector2(0, -33), new Vector2(innerW, 18), "", 12, TextAnchor.UpperLeft, ColText);
            var btnSettings = UiSkin.Button(side, "BtnSettings", new Vector2(-innerW * 0.5f + 30, -58), new Vector2(56, 24), "音量", ToggleSettings, ColBtn, 12, false, 8);
            var btnGraph = UiSkin.Button(side, "BtnGraph", new Vector2(0, -58), new Vector2(56, 24), "グラフ", ToggleGraph, ColBtn, 12, false, 8);
            var btnDebug = UiSkin.Button(side, "BtnDebug", new Vector2(innerW * 0.5f - 30, -58), new Vector2(56, 24), "DEBUG", ToggleDebug, ColBtn, 11, false, 8);

            // 常駐のスランプ（設定で出し入れする）。数字は右に小さく添える
            _miniBox = UiSkin.Rect(side, "MiniSlump", new Vector2(0, -83), new Vector2(innerW, 22));
            _mini = SlumpGraph.Create(_miniBox, new Vector2(-30, 0), new Vector2(innerW - 62, 22), _m.Credit, true);
            _miniLabel = UiFactory.Label(_miniBox, "MiniDiff", new Vector2(innerW * 0.5f - 29, 0), new Vector2(56, 16), "0", 11, TextAnchor.MiddleRight, ColTextSub);
            _miniBox.gameObject.SetActive(SaveData.LoadGraphAlwaysOn());

            // ===== 下段: 操作バー（左 BET / 中央 STOP×3 = リールと同じ物理配置 §16.3 / 右 AUTO）=====
            _btnBet = UiSkin.Button(_stage, "BtnBet", new Vector2(-ContentW * 0.5f + SideW * 0.5f, CtrlY), new Vector2(SideW, CtrlH), "MAX BET", OnBetClicked, ColAccent, 20, true, 12);
            AddSubHint(_btnBet, _isTouch ? "画面タップでも OK" : "Ctrl / Space");
            _btnStops = new Button[3];
            string[] stopKeys = { "Z / ←", "X / ↓", "C / →" };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                _btnStops[i] = UiSkin.Button(_stage, "BtnStop" + i, new Vector2((i - 1) * reelPitch, CtrlY), new Vector2(ReelView.ReelWidth, CtrlH), "STOP", () => StopReel(idx), ColBtn, 18, true, 12);
                AddSubHint(_btnStops[i], _isTouch ? "リールをタップ" : stopKeys[i]);
            }
            _btnAuto = UiSkin.Button(_stage, "BtnAuto", new Vector2(ContentW * 0.5f - SideW * 0.5f, CtrlY), new Vector2(SideW, CtrlH), "AUTO", CycleAuto, ColBtn, 18, true, 12);
            AddSubHint(_btnAuto, _isTouch ? "押すたび x1〜x6" : "A / Space長押し");

            // ベル択ナビ: リールの真上に大きな数字の丸バッジ（最前面・物理配置と一致 §16.3）
            const float badge = 66f;
            float naviY = StageCardY - StageCardH * 0.5f - 2f;   // 上段カードと筐体の隙間。リール上段に少し食い込ませて「リールの上」に見せる
            var navi = UiSkin.Rect(_stage, "Navi", new Vector2(0, naviY), new Vector2(cabW, badge));
            for (int i = 0; i < 3; i++)
            {
                var cell = UiSkin.Rect(navi, "Cell" + i, new Vector2((i - 1) * reelPitch, 0), new Vector2(badge, badge));
                _naviCells[i] = cell;
                _naviGlow[i] = UiSkin.Img(cell, "Glow", Vector2.zero, new Vector2(badge * 2.4f, badge * 2.4f), UiSkin.Glow(96), new Color(1, 0.85f, 0.3f, 0f));
                UiSkin.Img(cell, "Shadow", new Vector2(0, -3), new Vector2(badge + 18, badge + 18), UiSkin.Shadow(33, 9), new Color(0, 0, 0, 0.6f));
                _naviBg[i] = UiSkin.Img(cell, "Ring", Vector2.zero, new Vector2(badge, badge), UiSkin.Rounded(33), ColBtn);
                UiSkin.Img(cell, "Inner", Vector2.zero, new Vector2(badge - 8, badge - 8), UiSkin.Rounded(29), new Color(0.04f, 0.05f, 0.09f, 0.92f));
                UiSkin.Img(cell, "Sheen", new Vector2(0, badge * 0.22f), new Vector2(badge - 16, badge * 0.36f), UiSkin.GradientV(true), new Color(1, 1, 1, 0.14f)).type = Image.Type.Simple;
                _naviLabels[i] = UiFactory.Label(cell, "L", new Vector2(0, 2), new Vector2(badge, badge), "", 40, TextAnchor.MiddleCenter, ColText);
                _naviLabels[i].fontStyle = FontStyle.Bold;
                TextShadow(_naviLabels[i]);
            }
            _naviBox = navi.gameObject;
            _naviBox.SetActive(false);

            // 技術介入の課題バナー（ナビと同じ帯。両方同時には出ない）
            _techBanner = UiFactory.Label(_stage, "TechBanner", new Vector2(0, naviY), new Vector2(cabW, 28), "", 15, TextAnchor.MiddleCenter, ColGold);
            _techBanner.fontStyle = FontStyle.Bold;
            TextShadow(_techBanner);
            _techBanner.gameObject.SetActive(false);

            // ===== モーダル: 音量・設定 =====
            _settingsBox = BuildModal("Settings", new Vector2(400, 272), "サウンド / 設定", ToggleSettings, out var sBody);
            UiFactory.Label(sBody, "BgmLabel", new Vector2(-140, 40), new Vector2(60, 20), "BGM", 12, TextAnchor.MiddleLeft, ColTextSub);
            _bgmSlider = UiFactory.Slider(sBody, "BgmSlider", new Vector2(30, 40), new Vector2(230, 20), _audio.BgmVolume, v => { _audio.BgmVolume = v; });
            SkinSlider(_bgmSlider);
            _bgmSlider.gameObject.AddComponent<SliderReleaseSound>().OnRelease = () => { _audio.UiPop(); SaveData.SaveAudio(_audio); };
            UiFactory.Label(sBody, "SeLabel", new Vector2(-140, 8), new Vector2(60, 20), "SE", 12, TextAnchor.MiddleLeft, ColTextSub);
            _seSlider = UiFactory.Slider(sBody, "SeSlider", new Vector2(30, 8), new Vector2(230, 20), _audio.SeVolume, v => { _audio.SeVolume = v; });
            SkinSlider(_seSlider);
            _seSlider.gameObject.AddComponent<SliderReleaseSound>().OnRelease = () => { _audio.UiPop(); SaveData.SaveAudio(_audio); };
            var bgmToggle = UiSkin.Button(sBody, "BgmToggle", new Vector2(-96, -34), new Vector2(150, 30), "BGM ON / OFF", () => { _audio.ToggleBgm(); _audio.UiPop(); SaveData.SaveAudio(_audio); }, ColBtn, 12, false, 8);
            UiSkin.Button(sBody, "ResetSave", new Vector2(72, -34), new Vector2(150, 30), "セーブ削除", OnResetSavePressed, new Color(0.45f, 0.15f, 0.2f), 12, false, 8);
            UiSkin.Button(sBody, "BackToTown", new Vector2(-96, -66), new Vector2(150, 30), "街へ戻る", OnBackToTown, Hex("#5b3fd0"), 12, false, 8);
            _graphAlwaysBtn = UiSkin.Button(sBody, "GraphAlways", new Vector2(72, -66), new Vector2(150, 30), "", ToggleGraphAlways, ColBtn, 12, false, 8);
            _resetConfirm = UiFactory.Label(sBody, "ResetConfirm", new Vector2(0, -96), new Vector2(360, 16), "", 11, TextAnchor.MiddleCenter, ColGold);
            UiFactory.Label(sBody, "Keys", new Vector2(0, -120), new Vector2(370, 16),
                _isTouch ? "画面をタップ: BET / 順に停止      リールをタップ: そのリールを停止" : "B: BGM   G: グラフ   M: マップ   E: 装備   R: セーブ削除   F1〜F6: 設定   D: デバッグ   Esc: 閉じる", 10, TextAnchor.MiddleCenter, UiSkin.TextDim);
            RefreshGraphAlwaysLabel();
            _settingsBox.SetActive(false);

            // ===== モーダル: デバッグ =====
            // 左に状態表示、右に 2 列のボタン、下に設定 1〜6 の一列。重ならないよう位置は計算で置く
            const float dW = 660f, dH = 420f;
            _debugBox = BuildModal("Debug", new Vector2(dW, dH), "DEBUG", ToggleDebug, out var dBody);

            const float infoW = 280f, infoH = 268f;
            var info = UiSkin.Inset(dBody, "Info", new Vector2(-dW * 0.5f + 20 + infoW * 0.5f, 8), new Vector2(infoW, infoH), 8);
            _debug = UiFactory.Label(info, "Debug", new Vector2(6, 0), new Vector2(infoW - 24, infoH - 16), "", 12, TextAnchor.UpperLeft, ColTextSub);
            _debug.lineSpacing = 1.25f;

            // ボタン: 右側 2 列 × 6 行
            const float btnW = 148f, btnH = 30f, gapY = 34f;
            float colL = 60f, colR = colL + btnW + 12f;
            float rowTop = 116f;
            void DbgBtn(string label, System.Action act, int col, int row, Color? color = null)
            {
                var pos = new Vector2(col == 0 ? colL : colR, rowTop - row * gapY);
                UiSkin.Button(dBody, "Dbg_" + label, pos, new Vector2(btnW, btnH), label,
                    () => { act(); _audio.UiPop(); RefreshUi(); }, color ?? ColBtn, 12, false, 8);
            }
            DbgBtn("敵出現", () => { _m.DebugForceEnemy = true; SetMessage("DEBUG: 次の判定で敵出現", false, ColTextSub); }, 0, 0);
            DbgBtn("討伐ON/OFF", () => { if (_m.EnemyActive) _m.EnemyDefeatWon = !_m.EnemyDefeatWon; }, 1, 0);
            DbgBtn("BB強制", () => _m.DebugForceFlag = Flag.BB_A, 0, 1);
            DbgBtn("REG強制", () => _m.DebugForceFlag = Flag.RB_A, 1, 1);
            DbgBtn("ベル強制", () => _m.DebugForceFlag = Flag.BELL_A, 0, 2);
            DbgBtn("リプ強制", () => _m.DebugForceFlag = Flag.REPLAY_A, 1, 2);
            DbgBtn("スイカ強制", () => _m.DebugForceFlag = Flag.SUICA_A, 0, 3);
            DbgBtn("チェリー強制", () => _m.DebugForceFlag = Flag.CHERRY_A, 1, 3);
            DbgBtn("ハズレ強制", () => _m.DebugForceFlag = Flag.HAZE, 0, 4);
            DbgBtn("エンバー+1000", () => _m.Credit += 1000, 1, 4);
            DbgBtn("旅人", () => { var c = _m.Config.travelers ?? TravelerConfig.Default(); var t = c.travelers[_fxRng.Next(c.travelers.Count)]; if (_traveler == null) SpawnTraveler(t, t.chatter.Count > 0 ? t.chatter[_fxRng.Next(t.chatter.Count)] : null, false); }, 0, 5);
            DbgBtn("AT開始", () => { if (!_m.InAt) { _m.PendingAt = true; SetMessage("DEBUG: 次G から AT", false, ColTextSub); } }, 1, 5, Hex("#5b3fd0"));

            // 設定 1〜6（1行に並べる）
            UiFactory.Label(dBody, "SetLabel", new Vector2(-dW * 0.5f + 20 + 24, -dH * 0.5f + 34), new Vector2(48, 20), "設定", 12, TextAnchor.MiddleLeft, ColTextSub);
            for (int i = 0; i < 6; i++)
            {
                int s = i + 1;
                float x = -dW * 0.5f + 20 + 56 + 26 + i * 58f;
                UiSkin.Button(dBody, "Dbg_Set" + s, new Vector2(x, -dH * 0.5f + 34), new Vector2(52, 28), s.ToString(),
                    () => { if (!_m.IsGameActive) _m.SetSetting(s); _audio.UiPop(); RefreshUi(); }, ColBtn, 13, false, 8);
            }
            _debugBox.SetActive(false);

            // ===== モーダル: スランプグラフ =====
            // 上にグラフ、下に前回までの潜行の一覧。選ぶとその回の波形を薄く重ねる
            const float grW = 720f, grH = 500f;
            _graphBox = BuildModal("Graph", new Vector2(grW, grH), "スランプグラフ（エンバーの増減）", ToggleGraph, out var gBody);
            _graph = SlumpGraph.Create(gBody, new Vector2(0, grH * 0.5f - 38 - 8 - 132), new Vector2(660, 264), _m.Credit);
            _runBaseCredit = _m.Credit;
            UiSkin.Heading(gBody, "HistHead", new Vector2(0, grH * 0.5f - 38 - 8 - 280), 660, "前回までの冒険", 0f);
            _histRows = new Button[RunHistory.Keep];
            _histLabels = new Text[RunHistory.Keep];
            for (int i = 0; i < RunHistory.Keep; i++)
            {
                int idx = i;
                float y = grH * 0.5f - 38 - 8 - 302 - i * 26f;
                var b = UiSkin.Button(gBody, "Hist" + i, new Vector2(0, y), new Vector2(660, 24), "",
                    () => SelectHistory(idx), ColBtn, 12, false, 6);
                _histRows[i] = b;
                _histLabels[i] = b.GetComponentInChildren<Text>();
                if (_histLabels[i] != null) _histLabels[i].alignment = TextAnchor.MiddleLeft;
            }
            UiSkin.Button(gBody, "GraphReset", new Vector2(grW * 0.5f - 90, -grH * 0.5f + 20), new Vector2(140, 28), "ここから取り直す",
                () => { _audio.UiPop(); _graph.ResetTo(_m.Credit); _graph.SetGhost(null); _histPicked = -1; RefreshHistory(); }, ColBtn, 12, false, 8);
            _graphBox.SetActive(false);

            // ===== モーダル: 冒険マップ =====
            _mapBox = BuildModal("Map", new Vector2(760, 460), "冒険マップ", ToggleMap, out _mapBody);
            _mapInfo = UiFactory.Label(_mapBody, "Info", new Vector2(0, 460 * 0.5f - 46), new Vector2(700, 20), "", 13, TextAnchor.MiddleCenter, ColTextSub);
            _mapBox.SetActive(false);

            // ボーナス中の枠（AT 期待度）。舞台の四辺を縁取り、期待度のランク色で点滅する
            BuildAtFrame();
            BuildAtTicker();

            // エフェクト層（舞台と一緒に縮尺。UI の上、発光オーバーレイの下）
            UiFx.Init(_stage);

            // ステージ札とライフ札はカード内の最前面へ（背景・キャラ・帯に隠れないように）
            if (_stageTagBg != null) _stageTagBg.transform.parent.SetAsLastSibling();
            if (_torchTagRt != null) _torchTagRt.SetAsLastSibling();
            if (_routeBox != null) _routeBox.transform.SetAsLastSibling();

            // 赤発光オーバーレイ（画面全体・最前面）
            var glow = UiSkin.Img(root, "RedGlow", Vector2.zero, Vector2.zero, null, new Color(1, 0.1f, 0.1f, 0));
            UiSkin.Stretch(glow.rectTransform);
            _redGlow = glow;
        }

        /// <summary>街（ミニマップ）へ戻る。回転中は戻れない。セーブしてから画面を差し替える。</summary>
        private void OnBackToTown()
        {
            if (_m.IsGameActive || _inputLocked) { SetMessage("回転中は街へ戻れません", false, ColAccent); return; }
            _audio.UiPop();
            if (!_runRecorded) { RecordRun(_runEndReason); _runRecorded = true; }
            SaveData.Save(_m, _audio);
            CloseModals();
            var canvasGo = _stage != null ? _stage.GetComponentInParent<Canvas>()?.gameObject : null;
            if (canvasGo != null) Destroy(canvasGo);
            MapScreen.Open();
            Destroy(gameObject);
        }

        private void ToggleSettings() { _settingsBox.SetActive(!_settingsBox.activeSelf); if (_settingsBox.activeSelf) _debugBox.SetActive(false); _resetConfirm.text = ""; _audio.UiPop(); }
        private void ToggleDebug() { _debugBox.SetActive(!_debugBox.activeSelf); if (_debugBox.activeSelf) { _settingsBox.SetActive(false); _graphBox.SetActive(false); } _audio.UiPop(); }
        private void ToggleMap()
        {
            if (_mapBox == null) return;
            _mapBox.SetActive(!_mapBox.activeSelf);
            if (_mapBox.activeSelf) { _settingsBox.SetActive(false); _debugBox.SetActive(false); _graphBox.SetActive(false); RedrawMap(); }
            _audio.UiPop();
        }

        /// <summary>マップを描き直す（開くたび・ステージが変わるたび）。</summary>
        private void RedrawMap()
        {
            if (_mapBody == null || !_m.AdventureEnabled) return;
            if (_mapView != null) Destroy(_mapView.gameObject);
            var cfg = _m.Config.adventure;
            _mapView = StageMapView.Build(_mapBody, cfg, _m.Adv, new Vector2(700, 340));
            _mapView.anchoredPosition = new Vector2(0, -16);
            var n = _m.CurrentStage;
            string next = _m.Adv.nextId != null ? cfg.Find(_m.Adv.nextId)?.name : null;
            _mapInfo.text = $"{cfg.chapterName}   第{_m.Adv.chapter}章   現在地 {n?.id} {n?.name}   残り {_m.Adv.spinsLeft} G"
                            + (next != null ? $"   次 → {next}" : "") + (_m.Adv.treasuresFound > 0 ? $"   宝 {_m.Adv.treasuresFound}" : "");
            BuildConditionList(n);
        }

        /// <summary>マップの下に、今のステージの達成条件と進み具合を並べる。</summary>
        private void BuildConditionList(StageNode node)
        {
            if (_condList != null) Destroy(_condList.gameObject);
            _condList = null;
            var conds = node?.routeConditions;
            if (conds == null || conds.Count == 0) return;
            var cfg = _m.Config.adventure;

            _condList = UiSkin.Rect(_mapBody, "Conditions", new Vector2(0, -460 * 0.5f + 66), new Vector2(700, 96));
            UiFactory.Label(_condList, "Head", new Vector2(0, 38), new Vector2(700, 18), "このステージの分岐条件", 12, TextAnchor.MiddleCenter, ColTextSub);
            int shown = 0;
            foreach (var c in conds)
            {
                if (c == null || shown >= 3) continue;
                bool met = AdventureDirector.Meets(c, _m.Adv, _m.Credit, _m.Wallet.Souls, _m.PlayerLevel);
                if (c.hidden && !met) continue;
                var to = cfg.Find(c.to);
                float y = 16 - shown * 24;
                var col = met ? ColGold : ColText;
                UiSkin.Img(_condList, "Row" + shown, new Vector2(0, y), new Vector2(700, 22), UiSkin.Rounded(6), met ? new Color(1f, 0.82f, 0.25f, 0.14f) : new Color(1, 1, 1, 0.05f));
                UiFactory.Label(_condList, "To" + shown, new Vector2(-300, y), new Vector2(120, 20), to != null ? $"→ {to.id}" : "→ ?", 12, TextAnchor.MiddleLeft, met ? ColGold : ColTextSub);
                UiFactory.Label(_condList, "Desc" + shown, new Vector2(-60, y), new Vector2(360, 20), AdventureDirector.DescribeCondition(c), 12, TextAnchor.MiddleLeft, col);
                UiFactory.Label(_condList, "Prog" + shown, new Vector2(250, y), new Vector2(220, 20),
                    met ? "達成！" : AdventureDirector.ProgressText(c, _m.Adv, _m.Credit, _m.Wallet.Souls, _m.PlayerLevel),
                    12, TextAnchor.MiddleRight, met ? ColGold : ColTextSub);
                shown++;
            }
            if (shown == 0) Destroy(_condList.gameObject);
        }

        /// <summary>スランプの常駐を切り替える。表示の好みなので別キーに残す。</summary>
        private void ToggleGraphAlways()
        {
            bool on = !SaveData.LoadGraphAlwaysOn();
            SaveData.SaveGraphAlwaysOn(on);
            if (_miniBox != null) _miniBox.gameObject.SetActive(on);
            if (on) RedrawMini();
            RefreshGraphAlwaysLabel();
            _audio.UiPop();
        }

        private void RefreshGraphAlwaysLabel()
        {
            var t = _graphAlwaysBtn != null ? _graphAlwaysBtn.GetComponentInChildren<Text>() : null;
            if (t != null) t.text = SaveData.LoadGraphAlwaysOn() ? "スランプ常時表示 ON" : "スランプ常時表示 OFF";
        }

        /// <summary>常駐のスランプを描き直す。閉じているときは何もしない。</summary>
        private void RedrawMini()
        {
            if (_miniBox == null || !_miniBox.gameObject.activeSelf || _mini == null) return;
            _mini.Redraw();
            if (_miniLabel != null)
            {
                int d = _mini.LastDiff;
                _miniLabel.text = d.ToString("+#,##0;-#,##0;0");
                _miniLabel.color = d >= 0 ? ColGreen : ColAccent;
            }
        }

        /// <summary>履歴の一覧を書き直す。記録が無い行は押せなくする。</summary>
        private void RefreshHistory()
        {
            if (_histRows == null) return;
            var runs = RunHistory.Load();
            string hpName = _m.Config.adventure?.resource?.hpName ?? "ライフ";
            for (int i = 0; i < _histRows.Length; i++)
            {
                bool has = i < runs.Count;
                _histRows[i].interactable = has;
                if (_histLabels[i] == null) continue;
                _histLabels[i].text = has ? RunHistory.Label(runs[i], hpName) : "―";
                _histLabels[i].color = !has ? UiSkin.TextDim
                                     : i == _histPicked ? ColGold : ColTextSub;
            }
        }

        /// <summary>一覧の 1 行を押した。もう一度押すと重ね描きを消す。</summary>
        private void SelectHistory(int i)
        {
            var runs = RunHistory.Load();
            if (i < 0 || i >= runs.Count) return;
            _audio.UiPop();
            _histPicked = (_histPicked == i) ? -1 : i;
            _graph.SetGhost(_histPicked < 0 ? null : runs[_histPicked].wave);
            RefreshHistory();
        }

        /// <summary>いまの潜行を履歴に残す。街へ戻るときに 1 回だけ呼ぶ。</summary>
        private void RecordRun(string reason)
        {
            if (_graph == null) return;
            var node = _m.CurrentStage;
            RunHistory.Add(new RunRecord
            {
                when = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                spins = _graph.Spins,
                diff = _m.Credit - _runBaseCredit,
                maxDiff = _graph.MaxDiff,
                minDiff = _graph.MinDiff,
                chapter = _m.Adv?.chapter ?? 1,
                stage = node?.name ?? _m.Adv?.nodeId ?? "",
                reason = reason ?? "",
                wave = _graph.Snapshot(RunHistory.WavePoints),
            });
            _histPicked = -1;
            RefreshHistory();
        }

        private void ToggleGraph()
        {
            _graphBox.SetActive(!_graphBox.activeSelf);
            if (_graphBox.activeSelf) { _settingsBox.SetActive(false); _debugBox.SetActive(false); _graph.Redraw(); RefreshHistory(); }
            _audio.UiPop();
        }
        /// <summary>レバーオン・Esc でモーダルを閉じる（game-design §16.9: 遊技を止めさせない）。</summary>
        private void CloseModals() { if (_settingsBox != null) _settingsBox.SetActive(false); if (_debugBox != null) _debugBox.SetActive(false); if (_graphBox != null) _graphBox.SetActive(false); if (_mapBox != null) _mapBox.SetActive(false); if (_equipBox != null) { Destroy(_equipBox); _equipBox = null; } }

        /// <summary>セーブ削除は 3 秒以内の 2 度押しで確定（§6.1: 破壊的操作を連打で通過させない）。</summary>
        private void OnResetSavePressed()
        {
            if (_m.IsGameActive) { _resetConfirm.text = "回転中は削除できません"; _resetConfirmUntil = Time.time + 2f; return; }
            if (Time.time <= _resetConfirmUntil) { _resetConfirm.text = ""; _resetConfirmUntil = 0; ResetSave(); CloseModals(); return; }
            _resetConfirmUntil = Time.time + 3f;
            _resetConfirm.text = "エンバー・レベルが消えます。もう一度押すと確定";
        }

        private static RectTransform MakeImage(Transform parent, string name, Vector2 pos, Vector2 size, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (sprite == null) img.color = new Color(1, 1, 1, 0);
            return rt;
        }

        private void RefreshUi()
        {
            _creditNum.text = _m.Credit.ToString("N0");
            _payoutNum.text = _lastPayout.ToString();
            bool inBonus = _m.BonusMode != BonusMode.NORMAL;
            bool held = _m.HeldBonusFlag != Flag.HAZE && _m.BonusAnnounceRemaining <= 0;   // 前兆中はまだ明かさない
            string atRank = inBonus ? AtDirector.RankFor(_m.Config.atExpect, _m.AtExpectPercent)?.name : null;
            _bonusLabel.text = inBonus
                ? $"{(_m.BonusMode == BonusMode.BB ? "BIG" : "REG")}   {_m.BonusEarned} / {_m.BonusPayoutTarget}" + (atRank != null ? $"   AT期待度 {atRank}" : "")
                : held ? "ボーナス成立中  揃えよう" : "―";
            _bonusFill.rectTransform.sizeDelta = new Vector2(inBonus && _m.BonusPayoutTarget > 0 ? _bonusTrack.sizeDelta.x * Mathf.Clamp01((float)_m.BonusEarned / _m.BonusPayoutTarget) : 0f, _bonusTrack.sizeDelta.y);
            _mode.text = $"設定 {_m.Setting}   総 {_m.TotalSpinCount:N0} G";
            _soulText.text = $"魂 {_m.Wallet.Souls:N0}   火 {_m.Wallet.Embers:N0}";
            _gCount.text = $"{_m.SpinCount} G";
            if (_stageTag != null && _m.AdventureEnabled)
            {
                var stg = _m.CurrentStage;
                var sc = Hex(AdventureDirector.ColorFor(stg));
                _stageTag.text = stg != null ? $"{stg.id} {stg.name}   のこり {_m.Adv.spinsLeft}G" : "";
                if (_stageTagEdge != null) _stageTagEdge.color = sc;

                // ステージが止まっている間は鎖と錠で封じる
                bool stageLocked = _m.StageHeld;
                if (_holdBox != null && _holdBox.activeSelf != stageLocked)
                {
                    _holdBox.SetActive(stageLocked);
                    if (stageLocked) _holdPulse = 0f;
                }
                if (stageLocked && _holdReason != null) _holdReason.text = _m.StageHeldReason;

                RefreshRoutePanel(stg);

                var res = _m.Config.adventure?.resource;
                bool showTorch = res != null && res.enabled;
                _torchTagRt.gameObject.SetActive(showTorch);
                if (showTorch)
                {
                    // ライフバー: 1G で 1 減る。3 割を切ったら橙、1 割で赤
                    int hp = Mathf.Max(0, _m.Hp);
                    int hpMax = Mathf.Max(1, _m.HpMax);
                    float ratio = Mathf.Clamp01((float)hp / hpMax);
                    _torchTag.text = $"{res.hpName} {hp} / {hpMax}";
                    bool danger = ratio <= 0.1f;
                    var col = danger ? ColAccent : ratio <= 0.3f ? Hex("#ff9a3c") : Hex("#7ee0a0");
                    _torchTag.color = danger ? ColAccent : ColText;
                    _torchFill.color = col;
                    _torchFill.rectTransform.sizeDelta = new Vector2(_torchTrack.sizeDelta.x * ratio, _torchTrack.sizeDelta.y);
                }
            }

            // 帯のモードチップ（色は役割固定: 金=ボーナス / 赤=敵 / 灰=通常）
            string modeText; Color modeBg, modeFg = ColText;
            if (inBonus) { modeText = _m.BonusMode == BonusMode.BB ? "BIG BONUS" : "REG BONUS"; modeBg = ColGold; modeFg = ColBg; }
            else if (held) { modeText = "BONUS 成立"; modeBg = UiSkin.GoldDeep; modeFg = ColBg; }
            else if (_m.InBattle) { modeText = "狩猟中"; modeBg = new Color(0.75f, 0.25f, 0.1f); }
            else if (_m.InAt) { modeText = "洞窟 AT"; modeBg = Hex("#5b3fd0"); }
            else if (_m.IsTier2 || _m.PendingTier2) { modeText = "ENGAGE"; modeBg = new Color(0.85f, 0.12f, 0.2f); }
            else if (_m.PrecursorRemaining > 0) { modeText = "前兆"; modeBg = new Color(0.5f, 0.18f, 0.3f); }
            else { modeText = "通常"; modeBg = ColBtn; }
            _modeChip.text = modeText; _modeChip.color = modeFg; _modeChipBg.color = modeBg;

            string enemy = _m.ActiveEnemyTable != null && _m.EnemyActive ? _m.ActiveEnemyTable.name : "";
            string line1 = (_autoMode ? "AUTO ON" : "") + (_autoMode && _m.IsReplay ? "   " : "") + (_m.IsReplay ? "リプレイ" : "");
            string line2 = _m.IsTier2 ? $"敵: {enemy}" : _m.PendingTier2 ? "次G から敵戦闘" : _m.PrecursorRemaining > 0 ? $"前兆 残り {_m.PrecursorRemaining} G" : "";
            string mission = "";
            if (_m.Missions.Count > 0)
            {
                var ms = _m.Missions[0];
                var def = TechDirector.FindMission(_m.Config.tech, ms.id);
                if (def != null) mission = $"任務: {def.name} {ms.progress}/{def.target}";
            }
            string body = line1 + (line1.Length > 0 && line2.Length > 0 ? "\n" : "") + line2;
            if (mission.Length > 0) body = body.Length > 0 ? body + "\n" + mission : mission;
            _status.text = body;
            var dbg = new System.Text.StringBuilder();
            dbg.Append($"FLAG   {_m.CurrentFlag}\nRNG    {_m.CurrentRng}\nHELD   {_m.HeldBonusFlag}\nMODE   {_m.Mode}\nSLIP   {_m.Slip[0]}, {_m.Slip[1]}, {_m.Slip[2]}\n");
            if (_m.BonusAnnounceRemaining > 0 || _m.PseudoPlay)
                dbg.Append($"前兆   残り {_m.BonusAnnounceRemaining} / {_m.BonusAnnounceTotal} G{(_m.PseudoPlay ? "  擬似遊技" : "")}\n");
            if (inBonus) dbg.Append($"AT期待 {_m.AtExpectPercent}%  {atRank}\n");
            if (_m.InAt)
            {
                dbg.Append($"AT     残り {_m.AtSpinsRemaining} G   {_m.AtPayout} エンバー / {_m.AtSpinCount} G\n");
                if (_m.InBattle && _m.BattleMonster != null)
                    dbg.Append($"狩猟   {_m.BattleMonster.name}  HP {_m.BattleHp}/{_m.BattleHpMax}  残り {_m.BattleSpinsRemaining} G\n");
            }
            if (enemy.Length > 0) dbg.Append($"敵     {enemy}{(_m.EnemyDefeatWon ? "  ●討伐確定" : "")}\n");
            if (_m.DebugForceFlag.HasValue) dbg.Append($"次G強制 {_m.DebugForceFlag}\n");
            if (_m.DebugForceEnemy) dbg.Append("次判定  敵出現\n");
            dbg.Append($"\n{_fps:F0} fps   舞台 {_safe.Scale:F2}x\n{Screen.width}x{Screen.height}");
            _debug.text = dbg.ToString();
            _player.text = $"Lv {_m.PlayerLevel}";
            _expFill.rectTransform.sizeDelta = new Vector2(_expTrack.sizeDelta.x * Mathf.Clamp01((float)_m.PlayerExp / (_m.PlayerLevel * 100)), _expTrack.sizeDelta.y);

            _tier2Box.SetActive((_m.IsTier2 || _m.PendingTier2) && !_m.InAt);
            _tier2.text = _m.IsTier2 ? $"残り {Mathf.Max(0, _m.EngageMaxSpins - _m.Tier2SpinCount)} G" : "NEXT: ENGAGE";

            // AT: 残りG と 獲得枚数（狩猟中は残りGが止まる旨も出す）
            _atChipBox.SetActive(_m.InAt);
            if (_m.InAt)
            {
                if (_m.AtZone != null)
                    _atChip.text = $"{_m.AtZone.name} 残り {_m.AtZoneRemaining}G   AT {_m.AtSpinsRemaining}G   +{_m.AtPayout}";
                else
                    _atChip.text = _m.InBattle ? $"{_m.AtSet}set 残り {_m.AtSpinsRemaining}G （狩猟中）  +{_m.AtPayout}" : $"{_m.AtSet}set 残り {_m.AtSpinsRemaining}G   +{_m.AtPayout}";
                if (_atChipBg != null)
                    _atChipBg.color = _m.AtZone != null && !string.IsNullOrEmpty(_m.AtZone.color)
                        ? Hex(_m.AtZone.color) * 0.55f : Hex("#2b1e5a");
                _atChipBg.color = _m.InBattle ? new Color(0.55f, 0.2f, 0.08f) : Hex("#2b1e5a");
            }
            if (_m.InAt) _caveTint.color = new Color(0.06f, 0.04f, 0.16f, 0.45f);
            else if (_m.AtEntryRemaining <= 0) _caveTint.color = new Color(0.06f, 0.04f, 0.16f, 0f);

            // 狩猟中のモンスター HP
            _hpBox.SetActive(_m.InBattle && _m.BattleMonster != null);
            if (_m.InBattle && _m.BattleMonster != null)
            {
                _hpLabel.text = $"{_m.BattleMonster.name}    残り {_m.BattleSpinsRemaining} G";
                float ratio = _m.BattleHpMax > 0 ? Mathf.Clamp01((float)_m.BattleHp / _m.BattleHpMax) : 0f;
                _hpFill.rectTransform.sizeDelta = new Vector2(_hpTrack.sizeDelta.x * ratio, _hpTrack.sizeDelta.y);
                _hpFill.color = ratio > 0.5f ? ColGreen : ratio > 0.25f ? ColGold : ColAccent;
            }
            if (_resetConfirm.text.Length > 0 && Time.time > _resetConfirmUntil) _resetConfirm.text = "";

            RefreshNavi();
            bool spinning = _m.IsGameActive;
            bool betOk = !spinning && !_inputLocked;
            _btnBet.interactable = betOk;
            UiSkin.SetButtonText(_btnBet, _m.IsReplay ? "REPLAY" : "MAX BET");
            UiSkin.SetLamp(_btnBet, betOk, ColGold);
            for (int i = 0; i < 3; i++)
            {
                bool ok = spinning && !StopsLocked && !_m.PseudoPlay && _reels[i].IsSpinning && _m.Stopped[i] == null;
                _btnStops[i].interactable = ok;
                UiSkin.SetLamp(_btnStops[i], ok, ColAccent);
            }
            UiSkin.SetButtonText(_btnAuto, _autoMode ? (_holdAuto ? "AUTO  x1" : $"AUTO  x{_autoSpeed}") : "AUTO");
            UiSkin.SetButtonColor(_btnAuto, _autoMode ? ColGreen : ColBtn, _autoMode ? ColBg : ColText);
            UiSkin.SetLamp(_btnAuto, _autoMode, ColGreen);
        }
        private void SetMessage(string text, bool flash = false, Color? color = null)
        {
            _message.text = text;
            _message.color = color ?? ColText;
            _messageFlashUntil = flash ? Time.time + _m.Config.timings.nextWin / 1000f : 0f;
        }

        // -------------------------------------------------------- TECH FX
        /// <summary>技術介入とミッションの結果を出す。失敗しても取り上げるものは無い。</summary>
        private void ShowTechResult(GameResult r)
        {
            if (r.tech.Active)
            {
                if (r.techSuccess)
                {
                    _audio.NaviSuccess();
                    UiFx.Burst(_reels[r.tech.reel].GetComponent<RectTransform>(), UiFx.Preset.SuccessStars);
                    UiFx.Ring(_reels[r.tech.reel].GetComponent<RectTransform>(), new Color(1f, 0.9f, 0.4f, 0.9f), 40, 260, 0.5f);
                    var parts = new System.Collections.Generic.List<string>();
                    if (r.techSouls > 0) parts.Add($"SOUL +{r.techSouls}");
                    if (r.techEmbers > 0) parts.Add($"EMB +{r.techEmbers}");
                    if (r.techExp > 0) parts.Add($"EXP +{r.techExp}");
                    if (r.techAtGames > 0) parts.Add($"+{r.techAtGames}G");
                    SetMessage("技術介入 成功！  " + string.Join("  ", parts), true, ColGold);
                    PlayCharacter("victory");
                }
                else
                {
                    UiFx.PopText(_reels[r.tech.reel].GetComponent<RectTransform>(), "MISS", ColTextSub, 20, new Vector2(0, 40));
                }
            }
            if (r.missionCleared != null)
            {
                _audio.NaviSuccess();
                UiFx.Burst(_area, UiFx.Preset.Confetti, new Vector2(0, 40));
                SetMessage($"任務達成！  {r.missionCleared.name}", true, ColGold);
            }
            else if (r.missionStarted != null && !r.tech.Active)
            {
                SetMessage($"任務を受けた: {r.missionStarted.name}", false, ColTextSub);
            }
        }

        // ------------------------------------------------------------ TECH
        /// <summary>技術介入の課題を、対象リールの上のバッジと帯で示す。</summary>
        private void RefreshTech()
        {
            var t = _m.Tech;
            if (!t.Active || !_m.IsGameActive) { if (_techBanner != null) _techBanner.gameObject.SetActive(false); return; }
            _techBanner.gameObject.SetActive(true);
            string reel = t.reel == 0 ? "左" : t.reel == 1 ? "中" : "右";
            string how = t.kind == TechKind.Vita ? "中段にビタ" : "枠内に";
            bool done = _m.Stopped[t.reel] != null;
            if (!done)
            {
                _techBanner.text = $"技術介入   {reel}リールから  {SymbolName(t.symbol)} を {how}";
                _techBanner.color = t.kind == TechKind.Vita ? ColAccent : ColGold;
            }
            else
            {
                bool ok = TechDirector.Judge(t, _m.Stopped[t.reel]);
                _techBanner.text = ok ? "技術介入 成功！" : "技術介入 失敗（損はしない）";
                _techBanner.color = ok ? ColGold : ColTextSub;
            }
        }

        private static string SymbolName(Symbol s)
        {
            switch (s)
            {
                case Symbol.RED7: return "赤7";
                case Symbol.BLUE7: return "青7";
                case Symbol.BAR: return "BAR";
                case Symbol.STAR: return "★";
                case Symbol.WATERMELON: return "スイカ";
                case Symbol.CHERRY: return "チェリー";
                case Symbol.REPLAY: return "リプレイ";
                default: return "ブランク";
            }
        }

        // ---------------------------------------------------------------- NAVI
        /// <summary>ナビ表示: 第一停止は「1」、残り2つは「? ATTACK」「? GUARD」。第一停止後に選択肢だけ残す。</summary>
        /// <summary>押したナビのバッジを膨らませながら消す。</summary>
        private IEnumerator PopNaviBadge(int i)
        {
            var cell = _naviCells[i];
            if (cell == null) yield break;
            var cg = cell.GetComponent<CanvasGroup>() ?? cell.gameObject.AddComponent<CanvasGroup>();
            float t = 0f, d = 0.22f;
            while (t < d)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / d);
                float e = 1f - (1f - u) * (1f - u);      // 勢いよく出て、すっと消える
                cell.localScale = Vector3.one * (1f + 0.55f * e);
                cg.alpha = 1f - e;
                yield return null;
            }
            cell.localScale = Vector3.one * 1.55f;
            cg.alpha = 0f;
        }

        /// <summary>ナビのバッジを全部もとに戻す（1G の始め）。</summary>
        private void ResetNaviBadges()
        {
            for (int i = 0; i < 3; i++)
            {
                _naviPopped[i] = false;
                if (_naviCells[i] == null) continue;
                _naviCells[i].localScale = Vector3.one;
                var cg = _naviCells[i].GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
            }
        }

        /// <summary>
        /// 次のルートの達成条件を出す。基本ルートと、一度でも行ったことのある先は中身を見せ、
        /// まだ分からない先は ？？？ のままにする。
        /// </summary>
        /// <summary>錠前と鎖をゆっくり呼吸させる（止まっていることを目で分からせる）。</summary>
        private void UpdateHoldPulse()
        {
            if (_holdBox == null || !_holdBox.activeSelf) return;
            _holdPulse += Time.deltaTime;
            float k = 0.5f + 0.5f * Mathf.Sin(_holdPulse * 2.2f);
            if (_holdText != null) _holdText.color = Color.Lerp(UiSkin.GoldDeep, ColGold, k);
            if (_holdLock != null) _holdLock.color = Color.Lerp(new Color(0.78f, 0.80f, 0.86f), Color.white, k);
            float shimmer = 0.72f + 0.28f * k;
            for (int i = 0; i < _holdLinks.Count; i++)
            {
                if (_holdLinks[i] == null) continue;
                // 端から順に光が流れる
                float phase = Mathf.Repeat(_holdPulse * 0.9f - i * 0.12f, 2f);
                float lit = phase < 0.35f ? 1f - phase / 0.35f : 0f;
                float v = Mathf.Clamp01(shimmer + lit * 0.35f);
                _holdLinks[i].color = new Color(v, v * 1.03f, v * 1.10f, 0.95f);
            }
        }

        private float _holdPulse;

        private void RefreshRoutePanel(StageNode node)
        {
            if (_routeBox == null) return;
            var conds = node?.routeConditions;
            if (conds == null || conds.Count == 0) { _routeBox.SetActive(false); return; }
            _routeBox.SetActive(true);

            // 強い条件（上の道）から順に 3 つまで
            var list = new System.Collections.Generic.List<RouteCondition>(conds);
            list.Sort((a, b) => b.priority.CompareTo(a.priority));
            var cfg = _m.Config.adventure;

            for (int i = 0; i < 3; i++)
            {
                bool has = i < list.Count;
                _routeArrow[i].gameObject.SetActive(has);
                _routeText[i].gameObject.SetActive(has);
                _routeBar[i].gameObject.SetActive(has);
                if (!has) continue;

                var c = list[i];
                bool met = AdventureDirector.Meets(c, _m.Adv, _m.Credit, _m.Wallet.Souls, _m.PlayerLevel);
                bool basic = c.priority <= 20 && c.priority > 10;              // 基本ルート
                bool been = _m.Adv.visited.Contains(c.to);                     // 行ったことがある先
                bool known = (basic || been || met) && !(c.hidden && !met);

                // 上 / 同じ高さ / 下 を矢印で
                _routeArrow[i].text = c.priority >= 30 ? "▲" : c.priority <= 10 ? "▼" : "▶";
                var tone = c.priority >= 30 ? ColGold : c.priority <= 10 ? Hex("#7f8aa3") : ColText;
                _routeArrow[i].color = known ? tone : UiSkin.TextDim;

                if (!known)
                {
                    _routeText[i].text = "？？？";
                    _routeText[i].color = UiSkin.TextDim;
                    _routeBar[i].color = ColBtn;
                    continue;
                }
                _routeText[i].text = ShortCondition(c);
                _routeText[i].color = met ? ColGold : ColText;
                var dst = cfg?.Find(c.to);
                _routeBar[i].color = met ? ColGold : (dst != null ? Hex(AdventureDirector.ColorFor(dst)) : ColBtn);
            }
        }

        /// <summary>条件を 1 行に縮める（「スイカ 1/3」の形）。</summary>
        private string ShortCondition(RouteCondition c)
        {
            if (c.counters != null)
                foreach (var kv in c.counters)
                {
                    if (kv.Value <= 0) continue;
                    int cur = Mathf.Min(AdventureDirector.GetCount(_m.Adv, kv.Key), kv.Value);
                    return $"{AdventureDirector.CounterName(kv.Key)} {cur}/{kv.Value}";
                }
            if (c.state != null)
                foreach (var kv in c.state)
                    if (kv.Value > 0) return $"{AdventureDirector.StateName(kv.Key)} {kv.Value}";
            return AdventureDirector.DescribeCondition(c);
        }

        private void RefreshNavi()
        {
            RefreshTech();
            // AT 道中の押し順ナビ: 正解のリールに「1」を出して教える（隠さない）
            if (_m.Navi2.Active && _m.IsGameActive)
            {
                _naviBox.SetActive(true);
                int pressed2 = _m.PressOrder.Count;
                for (int i = 0; i < 3; i++)
                {
                    if (_naviPopped[i]) continue;   // 押して消したバッジは戻さない
                    bool isFirst = i == _m.Navi2.first;
                    bool done = pressed2 > 0;
                    _naviLabels[i].text = isFirst ? "1" : "-";
                    _naviLabels[i].color = isFirst ? ColGold : ColTextSub;
                    _naviBg[i].color = isFirst ? ColGold : ColBtnDisabled;
                    _naviGlow[i].color = isFirst && !done ? new Color(1f, 0.85f, 0.3f, 0.55f) : new Color(0, 0, 0, 0);
                }
                return;
            }
            if (!_m.Navi.Active || !_m.IsGameActive) { _naviBox.SetActive(false); return; }
            _naviBox.SetActive(true);
            var n = _m.Navi;
            int pressed = _m.PressOrder.Count;
            // 数字 + 位置 + 色の 3 重表現（§16.3）。1 = 最初に押すリール、? = 択、○ = 正解、× = 外れ
            var blue = new Color(0.3f, 0.55f, 1f);
            for (int i = 0; i < 3; i++)
            {
                if (_naviPopped[i]) continue;   // 押して消したバッジは戻さない
                string txt; Color ring, fg, glow;
                if (i == n.first) { txt = "1"; ring = ColGold; fg = ColGold; glow = new Color(1f, 0.85f, 0.3f, pressed == 0 ? 0.45f : 0.12f); }
                else { txt = "?"; ring = blue; fg = ColText; glow = new Color(0.4f, 0.6f, 1f, pressed == 1 && n.InChoice ? 0.45f : 0.2f); }
                if (pressed >= 2 && _m.CurrentCommand != BellCommand.None)
                {
                    bool ok = _m.CurrentCommand == BellCommand.Success;
                    if (i == n.correctReel) { txt = "○"; ring = ok ? ColGold : ColBtnDisabled; fg = ok ? ColGold : ColTextSub; glow = ok ? new Color(1f, 0.85f, 0.3f, 0.6f) : new Color(0, 0, 0, 0); }
                    else if (i != n.first) { txt = ok ? "-" : "×"; ring = ok ? ColBtnDisabled : ColAccent; fg = ok ? ColTextSub : ColAccent; glow = ok ? new Color(0, 0, 0, 0) : new Color(1f, 0.3f, 0.4f, 0.5f); }
                    else { fg = ColTextSub; ring = ColBtnDisabled; glow = new Color(0, 0, 0, 0); }
                }
                else if (pressed == 1 && !n.InChoice)
                {
                    if (i != n.first) { txt = "-"; ring = ColBtnDisabled; fg = ColTextSub; glow = new Color(0, 0, 0, 0); }
                }
                _naviLabels[i].text = txt; _naviLabels[i].color = fg; _naviBg[i].color = ring; _naviGlow[i].color = glow;
            }
        }

        /// <summary>択の最中: BGM がこもり（水中）、画面が少し沈む＝集中。</summary>
        private void EnterFocus()
        {
            _audio.SetFocus(true);
            PlayCharacter("focus");
            _darken.color = new Color(0.02f, 0.05f, 0.12f, 0.35f);
            UiFx.Burst(_charRt, UiFx.Preset.Focus, new Vector2(0, 40));
            _hint.text = "……";
            _hint.color = ColTextSub;
        }

        private void ExitFocus()
        {
            _audio.SetFocus(false);
            if (_m.PrecursorRemaining == 0) _darken.color = new Color(0, 0, 0, 0);
        }

        private IEnumerator NaviSuccessRoutine()
        {
            ExitFocus();
            _audio.NaviSuccess();
            UiFx.Burst(_area, UiFx.Preset.SuccessStars, new Vector2(0, 20));
            UiFx.Ring(_area, new Color(1f, 0.9f, 0.4f, 0.9f), 50, 600, 0.6f);
            UiFx.PopText(_area, "SUCCESS!", ColGold, 34, new Vector2(0, 30));
            _hint.text = "サクセス！";
            _hint.color = ColGold;
            // 白フラッシュ → 金の余韻
            float t = 0;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                float a = t < 0.1f ? 0.7f : 0.35f * (1f - (t - 0.1f) / 0.5f);
                _redGlow.color = new Color(1f, 0.95f, 0.6f, Mathf.Max(0, a));
                yield return null;
            }
            _redGlow.color = new Color(1f, 0.1f, 0.1f, 0f);
        }

        private IEnumerator NaviFailRoutine()
        {
            ExitFocus();
            _audio.NaviFail();
            UiFx.Burst(_charRt, UiFx.Preset.RedShards, new Vector2(10, 30));
            UiFx.Slash(_charRt, 30f, 200f, new Color(1f, 0.3f, 0.3f));
            _hint.text = "……外した";
            _hint.color = ColAccent;
            // 敵の攻撃を食らう: 敵が前に出て、キャラが仰け反り、赤い縁光
            StartCoroutine(Effects.Hit(_enemyRt, 0.35f));
            StartCoroutine(Effects.Miss(_charRt, 0.6f));
            StartCoroutine(Effects.Shake(_stage, 0.3f, 6f));
            yield return Effects.RedGlow(_redGlow, 0.8f);
        }

        // ---------------------------------------------------------- PRECURSOR
        /// <summary>
        /// 前兆（ENEMY 当選〜出現までの煽り）。stage 0=当選G（かすかな違和感）, 1..N-1=段階的に強く。
        ///   ・画面が段階的に暗くなる
        ///   ・右端から敵のシルエットがじわじわ近づく
        ///   ・キャラが足を止めて構える（idle）
        ///   ・段階ごとに小さな揺れ＋低い音
        /// </summary>
        private void StartPrecursor(int stage)
        {
            int total = Mathf.Max(1, _m.PrecursorTotal);
            float k = Mathf.Clamp01((stage + 1f) / (total + 1f));   // 0.25, 0.5, 0.75 ...
            if (_precursorRoutine != null) StopCoroutine(_precursorRoutine);
            _precursorRoutine = StartCoroutine(PrecursorRoutine(k, stage));
        }

        private IEnumerator PrecursorRoutine(float k, int stage)
        {
            PlayCharacter("idle");
            _bg.IsWalking = false; _bgOuter.IsWalking = false;
            _audio.EnemyEscape();   // 低いポップ音を「気配」に流用
            StartCoroutine(Effects.Shake(_stage, 0.25f, 2f + 4f * k));
            // 暗幕とシルエットを目標値へ 0.4 秒でなめらかに
            float a0 = _darken.color.a, a1 = 0.55f * k;
            var p0 = _shadowRt.anchoredPosition;
            float baseX = AreaW * 0.5f - 0.2f * AreaW - 75f;    // 敵の定位置
            var p1 = new Vector2(Mathf.Lerp(AreaW * 0.5f + 90f, baseX + 40f, k), p0.y);
            float s0 = _shadowImg.color.a, s1 = 0.85f * k;
            float t = 0;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0, 1, t / 0.4f);
                _darken.color = new Color(0, 0, 0, Mathf.Lerp(a0, a1, u));
                _shadowRt.anchoredPosition = Vector2.Lerp(p0, p1, u);
                _shadowImg.color = new Color(0, 0, 0, Mathf.Lerp(s0, s1, u));
                yield return null;
            }
            // 前兆のセリフは会話 UI（リールのすぐ上）に出す。色は付けない（示唆の色は期待度専用）
            ShowPrecursorLine(stage);
            // 段階が上がるほど暗幕が脈打つ
            float pulse = 0.08f * k;
            while (true)
            {
                _darken.color = new Color(0, 0, 0, a1 + pulse * Mathf.Sin(Time.time * (3f + 4f * k)));
                yield return null;
            }
        }

        private void StopPrecursor()
        {
            if (_precursorRoutine != null) { StopCoroutine(_precursorRoutine); _precursorRoutine = null; }
            _darken.color = new Color(0, 0, 0, 0);
            _shadowImg.color = new Color(0, 0, 0, 0);
            _shadowRt.anchoredPosition = new Vector2(AreaW * 0.5f + 90f, _shadowRt.anchoredPosition.y);
            _hint.text = "";
            HideDialogue();
        }

        /// <summary>前兆の段階セリフを会話 UI に出す（stage は 0 始まり）。</summary>
        private void ShowPrecursorLine(int stage)
        {
            var line = HeroDirector.PrecursorLine(_m.Config.hero, stage + 1, _fxRng);
            if (string.IsNullOrEmpty(line)) return;
            var tv = _m.Config.travelers ?? TravelerConfig.Default();
            if (_dialogRoutine != null) StopCoroutine(_dialogRoutine);
            _dialogRoutine = StartCoroutine(HeroLineRoutine(tv.heroName ?? "主人公", line, 2.8f));
        }

        // ------------------------------------------------------ ENGAGE HINTS
        /// <summary>案B: 1G目=敵の色 / 2G目=セリフ / 3G目=停止音。レバーオン時に抽選して適用。</summary>
        private void ApplyEngageHint()
        {
            _hotStopSound = false;
            _stopCountThisGame = 0;
            if (!_m.IsTier2 || !_m.EnemyActive || _m.BonusMode != BonusMode.NORMAL) { if (_m.PrecursorRemaining == 0) _hint.text = ""; return; }
            var h = EngageDirector.Roll(_m.Config.engageHints, _m.Tier2SpinCount, _m.EnemyDefeatWon, new SystemRandom());
            switch (h.stage)
            {
                case 1:
                    SetEnemyColor(h.color);
                    _hint.text = "";
                    break;
                case 2:
                    switch (h.serif)
                    {
                        case "weak": _hint.text = "「…なにか来る」"; _hint.color = ColTextSub; break;
                        case "mid": _hint.text = "「もらった…！」"; _hint.color = ColAccent; break;
                        case "strong": _hint.text = "「激熱ッ！」"; _hint.color = ColGold; StartCoroutine(Effects.RedGlow(_redGlow, 2f)); break;
                        default: _hint.text = ""; break;
                    }
                    break;
                case 3:
                    _hotStopSound = h.hotSound;
                    _hint.text = "";
                    break;
            }
        }

        /// <summary>敵の色（示唆）。rainbow は色相を回す。</summary>
        private void SetEnemyColor(string key)
        {
            if (_enemyRainbow != null) { StopCoroutine(_enemyRainbow); _enemyRainbow = null; }
            switch (key)
            {
                case "blue": _enemyImg.color = new Color(0.6f, 0.75f, 1f); break;
                case "yellow": _enemyImg.color = new Color(1f, 0.95f, 0.5f); break;
                case "green": _enemyImg.color = new Color(0.6f, 1f, 0.6f); break;
                case "red": _enemyImg.color = new Color(1f, 0.45f, 0.45f); break;
                case "rainbow": _enemyRainbow = StartCoroutine(Effects.Rainbow(_enemyImg)); break;
                default: _enemyImg.color = _enemyBaseColor; break;
            }
        }

        // --------------------------------------------------------- CHARACTER
        /// <summary>
        /// 主人公の動きを切り替える。
        ///   ループ: idle / walk / focus
        ///   一度きり: attack-f1 / attack-f2 / attack-f3-4 / slash / cast / guard / hit / victory
        /// 一度きりの動きは再生が終わるまで、あとから来た walk / idle で上書きしない。
        /// </summary>
        private void PlayCharacter(string type)
        {
            if (type == "walk" && (_m.EnemyActive || _m.PrecursorRemaining > 0)) type = "idle";
            bool loop = type == "idle" || type == "walk" || type == "focus";
            if (loop && Time.time < _charHoldUntil) return;       // 決め動作の途中は邪魔しない
            if (loop && type == _charState) { _bg.IsWalking = type == "walk"; _bgOuter.IsWalking = _bg.IsWalking; return; }

            _bg.IsWalking = type == "walk"; _bgOuter.IsWalking = _bg.IsWalking;
            _charState = loop ? type : "";
            switch (type)
            {
                case "idle": _charAnim.Play(_idleFrames, 1.6f, true); break;
                case "walk": _charAnim.Play(_walkFrames, 0.8f, true); break;
                case "focus": PlayOnce(_focusFrames, 1.2f, true); break;
                case "attack-f1": if (_attackFrames.Length > 0) _charAnim.Show(_attackFrames[0]); break;
                case "attack-f2": if (_attackFrames.Length > 1) _charAnim.Show(_attackFrames[1]); break;
                case "attack-f3-4":
                    if (_attackFrames.Length > 3) PlayOnce(new[] { _attackFrames[2], _attackFrames[3] }, 0.2f, false);
                    break;
                case "slash": PlayOnce(_slashFrames, 0.4f, false); break;
                case "cast": PlayOnce(_castFrames, 0.6f, false); break;
                case "guard": PlayOnce(_guardFrames, 0.5f, false); break;
                case "hit": PlayOnce(_hitFrames, 0.45f, false); break;
                case "victory": PlayOnce(_victoryFrames, 0.9f, false); break;
            }
        }

        /// <summary>一度きりの動きを流し、終わるまでループ系に奪われないようにする。</summary>
        private void PlayOnce(Sprite[] frames, float seconds, bool loop)
        {
            if (frames == null || frames.Length == 0) return;
            _charAnim.Play(frames, seconds, loop);
            _charHoldUntil = Time.time + (loop ? 0f : seconds);
        }

        /// <summary>敵テーブルの見た目（大きさ・基本色）を反映する。中ボスは大きく、色が付く。</summary>
        private void ApplyEnemyLook(EnemyTable table)
        {
            float s = table != null && table.scale > 0.1f ? table.scale : 1f;
            _enemyRt.sizeDelta = EnemyBaseSize * s;
            _shadowRt.sizeDelta = EnemyBaseSize * s;
            _enemyBaseColor = Color.white;
            if (table != null && !string.IsNullOrEmpty(table.color) && ColorUtility.TryParseHtmlString(table.color, out var c)) _enemyBaseColor = c;
        }

        private void ShowEnemy(EnemyTable table)
        {
            if (_traveler != null) { Destroy(_traveler.gameObject); _traveler = null; }
            HideDialogue();
            _enemyImg.sprite = ArtLoader.EnemySprite(table?.enemyType);
            ApplyEnemyLook(table);
            SetEnemyColor("none");
            _hint.text = "";
            if (_enemyIdle != null) StopCoroutine(_enemyIdle);
            StartCoroutine(SpawnEnemyRoutine());
            // 中ボスは名乗りを上げる（通常の雑魚とはっきり区別する）
            if (table != null && table.IsBoss) StartCoroutine(SlamTitle($"中ボス  {table.name}", Hex("#ff9a3c"), 1.5f, 46));
        }

        private IEnumerator SpawnEnemyRoutine()
        {
            yield return new WaitForSeconds(0.5f);
            _audio.EnemyAppearStart();                       // 接近のヒュゥゥ（SlideIn と同じ 0.5 秒）
            yield return Effects.SlideIn(_enemyRt, _enemyCg);
            _audio.EnemyAppearLand();                        // 着地のドンッ＋スティング
            UiFx.Burst(_enemyRt, UiFx.Preset.Dust, new Vector2(0, -60));
            UiFx.Ring(_enemyRt, new Color(1f, 0.3f, 0.3f, 0.8f), 40, 240, 0.4f);
            _enemyIdle = StartCoroutine(Effects.IdleBob(_enemyRt));
            StartCoroutine(EngageTitleRoutine());            // 「ENEMY ENGAGE!」叩きつけ（案1）
        }

        /// <summary>
        /// 案1「叩きつけ」: 黒帯が横切り、文字が 1.5 倍から着地。着地で揺れ＋赤フラッシュ。約 0.7 秒で出て 1.5 秒保持。
        /// 表示域の中だけに出す（リール・ナビには被せない §16.3）。
        /// </summary>
        private IEnumerator EngageTitleRoutine()
        {
            // 黒帯（表示域の幅より広く、左右は透明にフェード）
            var band = UiSkin.Rect(_area, "EngageBand", Vector2.zero, new Vector2(AreaW * 1.2f, 96));
            var bandImg = UiSkin.Img(band, "Bg", Vector2.zero, new Vector2(AreaW * 1.2f, 96), null, new Color(0, 0, 0, 0.78f));
            UiSkin.Img(band, "LineTop", new Vector2(0, 47), new Vector2(AreaW * 1.2f, 2), null, new Color(1f, 0.3f, 0.42f, 0.9f));
            UiSkin.Img(band, "LineBottom", new Vector2(0, -47), new Vector2(AreaW * 1.2f, 2), null, new Color(1f, 0.3f, 0.42f, 0.9f));
            band.localScale = new Vector3(0f, 1f, 1f);
            var bandCg = band.gameObject.AddComponent<CanvasGroup>();
            bandCg.blocksRaycasts = false;

            // 文字（影付き）
            var title = UiFactory.Label(_area, "EngageTitle", new Vector2(0, 2), new Vector2(AreaW, 96), "ENEMY ENGAGE!", 58, TextAnchor.MiddleCenter, Color.white);
            title.fontStyle = FontStyle.Bold;
            var sh = title.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.48f, 0f, 0.09f, 1f); sh.effectDistance = new Vector2(0, -3);
            var titleCg = title.gameObject.AddComponent<CanvasGroup>();
            titleCg.alpha = 0f; titleCg.blocksRaycasts = false;
            var glow = UiSkin.Img(_area, "EngageGlow", new Vector2(0, 2), new Vector2(520, 180), UiSkin.Glow(96), new Color(1f, 0.3f, 0.42f, 0f));
            glow.transform.SetSiblingIndex(title.transform.GetSiblingIndex());

            // 1) 帯が横切る 0.18s
            float t = 0;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.18f);
                band.localScale = new Vector3(1f - Mathf.Pow(1f - u, 3f), 1f, 1f);
                yield return null;
            }
            band.localScale = Vector3.one;
            // 2) 文字が 1.5 倍から着地 0.16s
            titleCg.alpha = 1f;
            t = 0;
            while (t < 0.16f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.16f);
                float s = Mathf.Lerp(1.5f, 1f, 1f - Mathf.Pow(1f - u, 2f));
                title.rectTransform.localScale = Vector3.one * s;
                yield return null;
            }
            title.rectTransform.localScale = Vector3.one;
            // 3) 着地: 揺れ＋赤フラッシュ＋文字の周りの発光
            StartCoroutine(Effects.Shake(_stage, 0.3f, 6f));
            StartCoroutine(EdgeGlow(ColAccent, 0.5f, false));
            UiFx.Burst(_area, UiFx.Preset.RedShards, new Vector2(0, 0));
            t = 0;
            while (t < 1.5f)
            {
                t += Time.deltaTime;
                glow.color = new Color(1f, 0.3f, 0.42f, 0.45f * Mathf.Exp(-t * 1.6f) + 0.08f);
                yield return null;
            }
            // 4) 消える 0.3s
            t = 0;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float a = 1f - Mathf.Clamp01(t / 0.3f);
                titleCg.alpha = a; bandCg.alpha = a; glow.color = new Color(1f, 0.3f, 0.42f, 0.08f * a);
                yield return null;
            }
            Destroy(band.gameObject); Destroy(title.gameObject); Destroy(glow.gameObject);
        }

        private void HideEnemy()
        {
            if (_enemyIdle != null) { StopCoroutine(_enemyIdle); _enemyIdle = null; }
            if (_enemyRainbow != null) { StopCoroutine(_enemyRainbow); _enemyRainbow = null; }
            _enemyCg.alpha = 0f;
            _enemyRt.localScale = Vector3.one;
            _dustRt.gameObject.SetActive(false);
        }

        // --------------------------------------------------------------- INPUT
        private void Update()
        {
            // Play 中にスクリプトが再コンパイルされると非シリアライズ参照が消える。その状態で回さない（NRE の連打防止）
            if (_m == null || _creditNum == null) return;
            var kb = Keyboard.current;
            if (kb != null && !_inputLocked)
            {
                if (kb.leftCtrlKey.wasPressedThisFrame || kb.rightCtrlKey.wasPressedThisFrame) OnBetClicked();
                if (kb.spaceKey.wasPressedThisFrame) OnSpaceStep();
                if (kb.zKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) StopReel(0);
                if (kb.xKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) StopReel(1);
                if (kb.cKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) StopReel(2);
                if (kb.aKey.wasPressedThisFrame) CycleAuto();
                if (kb.bKey.wasPressedThisFrame) { _audio.ToggleBgm(); _audio.UiPop(); SaveData.SaveAudio(_audio); }
                if (kb.dKey.wasPressedThisFrame) ToggleDebug();
                if (kb.gKey.wasPressedThisFrame) ToggleGraph();
                if (kb.mKey.wasPressedThisFrame) ToggleMap();
                if (kb.eKey.wasPressedThisFrame) ToggleEquip();
                if (kb.escapeKey.wasPressedThisFrame) CloseModals();
                if (kb.rKey.wasPressedThisFrame && !_m.IsGameActive) ResetSave();
                if ((kb.numpadPlusKey.wasPressedThisFrame || kb.semicolonKey.wasPressedThisFrame) && !_m.IsGameActive) { _m.Credit += 100; _audio.UiPop(); RefreshUi(); }
                for (int i = 0; i < 6; i++)
                {
                    var key = kb[Key.F1 + i];
                    if (key.wasPressedThisFrame && !_m.IsGameActive) { _m.SetSetting(i + 1); _audio.UiPop(); RefreshUi(); }
                }
            }
            UpdateAtFrame();
            UpdateHoldPulse();
            // 長押しの判定は入力ロック中も回す。ロック中に離してもオートが解除されるように
            if (kb != null) UpdateSpaceHold(kb, !_inputLocked);
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 1e-4f), 0.1f);
            if (Time.frameCount % 15 == 0 && _debug != null) RefreshUi();
            if (_messageFlashUntil > 0f)
            {
                float t = Mathf.PingPong(Time.time * 6f, 1f);
                _message.color = Color.Lerp(ColText, ColGold, t);
                if (Time.time > _messageFlashUntil) { _messageFlashUntil = 0f; _message.color = ColText; }
            }
        }

        private void ResetSave()
        {
            SaveData.Clear();
            int setting = _m.Setting;
            _m = GameDataLoader.CreateMachine(new SystemRandom(), setting);
            HideEnemy();
            StopPrecursor();
            _lastPayout = 0;
            _lastWasReplay = false;
            _graph?.ResetTo(_m.Credit);
            _mini?.ResetTo(_m.Credit);
            _runBaseCredit = _m.Credit;
            RedrawMini();
            SetMessage("SAVE RESET", true);
            _audio.UiPop();
            PlayCharacter("walk");
            RefreshUi();
        }

        // ---------------------------------------------------------------- FLOW
        /// <summary>Space: 止まっていれば BET+レバー、回転中なら左から順に停止。</summary>
        private void OnSpaceStep()
        {
            if (!_m.IsGameActive) { OnBetClicked(); return; }
            // AT の押し順ナビ中: 「1」を先に、残りは左から順
            if (_m.Navi2.Active)
            {
                if (_m.PressOrder.Count == 0) { StopReel(_m.Navi2.first); return; }
                for (int i = 0; i < 3; i++)
                    if (_reels[i].IsSpinning && _m.Stopped[i] == null) { StopReel(i); return; }
                return;
            }
            // ベル択ナビ中: 「1」を先に、残りの「?」はランダム。それ以外は左から順
            if (_m.Navi.Active)
            {
                int pressed = _m.PressOrder.Count;
                if (pressed == 0) { StopReel(_m.Navi.first); return; }
                var rest = new System.Collections.Generic.List<int>();
                for (int i = 0; i < 3; i++) if (_reels[i].IsSpinning && _m.Stopped[i] == null) rest.Add(i);
                if (rest.Count > 0) StopReel(rest[_fxRng.Next(rest.Count)]);
                return;
            }
            for (int i = 0; i < 3; i++)
                if (_reels[i].IsSpinning && _m.Stopped[i] == null) { StopReel(i); return; }
        }

        private void OnBetClicked()
        {
            if (_m.IsGameActive || _inputLocked || _leaving) return;
            bool wasReplay = _m.IsReplay;
            if (!_m.MaxBet())
            {
                SetMessage("エンバーが足りません", false, ColAccent);
                return;
            }
            if (!wasReplay) _audio.Bet();
            Lever();
        }

        private void Lever()
        {
            _lastPayout = 0;
            CloseModals();
            _dustRt.gameObject.SetActive(false);
            if (_m.EnemyActive) _enemyCg.alpha = 1f;   // JS onLever: 潰した敵を戻す
            _charRt.localRotation = Quaternion.identity;
            ExitFocus();
            _m.AutoPlaying = _autoMode;    // オート中は技術介入の課題を出さない
            var legacyHint = _m.Lever();   // 抽選はレバーオン時点で確定（旧示唆は使わない）
            ResetNaviBadges();             // 前のGのナビ（○×や消えたバッジ）を持ち越さない
            RefreshNavi();
            var hint = HintKind.None;
            ApplyEngageHint();
            RollPrecog();                  // 事前察知（役の予告）。リール始動と同時に出す
            ShowBonusAnnounce();           // ボーナス成立の前兆（3〜6G）。最後のGは擬似遊技
            if (_m.AtEntryStage > 0) ShowAtEntry(_m.AtEntryStage, _m.AtEntryTotal);   // 洞窟へ近づく前兆
            // ボーナス中のベル（15枚）は 3 体斬りの演出にする。レバーオンで敵が 3 体出る
            ClearBellShow();
            // エンゲージ中は敵との戦闘が主役なので 3 体斬りは出さない
            if (_m.BonusMode != BonusMode.NORMAL && _m.CurrentFlag.IsBell() && !_m.IsTier2 && !_m.EnemyActive && _m.PrecursorRemaining == 0 && !_m.InAt) StartBonusBellShow();
            if (_m.AtJustStarted) StartCoroutine(AtStartRoutine());                   // 洞窟に入った

            // ウェイト: リールは即回転。停止ボタンだけ「前回レバーから SpinWaitSeconds」まで無効
            float waitSec = _autoMode ? SpinWaitSeconds / Mathf.Max(1, _autoSpeed) : SpinWaitSeconds;
            float unlockAt = _lastSpinStartTime + waitSec;
            _stopUnlockTime = Mathf.Max(Time.time, unlockAt);
            _lastSpinStartTime = Time.time;
            StartReels(hint);
            if (_stopUnlockTime > Time.time + 0.02f)
            {
                if (_waitRoutine != null) StopCoroutine(_waitRoutine);
                _waitRoutine = StartCoroutine(WaitRoutine(_stopUnlockTime - Time.time));
            }
        }

        private IEnumerator WaitRoutine(float remain)
        {
            SetMessage("WAIT", false, ColTextSub);
            RefreshUi();
            OnWaitStart?.Invoke(remain);
            yield return new WaitForSeconds(remain);
            _waitRoutine = null;
            if (_m.IsGameActive) SetMessage($"{_m.SpinCount}G");
            RefreshUi();
        }

        private bool StopsLocked => Time.time < _stopUnlockTime;

        private void StartReels(HintKind hint)
        {
            _audio.SpinStart();
            if (!StopsLocked) SetMessage($"{_m.SpinCount}G");
            // JS drawLottery: ベル当選なら attack-f1、それ以外は walk
            PlayCharacter(_m.CurrentFlag.IsBell() ? "attack-f1" : "walk");
            foreach (var r in _reels) r.StartSpin();
            RefreshUi();
            if (_autoMode) StartAuto();
        }

        private void StopReel(int i) => StopReel(i, false);

        /// <summary>auto=true は擬似遊技など機械側の停止。false（手動）は擬似遊技中は無視する。</summary>
        private void StopReel(int i, bool auto)
        {
            if (!_m.IsGameActive || StopsLocked || !_reels[i].IsSpinning || _m.Stopped[i] != null) return;
            if (_m.PseudoPlay && !auto) return;   // 擬似遊技中は手動停止を受け付けない
            if (_bellShowActive && _bellShowStop < 3) StartCoroutine(BellShowDefeat(_bellShowStop++));
            var res = _m.Stop(i, _reels[i].TopIndex);
            _reels[i].StopAt(res.stopIndex, res.slip);
            _btnStops[i].interactable = false;
            if (_reels[i].LastWasPullIn)
            {
                // 引き込み開始: そのリールの窓が金色に光り、吸い込みの音が鳴る
                var reelRt = _reels[i].GetComponent<RectTransform>();
                UiFx.Ring(reelRt, new Color(1f, 0.85f, 0.3f, 0.7f), 60, 220, 0.5f);
                _audio.ReelPullIn();
            }
            if (_m.Tech.Active && i == _m.Tech.reel) RefreshTech();
            if (_m.Navi.Active)
            {
                if (_m.PressOrder.Count == 1 && _m.Navi.InChoice) EnterFocus();
                else if (_m.PressOrder.Count == 2 && _m.CurrentCommand == BellCommand.Success) StartCoroutine(NaviSuccessRoutine());
                else if (_m.PressOrder.Count == 2 && _m.CurrentCommand == BellCommand.Fail) StartCoroutine(NaviFailRoutine());
            }
            // 押したバッジは膨らんで消える（打感。次のGのナビと混ざらないように）
            if ((_m.Navi.Active || _m.Navi2.Active) && !_naviPopped[i])
            {
                _naviPopped[i] = true;
                StartCoroutine(PopNaviBadge(i));
            }
            RefreshNavi();

            // JS onStop: ベル時の第1/第2停止演出
            int pressed = 0;
            for (int k = 0; k < 3; k++) if (_m.Stopped[k] != null) pressed++;
            if (_m.CurrentFlag.IsBell())
            {
                if (pressed == 1) PlayCharacter("attack-f2");
                else if (pressed == 2)
                {
                    PlayCharacter("attack-f3-4");
                    _audio.Attack();
                    if (_m.EnemyActive) _dustRt.gameObject.SetActive(true);   // 敵は消さない（決着は3G目の判定のみ）
                }
            }
        }

        private void OnReelStopped(ReelView reel)
        {
            if (reel.LastWasPullIn)
            {
                // 引き込み完了: ガコンと重い停止音＋金の火花
                _audio.ReelPullInLand();
                UiFx.Burst(reel.GetComponent<RectTransform>(), UiFx.Preset.Sparks);
                UiFx.Ring(reel.GetComponent<RectTransform>(), new Color(1f, 0.9f, 0.5f, 0.9f), 30, 180, 0.35f);
            }
            else if (_hotStopSound)
            {
                _audio.StopHot(_stopCountThisGame++);
                UiFx.Burst(reel.GetComponent<RectTransform>(), UiFx.Preset.Sparks);
            }
            else _audio.Stop();
            foreach (var r in _reels) if (r.IsSpinning) return;
            Evaluate();
        }

        private void Evaluate()
        {
            // 期待度のランクは判定前に控えておく（昇格したかを比べるため）
            string rankBefore = AtDirector.RankFor(_m.Config.atExpect, _m.AtExpectPercent)?.name;
            var r = _m.Evaluate();
            _lastPayout = r.win.payout;
            _lastWasReplay = r.win.isReplay;
            if (_m.PrecursorRemaining == 0) _hint.text = "";
            PlayCharacter("walk");

            if (r.bonusStarted)
            {
                SetMessage($"BONUS START! 0 / {_m.BonusPayoutTarget}", true, Color.yellow);
                PlayCharacter("victory");
                StartCoroutine(Effects.Watermelon(_charRt));   // anim-bonus の代用（ジャンプ）
                if (r.win.winType == WinType.BIG) StartCoroutine(BigBonusStartRoutine());
                else _audio.Win();
                SaveData.Save(_m, _audio);
                RefreshUi();
                if (r.win.winType != WinType.BIG && _autoMode) StartAuto();
                return;
            }

            if (r.win.payout > 0)
            {
                // 獲得音: ボーナス中（15枚）はデュルデュルの連打、通常時は控えめな音
                if (_m.BonusMode != BonusMode.NORMAL) _audio.Payout(r.win.payout); else _audio.PayoutSmall(r.win.payout);
                // リールから湧いた灯火が、エンバーの数字へ吸い込まれる
                int emberDrops = Mathf.Clamp(2 + r.win.payout / 4, 3, 14);
                UiFx.Absorb(_reels[1].GetComponent<RectTransform>(), _creditNum.rectTransform, UiFx.Preset.EmberSoul, emberDrops, 0.78f);
                UiFx.Burst(_payoutNum.rectTransform, UiFx.Preset.Coins, new Vector2(0, -10));
                UiFx.PopText(_payoutNum.rectTransform, $"+{r.win.payout}", ColGold, 24, new Vector2(0, 20));
                RoleFx(r.win.winType, CellMaskFor(r.win));
                switch (r.win.winType)
                {
                    case WinType.BELL:
                        // 1〜2G目に「死んだように見える」演出は禁止。ヒット＋砂煙のみ。決着は3G目終了時の判定だけ
                        if (_m.EnemyActive && r.command != BellCommand.Fail) StartCoroutine(BellHit());
                        break;
                    case WinType.CHERRY:
                        PlayCharacter("slash");
                        StartCoroutine(Effects.Cherry(_charRt));
                        if (_m.EnemyActive) { StartCoroutine(Effects.Hit(_enemyRt)); UiFx.Slash(_enemyRt, 10f, 200f, new Color(1f, 0.5f, 0.7f)); }
                        else UiFx.Slash(_charRt, 10f, 180f, new Color(1f, 0.5f, 0.7f));
                        break;
                    case WinType.WATERMELON:
                        PlayCharacter("cast");
                        StartCoroutine(Effects.Watermelon(_charRt));
                        if (_m.EnemyActive) StartCoroutine(Effects.Hit(_enemyRt));
                        StartCoroutine(Effects.Shake(_stage));
                        StartCoroutine(DelayedFx(0.36f, () => { UiFx.Burst(_charRt, UiFx.Preset.Splash, new Vector2(0, -90)); UiFx.Ring(_charRt, new Color(0.6f, 1f, 0.5f, 0.8f), 40, 260, 0.45f); }));
                        break;
                }
                SetMessage(_m.BonusMode != BonusMode.NORMAL ? $"BONUS: {_m.BonusEarned} / {_m.BonusPayoutTarget}" : $"WIN! +{r.win.payout}", true);
            }
            else if (r.win.isReplay)
            {
                _audio.Replay();
                UiFx.Ring(_charRt, new Color(0.5f, 0.8f, 1f, 0.7f), 60, 160, 0.4f);
                RoleFx(WinType.REPLAY, CellMaskFor(r.win));
                // 主人公のリプレイ行動（弁当・水・背伸び…）。敵がいる時・ボーナス中は従来の小さな動きだけ
                string act = (_m.EnemyActive || _m.BonusMode != BonusMode.NORMAL) ? "none" : HeroDirector.RollReplayAction(_m.Config.hero, _fxRng);
                if (act == "none") StartCoroutine(Effects.Replay(_charRt));
                else StartCoroutine(ReplayAction(act));
                SetMessage("REPLAY!", true);
            }
            else
            {
                if (_m.BonusMode == BonusMode.NORMAL)
                {
                    PlayCharacter("hit");
                    StartCoroutine(Effects.Miss(_charRt));
                    if (_m.EnemyActive) { StartCoroutine(Effects.Hit(_enemyRt)); UiFx.Burst(_charRt, UiFx.Preset.RedShards, new Vector2(20, 20)); }
                }
                if (r.win.winType == WinType.CHANCE) RoleFx(WinType.CHANCE, CellMaskFor(r.win));
                SetMessage(_m.BonusMode != BonusMode.NORMAL ? $"BONUS: {_m.BonusEarned} / {_m.BonusPayoutTarget}" : (r.win.winType == WinType.CHANCE ? "CHANCE!" : "..."));
            }

            if (r.bonusEnded) SetMessage("BONUS END!", true, Color.yellow);

            if (r.enemyResolved == true)
            {
                _audio.EnemyDeath();
                StartCoroutine(DefeatRoutine());
                SetMessage($"ENEMY DEFEATED!  EXP +{_m.Config.expPerDefeat}{(r.levelUp ? $"   LEVEL UP! Lv.{_m.PlayerLevel}" : "")}", true, ColGold);
                StartCoroutine(Effects.Shake(_stage, 0.3f, 5f));
            }
            else if (r.enemyResolved == false)
            {
                _audio.EnemyEscape();
                StartCoroutine(EscapeRoutine());
                SetMessage("ENEMY ESCAPED...", false, ColTextSub);
            }
            if (r.precursorStarted)
            {
                _shadowImg.sprite = ArtLoader.EnemySprite(r.enemyTable?.enemyType);
                ApplyEnemyLook(r.enemyTable);
                StartPrecursor(0);
            }
            else if (r.precursorStage > 0 && !r.enemySpawned)
            {
                StartPrecursor(r.precursorStage);
            }
            if (r.enemySpawned)
            {
                StopPrecursor();
                PlayCharacter("idle");
                ShowEnemy(r.enemyTable);
            }

            if (r.soulsGained > 0)
            {
                // 敵から抜けた魂が主人公に集まり、そのあと SOUL の数字へ流れる
                var src = (_m.EnemyActive || _m.InBattle) ? _enemyRt : _charRt;
                int wisps = Mathf.Clamp(3 + r.soulsGained / 8, 4, 12);
                UiFx.Absorb(src, _charRt, UiFx.Preset.SoulWisp, wisps, 0.62f);
                StartCoroutine(DelayedFx(0.5f, () => UiFx.Absorb(_charRt, _soulText.rectTransform, UiFx.Preset.SoulWisp, Mathf.Min(6, wisps), 0.6f)));
                UiFx.PopText(_soulText.rectTransform, $"+{r.soulsGained}", Hex("#a98bff"), 20, new Vector2(0, 24));
            }
            if (r.levelUp && _m.Stats.Unspent > 0)
                UiFx.PopText(_player.rectTransform, $"ポイント +{_m.Config.stats?.pointsPerLevel ?? 0}", ColGold, 20, new Vector2(0, 26));
            AtExpectFx(r, rankBefore);
            AtFx(r);
            AdventureFx(r);
            RogueFx(r);
            _graph?.Push(_m.Credit);
            _mini?.Push(_m.Credit);
            if (_graphBox != null && _graphBox.activeSelf) _graph.Redraw();
            RedrawMini();
            ShowTechResult(r);
            RollTraveler();
            RollHeroMonologue(r);
            SaveData.Save(_m, _audio);
            RefreshUi();
            if (r.chapterCleared) { StartCoroutine(ChapterClearRoutine(r)); return; }   // 街へ戻るのでオートは止める
            if (r.returnedToTown) { StartCoroutine(ReturnToTownRoutine(r)); return; }        // ライフ切れ・エンバー切れ
            if (_autoMode) StartAuto();
        }

        // ------------------------------------------------------ 装備・呪い
        /// <summary>装備が落ちたとき、呪いが出たときの見せ方。</summary>
        private void RogueFx(GameResult r)
        {
            if (r.equipDropped != null) StartCoroutine(DropRoutine(r));
            if (r.curseOffer != null) StartCoroutine(DelayedFx(1.2f, ShowCurseOffer));
        }

        private IEnumerator DropRoutine(GameResult r)
        {
            var cfg = _m.Config.equipment;
            var rar = EquipDirector.RarityOf(cfg, r.equipDropped);
            var col = Hex(rar.color);
            _audio.Win();
            UiFx.Burst(_enemyRt != null && _m.EnemyActive ? _enemyRt : _charRt,
                       r.equipDropped.rarity >= 2 ? UiFx.Preset.RainbowStars : UiFx.Preset.Coins, new Vector2(0, 20));
            if (r.equipBagFull)
            {
                SetMessage($"{r.equipDropped.name} を見送った（手持ちの方が良い）", false, ColTextSub);
                yield break;
            }
            // 落ちた場所から鞄（右上）へ吸い込む
            UiFx.Absorb(_m.EnemyActive ? _enemyRt : _charRt, _stageTagBg.rectTransform, UiFx.Preset.SoulWisp, 5, 0.7f);
            if (r.equipDropped.rarity >= 2)
                yield return SlamTitle($"{rar.name}  {r.equipDropped.name}", col, 1.5f, 44);
            else
                UiFx.PopText(_charRt, r.equipDropped.name, col, 20, new Vector2(0, 60));
            SetMessage(r.equipAutoWorn ? $"{r.equipDropped.name} を装備した" : $"{r.equipDropped.name} を拾った", true, col);
        }

        /// <summary>呪いを受けるかの選択。受けると呪い 1 つと祝福 1 つが同時に付く。</summary>
        private void ShowCurseOffer()
        {
            if (_m.Curse.Offer == null || _curseBox != null) return;
            var off = _m.Curse.Offer;
            _inputLocked = true;
            RefreshUi();

            const float W = 520f, H = 260f;
            var overlay = UiFactory.Panel(_stage, "CurseOverlay", Vector2.zero, new Vector2(4000, 4000), new Color(0, 0, 0, 0.88f));
            var card = UiSkin.Card(overlay, "CurseCard", Vector2.zero, new Vector2(W, H), 16);
            var head = UiFactory.Label(card, "Head", new Vector2(0, H * 0.5f - 30), new Vector2(W - 40, 26), "呪 い の 申 し 出", 20, TextAnchor.MiddleCenter, Hex("#c060ff"));
            head.fontStyle = FontStyle.Bold;
            UiSkin.Img(card, "Line", new Vector2(0, H * 0.5f - 48), new Vector2(W - 40, 1), null, new Color(1, 1, 1, 0.1f));

            var cCard = UiSkin.Card(card, "Curse", new Vector2(-W * 0.25f + 4, 12), new Vector2(W * 0.5f - 24, 108), 10, UiSkin.PanelHi, true, false, false);
            UiSkin.Img(cCard, "Bar", new Vector2(0, 50), new Vector2(W * 0.5f - 26, 4), UiSkin.Rounded(2), ColAccent);
            UiFactory.Label(cCard, "T", new Vector2(0, 30), new Vector2(W * 0.5f - 40, 20), "呪い", 12, TextAnchor.MiddleCenter, ColAccent).fontStyle = FontStyle.Bold;
            UiFactory.Label(cCard, "N", new Vector2(0, 8), new Vector2(W * 0.5f - 40, 22), off.curseName, 16, TextAnchor.MiddleCenter, ColText).fontStyle = FontStyle.Bold;
            UiFactory.Label(cCard, "D", new Vector2(0, -22), new Vector2(W * 0.5f - 40, 34), CurseText(off.curseEffect, off.curseValue, true), 11, TextAnchor.UpperCenter, ColTextSub);

            var bCard = UiSkin.Card(card, "Bless", new Vector2(W * 0.25f - 4, 12), new Vector2(W * 0.5f - 24, 108), 10, UiSkin.PanelHi, true, false, false);
            UiSkin.Img(bCard, "Bar", new Vector2(0, 50), new Vector2(W * 0.5f - 26, 4), UiSkin.Rounded(2), ColGold);
            UiFactory.Label(bCard, "T", new Vector2(0, 30), new Vector2(W * 0.5f - 40, 20), "祝福", 12, TextAnchor.MiddleCenter, ColGold).fontStyle = FontStyle.Bold;
            UiFactory.Label(bCard, "N", new Vector2(0, 8), new Vector2(W * 0.5f - 40, 22), off.blessName, 16, TextAnchor.MiddleCenter, ColText).fontStyle = FontStyle.Bold;
            UiFactory.Label(bCard, "D", new Vector2(0, -22), new Vector2(W * 0.5f - 40, 34), CurseText(off.blessEffect, off.blessValue, false), 11, TextAnchor.UpperCenter, ColTextSub);

            void Close()
            {
                Destroy(_curseBox);
                _curseBox = null;
                _m.Curse.Offer = null;
                _inputLocked = false;
                SaveData.Save(_m, _audio);
                RefreshUi();
            }

            UiSkin.Button(card, "Take", new Vector2(-100, -H * 0.5f + 34), new Vector2(180, 38), "受ける", () =>
            {
                _m.Curse.Taken.Add(off);
                _audio.RoleChance();
                StartCoroutine(EdgeGlow(Hex("#c060ff"), 1.2f, false));
                Close();
            }, Hex("#7a3fd0"), 15, true, 10);
            UiSkin.Button(card, "Refuse", new Vector2(100, -H * 0.5f + 34), new Vector2(180, 38), "断る", () => { _audio.UiPop(); Close(); }, ColBtn, 15, false, 10);

            _curseBox = overlay.gameObject;
        }

        private static string CurseText(string effect, int value, bool isCurse)
        {
            string v = value.ToString();
            switch (effect)
            {
                case CurseEffects.TorchDrain: return $"ライフの減りが {v}% 速くなる";
                case CurseEffects.ConditionHarder: return $"ルートの必要回数が +{v}";
                case CurseEffects.PayoutCut: return $"払い出しが {v}% 減る";
                case CurseEffects.BetExtra: return $"1 回転あたり {v} 多く灯を使う";
                default:
                    return $"{EquipDirector.EffectName(effect)} {(isCurse ? "-" : "+")}{v}{EquipDirector.EffectUnit(effect)}";
            }
        }

        /// <summary>装備画面の開け閉め（E キー）。</summary>
        private void ToggleEquip()
        {
            if (_equipBox != null) { Destroy(_equipBox); _equipBox = null; _audio.UiPop(); return; }
            if (_m.Config.equipment == null || !_m.Config.equipment.enabled) return;
            CloseModals();
            _audio.UiPop();
            _equipBox = EquipScreen.Build(_stage, _m, _audio, () => SaveData.Save(_m, _audio),
                                          () => { Destroy(_equipBox); _equipBox = null; });
        }

        // ------------------------------------------------------------ 冒険
        /// <summary>ルート決定・宝・ステージ移動の演出。</summary>
        private void AdventureFx(GameResult r)
        {
            if (!_m.AdventureEnabled) return;
            var cfg = _m.Config.adventure;
            var tv = _m.Config.travelers ?? TravelerConfig.Default();
            if (r.treasure != null) StartCoroutine(TreasureRoutine(r.treasure));
            if (r.torchRefilled && r.treasure == null)
            {
                var res = _m.Config.adventure?.resource;
                _audio.RoleBell();
                UiFx.PopText(_torchTagRt, $"{res?.name ?? "回復薬"} +1", Hex("#ffb45c"), 20, new Vector2(0, -22));
                UiFx.Burst(_torchTagRt, UiFx.Preset.Sparks, new Vector2(0, -8));
            }
            // ライフが回復した（通常時のリプレイ／ボーナス中のベル）。
            // 通常時はバーが出ているのでその上に、止まっている間は主人公の上に出す
            if (r.hpHealed > 0)
            {
                var res = _m.Config.adventure?.resource;
                _audio.RoleBell();
                bool barShown = _torchTagRt != null && _torchTagRt.gameObject.activeInHierarchy;
                var at = barShown ? _torchTagRt : _charRt;
                var off = barShown ? new Vector2(0, 20) : new Vector2(0, 52);
                UiFx.PopText(at, $"{res?.hpName ?? "ライフ"} +{r.hpHealed}", Hex("#7ee0a0"), 22, off);
                UiFx.Burst(at, UiFx.Preset.SuccessStars, barShown ? new Vector2(0, 6) : new Vector2(0, 30));
            }
            if (r.atStockUsed > 0) UiFx.PopText(_atChip.rectTransform, $"地図 +{r.atStockUsed} G", ColGold, 20, new Vector2(0, 22));
            if (r.routeDecided != null && !r.stageChanged)
            {
                var n = cfg.Find(r.routeDecided);
                UiFx.Burst(_charRt, UiFx.Preset.SuccessStars, new Vector2(0, 40));
                _audio.RoleChance();
                if (r.routeCondition != null)
                {
                    // 達成条件で決まったときは、何を達成したのかを見せる
                    StartCoroutine(RouteConditionRoutine(r.routeCondition, n));
                }
                else if (_dialogRoutine == null && n != null)
                    _dialogRoutine = StartCoroutine(HeroLineRoutine(tv.heroName ?? "主人公", $"……道が開けた。「{n.name}」へ向かおう", 2.6f));
                else SetMessage($"ルート決定  →  {n?.name}", true, ColGold);
            }
            if (r.stageChanged) StartCoroutine(StageEnterRoutine(cfg.Find(r.stageTo), r.bossAmbush, r.stageAdvanced, r.stageFrom == r.stageTo, r.firstVisitSouls));
            if (_mapBox != null && _mapBox.activeSelf) RedrawMap();
        }

        /// <summary>達成条件でルートが決まったときの告知。</summary>
        private IEnumerator RouteConditionRoutine(RouteCondition c, StageNode to)
        {
            _audio.Win();
            UiFx.Burst(_charRt, UiFx.Preset.RainbowStars, new Vector2(0, 40));
            yield return SlamTitle($"条件達成！  {AdventureDirector.DescribeCondition(c)}", ColGold, 1.6f, 40);
            if (to != null) yield return SlamTitle($"{to.id}  {to.name} へ", Hex(AdventureDirector.ColorFor(to)), 1.3f, 44);
        }

        private IEnumerator TreasureRoutine(TreasureDef t)
        {
            _audio.Win();
            UiFx.Burst(_charRt, UiFx.Preset.Coins, new Vector2(0, 30));
            string what;
            switch (t.kind)
            {
                case "atSpins": what = $"次の洞窟  +{t.amount} G"; break;
                case "atExpect": what = $"次のボーナス  AT期待度 +{t.amount}%"; break;
                case "exp": what = $"EXP +{t.amount}"; break;
                default: what = $"+{t.amount} SOUL"; break;
            }
            yield return SlamTitle($"宝箱発見！  {t.name}", ColGold, 1.3f, 44);
            SetMessage($"{t.name}   {what}", true, ColGold);
        }

        private IEnumerator StageEnterRoutine(StageNode node, bool boss, bool advanced = true, bool stayed = false, int firstSouls = 0)
        {
            if (node == null) yield break;
            var color = Hex(AdventureDirector.ColorFor(node));
            if (advanced)
            {
                _audio.EnemyAppearLand();
                UiFx.Ring(_charRt, new Color(color.r, color.g, color.b, 0.8f), 40, 300, 0.5f);
                yield return SlamTitle($"{node.id}   {node.name}", color, 1.6f, 50);
                if (firstSouls > 0) yield return SlamTitle($"はじめての地   +{firstSouls} SOUL", ColGold, 1.2f, 36);
            }
            else
            {
                // 条件を落とした: 引き返す（色を落として、音も沈める）
                _audio.EnemyEscape();
                StartCoroutine(Effects.Miss(_charRt));
                yield return SlamTitle(stayed ? "進めなかった……" : "来た道を引き返す……", ColTextSub, 1.4f, 38);
                yield return SlamTitle($"{node.id}   {node.name}", new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f), 1.2f, 44);
                PlayStory(StoryDirector.OnBack(_m.Config.story, _m.Adv.chapter));
            }
            RefreshUi();
            if (boss) yield return SlamTitle("……何かが待ち構えている", Hex("#ff9a3c"), 1.1f, 34);
            // 物語が用意されていればそれを流す。無ければステージ固有の一言
            var story = StoryDirector.OnEnter(_m.Config.story, _m.Config.adventure, _m.Adv.chapter, node.id);
            if (story != null && _dialogRoutine == null)
            {
                _dialogRoutine = StartCoroutine(StoryRoutine(story));
            }
            else if (node.enterLines != null && node.enterLines.Count > 0 && _dialogRoutine == null)
            {
                var tv = _m.Config.travelers ?? TravelerConfig.Default();
                _dialogRoutine = StartCoroutine(HeroLineRoutine(tv.heroName ?? "主人公", node.enterLines[_fxRng.Next(node.enterLines.Count)], 2.4f));
            }
        }

        /// <summary>力尽きて街へ帰る。ライフ切れとエンバー切れのどちらも同じ罰になる。</summary>
        private IEnumerator ReturnToTownRoutine(GameResult r)
        {
            _leaving = true;
            _inputLocked = true;
            if (_autoMode) SetAuto(false, _autoSpeed);
            var res = _m.Config.adventure?.resource ?? new ResourceConfig();
            bool byHp = r.returnReason == "hp";
            _audio.EnemyEscape();
            StartCoroutine(Effects.Miss(_charRt));
            yield return SlamTitle(byHp ? $"{res.hpName}が尽きた……" : "力尽きた……", ColAccent, 2.2f, 54);
            _runEndReason = byHp ? "hp" : "credit";
            int lost = AdventureDirector.ApplyDeathPenalty(_m.Config.adventure, _m.Adv, _m.Wallet, _m.TorchSpinsPerUnit);
            string sub = res.resetOnDeath ? "章の最初からやり直し" : "街へ運ばれた";
            if (lost > 0) sub += $"   ソウル -{lost:N0}";
            yield return SlamTitle(sub, ColTextSub, 1.5f, 32);
            var story = byHp ? StoryDirector.OnTorchOut(_m.Config.story, _m.Adv.chapter)
                             : StoryDirector.OnDeath(_m.Config.story, _m.Adv.chapter);
            if (PlayStory(story)) yield return new WaitForSeconds(2.4f);
            SaveData.Save(_m, _audio);
            _inputLocked = false;
            _leaving = false;
            OnBackToTown();
        }

        /// <summary>章クリア: 報酬を見せてから街へ戻る（オートは止める）。</summary>
        private IEnumerator ChapterClearRoutine(GameResult r)
        {
            _leaving = true;
            _inputLocked = true;
            if (_autoMode) SetAuto(false, _autoSpeed);
            _audio.Win();
            UiFx.Burst(_charRt, UiFx.Preset.SuccessStars, new Vector2(0, 40));
            yield return SlamTitle($"第{Mathf.Max(1, _m.Adv.chapter - 1)}章  踏破！   +{r.chapterSouls} SOUL", ColGold, 2.4f, 50);
            if (r.chapterSetbacks > 0) yield return SlamTitle($"引き返した回数 {r.chapterSetbacks}   報酬はその分だけ減った", ColTextSub, 1.4f, 28);
            if (PlayStory(StoryDirector.OnClear(_m.Config.story, Mathf.Max(1, _m.Adv.chapter - 1)))) yield return new WaitForSeconds(3.2f);
            _runEndReason = "clear";
            yield return SlamTitle("街へ戻る……", ColText, 1.0f, 34);
            SaveData.Save(_m, _audio);
            _inputLocked = false;
            _leaving = false;
            OnBackToTown();
        }

        /// <summary>通常時（敵なし・前兆なし・ボーナスなし・会話中でない）に主人公がたまにひとりごとを言う。</summary>
        // ------------------------------------------------------ REPLAY ACTION
        /// <summary>
        /// リプレイ時の主人公アクション。素材が無いので小道具は UiSkin の図形で組む（素材ができたら差し替え）。
        /// 約 1.4 秒。キャラは idle に切り替え、終わったら walk に戻す。
        /// </summary>
        private IEnumerator ReplayAction(string kind)
        {
            PlayCharacter("idle");
            var origin = _charRt.anchoredPosition;
            var baseScale = _charRt.localScale;
            float w = _charRt.sizeDelta.x;
            // 小道具は表示域に置き、キャラの手前に出す
            var prop = UiSkin.Rect(_area, "Prop_" + kind, origin + new Vector2(w * 0.32f, -w * 0.18f), new Vector2(60, 44));
            prop.SetSiblingIndex(_charRt.GetSiblingIndex() + 1);
            var cg = prop.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f; cg.blocksRaycasts = false;
            string popText = null; Color popColor = ColText;
            switch (kind)
            {
                case "bento":
                    UiSkin.Img(prop, "Box", Vector2.zero, new Vector2(60, 40), UiSkin.Rounded(6), new Color(0.45f, 0.26f, 0.14f));
                    UiSkin.Img(prop, "Rice", new Vector2(-13, 2), new Vector2(26, 26), UiSkin.Rounded(6), new Color(0.97f, 0.97f, 0.95f));
                    UiSkin.Img(prop, "Ume", new Vector2(-13, 2), new Vector2(8, 8), UiSkin.Circle(16), new Color(0.9f, 0.2f, 0.25f));
                    UiSkin.Img(prop, "Side1", new Vector2(12, 8), new Vector2(22, 12), UiSkin.Rounded(4), new Color(0.95f, 0.7f, 0.2f));
                    UiSkin.Img(prop, "Side2", new Vector2(12, -7), new Vector2(22, 12), UiSkin.Rounded(4), new Color(0.35f, 0.7f, 0.3f));
                    popText = "もぐもぐ"; popColor = ColGold; break;
                case "drink":
                    UiSkin.Img(prop, "Bottle", new Vector2(0, -4), new Vector2(22, 40), UiSkin.Rounded(6), new Color(0.35f, 0.65f, 0.95f, 0.9f));
                    UiSkin.Img(prop, "Cap", new Vector2(0, 20), new Vector2(14, 8), UiSkin.Rounded(3), new Color(0.25f, 0.3f, 0.4f));
                    UiSkin.Img(prop, "Shine", new Vector2(-5, 0), new Vector2(4, 26), UiSkin.Rounded(2), new Color(1, 1, 1, 0.5f));
                    popText = "ごくごく"; popColor = UiSkin.Blue; break;
                case "map":
                    UiSkin.Img(prop, "Paper", Vector2.zero, new Vector2(64, 46), UiSkin.Rounded(3), new Color(0.93f, 0.87f, 0.7f));
                    UiSkin.Img(prop, "Road", new Vector2(-6, -2), new Vector2(44, 3), null, new Color(0.55f, 0.4f, 0.25f)).rectTransform.localRotation = Quaternion.Euler(0, 0, 25);
                    UiSkin.Img(prop, "X", new Vector2(16, 10), new Vector2(8, 8), UiSkin.Circle(16), new Color(0.85f, 0.2f, 0.2f));
                    popText = "ふむ……"; popColor = ColTextSub; break;
                case "stretch": popText = "んーっ"; popColor = ColText; break;
                case "hum": popText = "♪"; popColor = UiSkin.Blue; break;
                case "rest": popText = "ふぅ"; popColor = ColTextSub; break;
            }
            bool hasProp = prop.childCount > 0;
            float t = 0, dur = 1.4f;
            if (popText != null) UiFx.PopText(_charRt, popText, popColor, kind == "hum" ? 30 : 20, new Vector2(30, 110));
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = t / dur;
                if (hasProp) cg.alpha = u < 0.15f ? u / 0.15f : u > 0.85f ? (1f - u) / 0.15f : 1f;
                switch (kind)
                {
                    case "bento":   // 食べる: 小さく上下に頷く
                        _charRt.anchoredPosition = origin + new Vector2(0, -3f * Mathf.Abs(Mathf.Sin(t * 12f)));
                        if (t > 0.5f && Mathf.Repeat(t, 0.45f) < Time.deltaTime) UiFx.Burst(prop, UiFx.Preset.Dust, new Vector2(0, 10));
                        break;
                    case "drink":   // 飲む: ボトルが傾き、頭が少し上がる
                        prop.localRotation = Quaternion.Euler(0, 0, -35f * Mathf.SmoothStep(0, 1, Mathf.Clamp01((u - 0.2f) / 0.4f)));
                        _charRt.anchoredPosition = origin + new Vector2(0, 4f * Mathf.SmoothStep(0, 1, Mathf.Clamp01((u - 0.2f) / 0.4f)));
                        break;
                    case "stretch": // 背伸び: 縦に伸びて戻る
                        {
                            float s = 1f + 0.12f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u));
                            _charRt.localScale = new Vector3(baseScale.x * (2f - s), baseScale.y * s, 1f);
                            _charRt.anchoredPosition = origin + new Vector2(0, (s - 1f) * w * 0.5f);
                        }
                        break;
                    case "map":     // 地図: 小さく左右を見る
                        _charRt.localRotation = Quaternion.Euler(0, 0, 4f * Mathf.Sin(t * 5f));
                        break;
                    case "hum":     // 口笛: 音符がふわふわ
                        _charRt.anchoredPosition = origin + new Vector2(0, 2f * Mathf.Sin(t * 8f));
                        if (Mathf.Repeat(t, 0.35f) < Time.deltaTime) UiFx.PopText(_charRt, "♪", UiSkin.Blue, 22, new Vector2(20 + 20f * Mathf.Sin(t * 3f), 90));
                        break;
                    case "rest":    // 一息: 少し沈んで戻る
                        _charRt.anchoredPosition = origin + new Vector2(0, -8f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u)));
                        break;
                }
                yield return null;
            }
            _charRt.anchoredPosition = origin;
            _charRt.localScale = baseScale;
            _charRt.localRotation = Quaternion.identity;
            Destroy(prop.gameObject);
            if (!_m.IsGameActive) PlayCharacter("walk");
        }

        /// <summary>AT のイベント演出（洞窟発見・バトル・討伐・終了）を一本にまとめて出す。</summary>
        private void AtFx(GameResult r)
        {
            if (r.setContinued) StartCoroutine(SetContinueRoutine(r.continueSet));
            if (r.zoneStarted != null) StartCoroutine(ZoneStartRoutine(r.zoneStarted));
            else if (r.zoneEnded) SetMessage("ゾーン終了", true, ColTextSub);
            if (r.zoneAddedSpins > 0)
            {
                UiFx.PopText(_atChip.rectTransform, $"+{r.zoneAddedSpins}G", ColGold, 22, new Vector2(0, 24));
                UiFx.Burst(_atChip.rectTransform, UiFx.Preset.Sparks, new Vector2(0, 6));
                _audio.RoleBell();
            }
            if (r.atWon) { StartCoroutine(CaveFoundRoutine()); return; }   // 当選告知。実際に入るのは前兆のあと
            if (r.battleStarted) { StartCoroutine(BattleStartRoutine(r.battleMonster)); return; }
            if (r.battleResolved.HasValue || r.battleDamage > 0) { StartCoroutine(BattleHitRoutine(r)); return; }
            if (r.atEnded) StartCoroutine(AtEndRoutine(_m.AtPayout, _m.AtSpinCount));
        }

        private void RollHeroMonologue(GameResult r)
        {
            if (_m.BonusMode != BonusMode.NORMAL || _m.EnemyActive || _m.PrecursorRemaining > 0 || _m.IsTier2 || _m.PendingTier2 || _m.InAt || _m.AtEntryRemaining > 0) return;
            if (_dialogRoutine != null || r.enemySpawned || r.precursorStarted || r.bonusEnded) return;
            string context = r.win.payout > 0 ? "win" : (!r.win.isReplay && r.win.winType == WinType.NONE ? "miss" : "idle");
            var line = HeroDirector.Roll(_m.Config.hero, context, _m.SpinCount, _fxRng);
            if (line == null) return;
            var cfg = _m.Config.travelers ?? TravelerConfig.Default();
            _dialogRoutine = StartCoroutine(HeroLineRoutine(cfg.heroName ?? "主人公", line));
        }

        /// <summary>物語の台詞を順に流す。話者が空なら主人公、それ以外はその名前で出す。</summary>
        private IEnumerator StoryRoutine(System.Collections.Generic.List<StoryLine> lines, float seconds = 2.6f)
        {
            var tv = _m.Config.travelers ?? TravelerConfig.Default();
            string hero = tv.heroName ?? "主人公";
            foreach (var ln in lines)
            {
                if (ln == null || string.IsNullOrEmpty(ln.text)) continue;
                bool isHero = string.IsNullOrEmpty(ln.speaker);
                yield return ShowLine(isHero ? hero : ln.speaker, ln.text,
                                      isHero ? ColGreen : Hex("#8f6bff"), ColBg, seconds);
            }
            HideDialogue();
            _dialogRoutine = null;
        }

        /// <summary>節目の物語（引き返した・尽きた・踏破した）を流す。流したら true。</summary>
        private bool PlayStory(System.Collections.Generic.List<StoryLine> lines)
        {
            if (lines == null || lines.Count == 0 || _dialogRoutine != null) return false;
            _dialogRoutine = StartCoroutine(StoryRoutine(lines));
            return true;
        }

        private IEnumerator HeroLineRoutine(string hero, string line, float seconds = 2.3f)
        {
            yield return ShowLine(hero, line, ColGreen, ColBg, seconds);
            HideDialogue();
            _dialogRoutine = null;
        }

        /// <summary>通常時（敵なし・前兆なし・ボーナスなし）に旅人を抽選して通す。</summary>
        private void RollTraveler()
        {
            if (_traveler != null) return;
            if (_m.BonusMode != BonusMode.NORMAL || _m.EnemyActive || _m.PrecursorRemaining > 0 || _m.IsTier2 || _m.PendingTier2 || _m.InAt || _m.AtEntryRemaining > 0) return;
            var ev = TravelerDirector.Roll(_m.Config.travelers, _m.Mode, _fxRng);
            if (ev == null) return;
            SpawnTraveler(ev.Value.traveler, ev.Value.serif, ev.Value.isModeHint);
        }

        private void SpawnTraveler(TravelerDef def, string serif, bool isModeHint)
        {
            _traveler = TravelerView.Spawn(_area, def, serif, isModeHint, AreaW, GroundY);
            _traveler.transform.SetSiblingIndex(_charRt.GetSiblingIndex());   // キャラの後ろ
            _traveler.PauseSeconds = ConversationSeconds;
            _traveler.OnSpeak = (s, hint) => StartConversation(def, s, hint);
        }

        // ---------------------------------------------------------- DIALOGUE
        private const float LineSeconds = 2.7f;
        private const float ConversationSeconds = LineSeconds * 2f + 0.2f;

        /// <summary>旅人のセリフ → 主人公の返事、の 2 行を会話 UI で順に出す。</summary>
        private void StartConversation(TravelerDef def, string serif, bool isModeHint)
        {
            var cfg = _m.Config.travelers ?? TravelerConfig.Default();
            string reply = null;
            if (isModeHint && cfg.hintReplies != null && cfg.hintReplies.Count > 0) reply = cfg.hintReplies[_fxRng.Next(cfg.hintReplies.Count)];
            else if (def.replies != null && def.replies.Count > 0) reply = def.replies[_fxRng.Next(def.replies.Count)];
            if (reply == null) reply = "そうか。気をつけてね";
            if (_dialogRoutine != null) StopCoroutine(_dialogRoutine);
            _dialogRoutine = StartCoroutine(ConversationRoutine(def.name, serif, isModeHint, cfg.heroName ?? "主人公", reply));
        }

        private IEnumerator ConversationRoutine(string speaker, string line, bool isModeHint, string hero, string reply)
        {
            // 示唆セリフはタグを赤にして「意味がある言葉」だと分かるように（示唆と確定は形で区別 §16.5）
            yield return ShowLine(speaker, line, isModeHint ? ColAccent : ColGold, isModeHint ? ColText : ColBg, LineSeconds);
            yield return ShowLine(hero, reply, ColGreen, ColBg, LineSeconds);
            HideDialogue();
            _dialogRoutine = null;
        }

        /// <summary>1 行分: タグを付けて本文をタイプライタ表示し、seconds 経つまで保持。</summary>
        private IEnumerator ShowLine(string speaker, string text, Color tagBg, Color tagFg, float seconds)
        {
            _dialogBox.SetActive(true);
            _dialogName.text = speaker;
            _dialogNameBg.color = tagBg;
            _dialogName.color = tagFg;
            _dialogText.text = "";
            float t = 0;
            const float cps = 28f;
            int shown = -1;
            while (t < seconds)
            {
                t += Time.deltaTime;
                int n = Mathf.Min(text.Length, Mathf.FloorToInt(t * cps));
                if (n != shown) { shown = n; _dialogText.text = text.Substring(0, n); }
                yield return null;
            }
            _dialogText.text = text;
        }

        private void HideDialogue()
        {
            if (_dialogRoutine != null) { StopCoroutine(_dialogRoutine); _dialogRoutine = null; }
            if (_dialogBox != null) _dialogBox.SetActive(false);
        }

        // ------------------------------------------- ボーナス中の 15 枚ベル演出
        /// <summary>
        /// ボーナス中のベル（15 枚）で、レバーオン時に敵を 3 体出す。
        /// 第一〜第三停止で 1 体ずつ倒し、3 体目のあとに主人公が一回転してポーズを決める。
        /// 期待度が上がったGなら PUSH ボタンを出し、押すと「Chance！！」を叩きつける。
        /// </summary>
        private void StartBonusBellShow()
        {
            _bellShowActive = true;
            _bellShowStop = 0;
            var types = new[] { "slime", "goblin", "bat" };
            var at = _m.Config.at;
            if (at?.monsters != null && at.monsters.Count >= 3)
                for (int i = 0; i < 3; i++) types[i] = at.monsters[i].enemyType;

            for (int i = 0; i < 3; i++)
            {
                if (_bellFoe[i] != null) Destroy(_bellFoe[i].gameObject);
                float x = 40f + i * 150f;
                var rt = MakeImage(_area, "BellFoe" + i, new Vector2(x, EnemyY - 8f), new Vector2(112, 112), ArtLoader.EnemySprite(types[i]));
                var img = rt.GetComponent<Image>();
                img.color = Color.white;
                var cg = rt.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                _bellFoe[i] = rt; _bellFoeImg[i] = img; _bellFoeCg[i] = cg;
                StartCoroutine(BellFoeEnter(i));
            }
        }

        private IEnumerator BellFoeEnter(int i)
        {
            yield return new WaitForSeconds(0.06f * i);
            var rt = _bellFoe[i];
            if (rt == null) yield break;
            var to = rt.anchoredPosition;
            var from = to + new Vector2(140f, 0);
            float t = 0;
            while (t < 0.3f && rt != null)
            {
                t += Time.deltaTime;
                float u = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.3f), 2f);
                rt.anchoredPosition = Vector2.Lerp(from, to, u);
                if (_bellFoeCg[i] != null) _bellFoeCg[i].alpha = u;
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = to;
            if (_bellFoeCg[i] != null) _bellFoeCg[i].alpha = 1f;
            UiFx.Burst(rt, UiFx.Preset.Dust, new Vector2(0, -50));
        }

        /// <summary>停止のたびに 1 体倒す。3 体目は一回転＋ポーズまでやる。</summary>
        private IEnumerator BellShowDefeat(int index)
        {
            PlayCharacter("attack-f3-4");
            _audio.Attack();
            var rt = _bellFoe[index];
            if (rt != null)
            {
                UiFx.Slash(rt, -32f, 250f);
                UiFx.Burst(rt, UiFx.Preset.Sparks);
                UiFx.Ring(rt, new Color(1f, 0.9f, 0.6f, 0.85f), 30, 190, 0.35f);
                yield return Effects.Hit(rt, 0.18f);
                _audio.EnemyDeath();
                yield return Effects.Defeat(rt, _bellFoeImg[index], _bellFoeCg[index]);
                UiFx.Burst(rt, UiFx.Preset.Explode);
                Destroy(rt.gameObject);
                _bellFoe[index] = null;
            }
            if (index < 2) { PlayCharacter("idle"); yield break; }

            // 3 体目: 一回転してポーズ
            yield return HeroSpinPose();
            // 判定が終わるまで待ってから、期待度が上がっていれば PUSH を出す
            float wait = 0;
            while (_m.IsGameActive && wait < 1.5f) { wait += Time.deltaTime; yield return null; }
            _bellShowActive = false;
            if (_m.AtExpectGained > 0) yield return PushThenChance();
            PlayCharacter("walk");
        }

        /// <summary>一回転してポーズ（3 体倒した直後）。</summary>
        private IEnumerator HeroSpinPose()
        {
            var baseScale = _charRt.localScale;
            float t = 0;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.45f);
                _charRt.localRotation = Quaternion.Euler(0, 0, -360f * Mathf.SmoothStep(0, 1, u));
                _charRt.localScale = baseScale * (1f + 0.12f * Mathf.Sin(u * Mathf.PI));
                yield return null;
            }
            _charRt.localRotation = Quaternion.identity;
            // 決めポーズ: ぐっと縮んで弾む
            PlayCharacter("attack-f1");
            t = 0;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.35f);
                float s = u < 0.3f ? Mathf.Lerp(1f, 0.88f, u / 0.3f) : Mathf.Lerp(0.88f, 1.06f, (u - 0.3f) / 0.7f);
                _charRt.localScale = baseScale * s;
                yield return null;
            }
            _charRt.localScale = baseScale;
            UiFx.Burst(_charRt, UiFx.Preset.SuccessStars, new Vector2(0, 40));
            UiFx.PopText(_charRt, "★", ColGold, 34, new Vector2(40, 110));
            _audio.Precog(1);
        }

        /// <summary>PUSH ボタンを出し、押されたら（か時間切れで）「Chance！！」を叩きつける。</summary>
        private IEnumerator PushThenChance()
        {
            var cfg = _m.Config.atExpect ?? new AtExpectConfig();
            // 受付中は他の入力を止める。Space が BET と二重に反応するのを防ぐため
            _inputLocked = true;
            RefreshUi();
            _pushPressed = false;
            var root = UiSkin.Rect(_area, "PushButton", new Vector2(0, -10), new Vector2(210, 92));
            UiSkin.Img(root, "Glow", Vector2.zero, new Vector2(320, 200), UiSkin.Glow(96), new Color(1f, 0.85f, 0.3f, 0.5f));
            UiSkin.Img(root, "Shadow", new Vector2(0, -6), new Vector2(226, 108), UiSkin.Shadow(46, 14), new Color(0, 0, 0, 0.6f));
            UiSkin.Img(root, "Ring", Vector2.zero, new Vector2(210, 92), UiSkin.Rounded(46), ColGold);
            UiSkin.Img(root, "Body", Vector2.zero, new Vector2(196, 78), UiSkin.Rounded(39), new Color(0.55f, 0.10f, 0.14f, 0.98f), true);
            var label = UiFactory.Label(root, "Label", new Vector2(0, 2), new Vector2(200, 80), "PUSH !", 40, TextAnchor.MiddleCenter, Color.white);
            label.fontStyle = FontStyle.Bold;
            TextShadow(label);
            var btn = root.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => _pushPressed = true);

            float t = 0, limit = Mathf.Max(0.5f, cfg.pushSeconds);
            while (t < limit && !_pushPressed)
            {
                t += Time.deltaTime;
                root.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * 12f));
                var kb = Keyboard.current;
                if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame)) _pushPressed = true;
                yield return null;
            }
            _audio.UiPop();
            UiFx.Burst(root, UiFx.Preset.SuccessStars);
            Destroy(root.gameObject);
            yield return SlamTitle("Chance！！", ColGold, 1.5f, 76);
            _inputLocked = false;
            RefreshUi();
            if (_autoMode) StartAuto();
        }

        /// <summary>ベル演出の後片付け（次のレバーや画面切り替えで残さない）。</summary>
        private void ClearBellShow()
        {
            _bellShowActive = false;
            _bellShowStop = 0;
            for (int i = 0; i < 3; i++)
                if (_bellFoe[i] != null) { Destroy(_bellFoe[i].gameObject); _bellFoe[i] = null; }
        }

        // ------------------------------------------------------------ AT 洞窟
        /// <summary>
        /// 帯を横切らせて大きな文字を叩きつける（ENEMY ENGAGE と同じ形）。色と文言だけ差し替えて使い回す。
        /// </summary>
        private IEnumerator SlamTitle(string text, Color color, float hold = 1.5f, int fontSize = 58)
        {
            var band = UiSkin.Rect(_area, "SlamBand", Vector2.zero, new Vector2(AreaW * 1.2f, 96));
            UiSkin.Img(band, "Bg", Vector2.zero, new Vector2(AreaW * 1.2f, 96), null, new Color(0, 0, 0, 0.78f));
            UiSkin.Img(band, "LineTop", new Vector2(0, 47), new Vector2(AreaW * 1.2f, 2), null, new Color(color.r, color.g, color.b, 0.9f));
            UiSkin.Img(band, "LineBottom", new Vector2(0, -47), new Vector2(AreaW * 1.2f, 2), null, new Color(color.r, color.g, color.b, 0.9f));
            band.localScale = new Vector3(0f, 1f, 1f);
            var bandCg = band.gameObject.AddComponent<CanvasGroup>();
            bandCg.blocksRaycasts = false;

            var title = UiFactory.Label(_area, "SlamTitle", new Vector2(0, 2), new Vector2(AreaW, 96), text, fontSize, TextAnchor.MiddleCenter, Color.white);
            title.fontStyle = FontStyle.Bold;
            var sh = title.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(color.r * 0.35f, color.g * 0.35f, color.b * 0.35f, 1f);
            sh.effectDistance = new Vector2(0, -3);
            var titleCg = title.gameObject.AddComponent<CanvasGroup>();
            titleCg.alpha = 0f; titleCg.blocksRaycasts = false;
            var glow = UiSkin.Img(_area, "SlamGlow", new Vector2(0, 2), new Vector2(520, 180), UiSkin.Glow(96), new Color(color.r, color.g, color.b, 0f));
            glow.transform.SetSiblingIndex(title.transform.GetSiblingIndex());

            float t = 0;
            while (t < 0.18f) { t += Time.deltaTime; band.localScale = new Vector3(1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.18f), 3f), 1f, 1f); yield return null; }
            band.localScale = Vector3.one;
            titleCg.alpha = 1f;
            t = 0;
            while (t < 0.16f)
            {
                t += Time.deltaTime;
                title.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.5f, 1f, 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.16f), 2f));
                yield return null;
            }
            title.rectTransform.localScale = Vector3.one;
            StartCoroutine(Effects.Shake(_stage, 0.3f, 6f));
            StartCoroutine(EdgeGlow(color, 0.5f, false));
            t = 0;
            while (t < hold) { t += Time.deltaTime; glow.color = new Color(color.r, color.g, color.b, 0.45f * Mathf.Exp(-t * 1.6f) + 0.08f); yield return null; }
            t = 0;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float a = 1f - Mathf.Clamp01(t / 0.3f);
                titleCg.alpha = a; bandCg.alpha = a; glow.color = new Color(color.r, color.g, color.b, 0.08f * a);
                yield return null;
            }
            Destroy(band.gameObject); Destroy(title.gameObject); Destroy(glow.gameObject);
        }

        /// <summary>AT 当選: ボーナス終了Gに「洞窟を見つけた」。次Gから AT が始まる。</summary>
        /// <summary>特化ゾーンに入ったときの叩きつけ。ゾーンごとに色と文言を変える。</summary>
        /// <summary>セット継続。何セット目かを見せる（続くほど濃い色に）。</summary>
        private IEnumerator SetContinueRoutine(int set)
        {
            _audio.Win();
            var col = set >= 8 ? Hex("#ff4d6d") : set >= 4 ? ColGold : ColGreen;
            UiFx.Burst(_charRt, set >= 8 ? UiFx.Preset.RainbowStars : UiFx.Preset.SuccessStars, new Vector2(0, 46));
            StartCoroutine(EdgeGlow(col, 1.0f, set >= 8));
            yield return SlamTitle($"継 続   {set} セット目", col, 1.5f, 56);
        }

        private IEnumerator ZoneStartRoutine(AtZone z)
        {
            var col = string.IsNullOrEmpty(z.color) ? ColGold : Hex(z.color);
            _audio.RoleChance();
            StartCoroutine(EdgeGlow(col, 1.4f, z.kind == "beast"));
            UiFx.Burst(_charRt, z.kind == "boost" ? UiFx.Preset.Coins : UiFx.Preset.RainbowStars, new Vector2(0, 50));
            UiFx.Ring(_area, new Color(col.r, col.g, col.b, 0.9f), 60, 520, 0.7f);
            yield return SlamTitle(string.IsNullOrEmpty(z.slam) ? z.name : z.slam, col, 1.7f, 62);
            SetMessage($"{z.name}   {z.spins} G", true, col);
        }

        private IEnumerator CaveFoundRoutine()
        {
            _audio.Precog(2);
            UiFx.Burst(_area, UiFx.Preset.SuccessStars, Vector2.zero);
            yield return SlamTitle("洞窟を見つけた！", Hex("#8f6bff"), 1.4f, 52);
            var tv = _m.Config.travelers ?? TravelerConfig.Default();
            if (_dialogRoutine != null) StopCoroutine(_dialogRoutine);
            _dialogRoutine = StartCoroutine(HeroLineRoutine(tv.heroName ?? "主人公", "……奥から気配がする。行くか", 2.4f));
        }

        /// <summary>
        /// 洞窟に入るまでの前兆（2〜4G）。段階が上がるほど画面が洞窟色に沈み、主人公が近づいていく。
        /// 消化しきった次のGで「洞窟探索」のタイトルが出て AT が始まる。
        /// </summary>
        private void ShowAtEntry(int stage, int total)
        {
            float k = Mathf.Clamp01(stage / (float)Mathf.Max(1, total));
            _caveTint.color = new Color(0.06f, 0.04f, 0.16f, 0.30f * k);
            StartCoroutine(EdgeGlow(Hex("#8f6bff"), 0.5f + 0.4f * k, false));
            _audio.Precog(k > 0.6f ? 2 : 1);
            StartCoroutine(Effects.Shake(_stage, 0.22f, 2f + 4f * k));
            if (k > 0.6f) UiFx.Burst(_charRt, UiFx.Preset.Focus, new Vector2(0, 60));
            var line = HeroDirector.CaveEntryLine(_m.Config.hero, stage, _fxRng);
            if (line == null) return;
            var tv = _m.Config.travelers ?? TravelerConfig.Default();
            if (_dialogRoutine != null) StopCoroutine(_dialogRoutine);
            _dialogRoutine = StartCoroutine(HeroLineRoutine(tv.heroName ?? "主人公", line, 2.6f));
        }

        /// <summary>AT 開始: 「洞窟探索」を大きく叩きつけてから 50G スタート。</summary>
        private IEnumerator AtStartRoutine()
        {
            HideDialogue();
            _bg.IsWalking = true;
            var purple = Hex("#8f6bff");
            _audio.Precog(2);
            UiFx.Burst(_area, UiFx.Preset.SuccessStars, Vector2.zero);
            UiFx.Ring(_area, new Color(0.56f, 0.42f, 1f, 0.9f), 60, 520, 0.7f);
            yield return SlamTitle("洞 窟 探 索", purple, 1.9f, 76);
            // このGぶんが既に引かれているので、告知は設定値（初期G数）を出す
            int startSpins = (_m.Config.at ?? new AtConfig()).initialSpins;
            UiFx.PopText(_area, $"{startSpins} G スタート", ColGold, 34, new Vector2(0, -10));
            _audio.Precog(2);
        }

        /// <summary>バトル開始: モンスターが出て、名前を叩きつける。</summary>
        private IEnumerator BattleStartRoutine(MonsterDef mon)
        {
            HideDialogue();
            if (_traveler != null) { Destroy(_traveler.gameObject); _traveler = null; }
            _enemyImg.sprite = ArtLoader.EnemySprite(mon?.enemyType);
            SetEnemyColor("none");
            if (_enemyIdle != null) StopCoroutine(_enemyIdle);
            PlayCharacter("idle");
            yield return new WaitForSeconds(0.3f);
            _audio.EnemyAppearStart();
            yield return Effects.SlideIn(_enemyRt, _enemyCg);
            _audio.EnemyAppearLand();
            UiFx.Burst(_enemyRt, UiFx.Preset.Dust, new Vector2(0, -60));
            _enemyIdle = StartCoroutine(Effects.IdleBob(_enemyRt));
            StartCoroutine(SlamTitle($"{mon?.name} 出現！", new Color(1f, 0.45f, 0.15f), 1.3f, 48));
        }

        /// <summary>狩猟: ダメージ表示と、討伐 / 失敗の決着。</summary>
        private IEnumerator BattleHitRoutine(GameResult r)
        {
            if (r.battleDamage > 0)
            {
                _audio.Attack();
                UiFx.Slash(_enemyRt, -35f, 240f);
                UiFx.Burst(_enemyRt, UiFx.Preset.Sparks);
                UiFx.PopText(_enemyRt, $"-{r.battleDamage}", ColGold, 30, new Vector2(0, 60));
                StartCoroutine(Effects.Hit(_enemyRt, 0.3f));
                PlayCharacter("attack-f3-4");
            }
            yield return new WaitForSeconds(0.35f);
            if (r.battleResolved == true)
            {
                _audio.EnemyDeath();
                yield return DefeatRoutine();
                StartCoroutine(SlamTitle($"討伐！  +{r.atSpinsAdded} G", ColGold, 1.4f, 50));
                UiFx.Burst(_area, UiFx.Preset.Confetti, new Vector2(0, AreaH * 0.4f));
            }
            else if (r.battleResolved == false)
            {
                _audio.EnemyEscape();
                yield return EscapeRoutine();
                var tv = _m.Config.travelers ?? TravelerConfig.Default();
                if (_dialogRoutine != null) StopCoroutine(_dialogRoutine);
                _dialogRoutine = StartCoroutine(HeroLineRoutine(tv.heroName ?? "主人公", "……逃げられたか", 2.2f));
            }
        }

        /// <summary>AT 終了: 洞窟を出る。</summary>
        private IEnumerator AtEndRoutine(int payout, int spins)
        {
            yield return SlamTitle($"洞窟を抜けた   エンバー +{payout} / {spins} G", Hex("#8f6bff"), 1.6f, 40);
            PlayCharacter("walk");
        }

        // -------------------------------------------------------- AT EXPECT 枠
        /// <summary>
        /// ボーナス中の帯テロップ。表示域の下端に置き、右から左へ文言を流す。
        /// 会話 UI と場所が近いが、会話は通常時だけなのでボーナス中は競合しない。
        /// </summary>
        private void BuildAtTicker()
        {
            const float h = 28f;
            var band = UiSkin.Rect(_area, "AtTicker", new Vector2(0, -AreaH * 0.5f + h * 0.5f + 2), new Vector2(AreaW, h));
            _tickerBg = UiSkin.Img(band, "Bg", Vector2.zero, new Vector2(AreaW, h), null, new Color(0.02f, 0.02f, 0.05f, 0.88f));
            _tickerLineTop = UiSkin.Img(band, "LineTop", new Vector2(0, h * 0.5f - 1), new Vector2(AreaW, 2), null, Color.white);
            _tickerLineBottom = UiSkin.Img(band, "LineBottom", new Vector2(0, -h * 0.5f + 1), new Vector2(AreaW, 2), null, Color.white);
            // 文字は帯の中だけに見せる（はみ出しを切る）
            _tickerView = UiSkin.Rect(band, "View", Vector2.zero, new Vector2(AreaW - 8, h - 4));
            _tickerView.gameObject.AddComponent<RectMask2D>();
            _tickerText = UiFactory.Label(_tickerView, "Text", Vector2.zero, new Vector2(1200, h - 4), "", 17, TextAnchor.MiddleLeft, ColText);
            _tickerText.fontStyle = FontStyle.Bold;
            TextShadow(_tickerText);
            _tickerRt = _tickerText.rectTransform;
            _tickerBox = band.gameObject;
            _tickerBox.SetActive(false);
        }

        /// <summary>舞台の四辺に光る枠を作る（ボーナス中だけ表示。色 = AT 期待度のランク）。</summary>
        private void BuildAtFrame()
        {
            const float thick = 10f;
            var root = UiSkin.Rect(_stage, "AtFrame", Vector2.zero, new Vector2(StageW, StageH));
            _atFrameCg = root.gameObject.AddComponent<CanvasGroup>();
            _atFrameCg.alpha = 0f;
            _atFrameCg.blocksRaycasts = false;
            var list = new System.Collections.Generic.List<Image>();
            // (位置, サイズ) の 4 辺。各辺は「ぼけた光」＋「芯」の 2 枚重ね
            var edges = new (Vector2 pos, Vector2 size)[]
            {
                (new Vector2(0, StageH * 0.5f - thick * 0.5f), new Vector2(StageW, thick)),
                (new Vector2(0, -StageH * 0.5f + thick * 0.5f), new Vector2(StageW, thick)),
                (new Vector2(-StageW * 0.5f + thick * 0.5f, 0), new Vector2(thick, StageH)),
                (new Vector2(StageW * 0.5f - thick * 0.5f, 0), new Vector2(thick, StageH)),
            };
            foreach (var e in edges)
            {
                var blur = new Vector2(e.size.x < e.size.y ? 44 : 0, e.size.y < e.size.x ? 44 : 0);
                list.Add(UiSkin.Img(root, "Glow", e.pos, e.size + blur, UiSkin.Shadow(5, 18), Color.white));
                list.Add(UiSkin.Img(root, "Core", e.pos, e.size, UiSkin.Rounded(4), Color.white));
            }
            _atFrame = list.ToArray();
        }

        /// <summary>毎フレーム: ボーナス中だけ枠を点滅させ、炎とキラキラを撒き、帯テロップを流す。</summary>
        private void UpdateAtFrame()
        {
            if (_atFrameCg == null) return;
            bool on = _m.BonusMode != BonusMode.NORMAL;
            if (!on)
            {
                if (_atFrameCg.alpha > 0f) _atFrameCg.alpha = 0f;
                if (_tickerBox != null && _tickerBox.activeSelf) { _tickerBox.SetActive(false); _tickerRank = ""; }
                return;
            }

            var cfg = _m.Config.atExpect ?? new AtExpectConfig();
            _atRank = AtDirector.RankFor(cfg, _m.AtExpectPercent);
            float k = Mathf.Clamp01(_m.AtExpectPercent / (float)Mathf.Max(1, cfg.maxPercent));
            float speed = 3.2f + 5.5f * k;                                  // 期待度が高いほど速い
            float a = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.time * speed));
            _atFrameCg.alpha = a * (0.55f + 0.45f * k);                     // 高いほど濃い

            Color c;
            if (_atRank != null && _atRank.color == "rainbow") c = Color.HSVToRGB((Time.time * 0.6f) % 1f, 0.8f, 1f);
            else if (_atRank != null) { ColorUtility.TryParseHtmlString(_atRank.color, out c); if (c.a <= 0f) c = ColText; }
            else c = ColText;
            for (int i = 0; i < _atFrame.Length; i++)
                if (_atFrame[i] != null) _atFrame[i].color = new Color(c.r, c.g, c.b, (i % 2 == 0) ? 0.55f : 1f);

            UpdateAtTicker(cfg, c, k);
            EmitAtFrameFx(c, k);
        }

        /// <summary>帯テロップ: ランクごとの文言を右から左へ流し、流れ切ったら次の文言に替える。</summary>
        private void UpdateAtTicker(AtExpectConfig cfg, Color c, float k)
        {
            if (_tickerBox == null) return;
            if (!_tickerBox.activeSelf) _tickerBox.SetActive(true);
            string rank = _atRank?.name ?? "";
            float viewW = _tickerView.rect.width;

            // ランクが変わったら即座に文言を差し替えて頭から流す
            if (rank != _tickerRank)
            {
                _tickerRank = rank;
                _tickerIndex = 0;
                _tickerText.text = AtDirector.TickerFor(cfg, rank, _tickerIndex);
                _tickerRt.anchoredPosition = new Vector2(viewW * 0.5f, 0);
            }
            if (_tickerText.text.Length == 0)
                _tickerText.text = AtDirector.TickerFor(cfg, rank, _tickerIndex);

            float textW = _tickerText.preferredWidth;
            float sp = Mathf.Max(20f, cfg.tickerSpeed) * (1f + 0.6f * k);
            var p = _tickerRt.anchoredPosition;
            p.x -= sp * Time.deltaTime;
            if (p.x < -viewW * 0.5f - textW)                     // 流れ切った
            {
                _tickerIndex++;
                _tickerText.text = AtDirector.TickerFor(cfg, rank, _tickerIndex);
                p.x = viewW * 0.5f;
            }
            _tickerRt.anchoredPosition = p;
            _tickerRt.sizeDelta = new Vector2(Mathf.Max(200f, textW + 20f), _tickerRt.sizeDelta.y);
            _tickerText.color = Color.Lerp(ColText, c, 0.45f + 0.55f * k);
            _tickerLineTop.color = new Color(c.r, c.g, c.b, 0.9f);
            _tickerLineBottom.color = new Color(c.r, c.g, c.b, 0.9f);
            _tickerBg.color = new Color(c.r * 0.10f, c.g * 0.10f, c.b * 0.10f, 0.9f);
        }

        /// <summary>枠の周りに炎（下から立ち上る）とキラキラ（縁で弾ける）を撒く。期待度が高いほど激しい。</summary>
        private void EmitAtFrameFx(Color c, float k)
        {
            _fxTimer -= Time.deltaTime;
            if (_fxTimer > 0f) return;
            _fxTimer = 0.35f;

            var warm = Color.Lerp(c, new Color(1f, 0.55f, 0.1f), 0.45f);
            var flame = new UiFx.Preset
            {
                shape = UiFx.Shape.Circle, count = 1,
                sizeMin = 10 + 6 * k, sizeMax = 22 + 14 * k,
                speedMin = 70, speedMax = 150 + 90 * k,
                gravity = 40, life = 0.85f, spread = 44, direction = 90, drag = 1.1f, spin = 0,
                colors = new[] { warm, c, new Color(1f, 0.85f, 0.35f) },
            };
            // 帯の上端から立ち上る炎
            UiFx.Rain(_area, flame, AreaW * 0.96f, -AreaH * 0.5f + 18f, 0.4f, 8f + 22f * k);

            var spark = new UiFx.Preset
            {
                shape = UiFx.Shape.Star, count = 2 + Mathf.RoundToInt(3 * k),
                sizeMin = 8, sizeMax = 16 + 8 * k,
                speedMin = 40, speedMax = 140, gravity = 0, life = 0.7f,
                spread = 360, direction = 90, drag = 1.6f, spin = 420,
                colors = new[] { Color.white, c, warm },
            };
            // 枠の縁のどこかでキラキラ（期待度が高いほど回数が増える）
            int bursts = 1 + Mathf.RoundToInt(2 * k);
            for (int i = 0; i < bursts; i++) UiFx.Burst(_stage, spark, RandomFramePoint());
        }

        /// <summary>舞台の縁のどこか 1 点（枠の上をキラキラさせるため）。</summary>
        private Vector2 RandomFramePoint()
        {
            float w = StageW * 0.5f - 8f, h = StageH * 0.5f - 8f;
            switch (_fxRng.Next(4))
            {
                case 0: return new Vector2(Mathf.Lerp(-w, w, (float)_fxRng.NextDouble()), h);
                case 1: return new Vector2(Mathf.Lerp(-w, w, (float)_fxRng.NextDouble()), -h);
                case 2: return new Vector2(-w, Mathf.Lerp(-h, h, (float)_fxRng.NextDouble()));
                default: return new Vector2(w, Mathf.Lerp(-h, h, (float)_fxRng.NextDouble()));
            }
        }

        /// <summary>期待度が上がった G の昇格アピール（ランクが上がったときだけ強く出す）。</summary>
        private void AtExpectFx(GameResult r, string rankBefore)
        {
            if (r.atExpectGained <= 0) return;
            var cfg = _m.Config.atExpect ?? new AtExpectConfig();
            var rank = AtDirector.RankFor(cfg, _m.AtExpectPercent);
            Color c = ColText;
            if (rank != null && rank.color != "rainbow") ColorUtility.TryParseHtmlString(rank.color, out c);
            else if (rank != null) c = ColGold;
            bool rankUp = rank != null && rank.name != rankBefore;
            UiFx.PopText(_stage, $"期待度 +{r.atExpectGained}%", c, rankUp ? 26 : 18, new Vector2(0, StageCardY - 60));
            if (!rankUp) return;
            UiFx.Burst(_stage, UiFx.Preset.Sparks, new Vector2(0, StageCardY - 40));
            UiFx.PopText(_stage, $"{rank.name} 昇格！", c, 30, new Vector2(0, StageCardY - 96));
            _audio.Precog(2);
        }

        // ----------------------------------------------------- BONUS ANNOUNCE
        /// <summary>
        /// ボーナス成立の前兆（3〜6G）。段階が上がるほど金の縁光が強くなり、主人公が匂わせる。
        /// 最後のGは擬似遊技: 停止ボタンを受け付けず、機械が自分でボーナスを揃える。
        /// 前兆中は引き込みが切れているので、察した人は目押しで先に揃えられる（Core 側で制御）。
        /// </summary>
        private void ShowBonusAnnounce()
        {
            if (_m.BonusMode != BonusMode.NORMAL) return;
            if (_m.PseudoPlay) { StartCoroutine(PseudoPlayRoutine()); return; }
            if (_m.BonusAnnounceRemaining <= 0) return;

            int total = Mathf.Max(1, _m.BonusAnnounceTotal);
            int stage = total - _m.BonusAnnounceRemaining + 1;      // 1..total-1
            float k = Mathf.Clamp01((float)stage / total);
            StartCoroutine(EdgeGlow(ColGold, 0.4f + 0.5f * k, false));
            _audio.Precog(k > 0.6f ? 2 : 1);
            if (k > 0.6f) UiFx.Burst(_charRt, UiFx.Preset.Sparks, new Vector2(0, 70));
            var line = HeroDirector.BonusPrecursorLine(_m.Config.hero, _fxRng);
            if (line == null) return;
            var tv = _m.Config.travelers ?? TravelerConfig.Default();
            if (_dialogRoutine != null) StopCoroutine(_dialogRoutine);
            _dialogRoutine = StartCoroutine(HeroLineRoutine(tv.heroName ?? "主人公", line, 2.6f));
        }

        /// <summary>擬似遊技: 手動停止を止め、間を置いて左→中→右と自動で止める（引き込みでボーナスが揃う）。</summary>
        private IEnumerator PseudoPlayRoutine()
        {
            HideDialogue();
            RefreshUi();
            yield return new WaitUntil(() => !StopsLocked);
            StartCoroutine(EdgeGlow(ColGold, 1.2f, false));
            _audio.Precog(2);
            UiFx.Ring(_area, new Color(1f, 0.85f, 0.35f, 0.85f), 60, 420, 0.6f);
            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < 3; i++)
            {
                StopReel(i, true);
                if (i < 2) yield return new WaitForSeconds(i == 0 ? 0.55f : 0.75f);
            }
        }

        // ------------------------------------------------------------- PRECOG
        /// <summary>役キー → 演出色（揃ったときの役演出と同じ色。学習させるため固定）。</summary>
        private static Color RoleColor(string role)
        {
            switch (role)
            {
                case "BELL": return UiSkin.Gold;
                case "REPLAY": return UiSkin.Blue;
                case "CHERRY": return new Color(1f, 0.45f, 0.65f);
                case "SUICA": return new Color(0.4f, 0.95f, 0.5f);
                case "CHANCE": return Color.white;   // 虹（rainbow フラグで回す）
                case "BONUS": return new Color(1f, 0.97f, 0.85f);
                default: return UiSkin.TextSub;
            }
        }

        /// <summary>
        /// 事前察知: レバーオンで確定した役を precog テーブルで抽選し、リール始動と同時に匂わせる。
        ///   弱 = ラインが役色に 1 回光る＋短い音＋キャラの頭上に「…」
        ///   強 = 画面縁が役色に光る＋ライン 2 回＋役の粒子＋「！」＋上昇音
        /// ボーナス中・敵エンゲージ中（示唆が別にある）は出さない。ハズレでもごく低確率でガセが出る。
        /// </summary>
        private void RollPrecog()
        {
            if (_m.BonusMode != BonusMode.NORMAL || _m.IsTier2 || _m.PendingTier2) return;
            // ボーナス前兆中は前兆側が告知役なので、予告は出さない（同じことを二重に匂わせない）
            if (_m.BonusAnnounceRemaining > 0 || _m.PseudoPlay) return;
            var p = PrecogDirector.Roll(_m.Config.precog, _m.CurrentFlag, _fxRng);
            if (p.stage <= 0) return;
            var color = RoleColor(p.role);
            bool rainbow = p.role == "CHANCE";
            _audio.Precog(p.stage);
            PaylineFlash(color, p.stage == 1 ? 1 : 2, rainbow, AllCells);
            UiFx.PopText(_charRt, p.stage == 1 ? "…" : "！", color, p.stage == 1 ? 26 : 36, new Vector2(20, 100));
            if (p.stage < 2) return;
            StartCoroutine(EdgeGlow(color, 0.6f, rainbow));
            switch (p.role)
            {
                case "BELL": UiFx.Burst(_charRt, UiFx.Preset.Sparks, new Vector2(0, 60)); break;
                case "REPLAY": UiFx.Burst(_charRt, UiFx.Preset.Sparkle, new Vector2(0, 70)); break;
                case "CHERRY": UiFx.Rain(_area, UiFx.Preset.Petals, AreaW * 0.6f, AreaH * 0.5f + 10, 0.7f, 18f); break;
                case "SUICA": UiFx.Burst(_charRt, UiFx.Preset.Splash, new Vector2(0, -80)); UiFx.Ring(_charRt, new Color(0.6f, 1f, 0.5f, 0.7f), 40, 200, 0.4f); break;
                case "CHANCE": UiFx.Burst(_charRt, UiFx.Preset.RainbowStars, new Vector2(0, 40)); break;
                case "BONUS": UiFx.Burst(_charRt, UiFx.Preset.Confetti, new Vector2(0, 90)); UiFx.Ring(_charRt, new Color(1f, 0.95f, 0.7f, 0.9f), 40, 300, 0.5f); break;
            }
        }

        // ------------------------------------------------------------ ROLE FX
        /// <summary>
        /// 役ごとの演出（役名カットイン＋払い出しラインの発光＋音）。
        /// 強さの段階: ベル/リプレイ = 軽い（頻出）、チェリー = 弱レア、スイカ = 中レア、チャンス目 = 強レア（虹）。
        /// 色は役ごとに固定（金=ベル / 青=リプレイ / 桃=チェリー / 緑=スイカ / 虹=チャンス目）。
        /// </summary>
        private void RoleFx(WinType type, int cellMask)
        {
            bool inBonus = _m.BonusMode != BonusMode.NORMAL;
            var pink = new Color(1f, 0.45f, 0.65f);
            var green = new Color(0.4f, 0.95f, 0.5f);
            switch (type)
            {
                case WinType.BELL:
                    // 音は獲得音（通常=控えめ / ボーナス=連打）に任せ、ここでは重ねない（ベルは頻出。重ねると「うるさい」）
                    PaylineFlash(ColGold, 2, false, cellMask);
                    if (!inBonus) UiFx.Cutin(_area, "BELL", ColGold, ArtLoader.SymbolSprite(Symbol.STAR), 0.7f, 0.35f, false, new Vector2(0, -20));
                    break;
                case WinType.REPLAY:
                    PaylineFlash(UiSkin.Blue, 2, false, cellMask);
                    UiFx.Burst(_charRt, UiFx.Preset.Sparkle, new Vector2(0, 70));
                    if (!inBonus) UiFx.Cutin(_area, "REPLAY", UiSkin.Blue, ArtLoader.SymbolSprite(Symbol.REPLAY), 0.7f, 0.35f, false, new Vector2(0, -20));
                    break;
                case WinType.CHERRY:
                    PaylineFlash(pink, 3, false, cellMask);
                    _audio.RoleCherry();
                    UiFx.Rain(_area, UiFx.Preset.Petals, AreaW * 0.9f, AreaH * 0.5f + 10, 1.4f, 26f);
                    UiFx.Cutin(_area, "チェリー！", pink, ArtLoader.SymbolSprite(Symbol.CHERRY), 1.0f, 0.8f, false, new Vector2(0, 10));
                    StartCoroutine(EdgeGlow(pink, 0.8f, false));
                    break;
                case WinType.WATERMELON:
                    PaylineFlash(green, 3, false, cellMask);
                    _audio.RoleSuica();
                    UiFx.Cutin(_area, "スイカ！", green, ArtLoader.SymbolSprite(Symbol.WATERMELON), 1.15f, 0.9f, false, new Vector2(0, 10));
                    StartCoroutine(EdgeGlow(green, 1.0f, false));
                    break;
                case WinType.CHANCE:
                    PaylineFlash(Color.white, 4, true, cellMask);
                    _audio.RoleChance();
                    UiFx.Cutin(_area, "チャンス目！", ColGold, null, 1.3f, 1.1f, true, new Vector2(0, 10));
                    UiFx.Burst(_area, UiFx.Preset.RainbowStars, new Vector2(0, 0));
                    UiFx.Burst(_area, UiFx.Preset.Confetti, new Vector2(0, AreaH * 0.5f - 10));
                    StartCoroutine(Effects.Shake(_stage, 0.3f, 6f));
                    StartCoroutine(EdgeGlow(ColGold, 1.5f, true));
                    break;
            }
        }

        /// <summary>
        /// 揃ったコマ（cellMask のビット。index = リール*3 + 段, 段 0=上段）を役の色で pulses 回点滅させる。
        /// rainbow=true は色相を回す。cellMask が 0 のときは何も出さない。
        /// </summary>
        private void PaylineFlash(Color color, int pulses, bool rainbow, int cellMask)
        {
            if (_paylineRoutine != null) StopCoroutine(_paylineRoutine);
            if (cellMask == 0) { _paylineFlash.alpha = 0f; return; }
            _paylineRoutine = StartCoroutine(PaylineFlashRoutine(color, pulses, rainbow, cellMask));
        }

        private IEnumerator PaylineFlashRoutine(Color color, int pulses, bool rainbow, int cellMask)
        {
            for (int i = 0; i < _cellFx.Length; i++)
                if (_cellFx[i] != null) _cellFx[i].gameObject.SetActive((cellMask & (1 << i)) != 0);

            const float period = 0.22f;
            float total = period * pulses;
            float t = 0;
            while (t < total)
            {
                t += Time.deltaTime;
                float u = (t % period) / period;
                float a = u < 0.35f ? u / 0.35f : 1f - (u - 0.35f) / 0.65f;
                _paylineFlash.alpha = Mathf.Clamp01(a);
                var c = rainbow ? Color.HSVToRGB((t * 1.5f) % 1f, 0.7f, 1f) : color;
                for (int i = 0; i < _cellFx.Length; i++)
                    if (_cellFx[i] != null && _cellFx[i].gameObject.activeSelf) _cellFx[i].color = new Color(c.r, c.g, c.b, 0.55f);
                yield return null;
            }
            _paylineFlash.alpha = 0f;
            for (int i = 0; i < _cellFx.Length; i++) if (_cellFx[i] != null) _cellFx[i].gameObject.SetActive(false);
            _paylineRoutine = null;
        }

        /// <summary>WinResult の成立ライン・チェリー段から、光らせるコマのビットマスクを作る。</summary>
        private static int CellMaskFor(WinResult win)
        {
            int mask = 0;
            for (int l = 0; l < PayLines.Count; l++)
            {
                if ((win.lineMask & (1 << l)) == 0) continue;
                var rows = PayLines.Rows[l];
                for (int reel = 0; reel < 3; reel++) mask |= 1 << (reel * 3 + rows[reel]);
            }
            // チェリーは左リールの止まった段（ライン成立ではない）
            for (int row = 0; row < 3; row++)
                if ((win.cherryMask & (1 << row)) != 0) mask |= 1 << row;
            return mask;
        }

        /// <summary>画面縁の発光（役の色）。既存の赤発光オーバーレイを色違いで使う。</summary>
        private IEnumerator EdgeGlow(Color color, float duration, bool rainbow)
        {
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float a = 0.3f * (0.5f + 0.5f * Mathf.Sin(t * 12f)) * (1f - t / duration);
                var c = rainbow ? Color.HSVToRGB((t * 1.2f) % 1f, 0.8f, 1f) : color;
                _redGlow.color = new Color(c.r, c.g, c.b, a);
                yield return null;
            }
            _redGlow.color = new Color(1f, 0.1f, 0.1f, 0f);
        }

        private IEnumerator DefeatRoutine()
        {
            if (_enemyIdle != null) { StopCoroutine(_enemyIdle); _enemyIdle = null; }
            if (_enemyRainbow != null) { StopCoroutine(_enemyRainbow); _enemyRainbow = null; }
            _dustRt.gameObject.SetActive(false);
            _enemyCg.alpha = 1f;
            _enemyImg.color = Color.white;
            UiFx.Ring(_enemyRt, new Color(1f, 1f, 1f, 0.9f), 40, 320, 0.4f);
            yield return Effects.Defeat(_enemyRt, _enemyImg, _enemyCg);
            UiFx.Burst(_enemyRt, UiFx.Preset.Explode);
            UiFx.PopText(_enemyRt, $"EXP +{_m.Config.expPerDefeat}", ColGold, 22, new Vector2(0, 60));
            HideEnemy();
        }

        private IEnumerator EscapeRoutine()
        {
            if (_enemyIdle != null) { StopCoroutine(_enemyIdle); _enemyIdle = null; }
            if (_enemyRainbow != null) { StopCoroutine(_enemyRainbow); _enemyRainbow = null; }
            _dustRt.gameObject.SetActive(false);
            _enemyCg.alpha = 1f;
            _enemyImg.color = Color.white;
            UiFx.Burst(_enemyRt, UiFx.Preset.Dust, new Vector2(-20, -50));
            yield return Effects.SlideOut(_enemyRt, _enemyCg);
            HideEnemy();
        }

        private IEnumerator DelayedFx(float delay, System.Action act)
        {
            yield return new WaitForSeconds(delay);
            act?.Invoke();
        }

        private IEnumerator GuardFlash()
        {
            // 青い縁光: 敵の攻撃を弾いたイメージ
            float t = 0;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                float a = 0.3f * (1f - t / 0.5f);
                _redGlow.color = new Color(0.3f, 0.6f, 1f, a);
                yield return null;
            }
            _redGlow.color = new Color(1f, 0.1f, 0.1f, 0f);
        }

        private IEnumerator BellHit()
        {
            _enemyCg.alpha = 1f;
            UiFx.Slash(_enemyRt, -35f, 240f);
            UiFx.Burst(_enemyRt, UiFx.Preset.Sparks);
            UiFx.Ring(_enemyRt, new Color(1f, 0.9f, 0.6f, 0.8f), 30, 180, 0.35f);
            yield return Effects.Hit(_enemyRt, 0.35f);
            yield return new WaitForSeconds(0.15f);
            _dustRt.gameObject.SetActive(false);
        }

        private IEnumerator SquashThenRestore()
        {
            _dustRt.gameObject.SetActive(false);
            _enemyCg.alpha = 1f;
            yield return Effects.Squash(_enemyRt, _enemyCg);
            // 次のレバーで復活（Lever() が alpha を戻す）
        }

        /// <summary>JS: BB確定音を流し、終わるまで操作を止める。</summary>
        private IEnumerator BigBonusStartRoutine()
        {
            _inputLocked = true;
            RefreshUi();
            float len = _audio.BbConfirm();
            StartCoroutine(Effects.Cutin(_cutinRt, _cutinCg, 3f));
            yield return new WaitForSeconds(Mathf.Max(len, 3f));
            _inputLocked = false;
            _audio.StartBgm();
            RefreshUi();
            if (_autoMode) StartAuto();
        }

        // ---------------------------------------------------------------- AUTO
        /// <summary>
        /// スペース長押しでオート（x1）。押している間だけ動き、離すと元の状態に戻す。
        /// 押した瞬間の 1 回は従来どおり BET / 順送り停止として処理される。
        /// </summary>
        private void UpdateSpaceHold(UnityEngine.InputSystem.Keyboard kb, bool canStart)
        {
            const float holdSec = 0.35f;
            if (kb.spaceKey.isPressed)
            {
                _spaceHold += Time.deltaTime;
                if (canStart && !_holdAuto && !_autoMode && _spaceHold >= holdSec)
                {
                    _holdAuto = true;
                    SetAuto(true, 1);
                }
            }
            else
            {
                _spaceHold = 0f;
                if (_holdAuto)
                {
                    _holdAuto = false;
                    SetAuto(false, _autoSpeedPref);
                }
            }
        }

        /// <summary>AUTO ボタン: OFF → x1 → x2 → … → x6 → OFF と一巡する。</summary>
        private void CycleAuto()
        {
            _audio.UiPop();
            if (!_autoMode) { _autoSpeedPref = 1; SetAuto(true, 1); }
            else if (_autoSpeedPref < AutoSpeedMax) { _autoSpeedPref++; SetAuto(true, _autoSpeedPref); }
            else { _autoSpeedPref = 1; SetAuto(false, 1); }
            SaveData.SaveOptions(_autoSpeedPref);
        }

        /// <summary>オートの ON/OFF と速さをまとめて切り替える。</summary>
        private void SetAuto(bool on, int speed)
        {
            _autoMode = on;
            _autoSpeed = Mathf.Clamp(speed, 1, AutoSpeedMax);
            RefreshUi();
            if (_autoMode) StartAuto();
            else if (_autoRoutine != null) { StopCoroutine(_autoRoutine); _autoRoutine = null; }
        }

        private void StartAuto()
        {
            if (_autoRoutine != null) StopCoroutine(_autoRoutine);
            _autoRoutine = StartCoroutine(AutoRoutine());
        }

        /// <summary>main.js triggerNextAutoAction。timings に従って BET → 順押し。</summary>
        private IEnumerator AutoRoutine()
        {
            var t = _m.Config.timings;
            float sp = Mathf.Max(1, _autoSpeed);
            if (!_m.IsGameActive)
            {
                bool win = _lastPayout > 0 || _lastWasReplay;
                yield return new WaitForSeconds((win ? t.nextWin : t.next) / 1000f / sp);
                if (!_autoMode || _inputLocked) yield break;
                if (!_m.MaxBet()) { _autoMode = false; SetMessage("エンバーが足りません", false, ColAccent); RefreshUi(); yield break; }
                Lever();
                yield break;
            }
            // ウェイト中は停止解禁まで待つ
            yield return new WaitUntil(() => !StopsLocked);
            // 押し順: ベル択ナビが出ていれば「1」を先に、残りの「?」はランダムに選ぶ。無ければ順押し
            int[] order = { 0, 1, 2 };
            if (_m.Navi2.Active)
            {
                // AT の押し順ナビ: 教えられたリールを第一停止（外すと 15 枚が 3 枚になる）
                int f = _m.Navi2.first;
                var rest2 = new System.Collections.Generic.List<int> { 0, 1, 2 };
                rest2.Remove(f);
                order = new[] { f, rest2[0], rest2[1] };
            }
            else if (_m.Navi.Active)
            {
                int first = _m.Navi.first;
                var rest = new System.Collections.Generic.List<int> { 0, 1, 2 };
                rest.Remove(first);
                int pick = rest[_fxRng.Next(rest.Count)];
                rest.Remove(pick);
                order = new[] { first, pick, rest[0] };
            }
            yield return new WaitForSeconds(t.reel1 / 1000f / sp);
            StopReel(order[0]);
            yield return new WaitForSeconds(t.reel2 / 1000f / sp);
            StopReel(order[1]);
            yield return new WaitForSeconds(t.reel3 / 1000f / sp);
            StopReel(order[2]);
            _autoRoutine = null;
        }
    }
}
