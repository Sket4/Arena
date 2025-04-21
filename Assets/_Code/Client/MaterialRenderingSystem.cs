using Arena.Client.Presentation;
using TzarGames.GameCore;
using TzarGames.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(RenderingSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial class MaterialRenderingSystem : SystemBase
    {
        [BurstCompile]
        [WithChangeFilter(typeof(ColorData))]
        partial struct CopyColorJob : IJobEntity
        {
            public EntityCommandBuffer Commands;
            public void Execute(in DynamicBuffer<CopyColor> copyColor, in ColorData color)
            {
                foreach (var copy in copyColor)
                {
                    Commands.SetComponent(copy.Target, color);    
                }
            }
        }
        
        protected override void OnUpdate()
        {
            using (var ecb = new EntityCommandBuffer(Allocator.TempJob))
            {
                var copyColorJob = new CopyColorJob
                {
                    Commands = ecb,
                };
                copyColorJob.Run();
                
                Entities
                .WithDisabled<MaterialDisappearData>()
                .WithChangeFilter<LivingState>()
                .ForEach((Entity entity, DynamicBuffer<FadingRenderer> fadingRenderers, in LivingState state) =>
                {
                    if (state.IsAlive)
                    {
                        return;
                    }

                    var disappearData = SystemAPI.GetComponent<MaterialDisappearData>(entity);
                    ecb.SetComponent(entity, new MaterialFaderData { CurrentFadeTime = -disappearData.FadeDelay });
                    ecb.SetComponentEnabled<MaterialFaderData>(entity, true);
                    ecb.SetComponentEnabled<MaterialDisappearData>(entity, true);
                    ecb.SetComponentEnabled<MaterialAppearData>(entity, false);
                    
                    foreach(var fadingRenderer in fadingRenderers)
                    {
                        if(SystemAPI.HasComponent<MaterialFadeReplacement>(fadingRenderer.RendererEntity) == false)
                        {
                            continue;
                        }

                        var replacement = SystemAPI.GetComponent<MaterialFadeReplacement>(fadingRenderer.RendererEntity);
                        replacement.UseOriginal = false;
                        ecb.SetComponent(fadingRenderer.RendererEntity, replacement);
                        ecb.SetComponentEnabled<MaterialFadeReplacement>(fadingRenderer.RendererEntity, true);
                        var instanceData = SystemAPI.GetComponent<RendererInstanceData>(fadingRenderer.RendererEntity);
                        instanceData.CommonValue1 = 0;
                        ecb.SetComponent(fadingRenderer.RendererEntity, instanceData);
                    }

                }).Run();

                Entities
                    .WithoutBurst()
                    .ForEach((Entity entity, RenderInfo renderInfo, in MaterialFadeReplacement replacement) =>
                {
                    renderInfo.Material =
                        new CustomWeakObjectReference<Material>(replacement.UseOriginal
                            ? replacement.Original
                            : replacement.Replacement);
                    
                    renderInfo.ResetHash128_Managed();
                    
                    ecb.SetComponentEnabled<MaterialFadeReplacement>(entity, false);
                    ecb.SetSharedComponentManaged(entity, renderInfo);

                }).Run();

                var deltaTime = SystemAPI.Time.DeltaTime;

                // appear
                Entities
                    .ForEach((Entity entity, DynamicBuffer<FadingRenderer> fadingRenderers, ref MaterialFaderData fader, in MaterialAppearData appearData) =>
                {
                    fader.CurrentFadeTime += deltaTime;

                    if(fader.CurrentFadeTime >= appearData.FadeTime)
                    {
                        fader.CurrentFadeTime = appearData.FadeTime;
                        ecb.SetComponentEnabled<MaterialFaderData>(entity, false);

                        foreach (var fadingRenderer in fadingRenderers)
                        {
                            var replacement = SystemAPI.GetComponent<MaterialFadeReplacement>(fadingRenderer.RendererEntity);
                            replacement.UseOriginal = true;
                            ecb.SetComponent(fadingRenderer.RendererEntity, replacement);
                            ecb.SetComponentEnabled<MaterialFadeReplacement>(fadingRenderer.RendererEntity, true);
                        }
                    }

                    var fadeValue = fader.CurrentFadeTime / appearData.FadeTime;
                    fadeValue = 1.0f - fadeValue;   

                    foreach(var renderer in fadingRenderers)
                    {
                        var instanceData = SystemAPI.GetComponent<RendererInstanceData>(renderer.RendererEntity);
                        instanceData.CommonValue1 = fadeValue;
                        ecb.SetComponent(renderer.RendererEntity, instanceData);
                    }

                }).Run();
                
                // disappear
                Entities
                .ForEach((Entity entity, DynamicBuffer<FadingRenderer> fadingRenderers, ref MaterialFaderData fader, in MaterialDisappearData disappearData) =>
                {
                    fader.CurrentFadeTime += deltaTime;

                    if (fader.CurrentFadeTime < 0.0f)
                    {
                        return;
                    }
                    if(fader.CurrentFadeTime >= disappearData.FadeTime)
                    {
                        fader.CurrentFadeTime = disappearData.FadeTime;
                        ecb.SetComponentEnabled<MaterialFaderData>(entity, false);
                    }

                    var fadeValue = fader.CurrentFadeTime / disappearData.FadeTime;

                    foreach(var renderer in fadingRenderers)
                    {
                        var instanceData = SystemAPI.GetComponent<RendererInstanceData>(renderer.RendererEntity);
                        instanceData.CommonValue1 = fadeValue;
                        SystemAPI.SetComponent(renderer.RendererEntity, instanceData);
                    }

                }).Run();

                if (ecb.IsCreated)
                {
                    ecb.Playback(EntityManager);
                }
            }
        }
    }
}
