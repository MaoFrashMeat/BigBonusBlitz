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

    /// <summary>
    /// 押した精度のランク（2026-09-14 本人: 2コマ目押し / 1コマ目押し / ビタ押し。ビタの中はタイミングで 5 段、最高 Perfect!! 最低 Cool）。
    /// 上から順に見て最初に当てはまるものを使う。報酬は成功報酬に bonusPercent % を上乗せする。
    /// </summary>
    public sealed class TechRankDef
    {
        public string id = "";
        public string name = "";
        /// <summary>文字の色（#rrggbb）。</summary>
        public string color = "#ffffff";
        /// <summary>狙いの上段コマの何コマ手前で押したか（0 = ビタ、1 = 1コマ目押し、2 = 2コマ目押し）。</summary>
        public int koma = 0;
        /// <summary>ビタの中の精度: 押した瞬間のコマ内の位置が、コマの中心からどれだけずれていたか（0〜0.5）。負なら問わない。</summary>
        public float phase = -1f;
        /// <summary>成功報酬に上乗せする割合（%）。</summary>
        public int bonusPercent = 0;
        /// <summary>文字を虹色で出す（最高ランク用）。</summary>
        public bool rainbow = false;
    }

    /// <summary>ランクの文字の出し方（対象リールの上に出す。tools/fx_viewer.html と同じ式）。</summary>
    public sealed class TechRankFxConfig
    {
        /// <summary>リールの中心からの高さ（px、上が +）と文字の大きさ。</summary>
        public float y = 70f;
        public int fontSize = 44;
        /// <summary>入る秒（大きく出て縮む）/ 止まる秒 / 抜ける秒（上へ流れて消える）。</summary>
        public float inSeconds = 0.16f, holdSeconds = 0.9f, outSeconds = 0.35f;
        /// <summary>抜けるときに上へ流れる量（px）。</summary>
        public float rise = 40f;
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
        /// <summary>課題が出たGは、この秒数だけ停止を受け付けず「Ready？」を見せて構えさせる。</summary>
        public float readySeconds = 1.2f;
        /// <summary>押した精度のランク（上から順に判定）と、その文字の出し方。</summary>
        public List<TechRankDef> ranks = new List<TechRankDef>();
        public TechRankFxConfig rankFx = new TechRankFxConfig();

        public static List<TechRankDef> DefaultRanks() => new List<TechRankDef>
        {
            new TechRankDef { id = "perfect",   name = "Perfect!!",  color = "#ff66d9", koma = 0, phase = 0.10f, bonusPercent = 100, rainbow = true },
            new TechRankDef { id = "excellent", name = "Excellent!", color = "#ffd23f", koma = 0, phase = 0.20f, bonusPercent = 70 },
            new TechRankDef { id = "great",     name = "Great!",     color = "#7cf47c", koma = 0, phase = 0.30f, bonusPercent = 50 },
            new TechRankDef { id = "good",      name = "Good",       color = "#7cc8ff", koma = 0, phase = 0.40f, bonusPercent = 30 },
            new TechRankDef { id = "cool",      name = "Cool",       color = "#ffffff", koma = 0, phase = -1f,   bonusPercent = 20 },
            new TechRankDef { id = "koma1",     name = "1コマ",       color = "#d8d8d8", koma = 1, phase = -1f,   bonusPercent = 10 },
            new TechRankDef { id = "koma2",     name = "2コマ",       color = "#a8a8a8", koma = 2, phase = -1f,   bonusPercent = 0 },
        };

        public static TechConfig Default()
        {
            var c = new TechConfig();
            c.ranks = DefaultRanks();
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

        /// <summary>
        /// 押した精度のランク。pressIdx は押した瞬間に上段にあったコマ、phase はそのコマ内の位置（0〜1、0.5 が中心）。
        /// 狙いの上段コマ（その図柄が狙う段に来る位置）まで、滑る向きに何コマ手前かを数え、maxSlip を超えれば null（腕でなく制御で入った）。
        /// </summary>
        public static TechRankDef Rank(TechConfig cfg, TechChallenge ch, Symbol[] strip, int pressIdx, float phase, int maxSlip = SlipController.DefaultMaxSlip)
        {
            if (cfg?.ranks == null || cfg.ranks.Count == 0 || !ch.Active || strip == null || pressIdx < 0) return null;
            int len = strip.Length, koma = int.MaxValue;
            foreach (var aim in AimIndices(strip, ch.symbol, ch.row >= 0 && ch.row < 3 ? ch.row : 1))
            {
                int k = ((pressIdx - aim) % len + len) % len;   // 上段のコマ番号は回るほど減るので、aim+k で押せば k コマ滑って aim に止まる
                if (k < koma) koma = k;
            }
            if (koma > maxSlip) return null;
            float dev = Math.Abs(Math.Max(0f, Math.Min(1f, phase)) - 0.5f);
            foreach (var r in cfg.ranks)
                if (r != null && r.koma == koma && (r.phase < 0f || dev <= r.phase + 1e-4f)) return r;
            return null;
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
