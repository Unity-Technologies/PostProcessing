using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.PostProcessing
{
    [Serializable]
    public sealed class PostProcessMonitors
    {
        public LightMeterMonitor lightMeter;
        public HistogramMonitor histogram;
        public WaveformMonitor waveform;
        public VectorscopeMonitor vectorscope;

        Dictionary<MonitorType, Monitor> m_Monitors;

        public void RequestMonitorPass(MonitorType monitor)
        {
            m_Monitors[monitor].requested = true;
        }

        internal void OnEnable()
        {
            if (lightMeter == null)
                lightMeter = new LightMeterMonitor();

            if (histogram == null)
                histogram = new HistogramMonitor();

            if (waveform == null)
                waveform = new WaveformMonitor();

            if (vectorscope == null)
                vectorscope = new VectorscopeMonitor();

            m_Monitors = new Dictionary<MonitorType, Monitor>
            {
                { MonitorType.LightMeter, lightMeter },
                { MonitorType.Histogram, histogram },
                { MonitorType.Waveform, waveform },
                { MonitorType.Vectorscope, vectorscope }
            };

            foreach (var kvp in m_Monitors)
                kvp.Value.OnEnable();
        }

        internal void OnDisable()
        {
            foreach (var kvp in m_Monitors)
                kvp.Value.OnDisable();
        }

        internal void Render(PostProcessRenderContext context)
        {
            bool anyActive = false;
            bool needsHalfRes = false;

            foreach (var kvp in m_Monitors)
            {
                bool active = kvp.Value.IsRequestedAndSupported();
                anyActive |= active;
                needsHalfRes |= active && kvp.Value.NeedsHalfRes();
            }

            var cmd = context.command;
            if (anyActive)
                cmd.BeginSample("Monitors");

            if (needsHalfRes)
            {
                cmd.GetTemporaryRT(ShaderIDs.HalfResFinalCopy, context.width / 2, context.height / 2, 0, FilterMode.Bilinear, context.sourceFormat);
                cmd.Blit(context.destination, ShaderIDs.HalfResFinalCopy);
            }

            foreach (var kvp in m_Monitors)
            {
                var monitor = kvp.Value;

                if (monitor.requested)
                    monitor.Render(context);

                monitor.requested = false;
            }

            if (needsHalfRes)
                cmd.ReleaseTemporaryRT(ShaderIDs.HalfResFinalCopy);
            
            if (anyActive)
                cmd.EndSample("Monitors");
        }
    }
}
