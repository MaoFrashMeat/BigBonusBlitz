// 三日月の斬撃。u=弧に沿う(0→1で刃が進む) v=0内側 1外側（刃先の軌跡）
// 芯・本体・暗い縁は同じ場 f を内側へ削って作る。消えるときはノイズで削る（アルファエロージョン）
Shader "Lab/Slash"
{
    Properties
    {
        _NoiseTex ("Noise", 2D) = "gray" {}
        _FiberTex ("Fibers (R,G 繊維 / B 消え)", 2D) = "white" {}
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
            float _Head, _Tail, _Fade, _Thick, _Alpha, _Seed, _LabT;
            sampler2D _FiberTex;
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
                // 繊維の斬撃（docs/FX_RESEARCH.md「斬撃」）: 形は同じ場 f、中身は繊維テクスチャ 2 枚を違う速さで流す
                float aa = max(fwidth(f) * 1.2, 1e-3);
                float fz = saturate(f);                                   // 0 = 内側 … 1 = 刃先の縁
                float dn = tex2D(_FiberTex, float2(i.uv.x * 1.3 + _Seed, fz * 0.7)).b - 0.5;
                float fv = saturate(fz + dn * 0.08);                      // 消えノイズで筋を少し揺らす
                float pan = _LabT * 0.35;                                 // 弧に貼り付いたまま、刃と逆へ少し流れる
                float f1 = tex2D(_FiberTex, float2(i.uv.x * 1.6 - pan + _Seed, fv)).r;
                float f2 = tex2D(_FiberTex, float2(i.uv.x * 1.0 - pan * 0.6 + _Seed * 1.7, fv)).g;
                float fib = saturate(max(f1, f2 * 0.85));
                // 外側は詰まった本体、内側は太い筋だけが残る（尾のかすれ）
                float solid = smoothstep(0.66, 0.66 + aa, fz);
                float thrW = lerp(0.7, 0.2, saturate(fz / 0.66));
                float mask = max(solid, smoothstep(thrW, thrW + 0.12, fib)) * smoothstep(0, aa, f);
                // 消え: 薄くせずに削る。尾と内側から先に欠ける
                float e = tex2D(_FiberTex, float2(i.uv.x * 1.5 + _Seed * 0.3, fz)).b;
                float thr = _Fade * 1.2 - 0.1 + _Fade * ((1 - s) * 0.35 + (1 - fz) * 0.3);
                mask *= smoothstep(thr, thr + 0.05, e);
                // 階調（3 段）: 刃先の縁と明るい筋 = 芯（HDR）／本体／暗い縁
                float tone = fz * 0.6 + fib * 0.5;
                float core = max(smoothstep(0.93, 0.93 + aa, fz), smoothstep(0.93, 0.98, tone) * step(0.62, fz));
                float body = smoothstep(0.42, 0.47, tone);
                float3 col = lerp(_ColOuter.rgb, _ColMid.rgb, body);
                col = lerp(col, _ColCore.rgb, core);
                float a = mask * _Alpha * lerp(_ColOuter.a, _ColMid.a, body);
                return float4(col * a * _LabEmit, a * (1 - core * 0.5));
            }
            ENDCG
        }
    }
}
