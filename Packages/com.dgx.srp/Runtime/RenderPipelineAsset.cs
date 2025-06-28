using UnityEngine;
using UnityEngine.Rendering;

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
    [ReloadGroup]
    public class RenderPipelineAsset : RenderPipelineAsset<RenderPipeline>
    {
        [SerializeField]
        private ShadowSettings _shadowSettings;
        public ShadowSettings ShadowSettings => _shadowSettings;

        public Shader LightingPassShader;
        public Shader LinearizeDepthShader;
        
        [Reload("ShaderLibrary/Utils/Blit.shader")]
        public Shader BlitShader;
        [Reload("ShaderLibrary/Utils/CoreBlitColorAndDepth.shader")]
        public Shader BlitColorAndDepthShader;
        
        #if UNITY_EDITOR
        [Header("Editor only")]
        [SerializeField]
        Shader _terrainDetailLitShader;
        
        [Header("Editor only")]
        [SerializeField]
        Shader _terrainDetailGrassShader;
        #endif
        
        public override string renderPipelineShaderTag => string.Empty;

#if UNITY_EDITOR
        public override Shader terrainDetailLitShader => _terrainDetailLitShader;

        public override Shader terrainDetailGrassShader => _terrainDetailGrassShader;
        public override Shader terrainDetailGrassBillboardShader => _terrainDetailGrassShader;
#endif

        // Unity calls this method before rendering the first frame.
        // If a setting on the Render Pipeline Asset changes, Unity destroys the current Render Pipeline Instance and calls this method again before rendering the next frame

        const string defaultAssetName = "DgxRenderPipelineGlobalSettings";
        static string defaultPath => $"Assets/{defaultAssetName}.asset";
        
        protected override UnityEngine.Rendering.RenderPipeline CreatePipeline()
        {
            var result = new RenderPipeline(this);
            
            this.EnsureGlobalSettings();
            
            #if UNITY_EDITOR
            var currentInstance = GraphicsSettings.
                GetSettingsForRenderPipeline<RenderPipeline>() as DgxRenderPipelineGlobalSettings;

            if (RenderPipelineGlobalSettingsUtils.TryEnsure<DgxRenderPipelineGlobalSettings, RenderPipeline>(ref currentInstance, defaultPath, true))
            {
            }
            #endif
            
            if (BlitShader && BlitColorAndDepthShader)
            {
                Blitter.Cleanup();
                Blitter.Initialize(BlitShader, BlitColorAndDepthShader);    
            }
            return result;
        }
    }
}