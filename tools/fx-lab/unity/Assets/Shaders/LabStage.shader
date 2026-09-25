// 舞台（背景・キャラ）。一撃の間だけ暗く、色を抜く（docs/FX_RESEARCH.md 3）。_DimMul で主役ごとに効きを変える
Shader "Lab/Stage"
{
    Properties
    {
        _MainTex ("Tex", 2D) = "white" {}
        _Cutoff ("Cutoff", Float) = -1
        _DimMul ("DimMul", Float) = 1
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
            sampler2D _MainTex; float _Cutoff, _DimMul, _StageDim, _StageDesat;
            struct appdata { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }
            float4 frag(v2f i):SV_Target
            {
                float4 t = tex2D(_MainTex, i.uv);
                if (_Cutoff >= 0) clip(t.a - _Cutoff);
                float l = dot(t.rgb, float3(0.2126, 0.7152, 0.0722));
                float3 c = lerp(t.rgb, l.xxx, _StageDesat * _DimMul);
                c *= (1 - _StageDim * _DimMul) * lerp(1, float3(0.82, 0.9, 1.08), saturate(_StageDim * _DimMul * 1.5));   // 沈めるときは少し寒色へ
                return float4(c, 1);
            }
            ENDCG
        }
    }
}
