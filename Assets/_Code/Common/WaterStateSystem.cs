using TzarGames.GameCore;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Arena
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoCreation]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(GameCommandBufferSystem))]
    public partial struct WaterStateSystem : ISystem
    {
        private StateUpdateJob updateJob;
        
        void OnCreate(ref SystemState state)
        {
            updateJob = new StateUpdateJob
            {
                WaterStateLookup = state.GetComponentLookup<WaterState>(true),
                LocalToWorldLookup = state.GetComponentLookup<LocalToWorld>(true)
            };
        }

        void OnUpdate(ref SystemState state)
        {
            updateJob.WaterStateLookup.Update(ref state);
            updateJob.LocalToWorldLookup.Update(ref state);
            
            var singleton = SystemAPI.GetSingleton<GameCommandBufferSystem.Singleton>();
            var ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);
            updateJob.Commands = ecb.AsParallelWriter();

            state.Dependency = updateJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(Water))]
    partial struct StateUpdateJob : IJobEntity, IEntityTriggerStateEventHandler
    {
        [ReadOnly]
        public ComponentLookup<WaterState> WaterStateLookup;
        
        [ReadOnly]
        public ComponentLookup<LocalToWorld> LocalToWorldLookup;
        
        public EntityCommandBuffer.ParallelWriter Commands;

        private float waterWorld_Y;
        private int currentJobIndex;
        
        public void Execute(Entity entity, [ChunkIndexInQuery] int jobIndex, ref DynamicBuffer<OverlappingEntities> overlappers, in LocalToWorld l2w)
        {
            waterWorld_Y = l2w.Position.y;
            currentJobIndex = jobIndex;
            
            StatefulTriggerEventUtility.CheckState(this, entity, overlappers);
        }

        public void OnEnter(Entity colliderEntity, Entity enteredEntity)
        {
            if (WaterStateLookup.TryGetComponent(enteredEntity, out var enteredState) == false)
            {
                return;
            }
            Commands.SetComponentEnabled<WaterState>(currentJobIndex, enteredEntity, true);
            
            if (LocalToWorldLookup.TryGetComponent(enteredEntity, out var enteredL2W))
            {
                enteredState.Depth = waterWorld_Y - enteredL2W.Position.y;
                Commands.SetComponent(currentJobIndex, enteredEntity, enteredState);   
            }
        }

        public void OnStay(Entity colliderEntity, Entity stayingEntity)
        {
            if (WaterStateLookup.TryGetComponent(stayingEntity, out var enteredState) == false)
            {
                return;
            }
            if (LocalToWorldLookup.TryGetComponent(stayingEntity, out var enteredL2W))
            {
                enteredState.Depth = waterWorld_Y - enteredL2W.Position.y;
                Commands.SetComponent(currentJobIndex, stayingEntity, enteredState);   
            }
        }

        public void OnExit(Entity colliderEntity, Entity exitedEntity)
        {
            if (WaterStateLookup.HasComponent(exitedEntity) == false)
            {
                return;
            }
            Commands.SetComponentEnabled<WaterState>(currentJobIndex, exitedEntity, false);
        }
    }
}
