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
        _FireTex ("Fire flipbook 8x8", 2D) = "black" {}
        _FireRate ("FireRate", Float) = 30
        _FireTile ("FireTile", Float) = 3
        _Anime ("Anime", Float) = 0
        _Steps ("Steps", Float) = 0
        _A0 ("A0", Color) = (0.95,0.22,0.05,1)
        _A1 ("A1", Color) = (1,0.55,0.08,1)
        _A2 ("A2", Color) = (1,0.82,0.18,1)
        _A3 ("A3", Color) = (1.5,1.45,0.9,1)
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
    sampler2D _NoiseTex, _FiberTex, _FireTex; float _FireRate, _FireTile, _Anime, _Steps; float4 _A0, _A1, _A2, _A3;
    // アニメ調の炎（本人 2026-09-29 の参考画像）: 温度を 4 段にくっきり塗り分ける。暗い赤の縁 → 橙 → 黄 → 薄い黄の芯。外側に薄い光だけ残す
    // hole = 中に穴を抜くためのノイズ（0〜1）。境目は fwidth で 1px だけなめらかに
    float3 AnimeFire(float T, float hole, float3 c0, float3 c1, float3 c2, float3 c3, out float mask)
    {
        T = T * (0.7 + 0.6 * hole);
        float aa = max(fwidth(T) * 1.2, 1e-3);
        float e0 = smoothstep(0.12, 0.12 + aa, T), e1 = smoothstep(0.3, 0.3 + aa, T), e2 = smoothstep(0.52, 0.52 + aa, T), e3 = smoothstep(0.74, 0.74 + aa, T);
        float3 c = c0 * e0;
        c = lerp(c, c1, e1); c = lerp(c, c2, e2); c = lerp(c, c3, e3);
        float glow = smoothstep(0.0, 0.12, T) * (1 - e0);
        mask = e0;
        return c + c1 * glow * 0.35;
    }

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
        // 光（premultiplied: 加算の段は a = 0 で出すので加算と同じ。アニメの炎は a = 覆い で不透明に塗る）
        Pass
        {
            Blend One OneMinusSrcAlpha
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
                else if (_Mode > 5.5)
                {
                    // アニメの炎（本人 2026-09-29 の理想画像: 青い炎）。同じ形の場 F を、しきい値違いで 4 枚重ねて塗る
                    // 根元（縁）ほど明るく、先（内側）ほど暗い。形は縦に伸ばしたノイズをねじって（domain warp）うねらせ、先を尖らせる
                    if (_Steps > 0) t = floor(t * _Steps) / _Steps;   // コマ打ち
                    float wave = 0.5 + 0.5 * sin(u * 6.2831853 * 0.5 - t * 3.0);
                    float vv = v / (0.8 + 0.45 * wave + 0.45 * _Burst);
                    // 形は大きく滑らかに（一周で舌 20 本前後。細かいノイズはほとんど混ぜない）
                    float2 q = float2(u * 0.32 + _Seed, vv * 0.3 - t * 0.55);
                    float w1 = tex2D(_NoiseTex, q * 0.5).r, w2 = tex2D(_NoiseTex, q * 0.5 + float2(0.37, 0.11)).r;
                    float2 qw = q + float2(w1 - 0.5, (w2 - 0.5) * 0.35) * 0.6;          // 横へねじる（舌がうねる）
                    float n = tex2D(_NoiseTex, qw).r * 0.85 + tex2D(_NoiseTex, qw * 1.8 + float2(0.21, -t * 0.3)).r * 0.15;
                    n = saturate((n - 0.5) * 1.7 + 0.5);
                    float F = (1 - vv) * 1.35 - n * 1.15 + 0.05;
                    // 先を尖らせる: うねらせた三角波で、内側ほど舌と舌の間を削る（先が尖った舌になる）
                    float tooth = abs(frac(u * 1.9 + (w1 - 0.5) * 1.6 + t * 0.15) - 0.5) * 2;
                    F -= tooth * tooth * saturate(vv) * 0.75;
                    // ちぎれて浮く塊: 先の方でだけ、別のノイズが強い所を残す
                    float blob = tex2D(_NoiseTex, float2(u * 0.6 + 0.5, vv * 0.45 - t * 0.9)).r;
                    F = max(F, (blob - 0.72) * 2.2 * smoothstep(0.35, 0.8, vv) * (1 - smoothstep(0.9, 1.2, vv)));
                    float aa = max(fwidth(F) * 1.1, 1e-3);
                    // 段ごとに別のノイズでしきい値を揺らす（明るい芯そのものが舌の形になる。縁と平行な帯にしない）
                    float m1 = tex2D(_NoiseTex, qw * 1.3 + float2(0.53, 0.29)).r - 0.5;
                    float m2 = tex2D(_NoiseTex, qw * 1.6 + float2(0.17, 0.71)).r - 0.5;
                    float e0 = smoothstep(0.0, aa, F), e1 = smoothstep(0.22, 0.22 + aa, F + m1 * 0.35), e2 = smoothstep(0.5, 0.5 + aa, F + m2 * 0.45), e3 = smoothstep(0.85, 0.85 + aa, F + (m1 + m2) * 0.35);
                    float3 a0 = _A0.rgb, a1 = _A1.rgb, a2 = _A2.rgb, a3 = _A3.rgb;
                    if (_Rainbow > 0) { a0 = base * 0.45; a1 = base * 0.8; a2 = lerp(base, 1, 0.45); a3 = lerp(base, 1, 0.8) * 1.4; }
                    float3 ac = a0 * e0;
                    ac = lerp(ac, a1, e1); ac = lerp(ac, a2, e2); ac = lerp(ac, a3, e3);
                    ac *= lerp(1.12, 0.8, saturate(vv));                                   // 同じ段の中でも根元が明るい
                    c = ac + a1 * pow(saturate(1 - v), 5) * 0.35 * e0;                     // 根元にだけ薄い光
                    return float4(c * _Intensity * (1 + _Burst * 0.6) * corner, e0 * saturate(_Intensity) * corner);   // 不透明に塗る
                }
                else if (_Mode > 4.5)
                {
                    // 枠の炎（Blender の流体シミュレーションの連番を帯に並べる）。2 枚をずらして重ね、継ぎ目とくり返しを隠す
                    if (_Steps > 0) t = floor(t * _Steps) / _Steps;   // アニメのコマ打ち（1 枚を止める）
                    float wave = 0.5 + 0.5 * sin(u * 6.2831853 - t * 4.5);
                    float vv = v / (0.85 + 0.35 * wave + 0.4 * _Burst);
                    float T = 0;
                    [unroll] for (int L = 0; L < 2; L++)
                    {
                        float uu = u * _FireTile + L * 0.5 + _Seed;
                        float fr = floor(t * _FireRate + L * 29 + floor(uu) * 17);
                        fr = fr - 64 * floor(fr / 64);
                        float col = fr - 8 * floor(fr / 8), row = floor(fr / 8);
                        float2 tuv = float2((col + frac(uu)) / 8, 1 - (row + 1 - saturate(vv)) / 8);
                        float val = vv < 1 ? tex2D(_FireTex, tuv).r : 0;
                        T = max(T, val * (L == 0 ? 1 : 0.85));
                    }
                    T = pow(saturate(T * 1.15), 1.35);
                    if (_Anime > 0.5)
                    {
                        float hole = tex2D(_NoiseTex, float2(u * 1.7 + _Seed, vv * 0.8 - t * 1.2)).r;   // 中に穴を抜く
                        float m; float3 ac = AnimeFire(T * 1.3, saturate((hole - 0.2) * 1.6), _A0.rgb, _A1.rgb, _A2.rgb, _A3.rgb, m);
                        if (_Rainbow > 0) { float lr = dot(ac, float3(0.3, 0.55, 0.15)); ac = lerp(lr * base * 1.5, ac, smoothstep(0.7, 0.9, T)); }
                        c = ac;
                    }
                    else
                    {
                    float3 r = lerp(_R0.rgb, _R1.rgb, smoothstep(0.0, 0.3, T));
                    r = lerp(r, _R2.rgb, smoothstep(0.3, 0.65, T));
                    r = lerp(r, _R3.rgb, smoothstep(0.65, 1.0, T));
                    if (_Rainbow > 0) { float lr = dot(r, float3(0.3, 0.55, 0.15)); r = lerp(lr * base * 1.6, r, smoothstep(0.75, 1.0, T)); }
                    c = r * smoothstep(0.015, 0.2, T);
                    }
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
