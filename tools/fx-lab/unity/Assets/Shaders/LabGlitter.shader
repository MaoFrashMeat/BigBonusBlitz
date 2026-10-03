// きらめきと光芒（本人 2026-10-03 の参考動画: ロゴの周りに色とりどりの細かいきらめきが毎コマ瞬き、後ろで白金の光と光芒がゆっくり回る）。加算・HDR
// _Mode 0 = きらめき: 焼いた点の板を 3 段の大きさで読み、セルごとに別の位相で瞬く（15 Hz）。色相はセルのハッシュ（赤・緑・青・白が混ざる）。中心に近いほど密
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
            sampler2D _PStreak, _PSparks, _PNoise; float _Mode, _T, _Intensity, _Aspect; float4 _Center, _Col;
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
