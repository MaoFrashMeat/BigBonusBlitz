using System.Collections.Generic;
using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;

namespace BBB.Tests
{
    public class WorkflowAndEnemyTests
    {
        private Dictionary<string, WorkflowRole> _wf;
        private List<EnemyTable> _enemies;

        [SetUp]
        public void SetUp()
        {
            _wf = GameDataLoader.LoadWorkflow();
            _enemies = GameDataLoader.LoadEnemyTables();
        }

        [Test]
        public void 各役のNONEとカテゴリrateの合計が100()
        {
            foreach (var kv in _wf)
            {
                var r = kv.Value;
                int sum = r.NONE;
                foreach (var c in WorkflowLottery.Categories) sum += r.Get(c)?.rate ?? 0;
                Assert.AreEqual(100, sum, kv.Key);
            }
        }

        [Test]
        public void rateが0でないカテゴリのバリアント合計は100か0()
        {
            foreach (var kv in _wf)
                foreach (var c in WorkflowLottery.Categories)
                {
                    var cat = kv.Value.Get(c);
                    if (cat == null || cat.rate == 0) continue;
                    int sum = 0;
                    foreach (var v in WorkflowLottery.Variants) sum += cat.Variant(v);
                    Assert.IsTrue(sum == 100 || sum == 0, $"{kv.Key}.{c} variants sum={sum}");
                }
        }

        [Test]
        public void REPLAYはENEMY当選率50pct()
        {
            var rng = new SystemRandom(7);
            const int n = 200_000;
            int enemy = 0;
            for (int i = 0; i < n; i++)
                if (WorkflowLottery.Run(_wf, "REPLAY", rng).category == "ENEMY") enemy++;
            Assert.AreEqual(0.50, (double)enemy / n, 0.01);
        }

        [Test]
        public void 敵テーブルは3種_スライムはVariantA()
        {
            Assert.AreEqual(3, _enemies.Count);
            var slime = _enemies.Find(t => t.enemyType == "slime");
            Assert.IsNotNull(slime);
            Assert.AreEqual("A", slime.variant);
            Assert.AreEqual(25, slime.defeatProbabilities["BELL"]);
        }

        [Test]
        public void 敵Tier2テーブルの合計は65536()
        {
            foreach (var t in _enemies)
                Assert.IsTrue(new LotteryTable(t.tier2Probabilities).IsValid, t.id);
        }

        [Test]
        public void VariantAならスライム_未指定ならANYかランダム()
        {
            var rng = new SystemRandom(1);
            Assert.AreEqual("slime", EnemyEngage.SelectTable(_enemies, "A", rng).enemyType);
            // Variant B のテーブルは無い → variant 未設定のテーブル（goblin/bat）から選ばれる
            for (int i = 0; i < 50; i++)
            {
                var t = EnemyEngage.SelectTable(_enemies, "B", rng);
                Assert.AreNotEqual("slime", t.enemyType);
            }
        }

        [Test]
        public void 討伐率_CHANCEは100pct_REPLAYは10pct()
        {
            var slime = _enemies.Find(t => t.enemyType == "slime");
            var rng = new SystemRandom(3);
            for (int i = 0; i < 100; i++) Assert.IsTrue(EnemyEngage.RollDefeat(slime, WinType.CHANCE, rng));
            int hit = 0;
            for (int i = 0; i < 100_000; i++) if (EnemyEngage.RollDefeat(slime, WinType.REPLAY, rng)) hit++;
            Assert.AreEqual(0.10, hit / 100_000.0, 0.005);
        }

        [Test]
        public void 予告_ハズレのガセは低確率_チャンス目は高確率()
        {
            var cfg = GameDataLoader.LoadGameConfig().precog;
            var rng = new SystemRandom(11);
            int hazeHit = 0, chanceHit = 0, fakeNonRole = 0;
            for (int i = 0; i < 20_000; i++)
            {
                var h = PrecogDirector.Roll(cfg, Flag.HAZE, rng);
                if (h.stage > 0) { hazeHit++; if (h.role == "HAZE" || !h.IsFake) fakeNonRole++; }
                var c = PrecogDirector.Roll(cfg, Flag.CHANCE_D, rng);
                if (c.stage > 0) chanceHit++;
                Assert.AreEqual("CHANCE", c.role);
            }
            Assert.Less(hazeHit / 20_000.0, 0.10, "ハズレの予告が多すぎる");
            Assert.Greater(chanceHit / 20_000.0, 0.80, "チャンス目の予告が少なすぎる");
            Assert.AreEqual(0, fakeNonRole, "ガセは必ず役の色に化ける");
        }

