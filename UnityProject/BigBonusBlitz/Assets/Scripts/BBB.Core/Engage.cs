using System;
using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>役の 3 分類（エンゲージの判定はこれだけで決まる）。</summary>
    public enum EngageRole { Lose, Small, Rare }

    /// <summary>1 ターン目で決まる構え。</summary>
    public enum EngageStance { None, EnemyAttack, HeroCharge, HeroAttack }

    /// <summary>2 ターン目（と ポーションの回転）の結果。</summary>
    public enum EngageOutcome
    {
        None,
        Stance,          // 1 ターン目: 構えが決まっただけ
        Hit,             // 敵の攻撃を喰らった（LIFE が減る）
        Guard,           // 防御（減らない）
        Dodge,           // 回避
        Counter,         // 回避してカウンター（攻撃）
        Attack,          // 主人公の攻撃（attackSize が 小 / 中 / 大）
        PotionHeal,      // ルーレット: LIFE 回復
        PotionLarge,     // ルーレット: 次の攻撃が大攻撃
        PotionDefeat     // ルーレット: 討伐確定
    }

    public enum AttackSize { None, Small, Medium, Large, Counter }

    /// <summary>
    /// ENEMY エンゲージの数値と文（game_config.json の engage）。
    /// 1 セット = 2 ターン（2 G）。1 ターン目で構え、2 ターン目で結果。sets セット倒せなければ敵は逃げる。
    /// </summary>
    public sealed class EngageConfig
    {
        /// <summary>セット数（1 セット 2 G）。</summary>
        public int sets = 3;
        /// <summary>敵の攻撃を喰らったときに減る LIFE（回転数）。</summary>
        public int lifeDamage = 3;
        /// <summary>攻撃の大きさごとの討伐率（%）。装備・技能の上乗せは別に足す。</summary>
        public int defeatSmall = 15, defeatMedium = 35, defeatLarge = 70, defeatCounter = 50;
        /// <summary>「力を貯める」の 2 ターン目にハズレたときの内訳（重み）: 被弾 / 防御 / 回避。</summary>
        public int chargeLoseHit = 70, chargeLoseGuard = 15, chargeLoseDodge = 15;
        /// <summary>1 ターン目のレア役でポーション（次の G が追加の 1 回転 = ルーレット）。</summary>
        public bool rareGivesPotion = true;
        /// <summary>ルーレットの回復量（ハズレ）。</summary>
        public int potionHeal = 3;
        /// <summary>ベル択ナビの正解をレア役扱いにするか（false なら小役）。</summary>
        public bool naviSuccessIsRare = true;
        /// <summary>帯の文（{set} {sets} {turn} が入る）。</summary>
        public EngageTexts texts = new EngageTexts();
    }

    public sealed class EngageTexts
    {
        public string turn1 = "SET {set}/{sets}  ハズレ→敵の攻撃  小役→力を貯める  レア役→攻撃確定＋ポーション";
        public string enemyAttack = "SET {set}/{sets}  敵の攻撃！  ハズレ→被弾  小役→回避  レア役→回避してカウンター";
        public string heroCharge = "SET {set}/{sets}  力を貯めた！  小役→中攻撃  レア役→大攻撃  ハズレ→被弾（たまに防御・回避）";
        public string heroAttack = "SET {set}/{sets}  攻撃確定！  小役以下→中攻撃  レア役→大攻撃";
        public string potion = "ポーション！  ハズレ→LIFE 回復  小役→次が大攻撃  レア役→討伐確定";
        public string hit = "敵の攻撃！  LIFE −{dmg}";
        public string guard = "防御！";
        public string dodge = "回避！";
        public string counter = "カウンター！";
        public string attackSmall = "小攻撃！";
        public string attackMedium = "中攻撃！";
        public string attackLarge = "大攻撃！！";
        public string charge = "力を貯める……";
        public string potionGet = "ポーション GET!";
        public string potionHeal = "LIFE +{heal}";
        public string potionLarge = "次は大攻撃！";
        public string potionDefeat = "討伐確定！！";
        public string escaped = "敵は逃げた……";
    }

    /// <summary>この G のエンゲージで起きたこと（GameResult.engage）。</summary>
    public sealed class EngageStep
    {
        public int set, sets, turn;
        public EngageRole role;
        public EngageStance stance;
        public EngageOutcome outcome;
        public AttackSize attackSize;
        /// <summary>被弾で減った LIFE。</summary>
        public int damage;
        /// <summary>攻撃したときの討伐率（%）と、倒せたか。</summary>
        public int defeatPercent; public bool defeated;
        /// <summary>この G でポーションを得た / この G がポーションの回転だった。</summary>
        public bool potionGot, potionSpin;
        /// <summary>ルーレットの回復量。</summary>
        public int healed;
    }

    /// <summary>エンゲージの判定（乱数以外は純粋関数。テストしやすいように SlotMachine から切り出し）。</summary>
    public static class EngageBattle
    {
        /// <summary>役の 3 分類。ベルの択ナビは success で分ける（失敗はこぼれてハズレ）。</summary>
        public static EngageRole RoleOf(WinType win, bool naviSuccess, bool naviFail, EngageConfig cfg)
        {
            if (naviFail) return EngageRole.Lose;
            switch (win)
            {
                case WinType.CHERRY:
                case WinType.WATERMELON:
                case WinType.CHANCE:
                case WinType.BIG:
                case WinType.REG:
                    return EngageRole.Rare;
                case WinType.BELL:
                    return naviSuccess && (cfg?.naviSuccessIsRare ?? true) ? EngageRole.Rare : EngageRole.Small;
                case WinType.REPLAY:
                    return EngageRole.Small;
                default:
                    return EngageRole.Lose;
            }
        }

        /// <summary>1 ターン目: 役 → 構え。</summary>
        public static EngageStance StanceOf(EngageRole role)
        {
            switch (role)
            {
                case EngageRole.Rare: return EngageStance.HeroAttack;
                case EngageRole.Small: return EngageStance.HeroCharge;
                default: return EngageStance.EnemyAttack;
            }
        }

        /// <summary>2 ターン目: 構え × 役 → 結果と攻撃の大きさ。「力を貯める」のハズレだけ乱数。</summary>
        public static EngageOutcome Resolve(EngageStance stance, EngageRole role, EngageConfig cfg, IRandom rng, out AttackSize size)
        {
            size = AttackSize.None;
            switch (stance)
            {
                case EngageStance.EnemyAttack:
                    if (role == EngageRole.Lose) return EngageOutcome.Hit;
                    if (role == EngageRole.Small) return EngageOutcome.Dodge;
                    size = AttackSize.Counter; return EngageOutcome.Counter;
                case EngageStance.HeroCharge:
                    if (role == EngageRole.Small) { size = AttackSize.Medium; return EngageOutcome.Attack; }
                    if (role == EngageRole.Rare) { size = AttackSize.Large; return EngageOutcome.Attack; }
                    {
                        int h = Math.Max(0, cfg.chargeLoseHit), g = Math.Max(0, cfg.chargeLoseGuard), d = Math.Max(0, cfg.chargeLoseDodge);
                        int total = h + g + d; if (total <= 0) return EngageOutcome.Hit;
                        int r = rng.Next(total);
                        if (r < h) return EngageOutcome.Hit;
                        if (r < h + g) return EngageOutcome.Guard;
                        return EngageOutcome.Dodge;
                    }
                case EngageStance.HeroAttack:
                    size = role == EngageRole.Rare ? AttackSize.Large : AttackSize.Medium; return EngageOutcome.Attack;
                default:
                    return EngageOutcome.None;
            }
        }

        /// <summary>攻撃の大きさ → 討伐率（%）。</summary>
        public static int DefeatPercent(AttackSize size, EngageConfig cfg, int bonus = 0)
        {
            int p;
            switch (size)
            {
                case AttackSize.Small: p = cfg.defeatSmall; break;
                case AttackSize.Medium: p = cfg.defeatMedium; break;
                case AttackSize.Large: p = cfg.defeatLarge; break;
                case AttackSize.Counter: p = cfg.defeatCounter; break;
                default: return 0;
            }
            return Math.Max(0, Math.Min(100, p + bonus));
        }

        /// <summary>ポーションの回転: 役 → ルーレットの結果。</summary>
        public static EngageOutcome Roulette(EngageRole role)
        {
            switch (role)
            {
                case EngageRole.Rare: return EngageOutcome.PotionDefeat;
                case EngageRole.Small: return EngageOutcome.PotionLarge;
                default: return EngageOutcome.PotionHeal;
            }
        }

        /// <summary>帯の文に {set} などを入れる。</summary>
        public static string Fill(string s, int set, int sets, int turn, int dmg = 0, int heal = 0)
            => (s ?? "").Replace("{set}", set.ToString()).Replace("{sets}", sets.ToString()).Replace("{turn}", turn.ToString())
                        .Replace("{dmg}", dmg.ToString()).Replace("{heal}", heal.ToString());
    }
}
