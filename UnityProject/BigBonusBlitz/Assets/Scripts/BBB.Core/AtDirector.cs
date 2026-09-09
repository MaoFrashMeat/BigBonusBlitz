using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>期待度のランク 1 段。min 以上 next.min 未満でこのランク。</summary>
    public sealed class AtRank
    {
        public string name = "白";
        /// <summary>このランクになる下限 %。</summary>
        public int min = 0;
        /// <summary>枠の色（HTML カラー）。"rainbow" は色相を回す。</summary>
        public string color = "#f4f6fa";
    }

    /// <summary>
    /// ボーナス中の AT 期待度。各小役で加算値を抽選し、溜まった % でボーナス中の枠の点滅色が変わる。
    /// 色は業界慣習に合わせる（白 &lt; 青 &lt; 黄 &lt; 緑 &lt; 赤 &lt; 虹）。重みは game_config.json の atExpect。
    /// </summary>
    public sealed class AtExpectConfig
    {
        /// <summary>ボーナス開始時の期待度 %。</summary>
        public int startPercent = 0;
        /// <summary>期待度の上限 %。</summary>
        public int maxPercent = 100;
        /// <summary>役キー → 「加算値 → 重み」。加算値 0 は「上がらなかった」。</summary>
        public Dictionary<string, Dictionary<string, int>> gain = new Dictionary<string, Dictionary<string, int>>
        {
            ["BELL"] = new Dictionary<string, int> { ["0"] = 70, ["2"] = 22, ["5"] = 6, ["10"] = 2 },
            ["REPLAY"] = new Dictionary<string, int> { ["0"] = 80, ["1"] = 15, ["3"] = 5 },
            ["CHERRY"] = new Dictionary<string, int> { ["0"] = 40, ["5"] = 35, ["10"] = 20, ["25"] = 5 },
            ["SUICA"] = new Dictionary<string, int> { ["0"] = 30, ["8"] = 40, ["15"] = 25, ["30"] = 5 },
            ["CHANCE"] = new Dictionary<string, int> { ["0"] = 5, ["20"] = 35, ["35"] = 40, ["60"] = 20 },
            ["HAZE"] = new Dictionary<string, int> { ["0"] = 95, ["1"] = 5 },
        };
        /// <summary>帯テロップの流れる速さ（px/秒）。</summary>
        public float tickerSpeed = 110f;
        /// <summary>期待度が上がったとき出す PUSH ボタンの受付時間（秒）。この時間で自動的に押されたことにする。</summary>
        public float pushSeconds = 2.6f;
        /// <summary>ランク名 → 帯テロップの文言。ランクが上がるほど強い言い回しにする。</summary>
        public Dictionary<string, List<string>> ticker = new Dictionary<string, List<string>>
        {
            ["白"] = new List<string> { "小役を揃えて AT のチャンス！？", "スイカ・チェリーで期待度アップ" },
            ["青"] = new List<string> { "小役を揃えて AT のチャンス！？", "……何かが動き出している" },
            ["黄"] = new List<string> { "AT の気配が濃くなってきた！", "小役を引くほど近づく！" },
            ["緑"] = new List<string> { "AT 目前！ 小役を引け！", "この流れ、逃すな！" },
            ["赤"] = new List<string> { "AT 濃厚！ 最後まで引き切れ！", "洞窟はもう目の前だ！" },
            ["虹"] = new List<string> { "AT 確定！ 洞窟へ向かえ！！", "虹だ！ 掴み取れ！！" },
        };

        /// <summary>期待度 → 枠の色。min の昇順で並べる。</summary>
        public List<AtRank> ranks = new List<AtRank>
        {
            new AtRank { name = "白", min = 0, color = "#f4f6fa" },
            new AtRank { name = "青", min = 20, color = "#4da3ff" },
            new AtRank { name = "黄", min = 40, color = "#ffcf3f" },
            new AtRank { name = "緑", min = 60, color = "#3ddc84" },
            new AtRank { name = "赤", min = 80, color = "#ff4d6d" },
            new AtRank { name = "虹", min = 100, color = "rainbow" },
        };
    }

    public static class AtDirector
    {
        /// <summary>
        /// この G の成立フラグで期待度に何 % 足すかを抽選する。
        /// こぼしても成立役は同じなので、判定は winType ではなくフラグで行う。
        /// </summary>
        public static int RollGain(AtExpectConfig cfg, Flag flag, IRandom rng)
        {
            cfg = cfg ?? new AtExpectConfig();
            if (cfg.gain == null) return 0;
            string key = PrecogDirector.RoleKey(flag);
            if (!cfg.gain.TryGetValue(key, out var dist) || dist == null || dist.Count == 0) return 0;
            int total = 0;
            foreach (var kv in dist) total += System.Math.Max(0, kv.Value);
            if (total <= 0) return 0;
            int r = rng.Next(total);
            foreach (var kv in dist)
            {
                int w = System.Math.Max(0, kv.Value);
                if (r < w) return int.TryParse(kv.Key, out var v) ? System.Math.Max(0, v) : 0;
                r -= w;
            }
            return 0;
        }

        /// <summary>そのランクの帯テロップを 1 本返す。無ければ既定文。</summary>
        public static string TickerFor(AtExpectConfig cfg, string rankName, int index)
        {
            cfg = cfg ?? new AtExpectConfig();
            if (cfg.ticker != null && rankName != null && cfg.ticker.TryGetValue(rankName, out var list) && list != null && list.Count > 0)
                return list[((index % list.Count) + list.Count) % list.Count];
            return "小役を揃えて AT のチャンス！？";
        }

        /// <summary>いまの期待度に対応するランク。設定が空なら null。</summary>
        public static AtRank RankFor(AtExpectConfig cfg, int percent)
        {
            cfg = cfg ?? new AtExpectConfig();
            var ranks = cfg.ranks;
            if (ranks == null || ranks.Count == 0) return null;
            AtRank best = null;
            foreach (var r in ranks)
                if (r != null && percent >= r.min && (best == null || r.min >= best.min)) best = r;
            return best ?? ranks[0];
        }
    }
}
