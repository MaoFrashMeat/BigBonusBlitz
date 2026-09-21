using System.Collections.Generic;
using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;

namespace BBB.Tests
{
    public class AdventureTests
    {
        private static SlotMachine NewMachine(int seed) => GameDataLoader.CreateMachine(new SystemRandom(seed), 1);

        private static GameResult PlayOne(SlotMachine m, SystemRandom push)
        {
            if (m.Credit < SlotMachine.BetCost && !m.IsReplay) m.Credit += 100_000;
            Assert.IsTrue(m.MaxBet(), "BET失敗");
            m.Lever();
            for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
            return m.Evaluate();
        }

        private static AdventureConfig TinyConfig()
        {
            return new AdventureConfig
            {
                enabled = true, start = "A", hideRoute = true, chapterClearSouls = 100,
                nodes = new List<StageNode>
                {
                    new StageNode { id = "A", name = "a", column = "A", spins = 5, rank = 0,
                        routeByFlag = new Dictionary<string, Dictionary<string, int>> { ["CHANCE"] = new Dictionary<string, int> { ["B2"] = 100 } },
                        routeDefault = new Dictionary<string, int> { ["B1"] = 100 } },
                    new StageNode { id = "B1", name = "b1", column = "B", spins = 5, rank = 1,
                        routeDefault = new Dictionary<string, int> { ["C"] = 100 } },
                    new StageNode { id = "B2", name = "b2", column = "B", spins = 5, rank = 2, mode = "D",
                        treasure = new Dictionary<string, int> { ["BELL"] = 100 },
                        routeDefault = new Dictionary<string, int> { ["C"] = 100 } },
                    new StageNode { id = "C", name = "c", column = "C", spins = 5, rank = 3, routeDefault = new Dictionary<string, int>() },
                },
                treasures = new List<TreasureDef> { new TreasureDef { id = "map", name = "地図", kind = "atSpins", amount = 7, weight = 1 } },
                resource = new ResourceConfig
                {
                    enabled = true, name = "回復薬", startTorches = 1, maxTorches = 4, spinsPerTorch = 12,
                    torchCost = 40, creditCost = 30, creditAmount = 50, rescueCredit = 50, resetOnDeath = true,
                },
            };
        }

        [Test]
        public void 達成条件でルートが決まり確率では覆らない()
        {
            var cfg = TinyConfig();
            cfg.Find("A").routeConditions.Add(new RouteCondition
            {
                to = "B2", label = "リプレイ 3 回",
                counters = new Dictionary<string, int> { ["REPLAY"] = 3 },
                priority = 10,
            });
            var st = new AdventureState();
            AdventureDirector.Reset(cfg, st);
            var rng = new SystemRandom(4);

            Assert.IsNull(AdventureDirector.CheckConditions(cfg, st, 0, 0, 1), "まだ達成していないのに成立した");
            for (int i = 0; i < 3; i++) AdventureDirector.AddCount(st, "REPLAY");
            var hit = AdventureDirector.CheckConditions(cfg, st, 0, 0, 1);
            Assert.IsNotNull(hit, "リプレイ 3 回で成立しない");
            Assert.AreEqual("B2", hit.to);

            st.nextId = hit.to; st.decidedPriority = hit.priority;
            Assert.IsNull(AdventureDirector.CheckConditions(cfg, st, 0, 0, 1), "同じ条件で二度成立している");
            Assert.IsNull(AdventureDirector.RollRoute(cfg, st, "CHANCE", rng), "条件で決めたルートを確率が覆した");
            Assert.AreEqual("B2", st.nextId);
        }

        [Test]
        public void 持ち物の条件も見る()
        {
            var cfg = TinyConfig();
            cfg.Find("A").routeConditions.Add(new RouteCondition
            {
                to = "B2", label = "エンバー 15000 でリプレイ 2 回",
                counters = new Dictionary<string, int> { ["REPLAY"] = 2 },
                state = new Dictionary<string, int> { ["ember"] = 15000 },
                priority = 20,
            });
            var st = new AdventureState();
            AdventureDirector.Reset(cfg, st);
            AdventureDirector.AddCount(st, "REPLAY", 2);
            Assert.IsNull(AdventureDirector.CheckConditions(cfg, st, 14999, 0, 1), "エンバーが足りないのに成立した");
            Assert.IsNotNull(AdventureDirector.CheckConditions(cfg, st, 15000, 0, 1), "エンバーが足りているのに成立しない");
        }

