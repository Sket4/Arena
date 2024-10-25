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
    public struct DisableByPlayerDistance : IComponentData
    {
        public float Distance;
    }

    [TemporaryBakingType]
    public struct DisableByPlayerDistanceChilds : IBufferElementData
    {
        public Entity Entity;
    }
    
    [UseDefaultInspector]
    public class DisableByPlayerDistanceComponent : ComponentDataBehaviour<DisableByPlayerDistance>
    {
        public float DisableDistance = 50;
        public bool AddToChilds = false;
        
        protected override void Bake<K>(ref DisableByPlayerDistance serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            serializedData.Distance = DisableDistance * DisableDistance;

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
        [ReadOnly] public ComponentLookup<DisableByPlayerDistance> DisableLookup;
        
        public void Execute(in DisableByPlayerDistance distance, in DynamicBuffer<DisableByPlayerDistanceChilds> childs)
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
                DisableLookup = state.GetComponentLookup<DisableByPlayerDistance>(true)
            };
            mainQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<DisableByPlayerDistance>(),
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
