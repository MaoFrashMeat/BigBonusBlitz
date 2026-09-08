using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;

namespace BBB.Tests
{
    /// <summary>ゲーム本体を通しで回す結合テスト。</summary>
    public class SlotMachineTests
    {
        private static SlotMachine NewMachine(int seed, int setting = 1) =>
            GameDataLoader.CreateMachine(new SystemRandom(seed), setting);

        /// <summary>1G 回す（順押し・押し位置はランダム）。</summary>
        private static GameResult PlayOne(SlotMachine m, SystemRandom push)
        {
            Assert.IsTrue(m.MaxBet(), "BET失敗");
            m.Lever();
            for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
            return m.Evaluate();
        }

        [Test]
        public void 初期クレジット50_BETで47()
        {
            var m = NewMachine(1);
            Assert.AreEqual(50, m.Credit);
            Assert.IsTrue(m.MaxBet());
            Assert.AreEqual(47, m.Credit);
            Assert.AreEqual(3, m.Bet);
        }

        [Test]
        public void クレジット不足ならBETできない()
        {
            var m = NewMachine(1);
            m.Credit = 2;
            Assert.IsFalse(m.MaxBet());
        }

        [Test]
        public void リプレイ後はクレジットを減らさず回せる()
        {
            var m = NewMachine(1);
            m.IsReplay = true;
            m.Bet = 3;
            m.Credit = 10;
            Assert.IsTrue(m.MaxBet());
            // JS: replayCost(3) を引いた上で bet=3。クレジットは 10-3=7
            Assert.AreEqual(7, m.Credit);
            Assert.IsFalse(m.IsReplay);
        }

        [Test]
        public void 一万G回してもクレジットが破綻しない()
        {
            var m = NewMachine(42);
            var push = new SystemRandom(99);
            m.Credit = 100_000;
            int bonuses = 0;
            for (int g = 0; g < 10_000; g++)
            {
                var r = PlayOne(m, push);
                if (r.bonusStarted) bonuses++;
                Assert.GreaterOrEqual(m.Credit, 0);
            }
            Assert.Greater(bonuses, 0, "1万Gでボーナスが一度も揃わない");
        }

        [Test]
        public void ボーナス当選フラグは揃うまで持ち越す()
        {
            var m = NewMachine(5);
            m.Credit = 100_000;
            var push = new SystemRandom(1);
            // BB フラグを引くまで回す
            GameResult first = null;
            for (int g = 0; g < 50_000 && m.HeldBonusFlag == Flag.HAZE; g++) first = PlayOne(m, push);
            Assert.AreNotEqual(Flag.HAZE, m.HeldBonusFlag);
            var held = m.HeldBonusFlag;
            // 持ち越し中: 小役優先。フラグは持ち越しボーナスか小役のどちらか。持ち越しは消えない
            for (int g = 0; g < 20 && m.BonusMode == BonusMode.NORMAL; g++)
            {
                m.MaxBet(); m.Lever();
                Assert.IsTrue(m.CurrentFlag == held || !m.CurrentFlag.IsBonus(), $"G{g}: {m.CurrentFlag}");
                Assert.AreEqual(held, m.HeldBonusFlag);
                var flag = m.CurrentFlag;
                for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
                var res = m.Evaluate();
                // 小役を狙ったゲームでボーナスは揃わない
                if (flag != held) Assert.IsFalse(res.bonusStarted, $"G{g}: 小役優先なのにボーナス成立");
            }
        }

        [Test]
        public void BB中は目標枚数に達すると終了する()
        {
            var m = NewMachine(8);
            m.Credit = 100_000;
            var push = new SystemRandom(2);
            for (int g = 0; g < 100_000 && m.BonusMode == BonusMode.NORMAL; g++) PlayOne(m, push);
            Assert.AreNotEqual(BonusMode.NORMAL, m.BonusMode);
            int target = m.BonusPayoutTarget;
            Assert.Greater(target, 0);
            bool ended = false;
            for (int g = 0; g < 500 && !ended; g++) ended = PlayOne(m, push).bonusEnded;
            Assert.IsTrue(ended, "ボーナスが終わらない");
            Assert.AreEqual(BonusMode.NORMAL, m.BonusMode);
            Assert.AreEqual(0, m.SpinCount);
        }

        [Test]
        public void 敵出現の翌Gから3GでTier2が決着する()
        {
            var m = NewMachine(3);
            m.Credit = 1_000_000;
            var push = new SystemRandom(4);
            GameResult r = null;
            int started = -1;
            for (int g = 0; g < 200_000; g++)
            {
                r = PlayOne(m, push);
                if (r.precursorStarted) started = g;
                if (r.enemySpawned) { Assert.AreEqual(m.Config.enemyPrecursorSpins, g - started, "前兆G数"); break; }
            }
            Assert.IsTrue(r.enemySpawned, "敵が出現しない");
            Assert.IsNotNull(m.ActiveEnemyTable);
            Assert.IsTrue(m.PendingTier2);
            int resolvedAt = -1;
            for (int g = 1; g <= 10; g++)
            {
                r = PlayOne(m, push);
                if (m.BonusMode != BonusMode.NORMAL) Assert.Inconclusive("Tier2中にボーナス");
                if (r.enemyResolved.HasValue) { resolvedAt = g; break; }
            }
            Assert.AreEqual(m.Config.tier2MaxSpins, resolvedAt);
            Assert.IsFalse(m.IsTier2);
            Assert.IsFalse(m.EnemyActive);
        }

        [Test]
        public void チェリーは4コマ以内に無ければこぼす()
        {
            var m = NewMachine(2);
            var strip = m.Strips[0];
            // 左リールでチェリーが 4コマ以内に無い押し位置を探す
            int missIdx = -1;
            for (int b = 0; b < 20 && missIdx < 0; b++)
            {
                bool reachable = false;
                for (int k = 0; k <= 4 && !reachable; k++)
                {
                    int idx = ((b - k) % 20 + 20) % 20;
                    for (int r = 0; r < 3; r++) if (strip[(idx + r) % 20] == Symbol.CHERRY) reachable = true;
                }
                if (!reachable) missIdx = b;
            }
            Assert.GreaterOrEqual(missIdx, 0, "配列上チェリーがどこからでも届く");
            var stopped = new Symbol[3][];
            var res = SlipController.Stop(m.Strips, 0, missIdx, Flag.CHERRY_A, Flag.HAZE, stopped);
            CollectionAssert.DoesNotContain(res.symbols, Symbol.CHERRY);
        }

        [Test]
        public void 天井到達で強制ボーナス()
        {
            var m = NewMachine(11);
            m.Credit = 1_000_000;
            m.Mode = Mode.D; // 天井 100G
            m.SpinCount = m.Config.CeilingFor(Mode.D) - 1;
            m.HeldBonusFlag = Flag.HAZE;
            m.MaxBet(); m.Lever();
            Assert.IsTrue(m.CurrentFlag.IsBonus());
            Assert.IsTrue(m.HeldBonusFlag.IsBonus());
        }
    }
}
