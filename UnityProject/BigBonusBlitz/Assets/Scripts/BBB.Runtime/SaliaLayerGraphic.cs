using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>A static, cropped UI grid. Motion happens in the shader, not Canvas rebuilds.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SaliaLayerGraphic : MaskableGraphic
    {
        private Texture2D _texture;
        private SaliaTitleModel.Layer _part;
        private int _width, _height;
        public override Texture mainTexture => _texture != null ? _texture : Texture2D.whiteTexture;

        public void Initialize(Texture2D texture, Material mat, SaliaTitleModel.Layer part, int width, int height)
        {
            _texture = texture; _part = part; _width = width; _height = height;
            material = mat; raycastTarget = false; SetAllDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_part == null) return;
            var r = GetPixelAdjustedRect();
            material.SetVector("_RectSize", new Vector4(r.width, r.height, 0, 0));
            // Crop a single global lattice; independently spaced grids split at curved boundaries.
            const int gridX = 140, gridY = 80;
            int startX = Mathf.FloorToInt((float)_part.x / _width * gridX);
            int startY = Mathf.FloorToInt((float)_part.y / _height * gridY);
            int cols = Mathf.CeilToInt((float)(_part.x + _part.width) / _width * gridX) - startX;
            int rows = Mathf.CeilToInt((float)(_part.y + _part.height) / _height * gridY) - startY;
            var v = UIVertex.simpleVert; v.color = color;
            for (int y = 0; y <= rows; y++)
            {
                float top = (float)(startY + y) / gridY;
                float ty = (top * _height - _part.y) / _part.height;
                for (int x = 0; x <= cols; x++)
                {
                    float u = (float)(startX + x) / gridX;
                    float tx = (u * _width - _part.x) / _part.width;
                    v.position = new Vector3(r.xMin + u * r.width, r.yMax - top * r.height);
                    v.uv0 = new Vector2(u, 1 - top);
                    v.uv1 = new Vector2(tx, 1 - ty);
                    vh.AddVert(v);
                }
            }
            for (int y = 0; y < rows; y++) for (int x = 0; x < cols; x++)
            {
                int a = y * (cols + 1) + x;
                vh.AddTriangle(a, a + cols + 1, a + 1);
                vh.AddTriangle(a + 1, a + cols + 1, a + cols + 2);
            }
        }
    }
}
