using Newtonsoft.Json;
using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>
    /// 開発中の版の表示。Editor（BuildInfoWriter）が Play とビルドの前に書く Resources/Data/build_info.json を読む。
    /// 「dev 106 · 7a498f1」のようにコミット数と短いハッシュ。まだ α でもないので 1.0 のような番号は出さない。
    /// ファイルが無ければ「dev」だけ。
    /// </summary>
    public static class BuildInfo
    {
        [System.Serializable]
        private sealed class File { public int count; public string hash; public string date; }

        public static string Label
        {
            get
            {
                var ta = Resources.Load<TextAsset>("Data/build_info");
                if (ta == null) return "dev";
                try
                {
                    var f = JsonConvert.DeserializeObject<File>(ta.text);
                    if (f == null || f.count <= 0) return "dev";
                    return $"dev {f.count} · {f.hash}";
                }
                catch { return "dev"; }
            }
        }
    }
}
