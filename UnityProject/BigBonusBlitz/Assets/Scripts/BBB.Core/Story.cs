using System;
using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>
    /// 物語の 1 かたまり（1 つの場面で流す台詞）。話者を分けて持つ。
    /// speaker が空なら主人公。
    /// </summary>
    [Serializable]
    public sealed class StoryLine
    {
        public string speaker = "";
        public string text = "";
    }

    /// <summary>段（列）ごとの物語。枝の高さで 3 つのトーンに振り分ける。</summary>
    [Serializable]
    public sealed class StoryStage
    {
        /// <summary>対象の段（ステージの column と一致させる）。</summary>
        public string column = "";
        /// <summary>上の枝（灯が強い）。</summary>
        public List<StoryLine> high = new List<StoryLine>();
        /// <summary>真ん中。</summary>
        public List<StoryLine> mid = new List<StoryLine>();
        /// <summary>下の枝（灯が弱い）。</summary>
        public List<StoryLine> low = new List<StoryLine>();
    }

    [Serializable]
    public sealed class StoryChapter
    {
        public int chapter = 1;
        public string title = "";
        /// <summary>この章が問うていること（作り手の指針。画面には出さない）。</summary>
        public string theme = "";
        /// <summary>章に入るとき。</summary>
        public List<StoryLine> opening = new List<StoryLine>();
        /// <summary>段ごとの到達台詞。</summary>
        public List<StoryStage> stages = new List<StoryStage>();
        /// <summary>引き返したとき。</summary>
        public List<StoryLine> onBack = new List<StoryLine>();
        /// <summary>ライフが尽きて力尽きるとき。</summary>
        public List<StoryLine> onTorchOut = new List<StoryLine>();
        /// <summary>灯が尽きて力尽きたとき。</summary>
        public List<StoryLine> onDeath = new List<StoryLine>();
        /// <summary>章を踏破したとき。</summary>
        public List<StoryLine> onClear = new List<StoryLine>();
        /// <summary>2 周目以降に足す台詞（キーは周回数）。</summary>
        public Dictionary<string, List<StoryLine>> laps = new Dictionary<string, List<StoryLine>>();
        /// <summary>この章でのステージ名（キーはステージ id。無いステージは adventure の名のまま）。地図の形は章で変えない</summary>
        public Dictionary<string, string> stageNames = new Dictionary<string, string>();

        public StoryStage FindStage(string column)
        {
            if (stages == null || string.IsNullOrEmpty(column)) return null;
            foreach (var s in stages) if (s != null && s.column == column) return s;
            return null;
        }
    }

    /// <summary>序章の 1 場面（docs/scenario_op.md）。speaker: "" = 主人公、"声" = 姿のない語り手、"*" = ト書き、それ以外 = その名前。</summary>
    [Serializable]
    public sealed class OpeningScene
    {
        public string id = "";
        /// <summary>画面を暗くする度合い（0 = そのまま、1 = 真っ黒）。</summary>
        public float dim = 0.7f;
        public List<StoryLine> lines = new List<StoryLine>();
    }

    /// <summary>序章（最初からゲームを始めたときだけ。本人の骨組み 2026-09-21）。</summary>
    [Serializable]
    public sealed class OpeningConfig
    {
        public bool enabled = true;
        /// <summary>逃走の場面で持たせる LIFE（回転数）。これが尽きて倒れる。</summary>
        public int lifeSpins = 4;
        public List<OpeningScene> scenes = new List<OpeningScene>();
        public OpeningScene Find(string id) { foreach (var s in scenes) if (s != null && s.id == id) return s; return null; }
    }

    [Serializable]
    public sealed class StoryConfig
    {
        public bool enabled = true;
        /// <summary>序章。</summary>
        public OpeningConfig op = new OpeningConfig();
        /// <summary>「上の枝」とみなす枝番号の上限（1 なら最上位だけ）。</summary>
        public int highBranchMax = 2;
        /// <summary>「下の枝」とみなす、下からの枝数。</summary>
        public int lowBranchFromBottom = 1;
        public List<StoryChapter> chapters = new List<StoryChapter>();

        /// <summary>その章。用意していない章は、それより前で最後に用意した章を使い回す（第 3 章まであれば 4 章目以降は第 3 章）。</summary>
        public StoryChapter Find(int chapter)
        {
            if (chapters == null || chapters.Count == 0) return null;
            StoryChapter best = null;
            foreach (var c in chapters)
            {
                if (c == null) continue;
                if (c.chapter == chapter) return c;
                if (c.chapter < chapter && (best == null || c.chapter > best.chapter)) best = c;
            }
            return best ?? chapters[0];
        }

        /// <summary>その章だけ（使い回しなし）。無ければ null。</summary>
        public StoryChapter FindExact(int chapter)
        {
            if (chapters == null) return null;
            foreach (var c in chapters) if (c != null && c.chapter == chapter) return c;
            return null;
        }
    }

    public static class StoryDirector
    {
        /// <summary>
        /// そのステージに着いたときの台詞。枝の高さでトーンを選ぶ。
        /// 灯が強い（上の枝）ほど世界が応え、弱いほど冷たくなる。
        /// </summary>
        public static List<StoryLine> OnEnter(StoryConfig cfg, AdventureConfig adv, int chapter, string stageId)
        {
            if (cfg == null || !cfg.enabled || adv == null) return null;
            var node = adv.Find(stageId);
            if (node == null) return null;
            var ch = cfg.Find(chapter);
            var st = ch?.FindStage(node.column);
            if (st == null) return null;

            int branch = BranchIndex(stageId);
            int width = BranchCount(adv, node.column);
            var tone = ToneOf(cfg, branch, width);
            var list = tone == 0 ? st.high : tone == 1 ? st.mid : st.low;
            if (list == null || list.Count == 0) list = st.mid;
            if (list == null || list.Count == 0) list = st.high;
            return list != null && list.Count > 0 ? list : null;
        }

        /// <summary>0 = 上の枝 / 1 = 真ん中 / 2 = 下の枝。</summary>
        public static int ToneOf(StoryConfig cfg, int branchIndex, int width)
        {
            cfg = cfg ?? new StoryConfig();
            if (width <= 1) return 1;
            if (branchIndex < Math.Max(1, cfg.highBranchMax)) return 0;
            if (branchIndex >= width - Math.Max(1, cfg.lowBranchFromBottom)) return 2;
            return 1;
        }

        /// <summary>"C-3" → 2（0 始まり）。</summary>
        public static int BranchIndex(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) return 0;
            int dash = stageId.IndexOf('-');
            if (dash < 0 || dash + 1 >= stageId.Length) return 0;
            return int.TryParse(stageId.Substring(dash + 1), out var n) ? Math.Max(0, n - 1) : 0;
        }

        public static int BranchCount(AdventureConfig adv, string column)
        {
            if (adv?.nodes == null) return 1;
            int n = 0;
            foreach (var x in adv.nodes) if (x != null && x.column == column) n++;
            return Math.Max(1, n);
        }

        /// <summary>画面に出すステージ名。章にその id の名があればそれ、無ければ adventure の名。</summary>
        public static string StageName(StoryConfig cfg, int chapter, StageNode node)
        {
            if (node == null) return "";
            var ch = cfg != null && cfg.enabled ? cfg.Find(chapter) : null;   // 用意していない章は最後の章の名
            if (ch?.stageNames != null && !string.IsNullOrEmpty(node.id) && ch.stageNames.TryGetValue(node.id, out var name) && !string.IsNullOrEmpty(name)) return name;
            return node.name ?? "";
        }

        /// <summary>章の題。story にその章があればその題、無ければ使い回す章の題の「第n章」を今の番号にして出す（story が無ければ adventure.chapterName）。</summary>
        public static string ChapterTitle(StoryConfig cfg, AdventureConfig adv, int chapter)
        {
            var ch = cfg != null && cfg.enabled ? cfg.Find(chapter) : null;
            if (ch == null || string.IsNullOrEmpty(ch.title)) return adv?.chapterName ?? "";
            if (ch.chapter == chapter) return ch.title;
            return System.Text.RegularExpressions.Regex.Replace(ch.title, @"^第\s*\d+\s*章", "第" + chapter + "章");
        }

        public static List<StoryLine> Opening(StoryConfig cfg, int chapter) => cfg?.Find(chapter)?.opening;
        public static List<StoryLine> OnBack(StoryConfig cfg, int chapter) => cfg?.Find(chapter)?.onBack;
        public static List<StoryLine> OnTorchOut(StoryConfig cfg, int chapter) => cfg?.Find(chapter)?.onTorchOut;
        public static List<StoryLine> OnDeath(StoryConfig cfg, int chapter) => cfg?.Find(chapter)?.onDeath;
        public static List<StoryLine> OnClear(StoryConfig cfg, int chapter) => cfg?.Find(chapter)?.onClear;

        /// <summary>2 周目以降に足す台詞（同じ道をまた通っていることを世界が覚えている）。</summary>
        public static List<StoryLine> Lap(StoryConfig cfg, int chapter, int lap)
        {
            if (cfg == null || lap < 2) return null;
            // その章に周回の台詞があればそれ。用意した章（laps が空）は足さない。用意していない章（使い回し）は第 1 章の laps から足す
            var exact = cfg.FindExact(chapter);
            var laps = exact != null ? exact.laps : (cfg.chapters != null && cfg.chapters.Count > 0 ? cfg.chapters[0]?.laps : null);
            if (laps == null) return null;
            if (laps.TryGetValue(lap.ToString(), out var v) && v != null && v.Count > 0) return v;
            // 用意していない周回は、いちばん大きい番号のものを使う
            List<StoryLine> best = null;
            int bestKey = 1;
            foreach (var kv in laps)
                if (int.TryParse(kv.Key, out var k) && k <= lap && k > bestKey && kv.Value != null && kv.Value.Count > 0)
                { best = kv.Value; bestKey = k; }
            return best;
        }
    }
}
