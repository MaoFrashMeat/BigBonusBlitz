Shader "BBB/UI/AdventureSeamless"
{
    Properties
    {
        [PerRendererData] _MainTex("Atlas",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        _Row("Row start / height / top feather / bottom feather",Vector)=(0,1,.065,0)
        _Overlap("Horizontal overlap",Range(.05,.3))=.16
        _SeamTex("Content seam lookup (linear data)",2D)="gray"{}
        _UseSeam("Use content seam",Float)=0
        _SeamFeather("Seam half width",Float)=.012
        _Module("Black matte module (no overlap)",Float)=0
        _Ground("Procedural continuous ground",Float)=0
        _Scroll("Ground travel",Float)=0
        _StencilComp("Stencil Comparison",Float)=8
        _Stencil("Stencil ID",Float)=0
        _StencilOp("Stencil Operation",Float)=0
        _StencilWriteMask("Stencil Write Mask",Float)=255
        _StencilReadMask("Stencil Read Mask",Float)=255
        _ColorMask("Color Mask",Float)=15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Alpha Clip",Float)=0
    }
    SubShader
    {
        Tags{"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane"}
        Stencil{Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
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
            struct appdata{float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct v2f{float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float4 local:TEXCOORD1;};
            sampler2D _MainTex, _SeamTex;fixed4 _Color;float4 _Row,_ClipRect,_MainTex_TexelSize;float _Overlap,_UseSeam,_SeamFeather,_Module,_Ground,_Scroll;
            float3 KeyRGB(float2 uv)
            {
                float3 rgb=tex2D(_MainTex,uv).rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                rgb=LinearToGammaSpace(rgb);
                #endif
                return rgb;
            }
            float KeyValue(float2 uv){float3 c=KeyRGB(uv);return max(c.r,max(c.g,c.b));}
            v2f vert(appdata v){v2f o;o.local=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c;
                if(_Ground>.5)
                {
                    // Periodic flat paint facets, continuous in x; no bitmap join or mirrored rocks.
                    float x=i.uv.x*12+_Scroll;
                    float y=i.uv.y;
                    float2 cell=floor(float2(x+y*.8,y*6));
                    float seed=frac(sin(dot(float2(fmod(cell.x+1200,12),cell.y),float2(12.9898,78.233)))*43758.5453);
                    float edge=.91+.035*sin(x*6.2831853)+.018*sin(x*18.849556);
                    float band=step(edge,y);
                    float shade=.82+seed*.28;
                    c=i.color*fixed4(shade,shade,shade,1);
                    c.rgb=lerp(c.rgb,c.rgb*float3(.50,.67,.55),band);
                }
                else if(_Module>.5)
                {
                    // Modules end naturally in black empty space. Decode matte before tinting.
                    // No other image is mixed into this silhouette at the periodic boundary.
                    float x=frac(i.uv.x);
                    fixed4 texel=tex2D(_MainTex,float2(x,i.uv.y));
                    float3 keyColor=texel.rgb;
                    #ifndef UNITY_COLORSPACE_GAMMA
                    keyColor=LinearToGammaSpace(keyColor);
                    #endif
                    float value=max(keyColor.r,max(keyColor.g,keyColor.b));
                    float alpha=smoothstep(.015,.10,value)*texel.a;
                    float2 dx=float2(_MainTex_TexelSize.x,0),dy=float2(0,_MainTex_TexelSize.y);
                    float k0=KeyValue(float2(x,i.uv.y)+dx),k1=KeyValue(float2(x,i.uv.y)-dx);
                    float k2=KeyValue(float2(x,i.uv.y)+dy),k3=KeyValue(float2(x,i.uv.y)-dy);
                    // Correct black antialias fringes only immediately next to empty matte.
                    if(min(min(k0,k1),min(k2,k3))<.02)
                        alpha=min(alpha,value/max(max(max(k0,k1),max(k2,k3)),.10));
                    float guard=smoothstep(0,.02,min(x,1-x));
                    float3 unmatte=saturate(keyColor/max(alpha,.001));
                    #ifndef UNITY_COLORSPACE_GAMMA
                    unmatte=GammaToLinearSpace(unmatte);
                    #endif
                    c=fixed4(unmatte,alpha*guard)*i.color;
                }
                else
                {
                // Legacy atlas overlap is retained for stages not yet converted.
                float span=1-_Overlap;float x=frac(i.uv.x)*span;
                fixed4 a=tex2D(_MainTex,float2(x,i.uv.y));
                fixed4 b=tex2D(_MainTex,float2(min(1,x+span),i.uv.y));
                float blend=smoothstep(0,_Overlap,x);
                // The editor finds a low-error route through the overlapping artwork.
                // A narrow, curved join preserves solid landmarks instead of ghosting a wide strip.
                float seam=tex2D(_SeamTex,float2(.5,i.uv.y)).r*_Overlap;
                blend=lerp(blend,smoothstep(seam-_SeamFeather,seam+_SeamFeather,x),_UseSeam);
                float alpha=lerp(b.a,a.a,blend);
                float3 premul=lerp(b.rgb*b.a,a.rgb*a.a,blend);
                c=fixed4(premul/max(alpha,.0001),alpha)*i.color;
                float y=saturate((i.uv.y-_Row.x)/_Row.y);
                c.a*=1-smoothstep(1-_Row.z,1,y);
                if(_Row.w>0)c.a*=smoothstep(0,_Row.w,y);
                }
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
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
