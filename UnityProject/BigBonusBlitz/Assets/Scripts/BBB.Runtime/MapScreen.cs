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
            ArriveInTown();
            _audio.StartBgm();
            BuildUi();
        }

        /// <summary>街に着いたときの処理。帰還の理由を消して、力尽きていたら路銀を最低限だけ補う。</summary>
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
                if (res.rescueCredit > 0 && _m.Credit < res.rescueCredit) _m.Credit = res.rescueCredit;
                _arriveMessage = $"力尽きて街に運ばれた。宿で一晩休み、路銀 {_m.Credit:N0} 枚で目が覚めた";
            }
            else _arriveMessage = $"{res.name}が尽きて引き返した。補給すれば続きから行ける";
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
            var title = UiFactory.Label(stage, "Title", new Vector2(0, StageH * 0.5f - 44), new Vector2(StageW, 34), "ミニマップ", 26, TextAnchor.MiddleCenter, UiSkin.Text);
            title.fontStyle = FontStyle.Bold;
            UiFactory.Label(stage, "Sub", new Vector2(0, StageH * 0.5f - 74), new Vector2(StageW, 20), "行き先を選んでください", 13, TextAnchor.MiddleCenter, UiSkin.TextSub);

            // ソウル残高
            var purse = UiSkin.Card(stage, "Purse", new Vector2(StageW * 0.5f - 130, StageH * 0.5f - 52), new Vector2(220, 52), 12);
            UiFactory.Label(purse, "L", new Vector2(-58, 0), new Vector2(90, 24), "SOUL", 12, TextAnchor.MiddleLeft, UiSkin.TextSub);
            _soulText = UiSkin.Number(purse, "V", new Vector2(-8, 0), new Vector2(190, 30), "0", 24, UiSkin.Hex("#8f6bff"));

            // 左: 2 つの行き先（縦並び）。右: 冒険のステージマップ
            bool adv = _m.AdventureEnabled;
            if (adv)
            {
                MapNode(stage, "Town", new Vector2(-330, 52), "街", "ソウルでスキルと装備を買う", UiSkin.Hex("#c8961e"), OpenShop, new Vector2(250, 150));
                MapNode(stage, "Quest", new Vector2(-330, -116), "冒険", "", UiSkin.Accent, GoAdventure, new Vector2(250, 150));
                _mapPanel = UiSkin.Card(stage, "MapPanel", new Vector2(140, -32), new Vector2(640, 330), 14);
                _stageInfo = UiFactory.Label(_mapPanel, "StageInfo", new Vector2(0, 330 * 0.5f - 18), new Vector2(600, 20), "", 13, TextAnchor.MiddleCenter, UiSkin.Text);
                _stageInfo.fontStyle = FontStyle.Bold;
                RedrawStageMap();
            }
            else
            {
                MapNode(stage, "Town", new Vector2(-190, -20), "街", "ソウルでスキルと装備を買う", UiSkin.Hex("#c8961e"), OpenShop, new Vector2(300, 190));
                MapNode(stage, "Quest", new Vector2(190, -20), "冒険", "スロットを回して敵と戦う", UiSkin.Accent, GoAdventure, new Vector2(300, 190));
            }

            _infoText = UiFactory.Label(stage, "Info", new Vector2(0, -StageH * 0.5f + 62), new Vector2(StageW - 80, 22), _arriveMessage, 13, TextAnchor.MiddleCenter, UiSkin.Gold);

            UiSkin.Button(stage, "BtnTitle", new Vector2(-StageW * 0.5f + 90, -StageH * 0.5f + 30), new Vector2(140, 34), "タイトルへ",
                () => { _audio.UiPop(); StartCoroutine(BackToTitle()); }, UiSkin.Btn, 13, false, 8);

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
            var card = UiSkin.Card(parent, name, pos, size, 16);
            UiSkin.Img(card, "Accent", new Vector2(0, h * 0.5f - 9), new Vector2(w - 2, 6), UiSkin.Rounded(3), color);
            var t = UiFactory.Label(card, "Label", new Vector2(0, h * 0.5f - 51), new Vector2(w - 20, 40), label, h >= 180 ? 34 : 28, TextAnchor.MiddleCenter, UiSkin.Text);
            t.fontStyle = FontStyle.Bold;
            var d = UiFactory.Label(card, "Desc", new Vector2(0, h * 0.5f - 89), new Vector2(w - 20, 40), desc, 13, TextAnchor.MiddleCenter, UiSkin.TextSub);
            if (name == "Quest" && _m.AdventureEnabled) { _infoQuestDesc = d; RefreshQuestDesc(); }
            UiSkin.Button(card, "Go", new Vector2(0, -h * 0.5f + 40), new Vector2(w - 80, 44), "ここへ行く", () => { _audio.UiPop(); onClick(); }, color, 17, true, 12);
        }

        private Text _infoQuestDesc;

        /// <summary>冒険カードの説明: 現在地と残りG。</summary>
        private void RefreshQuestDesc()
        {
            if (_infoQuestDesc == null || !_m.AdventureEnabled) return;
            var n = _m.CurrentStage;
            var res = _m.Config.adventure?.resource;
            string line1 = n != null ? $"{n.id} {n.name} から再開" : "スロットを回して敵と戦う";
            string line2 = res != null && res.enabled ? $"{res.name} {Mathf.Max(0, _m.Adv.torches)} 本   {_m.Credit:N0} 枚" : $"残り {_m.Adv.spinsLeft} G";
            _infoQuestDesc.text = line1 + "\n" + line2;
        }

        /// <summary>右側のステージマップを描き直す。</summary>
        private void RedrawStageMap()
        {
            if (_mapPanel == null || !_m.AdventureEnabled) return;
            if (_mapView != null) Destroy(_mapView.gameObject);
            var cfg = _m.Config.adventure;
            _mapView = StageMapView.Build(_mapPanel, cfg, _m.Adv, new Vector2(610, 270));
            _mapView.anchoredPosition = new Vector2(0, -18);
            var n = _m.CurrentStage;
            string next = _m.Adv.nextId != null ? cfg.Find(_m.Adv.nextId)?.name : null;
            _stageInfo.text = $"{cfg.chapterName}   第{_m.Adv.chapter}章   現在地 {n?.id} {n?.name}" + (next != null ? $"   次 → {next}" : "");
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
                _infoText.text = $"{res.name}が無い。街で補給してから出発しよう";
                _infoText.color = UiSkin.Accent;
                _audio.UiPop();
                return;
            }
            if (_m.Credit < SlotMachine.BetCost)
            {
                _infoText.text = "クレジットが足りない。街で路銀を買おう";
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
            Destroy(_audio.gameObject);
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
            Destroy(_audio.gameObject);
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
