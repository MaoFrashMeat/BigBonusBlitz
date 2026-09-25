// メッシュ粒子のコイン。ライトは使わず、法線から金属の映り込みと鏡面を作る
Shader "Lab/Coin"
{
    Properties { _MainTex ("Face", 2D) = "white" {} _Gold ("Gold", Color) = (1,0.62,0.16,1) }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _Gold;
            struct appdata { float4 pos:POSITION; float3 n:NORMAL; float4 col:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float3 n:TEXCOORD1; float4 col:COLOR; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.n=UnityObjectToWorldNormal(i.n); o.col=i.col; o.uv=i.uv; return o; }
            float4 frag(v2f i, float face:VFACE):SV_Target
            {
                float3 N = normalize(i.n) * (face > 0 ? 1 : -1);
                float3 V = normalize(UNITY_MATRIX_V[2].xyz);
                if (dot(N, V) < 0) N = -N;
                float3 L = normalize(float3(-0.45, 0.75, -0.5));
                float3 R = reflect(-V, N);
                float env = lerp(0.18, 1.5, saturate(R.y * 0.6 + 0.5));
                env += pow(saturate(dot(R, L)), 30) * 6;
                float dif = saturate(dot(N, L)) * 0.5 + 0.35;
                float3 face01 = tex2D(_MainTex, i.uv).rgb;
                float3 c = _Gold.rgb * face01 * (dif + env * 0.8);
                c += pow(saturate(dot(R, L)), 90) * 10 * float3(1, 0.95, 0.85);
                return float4(c * i.col.rgb, 1);
            }
            ENDCG
        }
    }
}
