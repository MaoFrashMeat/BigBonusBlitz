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
        /// <summary>AT（洞窟）中の小役確率。null なら Tier2 → モードA の順で代用。</summary>
        public Dictionary<string, int> probabilities_AT;
        public Dictionary<string, int> ceilings;
        public Dictionary<string, Dictionary<string, Dictionary<string, int>>> modeTransitions;
        public int tier2MaxSpins = 3;
        /// <summary>持ち越しボーナスを狙うゲームでの最大滑りコマ数（20 = どこで押しても引き込む。実機どおりの目押しにするなら 4）。</summary>
        public int bonusPullInSlip = 20;
        /// <summary>
        /// ボーナス成立から擬似遊技で揃えるまでの前兆G数の重み（G数 → 重み）。
        /// 最後の 1G が擬似遊技（機械が自動で揃える）。前兆中は引き込みを切るので、
        /// 察した人は目押しで前兆を待たずに揃えられる。
        /// </summary>
        public Dictionary<string, int> bonusPrecursorSpins = new Dictionary<string, int> { ["3"] = 30, ["4"] = 30, ["5"] = 25, ["6"] = 15 };
        /// <summary>天井到達時に成立させるボーナスの重み（フラグ名 → 重み）。</summary>
        public Dictionary<string, int> ceilingBonus = new Dictionary<string, int> { ["BB_A"] = 50, ["RB_A"] = 50 };
        /// <summary>ENEMY 当選から敵が出現するまでの前兆G数（このG数目の終わりに出現）。</summary>
        public int enemyPrecursorSpins = 3;
        /// <summary>Tier2中に小役を連続で引くごとに討伐率へ加算する %（2連目 +1倍、3連目 +2倍）。ハズレでリセット。</summary>
        public int defeatStreakBonus = 15;
        public int expPerDefeat = 50;
        public HintConfig defaultHintConfig;
        /// <summary>敵エンゲージ中の段階示唆（案B）。null なら既定値。</summary>
        public EngageHintConfig engageHints;
        /// <summary>ベル択ナビ（ATTACK / GUARD）の設定。</summary>
        public BellCommandConfig bellCommand = new BellCommandConfig();
        /// <summary>通常時に通り過ぎる旅人（モード示唆）。null なら既定。</summary>
        public TravelerConfig travelers;
        /// <summary>技術介入（ビタ押し・2コマ目押し・ミッション）。null なら既定。</summary>
        public TechConfig tech;
        /// <summary>エンバー（補給の通貨）の入手量。</summary>
        public EmberConfig embers = new EmberConfig();
        /// <summary>事前察知（レバーオン時の役予告）。null なら既定。</summary>
        public PrecogConfig precog;
        /// <summary>主人公のひとりごと。null なら既定。</summary>
        public HeroConfig hero;
        /// <summary>ボーナス中の AT 期待度（枠の点滅色）。null なら既定。</summary>
        public AtExpectConfig atExpect;
        /// <summary>AT「洞窟」の設定。null なら既定。</summary>
        public AtConfig at;
        /// <summary>討伐で手に入るソウルの量。null なら既定。</summary>
        public SoulConfig souls;
        /// <summary>街のショップの品揃え。null なら空。</summary>
        public ShopConfig shop;
        /// <summary>冒険（ステージ制マップ）。null / enabled=false なら従来通り。</summary>
        public AdventureConfig adventure;
        /// <summary>レベルアップで振るステータス（ライフ / テクニック / ラック）。</summary>
        public StatsConfig stats;
        /// <summary>物語（章ごと・段ごと・枝の高さごとの台詞）。</summary>
        public StoryConfig story;
        /// <summary>装備のドロップ（潜行ごとに拾い直す）。</summary>
        public EquipConfig equipment;
        /// <summary>呪いと祝福。</summary>
        public CurseConfig curse;

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
        /// <summary>択の正解が「左」になる確率 %（残りは右）。</summary>
        public int correctLeftRate = 50;
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
        /// <summary>"normal" = 通常時の雑魚、"boss" = ボーナス中の中ボス。未指定は normal。</summary>
        public string group = "normal";
        /// <summary>見た目の大きさ倍率（中ボスは大きく見せる）。</summary>
        public float scale = 1f;
        /// <summary>基本の色（HTML カラー）。空なら白。示唆の色が乗るときはそちらが優先。</summary>
        public string color;
        /// <summary>討伐時の EXP。0 なら game_config の expPerDefeat を使う。</summary>
        public int expOnDefeat;

        public bool IsBoss => string.Equals(group, "boss", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>エンバーの入手量。ソウルと同じ場面で、別の量が入る。</summary>
    public sealed class EmberConfig
    {
        /// <summary>はじめから始めたときの所持。</summary>
        public int start = 500;
        /// <summary>雑魚の討伐。</summary>
        public int perMob = 6;
        /// <summary>ボスの討伐。</summary>
        public int perBoss = 40;
        /// <summary>AT バトルの討伐。</summary>
        public int perAtBattle = 15;
        /// <summary>逃した敵。</summary>
        public int perEscape = 2;
        /// <summary>章クリア。</summary>
        public int chapterClear = 120;
        /// <summary>ステージに初めて着いたとき。</summary>
        public int firstVisit = 10;
        /// <summary>ボーナス 1 回の終了時。</summary>
        public int perBonus = 12;
    }

}
