Shader "Hidden/PostProcessing/FinalPass"
{
    HLSLINCLUDE

        #pragma multi_compile __ UNITY_COLORSPACE_GAMMA
        #pragma multi_compile __ FXAA FXAA_LOW
        #include "../StdLib.hlsl"
        #include "../Colors.hlsl"

        // PS3 and XBOX360 aren't supported in Unity anymore, only use the PC variant
        #define FXAA_PC 1

        // Luma is encoded in alpha after the first Uber pass
        #define FXAA_GREEN_AS_LUMA 0

        #if FXAA_LOW
            #define FXAA_QUALITY__PRESET 28
            #define FXAA_QUALITY_SUBPIX 1.0
            #define FXAA_QUALITY_EDGE_THRESHOLD 0.125
            #define FXAA_QUALITY_EDGE_THRESHOLD_MIN 0.0625
        #else
            #define FXAA_QUALITY__PRESET 39
            #define FXAA_QUALITY_SUBPIX 1.0
            #define FXAA_QUALITY_EDGE_THRESHOLD 0.063
            #define FXAA_QUALITY_EDGE_THRESHOLD_MIN 0.0312
        #endif

        #include "FastApproximateAntialiasing.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float4 _MainTex_TexelSize;

        // Dithering
        TEXTURE2D_SAMPLER2D(_DitheringTex, sampler_DitheringTex);
        float4 _Dithering_Coords;

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            half4 color = 0.0;
            float2 uvSPR = UnityStereoTransformScreenSpaceTex(i.texcoord);

            // Fast Approximate Anti-aliasing
            #if FXAA || FXAA_LOW
            {
                #if FXAA_HLSL_4 || FXAA_HLSL_5
                    FxaaTex mainTex;
                    mainTex.tex = _MainTex;
                    mainTex.smpl = sampler_MainTex;
                #else
                    FxaaTex mainTex = _MainTex;
                #endif

                color = FxaaPixelShader(
                    i.texcoord,                 // pos
                    0.0,                        // fxaaConsolePosPos (unused)
                    mainTex,                    // tex
                    mainTex,                    // fxaaConsole360TexExpBiasNegOne (unused)
                    mainTex,                    // fxaaConsole360TexExpBiasNegTwo (unused)
                    _MainTex_TexelSize.xy,      // fxaaQualityRcpFrame
                    0.0,                        // fxaaConsoleRcpFrameOpt (unused)
                    0.0,                        // fxaaConsoleRcpFrameOpt2 (unused)
                    0.0,                        // fxaaConsole360RcpFrameOpt2 (unused)
                    FXAA_QUALITY_SUBPIX,
                    FXAA_QUALITY_EDGE_THRESHOLD,
                    FXAA_QUALITY_EDGE_THRESHOLD_MIN,
                    0.0,                        // fxaaConsoleEdgeSharpness (unused)
                    0.0,                        // fxaaConsoleEdgeThreshold (unused)
                    0.0,                        // fxaaConsoleEdgeThresholdMin (unused)
                    0.0                         // fxaaConsole360ConstDir (unused)
                );
            }
            #else
            {
                color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvSPR);
            }
            #endif

            {
                // Final dithering
                // Symmetric triangular distribution on [-1,1] with maximal density at 0
                float noise = SAMPLE_TEXTURE2D(_DitheringTex, sampler_DitheringTex, i.texcoord * _Dithering_Coords.xy + _Dithering_Coords.zw).a * 2.0 - 1.0;
                noise = sign(noise) * (1.0 - sqrt(1.0 - abs(noise)));

                #if UNITY_COLORSPACE_GAMMA
                    color.rgb += noise / 255.0;
                #else
                    color.rgb = SRGBToLinear(LinearToSRGB(color.rgb) + noise / 255.0);
                #endif
            }

            return float4(color.rgb, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag
                #pragma target 5.0

            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefaultNoFlip
                #pragma fragment Frag
                #pragma target 5.0

            ENDHLSL
        }
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefaultNoFlip
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
