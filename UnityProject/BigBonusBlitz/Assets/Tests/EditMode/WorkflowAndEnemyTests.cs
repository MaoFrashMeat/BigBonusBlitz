using System.Collections.Generic;
using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;

namespace BBB.Tests
{
    public class WorkflowAndEnemyTests
    {
        private Dictionary<string, WorkflowRole> _wf;
        private List<EnemyTable> _enemies;

        [SetUp]
        public void SetUp()
        {
            _wf = GameDataLoader.LoadWorkflow();
            _enemies = GameDataLoader.LoadEnemyTables();
        }

        [Test]
        public void 各役のNONEとカテゴリrateの合計が100()
        {
            foreach (var kv in _wf)
            {
                var r = kv.Value;
                int sum = r.NONE;
                foreach (var c in WorkflowLottery.Categories) sum += r.Get(c)?.rate ?? 0;
                Assert.AreEqual(100, sum, kv.Key);
            }
        }

        [Test]
        public void rateが0でないカテゴリのバリアント合計は100か0()
        {
            foreach (var kv in _wf)
                foreach (var c in WorkflowLottery.Categories)
                {
                    var cat = kv.Value.Get(c);
                    if (cat == null || cat.rate == 0) continue;
                    int sum = 0;
                    foreach (var v in WorkflowLottery.Variants) sum += cat.Variant(v);
                    Assert.IsTrue(sum == 100 || sum == 0, $"{kv.Key}.{c} variants sum={sum}");
                }
        }

        [Test]
        public void REPLAYはENEMY当選率50pct()
        {
            var rng = new SystemRandom(7);
            const int n = 200_000;
            int enemy = 0;
            for (int i = 0; i < n; i++)
                if (WorkflowLottery.Run(_wf, "REPLAY", rng).category == "ENEMY") enemy++;
            Assert.AreEqual(0.50, (double)enemy / n, 0.01);
        }

        [Test]
        public void 敵テーブルは3種_スライムはVariantA()
        {
            Assert.AreEqual(3, _enemies.Count);
            var slime = _enemies.Find(t => t.enemyType == "slime");
            Assert.IsNotNull(slime);
            Assert.AreEqual("A", slime.variant);
            Assert.AreEqual(5, slime.defeatProbabilities["BELL"]);
        }

        [Test]
        public void 敵Tier2テーブルの合計は65536()
        {
            foreach (var t in _enemies)
                Assert.IsTrue(new LotteryTable(t.tier2Probabilities).IsValid, t.id);
        }

        [Test]
        public void VariantAならスライム_未指定ならANYかランダム()
        {
            var rng = new SystemRandom(1);
            Assert.AreEqual("slime", EnemyEngage.SelectTable(_enemies, "A", rng).enemyType);
            // Variant B のテーブルは無い → variant 未設定のテーブル（goblin/bat）から選ばれる
            for (int i = 0; i < 50; i++)
            {
                var t = EnemyEngage.SelectTable(_enemies, "B", rng);
                Assert.AreNotEqual("slime", t.enemyType);
            }
        }

        [Test]
        public void 討伐率_CHANCEは100pct_REPLAYは1pct()
        {
            var slime = _enemies.Find(t => t.enemyType == "slime");
            var rng = new SystemRandom(3);
            for (int i = 0; i < 100; i++) Assert.IsTrue(EnemyEngage.RollDefeat(slime, WinType.CHANCE, rng));
            int hit = 0;
            for (int i = 0; i < 100_000; i++) if (EnemyEngage.RollDefeat(slime, WinType.REPLAY, rng)) hit++;
            Assert.AreEqual(0.01, hit / 100_000.0, 0.002);
        }
    }
}
