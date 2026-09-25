// 倒れる敵の絵（ディゾルブ）。_MapTex は C# が焼く: R = 消える順（小さいほど先に消える）/ G = 破片の番号 / B = ひびの線
// 消える前線の手前に焦げの帯（_CharCol）、前線に光る縁（_EdgeCol。HDR で光る）。破片・真っ二つは同じ絵を何枚も重ねて切り抜く
Shader "Lab/Dissolve"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _MapTex ("Map", 2D) = "black" {}
        _Cut ("Cut", Float) = 0
        _Edge ("Edge", Float) = 0.04
        _EdgeCol ("EdgeCol", Color) = (4,2,0.5,1)
        _CharW ("CharW", Float) = 0
        _CharCol ("CharCol", Color) = (0.1,0.03,0.02,1)
        _Flash ("Flash", Float) = 0
        _CellId ("CellId", Float) = -1
        _Crack ("Crack", Float) = 0
        _PlaneN ("PlaneN", Vector) = (0,1,0,0)
        _PlaneD ("PlaneD", Float) = 0
        _PlaneSide ("PlaneSide", Float) = 0
        _CutGlow ("CutGlow", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _MapTex;
            float _Cut, _Edge, _CharW, _Flash, _CellId, _Crack, _PlaneD, _PlaneSide, _CutGlow;
            float4 _EdgeCol, _CharCol, _PlaneN;
            struct appdata { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }
            float4 frag(v2f i):SV_Target
            {
                float4 t = tex2D(_MainTex, i.uv);
                clip(t.a - 0.5);
                float4 m = tex2D(_MapTex, i.uv);
                if (_CellId >= 0) clip(0.5 / 255 - abs(m.g - _CellId / 255));
                float cutEdge = 0;
                if (_PlaneSide != 0)
                {
                    float sd = dot(i.uv - 0.5, _PlaneN.xy) - _PlaneD;
                    clip(sd * _PlaneSide);
                    cutEdge = 1 - saturate(abs(sd) / 0.035);   // _PlaneN は world の法線 × 絵の大きさ（sd は world 単位）
                }
                float e = m.r - _Cut;
                clip(e);
                float on = step(0.0005, _Cut);
                float3 c = t.rgb;
                c = lerp(c, _CharCol.rgb, on * _CharCol.a * (1 - smoothstep(_Edge, _Edge + _CharW, e)));   // 焦げの帯
                c = lerp(c, float3(1, 1, 1), _Flash);
                float edge = on * (1 - smoothstep(0, _Edge, e));                                           // 光る縁
                c += _EdgeCol.rgb * (edge + m.b * _Crack + cutEdge * _CutGlow);
                return float4(c, 1);
            }
            ENDCG
        }
    }
}
