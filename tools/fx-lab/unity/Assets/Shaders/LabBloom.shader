// 0: 抽出（しきい値＋ソフトニー） 1: 縮小 2: 拡大して加算 3: 合成（揺れ・ズーム・方向ブラー・流線/集中線・ネガ・白フラッシュ・トーンマップ）
Shader "Hidden/LabBloom"
{
    Properties { _MainTex ("Tex", 2D) = "black" {} }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex; float4 _MainTex_TexelSize;
    sampler2D _BloomTex;
    float _Threshold, _Knee, _BloomIntensity, _Exposure;
    float4 _Shake;          // xy: uv オフセット z: 回転(rad)
    float _Zoom; float4 _ZoomCenter;
    float4 _BlurDir; float _BlurAmt;
    float _Lines, _LinesMode, _LinesSeed, _LinesAngle, _LinesDensity; float4 _LinesCenter; float4 _LinesColor;
    float _Darken, _Invert, _Mono, _Flash; float4 _Tint;
    float _Dim, _Contrast, _Sat;   // 背景を沈める量（0〜1。光っていない所だけ）、表示のコントラストと彩度
    struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
    v2f vert(appdata_img v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }
    float3 box4(float2 uv, float d)
    {
        float4 o = _MainTex_TexelSize.xyxy * float4(-d,-d,d,d);
        return 0.25 * (tex2D(_MainTex, uv+o.xy).rgb + tex2D(_MainTex, uv+o.zy).rgb + tex2D(_MainTex, uv+o.xw).rgb + tex2D(_MainTex, uv+o.zw).rgb);
    }
    float3 tent9(float2 uv)
    {
        float4 d = _MainTex_TexelSize.xyxy * float4(1,1,-1,0);
        float3 s = tex2D(_MainTex, uv - d.xy).rgb;
        s += tex2D(_MainTex, uv - d.wy).rgb * 2;
        s += tex2D(_MainTex, uv - d.zy).rgb;
        s += tex2D(_MainTex, uv + d.zw).rgb * 2;
        s += tex2D(_MainTex, uv).rgb * 4;
        s += tex2D(_MainTex, uv + d.xw).rgb * 2;
        s += tex2D(_MainTex, uv + d.zy).rgb;
        s += tex2D(_MainTex, uv + d.wy).rgb * 2;
        s += tex2D(_MainTex, uv + d.xy).rgb;
        return s / 16;
    }
    float3 aces(float3 x){ return saturate(x*(2.51*x+0.03)/(x*(2.43*x+0.59)+0.14)); }
    float hash(float n){ return frac(sin(n) * 43758.5453); }
    float3 scene(float2 uv){ return tex2D(_MainTex, uv).rgb + tex2D(_BloomTex, uv).rgb * _BloomIntensity; }
    float3 sceneLod(float2 uv){ return tex2Dlod(_MainTex, float4(uv, 0, 0)).rgb + tex2Dlod(_BloomTex, float4(uv, 0, 0)).rgb * _BloomIntensity; }

    // 平行の流線: 行ごとに乱数で長さ・位置・太さを決め、紡錘形に描く
    float linesPar(float2 uv)
    {
        float aspect = _MainTex_TexelSize.w / _MainTex_TexelSize.z;
        float2 p = (uv - 0.5) * float2(1 / aspect, 1);
        float ca = cos(_LinesAngle), sa = sin(_LinesAngle);
        float2 q = float2(ca * p.x + sa * p.y, -sa * p.x + ca * p.y);
        float rows = 120;
        float yy = (q.y + 1.5) * rows;
        float r = floor(yy), fy = frac(yy);
        float sd = _LinesSeed * 7.31;
        float h1 = hash(r * 1.37 + sd), h2 = hash(r * 2.71 + sd * 1.3), h3 = hash(r * 5.13 + sd * 0.7), h4 = hash(r * 7.77 + sd * 2.1);
        if (h1 > _LinesDensity) return 0;
        float len = 0.25 + h3 * 0.9;
        float start = -1.2 + h4 * 2.2;
        float s = (q.x - start) / len;
        if (s < 0 || s > 1) return 0;
        float w = (0.12 + h2 * 0.5) * sin(3.14159 * s);
        float d = abs(fy - 0.5) * 2;
        return smoothstep(w, w * 0.5, d) * (0.55 + 0.45 * h2);
    }
    // 集中線: 角度ごとに楔を描く。中心に向かって細くなる
    float linesRad(float2 uv)
    {
        float aspect = _MainTex_TexelSize.w / _MainTex_TexelSize.z;
        float2 p = (uv - _LinesCenter.xy) * float2(1 / aspect, 1);
        float rr = length(p);
        float a = atan2(p.y, p.x) / 6.2831853 + 0.5;
        float M = 170;
        float k = floor(a * M), f = frac(a * M);
        float sd = _LinesSeed * 3.17;
        float h1 = hash(k * 1.91 + sd), h2 = hash(k * 3.33 + sd * 1.7), h3 = hash(k * 4.47 + sd * 0.3);
        if (h1 > _LinesDensity) return 0;
        float r0 = 0.22 + h2 * 0.3;
        if (rr < r0) return 0;
        float grow = saturate((rr - r0) / 0.4);
        float w = (0.1 + h3 * 0.4) * grow;
        float d = abs(f - 0.5) * 2;
        return smoothstep(w, w * 0.5, d);
    }
    ENDCG
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment f
            float4 f(v2f i):SV_Target
            {
                float3 c = box4(i.uv, 1);
                float br = max(c.r, max(c.g, c.b));
                float rq = clamp(br - _Threshold + _Knee, 0, 2*_Knee);
                rq = rq*rq / (4*_Knee + 1e-4);
                float w = max(rq, br - _Threshold) / max(br, 1e-4);
                return float4(c * w, 1);
            }
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment f
            float4 f(v2f i):SV_Target { return float4(box4(i.uv, 1), 1); }
            ENDCG }
        Pass { Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment f
            float4 f(v2f i):SV_Target { return float4(tent9(i.uv), 1); }
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment f
            float4 f(v2f i):SV_Target
            {
                float aspect = _MainTex_TexelSize.w / _MainTex_TexelSize.z;
                float zoom = _Zoom > 0 ? _Zoom : 1;
                float2 zc = _ZoomCenter.z > 0 ? _ZoomCenter.xy : float2(0.5, 0.5);
                float2 uv = (i.uv - zc) / zoom + zc;
                // 回転は縦横比を戻してから
                float2 pc = (uv - 0.5) * float2(1, aspect);
                float cr = cos(_Shake.z), sr = sin(_Shake.z);
                pc = float2(cr * pc.x - sr * pc.y, sr * pc.x + cr * pc.y);
                uv = pc / float2(1, aspect) + 0.5 + _Shake.xy;
                float3 c = tex2D(_MainTex, uv).rgb;
                // メリハリ: 強い一撃のときは、光っていない所（背景・キャラ）だけ暗く沈める。光（HDR 1 以上）とブルームはそのまま
                float lum0 = dot(c, float3(0.2126, 0.7152, 0.0722));
                c *= lerp(1, 1 - _Dim, 1 - smoothstep(0.7, 1.6, lum0));
                c += tex2D(_BloomTex, uv).rgb * _BloomIntensity;
                if (_BlurAmt > 0)
                {
                    float3 acc = 0;
                    [unroll] for (int k = 0; k < 16; k++) acc += sceneLod(uv + _BlurDir.xy * (k / 15.0 - 0.5));
                    c = lerp(c, acc / 16, _BlurAmt);
                }
                float2 q = i.uv - 0.5;
                c *= 1 - dot(q, q) * 0.7;
                // 色を残すトーンマップ: 明るさだけを ACES で縮め、色味は保つ（光が真っ白に飛ばない）。普通の ACES と 6:4 で混ぜる
                float3 x = c * (_Exposure > 0 ? _Exposure : 1);
                float Lx = dot(x, float3(0.2126, 0.7152, 0.0722));
                float3 hp = x * (aces(Lx.xxx).x / max(Lx, 1e-4));
                hp /= max(1, max(hp.r, max(hp.g, hp.b)));
                c = lerp(aces(x), hp, 0.6);
                // ここからは表示の明るさで扱う
                float3 g = pow(max(c, 0), 1 / 2.2);
                g *= lerp(1, 1 - saturate(length(q * float2(1.3, 1)) * 1.4), _Darken);
                if (_Lines > 0)
                {
                    float L = _LinesMode < 0.5 ? linesPar(i.uv) : linesRad(i.uv);
                    g = lerp(g, _LinesColor.rgb, saturate(L * _Lines));
                }
                if (_Tint.a > 0) g = lerp(g, g * _Tint.rgb, _Tint.a);
                // コントラスト（中間 0.45 を軸に）と彩度
                float lg = dot(g, float3(0.299, 0.587, 0.114));
                g = lerp(lg.xxx, g, _Sat > 0 ? _Sat : 1);
                g = saturate((g - 0.45) * (_Contrast > 0 ? _Contrast : 1) + 0.45);
                float lum = dot(g, float3(0.299, 0.587, 0.114));
                float mono = saturate((lum - 0.5) * 2.2 + 0.5);
                g = lerp(g, mono.xxx, _Mono);
                g = lerp(g, 1 - g, _Invert);
                g = lerp(g, 1, _Flash * 0.6);   // 白フラッシュは控えめ（強さは光と暗転で出す）
                return float4(pow(saturate(g), 2.2), 1);
            }
            ENDCG }
    }
}
