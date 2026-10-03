Shader "HollowSignal/Environment/SimpleLitPackedAOCutoutVAT"
{
    Properties
    {
        [Header(OpenVAT Animation)]
        _PosTexA           ("VAT Position (RGB)", 2D) = "white" {}
        _NrmTexA           ("VAT Normal (RGB)", 2D) = "bump" {}
        _MinA              ("Min Bounds", Vector) = (0, 0, 0, 0)
        _MaxA              ("Max Bounds", Vector) = (0, 0, 0, 0)
        _FrameStartA       ("Frame Start", Float) = 1.0
        _FrameEndA         ("Frame End", Float) = 1.0
        _FPSA              ("FPS", Float) = 30.0
        _YResA             ("Y Resolution", Float) = 0.0
        _TimeOffsetA       ("Time Offset", Float) = 0.0
        [Toggle] _UseTime  ("Use Time Animation", Float) = 1.0
        [Toggle] _UseVNRM  ("Use Separate Normal Map", Float) = 0.0

        [Header(Color and Saturation)]
        [MainTexture] _BaseMap ("Albedo (RGB) + Cutout Alpha (A)", 2D) = "white" {}
        [MainColor] [HDR] _BaseColor ("Color Tint", Color) = (1, 1, 1, 1)
        _Saturation   ("Saturation", Float) = 1.0

        [Header(Cutout Transparency)]
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Header(Ridge and Cavity Detection)]
        [Toggle(_ENABLE_RIDGE_CAVITY)] _EnableRidgeCavity ("Enable Ridge & Cavity", Float) = 1.0
        _CavityStrength    ("Cavity (Crevice) Darkness", Range(0.0, 5.0)) = 1.5
        _CavityPower       ("Cavity Exponent (Pinch)", Range(0.5, 4.0)) = 1.2
        _CavityRadius      ("Crevice Sample Radius (Pixels)", Range(0.5, 5.0)) = 1.5
        [HDR] _CavityTint  ("Cavity Shadow Tint", Color) = (0.15, 0.15, 0.18, 1.0)
        _RidgeStrength     ("Ridge (Peak) Highlight", Range(0.0, 3.0)) = 0.8
        _RidgePower        ("Ridge Exponent", Range(0.5, 4.0)) = 2.0
        [HDR] _RidgeTint   ("Ridge Highlight Tint", Color) = (1.25, 1.25, 1.25, 1.0)
        _CurvatureBias     ("Curvature Sensitivity", Range(0.1, 5.0)) = 1.0

        [Header(World Height Gradient)]
        [Toggle(_ENABLE_HEIGHT_GRADIENT)] _EnableHeightGrad ("Enable Height Gradient", Float) = 0.0
        _HeightGradMinY    ("Height Min Y (Base Level)", Float) = 0.0
        _HeightGradMaxY    ("Height Max Y (Top Level)", Float) = 15.0
        [HDR] _HeightGradColor ("Ground Tint (Street Grime)", Color) = (0.4, 0.4, 0.45, 1.0)
        _HeightGradStrength ("Height Grad Strength", Float) = 0.5

        [Header(Stylized Shadow Sharpness)]
        [Toggle(_ENABLE_SHADOW_SHARPNESS)] _EnableShadowSharpness ("Enable Sharp Shadows", Float) = 0.0
        _ShadowThreshold   ("Shadow Threshold", Float) = 0.25
        _ShadowSoftness    ("Shadow Edge Softness", Float) = 0.15

        [Header(Lighting Tuning)]
        _AmbientBoost ("Ambient Boost", Float) = 1.0

        [Header(Render State)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0 // Double-sided by default for cutout quads/geometry
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        LOD 200
        Cull [_Cull]
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _ENABLE_RIDGE_CAVITY
            #pragma shader_feature_local _ENABLE_HEIGHT_GRADIENT
            #pragma shader_feature_local _ENABLE_SHADOW_SHARPNESS

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                float2 uv1          : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                half3  normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord : TEXCOORD3;
                #endif
                half fogFactor      : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_PosTexA);
            SAMPLER(sampler_PosTexA);
            TEXTURE2D(_NrmTexA);
            SAMPLER(sampler_NrmTexA);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half4  _HeightGradColor;
                half4  _CavityTint;
                half4  _RidgeTint;
                float4 _MinA;
                float4 _MaxA;
                half   _Cutoff;
                half   _Saturation;
                half   _CavityStrength;
                half   _CavityPower;
                half   _CavityRadius;
                half   _RidgeStrength;
                half   _RidgePower;
                half   _CurvatureBias;
                half   _HeightGradMinY;
                half   _HeightGradMaxY;
                half   _HeightGradStrength;
                half   _ShadowThreshold;
                half   _ShadowSoftness;
                half   _AmbientBoost;
                float  _FrameStartA;
                float  _FrameEndA;
                float  _FPSA;
                float  _YResA;
                float  _TimeOffsetA;
                float  _UseTime;
                float  _UseVNRM;
            CBUFFER_END

            void EvaluateOpenVAT(
                float2 uv1,
                inout float3 positionOS,
                inout float3 normalOS)
            {
                if (_YResA <= 0.0)
                    return;

                float totalFrames = max(_FrameEndA - _FrameStartA + 1.0, 1.0);
                float animTime = max(0.0, _Time.y - _TimeOffsetA);
                float timeInFrames = animTime * _FPSA;
                float frameMod = floor(timeInFrames - totalFrames * floor(timeInFrames / totalFrames));
                float animFrame = max(1.0, frameMod + _FrameStartA);
                float currentFrame = lerp(_FrameStartA, animFrame, _UseTime);

                float frameOffset = currentFrame - 1.0;
                float vOffset = frameOffset / _YResA;

                float2 vatUV = float2(uv1.x, uv1.y - vOffset);

                float4 posSample = SAMPLE_TEXTURE2D_LOD(_PosTexA, sampler_PosTexA, vatUV, 0);
                float3 remappedPos = lerp(_MinA.xyz, _MaxA.xyz, posSample.rgb);
                float3 vatOffset = remappedPos * 0.01 * float3(-1.0, 1.0, 1.0);
                positionOS += vatOffset;

                float4 nrmSample;
                if (_UseVNRM > 0.5)
                {
                    nrmSample = SAMPLE_TEXTURE2D_LOD(_NrmTexA, sampler_NrmTexA, vatUV, 0);
                }
                else
                {
                    float2 nrmUV = float2(vatUV.x, vatUV.y - 0.5);
                    nrmSample = SAMPLE_TEXTURE2D_LOD(_PosTexA, sampler_PosTexA, nrmUV, 0);
                }

                float3 remappedNorm = nrmSample.rgb * 2.0 - 1.0;
                normalOS = normalize(remappedNorm * float3(-1.0, 1.0, 1.0));
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 animatedPosOS = input.positionOS.xyz;
                float3 animatedNormOS = input.normalOS;
                EvaluateOpenVAT(input.uv1, animatedPosOS, animatedNormOS);

                VertexPositionInputs posInputs = GetVertexPositionInputs(animatedPosOS);
                VertexNormalInputs normInputs = GetVertexNormalInputs(animatedNormOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = GetShadowCoord(posInputs);
                #endif

                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            // Computes Blender-style Ridge and Cavity factor
            void EvaluateRidgeAndCavity(
                float2 screenUV,
                float3 positionWS,
                half3 normalWS,
                float rawDepth,
                out half outCavityFactor,
                out half3 outRidgeTint)
            {
                outCavityFactor = 1.0;
                outRidgeTint = half3(1.0, 1.0, 1.0);

                // View-space surface normal
                half3 normalVS = TransformWorldToViewNormal(normalWS);

                // 1. Intra-mesh Curvature via Screen-Space Derivatives
                half3 dNdx = ddx(normalVS);
                half3 dNdy = ddy(normalVS);
                half derivativeCurv = (-dNdx.x - dNdy.y) * _CurvatureBias;

                // 2. Screen-Space Normal Buffer Kernel Sampling
                float2 texelSize = rcp(_ScreenParams.xy);
                float2 offset = texelSize * _CavityRadius;

                float2 uv0 = screenUV + float2(-offset.x,  offset.y); // TL
                float2 uv1 = screenUV + float2( offset.x,  offset.y); // TR
                float2 uv2 = screenUV + float2(-offset.x, -offset.y); // BL
                float2 uv3 = screenUV + float2( offset.x, -offset.y); // BR

                half3 n0 = TransformWorldToViewNormal(SampleSceneNormals(uv0));
                half3 n1 = TransformWorldToViewNormal(SampleSceneNormals(uv1));
                half3 n2 = TransformWorldToViewNormal(SampleSceneNormals(uv2));
                half3 n3 = TransformWorldToViewNormal(SampleSceneNormals(uv3));

                // Depth discontinuity rejection to avoid silhouette halos
                float centerEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float d0 = abs(LinearEyeDepth(SampleSceneDepth(uv0), _ZBufferParams) - centerEyeDepth);
                float d1 = abs(LinearEyeDepth(SampleSceneDepth(uv1), _ZBufferParams) - centerEyeDepth);
                float d2 = abs(LinearEyeDepth(SampleSceneDepth(uv2), _ZBufferParams) - centerEyeDepth);
                float d3 = abs(LinearEyeDepth(SampleSceneDepth(uv3), _ZBufferParams) - centerEyeDepth);

                float depthThreshold = max(0.05, centerEyeDepth * 0.05);
                float w0 = d0 < depthThreshold ? 1.0 : 0.0;
                float w1 = d1 < depthThreshold ? 1.0 : 0.0;
                float w2 = d2 < depthThreshold ? 1.0 : 0.0;
                float w3 = d3 < depthThreshold ? 1.0 : 0.0;
                float totalWeight = w0 + w1 + w2 + w3;

                half screenCurv = 0.0;
                if (totalWeight > 1.5)
                {
                    half leftX   = (n0.x * w0 + n2.x * w2) / max(0.001, w0 + w2);
                    half rightX  = (n1.x * w1 + n3.x * w3) / max(0.001, w1 + w3);
                    half bottomY = (n2.y * w2 + n3.y * w3) / max(0.001, w2 + w3);
                    half topY    = (n0.y * w0 + n1.y * w1) / max(0.001, w0 + w1);

                    screenCurv = ((leftX - rightX) + (bottomY - topY)) * _CurvatureBias;
                }

                // Combined curvature measure: positive = valley (cavity), negative = ridge (peak)
                half totalCurvature = screenCurv + derivativeCurv;

                // Cavity (Valley / Inward Crevice)
                half valley = saturate(totalCurvature * _CavityStrength);
                valley = pow(valley, _CavityPower);
                outCavityFactor = 1.0 - valley;

                // Ridge (Convex Peak / Outer Edge Highlight)
                half ridge = saturate(-totalCurvature * _RidgeStrength);
                ridge = pow(ridge, _RidgePower);
                outRidgeTint = lerp(half3(1.0, 1.0, 1.0), _RidgeTint.rgb, ridge);
            }

            half4 frag(Varyings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 texSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half alpha = texSample.a * _BaseColor.a;

                // Hard cutout discard based purely on texture alpha
                clip(alpha - _Cutoff);

                half3 baseAlbedo = texSample.rgb * _BaseColor.rgb;

                // Saturation adjustment (Rec. 709 luminance weights)
                half luminance = dot(baseAlbedo, half3(0.2126h, 0.7152h, 0.0722h));
                baseAlbedo = lerp(luminance.xxx, baseAlbedo, _Saturation);

                // 1. World-Y Height Gradient (Ground Street Grime / Smog blend)
                #if defined(_ENABLE_HEIGHT_GRADIENT)
                    half heightFactor = saturate((input.positionWS.y - _HeightGradMinY) / max(0.0001, _HeightGradMaxY - _HeightGradMinY));
                    half3 bottomTint = lerp(_HeightGradColor.rgb, half3(1.0, 1.0, 1.0), heightFactor);
                    baseAlbedo *= lerp(half3(1.0, 1.0, 1.0), bottomTint, _HeightGradStrength);
                #endif

                // Normal calculation - flip normal for back faces so double-sided quads catch light from whichever side is facing the camera
                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                normalWS = IS_FRONT_VFACE(facing, normalWS, -normalWS);

                // 2. Blender-style Ridge & Cavity Detection (Screen-space procedural occlusion)
                half cavityDarkening = 1.0;
                #if defined(_ENABLE_RIDGE_CAVITY)
                    float2 screenUV = input.positionCS.xy / _ScreenParams.xy;
                    half cavityFactor;
                    half3 ridgeTint;
                    EvaluateRidgeAndCavity(screenUV, input.positionWS, normalWS, input.positionCS.z, cavityFactor, ridgeTint);

                    // Apply cavity darkening & crevice tint
                    half3 cavityColor = lerp(_CavityTint.rgb, half3(1.0, 1.0, 1.0), cavityFactor);
                    baseAlbedo *= cavityColor;
                    cavityDarkening = cavityFactor;

                    // Apply ridge peak highlight
                    baseAlbedo *= ridgeTint;
                #endif

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                    float4 shadowCoord = float4(0, 0, 0, 0);
                #endif

                // Main Directional Light
                Light mainLight = GetMainLight(shadowCoord);
                half NdotL = saturate(dot(normalWS, mainLight.direction));

                // 3. Stylized Shadow Sharpness (Crisp graphic falloff)
                #if defined(_ENABLE_SHADOW_SHARPNESS)
                    half halfSoft = max(0.0001, _ShadowSoftness * 0.5);
                    NdotL = smoothstep(_ShadowThreshold - halfSoft, _ShadowThreshold + halfSoft, NdotL);
                #endif

                half directAtten = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half3 directLighting = mainLight.color * (NdotL * directAtten * cavityDarkening);

                // Ambient / Spherical Harmonics
                half3 ambient = SampleSH(normalWS) * _AmbientBoost * cavityDarkening;

                // Additional Lights (point / spot)
                half3 additionalLighting = 0;
                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        #if defined(_ADDITIONAL_LIGHT_SHADOWS)
                            Light light = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                        #else
                            Light light = GetAdditionalLight(lightIndex, input.positionWS);
                        #endif
                        half addNdotL = saturate(dot(normalWS, light.direction));
                        #if defined(_ENABLE_SHADOW_SHARPNESS)
                            addNdotL = smoothstep(_ShadowThreshold - halfSoft, _ShadowThreshold + halfSoft, addNdotL);
                        #endif
                        half addAtten = light.distanceAttenuation * light.shadowAttenuation;
                        additionalLighting += light.color * (addNdotL * addAtten * cavityDarkening);
                    LIGHT_LOOP_END
                #endif

                // Combine diffuse & ambient
                half3 finalColor = baseAlbedo * (ambient + directLighting + additionalLighting);

                // Fog
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                float2 uv1          : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_PosTexA);
            SAMPLER(sampler_PosTexA);
            TEXTURE2D(_NrmTexA);
            SAMPLER(sampler_NrmTexA);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half4  _HeightGradColor;
                half4  _CavityTint;
                half4  _RidgeTint;
                float4 _MinA;
                float4 _MaxA;
                half   _Cutoff;
                half   _Saturation;
                half   _CavityStrength;
                half   _CavityPower;
                half   _CavityRadius;
                half   _RidgeStrength;
                half   _RidgePower;
                half   _CurvatureBias;
                half   _HeightGradMinY;
                half   _HeightGradMaxY;
                half   _HeightGradStrength;
                half   _ShadowThreshold;
                half   _ShadowSoftness;
                half   _AmbientBoost;
                float  _FrameStartA;
                float  _FrameEndA;
                float  _FPSA;
                float  _YResA;
                float  _TimeOffsetA;
                float  _UseTime;
                float  _UseVNRM;
            CBUFFER_END

            void EvaluateOpenVAT(
                float2 uv1,
                inout float3 positionOS,
                inout float3 normalOS)
            {
                if (_YResA <= 0.0)
                    return;

                float totalFrames = max(_FrameEndA - _FrameStartA + 1.0, 1.0);
                float animTime = max(0.0, _Time.y - _TimeOffsetA);
                float timeInFrames = animTime * _FPSA;
                float frameMod = floor(timeInFrames - totalFrames * floor(timeInFrames / totalFrames));
                float animFrame = max(1.0, frameMod + _FrameStartA);
                float currentFrame = lerp(_FrameStartA, animFrame, _UseTime);

                float frameOffset = currentFrame - 1.0;
                float vOffset = frameOffset / _YResA;

                float2 vatUV = float2(uv1.x, uv1.y - vOffset);

                float4 posSample = SAMPLE_TEXTURE2D_LOD(_PosTexA, sampler_PosTexA, vatUV, 0);
                float3 remappedPos = lerp(_MinA.xyz, _MaxA.xyz, posSample.rgb);
                float3 vatOffset = remappedPos * 0.01 * float3(-1.0, 1.0, 1.0);
                positionOS += vatOffset;

                float4 nrmSample;
                if (_UseVNRM > 0.5)
                {
                    nrmSample = SAMPLE_TEXTURE2D_LOD(_NrmTexA, sampler_NrmTexA, vatUV, 0);
                }
                else
                {
                    float2 nrmUV = float2(vatUV.x, vatUV.y - 0.5);
                    nrmSample = SAMPLE_TEXTURE2D_LOD(_PosTexA, sampler_PosTexA, nrmUV, 0);
                }

                float3 remappedNorm = nrmSample.rgb * 2.0 - 1.0;
                normalOS = normalize(remappedNorm * float3(-1.0, 1.0, 1.0));
            }

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 animatedPosOS = input.positionOS.xyz;
                float3 animatedNormalOS = input.normalOS;
                EvaluateOpenVAT(input.uv1, animatedPosOS, animatedNormalOS);

                float3 positionWS = TransformObjectToWorld(animatedPosOS);
                float3 normalWS = TransformObjectToWorldNormal(animatedNormalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = GetShadowPositionHClip(input);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float2 uv1          : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_PosTexA);
            SAMPLER(sampler_PosTexA);
            TEXTURE2D(_NrmTexA);
            SAMPLER(sampler_NrmTexA);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half4  _HeightGradColor;
                half4  _CavityTint;
                half4  _RidgeTint;
                float4 _MinA;
                float4 _MaxA;
                half   _Cutoff;
                half   _Saturation;
                half   _CavityStrength;
                half   _CavityPower;
                half   _CavityRadius;
                half   _RidgeStrength;
                half   _RidgePower;
                half   _CurvatureBias;
                half   _HeightGradMinY;
                half   _HeightGradMaxY;
                half   _HeightGradStrength;
                half   _ShadowThreshold;
                half   _ShadowSoftness;
                half   _AmbientBoost;
                float  _FrameStartA;
                float  _FrameEndA;
                float  _FPSA;
                float  _YResA;
                float  _TimeOffsetA;
                float  _UseTime;
                float  _UseVNRM;
            CBUFFER_END

            void EvaluateOpenVAT(
                float2 uv1,
                inout float3 positionOS,
                inout float3 normalOS)
            {
                if (_YResA <= 0.0)
                    return;

                float totalFrames = max(_FrameEndA - _FrameStartA + 1.0, 1.0);
                float animTime = max(0.0, _Time.y - _TimeOffsetA);
                float timeInFrames = animTime * _FPSA;
                float frameMod = floor(timeInFrames - totalFrames * floor(timeInFrames / totalFrames));
                float animFrame = max(1.0, frameMod + _FrameStartA);
                float currentFrame = lerp(_FrameStartA, animFrame, _UseTime);

                float frameOffset = currentFrame - 1.0;
                float vOffset = frameOffset / _YResA;

                float2 vatUV = float2(uv1.x, uv1.y - vOffset);

                float4 posSample = SAMPLE_TEXTURE2D_LOD(_PosTexA, sampler_PosTexA, vatUV, 0);
                float3 remappedPos = lerp(_MinA.xyz, _MaxA.xyz, posSample.rgb);
                float3 vatOffset = remappedPos * 0.01 * float3(-1.0, 1.0, 1.0);
                positionOS += vatOffset;

                float4 nrmSample;
                if (_UseVNRM > 0.5)
                {
                    nrmSample = SAMPLE_TEXTURE2D_LOD(_NrmTexA, sampler_NrmTexA, vatUV, 0);
                }
                else
                {
                    float2 nrmUV = float2(vatUV.x, vatUV.y - 0.5);
                    nrmSample = SAMPLE_TEXTURE2D_LOD(_PosTexA, sampler_PosTexA, nrmUV, 0);
                }

                float3 remappedNorm = nrmSample.rgb * 2.0 - 1.0;
                normalOS = normalize(remappedNorm * float3(-1.0, 1.0, 1.0));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 animatedPosOS = input.positionOS.xyz;
                float3 dummyNormal = float3(0, 1, 0);
                EvaluateOpenVAT(input.uv1, animatedPosOS, dummyNormal);
                output.positionCS = TransformObjectToHClip(animatedPosOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                float2 uv1          : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                half3  normalWS     : TEXCOORD0;
                float2 uv           : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_PosTexA);
            SAMPLER(sampler_PosTexA);
            TEXTURE2D(_NrmTexA);
            SAMPLER(sampler_NrmTexA);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half4  _HeightGradColor;
                half4  _CavityTint;
                half4  _RidgeTint;
                float4 _MinA;
                float4 _MaxA;
                half   _Cutoff;
                half   _Saturation;
                half   _CavityStrength;
                half   _CavityPower;
                half   _CavityRadius;
                half   _RidgeStrength;
                half   _RidgePower;
                half   _CurvatureBias;
                half   _HeightGradMinY;
                half   _HeightGradMaxY;
                half   _HeightGradStrength;
                half   _ShadowThreshold;
                half   _ShadowSoftness;
                half   _AmbientBoost;
                float  _FrameStartA;
                float  _FrameEndA;
                float  _FPSA;
                float  _YResA;
                float  _TimeOffsetA;
                float  _UseTime;
                float  _UseVNRM;
            CBUFFER_END

            void EvaluateOpenVAT(
                float2 uv1,
                inout float3 positionOS,
                inout float3 normalOS)
            {
                if (_YResA <= 0.0)
                    return;

                float totalFrames = max(_FrameEndA - _FrameStartA + 1.0, 1.0);
                float animTime = max(0.0, _Time.y - _TimeOffsetA);
                float timeInFrames = animTime * _FPSA;
                float frameMod = floor(timeInFrames - totalFrames * floor(timeInFrames / totalFrames));
                float animFrame = max(1.0, frameMod + _FrameStartA);
                float currentFrame = lerp(_FrameStartA, animFrame, _UseTime);

                float frameOffset = currentFrame - 1.0;
                float vOffset = frameOffset / _YResA;

                float2 vatUV = float2(uv1.x, uv1.y - vOffset);

                float4 posSample = SAMPLE_TEXTURE2D_LOD(_PosTexA, sampler_PosTexA, vatUV, 0);
                float3 remappedPos = lerp(_MinA.xyz, _MaxA.xyz, posSample.rgb);
                float3 vatOffset = remappedPos * 0.01 * float3(-1.0, 1.0, 1.0);
                positionOS += vatOffset;

                float4 nrmSample;
                if (_UseVNRM > 0.5)
                {
                    nrmSample = SAMPLE_TEXTURE2D_LOD(_NrmTexA, sampler_NrmTexA, vatUV, 0);
                }
                else
                {
                    float2 nrmUV = float2(vatUV.x, vatUV.y - 0.5);
                    nrmSample = SAMPLE_TEXTURE2D_LOD(_PosTexA, sampler_PosTexA, nrmUV, 0);
                }

                float3 remappedNorm = nrmSample.rgb * 2.0 - 1.0;
                normalOS = normalize(remappedNorm * float3(-1.0, 1.0, 1.0));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 animatedPosOS = input.positionOS.xyz;
                float3 animatedNormOS = input.normalOS;
                EvaluateOpenVAT(input.uv1, animatedPosOS, animatedNormOS);
                output.positionCS = TransformObjectToHClip(animatedPosOS);
                output.normalWS = TransformObjectToWorldNormal(animatedNormOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                normalWS = IS_FRONT_VFACE(facing, normalWS, -normalWS);
                return half4(normalWS, 0.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Simple Lit"
}
