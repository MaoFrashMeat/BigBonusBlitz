using System.Collections.Generic;
using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;

namespace BBB.Tests
{
    public class PlayerStatsTests
    {
        private static SlotMachine NewMachine(int seed) => GameDataLoader.CreateMachine(new SystemRandom(seed), 1);

        [Test]
        public void 設定ファイルのステータスが読み込める()
        {
            var m = NewMachine(1);
            var cfg = m.Config.stats;
            Assert.IsNotNull(cfg, "stats が無い");
            Assert.IsTrue(cfg.enabled);
            Assert.Greater(cfg.pointsPerLevel, 0);
            Assert.Greater(cfg.maxPerStat, 0);
            Assert.AreEqual(0, m.Stats.Total, "はじめは 0");
            Assert.AreEqual(0, m.Stats.Unspent);
        }

        [Test]
        public void 上限まで振れて上限を超えられない()
        {
            var m = NewMachine(2);
            var cfg = m.Config.stats;
            m.Stats.Unspent = cfg.maxPerStat + 5;
            for (int i = 0; i < cfg.maxPerStat; i++)
                Assert.IsTrue(StatsDirector.Spend(cfg, m.Stats, "life"), $"{i + 1} 回目で振れなくなった");
            Assert.AreEqual(cfg.maxPerStat, m.Stats.Life);
            Assert.IsFalse(StatsDirector.Spend(cfg, m.Stats, "life"), "上限を超えて振れた");
            Assert.AreEqual(5, m.Stats.Unspent, "上限で弾いたのにポイントが減った");
        }

        [Test]
        public void 振り直すとポイントが全部戻る()
        {
            var m = NewMachine(3);
            var cfg = m.Config.stats;
            m.Stats.Unspent = 9;
            for (int i = 0; i < 3; i++) StatsDirector.Spend(cfg, m.Stats, "life");
            for (int i = 0; i < 3; i++) StatsDirector.Spend(cfg, m.Stats, "technique");
            for (int i = 0; i < 3; i++) StatsDirector.Spend(cfg, m.Stats, "luck");
            Assert.AreEqual(0, m.Stats.Unspent);
            Assert.AreEqual(9, StatsDirector.Respec(m.Stats));
            Assert.AreEqual(9, m.Stats.Unspent);
            Assert.AreEqual(0, m.Stats.Total);
        }

        [Test]
        public void ポイントが無ければ振れない()
        {
            var m = NewMachine(4);
            m.Stats.Unspent = 0;
            Assert.IsFalse(StatsDirector.Spend(m.Config.stats, m.Stats, "luck"));
            Assert.AreEqual(0, m.Stats.Luck);
        }

        [Test]
        public void ライフは回復薬の効きを伸ばす()
        {
            var m = NewMachine(5);
            int baseSpins = m.TorchSpinsPerUnit;
            m.Stats.Life = m.Config.stats.maxPerStat;
            int grown = m.TorchSpinsPerUnit;
            Assert.Greater(grown, baseSpins, "ライフを振っても回復薬の効きが伸びない");
            Assert.AreEqual(baseSpins + (int)(m.Stats.Life * m.Config.stats.life.torchSpins), grown);
        }

        [Test]
        public void テクニックはエンゲージのG数を伸ばす()
        {
            var m = NewMachine(6);
            int baseSpins = m.EngageMaxSpins;
            m.Stats.Technique = m.Config.stats.maxPerStat;
            Assert.GreaterOrEqual(m.EngageMaxSpins, baseSpins, "エンゲージのG数が減った");
            // テクニックの +G は 2 G で 1 セット（ターン制）。端数は切り捨て
            Assert.AreEqual(baseSpins + (int)(m.Stats.Technique * m.Config.stats.technique.engageSpins) / 2 * 2, m.EngageMaxSpins);
        }

