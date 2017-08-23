Shader "Hidden/PostProcessing/MultiScaleVO"
{
    HLSLINCLUDE

        #include "../StdLib.hlsl"
        #include "Fog.hlsl"

        TEXTURE2D_SAMPLER2D(_MSVOcclusionTexture, sampler_MSVOcclusionTexture);
        float3 _AOColor;

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


                struct Output
                {
                    float4 gbuffer0 : SV_Target0;
                    float4 gbuffer3 : SV_Target1;
                };

                Output Frag(VaryingsDefault i)
                {
                    float ao = 1.0 - SAMPLE_TEXTURE2D(_MSVOcclusionTexture, sampler_MSVOcclusionTexture, i.texcoord).r;
                    Output o;
                    o.gbuffer0 = float4(0.0, 0.0, 0.0, ao);
                    o.gbuffer3 = float4(ao * _AOColor, 0.0);
                    return o;
                }

            ENDHLSL
        }

        // 2 - Composite to the frame buffer
        Pass
        {
            HLSLPROGRAM

                #pragma multi_compile _ FOG_LINEAR FOG_EXP FOG_EXP2
                #pragma vertex VertDefault
                #pragma fragment Frag

                TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
                TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);

                float4 Frag(VaryingsDefault i) : SV_Target
                {
                    float2 texcoord = TransformStereoScreenSpaceTex(i.texcoord, 1);
                    half ao = 1.0 - SAMPLE_TEXTURE2D(_MSVOcclusionTexture, sampler_MSVOcclusionTexture, texcoord).r;

                    // Apply fog when enabled (forward-only)
                #if (FOG_LINEAR || FOG_EXP || FOG_EXP2)
                    float d = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, texcoord));
                    d = ComputeFogDistance(d);
                    ao *= ComputeFog(d);
                #endif

                    half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, texcoord);
                    color.rgb *= 1.0 - ao * _AOColor;
                    return color;
                }

            ENDHLSL
        }
    }
}
