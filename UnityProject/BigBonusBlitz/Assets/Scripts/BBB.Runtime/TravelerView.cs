using System.Collections;
using BBB.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// 旅人（仮ビジュアル）: 体・頭・荷物を Image で組んだシルエットが右→左へ「よいしょよいしょ」と通り過ぎる。
    /// セリフがあれば中央付近で吹き出しを出す。本番のスプライトに差し替えるときは Build() だけ置き換える。
    /// </summary>
    public sealed class TravelerView : MonoBehaviour
    {
        private RectTransform _root, _body, _head, _pack, _bubble;
        private Text _bubbleText, _nameText;
        private float _areaW, _groundY;
        private SpriteAnimator _anim;      // スプライトがある場合
        private bool _useSprite;

        public static TravelerView Spawn(RectTransform area, TravelerDef def, string serif, bool isModeHint, float areaW, float groundY)
        {
            var go = new GameObject("Traveler_" + def.id, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(area, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(80, 110);
            var v = go.AddComponent<TravelerView>();
            v._root = rt; v._areaW = areaW; v._groundY = groundY;
            v.Build(def);
            v.StartCoroutine(v.Walk(def, serif, isModeHint));
            return v;
        }

        private static RectTransform Img(Transform parent, string name, Vector2 pos, Vector2 size, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = c; img.raycastTarget = false;
            return rt;
        }

        private void Build(TravelerDef def)
        {
            // スプライト（Resources/Art/Travelers/traveler_{id}.png, 4フレーム横並び）があればそれを使う
            var frames = ArtLoader.Strip("Art/Travelers/traveler_" + def.id, 4);
            if (frames.Length == 4)
            {
                _useSprite = true;
                var go = new GameObject("Sprite", typeof(RectTransform), typeof(Image));
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(_root, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, -8);
                rt.sizeDelta = new Vector2(84, 112);           // 48x64 の 1.75 倍
                var img = go.GetComponent<Image>();
                img.raycastTarget = false; img.preserveAspect = true;
                _anim = go.AddComponent<SpriteAnimator>();
                _anim.Play(frames, 0.55f, true);               // 4コマ 0.55秒 ≒ 歩調 3.6歩/秒
                _body = rt; _head = rt; _pack = rt;            // 揺れ処理の対象を差し替え
                Img(_root, "Shadow", new Vector2(0, -62), new Vector2(56, 10), new Color(0, 0, 0, 0.35f)).SetAsFirstSibling();
                _nameText = UiFactory.Label(_root, "Name", new Vector2(0, -74), new Vector2(120, 16), def.name, 11, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.8f));
                _bubble = Img(_root, "Bubble", new Vector2(0, 92), new Vector2(200, 34), new Color(1, 1, 1, 0.95f));
                _bubbleText = UiFactory.Label(_bubble, "T", Vector2.zero, new Vector2(192, 34), "", 13, TextAnchor.MiddleCenter, new Color(0.1f, 0.1f, 0.15f));
                _bubble.gameObject.SetActive(false);
                return;
            }
            ColorUtility.TryParseHtmlString(def.color, out var col);
            var dark = col * 0.55f; dark.a = 1f;
            // 影
            Img(_root, "Shadow", new Vector2(0, -52), new Vector2(60, 10), new Color(0, 0, 0, 0.35f));
            _body = Img(_root, "Body", new Vector2(0, -10), new Vector2(44, 64), col);
            _pack = Img(_body, "Pack", new Vector2(22, 4), new Vector2(26, 40), dark);
            _head = Img(_body, "Head", new Vector2(0, 46), new Vector2(34, 34), new Color(0.95f, 0.85f, 0.7f));
            Img(_head, "Hat", new Vector2(0, 16), new Vector2(44, 12), dark);
            Img(_body, "LegL", new Vector2(-10, -40), new Vector2(12, 22), dark);
            Img(_body, "LegR", new Vector2(10, -40), new Vector2(12, 22), dark);
            _nameText = UiFactory.Label(_root, "Name", new Vector2(0, -68), new Vector2(120, 16), def.name, 11, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.8f));
            // 吹き出し
            _bubble = Img(_root, "Bubble", new Vector2(0, 88), new Vector2(200, 34), new Color(1, 1, 1, 0.95f));
            _bubbleText = UiFactory.Label(_bubble, "T", Vector2.zero, new Vector2(192, 34), "", 13, TextAnchor.MiddleCenter, new Color(0.1f, 0.1f, 0.15f));
            _bubble.gameObject.SetActive(false);
        }

        private IEnumerator Walk(TravelerDef def, string serif, bool isModeHint)
        {
            float x0 = _areaW * 0.5f + 60f, x1 = -_areaW * 0.5f - 60f;
            float dur = Mathf.Max(2f, def.walkSeconds);
            float t = 0;
            bool said = false;
            float stepHz = 2.2f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = t / dur;
                float x = Mathf.Lerp(x0, x1, u);
                // よいしょよいしょ: 上下バウンド＋体の前後揺れ＋荷物の遅れ
                float ph = t * stepHz * Mathf.PI * 2f;
                float bob = Mathf.Abs(Mathf.Sin(ph)) * 6f;
                _root.anchoredPosition = new Vector2(x, _groundY + (_useSprite ? bob * 0.5f : bob));
                if (_useSprite)
                {
                    _body.localRotation = Quaternion.Euler(0, 0, 3f * Mathf.Sin(ph));
                }
                else
                {
                    _body.localRotation = Quaternion.Euler(0, 0, 6f * Mathf.Sin(ph));
                    _pack.anchoredPosition = new Vector2(22, 4 - 3f * Mathf.Sin(ph + 1f));
                    _head.localRotation = Quaternion.Euler(0, 0, -4f * Mathf.Sin(ph));
                }
                // 中央付近でセリフ
                if (!said && serif != null && u > 0.38f)
                {
                    said = true;
                    _bubble.gameObject.SetActive(true);
                    _bubbleText.text = serif;
                    _bubbleText.color = isModeHint ? new Color(0.6f, 0.1f, 0.15f) : new Color(0.1f, 0.1f, 0.15f);
                    _bubble.GetComponent<Image>().color = isModeHint ? new Color(1f, 0.95f, 0.75f, 0.97f) : new Color(1, 1, 1, 0.95f);
                    StartCoroutine(HideBubble(2.6f));
                }
                yield return null;
            }
            Destroy(gameObject);
        }

        private IEnumerator HideBubble(float sec)
        {
            yield return new WaitForSeconds(sec);
            if (_bubble != null) _bubble.gameObject.SetActive(false);
        }
    }
}
