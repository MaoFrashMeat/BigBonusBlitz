// 三日月の斬撃。u=弧に沿う(0→1で刃が進む) v=0内側 1外側（刃先の軌跡）
// 芯・本体・暗い縁は同じ場 f を内側へ削って作る。消えるときはノイズで削る（アルファエロージョン）
Shader "Lab/Slash"
{
    Properties
    {
        _NoiseTex ("Noise", 2D) = "gray" {}
        _ColOuter ("Outer", Color) = (0.3,0.5,1,1)
        _ColMid ("Mid", Color) = (0.7,0.85,1,1)
        _ColCore ("Core", Color) = (1,1,1,1)
        _Head ("Head", Float) = 1
        _Tail ("Tail", Float) = 0
        _Fade ("Fade", Float) = 0
        _Thick ("Thick", Float) = 1
        _Alpha ("Alpha", Float) = 1
        _Seed ("Seed", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One OneMinusSrcAlpha ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            float _LabEmit;
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _NoiseTex;
            float4 _ColOuter, _ColMid, _ColCore;
            float _Head, _Tail, _Fade, _Thick, _Alpha, _Seed;
            struct appdata { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }
            float4 frag(v2f i):SV_Target
            {
                float span = max(_Head - _Tail, 1e-3);
                float s = (i.uv.x - _Tail) / span;
                if (s < 0 || s > 1 || _Alpha <= 0) discard;
                // 尾から太り、刃先で鋭く尖る
                float prof = pow(saturate(s), 0.75) * pow(saturate((1 - s) / 0.14), 0.55);
                float th = max(_Thick * prof * (1 - _Fade * 0.55), 1e-4);
                float d = 1 - i.uv.y;
                float f = 1 - d / th;
                float n = tex2D(_NoiseTex, float2(i.uv.x * 5 + _Seed, i.uv.y * 1.5 + _Seed * 0.37)).r;
                f -= _Fade * (0.35 + n * 1.1);
                // 3 段（docs/FX_RESEARCH.md 2）: 刃先側の細い白芯（HDR。ここだけ光る）／飽和した本体（1 未満。光らない）／内側の暗い縁（背景から切り離す）
                float aa = max(fwidth(f) * 1.2, 1e-3);
                float rim = smoothstep(0, aa, f);
                float body = smoothstep(0.2, 0.2 + aa, f);
                float core = smoothstep(0.9, 0.9 + aa, f);
                float3 col = lerp(_ColOuter.rgb, _ColMid.rgb, body);
                col = lerp(col, _ColCore.rgb, core);
                float a = rim * _Alpha * lerp(_ColOuter.a, _ColMid.a, body);
                return float4(col * a * _LabEmit, a * (1 - core * 0.5));
            }
            ENDCG
        }
    }
}
