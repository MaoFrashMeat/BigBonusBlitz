#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Collections;
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
    public sealed class AdventureEnemyPlayTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static GameController game;
        static string output;
        static Camera camera;
        static bool sawHeroHit;
        static T Get<T>(string name)=>(T)typeof(GameController).GetField(name,Private).GetValue(game);
        static void Set(string name,object value)=>typeof(GameController).GetField(name,Private).SetValue(game,value);
        static object Call(string name,params object[] args)=>typeof(GameController).GetMethod(name,Private).Invoke(game,args);
        static IEnumerator Tick(float seconds)
        {
            float until=Time.time+seconds;
            while(Time.time<until){sawHeroHit|=Get<AdventureHeroPresenter>("_adventureHero").View.Player.State=="hit";Call("Update");sawHeroHit|=Get<AdventureHeroPresenter>("_adventureHero").View.Player.State=="hit";yield return null;}
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
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(2556,1179)})
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
        [UnityTest, Timeout(180000)]
        public IEnumerator SevenWeaponFamiliesDriveMainBattleAndCancelRangedHits()
        {
            if(Application.productName!="shop-qa"||!Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"))Assert.Ignore("Isolated QA required");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            yield return new EnterPlayMode();
            output=Path.Combine(Environment.GetEnvironmentVariable("ENEMY_GAMEPLAY_OUTPUT"),"weapons");Directory.CreateDirectory(output);
            foreach(var title in UnityEngine.Object.FindObjectsByType<TitleScreen>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(title.gameObject);
            foreach(var cv in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(cv.gameObject);
            yield return null;
            var audio=AudioManager.Create();audio.SeVolume=0;audio.BgmVolume=0;
            var machine=GameDataLoader.CreateMachine(new SystemRandom(930));machine.Credit=3000;machine.Adv.opDone=true;
            game=new GameObject("WeaponGameplayQA").AddComponent<GameController>();game.enabled=false;
            Set("_m",machine);Set("_audio",audio);Call("BuildUi");Call("RefreshUi");Call("PlayCharacter","idle");
            Call("ShowEnemy",GameDataLoader.LoadEnemyTables().First(t=>t.enemyType=="goblin"));yield return Tick(4);
            var hero=Get<AdventureHeroPresenter>("_adventureHero");var enemy=Get<AdventureEnemyPresenter>("_adventureEnemy");
            Assert.AreEqual(7,HeroPresentation.Config.weapons.Length);
            foreach(var look in HeroPresentation.Config.weapons)
            {
                hero.Cancel();enemy.Request("idle");machine.Equip.Worn[EquipSlot.Weapon]=new EquipItem{baseId=look.id,slot=EquipSlot.Weapon};
                yield return Tick(.05f);Assert.AreEqual(look.id,hero.View.Player.WeaponId);
                Call("PlayCharacter","attack-f2");yield return Tick(.05f);Capture(look.id+"-windup");
                int credit=machine.Credit;bool sawFlight=false,sawHit=false,captured=false;
                game.StartCoroutine((IEnumerator)Call("EngageStepRoutine",new EngageStep{outcome=EngageOutcome.Attack,attackSize=AttackSize.Medium,dealt=3,hpLeft=9,hpMax=12}));
                float until=Time.time+2;
                while(Time.time<until)
                {
                    Call("Update");
                    if(hero.IsProjectileFlying){sawFlight=true;Assert.IsFalse(hero.ImpactReady);Assert.AreNotEqual("hit",enemy.View.Player.State);}
                    if(enemy.View.Player.State=="hit"){sawHit=true;if(!captured){Capture(look.id+"-impact");captured=true;}}
                    yield return null;
                }
                Assert.IsTrue(sawHit,"Main battle missed hit "+look.id);Assert.AreEqual(credit,machine.Credit);
                if(look.ranged)
                {
                    Assert.IsTrue(sawFlight);Assert.Less(Mathf.Abs(hero.Advance),.001f);
                    hero.Cancel();enemy.Request("idle");
                    game.StartCoroutine((IEnumerator)Call("EngageStepRoutine",new EngageStep{outcome=EngageOutcome.Attack,dealt=3}));
                    Call("Update");yield return null;Assert.IsTrue(hero.IsProjectileFlying);
                    var modal=new GameObject("ProjectilePauseQA");Set("_settingsBox",modal);
                    var projectile=hero.transform.Find("HeroProjectile");Vector3 frozen=projectile.position;
                    yield return Tick(.4f);Assert.AreEqual(frozen,projectile.position);Assert.IsFalse(hero.ImpactReady);
                    Set("_settingsBox",null);UnityEngine.Object.Destroy(modal);
                    Call("HideEnemy");yield return Tick(.5f);Assert.IsFalse(hero.IsProjectileFlying);Assert.IsFalse(enemy.Active);
                    Call("ShowEnemy",GameDataLoader.LoadEnemyTables().First(t=>t.enemyType=="goblin"));yield return Tick(4);
                }
            }
            yield return new ExitPlayMode();
            File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"families\":7,\"resolutions\":2,\"mainBattleHit\":true,\"modalPausesFlight\":true,\"hideCancelsFlight\":true,\"productionSaveTouched\":false,\"finalArtApproved\":false}");
        }
        [UnityTest, Timeout(360000)]
        public IEnumerator MainEnemyPresentationKeepsResultsAndFallbacks()
        {
            if(Application.productName!="shop-qa"||!Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"))Assert.Ignore("Isolated QA required");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            yield return new EnterPlayMode();
            output=Environment.GetEnvironmentVariable("ENEMY_GAMEPLAY_OUTPUT");Assert.IsNotEmpty(output);Directory.CreateDirectory(output);
            foreach(var title in UnityEngine.Object.FindObjectsByType<TitleScreen>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(title.gameObject);
            foreach(var cv in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(cv.gameObject);
            yield return null;
            var audio=AudioManager.Create();audio.SeVolume=0;audio.BgmVolume=0;
            var machine=GameDataLoader.CreateMachine(new SystemRandom(928));machine.Credit=3000;machine.Adv.opDone=true;
            game=new GameObject("EnemyGameplayQA").AddComponent<GameController>();game.enabled=false;
            Set("_m",machine);Set("_audio",audio);Call("BuildUi");Call("RefreshUi");Call("PlayCharacter","idle");
            var supportedTables=GameDataLoader.LoadEnemyTables();Assert.AreEqual(20,supportedTables.Count);
            foreach(var table in supportedTables)
            {
                string type=table.enemyType;
                Call("ShowEnemy",table);
                yield return Tick(4f);
                var enemy=Get<AdventureEnemyPresenter>("_adventureEnemy");Assert.IsTrue(enemy.Active);Assert.IsFalse(Get<Image>("_enemyImg").enabled);
                Assert.IsNull(Get<Coroutine>("_enemyIdle"));Assert.AreEqual(Vector3.one,Get<RectTransform>("_enemyRt").localScale);
                Assert.AreEqual(1,Get<CanvasGroup>("_enemyCg").alpha);Capture(type+"-idle");
                game.StartCoroutine((IEnumerator)Call("EngageStepRoutine",new EngageStep{outcome=EngageOutcome.Stance,stance=EngageStance.EnemyAttack}));
                yield return Tick(.7f);Assert.AreEqual("ready",enemy.View.Player.State);Capture(type+"-ready");
                int credit=machine.Credit,sound=audio.SoundSequence;
                var strike=Array.Find(enemy.View.Look.motions,m=>m.id=="strike");
                enemy.Request("strike");enemy.Step(strike.seconds[0]*strike.eventAt[0]+.001f);Assert.IsTrue(enemy.ImpactReady);Capture(type+"-contact");
                Assert.AreEqual(credit,machine.Credit);Assert.AreEqual(sound,audio.SoundSequence,"Presentation emitted duplicate sound");
                float gap=enemy.View.Body.transform.position.x-Get<RectTransform>("_charRt").position.x;
                Assert.That(gap,Is.InRange(35f,150f),"Strike misses hero plane");
                enemy.Request("hit");enemy.Step(.08f);Capture(type+"-hit");yield return Tick(.8f);Assert.AreEqual("idle",enemy.View.Player.State);
                sawHeroHit=false;game.StartCoroutine((IEnumerator)Call("EngageStepRoutine",new EngageStep{outcome=EngageOutcome.Hit,damage=1}));
                Assert.AreEqual("strike",enemy.View.Player.State);yield return Tick(.35f);
                Assert.IsTrue(sawHeroHit,"Contact did not drive existing hero reaction");
                yield return Tick(1);
                game.StartCoroutine((IEnumerator)Call("DefeatRoutine"));enemy.Step(.25f);Capture(type+"-defeat");yield return Tick(.9f);
                Assert.IsFalse(enemy.Active);Assert.AreEqual(0,Get<CanvasGroup>("_enemyCg").alpha);
            }
            Call("ShowEnemy",new EnemyTable{enemyType="goblin",name="QA boss",group="boss",scale=1.35f,color="#ff8888"});yield return Tick(4f);
            Assert.AreEqual(Get<Image>("_enemyImg").color,Get<AdventureEnemyPresenter>("_adventureEnemy").View.Body.color);Capture("boss-goblin");
            Call("SetEnemyColor","rainbow");yield return Tick(.1f);Assert.AreEqual(Get<Image>("_enemyImg").color,Get<AdventureEnemyPresenter>("_adventureEnemy").View.Body.color);
            var enabledTypes=EnemyPresentation.Config.gameplayTypes;
            EnemyPresentation.Config.gameplayTypes=new[]{"slime","goblin"};
            Call("ShowEnemy",new EnemyTable{enemyType="bat",name="QA bat",scale=1});yield return Tick(4f);
            Assert.IsFalse(Get<AdventureEnemyPresenter>("_adventureEnemy").Active);Assert.IsTrue(Get<Image>("_enemyImg").enabled);Capture("legacy-bat");
            EnemyPresentation.Config.gameplayTypes=enabledTypes;
            game.StartCoroutine((IEnumerator)Call("BattleStartRoutine",new MonsterDef{enemyType="slime",name="QA AT slime"}));yield return Tick(1.2f);
            Assert.IsTrue(Get<AdventureEnemyPresenter>("_adventureEnemy").Active);Assert.AreEqual(Color.white,Get<Image>("_enemyImg").color);Assert.IsFalse(Get<bool>("_engagedBoss"));Assert.IsTrue(Get<MarqueeBand>("_engageBandTop")==null,"AT inherited normal battle banners");Assert.AreEqual(new Vector2(150,150),Get<RectTransform>("_enemyRt").sizeDelta);Capture("at-slime");
            game.StartCoroutine((IEnumerator)Call("BattleHitRoutine",new GameResult{battleDamage=3}));Assert.AreEqual("hit",Get<AdventureEnemyPresenter>("_adventureEnemy").View.Player.State);yield return Tick(.5f);
            var adapter=Get<AdventureEnemyPresenter>("_adventureEnemy");adapter.Request("ready");
            var modal=new GameObject("PauseQA");Set("_settingsBox",modal);float progress=adapter.View.Player.Progress;yield return Tick(.3f);
            Assert.AreEqual(progress,adapter.View.Player.Progress,"Modal did not pause enemy");Set("_settingsBox",null);UnityEngine.Object.Destroy(modal);yield return Tick(.4f);
            Assert.AreEqual(1,adapter.View.Player.Progress,"Enemy did not resume ready hold");
            game.StartCoroutine((IEnumerator)Call("BattleStartRoutine",new MonsterDef{enemyType="slime",name="stale AT"}));yield return Tick(.4f);
            Call("ShowEnemy",new EnemyTable{enemyType="goblin",name="replacement",scale=1});yield return Tick(3);
            Assert.AreEqual("goblin",adapter.View.Look.id);Assert.AreEqual(1,Get<CanvasGroup>("_enemyCg").alpha);
            sawHeroHit=false;game.StartCoroutine((IEnumerator)Call("EngageStepRoutine",new EngageStep{outcome=EngageOutcome.Hit,damage=1}));Call("HideEnemy");yield return Tick(.4f);
            Assert.IsFalse(sawHeroHit,"Hidden enemy delivered a delayed hit reaction");
            Call("ShowEnemy",new EnemyTable{enemyType="goblin",name="cancel",scale=1});Call("HideEnemy");yield return Tick(4f);
            Assert.IsFalse(Get<AdventureEnemyPresenter>("_adventureEnemy").Active);Assert.AreEqual(0,Get<CanvasGroup>("_enemyCg").alpha,"Old spawn revived hidden enemy");
            EnemyPresentation.Config.gameplayEnabled=false;
            Call("ShowEnemy",new EnemyTable{enemyType="slime",name="fallback",scale=1});Assert.IsFalse(Get<AdventureEnemyPresenter>("_adventureEnemy").Active);Assert.IsTrue(Get<Image>("_enemyImg").enabled);
            Call("HideEnemy");EnemyPresentation.Config.gameplayEnabled=true;
            yield return new ExitPlayMode();
            File.WriteAllText(Path.Combine(output,"validation.json"),"{\"passed\":true,\"stills\":206,\"supportedTypes\":20,\"disabledBatFallback\":true,\"atEntry\":true,\"impactDoesNotChangeCoreOrSound\":true,\"cancelSpawn\":true,\"finalArtApproved\":false}");
        }
    }
}
#endif
