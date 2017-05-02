Shader "Hidden/PostProcessing/DepthOfField"
{
    HLSLINCLUDE

        #pragma exclude_renderers d3d11_9x

    ENDHLSL

    // SubShader with SM 5.0 support
    // Gather intrinsics are used to reduce texture sample count.
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass // 0
        {
            Name "CoC Calculation"

            HLSLPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragCoC
                #include "DepthOfField.hlsl"
            ENDHLSL
        }

        Pass // 1
        {
            Name "CoC Temporal Filter"

            HLSLPROGRAM
                #pragma target 5.0
                #pragma vertex VertDefault
                #pragma fragment FragTempFilter
                #include "DepthOfField.hlsl"
            ENDHLSL
        }

        Pass // 2
        {
            Name "Downsample and Prefilter"

            HLSLPROGRAM
                #pragma target 5.0
                #pragma vertex VertDefault
                #pragma fragment FragPrefilter
                #pragma multi_compile __ UNITY_COLORSPACE_GAMMA
                #include "DepthOfField.hlsl"
            ENDHLSL
        }

        Pass // 3
        {
            Name "Bokeh Filter (small)"

            HLSLPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define KERNEL_SMALL
                #include "DepthOfField.hlsl"
            ENDHLSL
        }

        Pass // 4
        {
            Name "Bokeh Filter (medium)"

            HLSLPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define KERNEL_MEDIUM
                #include "DepthOfField.hlsl"
            ENDHLSL
        }

        Pass // 5
        {
            Name "Bokeh Filter (large)"

            HLSLPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define KERNEL_LARGE
                #include "DepthOfField.hlsl"
            ENDHLSL
        }

        Pass // 6
        {
            Name "Bokeh Filter (very large)"

            HLSLPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define KERNEL_VERYLARGE
                #include "DepthOfField.hlsl"
            ENDHLSL
        }

        Pass // 7
        {
            Name "Postfilter"

            HLSLPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragPostBlur
                #include "DepthOfField.hlsl"
            ENDHLSL
        }

        Pass // 8
        {
            Name "Combine"

            HLSLPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragCombine
                #include "DepthOfField.hlsl"
            ENDHLSL
        }
    }

    // Fallback SubShader with SM 3.0
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass // 0
        {
            Name "CoC Calculation"

            HLSLPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragCoC
                #include "DepthOfField.hlsl"
            ENDHLSL
        }

        Pass // 1
        {
            Name "CoC Temporal Filter"

            HLSLPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragTempFilter
                #include "DepthOfField.hlsl"
            ENDHLSL
        }

        Pass // 2
        {
            Name "Downsample and Prefilter"

            HLSLPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragPrefilter
                #pragma multi_compile __ UNITY_COLORSPACE_GAMMA
                #include "DepthOfField.hlsl"
            ENDHLSL
        }

        Pass // 3
        {
            Name "Bokeh Filter (small)"

            HLSLPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define KERNEL_SMALL
                #include "DepthOfField.hlsl"
            ENDHLSL
        }

        Pass // 4
        {
            Name "Bokeh Filter (medium)"

            HLSLPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define KERNEL_MEDIUM
                #include "DepthOfField.hlsl"
            ENDHLSL
        }

        Pass // 5
        {
            Name "Bokeh Filter (large)"

            HLSLPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define KERNEL_LARGE
                #include "DepthOfField.hlsl"
            ENDHLSL
        }

        Pass // 6
        {
            Name "Bokeh Filter (very large)"

            HLSLPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define KERNEL_VERYLARGE
                #include "DepthOfField.hlsl"
            ENDHLSL
        }

        Pass // 7
        {
            Name "Postfilter"

            HLSLPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragPostBlur
                #include "DepthOfField.hlsl"
            ENDHLSL
        }

        Pass // 8
        {
            Name "Combine"

            HLSLPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragCombine
                #include "DepthOfField.hlsl"
            ENDHLSL
        }
    }
}
