using BBB.Core;
using NUnit.Framework;

namespace BBB.Tests
{
    /// <summary>技術介入の押した精度のランク（2コマ / 1コマ / ビタ 5 段）と上乗せ。</summary>
    public class TechRankTests
    {
        private static Symbol[] StripWithBarAt(int idx)
        {
            var s = new Symbol[20];
            for (int i = 0; i < 20; i++) s[i] = Symbol.STAR;
            s[idx] = Symbol.BAR;
            return s;
        }

        private static TechChallenge Vita(int reel = 0) => new TechChallenge { kind = TechKind.Vita, id = "hard_bar", reel = reel, symbol = Symbol.BAR, row = 1 };

        [Test]
        public void ビタは押した位相でPerfectからCoolまで5段()
        {
            var cfg = TechConfig.Default();
            var strip = StripWithBarAt(5);   // 中段に BAR が来る上段コマは 4
            Assert.AreEqual("perfect", TechDirector.Rank(cfg, Vita(), strip, 4, 0.5f).id);
            Assert.AreEqual("perfect", TechDirector.Rank(cfg, Vita(), strip, 4, 0.41f).id);
            Assert.AreEqual("excellent", TechDirector.Rank(cfg, Vita(), strip, 4, 0.35f).id);
            Assert.AreEqual("great", TechDirector.Rank(cfg, Vita(), strip, 4, 0.25f).id);
            Assert.AreEqual("good", TechDirector.Rank(cfg, Vita(), strip, 4, 0.85f).id);
            Assert.AreEqual("cool", TechDirector.Rank(cfg, Vita(), strip, 4, 0.02f).id);
            Assert.AreEqual("cool", TechDirector.Rank(cfg, Vita(), strip, 4, 0.99f).id);
        }

        [Test]
        public void 手前で押した分だけ1コマ2コマになり_3コマ以上はランク無し()
        {
            var cfg = TechConfig.Default();
            var strip = StripWithBarAt(5);
            Assert.AreEqual("koma1", TechDirector.Rank(cfg, Vita(), strip, 5, 0.5f).id);   // 1 コマ手前（滑って中段に入る）
            Assert.AreEqual("koma2", TechDirector.Rank(cfg, Vita(), strip, 6, 0.5f).id);
            Assert.IsNull(TechDirector.Rank(cfg, Vita(), strip, 7, 0.5f));
            Assert.IsNull(TechDirector.Rank(cfg, Vita(), strip, 3, 0.5f));    // 通り過ぎた後は次の周まで無い
            Assert.IsNull(TechDirector.Rank(cfg, Vita(), strip, -1, 0.5f));   // 押していない
            Assert.IsNull(TechDirector.Rank(cfg, default, strip, 4, 0.5f));   // 課題が無い
        }

        [Test]
        public void 同じ図柄が複数あれば一番近い狙いで数える()
        {
            var cfg = TechConfig.Default();
            var strip = StripWithBarAt(5); strip[12] = Symbol.BAR;   // 狙いは 4 と 11
            Assert.AreEqual("perfect", TechDirector.Rank(cfg, Vita(), strip, 11, 0.5f).id);
            Assert.AreEqual("koma2", TechDirector.Rank(cfg, Vita(), strip, 13, 0.5f).id);
        }

        [Test]
        public void 表が空ならランク無し()
        {
            var cfg = TechConfig.Default(); cfg.ranks.Clear();
            Assert.IsNull(TechDirector.Rank(cfg, Vita(), StripWithBarAt(5), 4, 0.5f));
        }

        [Test]
        public void 既定の表は上から狭い順で_上乗せは最高100パーセント()
        {
            var ranks = TechConfig.DefaultRanks();
            Assert.AreEqual(7, ranks.Count);
            Assert.AreEqual(100, ranks[0].bonusPercent);
            for (int i = 1; i < 5; i++) Assert.Less(ranks[i].bonusPercent, ranks[i - 1].bonusPercent);
            for (int i = 1; i < 4; i++) Assert.Greater(ranks[i].phase, ranks[i - 1].phase);
        }
    }
}
