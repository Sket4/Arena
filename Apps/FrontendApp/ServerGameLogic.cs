using Arena.Server;

namespace FrontendApp
{
    public class ServerGameLogic
    {
        GameDatabaseService.GameDatabaseServiceClient dbClient;

        public ServerGameLogic(GameDatabaseService.GameDatabaseServiceClient dbClient)
        {
            this.dbClient = dbClient;
        }
    }
}
