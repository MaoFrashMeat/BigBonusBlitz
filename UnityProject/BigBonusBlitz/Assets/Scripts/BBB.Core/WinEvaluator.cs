namespace BBB.Core
{
    public struct WinResult
    {
        public int payout;
        public bool isReplay;
        public bool bonusWon;
        public WinType winType;
        /// <summary>揃った払い出しラインのビット（PayLines.Rows のインデックス 0..4）。演出で光らせる位置に使う。</summary>
        public int lineMask;
        /// <summary>左リールでチェリーが止まった段のビット（0=上, 1=中, 2=下）。チェリーはライン成立ではないので別持ち。</summary>
        public int cherryMask;
    }

    /// <summary>main.js evaluateWin のライン判定部分（状態変更なし）。</summary>
    public static class WinEvaluator
    {
        public static WinResult Evaluate(Symbol[][] stopped, BonusMode bonusMode, Payouts payouts)
        {
            var r = new WinResult { winType = WinType.NONE };

            // チェリーは左リール枠内にあれば成立（止まった段を覚えておく）
            bool cherry = false;
            for (int row = 0; row < stopped[0].Length; row++)
                if (stopped[0][row] == Symbol.CHERRY) { cherry = true; r.cherryMask |= 1 << row; }

            bool bellPaid = false, replayHit = false;
            foreach (var (flag, line) in WinCombos.CompletedWins(stopped))
            {
                // 同じ役が複数ラインで揃っても払い出しは1回（2ライン成立は制御で禁止。ここは保険）
                if (flag.IsBell()) { if (bellPaid) continue; bellPaid = true; }
                if (flag.IsReplay()) { if (replayHit) continue; replayHit = true; }
                int bit = 1 << line;
                if (flag.IsBB())
                {
                    if (bonusMode == BonusMode.NORMAL) { r.bonusWon = true; r.winType = WinType.BIG; r.lineMask |= bit; }
                }
                else if (flag.IsRB())
                {
                    if (bonusMode == BonusMode.NORMAL) { r.bonusWon = true; if (r.winType != WinType.BIG) r.winType = WinType.REG; r.lineMask |= bit; }
                }
                else if (flag.IsReplay()) { r.isReplay = true; r.lineMask |= bit; }
                else if (flag.IsBell())
                {
                    r.payout += payouts.STAR;
                    if (r.winType == WinType.NONE) r.winType = WinType.BELL;
                    r.lineMask |= bit;
                }
                else if (flag.IsSuica())
                {
                    r.payout += payouts.WATERMELON;
                    if (r.winType == WinType.NONE) r.winType = WinType.WATERMELON;
                    r.lineMask |= bit;
                }
                else if (flag == Flag.CHANCE_D)
                {
                    if (r.winType == WinType.NONE) r.winType = WinType.CHANCE;
                    r.lineMask |= bit;
                }
            }

            if (cherry)
            {
                r.payout += payouts.CHERRY;
                if (r.winType == WinType.NONE) r.winType = WinType.CHERRY;
            }
            return r;
        }
    }
}
