using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// ボタンにマウスが乗ったらうっすら明るく（本人 2026-09-21）。本体と同じ形の白い Image を alpha 0 で重ね、乗っている間だけ上げる。
    /// 押せないボタンでは光らない。色掛け（ColorBlock）だと絵の枠は明るくならないので、重ねる方式にした。
    /// </summary>
    public sealed class HoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>乗っているときの明るさ（白の alpha）。</summary>
        public const float Alpha = 0.035f;   // リニア色空間なので見た目は 2〜3 倍に出る（0.07 で AUTO が灰色に見えた）
        public Image glow;
        private Selectable _sel;
        private float _target;

        private void Awake() { _sel = GetComponent<Selectable>(); }
        public void OnPointerEnter(PointerEventData e) { _target = Alpha; }
        public void OnPointerExit(PointerEventData e) { _target = 0f; }
        private void OnDisable() { _target = 0f; if (glow != null) glow.color = new Color(1, 1, 1, 0); }

        private void Update()
        {
            if (glow == null) return;
            float want = _sel != null && !_sel.interactable ? 0f : _target;
            float a = glow.color.a;
            if (Mathf.Abs(a - want) < 0.001f) return;
            a = Mathf.MoveTowards(a, want, Time.unscaledDeltaTime * 1.6f);
            glow.color = new Color(1, 1, 1, a);
        }

        /// <summary>
        /// 本体の形に合わせた白い Image を重ねて、ボタンに付ける。
        /// 本体が子の Image（UiSkin）なら、本体に Mask を付けてその子に白を敷く（絵の形どおりに光る。同じ絵を白で重ねても
        /// 絵の色がもう一度乗るだけで明るくならない）。本体がボタンの根（AtelierUi / MapUiV2）なら、縁の飾りまで隠さないよう
        /// Mask は使わず、少し内側の角丸の白を敷く。
        /// </summary>
        public static void Attach(Button btn, Image body)
        {
            if (btn == null || body == null) return;
            var rt = body.rectTransform;
            var go = new GameObject("Hover", typeof(RectTransform), typeof(Image));
            var g = (RectTransform)go.transform;
            var img = go.GetComponent<Image>();
            img.color = new Color(1, 1, 1, 0); img.raycastTarget = false;
            if (body.gameObject == btn.gameObject)
            {
                g.SetParent(rt, false);
                g.anchorMin = Vector2.zero; g.anchorMax = Vector2.one; g.pivot = new Vector2(0.5f, 0.5f);
                g.anchoredPosition = Vector2.zero; g.sizeDelta = new Vector2(-6, -6);
                g.SetAsFirstSibling();
                img.sprite = UiSkin.Rounded(Mathf.Clamp(Mathf.RoundToInt(rt.sizeDelta.y * 0.25f), 4, 14)); img.type = Image.Type.Sliced;
            }
            else
            {
                var mask = body.GetComponent<Mask>() ?? body.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = true;
                g.SetParent(rt, false);
                g.anchorMin = Vector2.zero; g.anchorMax = Vector2.one; g.pivot = new Vector2(0.5f, 0.5f);
                g.anchoredPosition = Vector2.zero; g.sizeDelta = Vector2.zero;
            }
            var h = btn.gameObject.AddComponent<HoverGlow>();
            h.glow = img;
        }
    }
}
