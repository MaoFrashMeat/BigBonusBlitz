// 守護の影（ガーディアン）: キャラの絵のシルエットを光で描く（加算）
// 縁が一番明るい（アルファの差で輪郭を取る）/ 中はうっすら＋上へ流れるノイズ / 横の走査線 / 下から現れる（_Appear。境目が光る）
Shader "Lab/Ghost"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _NoiseTex ("Noise", 2D) = "gray" {}
        _Col ("Col", Color) = (1,0.8,0.3,1)
        _Rim ("Rim", Color) = (2.4,2,1.2,1)
        _Appear ("Appear", Float) = 1
        _Intensity ("Intensity", Float) = 1
        _Edge ("Edge px", Float) = 5
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
            sampler2D _MainTex, _NoiseTex; float4 _MainTex_TexelSize, _Col, _Rim; float _Appear, _Intensity, _Edge, _LabT;
            struct appdata { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }
            float4 frag(v2f i):SV_Target
            {
                float a = tex2D(_MainTex, i.uv).a;
                float mn = a;
                [unroll] for (int k = 0; k < 8; k++)
                {
                    float an = k * 0.785398;
                    mn = min(mn, tex2D(_MainTex, i.uv + float2(cos(an), sin(an)) * _MainTex_TexelSize.xy * _Edge).a);
                }
                float rim = saturate(a - mn);
                float n = tex2D(_NoiseTex, float2(i.uv.x * 2.5, i.uv.y * 1.5 - _LabT * 0.6)).r;
                float scan = 0.75 + 0.25 * sin(i.uv.y * 260 - _LabT * 12);
                float body = a * (0.28 + 0.4 * smoothstep(0.45, 0.8, n)) * scan;
                // 下から現れる: _Appear の高さより上は見えない。境目が明るく光る
                float h = i.uv.y - _Appear;
                float show = 1 - smoothstep(-0.01, 0.01, h);
                float front = exp(-abs(h) * 60) * a * 2;
                float3 c = _Col.rgb * body + _Rim.rgb * rim + _Rim.rgb * front;
                return float4(c * show * _Intensity + _Rim.rgb * front * (1 - show) * _Intensity, 0);
            }
            ENDCG
        }
    }
}
