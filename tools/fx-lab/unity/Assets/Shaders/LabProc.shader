// 式で描く光の見本（本人 2026-09-30「プロシージャルなテクスチャをしっかり選んで」「他にもいろいろ作ってみて」）
// 板 1 枚に加算・HDR で描く。絵の素材は使わず、勾配ノイズ・FBM・極座標・距離関数だけ
// _Mode 0 = 光の柱   縦の光。縁を FBM で揺らし、中を光の筋が昇る。足元に広がる光
// _Mode 1 = 光芒     画面の外の 1 点から差す薄い光の帯。角度方向の FBM が 2 段（太い束と細い筋）でゆっくり揺れる
// _Mode 2 = 稲妻     上から地面へ落ちる折れ線。FBM で 2 段にずらし、1/24 秒ごとに形が変わる。芯は白く硬く、周りは色
// _Mode 3 = 炎の渦   極座標（角度・log 半径）の上を FBM が中心へ巻き込む。炎の色は黒体の並び（黒 → 赤 → 橙 → 黄 → 白）
// _Mode 4 = 光の球   球の内側でプラズマ（domain warp した FBM の等高線）が走り、縁はフレネルで光る。外へ薄い光
// _Mode 5 = 衝撃波   半径を FBM で歪ませた輪。外縁は硬く内側は柔らかい。RGB で半径をずらして色収差
// _Mode 6 = 魔法陣   同心の輪・六角と三角の線・輪の上のルーン（ハッシュの目盛り）が別々の速さで回る
Shader "Lab/Proc"
{
    Properties
    {
        _Mode ("Mode", Float) = 0
        _Col ("Col", Color) = (1,0.6,0.2,1)
        _Hot ("Hot", Color) = (1,0.9,0.6,1)
        _T ("Time", Float) = 0
        _Intensity ("Intensity", Float) = 1
        _Aspect ("Aspect", Float) = 1.75
        _Seed ("Seed", Float) = 0
        _Radius ("Radius", Float) = 0.5
        _Width ("Width", Float) = 0.1
        _Reach ("Reach", Float) = 1
        _Origin ("Origin", Vector) = (0,0,0,0)
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
            float4 _Col, _Hot, _Origin; float _Mode, _T, _Intensity, _Aspect, _Seed, _Radius, _Width, _Reach;
            struct appdata { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }

            float h1(float x) { return frac(sin(x * 127.1 + _Seed * 57.3) * 43758.5453); }
            float h2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7)) + _Seed * 57.3) * 43758.5453); }
            float2 g2(float2 p) { float a = h2(p) * 6.2831853; return float2(cos(a), sin(a)); }
            float gnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p); float2 u = f * f * f * (f * (f * 6 - 15) + 10);
                float a = dot(g2(i), f), b = dot(g2(i + float2(1, 0)), f - float2(1, 0));
                float c = dot(g2(i + float2(0, 1)), f - float2(0, 1)), d = dot(g2(i + 1), f - 1);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y) * 0.7 + 0.5;
            }
            float fbm(float2 p) { float s = 0, a = 0.5; for (int k = 0; k < 5; k++) { s += a * gnoise(p); p = p * 2.03 + 17.1; a *= 0.5; } return s; }
            float fbm3(float2 p) { float s = 0, a = 0.5; for (int k = 0; k < 3; k++) { s += a * gnoise(p); p = p * 2.03 + 17.1; a *= 0.5; } return s; }
            // 黒体の並び（炎）
            float3 blackbody(float t)
            {
                t = saturate(t);
                return float3(smoothstep(0.0, 0.45, t), smoothstep(0.25, 0.8, t) * 0.85, smoothstep(0.6, 1.0, t)) * (0.2 + 2.8 * t);
            }
            // 正六角形の距離（線の太さは呼び出し側）
            float hexd(float2 p, float r) { p = abs(p); return max(dot(p, float2(0.8660254, 0.5)), p.y) - r; }
            float trid(float2 p, float r, float rot)
            {
                float ca = cos(rot), sa = sin(rot); p = float2(p.x * ca - p.y * sa, p.x * sa + p.y * ca);
                float d = -1e5; for (int k = 0; k < 3; k++) { float a = k * 2.0943951 + 1.5707963; d = max(d, dot(p, float2(cos(a), sin(a))) - r * 0.5); }
                return d;
            }

            float4 frag(v2f i):SV_Target
            {
                float2 p = (i.uv - 0.5) * float2(_Aspect, 1) * 2;          // 縦が -1…1
                float3 c = 0;
                if (_Mode < 0.5)
                {
                    // 光の柱
                    float top = smoothstep(1.0, 0.7, p.y) * smoothstep(-1.0, -0.85, p.y);
                    float edge = fbm(float2(p.y * 1.6 - _T * 2.2, 3.1));
                    float w = _Width * (0.75 + 0.5 * edge);
                    float x = abs(p.x - _Origin.x);
                    float core = exp(-pow(x / w, 2) * 3);
                    float glow = exp(-x / (w * 4)) * 0.35 + exp(-x / (w * 12)) * 0.12;
                    // 中を昇る筋: 細い列ごとに速さと長さをばらす
                    float col_ = floor((p.x - _Origin.x) / (w * 0.18)); float id = col_ + 31;
                    float sp = 1.5 + 2.5 * h1(id); float s = frac(p.y * 0.5 - _T * sp + h1(id + 5) * 9);
                    float dash = pow(saturate(s / (0.3 + 0.5 * h1(id + 9))), 3) * step(s, 0.3 + 0.5 * h1(id + 9));
                    float streak = dash * core * (0.3 + 1.5 * pow(h1(id + 13), 3));
                    // 足元の広がり
                    float2 g = float2((p.x - _Origin.x) * 0.5, (p.y + 0.86) * 1.8); float ground = exp(-dot(g, g) * 3) * smoothstep(-1, -0.86, p.y) * 0.9;
                    float reach = smoothstep(_Reach, _Reach - 0.25, 1 - (p.y + 1) * 0.5);   // 上から降りてくる
                    float k = (core * 1.6 + glow + streak) * top * reach + ground * reach;
                    c = lerp(_Col.rgb, _Hot.rgb, saturate(core * 0.9)) * k;
                    c = lerp(c, k, saturate(core * core * 0.8));
                }
                else if (_Mode < 1.5)
                {
                    // 光芒
                    float2 o = _Origin.xy; float2 d = p - o; float r = length(d); float ang = atan2(d.y, d.x);
                    float bands = pow(fbm(float2(ang * 9 + _Seed, _T * 0.12)), 2.2);
                    float fine = pow(fbm3(float2(ang * 60, _T * 0.35 + 7)), 1.6);
                    float dust = fbm3(p * 3 + float2(_T * 0.25, -_T * 0.1));
                    float k = bands * (0.4 + 0.9 * fine) * (0.7 + 0.5 * dust) * exp(-r * 0.55) * smoothstep(0.1, 0.6, r);
                    c = lerp(_Col.rgb, _Hot.rgb, saturate(k * 1.5)) * k * 1.6 * _Reach;
                }
                else if (_Mode < 2.5)
                {
                    // 稲妻: 3 本。形は 1/24 秒ごとに変わる
                    float step_ = floor(_T * 24);
                    float k = 0;
                    for (int b = 0; b < 3; b++)
                    {
                        float sd = step_ * 7.3 + b * 91.7;
                        float on = step(0.35, h1(sd + 3));                    // 消えている瞬間もある
                        float x0 = (h1(sd) - 0.5) * _Aspect * 1.4 + _Origin.x;
                        float y = p.y;
                        float dx = (fbm(float2(y * 2.2 + sd, 0.5)) - 0.5) * 1.1 + (fbm3(float2(y * 11 + sd * 1.3, 2.5)) - 0.5) * 0.28;
                        float dist = abs(p.x - x0 - dx * smoothstep(1.0, 0.6, y));     // 上は 1 点から
                        float ground = smoothstep(-1.0, -0.7, y);
                        float core = exp(-dist * 320) * 2.2, glow = exp(-dist * 22) * 0.45 + exp(-dist * 6) * 0.12;
                        float br = 0.5 + 0.8 * h1(sd + 11);
                        k += (core + glow) * on * br * ground;
                        // 枝: 本線から短く分かれる
                        float br_y = -0.2 + 0.7 * h1(sd + 17); float bd = abs(y - br_y);
                        float bx = x0 + dx + sign(h1(sd + 19) - 0.5) * (br_y - y) * 1.6;
                        float bdist = abs(p.x - bx) + bd * 0.15;
                        k += exp(-bdist * 260) * 1.2 * step(y, br_y) * step(br_y - 0.35, y) * on * br;
                    }
                    c = lerp(_Col.rgb, 1, saturate(k * 0.6)) * k;
                    // 落ちた瞬間の面の光（画面全体がふっと明るく）
                    c += _Col.rgb * 0.06 * step(0.7, h1(step_ * 1.7));
                }
                else if (_Mode < 3.5)
                {
                    // 炎の渦
                    float2 q = p - _Origin.xy; float r = length(q); float ang = atan2(q.y, q.x); float lr = log(max(r, 1e-4));
                    float u = ang * 2.5 + lr * 3.5 - _T * 1.6, v = lr * 2.5 - _T * 3.5;
                    float2 w = float2(fbm3(float2(u * 0.8 + 3, v * 0.8)), fbm3(float2(u * 0.8 + 9, v * 0.8 + 4)));
                    float n = fbm(float2(u, v) + (w - 0.5) * 1.5);
                    float mask = (1 - smoothstep(0.15, _Radius, r)) * smoothstep(0.0, 0.08, r);
                    float heat = saturate((n - 0.28) * 1.9) * mask + exp(-r * 6) * 1.2 * smoothstep(0.02, 0.1, r);
                    heat *= _Reach;
                    c = blackbody(heat) * 1.1;
                }
                else if (_Mode < 4.5)
                {
                    // 光の球
                    float2 q = p - _Origin.xy; float r = length(q) / _Radius;
                    float inside = 1 - smoothstep(0.98, 1.0, r);
                    float z = sqrt(saturate(1 - r * r));
                    float fres = pow(1 - z, 2.5);
                    float2 w = float2(fbm3(q * 2.5 + float2(_T * 0.7, 0)), fbm3(q * 2.5 + float2(0, -_T * 0.5) + 5));
                    float n = fbm(q * 3.2 + (w - 0.5) * 2.2 + float2(_T * 0.3, _T * 0.2));
                    float fil = pow(1 - saturate(abs(n - 0.5) * 7), 3);                // 等高線の細い糸
                    float n2 = fbm3(q * 6 + float2(-_T * 0.9, _T * 0.4) + 11);
                    float fil2 = pow(1 - saturate(abs(n2 - 0.5) * 9), 4) * 0.5;
                    float body = 0.12 + fres * 1.6 + fil * 1.1 + fil2;
                    float rim = exp(-(r - 1) * 9) * step(1, r) * 0.9 + exp(-(r - 1) * 2.5) * step(1, r) * 0.15;
                    float k = body * inside + rim;
                    c = lerp(_Col.rgb, _Hot.rgb, saturate(fil * 1.2 + fres)) * k * _Reach;
                }
                else if (_Mode < 5.5)
                {
                    // 衝撃波（RGB で半径をずらす）
                    float2 q = p - _Origin.xy; float r = length(q); float ang = atan2(q.y, q.x) / 6.2831853 + 0.5;
                    float2 cp = float2(cos(ang * 6.2831853), sin(ang * 6.2831853));
                    float n = fbm3(cp * 1.4 + _Seed);
                    float3 k3;
                    for (int ch = 0; ch < 3; ch++)
                    {
                        float R = _Radius * (1 + (n - 0.5) * 0.34) * (1 + ch * 0.022);
                        float d = r - R;
                        float outer = 1 - smoothstep(0, _Width * 0.15, d);
                        float inner = smoothstep(-_Width, 0, d);
                        float band = outer * inner;
                        float bright = pow(saturate(fbm3(cp * 3.1 + _Seed * 2 + 4) * 1.6 - 0.25), 2) * 2.2 + 0.15;   // 明るい所と切れかけた所
                        k3[ch] = band * bright + exp(-abs(d) / (_Width * 3)) * 0.12;
                    }
                    float hot = smoothstep(-_Width * 0.3, 0, r - _Radius) * (1 - smoothstep(0, _Width * 0.15, r - _Radius));
                    c = (lerp(_Col.rgb, 1, hot * 0.8) * k3) * _Reach * 1.8;
                }
                else
                {
                    // 魔法陣
                    float2 q = p - _Origin.xy; float r = length(q) / _Radius; float ang = atan2(q.y, q.x);
                    float lw = 0.009;
                    float k = 0;
                    // 同心の輪
                    float rings[5] = { 1.0, 0.94, 0.72, 0.66, 0.36 };
                    for (int j = 0; j < 5; j++) k += exp(-pow((r - rings[j]) / lw, 2)) * (j == 0 || j == 2 ? 1.4 : 0.7);
                    // 六角と三角（別々に回る）
                    float rot1 = _T * 0.35, rot2 = -_T * 0.5;
                    float2 q1 = float2(q.x * cos(rot1) - q.y * sin(rot1), q.x * sin(rot1) + q.y * cos(rot1)) / _Radius;
                    k += exp(-pow(hexd(q1, 0.66) / lw, 2)) * 1.1;
                    k += exp(-pow(trid(q / _Radius, 0.66, rot2) / lw, 2)) * 0.9 + exp(-pow(trid(q / _Radius, 0.66, rot2 + 3.14159) / lw, 2)) * 0.9;
                    // ルーン: 0.83 の輪の上に、ハッシュで長さ・有無を変えた目盛り
                    float ra = ang + _T * 0.25; float cell = floor(ra / 6.2831853 * 40 + 0.5); float fa = frac(ra / 6.2831853 * 40 + 0.5) - 0.5;
                    float rh = h1(cell + 100); float rl = 0.02 + 0.06 * h1(cell + 200);
                    float rune = step(0.25, rh) * exp(-pow(fa * 40 * 0.09 / lw, 2)) * (1 - smoothstep(0, 0.004, abs(r - 0.83) - rl));
                    float rune2 = step(0.6, h1(cell + 300)) * exp(-pow((r - 0.83 - rl - 0.02) / lw, 2)) * (1 - smoothstep(0.35, 0.4, abs(fa)));
                    k += (rune + rune2) * 1.2;
                    // 小さな輪（六角の頂点）
                    for (int v = 0; v < 6; v++)
                    {
                        float a = v * 1.0471976 + rot1 + 0.5235988; float2 cpos = float2(cos(a), sin(a)) * 0.66;
                        k += exp(-pow((length(q1 - cpos) - 0.07) / lw, 2)) * 0.9;
                    }
                    // 面のうっすらした光と、外へこぼれる光
                    k += (1 - smoothstep(0.9, 1.0, r)) * 0.05 + exp(-(r - 1) * 6) * step(1, r) * 0.15;
                    float pulse = 0.85 + 0.15 * sin(_T * 6);
                    c = lerp(_Col.rgb, _Hot.rgb, saturate(k * 0.5)) * k * pulse * _Reach * 1.3;
                }
                return float4(c * _Intensity, 0);
            }
            ENDCG
        }
    }
}
