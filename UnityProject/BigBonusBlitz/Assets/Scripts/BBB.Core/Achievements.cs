using System;
using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>
    /// 実績（トロフィー）1 つ。counter の累計が target に届いたら解除。解除時に rewardSouls をもらう。
    /// 一覧は Resources/Data/achievements.json。数字はそちらで直す。
    /// </summary>
    [Serializable]
    public sealed class AchievementDef
    {
        public string id = "";
        public string name = "";
        public string desc = "";
        /// <summary>数えもののキー（AchievementCounters 参照）。</summary>
        public string counter = "";
        public long target = 1;
        public int rewardSouls;
        /// <summary>解除するまで中身を見せない。</summary>
        public bool hidden;
    }

    [Serializable]
    public sealed class AchievementFile
    {
        public List<AchievementDef> list = new List<AchievementDef>();
    }

    /// <summary>数えもののキー。累計（Add）と最高値（Max）がある。</summary>
    public static class AchievementCounters
    {
        public const string Spins = "spins";             // 回した回数
        public const string Big = "big";                 // BIG の回数
        public const string Reg = "reg";                 // REG の回数
        public const string At = "at";                   // 洞窟（AT）に入った回数
        public const string AtBest = "atBest";           // 1 回の洞窟の最長 G（最高値）
        public const string Chapters = "chapters";       // 章クリア
        public const string Defeats = "defeats";         // エンゲージの討伐
        public const string Hunts = "hunts";             // 狩猟の討伐
        public const string Treasures = "treasures";     // 宝
        public const string Drops = "drops";             // 拾った装備
        public const string LegendDrops = "legendDrops"; // 伝説の装備
        public const string NaviHits = "naviHits";       // 押し順ナビ正解
        public const string TechWins = "techWins";       // 技術介入の成功
        public const string LevelMax = "levelMax";       // 最高レベル（最高値）
        public const string DepthMax = "depthMax";       // 最深（最高値）
        public const string SoulsTotal = "soulsTotal";   // ソウルの累計
        public const string Deaths = "deaths";           // 力尽きた回数
        public const string WornMax = "wornMax";         // 同時に着けた最多（最高値）
    }

    /// <summary>実績の進み具合（プレイヤー単位で永続。潜行をまたいで残る）。</summary>
    public sealed class AchievementState
    {
        public readonly Dictionary<string, long> Counters = new Dictionary<string, long>();
        public readonly HashSet<string> Unlocked = new HashSet<string>();

        public long Get(string key) => Counters.TryGetValue(key, out var v) ? v : 0;
        public void Add(string key, long n) { if (n != 0) Counters[key] = Get(key) + n; }
        public void Max(string key, long v) { if (v > Get(key)) Counters[key] = v; }
        public void Clear() { Counters.Clear(); Unlocked.Clear(); }
    }

    public static class AchievementDirector
    {
        /// <summary>
        /// 1 回転の結果から数えものを進め、新しく解除した実績を返す（報酬のソウルはここで渡す）。
        /// 画面はこの戻り値で「実績解除」を出す。
        /// </summary>
        public static List<AchievementDef> Track(List<AchievementDef> defs, AchievementState st, GameResult r, SlotMachine m)
        {
            var unlocked = new List<AchievementDef>();
            if (st == null || r == null || m == null) return unlocked;

            st.Add(AchievementCounters.Spins, 1);
            if (r.bonusStarted) st.Add(r.bonusModeAfter == BonusMode.BB ? AchievementCounters.Big : AchievementCounters.Reg, 1);
            if (r.atStarted) st.Add(AchievementCounters.At, 1);
            if (r.atEnded) st.Max(AchievementCounters.AtBest, m.AtSpinCount);
            if (r.chapterCleared) st.Add(AchievementCounters.Chapters, 1);
            if (r.enemyResolved == true) st.Add(AchievementCounters.Defeats, 1);
            if (r.battleResolved == true) st.Add(AchievementCounters.Hunts, 1);
            if (r.treasure != null) st.Add(AchievementCounters.Treasures, 1);
            if (r.equipDropped != null && !r.equipBagFull)
            {
                st.Add(AchievementCounters.Drops, 1);
                // 「伝説」は id が legend の段（黒）以上。無ければ一番上の段
                var rar = m.Config.equipment?.rarities;
                if (rar != null && rar.Count > 0)
                {
                    int legend = rar.FindIndex(x => x != null && x.id == "legend");
                    if (legend < 0) legend = rar.Count - 1;
                    if (r.equipDropped.rarity >= legend) st.Add(AchievementCounters.LegendDrops, 1);
                }
            }
            if (r.naviCorrect == true) st.Add(AchievementCounters.NaviHits, 1);
            if (r.techSuccess) st.Add(AchievementCounters.TechWins, 1);
            if (r.soulsGained > 0) st.Add(AchievementCounters.SoulsTotal, r.soulsGained);
            if (r.returnedToTown && r.returnReason == "hp") st.Add(AchievementCounters.Deaths, 1);
            st.Max(AchievementCounters.LevelMax, m.PlayerLevel);
            st.Max(AchievementCounters.DepthMax, m.CurrentDepth);
            int worn = 0;
            foreach (var kv in m.Equip.Worn) if (kv.Value != null) worn++;
            st.Max(AchievementCounters.WornMax, worn);

            if (defs == null) return unlocked;
            foreach (var d in defs)
            {
                if (d == null || string.IsNullOrEmpty(d.id) || st.Unlocked.Contains(d.id)) continue;
                if (st.Get(d.counter) < Math.Max(1, d.target)) continue;
                st.Unlocked.Add(d.id);
                if (d.rewardSouls > 0) m.Wallet.Souls += d.rewardSouls;
                unlocked.Add(d);
            }
            return unlocked;
        }

        /// <summary>進み具合（0〜1）。</summary>
        public static float Progress(AchievementDef d, AchievementState st)
        {
            if (d == null || st == null || d.target <= 0) return 0f;
            if (st.Unlocked.Contains(d.id)) return 1f;
            return (float)Math.Min(1.0, st.Get(d.counter) / (double)d.target);
        }
    }
}
