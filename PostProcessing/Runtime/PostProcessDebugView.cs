using System;

namespace UnityEngine.Experimental.PostProcessing
{
    // TODO: Make a nice debug view with ALL TEH SHIT
    [Serializable]
    public sealed class PostProcessDebugView
    {
        public enum Monitor
        {
            None,
            Histogram,
            Waveform,
            Vectorscope
        }

        public bool display = false;
        public Monitor monitor = Monitor.Waveform;
        public bool lightMeter = true;

        RenderTexture m_LightMeterRT;
        GUIStyle m_TickLabelStyle;

        public bool IsEnabledAndSupported()
        {
            return display
                && (lightMeter || monitor != Monitor.None)
                && SystemInfo.supportsComputeShaders;
        }

        internal void Release()
        {
            RuntimeUtilities.Destroy(m_LightMeterRT);
            m_LightMeterRT = null;
        }

        internal void OnGUI(PostProcessRenderContext context)
        {
            if (context == null || context.camera == null)
                return;

            if (lightMeter)
                OnLightMeterGUI(context);
        }

        void OnLightMeterGUI(PostProcessRenderContext context)
        {
            if (m_TickLabelStyle == null)
            {
                m_TickLabelStyle = new GUIStyle("Label")
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter
                };
            }

            var histogram = context.logHistogram;
            int kMargin = 8;
            int x = kMargin;
            int w = (int)(context.width * (3 / 5f) - kMargin * 2);
            int h = context.height / 4;
            int y = context.height - h - kMargin - 30;
            var rect = new Rect(x, y, w, h);

            if (Event.current.type == EventType.Repaint)
            {
                CheckTexture(ref m_LightMeterRT, (int)rect.width, (int)rect.height);

                var material = context.propertySheets.Get(context.resources.shaders.lightMeter).material;
                material.shaderKeywords = null;
                material.SetBuffer(Uniforms._HistogramBuffer, histogram.data);

                var scaleOffsetRes = histogram.GetHistogramScaleOffsetRes(context);
                scaleOffsetRes.z = 1f / rect.width;
                scaleOffsetRes.w = 1f / rect.height;

                material.SetVector(Uniforms._ScaleOffsetRes, scaleOffsetRes);

                if (context.logLut != null)
                {
                    material.EnableKeyword("COLOR_GRADING_HDR");
                    material.SetTexture(Uniforms._Lut3D, context.logLut);
                }

                if (context.autoExposure != null)
                {
                    var settings = context.autoExposure;

                    // Make sure filtering values are correct to avoid apocalyptic consequences
                    float lowPercent = settings.filtering.value.x;
                    float highPercent = settings.filtering.value.y;
                    const float kMinDelta = 1e-2f;
                    highPercent = Mathf.Clamp(highPercent, 1f + kMinDelta, 99f);
                    lowPercent = Mathf.Clamp(lowPercent, 1f, highPercent - kMinDelta);

                    material.EnableKeyword("AUTO_EXPOSURE");
                    material.SetVector(Uniforms._Params, new Vector4(lowPercent * 0.01f, highPercent * 0.01f, RuntimeUtilities.Exp2(settings.minLuminance.value), RuntimeUtilities.Exp2(settings.maxLuminance.value)));
                }

                RuntimeUtilities.BlitFullscreenTriangle(null, m_LightMeterRT, material, 0);
                GUI.DrawTexture(rect, m_LightMeterRT);
            }

            // Labels
            rect.y += rect.height;
            rect.height = 30;
            int maxSize = Mathf.FloorToInt(rect.width / (LogHistogram.rangeMax - LogHistogram.rangeMin + 2));

            GUI.DrawTexture(rect, RuntimeUtilities.blackTexture);

            GUILayout.BeginArea(rect);
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(4);

                    for (int i = LogHistogram.rangeMin; i < LogHistogram.rangeMax; i++)
                    {
                        GUILayout.Label(i + "\n" + RuntimeUtilities.Exp2(i).ToString("0.###"), m_TickLabelStyle, GUILayout.Width(maxSize));
                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.Label(LogHistogram.rangeMax + "\n" + RuntimeUtilities.Exp2(LogHistogram.rangeMax).ToString("0.###"), m_TickLabelStyle, GUILayout.Width(maxSize));
                    GUILayout.Space(4);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        void CheckTexture(ref RenderTexture rt, int width, int height)
        {
            if (rt == null || !rt.IsCreated() || rt.width != width || rt.height != height)
            {
                RuntimeUtilities.Destroy(rt);

                rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                {
                    anisoLevel = 0,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };

                rt.Create();
            }
        }
    }
}
