Shader "Hidden/PostProcessing/TemporalAntialiasing"
{
    HLSLINCLUDE
        
        #pragma exclude_renderers gles
        #include "../StdLib.hlsl"
        #include "../Colors.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float4 _MainTex_TexelSize;

        TEXTURE2D_SAMPLER2D(_HistoryTex, sampler_HistoryTex);

        TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
        float4 _CameraDepthTexture_TexelSize;

        TEXTURE2D_SAMPLER2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture);

        float2 _Jitter;
        float4 _FinalBlendParameters; // x: static, y: dynamic, z: motion amplification
        float _SharpenParameters;

        float2 GetClosestFragment(float2 uv)
        {
            const float2 k = _CameraDepthTexture_TexelSize.xy;
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

            return (uv + result.xy * k);
        }

        // Adapted from Playdead's TAA implementation
        // https://github.com/playdeadgames/temporal
        float4 ClipToAABB(float4 color, float4 p, float3 minimum, float3 maximum)
        {
            float4 r = color - p;

            maximum = maximum - p.xyz;
            minimum = minimum - p.xyz;

            if (r.x > maximum.x + 0.00000001)
                r *= (maximum.x / r.x);

            if (r.y > maximum.y + 0.00000001)
                r *= (maximum.y / r.y);

            if (r.z > maximum.z + 0.00000001)
                r *= (maximum.z / r.z);

            if (r.x < minimum.x - 0.00000001)
                r *= (minimum.x / r.x);

            if (r.y < minimum.y - 0.00000001)
                r *= (minimum.y / r.y);

            if (r.z < minimum.z - 0.00000001)
                r *= (minimum.z / r.z);

            return p + r;
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

            float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

            float4 topLeft = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - k * 0.5);
            float4 bottomRight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + k * 0.5);

            float4 corners = 4.0 * (topLeft + bottomRight) - 2.0 * color;

            // Sharpen output
            color += (color - (corners * 0.166667)) * 2.718282 * _SharpenParameters;
            color = max(0.0, color);

            // Tonemap color and history samples
            float4 average = FastTonemap((corners + color) * 0.142857);

            topLeft = FastTonemap(topLeft);
            bottomRight = FastTonemap(bottomRight);

            color = FastTonemap(color);

            float4 history = SAMPLE_TEXTURE2D(_HistoryTex, sampler_HistoryTex, texcoord - motion);

            float motionLength = length(motion);
            float2 luma = float2(Luminance(average), Luminance(color));
            //float nudge = 4.0 * abs(luma.x - luma.y);
            float nudge = lerp(4.0, 0.25, saturate(motionLength * 100.0)) * abs(luma.x - luma.y);

            float4 minimum = min(bottomRight, topLeft) - nudge;
            float4 maximum = max(topLeft, bottomRight) + nudge;

            history = FastTonemap(history);

            // Clip history samples
            history = ClipToAABB(history, clamp(color, minimum, maximum), minimum.xyz, maximum.xyz);

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
            float2 closest = GetClosestFragment(i.texcoord);
            float2 motion = SAMPLE_TEXTURE2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture, closest).xy;
            return Solve(motion, i.texcoord);
        }

        OutputSolver FragSolverNoDilate(VaryingsDefault i)
        {
            // Don't dilate in ortho !
            float2 motion = SAMPLE_TEXTURE2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture, i.texcoord).xy;
            return Solve(motion, i.texcoord);
        }

        float4 FragAlphaClear(VaryingsDefault i) : SV_Target
        {
            float3 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord).rgb;
            return float4(color, 0.0);
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

        // 2: Alpha clear
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragAlphaClear

            ENDHLSL
        }
    }
}
