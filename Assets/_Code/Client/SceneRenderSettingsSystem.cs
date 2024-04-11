using Unity.Entities;
using Unity.Entities.Content;
using UnityEngine;

namespace Arena.Client
{
    [DisableAutoCreation]
    [RequireMatchingQueriesForUpdate]
    public partial class SceneRenderSettingsSystem : SystemBase
    {
        struct SkyboxMaterialLoading : IComponentData
        {
            public WeakObjectReference<Material> Material;
        }
        
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithChangeFilter<SceneRenderSettings>()
                .ForEach((Entity entity, in SceneRenderSettings settings) =>
            {
                Debug.Log($"Applying scene render settings from entity {entity}");
                RenderSettings.fog = settings.FogEnabled;
                RenderSettings.fogColor = settings.FogColor;
                RenderSettings.fogMode = settings.FogMode;
                RenderSettings.fogDensity = settings.FogDensity;
                RenderSettings.fogStartDistance = settings.FogStartDistance;
                RenderSettings.fogEndDistance = settings.FogEndDistance;

                if (settings.SkyboxMaterial.LoadingStatus == ObjectLoadingStatus.None)
                {
                    settings.SkyboxMaterial.LoadAsync();    
                }
                settings.SkyboxMaterial.WaitForCompletion();

                var loadEntity = EntityManager.CreateEntity(typeof(SkyboxMaterialLoading));
                EntityManager.SetComponentData(loadEntity, new SkyboxMaterialLoading { Material = settings.SkyboxMaterial });
            }).Run();

            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, in SkyboxMaterialLoading loading) =>
                {
                    if (loading.Material.LoadingStatus == ObjectLoadingStatus.Loading 
                        || loading.Material.LoadingStatus == ObjectLoadingStatus.Queued)
                    {
                        return;
                    }
                    RenderSettings.skybox = loading.Material.Result;
                    EntityManager.DestroyEntity(entity);
                    
                }).Run();
        }
    }
}
