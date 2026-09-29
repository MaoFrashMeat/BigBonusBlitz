// 結晶のかけら（メッシュ粒子）。面ごとに明るさを変える（平らな法線）＋縁ほど明るい＋きらりと光る面。頂点色で色と透明度
Shader "Lab/Crystal"
{
    Properties { _Color ("Color", Color) = (0.4,0.8,1,1) _Glow ("Glow", Float) = 1.8 }
    SubShader
    {
        Tags { "Queue"="Transparent+4" "RenderType"="Transparent" }
        Blend One OneMinusSrcAlpha ZWrite Off Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color; float _Glow;
            struct appdata { float4 pos:POSITION; float3 n:NORMAL; float4 col:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float3 n:TEXCOORD0; float4 col:COLOR; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.n=normalize(mul((float3x3)unity_ObjectToWorld, i.n)); o.col=i.col; return o; }
            float4 frag(v2f i):SV_Target
            {
                float3 n = normalize(i.n);
                float3 L = normalize(float3(-0.4, 0.8, -0.5));
                float diff = saturate(dot(n, L));
                float rim = pow(1 - saturate(-n.z), 2.5);                  // 横を向いた面ほど明るい（縁が光る）
                float glint = pow(saturate(dot(reflect(-L, n), float3(0, 0, -1))), 24) * 3;   // きらり
                float3 base = i.col.rgb * _Color.rgb;
                float3 c = base * (0.35 + 0.65 * diff) + base * rim * 0.9 + glint;
                c *= _Glow;
                return float4(c * i.col.a, i.col.a * 0.9);
            }
            ENDCG
        }
    }
}
