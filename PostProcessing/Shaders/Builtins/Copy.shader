Shader "Hidden/PostProcessing/Copy"
{
    Properties
    {
        _MainTex ("", 2D) = "white" {}
    }

    HLSLINCLUDE

        #include "../StdLib.hlsl"

        struct AttributesClassic
        {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float4 _MainTex_ST;

        VaryingsDefault VertClassic(AttributesClassic v)
        {
            VaryingsDefault o;
            o.vertex = float4(v.vertex.xy * 2.0 - 1.0, 0.0, 1.0);
            o.texcoord = v.texcoord * _MainTex_ST.xy + _MainTex_ST.zw; // We need this for VR

            #if UNITY_UV_STARTS_AT_TOP
            o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
            #endif

            return o;
        }

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
            return color;
        }

        float4 FragKillNaN(VaryingsDefault i) : SV_Target
        {
            float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);

            #if !SHADER_API_GLES
            if (any(isnan(color)) || any(isinf(color)))
            {
                color = (0.0).xxxx;
            }
            #endif

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
