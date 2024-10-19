using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TzarGames.GameCore;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Server;
using UnityEngine;

namespace Arena.Server
{
    [CreateAssetMenu(fileName = "MainAreaGameType", menuName = "Arena/Game types/Main area game type")]
    public class MainAreaGameType : GameType
    {
        [SerializeField]
        GameSceneKey sceneID;

        [SerializeField]
        string gameTypeName;

        [SerializeField]
        ServerGameSettings gameSettings = default;
        
        GameServerLoop gameServer;
        IServerLauncher serverManager;
        SafeAreaMatchSystem matchSystem;
        AuthorizationSystem authorizationSystem;
        
        public override string GameTypeName => gameTypeName;

        private GameSessionID GameSessionID;

        public override async Task<HandleGameRequestResult> HandleGameRequest(ServerGameRequest gameRequest)
        {
            var playerIds = new List<PlayerId>();

            foreach (var userId in gameRequest.UserRequests)
            {
                playerIds.Add(new PlayerId { Value = userId.UserId.Value });
            }

            var addResult = await matchSystem.AddAllowedUsersToGame(GameSessionID, playerIds.ToArray());

            if(addResult == false)
            {
                return null;
            }

            return new HandleGameRequestResult(gameServer, GameSessionID, authorizationSystem.PublicEncryptionKey);
        }

        public override async Task<AddUsersToGameResult> HandleAddUsersRequest(AddUsersToGameRequest request)
        {
            if (System.Guid.TryParse(request.GameID, out var guidValue) == false
                || GameSessionID.Value != guidValue)
            {
                return new AddUsersToGameResult { Success = false };
            }
            
            var playerIds = new List<PlayerId>();

            foreach (var userId in request.Users)
            {
                playerIds.Add(new PlayerId { Value = userId.UserId.Value });
            }
            
            var addResult = await matchSystem.AddAllowedUsersToGame(GameSessionID, playerIds.ToArray());

            return new AddUsersToGameResult { Success = addResult };
        }

        public override async Task Initialize(IServerLauncher serverManager)
        {
            this.serverManager = serverManager;


            gameServer = serverManager.CreateServer(gameSettings) as GameServerLoop;

            //var idSystem = gameServer.World.GetExistingSystemManaged<NetworkIdentitySystem>();
            //var rpcSystem = gameServer.World.GetExistingSystemManaged<NetworkRpcSystem>();
            matchSystem = gameServer.AddGameSystem<SafeAreaMatchSystem>();

            // в безопасной зоне сохраняем изменения сразу же
            var itemActivationGroup = gameServer.World.GetExistingSystemManaged<ItemActivationSystemGroup>();
            gameServer.AddGameSystem<ActivateItemRequestProcessSystem>(itemActivationGroup);
            
            matchSystem.OnUserDisconnected += (user, gameSessionId) =>
            {
                CallOnPlayersRemovedFromGameSession(gameSessionId, new PlayerId[] { user });
            };

            //matchSystem.OnMatchFailed += (entity) =>
            //{
            //    gameServer.PendingShutdown = true;
            //};

            authorizationSystem = gameServer.World.GetExistingSystemManaged<AuthorizationSystem>();
            authorizationSystem.SkipAuthorization = DebugMode;

            gameServer.AddGameSystem<ArenaMatchNetSystem>();

            var matchInfo = new ArenaGameSessionInfo(sceneID.Id, 0, false, null);
            matchInfo.AllowedUserIds = new List<PlayerId>();

            Debug.Log($"Trying to create game session");
            var result = await matchSystem.CreateGameSessionAsync(matchInfo);

            if (result.Success == false)
            {
                Debug.LogError("Failed to create game session");
                gameServer.PendingShutdown = true;
                return;
            }
            GameSessionID = result.GameID;

            Debug.Log($"Game session created {result.Success} {result.GameID}");
        }
    }
}
