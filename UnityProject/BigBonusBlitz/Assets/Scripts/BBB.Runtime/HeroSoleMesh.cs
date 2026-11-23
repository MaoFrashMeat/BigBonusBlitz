using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    // The body is a single painted cel. Advance only its planted boot with the
    // scrolling surface during the cel hold, leaving the upper body and grips rigid.
    public sealed class HeroSoleMesh : BaseMeshEffect
    {
        float soleX,soleY,travel;
        public void SetTravel(float x,float y,float distance)
        {
            if(Mathf.Approximately(distance,travel)&&Mathf.Approximately(x,soleX)&&Mathf.Approximately(y,soleY))return;
            soleX=x;soleY=y;travel=distance;
            graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vertices)
        {
            if(!IsActive()||Mathf.Approximately(travel,0)||vertices.currentVertCount!=4)return;
            UIVertex bottom=default,top=default;
            vertices.PopulateUIVertex(ref bottom,0);
            vertices.PopulateUIVertex(ref top,2);
            vertices.Clear();
            const int columns=16,rows=8;
            for(int row=0;row<=rows;row++)
            {
                float y=row/(float)rows;
                float legWeight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(soleY+.04f,soleY+.33f,y));
                for(int column=0;column<=columns;column++)
                {
                    float x=column/(float)columns;
                    float bootWeight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.09f,.24f,Mathf.Abs(x-soleX)));
                    var vertex=bottom;
                    vertex.position=new Vector3(Mathf.Lerp(bottom.position.x,top.position.x,x)+travel*legWeight*bootWeight,
                        Mathf.Lerp(bottom.position.y,top.position.y,y),bottom.position.z);
                    vertex.uv0=new Vector2(Mathf.Lerp(bottom.uv0.x,top.uv0.x,x),Mathf.Lerp(bottom.uv0.y,top.uv0.y,y));
                    vertices.AddVert(vertex);
                    if(column==0||row==0)continue;
                    int i=row*(columns+1)+column;
                    vertices.AddTriangle(i-columns-2,i-1,i);
                    vertices.AddTriangle(i,i-columns-1,i-columns-2);
                }
            }
        }
    }
}