        [Test]
        public void 優先度が高い条件が勝つ()
        {
            var cfg = TinyConfig();
            var node = cfg.Find("A");
            node.routeConditions.Add(new RouteCondition { to = "B1", counters = new Dictionary<string, int> { ["REPLAY"] = 1 }, priority = 5 });
            node.routeConditions.Add(new RouteCondition { to = "B2", counters = new Dictionary<string, int> { ["REPLAY"] = 1 }, priority = 30 });
            var st = new AdventureState();
            AdventureDirector.Reset(cfg, st);
            AdventureDirector.AddCount(st, "REPLAY");
            var hit = AdventureDirector.CheckConditions(cfg, st, 0, 0, 1);
            Assert.AreEqual("B2", hit.to, "優先度の高い条件が選ばれていない");
            st.nextId = hit.to; st.decidedPriority = hit.priority;
            Assert.IsNull(AdventureDirector.CheckConditions(cfg, st, 0, 0, 1), "弱い条件が上書きした");
        }

        [Test]
        public void ステージが変わると数えものがリセットされる()
        {
            var cfg = TinyConfig();
            var st = new AdventureState();
            AdventureDirector.Reset(cfg, st);
            AdventureDirector.AddCount(st, "REPLAY", 9);
            st.replayChain = 4; st.decidedPriority = 10; st.decidedBy = "x";
            AdventureDirector.Enter(cfg, st, "B1");
            Assert.AreEqual(0, AdventureDirector.GetCount(st, "REPLAY"), "数えものが残っている");
            Assert.AreEqual(0, st.replayChain);
            Assert.AreEqual(0, st.decidedPriority);
            Assert.IsNull(st.decidedBy);
        }

        [Test]
        public void 実際に回してもリプレイ条件が成立する()
        {
            var m = NewMachine(41);
            m.Config.adventure = TinyConfig();
            m.Config.adventure.resource.enabled = false;
            var node = m.Config.adventure.Find("A");
            node.spins = 100000;   // ステージが終わらないようにして条件だけ見る
            node.routeByFlag.Clear();   // チャンス目のルートが先に B2 を決めると、条件は「もう同じ行き先」で出ない
            node.routeConditions.Add(new RouteCondition
            {
                to = "B2", label = "リプレイ 15 回",
                counters = new Dictionary<string, int> { ["REPLAY"] = 15 },
                priority = 10,
            });
            AdventureDirector.Reset(m.Config.adventure, m.Adv);
            m.Adv.spinsLeft = 100000;
            m.Credit = 1_000_000;
            var push = new SystemRandom(7);
            GameResult hit = null;
            for (int g = 0; g < 4000 && hit == null; g++)
            {
                var r = PlayOne(m, push);
                if (r.routeCondition != null) hit = r;
            }
            Assert.IsNotNull(hit, "回してもリプレイ条件が成立しない");
            Assert.AreEqual("B2", hit.routeDecided);
            Assert.GreaterOrEqual(AdventureDirector.GetCount(m.Adv, "REPLAY"), 15);
            Assert.AreEqual("B2", m.Adv.nextId);
        }

        [Test]
        public void ライフが尽きたら力尽きた扱いになる()
        {
            var m = NewMachine(31);
            m.Config.adventure = TinyConfig();
            m.Config.adventure.resource.replayHealAmount = 0;   // 回復と混ざると乱数次第で 500 G 以内に尽きない（落とし物の抽選が増えて乱数列が変わった 2026-09-21）
            m.Config.adventure.resource.bonusBellHealRate = 0;
            m.Config.adventure.chapterClearTorches = 0;         // 5 G × 3 ステージで章が終わり、回復薬 +2 で生き返ってしまう
            AdventureDirector.Reset(m.Config.adventure, m.Adv);
            AdventureDirector.ResetTorches(m.Config.adventure, m.Adv, m.TorchSpinsPerUnit);
            Assert.AreEqual(1, m.Adv.torches);
            Assert.AreEqual(12, m.Adv.torchSpins);
            m.Credit = 100_000;
            var push = new SystemRandom(6);
            GameResult hit = null;
            for (int g = 0; g < 4000 && hit == null; g++)   // AT（洞窟）の間はライフが減らないので、長めに回す
            {
                var r = PlayOne(m, push);
                if (r.outOfTorch) hit = r;
                Assert.LessOrEqual(m.Adv.torches, m.Config.adventure.resource.maxTorches, "回復薬が上限を超えた");
                Assert.GreaterOrEqual(m.Adv.torchSpins, 0, "回復薬の残量が負");
            }
            Assert.IsNotNull(hit, "ライフが尽きない");
            Assert.IsTrue(hit.returnedToTown);
            Assert.AreEqual("hp", hit.returnReason);
            Assert.AreEqual("hp", m.Adv.returnReason);
            Assert.AreEqual(0, m.Adv.torches);
            Assert.AreEqual(0, m.Hp, "ライフが 0 になっていない");
        }