        [Test]
        public void ライフを振るとBETが無料になることがある()
        {
            var m = NewMachine(7);
            m.Stats.Life = m.Config.stats.maxPerStat;
            m.Credit = 1_000_000;
            var push = new SystemRandom(3);
            int free = 0, paid = 0;
            for (int g = 0; g < 4000; g++)
            {
                bool wasReplay = m.IsReplay;
                int before = m.Credit;
                Assert.IsTrue(m.MaxBet());
                int used = before - m.Credit;
                if (!wasReplay) { if (used == 0) free++; else { paid++; Assert.AreEqual(SlotMachine.BetCost, used); } }
                m.Lever();
                for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
                m.Evaluate();
            }
            Assert.Greater(free, 0, "一度も無料にならない");
            Assert.Greater(paid, free, "無料の方が多いのはおかしい");
        }

        [Test]
        public void ラックを振るとハズレが減る()
        {
            int CountHaze(int luck)
            {
                var m = NewMachine(11);
                m.Config.stats.luck.rareRate = 1.5f;     // 1 点あたり 1.5%（20 点で 30%）。本番の値だと揺れに埋もれる
                m.Config.stats.luck.replayRate = 1.5f;
                m.Stats.Luck = luck;
                m.Credit = 10_000_000;
                var push = new SystemRandom(5);
                int haze = 0;
                for (int g = 0; g < 20000; g++)
                {
                    // 通常時だけを数える（ボーナス中・持ち越し中はラックの対象外）
                    bool normal = m.BonusMode == BonusMode.NORMAL && m.HeldBonusFlag == Flag.HAZE && !m.InAt;
                    Assert.IsTrue(m.MaxBet());
                    m.Lever();
                    for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
                    m.Evaluate();
                    if (normal && m.CurrentFlag == Flag.HAZE) haze++;
                }
                return haze;
            }
            int none = CountHaze(0);
            int full = CountHaze(20);
            Assert.Less(full, none, "ラックを振ってもハズレが減らない");
        }

        [Test]
        public void レベルアップでポイントが配られる()
        {
            var m = NewMachine(13);
            int per = m.Config.stats.pointsPerLevel;
            int lv = m.PlayerLevel;
            m.Credit = 5_000_000;
            var push = new SystemRandom(9);
            for (int g = 0; g < 60000 && m.PlayerLevel == lv; g++)
            {
                Assert.IsTrue(m.MaxBet());
                m.Lever();
                for (int i = 0; i < 3; i++) m.Stop(i, push.Next(20));
                m.Evaluate();
            }
            Assert.Greater(m.PlayerLevel, lv, "レベルが上がらない");
            Assert.GreaterOrEqual(m.Stats.Unspent, per, "レベルが上がったのにポイントが増えていない");
        }

        [Test]
        public void 実績は数えものが規定に届くと解除され_報酬のソウルが入る()
        {
            var m = NewMachine(7);
            m.Achievements = new List<AchievementDef>
            {
                new AchievementDef { id = "first_spin", name = "はじめの一回転", counter = AchievementCounters.Spins, target = 1, rewardSouls = 30 },
                new AchievementDef { id = "spins_3", name = "三回転", counter = AchievementCounters.Spins, target = 3, rewardSouls = 10 },
            };
            m.Credit = 1000;
            long souls = m.Wallet.Souls;
            int unlockedTotal = 0;
            for (int g = 0; g < 3; g++)
            {
                m.MaxBet(); m.Lever();
                for (int i = 0; i < 3; i++) m.Stop(i, 0);
                var r = m.Evaluate();
                var got = AchievementDirector.Track(m.Achievements, m.Ach, r, m);
                unlockedTotal += got.Count;
                if (g == 0) { Assert.AreEqual(1, got.Count, "1 回転目で解除されない"); Assert.AreEqual("first_spin", got[0].id); }
            }
            Assert.AreEqual(2, unlockedTotal, "解除の数が違う（二重に解除しているか、3 回転で解除されていない）");
            Assert.IsTrue(m.Ach.Unlocked.Contains("spins_3"));
            Assert.AreEqual(3, m.Ach.Get(AchievementCounters.Spins));
            Assert.AreEqual(souls + 40, m.Wallet.Souls, "報酬のソウルが入っていない");
        }

