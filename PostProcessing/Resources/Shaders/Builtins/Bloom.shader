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
        float _Response;

        half4 FragPrefilter(VaryingsDefault i) : SV_Target
        {
            half autoExposure = SAMPLE_TEXTURE2D(_AutoExposureTex, sampler_AutoExposureTex, i.texcoord).r;
            half3 color = Downsample13Tap(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy);
            color *= autoExposure;

            // Pixel brightness
            half br = Max3(color.r, color.g, color.b);

            // Under-threshold part: quadratic curve
            half rq = clamp(br - _Curve.x, 0.0, _Curve.y);
            rq = _Curve.z * rq * rq;

            // Combine and apply the brightness response curve.
            color *= max(rq, br - _Threshold) / max(br, 1e-5);

            return half4(color, 1.0);
        }

        half4 FragDownsample(VaryingsDefault i) : SV_Target
        {
            half3 color = Downsample13Tap(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy);
            return half4(color, 1.0);
        }

        half4 FragUpsample(VaryingsDefault i) : SV_Target
        {
            half3 bloom = UpsampleTent(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy, _SampleScale);
            half3 color = SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, i.texcoord).rgb;
            return half4(bloom * _Response + color, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0: Prefilter
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragPrefilter

            ENDHLSL
        }

        // 1: Downsample
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragDownsample

            ENDHLSL
        }

        // 2: Upsample
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragUpsample

            ENDHLSL
        }
    }
}
