using System;
using System.Collections;
using System.Collections.Generic;
using Arena.Maze;
using Arena.Quests;
using TzarGames.GameCore;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Client;
using UnityEngine;
using Random = UnityEngine.Random;

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

        [SerializeField] private bool debugGenerateMaze;

        [SerializeField]
        CharacterClass debugCharacterClass;

        [SerializeField] private GameSceneKey debugGameSceneKey = default;
        [SerializeField] private SpawnPointID debugSpawnPointID = default;

        [Serializable]
        class DebugQuestEntry
        {
            public QuestKey QuestKey = default;
            public QuestState State = QuestState.Active;
            public bool Exclude;
        }
        
        [Serializable]
        class DebugProgressIntEntry
        {
            public GameProgressIntKey Key = default;
            public int Value;
        }
        
        [SerializeField] private DebugQuestEntry[] debugQuests;
        [SerializeField] private DebugProgressIntEntry[] debugGameProgressIntegers;

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

            if (gameState)
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

            if (debugQuests != null && debugQuests.Length > 0)
            {
                var questList = new List<CharacterGameProgressQuests>();
                foreach (var debugQuest in debugQuests)
                {
                    if (debugQuest == null || debugQuest.QuestKey == false || debugQuest.Exclude)
                    {
                        continue;
                    }
                    questList.Add(new CharacterGameProgressQuests
                    {
                        QuestID = (ushort)debugQuest.QuestKey.Id,
                        QuestState = debugQuest.State
                    });
                }
                storeSystem.DebugQuests = questList;
            }

            if (debugGameProgressIntegers != null && debugGameProgressIntegers.Length > 0)
            {
                var kvList = new List<CharacterGameProgressKeyValue>();

                foreach (var kv in debugGameProgressIntegers)
                {
                    if (kv == null || kv.Key == null)
                    {
                        continue;
                    }
                    kvList.Add(new CharacterGameProgressKeyValue
                    {
                        Key = (ushort)kv.Key.Id,
                        Value = kv.Value
                    });
                }
                storeSystem.DebugGameProgressIntKeys = kvList;
            }

            var gameSceneId = useDebugSettings ? debugGameSceneKey.Id : GameState.GetOfflineGameInfo().GameSceneID;
            var spawnPointId = useDebugSettings ? debugSpawnPointID ? debugSpawnPointID.Id : 0 : GameState.GetOfflineGameInfo().SpawnPointID;
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
                                gameState.GoToBaseLocation();
                            }
                        };
                        gameLoop.AddGameSystem<ArenaSpawnerSystem>();
                        gameLoop.AddGameSystemUnmanaged<SpawnZoneSystem>();
                        gameLoop.AddGameSystem<MazeBuilderSystem>();
                        gameLoop.AddGameSystemUnmanaged<UpdateLinkedTransformsSystem>();
                        StartCoroutine(startMatch(matchSystem, gameSceneId, spawnPointId));
                    }
                    break;
                case GameLocationType.SafeZone:
                    {
                        var matchSystem = gameLoop.AddGameSystem<Server.SafeAreaMatchSystem>();
                        StartCoroutine(startSafeArea(matchSystem, gameSceneId, spawnPointId));
                    }
                    break;
            }
            
            return gameLoop;
        }

        IEnumerator startSafeArea(Server.SafeAreaMatchSystem matchSystem, int gameSceneId, int spawnPointId)
        {
            var matchInfo = new ArenaGameSessionInfo(gameSceneId, spawnPointId, true);
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

        IEnumerator startMatch(Server.ArenaMatchSystem matchSystem, int gameSceneId, int spawnPointId)
        {
            ArenaGameSessionInfo matchInfo;

            if (debugGenerateMaze)
            {
                var genSeed = (uint)Random.Range(0, int.MaxValue);
                matchInfo = new ArenaMazeGameSessionInfo(gameSceneId, spawnPointId, true, genSeed);
            }
            else
            {
                matchInfo = new ArenaGameSessionInfo(gameSceneId, spawnPointId, true);
            }
            
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

        static void testMazeGen(GameLoopBase gameLoop)
        {
            var em = gameLoop.World.EntityManager;
            
            var request = new BuildMazeRequest
            {
                Seed = (uint)Random.Range(0, int.MaxValue),
                State = BuildMazeRequestState.Pending,
                StartCellCount = 1,
                HorizontalCells = 10,
                VerticalCells = 10,
                Builder = em.CreateEntityQuery(typeof(MazeWorldBuilder)).GetSingletonEntity()
            };
            var requestEntity = em.CreateEntity();
            em.AddComponentData(requestEntity, request);
        }
    }
}
