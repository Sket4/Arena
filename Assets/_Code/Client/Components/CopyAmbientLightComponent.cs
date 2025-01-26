using System;
using TzarGames.GameCore;
using TzarGames.Rendering;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client.Presentation
{
    [Serializable]
    public struct CopyAmbientLight : IComponentData
    {
        public Entity Source;
    }
    [UseDefaultInspector]
    public class CopyAmbientLightComponent : ComponentDataBehaviour<CopyAmbientLight>
    {
        public Renderer Source;

        protected override void Bake<K>(ref CopyAmbientLight serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            serializedData.Source = baker.GetEntity(Source);
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial class CopyAmbientLightBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            Entities
                .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                .WithAll<CopyAmbientLight>()
                .ForEach((Entity entity) =>
            {
                ecb.RemoveComponent<LightProbeInterpolation>(entity);
            }).Run();
            
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
