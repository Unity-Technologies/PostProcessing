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
// Instancing needs to support D3D11 GNM/PSSL (and OGL in the future)
// Multi-view needs to support OGL

// This seems to be injected already?
//CBUFFER_START(UnityStereoEyeIndex)
//    int unity_StereoEyeIndex;
//CBUFFER_END

#if (defined(SHADER_API_D3D11)  || defined(SHADER_API_PSSL))
#define UNITY_SUPPORT_STEREO_INSTANCING
#endif

#if defined(UNITY_SUPPORT_STEREO_INSTANCING) && defined(STEREO_INSTANCING_ON)
// This might already be flipped on somehow...
#define UNITY_STEREO_INSTANCING_ENABLED
#endif

#if ((defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)) && !(defined(SHADER_API_SWITCH)))
#define UNITY_SUPPORT_MULTIVIEW
#endif

#if defined(UNITY_SUPPORT_MULTIVIEW) && defined(STEREO_MULTIVIEW_ON)
#define UNITY_STEREO_MULTIVIEW_ENABLED
#endif

#if defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN)
// These platforms have constant buffers disabled normally, but not here (see CBUFFER_START/CBUFFER_END in HLSLSupport.cginc).
#define UNITY_INSTANCING_CBUFFER_SCOPE_BEGIN(name)  cbuffer name {
#define UNITY_INSTANCING_CBUFFER_SCOPE_END          }
#else
#define UNITY_INSTANCING_CBUFFER_SCOPE_BEGIN(name)  CBUFFER_START(name)
#define UNITY_INSTANCING_CBUFFER_SCOPE_END          CBUFFER_END
#endif

// if I fold this into another header...
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
#define USING_STEREO_MATRICES
#endif

#if defined(USING_STEREO_MATRICES) && defined(UNITY_STEREO_MULTIVIEW_ENABLED)
CBUFFER_START(UnityStereoEyeIndices)
float4 unity_StereoEyeIndices[2];
CBUFFER_END
#endif

#if defined(UNITY_STEREO_INSTANCING_ENABLED)

// A global instance ID variable that functions can directly access.
static uint unity_InstanceID;

    // Don't make UnityDrawCallInfo an actual CB on GL
#if !defined(SHADER_API_GLES3) && !defined(SHADER_API_GLCORE)
    UNITY_INSTANCING_CBUFFER_SCOPE_BEGIN(UnityDrawCallInfo)
#endif
        int unity_BaseInstanceID;
        int unity_InstanceCount;
#if !defined(SHADER_API_GLES3) && !defined(SHADER_API_GLCORE)
    UNITY_INSTANCING_CBUFFER_SCOPE_END
#endif

#ifdef SHADER_API_PSSL
#define DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID uint instanceID;
#define UNITY_GET_INSTANCE_ID(input)    _GETINSTANCEID(input)
#else
#define DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID uint instanceID : SV_InstanceID;
#define UNITY_GET_INSTANCE_ID(input)    input.instanceID
#endif

#else
#define DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID
#endif // UNITY_INSTANCING_ENABLED

#if !defined(UNITY_VERTEX_INPUT_INSTANCE_ID)
#   define UNITY_VERTEX_INPUT_INSTANCE_ID DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID
#endif


// set up stereo target eye index
#ifdef UNITY_STEREO_INSTANCING_ENABLED
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO                          uint stereoTargetEyeIndex : SV_RenderTargetArrayIndex;
    #define DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)       output.stereoTargetEyeIndex = unity_StereoEyeIndex
    #define DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input, output)  output.stereoTargetEyeIndex = input.stereoTargetEyeIndex;
    #define DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input) unity_StereoEyeIndex = input.stereoTargetEyeIndex;
#elif defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO float stereoTargetEyeIndex : BLENDWEIGHT0;
    // HACK: Workaround for Mali shader compiler issues with directly using GL_ViewID_OVR (GL_OVR_multiview). This array just contains the values 0 and 1.
    #define DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output) output.stereoTargetEyeIndex = unity_StereoEyeIndices[unity_StereoEyeIndex].x;
    #define DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input, output) output.stereoTargetEyeIndex = input.stereoTargetEyeIndex;
    #if defined(SHADER_STAGE_VERTEX)
        #define DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
    #else
        #define DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input) unity_StereoEyeIndex = (uint) input.stereoTargetEyeIndex;
    #endif
