using System.Reflection;
using Arena.Server;
using TzarGames.GameCore;
using TzarGames.MatchFramework.Server;
using TzarGames.MultiplayerKit;
using Unity.Entities;
using UnityEngine;
using Unity.Scenes;

namespace Arena.GameSceneCode
{
    [Sync]
    public struct SceneSectionState : IBufferElementData
    {
        public int SectionIndex;
        public bool ShouldBeLoaded;

        [DontSync]
        public Entity SectionEntity;

        [DontSync]
        public SceneSectionLoadState State;
    }

    public enum SceneSectionLoadState : byte
    {
        Unloaded,
        Loading,
        Loaded
    }

    public struct SceneAssetInstance : ICleanupBufferElementData
    {
        public PrefabID ID;
        public Entity Value;
    }

    [DisableAutoCreation]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class GameSceneSystem : GameSystemBase
    {
        protected override void OnSystemUpdate()
        {
            var commands = CreateUniversalCommandBuffer();
            
            Entities
                .WithAll<GameSceneDescription>()
                .WithNone<SceneAssetInstance>().ForEach((Entity entity, int entityInQueryIndex) =>
            {
                commands.AddBuffer<SceneAssetInstance>(entityInQueryIndex, entity);
            
            }).Run();
            
            // уничтожаем все сцены, которые были созданы в удаленных игровых сценах
            Entities
                .WithNone<GameSceneDescription>()
                .ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<SceneAssetInstance> sceneInstances) =>
                {
                    Debug.Log($"Destorying scenes for game scene {entity.Index}");
                    foreach (var sceneAssetInstance in sceneInstances)
                    {
                        Debug.Log($"Destroying scene asset instance {sceneAssetInstance.Value.Index}");
                        commands.DestroyEntity(entityInQueryIndex, sceneAssetInstance.Value);
                    }
                    commands.RemoveComponent<SceneAssetInstance>(entityInQueryIndex, entity);
            
                }).Run();
            
            // контроль готовности загрузки сцен
            Entities
                .ForEach((
                    Entity entity, 
                    int entityInQueryIndex, 
                    DynamicBuffer<AutoLoadSceneSection> sectionsLoLoad,
                    DynamicBuffer<SceneAssetInstance> sceneAssetInstances,
                    ref SceneLoadingState loadingState,
                    in GameSceneDescription gameSceneDesc
                    ) =>
            {
                switch (loadingState.Value)
                {
                    case SceneLoadingStates.PendingStartLoading:
                        {
                            loadingState.Value = SceneLoadingStates.Loading;

                            var mainSubSceneEntity = gameSceneDesc.MainSubScene;

                            if (mainSubSceneEntity == Entity.Null)
                            {
                                Debug.LogError($"Main subscene entity is invalid, game scene entity: {entity.Index}");
                                return;
                            }

                            if (SystemAPI.HasComponent<SceneName>(mainSubSceneEntity) == false)
                            {
                                Debug.LogError($"Main subscene entity {mainSubSceneEntity.Index} does not contain SceneName component, game scene entity: {entity.Index}");
                                return;
                            }

                            if (SystemAPI.HasComponent<PrefabID>(mainSubSceneEntity) == false)
                            {
                                Debug.LogError($"Main subscene entity {mainSubSceneEntity.Index} does not contain PrefabID component, game scene entity: {entity.Index}");
                                return;
                            }
            
                            Debug.Log($"Instantiating main subscene {SystemAPI.GetComponent<SceneName>(gameSceneDesc.MainSubScene).Value} with id {SystemAPI.GetComponent<PrefabID>(gameSceneDesc.MainSubScene).Value}");
                            var sceneLoaderEntity = commands.Instantiate(entityInQueryIndex, gameSceneDesc.MainSubScene);
                            commands.AddComponent(entityInQueryIndex, sceneLoaderEntity, new NetworkID());
                            commands.AddComponent(entityInQueryIndex, sceneLoaderEntity, ArenaNetworkChannelIds.SceneLoading);
            
                            commands.AppendToBuffer(entityInQueryIndex, entity,
                                new SceneAssetInstance 
                                {
                                    ID = SystemAPI.GetComponent<PrefabID>(gameSceneDesc.MainSubScene),
                                    Value = sceneLoaderEntity 
                                });
            
                            var sectionStates = commands.AddBuffer<SceneSectionState>(entityInQueryIndex, sceneLoaderEntity);
                            foreach(var section in sectionsLoLoad)
                            {
                                sectionStates.Add(new SceneSectionState
                                {
                                    SectionIndex = section.SectionIndex,
                                    ShouldBeLoaded = true
                                });
                            }
                        }
                        break;
            
                    case SceneLoadingStates.Loading:
                    {
                        if (sceneAssetInstances.IsEmpty)
                        {
                            return;
                        }
                        
                        // wait for start scene loading
                        foreach (var loader in sceneAssetInstances)
                        {
                            if (SystemAPI.HasComponent<SceneLoadingStateData>(loader.Value) == false)
                            {
                                return;
                            }
            
                            var state = SystemAPI.GetComponent<SceneLoadingStateData>(loader.Value);
                            if (state.LoadingState != TzarGames.GameCore.SceneLoadingState.Loaded)
                            {
                                return;
                            }
                        }
            
                        var mainSceneID = SystemAPI.GetComponent<PrefabID>(gameSceneDesc.MainSubScene); 
                        Debug.Log($"Game scene {mainSceneID.Value} loading complete");
                        loadingState.Value = SceneLoadingStates.Running;
                    }
                    break;
                    
                    case SceneLoadingStates.Running:
                        break;
            
                    case SceneLoadingStates.PendingStartUnloading:
                        {
                            foreach (var loader in sceneAssetInstances)
                            {
                                Debug.Log($"Requesting unload scene asset with scene asset entity {loader.Value.Index}");
                                var unloadRequest = commands.CreateEntity(entityInQueryIndex);
                                commands.AddComponent(entityInQueryIndex, unloadRequest, new RequestSceneAssetUnload { SceneAssetEntity = loader.Value });
                            }
            
                            Debug.Log($"Start unloading game scene desc entity {entity.Index}");
                            loadingState.Value = SceneLoadingStates.Unloading;
                        }
                        break;
            
                    case SceneLoadingStates.Unloading:
                        {
                            if (sceneAssetInstances.IsEmpty)
                            {
                                return;
                            }
            
                            foreach (var loader in sceneAssetInstances)
                            {
                                if (SystemAPI.HasComponent<SceneLoadingStateData>(loader.Value) == false)
                                {
                                    continue;
                                }
            
                                var state = SystemAPI.GetComponent<SceneLoadingStateData>(loader.Value);
                                if (state.LoadingState != TzarGames.GameCore.SceneLoadingState.Unloaded)
                                {
                                    return;
                                }
                            }
                            Debug.Log($"Game scene desc {entity.Index} unloaded");
                            loadingState.Value = SceneLoadingStates.Unloaded;
                        }
                        break;
                }
            }).Run();

