using BBB.Core;
using NUnit.Framework;

namespace BBB.Tests
{
    /// <summary>ENEMY エンゲージのターン制（Engage.cs）。本人の仕様（2026-09-21）どおりに表が引けるか。</summary>
    public class EngageBattleTests
    {
        private static readonly EngageConfig Cfg = new EngageConfig();

        [Test]
        public void 役の分類_ハズレ小役レア役()
        {
            Assert.AreEqual(EngageRole.Lose, EngageBattle.RoleOf(WinType.NONE, false, false, Cfg));
            Assert.AreEqual(EngageRole.Small, EngageBattle.RoleOf(WinType.BELL, false, false, Cfg));
            Assert.AreEqual(EngageRole.Small, EngageBattle.RoleOf(WinType.REPLAY, false, false, Cfg));
            Assert.AreEqual(EngageRole.Rare, EngageBattle.RoleOf(WinType.CHERRY, false, false, Cfg));
            Assert.AreEqual(EngageRole.Rare, EngageBattle.RoleOf(WinType.WATERMELON, false, false, Cfg));
            Assert.AreEqual(EngageRole.Rare, EngageBattle.RoleOf(WinType.CHANCE, false, false, Cfg));
            Assert.AreEqual(EngageRole.Small, EngageBattle.RoleOf(WinType.BELL, true, false, Cfg), "択ナビ正解もただの小役（本人 2026-09-23）");
            Assert.AreEqual(EngageRole.Lose, EngageBattle.RoleOf(WinType.NONE, false, true, Cfg), "択ナビ失敗はハズレ");
        }

        [Test]
        public void リプレイは_揃った結果からでも小役()
        {
            // WinEvaluator はリプレイを isReplay だけで返す（winType は NONE）。その形のまま渡しても小役になること
            var replay = new WinResult { isReplay = true, winType = WinType.NONE };
            Assert.AreEqual(EngageRole.Small, EngageBattle.RoleOf(replay, false, false, Cfg), "EE ではリプレイも小役（2026-09-21）");
            Assert.AreEqual(EngageRole.Lose, EngageBattle.RoleOf(new WinResult { winType = WinType.NONE }, false, false, Cfg));
            Assert.AreEqual(EngageRole.Rare, EngageBattle.RoleOf(new WinResult { winType = WinType.CHERRY }, false, false, Cfg));
        }

        [Test]
        public void 一ターン目_役で構えが決まる()
        {
            Assert.AreEqual(EngageStance.EnemyAttack, EngageBattle.StanceOf(EngageRole.Lose));
            Assert.AreEqual(EngageStance.HeroCharge, EngageBattle.StanceOf(EngageRole.Small));
            Assert.AreEqual(EngageStance.HeroAttack, EngageBattle.StanceOf(EngageRole.Rare));
        }

        [Test]
        public void 二ターン目_敵の攻撃の構え()
        {
            var rng = new SystemRandom(1);
            Assert.AreEqual(EngageOutcome.Hit, EngageBattle.Resolve(EngageStance.EnemyAttack, EngageRole.Lose, Cfg, rng, out _));
            Assert.AreEqual(EngageOutcome.Dodge, EngageBattle.Resolve(EngageStance.EnemyAttack, EngageRole.Small, Cfg, rng, out _));
            Assert.AreEqual(EngageOutcome.Counter, EngageBattle.Resolve(EngageStance.EnemyAttack, EngageRole.Rare, Cfg, rng, out var size));
            Assert.AreEqual(AttackSize.Counter, size);
        }