        [Test]
        public void 実績の一覧が読めて_数えもののキーが全部知っているものである()
        {
            var defs = GameDataLoader.LoadAchievements();
            Assert.Greater(defs.Count, 10, "achievements.json が読めていない");
            var known = new HashSet<string>();
            foreach (var f in typeof(AchievementCounters).GetFields()) known.Add((string)f.GetValue(null));
            var ids = new HashSet<string>();
            foreach (var d in defs)
            {
                Assert.IsTrue(ids.Add(d.id), "id が重複: " + d.id);
                Assert.IsTrue(known.Contains(d.counter), d.id + " の counter が知らない名前: " + d.counter);
                Assert.Greater(d.target, 0, d.id + " の target が 0");
            }
        }

        [Test]
        public void 売った数と図鑑の種類数と呪い付きの踏破が数えられる()
        {
            var m = NewMachine(9);
            var cfg = m.Config.equipment;
            EquipItem Item(int rarity) => new EquipItem { baseId = "t", name = "品", slot = EquipSlot.Weapon, rarity = rarity, level = 1,
                effectKeys = new List<string> { ShopEffects.StatLife }, effectValues = new List<int> { 1 } };
            var a = Item(0); var b = Item(0); var c = Item(1);
            foreach (var it in new[] { a, b, c }) m.Equip.Bag.Add(it);
            m.SellEquip(a);
            Assert.AreEqual(1, m.Ach.Get(AchievementCounters.Sold));
            m.SellEquipBelow(0, out int n);
            Assert.AreEqual(1, n); Assert.AreEqual(2, m.Ach.Get(AchievementCounters.Sold), "まとめ売りが数えられていない");
            Assert.AreEqual(0, m.SellEquip(a), "売った物をもう一度売れる"); Assert.AreEqual(2, m.Ach.Get(AchievementCounters.Sold));

            // 図鑑: 同じ種類を 2 回拾っても種類数は 1
            var r1 = new GameResult { equipDropped = new EquipItem { baseId = "x1" } };
            var r2 = new GameResult { equipDropped = new EquipItem { baseId = "x1" } };
            var r3 = new GameResult { equipDropped = new EquipItem { baseId = "x2" } };
            foreach (var r in new[] { r1, r2, r3 }) AchievementDirector.Track(null, m.Ach, r, m);
            Assert.AreEqual(2, m.Ach.Get(AchievementCounters.Codex));
            Assert.AreEqual(2, m.Ach.Get(AchievementCounters.SeenPrefix + "x1"));
            // 古いセーブ（seen: だけあって codex が無い）でも、次の Track で拾った種類数から始まる
            m.Ach.Counters[AchievementCounters.SeenPrefix + "y1"] = 5;
            AchievementDirector.Track(null, m.Ach, new GameResult(), m);
            Assert.AreEqual(3, m.Ach.Get(AchievementCounters.Codex), "seen: から数え直していない");

            // 呪いを 3 つ抱えて踏破
            for (int i = 0; i < 3; i++) m.Curse.Taken.Add(new CurseInstance());
            AchievementDirector.Track(null, m.Ach, new GameResult { chapterCleared = true }, m);
            Assert.AreEqual(1, m.Ach.Get(AchievementCounters.CursedChapters));
            m.Curse.Taken.RemoveAt(0);
            AchievementDirector.Track(null, m.Ach, new GameResult { chapterCleared = true }, m);
            Assert.AreEqual(1, m.Ach.Get(AchievementCounters.CursedChapters), "2 つでは数えない");
            // Perfect!!
            AchievementDirector.Track(null, m.Ach, new GameResult { techSuccess = true, techRank = new TechRankDef { id = "perfect" } }, m);
            AchievementDirector.Track(null, m.Ach, new GameResult { techSuccess = true, techRank = new TechRankDef { id = "cool" } }, m);
            Assert.AreEqual(1, m.Ach.Get(AchievementCounters.TechPerfect));
        }

