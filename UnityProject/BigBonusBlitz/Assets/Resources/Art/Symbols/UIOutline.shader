// 図柄のシルエットの縁取り。図柄と同じ絵を子の Image に重ね、絵の外側だけを 1 色で塗る（内側は透明）。
// _WidthUV = 縁の太さ（UV。px / 表示幅）。16 方向にずらして alpha を拾い、「近くに絵があるが自分は透明」の所を縁にする。
// 色と濃さは Image.color（頂点色）。_SrcBlend / _DstBlend で通常 / 加算（光らせるとき）。RectMask2D の切り抜きに対応。
Shader "BBB/UI/Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _WidthUV ("Width (UV)", Float) = 0.02
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend [_SrcBlend] [_DstBlend]
        ColorMask [_ColorMask]
        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; float4 worldPosition : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            sampler2D _MainTex; fixed4 _Color; float4 _ClipRect; float4 _MainTex_ST; float4 _MainTex_TexelSize; float _WidthUV; float _SrcBlend;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float a = tex2D(_MainTex, uv).a;
                // 縦横比: 絵の幅に対する太さを UV で持つので、縦は texel の比で合わせる
                float2 w = float2(_WidthUV, _WidthUV * _MainTex_TexelSize.x / _MainTex_TexelSize.y);
                float m = 0;
                // 16 方向（2 つの輪: 太さ 100% と 50%）
                float2 d[8] = { float2(1,0), float2(-1,0), float2(0,1), float2(0,-1), float2(.707,.707), float2(-.707,.707), float2(.707,-.707), float2(-.707,-.707) };
                for (int i = 0; i < 8; i++) { m = max(m, tex2D(_MainTex, uv + d[i] * w).a); m = max(m, tex2D(_MainTex, uv + d[i] * w * 0.5).a); }
                float edge = saturate(m - a);            // 近くに絵があって自分は透明 → 縁
                edge = smoothstep(0.15, 0.6, edge);
                half4 color = half4(IN.color.rgb, IN.color.a * edge);
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                if (_SrcBlend == 1) color.rgb *= color.a;   // 加算のときは透明 = 何も足さない
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
