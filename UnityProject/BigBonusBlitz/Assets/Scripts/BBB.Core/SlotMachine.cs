using System;
using System.Collections.Generic;

namespace BBB.Core
{
    public enum BellCommand { None, Success, Fail }

    /// <summary>ベル択ナビ。first（中）を第一停止した後、残り2リールのどちらが正解かを当てる。</summary>
    public struct BellNavi
    {
        public int first;        // ナビ指定の第一停止リール（常に 1=中）
        public int correctReel;  // 第二停止でこれを押すと正解
        public bool Active;
        /// <summary>第一停止がナビ通りで、いま択の最中か。</summary>
        public bool InChoice;
    }

    /// <summary>1ゲームの結果（表示・演出側が読む）。</summary>
    public sealed class GameResult
    {
        /// <summary>このGで実行されたコマンド。</summary>
        public BellCommand command;
        /// <summary>正解で討伐が内部確定した（告知は3G目）。</summary>
        public bool naviDefeatGuaranteed;
        /// <summary>正解で得た EXP。</summary>
        public int naviExp;
        public Flag flag;
        public WinResult win;
        public bool bonusStarted;
        public bool bonusEnded;
        public BonusMode bonusModeAfter;
        public WorkflowResult workflow;
        public bool enemySpawned;
        public EnemyTable enemyTable;
        /// <summary>このGで前兆が始まった（ENEMY 当選）。</summary>
        public bool precursorStarted;
        /// <summary>このGの前兆段階（1..N）。前兆中でなければ 0。出現Gは N。</summary>
        public int precursorStage;
        /// <summary>Tier2 決着: null=未決着, true=討伐, false=逃走。</summary>
        public bool? enemyResolved;
        public bool levelUp;
        public HintKind hint;
    }

    /// <summary>
    /// main.js の state と onMaxBet / onLever / drawLottery / onStop / evaluateWin を
    /// MonoBehaviour に依存しない形で移植したゲーム本体。
    /// リールの物理位置（何コマ目で押されたか）は外から渡す。
    /// </summary>
    public sealed class SlotMachine
    {
        public const int BetCost = 3;

        public readonly GameConfig Config;
        public readonly Dictionary<string, WorkflowRole> Workflow;
        public readonly IList<EnemyTable> EnemyTables;
        public readonly Symbol[][] Strips;
        private readonly IRandom _rng;

        // --- 状態（main.js state と対応） ---
        public int Credit = 50;
        public int Bet;
        public bool IsReplay;
        public int Setting { get; private set; } = 1;
        public Mode Mode = Mode.A;
        public int SpinCount;
        public int TotalSpinCount;
        public Flag CurrentFlag = Flag.HAZE;
        public int CurrentRng;
        public Flag HeldBonusFlag = Flag.HAZE;
        public BonusMode BonusMode = BonusMode.NORMAL;
        public int BonusPayoutTarget;
        public int BonusEarned;

        public bool IsTier2;
        public bool PendingTier2;
        public int Tier2SpinCount;
        public bool EnemyActive;
        public bool EnemyDefeatWon;
        /// <summary>前兆の残りG（0 なら前兆中でない）。</summary>
        public int PrecursorRemaining;
        /// <summary>前兆の総G数（演出の段階計算用）。</summary>
        public int PrecursorTotal;
        /// <summary>今Gのベル択ナビ（Active=false なら無し）。</summary>
        public BellNavi Navi;
        /// <summary>今Gの押し順（停止した順にリール番号）。</summary>
        public readonly List<int> PressOrder = new List<int>();
        /// <summary>今Gの択結果（第二停止時点で決まる）。</summary>
        public BellCommand CurrentCommand;
        public EnemyTable ActiveEnemyTable;
        public int PlayerLevel = 1;
        public int PlayerExp;

        // --- デバッグ用（main.js の debug-force-flag 相当） ---
        /// <summary>次のレバーでこのフラグを強制する（null で通常抽選）。1回で解除。</summary>
        public Flag? DebugForceFlag;
        /// <summary>次の判定でワークフローを ENEMY 当選扱いにする（敵未出現時のみ）。1回で解除。</summary>
        public bool DebugForceEnemy;

        public readonly Symbol[][] Stopped = new Symbol[3][];
        public readonly int[] StopIndex = new int[3];
        public readonly int[] Slip = new int[3];
        public bool IsGameActive { get; private set; }

