using System;
using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>技術介入の種類。</summary>
    public enum TechKind
    {
        None,
        /// <summary>ビタ押し: 指定図柄を指定段にピタリ（滑り 0）で止める。</summary>
        Vita,
        /// <summary>2コマ目押し: 指定図柄を停止枠内（±2コマ相当）に入れる。</summary>
        TwoKoma,
    }

    /// <summary>1回分の課題。レバーオン時に決まり、第3停止で判定する。</summary>
    public struct TechChallenge
    {
        public TechKind kind;
        public string id;          // 難易度定義の id（報酬計算に使う）
        public int reel;           // 対象リール 0=左 1=中 2=右
        public Symbol symbol;      // 狙う図柄
        public int row;            // 狙う段 0=上 1=中 2=下（Vita のみ。TwoKoma は中段を目安に）
        /// <summary>狙い位置（この上段コマで押せば成功する）。デバッグ・自動検証用。</summary>
        public int aimIndex;
        public bool Active => kind != TechKind.None;
    }

    /// <summary>難易度と報酬。すべて game_config.json 側で調整する。</summary>
    public sealed class TechLevelDef
    {
        public string id = "";
        public string name = "";
        /// <summary>Vita / TwoKoma。</summary>
        public string kind = "TwoKoma";
        /// <summary>この難易度が選ばれる重み。</summary>
        public int weight = 10;
        /// <summary>狙う図柄（複数あればこの中から1つ）。</summary>
        public List<string> symbols = new List<string>();
        /// <summary>対象リール（空なら全リールから1つ）。</summary>
        public List<int> reels = new List<int>();
        /// <summary>成功報酬。失敗しても減らさない。</summary>
        public int souls, embers, exp, atGames;
    }

    /// <summary>場面ごとの発生率。オート中は出さない。</summary>
    public sealed class TechSceneRate
    {
        /// <summary>通常時 1G あたり %。</summary>
        public int normal = 4;
        /// <summary>敵エンゲージ中 %。</summary>
        public int engage = 12;
        /// <summary>ボーナス中 %。</summary>
        public int bonus = 8;
        /// <summary>AT（洞窟）中 %。</summary>
        public int at = 10;
    }

    public sealed class TechConfig
    {
        public bool enabled = true;
        public TechSceneRate rates = new TechSceneRate();
        public List<TechLevelDef> levels = new List<TechLevelDef>();
        /// <summary>ミッション（複数Gにまたがる課題）。</summary>
        public List<MissionDef> missions = new List<MissionDef>();
        /// <summary>同時に持てるミッション数。</summary>
        public int missionSlots = 1;

        public static TechConfig Default()
        {
            var c = new TechConfig();
            c.levels.Add(new TechLevelDef
            {
                id = "easy_star", name = "★を枠内に", kind = "TwoKoma", weight = 40,
                symbols = new List<string> { "STAR" }, souls = 8, embers = 4, exp = 5,
            });
            c.levels.Add(new TechLevelDef
            {
                id = "mid_melon", name = "スイカを枠内に", kind = "TwoKoma", weight = 30,
                symbols = new List<string> { "WATERMELON" }, souls = 16, embers = 8, exp = 10,
            });
            c.levels.Add(new TechLevelDef
            {
                id = "hard_bar", name = "BAR をビタ押し", kind = "Vita", weight = 20,
                symbols = new List<string> { "BAR" }, souls = 45, embers = 25, exp = 30, atGames = 5,
            });
            c.levels.Add(new TechLevelDef
            {
                id = "extreme_seven", name = "赤7 を中段ビタ", kind = "Vita", weight = 10,
                symbols = new List<string> { "RED7" }, reels = new List<int> { 0 },
                souls = 90, embers = 50, exp = 60, atGames = 10,
            });
            c.missions.Add(new MissionDef
            {
                id = "m_bar3", name = "BAR を枠内に 3 回", type = "techSuccess", target = 3,
                filterId = "hard_bar", souls = 120, embers = 60, exp = 80,
            });
            c.missions.Add(new MissionDef
            {
                id = "m_cherry10", name = "チェリーを 10 回", type = "win", target = 10,
                filterId = "CHERRY", souls = 60, embers = 40, exp = 40,
            });
            c.missions.Add(new MissionDef
            {
                id = "m_defeat5", name = "敵を 5 体討伐", type = "defeat", target = 5,
                souls = 100, embers = 70, exp = 100,
            });
            return c;
        }
    }

    /// <summary>ミッション定義。type: techSuccess / win / defeat / bonus。</summary>
    public sealed class MissionDef
    {
        public string id = "";
        public string name = "";
        public string type = "win";
        /// <summary>type に応じた絞り込み（techSuccess=難易度id、win=役名、他は無視）。</summary>
        public string filterId = "";
        public int target = 3;
        public int souls, embers, exp, atGames;
    }

    /// <summary>進行中のミッション。</summary>
    public sealed class MissionState
    {
        public string id = "";
        public int progress;
    }

    public static class TechDirector
    {
        /// <summary>
        /// この G に課題を出すか決める。auto が true（オート中）なら必ず出さない。
        /// scene: "normal" / "engage" / "bonus" / "at"
        /// </summary>
        public static TechChallenge Roll(TechConfig cfg, string scene, bool auto, Symbol[][] strips, IRandom rng)
        {
            var none = default(TechChallenge);
            if (cfg == null || !cfg.enabled || auto || strips == null) return none;
            var r = cfg.rates ?? new TechSceneRate();
            int rate = scene == "engage" ? r.engage : scene == "bonus" ? r.bonus : scene == "at" ? r.at : r.normal;
            if (rate <= 0 || rng.NextDouble() * 100 >= rate) return none;
            if (cfg.levels == null || cfg.levels.Count == 0) return none;

            int total = 0;
            foreach (var l in cfg.levels) total += Math.Max(0, l.weight);
            if (total <= 0) return none;
            int pick = rng.Next(total);
            TechLevelDef def = null;
            foreach (var l in cfg.levels)
            {
                int w = Math.Max(0, l.weight);
                if (pick < w) { def = l; break; }
                pick -= w;
            }
            if (def == null) return none;

            var kind = def.kind == "Vita" ? TechKind.Vita : TechKind.TwoKoma;
            if (def.symbols == null || def.symbols.Count == 0) return none;
            if (!Enum.TryParse<Symbol>(def.symbols[rng.Next(def.symbols.Count)], out var sym)) return none;

            // 対象リール: 指定が無ければ、その図柄が実在するリールから選ぶ
            var cand = new List<int>();
            if (def.reels != null && def.reels.Count > 0)
            {
                foreach (var i in def.reels) if (i >= 0 && i < strips.Length && Has(strips[i], sym)) cand.Add(i);
            }
            else
            {
                for (int i = 0; i < strips.Length; i++) if (Has(strips[i], sym)) cand.Add(i);
            }
            if (cand.Count == 0) return none;

            return new TechChallenge
            {
                kind = kind,
                id = def.id,
                reel = cand[rng.Next(cand.Count)],
                symbol = sym,
                row = 1,          // ビタは中段。枠内狙いも中段を目安に押してもらう
                aimIndex = -1,    // 実際の狙い位置は SlotMachine 側で決める
            };
        }

        /// <summary>その図柄が row 段に来る上段コマ番号を全部返す（狙い位置の候補）。</summary>
        public static List<int> AimIndices(Symbol[] strip, Symbol sym, int row)
        {
            var r = new List<int>();
            int len = strip.Length;
            for (int top = 0; top < len; top++) if (strip[(top + row) % len] == sym) r.Add(top);
            return r;
        }

        private static bool Has(Symbol[] strip, Symbol s)
        {
            foreach (var x in strip) if (x == s) return true;
            return false;
        }

        /// <summary>停止結果が課題を満たしたか。stopped は [上,中,下]。</summary>
        public static bool Judge(TechChallenge ch, Symbol[] stopped)
        {
            if (!ch.Active || stopped == null) return false;
            if (ch.kind == TechKind.Vita)
            {
                int row = ch.row >= 0 && ch.row < 3 ? ch.row : 1;
                return stopped[row] == ch.symbol;
            }
            foreach (var s in stopped) if (s == ch.symbol) return true;   // 枠内にあれば成功
            return false;
        }

        public static TechLevelDef FindLevel(TechConfig cfg, string id)
        {
            if (cfg?.levels == null || string.IsNullOrEmpty(id)) return null;
            foreach (var l in cfg.levels) if (l.id == id) return l;
            return null;
        }

        public static MissionDef FindMission(TechConfig cfg, string id)
        {
            if (cfg?.missions == null || string.IsNullOrEmpty(id)) return null;
            foreach (var m in cfg.missions) if (m.id == id) return m;
            return null;
        }

        /// <summary>今持っていないミッションから1つ選ぶ。全部持っていれば null。</summary>
        public static MissionDef PickNew(TechConfig cfg, List<MissionState> active, IRandom rng)
        {
            if (cfg?.missions == null || cfg.missions.Count == 0) return null;
            var pool = new List<MissionDef>();
            foreach (var m in cfg.missions)
            {
                bool held = false;
                if (active != null) foreach (var a in active) if (a.id == m.id) { held = true; break; }
                if (!held) pool.Add(m);
            }
            return pool.Count == 0 ? null : pool[rng.Next(pool.Count)];
        }
    }
}
