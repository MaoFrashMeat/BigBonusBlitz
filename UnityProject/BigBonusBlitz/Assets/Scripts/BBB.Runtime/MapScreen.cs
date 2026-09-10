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
            var title = UiFactory.Label(stage, "Title", new Vector2(0, StageH * 0.5f - 44), new Vector2(StageW, 34), "ミニマップ", 26, TextAnchor.MiddleCenter, UiSkin.Text);
            title.fontStyle = FontStyle.Bold;
            UiFactory.Label(stage, "Sub", new Vector2(0, StageH * 0.5f - 74), new Vector2(StageW, 20), "行き先を選んでください", 13, TextAnchor.MiddleCenter, UiSkin.TextSub);

            // ソウル残高
            var purse = UiSkin.Card(stage, "Purse", new Vector2(StageW * 0.5f - 130, StageH * 0.5f - 52), new Vector2(220, 52), 12);
            // 左から順に領域を取る: アイコン → SOUL → 数字（docs/ui_rules.md 1 番）
            UiSkin.Img(purse, "Icon", new Vector2(-92, 0), new Vector2(20, 20), UiSkin.Icon("soul", 64), Color.white);
            UiFactory.Label(purse, "L", new Vector2(-52, 0), new Vector2(52, 24), "SOUL", 12, TextAnchor.MiddleLeft, UiSkin.TextSub);
            _soulText = UiSkin.Number(purse, "V", new Vector2(55, 0), new Vector2(100, 30), "0", 22, UiSkin.Hex("#8f6bff"));

            // 左: 2 つの行き先（縦並び）。右: 冒険のステージマップ
            bool adv = _m.AdventureEnabled;
            if (adv)
            {
                MapNode(stage, "Town", new Vector2(-330, 78), "街", "ソウルでスキルと装備を買う", UiSkin.Hex("#c8961e"), OpenShop, new Vector2(250, 170));
                MapNode(stage, "Quest", new Vector2(-330, -104), "冒険", "", UiSkin.Accent, GoAdventure, new Vector2(250, 170));
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

            // 上から順に領域を取る。隣の位置を前の要素から出すので、重なりが式の上で起きない
            const float accentH = 6f, labelH = 34f, descH = 38f, btnH = 42f, gap = 6f, btnBottom = 16f;
            float top = h * 0.5f;
            float accentCy = top - accentH * 0.5f;
            float labelCy = accentCy - accentH * 0.5f - gap - labelH * 0.5f;
            float descCy = labelCy - labelH * 0.5f - 2f - descH * 0.5f;
            float btnCy = -top + btnBottom + btnH * 0.5f;

            UiSkin.Img(card, "Accent", new Vector2(0, accentCy), new Vector2(w - 2, accentH), UiSkin.Rounded(3), color);
            var t = UiFactory.Label(card, "Label", new Vector2(0, labelCy), new Vector2(w - 20, labelH), label, h >= 180 ? 32 : 28, TextAnchor.MiddleCenter, UiSkin.Text);
            t.fontStyle = FontStyle.Bold;
            var d = UiFactory.Label(card, "Desc", new Vector2(0, descCy), new Vector2(w - 18, descH), desc, 12, TextAnchor.UpperCenter, UiSkin.TextSub);
            if (name == "Quest" && _m.AdventureEnabled) { _infoQuestDesc = d; RefreshQuestDesc(); }
            UiSkin.Button(card, "Go", new Vector2(0, btnCy), new Vector2(w - 70, btnH), "ここへ行く", () => { _audio.UiPop(); onClick(); }, color, 16, true, 12);
        }

        private Text _infoQuestDesc;

        /// <summary>冒険カードの説明: 現在地と残りG。</summary>
        private void RefreshQuestDesc()
        {
            if (_infoQuestDesc == null || !_m.AdventureEnabled) return;
            var n = _m.CurrentStage;
            var res = _m.Config.adventure?.resource;
            string line1 = n != null ? $"{n.id} {n.name} から再開" : "スロットを回して敵と戦う";
            string line2 = res != null && res.enabled ? $"{res.hpName} {_m.Hp} / {_m.HpMax}   {res.name} {Mathf.Max(0, _m.Adv.torches)} 個   エンバー {_m.Credit:N0}" : $"残り {_m.Adv.spinsLeft} G";
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
            string lap = _m.Adv.chapter > 1 ? $"（{_m.Adv.chapter} 周目）" : "";
            _stageInfo.text = $"{cfg.chapterName}{lap}   現在地 {n?.id} {n?.name}" + (next != null ? $"   次 → {next}" : "");
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
