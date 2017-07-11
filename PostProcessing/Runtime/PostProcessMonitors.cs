using System;

namespace UnityEngine.Rendering.PostProcessing
{
    [Serializable]
    public sealed class PostProcessMonitors
    {
        public LightMeterMonitor lightMeter;
        public HistogramMonitor histogram;
        public WaveformMonitor waveform;
        public VectorscopeMonitor vectorscope;

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

            lightMeter.OnEnable();
            histogram.OnEnable();
            waveform.OnEnable();
            vectorscope.OnEnable();
        }

        internal void OnDisable()
        {
            lightMeter.OnDisable();
            histogram.OnDisable();
            waveform.OnDisable();
            vectorscope.OnDisable();
        }

        internal void Render(PostProcessRenderContext context)
        {
            bool lightMeterActive = lightMeter.IsEnabledAndSupported();
            bool histogramActive = histogram.IsEnabledAndSupported();
            bool waveformActive = waveform.IsEnabledAndSupported();
            bool vectorscopeActive = vectorscope.IsEnabledAndSupported();
            bool needHalfRes = histogramActive || vectorscopeActive || waveformActive;
            bool anyActive = lightMeterActive
                || histogramActive
                || waveformActive
                || vectorscopeActive;

            var cmd = context.command;
            if (anyActive)
                cmd.BeginSample("Monitors");

            if (needHalfRes)
            {
                cmd.GetTemporaryRT(ShaderIDs.HalfResFinalCopy, context.width / 2, context.height / 2, 0, FilterMode.Bilinear, context.sourceFormat);
                cmd.Blit(context.destination, ShaderIDs.HalfResFinalCopy);
            }

            if (lightMeterActive)
                lightMeter.Render(context);

            if (histogramActive)
                histogram.Render(context);

            if (waveformActive)
                waveform.Render(context);

            if (vectorscopeActive)
                vectorscope.Render(context);

            if (needHalfRes)
                cmd.ReleaseTemporaryRT(ShaderIDs.HalfResFinalCopy);
            
            if (anyActive)
                cmd.EndSample("Monitors");
        }
    }
}
