namespace UnityEngine.Experimental.PostProcessing
{
    // Pre-hashed uniform ids - naming conventions are a bit off in this file as we use the same
    // fields names as in the shaders for ease of use
    static class Uniforms
    {
        // Used by `PostProcessLayer` to ping-pong targets in render lists
        internal static readonly int[] _TempTargetPool =
        {
            Shader.PropertyToID("_TempTargetPool1"),
            Shader.PropertyToID("_TempTargetPool2"),
            Shader.PropertyToID("_TempTargetPool3"),
            Shader.PropertyToID("_TempTargetPool4"),
            Shader.PropertyToID("_TempTargetPool5")
        };

        internal static readonly int _LegacyTemp                      = Shader.PropertyToID("_LegacyTemp");
        internal static readonly int _MainTex                         = Shader.PropertyToID("_MainTex");

        internal static readonly int _AATemp                          = Shader.PropertyToID("_AATemp");
        internal static readonly int _Jitter                          = Shader.PropertyToID("_Jitter");
        internal static readonly int _SharpenParameters               = Shader.PropertyToID("_SharpenParameters");
        internal static readonly int _FinalBlendParameters            = Shader.PropertyToID("_FinalBlendParameters");
        internal static readonly int _HistoryTex                      = Shader.PropertyToID("_HistoryTex");

        internal static readonly int _MotionBlurTemp                  = Shader.PropertyToID("_MotionBlurTemp");
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
        internal static readonly int _DepthOfFieldParams              = Shader.PropertyToID("_DepthOfFieldParams");

        internal static readonly int _AutoExposureCopyTex             = Shader.PropertyToID("_AutoExposureCopyTex");
        internal static readonly int _AutoExposureTex                 = Shader.PropertyToID("_AutoExposureTex");
        internal static readonly int _HistogramBuffer                 = Shader.PropertyToID("_HistogramBuffer");
        internal static readonly int _Params                          = Shader.PropertyToID("_Params");
        internal static readonly int _Speed                           = Shader.PropertyToID("_Speed");
        internal static readonly int _ScaleOffsetRes                  = Shader.PropertyToID("_ScaleOffsetRes");
        internal static readonly int _ExposureCompensation            = Shader.PropertyToID("_ExposureCompensation");

        internal static readonly int _BloomTex                        = Shader.PropertyToID("_BloomTex");
        internal static readonly int _Bloom_DirtTex                   = Shader.PropertyToID("_Bloom_DirtTex");
        internal static readonly int _SampleScale                     = Shader.PropertyToID("_SampleScale");
        internal static readonly int _Bloom_Settings                  = Shader.PropertyToID("_Bloom_Settings");
        internal static readonly int _Threshold                       = Shader.PropertyToID("_Threshold");
        internal static readonly int _Curve                           = Shader.PropertyToID("_Curve");
        internal static readonly int _Response                        = Shader.PropertyToID("_Response");

        internal static readonly int _ChromaticAberration_Amount      = Shader.PropertyToID("_ChromaticAberration_Amount");
        internal static readonly int _ChromaticAberration_SpectralLut = Shader.PropertyToID("_ChromaticAberration_SpectralLut");

        internal static readonly int _LogLut                          = Shader.PropertyToID("_LogLut");
        internal static readonly int _LogLut_Params                   = Shader.PropertyToID("_LogLut_Params");

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
    }
}
