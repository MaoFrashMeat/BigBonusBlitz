using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>プレハブ無しで uGUI を組むためのヘルパー。仮 UI 用。</summary>
    public static class UiFactory
    {
        private static Font _font;
        public static Font Font => _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Canvas CreateCanvas(string name = "Canvas")
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540);
            scaler.matchWidthOrHeight = 1f;   // 高さ 540 を固定し、横は端末の比率どおり伸ばす（舞台は SafeStage で縮尺）
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }

        public static RectTransform Panel(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return rt;
        }

        public static Text Label(Transform parent, string name, Vector2 anchoredPos, Vector2 size, string text,
                                 int fontSize = 20, TextAnchor align = TextAnchor.MiddleCenter, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color ?? Color.white;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>横スライダー（背景バー＋つまみ）。</summary>
        public static Slider Slider(Transform parent, string name, Vector2 anchoredPos, Vector2 size, float value, System.Action<float> onChange)
        {
            var rt = Panel(parent, name, anchoredPos, size, new Color(0, 0, 0, 0));
            rt.GetComponent<Image>().raycastTarget = true;
            var slider = rt.gameObject.AddComponent<Slider>();

            var bg = Panel(rt, "Background", Vector2.zero, new Vector2(size.x, 6), new Color(0.2f, 0.2f, 0.25f));
            bg.GetComponent<Image>().raycastTarget = false;

            var fillArea = new GameObject("FillArea", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.SetParent(rt, false);
            fillArea.anchorMin = new Vector2(0, 0.5f); fillArea.anchorMax = new Vector2(1, 0.5f);
            fillArea.sizeDelta = new Vector2(0, 6); fillArea.anchoredPosition = Vector2.zero;
            var fill = Panel(fillArea, "Fill", Vector2.zero, new Vector2(0, 6), new Color(0.3f, 0.7f, 1f));
            fill.anchorMin = new Vector2(0, 0); fill.anchorMax = new Vector2(1, 1); fill.sizeDelta = Vector2.zero;
            fill.GetComponent<Image>().raycastTarget = false;

            var handleArea = new GameObject("HandleArea", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(rt, false);
            handleArea.anchorMin = new Vector2(0, 0); handleArea.anchorMax = new Vector2(1, 1);
            handleArea.sizeDelta = new Vector2(-12, 0); handleArea.anchoredPosition = Vector2.zero;
            var handle = Panel(handleArea, "Handle", Vector2.zero, new Vector2(12, size.y), Color.white);
            handle.anchorMin = new Vector2(0, 0); handle.anchorMax = new Vector2(0, 1); handle.sizeDelta = new Vector2(12, 0);

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f;
            slider.value = value;
            slider.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return slider;
        }

        public static Button ButtonWithLabel(Transform parent, string name, Vector2 anchoredPos, Vector2 size, string text,
                                             System.Action onClick, int fontSize = 18)
        {
            var rt = Panel(parent, name, anchoredPos, size, new Color(0.25f, 0.25f, 0.3f));
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = rt.GetComponent<Image>();
            var colors = btn.colors;
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            btn.colors = colors;
            Label(rt, "Label", Vector2.zero, size, text, fontSize);
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }
    }
}
