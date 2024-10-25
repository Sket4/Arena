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

    public struct SceneSettingsTag : IComponentData
    {
    }

    [UseDefaultInspector(true)]
    public class SceneRenderSettingsComponent : ComponentDataBehaviour<SceneSettingsTag>
    {
        public SceneShaderSettings ShaderSettings = new SceneShaderSettings
        {
            EnableAdditionalLight = false,
            EnableMainLight = false
        };

        public bool UseSceneRenderSettings = true;
        
        #if UNITY_EDITOR
        protected override void Bake<K>(ref SceneSettingsTag serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            if (UseSceneRenderSettings)
            {
                var renderSettings = new SceneRenderSettings();
                renderSettings.FogStartDistance = RenderSettings.fogStartDistance;
                renderSettings.FogEndDistance = RenderSettings.fogEndDistance;
                renderSettings.FogEnabled = RenderSettings.fog;
                renderSettings.FogDensity = RenderSettings.fogDensity;
                renderSettings.FogMode = RenderSettings.fogMode;
                renderSettings.FogColor = RenderSettings.fogColor;
                renderSettings.RealtimeShadowColor = RenderSettings.subtractiveShadowColor;
            
                renderSettings.SkyboxMaterial = new WeakObjectReference<Material>(RenderSettings.skybox);

                if (RenderSettings.skybox != null && renderSettings.SkyboxMaterial.IsReferenceValid == false)
                {
                    Debug.LogError($"invalid skybox material! {RenderSettings.skybox.name}");
                }
                baker.AddComponent(renderSettings);
            }
            
            baker.AddComponent(ShaderSettings);
        }
        #endif
    }
}
