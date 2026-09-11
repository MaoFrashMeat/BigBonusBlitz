using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>
    /// ゲーム画面の配置（Resources/Data/ui_layout.json）。
    /// tools/ui_viewer.html で枠を動かして保存すると、このファイルが書き換わり、次の Play で反映される。
    ///
    /// 座標は舞台（960x540）の中心が原点・上が +y。値はすべて「舞台上の絶対位置」で持ち、
    /// 親の中に置くものは呼び側で親の中心を引いて相対にする。
    /// ファイルが無い・キーが無いときは呼び側の既定値（コードの定数）を使うので、消しても壊れない。
    /// </summary>
    public static class UiLayout
    {
        public const string Path = "Data/ui_layout";

        [System.Serializable]
        public sealed class Rect
        {
            public float x, y, w, h;
            /// <summary>枠の絵の名前。"none" で手続き描画、空なら UiSkin の自動選択。</summary>
            public string frame;
            public Vector2 Pos => new Vector2(x, y);
            public Vector2 Size => new Vector2(w, h);
        }

        [System.Serializable]
        private sealed class File
        {
            public int version = 1;
            public Dictionary<string, Rect> elements = new Dictionary<string, Rect>();
        }

        private static Dictionary<string, Rect> _elements;

        /// <summary>Play に入るたびに読み直す（エディタで書き換えた直後の Play に効かせる）。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { _elements = null; }

        private static Dictionary<string, Rect> Elements
        {
            get
            {
                if (_elements != null) return _elements;
                _elements = new Dictionary<string, Rect>();
                var ta = Resources.Load<TextAsset>(Path);
                if (ta == null) return _elements;
                try
                {
                    var f = JsonConvert.DeserializeObject<File>(ta.text);
                    if (f?.elements != null) _elements = f.elements;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("ui_layout.json が読めない（既定の配置を使う）: " + e.Message);
                }
                return _elements;
            }
        }

        /// <summary>id の配置。無ければ既定値を返す。</summary>
        public static Rect Get(string id, float x, float y, float w, float h)
        {
            if (Elements.TryGetValue(id, out var r) && r != null) return r;
            return new Rect { x = x, y = y, w = w, h = h };
        }

        /// <summary>id の枠の指定。無ければ null（UiSkin が自動で選ぶ）。</summary>
        public static string Frame(string id)
            => Elements.TryGetValue(id, out var r) && r != null && !string.IsNullOrEmpty(r.frame) ? r.frame : null;

        public static bool Has(string id) => Elements.ContainsKey(id);
    }
}
