Shader "Hidden/PostProcessing/Copy"
{
    HLSLINCLUDE

        #include "../StdLib.hlsl"

        struct AttributesClassic
        {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        VaryingsDefault VertClassic(AttributesClassic v)
        {
            VaryingsDefault o;
            o.vertex = mul(unity_MatrixVP, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)));
            o.texcoord = v.texcoord;
            return o;
        }

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
            return color;
        }

        float4 FragKillNaN(VaryingsDefault i) : SV_Target
        {
            float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);

            if (any(isnan(color)) || any(isinf(color)))
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

        // 2 - Classic copy
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertClassic
                #pragma fragment Frag

            ENDHLSL
        }

        // 3 - Classic copy + NaN killer
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertClassic
                #pragma fragment FragKillNaN

            ENDHLSL
        }
    }
}
