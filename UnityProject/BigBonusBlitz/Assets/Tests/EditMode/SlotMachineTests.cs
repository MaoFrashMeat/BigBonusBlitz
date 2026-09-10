using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;

namespace BBB.Tests
{
    /// <summary>ゲーム本体を通しで回す結合テスト。</summary>
    public class SlotMachineTests
    {
        private static SlotMachine NewMachine(int seed, int setting = 1) =>
            GameDataLoader.CreateMachine(new SystemRandom(seed), setting);

        /// <summary>1G 回す（順押し・押し位置はランダム）。</summary>
        private static GameResult PlayOne(SlotMachine m, SystemRandom push)
        {
            Assert.IsTrue(m.MaxBet(), "BET失敗");
            m.Lever();
            for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
            return m.Evaluate();
        }

        [Test]
        public void 初期エンバー500_BETで497()
        {
            var m = NewMachine(1);
            Assert.AreEqual(500, m.Credit);
            Assert.IsTrue(m.MaxBet());
            Assert.AreEqual(497, m.Credit);
            Assert.AreEqual(3, m.Bet);
        }

        [Test]
        public void クレジット不足ならBETできない()
        {
            var m = NewMachine(1);
            m.Credit = 2;
            Assert.IsFalse(m.MaxBet());
        }

        [Test]
        public void リプレイ後はクレジットを減らさず回せる()
        {
            var m = NewMachine(1);
            m.IsReplay = true;
            m.Bet = 3;
            m.Credit = 10;
            Assert.IsTrue(m.MaxBet());
            // JS: replayCost(3) を引いた上で bet=3。クレジットは 10-3=7
            Assert.AreEqual(7, m.Credit);
            Assert.IsFalse(m.IsReplay);
        }

        [Test]
        public void 一万G回してもクレジットが破綻しない()
        {
            var m = NewMachine(42);
            var push = new SystemRandom(99);
            m.Credit = 100_000;
            int bonuses = 0;
            for (int g = 0; g < 10_000; g++)
            {
                var r = PlayOne(m, push);
                if (r.bonusStarted) bonuses++;
                Assert.GreaterOrEqual(m.Credit, 0);
            }
            Assert.Greater(bonuses, 0, "1万Gでボーナスが一度も揃わない");
        }

        [Test]
        public void ボーナス当選フラグは揃うまで持ち越す()
        {
            var m = NewMachine(5);
            m.Credit = 100_000;
            var push = new SystemRandom(1);
            // BB フラグを引くまで回す
            GameResult first = null;
            for (int g = 0; g < 50_000 && m.HeldBonusFlag == Flag.HAZE; g++) first = PlayOne(m, push);
            Assert.AreNotEqual(Flag.HAZE, m.HeldBonusFlag);
            var held = m.HeldBonusFlag;
            // 持ち越し中: 小役優先。フラグは持ち越しボーナスか小役のどちらか。持ち越しは消えない
            for (int g = 0; g < 20 && m.BonusMode == BonusMode.NORMAL; g++)
            {
                m.MaxBet(); m.Lever();
                Assert.IsTrue(m.CurrentFlag == held || !m.CurrentFlag.IsBonus(), $"G{g}: {m.CurrentFlag}");
                Assert.AreEqual(held, m.HeldBonusFlag);
                var flag = m.CurrentFlag;
                for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
                var res = m.Evaluate();
                // 小役を狙ったゲームでボーナスは揃わない
                if (flag != held) Assert.IsFalse(res.bonusStarted, $"G{g}: 小役優先なのにボーナス成立");
            }
        }

