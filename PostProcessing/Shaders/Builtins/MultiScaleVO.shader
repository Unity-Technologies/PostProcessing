Shader "Hidden/PostProcessing/MultiScaleVO"
{
    HLSLINCLUDE

        #include "../StdLib.hlsl"
        #include "Fog.hlsl"

        TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
        TEXTURE2D_SAMPLER2D(_MSVOcclusionTexture, sampler_MSVOcclusionTexture);
        float3 _AOColor;

        VaryingsDefault Vert(AttributesDefault v)
        {
            VaryingsDefault o;
            o.vertex = float4(v.vertex.xy, 0.0, 1.0);
            o.texcoord = TransformTriangleVertexToUV(v.vertex.xy);

        #if UNITY_UV_STARTS_AT_TOP
            o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
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

                #pragma vertex Vert
                #pragma fragment Frag

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

                #pragma vertex Vert
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
            Blend Zero OneMinusSrcColor, Zero OneMinusSrcAlpha

            HLSLPROGRAM

                #pragma multi_compile _ APPLY_FORWARD_FOG
                #pragma multi_compile _ FOG_LINEAR FOG_EXP FOG_EXP2
                #pragma vertex Vert
                #pragma fragment Frag

                float4 Frag(VaryingsDefault i) : SV_Target
                {
                    float2 texcoord = TransformStereoScreenSpaceTex(i.texcoord, 1);
                    half ao = 1.0 - SAMPLE_TEXTURE2D(_MSVOcclusionTexture, sampler_MSVOcclusionTexture, texcoord).r;

                    // Apply fog when enabled (forward-only)
                #if (APPLY_FORWARD_FOG)
                    float d = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, texcoord));
                    d = ComputeFogDistance(d);
                    ao *= ComputeFog(d);
                #endif

                    return float4(ao * _AOColor, 0.0);
                }

            ENDHLSL
        }

        // 3 - Debug overlay
        Pass
        {
            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment Frag

                float4 Frag(VaryingsDefault i) : SV_Target
                {
                    half ao = SAMPLE_TEXTURE2D(_MSVOcclusionTexture, sampler_MSVOcclusionTexture, i.texcoord).r;
                    return float4(ao.rrr, 1.0);
                }

            ENDHLSL
        }
    }
}
