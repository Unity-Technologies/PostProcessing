#ifndef UNITY_POSTFX_SAMPLING
#define UNITY_POSTFX_SAMPLING

#include "StdLib.hlsl"

// Better, temporally stable box filtering
// [Jimenez14] http://goo.gl/eomGso
// . . . . . . .
// . A . B . C .
// . . D . E . .
// . F . G . H .
// . . I . J . .
// . K . L . M .
// . . . . . . .
half3 DownsampleBox13Tap(TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize)
{
    half3 A = SAMPLE_TEXTURE2D(tex, samplerTex, uv + texelSize * float2(-1.0, -1.0)).rgb;
    half3 B = SAMPLE_TEXTURE2D(tex, samplerTex, uv + texelSize * float2( 0.0, -1.0)).rgb;
    half3 C = SAMPLE_TEXTURE2D(tex, samplerTex, uv + texelSize * float2( 1.0, -1.0)).rgb;
    half3 D = SAMPLE_TEXTURE2D(tex, samplerTex, uv + texelSize * float2(-0.5, -0.5)).rgb;
    half3 E = SAMPLE_TEXTURE2D(tex, samplerTex, uv + texelSize * float2( 0.5, -0.5)).rgb;
    half3 F = SAMPLE_TEXTURE2D(tex, samplerTex, uv + texelSize * float2(-1.0,  0.0)).rgb;
    half3 G = SAMPLE_TEXTURE2D(tex, samplerTex, uv                                 ).rgb;
    half3 H = SAMPLE_TEXTURE2D(tex, samplerTex, uv + texelSize * float2( 1.0,  0.0)).rgb;
    half3 I = SAMPLE_TEXTURE2D(tex, samplerTex, uv + texelSize * float2(-0.5,  0.5)).rgb;
    half3 J = SAMPLE_TEXTURE2D(tex, samplerTex, uv + texelSize * float2( 0.5,  0.5)).rgb;
    half3 K = SAMPLE_TEXTURE2D(tex, samplerTex, uv + texelSize * float2(-1.0,  1.0)).rgb;
    half3 L = SAMPLE_TEXTURE2D(tex, samplerTex, uv + texelSize * float2( 0.0,  1.0)).rgb;
    half3 M = SAMPLE_TEXTURE2D(tex, samplerTex, uv + texelSize * float2( 1.0,  1.0)).rgb;

    half2 div = (1.0 / 4.0) * half2(0.5, 0.125);

    half3 o = (D + E + I + J) * div.x;
    o += (A + B + G + F) * div.y;
    o += (B + C + H + G) * div.y;
    o += (F + G + L + K) * div.y;
    o += (G + H + M + L) * div.y;

    return o;
}

// Standard box filtering
half3 DownsampleBox4Tap(TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize)
{
    float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0);

    half3 s;
    s = (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xy)).rgb;
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.zy)).rgb;
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xw)).rgb;
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.zw)).rgb;

    return s * (1.0 / 4.0);
}

// 9-tap bilinear upsampler (tent filter)
half3 UpsampleTent(TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize, float sampleScale)
{
    float4 d = texelSize.xyxy * float4(1.0, 1.0, -1.0, 0.0) * sampleScale;

    half3 s;
    s =  SAMPLE_TEXTURE2D(tex, samplerTex, uv - d.xy).rgb;
    s += SAMPLE_TEXTURE2D(tex, samplerTex, uv - d.wy).rgb * 2.0;
    s += SAMPLE_TEXTURE2D(tex, samplerTex, uv - d.zy).rgb;

    s += SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.zw).rgb * 2.0;
    s += SAMPLE_TEXTURE2D(tex, samplerTex, uv       ).rgb * 4.0;
    s += SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xw).rgb * 2.0;

    s += SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.zy).rgb;
    s += SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.wy).rgb * 2.0;
    s += SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xy).rgb;

    return s * (1.0 / 16.0);
}

// Standard box filtering
half3 UpsampleBox(TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize, float sampleScale)
{
    float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * (sampleScale * 0.5);

    half3 s;
    s = (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xy)).rgb;
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.zy)).rgb;
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xw)).rgb;
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.zw)).rgb;

    return s * (1.0 / 4.0);
}

#endif // UNITY_POSTFX_SAMPLING
