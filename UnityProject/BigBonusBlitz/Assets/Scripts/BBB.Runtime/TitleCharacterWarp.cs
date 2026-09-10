using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// 立ち絵を波打たせて「生きている」ように見せる。Live2D の代わり。
    ///
    /// 絵をレイヤーに切って動かすやりかたもあるが、それだと重なりの裏を描き足す
    /// 必要があり、動かすと輪郭が二重に見えやすい。ここでは 1 枚のまま、
    /// 板を細かい格子に割って頂点をずらす。Live2D の変形と同じ考えかたで、
    /// 素材は 1 枚で済む。
    ///
    /// 動きは 3 つを重ねている。
    ///   呼吸  … 足元を軸に、上ほど大きく縦に伸び縮みする
    ///   揺れ  … 横方向の波。下（裾）と左右の端（髪）ほど大きい
    ///   傾き  … 全体をごくわずかに左右へ
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class TitleCharacterWarp : BaseMeshEffect
    {
        /// <summary>格子の細かさ。多いほど滑らかだが、毎フレーム作り直すので程々に。</summary>
        public int cols = 10, rows = 14;

        [Header("呼吸")]
        public float breathAmount = 0.007f;   // 縦の伸び縮み（1 = 等倍）
        public float breathSpeed = 0.62f;

        [Header("揺れ")]
        public float swayAmount = 3.2f;       // 横のずれ（px）
        public float swaySpeed = 0.85f;
        public float swayWaves = 1.6f;        // 縦に何回うねるか

        [Header("傾き")]
        public float leanAmount = 1.6f;       // 上端の横ずれ（px）
        public float leanSpeed = 0.31f;

        private float _t;
        private readonly UIVertex[] _quad = new UIVertex[4];

        protected override void Awake()
        {
            base.Awake();
            // 位相をずらして、他の絵と同時に揺れないようにする
            _t = Random.value * 10f;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount < 4) return;

            // 元の板（4 頂点）から、絵の四隅と UV を取り出す
            vh.PopulateUIVertex(ref _quad[0], 0);
            vh.PopulateUIVertex(ref _quad[1], 1);
            vh.PopulateUIVertex(ref _quad[2], 2);
            vh.PopulateUIVertex(ref _quad[3], 3);
            var p0 = _quad[0].position;
            var p2 = _quad[2].position;
            float xMin = Mathf.Min(p0.x, p2.x), xMax = Mathf.Max(p0.x, p2.x);
            float yMin = Mathf.Min(p0.y, p2.y), yMax = Mathf.Max(p0.y, p2.y);
            var u0 = _quad[0].uv0;
            var u2 = _quad[2].uv0;
            float uMin = Mathf.Min(u0.x, u2.x), uMax = Mathf.Max(u0.x, u2.x);
            float vMin = Mathf.Min(u0.y, u2.y), vMax = Mathf.Max(u0.y, u2.y);
            var color = _quad[0].color;

            int cx = Mathf.Max(2, cols), cy = Mathf.Max(2, rows);
            float w = xMax - xMin, h = yMax - yMin;
            if (w <= 0f || h <= 0f) return;

            float breath = 1f + Mathf.Sin(_t * breathSpeed * Mathf.PI * 2f) * breathAmount;
            float lean = Mathf.Sin(_t * leanSpeed * Mathf.PI * 2f) * leanAmount;

            vh.Clear();
            var vert = UIVertex.simpleVert;
            vert.color = color;
            for (int j = 0; j <= cy; j++)
            {
                float ty = (float)j / cy;                 // 0 = 下、1 = 上
                for (int i = 0; i <= cx; i++)
                {
                    float tx = (float)i / cx;             // 0 = 左、1 = 右

                    // 呼吸: 足元は動かさず、上へいくほど伸びる
                    float y = yMin + h * ty * breath;

                    // 揺れ: 縦にうねる波。中央より端のほうが大きく揺れる（髪と裾）
                    float edge = Mathf.Abs(tx - 0.5f) * 2f;           // 0 = 中央, 1 = 端
                    float lower = 1f - ty;                            // 下ほど大きい
                    float amp = swayAmount * (0.25f + 0.75f * edge) * (0.3f + 0.7f * lower);
                    float x = xMin + w * tx
                            + Mathf.Sin((_t * swaySpeed + ty * swayWaves) * Mathf.PI * 2f) * amp
                            + lean * ty;                              // 傾きは上ほど効く

                    vert.position = new Vector3(x, y, 0f);
                    vert.uv0 = new Vector2(Mathf.Lerp(uMin, uMax, tx), Mathf.Lerp(vMin, vMax, ty));
                    vh.AddVert(vert);
                }
            }
            int stride = cx + 1;
            for (int j = 0; j < cy; j++)
                for (int i = 0; i < cx; i++)
                {
                    int a = j * stride + i;
                    vh.AddTriangle(a, a + 1, a + stride + 1);
                    vh.AddTriangle(a, a + stride + 1, a + stride);
                }
        }
    }
}
