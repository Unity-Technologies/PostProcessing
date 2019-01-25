#ifndef UNITY_POSTFX_EXPOSURE_HISTOGRAM
#define UNITY_POSTFX_EXPOSURE_HISTOGRAM

// Don't forget to update 'LogHistogram.cs' if you change these values !
#define HISTOGRAM_BINS          128
#define HISTOGRAM_TEXELS        HISTOGRAM_BINS / 4

#if SHADER_API_GLES3 || SHADER_API_METAL
    #define HISTOGRAM_THREAD_X              8
    #define HISTOGRAM_THREAD_Y              8
    #define HISTOGRAM_REDUCTION_THREAD_X    8
    #define HISTOGRAM_REDUCTION_THREAD_Y    8
    #define HISTOGRAM_REDUCTION_ALT_PATH    1
#else
    #define HISTOGRAM_THREAD_X              16
    #define HISTOGRAM_THREAD_Y              16
    #define HISTOGRAM_REDUCTION_THREAD_X    HISTOGRAM_THREAD_X
    #define HISTOGRAM_REDUCTION_THREAD_Y    HISTOGRAM_BINS / HISTOGRAM_THREAD_Y
    #define HISTOGRAM_REDUCTION_ALT_PATH    0
#endif

#define HISTOGRAM_REDUCTION_BINS HISTOGRAM_REDUCTION_THREAD_X * HISTOGRAM_REDUCTION_THREAD_Y

float GetHistogramBinFromLuminance(float value, float2 scaleOffset)
{
    return saturate(log2(value) * scaleOffset.x + scaleOffset.y);
}

float GetLuminanceFromHistogramBin(float bin, float2 scaleOffset)
{
    return exp2((bin - scaleOffset.y) / scaleOffset.x);
}

float GetBinValue(StructuredBuffer<uint> buffer, uint index, float maxHistogramValue)
{
    return float(buffer[index]) * maxHistogramValue;
}

float FindMaxHistogramValue(StructuredBuffer<uint> buffer)
{
    uint maxValue = 0u;

    for (uint i = 0; i < HISTOGRAM_BINS; i++)
    {
        uint h = buffer[i];
        maxValue = max(maxValue, h);
    }

    return float(maxValue);
}

void FilterLuminance(StructuredBuffer<uint> buffer, uint i, float maxHistogramValue, float2 scaleOffset, inout float4 filter)
{
    float binValue = GetBinValue(buffer, i, maxHistogramValue);

    // Filter dark areas
    float offset = min(filter.z, binValue);
    binValue -= offset;
    filter.zw -= offset.xx;

    // Filter highlights
    binValue = min(filter.w, binValue);
    filter.w -= binValue;

    // Luminance at the bin
    float luminance = GetLuminanceFromHistogramBin(float(i) / float(HISTOGRAM_BINS), scaleOffset);

    filter.xy += float2(luminance * binValue, binValue);
}

float GetAverageLuminance(StructuredBuffer<uint> buffer, float4 params, float maxHistogramValue, float2 scaleOffset)
{
    // Sum of all bins
    uint i;
    float totalSum = 0.0;

    UNITY_UNROLL
    for (i = 0; i < HISTOGRAM_BINS; i++)
        totalSum += GetBinValue(buffer, i, maxHistogramValue);

    // Skip darker and lighter parts of the histogram to stabilize the auto exposure
    // x: filtered sum
    // y: accumulator
    // zw: fractions
    float4 filter = float4(0.0, 0.0, totalSum * params.xy);

    UNITY_UNROLL
    for (i = 0; i < HISTOGRAM_BINS; i++)
        FilterLuminance(buffer, i, maxHistogramValue, scaleOffset, filter);

    // Clamp to user brightness range
    return clamp(filter.x / max(filter.y, EPSILON), params.z, params.w);
}

#endif // UNITY_POSTFX_EXPOSURE_HISTOGRAM
