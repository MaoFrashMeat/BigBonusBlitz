using System;
using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>装備の部位。</summary>
    public static class EquipSlot
    {
        public const string Weapon = "weapon";
        public const string Armor = "armor";
        public const string Trinket = "trinket";
        public static readonly string[] All = { Weapon, Armor, Trinket };

        public static string DisplayName(string slot)
        {
            switch (slot)
            {
                case Weapon: return "武器";
                case Armor: return "防具";
                case Trinket: return "装身具";
                default: return slot;
            }
        }
    }

    /// <summary>接辞 1 つ。効果のキーは ShopEffects と同じものを使う。</summary>
    [Serializable]
    public sealed class EquipAffix
    {
        public string id = "";
        /// <summary>名前に足す語（接頭辞なら前、接尾辞なら後ろ）。</summary>
        public string label = "";
        /// <summary>接尾辞か（false なら接頭辞）。</summary>
        public bool suffix;
        public string effect = "";
        /// <summary>アイテムレベル 1 あたりの値と、下限。</summary>
        public float perLevel = 1f;
        public int min = 1;
        public int max = 99;
        /// <summary>この接辞が付く部位（空ならどこでも）。</summary>
        public string slot = "";
        public int weight = 100;
    }

    /// <summary>ベースとなる品。</summary>
    [Serializable]
    public sealed class EquipBase
    {
        public string id = "";
        public string name = "";
        public string slot = EquipSlot.Weapon;
        public string icon = "sword";
        /// <summary>基礎効果（接辞なしでも付く）。</summary>
        public string effect = "";
        public float perLevel = 1f;
        public int min = 1;
        public int weight = 100;
        /// <summary>この品が出はじめる深さ。</summary>
        public int minDepth = 1;
    }

    [Serializable]
    public sealed class EquipRarity
    {
        public string id = "common";
        public string name = "並";
        public string color = "#9aa4b8";
        /// <summary>付く接辞の数。</summary>
        public int affixes;
        /// <summary>効果に掛ける倍率 %（100 で等倍）。</summary>
        public int power = 100;
        /// <summary>深さ 1 のときの重み。</summary>
        public int weight = 100;
        /// <summary>深さ 1 ごとに重みへ足す値（負なら深いほど出にくい）。</summary>
        public float weightPerDepth;
    }

    [Serializable]
    public sealed class EquipConfig
    {
        public bool enabled = true;
        /// <summary>持てる数（あふれたら拾えない）。</summary>
        public int bagSize = 12;
        /// <summary>敵を倒したときに落ちる率 %。</summary>
        public int dropRateMob = 22;
        public int dropRateBoss = 100;
        /// <summary>AT の狩猟で討伐したときの率 %。</summary>
        public int dropRateHunt = 35;
        /// <summary>宝から出る率 %。</summary>
        public int dropRateTreasure = 45;
        public List<EquipRarity> rarities = new List<EquipRarity>();
        public List<EquipBase> bases = new List<EquipBase>();
        public List<EquipAffix> affixes = new List<EquipAffix>();
    }

    /// <summary>実際に手に入った 1 個。セーブ対象。</summary>
    [Serializable]
    public sealed class EquipItem
    {
        public string baseId = "";
        public string name = "";
        public string slot = "";
        public string icon = "";
        public int rarity;          // rarities の添字
        public int level = 1;       // 深さ
        /// <summary>効果キー → 値。接辞と基礎をまとめたもの。</summary>
        public List<string> effectKeys = new List<string>();
        public List<int> effectValues = new List<int>();

        public int Of(string effect)
        {
            for (int i = 0; i < effectKeys.Count && i < effectValues.Count; i++)
                if (effectKeys[i] == effect) return effectValues[i];
            return 0;
        }

        public int Power
        {
            get { int n = 0; foreach (var v in effectValues) n += v; return n; }
        }
    }

    /// <summary>持ち物と装備中。ランごとに作り直す。</summary>
    public sealed class EquipInventory
    {
        /// <summary>部位 → 装備中の品。</summary>
        public readonly Dictionary<string, EquipItem> Worn = new Dictionary<string, EquipItem>();
        /// <summary>鞄。</summary>
        public readonly List<EquipItem> Bag = new List<EquipItem>();

        public EquipItem WornOf(string slot) => Worn.TryGetValue(slot, out var v) ? v : null;

        public void Clear() { Worn.Clear(); Bag.Clear(); }

        /// <summary>装備中の合計。</summary>
        public int EffectTotal(string effect)
        {
            int n = 0;
            foreach (var kv in Worn) if (kv.Value != null) n += kv.Value.Of(effect);
            return n;
        }
    }

    public static class EquipDirector
    {
        /// <summary>深さ depth の品を 1 つ作る。設定が空なら null。</summary>
        public static EquipItem Roll(EquipConfig cfg, int depth, IRandom rng)
        {
            if (cfg == null || !cfg.enabled || cfg.bases == null || cfg.bases.Count == 0) return null;
            depth = Math.Max(1, depth);

            // ベース（その深さで出るものから重みで）
            var pool = new List<EquipBase>();
            foreach (var b in cfg.bases) if (b != null && b.minDepth <= depth) pool.Add(b);
            if (pool.Count == 0) pool.AddRange(cfg.bases);
            var base_ = PickWeighted(pool, b => b.weight, rng);
            if (base_ == null) return null;

            // レア度（深いほど上位が出やすい）
            int ri = 0;
            if (cfg.rarities != null && cfg.rarities.Count > 0)
            {
                var weights = new int[cfg.rarities.Count];
                int total = 0;
                for (int i = 0; i < cfg.rarities.Count; i++)
                {
                    var w = (int)Math.Round(cfg.rarities[i].weight + cfg.rarities[i].weightPerDepth * (depth - 1));
                    weights[i] = Math.Max(0, w);
                    total += weights[i];
                }
                if (total > 0)
                {
                    int r = rng.Next(total);
                    for (int i = 0; i < weights.Length; i++)
                    {
                        if (r < weights[i]) { ri = i; break; }
                        r -= weights[i];
                    }
                }
            }
            var rarity = cfg.rarities != null && ri < cfg.rarities.Count ? cfg.rarities[ri] : new EquipRarity();

            var item = new EquipItem
            {
                baseId = base_.id, slot = base_.slot, icon = base_.icon,
                rarity = ri, level = depth,
            };

            // 基礎効果
            if (!string.IsNullOrEmpty(base_.effect))
                Add(item, base_.effect, Scale(base_.perLevel, depth, base_.min, 9999, rarity.power));

            // 接辞
            string prefix = "", suffix = "";
            int want = Math.Max(0, rarity.affixes);
            if (want > 0 && cfg.affixes != null && cfg.affixes.Count > 0)
            {
                var avail = new List<EquipAffix>();
                foreach (var a in cfg.affixes)
                    if (a != null && (string.IsNullOrEmpty(a.slot) || a.slot == base_.slot)) avail.Add(a);
                for (int i = 0; i < want && avail.Count > 0; i++)
                {
                    var a = PickWeighted(avail, x => x.weight, rng);
                    if (a == null) break;
                    avail.Remove(a);
                    Add(item, a.effect, Scale(a.perLevel, depth, a.min, a.max, rarity.power));
                    if (a.suffix) { if (suffix.Length == 0) suffix = a.label; }
                    else if (prefix.Length == 0) prefix = a.label;
                }
            }
            item.name = (prefix.Length > 0 ? prefix : "") + base_.name + (suffix.Length > 0 ? suffix : "");
            return item;
        }

        private static int Scale(float perLevel, int depth, int min, int max, int power)
        {
            int v = (int)Math.Round(perLevel * depth * Math.Max(1, power) / 100.0);
            return Math.Max(min, Math.Min(max <= 0 ? 9999 : max, v));
        }

        private static void Add(EquipItem it, string effect, int value)
        {
            if (string.IsNullOrEmpty(effect) || value == 0) return;
            for (int i = 0; i < it.effectKeys.Count; i++)
                if (it.effectKeys[i] == effect) { it.effectValues[i] += value; return; }
            it.effectKeys.Add(effect);
            it.effectValues.Add(value);
        }

        private static T PickWeighted<T>(List<T> list, Func<T, int> weight, IRandom rng) where T : class
        {
            if (list == null || list.Count == 0) return null;
            int total = 0;
            foreach (var x in list) total += Math.Max(0, weight(x));
            if (total <= 0) return list[0];
            int r = rng.Next(total);
            foreach (var x in list)
            {
                int w = Math.Max(0, weight(x));
                if (r < w) return x;
                r -= w;
            }
            return list[list.Count - 1];
        }

        /// <summary>拾う。鞄がいっぱいなら false。</summary>
        public static bool PickUp(EquipConfig cfg, EquipInventory inv, EquipItem item)
        {
            if (inv == null || item == null) return false;
            int cap = Math.Max(1, cfg?.bagSize ?? 12);
            if (inv.Bag.Count < cap) { inv.Bag.Add(item); return true; }
            // いっぱいなら、鞄の中で一番弱いものと比べる。弱ければ入れ替える
            int worst = -1, worstPower = int.MaxValue;
            for (int i = 0; i < inv.Bag.Count; i++)
                if (inv.Bag[i] != null && inv.Bag[i].Power < worstPower) { worstPower = inv.Bag[i].Power; worst = i; }
            if (worst < 0 || worstPower >= item.Power) return false;
            inv.Bag[worst] = item;
            return true;
        }

        /// <summary>身に着ける。外れた品は鞄へ戻す。</summary>
        public static void Equip(EquipInventory inv, EquipItem item)
        {
            if (inv == null || item == null || string.IsNullOrEmpty(item.slot)) return;
            var old = inv.WornOf(item.slot);
            inv.Bag.Remove(item);
            inv.Worn[item.slot] = item;
            if (old != null) inv.Bag.Add(old);
        }

        public static void Unequip(EquipInventory inv, string slot)
        {
            if (inv == null) return;
            var old = inv.WornOf(slot);
            if (old == null) return;
            inv.Worn.Remove(slot);
            inv.Bag.Add(old);
        }

        /// <summary>捨てる。</summary>
        public static void Drop(EquipInventory inv, EquipItem item) => inv?.Bag.Remove(item);

        /// <summary>今より強ければ自動で着る（拾った直後の判断を省く）。</summary>
        public static bool AutoEquipIfBetter(EquipInventory inv, EquipItem item)
        {
            if (inv == null || item == null) return false;
            var cur = inv.WornOf(item.slot);
            if (cur != null && cur.Power >= item.Power) return false;
            Equip(inv, item);
            return true;
        }

        public static EquipRarity RarityOf(EquipConfig cfg, EquipItem item)
        {
            if (cfg?.rarities == null || item == null) return new EquipRarity();
            return item.rarity >= 0 && item.rarity < cfg.rarities.Count ? cfg.rarities[item.rarity] : new EquipRarity();
        }

        /// <summary>効果の表示名。</summary>
        public static string EffectName(string effect)
        {
            switch (effect)
            {
                case ShopEffects.DefeatBonus: return "討伐率";
                case ShopEffects.AtStartPercent: return "AT 期待度";
                case ShopEffects.AtInitialSpins: return "AT 初期G";
                case ShopEffects.BattleDamage: return "狩猟ダメージ";
                case ShopEffects.SoulGain: return "ソウル";
                case ShopEffects.ExpGain: return "EXP";
                case ShopEffects.TorchSpins: return "回復薬の効き";
                default: return effect;
            }
        }

        public static string EffectUnit(string effect)
        {
            switch (effect)
            {
                case ShopEffects.AtInitialSpins:
                case ShopEffects.TorchSpins: return "G";
                default: return "%";
            }
        }
    }
}
