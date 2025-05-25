using Arena.ScriptViz;
using Arena.Server;
using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using TzarGames.GameCore.ScriptViz.Commands;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Server;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(PreSimulationSystemGroup))]
    public partial class CommonEarlyGameSystem : GameSystemBase
    {
        private EntityQuery getGameProgressRequestQuery;
        private EntityQuery addGameProgressFlagRequestQuery;
        private EntityQuery setGameProgressKeyValueQuery;
        private EntityQuery saveGameRequestQuery;
        private EntityQuery setBaseLocationRequestQuery;
        private EntityQuery startQuestQuery;
        private EntityQuery addGameProgressQuestRequestQuery;
        private EntityQuery getMainCharacterRequestQuery;
        private EntityQuery getAimPointRequestQuery;

        struct NavMeshDataCleanup : ICleanupComponentData
        {
            public NavMeshDataInstance NavDataInstance;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            startQuestQuery = GetEntityQuery(ComponentType.ReadOnly<StartQuestRequest>());
            getAimPointRequestQuery = GetEntityQuery(ComponentType.ReadOnly<GetAimPointRequest>());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, in NavMeshDataCleanup data) =>
                {   
                    Debug.Log($"removing navmesh data from entity {entity}");
                    NavMesh.RemoveNavMeshData(data.NavDataInstance);
                    EntityManager.RemoveComponent<NavMeshDataCleanup>(entity);
                    
                }).Run();
        }

        Entity getMainPlayerEntity()
        {
            // TODO на данный момент тупо выбирается первый попавшийся игрок в качестве "главного". Сделать явное определение главного игрока и обрабатывать именно его данные прогресса
            var registeredPlayers = SystemAPI.GetSingletonBuffer<RegisteredPlayer>();
            if (registeredPlayers.Length == 0)
            {
                return Entity.Null;
            }
            return registeredPlayers[0].PlayerEntity;
        }

        protected override unsafe void OnSystemUpdate()
        {
            var ecb = CreateCommandBuffer();
            var commands = ecb.AsParallelWriter();
            var deltaTime = World.Time.DeltaTime;
            
            // проверяем событие смерти заранее, чтобы WasDeadInCurrentFrame работал и в сетевой версии тоже
            // из-за этого событие будет происходить с задержкой в один кадр в одиночной версии игры
            Entities
                .WithChangeFilter<DeathData>()
                .ForEach((
                    int entityInQueryIndex,
                    ScriptVizAspect aspect,
                    DynamicBuffer<ScriptViz.DeadEventData> deathEvents,
                    in LivingState livingState) =>
                {
                    if (livingState.WasDeadInCurrentFrame() == false)
                    {
                        return;
                    }
                    
                    var handle = new ContextDisposeHandle(ref aspect, ref commands, entityInQueryIndex, deltaTime);
                    
                    foreach (var deathEvent in deathEvents)
                    {
                        handle.Execute(deathEvent.CommandAddress);   
                    }

                }).Run();
            
            Entities
                .ForEach((Entity entity, ScriptVizAspect aspect, in DynamicBuffer<OnTargetChangedEventCommand> events, in Target target, in TargetChangedEventPreviousTarget prevTarget) =>
                {
                    int sortKey = 0;
                    
                    if (target.Value == prevTarget.Value)
                    {
                        return;
                    }
                    commands.SetComponent(sortKey, entity, new TargetChangedEventPreviousTarget { Value = target.Value });
                    
                    using (var handle = new ContextDisposeHandle(ref aspect, ref commands, sortKey, deltaTime))
                    {
                        var context = handle.Context;
                        
                        foreach (var evt in events)
                        {
                            if (evt.TargetEntityOutputAddress.IsValid)
                            {
                                context.WriteToTemp(target.Value, evt.TargetEntityOutputAddress);
                            }
                            Extensions.ExecuteCode(ref context, evt.CommandAddress);
                        }    
                    }
                    
                }).Run();

            if (startQuestQuery.IsEmpty == false)
            {
                EntityManager.DestroyEntity(startQuestQuery);
            }

            if (saveGameRequestQuery.IsEmpty == false)
            {
                var registeredPlayerEntity = getMainPlayerEntity();
                var characterEntity = SystemAPI.GetComponent<ControlledCharacter>(registeredPlayerEntity).Entity;
                var progressDataEntity = SystemAPI.GetComponent<CharacterGameProgressReference>(characterEntity).Value;
                
                Entities
                    .WithStoreEntityQueryInField(ref saveGameRequestQuery)
                    .WithAll<SaveGameRequest>()
                    .ForEach((Entity entity) =>
                    {
                        commands.DestroyEntity(0, entity);
                        
                        var owner = SystemAPI.GetComponent<Owner>(progressDataEntity);
                        
                        var playerEntity = SystemAPI.GetComponent<PlayerController>(owner.Value).Value;

                        if (SystemAPI.HasComponent<AuthorizedUser>(playerEntity) == false)
                        {
                            Debug.Log("Failed to save player data, player not authorized");
                            return;
                        }
                
                        Debug.Log($"Saving data for player character {owner.Value.Index} by request");
                        var user = SystemAPI.GetComponent<AuthorizedUser>(playerEntity);

                        createSavePlayerDataRequest(owner, playerEntity, user, commands);
                        
                    }).Run();
            }

            if (setGameProgressKeyValueQuery.IsEmpty == false)
            {
                var registeredPlayerEntity = getMainPlayerEntity();
                var characterEntity = SystemAPI.GetComponent<ControlledCharacter>(registeredPlayerEntity).Entity;
                var progressDataEntity = SystemAPI.GetComponent<CharacterGameProgressReference>(characterEntity).Value;
                
                Entities
                    .WithStoreEntityQueryInField(ref setGameProgressKeyValueQuery)
                    .ForEach((Entity entity, int entityInQueryIndex, in SetGameProgressKeyRequest request) =>
                {
                    commands.DestroyEntity(entityInQueryIndex, entity);
                    
                    var keys = SystemAPI.GetBuffer<CharacterGameProgressKeyValue>(progressDataEntity);
                    bool found = false;
                    
                    for (var index = 0; index < keys.Length; index++)
                    {
                        var keyValue = keys[index];
                        if (keyValue.Key == request.Key)
                        {
                            keyValue.Value = request.Value;
                            keys[index] = keyValue;
                            found = true;
                            Debug.Log($"set game progress key {request.Key} value: {keyValue.Value}");
                        }
                    }

                    if (found == false)
                    {
                        keys.Add(new CharacterGameProgressKeyValue
                        {
                            Key = (ushort)request.Key,
                            Value = request.Value
                        });
                    
                        Debug.Log($"add game progress key {request.Key} value: {request.Value}");    
                    }
                    
                    var owner = SystemAPI.GetComponent<Owner>(progressDataEntity);
                    var playerEntity = SystemAPI.GetComponent<PlayerController>(owner.Value).Value;

                    if (SystemAPI.HasComponent<AuthorizedUser>(playerEntity) == false)
                    {
                        Debug.Log("Failed to save player data on item activation, player not authorized");
                        return;
                    }
                
                    Debug.Log($"Saving data for player character {owner.Value.Index}, bcz progress data changed");
                    var user = SystemAPI.GetComponent<AuthorizedUser>(playerEntity);

                    createSavePlayerDataRequest(owner, playerEntity, user, commands);
                    
                }).Run();
            }

            if (addGameProgressFlagRequestQuery.IsEmpty == false)
            {
                var registeredPlayerEntity = getMainPlayerEntity();
                var characterEntity = SystemAPI.GetComponent<ControlledCharacter>(registeredPlayerEntity).Entity;
                var progressDataEntity = SystemAPI.GetComponent<CharacterGameProgressReference>(characterEntity).Value;
                
                Entities
                    .WithStoreEntityQueryInField(ref addGameProgressFlagRequestQuery)
                    .ForEach((Entity entity, int entityInQueryIndex, AddGameProgressFlagRequest request) =>
                    {
                        commands.DestroyEntity(entityInQueryIndex, entity);
                        
                        var flags = SystemAPI.GetBuffer<CharacterGameProgressFlags>(progressDataEntity);

                        foreach (var flag in flags)
                        {
                            if (flag.Value == request.FlagKey)
                            {
                                Debug.LogError($"Already has game progress flag {request.FlagKey}");
                                return;
                            }
                        }

                        flags.Add(new CharacterGameProgressFlags
                        {
                            Value = (ushort)request.FlagKey
                        });
                        
                        Debug.Log($"added game progress flag {request.FlagKey}");

                        var owner = SystemAPI.GetComponent<Owner>(progressDataEntity);
                        var playerEntity = SystemAPI.GetComponent<PlayerController>(owner.Value).Value;

                        if (SystemAPI.HasComponent<AuthorizedUser>(playerEntity) == false)
                        {
                            Debug.Log("Failed to save player data on item activation, player not authorized");
                            return;
                        }
                
                        Debug.Log($"Saving data for player character {owner.Value.Index}, bcz progress data changed");
                        var user = SystemAPI.GetComponent<AuthorizedUser>(playerEntity);

                        createSavePlayerDataRequest(owner, playerEntity, user, commands);

                    }).Run();    
            }
            
            if (addGameProgressQuestRequestQuery.IsEmpty == false)
            {
                var registeredPlayerEntity = getMainPlayerEntity();
                var characterEntity = SystemAPI.GetComponent<ControlledCharacter>(registeredPlayerEntity).Entity;
                var progressDataEntity = SystemAPI.GetComponent<CharacterGameProgressReference>(characterEntity).Value;
                
                Entities
                    .WithStoreEntityQueryInField(ref addGameProgressQuestRequestQuery)
                    .ForEach((Entity entity, int entityInQueryIndex, AddGameProgressQuestRequest request) =>
                    {
                        commands.DestroyEntity(entityInQueryIndex, entity);
                        
                        var quests = SystemAPI.GetBuffer<CharacterGameProgressQuests>(progressDataEntity);
                        int questIndex = -1;

                        for (var index = 0; index < quests.Length; index++)
                        {
                            var quest = quests[index];
                            
                            if (quest.QuestID == request.QuestKey)
                            {
                                if (quest.QuestState == request.State)
                                {
                                    Debug.LogWarning($"Quest {request.QuestKey} state already equals to {request.State}");
                                    return;
                                }
                                
                                Debug.Log($"Set quest {request.QuestKey} to state {request.State}");
                                quest.QuestState = request.State;
                                quests[index] = quest;
                                questIndex = index;
                                break;
                            }
                        }

                        if (questIndex == -1)
                        {
                            quests.Add(new CharacterGameProgressQuests
                            {
                                QuestID = (ushort)request.QuestKey
                            });
                            Debug.Log($"added quest {request.QuestKey} with state {request.State}");
                        }
                        
                        var owner = SystemAPI.GetComponent<Owner>(progressDataEntity);
                        var playerEntity = SystemAPI.GetComponent<PlayerController>(owner.Value).Value;

                        if (SystemAPI.HasComponent<AuthorizedUser>(playerEntity) == false)
                        {
                            Debug.Log("Failed to save player data on item activation, player not authorized");
                            return;
                        }
                
                        Debug.Log($"Saving data for player character {owner.Value.Index}, bcz progress data changed");
                        var user = SystemAPI.GetComponent<AuthorizedUser>(playerEntity);

                        createSavePlayerDataRequest(owner, playerEntity, user, commands);

                    }).Run();    
            }

            if (setBaseLocationRequestQuery.IsEmpty == false)
            {
                var registeredPlayerEntity = getMainPlayerEntity();

                if (registeredPlayerEntity != Entity.Null)
                {
                    var characterEntity = SystemAPI.GetComponent<ControlledCharacter>(registeredPlayerEntity).Entity;
                    var progressDataEntity = SystemAPI.GetComponent<CharacterGameProgressReference>(characterEntity).Value;
                    
                    Entities
                        .WithStoreEntityQueryInField(ref setBaseLocationRequestQuery)
                        .ForEach((Entity entity, int entityInQueryIndex, in SetBaseLocationRequest request) =>
                    {
                        commands.DestroyEntity(entityInQueryIndex, entity);

                        var gameProgress = SystemAPI.GetComponent<CharacterGameProgress>(progressDataEntity);
                        
                        gameProgress.CurrentBaseLocationID = request.LocationID;
                        gameProgress.CurrentBaseLocationSpawnPointID = request.SpawnPointID;
                        
                        SystemAPI.SetComponent(progressDataEntity, gameProgress);
                        Debug.Log($"set base location to {request.LocationID}");

                    }).Run();
                }
            }

            if(getMainCharacterRequestQuery.IsEmpty == false && SystemAPI.HasSingleton<RegisteredPlayer>())
            {
                var registeredPlayerEntity = getMainPlayerEntity();

                if(registeredPlayerEntity != Entity.Null)
                {
                    if (SystemAPI.HasComponent<ControlledCharacter>(registeredPlayerEntity))
                    {
                        var characterEntity = SystemAPI.GetComponent<ControlledCharacter>(registeredPlayerEntity).Entity;

                        Entities
                        .WithStoreEntityQueryInField(ref getMainCharacterRequestQuery)
                        .ForEach((
                            Entity entity,
                            int entityInQueryIndex,
                            GetMainCharacterRequest request) =>
                        {
                            commands.DestroyEntity(entityInQueryIndex, entity);

                            var aspect = SystemAPI.GetAspect<ScriptVizAspect>(request.ScriptVizEntity);

                            using (var contextHandle = new ContextDisposeHandle(ref aspect, ref commands, entityInQueryIndex, deltaTime))
                            {
                                if (request.CharacterAddress.IsValid)
                                {
                                    contextHandle.Context.WriteToTemp(ref characterEntity, request.CharacterAddress);
                                }

                                contextHandle.Execute(request.CommandAddress);
                            }

                        }).Run();
                    }
                }
            }
            

            if (getGameProgressRequestQuery.IsEmpty == false && SystemAPI.HasSingleton<RegisteredPlayer>())
            {
                var registeredPlayerEntity = getMainPlayerEntity();

                if (registeredPlayerEntity != Entity.Null)
                {
                    if (SystemAPI.HasComponent<ControlledCharacter>(registeredPlayerEntity))
                    {
                        var characterEntity = SystemAPI.GetComponent<ControlledCharacter>(registeredPlayerEntity).Entity;
                        var progressDataEntity = SystemAPI.GetComponent<CharacterGameProgressReference>(characterEntity).Value;
                    
                        Entities
                        .WithStoreEntityQueryInField(ref getGameProgressRequestQuery)
                        .ForEach((
                            Entity entity,
                            int entityInQueryIndex,
                            GetGameProgressRequest request) =>
                        {
                            commands.DestroyEntity(entityInQueryIndex, entity);

                            var aspect = SystemAPI.GetAspect<ScriptVizAspect>(request.ScriptVizEntity);
                            
                            using (var contextHandle = new ContextDisposeHandle(ref aspect, ref commands, entityInQueryIndex, deltaTime))
                            {
                                if (request.DataAddress.IsValid)
                                {
                                    var flagsData =
                                        SystemAPI.GetBuffer<CharacterGameProgressFlags>(progressDataEntity);

                                    var keysData =
                                        SystemAPI.GetBuffer<CharacterGameProgressKeyValue>(progressDataEntity);

                                    var questData =
                                        SystemAPI.GetBuffer<CharacterGameProgressQuests>(progressDataEntity);
                                        
                                    var progressData = new GameProgressSocketData
                                    {
                                        ProgressEntity = progressDataEntity,
                                        FlagsCount = (ushort)flagsData.Length,
                                        FlagsPointer = flagsData.GetUnsafeReadOnlyPtr(),
                                        KeysPointer = keysData.GetUnsafeReadOnlyPtr(),
                                        QuestsPointer = questData.GetUnsafeReadOnlyPtr(),
                                        QuestCount = (ushort)questData.Length,
                                        KeysCount = (ushort)keysData.Length
                                    };
                                        
                                    contextHandle.Context.WriteToTemp(ref progressData, request.DataAddress);
                                }
                                
                                contextHandle.Execute(request.CommandAddress);  
                            }
                            
                        }).Run();
                    }
                }
            }
            
            Entities
                .WithNone<DisableAutoDestroy>()
                .ForEach((Entity entity, int entityInQueryIndex, in PlayerController controller) =>
                {
                    if(SystemAPI.Exists(controller.Value) == false)
                    {
                        Debug.Log($"Destroying character {entity.Index}");
                        commands.DestroyEntity(entityInQueryIndex, entity);
                    }

                }).Run();
            
            Entities
                .WithoutBurst()
                .WithNone<NavMeshDataCleanup>()
                .ForEach((Entity entity, NavMeshManagedData navData) =>
                {
                    Debug.Log($"adding navmesh data from entity {entity}");
                    
                    var instance = NavMesh.AddNavMeshData(navData.Data);
                    navData.IsProcessed = true;
                    commands.AddComponent(0, entity, new NavMeshDataCleanup
                    {
                        NavDataInstance = instance
                    });

                }).Run();
            
            Entities
                .WithoutBurst()
                .WithNone<NavMeshManagedData>()
                .ForEach((Entity entity, in NavMeshDataCleanup data) =>
                {   
                    Debug.Log($"removing navmesh data from entity {entity}");
                    NavMesh.RemoveNavMeshData(data.NavDataInstance);
                    commands.RemoveComponent<NavMeshDataCleanup>(0, entity);
                    
                }).Run();
            
            if (getAimPointRequestQuery.IsEmpty == false)
            {
                ecb.DestroyEntity(getAimPointRequestQuery);
                var requestChunks = getAimPointRequestQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out var requestDeps);
                Dependency = JobHandle.CombineDependencies(Dependency, requestDeps);
                var reqeustChunkList = requestChunks.AsDeferredJobArray();
                
                var getAimPointJob = new GetAimHitPointJob()
                {
                    AimHitPointLookup = GetComponentLookup<AimHitPoint>(true),
                    DeltaTime = deltaTime,
                    EntityType = GetEntityTypeHandle(),
                    Commands = commands,
                    RequestChunks = reqeustChunkList,
                    RequestType = GetComponentTypeHandle<GetAimPointRequest>(true),
                };
#if TZAR_GAMECORE_THREADS
                Dependency = getTransformJob.Schedule(Dependency);
#else
                Dependency.Complete();
                getAimPointJob.Run();
#endif
                Dependency = requestChunks.Dispose(Dependency);
            }
        }

        static void createSavePlayerDataRequest(Owner owner, Entity playerEntity, AuthorizedUser user, EntityCommandBuffer.ParallelWriter commands)
        {
            var requestEntity = commands.CreateEntity(0);
                
            commands.AddComponent(0, requestEntity, new PlayerDataSaveRequest
            {
                Owner = Entity.Null,
                PlayerId = user.Value,
                State = PlayerDataRequestState.Pending
            });
            commands.AddComponent(0, requestEntity, new Target { Value = playerEntity });
            commands.AddComponent(0, requestEntity, new PlayerSaveData
            {
                CharacterEntity = owner.Value
            });
        }
    }
    
    [BurstCompile]
    partial struct GetAimHitPointJob : IJobEntity
    {
        [ReadOnly]
        public NativeArray<ArchetypeChunk> RequestChunks;

        [ReadOnly] public ComponentTypeHandle<GetAimPointRequest> RequestType;
        [ReadOnly] public EntityTypeHandle EntityType;
        [ReadOnly] public ComponentLookup<AimHitPoint> AimHitPointLookup;
        public float DeltaTime;

        public EntityCommandBuffer.ParallelWriter Commands;
        
        public void Execute(ScriptVizAspect aspect, [ChunkIndexInQuery] int sortIndex)
        {
            ContextDisposeHandle handle = default;
            
            foreach (var chunk in RequestChunks)
            {
                var requests = chunk.GetNativeArray(ref RequestType);
                var entities = chunk.GetNativeArray(EntityType);

                for (var i = 0; i < requests.Length; i++)
                {
                    var request = requests[i];
                    
                    if (request.RequestEntity != aspect.Self)
                    {
                        continue;
                    }

                    var entity = entities[i];

                    if (request.NextCommandAddress.IsInvalid)
                    {
                        Debug.LogError(
                            $"next command address is null, scriptviz entity: {request.RequestEntity.Index}, request: {entity.Index}:{entity.Version}");
                        return;
                    }

                    var target = request.TargetEntity;
                    
                    if (AimHitPointLookup.TryGetComponent(target, out var point) == false)
                    {
                        Debug.LogError(
                            $"failed to get aim hit point from target {target.Index}, request: {entity.Index}:{entity.Version},  exist: {AimHitPointLookup.EntityExists(target)}");
                        return;
                    }

                    if (handle.IsCreated == false)
                    {
                        handle = new ContextDisposeHandle(ref aspect, ref Commands, sortIndex, DeltaTime);
                    }

                    if (request.AimPointAddress.IsValid)
                    {
                        handle.Context.WriteToTemp(ref point.Value, request.AimPointAddress);
                    }

                    var dirFromSource = point.Value - request.SourcePoint;
                    
                    if (request.DirFromSourcePointAddress.IsValid)
                    {
                        handle.Context.WriteToTemp(math.normalizesafe(dirFromSource), request.DirFromSourcePointAddress);
                    }

                    if (request.DistanceFromSourcePointAddress.IsValid)
                    {
                        handle.Context.WriteToTemp(math.length(dirFromSource), request.DistanceFromSourcePointAddress);
                    }

                    handle.Execute(request.NextCommandAddress);
                }
            }
        }
    }
}
