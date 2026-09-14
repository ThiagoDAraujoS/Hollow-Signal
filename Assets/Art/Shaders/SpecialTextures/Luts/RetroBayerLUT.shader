Shader "Hidden/Retro/BayerLUT"
{
    Properties
    {
        _LUTTex ("Discrete LUT Texture", 2D) = "white" {}
        _CubeResolution ("Cube Resolution", Float) = 64.0
        _TileLayout ("Tile Layout (Cols X, Rows Y)", Vector) = (8, 8, 0, 0)
        _DitherSpread ("Dither Spread / Strength", Range(0.0, 1.0)) = 0.25
        _DitherMatrixSize ("Dither Matrix (0 = 4x4, 1 = 8x8)", Float) = 1.0
        _PixelSize ("Pixelation Scale", Range(1.0, 16.0)) = 2.0
        _ColorFragmentation ("Pre-LUT Quantization (0 = Off)", Float) = 0.0
        _ColorSpaceCorrection ("Linear to sRGB Processing", Float) = 1.0
        _Blend ("Effect Blend", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "RetroBayerLUTPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "RetroBayerLUT.hlsl"

            Texture2D _LUTTex;

            CBUFFER_START(UnityPerMaterial)
                float4 _TileLayout;
                float _CubeResolution;
                float _DitherSpread;
                float _DitherMatrixSize;
                float _PixelSize;
                float _ColorFragmentation;
                float _ColorSpaceCorrection;
                float _Blend;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float pixelSize = max(1.0, _PixelSize);
                float2 screenRes = _ScreenParams.xy;
                float2 downscaledRes = screenRes / pixelSize;
                float2 pixelatedUV = (floor(input.texcoord * downscaledRes) + 0.5) / downscaledRes;

                // Sample camera scene color
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, pixelatedUV);
                float3 c = sceneColor.rgb;

                // Convert Linear camera input to sRGB for perceptually uniform dither and 0-255 LUT lookup
                if (_ColorSpaceCorrection > 0.5)
                {
                    c = LinearToSRGB(c);
                }

                // Bayer Dither
                uint2 ditherCoord = uint2(pixelatedUV * downscaledRes);
                float dither = GetBayerDitherOffset(ditherCoord, _DitherMatrixSize > 0.5);
                c += dither * _DitherSpread;

                // Optional pre-quantization
                if (_ColorFragmentation > 1.0)
                {
                    c = floor(c * _ColorFragmentation + 0.5) / _ColorFragmentation;
                }

                c = saturate(c);

                // Sample discrete LUT
                // When _LUTTex has 'sRGB (Color Texture)' checked in Unity, the GPU hardware
                // automatically decodes sRGB to Linear space. Hence, 'recolored' is already Linear!
                float3 recolored = SampleDiscreteLUT(
                    _LUTTex,
                    sampler_PointClamp,
                    c,
                    _CubeResolution,
                    _TileLayout.xy
                );

                float3 finalRgb = lerp(sceneColor.rgb, recolored, _Blend);
                return half4(finalRgb, sceneColor.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
