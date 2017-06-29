Shader "Hidden/PostProcessing/AmbientOcclusion"
{
    HLSLINCLUDE

        #pragma target 3.0
        #pragma multi_compile __ UNITY_COLORSPACE_GAMMA

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0 - Occlusion estimation with CameraDepthTexture
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragAO
                #pragma multi_compile _ FOG_LINEAR FOG_EXP FOG_EXP2
                #define SOURCE_DEPTH
                #include "AmbientOcclusion.hlsl"

            ENDHLSL
        }

        // 1 - Occlusion estimation with G-Buffer
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragAO
                #pragma multi_compile _ FOG_LINEAR FOG_EXP FOG_EXP2
                #define SOURCE_GBUFFER
                #include "AmbientOcclusion.hlsl"

            ENDHLSL
        }

        // 2 - Separable blur (horizontal pass) with CameraDepthNormalsTexture
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define SOURCE_DEPTHNORMALS
                #define BLUR_HORIZONTAL
                #define BLUR_SAMPLE_CENTER_NORMAL
                #include "AmbientOcclusion.hlsl"

            ENDHLSL
        }

        // 4 - Separable blur (horizontal pass) with G-Buffer
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define SOURCE_GBUFFER
                #define BLUR_HORIZONTAL
                #define BLUR_SAMPLE_CENTER_NORMAL
                #include "AmbientOcclusion.hlsl"

            ENDHLSL
        }

        // 5 - Separable blur (vertical pass)
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define BLUR_VERTICAL
                #include "AmbientOcclusion.hlsl"

            ENDHLSL
        }

        // 6 - Final composition
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragComposition
                #include "AmbientOcclusion.hlsl"

            ENDHLSL
        }

        // 7: Final composition (ambient only mode)
        Pass
        {
            Blend Zero OneMinusSrcColor, Zero OneMinusSrcAlpha

            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragCompositionGBuffer
                #include "AmbientOcclusion.hlsl"

            ENDHLSL
        }
    }
}