        private LotteryTable _tA, _tB, _tC, _tD, _tBB, _tRB, _tTier2;
        private readonly Dictionary<string, LotteryTable> _enemyTier2 = new Dictionary<string, LotteryTable>();

        public SlotMachine(GameConfig config, Dictionary<string, WorkflowRole> workflow, IList<EnemyTable> enemyTables, IRandom rng, int setting = 1)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Workflow = workflow;
            EnemyTables = enemyTables ?? new List<EnemyTable>();
            Strips = config.ReelSymbols();
            _rng = rng ?? new SystemRandom();
            SetSetting(setting);
            Mode = ModeTransition.Determine(Config, "initial", Setting, _rng);
        }

        public void SetSetting(int setting)
        {
            Setting = Math.Max(1, Math.Min(6, setting));
            string s = Setting.ToString();
            _tA = new LotteryTable(Config.ProbabilitiesFor(Mode.A)[s]);
            _tB = new LotteryTable(Config.ProbabilitiesFor(Mode.B)[s]);
            _tC = new LotteryTable(Config.ProbabilitiesFor(Mode.C)[s]);
            _tD = new LotteryTable(Config.ProbabilitiesFor(Mode.D)[s]);
            _tBB = Config.probabilities_BB != null ? new LotteryTable(Config.probabilities_BB[s]) : null;
            _tRB = Config.probabilities_RB != null ? new LotteryTable(Config.probabilities_RB[s]) : null;
            _tTier2 = Config.probabilities_Tier2 != null ? new LotteryTable(Config.probabilities_Tier2) : null;
        }

        private LotteryTable Tier2TableFor(EnemyTable t)
        {
            if (t?.tier2Probabilities == null) return _tTier2;
            if (!_enemyTier2.TryGetValue(t.id, out var table))
            {
                table = new LotteryTable(t.tier2Probabilities);
                _enemyTier2[t.id] = table;
            }
            return table;
        }

        public Payouts CurrentPayouts => Config.PayoutsFor(BonusMode);

        // ---------------------------------------------------------------- BET
        /// <summary>onMaxBet。BET できたら true。</summary>
        public bool MaxBet()
        {
            if (IsGameActive) return false;
            if (IsReplay)
            {
                IsReplay = false;
                int cost = CurrentPayouts.REPLAY;
                Credit -= cost;
                Bet = BetCost;
                if (BonusMode != BonusMode.NORMAL) BonusEarned -= cost;
                return true;
            }
            if (Credit < BetCost) return false;
            Credit -= BetCost;
            Bet = BetCost;
            if (BonusMode != BonusMode.NORMAL) BonusEarned -= BetCost;
            return true;
        }

        // -------------------------------------------------------------- LEVER
        /// <summary>onLever + drawLottery。示唆演出の種別を返す。</summary>
        public HintKind Lever()
        {
            if (Bet == 0) throw new InvalidOperationException("BET されていない");
            IsGameActive = true;
            for (int i = 0; i < 3; i++) { Stopped[i] = null; Slip[i] = 0; }
            PressOrder.Clear();
            CurrentCommand = BellCommand.None;
            Navi = default;

            var hint = HintKind.None;
            if (IsTier2 && EnemyDefeatWon && ActiveEnemyTable != null)
                hint = EnemyEngage.RollHint(ActiveEnemyTable, Config.defaultHintConfig, _rng);

            DrawLottery();

            // ベル択ナビ: エンゲージ中のベル当選時だけ
            if (IsTier2 && EnemyActive && BonusMode == BonusMode.NORMAL && CurrentFlag.IsBell())
            {
                // 第一停止は常に「中」。左右のどちらが正解かはランダム（隠し）
                Navi = new BellNavi { first = 1, correctReel = _rng.NextDouble() < 0.5 ? 0 : 2, Active = true, InChoice = false };
            }
            return hint;
        }

