#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BBB.Tests
{
    public sealed class AdventurePresentationPlayTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static IEnumerator WaitFrames(int count)
        {
            int end=Time.frameCount+count;
            while(Time.frameCount<end)yield return null;
        }
        static IEnumerator WaitSeconds(float seconds)
        {
            float end=Time.time+seconds;
            while(Time.time<end)yield return null;
        }
        [UnityTest]
        public IEnumerator RewardsPauseAndResumeWithoutLosingDialogueOrGrantingAgain()
        {
            if(Application.productName!="shop-qa"||!Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"))
                Assert.Ignore("Requires isolated QA project and preferences");
            yield return new EnterPlayMode();
            foreach(var t in UnityEngine.Object.FindObjectsByType<TitleScreen>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(t.gameObject);
            foreach(var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(c.gameObject);
            yield return null;
            var m=GameDataLoader.CreateMachine(new SystemRandom(923));var audio=AudioManager.Create();audio.SeVolume=0;
            AtelierPreferences.Defaults();
            var game=new GameObject("AdventurePlayQA").AddComponent<GameController>();game.enabled=false;
            typeof(GameController).GetField("_m",Private).SetValue(game,m);typeof(GameController).GetField("_audio",Private).SetValue(game,audio);
            object Call(string name,params object[] args)=>typeof(GameController).GetMethod(name,Private).Invoke(game,args);
            T Get<T>(string name)=>(T)typeof(GameController).GetField(name,Private).GetValue(game);
            Call("BuildUi");Call("RefreshUi");
            var dialogue=Get<DialoguePresenter>("_dialogue");var gains=Get<RectTransform>("_gainLayer");int credit=m.Credit;
            m.Config.reelFx.emberGain.holdUntilBet=true;m.Config.reelFx.emberGain.betHoldMax=0;
            var enemy=Get<AdventureEnemyPresenter>("_adventureEnemy");
            var hero=Get<AdventureHeroPresenter>("_adventureHero");
            enemy.Show("slime",1,400);hero.Request("attack-f3-4",false);
            foreach(var icon in new[]{"ember","soul","book","ember"})Call("EnqueueGain",icon,120,null);
            yield return WaitSeconds(.25f);
            Assert.AreEqual(0,gains.childCount,"Rewards started before combat finished");
            hero.Cancel();enemy.Request("idle");Call("Update");
            // First-use art loading can span the fixed delay in a batch Editor. Await the
            // actual group, then its entrance, rather than sampling a scheduling frame.
            float deadline=Time.realtimeSinceStartup+10;
            while(gains.childCount<4&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(4,gains.childCount,"Same-frame rewards must be simultaneous");
            hero.Request("attack-f3-4",false);Call("Update");
            var pausedPosition=((RectTransform)gains.GetChild(0)).anchoredPosition;
            var pausedAlpha=gains.GetChild(0).GetComponent<CanvasGroup>().alpha;
            Assert.AreEqual(0,Get<CanvasGroup>("_gainVisibility").alpha,"Combat must hide active rewards");
            yield return WaitSeconds(.3f);
            Assert.AreEqual(pausedPosition,((RectTransform)gains.GetChild(0)).anchoredPosition,"Hidden entrance advanced");
            Assert.AreEqual(pausedAlpha,gains.GetChild(0).GetComponent<CanvasGroup>().alpha,"Hidden reward faded");
            hero.Cancel();enemy.Request("idle");Call("Update");
            Assert.AreEqual(1,Get<CanvasGroup>("_gainVisibility").alpha,"Combat rewards did not resume");
            yield return WaitSeconds(m.Config.reelFx.emberGain.inSeconds+.2f);
            dialogue.Begin("サリア","報酬を確かめて、先へ進もう。");dialogue.Reveal();
            yield return null;yield return null;
            foreach(RectTransform band in gains)
            {
                Assert.Greater(band.anchoredPosition.x,300,"Late dialogue did not move active reward into the side lane");
                foreach(Transform child in band.Find("Row"))if(child.name.StartsWith("Num"))Assert.Greater(child.localScale.x,.9f,"Digits never left their entrance scale");
            }
            var settings=Get<GameObject>("_settingsBox");settings.SetActive(true);Call("Update");
            Assert.AreEqual(0,Get<CanvasGroup>("_gainVisibility").alpha,"Rewards must be hidden behind a modal");
            yield return WaitSeconds(.25f);Assert.AreEqual(4,gains.childCount,"Modal consumed held rewards");
            settings.SetActive(false);Call("Update");Assert.AreEqual(1,Get<CanvasGroup>("_gainVisibility").alpha);
            typeof(GameController).GetField("_gainBetSerial",Private).SetValue(game,Get<int>("_gainBetSerial")+1);
            yield return WaitSeconds(m.Config.reelFx.emberGain.betWaitSeconds+m.Config.reelFx.emberGain.outSeconds+.3f);
            Assert.AreEqual(0,gains.childCount,"BET did not release reward group");Assert.AreEqual(credit,m.Credit,"Presentation granted rewards again");
            Call("EnqueueGain","ember",1,null);
            yield return WaitSeconds(m.Config.reelFx.emberGain.inSeconds+.2f);
            Assert.AreEqual(1,gains.childCount);
            hero.Request("attack-f3-4",false);Call("Update");
            typeof(GameController).GetField("_gainBetSerial",Private).SetValue(game,Get<int>("_gainBetSerial")+1);
            yield return WaitSeconds(.3f);Assert.AreEqual(1,gains.childCount,"BET consumed a hidden reward");
            hero.Cancel();enemy.Hide();Call("Update");
            yield return WaitSeconds(m.Config.reelFx.emberGain.betWaitSeconds+m.Config.reelFx.emberGain.outSeconds+.3f);
            Assert.AreEqual(0,gains.childCount,"BET during combat was forgotten");
            Assert.AreEqual(credit,m.Credit,"Resuming combat rewards granted again");
            m.Curse.Taken.Add(new CurseInstance{curseId="c_drain",curseName="燃え急ぎ",curseEffect=CurseEffects.TorchDrain,curseValue=12,blessName="魂寄せ",blessEffect="soulGain",blessValue=8});
            var hud=Get<CurseHudPresenter>("_curseHud");hud.Refresh(false);
            var focus=hud.GetComponentInChildren<CurseHudFocus>();
            ExecuteEvents.Execute(focus.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);
            yield return WaitSeconds(AdventurePresentation.Config.tooltipDelay+.05f);hud.Refresh(false);Assert.IsTrue(hud.TooltipVisible,"Hover did not show tooltip");
            ExecuteEvents.Execute(focus.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerExitHandler);Assert.IsFalse(hud.TooltipVisible,"Hover exit did not close");
            focus.GetComponent<Button>().onClick.Invoke();Assert.IsTrue(hud.TooltipVisible,"Touch/click did not pin tooltip");Call("CloseModals");hud.Refresh(false);Assert.IsFalse(hud.TooltipVisible,"Close reopened a pinned tooltip");
            ExecuteEvents.Execute(focus.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.selectHandler);
            yield return WaitSeconds(AdventurePresentation.Config.tooltipDelay+.05f);hud.Refresh(false);Assert.IsTrue(hud.TooltipVisible,"Focus did not show tooltip");hud.HideTooltip();
            hud.BindMachine(GameDataLoader.CreateMachine(new SystemRandom(924)));
            Assert.AreEqual(0,hud.GroupCount,"Recreated machine retained old curses");
            Assert.IsFalse(hud.gameObject.activeSelf,"Recreated machine retained old HUD");
            // Real coroutine and Button callback: first press reveals, second press moves to the next line.
            var opening=OpeningPlayer.Create(null,"サリア");var scene=new OpeningScene{ id="escape",dim=.6f };
            scene.lines.Add(new StoryLine{speaker="",text="灯が……もう、指先ほどしかない。"});
            scene.lines.Add(new StoryLine{speaker="声",text="……戻るのか"});
            opening.StartCoroutine(opening.Play(scene));yield return WaitFrames(3);
            var op=opening.GetComponentInChildren<DialoguePresenter>();Assert.AreEqual("サリア",op.Name.text);
            op.transform.Find("Background").GetComponent<Button>().onClick.Invoke();yield return WaitFrames(3);
            Assert.IsTrue(op.Revealed);Assert.AreEqual("サリア",op.Name.text,"One click skipped the line");
            op.transform.Find("Background").GetComponent<Button>().onClick.Invoke();yield return WaitFrames(3);
            Assert.AreEqual("声",op.Name.text,"Second click did not advance");
            var skip=opening.GetComponentsInChildren<Button>();foreach(var b in skip)if(b.name=="Skip")b.onClick.Invoke();
            Assert.IsTrue(opening.Skipped);opening.Close();UnityEngine.Object.Destroy(game.gameObject);
            yield return new ExitPlayMode();
        }
    }
}
#endif
