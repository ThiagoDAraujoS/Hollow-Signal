Shader "Custom/SpriteFlickerMask"
{
    Properties
    {
        [MainTexture] [PerRendererData] _MainTex ("Main Texture (RGBA)", 2D) = "white" {}
        _MaskTex ("Flicker Mask (B&W)", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Flicker Controls)]
        _FlickerAmount ("Flicker Amount (Script Driven)", Range(0, 1)) = 0.0
        _BaseBrightness ("Base Mask Brightness (At Rest)", Range(0, 2)) = 1.0
        _MaxBrightness ("Peak Flicker Brightness", Range(1, 10)) = 3.0
        [HDR] _FlickerColor ("Flicker Glow Tint", Color) = (1, 1, 1, 1)

        [Header(Autonomous Shader Flicker)]
        [Toggle] _AutoFlicker ("Enable Auto Shader Flicker", Float) = 0.0
        _FlickerSpeed ("Auto Flicker Speed", Float) = 25.0
        _FlickerSharpness ("Auto Flicker Sharpness", Range(1, 10)) = 3.0

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
        [Toggle(_ALPHAPREMULTIPLY_ON)] _StraightAlpha ("Straight Alpha Blend", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull [_Cull]
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SpriteFlickerForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MaskTex_ST;
                float4 _Color;
                float4 _FlickerColor;
                float _FlickerAmount;
                float _BaseBrightness;
                float _MaxBrightness;
                float _AutoFlicker;
                float _FlickerSpeed;
                float _FlickerSharpness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 mainCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                half mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv).r;

                // Procedural noise for autonomous flicker
                float proceduralFlicker = 0.0;
                if (_AutoFlicker > 0.5)
                {
                    float t = _Time.y * _FlickerSpeed;
                    float n1 = frac(sin(t * 12.9898) * 43758.5453);
                    float n2 = sin(t * 1.73) * 0.5 + 0.5;
                    float n3 = frac(sin(floor(t * 1.8) * 78.233) * 43758.5453);
                    proceduralFlicker = saturate(pow(n1 * n2, _FlickerSharpness) * 2.2 + (n3 > 0.82 ? 0.7 : 0.0));
                }

                float effectiveFlicker = saturate(_FlickerAmount + proceduralFlicker);

                // Calculate brightness multiplier:
                // Black mask (0.0) -> unchanged at 1.0x
                // White mask (1.0) -> transitions between _BaseBrightness and _MaxBrightness
                float targetBrightness = lerp(_BaseBrightness, _MaxBrightness, effectiveFlicker);
                float finalBrightness = lerp(1.0, targetBrightness, mask);

                half3 rgb = mainCol.rgb * finalBrightness;

                // Add subtle tinted incandescent/neon glow on white mask regions during peaks
                half3 glow = _FlickerColor.rgb * (mask * effectiveFlicker * 0.35 * _MaxBrightness);
                rgb += glow;

                return half4(rgb, mainCol.a);
            }
            ENDHLSL
        }
    }

    // Fallback for built-in pipeline or preview
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull [_Cull]
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            float4 _MainTex_ST;
            float4 _MaskTex_ST;
            fixed4 _Color;
            fixed4 _FlickerColor;
            fixed _FlickerAmount;
            fixed _BaseBrightness;
            fixed _MaxBrightness;
            fixed _AutoFlicker;
            fixed _FlickerSpeed;
            fixed _FlickerSharpness;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 mainCol = tex2D(_MainTex, i.texcoord) * i.color;
                fixed mask = tex2D(_MaskTex, i.texcoord).r;

                float proceduralFlicker = 0.0;
                if (_AutoFlicker > 0.5)
                {
                    float t = _Time.y * _FlickerSpeed;
                    float n1 = frac(sin(t * 12.9898) * 43758.5453);
                    float n2 = sin(t * 1.73) * 0.5 + 0.5;
                    float n3 = frac(sin(floor(t * 1.8) * 78.233) * 43758.5453);
                    proceduralFlicker = saturate(pow(n1 * n2, _FlickerSharpness) * 2.2 + (n3 > 0.82 ? 0.7 : 0.0));
                }

                float effectiveFlicker = saturate(_FlickerAmount + proceduralFlicker);
                float targetBrightness = lerp(_BaseBrightness, _MaxBrightness, effectiveFlicker);
                float finalBrightness = lerp(1.0, targetBrightness, mask);

                fixed3 rgb = mainCol.rgb * finalBrightness;
                fixed3 glow = _FlickerColor.rgb * (mask * effectiveFlicker * 0.35 * _MaxBrightness);
                rgb += glow;

                return fixed4(rgb, mainCol.a);
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
