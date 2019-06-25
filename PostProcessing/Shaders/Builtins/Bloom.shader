Shader "Hidden/PostProcessing/Bloom"
{
    HLSLINCLUDE
        
        #include "../StdLib.hlsl"
        #include "../Colors.hlsl"
        #include "../Sampling.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        TEXTURE2D_SAMPLER2D(_BloomTex, sampler_BloomTex);
        TEXTURE2D_SAMPLER2D(_AutoExposureTex, sampler_AutoExposureTex);

        float4 _MainTex_TexelSize;
        float  _SampleScale;
        float4 _ColorIntensity;
        float4 _Threshold; // x: threshold value (linear), y: threshold - knee, z: knee * 2, w: 0.25 / knee
        float4 _Params; // x: clamp, yzw: unused

        // ----------------------------------------------------------------------------------------
        // Prefilter 前置过滤

        half4 Prefilter(half4 color, float2 uv)
        {
            half autoExposure = SAMPLE_TEXTURE2D(_AutoExposureTex, sampler_AutoExposureTex, uv).r;
            color *= autoExposure;
            color = min(_Params.x, color); // clamp to max
            color = QuadraticThreshold(color, _Threshold.x, _Threshold.yzw);
            return color;
        }

        half4 FragPrefilter4(VaryingsDefault i) : SV_Target
        {
            half4 color = DownsampleBox4Tap(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy);
            return Prefilter(SafeHDR(color), i.texcoord);
        }

        // ----------------------------------------------------------------------------------------
        // Downsample 下采样

        half4 FragDownsample4(VaryingsDefault i) : SV_Target
        {
            half4 color = DownsampleBox4Tap(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy);
            return color;
        }

        // ----------------------------------------------------------------------------------------
        // Upsample & combine 上采样 & 组合

        half4 Combine(half4 bloom, float2 uv)
        {
            half4 color = SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, uv);
            return bloom + color;
        }

        half4 FragUpsampleBox(VaryingsDefault i) : SV_Target
        {
            half4 bloom = UpsampleBox(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy, _SampleScale);
            return Combine(bloom, i.texcoord);
        }

        // ----------------------------------------------------------------------------------------
        // Debug overlays

        half4 FragDebugOverlayThreshold(VaryingsDefault i) : SV_Target
        {
            half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
            return half4(Prefilter(SafeHDR(color), i.texcoord).rgb, 1.0);
        }

        half4 FragDebugOverlayBox(VaryingsDefault i) : SV_Target
        {
            half4 bloom = UpsampleBox(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy, _SampleScale);
            return half4(bloom.rgb * _ColorIntensity.w * _ColorIntensity.rgb, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0: Prefilter 4 taps
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragPrefilter4

            ENDHLSL
        }

        // 1: Downsample 4 taps
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragDownsample4

            ENDHLSL
        }

        // 2: Upsample box filter
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragUpsampleBox

            ENDHLSL
        }

        // 3: Debug overlay (threshold)
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragDebugOverlayThreshold

            ENDHLSL
        }

        // 4: Debug overlay (box filter)
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragDebugOverlayBox

            ENDHLSL
        }
    }
}
