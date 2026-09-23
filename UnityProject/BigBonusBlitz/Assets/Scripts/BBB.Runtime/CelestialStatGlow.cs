using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>Static feathered wash over an opaque illustrated card; no update loop or texture allocation.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CelestialStatGlow : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            const int columns = 12, rows = 16;
            var rect = rectTransform.rect;
            for (int y = 0; y <= rows; y++)
            for (int x = 0; x <= columns; x++)
            {
                float u = x / (float)columns, v = y / (float)rows;
                float fade = Mathf.SmoothStep(0, 1, Mathf.Min(u, 1-u) * 10)
                    * Mathf.SmoothStep(0, 1, Mathf.Min(v, 1-v) * 10);
                var c = color; c.a *= fade * .85f;
                mesh.AddVert(new Vector3(rect.xMin + rect.width*u, rect.yMin + rect.height*v), c, new Vector2(u,v));
            }
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                int a = y*(columns+1)+x, b = a+columns+1;
                mesh.AddTriangle(a,b,a+1); mesh.AddTriangle(a+1,b,b+1);
            }
        }
    }
}
