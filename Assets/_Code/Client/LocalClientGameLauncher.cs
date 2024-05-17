using System.Collections;
using TzarGames.GameCore;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Client;
using UnityEngine;

namespace Arena.Client
{
    /// создание лаунчера для локальной игры
    public class LocalClientGameLauncher : BaseClientGameLauncher
    {
        [SerializeField]
        ClientGameSettings clientGameSettings;

        [Header("Debug settings")]
        [SerializeField] private bool useDebugSettings = false;

        [SerializeField]
        GameLocationType debugLocationType;

        [SerializeField]
        CharacterClass debugCharacterClass;

        [SerializeField] private GameSceneKey debugGameSceneKey = default;

        protected override void Start()
        {
            base.Start();
            Application.targetFrameRate = 60;
            Debug.Log($"Graphics device type: {SystemInfo.graphicsDeviceType}");
        }

        protected override GameLoopBase CreateGameLoop(Unity.Entities.Hash128[] additionalScenes)
        {
            //Debug.Log("Jobs debuffer");
            //Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobCompilerEnabled = false;
        
            var gameLoop = new LocalGameClient("Local game", clientGameSettings, additionalScenes);

            var em = gameLoop.World.EntityManager;
            
            var gameState = GameState.Instance;

            if (gameState != null)
            {
                var gameInterfaceEntity = em.CreateEntity();
                em.AddComponentObject(gameInterfaceEntity, new GameInterface(gameState));
            }
            
            var player = em.CreateEntity();

            Debug.Log("Создание локального игрока");
            em.AddComponentData(player, new AuthorizedUser { Value = new PlayerId { Value = 1 } });
            em.AddComponentData(player, new TzarGames.MultiplayerKit.NetworkPlayer(0, true, true));
            em.AddComponentData(player, new LocalPlayerTag());

            var storeSystem = gameLoop.World.GetExistingSystemManaged<ArenaPlayerDataLocalStoreSystem>();
            storeSystem.DebugCharacterClass = debugCharacterClass;

            var gameSceneId = useDebugSettings ? debugGameSceneKey.Id : GameState.GetOfflineGameInfo().GameSceneID;
            var gameLocationType = useDebugSettings ? debugLocationType : GameState.GetOfflineGameInfo().MatchType == "Town_1" ? GameLocationType.SafeZone : GameLocationType.Arena;
            
            switch (gameLocationType)
            {
                case GameLocationType.Arena:
                    {
                        var matchSystem = gameLoop.AddGameSystem<Server.ArenaMatchSystem>();
                        matchSystem.OnMatchFailed += failedMatchEntity =>
                        {
                            Debug.Log("Local game match failed");
                            if (gameState != null)
                            {
                                gameState.GoToSafeZone();
                            }
                        };
                        gameLoop.AddGameSystem<ArenaSpawnerSystem>();
                        gameLoop.AddGameSystemUnmanaged<SpawnZoneSystem>();
                        StartCoroutine(startMatch(matchSystem, gameSceneId));
                    }
                    break;
                case GameLocationType.SafeZone:
                    {
                        var matchSystem = gameLoop.AddGameSystem<Server.SafeAreaMatchSystem>();
                        StartCoroutine(startSafeArea(matchSystem, gameSceneId));
                    }
                    break;
            }
            
            return gameLoop;
        }

        IEnumerator startSafeArea(Server.SafeAreaMatchSystem matchSystem, int gameSceneId)
        {
            var matchInfo = new ArenaGameSessionInfo(gameSceneId, true);
            matchInfo.AllowedUserIds = new System.Collections.Generic.List<PlayerId>
            {
                new PlayerId(1)
            };
            var task = matchSystem.CreateGameSessionAsync(matchInfo);
            while (task.IsCompleted == false)
            {
                yield return null;
            }
        }

        IEnumerator startMatch(Server.ArenaMatchSystem matchSystem, int gameSceneId)
        {
            var matchInfo = new ArenaGameSessionInfo(gameSceneId, true);
            matchInfo.AllowedUserIds = new System.Collections.Generic.List<PlayerId>
            {
                new PlayerId(1)
            };
            var task = matchSystem.CreateGameSessionAsync(matchInfo);
            while (task.IsCompleted == false)
            {
                yield return null;
            }
        }
    }
}
