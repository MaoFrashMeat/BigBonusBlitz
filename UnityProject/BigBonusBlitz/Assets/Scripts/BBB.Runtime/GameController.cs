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
    ///       F1〜F6=設定変更 / B=BGM / R=セーブ削除して再開 / +(テンキー) or ;=クレジット100追加
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<GameController>() != null) return;
            new GameObject("GameController").AddComponent<GameController>();
        }

        // Web版 #game-container 1280x720 → 0.75倍で 960x540 に収める
        private const float StageW = 960f, StageH = 540f;
        private const float AreaW = StageW - 40f;
        private const float AreaH = 284f;          // #character-area 378px * 0.75
        private const float AreaCenterY = 118f;

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

        private RectTransform _enemyRt;
        private Image _enemyImg;
        private CanvasGroup _enemyCg;
        private Coroutine _enemyIdle;
        private Coroutine _enemyRainbow;
        private Image _darken;            // 前兆: 画面を段階的に暗くする
        private RectTransform _shadowRt;  // 前兆: 敵のシルエット
        private Image _shadowImg;
        private Coroutine _precursorRoutine;
        private TravelerView _traveler;
        private readonly SystemRandom _fxRng = new SystemRandom();
        private GameObject _naviBox;
        private Text[] _naviLabels = new Text[3];
        private Image[] _naviBg = new Image[3];
        private bool _hotStopSound;
        private int _stopCountThisGame;
        private RectTransform _dustRt;
        private RectTransform _cutinRt;
        private CanvasGroup _cutinCg;
        private Image _redGlow;

        private bool _autoMode;
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
            SaveData.Load(_m, _audio);
            BuildUi();
            RefreshUi();
            SetMessage(_m.HeldBonusFlag != Flag.HAZE ? "ボーナス成立中  ―  揃えてください" : "Ctrl または Space で BET");
            _audio.StartBgm();
            PlayCharacter("walk");
        }

        private void OnApplicationQuit() => SaveData.Save(_m, _audio);

        // ------------------------------------------------------------------ UI
        // 配色トークン（ゲーム筐体風のダーク基調。アクセントは操作できるものだけ）
        static readonly Color ColBg = Hex("#0b0f1a");
        static readonly Color ColPanel = Hex("#141a29");
        static readonly Color ColPanelEdge = Hex("#2a3450");
        static readonly Color ColText = Hex("#f2f4f8");
        static readonly Color ColTextSub = Hex("#9aa4b8");
        static readonly Color ColAccent = Hex("#e94560");
        static readonly Color ColGold = Hex("#ffcc33");
        static readonly Color ColGreen = Hex("#3ddc84");
        static readonly Color ColBtn = Hex("#232b40");
        static readonly Color ColBtnDisabled = Hex("#1a2030");

        private GameObject _settingsBox, _debugBox;
        private Image _bonusFill;
        private Text _creditNum, _payoutNum, _bonusLabel, _status;

        private static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        /// <summary>枠線付きパネル（外枠 1px + 内側）。返すのは内側。</summary>
        private static RectTransform Card(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var edge = UiFactory.Panel(parent, name, pos, size, ColPanelEdge);
            var inner = UiFactory.Panel(edge, "Inner", Vector2.zero, size - new Vector2(2, 2), ColPanel);
            inner.GetComponent<Image>().raycastTarget = false;
            return inner;
        }

        private static void StyleButton(Button b, Color normal, Color? text = null)
        {
            b.GetComponent<Image>().color = Color.white;
            var cb = b.colors;
            cb.normalColor = normal;
            cb.highlightedColor = normal * 1.15f;
            cb.pressedColor = normal * 0.8f;
            cb.selectedColor = normal;
            cb.disabledColor = ColBtnDisabled;
            cb.colorMultiplier = 1f;
            b.colors = cb;
            var t = b.GetComponentInChildren<Text>();
            t.color = text ?? ColText;
            t.fontStyle = FontStyle.Bold;
        }

        private void BuildUi()
        {
            UiFactory.EnsureEventSystem();
            var canvas = UiFactory.CreateCanvas();
            var root = canvas.transform;
            UiFactory.Panel(root, "BG", Vector2.zero, new Vector2(4000, 4000), ColBg);
            _stage = UiFactory.Panel(root, "Stage", Vector2.zero, new Vector2(StageW, StageH), new Color(0, 0, 0, 0));

            // ===== 上段: キャラクターエリア（920×270, 上端から 8px）=====
            const float areaH = 270f;
            float areaY = StageH * 0.5f - 8f - areaH * 0.5f;
            var areaEdge = UiFactory.Panel(_stage, "CharacterAreaEdge", new Vector2(0, areaY), new Vector2(AreaW + 2, areaH + 2), ColPanelEdge);
            _area = UiFactory.Panel(areaEdge, "CharacterArea", Vector2.zero, new Vector2(AreaW, areaH), Hex("#111111"));
            _area.gameObject.AddComponent<RectMask2D>();
            _bg = ParallaxBackground.Create(_area);

            // キャラ（Web: height 302px, left 15%, bottom 20px → 0.75倍）
            _idleFrames = ArtLoader.Strip("Art/Characters/chr0001_idle_strip", 4);
            _walkFrames = ArtLoader.Strip("Art/Characters/popora_walk_strip_25f", 25);
            _attackFrames = ArtLoader.Strip("Art/Characters/chr0001_attack_strip", 4);
            _charRt = MakeImage(_area, "Character", new Vector2(-AreaW * 0.5f + 0.15f * AreaW + 113f, -areaH * 0.5f + 15f + 113f), new Vector2(226, 226), null);
            _charRt.GetComponent<Image>().color = Color.white;
            _charAnim = _charRt.gameObject.AddComponent<SpriteAnimator>();

            // 前兆: 暗幕（背景の上・キャラの下）とシルエット
            _darken = UiFactory.Panel(_area, "Darken", Vector2.zero, new Vector2(AreaW, areaH), new Color(0, 0, 0, 0)).GetComponent<Image>();
            _darken.raycastTarget = false;
            _shadowRt = MakeImage(_area, "EnemyShadow", new Vector2(AreaW * 0.5f + 90f, -areaH * 0.5f + 15f + 75f), new Vector2(150, 150), null);
            _shadowImg = _shadowRt.GetComponent<Image>();
            _shadowImg.color = new Color(0, 0, 0, 0);

            // 敵（Web: 202px, right 20%, bottom 20px）
            _enemyRt = MakeImage(_area, "Enemy", new Vector2(AreaW * 0.5f - 0.2f * AreaW - 75f, -areaH * 0.5f + 15f + 75f), new Vector2(150, 150), null);
            _enemyImg = _enemyRt.GetComponent<Image>();
            _enemyCg = _enemyRt.gameObject.AddComponent<CanvasGroup>();
            _enemyCg.alpha = 0f;
            _dustRt = MakeImage(_area, "Dust", _enemyRt.anchoredPosition, new Vector2(180, 180), ArtLoader.Sprite("Art/UI/dust_cloud"));
            _dustRt.gameObject.SetActive(false);

            // 左上: Lv / EXP
            var pinfo = UiFactory.Panel(_area, "PlayerInfo", new Vector2(-AreaW * 0.5f + 8 + 72, areaH * 0.5f - 8 - 22), new Vector2(144, 44), new Color(0, 0, 0, 0.6f));
            _player = UiFactory.Label(pinfo, "Lv", new Vector2(0, 9), new Vector2(128, 18), "Lv 1", 14, TextAnchor.MiddleLeft, ColGold);
            var expBg = UiFactory.Panel(pinfo, "ExpBg", new Vector2(0, -10), new Vector2(128, 6), Hex("#333333"));
            var fillRt = UiFactory.Panel(expBg, "ExpFill", Vector2.zero, new Vector2(0, 6), ColGreen);
            fillRt.anchorMin = new Vector2(0, 0.5f); fillRt.anchorMax = new Vector2(0, 0.5f); fillRt.pivot = new Vector2(0, 0.5f); fillRt.anchoredPosition = Vector2.zero;
            _expFill = fillRt.GetComponent<Image>();

            // 右上: ENGAGE（敵戦闘中だけ）
            var tinfo = UiFactory.Panel(_area, "Tier2Info", new Vector2(AreaW * 0.5f - 8 - 80, areaH * 0.5f - 8 - 22), new Vector2(160, 44), new Color(0.9f, 0.1f, 0.1f, 0.75f));
            _tier2 = UiFactory.Label(tinfo, "T", Vector2.zero, new Vector2(160, 44), "", 13, TextAnchor.MiddleCenter, ColText);
            _tier2Box = tinfo.gameObject;
            _tier2Box.SetActive(false);

            // 右下: G数
            var gframe = MakeImage(_area, "GFrame", new Vector2(AreaW * 0.5f - 8 - 56, -areaH * 0.5f + 8 + 19), new Vector2(112, 38), ArtLoader.Sprite("Art/UI/g_display_frame"));
            _gCount = UiFactory.Label(gframe, "G", new Vector2(-8, 0), new Vector2(100, 30), "0G", 14, TextAnchor.MiddleRight, ColGold);

            // 示唆セリフ
            _hint = UiFactory.Label(_area, "Hint", new Vector2(0, areaH * 0.5f - 44), new Vector2(500, 32), "", 24, TextAnchor.MiddleCenter, ColAccent);
            _hint.fontStyle = FontStyle.Bold;

            // BB カットイン
            _cutinRt = MakeImage(_stage, "Cutin", new Vector2(0, areaY), new Vector2(400, 400), ArtLoader.Sprite("Art/UI/britz_bonus_logo"));
            _cutinCg = _cutinRt.gameObject.AddComponent<CanvasGroup>();
            _cutinCg.alpha = 0f;
            _cutinCg.blocksRaycasts = false;

            // ===== 中段: 左 表示器 / 中央 リール筐体 / 右 ステータス =====
            float midY = -72f;
            const float reelGap = 8f;
            float reelPitch = ReelView.ReelWidth + reelGap;
            float panelH = ReelView.SymbolHeight * 3 + 32;

            // リール筐体
            float cabW = reelPitch * 3 + 24;
            var cabinet = Card(_stage, "ReelCabinet", new Vector2(0, midY), new Vector2(cabW, panelH));
            var strips = _m.Strips;
            _reels = new ReelView[3];
            for (int i = 0; i < 3; i++)
            {
                _reels[i] = ReelView.Create(cabinet, strips[i], new Vector2((i - 1) * reelPitch, 0));
                _reels[i].Stopped += OnReelStopped;
            }
            // ベル択ナビ（筐体の上に重ねる。既定は非表示）
            var navi = UiFactory.Panel(_stage, "Navi", new Vector2(0, midY + panelH * 0.5f + 16), new Vector2(cabW, 28), new Color(0, 0, 0, 0));
            navi.GetComponent<Image>().raycastTarget = false;
            for (int i = 0; i < 3; i++)
            {
                var cell = UiFactory.Panel(navi, "Cell" + i, new Vector2((i - 1) * reelPitch, 0), new Vector2(ReelView.ReelWidth, 26), ColBtn);
                cell.GetComponent<Image>().raycastTarget = false;
                _naviBg[i] = cell.GetComponent<Image>();
                _naviLabels[i] = UiFactory.Label(cell, "L", Vector2.zero, new Vector2(ReelView.ReelWidth, 26), "", 14, TextAnchor.MiddleCenter, ColText);
                _naviLabels[i].fontStyle = FontStyle.Bold;
            }
            _naviBox = navi.gameObject;
            _naviBox.SetActive(false);

            // 中段ラインの目印
            UiFactory.Panel(cabinet, "LineMarkL", new Vector2(-cabW * 0.5f + 6, 0), new Vector2(6, 4), ColGold).GetComponent<Image>().raycastTarget = false;
            UiFactory.Panel(cabinet, "LineMarkR", new Vector2(cabW * 0.5f - 6, 0), new Vector2(6, 4), ColGold).GetComponent<Image>().raycastTarget = false;

            // 左: CREDIT / PAYOUT / BONUS
            var disp = Card(_stage, "Display", new Vector2(-330, midY), new Vector2(200, panelH));
            UiFactory.Label(disp, "CreditLabel", new Vector2(0, 80), new Vector2(176, 16), "CREDIT", 12, TextAnchor.MiddleLeft, ColTextSub);
            _creditNum = UiFactory.Label(disp, "CreditNum", new Vector2(0, 58), new Vector2(176, 32), "50", 30, TextAnchor.MiddleRight, ColText);
            _creditNum.fontStyle = FontStyle.Bold;
            UiFactory.Label(disp, "PayoutLabel", new Vector2(0, 30), new Vector2(176, 16), "PAYOUT", 12, TextAnchor.MiddleLeft, ColTextSub);
            _payoutNum = UiFactory.Label(disp, "PayoutNum", new Vector2(0, 8), new Vector2(176, 32), "0", 30, TextAnchor.MiddleRight, ColGold);
            _payoutNum.fontStyle = FontStyle.Bold;
            _bonusLabel = UiFactory.Label(disp, "BonusLabel", new Vector2(0, -32), new Vector2(176, 16), "", 12, TextAnchor.MiddleLeft, ColGold);
            var bonusBg = UiFactory.Panel(disp, "BonusBg", new Vector2(0, -50), new Vector2(176, 8), Hex("#333333"));
            var bonusFill = UiFactory.Panel(bonusBg, "BonusFill", Vector2.zero, new Vector2(0, 8), ColGold);
            bonusFill.anchorMin = new Vector2(0, 0.5f); bonusFill.anchorMax = new Vector2(0, 0.5f); bonusFill.pivot = new Vector2(0, 0.5f); bonusFill.anchoredPosition = Vector2.zero;
            _bonusFill = bonusFill.GetComponent<Image>();
            _bonusFill.raycastTarget = false;
            _mode = UiFactory.Label(disp, "Mode", new Vector2(0, -78), new Vector2(176, 16), "", 11, TextAnchor.MiddleLeft, ColTextSub);

            // 右: ステータス + 音量/デバッグの開閉
            var side = Card(_stage, "Side", new Vector2(330, midY), new Vector2(200, panelH));
            UiFactory.Label(side, "StatusLabel", new Vector2(0, 80), new Vector2(176, 16), "STATUS", 12, TextAnchor.MiddleLeft, ColTextSub);
            _status = UiFactory.Label(side, "Status", new Vector2(0, 36), new Vector2(176, 64), "", 14, TextAnchor.UpperLeft, ColText);
            var btnSettings = UiFactory.ButtonWithLabel(side, "BtnSettings", new Vector2(-44, -70), new Vector2(84, 32), "音量", ToggleSettings, 13);
            StyleButton(btnSettings, ColBtn);
            var btnDebug = UiFactory.ButtonWithLabel(side, "BtnDebug", new Vector2(44, -70), new Vector2(84, 32), "DEBUG", ToggleDebug, 13);
            StyleButton(btnDebug, ColBtn);

            // 音量ポップ（右パネルの上に重ねる。既定は非表示）
            var settings = Card(_stage, "Settings", new Vector2(330, midY + 60), new Vector2(200, 96));
            UiFactory.Label(settings, "BgmLabel", new Vector2(-64, 24), new Vector2(48, 20), "BGM", 12, TextAnchor.MiddleLeft, ColTextSub);
            _bgmSlider = UiFactory.Slider(settings, "BgmSlider", new Vector2(24, 24), new Vector2(120, 16), _audio.BgmVolume, v => { _audio.BgmVolume = v; });
            UiFactory.Label(settings, "SeLabel", new Vector2(-64, -4), new Vector2(48, 20), "SE", 12, TextAnchor.MiddleLeft, ColTextSub);
            _seSlider = UiFactory.Slider(settings, "SeSlider", new Vector2(24, -4), new Vector2(120, 16), _audio.SeVolume, v => { _audio.SeVolume = v; });
            _seSlider.gameObject.AddComponent<SliderReleaseSound>().OnRelease = () => _audio.UiPop();
            UiFactory.Label(settings, "Keys", new Vector2(0, -32), new Vector2(184, 16), "B: BGM切替   R: セーブ削除   F1-F6: 設定", 10, TextAnchor.MiddleCenter, ColTextSub);
            _settingsBox = settings.parent.gameObject;
            _settingsBox.SetActive(false);

            // デバッグ（既定は非表示。D キーでも開閉）
            var dbg = Card(_stage, "DebugBox", new Vector2(330, midY + 60), new Vector2(200, 250));
            _debug = UiFactory.Label(dbg, "Debug", new Vector2(0, 66), new Vector2(184, 108), "", 11, TextAnchor.UpperLeft, ColTextSub);
            float by = -4;
            void DbgBtn(string label, System.Action act, float x, float w = 88)
            {
                var b = UiFactory.ButtonWithLabel(dbg, "Dbg_" + label, new Vector2(x, by), new Vector2(w, 24), label, () => { act(); _audio.UiPop(); RefreshUi(); }, 11);
                StyleButton(b, ColBtn);
            }
            DbgBtn("敵出現", () => { _m.DebugForceEnemy = true; SetMessage("DEBUG: 次の判定で敵出現", false, ColTextSub); }, -46);
            DbgBtn("討伐ON/OFF", () => { if (_m.EnemyActive) _m.EnemyDefeatWon = !_m.EnemyDefeatWon; }, 46); by -= 28;
            DbgBtn("BB強制", () => _m.DebugForceFlag = Flag.BB_A, -46);
            DbgBtn("REG強制", () => _m.DebugForceFlag = Flag.RB_A, 46); by -= 28;
            DbgBtn("ベル強制", () => _m.DebugForceFlag = Flag.BELL_A, -46);
            DbgBtn("リプ強制", () => _m.DebugForceFlag = Flag.REPLAY_A, 46); by -= 28;
            DbgBtn("スイカ強制", () => _m.DebugForceFlag = Flag.SUICA_A, -46);
            DbgBtn("チェリー強制", () => _m.DebugForceFlag = Flag.CHERRY_A, 46); by -= 28;
            DbgBtn("+1000枚", () => _m.Credit += 1000, -46);
            DbgBtn("旅人", () => { var c = _m.Config.travelers ?? TravelerConfig.Default(); var t = c.travelers[_fxRng.Next(c.travelers.Count)]; if (_traveler == null) _traveler = TravelerView.Spawn(_area, t, t.chatter.Count > 0 ? t.chatter[0] : null, false, AreaW, -270f * 0.5f + 15f + 55f); }, 46); by -= 28;
            DbgBtn("ハズレ強制", () => _m.DebugForceFlag = Flag.HAZE, 46);
            _debugBox = dbg.parent.gameObject;
            _debugBox.SetActive(false);

            // ===== メッセージ帯 =====
            var msgBar = Card(_stage, "MessageBar", new Vector2(0, -172), new Vector2(AreaW, 36));
            _message = UiFactory.Label(msgBar, "Message", Vector2.zero, new Vector2(AreaW - 16, 36), "", 18, TextAnchor.MiddleCenter, ColText);
            _message.fontStyle = FontStyle.Bold;

            // ===== 下段: 操作バー =====
            float ctrlY = -222f;
            _btnBet = UiFactory.ButtonWithLabel(_stage, "BtnBet", new Vector2(-330, ctrlY), new Vector2(200, 52), "MAX BET", OnBetClicked, 18);
            StyleButton(_btnBet, ColAccent);
            _btnStops = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                _btnStops[i] = UiFactory.ButtonWithLabel(_stage, "BtnStop" + i, new Vector2((i - 1) * reelPitch, ctrlY), new Vector2(ReelView.ReelWidth, 52), "STOP", () => StopReel(idx), 18);
                StyleButton(_btnStops[i], ColBtn);
            }
            _btnAuto = UiFactory.ButtonWithLabel(_stage, "BtnAuto", new Vector2(330, ctrlY), new Vector2(200, 52), "AUTO", ToggleAuto, 18);
            StyleButton(_btnAuto, ColBtn);
            UiFactory.Label(_stage, "Help", new Vector2(0, -262), new Vector2(AreaW, 16),
                "Ctrl / Space: BET＋レバー     Z X C / ← ↓ →: 停止     Space: 順送り     A: オート", 11, TextAnchor.MiddleCenter, ColTextSub);

            // エフェクト層（UI の上、発光オーバーレイの下）
            UiFx.Init(root);

            // 赤発光オーバーレイ（最前面）
            var glow = UiFactory.Panel(root, "RedGlow", Vector2.zero, new Vector2(4000, 4000), new Color(1, 0.1f, 0.1f, 0));
            _redGlow = glow.GetComponent<Image>();
            _redGlow.raycastTarget = false;
        }

        private void ToggleSettings() { _settingsBox.SetActive(!_settingsBox.activeSelf); if (_settingsBox.activeSelf) _debugBox.SetActive(false); _audio.UiPop(); }
        private void ToggleDebug() { _debugBox.SetActive(!_debugBox.activeSelf); if (_debugBox.activeSelf) _settingsBox.SetActive(false); _audio.UiPop(); }

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
            _bonusLabel.text = inBonus ? $"{(_m.BonusMode == BonusMode.BB ? "BIG" : "REG")} BONUS   {_m.BonusEarned} / {_m.BonusPayoutTarget}" : (_m.HeldBonusFlag != Flag.HAZE ? "BONUS 成立中" : "");
            _bonusFill.rectTransform.sizeDelta = new Vector2(inBonus && _m.BonusPayoutTarget > 0 ? 176f * Mathf.Clamp01((float)_m.BonusEarned / _m.BonusPayoutTarget) : 0f, 8);
            _mode.text = $"設定 {_m.Setting}   総 {_m.TotalSpinCount} G";
            _gCount.text = $"{_m.SpinCount}G";

            string enemy = _m.ActiveEnemyTable != null && _m.EnemyActive ? _m.ActiveEnemyTable.name : "";
            _status.text = (_autoMode ? "AUTO ON\n" : "") + (inBonus ? "ボーナス中\n" : "")
                + (_m.IsTier2 ? $"敵: {enemy}\n残り {_m.Config.tier2MaxSpins - _m.Tier2SpinCount} G\n" : _m.PendingTier2 ? "次G から敵戦闘\n" : _m.PrecursorRemaining > 0 ? $"前兆 残り {_m.PrecursorRemaining} G\n" : "")
                + (_m.IsReplay ? "リプレイ\n" : "");
            _debug.text = $"FLAG {_m.CurrentFlag}\nRNG {_m.CurrentRng}\nHELD {_m.HeldBonusFlag}\nMODE {_m.Mode}\nSLIP {_m.Slip[0]},{_m.Slip[1]},{_m.Slip[2]}\n{enemy}{(_m.EnemyDefeatWon ? " ●" : "")}\n{_fps:F0} fps{(_m.DebugForceFlag.HasValue ? $"\n次G強制: {_m.DebugForceFlag}" : "")}{(_m.DebugForceEnemy ? "\n次判定: 敵出現" : "")}";
            _player.text = $"Lv {_m.PlayerLevel}";
            _expFill.rectTransform.sizeDelta = new Vector2(128f * Mathf.Clamp01((float)_m.PlayerExp / (_m.PlayerLevel * 100)), 6);

            _tier2Box.SetActive(_m.IsTier2 || _m.PendingTier2);
            _tier2.text = _m.IsTier2 ? $"ENEMY ENGAGE\n残り {_m.Config.tier2MaxSpins - _m.Tier2SpinCount} G" : "NEXT: ENGAGE";

            RefreshNavi();
            bool spinning = _m.IsGameActive;
            _btnBet.interactable = !spinning && !_inputLocked;
            _btnBet.GetComponentInChildren<Text>().text = _m.IsReplay ? "REPLAY" : "MAX BET";
            for (int i = 0; i < 3; i++) _btnStops[i].interactable = spinning && !StopsLocked && _reels[i].IsSpinning && _m.Stopped[i] == null;
            _btnAuto.GetComponentInChildren<Text>().text = _autoMode ? "AUTO  ON" : "AUTO";
            StyleButton(_btnAuto, _autoMode ? ColGreen : ColBtn, _autoMode ? ColBg : ColText);
        }

        private void SetMessage(string text, bool flash = false, Color? color = null)
        {
            _message.text = text;
            _message.color = color ?? ColText;
            _messageFlashUntil = flash ? Time.time + _m.Config.timings.nextWin / 1000f : 0f;
        }

        // ---------------------------------------------------------------- NAVI
        /// <summary>ナビ表示: 第一停止は「1」、残り2つは「? ATTACK」「? GUARD」。第一停止後に選択肢だけ残す。</summary>
        private void RefreshNavi()
        {
            if (!_m.Navi.Active || !_m.IsGameActive) { _naviBox.SetActive(false); return; }
            _naviBox.SetActive(true);
            var n = _m.Navi;
            int pressed = _m.PressOrder.Count;
            for (int i = 0; i < 3; i++)
            {
                string txt; Color bg = ColBtn, fg = ColText;
                if (i == n.first) { txt = pressed == 0 ? "① ここから" : "①"; bg = ColGold; fg = ColBg; }
                else { txt = "?"; bg = new Color(0.2f, 0.25f, 0.4f); }
                if (pressed >= 2 && _m.CurrentCommand != BellCommand.None)
                {
                    if (i == n.correctReel) { txt = _m.CurrentCommand == BellCommand.Success ? "SUCCESS!" : "正解はこっち"; bg = _m.CurrentCommand == BellCommand.Success ? ColGold : ColBtnDisabled; fg = _m.CurrentCommand == BellCommand.Success ? ColBg : ColTextSub; }
                    else if (i != n.first) { txt = _m.CurrentCommand == BellCommand.Fail ? "MISS" : "-"; bg = _m.CurrentCommand == BellCommand.Fail ? ColAccent : ColBtnDisabled; }
                }
                else if (pressed == 1 && !n.InChoice)
                {
                    if (i != n.first) { txt = "-"; bg = ColBtnDisabled; fg = ColTextSub; }
                }
                _naviLabels[i].text = txt; _naviLabels[i].color = fg; _naviBg[i].color = bg;
            }
        }

        /// <summary>択の最中: BGM がこもり（水中）、画面が少し沈む＝集中。</summary>
        private void EnterFocus()
        {
            _audio.SetFocus(true);
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
            _bg.IsWalking = false;
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
            _hint.text = stage == 0 ? "…？" : stage == 1 ? "……なにかいる" : "……来る！";
            _hint.color = stage >= 2 ? ColAccent : ColTextSub;
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
                default: _enemyImg.color = Color.white; break;
            }
        }

        // --------------------------------------------------------- CHARACTER
        /// <summary>main.js playCharacterAnimation。敵がいる間は walk を idle に置き換える。</summary>
        private void PlayCharacter(string type)
        {
            if (type == "walk" && (_m.EnemyActive || _m.PrecursorRemaining > 0)) type = "idle";
            _bg.IsWalking = type == "walk";
            switch (type)
            {
                case "idle": _charAnim.Play(_idleFrames, 1.6f, true); break;
                case "walk": _charAnim.Play(_walkFrames, 2.4f, true); break;
                case "attack-f1": if (_attackFrames.Length > 0) _charAnim.Show(_attackFrames[0]); break;
                case "attack-f2": if (_attackFrames.Length > 1) _charAnim.Show(_attackFrames[1]); break;
                case "attack-f3-4":
                    if (_attackFrames.Length > 3) _charAnim.Play(new[] { _attackFrames[2], _attackFrames[3] }, 0.2f, false);
                    break;
            }
        }

        private void ShowEnemy(EnemyTable table)
        {
            if (_traveler != null) { Destroy(_traveler.gameObject); _traveler = null; }
            _enemyImg.sprite = ArtLoader.EnemySprite(table?.enemyType);
            SetEnemyColor("none");
            _hint.text = "ENEMY ENGAGE!";
            _hint.color = ColAccent;
            if (_enemyIdle != null) StopCoroutine(_enemyIdle);
            StartCoroutine(SpawnEnemyRoutine());
        }

        private IEnumerator SpawnEnemyRoutine()
        {
            yield return new WaitForSeconds(0.5f);
            yield return Effects.SlideIn(_enemyRt, _enemyCg);
            UiFx.Burst(_enemyRt, UiFx.Preset.Dust, new Vector2(0, -60));
            UiFx.Ring(_enemyRt, new Color(1f, 0.3f, 0.3f, 0.8f), 40, 240, 0.4f);
            _enemyIdle = StartCoroutine(Effects.IdleBob(_enemyRt));
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
            var kb = Keyboard.current;
            if (kb != null && !_inputLocked)
            {
                if (kb.leftCtrlKey.wasPressedThisFrame || kb.rightCtrlKey.wasPressedThisFrame) OnBetClicked();
                if (kb.spaceKey.wasPressedThisFrame) OnSpaceStep();
                if (kb.zKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) StopReel(0);
                if (kb.xKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) StopReel(1);
                if (kb.cKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) StopReel(2);
                if (kb.aKey.wasPressedThisFrame) ToggleAuto();
                if (kb.bKey.wasPressedThisFrame) { _audio.ToggleBgm(); _audio.UiPop(); }
                if (kb.dKey.wasPressedThisFrame) ToggleDebug();
                if (kb.rKey.wasPressedThisFrame && !_m.IsGameActive) ResetSave();
                if ((kb.numpadPlusKey.wasPressedThisFrame || kb.semicolonKey.wasPressedThisFrame) && !_m.IsGameActive) { _m.Credit += 100; _audio.UiPop(); RefreshUi(); }
                for (int i = 0; i < 6; i++)
                {
                    var key = kb[Key.F1 + i];
                    if (key.wasPressedThisFrame && !_m.IsGameActive) { _m.SetSetting(i + 1); _audio.UiPop(); RefreshUi(); }
                }
            }
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
            for (int i = 0; i < 3; i++)
                if (_reels[i].IsSpinning && _m.Stopped[i] == null) { StopReel(i); return; }
        }

        private void OnBetClicked()
        {
            if (_m.IsGameActive || _inputLocked) return;
            bool wasReplay = _m.IsReplay;
            if (!_m.MaxBet())
            {
                SetMessage("クレジットが足りません", false, ColAccent);
                return;
            }
            if (!wasReplay) _audio.Bet();
            Lever();
        }

        private void Lever()
        {
            _lastPayout = 0;
            _dustRt.gameObject.SetActive(false);
            if (_m.EnemyActive) _enemyCg.alpha = 1f;   // JS onLever: 潰した敵を戻す
            _charRt.localRotation = Quaternion.identity;
            ExitFocus();
            var legacyHint = _m.Lever();   // 抽選はレバーオン時点で確定（旧示唆は使わない）
            var hint = HintKind.None;
            ApplyEngageHint();

            // ウェイト: リールは即回転。停止ボタンだけ「前回レバーから SpinWaitSeconds」まで無効
            float unlockAt = _lastSpinStartTime + SpinWaitSeconds;
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

        private void StopReel(int i)
        {
            if (!_m.IsGameActive || StopsLocked || !_reels[i].IsSpinning || _m.Stopped[i] != null) return;
            var res = _m.Stop(i, _reels[i].TopIndex);
            _reels[i].StopAt(res.stopIndex);
            _btnStops[i].interactable = false;
            if (_m.Navi.Active)
            {
                if (_m.PressOrder.Count == 1 && _m.Navi.InChoice) EnterFocus();
                else if (_m.PressOrder.Count == 2 && _m.CurrentCommand == BellCommand.Success) StartCoroutine(NaviSuccessRoutine());
                else if (_m.PressOrder.Count == 2 && _m.CurrentCommand == BellCommand.Fail) StartCoroutine(NaviFailRoutine());
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
            if (_hotStopSound)
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
            var r = _m.Evaluate();
            _lastPayout = r.win.payout;
            _lastWasReplay = r.win.isReplay;
            if (_m.PrecursorRemaining == 0) _hint.text = "";
            PlayCharacter("walk");

            if (r.bonusStarted)
            {
                SetMessage($"BONUS START! 0 / {_m.BonusPayoutTarget}", true, Color.yellow);
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
                _audio.Win();
                UiFx.Burst(_payoutNum.rectTransform, UiFx.Preset.Coins, new Vector2(0, -10));
                UiFx.PopText(_payoutNum.rectTransform, $"+{r.win.payout}", ColGold, 24, new Vector2(0, 20));
                switch (r.win.winType)
                {
                    case WinType.BELL:
                        // 1〜2G目に「死んだように見える」演出は禁止。ヒット＋砂煙のみ。決着は3G目終了時の判定だけ
                        if (_m.EnemyActive && r.command != BellCommand.Fail) StartCoroutine(BellHit());
                        break;
                    case WinType.CHERRY:
                        StartCoroutine(Effects.Cherry(_charRt));
                        if (_m.EnemyActive) { StartCoroutine(Effects.Hit(_enemyRt)); UiFx.Slash(_enemyRt, 10f, 200f, new Color(1f, 0.5f, 0.7f)); }
                        else UiFx.Slash(_charRt, 10f, 180f, new Color(1f, 0.5f, 0.7f));
                        break;
                    case WinType.WATERMELON:
                        StartCoroutine(Effects.Watermelon(_charRt));
                        if (_m.EnemyActive) StartCoroutine(Effects.Hit(_enemyRt));
                        StartCoroutine(Effects.Shake(_stage));
                        StartCoroutine(DelayedFx(0.36f, () => { UiFx.Burst(_charRt, UiFx.Preset.Dust, new Vector2(0, -100)); UiFx.Ring(_charRt, new Color(0.6f, 1f, 0.5f, 0.8f), 40, 260, 0.45f); }));
                        break;
                }
                SetMessage(_m.BonusMode != BonusMode.NORMAL ? $"BONUS: {_m.BonusEarned} / {_m.BonusPayoutTarget}" : $"WIN! +{r.win.payout}", true);
            }
            else if (r.win.isReplay)
            {
                _audio.Replay();
                StartCoroutine(Effects.Replay(_charRt));
                UiFx.Ring(_charRt, new Color(0.5f, 0.8f, 1f, 0.7f), 60, 160, 0.4f);
                SetMessage("REPLAY!", true);
            }
            else
            {
                if (_m.BonusMode == BonusMode.NORMAL)
                {
                    StartCoroutine(Effects.Miss(_charRt));
                    if (_m.EnemyActive) { StartCoroutine(Effects.Hit(_enemyRt)); UiFx.Burst(_charRt, UiFx.Preset.RedShards, new Vector2(20, 20)); }
                }
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

            RollTraveler();
            SaveData.Save(_m, _audio);
            RefreshUi();
            if (_autoMode) StartAuto();
        }

        /// <summary>通常時（敵なし・前兆なし・ボーナスなし）に旅人を抽選して通す。</summary>
        private void RollTraveler()
        {
            if (_traveler != null) return;
            if (_m.BonusMode != BonusMode.NORMAL || _m.EnemyActive || _m.PrecursorRemaining > 0 || _m.IsTier2 || _m.PendingTier2) return;
            var ev = TravelerDirector.Roll(_m.Config.travelers, _m.Mode, _fxRng);
            if (ev == null) return;
            float groundY = -270f * 0.5f + 15f + 55f;
            _traveler = TravelerView.Spawn(_area, ev.Value.traveler, ev.Value.serif, ev.Value.isModeHint, AreaW, groundY);
            _traveler.transform.SetSiblingIndex(_charRt.GetSiblingIndex());   // キャラの後ろ
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
        private void ToggleAuto()
        {
            _autoMode = !_autoMode;
            _audio.UiPop();
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
            if (!_m.IsGameActive)
            {
                bool win = _lastPayout > 0 || _lastWasReplay;
                yield return new WaitForSeconds((win ? t.nextWin : t.next) / 1000f);
                if (!_autoMode || _inputLocked) yield break;
                if (!_m.MaxBet()) { _autoMode = false; SetMessage("クレジットが足りません", false, ColAccent); RefreshUi(); yield break; }
                Lever();
                yield break;
            }
            // ウェイト中は停止解禁まで待つ
            yield return new WaitUntil(() => !StopsLocked);
            yield return new WaitForSeconds(t.reel1 / 1000f);
            StopReel(0);
            yield return new WaitForSeconds(t.reel2 / 1000f);
            StopReel(1);
            yield return new WaitForSeconds(t.reel3 / 1000f);
            StopReel(2);
            _autoRoutine = null;
        }
    }
}
