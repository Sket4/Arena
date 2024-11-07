using TzarGames.GameCore;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Arena
{
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial class EnableByPlayerDistanceSystem : GameSystemBase
    {
        private EntityQuery playerCharactersQuery;
        private EntityQuery chunkQuery;
        private UpdateJob updateJob;

        private int FrameCounter;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            
            playerCharactersQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerController>(), 
                ComponentType.ReadOnly<LocalTransform>());

            chunkQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new []
                {
                    ComponentType.ReadOnly<DisableByPlayerDistance>(),
                    ComponentType.ReadOnly<LocalToWorld>(),
                },
                Options = EntityQueryOptions.IncludeDisabledEntities
            });
            
            updateJob = new UpdateJob
            {
                EntityType = GetEntityTypeHandle(),
                DisableDistanceType = GetSharedComponentTypeHandle<DisableByPlayerDistance>(),
                DisabledType = GetComponentTypeHandle<Disabled>(true),
                L2WType = GetComponentTypeHandle<LocalToWorld>(true)
            };
        }

        protected override void OnSystemUpdate()
        {
            FrameCounter++;

            // костыль для того, чтобы успел примениться UpdateLinkedTransformsSystem
            if (FrameCounter < 3)
            {
                return;
            }
            
            var playerTransforms =
                playerCharactersQuery.ToComponentDataListAsync<LocalTransform>(
                    Allocator.TempJob, 
                    Dependency, 
                    out var dataCollectDeps);
            
            Dependency = dataCollectDeps;
            var commands = CreateCommandBuffer().AsParallelWriter();

            updateJob.DisabledType.Update(this);
            updateJob.EntityType.Update(this);
            updateJob.DisableDistanceType.Update(this);
            updateJob.L2WType.Update(this);

            updateJob.PlayerTransforms = playerTransforms.AsDeferredJobArray();
            updateJob.Commands = commands;
            
            Dependency = updateJob.ScheduleParallel(chunkQuery, Dependency);
            
            playerTransforms.Dispose(Dependency);
        }
    }
    [BurstCompile]
    struct UpdateJob : IJobChunk
    {
        [ReadOnly] public NativeArray<LocalTransform> PlayerTransforms;
        [ReadOnly] public SharedComponentTypeHandle<DisableByPlayerDistance> DisableDistanceType;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> L2WType;
        [ReadOnly] public EntityTypeHandle EntityType;
        [ReadOnly] public ComponentTypeHandle<Disabled> DisabledType;
        
        public EntityCommandBuffer.ParallelWriter Commands;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var transforms = chunk.GetNativeArray(ref L2WType);
            var distance = chunk.GetSharedComponent(DisableDistanceType);
            var entities = chunk.GetNativeArray(EntityType);
            var hasDisabled = chunk.Has(ref DisabledType);

            for (int c = 0; c < chunk.Count; c++)
            {
                var transform = transforms[c];
                
                bool isAnyPlayerNear = false;
                var myPosition = transform.Position;
                
                foreach (var localTransform in PlayerTransforms)
                {
                    var sqDistance = math.distancesq(localTransform.Position, myPosition);
                    if (sqDistance <= distance.Distance)
                    {
                        isAnyPlayerNear = true;
                        break;
                    }
                }

                if (isAnyPlayerNear)
                {
                    if (hasDisabled)
                    {
                        Commands.RemoveComponent<Disabled>(unfilteredChunkIndex, entities[c]);    
                    }
                }
                else
                {
                    if (hasDisabled == false)
                    {
                        Commands.AddComponent<Disabled>(unfilteredChunkIndex, entities[c]);
                    }
                }
            }
        }
    }
}
