namespace BBB.Core
{
    /// <summary>main.js の FLAGS と同じ番号。順序は抽選テーブル生成順にも使う。</summary>
    public enum Flag
    {
        HAZE = 0,
        BB_A = 1, BB_B = 2, BB_C = 3, BB_D = 4,
        RB_A = 5, RB_B = 6,
        REPLAY_A = 7, REPLAY_B = 8, REPLAY_C = 9,
        BELL_A = 10, BELL_B = 11, BELL_C = 12,
        CHERRY_A = 13, CHERRY_B = 14, CHERRY_C = 15,
        SUICA_A = 16, SUICA_B = 17, SUICA_C = 18,
        CHANCE_A = 19, CHANCE_B = 20, CHANCE_C = 21,
        CHANCE_D = 22
    }

    public static class FlagExt
    {
        public const int Count = 23;

        public static bool IsBB(this Flag f) => f >= Flag.BB_A && f <= Flag.BB_D;
        public static bool IsRB(this Flag f) => f >= Flag.RB_A && f <= Flag.RB_B;
        public static bool IsBonus(this Flag f) => f.IsBB() || f.IsRB();
        public static bool IsReplay(this Flag f) => f >= Flag.REPLAY_A && f <= Flag.REPLAY_C;
        public static bool IsBell(this Flag f) => f >= Flag.BELL_A && f <= Flag.BELL_C;
        public static bool IsCherry(this Flag f) => f >= Flag.CHERRY_A && f <= Flag.CHERRY_C;
        public static bool IsSuica(this Flag f) => f >= Flag.SUICA_A && f <= Flag.SUICA_C;
        /// <summary>CHANCE_A〜C（リーチ目）。CHANCE_D はチャンス目で別扱い。</summary>
        public static bool IsReachMe(this Flag f) => f >= Flag.CHANCE_A && f <= Flag.CHANCE_C;
    }

    public enum Symbol { RED7, BLUE7, BAR, STAR, WATERMELON, CHERRY, REPLAY, BLANK }

    public enum Mode { A, B, C, D }
    public enum BonusMode { NORMAL, BB, RB }

    /// <summary>evaluateWin の winType。</summary>
    public enum WinType { NONE, BIG, REG, BELL, WATERMELON, CHERRY, CHANCE, REPLAY }
}
