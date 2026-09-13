using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;

namespace BBB.Tests
{
    /// <summary>リール滑り制御が「当選役だけを揃え、非当選役は揃えない」ことを全押し位置で検証。</summary>
    public class SlipControllerTests
    {
        private GameConfig _cfg;
        private Symbol[][] _strips;

        [SetUp]
        public void SetUp()
        {
            _cfg = GameDataLoader.LoadGameConfig();
            _strips = _cfg.ReelSymbols();
        }

        [Test]
        public void リール配列は3本x20コマ()
        {
            Assert.AreEqual(3, _strips.Length);
            foreach (var s in _strips) Assert.AreEqual(20, s.Length);
        }

        private Symbol[][] StopAll(Flag flag, Flag held, int b0, int b1, int b2)
        {
            var stopped = new Symbol[3][];
            var r0 = SlipController.Stop(_strips, 0, b0, flag, held, stopped); stopped[0] = r0.symbols;
            var r1 = SlipController.Stop(_strips, 1, b1, flag, held, stopped); stopped[1] = r1.symbols;
            var r2 = SlipController.Stop(_strips, 2, b2, flag, held, stopped); stopped[2] = r2.symbols;
            return stopped;
        }

        [Test]
        public void ハズレ時はどこで押しても役が揃わない()
        {
            for (int a = 0; a < 20; a++)
                for (int b = 0; b < 20; b++)
                    for (int c = 0; c < 20; c++)
                    {
                        var st = StopAll(Flag.HAZE, Flag.HAZE, a, b, c);
                        var win = WinEvaluator.Evaluate(st, BonusMode.NORMAL, _cfg.payouts);
                        Assert.AreEqual(0, win.payout, $"push=({a},{b},{c})");
                        Assert.IsFalse(win.isReplay, $"push=({a},{b},{c})");
                        Assert.IsFalse(win.bonusWon, $"push=({a},{b},{c})");
                    }
        }

        [TestCase(Flag.BELL_A)]
        [TestCase(Flag.BELL_B)]
        [TestCase(Flag.BELL_C)]
        public void ベル当選時は順押しなら必ずSTAR払い出し(Flag flag)
        {
            for (int a = 0; a < 20; a++)
                for (int b = 0; b < 20; b++)
                    for (int c = 0; c < 20; c++)
                    {
                        var st = StopAll(flag, Flag.HAZE, a, b, c);
                        var win = WinEvaluator.Evaluate(st, BonusMode.NORMAL, _cfg.payouts);
                        Assert.AreEqual(_cfg.payouts.STAR, win.payout, $"{flag} push=({a},{b},{c})");
                    }
        }

        [TestCase(Flag.REPLAY_A)]
        [TestCase(Flag.REPLAY_B)]
        [TestCase(Flag.REPLAY_C)]
        public void リプレイ当選時は必ず再遊技(Flag flag)
        {
            for (int a = 0; a < 20; a++)
                for (int b = 0; b < 20; b++)
                    for (int c = 0; c < 20; c++)
                    {
                        var st = StopAll(flag, Flag.HAZE, a, b, c);
                        var win = WinEvaluator.Evaluate(st, BonusMode.NORMAL, _cfg.payouts);
                        Assert.IsTrue(win.isReplay, $"{flag} push=({a},{b},{c})");
                        Assert.AreEqual(0, win.payout);
                    }
        }

        [TestCase(Flag.CHERRY_A, true)]
        [TestCase(Flag.CHERRY_C, false)]
        public void チェリー当選時は左リールにチェリーが止まる(Flag flag, bool anyRow)
        {
            // チェリーは必須役ではないので、4 コマ滑らせても届かない押し位置では取りこぼす。
            // A（どの段でもよい）は届く位置で必ず止まる。C は段の縛りがあるので、届く位置の一部で止まる
            int len = _strips[0].Length, reachable = 0, hits = 0;
            for (int a = 0; a < len; a++)
            {
                bool canReach = false;
                for (int k = 0; k <= SlipController.DefaultMaxSlip && !canReach; k++)
                    foreach (var s in SlipController.Window(_strips[0], ((a - k) % len + len) % len))
                        if (s == Symbol.CHERRY) { canReach = true; break; }
                var stopped = new Symbol[3][];
                var r = SlipController.Stop(_strips, 0, a, flag, Flag.HAZE, stopped);
                bool hit = System.Array.IndexOf(r.symbols, Symbol.CHERRY) >= 0;
                if (hit) hits++;
                Assert.IsFalse(hit && !canReach, $"{flag} push={a}: 届かないはずの位置でチェリーが止まった（滑りが 4 コマを超えている）");
                if (!canReach) continue;
                reachable++;
                if (anyRow) Assert.IsTrue(hit, $"{flag} push={a}: 届く位置なのに止まらない");
            }
            Assert.Greater(reachable, len / 2, "チェリーが届く押し位置が少なすぎる");
            if (!anyRow) Assert.GreaterOrEqual(hits, reachable * 2 / 3, $"{flag}: 段の縛りを差し引いても止まる位置が少なすぎる（{hits}/{reachable}）");
        }

        [Test]
        public void 非当選役では役の払い出しが起きない_全フラグ総当たり()
        {
            var payouts = _cfg.payouts;
            foreach (Flag flag in System.Enum.GetValues(typeof(Flag)))
            {
                if (flag == Flag.HAZE) continue;
                for (int a = 0; a < 20; a += 3)
                    for (int b = 0; b < 20; b += 3)
                        for (int c = 0; c < 20; c += 3)
                        {
                            var st = StopAll(flag, Flag.HAZE, a, b, c);
                            var win = WinEvaluator.Evaluate(st, BonusMode.NORMAL, payouts);
                            string ctx = $"{flag} push=({a},{b},{c})";
                            if (!flag.IsBell()) Assert.AreNotEqual(WinType.BELL, win.winType, ctx);
                            if (!flag.IsSuica()) Assert.AreNotEqual(WinType.WATERMELON, win.winType, ctx);
                            if (!flag.IsReplay()) Assert.IsFalse(win.isReplay, ctx);
                            if (!flag.IsBonus()) Assert.IsFalse(win.bonusWon, ctx);
                            if (!flag.IsCherry()) Assert.AreNotEqual(WinType.CHERRY, win.winType, ctx);
                        }
            }
        }

        [Test]
        public void スベリは最大4コマ()
        {
            foreach (Flag flag in System.Enum.GetValues(typeof(Flag)))
                for (int a = 0; a < 20; a++)
                {
                    var r = SlipController.Stop(_strips, 0, a, flag, Flag.HAZE, new Symbol[3][]);
                    Assert.LessOrEqual(r.slip, 4);
                    Assert.GreaterOrEqual(r.slip, 0);
                }
        }
    }
}