        [Test]
        public void ライフは回復薬の個数と残量から一意に決まる()
        {
            var m = NewMachine(11);
            m.Config.adventure = TinyConfig();          // 1 個 = 12
            int unit = m.TorchSpinsPerUnit;
            Assert.AreEqual(12, unit);
            Assert.AreEqual(4 * 12, m.HpMax);

            foreach (int hp in new[] { 0, 1, 11, 12, 13, 24, 47, 48 })
            {
                AdventureDirector.SetHp(m.Config.adventure, m.Adv, hp, unit);
                Assert.AreEqual(hp, m.Hp, $"ライフ {hp} を置いて読み直すとズレる");
                Assert.GreaterOrEqual(m.Adv.torchSpins, 0);
                Assert.LessOrEqual(m.Adv.torches, m.Config.adventure.resource.maxTorches);
            }
        }

        [Test]
        public void ライフの回復は上限で止まる()
        {
            var m = NewMachine(12);
            m.Config.adventure = TinyConfig();
            int unit = m.TorchSpinsPerUnit;
            AdventureDirector.SetHp(m.Config.adventure, m.Adv, m.HpMax - 5, unit);
            Assert.AreEqual(5, m.HealHp(20), "上限を超えて回復した");
            Assert.AreEqual(m.HpMax, m.Hp);
            Assert.AreEqual(0, m.HealHp(10), "満タンなのに回復した");
        }

        [Test]
        public void ボーナス中のベルでライフが回復する()
        {
            var m = NewMachine(13);
            m.Config.adventure = TinyConfig();
            var res = m.Config.adventure.resource;
            res.maxTorches = 1000;                     // このテストではライフ切れで止めない
            res.bonusBellHealRate = 100;               // 必ず回復させて経路だけ見る
            res.bonusBellHealAmount = 5;
            res.replayHealAmount = 0;                  // リプレイの回復と混ざらないよう切る
            AdventureDirector.SetHp(m.Config.adventure, m.Adv, 6000, m.TorchSpinsPerUnit);
            m.Credit = 100_000;

            var push = new SystemRandom(9);
            int healed = 0, healsInNormal = 0;
            for (int g = 0; g < 6000; g++)
            {
                bool inBonus = m.BonusMode != BonusMode.NORMAL;
                var r = PlayOne(m, push);
                if (r.hpHealed <= 0) continue;
                healed = r.hpHealed;
                if (!inBonus) healsInNormal++;
                break;
            }
            Assert.AreEqual(5, healed, "ボーナス中のベルで回復しない");
            Assert.AreEqual(0, healsInNormal, "通常時に回復した");
        }

        [Test]
        public void 通常時のリプレイでライフが回復する()
        {
            var m = NewMachine(23);
            m.Config.adventure = TinyConfig();
            var res = m.Config.adventure.resource;
            res.maxTorches = 1000;                     // このテストではライフ切れで止めない
            res.bonusBellHealRate = 0;                 // ベルの回復と混ざらないよう切る
            res.replayHealAmount = 3;
            AdventureDirector.SetHp(m.Config.adventure, m.Adv, 6000, m.TorchSpinsPerUnit);
            m.Credit = 100_000;

            var push = new SystemRandom(7);
            int replayHeals = 0, healsWhileHeld = 0, replaysInNormal = 0;
            for (int g = 0; g < 20000; g++)
            {
                if (m.Credit < SlotMachine.BetCost && !m.IsReplay) m.Credit += 100_000;
                Assert.IsTrue(m.MaxBet(), "BET失敗");
                m.Lever();
                bool held = m.StageHeld;   // レバーを叩いた時点（ボーナス成立・AT 開始はここで決まる）
                for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
                var r = m.Evaluate();
                bool replay = r.win.isReplay;
                if (replay && !held) replaysInNormal++;
                if (r.hpHealed <= 0) continue;
                if (held) healsWhileHeld++;
                else
                {
                    Assert.IsTrue(replay, "リプレイ以外で回復した");
                    Assert.AreEqual(3, r.hpHealed, "回復量が設定と違う");
                    replayHeals++;
                }
            }
            Assert.Greater(replaysInNormal, 0, "通常時のリプレイが 1 回も出ていない（テストが無意味）");
            Assert.AreEqual(replaysInNormal, replayHeals, "通常時のリプレイで回復していない回がある");
            Assert.AreEqual(0, healsWhileHeld, "ライフが止まっている間に回復した");
        }

