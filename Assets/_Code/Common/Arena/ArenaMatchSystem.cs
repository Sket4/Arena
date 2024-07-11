using System.Data;
using Arena.GameSceneCode;
using TzarGames.GameCore;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Server;
using TzarGames.MultiplayerKit;
using TzarGames.MultiplayerKit.Server;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using NetworkPlayer = TzarGames.MultiplayerKit.NetworkPlayer;
using SceneLoadingState = Arena.GameSceneCode.SceneLoadingState;

namespace Arena.Server
{
    public static class ArenaNetworkChannelIds
    {
        public static readonly NetworkChannelId SceneLoading = new NetworkChannelId(1);
    }

    public struct PlayerSaveData : IComponentData
    {
        public Entity CharacterEntity;
    }

    [BurstCompile]
    [WithNone(typeof(AuthorizedUser))]
    public partial struct UpdatePlayerNetChannelJob : IJobEntity
    {
        public BufferLookup<PlayerNetworkChannelState> ChannelStateLookup;
        public EntityCommandBuffer Commands;

        public void Execute(Entity entity, in NetworkPlayer player)
        {
            DynamicBuffer<PlayerNetworkChannelState> channelStates;

            if (ChannelStateLookup.TryGetBuffer(entity, out channelStates) == false)
            {
                channelStates = Commands.AddBuffer<PlayerNetworkChannelState>(entity);
            }

            if (channelStates.Length == 0)
            {
                Debug.Log($"Adding scene netchannel for player {player.ID}");
                channelStates.Add(new PlayerNetworkChannelState
                {
                    ChannelId = ArenaNetworkChannelIds.SceneLoading,
                    Enabled = true
                });
            }
            else
            {
                for (var i = 0; i < channelStates.Length; i++)
                {
                    var channelState = channelStates[i];
                    if (channelState.ChannelId.Value != ArenaNetworkChannelIds.SceneLoading.Value)
                    {
                        Debug.Log($"Disabling netchannel {channelState.ChannelId.Value} for player {player.ID}");
                        channelState.Enabled = false;
                        channelStates[i] = channelState;
                    }
                }
            }
        }
    }

    [DisableAutoCreation]
    [UpdateAfter(typeof(GameSceneSystem))]
    [UpdateBefore(typeof(MessageDispatherCleanSystem))]
    public partial class ArenaMatchSystem : MatchBaseSystem, IServerArenaCommands, IRpcProcessor
    {
        public static readonly Message MatchFinishedMessage = Message.CreateFromString("match_finished");
        public event System.Action OnReadyToShutdown;

        private EntityQuery spawnPointsQuery;
        EntityQuery saveRequestQuery;
        NativeArray<PlayerDataSaveRequest> collectedSaveRequests;
        NativeArray<Entity> collectedSaveRequestEntities;

        // сущности игроков, которые запросили продолжение матча
        NativeList<Entity> continueGameRequestedPlayers;

        // игроки, которые оповестили о выходе из матча
        NativeList<Entity> exitFromGameNotifiedPlayers;

        public NetworkIdentity NetIdentity { get; set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            
            RegisterState<RunningStateEx>();
            RegisterState<Arena_WaitingForPlayersState>();
            RegisterState<LoadingNextLevelState>();
            RegisterState<UnloadingCurrentLevelState>();
            RegisterState<WaitingForNextStepState>();
            RegisterState<SavingDataState>();
            RegisterState<FinishedState>();

            saveRequestQuery = GetEntityQuery(typeof(PlayerDataSaveRequest));
            continueGameRequestedPlayers = new NativeList<Entity>(Allocator.Persistent);
            exitFromGameNotifiedPlayers = new NativeList<Entity>(Allocator.Persistent);
            spawnPointsQuery = ArenaMatchUtility.CreatePlayerSpawnPointsQuery(EntityManager);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            spawnPointsQuery.Dispose();
            continueGameRequestedPlayers.Dispose();
            exitFromGameNotifiedPlayers.Dispose();
        }

        protected double GetNetworkTime()
        {
            if (SystemAPI.TryGetSingleton(out NetworkTime networkTime))
            {
                return networkTime.Value;
            }
            return 0;
        }

        void updatePlayers()
        {
            var commands = Commands;

            var channelStateBuffers = GetBufferLookup<PlayerNetworkChannelState>();

            var updateChannelStatesJob = new UpdatePlayerNetChannelJob
            {
                ChannelStateLookup = channelStateBuffers,
                Commands = commands
            };
            
            updateChannelStatesJob.Run();
        }

