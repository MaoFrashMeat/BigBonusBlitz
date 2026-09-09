using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>1ライン分の揃い定義。symbols[r] が null は「何でもよい」。</summary>
    public sealed class WinCombo
    {
        public Symbol[][] symbols;      // 各リールで許容される図柄（null=任意）
        public int[] validLines;
        public bool isReachMe;

        public bool AcceptsAt(int reel, Symbol s)
        {
            var a = symbols[reel];
            if (a == null) return true;
            foreach (var x in a) if (x == s) return true;
            return false;
        }
    }

    public static class PayLines
    {
        // 0: 上段, 1: 中段, 2: 下段, 3: 右下がり, 4: 右上がり
        // （main.js は lines[] が 0=中段 でコメントと食い違っていた。コメント側＝中段を正とする: 2026-09-08）
        public static readonly int[][] Rows =
        {
            new[] { 0, 0, 0 },
            new[] { 1, 1, 1 },
            new[] { 2, 2, 2 },
            new[] { 0, 1, 2 },
            new[] { 2, 1, 0 },
        };
        public const int Count = 5;
    }

    /// <summary>main.js WIN_COMBOS。</summary>
    public static class WinCombos
    {
        private static Symbol[] S(params Symbol[] s) => s;

        /// <summary>
        /// true: ベル／リプレイは A/B/C の区別なく 5ライン全部で成立扱い（2026-09-08 確定）。
        /// 判定（払い出し）と制御（先読み）の両方がこれを見る。
        /// </summary>
        public static bool AnyLineForBellReplay = true;

        public static readonly Dictionary<Flag, WinCombo> Table = new Dictionary<Flag, WinCombo>
        {
            { Flag.BB_A, new WinCombo { symbols = new[] { S(Symbol.RED7), S(Symbol.RED7), S(Symbol.RED7) }, validLines = new[] { 0, 1, 2, 3, 4 } } },
            { Flag.BB_B, new WinCombo { symbols = new[] { S(Symbol.BLUE7), S(Symbol.BLUE7), S(Symbol.BLUE7) }, validLines = new[] { 0, 1, 2, 3, 4 } } },
            // BAR 揃いは全ラインで BIG（中段限定だと、上段に BAR が3つ並んでも何も起きず「当たったのに何もない」と見える）
            { Flag.BB_C, new WinCombo { symbols = new[] { S(Symbol.BAR), S(Symbol.BAR), S(Symbol.BAR) }, validLines = new[] { 0, 1, 2, 3, 4 } } },
            // 青7・赤7・青7 も全ラインで BIG（BAR 揃いと同じ理由。中段限定だと紛らわしい停止形を制御が避けない）
            { Flag.BB_D, new WinCombo { symbols = new[] { S(Symbol.BLUE7), S(Symbol.RED7), S(Symbol.BLUE7) }, validLines = new[] { 0, 1, 2, 3, 4 } } },
            { Flag.RB_A, new WinCombo { symbols = new[] { S(Symbol.RED7), S(Symbol.RED7), S(Symbol.BAR) }, validLines = new[] { 0, 1, 2, 3, 4 } } },
            { Flag.RB_B, new WinCombo { symbols = new[] { S(Symbol.BLUE7), S(Symbol.BLUE7), S(Symbol.BAR) }, validLines = new[] { 0, 1, 2, 3, 4 } } },
            { Flag.REPLAY_A, new WinCombo { symbols = new[] { S(Symbol.REPLAY, Symbol.RED7), S(Symbol.REPLAY), S(Symbol.REPLAY) }, validLines = new[] { 0 } } },
            { Flag.REPLAY_B, new WinCombo { symbols = new[] { S(Symbol.REPLAY, Symbol.RED7), S(Symbol.REPLAY), S(Symbol.REPLAY) }, validLines = new[] { 3, 4 } } },
            { Flag.REPLAY_C, new WinCombo { symbols = new[] { S(Symbol.REPLAY, Symbol.RED7), S(Symbol.REPLAY), S(Symbol.REPLAY) }, validLines = new[] { 2 } } },
            { Flag.BELL_A, new WinCombo { symbols = new[] { S(Symbol.STAR, Symbol.BAR), S(Symbol.STAR), S(Symbol.STAR) }, validLines = new[] { 0, 3, 4 } } },
            { Flag.BELL_B, new WinCombo { symbols = new[] { S(Symbol.STAR, Symbol.BAR), S(Symbol.STAR), S(Symbol.STAR) }, validLines = new[] { 1 } } },
            { Flag.BELL_C, new WinCombo { symbols = new[] { S(Symbol.STAR, Symbol.BAR), S(Symbol.STAR), S(Symbol.STAR) }, validLines = new[] { 2 } } },
            { Flag.CHERRY_A, new WinCombo { symbols = new[] { S(Symbol.CHERRY), null, null }, validLines = new[] { 0, 2 } } },
            { Flag.CHERRY_B, new WinCombo { symbols = new[] { S(Symbol.CHERRY), null, S(Symbol.RED7, Symbol.BLUE7, Symbol.BAR) }, validLines = new[] { 0, 2 } } },
            { Flag.CHERRY_C, new WinCombo { symbols = new[] { S(Symbol.CHERRY), null, null }, validLines = new[] { 1 } } },
            { Flag.SUICA_A, new WinCombo { symbols = new[] { S(Symbol.WATERMELON), S(Symbol.WATERMELON), S(Symbol.WATERMELON) }, validLines = new[] { 0, 3, 4 } } },
            { Flag.SUICA_B, new WinCombo { symbols = new[] { S(Symbol.WATERMELON), S(Symbol.WATERMELON), S(Symbol.WATERMELON) }, validLines = new[] { 1 } } },
            { Flag.SUICA_C, new WinCombo { symbols = new[] { S(Symbol.WATERMELON), S(Symbol.WATERMELON), S(Symbol.WATERMELON) }, validLines = new[] { 2 } } },
            { Flag.CHANCE_A, new WinCombo { isReachMe = true } },
            { Flag.CHANCE_B, new WinCombo { isReachMe = true } },
            { Flag.CHANCE_C, new WinCombo { isReachMe = true } },
            // CHANCE_D（中段 RP/RP/★ のチャンス目）は 2026-09-08 廃止。抽選フラグは残すが停止形は持たない（リーチ目扱い）
            { Flag.CHANCE_D, new WinCombo { isReachMe = true } },
        };

        /// <summary>
        /// 3リール分の停止図柄（null は未停止）から、成立している役を列挙する。
        /// 全リール停止していないときは何も返さない（main.js と同じ）。
        /// </summary>
        public static IEnumerable<(Flag flag, int line)> CompletedWins(Symbol[][] stopped)
        {
            if (stopped[0] == null || stopped[1] == null || stopped[2] == null) yield break;
            for (int l = 0; l < PayLines.Count; l++)
            {
                var rows = PayLines.Rows[l];
                foreach (var kv in Table)
                {
                    var combo = kv.Value;
                    if (combo.isReachMe) continue;
                    bool anyLine = AnyLineForBellReplay && (kv.Key.IsBell() || kv.Key.IsReplay());
                    if (!anyLine && System.Array.IndexOf(combo.validLines, l) < 0) continue;
                    bool match = true;
                    for (int r = 0; r < 3 && match; r++)
                        if (!combo.AcceptsAt(r, stopped[r][rows[r]])) match = false;
                    if (match) yield return (kv.Key, l);
                }
            }
        }
    }
}
