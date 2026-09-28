// 板の絵（擬音など）。通常の半透明。_Color で色とフェード、_Flash で白く飛ばす
Shader "Lab/Sprite"
{
    Properties { _MainTex ("Tex", 2D) = "white" {} _Color ("Color", Color) = (1,1,1,1) _Flash ("Flash", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest Always Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _Color; float _Flash;
            struct appdata { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }
            float4 frag(v2f i):SV_Target
            {
                float4 t = tex2D(_MainTex, i.uv);
                float3 c = lerp(t.rgb * _Color.rgb, float3(1, 1, 1), _Flash);
                return float4(c, t.a * _Color.a);
            }
            ENDCG
        }
    }
}
