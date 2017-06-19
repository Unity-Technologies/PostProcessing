namespace UnityEngine.Experimental.PostProcessing
{
    // Pre-hashed uniform ids - naming conventions are a bit off in this file as we use the same
    // fields names as in the shaders for ease of use
    static class Uniforms
    {
        internal static readonly int _MainTex                         = Shader.PropertyToID("_MainTex");

        internal static readonly int _Jitter                          = Shader.PropertyToID("_Jitter");
        internal static readonly int _SharpenParameters               = Shader.PropertyToID("_SharpenParameters");
        internal static readonly int _FinalBlendParameters            = Shader.PropertyToID("_FinalBlendParameters");
        internal static readonly int _HistoryTex                      = Shader.PropertyToID("_HistoryTex");
        
        internal static readonly int _SMAA_Flip                       = Shader.PropertyToID("_SMAA_Flip");
        internal static readonly int _SMAA_Flop                       = Shader.PropertyToID("_SMAA_Flop");

        internal static readonly int _VelocityScale                   = Shader.PropertyToID("_VelocityScale");
        internal static readonly int _MaxBlurRadius                   = Shader.PropertyToID("_MaxBlurRadius");
        internal static readonly int _RcpMaxBlurRadius                = Shader.PropertyToID("_RcpMaxBlurRadius");
        internal static readonly int _VelocityTex                     = Shader.PropertyToID("_VelocityTex");
        internal static readonly int _Tile2RT                         = Shader.PropertyToID("_Tile2RT");
        internal static readonly int _Tile4RT                         = Shader.PropertyToID("_Tile4RT");
        internal static readonly int _Tile8RT                         = Shader.PropertyToID("_Tile8RT");
        internal static readonly int _TileMaxOffs                     = Shader.PropertyToID("_TileMaxOffs");
        internal static readonly int _TileMaxLoop                     = Shader.PropertyToID("_TileMaxLoop");
        internal static readonly int _TileVRT                         = Shader.PropertyToID("_TileVRT");
        internal static readonly int _NeighborMaxTex                  = Shader.PropertyToID("_NeighborMaxTex");
        internal static readonly int _LoopCount                       = Shader.PropertyToID("_LoopCount");

        internal static readonly int _DepthOfFieldTemp                = Shader.PropertyToID("_DepthOfFieldTemp");
        internal static readonly int _DepthOfFieldTex                 = Shader.PropertyToID("_DepthOfFieldTex");
        internal static readonly int _Distance                        = Shader.PropertyToID("_Distance");
        internal static readonly int _LensCoeff                       = Shader.PropertyToID("_LensCoeff");
        internal static readonly int _MaxCoC                          = Shader.PropertyToID("_MaxCoC");
        internal static readonly int _RcpMaxCoC                       = Shader.PropertyToID("_RcpMaxCoC");
        internal static readonly int _RcpAspect                       = Shader.PropertyToID("_RcpAspect");
        internal static readonly int _CoCTex                          = Shader.PropertyToID("_CoCTex");
        internal static readonly int _TaaParams                       = Shader.PropertyToID("_TaaParams");

        internal static readonly int _AutoExposureCopyTex             = Shader.PropertyToID("_AutoExposureCopyTex");
        internal static readonly int _AutoExposureTex                 = Shader.PropertyToID("_AutoExposureTex");
        internal static readonly int _HistogramBuffer                 = Shader.PropertyToID("_HistogramBuffer");
        internal static readonly int _Params                          = Shader.PropertyToID("_Params");
        internal static readonly int _Speed                           = Shader.PropertyToID("_Speed");
        internal static readonly int _ScaleOffsetRes                  = Shader.PropertyToID("_ScaleOffsetRes");
        internal static readonly int _ExposureCompensation            = Shader.PropertyToID("_ExposureCompensation");

        internal static readonly int _BloomTex                        = Shader.PropertyToID("_BloomTex");
        internal static readonly int _SampleScale                     = Shader.PropertyToID("_SampleScale");
        internal static readonly int _Threshold                       = Shader.PropertyToID("_Threshold");
        internal static readonly int _Bloom_DirtTex                   = Shader.PropertyToID("_Bloom_DirtTex");
        internal static readonly int _Bloom_Settings                  = Shader.PropertyToID("_Bloom_Settings");
        internal static readonly int _Bloom_Color                     = Shader.PropertyToID("_Bloom_Color");
        internal static readonly int _Bloom_Threshold                 = Shader.PropertyToID("_Bloom_Threshold");

        internal static readonly int _ChromaticAberration_Amount      = Shader.PropertyToID("_ChromaticAberration_Amount");
        internal static readonly int _ChromaticAberration_SpectralLut = Shader.PropertyToID("_ChromaticAberration_SpectralLut");

        internal static readonly int _Lut2D                           = Shader.PropertyToID("_Lut2D");
        internal static readonly int _Lut3D                           = Shader.PropertyToID("_Lut3D");
        internal static readonly int _Lut3D_Params                    = Shader.PropertyToID("_Lut3D_Params");
        internal static readonly int _Lut2D_Params                    = Shader.PropertyToID("_Lut2D_Params");
        internal static readonly int _PostExposure                    = Shader.PropertyToID("_PostExposure");
        internal static readonly int _ColorBalance                    = Shader.PropertyToID("_ColorBalance");
        internal static readonly int _ColorFilter                     = Shader.PropertyToID("_ColorFilter");
        internal static readonly int _HueSatCon                       = Shader.PropertyToID("_HueSatCon");
        internal static readonly int _Brightness                      = Shader.PropertyToID("_Brightness");
        internal static readonly int _ChannelMixerRed                 = Shader.PropertyToID("_ChannelMixerRed");
        internal static readonly int _ChannelMixerGreen               = Shader.PropertyToID("_ChannelMixerGreen");
        internal static readonly int _ChannelMixerBlue                = Shader.PropertyToID("_ChannelMixerBlue");
        internal static readonly int _Lift                            = Shader.PropertyToID("_Lift");
        internal static readonly int _InvGamma                        = Shader.PropertyToID("_InvGamma");
        internal static readonly int _Gain                            = Shader.PropertyToID("_Gain");
        internal static readonly int _Curves                          = Shader.PropertyToID("_Curves");

        internal static readonly int _Vignette_Color                  = Shader.PropertyToID("_Vignette_Color");
        internal static readonly int _Vignette_Center                 = Shader.PropertyToID("_Vignette_Center");
        internal static readonly int _Vignette_Settings               = Shader.PropertyToID("_Vignette_Settings");
        internal static readonly int _Vignette_Mask                   = Shader.PropertyToID("_Vignette_Mask");
        internal static readonly int _Vignette_Opacity                = Shader.PropertyToID("_Vignette_Opacity");
        internal static readonly int _Vignette_Mode                   = Shader.PropertyToID("_Vignette_Mode");

        internal static readonly int _Grain_Params1                   = Shader.PropertyToID("_Grain_Params1");
        internal static readonly int _Grain_Params2                   = Shader.PropertyToID("_Grain_Params2");
        internal static readonly int _GrainTex                        = Shader.PropertyToID("_GrainTex");
        internal static readonly int _Phase                           = Shader.PropertyToID("_Phase");

        internal static readonly int _DitheringTex                    = Shader.PropertyToID("_DitheringTex");
        internal static readonly int _Dithering_Coords                = Shader.PropertyToID("_Dithering_Coords");

        internal static readonly int _From                            = Shader.PropertyToID("_From");
        internal static readonly int _To                              = Shader.PropertyToID("_To");
        internal static readonly int _Interp                          = Shader.PropertyToID("_Interp");
    }
}
