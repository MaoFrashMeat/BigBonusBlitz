using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>Map-specific V2 artwork. Crop transparent margins in sprite coordinates, preserving source PNGs.</summary>
    public static class MapUiV2
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => Cache.Clear();

        public static Sprite Sprite(string name)
        {
            if (Cache.TryGetValue(name, out var cached) && cached != null) return cached;
            var tex = Resources.Load<Texture2D>("Art/UI/MapV2/" + name);
            if (tex == null) return null;
            var rect = new Rect(0, 0, tex.width, tex.height);
            Vector4 border = Vector4.zero;
            if (name == "panel_navy") { rect = Crop(tex, 1254, 1254, 70, 91, 1183, 1147); border = new Vector4(200, 190, 200, 200) * (tex.width / 1254f); }
            if (name == "btn_blue") { rect = Crop(tex, 1672, 941, 2, 170, 1669, 739); border = new Vector4(330, 105, 330, 105) * (tex.width / 1672f); }
            if (name == "btn_pink") { rect = Crop(tex, 2172, 724, 2, 6, 2171, 696); border = new Vector4(410, 150, 410, 150) * (tex.width / 2172f); }
            if (name == "btn_gray") { rect = Crop(tex, 2172, 724, 11, 28, 2163, 683); border = new Vector4(410, 150, 410, 150) * (tex.width / 2172f); }
            var sprite = UnityEngine.Sprite.Create(tex, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, border);
            Cache[name] = sprite;
            return sprite;
        }
        private static Rect Crop(Texture2D t, float w, float h, float l, float top, float r, float bottom) =>
            new Rect(l / w * t.width, (h - bottom) / h * t.height, (r - l) / w * t.width, (bottom - top) / h * t.height);

        public static RectTransform Frame(Transform parent, string name, Vector2 pos, Vector2 size, string art = "panel_navy")
        {
            var rt = UiSkin.Rect(parent, name, pos, size);
            var im = rt.gameObject.AddComponent<Image>();
            im.sprite = Sprite(art); im.type = Image.Type.Sliced; im.raycastTarget = false;
            im.pixelsPerUnitMultiplier = im.sprite != null ? im.sprite.texture.width / (art == "panel_navy" ? 1254f : art == "btn_blue" ? 1672f : 2172f) * 9f : 9f;
            return rt;
        }
        public static void Icon(Transform parent, string art, Vector2 pos, float size)
        {
            var im = UiSkin.Img(parent, art, pos, Vector2.one * size, Sprite(art), Color.white);
            im.preserveAspect = true; im.raycastTarget = false;
        }
        public static Button Button(Transform parent, string name, Vector2 pos, Vector2 size, string text, Action action, bool primary = false, bool secondary = false)
        {
            var rt = Frame(parent, name, pos, size, secondary ? "btn_gray" : primary ? "btn_pink" : "btn_blue");
            var im = rt.GetComponent<Image>(); im.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>(); button.targetGraphic = im;
            var colors = button.colors; colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f); colors.pressedColor = new Color(.68f, .78f, .9f); colors.selectedColor = new Color(1.1f, 1.1f, 1.1f); colors.fadeDuration = .08f; button.colors = colors;
            button.onClick.AddListener(() => action?.Invoke());
            var label = UiFactory.Label(rt, "Label", Vector2.zero, size - new Vector2(48, 8), text, 18, TextAnchor.MiddleCenter, Color.white);
            label.fontStyle = FontStyle.Bold;
            var shadow = label.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .85f); shadow.effectDistance = new Vector2(0, -1);
            return button;
        }
    }
}
