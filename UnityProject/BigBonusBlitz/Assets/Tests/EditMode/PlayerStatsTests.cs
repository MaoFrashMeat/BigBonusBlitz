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
        public void ライフは回復薬の効きを伸ばす()
        {
            var m = NewMachine(5);
            int baseSpins = m.TorchSpinsPerUnit;
            m.Stats.Life = m.Config.stats.maxPerStat;
            int grown = m.TorchSpinsPerUnit;
            Assert.Greater(grown, baseSpins, "ライフを振っても回復薬の効きが伸びない");
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

        [Test]
        public void 装備の枠は8つ_アクセは3つ着けられて4つ目は一番弱いものと入れ替わる()
        {
            var m = NewMachine(5);
            Assert.AreEqual(8, EquipSlot.All.Length);
            EquipItem Acc(int power) => new EquipItem { baseId = "t", name = "アクセ" + power, slot = EquipSlot.Accessory,
                effectKeys = new List<string> { ShopEffects.StatLuck }, effectValues = new List<int> { power } };
            var a1 = Acc(1); var a2 = Acc(2); var a3 = Acc(3); var a4 = Acc(4);
            foreach (var a in new[] { a1, a2, a3, a4 }) m.Equip.Bag.Add(a);
            EquipDirector.Equip(m.Equip, a1); EquipDirector.Equip(m.Equip, a2); EquipDirector.Equip(m.Equip, a3);
            Assert.IsTrue(m.Equip.IsWorn(a1) && m.Equip.IsWorn(a2) && m.Equip.IsWorn(a3), "アクセが 3 つ着けられない");
            Assert.AreEqual(6, m.LuckStat, "装備のラックが合算されていない");
            EquipDirector.Equip(m.Equip, a4);
            Assert.IsTrue(m.Equip.IsWorn(a4), "4 つ目が着けられない");
            Assert.IsFalse(m.Equip.IsWorn(a1), "一番弱いものが外れていない");
            Assert.IsTrue(m.Equip.Bag.Contains(a1), "外れた品が鞄に戻っていない");
            Assert.AreEqual(9, m.LuckStat);
        }

        [Test]
        public void 装備の種類ごとに品があり_昔の部位名も読み替えられる()
        {
            var m = NewMachine(6);
            var bases = m.Config.equipment.bases;
            foreach (var kind in EquipSlot.Kinds)
                Assert.IsTrue(bases.Exists(b => EquipSlot.Normalize(b.slot) == kind), kind + " の品が無い");
            Assert.AreEqual(EquipSlot.Body, EquipSlot.Normalize("armor"));
            Assert.AreEqual(EquipSlot.Accessory, EquipSlot.KindOf(EquipSlot.Normalize("trinket")));
            // 何度か作っても部位は 6 種類のどれかになる
            var rng = new SystemRandom(9);
            for (int i = 0; i < 200; i++)
            {
                var it = EquipDirector.Roll(m.Config.equipment, 1 + i % 8, rng);
                Assert.IsNotNull(it);
                Assert.Contains(it.slot, EquipSlot.Kinds, "知らない部位: " + it.slot);
            }
        }
    }
}
