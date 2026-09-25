Shader "Lab/FxAlpha"
{
    Properties { _MainTex ("Tex", 2D) = "white" {} _Intensity ("Intensity", Float) = 1 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; float _Intensity;
            struct appdata { float4 pos:POSITION; float4 col:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float4 col:COLOR; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.col=i.col; o.uv=TRANSFORM_TEX(i.uv,_MainTex); return o; }
            float4 frag(v2f i):SV_Target
            {
                float4 t = tex2D(_MainTex, i.uv);
                return float4(t.rgb * i.col.rgb * _Intensity, t.a * i.col.a);
            }
            ENDCG
        }
    }
}
