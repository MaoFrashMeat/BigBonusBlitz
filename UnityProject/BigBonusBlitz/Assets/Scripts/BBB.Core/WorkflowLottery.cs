using System.Collections.Generic;

namespace BBB.Core
{
    public struct WorkflowResult
    {
        public string roleKey;
        public string category;   // NONE / SERIF / ENEMY / ZONE / ACTION
        public string variant;    // A〜F / NONE / null
    }

    /// <summary>main.js runWorkflowLottery / winTypeToWorkflowRole。</summary>
    public static class WorkflowLottery
    {
        public static readonly string[] Categories = { "SERIF", "ENEMY", "ZONE", "ACTION" };
        public static readonly char[] Variants = { 'A', 'B', 'C', 'D', 'E', 'F' };

        public static string RoleKey(WinType winType, bool isReplay)
        {
            if (isReplay) return "REPLAY";
            switch (winType)
            {
                case WinType.BIG:
                case WinType.REG: return "BONUS";
                case WinType.BELL: return "BELL";
                case WinType.WATERMELON: return "SUICA";
                case WinType.CHERRY: return "CHERRY";
                case WinType.CHANCE: return "CHANCE";
                default: return "HAZE";
            }
        }

        public static WorkflowResult Run(Dictionary<string, WorkflowRole> config, string roleKey, IRandom rng)
        {
            var none = new WorkflowResult { roleKey = roleKey, category = "NONE", variant = null };
            if (config == null || !config.TryGetValue(roleKey, out var role) || role == null) return none;

            double roll = rng.NextDouble() * 100;
            double cum = role.NONE;
            if (roll < cum) return none;

            foreach (var catKey in Categories)
            {
                var cat = role.Get(catKey);
                if (cat == null || cat.rate == 0) continue;
                cum += cat.rate;
                if (roll < cum)
                {
                    double vroll = rng.NextDouble() * 100;
                    double vcum = 0;
                    foreach (var v in Variants)
                    {
                        vcum += cat.Variant(v);
                        if (vroll < vcum) return new WorkflowResult { roleKey = roleKey, category = catKey, variant = v.ToString() };
                    }
                    return new WorkflowResult { roleKey = roleKey, category = catKey, variant = "NONE" };
                }
            }
            return none;
        }
    }
}
