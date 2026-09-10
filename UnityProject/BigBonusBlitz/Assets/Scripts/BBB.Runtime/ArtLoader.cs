using System.Collections.Generic;
using BBB.Core;
using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>Resources/Art の読み込みとストリップ画像のフレーム分割。</summary>
    public static class ArtLoader
    {
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite[]> _strips = new Dictionary<string, Sprite[]>();

        /// <summary>Play に入るたびにキャッシュを捨てる（前回の破棄済み Sprite を掴まないように）。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() { _cache.Clear(); _strips.Clear(); }

        public static Sprite Sprite(string path)
        {
            if (_cache.TryGetValue(path, out var s) && s != null) return s;   // 破棄済みなら読み直す
            s = Resources.Load<Sprite>(path);
            if (s == null)
            {
                // 取り込み設定が Sprite になっていない場合の保険
                var tex = Resources.Load<Texture2D>(path);
                if (tex != null) s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            if (s == null) Debug.LogWarning("Art not found: " + path);
            _cache[path] = s;
            return s;
        }

        public static Texture2D Texture(string path) => Resources.Load<Texture2D>(path);

        /// <summary>横並びストリップを frames 等分して Sprite 配列にする。</summary>
        public static Sprite[] Strip(string path, int frames, float pivotY = 0f)
        {
            string key = path + "#" + frames + "#" + pivotY;
            if (_strips.TryGetValue(key, out var cached) && cached.Length > 0 && cached[0] != null) return cached;
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) { Debug.LogWarning("Strip not found: " + path); return new Sprite[0]; }
            var arr = new Sprite[frames];
            float w = (float)tex.width / frames;
            for (int i = 0; i < frames; i++)
                arr[i] = UnityEngine.Sprite.Create(tex, new Rect(i * w, 0, w, tex.height), new Vector2(0.5f, pivotY), 100f);
            _strips[key] = arr;
            return arr;
        }

        public static Sprite SymbolSprite(Symbol s)
        {
            switch (s)
            {
                case Symbol.RED7: return Sprite("Art/Symbols/red7");
                case Symbol.BLUE7: return Sprite("Art/Symbols/blue7");
                case Symbol.BAR: return Sprite("Art/Symbols/bar");
                case Symbol.STAR: return Sprite("Art/Symbols/star");
                case Symbol.WATERMELON: return Sprite("Art/Symbols/watermelon");
                case Symbol.CHERRY: return Sprite("Art/Symbols/cherry");
                case Symbol.REPLAY: return Sprite("Art/Symbols/replay");
                default: return Sprite("Art/Symbols/remix");
            }
        }

        public static Sprite EnemySprite(string enemyType)
        {
            switch (enemyType)
            {
                case "slime": return Sprite("Art/Enemies/slime");
                case "bat": return Sprite("Art/Enemies/bat");
                default: return Sprite("Art/Enemies/goblin");
            }
        }
    }
}
