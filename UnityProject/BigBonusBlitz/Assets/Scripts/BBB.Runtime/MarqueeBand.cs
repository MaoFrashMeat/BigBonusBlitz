using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// 文字が左へ流れ続ける斜めの帯（Web 版の敵出現バナーと同じ表現。2026-09-13 本人「Web 版の表現方法がいい」）。
    /// 青の半透明の帯に、白い太字（黒の縁取り）の文を繰り返して流す。同じ文を 2 枚並べて、はみ出したら右へ回す。
    /// </summary>
    public sealed class MarqueeBand : MonoBehaviour
    {
        private RectTransform _a, _b;
        private float _unitW, _speed, _x;

        /// <param name="unit">繰り返す 1 文（末尾の隙間は中で足す）</param>
        /// <param name="phase01">流れの位相（0〜1）。上下の帯で変えて同じ場所に文が来ないように</param>
        public static MarqueeBand Create(Transform parent, string name, Vector2 pos, float width, float height, float angleDeg,
                                         string unit, int fontSize, Color bg, Color fg, float speed, float phase01)
        {
            var root = UiSkin.Rect(parent, name, pos, new Vector2(width, height));
            root.localRotation = Quaternion.Euler(0, 0, angleDeg);
            UiSkin.Img(root, "Bg", Vector2.zero, new Vector2(width, height), null, bg);
            // 上下の細い縁（box-shadow の代わり。帯の輪郭をはっきりさせる）
            UiSkin.Img(root, "EdgeTop", new Vector2(0, height * 0.5f - 1), new Vector2(width, 2), null, new Color(bg.r, bg.g, bg.b, 0.9f));
            UiSkin.Img(root, "EdgeBottom", new Vector2(0, -height * 0.5f + 1), new Vector2(width, 2), null, new Color(bg.r, bg.g, bg.b, 0.9f));
            // 帯自体は切り抜かない（親の表示域のマスクで切れる。回転した RectMask2D は中身ごと消えてしまう）
            var mb = root.gameObject.AddComponent<MarqueeBand>();
            mb._speed = speed;
            // 1 枚が帯より長くなるまで文を繰り返す（全角スペース 5 つで隙間）
            string content = unit + "　　　　　";
            mb._a = mb.MakeText(root, content, fontSize, fg, height);
            float one = mb._a.GetComponent<Text>().preferredWidth;
            int repeat = Mathf.Max(1, Mathf.CeilToInt(width / Mathf.Max(1f, one)) + 1);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < repeat; i++) sb.Append(content);
            mb._a.GetComponent<Text>().text = sb.ToString();
            mb._unitW = Mathf.Ceil(mb._a.GetComponent<Text>().preferredWidth);
            mb._a.sizeDelta = new Vector2(mb._unitW, height);
            mb._b = mb.MakeText(root, sb.ToString(), fontSize, fg, height);
            mb._b.sizeDelta = new Vector2(mb._unitW, height);
            mb._x = -Mathf.Repeat(phase01, 1f) * mb._unitW;
            mb.Place();
            return mb;
        }

        private RectTransform MakeText(RectTransform root, string text, int fontSize, Color fg, float height)
        {
            var t = UiFactory.Label(root, "Text", Vector2.zero, new Vector2(10, height), text, fontSize, TextAnchor.MiddleLeft, fg);
            t.fontStyle = FontStyle.Bold;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f); rt.pivot = new Vector2(0, 0.5f);
            var outline = t.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var shadow = t.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(2, -2);
            return rt;
        }

        private void Place()
        {
            _a.anchoredPosition = new Vector2(_x, 0);
            _b.anchoredPosition = new Vector2(_x + _unitW, 0);
        }

        private void Update()
        {
            if (_unitW <= 0f) return;
            _x -= _speed * Time.deltaTime;
            if (_x <= -_unitW) _x += _unitW;
            Place();
        }
    }
}
