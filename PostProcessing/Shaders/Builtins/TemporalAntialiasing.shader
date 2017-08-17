Shader "Hidden/PostProcessing/TemporalAntialiasing"
{
    HLSLINCLUDE

        #pragma exclude_renderers gles
        #include "../StdLib.hlsl"
        #include "../Colors.hlsl"

    #if UNITY_VERSION >= 201710
        #define _MainTexSampler sampler_LinearClamp
    #else
        #define _MainTexSampler sampler_MainTex
    #endif

        TEXTURE2D_SAMPLER2D(_MainTex, _MainTexSampler);
        float4 _MainTex_TexelSize;
        float4 _MainTex_ST;

        TEXTURE2D_SAMPLER2D(_HistoryTex, sampler_HistoryTex);
        float4 _HistoryTex_ST;

        TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
        float4 _CameraDepthTexture_TexelSize;
        float4 _CameraDepthTexture_ST;

        TEXTURE2D_SAMPLER2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture);
        float4 _CameraMotionVectorsTexture_ST;

        float2 _Jitter;
        float4 _FinalBlendParameters; // x: static, y: dynamic, z: motion amplification
        float _SharpenParameters;

        float2 GetClosestFragment(float2 uv)
        {
            const float2 k = _CameraDepthTexture_TexelSize.xy;
            // TODO: All the neighborhood sample addresses need to run through UnityStereoClamp
            // probably do something like UnityStereoClamp(bleh, unity_StereoScaleOffset[unity_StereoEyeIndex])
            // or UnityStereoClamp(bleh, _CameraDepthTexture_ST);
            const float4 neighborhood = float4(
                SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv - k),
                SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2(k.x, -k.y)),
                SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2(-k.x, k.y)),
                SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv + k)
            );

        #if defined(UNITY_REVERSED_Z)
            #define COMPARE_DEPTH(a, b) step(b, a)
        #else
            #define COMPARE_DEPTH(a, b) step(a, b)
        #endif

            float3 result = float3(0.0, 0.0, SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv));
            result = lerp(result, float3(-1.0, -1.0, neighborhood.x), COMPARE_DEPTH(neighborhood.x, result.z));
            result = lerp(result, float3( 1.0, -1.0, neighborhood.y), COMPARE_DEPTH(neighborhood.y, result.z));
            result = lerp(result, float3(-1.0,  1.0, neighborhood.z), COMPARE_DEPTH(neighborhood.z, result.z));
            result = lerp(result, float3( 1.0,  1.0, neighborhood.w), COMPARE_DEPTH(neighborhood.w, result.z));

            // TODO: do I need to clamp this?  Should be lerping between clamped coords...
            return (uv + result.xy * k);
        }

        float4 ClipToAABB(float4 color, float3 minimum, float3 maximum)
        {
            // Note: only clips towards aabb center (but fast!)
            float3 center = 0.5 * (maximum + minimum);
            float3 extents = 0.5 * (maximum - minimum);

            // This is actually `distance`, however the keyword is reserved
            float3 offset = color.rgb - center;

            float3 ts = abs(extents / (offset + 0.0001));
            float t = saturate(Min3(ts.x, ts.y, ts.z));
            color.rgb = center + offset * t;
            return color;
        }

        struct OutputSolver
        {
            float4 destination : SV_Target0;
            float4 history     : SV_Target1;
        };

        OutputSolver Solve(float2 motion, float2 texcoord)
        {
            const float2 k = _MainTex_TexelSize.xy;
            float2 uv = texcoord - _Jitter;

            float4 color = SAMPLE_TEXTURE2D(_MainTex, _MainTexSampler, uv);

            // TODO: clamp the coords here
            float4 topLeft = SAMPLE_TEXTURE2D(_MainTex, _MainTexSampler, uv - k * 0.5);
            float4 bottomRight = SAMPLE_TEXTURE2D(_MainTex, _MainTexSampler, uv + k * 0.5);

            float4 corners = 4.0 * (topLeft + bottomRight) - 2.0 * color;

            // Sharpen output
            color += (color - (corners * 0.166667)) * 2.718282 * _SharpenParameters;
            color = max(0.0, color);

            // Tonemap color and history samples
            float4 average = FastTonemap((corners + color) * 0.142857);

            topLeft = FastTonemap(topLeft);
            bottomRight = FastTonemap(bottomRight);

            color = FastTonemap(color);

            // TODO: clamp the coords here
            float4 history = SAMPLE_TEXTURE2D(_HistoryTex, sampler_HistoryTex, texcoord - motion);

            float motionLength = length(motion);
            float2 luma = float2(Luminance(average), Luminance(color));
            //float nudge = 4.0 * abs(luma.x - luma.y);
            float nudge = lerp(4.0, 0.25, saturate(motionLength * 100.0)) * abs(luma.x - luma.y);

            float4 minimum = min(bottomRight, topLeft) - nudge;
            float4 maximum = max(topLeft, bottomRight) + nudge;

            history = FastTonemap(history);

            // Clip history samples
            history = ClipToAABB(history, minimum.xyz, maximum.xyz);

            // Blend method
            float weight = clamp(
                lerp(_FinalBlendParameters.x, _FinalBlendParameters.y, motionLength * _FinalBlendParameters.z),
                _FinalBlendParameters.y, _FinalBlendParameters.x
            );

            color = FastTonemapInvert(lerp(color, history, weight));

            OutputSolver output;
            output.destination = color;
            output.history = color;
            return output;
        }

        OutputSolver FragSolverDilate(VaryingsDefault i)
        {
            float2 adjustedTexCoord = UnityStereoTransformScreenSpaceTex(i.texcoord);

            // TODO: either use TRANSFORM_TEX or UnityStereoTransformScreenSpaceTex on i.texcoord
            //float2 closest = GetClosestFragment(i.texcoord);
            float2 closest = GetClosestFragment(adjustedTexCoord);

            // TODO: since closest is based off the transformed tex coord, we should be ok
            float2 motion = SAMPLE_TEXTURE2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture, closest).xy;

            // TODO: again, either use TRANSFORM_TEX or UnityStereoTransformScreenSpaceTex on i.texcoord
            //return Solve(motion, i.texcoord);
            return Solve(motion, adjustedTexCoord);
        }

        OutputSolver FragSolverNoDilate(VaryingsDefault i)
        {
            float2 adjustedTexCoord = UnityStereoTransformScreenSpaceTex(i.texcoord);

            // Don't dilate in ortho !
            // TODO: either use TRANSFORM_TEX or UnityStereoTransformScreenSpaceTex on i.texcoord
            //float2 motion = SAMPLE_TEXTURE2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture, i.texcoord).xy;

            // TODO: again, either use TRANSFORM_TEX or UnityStereoTransformScreenSpaceTex on i.texcoord
            //return Solve(motion, i.texcoord);

            float2 motion = SAMPLE_TEXTURE2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture, adjustedTexCoord).xy;
            return Solve(motion, adjustedTexCoord);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0: Perspective
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragSolverDilate

            ENDHLSL
        }

        // 1: Ortho
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragSolverNoDilate

            ENDHLSL
        }
    }
}
