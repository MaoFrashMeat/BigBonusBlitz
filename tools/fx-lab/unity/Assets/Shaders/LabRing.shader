// 強弱のある輪と放射の筋（本人 2026-09-28「描写の強弱がない」）。板 1 枚に式で描く（加算・HDR）
// _Mode 0 = 輪: 太さと明るさを角度ごとにノイズでムラにし、切れ目を入れる。外縁は硬く内側は柔らかい。_Erode で削れて消える
// _Mode 1 = 放射の筋: 筋ごとに長さ・太さ・明るさをばらし、先へ細る。_Grow で伸び、_Erode で細って消える
Shader "Lab/Ring"
{
    Properties
    {
        _NoiseTex ("Noise", 2D) = "gray" {}
        _Mode ("Mode", Float) = 0
        _Col ("Col", Color) = (1,1,1,1)
        _Core ("Core", Color) = (3,3,3,1)
        _R ("Radius", Float) = 0.3
        _W ("Width", Float) = 0.05
        _Grow ("Grow", Float) = 1
        _Erode ("Erode", Float) = 0
        _Seed ("Seed", Float) = 0
        _Count ("Count", Float) = 26
        _Intensity ("Intensity", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent+2" "RenderType"="Transparent" }
        Blend One One ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _NoiseTex; float4 _Col, _Core;
            float _Mode, _R, _W, _Grow, _Erode, _Seed, _Count, _Intensity;
            struct appdata { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }
            float hash(float x) { return frac(sin(x * 127.1 + _Seed * 311.7) * 43758.5453); }
            float4 frag(v2f i):SV_Target
            {
                float2 p = i.uv - 0.5;
                float r = length(p) * 2;                  // 0 中心 … 1 板の縁
                float ang = atan2(p.y, p.x) / 6.2831853 + 0.5;
                float3 c = 0;
                if (_Mode < 0.5)
                {
                    // 角度ごとのムラ（周回で継ぎ目なし: ノイズを円周に沿って読む）
                    float2 cp = float2(cos(ang * 6.2831853), sin(ang * 6.2831853));
                    // ノイズは低い周波数で読み、差を 2.2 倍に広げる（高い周波数だと数珠つなぎに見える）
                    float n = saturate((tex2D(_NoiseTex, cp * 0.1 + _Seed).r - 0.5) * 2.2 + 0.5);   // 一周で 2〜3 回の大きなうねり
                    float n2 = saturate((tex2D(_NoiseTex, cp * 0.22 + _Seed * 1.7).r - 0.5) * 2.2 + 0.5);
                    float w = _W * (0.4 + 1.1 * n);                         // 太い所・細い所（0.4〜1.5 倍）
                    float d = r - _R * 2;                                    // 輪の外縁からの距離（内側が負）
                    float outer = 1 - smoothstep(0, 0.01, d);               // 外縁は硬く
                    float inner = smoothstep(-w * 2, 0, d);                  // 内側は柔らかく
                    float band = outer * inner * step(-w * 2.5, d);
                    float gap = smoothstep(0.28, 0.42, n2);                  // 切れ目（弧の 2〜4 割が欠ける）
                    float er = smoothstep(_Erode - 0.05, _Erode + 0.05, n * 0.6 + n2 * 0.4);   // 削れて消える
                    float edgeHot = smoothstep(-w * 0.5, 0, d) * outer;      // 外縁ほど熱い
                    float k = band * gap * er * (0.35 + 0.9 * n2);           // 明るさのムラ
                    float innerCore = smoothstep(-w * 1.2, -w * 0.4, d) * (1 - smoothstep(-w * 0.4, 0, d));
                    c = lerp(_Col.rgb, _Core.rgb, saturate(edgeHot * n + innerCore * 0.6)) * k;   // 内側に白い芯、外は色
                }
                else
                {
                    // 放射の筋: 角度を _Count 本に割り、筋ごとにばらす
                    float cell = floor(ang * _Count), f = frac(ang * _Count) - 0.5;
                    float h1 = hash(cell), h2 = hash(cell + 17.3), h3 = hash(cell + 41.9);
                    float len = (0.35 + 0.65 * h1) * _Grow;                 // 長い筋・短い筋
                    float wid = (0.08 + 0.3 * h2 * h2) * (1 - _Erode);      // 太い筋・細い筋
                    float s = r / max(len, 1e-3);
                    float taper = wid * (1 - s);                             // 先へ細る
                    float k = (1 - smoothstep(taper * 0.6, taper, abs(f))) * step(s, 1) * smoothstep(0.02, 0.12, r);
                    float bright = 0.4 + 0.9 * h3;
                    c = lerp(_Col.rgb, _Core.rgb, saturate((1 - s) * h3)) * k * bright;
                }
                return float4(c * _Intensity, 0);
            }
            ENDCG
        }
    }
}