        [Test]
        public void クレジットが尽きたら力尽きて章の最初へ戻る()
        {
            var m = NewMachine(33);
            m.Config.adventure = TinyConfig();
            AdventureDirector.Enter(m.Config.adventure, m.Adv, "B2");
            m.Adv.spinsLeft = 10_000;
            AdventureDirector.AddTorch(m.Config.adventure, m.Adv, 4, m.TorchSpinsPerUnit);
            m.Adv.torchSpins = 100_000;   // ライフでは終わらせない
            m.Adv.stockAtSpins = 25;
            m.Credit = 3;
            var push = new SystemRandom(2);
            GameResult hit = null;
            for (int g = 0; g < 200 && hit == null; g++)
            {
                if (m.Credit < SlotMachine.BetCost && !m.IsReplay) break;
                Assert.IsTrue(m.MaxBet());
                m.Lever();
                for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
                var r = m.Evaluate();
                if (r.ranOutOfCredit) hit = r;
            }
            Assert.IsNotNull(hit, "クレジットが尽きない");
            Assert.AreEqual("credit", hit.returnReason);
            Assert.AreEqual("B2", m.Adv.nodeId, "罰を与える前に進行が動いている");

            int lost = AdventureDirector.ApplyDeathPenalty(m.Config.adventure, m.Adv, m.Wallet);
            Assert.AreEqual(0, lost, "ソウル没収は既定で 0 のはず");
            Assert.AreEqual("A", m.Adv.nodeId, "章の最初に戻っていない");
            Assert.AreEqual(1, m.Adv.visited.Count);
            Assert.AreEqual(0, m.Adv.stockAtSpins, "宝の貯金が残っている");
        }

        [Test]
        public void ボーナス持ち越し中はライフが減らない()
        {
            var m = NewMachine(35);
            m.Config.adventure = TinyConfig();
            AdventureDirector.Enter(m.Config.adventure, m.Adv, "B1");
            m.Adv.spinsLeft = 10_000;
            AdventureDirector.AddTorch(m.Config.adventure, m.Adv, 4, m.TorchSpinsPerUnit);
            m.Credit = 100_000;
            var push = new SystemRandom(4);
            m.DebugForceFlag = Flag.BB_A;
            PlayOne(m, push);
            Assume.That(m.HeldBonusFlag != Flag.HAZE, "持ち越しにならなかった");
            int spins = m.Adv.torchSpins, torches = m.Adv.torches;
            for (int g = 0; g < 3 && m.HeldBonusFlag != Flag.HAZE && m.BonusMode == BonusMode.NORMAL; g++) PlayOne(m, push);
            Assert.AreEqual(torches, m.Adv.torches, "持ち越し中に回復薬の個数が減った");
            Assert.AreEqual(spins, m.Adv.torchSpins, "持ち越し中にライフが減った");
        }

        [Test]
        public void 回復薬の補給は上限で止まる()
        {
            var cfg = TinyConfig();
            var st = new AdventureState();
            AdventureDirector.Reset(cfg, st);
            AdventureDirector.ResetTorches(cfg, st, 12);
            Assert.AreEqual(1, st.torches);
            Assert.AreEqual(3, AdventureDirector.AddTorch(cfg, st, 3, 12));
            Assert.AreEqual(4, st.torches);
            Assert.AreEqual(0, AdventureDirector.AddTorch(cfg, st, 5, 12), "上限を超えて増えた");
            Assert.AreEqual(4, st.torches);
            // 0 本から買い直すと残量が満タンに戻る
            st.torches = 0; st.torchSpins = 0;
            Assert.AreEqual(1, AdventureDirector.AddTorch(cfg, st, 1, 12));
            Assert.AreEqual(12, st.torchSpins);
        }