        protected override void OnUpdate()
        {
            InitilizeCommandsEarly();
            updatePlayers();
           
            var finishMessage = MatchFinishedMessage;

            Entities
                .WithChangeFilter<IncomingMessages>()
                .ForEach((Entity entity, DynamicBuffer<IncomingMessages> incomingMessages, ref ArenaMatchStateData stateData) =>
            {
                //Debug.Log($"changed {entity.Index}");
                if(IncomingMessages.Contains(incomingMessages, finishMessage))
                {
                    Debug.Log("Match entity received a finished event");
                    if(stateData.State == ArenaMatchState.Fighting)
                    {
                        stateData.IsMatchComplete = true;
                    }
                }

            }).Run();

            if(collectedSaveRequests.IsCreated)
            {
                collectedSaveRequests.Dispose();
            }
            if(collectedSaveRequestEntities.IsCreated)
            {
                collectedSaveRequestEntities.Dispose();
            }

            collectedSaveRequests = saveRequestQuery.ToComponentDataArray<PlayerDataSaveRequest>(Allocator.TempJob);
            collectedSaveRequestEntities = saveRequestQuery.ToEntityArray(Allocator.TempJob);

            base.OnUpdate();

            collectedSaveRequests.Dispose();
            collectedSaveRequestEntities.Dispose();
        }

        public void RequestContinueGame(NetMessageInfo info)
        {
            if(continueGameRequestedPlayers.Contains(info.SenderEntity) == false)
            {
                continueGameRequestedPlayers.Add(info.SenderEntity);
            }
        }

        public void NotifyExitingFromGame(NetMessageInfo info)
        {
            if(exitFromGameNotifiedPlayers.Contains(info.SenderEntity) == false)
            {
                exitFromGameNotifiedPlayers.Add(info.SenderEntity);
            }
        }

        void getSpawnPositionForPlayer(NativeArray<Entity> spawnPoints, int spawnPointId, out float3 position, out float cameraWorldYaw)
        {
            Entity spawnPointEntity = Entity.Null;

            foreach (var spawnPoint in spawnPoints)
            {
                var idData = EntityManager.GetComponentData<SpawnPointIdData>(spawnPoint);
                if (idData.ID == spawnPointId)
                {
                    spawnPointEntity = spawnPoint;
                    break;
                }
            }

            if (spawnPointEntity == Entity.Null)
            {
                spawnPointEntity = spawnPoints[0];    
            }
            
            cameraWorldYaw = EntityManager.GetComponentData<PlayerSpawnPoint>(spawnPointEntity).WorldViewRotation;
            position = EntityManager.GetComponentData<LocalToWorld>(spawnPointEntity).Position;
        }

        protected class Arena_WaitingForPlayersState : MatchSystem_WaitingForPlayers
        {
            public override void OnEnter(Entity entity)
            {
                ArenaMatchUtility.SetupGameSceneForGameSessionEntity(this, entity, Commands);

                base.OnEnter(entity);
            }

            protected override void OnPlayerAuthorized(Entity playerEntity, PlayerId userId)
            {
                base.OnPlayerAuthorized(playerEntity, userId);
                ArenaMatchUtility.TurnOffNetChannelsForPlayer(this, playerEntity, ref Commands);
            }

            protected override bool IsReadyToStartMatch(Entity entity, DynamicBuffer<RegisteredPlayer> players)
            {
                if(ArenaMatchUtility.IsGameSceneLoaded(this, entity) == false)
                {
                    return false;
                }
                
                return base.IsReadyToStartMatch(entity, players);
            }
        }

        struct PlayerToSave : IBufferElementData
        {
            public RegisteredPlayer Value;
            public Entity PlayerCharacter;
        }

