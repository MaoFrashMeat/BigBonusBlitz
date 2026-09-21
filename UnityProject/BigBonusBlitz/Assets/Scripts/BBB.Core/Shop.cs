using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>敵を倒したときに手に入るソウルの量。</summary>
    public sealed class SoulConfig
    {
        /// <summary>通常時の雑魚を討伐したとき。</summary>
        public int perMob = 10;
        /// <summary>ボーナス中の中ボスを討伐したとき。</summary>
        public int perBoss = 60;
        /// <summary>AT（洞窟）でモンスターを討伐したとき。</summary>
        public int perAtBattle = 25;
        /// <summary>討伐に失敗・逃げられたときの慰め。</summary>
        public int perEscape = 2;
    }

    /// <summary>
    /// ショップの品。スキルも道具も同じ仕組みで、買うとレベルが上がり効果量が増える。
    /// effect に何を強化するかのキーを書く（下の ShopEffects 参照）。
    /// </summary>
    public sealed class ShopItem
    {
        /// <summary>アイコンの種類（sword/soul/eye/book/lantern/amulet/oil/potion/boots/shield）。空なら kind から選ぶ。</summary>
        public string icon = "";
        public string id = "item";
        public string name = "名もなき品";
        /// <summary>1行の説明。{v} は現在のレベルでの効果量に置き換わる。</summary>
        public string desc = "";
        /// <summary>"skill" か "gear"。表示の色分けだけに使う。</summary>
        public string kind = "skill";
        public string effect = ShopEffects.DefeatBonus;
        /// <summary>1レベルあたりの効果量。</summary>
        public int valuePerLevel = 1;
        public int maxLevel = 5;
        public int baseCost = 100;
        /// <summary>レベルが1上がるごとに増える値段。</summary>
        public int costGrowth = 80;
    }

    /// <summary>効果キー。ここにない文字列は無視される（設定ミスで落とさない）。</summary>
    public static class ShopEffects
    {
        /// <summary>エンゲージの討伐率に +% する。</summary>
        public const string DefeatBonus = "defeatBonus";
        /// <summary>ボーナス開始時の AT 期待度に +% する。</summary>
        public const string AtStartPercent = "atStartPercent";
        /// <summary>AT の初期G数に +G する。</summary>
        public const string AtInitialSpins = "atInitialSpins";
        /// <summary>AT のバトルで与えるダメージに +% する。</summary>
        public const string BattleDamage = "battleDamage";
        /// <summary>手に入るソウルに +% する。</summary>
        public const string SoulGain = "soulGain";
        /// <summary>手に入る EXP に +% する。</summary>
        public const string ExpGain = "expGain";
        /// <summary>ステータス（ライフ / テクニック / ラック）に + する。装備だけが持つ。</summary>
        public const string StatLife = "statLife";
        public const string StatTechnique = "statTechnique";
        public const string StatLuck = "statLuck";
        /// <summary>回復薬 1 個で回復するライフに + する。</summary>
        public const string TorchSpins = "torchSpins";
        // ---- ターン制エンゲージ（2026-09-21。工房の装備が主に持つ）
        /// <summary>エンゲージの攻撃ダメージ +n（全部の大きさ）。</summary>
        public const string EngageDamage = "engageDamage";
        /// <summary>大攻撃だけ +n。</summary>
        public const string EngageLargeDamage = "engageLargeDamage";
        /// <summary>カウンターだけ +n。</summary>
        public const string EngageCounterDamage = "engageCounterDamage";
        /// <summary>被弾の LIFE 減少 −n（最低 1）。</summary>
        public const string LifeDamageCut = "lifeDamageCut";
        /// <summary>「力を貯める」のハズレで防御 / 回避になる重みに +n。</summary>
        public const string ChargeGuardRate = "chargeGuardRate";
        public const string ChargeDodgeRate = "chargeDodgeRate";
        /// <summary>ジャッジの率 +n%。</summary>
        public const string JudgeBonus = "judgeBonus";
        /// <summary>ルーレットの回復 +n。</summary>
        public const string PotionHeal = "potionHeal";
        /// <summary>ルーレットの小役で討伐確定になる率 %。</summary>
        public const string PotionDefeatSmall = "potionDefeatSmall";
        /// <summary>エンゲージのセット数 +n。</summary>
        public const string EngageSets = "engageSets";
        /// <summary>宝箱の当選率 +n（率に足す）。</summary>
        public const string TreasureRate = "treasureRate";
        /// <summary>リプレイの回復 +nG。</summary>
        public const string ReplayHeal = "replayHeal";
    }

    public sealed class ShopConfig
    {
        public List<ShopItem> items = new List<ShopItem>();
    }

    /// <summary>プレイヤーの財布と持ち物。セーブ対象。</summary>
    public sealed class PlayerWallet
    {
        public int Souls;
        /// <summary>累計で稼いだソウル（実績表示用）。</summary>
        public int TotalSouls;
        /// <summary>エンバー: ライフ・回復薬・満腹度・装備など、自分の状態を整える通貨。</summary>
        public int Embers;
        public int TotalEmbers;
        /// <summary>品ID → 所持レベル。</summary>
        public readonly Dictionary<string, int> Owned = new Dictionary<string, int>();
        /// <summary>素材 id → 個数（工房で使う。恒久）。</summary>
        public readonly Dictionary<string, int> Materials = new Dictionary<string, int>();

        public int LevelOf(string id) => id != null && Owned.TryGetValue(id, out var l) ? l : 0;
        public int MaterialCount(string id) => id != null && Materials.TryGetValue(id, out var n) ? n : 0;
        public void AddMaterial(string id, int delta)
        {
            if (string.IsNullOrEmpty(id)) return;
            int n = System.Math.Max(0, MaterialCount(id) + delta);
            if (n == 0) Materials.Remove(id); else Materials[id] = n;
        }

        public void Clear() { Souls = 0; TotalSouls = 0; Embers = 0; TotalEmbers = 0; Owned.Clear(); Materials.Clear(); }
    }

    public static class ShopDirector
    {
        /// <summary>次に買うときの値段。上限まで買っていれば -1。</summary>
        public static int NextCost(ShopItem item, int currentLevel)
        {
            if (item == null) return -1;
            if (currentLevel >= System.Math.Max(1, item.maxLevel)) return -1;
            return System.Math.Max(0, item.baseCost + item.costGrowth * currentLevel);
        }

        /// <summary>買えるなら買って true。ソウル不足・上限なら false。</summary>
        public static bool Buy(PlayerWallet wallet, ShopItem item)
        {
            if (wallet == null || item == null) return false;
            int lv = wallet.LevelOf(item.id);
            int cost = NextCost(item, lv);
            if (cost < 0 || wallet.Souls < cost) return false;
            wallet.Souls -= cost;
            wallet.Owned[item.id] = lv + 1;
            return true;
        }

        /// <summary>その効果キーの合計値（所持レベル × 1レベルあたりの効果量）。</summary>
        public static int EffectTotal(ShopConfig cfg, PlayerWallet wallet, string effect)
        {
            if (cfg?.items == null || wallet == null || effect == null) return 0;
            int sum = 0;
            foreach (var it in cfg.items)
            {
                if (it == null || it.effect != effect) continue;
                sum += it.valuePerLevel * wallet.LevelOf(it.id);
            }
            return sum;
        }

        /// <summary>説明文の {v} を、そのレベルでの効果量に置き換える。</summary>
        public static string Describe(ShopItem item, int level)
        {
            if (item == null) return "";
            int v = item.valuePerLevel * System.Math.Max(0, level);
            int next = item.valuePerLevel * (System.Math.Max(0, level) + 1);
            return (item.desc ?? "").Replace("{v}", v.ToString()).Replace("{next}", next.ToString());
        }
    }
}
