using Arena.GameSceneCode;
using TzarGames.GameCore;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Server;
using TzarGames.MultiplayerKit;
using TzarGames.MultiplayerKit.Server;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Arena.Server
{
    [Sync]
    public struct SafeZoneSyncData : IComponentData
    {
        private byte fakeData;
    }
    
    [DisableAutoCreation]
    [UpdateAfter(typeof(InventorySystem))]
    [UpdateBefore(typeof(GameCommandBufferSystem))]
    public partial class SafeAreaMatchSystem : GameplayStateSystemBase
    {
        WaitingForNewPlayer waitingState;
        EntityQuery spawnPointsQuery;
        EntityQuery playersToSetupQuery;
        
        public event System.Action<PlayerId, GameSessionID> OnUserDisconnected;

        struct ProcessedBySystemTag : IComponentData
        {
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            waitingState = RegisterState<WaitingForNewPlayer>();
            spawnPointsQuery = ArenaMatchUtility.CreatePlayerSpawnPointsQuery(EntityManager);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            spawnPointsQuery.Dispose();
        }

        protected override void OnAuthorizedPlayerDisconnected(PlayerId playerId, GameSessionID gameSessionID)
        {
            base.OnAuthorizedPlayerDisconnected(playerId, gameSessionID);
            OnUserDisconnected?.Invoke(playerId, gameSessionID);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            var commands = Commands;

            Entities
                .WithChangeFilter<TzarGames.MultiplayerKit.NetworkPlayer>()
                .WithNone<AuthorizedUser>()
                .WithAll<TzarGames.MultiplayerKit.NetworkPlayer>()
                .WithoutBurst()
                .ForEach((Entity entity) =>
                {
                    Debug.Log($"Turning off net channels for player entity {entity.Index}");
                    bool hasBuffer = EntityManager.HasComponent<PlayerNetworkChannelState>(entity);
                    ArenaMatchUtility.TurnOffNetChannelsForPlayer(entity, ref commands, hasBuffer);

                }).Run();

            // обработка запросов на загрузку данных игрока
            Entities
                .WithoutBurst()
                .ForEach((Entity entity, ref PlayerDataLoadRequest request) => 
            {
                if(request.State == PlayerDataRequestState.Pending || request.State == PlayerDataRequestState.Running)
                {
                    return;
                }

                commands.RemoveComponent<PlayerDataLoadRequest>(entity);

                if(request.State == PlayerDataRequestState.Failed)
                {
                    commands.DestroyEntity(entity);
                    if(HasComponent<TzarGames.MultiplayerKit.NetworkPlayer>(request.Player))
                    {
                        var player = GetComponent<TzarGames.MultiplayerKit.NetworkPlayer>(request.Player);
                        Debug.Log($"Failed to load data for player {player.ID}, disconnecting it");
                        commands.AddComponent(request.Player, new DisconnectRequest());
                    }
                    return;
                }

                commands.DestroyEntity(entity);
                
                var playerData = EntityManager.GetComponentObject<PlayerData>(entity);
                commands.AddComponent(request.Player, playerData);

            }).Run();

            // создание персонажей для игроков
            if(playersToSetupQuery.CalculateEntityCount() > 0 && HasSingleton<PlayerPrefab>() && spawnPointsQuery.CalculateEntityCount() > 0)
            {
                var playerPrefab = GetSingleton<PlayerPrefab>();
                var playerSpawnPoints = spawnPointsQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                
                var matchEntity = GetSingletonEntity<SessionInitializationData>();

                Entities
                    .WithStoreEntityQueryInField(ref playersToSetupQuery)
                    .WithoutBurst()
                    .WithNone<ControlledCharacter>()

                    .ForEach((Entity playerEntity, PlayerData playerData, in TzarGames.MultiplayerKit.NetworkPlayer networkPlayer) =>
                    {
                        var matchInitData = EntityManager.GetComponentData<SessionInitializationData>(matchEntity);
                        
                        if (matchInitData.IsLocalGame == false)
                        {
                            if (EntityManager.HasComponent<PlayerSceneLoadingState>(playerEntity) == false)
                            {
                                return;
                            }
                            
                            var sceneLoadingStates = EntityManager.GetBuffer<PlayerSceneLoadingState>(playerEntity);

                            if (ArenaMatchUtility.IsGameSceneLoadedOnPlayer(EntityManager, matchEntity,
                                    sceneLoadingStates) == false)
                            {
                                return;
                            }
                        } 
                        
                        var pspEntity = playerSpawnPoints[0];
                        var spl2w = GetComponent<LocalToWorld>(pspEntity);
                        var position = spl2w.Position;
                        var rotation = spl2w.Rotation;
                        
                        Debug.Log($"Creating a character for player {networkPlayer.ID}");
                        ArenaMatchUtility.SetupPlayerCharacter(waitingState, matchInitData.IsLocalGame, playerPrefab.Value, position, rotation, playerEntity, networkPlayer, ref commands);

                    }).Run();

                playerSpawnPoints.Dispose();
            }
            
            // сохранение данных игроков
            Entities
                .WithoutBurst()
                .WithNone<InitialPlayerDataLoad>()
                .ForEach((Entity transactionEntity, in InventoryTransaction invTransaction, in Target target) =>
            {
                if (invTransaction.Status != InventoryTransactionStatus.Success)
                {
                    return;
                }

                if (createSaveDataRequest(commands, target.Value) == false)
                {
                    return;
                }
                
                if (SystemAPI.HasComponent<AutoDestroyItemTransaction>(transactionEntity) == false
                    && SystemAPI.HasComponent<ProcessedBySystemTag>(transactionEntity) == false
                   )
                {
                    commands.AddComponent<ProcessedBySystemTag>(transactionEntity);    
                }
                
                
            }).Run();

            foreach (var eventData in SystemAPI.Query<RefRO<AbilityPointChangedEvent>>())
            {
                createSaveDataRequest(commands, eventData.ValueRO.Character);
            }
            
            foreach (var eventData in SystemAPI.Query<RefRO<AbilityActivatedEvent>>())
            {
                createSaveDataRequest(commands, eventData.ValueRO.Character);
            }
        }

        bool createSaveDataRequest(EntityCommandBuffer commands, Entity targetCharacter)
        {
            if (SystemAPI.HasComponent<PlayerController>(targetCharacter) == false)
            {
                Debug.Log($"Failed to save player data - character {targetCharacter.Index} has no player controller");
                return false;
            }
            var playerEntity = SystemAPI.GetComponent<PlayerController>(targetCharacter).Value;
            
            if (SystemAPI.HasComponent<AuthorizedUser>(playerEntity) == false)
            {
                Debug.Log($"Failed to save player data - player {playerEntity.Index} is not authorized");
                return false;
            }
            
            Debug.Log($"Saving data for player character {targetCharacter.Index}");
            var playerId = SystemAPI.GetComponent<AuthorizedUser>(playerEntity).Value;
            
            var requestEntity = commands.CreateEntity();
                
            commands.AddComponent(requestEntity, new PlayerDataSaveRequest
            {
                Owner = Entity.Null,
                PlayerId = playerId,
                State = PlayerDataRequestState.Pending
            });
            commands.AddComponent(requestEntity, new Target { Value = playerEntity });
            commands.AddComponent(requestEntity, new PlayerSaveData
            {
                CharacterEntity = targetCharacter
            });
            return true;
        }

        protected class WaitingForNewPlayer : WaitingForPlayers
        {
            public override void OnEnter(Entity entity)
            {
                base.OnEnter(entity);
                
                ArenaMatchUtility.SetupGameSceneForGameSessionEntity(this, entity, Commands);
                
                if (HasComponent<NetworkID>(entity) == false)
                {
                    Commands.AddComponent(entity, new NetworkID());
                }
                
                Commands.AddComponent<SafeZoneSyncData>(entity);
            }

            protected override void OnPlayerAuthorized(Entity playerEntity, PlayerId userId)
            {
                base.OnPlayerAuthorized(playerEntity, userId);

                // создаем запрос на загрузку данных для игрока
                Debug.Log($"Request loading data for player {userId.Value}");
                var request = Commands.CreateEntity();
                Commands.AddComponent(request, new PlayerDataLoadRequest { Player = playerEntity });
            }
        }
    }
}
