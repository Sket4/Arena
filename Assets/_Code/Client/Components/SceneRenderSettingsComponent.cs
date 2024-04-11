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
    }

    public class SceneRenderSettingsComponent : ComponentDataBehaviour<SceneRenderSettings>
    {
        #if UNITY_EDITOR
        protected override void Bake<K>(ref SceneRenderSettings serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            serializedData.FogStartDistance = RenderSettings.fogStartDistance;
            serializedData.FogEndDistance = RenderSettings.fogEndDistance;
            serializedData.FogEnabled = RenderSettings.fog;
            serializedData.FogDensity = RenderSettings.fogDensity;
            serializedData.FogMode = RenderSettings.fogMode;
            serializedData.FogColor = RenderSettings.fogColor;
            
            serializedData.SkyboxMaterial = new WeakObjectReference<Material>(RenderSettings.skybox);
        }
        #endif
    }
}
