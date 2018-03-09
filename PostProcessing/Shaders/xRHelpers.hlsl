#ifndef UNITY_POSTFX_XR_EXTRA_LIB
#define UNITY_POSTFX_XR_EXTRA_LIB

// Mostly borrowed from SRP UnityInstancing.hlsl

//////////////////////////////////////////
// Stereo instancing or multi-view support
#if (defined(SHADER_API_D3D11)  || defined(SHADER_API_PSSL))
    #define UNITY_SUPPORT_STEREO_INSTANCING
#endif

#if ((defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)) && !(defined(SHADER_API_SWITCH)))
    #define UNITY_SUPPORT_MULTIVIEW
#endif

#if defined(UNITY_SUPPORT_STEREO_INSTANCING) && defined(STEREO_INSTANCING_ON)
    // This might already be flipped on somehow...
    #define UNITY_STEREO_INSTANCING_ENABLED
#endif

#if defined(UNITY_SUPPORT_MULTIVIEW) && defined(STEREO_MULTIVIEW_ON)
    #define UNITY_STEREO_MULTIVIEW_ENABLED
#endif

//////////////////////////////////////////
// Multi-view constants 'hack' to work around drivers

#if defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    // Will this be injected already?
    CBUFFER_START(UnityStereoEyeIndices)
    float4 unity_StereoEyeIndices[2];
    CBUFFER_END

    // This seems to be injected already?
    //CBUFFER_START(UnityStereoEyeIndex)
    //    int unity_StereoEyeIndex;
    //CBUFFER_END
#endif

//////////////////////////////////////////
// Declare and grab instance ID

#if defined(UNITY_STEREO_INSTANCING_ENABLED)
    #ifdef SHADER_API_PSSL
        #define DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID uint instanceID;
        // Unity compiler handles fetching the instance ID from the VGPR
        #define UNITY_GET_INSTANCE_ID(input)    _GETINSTANCEID(input)
    #else
        #define DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID uint instanceID : SV_InstanceID;
        #define UNITY_GET_INSTANCE_ID(input)    input.instanceID
    #endif
#else
    #define DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID
#endif // UNITY_INSTANCING_ENABLED

#if !defined(UNITY_VERTEX_INPUT_INSTANCE_ID)
    #define UNITY_VERTEX_INPUT_INSTANCE_ID DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID
#endif

//////////////////////////////////////////
// Extract stereo eye index out of instance ID

#if defined(UNITY_STEREO_INSTANCING_ENABLED)
    void UnitySetupInstanceID(uint inputInstanceID)
    {
        // Do not configure unity_InstanceID, as we don't need it for normal instancing
        unity_StereoEyeIndex = inputInstanceID & 0x01;
    }

    #define DEFAULT_UNITY_SETUP_INSTANCE_ID(input)          { UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input));}
#else
    #define DEFAULT_UNITY_SETUP_INSTANCE_ID(input)
#endif

#if !defined(UNITY_SETUP_INSTANCE_ID)
#   define UNITY_SETUP_INSTANCE_ID(input) DEFAULT_UNITY_SETUP_INSTANCE_ID(input)
#endif

//////////////////////////////////////////
// Declare, populate, and use interpolated stereoTargetEyeIndex

#ifdef UNITY_STEREO_INSTANCING_ENABLED
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO                          uint stereoTargetEyeIndex : SV_RenderTargetArrayIndex;
    #define DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)       output.stereoTargetEyeIndex = unity_StereoEyeIndex
    #define DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)     unity_StereoEyeIndex = input.stereoTargetEyeIndex;
#elif defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO float stereoTargetEyeIndex : BLENDWEIGHT0;
    // HACK: Workaround for Mali shader compiler issues with directly using GL_ViewID_OVR (GL_OVR_multiview). This array just contains the values 0 and 1.
    #define DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output) output.stereoTargetEyeIndex = unity_StereoEyeIndices[unity_StereoEyeIndex].x;
    #if defined(SHADER_STAGE_VERTEX)
        #define DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
    #else
        #define DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input) unity_StereoEyeIndex = (uint) input.stereoTargetEyeIndex;
    #endif
#else
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO
    #define DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)
    #define DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
#endif

#if !defined(UNITY_VERTEX_OUTPUT_STEREO)
#   define UNITY_VERTEX_OUTPUT_STEREO                           DEFAULT_UNITY_VERTEX_OUTPUT_STEREO
#endif
#if !defined(UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO)
#   define UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)        DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)
#endif
#if !defined(UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX)
#   define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)      DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
#endif

#endif // UNITY_POSTFX_XR_EXTRA_LIB
