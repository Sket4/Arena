using Grpc.Core;
using Arena.Server;
using TzarGames.MatchFramework;

namespace DatabaseGameLib
{
    public class DatabaseUtility
    {
        public static ServiceWrapper<GameDatabaseService.GameDatabaseServiceClient> CreateDatabaseClient(string ip = "127.0.0.1", int port = 50052, string sslCertificate = null)
        {
            ChannelCredentials credentials;

            if(string.IsNullOrEmpty(sslCertificate))
            {
                credentials = ChannelCredentials.Insecure;
            }
            else
            {
                credentials = new SslCredentials(sslCertificate);
            }

            return new ServiceWrapper<GameDatabaseService.GameDatabaseServiceClient>(ip, port, credentials);
        }
    }
}
