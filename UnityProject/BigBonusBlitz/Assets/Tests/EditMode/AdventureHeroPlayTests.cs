#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.Collections;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace BBB.Tests
{
    public sealed class AdventureHeroPlayTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static GameController game;
        static int contact;
        static void Set(string name,object value)=>typeof(GameController).GetField(name,Private).SetValue(game,value);
        static T Get<T>(string name)=>(T)typeof(GameController).GetField(name,Private).GetValue(game);
        static object Call(string name,params object[] args)=>typeof(GameController).GetMethod(name,Private).Invoke(game,args);
        static void OnCue(string cue){if(cue=="contact")contact++;}
        [UnityTest]
        public IEnumerator RealReelsPreserveHeldPhasesEquipmentAndSingleContact()
        {
            if(Application.productName!="shop-qa"||!Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"))
                Assert.Ignore("Requires isolated QA project and preferences");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            yield return new EnterPlayMode();
            var audio=AudioManager.Create();audio.SeVolume=0;
            var machine=GameDataLoader.CreateMachine(new SystemRandom(928));machine.Credit=3000;
            game=new GameObject("AdventureHeroPlayQA").AddComponent<GameController>();game.enabled=false;
            Set("_m",machine);Set("_audio",audio);Call("BuildUi");Call("RefreshUi");
            var hero=Get<AdventureHeroPresenter>("_adventureHero");Assert.IsNotNull(hero);
            var stop=typeof(GameController).GetMethod("StopReel",Private,null,new[]{typeof(int)},null);
            contact=0;hero.View.Player.Cue+=OnCue;
            foreach(string weapon in new[]{"w_blade","w_spear"})
            {
                machine.Equip.Worn[EquipSlot.Weapon]=new EquipItem{baseId=weapon,slot=EquipSlot.Weapon,name="QA weapon",crafted=true};
                Call("RefreshUi");Assert.AreEqual(weapon,hero.View.Player.WeaponId);
                int before=contact;machine.DebugForceFlag=Flag.BELL_A;Set("_lastSpinStartTime",-100f);
                Call("OnBetClicked");Set("_stopUnlockTime",0f);
                Assert.IsTrue(machine.IsGameActive);Assert.AreEqual("attack-f1",hero.View.Player.State);
                hero.Step(30);Assert.AreEqual("attack-f1",hero.View.Player.State);Assert.AreEqual(before,contact);
                int first=weapon=="w_blade"?2:1;int second=weapon=="w_blade"?0:2;int third=weapon=="w_blade"?1:0;
                stop.Invoke(game,new object[]{first});hero.Step(30);Assert.AreEqual("attack-f2",hero.View.Player.State);
                stop.Invoke(game,new object[]{second});Assert.AreEqual("attack-f3-4",hero.View.Player.State);
                hero.Step(1);Assert.AreEqual(before+1,contact);
                stop.Invoke(game,new object[]{second});hero.Step(.01f);Assert.AreEqual(before+1,contact,"Duplicate stop attacked again");
                stop.Invoke(game,new object[]{third});
                float deadline=Time.realtimeSinceStartup+15;
                while(machine.IsGameActive&&Time.realtimeSinceStartup<deadline){hero.Step(Time.deltaTime);yield return null;}
                Assert.IsFalse(machine.IsGameActive,"Real reels never resolved");hero.Step(2);
                Assert.AreEqual(before+1,contact,"Third stop duplicated contact");
                Assert.AreNotEqual("attack-f1",hero.View.Player.State);Assert.AreNotEqual("attack-f2",hero.View.Player.State);
            }
            SaveData.Save(machine,audio);
            var loaded=GameDataLoader.CreateMachine(new SystemRandom(929));Assert.IsTrue(SaveData.Load(loaded,audio));
            Set("_m",loaded);Call("RefreshUi");Assert.AreEqual("w_spear",hero.View.Player.WeaponId,"Reload lost weapon look");
            loaded.Equip.Worn.Clear();Call("RefreshUi");Assert.AreEqual("w_blade",hero.View.Player.WeaponId,"Unequip lost fallback");
            // A weapon swap while walking must not pop the feet back to the first cell.
            hero.Cancel();Call("PlayCharacter","walk");hero.Step(.19f);
            float walkPhase=hero.View.Player.NormalizedTime;Assert.Greater(walkPhase,.05f);
            loaded.Equip.Worn[EquipSlot.Weapon]=new EquipItem{baseId="w_spear",slot=EquipSlot.Weapon,name="QA spear",crafted=true};
            Call("RefreshUi");Assert.AreEqual("walk",hero.View.Player.State);
            Assert.That(hero.View.Player.NormalizedTime,Is.EqualTo(walkPhase).Within(.06f),"Weapon swap restarted walk phase");
            Call("PlayCharacter","attack");loaded.Equip.Worn[EquipSlot.Weapon]=new EquipItem{baseId="w_blade",slot=EquipSlot.Weapon,name="QA blade",crafted=true};
            Call("RefreshUi");Assert.AreEqual("w_spear",hero.View.Player.WeaponId,"Attack changed weapon before recovery");
            Assert.AreEqual("w_blade",hero.View.Player.PendingWeapon,"Attack did not queue weapon change");
            hero.Step(1f);Assert.AreEqual("w_blade",hero.View.Player.WeaponId,"Queued weapon was not applied after recovery");
            hero.Cancel();hero.Request("attack-f3-4",false);
            int attackVersion=hero.MotionVersion;int attackContacts=contact;
            hero.Request("walk",false);
            Assert.AreEqual(attackVersion,hero.MotionVersion,"Queued walk invalidated contact waiter");
            Assert.AreEqual("attack-f3-4",hero.View.Player.State);
            hero.Step(.08f);Assert.IsTrue(hero.ImpactReady);Assert.AreEqual(attackContacts+1,contact);
            hero.Request("idle",false);
            Assert.AreEqual(attackVersion,hero.MotionVersion,"Post-contact loop request invalidated waiter");
            Assert.IsTrue(hero.ImpactReady,"Post-contact loop request erased impact");
            hero.Step(1);Assert.AreEqual("idle",hero.View.Player.State);
            hero.Request("hit",false);Assert.Greater(hero.MotionVersion,attackVersion);
            Assert.IsFalse(hero.ImpactReady,"New action retained previous contact");
            hero.Cancel();Call("PlayCharacter","attack-f1");Call("PlayCharacter","walk");Assert.AreEqual("walk",hero.View.Player.State,"Non-reel cut-in held forever");
            int pose=hero.View.Player.PoseIndex;
            var settings=Get<GameObject>("_settingsBox");settings.SetActive(true);
            float until=Time.realtimeSinceStartup+.25f;
            while(Time.realtimeSinceStartup<until){Call("Update");yield return null;}
            Assert.AreEqual(pose,hero.View.Player.PoseIndex,"Modal failed to pause hero");settings.SetActive(false);
            bool advanced=false;until=Time.realtimeSinceStartup+.4f;
            while(Time.realtimeSinceStartup<until){Call("Update");advanced|=hero.View.Player.PoseIndex!=pose;yield return null;}
            Assert.IsTrue(advanced,"GameController.Update did not resume hero clock");
            UnityEngine.Object.Destroy(game.gameObject);
            yield return new ExitPlayMode();
        }
    }
}
#endif