#else
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO
    #define DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)
    #define DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input, output)
    #define DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
#endif

#if !defined(UNITY_VERTEX_OUTPUT_STEREO)
#   define UNITY_VERTEX_OUTPUT_STEREO                           DEFAULT_UNITY_VERTEX_OUTPUT_STEREO
#endif
#if !defined(UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO)
#   define UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)        DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)
#endif
#if !defined(UNITY_TRANSFER_VERTEX_OUTPUT_STEREO)
#   define UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input, output)   DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input, output)
#endif
#if !defined(UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX)
#   define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)      DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
#endif

#if defined(UNITY_STEREO_INSTANCING_ENABLED)
    void UnitySetupInstanceID(uint inputInstanceID)
    {
    #ifdef UNITY_STEREO_INSTANCING_ENABLED
        // stereo eye index is automatically figured out from the instance ID
        unity_StereoEyeIndex = inputInstanceID & 0x01;
        unity_InstanceID = unity_BaseInstanceID + (inputInstanceID >> 1);
    #else
        unity_InstanceID = inputInstanceID + unity_BaseInstanceID;
    #endif
    }

    #define DEFAULT_UNITY_SETUP_INSTANCE_ID(input)          { UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input));}

    #define UNITY_TRANSFER_INSTANCE_ID(input, output)   output.instanceID = UNITY_GET_INSTANCE_ID(input)
#else
    #define DEFAULT_UNITY_SETUP_INSTANCE_ID(input)
    #define UNITY_TRANSFER_INSTANCE_ID(input, output)
#endif

#if !defined(UNITY_SETUP_INSTANCE_ID)
#   define UNITY_SETUP_INSTANCE_ID(input) DEFAULT_UNITY_SETUP_INSTANCE_ID(input)
#endif

/**********************************************/
// screen space textures

///////////////////////////////////////////////

        struct Attributes
        {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
//#if defined(STEREO_INSTANCING_ON)
//            uint instanceID : SV_InstanceID;
//#endif
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 vertex : SV_POSITION;
            float2 texcoord : TEXCOORD0;
//#if defined(STEREO_INSTANCING_ON)
//            uint stereoTargetEyeIndex : SV_RenderTargetArrayIndex;
//#endif
            UNITY_VERTEX_OUTPUT_STEREO
        };

//#if defined(STEREO_INSTANCING_ON)
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
        Texture2DArray _MainTex;
        SamplerState sampler_MainTex;
#else
        sampler2D _MainTex;
#endif
        float4 _MainTex_ST;

        Varyings Vert(Attributes v)
        {
            Varyings o;

            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            o.vertex = float4(v.vertex.xy * 2.0 - 1.0, 0.0, 1.0);
            o.texcoord = v.texcoord;

            #if UNITY_UV_STARTS_AT_TOP
            o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
            #endif

            o.texcoord = o.texcoord * _MainTex_ST.xy + _MainTex_ST.zw; // We need this for VR

//#if defined(STEREO_INSTANCING_ON)
//            o.stereoTargetEyeIndex = v.instanceID & 0x01;
//#endif

            return o;
        }

        float4 Frag(Varyings i) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
//#if defined(STEREO_INSTANCING_ON)
#if defined(UNITY_STEREO_INSTANCING_ENABLED)|| defined(UNITY_STEREO_MULTIVIEW_ENABLED)
            //float eyeIndex = i.stereoTargetEyeIndex;
            //float4 color = _MainTex.Sample(sampler_MainTex, float3(i.texcoord, eyeIndex));
            float4 color = _MainTex.Sample(sampler_MainTex, float3(i.texcoord, unity_StereoEyeIndex));
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
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
//#if defined(STEREO_INSTANCING_ON)
#if defined(UNITY_STEREO_INSTANCING_ENABLED)|| defined(UNITY_STEREO_MULTIVIEW_ENABLED)
            //float eyeIndex = i.stereoTargetEyeIndex;
            //float4 color = _MainTex.Sample(sampler_MainTex, float3(i.texcoord, eyeIndex));
            float4 color = _MainTex.Sample(sampler_MainTex, float3(i.texcoord, unity_StereoEyeIndex));
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
