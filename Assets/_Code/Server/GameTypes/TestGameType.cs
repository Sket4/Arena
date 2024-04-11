using System.Threading.Tasks;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Server;
using TzarGames.MultiplayerKit;
using TzarGames.MultiplayerKit.Server;
using UnityEngine;

namespace Arena.Server
{
    [CreateAssetMenu(fileName = "TestGameType", menuName = "Arena/Game types/Test game type")]
    public class TestGameType : GameType
    {
        [SerializeField]
        ServerGameSettings gameSettings = default;

        GameServerLoop gameServer;

        IServerLauncher serverManager;

        public override string GameTypeName => "TEST_GAME_TYPE";

        public override async Task<HandleGameRequestResult> HandleGameRequest(ServerGameRequest gameRequest)
        {
            var matchInfo = new GameSessionInfo();

            foreach (var userId in gameRequest.UserRequests)
            {
                matchInfo.AllowedUserIds.Add(new PlayerId { Value = userId.UserId.Value });
            }

            bool testMode = false;

            gameServer = serverManager.CreateServer(gameSettings) as GameServerLoop;
            gameServer.Start();

            var testMatchSystem = gameServer.AddGameSystem<TestMatchSystem>(gameServer);
            var authSystem = gameServer.AddGameSystem<AuthorizationSystem>();
            if (testMode)
            {
                authSystem.SkipAuthorization = true;
            }

            var idSystem = gameServer.World.GetExistingSystemManaged<NetworkIdentitySystem>();
            var rpcSystem = gameServer.World.GetExistingSystemManaged<NetworkRpcSystem>();
            gameServer.AddGameSystem<PlayerDataOnlineStoreSystem>(testMode);

            var createSessionResult = await testMatchSystem.CreateGameSessionAsync(matchInfo);
            Debug.Log("Finished creating a match");

            return new HandleGameRequestResult(gameServer, createSessionResult.GameID, authSystem.PublicEncryptionKey);
        }

        public override Task Initialize(IServerLauncher serverManager)
        {
            this.serverManager = serverManager;
            return Task.CompletedTask;
        }
        
        public override Task<AddUsersToGameResult> HandleAddUsersRequest(AddUsersToGameRequest request)
        {
            return Task<AddUsersToGameResult>.FromResult(new AddUsersToGameResult { Success = false });
        }
    }
}