        [Test]
        public void 揃ったラインが記録される_上段_下段_斜め_チェリー段()
        {
            var payouts = new Payouts { BIG = 300, REG = 90, STAR = 8, WATERMELON = 6, CHERRY = 4, REPLAY = 3 };
            Symbol[] W(Symbol a, Symbol b, Symbol c) => new[] { a, b, c };   // 上・中・下

            // 上段（ライン0）でベル（STAR）が揃う
            var top = WinEvaluator.Evaluate(new[] {
                W(Symbol.STAR, Symbol.BLANK, Symbol.BLANK),
                W(Symbol.STAR, Symbol.BLANK, Symbol.BLANK),
                W(Symbol.STAR, Symbol.BLANK, Symbol.BLANK) }, BonusMode.NORMAL, payouts);
            Assert.AreEqual(WinType.BELL, top.winType);
            Assert.AreEqual(1 << 0, top.lineMask, "上段ラインが記録されていない");

            // 下段（ライン2）
            var bottom = WinEvaluator.Evaluate(new[] {
                W(Symbol.BLANK, Symbol.BLANK, Symbol.STAR),
                W(Symbol.BLANK, Symbol.BLANK, Symbol.STAR),
                W(Symbol.BLANK, Symbol.BLANK, Symbol.STAR) }, BonusMode.NORMAL, payouts);
            Assert.AreEqual(1 << 2, bottom.lineMask, "下段ラインが記録されていない");

            // 右下がり（ライン3: 上→中→下）
            var diag = WinEvaluator.Evaluate(new[] {
                W(Symbol.STAR, Symbol.BLANK, Symbol.BLANK),
                W(Symbol.BLANK, Symbol.STAR, Symbol.BLANK),
                W(Symbol.BLANK, Symbol.BLANK, Symbol.STAR) }, BonusMode.NORMAL, payouts);
            Assert.AreEqual(1 << 3, diag.lineMask, "斜めラインが記録されていない");

            // チェリーは左リールの止まった段を記録（ライン成立ではない）
            var che = WinEvaluator.Evaluate(new[] {
                W(Symbol.BLANK, Symbol.CHERRY, Symbol.BLANK),
                W(Symbol.BLANK, Symbol.BLANK, Symbol.BLANK),
                W(Symbol.BLANK, Symbol.BLANK, Symbol.BLANK) }, BonusMode.NORMAL, payouts);
            Assert.AreEqual(WinType.CHERRY, che.winType);
            Assert.AreEqual(1 << 1, che.cherryMask, "チェリーの段が記録されていない");
            Assert.AreEqual(0, che.lineMask);
        }

        [Test]
        public void JSONのリストが既定値に追記されず置き換えになる()
        {
            // 既定値つきの List に JSON を読むと Json.NET が「追記」してしまう事故の再発防止
            var cfg = GameDataLoader.LoadGameConfig();
            Assert.AreEqual(3, cfg.at.monsters.Count, "モンスターが既定値と二重になっている");
            Assert.AreEqual(6, cfg.atExpect.ranks.Count, "期待度ランクが既定値と二重になっている");
            CollectionAssert.AllItemsAreUnique(cfg.at.monsters.ConvertAll(x => x.id));
            CollectionAssert.AllItemsAreUnique(cfg.atExpect.ranks.ConvertAll(x => x.name));
            CollectionAssert.AllItemsAreUnique(cfg.hero.bonusPrecursorLines);
            foreach (var kv in cfg.hero.lines) CollectionAssert.AllItemsAreUnique(kv.Value, $"hero.lines[{kv.Key}] が重複");
            foreach (var kv in cfg.hero.precursorLines) CollectionAssert.AllItemsAreUnique(kv.Value, $"precursorLines[{kv.Key}] が重複");
        }

        [Test]
        public void AT_ナビに従うと得_外すと損_バトルは討伐で上乗せ()
        {
            var cfg = GameDataLoader.LoadGameConfig();
            Assert.IsNotNull(cfg.at, "at が読めていない");
            Assert.Greater(cfg.at.naviCorrectPayout, cfg.at.naviWrongPayout, "ナビ正解の方が少ない");
            Assert.Greater(cfg.at.initialSpins, 0);

            // AT 中の小役テーブルは 65536。ボーナスは 0（AT 中に割り込ませない）
            int total = 0, bonus = 0;
            foreach (var kv in cfg.probabilities_AT)
            {
                total += kv.Value;
                if (kv.Key.StartsWith("BB_") || kv.Key.StartsWith("RB_")) bonus += kv.Value;
            }
            Assert.AreEqual(65536, total, "probabilities_AT の合計が 65536 でない");
            Assert.AreEqual(0, bonus, "AT 中にボーナスが抽選される設定になっている");

            // モンスターは重み・HP・狩猟G・報酬が正しく入っているか
            foreach (var mon in cfg.at.monsters)
            {
                Assert.Greater(mon.hp, 0, $"{mon.name} の HP");
                Assert.Greater(mon.spins, 0, $"{mon.name} の狩猟G");
                Assert.Greater(mon.weight, 0, $"{mon.name} の重み");
            }

            // ダメージ抽選: チャンス目は一撃、ハズレは 0
            var rng = new SystemRandom(4);
            for (int i = 0; i < 50; i++)
            {
                Assert.GreaterOrEqual(AtDirectorEx.RollDamage(cfg.at, Flag.CHANCE_D, rng), 999, "チャンス目が一撃討伐でない");
                Assert.AreEqual(0, AtDirectorEx.RollDamage(cfg.at, Flag.HAZE, rng), "ハズレでダメージが出ている");
            }
        }

