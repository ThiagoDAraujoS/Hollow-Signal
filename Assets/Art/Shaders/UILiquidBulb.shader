Shader "UI/LiquidBulb"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture (Optional)", 2D) = "white" {}
        [MainColor] [HDR] _LiquidColor ("Liquid Color (HDR)", Color) = (0.2, 0.8, 1.0, 0.85)
        [HDR] _SurfaceColor ("Surface Meniscus Color (HDR)", Color) = (0.8, 1.0, 1.0, 1.0)
        _FillAmount ("Fill Amount", Range(0.0, 1.0)) = 1.0
        _SurfaceThickness ("Surface Thickness", Range(0.001, 0.1)) = 0.02
        _EdgeSmoothness ("Fill Softness", Range(0.001, 0.05)) = 0.005
        _BulbCurvature ("Bulb Rim Darkening", Range(0.0, 1.0)) = 0.35

        [Header(Blend State)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 5 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 10 // OneMinusSrcAlpha
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0 // Off
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test Mode", Float) = 4 // LEqual
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Cull [_Cull]
        ZWrite Off
        ZTest [_ZTest]
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _LiquidColor;
                float4 _SurfaceColor;
                float  _FillAmount;
                float  _SurfaceThickness;
                float  _EdgeSmoothness;
                float  _BulbCurvature;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                // Vertical liquid cutoff with smooth micro-edge
                float fillDiff = _FillAmount - input.uv.y;
                float liquidAlpha = saturate(fillDiff / max(_EdgeSmoothness, 0.0001));

                // Glowing meniscus highlight at the top fluid level
                float surfaceMask = smoothstep(_SurfaceThickness, 0.0, abs(fillDiff)) * step(0.002, _FillAmount);

                // Subtle bulb glass cylindrical edge shading (darker towards left/right borders)
                float uCenterDist = abs(input.uv.x - 0.5) * 2.0;
                float curvatureFalloff = lerp(1.0, 1.0 - (uCenterDist * uCenterDist * _BulbCurvature), _BulbCurvature);

                // Blend liquid color with meniscus highlight
                half4 finalColor = lerp(_LiquidColor, _SurfaceColor, surfaceMask);
                finalColor.rgb *= tex.rgb * input.color.rgb * curvatureFalloff;
                finalColor.a *= tex.a * input.color.a * liquidAlpha;

                return finalColor;
            }
            ENDHLSL
        }
    }
}
