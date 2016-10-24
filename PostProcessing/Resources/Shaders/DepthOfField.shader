Shader "Hidden/Post FX/Depth Of Field"
{
    Properties
    {
        _MainTex ("", 2D) = "black"
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // (0) Downsampling, prefiltering & CoC
        Pass
        {
            CGPROGRAM
                #pragma vertex VertDOF
                #pragma fragment FragPrefilter
                #pragma target 3.0
                #pragma multi_compile __ DEJITTER_DEPTH
                #include "DepthOfField.cginc"
            ENDCG
        }

        // (1-4) Bokeh filter with disk-shaped kernels
        Pass
        {
            CGPROGRAM
                #pragma vertex VertDOF
                #pragma fragment FragBlur
                #define KERNEL_SMALL
                #include "DepthOfField.cginc"
            ENDCG
        }

        Pass
        {
            CGPROGRAM
                #pragma vertex VertDOF
                #pragma fragment FragBlur
                #define KERNEL_MEDIUM
                #include "DepthOfField.cginc"
            ENDCG
        }

        Pass
        {
            CGPROGRAM
                #pragma vertex VertDOF
                #pragma fragment FragBlur
                #define KERNEL_LARGE
                #include "DepthOfField.cginc"
            ENDCG
        }

        Pass
        {
            CGPROGRAM
                #pragma vertex VertDOF
                #pragma fragment FragBlur
                #define KERNEL_VERYLARGE
                #include "DepthOfField.cginc"
            ENDCG
        }
    }

    FallBack Off
}
