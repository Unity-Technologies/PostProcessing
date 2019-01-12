Shader "Hidden/PostProcessing/TemporalAntialiasing"
{
    HLSLINCLUDE

        #pragma exclude_renderers gles psp2
        #include "../StdLib.hlsl"
        #include "../Colors.hlsl"

    #if UNITY_VERSION >= 201710
        #define _MainTexSampler sampler_LinearClamp
    #else
        #define _MainTexSampler sampler_MainTex
    #endif

        TEXTURE2D_SAMPLER2D(_MainTex, _MainTexSampler);
        float4 _MainTex_TexelSize;

        TEXTURE2D_SAMPLER2D(_HistoryTex, sampler_HistoryTex);

        TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
        float4 _CameraDepthTexture_TexelSize;

        TEXTURE2D_SAMPLER2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture);

        float2 _Jitter;
        float4 _FinalBlendParameters; // x: static, y: dynamic, z: motion amplification
        float _Sharpness;
        int _ReconstructionFilter;

        #define RECONSTRUCTION_FILTER_UNITY 0
        #define RECONSTRUCTION_FILTER_GAUSSIAN 1
        #define RECONSTRUCTION_FILTER_BLACKMAN_HARRIS 2
        #define RECONSTRUCTION_FILTER_DODGSON_QUADRATIC 3
        #define RECONSTRUCTION_FILTER_MITCHELL 4
        #define RECONSTRUCTION_FILTER_ROUBIDOUX 5
        #define RECONSTRUCTION_FILTER_CATMULL_ROM 6
        #define RECONSTRUCTION_FILTER_LANCZOS_2 7
        #define RECONSTRUCTION_FILTER_LANCZOS_3 8
        #define RECONSTRUCTION_FILTER_LANCZOS_4 9
        #define RECONSTRUCTION_FILTER_LANCZOS_5 10

        float2 GetClosestFragment(float2 uv)
        {
            const float2 k = _CameraDepthTexture_TexelSize.xy;

            const float4 neighborhood = float4(
                SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoClamp(uv - k)),
                SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoClamp(uv + float2(k.x, -k.y))),
                SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoClamp(uv + float2(-k.x, k.y))),
                SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoClamp(uv + k))
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

        float4 ClipToAABB(float4 color, float3 minimum, float3 maximum)
        {
            // Note: only clips towards aabb center (but fast!)
            float3 center = 0.5 * (maximum + minimum);
            float3 extents = 0.5 * (maximum - minimum);

            // This is actually `distance`, however the keyword is reserved
            float3 offset = color.rgb - center;

            float3 ts = abs(extents / (offset + 0.0001));
            float t = saturate(Min3(ts.x, ts.y, ts.z));
            color.rgb = center + offset * t;
            return color;
        }

        float3 YcocgFromRgb(const in float3 rgb)
        {
            float g = rgb.g * 0.5f;
            float rb = rgb.r + rgb.b;

            return float3(
                rb * 0.25f + g,
                0.5f * (rgb.r - rgb.b),
                rb * -0.25f + g
            );
        }

        float3 RgbFromYcocg(const in float3 ycocg)
        {
            float xz = ycocg.x - ycocg.z;

            return float3(
                xz + ycocg.y,
                ycocg.x + ycocg.z,
                xz - ycocg.y
            );
        }

        struct OutputSolver
        {
            float4 destination : SV_Target0;
            float4 history     : SV_Target1;
        };

        OutputSolver Solve(float2 motion, float2 texcoord)
        {
            const float2 k = _MainTex_TexelSize.xy;
            float2 uv = UnityStereoClamp(texcoord - _Jitter);

            float4 color = SAMPLE_TEXTURE2D(_MainTex, _MainTexSampler, uv);

            float4 topLeft = SAMPLE_TEXTURE2D(_MainTex, _MainTexSampler, UnityStereoClamp(uv - k * 0.5));
            float4 bottomRight = SAMPLE_TEXTURE2D(_MainTex, _MainTexSampler, UnityStereoClamp(uv + k * 0.5));

            float4 corners = 4.0 * (topLeft + bottomRight) - 2.0 * color;

            // Sharpen output
            color += (color - (corners * 0.166667)) * 2.718282 * _Sharpness;
            color = clamp(color, 0.0, HALF_MAX_MINUS1);

            // Tonemap color and history samples
            float4 average = (corners + color) * 0.142857;

            float4 history = SAMPLE_TEXTURE2D(_HistoryTex, sampler_HistoryTex, UnityStereoClamp(texcoord - motion));

            float motionLength = length(motion);
            float2 luma = float2(Luminance(average), Luminance(color));
            //float nudge = 4.0 * abs(luma.x - luma.y);
            float nudge = lerp(4.0, 0.25, saturate(motionLength * 100.0)) * abs(luma.x - luma.y);

            float4 minimum = min(bottomRight, topLeft) - nudge;
            float4 maximum = max(topLeft, bottomRight) + nudge;

            // Clip history samples
            history = ClipToAABB(history, minimum.xyz, maximum.xyz);

            // Blend method
            float weight = clamp(
                lerp(_FinalBlendParameters.x, _FinalBlendParameters.y, motionLength * _FinalBlendParameters.z),
                _FinalBlendParameters.y, _FinalBlendParameters.x
            );

            color = lerp(color, history, weight);
            color = clamp(color, 0.0, HALF_MAX_MINUS1);

            OutputSolver output;
            output.destination = color;
            output.history = color;
            return output;
        }

        float WeightGaussianFitBlackmanHarris(const in float x2)
        {
            return exp(-2.29f * x2);
        }

        // Brent Burley: Filtering in PRMan
        // https://web.archive.org/web/20080908072950/http://www.renderman.org/RMR/st/PRMan_Filtering/Filtering_In_PRMan.html
        // https://www.mathworks.com/help/signal/ref/blackmanharris.html?requestedDomain=www.mathworks.com
        float WeightBlackmanHarris(const in float x)
        {
            const float WIDTH = 3.3f;
            const float HALF_WIDTH = 0.5f * WIDTH;
            const float SCALE = TWO_PI / WIDTH;
            const float BIAS = PI;

            float x1 = min(HALF_WIDTH, x) * SCALE + BIAS;

            const float C0 =  0.35875f;
            const float C1 = -0.48829f;
            const float C2 =  0.14128f;
            const float C3 = -0.01168f;

            return C0 + C1 * cos(x1) + C2 * cos(2.0 * x1) + C3 * cos(3.0 * x1);
        }

        float WeightLanczos(const in float x, const in float inverseWidth)
        {
            float c1 = PI * x;
            float c2 = inverseWidth * c1;
            return (c2 > PI)
                ? 0.0f
                : (x < 1e-5f)
                    ? 1.0
                    : (sin(c2) * sin(c1) / (c2 * c1));
        }

        float WeightDodgsonQuadratic(const in float x, const in float x2, const float r)
        {
            if (x > 1.5) { return 0.0; }

            return  (x > 0.5)
                ? (r * x2 - (2.0 * r + 0.5) * x + (r * 0.75 + 0.75))
                : ((r * 0.5 + 0.5) - 2.0 * r * x2);
        }

        #define BICUBIC_SHARPNESS_C_MITCHELL 1.0 / 3.0
        #define BICUBIC_SHARPNESS_C_ROUBIDOUX 0.3655305
        #define BICUBIC_SHARPNESS_C_CATMULL_ROM 0.5
        float WeightBicubic(const in float x, const in float x2, const float b, const float c)
        {
            float x3 = x2 * x;

            return (x < 2.0)
                ? ((x < 1.0)
                    ? ((2.0 - 1.5 * b - c) * x3 + (-3.0 + 2.0 * b + c) * x2 + (1.0 - b / 3.0))
                    : ((-b / 6.0 - c) * x3 + (b + 5.0 * c) * x2 + (-2.0 * b - 8.0 * c) * x + (4.0 / 3.0 * b + 4.0 * c)))
                : 0.0;
        }

        // Source: https://gist.github.com/TheRealMJP/c83b8c0f46b63f3a88a5986f4fa982b1
        // http://vec3.ca/bicubic-filtering-in-fewer-taps/
        // Sample a 2D texture with bicubic catmull-rom filtering using 9 bilinear texture samples instead of 16 nearest neighbor texture samples.
        void ComputeBicubicUVsAndWeights(out float2 resUVs[3], out float2 resWeights[3], const float sharpness, const in float2 uv, const in float2 resolution, const in float2 inverseResolution)
        {
            // We're going to sample a a 4x4 grid of texels surrounding the target UV coordinate. We'll do this by rounding
            // down the sample location to get the exact center of our starting texel. The starting texel will be at
            // location [1, 1] in the grid, where [0, 0] is the top left corner.
            float2 uvPixels = uv * resolution;
            float2 texPos0 = floor(uvPixels - 0.5) + 0.5;


            // Compute the fractional offset from our starting texel to our original sample location, which we'll
            // feed into the Catmull-Rom spline function to get our filter weights.
            float2 f = uvPixels - texPos0;

            float c = sharpness;
            float b = c * -2.0f + 1.0f;

            // Compute the weights using the fractional offset that we calculated earlier.
            // These equations are pre-expanded based on our knowledge of where the texels will be located,
            // which lets us avoid having to evaluate a piece-wise function.
            float2 w_1 = (-1.0f / 6.0f * b - c) * pow(f + 1.0f, 3.0f) + (b + 5.0f * c) * pow(f + 1.0f, 2.0f) + (-2.0f * b - 8.0f * c) * (f + 1.0f) + (4.0f / 3.0f * b + 4.0f * c);
            float2 w0 = (2.0f - 3.0f / 2.0f * b - c) * pow(f, 3.0f) + (-3.0f + 2.0f * b + c) * pow(f, 2.0f) + (1.0f - b / 3.0f);
            float2 w1 = (2.0f - 3.0f / 2.0f * b - c) * pow(1.0f - f, 3.0f) + (-3.0f + 2.0f * b + c) * pow(1.0f - f, 2.0f) + (1.0f - b / 3.0f);
            float2 w2 = (-1.0f / 6.0f * b - c) * pow(2.0f - f, 3.0f) + (b + 5.0f * c) * pow(2.0f - f, 2.0f) + (-2.0f * b - 8.0f * c) * (2.0f - f) + (4.0f / 3.0f * b + 4.0f * c);

            // Work out weighting factors and sampling offsets that will let us use bilinear filtering to
            // simultaneously evaluate the middle 2 samples from the 4x4 grid.
            float2 w01 = w0 + w1;
            float2 offset01 = w1 / w01;

            // Compute the final UV coordinates we'll use for sampling the texture
            float2 texPos_1 = texPos0 - 1;
            float2 texPos2 = texPos0 + 2;
            float2 texPos01 = texPos0 + offset01;

            resUVs[0] = texPos_1 * inverseResolution;
            resUVs[1] = texPos01 * inverseResolution;
            resUVs[2] = texPos2 * inverseResolution;

            resWeights[0] = w_1;
            resWeights[1] = w01;
            resWeights[2] = w2;
        }

        float4 SampleTextureCatmullRom(in Texture2D<float4> tex, in SamplerState linearSampler, in float2 uv, in float2 texSize)
        {
            // We're going to sample a a 4x4 grid of texels surrounding the target UV coordinate. We'll do this by rounding
            // down the sample location to get the exact center of our "starting" texel. The starting texel will be at
            // location [1, 1] in the grid, where [0, 0] is the top left corner.
            float2 samplePos = uv * texSize;
            float2 texPos1 = floor(samplePos - 0.5f) + 0.5f;

            // Compute the fractional offset from our starting texel to our original sample location, which we'll
            // feed into the Catmull-Rom spline function to get our filter weights.
            float2 f = samplePos - texPos1;

            // Compute the Catmull-Rom weights using the fractional offset that we calculated earlier.
            // These equations are pre-expanded based on our knowledge of where the texels will be located,
            // which lets us avoid having to evaluate a piece-wise function.
            float2 w0 = f * (-0.5f + f * (1.0f - 0.5f * f));
            float2 w1 = 1.0f + f * f * (-2.5f + 1.5f * f);
            float2 w2 = f * (0.5f + f * (2.0f - 1.5f * f));
            float2 w3 = f * f * (-0.5f + 0.5f * f);

            // Work out weighting factors and sampling offsets that will let us use bilinear filtering to
            // simultaneously evaluate the middle 2 samples from the 4x4 grid.
            float2 w12 = w1 + w2;
            float2 offset12 = w2 / (w1 + w2);

            // Compute the final UV coordinates we'll use for sampling the texture
            float2 texPos0 = texPos1 - 1;
            float2 texPos3 = texPos1 + 2;
            float2 texPos12 = texPos1 + offset12;

            texPos0 /= texSize;
            texPos3 /= texSize;
            texPos12 /= texSize;

            float4 result = 0.0f;
            result += tex.SampleLevel(linearSampler, float2(texPos0.x, texPos0.y), 0.0f) * w0.x * w0.y;
            result += tex.SampleLevel(linearSampler, float2(texPos12.x, texPos0.y), 0.0f) * w12.x * w0.y;
            result += tex.SampleLevel(linearSampler, float2(texPos3.x, texPos0.y), 0.0f) * w3.x * w0.y;

            result += tex.SampleLevel(linearSampler, float2(texPos0.x, texPos12.y), 0.0f) * w0.x * w12.y;
            result += tex.SampleLevel(linearSampler, float2(texPos12.x, texPos12.y), 0.0f) * w12.x * w12.y;
            result += tex.SampleLevel(linearSampler, float2(texPos3.x, texPos12.y), 0.0f) * w3.x * w12.y;

            result += tex.SampleLevel(linearSampler, float2(texPos0.x, texPos3.y), 0.0f) * w0.x * w3.y;
            result += tex.SampleLevel(linearSampler, float2(texPos12.x, texPos3.y), 0.0f) * w12.x * w3.y;
            result += tex.SampleLevel(linearSampler, float2(texPos3.x, texPos3.y), 0.0f) * w3.x * w3.y;

            return result;
        }

        OutputSolver SolveExperimental(float2 motion, float2 texcoord)
        {
            float3 colorMin = FLT_MAX;
            float3 colorMax = -FLT_MAX;

            float2 centerPixels = UnityStereoClamp(texcoord);

            float4 colorTotal = 0.0f;
            float weightTotal = 0.0f;

            int kernelRadius = 1;
            switch (_ReconstructionFilter)
            {
                case RECONSTRUCTION_FILTER_GAUSSIAN:
                    kernelRadius = 1;
                    break;
                case RECONSTRUCTION_FILTER_BLACKMAN_HARRIS:
                    kernelRadius = 1;
                    break;
                case RECONSTRUCTION_FILTER_DODGSON_QUADRATIC:
                    kernelRadius = 1;
                    break;
                case RECONSTRUCTION_FILTER_MITCHELL:
                    kernelRadius = 2;
                    break;
                case RECONSTRUCTION_FILTER_ROUBIDOUX:
                    kernelRadius = 2;
                    break;
                case RECONSTRUCTION_FILTER_CATMULL_ROM:
                    kernelRadius = 2;
                    break;
                case RECONSTRUCTION_FILTER_LANCZOS_2:
                    kernelRadius = 2;
                    break;
                case RECONSTRUCTION_FILTER_LANCZOS_3:
                    kernelRadius = 3;
                    break;
                case RECONSTRUCTION_FILTER_LANCZOS_4:
                    kernelRadius = 4;
                    break;
                case RECONSTRUCTION_FILTER_LANCZOS_5:
                    kernelRadius = 5;
                    break;
                default:
                    kernelRadius = 1;
                    break;
            }

            for (int y = -kernelRadius; y <= kernelRadius; ++y)
            {
                for (int x = -kernelRadius; x <= kernelRadius; ++x)
                {
                    float2 offsetPixels = float2(x, y) + _Jitter / _MainTex_TexelSize.xy;
                    float distanceSquaredPixels = dot(offsetPixels, offsetPixels);

                    float sampleWeight = 1.0f;
                    switch (_ReconstructionFilter)
                    {
                        case RECONSTRUCTION_FILTER_GAUSSIAN:
                            sampleWeight = WeightGaussianFitBlackmanHarris(distanceSquaredPixels);
                            break;
                        case RECONSTRUCTION_FILTER_BLACKMAN_HARRIS:
                            sampleWeight = WeightBlackmanHarris(distanceSquaredPixels);
                            break;
                        case RECONSTRUCTION_FILTER_DODGSON_QUADRATIC:
                            sampleWeight = WeightDodgsonQuadratic(sqrt(distanceSquaredPixels), distanceSquaredPixels, _Sharpness * (1.0f / 3.0f));
                            break;
                        case RECONSTRUCTION_FILTER_MITCHELL:
                            sampleWeight = WeightBicubic(sqrt(distanceSquaredPixels), distanceSquaredPixels, BICUBIC_SHARPNESS_C_MITCHELL * -2.0f + 1.0f, BICUBIC_SHARPNESS_C_MITCHELL);
                            break;
                        case RECONSTRUCTION_FILTER_ROUBIDOUX:
                            sampleWeight = WeightBicubic(sqrt(distanceSquaredPixels), distanceSquaredPixels, BICUBIC_SHARPNESS_C_ROUBIDOUX * -2.0f + 1.0f, BICUBIC_SHARPNESS_C_ROUBIDOUX);
                            break;
                        case RECONSTRUCTION_FILTER_CATMULL_ROM:
                            sampleWeight = WeightBicubic(sqrt(distanceSquaredPixels), distanceSquaredPixels, BICUBIC_SHARPNESS_C_CATMULL_ROM * -2.0f + 1.0f, BICUBIC_SHARPNESS_C_CATMULL_ROM);
                            break;
                        case RECONSTRUCTION_FILTER_LANCZOS_2:
                            sampleWeight = WeightLanczos(sqrt(distanceSquaredPixels), 1.0f / 2.0f);
                            break;
                        case RECONSTRUCTION_FILTER_LANCZOS_3:
                            sampleWeight = WeightLanczos(sqrt(distanceSquaredPixels), 1.0f / 3.0f);
                            break;
                        case RECONSTRUCTION_FILTER_LANCZOS_4:
                            sampleWeight = WeightLanczos(sqrt(distanceSquaredPixels), 1.0f / 4.0f);
                            break;
                        case RECONSTRUCTION_FILTER_LANCZOS_5:
                            sampleWeight = WeightLanczos(sqrt(distanceSquaredPixels), 1.0f / 5.0f);
                            break;
                        default:
                            sampleWeight = 1.0f;
                            break;
                    }


                    // float sampleWeight = WeightGaussianFitBlackmanHarris(distanceSquaredPixels);

                    // float sampleWeight = WeightBlackmanHarris(distanceSquaredPixels);

                    // float sampleWeight = WeightLanczos(sqrt(distanceSquaredPixels), 1.0f / 3.0f);

                    // float sampleWeight = WeightDodgsonQuadratic(sqrt(distanceSquaredPixels), distanceSquaredPixels, _Sharpness * (1.0f / 3.0f));

                    float4 sampleColor = SAMPLE_TEXTURE2D(_MainTex, _MainTexSampler, UnityStereoClamp(centerPixels + float2(x, y) * _MainTex_TexelSize.xy));

                    sampleColor.xyz = YcocgFromRgb(sampleColor.rgb);

                    sampleColor.xyz *= rcp(1.0f + sampleColor.x);

                    // if (max(abs(x), abs(y)) <= 1)
                    if (distanceSquaredPixels < (1.5f * 1.5f))
                    {
                        colorMin = min(colorMin, sampleColor.xyz);
                        colorMax = max(colorMax, sampleColor.xyz);
                    }

                    colorTotal += sampleColor * sampleWeight;
                    weightTotal += sampleWeight;
                }
            }

            float4 color = ((weightTotal == 0.0f) || (colorTotal != colorTotal)) ? 0.0f : (colorTotal * rcp(weightTotal));

            // color.xyz = clamp(color.xyz, colorMin, colorMax);

            float2 bicubicUVs[3];
            float2 bicubicWeights[3];
            ComputeBicubicUVsAndWeights(bicubicUVs, bicubicWeights, 0.5f * _Sharpness * (1.0f / 3.0f), texcoord - motion, 1.0f / _MainTex_TexelSize.xy, _MainTex_TexelSize.xy);

            float4 history = 0.0f;
            history += SAMPLE_TEXTURE2D(_HistoryTex, sampler_HistoryTex, UnityStereoClamp(float2(bicubicUVs[0].x, bicubicUVs[0].y))) * bicubicWeights[0].x * bicubicWeights[0].y;
            history += SAMPLE_TEXTURE2D(_HistoryTex, sampler_HistoryTex, UnityStereoClamp(float2(bicubicUVs[1].x, bicubicUVs[0].y))) * bicubicWeights[1].x * bicubicWeights[0].y;
            history += SAMPLE_TEXTURE2D(_HistoryTex, sampler_HistoryTex, UnityStereoClamp(float2(bicubicUVs[2].x, bicubicUVs[0].y))) * bicubicWeights[2].x * bicubicWeights[0].y;

            history += SAMPLE_TEXTURE2D(_HistoryTex, sampler_HistoryTex, UnityStereoClamp(float2(bicubicUVs[0].x, bicubicUVs[1].y))) * bicubicWeights[0].x * bicubicWeights[1].y;
            history += SAMPLE_TEXTURE2D(_HistoryTex, sampler_HistoryTex, UnityStereoClamp(float2(bicubicUVs[1].x, bicubicUVs[1].y))) * bicubicWeights[1].x * bicubicWeights[1].y;
            history += SAMPLE_TEXTURE2D(_HistoryTex, sampler_HistoryTex, UnityStereoClamp(float2(bicubicUVs[2].x, bicubicUVs[1].y))) * bicubicWeights[2].x * bicubicWeights[1].y;

            history += SAMPLE_TEXTURE2D(_HistoryTex, sampler_HistoryTex, UnityStereoClamp(float2(bicubicUVs[0].x, bicubicUVs[2].y))) * bicubicWeights[0].x * bicubicWeights[2].y;
            history += SAMPLE_TEXTURE2D(_HistoryTex, sampler_HistoryTex, UnityStereoClamp(float2(bicubicUVs[1].x, bicubicUVs[2].y))) * bicubicWeights[1].x * bicubicWeights[2].y;
            history += SAMPLE_TEXTURE2D(_HistoryTex, sampler_HistoryTex, UnityStereoClamp(float2(bicubicUVs[2].x, bicubicUVs[2].y))) * bicubicWeights[2].x * bicubicWeights[2].y;

            // history = SampleTextureCatmullRom(_HistoryTex, sampler_HistoryTex, texcoord - motion, 1.0f / _MainTex_TexelSize);

            // history = SAMPLE_TEXTURE2D(_HistoryTex, sampler_HistoryTex, UnityStereoClamp(texcoord - motion));
            history.xyz = YcocgFromRgb(history.rgb);
            history.xyz *= rcp(1.0f + history.x);
            history.xyz = clamp(history.xyz, colorMin, colorMax);
            // history = ClipToAABB(history, colorMin.xyz, colorMax.xyz);

            float motionLength = length(motion);
            float weightHistory = clamp(
                lerp(_FinalBlendParameters.x, _FinalBlendParameters.y, motionLength * _FinalBlendParameters.z),
                _FinalBlendParameters.y, _FinalBlendParameters.x
            );

            color = lerp(color, history, weightHistory);

            color.xyz *= rcp(1.0f - color.x);

            color.rgb = RgbFromYcocg(color.xyz);

            OutputSolver output;
            output.destination = color;
            output.history = color;
            return output;
        }

        OutputSolver FragSolverDilate(VaryingsDefault i)
        {
            float2 closest = GetClosestFragment(i.texcoordStereo);
            float2 motion = SAMPLE_TEXTURE2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture, closest).xy;

            if (_ReconstructionFilter == RECONSTRUCTION_FILTER_UNITY)
            {
                return Solve(motion, i.texcoordStereo);
            } else
            {
                return SolveExperimental(motion, i.texcoordStereo);
            }
        }

        OutputSolver FragSolverNoDilate(VaryingsDefault i)
        {
            // Don't dilate in ortho !
            float2 motion = SAMPLE_TEXTURE2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture, i.texcoordStereo).xy;
            if (_ReconstructionFilter == RECONSTRUCTION_FILTER_UNITY)
            {
                return Solve(motion, i.texcoordStereo);
            } else
            {
                return SolveExperimental(motion, i.texcoordStereo);
            }
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
    }
}
