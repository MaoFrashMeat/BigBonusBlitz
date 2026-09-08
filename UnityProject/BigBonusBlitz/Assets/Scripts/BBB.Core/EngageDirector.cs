using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>
    /// 敵エンゲージ中の段階示唆（案B: 潜伏当否型）。
    ///   1G目: 敵の色（none/blue/yellow/green/red/rainbow）
    ///   2G目: セリフ（none/weak/mid/strong）
    ///   3G目: 停止音（normal/hot）
    /// 各段階は「内部当選している(won)/していない(lost)」で別テーブル。重みは game_config.json の engageHints。
    /// </summary>
    public sealed class EngageHintConfig
    {
        public Dictionary<string, Dictionary<string, int>> stage1Color = new Dictionary<string, Dictionary<string, int>>
        {
            ["won"] = new Dictionary<string, int> { ["none"] = 20, ["blue"] = 10, ["yellow"] = 20, ["green"] = 15, ["red"] = 25, ["rainbow"] = 10 },
            ["lost"] = new Dictionary<string, int> { ["none"] = 60, ["blue"] = 25, ["yellow"] = 10, ["green"] = 4, ["red"] = 1, ["rainbow"] = 0 },
        };
        public Dictionary<string, Dictionary<string, int>> stage2Serif = new Dictionary<string, Dictionary<string, int>>
        {
            ["won"] = new Dictionary<string, int> { ["none"] = 30, ["weak"] = 20, ["mid"] = 25, ["strong"] = 25 },
            ["lost"] = new Dictionary<string, int> { ["none"] = 70, ["weak"] = 25, ["mid"] = 5, ["strong"] = 0 },
        };
        public Dictionary<string, Dictionary<string, int>> stage3Sound = new Dictionary<string, Dictionary<string, int>>
        {
            ["won"] = new Dictionary<string, int> { ["normal"] = 30, ["hot"] = 70 },
            ["lost"] = new Dictionary<string, int> { ["normal"] = 95, ["hot"] = 5 },
        };
    }

    public struct EngageHint
    {
        public int stage;          // 1..3
        public string color;       // stage1
        public string serif;       // stage2
        public bool hotSound;      // stage3
    }

    public static class EngageDirector
    {
        public static readonly string[] Colors = { "none", "blue", "yellow", "green", "red", "rainbow" };
        public static readonly string[] Serifs = { "none", "weak", "mid", "strong" };

        /// <summary>そのGの示唆を抽選する。stage は Tier2SpinCount（1 始まり）。</summary>
        public static EngageHint Roll(EngageHintConfig cfg, int stage, bool won, IRandom rng)
        {
            cfg ??= new EngageHintConfig();
            string key = won ? "won" : "lost";
            var h = new EngageHint { stage = stage, color = "none", serif = "none", hotSound = false };
            switch (stage)
            {
                case 1: h.color = Pick(cfg.stage1Color, key, Colors, rng); break;
                case 2: h.serif = Pick(cfg.stage2Serif, key, Serifs, rng); break;
                case 3: h.hotSound = Pick(cfg.stage3Sound, key, new[] { "normal", "hot" }, rng) == "hot"; break;
            }
            return h;
        }

        private static string Pick(Dictionary<string, Dictionary<string, int>> table, string key, string[] order, IRandom rng)
        {
            if (table == null || !table.TryGetValue(key, out var weights) || weights == null) return order[0];
            int total = 0;
            foreach (var o in order) if (weights.TryGetValue(o, out var w) && w > 0) total += w;
            if (total <= 0) return order[0];
            int roll = rng.Next(total);
            foreach (var o in order)
            {
                if (!weights.TryGetValue(o, out var w) || w <= 0) continue;
                if (roll < w) return o;
                roll -= w;
            }
            return order[0];
        }
    }
}
