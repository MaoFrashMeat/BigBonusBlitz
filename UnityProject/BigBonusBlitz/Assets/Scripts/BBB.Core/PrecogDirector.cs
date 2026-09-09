using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>
    /// 事前察知（予告）: レバーオンで役が決まった瞬間に「何かが来る」と匂わせる演出の抽選。
    /// 役ごとに none / weak / strong の重みを持ち、ハズレでもごく低確率で偽の予告（ガセ）が出る。
    /// 色は役ごとに固定（金=ベル / 青=リプレイ / 桃=チェリー / 緑=スイカ / 虹=チャンス目 / 白=ボーナス）。
    /// 重みは game_config.json の precog。
    /// </summary>
    public sealed class PrecogConfig
    {
        /// <summary>役キー → { none, weak, strong } の重み。</summary>
        public Dictionary<string, Dictionary<string, int>> byFlag = new Dictionary<string, Dictionary<string, int>>
        {
            ["BELL"] = new Dictionary<string, int> { ["none"] = 55, ["weak"] = 35, ["strong"] = 10 },
            ["REPLAY"] = new Dictionary<string, int> { ["none"] = 60, ["weak"] = 35, ["strong"] = 5 },
            ["CHERRY"] = new Dictionary<string, int> { ["none"] = 30, ["weak"] = 40, ["strong"] = 30 },
            ["SUICA"] = new Dictionary<string, int> { ["none"] = 25, ["weak"] = 40, ["strong"] = 35 },
            ["CHANCE"] = new Dictionary<string, int> { ["none"] = 10, ["weak"] = 30, ["strong"] = 60 },
            ["BONUS"] = new Dictionary<string, int> { ["none"] = 30, ["weak"] = 30, ["strong"] = 40 },
            ["HAZE"] = new Dictionary<string, int> { ["none"] = 93, ["weak"] = 6, ["strong"] = 1 },
        };
        /// <summary>ハズレで予告が出たとき、どの役の色に化けるか（ガセ）の重み。</summary>
        public Dictionary<string, int> fakeRole = new Dictionary<string, int>
        {
            ["BELL"] = 40, ["REPLAY"] = 30, ["CHERRY"] = 15, ["SUICA"] = 10, ["CHANCE"] = 5,
        };
    }

    public struct Precog
    {
        /// <summary>0=無し, 1=weak, 2=strong。</summary>
        public int stage;
        /// <summary>演出の色を決める役キー（BELL/REPLAY/CHERRY/SUICA/CHANCE/BONUS）。ガセなら偽の役。</summary>
        public string role;
        /// <summary>実際の役キー（HAZE 含む）。</summary>
        public string actual;
        public bool IsFake => actual == "HAZE" && stage > 0;
    }

    public static class PrecogDirector
    {
        public static string RoleKey(Flag f)
        {
            if (f.IsBell()) return "BELL";
            if (f.IsReplay()) return "REPLAY";
            if (f.IsCherry()) return "CHERRY";
            if (f.IsSuica()) return "SUICA";
            if (f.IsReachMe() || f == Flag.CHANCE_D) return "CHANCE";
            if (f.IsBonus()) return "BONUS";
            return "HAZE";
        }

        public static Precog Roll(PrecogConfig cfg, Flag flag, IRandom rng)
        {
            cfg = cfg ?? new PrecogConfig();
            string key = RoleKey(flag);
            var p = new Precog { stage = 0, role = key, actual = key };
            if (cfg.byFlag == null || !cfg.byFlag.TryGetValue(key, out var dist) || dist == null) return p;
            int none = Get(dist, "none"), weak = Get(dist, "weak"), strong = Get(dist, "strong");
            int total = none + weak + strong;
            if (total <= 0) return p;
            double r = rng.NextDouble() * total;
            p.stage = r < none ? 0 : r < none + weak ? 1 : 2;
            if (p.stage > 0 && key == "HAZE") p.role = PickFake(cfg.fakeRole, rng);
            return p;
        }

        private static int Get(Dictionary<string, int> d, string k) => d.TryGetValue(k, out var v) ? System.Math.Max(0, v) : 0;

        private static string PickFake(Dictionary<string, int> weights, IRandom rng)
        {
            if (weights == null || weights.Count == 0) return "BELL";
            int total = 0;
            foreach (var kv in weights) total += System.Math.Max(0, kv.Value);
            if (total <= 0) return "BELL";
            double r = rng.NextDouble() * total;
            foreach (var kv in weights)
            {
                int w = System.Math.Max(0, kv.Value);
                if (r < w) return kv.Key;
                r -= w;
            }
            return "BELL";
        }
    }
}
