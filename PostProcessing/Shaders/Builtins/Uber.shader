Shader "Hidden/PostProcessing/Uber"
{
    HLSLINCLUDE

        #pragma target 3.0

        #pragma multi_compile __ UNITY_COLORSPACE_GAMMA
        #pragma multi_compile __ CHROMATIC_ABERRATION CHROMATIC_ABERRATION_LOW
        #pragma multi_compile __ BLOOM
        #pragma multi_compile __ COLOR_GRADING_LDR COLOR_GRADING_HDR
        #pragma multi_compile __ VIGNETTE
        #pragma multi_compile __ GRAIN
        #pragma multi_compile __ FINALPASS
        
        #include "../StdLib.hlsl"
        #include "../Colors.hlsl"
        #include "../Sampling.hlsl"
        #include "Dithering.hlsl"

        #define MAX_CHROMATIC_SAMPLES 16

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float4 _MainTex_TexelSize;

        // Auto exposure / eye adaptation
        TEXTURE2D_SAMPLER2D(_AutoExposureTex, sampler_AutoExposureTex);

        // Bloom
        TEXTURE2D_SAMPLER2D(_BloomTex, sampler_BloomTex);
        TEXTURE2D_SAMPLER2D(_Bloom_DirtTex, sampler_Bloom_DirtTex);
        float4 _BloomTex_TexelSize;
        float4 _Bloom_DirtTileOffset; // xy: tiling, zw: offset
        half3 _Bloom_Settings; // x: sampleScale, y: intensity, z: dirt intensity
        half3 _Bloom_Color;

        // Chromatic aberration
        TEXTURE2D_SAMPLER2D(_ChromaticAberration_SpectralLut, sampler_ChromaticAberration_SpectralLut);
        half _ChromaticAberration_Amount;

        // Color grading
    #if COLOR_GRADING_HDR

        TEXTURE3D_SAMPLER3D(_Lut3D, sampler_Lut3D);
        float2 _Lut3D_Params;

    #elif COLOR_GRADING_LDR

        TEXTURE2D_SAMPLER2D(_Lut2D, sampler_Lut2D);
        float3 _Lut2D_Params;

    #endif

        half _PostExposure; // EV (exp2)

        // Vignette
        half3 _Vignette_Color;
        half2 _Vignette_Center; // UV space
        half4 _Vignette_Settings; // x: intensity, y: smoothness, z: roundness, w: rounded
        half _Vignette_Opacity;
        half _Vignette_Mode; // <0.5: procedural, >=0.5: masked
        TEXTURE2D_SAMPLER2D(_Vignette_Mask, sampler_Vignette_Mask);

        // Grain
        TEXTURE2D_SAMPLER2D(_GrainTex, sampler_GrainTex);
        half2 _Grain_Params1; // x: lum_contrib, y: intensity
        float4 _Grain_Params2; // x: xscale, h: yscale, z: xoffset, w: yoffset

        // Misc
        half _LumaInAlpha;

        half4 FragUber(VaryingsDefault i) : SV_Target
        {
            float2 uv = i.texcoord;
            half autoExposure = SAMPLE_TEXTURE2D(_AutoExposureTex, sampler_AutoExposureTex, uv).r;
            half4 color = (0.0).xxxx;

            // Inspired by the method described in "Rendering Inside" [Playdead 2016]
            // https://twitter.com/pixelmager/status/717019757766123520
            #if CHROMATIC_ABERRATION
            {
                float2 coords = 2.0 * uv - 1.0;
                float2 end = uv - coords * dot(coords, coords) * _ChromaticAberration_Amount;

                float2 diff = end - uv;
                int samples = clamp(int(length(_MainTex_TexelSize.zw * diff / 2.0)), 3, MAX_CHROMATIC_SAMPLES);
                float2 delta = diff / samples;
                float2 pos = uv;
                half4 sum = (0.0).xxxx, filterSum = (0.0).xxxx;

                for (int i = 0; i < samples; i++)
                {
                    half t = (i + 0.5) / samples;
                    half4 s = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(pos), 0);
                    half4 filter = half4(SAMPLE_TEXTURE2D_LOD(_ChromaticAberration_SpectralLut, sampler_ChromaticAberration_SpectralLut, float2(t, 0.0), 0).rgb, 1.0);

                    sum += s * filter;
                    filterSum += filter;
                    pos += delta;
                }

                color = sum / filterSum;
            }
            #elif CHROMATIC_ABERRATION_LOW
            {
                float2 coords = 2.0 * uv - 1.0;
                float2 end = uv - coords * dot(coords, coords) * _ChromaticAberration_Amount;
                float2 delta = (end - uv) / 3;

                half4 filterA = half4(SAMPLE_TEXTURE2D_LOD(_ChromaticAberration_SpectralLut, sampler_ChromaticAberration_SpectralLut, float2(0.5 / 3, 0.0), 0).rgb, 1.0);
                half4 filterB = half4(SAMPLE_TEXTURE2D_LOD(_ChromaticAberration_SpectralLut, sampler_ChromaticAberration_SpectralLut, float2(1.5 / 3, 0.0), 0).rgb, 1.0);
                half4 filterC = half4(SAMPLE_TEXTURE2D_LOD(_ChromaticAberration_SpectralLut, sampler_ChromaticAberration_SpectralLut, float2(2.5 / 3, 0.0), 0).rgb, 1.0);

                half4 texelA = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(uv), 0);
                half4 texelB = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(delta + uv), 0);
                half4 texelC = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(delta * 2.0 + uv), 0);

                half4 sum = texelA * filterA + texelB * filterB + texelC * filterC;
                half4 filterSum = filterA + filterB + filterC;
                color = sum / filterSum;
            }
            #else
            {
                color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);
            }
            #endif

            // Gamma space... Gah.
            #if UNITY_COLORSPACE_GAMMA
            {
                color = SRGBToLinear(color);
            }
            #endif

            color.rgb *= autoExposure;

            #if BLOOM
            {
                half4 bloom = UpsampleTent(TEXTURE2D_PARAM(_BloomTex, sampler_BloomTex), i.texcoord, _BloomTex_TexelSize.xy, _Bloom_Settings.x);
                half4 dirt = half4(SAMPLE_TEXTURE2D(_Bloom_DirtTex, sampler_Bloom_DirtTex, i.texcoord * _Bloom_DirtTileOffset.xy + _Bloom_DirtTileOffset.zw).rgb, 0.0);

                // Additive bloom (artist friendly)
                bloom *= _Bloom_Settings.y;
                dirt *= _Bloom_Settings.z;
                color += bloom * half4(_Bloom_Color, 1.0);
                color += dirt * bloom;
            }
            #endif

            #if VIGNETTE
            {
                if (_Vignette_Mode < 0.5)
                {
                    half2 d = abs(uv - _Vignette_Center) * _Vignette_Settings.x;
                    d.x *= lerp(1.0, _ScreenParams.x / _ScreenParams.y, _Vignette_Settings.w);
                    d = pow(saturate(d), _Vignette_Settings.z); // Roundness
                    half vfactor = pow(saturate(1.0 - dot(d, d)), _Vignette_Settings.y);
                    color.rgb *= lerp(_Vignette_Color, (1.0).xxx, vfactor);
                    color.a = lerp(1.0, color.a, vfactor);
                }
                else
                {
                    half vfactor = SAMPLE_TEXTURE2D(_Vignette_Mask, sampler_Vignette_Mask, uv).a;

                    #if !UNITY_COLORSPACE_GAMMA
                    {
                        vfactor = SRGBToLinear(vfactor);
                    }
                    #endif

                    half3 new_color = color.rgb * lerp(_Vignette_Color, (1.0).xxx, vfactor);
                    color.rgb = lerp(color.rgb, new_color, _Vignette_Opacity);
                    color.a = lerp(1.0, color.a, vfactor);
                }
            }
            #endif

            #if GRAIN
            {
                half3 grain = SAMPLE_TEXTURE2D(_GrainTex, sampler_GrainTex, i.texcoordStereo * _Grain_Params2.xy + _Grain_Params2.zw).rgb;

                // Noisiness response curve based on scene luminance
                float lum = 1.0 - sqrt(Luminance(saturate(color)));
                lum = lerp(1.0, lum, _Grain_Params1.x);

                color.rgb += color.rgb * grain * _Grain_Params1.y * lum;
            }
            #endif

            #if COLOR_GRADING_HDR
            {
                color *= _PostExposure; // Exposure is in ev units (or 'stops')
                float3 colorLutSpace = saturate(LUT_SPACE_ENCODE(color.rgb));
                color.rgb = ApplyLut3D(TEXTURE3D_PARAM(_Lut3D, sampler_Lut3D), colorLutSpace, _Lut3D_Params);
            }
            #elif COLOR_GRADING_LDR
            {
                color = saturate(color);
                color.rgb = ApplyLut2D(TEXTURE2D_PARAM(_Lut2D, sampler_Lut2D), color.rgb, _Lut2D_Params);
            }
            #endif

            half4 output = color;

            #if FINALPASS
            {
                output.rgb = Dither(output.rgb, i.texcoord);
            }
            #else
            {
                if (_LumaInAlpha > 0.5)
                {
                    // Put saturated luma in alpha for FXAA - higher quality than "green as luma" and
                    // necessary as RGB values will potentially still be HDR for the FXAA pass
                    half luma = Luminance(saturate(output));
                    output.a = luma;
                }
            }
            #endif

            #if UNITY_COLORSPACE_GAMMA
            {
                output = LinearToSRGB(output);
            }
            #endif

            // Output RGB is still HDR at that point (unless range was crunched by a tonemapper)
            return output;
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragUber

            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefaultNoFlip
                #pragma fragment FragUber

            ENDHLSL
        }
    }
}
