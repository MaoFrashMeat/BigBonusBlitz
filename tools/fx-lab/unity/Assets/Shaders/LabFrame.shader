// 画面の枠のエフェクト（ステップアップ）。画面全体の板に描く。枠は角丸の四角（継ぎ目なし）
// v = 画面の縁からの深さ / 太さ（0 外縁 … 1 内側）、u = 角丸の四角を一周する長さ（_UScale 倍して素材が一周でぴったり繰り返す）
// 1 パス目: 枠の下に暗い下地（乗算。光る物を立てる。docs/FX_RESEARCH.md 3）  2 パス目: 光（加算）
// _Mode 0 = 光の流れ / 1 = 雷（20fps で形が変わる）/ 2 = 炎（外縁から内へ舐める 3 段塗り）。_Rainbow 1 で色相を回す
Shader "Lab/Frame"
{
    Properties
    {
        _NoiseTex ("Noise", 2D) = "gray" {}
        _FiberTex ("Fibers", 2D) = "black" {}
        _Mode ("Mode", Float) = 0
        _Col ("Col", Color) = (0.3,0.6,1,1)
        _Core ("Core", Color) = (2,2,2,1)
        _Edge ("Edge", Color) = (0.3,0,0,1)
        _Intensity ("Intensity", Float) = 1
        _Shade ("Shade", Float) = 0.6
        _Thick ("Thick", Float) = 1.2
        _Radius ("Radius", Float) = 1.2
        _Half ("Half", Vector) = (6.4,3.6,0,0)
        _UScale ("UScale", Float) = 0.1
        _Burst ("Burst", Float) = 0
        _Rainbow ("Rainbow", Float) = 0
        _Seed ("Seed", Float) = 0
        _R0 ("R0", Color) = (0.12,0.01,0,1)
        _R1 ("R1", Color) = (0.9,0.16,0.01,1)
        _R2 ("R2", Color) = (1,0.55,0.08,1)
        _R3 ("R3", Color) = (2.3,1.55,0.6,1)
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _NoiseTex, _FiberTex;
    float _Mode, _Intensity, _Shade, _Thick, _Radius, _UScale, _Burst, _Rainbow, _Seed, _LabT;
    float4 _Col, _Core, _Edge, _Half, _R0, _R1, _R2, _R3;
    struct appdata { float4 pos:POSITION; };
    struct v2f { float4 pos:SV_POSITION; float2 wp:TEXCOORD0; };
    v2f vert(appdata i){ v2f o; o.pos = UnityObjectToClipPos(i.pos); o.wp = mul(unity_ObjectToWorld, i.pos).xy; return o; }
    // 角丸の四角: 縁からの深さ（内向き +）と、一周の長さ
    float2 frameUV(float2 p)
    {
        float R = _Radius; float2 b = _Half.xy - R;
        float2 q = abs(p) - b;
        float sd = length(max(q, 0)) + min(max(q.x, q.y), 0) - R;
        float L1 = 2 * b.x, L2 = 2 * b.y, A = R * 1.5707963;
        float s;
        if (q.x > 0 && q.y > 0)
        {
            if (p.x > 0 && p.y < 0) s = L1 + R * (atan2(p.y + b.y, p.x - b.x) + 1.5707963);
            else if (p.x > 0) s = L1 + A + L2 + R * atan2(p.y - b.y, p.x - b.x);
            else if (p.y > 0) s = 2 * L1 + 2 * A + L2 + R * (atan2(p.y - b.y, p.x + b.x) - 1.5707963);
            else s = 2 * L1 + 3 * A + 2 * L2 + R * (atan2(p.y + b.y, p.x + b.x) + 3.1415927);
        }
        else if (q.x > q.y)
            s = p.x > 0 ? L1 + A + (p.y + b.y) : 2 * L1 + 3 * A + L2 + (b.y - p.y);
        else
            s = p.y < 0 ? (p.x + b.x) : L1 + 2 * A + L2 + (b.x - p.x);
        return float2(s * _UScale, -sd / _Thick);   // 角丸の外（画面の四隅）は負
    }
    float hash(float x) { return frac(sin(x * 91.345) * 47453.13); }
    float vnoise(float x) { float i = floor(x), f = frac(x); f = f * f * (3 - 2 * f); return lerp(hash(i), hash(i + 1), f); }
    float3 hue(float h) { return saturate(abs(frac(h + float3(0, 2.0 / 3, 1.0 / 3)) * 6 - 3) - 1); }
    ENDCG
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
        ZWrite Off ZTest Always Cull Off
        // 暗い下地
        Pass
        {
            Blend DstColor Zero
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(v2f i):SV_Target
            {
                float v = frameUV(i.wp).y;
                float k = _Shade * saturate(_Intensity) * pow(saturate(1 - v * 0.85), 1.6);
                k = lerp(k, 0.85 * saturate(_Intensity), saturate(-v * 3));   // 四隅は暗い縁取り
                return float4((1 - k).xxx, 1);
            }
            ENDCG
        }
        // 光
        Pass
        {
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(v2f i):SV_Target
            {
                float2 fu = frameUV(i.wp);
                float u = fu.x, vr = fu.y, v = max(vr, 0);
                if (v > 1.05) discard;
                float corner = smoothstep(-0.35, 0, vr);   // 四隅は光らせない
                float t = _LabT;
                float3 base = _Col.rgb, core = _Core.rgb;
                if (_Rainbow > 0) { float3 h = hue(frac(u * 0.25 - t * 0.45)); base = h; core = lerp(h, 1, 0.55) * 2.4; }
                float3 c = 0;
                if (_Mode < 0.5)
                {
                    // 光の流れ: 繊維の筋が枠に沿って速く流れる。外縁ほど濃く、明るい筋だけ芯
                    float f1 = tex2D(_FiberTex, float2(u - t * 1.1 + _Seed, 0.25 + v * 0.7)).r;
                    float f2 = tex2D(_FiberTex, float2(u * 0.5 - t * 0.7 + _Seed * 2, 0.15 + v * 0.8)).g;
                    float s = max(f1, f2 * 0.85) * pow(saturate(1 - v), 1.8);
                    c = base * smoothstep(0.15, 0.5, s) * 0.7 + core * smoothstep(0.6, 0.8, s);
                }
                else if (_Mode < 1.5)
                {
                    // 雷: 20fps で形が変わる稲妻 3 本。芯は細い HDR、周りに細い光。外縁に弱い光
                    float st = floor(t * 20);
                    float acc = 0, glow = 0;
                    [unroll] for (int k = 0; k < 3; k++)
                    {
                        float sd = st * 1.37 + k * 17.1 + _Seed;
                        float on = step(0.3, hash(sd * 3.1));
                        float x = u * (6 + k * 2) + sd * 5.3;
                        float y = 0.2 + 0.55 * (vnoise(x) * 0.55 + vnoise(x * 2.9) * 0.3 + vnoise(x * 8.3) * 0.15);
                        float d = abs(v - y) * _Thick;   // world 単位の距離
                        acc += on * exp(-d * d * 9000);
                        glow += on * exp(-d * 28) * 0.45;
                    }
                    c = core * saturate(acc) + base * (glow + pow(saturate(1 - v), 7) * 0.6);
                }
                else if (_Mode > 3.5)
                {
                    // 枠で 1 つにつながった炎（本人 2026-09-28「並べるんじゃなくて枠で一つの炎」）
                    // ノイズを 4 段重ね、横にねじって（domain warp）舌の形にし、中心へ速く流す。温度 → 黒体の色（Lab/Fire と同じ段）
                    float wave = 0.5 + 0.5 * sin(u * 6.2831853 - t * 4.5);          // 枠を一周して走る大きなうねり
                    float reach = 1 + 0.3 * wave + 0.35 * _Burst;
                    float vv = v / reach;
                    float2 q = float2(u * 1.0 + _Seed, vv * 0.7 - t * 2.4);
                    float n = tex2D(_NoiseTex, q).r * 0.5 + tex2D(_NoiseTex, q * 2 + float2(0.13, -t * 1.1)).r * 0.25
                            + tex2D(_NoiseTex, q * 4 + float2(0.71, -t * 2.3)).r * 0.15;
                    float2 q2 = float2(u * 2 + (n - 0.45) * 0.55 + _Seed * 1.7, vv * 1.1 - t * 3.2);
                    float n2 = tex2D(_NoiseTex, q2).r * 0.55 + tex2D(_NoiseTex, q2 * 2 + float2(0.37, -t * 1.9)).r * 0.3
                             + tex2D(_NoiseTex, q2 * 4 + float2(0.19, -t * 3.7)).r * 0.15;
                    float flame = saturate((1 - vv) * 1.25 - n2 * 1.35 + 0.12);   // 縁でもノイズで濃淡が残るように
                    float T = pow(flame, 1.6);   // 本体は赤〜橙、芯だけ白熱
                    float3 r = lerp(_R0.rgb, _R1.rgb, smoothstep(0.0, 0.3, T));
                    r = lerp(r, _R2.rgb, smoothstep(0.3, 0.65, T));
                    r = lerp(r, _R3.rgb, smoothstep(0.65, 1.0, T));
                    if (_Rainbow > 0) { float lr = dot(r, float3(0.3, 0.55, 0.15)); r = lerp(lr * base * 1.6, r, smoothstep(0.75, 1.0, T)); }
                    c = r * smoothstep(0.02, 0.22, T);
                }
                else if (_Mode > 2.5)
                {
                    // 炎の床の明かり: 縁ほど明るく、炎に合わせてちらつく（炎の本体は粒子の連番が描く）
                    float fl = 0.75 + 0.25 * tex2D(_NoiseTex, float2(u * 0.7, t * 0.9)).r;
                    c = base * pow(saturate(1 - v), 3.0) * fl;
                }
                else
                {
                    // 炎: ノイズを内へ流し、外縁ほど高い値から削る。舌の形は強いノイズで出す。3 段（暗い縁 / 本体 / 芯）
                    // 内向きに細長い舌: u は細かく、v はゆっくり変わるノイズ（縦に伸びる）を内へ流す
                    float2 p = float2(u * 4.0 + _Seed, v * 0.45 - t * 1.5);
                    float n = tex2D(_NoiseTex, p).r * 0.55 + tex2D(_NoiseTex, float2(u * 9.0 + 0.37, v * 0.9 - t * 2.4)).r * 0.45;
                    n = saturate((n - 0.5) * 1.8 + 0.5);
                    float f = (1 - v) * 0.95 - n * 1.25 + 0.15;
                    float aa = max(fwidth(f), 1e-3);
                    float e1 = smoothstep(0.02, 0.02 + aa * 1.5, f), e2 = smoothstep(0.3, 0.3 + aa, f), e3 = smoothstep(0.75, 0.75 + aa, f);
                    float3 edgeCol = _Rainbow > 0 ? base * 0.3 : _Edge.rgb;
                    c = edgeCol * e1;
                    c = lerp(c, base * 0.6, e2);
                    c = lerp(c, core, e3);
                }
                c *= _Intensity * (1 + _Burst * 1.5) * corner;
                return float4(c, 0);
            }
            ENDCG
        }
    }
}
