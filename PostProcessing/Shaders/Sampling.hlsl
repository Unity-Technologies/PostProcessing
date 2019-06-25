#ifndef UNITY_PPSM_SAMPLING
#define UNITY_PPSM_SAMPLING

#include "StdLib.hlsl"

// Standard box filtering
// 下采样，取上下左右四点的平均值
half4 DownsampleBox4Tap(TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize)
{
    float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0);

    half4 s;
    s =  (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xy));
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.zy));
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xw));
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.zw));

    return s * (1.0 / 4.0);
}

// 9-tap bilinear upsampler (tent filter)
// 上采样，取上下左右，左上，右上，左下，右下，中间9点的不同权重之和
half4 UpsampleTent(TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize, float4 sampleScale)
{
    float4 d = texelSize.xyxy * float4(1.0, 1.0, -1.0, 0.0) * sampleScale;

    half4 s;
    s =  SAMPLE_TEXTURE2D(tex, samplerTex, uv - d.xy);
    s += SAMPLE_TEXTURE2D(tex, samplerTex, uv - d.wy) * 2.0;
    s += SAMPLE_TEXTURE2D(tex, samplerTex, uv - d.zy);

    s += SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.zw) * 2.0;
    s += SAMPLE_TEXTURE2D(tex, samplerTex, uv       ) * 4.0;
    s += SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xw) * 2.0;

    s += SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.zy);
    s += SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.wy) * 2.0;
    s += SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xy);

    return s * (1.0 / 16.0);
}

// Standard box filtering
// 上采样 取上下左右4点的平均值
half4 UpsampleBox(TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize, float4 sampleScale)
{
    float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * (sampleScale * 0.5);

    half4 s;
    s =  (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xy));
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.zy));
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xw));
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.zw));

    return s * (1.0 / 4.0);
}

#endif // UNITY_PPSM_SAMPLING
