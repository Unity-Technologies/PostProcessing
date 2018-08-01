Shader "Hidden/PostProcessing/Copy"
{
    HLSLINCLUDE

        #include "../StdLib.hlsl"

        SCREENSPACE_TEXTURE_SAMPLER(_MainTex, sampler_MainTex);

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            float4 color = SAMPLE_SCREENSPACE_TEXTURE(_MainTex, sampler_MainTex, i.texcoordStereo);
            return color;
        }

        float4 FragKillNaN(VaryingsDefault i) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            float4 color = SAMPLE_SCREENSPACE_TEXTURE(_MainTex, sampler_MainTex, i.texcoordStereo);

            if (AnyIsNan(color))
            {
                color = (0.0).xxxx;
            }

            return color;
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0 - Fullscreen triangle copy
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

            ENDHLSL
        }

        // 1 - Fullscreen triangle copy + NaN killer
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragKillNaN

            ENDHLSL
        }
    }
}
