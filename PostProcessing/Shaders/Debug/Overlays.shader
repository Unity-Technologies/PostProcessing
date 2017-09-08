Shader "Hidden/PostProcessing/Debug/Overlays"
{
    HLSLINCLUDE

        #include "../StdLib.hlsl"
        #include "../Colors.hlsl"
        #pragma target 3.0
        #pragma multi_compile _ UNITY_COLORSPACE_GAMMA

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
        TEXTURE2D_SAMPLER2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture);
        TEXTURE2D_SAMPLER2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture);

    #if SOURCE_GBUFFER
        TEXTURE2D_SAMPLER2D(_CameraGBufferTexture2, sampler_CameraGBufferTexture2);
    #endif

        float4 _MainTex_TexelSize;

        // -----------------------------------------------------------------------------
        // Depth

        float4 FragDepth(VaryingsDefault i) : SV_Target
        {
            float d = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoordStereo, 0);

        //#if !UNITY_COLORSPACE_GAMMA
        //    d = SRGBToLinear(d);
        //#endif

            return float4(d.xxx, 1.0);
        }

        // -----------------------------------------------------------------------------
        // Normals

        float4 FragNormals(VaryingsDefault i) : SV_Target
        {
        #if SOURCE_GBUFFER
            float3 norm = SAMPLE_TEXTURE2D(_CameraGBufferTexture2, sampler_CameraGBufferTexture2, i.texcoordStereo).xyz * 2.0 - 1.0;
            float3 n = mul((float3x3)unity_WorldToCamera, norm);
        #else
            float4 cdn = SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, i.texcoordStereo);
            float3 n = DecodeViewNormalStereo(cdn) * float3(1.0, 1.0, -1.0);
        #endif

        #if UNITY_COLORSPACE_GAMMA
            n = LinearToSRGB(n);
        #endif

            return float4(n, 1.0);
        }

        // -----------------------------------------------------------------------------
        // Motion vectors

        float2 _Params;

        float DistanceToLine(float2 p, float2 p1, float2 p2)
        {
            float2 center = (p1 + p2) * 0.5;
            float len = length(p2 - p1);
            float2 dir = (p2 - p1) / len;
            float2 rel_p = p - center;
            return dot(rel_p, float2(dir.y, -dir.x));
        }

        float DistanceToSegment(float2 p, float2 p1, float2 p2)
        {
            float2 center = (p1 + p2) * 0.5;
            float len = length(p2 - p1);
            float2 dir = (p2 - p1) / len;
            float2 rel_p = p - center;
            float dist1 = abs(dot(rel_p, float2(dir.y, -dir.x)));
            float dist2 = abs(dot(rel_p, dir)) - 0.5 * len;
            return max(dist1, dist2);
        }

        float DrawArrow(float2 texcoord, float body, float head, float height, float linewidth, float antialias)
        {
            float w = linewidth / 2.0 + antialias;
            float2 start = -float2(body / 2.0, 0.0);
            float2 end = float2(body / 2.0, 0.0);

            // Head: 3 lines
            float d1 = DistanceToLine(texcoord, end, end - head * float2(1.0, -height));
            float d2 = DistanceToLine(texcoord, end - head * float2(1.0, height), end);
            float d3 = texcoord.x - end.x + head;

            // Body: 1 segment
            float d4 = DistanceToSegment(texcoord, start, end - float2(linewidth, 0.0));

            float d = min(max(max(d1, d2), -d3), d4);
            return d;
        }

        float2 SampleMotionVectors(float2 coords)
        {
            float2 mv = SAMPLE_TEXTURE2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture, UnityStereoTransformScreenSpaceTex(coords)).xy;
            mv.y *= -1.0;
            return mv;
        }

        float4 FragMotionVectors(VaryingsDefault i) : SV_Target
        {
            float3 src = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo).rgb;
            float2 mv = SampleMotionVectors(i.texcoord);

            // Background color intensity - keep this low unless you want to make your eyes bleed
            const float kIntensity = _Params.x;

            // Map motion vector direction to color wheel (hue between 0 and 360deg)
            float phi = atan2(mv.x, mv.y);
            float hue = (phi / PI + 1.0) * 0.5;
            float r = abs(hue * 6.0 - 3.0) - 1.0;
            float g = 2.0 - abs(hue * 6.0 - 2.0);
            float b = 2.0 - abs(hue * 6.0 - 4.0);
            float a = length(mv * kIntensity);

            float4 color = saturate(float4(r, g, b, a));

            // Grid subdivisions
            const float kGrid = _Params.y;

            // Arrow grid (aspect ratio is kept)
            float rows = floor(kGrid * _MainTex_TexelSize.w / _MainTex_TexelSize.z);
            float cols = kGrid;
            float2 size = _MainTex_TexelSize.zw / float2(cols, rows);
            float body = (min(size.x, size.y) / 1.4142135623730951) * saturate(length(mv * kGrid * 0.25));
            float2 texcoord = i.vertex.xy;
            float2 center = (floor(texcoord / size) + 0.5) * size;
            texcoord -= center;

            // Sample the center of the cell to get the current arrow vector
            float2 arrow_coord = center / _MainTex_TexelSize.zw;
            float2 mv_arrow = SampleMotionVectors(arrow_coord);

            // Skip empty motion
            float d = 0.0;
            if (any(mv_arrow))
            {
                // Rotate the arrow according to the direction
                mv_arrow = normalize(mv_arrow);
                float2x2 rot = float2x2(mv_arrow.x, -mv_arrow.y, mv_arrow.y, mv_arrow.x);
                texcoord = mul(rot, texcoord);

                d = DrawArrow(texcoord, body, 0.25 * body, 0.5, 2.0, 1.0);
                d = 1.0 - saturate(d);
            }

        #if !UNITY_COLORSPACE_GAMMA
            src = LinearToSRGB(src);
        #endif

            color.rgb = lerp(src, color.rgb, color.a);

        #if !UNITY_COLORSPACE_GAMMA
            color.rgb = SRGBToLinear(color.rgb);
        #endif

            return float4(color.rgb + d.xxx, 1.0);
        }

        // -----------------------------------------------------------------------------
        // NAN tracker

        float4 FragNANTracker(VaryingsDefault i) : SV_Target
        {
            float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);

            if (any(isnan(color)) || any(isinf(color)))
            {
                color = float4(1.0, 0.0, 1.0, 1.0);
            }
            else
            {
                // Dim the color buffer so we can see NaNs & Infs better
                color.rgb = saturate(color.rgb) * 0.25;
            }

            return color;
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0 - Depth
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragDepth

            ENDHLSL
        }

        // 1 - Normals
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragNormals
                #pragma multi_compile _ SOURCE_GBUFFER

            ENDHLSL
        }

        // 2 - Motion vectors
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragMotionVectors

            ENDHLSL
        }

        // 3 - Nan tracker
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragNANTracker

            ENDHLSL
        }
    }
}
