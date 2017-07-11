namespace UnityEngine.Rendering.PostProcessing
{
    [ExecuteInEditMode]
    public sealed class PostProcessDebug : MonoBehaviour
    {
        public PostProcessLayer postProcessLayer;

        void Reset()
        {
            postProcessLayer = GetComponent<PostProcessLayer>();
        }

        void OnGUI()
        {
            if (postProcessLayer == null || !postProcessLayer.enabled)
                return;

            var monitors = postProcessLayer.monitors;
            var rect = new Rect(5, 5, 0, 0);

            DrawMonitor(ref rect, monitors.lightMeter);
            DrawMonitor(ref rect, monitors.histogram);
            DrawMonitor(ref rect, monitors.waveform);
            DrawMonitor(ref rect, monitors.vectorscope);
        }

        void DrawMonitor(ref Rect rect, Monitor monitor)
        {
            if (!monitor.IsEnabledAndSupported())
                return;

            if (monitor.output == null)
                return;

            rect.width = monitor.output.width;
            rect.height = monitor.output.height;
            GUI.DrawTexture(rect, monitor.output);
            rect.x += monitor.output.width + 5f;
        }
    }
}

