using System;

namespace BBB.Core
{
    /// <summary>
    /// レベルアップで配られるポイントを振り分ける 3 系統。
    /// ライフ = 延命（エンバーの持ち）、テクニック = 戦闘、ラック = 引き。
    /// 効果はすべて game_config.json の stats から読む（1 ポイントあたりの値）。
    /// </summary>
    public sealed class PlayerStats
    {
        public int Life;
        public int Technique;
        public int Luck;
        /// <summary>まだ振っていないポイント。</summary>
        public int Unspent;

        public int Total => Life + Technique + Luck;

        public int Get(string key)
        {
            switch (key)
            {
                case "life": return Life;
                case "technique": return Technique;
                case "luck": return Luck;
                default: return 0;
            }
        }

        public void Set(string key, int v)
        {
            switch (key)
            {
                case "life": Life = v; break;
                case "technique": Technique = v; break;
                case "luck": Luck = v; break;
            }
        }

        public void Clear() { Life = Technique = Luck = Unspent = 0; }
    }

    /// <summary>ライフ（延命）の 1 ポイントあたりの効き。</summary>
    [Serializable]
    public sealed class LifeStatConfig
    {
        /// <summary>BET でエンバーを使わずに済む率 %。</summary>
        public float freeBetRate = 0.6f;
        /// <summary>力尽きたときに補填されるエンバー。</summary>
        public float rescueBonus = 10f;
        /// <summary>回復薬 1 個ぶんのライフ。</summary>
        public float torchSpins = 0.6f;
    }

    /// <summary>テクニック（戦闘）の 1 ポイントあたりの効き。</summary>
    [Serializable]
    public sealed class TechniqueStatConfig
    {
        /// <summary>エンゲージのG数（小数は切り捨てて反映）。</summary>
        public float engageSpins = 0.12f;
        /// <summary>討伐率 +%。</summary>
        public float defeatBonus = 1.4f;
        /// <summary>狩猟で与えるダメージ +%。</summary>
        public float battleDamage = 2.5f;
    }

    /// <summary>ラック（引き）の 1 ポイントあたりの効き。通常時のハズレだけが対象。</summary>
    [Serializable]
    public sealed class LuckStatConfig
    {
        /// <summary>ハズレがレア役（チェリー・スイカ・チャンス目）に化ける率 %。</summary>
        public float rareRate = 0.35f;
        /// <summary>ハズレがリプレイに化ける率 %。</summary>
        public float replayRate = 0.5f;
        /// <summary>宝の発見率 +%。</summary>
        public float treasureBonus = 0.8f;
    }

    [Serializable]
    public sealed class StatsConfig
    {
        /// <summary>ステータスを使うか。</summary>
        public bool enabled = true;
        /// <summary>レベルアップ 1 回で配るポイント。</summary>
        public int pointsPerLevel = 2;
        /// <summary>1 系統に振れる上限。</summary>
        public int maxPerStat = 20;
        /// <summary>章クリアのときだけ無料で振り直せる。</summary>
        public bool freeRespecOnChapterClear = true;
        /// <summary>それ以外で振り直すときのソウル。</summary>
        public int respecCost = 200;
        public LifeStatConfig life = new LifeStatConfig();
        public TechniqueStatConfig technique = new TechniqueStatConfig();
        public LuckStatConfig luck = new LuckStatConfig();
    }

    public static class StatsDirector
    {
        public static readonly string[] Keys = { "life", "technique", "luck" };

        public static string DisplayName(string key)
        {
            switch (key)
            {
                case "life": return "ライフ";
                case "technique": return "テクニック";
                case "luck": return "ラック";
                default: return key;
            }
        }

        public static string Summary(string key)
        {
            switch (key)
            {
                case "life": return "長く潜れる。ライフの持ちとエンバーの減り";
                case "technique": return "戦える。エンゲージのG数と討伐率";
                case "luck": return "引きが強くなる。レア役とリプレイ";
                default: return "";
            }
        }

        /// <summary>その系統を 1 上げると何が変わるかの説明（今の値 → 次の値）。</summary>
        public static string Describe(StatsConfig cfg, string key, int cur)
        {
            cfg = cfg ?? new StatsConfig();
            int nxt = cur + 1;
            switch (key)
            {
                case "life":
                    return $"BET 無料 {cur * cfg.life.freeBetRate:F1}% → {nxt * cfg.life.freeBetRate:F1}%   "
                         + $"回復薬 +{(int)(cur * cfg.life.torchSpins)} → +{(int)(nxt * cfg.life.torchSpins)}";
                case "technique":
                    return $"討伐率 +{cur * cfg.technique.defeatBonus:F0}% → +{nxt * cfg.technique.defeatBonus:F0}%   "
                         + $"エンゲージ +{(int)(cur * cfg.technique.engageSpins)}G → +{(int)(nxt * cfg.technique.engageSpins)}G";
                case "luck":
                    return $"レア役 +{cur * cfg.luck.rareRate:F1}% → +{nxt * cfg.luck.rareRate:F1}%   "
                         + $"リプレイ +{cur * cfg.luck.replayRate:F1}% → +{nxt * cfg.luck.replayRate:F1}%";
                default: return "";
            }
        }

        /// <summary>今の効果を一覧にする（ステータス画面の右側）。</summary>
        public static string Effects(StatsConfig cfg, PlayerStats st, string key)
        {
            cfg = cfg ?? new StatsConfig();
            st = st ?? new PlayerStats();
            switch (key)
            {
                case "life":
                    return $"BET が無料になる  {st.Life * cfg.life.freeBetRate:F1}%\n"
                         + $"回復薬 1 個の効き  +{(int)(st.Life * cfg.life.torchSpins)}\n"
                         + $"力尽きたときの補填 +{(int)(st.Life * cfg.life.rescueBonus)}";
                case "technique":
                    return $"エンゲージのG数   +{(int)(st.Technique * cfg.technique.engageSpins)}G\n"
                         + $"討伐率            +{st.Technique * cfg.technique.defeatBonus:F0}%\n"
                         + $"狩猟のダメージ    +{st.Technique * cfg.technique.battleDamage:F0}%";
                case "luck":
                    return $"レア役に化ける    {st.Luck * cfg.luck.rareRate:F1}%\n"
                         + $"リプレイに化ける  {st.Luck * cfg.luck.replayRate:F1}%\n"
                         + $"宝の発見率        +{st.Luck * cfg.luck.treasureBonus:F1}%";
                default: return "";
            }
        }

        /// <summary>1 ポイント振る。振れたら true。</summary>
        public static bool Spend(StatsConfig cfg, PlayerStats st, string key)
        {
            cfg = cfg ?? new StatsConfig();
            if (st == null || st.Unspent <= 0) return false;
            int cur = st.Get(key);
            if (cur >= Math.Max(1, cfg.maxPerStat)) return false;
            st.Set(key, cur + 1);
            st.Unspent--;
            return true;
        }

        /// <summary>全部戻す（振り直し）。戻したポイント数を返す。</summary>
        public static int Respec(PlayerStats st)
        {
            if (st == null) return 0;
            int back = st.Total;
            st.Unspent += back;
            st.Life = st.Technique = st.Luck = 0;
            return back;
        }
    }
}
