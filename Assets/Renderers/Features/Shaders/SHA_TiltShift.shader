Shader "Hidden/PostProcess/TiltShift"
{
    Properties
    {
        _Intensity ("Intensity", Range(0.0, 1.0)) = 1.0
        _FocusCenter ("Focus Center Y", Range(0.0, 1.0)) = 0.5
        _FocusRange ("Focus Range", Range(0.0, 1.0)) = 0.2
        _Feather ("Feather", Range(0.01, 1.0)) = 0.3
        _Angle ("Angle (Degrees)", Range(-90.0, 90.0)) = 0.0
        _MaxBlurRadius ("Max Blur Radius (Pixels)", Range(0.0, 30.0)) = 8.0
        _BlurCurve ("Blur Curve Falloff", Range(0.5, 4.0)) = 2.0
        _ChromaticAberration ("Chromatic Aberration", Range(0.0, 2.0)) = 0.4
        _SaturationBoost ("Saturation Boost", Range(-0.5, 1.0)) = 0.2
        _BokehThreshold ("Bokeh Threshold", Range(0.0, 2.0)) = 0.7
        _BokehIntensity ("Bokeh Intensity", Range(0.0, 3.0)) = 0.8
        _PreviewFocus ("Preview Focus Area", Float) = 0.0
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
            Name "TiltShiftPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
                float _FocusCenter;
                float _FocusRange;
                float _Feather;
                float _Angle;
                float _MaxBlurRadius;
                float _BlurCurve;
                float _ChromaticAberration;
                float _SaturationBoost;
                float _BokehThreshold;
                float _BokehIntensity;
                float _PreviewFocus;
            CBUFFER_END

            // Golden Angle Spiral Sampling Kernel (20 Poisson-distributed taps)
            #define SAMPLE_COUNT 20
            static const float GOLDEN_ANGLE = 2.39996323;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 originalColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                if (_Intensity <= 0.001)
                {
                    return originalColor;
                }

                // Aspect-ratio corrected directional distance calculation
                float aspect = _ScreenParams.x * rcp(_ScreenParams.y);
                float rad = radians(_Angle);
                float2 dir = float2(sin(rad), cos(rad));

                float2 centeredUv = float2((uv.x - 0.5) * aspect, uv.y - _FocusCenter);
                float dist = abs(dot(centeredUv, dir));

                // Focus band and feathering calculation
                float halfRange = _FocusRange * 0.5;
                float falloff = max(0.0, dist - halfRange);
                float blurFactor = saturate(falloff * rcp(max(0.001, _Feather)));
                blurFactor = pow(blurFactor, _BlurCurve);

                float currentRadius = blurFactor * _MaxBlurRadius * _Intensity;

                // Preview mode: overlay visual feedback on focus vs blur regions
                if (_PreviewFocus > 0.5)
                {
                    half3 maskTint = lerp(half3(0.0, 1.0, 0.3), half3(1.0, 0.15, 0.1), (half)blurFactor);
                    half3 previewRgb = lerp(originalColor.rgb, maskTint, 0.4);
                    return half4(previewRgb, originalColor.a);
                }

                // If perfectly in focus, early out for zero performance cost
                if (currentRadius < 0.1)
                {
                    return originalColor;
                }

                float2 texelSize = _BlitTexture_TexelSize.xy;
                if (texelSize.x <= 0.00001)
                {
                    texelSize = rcp(_ScreenParams.xy);
                }

                half3 colorAccum = half3(0.0, 0.0, 0.0);
                float totalWeight = 0.0;
                float caScale = _ChromaticAberration * currentRadius * 0.08;

                // Circular Bokeh Spiral Convolution
                UNITY_UNROLL
                for (int i = 0; i < SAMPLE_COUNT; i++)
                {
                    float r = sqrt((i + 0.5) * (1.0 / SAMPLE_COUNT));
                    float theta = i * GOLDEN_ANGLE;
                    float2 tapOffset = float2(cos(theta), sin(theta)) * r;

                    float2 sampleUv = uv + tapOffset * currentRadius * texelSize;

                    half3 sampleColor;
                    if (_ChromaticAberration > 0.01)
                    {
                        float2 caOffset = tapOffset * caScale * texelSize;
                        sampleColor.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUv + caOffset).r;
                        sampleColor.g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUv).g;
                        sampleColor.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUv - caOffset).b;
                    }
                    else
                    {
                        sampleColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUv).rgb;
                    }

                    // Bokeh Highlight Weighting
                    float lum = dot(sampleColor, half3(0.2126, 0.7152, 0.0722));
                    float highlight = max(0.0, lum - _BokehThreshold);
                    float weight = 1.0 + (highlight * highlight) * _BokehIntensity;

                    colorAccum += sampleColor * weight;
                    totalWeight += weight;
                }

                half3 finalRgb = colorAccum / max(0.001, totalWeight);

                // Optional toy/miniature saturation enhancement in out-of-focus areas
                if (abs(_SaturationBoost) > 0.01)
                {
                    float lum = dot(finalRgb, half3(0.2126, 0.7152, 0.0722));
                    finalRgb = lerp(lum.xxx, finalRgb, 1.0 + (half)(_SaturationBoost * blurFactor));
                }

                return half4(finalRgb, originalColor.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
