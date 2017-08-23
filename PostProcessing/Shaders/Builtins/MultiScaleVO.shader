Shader "Hidden/PostProcessing/MultiScaleVO"
{
    HLSLINCLUDE

        #include "../StdLib.hlsl"

        // Full screen triangle with procedural draw
        // This can't be used when the destination can be the back buffer because
        // this doesn't support the situations that requires vertical flipping.
        VaryingsDefault VertProcedural(uint vid : SV_VertexID)
        {
            float x = vid == 1 ? 2 : 0;
            float y = vid >  1 ? 2 : 0;

            VaryingsDefault o;
            o.vertex = float4(x * 2.0 - 1.0, 1.0 - y * 2.0, 0.0, 1.0);

        #if UNITY_UV_STARTS_AT_TOP
            o.texcoord = float2(x, y);
        #else
            o.texcoord = float2(x, 1.0 - y);
        #endif

            o.texcoord = TransformStereoScreenSpaceTex(o.texcoord, 1);
            return o;
        }

        // The standard vertex shader for blit, slightly modified for supporting
        // single-pass stereo rendering.
        struct AttributesStd
        {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        VaryingsDefault VertStd(AttributesStd v)
        {
            VaryingsDefault o;
            o.vertex = float4(v.vertex.xy * 2.0 - 1.0, 0.0, 1.0);
            o.texcoord = TransformStereoScreenSpaceTex(v.texcoord, 1);

            #if UNITY_UV_STARTS_AT_TOP
            o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
            #endif

            return o;
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0 - Depth copy with procedural draw
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertProcedural
                #pragma fragment Frag

                TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);

                float4 Frag(VaryingsDefault i) : SV_Target
                {
                    return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoord);
                }

            ENDHLSL
        }

        // 1 - Composite to G-buffer with procedural draw
        Pass
        {
            Blend Zero OneMinusSrcColor, Zero OneMinusSrcAlpha

            HLSLPROGRAM

                #pragma vertex VertProcedural
                #pragma fragment Frag

                TEXTURE2D_SAMPLER2D(_OcclusionTexture, sampler_OcclusionTexture);

                struct Output
                {
                    float4 gbuffer0 : SV_Target0;
                    float4 gbuffer3 : SV_Target1;
                };

                Output Frag(VaryingsDefault i)
                {
                    float ao = 1.0 - SAMPLE_TEXTURE2D(_OcclusionTexture, sampler_OcclusionTexture, i.texcoord).r;
                    Output o;
                    o.gbuffer0 = float4(0.0, 0.0, 0.0, ao);
                    o.gbuffer3 = float4(ao, ao, ao, 0.0);
                    return o;
                }

            ENDHLSL
        }

        // 2 - Composite to the frame buffer with the standard blit
        Pass
        {
            Blend Zero SrcAlpha

            HLSLPROGRAM

                #pragma vertex VertStd
                #pragma fragment Frag

                TEXTURE2D_SAMPLER2D(_OcclusionTexture, sampler_OcclusionTexture);

                float4 Frag(VaryingsDefault i) : SV_Target
                {
                    return SAMPLE_TEXTURE2D(_OcclusionTexture, sampler_OcclusionTexture, i.texcoord).rrrr;
                }

            ENDHLSL
        }
    }
}
