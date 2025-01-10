using UnityEngine;

namespace DGX.SRP
{
    public enum CameraRenderIntervalMode
    {
        None,
        Frame,
        Time
    }
    
    [System.Serializable]
    public class CameraRenderSettingsData
    {
        public CameraRenderIntervalMode IntervalMode;
        public float RenderTimeInverval;
        public int RenderFrameInverval;
        public bool LowResRendering;
        public bool RenderShadows = true;
        public bool SkipDeferredPass = false;
    }
    
    public class CameraRenderSettings : MonoBehaviour
    {
        public CameraRenderSettingsData Settings = new()
        {
            IntervalMode = CameraRenderIntervalMode.Time,
            RenderTimeInverval = 1/30.0f,
            RenderFrameInverval = 1,
        };
    }
}
