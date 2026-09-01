Shader "Hidden/Rustline/NativePixelPresent"
{
    Properties
    {
        _MainTex ("Logical Image", 2D) = "black" {}
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
            float4 _MainTex_TexelSize;

            float4 Fragment(v2f_img input) : SV_Target
            {
                float2 sampleUv = input.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0.0)
                {
                    sampleUv.y = 1.0 - sampleUv.y;
                }
                #endif

                return tex2D(_MainTex, sampleUv);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
