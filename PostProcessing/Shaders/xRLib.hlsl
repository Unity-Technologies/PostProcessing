// VR/AR/xR lib

#ifndef UNITY_POSTFX_XRLIB
#define UNITY_POSTFX_XRLIB

#if defined(UNITY_SINGLE_PASS_STEREO)
CBUFFER_START(UnityStereoGlobals)
    float4x4 unity_StereoMatrixP[2];
    float4x4 unity_StereoMatrixV[2];
    float4x4 unity_StereoMatrixInvV[2];
    float4x4 unity_StereoMatrixVP[2];

    float4x4 unity_StereoCameraProjection[2];
    float4x4 unity_StereoCameraInvProjection[2];
    float4x4 unity_StereoWorldToCamera[2];
    float4x4 unity_StereoCameraToWorld[2];

    float3 unity_StereoWorldSpaceCameraPos[2];
    float4 unity_StereoScaleOffset[2];
CBUFFER_END

CBUFFER_START(UnityStereoEyeIndex)
    int unity_StereoEyeIndex;
CBUFFER_END
#endif

float2 UnityStereoScreenSpaceUVAdjust(float2 uv, float4 scaleAndOffset)
{
    return uv.xy * scaleAndOffset.xy + scaleAndOffset.zw;
}

float4 UnityStereoScreenSpaceUVAdjust(float4 uv, float4 scaleAndOffset)
{
    return float4(UnityStereoScreenSpaceUVAdjust(uv.xy, scaleAndOffset), UnityStereoScreenSpaceUVAdjust(uv.zw, scaleAndOffset));
}

#if defined(UNITY_SINGLE_PASS_STEREO)
float2 TransformStereoScreenSpaceTex(float2 uv, float w)
{
    float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
    return uv.xy * scaleOffset.xy + scaleOffset.zw * w;
}

float2 UnityStereoTransformScreenSpaceTex(float2 uv)
{
    return TransformStereoScreenSpaceTex(saturate(uv), 1.0);
}

float4 UnityStereoTransformScreenSpaceTex(float4 uv)
{
    return float4(UnityStereoTransformScreenSpaceTex(uv.xy), UnityStereoTransformScreenSpaceTex(uv.zw));
}

float2 UnityStereoClampScaleOffset(float2 uv, float4 scaleAndOffset)
{
    return float2(clamp(uv.x, scaleAndOffset.z, scaleAndOffset.z + scaleAndOffset.x), uv.y);
}

float2 UnityStereoClamp(float2 uv)
{
    return UnityStereoClampScaleOffset(uv, unity_StereoScaleOffset[unity_StereoEyeIndex]);
}
#else
#define TransformStereoScreenSpaceTex(uv, w) uv
#define UnityStereoTransformScreenSpaceTex(uv) uv
#define UnityStereoClampScaleOffset(uv, scaleAndOffset) uv
#define UnityStereoClamp(uv) uv
#endif

#endif // UNITY_POSTFX_XRLIB
