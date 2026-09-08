using BBB.Core;
using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>main.js saveGameState / loadGameState 相当。PlayerPrefs に JSON で保存。</summary>
    [System.Serializable]
    public sealed class SaveData
    {
        public const string Key = "bbb_save_v1";

        public int credit;
        public int heldBonusFlag;
        public int currentSetting;
        public string bonusMode;
        public int bonusPayoutTarget;
        public int bonusEarned;
        public int spinCount;
        public int totalSpinCount;
        public string mode;
        public int playerLevel;
        public int playerExp;
        public float bgmVolume = 0.5f;
        public float seVolume = 0.8f;
        public bool bgmEnabled = true;

        public static void Save(SlotMachine m, AudioManager audio)
        {
            var d = new SaveData
            {
                credit = m.Credit,
                heldBonusFlag = (int)m.HeldBonusFlag,
                currentSetting = m.Setting,
                bonusMode = m.BonusMode.ToString(),
                bonusPayoutTarget = m.BonusPayoutTarget,
                bonusEarned = m.BonusEarned,
                spinCount = m.SpinCount,
                totalSpinCount = m.TotalSpinCount,
                mode = m.Mode.ToString(),
                playerLevel = m.PlayerLevel,
                playerExp = m.PlayerExp,
                bgmVolume = audio != null ? audio.BgmVolume : 0.5f,
                seVolume = audio != null ? audio.SeVolume : 0.8f,
                bgmEnabled = audio == null || audio.BgmEnabled,
            };
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(d));
            PlayerPrefs.Save();
        }

        /// <summary>保存があれば m に流し込んで true。</summary>
        public static bool Load(SlotMachine m, AudioManager audio)
        {
            if (!PlayerPrefs.HasKey(Key)) return false;
            SaveData d;
            try { d = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key)); }
            catch (System.Exception e) { Debug.LogWarning("Save data parse error: " + e.Message); return false; }
            if (d == null) return false;

            m.Credit = d.credit;
            m.HeldBonusFlag = (Flag)d.heldBonusFlag;
            m.SetSetting(d.currentSetting <= 0 ? 1 : d.currentSetting);
            if (System.Enum.TryParse<BonusMode>(d.bonusMode, out var bm)) m.BonusMode = bm;
            m.BonusPayoutTarget = d.bonusPayoutTarget;
            m.BonusEarned = d.bonusEarned;
            m.SpinCount = d.spinCount;
            m.TotalSpinCount = d.totalSpinCount;
            if (System.Enum.TryParse<Mode>(d.mode, out var md)) m.Mode = md;
            m.PlayerLevel = Mathf.Max(1, d.playerLevel);
            m.PlayerExp = d.playerExp;
            if (audio != null)
            {
                audio.BgmVolume = d.bgmVolume;
                audio.SeVolume = d.seVolume;
                if (audio.BgmEnabled != d.bgmEnabled) audio.ToggleBgm();
            }
            return true;
        }

        public static void Clear() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }
    }
}
