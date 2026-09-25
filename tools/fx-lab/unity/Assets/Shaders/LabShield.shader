// 六角格子のシールド。縁の光（フレネル）＋格子＋走る帯＋被弾の波紋。出現は下から格子が埋まる
Shader "Lab/Shield"
{
    Properties
    {
        _Color ("Color", Color) = (0.3,0.8,1,1)
        _Appear ("Appear", Float) = 1
        _HexScale ("HexScale", Float) = 9
        _HitDir0 ("HitDir0", Vector) = (1,0,0,0)
        _HitT0 ("HitT0", Float) = -10
        _HitDir1 ("HitDir1", Vector) = (1,0,0,0)
        _HitT1 ("HitT1", Float) = -10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            float _LabEmit;
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color, _HitDir0, _HitDir1; float _Appear, _HexScale, _HitT0, _HitT1, _LabT;
            struct appdata { float4 pos:POSITION; float3 n:NORMAL; };
            struct v2f { float4 pos:SV_POSITION; float3 n:TEXCOORD0; float3 op:TEXCOORD1; };
            v2f vert(appdata i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.n=UnityObjectToWorldNormal(i.n); o.op=normalize(i.pos.xyz); return o; }
            float hash(float2 p){ return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float4 hex(float2 p)
            {
                const float2 s = float2(1, 1.7320508);
                float4 hc = floor(float4(p, p - float2(0.5, 1)) / s.xyxy) + 0.5;
                float4 h = float4(p - hc.xy * s, p - (hc.zw + 0.5) * s);
                return dot(h.xy, h.xy) < dot(h.zw, h.zw) ? float4(h.xy, hc.xy) : float4(h.zw, hc.zw + 0.5);
            }
            float wave(float3 dir, float3 hitDir, float t0)
            {
                float dt = _LabT - t0;
                if (dt < 0) return 0;
                float ang = acos(clamp(dot(dir, normalize(hitDir)), -1, 1));
                float ring = exp(-pow((ang - dt * 5.0) / 0.16, 2)) * saturate(1 - dt / 0.7);
                float spot = exp(-ang * 5) * saturate(1 - dt / 0.35) * 2.5;
                return ring + spot;
            }
            float4 frag(v2f i, float face:VFACE):SV_Target
            {
                float3 N = normalize(i.n);
                float3 V = normalize(UNITY_MATRIX_V[2].xyz);
                float fres = pow(1 - abs(dot(N, V)), 2.2);
                float3 p = i.op;
                float lon = atan2(p.z, p.x) / 6.2831853;
                float lat = asin(clamp(p.y, -1, 1)) / 3.1415927 + 0.5;
                float4 hx = hex(float2(lon * _HexScale * 2.2, lat * _HexScale));
                float2 q = abs(hx.xy);
                float edgeDist = 0.5 - max(dot(q, normalize(float2(1, 1.7320508))), q.x);
                float edge = 1 - smoothstep(0.0, 0.07, edgeDist);
                float r = hash(hx.zw);
                // 出現: 下から格子が埋まる。先端は明るい
                float front = _Appear * 1.35 - 0.2;
                float cellLat = lat + (r - 0.5) * 0.12;
                float vis = step(cellLat, front);
                float lead = exp(-pow((cellLat - front) / 0.05, 2)) * step(_Appear, 0.999);
                float band = exp(-pow((lat - frac(_LabT * 0.45) * 1.3 + 0.15) / 0.045, 2));
                float twinkle = step(0.9, frac(r * 13.7 + floor(_LabT * 8) * 0.618)) * 0.35;
                float w = wave(p, _HitDir0.xyz, _HitT0) + wave(p, _HitDir1.xyz, _HitT1);
                float I = fres * 1.3 + edge * (0.1 + fres * 1.0 + band * 0.9 + twinkle + w * 1.6) + w * 0.9 + band * 0.12 + 0.03;
                I = I * vis + lead * (0.6 + edge * 2.0);
                I *= face > 0 ? 1 : 0.45;
                return float4(_Color.rgb * I * _LabEmit, 0);
            }
            ENDCG
        }
    }
}
