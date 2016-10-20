Shader "Hidden/Post FX/Uber Shader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AutoExposure ("", 2D) = "" {}
        _BloomTex ("", 2D) = "" {}
        _Bloom_DirtTex ("", 2D) = "" {}
        _GrainTex ("", 2D) = "" {}
        _LogLut ("", 2D) = "" {}
        _UserLut ("", 2D) = "" {}
        _Vignette_Mask ("", 2D) = "" {}
        _ChromaticAberration_Spectrum ("", 2D) = "" {}
    }

    CGINCLUDE

        #pragma target 3.0

        #pragma multi_compile __ UNITY_COLORSPACE_GAMMA
        #pragma multi_compile __ EYE_ADAPTATION
        #pragma multi_compile __ CHROMATIC_ABERRATION
        #pragma multi_compile __ BLOOM
        #pragma multi_compile __ BLOOM_LENS_DIRT
        #pragma multi_compile __ COLOR_GRADING COLOR_GRADING_LOG_VIEW
        #pragma multi_compile __ USER_LUT
        #pragma multi_compile __ GRAIN
        #pragma multi_compile __ VIGNETTE_CLASSIC VIGNETTE_ROUND VIGNETTE_MASKED
		#pragma multi_compile __ SHARPEN

        #include "UnityCG.cginc"
        #include "Bloom.cginc"
        #include "ColorGrading.cginc"

        // Auto exposure / eye adaptation
        sampler2D _AutoExposure;

        // Chromatic aberration
        half _ChromaticAberration_Amount;
        sampler2D _ChromaticAberration_Spectrum;

        // Bloom
        sampler2D _BloomTex;
        float4 _BloomTex_TexelSize;
        half2 _Bloom_Settings; // x: sampleScale, y: bloom.intensity

        sampler2D _Bloom_DirtTex;
        half _Bloom_DirtIntensity;

        // Color grading & tonemapping
        sampler2D _LogLut;
        half3 _LogLut_Params; // x: 1 / lut_width, y: 1 / lut_height, z: lut_height - 1
        half _ExposureEV; // EV (exp2)

        // User lut
        sampler2D _UserLut;
        half4 _UserLut_Params; // @see _LogLut_Params

        // Grain
        half4 _Grain_Params1; // x: lum_contrib, y: intensityR, z: intensityG, w: intensityB
        half4 _Grain_Params2; // x: xscale, h: yscale, z: xoffset, w: yoffset
        sampler2D _GrainTex;

        // Vignette
        half3 _Vignette_Color;
        half2 _Vignette_Center; // UV space
        half3 _Vignette_Settings; // x: intensity, y: smoothness, z: roundness
        sampler2D _Vignette_Mask;
        half _Vignette_Opacity; // [0;1]

		// Sharpen
		sampler2D _SharpenTex;
		half3 _SharpenSettings;

        struct VaryingsFlipped
        {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
            float2 uvSPR : TEXCOORD1; // Single Pass Stereo UVs
            float2 uvFlipped : TEXCOORD2; // Flipped UVs (DX/MSAA/Forward)
            float2 uvFlippedSPR : TEXCOORD3; // Single Pass Stereo flipped UVs
        };

        VaryingsFlipped VertUber(AttributesDefault v)
        {
            VaryingsFlipped o;
            o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
            o.uv = v.texcoord.xy;
            o.uvSPR = UnityStereoScreenSpaceUVAdjust(v.texcoord.xy, _MainTex_ST);
            o.uvFlipped = v.texcoord.xy;

        #if UNITY_UV_STARTS_AT_TOP
            if (_MainTex_TexelSize.y < 0.0)
                o.uvFlipped.y = 1.0 - o.uvFlipped.y;
        #endif

            o.uvFlippedSPR = UnityStereoScreenSpaceUVAdjust(o.uvFlipped, _MainTex_ST);

            return o;
        }

        half4 FragUber(VaryingsFlipped i) : SV_Target
        {
            float2 uv = i.uv;
            half autoExposure = 1.0;

            // Store the auto exposure value for later
            #if EYE_ADAPTATION
            {
                autoExposure = tex2D(_AutoExposure, uv).r;
            }
            #endif

            half3 color = (0.0).xxx;

            //
            // HDR effects
            // ---------------------------------------------------------

            // Chromatic Aberration
            // Inspired by the method described in "Rendering Inside" [Playdead 2016]
            // https://twitter.com/pixelmager/status/717019757766123520
            // TODO: Take advantage of TAA to get even smoother results
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
                    half3 s = tex2Dlod(_MainTex, float4(UnityStereoScreenSpaceUVAdjust(pos, _MainTex_ST), 0, 0)).rgb;
                    half3 filter = tex2Dlod(_ChromaticAberration_Spectrum, float4(t, 0, 0, 0)).rgb;

                    sum += s * filter;
                    filterSum += filter;
                    pos += delta;
                }

                color = sum / filterSum;
            }
            #else
            {
                color = tex2D(_MainTex, i.uvSPR).rgb;
            }
            #endif

			#if SHARPEN
			color.rgb += (color.rgb - tex2D(_SharpenTex, uv).rgb) * _SharpenSettings.x;
			#endif

            // Apply auto exposure if any
            color *= autoExposure;

            // Gamma space... Gah.
            #if UNITY_COLORSPACE_GAMMA
            {
                color = GammaToLinearSpace(color);
            }
            #endif

            // HDR Bloom
            #if BLOOM
            {
                half3 bloom = UpsampleFilter(_BloomTex, i.uvFlippedSPR, _BloomTex_TexelSize.xy, _Bloom_Settings.x) * _Bloom_Settings.y;
                color += bloom;

                #if BLOOM_LENS_DIRT
                {
                    half3 dirt = tex2D(_Bloom_DirtTex, i.uvFlipped).rgb * _Bloom_DirtIntensity;
                    color += bloom * dirt;
                }
                #endif
            }
            #endif

            // Procedural vignette
            #if VIGNETTE_CLASSIC
            {
                half2 d = abs(uv - _Vignette_Center) * _Vignette_Settings.x;
                d = pow(d, _Vignette_Settings.z); // Roundness
                half vfactor = pow(saturate(1.0 - dot(d, d)), _Vignette_Settings.y);
                color *= lerp(_Vignette_Color, (1.0).xxx, vfactor);
            }

            // Perfectly round vignette
            #elif VIGNETTE_ROUND
            {
                half2 d = abs(uv - _Vignette_Center) * _Vignette_Settings.x;
                d.x *= _ScreenParams.x / _ScreenParams.y;
                half vfactor = pow(saturate(1.0 - dot(d, d)), _Vignette_Settings.y);
                color *= lerp(_Vignette_Color, (1.0).xxx, vfactor);
            }

            // Masked vignette
            #elif VIGNETTE_MASKED
            {
                half vfactor = tex2D(_Vignette_Mask, uv).a;
                half3 new_color = color * lerp(_Vignette_Color, (1.0).xxx, vfactor);
                color = lerp(color, new_color, _Vignette_Opacity);
            }
            #endif

            // HDR color grading & tonemapping
            #if COLOR_GRADING
            {
                color *= _ExposureEV; // Exposure is in ev units (or 'stops')

                half3 colorLogC = saturate(LinearToLogC(color));
                color = ApplyLut2d(_LogLut, colorLogC, _LogLut_Params);
            }
            #elif COLOR_GRADING_LOG_VIEW
            {
                color *= _ExposureEV;
                color = saturate(LinearToLogC(color));
            }
            #endif

            //
            // All the following effects happen in LDR
            // ---------------------------------------------------------

            color = saturate(color);

            // Grain / dithering
            #if (GRAIN)
            {
                float3 grain = tex2D(_GrainTex, uv * _Grain_Params2.xy + _Grain_Params2.zw).rgb;

                // Noisiness response curve based on scene luminance
                float luminance = lerp(0.0, AcesLuminance(color), _Grain_Params1.x);
                float lum = smoothstep(0.2, 0.0, luminance) + luminance;

                grain = lerp(grain, 0.0, Pow4(lum));
                color += grain * _Grain_Params1.yzw;
            }
            #endif

            // Back to gamma space if needed
            #if UNITY_COLORSPACE_GAMMA
            {
                color = LinearToGammaSpace(color);
            }
            #endif

            // LDR user lut
            #if USER_LUT
            {
                color = saturate(color);
                half3 colorGraded;

                #if !UNITY_COLORSPACE_GAMMA
                {
                    colorGraded = ApplyLut2d(_UserLut, LinearToGammaSpace(color), _UserLut_Params.xyz);
                    colorGraded = GammaToLinearSpace(colorGraded);
                }
                #else
                {
                    colorGraded = ApplyLut2d(_UserLut, color, _UserLut_Params.xyz);
                }
                #endif

                color = lerp(color, colorGraded, _UserLut_Params.w);
            }
            #endif

            // Done !
            return half4(color, 1.0);
        }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // (0)
        Pass
        {
            CGPROGRAM

                #pragma vertex VertUber
                #pragma fragment FragUber

            ENDCG
        }
    }
}