        [Test]
        public void 設定ファイルの冒険は読み込めてステージが繋がっている()
        {
            var m = NewMachine(1);
            Assert.IsTrue(m.AdventureEnabled, "adventure が無効");
            var cfg = m.Config.adventure;
            Assert.IsNotNull(cfg.Find(cfg.start), "開始ステージが無い");
            Assert.AreEqual(cfg.start, m.Adv.nodeId);
            Assert.Greater(m.Adv.spinsLeft, 0);
            int goals = 0;
            foreach (var n in cfg.nodes)
            {
                if (n.IsGoal) { goals++; continue; }
                foreach (var k in n.routeDefault.Keys) Assert.IsNotNull(cfg.Find(k), $"{n.id} の既定ルート {k} が無い");
                foreach (var t in n.routeByFlag.Values) foreach (var k in t.Keys) if (k != "NONE") Assert.IsNotNull(cfg.Find(k), $"{n.id} のルート {k} が無い");
            }
            Assert.GreaterOrEqual(goals, 1, "終点が無い（章が終わらない）");
            Assert.Greater(cfg.treasures.Count, 0);
        }

        [Test]
        public void ルート抽選は上位ランクにだけ書き換わる()
        {
            var cfg = TinyConfig();
            var st = new AdventureState();
            AdventureDirector.Reset(cfg, st);
            var rng = new SystemRandom(1);
            Assert.AreEqual("B2", AdventureDirector.RollRoute(cfg, st, "CHANCE", rng));
            Assert.AreEqual("B2", st.nextId);
            Assert.IsNull(AdventureDirector.RollRoute(cfg, st, "CHANCE", rng), "同じ行き先で再決定している");
            Assert.IsNull(AdventureDirector.RollRoute(cfg, st, "BELL", rng), "表に無い役で決まっている");
            // 下位に書き換えない
            st.nextId = "B2";
            cfg.Find("A").routeByFlag["CHERRY"] = new Dictionary<string, int> { ["B1"] = 100 };
            Assert.IsNull(AdventureDirector.RollRoute(cfg, st, "CHERRY", rng));
            Assert.AreEqual("B2", st.nextId);
        }

        [Test]
        public void 残りGが尽きたら次のステージへ進み終点で章クリア()
        {
            var m = NewMachine(3);
            m.Config.adventure = TinyConfig();
            m.Config.adventure.resource.enabled = false;   // ライフでは止めない
            AdventureDirector.Reset(m.Config.adventure, m.Adv);
            AdventureDirector.AddTorch(m.Config.adventure, m.Adv, 4, m.TorchSpinsPerUnit);
            m.Credit = 100_000;
            var push = new SystemRandom(2);
            var seen = new List<string> { m.Adv.nodeId };
            bool cleared = false;
            int soulsBefore = m.Wallet.Souls;
            for (int g = 0; g < 3000 && !cleared; g++)
            {
                var r = PlayOne(m, push);
                Assert.GreaterOrEqual(m.Adv.spinsLeft, 0);
                if (r.stageChanged)
                {
                    Assert.AreEqual(seen[seen.Count - 1], r.stageFrom);
                    Assert.AreEqual(m.Adv.nodeId, r.stageTo);
                    seen.Add(r.stageTo);
                }
                if (r.chapterCleared) cleared = true;
            }
            Assert.IsTrue(cleared, "章が終わらない");
            Assert.Greater(m.Adv.torches, 0, "章クリアで回復薬が補充されていない");
            Assert.AreEqual("C", seen[seen.Count - 1], "終点を通っていない");
            Assert.AreEqual("A", m.Adv.nodeId, "章クリア後に最初へ戻っていない");
            Assert.AreEqual(2, m.Adv.chapter);
            Assert.GreaterOrEqual(m.Wallet.Souls - soulsBefore, 100, "章クリアのソウルが無い");
            Assert.AreEqual(1, m.Adv.visited.Count, "通過記録が初期化されていない");
        }

