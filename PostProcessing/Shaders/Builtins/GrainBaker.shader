Shader "Hidden/PostProcessing/GrainBaker"
{
    HLSLINCLUDE

        #pragma exclude_renderers d3d11_9x
        #pragma target 3.0
        #include "../StdLib.hlsl"

        float _Phase;
        float3 _NoiseParameters;

        // Implementation based on Timothy Lottes' "Large Grain"
        // Reference code: https://www.shadertoy.com/view/4sSXDW
        // Other article of interest: http://devlog-martinsh.blogspot.fr/2013/05/image-imperfections-and-film-grain-post.html
        float Noise(float2 n, float x)
        {
            n += x;
            return frac(sin(dot(n.xy, _NoiseParameters.xy)) * _NoiseParameters.z);
        }

        float Step1(float2 uv, float n)
        {
            float b = 2.0, c = -12.0;
            return (1.0 / (4.0 + b * 4.0 + abs(c))) * (
                Noise(uv + float2(-1.0, -1.0), n) +
                Noise(uv + float2( 0.0, -1.0), n) * b +
                Noise(uv + float2( 1.0, -1.0), n) +
                Noise(uv + float2(-1.0,  0.0), n) * b +
                Noise(uv + float2( 0.0,  0.0), n) * c +
                Noise(uv + float2( 1.0,  0.0), n) * b +
                Noise(uv + float2(-1.0,  1.0), n) +
                Noise(uv + float2( 0.0,  1.0), n) * b +
                Noise(uv + float2( 1.0,  1.0), n)
            );
        }

        float Step2(float2 uv, float n)
        {
            float b = 2.0, c = 4.0;
            return (1.0 / (4.0 + b * 4.0 + abs(c))) * (
                Step1(uv + float2(-1.0, -1.0), n) +
                Step1(uv + float2( 0.0, -1.0), n) * b +
                Step1(uv + float2( 1.0, -1.0), n) +
                Step1(uv + float2(-1.0,  0.0), n) * b +
                Step1(uv + float2( 0.0,  0.0), n) * c +
                Step1(uv + float2( 1.0,  0.0), n) * b +
                Step1(uv + float2(-1.0,  1.0), n) +
                Step1(uv + float2( 0.0,  1.0), n) * b +
                Step1(uv + float2( 1.0,  1.0), n)
            );
        }

        float Step3BW(float2 uv)
        {
            return Step2(uv, frac(_Phase));
        }

        float3 Step3(float2 uv)
        {
            float a = Step2(uv, 0.07 * frac(_Phase));
            float b = Step2(uv, 0.11 * frac(_Phase));
            float c = Step2(uv, 0.13 * frac(_Phase));
            return float3(a, b, c);
        }

        float4 FragGrain(VaryingsDefault i) : SV_Target
        {
            float grain = Step3BW(i.texcoordStereo * float2(128.0, 128.0));
            return float4(grain.xxx, 1.0);
        }

        float4 FragGrainColored(VaryingsDefault i) : SV_Target
        {
            float3 grain = Step3(i.texcoordStereo * float2(128.0, 128.0));
            return float4(grain, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragGrain

            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragGrainColored

            ENDHLSL
        }
    }
}
