namespace UnityEngine.Rendering.PostProcessing
{
    [ExecuteInEditMode]
    public sealed class PostProcessDebug : MonoBehaviour
    {
        public PostProcessLayer postProcessLayer;

        public bool lightMeter;
        public bool histogram;
        public bool waveform;
        public bool vectorscope;

        void Reset()
        {
            postProcessLayer = GetComponent<PostProcessLayer>();
        }

        void Update()
        {
            if (lightMeter) postProcessLayer.monitors.RequestMonitorPass(MonitorType.LightMeter);
            if (histogram) postProcessLayer.monitors.RequestMonitorPass(MonitorType.Histogram);
            if (waveform) postProcessLayer.monitors.RequestMonitorPass(MonitorType.Waveform);
            if (vectorscope) postProcessLayer.monitors.RequestMonitorPass(MonitorType.Vectorscope);
        }

        void OnGUI()
        {
            if (postProcessLayer == null || !postProcessLayer.enabled)
                return;

            var monitors = postProcessLayer.monitors;
            var rect = new Rect(5, 5, 0, 0);

            DrawMonitor(ref rect, monitors.lightMeter, lightMeter);
            DrawMonitor(ref rect, monitors.histogram, histogram);
            DrawMonitor(ref rect, monitors.waveform, waveform);
            DrawMonitor(ref rect, monitors.vectorscope, vectorscope);
        }

        void DrawMonitor(ref Rect rect, Monitor monitor, bool enabled)
        {
            if (!enabled || monitor.output == null)
                return;

            rect.width = monitor.output.width;
            rect.height = monitor.output.height;
            GUI.DrawTexture(rect, monitor.output);
            rect.x += monitor.output.width + 5f;
        }
    }
}

