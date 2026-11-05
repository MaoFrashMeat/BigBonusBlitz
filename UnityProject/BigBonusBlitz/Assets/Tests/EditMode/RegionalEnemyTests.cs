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
