using System.Collections.Generic;
using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;

namespace BBB.Tests
{
    /// <summary>役ごとの技術介入（本人 2026-09-24）: チェリー = 左リール、BAR か 7 を枠内か枠のすぐ上下に。</summary>
    public class TechRoleTests
    {
        [Test]
        public void 左リールのチェリーで成功になる止め位置()
        {
            var m = GameDataLoader.CreateMachine(new SystemRandom(1), 1);
            var left = m.Strips[0];
            // 左: 15 番のチェリーを上段 → BAR（17）が下段（枠内）。14 番を上段 → チェリー中段・BAR が枠のすぐ下
            var bar = TechDirector.FrameTops(left, Symbol.CHERRY, Symbol.BAR);
            CollectionAssert.AreEquivalent(new[] { 14, 15 }, bar);
            // 1 番のチェリーを上段 → 赤7（4）が下段のすぐ下
            CollectionAssert.AreEquivalent(new[] { 1 }, TechDirector.FrameTops(left, Symbol.CHERRY, Symbol.RED7));
            foreach (var top in bar)
            {
                bool cherry = false; for (int k = 0; k < 3; k++) if (left[(top + k) % left.Length] == Symbol.CHERRY) cherry = true;
                Assert.IsTrue(cherry, "成功の止め位置にチェリーが入っていない: " + top);
            }
        }

        [Test]
        public void 役ごとに決まったリールで出る_リプレイとハズレでは出ない()
        {
            var m = GameDataLoader.CreateMachine(new SystemRandom(2), 1);
            var cfg = m.Config.tech;
            Assert.IsTrue(cfg.roleRules.Count >= 3);
            foreach (var r in cfg.roleRules) r.rate = 100;
            var rng = new SystemRandom(3);
            var c = TechDirector.RollByRole(cfg, Flag.CHERRY_A, false, m.Strips, rng);
            Assert.AreEqual(TechKind.Frame, c.kind); Assert.AreEqual(0, c.reel, "チェリーは左"); Assert.AreEqual(Symbol.CHERRY, c.role);
            Assert.IsTrue(c.symbol == Symbol.BAR || c.symbol == Symbol.RED7 || c.symbol == Symbol.BLUE7);
            Assert.AreEqual(1, TechDirector.RollByRole(cfg, Flag.SUICA_A, false, m.Strips, rng).reel, "スイカは中");
            Assert.AreEqual(2, TechDirector.RollByRole(cfg, Flag.BELL_A, false, m.Strips, rng).reel, "ベルは右");
            Assert.IsFalse(TechDirector.RollByRole(cfg, Flag.REPLAY_A, false, m.Strips, rng).Active, "リプレイでは出ない");
            Assert.IsFalse(TechDirector.RollByRole(cfg, Flag.HAZE, false, m.Strips, rng).Active, "ハズレでは出ない");
            Assert.IsFalse(TechDirector.RollByRole(cfg, Flag.CHERRY_A, true, m.Strips, rng).Active, "オート中は出ない");
        }

        [Test]
        public void 狙いの位置で押せば成功しランクが付く()
        {
            var m = GameDataLoader.CreateMachine(new SystemRandom(4), 1);
            foreach (var r in m.Config.tech.roleRules) r.rate = 100;
            var c = TechDirector.RollByRole(m.Config.tech, Flag.CHERRY_A, false, m.Strips, new SystemRandom(5));
            int top = c.tops[0];
            var stopped = new[] { m.Strips[0][top], m.Strips[0][(top + 1) % 20], m.Strips[0][(top + 2) % 20] };
            Assert.IsTrue(TechDirector.Judge(c, stopped, top));
            Assert.IsFalse(TechDirector.Judge(c, stopped, (top + 7) % 20));
            Assert.IsNotNull(TechDirector.Rank(m.Config.tech, c, m.Strips[0], top, 0.5f), "ビタで押せばランクが付く");
        }
    }
}
