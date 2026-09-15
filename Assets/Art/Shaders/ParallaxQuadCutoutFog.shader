Shader "Diorama/ParallaxQuadCutoutFog"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture (RGBA)", 2D) = "white" {}
        [MainColor] _Color ("Color Tint", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        [Header(Edge Smoothing)]
        [Toggle(_ALPHATOCOVERAGE_ON)] _AlphaToCoverage ("Enable Alpha To Coverage", Float) = 1
        
        [Header(Render State)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0 // Off (double-sided)

        [Header(Depth Fog Settings)]
        [Toggle(_ENABLE_FOG)] _EnableFog ("Enable Fog", Float) = 1
        [HDR] _FogNearColor ("Near Fog Color (A)", Color) = (0.3, 0.4, 0.5, 1.0)
        [HDR] _FogFarColor  ("Far Fog Color (B)", Color) = (0.05, 0.08, 0.15, 1.0)
        _FogStart ("Fog Start Distance", Float) = 5.0
        _FogEnd ("Fog End Distance", Float) = 30.0
        _FogNearBlend ("Near Fog Blend (At Start)", Range(0.0, 1.0)) = 0.0
        _FogFarBlend  ("Far Fog Blend (At End)", Range(0.0, 1.0)) = 1.0
        _FogIntensity ("Master Fog Intensity", Range(0.0, 1.0)) = 1.0
        
        [Header(Contrast and Shading)]
        _FogMultiply ("Multiply Blend", Range(0.0, 1.0)) = 1.0
        _FogPreserveDark ("Preserve Dark Pixels", Range(0.0, 1.0)) = 0.5
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

        Cull [_Cull]
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATOCOVERAGE_ON
            #pragma shader_feature_local _ENABLE_FOG

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float  depth        : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _FogNearColor;
                float4 _FogFarColor;
                float  _Cutoff;
                float  _FogStart;
                float  _FogEnd;
                float  _FogNearBlend;
                float  _FogFarBlend;
                float  _FogIntensity;
                float  _FogMultiply;
                float  _FogPreserveDark;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.depth = -posInputs.positionVS.z;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

                // High-performance cutout: discard transparent pixels immediately
                clip(col.a - _Cutoff);

                #if defined(_ENABLE_FOG)
                    float t = saturate((input.depth - _FogStart) / max(0.001, _FogEnd - _FogStart));
                    half3 fogTone = lerp(_FogNearColor.rgb, _FogFarColor.rgb, t);
                    float fogAmount = lerp(_FogNearBlend, _FogFarBlend, t) * _FogIntensity;

                    // Luminance masking to preserve dark linework/shadows on 2D planes
                    float lum = dot(col.rgb, half3(0.2126, 0.7152, 0.0722));
                    float darkMask = lerp(1.0, saturate(lum), _FogPreserveDark);
                    float effectiveFog = fogAmount * darkMask;

                    half3 linearFog = lerp(col.rgb, fogTone, effectiveFog);
                    half3 multiplyFog = lerp(col.rgb, col.rgb * fogTone, effectiveFog);
                    col.rgb = lerp(linearFog, multiplyFog, _FogMultiply);
                #endif

                return col;
            }
            ENDHLSL
        }

        // Depth-only pass for URP Depth Prepass and post-processing / SSAO / Motion Vectors
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _FogNearColor;
                float4 _FogFarColor;
                float  _Cutoff;
                float  _FogStart;
                float  _FogEnd;
                float  _FogNearBlend;
                float  _FogFarBlend;
                float  _FogIntensity;
                float  _FogMultiply;
                float  _FogPreserveDark;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                clip(col.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
