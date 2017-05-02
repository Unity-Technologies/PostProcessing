Shader "Hidden/PostProcessing/AutoExposure"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma multi_compile __ AUTO_KEY_VALUE
        #include "../StdLib.hlsl"
        #include "ExposureHistogram.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float4 _Params; // x: lowPercent, y: highPercent, z: minBrightness, w: maxBrightness
        float2 _Speed; // x: down, y: up
        float4 _ScaleOffsetRes; // x: scale, y: offset, w: histogram pass width, h: histogram pass height
        float _ExposureCompensation;

        StructuredBuffer<uint> _HistogramBuffer;

        float GetBinValue(uint index, float maxHistogramValue)
        {
            return float(_HistogramBuffer[index]) * maxHistogramValue;
        }

        // Done in the vertex shader
        float FindMaxHistogramValue()
        {
            uint maxValue = 0u;

            for (uint i = 0; i < HISTOGRAM_BINS; i++)
            {
                uint h = _HistogramBuffer[i];
                maxValue = max(maxValue, h);
            }

            return float(maxValue);
        }

        void FilterLuminance(uint i, float maxHistogramValue, inout float4 filter)
        {
            float binValue = GetBinValue(i, maxHistogramValue);

            // Filter dark areas
            float offset = min(filter.z, binValue);
            binValue -= offset;
            filter.zw -= offset.xx;

            // Filter highlights
            binValue = min(filter.w, binValue);
            filter.w -= binValue;

            // Luminance at the bin
            float luminance = GetLuminanceFromHistogramBin(float(i) / float(HISTOGRAM_BINS), _ScaleOffsetRes.xy);

            filter.xy += float2(luminance * binValue, binValue);
        }

        float GetAverageLuminance(float maxHistogramValue)
        {
            // Sum of all bins
            uint i;
            float totalSum = 0.0;

            UNITY_LOOP
            for (i = 0; i < HISTOGRAM_BINS; i++)
                totalSum += GetBinValue(i, maxHistogramValue);

            // Skip darker and lighter parts of the histogram to stabilize the auto exposure
            // x: filtered sum
            // y: accumulator
            // zw: fractions
            float4 filter = float4(0.0, 0.0, totalSum * _Params.xy);

            UNITY_LOOP
            for (i = 0; i < HISTOGRAM_BINS; i++)
                FilterLuminance(i, maxHistogramValue, filter);

            // Clamp to user brightness range
            return clamp(filter.x / max(filter.y, EPSILON), _Params.z, _Params.w);
        }

        float GetExposureMultiplier(float avgLuminance)
        {
            avgLuminance = max(EPSILON, avgLuminance);

        #if AUTO_KEY_VALUE
            half keyValue = 1.03 - (2.0 / (2.0 + log2(avgLuminance + 1.0)));
        #else
            half keyValue = _ExposureCompensation;
        #endif

            half exposure = keyValue / avgLuminance;
            return exposure;
        }

        float InterpolateExposure(float newExposure, float oldExposure)
        {
            float delta = newExposure - oldExposure;
            float speed = delta > 0.0 ? _Speed.x : _Speed.y;
            float exposure = oldExposure + delta * (1.0 - exp2(-unity_DeltaTime.x * speed));
            //float exposure = oldExposure + delta * (unity_DeltaTime.x * speed);
            return exposure;
        }

        float4 FragAdaptProgressive(VaryingsDefault i) : SV_Target
        {
            float maxValue = 1.0 / FindMaxHistogramValue();
            float avgLuminance = GetAverageLuminance(maxValue);
            float exposure = GetExposureMultiplier(avgLuminance);
            float prevExposure = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, (0.5).xx).r;
            exposure = InterpolateExposure(exposure, prevExposure);
            return exposure.xxxx;
        }

        float4 FragAdaptFixed(VaryingsDefault i) : SV_Target
        {
            float maxValue = 1.0 / FindMaxHistogramValue();
            float avgLuminance = GetAverageLuminance(maxValue);
            float exposure = GetExposureMultiplier(avgLuminance);
            return exposure.xxxx;
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragAdaptProgressive

            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragAdaptFixed

            ENDHLSL
        }
    }
}