        [Test]
        public void ステージ移動は敵戦闘や前兆を抱えたままでは起きない()
        {
            var m = NewMachine(5);
            m.Config.adventure = TinyConfig();
            m.Config.adventure.resource.enabled = false;
            AdventureDirector.Reset(m.Config.adventure, m.Adv);
            m.Credit = 100_000;
            var push = new SystemRandom(9);
            for (int g = 0; g < 4000; g++)
            {
                bool busyBefore = m.IsTier2 || m.PendingTier2 || m.EnemyActive || m.PrecursorRemaining > 0 || m.InAt || m.BonusMode != BonusMode.NORMAL || m.HeldBonusFlag != Flag.HAZE;
                var r = PlayOne(m, push);
                if (r.stageChanged || r.chapterCleared)
                {
                    Assert.IsFalse(busyBefore && (m.IsTier2 || m.EnemyActive || m.InAt || m.BonusMode != BonusMode.NORMAL), "何かを抱えたままステージが動いた");
                    if (!r.bossAmbush) Assert.IsFalse(m.IsTier2 || m.EnemyActive, "移動のGに敵が残っている");
                }
            }
        }

        [Test]
        public void 宝の洞窟の地図は次のATに上乗せされる()
        {
            var m = NewMachine(7);
            m.Config.adventure = TinyConfig();
            m.Config.adventure.resource.enabled = false;
            AdventureDirector.Enter(m.Config.adventure, m.Adv, "B2");
            m.Adv.spinsLeft = 1000;   // ステージが終わらないように
            m.Credit = 100_000;
            var push = new SystemRandom(4);
            m.DebugForceFlag = Flag.BELL_A;
            // 宝箱は当選から 2〜4 G の前兆を挟んで見つかる（本人 2026-09-21）。当選の G は treasurePrecursorStarted、見つかった G に treasure
            var r = PlayOne(m, push);
            Assert.IsTrue(r.treasurePrecursorStarted, "ベル 100% で宝に当選しない");
            Assert.IsNull(r.treasure, "前兆を挟まずに見つかった");
            int total = r.treasurePrecursorTotal;
            Assert.That(total, Is.InRange(m.Config.adventure.treasurePrecursorMin, m.Config.adventure.treasurePrecursorMax));
            for (int g = 1; g <= total; g++)
            {
                r = PlayOne(m, push);
                Assert.AreEqual(g, r.treasurePrecursorStage, "前兆の段階");
                if (g < total) Assert.IsNull(r.treasure, "前兆の途中で見つかった");
            }
            Assert.IsNotNull(r.treasure, "前兆が終わっても宝が出ない");
            Assert.AreEqual("atSpins", r.treasure.kind);
            Assert.AreEqual(7, m.Adv.stockAtSpins);

            // AT を始めると貯金が使われる
            m.PendingAt = true;
            Assert.IsTrue(m.MaxBet());
            m.Lever();
            int expect = System.Math.Max(1, (m.Config.at?.initialSpins ?? 50) + m.BonusOf(ShopEffects.AtInitialSpins)) + 7 - 1;   // 装備込みの +G。開始Gで 1G 消化
            Assert.AreEqual(expect, m.AtSpinsRemaining);
            Assert.AreEqual(0, m.Adv.stockAtSpins);
            for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
            Assert.AreEqual(7, m.Evaluate().atStockUsed);
        }

        [Test]
        public void 高確ステージは指定モードのテーブルで抽選する()
        {
            // B2 は mode="D"。内部モードが A でも D の表を引く（ハズレ率の差で確認）
            const int Shift = 12000;   // D の表: ハズレを減らしてスイカに回す（設定の表が同じでも仕組みを確かめられるように）
            var cfgA = GameDataLoader.CreateMachine(new SystemRandom(1), 1).Config;
            int hazeA = cfgA.probabilities_A["1"]["HAZE"], hazeD = cfgA.probabilities_D["1"]["HAZE"] - Shift;
            int Count(bool high)
            {
                var m = NewMachine(11);
                m.Config.probabilities_D["1"]["HAZE"] -= Shift;
                m.Config.probabilities_D["1"]["SUICA_A"] += Shift;
                m.SetSetting(1);
                m.Config.adventure = TinyConfig();
                m.Config.adventure.resource.enabled = false;
                AdventureDirector.Enter(m.Config.adventure, m.Adv, high ? "B2" : "B1");
                m.Adv.spinsLeft = 1_000_000;
                m.Mode = Mode.A;
                m.Credit = 10_000_000;
                var push = new SystemRandom(3);
                int haze = 0;
                for (int g = 0; g < 20000; g++)
                {
                    if (m.BonusMode != BonusMode.NORMAL || m.HeldBonusFlag != Flag.HAZE || m.InAt || m.IsTier2) { PlayOne(m, push); continue; }
                    m.Mode = Mode.A;
                    PlayOne(m, push);
                    if (m.CurrentFlag == Flag.HAZE) haze++;
                }
                return haze;
            }
            int normal = Count(false), high = Count(true);
            if (hazeD < hazeA) Assert.Less(high, normal * 0.95, "高確ステージでハズレが減っていない");
            else Assert.Greater(high, normal * 1.05, "高確ステージで表が変わっていない");
        }

