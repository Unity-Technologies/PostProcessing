// For now OpenGL is considered at GLES2 level
#define UNITY_UV_STARTS_AT_TOP 0
#define UNITY_REVERSED_Z 0
#define UNITY_GATHER_SUPPORTED 0
#define UNITY_CAN_READ_POSITION_IN_FRAGMENT_PROGRAM 1
#define UNITY_TEXARRAY_SUPPORTED (SHADER_TARGET >= 30)

#define ERROR_ON_UNSUPPORTED_FUNCTION(funcName) #error ##funcName is not supported on GLES 2.0

#define TEXTURE2D_SAMPLER2D(textureName, samplerName) sampler2D textureName
#define TEXTURE3D_SAMPLER3D(textureName, samplerName) sampler3D textureName
#if UNITY_TEXARRAY_SUPPORTED
#define TEXTURE2D_ARRAY_SAMPLER2D(textureName, samplerName) Texture2DArray textureName; SamplerState samplerName
#else
#define TEXTURE2D_ARRAY_SAMPLER2D(textureName, samplerName) samplerCUBE textureName // No support to texture2DArray
#endif


#define TEXTURE2D(textureName) sampler2D textureName
#define SAMPLER2D(samplerName)
#if UNITY_TEXARRAY_SUPPORTED
#define TEXTURE2D_ARRAY(textureName) Texture2DArray textureName
#else
#define TEXTURE2D_ARRAY(textureName) samplerCUBE textureName // No support to texture2DArray
#endif

#define TEXTURE3D(textureName) sampler3D textureName
#define SAMPLER3D(samplerName)

#define TEXTURE2D_ARGS(textureName, samplerName) sampler2D textureName
#define TEXTURE2D_PARAM(textureName, samplerName) textureName

#define TEXTURE3D_ARGS(textureName, samplerName) sampler3D textureName
#define TEXTURE3D_PARAM(textureName, samplerName) textureName

#if UNITY_TEXARRAY_SUPPORTED
#define TEXTURE2D_ARRAY_ARGS(textureName, samplerName) Texture2DArray textureName, SamplerState samplerName
#define TEXTURE2D_ARRAY_PARAM(textureName, samplerName) textureName, samplerName
#else
#define TEXTURE2D_ARRAY_ARGS(textureName, samplerName) samplerCUBE textureName // No support to texture2DArray
#define TEXTURE2D_ARRAY_PARAM(textureName, samplerName) textureName
#endif

#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2) tex2D(textureName, coord2)
#define SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod) tex2Dlod(textureName, float4(coord2, 0.0, lod))
#if UNITY_TEXARRAY_SUPPORTED
#define SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index) textureName.Sample(samplerName, float3(coord2, index))
#define SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, index, lod) textureName.SampleLevel(samplerName, float3(coord2, index), lod)
#else
#define SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index) ERROR_ON_UNSUPPORTED_FUNCTION(SAMPLE_TEXTURE2D_ARRAY)
#define SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, index, lod) ERROR_ON_UNSUPPORTED_FUNCTION(SAMPLE_TEXTURE2D_ARRAY_LOD)
#endif

#define SAMPLE_TEXTURE3D(textureName, samplerName, coord3) tex3D(textureName, coord3)

#define LOAD_TEXTURE2D(textureName, texelSize, icoord2) tex2D(textureName, icoord2 / texelSize)
#define LOAD_TEXTURE2D_LOD(textureName, texelSize, icoord2) tex2Dlod(textureName, float4(icoord2 / texelSize, 0.0, lod))

#define SAMPLE_DEPTH_TEXTURE(textureName, samplerName, coord2) SAMPLE_TEXTURE2D(textureName, samplerName, coord2).r
#define SAMPLE_DEPTH_TEXTURE_LOD(textureName, samplerName, coord2, lod) SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod).r

#if SHADER_API_GLES
#    define UNITY_BRANCH
#    define UNITY_FLATTEN
#    define UNITY_UNROLL
#    define UNITY_LOOP
#    define UNITY_FASTOPT
#else
#    define UNITY_BRANCH    [branch]
#    define UNITY_FLATTEN   [flatten]
#    define UNITY_UNROLL    [unroll]
#    define UNITY_LOOP      [loop]
#    define UNITY_FASTOPT   [fastopt]
#endif

#define CBUFFER_START(name)
#define CBUFFER_END

#define FXAA_HLSL_3 1
#define SMAA_HLSL_3 1

// pragma exclude_renderers is only supported since Unity 2018.1 for compute shaders
#if UNITY_VERSION < 201810 && !defined(SHADER_API_GLCORE)
#    define DISABLE_COMPUTE_SHADERS 1
#    define TRIVIAL_COMPUTE_KERNEL(name) [numthreads(1, 1, 1)] void name() {}
#endif
