using System.Threading.Tasks;
using Arena.Server;
using TzarGames.MatchFramework.Server;

namespace Arena.Tests
{
    public class TestEmptyGameType : GameType
    {
        public ServerGameSettings ServerGameSettings;
        public override string GameTypeName => "Test empty game type";
        GameServerLoop gameServer;

        public override Task<HandleGameRequestResult> HandleGameRequest(ServerGameRequest gameRequest)
        {
            //var matchSystem = gameServer.World.GetExistingSystemManaged<ArenaMatchSystem>();
            return Task.FromResult(new HandleGameRequestResult(gameServer, default, null));
        }

        public override Task Initialize(IServerLauncher serverManager)
        {
            gameServer = serverManager.CreateServer(ServerGameSettings) as GameServerLoop;
            return Task.CompletedTask;
        }
        
        public override Task<AddUsersToGameResult> HandleAddUsersRequest(AddUsersToGameRequest request)
        {
            return Task<AddUsersToGameResult>.FromResult(new AddUsersToGameResult { Success = false });
        }
    }
}
