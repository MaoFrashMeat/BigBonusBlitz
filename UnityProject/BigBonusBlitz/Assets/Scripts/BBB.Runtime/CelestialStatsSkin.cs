using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    public static class CelestialStatsSkin
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Cache.Clear();
        public static Sprite Sprite(string name)
        {
            if (Cache.TryGetValue(name, out var sprite) && sprite != null) return sprite;
            var texture = Resources.Load<Texture2D>("Art/UI/CelestialStats/" + name);
            if (texture == null) return null;
            return Cache[name] = UnityEngine.Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
        }
        public static Sprite Emblem(int index)
        {
            string key = "emblem-" + index;
            if (Cache.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            var texture = Resources.Load<Texture2D>("Art/UI/CelestialStats/emblems");
            if (texture == null) return AtelierUi.Icon(index == 0 ? "potion" : index == 1 ? "sword" : "soul");
            float side = texture.width / 3f;
            return Cache[key] = UnityEngine.Sprite.Create(texture, new Rect(index * side, 0, side, texture.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
        }
        public static void Card(RectTransform root, Color tint)
        {
            var art = UiSkin.Img(root, "OrnateFrame", Vector2.zero, root.sizeDelta, Sprite("card"), Color.white);
            art.raycastTarget = false;
            // The generated card is opaque. A feathered color wash preserves its engraved wings and gold corners.
            var field = UiSkin.Rect(root, "ColorWash", new Vector2(0, -9), new Vector2(242, 258));
            var glow = field.gameObject.AddComponent<CelestialStatGlow>();
            glow.color = tint; glow.raycastTarget = false;
        }
    }
}