        class WaitingForNextStepState : BaseState
        {
            public override void OnEnter(Entity entity)
            {
                base.OnEnter(entity);
                
                var internalState = GetComponent<ArenaMatchStateData>(entity);

                if (internalState.State != ArenaMatchState.WaitingForNextStep)
                {
                    internalState.State = ArenaMatchState.WaitingForNextStep;
                    SetComponent(entity, internalState);
                }
                
                var players = EntityManager.GetBuffer<RegisteredPlayer>(entity);
                DynamicBuffer<PlayerToSave> playersToSave;

                if (HasBuffer<PlayerToSave>(entity))
                {
                    playersToSave = Commands.SetBuffer<PlayerToSave>(entity);
                }
                else
                {
                    playersToSave = Commands.AddBuffer<PlayerToSave>(entity);
                }

                foreach (var player in players)
                {
                    if (HasComponent<ControlledCharacter>(player.PlayerEntity) == false)
                    {
                        continue;
                    }

                    var controllerCharacter = GetComponent<ControlledCharacter>(player.PlayerEntity);

                    if (Exists(controllerCharacter.Entity) == false)
                    {
                        continue;
                    }
                    
                    if (HasComponent<DisableAutoDestroy>(controllerCharacter.Entity) == false)
                    {
                        Commands.AddComponent<DisableAutoDestroy>(controllerCharacter.Entity);
                    }
                    playersToSave.Add(new PlayerToSave 
                    { 
                        Value = player,
                        PlayerCharacter = controllerCharacter.Entity,
                    });
                }
            }

            public override void OnUpdate(Entity entity)
            {
                base.OnUpdate(entity);
                
                var matchSystem = System as ArenaMatchSystem;

                var currentTime = matchSystem.TimeSystem.GameTime;
                var continueRequests = matchSystem.continueGameRequestedPlayers;
                var exitRequests = matchSystem.exitFromGameNotifiedPlayers;

                bool matchShouldBeFinished = false;
                int playersWantToContinueCount = 0;

                var internalState = GetComponent<ArenaMatchStateData>(entity);

                // даем немного больше времени, чтобы клиент успел отключиться до дисконнекта от сервера
                const float additionalWaitTimeSeconds = 2;

                if (currentTime - internalState.MatchEndTime >= internalState.DecisionWaitTime + additionalWaitTimeSeconds)
                {
                    // too many time for player decision
                    Debug.Log("Too many time elapsed while waiting for player decision");
                    matchShouldBeFinished = true;
                }
                else
                {
                    bool isAnyPlayerNotDecided = false;
                    var players = EntityManager.GetBuffer<RegisteredPlayer>(entity);
                    
                    foreach (var player in players)
                    {
                        bool hasDecision = false;

                        if (Exists(player.PlayerEntity) == false)
                        {
                            continue;
                        }

                        foreach(var request in continueRequests)
                        {
                            if(request == player.PlayerEntity)
                            {
                                hasDecision = true;
                                playersWantToContinueCount++;
                                break;
                            }
                        }

                        if(hasDecision == false)
                        {
                            foreach (var request in exitRequests)
                            {
                                if (request == player.PlayerEntity)
                                {
                                    hasDecision = true;
                                    break;
                                }
                            }
                        }

                        if(hasDecision == false)
                        {
                            isAnyPlayerNotDecided = true;
                            break;
                        }
                    }    

                    if(isAnyPlayerNotDecided == false)
                    {
                        Debug.Log("All players made their decisions, now finished the match");
                        matchShouldBeFinished = true;
                    }
                }

                if(matchShouldBeFinished)
                {
                    Debug.Log("Match finished");
                    
                    continueRequests.Clear();
                    exitRequests.Clear();

                    if (internalState.IsMatchFailed)
                    {
                        if (playersWantToContinueCount > 0)
                        {
                            RequestStateChange<UnloadingCurrentLevelState>(entity);    
                        }
                        else
                        {
                            RequestStateChange<FailedState>(entity);    
                        }
                    }
                    else
                    {
                        if (playersWantToContinueCount == 0)
                        {
                            internalState.IsNextSceneAvailable = false;
                        }
                        RequestStateChange<SavingDataState>(entity);
                    }
                    
                    internalState.State = ArenaMatchState.Finished;
                    SetComponent(entity, internalState);
                }
            }
        }

