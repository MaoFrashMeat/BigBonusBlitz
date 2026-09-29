// カットインの帯（本人 2026-09-29「カットインの演出のエフェクト色々」。参考: 斜め帯の流線 / 電撃と矢印）
// uv.x = 帯に沿う（左 0 → 右 1）、uv.y = 帯の幅（下 0 → 上 1）。_Open = 帯の開き（0 閉じ → 1 全開、上下から開く）
// _Mode 0 = 流線（線ごとに長さ・太さ・速さ・明るさを変える）/ 1 = 電撃（流線＋走る稲妻）/ 2 = 炎（上下の縁から帯の中へ燃えるアニメの炎）/ 3 = 矢印（＞＞＞ が流れる）/ 4 = 虹の流線
// 帯の縁は白い線＋外に黒い線（アニメのカットインの枠）
Shader "Lab/CutBand"
{
    Properties
    {
        _NoiseTex ("Noise", 2D) = "gray" {}
        _Mode ("Mode", Float) = 0
        _Base ("Base", Color) = (0.2,0.05,0.02,1)
        _C1 ("C1", Color) = (1,0.45,0.05,1)
        _C2 ("C2", Color) = (1,0.85,0.3,1)
        _C3 ("C3", Color) = (2,1.8,1.2,1)
        _Open ("Open", Float) = 1
        _Speed ("Speed", Float) = 3
        _Aspect ("Aspect", Float) = 8
        _Alpha ("Alpha", Float) = 1
        _Seed ("Seed", Float) = 0
        _Edge ("Edge", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent+15" "RenderType"="Transparent" }
        Blend One OneMinusSrcAlpha ZWrite Off ZTest Always Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _NoiseTex; float4 _Base, _C1, _C2, _C3;
            float _Mode, _Open, _Speed, _Aspect, _Alpha, _Seed, _Edge, _LabT;
            struct appdata { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }
            float hash(float x) { return frac(sin(x * 127.1 + _Seed * 311.7) * 43758.5453); }
            float3 hue(float h) { return saturate(abs(frac(h + float3(0, 2.0 / 3, 1.0 / 3)) * 6 - 3) - 1); }

            // 流線: 帯を細い段に割り、段ごとに線を 1 本。長さ・太さ・速さ・明るさはばらす（強弱）
            float streaks(float2 uv, float t, out float heat, float lanes)
            {
                float lane = floor(uv.y * lanes), f = frac(uv.y * lanes) - 0.5;
                float h1 = hash(lane), h2 = hash(lane + 9.1), h3 = hash(lane + 23.7), h4 = hash(lane + 41.3);
                float w = 0.08 + 0.4 * h2 * h2;                      // 太さ
                float len = 0.15 + 0.6 * h1;                          // 長さ（帯の幅に対する割合）
                float x = frac(uv.x * (_Aspect / 8) * (0.6 + 0.8 * h4) + t * _Speed * (0.6 + 0.9 * h3) + h1 * 7);
                float along = smoothstep(0, 0.02, x) * (1 - smoothstep(len * 0.6, len, x));   // 先が明るく尾へ消える
                float head = 1 - smoothstep(0, len * 0.5, x);
                float across = 1 - smoothstep(w * 0.4, w, abs(f));
                heat = h3;
                return along * across * (0.35 + 0.65 * h2) * (0.5 + 0.5 * head);
            }

            float4 frag(v2f i):SV_Target
            {
                float t = _LabT;
                float2 uv = i.uv;
                // 開き: 中心から上下へ
                float d = abs(uv.y - 0.5) * 2;                        // 0 中心 … 1 縁
                float open = max(_Open, 1e-3);
                if (d > open) discard;
                float dn = d / open;                                  // 開いた帯の中での縁までの距離
                float yb = (uv.y - 0.5) / open + 0.5;                 // 開いた帯の中の縦位置
                float3 c = _Base.rgb * (0.75 + 0.25 * (1 - dn));
                float heat;
                if (_Mode < 0.5 || _Mode > 3.5)
                {
                    float s1 = streaks(float2(uv.x, yb), t, heat, 22);
                    float s2 = streaks(float2(uv.x + 0.37, yb + 0.013), t * 0.8, heat, 9) * 0.8;   // 太い線を少し
                    float s = saturate(s1 + s2);
                    float3 sc = _Mode > 3.5 ? hue(frac(yb * 1.3 + uv.x * 0.2 - t * 0.6)) * 1.3 : lerp(_C1.rgb, _C2.rgb, heat);
                    c = lerp(c, sc, saturate(s * 1.4));
                    c += _C3.rgb * smoothstep(0.75, 1, s) * 0.8;      // 明るい線の芯だけ HDR
                    c += _C1.rgb * 0.35 * (1 - dn) * (0.8 + 0.2 * sin(uv.x * 30 + t * 40));   // 帯の中心がほんのり光る
                }
                else if (_Mode < 1.5)
                {
                    // 電撃: 流線（細い）＋帯に沿って走る稲妻（1/30 秒ごとに形が変わる）
                    float s = saturate(streaks(float2(uv.x, yb), t, heat, 30) * 1.2);
                    c = lerp(c, lerp(_C1.rgb, _C2.rgb, heat), s);
                    float st = floor(t * 30);
                    [unroll] for (int k = 0; k < 3; k++)
                    {
                        float sd = st * 1.7 + k * 13.1;
                        float x = uv.x * _Aspect * (1.2 + k * 0.5) + sd;
                        float n = frac(sin(floor(x) * 12.9 + sd) * 43758.5), n2 = frac(sin(floor(x + 1) * 12.9 + sd) * 43758.5);
                        float y = 0.2 + 0.6 * lerp(n, n2, frac(x)) ;
                        float dd = abs(yb - y) * 40;
                        float on = step(0.35, frac(sin(sd * 3.3) * 999));
                        c += (_C3.rgb * exp(-dd * dd * 2) + _C2.rgb * exp(-dd * 0.6) * 0.35) * on;
                    }
                }
                else if (_Mode < 2.5)
                {
                    // 炎: 上下の縁から帯の中へ燃えるアニメの炎（4 段のべた塗り）。帯に沿って後ろへ流れる
                    float vv = 1 - dn;                                // 0 縁 … 1 中心
                    vv = 1 - vv;                                      // 0 中心 … 1 縁 → 縁からの深さに直す
                    float depth = 1 - dn;                             // 縁 0 → 中心 1
                    float2 q = float2(uv.x * _Aspect * 0.25 + t * _Speed * 0.5 + (uv.y > 0.5 ? 3.1 : 0), depth * 0.6 - t * 1.2);
                    float n = tex2D(_NoiseTex, q).r * 0.7 + tex2D(_NoiseTex, q * 2.3 + 0.3).r * 0.3;
                    float F = (1 - depth) * 1.5 - n * 1.1 - 0.05;
                    float aa = max(fwidth(F), 1e-3);
                    float e0 = smoothstep(0, aa, F), e1 = smoothstep(0.25, 0.25 + aa, F), e2 = smoothstep(0.5, 0.5 + aa, F), e3 = smoothstep(0.75, 0.75 + aa, F);
                    float3 fc = _C1.rgb * 0.8 * e0; fc = lerp(fc, _C1.rgb, e1); fc = lerp(fc, _C2.rgb, e2); fc = lerp(fc, _C3.rgb, e3);
                    c = lerp(c, fc, e0);
                    float s = streaks(float2(uv.x, yb), t, heat, 14) * (1 - e0);
                    c += _C2.rgb * s * 0.6;
                }
                else
                {
                    // 矢印: ＞ の形が右から左へ流れる。1 つおきに明るさを変える
                    float x = uv.x * _Aspect * 1.2 + t * _Speed * 2.2;
                    float cell = floor(x), fx = frac(x);
                    float chev = abs(fx - 0.5 - (abs(yb - 0.5) * 0.9 - 0.2));
                    float m = 1 - smoothstep(0.16, 0.2, chev);
                    float alt = fmod(cell, 2) < 1 ? 1 : 0.6;
                    c = lerp(c, lerp(_C1.rgb, _C2.rgb, alt), m);
                    c += _C3.rgb * m * alt * 0.25;
                }
                // 縁: 白い線（内）＋黒い線（外）
                float edgeW = 0.06 / open;
                float white = smoothstep(1 - edgeW * 1.6, 1 - edgeW * 1.2, dn) * (1 - smoothstep(1 - edgeW * 0.6, 1 - edgeW * 0.4, dn));
                float black = smoothstep(1 - edgeW * 0.55, 1 - edgeW * 0.45, dn);
                c = lerp(c, float3(1.6, 1.6, 1.6), white * _Edge);
                c = lerp(c, float3(0, 0, 0), black * _Edge);
                return float4(c * _Alpha, _Alpha);
            }
            ENDCG
        }
    }
}