        private void DrawLottery()
        {
            if (PendingTier2)
            {
                IsTier2 = true;
                Tier2SpinCount = 0;
                PendingTier2 = false;
            }
            if (IsTier2 && BonusMode == BonusMode.NORMAL) Tier2SpinCount++;

            if (DebugForceFlag.HasValue)
            {
                CurrentFlag = DebugForceFlag.Value;
                DebugForceFlag = null;
                if (CurrentFlag.IsBonus() && BonusMode == BonusMode.NORMAL) HeldBonusFlag = CurrentFlag;
                return;
            }

            if (BonusMode != BonusMode.NORMAL)
            {
                int r = _rng.Next(LotteryTable.Denominator);
                CurrentRng = r + 1;
                var t = BonusMode == BonusMode.BB ? _tBB : _tRB;
                CurrentFlag = t != null && t.IsValid ? t[r] : Flag.HAZE;
                return;
            }

            // ボーナス持ち越し中: 小役優先。小役を引けば小役を狙い（ボーナスは揃えない）、
            // ハズレ／ボーナス役／リーチ目ならボーナスを狙う。G数は進めない（Web版と同じ）
            if (HeldBonusFlag != Flag.HAZE)
            {
                int r2 = _rng.Next(LotteryTable.Denominator);
                CurrentRng = r2 + 1;
                var drawn = CurrentTable()[r2];
                CurrentFlag = (drawn == Flag.HAZE || drawn.IsBonus() || drawn.IsReachMe()) ? HeldBonusFlag : drawn;
                return;
            }

            SpinCount++;
            TotalSpinCount++;
            if (SpinCount >= Config.CeilingFor(Mode))
            {
                // 天井: BB_A / RB_A を 1:1
                var forced = _rng.NextDouble() < 0.5 ? Flag.BB_A : Flag.RB_A;
                CurrentFlag = forced;
                HeldBonusFlag = forced;
                return;
            }

            int rng = _rng.Next(LotteryTable.Denominator);
            CurrentRng = rng + 1;
            CurrentFlag = CurrentTable()[rng];
            if (CurrentFlag.IsBonus()) HeldBonusFlag = CurrentFlag;
        }

        private LotteryTable CurrentTable()
        {
            if (IsTier2) return Tier2TableFor(ActiveEnemyTable) ?? _tA;
            switch (Mode)
            {
                case Mode.B: return _tB;
                case Mode.C: return _tC;
                case Mode.D: return _tD;
                default: return _tA;
            }
        }

        // --------------------------------------------------------------- STOP
        /// <summary>onStop。baseIdx は押した瞬間に上段にあるコマ番号。</summary>
        public SlipController.Result Stop(int reelIndex, int baseIdx, int maxSlip = SlipController.DefaultMaxSlip)
        {
            if (!IsGameActive) throw new InvalidOperationException("回転していない");
            if (Stopped[reelIndex] != null) throw new InvalidOperationException("停止済み");
            // 小役優先: 小役を狙うゲームではボーナス図柄を同時に揃えない
            var held = CurrentFlag == HeldBonusFlag ? HeldBonusFlag : Flag.HAZE;
            var idx = new[] { Stopped[0] == null ? -1 : StopIndex[0], Stopped[1] == null ? -1 : StopIndex[1], Stopped[2] == null ? -1 : StopIndex[2] };
            var res = SlipController.Stop(Strips, reelIndex, baseIdx, CurrentFlag, held, idx, maxSlip);
            Stopped[reelIndex] = res.symbols;
            StopIndex[reelIndex] = res.stopIndex;
            Slip[reelIndex] = res.slip;
            PressOrder.Add(reelIndex);
            if (Navi.Active)
            {
                if (PressOrder.Count == 1) Navi.InChoice = reelIndex == Navi.first;
                else if (PressOrder.Count == 2 && Navi.InChoice)
                {
                    Navi.InChoice = false;
                    CurrentCommand = reelIndex == Navi.correctReel ? BellCommand.Success : BellCommand.Fail;
                }
            }
            return res;
        }

        public bool AllStopped => Stopped[0] != null && Stopped[1] != null && Stopped[2] != null;