        class SavingDataState : BaseState
        {
            public override void OnEnter(Entity entity)
            {
                base.OnEnter(entity);

                Debug.Log("Entered to save game state");

                var em = EntityManager;
                var internalState = em.GetComponentData<ArenaMatchStateData>(entity);
                var players = em.GetBuffer<PlayerToSave>(entity);
                var commands = Commands;

                for (int i = 0; i < players.Length; i++)
                {
                    var playerToSave = players[i];
                    var player = playerToSave.Value;

                    var gameProgressEntity = em.GetComponentData<CharacterGameProgressReference>(playerToSave.PlayerCharacter).Value;
                    var gameProgress = em.GetComponentData<CharacterGameProgress>(gameProgressEntity);
                    gameProgress.CurrentStage = internalState.CurrentStage;
                    commands.SetComponent(gameProgressEntity, gameProgress);

                    var requestEntity = commands.CreateEntity();
                    commands.AddComponent(requestEntity, new PlayerDataSaveRequest
                    {
                        Owner = entity,
                        PlayerId = player.ID,
                        State = PlayerDataRequestState.Pending
                    });
                    commands.AddComponent(requestEntity, new Target { Value = player.PlayerEntity });
                    commands.AddComponent(requestEntity, new PlayerSaveData
                    {
                        CharacterEntity = playerToSave.PlayerCharacter
                    });
                } 
            }

            public override void OnUpdate(Entity entity)
            {
                base.OnUpdate(entity);

                var matchSystem = System as ArenaMatchSystem;
                var requests = matchSystem.collectedSaveRequests;
                var requestEntities = matchSystem.collectedSaveRequestEntities;

                bool allSaved = true;

                for (int i = 0; i < requests.Length; i++)
                {
                    var request = requests[i];

                    if (request.Owner != entity)
                    {
                        continue;
                    }

                    if (request.State != PlayerDataRequestState.Success
                        && request.State != PlayerDataRequestState.Failed)
                    {
                        allSaved = false;
                    }
                }

                if (allSaved)
                {
                    var internalState = GetComponent<ArenaMatchStateData>(entity);
                    internalState.Saved = true;
                    SetComponent(entity, internalState);
                    
                    var commands = Commands;

                    for (int i = 0; i < requests.Length; i++)
                    {
                        var request = requests[i];

                        if (request.Owner != entity)
                        {
                            continue;
                        }
                        var requestEntity = requestEntities[i];
                        commands.DestroyEntity(requestEntity);
                    }

                    var matchData = GetComponent<ArenaMatchStateData>(entity);

                    if (matchData.IsNextSceneAvailable)
                    {
                        RequestStateChange<UnloadingCurrentLevelState>(entity);    
                    }
                    else
                    {
                        RequestStateChange<FinishedState>(entity);
                    }
                }
            }
        }

        class FinishedState : BaseState
        {
            public override void OnEnter(Entity entity)
            {
                base.OnEnter(entity);
                (System as ArenaMatchSystem).OnReadyToShutdown?.Invoke();
            }
        }

        class UnloadingCurrentLevelState : BaseState
        {
            public override void OnEnter(Entity entity)
            {
                base.OnEnter(entity);

                Debug.Log("Unloading current level");

                var players = GetBuffer<RegisteredPlayer>(entity);

                foreach (var player in players)
                {
                    ArenaMatchUtility.TurnOffNetChannelsForPlayer(this, player.PlayerEntity, ref Commands);
                    Commands.RemoveComponent<ControlledCharacter>(player.PlayerEntity);
                }

                var matchData = GetComponent<SceneData>(entity);
                var currentGameSceneEntity = matchData.GameSceneInstance;
                Commands.SetComponent(currentGameSceneEntity, new SceneLoadingState { Value = SceneLoadingStates.PendingStartUnloading });
            }

            public override void OnUpdate(Entity entity)
            {
                base.OnUpdate(entity);

                if(IsGameSceneUnloaded(entity))
                {
                    RequestStateChange<LoadingNextLevelState>(entity);
                }
            }

            protected bool IsGameSceneUnloaded(Entity matchEntity)
            {
                if (HasComponent<SceneData>(matchEntity) == false)
                {
                    return false;
                }

                var matchData = GetComponent<SceneData>(matchEntity);

                if (Exists(matchData.GameSceneInstance) == false)
                {
                    return false;
                }

                //if (HasComponent<SceneLoadingState>(matchData.GameSceneInstance) == false)
                //{
                //    return true;
                //}

                var sceneLoadingState = GetComponent<SceneLoadingState>(matchData.GameSceneInstance);

                if (sceneLoadingState.Value != SceneLoadingStates.Unloaded)
                {
                    return false;
                }

                return true;
            }
        }

