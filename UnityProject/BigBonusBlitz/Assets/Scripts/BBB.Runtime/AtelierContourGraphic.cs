using UnityEngine;
using UnityEngine.UI;
namespace BBB.Runtime
{
    /// <summary>Static contour lines for the expedition map, clipped by its viewport.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AtelierContourGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var rect=rectTransform.rect;
            for(int ring=0;ring<14;ring++)
            {
                Vector2 Point(float t)
                {
                    float radius=50+ring*18;
                    return rect.center+new Vector2(25+Mathf.Cos(t)*(radius*1.65f+Mathf.Sin(t*3+ring*.16f)*18),Mathf.Sin(t)*(radius*.75f+Mathf.Cos(t*4)*12));
                }
                for(int i=0;i<128;i++)
                {
                    Vector2 a=Point(i*Mathf.PI*2/128),b=Point((i+1)*Mathf.PI*2/128);var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*.4f;int start=vh.currentVertCount;
                    vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);
                    vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
                }
            }
        }
    }
}
