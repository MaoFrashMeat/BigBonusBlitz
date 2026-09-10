using System;
using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>
    /// 呪いと祝福。段を進むときに「受けるか断るか」を選ばせる。
    /// 受けると呪い 1 つと祝福 1 つが同時に付き、深いほど両方が強くなる。
    /// </summary>
    [Serializable]
    public sealed class CurseDef
    {
        public string id = "";
        public string name = "";
        public string desc = "";
        /// <summary>効果キー。ShopEffects と同じものに加えて、下の CurseEffects も使える。</summary>
        public string effect = "";
        /// <summary>深さ 1 あたりの値（呪いは負の意味を持つが、値は正で持つ）。</summary>
        public float perLevel = 1f;
        public int min = 1;
        public int max = 99;
        public int weight = 100;
        /// <summary>この深さから出る。</summary>
        public int minDepth = 1;
    }

    /// <summary>呪い側だけが使う効果キー。</summary>
    public static class CurseEffects
    {
        /// <summary>ライフの消費を増やす %（100 で 2 倍の速さで減る）。</summary>
        public const string TorchDrain = "torchDrain";
        /// <summary>ルート条件の必要回数に足す。</summary>
        public const string ConditionHarder = "conditionHarder";
        /// <summary>払い出しを減らす %。</summary>
        public const string PayoutCut = "payoutCut";
        /// <summary>1G ごとのエンバー消費に足す。</summary>
        public const string BetExtra = "betExtra";
    }

    [Serializable]
    public sealed class CurseConfig
    {
        public bool enabled = true;
        /// <summary>段を進んだときに、呪いの選択を出す率 %。</summary>
        public int offerRate = 35;
        /// <summary>この深さから出しはじめる。</summary>
        public int minDepth = 2;
        /// <summary>同時に抱えられる呪いの数。</summary>
        public int maxStack = 4;
        public List<CurseDef> curses = new List<CurseDef>();
        public List<CurseDef> blessings = new List<CurseDef>();
    }

    /// <summary>いま受けている 1 組。</summary>
    [Serializable]
    public sealed class CurseInstance
    {
        public string curseId = "";
        public string curseName = "";
        public string curseEffect = "";
        public int curseValue;
        public string blessId = "";
        public string blessName = "";
        public string blessEffect = "";
        public int blessValue;
        public int depth = 1;
    }

    /// <summary>受けている呪いの束。ランごとに作り直す。</summary>
    public sealed class CurseState
    {
        public readonly List<CurseInstance> Taken = new List<CurseInstance>();
        /// <summary>いま提示している選択（受けるか断るか）。null なら提示なし。</summary>
        public CurseInstance Offer;

        public void Clear() { Taken.Clear(); Offer = null; }

        /// <summary>呪い側の合計。</summary>
        public int CurseTotal(string effect)
        {
            int n = 0;
            foreach (var c in Taken) if (c != null && c.curseEffect == effect) n += c.curseValue;
            return n;
        }

        /// <summary>祝福側の合計。</summary>
        public int BlessTotal(string effect)
        {
            int n = 0;
            foreach (var c in Taken) if (c != null && c.blessEffect == effect) n += c.blessValue;
            return n;
        }
    }

    public static class CurseDirector
    {
        /// <summary>その深さで 1 組を作る。設定が空なら null。</summary>
        public static CurseInstance Roll(CurseConfig cfg, int depth, IRandom rng)
        {
            if (cfg == null || !cfg.enabled) return null;
            if (cfg.curses == null || cfg.curses.Count == 0) return null;
            if (cfg.blessings == null || cfg.blessings.Count == 0) return null;
            depth = Math.Max(1, depth);

            var c = Pick(cfg.curses, depth, rng);
            var b = Pick(cfg.blessings, depth, rng);
            if (c == null || b == null) return null;
            return new CurseInstance
            {
                curseId = c.id, curseName = c.name, curseEffect = c.effect, curseValue = Value(c, depth),
                blessId = b.id, blessName = b.name, blessEffect = b.effect, blessValue = Value(b, depth),
                depth = depth,
            };
        }

        /// <summary>段に着いたときに提示するか。</summary>
        public static bool ShouldOffer(CurseConfig cfg, CurseState st, int depth, IRandom rng)
        {
            if (cfg == null || !cfg.enabled || st == null) return false;
            if (depth < Math.Max(1, cfg.minDepth)) return false;
            if (st.Taken.Count >= Math.Max(1, cfg.maxStack)) return false;
            if (st.Offer != null) return false;
            return rng.NextDouble() * 100 < Math.Max(0, cfg.offerRate);
        }

        private static CurseDef Pick(List<CurseDef> list, int depth, IRandom rng)
        {
            var pool = new List<CurseDef>();
            foreach (var x in list) if (x != null && x.minDepth <= depth) pool.Add(x);
            if (pool.Count == 0) pool.AddRange(list);
            int total = 0;
            foreach (var x in pool) total += Math.Max(0, x.weight);
            if (total <= 0) return pool[0];
            int r = rng.Next(total);
            foreach (var x in pool)
            {
                int w = Math.Max(0, x.weight);
                if (r < w) return x;
                r -= w;
            }
            return pool[pool.Count - 1];
        }

        private static int Value(CurseDef d, int depth)
            => Math.Max(d.min, Math.Min(d.max <= 0 ? 9999 : d.max, (int)Math.Round(d.perLevel * depth)));

        /// <summary>説明文（「{v}」を値に置き換える）。</summary>
        public static string Describe(string desc, int value)
            => string.IsNullOrEmpty(desc) ? "" : desc.Replace("{v}", value.ToString());
    }
}
