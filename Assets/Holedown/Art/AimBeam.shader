Shader "Holedown/AimBeam"
{
    // Ice-blue aiming beam drawn as a stepped quad strip: flat pixel bands across the beam, a
    // bright segment that travels from the launcher to the first-contact point, and a stepped
    // tip that fades out. Geometry comes from AimBeam; nothing here is a post effect.
    Properties
    {
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        _BaseColor("Base", Color) = (0.55, 0.83, 1.0, 1.0)
        _PulseColor("Pulse", Color) = (0.90, 0.99, 1.0, 1.0)
        _EdgeColor("Edge", Color) = (0.32, 0.62, 0.95, 1.0)
        _Alpha("Alpha", Range(0,1)) = 0.55
        _EdgeAlpha("Edge alpha", Range(0,1)) = 0.22
        _PixelSteps("Cross bands", Range(1,6)) = 3
        _PulseStrength("Pulse strength", Range(0,2)) = 0.9
        _Sweep("Sweep", Range(0,1)) = 0
        _SweepWidth("Sweep width", Range(0.01,1)) = 0.22
        _TipFade("Tip fade", Range(0.01,0.6)) = 0.10
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
            half4 _BaseColor;
            half4 _PulseColor;
            half4 _EdgeColor;
            float _Alpha;
            float _EdgeAlpha;
            float _PixelSteps;
            float _PulseStrength;
            float _Sweep;
            float _SweepWidth;
            float _TipFade;
            CBUFFER_END
            // A plain mesh renderer has no sprite globals (unity_SpriteProps/unity_SpriteColor),
            // so the vertex stage is written by hand instead of using the sprite helpers.
            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                o.color = input.color * _Color;
                return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float u = saturate(input.uv.x);            // 0 at the launcher, 1 at first contact
                float across = abs(input.uv.y - 0.5) * 2.0; // 0 core, 1 rim
                // Flat bands across the beam keep the pixel look instead of a soft gradient.
                float bands = max(1.0, _PixelSteps);
                float band = floor(across * bands) / max(1.0, bands - 1.0);
                float edge = saturate(band);
                half3 color = lerp(_BaseColor.rgb, _EdgeColor.rgb, edge);
                float alpha = lerp(_Alpha, _EdgeAlpha, edge);
                // The travelling highlight widens the beam towards white as it passes.
                float pulse = saturate(1.0 - abs(u - _Sweep) / max(_SweepWidth, 0.0001));
                pulse = pulse * pulse;
                color = lerp(color, _PulseColor.rgb, saturate(pulse * _PulseStrength));
                alpha = saturate(alpha + pulse * 0.55);
                // The tip dissolves in a few flat steps rather than a smooth gradient.
                float tail = saturate((1.0 - u) / max(_TipFade, 0.0001));
                tail = floor(tail * 3.0) / 3.0;
                return half4(color, alpha * saturate(tail));
            }
            ENDHLSL
        }
    }
}
