Shader "BBB/UI/SaliaLayer"
{
    Properties
    {
        [PerRendererData] _MainTex ("Layer", 2D) = "white" {}
        _ClosedTex ("Closed eyes", 2D) = "black" {}
        _SourceTex ("Joined surface colors", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip", Float) = 0
        _MotionTime ("Motion time", Float) = 0
        _Motion ("Breath Hair Cloth Range", Vector) = (.65,.7,.65,1.35)
        _Blink ("Left Right Closed", Vector) = (0,0,0,0)
        _ImageSize ("Canvas", Vector) = (1672,941,0,0)
        _RectSize ("UI Rect", Vector) = (1672,941,0,0)
        _PartClass ("Motion class", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float2 localUv:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float2 localUv:TEXCOORD1; float4 world:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex, _ClosedTex, _SourceTex;
            fixed4 _Color;
            float4 _Motion, _Blink, _ImageSize, _RectSize, _ClipRect;
            float _MotionTime, _PartClass;
            float field(float2 p,float2 c,float2 r) { float2 d=(p-c)/r; return exp(-dot(d,d)*2); }
            float pinBox(float2 p,float2 lo,float2 hi) { float2 d=max(max(lo-p,p-hi),0); return 1-smoothstep(0,30,length(d)); }
            float protectFace(float2 p,float2 c,float2 r) { return 1-smoothstep(1,1.4,length((p-c)/r)); }
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float t=_MotionTime;
                float2 q=float2(v.uv.x,1-v.uv.y)*_ImageSize.xy, p=q;
                float b=sin(t*1.30899694)*_Motion.x;
                p.y-=b*3.8*(1-smoothstep(390,880,q.y));
                float chest=field(q,float2(909,445),float2(140,160));
                p+=float2((q.x-909)*.004,-(q.y-530)*.003)*b*chest;
                // All cuts use the same spatial field, including their backing surface.
                {
                    float l=field(q,float2(477,462),float2(94,146));
                    float low=field(q,float2(716,574),float2(94,130));
                    float r=field(q,float2(1025,157),float2(146,112));
                    float side=field(q,float2(615,225),float2(53,80));
                    float crown=field(q,float2(735,18),float2(50,45));
                    float wind=sin(t*1.65-q.y*.004)+.3*sin(t*2.77+q.x*.007);
                    p.x+=_Motion.y*_Motion.w*wind*(7*l+5*low+7*r+2.5*side+3*crown);
                    p.y+=_Motion.y*_Motion.w*(sin(t*1.65-.8)*3*r+sin(t*1.65+.5)*2.2*l);
                    float face=max(protectFace(q,float2(727,234),float2(36,27)),max(protectFace(q,float2(813,193),float2(36,28)),protectFace(q,float2(792,277),float2(44,27))));
                    float front=(field(q,float2(748,157),float2(83,63))*5.5+field(q,float2(664,241),float2(32,83))*4+field(q,float2(878,234),float2(33,88))*4.5)*(1-face);
                    p+=_Motion.y*_Motion.w*front*float2(sin(t*1.65-q.y*.009-.65),.22*sin(t*1.65-q.y*.009-1.2));
                }
                {
                    float lower=smoothstep(635,900,q.y);
                    float edge=max(1-smoothstep(400,825,q.x),smoothstep(1040,1520,q.x));
                    float skirt=field(q,float2(1004,792),float2(194,116));
                    float wave=sin(t*1.27+q.x*.005-q.y*.004);
                    p+=_Motion.z*_Motion.w*float2(lower*edge*wave*9,lower*edge*wave*12+skirt*sin(t*1.27-.7)*3);
                }
                float hands=max(pinBox(q,float2(430,478),float2(740,725)),pinBox(q,float2(1195,350),float2(1335,485)));
                p=lerp(p,q,hands);
                v.vertex.xy+=(p-q)/_ImageSize.xy*_RectSize.xy*float2(1,-1);
                o.world=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color*_Color; o.uv=v.uv; o.localUv=v.localUv;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                clip(min(min(i.localUv.x,i.localUv.y),min(1-i.localUv.x,1-i.localUv.y)));
                fixed4 c=tex2D(_MainTex,i.localUv);
                c.rgb=tex2D(_SourceTex,i.uv).rgb;
                if (_PartClass<-.5) c.a=smoothstep(.97,.99,c.a);
                fixed4 closed=tex2D(_ClosedTex,i.uv);
                float blink=i.uv.x<.46?_Blink.x:_Blink.y;
                c.rgb=lerp(c.rgb,closed.rgb,closed.a*blink);
                c*=i.color;
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a-.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