            if (SystemAPI.TryGetSingletonEntity<SceneData>(out Entity sceneDataEntity))
            {
                // работа с зонами загрузки
                Entities
                    .WithChangeFilter<SessionEntityReference>()
                    //.WithoutBurst()
                    .ForEach((ref SessionEntityReference matchEntityReference) =>
                    {
                        if (matchEntityReference.Value == Entity.Null)
                        {
                            matchEntityReference.Value = sceneDataEntity;
#if UNITY_EDITOR
                            Debug.Log($"Initializing match entity {matchEntityReference.Value.Index}");
#endif
                        }
                    
                    }).Run();    
            }
            
            // загрузка секций
            Entities
                .WithoutBurst()
                .ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer <SceneSectionState> sectionStates, in SceneLoadingStateData sceneLoadingState) =>
                {
                    if(sceneLoadingState.SceneEntity == Entity.Null)
                    {
                        return;
                    }
            
                    if(SystemAPI.HasBuffer<ResolvedSectionEntity>(sceneLoadingState.SceneEntity) == false)
                    {
                        return;
                    }
            
                    DynamicBuffer<ResolvedSectionEntity> sectionEntities = default;
            
                    for(int i=0; i<sectionStates.Length; i++)
                    {
                        var sectionState = sectionStates[i];
            
                        if(sectionState.SectionEntity == Entity.Null)
                        {
                            if(sectionEntities.IsCreated == false)
                            {
                                sectionEntities = SystemAPI.GetBuffer<ResolvedSectionEntity>(sceneLoadingState.SceneEntity);
                            }
            
                            foreach(var sectionEntity in sectionEntities)
                            {
                                if(SystemAPI.HasComponent<SceneSectionData>(sectionEntity.SectionEntity) == false)
                                {
                                    continue;
                                }
                                var sectionData = SystemAPI.GetComponent<SceneSectionData>(sectionEntity.SectionEntity);
                                if(sectionData.SubSectionIndex == sectionState.SectionIndex)
                                {
                                    sectionState.SectionEntity = sectionEntity.SectionEntity;
                                    sectionStates[i] = sectionState;
                                    break;
                                }
                            }
                        }
            
                        if(sectionState.SectionEntity == Entity.Null)
                        {
                            continue;
                        }
            
                        var isSectionLoaded = SceneSystem.IsSectionLoaded(World.Unmanaged, sectionState.SectionEntity);
            
                        if (sectionState.ShouldBeLoaded)
                        {
                            if(isSectionLoaded)
                            {
                                if(sectionState.State == SceneSectionLoadState.Unloaded || sectionState.State == SceneSectionLoadState.Loading)
                                {
                                    Debug.Log($"Scene section {sectionState.SectionIndex} is loaded for scene {sceneLoadingState.SceneEntity.Index}, scene loader entity: {entity.Index}");
                                    sectionState.State = SceneSectionLoadState.Loaded;
                                    sectionStates[i] = sectionState;
                                }
                            }
                            else
                            {
                                if(sectionState.State == SceneSectionLoadState.Unloaded)
                                {
                                    // загружаем секции только если сцена загружается частично
                                    if(sceneLoadingState.AllowPartialLoad)
                                    {
                                        commands.AddComponent<RequestSceneLoaded>(entityInQueryIndex, sectionState.SectionEntity);
                                        Debug.Log($"Start loading scene section {sectionState.SectionIndex} (E: {sectionState.SectionEntity.Index}) for scene {sceneLoadingState.SceneEntity.Index}, scene loader entity: {entity.Index}");
                                        sectionState.State = SceneSectionLoadState.Loading;
                                        sectionStates[i] = sectionState;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if(isSectionLoaded)
                            {
                                commands.RemoveComponent<RequestSceneLoaded>(entityInQueryIndex, sectionState.SectionEntity);
                                Debug.Log($"Start unloading scene section {sectionState.SectionIndex} (E: {sectionState.SectionEntity.Index}) for scene {sceneLoadingState.SceneEntity}");
                                sectionState.State = SceneSectionLoadState.Unloaded;
                                sectionStates[i] = sectionState;
                            }
                            else
                            {
                                if(sectionState.State != SceneSectionLoadState.Unloaded)
                                {
                                    sectionState.State = SceneSectionLoadState.Unloaded;
                                    sectionStates[i] = sectionState;
                                }
                            }
                        }
                    }
            
                    //if (sectionData.SubSectionIndex == 0)
                    //{
                    //    Debug.Log($"Auto loading scene section {sectionData.SubSectionIndex} of entity {entity.Index}");
                    //    SystemAPI.AddComponentData(entity, new SceneSectionLoadingTag());
                    //    var request = new RequestSceneLoaded
                    //    {
                    //        //LoadFlags = SceneLoadFlags.BlockOnImport
                    //    };
                    //    SystemAPI.AddComponentData(entity, request);
                    //}
                }).Run();
            
             var currentTime = World.Time.ElapsedTime;

             var sceneAssetInstanceBuffers = GetBufferLookup<SceneAssetInstance>(true);

            // зоны перехода
            Entities
                .WithoutBurst()
                .WithAll<SessionEntityReference>()
                .ForEach((
                    Entity entity,
                    DynamicBuffer<OverlappingEntities> overlappingEntities,
                    DynamicBuffer<TransitionSectionToLoad> sectionsToLoad,
                    DynamicBuffer<TransitionSectionToUnload> sectionsToUnload,
                    DynamicBuffer<TransitionZoneEnableObject> objectsToEnable,
                    DynamicBuffer<TransitionZoneDisableObject> objectsToDisable,
                    ref TransitionZone transitionZone,
                    in SceneTag sceneTag) =>
            {
                if (transitionZone.State == TransitionZoneState.Activated)
                {
                    return;
                }
                
                var matchEntityReference = SystemAPI.GetComponent<SessionEntityReference>(entity);
                
                if (matchEntityReference.Value == Entity.Null)
                {
                    return;
                }
                
                if(transitionZone.State == TransitionZoneState.Transition)
                {
                    if(currentTime - transitionZone.TransitionStartTime < transitionZone.ActivationTime)
                    {
                        return;
                    }
                    transitionZone.State = TransitionZoneState.Activated;
                
                    var sceneEntityReference = SystemAPI.GetComponent<SceneEntityReference>(sceneTag.SceneEntity);
                
                    if (SystemAPI.HasComponent<SceneSectionData>(sceneEntityReference.SceneEntity))
                    {
                        sceneEntityReference = SystemAPI.GetComponent<SceneEntityReference>(sceneEntityReference.SceneEntity);
                    }
                    
                    var matchData = SystemAPI.GetComponent<SceneData>(matchEntityReference.Value);
                    var sceneAssetInstances = sceneAssetInstanceBuffers[matchData.GameSceneInstance];
                    
                    Entity sceneAssetEntity = Entity.Null;
                    
                    foreach(var sceneAssetInstance in sceneAssetInstances)
                    {
                        var sceneLoadingState = SystemAPI.GetComponent<SceneLoadingStateData>(sceneAssetInstance.Value);
                        if(sceneLoadingState.SceneEntity == sceneEntityReference.SceneEntity)
                        {
                            sceneAssetEntity = sceneAssetInstance.Value;
                            break;
                        }
                    }
                    
                    if(sceneAssetEntity == Entity.Null)
                    {
                        Debug.LogError($"Scene asset entity not found for transition zone {entity.Index}");
                        return;
                    }
                    
                    var sceneSectionStates = SystemAPI.GetBuffer<SceneSectionState>(sceneAssetEntity);
                    
                    foreach (var section in sectionsToLoad)
                    {
                        int stateIndex = -1;
                    
                        for (int i = 0; i < sceneSectionStates.Length; i++)
                        {
                            var sectionState = sceneSectionStates[i];
                    
                            if (sectionState.SectionIndex == section.SectionIndex)
                            {
                                stateIndex = i;
                                break;
                            }
                        }
                    
                        if (stateIndex == -1)
                        {
                            var state = new SceneSectionState
                            {
                                SectionIndex = section.SectionIndex,
                                ShouldBeLoaded = true,
                                State = SceneSectionLoadState.Unloaded,
                            };
                            sceneSectionStates.Add(state);
                        }
                        else
                        {
                            var state = sceneSectionStates[stateIndex];
                            if (state.ShouldBeLoaded == false)
                            {
                                state.ShouldBeLoaded = true;
                                sceneSectionStates[stateIndex] = state;
                            }
                        }
                    }
                    
                    foreach (var section in sectionsToUnload)
                    {
                        for (int i = 0; i < sceneSectionStates.Length; i++)
                        {
                            var sectionState = sceneSectionStates[i];
                    
                            if (sectionState.SectionIndex == section.SectionIndex)
                            {
                                if (sectionState.ShouldBeLoaded)
                                {
                                    sectionState.ShouldBeLoaded = false;
                                    sceneSectionStates[i] = sectionState;
                                }
                            }
                        }
                    }
                    
                    foreach (var obj in objectsToDisable)
                    {
                        commands.AddComponent<Disabled>(0, obj.Entity);
                    }
                    
                    if (SystemAPI.HasComponent<TransitionFinishedMessage>(entity))
                    {
                        var startMsg = SystemAPI.GetComponent<TransitionFinishedMessage>(entity);
                        var msgEntity = commands.CreateEntity(0);
                        commands.AddComponent(0, msgEntity, startMsg.Message);
                        commands.AddComponent(0, msgEntity, new MessageNetSyncTag());
                    }
                    return;
                }
                
                bool isAllPlayersInZone = true;
                
                // TODO make isReadonly = true
                var players = SystemAPI.GetBuffer<RegisteredPlayer>(matchEntityReference.Value);
                
                foreach (var player in players)
                {
                    if (SystemAPI.HasComponent<ControlledCharacter>(player.PlayerEntity) == false)
                    {
                        isAllPlayersInZone = false;
                        break;
                    }
                
                    var controllerCharacter = SystemAPI.GetComponent<ControlledCharacter>(player.PlayerEntity).Entity;
                    bool isInZone = false;
                
                    foreach (var overlappingEntity in overlappingEntities)
                    {
                        if (overlappingEntity.Entity == controllerCharacter)
                        {
                            isInZone = true;
                            break;
                        }
                    }
                
                    if (isInZone == false)
                    {
                        isAllPlayersInZone = false;
                        break;
                    }
                }
                
                if (isAllPlayersInZone == false)
                {
                    return;
                }
                
                Debug.Log("All players are within transition zone. Start transition");
                transitionZone.State = TransitionZoneState.Transition;
                transitionZone.TransitionStartTime = currentTime;
                
                // objects
                foreach (var obj in objectsToEnable)
                {
                    commands.RemoveComponent<Disabled>(0, obj.Entity);
                }
                
                if (SystemAPI.HasComponent<TransitionStartMessage>(entity))
                {
                    var startMsg = SystemAPI.GetComponent<TransitionStartMessage>(entity);
                    var msgEntity = commands.CreateEntity(0);
                    commands.AddComponent(0, msgEntity, startMsg.Message);
                    commands.AddComponent(0, msgEntity, new MessageNetSyncTag());
                }
            
            }).Run();
        }
    }
}