        class LoadingNextLevelState : BaseState
        {
            public override void OnEnter(Entity entity)
            {
                base.OnEnter(entity);

                var matchData = GetComponent<SceneData>(entity);
                var currentGameSceneEntity = matchData.GameSceneInstance;
                Debug.Log($"Destroying game scene entity {currentGameSceneEntity.Index}");
                Commands.DestroyEntity(currentGameSceneEntity);

                // TODO
                //var sceneNodeRef = GetComponent<CampaignTools.GameSceneNodeEntityReference>(currentGameSceneEntity);
                //var sceneNodeType = GetComponent<CampaignTools.GameSceneNodeType>(sceneNodeRef.Value);

                var internalState = GetComponent<ArenaMatchStateData>(entity);
                matchData.GameSceneInstance = Entity.Null;
                SetComponent(entity, matchData);
                
                if (internalState.IsMatchFailed)
                {
                    Debug.Log($"Reloading current scene");
                    ArenaMatchUtility.SetupGameSceneForGameSessionEntity(this, entity, Commands);
                }
                else
                {
                    // TODO
                    
                    // if(sceneNodeType.Value == CampaignTools.SceneNodeTypes.End)
                    // {
                    //     // конечный узел кампании
                    //     Debug.LogError("not implemented");
                    //     return;
                    // }
                    // else
                    // {
                    //     var sceneNodeConnections = GetBuffer<CampaignTools.GameSceneNodeConnection>(sceneNodeRef.Value);
                    //     var nextGameSceneNode = sceneNodeConnections[0].Value;
                    //     var nextGameScenes = GetBuffer<CampaignTools.GameSceneAssetReference>(nextGameSceneNode);
                    //     var nextGameScene = nextGameScenes[0].Value;
                    //
                    //     Debug.Log($"Loading next scene {nextGameScene}");
                    //     var nextGameSceneEntity = Commands.Instantiate(nextGameScene);
                    //     matchData.GameSceneInstance = nextGameSceneEntity;
                    //
                    //     Commands.SetComponent(nextGameSceneEntity, new SceneLoadingState { Value = SceneLoadingStates.PendingStartLoading });
                    //     Commands.SetComponent(nextGameSceneEntity, new SessionEntityReference { Value = entity });
                    //
                    //     Commands.AddComponent(entity, matchData);
                    // }
                }
            }

            public override void OnUpdate(Entity entity)
            {
                base.OnUpdate(entity);

                if(ArenaMatchUtility.IsGameSceneLoaded(this, entity))
                {
                    RequestStateChange<RunningStateEx>(entity);
                }
            }
        }

        class RunningStateEx : Running
        {
            private PlayerPrefab playerPrefab;
            
            public override void OnBeforeUpdate()
            {
                base.OnBeforeUpdate();
                if (System.HasSingleton<PlayerPrefab>())
                {
                    playerPrefab = System.GetSingleton<PlayerPrefab>();    
                }
            }

            public override void OnEnter(Entity entity)
            {
                base.OnEnter(entity);

                if(IsPendingStateChange(entity))
                {
                    return;
                }

                var arenaSystem = System as ArenaMatchSystem;
                var em = EntityManager;

                uint maxStage = 0;
                //uint maxLeague = 0;
                var playersBuffer = em.GetBuffer<RegisteredPlayer>(entity);
                var initData = GetComponent<SessionInitializationData>(entity);
                

                // DEBUG
                //var commands = new Utility.DebugCommandBuffer(em); //System.PostUpdateCommands;
                //using (var players = playersBuffer.ToNativeArray(Allocator.Temp))
                // DEBUG

                var playerSpawnPoints = arenaSystem.spawnPointsQuery.ToEntityArray(Allocator.Temp);
                
                for (int i = 0; i < playersBuffer.Length; i++)
                {
                    var playerElement = playersBuffer[i];
                    var player = playerElement.PlayerEntity;

                    var netPlayer = em.GetComponentData<NetworkPlayer>(player);
                    arenaSystem.getSpawnPositionForPlayer(playerSpawnPoints, initData.SpawnPointId, out float3 spawnPos, out float cameraWorldYaw);
                    var characterData = ArenaMatchUtility.SetupPlayerCharacter(this, initData.IsLocalGame, playerPrefab.Value, spawnPos, cameraWorldYaw, player, netPlayer, ref Commands);

                    if (characterData.Progress != null && maxStage < characterData.Progress.CurrentStage)
                    {
                        maxStage = (uint)characterData.Progress.CurrentStage;
                    }
                }

                playerSpawnPoints.Dispose();

                var arenaState = new ArenaMatchStateData
                {
                    State = ArenaMatchState.Preparing,
                    //CurrentLeague = (byte)maxLeague,
                    CurrentStage = (ushort)maxStage,
                };

                if(HasComponent<NetworkID>(entity) == false)
                {
                    Commands.AddComponent(entity, new NetworkID());
                }

                if (em.HasComponent<ArenaMatchStateData>(entity) == false)
                {
                    Commands.AddComponent(entity, arenaState);
                }
                else
                {
                    em.SetComponentData(entity, arenaState);
                }

                if(HasBuffer<IncomingMessages>(entity) == false)
                {
                    Commands.AddBuffer<IncomingMessages>(entity);

                    var registerRequest = Commands.CreateEntity();
                    var buffer = Commands.AddBuffer<RegisterListenerRequestMessage>(registerRequest);
                    buffer.Add(new RegisterListenerRequestMessage { Listener = entity, MessageToListen = MatchFinishedMessage });
                }
            }

