using UnityEngine;

namespace DGX.SRP
{
    public enum ShadowMapSize
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096
    };
    
    [System.Serializable]
    public class ShadowSettings
    {
        public float MaxDistance = 100;
        public ShadowMapSize DirectionalMapSize = ShadowMapSize._2048;
    }
    
    [CreateAssetMenu(menuName = "Rendering/DinogeniX render pipeline asset")]
    public class RenderPipelineAsset : UnityEngine.Rendering.RenderPipelineAsset
    {
        [SerializeField]
        private ShadowSettings _shadowSettings;
        public ShadowSettings ShadowSettings => _shadowSettings;

        public Shader LightingPassShader;
        
        // Unity calls this method before rendering the first frame.
        // If a setting on the Render Pipeline Asset changes, Unity destroys the current Render Pipeline Instance and calls this method again before rendering the next frame
        protected override UnityEngine.Rendering.RenderPipeline CreatePipeline()
        {
            return new RenderPipeline(this);
        }
    }
}