        [Test]
        public void BB中は目標枚数に達すると終了する()
        {
            var m = NewMachine(8);
            m.Credit = 100_000;
            var push = new SystemRandom(2);
            for (int g = 0; g < 100_000 && m.BonusMode == BonusMode.NORMAL; g++) PlayOne(m, push);
            Assert.AreNotEqual(BonusMode.NORMAL, m.BonusMode);
            int target = m.BonusPayoutTarget;
            Assert.Greater(target, 0);
            bool ended = false;
            for (int g = 0; g < 500 && !ended; g++) ended = PlayOne(m, push).bonusEnded;
            Assert.IsTrue(ended, "ボーナスが終わらない");
            Assert.AreEqual(BonusMode.NORMAL, m.BonusMode);
            Assert.AreEqual(0, m.SpinCount);
        }

        [Test]
        public void 敵出現の翌Gから3GでTier2が決着する()
        {
            var m = NewMachine(3);
            m.Credit = 1_000_000;
            var push = new SystemRandom(4);
            GameResult r = null;
            int started = -1;
            for (int g = 0; g < 200_000; g++)
            {
                r = PlayOne(m, push);
                if (r.precursorStarted) started = g;
                if (r.enemySpawned) { Assert.AreEqual(m.Config.enemyPrecursorSpins, g - started, "前兆G数"); break; }
            }
            Assert.IsTrue(r.enemySpawned, "敵が出現しない");
            Assert.IsNotNull(m.ActiveEnemyTable);
            Assert.IsTrue(m.PendingTier2);
            int resolvedAt = -1;
            for (int g = 1; g <= 10; g++)
            {
                r = PlayOne(m, push);
                if (m.BonusMode != BonusMode.NORMAL) Assert.Inconclusive("Tier2中にボーナス");
                if (r.enemyResolved.HasValue) { resolvedAt = g; break; }
            }
            Assert.AreEqual(m.Config.tier2MaxSpins, resolvedAt);
            Assert.IsFalse(m.IsTier2);
            Assert.IsFalse(m.EnemyActive);
        }

        [Test]
        public void チェリーは4コマ以内に無ければこぼす()
        {
            var m = NewMachine(2);
            var strip = m.Strips[0];
            // 左リールでチェリーが 4コマ以内に無い押し位置を探す
            int missIdx = -1;
            for (int b = 0; b < 20 && missIdx < 0; b++)
            {
                bool reachable = false;
                for (int k = 0; k <= 4 && !reachable; k++)
                {
                    int idx = ((b - k) % 20 + 20) % 20;
                    for (int r = 0; r < 3; r++) if (strip[(idx + r) % 20] == Symbol.CHERRY) reachable = true;
                }
                if (!reachable) missIdx = b;
            }
            Assert.GreaterOrEqual(missIdx, 0, "配列上チェリーがどこからでも届く");
            var stopped = new Symbol[3][];
            var res = SlipController.Stop(m.Strips, 0, missIdx, Flag.CHERRY_A, Flag.HAZE, stopped);
            CollectionAssert.DoesNotContain(res.symbols, Symbol.CHERRY);
        }

        [Test]
        public void ボーナス中でも敵エンゲージは進んで決着する()
        {
            var m = NewMachine(21);
            var push = new SystemRandom(3);
            m.Credit = 1_000_000;
            m.BonusMode = BonusMode.BB;
            m.BonusPayoutTarget = 1_000_000;   // ボーナスが終わらないように
            m.DebugForceEnemy = true;
            bool resolved = false;
            int spawnedAt = -1;
            for (int g = 0; g < 12 && !resolved; g++)
            {
                var r = PlayOne(m, push);
                Assert.AreEqual(BonusMode.BB, m.BonusMode, "ボーナスが終わってしまった");
                if (r.enemySpawned) spawnedAt = g;
                if (r.enemyResolved.HasValue) resolved = true;
            }
            Assert.IsTrue(spawnedAt >= 0, "敵が出現しない");
            Assert.IsTrue(resolved, "ボーナス中にエンゲージが決着しない（残りGが凍っている）");
            Assert.IsFalse(m.IsTier2);
        }

