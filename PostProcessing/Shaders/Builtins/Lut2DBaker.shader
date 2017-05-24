Shader "Hidden/PostProcessing/Lut2DBaker"
{
    HLSLINCLUDE

        #pragma target 3.0
        #include "../StdLib.hlsl"
        #include "../Colors.hlsl"
        #include "../ACES.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float4 _Lut2D_Params;

        float3 _ColorBalance;
        float3 _ColorFilter;
        float3 _HueSatCon;
        float _Brightness;

        float3 _ChannelMixerRed;
        float3 _ChannelMixerGreen;
        float3 _ChannelMixerBlue;

        float3 _Lift;
        float3 _InvGamma;
        float3 _Gain;

        TEXTURE2D_SAMPLER2D(_Curves, sampler_Curves);

        // -----------------------------------------------------------------------------------------
        // LDR Grading - no starting lut

        float3 ColorGradeLDR(float3 colorLinear)
        {
            const float kMidGrey = pow(0.5, 2.2);

            colorLinear *= _Brightness;
            colorLinear = Contrast(colorLinear, kMidGrey, _HueSatCon.z);
            colorLinear = WhiteBalance(colorLinear, _ColorBalance);
            colorLinear *= _ColorFilter;
            colorLinear = ChannelMixer(colorLinear, _ChannelMixerRed, _ChannelMixerGreen, _ChannelMixerBlue);
            colorLinear = LiftGammaGainLDR(colorLinear, _Lift, _InvGamma, _Gain);

            float3 hsv = RgbToHsv(colorLinear);

            // Hue Vs Sat
            float satMult;
            satMult = saturate(SAMPLE_TEXTURE2D_LOD(_Curves, sampler_Curves, float2(hsv.x, 0.25), 0).y) * 2.0;

            // Sat Vs Sat
            satMult *= saturate(SAMPLE_TEXTURE2D_LOD(_Curves, sampler_Curves, float2(hsv.y, 0.25), 0).z) * 2.0;

            // Lum Vs Sat
            satMult *= saturate(SAMPLE_TEXTURE2D_LOD(_Curves, sampler_Curves, float2(Luminance(colorLinear), 0.25), 0).w) * 2.0;

            // Hue Vs Hue
            float hue = hsv.x + _HueSatCon.x;
            float offset = saturate(SAMPLE_TEXTURE2D_LOD(_Curves, sampler_Curves, float2(hue, 0.25), 0).x) - 0.5;
            hue += offset;
            hsv.x = RotateHue(hue, 0.0, 1.0);

            colorLinear = HsvToRgb(hsv);

            colorLinear = Saturation(colorLinear, _HueSatCon.y * satMult);
            colorLinear = saturate(colorLinear);
            colorLinear = YrgbCurve(colorLinear, TEXTURE2D_PARAM(_Curves, sampler_Curves));

            return saturate(colorLinear);
        }

        float4 FragLDRFromScratch(VaryingsDefault i) : SV_Target
        {
            // 2D strip lut
            float3 colorLinear = GetLutStripValue(i.texcoord, _Lut2D_Params);
            float3 graded = ColorGradeLDR(colorLinear);
            return float4(graded, 1.0);
        }

        // -----------------------------------------------------------------------------------------
        // LDR Grading - with starting lut

        float4 FragLDR(VaryingsDefault i) : SV_Target
        {
            float3 colorLinear = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord).rgb;
            float3 graded = ColorGradeLDR(colorLinear);
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
                #pragma fragment FragLDRFromScratch

            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragLDR

            ENDHLSL
        }
    }
}
