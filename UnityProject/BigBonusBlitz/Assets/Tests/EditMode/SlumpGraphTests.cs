using BBB.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace BBB.Tests
{
    /// <summary>スランプグラフの「潜行の始まり」の帳尻。間引かれても今の潜行ぶんが正しく切り出せること。</summary>
    public class SlumpGraphTests
    {
        private GameObject _root;

        [SetUp] public void Up() { _root = new GameObject("GraphTestRoot", typeof(RectTransform)); }
        [TearDown] public void Down() { if (_root != null) Object.DestroyImmediate(_root); }

        private SlumpGraph NewGraph(int baseCredit) => SlumpGraph.Create(_root.transform, Vector2.zero, new Vector2(130, 60), baseCredit, compact: true);

        [Test]
        public void 潜行の始まりを置くと_そこからの回転数と増減だけを数える()
        {
            var g = NewGraph(1000);
            int credit = 1000;
            for (int i = 0; i < 30; i++) { credit += 5; g.Push(credit); }   // 前の潜行: +150
            g.BeginRun();
            Assert.AreEqual(0, g.RunSpins);
            Assert.AreEqual(0, g.RunMaxDiff); Assert.AreEqual(0, g.RunMinDiff);
            int lo = 0, hi = 0, d = 0;
            for (int i = 0; i < 20; i++)
            {
                int step = (i % 3 == 0) ? 40 : -15;
                credit += step; d += step; g.Push(credit);
                if (d > hi) hi = d; if (d < lo) lo = d;
            }
            Assert.AreEqual(20, g.RunSpins);
            Assert.AreEqual(hi, g.RunMaxDiff, "今の潜行の最高");
            Assert.AreEqual(lo, g.RunMinDiff, "今の潜行の最低");
            var wave = g.RunSnapshot(120);
            Assert.AreEqual(0, wave[0], "波形は潜行の始まりを 0 にする");
            Assert.AreEqual(d, wave[wave.Length - 1], "波形の終わりは今の増減");
            Assert.AreEqual(150 + d, g.LastDiff, "全体の増減は前の潜行ぶんを含む");
        }

        [Test]
        public void 点が間引かれても_潜行の始まりがずれない()
        {
            var g = NewGraph(0);
            int credit = 0;
            for (int i = 0; i < 100; i++) { credit += 1; g.Push(credit); }   // 前の潜行: +100（100 点）
            g.BeginRun();
            // 小型版は 160 点で間引く。ここから 400G 回して 2 回以上間引かせる
            for (int i = 0; i < 400; i++) { credit += (i % 2 == 0) ? 3 : -1; g.Push(credit); }
            Assert.AreEqual(400, g.RunSpins);
            var wave = g.RunSnapshot(120);
            Assert.AreEqual(0, wave[0], "始まりは 0");
            Assert.AreEqual(credit - 100, wave[wave.Length - 1], "終わりは今の潜行の増減");
            Assert.AreEqual(credit - 100, g.RunMaxDiff, "右肩上がりなので最高は終わりの値");
            Assert.LessOrEqual(g.RunMinDiff, 0);
            Assert.GreaterOrEqual(g.RunMinDiff, -1, "1 回転で -1 までしか下がらない");
        }

        [Test]
        public void 取り直すと_潜行の始まりも先頭に戻る()
        {
            var g = NewGraph(500);
            for (int i = 0; i < 10; i++) g.Push(500 + i);
            g.BeginRun();
            for (int i = 0; i < 10; i++) g.Push(509 - i);
            g.ResetTo(777);
            Assert.AreEqual(0, g.RunSpins);
            Assert.AreEqual(0, g.LastDiff);
            g.Push(787);
            Assert.AreEqual(1, g.RunSpins);
            Assert.AreEqual(10, g.RunMaxDiff);
        }
    }
}
