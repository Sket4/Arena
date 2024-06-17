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

        public bool IsSafeArea(int sceneId)
        {
            if(sceneId == 63)
            {
                return true;
            }
            return false;
        }
    }
}
