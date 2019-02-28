Shader "Hidden/PostProcessing/Editor/SubstanceLutGenerate"
{
    HLSLINCLUDE

        #pragma exclude_renderers gles gles3 d3d11_9x
        #pragma target 4.5
        #include "../StdLib.hlsl"
        #include "../Colors.hlsl"

        // float4 _Params; // x: lowPercent, y: highPercent, z: minBrightness, w: maxBrightness
        // float4 _ScaleOffsetRes; // x: scale, y: offset, w: histogram pass width, h: histogram pass height
        
        TEXTURE3D_SAMPLER3D(_Lut3D, sampler_Lut3D);
        float2 _Lut3D_Params;


        struct VaryingsSubstanceLutGenerate
        {
            float4 vertex : SV_POSITION;
            float2 uv : uv0;
        };

        VaryingsSubstanceLutGenerate Vert(AttributesDefault v)
        {
            VaryingsSubstanceLutGenerate o;
            o.vertex = float4(v.vertex.xy, 0.0, 1.0);
            o.uv = TransformTriangleVertexToUV(v.vertex.xy);

        #if UNITY_UV_STARTS_AT_TOP
            o.uv = o.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
        #endif

            return o;
        }

        float LinearToRec709Scalar(const in float x)
        {
            return (x < 0.018f)
                ? (x * 4.5f)
                : (1.099f * pow(x, 0.4545f) - 0.099f);
        }
        float3 LinearToRec709(const in float3 rgb)
        {
            return float3(
                LinearToRec709Scalar(rgb.r),
                LinearToRec709Scalar(rgb.g),
                LinearToRec709Scalar(rgb.b)
            );
        }

        float3 ReinhardToLinear(const in float3 rgbTonemapped)
        {
            return rgbTonemapped / (1.0f - rgbTonemapped);
        }

        float4 Frag(VaryingsSubstanceLutGenerate i) : SV_Target
        {
            // Substance LUT represents [0, 1] inclusive.
            // Need to remove the half pixel offset to move first pixel uv coordinate down to zero, and slightly scale out the make the uv coordinate of the final pixel up to one.
            const float2 SUBSTANCE_LUT_RESOLUTION = float2(2048.0f, 128.0f);
            float3 sampleLutUVW;

            // In Substance's LUT layout, the red channel horizontally interpolates from [0.0, 1.0] inclusive repeatedly in 64 pixel @ 2048 image resolution (32-slice per image) slice intervals.
            const float SLICE_COUNT = 32.0f;
            const float SLICE_RESOLUTION_X = SUBSTANCE_LUT_RESOLUTION.x / SLICE_COUNT;
            const float R_SCALE = SLICE_RESOLUTION_X / (SLICE_RESOLUTION_X - 1.0f);
            const float R_BIAS = -0.5f / (SLICE_RESOLUTION_X - 1.0f);
            sampleLutUVW.r = frac(i.uv.x * SLICE_COUNT) * R_SCALE + R_BIAS;

            // In Substance's LUT layout, the green channel is vertically interlaced (horizontal bands) of G range [0, 0.5] on even bands, and [0.5, 1.0] on odd bands.
            const float G_SCALE = 0.5f / (SLICE_COUNT - 1.0f);
            bool isEvenBand = ((uint)floor(i.uv.y * SUBSTANCE_LUT_RESOLUTION.y) & 1u) > 0u;
            float g_bias = isEvenBand ? 0.0f : 0.5f;
            sampleLutUVW.g = floor(i.uv.x * SLICE_COUNT) * G_SCALE + g_bias;

            // In Substance's LUT layout, the blue channel interpolates from [0.0, 1.0] vertially, at 2 pixel step sizes (to match each interlaced green even odd pair).
            const float B_SCALE = 1.0f / (SUBSTANCE_LUT_RESOLUTION.y * 0.5f - 1.0f);
            sampleLutUVW.b = floor((1.0f - i.uv.y) * SUBSTANCE_LUT_RESOLUTION.y * 0.5f) * B_SCALE;

            // Inverse reinhard tonemapper:
            sampleLutUVW = ReinhardToLinear(sampleLutUVW);

            sampleLutUVW = saturate(LUT_SPACE_ENCODE(sampleLutUVW));
            float3 sampleLut = ApplyLut3D(TEXTURE3D_PARAM(_Lut3D, sampler_Lut3D), sampleLutUVW, _Lut3D_Params);

            sampleLut = saturate(sampleLut);
            sampleLut = LinearToSRGB(sampleLut);

            return float4(sampleLut.r, sampleLut.g, sampleLut.b, 1.0f);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
