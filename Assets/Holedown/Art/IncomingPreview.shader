Shader "Holedown/IncomingPreview"
{
    // Display-only look for the row that waits below the floor line: the same block
    // silhouette, softened and faded towards the bottom, with no health or role colour.
    // Strength 0 outputs the plain sprite again, so a settling block can swap to its
    // normal material on the last frame without a visible change.
    Properties
    {
        [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        _Strength("Preview strength", Range(0,1)) = 1
        _TopAlpha("Top alpha", Range(0,1)) = 0.62
        _BottomAlpha("Bottom alpha", Range(0,1)) = 0.0
        _BottomShade("Bottom brightness", Range(0,1)) = 0.45
        _SoftenPixels("Edge soften (uv)", Range(0,0.25)) = 0.05
        _BlurGrowth("Downward blur growth", Range(0,4)) = 3
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma multi_compile_instancing
            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings { COMMON_2D_OUTPUTS half4 color : COLOR; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Strength;
            float _TopAlpha;
            float _BottomAlpha;
            float _BottomShade;
            float _SoftenPixels;
            float _BlurGrowth;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings o = CommonUnlitVertex(input); o.color = input.color * _Color * unity_SpriteColor; return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float style = saturate(_Strength);
                // The lower edge of the silhouette is softer and dimmer than the edge that
                // faces the board, so the row dissolves into the cave floor.
                float top = pow(saturate(input.uv.y), 0.8);
                float fall = 1.0 - top;
                float radius = _SoftenPixels * (1.0 + _BlurGrowth * fall) * style;
                float2 offset = float2(radius, radius);
                half4 center = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 blurred = center * 0.4
                    + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(offset.x, 0)) * 0.15
                    + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(offset.x, 0)) * 0.15
                    + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(0, offset.y)) * 0.15
                    + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(0, offset.y)) * 0.15;
                half4 tex = lerp(center, blurred, style);
                half4 c = tex * input.color;
                c.a *= lerp(1.0, lerp(_BottomAlpha, _TopAlpha, top), style);
                c.rgb *= lerp(1.0, lerp(_BottomShade, 1.0, top), style);
                return c;
            }
            ENDHLSL
        }
    }
}
