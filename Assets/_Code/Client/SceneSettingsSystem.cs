using System.Collections.Generic;
using TzarGames.Rendering;
using Unity.Entities;
using Unity.Entities.Content;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arena.Client
{
    [DisableAutoCreation]
    [RequireMatchingQueriesForUpdate]
    public partial class SceneSettingsSystem : SystemBase
    {
        struct SkyboxMaterialLoading : IComponentData
        {
            public WeakObjectReference<Material> Material;
        }

        private const string EnableMainLightKeywordName = "ARENA_USE_MAIN_LIGHT";
        private const string EnableAdditionalLightKeywordName = "ARENA_USE_ADD_LIGHT";
        
        private readonly static GlobalKeyword EnableMainLightKeyword = GlobalKeyword.Create(EnableMainLightKeywordName);
        private readonly static GlobalKeyword EnableAdditionalLightKeyword = GlobalKeyword.Create(EnableAdditionalLightKeywordName);

        private EntityQuery shaderSettingsQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            // materialsQuery = GetEntityQuery(new EntityQueryDesc
            // {
            //     All = new [] { ComponentType.ReadOnly<RenderInfo>() },
            //     Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities
            // });
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DGX.SRP.RenderPipeline.EnableDarkMode(false);
        }

        protected override void OnUpdate()
        {
            if (shaderSettingsQuery.IsEmpty == false)
            {
                var renderInfos = new List<RenderInfo>();
                var indices = new List<int>();
                EntityManager.GetAllUniqueSharedComponentsManaged(renderInfos, indices);
                
                Entities
                    .WithoutBurst()
                    .WithStoreEntityQueryInField(ref shaderSettingsQuery)
                    .WithChangeFilter<SceneShaderSettings>()
                    .ForEach((in SceneShaderSettings settings) =>
                    {
                        Debug.Log($"Set scene shader settings: enable main light: {settings.EnableMainLight}, enable add lights: {settings.EnableAdditionalLight}, enable darkmode: {settings.EnableDarkMode}");
                        Shader.SetKeyword(EnableMainLightKeyword, settings.EnableMainLight);
                        Shader.SetKeyword(EnableAdditionalLightKeyword, settings.EnableAdditionalLight);
                        DGX.SRP.RenderPipeline.EnableDarkMode(settings.EnableDarkMode);
                        
                        
                        // var materials = new List<Material>();
                        //
                        // foreach (var renderInfo in renderInfos)
                        // {
                        //     Material material;
                        //     
                        //     if (renderInfo.Material.LoadingMode ==
                        //         CustomWeakObjectReference<Material>.LoadingModes.Default)
                        //     {
                        //         if (renderInfo.Material.LoadingStatus != ObjectLoadingStatus.Completed)
                        //         {
                        //             if (renderInfo.Material.LoadingStatus == ObjectLoadingStatus.None)
                        //             {
                        //                 renderInfo.Material.LoadAsync();    
                        //             }
                        //             renderInfo.Material.UnityReference.WaitForCompletion();    
                        //         }
                        //         
                        //         material = renderInfo.Material.Result;
                        //     }
                        //     else
                        //     {
                        //         material = renderInfo.Material.Result;
                        //     }
                        //
                        //     if (material != null && materials.Contains(material) == false)
                        //     {
                        //         Debug.Log($"{material.name} =============================");
                        //         foreach (var kwd in material.enabledKeywords)
                        //         {
                        //             Debug.Log($"{kwd.name}");   
                        //         }
                        //         Debug.Log("============================");
                        //         
                        //         //Debug.Log($"set keywords for {material.name}");
                        //         // foreach (var keyword in material.shader.keywordSpace.keywords)
                        //         // {
                        //         //     if (keyword.name.Equals(EnableAdditionalLightKeywordName))
                        //         //     {
                        //         //         Debug.Log($"set main light keywords for {material.name} to {settings.EnableMainLight}");
                        //         //         material.SetKeyword(keyword, settings.EnableMainLight);
                        //         //     }
                        //         //     else if(keyword.name.Equals(EnableAdditionalLightKeywordName))
                        //         //     {
                        //         //         Debug.Log($"set add light keywords for {material.name} to {settings.EnableAdditionalLight}");
                        //         //         material.SetKeyword(keyword, settings.EnableAdditionalLight);
                        //         //     }
                        //         // }
                        //     }
                        // }
                    
                    }).Run();    
            }
            
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
                RenderSettings.subtractiveShadowColor = settings.RealtimeShadowColor;

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
