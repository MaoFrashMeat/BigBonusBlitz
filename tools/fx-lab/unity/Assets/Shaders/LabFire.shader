// 本物寄りの炎（粒子用・加算）。シミュレーションの炎の連番から「温度」を取り出し、黒体の色の段で塗り直す
// 温度 = 連番の明るさ × 濃さ。低い → _C0（暗い赤）→ _C1（赤橙）→ _C2（黄橙）→ _C3（白熱。HDR で光る）
// 頂点色: rgb = 色相の差し替え（虹のとき）、a = 寿命でのフェード
Shader "Lab/Fire"
{
    Properties
    {
        _MainTex ("Flipbook", 2D) = "black" {}
        _C0 ("C0", Color) = (0.12,0.01,0,1)
        _C1 ("C1", Color) = (0.9,0.18,0.01,1)
        _C2 ("C2", Color) = (1,0.55,0.08,1)
        _C3 ("C3", Color) = (2.6,2,1.1,1)
        _Intensity ("Intensity", Float) = 1
        _Gain ("Gain", Float) = 1.0
        _Tint ("Tint", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+5" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST;
            float4 _C0, _C1, _C2, _C3; float _Intensity, _Gain, _Tint;
            struct appdata { float4 pos:POSITION; float4 col:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float4 col:COLOR; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.col=i.col; o.uv=TRANSFORM_TEX(i.uv,_MainTex); return o; }
            float4 frag(v2f i):SV_Target
            {
                float4 t = tex2D(_MainTex, i.uv);
                // 温度: 明るさを曲線で締める（芯だけが白熱。本体は赤〜橙に残す）
                float T = pow(saturate(dot(t.rgb, float3(0.35, 0.5, 0.15)) * _Gain), 1.35) * saturate(t.a * 1.5);
                float3 c = lerp(_C0.rgb, _C1.rgb, smoothstep(0.0, 0.3, T));
                c = lerp(c, _C2.rgb, smoothstep(0.3, 0.65, T));
                c = lerp(c, _C3.rgb, smoothstep(0.65, 1.0, T));
                // 虹: 白熱の手前までを頂点色の色相に置き換える
                float l = dot(c, float3(0.3, 0.55, 0.15));
                c = lerp(c, l * i.col.rgb * 1.6, _Tint * (1 - smoothstep(0.75, 1.0, T)));
                c *= smoothstep(0.015, 0.2, T);   // 薄い所は消す（炎の輪郭）
                return float4(c * i.col.a * _Intensity, 0);
            }
            ENDCG
        }
    }
}
