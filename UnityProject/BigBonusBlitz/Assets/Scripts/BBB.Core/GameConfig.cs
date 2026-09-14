using System;
using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>
    /// ボーナス中のベルの払い出し。AT と同じく「押し順ナビ」で差を付ける。
    /// ナビが出たベルは従えば naviCorrectPayout、外せば naviWrongPayout。
    /// ナビが出ないベルは共通ベルとして commonBellPayout。
    /// </summary>
    [Serializable]
    public sealed class BonusBellConfig
    {
        /// <summary>押し順ナビが出る率 %。</summary>
        public int naviRate = 60;
        /// <summary>ナビに従ったときの払い出し。</summary>
        public int naviCorrectPayout = 11;
        /// <summary>ナビを外したときの払い出し（こぼし）。</summary>
        public int naviWrongPayout = 3;
        /// <summary>ナビの出ないベル（共通ベル）の払い出し。</summary>
        public int commonBellPayout = 8;
    }

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
        /// <summary>ボーナス中のベル。押し順ナビが出れば従って多め、出なければ共通ベルで少なめ。</summary>
        public BonusBellConfig bonusBell = new BonusBellConfig();
        /// <summary>
        /// ボーナスの G 数（BIG / REG）。このG数を消化したら、規定枚数に届いていなくても終わる。
        /// 規定枚数に先に届けばそこで終わる。0 なら G 数では終わらない（枚数だけ）。
        /// </summary>
        public Dictionary<string, int> bonusGames = new Dictionary<string, int> { ["BIG"] = 60, ["REG"] = 30 };
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
        /// <summary>敵・ボス・狩猟・宝箱から何が落ちるか（装備の率と、装備以外の落とし物）。</summary>
        public DropConfig drops;
        /// <summary>呪いと祝福。</summary>
        public CurseConfig curse;
        /// <summary>リールの見せ方（役が決まったコマの点滅）。tools/symbol_viewer.html で試して決める。</summary>
        public ReelFxConfig reelFx = new ReelFxConfig();

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
    /// <summary>「n EMB 獲得！」の帯（ベルの払い出し）。tools/fx_viewer.html で試して決める。</summary>
    public sealed class EmberGainFxConfig
    {
        /// <summary>右から滑り込む秒数 / 止まっている秒数 / 左へ抜ける秒数。</summary>
        public float inSeconds = 0.24f, holdSeconds = 0.55f, outSeconds = 0.26f;
        /// <summary>帯の位置（表示域の中央からの高さ）と大きさ。</summary>
        public float y = -8f, bandW = 360f, bandH = 64f;
        /// <summary>文字の大きさ（数字の絵が無いときの文字と、炎の絵の基準）と、「獲得」の絵の高さ、数字の絵の高さ。</summary>
        public int fontSize = 40;
        public float picH = 52f, numH = 56f;
        /// <summary>炎の絵と「獲得」の絵のずらし（px。右が +、上が +）。数字との並びや詰まり具合を合わせる用。</summary>
        public float iconX = 0f, iconY = 0f, picX = 0f, picY = 0f;
        /// <summary>詰め: 桁と桁の間 / 数字と炎の間 / 炎と「獲得」の間（px。負で重ねる）。傾き: 数字 / 炎 / 「獲得」（度、反時計回り+）。</summary>
        public float digitGap = 0f, iconGap = 6f, picGap = 10f, numRot = 0f, iconRot = 0f, picRot = 0f;
        /// <summary>文字の後ろに大きな炎の絵を敷く。大きさ（px）/ 濃さ（0〜1）/ ずらし（右+ 上+）/ 傾き（度、反時計回り+）/ 回る速さ（度/秒、0 で止まる）。</summary>
        public bool backIcon = false;
        public float backIconSize = 120f, backIconAlpha = 0.45f, backIconX = 0f, backIconY = 0f, backIconRot = 0f, backIconSpin = 0f;
        /// <summary>部品ごとの ON/OFF: 黒い帯 / 上下の線 / 後ろの光 / 止まっている間の震え / 止まった瞬間の火花 / 数字の右の炎（手前）。</summary>
        public bool showBand = false, showEdges = false, showGlow = true, shake = true, sparks = true, showIcon = true;
        /// <summary>
        /// 数字が 0 から増えていく。どの型にも足せる追加の効果。
        /// 画面で止まった瞬間を 0 として countOffset 秒後に数え始め、countSeconds 秒かけて枚数に届く（負なら止まる前から）。
        /// </summary>
        public bool countUp = false;
        public float countOffset = 0f, countSeconds = 0.3f;
        /// <summary>
        /// 足せる効果（どの型にも重なる）: 落ち影（ずれ x/y px、濃さ 0〜1）/ 縁取り（太さ px、濃さ）/
        /// 桁が順にポンと出る / 止まっている間、桁がゆらゆら揺れる（角度、速さ）/ 後ろの光が脈打つ。
        /// </summary>
        public bool shadow = false;
        public float shadowX = 3f, shadowY = -3f, shadowAlpha = 0.6f;
        public bool outline = false;
        public float outlineSize = 2f, outlineAlpha = 0.8f;
        public bool digitBounce = false, wobble = false, glowPulse = false;
        public float wobbleDeg = 6f, wobbleSpeed = 8f;
        /// <summary>
        /// 同じ G で複数（技術介入のソウル・EXP・エンバー・G 数など）得たときは縦に並べて一気に出す:
        /// 並びの中心の高さ（px。1 本のときの y とは別）/ 行の間隔（px）/ 行ごとの遅れ（秒、0 で完全に同時）/
        /// 縦に並べるときの帯の大きさ（1 で 1 本のときと同じ）/ 表示域（上下 300px）に収まらないときはさらに縮める。
        /// </summary>
        public float stackY = 0f, stackGap = 100f, stackStagger = 0.05f, stackScale = 0.7f;
        public bool stackFit = true;
        /// <summary>セリフ（説明の吹き出し）が出ているときは、その枠より上に出す（枠の上端からの余白 px）。</summary>
        public bool aboveDialogue = true;
        public float dialogueMargin = 8f;
        /// <summary>
        /// 動きの型。slideRL（右→左）/ slideLR（左→右）/ pop（中央でポン）/ drop（上から落ちて弾む）/ rise（下からふわっと）/
        /// zoom（急拡大して消える）/ flip（縦に開く）/ slam（叩きつけ）/ spiral（回りながら収まる）/ pulse（明滅）
        /// </summary>
        public string style = "slideRL";
    }

    public sealed class ReelFxConfig
    {
        /// <summary>
        /// 揃ったコマの見せ方。blink（暗↔明）/ pulse（拡大縮小）/ bounce（跳ねる）/ wobble（左右に傾く）/ shake（小刻み）/
        /// glow（役色の光を脈打たせる）/ flash（白く光る）/ rainbow（色相を回す）/ pop（最初に大きく膨らんでから点滅）。
        /// tools/symbol_viewer.html の「役の点滅」で試して決める。
        /// </summary>
        public string style = "blink";
        /// <summary>動きの強さ（0〜1。拡大・跳ね・傾き・光の量に掛かる）。</summary>
        public float strength = 1f;
        /// <summary>揃っていないコマを blinkDim まで暗くして、揃ったコマを際立たせる。</summary>
        public bool dimOthers = false;
        /// <summary>
        /// 点滅の「消えている側」の見え方。dark = 黒へ暗くする（blinkDim）/ tint = blinkColor で染める（掛け算）/
        /// light = blinkColor を加算して明るく光らせる（実機のバックライトのよう）。
        /// </summary>
        public string blinkMode = "dark";
        /// <summary>tint / light の色（#rrggbb）。</summary>
        public string blinkColor = "#ffffff";
        /// <summary>
        /// 揃った図柄に重ねる層。none / add（加算）/ screen（スクリーン）/ multiply（乗算）。
        /// 図柄の形そのままに layerColor を layerAlpha で重ね、layerPulse なら周期で強弱を付ける。
        /// </summary>
        public string layerMode = "none";
        public string layerColor = "#ffcf3f";
        public float layerAlpha = 0.6f;
        public bool layerPulse = true;
        /// <summary>図柄の中のランプ（赤 7・白 7 など。停止中ずっと点いている）。</summary>
        public SymbolLampConfig lamp = new SymbolLampConfig();
        /// <summary>点滅の周期（秒。暗→明で 1 周期）。</summary>
        public float blinkPeriod = 0.13f;
        /// <summary>暗いときの明るさ（0〜1）。</summary>
        public float blinkDim = 0.28f;
        /// <summary>点滅を続ける秒数。</summary>
        public float blinkSeconds = 1.4f;
        /// <summary>ベルの「n EMB 獲得！」の帯。</summary>
        public EmberGainFxConfig emberGain = new EmberGainFxConfig();
    }

    /// <summary>
    /// 図柄の中にランプが入っているような見え方（実機のバックライト）。図柄の後ろに色の光、図柄の形に加算の光を重ね、
    /// pulse（Hz）でゆっくり脈打つ。symbols に入れた図柄だけに出す。colors は図柄名 → #rrggbb。
    /// </summary>
    public sealed class SymbolLampConfig
    {
        public bool enabled = true;
        public List<string> symbols = new List<string> { "RED7", "BLUE7" };
        public Dictionary<string, string> colors = new Dictionary<string, string> { { "RED7", "#ff4a4a" }, { "BLUE7", "#9fd8ff" } };
        /// <summary>後ろの光の強さ（0〜1）。</summary>
        public float intensity = 0.7f;
        /// <summary>図柄の形に重ねる加算の光の強さ（0〜1）。</summary>
        public float inner = 0.5f;
        /// <summary>後ろの光の広がり（コマの幅に対する倍率）。</summary>
        public float size = 1.4f;
        /// <summary>脈打つ速さ（Hz）。0 で一定。</summary>
        public float pulse = 1.2f;
        /// <summary>脈の深さ（0〜1。0.4 なら 60%〜100% を行き来）。</summary>
        public float pulseDepth = 0.4f;
        /// <summary>回転中も点ける。</summary>
        public bool whileSpinning = false;
    }

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
