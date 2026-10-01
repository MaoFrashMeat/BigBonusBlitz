// 手描き風の硬い形（本人 2026-10-01 のリファレンス: Halcyon / Knights of Jadra 系。白い芯＋彩度の高い縁＋暗い輪郭、鋭く尖って尾を引く、コマ落とし）
// 加算の光でなく「不透明の形」。距離関数（SDF）に閾値を切って塗り分ける。縁のギザギザは焼いた板（ridge / voro）を閾値で切る
// _Mode 0 = 打撃の閃光: 中心の体（不規則な多角形）＋鋭い棘（本数・長さ・太さをハッシュで）。_Phase（0..1）で 1 コマずつ形が変わる
//            0.0 小さい点 → 0.15 最大 → 0.35 穴が空く・棘が伸びて体が縮む → 0.6 棘だけ → 0.8 破片 → 1 消える
// _Mode 1 = 輪: 細い輪が広がり、ところどころ千切れる。縁に飛沫
// _Mode 2 = 飛沫: 外へ飛ぶ涙形の滴（根元が太く先が細い）。_Phase で距離と細さ
Shader "Lab/Toon"
{
    Properties
    {
        _Mode ("Mode", Float) = 0
        _Phase ("Phase", Float) = 0
        _Core ("Core (white)", Color) = (1,1,1,1)
        _Rim ("Rim", Color) = (0.2,0.6,1,1)
        _Line ("Line (dark)", Color) = (0.05,0.1,0.3,1)
        _Aspect ("Aspect", Float) = 1
        _Seed ("Seed", Float) = 0
        _Radius ("Radius", Float) = 0.5
        _Spikes ("Spikes", Float) = 9
        _RimW ("Rim width", Float) = 0.035
        _LineW ("Line width", Float) = 0.012
        _Alpha ("Alpha", Float) = 1
        _PRidge ("ProcRidge", 2D) = "gray" {}
        _PVoro ("ProcVoro", 2D) = "gray" {}
        _PNoise ("ProcNoise", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest Always Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"
            sampler2D _PRidge, _PVoro, _PNoise;
            float4 _Core, _Rim, _Line; float _Mode, _Phase, _Aspect, _Seed, _Radius, _Spikes, _RimW, _LineW, _Alpha;
            struct appdata { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }
            float h1(float x) { return frac(sin(x * 127.1 + _Seed * 57.3) * 43758.5453); }

            // 打撃の閃光の「外へ出る量」: 角度ごとの棘（鋭い三角）＋体の不規則さ
            float burstR(float ang, float ph, float rr)
            {
                float N = _Spikes;
                float x = ang / 6.2831853 * N + 0.5; float cell = floor(x);
                float hA = h1(cell), hB = h1(cell + 7.7), hC = h1(cell + 19.1), hD = h1(cell + 31.3), hE = h1(cell + 43.9);
                // 曲がり: 外へ行くほど角度がずれる（手描きの「しなる」棘）
                float bend = (hD - 0.5) * 0.9 * pow(saturate(rr / _Radius), 1.5);
                float f = frac(x + bend) - 0.5;
                // 棘: 左右非対称の三角（片側が膨らむ）。長さの差を大きく（3 本に 1 本が長い）。太さもばらす
                float len = (hA > 0.74 ? 1.3 + 0.7 * hB : (hA > 0.35 ? 0.35 + 0.35 * hB : 0.1 + 0.15 * hB));   // 8 本に 2 本だけ長い
                float thin = ph > 0.35 ? 1 - 0.9 * saturate((ph - 0.35) / 0.5) : 1;
                float shortFade = (hA > 0.74) ? 1 : 1 - saturate((ph - 0.3) / 0.15);          // 散りでは短い棘が先に消え、長い棘だけが裂片として残る
                float wl = (0.09 + 0.2 * hC) * thin, wr = (0.09 + 0.2 * hE) * thin;
                float off = (hC - 0.5) * 0.3;
                float d = f - off; float spike = saturate(1 - (d < 0 ? -d / wl : d / wr));
                spike = pow(spike, d < 0 ? 2.0 : 2.6) * len * shortFade;            // 両側が凹む（爪のように）
                // 位相で変わる: 最大（0.15）から棘が伸び、体が縮む
                float body = 1.0;
                if (ph < 0.15) { float k = ph / 0.15; body = 0.15 + 0.85 * pow(k, 0.5); spike *= pow(k, 0.5) * 0.6; }
                else if (ph < 0.35) { float k = (ph - 0.15) / 0.2; body = 1.0 - 0.45 * k; spike *= 0.7 + 0.5 * k; }
                else if (ph < 0.6) { float k = (ph - 0.35) / 0.25; body = 0.55 * (1 - k) * (1 - k); spike *= 1.2 - 0.2 * k; }
                else { float k = (ph - 0.6) / 0.4; body = 0; spike *= 0.9 * (1 - k) ; }
                // 体の不規則さ（焼いたノイズ板を角度で読む）
                float nb = tex2D(_PNoise, float2(ang / 6.2831853 * 1.5 + _Seed * 0.1, 0.37)).r - 0.5;
                float nb2 = tex2D(_PNoise, float2(ang / 6.2831853 * 4 + _Seed * 0.1, 0.71)).g - 0.5;
                return body * (0.5 + 0.45 * nb + 0.12 * nb2) + spike;
            }

            float4 frag(v2f i):SV_Target
            {
                float2 p = (i.uv - 0.5) * float2(_Aspect, 1) * 2;
                float r = length(p); float ang = atan2(p.y, p.x);
                float sd = 1e5;          // 形の外までの距離（負が内側）。単位は半径 1 = _Radius
                float ph = saturate(_Phase);
                if (_Mode < 0.5)
                {
                    float ax = _Seed * 0.7; float2 e1 = float2(cos(ax), sin(ax)), e2 = float2(-e1.y, e1.x);
                    float2 ps = e1 * dot(p, e1) / 1.55 + e2 * dot(p, e2);                      // 主軸で 1.55 倍に伸びた形
                    float rs = length(ps); float angs = atan2(ps.y, ps.x);
                    float R = burstR(angs, ph, rs) * _Radius;
                    sd = (rs - R) * 1.2;
                    // 2 つ目の小さい閃光を少しずらして重ねる（手描きの非対称）
                    float2 p2 = ps - e1 * 0.45 * _Radius; float r2 = length(p2); float ang2 = atan2(p2.y, p2.x) + 1.3;
                    float R2 = burstR(ang2 + 2.1, ph, r2) * _Radius * 0.55;
                    sd = min(sd, (r2 - R2) * 1.2);
                    // 穴は使わない（花に見えた）。散りは体が縮んで棘だけが細く残る
                    if (false)
                    {
                        float hole = tex2D(_PVoro, p * 0.9 + _Seed * 0.3).r;          // セルの中心ほど小さい
                        float th = 0.18 + 0.5 * saturate((ph - 0.3) / 0.4);
                        float inside = step(r, _Radius * 0.6);                           // 穴は体の中だけ
                        if (hole < th && inside > 0) sd = max(sd, th - hole) ;          // 穴の中は外扱い（縁も付く）
                    }
                    // 破片: 0.6 以降、体の外に小さい滴を散らす
                    if (ph > 0.55)
                    {
                        float k = (ph - 0.55) / 0.45;
                        for (int j = 0; j < 7; j++)
                        {
                            float a0 = h1(j * 3.3 + 50) * 6.2831853; float dist = _Radius * (0.5 + 0.9 * k) * (0.7 + 0.6 * h1(j + 60));
                            float2 c0 = float2(cos(a0), sin(a0)) * dist;
                            float2 d = p - c0; float2 dir = float2(cos(a0), sin(a0));
                            float along = dot(d, dir), perp = dot(d, float2(-dir.y, dir.x));
                            float L = _Radius * 0.16 * (1 - k), Wd = _Radius * 0.05 * (1 - k);
                            float tear = length(float2(perp / Wd, (along + L * 0.3) / L)) - (along > 0 ? 1 - along / L : 1);   // 涙形
                            sd = min(sd, tear * Wd);
                        }
                    }
                }
                else if (_Mode < 1.5)
                {
                    // 輪: 半径 _Radius・太さ _RimW。千切れは稜線の板を閾値で
                    float torn = tex2D(_PRidge, float2(ang / 6.2831853 * 3 + _Seed, ph * 0.2)).g;
                    float gap = step(0.25 + 0.5 * ph, torn);                                 // 位相が進むほど千切れる
                    sd = abs(r - _Radius) - _RimW * (0.5 + 0.8 * torn) * gap - (1 - gap) * 10;
                }
                else
                {
                    // 飛沫: 12 滴
                    for (int j = 0; j < 9; j++)
                    {
                        float a0 = h1(j * 2.1 + 90) * 6.2831853; float sp = 0.6 + 0.8 * h1(j + 100);
                        float dist = _Radius * (0.75 + ph * 1.6 * sp);                      // 中心に寄ると花に見える → 体の外から出す
                        float2 dir = float2(cos(a0), sin(a0)); float2 d = p - dir * dist;
                        float along = dot(d, dir), perp = dot(d, float2(-dir.y, dir.x));
                        float L = _Radius * (0.05 + 0.16 * pow(h1(j + 110), 2)) * (1 - ph * 0.5), Wd = _Radius * (0.012 + 0.02 * h1(j + 120)) * (1 - ph * 0.6);
                        float tear = length(float2(perp / Wd, (along + L * 0.3) / L)) - (along > 0 ? 1 - along / L : 1);
                        sd = min(sd, tear * Wd);
                    }
                }
                // 塗り分け: 内側は白い芯、縁は色、その外に暗い輪郭。縁は硬い（アンチエイリアスだけ）
                float aa = fwidth(sd) * 1.2;
                float inCore = 1 - smoothstep(-_RimW - aa, -_RimW + aa, sd);
                float inRim1 = 1 - smoothstep(-_RimW * 0.45 - aa, -_RimW * 0.45 + aa, sd);     // 淡い縁
                float inRim = 1 - smoothstep(-aa, aa, sd);                                      // 濃い縁
                float inLine = 1 - smoothstep(_LineW - aa, _LineW + aa, sd);
                float glow = (1 - smoothstep(_LineW, _LineW + 0.1, sd)) * 0.22;                 // 外の薄い光
                float3 col = _Rim.rgb; float a = max(inLine, glow);
                col = lerp(col, _Line.rgb, inLine * (1 - inRim));
                col = lerp(col, _Rim.rgb, inRim); col = lerp(col, lerp(_Rim.rgb, _Core.rgb, 0.6), inRim1); col = lerp(col, _Core.rgb, inCore);
                return float4(col, a * _Alpha);
            }
            ENDCG
        }
    }
}
