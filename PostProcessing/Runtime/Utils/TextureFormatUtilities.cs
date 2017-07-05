using System;

namespace UnityEngine.Rendering.PostProcessing
{
    // Temporary code dump until the texture format refactor goes into trunk...
    public static class TextureFormatUtilities
    {
        public static RenderTextureFormat GetUncompressedRenderTextureFormat(Texture texture)
        {
            if (texture is RenderTexture)
                return (texture as RenderTexture).format;

            if (texture is Texture2D)
            {
                switch ((texture as Texture2D).format)
                {
                    case TextureFormat.Alpha8: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ARGB4444: return RenderTextureFormat.ARGB4444;
                    case TextureFormat.RGB24: return RenderTextureFormat.ARGB32;
                    case TextureFormat.RGBA32: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ARGB32: return RenderTextureFormat.ARGB32;
                    case TextureFormat.RGB565: return RenderTextureFormat.RGB565;
                    case TextureFormat.R16: return RenderTextureFormat.RHalf; // ???
                    case TextureFormat.DXT1: return RenderTextureFormat.ARGB32;
                    case TextureFormat.DXT5: return RenderTextureFormat.ARGB32;
                    case TextureFormat.RGBA4444: return RenderTextureFormat.ARGB4444;
                    case TextureFormat.BGRA32: return RenderTextureFormat.ARGB32;
                    case TextureFormat.RHalf: return RenderTextureFormat.RHalf;
                    case TextureFormat.RGHalf: return RenderTextureFormat.RGHalf;
                    case TextureFormat.RGBAHalf: return RenderTextureFormat.ARGBHalf;
                    case TextureFormat.RFloat: return RenderTextureFormat.RFloat;
                    case TextureFormat.RGFloat: return RenderTextureFormat.RGFloat;
                    case TextureFormat.RGBAFloat: return RenderTextureFormat.ARGBFloat;
                    case TextureFormat.RGB9e5Float: return RenderTextureFormat.ARGBHalf;
                    case TextureFormat.BC4: return RenderTextureFormat.R8;
                    case TextureFormat.BC5: return RenderTextureFormat.RGHalf;
                    case TextureFormat.BC6H: return RenderTextureFormat.ARGBHalf;
                    case TextureFormat.BC7: return RenderTextureFormat.ARGB32;
                #if !UNITY_IOS
                    case TextureFormat.DXT1Crunched: return RenderTextureFormat.ARGB32;
                    case TextureFormat.DXT5Crunched: return RenderTextureFormat.ARGB32;
                #endif
                    case TextureFormat.PVRTC_RGB2: return RenderTextureFormat.ARGB32;
                    case TextureFormat.PVRTC_RGBA2: return RenderTextureFormat.ARGB32;
                    case TextureFormat.PVRTC_RGB4: return RenderTextureFormat.ARGB32;
                    case TextureFormat.PVRTC_RGBA4: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ETC_RGB4: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ATC_RGB4: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ATC_RGBA8: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ETC2_RGB: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ETC2_RGBA1: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ETC2_RGBA8: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ASTC_RGB_4x4: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ASTC_RGB_5x5: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ASTC_RGB_6x6: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ASTC_RGB_8x8: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ASTC_RGB_10x10: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ASTC_RGB_12x12: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ASTC_RGBA_4x4: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ASTC_RGBA_5x5: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ASTC_RGBA_6x6: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ASTC_RGBA_8x8: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ASTC_RGBA_10x10: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ASTC_RGBA_12x12: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ETC_RGB4_3DS: return RenderTextureFormat.ARGB32;
                    case TextureFormat.ETC_RGBA8_3DS: return RenderTextureFormat.ARGB32;
                    case TextureFormat.EAC_R: goto default;
                    case TextureFormat.EAC_R_SIGNED: goto default;
                    case TextureFormat.EAC_RG: goto default;
                    case TextureFormat.EAC_RG_SIGNED: goto default;
                    case TextureFormat.YUY2: goto default;
                    default:
                        throw new NotSupportedException("Texture format not supported");
                }
            }

            return RenderTextureFormat.Default;
        }
    }
}
