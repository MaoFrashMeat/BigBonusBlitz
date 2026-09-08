using System;
using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>
    /// main.js ensureLotteryTables.generateTable 相当。
    /// 分母 65536 の配列。JS はシャッフルするが、一様乱数で添字を引く限り分布は同じなので
    /// 検証しやすいよう FLAGS 順に並べたまま持つ。
    /// </summary>
    public sealed class LotteryTable
    {
        public const int Denominator = 65536;
        private readonly Flag[] _table;
        public int Length => _table.Length;
        public Flag this[int i] => _table[i];

        public LotteryTable(Dictionary<string, int> probabilities)
        {
            if (probabilities == null) throw new ArgumentNullException(nameof(probabilities));
            var list = new List<Flag>(Denominator);
            for (int i = 1; i < FlagExt.Count; i++)
            {
                var f = (Flag)i;
                if (probabilities.TryGetValue(f.ToString(), out var n) && n > 0)
                    for (int k = 0; k < n; k++) list.Add(f);
            }
            int haze = probabilities.TryGetValue("HAZE", out var h) ? h : Denominator - list.Count;
            for (int k = 0; k < haze; k++) list.Add(Flag.HAZE);
            _table = list.ToArray();
        }

        public bool IsValid => _table.Length == Denominator;

        public Flag Draw(IRandom rng) => _table[rng.Next(Denominator)];

        public int Count(Flag f)
        {
            int c = 0;
            foreach (var x in _table) if (x == f) c++;
            return c;
        }
    }
}
