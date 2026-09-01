Shader "Hidden/Rustline/PalettePenumbra"
{
    Properties
    {
        _MainTex ("Logical World", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Palette Penumbra"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment Fragment

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float2 _LogicalSize;
            float2 _PlayerPixelCenter;
            float2 _WorldPixelOrigin;
            float _FullVisibleRadius;
            float _FullDarknessRadius;
            float _PenumbraEnabled;
            float4 _Palette[28];
            float4 _DarknessLut[140];

            float BayerThreshold(int2 pixel)
            {
                // The 4x4 period is a power of two; masking keeps negative world
                // coordinates stable and avoids the slower signed modulus path.
                int x = pixel.x & 3;
                int y = pixel.y & 3;
                int value;

                if (y == 0)
                {
                    value = x == 0 ? 0 : (x == 1 ? 8 : (x == 2 ? 2 : 10));
                }
                else if (y == 1)
                {
                    value = x == 0 ? 12 : (x == 1 ? 4 : (x == 2 ? 14 : 6));
                }
                else if (y == 2)
                {
                    value = x == 0 ? 3 : (x == 1 ? 11 : (x == 2 ? 1 : 9));
                }
                else
                {
                    value = x == 0 ? 15 : (x == 1 ? 7 : (x == 2 ? 13 : 5));
                }

                return (value + 0.5) / 16.0;
            }

            int FindNearestPaletteIndex(float3 source)
            {
                int nearestIndex = 0;
                float nearestDistance = 1000000.0;

                [unroll]
                for (int index = 0; index < 28; index++)
                {
                    float3 difference = source - _Palette[index].rgb;
                    float distanceSquared = dot(difference, difference);
                    if (distanceSquared < nearestDistance)
                    {
                        nearestDistance = distanceSquared;
                        nearestIndex = index;
                    }
                }

                return nearestIndex;
            }

            float4 Fragment(v2f_img input) : SV_Target
            {
                float2 sampleUv = input.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0.0)
                {
                    sampleUv.y = 1.0 - sampleUv.y;
                }
                #endif

                float4 source = tex2D(_MainTex, sampleUv);
                if (_PenumbraEnabled < 0.5)
                {
                    return source;
                }

                // input.uv is the output/logical-screen coordinate. sampleUv may be
                // vertically corrected only to read the camera RenderTexture on D3D.
                float2 logicalPixel = floor(input.uv * _LogicalSize);
                float distanceFromPlayer = distance(logicalPixel + 0.5, _PlayerPixelCenter);
                if (distanceFromPlayer <= _FullVisibleRadius)
                {
                    return source;
                }

                if (distanceFromPlayer >= _FullDarknessRadius)
                {
                    return float4(_Palette[0].rgb, 1.0);
                }

                float bandProgress = saturate(
                    (distanceFromPlayer - _FullVisibleRadius) /
                    (_FullDarknessRadius - _FullVisibleRadius));
                float levelPosition = bandProgress * 4.0;
                int lowerLevel = min((int)floor(levelPosition), 3);
                float adjacentLevelCoverage = frac(levelPosition);
                int2 worldPixel = int2(logicalPixel) + int2(_WorldPixelOrigin);
                int level = lowerLevel +
                    (BayerThreshold(worldPixel) < adjacentLevelCoverage ? 1 : 0);

                int sourceIndex = FindNearestPaletteIndex(source.rgb);
                return float4(_DarknessLut[sourceIndex * 5 + level].rgb, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
