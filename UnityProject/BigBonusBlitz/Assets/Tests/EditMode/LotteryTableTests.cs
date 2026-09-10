using System.Collections.Generic;
using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;

namespace BBB.Tests
{
    /// <summary>65536 分母の抽選テーブルが仕様書・settings.js と一致するか。</summary>
    public class LotteryTableTests
    {
        private GameConfig _cfg;

        [SetUp]
        public void SetUp() => _cfg = GameDataLoader.LoadGameConfig();

        private static IEnumerable<(string label, Dictionary<string, int> probs)> AllTables(GameConfig cfg)
        {
            foreach (var (name, set) in new[]
            {
                ("A", cfg.probabilities_A), ("B", cfg.probabilities_B), ("C", cfg.probabilities_C),
                ("D", cfg.probabilities_D), ("BB", cfg.probabilities_BB), ("RB", cfg.probabilities_RB)
            })
                foreach (var kv in set) yield return ($"{name}[{kv.Key}]", kv.Value);
            yield return ("Tier2", cfg.probabilities_Tier2);
        }

        [Test]
        public void 全テーブルの合計が65536()
        {
            foreach (var (label, probs) in AllTables(_cfg))
            {
                var t = new LotteryTable(probs);
                Assert.AreEqual(LotteryTable.Denominator, t.Length, label);
            }
        }

        [Test]
        public void テーブル内の各フラグ数が設定値と一致()
        {
            foreach (var (label, probs) in AllTables(_cfg))
            {
                var t = new LotteryTable(probs);
                foreach (var kv in probs)
                {
                    var f = (Flag)System.Enum.Parse(typeof(Flag), kv.Key);
                    Assert.AreEqual(kv.Value, t.Count(f), $"{label}.{kv.Key}");
                }
            }
        }

        [TestCase(1, 438, 200)]
        [TestCase(2, 468, 200)]
        [TestCase(6, 748, 300)]
        public void 仕様書の通常時モードA_ボーナス合算(int setting, int bb, int rb)
        {
            var t = new LotteryTable(_cfg.probabilities_A[setting.ToString()]);
            int bbCount = 0, rbCount = 0;
            for (int i = 0; i < t.Length; i++)
            {
                if (t[i].IsBB()) bbCount++;
                if (t[i].IsRB()) rbCount++;
            }
            Assert.AreEqual(bb, bbCount, "BB合算");
            Assert.AreEqual(rb, rbCount, "RB合算");
        }

        [Test]
        public void Tier2のSTAR合算はデータどおり()
        {
            var t = new LotteryTable(_cfg.probabilities_Tier2);
            int bell = 0;
            for (int i = 0; i < t.Length; i++) if (t[i].IsBell()) bell++;
            // 2026-09-10: 実装データ（game_config.json）を正とする。仕様書の 40000 は古い
            Assert.AreEqual(30000, bell);
        }

        [Test]
        public void モンテカルロ_設定1のBB出現率が理論値に収束()
        {
            var t = new LotteryTable(_cfg.probabilities_A["1"]);
            var rng = new SystemRandom(12345);
            const int n = 2_000_000;
            int bb = 0;
            for (int i = 0; i < n; i++) if (t.Draw(rng).IsBB()) bb++;
            double expected = 438.0 / 65536;
            double actual = (double)bb / n;
            // 3σ 以内
            double sigma = System.Math.Sqrt(expected * (1 - expected) / n);
            Assert.Less(System.Math.Abs(actual - expected), 3 * sigma, $"expected={expected} actual={actual}");
        }
    }
}
