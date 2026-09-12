using BBB.Core;
using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>main.js saveGameState / loadGameState 相当。PlayerPrefs に JSON で保存。</summary>
    [System.Serializable]
    public sealed class SaveData
    {
        public const string Key = "bbb_save_v1";
        // 音量は「設定」でありセーブデータではない。別キーに持ち、はじめから でも消さない
        private const string KeyBgmVol = "bbb_opt_bgm_vol";
        private const string KeySeVol = "bbb_opt_se_vol";
        private const string KeyBgmOn = "bbb_opt_bgm_on";
        private const string KeyAutoSpeed = "bbb_opt_auto_speed";
        private const string KeyGraphAlways = "bbb_graph_always";

        public int credit;
        public int heldBonusFlag;
        public int currentSetting;
        public string bonusMode;
        public int bonusPayoutTarget;
        public int bonusEarned;
        public int bonusGamesPlayed, bonusGamesTotal;
        public int spinCount;
        public int totalSpinCount;
        public string mode;
        public int playerLevel;
        public int playerExp;
        // ステータス（ライフ / テクニック / ラック）
        public int statLife, statTechnique, statLuck, statUnspent;
        public float bgmVolume = 0.5f;
        public float seVolume = 0.8f;
        public bool bgmEnabled = true;
        // --- ソウルと持ち物（Dictionary は JsonUtility で保存できないので 2 本の配列に分けて持つ）---
        public int souls;
        public int totalSouls;
        public int embers;
        public int totalEmbers;
        public string[] missionIds = new string[0];
        public int[] missionProgress = new int[0];
        public string[] ownedIds = new string[0];
        public int[] ownedLevels = new int[0];
        // --- 冒険（ステージ制マップ）---
        public string advNode = "";
        public int advSpinsLeft;
        public string advNext = "";
        public string[] advVisited = new string[0];
        public int advChapter = 1;
        public int advTreasures;
        public int advStockAtSpins;
        public int advStockAtExpect;
        public int advTorches = -1;
        public int advTorchSpins;
        public string advReturnReason = "";
        // 達成条件の数えもの（Dictionary は保存できないので 2 本の配列に分ける）
        public string[] advCounterKeys = new string[0];
        public int[] advCounterValues = new int[0];
        public int advReplayChain;
        public int advDecidedPriority;
        // 潜行中の拾い物と呪い（構造が深いので JSON 文字列で持つ）
        public string runEquip = "";
        public string runCurse = "";

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
                bonusGamesPlayed = m.BonusGamesPlayed,
                bonusGamesTotal = m.BonusGamesTotal,
                spinCount = m.SpinCount,
                totalSpinCount = m.TotalSpinCount,
                mode = m.Mode.ToString(),
                playerLevel = m.PlayerLevel,
                playerExp = m.PlayerExp,
                statLife = m.Stats.Life,
                statTechnique = m.Stats.Technique,
                statLuck = m.Stats.Luck,
                statUnspent = m.Stats.Unspent,
                bgmVolume = audio != null ? audio.BgmVolume : 0.5f,
                seVolume = audio != null ? audio.SeVolume : 0.8f,
                bgmEnabled = audio == null || audio.BgmEnabled,
                souls = m.Wallet.Souls,
                totalSouls = m.Wallet.TotalSouls,
                embers = m.Wallet.Embers,
                totalEmbers = m.Wallet.TotalEmbers,
                advNode = m.Adv.nodeId ?? "",
                advSpinsLeft = m.Adv.spinsLeft,
                advNext = m.Adv.nextId ?? "",
                advVisited = m.Adv.visited.ToArray(),
                advChapter = m.Adv.chapter,
                advTreasures = m.Adv.treasuresFound,
                advStockAtSpins = m.Adv.stockAtSpins,
                advStockAtExpect = m.Adv.stockAtExpect,
                advTorches = m.Adv.torches,
                advTorchSpins = m.Adv.torchSpins,
                advReturnReason = m.Adv.returnReason ?? "",
                advReplayChain = m.Adv.replayChain,
                advDecidedPriority = m.Adv.decidedPriority,
                runEquip = RunIO.SaveEquip(m.Equip),
                runCurse = RunIO.SaveCurse(m.Curse),
            };
            var ck = new System.Collections.Generic.List<string>();
            var cv = new System.Collections.Generic.List<int>();
            foreach (var kv in m.Adv.counters) { if (kv.Value == 0) continue; ck.Add(kv.Key); cv.Add(kv.Value); }
            d.advCounterKeys = ck.ToArray();
            d.advCounterValues = cv.ToArray();
            var ids = new System.Collections.Generic.List<string>();
            var lvs = new System.Collections.Generic.List<int>();
            foreach (var kv in m.Wallet.Owned) { if (kv.Value <= 0) continue; ids.Add(kv.Key); lvs.Add(kv.Value); }
            d.ownedIds = ids.ToArray();
            d.ownedLevels = lvs.ToArray();
            var mid = new System.Collections.Generic.List<string>();
            var mpr = new System.Collections.Generic.List<int>();
            foreach (var ms in m.Missions) { mid.Add(ms.id); mpr.Add(ms.progress); }
            d.missionIds = mid.ToArray();
            d.missionProgress = mpr.ToArray();
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(d));
            SaveAudio(audio);
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
            m.BonusGamesPlayed = d.bonusGamesPlayed;
            // 古いセーブ（G 数なし）でボーナス中なら、今の設定の G 数を当てる
            m.BonusGamesTotal = d.bonusGamesTotal > 0 || m.BonusMode == BonusMode.NORMAL ? d.bonusGamesTotal : m.BonusGamesFor(m.BonusMode);
            m.SpinCount = d.spinCount;
            m.TotalSpinCount = d.totalSpinCount;
            if (System.Enum.TryParse<Mode>(d.mode, out var md)) m.Mode = md;
            m.PlayerLevel = Mathf.Max(1, d.playerLevel);
            m.PlayerExp = d.playerExp;
            int statMax = Mathf.Max(1, m.Config.stats?.maxPerStat ?? 20);
            m.Stats.Life = Mathf.Clamp(d.statLife, 0, statMax);
            m.Stats.Technique = Mathf.Clamp(d.statTechnique, 0, statMax);
            m.Stats.Luck = Mathf.Clamp(d.statLuck, 0, statMax);
            m.Stats.Unspent = Mathf.Max(0, d.statUnspent);
            m.Wallet.Souls = Mathf.Max(0, d.souls);
            m.Wallet.TotalSouls = Mathf.Max(0, d.totalSouls);
            m.Wallet.Embers = Mathf.Max(0, d.embers);
            m.Wallet.TotalEmbers = Mathf.Max(0, d.totalEmbers);
            m.Missions.Clear();
            if (d.missionIds != null && d.missionProgress != null)
                for (int i = 0; i < d.missionIds.Length && i < d.missionProgress.Length; i++)
                    if (!string.IsNullOrEmpty(d.missionIds[i]) && TechDirector.FindMission(m.Config.tech, d.missionIds[i]) != null)
                        m.Missions.Add(new MissionState { id = d.missionIds[i], progress = Mathf.Max(0, d.missionProgress[i]) });
            m.Wallet.Owned.Clear();
            if (d.ownedIds != null && d.ownedLevels != null)
                for (int i = 0; i < d.ownedIds.Length && i < d.ownedLevels.Length; i++)
                    if (!string.IsNullOrEmpty(d.ownedIds[i]) && d.ownedLevels[i] > 0) m.Wallet.Owned[d.ownedIds[i]] = d.ownedLevels[i];
            // 冒険の進行。ステージが今の設定に無ければ章の最初へ
            if (m.AdventureEnabled)
            {
                var cfg = m.Config.adventure;
                if (!string.IsNullOrEmpty(d.advNode) && cfg.Find(d.advNode) != null)
                {
                    m.Adv.nodeId = d.advNode;
                    m.Adv.spinsLeft = Mathf.Max(0, d.advSpinsLeft);
                    m.Adv.nextId = !string.IsNullOrEmpty(d.advNext) && cfg.Find(d.advNext) != null ? d.advNext : null;
                    m.Adv.visited.Clear();
                    if (d.advVisited != null) foreach (var v in d.advVisited) if (!string.IsNullOrEmpty(v) && cfg.Find(v) != null && !m.Adv.visited.Contains(v)) m.Adv.visited.Add(v);
                    if (!m.Adv.visited.Contains(m.Adv.nodeId)) m.Adv.visited.Add(m.Adv.nodeId);
                }
                else AdventureDirector.Reset(cfg, m.Adv);
                m.Adv.chapter = Mathf.Max(1, d.advChapter);
                m.Adv.treasuresFound = Mathf.Max(0, d.advTreasures);
                m.Adv.stockAtSpins = Mathf.Max(0, d.advStockAtSpins);
                m.Adv.stockAtExpect = Mathf.Max(0, d.advStockAtExpect);
                m.Adv.torches = d.advTorches;
                m.Adv.torchSpins = d.advTorchSpins;
                m.Adv.returnReason = string.IsNullOrEmpty(d.advReturnReason) ? null : d.advReturnReason;
                AdventureDirector.NormalizeTorches(cfg, m.Adv, m.TorchSpinsPerUnit);
                m.Adv.counters.Clear();
                if (d.advCounterKeys != null && d.advCounterValues != null)
                    for (int i = 0; i < d.advCounterKeys.Length && i < d.advCounterValues.Length; i++)
                        if (!string.IsNullOrEmpty(d.advCounterKeys[i])) m.Adv.counters[d.advCounterKeys[i]] = d.advCounterValues[i];
                m.Adv.replayChain = Mathf.Max(0, d.advReplayChain);
                m.Adv.decidedPriority = Mathf.Max(0, d.advDecidedPriority);
                RunIO.LoadEquip(m.Equip, d.runEquip);
                RunIO.LoadCurse(m.Curse, d.runCurse);
            }
            LoadAudio(audio);   // 音量は別キー（無ければこのセーブの値）から
            return true;
        }

        public static bool Exists() => PlayerPrefs.HasKey(Key);

        /// <summary>音量設定だけを適用する（タイトル画面など、ゲーム本体を作らない場面用）。保存が無ければ false。</summary>
        public static bool LoadAudio(AudioManager audio)
        {
            if (audio == null) return false;
            // 新しい別キーがあればそれを使う。無ければ古いセーブの中の値から拾う（移行のため）
            if (PlayerPrefs.HasKey(KeyBgmVol) || PlayerPrefs.HasKey(KeySeVol) || PlayerPrefs.HasKey(KeyBgmOn))
            {
                audio.BgmVolume = PlayerPrefs.GetFloat(KeyBgmVol, 0.5f);
                audio.SeVolume = PlayerPrefs.GetFloat(KeySeVol, 0.8f);
                bool on = PlayerPrefs.GetInt(KeyBgmOn, 1) != 0;
                if (audio.BgmEnabled != on) audio.ToggleBgm();
                return true;
            }
            if (!PlayerPrefs.HasKey(Key)) return false;
            SaveData d;
            try { d = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key)); }
            catch (System.Exception) { return false; }
            if (d == null) return false;
            audio.BgmVolume = d.bgmVolume;
            audio.SeVolume = d.seVolume;
            if (audio.BgmEnabled != d.bgmEnabled) audio.ToggleBgm();
            SaveAudio(audio);     // 次からは別キーで読む
            return true;
        }

        /// <summary>スランプを常に出すか。表示の好みなので、セーブを消しても残す。</summary>
        public static void SaveGraphAlwaysOn(bool on)
        {
            PlayerPrefs.SetInt(KeyGraphAlways, on ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>保存された「スランプを常に出す」（既定は出さない）。</summary>
        public static bool LoadGraphAlwaysOn() => PlayerPrefs.GetInt(KeyGraphAlways, 0) != 0;

        /// <summary>操作の好み（オート速度）を保存する。セーブデータとは別。</summary>
        public static void SaveOptions(int autoSpeed)
        {
            PlayerPrefs.SetInt(KeyAutoSpeed, Mathf.Clamp(autoSpeed, 1, 6));
            PlayerPrefs.Save();
        }

        /// <summary>保存されたオート速度（無ければ 1）。</summary>
        public static int LoadAutoSpeed() => Mathf.Clamp(PlayerPrefs.GetInt(KeyAutoSpeed, 1), 1, 6);

        /// <summary>音量だけを別キーに保存する（セーブデータを消しても残る）。</summary>
        public static void SaveAudio(AudioManager audio)
        {
            if (audio == null) return;
            PlayerPrefs.SetFloat(KeyBgmVol, audio.BgmVolume);
            PlayerPrefs.SetFloat(KeySeVol, audio.SeVolume);
            PlayerPrefs.SetInt(KeyBgmOn, audio.BgmEnabled ? 1 : 0);
        }

        /// <summary>セーブデータだけを消す。音量などの設定は残す。</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            RunHistory.Clear();          // 冒険履歴も進行の記録なので一緒に消す
            PlayerPrefs.Save();
        }
    }
}
