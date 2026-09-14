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

        private Canvas _canvas;
        private SafeStage _safe;
        private AudioManager _audio;
        private SlotMachine _m;
        private CanvasGroup _fade;
        private Text _infoText;
        private bool _busy;
        private GameObject _shopBox, _equipBox, _trophyBox, _atelierSettings;
        private System.Action _atelierRefresh;

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
            var bg = UiSkin.Img(_canvas.transform, "BG", Vector2.zero, Vector2.zero, null, UiSkin.Hex("#122e32"));
            UiSkin.Stretch(bg.rectTransform);
            _safe = SafeStage.Create(_canvas);
            _atelierRefresh = AtelierMap.Build(_safe.Stage, _m, GoAdventure, OpenShop, OpenEquip, OpenTrophy, OpenSettings,
                () => { _audio.UiPop(); StartCoroutine(BackToTitle()); }, out _infoText);
            _infoText.text = _arriveMessage;
            var fade = UiFactory.Panel(_canvas.transform,"Fade",Vector2.zero,new Vector2(4000,4000),Color.black);
            _fade = fade.gameObject.AddComponent<CanvasGroup>(); _fade.alpha=1; _fade.blocksRaycasts=false;
            StartCoroutine(FadeTo(0,.4f));
        }

        private void OpenSettings()
        {
            if(_busy || _atelierSettings != null) return;
            _atelierSettings = AtelierSettings.Build(_safe.Stage,_audio,()=>{Destroy(_atelierSettings);_atelierSettings=null;});
        }

        private void RefreshQuestDesc() => _atelierRefresh?.Invoke();
        private void RefreshSouls() => _atelierRefresh?.Invoke();

        // -------------------------------------------------------------- ACTION
        /// <summary>装備画面（冒険中の E と同じ窓）。着け替えたらその場でセーブ。</summary>
        private void OpenEquip()
        {
            if (_equipBox != null || _m.Config.equipment == null || !_m.Config.equipment.enabled) return;
            _audio.UiPop();
            _equipBox = EquipScreen.Build(_safe.Stage, _m, _audio, () => SaveData.Save(_m, _audio),
                                          () => { Destroy(_equipBox); _equipBox = null; RefreshQuestDesc(); });
        }

        /// <summary>実績と図鑑の窓。</summary>
        private void OpenTrophy()
        {
            if (_trophyBox != null) return;
            _audio.UiPop();
            _trophyBox = TrophyScreen.Build(_safe.Stage, _m, _audio, () => { Destroy(_trophyBox); _trophyBox = null; });
        }

        private void OpenShop()
        {
            if (_busy) return;
            if (_shopBox != null) { Destroy(_shopBox); _shopBox = null; }
            _shopBox = ShopScreen.Build(_safe.Stage, _m, _audio, () => RefreshSouls(), () => { Destroy(_shopBox); _shopBox = null; RefreshQuestDesc(); });
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
            AudioManager.Create().Motion(MotionCue.Travel);
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
            AudioManager.Create().Motion(MotionCue.Travel);
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
            if(kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if(_atelierSettings != null){Destroy(_atelierSettings);_atelierSettings=null;}
            else if(_equipBox != null){Destroy(_equipBox);_equipBox=null;}
            else if(_trophyBox != null){Destroy(_trophyBox);_trophyBox=null;}
            else if(_shopBox != null){Destroy(_shopBox);_shopBox=null;RefreshQuestDesc();}
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
