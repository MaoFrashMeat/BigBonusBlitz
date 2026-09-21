using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;

namespace BBB.Tests
{
    /// <summary>序章（story.op）: 場面が揃っていて、飛ばす・続きの状態がセーブに乗る。</summary>
    public class OpeningTests
    {
        [Test]
        public void 序章の場面が揃っている()
        {
            var m = GameDataLoader.CreateMachine(new SystemRandom(1), 1);
            var op = m.Config.story.op;
            Assert.IsNotNull(op); Assert.IsTrue(op.enabled);
            foreach (var id in new[] { "escape", "collapse", "dark", "inn", "keeper", "shop", "gate", "gate_out" })
            {
                var sc = op.Find(id);
                Assert.IsNotNull(sc, id + " が無い");
                Assert.Greater(sc.lines.Count, 0, id + " に台詞が無い");
                Assert.That(sc.dim, Is.InRange(0f, 1f));
            }
            Assert.Greater(op.lifeSpins, 0);
            Assert.IsFalse(m.Adv.opDone, "新しいゲームは序章から");
            Assert.AreEqual("", m.Adv.opStep);
        }
    }
}
