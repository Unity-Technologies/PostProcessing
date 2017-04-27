Shader "Hidden/PostProcessing/DebugExposure"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma multi_compile __ AUTO_KEY_VALUE
        #include "../StdLib.hlsl"
        #include "ExposureHistogram.hlsl"

        float4 _Params; // x: lowPercent, y: highPercent, z: minBrightness, w: maxBrightness
        float4 _ScaleOffsetRes; // x: scale, y: offset, w: histogram pass width, h: histogram pass height
        float _OutputWidth;

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

        struct VaryingsHisto
        {
            float4 vertex : SV_POSITION;
            float2 texcoord : TEXCOORD0;
            float maxValue : TEXCOORD1;
            float avgLuminance : TEXCOORD2;
        };

        VaryingsHisto VertHisto(AttributesDefault v)
        {
            VaryingsHisto o;
            o.vertex = float4(v.vertex.xy, 0.0, 1.0);
            o.texcoord = TransformTriangleVertexToUV(v.vertex.xy);
            o.maxValue = 1.0 / FindMaxHistogramValue();
            o.avgLuminance = GetAverageLuminance(o.maxValue);
            return o;
        }

        float4 FragHisto(VaryingsHisto i) : SV_Target
        {
            const float3 kRangeColor = float3(0.05, 0.4, 0.6);
            const float3 kAvgColor = float3(0.8, 0.3, 0.05);

            float4 color = float4(0.0, 0.0, 0.0, 0.6);

            uint ix = (uint)(round(i.texcoord.x * HISTOGRAM_BINS));
            float bin = saturate(float(_HistogramBuffer[ix]) * i.maxValue);
            float fill = step(i.texcoord.y, bin);

            // Min / max brightness markers
            float luminanceMin = GetHistogramBinFromLuminance(_Params.z, _ScaleOffsetRes.xy);
            float luminanceMax = GetHistogramBinFromLuminance(_Params.w, _ScaleOffsetRes.xy);

            color.rgb += fill.rrr;

            if (i.texcoord.x > luminanceMin && i.texcoord.x < luminanceMax)
            {
                color.rgb = fill.rrr * kRangeColor;
                color.rgb += kRangeColor;
            }

            // Current average luminance marker
            float luminanceAvg = GetHistogramBinFromLuminance(i.avgLuminance, _ScaleOffsetRes.xy);
            float avgPx = luminanceAvg * _OutputWidth;

            if (abs(i.vertex.x - avgPx) < 2)
                color.rgb = kAvgColor;

            return color;
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertHisto
                #pragma fragment FragHisto

            ENDHLSL
        }
    }
}
