using TzarGames.GameCore;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    public struct StopSpawnAndDestroySpawned : IComponentData
    {
        public Entity Spawner;
    }

    public struct ArenaSpawnerFinishedEvent : IComponentData
    {
        public Entity Spawner;
    }

    [DisableAutoCreation]
    //[UpdateAfter(typeof(TimeSystem))]
    [UpdateBefore(typeof(SpawnerSystem))]
    public partial class ArenaSpawnerSystem : GameSystemBase
    {
        TimeSystem timeSystem;
        EntityQuery spawnedQuery;
        EntityArchetype spawnFinishedMessageArchetype;
        EntityArchetype spawnRequestArchetype;

        protected override void OnCreate()
        {
            base.OnCreate();
            timeSystem = World.GetExistingSystemManaged<TimeSystem>();
            spawnedQuery = GetEntityQuery(ComponentType.ReadOnly<SpawnedBy>());
            spawnFinishedMessageArchetype = EntityManager.CreateArchetype(typeof(Message), typeof(MessageDelay));
            spawnRequestArchetype = EntityManager.CreateArchetype(typeof(SpawnRequest), typeof(SpawnPointArrayReference));
        }

        protected override void OnSystemUpdate()
        {
            // удаление спаун запросов, если их спаунеры были удалены, например во время загрзуки сцен
            Entities
                .WithStructuralChanges()
                .WithoutBurst()
                .ForEach((Entity entity, in SpawnRequest request) =>
            {
                if (EntityManager.Exists(entity) == false)
                {
                    Debug.Log("Destroying spawn request with null spawner");
                    EntityManager.DestroyEntity(entity);
                }
            }).Run();
            
            var currentTime = timeSystem.GameTime;
            var commands = CreateUniversalCommandBuffer();
            var spawnedChunks = CreateArchetypeChunkArrayWithUpdateAllocator(spawnedQuery);
            var spawnedType = GetComponentTypeHandle<SpawnedBy>(true);
            var livingStateType = GetComponentTypeHandle<LivingState>(true);
            var entityType = GetEntityTypeHandle();

            Entities
                .WithReadOnly(spawnedChunks)
                .WithReadOnly(spawnedType)
                .WithReadOnly(entityType)
                .ForEach((Entity entity, int entityInQueryIndex, in StopSpawnAndDestroySpawned stopRequest) =>
            {
                commands.DestroyEntity(entityInQueryIndex, entity);
                var spawner = GetComponent<ArenaSpawner>(stopRequest.Spawner);
                spawner.State = ArenaSpawnerState.Finished;
                SetComponent(stopRequest.Spawner, spawner);

                for (int c = 0; c < spawnedChunks.Length; c++)
                {
                    var chunk = spawnedChunks[c];

                    var spawnedArray = chunk.GetNativeArray(spawnedType);
                    var spawnedEntityArray = chunk.GetNativeArray(entityType);

                    for (int i = 0; i < spawnedArray.Length; i++)
                    {
                        var spawned = spawnedArray[i];

                        if (spawned.SpawnerEntity == stopRequest.Spawner)
                        {
                            commands.DestroyEntity(entityInQueryIndex, spawnedEntityArray[i]);
                        }
                    }
                }

            }).Run();

            var requestArchetype = spawnRequestArchetype;

            Entities
                .WithReadOnly(spawnedChunks)
                .WithReadOnly(spawnedType)
                .WithReadOnly(livingStateType)
                .ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<SpawnInfoArrayElement> infos, ref ArenaSpawner spawner) =>
            {
                if(spawner.State == ArenaSpawnerState.Idle || spawner.State == ArenaSpawnerState.Finished)
                {
                    return;
                }

                if(spawner.State == ArenaSpawnerState.PendingStart)
                {
                    spawner.State = ArenaSpawnerState.Running;
                    spawner.CurrentObject = 0;
                    spawner.SpawnCounter = 0;
                    spawner.LastSpawnTime = double.MinValue;
                }

                if(spawner.State == ArenaSpawnerState.WaitingForFinish)
                {
                    int spawnedCount = 0;

                    for(int c=0; c<spawnedChunks.Length; c++)
                    {
                        var chunk = spawnedChunks[c];

                        var spawnedArray = chunk.GetNativeArray(spawnedType);
                        var livingStateArray = chunk.GetNativeArray(livingStateType);

                        for(int i=0; i<spawnedArray.Length; i++)
                        {
                            var spawned = spawnedArray[i];

                            if(spawned.SpawnerEntity == entity)
                            {
                                var livingState = livingStateArray[i];
                                if(livingState.IsAlive)
                                {
                                    spawnedCount++;
                                }
                            }
                        }
                    }

                    if(spawnedCount == 0)
                    {
                        CreateFinishedEvent(entity, entityInQueryIndex, commands);
                        spawner.State = ArenaSpawnerState.Finished;
                    }

                    return;
                }

                SpawnInfoArrayElement currentInfo = default;

                if (GetSpawnInfo(spawner.CurrentGroup, spawner.CurrentObject, ref infos, out currentInfo) == false)
                {
                    CreateFinishedEvent(entity, entityInQueryIndex, commands);
                    spawner.State = ArenaSpawnerState.Finished;
                    return;
                }

                if (currentInfo.Parameters.Count <= spawner.SpawnCounter)
                {
                    spawner.SpawnCounter = 0;
                    spawner.CurrentObject++;

                    if (GetSpawnInfo(spawner.CurrentGroup, spawner.CurrentObject, ref infos, out currentInfo) == false)
                    {
                        spawner.State = ArenaSpawnerState.WaitingForFinish;
                        return;
                    }
                }

                if(currentTime - spawner.LastSpawnTime < currentInfo.Parameters.SpawnInterval)
                {
                    return;
                }

                spawner.SpawnCounter++;
                spawner.LastSpawnTime = currentTime;

                var requestEntity = commands.CreateEntity(entityInQueryIndex, requestArchetype);

                //commands.AddComponent(nativeThreadIndex, requestEntity, new SpawnRequest { PrefabID = currentInfo.Parameters.ObjectKeyID });
                commands.AddComponent(entityInQueryIndex, requestEntity, new SpawnRequest { SpawnerEntity = entity, Prefab =currentInfo.Parameters.Prefab });
                var spawnPoints = currentInfo.Parameters.OptionalSpawnPoints.Entity != Entity.Null ? currentInfo.Parameters.OptionalSpawnPoints.Entity : entity;
                commands.AddComponent(entityInQueryIndex, requestEntity, new SpawnPointArrayReference { Entity = spawnPoints });
                //commands.AddComponent(nativeThreadIndex, requestEntity, new AllocateIdRequest());

            }).Run();
            
            

            Entities
                .WithChangeFilter<SpawnedBy>()
                .WithAll<Level>()
                .ForEach((Entity entity, int entityInQueryIndex, in SpawnedBy spawnedBy) =>
                {
                    if(HasComponent<Level>(spawnedBy.SpawnerEntity) == false)
                    {
                        return;
                    }
                    var level = GetComponent<Level>(spawnedBy.SpawnerEntity);
                    commands.SetComponent(entityInQueryIndex, entity, level);
                }).Run();

            if (SystemAPI.TryGetSingleton(out ArenaMatchStateData matchStateData))
            {
                var spawnerTriggerJob = new ArenaSpawnerTriggerJob
                {
                    Commands = commands,
                    InternalState = matchStateData
                };

                Entities
                    .ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<OverlappingEntities> overlappingEntities, in ArenaSpawner arenaSpawner, in Level level) =>
                    {
                        spawnerTriggerJob.Spawner = arenaSpawner;
                        spawnerTriggerJob.Level = level;
                        spawnerTriggerJob.SpawnerEntity = entity;
                        spawnerTriggerJob.EntityInQueryIndex = entityInQueryIndex;

                        StatefulTriggerEventUtility.CheckState(spawnerTriggerJob, entity, overlappingEntities);

                    }).Run();
            }
            

            var messagesFromEntity = GetBufferLookup<MessagesToSendOnFinishSpawn>(true);
            var msgArchetype = spawnFinishedMessageArchetype;

            Entities
                .ForEach((
                Entity entity,
                int entityInQueryIndex,
                in ArenaSpawnerFinishedEvent evt
                ) =>
            {
                commands.DestroyEntity(entityInQueryIndex, entity);

                if(messagesFromEntity.HasComponent(evt.Spawner) == false)
                {
                    return;
                }
                var messages = messagesFromEntity[evt.Spawner];
                var targets = GetBuffer<MessageTargets>(evt.Spawner);

                foreach(var message in messages)
                {
                    UnityEngine.Debug.Log($"Sending spawn finish message {message.Value.HashCode}"); 
                    var evtEntity = commands.CreateEntity(entityInQueryIndex, msgArchetype);
                    commands.SetComponent(entityInQueryIndex, evtEntity, message.Value);
                    commands.SetComponent(entityInQueryIndex, evtEntity, new MessageDelay
                    {
                        StartTime = currentTime,
                        Delay = message.Delay
                    });

                    DynamicBuffer<Targets> messageTargets = default;

                    foreach(var target in targets)
                    {
                        if(target.Message.HashCode != message.Value.HashCode)
                        {
                            continue;
                        }
                        if(messageTargets.IsCreated == false)
                        {
                            messageTargets = commands.AddBuffer<Targets>(entityInQueryIndex, evtEntity);
                        }
                        messageTargets.Add(new Targets(target.Target));
                    }
                }

            }).Run();
        }

        private static void CreateFinishedEvent(Entity entity, int nativeThreadIndex, UniversalCommandBuffer commands)
        {
            var evtEntity = commands.CreateEntity(nativeThreadIndex);
            commands.AddComponent(nativeThreadIndex, evtEntity, new ArenaSpawnerFinishedEvent
            {
                Spawner = entity
            });
        }

        [BurstCompile]
        struct ArenaSpawnerTriggerJob : IEntityTriggerStateEventHandler
        {
            public Entity SpawnerEntity;
            public ArenaSpawner Spawner;
            public ArenaMatchStateData InternalState;
            public Level Level;
            public UniversalCommandBuffer Commands;
            public int EntityInQueryIndex;

            public void OnEnter(Entity colliderEntity, Entity enteredEntity)
            {
                int state = (int)Spawner.State;
                Debug.Log($"Arena spawner trigger enter {state}");

                if(Spawner.State != ArenaSpawnerState.Idle)
                {
                    return;
                }

                Spawner.CurrentGroup = 0;
                Spawner.State = ArenaSpawnerState.PendingStart;

                Level = new Level { Value = (ushort)(InternalState.CurrentStage * 5) };

                Commands.SetComponent(EntityInQueryIndex, SpawnerEntity, Spawner);
                Commands.SetComponent(EntityInQueryIndex, SpawnerEntity, Level);
            }

            public void OnExit(Entity colliderEntity, Entity exitedEntity)
            {
            }

            public void OnStay(Entity colliderEntity, Entity stayingEntity)
            {
            }
        }

        static bool GetSpawnInfo(uint currentStage, int currentObject, ref DynamicBuffer<SpawnInfoArrayElement> infos, out SpawnInfoArrayElement result)
        {
            result = default;

            for (int i = 0; i < infos.Length; i++)
            {
                var info = infos[i];

                if (info.GroupIndex != currentStage)
                {
                    continue;
                }

                if (info.ObjectIndex != currentObject)
                {
                    continue;
                }
                result = info;
                return true;
            }
            return false;
        }
    }
}
