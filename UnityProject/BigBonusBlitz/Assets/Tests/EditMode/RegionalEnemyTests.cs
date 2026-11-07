using System.Collections.Generic;
using System.Linq;
using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;

namespace BBB.Tests
{
    public sealed class RegionalEnemyTests
    {
        [Test]
        public void RepeatedStrikeFiresContactAgainButHeldPoseKeepsItsPhase()
        {
            var player=new EnemyMotionPlayer(EnemyPresentation.Find("slime"));int contacts=0;player.Cue+=c=>{if(c=="contact")contacts++;};
            var strike=System.Array.Find(EnemyPresentation.Find("slime").motions,m=>m.id=="strike");
            int contactFrame=System.Array.IndexOf(strike.events,"contact");float pastContact=strike.seconds.Take(contactFrame+1).Sum()-.001f;
            player.Request("strike");player.Tick(pastContact);
            Assert.AreEqual(1,contacts);Assert.AreEqual("strike",player.State,"Fixture must re-request before the strike ends");
            player.Request("strike");player.Tick(pastContact);
            Assert.AreEqual(2,contacts,"A second strike would never report its impact and its result would be skipped");
            player.Request("ready");player.Tick(10);int pose=player.PoseIndex;float progress=player.Progress;
            player.Request("ready");
            Assert.AreEqual(pose,player.PoseIndex);Assert.AreEqual(progress,player.Progress,"A held pose restarted");
        }
        [Test]
        public void ReleasedEnemyAtlasIsLoadedAgainOnDemand()
        {
            var pose=EnemyPresentation.Find("slime").poses[0];string atlas=EnemyPresentation.Find("slime").atlas;
            var first=HeroPresentation.Sprite(atlas,pose.x,pose.y,pose.w,pose.h);
            Assert.AreSame(first,HeroPresentation.Sprite(atlas,pose.x,pose.y,pose.w,pose.h));
            HeroPresentation.ReleaseAtlas(atlas);
            Assert.IsTrue(first!=null,"Release must not destroy a sprite that may still be shown");
            Assert.AreNotSame(first,HeroPresentation.Sprite(atlas,pose.x,pose.y,pose.w,pose.h));
        }
        [Test]
        public void MissingEnemyArtStaysInvisibleInsteadOfAWhiteRectangle()
        {
            var host=new UnityEngine.GameObject("EnemyHost",typeof(UnityEngine.RectTransform),typeof(UnityEngine.UI.Image));
            try
            {
                var image=host.GetComponent<UnityEngine.UI.Image>();
                var presenter=AdventureEnemyPresenter.Attach((UnityEngine.RectTransform)host.transform,-50);
                presenter.Show("unknown-unmapped-type",1,400);Assert.IsFalse(image.enabled);
                image.sprite=ArtLoader.EnemySprite("slime");presenter.Hide();Assert.IsTrue(image.enabled);
            }
            finally{UnityEngine.Object.DestroyImmediate(host);}
        }
        [Test]
        public void FinishedStageChangeLetsGoOfTheHiddenTextures()
        {
            var host=new UnityEngine.GameObject("Area",typeof(UnityEngine.RectTransform));
            try
            {
                var bg=ParallaxBackground.Create((UnityEngine.RectTransform)host.transform);
                bg.SetStage("A-1",true);bg.SetStage("B-1",true);
                var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                var banks=(UnityEngine.UI.RawImage[][])typeof(ParallaxBackground).GetField("banks",flags).GetValue(bg);
                int active=(int)typeof(ParallaxBackground).GetField("activeBank",flags).GetValue(bg);
                Assert.IsTrue(banks[1-active][0].texture!=null,"Fixture must start with a retained stage");
                typeof(ParallaxBackground).GetMethod("Update",flags).Invoke(bg,null);
                Assert.IsTrue(banks[1-active][0].texture==null);Assert.IsTrue(banks[active][0].texture!=null,"Visible stage was released");
                Assert.IsTrue(bg.HasIllustration);
            }
            finally{UnityEngine.Object.DestroyImmediate(host);}
        }
        [Test]
        public void CurseHudFollowsChangesAndKeepsTheOverflowButtonInsideItsPanel()
        {
            var machine=GameDataLoader.CreateMachine(new SystemRandom(7));
            var host=new UnityEngine.GameObject("Stage",typeof(UnityEngine.RectTransform));
            try
            {
                var stage=(UnityEngine.RectTransform)host.transform;stage.sizeDelta=new UnityEngine.Vector2(1170,540);
                var hud=CurseHudPresenter.Create(stage,stage,machine,UnityEngine.Vector2.zero);
                for(int i=0;i<6;i++)machine.Curse.Taken.Add(new CurseInstance{curseId="qa"+i,curseName="QA"+i,curseEffect=CurseEffects.TorchDrain,curseValue=1});
                hud.Refresh(false);Assert.AreEqual(6,hud.GroupCount);
                float LeftOf(string name){var r=(UnityEngine.RectTransform)hud.transform.Find(name);return r.anchoredPosition.x-r.sizeDelta.x*.5f;}
                float RightOf(string name){var r=(UnityEngine.RectTransform)hud.transform.Find(name);return r.anchoredPosition.x+r.sizeDelta.x*.5f;}
                Assert.GreaterOrEqual(LeftOf("AllCurses"),RightOf("Curse_qa3"),"Overflow button overlaps the last curse");
                Assert.LessOrEqual(RightOf("AllCurses"),RightOf("Panel"),"Overflow button leaves the panel");
                machine.Curse.Taken[0].curseValue=9;machine.Curse.Taken[0].curseId="qa-changed";hud.Refresh(false);
                Assert.IsTrue(hud.transform.Find("Curse_qa-changed")!=null,"A changed curse was not rebuilt");
                UnityEngine.Object.DestroyImmediate(hud.gameObject);
            }
            finally{UnityEngine.Object.DestroyImmediate(host);}
        }
        [Test]
        public void AllTwentyProductionIdentitiesHaveMotionArtAndReachableEncounters()
        {
            var tables=GameDataLoader.LoadEnemyTables();Assert.AreEqual(20,tables.Count);
            Assert.AreEqual(20,tables.Select(t=>t.enemyType).Distinct().Count());
            foreach(var table in tables)
            {
                Assert.Contains(table.enemyType,EnemyPresentation.Config.gameplayTypes);
                var look=EnemyPresentation.Find(table.enemyType);Assert.AreEqual(8,look.poses.Length);
                Assert.IsNotNull(ArtLoader.EnemySprite(table.enemyType),table.id);
                var found=false;var rng=new SystemRandom(114);
                var stage=table.stageIds?.FirstOrDefault();
                for(int i=0;i<200;i++)found|=EnemyEngage.SelectTable(tables,table.variant,rng,table.IsBoss,stage)==table;
                Assert.IsTrue(found,"Unreachable: "+table.id);
            }
            Assert.IsNull(ArtLoader.EnemySprite("unknown-unmapped-type"));
        }
        sealed class Pick : IRandom
        {
            readonly int value;
            public Pick(int value){this.value=value;}
            public int Next(int max){Assert.Less(value,max);return value;}
            public double NextDouble(){return .5;}
        }
        static EnemyTable Table(string id,string variant="A",string[] stages=null,int weight=1,string group="normal")
            =>new EnemyTable{id=id,variant=variant,stageIds=stages,selectionWeight=weight,group=group};
        [Test]
        public void RegionOverridesOnlyMatchingVariantAndFallsBackOutsideRegion()
        {
            var legacy=Table("legacy");var wolf=Table("wolf",stages:new[]{"C-1"});
            var tables=new List<EnemyTable>{legacy,wolf,Table("other","B",new[]{"D-1"})};
            Assert.AreSame(wolf,EnemyEngage.SelectTable(tables,"A",new Pick(0),false,"C-1"));
            Assert.AreSame(legacy,EnemyEngage.SelectTable(tables,"A",new Pick(0),false,"D-1"));
            Assert.AreSame(legacy,EnemyEngage.SelectTable(tables,"A",new Pick(0)));
            Assert.AreSame(legacy,EnemyEngage.SelectTable(tables,"A",new Pick(0),false,"UNKNOWN"));
        }
        [Test]
        public void WeightsRespectBoundariesAndDisabledEntriesCannotWin()
        {
            var a=Table("a",stages:new[]{"C-1"},weight:2);var b=Table("b",stages:new[]{"C-1"},weight:3);
            var tables=new List<EnemyTable>{Table("disabled",stages:new[]{"C-1"},weight:0),a,b};
            Assert.AreSame(a,EnemyEngage.SelectTable(tables,"A",new Pick(1),false,"C-1"));
            Assert.AreSame(b,EnemyEngage.SelectTable(tables,"A",new Pick(2),false,"C-1"));
            Assert.AreSame(b,EnemyEngage.SelectTable(tables,"A",new Pick(4),false,"C-1"));
            Assert.IsNull(EnemyEngage.SelectTable(tables,"A",new Pick(0),false,"A-1"));
        }
        [Test]
        public void BossFilteringAndLegacyFallbackRemainAvailable()
        {
            var normal=Table("normal");var boss=Table("boss",group:"boss");
            var local=Table("localboss",stages:new[]{"E-4"},group:"boss");
            var tables=new List<EnemyTable>{normal,boss,local};
            Assert.AreSame(local,EnemyEngage.SelectTable(tables,"A",new Pick(0),true,"E-4"));
            Assert.AreSame(boss,EnemyEngage.SelectTable(tables,"A",new Pick(0),true,"A-1"));
            Assert.AreSame(normal,EnemyEngage.SelectTable(tables,"A",new Pick(0),false,"E-4"));
            Assert.AreSame(normal,EnemyEngage.SelectTable(new[]{normal},"A",new Pick(0),true));
        }
        [Test]
        public void LegacyUniformSelectionRetainsSeedSequence()
        {
            var tables=new[]{Table("a"),Table("b"),Table("c")};
            var expected=new SystemRandom(983);var actual=new SystemRandom(983);
            for(int i=0;i<100;i++)Assert.AreSame(tables[expected.Next(3)],EnemyEngage.SelectTable(tables,"A",actual));
        }
    }
}
