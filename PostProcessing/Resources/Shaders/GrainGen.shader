Shader "Hidden/Post FX/Grain Generator"
{
    CGINCLUDE

        #include "UnityCG.cginc"
        #include "Common.cginc"

        float4 _Params; // x: size, y: time, z: cos_angle, w: sin_angle

        float3 Rnm(float2 tc, float time)
        {
            float noise = sin(dot(tc + time.xx, float2(12.9898, 78.233))) * 43758.5453;
            return frac(noise.xxx * float3(1.0, 1.2154, 1.3647)) * 2.0 - 1.0;
        }

        float Fade(float t)
        {
            return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
        }

        // 2d gradient noise
        float PNoise(float2 p, float time)
        {
            const float kPermTexUnit = 1.0 / 256.0;
            const float kPermTexUnitHalf = 0.5 / 256.0;

            float2 pi = kPermTexUnit * floor(p) + kPermTexUnitHalf;
            float2 pf = frac(p);

            float perm00 = Rnm(pi, time).z;
            float2 grad000 = Rnm(float2(perm00, kPermTexUnitHalf), time).xy * 4.0 - 1.0;
            float n000 = dot(grad000, pf);

            float perm01 = Rnm(pi + float2(0.0, kPermTexUnit), time).z;
            half2 grad010 = Rnm(float2(perm01, kPermTexUnitHalf), time).xy * 4.0 - 1.0;
            float n010 = dot(grad010, pf - float2(0.0, 1.0));

            float perm10 = Rnm(pi + float2(kPermTexUnit, 0.0), time).z;
            float2 grad100 = Rnm(float2(perm10, kPermTexUnitHalf), time).xy * 4.0 - 1.0;
            float n100 = dot(grad100, pf - float2(1.0, 0.0));

            float perm11 = Rnm(pi + float2(kPermTexUnit, kPermTexUnit), time).z;
            float2 grad110 = Rnm(float2(perm11, kPermTexUnitHalf), time).xy * 4.0 - 1.0;
            float n110 = dot(grad110, pf - float2(1.0, 1.0));

            float2 n_x = lerp(float2(n000, n010), float2(n100, n110), Fade(pf.x));

            return lerp(n_x.x, n_x.y, Fade(pf.y));
        }

        float2 CoordRot(float2 tc, float2 angle)
        {
            float s = angle.y;
            float c = angle.x;
            tc = tc * 2.0 - 1.0;
            float2 rot = float2((tc.x * c) - (tc.y * s), (tc.y * c) + (tc.x * s));
            return rot * 0.5 + 0.5;
        }

        float4 FragGrain(VaryingsDefault i) : SV_Target
        {
            float2 rotCoords = CoordRot(i.uv, _Params.zw);
            float n = PNoise(rotCoords * (192.0).xx / _Params.x, _Params.y);
            return n.xxxx;
        }

        float4 FragGrainColored(VaryingsDefault i) : SV_Target
        {
            float2 rotCoordsR = CoordRot(i.uv, _Params.zw);
            float2 rotCoordsG = CoordRot(i.uv + (0.1).xx, _Params.zw);
            float2 rotCoordsB = CoordRot(i.uv - (0.1).xx, _Params.zw);

            float r = PNoise(rotCoordsR * (192.0).xx / _Params.x, _Params.y);
            float g = PNoise(rotCoordsG * (192.0).xx / _Params.x, _Params.y);
            float b = PNoise(rotCoordsB * (192.0).xx / _Params.x, _Params.y);

            return float4(r, g, b, 1.0);
        }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragGrain

            ENDCG
        }

        Pass
        {
            CGPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragGrainColored

            ENDCG
        }
    }
}
