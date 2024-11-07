using System;
using System.Collections.Generic;
using TzarGames.GameCore;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    [Serializable]
    public struct DisableByPlayerDistance : ISharedComponentData
    {
        public float Distance;
    }

    [TemporaryBakingType]
    struct DisableByPlayerDistanceBakingOnly : IComponentData
    {
        public DisableByPlayerDistance Data;
    }

    [TemporaryBakingType]
    public struct DisableByPlayerDistanceChilds : IBufferElementData
    {
        public Entity Entity;
    }
    
    [UseDefaultInspector]
    class DisableByPlayerDistanceComponent : ComponentDataBehaviour<DisableByPlayerDistanceBakingOnly>
    {
        public float DisableDistance = 50;
        public bool AddToChilds = false;
        public bool IgnoreSelf = false;

        public override bool ShouldBeConverted(IGCBaker baker)
        {
            if (AddToChilds == false && IgnoreSelf)
            {
                return false;
            }
            return base.ShouldBeConverted(baker);
        }

        protected override void Bake<K>(ref DisableByPlayerDistanceBakingOnly serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            var sharedData = new DisableByPlayerDistance
            {
                Distance = DisableDistance * DisableDistance
            };
            serializedData.Data = sharedData;

            if (IgnoreSelf == false)
            {
                baker.AddSharedComponent(sharedData);
            }

            if (AddToChilds)
            {
                var childs = GetComponentsInChildren<Transform>(true);
                var bakingData = baker.AddBuffer<DisableByPlayerDistanceChilds>();
                
                foreach (var child in childs)
                {
                    bakingData.Add(new DisableByPlayerDistanceChilds
                    {
                        Entity = baker.GetEntity(child)
                    });
                }
            }
        }
    }
    
    #if UNITY_EDITOR
    [BurstCompile]
    partial struct BakingJob : IJobEntity
    {
        public EntityCommandBuffer Commands;
        [ReadOnly] public ComponentLookup<DontDisableByPlayerDistance> DontDisableLookup;
        [ReadOnly] public ComponentLookup<DisableByPlayerDistanceBakingOnly> DisableLookup;
        
        public void Execute(in DisableByPlayerDistanceBakingOnly distance, in DynamicBuffer<DisableByPlayerDistanceChilds> childs)
        {
            foreach (var child in childs)
            {
                if (DontDisableLookup.EntityExists(child.Entity) == false)
                {
                    continue;
                }
                if (DontDisableLookup.HasComponent(child.Entity))
                {
                    continue;
                }
                if (DisableLookup.HasComponent(child.Entity))
                {
                    continue;
                }
                Commands.AddSharedComponent(child.Entity, distance.Data);
                Commands.AddComponent(child.Entity, distance);
            }
        }
    }
    
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [BurstCompile]
    partial struct BakingSystem : ISystem
    {
        private BakingJob bakingJob;
        private EntityQuery mainQuery;
        
        public void OnCreate(ref SystemState state)
        {
            bakingJob = new BakingJob
            {
                DontDisableLookup = state.GetComponentLookup<DontDisableByPlayerDistance>(true),
                DisableLookup = state.GetComponentLookup<DisableByPlayerDistanceBakingOnly>(true)
            };
            mainQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<DisableByPlayerDistanceBakingOnly>(),
                    ComponentType.ReadOnly<DisableByPlayerDistanceChilds>(),
                },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            bakingJob.Commands = ecb;
            bakingJob.DontDisableLookup.Update(ref state);
            bakingJob.DisableLookup.Update(ref state);

            state.Dependency = bakingJob.Schedule(mainQuery, state.Dependency);
            state.Dependency.Complete();
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
    #endif
}