        [Test]
        public void 落とし物は表の率で落ち_ソウルと回復薬が増える()
        {
            var m = NewMachine(8);
            var src = new DropSource
            {
                equipRate = 0,
                items = new List<DropEntry>
                {
                    new DropEntry { kind = "souls", name = "ソウルの欠片", amount = 10, rate = 100 },
                    new DropEntry { kind = "torch", name = "回復薬", amount = 1, rate = 100 },
                    new DropEntry { kind = "embers", name = "落ちない", amount = 5, rate = 0 },
                },
            };
            long souls = m.Wallet.Souls; int torches = m.Adv.torches;
            var r = new GameResult();
            m.RollDrops(r, src);
            Assert.AreEqual(2, r.itemDrops.Count, "率 100% の 2 つが落ち、0% は落ちない");
            Assert.AreEqual(r.soulsGained, m.Wallet.Souls - souls, "ソウルの加算が結果と合わない");
            Assert.GreaterOrEqual(m.Wallet.Souls, souls + 10);
            Assert.AreEqual(torches + 1, m.Adv.torches, "回復薬が増えていない");
            // 設定の表: 4 つの出どころが読めている
            foreach (var key in new[] { "mob", "boss", "hunt", "treasure" })
                Assert.IsNotNull(m.DropsOf(key), key + " の落とし物の表が無い");
            Assert.Greater(m.DropsOf("boss").items.Count, 0, "ボスの落とし物が無い");
        }

        [Test]
        public void 装備の枠は8つ_アクセは3つ着けられて4つ目は一番弱いものと入れ替わる()
        {
            var m = NewMachine(5);
            Assert.AreEqual(8, EquipSlot.All.Length);
            EquipItem Acc(int power) => new EquipItem { baseId = "t", name = "アクセ" + power, slot = EquipSlot.Accessory,
                effectKeys = new List<string> { ShopEffects.StatLuck }, effectValues = new List<int> { power } };
            var a1 = Acc(1); var a2 = Acc(2); var a3 = Acc(3); var a4 = Acc(4);
            foreach (var a in new[] { a1, a2, a3, a4 }) m.Equip.Bag.Add(a);
            EquipDirector.Equip(m.Equip, a1); EquipDirector.Equip(m.Equip, a2); EquipDirector.Equip(m.Equip, a3);
            Assert.IsTrue(m.Equip.IsWorn(a1) && m.Equip.IsWorn(a2) && m.Equip.IsWorn(a3), "アクセが 3 つ着けられない");
            Assert.AreEqual(6, m.LuckStat, "装備のラックが合算されていない");
            EquipDirector.Equip(m.Equip, a4);
            Assert.IsTrue(m.Equip.IsWorn(a4), "4 つ目が着けられない");
            Assert.IsFalse(m.Equip.IsWorn(a1), "一番弱いものが外れていない");
            Assert.IsTrue(m.Equip.Bag.Contains(a1), "外れた品が鞄に戻っていない");
            Assert.AreEqual(9, m.LuckStat);
        }

        [Test]
        public void 装備の種類ごとに品があり_昔の部位名も読み替えられる()
        {
            var m = NewMachine(6);
            var bases = m.Config.equipment.bases;
            foreach (var kind in EquipSlot.Kinds)
                Assert.IsTrue(bases.Exists(b => EquipSlot.Normalize(b.slot) == kind), kind + " の品が無い");
            Assert.AreEqual(EquipSlot.Body, EquipSlot.Normalize("armor"));
            Assert.AreEqual(EquipSlot.Accessory, EquipSlot.KindOf(EquipSlot.Normalize("trinket")));
            // 何度か作っても部位は 6 種類のどれかになる
            var rng = new SystemRandom(9);
            for (int i = 0; i < 200; i++)
            {
                var it = EquipDirector.Roll(m.Config.equipment, 1 + i % 8, rng);
                Assert.IsNotNull(it);
                Assert.Contains(it.slot, EquipSlot.Kinds, "知らない部位: " + it.slot);
            }
        }

