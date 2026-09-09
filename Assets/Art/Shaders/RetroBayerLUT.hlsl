#ifndef RETRO_BAYER_LUT_INCLUDED
#define RETRO_BAYER_LUT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#ifndef UNITY_TEXTURE_INCLUDED
// Fallback definition for regular .shader files where Texture.hlsl is not included
struct UnityTexture2D
{
    Texture2D tex;
    SamplerState samplerstate;
};
#endif

// ----------------------------------------------------------------------------------
// 4x4 and 8x8 Bayer Ordered Dithering Threshold Matrices
// ----------------------------------------------------------------------------------

static const float Bayer4x4[4][4] = {
    {  0.0 / 16.0,  8.0 / 16.0,  2.0 / 16.0, 10.0 / 16.0 },
    { 12.0 / 16.0,  4.0 / 16.0, 14.0 / 16.0,  6.0 / 16.0 },
    {  3.0 / 16.0, 11.0 / 16.0,  1.0 / 16.0,  9.0 / 16.0 },
    { 15.0 / 16.0,  7.0 / 16.0, 13.0 / 16.0,  5.0 / 16.0 }
};

static const float Bayer8x8[8][8] = {
    {  0.0 / 64.0, 32.0 / 64.0,  8.0 / 64.0, 40.0 / 64.0,  2.0 / 64.0, 34.0 / 64.0, 10.0 / 64.0, 42.0 / 64.0 },
    { 48.0 / 64.0, 16.0 / 64.0, 56.0 / 64.0, 24.0 / 64.0, 50.0 / 64.0, 18.0 / 64.0, 58.0 / 64.0, 26.0 / 64.0 },
    { 12.0 / 64.0, 44.0 / 64.0,  4.0 / 64.0, 36.0 / 64.0, 14.0 / 64.0, 46.0 / 64.0,  6.0 / 64.0, 38.0 / 64.0 },
    { 60.0 / 64.0, 28.0 / 64.0, 52.0 / 64.0, 20.0 / 64.0, 62.0 / 64.0, 30.0 / 64.0, 54.0 / 64.0, 22.0 / 64.0 },
    {  3.0 / 64.0, 35.0 / 64.0, 11.0 / 64.0, 43.0 / 64.0,  1.0 / 64.0, 33.0 / 64.0,  9.0 / 64.0, 41.0 / 64.0 },
    { 51.0 / 64.0, 19.0 / 64.0, 59.0 / 64.0, 27.0 / 64.0, 49.0 / 64.0, 17.0 / 64.0, 57.0 / 64.0, 25.0 / 64.0 },
    { 15.0 / 64.0, 47.0 / 64.0,  7.0 / 64.0, 39.0 / 64.0, 13.0 / 64.0, 45.0 / 64.0,  5.0 / 64.0, 37.0 / 64.0 },
    { 63.0 / 64.0, 31.0 / 64.0, 55.0 / 64.0, 23.0 / 64.0, 61.0 / 64.0, 29.0 / 64.0, 53.0 / 64.0, 21.0 / 64.0 }
};

// Returns centered dither value in range [-0.5, 0.5]
float GetBayerDitherOffset(uint2 pixelCoord, bool use8x8)
{
    if (use8x8)
    {
        return Bayer8x8[pixelCoord.y % 8][pixelCoord.x % 8] - 0.5;
    }
    else
    {
        return Bayer4x4[pixelCoord.y % 4][pixelCoord.x % 4] - 0.5;
    }
}

// ----------------------------------------------------------------------------------
// Discrete LUT Sampler (matches RetroLUT Compiler tile specifications)
// ----------------------------------------------------------------------------------
// cubeRes: Cube resolution along each axis (e.g. 64)
// tileLayout: (columns, rows), e.g. (8, 8)
float3 SampleDiscreteLUT(
    Texture2D lutTexture,
    SamplerState lutSampler,
    float3 color,
    float cubeRes,
    float2 tileLayout)
{
    color = saturate(color);
    float maxCoord = max(1.0, cubeRes - 1.0);

    // Blue slice index (0 to cubeRes - 1)
    float bIdx = round(color.b * maxCoord);

    // In RetroLUT Compiler:
    // b increases from left to right, then top to bottom.
    // In Unity UV space, V = 0 is the bottom row of tiles:
    float tileCol = fmod(bIdx, tileLayout.x);
    float tileRow = (tileLayout.y - 1.0) - floor(bIdx / tileLayout.x);

    // Inside each tile:
    // U corresponds to Red (left to right)
    // V corresponds to Green (bottom to top in Unity UV)
    // Texel-center offset (+0.5) ensures discrete point sampling without bleeding:
    float uTile = (round(color.r * maxCoord) + 0.5) / cubeRes;
    float vTile = (round(color.g * maxCoord) + 0.5) / cubeRes;

    float2 lutUV = float2(
        (tileCol + uTile) / tileLayout.x,
        (tileRow + vTile) / tileLayout.y
    );

    return lutTexture.SampleLevel(lutSampler, lutUV, 0).rgb;
}