        [Test]
        public void ボーナス成立後は前兆を挟み最後の擬似遊技で揃う()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var m = NewMachine(seed);
                var push = new SystemRandom(seed + 500);
                m.Credit = 5_000_000;
                while (m.HeldBonusFlag == Flag.HAZE) PlayOne(m, push);
                int total = m.BonusAnnounceTotal;
                Assert.IsTrue(total >= 3 && total <= 6, $"前兆G数が範囲外: {total}");

                bool sawPseudo = false;
                for (int g = 0; g < 30 && m.BonusMode == BonusMode.NORMAL; g++)
                {
                    Assert.IsTrue(m.MaxBet());
                    m.Lever();
                    bool pseudo = m.PseudoPlay;
                    if (pseudo)
                    {
                        sawPseudo = true;
                        Assert.AreEqual(m.HeldBonusFlag, m.CurrentFlag, "擬似遊技のGはボーナスを狙う");
                    }
                    else
                    {
                        // 前兆中は引き込みを切っている＝滑りは通常の最大4コマまで
                        Assert.Greater(m.BonusAnnounceRemaining, 0, "前兆が残っていないのに擬似遊技でない");
                    }
                    for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
                    if (!pseudo) for (int i = 0; i < 3; i++) Assert.LessOrEqual(m.Slip[i], 4, "前兆中に引き込みが効いている");
                    var r = m.Evaluate();
                    if (pseudo) Assert.IsTrue(r.bonusStarted, "擬似遊技でボーナスが揃わなかった");
                }
                Assert.IsTrue(sawPseudo || m.BonusMode != BonusMode.NORMAL, "擬似遊技もボーナスも来なかった");
            }
        }

        [Test]
        public void 通しで回しても状態が壊れない()
        {
            // 4 通りの押し方で回し、毎G 状態の整合性を確認する（回帰の網）
            for (int style = 0; style < 4; style++)
            {
                var m = NewMachine(1000 + style);
                var push = new SystemRandom(50 + style);
                m.Credit = 1_000_000;
                for (int g = 0; g < 40_000; g++)
                {
                    if (m.Credit < SlotMachine.BetCost && !m.IsReplay) m.Credit += 1_000_000;
                    Assert.IsTrue(m.MaxBet());
                    m.Lever();

                    Assert.IsFalse(m.Navi.Active && m.Navi2.Active, "2種類のナビが同時に出ている");
                    Assert.IsFalse(m.Navi2.Active && !m.InAt, "AT でないのに AT ナビが出ている");
                    Assert.IsFalse(m.Navi.Active && m.BonusMode != BonusMode.NORMAL, "ボーナス中に択ナビが出ている");
                    Assert.IsFalse(m.PseudoPlay && m.HeldBonusFlag == Flag.HAZE, "持ち越しが無いのに擬似遊技");
                    Assert.IsFalse(m.InBattle && m.BattleMonster == null, "バトル中なのにモンスターが居ない");
                    Assert.IsFalse(m.InBattle && (m.BattleHp <= 0 || m.BattleHp > m.BattleHpMax), "バトルの HP が範囲外");
                    Assert.GreaterOrEqual(m.AtSpinsRemaining, 0, "AT の残りGが負");
                    Assert.GreaterOrEqual(m.AtEntryRemaining, 0, "洞窟前兆の残りGが負");
                    Assert.GreaterOrEqual(m.BonusAnnounceRemaining, 0, "ボーナス前兆の残りGが負");
                    Assert.LessOrEqual(m.Tier2SpinCount, m.Config.tier2MaxSpins, "エンゲージが規定Gを超えて続く");

                    var order = new[] { 0, 1, 2 };
                    if (m.Navi2.Active)
                    {
                        int f = m.Navi2.first;
                        var rest = new System.Collections.Generic.List<int> { 0, 1, 2 };
                        rest.Remove(f);
                        order = new[] { f, rest[0], rest[1] };
                    }
                    else if (m.Navi.Active && style != 2)
                    {
                        int f = m.Navi.first, cor = m.Navi.correctReel, third = 0;
                        for (int i = 0; i < 3; i++) if (i != f && i != cor) third = i;
                        order = new[] { f, cor, third };
                    }
                    foreach (int i in order) m.Stop(i, push.Next(20));

                    int soulsBefore = m.Wallet.Souls;
                    var r = m.Evaluate();
                    Assert.GreaterOrEqual(m.Credit, 0, "クレジットが負になった");
                    Assert.GreaterOrEqual(m.Wallet.Souls, 0, "ソウルが負");
                    Assert.GreaterOrEqual(r.win.payout, 0, "払い出しが負");
                    Assert.IsFalse(r.atEnded && m.InAt, "AT 終了なのにまだ AT 中");
                    Assert.IsFalse(r.battleResolved.HasValue && m.InBattle, "バトル決着なのにまだバトル中");
                    Assert.IsFalse(m.Bet != 0 && !m.IsReplay, "判定後に BET が残っている");
                }
            }
        }

        [Test]
        public void AT開始で通常時のエンゲージは持ち込まれない()
        {
            var m = NewMachine(3);
            var push = new SystemRandom(8);
            m.Credit = 5_000_000;
            // エンゲージ中に AT を開始させる
            m.DebugForceEnemy = true;
            for (int g = 0; g < 20 && !m.IsTier2; g++) PlayOne(m, push);
            Assert.IsTrue(m.IsTier2 || m.PrecursorRemaining > 0, "エンゲージが始まらない");
            m.PendingAt = true;
            Assert.IsTrue(m.MaxBet());
            m.Lever();
            Assert.IsTrue(m.InAt, "AT が始まっていない");
            Assert.IsFalse(m.IsTier2, "AT 中にエンゲージが残っている");
            Assert.IsFalse(m.EnemyActive, "AT 中に敵が残っている");
            Assert.AreEqual(0, m.PrecursorRemaining, "AT 中に前兆が残っている");
            Assert.IsFalse(m.Navi.Active, "AT 中に択ナビが出ている");
        }

        [Test]
        public void AT当選後は洞窟前兆を挟んでから50Gで始まる()
        {
            var cfg = GameDataLoader.LoadGameConfig();
            int initial = cfg.at.initialSpins;
            int runs = 0;
            for (int seed = 0; seed < 40; seed++)
            {
                var m = NewMachine(seed);
                var push = new SystemRandom(seed + 400);
                m.Credit = 5_000_000;
                int guard = 0;
                while (m.AtEntryRemaining == 0 && !m.InAt && guard++ < 60_000) PlayOne(m, push);
                if (m.AtEntryRemaining == 0) continue;
                runs++;
                int total = m.AtEntryTotal;
                Assert.IsTrue(total >= 2 && total <= 4, $"洞窟前兆のG数が範囲外: {total}");
                Assert.IsFalse(m.InAt, "前兆を挟まずに AT が始まっている");

                var seq = new System.Collections.Generic.List<int>();
                bool started = false;
                for (int g = 0; g < total + 3 && !started; g++)
                {
                    Assert.IsTrue(m.MaxBet());
                    m.Lever();
                    if (m.AtEntryStage > 0) seq.Add(m.AtEntryStage);
                    if (m.AtJustStarted)
                    {
                        started = true;
                        Assert.AreEqual(total, seq.Count, "前兆の段階数が総G数と合わない");
                        Assert.AreEqual(initial - 1, m.AtSpinsRemaining, "AT の初期G数が違う（開始Gの1G消化ぶんを除く）");
                    }
                    for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
                    m.Evaluate();
                }
                Assert.IsTrue(started, "前兆のあとに AT が始まらない");
                for (int i = 0; i < seq.Count; i++) Assert.AreEqual(i + 1, seq[i], "前兆の段階が 1,2,3… の順でない");
            }
            Assert.Greater(runs, 0, "AT に一度も当選しなかった");
        }

        [Test]
        public void AT期待度_ボーナス中だけ溜まりランクが上がる()
        {
            var m = NewMachine(7);
            var push = new SystemRandom(3);
            m.Credit = 5_000_000;
            Assert.AreEqual(0, m.AtExpectPercent);
            int guard = 0;
            while (m.BonusMode == BonusMode.NORMAL && guard++ < 8000) PlayOne(m, push);
            Assert.AreNotEqual(BonusMode.NORMAL, m.BonusMode, "ボーナスに入らなかった");
            int atStart = m.AtExpectPercent;
            guard = 0;
            while (m.BonusMode != BonusMode.NORMAL && guard++ < 400) PlayOne(m, push);
            Assert.GreaterOrEqual(m.AtExpectPercent, atStart, "期待度が減っている");
            var cfg = m.Config.atExpect;
            Assert.IsNotNull(cfg, "atExpect が読めていない");
            Assert.AreEqual("白", AtDirector.RankFor(cfg, 0).name);
            Assert.AreEqual("青", AtDirector.RankFor(cfg, 25).name);
            Assert.AreEqual("赤", AtDirector.RankFor(cfg, 85).name);
            Assert.AreEqual("虹", AtDirector.RankFor(cfg, 100).name);
        }

        /// <summary>そのラインに 3 図柄を並べた停止形を作る。</summary>
        private static Symbol[][] Lineup(int line, Symbol a, Symbol b, Symbol c)
        {
            var rows = PayLines.Rows[line];
            var syms = new[] { a, b, c };
            var st = new Symbol[3][];
            for (int r = 0; r < 3; r++)
            {
                st[r] = new[] { Symbol.BLANK, Symbol.BLANK, Symbol.BLANK };
                st[r][rows[r]] = syms[r];
            }
            return st;
        }

        private static bool HasLineup(SlotMachine m, Symbol a, Symbol b, Symbol c)
        {
            var syms = new[] { a, b, c };
            for (int line = 0; line < PayLines.Count; line++)
            {
                var rows = PayLines.Rows[line];
                bool all = true;
                for (int r = 0; r < 3 && all; r++) if (m.Stopped[r][rows[r]] != syms[r]) all = false;
                if (all) return true;
            }
            return false;
        }

        [Test]
        public void BAR揃いと青赤青はどのラインでもBIG_紛らわしい停止形は出ない()
        {
            var payouts = NewMachine(1).Config.payouts;
            var combos = new[]
            {
                ("BAR 揃い", Symbol.BAR, Symbol.BAR, Symbol.BAR),
                ("青7赤7青7", Symbol.BLUE7, Symbol.RED7, Symbol.BLUE7),
            };
            foreach (var (name, a, b, c) in combos)
                for (int line = 0; line < PayLines.Count; line++)
                {
                    var res = WinEvaluator.Evaluate(Lineup(line, a, b, c), BonusMode.NORMAL, payouts);
                    Assert.AreEqual(WinType.BIG, res.winType, $"{name} ライン{line} が BIG になっていない");
                }

            // 実戦: 成立していないのに揃って見える停止形を制御が作らないこと
            var m = NewMachine(31);
            var push = new SystemRandom(88);
            m.Credit = 5_000_000;
            var fake = new int[combos.Length];
            var real = new int[combos.Length];
            for (int g = 0; g < 60_000; g++)
            {
                Assert.IsTrue(m.MaxBet());
                m.Lever();
                for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
                var r = m.Evaluate();
                for (int i = 0; i < combos.Length; i++)
                {
                    if (!HasLineup(m, combos[i].Item2, combos[i].Item3, combos[i].Item4)) continue;
                    if (r.bonusStarted) real[i]++; else fake[i]++;
                }
            }
            for (int i = 0; i < combos.Length; i++)
                Assert.AreEqual(0, fake[i], $"{combos[i].Item1} がボーナスにならずに揃っている");
            Assert.Greater(real[0], 0, "BAR 揃いの BIG が一度も出ていない");
        }

        [Test]
        public void 配列は全役をどの押し順_どの位置でも成立または回避できる()
        {
            var strips = NewMachine(1).Strips;
            foreach (System.Enum v in System.Enum.GetValues(typeof(Flag)))
            {
                var f = (Flag)v;
                Assert.IsTrue(SlipController.IsGuaranteed(strips, f), $"{f}: どこで押しても成立/回避できない");
                Assert.IsTrue(SlipController.IsGuaranteedOrdered(strips, f), $"{f}: 順押しで成立/回避できない");
            }
        }

        [Test]
        public void ベル択ナビ_外すとベルを取りこぼす()
        {
            int correctPaid = 0, correctGames = 0, missPaid = 0, missGames = 0;
            foreach (bool chooseCorrect in new[] { true, false })
            {
                var m = NewMachine(4);
                var push = new SystemRandom(11);
                m.Credit = 5_000_000;
                for (int g = 0; g < 60_000; g++)
                {
                    Assert.IsTrue(m.MaxBet());
                    m.Lever();
                    bool hadNavi = m.Navi.Active;
                    int first = m.Navi.first, correct = m.Navi.correctReel;
                    var order = new[] { 0, 1, 2 };
                    if (hadNavi)
                    {
                        int wrong = 0;
                        for (int i = 0; i < 3; i++) if (i != first && i != correct) wrong = i;
                        int second = chooseCorrect ? correct : wrong;
                        int third = 0;
                        for (int i = 0; i < 3; i++) if (i != first && i != second) third = i;
                        order = new[] { first, second, third };
                    }
                    foreach (int i in order) m.Stop(i, push.Next(20));
                    var r = m.Evaluate();
                    if (!hadNavi) continue;
                    if (chooseCorrect) { correctGames++; if (r.win.payout > 0) correctPaid++; }
                    else { missGames++; if (r.win.payout > 0) missPaid++; }
                }
            }
            Assert.Greater(correctGames, 100, "ナビが出ていない");
            Assert.Greater(missGames, 100, "ナビが出ていない");
            Assert.AreEqual(correctGames, correctPaid, "正解なのに払い出しが無いGがある");
            Assert.AreEqual(0, missPaid, "外したのにベルが揃って払い出されている");
        }

        [Test]
        public void 前兆が終われば引き込みでどこで押しても揃う()
        {
            var m = NewMachine(5);
            m.Credit = 1_000_000;
            var push = new SystemRandom(7);
            foreach (var flag in new[] { Flag.BB_A, Flag.BB_B, Flag.BB_C, Flag.BB_D, Flag.RB_A, Flag.RB_B })
            {
                for (int g = 0; g < 60; g++)
                {
                    m.BonusMode = BonusMode.NORMAL; m.BonusEarned = 0; m.BonusPayoutTarget = 0;
                    m.HeldBonusFlag = Flag.HAZE; m.IsReplay = false; m.Bet = 0;
                    Assert.IsTrue(m.MaxBet());
                    m.DebugForceFlag = flag;
                    m.Lever();
                    // 前兆を消化済みにする（前兆中は引き込みを切っているので目押しが要る）
                    m.BonusAnnounceRemaining = 0;
                    for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
                    var r = m.Evaluate();
                    Assert.IsTrue(r.bonusStarted, $"{flag} が揃わない (G{g})");
                }
            }
        }

        [Test]
        public void 天井到達で強制ボーナス()
        {
            var m = NewMachine(11);
            m.Credit = 1_000_000;
            m.Mode = Mode.D; // 天井 100G
            m.SpinCount = m.Config.CeilingFor(Mode.D) - 1;
            m.HeldBonusFlag = Flag.HAZE;
            m.MaxBet(); m.Lever();
            Assert.IsTrue(m.CurrentFlag.IsBonus());
            Assert.IsTrue(m.HeldBonusFlag.IsBonus());
        }
    }
}
