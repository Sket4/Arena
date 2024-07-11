using System.Threading.Tasks;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Server;
using TzarGames.MultiplayerKit;
using TzarGames.MultiplayerKit.Server;
using UnityEngine;

namespace Arena.Server
{
    [CreateAssetMenu(fileName = "ArenaGameType", menuName = "Arena/Game types/Arena match game type")]
    public class ArenaGameType : GameType
    {
        [SerializeField]
        ServerGameSettings gameSettings = default;
        IServerLauncher serverManager;

        public override string GameTypeName { get { return "ArenaMatch"; } }

        public override async Task<HandleGameRequestResult> HandleGameRequest(ServerGameRequest gameRequest)
        {
            if (gameRequest.MetaData.IntKeyValues.TryGet(MetaDataKeys.SceneId, out int sceneId) == false)
            {
                Debug.Log("No scene id in game request");
                return null;
            }

            gameRequest.MetaData.IntKeyValues.TryGet(MetaDataKeys.SpawnPointId, out int spawnPoindId);

            var matchInfo = new ArenaGameSessionInfo(sceneId, spawnPoindId, false);

            foreach (var userId in gameRequest.UserRequests)
            {
                matchInfo.AllowedUserIds.Add(new PlayerId { Value = userId.UserId.Value });
            }

            var gameServer = serverManager.CreateServer(gameSettings) as GameServerLoop;

            var idSystem = gameServer.World.GetExistingSystemManaged<NetworkIdentitySystem>();
            var rpcSystem = gameServer.World.GetExistingSystemManaged<NetworkRpcSystem>();
            gameServer.AddGameSystem<ArenaSpawnerSystem>();
            gameServer.AddGameSystem<DifficultySystem>(true);
            var matchSystem = gameServer.AddGameSystem<ArenaMatchSystem>();
            matchSystem.OnMatchFailed += (entity) =>
            {
                Debug.Log("match failed, pending shutdown...");
                gameServer.PendingShutdown = true;
            };
            matchSystem.OnReadyToShutdown += () =>
            {
                Debug.Log("match finished, pending shutdown...");
                gameServer.PendingShutdown = true;
            };
            
            matchSystem.OnUserDisconnected += (user, gameSessionId) =>
            {
                CallOnPlayersRemovedFromGameSession(gameSessionId, new PlayerId[] { user });
            };

            var authSystem = gameServer.World.GetExistingSystemManaged<AuthorizationSystem>();
            authSystem.SkipAuthorization = DebugMode;

            gameServer.AddGameSystem<ArenaMatchNetSystem>();

            Debug.Log($"Trying to create game session");
            var result = await matchSystem.CreateGameSessionAsync(matchInfo);
            
            if (result.Success == false)
            {
                Debug.LogError("Failed to create game session");
                gameServer.PendingShutdown = true;
                return null;
            }

            Debug.Log($"Game session created {result.Success} {result.GameID}");

            return new HandleGameRequestResult(gameServer, result.GameID, authSystem.PublicEncryptionKey);
        }
        
        public override async Task Initialize(IServerLauncher serverManager)
        {
            this.serverManager = serverManager;
        }

        public override Task<AddUsersToGameResult> HandleAddUsersRequest(AddUsersToGameRequest request)
        {
            return Task<AddUsersToGameResult>.FromResult(new AddUsersToGameResult { Success = false });
        }
    }
}
