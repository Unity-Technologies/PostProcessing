Shader "Hidden/PostProcessing/Bloom"
{
    HLSLINCLUDE

        #include "../StdLib.hlsl"
        #include "../Sampling.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        TEXTURE2D_SAMPLER2D(_BloomTex, sampler_BloomTex);
        TEXTURE2D_SAMPLER2D(_AutoExposureTex, sampler_AutoExposureTex);

        float4 _MainTex_TexelSize;
        float _SampleScale;
        float _Threshold;
        float3 _Curve;

        half3 Prefilter(half3 color, float2 uv)
        {
            half autoExposure = SAMPLE_TEXTURE2D(_AutoExposureTex, sampler_AutoExposureTex, uv).r;
            color *= autoExposure;

            // Pixel brightness
            half br = Max3(color.r, color.g, color.b);

            // Under-threshold part: quadratic curve
            half rq = clamp(br - _Curve.x, 0.0, _Curve.y);
            rq = _Curve.z * rq * rq;

            // Combine and apply the brightness response curve.
            color *= max(rq, br - _Threshold) / max(br, 1e-5);

            return color;
        }

        half4 FragPrefilter13(VaryingsDefault i) : SV_Target
        {
            half3 color = DownsampleBox13Tap(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy);
            color = Prefilter(color, i.texcoord);
            return half4(color, 1.0);
        }

        half4 FragPrefilter4(VaryingsDefault i) : SV_Target
        {
            half3 color = DownsampleBox4Tap(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy);
            color = Prefilter(color, i.texcoord);
            return half4(color, 1.0);
        }

        half4 FragDownsample13(VaryingsDefault i) : SV_Target
        {
            half3 color = DownsampleBox13Tap(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy);
            return half4(color, 1.0);
        }

        half4 FragDownsample4(VaryingsDefault i) : SV_Target
        {
            half3 color = DownsampleBox4Tap(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy);
            return half4(color, 1.0);
        }

        half4 FragUpsampleTent(VaryingsDefault i) : SV_Target
        {
            half3 bloom = UpsampleTent(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy, _SampleScale);
            half3 color = SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, i.texcoord).rgb;
            return half4(bloom + color, 1.0);
        }

        half4 FragUpsampleBox(VaryingsDefault i) : SV_Target
        {
            half3 bloom = UpsampleBox(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy, _SampleScale);
            half3 color = SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, i.texcoord).rgb;
            return half4(bloom + color, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0: Prefilter 13 taps
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragPrefilter13

            ENDHLSL
        }

        // 1: Prefilter 4 taps
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragPrefilter4

            ENDHLSL
        }

        // 2: Downsample 13 taps
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragDownsample13

            ENDHLSL
        }

        // 3: Downsample 4 taps
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragDownsample4

            ENDHLSL
        }

        // 4: Upsample tent filter
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragUpsampleTent

            ENDHLSL
        }

        // 5: Upsample box filter
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragUpsampleBox

            ENDHLSL
        }
    }
}
