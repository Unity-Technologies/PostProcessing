Shader "Hidden/PostProcessing/FinalPass"
{
    HLSLINCLUDE

        #pragma multi_compile __ UNITY_COLORSPACE_GAMMA
        #pragma multi_compile __ FXAA
        #pragma multi_compile __ GRAIN
        #include "../StdLib.hlsl"
        #include "../Colors.hlsl"

        // PS3 and XBOX360 aren't supported in Unity anymore, only use the PC variant
        #define FXAA_PC 1

    #if SHADER_TARGET < 50
        #define FXAA_HLSL_3 1
    #else
        #define FXAA_HLSL_5 1 // Use Gather() on platforms that support it
    #endif

        #define FXAA_QUALITY__PRESET 39
        #define FXAA_GREEN_AS_LUMA 0 // Luma is encoded in alpha after the first Uber pass

        #include "FastApproximateAntialiasing.hlsl"

    #if FXAA_HLSL_5
        Texture2D _MainTex;
        SamplerState sampler_MainTex;
    #else
        sampler2D _MainTex;
    #endif

        float4 _MainTex_TexelSize;

        // Dithering
        TEXTURE2D_SAMPLER2D(_DitheringTex, sampler_DitheringTex);
        float4 _Dithering_Coords;

        // Grain
        TEXTURE2D_SAMPLER2D(_GrainTex, sampler_GrainTex);
        half2 _Grain_Params1; // x: lum_contrib, y: intensity
        half4 _Grain_Params2; // x: xscale, h: yscale, z: xoffset, w: yoffset

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            half4 color = 0.0;

            #if FXAA
            {
            #if FXAA_HLSL_5
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
                    1.0,                        // fxaaQualitySubpix
                    0.063,                      // fxaaQualityEdgeThreshold
                    0.0312,                     // fxaaQualityEdgeThresholdMin
                    0.0,                        // fxaaConsoleEdgeSharpness (unused)
                    0.0,                        // fxaaConsoleEdgeThreshold (unused)
                    0.0,                        // fxaaConsoleEdgeThresholdMin (unused)
                    0.0                         // fxaaConsole360ConstDir (unused)
                );
            }
            #else
            {
            #if FXAA_HLSL_5
                color = _MainTex.Sample(sampler_MainTex, i.texcoord);
            #else
                color = tex2D(_MainTex, i.texcoord);
            #endif
            }
            #endif

            #if GRAIN
            {
                float3 grain = SAMPLE_TEXTURE2D(_GrainTex, sampler_GrainTex, i.texcoord * _Grain_Params2.xy + _Grain_Params2.zw).rgb;

                // Noisiness response curve based on scene luminance
                float lum = 1.0 - sqrt(color.a); // Luma is stored in alpha (see Uber/FXAA)
                lum = lerp(1.0, lum, _Grain_Params1.x);

                color.rgb += color.rgb * grain * _Grain_Params1.y * lum;
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
