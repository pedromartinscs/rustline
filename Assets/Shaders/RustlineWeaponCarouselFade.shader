Shader "Rustline/Weapon Carousel Palette Fade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _DarknessLookup ("Canonical Darkness Lookup", 2D) = "black" {}
        _Fade ("Binary Palette Fade", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            sampler2D _MainTex;
            sampler2D _DarknessLookup;
            float4 _MainTex_TexelSize;
            float _Fade;
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            float BayerThreshold(int2 pixel)
            {
                static const float values[16] = { 0,8,2,10,12,4,14,6,3,11,1,9,15,7,13,5 };
                return (values[(pixel.y & 3) * 4 + (pixel.x & 3)] + 0.5) / 16.0;
            }
            float4 Fragment(Varyings input) : SV_Target
            {
                float4 source = tex2D(_MainTex, input.uv);
                if (source.a < 0.5) { discard; }
                float fade = saturate(_Fade);
                int level = min((int)floor((1.0 - fade) * 4.0), 4);
                int3 quantized = (int3)round(saturate(source.rgb) * 31.0);
                int lookupX = quantized.r + quantized.g * 32;
                int lookupY = quantized.b + level * 32;
                float2 lookupUv = (float2(lookupX, lookupY) + 0.5) / float2(1024.0, 160.0);
                // The final quarter is binary-alpha ordered dithering. The source-pixel
                // coordinate anchors the pattern to the card instead of the screen.
                if (fade < 0.25)
                {
                    int2 pixel = int2(floor(input.uv * _MainTex_TexelSize.zw));
                    if (BayerThreshold(pixel) >= fade * 4.0) { discard; }
                }
                return float4(tex2D(_DarknessLookup, lookupUv).rgb, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
