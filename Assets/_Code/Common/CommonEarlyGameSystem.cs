using Arena.ScriptViz;
using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using TzarGames.MatchFramework.Server;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
        private EntityQuery setBaseLocationRequestQuery;
        
        struct NavMeshDataCleanup : ICleanupComponentData
        {
            public NavMeshDataInstance NavDataInstance;
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
                    Entity entity, 
                    int entityInQueryIndex, 
                    DynamicBuffer<ScriptViz.DeadEventData> deathEvents,
                    DynamicBuffer<VariableDataByte> variableData,
                    DynamicBuffer<EntityVariableData> entityVars,
                    in LivingState livingState, 
                    in ScriptVizCodeInfo codeDataInfo) =>
                {
                    if (livingState.WasDeadInCurrentFrame() == false)
                    {
                        return;
                    }
                    var codeData = SystemAPI.GetBuffer<CodeDataByte>(codeDataInfo.CodeDataEntity);
                    var constantEntityData = SystemAPI.GetBuffer<ConstantEntityVariableData>(codeDataInfo.CodeDataEntity);
                    var codeAsset = new ScriptVizCodeAsset((byte*)codeData.GetUnsafeReadOnlyPtr());

                    using (var stackMemory = new NativeArray<byte>(codeDataInfo.TempMemorySize, Allocator.Temp))
                    {
                        foreach (var deathEvent in deathEvents)
                        {
                            var context = new Context(
                                entity,
                                codeAsset, 
                                ref variableData, 
                                constantEntityData.AsNativeArray(), 
                                entityVars.AsNativeArray(),
                                stackMemory.GetUnsafePtr(), 
                                ref commands, 
                                entityInQueryIndex, 
                                deltaTime
                                //deathEvent.CommandAddress
                                );
                            
                            Extensions.ExecuteCode(ref context, deathEvent.CommandAddress);    
                        }
                    }

                }).Run();

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
                            
                            var codeBytes = SystemAPI.GetBuffer<CodeDataByte>(aspect.CodeInfo.ValueRO.CodeDataEntity);
                            var constEntityVarData = SystemAPI.GetBuffer<ConstantEntityVariableData>(aspect.CodeInfo.ValueRO.CodeDataEntity);

                            using (var contextHandle = new ContextDisposeHandle(codeBytes, constEntityVarData, ref aspect, ref commands, entityInQueryIndex, deltaTime))
                            {
                                if (request.DataAddress.IsValid)
                                {
                                    var flagsData =
                                        SystemAPI.GetBuffer<CharacterGameProgressFlags>(progressDataEntity);
                                        
                                    var progressData = new GameProgressSocketData
                                    {
                                        ProgressEntity = progressDataEntity,
                                        FlagsCount = (ushort)flagsData.Length,
                                        FlagsPointer = (System.IntPtr)flagsData.GetUnsafeReadOnlyPtr(),
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
    }
}
