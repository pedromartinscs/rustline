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
            #pragma vertex vert_img
            #pragma fragment Fragment

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _SourceScaleBias;

            float4 Fragment(v2f_img input) : SV_Target
            {
                float2 sampleUv = input.uv * _SourceScaleBias.xy + _SourceScaleBias.zw;
                return tex2D(_MainTex, sampleUv);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
