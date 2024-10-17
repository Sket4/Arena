using TzarGames.GameCore;
using Unity.Entities;
using Unity.Entities.Content;
using UnityEngine;

namespace Arena.Client
{
    [System.Serializable]
    public struct SceneRenderSettings : IComponentData
    {
        [HideInInspector] public WeakObjectReference<Material> SkyboxMaterial;
        [HideInInspector] public float FogStartDistance;
        [HideInInspector] public float FogEndDistance;
        [HideInInspector] public bool FogEnabled;
        [HideInInspector] public float FogDensity;
        [HideInInspector] public FogMode FogMode;
        [HideInInspector] public Color FogColor;
        [HideInInspector] public Color RealtimeShadowColor;
    }

    [System.Serializable]
    public struct SceneShaderSettings : IComponentData
    {
        public bool EnableMainLight;
        public bool EnableAdditionalLight;
    }

    [UseDefaultInspector(true)]
    public class SceneRenderSettingsComponent : ComponentDataBehaviour<SceneRenderSettings>
    {
        public SceneShaderSettings ShaderSettings = new SceneShaderSettings
        {
            EnableAdditionalLight = false,
            EnableMainLight = false
        };

        public bool UseSceneRenderSettings = true;
        
        #if UNITY_EDITOR
        protected override void Bake<K>(ref SceneRenderSettings serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            if (UseSceneRenderSettings)
            {
                serializedData.FogStartDistance = RenderSettings.fogStartDistance;
                serializedData.FogEndDistance = RenderSettings.fogEndDistance;
                serializedData.FogEnabled = RenderSettings.fog;
                serializedData.FogDensity = RenderSettings.fogDensity;
                serializedData.FogMode = RenderSettings.fogMode;
                serializedData.FogColor = RenderSettings.fogColor;
                serializedData.RealtimeShadowColor = RenderSettings.subtractiveShadowColor;
            
                serializedData.SkyboxMaterial = new WeakObjectReference<Material>(RenderSettings.skybox);    
            }
            
            baker.AddComponent(ShaderSettings);
        }
        #endif
    }
}
