using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using TzarGames.GameCore;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace Arena
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    [DisableAutoCreation]
    [BurstCompile]
    public partial struct UpdateLinkedTransformsSystem : ISystem
    {
        [BurstCompile]
        partial struct UpdateJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<Parent> ParentLookup;
            [ReadOnly] public ComponentLookup<DestroyTimer> DestroyTimerLookup;
            [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
            [ReadOnly] public ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;
            
            public EntityCommandBuffer.ParallelWriter Commands;
            
            public void Execute(Entity entity, [ChunkIndexInQuery] int sortIndex, in DynamicBuffer<LinkedEntityGroup> linkedEntities, in LocalTransform rootTransform)
            {
                foreach(var childEntity in linkedEntities)
                {
                    if(childEntity.Value == entity)
                    {
                        continue;
                    }

                    if(DestroyTimerLookup.HasComponent(childEntity.Value))
                    {
                        continue;
                    }
                    if(ParentLookup.HasComponent(childEntity.Value))
                    {
                        continue;
                    }
                    if(LocalToWorldLookup.HasComponent(childEntity.Value) == false)
                    {
                        continue;
                    }
                    
                    var childL2W = LocalToWorldLookup[childEntity.Value];
                    var finalTransformMatrix = math.mul(rootTransform.ToMatrix(), childL2W.Value);

                    if(PostTransformMatrixLookup.HasComponent(childEntity.Value))
                    {
                        var pos = finalTransformMatrix.Translation();
                        var rot = finalTransformMatrix.Rotation();
                        Commands.SetComponent(sortIndex, childEntity.Value, LocalTransform.FromPositionRotation(pos, rot));
                    }
                    else
                    {
                        Commands.SetComponent(sortIndex, childEntity.Value, LocalTransform.FromMatrix(finalTransformMatrix));
                    }
                }
            }

            public void UpdateState(ref SystemState system)
            {
                ParentLookup.Update(ref system);
                PostTransformMatrixLookup.Update(ref system);
                DestroyTimerLookup.Update(ref system);
                LocalToWorldLookup.Update(ref system);
            }
        }

        private UpdateJob updateJob;
        private EntityQuery query;

        public void OnCreate(ref SystemState state)
        {
            updateJob = new UpdateJob
            {
                DestroyTimerLookup = state.GetComponentLookup<DestroyTimer>(true),
                LocalToWorldLookup = state.GetComponentLookup<LocalToWorld>(true),
                PostTransformMatrixLookup = state.GetComponentLookup<PostTransformMatrix>(true),
                ParentLookup = state.GetComponentLookup<Parent>(true)
            };
            query = state.GetEntityQuery(
                ComponentType.ReadOnly<LocalTransform>(), 
                ComponentType.ReadOnly<UpdateLinkedTransforms>(),
                ComponentType.ReadOnly<LinkedEntityGroup>());
            
            query.SetChangedVersionFilter(ComponentType.ReadOnly<LocalTransform>());
            
            state.RequireForUpdate(query);
            state.RequireForUpdate<GameCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (query.IsEmpty)
            {
                return;
            }
            Debug.Log("Обновление UpdateLinkedTransforms");

            using var commands = new EntityCommandBuffer(Allocator.TempJob);
            
            updateJob.UpdateState(ref state);
            updateJob.Commands = commands.AsParallelWriter();
            
            state.Dependency = updateJob.ScheduleParallel(query, state.Dependency);
            state.Dependency.Complete();
            
            commands.Playback(state.EntityManager);
        }
    }
}