// ----------------------------------------------------------------------------------
// Core Retro Bayer LUT Processing
// ----------------------------------------------------------------------------------
float3 ProcessRetroBayerLUT(
    float3 inColor,
    float2 screenUV,
    float2 screenResolution,
    Texture2D lutTexture,
    SamplerState lutSampler,
    float cubeResolution,
    float2 tileLayout,
    float ditherSpread,
    float ditherMatrix8x8,
    float pixelScale,
    float colorFragmentation,
    float linearToSRGBInput)
{
    // Apply pixelation grid
    float pixelSize = max(1.0, pixelScale);
    float2 downscaledRes = screenResolution / pixelSize;
    float2 pixelatedUV = (floor(screenUV * downscaledRes) + 0.5) / downscaledRes;

    // Convert Linear camera input to perceptual gamma / sRGB space for uniform dithering & LUT lookup
    float3 c = inColor;
    if (linearToSRGBInput > 0.5)
    {
        c = LinearToSRGB(c);
    }

    // Bayer Dither
    uint2 ditherCoord = uint2(pixelatedUV * downscaledRes);
    float dither = GetBayerDitherOffset(ditherCoord, ditherMatrix8x8 > 0.5);
    c += dither * ditherSpread;

    // Optional pre-LUT posterization
    if (colorFragmentation > 1.0)
    {
        c = floor(c * colorFragmentation + 0.5) / colorFragmentation;
    }

    c = saturate(c);

    // Recolor via discrete LUT
    // Note: When lutTexture is imported with 'sRGB (Color Texture) = true' in Unity,
    // the GPU hardware automatically decodes it to Linear space upon sampling.
    // Therefore, do NOT apply a second SRGBToLinear call here to avoid crushing shadows.
    float3 recolored = SampleDiscreteLUT(
        lutTexture,
        lutSampler,
        c,
        cubeResolution,
        tileLayout
    );

    return recolored;
}

// ----------------------------------------------------------------------------------
// Shader Graph Custom Function Wrapper (uses UnityTexture2D from Texture.hlsl or fallback)
// ----------------------------------------------------------------------------------
void RetroBayerLUT_float(
    float3 InColor,
    float2 ScreenUV,
    float2 ScreenResolution,
    UnityTexture2D LUTTexture,
    float CubeResolution,
    float2 TileLayout,
    float DitherSpread,
    float DitherMatrix8x8,
    float PixelScale,
    float ColorFragmentation,
    float LinearToSRGBInput,
    out float3 OutColor)
{
    OutColor = ProcessRetroBayerLUT(
        InColor,
        ScreenUV,
        ScreenResolution,
        LUTTexture.tex,
        LUTTexture.samplerstate,
        CubeResolution,
        TileLayout,
        DitherSpread,
        DitherMatrix8x8,
        PixelScale,
        ColorFragmentation,
        LinearToSRGBInput
    );
}

// Standard HLSL overload without UnityTexture2D dependency
void RetroBayerLUT_float(
    float3 InColor,
    float2 ScreenUV,
    float2 ScreenResolution,
    Texture2D LUTTexture,
    SamplerState LUTSampler,
    float CubeResolution,
    float2 TileLayout,
    float DitherSpread,
    float DitherMatrix8x8,
    float PixelScale,
    float ColorFragmentation,
    float LinearToSRGBInput,
    out float3 OutColor)
{
    OutColor = ProcessRetroBayerLUT(
        InColor,
        ScreenUV,
        ScreenResolution,
        LUTTexture,
        LUTSampler,
        CubeResolution,
        TileLayout,
        DitherSpread,
        DitherMatrix8x8,
        PixelScale,
        ColorFragmentation,
        LinearToSRGBInput
    );
}

#endif // RETRO_BAYER_LUT_INCLUDED
