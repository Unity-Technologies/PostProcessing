Shader "Hidden/PostProcessing/CopyStd"
{
    //
    // We need this shader for the very first RT blit using the internal CommandBuffer.Blit() method
    // so it can handle AAResolve properly. We also need it to be separate because of VR and the
    // need for a Properties block. If we were to add this block to the other Copy shader it would
    // not allow us to manually bind _MainTex, thus breaking a few other things in the process...
    //

    Properties
    {
        _MainTex ("", 2D) = "white" {}
    }


    CGINCLUDE

// XRTODO:
// #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
// Instancing needs to support D3D11 (and OGL in the future)
// Multi-view needs to support OGL

#if defined(STEREO_INSTANCING_ON)
#else
#endif

        struct Attributes
        {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
#if defined(STEREO_INSTANCING_ON)
            uint instanceID : SV_InstanceID;
#endif
        };

        struct Varyings
        {
            float4 vertex : SV_POSITION;
            float2 texcoord : TEXCOORD0;
#if defined(STEREO_INSTANCING_ON)
            uint stereoTargetEyeIndex : SV_RenderTargetArrayIndex;
#endif
        };

#if defined(STEREO_INSTANCING_ON)
        Texture2DArray _MainTex;
        SamplerState sampler_MainTex;
        // CG versions of 2Darray don't seem to work...
#else
        sampler2D _MainTex;
#endif
        float4 _MainTex_ST;

        Varyings Vert(Attributes v)
        {
            Varyings o;
            o.vertex = float4(v.vertex.xy * 2.0 - 1.0, 0.0, 1.0);
            o.texcoord = v.texcoord;

            #if UNITY_UV_STARTS_AT_TOP
            o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
            #endif

            o.texcoord = o.texcoord * _MainTex_ST.xy + _MainTex_ST.zw; // We need this for VR

#if defined(STEREO_INSTANCING_ON)
            o.stereoTargetEyeIndex = v.instanceID & 0x01;
#endif

            return o;
        }

        float4 Frag(Varyings i) : SV_Target
        {
#if defined(STEREO_INSTANCING_ON)
            float eyeIndex = i.stereoTargetEyeIndex;
            float4 color = _MainTex.Sample(sampler_MainTex, float3(i.texcoord, eyeIndex));
#else
            float4 color = tex2D(_MainTex, i.texcoord);
#endif
            return color;
        }

        //>>> We don't want to include StdLib.hlsl in this file so let's copy/paste what we need
        bool IsNan(float x)
        {
            return (x < 0.0 || x > 0.0 || x == 0.0) ? false : true;
        }

        bool AnyIsNan(float4 x)
        {
            return IsNan(x.x) || IsNan(x.y) || IsNan(x.z) || IsNan(x.w);
        }
        //<<<

        float4 FragKillNaN(Varyings i) : SV_Target
        {
#if defined(STEREO_INSTANCING_ON)
            float eyeIndex = i.stereoTargetEyeIndex;
            float4 color = _MainTex.Sample(sampler_MainTex, float3(i.texcoord, eyeIndex));
#else
            float4 color = tex2D(_MainTex, i.texcoord);
#endif

            if (AnyIsNan(color))
            {
                color = (0.0).xxxx;
            }

            return color;
        }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0 - Copy
        Pass
        {
            CGPROGRAM

                #pragma vertex Vert
                #pragma fragment Frag

            ENDCG
        }

        // 1 - Copy + NaN killer
        Pass
        {
            CGPROGRAM

                #pragma vertex Vert
                #pragma fragment FragKillNaN

            ENDCG
        }
    }
}
