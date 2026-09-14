Shader "BBB/UI/AdventureSeamless"
{
    Properties
    {
        [PerRendererData] _MainTex("Atlas",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        _Row("Row start / height / top feather / bottom feather",Vector)=(0,1,.065,0)
        _Overlap("Horizontal overlap",Range(.05,.3))=.16
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
            sampler2D _MainTex;fixed4 _Color;float4 _Row,_ClipRect;float _Overlap;
            v2f vert(appdata v){v2f o;o.local=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                // Overlap the tail of one panorama with the start of the next. No mirrored architecture.
                float span=1-_Overlap;float x=frac(i.uv.x)*span;
                fixed4 a=tex2D(_MainTex,float2(x,i.uv.y));
                fixed4 b=tex2D(_MainTex,float2(min(1,x+span),i.uv.y));
                float blend=smoothstep(0,_Overlap,x);
                float alpha=lerp(b.a,a.a,blend);
                float3 premul=lerp(b.rgb*b.a,a.rgb*a.a,blend);
                fixed4 c=fixed4(premul/max(alpha,.0001),alpha)*i.color;
                float y=saturate((i.uv.y-_Row.x)/_Row.y);
                c.a*=1-smoothstep(1-_Row.z,1,y);
                if(_Row.w>0)c.a*=smoothstep(0,_Row.w,y);
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
