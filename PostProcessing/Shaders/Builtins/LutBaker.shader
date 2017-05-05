Shader "Hidden/PostProcessing/LutBaker"
{
    HLSLINCLUDE

        #pragma target 3.0
        #include "../StdLib.hlsl"
        #include "../Colors.hlsl"
        #include "../ACES.hlsl"

        TEXTURE2D_SAMPLER2D(_BaseLut, sampler_BaseLut);
        float4 _LutParams;

        float3 ColorGrade(float3 colorLinear)
        {
            return colorLinear;
        }

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            // 2D strip lut
            float2 uv = i.texcoord - _LutParams.yz;
            float3 color;
            color.r = frac(uv.x * _LutParams.x);
            color.b = uv.x - color.r / _LutParams.x;
            color.g = uv.y;

            // Lut is in LogC
            float3 colorLogC = color * _LutParams.w;

            // Switch back to unity linear and color grade
            float3 colorLinear = LogCToLinear(colorLogC);
            float3 graded = ColorGrade(colorLinear);

            return float4(graded, 1.0);
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
    }
}
