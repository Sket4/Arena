using TzarGames.GameCore;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Arena
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct SpawnZoneSystem : ISystem
    {
        private SpawnZoneUpdateJob updateJob;
        private EntityQuery playerCharacterQuery;
        private double lastUpdateTime;
        private const double updateInterval = 0.5f;
        
        void OnCreate(ref SystemState state)
        {
            lastUpdateTime -= updateInterval;
            
            playerCharacterQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new []
                {
                    ComponentType.ReadOnly<PlayerController>(),
                    ComponentType.ReadOnly<LocalToWorld>(),
                }
            });
            
            updateJob = new SpawnZoneUpdateJob
            {
                L2WType = state.GetComponentTypeHandle<LocalToWorld>(true),
                LivingStateLookup = state.GetComponentLookup<LivingState>(true),
                MoveAroundPointLookup = state.GetComponentLookup<MoveAroundPoint>(true)
            };
        }
        
        //void OnDestroy(ref SystemState state) {}

        void OnUpdate(ref SystemState state)
        {
            var currentTime = SystemAPI.Time.ElapsedTime;

            if (currentTime - lastUpdateTime < updateInterval)
            {
                return;
            }
            lastUpdateTime = currentTime;
            
            JobHandle deps;
            var playerChunks =
                playerCharacterQuery.ToArchetypeChunkListAsync(Allocator.TempJob, state.Dependency, out deps);
            state.Dependency = deps;

            updateJob.PlayerChunks = playerChunks.AsDeferredJobArray();
            updateJob.L2WType.Update(ref state);
            updateJob.LivingStateLookup.Update(ref state);
            updateJob.MoveAroundPointLookup.Update(ref state);
            updateJob.CurrentTime = currentTime;
            updateJob.CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
                
            var cmdBufferSystem = SystemAPI.GetSingleton<GameCommandBufferSystem.Singleton>();
            updateJob.Commands = cmdBufferSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            state.Dependency = updateJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    partial struct SpawnZoneUpdateJob : IJobEntity
    {
        [ReadOnly] public NativeArray<ArchetypeChunk> PlayerChunks;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> L2WType;
        [ReadOnly] public ComponentLookup<LivingState> LivingStateLookup;
        [ReadOnly] public ComponentLookup<MoveAroundPoint> MoveAroundPointLookup;
        [ReadOnly] public CollisionWorld CollisionWorld;
        public EntityCommandBuffer.ParallelWriter Commands;
        public double CurrentTime;
        
        public void Execute(Entity spawnZoneEntity, [ChunkIndexInQuery] int jobIndex, in SpawnZoneParameters spawnZoneParameters, in Radius radius, 
            in Height height, in LocalToWorld spawnZoneL2W, 
            ref DynamicBuffer<SpawnZoneInstance> spawnedInstances,
            in SpawnZoneStateData spawnZoneState)
        {
            CleanupSpawnedInstances(ref spawnedInstances);

            var spawnZonePosition = spawnZoneL2W.Position;
            var spawnZoneRadiusSq = radius.Value * radius.Value;
            bool isAnyPlayerInSpawnZone = false;

            foreach (var playerChunk in PlayerChunks)
            {
                var l2wArray = playerChunk.GetNativeArray(ref L2WType);

                foreach (var l2w in l2wArray)
                {
                    var distSq = math.distancesq(l2w.Position, spawnZonePosition);

                    if (distSq < spawnZoneRadiusSq)
                    {
                        isAnyPlayerInSpawnZone = true;
                        break;
                    }

                    if (isAnyPlayerInSpawnZone)
                    {
                        break;
                    }
                }
            }

            SpawnZoneStateData newState = default; 

            if (isAnyPlayerInSpawnZone == false)
            {
                for (var index = 0; index < spawnedInstances.Length; index++)
                {
                    var spawnZoneInstance = spawnedInstances[index];

                    if (spawnZoneInstance.Value == Entity.Null)
                    {
                        continue;
                    }
                    
                    Commands.DestroyEntity(jobIndex, spawnZoneInstance.Value);
                    
                    spawnZoneInstance.Value = Entity.Null;
                    spawnedInstances[index] = spawnZoneInstance;
                }

                newState = spawnZoneState;
                newState.LastSpawnTime = 0;
                Commands.SetComponent(jobIndex, spawnZoneEntity, newState);
                
                return;
            }

            int freeSlots = spawnZoneParameters.MaximumSpawnCount - spawnedInstances.Length;

            if (freeSlots <= 0)
            {
                freeSlots = 0;
                
                foreach (var spawnZoneInstance in spawnedInstances)
                {
                    if (spawnZoneInstance.Value != Entity.Null)
                    {
                        continue;
                    }

                    if (CurrentTime - spawnZoneInstance.DestroyTime < spawnZoneParameters.SpawnAfterDeathInverval)
                    {
                        continue;    
                    }
                
                    freeSlots++;
                }    
            }
            
            if (freeSlots == 0)
            {
                return;
            }

            var spawnTimeDiff = CurrentTime - spawnZoneState.LastSpawnTime;

            if (spawnTimeDiff < spawnZoneParameters.SpawnInterval)
            {
                return;
            }

            var random = Random.CreateFromIndex((uint)(spawnZoneEntity.Index + CurrentTime * 10));
            var traceRay = -math.up();
            var traceRadius = 1.0f;
            var collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = spawnZoneParameters.SpawnPointTraceLayers,
                GroupIndex = 0
            };

            int attemptCounter = 0;
            bool isSpawnPointFound = false;
            float3 spawnPosition = default;
            
            while (attemptCounter < 32)
            {;
                var randomDisp = random.NextFloat3Direction();
                randomDisp.y = 0;
                randomDisp = math.normalizesafe(randomDisp, math.forward());
                randomDisp *= random.NextFloat(0, spawnZoneParameters.SpawnRadius);

                var traceStartPos = spawnZonePosition + randomDisp;
                if (CollisionWorld.SphereCast(traceStartPos, traceRadius, traceRay, height.Value, out var hit, collisionFilter,
                        QueryInteraction.IgnoreTriggers))
                {
                    //Debug.DrawRay(traceStartPos, traceRay * height.Value, Color.red, 5);
                    isSpawnPointFound = true;
                    spawnPosition = hit.Position;
                    break;
                }

                attemptCounter++;
            }

            var prefab = spawnZoneParameters.Prefab;

            if (isSpawnPointFound)
            {
                var instance = Commands.Instantiate(jobIndex, prefab);
                Commands.SetComponent(jobIndex, instance, LocalTransform.FromPosition(spawnPosition));

                if (MoveAroundPointLookup.TryGetComponent(prefab, out var moveAroundPoint))
                {
                    moveAroundPoint.TargetPosition = spawnZonePosition + traceRay * height.Value;
                    moveAroundPoint.AreaRadius = spawnZoneParameters.SpawnRadius;
                    moveAroundPoint.TraceVerticalOffset = height.Value;

                    Commands.SetComponent(jobIndex, instance, moveAroundPoint);    
                }

                int slotIndex = -1;

                for (var index = 0; index < spawnedInstances.Length; index++)
                {
                    var spawnZoneInstance = spawnedInstances[index];
                    
                    if (spawnZoneInstance.Value != Entity.Null)
                    {
                        continue;
                    }

                    if (CurrentTime - spawnZoneInstance.DestroyTime < spawnZoneParameters.SpawnAfterDeathInverval)
                    {
                        continue;
                    }
                    slotIndex = index;
                    break;
                }

                if (slotIndex >= 0)
                {
                    var buffer = Commands.SetBuffer<SpawnZoneInstance>(jobIndex, spawnZoneEntity);
                    buffer.AddRange(spawnedInstances.AsNativeArray());
                    
                    buffer[slotIndex] = new SpawnZoneInstance
                    {
                        Value = instance
                    };
                }
                else
                {
                    Commands.AppendToBuffer(jobIndex, spawnZoneEntity, new SpawnZoneInstance
                    {
                        Value = instance
                    }); 
                }
            }
            
            newState = spawnZoneState;
            newState.LastSpawnTime = CurrentTime;
            Commands.SetComponent(jobIndex, spawnZoneEntity, newState);
        }

        void CleanupSpawnedInstances(ref DynamicBuffer<SpawnZoneInstance> spawnZoneInstances)
        {
            for (var index = spawnZoneInstances.Length - 1; index >= 0; index--)
            {
                var spawnZoneInstance = spawnZoneInstances[index];

                if (spawnZoneInstance.Value == Entity.Null)
                {
                    continue;
                }

                if (LivingStateLookup.TryGetComponent(spawnZoneInstance.Value, out var livingState))
                {
                    if (livingState.IsDead)
                    {
                        spawnZoneInstance.Value = Entity.Null;
                        spawnZoneInstance.DestroyTime = CurrentTime;
                        spawnZoneInstances[index] = spawnZoneInstance;
                    }
                }
                else
                {
                    spawnZoneInstance.Value = Entity.Null;
                    spawnZoneInstance.DestroyTime = CurrentTime;
                    spawnZoneInstances[index] = spawnZoneInstance;
                }
            }
        }
    }
}
