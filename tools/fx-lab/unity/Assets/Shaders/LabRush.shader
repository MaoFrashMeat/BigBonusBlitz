// 光の奔流と隙間の光（本人 2026-09-30「普通に作るのやめて。プロシージャルなテクスチャをしっかり選んで」「光のエフェクトが見るに耐えない」）
// 粒子と丸いぼかし絵をやめ、板 1 枚に式で描く（加算・HDR。後段のブルームで光る）
// _Mode 0 = 奔流: 極座標（角度・log 半径）の上で、筋を 3 層（太・中・細）。筋ごとに太さ・明るさ・速さ・長さをばらし、
//            頭が明るく尾が長い破線が外へ走る。角度方向に FBM でうねりを掛けて機械的な等間隔を崩す。色は RGB で半径を少しずらして縁に色収差
// _Mode 1 = 隙間の光: 縦の細い芯（白）→ 指数で落ちる色のにじみ。芯の明るさは縦方向の FBM で揺らぎ、横へ光条（アナモルフィック）が伸びる
Shader "Lab/Rush"
{
    Properties
    {
        _Mode ("Mode", Float) = 0
        _Col ("Col", Color) = (1,0.2,0.08,1)
        _Hot ("Hot", Color) = (1,0.7,0.35,1)
        _T ("Time", Float) = 0
        _Intensity ("Intensity", Float) = 1
        _Core ("Core", Float) = 1
        _Aspect ("Aspect", Float) = 1.75
        _Seed ("Seed", Float) = 0
        _Reach ("Reach", Float) = 1
        _Width ("Width", Float) = 0.02
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"
            float4 _Col, _Hot; float _Mode, _T, _Intensity, _Core, _Aspect, _Seed, _Reach, _Width;
            struct appdata { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }

            float h1(float x) { return frac(sin(x * 127.1 + _Seed * 57.3) * 43758.5453); }
            float h2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7)) + _Seed * 57.3) * 43758.5453); }
            // 勾配ノイズ（値ノイズより筋が出にくい）と FBM
            float2 g2(float2 p) { float a = h2(p) * 6.2831853; return float2(cos(a), sin(a)); }
            float gnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p); float2 u = f * f * f * (f * (f * 6 - 15) + 10);
                float a = dot(g2(i), f), b = dot(g2(i + float2(1, 0)), f - float2(1, 0));
                float c = dot(g2(i + float2(0, 1)), f - float2(0, 1)), d = dot(g2(i + 1), f - 1);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y) * 0.7 + 0.5;
            }
            float fbm(float2 p) { float s = 0, a = 0.5; for (int k = 0; k < 5; k++) { s += a * gnoise(p); p = p * 2.03 + 17.1; a *= 0.5; } return s; }

            // 1 層の筋: 角度を n 本に割り、筋ごとにばらした破線が外へ走る
            float streaks(float ang, float lr, float n, float speed, float widMul, float seed)
            {
                float x = ang * n;
                float acc = 0;
                // 隣の筋も読む（筋が太いとき・中心がずれたときに切れないように）
                for (int k = -1; k <= 1; k++)
                {
                    float cell = floor(x) + k; float id = fmod(cell + n, n) + seed * 131;
                    float ra = h1(id), rb = h1(id + 7.1), rc = h1(id + 13.7), rd = h1(id + 29.3), re = h1(id + 41.9);
                    float center = cell + 0.5 + (ra - 0.5) * 0.7;               // 等間隔を崩す
                    float w = (0.05 + 0.3 * rb * rb * rb) * widMul;              // 細いものが多く、太いものが少し
                    float d = (x - center) / w;
                    float prof = exp(-d * d);
                    float sp = speed * (0.6 + 0.9 * rc);                         // 速い筋・遅い筋
                    float len = 0.55 + 0.4 * rd;                                  // 破線の長さ（log 半径で）。長く、切れ目は短く
                    float s = frac(lr * 0.32 - _T * sp * 0.45 + re * 7.0);
                    float dash = pow(saturate(s / len), 2.2) * (1 - smoothstep(0.96 * len, len, s)) * step(s, len);   // 頭が明るく尾が長い
                    float br = 0.25 + 1.2 * pow(h1(id + 57.1), 3);                // 暗い筋が多く、明るい筋が少し
                    acc += prof * dash * br;
                }
                return acc;
            }

            float3 rushAt(float2 p, float rscale)
            {
                float r = length(p) * rscale;
                float ang = atan2(p.y, p.x) / 6.2831853 + 0.5;
                float lr = log(max(r, 1e-4));
                // 角度のうねり（FBM）で、筋の束がところどころ偏る
                float warp = (fbm(float2(ang * 6, lr * 0.5 - _T * 0.6)) - 0.5) * 0.012;
                float a = ang + warp;
                float thick = streaks(a, lr, 70, 2.2, 1.6, 1);
                float mid = streaks(a, lr, 190, 3.0, 1.0, 2);
                float thin = streaks(a, lr, 480, 3.8, 0.8, 3);
                float k = thick * 0.9 + mid * 0.8 + thin * 0.6;
                // 束の明暗（低い周波数の FBM）: 筋が一様に並ぶと安っぽい
                float bundle = smoothstep(0.3, 0.75, fbm(float2(ang * 11, _T * 0.8 + 3)));
                k *= 0.35 + 1.1 * bundle;
                // 中心近くは筋が重なり白く飛ぶ・遠くは消える（届く距離 _Reach）
                float near = smoothstep(0.02, 0.12, r);
                float far = 1 - smoothstep(0.35 * _Reach, 1.15 * _Reach, r);
                return k * near * far;
            }

            float4 frag(v2f i):SV_Target
            {
                float2 p = (i.uv - 0.5) * float2(_Aspect, 1) * 2;          // 縦が -1…1
                float3 c = 0;
                if (_Mode < 0.5)
                {
                    // 色収差: 半径を少しずつずらして RGB を別々に読む
                    float kr = rushAt(p, 1.0).r, kg = rushAt(p, 1.012).r, kb = rushAt(p, 1.026).r;
                    float3 k3 = float3(kr, kg, kb);
                    float kk = dot(k3, 0.333);
                    float r = length(p);
                    // 色の傾き: 強い所・中心ほど白、弱い所・外ほど深い色
                    float3 col = lerp(_Col.rgb, _Hot.rgb, saturate(kk * 0.8));
                    col = lerp(col, 1, saturate(kk * kk * 0.35 + (1 - smoothstep(0, 0.35, r)) * 0.6));
                    c = col * k3 * 2.2;
                    // 芯: 指数の光（丸い円盤にしない）＋星の光条（角度ノイズで長短）＋横のアナモルフィック
                    float ang = atan2(p.y, p.x);
                    float spikes = pow(saturate(fbm(float2(ang * 3.2, _T * 1.5)) * 1.3 - 0.2), 4) * exp(-r * 3.5);
                    float ana = exp(-abs(p.y) * 55) * exp(-abs(p.x) * 1.6);
                    float core = exp(-r * 9) * 3 + exp(-r * 3.2) * 0.5 + spikes * 2.5 + ana * 1.4;
                    c += lerp(_Hot.rgb, 1, 0.7) * core * _Core;
                }
                else
                {
                    // 隙間の光（縦）: 芯は細く硬い白、周りは色で指数に落ちる
                    float x = abs(p.x);
                    float flick = 0.7 + 0.6 * fbm(float2(p.y * 2.5, _T * 4));
                    float coreL = exp(-x / max(_Width, 1e-4)) * flick;
                    float glowL = exp(-x / max(_Width * 8, 1e-4)) * 0.5 + exp(-x / max(_Width * 30, 1e-4)) * 0.15;
                    // 板の縁で必ず 0 に落とす（四角い縁を見せない）
                    float ends = (1 - smoothstep(0.85, 1.0, abs(p.y))) * (1 - smoothstep(0.35 * _Aspect, 0.95 * _Aspect, x));
                    c = (lerp(_Col.rgb, 1, saturate(coreL)) * coreL * 2.5 + _Col.rgb * glowL) * ends;
                }
                return float4(c * _Intensity, 0);
            }
            ENDCG
        }
    }
}