            public override void OnUpdate(Entity entity)
            {
                base.OnUpdate(entity);

                var em = EntityManager;

                if(em.HasComponent<ArenaMatchStateData>(entity) == false)
                {
                    return;
                }

                var internalState = em.GetComponentData<ArenaMatchStateData>(entity);
                var players = em.GetBuffer<RegisteredPlayer>(entity);

                switch (internalState.State)
                {
                    case ArenaMatchState.Preparing:
                        {
                            internalState.State = ArenaMatchState.Fighting;
                            internalState.IsMatchFailed = false;
                            internalState.IsMatchComplete = false;
                            internalState.Saved = false;
                            em.SetComponentData(entity, internalState);

                            //var spawnerSystem = System as ArenaMatchSystem;

                            //for (int i = 0; i < spawnerSystem.collectedSpawners.Length; i++)
                            //{
                            //    var spawnerEntity = spawnerSystem.collectedSpawners[i];

                            //    var spawner = em.GetComponentData<ArenaSpawner>(spawnerEntity);
                            //    spawner.CurrentGroup = internalState.CurrentStage;
                            //    spawner.State = ArenaSpawnerState.PendingStart;
                            //    commands.SetComponent(spawnerEntity, spawner);

                            //    var level = new Level { Value = (ushort)(internalState.CurrentLeague * MaxStage + internalState.CurrentStage) };
                            //    if (em.HasComponent<Level>(spawnerEntity))
                            //    {
                            //        commands.SetComponent(spawnerEntity, level);
                            //    }
                            //    else
                            //    {
                            //        commands.AddComponent(spawnerEntity, level);
                            //    }
                            //}

                            if (IsAllPlayersDead(ref players))
                            {
                                RestartPlayers(ref players, entity);
                            }
                        }
                        break;
                    case ArenaMatchState.Fighting:
                        {
                            bool isFinished = false;
                            bool success = false;

                            if(internalState.IsMatchComplete)
                            {
                                success = true;
                                isFinished = true;
                            }
                            
                            if (IsAllPlayersDead(ref players))
                            {
                                success = false;
                                isFinished = true;
                            }

                            if (isFinished)
                            {
                                internalState.IsMatchComplete = true;

                                if(success)
                                {
                                    internalState.CurrentStage++;
                                    Debug.Log($"curr stage is {internalState.CurrentStage}");
                                    
                                    internalState.IsMatchFailed = false;

                                    var matchData = GetComponent<SceneData>(entity);
                                    //var gameSceneDesc = GetComponent<GameSceneDescription>(matchData.GameSceneInstance);

                                    // TODO
                                    //var gameSceneNode = GetComponent<CampaignTools.GameSceneNodeEntityReference>(matchData.GameSceneInstance);
                                    //var gameSceneNodeType = GetComponent<CampaignTools.GameSceneNodeType>(gameSceneNode.Value);
                                    //internalState.IsNextSceneAvailable = gameSceneNodeType.Value != CampaignTools.SceneNodeTypes.End;
                                    internalState.IsNextSceneAvailable = false;
                                }
                                else
                                {
                                    //FinishMatch(entity, ref players);
                                    internalState.IsMatchFailed = true;
                                }

                                double currentTime;

                                var initData = GetComponent<SessionInitializationData>(entity);

                                if (initData.IsLocalGame)
                                {
                                    currentTime = (System as ArenaMatchSystem).TimeSystem.GameTime;
                                }
                                else
                                {
                                    currentTime = (System as ArenaMatchSystem).GetNetworkTime();
                                }

                                internalState.State = ArenaMatchState.WaitingForNextStep;
                                internalState.MatchEndTime = currentTime;
                                internalState.DecisionWaitTime = 60;

                                em.SetComponentData(entity, internalState);
                                
                                RequestStateChange<WaitingForNextStepState>(entity);
                            }
                        }
                        break;

                    case ArenaMatchState.WaitingForNextStep:
                        {
                            RequestStateChange<WaitingForNextStepState>(entity);
                        }
                        break;
                }
            }

