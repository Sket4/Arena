using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TzarGames.Renderer.Baking
{
#if UNITY_EDITOR
    [TemporaryBakingType]
    class LodBakingData : IComponentData
    {
        public LODGroup Authoring;
        public Dictionary<Object, Entity> RendererToEntity;
    }

    class LODGroupBaker : Baker<LODGroup>
    {
        public override void Bake(LODGroup authoring)
        {
            if (authoring.lodCount > 8)
            {
                Debug.LogWarning("LODGroup has more than 8 LOD - Not supported", authoring);
                return;
            }

            var entity = GetEntity(TransformUsageFlags.Renderable);

            var bakingData = new LodBakingData
            {
                Authoring = authoring,
                RendererToEntity = new Dictionary<Object, Entity>()
            };

            AddComponentObject(entity, bakingData);

            var lodGroupLODs = authoring.GetLODs();

            for (int i = 0; i < authoring.lodCount; ++i)
            {
                var lod = lodGroupLODs[i];

                foreach(var r in lod.renderers)
                {
                    if (bakingData.RendererToEntity.ContainsKey(r))
                    {
                        Debug.LogWarning($"{bakingData.Authoring.name} already contains LOD renderer {r.name}");
                        continue;
                    }
                    var re = GetEntity(r, TransformUsageFlags.Renderable);
                    bakingData.RendererToEntity.Add(r, re);
                }
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial class LodGroupBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            Entities
                .WithoutBurst()
                .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                .ForEach((Entity entity, LodBakingData bakingData) =>
                {
                    var authoring = bakingData.Authoring;

                    //@TODO: LOD calculation should respect scale...
                    var worldSpaceSize = GetWorldSpaceScale(authoring.transform) * authoring.size;
                    
                    var lods = bakingData.Authoring.GetLODs();
                    float prevDistance = 0;

                    for (int i = 0; i < lods.Length; ++i)
                    {
                        var lod = lods[i];
                        float d = worldSpaceSize / lod.screenRelativeTransitionHeight;

                        foreach (var r in lod.renderers)
                        {
                            var re = bakingData.RendererToEntity[r];
                            var lodRange = new LODRange
                                { SquareDistanceMax = d * d, SquareDistanceMin = prevDistance * prevDistance };
                            ecb.AddComponent(re, lodRange);
                            //Debug.Log($"{r.name} LOD range: {lodRange.SquareDistanceMin} {lodRange.SquareDistanceMax}");
                        }

                        prevDistance = d;
                    }

                })
                .Run();

            ecb.Playback(EntityManager);
            
            ecb.Dispose();
            ecb = new EntityCommandBuffer(Allocator.Temp);

            Entities
                .WithoutBurst()
                .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                .ForEach((Entity entity, in LODRange lodRange, in RendererBakingData bakingData) =>
            {
                foreach (var subMeshData in bakingData.SubMeshBakingDatas)
                {
                    if (subMeshData.Entity == entity)
                    {
                        continue;
                    }
                    ecb.AddComponent(subMeshData.Entity, lodRange);
                }
                
            }).Run();
            
            ecb.Playback(EntityManager);
        }

        private static float GetWorldSpaceScale(Transform t)
        {
            Vector3 lossyScale = t.lossyScale;
            float a = Mathf.Abs(lossyScale.x);
            a = Mathf.Max(a, Mathf.Abs(lossyScale.y));
            return Mathf.Max(a, Mathf.Abs(lossyScale.z));
        }
    }
    #endif   
}