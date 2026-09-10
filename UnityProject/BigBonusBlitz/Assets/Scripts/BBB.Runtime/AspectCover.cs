using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>
    /// 親いっぱいを覆うように、縦横比を保ったまま大きさを合わせる。
    /// はみ出した側は親の外へ出る（親に RectMask2D を付けておけば切られる）。
    ///
    /// Image.preserveAspect は「収める」なので上下か左右に余白が出る。
    /// 一枚絵の背景は余白ではなく切れてほしいので、こちらを使う。
    /// </summary>
    public sealed class AspectCover : MonoBehaviour
    {
        /// <summary>絵の横 / 縦。</summary>
        public float aspect = 16f / 9f;

        private RectTransform _rt, _parent;
        private Vector2 _lastParent = new Vector2(-1, -1);

        public static AspectCover Attach(RectTransform rt, float aspect)
        {
            var c = rt.gameObject.AddComponent<AspectCover>();
            c.aspect = aspect > 0.01f ? aspect : 16f / 9f;
            c._rt = rt;
            c._parent = rt.parent as RectTransform;
            c.Apply();
            return c;
        }

        private void Update() => Apply();

        private void Apply()
        {
            if (_rt == null || _parent == null) return;
            var p = _parent.rect.size;
            if (p.x <= 0 || p.y <= 0 || p == _lastParent) return;
            _lastParent = p;
            // 親より横長なら幅を合わせ、そうでなければ高さを合わせる
            _rt.sizeDelta = (p.x / p.y > aspect)
                ? new Vector2(p.x, p.x / aspect)
                : new Vector2(p.y * aspect, p.y);
        }
    }
}
