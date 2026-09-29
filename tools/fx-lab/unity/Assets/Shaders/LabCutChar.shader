// カットインのキャラ: 太い縁取り（アルファを 12 方向に広げて外側だけ塗る）・白く飛ばす・透明度
Shader "Lab/CutChar"
{
    Properties
    {
        _MainTex ("Tex", 2D) = "white" {}
        _Outline ("Outline px", Float) = 6
        _OutlineCol ("OutlineCol", Color) = (0,0,0,1)
        _Flash ("Flash", Float) = 0
        _Tint ("Tint", Color) = (1,1,1,1)
        _Alpha ("Alpha", Float) = 1
        _EdgeX ("EdgeX", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent+18" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest Always Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_TexelSize, _OutlineCol, _Tint; float _Outline, _Flash, _Alpha, _EdgeX;
            struct appdata { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }
            float4 frag(v2f i):SV_Target
            {
                float4 t = tex2D(_MainTex, i.uv);
                float o = 0;
                [unroll] for (int k = 0; k < 12; k++)
                {
                    float a = k * 0.5235988;
                    o = max(o, tex2D(_MainTex, i.uv + float2(cos(a), sin(a)) * _MainTex_TexelSize.xy * _Outline).a);
                }
                float3 c = lerp(t.rgb * _Tint.rgb, float3(1, 1, 1), _Flash);
                float a = saturate(t.a * 1.2);
                float3 col = lerp(_OutlineCol.rgb, c, a);
                float alpha = max(a, smoothstep(0.3, 0.6, o) * _OutlineCol.a);
                // 絵の四辺は柔らかく消す（絵の中のエフェクトが縁で切れて四角が見えるため）
                float2 e = min(i.uv, 1 - i.uv);
                float edge = smoothstep(0.0, 0.12 * _EdgeX, e.x) * smoothstep(0.0, 0.16, e.y);   // 立ち絵は横を消さない（_EdgeX 0）、切った下だけ消える
                return float4(col, alpha * _Alpha * edge);
            }
            ENDCG
        }
    }
}
