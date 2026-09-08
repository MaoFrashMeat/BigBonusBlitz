using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>旅人1人分の定義。</summary>
    public sealed class TravelerDef
    {
        public string id = "A";
        public string name = "旅人A";
        /// <summary>出現重み（旅人が出るとき誰が出るか）。</summary>
        public int weight = 20;
        /// <summary>通過にかかる秒数（歩く速さ）。</summary>
        public float walkSeconds = 6f;
        /// <summary>セリフを言う確率 %。</summary>
        public int serifRate = 40;
        /// <summary>モード示唆のセリフを言う確率 %（serif を言うと決まった後）。残りは雑談。</summary>
        public int modeHintRate = 30;
        /// <summary>雑談セリフ。</summary>
        public List<string> chatter = new List<string> { "よいしょ、よいしょ…" };
        /// <summary>見た目（仮）: 体色 HTML カラー。</summary>
        public string color = "#c8a060";
    }

    /// <summary>モード示唆セリフ: key=セリフ, 各モードでの重み。</summary>
    public sealed class ModeSerif
    {
        public string text = "";
        public int A = 25, B = 25, C = 25, D = 25;
        public int WeightFor(Mode m) => m == Mode.A ? A : m == Mode.B ? B : m == Mode.C ? C : D;
    }

    public sealed class TravelerConfig
    {
        /// <summary>通常時 1G ごとの出現率 %（敵・前兆・ボーナス中は出ない）。</summary>
        public int appearanceRate = 6;
        public List<TravelerDef> travelers = new List<TravelerDef>();
        public List<ModeSerif> modeSerifs = new List<ModeSerif>();

        public static TravelerConfig Default()
        {
            var c = new TravelerConfig();
            c.travelers.Add(new TravelerDef { id = "A", name = "行商人", weight = 30, walkSeconds = 7f, serifRate = 35, modeHintRate = 15, color = "#c8a060", chatter = new List<string> { "よいしょ、よいしょ…", "今日も荷が重い…", "いい天気だ" } });
            c.travelers.Add(new TravelerDef { id = "B", name = "巡礼者", weight = 25, walkSeconds = 8f, serifRate = 45, modeHintRate = 30, color = "#8fb0d8", chatter = new List<string> { "……", "祈りを捧げよう", "道は続く" } });
            c.travelers.Add(new TravelerDef { id = "C", name = "狩人", weight = 20, walkSeconds = 5f, serifRate = 40, modeHintRate = 40, color = "#7ab060", chatter = new List<string> { "獲物の気配がない", "足跡だ…", "静かすぎる" } });
            c.travelers.Add(new TravelerDef { id = "D", name = "占い師", weight = 15, walkSeconds = 9f, serifRate = 70, modeHintRate = 70, color = "#b080d0", chatter = new List<string> { "星が囁いている…", "運命は動く", "ふふ…" } });
            c.travelers.Add(new TravelerDef { id = "E", name = "王の使者", weight = 10, walkSeconds = 4f, serifRate = 80, modeHintRate = 90, color = "#e8c040", chatter = new List<string> { "急ぎの用だ！", "道を空けよ！" } });
            c.modeSerifs.Add(new ModeSerif { text = "この先の道は長いぞ…", A = 60, B = 25, C = 10, D = 5 });
            c.modeSerifs.Add(new ModeSerif { text = "風向きが変わってきたな", A = 20, B = 50, C = 25, D = 5 });
            c.modeSerifs.Add(new ModeSerif { text = "ここは良い土地だ", A = 10, B = 25, C = 50, D = 15 });
            c.modeSerifs.Add(new ModeSerif { text = "宝の匂いがする…！", A = 2, B = 8, C = 30, D = 60 });
            c.modeSerifs.Add(new ModeSerif { text = "嵐が来る。備えよ", A = 0, B = 5, C = 25, D = 70 });
            return c;
        }
    }

    public struct TravelerEvent
    {
        public TravelerDef traveler;
        public string serif;      // null なら無言
        public bool isModeHint;
    }

    /// <summary>通常時に旅人を出すかを抽選し、出すなら誰が・何を言うかを決める。</summary>
    public static class TravelerDirector
    {
        public static TravelerEvent? Roll(TravelerConfig cfg, Mode mode, IRandom rng)
        {
            cfg ??= TravelerConfig.Default();
            if (cfg.travelers == null || cfg.travelers.Count == 0) return null;
            if (rng.NextDouble() * 100 >= cfg.appearanceRate) return null;

            int total = 0;
            foreach (var t in cfg.travelers) total += System.Math.Max(0, t.weight);
            if (total <= 0) return null;
            int roll = rng.Next(total);
            TravelerDef pick = cfg.travelers[0];
            foreach (var t in cfg.travelers)
            {
                int w = System.Math.Max(0, t.weight);
                if (roll < w) { pick = t; break; }
                roll -= w;
            }

            var ev = new TravelerEvent { traveler = pick, serif = null, isModeHint = false };
            if (rng.NextDouble() * 100 < pick.serifRate)
            {
                if (cfg.modeSerifs != null && cfg.modeSerifs.Count > 0 && rng.NextDouble() * 100 < pick.modeHintRate)
                {
                    int mt = 0;
                    foreach (var s in cfg.modeSerifs) mt += System.Math.Max(0, s.WeightFor(mode));
                    if (mt > 0)
                    {
                        int r = rng.Next(mt);
                        foreach (var s in cfg.modeSerifs)
                        {
                            int w = System.Math.Max(0, s.WeightFor(mode));
                            if (r < w) { ev.serif = s.text; ev.isModeHint = true; break; }
                            r -= w;
                        }
                    }
                }
                if (ev.serif == null && pick.chatter != null && pick.chatter.Count > 0)
                    ev.serif = pick.chatter[rng.Next(pick.chatter.Count)];
            }
            return ev;
        }
    }
}
