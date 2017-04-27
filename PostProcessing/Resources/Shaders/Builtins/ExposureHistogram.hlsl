#ifndef UNITY_POSTFX_EXPOSURE_HISTOGRAM
#define UNITY_POSTFX_EXPOSURE_HISTOGRAM

// Optimal values for PS4/GCN
// Using a group size of 32x32 seems to be a bit faster on Kepler/Maxwell
// Don't forget to update 'AutoExposureRenderer.cs' if you change these values !
#define HISTOGRAM_BINS          64
#define HISTOGRAM_TEXELS        HISTOGRAM_BINS / 4
#define HISTOGRAM_THREAD_X      16
#define HISTOGRAM_THREAD_Y      16

float GetHistogramBinFromLuminance(float value, float2 scaleOffset)
{
    return saturate(log2(value) * scaleOffset.x + scaleOffset.y);
}

float GetLuminanceFromHistogramBin(float bin, float2 scaleOffset)
{
    return exp2((bin - scaleOffset.y) / scaleOffset.x);
}

#endif // UNITY_POSTFX_EXPOSURE_HISTOGRAM
