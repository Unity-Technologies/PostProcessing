Shader "Hidden/Post FX/Fog"
{
    CGINCLUDE

        #pragma multi_compile FOG_LINEAR FOG_EXP FOG_EXP2
        #include "UnityCG.cginc"
        #include "Common.cginc"

        #define SKYBOX_THRESHOLD_VALUE 0.9999

        struct AttributesFog
        {
            float4 vertex : POSITION;
            float4 texcoord : TEXCOORD0;
            float4 texcoord1 : TEXCOORD1;
        };

        struct VaryingsFog
        {
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        struct VaryingsFogFade
        {
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 ray : TEXCOORD1;
        };

        sampler2D _CameraDepthTexture;

        half4 _FogColor;
        float4 _Density_Start_End;

        samplerCUBE _SkyCubemap;
        half4 _SkyCubemap_HDR;
        half4 _SkyTint;
        half4 _SkyExposure_Rotation;

        float3 RotateAroundYAxis(float3 v, float rad)
        {
            float alpha = rad;
            float sina, cosa;
            sincos(alpha, sina, cosa);
            float2x2 m = float2x2(cosa, -sina, sina, cosa);
            return float3(mul(m, v.xz), v.y).xzy;
        }

        VaryingsFog VertFog(AttributesDefault v)
        {
            VaryingsFog o;
            o.vertex = v.vertex;
            o.uv = UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST);
            return o;
        }

        VaryingsFogFade VertFogFade(AttributesFog v)
        {
            VaryingsFogFade o;
            o.vertex = v.vertex;
            o.uv = UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST);
            float _SkyRotation = _SkyExposure_Rotation.y;
            o.ray = RotateAroundYAxis(v.texcoord1.xyz, _SkyRotation);
            return o;
        }

        half ComputeFog(float z)
        {
            half fog = 0.0;

            float _Density = _Density_Start_End.x;
            float _Start = _Density_Start_End.y;
            float _End = _Density_Start_End.z;

        #if FOG_LINEAR
            fog = (_End - z) / (_End - _Start);
        #elif FOG_EXP
            fog = exp2(-_Density * z);
        #else // FOG_EXP2
            fog = _Density * z;
            fog = exp2(-fog * fog);
        #endif
            return saturate(fog);
        }

        float ComputeDistance(float depth)
        {
            float dist = depth * _ProjectionParams.z;
            dist -= _ProjectionParams.y;
            return dist;
        }

        half4 FragFog(VaryingsFog i) : SV_Target
        {
            float _Start = _Density_Start_End.y;

            float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
            depth = Linear01Depth(depth);
            float dist = ComputeDistance(depth) - _Start;
            half fog = 1.0 - ComputeFog(dist);

            half4 sceneColor = tex2D(_MainTex, i.uv);
            return lerp(sceneColor, _FogColor, fog);
        }

        half4 FragFogExcludeSkybox(VaryingsFog i) : SV_Target
        {
            float _Start = _Density_Start_End.y;

            float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
            depth = Linear01Depth(depth);
            float skybox = depth < SKYBOX_THRESHOLD_VALUE;
            float dist = ComputeDistance(depth) - _Start;
            half fog = 1.0 - ComputeFog(dist);

            half4 sceneColor = tex2D(_MainTex, i.uv);
            return lerp(sceneColor, _FogColor, fog);
        }

        half4 FragFogFadeToSkybox(VaryingsFogFade i) : SV_Target
        {
            float _Start = _Density_Start_End.y;

            float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
            depth = Linear01Depth(depth);
            float dist = ComputeDistance(depth) - _Start;
            half fog = 1.0 - ComputeFog(dist);

            float _SkyExposure = _SkyExposure_Rotation.x;
            // Look up the skybox color.
            half3 skyColor = DecodeHDR(texCUBE(_SkyCubemap, i.ray), _SkyCubemap_HDR);
            skyColor *= _SkyTint.rgb * _SkyExposure * unity_ColorSpaceDouble;
            // Lerp between source color to skybox color with fog amount.
            half4 sceneColor = tex2D(_MainTex, i.uv);
            return lerp(sceneColor, half4(skyColor, 1), fog);
        }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM

                #pragma vertex VertFog
                #pragma fragment FragFog

            ENDCG
        }

        Pass
        {
            CGPROGRAM

                #pragma vertex VertFog
                #pragma fragment FragFogExcludeSkybox

            ENDCG
        }

        Pass
        {
            CGPROGRAM

                #pragma vertex VertFogFade
                #pragma fragment FragFogFadeToSkybox

            ENDCG
        }
    }
}
