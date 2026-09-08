namespace BBB.Core
{
    /// <summary>main.js determineNextMode。timing は "initial" / "bonus"。</summary>
    public static class ModeTransition
    {
        public static Mode Determine(GameConfig cfg, string timing, int setting, IRandom rng)
        {
            if (cfg.modeTransitions == null) return Mode.A;
            if (!cfg.modeTransitions.TryGetValue(timing, out var bySetting)) return Mode.A;
            if (!bySetting.TryGetValue(setting.ToString(), out var probs)) return Mode.A;
            double roll = rng.NextDouble() * 100;
            double sum = 0;
            foreach (Mode m in new[] { Mode.A, Mode.B, Mode.C, Mode.D })
            {
                sum += probs.TryGetValue(m.ToString(), out var p) ? p : 0;
                if (roll < sum) return m;
            }
            return Mode.A;
        }
    }
}