            //void FinishMatch(Entity matchEntity, ref DynamicBuffer<RegisteredPlayer> players)
            //{
            //    Debug.Log($"Match finished, entity {matchEntity.Index}");

            //    if (players.IsCreated == false)
            //    {
            //        Debug.LogErrorFormat("Failed to get players array from {0}", matchEntity);
            //        return;
            //    }
            //    RestartPlayers(ref players);

            //    //var matchSystem = System as ArenaMatchSystem;

            //    //for (int i = 0; i < matchSystem.collectedSpawners.Length; i++)
            //    //{
            //    //    var spawnerEntity = matchSystem.collectedSpawners[i];
            //    //    var stopRequest = Commands.CreateEntity();
            //    //    Commands.AddComponent(stopRequest, new StopSpawnAndDestroySpawned() { Spawner = spawnerEntity });
            //    //}
            //}

            bool IsAllPlayersDead(ref DynamicBuffer<RegisteredPlayer> players)
            {
                var em = EntityManager;

                if (players.Length > 0)
                {
                    bool isAllPlayersDead = true;

                    for (int i = 0; i < players.Length; i++)
                    {
                        var player = players[i].PlayerEntity;

                        if(em.HasComponent<ControlledCharacter>(player) == false)
                        {
                            isAllPlayersDead = false;
                            break;
                        }

                        var character = em.GetComponentData<ControlledCharacter>(player).Entity;
                        var livingState = em.GetComponentData<LivingState>(character);

                        if (livingState.IsAlive)
                        {
                            isAllPlayersDead = false;
                            break;
                        }
                    }

                    if (isAllPlayersDead)
                    {
                        return true;
                    }
                }

                return false;
            }

            void RestartPlayers(ref DynamicBuffer<RegisteredPlayer> playersBuffer, Entity sessionEntity)
            {
                var em = EntityManager;

                // DEBUG
                //var commands = new Utility.DebugCommandBuffer(em);// System.PostUpdateCommands;
                //using(var players = playersBuffer.ToNativeArray(Allocator.Temp))
                //

                var playerSpawnPoints = (System as ArenaMatchSystem).spawnPointsQuery.ToEntityArray(Allocator.Temp);
                var databaseEntity = System.GetSingletonEntity<MainDatabaseTag>();
                var databaseBuffer = em.GetBuffer<IdToEntity>(databaseEntity);
                var initData = em.GetComponentData<SessionInitializationData>(sessionEntity);

                for (int i = 0; i < playersBuffer.Length; i++)
                {
                    var player = playersBuffer[i].PlayerEntity;

                    var currentCharacter = em.GetComponentData<ControlledCharacter>(player).Entity;
                    var livingState = em.GetComponentData<LivingState>(currentCharacter);

                    if (livingState.IsAlive == false)
                    {
                        //commands.AddComponent(player, new Resurrect { HP = float.MaxValue });
                        var playerData = em.GetComponentData<PlayerData>(player).Data as CharacterData;
                        
                        Commands.DestroyEntity(currentCharacter);
                        Commands.RemoveComponent<ControlledCharacter>(player);

                        var arenaSystem = System as ArenaMatchSystem;
                        
                        using (var database = databaseBuffer.ToNativeArray(Allocator.Temp))
                        {
                            arenaSystem.getSpawnPositionForPlayer(playerSpawnPoints, initData.SpawnPointId, out float3 spawnPos, out float cameraWorldYaw);
                            ArenaMatchUtility.CreateCharacter(playerPrefab.Value, player, spawnPos, cameraWorldYaw, database, playerData, Commands);    
                        }
                    }
                }
            }
        }
    }
}
