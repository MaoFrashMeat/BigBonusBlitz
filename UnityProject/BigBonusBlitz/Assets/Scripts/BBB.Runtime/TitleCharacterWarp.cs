using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>
    /// 立ち絵を動かして「生きている」ように見せる。Live2D の代わり。
    ///
    /// 絵を格子に割って頂点をずらす（Live2D の変形と同じ考えかた）。
    /// 大事なのは **体と髪で動きを変える** こと。全身を同じ波で揺らすと
    /// 絵が波打っているようにしか見えず、生きている感じにならない。
    ///
    ///   体（Mode.Body） … 足元を軸にした呼吸だけ。横には動かさない
    ///   髪（Mode.Hair） … 横の波。毛先ほど大きく、体より遅れて揺れる
    ///
    /// 体と髪は別の絵に分けてある（tools/comfy/split_hair.py が作る）。
    /// 髪を抜いたあとの穴は塞いであるので、揺らしても下から何も出ない。
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class TitleCharacterWarp : BaseMeshEffect
    {
        public enum Mode { Body, Hair }

        /// <summary>体として動かすか、髪として動かすか。</summary>
        public Mode mode = Mode.Body;

        /// <summary>格子の細かさ。多いほど滑らかだが、毎フレーム作り直すので程々に。</summary>
        public int cols = 10, rows = 14;

        [Header("呼吸（体）")]
        public float breathAmount = 0.006f;   // 縦の伸び縮み（1 = 等倍）
        public float breathSpeed = 0.28f;

        [Header("揺れ（髪）")]
        public float swayAmount = 5.5f;       // 毛先の横のずれ（px）
        public float swaySpeed = 0.33f;
        public float swayWaves = 0.9f;        // 縦に何回うねるか

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

            bool hair = mode == Mode.Hair;
            float breath = 1f + Mathf.Sin(_t * breathSpeed * Mathf.PI * 2f)
                             * (hair ? breathAmount * 0.6f : breathAmount);

            vh.Clear();
            var vert = UIVertex.simpleVert;
            vert.color = color;
            for (int j = 0; j <= cy; j++)
            {
                float ty = (float)j / cy;                 // 0 = 下、1 = 上
                for (int i = 0; i <= cx; i++)
                {
                    float tx = (float)i / cx;             // 0 = 左、1 = 右

                    // 呼吸: 足元は動かさず、上へいくほど伸びる。体も髪も同じだけ伸びる
                    float y = yMin + h * ty * breath;

                    float x = xMin + w * tx;
                    if (hair)
                    {
                        // 髪だけ横に揺らす。毛先（下）ほど大きく、左右の端ほど大きい。
                        // 体は動かさないので、髪が遅れて付いてくるように見える
                        float edge = Mathf.Abs(tx - 0.5f) * 2f;
                        float lower = 1f - ty;
                        float amp = swayAmount * (0.35f + 0.65f * edge) * lower * lower;
                        x += Mathf.Sin((_t * swaySpeed + ty * swayWaves) * Mathf.PI * 2f) * amp;
                    }

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
