#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Tests
{
    public sealed class HeroContactPlantTests
    {
        static void RequireIsolation()
        {
            if(Application.productName!="shop-qa"||!Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"))
                Assert.Ignore("Requires isolated QA project");
        }

        [TestCase("w_blade")]
        [TestCase("w_spear")]
        [TestCase("w_dagger")]
        [TestCase("w_holy")]
        [TestCase("w_hammer")]
        public void EnemyRecoilDoesNotDragHeroAfterContact(string weapon)
        {
            RequireIsolation();
            var stage=new GameObject("ContactPlantTest",typeof(RectTransform));
            try
            {
                var host=UiSkin.Rect(stage.transform,"Hero",new Vector2(-300,0),new Vector2(200,240));host.gameObject.AddComponent<Image>();
                var hero=AdventureHeroPresenter.Attach(host);
                var target=UiSkin.Rect(stage.transform,"Target",new Vector2(150,-70),new Vector2(100,80));
                foreach(string motion in new[]{"attack","attack-f3-4"})
                foreach(int fps in new[]{15,60,120})foreach(float displacement in new[]{-48f,48f})
                {
                    hero.Cancel();hero.Equip(new EquipItem{baseId=weapon,slot=EquipSlot.Weapon});
                    target.anchoredPosition=new Vector2(150,-70);
                    int contacts=0,observations=0;bool displaced=false;Vector2 planted=Vector2.zero;
                    System.Action<string> cue=s=>{
                        if(s!="contact")return;
                        contacts++;
                        // The public event must already expose the committed contact pose and position.
                        Assert.Less(Mathf.Abs(hero.View.WeaponTipWorld(hero.View.Player.PoseIndex,weapon).x-target.position.x),.01f);
                        planted=hero.View.Root.anchoredPosition;
                        target.anchoredPosition+=Vector2.right*displacement;
                        displaced=true;
                    };
                    hero.View.Player.Cue+=cue;
                    hero.PrepareAttackTarget(target);hero.Request(motion,false);hero.Request("walk",false);
                    for(int frame=0;frame<fps*3;frame++)
                    {
                        hero.Step(1f/fps);
                        if(displaced&&hero.View.Player.State==motion)
                        {
                            observations++;
                            Assert.Less(Vector2.Distance(planted,hero.View.Root.anchoredPosition),.01f,$"{weapon}/{motion}/{fps}: recoil dragged hero");
                        }
                    }
                    Assert.Greater(observations,0,"No follow-through was observed");
                    Assert.AreEqual(1,contacts);Assert.Less(Mathf.Abs(hero.Advance),.01f);Assert.IsFalse(hero.View.LowTarget);
                    hero.View.Player.Cue-=cue;
                    // A new stroke and cancellation must release the previous plant.
                    hero.PrepareAttackTarget(target);hero.Request(motion,false);hero.Step(.05f);hero.Cancel();
                    Assert.Less(Mathf.Abs(hero.Advance),.01f);Assert.IsFalse(hero.ImpactReady);Assert.IsFalse(hero.View.LowTarget);
                }
            }
            finally {Object.DestroyImmediate(stage);}
        }

        [TestCase("w_dagger")]
        [TestCase("w_holy")]
        [TestCase("w_hammer")]
        public void MeleeVariantsUseLowPosesOnlyForShortTargets(string weapon)
        {
            RequireIsolation();
            var stage=new GameObject("LowBladeTest",typeof(RectTransform));
            try
            {
                var host=UiSkin.Rect(stage.transform,"Hero",new Vector2(-300,0),new Vector2(200,240));host.gameObject.AddComponent<Image>();
                var hero=AdventureHeroPresenter.Attach(host);var target=UiSkin.Rect(stage.transform,"Target",Vector2.zero,new Vector2(100,80));
                foreach(bool low in new[]{true,false})
                {
                    hero.Cancel();hero.Equip(new EquipItem{baseId=weapon,slot=EquipSlot.Weapon});
                    target.anchoredPosition=new Vector2(150,low?-70:100);
                    hero.PrepareAttackTarget(target);hero.Request("attack",false);
                    for(int frame=0;frame<240&&!hero.ImpactReady;frame++)hero.Step(1f/120);
                    Assert.IsTrue(hero.ImpactReady);Assert.AreEqual(low,hero.View.LowTarget);
                    var pose=HeroPresentation.Config.poses[hero.View.RenderedPoseIndex];
                    Assert.AreEqual(low,(pose.atlas=="hero-low-contact-v1"||pose.atlas=="hammer-low"));
                }
            }
            finally {Object.DestroyImmediate(stage);}
        }
    }
}
#endif