        [Test]
        public void 二ターン目_力を貯めた構え()
        {
            var rng = new SystemRandom(2);
            Assert.AreEqual(EngageOutcome.Attack, EngageBattle.Resolve(EngageStance.HeroCharge, EngageRole.Small, Cfg, rng, out var s1));
            Assert.AreEqual(AttackSize.Medium, s1);
            Assert.AreEqual(EngageOutcome.Attack, EngageBattle.Resolve(EngageStance.HeroCharge, EngageRole.Rare, Cfg, rng, out var s2));
            Assert.AreEqual(AttackSize.Large, s2);
            // ハズレは 被弾 / 防御 / 回避 のどれか（重み 70 / 15 / 15）。小攻撃やミスにはならない（本人: 無し）
            int hit = 0, guard = 0, dodge = 0;
            for (int i = 0; i < 20_000; i++)
            {
                var o = EngageBattle.Resolve(EngageStance.HeroCharge, EngageRole.Lose, Cfg, rng, out var sz);
                Assert.AreEqual(AttackSize.None, sz);
                if (o == EngageOutcome.Hit) hit++; else if (o == EngageOutcome.Guard) guard++; else if (o == EngageOutcome.Dodge) dodge++;
                else Assert.Fail("想定外の結果 " + o);
            }
            Assert.That(hit / 20_000.0, Is.EqualTo(0.70).Within(0.03));
            Assert.That(guard / 20_000.0, Is.EqualTo(0.15).Within(0.03));
            Assert.That(dodge / 20_000.0, Is.EqualTo(0.15).Within(0.03));
        }

        [Test]
        public void 二ターン目_攻撃確定の構えは中攻撃以上()
        {
            var rng = new SystemRandom(3);
            foreach (var role in new[] { EngageRole.Lose, EngageRole.Small })
            {
                Assert.AreEqual(EngageOutcome.Attack, EngageBattle.Resolve(EngageStance.HeroAttack, role, Cfg, rng, out var sz));
                Assert.AreEqual(AttackSize.Medium, sz);
            }
            Assert.AreEqual(EngageOutcome.Attack, EngageBattle.Resolve(EngageStance.HeroAttack, EngageRole.Rare, Cfg, rng, out var big));
            Assert.AreEqual(AttackSize.Large, big);
        }

        [Test]
        public void ダメージとルーレット()
        {
            Assert.AreEqual(Cfg.damageMedium, EngageBattle.Damage(AttackSize.Medium, Cfg));
            Assert.AreEqual(Cfg.damageLarge, EngageBattle.Damage(AttackSize.Large, Cfg));
            Assert.AreEqual(Cfg.damageCounter, EngageBattle.Damage(AttackSize.Counter, Cfg));
            Assert.AreEqual(Cfg.damageLarge + 10, EngageBattle.Damage(AttackSize.Large, Cfg, 10), "装備・技能の上乗せはダメージに足す");
            Assert.AreEqual(0, EngageBattle.Damage(AttackSize.None, Cfg));
            Assert.AreEqual(EngageOutcome.PotionHeal, EngageBattle.Roulette(EngageRole.Lose));
            Assert.AreEqual(EngageOutcome.PotionLarge, EngageBattle.Roulette(EngageRole.Small));
            Assert.AreEqual(EngageOutcome.PotionDefeat, EngageBattle.Roulette(EngageRole.Rare));
        }

        [Test]
        public void ジャッジの率は削った分と役で決まる()
        {
            Assert.AreEqual(0, EngageBattle.JudgePercent(100, 100, EngageRole.Lose, Cfg), "無傷のハズレは 0");
            Assert.AreEqual(35, EngageBattle.JudgePercent(65, 100, EngageRole.Lose, Cfg));
            Assert.AreEqual(45, EngageBattle.JudgePercent(65, 100, EngageRole.Small, Cfg));
            Assert.AreEqual(75, EngageBattle.JudgePercent(65, 100, EngageRole.Rare, Cfg));
            Assert.AreEqual(Cfg.judgeMax, EngageBattle.JudgePercent(5, 100, EngageRole.Rare, Cfg), "上限で頭打ち");
        }

        [Test]
        public void 帯の文の置き換え()
        {
            Assert.AreEqual("SET 2/3 T1 −3 +5", EngageBattle.Fill("SET {set}/{sets} T{turn} −{dmg} +{heal}", 2, 3, 1, 3, 5));
        }
    }
}
