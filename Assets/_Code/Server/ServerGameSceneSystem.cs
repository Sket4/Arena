using Arena.Server;
using TzarGames.GameCore;
using TzarGames.GameCore.Server;
using TzarGames.MatchFramework.Server;
using Unity.Entities;

namespace Arena.GameSceneCode.Server
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(GameSceneSystem))]
    [UpdateBefore(typeof(ArenaMatchSystem))]
    public partial class ServerGameSceneSystem : GameSystemBase
    {
        protected override void OnSystemUpdate()
        {
            var playerSceneStateBuffers = GetBufferLookup<PlayerSceneLoadingState>(true);

            Entities
                .WithReadOnly(playerSceneStateBuffers)
                .ForEach(( DynamicBuffer<SceneAssetInstance> sceneLoaders, ref SceneLoadingState gameSceneState, in SessionEntityReference matchEntityReference, in GameSceneDescription gameSceneDesc) =>
            {
                switch (gameSceneState.Value)
                {
                    case SceneLoadingStates.PendingStartLoading:
                        break;
                    case SceneLoadingStates.Loading:
                        break;
                    case SceneLoadingStates.Running:
                        if (gameSceneDesc.ShouldWaitForLoadOnClient == false)
                        {
                            break;
                        }
                        
                        // если хотя бы одна загруженнная на сервере сцена не загружена на клиенте - возвращаемся в состояние Loading
                        
                        foreach (var sceneLoader in sceneLoaders)
                        {
                            if (HasComponent<SceneLoadingStateData>(sceneLoader.Value) == false)
                            {
                                gameSceneState.Value = SceneLoadingStates.Loading;
                                return;
                            }

                            var sceneId = GetComponent<PrefabID>(sceneLoader.Value);
                            var sceneLoadingState = GetComponent<SceneLoadingStateData>(sceneLoader.Value);

                            if (sceneLoadingState.LoadingState != TzarGames.GameCore.SceneLoadingState.Loaded)
                            {
                                gameSceneState.Value = SceneLoadingStates.Loading;
                                UnityEngine.Debug.LogError("not loaded");
                                continue;
                            }

                            var players = GetBuffer<RegisteredPlayer>(matchEntityReference.Value);
                            
                            foreach (var player in players)
                            {
                                if (playerSceneStateBuffers.HasComponent(player.PlayerEntity) == false)
                                {
                                    gameSceneState.Value = SceneLoadingStates.Loading;
                                    return;
                                }

                                var playerSceneStates = playerSceneStateBuffers[player.PlayerEntity];
                                bool playerHasSceneState = false;
                                
                                foreach (var playerSceneState in playerSceneStates)
                                {
                                    if (playerSceneState.SceneID.Value != sceneId.Value)
                                    {
                                        continue;
                                    }
                                    
                                    if (playerSceneState.IsLoaded == false)
                                    {
                                        gameSceneState.Value = SceneLoadingStates.Loading;
                                        return;
                                    }
                                    else
                                    {
                                        playerHasSceneState = true;
                                        break;
                                    }
                                }  

                                if(playerHasSceneState == false)
                                {
                                    gameSceneState.Value = SceneLoadingStates.Loading;
                                    return;
                                }
                            }
                        }
                        break;

                    case SceneLoadingStates.Unloaded:
                        
                        if (gameSceneDesc.ShouldWaitForLoadOnClient == false)
                        {
                            break;
                        }
                        
                        // если хотя бы одна выгруженная на сервере сцена загружена на клиенте - возвращаемся в состояние Unloading

                        foreach (var sceneLoader in sceneLoaders)
                        {
                            if(HasComponent<SceneLoadingStateData>(sceneLoader.Value))
                            {
                                var sceneLoadingState = GetComponent<SceneLoadingStateData>(sceneLoader.Value);

                                if (sceneLoadingState.LoadingState != TzarGames.GameCore.SceneLoadingState.Unloaded)
                                {
                                    gameSceneState.Value = SceneLoadingStates.Unloading;
                                    return;
                                }
                            }
                            
                            var players = GetBuffer<RegisteredPlayer>(matchEntityReference.Value);

                            foreach (var player in players)
                            {
                                var playerSceneStates = playerSceneStateBuffers[player.PlayerEntity];

                                foreach (var playerSceneState in playerSceneStates)
                                {
                                    if (playerSceneState.SceneID.Value != sceneLoader.ID.Value)
                                    {
                                        continue;
                                    }

                                    if (playerSceneState.IsLoaded)
                                    {
                                        gameSceneState.Value = SceneLoadingStates.Unloading;
                                        return;
                                    }
                                }
                            }
                        }
                        break;
                }
                
            }).Run();
        }
    }
}
