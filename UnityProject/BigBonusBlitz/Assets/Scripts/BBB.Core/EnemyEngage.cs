using System;
using System.Collections.Generic;

namespace BBB.Core
{
    public enum HintKind { None, RedGlow, TextOnly, Both }

    /// <summary>エネミーエンゲージの抽選部分（テーブル選択・討伐・示唆）。</summary>
    public static class EnemyEngage
    {
        /// <summary>evaluateWin の ENEMY 当選時テーブル選択。</summary>
        public static EnemyTable SelectTable(IList<EnemyTable> tables, string variant, IRandom rng)
        {
            if (tables == null || tables.Count == 0) return null;
            var v = variant ?? "ANY";
            var match = Filter(tables, t => t.variant == v);
            if (match.Count == 0) match = Filter(tables, t => string.IsNullOrEmpty(t.variant) || t.variant == "ANY");
            if (match.Count == 0) match = new List<EnemyTable>(tables);
            return match[rng.Next(match.Count)];
        }

        private static List<EnemyTable> Filter(IList<EnemyTable> src, Func<EnemyTable, bool> pred)
        {
            var r = new List<EnemyTable>();
            foreach (var t in src) if (pred(t)) r.Add(t);
            return r;
        }

        /// <summary>checkEnemyDefeat: 役ごとの討伐率（%）で内部当選。</summary>
        public static bool RollDefeat(EnemyTable table, WinType winType, IRandom rng, float multiplier = 1f)
        {
            if (table == null) return false;
            string key = winType.ToString();
            if (!table.defeatProbabilities.TryGetValue(key, out var prob) || prob <= 0) return false;
            double p = System.Math.Min(100.0, prob * multiplier);
            return rng.NextDouble() * 100 < p;
        }

        /// <summary>onLever の示唆演出抽選。</summary>
        public static HintKind RollHint(EnemyTable table, HintConfig fallback, IRandom rng)
        {
            var conf = table?.hintConfig ?? fallback ?? new HintConfig();
            var dist = conf.distribution ?? new HintDistribution();
            if (rng.NextDouble() * 100 >= conf.appearanceRate) return HintKind.None;
            int total = dist.redGlow + dist.textOnly + dist.both;
            if (total <= 0) return HintKind.None;
            double roll = rng.NextDouble() * total;
            if (roll < dist.redGlow) return HintKind.RedGlow;
            if (roll < dist.redGlow + dist.textOnly) return HintKind.TextOnly;
            return HintKind.Both;
        }
    }
}
