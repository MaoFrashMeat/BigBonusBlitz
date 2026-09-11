using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>
    /// タイトル画面の部品（ロゴ・TAP TO START・下のピル・右上のメニュー）の配置。
    /// Resources/Data/title_layers.json の "ui"（tools/title_viewer.html の「UI」タブで決める）。
    /// 座標は舞台（960x540）の中心が原点・上が +y。無いものはコードの既定値。
    /// </summary>
    public static class TitleUiLayout
    {
        [System.Serializable]
        public sealed class El
        {
            public float x, y, w, h;
            /// <summary>枠の絵。"none" で手続き描画、空なら呼び側の既定。</summary>
            public string frame;
            /// <summary>アイコンの名前（Resources/Art/UI/Icons）。空なら無し。</summary>
            public string icon;
            public float iconSize = 18f;
            /// <summary>アイコンの中心（枠の左端からの px）。</summary>
            public float iconX = 26f;
            public string label;
            public int font = 13;
            /// <summary>文字の中心のずらし（枠の中心からの px。アイコンぶん右へ寄せる）。</summary>
            public float labelX = 11f;
            public bool visible = true;
            public Vector2 Pos => new Vector2(x, y);
            public Vector2 Size => new Vector2(w, h);
        }

        [System.Serializable]
        private sealed class File { public Dictionary<string, El> ui; }

        private static Dictionary<string, El> _els;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { _els = null; }

        private static Dictionary<string, El> Els
        {
            get
            {
                if (_els != null) return _els;
                _els = new Dictionary<string, El>();
                var ta = Resources.Load<TextAsset>(TitleParallax.Path);
                if (ta == null) return _els;
                try { var f = JsonConvert.DeserializeObject<File>(ta.text); if (f?.ui != null) _els = f.ui; }
                catch (System.Exception e) { Debug.LogWarning("title_layers.json の ui が読めない（既定の配置を使う）: " + e.Message); }
                return _els;
            }
        }

        /// <summary>id の配置。無ければ既定値（icon / label / frame は呼び側の既定のまま）。</summary>
        public static El Get(string id, float x, float y, float w, float h, string icon = null, string label = null, string frame = null)
        {
            if (Els.TryGetValue(id, out var e) && e != null)
            {
                if (string.IsNullOrEmpty(e.icon)) e.icon = icon;
                if (string.IsNullOrEmpty(e.label)) e.label = label;
                if (string.IsNullOrEmpty(e.frame)) e.frame = frame;
                return e;
            }
            return new El { x = x, y = y, w = w, h = h, icon = icon, label = label, frame = frame };
        }
    }
}
