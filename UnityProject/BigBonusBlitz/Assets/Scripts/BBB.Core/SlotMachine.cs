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
        /// <summary>このGの終わりの AT 期待度 %（ボーナス中のみ）。</summary>
        public int atExpectPercent;
        /// <summary>このGで上がった期待度 %（0 なら上がらなかった）。</summary>
        public int atExpectGained;
        /// <summary>敵を討伐して得た EXP（中ボスは多い）。</summary>
        public int enemyExp;
        /// <summary>このGで得たソウル。</summary>
        public int soulsGained;
        // --- AT ---
        /// <summary>このGで AT に当選した（次Gから開始）。</summary>
        public bool atWon;
        /// <summary>このGで AT が始まった / 終わった。</summary>
        public bool atStarted, atEnded;
        /// <summary>このGでバトルに当選した。</summary>
        public bool battleStarted;
        public MonsterDef battleMonster;
        /// <summary>このGで与えたダメージ（バトル中のみ）。</summary>
        public int battleDamage;
        /// <summary>バトル決着: null=継続, true=討伐, false=失敗。</summary>
        public bool? battleResolved;
        /// <summary>このGで上乗せしたG数。</summary>
        public int atSpinsAdded;
        /// <summary>押し順ナビに正解したか（AT 道中のベルのみ。null=対象外）。</summary>
        public bool? naviCorrect;
        // --- 冒険（ステージ制マップ）---
        /// <summary>このGでルートが決まった／上位に書き換わった（行き先のステージid。null なら無し）。</summary>
        public string routeDecided;
        /// <summary>このGで見つけた宝（null なら無し）。</summary>
        public TreasureDef treasure;
        /// <summary>このGでステージが変わった（stageFrom → stageTo）。</summary>
        public bool stageChanged;
        public string stageFrom, stageTo;
        /// <summary>このGで章をクリアした（次Gから最初のステージに戻る）。</summary>
        public bool chapterCleared;
        /// <summary>章クリアで得たソウル。</summary>
        public int chapterSouls;
        /// <summary>ステージ到達時にボスの前兆が始まった。</summary>
        public bool bossAmbush;
        /// <summary>AT 開始時に宝の貯金から上乗せしたG数。</summary>
        public int atStockUsed;
        /// <summary>このGで松明が 1 本増えた。</summary>
        public bool torchRefilled;
        /// <summary>このGで松明が尽きた（街へ帰る）。</summary>
        public bool outOfTorch;
        /// <summary>このGでクレジットが尽きた（力尽きて街へ帰る）。</summary>
        public bool ranOutOfCredit;
        /// <summary>街へ強制帰還する（"torch" = 松明切れ / "credit" = 力尽き）。</summary>
        public bool returnedToTown;
        public string returnReason;
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
        /// <summary>ボーナス成立の前兆 残りG（0 なら前兆中でない）。0 になったGが擬似遊技。</summary>
        public int BonusAnnounceRemaining;
        /// <summary>その前兆の総G数（演出の段階計算用）。</summary>
        public int BonusAnnounceTotal;
        /// <summary>今Gが擬似遊技（機械が自動で揃える）か。レバーオンで決まる。</summary>
        public bool PseudoPlay;
        public BonusMode BonusMode = BonusMode.NORMAL;
        public int BonusPayoutTarget;
        public int BonusEarned;
        // --- AT「洞窟」 ---
        /// <summary>AT 中か。</summary>
        public bool InAt;
        /// <summary>次Gから AT を開始する。</summary>
        public bool PendingAt;
        /// <summary>洞窟に入るまでの前兆 残りG（0 なら前兆中でない）。</summary>
        public int AtEntryRemaining;
        /// <summary>その前兆の総G数。</summary>
        public int AtEntryTotal;
        /// <summary>今Gの洞窟前兆の段階（1..N）。前兆中でなければ 0。</summary>
        public int AtEntryStage;
        /// <summary>AT の残りG。</summary>
        public int AtSpinsRemaining;
        /// <summary>この AT で回した総G数と獲得枚数（表示用）。</summary>
        public int AtSpinCount, AtPayout;
        /// <summary>道中の押し順ナビ（Active=false なら無し）。</summary>
        public AtNavi Navi2;
        /// <summary>バトル（狩猟）中か。</summary>
        public bool InBattle;
        public MonsterDef BattleMonster;
        public int BattleHp, BattleHpMax, BattleSpinsRemaining;
        /// <summary>このGで AT が始まった（レバーオン時点で立つ）。</summary>
        public bool AtJustStarted;

        /// <summary>ボーナス中の AT 期待度 %（枠の点滅色に対応）。ボーナス開始でリセットされる。</summary>
        public int AtExpectPercent;
        /// <summary>直前のGで期待度が上がった分（0 なら上がらなかった）。演出の昇格アピール用。</summary>
        public int AtExpectGained;

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
        /// <summary>Tier2中の小役連続回数（ハズレでリセット）。討伐率に defeatStreakBonus × 回数 を加算。</summary>
        public int DefeatStreak;
        public EnemyTable ActiveEnemyTable;
        /// <summary>ソウルと持ち物。ショップの効果はここから読む。</summary>
        public readonly PlayerWallet Wallet = new PlayerWallet();
        public int PlayerLevel = 1;
        public int PlayerExp;
        /// <summary>冒険の進行（現在のステージ・残りG・決まったルート）。</summary>
        public readonly AdventureState Adv = new AdventureState();
        public bool AdventureEnabled => Config.adventure != null && Config.adventure.enabled && Config.adventure.nodes != null && Config.adventure.nodes.Count > 0;
        /// <summary>現在のステージ（冒険が無効なら null）。</summary>
        public StageNode CurrentStage => AdventureEnabled ? Config.adventure.Find(Adv.nodeId) : null;
        /// <summary>松明 1 本で進めるG数（装備の効果込み）。</summary>
        public int TorchSpinsPerUnit => Math.Max(1, (Config.adventure?.resource?.spinsPerTorch ?? 60)
            + ShopDirector.EffectTotal(Config.shop, Wallet, ShopEffects.TorchSpins));

        // --- デバッグ用（main.js の debug-force-flag 相当） ---
        /// <summary>次のレバーでこのフラグを強制する（null で通常抽選）。1回で解除。</summary>
        public Flag? DebugForceFlag;
        /// <summary>次の判定でワークフローを ENEMY 当選扱いにする（敵未出現時のみ）。1回で解除。</summary>
        public bool DebugForceEnemy;

        public readonly Symbol[][] Stopped = new Symbol[3][];
        public readonly int[] StopIndex = new int[3];
        public readonly int[] Slip = new int[3];
        public bool IsGameActive { get; private set; }

        private LotteryTable _tA, _tB, _tC, _tD, _tBB, _tRB, _tTier2, _tAT;
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
            if (AdventureEnabled)
            {
                AdventureDirector.Reset(Config.adventure, Adv);
                AdventureDirector.ResetTorches(Config.adventure, Adv, TorchSpinsPerUnit);
            }
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
            _tAT = Config.probabilities_AT != null ? new LotteryTable(Config.probabilities_AT) : null;
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
            Navi2 = default;
            AtJustStarted = false;

            var hint = HintKind.None;
            if (IsTier2 && EnemyDefeatWon && ActiveEnemyTable != null)
                hint = EnemyEngage.RollHint(ActiveEnemyTable, Config.defaultHintConfig, _rng);

            DrawLottery();

            // AT 道中の押し順ナビ: 正解のリールを「教える」表示（エンゲージの択ナビとは意味が逆）
            if (InAt && !InBattle && BonusMode == BonusMode.NORMAL && CurrentFlag.IsBell())
                Navi2 = new AtNavi { Active = true, first = _rng.Next(3) };

            // ベル択ナビ: エンゲージ中のベル当選時だけ
            if (IsTier2 && EnemyActive && !InAt && BonusMode == BonusMode.NORMAL && CurrentFlag.IsBell())
            {
                // 第一停止は常に「中」。左右のどちらが正解かはランダム（隠し）
                Navi = new BellNavi { first = 1, correctReel = _rng.NextDouble() * 100 < Config.bellCommand.correctLeftRate ? 0 : 2, Active = true, InChoice = false };
            }
            return hint;
        }

        private void DrawLottery()
        {
            PseudoPlay = false;
            if (PendingTier2)
            {
                IsTier2 = true;
                Tier2SpinCount = 0;
                PendingTier2 = false;
            }
            // エンゲージはボーナス中も進める（止めると「残り G」が凍って見える）。ベル択ナビだけは通常時限定
            // ボーナスが始まったGは判定を通らず決着処理が飛ぶので、上限で止めて「残り -1 G」を防ぐ
            if (IsTier2) Tier2SpinCount = Math.Min(Tier2SpinCount + 1, Math.Max(1, Config.tier2MaxSpins));

            // 洞窟に入るまでの前兆。消化しきった次のGで AT が始まる
            AtEntryStage = 0;
            if (AtEntryRemaining > 0 && BonusMode == BonusMode.NORMAL && !InAt)
            {
                AtEntryRemaining--;
                AtEntryStage = AtEntryTotal - AtEntryRemaining;
                if (AtEntryRemaining == 0) PendingAt = true;
            }

            // AT「洞窟」の開始とG消化
            if (PendingAt)
            {
                PendingAt = false;
                InAt = true;
                AtJustStarted = true;
                var atStart = Config.at ?? new AtConfig();
                AtSpinsRemaining = Math.Max(1, atStart.initialSpins + ShopDirector.EffectTotal(Config.shop, Wallet, ShopEffects.AtInitialSpins));
                // 宝で貯めた上乗せG（洞窟の地図など）はここで使い切る
                _atStockUsed = Math.Max(0, Adv.stockAtSpins);
                AtSpinsRemaining += _atStockUsed;
                Adv.stockAtSpins = 0;
                AtSpinCount = 0; AtPayout = 0;
                InBattle = false; BattleMonster = null; BattleHp = 0; BattleHpMax = 0; BattleSpinsRemaining = 0;
                // 洞窟に入ったら通常時のエンゲージは持ち込まない（択ナビと押し順ナビが重なるため）
                IsTier2 = false; PendingTier2 = false; EnemyActive = false; EnemyDefeatWon = false;
                Tier2SpinCount = 0; PrecursorRemaining = 0; PrecursorTotal = 0; ActiveEnemyTable = null;
            }
            if (InAt && BonusMode == BonusMode.NORMAL)
            {
                AtSpinCount++;
                var atc = Config.at ?? new AtConfig();
                // 狩猟中は既定でATのGを消費しない（battleConsumesAtSpins で変えられる）
                if ((!InBattle || atc.battleConsumesAtSpins) && AtSpinsRemaining > 0) AtSpinsRemaining--;
            }

            if (DebugForceFlag.HasValue)
            {
                CurrentFlag = DebugForceFlag.Value;
                DebugForceFlag = null;
                if (CurrentFlag.IsBonus() && BonusMode == BonusMode.NORMAL) HoldBonus(CurrentFlag);
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
                // 前兆を1G進める。0 になったGが擬似遊技（小役を無視してボーナスを揃えにいく）
                if (BonusAnnounceRemaining > 0)
                {
                    BonusAnnounceRemaining--;
                    PseudoPlay = BonusAnnounceRemaining == 0;
                }
                int r2 = _rng.Next(LotteryTable.Denominator);
                CurrentRng = r2 + 1;
                if (PseudoPlay) { CurrentFlag = HeldBonusFlag; return; }
                var drawn = CurrentTable()[r2];
                CurrentFlag = (drawn == Flag.HAZE || drawn.IsBonus() || drawn.IsReachMe()) ? HeldBonusFlag : drawn;
                return;
            }

            SpinCount++;
            TotalSpinCount++;
            if (AdventureEnabled && !InAt)
            {
                if (Adv.spinsLeft > 0) Adv.spinsLeft--;
                if (AdventureDirector.BurnTorch(Config.adventure, Adv, TorchSpinsPerUnit)) _torchOut = true;
            }
            if (SpinCount >= Config.CeilingFor(Mode))
            {
                // 天井: game_config.ceilingBonus の重みで成立ボーナスを選ぶ
                var forced = WeightedFlag(Config.ceilingBonus, Flag.BB_A);
                CurrentFlag = forced;
                HoldBonus(forced);
                return;
            }

            int rng = _rng.Next(LotteryTable.Denominator);
            CurrentRng = rng + 1;
            CurrentFlag = CurrentTable()[rng];
            if (CurrentFlag.IsBonus()) HoldBonus(CurrentFlag);
        }

        /// <summary>ボーナスを持ち越し状態にし、前兆G数を抽選する。</summary>
        private void HoldBonus(Flag f)
        {
            HeldBonusFlag = f;
            BonusAnnounceTotal = WeightedInt(Config.bonusPrecursorSpins, 3);
            BonusAnnounceRemaining = BonusAnnounceTotal;
        }

        /// <summary>「数値の文字列 → 重み」の表から 1 つ選ぶ。表が空・不正なら fallback。</summary>
        private int WeightedInt(Dictionary<string, int> weights, int fallback)
        {
            if (weights == null || weights.Count == 0) return fallback;
            int total = 0;
            foreach (var kv in weights) if (int.TryParse(kv.Key, out _)) total += Math.Max(0, kv.Value);
            if (total <= 0) return fallback;
            int r = _rng.Next(total);
            foreach (var kv in weights)
            {
                if (!int.TryParse(kv.Key, out var n)) continue;
                int w = Math.Max(0, kv.Value);
                if (r < w) return Math.Max(1, n);
                r -= w;
            }
            return fallback;
        }

        /// <summary>フラグ名 → 重み の表から 1 つ選ぶ。表が空・不正なら fallback。</summary>
        private Flag WeightedFlag(Dictionary<string, int> weights, Flag fallback)
        {
            if (weights == null || weights.Count == 0) return fallback;
            int total = 0;
            foreach (var kv in weights) if (Enum.TryParse<Flag>(kv.Key, out _)) total += Math.Max(0, kv.Value);
            if (total <= 0) return fallback;
            int r = _rng.Next(total);
            foreach (var kv in weights)
            {
                if (!Enum.TryParse<Flag>(kv.Key, out var f)) continue;
                int w = Math.Max(0, kv.Value);
                if (r < w) return f;
                r -= w;
            }
            return fallback;
        }

        private LotteryTable CurrentTable()
        {
            if (InAt) return _tAT ?? _tTier2 ?? _tA;
            if (IsTier2) return Tier2TableFor(ActiveEnemyTable) ?? _tA;
            var stage = CurrentStage;
            var mode = Mode;
            if (stage != null && stage.HasModeOverride && Enum.TryParse<Mode>(stage.mode, out var sm)) mode = sm;
            switch (mode)
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
            // ベル択ナビ（エンゲージ）の2択を外したら、そのGのベルは取りこぼす。
            // 制御に渡す役を HAZE にすることで「揃えない」停止を選ばせる（払い出しは自然に 0 になる）。
            // 外したかどうかは押したリール番号だけで決まるので、その第二停止から効かせられる。
            bool naviMissed = Navi.Active && CurrentFlag.IsBell() &&
                (CurrentCommand == BellCommand.Fail ||
                 (PressOrder.Count == 1 && Navi.InChoice && reelIndex != Navi.correctReel));
            var flagForStop = naviMissed ? Flag.HAZE : CurrentFlag;

            // 小役優先: 小役を狙うゲームではボーナス図柄を同時に揃えない
            var held = CurrentFlag == HeldBonusFlag ? HeldBonusFlag : Flag.HAZE;
            var idx = new[] { Stopped[0] == null ? -1 : StopIndex[0], Stopped[1] == null ? -1 : StopIndex[1], Stopped[2] == null ? -1 : StopIndex[2] };
            // 持ち越しボーナスを狙うGは引き込みを広げる（既定 20 コマ = 目押し不要。game_config.bonusPullInSlip）。
            // ただし前兆中は引き込みを切る＝実機どおりの目押しにする。察した人だけが擬似遊技を待たずに揃えられる
            bool pullIn = held != Flag.HAZE && CurrentFlag.IsBonus() && BonusAnnounceRemaining <= 0;
            int slipMax = pullIn ? Math.Min(Strips[reelIndex].Length - 1, Math.Max(maxSlip, Config.bonusPullInSlip)) : maxSlip;
            var res = SlipController.Stop(Strips, reelIndex, baseIdx, flagForStop, held, idx, slipMax);
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

            var result = new GameResult { flag = CurrentFlag, hint = HintKind.None, atStockUsed = _atStockUsed };
            _atStockUsed = 0;
            var payouts = CurrentPayouts;
            var win = WinEvaluator.Evaluate(Stopped, BonusMode, payouts);
            // AT 道中の押し順ベル: ナビ通りに第一停止できたかで払い出しが変わる（番長型）
            if (InAt && !InBattle && BonusMode == BonusMode.NORMAL && Navi2.Active && win.winType == WinType.BELL)
            {
                var atNavi = Config.at ?? new AtConfig();
                bool ok = PressOrder.Count > 0 && PressOrder[0] == Navi2.first;
                win.payout = Math.Max(0, ok ? atNavi.naviCorrectPayout : atNavi.naviWrongPayout);
                result.naviCorrect = ok;
            }
            result.win = win;

            if (win.bonusWon)
            {
                HeldBonusFlag = Flag.HAZE;
                BonusAnnounceRemaining = 0; BonusAnnounceTotal = 0; PseudoPlay = false;
                if (win.winType == WinType.BIG) { BonusMode = BonusMode.BB; BonusPayoutTarget = payouts.BIG; }
                else { BonusMode = BonusMode.RB; BonusPayoutTarget = payouts.REG; }
                BonusEarned = 0;
                // AT 期待度はボーナスごとに 0（設定値）から積み直す
                var atx = Config.atExpect ?? new AtExpectConfig();
                AtExpectPercent = Math.Min(Math.Max(1, atx.maxPercent),
                    atx.startPercent + ShopDirector.EffectTotal(Config.shop, Wallet, ShopEffects.AtStartPercent) + Math.Max(0, Adv.stockAtExpect));
                Adv.stockAtExpect = 0;
                AtExpectGained = 0;
                Mode = ModeTransition.Determine(Config, "bonus", Setting, _rng);
                result.bonusStarted = true;
                result.bonusModeAfter = BonusMode;
                Bet = 0;
                return result;   // JS はここで return（ワークフロー抽選なし）
            }

            AtExpectGained = 0;
            if (BonusMode != BonusMode.NORMAL)
            {
                // ボーナス中は毎G、役ごとに AT 期待度の上乗せを抽選する（枠の点滅色が上がる）
                var at = Config.atExpect ?? new AtExpectConfig();
                int gain = AtDirector.RollGain(at, CurrentFlag, _rng);
                if (gain > 0)
                {
                    int before = AtExpectPercent;
                    AtExpectPercent = Math.Min(Math.Max(1, at.maxPercent), AtExpectPercent + gain);
                    AtExpectGained = AtExpectPercent - before;
                }
                result.atExpectPercent = AtExpectPercent;
                result.atExpectGained = AtExpectGained;

                if (win.payout > 0) BonusEarned += win.payout;
                if (BonusEarned >= BonusPayoutTarget)
                {
                    BonusMode = BonusMode.NORMAL;
                    BonusEarned = 0;
                    BonusPayoutTarget = 0;
                    SpinCount = 0;
                    result.bonusEnded = true;
                    // AT 抽選: 溜めた期待度をそのまま当選率にする（枠の色が嘘にならないように）
                    var atEntry = Config.at ?? new AtConfig();
                    int atRate = atEntry.useExpectAsRate ? AtExpectPercent : atEntry.flatRate;
                    if (!InAt && !PendingAt && AtEntryRemaining <= 0 && _rng.NextDouble() * 100 < atRate)
                    {
                        // すぐには始めず、洞窟に入るまでの前兆を挟む
                        AtEntryTotal = WeightedInt(atEntry.entryPrecursorSpins, 3);
                        AtEntryRemaining = AtEntryTotal;
                        result.atWon = true;
                    }
                }
            }
            result.bonusModeAfter = BonusMode;

            // 2択の結果は「揃ったかどうか」に関係なく残す（外すとベルはこぼれて払い出し 0 になるため）
            if (Navi.Active && CurrentCommand != BellCommand.None) result.command = CurrentCommand;

            if (win.payout > 0)
            {
                Credit += win.payout;
                Bet = 0;
                if (win.winType == WinType.BELL && Navi.Active && CurrentCommand == BellCommand.Success)
                {
                    var bc = Config.bellCommand;
                    if (bc == null || bc.successGuaranteesDefeat) { result.naviDefeatGuaranteed = !EnemyDefeatWon; EnemyDefeatWon = true; }
                    int exp = bc?.successExp ?? 25;
                    if (exp > 0) { result.naviExp = exp; if (GainExp(exp)) result.levelUp = true; }
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
                if (IsTier2) DefeatStreak = 0;
            }

            // Tier2 決着（tier2MaxSpins ゲーム目の終わり）
            if (IsTier2 && Tier2SpinCount >= Config.tier2MaxSpins)
            {
                result.enemyResolved = EnemyDefeatWon;
                var soulCfg = Config.souls ?? new SoulConfig();
                if (EnemyDefeatWon)
                {
                    int exp = ActiveEnemyTable != null && ActiveEnemyTable.expOnDefeat > 0 ? ActiveEnemyTable.expOnDefeat : Config.expPerDefeat;
                    result.enemyExp = ApplyExpBonus(exp);
                    result.levelUp = GainExp(result.enemyExp);
                    result.soulsGained += GainSouls(ActiveEnemyTable != null && ActiveEnemyTable.IsBoss ? soulCfg.perBoss : soulCfg.perMob);
                }
                else result.soulsGained += GainSouls(soulCfg.perEscape);
                IsTier2 = false;
                EnemyActive = false;
            }

            // --- AT「洞窟」: 道中のバトル抽選と、狩猟中のダメージ判定 ---
            if (InAt && BonusMode == BonusMode.NORMAL)
            {
                var atc = Config.at ?? new AtConfig();
                AtPayout += win.payout;
                if (InBattle)
                {
                    int dmg = AtDirectorEx.RollDamage(atc, CurrentFlag, _rng);
                    int dmgBonus = ShopDirector.EffectTotal(Config.shop, Wallet, ShopEffects.BattleDamage);
                    if (dmg > 0 && dmg < 999 && dmgBonus > 0) dmg = dmg * (100 + dmgBonus) / 100;
                    bool oneShot = dmg >= 999;
                    result.battleDamage = oneShot ? BattleHp : Math.Min(dmg, BattleHp);
                    BattleHp = oneShot ? 0 : Math.Max(0, BattleHp - dmg);
                    if (BattleSpinsRemaining > 0) BattleSpinsRemaining--;
                    if (BattleHp <= 0)
                    {
                        result.battleResolved = true;
                        result.battleMonster = BattleMonster;
                        result.soulsGained += GainSouls((Config.souls ?? new SoulConfig()).perAtBattle);
                        int add = Math.Max(0, BattleMonster?.rewardSpins ?? 0);
                        AtSpinsRemaining += add;
                        result.atSpinsAdded = add;
                        InBattle = false;
                    }
                    else if (BattleSpinsRemaining <= 0)
                    {
                        result.battleResolved = false;
                        result.battleMonster = BattleMonster;
                        int add = Math.Max(0, atc.failRewardSpins);
                        AtSpinsRemaining += add;
                        result.atSpinsAdded = add;
                        InBattle = false;
                    }
                }
                else if (AtDirectorEx.RollBattle(atc, CurrentFlag, _rng))
                {
                    BattleMonster = AtDirectorEx.PickMonster(atc, _rng);
                    BattleHpMax = Math.Max(1, BattleMonster.hp);
                    BattleHp = BattleHpMax;
                    BattleSpinsRemaining = Math.Max(1, BattleMonster.spins);
                    InBattle = true;
                    result.battleStarted = true;
                    result.battleMonster = BattleMonster;
                }
                if (!InBattle && AtSpinsRemaining <= 0)
                {
                    InAt = false;
                    result.atEnded = true;
                }
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
            else if ((result.workflow.category == "ENEMY" || RollStageEngage()) && !EnemyActive && !InAt)   // AT 中は洞窟のバトルが主役なので通常の敵は出さない
            {
                DefeatStreak = 0;
                // ボーナス中は中ボス、通常時は雑魚から選ぶ（高確ステージの追加当選は variant A）
                string variant = result.workflow.category == "ENEMY" ? result.workflow.variant : "A";
                ActiveEnemyTable = EnemyEngage.SelectTable(EnemyTables, variant, _rng, BonusMode != BonusMode.NORMAL);
                result.enemyTable = ActiveEnemyTable;
                int n = Math.Max(1, Config.enemyPrecursorSpins);
                PrecursorTotal = n;
                PrecursorRemaining = n;
                result.precursorStarted = true;
                result.precursorStage = 0;
            }

            AdvanceAdventure(result);
            CheckAdventureReturn(result);
            return result;
        }

        private int _atStockUsed;
        private bool _torchOut;

        /// <summary>クレジットが尽きたら「力尽きた」として街へ帰す（松明切れは AdvanceAdventure 側）。</summary>
        private void CheckAdventureReturn(GameResult result)
        {
            if (!AdventureEnabled || Adv.MustReturn) return;
            if (Credit >= BetCost || IsReplay) return;
            Adv.returnReason = "credit";
            result.ranOutOfCredit = true;
            result.returnedToTown = true;
            result.returnReason = "credit";
        }

        /// <summary>高確ステージの追加エンゲージ抽選（ワークフローで ENEMY にならなかったGだけ呼ばれる）。</summary>
        private bool RollStageEngage()
        {
            var stage = CurrentStage;
            if (stage == null || stage.engageBoost <= 0) return false;
            if (BonusMode != BonusMode.NORMAL || InAt || HeldBonusFlag != Flag.HAZE || PrecursorRemaining > 0) return false;
            return _rng.NextDouble() * 100 < stage.engageBoost;
        }

        /// <summary>
        /// 冒険の 1G 分の進行。通常時（ボーナス・持ち越し・AT 以外）に、役でルート抽選と宝抽選を行い、
        /// ステージの残りGが尽きていて状態がきれいなら次のステージへ進む（終点なら章クリア）。
        /// </summary>
        private void AdvanceAdventure(GameResult result)
        {
            if (!AdventureEnabled) return;
            var cfg = Config.adventure;
            if (CurrentStage == null) AdventureDirector.Reset(cfg, Adv);

            bool normalPlay = BonusMode == BonusMode.NORMAL && !InAt && HeldBonusFlag == Flag.HAZE;
            if (normalPlay)
            {
                string key = PrecogDirector.RoleKey(CurrentFlag);   // こぼしても抽選はフラグで行う
                result.routeDecided = AdventureDirector.RollRoute(cfg, Adv, key, _rng);
                if (AdventureDirector.RollRefill(cfg, Adv, key, _rng, TorchSpinsPerUnit)) result.torchRefilled = true;
                var t = AdventureDirector.RollTreasure(cfg, Adv, key, _rng);
                if (t != null)
                {
                    result.treasure = t;
                    Adv.treasuresFound++;
                    switch (t.kind)
                    {
                        case "atSpins": Adv.stockAtSpins += Math.Max(0, t.amount); break;
                        case "atExpect": Adv.stockAtExpect += Math.Max(0, t.amount); break;
                        case "exp": if (GainExp(ApplyExpBonus(t.amount))) result.levelUp = true; break;
                        case "torch": if (AdventureDirector.AddTorch(cfg, Adv, Math.Max(1, t.amount), TorchSpinsPerUnit) > 0) result.torchRefilled = true; break;
                        default: result.soulsGained += GainSouls(t.amount); break;
                    }
                }
            }

            // ステージ移動は「何も抱えていない」Gの終わりだけ（AT・ボーナス・前兆・敵戦闘の途中では動かない）
            bool clean = normalPlay && !IsTier2 && !PendingTier2 && !EnemyActive && PrecursorRemaining == 0
                         && AtEntryRemaining == 0 && !PendingAt;

            // 帰るまでの間に宝やレア役で松明を拾ったら、帰らなくてよい
            if (_torchOut && Adv.torches > 0) _torchOut = false;

            // 松明が尽きた: ボーナスや戦闘が終わって落ち着いてから街へ帰す
            if (_torchOut && clean)
            {
                _torchOut = false;
                Adv.returnReason = "torch";
                result.outOfTorch = true;
                result.returnedToTown = true;
                result.returnReason = "torch";
                return;
            }
            if (Adv.spinsLeft > 0 || !clean) return;

            string next = AdventureDirector.ResolveNext(cfg, Adv, _rng);
            result.stageFrom = Adv.nodeId;
            if (next == null)
            {
                result.chapterCleared = true;
                result.chapterSouls = GainSouls(cfg.chapterClearSouls);
                result.soulsGained += result.chapterSouls;
                if (AdventureDirector.AddTorch(cfg, Adv, cfg.chapterClearTorches, TorchSpinsPerUnit) > 0) result.torchRefilled = true;
                Adv.chapter++;
                Adv.treasuresFound = 0;
                AdventureDirector.Reset(cfg, Adv);
                result.stageTo = Adv.nodeId;
                return;
            }
            AdventureDirector.Enter(cfg, Adv, next);
            result.stageChanged = true;
            result.stageTo = next;

            // ボスステージ: 到達した瞬間に中ボスの前兆が始まる
            var node = cfg.Find(next);
            if (node != null && node.bossRate > 0 && _rng.NextDouble() * 100 < node.bossRate)
            {
                DefeatStreak = 0;
                ActiveEnemyTable = EnemyEngage.SelectTable(EnemyTables, "A", _rng, true);
                result.enemyTable = ActiveEnemyTable;
                int n = Math.Max(1, Config.enemyPrecursorSpins);
                PrecursorTotal = n;
                PrecursorRemaining = n;
                result.precursorStarted = true;
                result.precursorStage = 0;
                result.bossAmbush = true;
            }
        }

        private bool RollDefeat(WinType winType, float multiplier = 1f)
        {
            if (!IsTier2 || ActiveEnemyTable == null || !EnemyActive) return false;
            if (EnemyDefeatWon) return false;
            int skillBonus = ShopDirector.EffectTotal(Config.shop, Wallet, ShopEffects.DefeatBonus);
            if (EnemyEngage.RollDefeat(ActiveEnemyTable, winType, _rng, multiplier, DefeatStreak, Config.defeatStreakBonus, skillBonus)) EnemyDefeatWon = true;
            DefeatStreak++;
            return EnemyDefeatWon;
        }

        /// <summary>ソウルを加算する（ショップの取得量アップを掛ける）。実際に加えた量を返す。</summary>
        private int GainSouls(int baseAmount)
        {
            if (baseAmount <= 0) return 0;
            int bonus = ShopDirector.EffectTotal(Config.shop, Wallet, ShopEffects.SoulGain);
            int amount = Math.Max(0, baseAmount * (100 + bonus) / 100);
            Wallet.Souls += amount;
            Wallet.TotalSouls += amount;
            return amount;
        }

        /// <summary>EXP にショップの取得量アップを掛ける。</summary>
        private int ApplyExpBonus(int baseExp)
        {
            if (baseExp <= 0) return 0;
            int bonus = ShopDirector.EffectTotal(Config.shop, Wallet, ShopEffects.ExpGain);
            return System.Math.Max(0, baseExp * (100 + bonus) / 100);
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
