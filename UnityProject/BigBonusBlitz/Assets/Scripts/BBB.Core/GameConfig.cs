using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>game_config.json（settings.js の CONFIG から変換）。</summary>
    public sealed class GameConfig
    {
        public Payouts payouts;
        public Payouts payouts_BB;
        public Payouts payouts_RB;
        public Timings timings;
        public string[][] reelStrips;
        public Dictionary<string, Dictionary<string, int>> probabilities_A;
        public Dictionary<string, Dictionary<string, int>> probabilities_B;
        public Dictionary<string, Dictionary<string, int>> probabilities_C;
        public Dictionary<string, Dictionary<string, int>> probabilities_D;
        public Dictionary<string, Dictionary<string, int>> probabilities_BB;
        public Dictionary<string, Dictionary<string, int>> probabilities_RB;
        public Dictionary<string, int> probabilities_Tier2;
        public Dictionary<string, int> ceilings;
        public Dictionary<string, Dictionary<string, Dictionary<string, int>>> modeTransitions;
        public int tier2MaxSpins = 3;
        /// <summary>ENEMY 当選から敵が出現するまでの前兆G数（このG数目の終わりに出現）。</summary>
        public int enemyPrecursorSpins = 3;
        public int expPerDefeat = 50;
        public HintConfig defaultHintConfig;
        /// <summary>敵エンゲージ中の段階示唆（案B）。null なら既定値。</summary>
        public EngageHintConfig engageHints;
        /// <summary>ベル択ナビ（ATTACK / GUARD）の設定。</summary>
        public BellCommandConfig bellCommand = new BellCommandConfig();
        /// <summary>通常時に通り過ぎる旅人（モード示唆）。null なら既定。</summary>
        public TravelerConfig travelers;

        public Payouts PayoutsFor(BonusMode m)
        {
            if (m == BonusMode.BB && payouts_BB != null) return payouts_BB;
            if (m == BonusMode.RB && payouts_RB != null) return payouts_RB;
            return payouts;
        }

        public Dictionary<string, Dictionary<string, int>> ProbabilitiesFor(Mode m)
        {
            switch (m)
            {
                case Mode.B: return probabilities_B ?? probabilities_A;
                case Mode.C: return probabilities_C ?? probabilities_A;
                case Mode.D: return probabilities_D ?? probabilities_A;
                default: return probabilities_A;
            }
        }

        public int CeilingFor(Mode m)
        {
            if (ceilings != null && ceilings.TryGetValue(m.ToString(), out var c)) return c;
            return 999;
        }

        /// <summary>reelStrips を Symbol 配列に変換。</summary>
        public Symbol[][] ReelSymbols()
        {
            var r = new Symbol[reelStrips.Length][];
            for (int i = 0; i < reelStrips.Length; i++)
            {
                r[i] = new Symbol[reelStrips[i].Length];
                for (int k = 0; k < reelStrips[i].Length; k++)
                    r[i][k] = (Symbol)System.Enum.Parse(typeof(Symbol), reelStrips[i][k]);
            }
            return r;
        }
    }

    /// <summary>ベル択ナビ（第一停止=中、残り2つのどちらかが正解の 2 択）。</summary>
    public sealed class BellCommandConfig
    {
        /// <summary>正解で討伐が内部確定する（告知は3G目）。</summary>
        public bool successGuaranteesDefeat = true;
        /// <summary>正解時の EXP ボーナス。</summary>
        public int successExp = 25;
        /// <summary>失敗時にプレイヤーが受けるペナルティ（今は演出のみ。将来 HP 等に使う）。</summary>
        public int failPenalty = 0;
    }

    public sealed class Payouts
    {
        public int BIG, REG, STAR, WATERMELON, CHERRY;
        public int REPLAY = 3;
    }

    public sealed class Timings
    {
        public int reel1, reel2, reel3, next, nextWin;
    }

    public sealed class HintConfig
    {
        public int appearanceRate = 70;
        public HintDistribution distribution = new HintDistribution();
    }

    public sealed class HintDistribution
    {
        public int redGlow = 40, textOnly = 30, both = 30;
    }

    /// <summary>workflow_config.json。役キー → カテゴリ設定。</summary>
    public sealed class WorkflowRole
    {
        public WorkflowCategory SERIF, ENEMY, ZONE, ACTION;
        public int NONE;

        public WorkflowCategory Get(string cat)
        {
            switch (cat)
            {
                case "SERIF": return SERIF;
                case "ENEMY": return ENEMY;
                case "ZONE": return ZONE;
                case "ACTION": return ACTION;
                default: return null;
            }
        }
    }

    public sealed class WorkflowCategory
    {
        public int rate;
        public int A, B, C, D, E, F;
        public int NONE;

        public int Variant(char v)
        {
            switch (v)
            {
                case 'A': return A; case 'B': return B; case 'C': return C;
                case 'D': return D; case 'E': return E; case 'F': return F;
                default: return 0;
            }
        }
    }

    /// <summary>enemy_tables.json。</summary>
    public sealed class EnemyTableSet
    {
        public List<EnemyTable> tables = new List<EnemyTable>();
    }

    public sealed class EnemyTable
    {
        public string id;
        public string name;
        public string enemyType;
        /// <summary>役名(BELL/CHERRY/WATERMELON/CHANCE/REPLAY) → 討伐率 %。</summary>
        public Dictionary<string, int> defeatProbabilities = new Dictionary<string, int>();
        public Dictionary<string, int> tier2Probabilities;
        /// <summary>null / "ANY" は指定なし。</summary>
        public string variant;
        public HintConfig hintConfig;
    }
}
