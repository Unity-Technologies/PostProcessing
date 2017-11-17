using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.PostProcessing
{
    // Temporary code dump until the texture format refactor goes into trunk...
    public static class TextureFormatUtilities
    {
        static Dictionary<TextureFormat, RenderTextureFormat> s_FormatMap;

        static TextureFormatUtilities()
        {
            s_FormatMap = new Dictionary<TextureFormat, RenderTextureFormat>
            {
                { TextureFormat.Alpha8, RenderTextureFormat.ARGB32 },
                { TextureFormat.ARGB4444, RenderTextureFormat.ARGB4444 },
                { TextureFormat.RGB24, RenderTextureFormat.ARGB32 },
                { TextureFormat.RGBA32, RenderTextureFormat.ARGB32 },
                { TextureFormat.ARGB32, RenderTextureFormat.ARGB32 },
                { TextureFormat.RGB565, RenderTextureFormat.RGB565 },
                { TextureFormat.R16, RenderTextureFormat.RHalf },
                { TextureFormat.DXT1, RenderTextureFormat.ARGB32 },
                { TextureFormat.DXT5, RenderTextureFormat.ARGB32 },
                { TextureFormat.RGBA4444, RenderTextureFormat.ARGB4444 },
                { TextureFormat.BGRA32, RenderTextureFormat.ARGB32 },
                { TextureFormat.RHalf, RenderTextureFormat.RHalf },
                { TextureFormat.RGHalf, RenderTextureFormat.RGHalf },
                { TextureFormat.RGBAHalf, RenderTextureFormat.ARGBHalf },
                { TextureFormat.RFloat, RenderTextureFormat.RFloat },
                { TextureFormat.RGFloat, RenderTextureFormat.RGFloat },
                { TextureFormat.RGBAFloat, RenderTextureFormat.ARGBFloat },
                { TextureFormat.RGB9e5Float, RenderTextureFormat.ARGBHalf },
                { TextureFormat.BC4, RenderTextureFormat.R8 },
                { TextureFormat.BC5, RenderTextureFormat.RGHalf },
                { TextureFormat.BC6H, RenderTextureFormat.ARGBHalf },
                { TextureFormat.BC7, RenderTextureFormat.ARGB32 },
            #if !UNITY_IOS && !UNITY_TVOS
                { TextureFormat.DXT1Crunched, RenderTextureFormat.ARGB32 },
                { TextureFormat.DXT5Crunched, RenderTextureFormat.ARGB32 },
            #endif
                { TextureFormat.PVRTC_RGB2, RenderTextureFormat.ARGB32 },
                { TextureFormat.PVRTC_RGBA2, RenderTextureFormat.ARGB32 },
                { TextureFormat.PVRTC_RGB4, RenderTextureFormat.ARGB32 },
                { TextureFormat.PVRTC_RGBA4, RenderTextureFormat.ARGB32 },
            #if !UNITY_2018_1_OR_NEWER
                { TextureFormat.ATC_RGB4, RenderTextureFormat.ARGB32 },
                { TextureFormat.ATC_RGBA8, RenderTextureFormat.ARGB32 },
            #endif
                { TextureFormat.ETC_RGB4, RenderTextureFormat.ARGB32 },
                { TextureFormat.ETC2_RGB, RenderTextureFormat.ARGB32 },
                { TextureFormat.ETC2_RGBA1, RenderTextureFormat.ARGB32 },
                { TextureFormat.ETC2_RGBA8, RenderTextureFormat.ARGB32 },
                { TextureFormat.ASTC_RGB_4x4, RenderTextureFormat.ARGB32 },
                { TextureFormat.ASTC_RGB_5x5, RenderTextureFormat.ARGB32 },
                { TextureFormat.ASTC_RGB_6x6, RenderTextureFormat.ARGB32 },
                { TextureFormat.ASTC_RGB_8x8, RenderTextureFormat.ARGB32 },
                { TextureFormat.ASTC_RGB_10x10, RenderTextureFormat.ARGB32 },
                { TextureFormat.ASTC_RGB_12x12, RenderTextureFormat.ARGB32 },
                { TextureFormat.ASTC_RGBA_4x4, RenderTextureFormat.ARGB32 },
                { TextureFormat.ASTC_RGBA_5x5, RenderTextureFormat.ARGB32 },
                { TextureFormat.ASTC_RGBA_6x6, RenderTextureFormat.ARGB32 },
                { TextureFormat.ASTC_RGBA_8x8, RenderTextureFormat.ARGB32 },
                { TextureFormat.ASTC_RGBA_10x10, RenderTextureFormat.ARGB32 },
                { TextureFormat.ASTC_RGBA_12x12, RenderTextureFormat.ARGB32 },
                { TextureFormat.ETC_RGB4_3DS, RenderTextureFormat.ARGB32 },
                { TextureFormat.ETC_RGBA8_3DS, RenderTextureFormat.ARGB32 }
            };
        }

        public static RenderTextureFormat GetUncompressedRenderTextureFormat(Texture texture)
        {
            Assert.IsNotNull(texture);

            if (texture is RenderTexture)
                return (texture as RenderTexture).format;

            if (texture is Texture2D)
            {
                var inFormat = ((Texture2D)texture).format;
                RenderTextureFormat outFormat;

                if (!s_FormatMap.TryGetValue(inFormat, out outFormat))
                    throw new NotSupportedException("Texture format not supported");

                return outFormat;
            }

            return RenderTextureFormat.Default;
        }
    }
}
