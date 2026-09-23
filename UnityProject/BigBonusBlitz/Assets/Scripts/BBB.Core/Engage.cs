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
        PotionDefeat,    // ルーレット: 討伐確定
        Judge            // 3 セット後のジャッジ（削った HP の分だけ倒せる。defeated で結果）
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
        /// <summary>敵の HP（EnemyTable.engageHp が 0 のときの既定）。攻撃のダメージで削り、0 で討伐。</summary>
        public int enemyHp = 100;
        /// <summary>攻撃の大きさごとのダメージ。装備・技能の討伐率の上乗せ（%）はダメージにそのまま足す。</summary>
        public int damageSmall = 15, damageMedium = 35, damageLarge = 70, damageCounter = 50;
        /// <summary>「力を貯める」の 2 ターン目にハズレたときの内訳（重み）: 被弾 / 防御 / 回避。</summary>
        public int chargeLoseHit = 70, chargeLoseGuard = 15, chargeLoseDodge = 15;
        /// <summary>1 ターン目のレア役でポーション（次の G が追加の 1 回転 = ルーレット）。</summary>
        public bool rareGivesPotion = true;
        /// <summary>ルーレットの回復量（ハズレ）。</summary>
        public int potionHeal = 3;
        /// <summary>ベル択ナビの正解をレア役扱いにするか（false なら小役）。</summary>
        public bool naviSuccessIsRare = false;   // 本人 2026-09-23「EE 時ベルナビはただの小役」
        /// <summary>
        /// 全セットで HP を削り切れなかったときのジャッジ（追加の 1 G）。倒せる率 = 削った割合（%）× judgeRatioScale + 役の上乗せ。
        /// judgeMin 〜 judgeMax に収める。0 なら逃走確定。
        /// </summary>
        public bool judgeEnabled = true;
        public float judgeRatioScale = 1f;
        public int judgeSmallBonus = 10, judgeRareBonus = 40, judgeMin = 0, judgeMax = 95;
        /// <summary>帯の文（{set} {sets} {turn} が入る）。</summary>
        public EngageTexts texts = new EngageTexts();
        /// <summary>ジャッジの G の演出（BET でカットイン → 第一〜第三停止で段階的に。本人 2026-09-23）。</summary>
        public JudgeFxConfig judgeFx = new JudgeFxConfig();
    }

    /// <summary>
    /// ジャッジの G の演出。BET で「JUDGE」のカットイン、第一停止で前触れ、第二停止で熱さの帯（青 / 黄 / 赤 / 虹）、第三停止で決着。
    /// 熱さは結果で重みを変える（勝つときほど赤・虹が出やすい）。tools/fx_viewer.html「ジャッジの演出」。
    /// </summary>
    public sealed class JudgeFxConfig
    {
        public string cutInText = "JUDGE";
        public int cutInSize = 72;
        public string cutInColor = "#ffd23f";
        public float cutInHold = 0.7f;
        /// <summary>熱さの名前と色（段 0〜3）。</summary>
        public string[] heatTexts = { "チャンス", "好機！", "激熱！！", "とどめの好機！！！" };
        public string[] heatColors = { "#4da3ff", "#ffcf3f", "#ff5a5a", "#ff8ae2" };
        /// <summary>第一停止の一言（段ごと）。</summary>
        public string[] stop1Texts = { "……", "敵が怯んだ", "敵の足が止まった！", "灯が燃え上がる！" };
        /// <summary>熱さの重み（勝つとき / 負けるとき）。</summary>
        public int[] heatWeightsWin = { 10, 30, 40, 20 };
        public int[] heatWeightsLose = { 60, 30, 10, 0 };
        /// <summary>段ごとの揺れの強さ（第二停止）。</summary>
        public float[] heatShake = { 2f, 4f, 7f, 10f };
    }

    public sealed class EngageTexts
    {
        public string turn1 = "小役を引いて魔物を討伐しろ！";
        public string enemyAttack = "敵の攻撃が来る！ 小役でかわせ！";
        public string heroCharge = "力を貯めた！ 小役で斬れ！";
        public string heroAttack = "攻撃確定！ レア役で大ダメージ！";
        public string potion = "ポーション！ 役で効果が決まる！";
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
        public string judge = "JUDGE！ とどめを刺せ！";
        public string judgeWin = "とどめ！！";
        public string judgeLose = "……逃げられた";
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
        /// <summary>攻撃で与えたダメージ、残り HP と最大 HP、倒せたか。</summary>
        public int dealt, hpLeft, hpMax; public bool defeated;
        /// <summary>この G でポーションを得た / この G がポーションの回転だった。</summary>
        public bool potionGot, potionSpin;
        /// <summary>ルーレットの回復量。</summary>
        public int healed;
        /// <summary>この G がジャッジだった / その率（%）。</summary>
        public bool judgeSpin; public int judgePercent;
    }

    /// <summary>エンゲージの判定（乱数以外は純粋関数。テストしやすいように SlotMachine から切り出し）。</summary>
    public static class EngageBattle
    {
        /// <summary>
        /// 揃った結果から役の 3 分類。リプレイは WinResult.isReplay だけが立って winType は NONE のままなので、ここで REPLAY に読み替える
        /// （本人の指示 2026-09-21「EE ではリプレイも小役扱い」。読み替えないとハズレ = 敵の攻撃になっていた）。
        /// </summary>
        public static EngageRole RoleOf(WinResult win, bool naviSuccess, bool naviFail, EngageConfig cfg)
            => RoleOf(win.isReplay && win.winType == WinType.NONE ? WinType.REPLAY : win.winType, naviSuccess, naviFail, cfg);

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
                    return naviSuccess && (cfg?.naviSuccessIsRare ?? false) ? EngageRole.Rare : EngageRole.Small;
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
        public static EngageOutcome Resolve(EngageStance stance, EngageRole role, EngageConfig cfg, IRandom rng, out AttackSize size, int guardBonus = 0, int dodgeBonus = 0)
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
                        int h = Math.Max(0, cfg.chargeLoseHit), g = Math.Max(0, cfg.chargeLoseGuard + guardBonus), d = Math.Max(0, cfg.chargeLoseDodge + dodgeBonus);
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

        /// <summary>攻撃の大きさ → ダメージ（bonus は装備・技能の上乗せ）。</summary>
        public static int Damage(AttackSize size, EngageConfig cfg, int bonus = 0)
        {
            int p;
            switch (size)
            {
                case AttackSize.Small: p = cfg.damageSmall; break;
                case AttackSize.Medium: p = cfg.damageMedium; break;
                case AttackSize.Large: p = cfg.damageLarge; break;
                case AttackSize.Counter: p = cfg.damageCounter; break;
                default: return 0;
            }
            return Math.Max(0, p + bonus);
        }

        /// <summary>ジャッジの率（%）: 削った割合 × scale + 役の上乗せ。min〜max に収める。</summary>
        public static int JudgePercent(int hpLeft, int hpMax, EngageRole role, EngageConfig cfg, int bonus = 0)
        {
            float ratio = hpMax > 0 ? 1f - Math.Max(0, Math.Min(hpLeft, hpMax)) / (float)hpMax : 0f;
            int p = (int)Math.Round(ratio * 100f * cfg.judgeRatioScale) + (role == EngageRole.Rare ? cfg.judgeRareBonus : role == EngageRole.Small ? cfg.judgeSmallBonus : 0) + bonus;
            return Math.Max(Math.Max(0, cfg.judgeMin), Math.Min(Math.Min(100, cfg.judgeMax), p));
        }

        /// <summary>ポーションの回転: 役 → ルーレットの結果。smallDefeatPercent は装備で「小役でも討伐確定」になる率。</summary>
        public static EngageOutcome Roulette(EngageRole role, IRandom rng = null, int smallDefeatPercent = 0)
        {
            switch (role)
            {
                case EngageRole.Rare: return EngageOutcome.PotionDefeat;
                case EngageRole.Small: return rng != null && smallDefeatPercent > 0 && rng.NextDouble() * 100 < smallDefeatPercent ? EngageOutcome.PotionDefeat : EngageOutcome.PotionLarge;
                default: return EngageOutcome.PotionHeal;
            }
        }

        /// <summary>帯の文に {set} などを入れる。</summary>
        public static string Fill(string s, int set, int sets, int turn, int dmg = 0, int heal = 0)
            => (s ?? "").Replace("{set}", set.ToString()).Replace("{sets}", sets.ToString()).Replace("{turn}", turn.ToString())
                        .Replace("{dmg}", dmg.ToString()).Replace("{heal}", heal.ToString());
    }
}
