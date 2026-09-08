namespace BBB.Core
{
    public struct WinResult
    {
        public int payout;
        public bool isReplay;
        public bool bonusWon;
        public WinType winType;
    }

    /// <summary>main.js evaluateWin のライン判定部分（状態変更なし）。</summary>
    public static class WinEvaluator
    {
        public static WinResult Evaluate(Symbol[][] stopped, BonusMode bonusMode, Payouts payouts)
        {
            var r = new WinResult { winType = WinType.NONE };

            // チェリーは左リール枠内にあれば成立
            bool cherry = false;
            foreach (var s in stopped[0]) if (s == Symbol.CHERRY) cherry = true;

            bool bellPaid = false, replayHit = false;
            foreach (var (flag, _) in WinCombos.CompletedWins(stopped))
            {
                // 同じ役が複数ラインで揃っても払い出しは1回（2ライン成立は制御で禁止。ここは保険）
                if (flag.IsBell()) { if (bellPaid) continue; bellPaid = true; }
                if (flag.IsReplay()) { if (replayHit) continue; replayHit = true; }
                if (flag.IsBB())
                {
                    if (bonusMode == BonusMode.NORMAL) { r.bonusWon = true; r.winType = WinType.BIG; }
                }
                else if (flag.IsRB())
                {
                    if (bonusMode == BonusMode.NORMAL) { r.bonusWon = true; if (r.winType != WinType.BIG) r.winType = WinType.REG; }
                }
                else if (flag.IsReplay()) r.isReplay = true;
                else if (flag.IsBell())
                {
                    r.payout += payouts.STAR;
                    if (r.winType == WinType.NONE) r.winType = WinType.BELL;
                }
                else if (flag.IsSuica())
                {
                    r.payout += payouts.WATERMELON;
                    if (r.winType == WinType.NONE) r.winType = WinType.WATERMELON;
                }
                else if (flag == Flag.CHANCE_D)
                {
                    if (r.winType == WinType.NONE) r.winType = WinType.CHANCE;
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
