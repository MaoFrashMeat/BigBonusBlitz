using System.Collections.Generic;
using BBB.Core;
using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>
    /// 潜行中の持ち物（装備・呪い）をセーブに載せるための橋渡し。
    /// JsonUtility は入れ子の Dictionary を扱えないので、平らな入れ物に詰め替えて 1 本の文字列にする。
    /// </summary>
    public static class RunIO
    {
        [System.Serializable]
        private sealed class EquipBlob
        {
            public List<EquipItem> bag = new List<EquipItem>();
            public List<string> wornSlots = new List<string>();
            public List<EquipItem> wornItems = new List<EquipItem>();
        }

        [System.Serializable]
        private sealed class CurseBlob
        {
            public List<CurseInstance> taken = new List<CurseInstance>();
        }

        public static string SaveEquip(EquipInventory inv)
        {
            if (inv == null) return "";
            var b = new EquipBlob();
            b.bag.AddRange(inv.Bag);
            foreach (var kv in inv.Worn)
            {
                if (kv.Value == null) continue;
                b.wornSlots.Add(kv.Key);
                b.wornItems.Add(kv.Value);
            }
            return JsonUtility.ToJson(b);
        }

        public static void LoadEquip(EquipInventory inv, string json)
        {
            if (inv == null) return;
            inv.Clear();
            if (string.IsNullOrEmpty(json)) return;
            EquipBlob b;
            try { b = JsonUtility.FromJson<EquipBlob>(json); }
            catch (System.Exception) { return; }
            if (b == null) return;
            // 昔の部位名（armor / trinket）は読み替える
            if (b.bag != null) foreach (var it in b.bag) if (it != null) { it.slot = EquipSlot.Normalize(it.slot); inv.Bag.Add(it); }
            if (b.wornSlots != null && b.wornItems != null)
                for (int i = 0; i < b.wornSlots.Count && i < b.wornItems.Count; i++)
                    if (!string.IsNullOrEmpty(b.wornSlots[i]) && b.wornItems[i] != null)
                    {
                        b.wornItems[i].slot = EquipSlot.Normalize(b.wornItems[i].slot);
                        string slot = EquipSlot.Normalize(b.wornSlots[i]);
                        if (System.Array.IndexOf(EquipSlot.All, slot) < 0) slot = inv.FreeSlotFor(b.wornItems[i].slot) ?? EquipSlot.SlotsFor(b.wornItems[i].slot)[0];
                        inv.Worn[slot] = b.wornItems[i];
                    }
        }

        [System.Serializable]
        private sealed class AchBlob
        {
            public List<string> keys = new List<string>();
            public List<long> values = new List<long>();
            public List<string> unlocked = new List<string>();
        }

        public static string SaveAchievements(AchievementState st)
        {
            if (st == null) return "";
            var b = new AchBlob();
            foreach (var kv in st.Counters) { b.keys.Add(kv.Key); b.values.Add(kv.Value); }
            b.unlocked.AddRange(st.Unlocked);
            return JsonUtility.ToJson(b);
        }

        public static void LoadAchievements(AchievementState st, string json)
        {
            if (st == null) return;
            st.Clear();
            if (string.IsNullOrEmpty(json)) return;
            AchBlob b;
            try { b = JsonUtility.FromJson<AchBlob>(json); }
            catch (System.Exception) { return; }
            if (b == null) return;
            for (int i = 0; i < b.keys.Count && i < b.values.Count; i++) if (!string.IsNullOrEmpty(b.keys[i])) st.Counters[b.keys[i]] = b.values[i];
            foreach (var id in b.unlocked) if (!string.IsNullOrEmpty(id)) st.Unlocked.Add(id);
        }

        public static string SaveCurse(CurseState st)
        {
            if (st == null) return "";
            var b = new CurseBlob();
            b.taken.AddRange(st.Taken);
            return JsonUtility.ToJson(b);
        }

        public static void LoadCurse(CurseState st, string json)
        {
            if (st == null) return;
            st.Clear();
            if (string.IsNullOrEmpty(json)) return;
            CurseBlob b;
            try { b = JsonUtility.FromJson<CurseBlob>(json); }
            catch (System.Exception) { return; }
            if (b?.taken == null) return;
            foreach (var c in b.taken) if (c != null) st.Taken.Add(c);
        }
    }
}
