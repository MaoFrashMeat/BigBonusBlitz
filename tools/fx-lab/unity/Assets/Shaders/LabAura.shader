// キャラのシルエットから立ち上がる炎のオーラ（キャラの後ろに置く）と、シルエットの加算
Shader "Lab/AuraFlame"
{
    Properties
    {
        _CharTex ("Char", 2D) = "black" {}
        _NoiseTex ("Noise", 2D) = "gray" {}
        _Scale ("Scale", Float) = 1.5
        _Intensity ("Intensity", Float) = 1
        _Rise ("Rise", Float) = 0.2
        _ColCore ("Core", Color) = (1,0.9,0.6,1)
        _ColMid ("Mid", Color) = (1,0.4,0.08,1)
        _ColEdge ("Edge", Color) = (0.6,0.05,0.02,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _CharTex, _NoiseTex; float _Scale, _Intensity, _Rise, _LabT, _LabEmit;
            float4 _ColCore, _ColMid, _ColEdge;
            struct appdata { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }
            float A(float2 uv) { return (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) ? 0 : tex2D(_CharTex, uv).a; }
            float4 frag(v2f i):SV_Target
            {
                float2 cuv = (i.uv - 0.5) * _Scale + 0.5;
                float n1 = tex2D(_NoiseTex, i.uv * float2(2.5, 1.6) + float2(0, -_LabT * 0.9)).r;
                float n2 = tex2D(_NoiseTex, i.uv * float2(5.5, 3.5) + float2(0.31, -_LabT * 1.9)).r;
                float2 d = float2(n1 - 0.5, (n2 - 0.5) * 0.6) * 0.09;
                float a = 0;
                [unroll] for (int k = 0; k < 12; k++)
                {
                    float t = k / 11.0;
                    float2 o = float2(0, -t * _Rise) + d * (0.4 + t * 1.8);
                    a = max(a, A(cuv + o) * (1 - t * 0.9));
                }
                a = max(a, A(cuv + float2(0.03, 0) + d) * 0.85);
                a = max(a, A(cuv - float2(0.03, 0) + d) * 0.85);
                float f = a * (0.55 + 0.9 * n2) * (0.7 + 0.6 * n1);
                float3 col = lerp(_ColEdge.rgb, _ColMid.rgb, smoothstep(0.2, 0.55, f));
                col = lerp(col, _ColCore.rgb, smoothstep(0.75, 1.05, f));
                return float4(col * smoothstep(0.08, 0.3, f) * _Intensity * _LabEmit, 0);
            }
            ENDCG
        }
    }
}
