using System;
using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>
    /// 冒険のステージ 1 つ。ステージは N G で終わり、終了時に次のステージへ進む（線形マップ型）。
    /// 道中で役を引くと「ルート抽選」が走り、行き先が早めに決まる（上位ルートへ書き換わることもある）。
    /// 全ての数値は game_config.json の adventure から読む。
    /// </summary>
    [Serializable]
    public sealed class StageNode
    {
        public string id = "A-1";
        public string name = "";
        /// <summary>表示上の段（A〜D）。マップの列になる。</summary>
        public string column = "A";
        /// <summary>種類: normal / high（高確） / treasure（宝） / boss / rest（休息）。演出と色の切り替えに使う。</summary>
        public string kind = "normal";
        /// <summary>このステージのG数（通常時のみ数える。持ち越し・ボーナス中は止まる）。</summary>
        public int spins = 40;
        /// <summary>滞在中に使う小役テーブルのモード（"" なら内部モードのまま）。高確ステージは "D" など。</summary>
        public string mode = "";
        /// <summary>ステージの良さ（ルート書き換えの判断に使う。高いほど良い）。</summary>
        public int rank = 0;
        /// <summary>毎G追加で敵出現を抽選する率 %（ワークフローで ENEMY にならなかったときだけ）。</summary>
        public int engageBoost = 0;
        /// <summary>到達時に中ボスとのエンゲージが始まる率 %（boss ステージ用）。</summary>
        public int bossRate = 0;
        /// <summary>役ごとの宝発見率 %。キーは BELL/REPLAY/CHERRY/SUICA/CHANCE/BONUS/HAZE。</summary>
        public Dictionary<string, int> treasure = new Dictionary<string, int>();
        /// <summary>役ごとのルート抽選。キーは役、値は {次のステージid: 重み, "NONE": 重み}。</summary>
        public Dictionary<string, Dictionary<string, int>> routeByFlag = new Dictionary<string, Dictionary<string, int>>();
        /// <summary>ステージ終了時に行き先が決まっていなければこの重みで選ぶ。空なら章クリア（終点）。</summary>
        public Dictionary<string, int> routeDefault = new Dictionary<string, int>();
        /// <summary>到着時に主人公が言うセリフ（ランダムに 1 つ）。</summary>
        public List<string> enterLines = new List<string>();
        /// <summary>マップの説明（1 行）。</summary>
        public string desc = "";
        /// <summary>マップ上の色（"#rrggbb"。空なら kind の既定色）。</summary>
        public string color = "";

        public bool IsGoal => routeDefault == null || routeDefault.Count == 0;
        public bool HasModeOverride => !string.IsNullOrEmpty(mode);
    }

    /// <summary>宝物 1 種。kind: souls / atSpins（次の AT に上乗せ）/ atExpect（次のボーナスの初期期待度）/ exp。</summary>
    [Serializable]
    public sealed class TreasureDef
    {
        public string id = "souls_s";
        public string name = "小さな宝箱";
        public string kind = "souls";
        public int amount = 30;
        public int weight = 50;
    }

    /// <summary>
    /// 冒険の資源（松明）と、力尽きたときの扱い。
    /// 松明は通常時のGでだけ減り、尽きると街へ強制帰還する（進行は残る）。
    /// クレジットが尽きたときは「力尽きた」扱いで、章の最初に戻される（進行を失う）。
    /// </summary>
    [Serializable]
    public sealed class ResourceConfig
    {
        /// <summary>松明を使うか。false なら資源制なし。</summary>
        public bool enabled = true;
        public string name = "松明";
        /// <summary>セーブが無いときの初期本数。</summary>
        public int startTorches = 3;
        public int maxTorches = 9;
        /// <summary>松明 1 本で進める通常G数。</summary>
        public int spinsPerTorch = 60;
        /// <summary>松明 1 本のソウル価格。</summary>
        public int torchCost = 40;
        /// <summary>路銀 1 口のソウル価格と、もらえるクレジット。</summary>
        public int creditCost = 30;
        public int creditAmount = 50;
        /// <summary>力尽きて街に戻ったとき、クレジットがこれ未満なら ここまで補填する（0 で補填なし）。</summary>
        public int rescueCredit = 50;
        /// <summary>力尽きたら章の最初へ戻す。</summary>
        public bool resetOnDeath = true;
        /// <summary>力尽きたときに失うソウルの割合 %（0 で没収なし）。</summary>
        public int deathSoulPenalty = 0;
        /// <summary>役ごとの松明が 1 本増える率 %。キーは BELL/REPLAY/CHERRY/SUICA/CHANCE/BONUS/HAZE。</summary>
        public Dictionary<string, int> refillByFlag = new Dictionary<string, int>();
    }

    [Serializable]
    public sealed class AdventureConfig
    {
        /// <summary>冒険を有効にするか（false なら従来通りステージ無し）。</summary>
        public bool enabled = true;
        public string chapterName = "第1章  はじまりの草原";
        public string start = "A-1";
        /// <summary>先のルートをマップで隠すか（通ったところだけ見える）。</summary>
        public bool hideRoute = true;
        /// <summary>章クリア時のソウル報酬。</summary>
        public int chapterClearSouls = 100;
        /// <summary>章クリア時にもらえる松明の本数。</summary>
        public int chapterClearTorches = 2;
        public List<StageNode> nodes = new List<StageNode>();
        public List<TreasureDef> treasures = new List<TreasureDef>();
        public ResourceConfig resource = new ResourceConfig();

        public StageNode Find(string id)
        {
            if (nodes == null || string.IsNullOrEmpty(id)) return null;
            foreach (var n in nodes) if (n != null && n.id == id) return n;
            return null;
        }
    }

    /// <summary>冒険の進行（セーブ対象）。</summary>
    public sealed class AdventureState
    {
        public string nodeId = "";
        /// <summary>現在のステージの残りG。</summary>
        public int spinsLeft;
        /// <summary>ルート抽選で決まった行き先（null なら未定）。</summary>
        public string nextId;
        /// <summary>この章で通ったステージ（現在地を含む）。</summary>
        public readonly List<string> visited = new List<string>();
        public int chapter = 1;
        public int treasuresFound;
        /// <summary>次の AT に上乗せするG数（宝で貯まる。AT 開始で消費）。</summary>
        public int stockAtSpins;
        /// <summary>次のボーナスの初期 AT 期待度に足す %（宝で貯まる。ボーナス開始で消費）。</summary>
        public int stockAtExpect;
        // --- 資源 ---
        /// <summary>松明の残り本数（使用中の 1 本を含む）。</summary>
        public int torches = -1;      // -1 = 未初期化（セーブが無いとき startTorches で埋める）
        /// <summary>使用中の松明の残りG。</summary>
        public int torchSpins;
        /// <summary>街へ強制帰還する理由（"torch" / "credit"）。街に着いたら空に戻す。</summary>
        public string returnReason;
        public bool MustReturn => !string.IsNullOrEmpty(returnReason);
    }

    public static class AdventureDirector
    {
        /// <summary>章の最初のステージに置く。</summary>
        public static void Reset(AdventureConfig cfg, AdventureState st)
        {
            st.nodeId = cfg?.start ?? "";
            var n = cfg?.Find(st.nodeId);
            st.spinsLeft = Math.Max(1, n?.spins ?? 1);
            st.nextId = null;
            st.visited.Clear();
            if (!string.IsNullOrEmpty(st.nodeId)) st.visited.Add(st.nodeId);
        }

        /// <summary>ステージに入る（残りGとルートを初期化）。</summary>
        public static void Enter(AdventureConfig cfg, AdventureState st, string id)
        {
            st.nodeId = id;
            var n = cfg?.Find(id);
            st.spinsLeft = Math.Max(1, n?.spins ?? 1);
            st.nextId = null;
            if (!st.visited.Contains(id)) st.visited.Add(id);
        }

        /// <summary>
        /// 道中のルート抽選。役ごとの重みで行き先を引く。決まっていない時は決める。
        /// 決まっている時は、より rank の高いステージを引いたときだけ書き換える。
        /// 決まった／書き換わったら新しい行き先を返す。それ以外は null。
        /// </summary>
        public static string RollRoute(AdventureConfig cfg, AdventureState st, string roleKey, IRandom rng)
        {
            var node = cfg?.Find(st.nodeId);
            if (node?.routeByFlag == null || !node.routeByFlag.TryGetValue(roleKey, out var table) || table == null) return null;
            string picked = Pick(table, rng);
            if (string.IsNullOrEmpty(picked) || picked == "NONE") return null;
            if (cfg.Find(picked) == null) return null;
            if (st.nextId == null) { st.nextId = picked; return picked; }
            if (st.nextId == picked) return null;
            int cur = cfg.Find(st.nextId)?.rank ?? 0;
            int nw = cfg.Find(picked).rank;
            if (nw > cur) { st.nextId = picked; return picked; }
            return null;
        }

        /// <summary>ステージ終了時の行き先。決まっていなければ既定の重みで引く。終点なら null。</summary>
        public static string ResolveNext(AdventureConfig cfg, AdventureState st, IRandom rng)
        {
            if (!string.IsNullOrEmpty(st.nextId) && cfg.Find(st.nextId) != null) return st.nextId;
            var node = cfg?.Find(st.nodeId);
            if (node == null || node.IsGoal) return null;
            string picked = Pick(node.routeDefault, rng);
            if (string.IsNullOrEmpty(picked) || picked == "NONE" || cfg.Find(picked) == null)
            {
                // 重みが壊れていても進めるよう、最初の候補に倒す
                foreach (var kv in node.routeDefault) if (kv.Key != "NONE" && cfg.Find(kv.Key) != null) return kv.Key;
                return null;
            }
            return picked;
        }

        /// <summary>宝の抽選。役ごとの率 % で当たり、当たれば重みで宝を 1 つ選ぶ。</summary>
        public static TreasureDef RollTreasure(AdventureConfig cfg, AdventureState st, string roleKey, IRandom rng)
        {
            var node = cfg?.Find(st.nodeId);
            if (node?.treasure == null || !node.treasure.TryGetValue(roleKey, out int rate) || rate <= 0) return null;
            if (cfg.treasures == null || cfg.treasures.Count == 0) return null;
            if (rng.NextDouble() * 100 >= rate) return null;
            int total = 0;
            foreach (var t in cfg.treasures) total += Math.Max(0, t.weight);
            if (total <= 0) return cfg.treasures[0];
            int r = rng.Next(total);
            foreach (var t in cfg.treasures)
            {
                int w = Math.Max(0, t.weight);
                if (r < w) return t;
                r -= w;
            }
            return cfg.treasures[cfg.treasures.Count - 1];
        }

        /// <summary>松明を初期化する（セーブが無いとき・章クリアで街に戻ったとき）。</summary>
        public static void ResetTorches(AdventureConfig cfg, AdventureState st, int perTorch = 0)
        {
            var r = cfg?.resource ?? new ResourceConfig();
            st.torches = Math.Max(0, r.startTorches);
            st.torchSpins = st.torches > 0 ? Unit(r, perTorch) : 0;
        }

        /// <summary>松明 1 本で進めるG数（装備の効果を足した値。0 なら設定値）。</summary>
        private static int Unit(ResourceConfig r, int perTorch) => perTorch > 0 ? perTorch : Math.Max(1, r.spinsPerTorch);

        /// <summary>セーブから読んだ値が壊れていたら直す。</summary>
        public static void NormalizeTorches(AdventureConfig cfg, AdventureState st, int perTorch = 0)
        {
            var r = cfg?.resource ?? new ResourceConfig();
            int unit = Unit(r, perTorch);
            if (st.torches < 0) { ResetTorches(cfg, st, perTorch); return; }
            st.torches = Math.Min(st.torches, Math.Max(1, r.maxTorches));
            if (st.torches > 0 && (st.torchSpins <= 0 || st.torchSpins > unit)) st.torchSpins = unit;
            if (st.torches <= 0) { st.torches = 0; st.torchSpins = 0; }
        }

        /// <summary>通常時の 1G ぶん松明を燃やす。尽きたら true。</summary>
        public static bool BurnTorch(AdventureConfig cfg, AdventureState st, int perTorch = 0)
        {
            var r = cfg?.resource;
            if (r == null || !r.enabled) return false;
            if (st.torches <= 0) return true;
            if (st.torchSpins > 0) st.torchSpins--;
            if (st.torchSpins > 0) return false;
            st.torches--;
            if (st.torches > 0) { st.torchSpins = Unit(r, perTorch); return false; }
            st.torchSpins = 0;
            return true;
        }

        /// <summary>松明を n 本足す（上限まで）。実際に増えた本数を返す。</summary>
        public static int AddTorch(AdventureConfig cfg, AdventureState st, int n = 1, int perTorch = 0)
        {
            var r = cfg?.resource ?? new ResourceConfig();
            int before = Math.Max(0, st.torches);
            st.torches = Math.Min(Math.Max(1, r.maxTorches), before + Math.Max(0, n));
            if (before <= 0 && st.torches > 0) st.torchSpins = Unit(r, perTorch);
            return st.torches - before;
        }

        /// <summary>道中で松明が 1 本増える抽選（役ごと）。増えたら true。</summary>
        public static bool RollRefill(AdventureConfig cfg, AdventureState st, string roleKey, IRandom rng, int perTorch = 0)
        {
            var r = cfg?.resource;
            if (r == null || !r.enabled || r.refillByFlag == null) return false;
            if (!r.refillByFlag.TryGetValue(roleKey, out int rate) || rate <= 0) return false;
            if (st.torches >= Math.Max(1, r.maxTorches)) return false;
            if (rng.NextDouble() * 100 >= rate) return false;
            return AddTorch(cfg, st, 1, perTorch) > 0;
        }

        /// <summary>力尽きたときの罰。章の最初へ戻し、宝の貯金を失う。失ったソウルを返す。</summary>
        public static int ApplyDeathPenalty(AdventureConfig cfg, AdventureState st, PlayerWallet wallet)
        {
            var r = cfg?.resource ?? new ResourceConfig();
            st.stockAtSpins = 0;
            st.stockAtExpect = 0;
            int lost = 0;
            if (wallet != null && r.deathSoulPenalty > 0)
            {
                lost = Math.Max(0, wallet.Souls * Math.Min(100, r.deathSoulPenalty) / 100);
                wallet.Souls -= lost;
            }
            if (r.resetOnDeath) Reset(cfg, st);
            return lost;
        }

        /// <summary>ステージの色（設定が無ければ種類の既定色）。</summary>
        public static string ColorFor(StageNode n)
        {
            if (n == null) return "#7f8aa3";
            if (!string.IsNullOrEmpty(n.color)) return n.color;
            switch (n.kind)
            {
                case "high": return "#ff4d6d";
                case "treasure": return "#ffcf3f";
                case "boss": return "#ff9a3c";
                case "rest": return "#3ddc84";
                default: return "#4da3ff";
            }
        }

        public static string KindLabel(string kind)
        {
            switch (kind)
            {
                case "high": return "高確";
                case "treasure": return "宝";
                case "boss": return "ボス";
                case "rest": return "休息";
                default: return "通常";
            }
        }

        private static string Pick(Dictionary<string, int> weights, IRandom rng)
        {
            if (weights == null || weights.Count == 0) return null;
            int total = 0;
            foreach (var kv in weights) total += Math.Max(0, kv.Value);
            if (total <= 0) return null;
            int r = rng.Next(total);
            foreach (var kv in weights)
            {
                int w = Math.Max(0, kv.Value);
                if (r < w) return kv.Key;
                r -= w;
            }
            return null;
        }
    }
}
