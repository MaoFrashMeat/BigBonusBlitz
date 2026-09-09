using System;
using System.Collections.Generic;

namespace BBB.Core
{
    public enum HintKind { None, RedGlow, TextOnly, Both }

    /// <summary>エネミーエンゲージの抽選部分（テーブル選択・討伐・示唆）。</summary>
    public static class EnemyEngage
    {
        /// <summary>
        /// evaluateWin の ENEMY 当選時テーブル選択。
        /// boss=true ならボーナス中の中ボスから選ぶ（居なければ通常の敵にフォールバック）。
        /// </summary>
        public static EnemyTable SelectTable(IList<EnemyTable> tables, string variant, IRandom rng, bool boss = false)
        {
            if (tables == null || tables.Count == 0) return null;
            var pool = Filter(tables, t => t.IsBoss == boss);
            if (pool.Count == 0) pool = new List<EnemyTable>(tables);
            var v = variant ?? "ANY";
            var match = Filter(pool, t => t.variant == v);
            if (match.Count == 0) match = Filter(pool, t => string.IsNullOrEmpty(t.variant) || t.variant == "ANY");
            if (match.Count == 0) match = pool;
            return match[rng.Next(match.Count)];
        }

        private static List<EnemyTable> Filter(IList<EnemyTable> src, Func<EnemyTable, bool> pred)
        {
            var r = new List<EnemyTable>();
            foreach (var t in src) if (pred(t)) r.Add(t);
            return r;
        }

        /// <summary>checkEnemyDefeat: 役ごとの討伐率（%）で内部当選。multiplier は倍率、streak は直前までの小役連続回数（1回ごとに streakBonus % 加算）。</summary>
        public static bool RollDefeat(EnemyTable table, WinType winType, IRandom rng, float multiplier = 1f, int streak = 0, int streakBonus = 0, int skillBonus = 0)
        {
            if (table == null) return false;
            string key = winType.ToString();
            if (!table.defeatProbabilities.TryGetValue(key, out var prob) || prob <= 0) return false;
            double p = System.Math.Min(100.0, (prob + Math.Max(0, streak) * Math.Max(0, streakBonus) + Math.Max(0, skillBonus)) * multiplier);
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
