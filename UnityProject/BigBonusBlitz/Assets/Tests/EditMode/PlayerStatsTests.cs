using System.Collections.Generic;
using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;

namespace BBB.Tests
{
    public class PlayerStatsTests
    {
        private static SlotMachine NewMachine(int seed) => GameDataLoader.CreateMachine(new SystemRandom(seed), 1);

        [Test]
        public void 設定ファイルのステータスが読み込める()
        {
            var m = NewMachine(1);
            var cfg = m.Config.stats;
            Assert.IsNotNull(cfg, "stats が無い");
            Assert.IsTrue(cfg.enabled);
            Assert.Greater(cfg.pointsPerLevel, 0);
            Assert.Greater(cfg.maxPerStat, 0);
            Assert.AreEqual(0, m.Stats.Total, "はじめは 0");
            Assert.AreEqual(0, m.Stats.Unspent);
        }

        [Test]
        public void 上限まで振れて上限を超えられない()
        {
            var m = NewMachine(2);
            var cfg = m.Config.stats;
            m.Stats.Unspent = cfg.maxPerStat + 5;
            for (int i = 0; i < cfg.maxPerStat; i++)
                Assert.IsTrue(StatsDirector.Spend(cfg, m.Stats, "life"), $"{i + 1} 回目で振れなくなった");
            Assert.AreEqual(cfg.maxPerStat, m.Stats.Life);
            Assert.IsFalse(StatsDirector.Spend(cfg, m.Stats, "life"), "上限を超えて振れた");
            Assert.AreEqual(5, m.Stats.Unspent, "上限で弾いたのにポイントが減った");
        }

        [Test]
        public void 振り直すとポイントが全部戻る()
        {
            var m = NewMachine(3);
            var cfg = m.Config.stats;
            m.Stats.Unspent = 9;
            for (int i = 0; i < 3; i++) StatsDirector.Spend(cfg, m.Stats, "life");
            for (int i = 0; i < 3; i++) StatsDirector.Spend(cfg, m.Stats, "technique");
            for (int i = 0; i < 3; i++) StatsDirector.Spend(cfg, m.Stats, "luck");
            Assert.AreEqual(0, m.Stats.Unspent);
            Assert.AreEqual(9, StatsDirector.Respec(m.Stats));
            Assert.AreEqual(9, m.Stats.Unspent);
            Assert.AreEqual(0, m.Stats.Total);
        }

        [Test]
        public void ポイントが無ければ振れない()
        {
            var m = NewMachine(4);
            m.Stats.Unspent = 0;
            Assert.IsFalse(StatsDirector.Spend(m.Config.stats, m.Stats, "luck"));
            Assert.AreEqual(0, m.Stats.Luck);
        }

        [Test]
        public void ライフは松明の持ちを伸ばす()
        {
            var m = NewMachine(5);
            int baseSpins = m.TorchSpinsPerUnit;
            m.Stats.Life = m.Config.stats.maxPerStat;
            int grown = m.TorchSpinsPerUnit;
            Assert.Greater(grown, baseSpins, "ライフを振っても松明が伸びない");
            Assert.AreEqual(baseSpins + (int)(m.Stats.Life * m.Config.stats.life.torchSpins), grown);
        }

        [Test]
        public void テクニックはエンゲージのG数を伸ばす()
        {
            var m = NewMachine(6);
            int baseSpins = m.EngageMaxSpins;
            m.Stats.Technique = m.Config.stats.maxPerStat;
            Assert.GreaterOrEqual(m.EngageMaxSpins, baseSpins, "エンゲージのG数が減った");
            Assert.AreEqual(baseSpins + (int)(m.Stats.Technique * m.Config.stats.technique.engageSpins), m.EngageMaxSpins);
        }

        [Test]
        public void ライフを振るとBETが無料になることがある()
        {
            var m = NewMachine(7);
            m.Stats.Life = m.Config.stats.maxPerStat;
            m.Credit = 1_000_000;
            var push = new SystemRandom(3);
            int free = 0, paid = 0;
            for (int g = 0; g < 4000; g++)
            {
                bool wasReplay = m.IsReplay;
                int before = m.Credit;
                Assert.IsTrue(m.MaxBet());
                int used = before - m.Credit;
                if (!wasReplay) { if (used == 0) free++; else { paid++; Assert.AreEqual(SlotMachine.BetCost, used); } }
                m.Lever();
                for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
                m.Evaluate();
            }
            Assert.Greater(free, 0, "一度も無料にならない");
            Assert.Greater(paid, free, "無料の方が多いのはおかしい");
        }

        [Test]
        public void ラックを振るとハズレが減る()
        {
            int CountHaze(int luck)
            {
                var m = NewMachine(11);
                m.Stats.Luck = luck;
                m.Credit = 10_000_000;
                var push = new SystemRandom(5);
                int haze = 0;
                for (int g = 0; g < 20000; g++)
                {
                    // 通常時だけを数える（ボーナス中・持ち越し中はラックの対象外）
                    bool normal = m.BonusMode == BonusMode.NORMAL && m.HeldBonusFlag == Flag.HAZE && !m.InAt;
                    Assert.IsTrue(m.MaxBet());
                    m.Lever();
                    for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
                    m.Evaluate();
                    if (normal && m.CurrentFlag == Flag.HAZE) haze++;
                }
                return haze;
            }
            int none = CountHaze(0);
            int full = CountHaze(20);
            Assert.Less(full, none, "ラックを振ってもハズレが減らない");
        }

        [Test]
        public void レベルアップでポイントが配られる()
        {
            var m = NewMachine(13);
            int per = m.Config.stats.pointsPerLevel;
            int lv = m.PlayerLevel;
            m.Credit = 5_000_000;
            var push = new SystemRandom(9);
            for (int g = 0; g < 60000 && m.PlayerLevel == lv; g++)
            {
                Assert.IsTrue(m.MaxBet());
                m.Lever();
                for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
                m.Evaluate();
            }
            Assert.Greater(m.PlayerLevel, lv, "レベルが上がらない");
            Assert.GreaterOrEqual(m.Stats.Unspent, per, "レベルが上がったのにポイントが増えていない");
        }
    }
}