        [Test]
        public void 物語は第2章まであり_段ごとに上中下の台詞がある()
        {
            var m = NewMachine(3);
            var story = m.Config.story;
            var adv = m.Config.adventure;
            Assert.IsNotNull(story);
            var ch2 = story.Find(2);
            Assert.AreEqual(2, ch2.chapter, "第 2 章が無い（第 1 章を使い回している）");
            Assert.Greater(ch2.opening.Count, 0);
            Assert.Greater(ch2.onClear.Count, 0);
            foreach (var n in adv.nodes)
            {
                var st = ch2.FindStage(n.column);
                Assert.IsNotNull(st, "第 2 章に段 " + n.column + " の台詞が無い");
                Assert.IsTrue(st.high.Count > 0 && st.mid.Count > 0 && st.low.Count > 0, "段 " + n.column + " の上中下が揃っていない");
                var lines = StoryDirector.OnEnter(story, adv, 2, n.id);
                Assert.IsNotNull(lines, n.id + " に着いたときの台詞が無い");
            }
            // 第 2 章の頭では「また来たのか」を足さない（初めて通る章）
            Assert.IsNull(StoryDirector.Lap(story, 2, 2));
            // 第 3 章も用意してあり、周回の台詞は足さない。第 4 章は用意していないので最後の章（第 3 章）を使い回し、周回の台詞は第 1 章の laps から足される
            Assert.AreEqual(3, story.Find(3).chapter);
            Assert.IsNull(StoryDirector.Lap(story, 3, 3));
            Assert.AreEqual(3, story.Find(4).chapter);
            Assert.AreEqual(3, story.Find(9).chapter);
            Assert.IsNotNull(StoryDirector.Lap(story, 4, 4));
            Assert.AreEqual(StoryDirector.Lap(story, 4, 4), StoryDirector.Lap(story, 1, 4), "使い回しの周回の台詞は第 1 章のもの");
            Assert.AreEqual("第4章  灯の歩幅", StoryDirector.ChapterTitle(story, m.Config.adventure, 4));
        }

        [Test]
        public void 第3章は全部の段に台詞があり_第2章はステージ名が全部書き換わる()
        {
            var m = NewMachine(3);
            var story = m.Config.story;
            var adv = m.Config.adventure;
            var ch3 = story.FindExact(3);
            Assert.IsNotNull(ch3);
            foreach (var n in adv.nodes)
            {
                var st = ch3.FindStage(n.column);
                Assert.IsNotNull(st, "第 3 章に段 " + n.column + " の台詞が無い");
                Assert.IsTrue(st.high.Count > 0 && st.mid.Count > 0 && st.low.Count > 0, "段 " + n.column + " の上中下が揃っていない");
                // 第 2 章は同じ地図でステージ名だけ変わる。第 1 章と第 3 章は adventure の名のまま
                string n2 = StoryDirector.StageName(story, 2, n);
                Assert.AreNotEqual(n.name, n2, n.id + " の第 2 章の名が第 1 章と同じ");
                Assert.AreEqual(n.name, StoryDirector.StageName(story, 1, n));
                Assert.AreEqual(n.name, StoryDirector.StageName(story, 3, n));
                Assert.AreEqual(StoryDirector.StageName(story, 3, n), StoryDirector.StageName(story, 9, n), "用意していない章は最後の章の名");
            }
            Assert.IsNull(story.FindExact(9));
            Assert.AreEqual(story.FindExact(2).title, StoryDirector.ChapterTitle(story, adv, 2));
            Assert.AreEqual("第9章  灯の歩幅", StoryDirector.ChapterTitle(story, adv, 9));
            Assert.AreEqual(adv.chapterName, StoryDirector.ChapterTitle(new StoryConfig(), adv, 9), "story が無ければ adventure の章名");
            Assert.AreEqual("", StoryDirector.StageName(story, 2, null));
        }
    }
}
