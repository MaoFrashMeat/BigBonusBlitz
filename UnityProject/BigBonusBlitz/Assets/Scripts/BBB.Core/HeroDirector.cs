using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>
    /// 主人公のひとりごと（通常時にたまに喋る）。名前は travelers.heroName を使う。
    /// 文脈キー: idle（何もない）/ miss（ハズレ直後）/ win（小役獲得直後）/ longRun（ボーナス無しが長い）。
    /// 重みは game_config.json の hero。
    /// </summary>
    public sealed class HeroConfig
    {
        /// <summary>1G ごとに喋る確率 %（通常時・敵なし・前兆なし・会話中でないとき）。</summary>
        public int monologueRate = 4;
        /// <summary>この G 数以上ボーナスが無いと longRun のセリフが半分の確率で混ざる。</summary>
        public int longRunSpins = 200;
        /// <summary>longRun 条件を満たしたとき longRun のセリフに差し替える確率 %。</summary>
        public int longRunMix = 50;
        /// <summary>リプレイ時の主人公アクションの重み（bento=弁当を食べる, drink=水を飲む, stretch=背伸び, map=地図を見る, hum=口笛, rest=一息, none=何もしない）。</summary>
        public Dictionary<string, int> replayActions = new Dictionary<string, int> { ["bento"] = 25, ["drink"] = 20, ["stretch"] = 20, ["map"] = 15, ["hum"] = 15, ["rest"] = 15, ["none"] = 10 };
        /// <summary>洞窟に入るまでの前兆で主人公が言うセリフ。キーは段階番号("1".."N")。</summary>
        public Dictionary<string, List<string>> caveEntryLines = new Dictionary<string, List<string>>
        {
            ["1"] = new List<string> { "……この先に洞窟があるのか", "岩の隙間から風が吹いている", "地図にない道だ" },
            ["2"] = new List<string> { "奥から物音がする……", "引き返すなら今だぞ", "回復薬は足りてるか" },
            ["3"] = new List<string> { "……入口が見えた", "ここから先は戻れない", "行くぞ" },
        };
        /// <summary>ボーナス成立の前兆で主人公が言うセリフ（毎G ランダムに 1 つ）。当たりを断定しない言い回しにする。</summary>
        public List<string> bonusPrecursorLines = new List<string>
        {
            "……手応えがあった気がする", "今のは……？", "何かが変わった", "……いけるか？", "指先が疼く", "……この感じ、覚えがある"
        };
        /// <summary>敵出現の前兆で主人公が言うセリフ。キーは段階番号("1".."N")、各段階の中からランダムに 1 つ。</summary>
        public Dictionary<string, List<string>> precursorLines = new Dictionary<string, List<string>>
        {
            ["1"] = new List<string> { "ん……？", "……なんだ、今の", "気のせいか……？" },
            ["2"] = new List<string> { "……なにかいるな", "……近づいてくる", "この気配は……" },
            ["3"] = new List<string> { "……来るぞ", "……そこか", "構えろ……" },
        };
        public Dictionary<string, List<string>> lines = new Dictionary<string, List<string>>
        {
            ["idle"] = new List<string> { "……いい風だ", "この先に何があるんだろう", "腹、減ったな", "今日はどこまで行こうか", "足取りは軽い", "鼻歌でも歌うか" },
            ["miss"] = new List<string> { "……惜しい", "まあ、こんな日もある", "次だ、次", "集中しろ", "焦るな……" },
            ["win"] = new List<string> { "よし！", "まずまずだな", "この調子", "手応えあり", "ふっ、悪くない" },
            ["longRun"] = new List<string> { "長い道だ……", "そろそろ何か起きてもいい頃だ", "……気配がない", "静かすぎる" },
        };
    }

    public static class HeroDirector
    {
        /// <summary>喋るなら 1 行返す。喋らないなら null。</summary>
        public static string Roll(HeroConfig cfg, string context, int spinCount, IRandom rng)
        {
            cfg = cfg ?? new HeroConfig();
            if (cfg.lines == null || cfg.lines.Count == 0) return null;
            if (rng.NextDouble() * 100 >= cfg.monologueRate) return null;
            string key = context ?? "idle";
            if (spinCount >= cfg.longRunSpins && Has(cfg, "longRun") && rng.NextDouble() * 100 < cfg.longRunMix) key = "longRun";
            if (!Has(cfg, key)) key = "idle";
            if (!Has(cfg, key)) return null;
            var list = cfg.lines[key];
            return list[rng.Next(list.Count)];
        }

        /// <summary>リプレイ時のアクションを重みで 1 つ選ぶ（"none" は何もしない）。</summary>
        public static string RollReplayAction(HeroConfig cfg, IRandom rng)
        {
            cfg = cfg ?? new HeroConfig();
            var w = cfg.replayActions;
            if (w == null || w.Count == 0) return "none";
            int total = 0;
            foreach (var kv in w) total += System.Math.Max(0, kv.Value);
            if (total <= 0) return "none";
            int r = rng.Next(total);
            foreach (var kv in w)
            {
                int v = System.Math.Max(0, kv.Value);
                if (r < v) return kv.Key;
                r -= v;
            }
            return "none";
        }

        /// <summary>ボーナス前兆のセリフを 1 つ返す。無ければ null。</summary>
        public static string BonusPrecursorLine(HeroConfig cfg, IRandom rng)
        {
            cfg = cfg ?? new HeroConfig();
            var list = cfg.bonusPrecursorLines;
            if (list == null || list.Count == 0) return null;
            return list[rng.Next(list.Count)];
        }

        /// <summary>洞窟前兆の段階セリフを 1 つ返す（stage は 1 始まり）。</summary>
        public static string CaveEntryLine(HeroConfig cfg, int stage, IRandom rng)
            => StageLine((cfg ?? new HeroConfig()).caveEntryLines, stage, rng);

        /// <summary>前兆の段階セリフを 1 つ返す（stage は 1 始まり）。設定より深い段階は最後の段階を使う。</summary>
        public static string PrecursorLine(HeroConfig cfg, int stage, IRandom rng)
            => StageLine((cfg ?? new HeroConfig()).precursorLines, stage, rng);

        /// <summary>「段階番号 → セリフ候補」から 1 つ選ぶ。設定より深い段階は最後の段階を使う。</summary>
        private static string StageLine(Dictionary<string, List<string>> table, int stage, IRandom rng)
        {
            if (table == null || table.Count == 0) return null;
            List<string> list = null;
            for (int k = System.Math.Max(1, stage); k >= 1; k--)
                if (table.TryGetValue(k.ToString(), out list) && list != null && list.Count > 0) break;
                else list = null;
            if (list == null || list.Count == 0) return null;
            return list[rng.Next(list.Count)];
        }

        private static bool Has(HeroConfig cfg, string key) => cfg.lines.TryGetValue(key, out var l) && l != null && l.Count > 0;
    }
}
