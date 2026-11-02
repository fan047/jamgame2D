Shader "Holedown/SpriteFlash"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        _FlashAmount("Flash", Range(0,1)) = 0
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
            float _FlashAmount;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings o = CommonUnlitVertex(input); o.color = input.color * _Color * unity_SpriteColor; return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 c = CommonUnlitFragment(input, input.color);
                c.rgb = lerp(c.rgb, half3(1,1,1), saturate(_FlashAmount)); return c;
            }
            ENDHLSL
        }
    }
}
