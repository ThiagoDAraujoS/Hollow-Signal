Shader "Hidden/Retro/InteractableOutline"
{
    Properties
    {
        _OutlineThickness ("Outline Thickness (Pixels)", Float) = 2.0
        _OutlineIntensity ("Outline Intensity", Range(0.0, 1.0)) = 1.0
        _InnerFillOpacity ("Inner Silhouette Fill Opacity", Range(0.0, 1.0)) = 0.0
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
            Name "InteractableOutlinePass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            Texture2D _HighlightMaskTexture;
            SamplerState sampler_HighlightMaskTexture;
            float4 _HighlightMaskTexture_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float _OutlineThickness;
                float _OutlineIntensity;
                float _InnerFillOpacity;
            CBUFFER_END

            half4 SampleMask(float2 uv)
            {
                return _HighlightMaskTexture.SampleLevel(sampler_HighlightMaskTexture, uv, 0);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);

                float2 texel = _HighlightMaskTexture_TexelSize.xy;
                if (texel.x <= 0.00001)
                    texel = _ScreenParams.zw - 1.0;

                float2 offset = texel * max(1.0, _OutlineThickness);

                half4 center = SampleMask(uv);

                // 8-tap cross and diagonal sampling
                half4 sN  = SampleMask(uv + float2( 0.0,       offset.y));
                half4 sS  = SampleMask(uv + float2( 0.0,      -offset.y));
                half4 sE  = SampleMask(uv + float2( offset.x,  0.0));
                half4 sW  = SampleMask(uv + float2(-offset.x,  0.0));
                half4 sNE = SampleMask(uv + float2( offset.x,  offset.y));
                half4 sNW = SampleMask(uv + float2(-offset.x,  offset.y));
                half4 sSE = SampleMask(uv + float2( offset.x, -offset.y));
                half4 sSW = SampleMask(uv + float2(-offset.x, -offset.y));

                half maxNeighborAlpha = max(max(max(sN.a, sS.a), max(sE.a, sW.a)), max(max(sNE.a, sNW.a), max(sSE.a, sSW.a)));
                half minNeighborAlpha = min(min(min(sN.a, sS.a), min(sE.a, sW.a)), min(min(sNE.a, sNW.a), min(sSE.a, sSW.a)));

                half4 edgeColor = center.a > 0.01 ? center : half4(0, 0, 0, 0);
                if (edgeColor.a < 0.01)
                {
                    if (sN.a > 0.01) edgeColor = sN;
                    else if (sS.a > 0.01) edgeColor = sS;
                    else if (sE.a > 0.01) edgeColor = sE;
                    else if (sW.a > 0.01) edgeColor = sW;
                    else if (sNE.a > 0.01) edgeColor = sNE;
                    else if (sNW.a > 0.01) edgeColor = sNW;
                    else if (sSE.a > 0.01) edgeColor = sSE;
                    else if (sSW.a > 0.01) edgeColor = sSW;
                }

                bool isOuterEdge = (center.a < 0.01) && (maxNeighborAlpha > 0.01);
                bool isInnerEdge = (center.a > 0.01) && (minNeighborAlpha < 0.99);
                bool isEdge = isOuterEdge || isInnerEdge;

                half3 finalColor = sceneColor.rgb;

                if (isEdge && edgeColor.a > 0.01)
                {
                    finalColor = lerp(finalColor, edgeColor.rgb, _OutlineIntensity);
                }
                else if (center.a > 0.01 && _InnerFillOpacity > 0.001)
                {
                    finalColor = lerp(finalColor, center.rgb, _InnerFillOpacity);
                }

                return half4(finalColor, sceneColor.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
