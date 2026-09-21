using System.Collections.Generic;
using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;

namespace BBB.Tests
{
    /// <summary>工房（Craft.cs）: 作った装備は恒久・接辞なし、素材はダンジョンの落とし物、拾い物は帰還で流れる。</summary>
    public class CraftTests
    {
        private static CraftConfig Cfg()
        {
            var c = new CraftConfig();
            c.materials.Add(new MaterialDef { id = "iron", name = "鉄鉱" });
            c.recipes.Add(new CraftRecipe { id = "sw", name = "鉄の剣", slot = EquipSlot.Weapon, tier = 2, effect = ShopEffects.EngageDamage, value = 8, souls = 100, materialIds = new List<string> { "iron" }, materialCounts = new List<int> { 3 }, unlockChapter = 0 });
            c.recipes.Add(new CraftRecipe { id = "late", name = "聖剣", slot = EquipSlot.Weapon, tier = 4, effect = ShopEffects.EngageDamage, value = 22, souls = 100, unlockChapter = 3 });
            return c;
        }

        [Test]
        public void 作るとソウルと素材が減り_空き枠に着く()
        {
            var cfg = Cfg(); var w = new PlayerWallet { Souls = 250 }; w.AddMaterial("iron", 5); var inv = new EquipInventory();
            var r = cfg.Recipe("sw");
            Assert.AreEqual("", CraftDirector.Missing(cfg, w, inv, r, 0));
            var it = CraftDirector.Craft(cfg, w, inv, r, 0, out bool worn);
            Assert.IsNotNull(it); Assert.IsTrue(worn, "空き枠なら着ける");
            Assert.AreEqual(150, w.Souls); Assert.AreEqual(2, w.MaterialCount("iron"));
            Assert.IsTrue(it.crafted); Assert.AreEqual("sw", it.craftId); Assert.AreEqual(8, it.Of(ShopEffects.EngageDamage));
            Assert.AreEqual(1, it.effectKeys.Count, "接辞は付かない");
            Assert.AreEqual(8, inv.EffectTotal(ShopEffects.EngageDamage));
            // 2 度は作れない
            Assert.AreEqual("作成済み", CraftDirector.Missing(cfg, w, inv, r, 0));
            Assert.IsNull(CraftDirector.Craft(cfg, w, inv, r, 0, out _));
        }

        [Test]
        public void 足りない理由と章の解放()
        {
            var cfg = Cfg(); var w = new PlayerWallet { Souls = 50 }; var inv = new EquipInventory();
            Assert.AreEqual("ソウルが足りない", CraftDirector.Missing(cfg, w, inv, cfg.Recipe("sw"), 0));
            w.Souls = 500;
            Assert.AreEqual("鉄鉱 が足りない", CraftDirector.Missing(cfg, w, inv, cfg.Recipe("sw"), 0));
            Assert.AreEqual("第3章を踏破すると作れる", CraftDirector.Missing(cfg, w, inv, cfg.Recipe("late"), 2));
            Assert.AreEqual("", CraftDirector.Missing(cfg, w, inv, cfg.Recipe("late"), 3));
        }

        [Test]
        public void 帰還で拾い物は流れ_工房の品は残る_鞄の数にも入らない()
        {
            var cfg = Cfg(); var w = new PlayerWallet { Souls = 500 }; w.AddMaterial("iron", 9); var inv = new EquipInventory();
            CraftDirector.Craft(cfg, w, inv, cfg.Recipe("sw"), 0, out _);
            var found = new EquipItem { baseId = "w_blade", name = "刃", slot = EquipSlot.Weapon }; found.effectKeys.Add(ShopEffects.DefeatBonus); found.effectValues.Add(5);
            var ec = new EquipConfig { bagSize = 2 };
            Assert.IsTrue(EquipDirector.PickUp(ec, inv, found));
            Assert.AreEqual(1, inv.BagCount, "工房の品は鞄の数に入らない");
            var second = new EquipItem { baseId = "w_bow", name = "弓", slot = EquipSlot.Weapon };
            Assert.IsTrue(EquipDirector.PickUp(ec, inv, second));
            Assert.AreEqual(2, inv.BagCount);
            // 売れない
            Assert.AreEqual(0, EquipDirector.Sell(ec, inv, w, inv.WornOf(EquipSlot.Weapon)));
            Assert.IsNotNull(inv.WornOf(EquipSlot.Weapon));
            inv.ClearFound();
            Assert.AreEqual(0, inv.BagCount); Assert.AreEqual(0, inv.Bag.Count);
            Assert.IsNotNull(inv.WornOf(EquipSlot.Weapon)); Assert.IsTrue(inv.WornOf(EquipSlot.Weapon).crafted);
        }

        [Test]
        public void 装備の上乗せがエンゲージに効く()
        {
            var e = new EngageConfig();
            Assert.AreEqual(e.damageLarge + 12, EngageBattle.Damage(AttackSize.Large, e, 12));
            Assert.AreEqual(45, EngageBattle.JudgePercent(65, 100, EngageRole.Lose, e, 10));
            int defeat = 0; var rng = new SystemRandom(5);
            for (int i = 0; i < 4000; i++) if (EngageBattle.Roulette(EngageRole.Small, rng, 25) == EngageOutcome.PotionDefeat) defeat++;
            Assert.That(defeat / 4000.0, Is.EqualTo(0.25).Within(0.03), "小役で討伐確定になる率");
            Assert.AreEqual(EngageOutcome.PotionLarge, EngageBattle.Roulette(EngageRole.Small, rng, 0));
            // 防御の重みを大きくすれば防御が増える
            int guard = 0;
            for (int i = 0; i < 4000; i++) if (EngageBattle.Resolve(EngageStance.HeroCharge, EngageRole.Lose, e, rng, out _, 300, 0) == EngageOutcome.Guard) guard++;
            Assert.Greater(guard / 4000.0, 0.7);
        }

        [Test]
        public void 素材の落とし物は財布に貯まる()
        {
            var m = GameDataLoader.CreateMachine(new SystemRandom(3), 1);
            var src = new DropSource { equipRate = 0 };
            src.items.Add(new DropEntry { kind = "material", id = "iron", name = "鉄鉱", amount = 2, rate = 100 });
            var r = new GameResult();
            m.RollDrops(r, src);
            Assert.AreEqual(2, m.Wallet.MaterialCount("iron"));
            Assert.AreEqual(1, r.materialsGained.Count); Assert.AreEqual("鉄鉱", r.materialsGained[0].name);
            Assert.IsTrue(m.Config.craft != null && m.Config.craft.recipes.Count >= 50, "game_config の工房に 50 の図面");
            foreach (var rc in m.Config.craft.recipes)
                foreach (var id in rc.materialIds) Assert.IsNotNull(m.Config.craft.Material(id), rc.id + " の素材 " + id + " が craft.materials に無い");
        }
    }
}
