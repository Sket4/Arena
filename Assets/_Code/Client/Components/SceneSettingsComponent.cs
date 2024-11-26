using System;
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

    [Serializable]
    public struct SceneShaderSettings : IComponentData
    {
        public bool EnableMainLight;
        public bool EnableAdditionalLight;
        public bool EnableDarkMode;
    }

    public struct SceneSettingsTag : IComponentData
    {
    }

    [Serializable]
    public struct SceneCameraSettings : IComponentData
    {
        public bool ChangeClearFlags;
        public CameraClearFlags ClearFlags;
        public bool ChangeClearColor;
        public Color ClearColor;
    }

    [UseDefaultInspector(true)]
    public class SceneSettingsComponent : ComponentDataBehaviour<SceneSettingsTag>
    {
        public SceneShaderSettings ShaderSettings = new SceneShaderSettings
        {
            EnableAdditionalLight = false,
            EnableMainLight = false
        };

        public bool UseSceneRenderSettings = true;
        public bool UseCameraSettings = false;

        public SceneCameraSettings CameraSettings;
        
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

            if (UseCameraSettings)
            {
                baker.AddComponent(CameraSettings);
            }
            
            baker.AddComponent(ShaderSettings);
        }
        #endif
    }
}
