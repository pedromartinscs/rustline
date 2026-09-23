Shader "Rustline/Weapon Carousel Palette Fade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _DarknessLookup ("Canonical Darkness Lookup", 2D) = "black" {}
        _ApertureBottom ("Visible Aperture Bottom", Float) = 0
        _ApertureTop ("Visible Aperture Top", Float) = 100
        _PenumbraThickness ("Spatial Penumbra Thickness", Float) = 20
        _PixelsPerUnit ("Logical Pixels Per Unit", Float) = 16
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            sampler2D _MainTex;
            sampler2D _DarknessLookup;
            float _ApertureBottom;
            float _ApertureTop;
            float _PenumbraThickness;
            float _PixelsPerUnit;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 logicalPosition : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                float3 worldPosition = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(worldPosition);
                output.uv = input.uv;
                output.logicalPosition = worldPosition.xy * _PixelsPerUnit;
                return output;
            }

            float BayerThreshold(int2 pixel)
            {
                static const float values[16] =
                {
                    0, 8, 2, 10,
                    12, 4, 14, 6,
                    3, 11, 1, 9,
                    15, 7, 13, 5
                };
                return (values[(pixel.y & 3) * 4 + (pixel.x & 3)] + 0.5) / 16.0;
            }

            float SpatialPenumbraDistance(float logicalY)
            {
                if (logicalY >= _ApertureBottom && logicalY <= _ApertureTop)
                {
                    return 0.0;
                }

                float thickness = max(_PenumbraThickness, 0.0001);
                return logicalY > _ApertureTop
                    ? (logicalY - _ApertureTop) / thickness
                    : (_ApertureBottom - logicalY) / thickness;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                float4 source = tex2D(_MainTex, input.uv);
                if (source.a < 0.5)
                {
                    discard;
                }

                float distance = SpatialPenumbraDistance(input.logicalPosition.y);
                if (distance > 1.0)
                {
                    discard;
                }

                // Map spatial distance 0..1 onto canonical darkness levels 0..4.
                // Ordered dithering chooses only between neighboring canonical levels;
                // it never interpolates RGB values.
                float scaledLevel = saturate(distance) * 4.0;
                int lowerLevel = min((int)floor(scaledLevel), 4);
                int upperLevel = min(lowerLevel + 1, 4);
                float levelFraction = scaledLevel - lowerLevel;

                int2 logicalPixel = int2(floor(input.logicalPosition));
                int level = lowerLevel;
                if (upperLevel > lowerLevel &&
                    BayerThreshold(logicalPixel) < levelFraction)
                {
                    level = upperLevel;
                }

                int3 quantized = (int3)round(saturate(source.rgb) * 31.0);
                int lookupX = quantized.r + quantized.g * 32;
                int lookupY = quantized.b + level * 32;
                float2 lookupUv =
                    (float2(lookupX, lookupY) + 0.5) / float2(1024.0, 160.0);

                // Alpha is always binary: surviving pixels are fully opaque and pixels
                // beyond the spatial penumbra are discarded.
                return float4(tex2D(_DarknessLookup, lookupUv).rgb, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
