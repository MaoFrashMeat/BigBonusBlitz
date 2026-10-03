// きらめきと光芒（本人 2026-10-03 の参考動画: ロゴの周りに色とりどりの細かいきらめきが毎コマ瞬き、後ろで白金の光と光芒がゆっくり回る）。加算・HDR
// _Mode 0 = きらめき: 焼いた点の板を 3 段の大きさで読み、セルごとに別の位相で瞬く（15 Hz）。色相はセルのハッシュ（赤・緑・青・白が混ざる）。中心に近いほど密
// _Mode 2 = 回るきらめきの輪（本人 2026-10-03「後ろでサークルに動いているキラキラ。あれだけ再現できれば良い」）: 傾いた楕円の輪に沿ってきらめきが回る。
//            密な弧（彗星の頭）が一周し、手前（下）は大きく明るく、奥（上）は小さく暗い。輪は 3 本（傾き・速さ・半径が違う）。粒は色とりどりで毎コマ瞬く
// _Mode 1 = 後ろの光: 白金の放射状の光＋光芒 2 層（逆向きにゆっくり回る）＋横のアナモルフィック。_Intensity で脈動
Shader "Lab/Glitter"
{
    Properties
    {
        _Mode ("Mode", Float) = 0
        _T ("Time", Float) = 0
        _Intensity ("Intensity", Float) = 1
        _Aspect ("Aspect", Float) = 1.75
        _Center ("Center (uv)", Vector) = (0.5,0.5,0,0)
        _Col ("Col", Color) = (1,0.85,0.5,1)
        _Ring ("Ring (radius, width, tilt, squash)", Vector) = (0.6,0.08,0.3,0.45)
        _Spin ("Spin (ring, arc, blinkHz, drift)", Vector) = (0.5,0.35,15,0.1)
        _PStreak ("ProcStreak", 2D) = "gray" {}
        _PSparks ("ProcSparks", 2D) = "black" {}
        _PNoise ("ProcNoise", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One ZWrite Off ZTest Always Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"
            sampler2D _PStreak, _PSparks, _PNoise; float _Mode, _T, _Intensity, _Aspect; float4 _Center, _Col, _Ring, _Spin;
            struct appdata { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }
            float h2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float3 hsv(float h, float s, float v) { float3 k = saturate(abs(frac(h + float3(0, 0.667, 0.333)) * 6 - 3) - 1); return v * lerp(1, k, s); }
            float4 frag(v2f i):SV_Target
            {
                float2 p = (i.uv - _Center.xy) * float2(_Aspect, 1) * 2; float r = length(p);
                float3 c = 0;
                if (_Mode < 0.5)
                {
                    // 3 段のきらめき。段ごとに大きさ・密度・瞬きの速さを変える
                    float dens = exp(-r * 0.7) * 0.7 + 0.45;                                 // 中心ほど密（周りも出す）
                    [unroll] for (int L = 0; L < 3; L++)
                    {
                        float sc = (L == 0 ? 1.6 : (L == 1 ? 1.0 : 0.6)); float2 uv2 = i.uv * sc + float2(L * 0.37, L * 0.11) + float2(_T * 0.004, -_T * 0.006) * (L + 1);
                        float3 sp = tex2D(_PSparks, uv2).rgb; float dot_ = pow(saturate((L == 0 ? sp.r : sp.g) * 2.5), 1.4);   // 鋭い点だけ（細かい点の板は値が小さいので持ち上げる）
                        float2 cell = floor(uv2 * (L == 0 ? 40 : (L == 1 ? 16 : 6)) + 0.5);
                        float hc = h2(cell + L * 9.1);
                        float blink = step(0.3 + 0.15 * L, h2(cell + floor(_T * (15 - L * 4) + hc * 9)));   // 毎コマ違う所が光る（密に）
                        float3 col = lerp(hsv(hc, 0.85, 1), 1, step(0.75, h2(cell + 3.3)));                // 1/4 は白
                        c += col * dot_ * blink * (1.2 - 0.3 * L) * dens * (0.5 + h2(cell + 5.5));
                    }
                    c *= 9.0;
                }
                else if (_Mode < 2.5 && _Mode > 1.5)
                {
                    // 回るきらめきの輪 × 3。粒は点の板（G = 中・B = 大）を極座標で読む。粒の間は暗いまま（帯の光はごく薄く・単色）
                    [unroll] for (int K = 0; K < 3; K++)
                    {
                        float R = _Ring.x * (K == 0 ? 1.0 : (K == 1 ? 0.78 : 1.25)), W = _Ring.y * (K == 2 ? 1.6 : 1.0);
                        float tilt = _Ring.z + (K - 1) * 0.18, sq = _Ring.w * (K == 1 ? 0.8 : 1.0);
                        float spin = _Spin.x * (K == 0 ? 1.0 : (K == 1 ? -0.7 : 0.55));
                        float ct = cos(tilt), st = sin(tilt); float2 q = float2(p.x * ct + p.y * st, -p.x * st + p.y * ct); q.y /= sq;
                        float rr = length(q); float ang = atan2(q.y, q.x);
                        float depth = 0.5 - 0.5 * sin(ang);                                 // 1 = 手前（下）、0 = 奥（上）
                        float wN = tex2D(_PNoise, float2(ang / 6.2831853 * 2 + K * 0.3, 0.5 + K * 0.2)).g;
                        float band = exp(-pow((rr - R) / (W * (0.7 + 1.0 * wN)), 2));
                        float bandSoft = exp(-pow((rr - R) / (W * 2.2), 2));
                        float arcA = ang - _T * _Spin.y * (K == 1 ? -1 : 1) - K * 2.1;
                        float head = pow(0.5 + 0.5 * cos(arcA), 5) * 1.4 + pow(0.5 + 0.5 * cos(arcA + 0.6), 2) * 0.6 + 0.45;
                        // 粒 2 段。手前ほど大きい粒（読みの倍率を depth で変える）。角度方向は 2.5 タイル
                        float zs = lerp(1.7, 1.0, depth);
                        float2 tuv = float2(ang / 6.2831853 * 14.0 + _T * spin * 0.45, rr * 2.3 - _T * _Spin.w + K * 0.37) * zs;   // 1 タイル ≒ 角度 0.78 × 半径 0.77（縦横比を揃える。ずれると粒が筋に伸びる）
                        float3 sA = tex2D(_PSparks, tuv).rgb, sB = tex2D(_PSparks, tuv * 1.8 + 0.31).rgb;
                        float dots = pow(saturate(sA.g * 2.6), 2.0) * 1.3 + pow(saturate(sA.b * 1.8), 2.5) * 2.0 * depth + pow(saturate(sB.g * 2.6), 2.0) * 1.0 + pow(saturate(sB.r * 3.0), 2.0) * 0.6;
                        // 瞬き（セルごと・毎コマ）と色（セルごと。1/4 は白）
                        float2 cell = floor(tuv * float2(40, 12) + 0.5); float hc = h2(cell + K * 7.7);
                        float blink = 0.3 + 0.7 * step(0.5, h2(cell + floor(_T * _Spin.z + hc * 9)));
                        float3 col = lerp(hsv(hc, 1.0, 1), 1, step(0.72, h2(cell + 3.3)));
                        c += col * dots * band * head * blink * (0.35 + 0.95 * depth) * (K == 2 ? 0.6 : 1.0);
                        c += _Col.rgb * bandSoft * head * 0.003 * (0.3 + depth);            // 粒の間を埋めるごく薄い光（単色）
                    }
                    c *= 7.5;
                }
                else
                {
                    // 後ろの光: 放射状の白金 + 光芒 2 層（逆回転）+ アナモルフィック
                    float ang = atan2(p.y, p.x); float lr = log(max(r, 1e-4));
                    float3 s1 = tex2D(_PStreak, float2(lr * 0.25, ang / 6.2831853 * 2.0 + _T * 0.02)).rgb;
                    float3 s2 = tex2D(_PStreak, float2(lr * 0.4 + 0.3, ang / 6.2831853 * 3.0 - _T * 0.013)).rgb;
                    float rays = (pow(s1.r, 3) * 0.9 + pow(s1.b, 3) * 1.1 + pow(s2.g, 3) * 0.8) * exp(-r * 2.4) * smoothstep(0.02, 0.12, r);
                    float glow = exp(-r * r * 11) * 2.2 + exp(-r * r * 2.5) * 0.9 + exp(-r * 3.5) * 0.2;
                    float ana = exp(-abs(p.y) * 30) * exp(-abs(p.x) * 1.2) * 0.6;
                    float3 gold = _Col.rgb; float3 white = float3(1, 0.97, 0.9);
                    c = lerp(gold, white, saturate(glow)) * glow + gold * rays * 1.4 + white * ana;
                }
                return float4(c * _Intensity, 0);
            }
            ENDCG
        }
    }
}
