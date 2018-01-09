// Small shim for VRSettings/XRSettings on XboxOne and Switch
#if !UNITY_2018_2_OR_NEWER && (UNITY_XBOXONE || UNITY_SWITCH) && !UNITY_EDITOR
using System;

namespace UnityEngine.XR
{
#if UNITY_2017_2_OR_NEWER
    public static class XRSettings
#elif UNITY_5_6_OR_NEWER
    public static class VRSettings
#endif
    {
        public static bool enabled { get; set; }
        public static bool isDeviceActive { get; private set; }
        public static bool showDeviceView { get; set; }
        [Obsolete("renderScale is deprecated, use XRSettings.eyeTextureResolutionScale instead (UnityUpgradable) -> eyeTextureResolutionScale")]
        public static float renderScale { get; set; }
        public static float eyeTextureResolutionScale { get; set; }
        public static int eyeTextureWidth { get; private set; }
        public static int eyeTextureHeight { get; private set; }
        public static RenderTextureDescriptor eyeTextureDesc { get; private set; }
        public static float renderViewportScale { get; set; }
        public static float occlusionMaskScale { get; set; }
        public static bool useOcclusionMesh { get; set; }
        public static string loadedDeviceName { get; private set; }
        public static string[] supportedDevices { get; private set; }
        public static void LoadDeviceByName(string deviceName) { }
        public static void LoadDeviceByName(string[] prioritizedDeviceNameList) { }
    }
}
#endif
