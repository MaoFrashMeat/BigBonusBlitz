using System;
using System.Collections.Generic;
using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>1 回の潜行（街を出てから戻るまで）の記録。</summary>
    [Serializable]
    public sealed class RunRecord
    {
        /// <summary>記録した時刻（Unix 秒）。表示には日付だけ使う。</summary>
        public long when;
        public int spins;
        /// <summary>エンバーの増減（潜行の開始時から見た差）。</summary>
        public int diff;
        public int maxDiff, minDiff;
        public int chapter = 1;
        /// <summary>最後にいたステージの名前。</summary>
        public string stage = "";
        /// <summary>終わりかた: "hp" = ライフ切れ / "credit" = エンバー切れ / "clear" = 章クリア。</summary>
        public string reason = "";
        /// <summary>間引いた波形。グラフに重ねて描くのに使う。</summary>
        public int[] wave = new int[0];
    }

    /// <summary>
    /// 直近の潜行を数回ぶん覚えておく。
    /// セーブ本体（SaveData）とは別のキーに置く。構造が深く、
    /// 本体は別の作業でよく書き換わるので、混ぜないほうが壊しにくい。
    /// </summary>
    public static class RunHistory
    {
        private const string Key = "bbb_runs_v1";
        /// <summary>覚えておく回数。古いものから捨てる。</summary>
        public const int Keep = 5;
        /// <summary>1 回ぶんの波形の点数。これ以上は間引く。</summary>
        public const int WavePoints = 120;

        [Serializable]
        private sealed class Blob { public List<RunRecord> runs = new List<RunRecord>(); }

        /// <summary>新しい順（先頭が直近）で返す。</summary>
        public static List<RunRecord> Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new List<RunRecord>();
            try
            {
                var b = JsonUtility.FromJson<Blob>(json);
                return b?.runs ?? new List<RunRecord>();
            }
            catch (Exception e)
            {
                Debug.LogWarning("冒険履歴が読めなかったので捨てる: " + e.Message);
                return new List<RunRecord>();
            }
        }

        /// <summary>1 回ぶん足す。古いものは Keep 件まで残す。</summary>
        public static void Add(RunRecord rec)
        {
            if (rec == null) return;
            var runs = Load();
            runs.Insert(0, rec);
            if (runs.Count > Keep) runs.RemoveRange(Keep, runs.Count - Keep);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(new Blob { runs = runs }));
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }

        /// <summary>一覧に出す 1 行。「3 章 D-4  1,240G  +8,300  ライフ切れ」の形。</summary>
        public static string Label(RunRecord r, string hpName)
        {
            if (r == null) return "";
            string end;
            switch (r.reason)
            {
                case "clear": end = "章クリア"; break;
                case "hp": end = (hpName ?? "ライフ") + "切れ"; break;
                case "credit": end = "エンバー切れ"; break;
                default: end = "帰還"; break;
            }
            string stage = string.IsNullOrEmpty(r.stage) ? "" : " " + r.stage;
            return $"{r.chapter} 章{stage}   {r.spins:N0}G   {r.diff:+#,##0;-#,##0;0}   {end}";
        }
    }
}
