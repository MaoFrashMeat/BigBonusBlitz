using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>AT 中に出るモンスター 1 体の定義（モンハン型: HP を削って討伐する）。</summary>
    public sealed class MonsterDef
    {
        public string id = "m_slime";
        public string name = "洞窟スライム";
        /// <summary>見た目に使う敵タイプ（Resources/Art/Enemies の名前。既存の敵と共用）。</summary>
        public string enemyType = "slime";
        /// <summary>出現重み。</summary>
        public int weight = 40;
        public int hp = 80;
        /// <summary>狩猟できるG数。この中で削り切れなければ失敗。</summary>
        public int spins = 5;
        /// <summary>討伐したときの上乗せG数。</summary>
        public int rewardSpins = 20;
    }

    /// <summary>AT の押し順ナビ（道中）。エンゲージの択ナビと違い「正解を教える」表示。</summary>
    public struct AtNavi
    {
        public bool Active;
        /// <summary>最初に押すリール（0=左, 1=中, 2=右）。ここを第一停止すれば正解。</summary>
        public int first;
    }

    /// <summary>
    /// AT「洞窟」の設定。道中は押し順ベルで枚数を稼ぎ（番長型）、バトル当選でモンスター狩猟に入る（モンハン型）。
    /// 重み・確率は全て game_config.json の at で調整する。
    /// </summary>
    public sealed class AtConfig
    {
        /// <summary>AT の初期G数。</summary>
        public int initialSpins = 50;
        /// <summary>
        /// AT 当選から洞窟に入るまでの前兆G数の重み（G数 → 重み）。
        /// この G を消化しきった次のGで「洞窟探索」のタイトルが出て AT が始まる。
        /// </summary>
        public Dictionary<string, int> entryPrecursorSpins = new Dictionary<string, int> { ["2"] = 20, ["3"] = 55, ["4"] = 25 };
        /// <summary>true: ボーナス終了時の AT 期待度 % をそのまま当選率にする。false: flatRate を使う。</summary>
        public bool useExpectAsRate = true;
        /// <summary>useExpectAsRate=false のときの固定当選率 %。</summary>
        public int flatRate = 20;
        /// <summary>押し順ナビに従ったときの払い出し枚数。</summary>
        public int naviCorrectPayout = 15;
        /// <summary>ナビを外したときの払い出し枚数（こぼし）。</summary>
        public int naviWrongPayout = 3;
        /// <summary>バトル中のGで AT の残りGを消費するか。false なら狩猟中はGが減らない。</summary>
        public bool battleConsumesAtSpins = false;
        /// <summary>討伐に失敗したときの救済上乗せG数。</summary>
        public int failRewardSpins = 0;
        /// <summary>役キー → バトル当選率 %。</summary>
        public Dictionary<string, int> battleRate = new Dictionary<string, int>
        {
            ["BELL"] = 2, ["REPLAY"] = 3, ["CHERRY"] = 25, ["SUICA"] = 35, ["CHANCE"] = 100, ["HAZE"] = 1,
        };
        /// <summary>役キー → 「ダメージ量 → 重み」。バトル中に毎G抽選する。</summary>
        public Dictionary<string, Dictionary<string, int>> damage = new Dictionary<string, Dictionary<string, int>>
        {
            ["BELL"] = new Dictionary<string, int> { ["5"] = 60, ["10"] = 30, ["20"] = 10 },
            ["REPLAY"] = new Dictionary<string, int> { ["0"] = 70, ["5"] = 30 },
            ["CHERRY"] = new Dictionary<string, int> { ["20"] = 50, ["35"] = 35, ["50"] = 15 },
            ["SUICA"] = new Dictionary<string, int> { ["30"] = 50, ["50"] = 35, ["80"] = 15 },
            ["CHANCE"] = new Dictionary<string, int> { ["999"] = 100 },
            ["HAZE"] = new Dictionary<string, int> { ["0"] = 100 },
        };
        public List<MonsterDef> monsters = new List<MonsterDef>
        {
            new MonsterDef { id = "m_slime", name = "洞窟スライム", enemyType = "slime", weight = 40, hp = 80, spins = 5, rewardSpins = 20 },
            new MonsterDef { id = "m_goblin", name = "洞窟ゴブリン", enemyType = "goblin", weight = 35, hp = 120, spins = 5, rewardSpins = 40 },
            new MonsterDef { id = "m_bat", name = "大コウモリ", enemyType = "bat", weight = 25, hp = 160, spins = 6, rewardSpins = 80 },
        };
    }

    public static class AtDirectorEx
    {
        /// <summary>「数値の文字列 → 重み」から 1 つ選ぶ。空・不正なら fallback。</summary>
        public static int RollWeightedInt(Dictionary<string, int> weights, IRandom rng, int fallback = 0)
        {
            if (weights == null || weights.Count == 0) return fallback;
            int total = 0;
            foreach (var kv in weights) if (int.TryParse(kv.Key, out _)) total += System.Math.Max(0, kv.Value);
            if (total <= 0) return fallback;
            int r = rng.Next(total);
            foreach (var kv in weights)
            {
                if (!int.TryParse(kv.Key, out var n)) continue;
                int w = System.Math.Max(0, kv.Value);
                if (r < w) return n;
                r -= w;
            }
            return fallback;
        }

        /// <summary>
        /// バトル当選抽選。判定は「成立フラグ」で行う（こぼしても成立役はスイカ／チェリーなので同じ扱い）。
        /// </summary>
        public static bool RollBattle(AtConfig cfg, Flag flag, IRandom rng)
        {
            cfg = cfg ?? new AtConfig();
            if (cfg.battleRate == null) return false;
            string key = PrecogDirector.RoleKey(flag);
            if (!cfg.battleRate.TryGetValue(key, out var rate) || rate <= 0) return false;
            return rng.NextDouble() * 100 < rate;
        }

        /// <summary>出現するモンスターを重みで選ぶ。</summary>
        public static MonsterDef PickMonster(AtConfig cfg, IRandom rng)
        {
            cfg = cfg ?? new AtConfig();
            var list = cfg.monsters;
            if (list == null || list.Count == 0) return new MonsterDef();
            int total = 0;
            foreach (var m in list) total += System.Math.Max(0, m.weight);
            if (total <= 0) return list[0];
            int r = rng.Next(total);
            foreach (var m in list)
            {
                int w = System.Math.Max(0, m.weight);
                if (r < w) return m;
                r -= w;
            }
            return list[0];
        }

        /// <summary>
        /// ダメージ抽選。999 は一撃討伐。判定は「成立フラグ」で行う。
        /// 目押しでこぼしても成立役はスイカ／チェリーなので、ダメージは同じように入る。
        /// </summary>
        public static int RollDamage(AtConfig cfg, Flag flag, IRandom rng)
        {
            cfg = cfg ?? new AtConfig();
            if (cfg.damage == null) return 0;
            string key = PrecogDirector.RoleKey(flag);
            if (!cfg.damage.TryGetValue(key, out var dist)) return 0;
            return System.Math.Max(0, RollWeightedInt(dist, rng));
        }
    }
}
