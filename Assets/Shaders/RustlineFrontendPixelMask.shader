Shader "Rustline/Frontend Pixel Mask"
{
    Properties { [PerRendererData] _MainTex ("Binary Glyph Mask", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always Blend Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            float4 frag(v2f input) : SV_Target
            {
                clip(tex2D(_MainTex, input.uv).a - 0.5);
                // Canvas.vertexColorAlwaysGammaSpace retains the authored palette bytes.
                // The linear backbuffer performs the final sRGB encoding exactly once.
                #ifndef UNITY_COLORSPACE_GAMMA
                    input.color.rgb = float3(GammaToLinearSpaceExact(input.color.r),
                        GammaToLinearSpaceExact(input.color.g), GammaToLinearSpaceExact(input.color.b));
                #endif
                return float4(input.color.rgb, 1.0);
            }
            ENDCG
        }
    }
}
