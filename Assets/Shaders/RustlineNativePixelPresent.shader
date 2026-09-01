Shader "Hidden/Rustline/NativePixelPresent"
{
    Properties
    {
        _MainTex ("Logical Image", 2D) = "black" {}
        _SourceScaleBias ("Source Scale Bias", Vector) = (1, 1, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Point Present"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vertex
            #pragma fragment Fragment

            sampler2D _MainTex;
            float4 _SourceScaleBias;

            struct FullscreenVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            FullscreenVaryings Vertex(uint vertexID : SV_VertexID)
            {
                float2 uv = float2(
                    vertexID == 1 ? 2.0 : 0.0,
                    vertexID == 2 ? 2.0 : 0.0);

                FullscreenVaryings output;
                output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                output.uv = uv;
                return output;
            }

            float4 Fragment(FullscreenVaryings input) : SV_Target
            {
                float2 sampleUv = input.uv * _SourceScaleBias.xy + _SourceScaleBias.zw;
                return tex2D(_MainTex, sampleUv);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
