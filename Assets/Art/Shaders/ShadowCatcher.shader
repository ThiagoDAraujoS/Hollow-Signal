Shader "HollowSignal/UI/ShadowCatcher"
{
    Properties
    {
        [Header(Shadow Settings)]
        _ShadowColor ("Shadow Tint & Opacity", Color) = (0.05, 0.05, 0.08, 0.75)
        _ShadowStrength ("Shadow Intensity", Range(0.0, 1.0)) = 1.0
        
        [Header(Stylized Falloff)]
        [Toggle(_ENABLE_STYLIZED_SHADOW)] _EnableStylized ("Enable Hard/Stylized Edge", Float) = 0.0
        _ShadowCutoff ("Hard Shadow Threshold", Range(0.0, 1.0)) = 0.5
        _ShadowFeather ("Hard Shadow Feather", Range(0.001, 0.5)) = 0.05

        [Header(Render State)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0 // Default Off (Double-Sided)
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

        LOD 100
        Cull [_Cull]
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Offset -1, -1

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _ENABLE_STYLIZED

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                half3  normalWS     : TEXCOORD1;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord : TEXCOORD3;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ShadowColor;
                half  _ShadowStrength;
                half  _ShadowCutoff;
                half  _ShadowFeather;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = GetShadowCoord(posInputs);
                #endif

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                    float4 shadowCoord = float4(0, 0, 0, 0);
                #endif

                // 1. Evaluate Main Light shadow
                Light mainLight = GetMainLight(shadowCoord);
                half mainShadow = 1.0 - mainLight.shadowAttenuation;

                // 2. Evaluate Additional Lights (Point/Spot) shadows
                half addShadow = 0.0;
                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        #if defined(_ADDITIONAL_LIGHT_SHADOWS)
                            Light light = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                            addShadow = max(addShadow, 1.0 - light.shadowAttenuation);
                        #endif
                    LIGHT_LOOP_END
                #endif

                // Combined shadow factor (0 = lit/invisible, 1 = maximum shadow)
                half shadowFactor = saturate(max(mainShadow, addShadow));

                #if defined(_ENABLE_STYLIZED)
                    half halfFeather = max(0.001, _ShadowFeather * 0.5);
                    shadowFactor = smoothstep(_ShadowCutoff - halfFeather, _ShadowCutoff + halfFeather, shadowFactor);
                #endif

                half finalAlpha = shadowFactor * _ShadowColor.a * _ShadowStrength;

                // Discard completely transparent pixels to save fill rate
                clip(finalAlpha - 0.001);

                return half4(_ShadowColor.rgb, finalAlpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
