// 板ポリ用の加算（頂点色を使わない）。_NoiseAmt>0 で上へ流れる揺らぎを掛ける。_UseAlphaOnly=1 でテクスチャのアルファだけ使う
Shader "Lab/QuadAdd"
{
    Properties
    {
        _MainTex ("Tex", 2D) = "white" {}
        _NoiseTex ("Noise", 2D) = "gray" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _NoiseAmt ("NoiseAmt", Float) = 0
        _NoiseTiling ("NoiseTiling", Vector) = (2,1,0,0)
        _Speed ("Speed", Float) = 0.6
        _UseAlphaOnly ("AlphaOnly", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            float _LabEmit;
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _NoiseTex; float4 _Tint, _NoiseTiling; float _NoiseAmt, _Speed, _UseAlphaOnly, _LabT;
            struct appdata { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }
            float4 frag(v2f i):SV_Target
            {
                float4 t = tex2D(_MainTex, i.uv);
                float3 base = _UseAlphaOnly > 0.5 ? t.aaa : t.rgb * t.a;
                float n = tex2D(_NoiseTex, i.uv * _NoiseTiling.xy + float2(0, -_LabT * _Speed)).r;
                float m = saturate(lerp(1, saturate(n * 1.8 - 0.2), _NoiseAmt));
                return float4(base * _Tint.rgb * m * _LabEmit, 0);
            }
            ENDCG
        }
    }
}
