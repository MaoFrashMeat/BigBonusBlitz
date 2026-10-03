// きらめきと光芒（本人 2026-10-03 の参考動画: ロゴの周りに色とりどりの細かいきらめきが毎コマ瞬き、後ろで白金の光と光芒がゆっくり回る）。加算・HDR
// _Mode 0 = きらめき: 焼いた点の板を 3 段の大きさで読み、セルごとに別の位相で瞬く（15 Hz）。色相はセルのハッシュ（赤・緑・青・白が混ざる）。中心に近いほど密
// _Mode 2 = 回るきらめきの輪（本人 2026-10-03「後ろでサークルに動いているキラキラ。あれだけ再現できれば良い」）: 傾いた楕円の輪に沿ってきらめきが回る。
//            密な弧（彗星の頭）が一周し、手前（下）は大きく明るく、奥（上）は小さく暗い。輪は 3 本（傾き・速さ・半径が違う）。粒は色とりどりで毎コマ瞬く
// _Mode 3 = 光の頭の周回（本人 2026-10-04「もう一個手前の、SG の後ろのやつ。背景ではない」。手ブレを止めた差分で見えた: ロゴの後ろを白金の大きく柔らかい光の頭が
//            楕円の軌道で一周し、尾に小さなきらめきが散る）。_Ring = (半径, 太さ, 傾き, 潰し)、_Spin.x = 周回の速さ、_Spin.y = 頭の数
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
        _PWarp ("ProcWarp", 2D) = "gray" {}
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
            sampler2D _PStreak, _PSparks, _PNoise, _PWarp; float _Mode, _T, _Intensity, _Aspect; float4 _Center, _Col, _Ring, _Spin;
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
                else if (_Mode > 2.5)
                {
                    // 楕円の軌道
                    float ct = cos(_Ring.z), st = sin(_Ring.z); float2 q = float2(p.x * ct + p.y * st, -p.x * st + p.y * ct); q.y /= _Ring.w;
                    float rr = length(q); float ang = atan2(q.y, q.x);
                    float depth = 0.5 - 0.5 * sin(ang);                                     // 1 = 手前（下）
                    float band = exp(-pow((rr - _Ring.x) / _Ring.y, 2));
                    float bandWide = exp(-pow((rr - _Ring.x) / (_Ring.y * 3.0), 2));
                    for (int H = 0; H < 2; H++)
                    {
                        if (H >= (int)_Spin.y) break;
                        float ha = ang - _T * _Spin.x - H * 3.14159;                          // 頭の角度
                        float da = atan2(sin(ha), cos(ha));                                   // -π..π
                        // 頭: 大きく柔らかい白金の玉（角度 ±0.5 rad）。尾: 後ろへ 2 rad ほど薄く伸びる
                        float head = exp(-da * da / (0.5 * 0.5));
                        float tail = (da < 0 ? exp(da * 1.0) : 0) * (1 - head);
                        float3 hot = float3(1, 0.97, 0.9), gold = _Col.rgb;
                        // 頭の玉（半径方向にも広い）
                        float2 hp = float2(cos(_T * _Spin.x + H * 3.14159), sin(_T * _Spin.x + H * 3.14159)) * _Ring.x;
                        float dh = length(q - hp);
                        float blob = exp(-dh * dh / (_Ring.y * _Ring.y * 9)) * 1.6 + exp(-dh / (_Ring.y * 2.2)) * 0.5;
                        c += lerp(gold, hot, saturate(blob)) * blob * (0.6 + 0.6 * depth);
                        // 軌道の芯（頭から尾へ）
                        c += lerp(gold, hot, head) * band * (head * 1.4 + tail * 0.5) * (0.5 + 0.7 * depth);
                        c += gold * bandWide * (head * 0.35 + tail * 0.12) * (0.5 + 0.7 * depth);
                        // 尾のきらめき: 点の板を軌道に沿って読み、毎コマ瞬く
                        float2 tuv = float2(ang / 6.2831853 * 10.0, rr * 1.6 + H * 0.4);
                        float3 sp = tex2D(_PSparks, tuv).rgb;
                        float2 cell = floor(tuv * float2(30, 10) + 0.5); float hc = h2(cell + H * 5.1);
                        float blink = step(0.45, h2(cell + floor(_T * 15 + hc * 9)));
                        float dots = pow(saturate(sp.g * 2.6), 1.6) * 1.3 + pow(saturate(sp.b * 1.8), 2.0) * 1.5 + pow(saturate(sp.r * 3.0), 1.8) * 0.8;
                        c += lerp(hot, hsv(hc, 0.6, 1), 0.5) * dots * bandWide * (head * 2.5 + tail * 4.0) * blink * (0.5 + 0.8 * depth);
                    }
                    c *= _Intensity;
                }
                else if (_Mode < 2.5 && _Mode > 1.5)
                {
                    // きらめきの円盤（本人 2026-10-03「全然違くない？」で作り直し。参考は画面を覆う巨大な円盤: 中心は画面の下（ロゴの奥の光）、
                    // 密な帯が上半分を弧で横切り、帯の中で色が地域ごとに変わる（青緑 → 金 → 桃）。粒は霞と一緒に発光し、全体がゆっくり回る）
                    // 円盤の座標: 中心 _Center（uv）、縦に _Ring.w で潰した楕円。_Ring.x = 帯の半径、_Ring.y = 帯の太さ
                    float2 q = p; q.y /= _Ring.w; float rr = length(q); float ang = atan2(q.y, q.x);
                    float rot = _T * _Spin.x;                                                   // 円盤の回転
                    float a2 = ang + rot;
                    float lr = log(max(rr, 1e-3));
                    // 帯の密度: 主帯（ガウス）＋ 2 本の副帯 ＋ 渦の腕（対数螺旋）
                    float band = exp(-pow((rr - _Ring.x) / _Ring.y, 2)) + 0.25 * exp(-pow((rr - _Ring.x * 1.22) / (_Ring.y * 0.8), 2)) + 0.18 * exp(-pow((rr - _Ring.x * 0.82) / (_Ring.y * 0.7), 2));   // 主帯＋細い副帯 2 本。帯の外は暗く
                    float arms = 0.55 + 0.45 * cos(3.0 * a2 - 4.5 * lr);
                    float arcN = tex2D(_PNoise, float2(a2 / 6.2831853 * 1.5, lr * 0.3 + 0.2)).r;   // 帯の濃淡（大きなむら）
                    float dens = pow(band, 1.4) * (0.4 + 0.6 * arms) * (0.5 + 0.9 * arcN);   // 帯の外では粒を出さない（裾を締める）
                    // 地域の色: 角度でゆっくり色相が変わる（青緑 → 金 → 桃）。密い所は白へ
                    float hue = frac(a2 * 0.55 + lr * 0.35 + _T * 0.015 + tex2D(_PNoise, float2(a2 * 0.3, lr * 0.5)).g * 0.25);   // 弧に沿って青緑 → 金 → 桃
                    float3 region = hsv(hue, 0.95, 1.0);
                    // 霞（粒の間の光）: 歪みの板を極座標で読む。密度に従う
                    float3 mist = tex2D(_PWarp, float2(a2 / 6.2831853 * 3.0 + _T * 0.01, lr * 0.9)).rgb;
                    float haze = pow(saturate(mist.g * 1.3 - 0.2), 1.6) * dens;
                    c += lerp(region, 1, saturate(haze * 0.6 - 0.3)) * haze * 1.0;
                    // 粒: 4 段の点の板を（角度 × log 半径）で読む。縦横比を揃える（角度 1 タイル = 2πr/N、半径 1 タイル = r/k → N = 2πk）
                    float dots = 0;
                    [unroll] for (int L2 = 0; L2 < 4; L2++)
                    {
                        float k = (L2 == 0 ? 2.5 : (L2 == 1 ? 4.5 : (L2 == 2 ? 8.0 : 14.0)));
                        float2 tuv = float2(a2 / 6.2831853 * 6.2831853 * k + _T * _Spin.w * (L2 + 1) * 0.3, lr * k + L2 * 0.37);
                        float3 s_ = tex2D(_PSparks, tuv).rgb;
                        float big = pow(saturate(s_.b * 1.8), 1.7), mid = pow(saturate(s_.g * 2.6), 1.6), fine = pow(saturate(s_.r * 3.2), 1.8);
                        float2 cell = floor(tuv * 6 + 0.5); float hc = h2(cell + L2 * 7.7);
                        float blink = 0.4 + 0.6 * step(0.45, h2(cell + floor(_T * _Spin.z + hc * 9)));
                        dots += (big * 1.6 + mid * 1.1 + fine * 0.7) * blink * (L2 == 0 ? 1.0 : (L2 == 1 ? 0.9 : (L2 == 2 ? 0.7 : 0.5)));
                    }
                    float2 cellC = floor(float2(a2 * 40, lr * 40) + 0.5); float hcol = h2(cellC);
                    float3 dotCol = lerp(region, lerp(hsv(hcol, 0.9, 1), 1, step(0.6, h2(cellC + 2.2))), 0.3);   // 地域の色に、粒ごとの色を半分混ぜる
                    c += dotCol * dots * dens * 5.5;
                    // 帯の内側に白金の光（参考では帯の下、ロゴの周りが白く明るい）
                    c += float3(1, 0.93, 0.75) * exp(-pow((rr - _Ring.x * 0.78) / (_Ring.x * 0.07), 2)) * 0.3 * (0.6 + 0.4 * arcN);   // 帯のすぐ下に暖かい光の筋
                    // 中心の白金の光（円盤の中心 = ロゴの奥）
                    c += _Col.rgb * (exp(-rr * rr * 9) * 1.0 + exp(-rr * 2.5) * 0.12);
                    c *= _Intensity;
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
