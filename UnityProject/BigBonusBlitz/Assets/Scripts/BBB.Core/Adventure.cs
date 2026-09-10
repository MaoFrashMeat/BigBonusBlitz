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
        /// <summary>達成条件で決まるルート（確率抽選より強い）。</summary>
        public List<RouteCondition> routeConditions = new List<RouteCondition>();
        /// <summary>条件を満たせなかったときに戻る先。空なら routeDefault、それも空ならその場に留まる。</summary>
        public string back = "";
        /// <summary>章の何番目か。深いほど良い報酬。表示と報酬の計算に使う。</summary>
        public int depth;
        /// <summary>はじめて到達したときだけ貰えるソウル（戻って再訪しても貰えない）。</summary>
        public int firstVisitSouls;
        /// <summary>到着時に主人公が言うセリフ（ランダムに 1 つ）。</summary>
        public List<string> enterLines = new List<string>();
        /// <summary>マップの説明（1 行）。</summary>
        public string desc = "";
        /// <summary>マップ上の色（"#rrggbb"。空なら kind の既定色）。</summary>
        public string color = "";

        public bool IsGoal => routeDefault == null || routeDefault.Count == 0;
        public bool HasModeOverride => !string.IsNullOrEmpty(mode);
    }

    /// <summary>
    /// ステージ滞在中の「達成条件」で決まるルート。確率抽選より強く、priority の高いものが勝つ。
    /// counters は滞在中に数えた回数、state はそのGの時点の持ち物や状態。どちらも「以上」で判定し、
    /// 並べたものを全部満たしたときだけ成立する。
    /// </summary>
    [Serializable]
    public sealed class RouteCondition
    {
        /// <summary>成立したときの行き先。</summary>
        public string to = "";
        /// <summary>マップに出す名前（「リプレイを 15 回」など）。空なら自動で組み立てる。</summary>
        public string label = "";
        /// <summary>滞在中に数える回数。キーは AdventureDirector.CounterKeys 参照。</summary>
        public Dictionary<string, int> counters = new Dictionary<string, int>();
        /// <summary>そのGの時点の状態。キーは AdventureDirector.StateKeys 参照。</summary>
        public Dictionary<string, int> state = new Dictionary<string, int>();
        /// <summary>優先度。高いほど強い。確率で決まったルートは 0 として扱う。</summary>
        public int priority = 10;
        /// <summary>達成するまでマップに出さない（隠し条件）。</summary>
        public bool hidden;
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
    /// 冒険の資源（ライフ）と、力尽きたときの扱い。
    ///
    /// ライフは通常時の 1G につき 1 減り、0 になると力尽きる（章の最初へ戻る）。
    /// エンバー（灯火）が尽きたときも同じく力尽きた扱いになる。
    ///
    /// 内部では「回復薬 n 個 × 1 個ぶんのG数」で持っている（torches / torchSpins）。
    /// 見た目は 1 本のバーだが、ショップで買う単位が要るのでこの持ち方にしてある。
    /// 合計値の読み書きは AdventureDirector.Hp / HealHp を通す。
    /// </summary>
    [Serializable]
    public sealed class ResourceConfig
    {
        /// <summary>ライフを使うか。false なら資源制なし。</summary>
        public bool enabled = true;
        /// <summary>ショップで買う回復アイテムの名前。</summary>
        public string name = "回復薬";
        /// <summary>バーに出す資源そのものの名前。</summary>
        public string hpName = "ライフ";
        /// <summary>セーブが無いときの初期の回復薬の個数（初期ライフ = これ × spinsPerTorch）。</summary>
        public int startTorches = 3;
        /// <summary>持てる上限の個数（最大ライフ = これ × spinsPerTorch）。</summary>
        public int maxTorches = 9;
        /// <summary>回復薬 1 個で回復するライフ（＝進めるG数）。</summary>
        public int spinsPerTorch = 60;
        /// <summary>回復薬 1 個のエンバー価格。</summary>
        public int torchCost = 40;
        /// <summary>ボーナス中にベルが揃ったときライフが回復する率 %。0 で回復なし。</summary>
        public int bonusBellHealRate = 30;
        /// <summary>そのときの回復量。</summary>
        public int bonusBellHealAmount = 8;
        /// <summary>通常時にリプレイが揃ったとき回復するライフ。0 で回復なし。</summary>
        public int replayHealAmount = 3;
        /// <summary>エンバー 1 口のソウル価格と、もらえる量。</summary>
        public int creditCost = 30;
        public int creditAmount = 50;
        /// <summary>力尽きて街に戻ったとき、エンバーがこれ未満なら ここまで補填する（0 で補填なし）。</summary>
        public int rescueCredit = 50;
        /// <summary>力尽きたら章の最初へ戻す。</summary>
        public bool resetOnDeath = true;
        /// <summary>力尽きたときに失うソウルの割合 %（0 で没収なし）。</summary>
        public int deathSoulPenalty = 0;
        /// <summary>役ごとに回復薬が 1 個増える率 %。キーは BELL/REPLAY/CHERRY/SUICA/CHANCE/BONUS/HAZE。</summary>
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
        /// <summary>章クリア時にもらえる回復薬の個数。</summary>
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
        // --- 資源（ライフ）---
        // ライフの合計 = (torches - 1) * 1個ぶん + torchSpins。読み書きは AdventureDirector.Hp / SetHp を通す
        /// <summary>回復薬の残り個数（使いかけの 1 個を含む）。</summary>
        public int torches = -1;      // -1 = 未初期化（セーブが無いとき startTorches で埋める）
        /// <summary>使いかけの 1 個に残っているライフ。</summary>
        public int torchSpins;
        /// <summary>ステージ滞在中に数えている回数（役の回数・G数・討伐数など）。ステージが変わると 0 に戻る。</summary>
        public readonly Dictionary<string, int> counters = new Dictionary<string, int>();
        /// <summary>いま何G連続でリプレイが続いているか。</summary>
        public int replayChain;
        /// <summary>行き先を決めた条件の優先度（確率で決まったときは 0）。</summary>
        public int decidedPriority;
        /// <summary>行き先を決めた条件の名前（表示用）。</summary>
        public string decidedBy;
        /// <summary>この章で到達した一番深いところ。</summary>
        public int deepest;
        /// <summary>この章で後退した回数（報酬の目減りに使う）。</summary>
        public int setbacks;
        /// <summary>街へ強制帰還する理由（"torch" / "credit"）。街に着いたら空に戻す。</summary>
        public string returnReason;
        public bool MustReturn => !string.IsNullOrEmpty(returnReason);
    }

    public static class AdventureDirector
    {
        /// <summary>数えられる回数のキー。</summary>
        public static readonly string[] CounterKeys = { "REPLAY", "BELL", "CHERRY", "SUICA", "CHANCE", "BONUS", "HAZE", "WIN", "SPINS", "DEFEAT", "TREASURE", "REPLAY_CHAIN", "PAYOUT" };
        /// <summary>その時点の状態で見られるキー。</summary>
        public static readonly string[] StateKeys = { "ember", "souls", "level", "torches" };

        public static string CounterName(string key)
        {
            switch (key)
            {
                case "REPLAY": return "リプレイ";
                case "BELL": return "ベル";
                case "CHERRY": return "チェリー";
                case "SUICA": return "スイカ";
                case "CHANCE": return "チャンス目";
                case "BONUS": return "ボーナス";
                case "HAZE": return "ハズレ";
                case "WIN": return "小役";
                case "SPINS": return "ゲーム数";
                case "DEFEAT": return "討伐";
                case "TREASURE": return "宝";
                case "REPLAY_CHAIN": return "リプレイ連続";
                case "PAYOUT": return "獲得";
                default: return key;
            }
        }

        public static string StateName(string key)
        {
            switch (key)
            {
                case "ember": return "エンバー";
                case "souls": return "ソウル";
                case "level": return "レベル";
                case "torches": return "回復薬";
                default: return key;
            }
        }

        /// <summary>章の最初のステージに置く。</summary>
        public static void Reset(AdventureConfig cfg, AdventureState st)
        {
            st.nodeId = cfg?.start ?? "";
            var n = cfg?.Find(st.nodeId);
            st.spinsLeft = Math.Max(1, n?.spins ?? 1);
            st.nextId = null;
            ClearProgress(st);
            st.visited.Clear();
            st.deepest = n?.depth ?? 0;
            st.setbacks = 0;
            if (!string.IsNullOrEmpty(st.nodeId)) st.visited.Add(st.nodeId);
        }

        /// <summary>ステージに入る（残りGとルートを初期化）。</summary>
        public static void Enter(AdventureConfig cfg, AdventureState st, string id)
        {
            st.nodeId = id;
            var n = cfg?.Find(id);
            st.spinsLeft = Math.Max(1, n?.spins ?? 1);
            st.nextId = null;
            ClearProgress(st);
            if (n != null && n.depth > st.deepest) st.deepest = n.depth;
            if (!st.visited.Contains(id)) st.visited.Add(id);
        }

        /// <summary>そのステージにまだ足を踏み入れていないか（初回報酬の判定）。</summary>
        public static bool IsFirstVisit(AdventureState st, string id) => st != null && !st.visited.Contains(id);

        /// <summary>
        /// ステージ終了時の行き先。条件を満たしていれば前へ、満たしていなければ戻る。
        /// 戻り先が無ければその場に留まり、終点なら null（章クリア）。
        /// forward には条件で決まった行き先が入る。
        /// </summary>
        public static string ResolveStep(AdventureConfig cfg, AdventureState st, IRandom rng, out bool advanced)
        {
            advanced = false;
            var node = cfg?.Find(st.nodeId);
            if (node == null) return null;

            // 条件を満たしている（= 行き先が決まっている）なら前へ
            if (!string.IsNullOrEmpty(st.nextId) && cfg.Find(st.nextId) != null)
            {
                advanced = true;
                return st.nextId;
            }
            if (node.IsGoal && string.IsNullOrEmpty(node.back)) return null;   // 終点で条件も無ければ章クリア

            // 満たせなかったので戻る
            if (!string.IsNullOrEmpty(node.back) && cfg.Find(node.back) != null) return node.back;

            // 戻り先が無いときは従来どおり既定の重みで選ぶ（後方互換）
            string picked = Pick(node.routeDefault, rng);
            if (!string.IsNullOrEmpty(picked) && picked != "NONE" && cfg.Find(picked) != null) { advanced = true; return picked; }
            if (node.IsGoal) return null;
            return st.nodeId;   // 留まる
        }

        /// <summary>ステージごとの数えものをリセットする。</summary>
        public static void ClearProgress(AdventureState st)
        {
            st.counters.Clear();
            st.replayChain = 0;
            st.decidedPriority = 0;
            st.decidedBy = null;
        }

        public static int GetCount(AdventureState st, string key)
            => st != null && key != null && st.counters.TryGetValue(key, out var v) ? v : 0;

        /// <summary>数えものを足す。</summary>
        public static void AddCount(AdventureState st, string key, int n = 1)
        {
            if (st == null || string.IsNullOrEmpty(key) || n == 0) return;
            st.counters[key] = GetCount(st, key) + n;
        }

        /// <summary>数えものを「これまでの最大」で更新する（リプレイ連続など）。</summary>
        public static void MaxCount(AdventureState st, string key, int v)
        {
            if (st == null || string.IsNullOrEmpty(key)) return;
            if (v > GetCount(st, key)) st.counters[key] = v;
        }

        private static int StateValue(string key, AdventureState st, int ember, int souls, int level)
        {
            switch (key)
            {
                case "ember": return ember;
                case "souls": return souls;
                case "level": return level;
                case "torches": return Math.Max(0, st.torches);
                default: return int.MaxValue;   // 知らないキーは条件として無視する
            }
        }

        /// <summary>条件をすべて満たしているか。</summary>
        public static bool Meets(RouteCondition c, AdventureState st, int ember, int souls, int level, int harder = 0)
        {
            if (c == null) return false;
            if (c.counters != null)
                foreach (var kv in c.counters)
                    if (kv.Value > 0 && GetCount(st, kv.Key) < kv.Value + Math.Max(0, harder)) return false;
            if (c.state != null)
                foreach (var kv in c.state)
                    if (kv.Value > 0 && StateValue(kv.Key, st, ember, souls, level) < kv.Value) return false;
            return true;
        }

        /// <summary>
        /// 今のステージの達成条件を見て、成立していて今より強いものがあれば返す。無ければ null。
        /// </summary>
        public static RouteCondition CheckConditions(AdventureConfig cfg, AdventureState st, int ember, int souls, int level, int harder = 0)
        {
            var node = cfg?.Find(st.nodeId);
            if (node?.routeConditions == null || node.routeConditions.Count == 0) return null;
            RouteCondition best = null;
            foreach (var c in node.routeConditions)
            {
                if (c == null || string.IsNullOrEmpty(c.to) || cfg.Find(c.to) == null) continue;
                if (c.priority <= st.decidedPriority) continue;          // 同じか弱い条件では覆さない
                if (c.to == st.nextId) continue;                          // もう同じ行き先
                if (!Meets(c, st, ember, souls, level, harder)) continue;
                if (best == null || c.priority > best.priority) best = c;
            }
            return best;
        }

        /// <summary>条件の説明（label が空なら中身から組み立てる）。</summary>
        public static string DescribeCondition(RouteCondition c)
        {
            if (c == null) return "";
            if (!string.IsNullOrEmpty(c.label)) return c.label;
            var parts = new List<string>();
            if (c.counters != null) foreach (var kv in c.counters) if (kv.Value > 0) parts.Add($"{CounterName(kv.Key)} {kv.Value}");
            if (c.state != null) foreach (var kv in c.state) if (kv.Value > 0) parts.Add($"{StateName(kv.Key)} {kv.Value} 以上");
            return parts.Count == 0 ? "条件なし" : string.Join(" ＋ ", parts);
        }

        /// <summary>達成の進み具合（「リプレイ 12/15」の形）。</summary>
        public static string ProgressText(RouteCondition c, AdventureState st, int ember, int souls, int level)
        {
            if (c == null) return "";
            var parts = new List<string>();
            if (c.counters != null)
                foreach (var kv in c.counters)
                {
                    if (kv.Value <= 0) continue;
                    parts.Add($"{CounterName(kv.Key)} {Math.Min(GetCount(st, kv.Key), kv.Value)}/{kv.Value}");
                }
            if (c.state != null)
                foreach (var kv in c.state)
                {
                    if (kv.Value <= 0) continue;
                    int v = StateValue(kv.Key, st, ember, souls, level);
                    if (v == int.MaxValue) continue;
                    parts.Add($"{StateName(kv.Key)} {v}/{kv.Value}");
                }
            return string.Join("  ", parts);
        }

        /// <summary>
        /// 道中のルート抽選。役ごとの重みで行き先を引く。決まっていない時は決める。
        /// 決まっている時は、より rank の高いステージを引いたときだけ書き換える。
        /// 決まった／書き換わったら新しい行き先を返す。それ以外は null。
        /// </summary>
        public static string RollRoute(AdventureConfig cfg, AdventureState st, string roleKey, IRandom rng)
        {
            if (st.decidedPriority > 0) return null;   // 条件で決まったルートは確率で覆さない
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
        public static TreasureDef RollTreasure(AdventureConfig cfg, AdventureState st, string roleKey, IRandom rng, float bonusRate = 0f)
        {
            var node = cfg?.Find(st.nodeId);
            if (node?.treasure == null || !node.treasure.TryGetValue(roleKey, out int rate) || rate <= 0) return null;
            if (cfg.treasures == null || cfg.treasures.Count == 0) return null;
            if (rng.NextDouble() * 100 >= rate + bonusRate) return null;
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

        // ---------------------------------------------------------------- ライフ
        // 内部の「回復薬 n 個 + 使いかけの残り」を 1 本のバーとして読み書きする層。
        // 1G で 1 減るので、合計値がそのまま「あと何G歩けるか」になる。

        /// <summary>いまのライフ。</summary>
        public static int Hp(AdventureConfig cfg, AdventureState st, int perTorch = 0)
        {
            if (st == null || st.torches <= 0) return 0;
            int unit = Unit(cfg?.resource ?? new ResourceConfig(), perTorch);
            return Math.Max(0, (st.torches - 1) * unit + Math.Max(0, st.torchSpins));
        }

        /// <summary>最大ライフ。</summary>
        public static int HpMax(AdventureConfig cfg, int perTorch)
        {
            var r = cfg?.resource ?? new ResourceConfig();
            return Math.Max(1, r.maxTorches) * Math.Max(1, Unit(r, perTorch));
        }

        /// <summary>ライフを直接置く（内部の個数と残りに割り直す）。</summary>
        public static void SetHp(AdventureConfig cfg, AdventureState st, int hp, int perTorch)
        {
            var r = cfg?.resource ?? new ResourceConfig();
            int unit = Unit(r, perTorch);
            hp = Math.Max(0, Math.Min(HpMax(cfg, perTorch), hp));
            if (hp <= 0) { st.torches = 0; st.torchSpins = 0; return; }
            st.torches = (hp + unit - 1) / unit;              // 使いかけを 1 個と数える
            st.torchSpins = hp - (st.torches - 1) * unit;
        }

        /// <summary>ライフを回復する。実際に回復した量を返す（満タンなら 0）。</summary>
        public static int HealHp(AdventureConfig cfg, AdventureState st, int amount, int perTorch = 0)
        {
            var r = cfg?.resource;
            if (r == null || !r.enabled || amount <= 0 || st == null) return 0;
            int unit = Unit(r, perTorch);
            int now = Hp(cfg, st, perTorch);
            int after = Math.Min(HpMax(cfg, perTorch), now + amount);
            if (after <= now) return 0;
            SetHp(cfg, st, after, perTorch);
            return after - now;
        }

        /// <summary>ライフを削る。0 になったら true（力尽きる）。</summary>
        public static bool DamageHp(AdventureConfig cfg, AdventureState st, int amount, int perTorch = 0)
        {
            var r = cfg?.resource;
            if (r == null || !r.enabled || st == null) return false;
            int unit = Unit(r, perTorch);
            int after = Math.Max(0, Hp(cfg, st, perTorch) - Math.Max(0, amount));
            SetHp(cfg, st, after, perTorch);
            return after <= 0;
        }

        /// <summary>ライフを初期化する（セーブが無いとき・章クリアで街に戻ったとき）。</summary>
        public static void ResetTorches(AdventureConfig cfg, AdventureState st, int perTorch = 0)
        {
            var r = cfg?.resource ?? new ResourceConfig();
            st.torches = Math.Max(0, r.startTorches);
            st.torchSpins = st.torches > 0 ? Unit(r, perTorch) : 0;
        }

        /// <summary>回復薬 1 個ぶんのライフ（装備の効果を足した値。0 なら設定値）。</summary>
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

        /// <summary>通常時の 1G ぶんライフを削る。尽きたら true。</summary>
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

        /// <summary>回復薬を n 個足す（上限まで）。実際に増えた個数を返す。</summary>
        public static int AddTorch(AdventureConfig cfg, AdventureState st, int n = 1, int perTorch = 0)
        {
            var r = cfg?.resource ?? new ResourceConfig();
            int before = Math.Max(0, st.torches);
            st.torches = Math.Min(Math.Max(1, r.maxTorches), before + Math.Max(0, n));
            if (before <= 0 && st.torches > 0) st.torchSpins = Unit(r, perTorch);
            return st.torches - before;
        }

        /// <summary>道中で回復薬が 1 個増える抽選（役ごと）。増えたら true。</summary>
        public static bool RollRefill(AdventureConfig cfg, AdventureState st, string roleKey, IRandom rng, int perTorch = 0)
        {
            var r = cfg?.resource;
            if (r == null || !r.enabled || r.refillByFlag == null) return false;
            if (!r.refillByFlag.TryGetValue(roleKey, out int rate) || rate <= 0) return false;
            if (st.torches >= Math.Max(1, r.maxTorches)) return false;
            if (rng.NextDouble() * 100 >= rate) return false;
            return AddTorch(cfg, st, 1, perTorch) > 0;
        }

        /// <summary>
        /// 力尽きたときの罰。章の最初へ戻し、宝の貯金を失う。失ったソウルを返す。
        /// ライフは初期値まで戻す（0 のままだと街から出られなくなるため）。
        /// </summary>
        public static int ApplyDeathPenalty(AdventureConfig cfg, AdventureState st, PlayerWallet wallet, int perTorch = 0)
        {
            var r = cfg?.resource ?? new ResourceConfig();
            st.stockAtSpins = 0;
            st.stockAtExpect = 0;
            int lost = 0;
            if (wallet != null && r.deathSoulPenalty > 0)
            {
                lost = Math.Max(0, wallet.Souls * Math.Min(100, r.deathSoulPenalty) / 100);
                wallet.Souls -= lost;   // 力尽きた代償はソウル（スキル資源）から
            }
            if (r.resetOnDeath) Reset(cfg, st);
            ResetTorches(cfg, st, perTorch);
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