        [Test]
        public void 装備を売るとレア度と深さのソウルが入り_着けていれば外れる()
        {
            var m = NewMachine(7);
            var cfg = m.Config.equipment;
            EquipItem Item(int rarity, int depth) => new EquipItem { baseId = "t", name = "品", slot = EquipSlot.Weapon, rarity = rarity, level = depth,
                effectKeys = new List<string> { ShopEffects.StatLife }, effectValues = new List<int> { 1 } };
            var cheap = Item(0, 1); var deep = Item(0, 8); var top = Item(cfg.rarities.Count - 1, 1);
            Assert.AreEqual(cfg.rarities[0].sellSouls, EquipDirector.SellValue(cfg, cheap), "深さ 1 はレア度の売値そのまま");
            Assert.Greater(EquipDirector.SellValue(cfg, deep), EquipDirector.SellValue(cfg, cheap), "深いほど高い");
            Assert.Greater(EquipDirector.SellValue(cfg, top), EquipDirector.SellValue(cfg, deep), "レアほど高い");
            foreach (var it in new[] { cheap, deep, top }) m.Equip.Bag.Add(it);
            EquipDirector.Equip(m.Equip, top);
            Assert.IsTrue(m.Equip.IsWorn(top));
            int before = m.Wallet.Souls;
            int got = EquipDirector.Sell(cfg, m.Equip, m.Wallet, top);
            Assert.AreEqual(EquipDirector.SellValue(cfg, top), got);
            Assert.AreEqual(before + got, m.Wallet.Souls, "ソウルが入っていない");
            Assert.IsFalse(m.Equip.IsWorn(top), "売った品が着いたまま");
            Assert.IsFalse(m.Equip.Bag.Contains(top), "売った品が鞄に残っている");
            Assert.AreEqual(0, EquipDirector.Sell(cfg, m.Equip, m.Wallet, top), "二度売れる");
        }

        [Test]
        public void まとめて売るはレア度以下だけで_着けている物は残る()
        {
            var m = NewMachine(8);
            var cfg = m.Config.equipment;
            EquipItem Item(int rarity, int depth) => new EquipItem { baseId = "t", name = "品", slot = EquipSlot.Weapon, rarity = rarity, level = depth,
                effectKeys = new List<string> { ShopEffects.StatLife }, effectValues = new List<int> { 1 } };
            var a = Item(0, 1); var b = Item(0, 5); var c = Item(1, 2); var d = Item(2, 1); var worn = Item(0, 3);
            foreach (var it in new[] { a, b, c, d, worn }) m.Equip.Bag.Add(it);
            EquipDirector.Equip(m.Equip, worn);
            Assert.AreEqual(2, EquipDirector.SellableBelow(m.Equip, 0).Count, "並以下で着けていないのは 2 つ");
            Assert.AreEqual(3, EquipDirector.SellableBelow(m.Equip, 1).Count);
            int before = m.Wallet.Souls;
            int expect = EquipDirector.SellValue(cfg, a) + EquipDirector.SellValue(cfg, b);
            int got = EquipDirector.SellBelow(cfg, m.Equip, m.Wallet, 0, out int n);
            Assert.AreEqual(2, n); Assert.AreEqual(expect, got);
            Assert.AreEqual(before + got, m.Wallet.Souls);
            Assert.IsTrue(m.Equip.IsWorn(worn), "着けている物を売った");   // 着けている物は鞄でなく Worn にある
            Assert.IsTrue(m.Equip.Bag.Contains(c) && m.Equip.Bag.Contains(d), "上のレア度を売った");
            Assert.AreEqual(0, EquipDirector.SellBelow(cfg, m.Equip, m.Wallet, 0, out n)); Assert.AreEqual(0, n);
        }
    }
}
