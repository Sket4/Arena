#if UNITY_EDITOR || UNITY_SERVER

using Unity.Entities;
using UnityEngine;
using TzarGames.MultiplayerKit;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.GameCore.Server;
using TzarGames.MultiplayerKit.Server;
using TzarGames.MatchFramework.Server;
using TzarGames.MatchFramework;
using System.Collections.Generic;
using Arena.GameSceneCode;
using Arena.GameSceneCode.Server;
using Unity.Core;
using Unity.Mathematics;

namespace Arena.Server
{
    public class GameServerLoop : GameServerLoopBase, IGameServer
    {
        private EntityQuery playersQuery;
        TimeSystem timeSystem;

        public int TargetTickRate = 60;
        const float MaxServerUpdateDelta = 1.0f / 10.0f;
        double lastTickTime = -1;
        float accumulatedTickTime = 0.0f;
        public int MaxTicksPerUpdate = 10;

        /// <summary>
        /// время ожидания для получения пропущенных комманд клиента
        /// </summary>
        public float MaxMissedCommandWaitTime = 0.3f;

        public ServerGameSettings GameSettings { get; private set; }
        ServiceWrapper<GameDatabaseService.GameDatabaseServiceClient> dbClient = default;

        public GameServerLoop(IServerGameSettings gameSettings, Unity.Entities.Hash128[] additionalScenes, IAuthorizationService authorizationService, ServerAddress dbServerAddress) : base(gameSettings)
        {
            InitSceneLoading(additionalScenes);
            
            GameSettings = gameSettings as ServerGameSettings;

            Utils.AddSharedSystems(this, true, "Server");

            timeSystem = World.GetExistingSystemManaged<TimeSystem>();
            timeSystem.UseCustomTime = true;

            AddGameSystem<AuthorizationSystem>(authorizationService);
            var dbClient = DatabaseGameLib.DatabaseUtility.CreateDatabaseClient(dbServerAddress.Address, (int)dbServerAddress.Port, dbServerAddress.Certificate.text);
            AddGameSystem<PlayerDataOnlineStoreSystem>(dbClient);

            AddPreSimGameSystem<PlayerInputReceiveSystem>();
            AddGameSystem<InputCommandPostprocessSystem>();

            AddGameSystem<TzarGames.GameCore.Server.Abilities.Networking.AbilityStateServerNetSyncSystem>();
            AddGameSystem<ItemPickupNotifySystem>(NetIdentity.ID);

            AddGameSystem<PositionHistorySystem>();

            // для приема сообщений о состоянии загруженных сцен на клиенте
            AddGameSystem<SceneLoaderServerNotificationSystem>();

            // для управления загрузкой игровых сцен
            AddGameSystem<GameSceneSystem>();
            AddGameSystem<ServerGameSceneSystem>();
            
#if UNITY_EDITOR
            AddGameSystem<SyncHybridTransformSystem>();
#endif

            // для синхронизации попаданий и их урона
            AddGameSystem<HitSyncSystem>(true);

            DataSyncSystemGroup.CreateNetworkRelevancySystem<MarkEverythingAsRelevantSystem>();
            DataSyncSystemGroup.CreateNetworkRelevancySystem<NetworkDistanceRelevancySystem>();
            DataSyncSystemGroup.CreateNetworkRelevancySystem<NetworkItemRelevancySystem>();

            Utils.AddDataSyncers(DataSyncSystemGroup.DataSyncSystem);
            
            AddGameSystem<ServerNetIdAllocSystem>();
            playersQuery = World.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<TzarGames.MultiplayerKit.NetworkPlayer>(),
                    ComponentType.ReadOnly<ControlledCharacter>(),
                    ComponentType.ReadOnly<PlayerInputInfo>(),
                    typeof(ServerPlayerInputCommand));
        }

        public override void Dispose()
        {
            base.Dispose();
            
            if(dbClient != null)
            {
                dbClient.ShutdownAsync();
            }
        }

        struct PlayerServerTickInfo
        {
            public Entity PlayerEntity;
            public Entity PlayerCharacterEntity;
            public List<PlayerInputCommand> Commands;
        }

        protected override void OnSimulationUpdate()
        {
            var targetTickTime = 1.0f / TargetTickRate;
            float serverUpdateDelta;
            var currentTime = ServerSystem.NetTime;

            if (lastTickTime >= 0)
            {
                serverUpdateDelta = (float)(currentTime - lastTickTime);

                if (serverUpdateDelta > MaxServerUpdateDelta)
                {
                    UnityEngine.Debug.LogWarning($"Превышена максимальная дельта времени обновления сервера: текущая: {serverUpdateDelta} макс: {MaxServerUpdateDelta}. Дельта будет приравнена к максимально допустимой, течение времени при этом замедлится");
                    serverUpdateDelta = MaxServerUpdateDelta;
                }
            }
            else
            {
                serverUpdateDelta = MaxServerUpdateDelta;
            }

            lastTickTime = currentTime;
            var em = World.EntityManager;

            using (var playerEntities = playersQuery.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                // ищем игрока с наибольшим количеством команд
                var maxCommandCount = int.MinValue;

                foreach (var playerEntity in playerEntities)
                {
                    var commands = em.GetBuffer<ServerPlayerInputCommand>(playerEntity);
                    if (commands.Length >= maxCommandCount)
                    {
                        maxCommandCount = commands.Length;
                    }
                }

                if (maxCommandCount > 0)
                {
                    if (maxCommandCount > 6)
                    {
                        //UnityEngine.Debug.LogWarning($"MaxCommandCount is too big {maxCommandCount}");
                        maxCommandCount = 6;
                    }
                    targetTickTime = math.clamp(serverUpdateDelta / maxCommandCount, 0, targetTickTime);
                }

                //Debug.Log($"Target tick time: {targetTickTime}, serverUpdateDelta: {serverUpdateDelta} max command count: {maxCommandCount}");
                
                while (true)
                {
                    timeSystem.SetCustomTime(targetTickTime, currentTime);
                    
                    //var timeBeforeTick = ServerSystem.NetTime; 
                    accumulatedTickTime += targetTickTime;

                    // выполняем апдейт сервера с примененными командами ввода от игроков
                    base.OnSimulationUpdate();

                    if (accumulatedTickTime >= serverUpdateDelta)
                    {
                        //UnityEngine.Debug.Log($"Обновления за тик {updates}, дельта {serverUpdateDelta}");
                        accumulatedTickTime -= serverUpdateDelta;
                        break;
                    }
                }
            }

            //var tickTime = ServerSystem.NetTime - currentTime;
            //totalAccumulatedTickTime += tickTime;
            //totalAccumulatedTicks++;

            //if(totalAccumulatedTickTime >= 1.0f)
            //{
            //    UnityEngine.Debug.Log($"Average tick time: {totalAccumulatedTickTime / totalAccumulatedTicks}, accumulated ticks: {totalAccumulatedTicks}");

            //    totalAccumulatedTicks = 0;
            //    totalAccumulatedTickTime -= 1.0;
            //}
        }

        [DisableAutoCreation]
        [UpdateAfter(typeof(SpawnerSystem))]
        [UpdateBefore(typeof(GameCommandBufferSystem))]
        partial class ServerNetIdAllocSystem : NetworkIdAllocatorSystem
        {
        }
    }
}
#endif