        [Test]
        public void ショップ_買うとレベルが上がり効果が乗る_ソウル不足は弾かれる()
        {
            var cfg = GameDataLoader.LoadGameConfig();
            Assert.IsNotNull(cfg.shop, "shop が読めていない");
            Assert.IsNotNull(cfg.souls, "souls が読めていない");
            Assert.Greater(cfg.shop.items.Count, 0);
            CollectionAssert.AllItemsAreUnique(cfg.shop.items.ConvertAll(x => x.id), "品IDが重複");

            var w = new PlayerWallet { Souls = 1_000_000 };
            foreach (var it in cfg.shop.items)
            {
                Assert.Greater(it.maxLevel, 0, it.name);
                Assert.Greater(it.baseCost, 0, it.name);
                // 上限まで買えて、上限を超えると買えない
                for (int lv = 0; lv < it.maxLevel; lv++)
                {
                    Assert.AreEqual(lv, w.LevelOf(it.id));
                    Assert.IsTrue(ShopDirector.Buy(w, it), $"{it.name} Lv{lv} が買えない");
                }
                Assert.AreEqual(it.maxLevel, w.LevelOf(it.id));
                Assert.AreEqual(-1, ShopDirector.NextCost(it, it.maxLevel), $"{it.name} が上限を超えて買える");
                Assert.IsFalse(ShopDirector.Buy(w, it), $"{it.name} が上限を超えて買えた");
                // 効果が所持レベルぶん乗る（同じ効果キーの他の品ぶんは差し引いて比べる）
                int mine = ShopDirector.EffectTotal(cfg.shop, w, it.effect) - OtherItemsWithSameEffect(cfg, w, it);
                Assert.AreEqual(it.valuePerLevel * it.maxLevel, mine, $"{it.name} の効果量が合わない");
            }

            // ソウルが足りなければ買えない
            var poor = new PlayerWallet { Souls = 0 };
            Assert.IsFalse(ShopDirector.Buy(poor, cfg.shop.items[0]), "ソウル 0 で買えてしまう");
            Assert.AreEqual(0, poor.LevelOf(cfg.shop.items[0].id));
        }

        /// <summary>同じ効果キーを持つ他の品ぶんの効果量（合計から差し引くため）。</summary>
        private static int OtherItemsWithSameEffect(GameConfig cfg, PlayerWallet w, ShopItem target)
        {
            int sum = 0;
            foreach (var it in cfg.shop.items)
                if (it.id != target.id && it.effect == target.effect) sum += it.valuePerLevel * w.LevelOf(it.id);
            return sum;
        }

        [Test]
        public void ソウル_討伐で貯まり取得量アップが効く()
        {
            var m = GameDataLoader.CreateMachine(new SystemRandom(2024));
            var push = new SystemRandom(11);
            m.Credit = 5_000_000;
            Assert.AreEqual(0, m.Wallet.Souls);
            int guard = 0;
            while (m.Wallet.Souls == 0 && guard++ < 100_000)
            {
                Assert.IsTrue(m.MaxBet());
                m.Lever();
                for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
                m.Evaluate();
            }
            Assert.Greater(m.Wallet.Souls, 0, "討伐してもソウルが貯まらない");
            Assert.AreEqual(m.Wallet.Souls, m.Wallet.TotalSouls, "累計が合わない");
        }

        [Test]
        public void 主人公のひとりごと_確率どおりに出て文脈に沿う()
        {
            var cfg = GameDataLoader.LoadGameConfig().hero;
            Assert.IsNotNull(cfg);
            var rng = new SystemRandom(8);
            int spoke = 0;
            for (int i = 0; i < 10_000; i++) if (HeroDirector.Roll(cfg, "idle", 0, rng) != null) spoke++;
            Assert.AreEqual(cfg.monologueRate / 100.0, spoke / 10_000.0, 0.01);
            var all = new HeroConfig { monologueRate = 100 };
            for (int i = 0; i < 200; i++)
            {
                var w = HeroDirector.Roll(all, "win", 0, rng);
                CollectionAssert.Contains(all.lines["win"], w);
                var lr = HeroDirector.Roll(all, "idle", 999, rng);
                Assert.IsTrue(all.lines["idle"].Contains(lr) || all.lines["longRun"].Contains(lr));
            }
            Assert.IsNull(HeroDirector.Roll(new HeroConfig { monologueRate = 0 }, "idle", 0, rng));
        }

        [Test]
        public void 討伐率_連続小役でストリークボーナス加算()
        {
            var slime = _enemies.Find(t => t.enemyType == "slime");
            var rng = new SystemRandom(5);
            int hit = 0;
            for (int i = 0; i < 100_000; i++) if (EnemyEngage.RollDefeat(slime, WinType.BELL, rng, streak: 2, streakBonus: 15)) hit++;
            Assert.AreEqual(0.55, hit / 100_000.0, 0.01);
        }
    }
}
