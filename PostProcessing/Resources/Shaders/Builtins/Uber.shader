Shader "Hidden/PostProcessing/Uber"
{
    HLSLINCLUDE

        #pragma multi_compile __ UNITY_COLORSPACE_GAMMA
        #pragma multi_compile __ CHROMATIC_ABERRATION
        #pragma multi_compile __ BLOOM
        #pragma multi_compile __ DEPTH_OF_FIELD
        #pragma multi_compile __ COLOR_GRADING
        #pragma multi_compile __ VIGNETTE
        
        #include "../StdLib.hlsl"
        #include "../Colors.hlsl"
        #include "../Sampling.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float4 _MainTex_TexelSize;

        // Auto exposure / eye adaptation
        TEXTURE2D_SAMPLER2D(_AutoExposureTex, sampler_AutoExposureTex);

        // Depth of field
        TEXTURE2D_SAMPLER2D(_DepthOfFieldTex, sampler_DepthOfFieldTex);
        TEXTURE2D_SAMPLER2D(_CoCTex, sampler_CoCTex);
        float4 _DepthOfFieldTex_TexelSize;
        float3 _DepthOfFieldParams; // x: distance, y: f^2 / (N * (S1 - f) * film_width * 2), z: max coc

        // Bloom
        TEXTURE2D_SAMPLER2D(_BloomTex, sampler_BloomTex);
        TEXTURE2D_SAMPLER2D(_Bloom_DirtTex, sampler_Bloom_DirtTex);
        float4 _BloomTex_TexelSize;
        half3 _Bloom_Settings; // x: sampleScale, y: intensity, z: lens intensity

        // Chromatic aberration
        TEXTURE2D_SAMPLER2D(_ChromaticAberration_SpectralLut, sampler_ChromaticAberration_SpectralLut);
        half _ChromaticAberration_Amount;

        // Color grading
        TEXTURE2D_SAMPLER2D(_LogLut, sampler_LogLut);
        float3 _LogLut_Params;

        // Vignette
        half3 _Vignette_Color;
        half2 _Vignette_Center; // UV space
        half4 _Vignette_Settings; // x: intensity, y: smoothness, z: roundness, w: rounded
        half _Vignette_Opacity;
        half _Vignette_Mode; // <0.5: procedural, >=0.5: masked
        TEXTURE2D_SAMPLER2D(_Vignette_Mask, sampler_Vignette_Mask);

        half4 FragUber(VaryingsDefault i) : SV_Target
        {
            //
            // HDR effects
            // ---------------------------------------------------------

            float2 uv = i.texcoord;
            half autoExposure = SAMPLE_TEXTURE2D(_AutoExposureTex, sampler_AutoExposureTex, uv).r;

            half3 color = (0.0).xxx;
            
            #if DEPTH_OF_FIELD && CHROMATIC_ABERRATION
            half4 dof = (0.0).xxxx;
            half ffa = 0.0; // far field alpha
            #endif

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

                #if DEPTH_OF_FIELD
                float2 dofDelta = delta;
                float2 dofPos = pos;
                if (_MainTex_TexelSize.y < 0.0)
                {
                    dofDelta.y = -dofDelta.y;
                    dofPos.y = 1.0 - dofPos.y;
                }
                half4 dofSum = (0.0).xxxx;
                half ffaSum = 0.0;
                #endif

                for (int i = 0; i < samples; i++)
                {
                    half t = (i + 0.5) / samples;
                    half3 s = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, pos, 0).rgb;
                    half3 filter = SAMPLE_TEXTURE2D_LOD(_ChromaticAberration_SpectralLut, sampler_ChromaticAberration_SpectralLut, float2(t, 0.0), 0).rgb;

                    sum += s * filter;
                    filterSum += filter;
                    pos += delta;

                    #if DEPTH_OF_FIELD
                    half4 sdof = SAMPLE_TEXTURE2D_LOD(_DepthOfFieldTex, sampler_DepthOfFieldTex, dofPos, 0);
                    half scoc = SAMPLE_TEXTURE2D_LOD(_CoCTex, sampler_CoCTex, dofPos, 0).r;
                    scoc = (scoc - 0.5) * 2.0 * _DepthOfFieldParams.z;
                    dofSum += sdof * half4(filter, 1.0);
                    ffaSum += smoothstep(_MainTex_TexelSize.y * 2.0, _MainTex_TexelSize.y * 4.0, scoc);
                    dofPos += dofDelta;
                    #endif
                }

                color = sum / filterSum;

                #if DEPTH_OF_FIELD
                dof = dofSum / half4(filterSum, samples);
                ffa = ffaSum / samples;
                #endif
            }
            #else
            {
                color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
            }
            #endif

            color *= autoExposure;

            // Gamma space... Gah.
            #if UNITY_COLORSPACE_GAMMA
            {
                color = SRGBToLinear(color);
            }
            #endif

            #if DEPTH_OF_FIELD
            {
                #if !CHROMATIC_ABERRATION
                half4 dof = SAMPLE_TEXTURE2D(_DepthOfFieldTex, sampler_DepthOfFieldTex, uv);
                half coc = SAMPLE_TEXTURE2D(_CoCTex, sampler_CoCTex, uv).r;
                coc = (coc - 0.5) * 2.0 * _DepthOfFieldParams.z;
                // Convert CoC to far field alpha value.
                float ffa = smoothstep(_MainTex_TexelSize.y * 2.0, _MainTex_TexelSize.y * 4.0, coc);
                #endif

                // lerp(lerp(color, dof, ffa), dof, dof.a)
                color = lerp(color, dof.rgb * autoExposure, ffa + dof.a - ffa * dof.a);
            }
            #endif

            #if BLOOM
            {
                half3 bloom = UpsampleTent(TEXTURE2D_PARAM(_BloomTex, sampler_BloomTex), i.texcoord, _BloomTex_TexelSize.xy, _Bloom_Settings.x).rgb * _Bloom_Settings.y;
                color += bloom;

                // Lens dirtiness
                half3 dirt = SAMPLE_TEXTURE2D(_Bloom_DirtTex, sampler_Bloom_DirtTex, i.texcoord).rgb * _Bloom_Settings.z;
                color += bloom * dirt;
            }
            #endif

            #if COLOR_GRADING
            {
                float3 uvw = saturate(LinearToLogC(color));
                color = ApplyLut2D(TEXTURE2D_PARAM(_LogLut, sampler_LogLut), uvw, _LogLut_Params);
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

            //
            // All the following effects happen in LDR
            // ---------------------------------------------------------

            color = saturate(color);

            // Put luma in alpha for FXAA - higher general quality than using green as luma
            // Also used for Grain's response curve in final pass
            half luma = Luminance(color);
            half4 output = half4(color, luma);

            #if UNITY_COLORSPACE_GAMMA
            {
                output = LinearToSRGB(output);
            }
            #endif

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
