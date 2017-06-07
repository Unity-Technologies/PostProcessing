Shader "Hidden/PostProcessing/Texture2DLerp"
{
    HLSLINCLUDE

        #include "../StdLib.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex); // From
        TEXTURE2D_SAMPLER2D(_To, sampler_To);
        float _Interp;

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            float4 from = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
            float4 to = SAMPLE_TEXTURE2D(_To, sampler_To, i.texcoord);
            return lerp(from, to, _Interp);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
