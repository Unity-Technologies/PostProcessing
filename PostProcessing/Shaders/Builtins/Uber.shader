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
        
        #include "../StdLib.hlsl"
        #include "../Colors.hlsl"
        #include "../Sampling.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float4 _MainTex_TexelSize;

        // Auto exposure / eye adaptation
        TEXTURE2D_SAMPLER2D(_AutoExposureTex, sampler_AutoExposureTex);

        // Bloom
        TEXTURE2D_SAMPLER2D(_BloomTex, sampler_BloomTex);
        TEXTURE2D_SAMPLER2D(_Bloom_DirtTex, sampler_Bloom_DirtTex);
        float4 _BloomTex_TexelSize;
        half3 _Bloom_Settings; // x: sampleScale, y: intensity, z: lens intensity
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

        half4 FragUber(VaryingsDefault i) : SV_Target
        {
            float2 uv = i.texcoord;
            float2 uvSPR = UnityStereoTransformScreenSpaceTex(i.texcoord);
            half autoExposure = SAMPLE_TEXTURE2D(_AutoExposureTex, sampler_AutoExposureTex, uv).r;
            half3 color = (0.0).xxx;

            // Inspired by the method described in "Rendering Inside" [Playdead 2016]
            // https://twitter.com/pixelmager/status/717019757766123520
            #if CHROMATIC_ABERRATION
            {
                float2 coords = 2.0 * uv - 1.0;
                float2 end = uv - coords * dot(coords, coords) * _ChromaticAberration_Amount;

                float2 diff = end - uv;
                int samples = clamp(int(length(_MainTex_TexelSize.zw * diff / 2.0)), 3, 16);
                float2 delta = diff / samples;
                float2 pos = uv;
                half3 sum = (0.0).xxx, filterSum = (0.0).xxx;

                for (int i = 0; i < samples; i++)
                {
                    half t = (i + 0.5) / samples;
                    half3 s = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(pos), 0).rgb;
                    half3 filter = SAMPLE_TEXTURE2D_LOD(_ChromaticAberration_SpectralLut, sampler_ChromaticAberration_SpectralLut, float2(t, 0.0), 0).rgb;

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

                float2 diff = end - uv;
                float2 delta = diff / 3;
                float2 pos = uv;
                half3 sum = (0.0).xxx, filterSum = (0.0).xxx;

                UNITY_UNROLL
                for (int i = 0; i < 3; i++)
                {
                    half t = (i + 0.5) / 3;
                    half3 s = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(pos)).rgb;
                    half3 filter = SAMPLE_TEXTURE2D(_ChromaticAberration_SpectralLut, sampler_ChromaticAberration_SpectralLut, float2(t, 0.0)).rgb;

                    sum += s * filter;
                    filterSum += filter;
                    pos += delta;
                }

                color = sum / filterSum;
            }
            #else
            {
                color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvSPR).rgb;
            }
            #endif

            // Gamma space... Gah.
            #if UNITY_COLORSPACE_GAMMA
            {
                color = SRGBToLinear(color);
            }
            #endif

            color *= autoExposure;

            #if BLOOM
            {
                half3 bloom = UpsampleTent(TEXTURE2D_PARAM(_BloomTex, sampler_BloomTex), uvSPR, _BloomTex_TexelSize.xy, _Bloom_Settings.x).rgb;
                half3 dirt = SAMPLE_TEXTURE2D(_Bloom_DirtTex, sampler_Bloom_DirtTex, i.texcoord).rgb;

                // Additive bloom (artist friendly)
                bloom *= _Bloom_Settings.y;
                dirt *= _Bloom_Settings.z;
                color += bloom * _Bloom_Color;
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
                    color *= lerp(_Vignette_Color, (1.0).xxx, vfactor);
                }
                else
                {
                    half vfactor = SAMPLE_TEXTURE2D(_Vignette_Mask, sampler_Vignette_Mask, uv).a;
                    half3 new_color = color * lerp(_Vignette_Color, (1.0).xxx, vfactor);
                    color = lerp(color, new_color, _Vignette_Opacity);
                }
            }
            #endif

            #if GRAIN
            {
                float3 grain = SAMPLE_TEXTURE2D(_GrainTex, sampler_GrainTex, uvSPR * _Grain_Params2.xy + _Grain_Params2.zw).rgb;

                // Noisiness response curve based on scene luminance
                float lum = 1.0 - sqrt(Luminance(saturate(color)));
                lum = lerp(1.0, lum, _Grain_Params1.x);

                color += color * grain * _Grain_Params1.y * lum;
            }
            #endif

            #if COLOR_GRADING_HDR
            {
                color *= _PostExposure; // Exposure is in ev units (or 'stops')
                float3 colorLutSpace = saturate(LUT_SPACE_ENCODE(color));
                color = ApplyLut3D(TEXTURE3D_PARAM(_Lut3D, sampler_Lut3D), colorLutSpace, _Lut3D_Params);
            }
            #elif COLOR_GRADING_LDR
            {
                color = saturate(color);
                color = ApplyLut2D(TEXTURE2D_PARAM(_Lut2D, sampler_Lut2D), color, _Lut2D_Params);
            }
            #endif

            // Put saturated luma in alpha for FXAA - higher quality than "green as luma" and
            // necessary as RGB values will potentially still be HDR for the FXAA pass
            half luma = Luminance(saturate(color));
            half4 output = half4(color, luma);

            #if UNITY_COLORSPACE_GAMMA
            {
                output = LinearToSRGB(output);
            }
            #endif

            // Output RGB is still HDR at that point (unless range was crunched by a tonemapper)
            // Alpha is luminance of saturate(rgb)
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
    }
}
