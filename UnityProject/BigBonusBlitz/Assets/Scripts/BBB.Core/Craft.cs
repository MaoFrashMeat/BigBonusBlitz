using System;
using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>素材（ダンジョンの落とし物。財布に恒久で貯まる）。</summary>
    [Serializable]
    public sealed class MaterialDef
    {
        public string id = "";
        public string name = "";
        public string icon = "oil";
    }

    /// <summary>
    /// 工房の図面（本人 2026-09-21: 案C + 案A' の混合）。作った装備は恒久で、＋ボーナス（接辞）は付かない。
    /// 拾った装備には接辞が付くことがあり、街に戻ると流れる。
    /// </summary>
    [Serializable]
    public sealed class CraftRecipe
    {
        public string id = "";
        public string name = "";
        /// <summary>部位（EquipSlot.Kinds）。</summary>
        public string slot = EquipSlot.Weapon;
        public string icon = "sword";
        /// <summary>段 I〜IV（1〜4）。枠の色と並び順に使う。</summary>
        public int tier = 1;
        /// <summary>効果キーと値（1 つ）。</summary>
        public string effect = ShopEffects.DefeatBonus;
        public int value = 1;
        /// <summary>ソウル。</summary>
        public int souls = 100;
        /// <summary>素材 id と個数（同じ添字で対）。</summary>
        public List<string> materialIds = new List<string>();
        public List<int> materialCounts = new List<int>();
        /// <summary>この章を踏破していれば作れる（0 = 最初から）。</summary>
        public int unlockChapter;
        public string desc = "";
    }

    [Serializable]
    public sealed class CraftConfig
    {
        public bool enabled = true;
        public List<MaterialDef> materials = new List<MaterialDef>();
        public List<CraftRecipe> recipes = new List<CraftRecipe>();

        public MaterialDef Material(string id) { foreach (var m in materials) if (m != null && m.id == id) return m; return null; }
        public CraftRecipe Recipe(string id) { foreach (var r in recipes) if (r != null && r.id == id) return r; return null; }
    }

    /// <summary>この G で拾った素材（GameResult.materialsGained）。</summary>
    public sealed class MaterialGain { public string id = ""; public string name = ""; public int amount; }

    public static class CraftDirector
    {
        /// <summary>作った品の baseId（図鑑・実績で拾い物と区別する）。</summary>
        public const string BasePrefix = "craft:";

        /// <summary>もう作ってあるか（鞄か装備中に同じ図面の品がある）。</summary>
        public static bool Owns(EquipInventory inv, string recipeId)
        {
            if (inv == null || string.IsNullOrEmpty(recipeId)) return false;
            foreach (var it in inv.Bag) if (it != null && it.crafted && it.craftId == recipeId) return true;
            foreach (var kv in inv.Worn) if (kv.Value != null && kv.Value.crafted && kv.Value.craftId == recipeId) return true;
            return false;
        }

        public static bool Unlocked(CraftRecipe r, int chaptersCleared) => r != null && chaptersCleared >= r.unlockChapter;

        /// <summary>足りない物の説明（空なら作れる）。</summary>
        public static string Missing(CraftConfig cfg, PlayerWallet wallet, EquipInventory inv, CraftRecipe r, int chaptersCleared)
        {
            if (r == null) return "図面が無い";
            if (!Unlocked(r, chaptersCleared)) return $"第{r.unlockChapter}章を踏破すると作れる";
            if (Owns(inv, r.id)) return "作成済み";
            if (wallet == null || wallet.Souls < r.souls) return "ソウルが足りない";
            for (int i = 0; i < r.materialIds.Count; i++)
            {
                int need = i < r.materialCounts.Count ? r.materialCounts[i] : 1;
                if (wallet.MaterialCount(r.materialIds[i]) < need) return (cfg?.Material(r.materialIds[i])?.name ?? r.materialIds[i]) + " が足りない";
            }
            return "";
        }

        public static bool CanCraft(CraftConfig cfg, PlayerWallet wallet, EquipInventory inv, CraftRecipe r, int chaptersCleared)
            => Missing(cfg, wallet, inv, r, chaptersCleared).Length == 0;

        /// <summary>図面から品を作る（接辞なし・固定値・恒久）。</summary>
        public static EquipItem MakeItem(CraftRecipe r)
        {
            var it = new EquipItem
            {
                baseId = BasePrefix + r.id, name = r.name, slot = EquipSlot.Normalize(r.slot), icon = string.IsNullOrEmpty(r.icon) ? "sword" : r.icon,
                rarity = 0, level = Math.Max(1, r.tier), crafted = true, craftId = r.id
            };
            it.effectKeys.Add(r.effect); it.effectValues.Add(r.value);
            return it;
        }

        /// <summary>作って手に入れる（ソウルと素材を払う）。空き枠があれば着ける。作れなければ null。</summary>
        public static EquipItem Craft(CraftConfig cfg, PlayerWallet wallet, EquipInventory inv, CraftRecipe r, int chaptersCleared, out bool worn)
        {
            worn = false;
            if (!CanCraft(cfg, wallet, inv, r, chaptersCleared)) return null;
            wallet.Souls -= r.souls;
            for (int i = 0; i < r.materialIds.Count; i++)
            {
                int need = i < r.materialCounts.Count ? r.materialCounts[i] : 1;
                wallet.AddMaterial(r.materialIds[i], -need);
            }
            var it = MakeItem(r);
            inv.Bag.Add(it);   // 作った品は鞄の数に入らない（EquipInventory.BagCount）
            if (inv.FreeSlotFor(it.slot) != null) { EquipDirector.Equip(inv, it); worn = true; }
            return it;
        }
    }
}
