#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace BBB.Tests
{
    public sealed class AdventureEnemyReelTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static GameController game;
        static SlotMachine machine;
        static bool enemyContact,earlyHit,sawHit,sawGuard;
        static int heroContacts;
        static string output;
        static List<string> trace;
        static Camera camera;
        static string still;
        static bool captureFrames;
        static int frame;
        static int clearStrikeFrames;
        static bool trackOutgoing,earlyEnemyHit,sawApproach;
        static float largestContactAimError;
        static float spearContactAimError,swordContactAimError;
        static bool spearReturnedToRest,swordReturnedToRest;
        static string impactStill;
        static bool heroContactStill;
        static readonly List<string> battleTimeline=new List<string>();
        static string timelineScenario,lastTimelineState;
        static readonly Dictionary<string,int> timelineScenarioCounts=new Dictionary<string,int>();
        [UnityTearDown]
        public IEnumerator RestoreCaptureClock()
        {
            captureFrames=false;
            Time.captureFramerate=0;
            if(!string.IsNullOrEmpty(output)&&battleTimeline.Count>0)
            {
                File.WriteAllLines(Path.Combine(output,"battle-timeline.csv"),new[]{"scenario,unitySeconds,event,heroState,heroPose,enemyState,enemyPose,stance,heroHp,enemyHp,payout,enemyActive,captureFps"}.Concat(battleTimeline));
            }
            if(Application.isPlaying)yield return new ExitPlayMode();
        }
        static T Get<T>(string name)=>(T)typeof(GameController).GetField(name,Private).GetValue(game);
        static void Set(string name,object value)=>typeof(GameController).GetField(name,Private).SetValue(game,value);
        static object Call(string name,params object[] args)=>typeof(GameController).GetMethod(name,Private).Invoke(game,args);
        static void OnEnemyCue(string cue){if(cue=="contact")enemyContact=true;}
        static void OnHeroCue(string cue)
        {
            if(cue!="contact")return;
            Timeline("hero-cue:"+cue);
            if(!string.IsNullOrEmpty(timelineScenario))
                Assert.IsNotNull(Get<MarqueeBand>("_engageBandTop"),"Combat intensity band disappeared before the active contact");
            heroContacts++;heroContactStill=true;
            if(!trackOutgoing)return;
            var hero=Get<AdventureHeroPresenter>("_adventureHero");
            var enemy=Get<AdventureEnemyPresenter>("_adventureEnemy");
            var target=enemy.Active?enemy.View.Body.rectTransform:Get<RectTransform>("_enemyRt");
            var parent=hero.View.Root.parent;
            float targetX=parent.InverseTransformPoint(target.TransformPoint(target.rect.center)).x;
            float tipX=parent.InverseTransformPoint(hero.View.WeaponTipWorld(hero.View.Player.PoseIndex,hero.View.Player.WeaponId)).x;
            largestContactAimError=Mathf.Max(largestContactAimError,Mathf.Abs(targetX-tipX));
        }
        static void Observe()
        {
            var heroPresenter=Get<AdventureHeroPresenter>("_adventureHero");
            string state=heroPresenter.View.Player.State;
            Timeline("state");
            if(trackOutgoing&&heroPresenter.Advance>0)sawApproach=true;
            if(state=="hit"){sawHit=true;if(!enemyContact)earlyHit=true;}
            sawGuard|=state=="guard";
            string enemy=Get<AdventureEnemyPresenter>("_adventureEnemy").View.Player.State;
            if(trackOutgoing&&(enemy=="hit"||enemy=="defeat"))
            {
                if(heroContacts==0)earlyEnemyHit=true;
                if(impactStill!=null)
                {
                    trace.Add(impactStill+" / observed hero="+state+" pose="+Get<AdventureHeroPresenter>("_adventureHero").View.Player.PoseIndex+" contacts="+heroContacts+" enemy="+enemy);
                    File.WriteAllLines(Path.Combine(output,"trace.txt"),trace);
                    Capture(impactStill);impactStill=null;
                }
            }
            if(still!=null&&state=="guard"){Capture(still);still=null;}
        }
        static void ObserveAfterUpdate()
        {
            Observe();
            if(heroContactStill)
            {
                Assert.AreEqual(0,Get<CanvasGroup>("_gainVisibility").alpha,"Rewards covered hero contact");
                Capture("hero-contact-"+(Get<AdventureHeroPresenter>("_adventureHero").View.Player.WeaponId=="w_spear"?"spear":"sword"));
                heroContactStill=false;
            }
            if(captureFrames&&Get<AdventureEnemyPresenter>("_adventureEnemy").View.Player.State=="strike")
            {
                Assert.IsFalse(Get<GameObject>("_dialogBox").activeSelf,"Explanation covered an actual incoming strike");
                Assert.IsNotNull(Get<MarqueeBand>("_engageBandTop"),"Combat intensity band disappeared during an incoming strike");
                Assert.AreEqual(0,Get<CanvasGroup>("_gainVisibility").alpha,"Rewards covered incoming strike");
                clearStrikeFrames++;
            }
        }
        static IEnumerator Tick(float seconds)
        {
            float until=Time.time+seconds;
            while(Time.time<until){Observe();Call("Update");ObserveAfterUpdate();if(captureFrames)Capture("frames/frame"+(frame++).ToString("D4"));yield return null;}
        }
        static IEnumerator Spin(Flag flag,EngageRole role)
        {
            string baseScenario=flag+"-"+role;timelineScenarioCounts.TryGetValue(baseScenario,out int index);timelineScenarioCounts[baseScenario]=++index;
            timelineScenario=baseScenario+"#"+index.ToString("D2");lastTimelineState=null;Timeline("before-bet");
            machine.DebugForceFlag=flag;Set("_lastSpinStartTime",-100f);Call("OnBetClicked");Set("_stopUnlockTime",0f);
            Timeline("bet");
            Assert.IsTrue(machine.IsGameActive,"BET was blocked");
            var reels=Get<ReelView[]>("_reels");
            for(int stopped=0;stopped<3;stopped++)
            {
                int i=Array.FindIndex(machine.Stopped,x=>x==null);
                if(flag.IsBell()&&machine.Navi.InChoice&&machine.Stopped[machine.Navi.correctReel]==null)i=machine.Navi.correctReel;
                Symbol target=flag.IsBell()?Symbol.STAR:flag.IsReplay()?Symbol.REPLAY:flag.IsCherry()?Symbol.CHERRY:Symbol.BLANK;
                int pos=Array.IndexOf(machine.Strips[i],target);if(pos<0)pos=0;
                typeof(ReelView).GetField("_pos",Private).SetValue(reels[i],(float)pos+.5f);
                typeof(GameController).GetMethod("StopReel",Private,null,new[]{typeof(int)},null).Invoke(game,new object[]{i});
                Observe();Timeline("stop-reel-"+i);
                if(flag.IsBell()&&stopped==1)Assert.AreNotEqual("attack-f3-4",Get<AdventureHeroPresenter>("_adventureHero").View.Player.State,"Battle bell attacked before result");
            }
            var win=WinEvaluator.Evaluate(machine.Stopped,machine.BonusMode,machine.CurrentPayouts);
            Assert.AreEqual(role,EngageBattle.RoleOf(win,false,false,machine.Config.engage),"Fixture did not land requested role");
            float deadline=Time.realtimeSinceStartup+12;
            while(machine.IsGameActive&&Time.realtimeSinceStartup<deadline){Observe();Call("Update");ObserveAfterUpdate();if(captureFrames)Capture("frames/frame"+(frame++).ToString("D4"));yield return null;}
            Assert.IsFalse(machine.IsGameActive,"Reels did not reach actual Evaluate");Observe();
            Timeline("evaluate-complete");
            trace.Add(flag+" / "+role+" / hero="+Get<AdventureHeroPresenter>("_adventureHero").View.Player.State+" / enemy="+Get<AdventureEnemyPresenter>("_adventureEnemy").View.Player.State+" / hp="+machine.EngageHp);
            File.WriteAllLines(Path.Combine(output,"trace.txt"),trace);
        }
        static void Timeline(string evt)
        {
            if(string.IsNullOrEmpty(timelineScenario)||game==null||machine==null)return;
            var hero=Get<AdventureHeroPresenter>("_adventureHero");var enemy=Get<AdventureEnemyPresenter>("_adventureEnemy");
            string heroPose=hero.View.Player.State=="idle"||hero.View.Player.State=="walk"?"-":hero.View.Player.PoseIndex.ToString();
            string enemyPose=enemy.View.Player.State=="idle"||enemy.View.Player.State=="walk"?"-":enemy.View.Player.PoseIndex.ToString();
            string signature=hero.View.Player.State+":"+heroPose+":"+enemy.View.Player.State+":"+enemyPose+":"+machine.EngageStance+":"+machine.Hp+":"+machine.EngageHp+":"+machine.Credit;
            if(evt=="state"&&signature==lastTimelineState)return;
            lastTimelineState=signature;
            battleTimeline.Add(string.Join(",",new[]{timelineScenario,Time.time.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture),evt,hero.View.Player.State,heroPose,enemy.View.Player.State,enemyPose,machine.EngageStance.ToString(),machine.Hp.ToString(),machine.EngageHp.ToString(),machine.Credit.ToString(),machine.EnemyActive.ToString().ToLowerInvariant(),Time.captureFramerate.ToString()}));
        }
        static void ResetObservation(){enemyContact=false;earlyHit=false;sawHit=false;sawGuard=false;heroContacts=0;trackOutgoing=false;earlyEnemyHit=false;sawApproach=false;largestContactAimError=0;impactStill=null;heroContactStill=false;}
        static Image EngageBandStateAccent()
        {
            var band=Get<MarqueeBand>("_engageBandTop");
            return band==null?null:band.transform.Find("StateAccent")?.GetComponent<Image>();
        }
        static string EngageBandCopy()
        {
            var band=Get<MarqueeBand>("_engageBandTop");
            return band==null?"":band.transform.Find("Text")?.GetComponent<Text>()?.text??"";
        }
        static void SetupEnemy(string type)
        {
            timelineScenario=null;lastTimelineState=null;
            machine.EnemyActive=true;machine.PendingTier2=true;machine.IsTier2=false;
            machine.ActiveEnemyTable=machine.EnemyTables.First(e=>e.enemyType==type&&!e.IsBoss);
            Call("ShowEnemy",machine.ActiveEnemyTable);
            Get<AdventureEnemyPresenter>("_adventureEnemy").View.Player.Cue+=OnEnemyCue;
            Get<AdventureHeroPresenter>("_adventureHero").Cancel();
        }
        static void PreparePotionSpin()
        {
            // Enter the already-open potion G so the next real BET reaches StepEngage's
            // potion branch. PendingTier2 is consumed deliberately; DrawLottery must not
            // reset EngagePotionSpin while this fixture is exercising the UI contract.
            machine.PendingTier2=false;machine.IsTier2=true;machine.EngageSet=1;machine.EngageTurn=1;
            machine.EngageStance=EngageStance.None;machine.EngagePotionSpin=true;machine.EngageJudgeSpin=false;
            machine.EngageNextLarge=false;machine.EngageHpMax=Math.Max(1,machine.ActiveEnemyTable.engageHp>0?machine.ActiveEnemyTable.engageHp:(machine.Config.engage?.enemyHp??100));machine.EngageHp=machine.EngageHpMax;
        }
        static void Capture(string name)
        {
            if(camera==null){camera=new GameObject("JourneyQACamera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.backgroundColor=AzureMapSkin.Navy;}
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas).ToArray();
            foreach(var c in canvases)
            {
                var scaler=c.GetComponent<CanvasScaler>();if(scaler!=null)UnityEngine.Object.DestroyImmediate(scaler);
                c.renderMode=RenderMode.WorldSpace;c.transform.position=Vector3.zero;c.transform.localScale=Vector3.one;c.worldCamera=camera;
                foreach(var safe in c.GetComponentsInChildren<SafeStage>()){safe.enabled=false;safe.Stage.SetParent(c.transform,false);}
            }
            foreach(var size in (captureFrames?new[]{new Vector2Int(1280,720)}:new[]{new Vector2Int(1280,720),new Vector2Int(2556,1179)}))
            {
                float w=540f*size.x/size.y;
                foreach(var c in canvases){((RectTransform)c.transform).sizeDelta=new Vector2(w,540);foreach(Transform child in c.transform)if(child.name=="Stage")child.localScale=Vector3.one*Mathf.Min(1,w/((RectTransform)child).sizeDelta.x);}
                Canvas.ForceUpdateCanvases();foreach(var c in canvases)foreach(var t in c.GetComponentsInChildren<Text>())t.font.RequestCharactersInTexture(t.text,t.fontSize,t.fontStyle);
                for(int pass=0;pass<3;pass++){foreach(var c in canvases)foreach(var g in c.GetComponentsInChildren<Graphic>()){g.SetAllDirty();g.Rebuild(CanvasUpdate.PreRender);g.canvasRenderer.cull=false;}Canvas.ForceUpdateCanvases();}
                var rt=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+"-"+size.x+".png"),image.EncodeToPNG());
                camera.targetTexture=null;RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);
            }
        }
        [UnityTest]
        public IEnumerator ReelResultsOwnBattleActions()
        {
            if(Application.productName!="shop-qa"||!Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"))Assert.Ignore("Isolated QA required");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();
            output=Environment.GetEnvironmentVariable("ENEMY_REEL_OUTPUT");Assert.IsNotEmpty(output);Directory.CreateDirectory(output);trace=new List<string>();battleTimeline.Clear();timelineScenarioCounts.Clear();timelineScenario=null;lastTimelineState=null;
            foreach(var title in UnityEngine.Object.FindObjectsByType<TitleScreen>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(title.gameObject);
            foreach(var cv in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(cv.gameObject);yield return null;
            var audio=AudioManager.Create();audio.SeVolume=0;audio.BgmVolume=0;
            machine=GameDataLoader.CreateMachine(new SystemRandom(928));machine.Credit=3000;machine.Adv.opDone=true;
            game=new GameObject("EnemyReelQA").AddComponent<GameController>();game.enabled=false;Set("_m",machine);Set("_audio",audio);Call("BuildUi");Call("RefreshUi");
            Get<AdventureHeroPresenter>("_adventureHero").View.Player.Cue+=OnHeroCue;
            SetupEnemy("slime");yield return Tick(4);ResetObservation();
            yield return Spin(Flag.HAZE,EngageRole.Lose);yield return Tick(.6f);
            Assert.AreEqual(EngageStance.EnemyAttack,machine.EngageStance);
            Assert.IsFalse(sawHit,"Stance-only spin incorrectly played hero damage");
            Assert.AreEqual("ready",Get<AdventureEnemyPresenter>("_adventureEnemy").View.Player.State);
            ResetObservation();yield return Spin(Flag.HAZE,EngageRole.Lose);yield return Tick(1);
            Assert.IsTrue(enemyContact&&sawHit);Assert.IsFalse(earlyHit,"Hero flinched before enemy contact");Assert.AreEqual(0,heroContacts);
            SetupEnemy("goblin");yield return Tick(4);ResetObservation();
            machine.Equip.Worn[EquipSlot.Weapon]=new EquipItem{baseId="w_spear",slot=EquipSlot.Weapon};Call("RefreshUi");
            yield return Spin(Flag.REPLAY_A,EngageRole.Small);yield return Tick(.8f);Assert.AreEqual(EngageStance.HeroCharge,machine.EngageStance);Assert.AreEqual(0,heroContacts);
            machine.Config.engage.chargeLoseHit=0;machine.Config.engage.chargeLoseGuard=100;machine.Config.engage.chargeLoseDodge=0;
            ResetObservation();still="guard-after-real-reels";yield return Spin(Flag.HAZE,EngageRole.Lose);yield return Tick(1);
            Assert.IsTrue(enemyContact&&sawGuard);Assert.IsFalse(sawHit);Assert.AreEqual(0,heroContacts);
            SetupEnemy("slime");yield return Tick(4);ResetObservation();yield return Spin(Flag.HAZE,EngageRole.Lose);yield return Tick(.7f);
            ResetObservation();yield return Spin(Flag.BELL_A,EngageRole.Small);yield return Tick(1);
            Assert.IsTrue(enemyContact);Assert.IsFalse(sawHit);Assert.AreEqual(0,heroContacts,"Dodge incorrectly attacked");
            SetupEnemy("goblin");yield return Tick(4);ResetObservation();yield return Spin(Flag.HAZE,EngageRole.Lose);yield return Tick(.7f);
            ResetObservation();trackOutgoing=true;clearStrikeFrames=0;Time.captureFramerate=12;captureFrames=true;Directory.CreateDirectory(Path.Combine(output,"frames"));
            yield return Spin(Flag.CHERRY_A,EngageRole.Rare);yield return Tick(3);captureFrames=false;Time.captureFramerate=0;
            Assert.IsTrue(enemyContact);Assert.IsFalse(sawHit);Assert.AreEqual(1,heroContacts,"Counter stroke duplicated");
            Assert.IsFalse(earlyEnemyHit,"Counter hit the enemy before the hero contact");
            Assert.Greater(clearStrikeFrames,0,"No incoming strike was rendered without the explanation");
            Assert.Less(machine.EngageHp,machine.EngageHpMax);
            SetupEnemy("slime");yield return Tick(4);
            machine.Equip.Worn[EquipSlot.Weapon]=new EquipItem{baseId="w_spear",slot=EquipSlot.Weapon};Call("RefreshUi");
            ResetObservation();yield return Spin(Flag.REPLAY_A,EngageRole.Small);yield return Tick(.7f);
            ResetObservation();trackOutgoing=true;impactStill="spear-impact";Time.captureFramerate=60;yield return Spin(Flag.BELL_A,EngageRole.Small);yield return Tick(1);Time.captureFramerate=0;Assert.IsFalse(sawHit);Assert.AreEqual(1,heroContacts,"Medium attack stroke duplicated");Assert.IsTrue(sawApproach,"Successful runtime spear attack did not use the enabled approach");
            Assert.LessOrEqual(largestContactAimError,2f,"Spear tip missed the target center at its contact pose");
            spearContactAimError=largestContactAimError;
            Assert.IsFalse(earlyEnemyHit,"Spear hit the enemy before the hero contact");
            Assert.Less(machine.EngageHp,machine.EngageHpMax);
            Assert.IsNull(Get<MarqueeBand>("_engageBandTop"),"Combat band remained over the result/reward phase after recovery");
            Capture("attack-resolved");
            yield return Tick(1);spearReturnedToRest=Mathf.Abs(Get<AdventureHeroPresenter>("_adventureHero").Advance)<=.01f;
            Assert.IsTrue(spearReturnedToRest,"Spear approach did not return to its rest position after recovery");
            Assert.AreEqual(1,Get<CanvasGroup>("_gainVisibility").alpha,"Rewards remained hidden after recovery");
            Assert.Greater(Get<RectTransform>("_gainLayer").childCount,0,"Battle reward was lost");
            Capture("combat-reward-resumed");
            // Controlled presentation clock checks suspension, modal recovery and story priority.
            Call("HideDialogue");Get<AdventureHeroPresenter>("_adventureHero").Cancel();
            var enemy=Get<AdventureEnemyPresenter>("_adventureEnemy");enemy.Request("idle");
            var dialogue=Get<DialoguePresenter>("_dialogue");
            Set("_explainActive",true);
            const string longExplanation="敵の攻撃を見切って反撃した。槍を構え直し、次の一撃に備えよう。戦闘の動作中はこの説明を待機し、動作が終わると同じ位置から文字送りを再開する。設定を開いている間にも文章を勝手に進めず、戻った後に読み続けられる。これは長文の表示と再開を確認するための検証用メッセージです。";
            var line=(IEnumerator)Call("ShowLine","説明",longExplanation,Color.white,Color.white,12f,true);
            Set("_dialogRoutine",game.StartCoroutine(line));
            int visible=dialogue.VisibleElements;
            enemy.Request("strike");Call("Update");
            var settings=Get<GameObject>("_settingsBox");settings.SetActive(true);
            yield return Tick(.25f);
            Assert.IsFalse(dialogue.gameObject.activeSelf);Assert.AreEqual(visible,dialogue.VisibleElements,"Hidden explanation consumed characters");
            settings.SetActive(false);enemy.Step(1);Call("Update");yield return Tick(.25f);
            Assert.IsTrue(dialogue.gameObject.activeSelf);Assert.Greater(dialogue.VisibleElements,visible,"Explanation failed to resume");Capture("explanation-resumed");
            enemy.Request("strike");Call("Update");Call("HideDialogue");enemy.Step(1);yield return Tick(.2f);
            Assert.IsFalse(dialogue.gameObject.activeSelf,"Cancelled explanation returned later");
            enemy.Request("strike");
            line=(IEnumerator)Call("ShowLine","サリア","ここは私に任せて！",Color.white,Color.white,12f,false);
            Set("_dialogRoutine",game.StartCoroutine(line));yield return null;
            Assert.IsTrue(dialogue.gameObject.activeSelf,"Combat incorrectly hid story dialogue");Call("HideDialogue");
            SetupEnemy("goblin");yield return Tick(4);
            machine.Equip.Worn.Remove(EquipSlot.Weapon);Call("RefreshUi"); // Default blade.
            ResetObservation();yield return Spin(Flag.REPLAY_A,EngageRole.Small);yield return Tick(.7f);
            machine.EngageHp=1;ResetObservation();trackOutgoing=true;impactStill="sword-lethal-impact";
            Time.captureFramerate=60;yield return Spin(Flag.BELL_A,EngageRole.Small);yield return Tick(2);Time.captureFramerate=0;
            swordReturnedToRest=Mathf.Abs(Get<AdventureHeroPresenter>("_adventureHero").Advance)<=.01f;
            Assert.IsTrue(swordReturnedToRest,"Sword approach did not return to its rest position after recovery");
            Assert.AreEqual(1,heroContacts,"Lethal sword stroke was interrupted or duplicated");Assert.IsFalse(earlyEnemyHit,"Defeat started before sword impact");
            Assert.IsTrue(sawApproach,"Successful runtime sword attack did not use the enabled approach");
            Assert.LessOrEqual(largestContactAimError,2f,"Sword tip missed the target center at its contact pose");
            swordContactAimError=largestContactAimError;
            Assert.IsFalse(machine.EnemyActive);Assert.IsFalse(enemy.Active,"Lethal result never completed");
            SetupEnemy("slime");yield return Tick(4);machine.EngageHp=1;machine.Config.engage.judgeMin=100;machine.Config.engage.judgeMax=100;
            // The fixture enters the already-open Judge G state.  PendingTier2 must be
            // consumed here; otherwise the next real BET's DrawLottery resets the
            // injected flag before Lever can roll the Judge result.
            machine.PendingTier2=false;machine.IsTier2=true;machine.EngageSet=machine.EngageSets+1;machine.EngageTurn=1;
            machine.EngageJudgeSpin=true;machine.JudgeHeat=3;machine.JudgePercentNow=100;machine.JudgeWillWin=true;
            ResetObservation();trackOutgoing=true;impactStill="judge-contact";Time.captureFramerate=60;yield return Spin(Flag.BELL_A,EngageRole.Small);yield return Tick(2);Time.captureFramerate=0;
            Assert.AreEqual(1,heroContacts,"Judge finisher did not use one contact");Assert.IsFalse(earlyEnemyHit,"Judge resolved before hero contact");Assert.IsFalse(machine.EnemyActive,"Judge win did not resolve enemy");
            // Potion roulette: Lose heals LIFE, Small arms the next large attack, and
            // Rare resolves the enemy immediately. Each case still goes through BET→
            // stop→Evaluate; only the encounter state is injected by the fixture.
            SetupEnemy("slime");yield return Tick(4);PreparePotionSpin();
            AdventureDirector.SetHp(machine.Config.adventure,machine.Adv,Math.Max(1,machine.HpMax-20),machine.TorchSpinsPerUnit);
            int hpBeforePotion=machine.Hp;int enemyHpBeforePotion=machine.EngageHp;ResetObservation();yield return Spin(Flag.HAZE,EngageRole.Lose);yield return Tick(2);
            Assert.Greater(machine.Hp,hpBeforePotion,"Potion heal did not restore LIFE");Assert.AreEqual(enemyHpBeforePotion,machine.EngageHp,"Potion heal changed enemy HP");Assert.IsTrue(machine.EnemyActive);
            SetupEnemy("goblin");yield return Tick(4);PreparePotionSpin();ResetObservation();yield return Spin(Flag.REPLAY_A,EngageRole.Small);yield return Tick(2);
            Assert.IsTrue(machine.EngageNextLarge,"Potion roulette did not arm the next large attack");Assert.IsTrue(machine.EnemyActive);
            var waveAccent=EngageBandStateAccent();Assert.IsNotNull(waveAccent,"Engage intensity band was not rebuilt");Assert.Greater(waveAccent.color.r,waveAccent.color.b,"Large attack did not raise the warm intensity accent");Assert.AreEqual(1f,waveAccent.color.a,"Intensity accent must stay opaque against the battle art");Assert.IsTrue(EngageBandCopy().Contains("大攻撃"),"Large attack reservation was missing from the band copy");
            machine.EngagePotionSpin=false;machine.EngageTurn=2;machine.EngageStance=EngageStance.HeroCharge;ResetObservation();trackOutgoing=true;impactStill="potion-large-attack";Time.captureFramerate=60;yield return Spin(Flag.REPLAY_A,EngageRole.Small);yield return Tick(1);Time.captureFramerate=0;
            Assert.AreEqual(1,heroContacts,"Potion large attack did not use one contact");Assert.IsFalse(earlyEnemyHit);Assert.Less(machine.EngageHp,machine.EngageHpMax);
            SetupEnemy("slime");yield return Tick(4);PreparePotionSpin();ResetObservation();yield return Spin(Flag.CHERRY_A,EngageRole.Rare);yield return Tick(2);
            Assert.IsFalse(machine.EnemyActive,"Rare potion did not resolve the enemy");Assert.AreEqual(0,heroContacts,"Potion defeat incorrectly played a hero attack");
            SetupEnemy("slime");yield return Tick(4);Call("HideDialogue");ResetObservation();
            var hero=Get<AdventureHeroPresenter>("_adventureHero");
            timelineScenario=null;lastTimelineState=null;
            game.StartCoroutine((IEnumerator)Call("EngageStepRoutine",new EngageStep{outcome=EngageOutcome.Attack,attackSize=AttackSize.Medium,dealt=35}));
            Assert.AreEqual("idle",enemy.View.Player.State,"Attack applied a hit synchronously");
            settings.SetActive(true);yield return Tick(.25f);
            Assert.IsFalse(hero.ImpactReady);Assert.AreEqual("idle",enemy.View.Player.State,"Modal advanced impact");
            hero.Cancel();settings.SetActive(false);yield return Tick(.3f);
            Assert.AreEqual("idle",enemy.View.Player.State,"Cancelled hero attack hit later");
            game.StartCoroutine((IEnumerator)Call("EngageStepRoutine",new EngageStep{outcome=EngageOutcome.Attack,attackSize=AttackSize.Medium,dealt=35}));
            Call("ShowEnemy",machine.EnemyTables.First(e=>e.enemyType=="goblin"&&!e.IsBoss));yield return Tick(1);
            Assert.AreEqual("idle",enemy.View.Player.State,"Old attack hit replacement enemy");
            Assert.Greater(battleTimeline.Count,0,"Real BET presentation timeline was empty");
            File.WriteAllLines(Path.Combine(output,"battle-timeline.csv"),new[]{"scenario,unitySeconds,event,heroState,heroPose,enemyState,enemyPose,stance,heroHp,enemyHp,payout,enemyActive,captureFps"}.Concat(battleTimeline));
            File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"realBetStopEvaluate\":true,\"earlyHitSuppressed\":true,\"hitGuardDodgeCounterAttack\":true,\"explanationPausesResumesCancels\":true,\"storyPriorityPreserved\":true,\"heroContactBeforeEnemyHit\":true,\"lethalSwordCompleted\":true,\"cancelAndReplacementProtected\":true,\"runtimeSpearApproach\":true,\"runtimeSwordApproach\":true,\"spearContactAimErrorPx\":"+spearContactAimError.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)+",\"swordContactAimErrorPx\":"+swordContactAimError.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)+",\"spearReturnedToRest\":"+spearReturnedToRest.ToString().ToLowerInvariant()+",\"swordReturnedToRest\":"+swordReturnedToRest.ToString().ToLowerInvariant()+",\"runtimeAttackStepBeforeContact\":true,\"realHardwareInput\":false,\"finalArtApproved\":false}");
            yield return new ExitPlayMode();
        }
    }
}
#endif
