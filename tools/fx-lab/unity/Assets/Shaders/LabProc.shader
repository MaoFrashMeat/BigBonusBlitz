// 式で描く光（本人 2026-09-30「プロシージャルなテクスチャをしっかり選んで」「他にもいろいろ作ってみて」→「全部、クオリティが低い」で作り直し）
// 板 1 枚に加算・HDR で描く。絵の素材は使わず、勾配ノイズ・FBM・極座標・距離関数・式の粒子だけ
// 作り直しで足した層: 式の粒子（火の粉・火花・破片・光の粒。速度方向に伸びる）、地面の照り返し、着地の輪、残像、前後の層（_Layer 0 = 主人公の後ろ、1 = 前）
// _Mode 0 = 光の柱   芯・鞘・らせんの帯・縁の裂け・昇る火の粉・着地の輪・地面の照り返し。_Origin.z = 着地の進み
// _Mode 1 = 光芒     遠い帯（低い周波数）と近い筋（高い周波数）の 2 層・漂う塵・光源のにじみ
// _Mode 2 = 稲妻     本線 3 本＋枝 2 段。1/24 秒ごとに作り直し、前の 2 コマを残像で残す。落ちた所で火花が散り、地面が照る
// _Mode 3 = 炎の渦   大きく遅い渦と小さく速い渦を重ね、穴のある炎に。外へ螺旋で飛ぶ火の粉。色は黒体
// _Mode 4 = 光の球   白熱の核・2 段のプラズマ糸・表面を走る放電（1/20 秒で変わる）・色収差のフレネル・周回する光の粒
// _Mode 5 = 衝撃波   硬い先端＋ノイズで千切れた尾・色収差・地面を走る土煙の輪・飛ぶ破片・中心の閃光
// _Mode 6 = 魔法陣   輪 7 本・文字の帯 2 段（目盛り・点・弧）・六芒星・歯車の輪・頂点の小円が別々に回る。床に寝かせるのは呼び出し側
// _Mode 7 = 陣の光   陣の縁から立ち上る細い光の線と、舞い上がる光の粒（寝かせない板で描く）
Shader "Lab/Proc"
{
    Properties
    {
        _Mode ("Mode", Float) = 0
        _Layer ("Layer", Float) = 1
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
        _Ground ("GroundY", Float) = -0.6
        _Sway ("Sway (bend, breathe, warp, drift)", Vector) = (1,1,1,1)
        _PNoise ("ProcNoise", 2D) = "gray" {}
        _PRidge ("ProcRidge", 2D) = "gray" {}
        _PVoro ("ProcVoro", 2D) = "gray" {}
        _PStreak ("ProcStreak", 2D) = "gray" {}
        _PWarp ("ProcWarp", 2D) = "gray" {}
        _PSparks ("ProcSparks", 2D) = "black" {}
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
            sampler2D _PNoise, _PRidge, _PVoro, _PStreak, _PWarp, _PSparks;
            float4 _Col, _Hot, _Origin, _Sway; float _Mode, _Layer, _T, _Intensity, _Aspect, _Seed, _Radius, _Width, _Reach, _Ground;
            struct appdata { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }

            float h1(float x) { return frac(sin(x * 127.1 + _Seed * 57.3) * 43758.5453); }
            float h2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7)) + _Seed * 57.3) * 43758.5453); }
            float2 g2(float2 p) { float a = h2(p) * 6.2831853; return float2(cos(a), sin(a)); }
            // ノイズは Blender で焼いた継ぎ目なしの板（textures/proc_*.png。bake_textures.py）を読む。式の勾配ノイズは使わない
            float gnoise(float2 p) { return tex2D(_PNoise, p * 0.06).b; }
            float fbm(float2 p) { float3 n = tex2D(_PNoise, p * 0.11).rgb; return n.r * 0.5 + n.g * 0.3 + n.b * 0.2; }
            float fbm3(float2 p) { float3 n = tex2D(_PNoise, p * 0.11).rgb; return n.g * 0.6 + n.b * 0.4; }
            float3 blackbody(float t)
            {
                t = saturate(t);
                return float3(smoothstep(0.0, 0.45, t), smoothstep(0.25, 0.8, t) * 0.85, smoothstep(0.6, 1.0, t)) * (0.2 + 2.8 * t);
            }
            float hexd(float2 p, float r) { p = abs(p); return max(dot(p, float2(0.8660254, 0.5)), p.y) - r; }
            float trid(float2 p, float r, float rot)
            {
                float ca = cos(rot), sa = sin(rot); p = float2(p.x * ca - p.y * sa, p.x * sa + p.y * ca);
                float d = -1e5; for (int k = 0; k < 3; k++) { float a = k * 2.0943951 + 1.5707963; d = max(d, dot(p, float2(cos(a), sin(a))) - r * 0.5); }
                return d;
            }
            float line_(float d, float w) { return exp(-d * d / (w * w)); }
            // うねり: 周波数の合わない sin の和（周期が見えない）。0 を中心に ±1
            float sway(float t, float s) { return (sin(t * 1.31 + s) * 0.5 + sin(t * 2.17 + s * 1.7) * 0.3 + sin(t * 3.71 + s * 0.4) * 0.2); }
            // 座標の揺れ: 歪みの板を時間で流して座標を押す（形そのものがゆらゆらする）
            float2 wobble(float2 p, float t, float amt) { float2 w = tex2D(_PWarp, p * 0.07 + float2(t * 0.013, -t * 0.009)).rg - 0.5; float2 w2 = tex2D(_PWarp, p * 0.2 + float2(-t * 0.03, t * 0.02) + 0.3).rg - 0.5; return (w * 0.7 + w2 * 0.3) * amt; }
            // 式の粒子: 原点から角度 a0 ± spread へ飛び、重力 grav で落ちる（負なら昇る）。速度方向に伸びる。life ごとに繰り返し出る
            float particles(float2 p, float2 org, float t, int n, float spd, float grav, float life, float size, float stretch, float a0, float spread, float seed)
            {
                float acc = 0;
                [loop] for (int i = 0; i < n; i++)
                {
                    float id = i * 1.7 + seed * 100;
                    float ha = h1(id), hb = h1(id + 1.3), hc = h1(id + 2.7), hd = h1(id + 3.9);
                    float age = frac(t / life + ha) * life;
                    float ang = a0 + (hb - 0.5) * spread;
                    float2 v = float2(cos(ang), sin(ang)) * spd * (0.45 + 1.1 * hc * hc);
                    float2 pos = org + v * age + float2(0, -grav * age * age);
                    float2 vel = v + float2(0, -2 * grav * age);
                    float vl = max(length(vel), 1e-3); float2 vn = vel / vl;
                    float2 q = p - pos; float along = dot(q, vn), perp = dot(q, float2(-vn.y, vn.x));
                    float s = size * (0.5 + hd);
                    float tail = s * (1 + stretch * vl);
                    float shape = exp(-perp * perp / (s * s)) * (along > 0 ? exp(-along * along / (s * s)) : exp(-along * along / (tail * tail)));
                    float fade = (1 - age / life) * smoothstep(0, 0.04, age);
                    acc += shape * fade * (0.3 + 1.4 * pow(h1(id + 5.1), 3));
                }
                return acc;
            }
            // 地面の照り返し（横長の楕円。FBM でむらを付ける）
            float groundLight(float2 p, float2 c, float2 r, float t)
            {
                float2 q = (p - c) / r; float g = exp(-dot(q, q) * 1.6);
                return g * (0.6 + 0.6 * fbm3(p * 2.5 + float2(t * 0.4, 0)));
            }
            // 地面を走る楕円の輪（土煙・衝撃の輪）
            float groundRing(float2 p, float2 c, float R, float squash, float w, float t)
            {
                float2 q = (p - c) * float2(1, 1 / squash); float r = length(q);
                float ang = atan2(q.y, q.x); float n = fbm3(float2(ang * 2.2, t)) - 0.5;
                float d = r - R * (1 + n * 0.25);
                return (1 - smoothstep(0, w * 0.2, d)) * smoothstep(-w, 0, d) * (0.5 + fbm3(float2(ang * 6, R * 3)));
            }
            // 稲妻 1 本の距離（x のずれを y の関数で。上 1 点から下へ）
            float boltDist(float2 p, float x0, float sd, float topY, float botY, float amp)
            {
                float y = p.y;
                float k = smoothstep(topY, topY - 0.25, y);
                float dx = ((fbm(float2(y * 2.2 + sd, 0.5)) - 0.5) * 1.1 + (fbm3(float2(y * 9 + sd * 1.3, 2.5)) - 0.5) * 0.3 + (gnoise(float2(y * 26 + sd * 2.1, 7)) - 0.5) * 0.08) * amp * k;
                return abs(p.x - x0 - dx) + step(y, botY) * 10 + step(topY, y) * 10;
            }

            float4 frag(v2f i):SV_Target
            {
                float2 p = (i.uv - 0.5) * float2(_Aspect, 1) * 2;          // 縦が -1…1
                p += wobble(p, _T, 0.05 * _Sway.z);                          // 形そのものがゆらぐ
                float3 c = 0;
                float back = _Layer < 0.5 ? 1 : 0, front = 1 - back;
                if (_Mode < 0.5)
                {
                    // ---- 光の柱 ----
                    float ox = _Origin.x + (sway(_T, 1) * 0.04 + sin(p.y * 1.4 + _T * 1.9) * 0.035 * (0.5 + 0.5 * p.y)) * _Sway.x;   // しなる（上ほど大きく）
                    float x = p.x - ox; float ax = abs(x);
                    float w = _Width * (1 + 0.18 * sway(_T * 1.6, 3) * _Sway.y + 0.1 * sin(p.y * 3 - _T * 4) * _Sway.y);   // 呼吸（縦に波が走る）
                    float reach = smoothstep(_Reach, _Reach - 0.2, 1 - (p.y + 1) * 0.5);            // 上から降りる
                    float ground = smoothstep(_Ground - 0.02, _Ground + 0.02, p.y);
                    float edgeN = fbm(float2(p.y * 1.4 - _T * 2.5, 3.1));
                    float we = w * (0.8 + 0.5 * edgeN);
                    // 鞘（後ろ）: 広く柔らかい。縦に流れるむら
                    float3 stk = tex2D(_PStreak, float2(p.y * 0.35 - _T * 0.9 - 0.2 * sin(_T * 0.9), x * 1.6 + 0.3 + 0.03 * sway(_T, 5))).rgb;                  // 縦に長い筋（u = y）
                    float sheathN = 0.35 + 1.1 * (stk.r * 0.6 + stk.g * 0.4) + stk.b * 0.6;
                    float sheath = exp(-ax / (w * 3.2)) * sheathN * 0.55 + exp(-ax / (w * 9)) * 0.1;
                    // 芯（前）: 硬く白い。縦の細かいちらつき
                    float coreN = 0.8 + 0.45 * tex2D(_PStreak, float2(p.y * 0.8 - _T * 2.2, x * 6 + 0.7)).b;
                    float core = exp(-pow(ax / we, 2) * 3.5) * coreN;
                    // らせんの帯 2 本: 手前に来たときは前の層、奥のときは後ろの層
                    float helix = 0;
                    for (int hb = 0; hb < 2; hb++)
                    {
                        float ph = p.y * (5.5 + 1.5 * sin(_T * 0.6)) + _T * (5 + 1.5 * sway(_T * 0.5, 7)) + hb * 3.14159;   // らせんの間隔と速さが揺れる
                        float xb = sin(ph) * w * 2.3; float depth = cos(ph);
                        float band = line_(x - xb, w * 0.22) * (0.5 + 0.5 * abs(depth));
                        helix += band * (depth > 0 ? front : back) * (0.6 + 0.4 * gnoise(float2(p.y * 3 - _T * 2, hb * 7)));
                    }
                    // 縁の裂け: 細い筋が縁から上へ昇る
                    float col_ = floor(x / (w * 0.35)); float id = col_ + 31;
                    float sp = 1.5 + 3 * h1(id); float s = frac(p.y * 0.55 - _T * sp + h1(id + 5) * 9);
                    float dl = 0.25 + 0.5 * h1(id + 9);
                    float tear = pow(saturate(s / dl), 3) * step(s, dl) * exp(-pow(ax / (w * 2.4), 4)) * step(w * 0.9, ax) * (0.3 + 1.2 * pow(h1(id + 13), 3));
                    // 昇る火の粉（前）と、着地の輪・地面の照り返し
                    float sparks = particles(p, float2(ox, _Ground + 0.02), _T, 40, 0.5, -0.35, 1.6, 0.007, 3.0, 1.5708, 1.1, 1) * front;
                    float hit = _Origin.z;
                    float ring = hit > 0 ? groundRing(p, float2(ox, _Ground), hit * 1.1, 0.3, 0.05 + hit * 0.1, 3.3) * (1 - hit) * 1.6 : 0;
                    float gl = groundLight(p, float2(ox, _Ground - 0.04), float2(0.7, 0.13), _T) * 0.9;
                    float top = smoothstep(1.0, 0.85, p.y);
                    float k = (core * 2.0 * front + sheath + helix * 0.9 + tear * 0.9) * top * reach * ground + (ring + gl) * reach + sparks * reach;
                    float3 col = lerp(_Col.rgb, _Hot.rgb, saturate(core * 0.9 + helix * 0.5));
                    col = lerp(col, 1, saturate(core * core * 0.9));
                    c = col * k;
                    c += 1 * sparks * reach * 0.6;   // 火の粉は白寄り
                }
                else if (_Mode < 1.5)
                {
                    // ---- 光芒 ----
                    float2 o = _Origin.xy + float2(sway(_T * 0.5, 2), sway(_T * 0.4, 9)) * 0.12 * _Sway.w;   // 光源が漂う
                    float2 d = p - o; float r = length(d); float ang = atan2(d.y, d.x) + 0.06 * sway(_T * 0.35, 4) * _Sway.x;   // 帯が掃く
                    float3 ray = tex2D(_PStreak, float2(r * 0.12 - _T * 0.02, ang * 1.4 + _Seed * 0.1 + _T * 0.004)).rgb;          // r に長い筋（u = r、v = 角度）
                    float3 ray2 = tex2D(_PStreak, float2(r * 0.3 + _T * 0.015, ang * 4.5 + 2.3 - _T * 0.012)).rgb;
                    float far_ = pow(ray.r * 0.7 + ray.g * 0.3, 2.6);                                            // 遠い太い帯
                    float near_ = pow(ray2.g * 0.5 + ray2.b * 0.6, 1.8);                                        // 近い細い筋
                    float breath = 0.85 + 0.15 * sin(_T * 0.7);
                    float dust = fbm3(p * 2.5 + float2(_T * 0.2, -_T * 0.08));
                    float rays = (far_ * 0.8 + far_ * near_ * 1.6) * (0.6 + 0.6 * dust) * exp(-r * 0.5) * smoothstep(0.1, 0.5, r) * breath;
                    // 漂う塵（ゆっくり落ちる粒。光の帯の中だけ光る）
                    float3 mt = tex2D(_PSparks, p * 0.9 + float2(_T * 0.01, -_T * 0.02)).rgb;                          // 漂う塵（点の板をゆっくり流す）
                    float motes = (mt.r * 1.2 + mt.g * 0.6) * (far_ * 2 + 0.15) * (0.6 + 0.4 * sin(_T * 3 + p.x * 20));
                    // 光源のにじみ（画面の外。縁ににじむ）
                    float src = exp(-r * 2.2) * 1.2 + exp(-abs(d.y - d.x * 0.6) * 9) * exp(-r * 1.2) * 0.5;
                    c = lerp(_Col.rgb, _Hot.rgb, saturate(rays * 1.2)) * (rays * 1.8 + src) * _Reach + _Hot.rgb * motes * 0.8;
                }
                else if (_Mode < 2.5)
                {
                    // ---- 稲妻 ----
                    float step_ = floor(_T * 24);
                    float k = 0, hot = 0, impactK = 0;
                    for (int b = 0; b < 3; b++)
                    {
                        // 今のコマと前の 2 コマ（残像）
                        for (int back_ = 0; back_ < 3; back_++)
                        {
                            float st = step_ - back_; float sd = st * 7.3 + b * 91.7;
                            float on = step(0.4, h1(sd + 3)) * (back_ == 0 ? 1 : (back_ == 1 ? 0.35 : 0.12));
                            if (on <= 0) continue;
                            float x0 = (h1(sd) - 0.5) * _Aspect * 1.3 + _Origin.x;
                            float dist = boltDist(p, x0, sd, 1.0, _Ground, 1.0);
                            float rg = tex2D(_PRidge, float2(p.x * 0.5 + sd * 0.01, p.y * 0.5)).g;
                            float core = exp(-dist * 340) * 2.4, mid = exp(-dist * 40) * (0.35 + 0.4 * rg), glow = exp(-dist * 7) * (0.08 + 0.12 * rg);
                            float br = 0.6 + 0.8 * h1(sd + 11);
                            k += (core + mid + glow) * on * br; hot += core * on;
                            // 枝 2 段: 本線の途中から短く、さらにその途中から
                            for (int br_ = 0; br_ < 3; br_++)
                            {
                                float bs = sd + 17 + br_ * 5.1; float by = _Ground + (0.15 + 0.7 * h1(bs)) * (1 - _Ground);
                                float bx0 = x0 + ((fbm(float2(by * 2.2 + sd, 0.5)) - 0.5) * 1.1 + (fbm3(float2(by * 9 + sd * 1.3, 2.5)) - 0.5) * 0.3) * smoothstep(1.0, 0.75, by);
                                float dirn = sign(h1(bs + 1) - 0.5); float len = 0.25 + 0.35 * h1(bs + 2);
                                float ty = clamp(by - p.y, 0, len);                       // 枝は下へ斜めに
                                float bxx = bx0 + dirn * ty * (0.9 + 0.6 * h1(bs + 3)) + (fbm3(float2(p.y * 8 + bs, 1)) - 0.5) * 0.15;
                                float bd = abs(p.x - bxx) + step(by, p.y) * 10 + step(p.y, by - len) * 10;
                                float bcore = exp(-bd * 300) * 1.4 + exp(-bd * 30) * 0.25;
                                k += bcore * on * br * 0.8;
                                float ty2 = clamp(by - len * 0.5 - p.y, 0, len * 0.5);   // 2 段目
                                float bxx2 = bx0 + dirn * len * 0.5 * (0.9 + 0.6 * h1(bs + 3)) - dirn * ty2 * 1.3;
                                float bd2 = abs(p.x - bxx2) + step(by - len * 0.5, p.y) * 10 + step(p.y, by - len) * 10;
                                k += (exp(-bd2 * 300) * 0.9 + exp(-bd2 * 30) * 0.15) * on * br * 0.7;
                            }
                            // 落ちた所: 火花と地面の照り返し（今のコマだけ）
                            if (back_ == 0)
                            {
                                float gx = x0 + ((fbm(float2(_Ground * 2.2 + sd, 0.5)) - 0.5) * 1.1 + (fbm3(float2(_Ground * 9 + sd * 1.3, 2.5)) - 0.5) * 0.3);
                                float age = _T - st / 24.0;
                                impactK += groundLight(p, float2(gx, _Ground), float2(0.28, 0.05), _T) * 1.1 * br;
                                impactK += particles(p, float2(gx, _Ground), _T, 24, 1.3, 1.6, 0.45, 0.005, 2.5, 1.5708, 2.4, st) * 1.2;
                                impactK += exp(-length(p - float2(gx, _Ground)) * 12) * 2.5 * exp(-age * 10);
                            }
                        }
                    }
                    // 上の雲の光（稲妻の根元がぼうっと光る）
                    float cloud = smoothstep(0.55, 1.0, p.y) * (0.05 + 0.1 * fbm3(float2(p.x * 1.5 + _T, 2))) * (0.5 + step(0.5, h1(step_ * 1.7)));
                    c = lerp(_Col.rgb, 1, saturate(hot * 0.7)) * k + _Col.rgb * (impactK + cloud);
                    c += _Col.rgb * 0.05 * step(0.7, h1(step_ * 1.7));   // 落ちた瞬間の面の光
                }
                else if (_Mode < 3.5)
                {
                    // ---- 炎の渦 ----
                    float2 q = p - _Origin.xy - float2(sway(_T * 0.8, 3), sway(_T * 0.6, 8) * 0.6) * 0.06 * _Sway.w;   // 中心が漂う
                    q.y -= max(0, q.y) * 0.25 * (0.5 + 0.5 * sin(_T * 2.3));                                            // 上へ引かれて伸び縮み
                    float r = length(q); float ang = atan2(q.y, q.x); float lr = log(max(r, 1e-4));
                    float spin = _T * 1.6 + 0.5 * sway(_T * 0.7, 6) * _Sway.x;                                          // 回る速さが揺れる
                    // 大きく遅い渦
                    float u1 = ang * 2.5 + lr * 3.5 - spin, v1 = lr * 2.5 - _T * 3.5;
                    float3 wp = tex2D(_PWarp, float2(u1, v1) * 0.09).rgb;                                             // 二重に歪ませた炎の板
                    float n1 = wp.g * 0.6 + wp.r * 0.4;
                    // 小さく速い渦（逆向きの捻り）
                    float u2 = ang * 4 - lr * 5 + _T * 2.2, v2 = lr * 4 - _T * 5.5;
                    float n2 = tex2D(_PRidge, float2(u2 + 11, v2) * 0.07).g;                                           // 細い稜線の糸
                    float n = n1 * 0.65 + n2 * 0.35;
                    float mask = (1 - smoothstep(0.1, _Radius, r)) * smoothstep(0.0, 0.06, r);
                    float thin = 1 - smoothstep(0.3, 1.0, r / _Radius);                                      // 外ほど薄く穴だらけ
                    float heat = saturate((n - 0.44 + 0.12 * thin) * 2.8) * mask;                                 // 穴だらけ（閾値を高く）
                    heat = pow(heat, 1.5) * _Reach;
                    float core = exp(-r * 14) * 0.9 * (0.9 + 0.2 * gnoise(float2(_T * 12, 3)));
                    c = blackbody(saturate(heat * 0.85 + core * 0.5)) * 0.75;
                    // 火の粉: 中心から螺旋で外へ、上へ流れる
                    float ember = 0;
                    [loop] for (int e = 0; e < 44; e++)
                    {
                        float id = e * 2.3 + 700; float ha = h1(id), hb = h1(id + 1), hc = h1(id + 2);
                        float life = 1.4 + hb; float age = frac(_T / life + ha) * life;
                        float th = hb * 6.2831853 + age * (2.5 + hc * 2) ; float rad = 0.05 + age * (0.35 + hc * 0.3);
                        float2 pos = _Origin.xy + float2(cos(th) * rad, sin(th) * rad * 0.9 + age * age * 0.25);
                        float2 vel = float2(-sin(th), cos(th)) * rad * 3 + float2(0, age * 0.5); float vl = max(length(vel), 1e-3); float2 vn = vel / vl;
                        float2 qq = p - pos; float along = dot(qq, vn), perp = dot(qq, float2(-vn.y, vn.x));
                        float s = 0.006; float shape = exp(-perp * perp / (s * s)) * (along > 0 ? exp(-along * along / (s * s)) : exp(-along * along / (s * s * (1 + vl * 4) * (1 + vl * 4))));
                        ember += shape * (1 - age / life) * smoothstep(0, 0.05, age) * (0.4 + 1.2 * hc);
                    }
                    c += blackbody(0.75) * ember * 1.4 * _Reach;
                    c += _Col.rgb * groundLight(p, float2(_Origin.x, _Ground), float2(1.1, 0.16), _T) * 0.7 * _Reach;
                }
                else if (_Mode < 4.5)
                {
                    // ---- 光の球 ----
                    float2 q = p - _Origin.xy - float2(sin(_T * 1.3) * 0.02 + sway(_T * 0.9, 2) * 0.012, sin(_T * 2.1 + 1) * 0.03 + sway(_T * 0.7, 5) * 0.01) * _Sway.w;   // 浮いて揺れる
                    float rr_ = _Radius * (1 + 0.02 * sway(_T * 3, 4) * _Sway.y);
                    float ca_ = cos(_T * 0.35), sa_ = sin(_T * 0.35); q = float2(q.x * ca_ - q.y * sa_, q.x * sa_ + q.y * ca_);   // 糸がゆっくり回る
                    float r = length(q) / rr_; float ang = atan2(q.y, q.x);
                    float inside = 1 - smoothstep(0.985, 1.0, r);
                    float z = sqrt(saturate(1 - r * r));
                    float2 w = float2(fbm3(q * 2.5 / _Radius + float2(_T * 0.7, 0)), fbm3(q * 2.5 / _Radius + float2(0, -_T * 0.5) + 5));
                    float n = fbm(q * 3.2 / _Radius + (w - 0.5) * 2.2 + float2(_T * 0.3, _T * 0.2));
                    float fil = pow(tex2D(_PVoro, (q / _Radius + (w - 0.5) * 0.9) * 0.3 + float2(_T * 0.03, -_T * 0.02)).g, 3.5) * 2.2;   // セルの縁の糸
                    float n2 = fbm3(q * 7 / _Radius + float2(-_T * 0.9, _T * 0.4) + 11);
                    float fil2 = pow(tex2D(_PRidge, (q / _Radius) * 0.4 + float2(-_T * 0.06, _T * 0.03) + 0.4).g, 4) * 1.1;
                    // 表面の放電: 1/20 秒ごとに場所が変わる細い弧
                    float st = floor(_T * 20); float arcs = 0;
                    for (int a_ = 0; a_ < 3; a_++)
                    {
                        float sd = st * 3.7 + a_ * 13.1; if (h1(sd) < 0.45) continue;
                        float a0 = h1(sd + 1) * 6.2831853, span = 0.8 + h1(sd + 2) * 1.5, rr = 0.55 + 0.4 * h1(sd + 3);
                        float da = ang - a0; da = atan2(sin(da), cos(da));
                        float rad = rr + (fbm3(float2(ang * 5 + sd, 1)) - 0.5) * 0.25;
                        float d = abs(r - rad) * _Radius;
                        arcs += exp(-d * d / (0.0025 * 0.0025)) * step(abs(da), span * 0.5) * 1.5 + exp(-d * 90) * step(abs(da), span * 0.5) * 0.2;
                    }
                    float core = exp(-r * r * 5) * (1.2 + 0.3 * sin(_T * 11)) ;
                    // フレネル（RGB で半径をずらす）
                    float3 fres;
                    for (int ch = 0; ch < 3; ch++) { float rc = r * (1 - ch * 0.012); float zc = sqrt(saturate(1 - rc * rc)); fres[ch] = pow(1 - zc, 3) * (1 - smoothstep(0.98, 1.0, rc)); }
                    float3 body = (0.08 + fil * 1.0 + fil2 + arcs + core) * inside + fres * 1.8;
                    // 外の光輪（角度でむら）と光条
                    float halo = exp(-(r - 1) * 5) * step(1, r) * (0.5 + 0.7 * fbm3(float2(ang * 4, _T * 0.8))) * 0.8 + exp(-(r - 1) * 1.5) * step(1, r) * 0.08;
                    float spikes = pow(saturate(fbm(float2(ang * 3.5, _T * 1.2)) * 1.4 - 0.3), 5) * exp(-(r - 1) * 1.8) * step(0.9, r) * 1.2;
                    // 周回する光の粒（傾いた楕円の軌道。奥は後ろの層）
                    float orbit = 0;
                    for (int ob = 0; ob < 10; ob++)
                    {
                        float id = ob * 3.1 + 900; float ph = _T * (1.2 + h1(id) * 1.5) + h1(id + 1) * 6.28; float tilt = (h1(id + 2) - 0.5) * 1.4; float rr = 1.15 + h1(id + 3) * 0.5;
                        float2 e = float2(cos(ph) * rr, sin(ph) * rr * 0.35); float2 pos = _Origin.xy + float2(e.x * cos(tilt) - e.y * sin(tilt), e.x * sin(tilt) + e.y * cos(tilt)) * _Radius;
                        float depth = sin(ph);
                        float d = length(p - pos); orbit += (exp(-d * d / (0.006 * 0.006)) * 1.5 + exp(-d * 60) * 0.3) * (depth > 0 ? front : back) * (0.6 + 0.4 * depth);
                    }
                    float3 col = lerp(_Col.rgb, _Hot.rgb, saturate(fil * 1.2 + fres.g + core));
                    c = (col * body + _Col.rgb * (halo + spikes) + lerp(_Col.rgb, 1, 0.6) * orbit) * _Reach;
                    c = lerp(c, c * 0.0 + col * halo * _Reach + lerp(_Col.rgb, 1, 0.6) * orbit * _Reach, back);   // 後ろの層は光輪と奥の粒だけ
                }
                else if (_Mode < 5.5)
                {
                    // ---- 衝撃波 ----
                    float2 q = p - _Origin.xy; float tl = 0.12 * _Sway.x; q = float2(q.x * cos(tl) - q.y * sin(tl), (q.x * sin(tl) + q.y * cos(tl)) * (1 + 0.08 * _Sway.y));   // 少し楕円で傾く
                    float r = length(q); float ang = atan2(q.y, q.x); float a01 = ang / 6.2831853 + 0.5;
                    float2 cp = float2(cos(ang), sin(ang));
                    float n = lerp(0.5, fbm3(cp * 1.4 + _Seed + _T * 0.3), saturate(_Radius * 2.5));   // 縁のうねりは広がるほど育つ
                    float3 k3;
                    for (int ch = 0; ch < 3; ch++)
                    {
                        float R = _Radius * (1 + (n - 0.5) * 0.3) * (1 + ch * 0.02);
                        float d = r - R;
                        float edge = (1 - smoothstep(0, _Width * 0.12, d)) * smoothstep(-_Width * 0.25, 0, d);        // 硬い先端
                        float body = smoothstep(-_Width, -_Width * 0.25, d) * (1 - smoothstep(-_Width * 0.25, 0, d));   // 尾
                        float torn = pow(tex2D(_PVoro, float2(a01 * 3, r * 0.6 + _Seed * 0.1)).r * 1.3, 2);                    // 千切れ（セルの板）
                        float bright = pow(saturate(fbm3(cp * 2.2 + _Seed * 2 + 4) * 1.6 - 0.2), 2) * 1.8 + 0.2;
                        k3[ch] = (edge * 2.2 + body * torn * 0.9) * bright + exp(-abs(d) / (_Width * 2.5)) * 0.08;
                    }
                    float hot = (1 - smoothstep(0, _Width * 0.12, abs(r - _Radius * (1 + (n - 0.5) * 0.3))));
                    // 地面の土煙の輪・破片・中心の閃光
                    float dust = groundRing(p, float2(_Origin.x, _Ground), _Radius * 1.15, 0.28, 0.1 + _Radius * 0.15, _Seed) * 0.5;
                    float debris = particles(p, float2(_Origin.x, _Ground), _T, 34, 1.4, 1.2, 0.7, 0.005, 2.0, 1.5708, 2.6, 5) * 1.0;
                    float flash = exp(-r * 9) * _Origin.z * 3 + exp(-r * 2.5) * _Origin.z * 0.4;
                    c = (lerp(_Col.rgb, 1, hot * 0.85) * k3) * _Reach * 1.5 + lerp(_Col.rgb, float3(0.8, 0.7, 0.6), 0.6) * dust * _Reach + lerp(_Col.rgb, 1, 0.4) * debris + lerp(_Col.rgb, 1, 0.7) * flash;
                }
                else if (_Mode < 6.5)
                {
                    // ---- 魔法陣 ----
                    float2 q = (p - _Origin.xy) / _Radius; float r = length(q); float ang = atan2(q.y, q.x);
                    float lw = 0.0075;
                    float k = 0;
                    float rot1 = _T * 0.3 + 0.25 * sway(_T * 0.4, 1) * _Sway.x, rot2 = -_T * 0.45 + 0.2 * sway(_T * 0.55, 6) * _Sway.x, rot3 = _T * 0.15 + 0.15 * sway(_T * 0.3, 9) * _Sway.x;
                    // 輪 7 本（太さと明るさを変える）
                    float rings[7] = { 1.0, 0.965, 0.86, 0.80, 0.62, 0.40, 0.18 };
                    float rw[7] = { 1.6, 0.7, 0.9, 0.7, 1.2, 0.8, 0.6 };
                    for (int j = 0; j < 7; j++) k += line_(r - rings[j], lw * (0.7 + 0.5 * rw[j])) * rw[j];
                    // 文字の帯 1（0.86〜0.965）: 目盛り・点・短い弧をハッシュで並べる。ゆっくり回る
                    {
                        float ra = ang + rot3; float N = 56; float cell = floor(ra / 6.2831853 * N + 0.5); float fa = (frac(ra / 6.2831853 * N + 0.5) - 0.5) / N * 6.2831853;   // 角度のずれ
                        float hA = h1(cell + 100), hB = h1(cell + 200), hC = h1(cell + 300);
                        float rc = 0.912; float wA = 0.012;
                        float blink = 0.55 + 0.45 * step(0.3, h1(cell + floor(_T * 6 + hA * 7)));                        // ルーンがコマ単位で明滅
                        float tick = line_(fa * rc, lw) * (1 - smoothstep(0, 0.003, abs(r - rc) - (0.012 + 0.03 * hA))) * step(0.2, hB) * blink;
                        float dot_ = exp(-(pow(fa * rc, 2) + pow(r - (0.88 + 0.06 * hC), 2)) / (0.0045 * 0.0045)) * step(0.55, hA);
                        float arc = line_(r - (0.885 + 0.05 * hB), lw * 0.8) * (1 - smoothstep(0.25, 0.4, abs(fa * N / 6.2831853))) * step(0.5, hC);
                        k += (tick + dot_ + arc) * 1.2;
                    }
                    // 文字の帯 2（0.62〜0.80）: 逆に回る。目盛りを太く少なく
                    {
                        float ra = ang - rot3 * 1.6; float N = 24; float cell = floor(ra / 6.2831853 * N + 0.5); float fa = (frac(ra / 6.2831853 * N + 0.5) - 0.5) / N * 6.2831853;
                        float hA = h1(cell + 400), hB = h1(cell + 500);
                        float rc = 0.71;
                        float tick = line_(fa * rc, lw * 1.4) * (1 - smoothstep(0, 0.003, abs(r - rc) - (0.02 + 0.05 * hA))) * step(0.3, hB);
                        float bar = line_(r - rc, lw) * (1 - smoothstep(0.1, 0.2, abs(fa * N / 6.2831853))) * step(0.6, hA);
                        k += (tick + bar) * 1.1;
                    }
                    // 六芒星（0.80 に内接。回る）と六角
                    k += line_(trid(q, 0.80, rot2), lw) * 1.0 + line_(trid(q, 0.80, rot2 + 3.14159), lw) * 1.0;
                    float2 q1 = float2(q.x * cos(rot1) - q.y * sin(rot1), q.x * sin(rot1) + q.y * cos(rot1));
                    k += line_(hexd(q1, 0.62), lw) * 0.9;
                    // 歯車の輪（0.40 の輪を 24 の歯で波打たせる）
                    float gear = 0.40 + 0.02 * sign(sin(ang * 24 + rot1 * 4));
                    k += line_(r - gear, lw * 0.9) * 0.9;
                    // 頂点の小円（六芒星の頂点）: 二重円と中の点
                    for (int v = 0; v < 6; v++)
                    {
                        float a = v * 1.0471976 + rot2 + 1.5707963; float2 cpos = float2(cos(a), sin(a)) * 0.80;
                        float dv = length(q - cpos);
                        k += line_(dv - 0.075, lw) * 1.1 + line_(dv - 0.05, lw * 0.7) * 0.6 + exp(-dv * dv / (0.012 * 0.012)) * 1.2;
                    }
                    // 中心の紋: 三重の輪と十字
                    k += line_(r - 0.10, lw * 0.8) * 0.8 + line_(abs(q.x) + step(0.17, abs(q.y)) * 10, lw) * 0.5 + line_(abs(q.y) + step(0.17, abs(q.x)) * 10, lw) * 0.5;
                    // 面のうっすらした光（中心へ向かって濃く）と、外へこぼれる光
                    k += (1 - smoothstep(0.0, 1.0, r)) * 0.06 + exp(-(r - 1) * 7) * step(1, r) * 0.18;
                    float pulse = 0.9 + 0.1 * sin(_T * 5);
                    // 現れ方: _Reach（0→1）で外から内へ描かれていく
                    float draw = smoothstep(_Reach * 1.25, _Reach * 1.25 - 0.25, r);
                    c = lerp(_Col.rgb, _Hot.rgb, saturate(k * 0.4)) * k * pulse * draw * 1.4;
                }
                else
                {
                    // ---- 陣の光（寝かせない板）: 陣の縁（楕円）から細い光の線が立ち上り、光の粒が舞い上がる ----
                    float2 o = float2(_Origin.x, _Ground); float R = _Radius, sq = _Width;   // _Width = 縦の潰し
                    float k = 0;
                    [loop] for (int l = 0; l < 22; l++)
                    {
                        float id = l * 1.9 + 1200; float ph = h1(id) * 6.2831853 + _T * 0.3;
                        float2 base = o + float2(cos(ph) * R, sin(ph) * R * sq);
                        float hgt = 0.25 + 0.6 * h1(id + 1); float flick = 0.6 + 0.6 * gnoise(float2(_T * 6 + id, 1));
                        float dy = p.y - base.y; float up = step(0, dy) * exp(-dy / hgt * 2.2);
                        float bend = sin(dy * 6 + _T * 3 + id) * 0.02 * dy * _Sway.x;                                        // 立ち上る線がゆらぐ
                        k += line_(p.x - base.x - bend, 0.003 + 0.004 * h1(id + 2)) * up * flick * (0.4 + 0.8 * h1(id + 3)) * (sin(ph) > 0 ? back : front) * step(-0.02, dy);
                    }
                    float motes = particles(p, o, _T, 40, 0.28, -0.06, 2.6, 0.005, 0.0, 1.5708, 2.0, 7);
                    // 陣の面の照り返し（主人公の足元がうっすら明るい）
                    float gl = groundLight(p, o, float2(R * 1.1, R * sq * 1.1), _T) * 0.35;
                    c = (lerp(_Col.rgb, _Hot.rgb, 0.4) * k * 1.2 + lerp(_Col.rgb, 1, 0.5) * motes + _Col.rgb * gl) * _Reach;
                }
                return float4(c * _Intensity, 0);
            }
            ENDCG
        }
    }
}