        // ----------------------------------------------------------- EVALUATE
        /// <summary>evaluateWin。全リール停止後に呼ぶ。</summary>
        public GameResult Evaluate()
        {
            if (!AllStopped) throw new InvalidOperationException("全リール停止前");
            IsGameActive = false;

            var result = new GameResult { flag = CurrentFlag, hint = HintKind.None };
            var payouts = CurrentPayouts;
            var win = WinEvaluator.Evaluate(Stopped, BonusMode, payouts);
            result.win = win;

            if (win.bonusWon)
            {
                HeldBonusFlag = Flag.HAZE;
                if (win.winType == WinType.BIG) { BonusMode = BonusMode.BB; BonusPayoutTarget = payouts.BIG; }
                else { BonusMode = BonusMode.RB; BonusPayoutTarget = payouts.REG; }
                BonusEarned = 0;
                Mode = ModeTransition.Determine(Config, "bonus", Setting, _rng);
                result.bonusStarted = true;
                result.bonusModeAfter = BonusMode;
                Bet = 0;
                return result;   // JS はここで return（ワークフロー抽選なし）
            }

            if (BonusMode != BonusMode.NORMAL)
            {
                if (win.payout > 0) BonusEarned += win.payout;
                if (BonusEarned >= BonusPayoutTarget)
                {
                    BonusMode = BonusMode.NORMAL;
                    BonusEarned = 0;
                    BonusPayoutTarget = 0;
                    SpinCount = 0;
                    result.bonusEnded = true;
                }
            }
            result.bonusModeAfter = BonusMode;

            if (win.payout > 0)
            {
                Credit += win.payout;
                Bet = 0;
                if (win.winType == WinType.BELL && Navi.Active && CurrentCommand != BellCommand.None)
                {
                    result.command = CurrentCommand;
                    if (CurrentCommand == BellCommand.Success)
                    {
                        var bc = Config.bellCommand;
                        if (bc == null || bc.successGuaranteesDefeat) { result.naviDefeatGuaranteed = !EnemyDefeatWon; EnemyDefeatWon = true; }
                        int exp = bc?.successExp ?? 25;
                        if (exp > 0) { result.naviExp = exp; if (GainExp(exp)) result.levelUp = true; }
                    }
                    else RollDefeat(win.winType);   // 失敗: 通常の討伐抽選だけ
                }
                else RollDefeat(win.winType);
            }
            else if (win.isReplay)
            {
                Bet = BetCost;
                IsReplay = true;
                RollDefeat(WinType.REPLAY);
            }
            else
            {
                Bet = 0;
            }

            // Tier2 決着（tier2MaxSpins ゲーム目の終わり）
            if (IsTier2 && Tier2SpinCount >= Config.tier2MaxSpins)
            {
                result.enemyResolved = EnemyDefeatWon;
                if (EnemyDefeatWon) result.levelUp = GainExp(Config.expPerDefeat);
                IsTier2 = false;
                EnemyActive = false;
            }

            // ワークフロー抽選（JS: リプレイ時も実行される）
            var roleKey = WorkflowLottery.RoleKey(win.winType, win.isReplay);
            result.workflow = WorkflowLottery.Run(Workflow, roleKey, _rng);
            if (DebugForceEnemy && !EnemyActive && PrecursorRemaining == 0)
            {
                result.workflow = new WorkflowResult { roleKey = roleKey, category = "ENEMY", variant = "A" };
                DebugForceEnemy = false;
            }

            // 前兆の進行（ENEMY 当選 → N G 煽って N G 目の終わりに出現）
            if (PrecursorRemaining > 0)
            {
                PrecursorRemaining--;
                result.precursorStage = PrecursorTotal - PrecursorRemaining;
                if (PrecursorRemaining == 0)
                {
                    EnemyActive = true;
                    PendingTier2 = true;
                    EnemyDefeatWon = false;
                    result.enemySpawned = true;
                    result.enemyTable = ActiveEnemyTable;
                }
            }
            else if (result.workflow.category == "ENEMY" && !EnemyActive)
            {
                ActiveEnemyTable = EnemyEngage.SelectTable(EnemyTables, result.workflow.variant, _rng);
                result.enemyTable = ActiveEnemyTable;
                int n = Math.Max(1, Config.enemyPrecursorSpins);
                PrecursorTotal = n;
                PrecursorRemaining = n;
                result.precursorStarted = true;
                result.precursorStage = 0;
            }
            return result;
        }

        private bool RollDefeat(WinType winType, float multiplier = 1f)
        {
            if (!IsTier2 || ActiveEnemyTable == null || !EnemyActive) return false;
            if (EnemyDefeatWon) return false;
            if (EnemyEngage.RollDefeat(ActiveEnemyTable, winType, _rng, multiplier)) EnemyDefeatWon = true;
            return EnemyDefeatWon;
        }

        private bool GainExp(int amount)
        {
            PlayerExp += amount;
            int need = PlayerLevel * 100;
            if (PlayerExp >= need)
            {
                PlayerLevel++;
                PlayerExp -= need;
                return true;
            }
            return false;
        }
    }
}
