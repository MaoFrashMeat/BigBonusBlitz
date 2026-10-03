// ロゴと背景（本人 2026-10-03 の参考動画「SG RUSH」: ロゴの面に虹が流れ、金の縁を光の帯が掃き、後ろは星雲と星）。半透明（アルファ）
// _Mode 0 = ロゴ: _MainTex（金の縁＋白い面）と _Face（面のマスク）。面は色相が斜めに流れる虹。_Shine の位置に白い帯（斜め）。縁は帯で金が光る
// _Mode 1 = 星雲の背景: 焼いた歪みの板を紫・青・桃に塗り、点の板で星。ゆっくり流れる。不透明
Shader "Lab/Logo"
{
    Properties
    {
        _MainTex ("Logo", 2D) = "white" {}
        _Face ("Face mask", 2D) = "black" {}
        _Mode ("Mode", Float) = 0
        _T ("Time", Float) = 0
        _Shine ("Shine pos", Float) = -1
        _Glow ("Rim glow", Float) = 0
        _Alpha ("Alpha", Float) = 1
        _Sky ("Sky (0 nebula, 1 blue sky)", Float) = 0
        _PWarp ("ProcWarp", 2D) = "gray" {}
        _PNoise ("ProcNoise", 2D) = "gray" {}
        _PSparks ("ProcSparks", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest Always Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"
            sampler2D _MainTex, _Face, _PWarp, _PNoise, _PSparks; float _Mode, _T, _Shine, _Glow, _Alpha, _Sky;
            struct appdata { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }
            float3 hsv(float h, float s, float v) { float3 k = saturate(abs(frac(h + float3(0, 0.667, 0.333)) * 6 - 3) - 1); return v * lerp(1, k, s); }
            float4 frag(v2f i):SV_Target
            {
                float2 uv = i.uv;
                if (_Mode < 0.5)
                {
                    float4 t = tex2D(_MainTex, uv); float face = tex2D(_Face, uv).r;
                    // 太い金の縁取り: 周り 8 方向のアルファの最大との差を縁にする（参考の厚い金枠）
                    float o = 0.0075; float dil = t.a;
                    [unroll] for (int k = 0; k < 8; k++) { float a_ = k * 0.7853982; dil = max(dil, tex2D(_MainTex, uv + float2(cos(a_), sin(a_)) * o).a); }
                    [unroll] for (int k2 = 0; k2 < 8; k2++) { float a_ = k2 * 0.7853982 + 0.39; dil = max(dil, tex2D(_MainTex, uv + float2(cos(a_), sin(a_)) * o * 0.6).a); }
                    float outline = saturate(dil - t.a);
                    float3 goldC = float3(1.0, 0.72, 0.25) * (0.7 + 0.5 * sin(uv.x * 40 + uv.y * 25 + _T * 2));   // 金は少しむらに光る
                    // 面の虹: 色相が斜めに流れる（参考では 1 秒で一周弱）。少し波打つ
                    float h = frac((uv.x * 0.7 + uv.y * 0.5) * 2.2 - _T * 0.45 + 0.08 * sin(uv.y * 9 + _T * 2));
                    float3 rainbow = hsv(h, 0.9, 1.0) * (0.75 + 0.5 * t.r);
                    float3 col = lerp(t.rgb, rainbow, face);
                    // 光の帯（斜め）: 白く飛ぶ。縁（金）は帯で強く光る（1 を超えてブルームへ）
                    float d = (uv.x + uv.y * 0.45) - _Shine;
                    float band = exp(-d * d / (0.05 * 0.05)) + exp(-d * d / (0.012 * 0.012)) * 0.8;
                    col += band * (face > 0.5 ? 0.9 : 2.2) * float3(1, 0.97, 0.85);
                    // 縁の脈動（周りの光に合わせて金が明るくなる）
                    col += (1 - face) * t.a * _Glow * float3(1, 0.8, 0.45);
                    // 縁取りを合成（縁は光の帯で白く飛ぶ）
                    col = lerp(col, goldC + band * 2.5 + _Glow * float3(1, 0.8, 0.45), outline * (1 - t.a));
                    float alpha = max(t.a, outline);
                    return float4(col, alpha * _Alpha);
                }
                else
                {
                    // 星雲: 歪みの板を 2 つの速さで流し、紫・青・桃に塗る。暗い所は黒に近く
                    float2 u1 = uv * 1.1 + float2(_T * 0.006, -_T * 0.004), u2 = uv * 0.6 + float2(-_T * 0.003, _T * 0.005) + 0.3;
                    float3 w1 = tex2D(_PWarp, u1).rgb, w2 = tex2D(_PWarp, u2).rgb;
                    float neb = pow(saturate(w1.g * 0.7 + w2.r * 0.7 - 0.45), 2.2);
                    float3 colN = lerp(float3(0.008, 0.004, 0.025), float3(0.3, 0.08, 0.5), neb) + float3(0.03, 0.18, 0.55) * pow(saturate(w2.g * 1.5 - 0.65), 2.5) + float3(0.55, 0.1, 0.35) * pow(saturate(w1.b * 1.5 - 0.7), 2.5);
                    // 星: 点の板を 2 段。細かい方はちらつく
                    float3 sp = tex2D(_PSparks, uv * 2.2 + float2(_T * 0.002, 0)).rgb, sp2 = tex2D(_PSparks, uv * 0.9 + 0.5).rgb;
                    float tw = 0.6 + 0.4 * sin(_T * 7 + uv.x * 400 + uv.y * 230);
                    float3 stars = (saturate(sp.r * 2.5) * 1.4 * tw + sp.g * 0.6) * float3(1, 1, 1) + sp2.g * 0.25 * float3(0.8, 0.9, 1);   // 大きいぼんやりした点（b）は使わない
                    if (_Sky > 0.5)
                    {
                        // 青い空: 上が濃い青、下へ白く。光条が下から差す
                        float3 sky = lerp(float3(0.05, 0.12, 0.38), float3(0.01, 0.02, 0.1), saturate(uv.y * 1.3));
                        float2 d = uv - float2(0.5, 0.15); float ang = atan2(d.y, d.x);
                        float ray = pow(tex2D(_PNoise, float2(ang * 2.5 + _T * 0.01, 0.3)).g, 2.5) * exp(-length(d) * 1.2);
                        colN = sky * (0.55 + 0.4 * neb) + float3(1, 0.95, 0.85) * ray * 0.05;
                        stars *= 0.5;
                    }
                    return float4(colN + stars, 1);
                }
            }
            ENDCG
        }
    }
}
