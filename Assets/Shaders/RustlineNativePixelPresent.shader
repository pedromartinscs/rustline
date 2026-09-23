Shader "Hidden/Rustline/NativePixelPresent"
{
    Properties
    {
        _MainTex ("Logical Image", 2D) = "black" {}
        _HudTex ("Logical HUD", 2D) = "black" {}
        _HudEnabled ("HUD Enabled", Float) = 0
        _SourceScaleBias ("Source Scale Bias", Vector) = (1, 1, 0, 0)
        _OutputRect ("Normalized World Output Rect", Vector) = (0, 0, 1, 1)
        _DeepSpaceColor ("Deep Space Color", Color) = (0, 0, 0, 1)
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
            sampler2D _HudTex;
            float4 _SourceScaleBias;
            float4 _OutputRect;
            float4 _DeepSpaceColor;
            float _HudEnabled;

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
                // The pass covers the entire physical backbuffer. The world remains in
                // its centered native-pixel output rectangle while HUD may occupy the
                // surrounding Deep Space area.
                float2 worldUv = (input.uv - _OutputRect.xy) / _OutputRect.zw;
                bool insideWorld =
                    worldUv.x >= 0.0 && worldUv.x < 1.0 &&
                    worldUv.y >= 0.0 && worldUv.y < 1.0;

                float4 world = _DeepSpaceColor;
                if (insideWorld)
                {
                    float2 sampleUv =
                        worldUv * _SourceScaleBias.xy + _SourceScaleBias.zw;
                    world = tex2D(_MainTex, sampleUv);
                }

                float4 hud = tex2D(_HudTex, input.uv);
                return _HudEnabled > 0.5 ? lerp(world, hud, hud.a) : world;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
