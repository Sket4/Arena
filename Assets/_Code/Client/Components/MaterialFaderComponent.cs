using System;
using System.Collections.Generic;
using TzarGames.GameCore;
using TzarGames.Rendering;
using Unity.Entities;
using Unity.Entities.Content;
using UnityEngine;

namespace Arena.Client
{
    [Serializable]
    public struct MaterialDisappearData : IComponentData, IEnableableComponent
    {
        public float FadeDelay;
        public float FadeTime;
    }

    [Serializable]
    public struct MaterialAppearData : IComponentData, IEnableableComponent
    {
        public float FadeTime;
    }
    
    public struct MaterialFaderData : IComponentData, IEnableableComponent
    {
        public float CurrentFadeTime;
    }

    [Serializable]
    public struct MaterialFadeReplacement : IComponentData, IEquatable<MaterialFadeReplacement>, IEnableableComponent
    {
        public bool UseOriginal;
        public WeakObjectReference<Material> Original;
        public WeakObjectReference<Material> Replacement;

        public bool Equals(MaterialFadeReplacement other)
        {
            return Original.Equals(other.Original) && Replacement.Equals(other.Replacement);
        }

        public override bool Equals(object obj)
        {
            return obj is MaterialFadeReplacement other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Original, Replacement);
        }
    }

    [Serializable]
    public struct FadingRenderer : IBufferElementData
    {
        public Entity RendererEntity;
    }

    

    [UseDefaultInspector(true)]
    public class MaterialFaderComponent : ComponentDataBehaviour<MaterialFaderData>
    {
        public MaterialAppearData AppearSettings;
        public MaterialDisappearData DisappearSettings;
        
        [Serializable]
        class MaterialFadingMappingManaged
        {
            public Material Original;
            public Material Replacement;
        }
        
        [SerializeField]
        private MaterialFadingMappingManaged[] fadingMaterials;

        protected override void Reset()
        {
            base.Reset();

            AppearSettings = new MaterialAppearData
            {
                FadeTime = 1
            };
            DisappearSettings = new MaterialDisappearData
            {
                FadeTime = 2,
                FadeDelay = 2
            };

            var materials = new List<Material>();
            
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var childRenderer in renderers)
            {
                foreach (var material in childRenderer.sharedMaterials)
                {
                    if (materials.Contains(material) == false)
                    {
                        materials.Add(material);
                    }
                }
            }

            fadingMaterials = new MaterialFadingMappingManaged[materials.Count];

            for (var index = 0; index < materials.Count; index++)
            {
                var material = materials[index];
                fadingMaterials[index] = new MaterialFadingMappingManaged
                {
                    Original = material,
                    Replacement = null
                };
            }
        }

#if UNITY_EDITOR
        protected override void Bake<K>(ref MaterialFaderData serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            baker.AddComponent(AppearSettings);
            baker.AddComponent(DisappearSettings);
            
            baker.AddBuffer<FadingRenderer>();

            var mapping = baker.AddBuffer<TempBakingData>();

            foreach (var fadingMaterial in fadingMaterials)
            {
                var data = new MaterialFadeReplacement
                {
                    Original = new WeakObjectReference<Material>(fadingMaterial.Original),
                    Replacement = new WeakObjectReference<Material>(fadingMaterial.Replacement)
                };
                mapping.Add(new TempBakingData { replacement = data });
            }
        }
        #endif
    }
    #if UNITY_EDITOR
    
    [TemporaryBakingType]
    struct TempBakingData : IBufferElementData
    {
        public MaterialFadeReplacement replacement;
    }
    
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    partial class MaterialFaderBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // foreach (var originalMaterial in MaterialFaderComponent.FadingMaterials)
            // {
            //     var fadingMat = new Material(originalMaterial);
            //     fadingMat.name += $"Fading {fadingMat.name}";
            //     fadingMat.EnableKeyword(FaderMaterialProperty.FadeShaderKeyword);
            //     var fadingMatInfo = new MaterialFadeMapping
            //     {
            //         //Replacement = new WeakObjectReference<Material>(fadingMat),
            //         Original = new WeakObjectReference<Material>(originalMaterial) 
            //     };
            //     mappedMats.Add(fadingMatInfo);
            //     fadingEnabledMats.Add(new TempHackFadingMaterials.MaterialInfo
            //     {
            //         Original = fadingMatInfo.Original,
            //         FadingVersion = fadingMat
            //     });
            // }
            
            using(var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp))
            {
                Entities
                    .WithoutBurst()
                    .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                    .WithAll<MaterialFaderData>()
                    .ForEach((
                        Entity entity,
                        DynamicBuffer<FadingRenderer> fadingRenderers,
                        DynamicBuffer<LinkedEntityGroup> linkedEntities,
                        DynamicBuffer<TempBakingData> tempBakingData) =>
                {
                    ecb.SetComponentEnabled<MaterialAppearData>(entity, true);
                    ecb.SetComponentEnabled<MaterialDisappearData>(entity, false);
                    ecb.SetComponentEnabled<MaterialFaderData>(entity, true);
                    
                    foreach (var linked in linkedEntities)
                    {
                        if (EntityManager.HasComponent<RenderInfo>(linked.Value) == false)
                        {
                            continue;
                        }
                        var renderInfo = EntityManager.GetSharedComponentManaged<RenderInfo>(linked.Value);
                        bool hasReplacement = false;
                        MaterialFadeReplacement replacement = default;
                        
                        foreach (var data in tempBakingData)
                        {
                            if (data.replacement.Original.Equals(renderInfo.Material.UnityReference))
                            {
                                hasReplacement = true;
                                replacement = data.replacement;
                                break;
                            }
                        }

                        if (hasReplacement == false)
                        {
                            continue;
                        }

                        renderInfo.Material = new CustomWeakObjectReference<Material>(replacement.Replacement);
                        renderInfo.ResetHash128();
                        ecb.SetSharedComponentManaged(linked.Value, renderInfo);
                        
                        ecb.AddComponent(linked.Value, replacement);
                        ecb.SetComponentEnabled<MaterialFadeReplacement>(linked.Value, false);
                        fadingRenderers.Add(new FadingRenderer { RendererEntity = linked.Value });
                    }

                }).Run();

                ecb.Playback(EntityManager);
            }
        }
    }
    #endif
}
