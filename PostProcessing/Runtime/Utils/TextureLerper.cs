using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.PostProcessing
{
    class TextureLerper
    {
        static TextureLerper m_Instance;
        internal static TextureLerper instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new TextureLerper();

                return m_Instance;
            }
        }

        CommandBuffer m_Command;
        PropertySheetFactory m_PropertySheets;
        PostProcessResources m_Resources;

        List<RenderTexture> m_Recycled;
        List<RenderTexture> m_Actives;

        TextureLerper()
        {
            m_Recycled = new List<RenderTexture>();
            m_Actives = new List<RenderTexture>();
        }

        internal void BeginFrame(PostProcessRenderContext context)
        {
            m_Command = context.command;
            m_PropertySheets = context.propertySheets;
            m_Resources = context.resources;
        }

        internal void EndFrame()
        {
            // Release any remaining RT in the recycled list
            if (m_Recycled.Count > 0)
            {
                foreach (var rt in m_Recycled)
                    RuntimeUtilities.Destroy(rt);

                m_Recycled.Clear();
            }

            // There's a high probability that RTs will be requested in the same order on next
            // frame so keep them in the same order
            if (m_Actives.Count > 0)
            {
                foreach (var rt in m_Actives)
                    m_Recycled.Add(rt);

                m_Actives.Clear();
            }
        }

        RenderTexture Get(RenderTextureFormat format, int w, int h, int d = 1)
        {
            RenderTexture rt = null;
            int i, len = m_Recycled.Count;

            for (i = 0; i < len; i++)
            {
                var r = m_Recycled[i];
                if (r.width == w && r.height == h && r.volumeDepth == d && r.format == format)
                {
                    rt = r;
                    break;
                }
            }

            if (rt == null)
            {
                rt = new RenderTexture(w, h, d, format)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0,
                    volumeDepth = d
                };
            }
            else m_Recycled.RemoveAt(i);

            m_Actives.Add(rt);
            return rt;
        }

        internal Texture Lerp(Texture from, Texture to, float t)
        {
            Assert.IsNotNull(from);
            Assert.IsNotNull(to);

            bool is3d = to is Texture3D
                    || (to is RenderTexture && ((RenderTexture)to).volumeDepth > 1);

            RenderTexture rt = null;

            if (is3d)
            {
                // TODO: Texture3D lerping
            }
            else
            {
                var format = TextureFormatUtilities.GetUncompressedRenderTextureFormat(to);
                rt = Get(format, to.width, to.height);

                var sheet = m_PropertySheets.Get(m_Resources.shaders.texture2dLerp);
                sheet.properties.SetTexture(Uniforms._To, to);
                sheet.properties.SetFloat(Uniforms._Interp, t);

                m_Command.BlitFullscreenTriangle(from, rt, sheet, 0);
            }

            return rt;
        }

        internal void Clear()
        {
            foreach (var rt in m_Actives)
                RuntimeUtilities.Destroy(rt);

            foreach (var rt in m_Recycled)
                RuntimeUtilities.Destroy(rt);

            m_Actives.Clear();
            m_Recycled.Clear();
        }
    }
}
