using System.Collections;
using BBB.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// ミニマップ。スタート画面のあとに開き、「街」と「冒険」を選ぶ。
    /// 街 = ショップ（ソウルでスキルや装備を買う）／冒険 = スロット本編。
    /// セーブは 1 本なので、ここでは SlotMachine を作ってソウルと持ち物だけ読み書きする。
    /// </summary>
    public sealed class MapScreen : MonoBehaviour
    {
        private const float StageW = SafeStage.StageW, StageH = SafeStage.StageH;

        private Canvas _canvas;
        private SafeStage _safe;
        private AudioManager _audio;
        private SlotMachine _m;
        private CanvasGroup _fade;
        private Text _soulText, _infoText, _stageInfo;
        private Text _chapterTitle, _hpValue, _torchValue, _emberValue;
        private RectTransform _mapView, _mapPanel;
        private bool _busy;
        private GameObject _shopBox;

        /// <summary>マップを開く（タイトルから、またはゲーム中の「街へ戻る」から）。</summary>
        public static MapScreen Open()
        {
            var exist = FindFirstObjectByType<MapScreen>();
            if (exist != null) return exist;
            return new GameObject("MapScreen").AddComponent<MapScreen>();
        }

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 120;
            _audio = AudioManager.Create();
            _m = GameDataLoader.CreateMachine(new SystemRandom());
            if (!SaveData.Load(_m, _audio)) SaveData.LoadAudio(_audio);   // セーブが無くても音量設定は引き継ぐ
            // 街に着いたら潜行中の拾い物と呪いは流す（恒久はソウル・ステータス・ショップだけ）
            bool hadRun = _m.Equip.Bag.Count > 0 || _m.Equip.Worn.Count > 0 || _m.Curse.Taken.Count > 0;
            _m.EndRun();
            if (hadRun) SaveData.Save(_m, _audio);
            ArriveInTown();
            _audio.StartBgm();
            BuildUi();
        }

        /// <summary>街に着いたときの処理。帰還の理由を消して、力尽きていたらエンバーを最低限だけ補う。</summary>
        private string _arriveMessage = "";

        private void ArriveInTown()
        {
            if (!_m.AdventureEnabled) return;
            var res = _m.Config.adventure.resource;
            if (res == null || !_m.Adv.MustReturn) return;
            string reason = _m.Adv.returnReason;
            _m.Adv.returnReason = null;
            if (reason == "credit")
            {
                // 補填はライフの効果込みの値を使う
                int rescue = _m.RescueCredit;
                if (rescue > 0 && _m.Credit < rescue) _m.Credit = rescue;
                _arriveMessage = $"力尽きて街に運ばれた。宿で一晩休み、エンバー {_m.Credit:N0} を分けてもらった";
            }
            else
            {
                // ライフ切れも力尽きた扱い（章の最初へ戻る）
                int rescue = _m.RescueCredit;
                if (rescue > 0 && _m.Credit < rescue) _m.Credit = rescue;
                _arriveMessage = $"{res.hpName}が尽きて倒れた。宿で手当てを受け、章の最初からやり直しになった";
            }
            SaveData.Save(_m, _audio);
        }

        // ------------------------------------------------------------------ UI
        private void BuildUi()
        {
            UiFactory.EnsureEventSystem();
            _canvas = UiFactory.CreateCanvas("MapCanvas");
            var root = _canvas.transform;
            var bg = UiSkin.Img(root, "BG", Vector2.zero, Vector2.zero, null, UiSkin.Bg);
            UiSkin.Stretch(bg.rectTransform);

            // 背景は地図らしい落ち着いた色。グリッドで「地図」感を出す
            var world = UiSkin.Img(root, "World", Vector2.zero, Vector2.zero, null, UiSkin.Hex("#12203a"));
            UiSkin.Stretch(world.rectTransform);
            _safe = SafeStage.Create(_canvas);
            var stage = _safe.Stage;
            DrawMapDecor(stage);

            // 見出し
            MapUiV2.Icon(stage, "compass", new Vector2(-428, 222), 48);
            var title = UiFactory.Label(stage, "Title", new Vector2(-228, 230), new Vector2(324, 34), "冒険の準備", 28, TextAnchor.MiddleLeft, UiSkin.Text);
            title.fontStyle = FontStyle.Bold;
            UiFactory.Label(stage, "Sub", new Vector2(-228, 202), new Vector2(324, 22), "街で整えて、次のステージへ", 15, TextAnchor.MiddleLeft, UiSkin.Hex("#c1cee2"));

            // ソウル残高
            var purse = UiSkin.Rect(stage, "Purse", new Vector2(342, 223), new Vector2(224, 60));
            // 左から順に領域を取る: アイコン → SOUL → 数字（docs/ui_rules.md 1 番）
            MapUiV2.Icon(purse, "crystal", new Vector2(-90, 0), 36);
            UiFactory.Label(purse, "L", new Vector2(-12, 16), new Vector2(110, 20), "所持ソウル", 14, TextAnchor.MiddleLeft, UiSkin.Hex("#b9cce5"));
            _soulText = UiFactory.Label(purse, "V", new Vector2(17, -10), new Vector2(168, 30), "0", 26, TextAnchor.MiddleLeft, UiSkin.Hex("#f7d991"));
            _soulText.fontStyle = FontStyle.Bold;

            // 左: 2 つの行き先（縦並び）。右: 冒険のステージマップ
            bool adv = _m.AdventureEnabled;
            if (adv)
            {
                const float contentTop = 174, cardW = 264, cardH = 180, gap = 14;
                float leftX = -StageW * .5f + 24 + cardW * .5f;
                MapNode(stage, "Town", new Vector2(leftX, contentTop - cardH * .5f), "街", "スキルと装備を購入\nソウルを使って強化", UiSkin.Gold, OpenShop, new Vector2(cardW, cardH));
                MapNode(stage, "Quest", new Vector2(leftX, contentTop - cardH - gap - cardH * .5f), "冒険", "", UiSkin.Accent, GoAdventure, new Vector2(cardW, cardH));
                float mapLeft = leftX + cardW * .5f + 20, mapW = StageW * .5f - 24 - mapLeft;
                _mapPanel = MapUiV2.Frame(stage, "MapPanel", new Vector2(mapLeft + mapW * .5f, contentTop - (cardH * 2 + gap) * .5f), new Vector2(mapW, cardH * 2 + gap));
                _chapterTitle = UiFactory.Label(_mapPanel, "Chapter", new Vector2(0, 150), new Vector2(mapW - 72, 28), "", 20, TextAnchor.MiddleLeft, UiSkin.Text);
                _chapterTitle.fontStyle = FontStyle.Bold;
                _stageInfo = UiFactory.Label(_mapPanel, "StageInfo", new Vector2(0, 124), new Vector2(mapW - 72, 20), "", 15, TextAnchor.MiddleLeft, UiSkin.Hex("#f7d991"));
                _stageInfo.fontStyle = FontStyle.Bold;
                UiFactory.Label(_mapPanel, "Legend", new Vector2(0, -158), new Vector2(mapW - 72, 22), "● 現在地    ━ 通過ルート    ○ 未到達    ? 未発見", 13, TextAnchor.MiddleCenter, UiSkin.Hex("#d1ddee"));
                RedrawStageMap();
            }
            else
            {
                MapNode(stage, "Town", new Vector2(-190, -20), "街", "ソウルでスキルと装備を買う", UiSkin.Hex("#c8961e"), OpenShop, new Vector2(300, 190));
                MapNode(stage, "Quest", new Vector2(190, -20), "冒険", "スロットを回して敵と戦う", UiSkin.Accent, GoAdventure, new Vector2(300, 190));
            }

            _infoText = UiFactory.Label(stage, "Info", new Vector2(0, -212), new Vector2(StageW - 64, 22), _arriveMessage, 14, TextAnchor.MiddleCenter, UiSkin.Gold);

            MapUiV2.Button(stage, "BtnTitle", new Vector2(-376, -244), new Vector2(160, 44), "タイトルへ", () => { _audio.UiPop(); StartCoroutine(BackToTitle()); }, secondary: true);
            _hpValue = FooterStat(stage, "heart", -180, _m.Config.adventure?.resource?.hpName ?? "ライフ");
            _torchValue = FooterStat(stage, "compass", 40, _m.Config.adventure?.resource?.name ?? "補給");
            _emberValue = FooterStat(stage, "ember", 260, "エンバー");
            RefreshQuestDesc();

            var fadeRt = UiFactory.Panel(root, "Fade", Vector2.zero, new Vector2(4000, 4000), Color.black);
            _fade = fadeRt.gameObject.AddComponent<CanvasGroup>();
            _fade.alpha = 1f;
            _fade.blocksRaycasts = false;
            StartCoroutine(FadeTo(0f, 0.4f));
            RefreshSouls();
        }

        /// <summary>地図らしい飾り（道と等高線もどき）。</summary>
        private void DrawMapDecor(Transform stage)
        {
            for (int i = 0; i < 7; i++)
            {
                float y = -StageH * 0.5f + 40 + i * 70;
                var line = UiSkin.Img(stage, "Contour" + i, new Vector2(0, y), new Vector2(StageW - 40, 1), null, new Color(1, 1, 1, 0.05f));
            }
            // 街 と 冒険 を結ぶ道
            if (!_m.AdventureEnabled) UiSkin.Img(stage, "Road", new Vector2(0, -20), new Vector2(380, 4), UiSkin.Rounded(2), new Color(0.85f, 0.75f, 0.5f, 0.35f));
        }

        /// <summary>マップ上の行き先 1 つ（大きめのカード）。</summary>
        private void MapNode(Transform parent, string name, Vector2 pos, string label, string desc, Color color, System.Action onClick, Vector2 size)
        {
            float w = size.x, h = size.y;
            var card = MapUiV2.Frame(parent, name, pos, size);

            // 上から順に領域を取る。隣の位置を前の要素から出すので、重なりが式の上で起きない
            const float labelH = 32f, descH = 42f, btnH = 48f, gap = 8f, btnBottom = 18f;
            float top = h * 0.5f;
            float labelCy = top - 24 - labelH * .5f;
            float descCy = labelCy - labelH * .5f - gap - descH * .5f;
            float btnCy = -top + btnBottom + btnH * 0.5f;

            float iconX = -w * .5f +  38;
            MapUiV2.Icon(card, name == "Town" ? "home" : "swords", new Vector2(iconX, labelCy), 34);
            float labelLeft = iconX + 17 + 10, labelW = w * .5f - 24 - labelLeft;
            var t = UiFactory.Label(card, "Label", new Vector2(labelLeft + labelW * .5f, labelCy), new Vector2(labelW, labelH), label, 25, TextAnchor.MiddleLeft, UiSkin.Text);
            t.fontStyle = FontStyle.Bold;
            var d = UiFactory.Label(card, "Desc", new Vector2(0, descCy), new Vector2(w - 48, descH), desc, 15, TextAnchor.UpperLeft, UiSkin.Hex("#d2dff1"));
            if (name == "Quest" && _m.AdventureEnabled) { _infoQuestDesc = d; RefreshQuestDesc(); }
            MapUiV2.Button(card, "Go", new Vector2(0, btnCy), new Vector2(w -  48, btnH), name == "Town" ? "街で準備する" : "冒険へ進む", () => { _audio.UiPop(); onClick(); }, name == "Quest");
        }

        private Text FooterStat(Transform parent, string icon, float x, string label)
        {
            var row = UiSkin.Rect(parent, "Stat" + icon, new Vector2(x, -247), new Vector2(196, 44));
            MapUiV2.Icon(row, icon, new Vector2(-80, 0), 30);
            UiFactory.Label(row, "Caption", new Vector2(20, 11), new Vector2(150, 16), label, 12, TextAnchor.MiddleLeft, UiSkin.Hex("#b9cce5"));
            var value = UiFactory.Label(row, "Value", new Vector2(20, -9), new Vector2(150, 24), "—", 18, TextAnchor.MiddleLeft, UiSkin.Text);
            value.fontStyle = FontStyle.Bold; return value;
        }

        private Text _infoQuestDesc;

        /// <summary>冒険カードの説明: 現在地と残りG。</summary>
        private void RefreshQuestDesc()
        {
            if (_m == null) return;
            if (_hpValue != null) _hpValue.text = $"{_m.Hp:N0} / {_m.HpMax:N0}";
            if (_torchValue != null) _torchValue.text = $"{Mathf.Max(0, _m.Adv.torches):N0} 個";
            if (_emberValue != null) _emberValue.text = _m.Credit.ToString("N0");
            if (_infoQuestDesc == null || !_m.AdventureEnabled) return;
            var n = _m.CurrentStage;
            var res = _m.Config.adventure?.resource;
            string line1 = n != null ? $"{n.id} から再開" : "スロットを回して敵と戦う";
            string line2 = $"このステージ：残り {_m.Adv.spinsLeft} G";
            _infoQuestDesc.text = line1 + "\n" + line2;
        }

        /// <summary>右側のステージマップを描き直す。</summary>
        private void RedrawStageMap()
        {
            if (_mapPanel == null || !_m.AdventureEnabled) return;
            if (_mapView != null) Destroy(_mapView.gameObject);
            var cfg = _m.Config.adventure;
            _mapView = StageMapView.Build(_mapPanel, cfg, _m.Adv, new Vector2(_mapPanel.sizeDelta.x - 64, 258), true);
            _mapView.anchoredPosition = new Vector2(0, -22);
            var n = _m.CurrentStage;
            string next = _m.Adv.nextId != null ? cfg.Find(_m.Adv.nextId)?.name : null;
            string lap = _m.Adv.chapter > 1 ? $"（{_m.Adv.chapter} 周目）" : "";
            _chapterTitle.text = $"{cfg.chapterName}{lap}";
            _stageInfo.text = $"現在地  {n?.id}  {n?.name}   ·   残り {_m.Adv.spinsLeft} G";
            RefreshQuestDesc();
        }

        private void RefreshSouls()
        {
            if (_soulText != null) _soulText.text = _m.Wallet.Souls.ToString("N0");
        }

        // -------------------------------------------------------------- ACTION
        private void OpenShop()
        {
            if (_busy) return;
            if (_shopBox != null) { Destroy(_shopBox); _shopBox = null; }
            _shopBox = ShopScreen.Build(_safe.Stage, _m, _audio, () => { RefreshSouls(); RefreshQuestDesc(); }, () => { Destroy(_shopBox); _shopBox = null; RefreshQuestDesc(); });
        }

        private void GoAdventure()
        {
            if (_busy) return;
            var res = _m.Config.adventure?.resource;
            if (_m.AdventureEnabled && res != null && res.enabled && _m.Adv.torches <= 0)
            {
                _infoText.text = $"{res.hpName}が無い。街で{res.name}を買ってから出発しよう";
                _infoText.color = UiSkin.Accent;
                _audio.UiPop();
                return;
            }
            if (_m.Credit < SlotMachine.BetCost)
            {
                _infoText.text = "エンバーが足りない。街で分けてもらおう";
                _infoText.color = UiSkin.Accent;
                _audio.UiPop();
                return;
            }
            _busy = true;
            StartCoroutine(GoAdventureRoutine());
        }

        private IEnumerator GoAdventureRoutine()
        {
            _fade.blocksRaycasts = true;
            yield return FadeTo(1f, 0.35f);
            SaveData.Save(_m, _audio);           // 買い物の結果を残してから本編へ
            Destroy(_canvas.gameObject);
            GameController.Launch();
            yield return FadeOutOverlay();
            Destroy(gameObject);
        }

        private IEnumerator BackToTitle()
        {
            if (_busy) yield break;
            _busy = true;
            _fade.blocksRaycasts = true;
            yield return FadeTo(1f, 0.3f);
            SaveData.Save(_m, _audio);
            Destroy(_canvas.gameObject);
            TitleScreen.Open();
            yield return FadeOutOverlay();
            Destroy(gameObject);
        }

        /// <summary>次の画面が出たあと、黒幕を最前面に出してから明転する。</summary>
        private IEnumerator FadeOutOverlay()
        {
            var go = new GameObject("MapFadeOut", typeof(Canvas), typeof(CanvasGroup));
            var cv = go.GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 1000;
            var img = new GameObject("Black", typeof(RectTransform), typeof(Image));
            var rt = img.GetComponent<RectTransform>();
            rt.SetParent(go.transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            var im = img.GetComponent<Image>();
            im.color = Color.black; im.raycastTarget = false;
            _fade = go.GetComponent<CanvasGroup>();
            _fade.alpha = 1f; _fade.blocksRaycasts = false;
            yield return null;
            yield return FadeTo(0f, 0.4f);
            Destroy(go);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && _shopBox != null)
            {
                Destroy(_shopBox); _shopBox = null; _audio.UiPop();
            }
        }

        private IEnumerator FadeTo(float target, float sec)
        {
            float from = _fade.alpha, t = 0;
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
