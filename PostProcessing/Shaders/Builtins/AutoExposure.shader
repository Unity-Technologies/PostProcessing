Shader "Hidden/PostProcessing/AutoExposure"
{
    HLSLINCLUDE

        #pragma target 4.5
        #include "../StdLib.hlsl"
        #include "ExposureHistogram.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float4 _Params; // x: lowPercent, y: highPercent, z: minBrightness, w: maxBrightness
        float2 _Speed; // x: down, y: up
        float4 _ScaleOffsetRes; // x: scale, y: offset, w: histogram pass width, h: histogram pass height
        float _ExposureCompensation;

        StructuredBuffer<uint> _HistogramBuffer;

        float GetExposureMultiplier(float avgLuminance)
        {
            avgLuminance = max(EPSILON, avgLuminance);
            //float keyValue = 1.03 - (2.0 / (2.0 + log2(avgLuminance + 1.0)));
            float keyValue = _ExposureCompensation;
            float exposure = keyValue / avgLuminance;
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
            float maxValue = 1.0 / FindMaxHistogramValue(_HistogramBuffer);
            float avgLuminance = GetAverageLuminance(_HistogramBuffer, _Params, maxValue, _ScaleOffsetRes.xy);
            float exposure = GetExposureMultiplier(avgLuminance);
            float prevExposure = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, (0.5).xx).r;
            exposure = InterpolateExposure(exposure, prevExposure);
            return exposure.xxxx;
        }

        float4 FragAdaptFixed(VaryingsDefault i) : SV_Target
        {
            float maxValue = 1.0 / FindMaxHistogramValue(_HistogramBuffer);
            float avgLuminance = GetAverageLuminance(_HistogramBuffer, _Params, maxValue, _ScaleOffsetRes.xy);
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
