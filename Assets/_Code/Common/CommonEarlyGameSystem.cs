using Arena.ScriptViz;
using Arena.Server;
using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using TzarGames.GameCore.ScriptViz.Commands;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Server;
using Unity.Entities;
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
        private EntityQuery setBaseLocationRequestQuery;
        private EntityQuery startQuestQuery;
        
        struct NavMeshDataCleanup : ICleanupComponentData
        {
            public NavMeshDataInstance NavDataInstance;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            startQuestQuery = GetEntityQuery(ComponentType.ReadOnly<StartQuestRequest>());
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
            var commands = CreateUniversalCommandBuffer();
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
                                        
                                    var progressData = new GameProgressSocketData
                                    {
                                        ProgressEntity = progressDataEntity,
                                        FlagsCount = (ushort)flagsData.Length,
                                        FlagsPointer = (System.IntPtr)flagsData.GetUnsafeReadOnlyPtr(),
                                        KeysPointer = (System.IntPtr)keysData.GetUnsafeReadOnlyPtr(),
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
                .ForEach((Entity entity, in NavMeshManagedData navData) =>
                {
                    Debug.Log($"adding navmesh data from entity {entity}");
                    
                    var instance = NavMesh.AddNavMeshData(navData.Data);
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
        }

        static void createSavePlayerDataRequest(Owner owner, Entity playerEntity, AuthorizedUser user, UniversalCommandBuffer commands)
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
}
